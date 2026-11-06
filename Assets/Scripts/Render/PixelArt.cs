using System.Collections.Generic;
using UnityEngine;

// Procedural pixel-art sprites at 1 pixel per unit, point-filtered.
// Conforming to the 2DObscure "Starlit Reliquary" visual system.
namespace Obscure
{
    public static class PixelArt
    {
        public static readonly Vector2 TopLeft = new Vector2(0, 1);
        static readonly Dictionary<object, Sprite> Cache = new Dictionary<object, Sprite>();

        static Sprite pixel;
        public static Sprite Pixel => pixel ? pixel : pixel = SpriteFrom(new[] { (Color32)Color.white }, 1, 1, TopLeft, "pixel");

        public static Sprite SpriteFrom(Color32[] px, int w, int h, Vector2 pivot, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
            tex.SetPixels32(px);
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 1f, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }

        // Rows are top-to-bottom; short rows are padded with transparency.
        public static Sprite FromRows(string[] rows, Vector2? pivot = null)
        {
            var p = pivot ?? TopLeft;
            var key = (rows, p);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            int w = 0, h = rows.Length;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    px[(h - 1 - y) * w + x] = Palette.Key(rows[y][x]);
            return Cache[key] = SpriteFrom(px, w, h, p, "art");
        }

        // Starlit Reliquary panel: square corners, border, ink inset ring and gold rivets on the corners.
        // Ornament lives on the frame only; the interior stays calm.
        public static Sprite Box(int w, int h, Color fill, Color border)
        {
            var key = ("box_reliquary", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            bool rivets = w >= 24 && h >= 20;

            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y; // 0 at top
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    bool rivet = rivets && (x < 3 || x >= w - 3) && (py < 3 || py >= h - 3);
                    px[y * w + x] = rivet ? Palette.Gold3 : edge == 0 ? border : edge == 1 ? Palette.Ink1 : fill;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "box");
        }

        // Notched box (.ec): enemy cards have 2px chamfered / notched corners.
        public static Sprite NotchedBox(int w, int h, Color fill, Color border)
        {
            var key = ("notched_box", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    bool c0 = (x < 2 && py < 2 && (x + py < 2));
                    bool c1 = (x >= w - 2 && py < 2 && ((w - 1 - x) + py < 2));
                    bool c2 = (x < 2 && py >= h - 2 && (x + (h - 1 - py) < 2));
                    bool c3 = (x >= w - 2 && py >= h - 2 && ((w - 1 - x) + (h - 1 - py) < 2));
                    if (c0 || c1 || c2 || c3)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }
                    bool b0 = (x + py == 2) || ((w - 1 - x) + py == 2) || (x + (h - 1 - py) == 2) || ((w - 1 - x) + (h - 1 - py) == 2);
                    bool edge = (x == 0 || x == w - 1 || py == 0 || py == h - 1);
                    px[y * w + x] = (edge || b0) ? border : fill;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "notched_box");
        }

        // Double-border box for Legendary & Mythic items (.rar.r-leg, .rar.r-myt).
        public static Sprite DoubleBorderBox(int w, int h, Color fill, Color border)
        {
            var key = ("double_border_box", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    px[y * w + x] = (edge == 0 || edge == 2) ? border : fill;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "double_border_box");
        }

        // Starlit Reliquary button: ink slab with a pressed-in bottom lip.
        // Focused = lifted ink + gold frame; disabled = flat with a dashed border.
        public static Sprite ButtonBox(int w, int h, bool focused, bool interactable)
        {
            var key = ("btn_reliquary", w, h, focused, interactable);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];

