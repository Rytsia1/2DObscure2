using System.Collections.Generic;
using UnityEngine;

// Procedural pixel-art sprites at 1 pixel per unit, point-filtered.
// Conforming to 2DObscure UI/UX Visual Direction Specification (Octopath Traveler II Style).
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

        // Octopath-style framed panel with chamfered corners, thin Antique Gold filigree, and corner bracket accents.
        public static Sprite Box(int w, int h, Color fill, Color border)
        {
            var key = ("box_octo", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];

            bool isGold = border == Palette.AntiqueGold || border == Palette.Gold || border == Palette.BorderGold;
            Color highlight = isGold ? Palette.AntiqueGoldBright
                : border == Palette.Crimson ? Palette.CrimsonBright
                : border == Palette.Emerald ? Palette.Emerald
                : border == Palette.ArcaneViolet ? Palette.Lilac
                : border == Palette.DarkBorder ? Palette.Shade
                : Color.Lerp(border, Color.white, 0.3f);

            bool large = w >= 40 && h >= 22;

            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y; // 0 at top
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    bool corner = (x == 0 || x == w - 1) && (py == 0 || py == h - 1);

                    if (corner)
                    {
                        px[y * w + x] = Color.clear;
                    }
                    else if (edge == 0)
                    {
                        // Outer subtle drop shadow line
                        px[y * w + x] = Palette.OverlayCharcoal;
                    }
                    else if (edge == 1)
                    {
                        // 1px primary filigree border
                        px[y * w + x] = border;
                    }
                    else if (edge == 2 && py == 2)
                    {
                        // Specular top highlight line for depth
                        px[y * w + x] = highlight;
                    }
                    else if (isGold && large && (
                        (x <= 4 && (py == 2 || py == 3)) || (x >= w - 5 && (py == 2 || py == 3)) ||
                        (x <= 4 && (py == h - 3 || py == h - 4)) || (x >= w - 5 && (py == h - 3 || py == h - 4)) ||
                        (py <= 4 && (x == 2 || x == 3)) || (py >= h - 5 && (x == 2 || x == 3)) ||
                        (py <= 4 && (x == w - 3 || x == w - 4)) || (py >= h - 5 && (x == w - 3 || x == w - 4))
                    ))
                    {
                        // Delicate Antique Gold corner bracket accents
                        px[y * w + x] = Palette.AntiqueGoldDim;
                    }
                    else
                    {
                        px[y * w + x] = fill;
                    }
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "box");
        }

        // Modern tactile button with Octopath selection states (Antique Gold border, Warm Ivory focus, left chevron accent)
        public static Sprite ButtonBox(int w, int h, bool focused, bool interactable)
        {
            var key = ("btn_octo", w, h, focused, interactable);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[w * h];

            Color border = !interactable ? Palette.DarkBorder : focused ? Palette.AntiqueGold : Palette.DarkBorder;
            Color fill = !interactable ? Palette.OverlayCharcoal : focused ? Palette.MidnightNavy : Palette.DeepCharcoal;
            Color topRim = !interactable ? Palette.Shade : focused ? Palette.AntiqueGoldBright : Palette.DarkBorder;
            Color leftTag = focused ? Palette.AntiqueGoldBright : Color.clear;

            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y; // 0 at top
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(py, h - 1 - py));
                    bool corner = (x == 0 || x == w - 1) && (py == 0 || py == h - 1);

                    if (corner)
                    {
                        px[y * w + x] = Color.clear;
                    }
                    else if (edge == 0)
                    {
                        px[y * w + x] = Palette.OverlayCharcoal;
                    }
                    else if (edge == 1)
                    {
                        px[y * w + x] = border;
                    }
                    else if (focused && (x == 2 || x == 3) && py >= 2 && py <= h - 3)
                    {
                        // Golden selection notch on the left
                        px[y * w + x] = leftTag;
                    }
                    else if (py == 2 && edge >= 2)
                    {
                        // Lit top highlight
                        px[y * w + x] = topRim;
                    }
                    else
                    {
                        px[y * w + x] = fill;
                    }
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "btn_box");
        }

        // Framed gauge bar (HP: Emerald, MP: Arcane Violet, Break: Antique Gold) with dark bezel and gradient fill
        public static Sprite GaugeBar(int w, int h, float ratio, Color fill, Color highlight, Color bg)
        {
            ratio = Mathf.Clamp01(ratio);
            int fillW = Mathf.RoundToInt((w - 2) * ratio);
            var key = ("gauge_v2", w, h, fillW, fill, highlight, bg);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y; // 0 at top
                for (int x = 0; x < w; x++)
                {
                    bool border = x == 0 || x == w - 1 || py == 0 || py == h - 1;
                    if (border)
                    {
                        px[y * w + x] = Palette.OverlayCharcoal;
                    }
                    else if (x <= fillW)
                    {
                        if (py == 1) px[y * w + x] = highlight;
                        else if (x == fillW && fillW > 1 && py == h - 2) px[y * w + x] = highlight;
                        else px[y * w + x] = fill;
                    }
                    else
                    {
                        px[y * w + x] = bg;
                    }
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

        // Pill badge with chamfered ends for tags and stats
        public static Sprite Badge(int w, int h, Color fill, Color border)
        {
            var key = ("badge_v2", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    bool corner = (x == 0 && (py <= 1 || py >= h - 2)) ||
                                  (x == 1 && (py == 0 || py == h - 1)) ||
                                  (x == w - 1 && (py <= 1 || py >= h - 2)) ||
                                  (x == w - 2 && (py == 0 || py == h - 1));

                    if (corner)
                    {
                        px[y * w + x] = Color.clear;
                    }
                    else if (x == 0 || x == w - 1 || py == 0 || py == h - 1 ||
                            (x == 1 && (py == 1 || py == h - 2)) ||
                            (x == w - 2 && (py == 1 || py == h - 2)))
                    {
                        px[y * w + x] = border;
                    }
                    else
                    {
                        px[y * w + x] = fill;
                    }
                }
            }
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
