using System.Collections.Generic;
using System.Linq;
using ErvasDoBrasil.Core;
using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>Menu inicial, menu da partida (Esc) e lista de jogos salvos.</summary>
    public partial class GameUI
    {
        Window savesReturn = Window.Inicio;
        List<SaveData> saves = new List<SaveData>();
        string deleteArmed;   // jogo esperando a confirmação para apagar
        string loadArmed;     // no meio de uma partida, carregar outra também pede confirmação
        string lastDeleted, lastDeletedAt; // o último apagado pode voltar da lixeira ("Desfazer")

        /// <summary>Tela inicial (ou a lista de salvos aberta a partir dela): a partida ainda não começou.</summary>
        bool OnTitle
        {
            get
            {
                return Current == Window.Inicio || (Current == Window.Salvos && savesReturn == Window.Inicio) ||
                       (Current == Window.Bastidores && bastidoresReturn == Window.Inicio);
            }
        }

        public void OpenTitle()
        {
            RefreshSaves();
            Open(Window.Inicio);
        }

        void OpenSaves(Window from)
        {
            savesReturn = from;
            deleteArmed = loadArmed = lastDeleted = null;
            RefreshSaves();
            Open(Window.Salvos);
        }

        void RefreshSaves() { saves = SaveStore.List(); }

        string Resumo(SaveData d, bool comData = true)
        {
            var hora = new DayCycle(GameManager.DayLengthSeconds, d.dia, d.hora01).Clock;
            return "Dia " + d.dia + " · " + hora + " · nível " + Progression.NivelPara(d.xp) + " · " + d.moedas + " moedas · Livro " +
                   (d.herbario != null ? d.herbario.Count : 0) + "/" + S.Db.Plants.Count + (comData ? "\nSalvo em " + SaveStore.Pretty(d.salvoEm) : "");
        }

        public const string BotaoSair = "Sair do jogo";
        public const string BotaoSaindo = "Saindo...";

        // Sair em dois tempos: o clique escurece a tela com "Fechando o jogo..." e o Application.Quit() vem no Update
        // seguinte, depois que esse aviso foi desenhado. Antes o app ficava um instante parado, sem resposta, e parecia
        // que o primeiro clique não tinha pegado.
        bool saindo, saindoDesenhado, saiu;
        GUIStyle sSaindo;

        /// <summary>Testes: o jogo já mostra "Fechando o jogo..." (e fecha no frame seguinte).</summary>
        public bool Saindo { get { return saindo; } }

        /// <summary>No Update: fecha o app uma vez só, e só depois de um frame com o aviso na tela.</summary>
        void TickSair()
        {
            if (!saindo || !saindoDesenhado || saiu) return;
            saiu = true;
            GameManager.Sair();
        }

        void DrawSaindo(float vw)
        {
            if (!saindo) return;
            if (sSaindo == null) sSaindo = Label(30, Pergaminho, FontStyle.Bold, TextAnchor.MiddleCenter, false);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, vw, VirtualHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0f, VirtualHeight / 2f - 30f, vw, 60f), "Fechando o jogo...", sSaindo);
            if (Event.current.type == EventType.Repaint) saindoDesenhado = true;
        }

        void DrawInicio(Rect body)
        {
            GUILayout.BeginArea(body);
            GUILayout.BeginHorizontal();
            var portrait = ArtLibrary.Get("player_portrait");
            if (portrait != null)
            {
                var pr = GUILayoutUtility.GetRect(200, 260, GUILayout.Width(200), GUILayout.Height(260));
                SpriteFactory.DrawSprite(pr, portrait);
                GUILayout.Space(16);
            }
            GUILayout.BeginVertical();
            GUILayout.Label("Um RPG de ervas, chás e folclore brasileiro.", sSubtitle);
            GUILayout.Space(16);

            var last = saves.FirstOrDefault();
            if (last != null)
            {
                if (Button("Continuar", sButton, GUILayout.Height(52))) Defer(() => GameManager.I.LoadGame(last));
                GUILayout.Label(Resumo(last), sTextItalic);
                GUILayout.Space(10);
            }
            if (Button("Novo jogo", sButton, GUILayout.Height(52))) Defer(() => GameManager.I.NewGame());
            if (saves.Count > 0)
            {
                GUILayout.Space(10);
                if (Button("Jogos salvos (" + saves.Count + ")", sButton, GUILayout.Height(52))) OpenSaves(Window.Inicio);
            }
            GUILayout.Space(10);
            if (Button("Bastidores do desenvolvimento", sTab, GUILayout.Height(44))) OpenBastidores(Window.Inicio);
            if (!InputCompat.IsTouchDevice)
            {
                // no iPhone o app não se fecha sozinho: quem fecha é o sistema (a Apple recusa botão de sair)
                GUILayout.Space(10);
                bool old = GUI.enabled;
                GUI.enabled = !saindo; // o segundo clique não faz nada
                if (Button(saindo ? BotaoSaindo : BotaoSair, sTab, GUILayout.Height(44))) saindo = true;
                GUI.enabled = old;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(Aviso, sTextItalic);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawPausa(Rect body)
        {
            var gm = GameManager.I;
            GUILayout.BeginArea(body);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Min(440f, body.width)));
            if (Button("Voltar ao jogo", sButton, GUILayout.Height(52))) Close();
            GUILayout.Space(10);
            if (Button("Salvar jogo", sButton, GUILayout.Height(52))) Defer(() => gm.SaveNow());
            GUILayout.Label(gm.LastSavedAt == null ? "Esta partida ainda não foi salva." : "Salvo por último em " + SaveStore.Pretty(gm.LastSavedAt) + ".", sTextItalic);
            GUILayout.Space(10);
            if (Button("Jogos salvos", sButton, GUILayout.Height(52))) OpenSaves(Window.Pausa);
            GUILayout.Space(10);
            if (Button("Bastidores do desenvolvimento", sButton, GUILayout.Height(52))) OpenBastidores(Window.Pausa);
            GUILayout.Space(10);
            if (Button("Menu inicial", sButton, GUILayout.Height(52))) Defer(() => gm.BackToTitle());
            GUILayout.Space(14);
            GUILayout.Label("Dormir na cama da Casa da Vó também salva o jogo. O que não foi salvo se perde ao voltar ao menu inicial.", sTextItalic);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Lista de jogos salvos, do salvo por último para o mais antigo. Cada linha diz quando foi salva (é a ordem da lista
        /// e do Continuar); antes o título era a data em que a partida começou, e uma partida antiga jogada agora parecia velha.
        /// Apagar e, durante uma partida, carregar outra pedem confirmação; o apagado vai para a lixeira e dá para desfazer.
        /// </summary>
        void DrawSalvos(Rect body)
        {
            var gm = GameManager.I;
            bool emJogo = savesReturn == Window.Pausa;
            GUILayout.BeginArea(body);
            if (emJogo) GUILayout.Label("Carregar outra partida descarta o que não foi salvo nesta.", sWarn);
            if (lastDeleted != null)
            {
                GUILayout.BeginHorizontal(sCard);
                GUILayout.Label("Você apagou a partida salva em " + SaveStore.Pretty(lastDeletedAt) + ".", sText);
                GUILayout.FlexibleSpace();
                if (ListButton("Desfazer", true, 130f))
                {
                    var id = lastDeleted;
                    Defer(() =>
                    {
                        Toast(SaveStore.Undelete(id) ? "A partida voltou para a lista." : "Não foi possível desfazer.");
                        lastDeleted = null;
                        RefreshSaves();
                    });
                }
                GUILayout.EndHorizontal();
            }
            scroll = GUILayout.BeginScrollView(scroll);
            if (saves.Count == 0) GUILayout.Label("Nenhum jogo salvo ainda.", sText);
            // largura fixa: sem ela o título quebrava linha e o fim, "(é o Continuar)", sumia atrás da linha de baixo
            float textWidth = CardTextWidth(body, 300f);
            for (int i = 0; i < saves.Count; i++)
            {
                var d = saves[i];
                bool atual = emJogo && d.id == gm.SaveId;
                bool continuar = i == 0 && !emJogo; // é este que o botão Continuar do menu inicial carrega
                GUILayout.BeginHorizontal(sCard);
                GUILayout.BeginVertical(GUILayout.Width(textWidth));
                GUILayout.Label("Salvo em " + SaveStore.Pretty(d.salvoEm, true) + (atual ? "  (esta partida)" : continuar ? "  (é o Continuar)" : ""), sSection);
                GUILayout.Label(Resumo(d, false) + "\nComeçou em " + SaveStore.Pretty(d.criadaEm), sText);
                if (deleteArmed == d.id)
                    GUILayout.Label(continuar ? "Esta é a partida do Continuar. Apagar mesmo?" : "Apagar esta partida? Dá para desfazer logo depois.", sWarn);
                if (loadArmed == d.id) GUILayout.Label("O que não foi salvo na partida de agora se perde.", sWarn);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (deleteArmed == d.id)
                {
                    if (ListButton("Apagar mesmo", true, 150f))
                        Defer(() =>
                        {
                            if (SaveStore.Delete(d.id)) { lastDeleted = d.id; lastDeletedAt = d.salvoEm; }
                            deleteArmed = null;
                            RefreshSaves();
                        });
                    if (ListButton("Cancelar", true, 120f)) Defer(() => deleteArmed = null);
                }
                else if (loadArmed == d.id)
                {
                    if (ListButton("Carregar mesmo", true, 160f)) Defer(() => gm.LoadGame(d));
                    if (ListButton("Cancelar", true, 120f)) Defer(() => loadArmed = null);
                }
                else
                {
                    if (ListButton("Carregar", true, 120f))
                    {
                        if (emJogo) Defer(() => { loadArmed = d.id; deleteArmed = null; });
                        else Defer(() => gm.LoadGame(d));
                    }
                    if (ListButton("Apagar", !atual, 100f)) Defer(() => { deleteArmed = d.id; loadArmed = null; });
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
