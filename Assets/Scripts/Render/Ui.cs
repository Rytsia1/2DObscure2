using System;
using System.Collections.Generic;
using UnityEngine;

// Sprite-based UI Engine conforming to Octopath Traveler II visual direction.
// Reference resolution: 640x360 (16:9), top-left pixel coordinates (0..640, 0..360), y down.
namespace Obscure
{
    public enum Align { Left, Center, Right }

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

        // Primary panel: Deep Charcoal (#171B24) fill with thin Antique Gold (#C5A66A) filigree border
        public static SpriteRenderer Panel(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.DeepCharcoal, border ?? Palette.AntiqueGold), x, y, Panels);

        // Secondary card / nested container: Midnight Navy (#263448) with subtle inner border (#2D3E56)
        public static SpriteRenderer Card(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.MidnightNavy, border ?? Palette.DarkBorder), x, y, Panels + 1);

        // Framed text button with Octopath tactile styling, top highlight sheen, and selection cursor.
        public static Selectable Button(Transform parent, string text, int x, int y, int w, Action onClick, int h = 18, bool interactable = true, bool showCursor = true, FontKind font = FontKind.Interface)
        {
            var go = new GameObject("Button:" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            var box = Image(go.transform, null, 0, 0, Widget);
            int gHeight = font == FontKind.Serif ? PixelFont.SerifGlyphHeight : PixelFont.GlyphHeight;
            var label = Label(go.transform, text, w / 2, (h - gHeight) / 2, Palette.WarmIvory, Align.Center, order: Widget + 1, font: font);
            var cursor = showCursor ? Label(go.transform, ">", -9, (h - PixelFont.GlyphHeight) / 2, Palette.AntiqueGoldBright, order: Widget + 1) : null;
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(w, h);
            sel.onClick = onClick;
            sel.label = label;
            sel.interactable = interactable;
            sel.visual = on =>
            {
                box.sprite = PixelArt.ButtonBox(w, h, on, sel.interactable);
                label.Color = !sel.interactable ? Palette.MutedGray : on ? Palette.WarmIvory : Palette.MutedGray;
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

        // Pill badge
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

    public class Backdrop : MonoBehaviour
    {
        readonly List<(Transform t, float speed, float phase, float x)> embers = new List<(Transform, float, float, float)>();
        readonly List<(Transform t, float phase, float rate)> stars = new List<(Transform, float, float)>();

        public static Backdrop Create(Transform parent, bool moon, int embersCount, params Color[] sky)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            Color[] finalSky = sky != null && sky.Length > 0 ? sky : new[] { Palette.Void, Palette.DeepCharcoal, Palette.MidnightNavy };
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
