using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Feira, aldeões, menu de plantio, introdução e vitória.</summary>
    public partial class GameUI
    {
        /// <summary>No canteiro cavado, o botão das sementes que não são de proteção (desabilitado).</summary>
        public const string BotaoSoNaHorta = "Só na horta";

        FarmPlot plotToPlant;
        Villager villager;
        bool feiraComprar;

        public void OpenPlantMenu(FarmPlot plot)
        {
            plotToPlant = plot;
            Open(Window.Plantar);
        }

        public void OpenVillager(Villager v)
        {
            villager = v;
            Open(Window.Aldeao);
        }

        void DrawPlantar(Rect body)
        {
            GUILayout.BeginArea(body);
            scroll = GUILayout.BeginScrollView(scroll);
            var seeds = S.Inventory.OfKind(ItemKind.Semente).ToList();
            bool cavado = plotToPlant != null && plotToPlant.Model.Cavado;
            if (cavado)
                GUILayout.Label("Canteiro cavado com a pá: é para as plantas de proteção, que fazem efeito na área (" + S.PlantasDeProtecao + "). As outras vão na horta da vó.", sTextItalic);
            if (seeds.Count == 0) GUILayout.Label("Você não tem sementes. Compre na feira ou colete na mata.", sText);
            float textWidth = CardTextWidth(body, 48f + 150f + 120f);
            foreach (var kv in seeds)
            {
                ItemKind k; string id;
                if (!Inventory.TryParseKey(kv.Key, out k, out id)) continue;
                var p = S.Db.Plant(id);
                GUILayout.BeginHorizontal(sCard);
                var ir = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                DrawItemIcon(ir, kv.Key);
                GUILayout.BeginVertical(GUILayout.Width(textWidth));
                // o mesmo nome da Bolsa: antes as sementes ainda fora do Livro viravam "Semente misteriosa" aqui,
                // e no início as quatro do kit apareciam iguais, sem dizer qual ia para a terra
                GUILayout.Label(S.DisplayName(kv.Key) + "  x" + kv.Value, sSection);
                GUILayout.Label("Cresce em ~" + Mathf.RoundToInt(p.tempoCrescimento) + "s se regada todo dia." + (p.TemAura ? " Madura, protege a área!" : ""), sTextItalic);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                bool liberada = S.Progresso.PodeCultivar(p);
                bool cabe = plotToPlant == null || plotToPlant.Model.Aceita(p);
                string botao = "Plantar";
                if (!cabe)
                {
                    GUILayout.Label("Esta vai na horta da vó.", sTextItalic, GUILayout.Width(150));
                    botao = BotaoSoNaHorta;
                }
                else if (!liberada)
                {
                    GUILayout.Label("Você ainda não sabe cultivar esta.", sTextItalic, GUILayout.Width(150));
                    botao = "Nível " + p.NivelMinimo;
                }
                if (ListButton(botao, liberada && cabe, 120f))
                {
                    var plot = plotToPlant;
                    Defer(() => { if (plot != null && S.PlantSeed(plot.Model, id)) Close(); });
                }
                GUILayout.EndHorizontal();
            }
            if (plotToPlant != null && plotToPlant.Model.Cavado)
            {
                // canteiro cavado com a pá e ainda vazio: dá para fechar de volta
                GUILayout.Space(8);
                var plot = plotToPlant;
                if (ListButton("Desfazer este canteiro", true, 240f)) Defer(() => { if (S.FillPlot(plot.Model)) Close(); });
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        bool sellAllArmed; // "Vender tudo" esperando a confirmação

        void DrawFeira(Rect body)
        {
            GUILayout.BeginArea(body);
            GUILayout.BeginHorizontal();
            if (Button("Vender", !feiraComprar ? sTabOn : sTab, GUILayout.Width(160), GUILayout.Height(40))) { feiraComprar = false; sellAllArmed = false; scroll = Vector2.zero; }
            if (Button("Comprar", feiraComprar ? sTabOn : sTab, GUILayout.Width(160), GUILayout.Height(40))) { feiraComprar = true; sellAllArmed = false; scroll = Vector2.zero; }
            GUILayout.FlexibleSpace();
            GUILayout.Label(S.Money + " moedas", sSubtitle);
            GUILayout.EndHorizontal();

            float mult = S.Status.SellPriceMultiplier;
            if (mult < 0.99f) GUILayout.Label("Hoje você não está negociando bem (" + Mathf.RoundToInt(mult * 100) + "% do preço).", sWarn);
            else if (mult > 1.01f) GUILayout.Label("A sorte está do seu lado: preços melhores hoje!", sSubtitle);

            if (!feiraComprar && S.SellAllValue > 0)
            {
                // tudo de uma vez, sem clicar item por item (pede confirmação: vende também remédios e sementes)
                GUILayout.BeginHorizontal(sCard);
                GUILayout.Label(sellAllArmed
                    ? "Vender toda a bolsa, inclusive remédios e sementes, por " + S.SellAllValue + " moedas?"
                    : "Tudo o que está na bolsa vale " + S.SellAllValue + " moedas hoje.", sellAllArmed ? sWarn : sText);
                GUILayout.FlexibleSpace();
                if (sellAllArmed)
                {
                    if (ListButton("Confirmar venda", true, 180f)) Defer(() => { S.SellAll(); sellAllArmed = false; });
                    if (ListButton("Cancelar", true, 120f)) Defer(() => sellAllArmed = false);
                }
                else if (ListButton("Vender tudo", true, 150f)) Defer(() => sellAllArmed = true);
                GUILayout.EndHorizontal();
            }

            scroll = GUILayout.BeginScrollView(scroll);
            if (!feiraComprar)
            {
                var items = S.Inventory.All.ToList();
                if (items.Count == 0) GUILayout.Label("Nada para vender.", sText);
                foreach (var kv in items)
                {
                    string key = kv.Key;
                    int price = S.SellPrice(key);
                    int qty = kv.Value;
                    GUILayout.BeginHorizontal(sCard);
                    var ir = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
                    DrawItemIcon(ir, key);
                    GUILayout.Label(S.DisplayName(key) + "  x" + qty + "   (" + price + " cada)", sSection);
                    GUILayout.FlexibleSpace();
                    if (ListButton("Vender 1", price > 0, 120f)) Defer(() => S.Sell(key, 1));
                    if (ListButton("Vender todos", price > 0 && qty > 1, 150f)) Defer(() => S.Sell(key, qty));
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.BeginHorizontal(sCard);
                var pa = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
                SpriteFactory.DrawSprite(pa, ArtLibrary.Get("icon_pa"));
                GUILayout.BeginVertical();
                GUILayout.Label("Pá de cavar   " + GameSession.PrecoPa + " moedas", sSection);
                GUILayout.Label("Cava canteiros em qualquer canto do sítio para as plantas de proteção (" + S.PlantasDeProtecao + "): pare num lugar livre e aperte E (Ação no iPhone). Boa para pôr Espada-de-São-Jorge na porta de casa.", sTextItalic);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (ListButton(S.TemPa ? "Já é sua" : "Comprar", !S.TemPa && S.Money >= GameSession.PrecoPa, 120f)) Defer(() => S.BuyShovel());
                GUILayout.EndHorizontal();

                GUILayout.Label("Nível " + S.Progresso.Nivel + ": a Dona Cida vende mais sementes conforme você ganha experiência.", sTextItalic);
                foreach (var p in S.Db.Plants.OrderBy(x => x.NivelMinimo))
                {
                    string id = p.id;
                    int price = Economy.SeedPrice(p);
                    bool known = S.Herbarium.IsDiscovered(id);
                    bool liberada = S.Progresso.PodeCultivar(p);
                    GUILayout.BeginHorizontal(sCard);
                    var ir = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
                    DrawPlantIcon(ir, id, 0);
                    GUILayout.BeginVertical();
                    GUILayout.Label("Semente de " + p.nome + "   " + price + " moedas", sSection);
                    GUILayout.Label(known ? p.efeitoNoJogo : "Dona Cida: \"" + Ditado(p) + "\"", sTextItalic);
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    if (ListButton(liberada ? "Comprar" : "Nível " + p.NivelMinimo, liberada && S.Money >= price, 120f)) Defer(() => S.BuySeed(id));
                    // de cinco em cinco, para não clicar dez vezes
                    if (liberada && ListButton("+5", S.Money >= price * 5, 70f)) Defer(() => S.BuySeeds(id, 5));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static string Ditado(PlantData p)
        {
            var c = p.Cures.FirstOrDefault();
            if (c != Ailment.Nenhum) return "Essa aqui é boa pra " + Ailments.Nome(c).ToLowerInvariant() + ".";
            if (p.TemAura) return "Planta essa na porta que nada de ruim entra.";
            return "Essa traz sorte, minha filha.";
        }

        void DrawAldeao(Rect body)
        {
            GUILayout.BeginArea(body);
            var req = villager != null ? villager.Request : null;
            if (req == null) { GUILayout.Label("...", sText); GUILayout.EndArea(); return; }
            var info = Ailments.Info(req.Mal);
            GUILayout.Label(req.Nome, sTitle);
            if (req.Atendido)
            {
                GUILayout.Label("\"Deus lhe pague! Tô novinho em folha.\"", sText);
                GUILayout.Label(S.PedidosConcluidos
                    ? "Você atendeu todos os pedidos da vila: amanhã chegam outros."
                    : "Novos pedidos chegam quando os outros forem atendidos, ou no dia " + (S.PedidosValemAte + 1) + ".", sTextItalic);
                GUILayout.EndArea();
                return;
            }
            GUILayout.Label("\"Ai, tô com " + info.Nome.ToLowerInvariant() + "... " + info.Descricao + " Você tem alguma coisa pra mim?\"", sText);
            GUILayout.Label("Recompensa: " + req.Recompensa + " moedas", sSubtitle);
            // os pedidos duram uma semana: depois disso quem não foi atendido procura outra raizeira
            GUILayout.Label(S.Clock.Day >= S.PedidosValemAte
                ? "O pedido vale só até o fim de hoje."
                : "O pedido vale até o fim do dia " + S.PedidosValemAte + " (hoje é o dia " + S.Clock.Day + ").", sTextItalic);
            GUILayout.Space(8);

            scroll = GUILayout.BeginScrollView(scroll);
            var products = S.Inventory.OfKind(ItemKind.Produto).ToList();
            bool any = false;
            foreach (var kv in products)
            {
                ItemKind k; string id;
                if (!Inventory.TryParseKey(kv.Key, out k, out id)) continue;
                var r = S.Db.Recipe(id);
                if (!Village.CanTreat(req, r)) continue;
                any = true;
                GUILayout.BeginHorizontal(sCard);
                GUILayout.Label(r.nome + "  x" + kv.Value, sSection);
                GUILayout.FlexibleSpace();
                if (ListButton("Entregar", true, 130f)) Defer(() => S.TreatVillager(req, id));
                GUILayout.EndHorizontal();
            }
            if (!any)
            {
                var hints = S.Db.RecipesThatCure(req.Mal).Where(KnownRecipe).Select(r => r.nome).Distinct().ToArray();
                GUILayout.Label(hints.Length > 0
                    ? "Você não tem o remédio na bolsa. Dá pra preparar: " + string.Join(", ", hints) + "."
                    : "Você ainda não conhece a cura. Explore e consulte o Livro (aba Males).", sTextItalic);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawIntro(Rect body)
        {
            GUILayout.BeginArea(body);
            GUILayout.BeginHorizontal();
            var portrait = ArtLibrary.Get("player_portrait");
            if (portrait != null)
            {
                var pr = GUILayoutUtility.GetRect(200, 260, GUILayout.Width(200), GUILayout.Height(260));
                SpriteFactory.DrawSprite(pr, portrait);
                GUILayout.Space(12);
            }
            GUILayout.BeginVertical();
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Sua avó, a raizeira mais querida da região, deixou para você o sítio, o caldeirão e um Livro de ervas com quase todas as páginas em branco.", sText);
            GUILayout.Space(6);
            GUILayout.Label("Você também decidiu largar o cigarro — e a fissura vem em ondas. A vó dizia que chá de mulungu acalma o nervoso...", sText);
            GUILayout.Space(6);
            GUILayout.Label("Prepare a terra, plante e regue. Colete ervas nativas na mata. Seque no varal, pile no pilão e prepare chás, garrafadas, elixires e banhos no caldeirão. Cure a si mesma e os aldeões, venda na feira e complete o Livro. Na feira tem pá: com ela você cava canteiros em qualquer canto do sítio para as plantas de proteção, como a Espada-de-São-Jorge na porta de casa.", sText);
            GUILayout.Space(6);
            GUILayout.Label("À noite as assombrações saem (nas duas primeiras noites a casa ainda está benzida pela vó). Aqui ninguém luta: plante Espada-de-São-Jorge, Pimenteira e Guiné, carregue arruda e faça defumação.", sText);
            GUILayout.Space(6);
            GUILayout.Label("Colhendo, coletando, preparando e curando você ganha experiência: cada nível libera novas plantas na feira e na mata. Dormir na cama salva o jogo, e o Menu também.", sText);
            GUILayout.Space(10);
            GUILayout.Label(InputCompat.IsTouchDevice
                ? "Controles: arraste no lado esquerdo para andar · botão grande = ação · Bolsa e Livro nos botões."
                : "Controles: WASD/setas para andar · E para agir · I: Bolsa · B: Livro · Esc: Menu e fecha janelas.", sSubtitle);
            GUILayout.Space(6);
            GUILayout.Label(Aviso, sTextItalic);
            GUILayout.EndScrollView();
            if (Button("Começar", sButton, GUILayout.Height(52))) Close();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawVitoria(Rect body)
        {
            GUILayout.BeginArea(body);
            GUILayout.Label("Você completou o Grimório Botânico e está livre de todos os males.", sSubtitle);
            GUILayout.Label("Dias: " + S.Clock.Day + "  ·  Aldeões curados: " + S.Reputacao + "  ·  Moedas: " + S.Money, sText);
            GUILayout.Space(10);
            GUILayout.Label("A vó estaria orgulhosa. O sítio pode continuar — sempre tem alguém na vila precisando de um chá.", sText);
            GUILayout.Space(10);
            if (Button("Continuar jogando", sButton, GUILayout.Height(52))) Close();
            GUILayout.EndArea();
        }
    }
}
