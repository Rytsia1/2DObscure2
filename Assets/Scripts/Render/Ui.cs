using System;
using System.Collections.Generic;
using UnityEngine;

// Sprite-based UI Engine conforming to the "Starlit Reliquary" visual system.
// Reference resolution: 640x360 (16:9), top-left pixel coordinates (0..640, 0..360), y down.
namespace Obscure
{
    public enum Align { Left, Center, Right }
    public enum ButtonStyle { Slab, Primary, Flat }

    public static class Ui
    {
        public const int Width = 640, Height = 360;

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

        public static Label Label(Transform parent, string text, int x, int y, Color color, Align align = Align.Left, int scale = 1, int order = Text, FontKind font = FontKind.Interface)
        {
            var sr = Image(parent, null, 0, 0, order, color);
            var label = sr.gameObject.AddComponent<Label>();
            label.Init(sr, x, y, align, scale, font);
            label.Set(text);
            return label;
        }

        public static Label Label(Transform parent, string text, int x, int y, Color color, Align align, int scale, FontKind font, int order = Text)
        {
            return Label(parent, text, x, y, color, align, scale, order, font);
        }

        // Primary panel: Ink 2 fill, Ink 5 border, gold rivets on the corners
        public static SpriteRenderer Panel(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.DeepCharcoal, border ?? Palette.DarkBorder), x, y, Panels);

