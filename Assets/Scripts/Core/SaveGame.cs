using System;
using System.Collections.Generic;
using System.Linq;

namespace ErvasDoBrasil.Core
{
    [Serializable] public class SaveItem { public string chave; public int qtd; }
    [Serializable] public class SaveMal { public string mal; public int doses; }
    [Serializable] public class SaveVaral { public string planta; public float segundos; }
    /// <summary>Canteiro salvo. x, y e cavado são da versão 2 (a pá); nos saves antigos vêm zerados e valem para a horta da vó.</summary>
    [Serializable]
    public class SaveCanteiro
    {
        public string planta;
        public float crescimento;
        public bool preparado;
        public bool regado;
        public float x;
        public float y;
        public bool cavado;
    }
    [Serializable] public class SavePedido { public string nome; public string mal; public int recompensa; public bool atendido; }
    /// <summary>Um aviso do histórico (botão Histórico). Os saves antigos não têm: a partida abre com o histórico vazio.</summary>
    [Serializable] public class SaveAviso { public int dia; public string hora; public string texto; public bool nivel; public int vezes; }

    [Serializable]
    public class SaveStatus
    {
        public float energia;
        public float sanidade;
        public float aura;
        public List<SaveMal> males = new List<SaveMal>();
        public int amuletoDias;
        public int sorteDias;
        public float velocidadePct;
        public float velocidadeSegundos;
        public float revelarSegundos;
        public float fissuraSegundos;
        /// <summary>Salvo antes do primeiro plantio. Nos saves antigos vem false: as crises correm como antes.</summary>
        public bool fissuraEmEspera;
    }

    /// <summary>Uma partida salva. Campos públicos em minúsculo para o JsonUtility, como os dados do jogo.</summary>
    [Serializable]
    public class SaveData
    {
        public int versao;

        // preenchidos pela Unity: arquivo, datas e mundo
        public string id;
        public string criadaEm;
        public string salvoEm;
        public float jogadoraX;
        public float jogadoraY;
        public bool vitoriaVista;

        // regras (GameSession)
        public int dia;
        public float hora01;
        public int moedas;
        public int reputacao;
        public int xp;
        public bool pa;
        public SaveStatus status = new SaveStatus();
        public List<SaveItem> bolsa = new List<SaveItem>();
        public List<string> herbario = new List<string>();
        public List<SaveVaral> varal = new List<SaveVaral>();
        public List<SaveCanteiro> canteiros = new List<SaveCanteiro>();
        public List<SavePedido> pedidos = new List<SavePedido>();
        /// <summary>Dia em que chegaram os pedidos (valem uma semana). Nos saves antigos vem 0: a semana conta do dia carregado.</summary>
        public int pedidosDesde;
        public List<SaveAviso> avisos = new List<SaveAviso>();
    }

    /// <summary>Converte a GameSession em SaveData e de volta. Ler e gravar o arquivo fica com a Unity (SaveStore).</summary>
    public static class SaveGame
    {
        public const int Versao = 2;

        // ───────── Limites de um save: o arquivo pode ter sido editado à mão ou baixado da internet (docs/15-seguranca.md) ─────────

        /// <summary>Tamanho máximo do arquivo. Um save de verdade tem poucos KB; um arquivo gigante travaria o menu de jogos salvos.</summary>
        public const long MaxBytes = 1024 * 1024;
        /// <summary>Teto de moedas, xp, dia...: com int.MaxValue a primeira soma estouraria e o valor viraria negativo.</summary>
        public const int MaxContador = 1000000000;
        /// <summary>Teto de cada item da bolsa, pelo mesmo motivo.</summary>
        public const int MaxQtdItem = 1000000;
        /// <summary>Teto de cada lista do save (bolsa, pedidos, varal...).</summary>
        public const int MaxEntradas = 500;
        /// <summary>Nome do aldeão num pedido: um texto enorme travaria a janela que o desenha.</summary>
        public const int MaxNome = 40;
        /// <summary>Bônus de velocidade: o maior dos dados é +50%.</summary>
        const float MaxBuffVelocidade = 2f;
        /// <summary>Posição de canteiro cavado: o mapa tem poucas dezenas de unidades.</summary>
        const float MaxCoordenada = 1000f;

