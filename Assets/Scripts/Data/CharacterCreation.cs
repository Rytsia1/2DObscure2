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
        public CreationMethod Method { get; private set; } = CreationMethod.Manual;
        public string ActiveTemplateId { get; private set; }
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
            Method = CreationMethod.Manual;
            ActiveTemplateId = null;
            CurrentStep = CreationStep.SelectRace;
            PointBuy.Reset();
            Race = Database.Races.TryGetValue("human", out var human) ? human : Database.Races.Values.First();
            var defaultClass = Database.Classes.TryGetValue("fighter", out var fighter) ? fighter : Database.Classes.Values.First();
            Background = Database.Backgrounds.Values.First();
            CharacterName = defaultClass.name;
            ApplyClassSelection(defaultClass, resetEquipmentAndLoadout: true);
            LastFeedbackMessage = "NEW CHARACTER STARTED (MANUAL).";
            Changed?.Invoke();
        }

        public void SetCreationMethod(CreationMethod method)
        {
            Method = method;
            if (method != CreationMethod.Template)
                ActiveTemplateId = null;
            LastFeedbackMessage = $"CREATION METHOD: {method.ToString().ToUpperInvariant()}.";
            Changed?.Invoke();
        }

        // Validates a predefined character template against all Race, Class, Point Buy, Skill, and Equipment rules (Section 4.2).
        public static string ValidateTemplate(CharacterTemplateDef template, PointBuyConfig config = null)
        {
            if (template == null) return "Template is null.";
            if (string.IsNullOrWhiteSpace(template.id)) return "Template ID is required.";
            if (string.IsNullOrWhiteSpace(template.name)) return $"Template '{template.id}' is missing a name.";
            if (string.IsNullOrEmpty(template.raceId) || !Database.Races.TryGetValue(template.raceId, out var race) || !race.isPlayable)
                return $"Template '{template.name}' specifies invalid or non-playable race '{template.raceId}'.";
            if (string.IsNullOrEmpty(template.classId) || !Database.Classes.TryGetValue(template.classId, out var classDef) || !classDef.isPlayable)
                return $"Template '{template.name}' specifies invalid or non-playable class '{template.classId}'.";

            var cfg = config ?? PointBuyConfig.Standard;
            string pbErr = cfg.Validate(template.baseScores);
            if (pbErr != null)
                return $"Template '{template.name}' has invalid Ability Score allocation: {pbErr}";
            if (cfg.TotalCost(template.baseScores) > cfg.budget)
                return $"Template '{template.name}' exceeds Point Buy budget ({cfg.TotalCost(template.baseScores)}/{cfg.budget}).";

            if (!string.IsNullOrEmpty(template.starterPassiveId) && template.starterPassiveId != classDef.starterPassiveId)
                return $"Template '{template.name}' specifies incompatible starter passive '{template.starterPassiveId}' for class '{classDef.name}'.";

            var tempLoadout = new SkillLoadout();
            tempLoadout.InitializeForClass(classDef);
            if (template.activeSkillLoadout != null)
            {
                if (template.activeSkillLoadout.Length > SkillLoadout.ActiveSlotCount)
                    return $"Template '{template.name}' exceeds {SkillLoadout.ActiveSlotCount} active skill slots.";
                foreach (var sid in template.activeSkillLoadout)
                {
                    if (!string.IsNullOrEmpty(sid) && !tempLoadout.UnlockedActiveSkills.Contains(sid))
                        return $"Template '{template.name}' equips locked active skill '{sid}'.";
                }
            }
            if (template.passiveSkillLoadout != null)
            {
                if (template.passiveSkillLoadout.Length > SkillLoadout.PassiveSlotCount)
                    return $"Template '{template.name}' exceeds {SkillLoadout.PassiveSlotCount} passive skill slots.";
                foreach (var pid in template.passiveSkillLoadout)
                {
                    if (!string.IsNullOrEmpty(pid) && !tempLoadout.UnlockedPassiveSkills.Contains(pid))
                        return $"Template '{template.name}' equips locked passive skill '{pid}'.";
                }
            }

            var eqAbilBonus = AbilityScores.Zero;
            if (template.startingEquipment != null)
            {
                foreach (var entry in template.startingEquipment)
                {
                    if (string.IsNullOrEmpty(entry.equipmentId)) continue;
                    if (!Database.Equipment.TryGetValue(entry.equipmentId, out var item))
                        return $"Template '{template.name}' references unknown equipment '{entry.equipmentId}'.";
                    eqAbilBonus = eqAbilBonus.Plus(item.abilityBonuses);
                }
                var finalScores = template.baseScores.Plus(race.abilityModifiers).Plus(eqAbilBonus);
                foreach (var entry in template.startingEquipment)
                {
                    if (string.IsNullOrEmpty(entry.equipmentId)) continue;
                    var item = Database.Equipment[entry.equipmentId];
                    string eqErr = EquipmentValidator.Validate(item, entry.slot, classDef, race, 1, finalScores);
                    if (eqErr != null)
                        return $"Template '{template.name}' equipment error: {eqErr}";
                }
            }

            return null;
        }

        // Applies a predefined template to the current character after full validation.
        // Rejects invalid templates without silently accepting invalid data. Player remains free to edit before confirming.
        public bool TryApplyTemplate(string templateId, out string error)
        {
            if (string.IsNullOrEmpty(templateId) || !Database.Templates.TryGetValue(templateId, out var t))
            {
                error = $"Unknown template ID '{templateId}'.";
                LastFeedbackMessage = error.ToUpperInvariant();
                return false;
            }
            return TryApplyTemplate(t, out error);
        }

        public bool TryApplyTemplate(CharacterTemplateDef template, out string error)
        {
            error = ValidateTemplate(template, PointBuy.Config);
            if (error != null)
            {
                LastFeedbackMessage = error.ToUpperInvariant();
                return false;
            }

            IsConfirmed = false;
            Method = CreationMethod.Template;
            ActiveTemplateId = template.id;
            Race = Database.Races[template.raceId];
            Class = Database.Classes[template.classId];
            Background = !string.IsNullOrEmpty(template.backgroundId) && Database.Backgrounds.TryGetValue(template.backgroundId, out var bg)
                ? bg
                : Database.Backgrounds.Values.First();
            CharacterName = template.name;

            PointBuy.SetAllOrThrow(template.baseScores);
            Loadout.InitializeForClass(Class);

            if (template.activeSkillLoadout != null && template.activeSkillLoadout.Length > 0)
            {
                for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
                    Loadout.TryEquipActive(i, i < template.activeSkillLoadout.Length ? template.activeSkillLoadout[i] : null, out _);
            }
            if (template.passiveSkillLoadout != null && template.passiveSkillLoadout.Length > 0)
            {
                for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
                    Loadout.TryEquipPassive(i, i < template.passiveSkillLoadout.Length ? template.passiveSkillLoadout[i] : null, out _);
            }

            Equipped.Clear();
            if (template.startingEquipment != null)
            {
                foreach (var entry in template.startingEquipment)
                {
                    if (!string.IsNullOrEmpty(entry.equipmentId) && Database.Equipment.TryGetValue(entry.equipmentId, out var item))
                        Equipped[entry.slot] = item;
                }
            }
            else
            {
                ResetToClassStartingEquipment();
            }

            LastFeedbackMessage = $"LOADED TEMPLATE: {template.name.ToUpperInvariant()}.";
            Changed?.Invoke();
            return true;
        }

        // Generates a valid random Point Buy Ability Score allocation within [minScore..maxScoreBeforeRacial]
        // that spends the full configured budget (Section 5).
        public static AbilityScores GenerateRandomPointBuy(PointBuyConfig config, Random rng = null)
        {
            var cfg = config ?? PointBuyConfig.Standard;
            var r = rng ?? new Random();
            var scores = AbilityScores.Uniform(cfg.baseScore);

            int safety = 64;
            while (cfg.RemainingPoints(scores) > 0 && safety-- > 0)
            {
                int rem = cfg.RemainingPoints(scores);
                var candidates = new List<Ability>();
                foreach (var a in AbilityScores.AllAbilities)
                {
                    int cur = scores[a];
                    if (cur >= cfg.maxScoreBeforeRacial) continue;
                    int stepCost = cfg.GetCost(cur + 1) - cfg.GetCost(cur);
                    if (stepCost <= rem)
                        candidates.Add(a);
                }
                if (candidates.Count == 0) break;
                var pick = candidates[r.Next(candidates.Count)];
                scores[pick] = scores[pick] + 1;
            }
            return scores;
        }

        // Random Creation: automatically generates a valid character with random Race, Class, Point Buy allocation,
        // Starter Passive, Skill Loadout, and valid Starting Equipment using bounded retries (Section 5).
        public const int MaxRandomRetries = 16;

        public bool TryRandomize(Random rng, out string error)
        {
            var r = rng ?? new Random();
            error = null;

            for (int attempt = 0; attempt < MaxRandomRetries; attempt++)
            {
                string raceId = Database.PlayableRaceIds[r.Next(Database.PlayableRaceIds.Length)];
                string classId = Database.PlayableClassIds[r.Next(Database.PlayableClassIds.Length)];
                if (!Database.Races.TryGetValue(raceId, out var race) || !Database.Classes.TryGetValue(classId, out var classDef))
                    continue;

                var randomScores = GenerateRandomPointBuy(PointBuy.Config, r);
                if (PointBuy.Config.Validate(randomScores) != null)
                    continue;

                IsConfirmed = false;
                Method = CreationMethod.Random;
                ActiveTemplateId = null;
                Race = race;
                Class = classDef;
                Background = Database.Backgrounds.Values.ElementAt(r.Next(Database.Backgrounds.Count));
                CharacterName = Class.name;

                PointBuy.SetAllOrThrow(randomScores);
                Loadout.InitializeForClass(Class);

                Equipped.Clear();
                foreach (var slot in EquipmentSlots.All)
                {
                    var validForSlot = Database.Equipment.Values
                        .Where(item =>
                            item.rarity == EquipmentRarity.Common &&
                            EquipmentValidator.Validate(item, slot, Class, Race, 1, FinalScores) == null)
                        .ToList();

                    if (slot == EquipmentSlot.Accessory2 && Equipped.TryGetValue(EquipmentSlot.Accessory1, out var acc1) && acc1 != null)
                    {
                        var distinctAcc = validForSlot.Where(x => x.id != acc1.id).ToList();
                        if (distinctAcc.Count > 0) validForSlot = distinctAcc;
                    }

                    if (validForSlot.Count > 0)
                        Equipped[slot] = validForSlot[r.Next(validForSlot.Count)];
                }

                RemoveInvalidEquippedItems();
                error = Validate();
                if (error == null)
                {
                    LastFeedbackMessage = $"RANDOMIZED: {Race.name.ToUpperInvariant()} {Class.name.ToUpperInvariant()}.";
                    Changed?.Invoke();
                    return true;
                }
            }

            error = error ?? "Random generation could not produce a valid configuration.";
            LastFeedbackMessage = error.ToUpperInvariant();
            return false;
        }

        public void Randomize(Random rng = null)
        {
            if (!TryRandomize(rng, out string err))
                throw new InvalidOperationException(err);
        }

        // Restores the session from an existing pending CharacterDef so a player can edit a character from Party Review.
        public void LoadFromCharacterDef(CharacterDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            IsConfirmed = false;
            Method = def.creationMethod;
            ActiveTemplateId = def.templateId;
            Race = !string.IsNullOrEmpty(def.raceId) && Database.Races.TryGetValue(def.raceId, out var r) ? r : Database.Races["human"];
            Class = !string.IsNullOrEmpty(def.classId) && Database.Classes.TryGetValue(def.classId, out var c) ? c : Database.Classes["fighter"];
            Background = !string.IsNullOrEmpty(def.backgroundId) && Database.Backgrounds.TryGetValue(def.backgroundId, out var bg)
                ? bg
                : Database.Backgrounds.Values.First();
            CharacterName = !string.IsNullOrWhiteSpace(def.name) ? def.name : Class.name;

            PointBuy.SetAllOrThrow(def.baseScores.Equals(default) ? AbilityScores.DefaultBase : def.baseScores);
            Loadout.InitializeForClass(Class);

            if (def.activeSkillLoadout != null && def.activeSkillLoadout.Length > 0)
            {
                for (int i = 0; i < Math.Min(SkillLoadout.ActiveSlotCount, def.activeSkillLoadout.Length); i++)
                    Loadout.activeSlots[i] = def.activeSkillLoadout[i];
            }
            if (def.passiveSkillLoadout != null && def.passiveSkillLoadout.Length > 0)
            {
                for (int i = 0; i < Math.Min(SkillLoadout.PassiveSlotCount, def.passiveSkillLoadout.Length); i++)
                    Loadout.passiveSlots[i] = def.passiveSkillLoadout[i];
            }

            Equipped.Clear();
            if (def.equipmentLoadout != null && def.equipmentLoadout.Length > 0)
            {
                foreach (var assign in def.equipmentLoadout)
                    if (!string.IsNullOrEmpty(assign.equipmentId) && Database.Equipment.TryGetValue(assign.equipmentId, out var eq))
                        Equipped[assign.slot] = eq;
            }
            else if (def.startingEquipment != null)
            {
                foreach (var eid in def.startingEquipment)
                    if (!string.IsNullOrEmpty(eid) && Database.Equipment.TryGetValue(eid, out var eq))
                        Equipped[eq.slot] = eq;
            }

            RemoveInvalidEquippedItems();
            CurrentStep = CreationStep.SelectRace;
            LastFeedbackMessage = $"EDITING {CharacterName.ToUpperInvariant()} ({Race.name.ToUpperInvariant()} {Class.name.ToUpperInvariant()}).";
            Changed?.Invoke();
        }

        // Individual Character Review summary containing Race, Class, Creation Method, Ability Scores,
        // Ability Modifiers, Derived Stats, Starter Passive, Active/Passive Skill Loadout, and Equipment Loadout (Section 11).
        public string GetCharacterReviewSummary()
        {
            var fin = FinalScores;
            var mods = AbilityModifiers;
            var der = PreviewDerivedStats;
            string spName = StarterPassive != null ? StarterPassive.name : "None";
            string actives = string.Join(", ", Loadout.GetEquippedActives());
            string passives = string.Join(", ", Loadout.GetEquippedPassives());
            string eqSummary = string.Join(", ", EquipmentSlots.All.Select(s =>
                $"{EquipmentSlots.ShortName(s)}:{(Equipped.TryGetValue(s, out var e) && e != null ? e.name : "Empty")}"));

            return $"NAME: {CharacterName} | METHOD: {Method} | RACE: {Race?.name} | CLASS: {Class?.name}\n" +
                   $"SCORES: STR {fin.str}({AbilityScores.FormatMod(mods.str)}) DEX {fin.dex}({AbilityScores.FormatMod(mods.dex)}) " +
                   $"VIT {fin.vit}({AbilityScores.FormatMod(mods.vit)}) INT {fin.intel}({AbilityScores.FormatMod(mods.intel)}) " +
                   $"WIS {fin.wis}({AbilityScores.FormatMod(mods.wis)}) CHA {fin.cha}({AbilityScores.FormatMod(mods.cha)})\n" +
                   $"DERIVED: HP {der.maxHp} MP {der.maxMp} ATK {der.physicalPower} RNG {der.rangedPower} MAG {der.magicPower} DEF {der.physicalDefense} MDF {der.magicDefense} INI {der.initiative}\n" +
                   $"STARTER PASSIVE: {spName} | ACTIVES: [{actives}] | PASSIVES: [{passives}]\n" +
                   $"EQUIPMENT: {eqSummary}";
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
                creationMethod = Method,
                templateId = ActiveTemplateId,
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

        // Reusable validation for a CharacterDef against all Character Creation rules (Race, Class, Point Buy, Starter Passive, Equipment).
        public static string ValidateCharacterDef(CharacterDef def, PointBuyConfig config = null)
        {
            if (def == null) return "Character is null.";
            if (string.IsNullOrWhiteSpace(def.id)) return "Character ID is required.";
            if (string.IsNullOrWhiteSpace(def.name)) return "Character name is required.";
            if (string.IsNullOrEmpty(def.raceId) || !Database.Races.TryGetValue(def.raceId, out var race))
                return $"Unknown race '{def.raceId}'.";
            if (string.IsNullOrEmpty(def.classId) || !Database.Classes.TryGetValue(def.classId, out var classDef))
                return $"Unknown class '{def.classId}'.";

            var cfg = config ?? PointBuyConfig.Standard;
            string pbErr = cfg.Validate(def.baseScores);
            if (pbErr != null) return pbErr;

            var eqAbilBonus = AbilityScores.Zero;
            if (def.equipmentLoadout != null)
            {
                foreach (var assign in def.equipmentLoadout)
                {
                    if (string.IsNullOrEmpty(assign.equipmentId)) continue;
                    if (!Database.Equipment.TryGetValue(assign.equipmentId, out var eq))
                        return $"Unknown equipment '{assign.equipmentId}'.";
                    eqAbilBonus = eqAbilBonus.Plus(eq.abilityBonuses);
                }
                var finalScores = def.baseScores.Plus(race.abilityModifiers).Plus(eqAbilBonus);
                foreach (var assign in def.equipmentLoadout)
                {
                    if (string.IsNullOrEmpty(assign.equipmentId)) continue;
                    var eq = Database.Equipment[assign.equipmentId];
                    string eqErr = EquipmentValidator.Validate(eq, assign.slot, classDef, race, 1, finalScores);
                    if (eqErr != null) return eqErr;
                }
            }
            return null;
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

        // Replaces the confirmed party in the roster with a validated 4-character party.
        public void CommitConfirmedParty(IEnumerable<CharacterDef> confirmedParty)
        {
            if (confirmedParty == null) throw new ArgumentNullException(nameof(confirmedParty));
            var list = confirmedParty.ToList();
            if (list.Count != PartySize)
                throw new InvalidOperationException($"Confirmed party must contain {PartySize} characters.");
            if (list.Distinct().Count() != PartySize || list.Select(c => c.id).Distinct().Count() != PartySize)
                throw new InvalidOperationException("Confirmed party contains duplicate character instances.");

            SelectedPartyIds.Clear();
            AssembledParty.Clear();
            foreach (var c in list)
            {
                AddCharacter(c, selectInPartyIfRoom: false);
                SelectedPartyIds.Add(c.id);
                AssembledParty.Add(c);
            }
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

    // State machine for New Game Character Creation -> Party Review -> Party Confirmation -> Save -> Opening Sequence -> First Expedition (Section 12).
    public enum NewGameFlowState
    {
        Idle = 0,
        NewGameInitialization = 1,
        PendingCharacterCreation = 2,
        PendingPartyReview = 3,
        PartyConfirmation = 4,
        PersistentSaveInitialization = 5,
        OpeningSequence = 6,
        FirstExpedition = 7
    }

    // Orchestrates New Game Initial Party Creation (4 characters via Manual, Template, or Random Creation),
    // Randomize All, Reroll Selected, Full Party Review, and Party Confirmation while keeping pending state
    // strictly separated from the persistent GameSave and confirmed CharacterRoster until ConfirmParty() (Sections 2-5, 11-12).
    public class NewGamePartyBuilder
    {
        public NewGameFlowState FlowState { get; private set; } = NewGameFlowState.Idle;
        public CharacterCreationSession CurrentSession { get; }
        public readonly List<CharacterDef> PendingParty = new List<CharacterDef>();
        public int CurrentMemberIndex { get; private set; }
        public int SelectedReviewIndex { get; private set; }
        public bool EditingExistingMember { get; private set; }
        public bool IsPartyConfirmed { get; private set; }
        public string LastFeedback { get; private set; } = "";

        public event Action Changed;

        public NewGamePartyBuilder(CharacterCreationSession session = null)
        {
            CurrentSession = session ?? new CharacterCreationSession();
            CurrentSession.Changed += () => Changed?.Invoke();
            StartNewGame();
        }

        // Initializes a pending New Game character creation flow without modifying any existing saved game.
        public void StartNewGame()
        {
            FlowState = NewGameFlowState.NewGameInitialization;
            PendingParty.Clear();
            CurrentMemberIndex = 0;
            SelectedReviewIndex = 0;
            EditingExistingMember = false;
            IsPartyConfirmed = false;
            CurrentSession.CreateNewCharacter();
            FlowState = NewGameFlowState.PendingCharacterCreation;
            LastFeedback = "NEW GAME: CREATE PARTY MEMBER 1 OF 4.";
            Changed?.Invoke();
        }

        public void SelectReviewMember(int slotIndex)
        {
            if (PendingParty.Count == 0) return;
            SelectedReviewIndex = Math.Clamp(slotIndex, 0, PendingParty.Count - 1);
            Changed?.Invoke();
        }

        // Confirms the current character being configured and adds/updates them in the pending party (Section 3).
        // Never allows duplicate character instances in the pending party.
        public bool TryConfirmCurrentCharacter(out CharacterDef confirmedHero, out string error)
        {
            confirmedHero = null;
            error = CurrentSession.Validate();
            if (error != null)
            {
                LastFeedback = error.ToUpperInvariant();
                return false;
            }

            int targetSlot = EditingExistingMember ? SelectedReviewIndex : PendingParty.Count;
            if (!EditingExistingMember && PendingParty.Count >= CharacterRoster.PartySize)
            {
                error = $"Pending party already has {CharacterRoster.PartySize} characters.";
                LastFeedback = error.ToUpperInvariant();
                return false;
            }

            string heroId = $"hero_{targetSlot + 1}_{CurrentSession.Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            CurrentSession.CharacterName = $"{CurrentSession.Class.name}";
            var def = CurrentSession.Confirm(null, heroId);

            if (PendingParty.Where((_, idx) => !EditingExistingMember || idx != targetSlot).Any(c => ReferenceEquals(c, def) || c.id == def.id))
            {
                error = $"Character '{def.name}' ({def.id}) is already in the party.";
                LastFeedback = error.ToUpperInvariant();
                return false;
            }

            if (EditingExistingMember && targetSlot >= 0 && targetSlot < PendingParty.Count)
            {
                PendingParty[targetSlot] = def;
                EditingExistingMember = false;
            }
            else
            {
                PendingParty.Add(def);
            }

            confirmedHero = def;
            IsPartyConfirmed = false;

            if (PendingParty.Count < CharacterRoster.PartySize)
            {
                CurrentMemberIndex = PendingParty.Count;
                SelectedReviewIndex = PendingParty.Count - 1;
                FlowState = NewGameFlowState.PendingCharacterCreation;
                CurrentSession.CreateNewCharacter();
                LastFeedback = $"CONFIRMED HERO {PendingParty.Count}/4 ({def.name.ToUpperInvariant()}). CREATE HERO {PendingParty.Count + 1}/4.";
            }
            else
            {
                CurrentMemberIndex = CharacterRoster.PartySize - 1;
                SelectedReviewIndex = 0;
                FlowState = NewGameFlowState.PendingPartyReview;
                LastFeedback = "ALL 4 HEROES CREATED! REVIEW AND CONFIRM PARTY.";
            }

            Changed?.Invoke();
            return true;
        }

        // Rejects adding the same CharacterDef instance/ID directly into the pending party.
        public bool TryAddPendingCharacter(CharacterDef def, out string error)
        {
            error = null;
            if (def == null)
            {
                error = "Character is null.";
                return false;
            }
            if (PendingParty.Count >= CharacterRoster.PartySize)
            {
                error = $"Pending party is already full ({CharacterRoster.PartySize}/{CharacterRoster.PartySize}).";
                return false;
            }
            if (PendingParty.Contains(def) || PendingParty.Any(c => c.id == def.id))
            {
                error = $"Duplicate character instance '{def.id}' cannot be added to the party.";
                return false;
            }
            string valErr = CharacterCreationSession.ValidateCharacterDef(def, CurrentSession.PointBuy.Config);
            if (valErr != null)
            {
                error = valErr;
                return false;
            }
            PendingParty.Add(def);
            IsPartyConfirmed = false;
            if (PendingParty.Count == CharacterRoster.PartySize)
                FlowState = NewGameFlowState.PendingPartyReview;
            Changed?.Invoke();
            return true;
        }

        // Section 5.1 — Randomize All:
        // Generates a completely new randomized initial party of 4 valid, independently initialized characters.
        // Enters PendingPartyReview without auto-confirming the party, without starting the expedition,
        // and without modifying any existing saved game.
        public bool TryRandomizeAll(Random rng, out string error)
        {
            var r = rng ?? new Random();
            error = null;
            var generated = new List<CharacterDef>(CharacterRoster.PartySize);
            var tempSession = new CharacterCreationSession(CurrentSession.PointBuy.Config);

            for (int i = 0; i < CharacterRoster.PartySize; i++)
            {
                if (!tempSession.TryRandomize(r, out error))
                {
                    LastFeedback = (error ?? "RANDOMIZE ALL FAILED.").ToUpperInvariant();
                    return false;
                }
                string id = $"hero_{i + 1}_{tempSession.Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                tempSession.CharacterName = tempSession.Class.name;
                var hero = tempSession.Confirm(null, id);
                generated.Add(hero);
            }

            PendingParty.Clear();
            PendingParty.AddRange(generated);
            CurrentMemberIndex = CharacterRoster.PartySize - 1;
            SelectedReviewIndex = 0;
            EditingExistingMember = false;
            IsPartyConfirmed = false;
            FlowState = NewGameFlowState.PendingPartyReview;

            CurrentSession.LoadFromCharacterDef(PendingParty[0]);
            LastFeedback = "RANDOMIZED ALL 4 HEROES. REVIEW PARTY BEFORE CONFIRMING.";
            Changed?.Invoke();
            return true;
        }

        public void RandomizeAll(Random rng = null)
        {
            if (!TryRandomizeAll(rng, out string err))
                throw new InvalidOperationException(err);
        }

        // Section 5.2 — Reroll Selected:
        // Generates a new randomized configuration for ONLY the selected party member (0..3).
        // The other 3 characters remain completely unchanged. Does not auto-confirm the character or party,
        // and does not modify any existing saved game.
        public bool TryRerollSelected(int slotIndex, Random rng, out string error)
        {
            error = null;
            if (slotIndex < 0 || slotIndex >= PendingParty.Count)
            {
                error = $"Invalid party member index {slotIndex}.";
                LastFeedback = error.ToUpperInvariant();
                return false;
            }

            var r = rng ?? new Random();
            var tempSession = new CharacterCreationSession(CurrentSession.PointBuy.Config);
            if (!tempSession.TryRandomize(r, out error))
            {
                LastFeedback = (error ?? "REROLL FAILED.").ToUpperInvariant();
                return false;
            }

            string id = $"hero_{slotIndex + 1}_{tempSession.Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            tempSession.CharacterName = tempSession.Class.name;
            var replacement = tempSession.Confirm(null, id);
            replacement.creationMethod = CreationMethod.Random;

            PendingParty[slotIndex] = replacement;
            SelectedReviewIndex = slotIndex;
            IsPartyConfirmed = false;

            // Load the rerolled configuration into CurrentSession in unconfirmed state so the player can review it.
            CurrentSession.LoadFromCharacterDef(replacement);
            LastFeedback = $"REROLLED HERO {slotIndex + 1}: {replacement.raceId.ToUpperInvariant()} {replacement.name.ToUpperInvariant()}.";
            Changed?.Invoke();
            return true;
        }

        public void RerollSelected(int slotIndex, Random rng = null)
        {
            if (!TryRerollSelected(slotIndex, rng, out string err))
                throw new InvalidOperationException(err);
        }

        public void RerollSelected(Random rng = null) => RerollSelected(SelectedReviewIndex, rng);

        // Allows the player to return from Full Party Review to edit a specific character's configuration (Section 11).
        public bool ReturnToEditCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PendingParty.Count) return false;
            SelectedReviewIndex = slotIndex;
            CurrentMemberIndex = slotIndex;
            EditingExistingMember = true;
            IsPartyConfirmed = false;
            FlowState = NewGameFlowState.PendingCharacterCreation;
            CurrentSession.LoadFromCharacterDef(PendingParty[slotIndex]);
            LastFeedback = $"EDITING HERO {slotIndex + 1} ({PendingParty[slotIndex].name.ToUpperInvariant()}).";
            Changed?.Invoke();
            return true;
        }

        public string ValidatePendingParty()
        {
            if (PendingParty.Count < CharacterRoster.PartySize)
                return $"Party is incomplete ({PendingParty.Count}/{CharacterRoster.PartySize} characters).";
            if (PendingParty.Count > CharacterRoster.PartySize)
                return $"Party exceeds maximum size ({PendingParty.Count}/{CharacterRoster.PartySize}).";
            if (PendingParty.Any(c => c == null))
                return "Party contains an unconfigured character slot.";
            if (PendingParty.Distinct().Count() != CharacterRoster.PartySize ||
                PendingParty.Select(c => c.id).Distinct().Count() != CharacterRoster.PartySize)
                return "Party contains duplicate character instances.";

            for (int i = 0; i < PendingParty.Count; i++)
            {
                string cErr = CharacterCreationSession.ValidateCharacterDef(PendingParty[i], CurrentSession.PointBuy.Config);
                if (cErr != null)
                    return $"Party member {i + 1} ({PendingParty[i].name}) is invalid: {cErr}";
            }
            return null;
        }

        public bool CanConfirmParty => ValidatePendingParty() == null;

        // Full Party Review overview containing all 4 characters, Race & Class, stats, Starter Passive,
        // Equipment summary, and Party Composition summary (Section 11).
        public string GetPartyReviewSummary()
        {
            if (PendingParty.Count == 0) return "NO CHARACTERS IN PENDING PARTY.";
            var lines = new List<string>();
            var roles = new List<string>();
            for (int i = 0; i < PendingParty.Count; i++)
            {
                var c = PendingParty[i];
                var u = new Unit(c);
                string raceName = u.race?.name ?? c.raceId;
                string className = u.classDef?.name ?? c.classId;
                string spName = !string.IsNullOrEmpty(u.starterPassiveId) && Database.Passives.TryGetValue(u.starterPassiveId, out var sp)
                    ? sp.name
                    : "None";
                string mainEq = u.equipped.TryGetValue(EquipmentSlot.MainHand, out var mh) && mh != null ? mh.name : "Unarmed";
                string offEq = u.equipped.TryGetValue(EquipmentSlot.SecondHand, out var sh) && sh != null ? sh.name : "None";
                roles.Add($"{className}({u.role})");
                lines.Add($"{i + 1}. {c.name} [{c.creationMethod}] - {raceName} {className} | HP:{u.Derived.maxHp} MP:{u.Derived.maxMp} ATK:{u.Derived.physicalPower} DEF:{u.Derived.physicalDefense} MAG:{u.Derived.magicPower} INI:{u.Derived.initiative} | Passive: {spName} | Eqp: {mainEq}/{offEq}");
            }
            lines.Add($"COMPOSITION ({PendingParty.Count}/{CharacterRoster.PartySize}): " + string.Join(", ", roles));
            return string.Join("\n", lines);
        }

        // Confirms the completed 4-character party, commits it to the roster, initializes the persistent GameSave,
        // and transitions to the Opening Sequence (Section 11 & Section 12).
        public bool TryConfirmParty(CharacterRoster targetRoster, out string error, int? initialSeed = null)
        {
            error = ValidatePendingParty();
            if (error != null)
            {
                LastFeedback = error.ToUpperInvariant();
                return false;
            }

            FlowState = NewGameFlowState.PartyConfirmation;
            targetRoster?.CommitConfirmedParty(PendingParty);

            FlowState = NewGameFlowState.PersistentSaveInitialization;
            int seed = initialSeed ?? Environment.TickCount;
            GameSave.SaveNewGame(PendingParty, seed);

            IsPartyConfirmed = true;
            FlowState = NewGameFlowState.OpeningSequence;
            LastFeedback = "PARTY CONFIRMED AND SAVED! READY FOR OPENING SEQUENCE.";
            Changed?.Invoke();
            return true;
        }

        // Transitions from Opening Sequence into the First Expedition (Section 2 & Section 12).
        public bool TryBeginOpeningSequence(out string error)
        {
            error = ValidatePendingParty();
            if (error != null)
            {
                LastFeedback = error.ToUpperInvariant();
                return false;
            }
            if (!IsPartyConfirmed)
            {
                error = "Party must be confirmed before beginning the opening sequence.";
                LastFeedback = error.ToUpperInvariant();
                return false;
            }
            FlowState = NewGameFlowState.OpeningSequence;
            Changed?.Invoke();
            return true;
        }

        public RunState BeginFirstExpedition(CharacterRoster roster, int seed = 1)
        {
            if (!IsPartyConfirmed)
                throw new InvalidOperationException("Cannot begin the first expedition before confirming the party.");
            string err = ValidatePendingParty();
            if (err != null) throw new InvalidOperationException(err);

            var activeRoster = roster ?? new CharacterRoster(startWithEmptyParty: true);
            activeRoster.CommitConfirmedParty(PendingParty);
            FlowState = NewGameFlowState.FirstExpedition;
            var run = activeRoster.BeginExpedition(seed);
            GameSave.SaveRunState(run, PendingParty);
            Changed?.Invoke();
            return run;
        }
    }

    // Persistent save data for a single character. Stores authoritative primary data (Race, Class, Base Ability Scores,
    // Starter Passive, Skill Loadout, Equipment Loadout, Creation Method) so Derived Stats are recalculated on load
    // without fragile duplicate state (Section 12).
    [Serializable]
    public class SavedCharacterData
    {
        public string id;
        public string name;
        public string role;
        public CreationMethod creationMethod;
        public string templateId;
        public string raceId;
        public string classId;
        public string backgroundId;
        public AbilityScores baseScores;
        public AbilityScores permanentAbilityBonuses;
        public string starterPassiveId;
        public string[] activeSkillLoadout;
        public string[] passiveSkillLoadout;
        public string[] unlockedSkills;
        public EquipmentSlotAssignment[] equipmentLoadout;
        public EquipmentInstanceSaveData[] equippedInstances = Array.Empty<EquipmentInstanceSaveData>();
        public int level = 1;
        public int hp;
        public int mp;
        public int unspentAsiPoints;

        public static SavedCharacterData FromCharacterDef(CharacterDef d, Unit runtimeUnit = null)
        {
            return new SavedCharacterData
            {
                id = d.id,
                name = d.name,
                role = d.role,
                creationMethod = d.creationMethod,
                templateId = d.templateId,
                raceId = d.raceId,
                classId = d.classId,
                backgroundId = d.backgroundId,
                baseScores = runtimeUnit != null ? runtimeUnit.BaseScores : d.baseScores,
                permanentAbilityBonuses = runtimeUnit != null ? runtimeUnit.PermanentAbilityBonuses : AbilityScores.Zero,
                starterPassiveId = runtimeUnit != null ? runtimeUnit.starterPassiveId : d.starterPassiveId,
                activeSkillLoadout = runtimeUnit != null ? runtimeUnit.skillLoadout.ActiveSlots.ToArray() : (d.activeSkillLoadout ?? Array.Empty<string>()),
                passiveSkillLoadout = runtimeUnit != null ? runtimeUnit.skillLoadout.PassiveSlots.ToArray() : (d.passiveSkillLoadout ?? Array.Empty<string>()),
                unlockedSkills = runtimeUnit != null
                    ? runtimeUnit.skillLoadout.UnlockedActiveSkills.Concat(runtimeUnit.skillLoadout.UnlockedPassiveSkills).ToArray()
                    : (d.unlockedSkills ?? Array.Empty<string>()),
                equipmentLoadout = runtimeUnit != null
                    ? runtimeUnit.equipped.Where(kv => kv.Value != null).Select(kv => new EquipmentSlotAssignment(kv.Key, kv.Value.id)).ToArray()
                    : (d.equipmentLoadout ?? Array.Empty<EquipmentSlotAssignment>()),
                equippedInstances = runtimeUnit != null
                    ? runtimeUnit.equippedInstances.Where(kv => kv.Value != null).Select(kv => kv.Value.ToSaveData(kv.Key)).ToArray()
                    : Array.Empty<EquipmentInstanceSaveData>(),
                level = runtimeUnit != null ? runtimeUnit.level : 1,
                hp = runtimeUnit != null ? runtimeUnit.hp : 0,
                mp = runtimeUnit != null ? runtimeUnit.mp : 0,
                unspentAsiPoints = runtimeUnit != null ? runtimeUnit.unspentAsiPoints : 0,
            };
        }

        public CharacterDef ToCharacterDef()
        {
            var race = !string.IsNullOrEmpty(raceId) && Database.Races.TryGetValue(raceId, out var r) ? r : Database.Races["human"];
            var classDef = !string.IsNullOrEmpty(classId) && Database.Classes.TryGetValue(classId, out var c) ? c : Database.Classes["fighter"];
            var bg = !string.IsNullOrEmpty(backgroundId) && Database.Backgrounds.TryGetValue(backgroundId, out var b) ? b : Database.Backgrounds.Values.First();

            var def = new CharacterDef
            {
                id = id,
                name = name,
                role = !string.IsNullOrEmpty(role) ? role : classDef.role,
                creationMethod = creationMethod,
                templateId = templateId,
                description = $"{race.name} {classDef.name} ({classDef.gameplayIdentity ?? bg.name}). {classDef.description}",
                raceId = race.id,
                classId = classDef.id,
                backgroundId = bg.id,
                baseScores = baseScores.Equals(default) ? AbilityScores.DefaultBase : baseScores,
                starterPassiveId = !string.IsNullOrEmpty(starterPassiveId) ? starterPassiveId : classDef.starterPassiveId,
                activeSkillLoadout = activeSkillLoadout ?? Array.Empty<string>(),
                passiveSkillLoadout = passiveSkillLoadout ?? Array.Empty<string>(),
                unlockedSkills = unlockedSkills ?? Array.Empty<string>(),
                equipmentLoadout = equipmentLoadout ?? Array.Empty<EquipmentSlotAssignment>(),
                startingEquipment = (equipmentLoadout ?? Array.Empty<EquipmentSlotAssignment>()).Select(a => a.equipmentId).ToArray(),
                passives = (passiveSkillLoadout ?? Array.Empty<string>()).Where(s => !string.IsNullOrEmpty(s)).ToArray(),
                skills = (activeSkillLoadout ?? Array.Empty<string>()).Where(s => !string.IsNullOrEmpty(s)).ToArray(),
                affinities = classDef.affinities ?? Affinities.None,
                sprite = classDef.sprite ?? Art.Knight,
            };

            // Recalculate Derived Stats authoritatively through Unit
            var unit = new Unit(def);
            if (equippedInstances != null && equippedInstances.Length > 0)
            {
                unit.equipped.Clear();
                unit.equippedInstances.Clear();
                foreach (var eqData in equippedInstances)
                {
                    var inst = EquipmentInstance.FromSaveData(eqData);
                    if (inst != null && eqData.slot >= 0)
                    {
                        var slot = (EquipmentSlot)eqData.slot;
                        unit.TryEquip(slot, inst, out _);
                    }
                }
            }
            def.stats = unit.Derived.ToLegacyStats();
            return def;
        }
    }

    [Serializable]
    public class GameSaveData
    {
        public bool hasConfirmedParty;
        public int seed = 1;
        public int act = 1;
        public int gold;
        public string currentNodeId;
        public string[] clearedNodeIds = Array.Empty<string>();
        public string[] items = Array.Empty<string>();
        public string[] relics = Array.Empty<string>();
        public SavedCharacterData[] party = Array.Empty<SavedCharacterData>();

        // Inventory & Equipment persistence (GDD Section 10 & 16)
        public EquipmentInstanceSaveData[] inventoryEquipment = Array.Empty<EquipmentInstanceSaveData>();
        public ConsumableStackSaveData[] inventoryConsumables = Array.Empty<ConsumableStackSaveData>();
        public MaterialEntrySaveData[] inventoryMaterials = Array.Empty<MaterialEntrySaveData>();
        public string[] inventoryKeyItems = Array.Empty<string>();
    }

    // Persistent Save Manager using the existing PlayerPrefs + JsonUtility architecture (Sections 2 & 12).
    public static class GameSave
    {
        public const string PrefKey = "obscure.save";
        static GameSaveData memoryCache;

        public static bool HasSavedGame()
        {
            if (memoryCache != null && memoryCache.hasConfirmedParty && memoryCache.party != null && memoryCache.party.Length == CharacterRoster.PartySize)
                return true;
            string raw = UnityEngine.PlayerPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(raw)) return false;
            try
            {
                var data = UnityEngine.JsonUtility.FromJson<GameSaveData>(raw);
                return data != null && data.hasConfirmedParty && data.party != null && data.party.Length == CharacterRoster.PartySize;
            }
            catch
            {
                return false;
            }
        }

        public static void ClearSave()
        {
            memoryCache = null;
            UnityEngine.PlayerPrefs.DeleteKey(PrefKey);
            UnityEngine.PlayerPrefs.Save();
        }

        public static GameSaveData SaveNewGame(IReadOnlyList<CharacterDef> confirmedParty, int seed = 1)
        {
            if (confirmedParty == null || confirmedParty.Count != CharacterRoster.PartySize)
                throw new InvalidOperationException($"Cannot save an incomplete party (requires {CharacterRoster.PartySize} characters).");

            var data = new GameSaveData
            {
                hasConfirmedParty = true,
                seed = seed,
                act = 1,
                gold = 0,
                currentNodeId = "",
                clearedNodeIds = Array.Empty<string>(),
                items = Database.StartingItems.ToArray(),
                inventoryConsumables = Database.StartingItems.Select(id => new ConsumableStackSaveData { itemId = id, count = 1, maxStack = 99 }).ToArray(),
                relics = Array.Empty<string>(),
                party = confirmedParty.Select(c => SavedCharacterData.FromCharacterDef(c)).ToArray()
            };
            WriteSaveData(data);
            return data;
        }

        public static GameSaveData SaveRunState(RunState run, IReadOnlyList<CharacterDef> partyDefs = null)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var defs = partyDefs ?? run.Party.Select(u => new CharacterDef
            {
                id = u.id,
                name = u.name,
                role = u.role,
                raceId = u.race?.id ?? "human",
                classId = u.classDef?.id ?? "fighter",
                backgroundId = u.background?.id ?? "oathsworn",
                baseScores = u.BaseScores,
                starterPassiveId = u.starterPassiveId,
                activeSkillLoadout = u.skillLoadout.ActiveSlots.ToArray(),
                passiveSkillLoadout = u.skillLoadout.PassiveSlots.ToArray(),
                unlockedSkills = u.skillLoadout.UnlockedActiveSkills.Concat(u.skillLoadout.UnlockedPassiveSkills).ToArray(),
                equipmentLoadout = u.equipped.Where(kv => kv.Value != null).Select(kv => new EquipmentSlotAssignment(kv.Key, kv.Value.id)).ToArray(),
            }).ToList();

            var invData = run.Inventory != null ? run.Inventory.ToSaveData() : new InventorySaveData();
            var data = new GameSaveData
            {
                hasConfirmedParty = true,
                seed = run.Seed,
                act = run.Act,
                gold = run.Gold,
                currentNodeId = run.Current?.id ?? "",
                clearedNodeIds = run.Cleared.ToArray(),
                items = run.Items.ToArray(),
                relics = run.Relics.ToArray(),
                party = defs.Select((d, i) => SavedCharacterData.FromCharacterDef(d, i < run.Party.Count ? run.Party[i] : null)).ToArray(),
                inventoryEquipment = invData.equipment ?? Array.Empty<EquipmentInstanceSaveData>(),
                inventoryConsumables = invData.consumables ?? Array.Empty<ConsumableStackSaveData>(),
                inventoryMaterials = invData.materials ?? Array.Empty<MaterialEntrySaveData>(),
                inventoryKeyItems = invData.keyItems ?? Array.Empty<string>()
            };
            WriteSaveData(data);
            return data;
        }

        public static void WriteSaveData(GameSaveData data)
        {
            memoryCache = data;
            string json = UnityEngine.JsonUtility.ToJson(data);
            UnityEngine.PlayerPrefs.SetString(PrefKey, json);
            UnityEngine.PlayerPrefs.Save();
        }

        public static GameSaveData LoadSaveData()
        {
            string raw = UnityEngine.PlayerPrefs.GetString(PrefKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    var parsed = UnityEngine.JsonUtility.FromJson<GameSaveData>(raw);
                    if (parsed != null && parsed.hasConfirmedParty)
                    {
                        memoryCache = parsed;
                        return parsed;
                    }
                }
                catch
                {
                    // Fall back to memoryCache if present
                }
            }
            return memoryCache;
        }

        // Restores the confirmed CharacterRoster and active RunState from the saved game (Section 2 Continue & Section 12).
        public static bool TryLoadContinue(out CharacterRoster restoredRoster, out RunState restoredRun)
        {
            restoredRoster = null;
            restoredRun = null;
            var data = LoadSaveData();
            if (data == null || !data.hasConfirmedParty || data.party == null || data.party.Length != CharacterRoster.PartySize)
                return false;

            var partyDefs = data.party.Select(p => p.ToCharacterDef()).ToList();
            restoredRoster = new CharacterRoster(startWithEmptyParty: true);
            restoredRoster.CommitConfirmedParty(partyDefs);

            restoredRun = new RunState(MapGenerator.Generate(data.seed), partyDefs, data.seed);
            restoredRun.RestoreFromSave(data);
            return true;
        }
    }
}
