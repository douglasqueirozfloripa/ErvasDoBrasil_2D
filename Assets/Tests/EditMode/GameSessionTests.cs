using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    public class GameSessionTests
    {
        GameSession s;
        FixedRandom rng;
        List<string> msgs;

        [SetUp]
        public void SetUp()
        {
            rng = new FixedRandom();
            s = new GameSession(TestData.Db(), rng, 100f, 4);
            msgs = new List<string>();
            s.Message += msgs.Add;
            s.StartNewGame();
        }

        [Test]
        public void Novo_jogo_da_kit_inicial_e_comeca_com_fissura()
        {
            Assert.AreEqual(GameSession.DinheiroInicial, s.Money);
            Assert.AreEqual(2, s.Inventory.Count(Inventory.Seed("boldo")));
            Assert.AreEqual(1, s.Inventory.Count(Inventory.Herb("mulungu")));
            Assert.IsTrue(s.Status.Has(Ailment.VontadeDeFumar));
            Assert.AreEqual(3, s.Requests.Count);
        }

        [Test]
        public void Primeira_crise_de_fissura_so_vem_depois_do_primeiro_plantio()
        {
            s.Tick(PlayerStatus.CriseFumoIntervalo + 1f);
            Assert.AreEqual(100f, s.Status.Energy, "antes de plantar o começo é calmo: energia cheia");
            Assert.IsFalse(msgs.Any(m => m.Contains("Respira fundo")), "nenhum aviso de crise antes de plantar");

            var plot = s.Plots[0];
            Assert.IsTrue(s.PreparePlot(plot));
            Assert.IsTrue(s.Status.FissuraEmEspera, "preparar a terra ainda não acorda a fissura");
            Assert.IsTrue(s.PlantSeed(plot, "boldo"));
            Assert.IsFalse(s.Status.FissuraEmEspera);

            s.Tick(PlayerStatus.CriseFumoIntervalo);
            Assert.AreEqual(100f - PlayerStatus.CriseFumoEnergia, s.Status.Energy, 0.001f, "25 s depois do plantio vem a primeira crise");
            Assert.IsTrue(msgs.Any(m => m.Contains("Respira fundo")), "a crise avisa");
        }

        [Test]
        public void Sem_plantar_a_fissura_acorda_no_dia_seguinte()
        {
            s.Tick(50f); // 06h + 12h = 18h, sem plantar nada
            Assert.IsFalse(msgs.Any(m => m.Contains("Respira fundo")), "o dia 1 inteiro sem crise");
            Assert.IsTrue(s.Sleep());
            Assert.IsFalse(s.Status.FissuraEmEspera, "o dia 2 acorda a fissura");
            s.Tick(PlayerStatus.CriseFumoIntervalo);
            Assert.AreEqual(100f - PlayerStatus.CriseFumoEnergia, s.Status.Energy, 0.001f, "25 s depois de acordar vem a crise");
        }

        [Test]
        public void Assombracao_Cuca_e_Bernuncia_avisam_a_energia_perdida()
        {
            var perdas = new List<float>();
            s.Status.EnergyDrained += perdas.Add;
            s.OnHaunted();
            s.OnCuca();
            s.OnBernunciaSwallow();
            CollectionAssert.AreEqual(new[] { 20f, 25f, 10f }, perdas, "cada uma pisca a barra com o quanto tirou");
        }

        [Test]
        public void Loop_da_horta_preparar_plantar_regar_colher_descobre_no_livro()
        {
            var plot = s.Plots[0];
            Assert.IsFalse(s.PlantSeed(plot, "boldo"), "precisa preparar");
            Assert.IsTrue(s.PreparePlot(plot));
            Assert.IsTrue(s.PlantSeed(plot, "boldo"));
            Assert.AreEqual(1, s.Inventory.Count(Inventory.Seed("boldo")));
            Assert.IsTrue(s.WaterPlot(plot));
            s.Tick(10f);
            Assert.IsTrue(plot.IsReady);

            rng.Ints(2);
            var r = s.HarvestPlot(plot);
            Assert.AreEqual(2, r.Ervas);
            Assert.AreEqual(2, s.Inventory.Count(Inventory.Herb("boldo")));
            Assert.IsTrue(s.Herbarium.IsDiscovered("boldo"));
            Assert.IsTrue(msgs.Any(m => m.Contains("Livro")));
        }

        [Test]
        public void Plantar_sem_semente_falha()
        {
            var plot = s.Plots[0];
            s.PreparePlot(plot);
            Assert.IsFalse(s.PlantSeed(plot, "guarana"));
        }

        [Test]
        public void Coleta_na_mata()
        {
            rng.Ints(2).Values(0.1f);
            Assert.AreEqual(2, s.CollectWild("guine"));
            Assert.AreEqual(1, s.Inventory.Count(Inventory.Seed("guine")));
            Assert.AreEqual(0, s.CollectWild("planta_inexistente"));
        }

        [Test]
        public void Cadeia_completa_secar_pilar_preparar_usar()
        {
            s.Inventory.Add(Inventory.Herb("mulungu"), 1); // total 2
            s.Inventory.Add(Inventory.Herb("camomila"), 1);
            Assert.IsTrue(s.HangToDry("mulungu"));
            Assert.IsTrue(s.HangToDry("mulungu"));
            s.Tick(DryingRack.DefaultDryingSeconds);
            Assert.AreEqual(2, s.CollectDried());
            Assert.IsTrue(s.Grind("mulungu"));
            Assert.IsTrue(s.Grind("mulungu"));
            Assert.AreEqual(CraftOutcome.Sucesso, s.Craft("elixir_recomeco"));
            var r = s.Use("elixir_recomeco");
            Assert.IsTrue(r.Consumed);
            Assert.AreEqual(1, s.Status.DosesRestantes(Ailment.VontadeDeFumar));
        }

        [Test]
        public void Vender_e_comprar()
        {
            s.Inventory.Add(Inventory.Herb("boldo"), 2);
            Assert.IsTrue(s.Sell(Inventory.Herb("boldo"), 2));
            Assert.AreEqual(GameSession.DinheiroInicial + 8, s.Money);
            Assert.IsFalse(s.Sell(Inventory.Herb("boldo")));
            Assert.IsTrue(s.BuySeed("guarana"));
            Assert.AreEqual(GameSession.DinheiroInicial + 8 - 3, s.Money);
        }

        [Test]
        public void Nao_compra_sem_dinheiro()
        {
            for (int i = 0; i < 100 && s.Money >= 3; i++) s.BuySeed("boldo");
            Assert.IsFalse(s.BuySeed("boldo"));
            Assert.IsTrue(s.Money < 3);
        }

        [Test]
        public void Tratar_aldeao_com_o_remedio_certo_paga_e_da_reputacao()
        {
            var req = new VillagerRequest { Nome = "Seu Zé", Mal = Ailment.DorDeBarriga, Recompensa = 20 };
            s.Inventory.Add(Inventory.Product("remedio_camomila"));
            Assert.IsFalse(s.TreatVillager(req, "remedio_camomila"), "remédio errado");
            s.Inventory.Add(Inventory.Product("remedio_boldo"));
            int antes = s.Money;
            Assert.IsTrue(s.TreatVillager(req, "remedio_boldo"));
            Assert.AreEqual(antes + 20, s.Money);
            Assert.AreEqual(1, s.Reputacao);
            Assert.IsTrue(req.Atendido);
            Assert.IsFalse(s.TreatVillager(req, "remedio_boldo"), "não atende duas vezes");
        }

        [Test]
        public void Dormir_so_a_noite_e_recupera_tudo()
        {
            Assert.IsFalse(s.Sleep());
            s.Status.Drain(80f);
            s.Tick(50f); // 06h + 12h = 18h
            Assert.IsTrue(s.Sleep());
            Assert.AreEqual(2, s.Clock.Day);
            Assert.AreEqual(100f, s.Status.Energy);
            Assert.AreEqual(100f, s.Status.Sanidade);
        }

        [Test]
        public void Insonia_impede_recuperar_ao_dormir()
        {
            s.Status.Add(Ailment.Insonia);
            s.Status.Drain(80f);
            s.Tick(50f);
            float antes = s.Status.Energy;
            s.Sleep();
            Assert.AreEqual(antes, s.Status.Energy, 0.001f);
        }

        [Test]
        public void Virar_a_noite_acordado_da_cansaco_e_seca_a_horta()
        {
            var plot = s.Plots[0];
            s.PreparePlot(plot); s.PlantSeed(plot, "boldo"); s.WaterPlot(plot);
            s.Tick(80f); // passa da meia-noite
            Assert.AreEqual(2, s.Clock.Day);
            Assert.IsTrue(s.Status.Has(Ailment.Cansaco));
        }

        [Test]
        public void Amanhecer_pode_trazer_mal_novo()
        {
            rng.Values(0.1f).Ints(0); // rola mal e escolhe o primeiro candidato
            s.Tick(50f);
            s.Sleep();
            Assert.IsTrue(s.Status.Has(Ailments.DailyPool[0]));
        }

        [Test]
        public void Saci_rouba_item_e_assombracao_drena()
        {
            int antes = s.Inventory.TotalItems;
            Assert.IsNotNull(s.OnSaciSteal());
            Assert.AreEqual(antes - 1, s.Inventory.TotalItems);

            rng.Values(0.1f);
            s.OnHaunted();
            Assert.AreEqual(80f, s.Status.Energy);
            Assert.AreEqual(55f, s.Status.Aura);
            Assert.IsTrue(s.Status.Assombrado);
        }

        [Test]
        public void Vitoria_exige_herbario_completo_e_nenhum_mal()
        {
            Assert.IsFalse(s.Won);
            foreach (var p in s.Db.Plants) s.Herbarium.Discover(p.id);
            Assert.IsFalse(s.Won, "ainda com fissura");
            s.Status.ClearAilments();
            Assert.IsTrue(s.Won);
            Assert.AreEqual(1f, s.Herbarium.Progress(s.Db));
        }

        [Test]
        public void DisplayName_amigavel()
        {
            Assert.AreEqual("Semente de boldo", s.DisplayName(Inventory.Seed("boldo")));
            Assert.AreEqual("guine (seca)", s.DisplayName(Inventory.Dried("guine")));
            Assert.AreEqual("Chá de boldo", s.DisplayName(Inventory.Product("remedio_boldo")));
        }
    }

    public class VillageAndDailyTests
    {
        [Test]
        public void Pedido_de_aldeao_sempre_tem_cura_possivel()
        {
            var db = TestData.Db();
            for (int i = 0; i < 20; i++)
            {
                var req = Village.NewRequest("x", db, new SystemRandom(i));
                Assert.IsNotNull(req);
                Assert.IsTrue(db.RecipesThatCure(req.Mal).Any(), req.Mal.ToString());
                Assert.GreaterOrEqual(req.Recompensa, Village.RecompensaBase);
            }
        }

        [Test]
        public void Mal_do_dia_respeita_a_chance_e_nao_repete()
        {
            var st = new PlayerStatus();
            Assert.AreEqual(Ailment.Nenhum, DailyEvents.RollMorningAilment(st, new FixedRandom().Values(0.9f)));
            foreach (var a in Ailments.DailyPool) st.Add(a);
            Assert.AreEqual(Ailment.Nenhum, DailyEvents.RollMorningAilment(st, new FixedRandom().Values(0.0f)));
        }

        [Test]
        public void Herbario_conta_descobertas_unicas()
        {
            var h = new Herbarium();
            Assert.IsTrue(h.Discover("boldo"));
            Assert.IsFalse(h.Discover("boldo"));
            Assert.IsFalse(h.Discover(""));
            Assert.AreEqual(1, h.Count);
        }
    }
}
