using System.Collections.Generic;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Estilos IMGUI com a paleta do Livro (pergaminho + madeira), com cantos arredondados.</summary>
    public partial class GameUI
    {
        public static readonly Color Pergaminho = new Color(0.97f, 0.92f, 0.80f);
        public static readonly Color PergaminhoEscuro = new Color(0.91f, 0.84f, 0.68f);
        public static readonly Color Madeira = new Color(0.33f, 0.22f, 0.13f);
        public static readonly Color Tinta = new Color(0.24f, 0.15f, 0.09f);
        public static readonly Color Folha = new Color(0.30f, 0.45f, 0.22f);

        /// <summary>
        /// Verde dos botões. Mais escuro que a Folha e com borda creme: em cima da grama do mapa o verde antigo sumia
        /// (contraste 2,5:1); este dá 3:1 com a grama e 9:1 com o texto branco, e a borda clara separa o botão do fundo.
        /// </summary>
        public static readonly Color FolhaEscura = new Color(0.16f, 0.30f, 0.12f);
        static readonly Color Ouro = new Color(1f, 0.82f, 0.32f);

        GUIStyle sPanel, sTitle, sSubtitle, sText, sTextItalic, sWarn, sButton, sTab, sTabOn, sListItem, sListItemOn,
                 sHudBox, sHudToggle, sHudText, sHudBad, sHudGood, sHudSpirit, sHudSmall, sHudSmallRight, sBarText, sBarDelta, sToast,
                 sPrompt, sRoundButton, sCenterSmall, sWorldLabel, sWorldLabelWrap, sChip, sIconBadge, sCard, sSection,
                 sBarBack, sBarFill, sShadow, sPortrait, sLevelBanner, sLevelText, sClock, sSmallButton, sSlotEmpty, sSlotText;
        bool stylesReady;

        static readonly Dictionary<string, Texture2D> rounded = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Retângulo de cantos arredondados (com antisserrilhado) para fundo de GUIStyle. A textura é pequena
        /// e a IMGUI estica só o miolo (GUIStyle.border = raio + borda), então o canto fica redondo em qualquer tamanho.
        /// </summary>
        static Texture2D Rounded(Color fill, int radius, Color border, int borderWidth = 0)
        {
            string key = fill + "|" + border + "|" + radius + "|" + borderWidth;
            Texture2D t;
            if (rounded.TryGetValue(key, out t) && t != null) return t;
            int size = radius * 2 + 4;
            t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float outer = Mathf.Clamp01(0.5f - RoundedDistance(p, half, radius));
                    float inner = borderWidth > 0 ? Mathf.Clamp01(0.5f - RoundedDistance(p, half - borderWidth, Mathf.Max(0f, radius - borderWidth))) : 1f;
                    var c = Color.Lerp(border, fill, inner);
                    c.a *= outer;
                    px[y * size + x] = c;
                }
            t.SetPixels32(px);
            t.Apply();
            rounded[key] = t;
            return t;
        }

        /// <summary>Distância com sinal até a borda de um quadrado de meia-largura h e raio r (negativa dentro).</summary>
        static float RoundedDistance(Vector2 p, float h, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x) - (h - r), Mathf.Abs(p.y) - (h - r));
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        static GUIStyle RoundedBox(Color fill, int radius, Color border, int borderWidth = 0)
        {
            var s = new GUIStyle(GUI.skin.box);
            s.normal.background = Rounded(fill, radius, border, borderWidth);
            int b = radius + borderWidth;
            s.border = new RectOffset(b, b, b, b);
            return s;
        }

        GUIStyle Label(int size, Color color, FontStyle fs = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = true)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fs, alignment = anchor, wordWrap = wrap, richText = true };
            s.normal.textColor = color;
            return s;
        }

        GUIStyle Boxed(Color bg, Color fg, int size, TextAnchor anchor = TextAnchor.MiddleCenter, Color? border = null, int radius = 10)
        {
            var s = new GUIStyle(GUI.skin.button) { fontSize = size, alignment = anchor, wordWrap = true, richText = true };
            var line = border ?? Color.Lerp(bg, Color.black, 0.25f);
            int bw = border.HasValue ? 2 : 1;
            s.normal.background = Rounded(bg, radius, line, bw);
            s.hover.background = Rounded(Color.Lerp(bg, Color.white, 0.14f), radius, line, bw);
            s.active.background = Rounded(Color.Lerp(bg, Color.black, 0.18f), radius, line, bw);
            s.focused.background = s.normal.background;
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = fg;
            int b = radius + bw;
            s.border = new RectOffset(b, b, b, b);
            s.padding = new RectOffset(12, 12, 6, 6);
            return s;
        }

        void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            sPanel = RoundedBox(Pergaminho, 18, Madeira, 3);
            sCard = RoundedBox(PergaminhoEscuro, 10, new Color(Madeira.r, Madeira.g, Madeira.b, 0.28f), 1);
            sCard.padding = new RectOffset(12, 12, 8, 8);
            sCard.margin = new RectOffset(0, 0, 0, 8);
            sPortrait = RoundedBox(new Color(0.2f, 0.24f, 0.3f), 12, Madeira, 2);
            sShadow = RoundedBox(new Color(0f, 0f, 0f, 0.35f), 12, new Color(0f, 0f, 0f, 0.35f));

            sTitle = Label(28, Tinta, FontStyle.Bold);
            sSubtitle = Label(20, Folha, FontStyle.Bold);
            sSection = Label(18, Madeira, FontStyle.Bold);
            sText = Label(17, Tinta);
            sTextItalic = Label(16, new Color(0.35f, 0.25f, 0.18f), FontStyle.Italic);
            // itálico sintético inclina a letra: a perna do "p" no início da linha sai à esquerda e era cortada
            sTextItalic.clipping = TextClipping.Overflow;
            sTextItalic.padding.left += 4;
            sWarn = Label(16, new Color(0.62f, 0.16f, 0.12f));
            sClock = Label(15, new Color(0.35f, 0.25f, 0.18f), FontStyle.Italic, TextAnchor.MiddleRight, false);

            sButton = Boxed(FolhaEscura, Color.white, 17, TextAnchor.MiddleCenter, Pergaminho, 12);
            sSmallButton = Boxed(FolhaEscura, Color.white, 14, TextAnchor.MiddleCenter, Pergaminho, 8);
            sSmallButton.padding = new RectOffset(4, 4, 2, 2);
            sSlotEmpty = Label(13, Madeira, FontStyle.Italic, TextAnchor.MiddleCenter);
            sSlotText = Label(12, Tinta, FontStyle.Bold, TextAnchor.MiddleCenter);
            sTab = Boxed(PergaminhoEscuro, Tinta, 17, TextAnchor.MiddleCenter, null, 12);
            sTabOn = Boxed(Madeira, Pergaminho, 17, TextAnchor.MiddleCenter, null, 12);
            sListItem = Boxed(PergaminhoEscuro, Tinta, 16, TextAnchor.MiddleLeft, null, 8);
            sListItemOn = Boxed(Madeira, Pergaminho, 16, TextAnchor.MiddleLeft, null, 8);

            sHudBox = RoundedBox(new Color(0.12f, 0.09f, 0.06f, 0.8f), 12, new Color(Pergaminho.r, Pergaminho.g, Pergaminho.b, 0.35f), 1);
            sHudToggle = Label(12, Pergaminho, FontStyle.Normal, TextAnchor.MiddleCenter, false);
            sHudText = Label(16, Pergaminho, FontStyle.Bold, TextAnchor.UpperLeft, false);
            sHudBad = Label(15, new Color(1f, 0.72f, 0.6f), FontStyle.Normal, TextAnchor.UpperLeft, false);
            sHudSpirit = Label(15, new Color(0.85f, 0.7f, 1f), FontStyle.Normal, TextAnchor.UpperLeft, false);
            sHudGood = Label(15, new Color(0.7f, 1f, 0.7f), FontStyle.Normal, TextAnchor.UpperLeft, false);
            sHudSmall = Label(13, new Color(0.9f, 0.85f, 0.75f), FontStyle.Normal, TextAnchor.UpperLeft, false);
            sHudSmallRight = Label(13, new Color(1f, 1f, 1f, 0.85f), FontStyle.Normal, TextAnchor.MiddleRight, false);
            sBarText = Label(13, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft, false);
            sBarText.padding = new RectOffset(8, 8, 0, 0);
            sBarDelta = Label(14, new Color(1f, 0.55f, 0.48f), FontStyle.Bold, TextAnchor.MiddleRight, false); // o "−15" da energia perdida
            sBarDelta.padding = new RectOffset(8, 8, 0, 0);
            sBarBack = RoundedBox(new Color(0f, 0f, 0f, 0.5f), 8, new Color(0f, 0f, 0f, 0.5f));
            sBarFill = RoundedBox(Color.white, 6, Color.white);
            sToast = Label(15, Pergaminho);
            sPrompt = Boxed(new Color(0.98f, 0.92f, 0.75f, 0.97f), Tinta, 18, TextAnchor.MiddleCenter, Madeira, 14);
            sRoundButton = Label(15, Tinta, FontStyle.Bold, TextAnchor.MiddleCenter);
            sCenterSmall = Label(13, new Color(1, 1, 1, 0.5f), FontStyle.Normal, TextAnchor.MiddleCenter);
            sWorldLabel = Label(13, Pergaminho, FontStyle.Bold, TextAnchor.MiddleCenter, false);
            sWorldLabelWrap = Label(13, Pergaminho, FontStyle.Bold, TextAnchor.MiddleCenter);
            sChip = RoundedBox(new Color(0.12f, 0.09f, 0.06f, 0.68f), 8, new Color(0.12f, 0.09f, 0.06f, 0.68f));
            sIconBadge = Label(11, Color.white, FontStyle.Bold, TextAnchor.LowerRight, false);

            sLevelBanner = RoundedBox(new Color(0.2f, 0.13f, 0.07f, 0.95f), 20, Ouro, 4);
            sLevelText = Label(18, Pergaminho, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
    }
}
