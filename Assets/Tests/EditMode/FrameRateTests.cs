using System.Collections.Generic;
using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    /// <summary>A régua do teste de desempenho (FPS médio, 1% low e engasgos) e a nota ruim / regular / bom / excelente.</summary>
    public class FrameRateTests
    {
        static List<float> Quadros(int quantos, float segundos)
        {
            var t = new List<float>(quantos);
            for (int i = 0; i < quantos; i++) t.Add(segundos);
            return t;
        }

        [Test]
        public void Sessenta_fps_constantes_por_15_s_sao_excelentes()
        {
            var r = FrameRateReport.Medir(Quadros(900, 1f / 60f));
            Assert.AreEqual(900, r.Quadros);
            Assert.AreEqual(15f, r.Segundos, 0.01f);
            Assert.AreEqual(60f, r.FpsMedio, 0.1f);
            Assert.AreEqual(60f, r.FpsUmPorCento, 0.1f);
            Assert.AreEqual(0, r.Engasgos);
            Assert.AreEqual(Fluidez.Excelente, r.Nota);
        }

        [Test]
        public void Fps_alto_com_um_quadro_de_50_ms_engasga_e_cai_para_regular()
        {
            // o contador marcaria uns 115 fps, mas o olho vê a travada: o 1% low entrega
            var t = Quadros(119, 1f / 120f);
            t.Add(0.051f);
            var r = FrameRateReport.Medir(t);
            Assert.Greater(r.FpsMedio, 100f);
            Assert.AreEqual(1, r.Engasgos);
            Assert.AreEqual(51f, r.PiorQuadroMs, 0.5f);
            Assert.Less(r.FpsUmPorCento, 30f, "o pior 1% roda a 20 fps");
            Assert.AreEqual(Fluidez.Regular, r.Nota);
        }

        [Test]
        public void Sessenta_fps_com_dois_engasgos_em_15_s_e_bom()
        {
            var t = Quadros(898, 1f / 60f);
            t.Insert(300, 0.06f);
            t.Insert(600, 0.06f);
            var r = FrameRateReport.Medir(t);
            Assert.AreEqual(2, r.Engasgos);
            Assert.GreaterOrEqual(r.FpsUmPorCento, 30f);
            Assert.AreEqual(Fluidez.Bom, r.Nota);
        }

        [Test]
        public void Trinta_fps_estaveis_sao_regulares()
        {
            var r = FrameRateReport.Medir(Quadros(450, 1f / 30f));
            Assert.AreEqual(30f, r.FpsMedio, 0.1f);
            Assert.AreEqual(0, r.Engasgos, "33 ms por quadro ainda não é engasgo");
            Assert.AreEqual(Fluidez.Regular, r.Nota);
        }

        [Test]
        public void Quinze_fps_sao_ruins_e_cada_quadro_e_um_engasgo()
        {
            var r = FrameRateReport.Medir(Quadros(225, 1f / 15f));
            Assert.AreEqual(225, r.Engasgos);
            Assert.AreEqual(Fluidez.Ruim, r.Nota);
        }

        [Test]
        public void Oscilar_entre_rapido_e_lento_pesa_mais_que_a_media()
        {
            // metade dos quadros a 130 fps e metade a 25 fps: a média passa de 40 fps, mas o 1% low fica em 25
            var t = new List<float>();
            for (int i = 0; i < 400; i++) { t.Add(1f / 130f); t.Add(1f / 25f); }
            var r = FrameRateReport.Medir(t);
            Assert.Greater(r.FpsMedio, 40f);
            Assert.AreEqual(25f, r.FpsUmPorCento, 0.5f);
            Assert.AreEqual(Fluidez.Regular, r.Nota);
        }

        [TestCase(60f, 50f, 0, Fluidez.Excelente)]
        [TestCase(55f, 45f, 0, Fluidez.Excelente, TestName = "Avaliar: no limite ainda é excelente")]
        [TestCase(55f, 45f, 1, Fluidez.Bom, TestName = "Avaliar: um engasgo tira o excelente")]
        [TestCase(54f, 45f, 0, Fluidez.Bom)]
        [TestCase(45f, 30f, 2, Fluidez.Bom)]
        [TestCase(45f, 30f, 3, Fluidez.Regular, TestName = "Avaliar: três engasgos tiram o bom")]
        [TestCase(59f, 29f, 0, Fluidez.Regular, TestName = "Avaliar: 1% low fraco tira o bom mesmo com média alta")]
        [TestCase(30f, 15f, 5, Fluidez.Regular)]
        [TestCase(29f, 15f, 5, Fluidez.Ruim)]
        [TestCase(30f, 14f, 5, Fluidez.Ruim)]
        [TestCase(30f, 15f, 6, Fluidez.Ruim)]
        public void Avaliar_segue_a_regua_do_alvo_de_60_fps(float media, float umPorCento, int engasgos, Fluidez esperada)
        {
            Assert.AreEqual(esperada, FrameRateReport.Avaliar(media, umPorCento, engasgos));
        }

        [Test]
        public void A_regua_acompanha_outro_alvo()
        {
            // 28 fps é excelente para um alvo de 30 fps (limites pela metade) e ruim para o alvo de 60
            Assert.AreEqual(Fluidez.Excelente, FrameRateReport.Avaliar(28f, 23f, 0, 30f));
            Assert.AreEqual(Fluidez.Ruim, FrameRateReport.Avaliar(28f, 23f, 0, 60f));
        }

        [Test]
        public void Vinte_e_nove_virgula_noventa_e_nove_fps_contam_como_30()
        {
            // 450 quadros de 1/30 s somam 15,000001 s em float: a média dá 29,99999 e ainda é regular
            Assert.AreEqual(Fluidez.Regular, FrameRateReport.Avaliar(29.99f, 15f, 0));
            Assert.AreEqual(Fluidez.Ruim, FrameRateReport.Avaliar(29.9f, 15f, 0));
        }

        [Test]
        public void Sem_quadros_ou_so_valores_invalidos_e_ruim_sem_estourar()
        {
            var vazio = FrameRateReport.Medir(new List<float>());
            Assert.AreEqual(0, vazio.Quadros);
            Assert.AreEqual(Fluidez.Ruim, vazio.Nota);

            var invalidos = FrameRateReport.Medir(new List<float> { float.NaN, 0f, -1f, float.PositiveInfinity });
            Assert.AreEqual(0, invalidos.Quadros);
            Assert.AreEqual(Fluidez.Ruim, invalidos.Nota);
        }

        [Test]
        public void Valores_invalidos_no_meio_sao_ignorados()
        {
            var t = Quadros(300, 1f / 60f);
            t.Add(float.NaN);
            t.Add(0f);
            var r = FrameRateReport.Medir(t);
            Assert.AreEqual(300, r.Quadros);
            Assert.AreEqual(Fluidez.Excelente, r.Nota);
        }

        [Test]
        public void Resumo_traz_a_nota_e_os_numeros()
        {
            var t = Quadros(899, 1f / 60f);
            t.Add(0.06f);
            string s = FrameRateReport.Medir(t).Resumo();
            StringAssert.StartsWith("BOM", s);
            StringAssert.Contains("fps de média", s);
            StringAssert.Contains("1% low", s);
            StringAssert.Contains("pior quadro 60 ms", s);
            StringAssert.Contains("1 engasgo (> 50 ms)", s);
            StringAssert.Contains("900 quadros em 15.0 s", s);
        }
    }
}
