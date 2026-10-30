using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    public class DamageSystemTests
    {
        // =====================================================================
        // 16.1 Physical Damage Tests
        // =====================================================================

        [Test]
        public void PhysicalDamage_BaseFormula_UsesSkillPowerPlusStrTimesScaling()
        {
            // BaseDamage = SkillPower (40) + STR (20) * Scaling (1.5) = 40 + 30 = 70
            var ctx = new DamageContext(
                attackerStr: 20,
                attackerInt: 10,
                targetPhysicalDefense: 0f,
                components: new[] { DamageComponent.Physical(skillPower: 40f, scaling: 1.5f) })
            {
                forcedCriticalHit = false,
                qteResult = QteResult.Good
            };

            var res = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(70, res.totalDamage);
            Assert.AreEqual(70f, res.FirstHit.componentResults[0].baseDamage, 0.001f);
        }

        [Test]
        public void PhysicalDamage_FollowsExactCalculationOrder_BonusQteCritDefenseTakenAffinity()
        {
            // 1. BaseDamage = 50 + (20 * 2.0) = 90
            // 2. * PhysicalDamageBonus (1.2) = 108
            // 3. * AllDamageBonus (1.25) = 135
            // 4. * QTE Perfect (1.5) = 202.5
            // 5. * CriticalHit (1.5) = 303.75 (Crit applied BEFORE PhysicalDefense!)
            // 6. - PhysicalDefense (53.75) = 250.0
            // 7. * PhysicalDamageTaken (1.2) * AllDamageTaken (1.1) = 330.0
            // 8. * PhysicalEnemyAffinity Weakness (1.5) = 495.0
            var ctx = new DamageContext(
                attackerStr: 20,
                attackerInt: 10,
                targetPhysicalDefense: 53.75f,
                components: new[] { DamageComponent.Physical(skillPower: 50f, scaling: 2.0f) })
            {
                qteResult = QteResult.Perfect,
                forcedCriticalHit = true
            };
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.PhysicalDamageBonus, 1.2f);
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.AllDamageBonus, 1.25f);
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.PhysicalDamageTaken, 1.2f);
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 1.1f);
            ctx.targetEnemyAffinity.SetPhysicalAffinity(AffinityType.Weakness);

            var res = DamageCalculator.Calculate(ctx);
            var comp = res.FirstHit.componentResults[0];

            Assert.AreEqual(90f, comp.baseDamage, 0.001f);
            Assert.AreEqual(135f, comp.afterBonusDamage, 0.001f);
            Assert.AreEqual(202.5f, comp.afterQteDamage, 0.001f);
            Assert.IsTrue(comp.isCriticalHit);
            Assert.AreEqual(303.75f, comp.afterCriticalDamage, 0.001f);
            Assert.AreEqual(250f, comp.afterDefenseDamage, 0.001f);
            Assert.AreEqual(330f, comp.afterDamageTakenDamage, 0.001f);
            Assert.AreEqual(495, res.totalDamage);
        }

        [Test]
        public void PhysicalDamage_DefenseExceedingDamage_ReducesToZeroWithNoMinimumOne()
        {
            // BaseDamage = 30 + (10 * 1.0) = 40; Defense = 100 -> clamped to 0 (NOT 1).
            // Even with Weakness (1.5x) and DamageTaken (2.0x), 0 * 2.0 * 1.5 = 0.
            var ctx = new DamageContext(
                attackerStr: 10,
                attackerInt: 10,
                targetPhysicalDefense: 100f,
                components: new[] { DamageComponent.Physical(skillPower: 30f, scaling: 1.0f) })
            {
                forcedCriticalHit = false
            };
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.PhysicalDamageTaken, 2.0f);
            ctx.targetEnemyAffinity.SetPhysicalAffinity(AffinityType.Weakness);

            var res = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(0, res.totalDamage);
            Assert.AreEqual(0f, res.FirstHit.componentResults[0].afterDefenseDamage, 0.001f);
        }

        [Test]
        public void PhysicalDamage_InteractsWithPhysicalEnemyAffinityStates()
        {
            DamageContext MakeCtx(AffinityType affinity)
            {
                var c = new DamageContext(
                    attackerStr: 20,
                    attackerInt: 10,
                    targetPhysicalDefense: 0f,
                    components: new[] { DamageComponent.Physical(skillPower: 60f, scaling: 2.0f) })
                {
                    forcedCriticalHit = false
                };
                c.targetEnemyAffinity.SetPhysicalAffinity(affinity);
                return c;
            }

            // Base = 60 + 40 = 100
            Assert.AreEqual(150, DamageCalculator.Calculate(MakeCtx(AffinityType.Weakness)).totalDamage);
            Assert.AreEqual(100, DamageCalculator.Calculate(MakeCtx(AffinityType.Normal)).totalDamage);
            Assert.AreEqual(50, DamageCalculator.Calculate(MakeCtx(AffinityType.Resistant)).totalDamage);
            Assert.AreEqual(0, DamageCalculator.Calculate(MakeCtx(AffinityType.Nullification)).totalDamage);
        }

        // =====================================================================
        // 16.2 Elemental Damage Tests
        // =====================================================================

        [Test]
        public void ElementalDamage_SupportsAllEightElements_AndUsesIntScaling()
        {
            // Prompt Section 3.2: Fire, Ice, Lightning, Earth, Wind, Water, Holy, Shadow.
            Assert.AreEqual(8, ElementTypes.All.Length);

            foreach (var elem in ElementTypes.All)
            {
                var ctx = new DamageContext(
                    attackerStr: 5,
                    attackerInt: 25,
                    targetPhysicalDefense: 999f, // Ignored by Elemental Damage
                    components: new[] { DamageComponent.Elemental(elem, skillPower: 50f, scaling: 2.0f) })
                {
                    forcedCriticalHit = true // Elemental cannot crit even if forcedCriticalHit is true
                };

                var res = DamageCalculator.Calculate(ctx);
                // Base = 50 + (25 * 2.0) = 100
                Assert.AreEqual(100, res.totalDamage, $"Failed for element {elem}");
                Assert.IsFalse(res.FirstHit.anyComponentCriticalHit, $"Elemental {elem} must never crit");
            }
        }

        [Test]
        public void ElementalDamage_FollowsExactCalculationOrder_BonusesQteTakenAffinityResistance()
        {
            // 1. Base = 40 + (30 * 2.0) = 100
            // 2. * ElementalDamageBonus (1.2) = 120
            // 3. * FireDamageBonus (1.25) = 150
            // 4. * AllDamageBonus (1.1) = 165
            // 5. * QTE Perfect (1.5) = 247.5
            // 6. * ElementalDamageTaken (1.2) = 297
            // 7. * FireDamageTaken (1.5) * AllDamageTaken (1.1) = 490.05
            // 8. * EnemyElementalAffinity Weakness (1.5) = 735.075
            // 9. * (1 - TargetFireResistance 0.20) = 735.075 * 0.8 = 588.06 -> Floor = 588
            var ctx = new DamageContext(
                attackerStr: 10,
                attackerInt: 30,
                targetPhysicalDefense: 500f, // ignored
                components: new[] { DamageComponent.Elemental(ElementType.Fire, skillPower: 40f, scaling: 2.0f) })
            {
                qteResult = QteResult.Perfect,
                forcedCriticalHit = true // ignored for Elemental
            };

            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.ElementalDamageBonus, 1.2f);
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.ElementSpecificDamageBonus, 1.25f, ElementType.Fire);
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.ElementSpecificDamageBonus, 2.0f, ElementType.Ice); // Wrong element, must not apply!
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.AllDamageBonus, 1.1f);

            ctx.targetModifiers.AddMultiplier(DamageModifierScope.ElementalDamageTaken, 1.2f);
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.ElementSpecificDamageTaken, 1.5f, ElementType.Fire);
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 1.1f);

            ctx.targetEnemyAffinity.SetElementalAffinity(ElementType.Fire, AffinityType.Weakness);
            ctx.targetElementalResistance.SetSources(ElementType.Fire, equipment: 0.20f);

            var res = DamageCalculator.Calculate(ctx);
            var comp = res.FirstHit.componentResults[0];

            Assert.AreEqual(100f, comp.baseDamage, 0.001f);
            Assert.AreEqual(165f, comp.afterBonusDamage, 0.001f);
            Assert.AreEqual(247.5f, comp.afterQteDamage, 0.001f);
            Assert.IsFalse(comp.isCriticalHit);
            Assert.AreEqual(490.05f, comp.afterDamageTakenDamage, 0.01f);
            Assert.AreEqual(0.20f, comp.finalElementalResistance, 0.001f);
            Assert.AreEqual(588.06f, comp.unroundedDamage, 0.01f);
            Assert.AreEqual(588, res.totalDamage);
        }

        // =====================================================================
        // 16.3 Almighty Damage Tests
        // =====================================================================

        [Test]
        public void AlmightyDamage_IgnoresDefenseAffinityNullificationResistanceAndCrit_AppliesAllBonusTakenAndQte()
        {
            // Base = 60 + (20 * 2.0) = 100
            // * AllDamageBonus (1.2) = 120
            // * QTE Perfect (1.5) = 180
            // * AllDamageTaken (1.25) = 225
            // Physical/Elemental bonuses, PhysicalDefense, Affinity (even Nullification), ElementalResistance, and Crit must all be ignored!
            var ctx = new DamageContext(
                attackerStr: 99,
                attackerInt: 20,
                targetPhysicalDefense: 9999f,
                components: new[] { DamageComponent.Almighty(skillPower: 60f, scaling: 2.0f) })
            {
                qteResult = QteResult.Perfect,
                forcedCriticalHit = true
            };

            // Add Physical & Elemental bonuses that Almighty must ignore:
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.PhysicalDamageBonus, 3.0f);
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.ElementalDamageBonus, 3.0f);
            ctx.attackerModifiers.AddMultiplier(DamageModifierScope.AllDamageBonus, 1.2f);

            ctx.targetModifiers.AddMultiplier(DamageModifierScope.PhysicalDamageTaken, 3.0f);
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.ElementalDamageTaken, 3.0f);
            ctx.targetModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 1.25f);

            // Set enemy to Nullify everything:
            ctx.targetEnemyAffinity.SetPhysicalAffinity(AffinityType.Nullification);
            foreach (var elem in ElementTypes.All)
            {
                ctx.targetEnemyAffinity.SetElementalAffinity(elem, AffinityType.Nullification);
                ctx.targetElementalResistance.SetSources(elem, equipment: 0.75f);
            }

            var res = DamageCalculator.Calculate(ctx);
            var comp = res.FirstHit.componentResults[0];

            Assert.IsFalse(comp.isCriticalHit);
            Assert.IsFalse(comp.wasNullified);
            Assert.AreEqual(225, res.totalDamage);
        }

        // =====================================================================
        // 16.4 Multi-Type Damage Tests
        // =====================================================================

        [Test]
        public void MultiTypeDamage_PartialNullification_OnlyZeroesNullifiedComponent()
        {
            // Skill has Physical (Base = 50 + 20*1 = 70, after Def 10 = 60) + Fire (Base = 40 + 20*1 = 60).
            // Target has Fire = Nullification, Physical = Normal.
            // Expected: Fire component = 0, Physical component = 60, Total = 60.
            var components = new[]
            {
                DamageComponent.Physical(skillPower: 50f, scaling: 1.0f),
                DamageComponent.Elemental(ElementType.Fire, skillPower: 40f, scaling: 1.0f)
            };

            var ctx = new DamageContext(
                attackerStr: 20,
                attackerInt: 20,
                targetPhysicalDefense: 10f,
                components: components)
            {
                forcedCriticalHit = false
            };
            ctx.targetEnemyAffinity.SetElementalAffinity(ElementType.Fire, AffinityType.Nullification);

            var res = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(60f, res.FirstHit.componentResults[0].unroundedDamage, 0.001f);
            Assert.IsFalse(res.FirstHit.componentResults[0].wasNullified);
            Assert.AreEqual(0f, res.FirstHit.componentResults[1].unroundedDamage, 0.001f);
            Assert.IsTrue(res.FirstHit.componentResults[1].wasNullified);
            Assert.IsFalse(res.FirstHit.allComponentsNullified);
            Assert.AreEqual(60, res.totalDamage);
        }

        [Test]
        public void MultiTypeDamage_CriticalHit_OnlyAppliesToPhysicalComponent()
        {
            // Physical Base = 100, Crit (1.5x) = 150, Def 30 -> 120.
            // Fire Base = 100, Crit must NOT apply -> 100.
            // Total = 120 + 100 = 220.
            var components = new[]
            {
                DamageComponent.Physical(skillPower: 80f, scaling: 1.0f),
                DamageComponent.Elemental(ElementType.Fire, skillPower: 80f, scaling: 1.0f)
            };

            var ctx = new DamageContext(
                attackerStr: 20,
                attackerInt: 20,
                targetPhysicalDefense: 30f,
                components: components)
            {
                forcedCriticalHit = true
            };

            var res = DamageCalculator.Calculate(ctx);
            Assert.IsTrue(res.FirstHit.componentResults[0].isCriticalHit);
            Assert.AreEqual(120f, res.FirstHit.componentResults[0].unroundedDamage, 0.001f);
            Assert.IsFalse(res.FirstHit.componentResults[1].isCriticalHit);
            Assert.AreEqual(100f, res.FirstHit.componentResults[1].unroundedDamage, 0.001f);
            Assert.AreEqual(220, res.totalDamage);
        }

        [Test]
        public void MultiTypeDamage_PreservesDecimalValuesAcrossComponents_BeforeRoundingOncePerHit()
        {
            // Physical component: Base = 12.6
            // Fire component: Base = 15.6
            // Unrounded sum = 28.2 -> Floor = 28 (if rounded prematurely per component, 12 + 15 = 27).
            var components = new[]
            {
                DamageComponent.Physical(skillPower: 12.6f, scaling: 0f),
                DamageComponent.Elemental(ElementType.Fire, skillPower: 15.6f, scaling: 0f)
            };

            var ctx = new DamageContext(
                attackerStr: 10,
                attackerInt: 10,
                targetPhysicalDefense: 0f,
                components: components)
            {
                forcedCriticalHit = false,
                roundingMode = DamageRoundingMode.Floor
            };

            var res = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(28.2f, res.FirstHit.unroundedTotalDamage, 0.001f);
            Assert.AreEqual(28, res.FirstHit.finalHitDamage);
            Assert.AreEqual(28, res.totalDamage);
        }

        // =====================================================================
        // 16.5 Multi-Hit Damage Tests
        // =====================================================================

        [Test]
        public void MultiHitDamage_RollsCriticalHitIndependentlyPerHit_AndRoundsEachHitIndependently()
        {
            // 3-hit Physical skill:
            // Base = 25.4, Def = 5 -> Non-crit unrounded = 20.4 -> Floor per hit = 20.
            // Crit (1.5x): 25.4 * 1.5 = 38.1, - Def 5 = 33.1 -> Floor per hit = 33.
            // Hit 0: Crit (33), Hit 1: No Crit (20), Hit 2: Crit (33) -> Total = 86.
            var ctx = new DamageContext(
                attackerStr: 0,
                attackerInt: 0,
                targetPhysicalDefense: 5f,
                components: new[] { DamageComponent.Physical(skillPower: 25.4f, scaling: 0f) },
                hitCount: 3)
            {
                forcedCriticalHitsPerHit = new List<bool> { true, false, true },
                roundingMode = DamageRoundingMode.Floor
            };

            var res = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(3, res.hits.Count);
            Assert.AreEqual(2, res.criticalHitCount);
            Assert.AreEqual(33, res.hits[0].finalHitDamage);
            Assert.AreEqual(20, res.hits[1].finalHitDamage);
            Assert.AreEqual(33, res.hits[2].finalHitDamage);
            Assert.AreEqual(86, res.totalDamage);
        }

        // =====================================================================
        // 16.6 Nullification and Null Break Tests
        // =====================================================================

        [Test]
        public void NullBreak_ConvertsTargetedNullificationToNormal_WithoutAffectingWeaknessResistantOrOtherElements()
        {
            var profile = new EnemyAffinityProfile();
            profile.SetElementalAffinity(ElementType.Fire, AffinityType.Nullification);
            profile.SetElementalAffinity(ElementType.Ice, AffinityType.Nullification);
            profile.SetElementalAffinity(ElementType.Lightning, AffinityType.Weakness);
            profile.SetElementalAffinity(ElementType.Earth, AffinityType.Resistant);

            var nullBreak = new NullBreakController();
            Assert.IsTrue(nullBreak.TryApplyElemental(ElementType.Fire, skillLevel: 2));

            // Fire Nullification -> Normal (1.0x)
            Assert.AreEqual(AffinityType.Normal, profile.GetEffectiveAffinity(DamageType.Elemental, ElementType.Fire, nullBreak));
            Assert.AreEqual(1.0f, profile.GetEffectiveMultiplier(DamageType.Elemental, ElementType.Fire, nullBreak), 0.001f);

            // Ice Nullification (untargeted) remains Nullification (0.0x)
            Assert.AreEqual(AffinityType.Nullification, profile.GetEffectiveAffinity(DamageType.Elemental, ElementType.Ice, nullBreak));

            // Even if Null Break is applied to an element with Weakness or Resistant, they remain unchanged:
            var weakNullBreak = new NullBreakController();
            weakNullBreak.TryApplyElemental(ElementType.Lightning, skillLevel: 1);
            Assert.AreEqual(AffinityType.Weakness, profile.GetEffectiveAffinity(DamageType.Elemental, ElementType.Lightning, weakNullBreak));

            var resistNullBreak = new NullBreakController();
            resistNullBreak.TryApplyElemental(ElementType.Earth, skillLevel: 1);
            Assert.AreEqual(AffinityType.Resistant, profile.GetEffectiveAffinity(DamageType.Elemental, ElementType.Earth, resistNullBreak));
        }

        [Test]
        public void NullBreak_DoesNotStackRefreshOrExtendWhileActive_AndScalesDurationBySkillLevel()
        {
            // Duration rules: Lv1 = 1 turn, Lv2 = 2 turns, Lv3 = 3 turns.
            Assert.AreEqual(1, NullBreakController.GetDurationForSkillLevel(1));
            Assert.AreEqual(2, NullBreakController.GetDurationForSkillLevel(2));
            Assert.AreEqual(3, NullBreakController.GetDurationForSkillLevel(3));

            var nb = new NullBreakController();
            Assert.IsTrue(nb.TryApplyElemental(ElementType.Fire, skillLevel: 2));
            Assert.AreEqual(2, nb.RemainingTurns);

            // Attempting to apply again (same element or different element, higher level) while active MUST fail:
            Assert.IsFalse(nb.TryApplyElemental(ElementType.Fire, skillLevel: 3));
            Assert.IsFalse(nb.TryApplyElemental(ElementType.Ice, skillLevel: 3));
            Assert.IsFalse(nb.TryApplyPhysical(skillLevel: 3));
            Assert.AreEqual(2, nb.RemainingTurns);
            Assert.IsTrue(nb.IsNullBreakActiveFor(DamageType.Elemental, ElementType.Fire));

            // Tick 1 turn -> 1 turn remaining
            nb.TickTurn();
            Assert.IsTrue(nb.HasActiveNullBreak);
            Assert.AreEqual(1, nb.RemainingTurns);

            // Still active -> cannot refresh
            Assert.IsFalse(nb.TryApplyElemental(ElementType.Fire, skillLevel: 3));

            // Tick 2nd turn -> expires
            nb.TickTurn();
            Assert.IsFalse(nb.HasActiveNullBreak);
            Assert.AreEqual(0, nb.RemainingTurns);

            // Now a new Null Break can be applied (Lv3 = 3 turns)
            Assert.IsTrue(nb.TryApplyPhysical(skillLevel: 3));
            Assert.AreEqual(3, nb.RemainingTurns);
            Assert.IsTrue(nb.IsNullBreakActiveFor(DamageType.Physical));
        }

        // =====================================================================
        // 16.7 Player Elemental Resistance Tests
        // =====================================================================

        [Test]
        public void ElementalResistance_StacksAdditively_CapsAt75Percent_AndIgnoredByPhysicalAndAlmighty()
        {
            var resProfile = new ElementalResistanceProfile();
            // 40% Equipment + 30% Stats + 20% Buff = 90% uncapped -> clamped to 75% (0.75f)
            resProfile.SetSources(ElementType.Fire, equipment: 0.40f, stats: 0.30f, buff: 0.20f, other: 0f);

            Assert.AreEqual(0.90f, resProfile.GetUncappedResistance(ElementType.Fire), 0.001f);
            Assert.AreEqual(0.75f, resProfile.GetFinalResistance(ElementType.Fire), 0.001f);

            // 100 incoming Fire damage -> 100 * (1 - 0.75) = 25
            var fireCtx = new DamageContext(
                attackerStr: 0,
                attackerInt: 0,
                targetPhysicalDefense: 0f,
                components: new[] { DamageComponent.Elemental(ElementType.Fire, skillPower: 100f, scaling: 0f) })
            {
                targetElementalResistance = resProfile
            };
            Assert.AreEqual(25, DamageCalculator.Calculate(fireCtx).totalDamage);

            // Physical damage ignores Elemental Resistance (deals full 100):
            var physCtx = new DamageContext(
                attackerStr: 0,
                attackerInt: 0,
                targetPhysicalDefense: 0f,
                components: new[] { DamageComponent.Physical(skillPower: 100f, scaling: 0f) })
            {
                forcedCriticalHit = false,
                targetElementalResistance = resProfile
            };
            Assert.AreEqual(100, DamageCalculator.Calculate(physCtx).totalDamage);

            // Almighty damage ignores Elemental Resistance (deals full 100):
            var almightyCtx = new DamageContext(
                attackerStr: 0,
                attackerInt: 0,
                targetPhysicalDefense: 0f,
                components: new[] { DamageComponent.Almighty(skillPower: 100f, scaling: 0f) })
            {
                targetElementalResistance = resProfile
            };
            Assert.AreEqual(100, DamageCalculator.Calculate(almightyCtx).totalDamage);
        }

        // =====================================================================
        // 16.8 Multiplicative Modifiers Tests
        // =====================================================================

        [Test]
        public void DamageModifiers_StackMultiplicativelyWithinAndAcrossScopes()
        {
            var mods = new DamageModifierSet();
            // +20% (1.2x) and +50% (1.5x) must stack as 1.2 * 1.5 = 1.8x (NOT 1.7x)
            mods.AddPercentBonus(DamageModifierScope.PhysicalDamageBonus, 0.20f);
            mods.AddPercentBonus(DamageModifierScope.PhysicalDamageBonus, 0.50f);
            Assert.AreEqual(1.8f, mods.GetPhysicalDamageBonus(), 0.0001f);

            var takenMods = new DamageModifierSet();
            takenMods.AddMultiplier(DamageModifierScope.PhysicalDamageTaken, 1.2f);
            takenMods.AddMultiplier(DamageModifierScope.AllDamageTaken, 1.25f);

            // Base 100 * 1.8 (bonus) * 1.2 (phys taken) * 1.25 (all taken) = 270
            var ctx = new DamageContext(
                attackerStr: 0,
                attackerInt: 0,
                targetPhysicalDefense: 0f,
                components: new[] { DamageComponent.Physical(skillPower: 100f, scaling: 0f) })
            {
                attackerModifiers = mods,
                targetModifiers = takenMods,
                forcedCriticalHit = false
            };

            Assert.AreEqual(270, DamageCalculator.Calculate(ctx).totalDamage);
        }

        // =====================================================================
        // 16.9 Critical Hit Caps, QTE Multipliers & Battle Integration Tests
        // =====================================================================

        [Test]
        public void CriticalHitConfig_EnforcesBaseValuesAndCaps()
        {
            var def = CriticalHitConfig.Default;
            Assert.AreEqual(0.05f, def.FinalCriticalChance, 0.0001f);
            Assert.AreEqual(1.5f, def.FinalCriticalDamage, 0.0001f);

            // Cap at 95% crit chance and 300% (3.0x) crit damage
            var capped = new CriticalHitConfig
            {
                bonusCriticalChance = 2.0f,
                bonusCriticalDamage = 5.0f
            };
            Assert.AreEqual(0.95f, capped.FinalCriticalChance, 0.0001f);
            Assert.AreEqual(3.0f, capped.FinalCriticalDamage, 0.0001f);
        }

        [Test]
        public void QteMultipliers_MatchSpecification_AndDamageOverTimeIgnoresQteAndCrit()
        {
            Assert.AreEqual(1.5f, DamageCalculator.GetQteMultiplier(QteResult.Perfect), 0.0001f);
            Assert.AreEqual(1.0f, DamageCalculator.GetQteMultiplier(QteResult.Good), 0.0001f);
            Assert.AreEqual(0.5f, DamageCalculator.GetQteMultiplier(QteResult.Miss), 0.0001f);
            Assert.AreEqual(1.0f, DamageCalculator.GetQteMultiplier(QteResult.Perfect, isDamageOverTime: true), 0.0001f);

            var dotCtx = new DamageContext(
                attackerStr: 10,
                attackerInt: 10,
                targetPhysicalDefense: 0f,
                components: new[] { DamageComponent.Physical(skillPower: 100f, scaling: 0f) })
            {
                qteResult = QteResult.Perfect,
                forcedCriticalHit = true,
                isDamageOverTime = true
            };

            var res = DamageCalculator.Calculate(dotCtx);
            Assert.AreEqual(100, res.totalDamage);
            Assert.IsFalse(res.FirstHit.anyComponentCriticalHit);
        }

        [Test]
        public void BattleIntegration_SkillWithDamageComponentsAndNullBreak_ExecutesCorrectly()
        {
            var heroDef = Database.Characters["knight"];
            var heroUnit = new Unit(heroDef);
            var enemyDef = new EnemyDef
            {
                id = "wraith_test",
                name = "Flame Wraith",
                shortName = "Wraith",
                tag = "WRT",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 500, maxMp: 20, atk: 10, def: 10, spd: 1, mag: 10),
                affinityProfile = new EnemyAffinityProfile()
            };
            enemyDef.affinityProfile.SetElementalAffinity(ElementType.Fire, AffinityType.Nullification);

            var fireSlash = new SkillDef
            {
                id = "test_fire_slash",
                name = "Fire Slash",
                kind = SkillKind.Magic,
                target = TargetType.Enemy,
                mpCost = 0,
                skillLevel = 2,
                damageComponents = new[]
                {
                    DamageComponent.Elemental(ElementType.Fire, skillPower: 50f, scaling: 0f)
                }
            };
            var nullBreakSkill = new SkillDef
            {
                id = "test_fire_null_break",
                name = "Fire Null Break",
                kind = SkillKind.Support,
                target = TargetType.Enemy,
                mpCost = 0,
                skillLevel = 2,
                appliesNullBreak = true,
                nullBreakTarget = AffinityDamageKey.Elemental(ElementType.Fire)
            };

            Database.Skills[fireSlash.id] = fireSlash;
            Database.Skills[nullBreakSkill.id] = nullBreakSkill;
            heroUnit.skillLoadout.UnlockActive(fireSlash.id);
            heroUnit.skillLoadout.UnlockActive(nullBreakSkill.id);
            heroUnit.skillLoadout.activeSlots[0] = fireSlash.id;
            heroUnit.skillLoadout.activeSlots[1] = nullBreakSkill.id;

            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef }, new List<string>(), new System.Random(42));
            var actor = battle.NextTurn();
            var target = battle.Enemies[0];

            // 1. Before Null Break: Fire Slash is nullified (0 damage)
            var hitsBefore = battle.Perform(BattleAction.Skill(actor, fireSlash, target), power: 1.0f);
            Assert.AreEqual(0, hitsBefore[0].amount);
            Assert.AreEqual(500, target.unit.hp);

            // Enemy acts
            var enemyTurn = battle.NextTurn();
            battle.Perform(BattleAction.Guard(enemyTurn));

            // 2. Hero applies Fire Null Break (Lv2 = 2 turns)
            actor = battle.NextTurn();
            battle.Perform(BattleAction.Skill(actor, nullBreakSkill, target), power: 1.0f);
            Assert.IsTrue(target.NullBreak.HasActiveNullBreak);
            Assert.AreEqual(2, target.NullBreak.RemainingTurns);

            // Enemy acts (ticks 1st turn of Null Break -> 1 turn remaining)
            enemyTurn = battle.NextTurn();
            Assert.AreEqual(1, target.NullBreak.RemainingTurns);
            battle.Perform(BattleAction.Guard(enemyTurn));

            // 3. Hero uses Fire Slash while Null Break is active -> deals 50 damage!
            actor = battle.NextTurn();
            var hitsAfter = battle.Perform(BattleAction.Skill(actor, fireSlash, target), power: 1.0f);
            Assert.AreEqual(50, hitsAfter[0].amount);
            Assert.AreEqual(450, target.unit.hp);
        }
    }
}

