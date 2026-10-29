using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    /// <summary>
    /// Playtest de 29/10/2026: o save tinha 4 mulungus secos, nenhum fresco, e o chá ficava travado em "Faltam ingredientes",
    /// porque o preparo simples pedia a erva fresca. Agora ele aceita a erva fresca ou seca.
    /// </summary>
    public class ChaFrescaOuSecaTests
    {
        const string Cha = GameDatabase.SimpleRemedyPrefix + "mulungu";

        GameDatabase db;
        Inventory inv;
        PlayerStatus st;

        [SetUp]
        public void SetUp()
        {
            db = TestData.Db();
            inv = new Inventory();
            st = new PlayerStatus();
        }

        [Test]
        public void Preparo_simples_gerado_pede_a_erva_fresca_ou_seca()
        {
            var ing = db.Recipe(Cha).ingredientes[0];
            Assert.AreEqual(HerbForm.FrescaOuSeca, ing.Form);
            Assert.AreEqual("erva:mulungu", Inventory.IngredientKey(ing), "a chave principal continua sendo a fresca");
            CollectionAssert.AreEqual(new[] { "erva:mulungu", "seca:mulungu" }, Inventory.IngredientKeys(ing));
        }

        [Test]
        public void Mulungu_seco_na_bolsa_faz_o_cha_como_no_save_de_29_10()
        {
            inv.Add(Inventory.Dried("mulungu"), 4);
            st.Add(Ailment.VontadeDeFumar);
            var r = db.Recipe(Cha);
            Assert.IsTrue(Crafting.HasIngredients(r, inv), "4 mulungus secos bastam para o chá");
            Assert.AreEqual(CraftOutcome.Sucesso, Crafting.Craft(r, inv, st, new FixedRandom()));
            Assert.AreEqual(3, inv.Count(Inventory.Dried("mulungu")), "o chá gasta um mulungu seco");
            Assert.AreEqual(1, inv.Count(Inventory.Product(Cha)));
        }

        [Test]
        public void Cha_gasta_a_erva_fresca_antes_da_seca()
        {
            inv.Add(Inventory.Herb("mulungu"));
            inv.Add(Inventory.Dried("mulungu"));
            var r = db.Recipe(Cha);
            Assert.AreEqual(2, inv.Count(r.ingredientes[0]), "a contagem soma fresca e seca");

            Assert.AreEqual(CraftOutcome.Sucesso, Crafting.Craft(r, inv, st, new FixedRandom()));
            Assert.AreEqual(0, inv.Count(Inventory.Herb("mulungu")));
            Assert.AreEqual(1, inv.Count(Inventory.Dried("mulungu")));

            Assert.AreEqual(CraftOutcome.Sucesso, Crafting.Craft(r, inv, st, new FixedRandom()));
            Assert.AreEqual(0, inv.Count(Inventory.Dried("mulungu")));
            Assert.AreEqual(CraftOutcome.FaltamIngredientes, Crafting.Craft(r, inv, st, new FixedRandom()));
        }

        [Test]
        public void Sem_o_bastante_nao_tira_nada_da_bolsa()
        {
            var ing = new Ingredient("mulungu", 3, HerbForm.FrescaOuSeca);
            inv.Add(Inventory.Herb("mulungu"));
            inv.Add(Inventory.Dried("mulungu"));
            Assert.IsFalse(inv.Has(ing));
            Assert.IsFalse(inv.Remove(ing));
            Assert.AreEqual(1, inv.Count(Inventory.Herb("mulungu")));
            Assert.AreEqual(1, inv.Count(Inventory.Dried("mulungu")));
        }

        [Test]
        public void Po_nao_serve_para_o_cha_e_o_elixir_continua_pedindo_po()
        {
            inv.Add(Inventory.Powder("mulungu"), 2);
            Assert.AreEqual(CraftOutcome.FaltamIngredientes, Crafting.Craft(db.Recipe(Cha), inv, st, new FixedRandom()));

            var elixir = db.Recipe("elixir_recomeco");
            inv.Add(Inventory.Dried("camomila")); // o elixir pede camomila fresca: a seca não serve ali
            Assert.AreEqual(CraftOutcome.FaltamIngredientes, Crafting.Craft(elixir, inv, st, new FixedRandom()));
            inv.Add(Inventory.Herb("camomila"));
            Assert.AreEqual(CraftOutcome.Sucesso, Crafting.Craft(elixir, inv, st, new FixedRandom()));
            Assert.AreEqual(0, inv.Count(Inventory.Powder("mulungu")));
        }

        [Test]
        public void Forma_do_json_aceita_fresca_ou_seca_e_desconhecida_vira_fresca()
        {
            Assert.AreEqual(HerbForm.FrescaOuSeca, new Ingredient { plantId = "boldo", forma = "frescaouseca" }.Form);
            Assert.AreEqual(HerbForm.Fresca, new Ingredient { plantId = "boldo", forma = "Triturada" }.Form);
            Assert.AreEqual(HerbForm.Fresca, new Ingredient { plantId = "boldo" }.Form);
        }
    }
}
