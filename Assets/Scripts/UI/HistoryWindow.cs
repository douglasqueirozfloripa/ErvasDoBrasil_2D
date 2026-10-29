using System.Globalization;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Histórico: os últimos avisos da partida (os do canto da tela, que somem em 4 s), do mais novo para o mais antigo,
    /// para a jogadora lembrar o que aconteceu. Subir de nível, com as plantas liberadas, aparece em destaque dourado.
    /// Abre pelo botão ao lado da Bolsa ou pela tecla H; o selo no botão conta os avisos novos desde a última vez.
    /// </summary>
    public partial class GameUI
    {
        public const string BotaoHistorico = "Histórico (H)";

        const float HoraWidth = 110f;

        GUIStyle sCardNivel, sTextNivel, sHora, sBadge, sRoundSmall;

        void DrawHistorico(Rect body)
        {
            InitHistoricoStyles();
            var avisos = S.Avisos;
            avisos.MarcarLidos(); // com a janela aberta, o que chega já está sendo visto
            GUILayout.BeginArea(body);
            GUILayout.Label(avisos.Count == 0
                ? "Ainda não aconteceu nada nesta partida. Os avisos que aparecem no canto da tela ficam guardados aqui."
                : "Os últimos avisos da partida, do mais novo para o mais antigo (ficam os " + MessageLog.Capacidade + " últimos).", sTextItalic);
            GUILayout.Space(6);
            scroll = GUILayout.BeginScrollView(scroll);
            // largura fixa com folga para a barra de rolagem: sem ela aparece a barra horizontal
            float textWidth = CardTextWidth(body, HoraWidth + 10f);
            foreach (var a in avisos.DoMaisNovo)
            {
                bool nivel = a.Tipo == AvisoTipo.Nivel;
                GUILayout.BeginHorizontal(nivel ? sCardNivel : sCard);
                GUILayout.Label("Dia " + a.Dia + " · " + a.Hora, sHora, GUILayout.Width(HoraWidth));
                GUILayout.Label(a.Texto + (a.Vezes > 1 ? "  (" + a.Vezes + " vezes)" : ""), nivel ? sTextNivel : sText, GUILayout.Width(textWidth));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Selo vermelho no canto do botão: quantos avisos chegaram desde a última vez que o Histórico foi aberto.</summary>
        void DrawBadgeNaoLidos(Vector2 c)
        {
            int n = S.Avisos.NaoLidos;
            if (n <= 0) return;
            InitHistoricoStyles();
            Circle(c, 12f, new Color(0.72f, 0.18f, 0.12f));
            GUI.Label(new Rect(c.x - 12f, c.y - 12f, 24f, 24f), n > 9 ? "9+" : n.ToString(CultureInfo.InvariantCulture), sBadge);
        }

        void InitHistoricoStyles()
        {
            if (sCardNivel != null) return;
            sCardNivel = RoundedBox(new Color(1f, 0.93f, 0.7f), 10, Ouro, 2);
            sCardNivel.padding = new RectOffset(12, 12, 8, 8);
            sCardNivel.margin = new RectOffset(0, 0, 0, 8);
            sTextNivel = Label(17, Tinta, FontStyle.Bold);
            sHora = Label(14, new Color(0.35f, 0.25f, 0.18f), FontStyle.Italic, TextAnchor.UpperLeft, false);
            sHora.padding.top += 2;
            sBadge = Label(12, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, false);
            // "Histórico" não cabe na letra dos outros botões redondos do toque
            sRoundSmall = Label(12, Tinta, FontStyle.Bold, TextAnchor.MiddleCenter, false);
        }
    }
}
