using UnityEngine;

// 2DObscure Design System Palette ("Starlit Reliquary" visual direction from FELNA — UI System).
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
            Ok     = Hex(0x52D18C),
            Err    = Hex(0xFF6A55),
            Warn   = Hex(0xF2C25B),

            // Elements (hue + 2-letter code)
            ElementFire      = Hex(0xFF7A45), // FI
            ElementIce       = Hex(0x8FDCF7), // IC
            ElementLightning = Hex(0xF5E04A), // LI
            ElementEarth     = Hex(0xC39A5F), // EA
            ElementWind      = Hex(0x86E6B0), // WI
            ElementWater     = Hex(0x4D9BFF), // WA
            ElementHoly      = Hex(0xFFF1B8), // HO
            ElementShadow    = Hex(0xA77CF5), // SH
            ElementPhysical  = Hex(0xC9CED9), // PH
            ElementAll       = Hex(0xFF7CBF), // AL

            // Rarity — always paired with a letter badge
            RarityCommon = Hex(0xA7AFC4), RarityUncommon = Hex(0x5FD08F), RarityRare = Hex(0x5AA2FF),
            RarityEpic = Hex(0xB98AFF), RarityLegendary = Hex(0xF5A742), RarityMythic = Hex(0xFF6F8E),

            // Class Accents
            ClassFighter       = Hex(0xD9634C),
            ClassBarbarian     = Hex(0xC2452F),
            ClassMartialArtist = Hex(0xE89A3C),
            ClassRogue         = Hex(0x7B6BD6),
            ClassRanger        = Hex(0x4FAE6A),
            ClassCleric        = Hex(0xE7CF72),
            ClassWizard        = Hex(0x4D8FF0),
            ClassWarlock       = Hex(0xA24FD0),
            ClassDruid         = Hex(0x6FBF52);

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

        public static Color ForHp(int cur, int max) => (max > 0 && cur <= max * 0.25f) ? HpLow : Hp;

        public static Color ForElement(Element el) => el switch
        {
            Element.Fire => ElementFire,
            Element.Ice => ElementIce,
            Element.Lightning => ElementLightning,
            Element.Earth => ElementEarth,
            Element.Wind => ElementWind,
            Element.Water => ElementWater,
            Element.Holy => ElementHoly,
            Element.Dark or Element.Shadow => ElementShadow,
            Element.Arcane => ElementAll,
            _ => ElementPhysical,
        };

        public static Color ForElement(string code) => (code ?? "").ToUpperInvariant() switch
        {
            "FI" => ElementFire,
            "IC" => ElementIce,
            "LI" => ElementLightning,
            "EA" => ElementEarth,
            "WI" => ElementWind,
            "WA" => ElementWater,
            "HO" => ElementHoly,
            "SH" or "DK" => ElementShadow,
            "AL" or "AR" => ElementAll,
            _ => ElementPhysical,
        };

        public static string ElementCode(Element el) => el switch
        {
            Element.Fire => "FI",
            Element.Ice => "IC",
            Element.Lightning => "LI",
            Element.Earth => "EA",
            Element.Wind => "WI",
            Element.Water => "WA",
            Element.Holy => "HO",
            Element.Dark or Element.Shadow => "SH",
            Element.Arcane => "AL",
            _ => "PH",
        };

        public static Color ForRarity(EquipmentRarity r) => r switch
        {
            EquipmentRarity.Common => RarityCommon,
            EquipmentRarity.Uncommon => RarityUncommon,
            EquipmentRarity.Rare => RarityRare,
            EquipmentRarity.Epic => RarityEpic,
            EquipmentRarity.Legendary => RarityLegendary,
            EquipmentRarity.Mythic => RarityMythic,
            _ => RarityCommon,
        };

        public static string RarityBadge(EquipmentRarity r) => r switch
        {
            EquipmentRarity.Common => "C",
            EquipmentRarity.Uncommon => "U",
            EquipmentRarity.Rare => "R",
            EquipmentRarity.Epic => "E",
            EquipmentRarity.Legendary => "L",
            EquipmentRarity.Mythic => "M",
            _ => "C",
        };

        public static Color ForClass(string classId) => (classId?.ToLowerInvariant()) switch
        {
            "fighter" => ClassFighter,
            "barbarian" => ClassBarbarian,
            "martial_artist" or "martialartist" => ClassMartialArtist,
            "rogue" => ClassRogue,
            "ranger" => ClassRanger,
            "cleric" => ClassCleric,
            "wizard" or "mage" => ClassWizard,
            "warlock" => ClassWarlock,
            "druid" => ClassDruid,
            _ => WarmIvory,
        };

        public static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        public static Color Hex(int rgb, byte alpha) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha);
    }
}
