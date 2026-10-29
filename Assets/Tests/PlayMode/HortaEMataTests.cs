using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>Horta (preparar, plantar, regar, colher) e coleta na mata.</summary>
    public class HortaEMataTests : JogadorSimulado
    {
        [UnityTest]
        public IEnumerator Novo_jogo_preparar_plantar_e_regar_na_horta()
        {
            yield return Clicar("Novo jogo");
            yield return Esperar(() => Tela.Current == GameUI.Window.Intro, "a introdução não abriu");
            yield return Foto("introducao");
            yield return Clicar("Começar");
            yield return Esperar(() => !Tela.IsModal, "a introdução não fechou");

            var canteiro = Interactable.All.OfType<FarmPlot>().First();
            yield return PrepararPlantarERegar(canteiro);
            Assert.IsNull(canteiro.Label, "regado e crescendo, o canteiro não pede nada");
            yield return Foto("canteiro-regado");
        }

        [UnityTest]
        public IEnumerator Planta_regada_cresce_e_e_colhida()
        {
            yield return ComecarPartida();
            var canteiro = Interactable.All.OfType<FarmPlot>().First();
            yield return PrepararPlantarERegar(canteiro);
            string planta = canteiro.Model.PlantId;

            yield return Acelerar(() => canteiro.Model.IsReady, "a planta não cresceu");
            yield return Esperar(() => Jogadora.Current == canteiro, "a planta pronta não virou o alvo da ação");
            Assert.AreEqual("Colher [E]", canteiro.Label);
            yield return Foto("pronta-para-colher");

            int xp = Sessao.Progresso.Xp;
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => canteiro.Model.IsEmpty, "a planta não foi colhida");
            Assert.Greater(Sessao.Inventory.Count(Inventory.Herb(planta)), 0, "a colheita foi para a bolsa");
            Assert.IsTrue(Sessao.Herbarium.IsDiscovered(planta), "colher descobre a página do Livro");
            Assert.Greater(Sessao.Progresso.Xp, xp, "colher dá experiência");
        }

        [UnityTest]
        public IEnumerator Coleta_na_mata_respeita_o_nivel_da_raizeira()
        {
            yield return ComecarPartida();
            var ervas = Interactable.All.OfType<WildHerb>().OrderBy(h => Vector2.Distance(h.transform.position, Jogadora.transform.position)).ToList();

            var liberada = ervas.First(h => Sessao.Progresso.PodeCultivar(Sessao.Db.Plant(h.PlantId)));
            yield return IrAte(liberada);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Sessao.Inventory.Count(Inventory.Herb(liberada.PlantId)) > 0, "a erva de nível 1 não foi coletada");
            Assert.IsTrue(Sessao.Herbarium.IsDiscovered(liberada.PlantId), "coletar descobre a página do Livro");

            var trancada = ervas.First(h => !Sessao.Progresso.PodeCultivar(Sessao.Db.Plant(h.PlantId)));
            yield return IrAte(trancada);
            StringAssert.Contains("(nível", trancada.Prompt, "o prompt avisa o nível que falta");
            int itens = Sessao.Inventory.TotalItems;
            yield return Apertar(GameKey.Interagir);
            yield return Foto("erva-trancada");
            Assert.AreEqual(itens, Sessao.Inventory.TotalItems, "erva acima do nível não é coletada");
        }

        /// <summary>E prepara a terra, E abre o plantio, "Plantar" planta a primeira semente e E rega.</summary>
        static IEnumerator PrepararPlantarERegar(FarmPlot canteiro)
        {
            yield return IrAte(canteiro);
            Assert.AreEqual("Preparar a terra", canteiro.Prompt);
            float energia = Sessao.Status.Energy;
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => canteiro.Model.State == PlotState.Preparado, "a terra não foi preparada");
            Assert.Less(Sessao.Status.Energy, energia, "preparar a terra gasta energia");

            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Plantar, "o menu de plantio não abriu");
            yield return Clicar("Plantar");
            yield return Esperar(() => canteiro.Model.State == PlotState.Plantado, "a semente não foi plantada");
            yield return Esperar(() => !Tela.IsModal, "plantar não fechou o menu");
            Assert.AreEqual("Regar [E]", canteiro.Label, "o canteiro pede rega dizendo a tecla");

            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => canteiro.Model.Watered, "o canteiro não foi regado");
        }
    }
}
