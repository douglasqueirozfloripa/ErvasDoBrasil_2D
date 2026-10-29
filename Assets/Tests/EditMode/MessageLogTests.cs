using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;

namespace ErvasDoBrasil.Tests
{
    /// <summary>Histórico dos avisos (botão Histórico): ordem, repetidos, limite, não lidos, subir de nível e o save.</summary>
    public class MessageLogTests
    {
        static GameSession NovaSessao() { return new GameSession(TestData.Db(), new FixedRandom(), 100f, 4); }

        static string[] Textos(MessageLog log) { return log.DoMaisNovo.Select(a => a.Texto).ToArray(); }

        [Test]
        public void Guarda_do_mais_novo_para_o_mais_antigo_com_dia_e_hora()
        {
            var log = new MessageLog();
            log.Registrar(1, "06:10", "Você colheu boldo.");
            log.Registrar(2, "09:30", "Jogo salvo.");
            CollectionAssert.AreEqual(new[] { "Jogo salvo.", "Você colheu boldo." }, Textos(log));
            var novo = log.DoMaisNovo.First();
            Assert.AreEqual(2, novo.Dia);
            Assert.AreEqual("09:30", novo.Hora);
        }

        [Test]
        public void Aviso_repetido_em_seguida_vira_uma_linha_com_a_contagem()
        {
            var log = new MessageLog();
            log.Registrar(1, "07:00", "Crise de fissura!");
            log.Registrar(1, "08:00", "Crise de fissura!");
            log.Registrar(1, "09:00", "Crise de fissura!");
            Assert.AreEqual(1, log.Count, "uma linha só");
            Assert.AreEqual(3, log.Ultimo.Vezes);
            Assert.AreEqual("09:00", log.Ultimo.Hora, "com a hora da última vez");
            Assert.AreEqual(3, log.NaoLidos, "cada repetição é um aviso novo para o selo do botão");

            log.Registrar(1, "10:00", "+1 Boldo");
            log.Registrar(1, "11:00", "Crise de fissura!");
            Assert.AreEqual(3, log.Count, "repetido só junta quando vem logo em seguida");
        }

        [Test]
        public void Guarda_so_os_ultimos_avisos()
        {
            var log = new MessageLog();
            for (int i = 0; i < MessageLog.Capacidade + 10; i++) log.Registrar(1, "06:00", "aviso " + i);
            Assert.AreEqual(MessageLog.Capacidade, log.Count);
            Assert.AreEqual("aviso 10", log.EmOrdem[0].Texto, "os mais antigos saem primeiro");
            Assert.AreEqual("aviso " + (MessageLog.Capacidade + 9), log.Ultimo.Texto);
        }

        [Test]
        public void Abrir_o_historico_zera_os_nao_lidos()
        {
            var log = new MessageLog();
            log.Registrar(1, "06:00", "a");
            log.Registrar(1, "06:00", "b");
            Assert.AreEqual(2, log.NaoLidos);
            log.MarcarLidos();
            Assert.AreEqual(0, log.NaoLidos);
            Assert.AreEqual(2, log.Count, "marcar como lido não apaga nada");
        }

        [Test]
        public void Texto_vazio_nao_entra_e_texto_enorme_e_cortado()
        {
            var log = new MessageLog();
            log.Registrar(1, "06:00", "");
            log.Registrar(1, "06:00", null);
            Assert.AreEqual(0, log.Count);
            log.Registrar(1, "06:00", new string('a', 5000));
            Assert.AreEqual(MessageLog.MaxTexto, log.Ultimo.Texto.Length);
        }

        [Test]
        public void Subir_de_nivel_entra_no_historico_em_destaque_com_as_plantas_liberadas()
        {
            var s = NovaSessao();
            s.StartNewGame();
            s.Progresso.Add(Progression.XpPorNivel[1]);
            var aviso = s.Avisos.EmOrdem.LastOrDefault(a => a.Tipo == AvisoTipo.Nivel);
            Assert.IsNotNull(aviso, "a subida de nível fica no histórico, marcada como nível");
            StringAssert.StartsWith("Nível 2!", aviso.Texto);
            foreach (var p in s.Db.Plants.Where(p => p.NivelMinimo == 2))
                StringAssert.Contains(p.nome, aviso.Texto, "a dica das plantas novas também fica");
            Assert.AreEqual(s.Clock.Day, aviso.Dia);
            Assert.AreEqual(s.Clock.Clock, aviso.Hora);
        }

