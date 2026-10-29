using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Controle de som no rodapé, à direita: botão de mudo (ou tecla M) e o volume do jogo, com a porcentagem ao lado.
    /// Aparece também no menu inicial e com as janelas abertas, quando cabe embaixo do painel.
    /// </summary>
    public partial class GameUI
    {
        /// <summary>Nome do botão de mudo para os testes de interface: na tela ele mostra só o ícone do alto-falante.</summary>
        public const string BotaoMudo = "Mudo";

        /// <summary>Nome da barra de volume para os testes de interface (<see cref="Arrastar"/>).</summary>
        public const string BarraVolume = "Volume";

        static Texture2D iconSom, iconMudo;
        GUIStyle sSliderTrack, sSliderThumb;

        /// <summary>Altura da faixa do rodapé; no toque ela cresce para caber o dedo.</summary>
        static float SoundHeight { get { return InputCompat.IsTouchDevice ? 44f : 30f; } }

        Rect SoundRect
        {
            get
            {
                float w = InputCompat.IsTouchDevice ? 250f : 212f;
                return new Rect(safe.xMax - w, safe.yMax - SoundHeight, w, SoundHeight);
            }
        }

        void DrawSound()
        {
            var r = SoundRect;
            if (IsModal && r.y < panel.yMax + 2f) return; // no iPhone o painel desce até o rodapé: o controle fica só fora das janelas
            InitSoundStyles();
            var som = GameAudio.Settings;
            GUI.Box(r, GUIContent.none, sHudBox);

            var b = new Rect(r.x + 3f, r.y + 3f, r.height + 2f, r.height - 6f);
            if (Pressed(GUI.Button(b, GUIContent.none, sSmallButton), BotaoMudo)) GameAudio.AlternarMudo();
            float s = b.height - 6f;
            GUI.color = som.Silencioso ? new Color(1f, 0.72f, 0.6f) : Pergaminho;
            GUI.DrawTexture(new Rect(b.center.x - s / 2f, b.center.y - s / 2f, s, s), SpeakerIcon(som.Silencioso));
            GUI.color = Color.white;

            var pct = new Rect(r.xMax - 50f, r.y, 44f, r.height);
            var track = new Rect(b.xMax + 10f, r.y, pct.x - b.xMax - 16f, r.height);
            // no mudo a barra fica cinza, mas guarda o nível: é para ele que o som volta
            FillBar(new Rect(track.x, track.center.y - 6f, track.width, 12f), som.Volume, 1f, som.Mudo ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.42f, 0.78f, 0.36f));
            float v = Slider(track, som.Volume, 0f, 1f, sSliderTrack, sSliderThumb, BarraVolume);
            if (v != som.Volume) GameAudio.AjustarVolume(v);
            GUI.Label(pct, som.Mudo ? "mudo" : som.Porcentagem + "%", sHudSmallRight);
        }

        /// <summary>
        /// O slider da IMGUI só cuida do arraste: a trilha é a FillBar desenhada embaixo, e o puxador é um círculo de pergaminho.
        /// O puxador tem a altura do slider menos o padding, por isso o padding centraliza um círculo de lado <c>thumb</c>.
        /// </summary>
        void InitSoundStyles()
        {
            if (sSliderTrack != null) return;
            int thumb = InputCompat.IsTouchDevice ? 26 : 18;
            int pad = ((int)SoundHeight - thumb) / 2;
            sSliderTrack = new GUIStyle { padding = new RectOffset(0, 0, pad, pad) };
            int radius = thumb / 2 - 2;
            sSliderThumb = new GUIStyle { fixedWidth = thumb, border = new RectOffset(radius + 2, radius + 2, radius + 2, radius + 2) };
            sSliderThumb.normal.background = Rounded(Pergaminho, radius, Madeira, 2);
            sSliderThumb.hover.background = Rounded(Color.white, radius, Madeira, 2);
            sSliderThumb.active.background = Rounded(Ouro, radius, Madeira, 2);
        }

        /// <summary>Alto-falante desenhado em código (a fonte da IMGUI não tem emoji): com as ondas do som ou, no mudo, com um X.</summary>
        static Texture2D SpeakerIcon(bool mudo)
        {
            var t = mudo ? iconMudo : iconSom;
            if (t != null) return t;
            const int size = 64, sub = 4;
            t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // 4×4 amostras por pixel: a borda sai suave (antisserrilhado)
                    int hits = 0;
                    for (int i = 0; i < sub; i++)
                        for (int j = 0; j < sub; j++)
                            if (InSpeaker(x + (i + 0.5f) / sub, y + (j + 0.5f) / sub, mudo)) hits++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (sub * sub)));
                }
            t.SetPixels32(px);
            t.Apply();
            if (mudo) iconMudo = t; else iconSom = t;
            return t;
        }

        static bool InSpeaker(float x, float y, bool mudo)
        {
            float dy = Mathf.Abs(y - 32f);
            if (x >= 6f && x <= 20f && dy <= 9f) return true;               // caixa
            if (x >= 19f && x <= 33f && dy <= 9f + (x - 19f)) return true;  // cone
            if (mudo)
            {
                float u = x - 49f, v = y - 32f;
                return Mathf.Abs(u) <= 10f && Mathf.Abs(v) <= 10f && (Mathf.Abs(u - v) <= 4f || Mathf.Abs(u + v) <= 4f);
            }
            float dx = x - 30f, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (dx < d * 0.6f) return false; // só o arco da frente
            return Mathf.Abs(d - 13f) <= 2.5f || Mathf.Abs(d - 22f) <= 2.5f;
        }
    }
}
