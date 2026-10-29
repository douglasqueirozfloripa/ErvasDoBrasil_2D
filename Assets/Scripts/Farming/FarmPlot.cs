using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Canteiro da horta: visual + interação. As regras ficam em FarmPlotModel (core).</summary>
    public class FarmPlot : Interactable
    {
        public FarmPlotModel Model;

        SpriteRenderer soil;
        SpriteRenderer mira;
        SpriteRenderer crop;
        ProtectionAura aura;
        int lastStage = -1;
        string lastPlant;

        // terra opaca e com contorno: cada canteiro aparece inteiro sobre a grama, até o bruto
        static readonly Color Bruto = new Color(0.72f, 0.58f, 0.42f);
        static readonly Color Preparado = new Color(0.50f, 0.35f, 0.22f);
        static readonly Color Molhado = new Color(0.36f, 0.25f, 0.17f);
        public static readonly Color MiraCor = new Color(1f, 0.95f, 0.65f);

        /// <summary>Altura do canteiro no mundo; a grade (FarmLayout) deixa um vão entre um e outro.</summary>
        public const float Tamanho = 1.15f;

        public void Init(FarmPlotModel model)
        {
            Model = model;
            Radius = 0.9f;
            soil = SpriteFactory.Create("Terra", transform, SpriteFactory.Bed(false), transform.position, Tamanho, Bruto, SpriteFactory.ChaoOrder + 500);
            mira = SpriteFactory.Create("Mira", transform, SpriteFactory.Frame(), transform.position, Tamanho + 0.12f, MiraCor, SpriteFactory.ChaoOrder + 510);
            mira.enabled = false;
            crop = SpriteFactory.Create("Planta", transform, null, transform.position + new Vector3(0, -0.4f, 0), 1f, Color.white, SpriteFactory.YOrder(transform.position.y));
            aura = gameObject.AddComponent<ProtectionAura>();
        }

        PlantData Data { get { return Model.IsEmpty ? null : Session.Db.Plant(Model.PlantId); } }

        public override string Prompt
        {
            get
            {
                var p = Data;
                switch (Model.State)
                {
                    case PlotState.Bruto: return "Preparar a terra";
                    case PlotState.Preparado: return "Plantar";
                    case PlotState.Plantado: return Model.NeedsWater ? "Regar " + p.nome : p.nome + " crescendo " + Mathf.RoundToInt(Model.Growth * 100f) + "%";
                    default: return p.TemAura ? "Colher " + p.nome + " (desfaz a proteção)" : "Colher " + p.nome;
                }
            }
        }

        public override bool CanInteract { get { return Model.State != PlotState.Plantado || Model.NeedsWater; } }

        /// <summary>
        /// Diz o que o canteiro precisa: "Regar" (não há água para buscar: chegar perto e agir já rega) ou "Colher".
        /// A tecla só aparece no canteiro que a ação vai atingir, para o "Regar [E]" do vizinho não parecer
        /// a ação do canteiro que está pronto para colher.
        /// </summary>
        public override string Label
        {
            get
            {
                string acao = Model.NeedsWater ? "Regar" : (Model.IsReady ? "Colher" : null);
                if (acao == null) return null;
                var gm = GameManager.I;
                bool alvo = gm != null && gm.Player != null && gm.Player.Current == this;
                return alvo ? acao + (InputCompat.IsTouchDevice ? " (Ação)" : " [E]") : acao;
            }
        }

        public override void Interact(PlayerController player)
        {
            switch (Model.State)
            {
                case PlotState.Bruto:
                    if (Session.PreparePlot(Model)) player.PlayAction("forage", 0.4f);
                    break;
                case PlotState.Preparado:
                    UI.OpenPlantMenu(this);
                    break;
                case PlotState.Plantado:
                    Session.WaterPlot(Model);
                    break;
                case PlotState.Pronto:
                    var r = Session.HarvestPlot(Model);
                    if (r != null)
                    {
                        player.PlayAction("forage", 0.5f);
                        UI.Toast("+" + r.Ervas + " " + Session.Db.Plant(r.PlantId).nome + (r.Sementes > 0 ? " e 1 semente" : ""));
                    }
                    break;
            }
        }

        void Update()
        {
            bool bruto = Model.State == PlotState.Bruto;
            soil.color = bruto ? Bruto : (Model.Watered ? Molhado : Preparado);
            var bed = SpriteFactory.Bed(!bruto);
            if (soil.sprite != bed) soil.sprite = bed;

            // a moldura mostra qual canteiro a ação vai atingir
            var gm = GameManager.I;
            mira.enabled = gm != null && gm.Player != null && gm.Player.Current == this;
            if (mira.enabled) mira.color = new Color(MiraCor.r, MiraCor.g, MiraCor.b, 0.65f + 0.35f * Mathf.Sin(Time.time * 5f));

            var p = Data;
            aura.Set(p != null && Model.AuraActive(p) ? p.Aura : AuraType.Nenhuma, p != null ? p.raioAura : 0f);

            if (p == null)
            {
                crop.enabled = false;
                lastStage = -1;
                return;
            }
            crop.enabled = true;

            int stage = Model.IsReady ? 4 : Mathf.Clamp(Mathf.FloorToInt(Model.Growth * 4f), 0, 3);
            if (stage == lastStage && lastPlant == p.id) return;
            lastStage = stage;
            lastPlant = p.id;

            var art = ArtLibrary.Get("plant_" + p.id + "_" + stage);
            if (art != null)
            {
                crop.color = Color.white;
                crop.transform.localPosition = new Vector3(0f, -0.4f, 0f); // arte com pivô no pé
                SpriteFactory.SetSprite(crop, art, 0.45f + 0.25f * stage);
            }
            else
            {
                // reserva: bolinha da cor da planta que cresce
                crop.color = stage == 0 ? new Color(0.45f, 0.3f, 0.15f) : SpriteFactory.Hex(p.cor, Color.green);
                crop.transform.localPosition = Vector3.zero;
                SpriteFactory.SetSprite(crop, SpriteFactory.Circle(), 0.25f + 0.18f * stage);
            }
        }
    }
}
