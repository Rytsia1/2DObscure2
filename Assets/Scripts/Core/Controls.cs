using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Menu and QTE input, keyboard + gamepad + mouse. Typing QTEs read Keyboard.onTextInput instead.
namespace Obscure
{
    public static class Controls
    {
        public static bool Locked;
        public static bool SuppressKeyboardNavigation;

        static bool Pressed(params Key[] keys)
        {
            var kb = Keyboard.current;
            if (Locked || SuppressKeyboardNavigation || kb == null) return false;
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
        public static bool PrevPage => Pressed(Key.Q) || Pad(p => p.leftShoulder);
        public static bool NextPage => Pressed(Key.E) || Pad(p => p.rightShoulder);
        public static bool Details => Pressed(Key.Tab) || Pad(p => p.selectButton);

        // Timing/defense QTE button: one remappable key (Settings) plus gamepad south.
        public static Key QteKey = Key.Space;
        public static bool QtePress => Pressed(QteKey) || Pad(p => p.buttonSouth);
        // Charge QTE: letting go of the QTE button.
        public static bool QteReleased =>
            (!Locked && !SuppressKeyboardNavigation && Keyboard.current != null && Keyboard.current[QteKey].wasReleasedThisFrame)
            || (!Locked && Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame);

        // Last device the player touched wins. A keyboard or gamepad press hides the cursor and stops hover from moving
        // the menu selection; the mouse takes over again only once it really moves, clicks or scrolls.
        // MouseWakeDistance is a starting value, untested: raise it if testers still see the selection jump while
        // using the keyboard, lower it if the cursor feels slow to come back (GDD Bab 15, keyboard-first input).
        public const float MouseWakeDistance = 4f; // screen pixels
        static bool mouseActive = true;
        static Vector2 mouseAnchor;

        // Checked when read, not only in Tick: UI Toolkit sends the hover event in the same frame the mouse moves,
        // before Game.Update runs Tick, so the first real move after keyboard use must count straight away.
        public static bool MouseActive
        {
            get
            {
                var mouse = Mouse.current;
                if (mouse == null) return false;
                if (!mouseActive && ((mouse.position.ReadValue() - mouseAnchor).sqrMagnitude > MouseWakeDistance * MouseWakeDistance
                    || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                    || mouse.scroll.ReadValue() != Vector2.zero))
                    mouseActive = true;
                return mouseActive;
            }
        }

        public static void Tick()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || PadPressed(Gamepad.current))
            {
                mouseActive = false;
                if (mouse != null) mouseAnchor = mouse.position.ReadValue();
            }
            Cursor.visible = MouseActive;
        }

        static bool PadPressed(Gamepad pad)
        {
            if (pad == null) return false;
            foreach (var c in pad.allControls)
                if (c is ButtonControl b && b.wasPressedThisFrame) return true;
            return false;
        }
    }
}
