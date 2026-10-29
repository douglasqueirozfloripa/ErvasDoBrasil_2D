using System.Globalization;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Bastidores do desenvolvimento: como o jogo foi feito em Unity, o ciclo de vida do app,
    /// o que muda em relação a um sistema web e um painel ao vivo com o diário do ciclo de vida (LifecycleLog).
    /// Abre pelo menu inicial e pelo Menu da partida.
    /// </summary>
    public partial class GameUI
    {
        enum DevTab { ComoFoiFeito, CicloDeVida, UnityXWeb, AoVivo }
        DevTab devTab = DevTab.ComoFoiFeito;
        Window bastidoresReturn = Window.Inicio;

        // medições do painel Ao vivo
        float fps;
        int onGuiFrame = -1, onGuiCalls, onGuiCallsLastFrame;
        bool loggedFirstUpdate, loggedFirstOnGui;

        GUIStyle sStep, sStepOn, sStepTitle, sStepTitleOn, sStepText, sStepTextOn, sArrow, sColHead, sCol, sLog;

        /// <summary>Janelas com lista à esquerda e página à direita: no toque, cada metade rola a sua coluna.</summary>
        bool TwoColumns { get { return Current == Window.Livro || (Current == Window.Bastidores && devTab == DevTab.AoVivo); } }

        void OpenBastidores(Window from)
        {
            bastidoresReturn = from;
            Open(Window.Bastidores);
        }

        /// <summary>Chamado no Update: registra o primeiro frame da cena e suaviza o FPS.</summary>
        void TrackFrame()
        {
            if (!loggedFirstUpdate)
            {
                loggedFirstUpdate = true;
                LifecycleLog.Add("GameUI.Update(): primeiro frame desta cena");
            }
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fps = fps <= 0f ? 1f / dt : Mathf.Lerp(fps, 1f / dt, 0.05f);
        }

        /// <summary>Chamado no OnGUI: conta quantas vezes ele roda em cada frame (Layout, Repaint e um por evento).</summary>
        void CountOnGui()
        {
            if (!loggedFirstOnGui)
            {
                loggedFirstOnGui = true;
                LifecycleLog.Add("GameUI.OnGUI(): primeira vez, depois que o frame foi desenhado");
            }
            if (Time.frameCount != onGuiFrame)
            {
                onGuiCallsLastFrame = onGuiCalls;
                onGuiCalls = 0;
                onGuiFrame = Time.frameCount;
            }
            onGuiCalls++;
        }

        void InitDevStyles()
        {
            if (sStep != null) return;
            sStep = RoundedBox(PergaminhoEscuro, 10, new Color(Madeira.r, Madeira.g, Madeira.b, 0.28f), 1);
            sStep.padding = new RectOffset(8, 8, 8, 8);
            sStep.margin = new RectOffset(0, 0, 0, 0);
            sStepOn = new GUIStyle(sStep);
            sStepOn.normal.background = Rounded(Madeira, 10, Madeira, 1);
            sStepTitle = Label(15, Tinta, FontStyle.Bold, TextAnchor.UpperCenter, false);
            sStepTitleOn = Label(15, Pergaminho, FontStyle.Bold, TextAnchor.UpperCenter, false);
            sStepText = Label(13, Tinta, FontStyle.Normal, TextAnchor.UpperCenter);
            sStepTextOn = Label(13, Pergaminho, FontStyle.Normal, TextAnchor.UpperCenter);
            sArrow = Label(22, Madeira, FontStyle.Bold, TextAnchor.MiddleCenter, false);
            sArrow.margin = new RectOffset(0, 0, 0, 0);
            sColHead = Label(14, Folha, FontStyle.Bold, TextAnchor.UpperLeft, false);
            sCol = Label(15, Tinta);
            sLog = Label(14, Tinta);
        }

        void DrawBastidores(Rect body)
        {
            InitDevStyles();
            float tw = Mathf.Min(190f, body.width / DevTabs.Length - 6f);
            for (int i = 0; i < DevTabs.Length; i++)
            {
                var r = new Rect(body.x + i * (tw + 6f), body.y, tw, 40f);
                if (Button(r, DevTabs[i], (int)devTab == i ? sTabOn : sTab)) { devTab = (DevTab)i; scroll = scroll2 = Vector2.zero; }
            }
            var content = new Rect(body.x, body.y + 50f, body.width, body.height - 50f);

            switch (devTab)
            {
                case DevTab.ComoFoiFeito: DrawDevBuild(content); break;
                case DevTab.CicloDeVida: DrawDevLifecycle(content); break;
                case DevTab.UnityXWeb: DrawDevWeb(content); break;
                case DevTab.AoVivo: DrawDevLive(content); break;
            }
        }

        // ───────── Como foi feito ─────────

        // estáticos: o OnGUI roda várias vezes por frame e não deve criar arrays novos a cada chamada
        static readonly string[][] ArchitectureFlow =
        {
            new[] { "Entrada", "teclado, mouse e toque viram GameKey (InputCompat)" },
            new[] { "MonoBehaviours", "PlayerController, canteiros, aldeões e GameUI chamam as regras" },
            new[] { "GameSession", "Core: regras em C# puro, sem UnityEngine, testadas" },
            new[] { "Tela e som", "sprites, janelas no OnGUI() e a trilha por ambiente" },
        };

        static readonly string[][] FrameFlow =
        {
            new[] { "Entrada", "a Unity lê teclado, mouse e toque" },
            new[] { "Update()", "relógio do jogo, espíritos, a jogadora anda" },
            new[] { "LateUpdate()", "a câmera segue a jogadora depois que todos andaram" },
            new[] { "Desenho", "a câmera renderiza os sprites, ordenados pela altura" },
            new[] { "OnGUI()", "HUD e janelas, várias vezes por frame" },
        };

        static readonly string[] DevTabs = { "Como foi feito", "Ciclo de vida", "Unity × Web", "Ao vivo" };

        void DrawDevBuild(Rect area)
        {
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Um jogo inteiro sem arrastar nada no editor: tudo nasce do código.", sSubtitle);
            GUILayout.Space(6);
            FlowRow(area.width, ArchitectureFlow, -1);
            GUILayout.Space(10);

            DevCard("Ferramentas",
                "Unity " + Application.unityVersion + " (6.3 LTS) com C#. O código foi escrito em parceria com o Claude Code. " +
                "A arte e a trilha são geradas por scripts Python (Pillow, numpy e ffmpeg). Testes, lint e builds rodam pelo terminal " +
                "(scripts/*.sh), com a Unity em modo batch, sem abrir o editor.");
            DevCard("Uma cena vazia, um mundo feito em código",
                "Main.unity só tem a câmera e o GameManager. No Awake(), o GameManager lê os JSON, cria a GameSession e monta tudo com " +
                "new GameObject() + AddComponent(): mapa, casa, canteiros, feira, aldeões, ervas da mata, música, jogadora e câmera. " +
                "Sem prefabs nem objetos posicionados à mão: o mundo inteiro está no código e no controle de versão.");
            DevCard("Duas camadas: regras × motor",
                "ErvasDoBrasil.Core é C# puro (o asmdef proíbe UnityEngine): energia, males, horta, receitas, economia, níveis e save. " +
                "A camada Game (MonoBehaviours) só lê a entrada, chama a GameSession e desenha o resultado. É como separar as regras " +
                "de negócio da API num back-end: o Core é testado sem abrir a Unity.");
            DevCard("Dados em JSON",
                "As " + S.Db.Plants.Count + " plantas e os " + S.Db.Recipes.Count + " preparos vivem em Assets/Resources/Data " +
                "(plants.json e recipes.json). Para incluir uma planta, basta editar o JSON: GameDatabase.Validate() e os testes conferem ids, " +
                "ingredientes e níveis.");
            DevCard("Arte e música geradas por código",
                "tools/arte desenha cada sprite em Python no estilo \"Quintal Minimalista\"; tools/musica compõe as faixas de dia, feira e noite. " +
                "Na Unity, o ArtPipeline importa os PNGs e gera a ArtLibrary (nome → sprite). Se um sprite faltar, o jogo desenha uma forma " +
                "colorida no lugar, então dá para jogar até sem arte.");
            DevCard("Interface em IMGUI",
                "Todas as janelas, inclusive esta, são desenhadas no OnGUI() com GUILayout, sem montar nada na cena. A UI usa uma tela " +
                "virtual de 720 de altura escalada pela GUI.matrix, e a Screen.safeArea desvia do notch do iPhone. Na versão final, dá para " +
                "migrar para UI Toolkit mantendo as mesmas chamadas à GameSession.");
            DevCard("Qualidade: testes e lint",
                "130 testes NUnit (EditMode) cobrem o Core e os dados: ./scripts/run-tests.sh. A análise estática roda com ./scripts/lint.sh: " +
                "o compilador Roslyn com os analisadores do .NET e do Microsoft.Unity.Analyzers, mais o dotnet format para o estilo, " +
                "tudo sem abrir a Unity.");
            DevCard("Build e plataformas",
                "macOS: ./scripts/build-macos.sh gera o ErvasDoBrasil.app (backend Mono). iPhone: ./scripts/build-ios-sim.sh gera um " +
                "projeto Xcode (IL2CPP: o C# vira C++) e abre no Simulador. A mesma base de código atende teclado no Mac e joystick " +
                "virtual no toque.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ───────── Ciclo de vida ─────────

        void DrawDevLifecycle(Rect area)
        {
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("1. Do ícone ao primeiro frame", sSubtitle);
            DevCard("Abrir o app",
                "O sistema inicia o executável e a Unity sobe o runtime C# (Mono no Mac, IL2CPP no iPhone). Ganchos " +
                "[RuntimeInitializeOnLoadMethod] podem rodar antes de existir qualquer cena (SubsystemRegistration, BeforeSceneLoad).");
            DevCard("Carregar a cena e acordar os componentes",
                "A Unity carrega a primeira cena da build (Main.unity) e chama Awake() em cada componente, assim que ele nasce: é aqui " +
                "que o GameManager monta o mundo inteiro. Logo depois vem OnEnable(): canteiros, ervas e espíritos se inscrevem em listas " +
                "estáticas (Interactable.All, Spirit.All).");
            DevCard("Depois da cena",
                "GameManager.Boot() [AfterSceneLoad] cria um GameManager se a cena não tiver um (por isso dá Play em qualquer cena). " +
                "Start() roda uma vez, logo antes do primeiro Update().");

            GUILayout.Space(8);
            GUILayout.Label("2. A cada frame, cerca de 60 vezes por segundo", sSubtitle);
            GUILayout.Space(4);
            FlowRow(area.width, FrameFlow, (int)(Time.unscaledTime / 1.2f) % FrameFlow.Length);
            GUILayout.Space(8);
            DevCard("Update() no jogo",
                "GameManager.Update() avança o relógio com Session.Tick(Time.deltaTime) e faz nascer espíritos à noite; " +
                "PlayerController.Update() move a raizeira; cada Spirit persegue ou foge da aura. Multiplicar pelo deltaTime faz tudo andar " +
                "na mesma velocidade a 30 ou a 120 fps. FixedUpdate() serve para física em passo fixo, e este jogo não usa física.");
            DevCard("Pausar sem parar o loop",
                "Com o Menu, os jogos salvos ou esta janela abertos (GameUI.PausesWorld), GameManager.Update() retorna cedo e o relógio do jogo " +
                "para, mas o loop continua rodando: na aba Ao vivo, o contador de frames não para de subir. Bolsa, Livro, Caldeirão, Varal, " +
                "Pilão e Feira não pausam: o tempo corre enquanto você mexe neles, e o varal seca na sua frente.");

            GUILayout.Space(8);
            GUILayout.Label("3. Trocar de cena e sair", sSubtitle);
            DevCard("Trocar de cena é como recarregar a página",
                "Carregar um jogo salvo, começar outro ou voltar ao menu inicial chama SceneManager.LoadScene: todos os objetos recebem " +
                "OnDisable() e OnDestroy(), a cena volta do zero e o ciclo recomeça no Awake(). Só os campos static sobrevivem, e é por " +
                "eles que o pedido \"carregue este save\" atravessa a recarga (como o sessionStorage de uma página web).");
            DevCard("Segundo plano e fechar",
                "No iPhone, sair do app gera OnApplicationPause(true), e o sistema pode encerrar o processo sem outro aviso. No Mac, trocar " +
                "de janela ou minimizar gera OnApplicationFocus(false). O Run In Background está ligado, então o loop continua e a música " +
                "não corta; para a partida não seguir sozinha, o GameUI abre o Menu, que pausa o relógio. Ao fechar, vem OnApplicationQuit(). " +
                "Hoje o jogo salva pelo Menu ou ao dormir; salvar também no OnApplicationPause seria o próximo passo.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ───────── Unity × Web ─────────

        static readonly string[][] WebComparison =
        {
            new[] { "Quem manda no tempo",
                "Orientado a eventos: o servidor responde a uma requisição e fica parado; o navegador só redesenha quando algo muda.",
                "Game loop: cerca de 60 vezes por segundo o jogo roda Update() e redesenha a tela inteira, mesmo com a jogadora parada." },
            new[] { "Onde fica o estado",
                "Servidor sem estado: os dados ficam no banco, e a sessão num cookie ou token. No front, no estado do React e na URL.",
                "Tudo na memória do processo (GameSession). Não há servidor nem banco: o save é um arquivo JSON gravado em persistentDataPath." },
            new[] { "Interface",
                "DOM + CSS em modo retido: você descreve a árvore, o navegador a guarda e redesenha. Um clique vira evento onClick.",
                "IMGUI em modo imediato: a cada OnGUI() o código desenha cada botão de novo, e GUI.Button() devolve true no frame do clique." },
            new[] { "Entrada",
                "Eventos empurrados para handlers (addEventListener, onKeyDown).",
                "Polling: a cada frame o código pergunta se a tecla foi apertada (Input.GetKeyDown), e o toque vira joystick virtual." },
            new[] { "Tempo e animação",
                "setTimeout, setInterval, requestAnimationFrame e transições de CSS.",
                "Time.deltaTime em cada Update(): um dia do jogo são 240 segundos de frames somados, e ele para quando o Menu abre." },
            new[] { "Layout e telas",
                "px, flexbox e media queries; o navegador calcula o layout.",
                "Mundo em unidades com câmera ortográfica; a UI é uma tela virtual de 720 de altura escalada, e a safeArea desvia do notch." },
            new[] { "Arquivos e assets",
                "Imagens baixadas por HTTP sob demanda, com cache do navegador e CDN.",
                "Assets importados no editor (PNG vira sprite, OGG vira AudioClip) e empacotados no app; Resources.Load lê do pacote, sem rede." },
            new[] { "Entrega",
                "Deploy no servidor: o usuário abre a URL e já tem a versão nova. Rollback é voltar a versão no servidor.",
                "Um binário por plataforma (.app no Mac, projeto Xcode no iPhone). Cada mudança pede build novo e reinstalação; na App Store, revisão da Apple." },
            new[] { "Linguagem e runtime",
                "JavaScript/TypeScript no navegador; no servidor, Node, Java, Go...",
                "C# 9 sobre .NET Standard 2.1. No Mac roda em Mono (JIT); no iPhone o IL2CPP compila para C++ antes (AOT), sem gerar código em tempo de execução." },
            new[] { "Concorrência",
                "Muitos usuários ao mesmo tempo; async/await, filas e o banco cuidando dos conflitos.",
                "Um jogador e um processo. A API da Unity só pode ser chamada na thread principal." },
            new[] { "Testes e análise estática",
                "Unitários (Jest, JUnit), testes de API e E2E no navegador (Playwright, Cypress); ESLint no código.",
                "NUnit no Core (EditMode, sem abrir o jogo); PlayMode testa cenas; o E2E costuma ser o playtest. Lint: analisadores .NET + dotnet format." },
            new[] { "Depuração e logs",
                "DevTools: console, aba Network, inspetor do DOM; logs centralizados do servidor.",
                "Console, Inspector e Profiler do editor com o jogo rodando; no Mac o app grava o Player.log (o caminho está na aba Ao vivo)." },
            new[] { "Segurança",
                "Autenticação, CORS, validação de entrada, injeção de SQL, rate limit contra DDoS...",
                "Offline e sem login: não há API para atacar nem requisição para vazar dados. O lint proíbe APIs de rede, o build sai sem porta aberta e o save é validado como entrada não confiável. Editar o próprio save é aceitável num jogo single-player." },
        };

        void DrawDevWeb(Rect area)
        {
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Para quem vem da web: o que muda em um jogo Unity", sSubtitle);
            GUILayout.Space(6);
            // largura útil: área − barra de rolagem − margens do cartão − espaço entre as colunas
            float colW = Mathf.Max(120f, (area.width - 70f) / 2f);
            foreach (var c in WebComparison)
            {
                GUILayout.BeginVertical(sCard);
                GUILayout.Label(c[0], sSection);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(colW));
                GUILayout.Label("Na web", sColHead);
                GUILayout.Label(c[1], sCol);
                GUILayout.EndVertical();
                GUILayout.Space(16);
                GUILayout.BeginVertical(GUILayout.Width(colW));
                GUILayout.Label("Neste jogo (Unity)", sColHead);
                GUILayout.Label(c[2], sCol);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ───────── Ao vivo ─────────

        static string Num(float v, string format) { return v.ToString(format, CultureInfo.InvariantCulture).Replace('.', ','); }

        void DrawDevLive(Rect area)
        {
            var left = new Rect(area.x, area.y, Mathf.Min(380f, area.width * 0.42f), area.height);
            var right = new Rect(left.xMax + 14f, area.y, area.width - left.width - 14f, area.height);

            GUILayout.BeginArea(left);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Agora", sSection);
            float t = Time.realtimeSinceStartup;
            LiveLine("Frame", Time.frameCount.ToString(CultureInfo.InvariantCulture));
            LiveLine("Velocidade", "≈ " + Num(fps, "0") + " fps · " + Num(Time.unscaledDeltaTime * 1000f, "0.0") + " ms por frame");
            LiveLine("App aberto há", ((int)t / 60) + " min " + ((int)t % 60).ToString("00", CultureInfo.InvariantCulture) + " s");
            LiveLine("Relógio do jogo", "Dia " + S.Clock.Day + " · " + S.Clock.Clock + " (parado: esta janela pausa o jogo)");
            LiveLine("OnGUI() no último frame", onGuiCallsLastFrame + " vezes (mexa o mouse aqui e veja subir)");
            LiveLine("Na cena", Interactable.All.Count + " objetos interativos · " + Spirit.All.Count + " espíritos");
            LiveLine("Plataforma", Application.platform + " · Unity " + Application.unityVersion);
            LiveLine("Tela", Screen.width + " × " + Screen.height + " px · escala da UI " + Num(uiScale, "0.00"));
            LiveLine("Jogos salvos em", Privacy.Redact(SaveStore.Dir));
            LiveLine("Log do app", string.IsNullOrEmpty(Application.consoleLogPath) ? "console do Xcode" : Privacy.Redact(Application.consoleLogPath));
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(right);
            GUILayout.Label("Diário do ciclo de vida", sSection);
            GUILayout.Label("Experimente trocar de janela (Cmd+Tab) e voltar, ou carregar um jogo salvo, e reabrir esta aba.", sTextItalic);
            scroll2 = GUILayout.BeginScrollView(scroll2);
            foreach (var e in LifecycleLog.Entries)
            {
                GUILayout.BeginVertical(sCard);
                GUILayout.Label(Num(e.Seconds, "0.00") + " s · frame " + e.Frame, sColHead);
                GUILayout.Label(e.Text, sLog);
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void LiveLine(string label, string value)
        {
            GUILayout.Label(label, sColHead);
            GUILayout.Label(value, sLog);
            GUILayout.Space(4);
        }

        // ───────── Peças compartilhadas ─────────

        void DevCard(string title, string text)
        {
            GUILayout.BeginVertical(sCard);
            GUILayout.Label(title, sSection);
            GUILayout.Label(text, sText);
            GUILayout.EndVertical();
        }

        /// <summary>Fileira de caixas ligadas por setas (título + legenda); a caixa <paramref name="highlight"/> fica em destaque.</summary>
        void FlowRow(float width, string[][] steps, int highlight)
        {
            const float Arrow = 22f, Height = 112f;
            // desconta a barra de rolagem vertical e uma folga: se a fileira passar da largura, surge rolagem horizontal
            float w = Mathf.Max(90f, (width - 44f - Arrow * (steps.Length - 1)) / steps.Length);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < steps.Length; i++)
            {
                bool on = i == highlight;
                GUILayout.BeginVertical(on ? sStepOn : sStep, GUILayout.Width(w), GUILayout.Height(Height));
                GUILayout.Label(steps[i][0], on ? sStepTitleOn : sStepTitle);
                GUILayout.Space(4);
                GUILayout.Label(steps[i][1], on ? sStepTextOn : sStepText);
                GUILayout.EndVertical();
                if (i < steps.Length - 1) GUILayout.Label("→", sArrow, GUILayout.Width(Arrow), GUILayout.Height(Height));
            }
            GUILayout.EndHorizontal();
        }
    }
}
