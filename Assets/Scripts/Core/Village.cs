using System.Linq;

namespace ErvasDoBrasil.Core
{
    /// <summary>Um aldeão com um mal específico procurando um remédio.</summary>
    public class VillagerRequest
    {
        public string Nome;
        public Ailment Mal;
        public int Recompensa;
        public bool Atendido;
    }

    public static class Village
    {
        public static readonly string[] Nomes =
        {
            "Seu Zé do Engenho", "Dona Cida", "Tonhão da Venda", "Benzedeira Maria", "Pescador Juca", "Professora Lúcia"
        };

        /// <summary>
        /// A arte de cada aldeão, na ordem de <see cref="Nomes"/> (Assets/Art/Characters/npc_{id}.png, gerada por tools/arte).
        /// A arte segue o nome do pedido, não o lugar na vila: antes o Tonhão caía no lugar da Benzedeira e aparecia de senhora.
        /// </summary>
        public static readonly string[] Ids = { "ze", "cida", "tonhao", "maria", "juca", "lucia" };

        /// <summary>Nome da arte do aldeão ("npc_tonhao"), ou null se o nome não for de um aldeão conhecido (save editado).</summary>
        public static string Arte(string nome)
        {
            int i = System.Array.IndexOf(Nomes, nome);
            return i >= 0 && i < Ids.Length ? "npc_" + Ids[i] : null;
        }

        public const int RecompensaBase = 15;

        /// <summary>Sorteia um pedido cujo mal tenha pelo menos uma receita que cure.</summary>
        public static VillagerRequest NewRequest(string nome, GameDatabase db, IRandom rng)
        {
            var pool = Ailments.DailyPool.Concat(new[] { Ailment.MauOlhado })
                .Where(a => db.RecipesThatCure(a).Any())
                .ToList();
            if (pool.Count == 0) return null;
            return new VillagerRequest
            {
                Nome = nome,
                Mal = pool[rng.Range(0, pool.Count)],
                Recompensa = RecompensaBase + rng.Range(0, 11),
                Atendido = false
            };
        }

        public static bool CanTreat(VillagerRequest req, RecipeData r)
        {
            return req != null && !req.Atendido && r != null && r.Cures.Contains(req.Mal);
        }
    }
}
