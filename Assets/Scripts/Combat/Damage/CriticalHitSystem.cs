using System;
using UnityEngine;

namespace Obscure
{
    // Critical Hit configuration and rules (Prompt Section 7 & GDD Section 2.3).
    // Critical Hits apply ONLY to direct Physical Damage.
    [Serializable]
    public struct CriticalHitConfig
    {
        public const float BaseCriticalChance = 0.05f; // 5%
        public const float BaseCriticalDamage = 1.5f;  // 150%
        public const float CriticalChanceCap = 0.95f;  // 95%
        public const float CriticalDamageCap = 3.0f;   // 300%

        public float bonusCriticalChance;
        public float bonusCriticalDamage;

        public float FinalCriticalChance =>
            Mathf.Clamp(BaseCriticalChance + bonusCriticalChance, 0f, CriticalChanceCap);

        public float FinalCriticalDamage =>
            Mathf.Clamp(BaseCriticalDamage + bonusCriticalDamage, 1f, CriticalDamageCap);

        public static CriticalHitConfig Default => new CriticalHitConfig
        {
            bonusCriticalChance = 0f,
            bonusCriticalDamage = 0f
        };

        public static CriticalHitConfig FromTotals(float totalCritChance, float totalCritDamage = BaseCriticalDamage) =>
            new CriticalHitConfig
            {
                bonusCriticalChance = totalCritChance - BaseCriticalChance,
                bonusCriticalDamage = totalCritDamage - BaseCriticalDamage
            };
    }

    public static class CriticalHitSystem
    {
        public const float BaseCriticalChance = CriticalHitConfig.BaseCriticalChance;
        public const float BaseCriticalDamage = CriticalHitConfig.BaseCriticalDamage;
        public const float CriticalChanceCap = CriticalHitConfig.CriticalChanceCap;
        public const float CriticalDamageCap = CriticalHitConfig.CriticalDamageCap;

        // Prompt Section 7.1: Critical Hits apply ONLY to direct Physical Damage (not Elemental, Almighty, or DoT).
        public static bool CanCriticalHit(DamageType damageType, bool isDamageOverTime = false) =>
            !isDamageOverTime && damageType == DamageType.Physical;

        public static float ClampCriticalChance(float rawChance) =>
            Mathf.Clamp(rawChance, 0f, CriticalChanceCap);

        public static float ClampCriticalDamage(float rawMultiplier) =>
            Mathf.Clamp(rawMultiplier, 1f, CriticalDamageCap);

        // Evaluates whether a single hit crits given a roll in [0, 1).
        public static bool EvaluateRoll(float rawCritChance, float rollZeroToOne)
        {
            float chance = ClampCriticalChance(rawCritChance);
            if (chance <= 0f) return false;
            return rollZeroToOne < chance;
        }

        public static bool RollCriticalHit(float rawCritChance, Func<float> rngZeroToOne = null)
        {
            float roll = rngZeroToOne != null ? rngZeroToOne() : UnityEngine.Random.value;
            return EvaluateRoll(rawCritChance, roll);
        }
    }
}

