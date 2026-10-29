using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;

namespace ErvasDoBrasil.Tests
{
    /// <summary>Jogo salvo, nível de raizeira e as crises de ansiedade/fissura.</summary>
    public class SaveAndProgressionTests
    {
        static GameSession NewSession(GameDatabase db = null)
        {
            return new GameSession(db ?? TestData.Db(), new FixedRandom(), 100f, 4);
        }

        // ───────── Jogo salvo ─────────

        [Test]
        public void Salvar_e_carregar_devolve_a_mesma_partida()
        {
            var s = NewSession();
            s.StartNewGame();
            var plot = s.Plots[0];
            Assert.IsTrue(s.PreparePlot(plot));
            Assert.IsTrue(s.PlantSeed(plot, "boldo"));
            Assert.IsTrue(s.WaterPlot(plot));
            s.Tick(3f);
            s.Inventory.Add(Inventory.Herb("camomila"), 2);
            Assert.IsTrue(s.HangToDry("camomila"));
            s.Status.Add(Ailment.Gripe);
            s.Status.AmuletoDias = 2;
            s.Progresso.Add(50);
            s.Requests[0].Atendido = true;

            // passa pelo JSON como o jogo faz
            var json = JsonUtility.ToJson(SaveGame.Capture(s));
            var loaded = NewSession();
            SaveGame.Restore(loaded, JsonUtility.FromJson<SaveData>(json));

            Assert.AreEqual(s.Clock.Day, loaded.Clock.Day);
            Assert.AreEqual(s.Clock.Time01, loaded.Clock.Time01, 0.0001f);
            Assert.AreEqual(s.Money, loaded.Money);
            Assert.AreEqual(50, loaded.Progresso.Xp);
            Assert.AreEqual(2, loaded.Progresso.Nivel);
            CollectionAssert.AreEqual(s.Inventory.All.ToList(), loaded.Inventory.All.ToList());
            CollectionAssert.AreEqual(s.Herbarium.All.ToList(), loaded.Herbarium.All.ToList());
            Assert.AreEqual("boldo", loaded.Plots[0].PlantId);
            Assert.AreEqual(s.Plots[0].Growth, loaded.Plots[0].Growth, 0.0001f);
            Assert.IsTrue(loaded.Plots[0].Watered);
            Assert.IsTrue(loaded.Plots[1].IsEmpty, "canteiro vazio continua vazio (o JSON grava \"\")");
            Assert.AreEqual(1, loaded.Rack.Slots.Count);
            Assert.AreEqual(s.Status.Energy, loaded.Status.Energy, 0.001f);
            Assert.AreEqual(s.Status.Sanidade, loaded.Status.Sanidade, 0.001f);
            Assert.IsTrue(loaded.Status.Has(Ailment.Gripe));
            Assert.AreEqual(s.Status.DosesRestantes(Ailment.VontadeDeFumar), loaded.Status.DosesRestantes(Ailment.VontadeDeFumar));
            Assert.AreEqual(2, loaded.Status.AmuletoDias);
            Assert.AreEqual(s.Requests.Count, loaded.Requests.Count);
            Assert.AreEqual(s.Requests[0].Mal, loaded.Requests[0].Mal);
            Assert.IsTrue(loaded.Requests[0].Atendido);
        }

        [Test]
        public void Salvar_antes_do_primeiro_plantio_mantem_a_fissura_em_espera()
        {
            var s = NewSession();
            s.StartNewGame();
            var loaded = NewSession();
            SaveGame.Restore(loaded, JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(SaveGame.Capture(s))));
            Assert.IsTrue(loaded.Status.FissuraEmEspera, "carregar não adianta a primeira crise");

            // save de antes desta regra, sem o campo: as crises correm como sempre correram
            var antigo = new SaveData { dia = 1 };
            antigo.status.males.Add(new SaveMal { mal = "VontadeDeFumar", doses = 3 });
            var velho = NewSession();
            SaveGame.Restore(velho, antigo);
            Assert.IsFalse(velho.Status.FissuraEmEspera);
        }

        [Test]
        public void Carregar_ignora_plantas_que_nao_existem_mais()
        {
            var d = new SaveData { dia = 2 };
            d.bolsa.Add(new SaveItem { chave = "erva:boldo", qtd = 1 });
            d.bolsa.Add(new SaveItem { chave = "chave-quebrada", qtd = 3 });
            d.herbario.Add("planta_removida");
            d.canteiros.Add(new SaveCanteiro { planta = "planta_removida", crescimento = 0.5f, preparado = true });
            d.status.males.Add(new SaveMal { mal = "MalInventado", doses = 1 });

            var s = NewSession();
            SaveGame.Restore(s, d);

            Assert.AreEqual(1, s.Inventory.TotalItems);
            Assert.AreEqual(0, s.Herbarium.Count);
            Assert.IsTrue(s.Plots[0].IsEmpty);
            Assert.AreEqual(PlotState.Preparado, s.Plots[0].State);
            Assert.AreEqual(0, s.Status.ActiveAilments.Count);
        }

