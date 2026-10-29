using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>O Livro (Grimório Botânico): Plantas, Males, Receitas e Assombrações.</summary>
    public partial class GameUI
    {
        enum BookTab { Plantas, Males, Receitas, Assombracoes }
        BookTab bookTab = BookTab.Plantas;
        int selPlant;
        int selSpirit;

        const string Aviso = "Conteúdo educativo sobre usos tradicionais e cultura popular. Não substitui orientação médica — procure um profissional de saúde.";

        void DrawLivro(Rect body)
        {
            // abas
            string[] tabs = { "Plantas", "Males", "Receitas", "Assombrações" };
            float tw = Mathf.Min(170f, body.width / tabs.Length - 6f);
            for (int i = 0; i < tabs.Length; i++)
            {
                var r = new Rect(body.x + i * (tw + 6f), body.y, tw, 40f);
                if (Button(r, tabs[i], (int)bookTab == i ? sTabOn : sTab)) { bookTab = (BookTab)i; scroll = scroll2 = Vector2.zero; }
            }
            var content = new Rect(body.x, body.y + 50f, body.width, body.height - 50f);

            switch (bookTab)
            {
                case BookTab.Plantas: DrawBookPlants(content); break;
                case BookTab.Males: DrawBookAilments(content); break;
                case BookTab.Receitas: DrawBookRecipes(content); break;
                case BookTab.Assombracoes: DrawBookSpirits(content); break;
            }
        }

        // ───────── Plantas ─────────

        void DrawBookPlants(Rect area)
        {
            var plants = S.Db.Plants;
            var left = new Rect(area.x, area.y, Mathf.Min(260f, area.width * 0.32f), area.height);
            var right = new Rect(left.xMax + 14f, area.y, area.width - left.width - 14f, area.height);

            GUILayout.BeginArea(left);
            GUILayout.Label("Herbário: " + S.Herbarium.Count + "/" + plants.Count, sSection);
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < plants.Count; i++)
            {
                bool known = S.Herbarium.IsDiscovered(plants[i].id);
                if (Button(known ? plants[i].nome : "???", i == selPlant ? sListItemOn : sListItem, GUILayout.Height(38)))
                {
                    selPlant = i;
                    scroll2 = Vector2.zero;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            if (plants.Count == 0) return;
            var p = plants[Mathf.Clamp(selPlant, 0, plants.Count - 1)];
            bool found = S.Herbarium.IsDiscovered(p.id);

            GUILayout.BeginArea(right);
            scroll2 = GUILayout.BeginScrollView(scroll2);
            GUILayout.BeginHorizontal();
            var iconRect = GUILayoutUtility.GetRect(120, 120, GUILayout.Width(120), GUILayout.Height(120));
            if (found) DrawPlantIcon(iconRect, p.id, 4);
            else GUI.Label(iconRect, "?", Label(64, PergaminhoEscuro, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUILayout.BeginVertical();
            GUILayout.Label(found ? p.nome : "Página em branco", sTitle);
            if (found)
            {
                GUILayout.Label(p.nomeCientifico, sTextItalic);
                GUILayout.Label(Categoria(p) + (p.nativa ? "  ·  nativa do Brasil" : ""), sSubtitle);
                GUILayout.Label("Cultivo e coleta a partir do nível " + p.NivelMinimo, sTextItalic);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (!found)
            {
                GUILayout.Space(10);
                GUILayout.Label(p.selvagem
                    ? "Dica: esta planta cresce na mata. Explore e colete para desbloquear a página."
                    : "Dica: compre a semente na feira, plante, regue e colha para desbloquear a página.", sText);
            }
            else
            {
                Section("No jogo", p.efeitoNoJogo);
                Section("Benefícios tradicionais", p.beneficios);
                Section("Sobre a planta", p.descricao);
                Section("Como preparar", (string.IsNullOrEmpty(p.remedio) ? "" : p.remedio + ": ") + p.preparo + (string.IsNullOrEmpty(p.parteUsada) ? "" : "\nParte usada: " + p.parteUsada));
                Section("Cultura popular", p.culturaPopular);
                Section("Origem e bioma", p.origem + (string.IsNullOrEmpty(p.bioma) ? "" : " — " + p.bioma));
                GUILayout.Space(6);
                GUILayout.Label("Cuidados", sSection);
                GUILayout.Label(p.cuidados, sWarn);
                var usadas = S.Db.Recipes.Where(r => r.ingredientes != null && r.ingredientes.Any(i => i.plantId == p.id) && !r.id.StartsWith(GameDatabase.SimpleRemedyPrefix, System.StringComparison.Ordinal)).Select(r => r.nome).ToArray();
                if (usadas.Length > 0) Section("Entra nas receitas", string.Join(", ", usadas));
            }
            GUILayout.Space(14);
            GUILayout.Label(Aviso, sTextItalic);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static string Categoria(PlantData p)
        {
            switch (p.categoria)
            {
                case "Espiritual": return "Espiritual / proteção";
                case "Energetica": return "Energética";
                default: return "Medicinal";
            }
        }

        void Section(string title, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            GUILayout.Space(6);
            GUILayout.Label(title, sSection);
            GUILayout.Label(text, sText);
        }

        // ───────── Males ─────────

        void DrawBookAilments(Rect area)
        {
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var info in Ailments.All)
            {
                bool ativo = S.Status.Has(info.Tipo);
                GUILayout.BeginVertical(sCard);
                GUILayout.Label((ativo ? "► " : "") + info.Nome + (info.Kind == AilmentKind.Espiritual ? "  (espiritual)" : info.Kind == AilmentKind.Mental ? "  (mente)" : ""), ativo ? sWarn : sSection);
                GUILayout.Label(info.Descricao + "  Efeito: " + info.Penalidade + (info.DosesParaCurar > 1 ? "  ·  precisa de " + info.DosesParaCurar + " doses." : ""), sText);
                var cures = S.Db.RecipesThatCure(info.Tipo)
                    .Select(r => KnownRecipe(r) ? r.nome : "???")
                    .Distinct().ToArray();
                GUILayout.Label("O que cura: " + string.Join(", ", cures), sTextItalic);
                GUILayout.EndVertical();
            }
            GUILayout.Label(Aviso, sTextItalic);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Receita "conhecida": todas as plantas dela já foram descobertas.</summary>
        bool KnownRecipe(RecipeData r)
        {
            return r.ingredientes != null && r.ingredientes.All(i => S.Herbarium.IsDiscovered(i.plantId));
        }

        // ───────── Receitas ─────────

        void DrawBookRecipes(Rect area)
        {
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Fresca → Varal (seca) → Pilão (pó) → Caldeirão", sSubtitle);
            foreach (var r in S.Db.Recipes.Where(x => !x.id.StartsWith(GameDatabase.SimpleRemedyPrefix, System.StringComparison.Ordinal)))
            {
                GUILayout.BeginVertical(sCard);
                GUILayout.Label(r.nome + "  ·  " + TipoNome(r.tipo), sSection); // "Defumação", não "Defumacao" do JSON
                GUILayout.Label(r.descricao, sText);
                GUILayout.Label("Ingredientes: " + string.Join(" + ", r.ingredientes.Select(i => KnownPlant(i.plantId) ? IngredientText(i, false) : i.qtd + "x ???").ToArray()), sText);
                GUILayout.Label(Describe(r) + (r.chanceQueimadura > 0 ? "  ·  cuidado: o caldeirão queima!" : ""), sTextItalic);
                GUILayout.EndVertical();
            }
            GUILayout.Label("Preparos simples (chás, banhos, defumações de uma erva) aparecem no Caldeirão quando você descobre a planta.", sTextItalic);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        bool KnownPlant(string id) { return S.Herbarium.IsDiscovered(id); }

        // ───────── Assombrações ─────────

        void DrawBookSpirits(Rect area)
        {
            var all = SpiritCodex.All.ToList();
            var left = new Rect(area.x, area.y, Mathf.Min(240f, area.width * 0.3f), area.height);
            var right = new Rect(left.xMax + 14f, area.y, area.width - left.width - 14f, area.height);

            GUILayout.BeginArea(left);
            GUILayout.Label("Entidades da noite", sSection);
            for (int i = 0; i < all.Count; i++)
            {
                GUILayout.BeginHorizontal();
                var ir = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
                SpriteFactory.DrawSprite(ir, ArtLibrary.Get(Spirit.ArtKey(all[i].Tipo) + "_0"));
                if (Button(all[i].Nome, i == selSpirit ? sListItemOn : sListItem, GUILayout.Height(40))) { selSpirit = i; scroll2 = Vector2.zero; }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }
            GUILayout.Space(10);
            GUILayout.Label("Aqui ninguém luta: as assombrações são afastadas com plantas, amuletos, banhos e defumação.", sTextItalic);
            GUILayout.EndArea();

            var l = all[Mathf.Clamp(selSpirit, 0, all.Count - 1)];
            GUILayout.BeginArea(right);
            scroll2 = GUILayout.BeginScrollView(scroll2);
            GUILayout.BeginHorizontal();
            var pr = GUILayoutUtility.GetRect(140, 140, GUILayout.Width(140), GUILayout.Height(140));
            GUI.Box(pr, GUIContent.none, sPortrait);
            SpriteFactory.DrawSprite(new Rect(pr.x + 8, pr.y + 8, pr.width - 16, pr.height - 16), ArtLibrary.Get(Spirit.ArtKey(l.Tipo) + "_" + ((int)(Time.unscaledTime * 2f) % 2)));
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label(l.Nome, sTitle);
            GUILayout.Label("Também chamada(o): " + l.Apelidos, sTextItalic);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            Section("De onde vem", l.Origem);
            Section("O que simboliza", l.Significado);
            Section("No jogo", l.NoJogo);
            Section("Como se proteger", l.Protecao);
            var plantas = l.PlantasQueProtegem.Select(id => S.Db.Plant(id)).Where(p => p != null)
                .Select(p => KnownPlant(p.id) ? p.nome : "???").ToArray();
            if (plantas.Length > 0) Section("Plantas aliadas", string.Join(", ", plantas));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
