using System;
using System.Collections.Generic;
using System.Globalization;

namespace ErvasDoBrasil.Core
{
    /// <summary>A nota do teste de desempenho: como o jogo ficou de jogar.</summary>
    public enum Fluidez { Ruim, Regular, Bom, Excelente }

    /// <summary>
    /// Avaliação de desempenho a partir do tempo de cada quadro (frametime), como num playtest de 15 s.
    /// O FPS médio sozinho engana: 119 quadros rápidos e um de 50 ms dão um número alto no contador, mas o olho vê
    /// a engasgada. Por isso a nota olha três coisas: o FPS médio, o "1% low" (o FPS do 1% de quadros mais lentos,
    /// que mede a consistência) e os engasgos (quadros acima de <see cref="EngasgoMs"/>, uma travada visível).
    /// </summary>
    public sealed class FrameRateReport
    {
        /// <summary>O jogo mira 60 fps (Application.targetFrameRate).</summary>
        public const float AlvoPadrao = 60f;
        /// <summary>Um quadro mais longo que isto, em ms, é um engasgo que dá para ver.</summary>
        public const float EngasgoMs = 50f;

        public int Quadros { get; private set; }
        public float Segundos { get; private set; }
        public float FpsMedio { get; private set; }
        /// <summary>FPS médio do 1% de quadros mais lentos (pelo menos um quadro): a consistência, não a média.</summary>
        public float FpsUmPorCento { get; private set; }
        public float PiorQuadroMs { get; private set; }
        /// <summary>Quadros acima de <see cref="EngasgoMs"/>.</summary>
        public int Engasgos { get; private set; }
        public float Alvo { get; private set; }
        public Fluidez Nota { get; private set; }

        /// <summary>Mede a partir dos tempos de cada quadro, em segundos (Time.unscaledDeltaTime). Valores inválidos são ignorados.</summary>
        public static FrameRateReport Medir(IReadOnlyList<float> temposDosQuadros, float alvo = AlvoPadrao)
        {
            var r = new FrameRateReport { Alvo = alvo };
            var tempos = new List<float>(temposDosQuadros.Count);
            foreach (float t in temposDosQuadros)
            {
                if (float.IsNaN(t) || float.IsInfinity(t) || t <= 0f) continue;
                tempos.Add(t);
                r.Segundos += t;
                if (t * 1000f > r.PiorQuadroMs) r.PiorQuadroMs = t * 1000f;
                if (t * 1000f > EngasgoMs) r.Engasgos++;
            }
            r.Quadros = tempos.Count;
            if (r.Quadros == 0 || r.Segundos <= 0f)
            {
                r.Nota = Fluidez.Ruim;
                return r;
            }
            r.FpsMedio = r.Quadros / r.Segundos;

            tempos.Sort((a, b) => b.CompareTo(a)); // do mais lento para o mais rápido
            int piores = r.Quadros / 100;
            if (piores < 1) piores = 1;
            float soma = 0f;
            for (int i = 0; i < piores; i++) soma += tempos[i];
            r.FpsUmPorCento = piores / soma;

            r.Nota = Avaliar(r.FpsMedio, r.FpsUmPorCento, r.Engasgos, alvo);
            return r;
        }

        /// <summary>
        /// A régua, para o alvo de 60 fps: excelente = média ≥ 55 fps, 1% low ≥ 45 fps e nenhum engasgo;
        /// bom = média ≥ 45, 1% low ≥ 30 e até 2 engasgos; regular = média ≥ 30, 1% low ≥ 15 e até 5 engasgos;
        /// o resto é ruim. Com outro alvo os limites escalam na proporção. Um FPS alto que oscila cai de nota
        /// pelo 1% low e pelos engasgos. Os FPS são comparados com uma casa decimal, para 29,99 valer como 30.
        /// </summary>
        public static Fluidez Avaliar(float fpsMedio, float fpsUmPorCento, int engasgos, float alvo = AlvoPadrao)
        {
            float escala = alvo / AlvoPadrao;
            double media = Math.Round(fpsMedio, 1), umPorCento = Math.Round(fpsUmPorCento, 1);
            if (media >= Limite(55f, escala) && umPorCento >= Limite(45f, escala) && engasgos == 0) return Fluidez.Excelente;
            if (media >= Limite(45f, escala) && umPorCento >= Limite(30f, escala) && engasgos <= 2) return Fluidez.Bom;
            if (media >= Limite(30f, escala) && umPorCento >= Limite(15f, escala) && engasgos <= 5) return Fluidez.Regular;
            return Fluidez.Ruim;
        }

        static double Limite(float fpsNoAlvoDe60, float escala)
        {
            return Math.Round(fpsNoAlvoDe60 * escala, 1);
        }

        /// <summary>Uma linha para o resultado do teste: a nota e os números que a explicam.</summary>
        public string Resumo()
        {
            var c = CultureInfo.InvariantCulture;
            return Nota.ToString().ToUpperInvariant()
                + " · " + FpsMedio.ToString("0", c) + " fps de média"
                + " · 1% low " + FpsUmPorCento.ToString("0", c) + " fps"
                + " · pior quadro " + PiorQuadroMs.ToString("0", c) + " ms"
                + " · " + Engasgos.ToString(c) + (Engasgos == 1 ? " engasgo" : " engasgos") + " (> " + EngasgoMs.ToString("0", c) + " ms)"
                + " · " + Quadros.ToString(c) + " quadros em " + Segundos.ToString("0.0", c) + " s";
        }
    }
}
