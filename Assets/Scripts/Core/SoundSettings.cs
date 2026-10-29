namespace ErvasDoBrasil.Core
{
    /// <summary>
    /// Volume geral do jogo (0 a 1) e o botão de mudo. Mexe só no som do jogo: o volume do Mac ou do iPhone fica como está.
    /// Mudo não apaga o volume escolhido: ligar o som de novo volta ao mesmo nível.
    /// </summary>
    public sealed class SoundSettings
    {
        public const float VolumePadrao = 0.8f;

        float volume = VolumePadrao;

        /// <summary>Nível escolhido no controle, de 0 a 1 (continua guardado enquanto o jogo está mudo).</summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value < 0f ? 0f : (value > 1f ? 1f : value); }
        }

        public bool Mudo { get; set; }

        /// <summary>Sem som nenhum: no mudo ou com o volume no zero.</summary>
        public bool Silencioso { get { return Mudo || volume <= 0f; } }

        /// <summary>O volume que vai para o jogo.</summary>
        public float Efetivo { get { return Mudo ? 0f : volume; } }

        public int Porcentagem { get { return (int)System.Math.Round(Efetivo * 100f); } }

        /// <summary>Mexer no controle de volume liga o som de novo.</summary>
        public void AjustarVolume(float v)
        {
            Volume = v;
            if (volume > 0f) Mudo = false;
        }

        /// <summary>O botão de mudo: silencia ou devolve o som. Se o volume estava no zero, volta no padrão para dar para ouvir.</summary>
        public void AlternarMudo()
        {
            if (!Silencioso) { Mudo = true; return; }
            Mudo = false;
            if (volume <= 0f) volume = VolumePadrao;
        }
    }
}
