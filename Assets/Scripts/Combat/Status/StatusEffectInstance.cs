using System;
using UnityEngine;

namespace Obscure
{
    // Runtime status effect instance on a combatant (Prompt Section 4 & GDD Section 8).
    [Serializable]
    public class StatusEffectInstance
    {
        private static int nextInstanceId = 1;

        public int instanceId;
        public StatusId statusId;
        public Combatant source;
        public Combatant target;
        public int remainingTurns;
        public float magnitude;
        public int appliedSkillLevel = 1;

        // Dynamic DoT fields (GDD Section 10)
        public DamageType dotDamageType;
        public ElementType dotElementType;
        public Ability dotScalingStat;
        public float dotSkillPower;
        public float dotScaling;

        // Contextual metadata
        public Combatant forbiddenTarget; // Charm: target cannot attack this combatant
        public Combatant controller;      // MindControl: controller who dictates actions

        public bool IsExpired => remainingTurns <= 0;
        public StatusEffectDef Definition => StatusEffectDefs.Get(statusId);
        public bool IsMental => StatusInfo.IsMental(statusId);
        public bool IsDoT => StatusInfo.IsDoT(statusId);

        public StatusEffectInstance()
        {
            instanceId = nextInstanceId++;
        }

        public static StatusEffectInstance Create(
            StatusApply apply,
            Combatant source,
            Combatant target,
            int skillLevel = 1)
        {
            var def = StatusEffectDefs.Get(apply.id);
            int duration = apply.turns > 0 ? apply.turns : def.defaultDuration;
            float mag = apply.magnitude != 0f ? apply.magnitude : def.defaultMagnitude;

            var inst = new StatusEffectInstance
            {
                statusId = apply.id,
                source = source,
                target = target,
                remainingTurns = duration,
                magnitude = mag,
                appliedSkillLevel = skillLevel,
                dotDamageType = apply.dotDamageType ?? def.dotDamageType,
                dotElementType = apply.dotElementType ?? def.dotElementType,
                dotScalingStat = apply.dotScalingStat ?? def.dotScalingStat,
                dotSkillPower = apply.dotSkillPower.HasValue ? apply.dotSkillPower.Value : (apply.magnitude > 0f ? apply.magnitude : def.dotSkillPower),
                dotScaling = apply.dotScaling.HasValue ? apply.dotScaling.Value : def.dotScaling,
                forbiddenTarget = apply.id == StatusId.Charm ? source : null,
                controller = apply.id == StatusId.MindControl ? source : null
            };
            return inst;
        }

        // Calculates dynamic DoT damage for this instance at the current moment (GDD Section 10).
        // Uses source stats/modifiers against target defense/affinity/resistances via DamageCalculator.
        public int CalculateDynamicDoTDamage(DamageRoundingMode roundingMode = DamageRoundingMode.Floor)
        {
            if (!IsDoT) return 0;

            if (source == null)
            {
                return (int)Math.Max(1, Math.Round(magnitude > 0f ? magnitude : dotSkillPower));
            }

            int attackerStr = source?.unit?.FinalScores.str ?? 10;
            int attackerInt = source?.unit?.FinalScores.intel ?? 10;

            var comp = dotDamageType switch
            {
                DamageType.Elemental => DamageComponent.Elemental(dotElementType.IsValidElement() ? dotElementType : ElementType.Fire, dotSkillPower, dotScaling),
                DamageType.Almighty => DamageComponent.Almighty(dotSkillPower, dotScaling),
                _ => DamageComponent.Physical(dotSkillPower, dotScaling)
            };

            var context = new DamageContext
            {
                attackerStr = attackerStr,
                attackerInt = attackerInt,
                attackerModifiers = source?.DamageModifiers ?? new DamageModifierSet(),
                criticalHitConfig = CriticalHitConfig.Default, // DoTs do not crit
                targetPhysicalDefense = target != null ? target.Def : 0f,
                targetModifiers = target?.DamageModifiers ?? new DamageModifierSet(),
                targetEnemyAffinity = target?.AffinityProfile ?? new EnemyAffinityProfile(),
                targetNullBreak = target?.NullBreak,
                targetElementalResistance = target?.ElementalResistances ?? new ElementalResistanceProfile(),
                components = new[] { comp },
                hitCount = 1,
                isDamageOverTime = true, // Forces QTE = 1.0f and disables Critical Hit (GDD Section 10)
                roundingMode = roundingMode
            };

            var result = DamageCalculator.Calculate(context);
            return result.totalDamage;
        }
    }
}
