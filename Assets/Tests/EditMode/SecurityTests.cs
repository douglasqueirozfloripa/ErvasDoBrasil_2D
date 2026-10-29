using System.IO;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;

namespace ErvasDoBrasil.Tests
{
    /// <summary>Segurança (docs/15-seguranca.md): um save vindo de fora não quebra o jogo, e o projeto continua sem rede.</summary>
    public class SecurityTests
    {
        static GameSession Carregar(SaveData d)
        {
            var s = new GameSession(TestData.Db(), new FixedRandom(), 100f, 4);
            SaveGame.Restore(s, d);
            return s;
        }

        static bool Finito(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }

        // ───────── Save adulterado ─────────

        [Test]
        public void Save_com_NaN_e_infinito_carrega_so_numeros_finitos()
        {
            var d = new SaveData { hora01 = float.NaN };
            d.status.energia = float.NaN;
            d.status.sanidade = float.PositiveInfinity;
            d.status.aura = float.NegativeInfinity;
            d.status.velocidadePct = float.NaN;
            d.status.velocidadeSegundos = float.PositiveInfinity;
            d.status.revelarSegundos = float.NaN;
            d.varal.Add(new SaveVaral { planta = "boldo", segundos = float.NaN });
            d.canteiros.Add(new SaveCanteiro { planta = "boldo", crescimento = float.NaN, preparado = true });
            d.canteiros.Add(new SaveCanteiro { planta = "boldo", crescimento = 0.5f, cavado = true, x = float.NaN, y = float.PositiveInfinity });

            var s = Carregar(d);

            Assert.IsTrue(Finito(s.Clock.Time01));
            Assert.IsTrue(Finito(s.Status.Energy) && Finito(s.Status.Sanidade) && Finito(s.Status.Aura));
            Assert.IsTrue(Finito(s.Status.BuffVelocidadePct) && Finito(s.Status.BuffVelocidadeSegundos) && Finito(s.Status.RevelarSegundos));
            Assert.IsTrue(s.Rack.Slots.All(v => Finito(v.Elapsed)));
            Assert.IsTrue(s.Plots.All(p => Finito(p.Growth) && Finito(p.X) && Finito(p.Y)));
        }

        [Test]
        public void Save_com_numeros_gigantes_nao_estoura_na_proxima_soma()
        {
            string boldo = Inventory.Herb("boldo");
            var d = new SaveData { dia = int.MaxValue, moedas = int.MaxValue, reputacao = int.MaxValue, xp = int.MaxValue };
            d.status.amuletoDias = int.MaxValue;
            d.status.velocidadePct = 1000f;
            d.status.velocidadeSegundos = 60f;
            d.bolsa.Add(new SaveItem { chave = boldo, qtd = int.MaxValue });
            d.bolsa.Add(new SaveItem { chave = boldo, qtd = int.MaxValue });
            d.pedidos.Add(new SavePedido { nome = "Dona Cida", mal = Ailment.Gripe.ToString(), recompensa = int.MaxValue });

            var s = Carregar(d);
            Assert.IsTrue(s.Sell(boldo, 10), "vender soma moedas");
            s.Inventory.Add(boldo, 1000);
            s.Progresso.Add(1000);

            Assert.That(s.Money, Is.GreaterThan(SaveGame.MaxContador), "somou sem virar negativo");
            Assert.That(s.Inventory.Count(boldo), Is.GreaterThan(0));
            Assert.That(s.Progresso.Xp, Is.GreaterThan(0));
            Assert.AreEqual(SaveGame.MaxContador, s.Clock.Day);
            Assert.AreEqual(SaveGame.MaxContador, s.Reputacao);
            Assert.AreEqual(SaveGame.MaxContador, s.Status.AmuletoDias);
            Assert.AreEqual(SaveGame.MaxContador, s.Requests[0].Recompensa);
            Assert.That(s.Status.BuffVelocidadePct, Is.LessThanOrEqualTo(2f), "no máximo +200% de velocidade");
        }

