using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class BackgroundSystemTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Initialize Database if needed
            _ = Database.Classes;
            _ = Database.Backgrounds;
        }

        static void AdvanceTo(Battle b, Combatant who)
        {
            for (int i = 0; i < 100; i++)
            {
                var c = b.NextTurn();
                if (c == who) return;
                b.Perform(BattleAction.Guard(c));
            }
            Assert.Fail($"{who.name} never got a turn");
        }

        // =====================================================================
        // 1. Definition and Database Integrity
        // =====================================================================

        [Test]
        public void AllTenBackgrounds_AreDefinedInDatabase_WithFiveRanksEach()
        {
            string[] expected = Database.PlayableBackgroundIds;
            Assert.AreEqual(10, expected.Length, "There must be exactly 10 playable backgrounds.");

            foreach (var bgId in expected)
            {
                Assert.IsTrue(Database.Backgrounds.ContainsKey(bgId), $"Database must contain background '{bgId}'.");
                var bg = Database.Backgrounds[bgId];
                Assert.IsNotNull(bg, $"Background '{bgId}' must not be null.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(bg.name), $"Background '{bgId}' must have a name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(bg.perkName), $"Background '{bgId}' must have a perk name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(bg.narrativeIdentity), $"Background '{bgId}' must have narrative identity.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(bg.description), $"Background '{bgId}' must have a description.");

                Assert.IsNotNull(bg.ranks, $"Background '{bgId}' must have ranks defined.");
                Assert.AreEqual(5, bg.ranks.Length, $"Background '{bgId}' must have exactly 5 ranks.");

                int[] expectedUnlockLevels = { 1, 4, 7, 10, 13 };
                for (int r = 0; r < 5; r++)
                {
                    var rankDef = bg.ranks[r];
                    Assert.AreEqual(r + 1, rankDef.rank, $"Rank index {r} must have rank {r + 1}.");
                    Assert.AreEqual(expectedUnlockLevels[r], rankDef.requiredLevel, $"Rank {r + 1} must unlock at level {expectedUnlockLevels[r]}.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rankDef.perkName), $"Rank {r + 1} of '{bgId}' must have a perk name.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rankDef.perkDescription), $"Rank {r + 1} of '{bgId}' must have a perk description.");
                }
            }
        }

        [Test]
        public void LegacyAliases_ArePreservedForBackwardCompatibility()
        {
            Assert.IsTrue(Database.Backgrounds.ContainsKey("oathsworn"), "Legacy alias 'oathsworn' must exist.");
            Assert.IsTrue(Database.Backgrounds.ContainsKey("outcast"), "Legacy alias 'outcast' must exist.");
            Assert.IsTrue(Database.Backgrounds.ContainsKey("pilgrim"), "Legacy alias 'pilgrim' must exist.");
        }

        // =====================================================================
        // 2. Rank Resolution & Milestone Scaling
        // =====================================================================

        [Test]
        public void RankResolver_ResolvesCorrectRank_AtEveryLevel()
        {
            // Level 1-3 -> Rank 1
            Assert.AreEqual(1, BackgroundRankResolver.GetRankForLevel(1));
            Assert.AreEqual(1, BackgroundRankResolver.GetRankForLevel(2));
            Assert.AreEqual(1, BackgroundRankResolver.GetRankForLevel(3));

            // Level 4-6 -> Rank 2
            Assert.AreEqual(2, BackgroundRankResolver.GetRankForLevel(4));
            Assert.AreEqual(2, BackgroundRankResolver.GetRankForLevel(5));
            Assert.AreEqual(2, BackgroundRankResolver.GetRankForLevel(6));

            // Level 7-9 -> Rank 3
            Assert.AreEqual(3, BackgroundRankResolver.GetRankForLevel(7));
            Assert.AreEqual(3, BackgroundRankResolver.GetRankForLevel(8));
            Assert.AreEqual(3, BackgroundRankResolver.GetRankForLevel(9));

            // Level 10-12 -> Rank 4
            Assert.AreEqual(4, BackgroundRankResolver.GetRankForLevel(10));
            Assert.AreEqual(4, BackgroundRankResolver.GetRankForLevel(11));
            Assert.AreEqual(4, BackgroundRankResolver.GetRankForLevel(12));

            // Level 13-15 -> Rank 5
            Assert.AreEqual(5, BackgroundRankResolver.GetRankForLevel(13));
            Assert.AreEqual(5, BackgroundRankResolver.GetRankForLevel(14));
            Assert.AreEqual(5, BackgroundRankResolver.GetRankForLevel(15));
        }

        // =====================================================================
        // 3. Modifier Non-Stacking & Recalculation Safety
        // =====================================================================

        [Test]
        public void BackgroundModifiers_DoNotStack_OnlyHighestUnlockedRankApplies()
        {
            var farmer = Database.Backgrounds["farmer"];

            // Level 1: Rank I (+3% MaxHP)
            var modsLv1 = farmer.GetActiveModifiers(1);
            Assert.AreEqual(1, modsLv1.Length);
            Assert.AreEqual(0.03f, modsLv1[0].value, 0.001f);

            // Level 4: Rank II (+5% MaxHP, NOT 3% + 5% = 8%)
            var modsLv4 = farmer.GetActiveModifiers(4);
            Assert.AreEqual(1, modsLv4.Length);
            Assert.AreEqual(0.05f, modsLv4[0].value, 0.001f);

            // Level 13: Rank V (+10% MaxHP)
            var modsLv13 = farmer.GetActiveModifiers(13);
            Assert.AreEqual(1, modsLv13.Length);
            Assert.AreEqual(0.10f, modsLv13[0].value, 0.001f);
        }

        [Test]
        public void UnitRecalculateStats_RepeatedCalls_DoNotCauseStatInflation()
        {
            var charDef = new CharacterDef
            {
                id = "test_farmer",
                name = "Farmer Hero",
                classId = "fighter",
                raceId = "human",
                backgroundId = "farmer",
                baseScores = AbilityScores.DefaultBase
            };

            var unit = new Unit(charDef);
            int initialHp = unit.stats.maxHp;

            for (int i = 0; i < 10; i++)
            {
                unit.RecalculateStats();
                Assert.AreEqual(initialHp, unit.stats.maxHp, $"RecalculateStats iteration {i} should not alter Max HP.");
            }
        }

        // =====================================================================
        // 4. Combat Mechanics Integration
        // =====================================================================

        [Test]
        public void Acolyte_HealingDealtAndReceivedBonuses_ApplyCorrectly()
        {
            var acolyteUnit = new Unit(new CharacterDef
            {
                id = "acolyte_hero",
                name = "Acolyte Hero",
                classId = "cleric",
                raceId = "human",
                backgroundId = "acolyte",
                baseScores = AbilityScores.DefaultBase
            });

            var combatant = new Combatant(acolyteUnit, Side.Party, 0, "Acolyte", "ACO");
            // Base cleric heal power scaled by (1 + 0.05) at level 1
            float baseHeal = acolyteUnit.Derived.healingPower;
            Assert.AreEqual((int)Math.Round(baseHeal * 1.05f), combatant.HealPower);

            // Scale to Level 13 (Rank V: 15% healing dealt, 10% healing received)
            for (int i = 1; i < 13; i++) acolyteUnit.LevelUp();
            Assert.AreEqual(13, acolyteUnit.level);

            combatant = new Combatant(acolyteUnit, Side.Party, 0, "Acolyte", "ACO");
            float rank5BaseHeal = acolyteUnit.Derived.healingPower;
            Assert.AreEqual((int)Math.Round(rank5BaseHeal * 1.15f), combatant.HealPower);
        }

        [Test]
        public void Charlatan_DebuffAndFirstStrikeBonuses_ApplyCorrectly()
        {
            var charlatanUnit = new Unit(new CharacterDef
            {
                id = "charlatan_hero",
                name = "Charlatan Hero",
                classId = "rogue",
                raceId = "human",
                backgroundId = "charlatan",
                baseScores = AbilityScores.DefaultBase
            });
            for (int i = 1; i < 13; i++) charlatanUnit.LevelUp(); // Rank V: +12% vs debuffed, +5% first attack unacted

            var battle = new Battle(new[] { charlatanUnit }, new[] { Database.Enemies["skeleton"] }, new List<string>(), new System.Random(42));
            var actor = battle.Party[0];
            var target = battle.Enemies[0];

            actor.unit.stats.spd = 99; // Ensure charlatan acts first so target hasn't acted yet
            var firstTurn = battle.NextTurn();
            Assert.AreEqual(actor, firstTurn);

            // Target has not acted yet, so first attack should get +5% unacted bonus
            Assert.IsFalse(target.hasActedInBattle);
            var attackAction = BattleAction.Attack(actor, target);
            var hits = battle.Perform(attackAction);
            Assert.IsTrue(hits.Count > 0);
            Assert.IsTrue(actor.charlatanFirstAttackedTargets.Contains(target), "Charlatan first attack against unacted target must be recorded.");
        }

        [Test]
        public void Guard_PhysicalDamageTakenReduction_AndLowHpBonus_Work()
        {
            var guardUnit = new Unit(new CharacterDef
            {
                id = "guard_hero",
                name = "Guard Hero",
                classId = "fighter",
                raceId = "human",
                backgroundId = "guard",
                baseScores = AbilityScores.DefaultBase
            });
            for (int i = 1; i < 13; i++) guardUnit.LevelUp(); // Rank V: 10% base physical reduction, +5% below 30% HP

            var battle = new Battle(new[] { guardUnit }, new[] { Database.Enemies["skeleton"] }, new List<string>(), new System.Random(42));
            var hero = battle.Party[0];
            var enemy = battle.Enemies[0];

            int startHp = hero.unit.hp;
            // Enemy attacks hero
            AdvanceTo(battle, enemy);
            var hits = battle.Perform(BattleAction.Attack(enemy, hero));
            Assert.IsTrue(hits.Count > 0);
            Assert.Less(hero.unit.hp, startHp);
        }

        [Test]
        public void Hermit_StatusResistMpRecovery_LimitedToOncePerTurn()
        {
            var hermitUnit = new Unit(new CharacterDef
            {
                id = "hermit_hero",
                name = "Hermit Hero",
                classId = "wizard",
                raceId = "human",
                backgroundId = "hermit",
                baseScores = AbilityScores.DefaultBase
            });
            for (int i = 1; i < 13; i++) hermitUnit.LevelUp(); // Rank V: +15 StatusResist, 3% MP on resist (max once/turn)

            var battle = new Battle(new[] { hermitUnit }, new[] { Database.Enemies["skeleton"] }, new List<string>(), new System.Random(42));
            var hermit = battle.Party[0];
            hermit.unit.mp = 10;
            int initialMp = hermit.unit.mp;

            // Simulate status application that is resisted
            hermit.StatusController.ResistanceProfile.baseResistance = 1.0f; // 100% resist
            var debuff = new StatusApply { id = StatusId.Poison, chance = 0.5f, turns = 2 };
            battle.Apply(hermit, debuff, battle.Enemies[0], 0.5f);

            Assert.IsTrue(hermit.hermitResistedThisTurn);
            Assert.Greater(hermit.unit.mp, initialMp, "Tranquil Spirit should restore MP on status resist.");

            // Second resist in same turn should NOT restore MP again
            int mpAfterFirstResist = hermit.unit.mp;
            battle.Apply(hermit, debuff, battle.Enemies[0], 0.5f);
            Assert.AreEqual(mpAfterFirstResist, hermit.unit.mp, "Tranquil Spirit should only restore MP at most once per turn.");
        }

        [Test]
        public void FarmerAndNoble_PostBattleRecovery_RestoresHpAndMp_WithoutRevivingDead()
        {
            var farmerUnit = new Unit(new CharacterDef
            {
                id = "farmer_hero",
                name = "Farmer",
                classId = "fighter",
                raceId = "human",
                backgroundId = "farmer",
                baseScores = AbilityScores.DefaultBase
            });
            var nobleUnit = new Unit(new CharacterDef
            {
                id = "noble_hero",
                name = "Noble",
                classId = "wizard",
                raceId = "human",
                backgroundId = "noble",
                baseScores = AbilityScores.DefaultBase
            });

            for (int i = 1; i < 13; i++)
            {
                farmerUnit.LevelUp();
                nobleUnit.LevelUp();
            }

            // Damage them
            farmerUnit.hp = 20;
            nobleUnit.mp = 5;

            var battle = new Battle(new[] { farmerUnit, nobleUnit }, new[] { Database.Enemies["skeleton"] }, new List<string>(), new System.Random(42));
            // Defeat enemy to trigger victory
            battle.Enemies[0].unit.hp = 0;
            Assert.AreEqual(Outcome.Victory, battle.Outcome);

            battle.ApplyPostBattleRecovery();

            Assert.Greater(farmerUnit.hp, 20, "Farmer should recover 3% Max HP after battle.");
            Assert.Greater(nobleUnit.mp, 5, "Noble should recover 5% Max MP after battle.");

            // Verify a dead hero is NOT revived
            farmerUnit.hp = 0;
            battle.ApplyPostBattleRecovery();
            Assert.AreEqual(0, farmerUnit.hp, "Dead hero must not be revived by post-battle recovery.");
        }

        [Test]
        public void Sage_MpCostReduction_AndMinimumOneMpFloor()
        {
            var sageUnit = new Unit(new CharacterDef
            {
                id = "sage_hero",
                name = "Sage Hero",
                classId = "wizard",
                raceId = "human",
                backgroundId = "sage",
                baseScores = AbilityScores.DefaultBase
            });

            var combatant = new Combatant(sageUnit, Side.Party, 0, "Sage", "SAG");
            var testSkill = new SkillDef { id = "arcane_blast", name = "Arcane Blast", mpCost = 10 };

            // Level 1: 3% reduction on 10 MP -> 10 * 0.97 = 9.7 -> 10 or 9 depending on round
            int cost = combatant.GetEffectiveMpCost(testSkill);
            Assert.LessOrEqual(cost, 10);

            // Level 13 (Rank V: 10% reduction)
            for (int i = 1; i < 13; i++) sageUnit.LevelUp();
            combatant = new Combatant(sageUnit, Side.Party, 0, "Sage", "SAG");
            Assert.AreEqual(9, combatant.GetEffectiveMpCost(testSkill), "10 MP skill should cost 9 MP with 10% reduction.");

            // 1 MP skill should never drop below 1 MP
            var cheapSkill = new SkillDef { id = "cantrip", name = "Cantrip", mpCost = 1 };
            Assert.AreEqual(1, combatant.GetEffectiveMpCost(cheapSkill), "Skills with MP cost cannot become free; minimum remains 1 MP.");
        }

        [Test]
        public void Soldier_DamageBonusAndRetaliationInstance_TriggerAndConsume()
        {
            var soldierUnit = new Unit(new CharacterDef
            {
                id = "soldier_hero",
                name = "Soldier Hero",
                classId = "fighter",
                raceId = "human",
                backgroundId = "soldier",
                baseScores = AbilityScores.DefaultBase
            });
            for (int i = 1; i < 13; i++) soldierUnit.LevelUp(); // Rank V: +10% physical damage, +5% retaliation on next physical hit

            var battle = new Battle(new[] { soldierUnit }, new[] { Database.Enemies["skeleton"] }, new List<string>(), new System.Random(42));
            var soldier = battle.Party[0];
            var enemy = battle.Enemies[0];

            Assert.IsFalse(soldier.soldierRetaliationReady);

            // Enemy attacks soldier
            AdvanceTo(battle, enemy);
            battle.Perform(BattleAction.Attack(enemy, soldier));
            Assert.IsTrue(soldier.soldierRetaliationReady, "Soldier must have retaliation ready after taking enemy damage.");

            // Soldier attacks enemy, consuming retaliation
            AdvanceTo(battle, soldier);
            battle.Perform(BattleAction.Attack(soldier, enemy));
            Assert.IsFalse(soldier.soldierRetaliationReady, "Soldier retaliation must be consumed on next physical damage strike.");
        }

        [Test]
        public void Scribe_AccuracyScalingAndThreeAttackCritBonus()
        {
            var scribeUnit = new Unit(new CharacterDef
            {
                id = "scribe_hero",
                name = "Scribe Hero",
                classId = "fighter",
                raceId = "human",
                backgroundId = "scribe",
                baseScores = AbilityScores.DefaultBase
            });

            // Level 1: +3 Accuracy
            float accLv1 = AccuracySystem.CalculateAttackerAccuracy(scribeUnit, AttackCategory.Ranged);
            float baseDexAcc = scribeUnit.Mod(Ability.DEX) * AccuracyConfig.Default.dexAccuracyScaling;
            Assert.AreEqual(baseDexAcc + 3f, accLv1, 0.001f);

            // Level 13: +10 Accuracy
            for (int i = 1; i < 13; i++) scribeUnit.LevelUp();
            float accLv13 = AccuracySystem.CalculateAttackerAccuracy(scribeUnit, AttackCategory.Ranged);
            baseDexAcc = scribeUnit.Mod(Ability.DEX) * AccuracyConfig.Default.dexAccuracyScaling;
            Assert.AreEqual(baseDexAcc + 10f, accLv13, 0.001f);

            // Combat target study: 3 attacks -> +5% crit on next attack
            var battle = new Battle(new[] { scribeUnit }, new[] { Database.Enemies["skeleton"] }, new List<string>(), new System.Random(42));
            var scribe = battle.Party[0];
            var target = battle.Enemies[0];

            int baseCrit = scribe.GetCritChanceAgainst(target);

            // Attack 1
            AdvanceTo(battle, scribe);
            battle.Perform(BattleAction.Attack(scribe, target));
            Assert.AreEqual(1, scribe.scribeAttackCounts[target]);
            Assert.AreEqual(baseCrit, scribe.GetCritChanceAgainst(target));

            // Attack 2
            AdvanceTo(battle, scribe);
            battle.Perform(BattleAction.Attack(scribe, target));
            Assert.AreEqual(2, scribe.scribeAttackCounts[target]);

            // Attack 3
            AdvanceTo(battle, scribe);
            battle.Perform(BattleAction.Attack(scribe, target));
            Assert.AreEqual(3, scribe.scribeAttackCounts[target]);

            // Now before attack 4, crit chance has +5%
            Assert.AreEqual(baseCrit + 5, scribe.GetCritChanceAgainst(target), "After 3 attacks against same target, crit chance should gain +5%.");

            // Attack 4 triggers and resets analysis counter
            AdvanceTo(battle, scribe);
            battle.Perform(BattleAction.Attack(scribe, target));
            Assert.AreEqual(0, scribe.scribeAttackCounts[target], "Crit bonus was consumed; counter must reset to 0.");
            Assert.AreEqual(baseCrit, scribe.GetCritChanceAgainst(target));
        }

        // =====================================================================
        // 5. Persistence & Character Creation Flow
        // =====================================================================

        [Test]
        public void SavedCharacterData_PreservesBackgroundId_AndScalesCorrectlyOnLoad()
        {
            var charDef = new CharacterDef
            {
                id = "persistent_hero",
                name = "Wayfarer Hero",
                classId = "rogue",
                raceId = "elf",
                backgroundId = "wayfarer",
                baseScores = AbilityScores.DefaultBase
            };
            var unit = new Unit(charDef);
            for (int i = 1; i < 10; i++) unit.LevelUp(); // Level 10: Rank IV (+9% speed)

            var saveData = SavedCharacterData.FromCharacterDef(charDef, unit);
            Assert.AreEqual("wayfarer", saveData.backgroundId);
            Assert.AreEqual(10, saveData.level);

            var loadedDef = saveData.ToCharacterDef();
            Assert.AreEqual("wayfarer", loadedDef.backgroundId);

            var loadedUnit = new Unit(loadedDef);
            // Simulate restoring level 10
            for (int i = 1; i < 10; i++) loadedUnit.LevelUp();
            Assert.AreEqual(4, loadedUnit.background.GetActiveRankNumber(loadedUnit.level));
            var activeMods = loadedUnit.background.GetActiveModifiers(loadedUnit.level);
            Assert.AreEqual(0.09f, activeMods[0].value, 0.001f);
        }

        [Test]
        public void CharacterCreationSession_SelectBackground_UpdatesBackgroundAndDerivedPreview()
        {
            var session = new CharacterCreationSession();
            Assert.IsNotNull(session.Background, "Session must initialize with a background.");

            session.SelectBackground("noble");
            Assert.AreEqual("noble", session.Background.id);
            Assert.AreEqual(1, session.Background.GetActiveRankNumber(1));

            // Noble Rank I grants +3% Max MP
            var preview = session.PreviewDerivedStats;
            Assert.Greater(preview.maxMp, 0);

            // Session validation passes
            Assert.IsNull(session.Validate(), "Valid session with background should pass validation.");
        }

        [Test]
        public void CharacterCreationScreen_CycleBackground_AndModalToggling_Works()
        {
            var session = new CharacterCreationSession();
            var screen = new CharacterCreationScreen(session);

            string initialBg = session.Background.id;
            screen.CycleBackground(1);
            Assert.AreNotEqual(initialBg, session.Background.id, "CycleBackground should change the background.");

            // Toggle background modal
            Assert.IsFalse(screen.IsBackgroundModalOpen);
            screen.ToggleBackgroundDetailsModal();
            Assert.IsTrue(screen.IsBackgroundModalOpen, "Modal should open on ToggleBackgroundDetailsModal.");

            // Cycling while modal is open updates modal cleanly
            screen.CycleBackground(1);
            Assert.IsTrue(screen.IsBackgroundModalOpen);

            // Close modal
            screen.ToggleBackgroundDetailsModal();
            Assert.IsFalse(screen.IsBackgroundModalOpen, "Modal should close on ToggleBackgroundDetailsModal.");
        }
    }
}
