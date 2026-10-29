using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>A fissura do kit inicial: o começo é calmo e a crise aparece na barra de energia, não como um carregamento.</summary>
    public class FissuraTests : JogadorSimulado
    {
        [UnityTest]
        public IEnumerator Comeco_calmo_e_a_crise_depois_do_plantio_pisca_a_barra_de_energia()
        {
            yield return ComecarPartida();
            float inicio = Sessao.Clock.Time01;
            yield return Acelerar(() => Sessao.Clock.Time01 >= inicio + 40f / GameManager.DayLengthSeconds, "o relógio não andou");
            Assert.AreEqual(100f, Sessao.Status.Energy, "antes de plantar nenhuma crise: a energia fica cheia");
            Assert.IsFalse(Tela.PiscandoEnergia);
            yield return Foto("comeco-calmo");

            var canteiro = Interactable.All.OfType<FarmPlot>().First();
            yield return IrAte(canteiro);
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => canteiro.Model.State == PlotState.Preparado, "a terra não foi preparada");
            Assert.IsFalse(Tela.PiscandoEnergia, "gastar energia numa ação não pisca a barra");
            yield return Apertar(GameKey.Interagir);
            yield return Esperar(() => Tela.Current == GameUI.Window.Plantar, "o menu de plantio não abriu");
            yield return Clicar("Plantar");
            yield return Esperar(() => canteiro.Model.State == PlotState.Plantado, "a semente não foi plantada");
            yield return Esperar(() => !Tela.IsModal, "plantar não fechou o menu");
            Assert.IsFalse(Sessao.Status.FissuraEmEspera, "o primeiro plantio acorda a fissura");

            yield return Acelerar(() => Tela.PiscandoEnergia, "a primeira crise não veio depois do plantio");
            Assert.Less(Sessao.Status.Energy, 100f, "a crise tira energia");
            Assert.AreEqual(Mathf.RoundToInt(PlayerStatus.CriseFumoEnergia), Tela.EnergiaPerdidaNaBarra, "a barra mostra −15");
            yield return Foto("crise-barra-piscando");
        }

        [UnityTest]
        public IEnumerator Perdas_seguidas_somam_no_mesmo_menos_N_e_a_barra_volta_ao_normal()
        {
            yield return ComecarPartida();
            Assert.AreEqual(0, Tela.EnergiaPerdidaNaBarra, "sem perda, a barra não mostra −N");
            Sessao.OnCuca(); // −25
            Assert.AreEqual(25, Tela.EnergiaPerdidaNaBarra);
            Sessao.OnBernunciaSwallow(); // logo em seguida, −10
            Assert.AreEqual(35, Tela.EnergiaPerdidaNaBarra, "perdas em seguida somam no mesmo −N");
            yield return Foto("menos-35");

            yield return Esperar(() => !Tela.PiscandoEnergia, "a barra não parou de piscar", 4f);
            Assert.AreEqual(0, Tela.EnergiaPerdidaNaBarra);
            Sessao.OnBernunciaSwallow();
            Assert.AreEqual(10, Tela.EnergiaPerdidaNaBarra, "depois que a barra para, a próxima perda começa do zero");
        }

        [UnityTest]
        public IEnumerator Salvar_antes_de_plantar_e_continuar_mantem_o_comeco_calmo()
        {
            yield return ComecarPartida();
            yield return SalvarPeloMenu();
            yield return VoltarAoMenuInicial();
            yield return Clicar("Continuar");
            yield return Esperar(() => !Tela.IsModal, "o Continuar não abriu a partida");
            Assert.IsTrue(Sessao.Status.FissuraEmEspera, "o save guardou que ainda não houve plantio");

            float inicio = Sessao.Clock.Time01;
            yield return Acelerar(() => Sessao.Clock.Time01 >= inicio + 40f / GameManager.DayLengthSeconds, "o relógio não andou");
            Assert.AreEqual(100f, Sessao.Status.Energy, "carregar o jogo não traz a crise antes do plantio");
            Assert.IsFalse(Tela.PiscandoEnergia);
        }
    }
}
