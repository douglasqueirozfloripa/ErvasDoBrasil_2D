using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    public class DayCycleTests
    {
        [Test]
        public void Comeca_as_6h_do_dia_1()
        {
            var c = new DayCycle(240f);
            Assert.AreEqual(1, c.Day);
            Assert.AreEqual("06:00", c.Clock);
            Assert.IsFalse(c.IsNight);
        }

        [Test]
        public void Anoitece_as_18h()
        {
            var c = new DayCycle(240f);
            c.Advance(240f * 0.5f); // 06h + 12h
            Assert.AreEqual("18:00", c.Clock);
            Assert.IsTrue(c.IsNight);
            Assert.IsTrue(c.CanSleep);
        }

        [Test]
        public void Meia_noite_vira_o_dia()
        {
            var c = new DayCycle(100f);
            Assert.AreEqual(1, c.Advance(80f));
            Assert.AreEqual(2, c.Day);
            Assert.AreEqual(2, c.Advance(200f));
            Assert.AreEqual(4, c.Day);
        }

        [Test]
        public void Dormir_a_noite_leva_para_6h_do_dia_seguinte()
        {
            var c = new DayCycle(100f, 1, 0.8f);
            Assert.IsTrue(c.SleepUntilMorning());
            Assert.AreEqual(2, c.Day);
            Assert.AreEqual(DayCycle.Amanhecer, c.Time01, 0.0001f);
        }

        [Test]
        public void Dormir_de_madrugada_nao_pula_dia_e_de_dia_nao_pode()
        {
            var c = new DayCycle(100f, 3, 0.1f);
            Assert.IsTrue(c.SleepUntilMorning());
            Assert.AreEqual(3, c.Day);
            Assert.IsFalse(c.SleepUntilMorning(), "06h é dia");
        }

        [Test]
        public void Escuridao_vai_de_0_a_1()
        {
            Assert.AreEqual(0f, new DayCycle(100f, 1, 0.5f).Darkness);
            Assert.AreEqual(1f, new DayCycle(100f, 1, 0.9f).Darkness);
            float crep = new DayCycle(100f, 1, 0.75f).Darkness;
            Assert.Greater(crep, 0f);
            Assert.Less(crep, 1f);
        }
    }

    public class FarmPlotTests
    {
        PlantData boldo;
        FarmPlotModel plot;

        [SetUp]
        public void SetUp()
        {
            boldo = TestData.Plants().First(p => p.id == "boldo");
            plot = new FarmPlotModel();
        }

        [Test]
        public void Fluxo_completo_bruto_preparado_plantado_pronto()
        {
            Assert.AreEqual(PlotState.Bruto, plot.State);
            Assert.IsFalse(plot.Plant("boldo"), "não planta em terra bruta");
            Assert.IsTrue(plot.Prepare());
            Assert.IsTrue(plot.Plant("boldo"));
            Assert.AreEqual(PlotState.Plantado, plot.State);
            Assert.IsTrue(plot.Water());
            plot.Tick(boldo.tempoCrescimento, boldo);
            Assert.AreEqual(PlotState.Pronto, plot.State);
        }

        [Test]
        public void Sem_agua_nao_cresce()
        {
            plot.Prepare(); plot.Plant("boldo");
            plot.Tick(100f, boldo);
            Assert.AreEqual(0f, plot.Growth);
            Assert.IsTrue(plot.NeedsWater);
        }

        [Test]
        public void Novo_dia_seca_a_terra()
        {
            plot.Prepare(); plot.Plant("boldo"); plot.Water();
            plot.Tick(boldo.tempoCrescimento / 2f, boldo);
            plot.OnNewDay();
            Assert.IsTrue(plot.NeedsWater);
            plot.Tick(100f, boldo);
            Assert.AreEqual(0.5f, plot.Growth, 0.001f);
        }

        [Test]
        public void Colheita_normal_e_com_mau_olhado()
        {
            var st = new PlayerStatus();
            Pronto();
            var r = plot.Harvest(st, new FixedRandom().Ints(3).Values(0.1f));
            Assert.AreEqual(3, r.Ervas);
            Assert.AreEqual(1, r.Sementes);
            Assert.AreEqual(PlotState.Preparado, plot.State, "fica preparado para replantar");

            st.Add(Ailment.MauOlhado);
            Pronto();
            r = plot.Harvest(st, new FixedRandom().Ints(3));
            Assert.AreEqual(1, r.Ervas);
            Assert.AreEqual(0, r.Sementes);
        }

        [Test]
        public void Sorte_da_mais_uma_erva()
        {
            var st = new PlayerStatus { SorteDias = 1 };
            Pronto();
            Assert.AreEqual(3, plot.Harvest(st, new FixedRandom().Ints(2)).Ervas);
        }

        [Test]
        public void Colher_antes_da_hora_retorna_null()
        {
            Assert.IsNull(plot.Harvest(new PlayerStatus(), new FixedRandom()));
        }

        [Test]
        public void Aura_so_quando_planta_de_protecao_esta_madura()
        {
            var espada = TestData.Plants().First(p => p.id == "espada_sao_jorge");
            plot.Prepare(); plot.Plant(espada.id); plot.Water();
            Assert.IsFalse(plot.AuraActive(espada));
            plot.Tick(espada.tempoCrescimento, espada);
            Assert.IsTrue(plot.AuraActive(espada));
            Assert.IsFalse(plot.AuraActive(boldo));
        }

        void Pronto()
        {
            if (plot.State == PlotState.Bruto) plot.Prepare();
            plot.Plant("boldo"); plot.Water(); plot.Tick(1000f, boldo);
        }
    }

    public class ProcessingTests
    {
        [Test]
        public void Varal_seca_erva_depois_do_tempo()
        {
            var inv = new Inventory();
            inv.Add(Inventory.Herb("guine"), 2);
            var rack = new DryingRack(capacity: 2, dryingSeconds: 10f);
            Assert.IsTrue(rack.Hang("guine", inv));
            Assert.IsTrue(rack.Hang("guine", inv));
            Assert.IsTrue(rack.IsFull);
            rack.Tick(5f);
            Assert.AreEqual(0, rack.CollectDry(inv));
            rack.Tick(5f);
            Assert.AreEqual(2, rack.CollectDry(inv));
            Assert.AreEqual(2, inv.Count(Inventory.Dried("guine")));
            Assert.AreEqual(0, rack.Slots.Count);
        }

        [Test]
        public void Varal_cheio_ou_sem_erva_recusa()
        {
            var inv = new Inventory();
            var rack = new DryingRack(1, 10f);
            Assert.IsFalse(rack.Hang("boldo", inv));
            inv.Add(Inventory.Herb("boldo"), 2);
            rack.Hang("boldo", inv);
            Assert.IsFalse(rack.Hang("boldo", inv));
            Assert.AreEqual(1, inv.Count(Inventory.Herb("boldo")));
        }

        [Test]
        public void Pilao_transforma_seca_em_po_e_gasta_energia()
        {
            var inv = new Inventory();
            var st = new PlayerStatus();
            Assert.IsFalse(Mortar.Grind("mulungu", inv, st));
            inv.Add(Inventory.Dried("mulungu"));
            Assert.IsTrue(Mortar.Grind("mulungu", inv, st));
            Assert.AreEqual(1, inv.Count(Inventory.Powder("mulungu")));
            Assert.AreEqual(100f - Mortar.CustoEnergia, st.Energy);
        }
    }
}
