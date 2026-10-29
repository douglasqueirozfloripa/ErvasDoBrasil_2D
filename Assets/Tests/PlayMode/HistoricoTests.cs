using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// Histórico: o botão ao lado da Bolsa (e a tecla H) mostra os últimos avisos da partida, com a subida de nível
    /// e as plantas liberadas em destaque; guarda também os avisos que nascem na Unity, e volta com o jogo salvo.
    /// </summary>
    public class HistoricoTests : JogadorSimulado
    {
        static bool NoHistorico(string trecho) { return Sessao.Avisos.EmOrdem.Any(a => a.Texto.Contains(trecho)); }

        [UnityTest]
        public IEnumerator Subir_de_nivel_fica_no_historico_com_as_plantas_novas()
        {
            yield return ComecarPartida();
            Sessao.Progresso.Add(Progression.XpPorNivel[1]); // experiência para o nível 2
            yield return Esperar(() => !Tela.CelebrandoNivel, "a festa do nível não terminou", 12f);

            var aviso = Sessao.Avisos.EmOrdem.LastOrDefault(a => a.Tipo == AvisoTipo.Nivel);
            Assert.IsNotNull(aviso, "a subida de nível ficou no histórico");
            StringAssert.StartsWith("Nível 2!", aviso.Texto);
            foreach (var p in Sessao.Db.Plants.Where(p => p.NivelMinimo == 2))
                StringAssert.Contains(p.nome, aviso.Texto, "com a dica das plantas que o nível liberou");
            Assert.Greater(Sessao.Avisos.NaoLidos, 0, "o selo do botão conta os avisos novos");
            yield return Foto("botao-historico-com-selo");

            yield return Clicar(GameUI.BotaoHistorico);
            yield return Esperar(() => Tela.Current == GameUI.Window.Historico, "o botão não abriu o Histórico");
            Assert.AreEqual(0, Sessao.Avisos.NaoLidos, "abrir o Histórico zera o selo");
            yield return Foto("historico-com-nivel");

            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o Histórico");
            yield return Apertar(GameKey.Historico);
            yield return Esperar(() => Tela.Current == GameUI.Window.Historico, "a tecla H não abriu o Histórico");
            yield return Apertar(GameKey.Historico);
            yield return Esperar(() => !Tela.IsModal, "a tecla H não fechou o Histórico");
        }

        [UnityTest]
        public IEnumerator Historico_guarda_avisos_da_Unity_e_volta_com_o_jogo_salvo()
        {
            yield return ComecarPartida();
            Sessao.Progresso.Add(Progression.XpPorNivel[1]);
            yield return SalvarPeloMenu();
            Assert.IsTrue(NoHistorico("Jogo salvo."), "o aviso de salvar (que nasce na Unity, não nas regras) também fica");

            yield return VoltarAoMenuInicial();
            yield return Clicar("Continuar");
            yield return Esperar(() => !Tela.IsModal, "o Continuar não abriu a partida");
            Assert.IsTrue(NoHistorico("Nível 2!"), "o histórico voltou com o jogo salvo");
            Assert.IsTrue(NoHistorico("Jogo carregado"), "e ganhou o aviso de agora");

            yield return Apertar(GameKey.Historico);
            yield return Esperar(() => Tela.Current == GameUI.Window.Historico, "a tecla H não abriu o Histórico");
            yield return Foto("historico-depois-de-carregar");
        }
    }
}
