using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    public class StatusEffectTests
    {
        private Combatant CreateMockHero(string name = "Hero", int str = 15, int intel = 15, int vit = 10, int spd = 10)
        {
            var def = Database.Characters["knight"];
            var unit = new Unit(def);
            unit.abilities.racialModifiers = AbilityScores.Zero;
            unit.abilities.baseScores = new AbilityScores(str, 10, vit, intel, 10, 10);
            unit.RecalculateStats(true);
            var hero = new Combatant(unit, Side.Party, 0, name, name.Substring(0, 3).ToUpperInvariant());
            hero.StatusController.ResistanceProfile.baseResistance = 0f;
            hero.StatusController.ResistanceProfile.statsResistance = 0f;
            hero.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
            return hero;
        }

        private Combatant CreateMockEnemy(string name = "Foe", bool isBoss = false, int hp = 100, int def = 10, int spd = 8)
        {
            var enemyDef = new EnemyDef
            {
                id = name.ToLowerInvariant(),
                name = name,
                shortName = name,
                tag = name.Substring(0, Math.Min(3, name.Length)).ToUpperInvariant(),
                tier = isBoss ? Tier.Boss : Tier.Normal,
                stats = new Stats(maxHp: hp, maxMp: 20, atk: 12, def: def, spd: spd, mag: 10)
            };
            var unit = new Unit(enemyDef);
            var enemy = new Combatant(unit, Side.Enemy, 0, name, name.Substring(0, Math.Min(3, name.Length)).ToUpperInvariant(), enemyDef);
            enemy.StatusController.ResistanceProfile.statsResistance = 0f;
            enemy.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
            return enemy;
        }

        // =====================================================================
        // 1. Status Resistance / Mental Resistance & Application Tests (GDD Bab 11 §9 & §13)
        // =====================================================================

        [Test]
        public void SkillChipsShowEffectSizeAndOnlyUncertainChances()
        {
            var bash = Database.Skills["shield_bash"].apply;      // Defense Down 0.25 (sure), Stun 35% chance
            string defDown = StatusInfo.ChipText(bash.First(a => a.id == StatusId.DefenseDown));
            StringAssert.Contains("-25%", defDown);
            StringAssert.DoesNotContain("100%", defDown, "a certain effect shows no chance, and never as 100%");
            StringAssert.EndsWith("· 2T", defDown);

            string stun = StatusInfo.ChipText(bash.First(a => a.id == StatusId.Stun));
            StringAssert.Contains("35% CHANCE", stun);

            string bless = StatusInfo.ChipText(Database.Skills["blessing"].apply[0]); // Defense Up 0.30
            StringAssert.Contains("+30%", bless);
            StringAssert.DoesNotContain("100%", bless);
        }

        [Test]
        public void StatusResistance_StacksAdditively_AndCapsAt85Percent()
        {
            var profile = new StatusResistanceProfile();
            profile.baseResistance = 0.20f;
            profile.equipmentResistance = 0.25f;
            profile.statsResistance = 0.20f;
            profile.buffResistance = 0.25f; // Uncapped = 0.90f (90%)

            Assert.AreEqual(0.90f, profile.TotalUncappedAdditive, 0.001f);
            Assert.AreEqual(0.85f, profile.InitialResistance, 0.001f);
            Assert.AreEqual(0.85f, profile.GetEffectiveResistance(), 0.001f);
        }

        [Test]
        public void StatusAndMentalPools_AreSeparate()
        {
            var p = new StatusResistanceProfile { statsResistance = 0.50f, mentalStatsResistance = 0.20f };

            Assert.AreEqual(0.50f, p.GetEffectiveResistance(StatusId.Burn), 1e-5f, "non-Mental uses Status Resistance");
            Assert.AreEqual(0.20f, p.GetEffectiveResistance(StatusId.Sleep), 1e-5f, "Mental uses Mental Resistance only");
            Assert.AreEqual(0.20f, p.GetEffectiveMentalResistance(), 1e-5f);

            p.mentalBuffResistance = 0.10f;
            Assert.AreEqual(0.30f, p.GetEffectiveResistance(StatusId.Charm), 1e-5f, "Mental buffs add to the Mental pool");
            Assert.AreEqual(0.50f, p.GetEffectiveResistance(StatusId.Poison), 1e-5f, "Mental buffs do not touch Status Resistance");

            var clone = p.Clone();
            Assert.AreEqual(0.30f, clone.GetEffectiveResistance(StatusId.Charm), 1e-5f, "Clone keeps the Mental pool");
        }

        [Test]
        public void BothPools_ClampAtPlusMinus85_AndNegativeRaisesChance()
        {
            var p = new StatusResistanceProfile { statsResistance = -2f, mentalStatsResistance = 2f };
            Assert.AreEqual(-0.85f, p.GetEffectiveResistance(StatusId.Burn), 1e-5f);
            Assert.AreEqual(0.85f, p.GetEffectiveResistance(StatusId.Fear), 1e-5f);
            Assert.AreEqual(0.925f, p.CalculateFinalApplicationChance(0.5f, StatusId.Burn), 1e-5f, "-85% turns 50% into 92.5%");
            Assert.AreEqual(1f, p.CalculateFinalApplicationChance(0.9f, StatusId.Burn), 1e-5f, "final chance clamps to 100%");

            var m = new StatusResistanceProfile { mentalStatsResistance = -2f };
            Assert.AreEqual(-0.85f, m.GetEffectiveMentalResistance(), 1e-5f);
        }

        [Test]
        public void ResistanceDown_ShrinksPositiveInBothPools_AndLeavesNegativeUnchanged()
        {
            var p = new StatusResistanceProfile { statsResistance = 0.60f, mentalStatsResistance = 0.40f };
            p.AddReduction(0.50f);
            Assert.AreEqual(0.30f, p.GetEffectiveResistance(StatusId.Burn), 1e-5f);
            Assert.AreEqual(0.20f, p.GetEffectiveResistance(StatusId.Sleep), 1e-5f);

            var n = new StatusResistanceProfile { statsResistance = -0.40f, mentalStatsResistance = -0.30f };
            n.AddReduction(0.50f);
            Assert.AreEqual(-0.40f, n.GetEffectiveResistance(StatusId.Burn), 1e-5f, "a reduction must not help the target");
            Assert.AreEqual(-0.30f, n.GetEffectiveResistance(StatusId.Sleep), 1e-5f);
        }

        [Test]
        public void Boss_MentalImmunity_HoldsAgainstNegativeMentalResistanceAndReductions()
        {
            var p = new StatusResistanceProfile { mentalStatsResistance = -0.85f };
            p.AddReduction(0.90f);
            foreach (var id in new[] { StatusId.Sleep, StatusId.Fear, StatusId.Charm, StatusId.MindControl })
            {
                Assert.IsTrue(p.IsImmune(id, isBoss: true));
                Assert.AreEqual(0f, p.CalculateFinalApplicationChance(1f, id, isBoss: true), 1e-5f);
                Assert.IsFalse(p.RollApplication(1f, id, isBoss: true, () => 0f));
            }
        }

        [Test]
        public void MindThemedContent_GrantsMentalResistance()
        {
            var hermitRank1 = Database.Backgrounds["hermit"].ranks[0];
            Assert.AreEqual(5, hermitRank1.mentalResistanceBonus);
            Assert.IsTrue(hermitRank1.modifiers.All(m => m.derivedStat == DerivedStatId.MentalResistance));
            foreach (var pid in new[] { "iron_will", "inner_focus", "unwavering_faith" })
                Assert.IsTrue(Database.Passives[pid].modifiers.Any(m => m.derivedStat == DerivedStatId.MentalResistance), pid);
        }

        [Test]
        public void ResistanceReductions_StackMultiplicatively_MatchingGDDFormulas()
        {
            // GDD Section 9 example:
            // "Status Effect Chance 80%, Status Resistance 85% -> chance final 12%."
            var profile1 = new StatusResistanceProfile { baseResistance = 0.85f };
            float chance1 = profile1.CalculateFinalApplicationChance(0.80f, StatusId.Burn);
            Assert.AreEqual(0.12f, chance1, 0.0001f);

            // GDD Section 9 second example:
            // "Resistance 60% with two 20% debuffs -> 38.4% resistance; skill chance 40% -> 24.64%."
            var profile2 = new StatusResistanceProfile { baseResistance = 0.60f };
            profile2.AddReduction(0.20f);
            profile2.AddReduction(0.20f);

            Assert.AreEqual(0.384f, profile2.GetEffectiveResistance(), 0.0001f);
            float chance2 = profile2.CalculateFinalApplicationChance(0.40f, StatusId.Burn);
            Assert.AreEqual(0.2464f, chance2, 0.0001f);
        }

        [Test]
        public void Boss_IsImmuneToMentalEffects_AndResistanceReductionDoesNotBypassImmunity()
        {
            var boss = CreateMockEnemy("Demon Lord", isBoss: true);
            var hero = CreateMockHero("Hero");

            // Boss has Mental Effect immunity (GDD Section 13)
            Assert.IsTrue(boss.StatusController.ResistanceProfile.IsImmune(StatusId.Sleep, isBoss: true));
            Assert.IsTrue(boss.StatusController.ResistanceProfile.IsImmune(StatusId.Charm, isBoss: true));
            Assert.IsTrue(boss.StatusController.ResistanceProfile.IsImmune(StatusId.Confusion, isBoss: true));
            Assert.IsTrue(boss.StatusController.ResistanceProfile.IsImmune(StatusId.Silence, isBoss: true));

            // Apply Resistance Reduction debuffs to Boss
            boss.StatusController.ResistanceProfile.AddReduction(0.50f);
            boss.StatusController.ResistanceProfile.AddReduction(0.50f);

            // GDD Section 13: "Immunity bypass: Status Effect Resistance Reduction does not pierce Mental Effect immunity."
            Assert.AreEqual(0f, boss.StatusController.ResistanceProfile.CalculateFinalApplicationChance(1.0f, StatusId.Sleep, isBoss: true));

            var sleepApply = new StatusApply { id = StatusId.Sleep, turns = 2, chance = 1f };
            bool applied = boss.StatusController.TryApplyStatus(sleepApply, hero, boss, out _);
            Assert.IsFalse(applied, "Boss must remain immune to Sleep despite Resistance Reductions");
            Assert.IsFalse(boss.StatusController.HasStatus(StatusId.Sleep));

            // Non-Mental effects (like Burn) CAN be applied to Boss
            var burnApply = new StatusApply { id = StatusId.Burn, turns = 2, chance = 1f };
            // Set RNG to succeed
            bool burnApplied = boss.StatusController.TryApplyStatus(burnApply, hero, boss, out _, new System.Random(1));
            Assert.IsTrue(burnApplied, "Boss is susceptible to Non-Mental status effects like Burn");
        }

        [Test]
        public void Application_FailsOnDeadTarget()
        {
            var hero = CreateMockHero();
            var target = CreateMockEnemy("Ghost", hp: 0);
            target.unit.hp = 0;
            Assert.IsFalse(target.Alive);

            var burnApply = new StatusApply { id = StatusId.Burn, turns = 2, chance = 1f };
            bool applied = target.StatusController.TryApplyStatus(burnApply, hero, target, out _);
            Assert.IsFalse(applied, "Cannot apply status effects to a dead target");
        }

        // =====================================================================
        // 2. Stacking & Independent Instances Tests (GDD Section 8)
        // =====================================================================

        [Test]
        public void IndependentInstances_AllowMultipleInstancesOfSameStatusWithoutLimitOrRefresh()
        {
            var hero = CreateMockHero();
            var target = CreateMockEnemy();

            // GDD Section 8: "Status instances are independent: a new instance does not refresh/extend an old one.
            // There is no limit on the number of active instances of the same status."
            var burn1 = new StatusApply { id = StatusId.Burn, turns = 2, chance = 1f };
            var burn2 = new StatusApply { id = StatusId.Burn, turns = 3, chance = 1f };

            // Bypass resistance check by setting resistance to 0
            target.StatusController.ResistanceProfile.baseResistance = 0f;
            target.StatusController.ResistanceProfile.statsResistance = 0f;
            target.StatusController.ResistanceProfile.mentalStatsResistance = 0f;

            Assert.IsTrue(target.StatusController.TryApplyStatus(burn1, hero, target, out var inst1));
            Assert.IsTrue(target.StatusController.TryApplyStatus(burn2, hero, target, out var inst2));

            Assert.AreEqual(2, target.StatusController.CountStatus(StatusId.Burn));
            Assert.AreNotEqual(inst1.instanceId, inst2.instanceId);
            Assert.AreEqual(2, inst1.remainingTurns);
            Assert.AreEqual(3, inst2.remainingTurns);
        }

        // =====================================================================
        // 3. Dynamic DoT Processing Tests (GDD Section 10 & 11)
        // =====================================================================

        [Test]
        public void DynamicDoT_CalculatesDamageViaDamageCalculator_AndRespectsAffinityAndDefense()
        {
            // Human race grants +1 STR, so Base STR 9 yields Final STR 10.
            var attacker = CreateMockHero("Pyromancer", str: 9, intel: 20);
            var target = CreateMockEnemy("Target", def: 10, hp: 200);
            target.StatusController.ResistanceProfile.baseResistance = 0f;

            // Burn DoT: Elemental Fire, uses INT scaling (Base = 20 + 20*1 = 40)
            var burnApply = new StatusApply
            {
                id = StatusId.Burn,
                turns = 3,
                chance = 1f,
                dotSkillPower = 20f,
                dotScaling = 1.0f,
                dotDamageType = DamageType.Elemental,
                dotElementType = ElementType.Fire
            };
            target.StatusController.TryApplyStatus(burnApply, attacker, target, out var burnInst);

            // Target has Fire Weakness (1.5x)
            target.AffinityProfile.SetElementalAffinity(ElementType.Fire, AffinityType.Weakness);

            // 40 Base * 1.5 (Weakness) = 60 Fire damage
            int dotDamage = burnInst.CalculateDynamicDoTDamage();
            Assert.AreEqual(60, dotDamage);

            // Target changes equipment to 50% Fire Resistance -> 60 * (1 - 0.5) = 30
            target.ElementalResistances.SetSources(ElementType.Fire, equipment: 0.50f);
            int dotDamageResisted = burnInst.CalculateDynamicDoTDamage();
            Assert.AreEqual(30, dotDamageResisted);

            // Bleed DoT: Physical, uses STR scaling against Physical Defense
            var bleedApply = new StatusApply
            {
                id = StatusId.Bleed,
                turns = 2,
                chance = 1f,
                dotSkillPower = 30f,
                dotScaling = 1.0f,
                dotDamageType = DamageType.Physical
            };
            target.StatusController.TryApplyStatus(bleedApply, attacker, target, out var bleedInst);
            // Attacker STR = 10 -> Base = 30 + 10 = 40. Target Def = 10 -> 40 - 10 = 30.
            Assert.AreEqual(30, bleedInst.CalculateDynamicDoTDamage());
        }

        [Test]
        public void TurnStart_ProcessesDoTsAndDecrementsRemainingTurns()
        {
            var hero = CreateMockHero(intel: 10);
            var target = CreateMockEnemy(hp: 100);
            target.StatusController.ResistanceProfile.baseResistance = 0f;

            var burn1 = new StatusApply { id = StatusId.Burn, turns = 1, dotSkillPower = 10f, dotScaling = 0f, chance = 1f };
            var burn2 = new StatusApply { id = StatusId.Burn, turns = 2, dotSkillPower = 15f, dotScaling = 0f, chance = 1f };

            target.StatusController.TryApplyStatus(burn1, hero, target, out _);
            target.StatusController.TryApplyStatus(burn2, hero, target, out _);

            Assert.AreEqual(100, target.unit.hp);
            Assert.AreEqual(2, target.StatusController.CountStatus(StatusId.Burn));

            // Tick Turn 1: 10 + 15 = 25 damage -> HP = 75.
            // burn1 expires (1 -> 0); burn2 remains (2 -> 1).
            target.StatusController.ProcessTurnStart(null, target, null);

            Assert.AreEqual(75, target.unit.hp);
            Assert.AreEqual(1, target.StatusController.CountStatus(StatusId.Burn));

            // Tick Turn 2: burn2 deals 15 damage -> HP = 60. burn2 expires (1 -> 0).
            target.StatusController.ProcessTurnStart(null, target, null);

            Assert.AreEqual(60, target.unit.hp);
            Assert.AreEqual(0, target.StatusController.CountStatus(StatusId.Burn));
        }

        // =====================================================================
        // 4. Mental Effects Tests (GDD Section 12)
        // =====================================================================

        [Test]
        public void Sleep_PreventsAction_AndBreaksImmediatelyUponTakingDamage()
        {
            var hero = CreateMockHero();
            var target = CreateMockEnemy();
            target.StatusController.ResistanceProfile.baseResistance = 0f;

            var sleepApply = new StatusApply { id = StatusId.Sleep, turns = 3, chance = 1f };
            target.StatusController.TryApplyStatus(sleepApply, hero, target, out _);

            Assert.IsTrue(target.StatusController.HasStatus(StatusId.Sleep));
            Assert.IsFalse(target.StatusController.CanPerformAction(BattleAction.Attack(target, hero), out var reason));
            StringAssert.Contains("asleep", reason.ToLowerInvariant());

            // Target takes damage -> Sleep is removed immediately (GDD Section 12)
            target.StatusController.OnDamageReceived(10, isDoT: false);
            Assert.IsFalse(target.StatusController.HasStatus(StatusId.Sleep), "Sleep must break when damage is taken");
            Assert.IsTrue(target.StatusController.CanPerformAction(BattleAction.Attack(target, hero), out _));
        }

        [Test]
        public void Silence_PreventsSkills_WhileAllowingAttackAndItems()
        {
            var hero = CreateMockHero();
            hero.StatusController.ResistanceProfile.baseResistance = 0f;

            var silenceApply = new StatusApply { id = StatusId.Silence, turns = 2, chance = 1f };
            hero.StatusController.TryApplyStatus(silenceApply, null, hero, out _);

            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.Silence));

            var skillAction = BattleAction.Skill(hero, Database.Skills["shield_bash"]);
            var attackAction = BattleAction.Attack(hero, hero);
            var guardAction = BattleAction.Guard(hero);

            Assert.IsFalse(hero.StatusController.CanPerformAction(skillAction, out var reason));
            StringAssert.Contains("silenced", reason.ToLowerInvariant());

            Assert.IsTrue(hero.StatusController.CanPerformAction(attackAction, out _));
            Assert.IsTrue(hero.StatusController.CanPerformAction(guardAction, out _));
        }

        [Test]
        public void Charm_PreventsAttackingCharmer_WhileAllowingOtherTargets()
        {
            var charmer = CreateMockEnemy("Siren");
            var victim = CreateMockHero("Knight");
            var otherEnemy = CreateMockEnemy("Goblin");
            victim.StatusController.ResistanceProfile.baseResistance = 0f;

            var charmApply = new StatusApply { id = StatusId.Charm, turns = 2, chance = 1f };
            victim.StatusController.TryApplyStatus(charmApply, charmer, victim, out _);

            Assert.IsTrue(victim.StatusController.HasStatus(StatusId.Charm));

            // Cannot attack charmer
            var attackCharmer = BattleAction.Attack(victim, charmer);
            Assert.IsFalse(victim.StatusController.CanPerformAction(attackCharmer, out var reason));
            StringAssert.Contains("charmed", reason.ToLowerInvariant());

            // CAN attack other enemies
            var attackOther = BattleAction.Attack(victim, otherEnemy);
            Assert.IsTrue(victim.StatusController.CanPerformAction(attackOther, out _));
        }

        [Test]
        public void Fear_ReducesOutgoingDamageDealtMultiplier()
        {
            var hero = CreateMockHero();
            hero.StatusController.ResistanceProfile.baseResistance = 0f;

            Assert.AreEqual(1.0f, hero.StatusController.GetDamageDealtMultiplier(), 0.001f);

            var fearApply = new StatusApply { id = StatusId.Fear, turns = 2, magnitude = 0.30f, chance = 1f };
            hero.StatusController.TryApplyStatus(fearApply, null, hero, out _);

            Assert.AreEqual(0.70f, hero.StatusController.GetDamageDealtMultiplier(), 0.001f);
        }

        [Test]
        public void Freeze_ReducesSpeed()
        {
            var hero = CreateMockHero(spd: 20);
            hero.StatusController.ResistanceProfile.baseResistance = 0f;

            int speedBefore = hero.Speed;

            var freezeApply = new StatusApply { id = StatusId.Freeze, turns = 2, magnitude = 0.40f, chance = 1f };
            hero.StatusController.TryApplyStatus(freezeApply, null, hero, out _);

            Assert.AreEqual(0.60f, hero.StatusController.GetSpeedMultiplier(), 0.001f);
            Assert.Less(hero.Speed, speedBefore);
        }

        // =====================================================================
        // 5. Cleansing Tests (Prompt Section 10)
        // =====================================================================

        [Test]
        public void Cleanse_RemovesDebuffsAndMentalEffects()
        {
            var hero = CreateMockHero();
            hero.StatusController.ResistanceProfile.baseResistance = 0f;

            hero.StatusController.TryApplyStatus(new StatusApply { id = StatusId.Burn, turns = 3, chance = 1f }, null, hero, out _);
            hero.StatusController.TryApplyStatus(new StatusApply { id = StatusId.Fear, turns = 2, chance = 1f }, null, hero, out _);
            hero.StatusController.TryApplyStatus(new StatusApply { id = StatusId.Silence, turns = 2, chance = 1f }, null, hero, out _);
            hero.StatusController.TryApplyStatus(new StatusApply { id = StatusId.AttackUp, turns = 2, chance = 1f }, null, hero, out _); // Buff!

            Assert.AreEqual(4, hero.StatusController.ActiveInstances.Count);

            // Cleanse Mental Effects: removes Fear and Silence
            int cleansedMental = hero.StatusController.CleanseMentalEffects();
            Assert.AreEqual(2, cleansedMental);
            Assert.IsFalse(hero.StatusController.HasStatus(StatusId.Fear));
            Assert.IsFalse(hero.StatusController.HasStatus(StatusId.Silence));
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.Burn));
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.AttackUp));

            // Cleanse Debuffs: removes Burn, leaves AttackUp
            int cleansedDebuffs = hero.StatusController.CleanseDebuffs();
            Assert.AreEqual(1, cleansedDebuffs);
            Assert.IsFalse(hero.StatusController.HasStatus(StatusId.Burn));
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.AttackUp));
        }

        // =====================================================================
        // 6. End-to-End Battle Integration Test
        // =====================================================================

        [Test]
        public void BattleIntegration_DoTAndStatusLifecycle_WorksInBattleFlow()
        {
            var heroDef = Database.Characters["mage"];
            var heroUnit = new Unit(heroDef);
            var enemyDef = new EnemyDef
            {
                id = "test_goblin",
                name = "Goblin Scout",
                shortName = "Goblin",
                tag = "GOB",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 80, maxMp: 10, atk: 5, def: 2, spd: 5, mag: 2)
            };

            var igniteSkill = new SkillDef
            {
                id = "test_ignite",
                name = "Ignite",
                kind = SkillKind.Magic,
                target = TargetType.Enemy,
                mpCost = 0,
                apply = new[]
                {
                    new StatusApply
                    {
                        id = StatusId.Burn,
                        turns = 2,
                        chance = 1f,
                        dotSkillPower = 15f,
                        dotScaling = 1.0f,
                        dotDamageType = DamageType.Elemental,
                        dotElementType = ElementType.Fire
                    }
                }
            };
            Database.Skills[igniteSkill.id] = igniteSkill;
            heroUnit.skillLoadout.UnlockActive(igniteSkill.id);
            heroUnit.skillLoadout.activeSlots[0] = igniteSkill.id;

            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef }, new List<string>(), new System.Random(42));

            var mage = battle.NextTurn();
            var goblin = battle.Enemies[0];
            goblin.StatusController.ResistanceProfile.baseResistance = 0f;

            // Mage applies Ignite (Burn 2 turns)
            battle.Perform(BattleAction.Skill(mage, igniteSkill, goblin), power: 1.0f);
            Assert.IsTrue(goblin.StatusController.HasStatus(StatusId.Burn));

            // Goblin's turn: StartTurn triggers Burn tick dynamically
            var goblinTurn = battle.NextTurn();
            Assert.AreSame(goblin, goblinTurn);
            Assert.Less(goblin.unit.hp, 80, "Goblin took Burn DoT damage at turn start");
        }

        // =====================================================================
        // 7. Freeze, Curse, Despair & Mind Control Design Alignment Tests
        // =====================================================================

        [Test]
        public void Freeze_DescriptionAndChip_ReflectSpeedReduction()
        {
            string desc = StatusInfo.Description(StatusId.Freeze).ToLowerInvariant();
            StringAssert.Contains("speed", desc);
            StringAssert.DoesNotContain("cannot act", desc);

            string chip = StatusInfo.ChipText(new StatusApply { id = StatusId.Freeze, turns = 2, magnitude = 0.35f });
            StringAssert.Contains("-35%", chip);
            StringAssert.EndsWith("· 2T", chip);
        }

        [Test]
        public void Curse_ReducesHealingReceivedBy50Percent_FixedAcrossInstances()
        {
            var heroUnit = new Unit(Database.Characters["knight"]);
            var enemyDef = new EnemyDef
            {
                id = "curse_dummy",
                name = "Dummy",
                shortName = "Dummy",
                tag = "DUM",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 200, maxMp: 20, atk: 10, def: 10, spd: 5, mag: 10)
            };
            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef }, new List<string>(), new System.Random(1));
            var hero = battle.Party[0];
            hero.StatusController.ResistanceProfile.baseResistance = 0f;
            hero.StatusController.ResistanceProfile.statsResistance = 0f;
            hero.StatusController.ResistanceProfile.mentalStatsResistance = 0f;

            // Uncursed heal
            hero.unit.hp = 10;
            var normalHeal = battle.Heal(hero, 40);
            Assert.AreEqual(40, normalHeal.amount);

            // Single Curse -> fixed -50% healing received
            battle.Apply(hero, new StatusApply { id = StatusId.Curse, turns = 3, chance = 1f }, battle.Enemies[0], 1f);
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.Curse));
            Assert.AreEqual(0.50f, hero.StatusController.GetHealingReceivedMultiplier(), 0.001f);

            hero.unit.hp = 10;
            var cursedHeal = battle.Heal(hero, 40);
            Assert.AreEqual(20, cursedHeal.amount, "Curse must reduce healing received by 50%");

            // Overlapping Curse instance must NOT stack past fixed -50%
            battle.Apply(hero, new StatusApply { id = StatusId.Curse, turns = 2, chance = 1f }, battle.Enemies[0], 1f);
            Assert.AreEqual(2, hero.StatusController.CountStatus(StatusId.Curse));
            hero.unit.hp = 10;
            var multiCursedHeal = battle.Heal(hero, 40);
            Assert.AreEqual(20, multiCursedHeal.amount, "Multiple Curse instances must remain fixed at -50% healing");

            // Cleanse Curse -> healing returns to 100%
            hero.StatusController.RemoveStatus(StatusId.Curse);
            hero.unit.hp = 10;
            var restoredHeal = battle.Heal(hero, 40);
            Assert.AreEqual(40, restoredHeal.amount);
        }

        [Test]
        public void Curse_IncreasesAllDamageTakenBy25Percent_WithoutDoubleApplication()
        {
            // 1. Modular DamageCalculator path via CreateDamageContext
            var attacker = CreateMockHero("Attacker", str: 19, intel: 20);
            var target = CreateMockEnemy("Target", hp: 500, def: 10);
            target.StatusController.ResistanceProfile.baseResistance = 0f;

            var comp = new[] { DamageComponent.Elemental(ElementType.Fire, 80f, 1.0f) };
            var uncursedRes = DamageCalculator.Calculate(attacker.CreateDamageContext(target, comp, 1, QteResult.Good));
            int baseDmg = uncursedRes.totalDamage; // 80 + 20 = 100

            target.StatusController.TryApplyStatus(new StatusApply { id = StatusId.Curse, turns = 3, chance = 1f }, attacker, target, out _);
            var cursedRes = DamageCalculator.Calculate(attacker.CreateDamageContext(target, comp, 1, QteResult.Good));
            Assert.AreEqual((int)Math.Floor(baseDmg * 1.25f), cursedRes.totalDamage, "Curse must add +25% All Damage Taken in DamageCalculator");

            // Second Curse instance stays fixed at +25%
            target.StatusController.TryApplyStatus(new StatusApply { id = StatusId.Curse, turns = 2, chance = 1f }, attacker, target, out _);
            var doubleCursedRes = DamageCalculator.Calculate(attacker.CreateDamageContext(target, comp, 1, QteResult.Good));
            Assert.AreEqual(cursedRes.totalDamage, doubleCursedRes.totalDamage, "Multiple Curse instances must stay fixed at +25%");

            // 2. Dynamic DoT path
            var burnApply = new StatusApply
            {
                id = StatusId.Burn,
                turns = 2,
                chance = 1f,
                dotSkillPower = 20f,
                dotScaling = 1.0f,
                dotDamageType = DamageType.Elemental,
                dotElementType = ElementType.Fire
            };
            target.StatusController.TryApplyStatus(burnApply, attacker, target, out var burnInst);
            // Base = 20 + 20 = 40; with Curse 1.25x = 50
            Assert.AreEqual(50, burnInst.CalculateDynamicDoTDamage());

            // 3. Battle.Perform legacy Attack path (Battle.Deal) vs Battle.Perform modular Skill path (no double-count)
            var modSkill = new SkillDef
            {
                id = "test_curse_mod_skill",
                name = "Arcane Test",
                kind = SkillKind.Magic,
                target = TargetType.Enemy,
                mpCost = 0,
                element = Element.Fire,
                damageComponents = new[] { DamageComponent.Elemental(ElementType.Fire, 80f, 1.0f) }
            };
            Database.Skills[modSkill.id] = modSkill;

            Battle MakeBattle(out Combatant h, out Combatant e)
            {
                var hu = new Unit(Database.Characters["mage"]);
                hu.abilities.racialModifiers = AbilityScores.Zero;
                hu.abilities.baseScores = new AbilityScores(10, 10, 10, 20, 10, 20);
                hu.RecalculateStats(true);
                hu.skillLoadout.UnlockActive(modSkill.id);
                hu.skillLoadout.activeSlots[0] = modSkill.id;
                var ed = new EnemyDef
                {
                    id = "curse_target",
                    name = "Target",
                    shortName = "Target",
                    tag = "TGT",
                    tier = Tier.Normal,
                    stats = new Stats(maxHp: 1000, maxMp: 10, atk: 10, def: 10, spd: 1, mag: 10)
                };
                var b = new Battle(new[] { hu }, new[] { ed }, new List<string>(), new System.Random(7));
                h = b.NextTurn();
                e = b.Enemies[0];
                e.StatusController.ResistanceProfile.baseResistance = 0f;
                e.StatusController.ResistanceProfile.statsResistance = 0f;
                e.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
                return b;
            }

            // Legacy basic attack through Battle.Deal
            var bBase = MakeBattle(out var h1, out var e1);
            int legacyBase = bBase.Perform(BattleAction.Attack(h1, e1), 1f)[0].amount;

            var bCursed = MakeBattle(out var h2, out var e2);
            bCursed.Apply(e2, new StatusApply { id = StatusId.Curse, turns = 3, chance = 1f }, h2, 1f);
            int legacyCursed = bCursed.Perform(BattleAction.Attack(h2, e2), 1f)[0].amount;
            Assert.AreEqual((int)Math.Round(legacyBase * 1.25f), legacyCursed, "Legacy Battle.Deal must apply +25% Curse damage");

            // Modular skill through Battle.Perform -> applied ONCE (not 1.25 * 1.25)
            var bModBase = MakeBattle(out var h3, out var e3);
            int modBase = bModBase.Perform(BattleAction.Skill(h3, modSkill, e3), 1f)[0].amount;

            var bModCursed = MakeBattle(out var h4, out var e4);
            bModCursed.Apply(e4, new StatusApply { id = StatusId.Curse, turns = 3, chance = 1f }, h4, 1f);
            int modCursed = bModCursed.Perform(BattleAction.Skill(h4, modSkill, e4), 1f)[0].amount;
            Assert.AreEqual((int)Math.Floor(modBase * 1.25f), modCursed, "Modular skill must apply Curse +25% exactly once");
        }

        [Test]
        public void Despair_BlocksMpRecovery_AndResumesAfterCleanse()
        {
            var heroUnit = new Unit(Database.Characters["mage"]);
            var enemyDef = new EnemyDef
            {
                id = "despair_foe",
                name = "Wraith",
                shortName = "Wraith",
                tag = "WRT",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 300, maxMp: 20, atk: 10, def: 5, spd: 1, mag: 10)
            };
            var manaItem = new ItemDef { id = "test_ether", name = "Test Ether", mana = 15 };
            Database.Items[manaItem.id] = manaItem;

            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef }, new List<string> { manaItem.id }, new System.Random(1));
            var hero = battle.NextTurn();
            var foe = battle.Enemies[0];
            hero.StatusController.ResistanceProfile.baseResistance = 0f;
            hero.StatusController.ResistanceProfile.statsResistance = 0f;
            hero.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
            hero.unit.mp = 5;

            battle.Apply(hero, new StatusApply { id = StatusId.Despair, turns = 3, magnitude = 0f, chance = 1f }, foe, 1f);
            Assert.IsFalse(hero.StatusController.CanRestoreMp());

            // 1. Direct RestoreMp is blocked
            int gained = battle.RestoreMp(hero, 10);
            Assert.AreEqual(0, gained);
            Assert.AreEqual(5, hero.unit.mp);

            // 2. Mana item is blocked (pass 0% disruption magnitude via Despair instance to ensure action executes)
            hero.StatusController.GetFirst(StatusId.Despair).magnitude = 0.0001f;
            // Use RNG seed that won't disrupt (< 0.0001f)
            battle.Perform(BattleAction.Item(hero, manaItem, hero));
            Assert.AreEqual(5, hero.unit.mp, "Mana item must not restore MP while Despaired");

            // 3. Perfect Offensive QTE MP refund is blocked
            var nextHero = battle.NextTurn();
            if (nextHero == foe)
            {
                battle.Perform(BattleAction.Attack(foe, hero));
                nextHero = battle.NextTurn();
            }
            Assert.AreSame(hero, nextHero);
            hero.StatusController.GetFirst(StatusId.Despair).magnitude = 0.0001f;
            battle.Perform(BattleAction.Attack(hero, foe), power: QteConfig.PerfectPower);
            Assert.AreEqual(5, hero.unit.mp, "Perfect QTE MP refund must be blocked while Despaired");

            // 4. After cleansing Despair, MP recovery works again
            hero.StatusController.CleanseMentalEffects();
            Assert.IsTrue(hero.StatusController.CanRestoreMp());
            int restored = battle.RestoreMp(hero, 10);
            Assert.Greater(restored, 0);
            Assert.AreEqual(5 + restored, hero.unit.mp);
        }

        [Test]
        public void Despair_BlocksNewBuffs_PreservesExistingBuffs_AndKeepsActionDisruption()
        {
            var heroUnit = new Unit(Database.Characters["knight"]);
            var enemyDef = new EnemyDef
            {
                id = "despair_foe_2",
                name = "Wraith",
                shortName = "Wraith",
                tag = "WRT",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 200, maxMp: 10, atk: 10, def: 5, spd: 5, mag: 10)
            };
            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef }, new List<string>(), new System.Random(2));
            var hero = battle.Party[0];
            var foe = battle.Enemies[0];
            hero.StatusController.ResistanceProfile.baseResistance = 0f;
            hero.StatusController.ResistanceProfile.statsResistance = 0f;
            hero.StatusController.ResistanceProfile.mentalStatsResistance = 0f;

            // Pre-existing buff before Despair
            battle.Apply(hero, new StatusApply { id = StatusId.DefenseUp, turns = 3, magnitude = 0.25f, chance = 1f }, hero, 1f);
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.DefenseUp));

            // Apply Despair
            battle.Apply(hero, new StatusApply { id = StatusId.Despair, turns = 3, magnitude = 0.35f, chance = 1f }, foe, 1f);
            Assert.IsFalse(hero.StatusController.CanGainBuffs());
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.DefenseUp), "Existing buffs must remain active");

            // New buff via Battle.Apply is blocked
            battle.Apply(hero, new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.25f, chance = 1f }, hero, 1f);
            Assert.IsFalse(hero.StatusController.HasStatus(StatusId.AttackUp), "Battle.Apply must block new buffs under Despair");
            Assert.IsFalse(hero.Has(StatusId.AttackUp));

            // Direct AddStatus and TryApplyStatus also block new buffs
            hero.AddStatus(StatusId.Evasion, 2, 1.25f, hero);
            Assert.IsFalse(hero.Has(StatusId.Evasion), "Combatant.AddStatus must block new buffs under Despair");
            Assert.IsFalse(hero.StatusController.TryApplyStatus(new StatusApply { id = StatusId.MagicDefenseUp, turns = 2, magnitude = 0.2f, chance = 1f }, hero, hero, out _));

            // Debuffs still apply normally
            battle.Apply(hero, new StatusApply { id = StatusId.Burn, turns = 2, chance = 1f }, foe, 1f);
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.Burn), "Debuffs must still apply under Despair");

            // Action disruption still triggers on low roll and passes on high roll
            var dummyAction = BattleAction.Attack(hero, foe);
            bool disruptedLow = hero.StatusController.CheckActionDisruption(dummyAction, new FixedRandom(0.10), out var reason);
            Assert.IsTrue(disruptedLow, "Despair must still disrupt actions on roll < 0.35");
            StringAssert.Contains("Despair", reason);
            bool disruptedHigh = hero.StatusController.CheckActionDisruption(dummyAction, new FixedRandom(0.90), out _);
            Assert.IsFalse(disruptedHigh, "Despair should not disrupt actions on roll >= 0.35");

            // Cleansing Despair re-enables buffs
            hero.StatusController.CleanseMentalEffects();
            battle.Apply(hero, new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.25f, chance = 1f }, hero, 1f);
            Assert.IsTrue(hero.StatusController.HasStatus(StatusId.AttackUp));
        }

        [Test]
        public void MindControl_HeroAttacksAlly_AndHitsSelfWhenAlone()
        {
            // Case 1: 2 heroes + 1 enemy -> Mind Controlled hero attacks living ally
            var h1Unit = new Unit(Database.Characters["knight"]);
            var h2Unit = new Unit(Database.Characters["mage"]);
            var foeDef = new EnemyDef
            {
                id = "mc_siren",
                name = "Siren",
                shortName = "Siren",
                tag = "SRN",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 200, maxMp: 20, atk: 10, def: 5, spd: 1, mag: 10)
            };
            var battle = new Battle(new[] { h1Unit, h2Unit }, new[] { foeDef }, new List<string>(), new System.Random(10));
            var hero1 = battle.NextTurn();
            var ally = battle.Party.First(p => p != hero1);
            var foe = battle.Enemies[0];
            hero1.StatusController.ResistanceProfile.baseResistance = 0f;
            hero1.StatusController.ResistanceProfile.statsResistance = 0f;
            hero1.StatusController.ResistanceProfile.mentalStatsResistance = 0f;

            battle.Apply(hero1, new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, foe, 1f);
            Assert.IsTrue(hero1.IsMindControlled);

            var mcAction = battle.BuildMindControlledAction(hero1);
            Assert.AreEqual(ActionKind.Attack, mcAction.kind);
            Assert.AreSame(ally, mcAction.target, "Mind Controlled hero must target a living ally when one exists");
            Assert.IsNull(battle.Validate(mcAction), "Battle.Validate must accept Mind Control friendly-fire attack");

            int allyHpBefore = ally.unit.hp;
            int foeHpBefore = foe.unit.hp;
            battle.Perform(mcAction);
            Assert.Less(ally.unit.hp, allyHpBefore, "Ally must take damage from Mind Controlled hero");
            Assert.AreEqual(foeHpBefore, foe.unit.hp, "Enemy must not take damage from Mind Controlled hero");

            // Also verify direct foe-targeted BattleAction gets redirected by TryRedirectAction
            var battle2 = new Battle(new[] { new Unit(Database.Characters["knight"]), new Unit(Database.Characters["mage"]) }, new[] { foeDef }, new List<string>(), new System.Random(11));
            var a2 = battle2.NextTurn();
            var ally2 = battle2.Party.First(p => p != a2);
            var foe2 = battle2.Enemies[0];
            a2.StatusController.ResistanceProfile.baseResistance = 0f;
            a2.StatusController.ResistanceProfile.statsResistance = 0f;
            a2.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
            battle2.Apply(a2, new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, foe2, 1f);

            int ally2Before = ally2.unit.hp;
            int foe2Before = foe2.unit.hp;
            battle2.Perform(BattleAction.Attack(a2, foe2));
            Assert.Less(ally2.unit.hp, ally2Before, "TryRedirectAction must redirect foe-targeted attack to ally");
            Assert.AreEqual(foe2Before, foe2.unit.hp);

            // Case 2: Lone hero -> hits self cleanly
            var soloBattle = new Battle(new[] { new Unit(Database.Characters["knight"]) }, new[] { foeDef }, new List<string>(), new System.Random(12));
            var soloHero = soloBattle.NextTurn();
            var soloFoe = soloBattle.Enemies[0];
            soloHero.StatusController.ResistanceProfile.baseResistance = 0f;
            soloHero.StatusController.ResistanceProfile.statsResistance = 0f;
            soloHero.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
            soloBattle.Apply(soloHero, new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, soloFoe, 1f);

            var selfAct = soloBattle.BuildMindControlledAction(soloHero);
            Assert.AreSame(soloHero, selfAct.target, "Lone Mind Controlled hero must target self");
            Assert.IsNull(soloBattle.Validate(selfAct));
            int soloHpBefore = soloHero.unit.hp;
            soloBattle.Perform(selfAct);
            Assert.Less(soloHero.unit.hp, soloHpBefore, "Lone Mind Controlled hero must damage self");
        }

        [Test]
        public void MindControl_EnemyAttacksOtherEnemy_OrSelfWhenAlone_WithNoDefenseQte()
        {
            var heroUnit = new Unit(Database.Characters["knight"]);
            var enemyDef = new EnemyDef
            {
                id = "mc_goblin",
                name = "Goblin",
                shortName = "Goblin",
                tag = "GOB",
                tier = Tier.Normal,
                ai = AiKind.Priority,
                stats = new Stats(maxHp: 200, maxMp: 20, atk: 18, def: 4, spd: 30, mag: 10),
                attacks = new[]
                {
                    new EnemyAttack { name = "Slash", element = Element.Physical, hitDamage = new[] { 18 }, aiPriority = 1 }
                }
            };

            // Case 1: 2 enemies -> controlled enemy attacks the other enemy, no Defense QTE, Plan skipped
            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef, enemyDef }, new List<string>(), new System.Random(20));
            var e1 = battle.NextTurn();
            Assert.IsTrue(e1.IsEnemy);
            var e2 = battle.Enemies.First(e => e != e1);
            var hero = battle.Party[0];
            e1.StatusController.ResistanceProfile.baseResistance = 0f;
            e1.StatusController.ResistanceProfile.statsResistance = 0f;
            e1.StatusController.ResistanceProfile.mentalStatsResistance = 0f;

            battle.Apply(e1, new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, hero, 1f);
            Assert.IsNull(e1.Intent, "Applying Mind Control must clear existing enemy intent");
            EnemyAI.Plan(battle, e1);
            Assert.IsNull(e1.Intent, "EnemyAI.Plan must not publish party-targeted intent while Mind Controlled");
            Assert.IsTrue(EnemyAI.HasLegalAction(battle, e1), "Mind Controlled enemy must always have a legal friendly-fire action");

            var enemyAct = EnemyAI.Choose(battle, e1);
            Assert.AreSame(e2, enemyAct.target, "Mind Controlled enemy must target another living enemy");
            Assert.IsNull(QteFactory.Defense(enemyAct, QteAssist.Standard, null, new System.Random(1)),
                "Enemy-on-enemy Mind Control attack must not spawn a player Defensive QTE");

            int e2Before = e2.unit.hp;
            int heroBefore = hero.unit.hp;
            battle.Perform(enemyAct);
            Assert.Less(e2.unit.hp, e2Before, "Other enemy must take damage from Mind Controlled ally");
            Assert.AreEqual(heroBefore, hero.unit.hp, "Hero must not take damage from Mind Controlled enemy");

            // Case 2: Lone enemy -> hits self
            var soloBattle = new Battle(new[] { new Unit(Database.Characters["knight"]) }, new[] { enemyDef }, new List<string>(), new System.Random(21));
            var soloEnemy = soloBattle.NextTurn();
            var soloHero = soloBattle.Party[0];
            soloEnemy.StatusController.ResistanceProfile.baseResistance = 0f;
            soloEnemy.StatusController.ResistanceProfile.statsResistance = 0f;
            soloEnemy.StatusController.ResistanceProfile.mentalStatsResistance = 0f;
            soloBattle.Apply(soloEnemy, new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, soloHero, 1f);

            Assert.IsTrue(EnemyAI.HasLegalAction(soloBattle, soloEnemy));
            var soloAct = EnemyAI.Choose(soloBattle, soloEnemy);
            Assert.AreSame(soloEnemy, soloAct.target, "Lone Mind Controlled enemy must target self");
            int soloBefore = soloEnemy.unit.hp;
            soloBattle.Perform(soloAct);
            Assert.Less(soloEnemy.unit.hp, soloBefore);
        }

        [Test]
        public void MindControl_DefaultDurationLastsOneControlledTurn_GrantsControlImmunity_AndBossesStayImmune()
        {
            Assert.AreEqual(2, StatusEffectDefs.Get(StatusId.MindControl).defaultDuration,
                "Default Mind Control duration must be 2 so StartTurn tick leaves 1 controlled turn active");

            var heroUnit = new Unit(Database.Characters["mage"]);
            heroUnit.abilities.racialModifiers = AbilityScores.Zero;
            heroUnit.abilities.baseScores = new AbilityScores(10, 10, 10, 15, 10, 30); // fast hero
            heroUnit.RecalculateStats(true);

            var enemyDef = new EnemyDef
            {
                id = "mc_lifecycle_foe",
                name = "Bandit",
                shortName = "Bandit",
                tag = "BND",
                tier = Tier.Normal,
                stats = new Stats(maxHp: 300, maxMp: 10, atk: 12, def: 4, spd: 5, mag: 5)
            };
            var battle = new Battle(new[] { heroUnit }, new[] { enemyDef }, new List<string>(), new System.Random(30));
            var hero = battle.NextTurn();
            var enemy = battle.Enemies[0];
            enemy.StatusController.ResistanceProfile.baseResistance = 0f;
            enemy.StatusController.ResistanceProfile.statsResistance = 0f;
            enemy.StatusController.ResistanceProfile.mentalStatsResistance = 0f;

            // Apply Mind Control using defaultDuration (turns = 0)
            battle.Apply(enemy, new StatusApply { id = StatusId.MindControl, turns = 0, chance = 1f }, hero, 1f);
            Assert.IsTrue(enemy.IsMindControlled);
            battle.Perform(BattleAction.Guard(hero));

            // Turn 1 of enemy: StartTurn ticks 2 -> 1, enemy is STILL Mind Controlled and hits self
            var turn1 = battle.NextTurn();
            Assert.AreSame(enemy, turn1);
            Assert.IsTrue(enemy.IsMindControlled, "Enemy must still be Mind Controlled on its turn after StartTurn tick");
            var act1 = EnemyAI.Choose(battle, enemy);
            Assert.AreSame(enemy, act1.target);
            battle.Perform(act1);

            // Round 2: Hero acts, then Enemy's StartTurn ticks 1 -> 0 -> expires & grants Control Immunity
            var hTurn2 = battle.NextTurn();
            Assert.AreSame(hero, hTurn2);
            battle.Perform(BattleAction.Guard(hero));

            var turn2 = battle.NextTurn();
            Assert.AreSame(enemy, turn2);
            Assert.IsFalse(enemy.IsMindControlled, "Mind Control must expire on the following turn");
            Assert.IsTrue(enemy.StatusController.HasControlImmunity(StatusId.MindControl), "Expiry must grant 1-turn Control Immunity");

            // Immediate re-application must fail during Control Immunity
            battle.Apply(enemy, new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, hero, 1f);
            Assert.IsFalse(enemy.IsMindControlled);

            // Bosses remain immune to Mind Control
            var boss = CreateMockEnemy("Boss", isBoss: true);
            Assert.IsFalse(boss.StatusController.TryApplyStatus(new StatusApply { id = StatusId.MindControl, turns = 2, chance = 1f }, hero, boss, out _));
        }

        private class FixedRandom : System.Random
        {
            private readonly double value;
            public FixedRandom(double value) => this.value = value;
            public override double NextDouble() => value;
        }
    }
}
