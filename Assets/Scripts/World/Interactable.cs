using System.Collections.Generic;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Qualquer coisa do mundo com a qual o jogador interage (botão Ação / tecla E).</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float Radius = 1.2f;

        /// <summary>Altura do rótulo flutuante acima do pé do objeto (a arte de pé, como os aldeões, é mais alta que 1 m).</summary>
        public float LabelHeight = 1f;

        protected GameSession Session { get { return GameManager.I.Session; } }
        protected GameUI UI { get { return GameManager.I.UI; } }

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        /// <summary>Texto do botão de ação ("Colher boldo").</summary>
        public abstract string Prompt { get; }

        /// <summary>Rótulo flutuante no mundo (opcional).</summary>
        public virtual string Label { get { return null; } }

        public virtual bool CanInteract { get { return true; } }

        public abstract void Interact(PlayerController player);

        /// <summary>
        /// O alvo da tecla E: o mais perto ao alcance, dando preferência ao que está à frente da jogadora.
        /// Na horta os canteiros ficam a 1,4 m um do outro; parada entre duas fileiras, a jogadora fica à mesma distância
        /// do canteiro de cima e do de baixo, e sem olhar para onde ela está virada o E regava o canteiro de trás
        /// quando ela queria colher o da frente.
        /// </summary>
        public static Interactable Nearest(Vector2 pos, Vector2 facing = default)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var i in All)
            {
                if (i == null || !i.isActiveAndEnabled || !i.CanInteract) continue;
                Vector2 to = (Vector2)i.transform.position - pos;
                float d = to.magnitude;
                if (d > i.Radius) continue;
                float ahead = d > 0.01f && facing.sqrMagnitude > 0.01f ? Vector2.Dot(to / d, facing.normalized) : 0f;
                float score = d * (1f - 0.35f * ahead);
                if (score < bestScore) { best = i; bestScore = score; }
            }
            return best;
        }
    }

    public class Bed : Interactable
    {
        public override string Prompt { get { return "Dormir"; } }
        public override string Label { get { return "Casa da Vó"; } }

        public override void Interact(PlayerController player)
        {
            if (Session.Sleep()) GameManager.I.OnSlept();
        }
    }

    /// <summary>Estações de preparo: caldeirão, varal e pilão abrem janelas.</summary>
    public class Station : Interactable
    {
        public GameUI.Window Window;
        public string Nome;

        public override string Prompt { get { return "Usar o " + Nome.ToLowerInvariant(); } }
        public override string Label { get { return Nome; } }

        public override void Interact(PlayerController player)
        {
            UI.Open(Window);
        }
    }

    public class Market : Interactable
    {
        public override string Prompt { get { return "Feira da Dona Cida"; } }
        public override string Label { get { return "Feira"; } }

        public override void Interact(PlayerController player) { UI.Open(GameUI.Window.Feira); }
    }

    public class Villager : Interactable
    {
        public int Index;

        public VillagerRequest Request
        {
            get { var r = Session.Requests; return Index < r.Count ? r[Index] : null; }
        }

        public override bool CanInteract { get { return Request != null; } }
        public override string Prompt { get { return Request == null ? "" : "Falar com " + Request.Nome; } }

        public override string Label
        {
            get
            {
                var r = Request;
                if (r == null) return null;
                // nome em cima e o mal embaixo: balão estreito, os três aldeões cabem lado a lado
                return r.Atendido ? r.Nome + "\n(curado)" : r.Nome + "\n" + Ailments.Nome(r.Mal) + "!";
            }
        }

        public override void Interact(PlayerController player)
        {
            UI.OpenVillager(this);
        }

        public const float Altura = 1.4f;

        SpriteRenderer sr;

        /// <summary>A arte na tela agora ("npc_tonhao"). Muda quando os pedidos trocam e outro aldeão ocupa este lugar.</summary>
        public string Arte { get; private set; }

        void TrocarArte(string arte)
        {
            if (arte == null || arte == Arte) return;
            var sprite = ArtLibrary.Get(arte);
            if (sprite == null) return; // sem o PNG fica a forma colorida do Spawn
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            Arte = arte;
            sr.color = Color.white;
            SpriteFactory.SetSprite(sr, sprite, Altura);
        }

        void Update()
        {
            var r = Request;
            if (r != null) TrocarArte(Village.Arte(r.Nome));
            // balancinho de quem está passando mal
            float wobble = r != null && !r.Atendido ? Mathf.Sin(Time.time * 3f + Index) * 4f : 0f;
            transform.localRotation = Quaternion.Euler(0, 0, wobble);
        }
    }
}
