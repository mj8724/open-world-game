using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OpenWorld
{
    public static class OpenWorldInput
    {
        public static Keyboard Keyboard => Keyboard.current;
        public static Mouse Mouse => Mouse.current;

        public static Vector2 PointerPosition => Mouse?.position.ReadValue() ?? Vector2.zero;
        public static Vector2 Scroll => Mouse?.scroll.ReadValue() ?? Vector2.zero;

        public static bool Pressed(ButtonControl control) => control != null && control.wasPressedThisFrame;
        public static bool Released(ButtonControl control) => control != null && control.wasReleasedThisFrame;
        public static bool Held(ButtonControl control) => control != null && control.isPressed;

        public static bool ShiftHeld
        {
            get
            {
                var keyboard = Keyboard;
                return keyboard != null && (Held(keyboard.leftShiftKey) || Held(keyboard.rightShiftKey));
            }
        }
    }
}
