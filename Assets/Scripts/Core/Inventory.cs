using System;
using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    public enum ItemKind { Erva, ErvaSeca, Po, Semente, Produto }

    /// <summary>Bolsa do personagem. Chaves: "erva:boldo" (fresca), "seca:boldo", "po:boldo", "semente:boldo", "prod:remedio_boldo".</summary>
    public class Inventory
    {
        public const string HerbPrefix = "erva:";
        public const string DriedPrefix = "seca:";
        public const string PowderPrefix = "po:";
        public const string SeedPrefix = "semente:";
        public const string ProductPrefix = "prod:";

        readonly Dictionary<string, int> items = new Dictionary<string, int>();

        public event Action Changed;

        public static string Herb(string plantId) { return HerbPrefix + plantId; }
        public static string Dried(string plantId) { return DriedPrefix + plantId; }
        public static string Powder(string plantId) { return PowderPrefix + plantId; }
        public static string Seed(string plantId) { return SeedPrefix + plantId; }

        public static string HerbKey(string plantId, HerbForm form)
        {
            switch (form)
            {
                case HerbForm.Seca: return Dried(plantId);
                case HerbForm.Po: return Powder(plantId);
                default: return Herb(plantId);
            }
        }

        public static string IngredientKey(Ingredient ing) { return HerbKey(ing.plantId, ing.Form); }
        public static string Product(string recipeId) { return ProductPrefix + recipeId; }

        public static bool TryParseKey(string key, out ItemKind kind, out string id)
        {
            kind = ItemKind.Erva;
            id = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (key.StartsWith(HerbPrefix, StringComparison.Ordinal)) { kind = ItemKind.Erva; id = key.Substring(HerbPrefix.Length); }
            else if (key.StartsWith(DriedPrefix, StringComparison.Ordinal)) { kind = ItemKind.ErvaSeca; id = key.Substring(DriedPrefix.Length); }
            else if (key.StartsWith(PowderPrefix, StringComparison.Ordinal)) { kind = ItemKind.Po; id = key.Substring(PowderPrefix.Length); }
            else if (key.StartsWith(SeedPrefix, StringComparison.Ordinal)) { kind = ItemKind.Semente; id = key.Substring(SeedPrefix.Length); }
            else if (key.StartsWith(ProductPrefix, StringComparison.Ordinal)) { kind = ItemKind.Produto; id = key.Substring(ProductPrefix.Length); }
            else return false;
            return id.Length > 0;
        }

        public int Count(string key)
        {
            int n;
            return key != null && items.TryGetValue(key, out n) ? n : 0;
        }

        public bool Has(string key, int n = 1) { return Count(key) >= n; }

        public void Add(string key, int n = 1)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Quantidade deve ser positiva");
            ItemKind k; string id;
            if (!TryParseKey(key, out k, out id)) throw new ArgumentException("Chave de item inválida: " + key);
            items[key] = Count(key) + n;
            if (Changed != null) Changed();
        }

        /// <summary>Remove n unidades. Não remove nada se não houver o suficiente.</summary>
        public bool Remove(string key, int n = 1)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Quantidade deve ser positiva");
            int have = Count(key);
            if (have < n) return false;
            if (have == n) items.Remove(key); else items[key] = have - n;
            if (Changed != null) Changed();
            return true;
        }

        public int TotalItems { get { return items.Values.Sum(); } }

        public IEnumerable<KeyValuePair<string, int>> All
        {
            get { return items.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList(); }
        }

        public IEnumerable<KeyValuePair<string, int>> OfKind(ItemKind kind)
        {
            return All.Where(kv =>
            {
                ItemKind k; string id;
                return TryParseKey(kv.Key, out k, out id) && k == kind;
            });
        }

        /// <summary>Escolhe um item aleatório ponderado pela quantidade (usado pelo Saci).</summary>
        public string RandomKey(IRandom rng)
        {
            int total = TotalItems;
            if (total == 0) return null;
            int pick = rng.Range(0, total);
            foreach (var kv in All)
            {
                if (pick < kv.Value) return kv.Key;
                pick -= kv.Value;
            }
            return null;
        }
    }
}
