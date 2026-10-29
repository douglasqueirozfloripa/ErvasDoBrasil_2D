using System;
using System.Collections.Generic;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Índice de todos os sprites de Assets/Art, por nome de arquivo (minúsculo, sem extensão).
    /// É gerado automaticamente pelo editor (menu Ervas do Brasil ▸ Atualizar biblioteca de arte)
    /// e salvo em Assets/ScriptableObjects/Resources/ArtLibrary.asset.
    ///
    /// Convenção de nomes:
    ///   plant_{id}_{0..4}         estágios de crescimento (0 = semente ... 4 = madura)
    ///   icon_{id}                 ícone da planta/produto na UI (opcional)
    ///   player_{down|up|right}_{n} quadros de caminhada; player_idle, player_forage, player_grind
    ///   npc_{id}                  um aldeão por id de Village.Ids (npc_tonhao...): a arte segue o nome do pedido
    ///   spirit_{alma|visagem|saci|olhogordo}_{n}
    ///   map_fazenda               fundo do mapa
    /// </summary>
    [CreateAssetMenu(menuName = "Ervas do Brasil/Art Library", fileName = "ArtLibrary")]
    public class ArtLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public Sprite sprite;
        }

        public List<Entry> sprites = new List<Entry>();

        Dictionary<string, Sprite> lookup;
        static ArtLibrary instance;
        static bool loaded;

        public static ArtLibrary Instance
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    instance = Resources.Load<ArtLibrary>("ArtLibrary");
                }
                return instance;
            }
        }

        public static Sprite Get(string key)
        {
            var lib = Instance;
            return lib != null ? lib.Find(key) : null;
        }

        /// <summary>Quadros "{prefix}_0", "{prefix}_1"... até faltar um.</summary>
        public static Sprite[] Frames(string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                var s = Get(prefix + "_" + i);
                if (s == null) break;
                list.Add(s);
            }
            return list.ToArray();
        }

        public Sprite Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in sprites)
                    if (e != null && e.sprite != null && !string.IsNullOrEmpty(e.key)) lookup[e.key] = e.sprite;
            }
            Sprite s;
            return lookup.TryGetValue(key, out s) ? s : null;
        }

        public void ResetCache() { lookup = null; }
    }
}
