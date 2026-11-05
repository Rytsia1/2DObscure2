using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    [TestFixture]
    public class Layer1RelicTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            // Ensure Database static constructor has run
            _ = Database.Relics.Count;
        }

        [Test]
        public void All31Relics_ExistInDatabase_WithMatchingMetadata()
        {
            Assert.AreEqual(31, Database.Layer1Relics.Count, "Layer 1 Relics count must be exactly 31.");
            Assert.IsFalse(Database.Layer1Relics.ContainsKey("R007"), "R007 (Mourner's Coin) must be removed.");
            Assert.IsFalse(Database.Relics.ContainsKey("R007"), "R007 must not exist in Database.Relics.");

            string[] expectedIds = {
                "R001", "R002", "R003", "R004", "R005", "R006", "R008", "R009", "R010",
                "R011", "R012", "R013", "R014", "R015", "R016", "R017", "R018", "R019", "R020",
                "R021", "R022", "R023", "R024", "R025", "R026", "R027", "R028", "R029", "R030",
                "R031", "R032"
            };

            Assert.AreEqual(31, expectedIds.Length);

            foreach (var id in expectedIds)
            {
                Assert.IsTrue(Database.Layer1Relics.ContainsKey(id), $"Layer1Relics must contain {id}");
                Assert.IsTrue(Database.Relics.ContainsKey(id), $"Database.Relics must contain {id}");

                var relic = Database.Relics[id];
                Assert.IsFalse(string.IsNullOrEmpty(relic.name), $"{id} must have a name");
                Assert.IsFalse(string.IsNullOrEmpty(relic.gameplayEffect), $"{id} must have gameplayEffect");
                Assert.IsFalse(string.IsNullOrEmpty(relic.designIntent), $"{id} must have designIntent");
            }

            // Verify specific key relics
            Assert.AreEqual("Ashen Signet", Database.Relics["R001"].name);
            Assert.AreEqual(RelicCategory.Offensive, Database.Relics["R001"].category);
            Assert.AreEqual(RelicProposedRarity.Common, Database.Relics["R001"].proposedRarity);

            Assert.AreEqual("Soulglass Shard", Database.Relics["R002"].name);
            Assert.AreEqual(RelicCategory.Resource, Database.Relics["R002"].category);
            Assert.AreEqual(RelicActivationLimit.Counter, Database.Relics["R002"].activationLimit);

            Assert.AreEqual("Soul-Tether Ring", Database.Relics["R009"].name);
            Assert.AreEqual(RelicProposedRarity.Epic, Database.Relics["R009"].proposedRarity);
            Assert.AreEqual(RelicActivationLimit.OncePerBattle, Database.Relics["R009"].activationLimit);

            Assert.AreEqual("Zoroa's Dull Crown", Database.Relics["R022"].name);
            Assert.AreEqual(RelicProposedRarity.Rare, Database.Relics["R022"].proposedRarity);
            Assert.AreEqual(RelicActivationLimit.StackTrigger, Database.Relics["R022"].activationLimit);

            // Demerit relics R023 - R032
            Assert.IsTrue(Database.Relics["R023"].HasDemerit);
            Assert.AreEqual("Crimson Testament", Database.Relics["R023"].name);
            Assert.IsFalse(string.IsNullOrEmpty(Database.Relics["R023"].demerit));

            Assert.IsTrue(Database.Relics["R032"].HasDemerit);
            Assert.AreEqual("Merchant's Burden", Database.Relics["R032"].name);
        }

        [Test]
        public void RelicCapacity_Max5Relics_RejectsSixthRelic()
        {
            var manager = new RelicManager();
            Assert.AreEqual(0, manager.Count);
            Assert.IsFalse(manager.IsFull);
            Assert.IsTrue(manager.CanAddRelic);

            // Add 5 distinct relics
            Assert.IsTrue(manager.TryAddRelic("R001", out var err1), err1);
            Assert.IsTrue(manager.TryAddRelic("R002", out var err2), err2);
            Assert.IsTrue(manager.TryAddRelic("R003", out var err3), err3);
            Assert.IsTrue(manager.TryAddRelic("R004", out var err4), err4);
            Assert.IsTrue(manager.TryAddRelic("R005", out var err5), err5);

            Assert.AreEqual(5, manager.Count);
            Assert.IsTrue(manager.IsFull);
            Assert.IsFalse(manager.CanAddRelic);

            // Attempting 6th relic must fail
            bool added6 = manager.TryAddRelic("R006", out string error6);
            Assert.IsFalse(added6);
            Assert.AreEqual(5, manager.Count);
            StringAssert.Contains("Relic capacity is full (5/5)", error6);
        }

        [Test]
        public void DuplicateRelic_IsRejectedWithoutCreatingInstance()
        {
            var manager = new RelicManager();
            Assert.IsTrue(manager.TryAddRelic("R001", out _));
            Assert.AreEqual(1, manager.Count);

            // Attempting to add duplicate R001 must be rejected
            bool addedDup = manager.TryAddRelic("R001", out string dupError);
            Assert.IsFalse(addedDup);
            Assert.AreEqual(1, manager.Count);
            StringAssert.Contains("Duplicate relics are not permitted", dupError);
        }

        [Test]
        public void ReplaceRelic_WhenAtCapacity_ReplacesOldWithNew()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R001", out _);
            manager.TryAddRelic("R002", out _);
            manager.TryAddRelic("R003", out _);
            manager.TryAddRelic("R004", out _);
            manager.TryAddRelic("R005", out _);

            Assert.IsTrue(manager.IsFull);

            // Replace R001 with R006
            bool replaced = manager.ReplaceRelic("R001", "R006", out string error);
            Assert.IsTrue(replaced, error);
            Assert.AreEqual(5, manager.Count);
            Assert.IsFalse(manager.HasRelic("R001"), "R001 should have been removed");
            Assert.IsTrue(manager.HasRelic("R006"), "R006 should have been added");

            // Attempting to replace with an already owned relic must fail
            bool replacedDup = manager.ReplaceRelic("R002", "R003", out string dupErr);
            Assert.IsFalse(replacedDup);
            StringAssert.Contains("Duplicates are not permitted", dupErr);
        }

        [Test]
        public void DungeonTransition_PreservesRelics_PermanentProgression()
        {
            var map = MapGenerator.Generate(42, 1);
            var party = Database.Party.Take(4).Select(id => Database.Characters[id]);
            var run = new RunState(map, party, 42);

            Assert.IsTrue(run.TryAddRelic("R001", out _));
            Assert.IsTrue(run.TryAddRelic("R002", out _));
            Assert.AreEqual(2, run.RelicManager.Count);
            Assert.AreEqual(2, run.Relics.Count);

            // Dungeon Clear: AdvanceToNextDungeon preserves relics per Bab 14 §2 Permanent Progression
            var nextMap = MapGenerator.Generate(43, 2);
            run.AdvanceToNextDungeon(nextMap);

            Assert.AreEqual(2, run.RelicManager.Count, "Next dungeon must keep existing relics");
            Assert.AreEqual(2, run.Relics.Count);
            Assert.IsTrue(run.RelicManager.HasRelic("R001"));
            Assert.IsTrue(run.RelicManager.HasRelic("R002"));
        }

        [Test]
        public void RunFailure_PreservesRelics_PermanentProgression()
        {
            var map = MapGenerator.Generate(42, 1);
            var party = Database.Party.Take(4).Select(id => Database.Characters[id]);
            var run = new RunState(map, party, 42);

            Assert.IsTrue(run.TryAddRelic("R003", out _));
            Assert.AreEqual(1, run.RelicManager.Count);

            // Run failure (Bab 14 §2: Relics are permanent player progression and remain owned across wipes)
            run.LoseCombat();

            Assert.AreEqual(1, run.RelicManager.Count, "Relics must persist across run failure");
            Assert.AreEqual(1, run.Relics.Count);
            Assert.IsTrue(run.RelicManager.HasRelic("R003"));
        }

        [Test]
        public void R001_AshenSignet_GrantsBonusOnlyAbove80PercentHp()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R001", out _);

            var enemyDef = Database.Enemies.Values.First();
            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Target at 90% HP (> 80%): +5% damage
            target.unit.hp = (int)(target.Stats.maxHp * 0.90f);
            float mult1 = 1f;
            int break1 = 0;
            manager.ModifyDamageDealt(actor, target, ref mult1, ref break1, false);
            Assert.AreEqual(1.05f, mult1, 0.001f, "Damage must be +5% when target HP > 80%");

            // Target at 70% HP (<= 80%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.70f);
            float mult2 = 1f;
            int break2 = 0;
            manager.ModifyDamageDealt(actor, target, ref mult2, ref break2, false);
            Assert.AreEqual(1.00f, mult2, 0.001f, "Damage must NOT be boosted when target HP <= 80%");
        }

        [Test]
        public void R002_SoulglassShard_Restores2PercentMaxMpEvery3PerfectOffensiveQtes()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R002", out _);

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            actor.unit.mp = 10;
            int initialMp = actor.unit.mp;
            int expectedRestore = Math.Max(1, (int)Math.Floor(actor.Stats.maxMp * 0.02f));

            // 1st Perfect QTE
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp, actor.unit.mp, "No MP on 1st QTE");

            // 2nd Perfect QTE
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp, actor.unit.mp, "No MP on 2nd QTE");

            // 3rd Perfect QTE -> restores 2% Max MP
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp + expectedRestore, actor.unit.mp, "2% Max MP restored on 3rd Perfect Offensive QTE");

            // 4th Perfect QTE
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp + expectedRestore, actor.unit.mp, "No MP on 4th QTE");

            // Good QTE does not increment counter
            manager.OnQteResolved(actor, true, QteResult.Good);
            Assert.AreEqual(initialMp + expectedRestore, actor.unit.mp);

            // Defensive QTE does not trigger R002
            manager.OnQteResolved(actor, false, QteResult.Perfect);
            Assert.AreEqual(initialMp + expectedRestore, actor.unit.mp);
        }

        [Test]
        public void R003_RustboundMedal_ReducesDirectDamageTakenUnder30PercentHp()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R003", out _);

            var target = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");

            // Target at 25% HP (< 30%): -8% damage taken
            target.unit.hp = (int)(target.Stats.maxHp * 0.25f);
            float mult1 = 1f;
            manager.ModifyDamageReceived(target, ref mult1, true);
            Assert.AreEqual(0.92f, mult1, 0.001f, "Direct damage taken must be reduced by 8% under 30% HP");

            // Target at 50% HP (>= 30%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.50f);
            float mult2 = 1f;
            manager.ModifyDamageReceived(target, ref mult2, true);
            Assert.AreEqual(1.00f, mult2, 0.001f, "Damage taken must NOT be reduced at or above 30% HP");
        }

        [Test]
        public void R009_SoulTetherRing_PreventsLethalDamageOncePerBattle()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R009", out _);

            var target = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            target.unit.hp = 10;

            // 1st lethal hit: 20 damage to 10 HP
            bool prevented1 = manager.CheckLethalDamageProtection(target, 20);
            Assert.IsTrue(prevented1, "1st lethal blow must be prevented");
            Assert.AreEqual(1, target.unit.hp, "Target must remain at 1 HP");

            // 2nd lethal hit in same battle: must NOT be prevented
            bool prevented2 = manager.CheckLethalDamageProtection(target, 5);
            Assert.IsFalse(prevented2, "2nd lethal blow in same battle must not be prevented");

            // After resetting battle state, can prevent again
            manager.OnBattleStart(null);
            target.unit.hp = 10;
            bool prevented3 = manager.CheckLethalDamageProtection(target, 15);
            Assert.IsTrue(prevented3, "Lethal blow can be prevented in a new battle");
            Assert.AreEqual(1, target.unit.hp);
        }

        [Test]
        public void R010_ExecutionerBell_BoostsBreakPowerOnFirstStrikeOnly()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R010", out _);

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var enemyDef = Database.Enemies.Values.First();
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // First strike in battle: +10% Break Power (10 + floor(10 * 0.10) = 11)
            float mult1 = 1f;
            int break1 = 10;
            manager.ModifyDamageDealt(actor, target, ref mult1, ref break1, true);
            Assert.AreEqual(11, break1, "First strike gets +10% Break Power (10 + 1)");

            // Second strike in battle
            float mult2 = 1f;
            int break2 = 10;
            manager.ModifyDamageDealt(actor, target, ref mult2, ref break2, false);
            Assert.AreEqual(10, break2, "Subsequent strikes do not gain first strike bonus");
        }

        [Test]
        public void R013_DirgeEaterFang_RestoresMpOnKill_CappedAt2PerBattle()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R013", out _);

            var killer = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            killer.unit.mp = 10;
            var enemyDef = Database.Enemies.Values.First();
            var enemy = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Kills 1, 2 restore 1 MP each
            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(11, killer.unit.mp);

            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(12, killer.unit.mp);

            // 3rd kill in same battle must NOT restore MP (capped at 2)
            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(12, killer.unit.mp, "Must cap at 2 MP per battle");

            // Resetting battle restores capacity
            manager.OnBattleStart(null);
            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(13, killer.unit.mp, "New battle allows up to 2 MP again");
        }

        [Test]
        public void R014_PallbearerGloves_NegatesFirstMinorTrapPerDungeon()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R014", out _);

            // 1st trap in dungeon is negated
            Assert.IsTrue(manager.TryNegateMinorChestTrap(out string msg1));
            StringAssert.Contains("disarmed", msg1);

            // 2nd trap in same dungeon is NOT negated
            Assert.IsFalse(manager.TryNegateMinorChestTrap(out string msg2));
            Assert.IsNull(msg2);

            // Resetting dungeon state allows negation again
            manager.GetInstance("R014").ResetDungeonState();
            Assert.IsTrue(manager.TryNegateMinorChestTrap(out _));
        }

        [Test]
        public void R015_VeilOfDust_Grants8PercentStatusResistance()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R015", out _);

            Assert.AreEqual(0.08f, manager.GetStatusResistanceBonus(StatusId.Burn), 0.001f);
            Assert.AreEqual(0.08f, manager.GetStatusResistanceBonus(StatusId.Poison), 0.001f);
            Assert.AreEqual(0.08f, manager.GetStatusResistanceBonus(StatusId.Bleed), 0.001f);
            Assert.AreEqual(0.00f, manager.GetStatusResistanceBonus(StatusId.Stun), 0.001f);
        }

        [Test]
        public void R016_RoyalGravewax_BoostsConsumableHealBy5Percent()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R016", out _);

            float healMult = 1f;
            manager.ModifyConsumableHeal(ref healMult);
            Assert.AreEqual(1.05f, healMult, 0.001f);
        }

        [Test]
        public void R021_CoffinNail_BoostsDamageAgainstBrokenTargetBy7Percent()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R021", out _);

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var enemyDef = Database.Enemies.Values.First();
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Target not broken
            float mult1 = 1f;
            int break1 = 0;
            manager.ModifyDamageDealt(actor, target, ref mult1, ref break1, false);
            Assert.AreEqual(1.00f, mult1, 0.001f);

            // Break the target
            target.MaxBreakGauge = 50;
            target.CurrentBreakGauge = 50;
            target.ApplyBreakDamage(100, actor, out _);
            Assert.IsTrue(target.IsBroken);

            float mult2 = 1f;
            int break2 = 0;
            manager.ModifyDamageDealt(actor, target, ref mult2, ref break2, false);
            Assert.AreEqual(1.07f, mult2, 0.001f, "Damage must be +7% against Broken target");
        }

        [Test]
        public void R022_ZoroaDullCrown_ActivatesDamageBuffAfter3KillsInBattle()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R022", out _);

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var enemyDef = Database.Enemies.Values.First();
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // 2 kills: not active yet
            for (int i = 0; i < 2; i++)
            {
                manager.OnEnemyDefeated(actor, target);
                float mult = 1f;
                int b = 0;
                manager.ModifyDamageDealt(actor, target, ref mult, ref b, false);
                Assert.AreEqual(1.00f, mult);
            }

            // 3rd kill: activates buff (+3% All Damage)
            manager.OnEnemyDefeated(actor, target);
            float activeMult = 1f;
            int brk = 0;
            manager.ModifyDamageDealt(actor, target, ref activeMult, ref brk, false);
            Assert.AreEqual(1.03f, activeMult, 0.001f, "+3% party damage active after 3 kills");
        }

        [Test]
        public void DemeritRelics_ApplyCorrectPositiveAndNegativeModifiers()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R023", out _); // Crimson Testament: +18% damage dealt, +12% damage taken

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var enemyDef = Database.Enemies.Values.First();
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Damage dealt: +18%
            float dealtMult = 1f;
            int brk = 0;
            manager.ModifyDamageDealt(actor, target, ref dealtMult, ref brk, false);
            Assert.AreEqual(1.18f, dealtMult, 0.001f);

            // Damage received by party member: +12%
            float recvMult = 1f;
            manager.ModifyDamageReceived(actor, ref recvMult, true);
            Assert.AreEqual(1.12f, recvMult, 0.001f);

            // R025: Arcanum Unbound (+25% skill dmg, +20% MP cost)
            var manager2 = new RelicManager();
            manager2.TryAddRelic("R025", out _);
            float skillDmg = 1f;
            int brk2 = 0;
            manager2.ModifyDamageDealt(actor, target, ref skillDmg, ref brk2, false, isSkill: true);
            Assert.AreEqual(1.25f, skillDmg, 0.001f);

            var testSkill = new SkillDef { id = "fireball", name = "Fireball", mpCost = 10 };
            int mpCost = 10;
            manager2.ModifyMpCost(actor, testSkill, ref mpCost);
            Assert.AreEqual(12, mpCost);

            // R026: The Empty Chalice (-30% MP cost, -25% healing)
            var manager3 = new RelicManager();
            manager3.TryAddRelic("R026", out _);
            int chaliceMpCost = 10;
            manager3.ModifyMpCost(actor, testSkill, ref chaliceMpCost);
            Assert.AreEqual(7, chaliceMpCost);

            float healMult = 1f;
            manager3.ModifyHealing(actor, actor, ref healMult);
            Assert.AreEqual(0.75f, healMult, 0.001f);

            // R032: Merchant's Burden (+15% Gold, -15% EXP)
            var manager4 = new RelicManager();
            manager4.TryAddRelic("R032", out _);
            int gold = 100;
            manager4.ModifyGoldReward(ref gold);
            Assert.AreEqual(115, gold);
        }

        [Test]
        public void RelicSaveRestore_PreservesRuntimeCountersAndFlags()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R002", out _); // QTE counter
            manager.TryAddRelic("R009", out _); // once/battle lethal
            manager.TryAddRelic("R013", out _); // cap/battle MP
            manager.TryAddRelic("R014", out _); // once/dungeon trap

            var r002 = manager.GetInstance("R002");
            r002.counter = 2;

            var r009 = manager.GetInstance("R009");
            r009.battleTriggered = true;

            var r013 = manager.GetInstance("R013");
            r013.battleUsageCount = 2;

            var r014 = manager.GetInstance("R014");
            r014.dungeonTriggered = true;

            // Serialize
            var saveData = manager.ToSaveData();
            Assert.AreEqual(4, saveData.Length);

            // Restore in fresh manager
            var restoredManager = new RelicManager();
            restoredManager.RestoreFromSave(saveData);

            Assert.AreEqual(4, restoredManager.Count);
            Assert.AreEqual(2, restoredManager.GetInstance("R002").counter);
            Assert.IsTrue(restoredManager.GetInstance("R009").battleTriggered);
            Assert.AreEqual(2, restoredManager.GetInstance("R013").battleUsageCount);
            Assert.IsTrue(restoredManager.GetInstance("R014").dungeonTriggered);
        }
    }
}
