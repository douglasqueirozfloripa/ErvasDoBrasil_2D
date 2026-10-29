using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;

namespace ErvasDoBrasil.Tests
{
    /// <summary>A pá: comprar na feira, cavar canteiros fora da horta (só para plantas de proteção), desfazer e salvar.</summary>
    public class ShovelTests
    {
        GameSession s;
        List<string> msgs;
        List<FarmPlotModel> added;

        [SetUp]
        public void SetUp()
        {
            s = new GameSession(TestData.Db(), new FixedRandom(), 100f, 4);
            msgs = new List<string>();
            added = new List<FarmPlotModel>();
            s.Message += msgs.Add;
            s.PlotAdded += added.Add;
            s.StartNewGame();
        }

        void ComPa() { Assert.IsTrue(s.BuyShovel(), "o dinheiro inicial paga a pá"); }

        [Test]
        public void Horta_da_vo_fica_na_grade_linha_a_linha()
        {
            Assert.AreEqual(FarmLayout.OrigemX, s.Plots[0].X, 0.0001f);
            Assert.AreEqual(FarmLayout.OrigemY, s.Plots[0].Y, 0.0001f);
            Assert.AreEqual(FarmLayout.OrigemX + 3 * FarmLayout.Largura, s.Plots[3].X, 0.0001f);
            Assert.AreEqual(FarmLayout.OrigemY - FarmLayout.Altura, FarmLayout.HortaY(4), 0.0001f, "o quinto canteiro abre a segunda linha");
            Assert.IsFalse(s.Plots.Any(p => p.Cavado));
        }

        [Test]
        public void Snap_leva_o_ponto_para_a_casa_mais_perto()
        {
            Assert.AreEqual(FarmLayout.OrigemX + 2 * FarmLayout.Largura, FarmLayout.SnapX(FarmLayout.OrigemX + 2 * FarmLayout.Largura + 0.7f), 0.0001f);
            Assert.AreEqual(FarmLayout.OrigemY - FarmLayout.Altura, FarmLayout.SnapY(FarmLayout.OrigemY - FarmLayout.Altura - 0.6f), 0.0001f);
        }

        [Test]
        public void Comprar_pa_desconta_o_preco_e_so_compra_uma()
        {
            Assert.IsTrue(s.BuyShovel());
            Assert.IsTrue(s.TemPa);
            Assert.AreEqual(GameSession.DinheiroInicial - GameSession.PrecoPa, s.Money);
            Assert.IsFalse(s.BuyShovel(), "já tem");
            Assert.AreEqual(GameSession.DinheiroInicial - GameSession.PrecoPa, s.Money);
        }

        [Test]
        public void Sem_dinheiro_nao_compra_a_pa()
        {
            while (s.Money >= GameSession.PrecoPa) Assert.IsTrue(s.BuySeed("boldo"));
            Assert.IsFalse(s.BuyShovel());
            Assert.IsFalse(s.TemPa);
            Assert.IsTrue(msgs.Any(m => m.Contains("Dinheiro")));
        }

        [Test]
        public void Sem_pa_nao_cava()
        {
            Assert.IsNull(s.DigPlot(9f, 2f));
            Assert.AreEqual(4, s.Plots.Count);
            Assert.IsTrue(msgs.Any(m => m.Contains("pá")));
        }

        [Test]
        public void Cavar_cria_canteiro_preparado_na_grade_e_avisa_a_unity()
        {
            ComPa();
            float antes = s.Status.Energy;
            var plot = s.DigPlot(9.1f, 2.3f);

            Assert.IsNotNull(plot);
            Assert.IsTrue(plot.Cavado);
            Assert.AreEqual(PlotState.Preparado, plot.State, "a pá já prepara a terra");
            Assert.AreEqual(FarmLayout.SnapX(9.1f), plot.X, 0.0001f);
            Assert.AreEqual(FarmLayout.SnapY(2.3f), plot.Y, 0.0001f);
            Assert.AreEqual(5, s.Plots.Count);
            CollectionAssert.AreEqual(new[] { plot }, added);
            Assert.AreEqual(antes - FarmPlotModel.CustoCavar * s.Status.ActionCostMultiplier, s.Status.Energy, 0.001f);
        }

        [Test]
        public void Nao_cava_onde_ja_tem_canteiro()
        {
            ComPa();
            Assert.IsNull(s.DigPlot(FarmLayout.OrigemX + 0.3f, FarmLayout.OrigemY - 0.2f), "casa do primeiro canteiro da horta");
            Assert.IsNotNull(s.DigPlot(9f, 2f));
            Assert.IsNull(s.DigPlot(9.2f, 2.1f), "mesma casa do que acabou de cavar");
            Assert.AreEqual(5, s.Plots.Count);
        }

        [Test]
        public void Sem_energia_nao_cava()
        {
            ComPa();
            s.Status.Drain(1000f);
            Assert.IsNull(s.DigPlot(9f, 2f));
            Assert.AreEqual(4, s.Plots.Count);
        }

        [Test]
        public void Tem_limite_de_canteiros_cavados()
        {
            ComPa();
            for (int i = 0; i < GameSession.MaxCanteirosCavados; i++)
            {
                s.Status.RestoreFull();
                Assert.IsNotNull(s.DigPlot(-15f + i * FarmLayout.Largura, 8f), "canteiro " + i);
            }
            s.Status.RestoreFull();
            Assert.IsNull(s.DigPlot(-15f, 5f));
            Assert.AreEqual(GameSession.MaxCanteirosCavados, s.CanteirosCavados);
        }