            Color border = interactable && focused ? Palette.Gold2 : Palette.Ink5;
            Color fill = !interactable ? Palette.Ink2 : focused ? Palette.Ink4 : Palette.Ink3;
            Color lip = !interactable ? fill : focused ? Palette.Ink3 : Palette.Ink2;

            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y; // 0 at top
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    bool gap = !interactable && ((x + py) / 2) % 2 == 1;
                    px[y * w + x] = edge == 0 ? (gap ? fill : border) : py == h - 2 ? lip : fill;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "btn_box");
        }

        // Gold primary button (.btn.pri): one per view. Focus brightens the slab.
        public static Sprite PrimaryBox(int w, int h, bool focused, bool interactable)
        {
            if (!interactable) return ButtonBox(w, h, false, false);
            var key = ("btn_primary", w, h, focused);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            Color fill = focused ? Palette.Gold1 : Palette.Gold2;
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    px[y * w + x] = edge == 0 ? (py == h - 1 ? Palette.Gold4 : Palette.Gold1) : py == h - 2 ? Palette.Gold3 : fill;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "btn_primary");
        }

        // Danger button (.btn.danger): destructive confirmation button with crimson alert style.
        public static Sprite DangerBox(int w, int h, bool focused, bool interactable)
        {
            if (!interactable) return ButtonBox(w, h, false, false);
            var key = ("btn_danger", w, h, focused);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            Color fill = focused ? Palette.Hex(0x5A1A1E) : Palette.Hex(0x321014);
            Color border = focused ? Palette.HpLow : Palette.Hex(0xA83232);
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    px[y * w + x] = edge == 0 ? border : py == h - 2 ? Palette.Hex(0x200A0C) : fill;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "btn_danger");
        }

        // Currency Coin: solid disc for Hub Gold, diamond for Run Gold
        public static Sprite Coin(int size, bool isHubGold)
        {
            return isHubGold ? Disc(size / 2, Palette.Gold2) : Diamond(size, Palette.Ember2);
        }

        // Menu-list focus (.mi.is-focus): gold wash fading out to the right.
        public static Sprite MenuHighlight(int w, int h)
        {
            var key = ("menu_hl", w, h);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                var c = Palette.Gold2;
                c.a = 0.18f * (1f - (float)x / w);
                for (int y = 0; y < h; y++) px[y * w + x] = c;
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "menu_hl");
        }

        // Diamond pip (rotated square), filled or outlined; size should be odd.
        public static Sprite Diamond(int size, Color color, bool filled = true)
        {
            var key = ("diamond", size, color, filled);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[size * size];
            int r = size / 2;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int d = Mathf.Abs(x - r) + Mathf.Abs(y - r);
                    if (filled ? d <= r : d == r) px[y * size + x] = color;
                }
            return Cache[key] = SpriteFrom(px, size, size, TopLeft, "diamond");
        }

        // Pixel hand-pointer: shown only for keyboard/controller focus.
        static readonly string[] PointerRows = { "YYY", "YYYYY", "YYYYYYY", "YYYYYYY", "YYYYYYY", "YYYYY", "YYY" };
        public static Sprite Pointer => FromRows(PointerRows);

        // Segmented vital bar (HP / MP / XP): bordered trough, lit top edge, tick every 5px for quick reading.
        public static Sprite GaugeBar(int w, int h, Color fill, float ratio) =>
            GaugeBar(w, h, ratio, fill, Color.Lerp(fill, Color.white, 0.35f), Palette.OverlayCharcoal);

        public static Sprite GaugeBar(int w, int h, float ratio, Color fill)
        {
            return GaugeBar(w, h, fill, ratio);
        }

        public static Sprite GaugeBar(int w, int h, float ratio, Color fill, Color highlight, Color bg)
        {
            ratio = Mathf.Clamp01(ratio);
            int fillW = Mathf.RoundToInt((w - 2) * ratio);
            var key = ("gauge_reliquary", w, h, fillW, fill, highlight, bg);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            Color shade = Color.Lerp(fill, Color.black, 0.25f);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y; // 0 at top
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || x == w - 1 || py == 0 || py == h - 1)
                    {
                        px[y * w + x] = Palette.Ink5;
                        continue;
                    }
                    Color c = x > fillW ? bg : py == 1 ? highlight : py == h - 2 && h >= 5 ? shade : fill;
                    if (x % 5 == 0) c = Color.Lerp(c, Palette.Ink0, 0.55f); // segment tick
                    px[y * w + x] = c;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "gauge");
        }

        // Tapered horizontal rule with antique gold central diamond crest and wing accents (───◆───)
        public static Sprite Divider(int w, Color lineCol, Color centerCol)
        {
            int h = 5;
            var key = ("divider_octo", w, lineCol, centerCol);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            var px = new Color32[w * h];
            int cx = w / 2;
            for (int x = 0; x < w; x++)
            {
                if (x >= 4 && x <= w - 5)
                {
                    px[2 * w + x] = lineCol;
                }
                int dist = Mathf.Abs(x - cx);
                if (dist == 0)
                {
                    px[0 * w + x] = centerCol;
                    px[1 * w + x] = centerCol;
                    px[2 * w + x] = centerCol;
                    px[3 * w + x] = centerCol;
                    px[4 * w + x] = centerCol;
                }
                else if (dist == 1)
                {
                    px[1 * w + x] = centerCol;
                    px[2 * w + x] = centerCol;
                    px[3 * w + x] = centerCol;
                }
                else if (dist == 2)
                {
                    px[2 * w + x] = centerCol;
                }
                else if (dist == 3)
                {
                    px[2 * w + x] = lineCol;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "divider");
        }

        // Square chip for tags, status, rarity and stats: 1px border on a dark fill.
        // Consumable / material icons (.pi placeholders) tinted with one color; gear uses Art.GearIcon.
        static readonly Dictionary<string, string[]> ItemIconRows = new Dictionary<string, string[]>
        {
            ["potion"] = new[] { "...####...", "....##....", "....##....", "..######..", ".########.", ".########.", ".########.", "..######.." },
            ["scroll"] = new[] { "##########", ".########.", ".########.", ".########.", ".########.", ".########.", ".########.", "##########" },
            ["ore"]    = new[] { "...###....", "..######..", ".########.", "##########", ".########.", "..######.." },
            ["herb"]   = new[] { "....##....", "...####...", "..######..", ".###..###.", "..######..", "....##....", "....##....", "....##...." },
        };

        public static Sprite ItemIcon(string kind, Color color)
        {
            if (!ItemIconRows.TryGetValue(kind ?? "", out var rows)) rows = ItemIconRows["scroll"];
            var key = ("item_icon", kind, color);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            int w = rows[0].Length, h = rows.Length;
            var px = new Color32[w * h];
            for (int r = 0; r < h; r++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - r) * w + x] = rows[r][x] == '#' ? (Color32)color : default;
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "item_icon");
        }

        // Relic frame (.relic): octagon, never square like gear. Optional inner diamond; dashed edge for empty slots.
        public static Sprite Octagon(int size, Color fill, Color border, bool dashed = false)
        {
            var key = ("octagon", size, fill, border, dashed);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            int cut = Mathf.Max(2, size / 4), r = size / 2;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x), dy = Mathf.Min(y, size - 1 - y);
                    if (dx + dy < cut) continue; // outside the cut corners
                    bool edge = dx == 0 || dy == 0 || dx + dy == cut;
                    bool diamond = Mathf.Abs(x - r) + Mathf.Abs(y - r) == r - cut / 2;
                    if (edge) px[y * size + x] = dashed && ((x + y) / 2) % 2 == 1 ? fill : border;
                    else px[y * size + x] = diamond ? Color.Lerp(fill, border, 0.55f) : fill;
                }
            return Cache[key] = SpriteFrom(px, size, size, TopLeft, "octagon");
        }

        // Dashed 1px frame (.tag.temp / .tag.cond / locked & upcoming states): 2 on, 2 off.
        public static Sprite DashedBadge(int w, int h, Color fill, Color border)
        {
            var key = ("badge_dashed", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    px[y * w + x] = edge ? (((x + y) / 2) % 2 == 0 ? border : fill) : fill;
                }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "badge_dashed");
        }

        public static Sprite Badge(int w, int h, Color fill, Color border)
        {
            var key = ("badge_reliquary", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = x == 0 || x == w - 1 || y == 0 || y == h - 1 ? border : fill;
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "badge");
        }

        // Solid circle disc
        public static Sprite Disc(int radius, Color color)
        {
            var key = ("disc", radius, color);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            int d = radius * 2 + 1;
            var px = new Color32[d * d];
            int r2 = radius * radius;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    int dx = x - radius, dy = y - radius;
                    if (dx * dx + dy * dy <= r2) px[y * d + x] = color;
                }
            return Cache[key] = SpriteFrom(px, d, d, new Vector2(0.5f, 0.5f), "disc");
        }

        // Dithered horizontal gradient between two colors
        public static Sprite Gradient(int w, int h, params Color[] stops)
        {
            var key = ("grad_v2", w, h, stops.Length, stops[0]);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / Mathf.Max(1, h - 1);
                float seg = t * (stops.Length - 1);
                int i0 = Mathf.Min(stops.Length - 2, Mathf.FloorToInt(seg));
                float f = seg - i0;
                Color col = Color.Lerp(stops[i0], stops[i0 + 1], f);

                for (int x = 0; x < w; x++)
                {
                    // 2x2 Bayer dither
                    int dither = ((x % 2) ^ (y % 2)) == 0 ? 1 : -1;
                    Color c = col + new Color(dither * 0.015f, dither * 0.015f, dither * 0.015f, 0);
                    px[y * w + x] = c;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "gradient");
        }

        // Jagged mountain ridge line
        public static Sprite Ridge(int w, int h, Color color, int seed)
        {
            var key = ("ridge_v2", w, h, color, seed);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];
            var rng = new System.Random(seed);
            int curH = h / 2;
            int[] heights = new int[w];
            for (int x = 0; x < w; x++)
            {
                curH += rng.Next(-1, 2);
                curH = Mathf.Clamp(curH, 2, h - 2);
                heights[x] = curH;
            }
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < heights[x]; y++)
                {
                    px[y * w + x] = color;
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "ridge");
        }
    }
}
