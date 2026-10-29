using System;
using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    /// <summary>
    /// Fachada com todas as regras do jogo, sem depender da Unity.
    /// Os MonoBehaviours só chamam estes métodos e desenham o resultado — por isso o core é 100% testável.
    /// </summary>
    public class GameSession
    {
        public const int DinheiroInicial = 30;
        public const float CustoColetaSelvagem = 3f;

        public readonly GameDatabase Db;
        public readonly IRandom Rng;
        public readonly Inventory Inventory = new Inventory();
        public readonly PlayerStatus Status = new PlayerStatus();
        public readonly Herbarium Herbarium = new Herbarium();
        public readonly DryingRack Rack = new DryingRack();
        public readonly DayCycle Clock;
        public readonly List<FarmPlotModel> Plots = new List<FarmPlotModel>();
        public readonly List<VillagerRequest> Requests = new List<VillagerRequest>();
        public readonly Progression Progresso = new Progression();

        /// <summary>Histórico dos avisos (botão Histórico): tudo o que passa por <see cref="Avisar"/> e pelo Message fica aqui.</summary>
        public readonly MessageLog Avisos = new MessageLog();

        public int Money { get; internal set; }
        public int Reputacao { get; internal set; }

        /// <summary>Comprou a pá na feira: pode cavar canteiros fora da horta.</summary>
        public bool TemPa { get; internal set; }

        public event Action<string> Message;
        public event Action<int> NewDay;

        /// <summary>Um canteiro novo apareceu (cavado ou carregado do save): a Unity cria o visual dele.</summary>
        public event Action<FarmPlotModel> PlotAdded;
        public event Action<FarmPlotModel> PlotRemoved;

        public GameSession(GameDatabase db, IRandom rng, float dayLengthSeconds = 240f, int plotCount = 12)
        {
            Db = db;
            Rng = rng;
            Clock = new DayCycle(dayLengthSeconds);
            for (int i = 0; i < plotCount; i++) Plots.Add(new FarmPlotModel(FarmLayout.HortaX(i), FarmLayout.HortaY(i)));
            Status.Message += Say;
            Progresso.LevelUp += OnLevelUp;
        }

        /// <summary>Kit inicial: a vó deixou umas sementes, e você está tentando largar o cigarro.</summary>
        public void StartNewGame()
        {
            Avisos.Limpar(); // o que apareceu no menu inicial (desfazer um save apagado...) não é desta partida
            Money = DinheiroInicial;
            foreach (var id in new[] { "boldo", "camomila", "arruda", "espada_sao_jorge" })
                if (Db.Plant(id) != null) Inventory.Add(Inventory.Seed(id), 2);
            if (Db.Plant("mulungu") != null)
            {
                Inventory.Add(Inventory.Herb("mulungu"), 1);
                Herbarium.Discover("mulungu"); // a vó deixou o chá anotado: dá para preparar desde o primeiro dia
            }
            Status.Add(Ailment.VontadeDeFumar);
            Status.FissuraEmEspera = true; // a primeira crise vem depois do primeiro plantio (roteiro, Ato I): o começo é calmo
            RefreshRequests();
        }

        // ───────── Horta ─────────

        public bool PreparePlot(FarmPlotModel plot)
        {
            if (plot.State != PlotState.Bruto) return false;
            if (!Status.TrySpend(FarmPlotModel.CustoPreparar)) return false;
            return plot.Prepare();
        }

        /// <summary>As plantas que fazem efeito na área, as únicas do canteiro cavado com a pá ("Espada-de-São-Jorge, Pimenteira...").</summary>
        public string PlantasDeProtecao
        {
            get { return string.Join(", ", Db.Plants.Where(p => p.TemAura).OrderBy(p => p.NivelMinimo).Select(p => p.nome).ToArray()); }
        }

        public bool PlantSeed(FarmPlotModel plot, string plantId)
        {
            if (plot.State != PlotState.Preparado || Db.Plant(plantId) == null) return false;
            if (!plot.Aceita(Db.Plant(plantId)))
            {
                Say("Canteiro cavado com a pá é para plantas de proteção (" + PlantasDeProtecao + "). " + Db.Plant(plantId).nome + " vai na horta da vó.");
                return false;
            }
            if (!Inventory.Has(Inventory.Seed(plantId))) { Say("Você não tem semente de " + Db.Plant(plantId).nome + "."); return false; }
            if (!Progresso.PodeCultivar(Db.Plant(plantId))) { Say("Você ainda não sabe cultivar " + Db.Plant(plantId).nome + " (nível " + Db.Plant(plantId).NivelMinimo + ")."); return false; }
            if (!Status.TrySpend(FarmPlotModel.CustoPlantar)) return false;
            Inventory.Remove(Inventory.Seed(plantId));
            Status.FissuraEmEspera = false; // a fissura começa a contar: a primeira crise vem daqui a 25 s
            return plot.Plant(plantId);
        }

        public bool WaterPlot(FarmPlotModel plot)
        {
            if (!plot.NeedsWater) return false;
            if (!Status.TrySpend(FarmPlotModel.CustoRegar)) return false;
            return plot.Water();
        }

        public HarvestResult HarvestPlot(FarmPlotModel plot)
        {
            if (!plot.IsReady) return null;
            if (!Status.TrySpend(FarmPlotModel.CustoColher)) return null;
            var r = plot.Harvest(Status, Rng);
            Inventory.Add(Inventory.Herb(r.PlantId), r.Ervas);
            if (r.Sementes > 0) Inventory.Add(Inventory.Seed(r.PlantId), r.Sementes);
            Discover(r.PlantId);
            Progresso.Add(Progression.XpColheita);
            return r;
        }

        // ───────── Pá: canteiros em qualquer canto do sítio ─────────

        public const int PrecoPa = 25;
        public const int MaxCanteirosCavados = 24;

        public bool BuyShovel()
        {
            if (TemPa) { Say("Você já tem uma pá."); return false; }
            if (Money < PrecoPa) { Say("Dinheiro insuficiente."); return false; }
            Money -= PrecoPa;
            TemPa = true;
            Say("Comprou a pá! Pare num lugar livre e aperte a ação para cavar um canteiro.");
            return true;
        }

        public int CanteirosCavados { get { return Plots.Count(p => p.Cavado); } }

        /// <summary>Canteiro que ocupa a casa da grade deste ponto (ou null).</summary>
        public FarmPlotModel PlotAt(float x, float y)
        {
            float sx = FarmLayout.SnapX(x), sy = FarmLayout.SnapY(y);
            return Plots.FirstOrDefault(p => FarmLayout.MesmaCasa(p.X, p.Y, sx, sy));
        }

        /// <summary>
        /// Cava um canteiro novo na casa da grade mais perto de (x, y). Ele já nasce preparado: é só plantar.
        /// Se o lugar está livre de casas, lago e bichos quem diz é a Unity; aqui ficam a pá, a vaga, o limite e a energia.
        /// </summary>
        public FarmPlotModel DigPlot(float x, float y)
        {
            if (!TemPa) { Say("Você precisa de uma pá. A Dona Cida vende na feira."); return null; }
            if (PlotAt(x, y) != null) { Say("Já tem um canteiro aqui."); return null; }
            if (CanteirosCavados >= MaxCanteirosCavados) { Say("Já são " + MaxCanteirosCavados + " canteiros cavados. Desfaça um vazio antes de cavar outro."); return null; }
            if (!Status.TrySpend(FarmPlotModel.CustoCavar)) return null;
            var plot = new FarmPlotModel(FarmLayout.SnapX(x), FarmLayout.SnapY(y), true);
            plot.Prepare();
            AddPlot(plot);
            return plot;
        }

        /// <summary>Tapa um canteiro cavado e vazio (a horta da vó fica).</summary>
        public bool FillPlot(FarmPlotModel plot)
        {
            if (plot == null || !plot.Cavado || !plot.IsEmpty || !Plots.Remove(plot)) return false;
            if (PlotRemoved != null) PlotRemoved(plot);
            return true;
        }

        internal void AddPlot(FarmPlotModel plot)
        {
            Plots.Add(plot);
            if (PlotAdded != null) PlotAdded(plot);
        }

        /// <summary>Coleta de campo na mata. Retorna a quantidade coletada (0 se falhou).</summary>
        public int CollectWild(string plantId)
        {
            var p = Db.Plant(plantId);
            if (p == null) return 0;
            if (!Progresso.PodeCultivar(p)) { Say("Você ainda não sabe colher esta planta. Volte no nível " + p.NivelMinimo + "."); return 0; }
            if (!Status.TrySpend(CustoColetaSelvagem)) return 0;
            int qty = Math.Max(1, (int)Math.Floor(Rng.Range(1, 3) * Status.HarvestMultiplier));
            Inventory.Add(Inventory.Herb(plantId), qty);
            if (Rng.Value() < 0.3f) Inventory.Add(Inventory.Seed(plantId), 1);
            Discover(plantId);
            Progresso.Add(Progression.XpColetaSelvagem);
            return qty;
        }

        public bool Discover(string plantId)
        {
            if (!Herbarium.Discover(plantId)) return false;
            var p = Db.Plant(plantId);
            Say("Nova página no Livro: " + (p != null ? p.nome : plantId) + "!");
            Progresso.Add(Progression.XpNovaPlanta);
            return true;
        }

        // ───────── Preparo ─────────

        public bool HangToDry(string plantId) { return Rack.Hang(plantId, Inventory); }
        public int CollectDried() { return Rack.CollectDry(Inventory); }

        /// <summary>Tira uma erva do varal antes da hora (ou já seca) e devolve à bolsa, sem perder nada.</summary>
        public string TakeFromRack(int index)
        {
            var key = Rack.Take(index, Inventory);
            if (key != null) Say(DisplayName(key) + " voltou para a bolsa.");
            return key;
        }
        public bool Grind(string plantId) { return Mortar.Grind(plantId, Inventory, Status); }

        public CraftOutcome Craft(string recipeId)
        {
            var r = Db.Recipe(recipeId);
            var outcome = Crafting.Craft(r, Inventory, Status, Rng);
            switch (outcome)
            {
                case CraftOutcome.Sucesso: Say("Preparou: " + r.nome); Progresso.Add(Progression.XpReceita); break;
                case CraftOutcome.SucessoComQueimadura: Say("Preparou " + r.nome + ", mas queimou a mão no caldeirão!"); Progresso.Add(Progression.XpReceita); break;
                case CraftOutcome.MaosQueimadas: Say("Com a mão queimada não dá. Passe gel de babosa."); break;
                case CraftOutcome.FaltamIngredientes: Say("Faltam ingredientes."); break;
            }
            return outcome;
        }

        public UseResult Use(string recipeId)
        {
            var r = Db.Recipe(recipeId);
            var res = ItemEffects.Use(r, Inventory, Status);
            if (res.Consumed && res.Curados.Count == 0 && res.Melhorados.Count == 0 && r.Effect == ItemEffect.Cura)
                Say("Você usou " + r.nome + ". Fez bem, mas não havia o que curar.");
            return res;
        }

        // ───────── Economia ─────────

        public int SellPrice(string key) { return Economy.SellPrice(key, Db, Status); }

        public bool Sell(string key, int qty = 1)
        {
            if (qty <= 0 || !Inventory.Has(key, qty)) return false;
            int price = SellPrice(key);
            if (price <= 0) return false;
            Inventory.Remove(key, qty);
            Money += price * qty;
            return true;
        }

        /// <summary>Quanto a feira paga hoje por tudo que dá para vender na bolsa.</summary>
        public int SellAllValue { get { return Inventory.All.Sum(kv => SellPrice(kv.Key) * kv.Value); } }

        /// <summary>Vende de uma vez tudo que tem preço. Retorna as moedas ganhas.</summary>
        public int SellAll()
        {
            int ganho = 0;
            foreach (var kv in Inventory.All.ToList())
            {
                int price = SellPrice(kv.Key);
                if (price <= 0 || !Sell(kv.Key, kv.Value)) continue;
                ganho += price * kv.Value;
            }
            if (ganho > 0) Say("Vendeu tudo por " + ganho + " moedas.");
            return ganho;
        }

        /// <summary>Compra até n sementes, parando quando o dinheiro acaba. Retorna quantas comprou.</summary>
        public int BuySeeds(string plantId, int n)
        {
            int compradas = 0;
            var p = Db.Plant(plantId);
            while (compradas < n && p != null && Money >= Economy.SeedPrice(p) && BuySeed(plantId)) compradas++;
            return compradas;
        }

        public bool BuySeed(string plantId)
        {
            var p = Db.Plant(plantId);
            if (p == null) return false;
            if (!Progresso.PodeCultivar(p)) { Say("A Dona Cida só vende essa semente a partir do nível " + p.NivelMinimo + "."); return false; }
            int price = Economy.SeedPrice(p);
            if (Money < price) { Say("Dinheiro insuficiente."); return false; }
            Money -= price;
            Inventory.Add(Inventory.Seed(plantId));
            return true;
        }

        public bool TreatVillager(VillagerRequest req, string recipeId)
        {
            var r = Db.Recipe(recipeId);
            if (!Village.CanTreat(req, r) || !Inventory.Has(Inventory.Product(recipeId))) return false;
            Inventory.Remove(Inventory.Product(recipeId));
            req.Atendido = true;
            Reputacao++;
            Money += req.Recompensa;
            Say(req.Nome + " ficou curado(a) de " + Ailments.Nome(req.Mal) + "! +" + req.Recompensa + " moedas");
            if (PedidosConcluidos) Say("Você atendeu os " + Requests.Count + " pedidos da vila! Amanhã chegam pedidos novos.");
            Progresso.Add(Progression.XpAldeaoCurado);
            return true;
        }

        /// <summary>Os pedidos da vila valem por uma semana, ou até todos serem atendidos: aí chegam outros na manhã seguinte.</summary>
        public const int DiasDosPedidos = 7;

        /// <summary>Dia em que chegaram os pedidos de agora.</summary>
        public int PedidosDesde { get; internal set; } = 1;

        /// <summary>Último dia dos pedidos de agora: na manhã seguinte chegam outros.</summary>
        public int PedidosValemAte { get { return PedidosDesde + DiasDosPedidos - 1; } }

        /// <summary>Todos os pedidos atendidos (ou nenhum pedido, como num save sem pedidos): na próxima manhã chegam outros.</summary>
        public bool PedidosConcluidos { get { return Requests.All(r => r.Atendido); } }

        /// <summary>Na manhã de um dia novo: troca os pedidos se todos foram atendidos ou se a semana deles acabou.</summary>
        void AtualizarPedidos()
        {
            bool concluidos = PedidosConcluidos;
            if (!concluidos && Clock.Day <= PedidosValemAte) return;
            RefreshRequests();
            Say(concluidos ? "Chegaram pedidos novos na vila." : "Passou uma semana: quem não foi atendido procurou outra raizeira. Chegaram pedidos novos na vila.");
        }

        public void RefreshRequests()
        {
            PedidosDesde = Clock.Day;
            Requests.Clear();
            var names = Village.Nomes.ToList();
            int n = Math.Min(3, names.Count);
            for (int i = 0; i < n; i++)
            {
                int k = Rng.Range(0, names.Count);
                var req = Village.NewRequest(names[k], Db, Rng);
                names.RemoveAt(k);
                if (req != null) Requests.Add(req);
            }
        }

        // ───────── Espíritos (combate passivo) ─────────

        /// <summary>Assombração encostou: drena energia e aura; pode deixar você assombrado.</summary>
        public void OnHaunted()
        {
            Status.Drain(20f);
            Status.HitAura(15f);
            Status.LoseSanity(10f);
            if (Status.Aura < PlayerStatus.AuraVulneravel || Rng.Value() < 0.3f) Status.Add(Ailment.Assombrado);
        }

        /// <summary>Visagem por perto (invisível): drena sanidade aos poucos.</summary>
        public void OnVisagemNear(float dt) { Status.LoseSanity(4f * dt); }

        /// <summary>Olho-gordo lançou mau-olhado. Retorna true se pegou.</summary>
        public bool OnEvilEye(bool insideAntiOlhoGordo) { return Status.TryCurse(insideAntiOlhoGordo); }

        /// <summary>Saci passou: rouba um item aleatório. Retorna a chave roubada (ou null).</summary>
        public string OnSaciSteal()
        {
            var key = Inventory.RandomKey(Rng);
            if (key == null) return null;
            Inventory.Remove(key);
            Say("O Saci levou " + DisplayName(key) + " num redemoinho! Plante guiné para afastá-lo.");
            return key;
        }

        /// <summary>A Cuca pegou você acordada de madrugada: o sono atrasado vem de uma vez.</summary>
        public void OnCuca()
        {
            Status.Drain(25f);
            Status.LoseSanity(10f);
            Status.Add(Ailment.Cansaco);
            Say("A Cuca te pegou acordada! \"Nana, neném...\" O sono atrasado veio de uma vez. Vá dormir antes das 22h.");
        }

        /// <summary>A Bernúncia te engoliu e te cuspiu na porta de casa (a Unity leva a jogadora até lá).</summary>
        public void OnBernunciaSwallow()
        {
            Status.Drain(10f);
            Say("A Bernúncia te engoliu... e te cuspiu na porta da Casa da Vó!");
        }

        /// <summary>A Bernúncia engoliu um canteiro. Retorna a planta perdida (ou null se o canteiro estava vazio).</summary>
        public string OnBernunciaDevour(FarmPlotModel plot)
        {
            var id = plot != null ? plot.Devour() : null;
            if (id != null)
            {
                var p = Db.Plant(id);
                Say("A Bernúncia engoliu o canteiro de " + (p != null ? p.nome : id) + "! Uma barreira de Espada-de-São-Jorge protege a horta.");
            }
            return id;
        }

        // ───────── Tempo ─────────

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            Status.Tick(dt, Clock.IsNight);
            Rack.Tick(dt);
            foreach (var plot in Plots) plot.Tick(dt, Db.Plant(plot.PlantId));

            int midnights = Clock.Advance(dt);
            for (int i = 0; i < midnights; i++)
            {
                // passou da meia-noite acordado: a noite em claro cansa
                Status.Add(Ailment.Cansaco);
            }
            if (midnights > 0) BeginDay(false);
        }

        public bool Sleep()
        {
            if (!Clock.CanSleep) { Say("Ainda é cedo pra dormir. Volte depois das 18h."); return false; }
            Clock.SleepUntilMorning();
            if (Status.SleepRestores)
            {
                Status.RestoreFull();
                Status.RestoreSanity(PlayerStatus.MaxSanidade);
                Status.CureDose(Ailment.Cansaco, 99);
                Status.CureDose(Ailment.Ansiedade, 99); // uma boa noite de sono acalma
            }
            else Say("A insônia não deixou você descansar. Chá de erva-cidreira ajuda.");
            BeginDay(true);
            return true;
        }

        void BeginDay(bool slept)
        {
            Status.OnNewDay();
            foreach (var p in Plots) p.OnNewDay();
            DailyEvents.RollMorningAilment(Status, Rng);
            AtualizarPedidos();
            Say("Dia " + Clock.Day + (slept ? " — bom dia!" : ""));
            if (NewDay != null) NewDay(Clock.Day);
        }

        // ───────── Progressão ─────────

        void OnLevelUp(int nivel)
        {
            var novas = Db.Plants.Where(p => p.NivelMinimo == nivel).Select(p => p.nome).ToArray();
            Say("Nível " + nivel + "!" + (novas.Length > 0 ? " Agora você pode cultivar e colher: " + string.Join(", ", novas) + "." : ""), AvisoTipo.Nivel);
        }

        // ───────── Objetivo ─────────

        /// <summary>Vitória: herbário completo e nenhum mal ativo.</summary>
        public bool Won { get { return Herbarium.IsComplete(Db) && Status.ActiveAilments.Count == 0; } }

        public string DisplayName(string key)
        {
            ItemKind kind; string id;
            if (!Inventory.TryParseKey(key, out kind, out id)) return key;
            var p = Db.Plant(id);
            switch (kind)
            {
                case ItemKind.Erva: return p != null ? p.nome : id;
                case ItemKind.ErvaSeca: return (p != null ? p.nome : id) + " (seca)";
                case ItemKind.Po: return (p != null ? p.nome : id) + " (pó)";
                case ItemKind.Semente: return "Semente de " + (p != null ? p.nome : id);
                case ItemKind.Produto: var r = Db.Recipe(id); return r != null ? r.nome : id;
            }
            return key;
        }

        /// <summary>
        /// Avisos que nascem fora das regras (colheita na Unity, assombrações, jogo salvo): entram no histórico
        /// e vão para o canto da tela pelo mesmo Message dos avisos das regras.
        /// </summary>
        public void Avisar(string msg) { Say(msg); }

        void Say(string msg) { Say(msg, AvisoTipo.Comum); }

        void Say(string msg, AvisoTipo tipo)
        {
            if (string.IsNullOrEmpty(msg)) return;
            Avisos.Registrar(Clock.Day, Clock.Clock, msg, tipo);
            if (Message != null) Message(msg);
        }
    }
}
