using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace NThirdPerson
{
    /// <summary>
    /// Lo que este paquete le pedia a UnityEngine.Input, sobre el Input System nuevo: el proyecto lo usa en
    /// exclusiva (activeInputHandler = 1) y con el la clase Input lanza excepciones.
    ///
    /// Se conservan los KeyCode que ya estan guardados en escenas y prefabs (walkKey, jumpKey, la tecla de
    /// NKeyEvent): aqui se traducen a la tecla equivalente, asi que no hay que volver a configurar nada.
    /// </summary>
    public static class EntradaNueva
    {
        /// <summary>Los ejes «Horizontal» y «Vertical» de antes: WASD, flechas y el stick izquierdo. De -1 a 1.</summary>
        public static float Eje(string nombre)
        {
            var horizontal = nombre == "Horizontal";
            if (!horizontal && nombre != "Vertical") { return 0; }

            float valor = 0;
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                if (horizontal) { valor = Par(teclado.dKey, teclado.rightArrowKey) - Par(teclado.aKey, teclado.leftArrowKey); }
                else { valor = Par(teclado.wKey, teclado.upArrowKey) - Par(teclado.sKey, teclado.downArrowKey); }
            }

            var mando = Gamepad.current;
            if (mando != null)
            {
                var stick = mando.leftStick.ReadValue();
                valor += horizontal ? stick.x : stick.y;
            }
            return Mathf.Clamp(valor, -1, 1);
        }

        /// <summary>Input.GetKey: la tecla esta bajada.</summary>
        public static bool Tecla(KeyCode codigo)
        {
            var control = Control(codigo);
            return control != null && control.isPressed;
        }

        /// <summary>Input.GetKeyDown: la tecla se bajo en este fotograma.</summary>
        public static bool TeclaPulsada(KeyCode codigo)
        {
            var control = Control(codigo);
            return control != null && control.wasPressedThisFrame;
        }

        private static float Par(KeyControl a, KeyControl b) { return a.isPressed || b.isPressed ? 1 : 0; }

        // Los KeyCode cuyo nombre no coincide con el de Key. El resto (letras, flechas, F1…, Space, Tab,
        // Escape, LeftShift…) se llaman igual y se resuelven por nombre.
        private static readonly Dictionary<KeyCode, Key> Distintas = new Dictionary<KeyCode, Key>
        {
            { KeyCode.Return, Key.Enter }, { KeyCode.KeypadEnter, Key.NumpadEnter },
            { KeyCode.LeftControl, Key.LeftCtrl }, { KeyCode.RightControl, Key.RightCtrl },
            { KeyCode.LeftCommand, Key.LeftMeta }, { KeyCode.RightCommand, Key.RightMeta },
            { KeyCode.LeftWindows, Key.LeftMeta }, { KeyCode.RightWindows, Key.RightMeta },
            { KeyCode.BackQuote, Key.Backquote }, { KeyCode.Print, Key.PrintScreen }, { KeyCode.Numlock, Key.NumLock },
            { KeyCode.Alpha0, Key.Digit0 }, { KeyCode.Alpha1, Key.Digit1 }, { KeyCode.Alpha2, Key.Digit2 },
            { KeyCode.Alpha3, Key.Digit3 }, { KeyCode.Alpha4, Key.Digit4 }, { KeyCode.Alpha5, Key.Digit5 },
            { KeyCode.Alpha6, Key.Digit6 }, { KeyCode.Alpha7, Key.Digit7 }, { KeyCode.Alpha8, Key.Digit8 },
            { KeyCode.Alpha9, Key.Digit9 },
            { KeyCode.Keypad0, Key.Numpad0 }, { KeyCode.Keypad1, Key.Numpad1 }, { KeyCode.Keypad2, Key.Numpad2 },
            { KeyCode.Keypad3, Key.Numpad3 }, { KeyCode.Keypad4, Key.Numpad4 }, { KeyCode.Keypad5, Key.Numpad5 },
            { KeyCode.Keypad6, Key.Numpad6 }, { KeyCode.Keypad7, Key.Numpad7 }, { KeyCode.Keypad8, Key.Numpad8 },
            { KeyCode.Keypad9, Key.Numpad9 }
        };

        private static readonly Dictionary<KeyCode, Key> Traducidas = new Dictionary<KeyCode, Key>();

        private static ButtonControl Control(KeyCode codigo)
        {
            if (codigo >= KeyCode.Mouse0 && codigo <= KeyCode.Mouse2)
            {
                var raton = Mouse.current;
                if (raton == null) { return null; }
                return codigo == KeyCode.Mouse0 ? raton.leftButton : codigo == KeyCode.Mouse1 ? raton.rightButton : raton.middleButton;
            }

            var teclado = Keyboard.current;
            if (teclado == null) { return null; }

            Key tecla;
            if (!Traducidas.TryGetValue(codigo, out tecla))
            {
                if (!Distintas.TryGetValue(codigo, out tecla) && !Enum.TryParse(codigo.ToString(), out tecla)) { tecla = Key.None; }
                Traducidas[codigo] = tecla;
            }
            return tecla == Key.None ? null : teclado[tecla];
        }
    }
}
