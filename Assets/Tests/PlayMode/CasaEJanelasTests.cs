using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>Janelas do dia a dia (Bolsa, Livro, Menu), a cama da Casa da Vó e a câmera nas bordas do mapa.</summary>
    public class CasaEJanelasTests : JogadorSimulado
    {
        static readonly WaitForSeconds UmSegundo = new WaitForSeconds(1f);

        [UnityTest]
        public IEnumerator Bolsa_Livro_e_Menu_abrem_e_fecham()
        {
            yield return ComecarPartida();

            yield return Apertar(GameKey.Bolsa);
            yield return Esperar(() => Tela.Current == GameUI.Window.Bolsa, "a tecla I não abriu a Bolsa");
            yield return Foto("bolsa");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou a Bolsa");

            yield return Apertar(GameKey.Livro);
            yield return Esperar(() => Tela.Current == GameUI.Window.Livro, "a tecla B não abriu o Livro");
            yield return Clicar("Males");
            yield return Foto("livro-males");
            yield return Clicar("X");
            yield return Esperar(() => !Tela.IsModal, "o X não fechou o Livro");

            yield return Clicar("Menu (Esc)");
            yield return Esperar(() => Tela.Current == GameUI.Window.Pausa, "o botão Menu não abriu o Menu da partida");
            yield return Foto("menu-da-partida");
            yield return Clicar("Voltar ao jogo");
            yield return Esperar(() => !Tela.IsModal, "Voltar ao jogo não fechou o Menu");
        }

        [UnityTest]
        public IEnumerator Dormir_so_a_noite_e_dormir_salva_o_jogo()
        {
            yield return ComecarPartida();
            var cama = Interactable.All.OfType<Bed>().First();
            yield return IrAte(cama);

            yield return Apertar(GameKey.Interagir); // de dia: "ainda é cedo"
            Assert.AreEqual(1, Sessao.Clock.Day, "de dia não dá para dormir");

            yield return Acelerar(() => Sessao.Clock.IsNight, "a noite não chegou", 30f);
            yield return Foto("noite");
            yield return Esperar(() => Jogadora.Current == cama, "a cama não é o alvo da ação");
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Sessao.Clock.Day == 2, "dormir não passou para o dia 2");
            Assert.AreEqual(1, Directory.GetFiles(SaveStore.Dir, "*.json").Length, "dormir salva o jogo");
        }

        [UnityTest]
        public IEnumerator Raizeira_inteira_na_tela_no_topo_do_mapa()
        {
            yield return ComecarPartida();
            yield return AndarAte(new Vector2(Jogadora.transform.position.x, Jogo.WorldBounds.yMax));
            yield return UmSegundo; // a câmera alcança a raizeira

            var sprite = Jogadora.GetComponent<SpriteRenderer>();
            Assert.Greater(sprite.sortingOrder, SpriteFactory.ChaoOrder + 500, "a raizeira fica acima de todas as camadas de chão");
            var cabeca = Camera.main.WorldToViewportPoint(sprite.bounds.max);
            var pes = Camera.main.WorldToViewportPoint(sprite.bounds.min);
            Assert.LessOrEqual(cabeca.y, 1f, "a cabeça da raizeira cabe na tela");
            Assert.GreaterOrEqual(pes.y, 0f, "os pés da raizeira cabem na tela");
            yield return Foto("topo-do-mapa");
        }
    }
}
