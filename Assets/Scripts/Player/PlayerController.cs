using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>A raizeira: movimento top-down, animação de 4 direções, ações e interação.</summary>
    public class PlayerController : MonoBehaviour
    {
        public const float BaseSpeed = 4f;
        public const float Height = 1.6f;

        SpriteRenderer sr;
        Sprite[] down, up, right;
        Sprite idle;
        Sprite actionSprite;
        float actionUntil;
        float animTime;
        Vector2 facing = Vector2.down;
        float wobbleSeed;

        public Interactable Current { get; private set; }

        // pá: parada e sem nada para fazer por perto, a raizeira mira a casa da grade à frente
        SpriteRenderer digMark;
        float stillFor;
        public bool CanDig { get; private set; }
        public Vector2 DigCell { get; private set; }

        /// <summary>Texto do botão de ação: o do objeto mais perto ou, com a pá, cavar um canteiro novo.</summary>
        public string ActionPrompt { get { return Current != null ? Current.Prompt : (CanDig ? "Cavar canteiro aqui" : null); } }

        /// <summary>Leva a jogadora até um ponto do mapa (a Bernúncia a cospe na porta de casa).</summary>
        public void Teleport(Vector2 p)
        {
            var gm = GameManager.I;
            if (gm != null) p = gm.ClampToWorld(p);
            transform.position = new Vector3(p.x, p.y, 0f);
            facing = Vector2.down;
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(p.x, p.y, cam.transform.position.z);
        }

        public void Init()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            down = ArtLibrary.Frames("player_down");
            up = ArtLibrary.Frames("player_up");
            right = ArtLibrary.Frames("player_right");
            idle = ArtLibrary.Get("player_idle");
            wobbleSeed = Random.value * 10f;

            if (down.Length == 0)
            {
                // reserva sem arte: bonequinha de avental verde
                sr.color = new Color(0.35f, 0.55f, 0.3f);
                SpriteFactory.SetSprite(sr, SpriteFactory.Circle(), 0.8f);
                var head = SpriteFactory.Create("Cabeca", transform, SpriteFactory.Circle(), transform.position + new Vector3(0, 0.55f, 0), 0.45f, new Color(0.55f, 0.35f, 0.22f), 1);
                head.transform.localPosition = new Vector3(0, 0.7f, 0);
            }
            else SpriteFactory.SetSprite(sr, down[0], Height);
        }

        /// <summary>Mostra um quadro de ação (colhendo, pilando) por alguns segundos.</summary>
        public void PlayAction(string action, float seconds)
        {
            var s = ArtLibrary.Get("player_" + action);
            if (s == null) return;
            actionSprite = s;
            actionUntil = Time.time + seconds;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var st = gm.Session.Status;

            Vector2 move = gm.UI.IsModal ? Vector2.zero : InputCompat.Move();

            // Náusea: passos tortos
            if (st.Has(Core.Ailment.Enjoo) && move.sqrMagnitude > 0.01f)
                move = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2.3f + wobbleSeed) * 25f) * move;

            float speed = BaseSpeed * st.SpeedMultiplier;
            Vector2 pos = (Vector2)transform.position + speed * Time.deltaTime * move;
            pos = gm.ClampToWorld(pos);
            transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.sortingOrder = SpriteFactory.YOrder(pos.y, 10);

            Animate(move);

            Current = gm.UI.IsModal ? null : Interactable.Nearest(pos, facing);
            stillFor = move.sqrMagnitude > 0.01f ? 0f : stillFor + Time.deltaTime;
            UpdateDig(gm, pos);
            if (InputCompat.Pressed(GameKey.Interagir))
            {
                if (Current != null) Current.Interact(this);
                else if (CanDig) gm.Dig(DigCell);
            }
        }

        /// <summary>
        /// Com a pá, parada há um instante e sem objeto por perto: mostra a moldura do canteiro que vai cavar à frente.
        /// Andando a moldura some, para não seguir a jogadora pelo mapa inteiro.
        /// </summary>
        void UpdateDig(GameManager gm, Vector2 pos)
        {
            CanDig = false;
            if (gm.Session.TemPa && Current == null && !gm.UI.IsModal && stillFor > 0.2f)
            {
                // para cima a casa fica um pouco mais longe, senão o canteiro some atrás do corpo
                var ahead = pos + facing * (facing.y > 0f ? 1.2f : 0.9f);
                DigCell = new Vector2(FarmLayout.SnapX(ahead.x), FarmLayout.SnapY(ahead.y));
                CanDig = gm.CanDigAt(DigCell);
            }
            if (digMark == null)
            {
                digMark = SpriteFactory.Create("Mira da pá", null, SpriteFactory.Frame(), Vector2.zero, FarmPlot.Tamanho + 0.12f, FarmPlot.MiraCor, SpriteFactory.ChaoOrder + 510);
                digMark.gameObject.SetActive(false);
            }
            digMark.gameObject.SetActive(CanDig);
            if (!CanDig) return;
            digMark.transform.position = DigCell;
            digMark.color = new Color(FarmPlot.MiraCor.r, FarmPlot.MiraCor.g, FarmPlot.MiraCor.b, 0.55f + 0.35f * Mathf.Sin(Time.time * 5f));
        }

        void Animate(Vector2 move)
        {
            if (down.Length == 0) return;

            if (Time.time < actionUntil && actionSprite != null)
            {
                SetFrame(actionSprite, false);
                return;
            }

            bool moving = move.sqrMagnitude > 0.01f;
            if (moving)
            {
                facing = Mathf.Abs(move.x) > Mathf.Abs(move.y) ? new Vector2(Mathf.Sign(move.x), 0) : new Vector2(0, Mathf.Sign(move.y));
                animTime += Time.deltaTime * 8f * Mathf.Clamp(move.magnitude, 0.4f, 1f);
            }

            Sprite[] set = facing.y > 0 ? up : (facing.y < 0 ? down : right);
            if (set.Length == 0) set = down;
            bool flip = facing.x < 0;

            if (!moving)
            {
                SetFrame(facing.y < 0 && idle != null ? idle : set[0], flip);
                return;
            }
            SetFrame(set[(int)animTime % set.Length], flip);
        }

        void SetFrame(Sprite s, bool flip)
        {
            if (sr.sprite != s) SpriteFactory.SetSprite(sr, s, Height);
            sr.flipX = flip;
        }
    }
}
