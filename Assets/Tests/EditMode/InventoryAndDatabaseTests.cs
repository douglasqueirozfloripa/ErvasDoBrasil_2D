using System;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    public class InventoryTests
    {
        Inventory inv;

        [SetUp]
        public void SetUp() { inv = new Inventory(); }

        [Test]
        public void Add_e_Count_acumulam()
        {
            inv.Add(Inventory.Herb("boldo"), 2);
            inv.Add(Inventory.Herb("boldo"));
            Assert.AreEqual(3, inv.Count("erva:boldo"));
            Assert.AreEqual(3, inv.TotalItems);
        }

        [Test]
        public void Remove_nao_remove_parcialmente()
        {
            inv.Add(Inventory.Seed("boldo"), 1);
            Assert.IsFalse(inv.Remove(Inventory.Seed("boldo"), 2));
            Assert.AreEqual(1, inv.Count(Inventory.Seed("boldo")));
        }

        [Test]
        public void Remove_tudo_apaga_a_chave()
        {
            inv.Add(Inventory.Herb("boldo"), 2);
            Assert.IsTrue(inv.Remove(Inventory.Herb("boldo"), 2));
            Assert.AreEqual(0, inv.All.Count());
        }

        [Test]
        public void Quantidade_invalida_lanca_excecao()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Add(Inventory.Herb("x"), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Remove(Inventory.Herb("x"), -1));
            Assert.Throws<ArgumentException>(() => inv.Add("chave-sem-prefixo", 1));
        }

        [TestCase("erva:boldo", ItemKind.Erva, "boldo")]
        [TestCase("seca:guine", ItemKind.ErvaSeca, "guine")]
        [TestCase("po:mulungu", ItemKind.Po, "mulungu")]
        [TestCase("semente:arruda", ItemKind.Semente, "arruda")]
        [TestCase("prod:remedio_boldo", ItemKind.Produto, "remedio_boldo")]
        public void TryParseKey_reconhece_todos_os_tipos(string key, ItemKind kind, string id)
        {
            ItemKind k; string i;
            Assert.IsTrue(Inventory.TryParseKey(key, out k, out i));
            Assert.AreEqual(kind, k);
            Assert.AreEqual(id, i);
        }

        [Test]
        public void HerbKey_usa_a_forma_do_ingrediente()
        {
            Assert.AreEqual("po:mulungu", Inventory.IngredientKey(new Ingredient("mulungu", 1, HerbForm.Po)));
            Assert.AreEqual("seca:guine", Inventory.IngredientKey(new Ingredient("guine", 1, HerbForm.Seca)));
            Assert.AreEqual("erva:boldo", Inventory.IngredientKey(new Ingredient { plantId = "boldo", qtd = 1 }));
        }

        [Test]
        public void RandomKey_pondera_pela_quantidade_e_retorna_null_se_vazio()
        {
            Assert.IsNull(inv.RandomKey(new FixedRandom()));
            inv.Add(Inventory.Herb("arruda"), 1);   // índice 0
            inv.Add(Inventory.Herb("boldo"), 3);    // índices 1..3
            Assert.AreEqual("erva:arruda", inv.RandomKey(new FixedRandom().Ints(0)));
            Assert.AreEqual("erva:boldo", inv.RandomKey(new FixedRandom().Ints(3)));
        }

        [Test]
        public void Changed_dispara_ao_alterar()
        {
            int calls = 0;
            inv.Changed += () => calls++;
            inv.Add(Inventory.Herb("boldo"));
            inv.Remove(Inventory.Herb("boldo"));
            Assert.AreEqual(2, calls);
        }
    }

    public class GameDatabaseTests
    {
        [Test]
        public void Gera_preparo_simples_para_cada_planta_com_remedio()
        {
            var db = TestData.Db();
            var cha = db.Recipe("remedio_boldo");
            Assert.IsNotNull(cha);
            Assert.AreEqual("Chá de boldo", cha.nome);
            Assert.AreEqual(ItemEffect.Cura, cha.Effect);
            CollectionAssert.Contains(cha.Cures.ToList(), Ailment.Enjoo);
            Assert.IsNull(db.Recipe("remedio_espada_sao_jorge"), "Espada é só proteção, não vira chá");
        }

        [Test]
        public void Defumacao_simples_tem_raio()
        {
            var r = TestData.Db().Recipe("remedio_guine");
            Assert.AreEqual(ItemEffect.Banir, r.Effect);
            Assert.Greater(r.raio, 0f);
        }

        [Test]
        public void Ids_duplicados_viram_erro_de_validacao()
        {
            var plants = TestData.Plants();
            plants.Add(TestData.Plant("boldo", new[] { "Enjoo" }, "outro"));
            var errors = new GameDatabase(plants, TestData.Recipes()).Validate();
            Assert.IsTrue(errors.Any(e => e.Contains("duplicada")));
        }

        [Test]
        public void Validate_detecta_ingrediente_inexistente_e_mal_desconhecido()
        {
            var recipes = TestData.Recipes();
            recipes.Add(new RecipeData
            {
                id = "quebrada", nome = "x", efeito = "Cura", precoVenda = 1,
                cura = new[] { "Saudade" },
                ingredientes = new[] { new Ingredient("planta_fantasma", 1) }
            });
            var errors = new GameDatabase(TestData.Plants(), recipes).Validate();
            Assert.IsTrue(errors.Any(e => e.Contains("planta_fantasma")));
            Assert.IsTrue(errors.Any(e => e.Contains("Saudade")));
        }

        [Test]
        public void Validate_aponta_males_sem_cura()
        {
            var errors = TestData.Db().Validate();
            Assert.IsTrue(errors.Any(e => e.Contains(Ailments.Nome(Ailment.Tosse))));
            Assert.IsFalse(errors.Any(e => e.Contains("cura " + Ailments.Nome(Ailment.Ansiedade))));
        }

        [Test]
        public void Consultas_por_mal()
        {
            var db = TestData.Db();
            CollectionAssert.Contains(db.PlantsThatCure(Ailment.DorDeBarriga).Select(p => p.id).ToList(), "boldo");
            var ids = db.RecipesThatCure(Ailment.VontadeDeFumar).Select(r => r.id).ToList();
            CollectionAssert.Contains(ids, "remedio_mulungu");
            CollectionAssert.Contains(ids, "elixir_recomeco");
        }

        [Test]
        public void Ailments_TryParse_ignora_caixa_e_rejeita_nenhum()
        {
            Ailment a;
            Assert.IsTrue(Ailments.TryParse("mauolhado", out a));
            Assert.AreEqual(Ailment.MauOlhado, a);
            Assert.IsFalse(Ailments.TryParse("Nenhum", out _));
            Assert.IsFalse(Ailments.TryParse("", out _));
            Assert.IsFalse(Ailments.TryParse("99", out _));
        }

        [Test]
        public void Todo_mal_tem_ficha_completa()
        {
            foreach (Ailment a in Enum.GetValues(typeof(Ailment)))
            {
                if (a == Ailment.Nenhum) continue;
                var info = Ailments.Info(a);
                Assert.IsNotNull(info, a.ToString());
                Assert.IsFalse(string.IsNullOrEmpty(info.Penalidade), a.ToString());
                Assert.GreaterOrEqual(info.DosesParaCurar, 1);
            }
        }
    }
}
