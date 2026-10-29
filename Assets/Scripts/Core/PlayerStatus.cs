using System;
using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    public class ActiveAilment
    {
        public Ailment Tipo;
        public int DosesRestantes;
    }

    public enum CureResult { NaoTinha, Melhorou, Curado }

    /// <summary>
    /// Os três "corpos" do personagem:
    ///  • Energia física (cansaço, indigestão...)
    ///  • Sanidade/Calma (noite, sombras, visagens, ansiedade)
    ///  • Aura espiritual (atrai ou repele mau-olhado e espíritos)
    /// além dos males ativos e das bênçãos (amuleto, sorte, revelação).
    /// </summary>
    public class PlayerStatus
    {
        public const float BaseMaxEnergy = 100f;
        public const float GripeMaxEnergy = 60f;
        public const float MaxSanidade = 100f;
        public const float MaxAura = 100f;

        public const float RegenEnergiaPorSegundo = 1.5f;
        public const float RegenSanidadeDia = 0.5f;
        public const float PerdaSanidadeNoite = 0.3f;
        public const float RegenAuraDia = 0.1f;
        public const float LimiarAnsiedade = 30f;   // sanidade abaixo disso → ansiedade
        public const float AuraVulneravel = 30f;    // aura abaixo disso → espíritos atraídos e mau-olhado pega
        public const float AuraForte = 70f;         // aura acima disso → resiste a mau-olhado

        public const float CriseFumoIntervalo = 25f;
        public const float CriseFumoEnergia = 15f;
        public const float CriseFumoSanidade = 8f;
        public const float CriseFumoTremor = 0.6f;  // segundos de tela tremendo a cada crise

        readonly List<ActiveAilment> ailments = new List<ActiveAilment>();
        float smokeTimer;
        float tremorCrise;

        public float Energy { get; private set; }
        public float Sanidade { get; private set; }
        public float Aura { get; private set; }

        public int AmuletoDias;
        public int SorteDias;
        public float BuffVelocidadePct;
        public float BuffVelocidadeSegundos;
        public float RevelarSegundos;

        /// <summary>Jogo novo: a fissura só começa a contar no primeiro plantio (ou no dia seguinte). O começo fica calmo e com a energia cheia.</summary>
        public bool FissuraEmEspera;

        public event Action<string> Message;
        /// <summary>Energia perdida de uma vez, sem ser por uma ação (crise de fissura, assombração, Cuca...): a barra pisca e mostra "−N".</summary>
        public event Action<float> EnergyDrained;

        public PlayerStatus()
        {
            Energy = BaseMaxEnergy;
            Sanidade = MaxSanidade;
            Aura = 70f;
        }

        // ───────── Males ─────────

        public IList<ActiveAilment> ActiveAilments { get { return ailments.AsReadOnly(); } }

        public bool Has(Ailment a) { return ailments.Any(x => x.Tipo == a); }

        public int DosesRestantes(Ailment a)
        {
            var x = ailments.FirstOrDefault(i => i.Tipo == a);
            return x == null ? 0 : x.DosesRestantes;
        }

        public bool Add(Ailment a)
        {
            if (a == Ailment.Nenhum || Has(a)) return false;
            var info = Ailments.Info(a);
            ailments.Add(new ActiveAilment { Tipo = a, DosesRestantes = info != null ? info.DosesParaCurar : 1 });
            if (a == Ailment.Gripe) Energy = Math.Min(Energy, MaxEnergy);
            Say("Você está com " + Ailments.Nome(a) + ".");
            return true;
        }

        public CureResult CureDose(Ailment a, int doses = 1)
        {
            var x = ailments.FirstOrDefault(i => i.Tipo == a);
            if (x == null) return CureResult.NaoTinha;
            x.DosesRestantes -= Math.Max(1, doses);
            if (x.DosesRestantes <= 0)
            {
                ailments.Remove(x);
                Say(Ailments.Nome(a) + ": curado(a)!");
                return CureResult.Curado;
            }
            Say(Ailments.Nome(a) + " melhorando... faltam " + x.DosesRestantes + " dose(s).");
            return CureResult.Melhorou;
        }

        public void ClearAilments() { ailments.Clear(); }

        // ───────── Atributos derivados ─────────

        public float MaxEnergy { get { return Has(Ailment.Gripe) ? GripeMaxEnergy : BaseMaxEnergy; } }

        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                if (Has(Ailment.DorDeBarriga)) m *= 0.6f;
                if (Has(Ailment.DorRenal)) m *= 0.5f;
                if (Has(Ailment.Colica)) m *= 0.7f;
                if (Has(Ailment.Cansaco)) m *= 0.75f;
                if (Has(Ailment.Enjoo)) m *= 0.85f;
                if (Energy <= 0f) m *= 0.5f;
                if (BuffVelocidadeSegundos > 0f) m *= 1f + BuffVelocidadePct;
                return m;
            }
        }

        /// <summary>Intensidade do tremor de tela (0..1): pânico quando a sanidade cai, e um tranco curto a cada crise de fissura.</summary>
        public float ScreenShake
        {
            get
            {
                float s = Sanidade < LimiarAnsiedade ? (LimiarAnsiedade - Sanidade) / LimiarAnsiedade : 0f;
                if (Has(Ailment.Ansiedade)) s = Math.Max(s, 0.35f);
                if (tremorCrise > 0f) s = Math.Max(s, tremorCrise / CriseFumoTremor);
                return Math.Min(1f, s);
            }
        }

        public float ActionCostMultiplier { get { return Has(Ailment.Azia) ? 2f : 1f; } }
        public float HarvestMultiplier { get { return Has(Ailment.MauOlhado) ? 0.5f : 1f; } }
        public bool CanUseCauldron { get { return !Has(Ailment.Queimadura); } }
        public bool SleepRestores { get { return !Has(Ailment.Insonia); } }
        public bool ProtegidoPorAmuleto { get { return AmuletoDias > 0; } }
        public bool Assombrado { get { return Has(Ailment.Assombrado); } }
        public bool VeEspiritosOcultos { get { return RevelarSegundos > 0f; } }

        /// <summary>Aura baixa atrai espíritos de longe; aura alta os mantém afastados.</summary>
        public float AggroMultiplier
        {
            get
            {
                float m = Has(Ailment.Tosse) ? 2f : 1f;
                m *= 1.5f - Aura / MaxAura; // aura 100 → 0.5x | aura 0 → 1.5x
                return m;
            }
        }

        public float SellPriceMultiplier
        {
            get
            {
                float m = 1f;
                if (Has(Ailment.DorDeDente)) m *= 0.7f;
                if (Has(Ailment.MauOlhado)) m *= 0.8f;
                if (SorteDias > 0) m *= 1.3f;
                return m;
            }
        }

        // ───────── Energia ─────────

        /// <summary>Gasta energia de uma ação (multiplicada pela Azia). Falha sem gastar se não houver o bastante.</summary>
        public bool TrySpend(float cost)
        {
            float c = cost * ActionCostMultiplier;
            if (Energy < c) { Say("Sem energia. Descanse ou tome algo revigorante."); return false; }
            Energy -= c;
            return true;
        }

        public void Drain(float amount)
        {
            float antes = Energy;
            Energy = Clamp(Energy - Math.Max(0f, amount), 0f, MaxEnergy);
            if (Energy < antes && EnergyDrained != null) EnergyDrained(antes - Energy);
        }
        public void Restore(float amount) { Energy = Clamp(Energy + Math.Max(0f, amount), 0f, MaxEnergy); }
        public void RestoreFull() { Energy = MaxEnergy; }

        // ───────── Sanidade ─────────

        public void LoseSanity(float amount)
        {
            float mult = Has(Ailment.Ansiedade) ? 2f : 1f;
            float antes = Sanidade;
            Sanidade = Clamp(Sanidade - Math.Max(0f, amount) * mult, 0f, MaxSanidade);
            // só ao cruzar o limiar: curada a ansiedade com a sanidade ainda baixa, ela não volta no quadro seguinte
            if (antes >= LimiarAnsiedade && Sanidade < LimiarAnsiedade && !Has(Ailment.Ansiedade)) Add(Ailment.Ansiedade);
        }

        public void RestoreSanity(float amount) { Sanidade = Clamp(Sanidade + Math.Max(0f, amount), 0f, MaxSanidade); }

        // ───────── Aura ─────────

        /// <summary>Golpe espiritual na aura. O amuleto de arruda segura a aura.</summary>
        public void HitAura(float amount)
        {
            if (ProtegidoPorAmuleto) return;
            Aura = Clamp(Aura - Math.Max(0f, amount), 0f, MaxAura);
        }

        public void RestoreAura(float amount) { Aura = Clamp(Aura + Math.Max(0f, amount), 0f, MaxAura); }

        /// <summary>
        /// Tentativa de mau-olhado. Bloqueada por amuleto, por estar numa aura de pimenteira,
        /// ou resistida por aura forte. Retorna true se pegou.
        /// </summary>
        public bool TryCurse(bool insideAntiOlhoGordo)
        {
            if (ProtegidoPorAmuleto || insideAntiOlhoGordo) return false;
            if (Aura >= AuraForte) { HitAura(10f); return false; }
            HitAura(10f);
            return Add(Ailment.MauOlhado);
        }

        // ───────── Tempo ─────────

        public void Tick(float dt, bool isNight)
        {
            if (dt <= 0f) return;

            Restore(RegenEnergiaPorSegundo * dt);

            if (isNight) LoseSanity(PerdaSanidadeNoite * dt);
            else RestoreSanity(RegenSanidadeDia * dt * (Has(Ailment.Ansiedade) ? 0.5f : 1f)); // ansiosa, recupera pela metade

            if (!isNight && !Has(Ailment.MauOlhado)) RestoreAura(RegenAuraDia * dt);

            // antes da crise: o tranco de uma crise neste mesmo quadro não some na hora
            if (tremorCrise > 0f) tremorCrise = Math.Max(0f, tremorCrise - dt);

            if (Has(Ailment.VontadeDeFumar) && !FissuraEmEspera)
            {
                smokeTimer += dt;
                float intervalo = IntervaloCriseFumo;
                while (smokeTimer >= intervalo)
                {
                    smokeTimer -= intervalo;
                    Drain(CriseFumoEnergia);
                    LoseSanity(CriseFumoSanidade);
                    tremorCrise = CriseFumoTremor;
                    Say("Fissura! Respira fundo... um chá de mulungu ajuda.");
                }
            }
            else smokeTimer = 0f;

            if (BuffVelocidadeSegundos > 0f) BuffVelocidadeSegundos = Math.Max(0f, BuffVelocidadeSegundos - dt);
            if (RevelarSegundos > 0f) RevelarSegundos = Math.Max(0f, RevelarSegundos - dt);
        }

        /// <summary>Cada dose tomada espaça as crises: 25 s sem tratamento, 50 s com uma dose, 75 s com duas.</summary>
        public float IntervaloCriseFumo
        {
            get
            {
                var info = Ailments.Info(Ailment.VontadeDeFumar);
                int tomadas = info != null ? Math.Max(0, info.DosesParaCurar - DosesRestantes(Ailment.VontadeDeFumar)) : 0;
                return CriseFumoIntervalo * (1 + tomadas);
            }
        }

        public void OnNewDay()
        {
            if (AmuletoDias > 0) AmuletoDias--;
            if (SorteDias > 0) SorteDias--;
            FissuraEmEspera = false; // sem plantar no primeiro dia, a fissura acorda no segundo
        }

        // ───────── Jogo salvo ─────────

        internal float SmokeTimer { get { return smokeTimer; } }

        /// <summary>Volta um mal salvo, sem mensagem. Chame antes de <see cref="Restore(float, float, float, float)"/> (a gripe limita a energia).</summary>
        internal void RestoreAilment(Ailment a, int doses)
        {
            if (a == Ailment.Nenhum || Has(a)) return;
            ailments.Add(new ActiveAilment { Tipo = a, DosesRestantes = Math.Max(1, doses) });
        }

        internal void Restore(float energy, float sanidade, float aura, float smokeTimerSeconds)
        {
            Energy = Clamp(energy, 0f, MaxEnergy);
            Sanidade = Clamp(sanidade, 0f, MaxSanidade);
            Aura = Clamp(aura, 0f, MaxAura);
            smokeTimer = Math.Max(0f, smokeTimerSeconds);
        }

        static float Clamp(float v, float min, float max) { return v < min ? min : (v > max ? max : v); }

        void Say(string msg)
        {
            if (Message != null) Message(msg);
        }
    }
}
