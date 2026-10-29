using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 5x7 bitmap font baked to sprites (white glyphs + 1px dark shadow, tint via SpriteRenderer.color).
namespace Obscure
{
    public static class PixelFont
    {
        public const int GlyphHeight = 7, LineHeight = 10;

        static readonly Dictionary<char, string[]> Glyphs = Parse(new Dictionary<char, string>
        {
            ['A'] = ".###.|#...#|#...#|#####|#...#|#...#|#...#",
            ['B'] = "####.|#...#|#...#|####.|#...#|#...#|####.",
            ['C'] = ".###.|#...#|#....|#....|#....|#...#|.###.",
            ['D'] = "####.|#...#|#...#|#...#|#...#|#...#|####.",
            ['E'] = "#####|#....|#....|####.|#....|#....|#####",
            ['F'] = "#####|#....|#....|####.|#....|#....|#....",
            ['G'] = ".###.|#...#|#....|#.###|#...#|#...#|.####",
            ['H'] = "#...#|#...#|#...#|#####|#...#|#...#|#...#",
            ['I'] = "###|.#.|.#.|.#.|.#.|.#.|###",
            ['J'] = "..###|...#.|...#.|...#.|...#.|#..#.|.##..",
            ['K'] = "#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#",
            ['L'] = "#....|#....|#....|#....|#....|#....|#####",
            ['M'] = "#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#",
            ['N'] = "#...#|#...#|##..#|#.#.#|#..##|#...#|#...#",
            ['O'] = ".###.|#...#|#...#|#...#|#...#|#...#|.###.",
            ['P'] = "####.|#...#|#...#|####.|#....|#....|#....",
            ['Q'] = ".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#",
            ['R'] = "####.|#...#|#...#|####.|#.#..|#..#.|#...#",
            ['S'] = ".####|#....|#....|.###.|....#|....#|####.",
            ['T'] = "#####|..#..|..#..|..#..|..#..|..#..|..#..",
            ['U'] = "#...#|#...#|#...#|#...#|#...#|#...#|.###.",
            ['V'] = "#...#|#...#|#...#|#...#|#...#|.#.#.|..#..",
            ['W'] = "#...#|#...#|#...#|#.#.#|#.#.#|#.#.#|.#.#.",
            ['X'] = "#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#",
            ['Y'] = "#...#|#...#|.#.#.|..#..|..#..|..#..|..#..",
            ['Z'] = "#####|....#|...#.|..#..|.#...|#....|#####",
            ['0'] = ".###.|#...#|#..##|#.#.#|##..#|#...#|.###.",
            ['1'] = ".#.|##.|.#.|.#.|.#.|.#.|###",
            ['2'] = ".###.|#...#|....#|...#.|..#..|.#...|#####",
            ['3'] = "####.|....#|....#|.###.|....#|....#|####.",
            ['4'] = "...#.|..##.|.#.#.|#..#.|#####|...#.|...#.",
            ['5'] = "#####|#....|####.|....#|....#|#...#|.###.",
            ['6'] = ".###.|#....|#....|####.|#...#|#...#|.###.",
            ['7'] = "#####|....#|...#.|..#..|.#...|.#...|.#...",
            ['8'] = ".###.|#...#|#...#|.###.|#...#|#...#|.###.",
            ['9'] = ".###.|#...#|#...#|.####|....#|....#|.###.",
            [' '] = "...|...|...|...|...|...|...",
            ['.'] = ".|.|.|.|.|.|#",
            [','] = "..|..|..|..|.#|.#|#.",
            [':'] = ".|.|#|.|.|#|.",
            ['!'] = "#|#|#|#|#|.|#",
            ['?'] = ".###.|#...#|....#|...#.|..#..|.....|..#..",
            ['\''] = "#|#|.|.|.|.|.",
            ['-'] = "...|...|...|###|...|...|...",
            ['_'] = "....|....|....|....|....|....|####",
            ['+'] = ".....|..#..|..#..|#####|..#..|..#..|.....",
            ['='] = ".....|.....|#####|.....|#####|.....|.....",
            ['/'] = "....#|....#|...#.|..#..|.#...|#....|#....",
            ['%'] = "##..#|##..#|...#.|..#..|.#...|#..##|#..##",
            ['('] = ".#|#.|#.|#.|#.|#.|.#",
            [')'] = "#.|.#|.#|.#|.#|.#|#.",
            ['['] = "##|#.|#.|#.|#.|#.|##",
            [']'] = "##|.#|.#|.#|.#|.#|##",
            ['<'] = "...#|..#.|.#..|#...|.#..|..#.|...#",
            ['>'] = "#...|.#..|..#.|...#|..#.|.#..|#...",
            ['|'] = "#|#|#|#|#|#|#",
            ['*'] =".....|#.#.#|.###.|#####|.###.|#.#.#|.....",
        });

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static Dictionary<char, string[]> Parse(Dictionary<char, string> src)
        {
            var d = new Dictionary<char, string[]>();
            foreach (var kv in src) d[kv.Key] = kv.Value.Split('|');
            return d;
        }

