using System;

namespace ErvasDoBrasil.Core
{
    /// <summary>Preços da feira da Dona Cida, a raizeira.</summary>
    public static class Economy
    {
        public static int BasePrice(string key, GameDatabase db)
        {
            ItemKind kind; string id;
            if (!Inventory.TryParseKey(key, out kind, out id)) return 0;
            switch (kind)
            {
                case ItemKind.Erva:
                    var p = db.Plant(id); return p != null ? p.precoVenda : 0;
                case ItemKind.ErvaSeca:
                    var d = db.Plant(id); return d != null ? (int)Math.Ceiling(d.precoVenda * 1.5) : 0;
                case ItemKind.Po:
                    var w = db.Plant(id); return w != null ? d2(w.precoVenda) : 0;
                case ItemKind.Semente:
                    var s = db.Plant(id); return s != null ? Math.Max(1, s.precoSemente / 2) : 0;
                case ItemKind.Produto:
                    var r = db.Recipe(id); return r != null ? r.precoVenda : 0;
            }
            return 0;
        }

        public static int SellPrice(string key, GameDatabase db, PlayerStatus status)
        {
            int b = BasePrice(key, db);
            if (b <= 0) return 0;
            return Math.Max(1, (int)Math.Round(b * status.SellPriceMultiplier, MidpointRounding.AwayFromZero));
        }

        static int d2(int v) { return v * 2; }

        public static int SeedPrice(PlantData p) { return p == null ? 0 : p.precoSemente; }
    }
}
