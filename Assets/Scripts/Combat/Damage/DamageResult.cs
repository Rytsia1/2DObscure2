using System;
using System.Collections.Generic;

namespace Obscure
{
    // Detailed breakdown of a single DamageComponent within one hit (Prompt Section 4, 10, 14).
    // Preserves exact decimal (unrounded) damage so multi-type components are summed before per-hit rounding.
    [Serializable]
    public struct ComponentDamageResult
    {
        public DamageType damageType;
        public ElementType elementType;
        public float baseDamage;
        public float afterBonusDamage;
        public float afterQteDamage;
        public bool isCriticalHit;
        public float afterCriticalDamage;
        public float afterDefenseDamage;
        public float afterDamageTakenDamage;
        public AffinityType rawAffinity;
        public AffinityType effectiveAffinity;
        public bool nullBreakApplied;
        public bool wasNullified;
        public float finalElementalResistance;
        public float unroundedDamage;
    }

    // Breakdown of a single hit (which may contain 1 or more DamageComponents, Prompt Section 10 & 11).
    [Serializable]
    public class HitDamageResult
    {
        public int hitIndex;
        public bool rolledCriticalHit;
        public bool anyComponentCriticalHit;
        public List<ComponentDamageResult> componentResults = new List<ComponentDamageResult>();
        public float unroundedTotalDamage;
        public int finalHitDamage;
        public bool allComponentsNullified;
    }

    // Complete result of executing a single-hit or multi-hit damage calculation (Prompt Section 11 & 14).
    [Serializable]
    public class DamageResult
    {
        public List<HitDamageResult> hits = new List<HitDamageResult>();
        public int totalDamage;
        public int hitCount;
        public int criticalHitCount;
        public float qteMultiplierUsed;

        public HitDamageResult FirstHit => hits.Count > 0 ? hits[0] : null;
    }
}

