using System.Collections.Generic;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Diário do ciclo de vida do app, mostrado ao vivo na janela Bastidores (aba Ao vivo) e gravado no log.
    /// Fica num estático: sobrevive à recarga da cena (carregar jogo, menu inicial), como os pedidos do GameManager.
    /// </summary>
    public static class LifecycleLog
    {
        public struct Entry
        {
            public float Seconds;
            public int Frame;
            public string Text;
        }

        const int MaxEntries = 60;
        static readonly List<Entry> entries = new List<Entry>();

        public static IList<Entry> Entries { get { return entries; } }

        /// <summary>Registra um evento; ele também vai para o Console da Unity e para o Player.log.</summary>
        public static void Add(string text)
        {
            var e = new Entry { Seconds = Time.realtimeSinceStartup, Frame = Time.frameCount, Text = text };
            entries.Add(e);
            if (entries.Count > MaxEntries) entries.RemoveAt(0);
            Debug.Log("[Ciclo de vida] frame " + e.Frame + " · " + text);
        }

        // os dois primeiros ganchos que a Unity oferece, antes de existir qualquer cena
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnSubsystemRegistration()
        {
            entries.Clear(); // no editor, sem recarregar o domínio, o estático viria do Play anterior
            Add("SubsystemRegistration: a Unity subiu o runtime C#");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad() { Add("BeforeSceneLoad: a primeira cena vai ser carregada"); }
    }
}
