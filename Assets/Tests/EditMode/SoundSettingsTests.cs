using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    public class SoundSettingsTests
    {
        [Test]
        public void Comeca_com_som_no_volume_padrao()
        {
            var s = new SoundSettings();
            Assert.IsFalse(s.Mudo);
            Assert.AreEqual(SoundSettings.VolumePadrao, s.Efetivo, 1e-5f);
            Assert.AreEqual(80, s.Porcentagem);
        }

        [Test]
        public void Mudo_silencia_e_desmutar_volta_ao_mesmo_volume()
        {
            var s = new SoundSettings();
            s.AjustarVolume(0.35f);
            s.AlternarMudo();
            Assert.IsTrue(s.Mudo);
            Assert.AreEqual(0f, s.Efetivo);
            Assert.AreEqual(0.35f, s.Volume, 1e-5f, "o volume escolhido fica guardado no mudo");
            s.AlternarMudo();
            Assert.IsFalse(s.Mudo);
            Assert.AreEqual(0.35f, s.Efetivo, 1e-5f);
        }

        [Test]
        public void Mexer_no_volume_tira_do_mudo()
        {
            var s = new SoundSettings();
            s.AlternarMudo();
            s.AjustarVolume(0.5f);
            Assert.IsFalse(s.Mudo);
            Assert.AreEqual(50, s.Porcentagem);
        }

        [Test]
        public void Volume_fica_entre_0_e_1()
        {
            var s = new SoundSettings();
            s.AjustarVolume(1.7f);
            Assert.AreEqual(1f, s.Volume);
            s.AjustarVolume(-0.2f);
            Assert.AreEqual(0f, s.Volume);
            Assert.IsTrue(s.Silencioso, "volume no zero também é silêncio");
        }

        [Test]
        public void Desmutar_com_volume_no_zero_volta_ao_padrao_para_dar_para_ouvir()
        {
            var s = new SoundSettings();
            s.AjustarVolume(0f);
            s.AlternarMudo();
            Assert.IsFalse(s.Silencioso);
            Assert.AreEqual(SoundSettings.VolumePadrao, s.Efetivo, 1e-5f);
        }
    }
}
