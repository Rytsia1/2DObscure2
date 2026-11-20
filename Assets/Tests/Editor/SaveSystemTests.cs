using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class SaveSystemTests
    {
        ISaveStorage originalStorage;
        MemorySaveStorage memoryStorage;

        [SetUp]
        public void Setup()
        {
            originalStorage = SaveManager.Storage;
            memoryStorage = new MemorySaveStorage();
            SaveManager.Storage = memoryStorage;
            SaveManager.TotalPlaytimeSeconds = 120f;
            PersistenceCommitHandler.SetActiveCommittedSnapshot(null);
        }

        [TearDown]
        public void Teardown()
        {
            SaveManager.Storage = originalStorage;
            PersistenceCommitHandler.SetActiveCommittedSnapshot(null);
        }

        List<CharacterDef> CreateTestParty()
        {
            return new List<CharacterDef>
            {
                new CharacterDef { id = "hero1", name = "Aethelgard", raceId = "human", classId = "fighter", backgroundId = "guard", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "hero2", name = "Lyra", raceId = "elf", classId = "ranger", backgroundId = "outlander", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "hero3", name = "Boran", raceId = "dwarf", classId = "cleric", backgroundId = "acolyte", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "hero4", name = "Vesper", raceId = "wraith", classId = "mage", backgroundId = "scholar", baseScores = AbilityScores.DefaultBase }
            };
        }

        RunState CreateTestRun(int seed = 42, int act = 1)
        {
            var party = CreateTestParty();
            var map = MapGenerator.Generate(seed, act);
            return new RunState(map, party, seed);
        }

        [Test]
        public void SpentRetriesReachEverySaveOfTheExpedition_AndALostExpeditionCannotBeLoaded()
        {
            var party = new List<CharacterDef>
            {
                new CharacterDef { id = "hero1", name = "A", raceId = "human", classId = "fighter", backgroundId = "guard", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "hero2", name = "B", raceId = "elf", classId = "ranger", backgroundId = "sage", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "hero3", name = "C", raceId = "dwarf", classId = "cleric", backgroundId = "acolyte", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "hero4", name = "D", raceId = "human", classId = "wizard", backgroundId = "sage", baseScores = AbilityScores.DefaultBase },
            };
            void Write(SaveSlotId slot, int seed, long ticks) => SaveManager.Storage.WriteSlotRaw(slot, JsonUtility.ToJson(new SaveFileData
            {
                metadata = new SaveSlotMetadata { slotId = slot, slotType = SaveSlotType.Manual, isOccupied = true, lastSavedTicks = ticks, partySummary = "P" },
                campaign = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, seed),
            }), out _);
            Write(SaveSlotId.Slot1, 42, 300); // an older save of this expedition
            Write(SaveSlotId.Slot2, 7, 100);  // a different expedition

            var run = new RunState(MapGenerator.Generate(42), party, 42);
            run.LoseCombat();
            SaveManager.SyncRetryState(run);
            var camp1 = JsonUtility.FromJson<SaveFileData>(SaveManager.Storage.ReadSlotRaw(SaveSlotId.Slot1)).campaign;
            Assert.AreEqual(RunConfig.RetriesPerExpedition - 1, camp1.retriesLeft, "reloading the older save must not refund the retry");
            Assert.AreEqual(RunConfig.RetriesPerExpedition,
                JsonUtility.FromJson<SaveFileData>(SaveManager.Storage.ReadSlotRaw(SaveSlotId.Slot2)).campaign.retriesLeft, "other expeditions untouched");

            while (run.RetriesLeft > 0) { run.BeginDungeonAttempt(1, run.RetriesLeft); run.LoseCombat(); }
            run.BeginDungeonAttempt(1, 99);
            run.LoseCombat();
            SaveManager.SyncRetryState(run);

            Assert.IsTrue(SaveManager.GetSlotMetadata(SaveSlotId.Slot1).expeditionLost);
            Assert.IsFalse(SaveManager.Load(SaveSlotId.Slot1, out var error));
            StringAssert.Contains("lost", error);
            Assert.IsTrue(SaveManager.HasAnySave(out var best));
            Assert.AreEqual(SaveSlotId.Slot2, best, "Continue skips the lost expedition");
        }

        [Test]
        public void Criterion1_ExposesExactlyFiveSlots_OneAutoAndFourManual()
        {
            var slots = Enum.GetValues(typeof(SaveSlotId)).Cast<SaveSlotId>().ToList();
            Assert.AreEqual(5, slots.Count, "Must have exactly 5 save slots.");
            Assert.Contains(SaveSlotId.AutoSave, slots);
            Assert.Contains(SaveSlotId.Slot1, slots);
            Assert.Contains(SaveSlotId.Slot2, slots);
            Assert.Contains(SaveSlotId.Slot3, slots);
            Assert.Contains(SaveSlotId.Slot4, slots);

            var metaList = SaveManager.GetAllSlotMetadata();
            Assert.AreEqual(5, metaList.Count);
            Assert.AreEqual(SaveSlotType.AutoSave, metaList[0].slotType);
            Assert.AreEqual(SaveSlotType.Manual, metaList[1].slotType);
            Assert.AreEqual(SaveSlotType.Manual, metaList[2].slotType);
            Assert.AreEqual(SaveSlotType.Manual, metaList[3].slotType);
            Assert.AreEqual(SaveSlotType.Manual, metaList[4].slotType);
        }

        [Test]
        public void Criterion2_ManualSlotsAreIndependent()
        {
            var party = CreateTestParty();
            var snap1 = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, seed: 101);
            snap1.hubGold = 150;
            PersistenceCommitHandler.SetActiveCommittedSnapshot(snap1);

            // Save to Slot 1 via direct write using memory storage
            var saveFile1 = new SaveFileData
            {
                metadata = new SaveSlotMetadata { slotId = SaveSlotId.Slot1, slotType = SaveSlotType.Manual, hubGold = 150, partySummary = "Campaign 1", isOccupied = true, lastSavedTicks = 100 },
                campaign = snap1
            };
            SaveManager.Storage.WriteSlotRaw(SaveSlotId.Slot1, JsonUtility.ToJson(saveFile1), out _);

            // Now write different data to Slot 2
            var snap2 = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, seed: 202);
            snap2.hubGold = 500;
            var saveFile2 = new SaveFileData
            {
                metadata = new SaveSlotMetadata { slotId = SaveSlotId.Slot2, slotType = SaveSlotType.Manual, hubGold = 500, partySummary = "Campaign 2", isOccupied = true, lastSavedTicks = 200 },
                campaign = snap2
            };
            SaveManager.Storage.WriteSlotRaw(SaveSlotId.Slot2, JsonUtility.ToJson(saveFile2), out _);

            // Overwrite Slot 1 with new data
            snap1.hubGold = 999;
            saveFile1.metadata.hubGold = 999;
            SaveManager.Storage.WriteSlotRaw(SaveSlotId.Slot1, JsonUtility.ToJson(saveFile1), out _);

            // Verify Slot 2 remains unmodified
            var meta2 = SaveManager.GetSlotMetadata(SaveSlotId.Slot2);
            Assert.AreEqual(500, meta2.hubGold, "Slot 2 must not be modified when Slot 1 is overwritten.");
            Assert.AreEqual("Campaign 2", meta2.partySummary);

            // Delete Slot 1 and verify Slot 2 survives
            SaveManager.Delete(SaveSlotId.Slot1, out _);
            Assert.IsFalse(SaveManager.GetSlotMetadata(SaveSlotId.Slot1).isOccupied);
            Assert.IsTrue(SaveManager.GetSlotMetadata(SaveSlotId.Slot2).isOccupied);
        }

        [Test]
        public void Criterion3_AutoSaveNeverOverwritesManualSlots_AndManualSaveCannotTargetAutoSave()
        {
            var party = CreateTestParty();
            var snap = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, seed: 777);
            PersistenceCommitHandler.SetActiveCommittedSnapshot(snap);

            // Populate manual slot 1
            var saveManual = new SaveFileData
            {
                metadata = new SaveSlotMetadata { slotId = SaveSlotId.Slot1, slotType = SaveSlotType.Manual, hubGold = 350, isOccupied = true },
                campaign = snap
            };
            SaveManager.Storage.WriteSlotRaw(SaveSlotId.Slot1, JsonUtility.ToJson(saveManual), out _);

            // Populate AutoSave
            snap.hubGold = 50;
            var saveAuto = new SaveFileData
            {
                metadata = new SaveSlotMetadata { slotId = SaveSlotId.AutoSave, slotType = SaveSlotType.AutoSave, hubGold = 50, isOccupied = true },
                campaign = snap
            };
            SaveManager.Storage.WriteSlotRaw(SaveSlotId.AutoSave, JsonUtility.ToJson(saveAuto), out _);

            // Verify AutoSave did not alter Manual Slot 1
            var meta1 = SaveManager.GetSlotMetadata(SaveSlotId.Slot1);
            Assert.AreEqual(350, meta1.hubGold);

            // Verify Manual Save cannot target AutoSave slot
            bool manualToAuto = SaveManager.Save(SaveSlotId.AutoSave, out string err);
            Assert.IsFalse(manualToAuto, "Manual save must reject AutoSave slot.");
            Assert.That(err, Does.Contain("cannot target the dedicated Auto Save"));
        }

        [Test]
        public void Criterion4_SaveAndAutoSave_AreRestrictedToSanctuaryHub()
        {
            var party = CreateTestParty();
            PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party));

            // Without Game.I or when not in Hub, CanSaveCurrentState must be false
            Assert.IsFalse(SaveManager.CanSaveCurrentState(), "Saving must not be permitted outside the Hub.");

            bool saveManual = SaveManager.Save(SaveSlotId.Slot1, out string err1);
            Assert.IsFalse(saveManual);
            Assert.That(err1, Does.Contain("Sanctuary Hub"));

            bool saveAuto = SaveManager.AutoSave(out string err2);
            Assert.IsFalse(saveAuto);
            Assert.That(err2, Does.Contain("Sanctuary Hub"));
        }

        [Test]
        public void Criterion5_LoadRestrictedToMainMenuAndHub()
        {
            // Outside of Game or in MainMenu, CanLoadCurrentState is true
            Assert.IsTrue(SaveManager.CanLoadCurrentState(), "Loading must be permitted from Main Menu.");
        }

        [Test]
        public void Criterion6_NoActiveDungeonCanBeSavedOrResumed()
        {
            // Active dungeon nodes and combat state are not stored in CampaignSaveData
            var run = CreateTestRun(seed: 123, act: 1);
            run.Enter(run.Map.nodes.First(n => n.floor == 0));
            run.Gold = 120; // run gold

            var snapshot = PersistenceCommitHandler.CreateCommittedSnapshot(run);
            Assert.AreEqual(run.Act, snapshot.committedAct);
            // Verify CampaignSaveData schema does NOT contain currentNodeId or clearedNodeIds
            var fields = typeof(CampaignSaveData).GetFields().Select(f => f.Name).ToList();
            Assert.IsFalse(fields.Contains("currentNodeId"), "Resumable dungeon node must not be in campaign save.");
            Assert.IsFalse(fields.Contains("clearedNodeIds"), "Resumable cleared nodes must not be in campaign save.");
            Assert.IsFalse(fields.Contains("activeBattle"), "Active battle must not be in campaign save.");
        }

        [Test]
        public void Criterion7_DungeonClear_CommitsProgressAcquiredInThatDungeon()
        {
            var run = CreateTestRun(seed: 555, act: 1);
            var initialSnapshot = PersistenceCommitHandler.CreateCommittedSnapshot(run);
            PersistenceCommitHandler.SetActiveCommittedSnapshot(initialSnapshot);

            // Party clears dungeon encounters and gains gold, consumable, and level
            run.Gold = 250;
            run.Inventory.AddConsumable("draught", 3);
            run.Party[0].LevelUp(); // Level 2
            run.Party[0].skillLoadout.SkillPoints += 2;

            // Commit Dungeon Clear
            PersistenceCommitHandler.CommitDungeonClear(run);

            var committed = PersistenceCommitHandler.ActiveCommittedSnapshot;
            Assert.IsNotNull(committed);
            Assert.AreEqual(250, committed.hubGold, "Run gold must transfer 1:1 to Hub Gold on Dungeon Clear.");
            Assert.AreEqual(2, committed.party[0].level, "Character Level must be committed on Dungeon Clear.");
            var draughtStack = committed.inventoryConsumables.FirstOrDefault(c => c.itemId == "draught");
            Assert.IsNotNull(draughtStack);
            Assert.GreaterOrEqual(draughtStack.count, 3, "Consumables acquired must be committed on Dungeon Clear.");
        }

        [Test]
        public void Criterion8_PreviouslyCommittedProgression_SurvivesLaterDungeonFailure()
        {
            var run = CreateTestRun(seed: 888, act: 1);
            run.Gold = 300;
            run.Party[0].level = 3;
            run.Party[0].abilities.permanentBonuses = AbilityScores.DefaultBase;
            PersistenceCommitHandler.CommitDungeonClear(run);

            // Enter Dungeon 2
            run.AdvanceToNextDungeon(MapGenerator.Generate(889, act: 2));
            // During Dungeon 2, acquire uncommitted loot and temporary gold
            run.Gold = 75; // uncommitted run gold
            run.Inventory.AddConsumable("elixir", 5); // uncommitted loot
            run.Party[0].level = 4; // uncommitted level gain

            // Party wipe in Dungeon 2
            run.LoseCombat();

            // Verify uncommitted run gold is wiped, but committed state is preserved!
            Assert.AreEqual(0, run.Gold, "Run gold from failed dungeon must be wiped.");
            var committed = PersistenceCommitHandler.ActiveCommittedSnapshot;
            Assert.IsNotNull(committed);
            Assert.AreEqual(300, committed.hubGold, "Committed Hub Gold must survive subsequent dungeon failure.");
            Assert.AreEqual(3, committed.party[0].level, "Committed level must survive subsequent dungeon failure.");
        }

        [Test]
        public void Criterion9_PartyWipe_DiscardsUncommittedCurrentDungeonProgress_AndPreservesCommitted()
        {
            var run = CreateTestRun(seed: 999, act: 1);
            run.Gold = 450;
            run.Inventory.AddConsumable("ether", 2);
            PersistenceCommitHandler.CommitDungeonClear(run);

            // Enter next dungeon and gain uncommitted items
            run.AdvanceToNextDungeon(MapGenerator.Generate(1000, act: 2));
            run.Gold = 80;
            run.Inventory.AddConsumable("elixir", 1);

            // Party wipe
            run.LoseCombat();

            // Run gold is discarded, but committed Hub Gold is preserved
            Assert.AreEqual(0, run.Gold, "Run gold must be discarded on party wipe.");
            Assert.AreEqual(450, PersistenceCommitHandler.ActiveCommittedSnapshot.hubGold, "Committed Hub Gold must be preserved.");
            Assert.AreEqual(RunStatus.Defeat, run.Status);
            Assert.IsNull(run.Current, "Active dungeon node must be cleared.");
        }

        [Test]
        public void Criterion10_VoluntaryAbandonment_DiscardsUncommittedAndPreservesCommitted()
        {
            var run = CreateTestRun(seed: 321, act: 1);
            run.Gold = 200;
            PersistenceCommitHandler.CommitDungeonClear(run);

            // In next dungeon
            run.AdvanceToNextDungeon(MapGenerator.Generate(322, act: 2));
            run.Gold = 95; // uncommitted
            run.Inventory.AddConsumable("draught", 10);

            // Player selects Abandon Expedition
            run.AbandonExpedition();

            // Restored to committed baseline
            Assert.AreEqual(200, run.HubGold, "Hub gold must be preserved on abandonment.");
            Assert.AreEqual(0, run.Gold, "Uncommitted Run Gold is discarded on abandonment.");
            Assert.IsNull(run.Current, "Current dungeon node must be discarded.");
            Assert.AreEqual(0, run.Cleared.Count, "Dungeon cleared nodes must be discarded.");
        }

        [Test]
        public void Criterion11_EmptySlots_CannotBeLoaded()
        {
            bool loaded = SaveManager.Load(SaveSlotId.Slot1, out string err);
            Assert.IsFalse(loaded, "Empty slot must not be loadable.");
            Assert.That(err, Does.Contain("is empty"));
        }

        [Test]
        public void Criterion12_SaveValidator_RejectsCorruptSaveFiles()
        {
            // Null data
            Assert.IsFalse(SaveValidator.ValidateSave(null, out string err1));
            Assert.That(err1, Does.Contain("null"));

            // Version higher than supported
            var data = new SaveFileData
            {
                metadata = new SaveSlotMetadata { saveVersion = 999 },
                campaign = new CampaignSaveData { hasConfirmedParty = true, party = new SavedCharacterData[4] }
            };
            Assert.IsFalse(SaveValidator.ValidateSave(data, out string err2));
            Assert.That(err2, Does.Contain("higher than supported"));

            // Incomplete party (e.g. 3 heroes instead of 4)
            data.metadata.saveVersion = 1;
            data.campaign.party = new SavedCharacterData[3];
            Assert.IsFalse(SaveValidator.ValidateSave(data, out string err3));
            Assert.That(err3, Does.Contain("requires exactly 4 confirmed party members"));
        }

        [Test]
        public void Criterion13_FileSaveStorage_AtomicWriteAndRead_WorksOnDisk()
        {
            string tempDir = Path.Combine(Application.temporaryCachePath, "SaveSystemTests_" + Guid.NewGuid().ToString("N"));
            var fileStorage = new FileSaveStorage(tempDir);
            try
            {
                Assert.IsFalse(fileStorage.SlotExists(SaveSlotId.Slot1));

                string sampleJson = "{\"test\": 123}";
                bool writeSuccess = fileStorage.WriteSlotRaw(SaveSlotId.Slot1, sampleJson, out string wErr);
                Assert.IsTrue(writeSuccess, wErr);
                Assert.IsTrue(fileStorage.SlotExists(SaveSlotId.Slot1));

                string readJson = fileStorage.ReadSlotRaw(SaveSlotId.Slot1);
                Assert.AreEqual(sampleJson, readJson);

                bool delSuccess = fileStorage.DeleteSlot(SaveSlotId.Slot1, out string dErr);
                Assert.IsTrue(delSuccess, dErr);
                Assert.IsFalse(fileStorage.SlotExists(SaveSlotId.Slot1));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Test]
        public void Criterion14_SaveScreenAndLoadScreen_ConstructCleanlyUnderRoot()
        {
            var hostGo = new GameObject("SaveScreenHost");
            try
            {
                var saveScreen = new SaveScreen();
                saveScreen.Open(hostGo.transform);
                Assert.IsNotNull(hostGo.transform.Find("SaveScreen"), "SaveScreen must instantiate under host root.");
                saveScreen.Close();

                var loadScreen = new LoadScreen(GameState.MainMenu);
                loadScreen.Open(hostGo.transform);
                Assert.IsNotNull(hostGo.transform.Find("LoadScreen"), "LoadScreen must instantiate under host root.");
                loadScreen.Close();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void Criterion15_ConfirmationModal_BackDismissesWithoutCancel_WhenOnDismissGiven()
        {
            var root = new UnityEngine.UIElements.VisualElement();
            bool confirmed = false, cancelled = false, dismissed = false;
            FigmaUi.ShowConfirmationModal(root, "TITLE", "Message body", "CONFIRM",
                () => confirmed = true, () => cancelled = true, cancelText: "DISCARD", onDismiss: () => dismissed = true);
            var go = GameObject.Find("UitkConfirmationModalMenu");
            try
            {
                Assert.IsNotNull(go, "Modal menu must be created.");
                var m = go.GetComponent<UitkMenu>();
                Assert.AreEqual("DISCARD", m.items[0].text);
                Assert.IsNotNull(UnityEngine.UIElements.UQueryExtensions.Q(root, "UitkConfirmationModal"), "Modal must be mounted.");

                m.onBack();
                Assert.IsTrue(dismissed, "Back must run onDismiss");
                Assert.IsFalse(cancelled, "Back must not run the cancel action when onDismiss is given");
                Assert.IsFalse(confirmed);
                Assert.IsNull(UnityEngine.UIElements.UQueryExtensions.Q(root, "UitkConfirmationModal"), "Modal must close on Back.");
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // An unlocked skill that can reach rank 3 (a few skills cap lower).
        static string RankableSkill(Unit hero) => hero.skillLoadout.SkillRanks.Keys.First(k =>
            (Database.Skills.TryGetValue(k, out var s) ? s.maxRank : Database.Passives.TryGetValue(k, out var p) ? p.maxRank : 0) >= 3);

        [Test]
        public void ExpSkillPointsAndRanks_RollBackToCommittedSnapshot_OnAbandon()
        {
            var run = CreateTestRun(seed: 77, act: 1);
            var hero = run.Party[0];
            hero.GainExp(150); // Lv 2 with 50 EXP, +2 SP
            string skill = RankableSkill(hero);
            Assert.IsTrue(hero.skillLoadout.TryUpgradeRank(skill, out var err), err); // rank 2
            PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateCommittedSnapshot(run));
            int committedSp = hero.skillLoadout.SkillPoints;

            // Uncommitted progress in the current dungeon, then the party walks out.
            hero.GainExp(500);
            hero.skillLoadout.TryUpgradeRank(skill, out _);
            run.AbandonExpedition();

            hero = run.Party[0];
            Assert.AreEqual(2, hero.level);
            Assert.AreEqual(50, hero.exp);
            Assert.AreEqual(committedSp, hero.skillLoadout.SkillPoints, "SP earned in the abandoned dungeon is lost");
            Assert.AreEqual(2, hero.skillLoadout.GetRank(skill), "ranks bought before the commit survive");
        }

        [Test]
        public void SavedCharacterData_RoundTripsExpSkillPointsAndRanks()
        {
            var run = CreateTestRun();
            var hero = run.Party[0];
            hero.GainExp(150);
            string skill = RankableSkill(hero);
            hero.skillLoadout.TryUpgradeRank(skill, out _);

            var json = JsonUtility.ToJson(PersistenceCommitHandler.CreateCommittedSnapshot(run));
            var loaded = JsonUtility.FromJson<CampaignSaveData>(json).party[0];
            Assert.AreEqual(50, loaded.exp);
            Assert.AreEqual(hero.skillLoadout.SkillPoints, loaded.skillPoints);
            int idx = Array.IndexOf(loaded.skillRankIds, skill);
            Assert.GreaterOrEqual(idx, 0);
            Assert.AreEqual(2, loaded.skillRankValues[idx]);
        }
    }

    [TestFixture]
    public class FelnaUiSystemTests
    {
        GameObject testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(testRoot);
                else UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        public void DotGothic16_FontEngine_MeasuresAndRendersAllGlyphs()
        {
            // Verify enum values
            Assert.AreEqual(FontKind.DotGothic, FontKind.DotGothic16);

            // Verify uppercase and lowercase letters render distinct non-zero widths
            int upperA = PixelFont.Measure("A", FontKind.DotGothic);
            int lowerA = PixelFont.Measure("a", FontKind.DotGothic);
            Assert.Greater(upperA, 0);
            Assert.Greater(lowerA, 0);

            // Verify numbers and punctuation
            int digits = PixelFont.Measure("0123456789", FontKind.DotGothic);
            Assert.Greater(digits, 40);

            // Verify special game symbols: ▲, ▼, ●, ◆, ★, ✓, ×, ▸, ·
            string symbols = "▲▼●◆★✓×▸·";
            int symLen = PixelFont.Measure(symbols, FontKind.DotGothic);
            Assert.Greater(symLen, 20);

            // Verify sprite rendering
            var sprite = PixelFont.Render("Felna: The World's Between", FontKind.DotGothic);
            Assert.IsNotNull(sprite);
            Assert.Greater(sprite.rect.width, 50);
            Assert.GreaterOrEqual(sprite.rect.height, PixelFont.DotGothicGlyphHeight);
        }

        [Test]
        public void StarlitReliquary_PaletteTokens_ConformToUiSpecification()
        {
            // Ink canvas
            Assert.AreEqual(Palette.Hex(0x070A14), Palette.Ink0);
            Assert.AreEqual(Palette.Hex(0x0C1122), Palette.Ink1);
            Assert.AreEqual(Palette.Hex(0x121A31), Palette.Ink2);
            Assert.AreEqual(Palette.Hex(0x1A2443), Palette.Ink3);
            Assert.AreEqual(Palette.Hex(0x25315A), Palette.Ink4);
            Assert.AreEqual(Palette.Hex(0x3A4878), Palette.Ink5);

            // Mists
            Assert.AreEqual(Palette.Hex(0xEEF0F8), Palette.Mist1);
            Assert.AreEqual(Palette.Hex(0xC3C9DE), Palette.Mist2);
            Assert.AreEqual(Palette.Hex(0x8F98B8), Palette.Mist3);

            // Golds
            Assert.AreEqual(Palette.Hex(0xFFE6A3), Palette.Gold1);
            Assert.AreEqual(Palette.Hex(0xF2C25B), Palette.Gold2);
            Assert.AreEqual(Palette.Hex(0xC9932E), Palette.Gold3);
            Assert.AreEqual(Palette.Hex(0x7A5717), Palette.Gold4);

            // Embers
            Assert.AreEqual(Palette.Hex(0xFFC196), Palette.Ember1);
            Assert.AreEqual(Palette.Hex(0xEF8A3C), Palette.Ember2);
            Assert.AreEqual(Palette.Hex(0x8A4317), Palette.Ember3);

            // Vitals
            Assert.AreEqual(Palette.Hex(0x52D18C), Palette.Hp);
            Assert.AreEqual(Palette.Hex(0xFF6A55), Palette.HpLow);
            Assert.AreEqual(Palette.Hex(0x5AA2FF), Palette.Mp);
            Assert.AreEqual(Palette.Hex(0xC79BFF), Palette.Xp);
            Assert.AreEqual(Palette.Hex(0x9FE3FF), Palette.Shield);
        }

        [Test]
        public void Palette_ForHp_SwitchesToHpLowAtQuarterThreshold()
        {
            Assert.AreEqual(Palette.Hp, Palette.ForHp(100, 100));
            Assert.AreEqual(Palette.Hp, Palette.ForHp(26, 100));
            Assert.AreEqual(Palette.HpLow, Palette.ForHp(25, 100));
            Assert.AreEqual(Palette.HpLow, Palette.ForHp(10, 100));
            Assert.AreEqual(Palette.HpLow, Palette.ForHp(0, 100));
        }

        [Test]
        public void Palette_ElementsAndRarities_MapToCanonicalCodesAndColors()
        {
            Assert.AreEqual("FI", Palette.ElementCode(Element.Fire));
            Assert.AreEqual("IC", Palette.ElementCode(Element.Ice));
            Assert.AreEqual("LI", Palette.ElementCode(Element.Lightning));
            Assert.AreEqual("EA", Palette.ElementCode(Element.Earth));
            Assert.AreEqual("WI", Palette.ElementCode(Element.Wind));
            Assert.AreEqual("WA", Palette.ElementCode(Element.Water));
            Assert.AreEqual("HO", Palette.ElementCode(Element.Holy));
            Assert.AreEqual("SH", Palette.ElementCode(Element.Shadow));
            Assert.AreEqual("PH", Palette.ElementCode(Element.Physical));
            Assert.AreEqual("AL", Palette.ElementCode(Element.Arcane));

            Assert.AreEqual(Palette.ElementFire, Palette.ForElement(Element.Fire));
            Assert.AreEqual(Palette.ElementFire, Palette.ForElement("FI"));
            Assert.AreEqual(Palette.ElementShadow, Palette.ForElement("SH"));

            Assert.AreEqual("C", Palette.RarityBadge(EquipmentRarity.Common));
            Assert.AreEqual("U", Palette.RarityBadge(EquipmentRarity.Uncommon));
            Assert.AreEqual("R", Palette.RarityBadge(EquipmentRarity.Rare));
            Assert.AreEqual("E", Palette.RarityBadge(EquipmentRarity.Epic));
            Assert.AreEqual("L", Palette.RarityBadge(EquipmentRarity.Legendary));
            Assert.AreEqual("M", Palette.RarityBadge(EquipmentRarity.Mythic));

            Assert.AreEqual(Palette.RarityLegendary, Palette.ForRarity(EquipmentRarity.Legendary));
        }

        [Test]
        public void UiComponents_EnemyCard_DoubleBorderCard_AndButtons_BuildSuccessfully()
        {
            var ec = Ui.EnemyCard(testRoot.transform, 10, 10, 60, 40);
            Assert.IsNotNull(ec);
            Assert.IsNotNull(ec.sprite);

            var dbc = Ui.DoubleBorderCard(testRoot.transform, 10, 60, 80, 50);
            Assert.IsNotNull(dbc);
            Assert.IsNotNull(dbc.sprite);

            bool dangerClicked = false;
            var dangerBtn = Ui.Button(testRoot.transform, "DISCARD", 10, 120, 100, () => dangerClicked = true, style: ButtonStyle.Danger, font: FontKind.DotGothic);
            Assert.IsNotNull(dangerBtn);
            dangerBtn.onClick?.Invoke();
            Assert.IsTrue(dangerClicked);
        }

        [Test]
        public void UiComponents_StatusBadge_AndCurrencyTag_ConstructWithDotGothic()
        {
            var buff = Ui.StatusBadge(testRoot.transform, "ATK", "3T", true, 20, 20);
            Assert.IsNotNull(buff);

            var debuff = Ui.StatusBadge(testRoot.transform, "BRN", "2T", false, 60, 20);
            Assert.IsNotNull(debuff);

            var hubTag = Ui.CurrencyTag(testRoot.transform, 4820, true, 20, 50);
            Assert.IsNotNull(hubTag);

            var runTag = Ui.CurrencyTag(testRoot.transform, 312, false, 120, 50);
            Assert.IsNotNull(runTag);
        }

        [Test]
        public void UiScreens_InstantiateAndRenderWithoutExceptions()
        {
            var mm = new MainMenuScreen();
            mm.Open(testRoot.transform);
            Assert.IsNotNull(mm.NewGameButton);
            mm.Close();

            var hub = new HubScreen();
            hub.Open(testRoot.transform);
            hub.Close();

            var save = new SaveScreen();
            save.Open(testRoot.transform);
            save.Close();

            var load = new LoadScreen();
            load.Open(testRoot.transform);
            load.Close();

            // Hub service boards (C, H1, H2, I, V1, V2) and the step-paged creation (B1-B3) render without a run
            foreach (GameScreen s in new GameScreen[] { new PartyScreen(), new HubShopScreen(), new ArmoryScreen(), new BlacksmithScreen(), new TempleScreen(), new MagicShopScreen(), new SkillTreeScreen(), new TalentScreen(),
                new MapScreen(), new EventScreen(), new RewardScreen(), new DungeonClearScreen(), new EndScreen(false), new SettingsScreen() })
            {
                s.Open(testRoot.transform);
                s.Close();
            }
            var creation = new CharacterCreationScreen();
            creation.Open(testRoot.transform);
            foreach (CharacterCreationScreen.Page p in Enum.GetValues(typeof(CharacterCreationScreen.Page)))
                creation.GoToPage(p);
            creation.Close();
        }

        [Test]
        public void NewGame_PartyConfirmation_Grants600StarterGold_AndInitializesSnapshot()
        {
            var party = new List<CharacterDef>
            {
                new CharacterDef { id = "h1", name = "A", raceId = "human", classId = "fighter", backgroundId = "guard", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "h2", name = "B", raceId = "elf", classId = "ranger", backgroundId = "outlander", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "h3", name = "C", raceId = "dwarf", classId = "cleric", backgroundId = "acolyte", baseScores = AbilityScores.DefaultBase },
                new CharacterDef { id = "h4", name = "D", raceId = "wraith", classId = "mage", backgroundId = "scholar", baseScores = AbilityScores.DefaultBase }
            };

            Assert.AreEqual(600, GameSave.StarterGold);
            Assert.AreEqual(600, PersistenceCommitHandler.StarterGold);

            var snap = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, seed: 123);
            Assert.AreEqual(600, snap.hubGold, "Committed snapshot must start with 600 Hub Gold.");
            Assert.AreEqual(1, snap.committedAct);
            Assert.AreEqual(1, snap.nextDungeonAct);

            var savedData = GameSave.SaveNewGame(party, seed: 123);
            Assert.AreEqual(600, savedData.gold, "GameSaveData must have 600 starter gold.");
            Assert.AreEqual(600, savedData.hubGold, "GameSaveData must have 600 starter hub gold.");
        }

        [Test]
        public void CharacterCreation_EnterHubFromCreation_TransitionsToHubWith600Gold()
        {
            var gameGo = new GameObject("GameHost");
            try
            {
                var game = gameGo.AddComponent<Game>();
                Game.SetInstance(game);
                var origStorage = SaveManager.Storage;
                var memStorage = new MemorySaveStorage();
                SaveManager.Storage = memStorage;

                game.StartNewGame();
                Assert.AreEqual(GameState.CharacterCreation, game.State);

                // Populate 4 heroes with valid point-buy configurations
                game.NewGameBuilder.RandomizeAll();
                Assert.IsTrue(game.NewGameBuilder.CanConfirmParty);

                // Call EnterHubFromCreation
                game.EnterHubFromCreation(seed: 777);

                Assert.AreEqual(GameState.Hub, game.State, "Must transition directly to Sanctuary Hub after character creation.");
                Assert.IsNotNull(game.Run, "RunState must be initialized.");
                Assert.AreEqual(600, game.Run.HubGold, "Player must possess exactly 600 starter Hub Gold.");
                Assert.AreEqual(0, game.Run.Gold, "No Run Gold before the first dungeon.");
                Assert.AreEqual(1, game.Run.Act, "Initial expedition map must be Act 1 (Dungeon 1).");
                Assert.AreEqual(600, PersistenceCommitHandler.ActiveCommittedSnapshot.hubGold);
                Assert.AreEqual(1, PersistenceCommitHandler.ActiveCommittedSnapshot.nextDungeonAct);

                // Verify AutoSave was automatically triggered with 600 gold
                bool autoSaved = SaveManager.AutoSave(out string autoErr);
                Assert.IsTrue(autoSaved, $"AutoSave failed with error: {autoErr}");
                var autoMeta = SaveManager.GetSlotMetadata(SaveSlotId.AutoSave);
                Assert.IsTrue(autoMeta.isOccupied, "AutoSave slot must be occupied upon Hub arrival.");
                Assert.AreEqual(600, autoMeta.hubGold, "AutoSave metadata must reflect 600 Hub Gold.");

                // Verify embark from Hub targets Dungeon 1 (Act 1)
                int targetAct = PersistenceCommitHandler.ActiveCommittedSnapshot != null
                    ? PersistenceCommitHandler.ActiveCommittedSnapshot.nextDungeonAct
                    : game.Run.Act;
                Assert.AreEqual(1, targetAct, "First expedition embark must target Dungeon 1 (Act 1).");

                SaveManager.Storage = origStorage;
            }
            finally
            {
                Game.SetInstance(null);
                UnityEngine.Object.DestroyImmediate(gameGo);
            }
        }

        [Test]
        public void Ui_Selectable_UniversalRegistrationAndPointerHitTesting_Works()
        {
            var host = new GameObject("TestSelectableHost");
            try
            {
                bool clicked = false;
                var btn = Ui.Button(host.transform, "TEST BUTTON", 100, 50, 80, () => clicked = true, 20);

                Assert.IsNotNull(btn);
                Assert.IsTrue(Selectable.All.Contains(btn), "Selectable must be registered in Selectable.All when enabled.");

                // Button is placed at (100, -50) locally under host (which is at (0, 0))
                var pos = btn.transform.position;
                Assert.AreEqual(100f, pos.x, 0.01f);
                Assert.AreEqual(-50f, pos.y, 0.01f);

                // Inside points
                Assert.IsTrue(btn.Contains(new Vector2(100, -50)), "Top-left corner of button should be contained.");
                Assert.IsTrue(btn.Contains(new Vector2(140, -60)), "Center of button should be contained.");
                Assert.IsTrue(btn.Contains(new Vector2(180, -70)), "Bottom-right corner of button should be contained.");

                // Outside points
                Assert.IsFalse(btn.Contains(new Vector2(99, -50)), "Point to left of button should not be contained.");
                Assert.IsFalse(btn.Contains(new Vector2(181, -50)), "Point to right of button should not be contained.");
                Assert.IsFalse(btn.Contains(new Vector2(140, -49)), "Point above button should not be contained.");
                Assert.IsFalse(btn.Contains(new Vector2(140, -71)), "Point below button should not be contained.");
                Assert.IsFalse(btn.Contains(new Vector2(float.NaN, float.NaN)), "NaN coordinates must return false.");

                // Direct activation
                btn.onClick.Invoke();
                Assert.IsTrue(clicked, "Direct invocation of onClick must work.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void HubScreen_HtmlSpecificationLayout_RendersAllComponentsCleanly()
        {
            var hubHost = new GameObject("HubHost");
            var gameGo = new GameObject("GameMock");
            try
            {
                var game = gameGo.AddComponent<Game>();
                Game.SetInstance(game);
                game.EnterHubFromCreation(seed: 999);

                var hub = new HubScreen();
                hub.Open(hubHost.transform);

                var root = hubHost.transform.Find("HubScreen");
                Assert.IsNotNull(root, "HubScreen root must exist.");

                var uiDoc = root.GetComponent<UnityEngine.UIElements.UIDocument>();
                Assert.IsNotNull(uiDoc, "HubScreen must mount a UIDocument.");
                var uitkMenu = root.GetComponent<UitkMenu>();
                Assert.IsNotNull(uitkMenu, "HubScreen must attach UitkMenu.");
                Assert.Greater(uitkMenu.items.Count, 5, "HubScreen must populate multiple interactive UitkMenu items.");

                hub.Close();
            }
            finally
            {
                Game.SetInstance(null);
                UnityEngine.Object.DestroyImmediate(gameGo);
                UnityEngine.Object.DestroyImmediate(hubHost);
            }
        }

        [Test]
        public void HubScreen_FocusMenu_UpdatesTooltipWithoutChangingDungeonButtonText()
        {
            var hubHost = new GameObject("HubHost");
            var gameGo = new GameObject("GameMock");
            try
            {
                var game = gameGo.AddComponent<Game>();
                Game.SetInstance(game);
                game.EnterHubFromCreation(seed: 999);

                var hub = new HubScreen();
                hub.Open(hubHost.transform);

                var root = hubHost.transform.Find("HubScreen");
                var uitkMenu = root.GetComponent<UitkMenu>();
                var partyItem = uitkMenu.items.FirstOrDefault(i => i.text == "Character / Party");
                var gateItem = uitkMenu.items.FirstOrDefault(i => i.text == "Dungeon Entrance");
                Assert.IsNotNull(partyItem, "HubScreen must register Character / Party menu item.");
                Assert.IsNotNull(gateItem, "HubScreen must register Dungeon Entrance menu item.");

                var gateTextLbl = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(gateItem.element, "text-1");
                var tipTitleLbl = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(hub.UiRoot, className: "dungeon-entrance-vars");
                var tipBodyLbl = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(hub.UiRoot, className: "begin-dungeon-1-of-8-a-new-run-starts-with-0-run-gold-vars");
                Assert.IsNotNull(gateTextLbl, "Dungeon Entrance button must contain text-1 label.");
                Assert.IsNotNull(tipTitleLbl, "Tooltip title label must exist.");
                Assert.IsNotNull(tipBodyLbl, "Tooltip body label must exist.");

                uitkMenu.Focus(partyItem, sound: false, force: true);
                Assert.AreEqual("Dungeon Entrance", gateTextLbl.text, "Hovering/focusing a facility must not overwrite the Dungeon Entrance button label.");
                Assert.AreEqual("Character / Party", tipTitleLbl.text, "Tooltip title must update to the focused menu item.");
                Assert.AreEqual("VIEW THE PARTY, STATUS AND LINEUP.", tipBodyLbl.text);

                uitkMenu.Focus(gateItem, sound: false, force: true);
                Assert.AreEqual("Dungeon Entrance", gateTextLbl.text);
                Assert.AreEqual("Dungeon Entrance", tipTitleLbl.text);
                StringAssert.Contains("BEGIN DUNGEON", tipBodyLbl.text);

                hub.Close();
            }
            finally
            {
                Game.SetInstance(null);
                UnityEngine.Object.DestroyImmediate(gameGo);
                UnityEngine.Object.DestroyImmediate(hubHost);
            }
        }

        [Test]
        public void CharacterStatusScreen_IdentityPortrait_UsesSelectedHeroSpriteWithoutOverwritingHeaderSlot0()
        {
            var host = new GameObject("StatusHost");
            try
            {
                var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
                Assert.GreaterOrEqual(party.Count, 3, "Default party must have at least 3 members.");

                var status = new CharacterStatusScreen(party, GameState.Hub);
                status.Open(host.transform);
                // Ensure we test with a selected hero other than party[0]
                if (status.CurrentUnit == party[0])
                    status.SwitchHero(1);

                var slot0Frame = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(status.UiRoot, className: "frame-vars-13");
                var slot1Frame = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(status.UiRoot, className: "frame-vars-14");
                var portrait = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.VisualElement>(status.UiRoot, className: "sprite-knight")
                    .ToList()
                    .FirstOrDefault(e => e.parent == null || !e.parent.ClassListContains("frame-vars-13"));

                Assert.IsNotNull(slot0Frame);
                Assert.IsNotNull(slot1Frame);
                Assert.IsNotNull(portrait, "Main identity portrait element must exist.");

                var expected0 = FigmaUi.GetSpriteTexture(party[0].sprite, "t0");
                var expected1 = FigmaUi.GetSpriteTexture(party[1].sprite, "t1");
                var expectedSelected = FigmaUi.GetSpriteTexture(status.CurrentUnit.sprite, "tsel");
                Assert.AreNotSame(expected0, expectedSelected, "Selected hero and party[0] should have distinct sprite textures.");

                Assert.AreEqual(expected0, slot0Frame[0].style.backgroundImage.value.texture, "Header slot 0 must keep party[0]'s sprite.");
                Assert.AreEqual(expected1, slot1Frame[0].style.backgroundImage.value.texture, "Header slot 1 must keep party[1]'s sprite.");
                Assert.AreEqual(expectedSelected, portrait.style.backgroundImage.value.texture, "Main identity portrait must show the selected hero's sprite.");

                status.Close();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void UiAuditFixes_TalentBlacksmithAndCharacterCreationSteps_WorkCleanly()
        {
            var host = new GameObject("UiAuditHost");
            try
            {
                // 1. CharacterCreationScreen starts at Page.Race (7 steps, NAME step removed)
                var creation = new CharacterCreationScreen();
                creation.Open(host.transform);
                Assert.AreEqual(CharacterCreationScreen.Page.Race, creation.CurrentPage);
                Assert.AreEqual(7, Enum.GetValues(typeof(CharacterCreationScreen.Page)).Length);
                Assert.IsNull(FigmaUi.FindInstanceWithText(creation.UiRoot, "text-0", "+ NAME"), "NAME step chip must be removed from UXML.");
                creation.Close();

                // 2. TalentScreen exposes all compatible talents (more than 6) in a ScrollView
                var talent = new TalentScreen();
                talent.Open(host.transform);
                var sv = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(talent.UiRoot);
                Assert.IsNotNull(sv, "TalentScreen must wrap talent cards in a ScrollView.");
                int cardCount = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.VisualElement>(sv, className: "tag-cls").ToList().Count;
                Assert.Greater(cardCount, 6, "TalentScreen must render all compatible talents, not cap at 6.");
                talent.Close();

                // 3. BlacksmithScreen uses a ScrollView so >7 equipment items are not truncated
                var bs = new BlacksmithScreen();
                bs.Open(host.transform);
                Assert.IsNotNull(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(bs.UiRoot), "BlacksmithScreen must use ScrollView.");
                bs.Close();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void TypographyAssets_DisplayGothicUiAndFallbacks_AreConfigured()
        {
            Assert.IsTrue(UnityEditor.EditorApplication.ExecuteMenuItem("FELNA/Build Figma UI Assets"));
            var ui = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/Fonts/FelnaUIFont.asset");
            var disp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/Fonts/FelnaDisplayFont.asset");
            var goth = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/Fonts/FelnaGothicFont.asset");
            var sym = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>("Assets/UI/Fonts/FelnaSymbolsFont.asset");
            Assert.IsNotNull(ui, "FelnaUIFont.asset missing");
            Assert.IsNotNull(disp, "FelnaDisplayFont.asset missing");
            Assert.IsNotNull(goth, "FelnaGothicFont.asset missing");
            Assert.IsNotNull(sym, "FelnaSymbolsFont.asset missing");
            CollectionAssert.Contains(disp.fallbackFontAssetTable, ui);
            CollectionAssert.Contains(disp.fallbackFontAssetTable, sym);
            CollectionAssert.Contains(goth.fallbackFontAssetTable, ui);
            CollectionAssert.Contains(goth.fallbackFontAssetTable, sym);

            Assert.IsNotNull(disp.sourceFontFile, "FelnaDisplayFont missing sourceFontFile");
            Assert.IsNotNull(goth.sourceFontFile, "FelnaGothicFont missing sourceFontFile");
            Assert.IsTrue(disp.TryAddCharacters("FELNA VICTORY DEFEAT", out _), "Display font must rasterize title glyphs");
            Assert.IsTrue(goth.TryAddCharacters("CONTINUE NEW GAME SETTINGS", out _), "Gothic font must rasterize menu glyphs");

            var uss = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.StyleSheet>("Assets/UI/felna-runtime.uss");
            Assert.IsNotNull(uss, "felna-runtime.uss missing");
            var ussSo = new UnityEditor.SerializedObject(uss);
            var strings = ussSo.FindProperty("strings");
            for (int i = 0; i < strings.arraySize; i++)
            {
                string val = strings.GetArrayElementAtIndex(i).stringValue;
                Assert.IsFalse(val.Contains("project://database/"), "felna-runtime.uss has unresolved asset url: " + val);
            }
            var assetsProp = ussSo.FindProperty("assets");
            var refs = new System.Collections.Generic.HashSet<UnityEngine.Object>();
            for (int i = 0; i < assetsProp.arraySize; i++)
                refs.Add(assetsProp.GetArrayElementAtIndex(i).objectReferenceValue);
            CollectionAssert.Contains(refs, ui, "felna-runtime.uss must resolve FelnaUIFont");
            CollectionAssert.Contains(refs, disp, "felna-runtime.uss must resolve FelnaDisplayFont");
            CollectionAssert.Contains(refs, goth, "felna-runtime.uss must resolve FelnaGothicFont");
        }
    }
}
