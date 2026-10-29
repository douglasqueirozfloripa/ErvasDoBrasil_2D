using System.Collections.Generic;

namespace ErvasDoBrasil.Core
{
    /// <summary>Ficha de uma entidade para a seção "Assombrações" do Livro.</summary>
    public sealed class SpiritLore
    {
        public SpiritType Tipo;
        public string Nome;
        public string Apelidos;
        public string Origem;      // de onde vem a crença
        public string Significado; // o que ela simboliza
        public string NoJogo;      // comportamento e efeitos
        public string Protecao;    // como se proteger
        public string[] PlantasQueProtegem;
    }

    public static class SpiritCodex
    {
        static readonly Dictionary<SpiritType, SpiritLore> lore = new Dictionary<SpiritType, SpiritLore>
        {
            {
                SpiritType.Assombracao, new SpiritLore
                {
                    Tipo = SpiritType.Assombracao,
                    Nome = "Alma da Lamparina",
                    Apelidos = "alma penada, alma do candeeiro",
                    Origem = "A lamparina é a luminária antiga e portátil, de óleo ou querosene, que ilumina as casas do interior com uma luz amarelada, suave e trêmula. Muita história de assombração começa com uma luzinha andando sozinha no escuro.",
                    Significado = "\"Alma de lamparina\" tem dois sentidos. Resiliência: uma alma que, mesmo na escuridão total, mantém acesa uma pequena chama de esperança, fé ou sabedoria — luz discreta, mas constante, que guia o caminho no meio do mistério. E fragilidade: uma existência antiga e humilde, que brilha oscilante como o pavio de um candeeiro à mercê do vento.",
                    NoJogo = "Vaga à noite procurando o caminho de volta. Não é má, mas quando te atravessa leva um pouco do seu calor: -20 de energia, -15 de aura e -10 de sanidade. Com a aura fraca, ela gruda em você (Assombrado) e passa a aparecer até de dia.",
                    Protecao = "O amuleto de arruda faz ela se afastar. Espada-de-São-Jorge e Comigo-ninguém-pode madura criam barreira que ela não atravessa. Defumação de guiné a bane, e o Banho de Descarrego tira o \"assombrado\".",
                    PlantasQueProtegem = new[] { "arruda", "espada_sao_jorge", "comigo_ninguem_pode", "guine" }
                }
            },
            {
                SpiritType.Visagem, new SpiritLore
                {
                    Tipo = SpiritType.Visagem,
                    Nome = "Visagem da Neblina",
                    Apelidos = "visagem, visage, aparição",
                    Origem = "No Norte e no Nordeste do Brasil, \"visagem\" é o nome popular para aparição, fantasma, espírito ou assombração. A neblina — a névoa que nubla a visão — dá ao lugar uma atmosfera de mistério.",
                    Significado = "É a silhueta que parece tomar forma no meio da névoa densa. Simboliza o incerto, aquilo que não conseguimos enxergar com clareza, os segredos e os medos que aparecem quando o caminho à frente está encoberto.",
                    NoJogo = "É invisível a olho nu. Quando chega perto, a sanidade vai caindo sem você entender o porquê — e sanidade baixa traz ansiedade e a tela começa a tremer.",
                    Protecao = "O raminho de arruda revela as visagens por um tempo. O amuleto as mantém longe, a barreira da Espada-de-São-Jorge segura, e a defumação de guiné as dissipa. Chá de camomila devolve a calma.",
                    PlantasQueProtegem = new[] { "arruda", "espada_sao_jorge", "guine", "camomila" }
                }
            },
            {
                SpiritType.Saci, new SpiritLore
                {
                    Tipo = SpiritType.Saci,
                    Nome = "Saci-Pererê",
                    Apelidos = "Saci, Pererê",
                    Origem = "Um dos personagens mais famosos do folclore brasileiro, com raízes em tradições indígenas, africanas e europeias. É um menino de uma perna só, de gorro vermelho (de onde viria seu poder) e cachimbo, que se esconde e viaja dentro dos redemoinhos de vento. Popularizado por Monteiro Lobato; 31 de outubro é celebrado como Dia do Saci.",
                    Significado = "É o espírito da travessura: some com objetos, dá nó na crina dos cavalos, azeda o leite, apaga o fogo. Não é maldoso — é brincalhão e imprevisível, como o vento que vira tudo de lugar.",
                    NoJogo = "Rápido e saltitante, rouba um item aleatório da sua bolsa e some num redemoinho. Se você estiver com fissura, o cheiro do cachimbo dele mexe com a sua sanidade.",
                    Protecao = "Amuleto não adianta: o Saci só respeita a guiné. Plante guiné (aura anti-Saci) perto da horta, ou use a barreira da Espada-de-São-Jorge. Uma defumação também o espanta.",
                    PlantasQueProtegem = new[] { "guine", "espada_sao_jorge", "comigo_ninguem_pode" }
                }
            },
            {
                SpiritType.OlhoGordo, new SpiritLore
                {
                    Tipo = SpiritType.OlhoGordo,
                    Nome = "Olho-Gordo",
                    Apelidos = "mau-olhado, olhado, quebranto, uruca",
                    Origem = "Crença popular, presente no Brasil e em muitas culturas, de que a inveja e o olhar cobiçoso trazem azar, cansaço, doença e fazem planta murchar. Benzedeiras \"tiram o olhado\" rezando com um raminho de arruda, que murcha quando o olhado é forte.",
                    Significado = "Representa a inveja e o peso do julgamento dos outros. No jogo virou criatura: um olho verde de inveja, de pálpebra roxa e cílios de espinho.",
                    NoJogo = "Fica de longe e lança mau-olhado (uruca): colheita pela metade, vendas 20% mais baratas e a aura para de se recuperar. Quem tem aura forte (70+) resiste.",
                    Protecao = "Plante pimenteira: dentro da aura dela o olhado não pega. Amuleto de arruda protege por dias; o raminho de arruda e o Banho de Descarrego limpam a uruca.",
                    PlantasQueProtegem = new[] { "pimenteira", "arruda", "alecrim" }
                }
            },
            {
                SpiritType.Cuca, new SpiritLore
                {
                    Tipo = SpiritType.Cuca,
                    Nome = "Cuca",
                    Apelidos = "Coca, a bruxa-jacaré",
                    Origem = "Vem da \"Coca\" das tradições ibéricas, uma papona que assustava as crianças, e chegou ao Brasil nas cantigas de ninar: \"Nana, neném, que a Cuca vem pegar...\". Monteiro Lobato a pôs no Sítio do Picapau Amarelo como uma bruxa velha com cabeça de jacaré, e foi assim que ela ficou famosa.",
                    Significado = "É o medo que manda criança para a cama. Por trás do susto está um cuidado antigo: a noite é para descansar, e quem não dorme fica fraco, irritado e desprotegido.",
                    NoJogo = "Aparece de madrugada (das 22h às 4h), a partir do dia 5, e vai atrás de quem ainda está acordada. Quando te alcança, joga o sono atrasado de uma vez: -25 de energia, -10 de sanidade e Cansaço.",
                    Protecao = "Durma antes das 22h: na cama da Casa da Vó ela não entra. O amuleto de arruda a mantém longe, a barreira da Espada-de-São-Jorge segura, e a defumação a espanta. Chá de camomila ou de erva-cidreira ajuda o sono a vir.",
                    PlantasQueProtegem = new[] { "arruda", "espada_sao_jorge", "camomila", "erva_cidreira" }
                }
            },
            {
                SpiritType.Bernuncia, new SpiritLore
                {
                    Tipo = SpiritType.Bernuncia,
                    Nome = "Bernúncia",
                    Apelidos = "Bernunça, a bicharada do Boi-de-Mamão",
                    Origem = "Personagem do Boi-de-Mamão, a brincadeira de rua do litoral de Santa Catarina e de Florianópolis, que veio com os açorianos. É um bicho fantástico de pano, de bocarra enorme, levado por gente escondida embaixo do tecido. Na roda, ela \"engole\" as crianças da plateia e depois as devolve, entre gritos e risadas.",
                    Significado = "É o susto de brincadeira, feito em comunidade: todo mundo corre, todo mundo ri e ninguém se machuca. Lembra que o folclore da Ilha está vivo nas festas e nas ruas.",
                    NoJogo = "Aparece à noite, a partir do dia 5, com uma fome danada. Vai direto para a horta e engole um canteiro plantado (a terra continua preparada). Se te alcança, te engole e te cospe na porta da Casa da Vó (-10 de energia). Satisfeita, vai embora.",
                    Protecao = "Amuleto não adianta. Plante Espada-de-São-Jorge ou Comigo-ninguém-pode perto da horta: a barreira segura a Bernúncia longe dos canteiros. A defumação também a espanta, e ao amanhecer ela some.",
                    PlantasQueProtegem = new[] { "espada_sao_jorge", "comigo_ninguem_pode", "guine" }
                }
            },
        };

        public static SpiritLore Get(SpiritType t)
        {
            SpiritLore l;
            return lore.TryGetValue(t, out l) ? l : null;
        }

        public static IEnumerable<SpiritLore> All { get { return lore.Values; } }
    }
}
