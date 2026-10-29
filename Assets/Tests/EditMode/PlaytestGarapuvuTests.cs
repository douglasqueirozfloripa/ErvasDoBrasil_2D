using System.Collections.Generic;
using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    /// <summary>Correções e melhorias do playtest do encontro Garapuvu de 28/10: varal, feira, XP, noites seguras, Cuca e Bernúncia.</summary>
    public class PlaytestGarapuvuTests
    {
        GameSession s;
        List<string> msgs;

        [SetUp]
        public void SetUp()
        {
            s = new GameSession(TestData.Db(), new FixedRandom(), 100f, 4);
            msgs = new List<string>();
            s.Message += msgs.Add;
            s.StartNewGame();
        }

        // ───────── Varal: tirar sem perder ─────────

        [Test]
        public void Tirar_do_varal_antes_de_secar_devolve_a_erva_fresca()
        {
            s.Inventory.Add(Inventory.Herb("boldo"));
            Assert.IsTrue(s.HangToDry("boldo"));
            Assert.AreEqual(0, s.Inventory.Count(Inventory.Herb("boldo")));
            s.Rack.Tick(DryingRack.DefaultDryingSeconds / 2f);

            Assert.AreEqual(Inventory.Herb("boldo"), s.TakeFromRack(0));
            Assert.AreEqual(1, s.Inventory.Count(Inventory.Herb("boldo")), "nada se perde");
            Assert.AreEqual(0, s.Rack.Slots.Count);
        }

        [Test]
        public void Tirar_do_varal_depois_de_secar_devolve_seca_e_indice_invalido_nao_faz_nada()
        {
            s.Inventory.Add(Inventory.Herb("boldo"), 2);
            s.HangToDry("boldo");
            s.HangToDry("boldo");
            s.Rack.Tick(DryingRack.DefaultDryingSeconds);

            Assert.IsNull(s.TakeFromRack(5));
            Assert.IsNull(s.TakeFromRack(-1));
            Assert.AreEqual(Inventory.Dried("boldo"), s.TakeFromRack(1));
            Assert.AreEqual(1, s.Inventory.Count(Inventory.Dried("boldo")));
            Assert.AreEqual(1, s.Rack.Slots.Count, "a outra continua pendurada");
        }

        // ───────── Feira: vender tudo, comprar de 5 em 5 ─────────

        [Test]
        public void Vender_tudo_vende_a_bolsa_inteira_pelo_valor_anunciado()
        {
            int valor = s.SellAllValue;
            int antes = s.Money;
            Assert.Greater(valor, 0);

            Assert.AreEqual(valor, s.SellAll());
            Assert.AreEqual(antes + valor, s.Money);
            Assert.AreEqual(0, s.Inventory.TotalItems);
            Assert.AreEqual(0, s.SellAll(), "bolsa vazia não vende nada");
        }

        [Test]
        public void Comprar_cinco_sementes_para_quando_o_dinheiro_acaba()
        {
            var boldo = s.Db.Plant("boldo");
            int preco = Economy.SeedPrice(boldo);
            int antes = s.Inventory.Count(Inventory.Seed("boldo"));

            Assert.AreEqual(5, s.BuySeeds("boldo", 5));
            Assert.AreEqual(antes + 5, s.Inventory.Count(Inventory.Seed("boldo")));

            int sobra = s.Money;
            Assert.AreEqual(sobra / preco, s.BuySeeds("boldo", 1000), "compra só o que o dinheiro paga");
            Assert.AreEqual(sobra % preco, s.Money);
            Assert.AreEqual(0, s.BuySeeds("boldo", 5));
        }

        // ───────── Barra de XP ─────────

        [Test]
        public void Fracao_do_nivel_vai_de_0_a_1_dentro_de_cada_nivel()
        {
            var p = new Progression();
            Assert.AreEqual(0f, p.FracaoDoNivel);
            p.Add(Progression.XpPorNivel[1] / 2);
            Assert.AreEqual(0.5f, p.FracaoDoNivel, 0.001f);
            p.Add(Progression.XpPorNivel[1]); // passa para o nível 2
            Assert.AreEqual(2, p.Nivel);
            Assert.That(p.FracaoDoNivel, Is.InRange(0f, 1f));
            p.Add(10000);
            Assert.IsTrue(p.NoNivelMaximo);
            Assert.AreEqual(1f, p.FracaoDoNivel);
        }

        // ───────── Noites seguras e lendas ─────────

        [Test]
        public void Lendas_so_aparecem_a_partir_do_dia_5_Cuca_de_madrugada_e_Bernuncia_no_resto_da_noite()
        {
            // antes do dia das lendas, o primeiro sorteio já é o de sempre (0,1 → Alma da Lamparina)
            Assert.AreEqual(SpiritType.Assombracao, ProtectionRules.PickSpawn(new FixedRandom().Values(0.1f), ProtectionRules.DiaDasLendas - 1, true));
            Assert.AreEqual(SpiritType.Cuca, ProtectionRules.PickSpawn(new FixedRandom().Values(0.1f), ProtectionRules.DiaDasLendas, true));
            Assert.AreEqual(SpiritType.Bernuncia, ProtectionRules.PickSpawn(new FixedRandom().Values(0.1f), ProtectionRules.DiaDasLendas, false));
            // sem a chance da lenda, sorteia entre os quatro de sempre com o próximo valor
            Assert.AreEqual(SpiritType.Saci, ProtectionRules.PickSpawn(new FixedRandom().Values(0.9f, 0.6f), 10, true));
        }

        [TestCase(22f, true)]
        [TestCase(3f, true)]
        [TestCase(21.5f, false)]
        [TestCase(5f, false)]
        public void Madrugada_vai_das_22h_as_4h(float hora, bool esperado)
        {
            Assert.AreEqual(esperado, ProtectionRules.Madrugada(hora / 24f));
        }

        [Test]
        public void Amuleto_espanta_a_Cuca_mas_nao_a_Bernuncia_e_a_barreira_segura_as_duas()
        {
            Assert.IsTrue(ProtectionRules.AmuletoRepele(SpiritType.Cuca));
            Assert.IsFalse(ProtectionRules.AmuletoRepele(SpiritType.Bernuncia));
            Assert.IsTrue(ProtectionRules.AuraBlocks(AuraType.Barreira, SpiritType.Cuca));
            Assert.IsTrue(ProtectionRules.AuraBlocks(AuraType.Barreira, SpiritType.Bernuncia));
            Assert.IsFalse(ProtectionRules.AuraBlocks(AuraType.AntiSaci, SpiritType.Bernuncia));
        }

        [Test]
        public void Cuca_traz_o_sono_atrasado_de_uma_vez()
        {
            float energia = s.Status.Energy, sanidade = s.Status.Sanidade;
            s.OnCuca();
            Assert.AreEqual(energia - 25f, s.Status.Energy, 0.01f);
            Assert.AreEqual(sanidade - 10f, s.Status.Sanidade, 0.01f);
            Assert.IsTrue(s.Status.Has(Ailment.Cansaco));
        }

        [Test]
        public void Bernuncia_engole_o_canteiro_e_a_terra_continua_preparada()
        {
            var plot = s.Plots[0];
            Assert.IsNull(s.OnBernunciaDevour(plot), "canteiro vazio: nada para engolir");
            s.PreparePlot(plot);
            s.PlantSeed(plot, "boldo");
            s.WaterPlot(plot);

            Assert.AreEqual("boldo", s.OnBernunciaDevour(plot));
            Assert.IsTrue(plot.IsEmpty);
            Assert.AreEqual(PlotState.Preparado, plot.State);
            Assert.IsFalse(plot.Watered);
            Assert.That(msgs, Has.Some.Contains("Bernúncia"));
        }

        [Test]
        public void Bernuncia_engole_a_jogadora_e_gasta_energia()
        {
            float energia = s.Status.Energy;
            s.OnBernunciaSwallow();
            Assert.AreEqual(energia - 10f, s.Status.Energy, 0.01f);
        }
    }
}