        static string[] Glyph(char c) => Glyphs.TryGetValue(char.ToUpperInvariant(c), out var g) ? g : Glyphs['?'];

        public static int Measure(string line)
        {
            int w = 0;
            foreach (var c in line) w += Glyph(c)[0].Length + 1;
            return Mathf.Max(0, w - 1);
        }

        // Trims text to fit maxWidth pixels, ending in '.' when cut.
        public static string Fit(string text, int maxWidth)
        {
            if (Measure(text) <= maxWidth) return text;
            while (text.Length > 1 && Measure(text + ".") > maxWidth) text = text.Substring(0, text.Length - 1);
            return text.TrimEnd() + ".";
        }

        // Names: "MARTIAL ARTIST" -> "MARTIAL" when the full name does not fit, then trimmed if still too wide.
        public static string FitName(string name, int maxWidth)
        {
            if (Measure(name) > maxWidth && name.Contains(' ')) name = name.Substring(0, name.IndexOf(' '));
            return Fit(name, maxWidth);
        }

        public static string Wrap(string text, int maxWidth)
        {
            if (text.Contains('\n')) // keep explicit line breaks, wrap each paragraph
                return string.Join("\n", System.Array.ConvertAll(text.Split('\n'), p => Wrap(p, maxWidth)));
            var sb = new StringBuilder();
            var line = "";
            foreach (var word in text.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (Measure(candidate) > maxWidth && line.Length > 0) { sb.Append(line).Append('\n'); line = word; }
                else line = candidate;
            }
            return sb.Append(line).ToString();
        }

        // ponytail: one texture per distinct string, never evicted. Fine for menu text; add LRU if dynamic text explodes.
        public static Sprite Render(string text)
        {
            if (Cache.TryGetValue(text, out var cached) && cached != null) return cached;
            var lines = text.Split('\n');
            int w = 1, h = (lines.Length - 1) * LineHeight + GlyphHeight + 1;
            foreach (var l in lines) w = Mathf.Max(w, Measure(l) + 1);

            var px = new Color32[w * h];
            Color32 shadow = Palette.Void, ink = Color.white;
            for (int pass = 0; pass < 2; pass++)
            {
                int off = pass == 0 ? 1 : 0;
                for (int li = 0; li < lines.Length; li++)
                {
                    int x = 0, y0 = li * LineHeight;
                    foreach (var c in lines[li])
                    {
                        var g = Glyph(c);
                        for (int gy = 0; gy < g.Length; gy++)
                            for (int gx = 0; gx < g[gy].Length; gx++)
                                if (g[gy][gx] == '#')
                                    px[(h - 1 - (y0 + gy + off)) * w + x + gx + off] = pass == 0 ? shadow : ink;
                        x += g[0].Length + 1;
                    }
                }
            }
            var sprite = PixelArt.SpriteFrom(px, w, h, new Vector2(0, 1), "text:" + text);
            Cache[text] = sprite;
            return sprite;
        }
    }
}
