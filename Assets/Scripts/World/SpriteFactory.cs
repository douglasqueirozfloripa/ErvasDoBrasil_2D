using System.Collections.Generic;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Sprites procedurais de reserva (enquanto não existe arte) e utilitários de desenho.</summary>
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<Color, Texture2D> solid = new Dictionary<Color, Texture2D>();

        public static Sprite Circle(int size = 64) { return Shape("circle", size, true); }
        public static Sprite Square(int size = 16) { return Shape("square", size, false); }

        static Sprite Shape(string name, int size, bool circle)
        {
            string key = name + size;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = size / 2f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    if (circle)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        a = Mathf.Clamp01(r - d);
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cache[key] = s;
            return s;
        }

        /// <summary>
        /// Canteiro de terra: retângulo arredondado com contorno escuro (bruto com pedrinhas, preparado com sulcos).
        /// O miolo é claro para ser tingido pela cor do SpriteRenderer (terra seca, preparada, molhada).
        /// </summary>
        public static Sprite Bed(bool sulcos) { return Rounded(sulcos ? "canteiro_sulcos" : "canteiro", sulcos ? 1 : 0, 0.24f, 1f); }

        /// <summary>Moldura da mira (canteiro que a ação vai atingir, lugar onde a pá vai cavar): borda cheia, miolo quase transparente.</summary>
        public static Sprite Frame() { return Rounded("mira", 2, 1f, 0.2f); }

        const int BedW = 98, BedH = 80;
        const float BedRadius = 12f, BedBorder = 4f;

        static Sprite Rounded(string key, int pattern, float edge, float fillAlpha)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(BedW, BedH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[BedW * BedH];
            for (int y = 0; y < BedH; y++)
                for (int x = 0; x < BedW; x++)
                {
                    // distância até a borda do retângulo arredondado (negativa por dentro)
                    float qx = Mathf.Abs(x + 0.5f - BedW / 2f) - (BedW / 2f - BedRadius);
                    float qy = Mathf.Abs(y + 0.5f - BedH / 2f) - (BedH / 2f - BedRadius);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - BedRadius;
                    float a = Mathf.Clamp01(0.5f - d);
                    bool contorno = d > -BedBorder - (pattern == 2 ? 1f : 0f);
                    float v = contorno ? edge : Miolo(x, y, pattern);
                    px[y * BedW + x] = new Color(v, v, v, contorno ? a : a * fillAlpha);
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, BedW, BedH), new Vector2(0.5f, 0.5f), BedH);
            cache[key] = s;
            return s;
        }

        static float Miolo(int x, int y, int pattern)
        {
            if (pattern == 1) return y % 18 < 5 ? 0.72f : (y % 18 == 5 ? 0.86f : 1f);   // sulcos
            if (pattern == 0 && (x * 7 + y * 13) % 37 == 0) return 0.78f;               // pedrinhas
            return 1f;
        }

        /// <summary>Textura 1x1 de cor sólida (para a UI IMGUI).</summary>
        public static Texture2D Solid(Color c)
        {
            Texture2D t;
            if (solid.TryGetValue(c, out t) && t != null) return t;
            t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            solid[c] = t;
            return t;
        }

        public static Color Hex(string hex, Color fallback)
        {
            Color c;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c) ? c : fallback;
        }

        /// <summary>Cria um GameObject com SpriteRenderer, ajustando a escala para a altura desejada no mundo.</summary>
        public static SpriteRenderer Create(string name, Transform parent, Sprite sprite, Vector2 pos, float worldHeight, Color color, int order = 0)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            sr.sortingOrder = order;
            SetSprite(sr, sprite, worldHeight);
            return sr;
        }

        public static void SetSprite(SpriteRenderer sr, Sprite sprite, float worldHeight)
        {
            sr.sprite = sprite;
            if (sprite == null) return;
            float h = sprite.bounds.size.y;
            float s = h > 0.0001f ? worldHeight / h : 1f;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Ordenação top-down: quem está mais embaixo na tela fica na frente.</summary>
        public static int YOrder(float y, int offset = 0) { return offset - Mathf.RoundToInt(y * 100f); }

        /// <summary>
        /// Base das camadas de chão (mapa, mata, terra, anel da aura). Fica abaixo de qualquer YOrder:
        /// no topo do mapa (y ≈ 12) o YOrder chega a −1200, e com o chão em −1000 a jogadora sumia atrás dele.
        /// </summary>
        public const int ChaoOrder = -32000;

        /// <summary>Desenha um sprite (respeitando o recorte dele na textura) num Rect da IMGUI.</summary>
        public static void DrawSprite(Rect r, Sprite s, bool keepAspect = true)
        {
            if (s == null || s.texture == null) return;
            var tr = s.textureRect;
            var tex = s.texture;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            if (keepAspect)
            {
                float aspect = tr.width / tr.height;
                if (r.width / r.height > aspect) { float w = r.height * aspect; r = new Rect(r.x + (r.width - w) / 2f, r.y, w, r.height); }
                else { float h = r.width / aspect; r = new Rect(r.x, r.y + (r.height - h) / 2f, r.width, h); }
            }
            GUI.DrawTextureWithTexCoords(r, tex, uv, true);
        }
    }
}