        /// <summary>Id de save = nome do arquivo sem o .json. Sem barra nem "..": o arquivo nunca sai da pasta dos saves.</summary>
        public static bool IdValido(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 100 || id == "." || id == "..") return false;
            foreach (char c in id)
                if (c == '/' || c == '\\' || c == ':' || char.IsControl(c)) return false;
            return true;
        }

        public static SaveData Capture(GameSession s)
        {
            var d = new SaveData
            {
                versao = Versao,
                dia = s.Clock.Day,
                hora01 = s.Clock.Time01,
                moedas = s.Money,
                reputacao = s.Reputacao,
                xp = s.Progresso.Xp,
                pa = s.TemPa,
                pedidosDesde = s.PedidosDesde
            };

            var st = s.Status;
            d.status.energia = st.Energy;
            d.status.sanidade = st.Sanidade;
            d.status.aura = st.Aura;
            d.status.amuletoDias = st.AmuletoDias;
            d.status.sorteDias = st.SorteDias;
            d.status.velocidadePct = st.BuffVelocidadePct;
            d.status.velocidadeSegundos = st.BuffVelocidadeSegundos;
            d.status.revelarSegundos = st.RevelarSegundos;
            d.status.fissuraSegundos = st.SmokeTimer;
            d.status.fissuraEmEspera = st.FissuraEmEspera;
            foreach (var a in st.ActiveAilments) d.status.males.Add(new SaveMal { mal = a.Tipo.ToString(), doses = a.DosesRestantes });

            foreach (var kv in s.Inventory.All) d.bolsa.Add(new SaveItem { chave = kv.Key, qtd = kv.Value });
            d.herbario.AddRange(s.Herbarium.All);
            foreach (var slot in s.Rack.Slots) d.varal.Add(new SaveVaral { planta = slot.PlantId, segundos = slot.Elapsed });
            foreach (var p in s.Plots)
                d.canteiros.Add(new SaveCanteiro { planta = p.PlantId, crescimento = p.Growth, preparado = p.Prepared, regado = p.Watered, x = p.X, y = p.Y, cavado = p.Cavado });
            foreach (var r in s.Requests) d.pedidos.Add(new SavePedido { nome = r.Nome, mal = r.Mal.ToString(), recompensa = r.Recompensa, atendido = r.Atendido });
            foreach (var a in s.Avisos.EmOrdem) d.avisos.Add(new SaveAviso { dia = a.Dia, hora = a.Hora, texto = a.Texto, nivel = a.Tipo == AvisoTipo.Nivel, vezes = a.Vezes });
            return d;
        }

