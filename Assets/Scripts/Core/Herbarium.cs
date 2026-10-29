using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    /// <summary>Entradas do Livro desbloqueadas ao colher/coletar a planta pela primeira vez.</summary>
    public class Herbarium
    {
        readonly HashSet<string> discovered = new HashSet<string>();

        public bool IsDiscovered(string plantId) { return plantId != null && discovered.Contains(plantId); }

        public bool Discover(string plantId)
        {
            if (string.IsNullOrEmpty(plantId)) return false;
            return discovered.Add(plantId);
        }

        public int Count { get { return discovered.Count; } }

        public IEnumerable<string> All { get { return discovered.OrderBy(id => id, System.StringComparer.Ordinal).ToList(); } }

        public float Progress(GameDatabase db)
        {
            int total = db.Plants.Count;
            if (total == 0) return 0f;
            return db.Plants.Count(p => discovered.Contains(p.id)) / (float)total;
        }

        public bool IsComplete(GameDatabase db)
        {
            return db.Plants.Count > 0 && db.Plants.All(p => discovered.Contains(p.id));
        }
    }

    public static class DailyEvents
    {
        public const float ChanceMalDoDia = 0.5f;

        /// <summary>Ao amanhecer, com certa chance o personagem acorda com um mal novo.</summary>
        public static Ailment RollMorningAilment(PlayerStatus status, IRandom rng, float chance = ChanceMalDoDia)
        {
            if (rng.Value() >= chance) return Ailment.Nenhum;
            var candidates = Ailments.DailyPool.Where(a => !status.Has(a)).ToList();
            if (candidates.Count == 0) return Ailment.Nenhum;
            var pick = candidates[rng.Range(0, candidates.Count)];
            status.Add(pick);
            return pick;
        }
    }
}
