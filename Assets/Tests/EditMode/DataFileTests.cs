using System.IO;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;

namespace ErvasDoBrasil.Tests
{
    /// <summary>Valida os JSONs reais do jogo (Assets/Resources/Data) — roda só dentro da Unity.</summary>
    public class DataFileTests
    {
        GameDatabase db;

        [OneTimeSetUp]
        public void Load()
        {
            var dir = Path.Combine(Application.dataPath, "Resources/Data");
            var plants = JsonUtility.FromJson<PlantCatalog>(File.ReadAllText(Path.Combine(dir, "plants.json")));
            var recipes = JsonUtility.FromJson<RecipeCatalog>(File.ReadAllText(Path.Combine(dir, "recipes.json")));
            db = new GameDatabase(plants.plantas, recipes.receitas);
        }

        [Test]
        public void Dados_do_jogo_sao_validos()
        {
            var errors = db.Validate();
            Assert.IsEmpty(errors, string.Join("\n", errors.ToArray()));
        }

        [Test]
        public void Plantas_do_briefing_existem()
        {
            foreach (var id in new[] { "boldo", "camomila", "mulungu", "espada_sao_jorge", "arruda", "guarana", "quebra_pedra" })
                Assert.IsNotNull(db.Plant(id), id);
        }

        [Test]
        public void Toda_planta_tem_nivel_valido_e_o_kit_inicial_e_do_nivel_1()
        {
            foreach (var p in db.Plants)
                Assert.That(p.NivelMinimo, Is.InRange(1, Progression.NivelMaximo), p.id);

            var s = new GameSession(db, new FixedRandom());
            s.StartNewGame();
            foreach (var kv in s.Inventory.OfKind(ItemKind.Semente))
            {
                ItemKind kind; string id;
                Assert.IsTrue(Inventory.TryParseKey(kv.Key, out kind, out id), "chave inválida: " + kv.Key);
                Assert.AreEqual(1, db.Plant(id).NivelMinimo, "semente do kit precisa ser plantável no início: " + id);
            }
        }

        [Test]
        public void Mulungu_acalma_a_ansiedade_e_a_fissura()
        {
            var cura = db.Plant("mulungu").Cures.ToList();
            CollectionAssert.Contains(cura, Ailment.VontadeDeFumar);
            CollectionAssert.Contains(cura, Ailment.Ansiedade);
        }

        [Test]
        public void Toda_planta_tem_texto_para_o_Livro()
        {
            foreach (var p in db.Plants)
            {
                Assert.IsFalse(string.IsNullOrEmpty(p.efeitoNoJogo), p.id);
                Assert.IsFalse(string.IsNullOrEmpty(p.culturaPopular), p.id);
                Assert.IsFalse(string.IsNullOrEmpty(p.cuidados), p.id);
            }
        }

        [Test]
        public void Plantas_aliadas_das_assombracoes_existem()
        {
            foreach (var l in SpiritCodex.All)
                foreach (var id in l.PlantasQueProtegem)
                    Assert.IsNotNull(db.Plant(id), l.Nome + " cita planta inexistente: " + id);
        }

        [Test]
        public void Existem_plantas_na_mata_para_coletar()
        {
            Assert.GreaterOrEqual(db.Plants.Count(p => p.selvagem), 5);
        }
    }
}
