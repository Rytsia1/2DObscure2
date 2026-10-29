using System.Collections.Generic;
using UnityEngine;

// Procedural pixel-art sprites at 1 pixel per unit, point-filtered.
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
            if (Cache.TryGetValue(key, out var s)) return s;
            int w = 0, h = rows.Length;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    px[(h - 1 - y) * w + x] = Palette.Key(rows[y][x]);
            return Cache[key] = SpriteFrom(px, w, h, p, "art");
        }

        // Framed panel: 1px dark outline, 1px border color, flat fill.
        public static Sprite Box(int w, int h, Color fill, Color border)
        {
            var key = ("box", w, h, fill, border);
            if (Cache.TryGetValue(key, out var s)) return s;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    bool corner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
                    px[y * w + x] = corner ? Color.clear : edge == 0 ? Palette.Void : edge == 1 ? border : fill;
                }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "box");
        }

        static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        // Vertical gradient through palette stops using ordered dithering (no blended colors).
        public static Sprite Gradient(int w, int h, params Color[] stops)
        {
            var key = ("grad", w, h, string.Join(",", stops));
            if (Cache.TryGetValue(key, out var s)) return s;
            var px = new Color32[w * h];
            int bands = stops.Length - 1;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1) * bands; // 0 at top
                int band = Mathf.Min((int)t, bands - 1);
                float f = t - band;
                for (int x = 0; x < w; x++)
                {
                    bool next = f * 16f > Bayer4[(y & 3) * 4 + (x & 3)] + 0.5f;
                    px[(h - 1 - y) * w + x] = stops[next ? band + 1 : band];
                }
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "gradient");
        }

        // Jagged horizon silhouette filled downward (hills, crypt rubble).
        public static Sprite Ridge(int w, int h, Color color, int seed)
        {
            var key = ("ridge", w, h, color, seed);
            if (Cache.TryGetValue(key, out var s)) return s;
            var rng = new System.Random(seed);
            var px = new Color32[w * h];
            int top = h / 2;
            for (int x = 0; x < w; x++)
            {
                if (x % 3 == 0) top = Mathf.Clamp(top + rng.Next(-2, 3), 0, h - 2);
                for (int y = top; y < h; y++) px[(h - 1 - y) * w + x] = color;
            }
            return Cache[key] = SpriteFrom(px, w, h, TopLeft, "ridge");
        }

        public static Sprite Disc(int r, Color color)
        {
            var key = ("disc", r, color);
            if (Cache.TryGetValue(key, out var s)) return s;
            int d = r * 2 + 1;
            var px = new Color32[d * d];
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                    if ((x - r) * (x - r) + (y - r) * (y - r) <= r * r + r) px[y * d + x] = color;
            return Cache[key] = SpriteFrom(px, d, d, TopLeft, "disc");
        }
    }
}
