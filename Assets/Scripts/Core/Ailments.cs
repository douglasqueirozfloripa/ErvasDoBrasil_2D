using System;
using System.Collections.Generic;

namespace ErvasDoBrasil.Core
{
    /// <summary>Males do corpo e do espírito. Cada um tem uma penalidade de gameplay e ervas que curam.</summary>
    public enum Ailment
    {
        Nenhum = 0,
        DorDeBarriga,
        Enjoo,
        Ansiedade,
        VontadeDeFumar,
        Insonia,
        Tosse,
        Gripe,
        Azia,
        Queimadura,
        Cansaco,
        DorDeDente,
        Colica,
        DorRenal,
        MauOlhado,
        Assombrado
    }

    public enum AilmentKind { Fisico, Mental, Espiritual }

    public sealed class AilmentInfo
    {
        public readonly Ailment Tipo;
        public readonly string Nome;
        public readonly string Descricao;
        public readonly string Penalidade;
        public readonly int DosesParaCurar;
        public readonly AilmentKind Kind;

        public AilmentInfo(Ailment tipo, string nome, string descricao, string penalidade, AilmentKind kind, int doses = 1)
        {
            Tipo = tipo;
            Nome = nome;
            Descricao = descricao;
            Penalidade = penalidade;
            Kind = kind;
            DosesParaCurar = doses;
        }
    }

    public static class Ailments
    {
        static readonly Dictionary<Ailment, AilmentInfo> info = new Dictionary<Ailment, AilmentInfo>
        {
            { Ailment.DorDeBarriga, new AilmentInfo(Ailment.DorDeBarriga, "Dor de barriga", "Comeu pastel de feira demais.", "Velocidade -40%", AilmentKind.Fisico) },
            { Ailment.Enjoo, new AilmentInfo(Ailment.Enjoo, "Náusea", "Tudo gira um pouquinho.", "Velocidade -15% e passos tortos", AilmentKind.Fisico) },
            { Ailment.Ansiedade, new AilmentInfo(Ailment.Ansiedade, "Ansiedade", "O coração acelera e a cabeça não para.", "Sanidade cai 2x mais rápido; a tela treme", AilmentKind.Mental) },
            { Ailment.VontadeDeFumar, new AilmentInfo(Ailment.VontadeDeFumar, "Fissura (vontade de fumar)", "Você está largando o cigarro. As crises vêm em ondas.", "Crises periódicas drenam energia e sanidade", AilmentKind.Mental, 3) },
            { Ailment.Insonia, new AilmentInfo(Ailment.Insonia, "Insônia", "Rola na cama e o sono não vem.", "Dormir não recupera energia", AilmentKind.Mental) },
            { Ailment.Tosse, new AilmentInfo(Ailment.Tosse, "Tosse", "Cof, cof... dá pra ouvir lá da mata.", "Espíritos te percebem de 2x mais longe", AilmentKind.Fisico) },
            { Ailment.Gripe, new AilmentInfo(Ailment.Gripe, "Gripe", "Corpo mole e nariz entupido.", "Energia máxima cai para 60", AilmentKind.Fisico) },
            { Ailment.Azia, new AilmentInfo(Ailment.Azia, "Azia", "Queimação subindo pelo peito.", "Ações custam o dobro de energia", AilmentKind.Fisico) },
            { Ailment.Queimadura, new AilmentInfo(Ailment.Queimadura, "Queimadura", "Encostou no caldeirão quente.", "Não consegue usar o caldeirão", AilmentKind.Fisico) },
            { Ailment.Cansaco, new AilmentInfo(Ailment.Cansaco, "Cansaço", "Passou a noite em claro ou trabalhou demais.", "Velocidade -25%", AilmentKind.Fisico) },
            { Ailment.DorDeDente, new AilmentInfo(Ailment.DorDeDente, "Dor de dente", "Fica difícil até de conversar.", "Preço de venda na feira -30%", AilmentKind.Fisico) },
            { Ailment.Colica, new AilmentInfo(Ailment.Colica, "Cólica", "Aquela pontada que não passa.", "Velocidade -30%", AilmentKind.Fisico) },
            { Ailment.DorRenal, new AilmentInfo(Ailment.DorRenal, "Dor nos rins", "Pedra no caminho... e nos rins.", "Lerdeza severa: velocidade -50%", AilmentKind.Fisico) },
            { Ailment.MauOlhado, new AilmentInfo(Ailment.MauOlhado, "Mau-olhado (uruca)", "Alguém botou olho gordo em você.", "Colheita pela metade, vendas -20%, aura não regenera", AilmentKind.Espiritual) },
            { Ailment.Assombrado, new AilmentInfo(Ailment.Assombrado, "Assombrado", "Uma assombração grudou em você.", "Espíritos aparecem até de dia", AilmentKind.Espiritual) },
        };

        /// <summary>Males que podem aparecer ao amanhecer ou nos aldeões (os espirituais vêm das criaturas).</summary>
        public static readonly Ailment[] DailyPool =
        {
            Ailment.DorDeBarriga, Ailment.Enjoo, Ailment.Ansiedade, Ailment.Insonia, Ailment.Tosse, Ailment.Gripe,
            Ailment.Azia, Ailment.DorDeDente, Ailment.Colica, Ailment.DorRenal
        };

        public static AilmentInfo Info(Ailment a)
        {
            AilmentInfo i;
            return info.TryGetValue(a, out i) ? i : null;
        }

        public static string Nome(Ailment a)
        {
            var i = Info(a);
            return i != null ? i.Nome : a.ToString();
        }

        public static IEnumerable<AilmentInfo> All { get { return info.Values; } }

        public static bool TryParse(string s, out Ailment a)
        {
            a = Ailment.Nenhum;
            if (string.IsNullOrEmpty(s)) return false;
            return Enum.TryParse(s, true, out a) && a != Ailment.Nenhum && Enum.IsDefined(typeof(Ailment), a);
        }
    }
}
