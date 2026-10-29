using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>Menu inicial, Menu da partida e lista de jogos salvos (salvar, continuar, carregar, apagar e desfazer).</summary>
    public class MenuEJogosSalvosTests : JogadorSimulado
    {
        static readonly WaitForSecondsRealtime UmSegundo = new WaitForSecondsRealtime(1.1f);

        string recente, antiga;

        [UnityTest]
        public IEnumerator Menu_inicial_sem_jogos_salvos_tem_novo_jogo_e_bastidores()
        {
            yield return Foto("menu-inicial");
            yield return NaoConsegueClicar("Continuar"); // sem jogo salvo, não há o que continuar
            yield return Clicar("Bastidores do desenvolvimento");
            yield return Esperar(() => Tela.Current == GameUI.Window.Bastidores, "os Bastidores não abriram");
            yield return Foto("bastidores");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => Tela.Current == GameUI.Window.Inicio, "Esc não voltou ao menu inicial");
        }

        [UnityTest]
        public IEnumerator Sair_do_jogo_no_menu_inicial_fecha_o_app()
        {
            Assert.IsFalse(PediuParaSair, "abrir o jogo não pede para fechar");
            yield return Clicar(GameUI.BotaoSair);
            Assert.IsTrue(Tela.Saindo, "o clique responde na hora: a tela escurece com \"Fechando o jogo...\"");
            yield return Foto("fechando-o-jogo");
            yield return Esperar(() => PediuParaSair, "o Sair não pediu para a Unity fechar o app (Application.Quit)");

            // o relato: parecia que só o segundo clique fechava. Agora o botão vira "Saindo..." e não aceita outro clique
            yield return NaoConsegueClicar(GameUI.BotaoSaindo);
            yield return NaoConsegueClicar(GameUI.BotaoSair);
            Assert.AreEqual(1, PedidosParaSair, "um clique, um pedido para fechar");
        }

        /// <summary>Com Continuar e Jogos salvos na tela o menu fica mais longo: o Sair continua cabendo acima do aviso.</summary>
        [UnityTest]
        public IEnumerator Sair_do_jogo_aparece_tambem_com_jogos_salvos_e_nao_fica_no_menu_da_partida()
        {
            yield return CriarDuasPartidasSalvas();
            yield return Foto("menu-inicial-com-sair");

            yield return Clicar("Continuar");
            yield return Esperar(() => !Tela.IsModal, "o Continuar não abriu a partida");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => Tela.Current == GameUI.Window.Pausa, "Esc não abriu o Menu");
            yield return NaoConsegueClicar(GameUI.BotaoSair); // no meio da partida, sair sem salvar perderia o progresso
            Assert.IsFalse(PediuParaSair);

            yield return Clicar("Menu inicial");
            yield return Esperar(() => GameManager.I != null && Tela.Current == GameUI.Window.Inicio, "não voltou ao menu inicial");
            yield return Clicar(GameUI.BotaoSair);
            yield return Esperar(() => PediuParaSair, "de volta ao menu inicial, o Sair fecha o app");
        }

        [UnityTest]
        public IEnumerator Salvar_voltar_ao_menu_inicial_e_continuar_a_partida()
        {
            yield return ComecarPartida();
            var canteiro = Interactable.All.OfType<FarmPlot>().First();
            yield return IrAte(canteiro);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => canteiro.Model.State == PlotState.Preparado, "a terra não foi preparada");
            Vector2 posicao = Jogadora.transform.position;

            yield return SalvarPeloMenu();
            Assert.AreEqual(1, Directory.GetFiles(SaveStore.Dir, "*.json").Length, "salvar grava um arquivo da partida");

            yield return VoltarAoMenuInicial();
            yield return Foto("menu-inicial-com-continuar");
            yield return Clicar("Continuar");
            yield return Esperar(() => !Tela.IsModal, "o Continuar não abriu a partida");
            Assert.AreEqual(PlotState.Preparado, Interactable.All.OfType<FarmPlot>().First().Model.State, "o canteiro preparado voltou");
            Assert.Less(Vector2.Distance(posicao, Jogadora.transform.position), 0.01f, "a raizeira voltou ao mesmo lugar");
            yield return Foto("partida-carregada");
        }

        /// <summary>O bug relatado: apagar um jogo de outro horário não pode levar o jogo do Continuar.</summary>
        [UnityTest]
        public IEnumerator Apagar_um_jogo_antigo_mantem_o_do_Continuar_e_da_para_desfazer()
        {
            yield return CriarDuasPartidasSalvas();
            yield return Clicar("Jogos salvos (2)");
            yield return Esperar(() => Tela.Current == GameUI.Window.Salvos, "a lista de jogos salvos não abriu");
            yield return Foto("lista-com-duas-partidas");

            yield return Clicar("Apagar", 1); // o Apagar da segunda linha: a partida mais antiga
            yield return Foto("confirmar-apagar");
            yield return Clicar("Apagar mesmo");
            CollectionAssert.AreEqual(new[] { recente }, Ids(), "só a partida antiga foi apagada");
            yield return Foto("depois-de-apagar");

            yield return Clicar("Desfazer");
            CollectionAssert.AreEquivalent(new[] { recente, antiga }, Ids(), "o Desfazer trouxe a partida de volta");

            yield return Clicar("Apagar", 1);
            yield return Clicar("Apagar mesmo");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => Tela.Current == GameUI.Window.Inicio, "Esc não voltou ao menu inicial");
            yield return Clicar("Continuar");
            yield return Esperar(() => !Tela.IsModal, "o Continuar não abriu a partida");
            Assert.AreEqual(recente, Jogo.SaveId, "o Continuar abre a partida que sobrou, a mais recente");
        }

        [UnityTest]
        public IEnumerator Carregar_pela_lista_abre_a_partida_escolhida()
        {
            yield return CriarDuasPartidasSalvas();
            yield return Clicar("Jogos salvos (2)");
            yield return Esperar(() => Tela.Current == GameUI.Window.Salvos, "a lista de jogos salvos não abriu");
            yield return Clicar("Carregar", 1); // no menu inicial, carrega direto
            yield return Esperar(() => !Tela.IsModal, "o Carregar não abriu a partida");
            Assert.AreEqual(antiga, Jogo.SaveId, "abriu a partida da segunda linha");
        }

        /// <summary>Duas partidas salvas com pelo menos 1 s de diferença: a segunda fica no topo da lista e no Continuar.</summary>
        IEnumerator CriarDuasPartidasSalvas()
        {
            yield return ComecarPartida();
            yield return SalvarPeloMenu();
            yield return VoltarAoMenuInicial();
            yield return UmSegundo; // o horário salvo tem resolução de 1 s
            yield return ComecarPartida();
            yield return SalvarPeloMenu();
            yield return VoltarAoMenuInicial();

            var lista = SaveStore.List();
            Assert.AreEqual(2, lista.Count, "duas partidas salvas");
            recente = lista[0].id;
            antiga = lista[1].id;
        }

        static List<string> Ids() { return SaveStore.List().Select(d => d.id).ToList(); }
    }
}
