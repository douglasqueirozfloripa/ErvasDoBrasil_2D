using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// Controle de som do rodapé: o botão de mudo e a tecla M silenciam só o jogo e devolvem o volume de antes,
    /// a barra muda o volume, a música não para no mudo e a escolha volta quando o jogo abre de novo.
    /// </summary>
    public class SomTests : JogadorSimulado
    {
        static readonly WaitForSecondsRealtime MeioSegundo = new WaitForSecondsRealtime(0.5f);

        static void ComSom(float volume)
        {
            GameAudio.Settings.Mudo = false;
            GameAudio.Settings.Volume = volume;
            GameAudio.Aplicar();
        }

        [UnityTest]
        public IEnumerator Botao_de_mudo_do_rodape_silencia_o_jogo_e_devolve_o_volume()
        {
            ComSom(0.6f);
            yield return ComecarPartida();
            yield return Foto("rodape-com-som");

            yield return Clicar(GameUI.BotaoMudo);
            Assert.IsTrue(GameAudio.Settings.Mudo, "o botão do rodapé deixa o jogo mudo");
            Assert.AreEqual(0f, AudioListener.volume, "no mudo o jogo não toca nada");
            yield return Foto("rodape-mudo");

            yield return Clicar(GameUI.BotaoMudo);
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f, "o som volta no volume de antes");

            yield return Apertar(GameKey.Som);
            Assert.IsTrue(GameAudio.Settings.Mudo, "a tecla M também deixa mudo");
            yield return Apertar(GameKey.Som);
            Assert.AreEqual(0.6f, AudioListener.volume, 1e-4f, "e a tecla M devolve o som");
        }

        [UnityTest]
        public IEnumerator Controle_de_som_aparece_no_menu_inicial_e_no_menu_da_partida()
        {
            ComSom(0.6f);
            yield return Clicar(GameUI.BotaoMudo);
            Assert.AreEqual(0f, AudioListener.volume, "dá para silenciar já no menu inicial");
            yield return Foto("menu-inicial-mudo");
            yield return Clicar(GameUI.BotaoMudo);

            yield return ComecarPartida();
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => Tela.Current == GameUI.Window.Pausa, "Esc não abriu o Menu");
            yield return Clicar(GameUI.BotaoMudo);
            Assert.AreEqual(0f, AudioListener.volume, "com o Menu aberto o controle continua no rodapé");
            yield return Foto("menu-da-partida-mudo");
            yield return Clicar(GameUI.BotaoMudo);
        }

        [UnityTest]
        public IEnumerator Arrastar_a_barra_do_rodape_muda_o_volume_do_jogo()
        {
            yield return ComecarPartida();
            Assert.AreEqual(SoundSettings.VolumePadrao, AudioListener.volume, 1e-4f, "o jogo começa no volume padrão");

            yield return Arrastar(GameUI.BarraVolume, 0.3f);
            Assert.AreEqual(0.3f, AudioListener.volume, 1e-4f, "puxar o pino muda o volume do jogo");
            Assert.AreEqual(30, GameAudio.Settings.Porcentagem, "a porcentagem ao lado acompanha");
            yield return Foto("volume-30");

            yield return Arrastar(GameUI.BarraVolume, 1f);
            Assert.AreEqual(1f, AudioListener.volume, 1e-4f, "no fim da barra o volume é o máximo");
        }

        [UnityTest]
        public IEnumerator No_mudo_puxar_a_barra_liga_o_som_de_novo()
        {
            ComSom(0.6f);
            yield return ComecarPartida();
            yield return Clicar(GameUI.BotaoMudo);
            Assert.AreEqual(0f, AudioListener.volume);

            yield return Arrastar(GameUI.BarraVolume, 0.4f); // diferente dos 0,6 guardados: é um arraste de verdade
            Assert.IsFalse(GameAudio.Settings.Mudo, "mexer no volume tira do mudo");
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-4f, "e o som volta no nível novo");
        }

        [UnityTest]
        public IEnumerator Barra_no_zero_silencia_e_o_botao_devolve_o_volume_padrao()
        {
            yield return ComecarPartida();
            yield return Arrastar(GameUI.BarraVolume, 0f);
            Assert.AreEqual(0f, AudioListener.volume, "com a barra no zero o jogo fica em silêncio");
            Assert.IsTrue(GameAudio.Settings.Silencioso);
            yield return Foto("volume-zero");

            yield return Clicar(GameUI.BotaoMudo);
            Assert.AreEqual(SoundSettings.VolumePadrao, AudioListener.volume, 1e-4f, "o botão devolve um volume que dá para ouvir");
        }

        [UnityTest]
        public IEnumerator No_mudo_a_trilha_continua_tocando_sem_volume()
        {
            yield return ComecarPartida();
            var musica = Object.FindAnyObjectByType<MusicManager>();
            Assert.IsNotNull(musica, "o mundo tem a música");
            yield return Esperar(() => musica.GetComponents<AudioSource>().Any(a => a.isPlaying), "a trilha não começou a tocar");

            yield return Clicar(GameUI.BotaoMudo);
            yield return MeioSegundo;
            Assert.AreEqual(0f, AudioListener.volume);
            Assert.IsTrue(musica.GetComponents<AudioSource>().Any(a => a.isPlaying), "o mudo só tira o volume: a música segue tocando por baixo");
        }

        [UnityTest]
        public IEnumerator Volume_e_mudo_escolhidos_voltam_quando_o_jogo_abre_de_novo()
        {
            yield return ComecarPartida();
            yield return Arrastar(GameUI.BarraVolume, 0.4f);
            yield return Clicar(GameUI.BotaoMudo);

            // abrir o jogo de novo: a memória esquece a escolha e o AudioListener volta ao 1 de um processo novo;
            // quem tem de deixar mudo é o GameManager.Awake, lendo o PlayerPrefs
            GameAudio.Recarregar();
            AudioListener.volume = 1f;
            yield return VoltarAoMenuInicial();
            Assert.IsTrue(GameAudio.Settings.Mudo, "o jogo abre mudo, como ficou");
            Assert.AreEqual(0f, AudioListener.volume, "e não toca nada ao abrir");
            yield return Foto("menu-inicial-mudo-gravado");

            yield return Clicar(GameUI.BotaoMudo);
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-4f, "tirar do mudo volta no volume escolhido antes de fechar");
        }
    }
}
