using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// UI do protótipo em IMGUI (zero setup de cena/prefab): HUD, controles de toque, janelas e o Livro.
    /// Dividida em arquivos parciais: Book (Livro), Inventory (Bolsa), Crafting (Caldeirão/Varal/Pilão), Shop (Feira/Aldeões/Plantio).
    /// Para a versão final, o freelancer pode migrar para UI Toolkit/uGUI mantendo as chamadas à GameSession.
    /// </summary>
    public partial class GameUI : MonoBehaviour
    {
        public enum Window { None, Intro, Bolsa, Livro, Plantar, Caldeirao, Varal, Pilao, Feira, Aldeao, Vitoria, Inicio, Pausa, Salvos, Bastidores, Historico }

        public Window Current { get; private set; }
        public bool IsModal { get { return Current != Window.None; } }

        /// <summary>
        /// Janelas que param o relógio do jogo: o Menu, os jogos salvos, os Bastidores e as telas de abertura e de fim.
        /// Bolsa, Livro, Caldeirão, Varal, Pilão, Feira, plantio e aldeões deixam o tempo correr (a jogadora só não anda).
        /// </summary>
        public bool PausesWorld
        {
            get
            {
                switch (Current)
                {
                    case Window.Intro:
                    case Window.Vitoria:
                    case Window.Inicio:
                    case Window.Pausa:
                    case Window.Salvos:
                    case Window.Bastidores: return true;
                    default: return false;
                }
            }
        }

        /// <summary>Trocar de janela no meio da partida abre o Menu, que pausa o relógio. Os testes de interface desligam.</summary>
        public static bool PauseOnFocusLoss { get; set; } = true;

        void OnApplicationFocus(bool focus)
        {
            if (!focus && PauseOnFocusLoss && !IsModal) Open(Window.Pausa);
        }

        const float VirtualHeight = 720f;

        GameSession S { get { return GameManager.I.Session; } }
        PlayerController Player { get { return GameManager.I.Player; } }

        sealed class ToastMsg { public string Text; public float Until; }
        readonly List<ToastMsg> toasts = new List<ToastMsg>();

        Vector2 scroll, scroll2;
        float uiScale = 1f;
        Rect safe;
        Rect panel;

        // toque
        int stickFinger = -1;
        Vector2 stickOrigin;
        Vector2 stickNow;
        const float StickRadius = 80f;

        // HUD de status: clique recolhe para cima (fica só a linha de dia/hora/moedas)
        bool hudCollapsed;
        float hudFold;   // 0 = aberto, 1 = recolhido (animado no Update)
        Rect hudRect;
        const float HudHeaderHeight = 40f;

        // energia perdida de uma vez (crise de fissura, assombração...): a barra pisca e mostra "−N"
        const float EnergiaPerdidaSegundos = 2f;
        float energiaPerdidaEm = -100f;
        float energiaPerdida;

        // ───────── API ─────────

        /// <summary>Ligado ao evento PlayerStatus.EnergyDrained pelo GameManager. Perdas em seguida somam no mesmo "−N".</summary>
        public void FlashEnergy(float perdida)
        {
            energiaPerdida = (PiscandoEnergia ? energiaPerdida : 0f) + perdida;
            energiaPerdidaEm = Time.unscaledTime;
        }

        /// <summary>Testes: a barra de energia está piscando depois de uma perda.</summary>
        public bool PiscandoEnergia { get { float t = Time.unscaledTime - energiaPerdidaEm; return t >= 0f && t <= EnergiaPerdidaSegundos; } }

        /// <summary>O N do "−N" escrito na barra de energia (0 quando ela não está piscando).</summary>
        public int EnergiaPerdidaNaBarra { get { return PiscandoEnergia ? Mathf.RoundToInt(energiaPerdida) : 0; } }

        /// <summary>
        /// Aviso no canto da tela. Passa pela GameSession (Avisar) para entrar no Histórico; a sessão devolve pelo
        /// Session.Message, que o GameManager liga ao <see cref="MostrarToast"/>.
        /// </summary>
        public void Toast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            var gm = GameManager.I;
            if (gm != null && gm.Session != null) gm.Session.Avisar(msg);
            else MostrarToast(msg);
        }

        /// <summary>Desenha o aviso que a sessão já registrou no Histórico (ligado ao Session.Message).</summary>
        public void MostrarToast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            // o mesmo aviso de novo (crises de fissura seguidas) só renova o tempo, em vez de empilhar cópias
            var same = toasts.Find(t => t.Text == msg);
            if (same != null) { same.Until = Time.unscaledTime + 4f; return; }
            toasts.Add(new ToastMsg { Text = msg, Until = Time.unscaledTime + 4f });
            if (toasts.Count > 5) toasts.RemoveAt(0);
        }

        public void Open(Window w)
        {
            Current = w;
            scroll = scroll2 = Vector2.zero;
            InputCompat.VirtualStick = Vector2.zero;
            stickFinger = -1;
        }

        public void Close() { Open(Window.None); }

        // ───────── Update: teclas e toque ─────────

        void Update()
        {
            TrackFrame();
            if (InputCompat.Pressed(GameKey.Fechar))
            {
                if (IsModal) Back();
                else Open(Window.Pausa);
            }
            if (InputCompat.Pressed(GameKey.Bolsa)) Toggle(Window.Bolsa);
            if (InputCompat.Pressed(GameKey.Livro)) Toggle(Window.Livro);
            if (InputCompat.Pressed(GameKey.Historico)) Toggle(Window.Historico);
            if (InputCompat.Pressed(GameKey.Som)) GameAudio.AlternarMudo();
            TickSair();
            hudFold = Mathf.MoveTowards(hudFold, hudCollapsed ? 1f : 0f, Time.unscaledDeltaTime / 0.2f);
            HandleTouches();
        }

        void Toggle(Window w)
        {
            if (Current == Window.Intro || Current == Window.Vitoria || OnTitle) return;
            Open(Current == w ? Window.None : w);
        }

        /// <summary>Esc / X: a lista de jogos salvos volta para quem a abriu; a introdução e o menu inicial não fecham.</summary>
        void Back()
        {
            switch (Current)
            {
                case Window.Intro:
                case Window.Inicio: return;
                case Window.Salvos: Open(savesReturn); return;
                case Window.Bastidores: Open(bastidoresReturn); return;
                default: Close(); return;
            }
        }

        Vector2 ToGui(Vector2 screen)
        {
            return new Vector2(screen.x / uiScale, (Screen.height - screen.y) / uiScale);
        }

        void HandleTouches()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!InputCompat.IsTouchDevice) return;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                var g = ToGui(t.position);

                if (IsModal)
                {
                    // arrastar para rolar listas (lado esquerdo do painel = lista; direito = página)
                    if (t.phase == TouchPhase.Moved)
                    {
                        float dy = t.deltaPosition.y / uiScale;
                        if (TwoColumns && g.x > panel.center.x) scroll2.y += dy; else scroll.y += dy;
                    }
                    continue;
                }

                if (t.phase == TouchPhase.Began)
                {
                    GameKey key;
                    if (HitButton(g, out key)) { InputCompat.PressVirtual(key); continue; }
                    if (hudRect.Contains(g)) continue;   // toque no HUD recolhe/abre (GUI.Button), não vira joystick
                    if (stickFinger < 0 && g.x < safe.x + safe.width * 0.45f)
                    {
                        stickFinger = t.fingerId;
                        stickOrigin = stickNow = g;
                    }
                }
                if (t.fingerId == stickFinger)
                {
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        stickFinger = -1;
                        InputCompat.VirtualStick = Vector2.zero;
                    }
                    else
                    {
                        stickNow = g;
                        var v = Vector2.ClampMagnitude((g - stickOrigin) / StickRadius, 1f);
                        InputCompat.VirtualStick = new Vector2(v.x, -v.y);
                    }
                }
            }
