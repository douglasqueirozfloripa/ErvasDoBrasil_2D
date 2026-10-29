using UnityEngine;

namespace ErvasDoBrasil
{
    public enum GameKey { Interagir, Bolsa, Livro, Fechar, Som, Historico }

    /// <summary>
    /// Entrada unificada: teclado (macOS) + joystick/botões virtuais (iPhone).
    /// O projeto usa o Input Manager clássico (Project Settings ▸ Player ▸ Active Input Handling = Input Manager ou Both).
    /// </summary>
    public static class InputCompat
    {
        /// <summary>Preenchido a cada frame pelos controles de toque.</summary>
        public static Vector2 VirtualStick { get; set; }
        static int virtualPressedFrame = -1;
        static GameKey virtualKey;

        public static void PressVirtual(GameKey k)
        {
            virtualKey = k;
            virtualPressedFrame = Time.frameCount;
        }

        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
#endif
            if (VirtualStick.sqrMagnitude > v.sqrMagnitude) v = VirtualStick;
            return Vector2.ClampMagnitude(v, 1f);
        }

        public static bool Pressed(GameKey k)
        {
            // aceita o toque deste frame ou do anterior (OnGUI roda depois do Update)
            if (virtualKey == k && Time.frameCount - virtualPressedFrame <= 1)
            {
                virtualPressedFrame = -10;
                return true;
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (k)
            {
                case GameKey.Interagir: return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
                case GameKey.Bolsa: return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
                case GameKey.Livro: return Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.L);
                case GameKey.Fechar: return Input.GetKeyDown(KeyCode.Escape);
                case GameKey.Som: return Input.GetKeyDown(KeyCode.M);
                case GameKey.Historico: return Input.GetKeyDown(KeyCode.H);
            }
#endif
            return false;
        }

        public static bool IsTouchDevice
        {
            get { return Application.isMobilePlatform; }
        }
    }
}
