using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Erva nativa na mata: coleta de campo. Some e renasce depois de um tempo.</summary>
    public class WildHerb : Interactable
    {
        public const float RespawnSeconds = 90f;

        public string PlantId;
        SpriteRenderer sr;
        float respawnAt = -1f;

        public void Init(string plantId)
        {
            PlantId = plantId;
            Radius = 1f;
            var p = GameManager.I.Session.Db.Plant(plantId);
            var art = ArtLibrary.Get("plant_" + plantId + "_3");
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SpriteFactory.YOrder(transform.position.y);
            if (art != null) SpriteFactory.SetSprite(sr, art, 0.9f);
            else
            {
                sr.color = SpriteFactory.Hex(p != null ? p.cor : null, Color.green);
                SpriteFactory.SetSprite(sr, SpriteFactory.Circle(), 0.55f);
            }
        }

        bool Available { get { return respawnAt < 0f || Time.time >= respawnAt; } }

        public override bool CanInteract { get { return Available; } }

        public override string Prompt
        {
            get
            {
                var p = Session.Db.Plant(PlantId);
                bool known = Session.Herbarium.IsDiscovered(PlantId);
                string nome = known ? "Coletar " + p.nome : "Coletar planta desconhecida";
                return Session.Progresso.PodeCultivar(p) ? nome : nome + " (nível " + p.NivelMinimo + ")";
            }
        }

        public override void Interact(PlayerController player)
        {
            int n = Session.CollectWild(PlantId);
            if (n <= 0) return;
            player.PlayAction("forage", 0.6f);
            UI.Toast("+" + n + " " + Session.Db.Plant(PlantId).nome);
            respawnAt = Time.time + RespawnSeconds;
        }

        void Update()
        {
            if (sr == null) return;
            var c = sr.color;
            c.a = Mathf.MoveTowards(c.a, Available ? 1f : 0.15f, Time.deltaTime * 2f);
            sr.color = c;
        }
    }
}
