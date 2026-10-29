namespace ErvasDoBrasil.Core
{
    public enum CraftOutcome { Sucesso, SucessoComQueimadura, ReceitaDesconhecida, MaosQueimadas, FaltamIngredientes, SemEnergia }

    /// <summary>O caldeirão da vó: transforma ervas em chás, garrafadas, elixires, banhos e amuletos.</summary>
    public static class Crafting
    {
        public const float CustoEnergia = 5f;

        public static bool HasIngredients(RecipeData r, Inventory inv)
        {
            if (r == null || r.ingredientes == null) return false;
            foreach (var ing in r.ingredientes)
                if (!inv.Has(ing)) return false;
            return true;
        }

        public static CraftOutcome Craft(RecipeData r, Inventory inv, PlayerStatus status, IRandom rng)
        {
            if (r == null || r.ingredientes == null || r.ingredientes.Length == 0) return CraftOutcome.ReceitaDesconhecida;
            if (!status.CanUseCauldron) return CraftOutcome.MaosQueimadas;
            if (!HasIngredients(r, inv)) return CraftOutcome.FaltamIngredientes;
            if (!status.TrySpend(CustoEnergia)) return CraftOutcome.SemEnergia;

            foreach (var ing in r.ingredientes)
                inv.Remove(ing);
            inv.Add(Inventory.Product(r.id), 1);

            if (r.chanceQueimadura > 0f && rng.Value() < r.chanceQueimadura)
            {
                status.Add(Ailment.Queimadura);
                return CraftOutcome.SucessoComQueimadura;
            }
            return CraftOutcome.Sucesso;
        }
    }
}
