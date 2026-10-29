using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Liga as <see cref="SoundSettings"/> (Core) ao AudioListener.volume, o volume geral da Unity: vale para a trilha
    /// e para os efeitos, e não mexe no volume do sistema. A escolha fica no PlayerPrefs e vale para todas as partidas.
    /// </summary>
    public static class GameAudio
    {
        public const string ChavePadrao = "som";

        static SoundSettings settings;

        /// <summary>
        /// Começo das chaves no PlayerPrefs (som.volume, som.mudo). Os testes de interface usam outro,
        /// para não mudar o volume que a jogadora deixou no jogo de verdade.
        /// </summary>
        public static string Chave { get; set; } = ChavePadrao;

        static string ChaveVolume { get { return Chave + ".volume"; } }
        static string ChaveMudo { get { return Chave + ".mudo"; } }

        public static SoundSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = new SoundSettings
                    {
                        Volume = PlayerPrefs.GetFloat(ChaveVolume, SoundSettings.VolumePadrao),
                        Mudo = PlayerPrefs.GetInt(ChaveMudo, 0) == 1,
                    };
                }
                return settings;
            }
        }

        public static void AjustarVolume(float v)
        {
            if (Mathf.Approximately(v, Settings.Volume)) return;
            Settings.AjustarVolume(v);
            Mudou();
        }

        public static void AlternarMudo()
        {
            Settings.AlternarMudo();
            Mudou();
        }

        /// <summary>Passa o volume para a Unity. O GameManager chama ao abrir o jogo, com o volume que ficou gravado.</summary>
        public static void Aplicar() { AudioListener.volume = Settings.Efetivo; }

        /// <summary>Esquece o que está na memória e lê o PlayerPrefs de novo, como ao abrir o jogo (os testes simulam uma nova abertura).</summary>
        public static void Recarregar()
        {
            settings = null;
            Aplicar();
        }

        /// <summary>Apaga a escolha gravada nas chaves atuais: o próximo <see cref="Recarregar"/> volta ao volume padrão, com som.</summary>
        public static void Apagar()
        {
            PlayerPrefs.DeleteKey(ChaveVolume);
            PlayerPrefs.DeleteKey(ChaveMudo);
            PlayerPrefs.Save();
        }

        static void Mudou()
        {
            Aplicar();
            PlayerPrefs.SetFloat(ChaveVolume, Settings.Volume);
            PlayerPrefs.SetInt(ChaveMudo, Settings.Mudo ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
