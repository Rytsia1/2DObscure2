using UnityEngine;

// 2DObscure Design System Palette (Octopath Traveler II Inspired Visual Direction).
// Every color on screen originates from here.
namespace Obscure
{
    public static class Palette
    {
        // 1. Canonical Spec Design Tokens (Specification Section 2.1)
        public static readonly Color
            DeepCharcoal    = Hex(0x171B24), // Dark neutral: primary panel fill, backdrop, letterbox
            MidnightNavy    = Hex(0x263448), // Dark blue: secondary nested panels, active containers, hero cards
            AntiqueGold     = Hex(0xC5A66A), // Primary accent: 1px filigree borders, selection cursors, gold
            WarmIvory       = Hex(0xE8E2D6), // Primary text: headers, hero names, primary values, active options
            MutedGray       = Hex(0xA5A7AC), // Secondary text: subtitles, formulas, disabled options, descriptions
            SpecCrimson     = Hex(0xC75B5B), // Danger/damage: HP damage, negative stat comparisons, alerts
            Emerald         = Hex(0x76A88B), // Positive/healing: HP recovery, positive stat gains, buffs
            ArcaneViolet    = Hex(0x9B83C7), // Magic/special: MP resource bars, spell ranks, mystic effects
            DarkBorder      = Hex(0x2D3E56), // Subtle inner card border
            OverlayCharcoal = Hex(0x10131A), // Overlays and tooltips (95% opaque dark charcoal)
            AntiqueGoldBright = Hex(0xE8CA90), // Bright gold specular highlight
            AntiqueGoldDim  = Hex(0x8A7246); // Dim gold for inactive filigree

        // 2. Core Legacy / Sprite Art Palette (Harmonized with Spec)
        public static readonly Color
            Void        = Hex(0x0E1117), // Deepest abyss void
            Night       = DeepCharcoal,
            Dusk        = Hex(0x1E2430),
            Shade       = Hex(0x323F52),
            Ash         = MutedGray,
            Bone        = WarmIvory,
            Parchment   = Hex(0xD0C8BA),
            Blood       = Hex(0x632026),
            Crimson     = SpecCrimson,
            Ember       = Hex(0xD47A3B),
            Gold        = AntiqueGold,
            Moss        = Emerald,
            Frost       = Hex(0x8EBECB),
            Arcane      = ArcaneViolet,
            Lilac       = Hex(0xB9A5DE),
            Steel       = Hex(0x7F8796),
            SteelLight  = Hex(0xB8BFCE),
            Wood        = Hex(0x6D4E35),
            WoodDark    = Hex(0x432F20),
            Skin        = Hex(0xD4A383),
            Abyss       = Hex(0x3DB8A8),

            // Modern UI Aliases
            GoldBright    = AntiqueGoldBright,
            GoldDim       = AntiqueGoldDim,
            MossBright    = Emerald,
            FrostBright   = Hex(0xB0DEEA),
            CrimsonBright = Hex(0xE06868),
            PanelDark     = DeepCharcoal,
            PanelEdge     = DarkBorder,
            CardInner     = MidnightNavy,
            BorderBright  = DarkBorder,
            BorderGold    = AntiqueGold;

        public static Color Key(char k) => k switch
        {
            'k' => Void, 'D' => Dusk, 'd' => Shade, 'a' => Ash,
            'g' => Steel, 'G' => SteelLight, 'b' => Bone, 'w' => Bone, 's' => Skin,
            'r' => Crimson, 'R' => Blood, 'o' => Ember, 'y' => Gold, 'Y' => GoldBright,
            'e' => Moss, 'E' => MossBright, 'f' => Frost, 'F' => FrostBright,
            'p' => Arcane, 'P' => Lilac, 'n' => Wood, 'N' => WoodDark, 'c' => Abyss, 'B' => CrimsonBright,
            _ => Color.clear,
        };

        public static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        public static Color Hex(int rgb, byte alpha) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha);
    }
}