        [Test]
        public void Avisar_registra_no_historico_e_vai_para_o_canto_da_tela()
        {
            var s = NovaSessao();
            var tela = new List<string>();
            s.Message += tela.Add;
            s.Avisar("Jogo salvo.");
            CollectionAssert.AreEqual(new[] { "Jogo salvo." }, tela, "o aviso aparece no canto da tela");
            Assert.AreEqual("Jogo salvo.", s.Avisos.Ultimo.Texto, "e fica no histórico");
            Assert.AreEqual(AvisoTipo.Comum, s.Avisos.Ultimo.Tipo);
        }

        [Test]
        public void Novo_jogo_comeca_com_o_historico_limpo()
        {
            var s = NovaSessao();
            s.Avisar("A partida voltou para a lista."); // aviso do menu inicial, antes de começar
            s.StartNewGame();
            Assert.IsFalse(s.Avisos.EmOrdem.Any(a => a.Texto == "A partida voltou para a lista."));
        }

        [Test]
        public void Historico_volta_com_o_jogo_salvo_sem_contar_como_novo()
        {
            var s = NovaSessao();
            s.StartNewGame();
            s.Avisar("+1 Boldo");
            s.Avisar("+1 Boldo");
            s.Progresso.Add(Progression.XpPorNivel[1]);
            var json = JsonUtility.ToJson(SaveGame.Capture(s));

            var carregada = NovaSessao();
            SaveGame.Restore(carregada, JsonUtility.FromJson<SaveData>(json));
            CollectionAssert.AreEqual(Textos(s.Avisos), Textos(carregada.Avisos), "os mesmos avisos, na mesma ordem");
            var boldo = carregada.Avisos.EmOrdem.First(a => a.Texto == "+1 Boldo");
            Assert.AreEqual(2, boldo.Vezes, "a contagem de repetidos volta");
            Assert.IsTrue(carregada.Avisos.EmOrdem.Any(a => a.Tipo == AvisoTipo.Nivel), "o destaque do nível volta");
            Assert.AreEqual(0, carregada.Avisos.NaoLidos, "o que já estava no save não aparece como novo no selo");
        }

        [Test]
        public void Save_antigo_sem_avisos_abre_com_o_historico_vazio()
        {
            var s = NovaSessao();
            var d = SaveGame.Capture(NovaSessao());
            d.avisos = null; // save de antes do Histórico
            SaveGame.Restore(s, d);
            Assert.AreEqual(0, s.Avisos.Count);
        }

        [Test]
        public void Save_adulterado_nao_estoura_o_historico()
        {
            var d = SaveGame.Capture(NovaSessao());
            d.avisos = new List<SaveAviso>();
            for (int i = 0; i < 1000; i++) d.avisos.Add(new SaveAviso { dia = 1, hora = "06:00", texto = "aviso " + i, vezes = 1 });
            d.avisos.Add(null);
            d.avisos.Add(new SaveAviso { dia = -5, hora = new string('9', 100), texto = new string('x', 10000), vezes = -3 });

            var s = NovaSessao();
            SaveGame.Restore(s, d);
            Assert.AreEqual(MessageLog.Capacidade, s.Avisos.Count, "de uma lista enorme ficam só os últimos");
            var ultimo = s.Avisos.Ultimo;
            Assert.AreEqual(MessageLog.MaxTexto, ultimo.Texto.Length, "texto enorme é cortado");
            Assert.LessOrEqual(ultimo.Hora.Length, 5);
            Assert.AreEqual(1, ultimo.Vezes, "contagem negativa vira 1");
            Assert.AreEqual(0, ultimo.Dia, "dia negativo vira 0");
        }
    }
}
