using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// Base dos testes de interface: abre o jogo de verdade (cena Main) e age como uma jogadora,
    /// pelo mesmo caminho da entrada real: joystick virtual para andar (o do iPhone), teclas virtuais (E, I, B, Esc)
    /// e cliques nos botões da tela (<see cref="GameUI.Click"/>). Cada passo pode tirar uma foto da tela.
    /// </summary>
    public abstract class JogadorSimulado
    {
        const float TempoLimite = 10f;

        /// <summary>Fotos e saves dos testes, longe da pasta de saves do jogo. O scripts/run-ui-tests.sh copia as fotos para Builds/ui-tests.</summary>
        public static string Pasta { get { return Path.Combine(Application.persistentDataPath, "ui-tests"); } }

        static string PastaSaves { get { return Path.Combine(Pasta, "saves"); } }

        int fotos;

        protected static GameManager Jogo { get { return GameManager.I; } }
        protected static GameUI Tela { get { return GameManager.I.UI; } }
        protected static GameSession Sessao { get { return GameManager.I.Session; } }
        protected static PlayerController Jogadora { get { return GameManager.I.Player; } }

        static string JanelaAtual { get { return GameManager.I != null ? Tela.Current.ToString() : "sem jogo"; } }

        /// <summary>Quantas vezes o jogo pediu para fechar ("Sair do jogo"). Nos testes o app não fecha: só fica contado aqui.</summary>
        protected static int PedidosParaSair { get; private set; }

        protected static bool PediuParaSair { get { return PedidosParaSair > 0; } }

        [UnitySetUp]
        public IEnumerator AbrirOJogo()
        {
            fotos = 0;
            if (Directory.Exists(PastaSaves)) Directory.Delete(PastaSaves, true);
            SaveStore.Dir = PastaSaves;
            GameUI.PauseOnFocusLoss = false; // clicar em outra janela do Mac durante o teste não abre o Menu
            // o som dos testes fica em chaves próprias do PlayerPrefs (o volume do jogo de verdade não muda) e começa no padrão
            GameAudio.Chave = "ui-tests.som";
            GameAudio.Apagar();
            GameAudio.Recarregar();
            PedidosParaSair = 0;
            GameManager.AoSair = () => PedidosParaSair++; // "Sair do jogo" não fecha o app de teste no meio da rodada
            InputCompat.VirtualStick = Vector2.zero;
            AmostradorDeQuadros.Garantir(); // mede o FPS da bateria inteira (sobrevive às trocas de cena)
            SceneManager.LoadScene("Main");
            yield return Esperar(() => SceneManager.GetActiveScene().name == "Main" && GameManager.I != null && Tela.Current == GameUI.Window.Inicio,
                "o menu inicial não abriu");
        }

        [TearDown]
        public void Limpar()
        {
            Time.timeScale = 1f;
            InputCompat.VirtualStick = Vector2.zero;
            if (GameManager.I != null) Tela.CancelClick();
            SaveStore.Dir = null;
            GameUI.PauseOnFocusLoss = true;
            GameAudio.Apagar();
            GameAudio.Chave = GameAudio.ChavePadrao;
            GameAudio.Recarregar();
            GameManager.AoSair = null;
            if (AmostradorDeQuadros.I != null) AmostradorDeQuadros.I.FimDoTeste(Path.Combine(Pasta, "desempenho.txt"));
        }

        // ───────── Esperas ─────────

        protected static IEnumerator Esperar(Func<bool> condicao, string erro, float limite = TempoLimite)
        {
            float fim = Time.realtimeSinceStartup + limite;
            while (!condicao())
            {
                if (Time.realtimeSinceStartup > fim) Assert.Fail(erro + " (janela aberta: " + JanelaAtual + ")");
                yield return null;
            }
        }

        /// <summary>
        /// Acelera o relógio do jogo (Time.timeScale) até a condição valer: plantas crescem, ervas secam, a noite chega.
        /// O tempo só corre com o mundo rodando, então chame com o Menu fechado.
        /// </summary>
        protected static IEnumerator Acelerar(Func<bool> ate, string erro, float escala = 20f, float limite = 20f)
        {
            Time.timeScale = escala;
            try
            {
                yield return Esperar(ate, erro, limite);
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        // ───────── Mãos da jogadora ─────────

        /// <summary>
        /// Clica num botão da tela pelo texto: no primeiro habilitado ou, com <paramref name="posicao"/>, no daquela posição
        /// (0 = o primeiro desenhado com esse texto). Falha se ele não aparecer habilitado.
        /// </summary>
        protected static IEnumerator Clicar(string botao, int posicao = -1)
        {
            Tela.Click(botao, posicao);
            yield return Esperar(() => !Tela.ClickPending,
                "o botão \"" + botao + "\"" + (posicao >= 0 ? " (posição " + posicao + ")" : "") + " não apareceu habilitado na tela");
            yield return null; // as ações adiadas (Defer) rodam no fim do OnGUI
        }

        /// <summary>Puxa o pino de um controle deslizante da tela (a barra de volume) até o valor. Falha se ele não aparecer habilitado.</summary>
        protected static IEnumerator Arrastar(string controle, float valor)
        {
            Tela.Arrastar(controle, valor);
            yield return Esperar(() => !Tela.DragPending, "o controle \"" + controle + "\" não apareceu habilitado na tela");
            yield return null;
        }

        /// <summary>O botão está desabilitado ou nem aparece na tela: o clique não acontece.</summary>
        protected static IEnumerator NaoConsegueClicar(string botao)
        {
            Tela.Click(botao);
            for (int i = 0; i < 10; i++) yield return null;
            bool clicou = !Tela.ClickPending;
            Tela.CancelClick();
            Assert.IsFalse(clicou, "o botão \"" + botao + "\" não deveria estar clicável");
        }

        /// <summary>Uma tecla (ou o botão de toque equivalente): vale para o próximo Update.</summary>
        protected static IEnumerator Apertar(GameKey tecla)
        {
            InputCompat.PressVirtual(tecla);
            yield return null;
            yield return null;
        }

        /// <summary>Anda com o joystick virtual até o ponto, como arrastar o dedo no iPhone.</summary>
        protected static IEnumerator AndarAte(Vector2 alvo, float folga = 0.15f, float limite = 15f)
        {
            alvo = Jogo.ClampToWorld(alvo); // o ponto mais perto aonde ela consegue ir
            float fim = Time.realtimeSinceStartup + limite;
            while (true)
            {
                Vector2 falta = alvo - (Vector2)Jogadora.transform.position;
                if (falta.magnitude <= folga) break;
                if (Time.realtimeSinceStartup > fim)
                {
                    InputCompat.VirtualStick = Vector2.zero;
                    Assert.Fail("a raizeira não chegou em " + alvo + " (parou em " + (Vector2)Jogadora.transform.position + ")");
                }
                // perto do alvo, o joystick vai só um pouco, para não passar do ponto
                InputCompat.VirtualStick = falta.magnitude > 0.5f ? falta.normalized : falta / 0.5f;
                yield return null;
            }
            InputCompat.VirtualStick = Vector2.zero;
            yield return null;
        }

        /// <summary>Anda até um objeto e espera ele virar o alvo da ação (o prompt "[E] ..." aparece).</summary>
        protected static IEnumerator IrAte(Interactable alvo)
        {
            yield return AndarAte(alvo.transform.position);
            yield return Esperar(() => Jogadora.Current == alvo, "chegou em " + alvo.name + ", mas ele não virou o alvo da ação");
        }

        /// <summary>O caldeirão, o varal ou o pilão da Casa da Vó.</summary>
        protected static Station Estacao(GameUI.Window janela)
        {
            return Interactable.All.OfType<Station>().First(s => s.Window == janela);
        }

        /// <summary>Anda até a estação (ou outro objeto), aperta E e espera a janela abrir.</summary>
        protected static IEnumerator Abrir(Interactable alvo, GameUI.Window janela)
        {
            yield return IrAte(alvo);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == janela, "E em " + alvo.name + " não abriu a janela " + janela);
        }

        /// <summary>Esc ▸ Salvar jogo ▸ Esc.</summary>
        protected static IEnumerator SalvarPeloMenu()
        {
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => Tela.Current == GameUI.Window.Pausa, "Esc não abriu o Menu");
            yield return Clicar("Salvar jogo");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o Menu");
        }

        /// <summary>Esc ▸ Menu inicial (a cena recarrega).</summary>
        protected static IEnumerator VoltarAoMenuInicial()
        {
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => Tela.Current == GameUI.Window.Pausa, "Esc não abriu o Menu");
            yield return Clicar("Menu inicial");
            yield return Esperar(() => GameManager.I != null && Tela.Current == GameUI.Window.Inicio, "não voltou ao menu inicial");
        }

        /// <summary>Menu inicial ▸ Novo jogo ▸ introdução ▸ Começar.</summary>
        protected static IEnumerator ComecarPartida()
        {
            yield return Clicar("Novo jogo");
            yield return Esperar(() => Tela.Current == GameUI.Window.Intro, "a introdução não abriu");
            yield return Clicar("Começar");
            yield return Esperar(() => !Tela.IsModal, "a introdução não fechou");
        }

        // ───────── Fotos ─────────

        /// <summary>Foto da tela em ui-tests/fotos/&lt;teste&gt;/NN-nome.png. Sem placa de vídeo (modo batch) não há o que fotografar.</summary>
        protected IEnumerator Foto(string nome)
        {
            if (Application.isBatchMode) yield break;
            var amostrador = AmostradorDeQuadros.I;
            if (amostrador != null) amostrador.Pausado = true; // ler a tela e gravar o PNG custam quadros que não são do jogo
            yield return new WaitForEndOfFrame();
            int w = Screen.width, h = Screen.height;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false);
            var png = Png.Encode(tex.GetPixels32(), w, h);
            UnityEngine.Object.Destroy(tex);

            var dir = Path.Combine(Pasta, "fotos", TestContext.CurrentContext.Test.Name);
            Directory.CreateDirectory(dir);
            fotos++;
            File.WriteAllBytes(Path.Combine(dir, fotos.ToString("00", CultureInfo.InvariantCulture) + "-" + nome + ".png"), png);
            yield return null; // o quadro que pagou a foto
            yield return null;
            if (amostrador != null) amostrador.Pausado = false;
        }
    }
}
