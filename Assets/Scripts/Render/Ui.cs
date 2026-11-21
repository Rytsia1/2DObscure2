using System;
using System.Collections.Generic;
using UnityEngine;

// Sprite-based UI Engine conforming to the "Starlit Reliquary" visual system.
// Reference resolution: 640x360 (16:9), top-left pixel coordinates (0..640, 0..360), y down.
namespace Obscure
{
    public enum Align { Left, Center, Right }
    public enum TagKind { Cls, Keep, Temp, Cond, Gold }

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

        public static Label Label(Transform parent, string text, int x, int y, Color color, Align align = Align.Left, int scale = 1, int order = Text, FontKind font = FontKind.DotGothic)
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

        // Enemy card (.ec): notched corners
        public static SpriteRenderer EnemyCard(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.NotchedBox(w, h, fill ?? Palette.MidnightNavy, border ?? Palette.DarkBorder), x, y, Panels + 1);

        // Double border card (.rar.r-leg / .rar.r-myt)
        public static SpriteRenderer DoubleBorderCard(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.DoubleBorderBox(w, h, fill ?? Palette.DeepCharcoal, border ?? Palette.Gold2), x, y, Panels + 1);

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
        public static Label Header(Transform parent, string text, int x, int y, int w, int order = Text, FontKind font = FontKind.DotGothic)
        {
            Image(parent, PixelArt.Diamond(5, Palette.Gold2), x, y + 1, order);
            Rect(parent, x - 6, y + 13, w, 1, Palette.Ink4, order);
            return Label(parent, text, x + 9, y, Palette.Gold2, order: order, font: font);
        }

        // Status tag (.st): ▲ buff, ▼ debuff, ● neutral
        public static GameObject StatusBadge(Transform parent, string code, string timeOrStack, bool isBuff, int x, int y, int order = Widget)
        {
            var go = new GameObject("Status:" + code);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            Color fill = isBuff ? Palette.Hex(0x0E2818) : Palette.Hex(0x280E12);
            Color border = isBuff ? Palette.Hp : Palette.HpLow;
            Color textCol = isBuff ? Palette.Hp : Palette.HpLow;
            string prefix = isBuff ? "▲ " : "▼ ";
            string text = prefix + code + (string.IsNullOrEmpty(timeOrStack) ? "" : " " + timeOrStack);
            int w = Mathf.Max(28, PixelFont.Measure(text, FontKind.DotGothic) + 6);
            Image(go.transform, PixelArt.Badge(w, 14, fill, border), 0, 0, order);
            Label(go.transform, text, w / 2, 2, textCol, Align.Center, order: order + 1, font: FontKind.DotGothic);
            return go;
        }

        // Currency tag (.cur): solid gold frame for Hub Gold, dashed ember frame for Run Gold
        public static GameObject CurrencyTag(Transform parent, int amount, bool isHubGold, int x, int y, int order = Widget)
        {
            var go = new GameObject("Currency:" + (isHubGold ? "Hub" : "Run"));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            string lbl = CurrencyText(amount, isHubGold);
            Color bg = isHubGold ? Palette.Hex(0x221A08) : Palette.Hex(0x241008);
            Color border = isHubGold ? Palette.Gold2 : Palette.Ember2;
            Color textCol = isHubGold ? Palette.Gold1 : Palette.Ember1;
            int w = CurrencyTagWidth(amount, isHubGold);
            Image(go.transform, PixelArt.Badge(w, 18, bg, border), 0, 0, order);
            Image(go.transform, PixelArt.Coin(7, isHubGold), 6, 5, order + 1);
            Label(go.transform, lbl, 16, 3, textCol, Align.Left, order: order + 1, font: FontKind.DotGothic);
            return go;
        }

        static string CurrencyText(int amount, bool isHubGold) => $"{amount:#,##0} " + (isHubGold ? "GOLD" : "SPOILS");

        // 16px coin gutter + text + 6px right padding.
        public static int CurrencyTagWidth(int amount, bool isHubGold) =>
            Mathf.Max(84, PixelFont.Measure(CurrencyText(amount, isHubGold), FontKind.DotGothic) + 22);

        // Key prompt (.hint): keycaps then the action. Returns the x where the next hint can start.
        public static int KeyHint(Transform parent, int x, int y, string action, params string[] keys)
        {
            foreach (var k in keys)
            {
                // Keycaps use the compact 5x7 face: DotGothic's 10px glyphs don't fit an 11px cap.
                int w = Mathf.Max(11, PixelFont.Measure(k, FontKind.Interface) + 6);
                Badge(parent, k, x, y, w, 11, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);
                x += w + 2;
            }
            Label(parent, action, x + 2, y + 2, Palette.Mist2, font: FontKind.DotGothic);
            return x + 2 + PixelFont.Measure(action, FontKind.DotGothic) + 14;
        }