        [Test]
        public void Espada_de_sao_jorge_cresce_no_canteiro_cavado_e_protege()
        {
            ComPa();
            var plot = s.DigPlot(9f, 2f);
            Assert.IsTrue(s.PlantSeed(plot, "espada_sao_jorge"));
            Assert.IsTrue(s.WaterPlot(plot));
            s.Tick(10f);
            Assert.IsTrue(plot.IsReady);
            Assert.IsTrue(plot.AuraActive(s.Db.Plant("espada_sao_jorge")));
        }

        [Test]
        public void Canteiro_cavado_so_aceita_plantas_de_protecao()
        {
            ComPa();
            var plot = s.DigPlot(9f, 2f);
            float energia = s.Status.Energy;
            Assert.IsFalse(s.PlantSeed(plot, "boldo"), "boldo não faz efeito na área");
            Assert.IsTrue(plot.IsEmpty, "o canteiro continua vazio");
            Assert.AreEqual(2, s.Inventory.Count(Inventory.Seed("boldo")), "a semente não é gasta");
            Assert.AreEqual(energia, s.Status.Energy, "nem a energia");
            StringAssert.Contains("plantas de proteção", msgs.Last());
            StringAssert.Contains("horta da vó", msgs.Last(), "o aviso diz onde a planta vai");
            Assert.IsTrue(s.PlantSeed(plot, "espada_sao_jorge"), "a Espada-de-São-Jorge pode");
        }

        [Test]
        public void Horta_da_vo_aceita_qualquer_planta()
        {
            s.PreparePlot(s.Plots[0]);
            Assert.IsTrue(s.PlantSeed(s.Plots[0], "boldo"));
            s.PreparePlot(s.Plots[1]);
            Assert.IsTrue(s.PlantSeed(s.Plots[1], "espada_sao_jorge"));
        }

        [Test]
        public void Planta_de_protecao_e_a_que_faz_efeito_na_area()
        {
            Assert.AreEqual("espada_sao_jorge, guine", s.PlantasDeProtecao);
            var cavado = new FarmPlotModel(0f, 0f, true);
            Assert.IsTrue(cavado.Aceita(s.Db.Plant("guine")));
            Assert.IsFalse(cavado.Aceita(s.Db.Plant("arruda")), "a arruda protege quem carrega, mas não faz efeito na área");
            Assert.IsFalse(cavado.Aceita(null));
            Assert.IsTrue(new FarmPlotModel().Aceita(s.Db.Plant("arruda")), "na horta da vó vale tudo");
        }

        [Test]
        public void Desfazer_so_vale_para_canteiro_cavado_e_vazio()
        {
            ComPa();
            var removed = new List<FarmPlotModel>();
            s.PlotRemoved += removed.Add;
            var plot = s.DigPlot(9f, 2f);

            Assert.IsFalse(s.FillPlot(s.Plots[0]), "a horta da vó fica");
            Assert.IsTrue(s.PlantSeed(plot, "espada_sao_jorge"));
            Assert.IsFalse(s.FillPlot(plot), "tem planta");

            var vazio = s.DigPlot(12f, 2f);
            Assert.IsTrue(s.FillPlot(vazio));
            Assert.IsFalse(s.Plots.Contains(vazio));
            CollectionAssert.AreEqual(new[] { vazio }, removed);
            Assert.IsNotNull(s.DigPlot(12f, 2f), "a casa ficou livre de novo");
        }

        [Test]
        public void Salvar_e_carregar_devolve_a_pa_e_os_canteiros_cavados()
        {
            ComPa();
            s.PreparePlot(s.Plots[1]);
            var plot = s.DigPlot(9f, 2f);
            s.PlantSeed(plot, "espada_sao_jorge");
            s.WaterPlot(plot);
            s.Tick(3f);
            s.DigPlot(-10f, 6f);

            var json = JsonUtility.ToJson(SaveGame.Capture(s));
            var loaded = new GameSession(TestData.Db(), new FixedRandom(), 100f, 4);
            var spawned = new List<FarmPlotModel>();
            loaded.PlotAdded += spawned.Add;
            SaveGame.Restore(loaded, JsonUtility.FromJson<SaveData>(json));

            Assert.IsTrue(loaded.TemPa);
            Assert.AreEqual(6, loaded.Plots.Count);
            Assert.AreEqual(2, spawned.Count, "a Unity desenha os canteiros cavados que voltaram do save");
            Assert.AreEqual(PlotState.Preparado, loaded.Plots[1].State, "a horta da vó continua na ordem");
            var volta = loaded.PlotAt(9f, 2f);
            Assert.IsNotNull(volta);
            Assert.IsTrue(volta.Cavado);
            Assert.AreEqual("espada_sao_jorge", volta.PlantId);
            Assert.AreEqual(plot.Growth, volta.Growth, 0.0001f);
            Assert.IsTrue(volta.Watered);
            Assert.IsTrue(loaded.PlotAt(-10f, 6f).IsEmpty);
        }

        [Test]
        public void Save_antigo_sem_pa_carrega_a_horta_como_antes()
        {
            var d = new SaveData { versao = 1, dia = 2, moedas = 10 };
            d.canteiros.Add(new SaveCanteiro { planta = "boldo", crescimento = 0.5f, preparado = true });
            d.canteiros.Add(new SaveCanteiro { planta = "", preparado = true });

            var loaded = new GameSession(TestData.Db(), new FixedRandom(), 100f, 4);
            SaveGame.Restore(loaded, d);

            Assert.IsFalse(loaded.TemPa);
            Assert.AreEqual(4, loaded.Plots.Count);
            Assert.AreEqual("boldo", loaded.Plots[0].PlantId);
            Assert.AreEqual(PlotState.Preparado, loaded.Plots[1].State);
            Assert.AreEqual(FarmLayout.HortaX(1), loaded.Plots[1].X, 0.0001f, "a posição vem da grade, não do save");
        }
    }
}
