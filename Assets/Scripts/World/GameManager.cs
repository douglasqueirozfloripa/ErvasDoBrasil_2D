using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Ponto de entrada. Cria o mundo inteiro em código a partir de uma cena vazia,
    /// carrega os dados (Resources/Data/*.json) e liga a GameSession (core) aos objetos da Unity.
    /// Basta dar Play em qualquer cena.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public const float DayLengthSeconds = 240f;

        public GameSession Session { get; private set; }
        public PlayerController Player { get; private set; }
        public GameUI UI { get; private set; }
        public Rect WorldBounds { get; private set; }

        Rect mata;
        Transform world;
        readonly Dictionary<FarmPlotModel, FarmPlot> plotViews = new Dictionary<FarmPlotModel, FarmPlot>();
        Vector2 portaDeCasa;
        float spawnTimer;
        bool wonShown;
        int avisoNoite;        // último dia em que o aviso das noites seguras apareceu

        // ── partida e jogo salvo ──
        // carregar ou começar outra partida recarrega a cena; o pedido sobrevive à recarga nestes estáticos
        static SaveData pendingLoad;
        static bool pendingNew;
        bool started;          // false enquanto o menu inicial está aberto (a sessão ainda está intacta)
        string createdAt;
        public string SaveId { get; private set; }
        public string LastSavedAt { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            LifecycleLog.Add("GameManager.Boot() [AfterSceneLoad]: garante um GameManager na cena");
            EnsureManager();
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                LifecycleLog.Add("SceneManager.sceneLoaded: cena \"" + scene.name + "\" recarregada");
                EnsureManager();
            };
        }

        static void EnsureManager()
        {
            if (FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            LifecycleLog.Add("GameManager.Awake(): lê os JSON, cria a GameSession e monta o mundo");
            Application.targetFrameRate = 60;
            // sem foco o jogo continua rodando (a música não corta ao trocar de janela ou minimizar);
            // a partida em si pausa sozinha: OnApplicationFocus(false) abre o Menu
            Application.runInBackground = true;
            GameAudio.Aplicar(); // volume e mudo escolhidos da última vez (PlayerPrefs)

            var db = LoadDatabase();
            Session = new GameSession(db, new SystemRandom(), DayLengthSeconds, 12);

            UI = gameObject.AddComponent<GameUI>();
            Session.Message += UI.MostrarToast; // a sessão já registrou o aviso no Histórico; a tela só desenha
            Session.Progresso.LevelUp += UI.CelebrateLevel;
            Session.Status.EnergyDrained += UI.FlashEnergy;

            BuildWorld();
            LifecycleLog.Add("  mundo montado: " + Interactable.All.Count + " objetos interativos se registraram no OnEnable()");
            if (pendingLoad != null)
            {
                var d = pendingLoad;
                pendingLoad = null;
                StartFromSave(d);
            }
            else if (pendingNew)
            {
                pendingNew = false;
                StartNew();
            }
            else UI.OpenTitle();
        }

        void Start() { LifecycleLog.Add("GameManager.Start(): roda uma vez, logo antes do primeiro Update()"); }

        // eventos do sistema operacional: trocar de janela (Mac), ir para o segundo plano (iPhone), descarregar a cena
        // perder o foco no meio da partida abre o Menu: GameUI.OnApplicationFocus
        void OnApplicationFocus(bool focus) { LifecycleLog.Add("OnApplicationFocus(" + (focus ? "true" : "false") + "): " + (focus ? "o app ganhou o foco" : "o app perdeu o foco")); }
        void OnApplicationPause(bool paused) { LifecycleLog.Add("OnApplicationPause(" + (paused ? "true" : "false") + "): " + (paused ? "o app foi pausado (segundo plano ou sem foco)" : "o app está rodando em primeiro plano")); }
        void OnDestroy() { if (I == this) LifecycleLog.Add("GameManager.OnDestroy(): a cena está sendo descarregada (troca de cena ou saída)"); }
        void OnApplicationQuit() { LifecycleLog.Add("OnApplicationQuit(): o app vai fechar (Sair do jogo ou Cmd+Q)"); }

        // ─────────────────────────── Partida e jogo salvo ───────────────────────────

        void StartNew()
        {
            started = true;
            Session.StartNewGame();
            UI.Open(GameUI.Window.Intro);
        }

        void StartFromSave(SaveData d)
        {
            started = true;
            SaveGame.Restore(Session, d);
            SaveId = d.id;
            createdAt = d.criadaEm;
            LastSavedAt = d.salvoEm;
            wonShown = d.vitoriaVista;
            Player.transform.position = ClampToWorld(new Vector2(d.jogadoraX, d.jogadoraY));
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(Player.transform.position.x, Player.transform.position.y, -10f);
            UI.Close();
            UI.Toast("Jogo carregado: dia " + Session.Clock.Day + ".");
        }

        public void NewGame()
        {
            if (!started) StartNew();
            else { pendingNew = true; Reload(); }
        }

        public void LoadGame(SaveData d)
        {
            if (d == null) return;
            if (!started) StartFromSave(d);
            else { pendingLoad = d; Reload(); }
        }

        public void BackToTitle() { Reload(); }

        /// <summary>Testes de interface: em vez de fechar o app de teste, anotam que o jogo pediu para sair (null = fecha de verdade).</summary>
        public static System.Action AoSair { get; set; }

        /// <summary>
        /// "Sair do jogo" no menu inicial: fecha o app. No editor o Application.Quit() é ignorado (pare o Play).
        /// O iPhone não tem o botão: lá quem fecha o app é o sistema.
        /// </summary>
        public static void Sair()
        {
            LifecycleLog.Add("Sair do jogo: Application.Quit() pede para a Unity fechar o app");
            if (AoSair != null) { AoSair(); return; }
            Application.Quit();
        }

        static void Reload() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

        /// <summary>Salva a partida atual (cria o arquivo na primeira vez). Chamado pelo menu e ao dormir.</summary>
        public bool SaveNow()
        {
            var d = SaveGame.Capture(Session);
            if (SaveId == null)
            {
                SaveId = SaveStore.NewId();
                createdAt = SaveStore.Now();
            }
            d.id = SaveId;
            d.criadaEm = createdAt;
            d.salvoEm = SaveStore.Now();
            d.jogadoraX = Player.transform.position.x;
            d.jogadoraY = Player.transform.position.y;
            d.vitoriaVista = wonShown;
            bool ok = SaveStore.Write(d);
            if (ok) LastSavedAt = d.salvoEm;
            UI.Toast(ok ? "Jogo salvo." : "Não foi possível salvar o jogo.");
            return ok;
        }

        public static GameDatabase LoadDatabase()
        {
            var plantsText = Resources.Load<TextAsset>("Data/plants");
            var recipesText = Resources.Load<TextAsset>("Data/recipes");
            var plants = plantsText != null ? JsonUtility.FromJson<PlantCatalog>(plantsText.text) : null;
            var recipes = recipesText != null ? JsonUtility.FromJson<RecipeCatalog>(recipesText.text) : null;
            var db = new GameDatabase(plants != null ? plants.plantas : null, recipes != null ? recipes.receitas : null);
            foreach (var e in db.Validate()) Debug.LogWarning("[Dados] " + e);
            return db;
        }

        // ─────────────────────────── Mundo ───────────────────────────

        void BuildWorld()
        {
            world = new GameObject("Mundo").transform;

            // Chão: usa o mapa ilustrado se existir; senão, campos de cor
            var map = ArtLibrary.Get("map_fazenda");
            float worldW = 36f, worldH = 24f;
            if (map != null)
            {
                worldH = worldW * map.bounds.size.y / map.bounds.size.x;
                SpriteFactory.Create("Mapa", world, map, Vector2.zero, worldH, Color.white, SpriteFactory.ChaoOrder);
            }
            else
            {
                var g = SpriteFactory.Create("Chao", world, SpriteFactory.Square(), Vector2.zero, worldH, new Color(0.47f, 0.62f, 0.33f), SpriteFactory.ChaoOrder);
                g.transform.localScale = new Vector3(worldW, worldH, 1f);
            }
            WorldBounds = new Rect(-worldW / 2f, -worldH / 2f, worldW, worldH);

            // Mata (canto superior esquerdo): ervas nativas e espíritos
            mata = new Rect(WorldBounds.xMin + 1f, 1.5f, 11f, WorldBounds.yMax - 2.5f);
            if (map == null)
            {
                var m = SpriteFactory.Create("Mata", world, SpriteFactory.Square(), mata.center, 1f, new Color(0.18f, 0.36f, 0.2f), SpriteFactory.ChaoOrder + 1);
                m.transform.localScale = new Vector3(mata.width, mata.height, 1f);
            }

            // Casa da vó (direita, perto do celeiro do mapa)
            var casa = new Vector2(9.5f, 4.2f);
            portaDeCasa = casa + new Vector2(-1f, -3.5f);
            Spawn<Bed>("Cama", world, casa, ArtLibrary.Get("prop_casa"), 2.2f, new Color(0.6f, 0.3f, 0.2f));
            SpawnStation("Caldeirão", GameUI.Window.Caldeirao, world, casa + new Vector2(-2.6f, -1.6f), "prop_caldeirao", new Color(0.2f, 0.2f, 0.22f));
            SpawnStation("Varal", GameUI.Window.Varal, world, casa + new Vector2(2.6f, -1.6f), "prop_varal", new Color(0.65f, 0.5f, 0.3f));
            SpawnStation("Pilão", GameUI.Window.Pilao, world, casa + new Vector2(0f, -2.4f), "prop_pilao", new Color(0.5f, 0.35f, 0.22f));

            // Horta 4x3 (centro-sul): as posições vêm do core (FarmLayout). Os canteiros cavados com a pá,
            // agora ou ao carregar um save, chegam pelo evento PlotAdded
            foreach (var m in Session.Plots) SpawnPlot(m);
            Session.PlotAdded += SpawnPlot;
            Session.PlotRemoved += DespawnPlot;

            // Vila e feira (sudoeste)
            var vila = new Vector2(-6f, -3.2f);
            Spawn<Market>("Feira", world, vila, ArtLibrary.Get("prop_feira"), 1.6f, new Color(0.85f, 0.55f, 0.2f));
            for (int i = 0; i < 3; i++)
            {
                // a arte do começo é só para o tamanho e o rótulo: no primeiro Update cada um veste a do aldeão do seu pedido
                var v = Spawn<Villager>("Aldeao " + i, world, vila + new Vector2(-2.5f + i * 2.5f, -2.3f), ArtLibrary.Get(Village.Arte(Village.Nomes[i])), Villager.Altura,
                    Color.HSVToRGB(0.08f + i * 0.25f, 0.45f, 0.85f));
                v.Index = i;
            }

            // Ervas selvagens espalhadas pela mata
            var wild = Session.Db.Plants.Where(p => p.selvagem).ToList();
            var rnd = new System.Random(7);
            for (int i = 0; i < 16 && wild.Count > 0; i++)
            {
                var pos = new Vector2(mata.xMin + (float)rnd.NextDouble() * mata.width, mata.yMin + (float)rnd.NextDouble() * mata.height);
                var go = new GameObject("Erva selvagem");
                go.transform.SetParent(world);
                go.transform.position = pos;
                go.AddComponent<WildHerb>().Init(wild[i % wild.Count].id);
            }

            // Trilha sonora
            gameObject.AddComponent<MusicManager>().VillageCenter = vila;

            // Jogadora
            var pgo = new GameObject("Raizeira");
            pgo.transform.position = portaDeCasa;
            Player = pgo.AddComponent<PlayerController>();
            Player.Init();

            // Câmera
            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
            }
            if (!cam.TryGetComponent<AudioListener>(out _)) cam.gameObject.AddComponent<AudioListener>();
            if (!cam.TryGetComponent<CameraFollow>(out var follow)) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = Player.transform;
            cam.transform.position = new Vector3(Player.transform.position.x, Player.transform.position.y, -10f);
        }

        T Spawn<T>(string name, Transform parent, Vector2 pos, Sprite art, float height, Color fallback) where T : Interactable
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SpriteFactory.YOrder(pos.y);
            if (art != null) SpriteFactory.SetSprite(sr, art, height);
            else
            {
                sr.color = fallback;
                SpriteFactory.SetSprite(sr, SpriteFactory.Circle(), Mathf.Min(height, 1f));
            }
            var t = go.AddComponent<T>();
            t.Radius = Mathf.Max(1.2f, height * 0.7f);
            if (art != null) t.LabelHeight = height + 0.12f; // a arte tem o pivô no pé: o rótulo fica acima da cabeça
            return t;
        }

        void SpawnStation(string nome, GameUI.Window window, Transform parent, Vector2 pos, string artKey, Color fallback)
        {
            var s = Spawn<Station>(nome, parent, pos, ArtLibrary.Get(artKey), 1.1f, fallback);
            s.Nome = nome;
            s.Window = window;
        }

        void SpawnPlot(FarmPlotModel m)
        {
            var go = new GameObject(m.Cavado ? "Canteiro cavado" : "Canteiro " + (plotViews.Count + 1));
            go.transform.SetParent(world);
            go.transform.position = new Vector2(m.X, m.Y);
            var view = go.AddComponent<FarmPlot>();
            view.Init(m);
            plotViews[m] = view;
        }

        void DespawnPlot(FarmPlotModel m)
        {
            FarmPlot view;
            if (!plotViews.TryGetValue(m, out view)) return;
            plotViews.Remove(m);
            if (view != null) Destroy(view.gameObject);
        }

        // ─────────────────────────── Pá ───────────────────────────

        // lago do mapa (tools/arte/cenario.py: centro (13.5, −6.5), raios 3.2 × 1.8), com folga para os juncos
        static readonly Vector2 LagoCentro = new Vector2(13.5f, -6.5f);
        static readonly Vector2 LagoRaio = new Vector2(4f, 2.6f);

        /// <summary>
        /// Dá para cavar nesta casa da grade? Não dá fora do mapa, na mata, no lago, em cima de outro canteiro
        /// nem por cima da casa, das estações, da feira, dos aldeões ou das ervas da mata.
        /// </summary>
        public bool CanDigAt(Vector2 cell)
        {
            var half = new Vector2(FarmLayout.Largura, FarmLayout.Altura) * 0.5f;
            var r = new Rect(cell - half, half * 2f);
            var b = WorldBounds;
            if (r.xMin < b.xMin + 1f || r.xMax > b.xMax - 1f || r.yMin < b.yMin + 1f || r.yMax > b.yMax - 1f) return false;
            if (mata.Overlaps(r)) return false;
            var d = new Vector2((cell.x - LagoCentro.x) / LagoRaio.x, (cell.y - LagoCentro.y) / LagoRaio.y);
            if (d.sqrMagnitude < 1f) return false;
            if (Session.PlotAt(cell.x, cell.y) != null) return false;
            foreach (var i in Interactable.All)
            {
                if (i == null || i is FarmPlot) continue;
                Rect o;
                if (i.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
                    o = new Rect(sr.bounds.min.x - 0.15f, sr.bounds.min.y - 0.15f, sr.bounds.size.x + 0.3f, sr.bounds.size.y + 0.3f);
                else
                    o = new Rect((Vector2)i.transform.position - Vector2.one * 0.6f, Vector2.one * 1.2f);
                if (o.Overlaps(r)) return false;
            }
            return true;
        }

        /// <summary>Cava um canteiro nesta casa da grade. As regras (pá, energia, limite) ficam na GameSession.</summary>
        public bool Dig(Vector2 cell)
        {
            if (!CanDigAt(cell) || Session.DigPlot(cell.x, cell.y) == null) return false;
            Player.PlayAction("forage", 0.5f);
            return true;
        }

        /// <summary>
        /// Mantém dentro do mapa. A posição é o pé (pivô embaixo do sprite): em cima a margem é a altura da raizeira,
        /// senão no topo do mapa a cabeça dela saía da tela (a câmera para na borda do mapa).
        /// </summary>
        public Vector2 ClampToWorld(Vector2 p)
        {
            var b = WorldBounds;
            return new Vector2(Mathf.Clamp(p.x, b.xMin + 0.4f, b.xMax - 0.4f), Mathf.Clamp(p.y, b.yMin + 0.4f, b.yMax - (PlayerController.Height + 0.2f)));
        }

        // ─────────────────────────── Loop ───────────────────────────

        void Update()
        {
            // o Menu, os jogos salvos e as telas de abertura pausam o relógio; Bolsa, Livro, Caldeirão, Feira... não
            if (UI.PausesWorld) return;

            Session.Tick(Time.deltaTime);
            NightNotice();
            SpawnSpirits();

            if (!wonShown && Session.Won)
            {
                wonShown = true;
                UI.Open(GameUI.Window.Vitoria);
            }
        }

        /// <summary>Ao anoitecer, avisa se a noite é tranquila (casa benzida) ou se a proteção da vó acabou.</summary>
        void NightNotice()
        {
            int dia = Session.Clock.Day;
            if (!Session.Clock.IsNight || Session.Clock.Time01 < DayCycle.Anoitecer || avisoNoite == dia) return;
            avisoNoite = dia;
            if (dia <= ProtectionRules.NoitesSeguras)
                UI.Toast("A casa ainda está benzida pela vó: esta noite é tranquila, nenhuma assombração aparece.");
            else if (dia == ProtectionRules.NoitesSeguras + 1)
                UI.Toast("A bênção da vó acabou: a partir de hoje as assombrações saem à noite. Plante Espada-de-São-Jorge perto de casa!");
            else if (dia == ProtectionRules.DiaDasLendas)
                UI.Toast("Dizem na vila que a Cuca e a Bernúncia andam rondando o sítio à noite...");
        }

        /// <summary>A Bernúncia engoliu a jogadora: ela sai na porta da Casa da Vó.</summary>
        public void SpitAtHome() { Player.Teleport(portaDeCasa); }

        void SpawnSpirits()
        {
            var st = Session.Status;
            bool active = Session.Clock.IsNight || st.Assombrado;
            if (!active) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            spawnTimer = Session.Clock.IsNight ? 8f : 20f;

            if (Spirit.All.Count >= ProtectionRules.MaxSpirits(Session.Clock.Day)) return;

            var type = ProtectionRules.PickSpawn(Session.Rng, Session.Clock.Day, ProtectionRules.Madrugada(Session.Clock.Time01));
            // uma lenda de cada vez
            if ((type == SpiritType.Cuca || type == SpiritType.Bernuncia) && Spirit.All.Exists(x => x.Type == type))
                type = ProtectionRules.PickSpawn(Session.Rng);
            Vector2 pos;
            // nascem na mata ou, se assombrada, num anel ao redor da jogadora
            if (st.Assombrado || Random.value < 0.4f)
                pos = (Vector2)Player.transform.position + Random.insideUnitCircle.normalized * 9f;
            else
                pos = new Vector2(Random.Range(mata.xMin, mata.xMax), Random.Range(mata.yMin, mata.yMax));
            pos = ClampToWorld(ProtectionAura.PushOut(pos, type));

            var go = new GameObject("Espirito " + type);
            go.transform.position = pos;
            go.AddComponent<Spirit>().Init(type);
        }

        /// <summary>Chamado pela UI ao usar um produto (defumação bane espíritos ao redor).</summary>
        public void ApplyUse(UseResult r)
        {
            if (r == null || !r.Consumed) return;
            if (r.Banish)
            {
                Vector2 p = Player.transform.position;
                foreach (var s in new List<Spirit>(Spirit.All))
                    if (Vector2.Distance(p, s.transform.position) <= r.BanishRadius) s.Banish();
            }
            if (Session.Status.VeEspiritosOcultos) UI.Toast("Com a arruda, você enxerga o que estava escondido...");
        }

        public void OnSlept()
        {
            foreach (var s in new List<Spirit>(Spirit.All)) Destroy(s.gameObject);
            SaveNow(); // dormir salva o jogo
        }
    }
}
