using System.Collections.Generic;

namespace ErvasDoBrasil.Core
{
    public enum AvisoTipo { Comum, Nivel }

    /// <summary>Um aviso do histórico: quando aconteceu (dia e hora do jogo), o texto e quantas vezes veio seguido.</summary>
    public sealed class Aviso
    {
        public int Dia;
        public string Hora;
        public string Texto;
        public AvisoTipo Tipo;
        public int Vezes = 1;
    }

    /// <summary>
    /// Histórico dos avisos do jogo, os mesmos que aparecem no canto da tela e somem em 4 s: subir de nível e as plantas
    /// liberadas, colheitas, curas, vendas, assombrações... A jogadora abre pelo botão Histórico para lembrar o que aconteceu.
    /// Guarda só os últimos <see cref="Capacidade"/>; o mesmo aviso repetido em seguida vira um só, com a contagem.
    /// </summary>
    public sealed class MessageLog
    {
        public const int Capacidade = 60;
        /// <summary>Um aviso maior que isto é cortado (texto de save editado à mão não trava a janela).</summary>
        public const int MaxTexto = 300;

        readonly List<Aviso> itens = new List<Aviso>(); // do mais antigo para o mais novo

        public int Count { get { return itens.Count; } }

        /// <summary>Avisos que chegaram desde a última vez que o histórico foi aberto.</summary>
        public int NaoLidos { get; private set; }

        /// <summary>Do mais novo para o mais antigo, como a janela mostra.</summary>
        public IEnumerable<Aviso> DoMaisNovo
        {
            get
            {
                for (int i = itens.Count - 1; i >= 0; i--) yield return itens[i];
            }
        }

        /// <summary>Do mais antigo para o mais novo, como o save grava.</summary>
        public IReadOnlyList<Aviso> EmOrdem { get { return itens; } }

        public Aviso Ultimo { get { return itens.Count > 0 ? itens[itens.Count - 1] : null; } }

        public void Registrar(int dia, string hora, string texto, AvisoTipo tipo = AvisoTipo.Comum)
        {
            if (string.IsNullOrEmpty(texto)) return;
            texto = Corta(texto, MaxTexto);
            NaoLidos++;
            var ultimo = Ultimo;
            if (ultimo != null && ultimo.Texto == texto && ultimo.Tipo == tipo)
            {
                // crises de fissura seguidas, vários "+1 Boldo"...: uma linha só, com a hora da última vez
                ultimo.Vezes++;
                ultimo.Dia = dia;
                ultimo.Hora = hora;
                return;
            }
            itens.Add(new Aviso { Dia = dia, Hora = hora, Texto = texto, Tipo = tipo });
            if (itens.Count > Capacidade) itens.RemoveAt(0);
        }

        public void MarcarLidos() { NaoLidos = 0; }

        public void Limpar()
        {
            itens.Clear();
            NaoLidos = 0;
        }

        /// <summary>Volta um aviso gravado no save, sem contar como novo. Passa pelos mesmos limites (texto, contagem, capacidade).</summary>
        internal void Restore(int dia, string hora, string texto, AvisoTipo tipo, int vezes)
        {
            if (string.IsNullOrEmpty(texto)) return;
            itens.Add(new Aviso { Dia = dia, Hora = Corta(hora ?? "", 5), Texto = Corta(texto, MaxTexto), Tipo = tipo, Vezes = vezes < 1 ? 1 : vezes });
            if (itens.Count > Capacidade) itens.RemoveAt(0);
        }

        static string Corta(string s, int max) { return s.Length > max ? s.Substring(0, max) : s; }
    }
}
