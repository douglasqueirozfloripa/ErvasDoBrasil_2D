using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    public class CraftingTests
    {
        GameDatabase db;
        Inventory inv;
        PlayerStatus st;

        [SetUp]
        public void SetUp()
        {
            db = TestData.Db();
            inv = new Inventory();
            st = new PlayerStatus();
        }

        [Test]
        public void Elixir_exige_a_forma_certa_dos_ingredientes()
        {
            var r = db.Recipe("elixir_recomeco");
            inv.Add(Inventory.Herb("mulungu"), 2); // fresca, mas pede pó
            inv.Add(Inventory.Herb("camomila"));
            Assert.AreEqual(CraftOutcome.FaltamIngredientes, Crafting.Craft(r, inv, st, new FixedRandom()));
            inv.Add(Inventory.Powder("mulungu"), 2);
            Assert.AreEqual(CraftOutcome.Sucesso, Crafting.Craft(r, inv, st, new FixedRandom()));
            Assert.AreEqual(1, inv.Count(Inventory.Product("elixir_recomeco")));
            Assert.AreEqual(0, inv.Count(Inventory.Powder("mulungu")));
            Assert.AreEqual(2, inv.Count(Inventory.Herb("mulungu")), "erva fresca não é consumida");
        }

        [Test]
        public void Pode_queimar_a_mao_e_entao_bloqueia_o_caldeirao()
        {
            var r = db.Recipe("elixir_recomeco");
            inv.Add(Inventory.Powder("mulungu"), 4);
            inv.Add(Inventory.Herb("camomila"), 2);
            Assert.AreEqual(CraftOutcome.SucessoComQueimadura, Crafting.Craft(r, inv, st, new FixedRandom().Values(0.1f)));
            Assert.IsTrue(st.Has(Ailment.Queimadura));
            Assert.AreEqual(CraftOutcome.MaosQueimadas, Crafting.Craft(r, inv, st, new FixedRandom()));
        }

        [Test]
        public void Sem_energia_nao_consome_ingredientes()
        {
            var r = db.Recipe("remedio_boldo");
            inv.Add(Inventory.Herb("boldo"));
            st.Drain(100f);
            Assert.AreEqual(CraftOutcome.SemEnergia, Crafting.Craft(r, inv, st, new FixedRandom()));
            Assert.AreEqual(1, inv.Count(Inventory.Herb("boldo")));
        }

        [Test]
        public void Receita_nula_e_desconhecida()
        {
            Assert.AreEqual(CraftOutcome.ReceitaDesconhecida, Crafting.Craft(null, inv, st, new FixedRandom()));
        }
    }

    public class ItemEffectsTests
    {
        GameDatabase db;
        Inventory inv;
        PlayerStatus st;

        [SetUp]
        public void SetUp()
        {
            db = TestData.Db();
            inv = new Inventory();
            st = new PlayerStatus();
        }

        UseResult Usar(string recipeId)
        {
            inv.Add(Inventory.Product(recipeId));
            return ItemEffects.Use(db.Recipe(recipeId), inv, st);
        }

        [Test]
        public void Sem_o_produto_nada_acontece()
        {
            Assert.IsFalse(ItemEffects.Use(db.Recipe("remedio_boldo"), inv, st).Consumed);
        }

        [Test]
        public void Cha_de_boldo_cura_dois_males_e_devolve_30pct_de_energia()
        {
            st.Add(Ailment.DorDeBarriga);
            st.Add(Ailment.Enjoo);
            st.Drain(50f);
            var r = Usar("remedio_boldo");
            Assert.IsTrue(r.Consumed);
            Assert.AreEqual(2, r.Curados.Count);
            Assert.AreEqual(80f, st.Energy, 0.001f);
            Assert.AreEqual(0, inv.TotalItems);
        }

        [Test]
        public void Elixir_do_recomeco_vale_duas_doses_de_mulungu()
        {
            st.Add(Ailment.VontadeDeFumar);
            var r = Usar("elixir_recomeco");
            CollectionAssert.Contains(r.Melhorados, Ailment.VontadeDeFumar);
            Assert.AreEqual(1, st.DosesRestantes(Ailment.VontadeDeFumar));
        }

        [Test]
        public void Camomila_restaura_sanidade()
        {
            st.LoseSanity(50f);
            Usar("remedio_camomila");
            Assert.AreEqual(85f, st.Sanidade, 0.001f);
        }

        [Test]
        public void Arruda_limpa_uruca_sobe_aura_e_revela()
        {
            st.HitAura(50f);
            st.Add(Ailment.MauOlhado);
            Usar("remedio_arruda");
            Assert.IsFalse(st.Has(Ailment.MauOlhado));
            Assert.AreEqual(60f, st.Aura, 0.001f);
            Assert.IsTrue(st.VeEspiritosOcultos);
            Assert.AreEqual(60f, st.RevelarSegundos, 0.001f);
        }

        [Test]
        public void Guarana_da_mais_50pct_de_velocidade()
        {
            Usar("remedio_guarana");
            Assert.AreEqual(1.5f, st.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(90f, st.BuffVelocidadeSegundos, 0.001f);
        }

        [Test]
        public void Banho_limpa_o_espiritual_enche_a_aura_e_da_sorte()
        {
            st.Add(Ailment.MauOlhado);
            st.Add(Ailment.Assombrado);
            st.HitAura(60f);
            var r = Usar("banho_descarrego");
            Assert.AreEqual(2, r.Curados.Count);
            Assert.AreEqual(100f, st.Aura);
            Assert.AreEqual(1, st.SorteDias);
        }

        [Test]
        public void Amuleto_dura_os_dias_da_receita()
        {
            Usar("amuleto_arruda");
            Assert.AreEqual(2, st.AmuletoDias);
            Assert.IsTrue(st.ProtegidoPorAmuleto);
        }

        [Test]
        public void Defumacao_bane_no_raio_e_tira_assombracao()
        {
            st.Add(Ailment.Assombrado);
            var r = Usar("remedio_guine");
            Assert.IsTrue(r.Banish);
            Assert.AreEqual(5f, r.BanishRadius);
            Assert.IsFalse(st.Assombrado);
        }
    }

    public class EconomyTests
    {
        [Test]
        public void Precos_por_forma_da_erva()
        {
            var db = TestData.Db(); // boldo: venda 4, semente 3
            var st = new PlayerStatus();
            Assert.AreEqual(4, Economy.SellPrice(Inventory.Herb("boldo"), db, st));
            Assert.AreEqual(6, Economy.SellPrice(Inventory.Dried("boldo"), db, st));
            Assert.AreEqual(8, Economy.SellPrice(Inventory.Powder("boldo"), db, st));
            Assert.AreEqual(1, Economy.SellPrice(Inventory.Seed("boldo"), db, st));
            Assert.AreEqual(10, Economy.SellPrice(Inventory.Product("remedio_boldo"), db, st));
            Assert.AreEqual(0, Economy.SellPrice(Inventory.Herb("inexistente"), db, st));
        }

        [Test]
        public void Dor_de_dente_baixa_preco_mas_nunca_abaixo_de_1()
        {
            var db = TestData.Db();
            var st = new PlayerStatus();
            st.Add(Ailment.DorDeDente);
            Assert.AreEqual(28, Economy.SellPrice(Inventory.Product("elixir_recomeco"), db, st));
            Assert.AreEqual(1, Economy.SellPrice(Inventory.Seed("boldo"), db, st));
        }
    }

    public class ProtectionRulesTests
    {
        [TestCase(AuraType.Barreira, SpiritType.Assombracao, true)]
        [TestCase(AuraType.Barreira, SpiritType.Saci, true)]
        [TestCase(AuraType.AntiOlhoGordo, SpiritType.OlhoGordo, true)]
        [TestCase(AuraType.AntiOlhoGordo, SpiritType.Saci, false)]
        [TestCase(AuraType.AntiSaci, SpiritType.Saci, true)]
        [TestCase(AuraType.AntiSaci, SpiritType.Visagem, false)]
        [TestCase(AuraType.Nenhuma, SpiritType.Assombracao, false)]
        public void Quem_afasta_quem(AuraType aura, SpiritType spirit, bool bloqueia)
        {
            Assert.AreEqual(bloqueia, ProtectionRules.AuraBlocks(aura, spirit));
        }

        [Test]
        public void Saci_nao_respeita_amuleto()
        {
            Assert.IsFalse(ProtectionRules.AmuletoRepele(SpiritType.Saci));
            Assert.IsTrue(ProtectionRules.AmuletoRepele(SpiritType.Assombracao));
        }

        [Test]
        public void Visagem_so_aparece_com_arruda()
        {
            var st = new PlayerStatus();
            Assert.IsFalse(ProtectionRules.IsVisible(SpiritType.Visagem, st));
            Assert.IsTrue(ProtectionRules.IsVisible(SpiritType.Saci, st));
            st.RevelarSegundos = 10f;
            Assert.IsTrue(ProtectionRules.IsVisible(SpiritType.Visagem, st));
        }

        [Test]
        public void Noites_seguras_e_depois_mais_espiritos_aos_poucos_com_teto()
        {
            Assert.AreEqual(0, ProtectionRules.MaxSpirits(1), "a casa ainda está benzida pela vó");
            Assert.AreEqual(0, ProtectionRules.MaxSpirits(ProtectionRules.NoitesSeguras));
            Assert.AreEqual(1, ProtectionRules.MaxSpirits(ProtectionRules.NoitesSeguras + 1));
            Assert.AreEqual(1, ProtectionRules.MaxSpirits(4));
            Assert.AreEqual(2, ProtectionRules.MaxSpirits(5));
            Assert.AreEqual(8, ProtectionRules.MaxSpirits(100));
        }

        [Test]
        public void Toda_entidade_tem_ficha_completa_no_Livro()
        {
            foreach (SpiritType t in System.Enum.GetValues(typeof(SpiritType)))
            {
                var l = SpiritCodex.Get(t);
                Assert.IsNotNull(l, t.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(l.Origem), t + " origem");
                Assert.IsFalse(string.IsNullOrEmpty(l.Significado), t + " significado");
                Assert.IsFalse(string.IsNullOrEmpty(l.NoJogo), t + " no jogo");
                Assert.IsFalse(string.IsNullOrEmpty(l.Protecao), t + " proteção");
                Assert.Greater(l.PlantasQueProtegem.Length, 0);
            }
        }

        [TestCase(0.0f, SpiritType.Assombracao)]
        [TestCase(0.4f, SpiritType.Visagem)]
        [TestCase(0.6f, SpiritType.Saci)]
        [TestCase(0.9f, SpiritType.OlhoGordo)]
        public void Sorteio_de_espiritos(float roll, SpiritType esperado)
        {
            Assert.AreEqual(esperado, ProtectionRules.PickSpawn(new FixedRandom().Values(roll)));
        }
    }

    public class MusicRulesTests
    {
        [TestCase(false, false, 20f, MusicTrack.Dia)]
        [TestCase(false, false, 3f, MusicTrack.Feira)]
        [TestCase(true, false, 3f, MusicTrack.Noite)]
        [TestCase(false, true, 20f, MusicTrack.Noite)]
        public void Escolhe_a_faixa_pelo_ambiente(bool noite, bool assombrado, float distVila, MusicTrack esperado)
        {
            Assert.AreEqual(esperado, MusicRules.Choose(noite, assombrado, distVila));
        }

        [Test]
        public void Cada_faixa_tem_um_arquivo()
        {
            Assert.AreEqual("trilha_dia", MusicRules.ResourceName(MusicTrack.Dia));
            Assert.AreEqual("trilha_feira", MusicRules.ResourceName(MusicTrack.Feira));
            Assert.AreEqual("trilha_noite", MusicRules.ResourceName(MusicTrack.Noite));
        }
    }
}
