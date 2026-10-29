using System.Collections;
using ErvasDoBrasil.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>A festa de subir de nível: balões, fogos e o número trocando como uma planta que brota.</summary>
    public class NivelTests : JogadorSimulado
    {
        static readonly float[] Momentos = { 0.3f, 0.85f, 1.7f, 2.7f, 4.2f };
        static readonly string[] Nomes = { "faixa-com-o-numero-antigo", "numero-antigo-murchando", "numero-novo-brotando", "florada-e-fogos", "baloes-subindo" };

        [UnityTest]
        public IEnumerator Subir_de_nivel_faz_festa_e_o_numero_novo_brota_no_lugar_do_antigo()
        {
            yield return ComecarPartida();
            Assert.AreEqual(1, Sessao.Progresso.Nivel);
            Assert.IsFalse(Tela.CelebrandoNivel);

            float inicio = Time.unscaledTime;
            Sessao.Progresso.Add(Progression.XpPorNivel[1]);
            Assert.AreEqual(2, Sessao.Progresso.Nivel);
            Assert.IsTrue(Tela.CelebrandoNivel, "subir de nível não começou a comemoração");

            for (int i = 0; i < Momentos.Length; i++)
            {
                while (Time.unscaledTime - inicio < Momentos[i]) yield return null;
                yield return Foto(Nomes[i]);
            }

            yield return Esperar(() => !Tela.CelebrandoNivel, "a comemoração não terminou");
            Assert.IsFalse(Tela.IsModal, "a comemoração não abre janela nem prende a jogadora");
        }
    }
}
