using System.Collections.Generic;
using ErvasDoBrasil.Core;

namespace ErvasDoBrasil.Tests
{
    /// <summary>Aleatoriedade roteirizada: devolve os valores na ordem enfileirada.</summary>
    public class FixedRandom : IRandom
    {
        readonly Queue<float> values = new Queue<float>();
        readonly Queue<int> ints = new Queue<int>();
        public float DefaultValue = 0.99f; // por padrão, "nada de sorte/azar" (acima das chances)

        public FixedRandom Values(params float[] v) { foreach (var x in v) values.Enqueue(x); return this; }
        public FixedRandom Ints(params int[] v) { foreach (var x in v) ints.Enqueue(x); return this; }

        public float Value() { return values.Count > 0 ? values.Dequeue() : DefaultValue; }

        public int Range(int minInclusive, int maxExclusive)
        {
            int v = ints.Count > 0 ? ints.Dequeue() : minInclusive;
            if (v < minInclusive) v = minInclusive;
            if (v >= maxExclusive) v = maxExclusive - 1;
            return v;
        }
    }

    /// <summary>Banco pequeno e previsível para os testes (independente do JSON do jogo).</summary>
    public static class TestData
    {
        public static PlantData Plant(string id, string[] cura = null, string remedio = null, string efeito = "Cura",
            StatBonus bonus = null, string aura = "", float raio = 0f, float tempo = 10f, int semente = 3, int venda = 4)
        {
            return new PlantData
            {
                id = id,
                nome = id,
                nomeCientifico = id + " sp.",
                categoria = "Medicinal",
                cura = cura ?? System.Array.Empty<string>(),
                remedio = remedio,
                efeitoRemedio = efeito,
                bonus = bonus ?? new StatBonus(),
                descricao = "desc",
                beneficios = "benef",
                aura = aura,
                raioAura = raio,
                tempoCrescimento = tempo,
                precoSemente = semente,
                precoVenda = venda
            };
        }

        public static List<PlantData> Plants()
        {
            return new List<PlantData>
            {
                Plant("boldo", new[] { "DorDeBarriga", "Enjoo" }, "Chá de boldo", bonus: new StatBonus { energiaPct = 0.3f }),
                Plant("camomila", new[] { "Ansiedade" }, "Chá de camomila", bonus: new StatBonus { sanidade = 35f }),
                Plant("mulungu", new[] { "VontadeDeFumar" }, "Chá de mulungu"),
                Plant("espada_sao_jorge", aura: "Barreira", raio: 3f),
                Plant("arruda", new[] { "MauOlhado" }, "Raminho de arruda", "Revelar", new StatBonus { aura = 40f, segundos = 60f }),
                Plant("guine", new[] { "Assombrado" }, "Defumação de guiné", "Banir", aura: "AntiSaci", raio: 3.5f),
                Plant("guarana", new[] { "Cansaco" }, "Guaraná", bonus: new StatBonus { velocidadePct = 0.5f, segundos = 90f }),
            };
        }

        public static List<RecipeData> Recipes()
        {
            return new List<RecipeData>
            {
                new RecipeData
                {
                    id = "elixir_recomeco", nome = "Elixir do Recomeço", tipo = "Elixir", efeito = "Cura",
                    cura = new[] { "VontadeDeFumar", "Ansiedade" },
                    ingredientes = new[] { new Ingredient("mulungu", 2, HerbForm.Po), new Ingredient("camomila", 1) },
                    precoVenda = 40, doses = 2, chanceQueimadura = 0.5f, bonus = new StatBonus { sanidade = 30f }
                },
                new RecipeData
                {
                    id = "banho_descarrego", nome = "Banho de Descarrego", tipo = "Banho", efeito = "Banho",
                    cura = new[] { "MauOlhado", "Assombrado" },
                    ingredientes = new[] { new Ingredient("arruda", 1), new Ingredient("guine", 1) },
                    precoVenda = 30
                },
                new RecipeData
                {
                    id = "amuleto_arruda", nome = "Amuleto", tipo = "Amuleto", efeito = "Amuleto",
                    cura = System.Array.Empty<string>(),
                    ingredientes = new[] { new Ingredient("arruda", 2) },
                    precoVenda = 30, duracaoDias = 2
                },
            };
        }

        public static GameDatabase Db() { return new GameDatabase(Plants(), Recipes()); }
    }
}
