using System;
using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    /// <summary>Catálogo de plantas e receitas. Gera automaticamente o preparo simples de cada planta.</summary>
    public class GameDatabase
    {
        public const string SimpleRemedyPrefix = "remedio_";

        readonly Dictionary<string, PlantData> plants = new Dictionary<string, PlantData>();
        readonly Dictionary<string, RecipeData> recipes = new Dictionary<string, RecipeData>();
        readonly List<PlantData> plantList = new List<PlantData>();
        readonly List<RecipeData> recipeList = new List<RecipeData>();
        readonly List<string> loadErrors = new List<string>();

        public GameDatabase(IEnumerable<PlantData> plantas, IEnumerable<RecipeData> receitas)
        {
            if (plantas != null)
            {
                foreach (var p in plantas)
                {
                    if (p == null || string.IsNullOrEmpty(p.id)) { loadErrors.Add("Planta sem id"); continue; }
                    if (plants.ContainsKey(p.id)) { loadErrors.Add("Planta duplicada: " + p.id); continue; }
                    plants[p.id] = p;
                    plantList.Add(p);
                }
            }

            if (receitas != null)
            {
                foreach (var r in receitas)
                {
                    if (r == null || string.IsNullOrEmpty(r.id)) { loadErrors.Add("Receita sem id"); continue; }
                    if (recipes.ContainsKey(r.id)) { loadErrors.Add("Receita duplicada: " + r.id); continue; }
                    recipes[r.id] = r;
                    recipeList.Add(r);
                }
            }

            foreach (var p in plantList)
            {
                if (!p.TemRemedio) continue;
                var simple = SimpleRemedyFor(p);
                if (recipes.ContainsKey(simple.id)) continue;
                recipes[simple.id] = simple;
                recipeList.Insert(0, simple);
            }
        }

        public IList<PlantData> Plants { get { return plantList.AsReadOnly(); } }
        public IList<RecipeData> Recipes { get { return recipeList.AsReadOnly(); } }

        public PlantData Plant(string id)
        {
            PlantData p;
            return id != null && plants.TryGetValue(id, out p) ? p : null;
        }

        public RecipeData Recipe(string id)
        {
            RecipeData r;
            return id != null && recipes.TryGetValue(id, out r) ? r : null;
        }

        public IEnumerable<PlantData> PlantsThatCure(Ailment a)
        {
            return plantList.Where(p => p.Cures.Contains(a));
        }

        public IEnumerable<RecipeData> RecipesThatCure(Ailment a)
        {
            return recipeList.Where(r => r.Cures.Contains(a));
        }

        public static RecipeData SimpleRemedyFor(PlantData p)
        {
            ItemEffect effect;
            if (string.IsNullOrEmpty(p.efeitoRemedio) || !Enum.TryParse(p.efeitoRemedio, true, out effect))
                effect = ItemEffect.Cura;

            var cura = p.Cures.Select(a => a.ToString()).ToArray();
            return new RecipeData
            {
                id = SimpleRemedyPrefix + p.id,
                nome = p.remedio,
                tipo = effect == ItemEffect.Cura ? "Cha" : effect.ToString(),
                bonus = p.bonus,
                descricao = p.preparo,
                efeito = effect.ToString(),
                cura = cura,
                ingredientes = new[] { new Ingredient(p.id, 1, HerbForm.FrescaOuSeca) }, // chá se faz com a erva fresca ou seca
                precoVenda = p.precoVenda * 2 + 2,
                doses = 1,
                duracaoDias = 1,
                raio = effect == ItemEffect.Banir ? 5f : 0f,
                chanceQueimadura = 0f
            };
        }

        /// <summary>Checa a integridade dos dados. Lista vazia = tudo certo.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>(loadErrors);

            foreach (var p in plantList)
            {
                if (string.IsNullOrEmpty(p.nome)) errors.Add(p.id + ": sem nome");
                if (string.IsNullOrEmpty(p.descricao)) errors.Add(p.id + ": sem descrição para o Livro");
                if (string.IsNullOrEmpty(p.beneficios)) errors.Add(p.id + ": sem benefícios para o Livro");
                if (p.tempoCrescimento <= 0) errors.Add(p.id + ": tempoCrescimento deve ser > 0");
                if (p.precoSemente <= 0 || p.precoVenda <= 0) errors.Add(p.id + ": preços devem ser > 0");
                if (p.cura != null)
                    foreach (var c in p.cura)
                        if (!Ailments.TryParse(c, out _)) errors.Add(p.id + ": mal desconhecido '" + c + "'");
                if (!p.TemRemedio && !p.TemAura) errors.Add(p.id + ": planta sem uso (nem remédio nem aura)");
                if (!string.IsNullOrEmpty(p.aura) && p.Aura == AuraType.Nenhuma) errors.Add(p.id + ": aura desconhecida '" + p.aura + "'");
                if (p.Aura != AuraType.Nenhuma && p.raioAura <= 0) errors.Add(p.id + ": aura sem raio");
            }

            foreach (var r in recipeList)
            {
                if (r.ingredientes == null || r.ingredientes.Length == 0) errors.Add(r.id + ": receita sem ingredientes");
                else
                {
                    foreach (var ing in r.ingredientes)
                    {
                        if (Plant(ing.plantId) == null) errors.Add(r.id + ": ingrediente inexistente '" + ing.plantId + "'");
                        if (ing.qtd <= 0) errors.Add(r.id + ": quantidade inválida de " + ing.plantId);
                        if (!string.IsNullOrEmpty(ing.forma) && !Enum.TryParse<HerbForm>(ing.forma, true, out _)) errors.Add(r.id + ": forma desconhecida '" + ing.forma + "'");
                    }
                }
                if (r.cura != null)
                    foreach (var s in r.cura)
                        if (!Ailments.TryParse(s, out _)) errors.Add(r.id + ": mal desconhecido '" + s + "'");
                if (!string.IsNullOrEmpty(r.efeito) && !Enum.TryParse<ItemEffect>(r.efeito, true, out _)) errors.Add(r.id + ": efeito desconhecido '" + r.efeito + "'");
                if (r.Effect == ItemEffect.Banir && r.raio <= 0) errors.Add(r.id + ": defumação sem raio");
                if (r.precoVenda <= 0) errors.Add(r.id + ": preço de venda deve ser > 0");
            }

            foreach (var info in Ailments.All)
            {
                if (!RecipesThatCure(info.Tipo).Any())
                    errors.Add("Nenhuma receita cura " + info.Nome);
            }

            return errors;
        }
    }
}