        /// <summary>
        /// Carrega a partida numa sessão recém-criada (sem StartNewGame).
        /// Plantas, receitas e males que não existem mais nos dados são ignorados.
        /// Números e listas passam pelos limites acima: um save adulterado não trava nem quebra o jogo.
        /// </summary>
        public static void Restore(GameSession s, SaveData d)
        {
            if (s == null || d == null) return;
            s.Clock.Restore(Math.Min(d.dia, MaxContador), Finito(d.hora01));
            s.Money = Contador(d.moedas);
            s.Reputacao = Contador(d.reputacao);
            s.Progresso.Restore(Contador(d.xp));
            s.TemPa = d.pa;

            var st = d.status ?? new SaveStatus();
            foreach (var m in Each(st.males))
            {
                Ailment a;
                if (m != null && Enum.TryParse(m.mal, out a)) s.Status.RestoreAilment(a, m.doses);
            }
            s.Status.Restore(Finito(st.energia), Finito(st.sanidade), Finito(st.aura), Finito(st.fissuraSegundos));
            s.Status.AmuletoDias = Contador(st.amuletoDias);
            s.Status.SorteDias = Contador(st.sorteDias);
            s.Status.BuffVelocidadePct = Math.Max(0f, Math.Min(Finito(st.velocidadePct), MaxBuffVelocidade));
            s.Status.BuffVelocidadeSegundos = Math.Max(0f, Finito(st.velocidadeSegundos));
            s.Status.RevelarSegundos = Math.Max(0f, Finito(st.revelarSegundos));
            s.Status.FissuraEmEspera = st.fissuraEmEspera;

            foreach (var it in Each(d.bolsa))
                if (it != null && it.qtd > 0 && Inventory.TryParseKey(it.chave, out _, out _)) s.Inventory.Add(it.chave, Math.Min(it.qtd, MaxQtdItem));
            foreach (var id in Each(d.herbario))
                if (s.Db.Plant(id) != null) s.Herbarium.Discover(id);
            foreach (var v in Each(d.varal))
                if (v != null && s.Db.Plant(v.planta) != null) s.Rack.Restore(v.planta, Finito(v.segundos));

            // a horta da vó vem primeiro, na ordem; os cavados com a pá voltam para o lugar deles
            int horta = s.Plots.Count, i = 0;
            foreach (var c in Each(d.canteiros))
            {
                if (c != null && c.cavado)
                {
                    if (s.CanteirosCavados >= GameSession.MaxCanteirosCavados) continue; // o mesmo limite da pá
                    float x = Coordenada(c.x), y = Coordenada(c.y);
                    if (s.PlotAt(x, y) != null) continue; // save editado à mão: dois canteiros na mesma casa
                    var cavado = new FarmPlotModel(FarmLayout.SnapX(x), FarmLayout.SnapY(y), true);
                    cavado.Restore(s.Db.Plant(c.planta) != null ? c.planta : null, Finito(c.crescimento), true, c.regado);
                    s.AddPlot(cavado);
                    continue;
                }
                int k = i++;
                if (c == null || k >= horta) continue;
                // o JsonUtility grava string nula como "": canteiro vazio
                s.Plots[k].Restore(s.Db.Plant(c.planta) != null ? c.planta : null, Finito(c.crescimento), c.preparado, c.regado);
            }

            s.Requests.Clear();
            foreach (var r in Each(d.pedidos))
            {
                Ailment a;
                if (r != null && Enum.TryParse(r.mal, out a))
                    s.Requests.Add(new VillagerRequest { Nome = Corta(r.nome), Mal = a, Recompensa = Contador(r.recompensa), Atendido = r.atendido });
            }
            // saves antigos (0) contam a semana a partir de hoje; um dia no futuro (save editado) também volta para hoje
            s.PedidosDesde = d.pedidosDesde > 0 ? Math.Min(d.pedidosDesde, s.Clock.Day) : s.Clock.Day;

            // o histórico guarda só os últimos MessageLog.Capacidade: de uma lista enorme, ficam os do fim (os mais novos)
            s.Avisos.Limpar();
            var avisos = (d.avisos ?? new List<SaveAviso>()).Where(a => a != null && !string.IsNullOrEmpty(a.texto)).ToList();
            foreach (var a in avisos.Skip(Math.Max(0, avisos.Count - MessageLog.Capacidade)))
                s.Avisos.Restore(Contador(a.dia), a.hora, a.texto, a.nivel ? AvisoTipo.Nivel : AvisoTipo.Comum, Contador(a.vezes));
        }

        /// <summary>Lista do save, no máximo MaxEntradas itens.</summary>
        static IEnumerable<T> Each<T>(List<T> list) { return list == null ? Enumerable.Empty<T>() : list.Take(MaxEntradas); }
        /// <summary>NaN e infinito (save corrompido) viram 0.</summary>
        static float Finito(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? 0f : v; }
        static int Contador(int v) { return Math.Max(0, Math.Min(v, MaxContador)); }
        static float Coordenada(float v) { return Math.Max(-MaxCoordenada, Math.Min(Finito(v), MaxCoordenada)); }
        static string Corta(string s) { return s != null && s.Length > MaxNome ? s.Substring(0, MaxNome) : s; }
    }
}