        [Test]
        public void Save_com_listas_enormes_para_no_limite()
        {
            var d = new SaveData();
            for (int i = 0; i < 5000; i++)
            {
                d.bolsa.Add(new SaveItem { chave = Inventory.Herb("planta" + i), qtd = 1 });
                d.canteiros.Add(new SaveCanteiro { cavado = true, x = i * FarmLayout.Largura, y = 0f });
                d.pedidos.Add(new SavePedido { nome = "Aldeão", mal = Ailment.Gripe.ToString(), recompensa = 1 });
            }

            var s = Carregar(d);

            Assert.AreEqual(SaveGame.MaxEntradas, s.Inventory.All.Count());
            Assert.AreEqual(GameSession.MaxCanteirosCavados, s.CanteirosCavados, "o mesmo limite da pá");
            Assert.AreEqual(SaveGame.MaxEntradas, s.Requests.Count);
        }

        [Test]
        public void Nome_enorme_num_pedido_e_cortado()
        {
            var d = new SaveData();
            d.pedidos.Add(new SavePedido { nome = new string('a', 100000), mal = Ailment.Gripe.ToString(), recompensa = 5 });

            Assert.AreEqual(SaveGame.MaxNome, Carregar(d).Requests[0].Nome.Length);
        }

        [TestCase("partida_20261029_101500", true)]
        [TestCase("minha partida", true)]
        [TestCase("../../Documents/x", false)]
        [TestCase("..", false)]
        [TestCase(".", false)]
        [TestCase("a/b", false)]
        [TestCase("a\\b", false)]
        [TestCase("C:x", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Id_de_save_nunca_sai_da_pasta_dos_saves(string id, bool valido)
        {
            Assert.AreEqual(valido, SaveGame.IdValido(id));
        }

        [Test]
        public void Id_de_save_comprido_demais_e_recusado()
        {
            Assert.IsFalse(SaveGame.IdValido(new string('a', 101)));
        }

        // ───────── Projeto sem rede ─────────

        [Test]
        public void Regra_acha_pacote_de_rede_mesmo_como_dependencia()
        {
            const string lockJson = "{ \"dependencies\": { \"com.unity.test-framework\": { \"version\": \"1.6.0\", \"dependencies\": " +
                                    "{ \"com.unity.modules.unitywebrequest\": \"1.0.0\" } }, \"com.unity.services.analytics\": { \"version\": \"6.0.0\" } } }";

            CollectionAssert.AreEquivalent(new[] { "com.unity.modules.unitywebrequest", "com.unity.services.analytics" },
                SecurityRules.ForbiddenPackages(lockJson));
        }

        [TestCase("Packages/manifest.json")]
        [TestCase("Packages/packages-lock.json")]
        public void Projeto_nao_tem_pacote_de_rede(string file)
        {
            CollectionAssert.IsEmpty(SecurityRules.ForbiddenPackages(File.ReadAllText(file)));
        }

        [Test]
        public void Regra_reconhece_o_app_que_abre_a_porta_do_Profiler()
        {
            Assert.IsTrue(SecurityRules.OpensPlayerConnection("player-connection-mode=Listen\nplayer-connection-ip=192.168.1.13\ngfx-threading-mode=4\n"));
            Assert.IsFalse(SecurityRules.OpensPlayerConnection("gfx-threading-mode=4\nwait-for-native-debugger=0\n"));
        }

        [Test]
        public void Player_Settings_nao_liberam_HTTP_nem_servicos_da_Unity()
        {
            string player = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            StringAssert.Contains("insecureHttpOption: 0", player);
            StringAssert.Contains("enableCrashReportAPI: 0", player);

            // Analytics, Ads, IAP, Insights e Performance Reporting têm cada um o seu m_Enabled
            string connect = File.ReadAllText("ProjectSettings/UnityConnectSettings.asset");
            StringAssert.DoesNotContain("m_Enabled: 1", connect);
            StringAssert.Contains("m_EnableCloudDiagnosticsReporting: 0", connect);
        }
    }
}
