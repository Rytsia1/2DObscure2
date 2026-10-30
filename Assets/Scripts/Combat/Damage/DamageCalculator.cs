using System;
using UnityEngine;

namespace Obscure
{
    // Centralized, pure-C# damage calculation engine (Prompt Sections 3-15 & GDD Sections 1-7).
    // Strictly enforces the exact calculation order for Physical, Elemental, and Almighty damage,
    // multi-type decimal summation, per-hit rounding, and independent multi-hit execution.
    public static class DamageCalculator
    {
        // Prompt Section 6.1: QTE Multipliers for direct damage skills.
        public const float QtePerfectMultiplier = 1.5f;
        public const float QteGoodMultiplier = 1.0f;
        public const float QteMissMultiplier = 0.5f;
        public const float DotQteMultiplier = 1.0f;

        public static float GetQteMultiplier(QteResult qteResult, bool isDamageOverTime = false)
        {
            if (isDamageOverTime) return DotQteMultiplier;
            return qteResult switch
            {
                QteResult.Perfect => QtePerfectMultiplier,
                QteResult.Good => QteGoodMultiplier,
                QteResult.Miss => QteMissMultiplier,
                _ => QteGoodMultiplier
            };
        }

        public static int RoundDamage(float unroundedDamage, DamageRoundingMode roundingMode = DamageRoundingMode.Floor)
        {
            float clamped = Mathf.Max(0f, unroundedDamage);
            // Guard against tiny floating-point inaccuracies right below an integer boundary (e.g., 59.999996f -> 60).
            const float epsilon = 1e-4f;
            return roundingMode switch
            {
                DamageRoundingMode.Floor => Mathf.FloorToInt(clamped + epsilon),
                DamageRoundingMode.Nearest => Mathf.RoundToInt(clamped),
                DamageRoundingMode.Ceiling => Mathf.CeilToInt(clamped - epsilon),
                _ => Mathf.FloorToInt(clamped + epsilon)
            };
        }

        public static DamageResult Calculate(DamageContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            int hitCount = Mathf.Max(1, context.hitCount);
            float qteMultiplier = context.isDamageOverTime
                ? DotQteMultiplier
                : (context.customQteMultiplier ?? GetQteMultiplier(context.qteResult, false));

            var result = new DamageResult
            {
                hitCount = hitCount,
                qteMultiplierUsed = qteMultiplier
            };

            for (int hitIdx = 0; hitIdx < hitCount; hitIdx++)
            {
                var hitResult = CalculateSingleHit(context, hitIdx, qteMultiplier);
                result.hits.Add(hitResult);
                result.totalDamage += hitResult.finalHitDamage;
                if (hitResult.anyComponentCriticalHit)
                    result.criticalHitCount++;
            }

            return result;
        }

        public static HitDamageResult CalculateSingleHit(DamageContext context, int hitIndex, float qteMultiplier)
        {
            bool rolledCrit = context.ResolveCriticalHitForHit(hitIndex);
            var hitResult = new HitDamageResult
            {
                hitIndex = hitIndex,
                rolledCriticalHit = rolledCrit
            };

            var components = context.components;
            if (components == null || components.Count == 0)
            {
                hitResult.unroundedTotalDamage = 0f;
                hitResult.finalHitDamage = 0;
                hitResult.allComponentsNullified = false;
                return hitResult;
            }

            float unroundedSum = 0f;
            bool allNullified = true;

            for (int i = 0; i < components.Count; i++)
            {
                var comp = components[i];
                ComponentDamageResult compResult = comp.damageType switch
                {
                    DamageType.Physical => CalculatePhysicalComponent(context, comp, qteMultiplier, rolledCrit),
                    DamageType.Elemental => CalculateElementalComponent(context, comp, qteMultiplier),
                    DamageType.Almighty => CalculateAlmightyComponent(context, comp, qteMultiplier),
                    _ => default
                };

                if (compResult.isCriticalHit)
                    hitResult.anyComponentCriticalHit = true;

                if (!compResult.wasNullified)
                    allNullified = false;

                unroundedSum += compResult.unroundedDamage;
                hitResult.componentResults.Add(compResult);
            }

            hitResult.allComponentsNullified = allNullified;
            hitResult.unroundedTotalDamage = Mathf.Max(0f, unroundedSum);
            // Prompt Section 10.2 & 11.1: Preserve decimal values across components and round once per hit.
            hitResult.finalHitDamage = RoundDamage(hitResult.unroundedTotalDamage, context.roundingMode);
            return hitResult;
        }

