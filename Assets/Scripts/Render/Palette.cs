using UnityEngine;

// 2DObscure Design System Palette ("Starlit Reliquary" visual direction).
// Ink-navy grounds, one warm gold accent, element hues reserved for combat meaning.
// Every color on screen originates from here.
namespace Obscure
{
    public static class Palette
    {
        // 0. Starlit Reliquary tokens
        public static readonly Color
            Ink0  = Hex(0x070A14), // void: wells, bar troughs
            Ink1  = Hex(0x0C1122), // ground
            Ink2  = Hex(0x121A31), // panel
            Ink3  = Hex(0x1A2443), // raised
            Ink4  = Hex(0x25315A), // line
            Ink5  = Hex(0x3A4878), // border
            Mist1 = Hex(0xEEF0F8), // primary text
            Mist2 = Hex(0xC3C9DE), // body / small text
            Mist3 = Hex(0x8F98B8), // labels, muted, disabled
            Gold1 = Hex(0xFFE6A3), // focus, pointer, highlights
            Gold2 = Hex(0xF2C25B), // primary accent, headings
            Gold3 = Hex(0xC9932E), // rivets, gold frames
            Gold4 = Hex(0x7A5717), // dim gold
            Ember1 = Hex(0xFFC196), Ember2 = Hex(0xEF8A3C), Ember3 = Hex(0x8A4317), // run-only, temporary
            Hp     = Hex(0x52D18C),
            HpLow  = Hex(0xFF6A55),
            Mp     = Hex(0x5AA2FF),
            Xp     = Hex(0xC79BFF),
            Shield = Hex(0x9FE3FF),
            // Rarity — always paired with a letter badge
            RarityCommon = Hex(0xA7AFC4), RarityUncommon = Hex(0x5FD08F), RarityRare = Hex(0x5AA2FF),
            RarityEpic = Hex(0xB98AFF), RarityLegendary = Hex(0xF5A742), RarityMythic = Hex(0xFF6F8E);

        // 1. Canonical spec roles, mapped onto the Starlit Reliquary tokens
        public static readonly Color
            DeepCharcoal    = Ink2,   // Primary panel fill, backdrop, letterbox
            MidnightNavy    = Ink3,   // Secondary nested panels, active containers, hero cards
            AntiqueGold     = Gold2,  // Primary accent: headings, selection, gold
            WarmIvory       = Mist1,  // Primary text: headers, hero names, primary values, active options
            MutedGray       = Mist3,  // Secondary text: subtitles, formulas, disabled options
            SpecCrimson     = HpLow,  // Danger/damage: HP damage, negative stat comparisons, alerts
            Emerald         = Hp,     // Positive/healing: HP recovery, positive stat gains, buffs
            ArcaneViolet    = Xp,     // Magic/special: spell ranks, mystic effects, affixes
            DarkBorder      = Ink5,   // Panel / card border
            OverlayCharcoal = Ink0,   // Overlays, tooltips, gauge troughs
            AntiqueGoldBright = Gold1, // Bright gold highlight, focus
            AntiqueGoldDim  = Gold4;  // Dim gold for inactive accents

        // 2. Sprite art palette (base hues kept so character/enemy art reads the same; outlines go navy)
        public static readonly Color
            Void        = Ink0,
            Night       = DeepCharcoal,
            Dusk        = Ink3,
            Shade       = Ink4,
            Ash         = Hex(0xA5A7AC),
            Bone        = Hex(0xE8E2D6),
            Parchment   = Hex(0xD0C8BA),
            Blood       = Hex(0x632026),
            Crimson     = Hex(0xC75B5B),
            Ember       = Hex(0xD47A3B),
            Gold        = Hex(0xC5A66A),
            Moss        = Hex(0x76A88B),
            Frost       = Hex(0x8EBECB),
            Arcane      = Hex(0x9B83C7),
            Lilac       = Hex(0xB9A5DE),
            Steel       = Hex(0x7F8796),
            SteelLight  = Hex(0xB8BFCE),
            Wood        = Hex(0x6D4E35),
            WoodDark    = Hex(0x432F20),
            Skin        = Hex(0xD4A383),
            Abyss       = Hex(0x3DB8A8),

            // Modern UI Aliases
            GoldBright    = Gold1,
            GoldDim       = Gold4,
            MossBright    = Hp,
            FrostBright   = Shield,
            CrimsonBright = HpLow,
            PanelDark     = DeepCharcoal,
            PanelEdge     = DarkBorder,
            CardInner     = MidnightNavy,
            BorderBright  = DarkBorder,
            BorderGold    = Gold3;

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
