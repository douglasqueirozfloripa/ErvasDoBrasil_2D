using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>A pá: comprar na feira, cavar um canteiro fora da horta, plantar nele (só plantas de proteção) e desfazer.</summary>
    public class PaTests : JogadorSimulado
    {
        // ao lado da porta da Casa da Vó, longe do caldeirão e do pilão: parada aqui olhando para baixo,
        // a moldura cai na casa da grade (6,8; 0,4)
        static readonly Vector2 PertoDaPorta = new Vector2(6.8f, 1.0f);
        static readonly Vector2 CasaDoCanteiro = new Vector2(FarmLayout.SnapX(6.8f), FarmLayout.SnapY(0.4f));

        [UnityTest]
        public IEnumerator Comprar_a_pa_cavar_na_porta_de_casa_e_plantar_espada_de_sao_jorge()
        {
            yield return ComecarPartida();
            yield return AndarAte(new Vector2(1.2f, -4.4f));
            yield return Foto("horta-3-fileiras");

            var feira = Interactable.All.OfType<Market>().First();
            yield return IrAte(feira);
            yield return Foto("rotulos-da-vila");
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Feira, "a feira não abriu");
            yield return Clicar("Comprar");          // aba
            yield return Foto("feira-pa");
            int moedas = Sessao.Money;
            yield return Clicar("Comprar", 1);       // o primeiro cartão da aba é a pá
            Assert.IsTrue(Sessao.TemPa, "comprou a pá");
            Assert.AreEqual(moedas - GameSession.PrecoPa, Sessao.Money);
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou a feira");

            yield return Parar(PertoDaPorta);
            yield return Esperar(() => Jogadora.CanDig, "parada num lugar livre, a mira da pá não apareceu");
            Assert.AreEqual(CasaDoCanteiro, Jogadora.DigCell);
            Assert.AreEqual("Cavar canteiro aqui", Jogadora.ActionPrompt);
            yield return Foto("mira-da-pa");

            int canteiros = Sessao.Plots.Count;
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Sessao.Plots.Count == canteiros + 1, "E não cavou o canteiro");
            var cavado = Interactable.All.OfType<FarmPlot>().First(p => p.Model.Cavado);
            Assert.AreEqual(PlotState.Preparado, cavado.Model.State, "a pá já prepara a terra");
            Assert.AreEqual(CasaDoCanteiro, (Vector2)cavado.transform.position);

            yield return IrAte(cavado);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Plantar, "E no canteiro cavado não abriu o plantio");
            yield return Foto("plantar-no-cavado");
            // no cavado só as plantas de proteção: das sementes do kit, só a Espada-de-São-Jorge tem "Plantar"
            foreach (var nome in new[] { "Espada-de-São-Jorge", "Pimenteira", "Guiné", "Comigo-ninguém-pode" })
                StringAssert.Contains(nome, Sessao.PlantasDeProtecao);
            yield return NaoConsegueClicar(GameUI.BotaoSoNaHorta);
            yield return Clicar("Plantar");
            Assert.AreEqual("espada_sao_jorge", cavado.Model.PlantId);
            yield return Esperar(() => !Tela.IsModal, "plantar não fechou o menu");
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => cavado.Model.Watered, "o canteiro cavado não foi regado");
            yield return Foto("espada-na-porta");
        }

        [UnityTest]
        public IEnumerator Sem_pa_nao_aparece_a_mira()
        {
            yield return ComecarPartida();
            yield return Parar(PertoDaPorta);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsFalse(Jogadora.CanDig);
            Assert.IsNull(Jogadora.ActionPrompt);
        }

        [UnityTest]
        public IEnumerator Canteiro_cavado_e_vazio_pode_ser_desfeito()
        {
            yield return ComecarPartida();
            Assert.IsTrue(Sessao.BuyShovel());
            yield return Parar(PertoDaPorta);
            yield return Esperar(() => Jogadora.CanDig, "a mira da pá não apareceu");
            yield return Apertar(GameKey.Interagir);
            var cavado = Interactable.All.OfType<FarmPlot>().FirstOrDefault(p => p.Model.Cavado);
            Assert.IsNotNull(cavado, "E não cavou o canteiro");

            yield return IrAte(cavado);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Plantar, "o plantio não abriu");
            yield return Clicar("Desfazer este canteiro");
            yield return Esperar(() => !Tela.IsModal, "desfazer não fechou o menu");
            Assert.IsFalse(Sessao.Plots.Contains(cavado.Model));
            yield return Esperar(() => cavado == null, "o canteiro desfeito continuou no mapa");
        }

        /// <summary>A mesma semente comum: o canteiro cavado recusa (fica na bolsa) e a horta da vó aceita.</summary>
        [UnityTest]
        public IEnumerator Semente_comum_vai_na_horta_da_vo_e_nao_no_canteiro_cavado()
        {
            yield return ComecarPartida();
            Assert.IsTrue(Sessao.BuyShovel());
            string boldo = Inventory.Seed("boldo");
            int sementes = Sessao.Inventory.Count(boldo);

            // no canteiro cavado o boldo aparece, mas com "Só na horta", e não vai para a terra
            yield return Parar(PertoDaPorta);
            yield return Esperar(() => Jogadora.CanDig, "a mira da pá não apareceu");
            yield return Apertar(GameKey.Interagir);
            var cavado = Interactable.All.OfType<FarmPlot>().FirstOrDefault(p => p.Model.Cavado);
            Assert.IsNotNull(cavado, "E não cavou o canteiro");
            yield return IrAte(cavado);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Plantar, "o plantio do canteiro cavado não abriu");
            yield return NaoConsegueClicar(GameUI.BotaoSoNaHorta);
            Assert.IsTrue(cavado.Model.IsEmpty, "o canteiro cavado continua vazio");
            Assert.AreEqual(sementes, Sessao.Inventory.Count(boldo), "o boldo continua na bolsa");
            yield return Foto("cavado-recusa-semente-comum");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o plantio");

            // na horta da vó o mesmo boldo planta: nenhuma semente fica de fora
            var horta = Interactable.All.OfType<FarmPlot>().First(p => !p.Model.Cavado);
            yield return IrAte(horta);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => horta.Model.State == PlotState.Preparado, "a terra da horta não foi preparada");
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Plantar, "o plantio da horta não abriu");
            yield return NaoConsegueClicar(GameUI.BotaoSoNaHorta);
            yield return Foto("horta-aceita-todas");
            var lista = Sessao.Inventory.OfKind(ItemKind.Semente).Select(kv => kv.Key).ToList();
            yield return Clicar("Plantar", lista.IndexOf(boldo));
            Assert.AreEqual("boldo", horta.Model.PlantId, "na horta da vó o boldo vai para a terra");
            Assert.AreEqual(sementes - 1, Sessao.Inventory.Count(boldo));
        }

        /// <summary>Chega por cima e termina andando para baixo, para a raizeira ficar virada para o sul.</summary>
        static IEnumerator Parar(Vector2 ponto)
        {
            yield return AndarAte(ponto + new Vector2(0f, 0.6f));
            yield return AndarAte(ponto);
        }
    }
}
