using System.Collections;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// Cada aldeão aparece com a sua arte, a do nome do pedido (o relato: o Tonhão da Venda caía no lugar da Benzedeira
    /// e aparecia de senhora), a arte acompanha quando os pedidos trocam, e a janela diz até quando vale o pedido.
    /// </summary>
    public class AldeoesTests : JogadorSimulado
    {
        static Villager[] Aldeoes() { return Interactable.All.OfType<Villager>().OrderBy(v => v.Index).ToArray(); }

        static void ConferirArtes()
        {
            foreach (var v in Aldeoes())
            {
                string arte = Village.Arte(v.Request.Nome);
                Assert.IsNotNull(ArtLibrary.Get(arte), "falta o PNG " + arte);
                Assert.AreEqual(arte, v.Arte, v.Request.Nome + " está com a arte de outro aldeão");
                Assert.AreSame(ArtLibrary.Get(arte), v.GetComponent<SpriteRenderer>().sprite, v.Request.Nome + " está com o sprite errado");
            }
        }

        [UnityTest]
        public IEnumerator Cada_aldeao_aparece_com_a_arte_do_seu_nome()
        {
            yield return ComecarPartida();
            string[] nomes = { "Benzedeira Maria", "Tonhão da Venda", "Professora Lúcia" };
            for (int i = 0; i < nomes.Length; i++) Sessao.Requests[i].Nome = nomes[i]; // o Tonhão no lugar do meio, o da Benzedeira de antes
            yield return null;
            ConferirArtes();
            Assert.AreEqual("npc_tonhao", Aldeoes()[1].Arte);

            var a = Aldeoes();
            yield return AndarAte((a[0].transform.position + a[1].transform.position) / 2f); // entre dois, para não tampar nenhum
            yield return Foto("aldeoes-cada-um-com-a-sua-arte");

            yield return Abrir(a[1], GameUI.Window.Aldeao);
            yield return Foto("tonhao-e-ate-quando-vale-o-pedido");
        }

        [UnityTest]
        public IEnumerator Quando_os_pedidos_trocam_a_arte_acompanha_o_novo_aldeao()
        {
            yield return ComecarPartida();
            for (int vez = 0; vez < 4; vez++)
            {
                Sessao.RefreshRequests(); // outra semana: outros aldeões nos mesmos lugares
                yield return null;
                ConferirArtes();
            }
        }
    }
}
