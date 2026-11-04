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
        public void All22Relics_ExistInDatabase_WithMatchingMetadata()
        {
            Assert.AreEqual(22, Database.Layer1Relics.Count, "Layer 1 Relics count must be exactly 22.");

            string[] expectedIds = {
                "R001", "R002", "R003", "R004", "R005", "R006", "R007", "R008", "R009", "R010",
                "R011", "R012", "R013", "R014", "R015", "R016", "R017", "R018", "R019", "R020",
                "R021", "R022"
            };

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
            Assert.AreEqual(RelicProposedRarity.Epic, Database.Relics["R022"].proposedRarity);
            Assert.AreEqual(RelicActivationLimit.StackTrigger, Database.Relics["R022"].activationLimit);
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
        public void DungeonClear_WipesRelics_NextDungeonStartsEmpty()
        {
            var map = MapGenerator.Generate(42, 1);
            var party = Database.Party.Take(4).Select(id => Database.Characters[id]);
            var run = new RunState(map, party, 42);

            Assert.IsTrue(run.TryAddRelic("R001", out _));
            Assert.IsTrue(run.TryAddRelic("R002", out _));
            Assert.AreEqual(2, run.RelicManager.Count);
            Assert.AreEqual(2, run.Relics.Count);

            // Dungeon Clear: AdvanceToNextDungeon removes all relics per Confirmed Rule 4
            var nextMap = MapGenerator.Generate(43, 2);
            run.AdvanceToNextDungeon(nextMap);

            Assert.AreEqual(0, run.RelicManager.Count, "Next dungeon must start with 0 relics");
            Assert.AreEqual(0, run.Relics.Count);
        }

        [Test]
        public void RunFailure_WipesAllRelics()
        {
            var map = MapGenerator.Generate(42, 1);
            var party = Database.Party.Take(4).Select(id => Database.Characters[id]);
            var run = new RunState(map, party, 42);

            Assert.IsTrue(run.TryAddRelic("R003", out _));
            Assert.AreEqual(1, run.RelicManager.Count);

            // Run failure
            run.LoseCombat();

            Assert.AreEqual(0, run.RelicManager.Count, "Failed run must wipe all relics");
            Assert.AreEqual(0, run.Relics.Count);
        }

        [Test]
        public void R001_AshenSignet_GrantsBonusOnlyAbove80PercentHp()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R001", out _);

            var enemyDef = Database.Enemies.Values.First();
            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Target at 90% HP (> 80%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.90f);
            float mult1 = 1f;
            int break1 = 0;
            manager.ModifyDamageDealt(actor, target, ref mult1, ref break1, false);
            Assert.AreEqual(1.08f, mult1, 0.001f, "Damage must be +8% when target HP > 80%");

            // Target at 70% HP (<= 80%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.70f);
            float mult2 = 1f;
            int break2 = 0;
            manager.ModifyDamageDealt(actor, target, ref mult2, ref break2, false);
            Assert.AreEqual(1.00f, mult2, 0.001f, "Damage must NOT be boosted when target HP <= 80%");
        }

        [Test]
        public void R002_SoulglassShard_Restores2MpEvery3PerfectOffensiveQtes()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R002", out _);

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            actor.unit.mp = 10;
            int initialMp = actor.unit.mp;

            // 1st Perfect QTE
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp, actor.unit.mp, "No MP on 1st QTE");

            // 2nd Perfect QTE
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp, actor.unit.mp, "No MP on 2nd QTE");

            // 3rd Perfect QTE -> +2 MP
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp + 2, actor.unit.mp, "+2 MP restored on 3rd Perfect Offensive QTE");

            // 4th Perfect QTE
            manager.OnQteResolved(actor, true, QteResult.Perfect);
            Assert.AreEqual(initialMp + 2, actor.unit.mp, "No MP on 4th QTE");

            // Good QTE does not increment counter
            manager.OnQteResolved(actor, true, QteResult.Good);
            Assert.AreEqual(initialMp + 2, actor.unit.mp);

            // Defensive QTE does not trigger R002
            manager.OnQteResolved(actor, false, QteResult.Perfect);
            Assert.AreEqual(initialMp + 2, actor.unit.mp);
        }

        [Test]
        public void R003_RustboundMedal_ReducesDirectDamageTakenUnder30PercentHp()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R003", out _);

            var target = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");

            // Target at 25% HP (< 30%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.25f);
            float mult1 = 1f;
            manager.ModifyDamageReceived(target, ref mult1, true);
            Assert.AreEqual(0.90f, mult1, 0.001f, "Direct damage taken must be reduced by 10% under 30% HP");

            // Target at 50% HP (>= 30%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.50f);
            float mult2 = 1f;
            manager.ModifyDamageReceived(target, ref mult2, true);
            Assert.AreEqual(1.00f, mult2, 0.001f, "Damage taken must NOT be reduced at or above 30% HP");
        }

        [Test]
        public void R007_MournerCoin_IncreasesGoldBy10Percent()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R007", out _);

            int gold = 100;
            manager.ModifyGoldReward(ref gold);
            Assert.AreEqual(110, gold, "+10% Run Gold granted");

            int gold2 = 15;
            manager.ModifyGoldReward(ref gold2);
            Assert.AreEqual(16, gold2, "+10% Run Gold rounded down (15 + 1)");
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

            // First strike in battle
            float mult1 = 1f;
            int break1 = 10;
            manager.ModifyDamageDealt(actor, target, ref mult1, ref break1, true);
            Assert.AreEqual(12, break1, "First strike gets +15% Break Power (10 + 2)");

            // Second strike in battle
            float mult2 = 1f;
            int break2 = 10;
            manager.ModifyDamageDealt(actor, target, ref mult2, ref break2, false);
            Assert.AreEqual(10, break2, "Subsequent strikes do not gain first strike bonus");
        }

        [Test]
        public void R013_DirgeEaterFang_RestoresMpOnKill_CappedAt3PerBattle()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R013", out _);

            var killer = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            killer.unit.mp = 10;
            var enemyDef = Database.Enemies.Values.First();
            var enemy = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Kills 1, 2, 3 restore 1 MP each
            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(11, killer.unit.mp);

            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(12, killer.unit.mp);

            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(13, killer.unit.mp);

            // 4th kill in same battle must NOT restore MP (capped at 3)
            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(13, killer.unit.mp, "Must cap at 3 MP per battle");

            // Resetting battle restores capacity
            manager.OnBattleStart(null);
            manager.OnEnemyDefeated(killer, enemy);
            Assert.AreEqual(14, killer.unit.mp, "New battle allows up to 3 MP again");
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
            manager.ClearDungeonRelics(); // or manager.GetInstance("R014").ResetDungeonState()
        }

        [Test]
        public void R015_VeilOfDust_Grants10PercentStatusResistance()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R015", out _);

            Assert.AreEqual(0.10f, manager.GetStatusResistanceBonus(StatusId.Burn), 0.001f);
            Assert.AreEqual(0.10f, manager.GetStatusResistanceBonus(StatusId.Poison), 0.001f);
            Assert.AreEqual(0.10f, manager.GetStatusResistanceBonus(StatusId.Bleed), 0.001f);
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
        public void R021_CoffinNail_BoostsDamageAgainstBrokenTargetBy10Percent()
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
            Assert.AreEqual(1.10f, mult2, 0.001f, "Damage must be +10% against Broken target");
        }

        [Test]
        public void R022_ZoroaDullCrown_ActivatesDamageBuffAfter5KillsInBattle()
        {
            var manager = new RelicManager();
            manager.TryAddRelic("R022", out _);

            var actor = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var enemyDef = Database.Enemies.Values.First();
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // 4 kills: not active yet
            for (int i = 0; i < 4; i++)
            {
                manager.OnEnemyDefeated(actor, target);
                float mult = 1f;
                int b = 0;
                manager.ModifyDamageDealt(actor, target, ref mult, ref b, false);
                Assert.AreEqual(1.00f, mult);
            }

            // 5th kill: activates buff
            manager.OnEnemyDefeated(actor, target);
            float activeMult = 1f;
            int brk = 0;
            manager.ModifyDamageDealt(actor, target, ref activeMult, ref brk, false);
            Assert.AreEqual(1.04f, activeMult, 0.001f, "+4% party damage active after 5 kills");
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

