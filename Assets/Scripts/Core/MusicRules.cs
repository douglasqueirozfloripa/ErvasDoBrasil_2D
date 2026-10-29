namespace ErvasDoBrasil.Core
{
    public enum MusicTrack { Dia, Feira, Noite }

    /// <summary>Qual faixa toca em cada momento (a troca é feita com crossfade pelo MusicManager).</summary>
    public static class MusicRules
    {
        public const float RaioDaVila = 7f;

        public static MusicTrack Choose(bool isNight, bool assombrado, float distanceToVillage)
        {
            if (isNight || assombrado) return MusicTrack.Noite;
            return distanceToVillage <= RaioDaVila ? MusicTrack.Feira : MusicTrack.Dia;
        }

        /// <summary>Nome do arquivo em Assets/Audio/Resources/Music (sem extensão).</summary>
        public static string ResourceName(MusicTrack t)
        {
            switch (t)
            {
                case MusicTrack.Feira: return "trilha_feira";
                case MusicTrack.Noite: return "trilha_noite";
                default: return "trilha_dia";
            }
        }
    }
}
