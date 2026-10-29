using System;

namespace ErvasDoBrasil.Core
{
    /// <summary>
    /// Nível de raizeira. A experiência vem de colher, coletar na mata, descobrir plantas, preparar receitas e curar aldeões.
    /// Cada planta tem um nível mínimo (plants.json → "nivel") para comprar a semente, plantar e coletar na mata.
    /// </summary>
    public class Progression
    {
        /// <summary>XP total para chegar a cada nível (índice 0 = nível 1).</summary>
        public static readonly int[] XpPorNivel = { 0, 40, 110, 220, 380 };
        public static int NivelMaximo { get { return XpPorNivel.Length; } }

        public const int XpColheita = 5;
        public const int XpColetaSelvagem = 3;
        public const int XpNovaPlanta = 10;
        public const int XpReceita = 5;
        public const int XpAldeaoCurado = 15;

        public int Xp { get; private set; }

        /// <summary>Disparado uma vez para cada nível alcançado.</summary>
        public event Action<int> LevelUp;

        public int Nivel { get { return NivelPara(Xp); } }

        public static int NivelPara(int xp)
        {
            int n = 1;
            while (n < NivelMaximo && xp >= XpPorNivel[n]) n++;
            return n;
        }

        public bool NoNivelMaximo { get { return Nivel >= NivelMaximo; } }

        /// <summary>XP total do próximo nível (no máximo, o do último).</summary>
        public int XpProximoNivel { get { return XpPorNivel[Math.Min(Nivel, NivelMaximo - 1)]; } }

        /// <summary>Quanto do nível atual já foi percorrido (0 a 1), para a barra de XP. No nível máximo, 1.</summary>
        public float FracaoDoNivel
        {
            get
            {
                if (NoNivelMaximo) return 1f;
                int inicio = XpPorNivel[Nivel - 1];
                int fim = XpProximoNivel;
                return fim <= inicio ? 1f : Math.Min(1f, (Xp - inicio) / (float)(fim - inicio));
            }
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            int antes = Nivel;
            Xp += amount;
            for (int n = antes + 1; n <= Nivel; n++)
                if (LevelUp != null) LevelUp(n);
        }

        public bool PodeCultivar(PlantData p) { return p != null && Nivel >= p.NivelMinimo; }

        internal void Restore(int xp) { Xp = Math.Max(0, xp); }
    }
}