#endif
        }

        // botões de ação (coordenadas da GUI virtual)
        Vector2 BtnAcao { get { return new Vector2(safe.xMax - 110f, safe.yMax - 120f); } }
        Vector2 BtnBolsa { get { return new Vector2(safe.xMax - 245f, safe.yMax - 70f); } }
        Vector2 BtnLivro { get { return new Vector2(safe.xMax - 70f, safe.yMax - 255f); } }
        Vector2 BtnMenu { get { return new Vector2(safe.xMax - 34f, safe.y + 34f); } }
        Vector2 BtnHistorico { get { return new Vector2(safe.xMax - 110f, safe.y + 38f); } }

        bool HitButton(Vector2 g, out GameKey key)
        {
            key = GameKey.Interagir;
            if (Vector2.Distance(g, BtnAcao) < 75f) { key = GameKey.Interagir; return true; }
            if (Vector2.Distance(g, BtnBolsa) < 50f) { key = GameKey.Bolsa; return true; }
            if (Vector2.Distance(g, BtnLivro) < 50f) { key = GameKey.Livro; return true; }
            if (Vector2.Distance(g, BtnMenu) < 34f) { key = GameKey.Fechar; return true; } // fora das janelas, "Fechar" abre o menu
            if (Vector2.Distance(g, BtnHistorico) < 36f) { key = GameKey.Historico; return true; }
            return false;
        }

        // ───────── OnGUI ─────────

        void OnGUI()
        {
            if (GameManager.I == null || S == null) return;
            CountOnGui();
            InitStyles();

            uiScale = Mathf.Max(0.5f, Screen.height / VirtualHeight);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(uiScale, uiScale, 1f));
            float vw = Screen.width / uiScale;
            var sa = Screen.safeArea;
            safe = new Rect(sa.x / uiScale + 8f, (Screen.height - sa.yMax) / uiScale + 8f, sa.width / uiScale - 16f, sa.height / uiScale - 16f);

            DrawNight(vw);
            if (!OnTitle)
            {
                // na tela inicial a partida ainda não começou: sem HUD, rótulos nem avisos
                DrawWorldLabels();
                DrawHud();
                if (!IsModal) DrawControls();
                DrawToasts();
            }

            if (IsModal)
            {
                GUI.color = new Color(0, 0, 0, 0.45f);
                GUI.DrawTexture(new Rect(0, 0, vw, VirtualHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float w = Mathf.Min(safe.width - 20f, 960f);
                float h = Mathf.Min(safe.height - 20f, 640f);
                panel = new Rect(safe.center.x - w / 2f, safe.center.y - h / 2f, w, h);
                DrawWindow();
            }
            DrawSound(); // depois das janelas: o escurecido do fundo não apaga o controle de som
            if (!OnTitle) DrawLevelUp(vw);
            DrawSaindo(vw); // por cima de tudo: "Fechando o jogo..."

            // ações clicadas nas listas rodam depois do layout (evita erros de IMGUI ao mudar a lista no meio do desenho)
            if (deferred != null)
            {
                var a = deferred;
                deferred = null;
                a();
            }
        }

        System.Action deferred;
        void Defer(System.Action a) { deferred += a; }

        void DrawNight(float vw)
        {
            float dark = S.Clock.Darkness;
            if (dark <= 0f) return;
            GUI.color = new Color(0.05f, 0.07f, 0.22f, dark * 0.45f);
            GUI.DrawTexture(new Rect(0, 0, vw, VirtualHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        const float WorldLabelMaxWidth = 170f;
        const float WorldLabelGap = 2f;
        struct WorldLabel { public Rect Text; public string Value; public GUIStyle Style; }
        readonly List<WorldLabel> worldLabels = new List<WorldLabel>();

        static Rect ChipOf(Rect text) { return new Rect(text.x - 6f, text.y - 1f, text.width + 12f, text.height + 2f); }

        void DrawWorldLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;
            worldLabels.Clear();
            foreach (var i in Interactable.All)
            {
                var text = i.Label;
                if (string.IsNullOrEmpty(text)) continue;
                var sp = cam.WorldToScreenPoint(i.transform.position + new Vector3(0, i.LabelHeight, 0));
                if (sp.z < 0) continue;
                var g = ToGui(sp);
                var content = new GUIContent(text);
                var style = sWorldLabel;
                var size = style.CalcSize(content);
                if (size.x > WorldLabelMaxWidth)
                {
                    // só quebra linha quando passa do limite; mede mais estreito do que desenha (o texto vai no fundo,
                    // 12 px mais largo), pois com a escala da tela quebrar na largura exata dá uma linha a mais e corta a fonte
                    style = sWorldLabelWrap;
                    size = new Vector2(WorldLabelMaxWidth, style.CalcHeight(content, WorldLabelMaxWidth));
                }
                var r = new Rect(g.x - size.x / 2f, g.y - size.y, size.x, size.y);

                // rótulos vizinhos (aldeões lado a lado) não se sobrepõem: sobe até ficar livre
                for (int k = 0; k < worldLabels.Count; k++)
                {
                    var o = worldLabels[k].Text;
                    var busy = new Rect(o.x - WorldLabelGap, o.y - WorldLabelGap, o.width + WorldLabelGap * 2f, o.height + WorldLabelGap * 2f);
                    if (!busy.Overlaps(r)) continue;
                    r.y = busy.y - r.height;
                    k = -1;
                }
                worldLabels.Add(new WorldLabel { Text = r, Value = text, Style = style });
            }

            // fundos antes dos textos: o fundo de um vizinho nunca cobre a letra de outro
            foreach (var l in worldLabels) GUI.Box(ChipOf(l.Text), GUIContent.none, sChip);
            foreach (var l in worldLabels) GUI.Label(ChipOf(l.Text), l.Value, l.Style);
        }

        void DrawHud()
        {
            var st = S.Status;
            var buffs = BuffLines();
            string header = "Dia " + S.Clock.Day + "  ·  " + S.Clock.Clock + (S.Clock.IsNight ? "  (noite)" : "") + "  ·  " + S.Money + " moedas";
            // a linha de cima cresce com o dia e as moedas: a caixa alarga em vez de cortar o fim
            float width = Mathf.Max(300f, sHudText.CalcSize(new GUIContent(header)).x + 28f);
            float inner = width - 24f;
            float fullHeight = 176f + st.ActiveAilments.Count * 22f + buffs.Count * 22f;
            var box = new Rect(safe.x, safe.y, width, Mathf.Lerp(fullHeight, HudHeaderHeight, Mathf.SmoothStep(0f, 1f, hudFold)));
            var tab = new Rect(box.center.x - 22f, box.yMax - 2f, 44f, 20f);
            hudRect = new Rect(box.x, box.y, box.width, tab.yMax - box.y);

            GUI.Box(tab, GUIContent.none, sHudBox);
            GUI.Box(box, GUIContent.none, sHudBox);
            GUI.Label(new Rect(tab.x, tab.y + 2f, tab.width, tab.height - 2f), hudCollapsed ? "▼" : "▲", sHudToggle);

            // a caixa corta o conteúdo enquanto recolhe para cima
            GUI.BeginGroup(box);
            float y = 8f;
            GUI.Label(new Rect(12f, y, inner + 4f, 24f), header, sHudText);
            y += 28f;
            EnergyBar(new Rect(12f, y, inner, 20f), st); y += 26f;
            Bar(new Rect(12f, y, inner, 20f), "Sanidade", st.Sanidade, PlayerStatus.MaxSanidade, PlayerStatus.MaxSanidade, new Color(0.45f, 0.65f, 0.95f)); y += 26f;
            Bar(new Rect(12f, y, inner, 20f), "Aura", st.Aura, PlayerStatus.MaxAura, PlayerStatus.MaxAura, new Color(0.75f, 0.55f, 0.95f)); y += 26f;
            XpBar(new Rect(12f, y, inner, 20f)); y += 30f;

            foreach (var a in st.ActiveAilments)
            {
                var info = Ailments.Info(a.Tipo);
                string doses = a.DosesRestantes > 1 ? " (" + a.DosesRestantes + " doses)" : "";
                GUI.Label(new Rect(12f, y, inner + 4f, 22f), "• " + info.Nome + doses, info.Kind == AilmentKind.Espiritual ? sHudSpirit : sHudBad);
                y += 22f;
            }
            foreach (var b in buffs)
            {
                GUI.Label(new Rect(12f, y, inner + 4f, 22f), "+ " + b, sHudGood);
                y += 22f;
            }
            GUI.Label(new Rect(12f, y, inner + 4f, 20f), "Livro: " + S.Herbarium.Count + "/" + S.Db.Plants.Count + " plantas", sHudSmall);
            GUI.EndGroup();

            if (!IsModal && Pressed(GUI.Button(hudRect, GUIContent.none, GUIStyle.none), "HUD")) hudCollapsed = !hudCollapsed;
        }

        /// <summary>
        /// Barra de energia. Quando a energia cai de uma vez, pisca três vezes em vermelho e mostra "−15" no pedaço que esvaziou:
        /// assim a recarga que vem depois não parece um carregamento.
        /// </summary>
        void EnergyBar(Rect r, PlayerStatus st)
        {
            var cor = new Color(0.95f, 0.72f, 0.25f);
            float t = Time.unscaledTime - energiaPerdidaEm;
            bool piscando = t >= 0f && t <= EnergiaPerdidaSegundos;
            // vermelho em 0 s, 0,8 s e 1,6 s; termina na cor de sempre
            if (piscando) cor = Color.Lerp(cor, new Color(0.9f, 0.2f, 0.16f), 0.5f + 0.5f * Mathf.Cos(t * Mathf.PI * 2.5f));
            Bar(r, "Energia", st.Energy, st.MaxEnergy, PlayerStatus.BaseMaxEnergy, cor);
            if (!piscando) return;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((EnergiaPerdidaSegundos - t) / 0.5f)); // some no último meio segundo
            GUI.Label(r, "−" + EnergiaPerdidaNaBarra, sBarDelta);
            GUI.color = old;
        }

        /// <summary>Barra de experiência: "Nível 2 · XP 57/110". Brilha em dourado logo depois de subir de nível.</summary>
        void XpBar(Rect r)
        {
            var prog = S.Progresso;
            float t = Time.unscaledTime - levelUpAt;
            float brilho = t >= 0f && t < 2f ? 0.5f + 0.5f * Mathf.Sin(t * 12f) : 0f;
            var cor = Color.Lerp(new Color(0.42f, 0.78f, 0.36f), Ouro, brilho);
            string texto = "Nível " + prog.Nivel + "  ·  XP " + prog.Xp + (prog.NoNivelMaximo ? " (nível máximo)" : "/" + prog.XpProximoNivel);
            FillBar(r, prog.FracaoDoNivel, 1f, cor);
            GUI.Label(r, texto, sBarText);
        }
        List<string> BuffLines()
        {
            var st = S.Status;
            var l = new List<string>();
            if (st.AmuletoDias > 0) l.Add("Amuleto (" + st.AmuletoDias + " dia/s)");
            if (st.SorteDias > 0) l.Add("Sorte (" + st.SorteDias + " dia/s)");
            if (st.RevelarSegundos > 0) l.Add("Arruda: vê o oculto (" + Mathf.CeilToInt(st.RevelarSegundos) + "s)");
            if (st.BuffVelocidadeSegundos > 0) l.Add("Ligeira +" + Mathf.RoundToInt(st.BuffVelocidadePct * 100) + "% (" + Mathf.CeilToInt(st.BuffVelocidadeSegundos) + "s)");
            return l;
        }

        void Bar(Rect r, string label, float v, float max, float scaleMax, Color c)
        {
            FillBar(r, Mathf.Clamp01(v / scaleMax), max / scaleMax, c);
            GUI.Label(r, label + "  " + Mathf.RoundToInt(v) + "/" + Mathf.RoundToInt(max), sBarText);
        }

        /// <summary>Fundo arredondado, preenchimento de 0 a 1 e, se o máximo caiu (gripe), a parte bloqueada em vermelho.</summary>
        void FillBar(Rect r, float frac, float capFrac, Color c)
        {
            GUI.Box(r, GUIContent.none, sBarBack);
            var inside = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
            var old = GUI.backgroundColor;
            if (capFrac < 1f)
            {
                GUI.backgroundColor = new Color(0.6f, 0.12f, 0.12f, 0.7f);
                float x = inside.x + inside.width * capFrac;
                GUI.Box(new Rect(x - 6f, inside.y, inside.xMax - x + 6f, inside.height), GUIContent.none, sBarFill);
            }
            float w = inside.width * Mathf.Clamp01(frac);
            if (w > 0.5f)
            {
                GUI.backgroundColor = c;
                // abaixo da largura dos dois cantos a forma arredondada não cabe: recorta uma barra mínima
                GUI.BeginGroup(new Rect(inside.x, inside.y, w, inside.height));
                GUI.Box(new Rect(0f, 0f, Mathf.Max(w, 12f), inside.height), GUIContent.none, sBarFill);
                GUI.EndGroup();
            }
            GUI.backgroundColor = old;
        }

        void DrawControls()
        {
            var prompt = Player != null ? Player.ActionPrompt : null;

            if (InputCompat.IsTouchDevice)
            {
                // joystick
                if (stickFinger >= 0)
                {
                    Circle(stickOrigin, StickRadius, new Color(1, 1, 1, 0.18f));
                    Circle(stickOrigin + Vector2.ClampMagnitude(stickNow - stickOrigin, StickRadius), 34f, new Color(1, 1, 1, 0.5f));
                }
                else
                {
                    var c = new Vector2(safe.x + 120f, safe.yMax - 120f);
                    Circle(c, StickRadius, new Color(1, 1, 1, 0.08f));
                    GUI.Label(new Rect(c.x - 60, c.y - 12, 120, 24), "arraste", sCenterSmall);
                }
                RoundButton(BtnAcao, 70f, string.IsNullOrEmpty(prompt) ? "Ação" : prompt, !string.IsNullOrEmpty(prompt));
                RoundButton(BtnBolsa, 45f, "Bolsa", true);
                RoundButton(BtnLivro, 45f, "Livro", true);
                RoundButton(BtnMenu, 28f, "Menu", true);
                InitHistoricoStyles();
                RoundButton(BtnHistorico, 36f, "Histórico", true, sRoundSmall);
                DrawBadgeNaoLidos(BtnHistorico + new Vector2(26f, -26f));
            }
            else
            {
                if (!string.IsNullOrEmpty(prompt))
                {
                    var size = sPrompt.CalcSize(new GUIContent("[E] " + prompt));
                    var r = new Rect(safe.center.x - size.x / 2f - 14f, safe.yMax - 70f, size.x + 28f, 44f);
                    if (Button(r, "[E] " + prompt, sPrompt)) InputCompat.PressVirtual(GameKey.Interagir);
                }
                // à esquerda do controle de som, que ocupa o canto do rodapé
                GUI.Label(new Rect(SoundRect.x - 650f, safe.yMax - 26f, 640f, 24f), "WASD/setas: andar · E: ação · I: Bolsa · B: Livro · H: Histórico · M: som · Esc: Menu/fechar", sHudSmallRight);
                if (HudButton(new Rect(safe.xMax - 490f, safe.y, 120f, 40f), "Menu (Esc)")) Open(Window.Pausa);
                var historico = new Rect(safe.xMax - 360f, safe.y, 140f, 40f);
                if (HudButton(historico, BotaoHistorico)) Toggle(Window.Historico);
                DrawBadgeNaoLidos(new Vector2(historico.xMax - 4f, historico.y + 4f));
                if (HudButton(new Rect(safe.xMax - 210f, safe.y, 100f, 40f), "Bolsa (I)")) Toggle(Window.Bolsa);
                if (HudButton(new Rect(safe.xMax - 100f, safe.y, 100f, 40f), "Livro (B)")) Toggle(Window.Livro);
            }
        }

        /// <summary>Botão por cima do mapa: sombra embaixo para destacar da grama.</summary>
        bool HudButton(Rect r, string text)
        {
            GUI.Box(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), GUIContent.none, sShadow);
            return Button(r, text, sButton);
        }

        void Circle(Vector2 c, float r, Color col)
        {
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2, r * 2), SpriteFactory.Circle(128).texture);
            GUI.color = Color.white;
        }

        void RoundButton(Vector2 c, float r, string text, bool active, GUIStyle style = null)
        {
            Circle(c, r, active ? new Color(0.98f, 0.92f, 0.75f, 0.9f) : new Color(1, 1, 1, 0.35f));
            GUI.Label(new Rect(c.x - r + 6, c.y - r, r * 2 - 12, r * 2), text, style ?? sRoundButton);
        }

        void DrawToasts()
        {
            toasts.RemoveAll(t => t.Until < Time.unscaledTime);
            float y = safe.y + 50f;
            float w = Mathf.Min(520f, safe.width - 340f);
            foreach (var t in toasts)
            {
                float h = sToast.CalcHeight(new GUIContent(t.Text), w - 20f) + 10f;
                var r = new Rect(safe.xMax - w, y, w, h);
                GUI.Box(r, GUIContent.none, sHudBox);
                GUI.Label(new Rect(r.x + 10, r.y + 5, r.width - 20, r.height - 10), t.Text, sToast);
                y += h + 6f;
            }
        }

        // ───────── Janelas ─────────

        void DrawWindow()
        {
            GUI.Box(panel, GUIContent.none, sPanel);
            var inner = new Rect(panel.x + 20, panel.y + 16, panel.width - 40, panel.height - 32);

            string title = TitleOf(Current);
            GUI.Label(new Rect(inner.x, inner.y, inner.width - 60, 40), title, sTitle);
            if (!PausesWorld && !OnTitle)
            {
                // estas janelas não pausam: o relógio no canto mostra o tempo correndo
                GUI.Label(new Rect(panel.xMax - 330f, panel.y + 18f, 260f, 36f),
                    "Dia " + S.Clock.Day + " · " + S.Clock.Clock + (S.Clock.IsNight ? " (noite)" : "") + " · o tempo corre", sClock);
            }
            if (Current != Window.Intro && Current != Window.Inicio && Button(new Rect(panel.xMax - 60, panel.y + 12, 46, 46), "X", sButton)) { Back(); return; }

            var body = new Rect(inner.x, inner.y + 50, inner.width, inner.height - 50);
            switch (Current)
            {
                case Window.Intro: DrawIntro(body); break;
                case Window.Bolsa: DrawBolsa(body); break;
                case Window.Livro: DrawLivro(body); break;
                case Window.Plantar: DrawPlantar(body); break;
                case Window.Caldeirao: DrawCaldeirao(body); break;
                case Window.Varal: DrawVaral(body); break;
                case Window.Pilao: DrawPilao(body); break;
                case Window.Feira: DrawFeira(body); break;
                case Window.Aldeao: DrawAldeao(body); break;
                case Window.Vitoria: DrawVitoria(body); break;
                case Window.Inicio: DrawInicio(body); break;
                case Window.Pausa: DrawPausa(body); break;
                case Window.Salvos: DrawSalvos(body); break;
                case Window.Bastidores: DrawBastidores(body); break;
                case Window.Historico: DrawHistorico(body); break;
            }
        }

        static string TitleOf(Window w)
        {
            switch (w)
            {
                case Window.Intro: return "Ervas do Brasil";
                case Window.Bolsa: return "Bolsa de palha";
                case Window.Livro: return "Grimório Botânico da Vó";
                case Window.Plantar: return "O que plantar?";
                case Window.Caldeirao: return "Caldeirão";
                case Window.Varal: return "Varal de secagem";
                case Window.Pilao: return "Pilão";
                case Window.Feira: return "Feira da Dona Cida";
                case Window.Aldeao: return "Aldeão";
                case Window.Vitoria: return "O Livro está completo!";
                case Window.Inicio: return "Ervas do Brasil";
                case Window.Pausa: return "Menu";
                case Window.Salvos: return "Jogos salvos";
                case Window.Bastidores: return "Bastidores do desenvolvimento";
                case Window.Historico: return "Histórico";
            }
            return "";
        }

        // ───────── Utilidades compartilhadas pelas janelas parciais ─────────

        /// <summary>Ícone de um item da bolsa (arte se houver; senão um quadradinho da cor da planta).</summary>
        void DrawItemIcon(Rect r, string key)
        {
            ItemKind kind; string id;
            if (!Inventory.TryParseKey(key, out kind, out id)) return;
            string plantId = id;
            if (kind == ItemKind.Produto)
            {
                var icon = ArtLibrary.Get("icon_" + id);
                if (icon != null) { SpriteFactory.DrawSprite(r, icon); return; }
                var rec = S.Db.Recipe(id);
                plantId = rec != null && rec.ingredientes != null && rec.ingredientes.Length > 0 ? rec.ingredientes[0].plantId : id;
            }
            DrawPlantIcon(r, plantId, kind == ItemKind.Semente ? 0 : 4);
            if (kind == ItemKind.Produto) GUI.Label(r, "+", sIconBadge);
            if (kind == ItemKind.ErvaSeca) GUI.Label(r, "seca", sIconBadge);
            if (kind == ItemKind.Po) GUI.Label(r, "pó", sIconBadge);
        }

        void DrawPlantIcon(Rect r, string plantId, int stage)
        {
            var s = ArtLibrary.Get("icon_" + plantId);
            if (s == null) s = ArtLibrary.Get("plant_" + plantId + "_" + stage);
            if (s == null) s = ArtLibrary.Get("plant_" + plantId + "_4");
            if (stage == 0 && ArtLibrary.Get("plant_" + plantId + "_0") != null) s = ArtLibrary.Get("plant_" + plantId + "_0");
            if (s != null) { SpriteFactory.DrawSprite(r, s); return; }
            var p = S.Db.Plant(plantId);
            GUI.color = SpriteFactory.Hex(p != null ? p.cor : null, Color.gray);
            GUI.DrawTexture(new Rect(r.x + r.width * 0.2f, r.y + r.height * 0.2f, r.width * 0.6f, r.height * 0.6f), SpriteFactory.Circle(128).texture);
            GUI.color = Color.white;
        }

        string Describe(RecipeData r)
        {
            var parts = new List<string>();
            // males de várias doses dizem quantas precisam (a fissura pede 3 usos do chá); banho e defumação limpam de uma vez
            bool deUmaVez = r.Effect == ItemEffect.Banho || r.Effect == ItemEffect.Banir;
            var cures = r.Cures.Select(a =>
            {
                var info = Ailments.Info(a);
                return Ailments.Nome(a) + (!deUmaVez && info != null && info.DosesParaCurar > 1 ? " (precisa de " + info.DosesParaCurar + " doses)" : "");
            }).ToList();
            if (cures.Count > 0) parts.Add("Cura: " + string.Join(", ", cures.ToArray()) + (!deUmaVez && r.doses > 1 ? " · cada uso vale " + r.doses + " doses" : ""));
            var b = r.bonus;
            if (b != null)
            {
                if (b.energiaPct > 0) parts.Add("+" + Mathf.RoundToInt(b.energiaPct * 100) + "% energia");
                if (b.sanidade > 0) parts.Add("+" + Mathf.RoundToInt(b.sanidade) + " sanidade");
                if (b.aura > 0) parts.Add("+" + Mathf.RoundToInt(b.aura) + " aura");
                if (b.velocidadePct > 0) parts.Add("+" + Mathf.RoundToInt(b.velocidadePct * 100) + "% velocidade");
            }
            switch (r.Effect)
            {
                case ItemEffect.Amuleto: parts.Add("Amuleto por " + r.duracaoDias + " dia(s)"); break;
                case ItemEffect.Banho: parts.Add("Aura cheia + sorte"); break;
                case ItemEffect.Banir: parts.Add("Bane espíritos num raio de " + (r.raio > 0 ? r.raio : 5) + " m"); break;
                case ItemEffect.Revelar: parts.Add("Revela visagens"); break;
                case ItemEffect.Sorte: parts.Add("Sorte: vende e colhe mais"); break;
            }
            return string.Join(" · ", parts.ToArray());
        }

        string IngredientText(Ingredient ing, bool showHave)
        {
            var p = S.Db.Plant(ing.plantId);
            string txt = ing.qtd + "x " + (p != null ? p.nome : ing.plantId) + FormaTexto(ing.Form);
            if (showHave) txt += " [" + S.Inventory.Count(ing) + "]"; // soma as formas que a receita aceita
            return txt;
        }

        static string FormaTexto(HerbForm forma)
        {
            switch (forma)
            {
                case HerbForm.Seca: return " (seca)";
                case HerbForm.Po: return " (pó)";
                case HerbForm.FrescaOuSeca: return " (fresca ou seca)";
                default: return "";
            }
        }

        float clickLockUntil;
        const float ClickLock = 0.3f;

        /// <summary>
        /// Botão das listas. Depois de um clique, os botões das listas ficam travados por um instante: o clique duplo
        /// não prepara duas vezes, e o segundo clique não cai no botão da linha que subiu para o lugar da que sumiu.
        /// Travado, o botão continua com a cara normal (sem piscar cinza) e só ignora o clique; o clique pedido por um
        /// teste de interface (Button) fica esperando a trava passar.
        /// </summary>
        bool ListButton(string text, bool enabled = true, float width = 150f, float lockSeconds = ClickLock)
        {
            bool old = GUI.enabled;
            GUI.enabled = enabled;
            bool r = false;
            if (ClickLocked) GUILayout.Button(text, sButton, GUILayout.Width(width), GUILayout.Height(44));
            else r = Button(text, sButton, GUILayout.Width(width), GUILayout.Height(44));
            GUI.enabled = old;
            if (r) clickLockUntil = Time.unscaledTime + lockSeconds;
            return r;
        }

        bool ClickLocked { get { return Time.unscaledTime < clickLockUntil; } }
    }
}
