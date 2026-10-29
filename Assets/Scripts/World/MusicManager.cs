using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Trilha por ambiente com crossfade: Dia (horta/mata), Feira (perto da vila) e Noite (assombrações).
    /// As faixas ficam em Assets/Audio/Resources/Music com os nomes trilha_dia / trilha_feira / trilha_noite.
    /// Para trocar a música, basta substituir o arquivo mantendo o nome (ogg, mp3 ou wav).
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public const float FadeSeconds = 2.5f;
        public static float Volume { get; set; } = 0.55f;

        AudioSource a, b;
        AudioSource current;
        MusicTrack? playing;
        public Vector2 VillageCenter;

        void Awake()
        {
            a = gameObject.AddComponent<AudioSource>();
            b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { a, b })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                s.spatialBlend = 0f;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null) return;

            float dist = Vector2.Distance(gm.Player.transform.position, VillageCenter);
            var want = MusicRules.Choose(gm.Session.Clock.IsNight, gm.Session.Status.Assombrado, dist);
            if (playing != want) SwitchTo(want);

            float step = Time.unscaledDeltaTime / FadeSeconds;
            foreach (var s in new[] { a, b })
            {
                float target = s == current ? Volume : 0f;
                s.volume = Mathf.MoveTowards(s.volume, target, step * Volume);
                if (s != current && s.volume <= 0f && s.isPlaying) s.Stop();
            }
        }

        void SwitchTo(MusicTrack t)
        {
            playing = t;
            var clip = Resources.Load<AudioClip>("Music/" + MusicRules.ResourceName(t));
            if (clip == null) { Debug.LogWarning("[Música] faixa não encontrada: " + MusicRules.ResourceName(t)); return; }
            var next = current == a ? b : a;
            if (next.clip != clip) { next.clip = clip; next.time = 0f; }
            if (!next.isPlaying) next.Play();
            current = next;
        }
    }
}
