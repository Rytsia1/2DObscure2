using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    public enum SaveSlotId
    {
        AutoSave = 0,
        Slot1 = 1,
        Slot2 = 2,
        Slot3 = 3,
        Slot4 = 4
    }

    public enum SaveSlotType
    {
        AutoSave,
        Manual
    }

    [Serializable]
    public class SaveSlotMetadata
    {
        public int saveVersion = 1;
        public SaveSlotId slotId;
        public SaveSlotType slotType;
        public string createdAtIso;
        public string lastSavedAtIso;
        public long lastSavedTicks;
        public int committedAct = 1;
        public string committedDungeonName = "Geistwald";
        public string partySummary = "4 Heroes";
        public int partyAverageLevel = 1;
        public int hubGold = 0;
        public float playtimeSeconds = 0f;
        public bool isOccupied = false;

        public static string FormatSlotName(SaveSlotId slot) => slot switch
        {
            SaveSlotId.AutoSave => "AUTO SAVE",
            SaveSlotId.Slot1 => "MANUAL SLOT 1",
            SaveSlotId.Slot2 => "MANUAL SLOT 2",
            SaveSlotId.Slot3 => "MANUAL SLOT 3",
            SaveSlotId.Slot4 => "MANUAL SLOT 4",
            _ => slot.ToString().ToUpperInvariant()
        };

        public static string GetDungeonName(int act) => act switch
        {
            1 => "Geistwald (Layer 1)",
            2 => "Sunken Spire (Layer 2)",
            3 => "Obsidian Reach (Layer 3)",
            4 => "Ashen Vale (Layer 4)",
            5 => "Crystalline Hollow (Layer 5)",
            6 => "Abyssal Crypt (Layer 6)",
            7 => "Shattered Sanctum (Layer 7)",
            8 => "The Rift Core (Layer 8)",
            _ => $"Dungeon {act}"
        };

        public string GetFormattedPlaytime()
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, (double)playtimeSeconds));
            return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        }
    }

    [Serializable]
    public class CampaignSaveData
    {
        public bool hasConfirmedParty = true;
        public int seed = 1;
        public int committedAct = 1;
        public int nextDungeonAct = 1;
        public int hubGold = 0;
        public float playtimeSeconds = 0f;

        // Character Roster (exact 4 confirmed heroes)
        public SavedCharacterData[] party = Array.Empty<SavedCharacterData>();

        // Persistent Inventory & Equipment
        public EquipmentInstanceSaveData[] inventoryEquipment = Array.Empty<EquipmentInstanceSaveData>();
        public ConsumableStackSaveData[] inventoryConsumables = Array.Empty<ConsumableStackSaveData>();
        public MaterialEntrySaveData[] inventoryMaterials = Array.Empty<MaterialEntrySaveData>();
        public string[] inventoryKeyItems = Array.Empty<string>();

        // Persistent Relics (Shared Party Relics)
        public string[] relics = Array.Empty<string>();
        public RelicInstanceSaveData[] relicInstances = Array.Empty<RelicInstanceSaveData>();

        // Hub State
        public ShopItemSaveData[] hubArmoryStock = Array.Empty<ShopItemSaveData>();
    }

    [Serializable]
    public class SaveFileData
    {
        public SaveSlotMetadata metadata;
        public CampaignSaveData campaign;
    }

    public interface ISaveStorage
    {
        bool SlotExists(SaveSlotId slot);
        string ReadSlotRaw(SaveSlotId slot);
        bool WriteSlotRaw(SaveSlotId slot, string rawJson, out string error);
        bool DeleteSlot(SaveSlotId slot, out string error);
        void ClearAll();
        string GetPhysicalPath(SaveSlotId slot);
    }

    public class FileSaveStorage : ISaveStorage
    {
        readonly string saveDirectory;

        public FileSaveStorage(string customDirectory = null)
        {
            saveDirectory = customDirectory ?? Path.Combine(Application.persistentDataPath, "Saves");
            EnsureDirectory();
        }

        void EnsureDirectory()
        {
            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);
        }

        string GetFileName(SaveSlotId slot) => slot switch
        {
            SaveSlotId.AutoSave => "autosave.json",
            SaveSlotId.Slot1 => "slot_1.json",
            SaveSlotId.Slot2 => "slot_2.json",
            SaveSlotId.Slot3 => "slot_3.json",
            SaveSlotId.Slot4 => "slot_4.json",
            _ => $"slot_{(int)slot}.json"
        };

        public string GetPhysicalPath(SaveSlotId slot) => Path.Combine(saveDirectory, GetFileName(slot));

        public bool SlotExists(SaveSlotId slot)
        {
            string path = GetPhysicalPath(slot);
            return File.Exists(path);
        }

        public string ReadSlotRaw(SaveSlotId slot)
        {
            string path = GetPhysicalPath(slot);
            if (!File.Exists(path)) return null;
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveStorage] Failed to read {slot}: {ex.Message}");
                return null;
            }
        }

        public bool WriteSlotRaw(SaveSlotId slot, string rawJson, out string error)
        {
            error = null;
            EnsureDirectory();
            string targetPath = GetPhysicalPath(slot);
            string tempPath = targetPath + ".tmp";
            try
            {
                File.WriteAllText(tempPath, rawJson);
                if (File.Exists(targetPath))
                    File.Delete(targetPath);
                File.Move(tempPath, targetPath);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Write failed: {ex.Message}";
                Debug.LogError($"[FileSaveStorage] Write error for {slot}: {ex.Message}");
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                return false;
            }
        }

        public bool DeleteSlot(SaveSlotId slot, out string error)
        {
            error = null;
            string path = GetPhysicalPath(slot);
            if (!File.Exists(path)) return true;
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Delete failed: {ex.Message}";
                return false;
            }
        }

        public void ClearAll()
        {
            foreach (SaveSlotId slot in Enum.GetValues(typeof(SaveSlotId)))
                DeleteSlot(slot, out _);
        }
    }

    public class MemorySaveStorage : ISaveStorage
    {
        readonly Dictionary<SaveSlotId, string> storage = new Dictionary<SaveSlotId, string>();

        public bool SlotExists(SaveSlotId slot) => storage.ContainsKey(slot);

        public string ReadSlotRaw(SaveSlotId slot)
        {
            return storage.TryGetValue(slot, out string val) ? val : null;
        }

        public bool WriteSlotRaw(SaveSlotId slot, string rawJson, out string error)
        {
            error = null;
            storage[slot] = rawJson;
            return true;
        }

        public bool DeleteSlot(SaveSlotId slot, out string error)
        {
            error = null;
            storage.Remove(slot);
            return true;
        }

        public void ClearAll() => storage.Clear();

        public string GetPhysicalPath(SaveSlotId slot) => $"memory://{slot}";
    }

    public static class SaveValidator
    {
        public static bool ValidateSave(SaveFileData data, out string error)
        {
            error = null;
            if (data == null)
            {
                error = "Save data is null.";
                return false;
            }
            if (data.metadata == null)
            {
                error = "Save metadata is missing.";
                return false;
            }
            if (data.campaign == null)
            {
                error = "Campaign data is missing.";
                return false;
            }
            if (data.metadata.saveVersion > SaveManager.CurrentVersion)
            {
                error = $"Save version ({data.metadata.saveVersion}) is higher than supported ({SaveManager.CurrentVersion}).";
                return false;
            }
            if (!data.campaign.hasConfirmedParty || data.campaign.party == null || data.campaign.party.Length != CharacterRoster.PartySize)
            {
                error = $"Save requires exactly {CharacterRoster.PartySize} confirmed party members.";
                return false;
            }
            foreach (var hero in data.campaign.party)
            {
                if (hero == null || string.IsNullOrEmpty(hero.id))
                {
                    error = "Invalid character data in save file.";
                    return false;
                }
                if (!string.IsNullOrEmpty(hero.raceId) && !Database.Races.ContainsKey(hero.raceId))
                {
                    error = $"Unknown race reference '{hero.raceId}'.";
                    return false;
                }
                if (!string.IsNullOrEmpty(hero.classId) && !Database.Classes.ContainsKey(hero.classId))
                {
                    error = $"Unknown class reference '{hero.classId}'.";
                    return false;
                }
            }
            return true;
        }
    }

    public static class PersistenceCommitHandler
    {
        static CampaignSaveData activeCommittedSnapshot;

        public static CampaignSaveData ActiveCommittedSnapshot => activeCommittedSnapshot;

        public static void SetActiveCommittedSnapshot(CampaignSaveData snapshot)
        {
            activeCommittedSnapshot = snapshot;
        }

        public static CampaignSaveData CreateCommittedSnapshot(RunState run, IReadOnlyList<CharacterDef> partyDefs = null)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var defs = partyDefs ?? run.Party.Select(u => new CharacterDef
            {
                id = u.id,
                name = u.name,
                role = u.role,
                raceId = u.race?.id ?? "human",
                classId = u.classDef?.id ?? "fighter",
                backgroundId = u.background?.id ?? "guard",
                baseScores = u.BaseScores,
                stats = u.stats,
                starterPassiveId = u.starterPassiveId,
                activeSkillLoadout = u.skillLoadout.ActiveSlots.ToArray(),
                passiveSkillLoadout = u.skillLoadout.PassiveSlots.ToArray(),
                unlockedSkills = u.skillLoadout.UnlockedActiveSkills.Concat(u.skillLoadout.UnlockedPassiveSkills).ToArray(),
                talentIds = u.selectedTalentIds.ToArray(),
                equipmentLoadout = u.equipped.Where(kv => kv.Value != null).Select(kv => new EquipmentSlotAssignment(kv.Key, kv.Value.id)).ToArray(),
            }).ToList();

            var invData = run.Inventory != null ? run.Inventory.ToSaveData() : new InventorySaveData();

            int nextAct = activeCommittedSnapshot != null
                ? activeCommittedSnapshot.nextDungeonAct
                : run.Act;

            int comAct = activeCommittedSnapshot != null
                ? activeCommittedSnapshot.committedAct
                : run.Act;

            return new CampaignSaveData
            {
                hasConfirmedParty = true,
                seed = run.Seed,
                committedAct = comAct,
                nextDungeonAct = nextAct,
                hubGold = run.Gold,
                playtimeSeconds = SaveManager.TotalPlaytimeSeconds,
                party = defs.Select((d, i) => SavedCharacterData.FromCharacterDef(d, i < run.Party.Count ? run.Party[i] : null)).ToArray(),
                inventoryEquipment = invData.equipment ?? Array.Empty<EquipmentInstanceSaveData>(),
                inventoryConsumables = invData.consumables ?? Array.Empty<ConsumableStackSaveData>(),
                inventoryMaterials = invData.materials ?? Array.Empty<MaterialEntrySaveData>(),
                inventoryKeyItems = invData.keyItems ?? Array.Empty<string>(),
                relics = run.Relics.ToArray(),
                relicInstances = run.RelicManager != null ? run.RelicManager.ToSaveData() : Array.Empty<RelicInstanceSaveData>(),
                hubArmoryStock = run.CurrentShop != null && run.CurrentShop.items != null
                    ? run.CurrentShop.items.Select(i => i.ToSaveData()).ToArray()
                    : Array.Empty<ShopItemSaveData>()
            };
        }

        public const int StarterGold = 600;

        public static CampaignSaveData CreateNewGameCommittedSnapshot(IReadOnlyList<CharacterDef> confirmedParty, int seed = 1, int starterGold = StarterGold)
        {
            if (confirmedParty == null || confirmedParty.Count != CharacterRoster.PartySize)
                throw new InvalidOperationException($"Cannot create snapshot with incomplete party (requires {CharacterRoster.PartySize}).");

            return new CampaignSaveData
            {
                hasConfirmedParty = true,
                seed = seed,
                committedAct = 1,
                nextDungeonAct = 1,
                hubGold = starterGold,
                playtimeSeconds = 0f,
                party = confirmedParty.Select(c => SavedCharacterData.FromCharacterDef(c)).ToArray(),
                inventoryEquipment = Array.Empty<EquipmentInstanceSaveData>(),
                inventoryConsumables = Database.StartingItems.Select(id => new ConsumableStackSaveData { itemId = id, count = 1, maxStack = 99 }).ToArray(),
                inventoryMaterials = Array.Empty<MaterialEntrySaveData>(),
                inventoryKeyItems = Array.Empty<string>(),
                relics = Array.Empty<string>(),
                relicInstances = Array.Empty<RelicInstanceSaveData>(),
                hubArmoryStock = Array.Empty<ShopItemSaveData>()
            };
        }

        // Section 3.1: Commit acquired progression upon successful Dungeon Clear
        public static void CommitDungeonClear(RunState run)
        {
            if (run == null) return;
            // 1. Convert Run Gold to Hub Gold 1:1
            run.TransferRunGoldToHub();

            // 2. Build new committed baseline from current run state
            activeCommittedSnapshot = CreateCommittedSnapshot(run);
            activeCommittedSnapshot.committedAct = run.Act;
            activeCommittedSnapshot.nextDungeonAct = Math.Min(8, run.Act + 1);
        }

        // Section 3.2 & 5.4 / 5.5: Discards uncommitted current-dungeon progress and restores committed baseline
        public static void RollbackUncommitted(RunState run)
        {
            if (run == null) return;
            if (activeCommittedSnapshot == null)
            {
                run.Gold = 0;
                return;
            }

            // Restore from committed snapshot
            run.RestoreFromCampaignSave(activeCommittedSnapshot);
        }
    }

    public static class SaveManager
    {
        public const int CurrentVersion = 1;

        static ISaveStorage storage;
        public static ISaveStorage Storage
        {
            get
            {
                if (storage == null) storage = new FileSaveStorage();
                return storage;
            }
            set => storage = value;
        }

        public static float TotalPlaytimeSeconds { get; set; } = 0f;

        public static void RecordPlaytime(float delta)
        {
            if (delta > 0) TotalPlaytimeSeconds += delta;
        }

        public static bool CanSaveCurrentState()
        {
            return Game.I != null && Game.I.State == GameState.Hub;
        }

        public static bool CanLoadCurrentState()
        {
            return Game.I == null || Game.I.State == GameState.MainMenu || Game.I.State == GameState.Hub;
        }

        public static SaveSlotMetadata GetSlotMetadata(SaveSlotId slot)
        {
            if (!Storage.SlotExists(slot))
            {
                return new SaveSlotMetadata
                {
                    slotId = slot,
                    slotType = slot == SaveSlotId.AutoSave ? SaveSlotType.AutoSave : SaveSlotType.Manual,
                    isOccupied = false
                };
            }

            string raw = Storage.ReadSlotRaw(slot);
            if (string.IsNullOrEmpty(raw))
            {
                return new SaveSlotMetadata
                {
                    slotId = slot,
                    slotType = slot == SaveSlotId.AutoSave ? SaveSlotType.AutoSave : SaveSlotType.Manual,
                    isOccupied = false
                };
            }

            try
            {
                var file = JsonUtility.FromJson<SaveFileData>(raw);
                if (file != null && file.metadata != null)
                {
                    file.metadata.isOccupied = true;
                    return file.metadata;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Corrupted slot {slot}: {ex.Message}");
            }

            return new SaveSlotMetadata
            {
                slotId = slot,
                slotType = slot == SaveSlotId.AutoSave ? SaveSlotType.AutoSave : SaveSlotType.Manual,
                isOccupied = true,
                partySummary = "[CORRUPTED SAVE]"
            };
        }

        public static List<SaveSlotMetadata> GetAllSlotMetadata()
        {
            var list = new List<SaveSlotMetadata>();
            foreach (SaveSlotId slot in Enum.GetValues(typeof(SaveSlotId)))
                list.Add(GetSlotMetadata(slot));
            return list;
        }

        public static bool HasAnySave(out SaveSlotId bestSlot)
        {
            bestSlot = SaveSlotId.AutoSave;
            long latestTicks = -1;
            bool found = false;

            foreach (SaveSlotId slot in Enum.GetValues(typeof(SaveSlotId)))
            {
                var meta = GetSlotMetadata(slot);
                if (meta.isOccupied && meta.saveVersion <= CurrentVersion && meta.partySummary != "[CORRUPTED SAVE]")
                {
                    if (meta.lastSavedTicks > latestTicks)
                    {
                        latestTicks = meta.lastSavedTicks;
                        bestSlot = slot;
                        found = true;
                    }
                }
            }
            return found;
        }

        public static bool HasAutoSave()
        {
            var meta = GetSlotMetadata(SaveSlotId.AutoSave);
            return meta.isOccupied && meta.saveVersion <= CurrentVersion;
        }

        public static bool Save(SaveSlotId slot, out string error)
        {
            error = null;
            if (slot == SaveSlotId.AutoSave)
            {
                error = "Manual save cannot target the dedicated Auto Save slot.";
                return false;
            }
            if (!CanSaveCurrentState())
            {
                error = "Campaign can only be saved while in the Sanctuary Hub.";
                return false;
            }

            return WriteSlotInternal(slot, SaveSlotType.Manual, out error);
        }

        public static bool AutoSave(out string error)
        {
            error = null;
            if (!CanSaveCurrentState())
            {
                error = "Auto Save is restricted strictly to the Sanctuary Hub.";
                return false;
            }

            return WriteSlotInternal(SaveSlotId.AutoSave, SaveSlotType.AutoSave, out error);
        }

        static bool WriteSlotInternal(SaveSlotId slot, SaveSlotType slotType, out string error)
        {
            error = null;
            var run = Game.I?.Run;
            if (run == null && PersistenceCommitHandler.ActiveCommittedSnapshot == null)
            {
                error = "No active campaign state to save.";
                return false;
            }

            CampaignSaveData campaignData = run != null
                ? PersistenceCommitHandler.CreateCommittedSnapshot(run)
                : PersistenceCommitHandler.ActiveCommittedSnapshot;

            PersistenceCommitHandler.SetActiveCommittedSnapshot(campaignData);

            int avgLevel = campaignData.party != null && campaignData.party.Length > 0
                ? (int)Math.Round(campaignData.party.Average(p => p.level))
                : 1;

            string partyDesc = campaignData.party != null
                ? string.Join(", ", campaignData.party.Select(p => p.name))
                : "4 Heroes";

            var now = DateTime.UtcNow;
            var meta = new SaveSlotMetadata
            {
                saveVersion = CurrentVersion,
                slotId = slot,
                slotType = slotType,
                createdAtIso = now.ToString("o"),
                lastSavedAtIso = now.ToString("yyyy-MM-dd HH:mm"),
                lastSavedTicks = now.Ticks,
                committedAct = campaignData.committedAct,
                committedDungeonName = SaveSlotMetadata.GetDungeonName(campaignData.committedAct),
                partySummary = partyDesc,
                partyAverageLevel = avgLevel,
                hubGold = campaignData.hubGold,
                playtimeSeconds = TotalPlaytimeSeconds,
                isOccupied = true
            };

            var saveFile = new SaveFileData
            {
                metadata = meta,
                campaign = campaignData
            };

            if (!SaveValidator.ValidateSave(saveFile, out error))
                return false;

            string json = JsonUtility.ToJson(saveFile, true);
            return Storage.WriteSlotRaw(slot, json, out error);
        }

        public static bool Load(SaveSlotId slot, out string error)
        {
            error = null;
            if (!CanLoadCurrentState())
            {
                error = "Loading is only permitted from the Main Menu or Sanctuary Hub.";
                return false;
            }

            if (!Storage.SlotExists(slot))
            {
                error = $"Slot {SaveSlotMetadata.FormatSlotName(slot)} is empty.";
                return false;
            }

            string raw = Storage.ReadSlotRaw(slot);
            if (string.IsNullOrEmpty(raw))
            {
                error = $"Failed to read data from {SaveSlotMetadata.FormatSlotName(slot)}.";
                return false;
            }

            SaveFileData saveFile;
            try
            {
                saveFile = JsonUtility.FromJson<SaveFileData>(raw);
            }
            catch (Exception ex)
            {
                error = $"Save data parsing error: {ex.Message}";
                return false;
            }

            if (!SaveValidator.ValidateSave(saveFile, out error))
                return false;

            // Restore campaign state
            var camp = saveFile.campaign;
            TotalPlaytimeSeconds = camp.playtimeSeconds;
            PersistenceCommitHandler.SetActiveCommittedSnapshot(camp);

            var partyDefs = camp.party.Select(p => p.ToCharacterDef()).ToList();
            var roster = new CharacterRoster(startWithEmptyParty: true);
            roster.CommitConfirmedParty(partyDefs);

            var run = new RunState(MapGenerator.Generate(camp.seed, camp.committedAct), partyDefs, camp.seed);
            run.RestoreFromCampaignSave(camp);

            if (Game.I != null)
            {
                Game.I.ApplyLoadedCampaign(roster, run);
                Game.I.Go(GameState.Hub);
            }

            return true;
        }

        public static bool Delete(SaveSlotId slot, out string error)
        {
            return Storage.DeleteSlot(slot, out error);
        }

        public static bool TryContinue(out string error)
        {
            if (HasAnySave(out var bestSlot))
                return Load(bestSlot, out error);

            error = "No valid saved campaign found.";
            return false;
        }
    }
}

