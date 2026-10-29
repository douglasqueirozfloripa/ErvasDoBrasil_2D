using System;
using System.Collections.Generic;

namespace ErvasDoBrasil.Core
{
    public class UseResult
    {
        public bool Consumed;
        public bool Banish;
        public float BanishRadius;
        public readonly List<Ailment> Curados = new List<Ailment>();
        public readonly List<Ailment> Melhorados = new List<Ailment>();
    }

    /// <summary>O que acontece quando o personagem usa um produto do caldeirão.</summary>
    public static class ItemEffects
    {
        public const float RaioDefumacaoPadrao = 5f;
        public const float RevelarPadraoSegundos = 60f;

        public static UseResult Use(RecipeData r, Inventory inv, PlayerStatus status)
        {
            var result = new UseResult();
            if (r == null || !inv.Has(Inventory.Product(r.id))) return result;

            inv.Remove(Inventory.Product(r.id));
            result.Consumed = true;

            var effect = r.Effect;
            // Banhos e defumações limpam o mal espiritual de uma vez; o resto é por dose.
            int doses = effect == ItemEffect.Banho || effect == ItemEffect.Banir ? 99 : Math.Max(1, r.doses);
            foreach (var a in r.Cures)
            {
                var cr = status.CureDose(a, doses);
                if (cr == CureResult.Curado) result.Curados.Add(a);
                else if (cr == CureResult.Melhorou) result.Melhorados.Add(a);
            }

            ApplyBonus(r.bonus, status, effect);

            int dias = Math.Max(1, r.duracaoDias);
            switch (effect)
            {
                case ItemEffect.Amuleto:
                    status.AmuletoDias = Math.Max(status.AmuletoDias, dias);
                    break;
                case ItemEffect.Banho:
                    status.RestoreAura(PlayerStatus.MaxAura);
                    status.SorteDias = Math.Max(status.SorteDias, dias);
                    break;
                case ItemEffect.Sorte:
                    status.SorteDias = Math.Max(status.SorteDias, dias);
                    break;
                case ItemEffect.Banir:
                    result.Banish = true;
                    result.BanishRadius = r.raio > 0f ? r.raio : RaioDefumacaoPadrao;
                    if (status.CureDose(Ailment.Assombrado, 99) == CureResult.Curado && !result.Curados.Contains(Ailment.Assombrado))
                        result.Curados.Add(Ailment.Assombrado);
                    break;
                case ItemEffect.Revelar:
                    float secs = r.bonus != null && r.bonus.segundos > 0f ? r.bonus.segundos : RevelarPadraoSegundos;
                    status.RevelarSegundos = Math.Max(status.RevelarSegundos, secs);
                    break;
            }
            return result;
        }

        static void ApplyBonus(StatBonus b, PlayerStatus status, ItemEffect effect)
        {
            if (b == null) return;
            if (b.energiaPct > 0f) status.Restore(status.MaxEnergy * b.energiaPct);
            if (b.sanidade > 0f) status.RestoreSanity(b.sanidade);
            if (b.aura > 0f) status.RestoreAura(b.aura);
            if (b.velocidadePct > 0f && effect != ItemEffect.Revelar)
            {
                status.BuffVelocidadePct = Math.Max(status.BuffVelocidadePct * (status.BuffVelocidadeSegundos > 0f ? 1f : 0f), b.velocidadePct);
                status.BuffVelocidadeSegundos = Math.Max(status.BuffVelocidadeSegundos, b.segundos > 0f ? b.segundos : 60f);
            }
        }
    }
}
