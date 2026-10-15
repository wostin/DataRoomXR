using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DataRoomXR
{
    /// <summary>Teclado y mouse con el Input System nuevo o el clásico (sirve con ambos).</summary>
    public static class Entrada
    {
        public static Vector2 Mover()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        public static Vector2 Mirar()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.08f;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f;
#endif
        }

        public static bool Clic()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current;
            return (k != null && k.eKey.wasPressedThisFrame) ||
                   (m != null && m.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked);
#else
            return Input.GetKeyDown(KeyCode.E) ||
                   (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked);
#endif
        }

        public static bool Pausa()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>Tecla para abrir/cerrar el panel de preguntas.</summary>
        public static bool AlternarPanel()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.tabKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        // ---- Captura de texto escrito (sirve con ambos sistemas de input) ----
#if ENABLE_INPUT_SYSTEM
        static readonly System.Text.StringBuilder _buffer = new System.Text.StringBuilder();
        static bool _suscrito;

        static void Suscribir()
        {
            if (_suscrito || Keyboard.current == null) return;
            Keyboard.current.onTextInput += c => { if (!char.IsControl(c) || c == '\b') _buffer.Append(c); };
            _suscrito = true;
        }
#endif

        /// <summary>Devuelve los caracteres escritos en este frame (vacío si no hubo).</summary>
        public static string TextoEscrito()
        {
#if ENABLE_INPUT_SYSTEM
            Suscribir();
            if (_buffer.Length == 0) return "";
            string s = _buffer.ToString();
            _buffer.Clear();
            return s;
#else
            return Input.inputString;
#endif
        }

        /// <summary>Enter pulsado este frame.</summary>
        public static bool Enter()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        /// <summary>Retroceso pulsado este frame.</summary>
        public static bool Backspace()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.backspaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Backspace);
#endif
        }
    }

    /// <summary>Estado compartido: lo llena JugadorFPS (PC) o JugadorVR (visor).</summary>
    public static class EstadoJugador
    {
        public static Transform Raiz;
        public static Transform Cabeza;
        public static bool ModoVR;
        public static bool Bloqueado;
        public static BarraDato Apuntada;
    }
}