        // Tag chip (.tag): cls = ink, keep = gold solid, temp = ember dashed, cond = mist dashed, gold = class/selected accent.
        // Returns the chip width so callers can lay tags out in a row.
        public static int Tag(Transform parent, string text, int x, int y, TagKind kind = TagKind.Cls, int h = 11, int order = Widget)
        {
            text = (text ?? "").ToUpperInvariant();
            int w = PixelFont.Measure(text, FontKind.Interface) + 8;
            var (fill, border, ink) = kind switch
            {
                TagKind.Keep => (Palette.Hex(0x1D1608), Palette.Gold3, Palette.Gold1),
                TagKind.Temp => (Palette.Hex(0x22120A), Palette.Ember2, Palette.Ember1),
                TagKind.Cond => (Color.clear, Palette.Mist3, Palette.Mist2),
                TagKind.Gold => (Palette.Ink3, Palette.Gold3, Palette.Gold1),
                _ => (Palette.Ink3, Palette.Ink5, Palette.Mist1),
            };
            bool dashed = kind == TagKind.Temp || kind == TagKind.Cond;
            Image(parent, dashed ? PixelArt.DashedBadge(w, h, fill, border) : PixelArt.Badge(w, h, fill, border), x, y, order);
            Label(parent, text, x + 4, y + (h - PixelFont.GlyphHeight) / 2, ink, order: order + 1, font: FontKind.Interface);
            return w;
        }

        // Inventory / equipment slot (.slot): ink well, rarity-tinted border, item icon, rarity letter top-left.
        // Empty slots show the slot's silhouette dimmed so the player can still tell what goes there.
        public static void ItemSlot(Transform parent, int x, int y, int size, EquipmentDef item, EquipmentSlot slot, bool selected = false, int order = Widget, string count = null)
        {
            Color rc = item != null ? Palette.ForRarity(item.rarity) : Palette.Ink4;
            Color border = selected ? Palette.Gold2 : item != null ? Color.Lerp(rc, Palette.Ink4, 0.45f) : Palette.Ink4;
            Image(parent, PixelArt.Box(size, size, Palette.Ink0, border), x, y, order);
            if (selected) Image(parent, PixelArt.Badge(size + 4, size + 4, Color.clear, Palette.Gold3), x - 2, y - 2, order);
            var icon = PixelArt.FromRows(Art.GearIcon(slot, item));
            int iw = (int)icon.rect.width, ih = (int)icon.rect.height;
            Image(parent, icon, x + (size - iw) / 2, y + (size - ih) / 2, order + 1, item != null ? rc : Palette.Ink4);
            if (item != null)
            {
                Image(parent, PixelArt.Badge(7, 8, rc, rc), x, y, order + 2);
                Label(parent, Palette.RarityBadge(item.rarity), x + 1, y + 1, Palette.Ink0, order: order + 3, font: FontKind.Interface);
            }
            if (!string.IsNullOrEmpty(count))
                Label(parent, count, x + size - 2, y + size - 9, Palette.Mist1, Align.Right, order: order + 3, font: FontKind.Interface);
        }

        // Relic token (.relic): octagon in the relic's rarity color with a 2-letter code; empty = dashed ink.
        public static void Relic(Transform parent, int x, int y, int size, RelicDef relic, int order = Widget)
        {
            Color c = relic != null ? RelicColor(relic) : Palette.Ink5;
            Image(parent, PixelArt.Octagon(size, Palette.Ink0, c, relic == null), x, y, order);
            if (relic != null) Label(parent, RelicCode(relic), x + size / 2, y + (size - PixelFont.GlyphHeight) / 2, c, Align.Center, order: order + 1, font: FontKind.Interface);
        }

        public static Color RelicColor(RelicDef r) => r.proposedRarity switch
        {
            RelicProposedRarity.Uncommon => Palette.RarityUncommon,
            RelicProposedRarity.Rare => Palette.RarityRare,
            RelicProposedRarity.Epic => Palette.RarityEpic,
            _ => Palette.RarityCommon,
        };

        public static string RelicCode(RelicDef r)
        {
            var words = (r?.name ?? "?").Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            return (words.Length >= 2 ? $"{words[0][0]}{words[1][0]}" : words[0].Substring(0, Math.Min(2, words[0].Length))).ToUpperInvariant();
        }

        // Square chip
        public static SpriteRenderer Badge(Transform parent, string text, int x, int y, int w, int h, Color fill, Color border, Color textColor, Align align = Align.Center, int order = Widget, FontKind font = FontKind.DotGothic)
        {
            var box = Image(parent, PixelArt.Badge(w, h, fill, border), x, y, order);
            int gHeight = font == FontKind.DotGothic ? PixelFont.DotGothicGlyphHeight : font == FontKind.Serif ? PixelFont.SerifGlyphHeight : PixelFont.GlyphHeight;
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

        public void Init(SpriteRenderer renderer, int px, int py, Align a, int s, FontKind f = FontKind.DotGothic)
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
