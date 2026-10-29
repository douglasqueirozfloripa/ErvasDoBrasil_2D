using System;

namespace ErvasDoBrasil.Core
{
    public enum SpiritType
    {
        Assombracao, // visível; ao encostar drena energia e aura, pode deixar você Assombrado
        Visagem,     // invisível sem arruda; drena sanidade de perto
        Saci,        // rouba um item e some num redemoinho
        OlhoGordo,   // de longe, joga mau-olhado
        Cuca,        // de madrugada, pega quem ainda está acordada: cansaço e sono atrasado
        Bernuncia    // a bicharada do Boi-de-Mamão: engole um canteiro (ou você, e te cospe em casa)
    }

    /// <summary>Regras do combate passivo: quem afasta quem. Nenhum espírito é "morto" — só afastado ou banido.</summary>
    public static class ProtectionRules
    {
        public const float AggroBase = 7f;
        public const float OlhoGordoAlcance = 4.5f;

        /// <summary>A casa ainda está benzida pela vó: nas primeiras noites nenhuma assombração aparece.</summary>
        public const int NoitesSeguras = 2;

        /// <summary>A partir deste dia a Cuca e a Bernúncia também podem aparecer.</summary>
        public const int DiaDasLendas = 5;
        public const float ChanceLenda = 0.25f;

        /// <summary>Madrugada (22h às 04h): a hora da Cuca.</summary>
        public static bool Madrugada(float time01) { return time01 >= 22f / 24f || time01 < 4f / 24f; }

        /// <summary>Espada-de-São-Jorge e Comigo-ninguém-pode barram todos; Pimenteira barra o Olho-Gordo; Guiné barra o Saci.</summary>
        public static bool AuraBlocks(AuraType aura, SpiritType spirit)
        {
            switch (aura)
            {
                case AuraType.Barreira: return true;
                case AuraType.AntiOlhoGordo: return spirit == SpiritType.OlhoGordo;
                case AuraType.AntiSaci: return spirit == SpiritType.Saci;
                default: return false;
            }
        }

        /// <summary>O amuleto espanta assombração, visagem, olho-gordo e a Cuca. O Saci e a Bernúncia não ligam para ele.</summary>
        public static bool AmuletoRepele(SpiritType spirit) { return spirit != SpiritType.Saci && spirit != SpiritType.Bernuncia; }

        /// <summary>Visagens só aparecem para quem está sob efeito da arruda.</summary>
        public static bool IsVisible(SpiritType spirit, PlayerStatus status)
        {
            return spirit != SpiritType.Visagem || status.VeEspiritosOcultos;
        }

        public static float AggroRadius(PlayerStatus status) { return AggroBase * status.AggroMultiplier; }

        /// <summary>Nenhum nas noites seguras; depois cresce devagar (1, 1, 2, 2, 3...) até 8.</summary>
        public static int MaxSpirits(int day)
        {
            if (day <= NoitesSeguras) return 0;
            return Math.Min(8, (day - NoitesSeguras - 1) / 2 + 1);
        }

        /// <summary>
        /// Sorteia quem aparece. Do dia <see cref="DiaDasLendas"/> em diante, às vezes vem uma lenda:
        /// a Cuca de madrugada, a Bernúncia no resto da noite.
        /// </summary>
        public static SpiritType PickSpawn(IRandom rng, int day = 1, bool madrugada = false)
        {
            if (day >= DiaDasLendas && rng.Value() < ChanceLenda)
                return madrugada ? SpiritType.Cuca : SpiritType.Bernuncia;
            float v = rng.Value();
            if (v < 0.35f) return SpiritType.Assombracao;
            if (v < 0.55f) return SpiritType.Visagem;
            if (v < 0.80f) return SpiritType.Saci;
            return SpiritType.OlhoGordo;
        }
    }
}
