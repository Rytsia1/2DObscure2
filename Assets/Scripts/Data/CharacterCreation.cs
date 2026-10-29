using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Signature Racial Feat (Section 4 of Character Creation System).
    // Supports Halfling Second Chance, Dragonborn Draconic Overdrive, Merfolk Ocean's Embrace, Liichtian Lightbound, etc.
    [Serializable]
    public class RacialFeatDef
    {
        public string id, name, description;
        public bool canRetryFailedQte;          // Halfling — Second Chance
        public int maxQteRetriesPerBattle = 1;
        public float overdrivePowerBonus;       // Dragonborn — Draconic Overdrive
        public int overdriveDurationTurns = 2;
        public int exhaustDurationTurns = 1;
        public int allyShieldAmount;            // Merfolk — Ocean's Embrace
    }

    // Racial Utility ability usable outside or during combat (Section 4).
    [Serializable]
    public class RacialUtilityDef
    {
        public string id, name, description;
        public string utilityTag;
    }

    // Race definition: Race ID, Name, Description, Racial Ability Bonuses, Racial Feat, Racial Utility, and Restrictions.
    // Each race has three conceptual components: Racial Ability Bonus, Racial Feat, and Racial Utility.
    [Serializable]
    public class RaceDef
    {
        public string id, name, description;
        public bool isPlayable = true;
        public AbilityScores abilityModifiers;
        public RacialFeatDef racialFeat;
        public RacialUtilityDef racialUtility;
        public EquipmentRestriction restrictions;

        // Legacy fields preserved for backward compatibility with existing tests
        public StatModifier[] passiveBonuses = Array.Empty<StatModifier>();
        public string[] traits = Array.Empty<string>();
        public string[] racialAbilities = Array.Empty<string>();
    }

    // Hybrid skill unlock progression entry: character level unlocks tiers; Skill Points unlock individual skills.
    [Serializable]
    public class SkillUnlockNode
    {
        public string skillId;
        public bool isPassive;
        public int tier = 1;
        public int requiredLevel = 1;
        public int skillPointCost = 1;
        public int maxRank = 3;
    }

    // 3 Active Skill Slots + 3 Passive Skill Slots + Hybrid Unlock & Rank progression (Section 7).
    [Serializable]
    public class SkillLoadout
    {
        public const int ActiveSlotCount = 3;
        public const int PassiveSlotCount = 3;

        public readonly string[] ActiveSlots = new string[ActiveSlotCount];
        public readonly string[] PassiveSlots = new string[PassiveSlotCount];
        public string[] activeSlots => ActiveSlots;
        public string[] passiveSlots => PassiveSlots;
        public readonly HashSet<string> UnlockedActiveSkills = new HashSet<string>();
        public readonly HashSet<string> UnlockedPassiveSkills = new HashSet<string>();
        public readonly Dictionary<string, int> SkillRanks = new Dictionary<string, int>();
        public int SkillPoints { get; set; }

        public void InitializeForClass(ClassDef classDef, int level = 1)
        {
            Array.Clear(ActiveSlots, 0, ActiveSlotCount);
            Array.Clear(PassiveSlots, 0, PassiveSlotCount);
            UnlockedActiveSkills.Clear();
            UnlockedPassiveSkills.Clear();
            SkillRanks.Clear();

            if (classDef == null) return;

            // Starter Passive is always unlocked and equipped in Passive Slot 0 at character creation
            if (!string.IsNullOrEmpty(classDef.starterPassiveId) && Database.Passives.ContainsKey(classDef.starterPassiveId))
            {
                UnlockedPassiveSkills.Add(classDef.starterPassiveId);
                SkillRanks[classDef.starterPassiveId] = 1;
                PassiveSlots[0] = classDef.starterPassiveId;
            }

            // Initialize starting Active Skills into the 3 Active Skill Slots
            if (classDef.startingSkills != null)
            {
                int slot = 0;
                foreach (var sid in classDef.startingSkills)
                {
                    if (string.IsNullOrEmpty(sid) || !Database.Skills.ContainsKey(sid)) continue;
                    UnlockedActiveSkills.Add(sid);
                    SkillRanks[sid] = 1;
                    if (slot < ActiveSlotCount)
                        ActiveSlots[slot++] = sid;
                }
            }
        }

        public static int GetMaxUnlockedTier(int characterLevel) =>
            characterLevel >= 5 ? 3 : characterLevel >= 3 ? 2 : 1;

        public void UnlockActive(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            UnlockedActiveSkills.Add(skillId);
            if (!SkillRanks.ContainsKey(skillId)) SkillRanks[skillId] = 1;
        }

        public void UnlockPassive(string passiveId)
        {
            if (string.IsNullOrEmpty(passiveId)) return;
            UnlockedPassiveSkills.Add(passiveId);
            if (!SkillRanks.ContainsKey(passiveId)) SkillRanks[passiveId] = 1;
        }

        public void SyncLevelUnlocks(ClassDef classDef, int level)
        {
            SkillPoints++;
        }

        public bool TryUnlockSkill(string id, int characterLevel, ClassDef classDef, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(id)) { error = "Skill ID is empty."; return false; }
            if (UnlockedActiveSkills.Contains(id) || UnlockedPassiveSkills.Contains(id))
            {
                error = "Skill is already unlocked.";
                return false;
            }

            var node = classDef?.skillUnlockProgression?.FirstOrDefault(n => n.skillId == id);
            int reqLevel = node?.requiredLevel ?? 1;
            int reqTier = node?.tier ?? 1;
            int cost = node?.skillPointCost ?? 1;
            bool isPassive = node?.isPassive ?? Database.Passives.ContainsKey(id);

            if (characterLevel < reqLevel || GetMaxUnlockedTier(characterLevel) < reqTier)
            {
                error = $"Requires character level {reqLevel} (Tier {reqTier}).";
                return false;
            }
            if (SkillPoints < cost)
            {
                error = $"Requires {cost} Skill Points (available: {SkillPoints}).";
                return false;
            }

            SkillPoints -= cost;
            if (isPassive) UnlockedPassiveSkills.Add(id);
            else UnlockedActiveSkills.Add(id);
            SkillRanks[id] = 1;
            return true;
        }

        public bool TryEquipActive(int slotIndex, string skillId, out string error)
        {
            error = null;
            if (slotIndex < 0 || slotIndex >= ActiveSlotCount)
            {
                error = $"Active slot index ({slotIndex}) must be 0..{ActiveSlotCount - 1}.";
                return false;
            }
            if (string.IsNullOrEmpty(skillId))
            {
                ActiveSlots[slotIndex] = null;
                return true;
            }
            if (!Database.Skills.ContainsKey(skillId))
            {
                error = $"'{skillId}' is not a valid Active Skill.";
                return false;
            }
            if (!UnlockedActiveSkills.Contains(skillId))
            {
                error = $"Active Skill '{skillId}' is not unlocked.";
                return false;
            }
            for (int i = 0; i < ActiveSlotCount; i++)
                if (i != slotIndex && ActiveSlots[i] == skillId) ActiveSlots[i] = null;
            ActiveSlots[slotIndex] = skillId;
            return true;
        }

        public bool TryEquipPassive(int slotIndex, string passiveId, out string error)
        {
            error = null;
            if (slotIndex < 0 || slotIndex >= PassiveSlotCount)
            {
                error = $"Passive slot index ({slotIndex}) must be 0..{PassiveSlotCount - 1}.";
                return false;
            }
            if (string.IsNullOrEmpty(passiveId))
            {
                PassiveSlots[slotIndex] = null;
                return true;
            }
            if (!Database.Passives.ContainsKey(passiveId))
            {
                error = $"'{passiveId}' is not a valid Passive Skill.";
                return false;
            }
            if (!UnlockedPassiveSkills.Contains(passiveId))
            {
                error = $"Passive Skill '{passiveId}' is not unlocked.";
                return false;
            }
            for (int i = 0; i < PassiveSlotCount; i++)
                if (i != slotIndex && PassiveSlots[i] == passiveId) PassiveSlots[i] = null;
            PassiveSlots[slotIndex] = passiveId;
            return true;
        }

        public bool TryUpgradeRank(string id, out string error)
        {
            error = null;
            if (!SkillRanks.TryGetValue(id, out int curRank))
            {
                error = $"Skill '{id}' is not unlocked.";
                return false;
            }
            int maxRank = Database.Skills.TryGetValue(id, out var s) ? s.maxRank
                : Database.Passives.TryGetValue(id, out var p) ? p.maxRank : 3;
            if (curRank >= maxRank)
            {
                error = $"Skill '{id}' is already at max rank ({maxRank}).";
                return false;
            }
            if (SkillPoints < 1)
            {
                error = "Not enough Skill Points to upgrade rank.";
                return false;
            }
            SkillPoints--;
            SkillRanks[id] = curRank + 1;
            return true;
        }

        public int GetRank(string id) => SkillRanks.TryGetValue(id, out int r) ? r : 0;
        public string[] GetEquippedActives() => ActiveSlots.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        public string[] GetEquippedPassives() => PassiveSlots.Where(p => !string.IsNullOrEmpty(p)).ToArray();
    }

    // Class definition: 9 playable classes with Gameplay Identity, Starter Passive, Active/Passive Skills,
    // Strict Weapon Compatibility (Main Hand & Second Hand), and 8-slot Starting Equipment Configuration.
    [Serializable]
    public class ClassDef
    {
        public string id, name, role, description;
        public bool isPlayable = true;
        public string gameplayIdentity;
        public string starterPassiveId;
        public string[] startingSkills = Array.Empty<string>();
        public string[] activeSkillPool = Array.Empty<string>();
        public string[] passiveSkillPool = Array.Empty<string>();
        public SkillUnlockNode[] skillUnlockProgression = Array.Empty<SkillUnlockNode>();

        // Strict Weapon Compatibility lists (Section 8)
        public WeaponType[] mainHandWeapons = Array.Empty<WeaponType>();
        public WeaponType[] secondHandWeapons = Array.Empty<WeaponType>();
        public EquipmentSlotAssignment[] startingEquipment = Array.Empty<EquipmentSlotAssignment>();

        public string[] weaponProficiencies = Array.Empty<string>();
        public string[] armorProficiencies = Array.Empty<string>();
        public string[] classMechanics = Array.Empty<string>();
        public Affinities affinities = Affinities.None;
        public string[] sprite;

        // Resource scaling
        public int baseHp = 80;
        public float hpGrowth = 10f;       // multiplied by VITModifier
        public int levelHpGrowth = 8;      // per level above 1
        public int baseMp = 30;
        public float mpGrowth = 5f;        // multiplied by WISModifier
        public int levelMpGrowth = 4;      // per level above 1
        public int baseShield = 0;
        public float shieldGrowth = 2f;    // multiplied by max(0, VITModifier)

        // Offensive scaling
        public int baseWeaponPower = 10;
        public float weaponScaling = 2f;   // multiplied by STRModifier
        public int baseRangedPower = 10;
        public float rangedScaling = 2f;   // multiplied by DEXModifier
        public int baseSpellPower = 8;
        public float spellScaling = 2f;    // multiplied by INTModifier
        public int baseHealing = 8;
        public float healingScaling = 2f;  // multiplied by WISModifier
        public int baseSupportPower = 10;
        public float supportScaling = 2f;  // multiplied by CHAModifier
        public int baseCritChance = 5;
        public float critChanceScaling = 2f;
        public int baseCritDamage = 150;
        public int baseArmorPen = 0;
        public float armorPenScaling = 2f;
        public int baseMagicPen = 0;
        public float magicPenScaling = 2f;

        // Defensive scaling
        public int basePhysicalDefense = 5;
        public float physicalDefenseScaling = 2f; // multiplied by VITModifier
        public int baseMagicDefense = 5;
        public float magicDefenseScaling = 2f;    // multiplied by WISModifier
        public int baseEvasion = 5;
        public float evasionScaling = 2f;         // multiplied by DEXModifier
        public int baseBlockPower = 50;
        public float blockScaling = 3f;           // multiplied by STRModifier
        public int baseStatusResistance = 5;
        public float statusResistanceScaling = 3f; // multiplied by (VITModifier + WISModifier)
        public int baseStaggerResistance = 10;
        public float staggerResistanceScaling = 4f; // multiplied by VITModifier

        // Turn scaling
        public int baseInitiative = 8;            // + DEXModifier + bonuses
        public int baseActionSpeed = 100;
        public float actionSpeedScaling = 5f;

        // QTE scaling
        public int baseQteTimingBonus = 0;
        public float qteTimingScaling = 3f;       // +3% window per DEXModifier
        public int baseQteStability = 0;
        public float qteStabilityScaling = 4f;    // +4% stability per positive VITModifier
        public int baseCounterPower = 10;
        public float counterPowerScaling = 2f;    // multiplied by (STRModifier + DEXModifier)

        // Break scaling
        public int baseBreakPower = 12;
        public float breakPowerScaling = 3f;      // Physical -> STRModifier, Magic -> INTModifier

        public bool IsWeaponCompatible(EquipmentSlot slot, WeaponType weaponType)
        {
            if (weaponType == WeaponType.None) return false;
            if (slot == EquipmentSlot.MainHand)
                return mainHandWeapons != null && mainHandWeapons.Contains(weaponType);
            if (slot == EquipmentSlot.SecondHand)
                return secondHandWeapons != null && secondHandWeapons.Contains(weaponType);
            return false;
        }
    }

    // Background definition: narrative identity, proficiencies, passive effects, roguelike event interactions, and social bonuses.
    [Serializable]
    public class BackgroundDef
    {
        public string id, name, narrativeIdentity, description;
        public string[] proficiencies = Array.Empty<string>();
        public StatModifier[] passiveEffects = Array.Empty<StatModifier>();
        public string[] eventInteractions = Array.Empty<string>();
        public int socialBonus;
    }

    // Step-by-step navigation states for Character Creation (Section 2).
    // Players can advance or return to previous steps at any point before confirming the character.
    public enum CreationStep
    {
        SelectRace = 1,
        SelectClass = 2,
        AllocateAbilityScores = 3,
        ReviewStats = 4,
        ReviewStarterPassive = 5,
        ConfigureEquipment = 6,
        ConfirmCharacter = 7
    }

    // Interactive session implementing the full Character Creation flow (Sections 2-8):
    // 1. Create New Character -> 2. Select Race -> 3. Select Class -> 4. Allocate Ability Scores
    // -> 5. Review Character Stats -> 6. Review Starter Passive -> 7. Configure Equipment
    // -> 8. Confirm Character -> 9. Add Character to Party -> 10. Repeat to 4 -> 11. Begin Expedition.
    public class CharacterCreationSession
    {
        public string CharacterName { get; set; }
        public CreationStep CurrentStep { get; private set; } = CreationStep.SelectRace;
        public RaceDef Race { get; private set; }
        public ClassDef Class { get; private set; }
        public BackgroundDef Background { get; private set; }
        public PointBuyAllocation PointBuy { get; }
        public SkillLoadout Loadout { get; } = new SkillLoadout();
        public readonly Dictionary<EquipmentSlot, EquipmentDef> Equipped = new Dictionary<EquipmentSlot, EquipmentDef>();
        public string LastFeedbackMessage { get; private set; } = "";
        public bool IsConfirmed { get; private set; }

        public event Action Changed;

        public CharacterCreationSession(PointBuyConfig config = null)
        {
            PointBuy = new PointBuyAllocation(config);
            PointBuy.Changed += () => Changed?.Invoke();
            CreateNewCharacter();
        }

        // Starts a fresh character creation build at Step 1 (SelectRace).
        public void CreateNewCharacter()
        {
            IsConfirmed = false;
            CurrentStep = CreationStep.SelectRace;
            PointBuy.Reset();
            Race = Database.Races.TryGetValue("human", out var human) ? human : Database.Races.Values.First();
            var defaultClass = Database.Classes.TryGetValue("fighter", out var fighter) ? fighter : Database.Classes.Values.First();
            Background = Database.Backgrounds.Values.First();
            CharacterName = defaultClass.name;
            ApplyClassSelection(defaultClass, resetEquipmentAndLoadout: true);
            LastFeedbackMessage = "NEW CHARACTER STARTED.";
            Changed?.Invoke();
        }

        public bool NextStep()
        {
            if (CurrentStep >= CreationStep.ConfirmCharacter) return false;
            CurrentStep = (CreationStep)((int)CurrentStep + 1);
            Changed?.Invoke();
            return true;
        }

        public bool PreviousStep()
        {
            if (IsConfirmed)
            {
                LastFeedbackMessage = "CHARACTER ALREADY CONFIRMED.";
                return false;
            }
            if (CurrentStep <= CreationStep.SelectRace) return false;
            CurrentStep = (CreationStep)((int)CurrentStep - 1);
            Changed?.Invoke();
            return true;
        }

        public bool GoToStep(CreationStep step)
        {
            if (IsConfirmed)
            {
                LastFeedbackMessage = "CHARACTER ALREADY CONFIRMED.";
                return false;
            }
            CurrentStep = step;
            Changed?.Invoke();
            return true;
        }

        public void SelectRace(string raceId)
        {
            if (!Database.Races.TryGetValue(raceId, out var r))
                throw new ArgumentException($"Unknown race ID '{raceId}'.", nameof(raceId));
            SelectRace(r);
        }

        public void SelectRace(RaceDef race)
        {
            Race = race ?? throw new ArgumentNullException(nameof(race));
            RemoveInvalidEquippedItems();
            LastFeedbackMessage = $"SELECTED RACE: {Race.name.ToUpperInvariant()}.";
            Changed?.Invoke();
        }

        // Selecting a Class initializes its Starter Passive, Skill Loadout, and Starting Equipment,
        // while NEVER overwriting Point Buy Ability Score allocations.
        public void SelectClass(string classId)
        {
            if (!Database.Classes.TryGetValue(classId, out var c))
                throw new ArgumentException($"Unknown class ID '{classId}'.", nameof(classId));
            SelectClass(c);
        }

        public void SelectClass(ClassDef classDef)
        {
            if (classDef == null) throw new ArgumentNullException(nameof(classDef));
            ApplyClassSelection(classDef, resetEquipmentAndLoadout: true);
            LastFeedbackMessage = $"SELECTED CLASS: {Class.name.ToUpperInvariant()} ({Class.gameplayIdentity ?? Class.role}).";
            Changed?.Invoke();
        }

        void ApplyClassSelection(ClassDef classDef, bool resetEquipmentAndLoadout)
        {
            bool defaultName = string.IsNullOrWhiteSpace(CharacterName) || (Class != null && CharacterName == Class.name);
            Class = classDef;
            if (defaultName) CharacterName = Class.name;

            if (resetEquipmentAndLoadout)
            {
                Loadout.InitializeForClass(Class);
                ResetToClassStartingEquipment();
            }
        }

        public void ResetToClassStartingEquipment()
        {
            Equipped.Clear();
            if (Class?.startingEquipment == null) return;
            foreach (var entry in Class.startingEquipment)
            {
                if (string.IsNullOrEmpty(entry.equipmentId)) continue;
                if (Database.Equipment.TryGetValue(entry.equipmentId, out var item))
                {
                    if (EquipmentValidator.Validate(item, entry.slot, Class, Race, 1, FinalScores) == null)
                        Equipped[entry.slot] = item;
                }
            }
        }

        void RemoveInvalidEquippedItems()
        {
            foreach (var slot in Equipped.Keys.ToList())
            {
                var item = Equipped[slot];
                if (EquipmentValidator.Validate(item, slot, Class, Race, 1, FinalScores) != null)
                    Equipped.Remove(slot);
            }
        }

        // Selecting a Background does NOT alter Point Buy costs or scores.
        public void SelectBackground(string backgroundId) => SelectBackground(Database.Backgrounds[backgroundId]);
        public void SelectBackground(BackgroundDef bg)
        {
            Background = bg ?? throw new ArgumentNullException(nameof(bg));
            Changed?.Invoke();
        }

        public PassiveDef StarterPassive =>
            Class?.starterPassiveId != null && Database.Passives.TryGetValue(Class.starterPassiveId, out var p) ? p : null;

        public bool IncreaseStat(Ability a)
        {
            bool ok = PointBuy.TryIncrease(a);
            if (!ok) LastFeedbackMessage = $"CANNOT INCREASE {a} (INSUFFICIENT POINTS OR AT CAP 15).";
            else LastFeedbackMessage = $"{a} INCREASED TO {BaseScores[a]}.";
            return ok;
        }

        public bool DecreaseStat(Ability a)
        {
            bool ok = PointBuy.TryDecrease(a);
            if (!ok) LastFeedbackMessage = $"CANNOT DECREASE {a} BELOW {PointBuy.Config.minScore}.";
            else
            {
                RemoveInvalidEquippedItems();
                LastFeedbackMessage = $"{a} DECREASED TO {BaseScores[a]}.";
            }
            return ok;
        }

        public void ResetPointBuy()
        {
            PointBuy.Reset();
            RemoveInvalidEquippedItems();
            LastFeedbackMessage = "ABILITY SCORES RESET TO 8.";
        }

        // Attempts to equip an item in one of the 8 Equipment Slots during character creation.
        // Enforces Strict Weapon Compatibility and item restrictions.
        public bool TryEquip(EquipmentSlot slot, string equipmentId, out string error)
        {
            if (string.IsNullOrEmpty(equipmentId))
            {
                Equipped.Remove(slot);
                error = null;
                LastFeedbackMessage = $"CLEARED {EquipmentSlots.DisplayName(slot).ToUpperInvariant()}.";
                Changed?.Invoke();
                return true;
            }
            if (!Database.Equipment.TryGetValue(equipmentId, out var item))
            {
                error = $"Unknown equipment ID '{equipmentId}'.";
                LastFeedbackMessage = error.ToUpperInvariant();
                return false;
            }
            return TryEquip(slot, item, out error);
        }

        public bool TryEquip(EquipmentSlot slot, EquipmentDef item, out string error)
        {
            error = EquipmentValidator.Validate(item, slot, Class, Race, 1, FinalScores);
            if (error != null)
            {
                LastFeedbackMessage = error.ToUpperInvariant();
                return false;
            }
            Equipped[slot] = item;
            LastFeedbackMessage = $"EQUIPPED {item.name.ToUpperInvariant()} IN {EquipmentSlots.DisplayName(slot).ToUpperInvariant()}.";
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            bool removed = Equipped.Remove(slot);
            if (removed) Changed?.Invoke();
            return removed;
        }

        public EquipmentDef GetEquipped(EquipmentSlot slot) =>
            Equipped.TryGetValue(slot, out var eq) ? eq : null;

        public IEnumerable<EquipmentDef> GetCompatibleEquipmentForSlot(EquipmentSlot slot) =>
            Database.Equipment.Values.Where(item =>
                EquipmentValidator.Validate(item, slot, Class, Race, 1, FinalScores) == null);

        public AbilityScores BaseScores => PointBuy.Scores;
        public AbilityScores RacialModifiers => Race?.abilityModifiers ?? AbilityScores.Zero;
        public AbilityScores EquipmentAbilityBonuses
        {
            get
            {
                var bonus = AbilityScores.Zero;
                foreach (var eq in Equipped.Values)
                    if (eq != null) bonus = bonus.Plus(eq.abilityBonuses);
                foreach (var pid in Loadout.GetEquippedPassives())
                    if (Database.Passives.TryGetValue(pid, out var p)) bonus = bonus.Plus(p.abilityBonuses);
                return bonus;
            }
        }
        public AbilityScores FinalScores => BaseScores.Plus(RacialModifiers).Plus(EquipmentAbilityBonuses);
        public AbilityScores AbilityModifiers => FinalScores.Modifiers;

        public AbilityScoreSheet BuildAbilitySheet() => new AbilityScoreSheet
        {
            baseScores = BaseScores,
            racialModifiers = RacialModifiers,
            equipmentBonuses = EquipmentAbilityBonuses
        };

        public List<PassiveDef> CollectEquippedPassives()
        {
            var list = new List<PassiveDef>();
            foreach (var pid in Loadout.GetEquippedPassives())
                if (Database.Passives.TryGetValue(pid, out var p)) list.Add(p);
            return list;
        }

        public List<StatModifier> CollectCreationModifiers()
        {
            var mods = new List<StatModifier>();
            if (Race?.passiveBonuses != null) mods.AddRange(Race.passiveBonuses);
            if (Background?.passiveEffects != null) mods.AddRange(Background.passiveEffects);
            foreach (var p in CollectEquippedPassives())
                if (p.modifiers != null) mods.AddRange(p.modifiers);
            return mods;
        }

        // Real-time preview of Derived Stats based on current Race, Class, Background, Point Buy, Starter Passive, and Equipment.
        public DerivedStats PreviewDerivedStats =>
            StatCalculator.Calculate(BuildAbilitySheet(), Class, 1, Equipped.Values, CollectCreationModifiers(), default, CollectEquippedPassives());

        public string Validate()
        {
            if (Race == null) return "Race must be selected.";
            if (Class == null) return "Class must be selected.";
            if (Background == null) return "Background must be selected.";
            if (string.IsNullOrWhiteSpace(CharacterName)) return "Character name is required.";
            var pbErr = PointBuy.ValidationError;
            if (pbErr != null) return pbErr;

            if (!string.IsNullOrEmpty(Class.starterPassiveId) && !Loadout.UnlockedPassiveSkills.Contains(Class.starterPassiveId))
                return $"Starter passive '{Class.starterPassiveId}' must be unlocked.";

            foreach (var kv in Equipped)
            {
                string eqErr = EquipmentValidator.Validate(kv.Value, kv.Key, Class, Race, 1, FinalScores);
                if (eqErr != null) return eqErr;
            }
            return null;
        }

        public bool CanConfirm => Validate() == null;

        // Confirms character creation, producing a complete CharacterDef with Race, Class, Ability Scores,
        // Starter Passive, 3+3 Skill Loadout, and 8-slot Equipment Loadout.
        public CharacterDef Confirm(CharacterRoster roster = null, string customId = null)
        {
            var err = Validate();
            if (err != null)
            {
                LastFeedbackMessage = err.ToUpperInvariant();
                throw new InvalidOperationException(err);
            }

            var derived = PreviewDerivedStats;
            var activeSkills = Loadout.GetEquippedActives().ToList();
            if (activeSkills.Count == 0 && Class.startingSkills != null)
                activeSkills.AddRange(Class.startingSkills.Take(SkillLoadout.ActiveSlotCount));

            if (Race.racialAbilities != null)
                foreach (var ra in Race.racialAbilities)
                    if (!activeSkills.Contains(ra) && Database.Skills.ContainsKey(ra))
                        activeSkills.Add(ra);

            var passiveSkills = Loadout.GetEquippedPassives();
            var eqAssignments = Equipped
                .Where(kv => kv.Value != null)
                .Select(kv => new EquipmentSlotAssignment(kv.Key, kv.Value.id))
                .ToArray();

            string id = customId ?? $"{Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            var def = new CharacterDef
            {
                id = id,
                name = CharacterName.Trim(),
                role = Class.role,
                description = $"{Race.name} {Class.name} ({Class.gameplayIdentity ?? Background.name}). {Class.description}",
                raceId = Race.id,
                classId = Class.id,
                backgroundId = Background.id,
                baseScores = BaseScores,
                starterPassiveId = Class.starterPassiveId,
                activeSkillLoadout = Loadout.ActiveSlots.ToArray(),
                passiveSkillLoadout = Loadout.PassiveSlots.ToArray(),
                unlockedSkills = Loadout.UnlockedActiveSkills.Concat(Loadout.UnlockedPassiveSkills).ToArray(),
                passives = passiveSkills,
                startingEquipment = eqAssignments.Select(a => a.equipmentId).ToArray(),
                equipmentLoadout = eqAssignments,
                affinities = Class.affinities ?? Affinities.None,
                stats = derived.ToLegacyStats(),
                skills = activeSkills.ToArray(),
                sprite = Class.sprite ?? Art.Knight
            };

            IsConfirmed = true;
            CurrentStep = CreationStep.ConfirmCharacter;
            LastFeedbackMessage = $"CONFIRMED {def.name.ToUpperInvariant()} ({Race.name.ToUpperInvariant()} {Class.name.ToUpperInvariant()}).";
            roster?.AddCharacter(def);
            return def;
        }
    }

    // Manages the roster of created characters, the 4-character Party assembly, and Expedition initialization (Section 3).
    public class CharacterRoster
    {
        public const int PartySize = 4;
        public readonly List<CharacterDef> Characters = new List<CharacterDef>();
        public readonly List<string> SelectedPartyIds = new List<string>();
        public readonly List<CharacterDef> AssembledParty = new List<CharacterDef>();

        public CharacterRoster(bool startWithEmptyParty = false)
        {
            if (startWithEmptyParty)
                StartNewPartyAssembly();
            else
                ResetToDefault();
        }

        // Resets to the 4 default preset heroes (used when booting directly into Quick Start).
        public void ResetToDefault()
        {
            Characters.Clear();
            SelectedPartyIds.Clear();
            AssembledParty.Clear();
            foreach (var id in Database.Party)
            {
                var c = Database.Characters[id];
                Characters.Add(c);
                SelectedPartyIds.Add(id);
                AssembledParty.Add(c);
            }
        }

        // Clears the active party so the player must create and add 4 distinct characters before starting an expedition.
        public void StartNewPartyAssembly()
        {
            SelectedPartyIds.Clear();
            AssembledParty.Clear();
        }

        public void AddCharacter(CharacterDef def, bool selectInPartyIfRoom = false)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            int existing = Characters.FindIndex(c => c.id == def.id);
            if (existing >= 0) Characters[existing] = def;
            else Characters.Add(def);

            if (selectInPartyIfRoom && SelectedPartyIds.Count < PartySize && !SelectedPartyIds.Contains(def.id))
            {
                SelectedPartyIds.Add(def.id);
                AssembledParty.Add(def);
            }
        }

        // Adds a confirmed character to the expedition party.
        // Rejects nulls, duplicate character instances/IDs, and adding beyond 4 members.
        public bool TryAddToParty(CharacterDef def, out string error)
        {
            error = null;
            if (def == null)
            {
                error = "Cannot add a null character to the party.";
                return false;
            }
            if (AssembledParty.Contains(def) || SelectedPartyIds.Contains(def.id))
            {
                error = $"Character '{def.name}' ({def.id}) is already in the party.";
                return false;
            }
            if (SelectedPartyIds.Count >= PartySize || AssembledParty.Count >= PartySize)
            {
                error = $"Party is already full ({PartySize}/{PartySize} characters).";
                return false;
            }

            if (!Characters.Any(c => c.id == def.id))
                Characters.Add(def);

            SelectedPartyIds.Add(def.id);
            AssembledParty.Add(def);
            return true;
        }

        public bool RemoveFromParty(string characterId)
        {
            int idx = SelectedPartyIds.IndexOf(characterId);
            if (idx < 0) return false;
            SelectedPartyIds.RemoveAt(idx);
            if (idx < AssembledParty.Count) AssembledParty.RemoveAt(idx);
            return true;
        }

        // Replaces a party slot (0..3) with a character from the roster. Never allows duplicate instances in two slots.
        public void SetPartySlot(int slot, string characterId)
        {
            if (slot < 0 || slot >= PartySize) throw new ArgumentOutOfRangeException(nameof(slot));
            var def = Characters.FirstOrDefault(c => c.id == characterId)
                      ?? (Database.Characters.TryGetValue(characterId, out var dc) ? dc : null);
            if (def == null) throw new ArgumentException($"Character {characterId} is not in roster.");

            while (SelectedPartyIds.Count <= slot)
            {
                string fallbackId = Database.Party[SelectedPartyIds.Count];
                SelectedPartyIds.Add(fallbackId);
                AssembledParty.Add(Database.Characters[fallbackId]);
            }

            int otherIdx = SelectedPartyIds.IndexOf(characterId);
            if (otherIdx >= 0 && otherIdx != slot)
            {
                SelectedPartyIds[otherIdx] = SelectedPartyIds[slot];
                AssembledParty[otherIdx] = AssembledParty[slot];
            }
            SelectedPartyIds[slot] = characterId;
            AssembledParty[slot] = def;
        }

        public int PartyCount => SelectedPartyIds.Count;
        public bool IsPartyComplete =>
            SelectedPartyIds.Count == PartySize &&
            SelectedPartyIds.Distinct().Count() == PartySize;

        // Validates whether the party is ready to begin an expedition (must have 4 distinct, valid characters).
        public string ValidateExpeditionStart()
        {
            if (SelectedPartyIds.Count < PartySize)
                return $"Party is incomplete ({SelectedPartyIds.Count}/{PartySize} characters). Must have {PartySize} characters to begin expedition.";
            if (SelectedPartyIds.Count > PartySize)
                return $"Party exceeds maximum size ({SelectedPartyIds.Count}/{PartySize}).";
            if (SelectedPartyIds.Distinct().Count() != PartySize)
                return "Party contains duplicate character instances.";
            return null;
        }

        public bool CanBeginExpedition => ValidateExpeditionStart() == null;

        // Begins a new expedition (RunState) with the 4 assembled party members. Throws if party is incomplete.
        public RunState BeginExpedition(int seed = 1)
        {
            string err = ValidateExpeditionStart();
            if (err != null) throw new InvalidOperationException(err);
            var partyDefs = GetPartyForExpedition();
            return new RunState(MapGenerator.Generate(seed), partyDefs, seed);
        }

        public List<CharacterDef> GetPartyForExpedition()
        {
            string err = ValidateExpeditionStart();
            if (err != null) throw new InvalidOperationException(err);
            return SelectedPartyIds.Select(id => Characters.FirstOrDefault(c => c.id == id) ?? Database.Characters[id]).ToList();
        }

        public List<CharacterDef> GetActiveParty()
        {
            if (SelectedPartyIds.Count != PartySize)
                return Database.Party.Select(id => Database.Characters[id]).ToList();
            return SelectedPartyIds.Select(id => Characters.FirstOrDefault(c => c.id == id) ?? Database.Characters[id]).ToList();
        }
    }
}
