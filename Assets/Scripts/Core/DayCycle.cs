using System;
using System.Globalization;

namespace ErvasDoBrasil.Core
{
    /// <summary>Relógio do sítio. Time01: 0 = meia-noite, 0.25 = 06h, 0.5 = meio-dia, 0.75 = 18h.</summary>
    public class DayCycle
    {
        public const float Amanhecer = 0.25f;
        public const float Anoitecer = 0.75f;

        public readonly float DayLengthSeconds;
        public int Day { get; private set; }
        public float Time01 { get; private set; }

        public DayCycle(float dayLengthSeconds = 240f, int day = 1, float time01 = Amanhecer)
        {
            if (dayLengthSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(dayLengthSeconds));
            DayLengthSeconds = dayLengthSeconds;
            Day = Math.Max(1, day);
            Time01 = Clamp01(time01);
        }

        public bool IsNight { get { return Time01 < Amanhecer || Time01 >= Anoitecer; } }
        public bool CanSleep { get { return IsNight; } }

        public int Hour { get { return (int)(Time01 * 24f) % 24; } }
        public int Minute { get { return (int)(Time01 * 24f * 60f) % 60; } }
        public string Clock { get { return Hour.ToString("00", CultureInfo.InvariantCulture) + ":" + Minute.ToString("00", CultureInfo.InvariantCulture); } }

        /// <summary>Quanto escuro está (0 dia, 1 noite fechada) — usado para o filtro de cor.</summary>
        public float Darkness
        {
            get
            {
                float t = Time01;
                if (t >= 0.30f && t <= 0.70f) return 0f;
                if (t > 0.70f && t < 0.80f) return (t - 0.70f) / 0.10f;
                if (t > 0.20f && t < 0.30f) return 1f - (t - 0.20f) / 0.10f;
                return 1f;
            }
        }

        /// <summary>Avança o relógio. Retorna quantas meias-noites passaram.</summary>
        public int Advance(float dt)
        {
            if (dt <= 0f) return 0;
            Time01 += dt / DayLengthSeconds;
            int passed = 0;
            while (Time01 >= 1f)
            {
                Time01 -= 1f;
                Day++;
                passed++;
            }
            return passed;
        }

        /// <summary>Dorme até as 06h. Retorna false se ainda é dia.</summary>
        public bool SleepUntilMorning()
        {
            if (!CanSleep) return false;
            if (Time01 >= Anoitecer) Day++;
            Time01 = Amanhecer;
            return true;
        }

        /// <summary>Volta o relógio para um dia e hora salvos.</summary>
        internal void Restore(int day, float time01)
        {
            Day = Math.Max(1, day);
            Time01 = Clamp01(time01);
        }

        static float Clamp01(float v) { return v < 0f ? 0f : (v >= 1f ? 0f : v); }
    }
}
