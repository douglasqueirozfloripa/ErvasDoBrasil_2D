using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Comemoração de subir de nível (uns 6 s), toda desenhada na IMGUI e com som gerado em código:
    /// balões subindo, fogos de artifício e, no centro, a troca do número como uma planta: o número antigo murcha e
    /// afunda na terra, o novo brota dela (verde que amadurece em dourado, com folhas se abrindo e uma florada),
    /// enquanto duas plantas liberadas no nível crescem dos lados pelos 5 estágios da arte do jogo.
    /// </summary>
    public partial class GameUI
    {
        const float LevelUpSeconds = 6.5f;
        float levelUpAt = -100f;
        int levelUpNivel, levelUpAntes;
        string levelUpNovas;
        string[] levelUpPlantas = { "mulungu", "mulungu" };
        AudioSource sfx;
        static AudioClip levelUpClip, fogosClip;
        GUIStyle sLevelNumber, sLevelHeader;

        sealed class Balao { public float X, Atraso, Velocidade, Tamanho, Fase; public Color Cor; }
        sealed class Fogo { public float Em, DeX, ParaX, ParaY; public Color Cor; public int Faiscas; public bool Tocou; }
        readonly List<Balao> baloes = new List<Balao>();
        readonly List<Fogo> fogos = new List<Fogo>();

        static readonly Color[] CoresDeFesta =
        {
            new Color(0.92f, 0.26f, 0.24f), new Color(1f, 0.8f, 0.2f), new Color(0.26f, 0.56f, 0.95f), new Color(0.36f, 0.76f, 0.34f),
            new Color(0.96f, 0.45f, 0.7f), new Color(1f, 0.56f, 0.2f), new Color(0.62f, 0.42f, 0.88f),
        };
        static readonly Color Terra = new Color(0.42f, 0.27f, 0.14f);
        static readonly Color Broto = new Color(0.42f, 0.78f, 0.32f);
        static readonly Color Folhagem = new Color(0.28f, 0.6f, 0.24f);

        /// <summary>Testes: a comemoração está na tela.</summary>
        public bool CelebrandoNivel { get { float t = Time.unscaledTime - levelUpAt; return t >= 0f && t <= LevelUpSeconds; } }

        /// <summary>Ligado ao evento Progression.LevelUp pelo GameManager.</summary>
        public void CelebrateLevel(int nivel)
        {
            // dois níveis de uma vez: o número que murcha continua sendo o de antes da festa
            if (!CelebrandoNivel) levelUpAntes = nivel - 1;
            levelUpAt = Time.unscaledTime;
            levelUpNivel = nivel;
            var novas = S.Db.Plants.Where(p => p.NivelMinimo == nivel).ToList();
            levelUpNovas = novas.Count > 0 ? "Novas plantas na feira e na mata: " + string.Join(", ", novas.Select(p => p.nome).ToArray()) : "Suas mãos de raizeira estão mais sábias.";
            // as plantas que crescem dos lados: as liberadas agora que têm arte (senão o mulungu, a árvore do enredo)
            var comArte = novas.Select(p => p.id).Where(id => ArtLibrary.Get("plant_" + id + "_4") != null).ToList();
            if (comArte.Count == 0) comArte.Add("mulungu");
            levelUpPlantas = new[] { comArte[0], comArte[comArte.Count > 1 ? 1 : 0] };
            PrepararFesta(nivel);

            if (sfx == null)
            {
                sfx = gameObject.AddComponent<AudioSource>();
                sfx.playOnAwake = false;
                sfx.spatialBlend = 0f;
            }
            sfx.PlayOneShot(LevelUpClip(), 0.7f);
        }

        void PrepararFesta(int nivel)
        {
            var rnd = new System.Random(nivel * 7919 + Time.frameCount);
            float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }

            baloes.Clear();
            for (int i = 0; i < 14; i++)
            {
                baloes.Add(new Balao
                {
                    X = (i + R(0.15f, 0.85f)) / 14f, // espalhados pela largura, sem amontoar
                    Atraso = R(0f, 1.3f),
                    Velocidade = R(170f, 240f),
                    Tamanho = R(34f, 48f),
                    Fase = R(0f, 6.28f),
                    Cor = CoresDeFesta[i % CoresDeFesta.Length]
                });
            }

            fogos.Clear();
            float[] quando = { 0.45f, 1.0f, 1.55f, 2.15f, 2.8f, 3.5f, 4.1f };
            for (int i = 0; i < quando.Length; i++)
            {
                // estouram onde a tela está livre: dos lados da faixa (abaixo do HUD e dos avisos) e acima do centro
                float x, y;
                switch (i % 3)
                {
                    case 0: x = R(0.08f, 0.26f); y = R(0.42f, 0.6f); break;
                    case 1: x = R(0.74f, 0.92f); y = R(0.42f, 0.6f); break;
                    default: x = R(0.4f, 0.6f); y = R(0.07f, 0.1f); break;
                }
                fogos.Add(new Fogo
                {
                    Em = quando[i] + R(-0.1f, 0.1f),
                    DeX = x + R(-0.05f, 0.05f),
                    ParaX = x,
                    ParaY = y,
                    Cor = Color.Lerp(CoresDeFesta[(i * 3 + 1) % CoresDeFesta.Length], Color.white, 0.2f),
                    Faiscas = 32
                });
            }
        }

        // ───────── Sons (gerados uma vez) ─────────

        /// <summary>Arpejo dó-mi-sol-dó com um sininho no fim.</summary>
        static AudioClip LevelUpClip()
        {
            if (levelUpClip != null) return levelUpClip;
            const int rate = 44100;
            float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float passo = 0.11f, dur = 1.2f;
            var data = new float[(int)(rate * dur)];
            for (int n = 0; n < notas.Length; n++)
            {
                int start = (int)(n * passo * rate);
                bool ultima = n == notas.Length - 1;
                for (int i = start; i < data.Length; i++)
                {
                    float t = (i - start) / (float)rate;
                    float env = Mathf.Exp(-t * (ultima ? 3.5f : 9f)) * Mathf.Clamp01(t * 200f);
                    float f = notas[n];
                    data[i] += env * 0.22f * (Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * f * t));
                }
            }
            levelUpClip = AudioClip.Create("subiu_de_nivel", data.Length, 1, rate, false);
            levelUpClip.SetData(data, 0);
            return levelUpClip;
        }

        /// <summary>Um fogo: assobio subindo (0,45 s), estouro de ruído e estalinhos caindo.</summary>
        static AudioClip FogosClip()
        {
            if (fogosClip != null) return fogosClip;
            const int rate = 44100;
            const float subida = 0.45f, dur = 1.6f;
            var data = new float[(int)(rate * dur)];
            var rnd = new System.Random(31);
            float fase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                if (t < subida)
                {
                    // fiu: seno que sobe de 700 para 1600 Hz
                    float f = Mathf.Lerp(700f, 1600f, t / subida);
                    fase += 2f * Mathf.PI * f / rate;
                    data[i] = 0.07f * Mathf.Sin(fase) * Mathf.Clamp01(t * 30f);
                }
                else
                {
                    // pou: ruído que decai rápido, e depois os estalinhos esparsos
                    float tb = t - subida;
                    float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    float pou = ruido * 0.5f * Mathf.Exp(-tb * 14f);
                    float estalo = rnd.NextDouble() < 0.004 * Mathf.Exp(-tb * 2f) ? ruido * 0.35f : 0f;
                    data[i] = pou + estalo;
                }
            }
            fogosClip = AudioClip.Create("fogos", data.Length, 1, rate, false);
            fogosClip.SetData(data, 0);
            return fogosClip;
        }

        // ───────── Desenho ─────────

        /// <summary>
        /// Gira e escala em volta de um ponto da tela virtual. Vai multiplicado à direita da GUI.matrix (que já tem a escala da tela);
        /// o GUIUtility.RotateAroundPivot põe o pivô em pixels da tela e erra o lugar quando a GUI está escalada.
        /// </summary>
        static Matrix4x4 Around(Vector2 c, float angle, float scale) { return Around(c, angle, scale, scale); }

        static Matrix4x4 Around(Vector2 c, float angle, float sx, float sy)
        {
            // escala 0 (folha fechada, número afundado) dá matriz sem inversa, e a Unity recusa a GUI.matrix com erro
            sx = Mathf.Abs(sx) < 0.001f ? 0.001f : sx;
            sy = Mathf.Abs(sy) < 0.001f ? 0.001f : sy;
            return Matrix4x4.TRS(c, Quaternion.Euler(0f, 0f, angle), new Vector3(sx, sy, 1f)) * Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);
        }

        static float EaseOut(float x) { return 1f - (1f - x) * (1f - x) * (1f - x); }

        /// <summary>Passa um pouco do fim e volta (o broto que estica e assenta).</summary>
        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float k = x - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }

        static void Ellipse(Vector2 c, float w, float h, Color col)
        {
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - w / 2f, c.y - h / 2f, w, h), SpriteFactory.Circle(128).texture);
        }

        void InitLevelStyles()
        {
            if (sLevelNumber != null) return;
            sLevelNumber = Label(96, Ouro, FontStyle.Bold, TextAnchor.LowerCenter, false);
            sLevelHeader = Label(28, Ouro, FontStyle.Bold, TextAnchor.MiddleCenter, false);
        }

        void DrawLevelUp(float vw)
        {
            float t = Time.unscaledTime - levelUpAt;
            if (t < 0f || t > LevelUpSeconds) return;
            InitLevelStyles();
            float fade = t > LevelUpSeconds - 0.7f ? (LevelUpSeconds - t) / 0.7f : 1f;
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;

            // clarão dourado na tela inteira
            if (t < 0.5f)
            {
                GUI.color = new Color(Ouro.r, Ouro.g, Ouro.b, 0.25f * (1f - t / 0.5f));
                GUI.DrawTexture(new Rect(0, 0, vw, VirtualHeight), Texture2D.whiteTexture);
            }

            DrawFogos(t, vw, fade);
            DrawBrilhoNaRaizeira(t);
            DrawBaloes(t, vw, fade, oldMatrix);
            DrawFaixa(t, fade, oldMatrix);

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        void DrawBrilhoNaRaizeira(float t)
        {
            var cam = Camera.main;
            if (cam == null || Player == null || t > 2.2f) return;
            var sp = cam.WorldToScreenPoint(Player.transform.position + new Vector3(0f, 0.8f, 0f));
            if (sp.z <= 0f) return;
            var g = ToGui(sp);
            float k = t / 2.2f;
            float halo = 40f + 90f * EaseOut(Mathf.Clamp01(t / 0.8f));
            Ellipse(g, halo * 2f, halo * 2f, new Color(Ouro.r, Ouro.g, Ouro.b, 0.35f * (1f - k)));
            for (int i = 0; i < 12; i++)
            {
                float ang = i * Mathf.PI * 2f / 12f + t * 2.5f;
                float rad = 30f + 50f * k;
                var p = new Vector2(g.x + Mathf.Cos(ang) * rad, g.y + Mathf.Sin(ang) * rad * 0.5f - 120f * k);
                float s = 14f * (1f - k) + 4f;
                Ellipse(p, s, s, new Color(1f, 0.95f, 0.6f, 1f - k));
            }
        }

        void DrawBaloes(float t, float vw, float fade, Matrix4x4 oldMatrix)
        {
            foreach (var b in baloes)
            {
                float tb = t - b.Atraso;
                if (tb < 0f) continue;
                float w = b.Tamanho, h = b.Tamanho * 1.2f;
                var c = new Vector2(b.X * vw + Mathf.Sin(tb * 1.6f + b.Fase) * 18f, VirtualHeight + 70f - b.Velocidade * tb);
                if (c.y < -h - 90f) continue;

                // barbante balançando embaixo do nó
                for (int s = 0; s < 10; s++)
                {
                    float sy = c.y + h / 2f + 4f + s * 7f;
                    float sx = c.x + Mathf.Sin(tb * 3f + b.Fase + s * 0.5f) * (s * 0.6f);
                    GUI.color = new Color(1f, 1f, 1f, 0.55f * fade);
                    GUI.DrawTexture(new Rect(sx - 0.75f, sy, 1.5f, 7.5f), Texture2D.whiteTexture);
                }

                // corpo inclinado pelo vento, brilho e nó
                GUI.matrix = oldMatrix * Around(c, Mathf.Sin(tb * 1.6f + b.Fase) * 7f, 1f);
                var cor = new Color(b.Cor.r, b.Cor.g, b.Cor.b, 0.95f * fade);
                Ellipse(c, w, h, cor);
                Ellipse(new Vector2(c.x - w * 0.2f, c.y - h * 0.22f), w * 0.22f, h * 0.3f, new Color(1f, 1f, 1f, 0.5f * fade));
                Ellipse(new Vector2(c.x, c.y + h / 2f + 1f), 9f, 7f, Color.Lerp(cor, Color.black, 0.25f));
                GUI.matrix = oldMatrix;
            }
        }

        void DrawFogos(float t, float vw, float fade)
        {
            const float subida = 0.45f, vida = 1.6f;
            foreach (var f in fogos)
            {
                float tf = t - f.Em;
                if (tf < 0f) continue;
                if (!f.Tocou && Event.current.type == EventType.Repaint && sfx != null)
                {
                    f.Tocou = true;
                    sfx.PlayOneShot(FogosClip(), 0.35f);
                }
                var de = new Vector2(f.DeX * vw, VirtualHeight + 10f);
                var para = new Vector2(f.ParaX * vw, f.ParaY * VirtualHeight);

                if (tf < subida)
                {
                    // foguete subindo com um rastro que apaga
                    for (int k = 0; k < 6; k++)
                    {
                        float tk = Mathf.Max(0f, tf - k * 0.03f);
                        var p = Vector2.Lerp(de, para, EaseOut(tk / subida));
                        float s = 7f - k;
                        Ellipse(p, s, s, new Color(1f, 0.9f, 0.6f, (1f - k / 6f) * fade));
                    }
                    continue;
                }

                float tb = tf - subida;
                if (tb > vida) continue;
                if (tb < 0.25f) Ellipse(para, 110f * (1f - tb / 0.25f) + 20f, 110f * (1f - tb / 0.25f) + 20f, new Color(f.Cor.r, f.Cor.g, f.Cor.b, 0.45f * (1f - tb / 0.25f)));
                float vidaRestante = 1f - tb / vida;
                for (int i = 0; i < f.Faiscas; i++)
                {
                    float ang = i * Mathf.PI * 2f / f.Faiscas + (i % 2) * 0.12f;
                    var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    float v = 170f + 60f * (i % 3);
                    // brilho piscando no fim, como os fogos de verdade
                    float pisca = tb > 0.8f ? 0.55f + 0.45f * Mathf.Sin(tb * 40f + i) : 1f;
                    for (int k = 0; k < 3; k++)
                    {
                        float tk = Mathf.Max(0f, tb - k * 0.05f);
                        var p = para + v * tk * (1f - tk * 0.3f) * dir + new Vector2(0f, 70f * tk * tk);
                        float s = (10f - k * 3f) * (0.5f + 0.5f * vidaRestante);
                        Ellipse(p, s, s, new Color(f.Cor.r, f.Cor.g, f.Cor.b, vidaRestante * pisca * fade * (1f - k * 0.3f)));
                        // miolo branco na cabeça da faísca: brilha mais que a cor
                        if (k == 0) Ellipse(p, s * 0.45f, s * 0.45f, new Color(1f, 1f, 1f, vidaRestante * pisca * fade));
                    }
                }
            }
        }

        void DrawFaixa(float t, float fade, Matrix4x4 oldMatrix)
        {
            var center = new Vector2(safe.center.x, safe.y + safe.height * 0.32f);
            float pop = t < 0.35f ? Mathf.Lerp(0.4f, 1.1f, EaseOut(t / 0.35f)) : Mathf.Lerp(1.1f, 1f, Mathf.Clamp01((t - 0.35f) / 0.2f));
            var baseMatrix = oldMatrix * Around(center, 0f, pop);
            GUI.matrix = baseMatrix;

            var banner = new Rect(center.x - 320f, center.y - 115f, 640f, 230f);
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.Box(banner, GUIContent.none, sLevelBanner);
            GUI.Label(new Rect(banner.x, banner.y + 10f, banner.width, 38f), "SUBIU DE NÍVEL!", sLevelHeader);
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.Label(new Rect(banner.x + 24f, banner.yMax - 52f, banner.width - 48f, 46f), levelUpNovas, sLevelText);

            // o canteiro do número
            float cx = banner.center.x, chao = banner.y + 168f;
            Ellipse(new Vector2(cx, chao + 4f), 150f, 26f, new Color(Terra.r, Terra.g, Terra.b, fade));
            Ellipse(new Vector2(cx, chao), 128f, 16f, new Color(0.52f, 0.35f, 0.19f, fade));

            DrawPlantasDoLado(t, cx, chao, fade);
            DrawNumeroBrotando(t, cx, chao, fade, baseMatrix);
        }

        /// <summary>As duas plantas dos lados crescem pelos estágios 0 → 4 da arte do jogo, com um saltinho a cada estágio.</summary>
        void DrawPlantasDoLado(float t, float cx, float chao, float fade)
        {
            for (int lado = 0; lado < 2; lado++)
            {
                float px = cx + (lado == 0 ? -190f : 190f);
                Ellipse(new Vector2(px, chao + 4f), 84f, 18f, new Color(Terra.r, Terra.g, Terra.b, fade));
                float tp = t - 0.9f - lado * 0.15f;
                if (tp < 0f) continue;
                int estagio = Mathf.Min(4, (int)(tp / 0.32f));
                float desdeTroca = tp - estagio * 0.32f;
                float salto = estagio < 4 || desdeTroca < 0.2f ? 1f + 0.15f * Mathf.Clamp01(1f - desdeTroca / 0.18f) : 1f;
                float alto = (34f + estagio * 18f) * salto;
                var sprite = ArtLibrary.Get("plant_" + levelUpPlantas[lado] + "_" + estagio);
                GUI.color = new Color(1f, 1f, 1f, fade);
                if (sprite != null) SpriteFactory.DrawSprite(new Rect(px - alto / 2f, chao - alto + 4f, alto, alto), sprite);
            }

        }

        /// <summary>O número de antes murcha e afunda; o novo brota da mesma terra, abre folhas, amadurece em dourado e floresce.</summary>
        void DrawNumeroBrotando(float t, float cx, float chao, float fade, Matrix4x4 baseMatrix)
        {
            var pe = new Vector2(cx, chao + 2f);
            var caixa = new Rect(cx - 90f, chao - 112f, 180f, 114f);

            // 1) o número de antes murcha, fica marrom, entorta e afunda na terra
            float murcha = Mathf.Clamp01((t - 0.55f) / 0.55f);
            if (murcha < 1f)
            {
                GUI.matrix = baseMatrix * Around(pe, -20f * murcha, 1f - 0.25f * murcha, 1f - murcha);
                GUI.color = new Color(1f, 1f, 1f, fade);
                var cor = Color.Lerp(Pergaminho, new Color(0.45f, 0.3f, 0.15f), murcha);
                sLevelNumber.normal.textColor = cor;
                GUI.Label(caixa, levelUpAntes.ToString(System.Globalization.CultureInfo.InvariantCulture), sLevelNumber);
                GUI.matrix = baseMatrix;
            }

            // 2) o novo brota: estica passando um pouco do tamanho, assenta e vai de verde-broto a dourado
            float cresce = Mathf.Clamp01((t - 1.2f) / 1.1f);
            if (cresce > 0f)
            {
                float sy = EaseOutBack(cresce);
                float sx = Mathf.Lerp(0.45f, 1f, EaseOut(cresce));
                float balanco = Mathf.Sin(t * 2.2f) * 4f * cresce;

                // brilho atrás do número maduro
                if (cresce >= 1f) Ellipse(new Vector2(cx, chao - 56f), 170f, 150f, new Color(Ouro.r, Ouro.g, Ouro.b, (0.18f + 0.07f * Mathf.Sin(t * 5f)) * fade));

                // folhas que se abrem da base para os lados
                float abre = Mathf.Clamp01((cresce - 0.15f) / 0.6f);
                for (int lado = -1; abre > 0f && lado <= 1; lado += 2)
                {
                    var baseFolha = new Vector2(cx + lado * 22f, chao - 6f);
                    float ang = lado * (10f + 55f * EaseOut(abre)) + balanco;
                    GUI.matrix = baseMatrix * Around(baseFolha, ang, abre);
                    Ellipse(new Vector2(baseFolha.x, baseFolha.y - 24f), 22f, 50f, new Color(Folhagem.r, Folhagem.g, Folhagem.b, fade));
                    GUI.color = new Color(0.18f, 0.42f, 0.15f, fade);
                    GUI.DrawTexture(new Rect(baseFolha.x - 1f, baseFolha.y - 44f, 2f, 40f), Texture2D.whiteTexture);
                }

                GUI.matrix = baseMatrix * Around(pe, balanco, sx, sy);
                GUI.color = new Color(1f, 1f, 1f, fade);
                sLevelNumber.normal.textColor = Color.Lerp(Broto, Ouro, Mathf.Clamp01((cresce - 0.4f) / 0.6f));
                GUI.Label(caixa, levelUpNivel.ToString(System.Globalization.CultureInfo.InvariantCulture), sLevelNumber);
                GUI.matrix = baseMatrix;

                // 3) florada: pétalas abrem em volta do topo quando o número amadurece
                float flor = (t - 2.3f) / 0.9f;
                if (flor > 0f && flor < 1f)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * Mathf.PI / 4f + flor;
                        var p = new Vector2(cx, chao - 70f) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f) * (40f + 70f * EaseOut(flor));
                        float s = 14f * (1f - flor) + 4f;
                        Ellipse(p, s, s, new Color(i % 2 == 0 ? 1f : 0.98f, i % 2 == 0 ? 0.82f : 0.55f, i % 2 == 0 ? 0.9f : 0.7f, (1f - flor) * fade));
                    }
                }
            }
        }
    }
}
