using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ErvasDoBrasil.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// Mede o FPS da bateria inteira de testes de interface: nasce no primeiro teste, sobrevive às trocas de cena
    /// (DontDestroyOnLoad) e anota o tempo de cada quadro até o último teste. Guarda duas listas: <b>tudo</b> (cada quadro
    /// que rodou) e <b>jogo</b> (sem os quadros em que o próprio teste trabalha: os 3 quadros de cada troca de cena, em que
    /// o mundo é montado, e os quadros que pagam a foto da tela). A nota do jogo é a que vale; a de tudo mostra o custo
    /// da bateria. No fim de cada teste, <see cref="FimDoTeste"/> regrava o relatório, então o arquivo sempre tem a
    /// bateria até ali, e o run-ui-tests.sh mostra a versão final.
    /// </summary>
    public sealed class AmostradorDeQuadros : MonoBehaviour
    {
        const int QuadrosDaTrocaDeCena = 3;

        public static AmostradorDeQuadros I { get; private set; }

        readonly List<float> tudo = new List<float>(20000);
        readonly List<float> jogo = new List<float>(20000);
        int pular;

        /// <summary>Quantos testes já terminaram desde que o amostrador nasceu.</summary>
        public int Testes { get; private set; }

        /// <summary>Ligado pelo teste enquanto ele faz algo que não é jogo (a foto da tela).</summary>
        public bool Pausado { get; set; }

        public static void Garantir()
        {
            if (I != null) return;
            var go = new GameObject("amostrador-de-quadros");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AmostradorDeQuadros>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += AoCarregarCena;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= AoCarregarCena;
        }

        void AoCarregarCena(Scene cena, LoadSceneMode modo)
        {
            pular = QuadrosDaTrocaDeCena; // a cena acabou de nascer: os próximos quadros pagam o Awake do mundo inteiro
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            tudo.Add(dt);
            if (pular > 0) { pular--; return; }
            if (Pausado) return;
            jogo.Add(dt);
        }

        public string Relatorio()
        {
            var c = CultureInfo.InvariantCulture;
            var doJogo = FrameRateReport.Medir(jogo);
            var deTudo = FrameRateReport.Medir(tudo);
            int seg = (int)deTudo.Segundos;
            return "Bateria inteira: " + Testes.ToString(c) + " testes · " + (seg / 60).ToString(c) + " min " + (seg % 60).ToString(c) + " s · "
                + deTudo.Quadros.ToString(c) + " quadros\n"
                + "  jogo (sem as trocas de cena e as fotos): " + doJogo.Resumo() + "\n"
                + "  tudo (com as trocas de cena e as fotos): " + deTudo.Resumo() + "\n";
        }

        /// <summary>Conta o teste que acabou e regrava o relatório da bateria até aqui.</summary>
        public void FimDoTeste(string arquivo)
        {
            Testes++;
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo));
            File.WriteAllText(arquivo, Relatorio());
        }
    }
}
