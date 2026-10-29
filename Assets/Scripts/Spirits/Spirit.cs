using System.Collections.Generic;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Criatura do folclore. Combate é passivo: ninguém morre, os espíritos são afastados por auras,
    /// amuletos e banidos por defumação. Ao amanhecer eles somem.
    /// </summary>
    public class Spirit : MonoBehaviour
    {
        public static readonly List<Spirit> All = new List<Spirit>();

        public SpiritType Type;
        SpriteRenderer sr;
        Sprite[] frames;
        Vector2 wanderTarget;
        float cooldown;
        float alpha;
        bool leaving;
        float speed;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static string ArtKey(SpiritType t)
        {
            switch (t)
            {
                case SpiritType.Assombracao: return "spirit_alma";
                case SpiritType.Visagem: return "spirit_visagem";
                case SpiritType.Saci: return "spirit_saci";
                case SpiritType.Cuca: return "spirit_cuca";
                case SpiritType.Bernuncia: return "spirit_bernuncia";
                default: return "spirit_olhogordo";
            }
        }

        public static string Nome(SpiritType t)
        {
            switch (t)
            {
                case SpiritType.Assombracao: return "Alma da Lamparina";
                case SpiritType.Visagem: return "Visagem da Neblina";
                case SpiritType.Saci: return "Saci";
                case SpiritType.Cuca: return "Cuca";
                case SpiritType.Bernuncia: return "Bernúncia";
                default: return "Olho-Gordo";
            }
        }

        public void Init(SpiritType type)
        {
            Type = type;
            sr = gameObject.AddComponent<SpriteRenderer>();
            frames = ArtLibrary.Frames(ArtKey(type));
            if (frames.Length > 0) SpriteFactory.SetSprite(sr, frames[0], Height(type));
            else
            {
                sr.color = FallbackColor(type);
                SpriteFactory.SetSprite(sr, SpriteFactory.Circle(), 0.8f);
            }
            speed = Speed(type);
            wanderTarget = transform.position;
            alpha = 0f;
        }

        static float Height(SpiritType t)
        {
            switch (t)
            {
                case SpiritType.Cuca: return 1.6f;
                case SpiritType.Bernuncia: return 1.9f;
                default: return 1.3f;
            }
        }

        static float Speed(SpiritType t)
        {
            switch (t)
            {
                case SpiritType.Saci: return 3.2f;
                case SpiritType.OlhoGordo: return 1.4f;
                case SpiritType.Cuca: return 2.1f;
                case SpiritType.Bernuncia: return 1.7f;
                default: return 1.8f;
            }
        }

        static Color FallbackColor(SpiritType t)
        {
            switch (t)
            {
                case SpiritType.Cuca: return new Color(0.42f, 0.62f, 0.3f);
                case SpiritType.Bernuncia: return new Color(0.9f, 0.45f, 0.2f);
                case SpiritType.Assombracao: return new Color(0.92f, 0.88f, 1f);
                case SpiritType.Visagem: return new Color(0.6f, 0.8f, 0.78f);
                case SpiritType.Saci: return new Color(0.84f, 0.2f, 0.17f);
                default: return new Color(0.45f, 0.75f, 0.3f);
            }
        }

        public void Banish()
        {
            GameManager.I.UI.Toast(Nome(Type) + " foi banido(a) pela fumaça!");
            Destroy(gameObject);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.UI.PausesWorld) return;
            var s = gm.Session;
            var st = s.Status;
            float dt = Time.deltaTime;

            if (!s.Clock.IsNight && !st.Assombrado) leaving = true;

            bool visible = ProtectionRules.IsVisible(Type, st);
            float targetAlpha = leaving ? 0f : (visible ? 1f : 0f);
            alpha = Mathf.MoveTowards(alpha, targetAlpha, dt * 1.5f);
            if (leaving && alpha <= 0f) { Destroy(gameObject); return; }

            Vector2 pos = transform.position;
            Vector2 player = gm.Player.transform.position;
            Vector2 toPlayer = player - pos;
            float dist = toPlayer.magnitude;
            float aggro = ProtectionRules.AggroRadius(st);
            bool flee = st.ProtegidoPorAmuleto && ProtectionRules.AmuletoRepele(Type);

            // a Bernúncia tem fome: vai primeiro para a horta (a jogadora só se estiver no caminho)
            FarmPlot meal = Type == SpiritType.Bernuncia && !leaving && dist > 2.5f ? Meal(pos) : null;

            Vector2 vel;
            if (meal != null) vel = ((Vector2)meal.transform.position - pos).normalized * speed;
            else if (dist < aggro && flee) vel = -toPlayer.normalized * speed;
            else if (dist < aggro)
            {
                if (Type == SpiritType.OlhoGordo && dist < ProtectionRules.OlhoGordoAlcance * 0.8f) vel = Vector2.zero;
                else vel = toPlayer.normalized * speed;
            }
            else
            {
                if (Vector2.Distance(pos, wanderTarget) < 0.3f)
                    wanderTarget = pos + Random.insideUnitCircle * 4f;
                vel = speed * 0.4f * (wanderTarget - pos).normalized;
            }

            Vector2 next = ProtectionAura.PushOut(pos + vel * dt, Type);
            next = gm.ClampToWorld(next);
            float bob = Type == SpiritType.Saci ? 0f : Mathf.Sin(Time.time * 2f + GetInstanceID()) * 0.004f;
            transform.position = new Vector3(next.x, next.y + bob, 0f);

            if (frames.Length > 1)
            {
                int f = (int)(Time.time * (Type == SpiritType.Saci ? 6f : 2.5f)) % frames.Length;
                sr.sprite = frames[f];
            }
            if (Mathf.Abs(vel.x) > 0.05f) sr.flipX = vel.x < 0f;
            var c = sr.color;
            c.a = alpha * (Type == SpiritType.Visagem ? 0.75f : 1f);
            sr.color = c;
            sr.sortingOrder = SpriteFactory.YOrder(next.y, 50);

            cooldown -= dt;
            if (leaving || (alpha < 0.3f && visible)) return;
            if (meal != null && Vector2.Distance(next, meal.transform.position) < 0.5f)
            {
                s.OnBernunciaDevour(meal.Model);
                leaving = true; // de barriga cheia, vai embora
                return;
            }
            ApplyEffect(s, dist);
        }

        /// <summary>O canteiro plantado mais perto que nenhuma barreira protege (os de dentro dela a Bernúncia não alcança).</summary>
        static FarmPlot Meal(Vector2 from)
        {
            FarmPlot best = null;
            float bestD = float.MaxValue;
            foreach (var i in Interactable.All)
            {
                var plot = i as FarmPlot;
                if (plot == null || plot.Model == null || plot.Model.IsEmpty) continue;
                if (ProtectionAura.Inside(plot.transform.position, AuraType.Barreira)) continue;
                float d = Vector2.Distance(from, plot.transform.position);
                if (d < bestD) { best = plot; bestD = d; }
            }
            return best;
        }

        void ApplyEffect(GameSession s, float dist)
        {
            var ui = GameManager.I.UI;
            switch (Type)
            {
                case SpiritType.Assombracao:
                    if (dist < 0.8f && cooldown <= 0f)
                    {
                        s.OnHaunted();
                        ui.Toast("A Alma da Lamparina te atravessou! Brrr...");
                        cooldown = 3f;
                        transform.position += (Vector3)(Random.insideUnitCircle.normalized * 2f);
                    }
                    else if (dist < 3f) s.Status.LoseSanity(1.5f * Time.deltaTime);
                    break;
                case SpiritType.Visagem:
                    if (dist < 2.5f) s.OnVisagemNear(Time.deltaTime);
                    break;
                case SpiritType.Saci:
                    if (dist < 0.8f)
                    {
                        s.OnSaciSteal();
                        if (s.Status.Has(Ailment.VontadeDeFumar)) s.Status.LoseSanity(10f); // o cheiro do cachimbo...
                        Destroy(gameObject);
                    }
                    break;
                case SpiritType.Cuca:
                    if (dist < 0.8f)
                    {
                        s.OnCuca();
                        leaving = true; // já deu o recado
                    }
                    break;
                case SpiritType.Bernuncia:
                    if (dist < 0.9f)
                    {
                        s.OnBernunciaSwallow();
                        GameManager.I.SpitAtHome();
                        leaving = true;
                    }
                    break;
                case SpiritType.OlhoGordo:
                    if (dist < ProtectionRules.OlhoGordoAlcance && cooldown <= 0f)
                    {
                        bool inside = ProtectionAura.Inside(GameManager.I.Player.transform.position, AuraType.AntiOlhoGordo);
                        if (s.OnEvilEye(inside)) ui.Toast("O Olho-Gordo botou mau-olhado em você!");
                        else ui.Toast("O Olho-Gordo tentou... mas sua proteção segurou.");
                        cooldown = 7f;
                    }
                    break;
            }
        }
    }
}
