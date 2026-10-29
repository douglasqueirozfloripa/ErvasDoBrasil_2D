using System;
using System.Collections.Generic;

namespace ErvasDoBrasil.Core
{
    public enum AuraType { Nenhuma, Barreira, AntiOlhoGordo, AntiSaci }

    /// <summary>Efeito principal de um produto do caldeirão.</summary>
    public enum ItemEffect
    {
        Cura,     // chás, garrafadas, pomadas: curam males por dose
        Amuleto,  // protege a aura e espanta espíritos por N dias
        Banho,    // limpa o espiritual de uma vez e dá sorte
        Banir,    // defumação: bane espíritos num raio
        Revelar,  // arruda: revela visagens ocultas por um tempo
        Sorte     // prosperidade: vende melhor e colhe mais
    }

    /// <summary>Forma em que a erva entra numa receita.</summary>
    public enum HerbForm { Fresca, Seca, Po }

    /// <summary>
    /// Bônus de atributos aplicados ao usar um produto. Compartilhado por plantas (preparo simples) e receitas.
    /// </summary>
    [Serializable]
    public class StatBonus
    {
        public float energiaPct;     // 0.3 = recupera 30% da energia máxima
        public float sanidade;       // pontos (0-100)
        public float aura;           // pontos (0-100)
        public float velocidadePct;  // 0.5 = +50% de velocidade
        public float segundos;       // duração do buff de velocidade/revelar
    }

    /// <summary>Uma planta do herbário. Campos públicos em minúsculo para casar com o JSON (JsonUtility).</summary>
    [Serializable]
    public class PlantData
    {
        public string id;
        public string nome;
        public string nomeCientifico;
        public string categoria;        // "Medicinal" | "Espiritual" | "Energetica"
        public string[] cura;           // males que o preparo simples cura
        public string remedio;          // nome do preparo simples (ex.: "Chá de boldo")
        public string efeitoRemedio;    // ItemEffect do preparo simples
        public StatBonus bonus;         // bônus do preparo simples
        public string efeitoNoJogo;     // texto do Livro
        public string parteUsada;
        public string preparo;
        public string descricao;
        public string beneficios;
        public string culturaPopular;
        public string cuidados;
        public string origem;
        public string bioma;
        public string cor;              // hex "#RRGGBB" (placeholder enquanto não há sprite)
        public string aura;             // AuraType quando plantada e madura
        public float raioAura;
        public float tempoCrescimento;  // segundos de jogo regados
        public int precoSemente;
        public int precoVenda;
        public bool selvagem;           // nasce na mata
        public bool nativa;             // nativa do Brasil
        public int nivel;               // nível de raizeira para comprar a semente, plantar e coletar (0 ou 1 = desde o início)

        public IEnumerable<Ailment> Cures { get { return ParseAilments(cura); } }
        public int NivelMinimo { get { return Math.Max(1, nivel); } }

        public AuraType Aura
        {
            get
            {
                AuraType t;
                return !string.IsNullOrEmpty(aura) && Enum.TryParse(aura, true, out t) ? t : AuraType.Nenhuma;
            }
        }

        public bool TemAura { get { return Aura != AuraType.Nenhuma && raioAura > 0f; } }
        public bool TemRemedio { get { return !string.IsNullOrEmpty(remedio); } }

        internal static IEnumerable<Ailment> ParseAilments(string[] list)
        {
            if (list == null) yield break;
            foreach (var s in list)
            {
                Ailment a;
                if (Ailments.TryParse(s, out a)) yield return a;
            }
        }
    }

    [Serializable]
    public class Ingredient
    {
        public string plantId;
        public int qtd = 1;
        public string forma;   // "Fresca" (padrão) | "Seca" | "Po"

        public Ingredient() { }

        public Ingredient(string plantId, int qtd, HerbForm form = HerbForm.Fresca)
        {
            this.plantId = plantId;
            this.qtd = qtd;
            forma = form.ToString();
        }

        public HerbForm Form
        {
            get
            {
                HerbForm f;
                return !string.IsNullOrEmpty(forma) && Enum.TryParse(forma, true, out f) ? f : HerbForm.Fresca;
            }
        }
    }

    /// <summary>Receita do caldeirão: chás, garrafadas, elixires, pomadas, banhos, amuletos, defumações.</summary>
    [Serializable]
    public class RecipeData
    {
        public string id;
        public string nome;
        public string tipo;            // Cha, Garrafada, Elixir, Pomada, Banho, Amuleto, Defumacao
        public string descricao;
        public string efeito;          // ItemEffect
        public string[] cura;          // males curados
        public Ingredient[] ingredientes;
        public StatBonus bonus;
        public int precoVenda;
        public int doses = 1;          // doses de cura aplicadas por uso
        public int duracaoDias = 1;    // amuleto / sorte
        public float raio;             // defumação
        public float chanceQueimadura; // risco ao preparar

        public ItemEffect Effect
        {
            get
            {
                ItemEffect e;
                return !string.IsNullOrEmpty(efeito) && Enum.TryParse(efeito, true, out e) ? e : ItemEffect.Cura;
            }
        }

        public IEnumerable<Ailment> Cures { get { return PlantData.ParseAilments(cura); } }
    }

    [Serializable]
    public class PlantCatalog { public PlantData[] plantas; }

    [Serializable]
    public class RecipeCatalog { public RecipeData[] receitas; }
}
