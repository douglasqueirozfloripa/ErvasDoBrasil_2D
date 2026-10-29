using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;

namespace ErvasDoBrasil.Tests
{
    /// <summary>
    /// Pedidos da vila: valem uma semana ou até os três serem atendidos (antes trocavam todo dia),
    /// e cada aldeão tem a sua arte, que segue o nome do pedido.
    /// </summary>
    public class PedidosDaVilaTests
    {
        const float Dia = 100f;

        static GameSession NovaPartida()
        {
            var s = new GameSession(TestData.Db(), new FixedRandom(), Dia, 4);
            s.StartNewGame();
            return s;
        }

        /// <summary>Um dia inteiro acordada: passa da meia-noite e a manhã seguinte começa.</summary>
        static void PassarUmDia(GameSession s) { s.Tick(Dia); }

        static VillagerRequest[] Pedidos(GameSession s) { return s.Requests.ToArray(); }

        static void Curar(GameSession s, VillagerRequest req)
        {
            var remedio = s.Db.RecipesThatCure(req.Mal).First();
            s.Inventory.Add(Inventory.Product(remedio.id));
            Assert.IsTrue(s.TreatVillager(req, remedio.id));
        }

        [Test]
        public void Pedidos_valem_uma_semana()
        {
            var s = NovaPartida();
            var semana = Pedidos(s);
            Assert.AreEqual(3, semana.Length);
            Assert.AreEqual(1, s.PedidosDesde);
            Assert.AreEqual(7, s.PedidosValemAte);

            for (int i = 0; i < 6; i++) PassarUmDia(s);
            Assert.AreEqual(7, s.Clock.Day);
            CollectionAssert.AreEqual(semana, Pedidos(s), "do dia 1 ao 7 são os mesmos pedidos (antes trocavam todo dia)");

            PassarUmDia(s);
            Assert.AreEqual(8, s.Clock.Day);
            Assert.IsFalse(Pedidos(s).Intersect(semana).Any(), "no dia 8 chegam pedidos novos");
            Assert.AreEqual(8, s.PedidosDesde);
            Assert.IsTrue(s.Avisos.EmOrdem.Any(a => a.Texto.StartsWith("Passou uma semana", System.StringComparison.Ordinal)), "o aviso explica por que trocou");
        }

        [Test]
        public void Atender_os_tres_traz_pedidos_novos_na_manha_seguinte()
        {
            var s = NovaPartida();
            var semana = Pedidos(s);
            foreach (var req in semana) Curar(s, req);
            Assert.IsTrue(s.PedidosConcluidos);
            Assert.IsTrue(s.Avisos.EmOrdem.Any(a => a.Texto.StartsWith("Você atendeu os 3 pedidos da vila", System.StringComparison.Ordinal)), "o terceiro atendido avisa que amanhã tem mais");
            CollectionAssert.AreEqual(semana, Pedidos(s), "os curados continuam na vila até o fim do dia");

            PassarUmDia(s);
            Assert.IsFalse(Pedidos(s).Intersect(semana).Any(), "na manhã seguinte chegam pedidos novos, antes da semana acabar");
            Assert.IsTrue(Pedidos(s).All(r => !r.Atendido));
            Assert.AreEqual(2, s.PedidosDesde, "e a semana deles começa hoje");
            Assert.IsTrue(s.Avisos.EmOrdem.Any(a => a.Texto == "Chegaram pedidos novos na vila."));
        }

        [Test]
        public void Atender_so_alguns_mantem_os_pedidos()
        {
            var s = NovaPartida();
            var semana = Pedidos(s);
            Curar(s, semana[0]);
            Curar(s, semana[1]);
            Assert.IsFalse(s.PedidosConcluidos);
            PassarUmDia(s);
            CollectionAssert.AreEqual(semana, Pedidos(s), "com um pedido em aberto, ninguém troca antes da semana");
        }

        [Test]
        public void Dormir_tambem_conta_os_dias_da_semana()
        {
            var s = NovaPartida();
            var semana = Pedidos(s);
            for (int dia = 1; dia <= 7; dia++)
            {
                s.Tick(Dia * 0.55f); // até depois das 18h
                Assert.IsTrue(s.Sleep(), "dorme no dia " + s.Clock.Day);
            }
            Assert.AreEqual(8, s.Clock.Day);
            Assert.IsFalse(Pedidos(s).Intersect(semana).Any());
        }

        [Test]
        public void Semana_dos_pedidos_volta_com_o_jogo_salvo()
        {
            var s = NovaPartida();
            for (int i = 0; i < 3; i++) PassarUmDia(s);
            var json = JsonUtility.ToJson(SaveGame.Capture(s));
            var carregada = new GameSession(TestData.Db(), new FixedRandom(), Dia, 4);
            SaveGame.Restore(carregada, JsonUtility.FromJson<SaveData>(json));
            Assert.AreEqual(1, carregada.PedidosDesde, "os pedidos do dia 1 continuam valendo até o dia 7");
            Assert.AreEqual(7, carregada.PedidosValemAte);
        }

        [Test]
        public void Save_antigo_ou_editado_conta_a_semana_a_partir_do_dia_carregado()
        {
            var s = NovaPartida();
            for (int i = 0; i < 4; i++) PassarUmDia(s);
            var d = SaveGame.Capture(s);

            d.pedidosDesde = 0; // save de antes desta regra
            var antigo = new GameSession(TestData.Db(), new FixedRandom(), Dia, 4);
            SaveGame.Restore(antigo, d);
            Assert.AreEqual(5, antigo.PedidosDesde);

            d.pedidosDesde = 999; // dia no futuro: os pedidos nunca venceriam
            var editado = new GameSession(TestData.Db(), new FixedRandom(), Dia, 4);
            SaveGame.Restore(editado, d);
            Assert.AreEqual(5, editado.PedidosDesde);
        }

        [Test]
        public void Save_sem_pedidos_recebe_pedidos_na_manha_seguinte()
        {
            var s = NovaPartida();
            var d = SaveGame.Capture(s);
            d.pedidos.Clear();
            var carregada = new GameSession(TestData.Db(), new FixedRandom(), Dia, 4);
            SaveGame.Restore(carregada, d);
            Assert.AreEqual(0, carregada.Requests.Count);
            PassarUmDia(carregada);
            Assert.AreEqual(3, carregada.Requests.Count, "a vila não fica uma semana vazia");
        }

        [Test]
        public void Cada_aldeao_tem_a_sua_arte()
        {
            Assert.AreEqual(Village.Nomes.Length, Village.Ids.Length, "um visual para cada nome");
            CollectionAssert.AllItemsAreUnique(Village.Ids);
            Assert.AreEqual("npc_tonhao", Village.Arte("Tonhão da Venda"), "o Tonhão tem a arte dele, de homem");
            Assert.AreEqual("npc_maria", Village.Arte("Benzedeira Maria"));
            Assert.IsNull(Village.Arte("Fulano de Tal"), "nome desconhecido (save editado) fica com a arte que já estava");
        }
    }
}