        // Prompt Section 4.1: Physical Damage Order
        // 1. BaseDamage = SkillPower + (STR * Scaling)
        // 2. * PhysicalDamageBonus
        // 3. * AllDamageBonus
        // 4. * QTE Multiplier
        // 5. Roll Critical Hit & if critical: * CriticalDamage
        // 6. Subtract PhysicalDefense: Mathf.Max(0f, DamageBeforeDefense - PhysicalDefense) (0 is valid; no min 1)
        // 7. * PhysicalDamageTaken * AllDamageTaken
        // 8. * PhysicalEnemyAffinity
        // 9. Nullification rules (0 unless Null Break is active for Physical)
        public static ComponentDamageResult CalculatePhysicalComponent(
            DamageContext context,
            DamageComponent component,
            float qteMultiplier,
            bool hitRolledCrit)
        {
            var attackerMods = context.attackerModifiers ?? new DamageModifierSet();
            var targetMods = context.targetModifiers ?? new DamageModifierSet();
            var affinityProfile = context.targetEnemyAffinity ?? new EnemyAffinityProfile();

            // 1. Base Damage
            float baseDamage = component.CalculateBaseDamage(context.attackerStr, context.attackerInt);

            // 2 & 3. Physical Damage Bonus * All Damage Bonus (multiplicative)
            float physBonus = attackerMods.GetPhysicalDamageBonus();
            float allBonus = attackerMods.GetAllDamageBonus();
            float afterBonus = baseDamage * physBonus * allBonus;

            // 4. QTE Multiplier
            float afterQte = afterBonus * qteMultiplier;

            // 5. Critical Hit (Physical only, applied after QTE and before Physical Defense)
            bool isCrit = !context.isDamageOverTime && hitRolledCrit;
            float critMultiplier = isCrit ? context.criticalHitConfig.FinalCriticalDamage : 1f;
            float afterCrit = afterQte * critMultiplier;

            // 6. Subtract Physical Defense (flat subtraction, clamped at 0; no minimum 1 damage)
            float afterDefense = Mathf.Max(0f, afterCrit - Mathf.Max(0f, context.targetPhysicalDefense));

            // 7. Physical Damage Taken * All Damage Taken (multiplicative)
            float physTaken = targetMods.GetPhysicalDamageTaken();
            float allTaken = targetMods.GetAllDamageTaken();
            float afterDamageTaken = afterDefense * physTaken * allTaken;

            // 8 & 9. Physical Enemy Affinity & Null Break / Nullification
            AffinityType rawAffinity = affinityProfile.GetRawPhysicalAffinity();
            AffinityType effectiveAffinity = affinityProfile.GetEffectiveAffinity(
                DamageType.Physical,
                ElementType.None,
                context.targetNullBreak);

            bool nullBreakApplied = rawAffinity == AffinityType.Nullification &&
                                    effectiveAffinity == AffinityType.Normal;
            bool wasNullified = effectiveAffinity == AffinityType.Nullification;

            float unrounded = wasNullified
                ? 0f
                : afterDamageTaken * effectiveAffinity.GetMultiplier();

            return new ComponentDamageResult
            {
                damageType = DamageType.Physical,
                elementType = ElementType.None,
                baseDamage = baseDamage,
                afterBonusDamage = afterBonus,
                afterQteDamage = afterQte,
                isCriticalHit = isCrit,
                afterCriticalDamage = afterCrit,
                afterDefenseDamage = afterDefense,
                afterDamageTakenDamage = afterDamageTaken,
                rawAffinity = rawAffinity,
                effectiveAffinity = effectiveAffinity,
                nullBreakApplied = nullBreakApplied,
                wasNullified = wasNullified,
                finalElementalResistance = 0f,
                unroundedDamage = Mathf.Max(0f, unrounded)
            };
        }

