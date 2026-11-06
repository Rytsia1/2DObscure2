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
                var mouse = Mouse.current;
                if (mouse == null) return new Vector2(float.NaN, float.NaN);
                Vector2 screenPos = mouse.position.ReadValue();
                var cam = Camera.main;
                if (cam == null) return new Vector2(float.NaN, float.NaN);

                var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
                if (ppc != null && ppc.enabled && ppc.pixelRatio > 0)
                {
                    int ratio = ppc.pixelRatio;
                    int viewW = ppc.refResolutionX * ratio;
                    int viewH = ppc.refResolutionY * ratio;
                    float viewX = (Screen.width - viewW) * 0.5f;
                    float viewY = (Screen.height - viewH) * 0.5f;

                    float normX = (screenPos.x - viewX) / viewW;
                    float normY = (screenPos.y - viewY) / viewH;
                    if (normX < 0f || normX > 1f || normY < 0f || normY > 1f)
                        return new Vector2(float.NaN, float.NaN);

                    float pixelX = normX * Ui.Width;
                    float pixelY = (1f - normY) * Ui.Height;
                    return new Vector2(-Ui.Width * 0.5f + pixelX, Ui.Height * 0.5f - pixelY);
                }

                if (cam.orthographic)
                {
                    float targetAspect = (float)Ui.Width / Ui.Height;
                    float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                    float viewW, viewH, viewX, viewY;
                    if (screenAspect > targetAspect)
                    {
                        viewH = Screen.height;
                        viewW = viewH * targetAspect;
                        viewX = (Screen.width - viewW) * 0.5f;
                        viewY = 0f;
                    }
                    else
                    {
                        viewW = Screen.width;
                        viewH = viewW / targetAspect;
                        viewX = 0f;
                        viewY = (Screen.height - viewH) * 0.5f;
                    }

                    float normX = (screenPos.x - viewX) / viewW;
                    float normY = (screenPos.y - viewY) / viewH;
                    if (normX < 0f || normX > 1f || normY < 0f || normY > 1f)
                        return new Vector2(float.NaN, float.NaN);

                    float pixelX = normX * Ui.Width;
                    float pixelY = (1f - normY) * Ui.Height;
                    return new Vector2(-Ui.Width * 0.5f + pixelX, Ui.Height * 0.5f - pixelY);
                }

                return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            }
        }
    }
}
