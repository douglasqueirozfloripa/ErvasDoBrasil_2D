namespace ErvasDoBrasil.Core
{
    /// <summary>Abstração de aleatoriedade para que as regras sejam testáveis de forma determinística.</summary>
    public interface IRandom
    {
        /// <summary>Valor em [0, 1).</summary>
        float Value();

        /// <summary>Inteiro em [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class SystemRandom : IRandom
    {
        readonly System.Random random;

        public SystemRandom() { random = new System.Random(); }
        public SystemRandom(int seed) { random = new System.Random(seed); }

        public float Value() { return (float)random.NextDouble(); }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
