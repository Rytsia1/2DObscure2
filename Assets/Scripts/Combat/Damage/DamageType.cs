using System;

namespace Obscure
{
    // The three damage categories defined in GDD Section 1 & Prompt Section 3.
    // Note: There is no separate Magical Damage category; spells use Elemental or Almighty Damage.
    public enum DamageType
    {
        Physical = 0,
        Elemental = 1,
        Almighty = 2
    }

    // Configurable final per-hit rounding policy (GDD Section 7 & Prompt Section 15).
    // Floor is the GDD working recommendation; Nearest and Ceiling are supported for configuration.
    public enum DamageRoundingMode
    {
        Floor = 0,
        Nearest = 1,
        Ceiling = 2
    }
}

