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
            return enemy;
        }

        // =====================================================================
        // 1. Universal Status Effect Resistance & Application Tests (GDD Section 9 & 13)
        // =====================================================================

        [Test]
        public void UniversalResistance_StacksAdditively_AndCapsAt75Percent()
        {
            var profile = new StatusResistanceProfile();
            profile.baseResistance = 0.20f;
            profile.equipmentResistance = 0.25f;
            profile.statsResistance = 0.20f;
            profile.buffResistance = 0.25f; // Uncapped = 0.90f (90%)

            Assert.AreEqual(0.90f, profile.TotalUncappedAdditive, 0.001f);
            Assert.AreEqual(0.75f, profile.InitialResistance, 0.001f);
            Assert.AreEqual(0.75f, profile.GetEffectiveResistance(), 0.001f);
        }

        [Test]
        public void ResistanceReductions_StackMultiplicatively_MatchingGDDFormulas()
        {
            // GDD Section 9 example:
            // "Status Effect Chance 80%, resistance 75% -> chance final 20%."
            var profile1 = new StatusResistanceProfile { baseResistance = 0.75f };
            float chance1 = profile1.CalculateFinalApplicationChance(0.80f, StatusId.Burn);
            Assert.AreEqual(0.20f, chance1, 0.0001f);

            // GDD Section 9 second example:
            // "Resistance 60% dengan dua debuff 20% -> 38.4% resistance; chance skill 40% -> 24.64%."
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

            // GDD Section 13: "Immunity bypass: Status Effect Resistance Reduction tidak menembus Mental Effect immunity."
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

            // GDD Section 8: "Status instances independen: instance baru tidak refresh/extend instance lama.
            // Tidak ada batas jumlah instance aktif untuk status yang sama."
            var burn1 = new StatusApply { id = StatusId.Burn, turns = 2, chance = 1f };
            var burn2 = new StatusApply { id = StatusId.Burn, turns = 3, chance = 1f };

            // Bypass resistance check by setting resistance to 0
            target.StatusController.ResistanceProfile.baseResistance = 0f;
            target.StatusController.ResistanceProfile.statsResistance = 0f;

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
    }
}
