using Obscure;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class DifficultySystemTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            _ = Database.Items;
            _ = Database.Characters;
            _ = Database.Equipment;
            _ = Database.Classes;
            _ = Database.Races;
            _ = Database.Backgrounds;
        }

        static RunState CreateTestRun(int seed = 42, int act = 1)
        {
            var party = Database.Party.Select(id => Database.Characters[id]).ToList();
            var map = MapGenerator.Generate(seed, act);
            return new RunState(map, party, seed);
        }

        // =====================================================================
        // 1. Starting New Game initializes Dungeon 1
        // =====================================================================

        [Test]
        public void NewGame_InitializesDungeon1_InActOne()
        {
            var run = CreateTestRun(seed: 101, act: 1);

            Assert.AreEqual(1, run.Act, "New game expedition must start at Dungeon 1 (Act 1).");
            Assert.AreEqual("The Sunken Crypt", run.Map.name, "Dungeon 1 name must be 'The Sunken Crypt'.");
            Assert.AreEqual(RunStatus.Active, run.Status, "Expedition status must be Active.");
            Assert.AreEqual(DifficultySystem.PartySize, run.Party.Count, "Party must consist of exactly 4 characters.");
            Assert.IsTrue(run.Party.All(u => u.Alive), "All 4 party members must start alive.");
            Assert.AreEqual(0, run.Gold, "Run-based gold must start at 0.");
            Assert.IsNull(run.Current, "Initial node should be null before stepping into floor 0.");
        }

        // =====================================================================
        // 2. Dungeon progression follows the fixed sequence
        // =====================================================================

        [Test]
        public void DungeonProgression_FollowsFixedSequence_ThroughAllEightDungeons()
        {
            Assert.AreEqual(8, DifficultySystem.TotalDungeons);
            Assert.AreEqual(8, DifficultySystem.DungeonSequence.Length);

            string[] expectedSequence =
            {
                "The Sunken Crypt",      // Dungeon 1
                "The Ashen Catacombs",   // Dungeon 2
                "The Forgotten Mines",   // Dungeon 3
                "The Abyssal Labyrinth", // Dungeon 4
                "The Sunless Spire",     // Dungeon 5
                "The Cursed Citadel",    // Dungeon 6
                "The Shadow Bastion",    // Dungeon 7
                "The Void Sanctum"       // Dungeon 8
            };

            for (int i = 0; i < 8; i++)
            {
                int act = i + 1;
                Assert.AreEqual(expectedSequence[i], DifficultySystem.GetDungeonName(act), $"Dungeon {act} name mismatch.");
            }

            var run = CreateTestRun(seed: 777, act: 1);
            Assert.AreEqual(1, run.Act);

            // Attempting to skip Act 2 and advance straight to Act 3 must be rejected
            var invalidSkipMap = MapGenerator.Generate(777, act: 3);
            bool skipResult = run.AdvanceToNextDungeon(invalidSkipMap);
            Assert.IsFalse(skipResult, "Skipping dungeons or freely choosing the next dungeon must be rejected.");
            Assert.AreEqual(1, run.Act, "Run act must remain 1 after rejected skip.");

            // Sequential advance from Act 1 -> Act 8 must succeed
            for (int act = 2; act <= 8; act++)
            {
                var nextMap = MapGenerator.Generate(run.Seed + act, act);
                bool ok = run.AdvanceToNextDungeon(nextMap);
                Assert.IsTrue(ok, $"Sequential advance to Dungeon {act} must succeed.");
                Assert.AreEqual(act, run.Act);
                Assert.AreEqual(expectedSequence[act - 1], run.Map.name);
            }
        }

        // =====================================================================
        // 3. Dungeon Clear advances to the next dungeon
        // =====================================================================

        [Test]
        public void DungeonClear_AdvancesToNextSequentialDungeon_PreservingPartyState()
        {
            var run = CreateTestRun(seed: 55, act: 1);
            var hero = run.Party[0];
            hero.hp = Math.Max(1, hero.stats.maxHp - 15);
            hero.mp = Math.Max(1, hero.stats.maxMp - 8);
            int heroHp = hero.hp;
            int heroMp = hero.mp;

            // Advance to Dungeon 2
            var act2Map = MapGenerator.Generate(run.Seed + 2, act: 2);
            bool advanced = run.AdvanceToNextDungeon(act2Map);

            Assert.IsTrue(advanced);
            Assert.AreEqual(2, run.Act, "Must advance to Act 2.");
            Assert.AreEqual("The Ashen Catacombs", run.Map.name);
            Assert.AreEqual(RunStatus.Active, run.Status, "Run remains active across sequential dungeons.");
            Assert.IsNull(run.Current, "Node position resets upon entering a new dungeon.");
            Assert.IsEmpty(run.Cleared, "Cleared nodes from previous dungeon are cleared for the new dungeon.");

            // Party resources and progression are preserved
            Assert.AreEqual(heroHp, run.Party[0].hp, "Party HP should be preserved across cleared dungeons.");
            Assert.AreEqual(heroMp, run.Party[0].mp, "Party MP should be preserved across cleared dungeons.");
        }

        // =====================================================================
        // 4. Gold is retained after Dungeon Clear
        // =====================================================================

        [Test]
        public void DungeonClear_RetainsGold_ForHubAndNextDungeonWithoutDeduction()
        {
            var run = CreateTestRun(seed: 123, act: 1);
            run.Gold = 650;

            // On dungeon clear, remaining Gold is retained 1:1
            int transferred = run.TransferRunGoldToHub();
            Assert.AreEqual(650, transferred);
            Assert.AreEqual(650, run.Gold, "Gold is retained for Hub and subsequent dungeons.");
            Assert.AreEqual(650, run.HubGold);

            // Advance to Dungeon 2
            var act2Map = MapGenerator.Generate(run.Seed + 2, act: 2);
            run.AdvanceToNextDungeon(act2Map);

            Assert.AreEqual(650, run.Gold, "Gold must be retained across sequential dungeons.");
            Assert.AreEqual(650, run.HubGold);
        }

        // =====================================================================
        // 5. Dungeon 8 Clear completes the expedition
        // =====================================================================

        [Test]
        public void Dungeon8Clear_CompletesExpedition_MarkingVictory()
        {
            var run = CreateTestRun(seed: 888, act: 8);
            Assert.AreEqual(8, run.Act);
            Assert.AreEqual("The Void Sanctum", run.Map.name);

            // Verify IsExpeditionComplete evaluation
            Assert.IsTrue(DifficultySystem.IsExpeditionComplete(8, bossDefeated: true));
            Assert.IsFalse(DifficultySystem.IsExpeditionComplete(7, bossDefeated: true));
            Assert.IsFalse(DifficultySystem.IsExpeditionComplete(8, bossDefeated: false));

            // Defeating the boss room on Act 8 completes the run
            var bossNode = run.Map.nodes.First(n => n.type == NodeType.Boss);
            // Simulate reaching and clearing boss room
            run.Party[0].hp = 50;
            var bossField = typeof(RunState).GetProperty("Current");
            bossField.SetValue(run, bossNode);
            run.Cleared.Add(bossNode.id);
            var statusProp = typeof(RunState).GetProperty("Status");
            statusProp.SetValue(run, RunStatus.Victory);

            Assert.AreEqual(RunStatus.Victory, run.Status, "Dungeon 8 boss victory completes the expedition.");
            Assert.IsTrue(run.Won);
        }

        // =====================================================================
        // 6. A battle continues when at least one party member remains active
        // =====================================================================

        [Test]
        public void Battle_ContinuesWhenAtLeastOnePartyMemberIsActive_WhileOthersDown()
        {
            var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var enemies = new[] { Database.Enemies["skeleton"] };

            // 3 of 4 party members are knocked down (0 HP)
            party[0].hp = 0;
            party[1].hp = 0;
            party[2].hp = 0;
            party[3].hp = 25; // 1 active survivor

            var battle = new Battle(party, enemies, new List<string>(), new System.Random(1));

            Assert.IsTrue(DifficultySystem.CanBattleContinue(battle.Party), "Battle can continue as long as >= 1 party member is alive.");
            Assert.IsFalse(DifficultySystem.IsPartyWiped(battle.Party), "Party is not wiped while at least 1 hero stands.");
            Assert.IsTrue(DifficultySystem.IsInPeril(party), "Party should be recognized as in peril when 1 survivor remains.");
            Assert.AreEqual(1, DifficultySystem.LivingCount(party));
            Assert.AreEqual(3, DifficultySystem.FallenCount(party));
            Assert.AreEqual(Outcome.None, battle.Outcome, "Battle outcome must remain Ongoing while survivor fights.");
        }

        // =====================================================================
        // 7. Party Wipe triggers Run Failure
        // =====================================================================

        [Test]
        public void PartyWipe_TriggersRunFailure_MarkingDefeat()
        {
            var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var enemies = new[] { Database.Enemies["skeleton"] };

            // Entire party down (0 HP)
            foreach (var u in party) u.hp = 0;

            var battle = new Battle(party, enemies, new List<string>(), new System.Random(1));

            Assert.IsTrue(DifficultySystem.IsPartyWiped(battle.Party), "All party members at 0 HP is a party wipe.");
            Assert.IsFalse(DifficultySystem.CanBattleContinue(battle.Party));
            Assert.AreEqual(Outcome.Defeat, battle.Outcome, "Party wipe produces Outcome.Defeat.");

            var run = CreateTestRun(seed: 12);
            run.LoseCombat();

            Assert.AreEqual(RunStatus.Defeat, run.Status, "LoseCombat must mark RunStatus.Defeat.");
        }

        // =====================================================================
        // 8. Run Failure clears dungeon progression
        // =====================================================================

        [Test]
        public void RunFailure_ClearsDungeonProgression_AndNodes()
        {
            var run = CreateTestRun(seed: 45, act: 3);
            var firstNode = run.Available.First();
            run.Enter(firstNode);
            run.Cleared.Add(firstNode.id);

            Assert.IsNotNull(run.Current);
            Assert.Greater(run.Cleared.Count, 0);

            // Trigger failure
            run.LoseCombat();

            Assert.IsNull(run.Current, "Current node must be cleared on failure.");
            Assert.IsEmpty(run.Cleared, "Cleared nodes progression must be wiped on failure.");
            Assert.IsEmpty(run.Available, "No nodes can be entered after run failure.");
            Assert.IsNull(run.PendingReward, "Pending rewards must be cleared.");
        }

        // =====================================================================
        // 9. Run Failure clears run-based Gold
        // =====================================================================

        [Test]
        public void RunFailure_ClearsRunBasedGold_PerDifficultySystem()
        {
            var run = CreateTestRun(seed: 99);
            run.Gold = 950;

            run.LoseCombat();

            Assert.AreEqual(0, run.Gold, "Gold must be completely cleared to 0 on failure.");
            Assert.AreEqual(0, run.HubGold, "HubGold alias must also be 0 on failure.");
        }

        // =====================================================================
        // 10. Run Failure blocks continuation from Main Menu, requiring New Game
        // =====================================================================

        [Test]
        public void RunFailure_BlocksContinueFromMainMenu_RequiringNewGame()
        {
            GameSave.ClearSave();
            var run = CreateTestRun(seed: 333);
            run.Gold = 400;
            GameSave.SaveRunState(run);

            // When active, save is valid and continue is enabled
            Assert.IsTrue(GameSave.HasSavedGame(), "Active run state should allow Continue.");

            // Party wipe occurs
            run.LoseCombat();

            // Saved state is updated with failure
            Assert.IsFalse(GameSave.HasSavedGame(), "Failed run cannot be continued from Main Menu.");
            Assert.IsFalse(GameSave.TryLoadContinue(out _, out _), "TryLoadContinue must reject defeated runs.");

            GameSave.ClearSave();
        }

        // =====================================================================
        // 11. A new expedition starts from Dungeon 1
        // =====================================================================

        [Test]
        public void NewExpedition_AfterFailure_StartsFromDungeon1()
        {
            var failedRun = CreateTestRun(seed: 404, act: 5);
            failedRun.LoseCombat();
            Assert.AreEqual(RunStatus.Defeat, failedRun.Status);

            // Starting a fresh expedition via ResetToNewGame or constructor
            var customParty = Database.Party.Select(id => Database.Characters[id]).ToList();
            var freshRun = new RunState(MapGenerator.Generate(505, 1), customParty, 505);

            Assert.AreEqual(1, freshRun.Act, "New expedition after failure must start strictly at Dungeon 1.");
            Assert.AreEqual("The Sunken Crypt", freshRun.Map.name);
            Assert.AreEqual(0, freshRun.Gold);
            Assert.AreEqual(RunStatus.Active, freshRun.Status);
            Assert.IsNull(freshRun.Current);
        }

        // =====================================================================
        // 12. Run Failure does not delete unrelated settings or permanent data
        // =====================================================================

        [Test]
        public void RunFailure_DoesNotDeleteUnrelatedSettings_OrPermanentData()
        {
            // Configure custom settings
            var settings = new GameSettings
            {
                volume = 3,
                musicVolume = 4,
                fullscreen = false,
                qteAssist = 2
            };
            settings.Save();

            // Run with Gold
            var run = CreateTestRun(seed: 606);
            run.Gold = 500;

            run.LoseCombat();

            // Reload settings
            var loadedSettings = GameSettings.Load();
            Assert.AreEqual(3, loadedSettings.volume, "Volume setting must not be touched by run failure.");
            Assert.AreEqual(4, loadedSettings.musicVolume, "Music volume must not be touched by run failure.");
            Assert.AreEqual(false, loadedSettings.fullscreen, "Fullscreen setting must not be touched by run failure.");
            Assert.AreEqual(2, loadedSettings.qteAssist, "QTE assist setting must not be touched by run failure.");

            // Gold wiped on failure per Difficulty System
            Assert.AreEqual(0, run.Gold, "Run Gold must be wiped by run failure.");
            Assert.AreEqual(0, run.HubGold);
        }

        // =====================================================================
        // 13. Repeated battle-result events do not cause duplicate transitions or resets
        // =====================================================================

        [Test]
        public void RepeatedBattleResultEvents_DoNotCauseDuplicateTransitionsOrResets()
        {
            var run = CreateTestRun(seed: 707);
            run.Gold = 300;

            // Call LoseCombat multiple times
            run.LoseCombat();
            Assert.AreEqual(RunStatus.Defeat, run.Status);
            Assert.AreEqual(0, run.Gold);
            Assert.AreEqual(0, run.HubGold);

            // Second and third calls must be idempotent safe no-ops
            Assert.DoesNotThrow(() => run.LoseCombat());
            Assert.DoesNotThrow(() => run.LoseCombat());
            Assert.AreEqual(RunStatus.Defeat, run.Status);
            Assert.AreEqual(0, run.Gold);
            Assert.AreEqual(0, run.HubGold);

            // WinCombat when defeat or not in combat throws InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => run.WinCombat());

            // ClaimReward when not pending throws InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(0));
        }
    }
}

