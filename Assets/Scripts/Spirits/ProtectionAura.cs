using System.Collections.Generic;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Aura de uma planta de proteção madura (espada-de-são-jorge, pimenteira, guiné...).</summary>
    public class ProtectionAura : MonoBehaviour
    {
        public static readonly List<ProtectionAura> All = new List<ProtectionAura>();

        public AuraType Type { get; private set; }
        public float Radius { get; private set; }
        SpriteRenderer ring;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public bool Active { get { return Type != AuraType.Nenhuma && Radius > 0f; } }
        public Vector2 Center { get { return transform.position; } }

        public void Set(AuraType type, float radius)
        {
            Type = type;
            Radius = radius;
            if (ring == null)
            {
                ring = SpriteFactory.Create("Aura", transform, SpriteFactory.Circle(128), transform.position, 1f, Color.clear, SpriteFactory.ChaoOrder + 100);
            }
            ring.enabled = Active;
            if (!Active) return;
            ring.transform.localScale = Vector3.one * (radius * 2f) / Mathf.Max(0.001f, transform.lossyScale.x);
            float pulse = 0.10f + 0.04f * Mathf.Sin(Time.time * 2f);
            ring.color = ColorFor(type, pulse);
        }

        public static Color ColorFor(AuraType t, float a)
        {
            switch (t)
            {
                case AuraType.Barreira: return new Color(0.45f, 0.95f, 0.55f, a);
                case AuraType.AntiOlhoGordo: return new Color(1f, 0.45f, 0.3f, a);
                case AuraType.AntiSaci: return new Color(0.75f, 0.6f, 1f, a);
            }
            return Color.clear;
        }

        /// <summary>Empurra o espírito para fora de qualquer aura que o barre.</summary>
        public static Vector2 PushOut(Vector2 pos, SpiritType spirit)
        {
            foreach (var a in All)
            {
                if (!a.Active || !ProtectionRules.AuraBlocks(a.Type, spirit)) continue;
                var d = pos - a.Center;
                float r = a.Radius;
                if (d.sqrMagnitude < r * r)
                {
                    if (d.sqrMagnitude < 0.0001f) d = Vector2.up;
                    pos = a.Center + d.normalized * r;
                }
            }
            return pos;
        }

        public static bool Inside(Vector2 pos, AuraType type)
        {
            foreach (var a in All)
                if (a.Active && a.Type == type && Vector2.Distance(pos, a.Center) < a.Radius) return true;
            return false;
        }
    }
}
