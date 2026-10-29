using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Alquimia caseira: Varal (secar) → Pilão (pó) → Caldeirão (chás, garrafadas, elixires, banhos...).</summary>
    public partial class GameUI
    {
        void DrawCaldeirao(Rect body)
        {
            GUILayout.BeginArea(body);
            if (!S.Status.CanUseCauldron)
                GUILayout.Label("Sua mão está queimada! Passe gel de babosa antes de mexer no caldeirão.", sWarn);
            GUILayout.Label("Energia por preparo: " + Mathf.RoundToInt(Crafting.CustoEnergia * S.Status.ActionCostMultiplier), sTextItalic);
            scroll = GUILayout.BeginScrollView(scroll);

            var recipes = S.Db.Recipes.Where(r => !r.id.StartsWith(GameDatabase.SimpleRemedyPrefix, System.StringComparison.Ordinal) || KnownRecipe(r))
                .OrderByDescending(r => Crafting.HasIngredients(r, S.Inventory)).ToList();
            if (recipes.Count == 0) GUILayout.Label("Descubra plantas para aprender preparos.", sText);

            // texto com largura fixa: num grupo horizontal o rótulo que quebra linha mede a frase inteira e passava da borda do cartão
            float textWidth = CardTextWidth(body, 130f);
            foreach (var r in recipes)
            {
                bool known = KnownRecipe(r);
                bool can = Crafting.HasIngredients(r, S.Inventory) && S.Status.CanUseCauldron;
                GUILayout.BeginHorizontal(sCard);
                GUILayout.BeginVertical(GUILayout.Width(textWidth));
                GUILayout.Label(r.nome + "  ·  " + TipoNome(r.tipo), sSection);
                GUILayout.Label(string.Join(" + ", r.ingredientes.Select(i => KnownPlant(i.plantId) ? IngredientText(i, true) : i.qtd + "x ???").ToArray()), can ? sText : sTextItalic);
                GUILayout.Label(known ? Describe(r) : "Descubra as plantas desta receita para ler o preparo.", sTextItalic);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                // trava de 0,8 s: o clique duplo não prepara duas vezes (e gasta os ingredientes em dobro)
                bool fazendo = preparando == r.id && ClickLocked;
                if (ListButton(fazendo ? "Preparando…" : "Preparar", can, 130f, 0.8f))
                {
                    var id = r.id;
                    preparando = id;
                    Defer(() => S.Craft(id));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        string preparando; // receita do último clique em "Preparar"

        /// <summary>Largura do texto de um cartão de lista com botões à direita (desconta botões, margens e a barra de rolagem).</summary>
        static float CardTextWidth(Rect body, float buttons) { return Mathf.Max(200f, body.width - buttons - 90f); }

        static string TipoNome(string tipo)
        {
            switch (tipo)
            {
                case "Cha": return "Chá";
                case "Defumacao": return "Defumação";
                case "Banir": return "Defumação";
                case "Revelar": return "Ramo";
                case "Sorte": return "Banho";
                default: return tipo;
            }
        }

        void DrawVaral(Rect body)
        {
            var rack = S.Rack;
            GUILayout.BeginArea(body);
            GUILayout.Label("Pendure ervas frescas: elas secam em " + Mathf.RoundToInt(rack.DryingSeconds) + "s. Mudou de ideia? \"Tirar\" devolve a erva para a bolsa (seca, se já secou), sem perder nada.", sTextItalic);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < rack.Capacity; i++)
            {
                var slot = i < rack.Slots.Count ? rack.Slots[i] : null;
                var r = GUILayoutUtility.GetRect(90, 140, GUILayout.Width(90), GUILayout.Height(140));
                GUI.Box(r, GUIContent.none, sCard);
                if (slot == null) { GUI.Label(r, "vazio", sSlotEmpty); continue; }
                DrawPlantIcon(new Rect(r.x + 10, r.y + 6, r.width - 20, 56), slot.PlantId, 4);
                float prog = rack.Progress(slot);
                FillBar(new Rect(r.x + 8, r.y + 64, r.width - 16, 16), prog, 1f, prog >= 1 ? Folha : new Color(0.85f, 0.6f, 0.2f));
                GUI.Label(new Rect(r.x, r.y + 82, r.width, 18), prog >= 1 ? "seca!" : Mathf.RoundToInt(prog * 100) + "%", sSlotText);
                var tirar = new Rect(r.x + 8, r.yMax - 36, r.width - 16, 30);
                if (ClickLocked) GUI.Button(tirar, "Tirar", sSmallButton); // travado: mesma cara, clique ignorado
                else if (Button(tirar, "Tirar", sSmallButton))
                {
                    int idx = i;
                    clickLockUntil = Time.unscaledTime + ClickLock;
                    Defer(() => S.TakeFromRack(idx));
                }
            }
            GUILayout.EndHorizontal();

            if (ListButton("Recolher secas (" + rack.ReadyCount + ")", rack.ReadyCount > 0, 260f))
                Defer(() => { int n = S.CollectDried(); Toast(n + " erva(s) seca(s) na bolsa."); });

            GUILayout.Space(8);
            GUILayout.Label("Ervas frescas na bolsa", sSection);
            scroll = GUILayout.BeginScrollView(scroll);
            var herbs = S.Inventory.OfKind(ItemKind.Erva).ToList();
            if (herbs.Count == 0) GUILayout.Label("Nenhuma erva fresca.", sText);
            foreach (var kv in herbs)
            {
                ItemKind k; string id;
                if (!Inventory.TryParseKey(kv.Key, out k, out id)) continue;
                GUILayout.BeginHorizontal(sCard);
                var ir = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
                DrawItemIcon(ir, kv.Key);
                GUILayout.Label(S.DisplayName(kv.Key) + "  x" + kv.Value, sSection);
                GUILayout.FlexibleSpace();
                if (ListButton("Pendurar", !rack.IsFull, 130f)) Defer(() => S.HangToDry(id));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawPilao(Rect body)
        {
            GUILayout.BeginArea(body);
            GUILayout.Label("Pile ervas secas até virar pó — a base dos elixires. Gasta " + Mathf.RoundToInt(Mortar.CustoEnergia) + " de energia.", sTextItalic);
            scroll = GUILayout.BeginScrollView(scroll);
            var dried = S.Inventory.OfKind(ItemKind.ErvaSeca).ToList();
            if (dried.Count == 0) GUILayout.Label("Nenhuma erva seca. Use o varal primeiro.", sText);
            foreach (var kv in dried)
            {
                ItemKind k; string id;
                if (!Inventory.TryParseKey(kv.Key, out k, out id)) continue;
                GUILayout.BeginHorizontal(sCard);
                var ir = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
                DrawItemIcon(ir, kv.Key);
                GUILayout.Label(S.DisplayName(kv.Key) + "  x" + kv.Value, sSection);
                GUILayout.FlexibleSpace();
                if (ListButton("Pilar", true, 120f))
                    Defer(() => { if (S.Grind(id)) Player.PlayAction("grind", 0.8f); });
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
