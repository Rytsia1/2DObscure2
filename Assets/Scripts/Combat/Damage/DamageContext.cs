using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Encapsulates all attacker, defender, skill, QTE, and RNG inputs required by DamageCalculator (Prompt Section 14).
    // Pure C# object with zero MonoBehaviour/scene dependencies for deterministic unit testing.
    public class DamageContext
    {
        // Attacker state
        public int attackerStr;
        public int attackerInt;
        public DamageModifierSet attackerModifiers = new DamageModifierSet();
        public CriticalHitConfig criticalHitConfig = CriticalHitConfig.Default;

        // Defender state
        public float targetPhysicalDefense;
        public DamageModifierSet targetModifiers = new DamageModifierSet();
        public EnemyAffinityProfile targetEnemyAffinity = new EnemyAffinityProfile();
        public NullBreakController targetNullBreak;
        public ElementalResistanceProfile targetElementalResistance = new ElementalResistanceProfile();

        // Skill & execution state
        public IReadOnlyList<DamageComponent> components = Array.Empty<DamageComponent>();
        public int hitCount = 1;
        public QteResult qteResult = QteResult.Good;
        public float? customQteMultiplier;
        public bool isDamageOverTime;
        public DamageRoundingMode roundingMode = DamageRoundingMode.Floor;

        // Deterministic / testable Critical Hit hooks (rolled once per hit)
        public bool? forcedCriticalHit;
        public IReadOnlyList<bool> forcedCriticalHitsPerHit;
        public Func<float> criticalRollProvider;

        public DamageContext() { }

        public DamageContext(
            int attackerStr,
            int attackerInt,
            float targetPhysicalDefense,
            IReadOnlyList<DamageComponent> components,
            int hitCount = 1,
            QteResult qteResult = QteResult.Good)
        {
            this.attackerStr = attackerStr;
            this.attackerInt = attackerInt;
            this.targetPhysicalDefense = targetPhysicalDefense;
            this.components = components ?? Array.Empty<DamageComponent>();
            this.hitCount = Mathf.Max(1, hitCount);
            this.qteResult = qteResult;
        }

        public bool ResolveCriticalHitForHit(int hitIndex)
        {
            if (isDamageOverTime)
                return false;

            if (forcedCriticalHitsPerHit != null && hitIndex >= 0 && hitIndex < forcedCriticalHitsPerHit.Count)
                return forcedCriticalHitsPerHit[hitIndex];

            if (forcedCriticalHit.HasValue)
                return forcedCriticalHit.Value;

            return CriticalHitSystem.RollCriticalHit(criticalHitConfig.FinalCriticalChance, criticalRollProvider);
        }
    }
}

