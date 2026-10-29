using System;

namespace ErvasDoBrasil.Core
{
    public enum PlotState { Bruto, Preparado, Plantado, Pronto }

    public class HarvestResult
    {
        public string PlantId;
        public int Ervas;
        public int Sementes;
    }

    /// <summary>
    /// Grade dos canteiros no mundo (em unidades). A horta da vó é 4x3 a partir de Origem;
    /// os canteiros cavados com a pá caem nas mesmas linhas e colunas, então nunca se sobrepõem.
    /// </summary>
    public static class FarmLayout
    {
        public const float OrigemX = -1.2f;
        public const float OrigemY = -5.2f;
        public const float Largura = 1.6f;
        public const float Altura = 1.4f;
        public const int ColunasHorta = 4;

        public static float SnapX(float x) { return OrigemX + (float)Math.Round((x - OrigemX) / Largura) * Largura; }
        public static float SnapY(float y) { return OrigemY + (float)Math.Round((y - OrigemY) / Altura) * Altura; }

        /// <summary>Posição do canteiro i da horta (linha a linha, de cima para baixo).</summary>
        public static float HortaX(int i) { return OrigemX + i % ColunasHorta * Largura; }
        public static float HortaY(int i) { return OrigemY - i / ColunasHorta * Altura; }

        /// <summary>Mesma casa da grade (as posições vêm de Snap, então basta meia casa de tolerância).</summary>
        public static bool MesmaCasa(float x1, float y1, float x2, float y2)
        {
            return Math.Abs(x1 - x2) < Largura / 2f && Math.Abs(y1 - y2) < Altura / 2f;
        }
    }

    /// <summary>
    /// Um canteiro: terra bruta → preparada (enxada) → plantada → regada cresce → pronta para colher.
    /// A rega vale pelo dia; sem água a planta não cresce. Os canteiros cavados com a pá já nascem preparados.
    /// </summary>
    public class FarmPlotModel
    {
        public const float CustoPreparar = 4f;
        public const float CustoCavar = 6f;
        public const float CustoPlantar = 2f;
        public const float CustoRegar = 1f;
        public const float CustoColher = 2f;

        public FarmPlotModel(float x = 0f, float y = 0f, bool cavado = false)
        {
            X = x;
            Y = y;
            Cavado = cavado;
        }

        /// <summary>Centro do canteiro no mundo.</summary>
        public float X { get; private set; }
        public float Y { get; private set; }

        /// <summary>Cavado pela jogadora com a pá (fora da horta da vó). Só estes podem ser desfeitos.</summary>
        public bool Cavado { get; private set; }

        /// <summary>
        /// A horta da vó aceita qualquer planta; o canteiro cavado com a pá é para as plantas de proteção, as que fazem
        /// efeito na área (Espada-de-São-Jorge, Pimenteira, Guiné, Comigo-ninguém-pode), plantadas onde a casa precisa.
        /// </summary>
        public bool Aceita(PlantData planta) { return planta != null && (!Cavado || planta.TemAura); }

        public string PlantId { get; private set; }
        public float Growth { get; private set; }
        public bool Prepared { get; private set; }
        public bool Watered { get; private set; }

        public PlotState State
        {
            get
            {
                if (PlantId != null) return Growth >= 1f ? PlotState.Pronto : PlotState.Plantado;
                return Prepared ? PlotState.Preparado : PlotState.Bruto;
            }
        }

        public bool IsEmpty { get { return PlantId == null; } }
        public bool IsReady { get { return State == PlotState.Pronto; } }
        public bool NeedsWater { get { return State == PlotState.Plantado && !Watered; } }

        public bool Prepare()
        {
            if (State != PlotState.Bruto) return false;
            Prepared = true;
            return true;
        }

        public bool Plant(string plantId)
        {
            if (State != PlotState.Preparado || string.IsNullOrEmpty(plantId)) return false;
            PlantId = plantId;
            Growth = 0f;
            return true;
        }

        public bool Water()
        {
            if (State != PlotState.Plantado || Watered) return false;
            Watered = true;
            return true;
        }

        public void Tick(float dt, PlantData data)
        {
            if (State != PlotState.Plantado || !Watered || data == null || dt <= 0f) return;
            float total = Math.Max(1f, data.tempoCrescimento);
            Growth = Math.Min(1f, Growth + dt / total);
        }

        /// <summary>Volta o canteiro para um estado salvo.</summary>
        internal void Restore(string plantId, float growth, bool prepared, bool watered)
        {
            PlantId = string.IsNullOrEmpty(plantId) ? null : plantId;
            Prepared = prepared || PlantId != null;
            Growth = PlantId == null ? 0f : Math.Max(0f, Math.Min(1f, growth));
            Watered = PlantId != null && watered;
        }

        /// <summary>Um novo dia seca a terra: precisa regar de novo.</summary>
        public void OnNewDay() { Watered = false; }

        /// <summary>A Bernúncia engoliu a planta: o canteiro fica vazio, mas a terra continua preparada. Retorna a planta perdida.</summary>
        public string Devour()
        {
            if (IsEmpty) return null;
            var id = PlantId;
            PlantId = null;
            Growth = 0f;
            Watered = false;
            return id;
        }

        /// <summary>Planta espiritual madura irradia a aura enquanto não for colhida.</summary>
        public bool AuraActive(PlantData data) { return data != null && IsReady && data.TemAura; }

        public HarvestResult Harvest(PlayerStatus status, IRandom rng)
        {
            if (!IsReady) return null;
            int qty = rng.Range(2, 4) + (status.SorteDias > 0 ? 1 : 0);
            qty = Math.Max(1, (int)Math.Floor(qty * status.HarvestMultiplier));
            int seeds = rng.Value() < 0.4f ? 1 : 0;
            var result = new HarvestResult { PlantId = PlantId, Ervas = qty, Sementes = seeds };
            PlantId = null;
            Growth = 0f;
            Watered = false;
            // continua preparado: dá pra replantar direto
            return result;
        }
    }
}