        // Secondary card / nested container: raised Ink 3 with Ink 5 border
        public static SpriteRenderer Card(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.MidnightNavy, border ?? Palette.DarkBorder), x, y, Panels + 1);

        // Ink slab button; focus lifts it into a gold frame with gold text and the pixel hand-pointer.
        // Primary = the one gold call-to-action per view; Flat = menu-list item (.mi) with a gold wash on focus.
        public static Selectable Button(Transform parent, string text, int x, int y, int w, Action onClick, int h = 18, bool interactable = true, bool showCursor = true, FontKind font = FontKind.Interface, ButtonStyle style = ButtonStyle.Slab)
        {
            var go = new GameObject("Button:" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            var box = Image(go.transform, null, 0, 0, Widget);
            int gHeight = font == FontKind.Serif ? PixelFont.SerifGlyphHeight : PixelFont.GlyphHeight;
            bool flat = style == ButtonStyle.Flat;
            var label = Label(go.transform, text, flat ? 6 : w / 2, (h - gHeight) / 2, Palette.WarmIvory, flat ? Align.Left : Align.Center, order: Widget + 1, font: font);
            var cursor = showCursor ? Image(go.transform, PixelArt.Pointer, -10, (h - 7) / 2, Widget + 1) : null;
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(w, h);
            sel.onClick = onClick;
            sel.label = label;
            sel.interactable = interactable;
            sel.visual = on =>
            {
                box.sprite = style == ButtonStyle.Flat ? (on ? PixelArt.MenuHighlight(w, h) : null)
                    : style == ButtonStyle.Primary ? PixelArt.PrimaryBox(w, h, on, sel.interactable)
                    : PixelArt.ButtonBox(w, h, on, sel.interactable);
                label.Color = !sel.interactable ? Palette.MutedGray
                    : style == ButtonStyle.Primary ? Palette.Ink1
                    : on ? Palette.AntiqueGoldBright : Palette.WarmIvory;
                if (cursor != null) cursor.gameObject.SetActive(on);
            };
            sel.visual(false);
            return sel;
        }

        // Modern gauge with outer bezel and two-tone gradient fill
        public static SpriteRenderer Gauge(Transform parent, int x, int y, int w, int h, float ratio, Color fill, Color highlight, Color? bg = null, int order = Widget)
        {
            return Image(parent, PixelArt.GaugeBar(w, h, ratio, fill, highlight, bg ?? Palette.OverlayCharcoal), x, y, order);
        }

        // Decorative horizontal divider with jewel center
        public static SpriteRenderer Divider(Transform parent, int x, int y, int w, Color? lineCol = null, Color? centerCol = null, int order = Panels + 1)
        {
            return Image(parent, PixelArt.Divider(w, lineCol ?? Palette.DarkBorder, centerCol ?? Palette.AntiqueGold), x, y, order);
        }

        // Solid rectangle (rules, scrims, fills).
        public static SpriteRenderer Rect(Transform parent, int x, int y, int w, int h, Color color, int order = Panels + 1)
        {
            var sr = Image(parent, PixelArt.Pixel, x, y, order, color);
            sr.transform.localScale = new Vector3(w, h, 1);
            return sr;
        }

        // Panel header (.ph): gold diamond + gold title over an ink rule spanning the panel width.
        public static Label Header(Transform parent, string text, int x, int y, int w, int order = Text)
        {
            Image(parent, PixelArt.Diamond(5, Palette.Gold2), x, y + 1, order);
            Rect(parent, x - 6, y + 12, w, 1, Palette.Ink4, order);
            return Label(parent, text, x + 9, y, Palette.Gold2, order: order);
        }

        // Key prompt (.hint): keycaps then the action. Returns the x where the next hint can start.
        public static int KeyHint(Transform parent, int x, int y, string action, params string[] keys)
        {
            foreach (var k in keys)
            {
                int w = Mathf.Max(11, PixelFont.Measure(k) + 6);
                Badge(parent, k, x, y, w, 11, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1);
                x += w + 2;
            }
            Label(parent, action, x + 2, y + 2, Palette.Mist2);
            return x + 2 + PixelFont.Measure(action) + 14;
        }

        // Square chip
        public static SpriteRenderer Badge(Transform parent, string text, int x, int y, int w, int h, Color fill, Color border, Color textColor, Align align = Align.Center, int order = Widget, FontKind font = FontKind.Interface)
        {
            var box = Image(parent, PixelArt.Badge(w, h, fill, border), x, y, order);
            int gHeight = font == FontKind.Serif ? PixelFont.SerifGlyphHeight : PixelFont.GlyphHeight;
            Label(parent, text, align == Align.Center ? x + w / 2 : align == Align.Right ? x + w - 3 : x + 3, y + (h - gHeight) / 2, textColor, align, order: order + 1, font: font);
            return box;
        }
    }

    public class Label : MonoBehaviour
    {
        SpriteRenderer sr;
        int x, y, scale;
        Align align;
        FontKind font;
        public string Text { get; private set; }

        public void Init(SpriteRenderer renderer, int px, int py, Align a, int s, FontKind f = FontKind.Interface)
        {
            sr = renderer; x = px; y = py; align = a; scale = s; font = f;
            transform.localScale = new Vector3(s, s, 1);
        }

        public Color Color { set => sr.color = value; }

        public void Set(string text, FontKind? overrideFont = null)
        {
            Text = text ?? "";
            if (overrideFont.HasValue) font = overrideFont.Value;
            sr.sprite = PixelFont.Render(Text, font);
            int w = (int)sr.sprite.rect.width * scale;
            int ox = align == Align.Center ? w / 2 : align == Align.Right ? w : 0;
            transform.localPosition = new Vector3(x - ox, -y, 0);
        }
    }

    public class Selectable : MonoBehaviour
    {
        public Vector2Int size;
        public Action onClick;
        public Action<int> onAdjust;
        public Action<bool> visual;
        public Label label;
        public bool interactable = true;
        public string tooltip;

        public bool Contains(Vector2 world)
        {
            var p = (Vector2)transform.position;
            return world.x >= p.x && world.x < p.x + size.x && world.y <= p.y && world.y > p.y - size.y;
        }
    }

    public class Menu : MonoBehaviour
    {
        public readonly List<Selectable> items = new List<Selectable>();
        public Action onBack;
        public Action<Selectable> onFocus;
        int index, bornFrame;
        Vector2 lastMouse;

        // Hover only focuses after the mouse moves, so a resting cursor never steals the keyboard default on a new screen.
        void Awake()
        {
            bornFrame = Time.frameCount;
            lastMouse = Controls.MouseWorld;
        }

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

    public class Backdrop : MonoBehaviour
    {
        readonly List<(Transform t, float speed, float phase, float x)> embers = new List<(Transform, float, float, float)>();
        readonly List<(Transform t, float phase, float rate)> stars = new List<(Transform, float, float)>();

        public static Backdrop Create(Transform parent, bool moon, int embersCount, params Color[] sky)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            // Default: Starlit Reliquary night sky
            Color[] finalSky = sky != null && sky.Length > 0 ? sky : new[] { Palette.Hex(0x070B1C), Palette.Hex(0x101A3A), Palette.Hex(0x28284F), Palette.Hex(0x3A2F55) };
            Ui.Image(go.transform, PixelArt.Gradient(Ui.Width, Ui.Height, finalSky), 0, 0, Ui.Back);
            if (moon)
            {
                // Cinematic moon positioned elegantly in the upper right
                Ui.Image(go.transform, PixelArt.Disc(22, Palette.Shade), 510, 24, Ui.Back + 1);
                Ui.Image(go.transform, PixelArt.Disc(19, Palette.WarmIvory), 513, 27, Ui.Back + 2);
                Ui.Image(go.transform, PixelArt.Disc(16, Palette.Shade), 522, 26, Ui.Back + 3);

                var rngStars = new System.Random(42);
                for (int i = 0; i < 45; i++)
                {
                    int sx = rngStars.Next(16, Ui.Width - 16);
                    int sy = rngStars.Next(10, 150);
                    if (Mathf.Abs(sx - 515) < 32 && Mathf.Abs(sy - 30) < 32) continue;
                    var star = Ui.Image(go.transform, PixelArt.Pixel, sx, sy, Ui.Back + 1, Palette.MutedGray);
                    b.stars.Add((star.transform, (float)rngStars.NextDouble() * Mathf.PI * 2, 1.5f + (float)rngStars.NextDouble() * 2f));
                }
            }
            var rng = new System.Random(7);
            for (int i = 0; i < embersCount; i++)
            {
                var sr = Ui.Image(go.transform, PixelArt.Pixel, 0, 0, Ui.Back + 4, i % 3 == 0 ? Palette.AntiqueGold : Palette.Ember);
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
            foreach (var (t, phase, rate) in stars)
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float brightness = (Mathf.Sin(time * rate + phase) + 1f) * 0.5f;
                    sr.color = brightness > 0.65f ? Palette.WarmIvory : brightness > 0.3f ? Palette.MutedGray : Palette.DarkBorder;
                }
            }
        }
    }
}
