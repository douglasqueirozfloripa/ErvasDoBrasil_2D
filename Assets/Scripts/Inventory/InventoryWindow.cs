using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Bolsa: lista de itens, com "Usar" para os produtos do caldeirão.</summary>
    public partial class GameUI
    {
        void DrawBolsa(Rect body)
        {
            GUILayout.BeginArea(body);
            var items = S.Inventory.All.ToList();
            GUILayout.Label(S.Money + " moedas  ·  " + S.Inventory.TotalItems + " itens", sSubtitle);
            scroll = GUILayout.BeginScrollView(scroll);
            if (items.Count == 0) GUILayout.Label("A bolsa está vazia. Colete ervas na mata ou compre sementes na feira.", sText);

            foreach (var group in new[] { ItemKind.Produto, ItemKind.Erva, ItemKind.ErvaSeca, ItemKind.Po, ItemKind.Semente })
            {
                var list = S.Inventory.OfKind(group).ToList();
                if (list.Count == 0) continue;
                GUILayout.Label(GroupName(group), sSection);
                foreach (var kv in list)
                {
                    GUILayout.BeginHorizontal(sCard);
                    var ir = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                    DrawItemIcon(ir, kv.Key);
                    GUILayout.BeginVertical();
                    GUILayout.Label(S.DisplayName(kv.Key) + "  x" + kv.Value, sSection);
                    string extra = ExtraInfo(kv.Key);
                    if (!string.IsNullOrEmpty(extra)) GUILayout.Label(extra, sTextItalic);
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    if (group == ItemKind.Produto && ListButton("Usar", true, 110f))
                    {
                        ItemKind k; string id;
                        if (Inventory.TryParseKey(kv.Key, out k, out id)) Defer(() => GameManager.I.ApplyUse(S.Use(id)));
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (S.TemPa)
            {
                GUILayout.Label("Ferramentas", sSection);
                GUILayout.BeginHorizontal(sCard);
                var pr = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                SpriteFactory.DrawSprite(pr, ArtLibrary.Get("icon_pa"));
                GUILayout.BeginVertical();
                GUILayout.Label("Pá de cavar", sSection);
                GUILayout.Label("Pare num lugar livre do sítio e aperte E (Ação) para cavar um canteiro. A moldura amarela mostra onde.", sTextItalic);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static string GroupName(ItemKind k)
        {
            switch (k)
            {
                case ItemKind.Produto: return "Remédios e preparos";
                case ItemKind.Erva: return "Ervas frescas";
                case ItemKind.ErvaSeca: return "Ervas secas";
                case ItemKind.Po: return "Pós";
                default: return "Sementes";
            }
        }

        string ExtraInfo(string key)
        {
            ItemKind kind; string id;
            if (!Inventory.TryParseKey(key, out kind, out id)) return null;
            if (kind == ItemKind.Produto)
            {
                var r = S.Db.Recipe(id);
                return r != null ? Describe(r) : null;
            }
            var p = S.Db.Plant(id);
            if (p == null) return null;
            if (kind == ItemKind.Semente) return "Plante num canteiro preparado. Cresce em ~" + Mathf.RoundToInt(p.tempoCrescimento) + "s regada.";
            return S.Herbarium.IsDiscovered(id) ? p.efeitoNoJogo : null;
        }
    }
}
