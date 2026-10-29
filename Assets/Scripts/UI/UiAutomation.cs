using UnityEngine;

namespace ErvasDoBrasil
{
    /// <summary>
    /// Todos os botões da IMGUI passam por <see cref="Button(string, GUIStyle, GUILayoutOption[])"/>.
    /// Além do clique de verdade, eles aceitam um clique pedido pelos testes de interface (<see cref="Click"/>):
    /// o clique só acontece se um botão com esse texto for desenhado e estiver habilitado,
    /// então o teste prova que o botão está na tela, como uma jogadora veria.
    /// Os controles deslizantes (o volume) passam por <see cref="Slider"/> e aceitam um arraste pedido pelos testes (<see cref="Arrastar"/>).
    /// </summary>
    public partial class GameUI
    {
        string pendingClick;
        int pendingIndex;           // -1: o primeiro habilitado; 0, 1...: o 1º, 2º... botão com esse texto na ordem da tela
        int seenFrame = -1, seen;   // quantos botões com esse texto já foram desenhados neste frame
        string pendingSlider;
        float pendingSliderValue;

        /// <summary>
        /// Testes: clica no botão com este texto no próximo frame desenhado.
        /// Sem <paramref name="posicao"/>, no primeiro habilitado; com ela, no daquela posição na tela
        /// (0 = o de cima, 1 = o da linha seguinte...), mesmo que ele esteja desabilitado, e aí o clique não acontece.
        /// </summary>
        public void Click(string label, int posicao = -1)
        {
            pendingClick = label;
            pendingIndex = posicao;
        }

        /// <summary>Testes: o clique pedido ainda não encontrou o botão na tela.</summary>
        public bool ClickPending { get { return pendingClick != null; } }

        /// <summary>Testes: no próximo frame desenhado, puxa o pino do controle deslizante com este nome até o valor.</summary>
        public void Arrastar(string nome, float valor)
        {
            pendingSlider = nome;
            pendingSliderValue = valor;
        }

        /// <summary>Testes: o arraste pedido ainda não encontrou o controle na tela.</summary>
        public bool DragPending { get { return pendingSlider != null; } }

        /// <summary>Testes: desiste de um clique (ou arraste) que não encontrou o controle.</summary>
        public void CancelClick()
        {
            pendingClick = null;
            pendingSlider = null;
        }

        /// <summary>Controle deslizante. Como no clique, o arraste de um teste só vale no Repaint, com o controle desenhado e habilitado.</summary>
        float Slider(Rect r, float value, float min, float max, GUIStyle slider, GUIStyle thumb, string nome)
        {
            float v = GUI.HorizontalSlider(r, value, min, max, slider, thumb);
            if (pendingSlider != nome || Event.current.type != EventType.Repaint || !GUI.enabled) return v;
            pendingSlider = null;
            return Mathf.Clamp(pendingSliderValue, min, max);
        }

        bool Button(string text, GUIStyle style, params GUILayoutOption[] options) { return Pressed(GUILayout.Button(text, style, options), text); }

        bool Button(Rect r, string text, GUIStyle style) { return Pressed(GUI.Button(r, text, style), text); }

        /// <summary>O clique de teste vale no Repaint, depois do Layout, no mesmo ponto do frame em que chega um clique de verdade.</summary>
        bool Pressed(bool clicked, string label)
        {
            if (clicked) return true;
            if (pendingClick != label || Event.current.type != EventType.Repaint) return false;
            if (seenFrame != Time.frameCount)
            {
                seenFrame = Time.frameCount;
                seen = 0;
            }
            int posicao = seen++;
            if (!GUI.enabled || (pendingIndex >= 0 && posicao != pendingIndex)) return false;
            pendingClick = null;
            // como o GUI.Button faz no clique de verdade: o evento vira "usado", e o resto deste OnGUI
            // não cobra o layout calculado antes do clique (a janela pode ter mudado)
            Event.current.type = EventType.Used;
            return true;
        }
    }
}
