using System.Collections;
using System.Collections.Generic;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// Teste de desempenho: a jogadora simulada anda 15 s pelo sítio inteiro (casa, horta, feira e mata) enquanto o
    /// tempo de cada quadro é anotado. No fim, <see cref="FrameRateReport"/> dá a nota: ruim, regular, bom ou excelente,
    /// olhando o FPS médio, o 1% low (consistência) e os engasgos (quadros acima de 50 ms). A nota sai no log com o
    /// prefixo [Desempenho] (o run-ui-tests.sh mostra no terminal), na foto e na mensagem do teste; ruim reprova.
    /// </summary>
    public class DesempenhoTests : JogadorSimulado
    {
        /// <summary>Segundos descartados no começo: a cena acabou de montar o mundo e os primeiros quadros são mais lentos.</summary>
        const float Aquecimento = 1f;
        const float Duracao = 15f;

        [UnityTest]
        public IEnumerator Quinze_segundos_andando_pelo_sitio_ganham_nota_de_fluidez()
        {
            yield return ComecarPartida();

            // o percurso passa pelos quatro cantos do mapa, de modo que a câmera atravesse tudo o que o jogo desenha
            var b = Jogo.WorldBounds;
            const float margem = 2f;
            var cantos = new[]
            {
                new Vector2(b.xMax - margem, b.yMax - margem),
                new Vector2(b.xMin + margem, b.yMax - margem),
                new Vector2(b.xMin + margem, b.yMin + margem),
                new Vector2(b.xMax - margem, b.yMin + margem),
            };
            int alvo = 0;
            var tempos = new List<float>((int)(Duracao * 120f));
            float inicio = Time.realtimeSinceStartup;
            while (true)
            {
                float t = Time.realtimeSinceStartup - inicio;
                if (t >= Aquecimento + Duracao) break;
                if (t >= Aquecimento) tempos.Add(Time.unscaledDeltaTime);

                Vector2 falta = cantos[alvo] - (Vector2)Jogadora.transform.position;
                if (falta.magnitude < 0.3f) alvo = (alvo + 1) % cantos.Length;
                InputCompat.VirtualStick = falta.normalized;
                yield return null;
            }
            InputCompat.VirtualStick = Vector2.zero;

            var relatorio = FrameRateReport.Medir(tempos);
            string resumo = relatorio.Resumo();
            Debug.Log("[Desempenho] " + resumo);
            Tela.MostrarToast("Desempenho: " + resumo);
            yield return Foto("nota-" + relatorio.Nota.ToString().ToLowerInvariant());

            Assert.GreaterOrEqual(relatorio.Segundos, Duracao - 1f, "o teste mediu menos tempo do que devia: " + resumo);
            Assert.AreNotEqual(Fluidez.Ruim, relatorio.Nota, "o jogo ficou ruim de jogar: " + resumo);
        }
    }
}