        // ───────── Nível de raizeira ─────────

        [Test]
        public void Experiencia_sobe_de_nivel_e_avisa_cada_nivel()
        {
            var p = new Progression();
            var niveis = new List<int>();
            p.LevelUp += niveis.Add;
            p.Add(Progression.XpPorNivel[1] - 1);
            Assert.AreEqual(1, p.Nivel);
            p.Add(Progression.XpPorNivel[2]); // pula dois níveis de uma vez
            Assert.AreEqual(3, p.Nivel);
            CollectionAssert.AreEqual(new[] { 2, 3 }, niveis);
        }

        [Test]
        public void Planta_de_nivel_alto_fica_trancada_na_feira_no_plantio_e_na_mata()
        {
            var db = TestData.Db();
            db.Plant("guarana").nivel = 3;
            var s = NewSession(db);
            s.StartNewGame();
            int money = s.Money;

            Assert.IsFalse(s.BuySeed("guarana"));
            Assert.AreEqual(money, s.Money);
            Assert.AreEqual(0, s.CollectWild("guarana"));
            s.Inventory.Add(Inventory.Seed("guarana"));
            var plot = s.Plots[0];
            Assert.IsTrue(s.PreparePlot(plot));
            Assert.IsFalse(s.PlantSeed(plot, "guarana"));

            s.Progresso.Add(Progression.XpPorNivel[2]);
            Assert.IsTrue(s.PlantSeed(plot, "guarana"));
            Assert.IsTrue(s.BuySeed("guarana"));
        }

        [Test]
        public void Coletar_e_descobrir_planta_dao_experiencia()
        {
            var s = NewSession();
            s.StartNewGame();
            Assert.Greater(s.CollectWild("guarana"), 0);
            Assert.AreEqual(Progression.XpColetaSelvagem + Progression.XpNovaPlanta, s.Progresso.Xp);
        }

        // ───────── Mulungu, ansiedade e fissura ─────────

        [Test]
        public void Novo_jogo_ja_conhece_o_mulungu_da_vo()
        {
            var s = NewSession();
            s.StartNewGame();
            Assert.IsTrue(s.Herbarium.IsDiscovered("mulungu"), "sem isso o chá não aparece no caldeirão");
            Assert.AreEqual(0, s.Progresso.Xp, "a página deixada pela vó não conta experiência");
        }

        [Test]
        public void Ansiedade_curada_com_sanidade_baixa_nao_volta_no_quadro_seguinte()
        {
            var st = new PlayerStatus();
            st.LoseSanity(80f); // 20: cruzou o limiar
            Assert.IsTrue(st.Has(Ailment.Ansiedade));
            st.CureDose(Ailment.Ansiedade, 99);
            st.Tick(1f, true); // a noite continua drenando
            Assert.IsFalse(st.Has(Ailment.Ansiedade));

            st.RestoreSanity(30f); // volta acima de 30...
            st.LoseSanity(20f);    // ...e cai de novo: aí sim volta
            Assert.IsTrue(st.Has(Ailment.Ansiedade));
        }

        [Test]
        public void Ansiosa_recupera_sanidade_pela_metade_durante_o_dia()
        {
            var st = new PlayerStatus();
            st.LoseSanity(50f);
            st.Add(Ailment.Ansiedade);
            st.Tick(10f, false);
            Assert.AreEqual(50f + PlayerStatus.RegenSanidadeDia * 10f * 0.5f, st.Sanidade, 0.001f);
        }

        [Test]
        public void Doses_tomadas_espacam_as_crises_de_fissura()
        {
            var st = new PlayerStatus();
            st.Add(Ailment.VontadeDeFumar);
            Assert.AreEqual(PlayerStatus.CriseFumoIntervalo, st.IntervaloCriseFumo, 0.001f);
            st.CureDose(Ailment.VontadeDeFumar);
            Assert.AreEqual(PlayerStatus.CriseFumoIntervalo * 2f, st.IntervaloCriseFumo, 0.001f);
        }

        [Test]
        public void Dormir_bem_acalma_a_ansiedade()
        {
            var s = NewSession();
            s.StartNewGame();
            s.Status.Add(Ailment.Ansiedade);
            s.Tick(55f); // 06h + 55% do dia: já é noite
            Assert.IsTrue(s.Sleep());
            Assert.IsFalse(s.Status.Has(Ailment.Ansiedade));
        }
    }
}