        // Prompt Section 4.2: Elemental Damage Order
        // 1. BaseDamage = SkillPower + (INT * Scaling)
        // 2. * ElementalDamageBonus
        // 3. * ElementSpecificDamageBonus
        // 4. * AllDamageBonus
        // 5. * QTE Multiplier (cannot Critical Hit; ignores PhysicalDefense)
        // 6. * ElementalDamageTaken
        // 7. * ElementSpecificDamageTaken * AllDamageTaken
        // 8. * EnemyElementalAffinity
        // 9. * (1 - TargetElementalResistance) (capped at 75%)
        // 10. Nullification rules (0 unless Null Break is active for that element)
        public static ComponentDamageResult CalculateElementalComponent(
            DamageContext context,
            DamageComponent component,
            float qteMultiplier)
        {
            var element = component.elementType;
            var attackerMods = context.attackerModifiers ?? new DamageModifierSet();
            var targetMods = context.targetModifiers ?? new DamageModifierSet();
            var affinityProfile = context.targetEnemyAffinity ?? new EnemyAffinityProfile();
            var resistanceProfile = context.targetElementalResistance ?? new ElementalResistanceProfile();

            // 1. Base Damage
            float baseDamage = component.CalculateBaseDamage(context.attackerStr, context.attackerInt);

            // 2, 3, 4. ElementalDamageBonus * ElementSpecificDamageBonus * AllDamageBonus
            float elemBonus = attackerMods.GetElementalDamageBonus();
            float specificBonus = attackerMods.GetElementSpecificDamageBonus(element);
            float allBonus = attackerMods.GetAllDamageBonus();
            float afterBonus = baseDamage * elemBonus * specificBonus * allBonus;

            // 5. QTE Multiplier (Elemental Damage cannot crit and ignores PhysicalDefense)
            float afterQte = afterBonus * qteMultiplier;

            // 6 & 7. ElementalDamageTaken * ElementSpecificDamageTaken * AllDamageTaken
            float elemTaken = targetMods.GetElementalDamageTaken();
            float specificTaken = targetMods.GetElementSpecificDamageTaken(element);
            float allTaken = targetMods.GetAllDamageTaken();
            float afterDamageTaken = afterQte * elemTaken * specificTaken * allTaken;

            // 8, 9, 10. Enemy Elemental Affinity, Target Elemental Resistance, and Nullification
            AffinityType rawAffinity = affinityProfile.GetRawElementalAffinity(element);
            AffinityType effectiveAffinity = affinityProfile.GetEffectiveAffinity(
                DamageType.Elemental,
                element,
                context.targetNullBreak);

            bool nullBreakApplied = rawAffinity == AffinityType.Nullification &&
                                    effectiveAffinity == AffinityType.Normal;
            bool wasNullified = effectiveAffinity == AffinityType.Nullification;

            float finalResistance = resistanceProfile.GetFinalResistance(element);

            float unrounded = wasNullified
                ? 0f
                : afterDamageTaken * effectiveAffinity.GetMultiplier() * (1f - finalResistance);

            return new ComponentDamageResult
            {
                damageType = DamageType.Elemental,
                elementType = element,
                baseDamage = baseDamage,
                afterBonusDamage = afterBonus,
                afterQteDamage = afterQte,
                isCriticalHit = false,
                afterCriticalDamage = afterQte,
                afterDefenseDamage = afterQte,
                afterDamageTakenDamage = afterDamageTaken,
                rawAffinity = rawAffinity,
                effectiveAffinity = effectiveAffinity,
                nullBreakApplied = nullBreakApplied,
                wasNullified = wasNullified,
                finalElementalResistance = finalResistance,
                unroundedDamage = Mathf.Max(0f, unrounded)
            };
        }

        // Prompt Section 4.3: Almighty Damage Order
        // 1. BaseDamage = SkillPower + (INT * Scaling)
        // 2. * AllDamageBonus
        // 3. * QTE Multiplier
        // 4. * AllDamageTaken
        // 5. Ignore PhysicalDefense, EnemyAffinity (including Nullification), ElementalResistance, and Critical Hit.
        public static ComponentDamageResult CalculateAlmightyComponent(
            DamageContext context,
            DamageComponent component,
            float qteMultiplier)
        {
            var attackerMods = context.attackerModifiers ?? new DamageModifierSet();
            var targetMods = context.targetModifiers ?? new DamageModifierSet();

            // 1. Base Damage
            float baseDamage = component.CalculateBaseDamage(context.attackerStr, context.attackerInt);

            // 2. All Damage Bonus only
            float allBonus = attackerMods.GetAllDamageBonus();
            float afterBonus = baseDamage * allBonus;

            // 3. QTE Multiplier
            float afterQte = afterBonus * qteMultiplier;

            // 4. All Damage Taken only
            float allTaken = targetMods.GetAllDamageTaken();
            float afterDamageTaken = afterQte * allTaken;

            return new ComponentDamageResult
            {
                damageType = DamageType.Almighty,
                elementType = ElementType.None,
                baseDamage = baseDamage,
                afterBonusDamage = afterBonus,
                afterQteDamage = afterQte,
                isCriticalHit = false,
                afterCriticalDamage = afterQte,
                afterDefenseDamage = afterQte,
                afterDamageTakenDamage = afterDamageTaken,
                rawAffinity = AffinityType.Normal,
                effectiveAffinity = AffinityType.Normal,
                nullBreakApplied = false,
                wasNullified = false,
                finalElementalResistance = 0f,
                unroundedDamage = Mathf.Max(0f, afterDamageTaken)
            };
        }
    }
}

