using System;
using System.Collections.Generic;
using UnityEngine;

// Minimal sprite-based UI. Coordinates are top-left screen pixels (0..320, 0..180), y down.
namespace Obscure
{
    public enum Align { Left, Center, Right }

    public static class Ui
    {
        public const int Width = 320, Height = 180;

        // Sorting layers by convention.
        public const int Back = 0, World = 10, Panels = 20, Text = 30, Widget = 40, Overlay = 1000;

        public static SpriteRenderer Image(Transform parent, Sprite sprite, int x, int y, int order, Color? tint = null)
        {
            var go = new GameObject(sprite ? sprite.name : "image");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = tint ?? Color.white;
            return sr;
        }

        public static Label Label(Transform parent, string text, int x, int y, Color color, Align align = Align.Left, int scale = 1, int order = Text)
        {
            var sr = Image(parent, null, 0, 0, order, color);
            var label = sr.gameObject.AddComponent<Label>();
            label.Init(sr, x, y, align, scale);
            label.Set(text);
            return label;
        }

        public static SpriteRenderer Panel(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.Night, border ?? Palette.Shade), x, y, Panels);

        // Framed text button with selection cursor.
        public static Selectable Button(Transform parent, string text, int x, int y, int w, Action onClick, int h = 13, bool interactable = true)
        {
            var go = new GameObject("Button:" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            var box = Image(go.transform, null, 0, 0, Widget);
            var label = Label(go.transform, text, w / 2, (h - PixelFont.GlyphHeight) / 2, Palette.Parchment, Align.Center, order: Widget + 1);
            var cursor = Label(go.transform, ">", -7, (h - PixelFont.GlyphHeight) / 2, Palette.Gold, order: Widget + 1);
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(w, h);
            sel.onClick = onClick;
            sel.label = label;
            sel.interactable = interactable;
            sel.visual = on =>
            {
                box.sprite = PixelArt.Box(w, h, on ? Palette.Dusk : Palette.Night, on ? Palette.Gold : Palette.Shade);
                label.Color = !sel.interactable ? Palette.Ash : on ? Palette.Bone : Palette.Parchment;
                cursor.gameObject.SetActive(on);
            };
            sel.visual(false);
            return sel;
        }
    }

    public class Label : MonoBehaviour
    {
        SpriteRenderer sr;
        int x, y, scale;
        Align align;
        public string Text { get; private set; }

        public void Init(SpriteRenderer renderer, int px, int py, Align a, int s)
        {
            sr = renderer; x = px; y = py; align = a; scale = s;
            transform.localScale = new Vector3(s, s, 1);
        }

        public Color Color { set => sr.color = value; }

        public void Set(string text)
        {
            Text = text;
            sr.sprite = PixelFont.Render(text);
            int w = (int)sr.sprite.rect.width * scale;
            int ox = align == Align.Center ? w / 2 : align == Align.Right ? w : 0;
            transform.localPosition = new Vector3(x - ox, -y, 0);
        }
    }

    public class Selectable : MonoBehaviour
    {
        public Vector2Int size;
        public Action onClick;
        public Action<int> onAdjust;   // left/right changes a value instead of moving focus
        public Action<bool> visual;
        public Label label;
        public bool interactable = true; // focusable but refuses activation (e.g. not enough MP)
        public string tooltip;

        public bool Contains(Vector2 world)
        {
            var p = (Vector2)transform.position;
            return world.x >= p.x && world.x < p.x + size.x && world.y <= p.y && world.y > p.y - size.y;
        }
    }

    // Keyboard/gamepad/mouse focus over a flat list of selectables.
    public class Menu : MonoBehaviour
    {
        public readonly List<Selectable> items = new List<Selectable>();
        public Action onBack;
        public Action<Selectable> onFocus;
        int index, bornFrame;
        Vector2 lastMouse;

        // A submenu opened by a key press must not consume that same press.
        void Awake() => bornFrame = Time.frameCount;

        public Selectable Current => index < items.Count ? items[index] : null;

        public Selectable Add(Selectable s)
        {
            items.Add(s);
            if (items.Count - 1 == index) Focus(index, false, true);
            return s;
        }

        public void Focus(int i, bool sound = true, bool force = false)
        {
            if (i == index && !force) return;
            Current?.visual?.Invoke(false);
            index = i;
            Current?.visual?.Invoke(true);
            if (sound) Sfx.Play(Sound.Move);
            onFocus?.Invoke(Current);
        }

        void Update()
        {
            if (Controls.Locked || items.Count == 0 || Time.frameCount == bornFrame) return;

            var mouse = Controls.MouseWorld;
            int hit = items.FindIndex(s => s.Contains(mouse));
            if (mouse != lastMouse && hit >= 0) Focus(hit);
            lastMouse = mouse;
            if (Controls.Click && hit >= 0) { Focus(hit, false); Activate(); return; }

            var adjust = Current.onAdjust;
            if (adjust != null && (Controls.Left || Controls.Right)) { adjust(Controls.Left ? -1 : 1); Sfx.Play(Sound.Move); }
            else if (Controls.Up || Controls.Left) Focus((index - 1 + items.Count) % items.Count);
            else if (Controls.Down || Controls.Right) Focus((index + 1) % items.Count);

            if (Controls.Confirm) Activate();
            else if (Controls.Back && onBack != null) { Sfx.Play(Sound.Back); onBack(); }
        }

        void Activate()
        {
            if (Current.onClick == null) return;
            if (!Current.interactable) { Sfx.Play(Sound.Deny); return; }
            Sfx.Play(Sound.Confirm);
            Current.onClick();
        }
    }

    // Shared atmospheric background: dithered sky, optional moon, ridge, drifting embers.
    public class Backdrop : MonoBehaviour
    {
        readonly List<(Transform t, float speed, float phase, float x)> embers = new List<(Transform, float, float, float)>();

        public static Backdrop Create(Transform parent, bool moon, int embersCount, params Color[] sky)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            Ui.Image(go.transform, PixelArt.Gradient(Ui.Width, Ui.Height, sky), 0, 0, Ui.Back);
            if (moon)
            {
                Ui.Image(go.transform, PixelArt.Disc(13, Palette.Shade), 245, 14, Ui.Back + 1);
                Ui.Image(go.transform, PixelArt.Disc(11, Palette.Parchment), 247, 16, Ui.Back + 2);
                Ui.Image(go.transform, PixelArt.Disc(9, Palette.Shade), 253, 15, Ui.Back + 3);
            }
            var rng = new System.Random(7);
            for (int i = 0; i < embersCount; i++)
            {
                var sr = Ui.Image(go.transform, PixelArt.Pixel, 0, 0, Ui.Back + 4, i % 3 == 0 ? Palette.Gold : Palette.Ember);
                b.embers.Add((sr.transform, 6f + rng.Next(10), rng.Next(1000) / 100f, rng.Next(Ui.Width)));
            }
            return b;
        }

        void Update()
        {
            float time = Time.unscaledTime;
            foreach (var (t, speed, phase, x) in embers)
            {
                float y = Ui.Height - ((time * speed + phase * 40f) % (Ui.Height + 10));
                t.localPosition = new Vector3(Mathf.Round(x + Mathf.Sin(time + phase) * 4f), -Mathf.Round(y), 0);
            }
        }
    }
}
