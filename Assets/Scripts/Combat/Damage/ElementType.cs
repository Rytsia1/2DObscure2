using System;

namespace Obscure
{
    // Supported elements for Elemental Damage, Enemy Elemental Affinity, and Player Elemental Resistance (Prompt Section 3.2).
    // Note: Nature is NOT a damage element (it is a thematic identity for the Druid class).
    public enum ElementType
    {
        None = 0,
        Fire = 1,
        Ice = 2,
        Lightning = 3,
        Earth = 4,
        Wind = 5,
        Water = 6,
        Holy = 7,
        Shadow = 8
    }

    public static class ElementTypes
    {
        public static readonly ElementType[] All =
        {
            ElementType.Fire,
            ElementType.Ice,
            ElementType.Lightning,
            ElementType.Earth,
            ElementType.Wind,
            ElementType.Water,
            ElementType.Holy,
            ElementType.Shadow
        };

        public static bool IsValidElement(this ElementType element) =>
            element != ElementType.None && Enum.IsDefined(typeof(ElementType), element);

        public static string ShortName(this ElementType element) => element switch
        {
            ElementType.Fire => "FIR",
            ElementType.Ice => "ICE",
            ElementType.Lightning => "LIG",
            ElementType.Earth => "ERT",
            ElementType.Wind => "WND",
            ElementType.Water => "WTR",
            ElementType.Holy => "HOL",
            ElementType.Shadow => "SHD",
            _ => "NON"
        };

        // Bridges legacy Element enum values to ElementType.
        public static ElementType FromLegacyElement(Element element) => element switch
        {
            Element.Fire => ElementType.Fire,
            Element.Ice => ElementType.Ice,
            Element.Lightning => ElementType.Lightning,
            Element.Earth => ElementType.Earth,
            Element.Wind => ElementType.Wind,
            Element.Water => ElementType.Water,
            Element.Holy => ElementType.Holy,
            Element.Dark => ElementType.Shadow,
            _ => ElementType.None
        };

        public static Element ToLegacyElement(this ElementType element) => element switch
        {
            ElementType.Fire => Element.Fire,
            ElementType.Ice => Element.Ice,
            ElementType.Lightning => Element.Lightning,
            ElementType.Earth => Element.Earth,
            ElementType.Wind => Element.Wind,
            ElementType.Water => Element.Water,
            ElementType.Holy => Element.Holy,
            ElementType.Shadow => Element.Dark,
            _ => Element.Physical
        };
    }
}

