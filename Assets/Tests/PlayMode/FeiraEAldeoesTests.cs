using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>Feira (vender e comprar sementes por nível) e aldeões.</summary>
    public class FeiraEAldeoesTests : JogadorSimulado
    {
        [UnityTest]
        public IEnumerator Feira_so_vende_sementes_do_nivel_da_raizeira()
        {
            yield return ComecarPartida();
            yield return Abrir(Interactable.All.OfType<Market>().First(), GameUI.Window.Feira);
            yield return Clicar("Comprar", 0); // a aba
            yield return Foto("feira-sementes");

            int moedas = Sessao.Money;
            int sementes = Sessao.Inventory.OfKind(ItemKind.Semente).Sum(kv => kv.Value);
            // depois da aba vem a pá ("Comprar" ou "Já é sua") e então as sementes, da mais fácil para a mais difícil
            yield return Clicar("Comprar", Sessao.TemPa ? 1 : 2);
            yield return Esperar(() => Sessao.Money < moedas, "comprar não gastou moedas");
            Assert.AreEqual(sementes + 1, Sessao.Inventory.OfKind(ItemKind.Semente).Sum(kv => kv.Value), "a semente foi para a bolsa");

            Assert.AreEqual(1, Sessao.Progresso.Nivel);
            yield return NaoConsegueClicar("Nível " + Progression.NivelMaximo); // o guaraná fica trancado no nível 1
        }

        [UnityTest]
        public IEnumerator Vender_uma_erva_na_feira_rende_moedas()
        {
            yield return ComecarPartida();
            yield return Abrir(Interactable.All.OfType<Market>().First(), GameUI.Window.Feira);
            yield return Foto("feira-vender");

            string erva = Inventory.Herb("mulungu"); // a primeira linha da bolsa
            int moedas = Sessao.Money;
            yield return Clicar("Vender 1", 0);
            yield return Esperar(() => Sessao.Inventory.Count(erva) == 0, "a erva não foi vendida");
            Assert.Greater(Sessao.Money, moedas, "vender rende moedas");
        }

        [UnityTest]
        public IEnumerator Entregar_remedio_cura_o_aldeao()
        {
            yield return ComecarPartida();
            var aldeao = Interactable.All.OfType<Villager>().First(v => v.Request != null);
            var pedido = aldeao.Request;
            var remedio = Sessao.Db.RecipesThatCure(pedido.Mal).First();
            Sessao.Inventory.Add(Inventory.Product(remedio.id)); // arranjo: o remédio certo já está na bolsa
            int moedas = Sessao.Money;

            yield return Abrir(aldeao, GameUI.Window.Aldeao);
            yield return Foto("aldeao");
            yield return Clicar("Entregar");
            yield return Esperar(() => pedido.Atendido, "o aldeão não foi curado");
            Assert.AreEqual(moedas + pedido.Recompensa, Sessao.Money, "curar paga a recompensa");
        }
    }
}
