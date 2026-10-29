using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Menu and QTE input, keyboard + gamepad + mouse. Typing QTEs read Keyboard.onTextInput instead.
namespace Obscure
{
    public static class Controls
    {
        public static bool Locked;

        static bool Pressed(params Key[] keys)
        {
            var kb = Keyboard.current;
            if (Locked || kb == null) return false;
            foreach (var k in keys) if (kb[k].wasPressedThisFrame) return true;
            return false;
        }

        static bool Pad(System.Func<Gamepad, ButtonControl> button) =>
            !Locked && Gamepad.current != null && button(Gamepad.current).wasPressedThisFrame;

        public static bool Up => Pressed(Key.UpArrow, Key.W) || Pad(p => p.dpad.up);
        public static bool Down => Pressed(Key.DownArrow, Key.S) || Pad(p => p.dpad.down);
        public static bool Left => Pressed(Key.LeftArrow, Key.A) || Pad(p => p.dpad.left);
        public static bool Right => Pressed(Key.RightArrow, Key.D) || Pad(p => p.dpad.right);
        public static bool Confirm => Pressed(Key.Enter, Key.NumpadEnter, Key.Space, Key.Z) || Pad(p => p.buttonSouth);
        public static bool Back => Pressed(Key.Escape, Key.Backspace, Key.X) || Pad(p => p.buttonEast);

        // Timing/defense QTE button: one remappable key (Settings) plus gamepad south.
        public static Key QteKey = Key.Space;
        public static bool QtePress => Pressed(QteKey) || Pad(p => p.buttonSouth);

        public static bool Click => !Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static Vector2 MouseWorld
        {
            get
            {
                var cam = Camera.main;
                if (Mouse.current == null || cam == null) return new Vector2(float.NaN, float.NaN);
                return cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }
        }
    }
}
