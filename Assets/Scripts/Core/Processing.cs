using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    /// <summary>Varal de secagem: erva fresca vira erva seca depois de um tempo.</summary>
    public class DryingRack
    {
        public class Slot
        {
            public string PlantId;
            public float Elapsed;
        }

        public const int DefaultCapacity = 6;
        public const float DefaultDryingSeconds = 45f;

        readonly List<Slot> slots = new List<Slot>();
        public readonly int Capacity;
        public readonly float DryingSeconds;

        public DryingRack(int capacity = DefaultCapacity, float dryingSeconds = DefaultDryingSeconds)
        {
            Capacity = capacity;
            DryingSeconds = dryingSeconds;
        }

        public IList<Slot> Slots { get { return slots.AsReadOnly(); } }
        public bool IsFull { get { return slots.Count >= Capacity; } }
        public int ReadyCount { get { return slots.Count(s => s.Elapsed >= DryingSeconds); } }

        public float Progress(Slot s) { return DryingSeconds <= 0f ? 1f : System.Math.Min(1f, s.Elapsed / DryingSeconds); }

        /// <summary>Pendura uma erva fresca da bolsa no varal.</summary>
        public bool Hang(string plantId, Inventory inv)
        {
            if (IsFull || !inv.Has(Inventory.Herb(plantId))) return false;
            inv.Remove(Inventory.Herb(plantId));
            slots.Add(new Slot { PlantId = plantId, Elapsed = 0f });
            return true;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            foreach (var s in slots) s.Elapsed += dt;
        }

        /// <summary>Pendura de volta uma erva de um jogo salvo (sem tirar da bolsa).</summary>
        internal void Restore(string plantId, float elapsed)
        {
            if (IsFull || string.IsNullOrEmpty(plantId)) return;
            slots.Add(new Slot { PlantId = plantId, Elapsed = System.Math.Max(0f, elapsed) });
        }

        /// <summary>
        /// Tira uma erva do varal sem perder nada: volta seca se já secou, fresca se ainda não deu tempo.
        /// Retorna a chave do item devolvido à bolsa (ou null se o índice não existe).
        /// </summary>
        public string Take(int index, Inventory inv)
        {
            if (index < 0 || index >= slots.Count) return null;
            var s = slots[index];
            string key = s.Elapsed >= DryingSeconds ? Inventory.Dried(s.PlantId) : Inventory.Herb(s.PlantId);
            inv.Add(key);
            slots.RemoveAt(index);
            return key;
        }

        /// <summary>Recolhe tudo que já secou para a bolsa. Retorna quantas ervas secas foram recolhidas.</summary>
        public int CollectDry(Inventory inv)
        {
            var ready = slots.Where(s => s.Elapsed >= DryingSeconds).ToList();
            foreach (var s in ready)
            {
                inv.Add(Inventory.Dried(s.PlantId));
                slots.Remove(s);
            }
            return ready.Count;
        }
    }

    /// <summary>Pilão: erva seca vira pó (base dos elixires).</summary>
    public static class Mortar
    {
        public const float CustoEnergia = 3f;

        public static bool Grind(string plantId, Inventory inv, PlayerStatus status)
        {
            if (!inv.Has(Inventory.Dried(plantId))) return false;
            if (!status.TrySpend(CustoEnergia)) return false;
            inv.Remove(Inventory.Dried(plantId));
            inv.Add(Inventory.Powder(plantId));
            return true;
        }
    }
}
