using System;

namespace Obscure
{
    // Classification of status effects per GDD Section 11 & 12.
    public enum StatusCategory
    {
        Buff = 0,
        Debuff = 1,
        DamageOverTime = 2,
        CrowdControl = 3,
        MentalEffect = 4
    }

    // Instance stacking rules per GDD Section 8:
    // - Default for GDD status effects is Independent (new applications do NOT refresh or extend old instances; each instance tracks duration independently).
    // - RefreshDuration / MaxMagnitude are supported for legacy buffs/debuffs (e.g. Broken, AttackUp).
    public enum StatusStackingRule
    {
        Independent = 0,
        RefreshDuration = 1,
        MaxMagnitude = 2,
        NoStack = 3
    }

    // Trigger timing for status effects.
    public enum StatusTickTiming
    {
        TurnStart = 0,
        TurnEnd = 1,
        Immediate = 2
    }
}

