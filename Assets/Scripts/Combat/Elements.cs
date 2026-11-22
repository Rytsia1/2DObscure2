using System;
using System.Collections.Generic;

namespace Obscure
{
    // Explicit values are pinned for save compatibility (Stage 3a: Arcane = 4 removed).
    public enum Element
    {
        Physical = 0,
        Fire = 1,
        Ice = 2,
        Lightning = 3,
        Holy = 5,
        Dark = 6,
        Earth = 7,
        Wind = 8,
        Water = 9,
        Shadow = Dark,
    }

    public enum Effectiveness { Neutral, Weak, Resist, Immune, Drain, Reflect }

    // Damage multipliers for elemental matchups. The only place these numbers live.
    public static class ElementConfig
    {
        public const float Weak = 1.5f, Resist = 0.5f, Immune = 0f;

        public static Element FromInt(int value) =>
            Enum.IsDefined(typeof(Element), value) ? (Element)value : Element.Physical;

        public static string Short(Element e) => e switch
        {
            Element.Physical => "PHY", Element.Fire => "FIR", Element.Ice => "ICE", Element.Lightning => "LIG",
            Element.Holy => "HOL", Element.Earth => "ERT", Element.Wind => "WND",
            Element.Water => "WTR", _ => "DRK",
        };
    }

    // A unit's elemental profile (party and enemies alike).
    public class Affinities
    {
        public Element[] weak = Array.Empty<Element>(), resist = Array.Empty<Element>(), immune = Array.Empty<Element>();
        public Element[] drain = Array.Empty<Element>(), reflect = Array.Empty<Element>();

        public static readonly Affinities None = new Affinities();

        public Effectiveness Against(Element e) =>
            Array.IndexOf(reflect, e) >= 0 ? Effectiveness.Reflect
            : Array.IndexOf(drain, e) >= 0 ? Effectiveness.Drain
            : Array.IndexOf(immune, e) >= 0 ? Effectiveness.Immune
            : Array.IndexOf(weak, e) >= 0 ? Effectiveness.Weak
            : Array.IndexOf(resist, e) >= 0 ? Effectiveness.Resist
            : Effectiveness.Neutral;

        public static float Multiplier(Effectiveness e) => e switch
        {
            Effectiveness.Weak => ElementConfig.Weak,
            Effectiveness.Resist => ElementConfig.Resist,
            Effectiveness.Immune => ElementConfig.Immune,
            Effectiveness.Drain => 0f,
            Effectiveness.Reflect => 0f,
            _ => 1f,
        };

        // "WK:FIR,HOL RS:ICE" for target tooltips.
        public string Describe()
        {
            var parts = new List<string>();
            void Add(string label, Element[] list)
            {
                if (list != null && list.Length > 0) parts.Add(label + string.Join(",", Array.ConvertAll(list, ElementConfig.Short)));
            }
            Add("WK:", weak);
            Add("RS:", resist);
            Add("IM:", immune);
            Add("DR:", drain);
            Add("RF:", reflect);
            return string.Join(" ", parts);
        }
    }
}
