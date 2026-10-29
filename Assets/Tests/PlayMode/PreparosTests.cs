using System.Collections;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>Caldeirão, Bolsa, varal e pilão, com o mulungu que a vó deixou no kit inicial.</summary>
    public class PreparosTests : JogadorSimulado
    {
        [UnityTest]
        public IEnumerator Cha_de_mulungu_do_caldeirao_usado_pela_Bolsa_alivia_a_fissura()
        {
            yield return ComecarPartida();
            yield return Abrir(Estacao(GameUI.Window.Caldeirao), GameUI.Window.Caldeirao);
            yield return Foto("caldeirao");
            string cha = Inventory.Product(GameDatabase.SimpleRemedyPrefix + "mulungu");
            yield return Clicar("Preparar"); // o único preparo com ingredientes na bolsa
            yield return Esperar(() => Sessao.Inventory.Count(cha) == 1, "o chá de mulungu não foi preparado");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o caldeirão");

            int doses = Sessao.Status.DosesRestantes(Ailment.VontadeDeFumar);
            yield return Apertar(GameKey.Bolsa);
            yield return Esperar(() => Tela.Current == GameUI.Window.Bolsa, "a tecla I não abriu a Bolsa");
            yield return Foto("bolsa-com-cha");
            yield return Clicar("Usar");
            yield return Esperar(() => Sessao.Status.DosesRestantes(Ailment.VontadeDeFumar) == doses - 1, "o chá não contou uma dose para a fissura");
            Assert.AreEqual(0, Sessao.Inventory.Count(cha), "usar gasta o chá");
        }

        [UnityTest]
        public IEnumerator Varal_seca_a_erva_e_o_pilao_vira_po()
        {
            yield return ComecarPartida();
            var varal = Estacao(GameUI.Window.Varal);
            yield return Abrir(varal, GameUI.Window.Varal);
            yield return Clicar("Pendurar"); // o mulungu fresco do kit
            yield return Esperar(() => Sessao.Rack.Slots.Count == 1, "a erva não foi pendurada");
            yield return Foto("erva-no-varal");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o varal");

            yield return Acelerar(() => Sessao.Rack.ReadyCount == 1, "a erva não secou");
            yield return Abrir(varal, GameUI.Window.Varal);
            yield return Clicar("Recolher secas (1)");
            yield return Esperar(() => Sessao.Inventory.Count(Inventory.Dried("mulungu")) == 1, "a erva seca não foi para a bolsa");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o varal");

            yield return Abrir(Estacao(GameUI.Window.Pilao), GameUI.Window.Pilao);
            yield return Foto("pilao");
            yield return Clicar("Pilar");
            yield return Esperar(() => Sessao.Inventory.Count(Inventory.Powder("mulungu")) == 1, "a erva seca não virou pó");
        }

        /// <summary>Playtest de 29/10: a jogadora secou todo o mulungu e o chá ficava travado em "Faltam ingredientes".</summary>
        [UnityTest]
        public IEnumerator Mulungu_seco_tambem_faz_o_cha_no_caldeirao()
        {
            yield return ComecarPartida();
            var varal = Estacao(GameUI.Window.Varal);
            int frescos = Sessao.Inventory.Count(Inventory.Herb("mulungu"));
            yield return Abrir(varal, GameUI.Window.Varal);
            for (int i = 0; i < frescos; i++)
            {
                yield return Clicar("Pendurar");
                yield return Esperar(() => Sessao.Rack.Slots.Count == i + 1, "a erva não foi pendurada");
            }
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o varal");
            yield return Acelerar(() => Sessao.Rack.ReadyCount == frescos, "as ervas não secaram");
            yield return Abrir(varal, GameUI.Window.Varal);
            yield return Clicar("Recolher secas (" + frescos + ")");
            yield return Esperar(() => Sessao.Inventory.Count(Inventory.Dried("mulungu")) == frescos, "as ervas secas não foram para a bolsa");
            yield return Apertar(GameKey.Fechar);
            yield return Esperar(() => !Tela.IsModal, "Esc não fechou o varal");
            Assert.AreEqual(0, Sessao.Inventory.Count(Inventory.Herb("mulungu")), "não sobrou mulungu fresco: só o seco pode servir");

            yield return Abrir(Estacao(GameUI.Window.Caldeirao), GameUI.Window.Caldeirao);
            yield return Foto("caldeirao-so-com-mulungu-seco");
            string cha = Inventory.Product(GameDatabase.SimpleRemedyPrefix + "mulungu");
            yield return Clicar("Preparar");
            yield return Esperar(() => Sessao.Inventory.Count(cha) == 1, "o chá de mulungu não saiu com a erva seca");
            Assert.AreEqual(frescos - 1, Sessao.Inventory.Count(Inventory.Dried("mulungu")), "o chá gastou um mulungu seco");
        }
    }
}
