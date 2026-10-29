using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    public class PlayerStatusTests
    {
        PlayerStatus st;

        [SetUp]
        public void SetUp() { st = new PlayerStatus(); }

        [Test]
        public void Comeca_com_barras_cheias_e_aura_70()
        {
            Assert.AreEqual(100f, st.Energy);
            Assert.AreEqual(100f, st.Sanidade);
            Assert.AreEqual(70f, st.Aura);
            Assert.AreEqual(0, st.ActiveAilments.Count);
        }

        [Test]
        public void Nao_duplica_mal()
        {
            Assert.IsTrue(st.Add(Ailment.Tosse));
            Assert.IsFalse(st.Add(Ailment.Tosse));
            Assert.IsFalse(st.Add(Ailment.Nenhum));
            Assert.AreEqual(1, st.ActiveAilments.Count);
        }

        [Test]
        public void Fissura_precisa_de_3_doses()
        {
            st.Add(Ailment.VontadeDeFumar);
            Assert.AreEqual(CureResult.Melhorou, st.CureDose(Ailment.VontadeDeFumar));
            Assert.AreEqual(CureResult.Melhorou, st.CureDose(Ailment.VontadeDeFumar));
            Assert.AreEqual(CureResult.Curado, st.CureDose(Ailment.VontadeDeFumar));
            Assert.IsFalse(st.Has(Ailment.VontadeDeFumar));
        }

        [Test]
        public void Dose_dupla_acelera_a_cura()
        {
            st.Add(Ailment.VontadeDeFumar);
            st.CureDose(Ailment.VontadeDeFumar, 2);
            Assert.AreEqual(1, st.DosesRestantes(Ailment.VontadeDeFumar));
        }

        [Test]
        public void Curar_mal_inexistente_retorna_NaoTinha()
        {
            Assert.AreEqual(CureResult.NaoTinha, st.CureDose(Ailment.Gripe));
        }

        [TestCase(Ailment.DorDeBarriga, 0.6f)]
        [TestCase(Ailment.DorRenal, 0.5f)]
        [TestCase(Ailment.Colica, 0.7f)]
        [TestCase(Ailment.Cansaco, 0.75f)]
        [TestCase(Ailment.Enjoo, 0.85f)]
        public void Males_fisicos_deixam_mais_lento(Ailment a, float esperado)
        {
            st.Add(a);
            Assert.AreEqual(esperado, st.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void Velocidades_se_multiplicam_e_buff_de_guarana_soma()
        {
            st.Add(Ailment.DorDeBarriga);
            st.Add(Ailment.Cansaco);
            Assert.AreEqual(0.45f, st.SpeedMultiplier, 0.0001f);
            st.BuffVelocidadePct = 0.5f;
            st.BuffVelocidadeSegundos = 10f;
            Assert.AreEqual(0.675f, st.SpeedMultiplier, 0.0001f);
        }

        [Test]
        public void Gripe_limita_energia_maxima()
        {
            st.Add(Ailment.Gripe);
            Assert.AreEqual(60f, st.MaxEnergy);
            Assert.AreEqual(60f, st.Energy);
            st.RestoreFull();
            Assert.AreEqual(60f, st.Energy);
        }

        [Test]
        public void Azia_dobra_custo_e_TrySpend_nao_gasta_se_faltar()
        {
            st.Add(Ailment.Azia);
            Assert.IsTrue(st.TrySpend(10f));
            Assert.AreEqual(80f, st.Energy);
            st.Drain(75f);
            Assert.IsFalse(st.TrySpend(5f));
            Assert.AreEqual(5f, st.Energy);
        }

        [Test]
        public void Sanidade_baixa_dispara_ansiedade_e_tremor()
        {
            st.LoseSanity(75f);
            Assert.IsTrue(st.Has(Ailment.Ansiedade));
            Assert.Greater(st.ScreenShake, 0f);
        }

        [Test]
        public void Ansiedade_dobra_perda_de_sanidade()
        {
            st.Add(Ailment.Ansiedade);
            st.LoseSanity(10f);
            Assert.AreEqual(80f, st.Sanidade);
        }

        [Test]
        public void Noite_drena_sanidade_e_dia_recupera()
        {
            st.LoseSanity(50f);
            st.Tick(10f, true);
            Assert.AreEqual(50f - PlayerStatus.PerdaSanidadeNoite * 10f, st.Sanidade, 0.001f);
            st.Tick(10f, false);
            Assert.AreEqual(50f - PlayerStatus.PerdaSanidadeNoite * 10f + PlayerStatus.RegenSanidadeDia * 10f, st.Sanidade, 0.001f);
        }

        [Test]
        public void Crise_de_fissura_drena_energia_a_cada_intervalo()
        {
            st.Add(Ailment.VontadeDeFumar);
            st.Drain(50f); // 50
            st.Tick(PlayerStatus.CriseFumoIntervalo, false);
            float regen = PlayerStatus.RegenEnergiaPorSegundo * PlayerStatus.CriseFumoIntervalo;
            Assert.AreEqual(50f + regen - PlayerStatus.CriseFumoEnergia, st.Energy, 0.001f);
        }

        [Test]
        public void Crise_de_fissura_treme_a_tela_por_um_instante()
        {
            st.Add(Ailment.VontadeDeFumar);
            Assert.AreEqual(0f, st.ScreenShake, "sanidade cheia e sem crise: a tela fica parada");
            st.Tick(PlayerStatus.CriseFumoIntervalo, false);
            Assert.AreEqual(1f, st.ScreenShake, 0.001f, "a crise dá um tranco na tela");
            st.Tick(PlayerStatus.CriseFumoTremor, false);
            Assert.AreEqual(0f, st.ScreenShake, "o tranco passa logo");
        }

        [Test]
        public void Fissura_em_espera_nao_tem_crise_ate_o_dia_seguinte()
        {
            st.Add(Ailment.VontadeDeFumar);
            st.FissuraEmEspera = true;
            st.Tick(PlayerStatus.CriseFumoIntervalo * 3f, false);
            Assert.AreEqual(100f, st.Energy, "nenhuma crise enquanto a fissura espera");
            st.OnNewDay();
            Assert.IsFalse(st.FissuraEmEspera, "no dia seguinte a fissura acorda, mesmo sem plantar");
            st.Tick(PlayerStatus.CriseFumoIntervalo, false);
            Assert.AreEqual(100f - PlayerStatus.CriseFumoEnergia, st.Energy, 0.001f);
        }

        [Test]
        public void Perda_de_energia_avisa_quanto_caiu_e_gasto_de_acao_nao()
        {
            float avisado = 0f;
            int avisos = 0;
            st.EnergyDrained += x => { avisado = x; avisos++; };
            Assert.IsTrue(st.TrySpend(10f));
            Assert.AreEqual(0, avisos, "gastar energia numa ação não pisca a barra");
            st.Drain(25f);
            Assert.AreEqual(25f, avisado, 0.001f);
            st.Drain(80f); // 65 → 0: só caiu 65
            Assert.AreEqual(65f, avisado, 0.001f);
            st.Drain(10f);
            Assert.AreEqual(2, avisos, "já no zero, não há o que avisar");
        }

        [Test]
        public void Amuleto_segura_a_aura()
        {
            st.AmuletoDias = 1;
            st.HitAura(50f);
            Assert.AreEqual(70f, st.Aura);
            st.OnNewDay();
            st.HitAura(50f);
            Assert.AreEqual(20f, st.Aura);
        }

        [Test]
        public void Aura_forte_resiste_ao_mau_olhado()
        {
            st.RestoreAura(100f);
            Assert.IsFalse(st.TryCurse(false));
            Assert.IsFalse(st.Has(Ailment.MauOlhado));
            Assert.AreEqual(90f, st.Aura);
        }

        [Test]
        public void Aura_fraca_pega_mau_olhado_mas_pimenteira_e_amuleto_bloqueiam()
        {
            st.HitAura(40f); // 30
            Assert.IsFalse(st.TryCurse(true), "dentro da aura da pimenteira");
            st.AmuletoDias = 1;
            Assert.IsFalse(st.TryCurse(false), "com amuleto");
            st.AmuletoDias = 0;
            Assert.IsTrue(st.TryCurse(false));
            Assert.IsTrue(st.Has(Ailment.MauOlhado));
        }

        [Test]
        public void Aura_baixa_atrai_espiritos_de_mais_longe()
        {
            float normal = st.AggroMultiplier;
            st.HitAura(70f);
            Assert.Greater(st.AggroMultiplier, normal);
            st.Add(Ailment.Tosse);
            Assert.AreEqual(3f, st.AggroMultiplier, 0.0001f);
        }

        [Test]
        public void Mau_olhado_e_dor_de_dente_baixam_preco_e_sorte_sobe()
        {
            st.Add(Ailment.DorDeDente);
            st.Add(Ailment.MauOlhado);
            Assert.AreEqual(0.56f, st.SellPriceMultiplier, 0.0001f);
            st.SorteDias = 1;
            Assert.AreEqual(0.728f, st.SellPriceMultiplier, 0.0001f);
        }

        [Test]
        public void Bloqueios_por_mal()
        {
            st.Add(Ailment.Queimadura);
            st.Add(Ailment.Insonia);
            st.Add(Ailment.MauOlhado);
            Assert.IsFalse(st.CanUseCauldron);
            Assert.IsFalse(st.SleepRestores);
            Assert.AreEqual(0.5f, st.HarvestMultiplier);
        }

        [Test]
        public void Revelar_expira_com_o_tempo()
        {
            st.RevelarSegundos = 5f;
            Assert.IsTrue(st.VeEspiritosOcultos);
            st.Tick(6f, false);
            Assert.IsFalse(st.VeEspiritosOcultos);
        }
    }
}
