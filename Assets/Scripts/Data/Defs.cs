using System;
using System.Collections.Generic;
using System.Linq;

// Static gameplay definitions. Runtime state lives in Run/, never here.
namespace Obscure
{
    public enum Tier { Normal, Elite, Boss }
    public enum DragonbornLineage { Crimson = 0, Stone = 1, Azure = 2, Frost = 3 }

    [Serializable]
    public class DragonbornLineageDef
    {
        public string id;
        public string name;
        public DragonbornLineage lineage;
        public Element element;
        public AbilityScores abilityModifiers;
    }
    public enum NodeType { Battle, Elite, Event, Treasure, Rest, Boss, Shop }
    public enum SkillKind { Physical, Magic, Heal, Support }
    public enum TargetType { Enemy, AllEnemies, Ally, AllAllies, Self }

    [Serializable]
    public struct Stats
    {
        public int maxHp, maxMp, atk, def, spd, mag;
        public Stats(int maxHp, int maxMp, int atk, int def, int spd, int mag)
        {
            this.maxHp = maxHp; this.maxMp = maxMp; this.atk = atk; this.def = def; this.spd = spd; this.mag = mag;
        }

        public Stats Plus(Stats b) => new Stats(maxHp + b.maxHp, maxMp + b.maxMp, atk + b.atk, def + b.def, spd + b.spd, mag + b.mag);

        public bool IsZero => maxHp == 0 && maxMp == 0 && atk == 0 && def == 0 && spd == 0 && mag == 0;

        // "MAX HP 125>140, DEF 13>15": what changes when b is added.
        public string Preview(Stats b)
        {
            var parts = new List<string>();
            void Add(string label, int from, int delta) { if (delta != 0) parts.Add($"{label} {from}>{from + delta}"); }
            Add("MAX HP", maxHp, b.maxHp); Add("MAX MP", maxMp, b.maxMp); Add("ATK", atk, b.atk);
            Add("DEF", def, b.def); Add("SPD", spd, b.spd); Add("MAG", mag, b.mag);
            return string.Join(", ", parts);
        }
    }

    // Priority: data-driven, deterministic policy (EnemyAttack.aiPriority / cooldown) planned at round start (Bab 10 §8.9).
    public enum AiKind { Basic, Tactical, Boss, Priority }
    public enum TargetRule { Random, LowestHp, LowestDef }

    public enum CreationMethod
    {
        Manual = 0,
        Template = 1,
        Random = 2
    }

    [Serializable]
    public struct EquipmentSlotAssignment
    {
        public EquipmentSlot slot;
        public string equipmentId;

        public EquipmentSlotAssignment(EquipmentSlot slot, string equipmentId)
        {
            this.slot = slot;
            this.equipmentId = equipmentId;
        }
    }

    // Data-driven predefined character build template for Template Creation (Section 4.2).
    [Serializable]
    public class CharacterTemplateDef
    {
        public string id;
        public string name;
        public string description;
        public string raceId = "human";
        public string classId = "fighter";
        public string backgroundId = "oathsworn";
        public AbilityScores baseScores = AbilityScores.DefaultBase;
        public string starterPassiveId;
        public string[] activeSkillLoadout = Array.Empty<string>();
        public string[] passiveSkillLoadout = Array.Empty<string>();
        public EquipmentSlotAssignment[] startingEquipment = Array.Empty<EquipmentSlotAssignment>();
    }

    // Combat uses only HP and MP resource pools (Section 5).
    public class CharacterDef
    {
        public string id, name, role, description;
        public CreationMethod creationMethod = CreationMethod.Manual;
        public string templateId;
        public string raceId = "human", classId = "knight", backgroundId = "oathsworn";
        public string lineageId;
        public AbilityScores baseScores = AbilityScores.DefaultBase;
        public string starterPassiveId;
        public string[] activeSkillLoadout = Array.Empty<string>();
        public string[] passiveSkillLoadout = Array.Empty<string>();
        public string[] unlockedSkills = Array.Empty<string>();
        public string[] startingEquipment = Array.Empty<string>();
        public EquipmentSlotAssignment[] equipmentLoadout = Array.Empty<EquipmentSlotAssignment>();
        public string[] passives = Array.Empty<string>();
        public Affinities affinities = Affinities.None;
        public Stats stats;
        public string[] skills;
        public string[] talentIds = Array.Empty<string>();
        public string[] sprite;
    }

    public class EnemyDef
    {
        public string id, name, description;
        public string shortName, tag; // combat log name, 2-3 letter turn-order tag
        public Tier tier;             // also selects the defense QTE profile
        public AiKind ai;
        public Affinities affinities = Affinities.None;
        public EnemyAffinityProfile affinityProfile;
        public AbilityScores abilityScores = AbilityScores.Uniform(10);
        public Stats stats;
        public int magicDef = -1;     // -1 defaults to stats.def if not separately specified
        public int maxBreakGauge = 40;
        public int breakResistance = 0;
        public int breakDuration = 1;
        public int breakStunTurns = 1;
        public float breakDamageMultiplier = 1.5f;
        public bool bossBreakRecoverOnPhase2 = true;
        public int gold;
        public string[] sprite;
        public EnemyAttack[] attacks;
        public float phase2At;         // boss: HP fraction that triggers phase 2 (0 = no phases)
        public string phase2Text;
        public StatusApply phase2Buff;
        public string nextPhase;       // boss: EnemyDef id that replaces this one when its HP bar is depleted (separate full bar)
        public string type, role;      // roster flavour: "Undead", "Vanguard"
        public TargetRule targetRule;  // Priority AI: who single-target moves aim at
    }

    // One enemy move. Each hit gets its own defense reaction; allTargets hits every living party member
    // with one shared reaction per hit. Statuses roll once per target per attack (Bab 12 §4.3).
    public class EnemyAttack
    {
        public string name;
        public int hits = 1;          // 0 = status-only action: no damage, no Defensive QTE
        public float power = 1f;      // per hit, multiplies Atk (Physical) or Magic (Elemental)
        public Element element = Element.Physical;
        public bool allTargets;
        public bool self;             // a stance/buff on the user: no damage, no reaction QTE
        public bool isHeavy;          // every hit is Heavy: Defensive Perfect reduces 50% damage instead of 100% (Bab 12)
        public float delay;           // extra hold before the first hit lands (a delayed swing)
        public int phase;             // 0 = any phase, otherwise only in that boss phase
        public StatusApply[] apply;   // rolled once per target per attack; a Perfect parry does not block it (Bab 11 §2.3)

        // Roster data (Dungeon 1 v1.7). Authored D1 damage is turned into ATK/MATK coefficients once
        // (EnemyDef stats at D1), so dungeon scaling of ATK/MATK is the only scaling damage receives.
        public int[] hitDamage;       // base damage per hit before mitigation
        public float[] hitPower;      // per-hit coefficient; null = power for every hit
        public float[] intervals;     // seconds between hit markers; null = tier default (Steady 350 ms)
        public int heavyHit = -1;     // index of the single Heavy marker (-1 = none)
        public int mpCost;            // paid when the attack executes; enemy MP never regenerates
        public bool telegraph;        // charged: one no-damage turn (shown as intent), released next turn; Broken cancels it
        public float drain;           // heals the user for this fraction of the final damage dealt
        public float restoreMp;       // self skill: restores this fraction of Max MP, capped at Max MP (Arcane Charge = 0.5)

        // Priority AI policy (configurable, deterministic). Highest ready priority wins.
        public int aiPriority;
        public int cooldown;          // rounds the move is unavailable after it executes
        public int initialCooldown;   // rounds unavailable at battle / phase start
        public int minTargets = 1;    // living opponents required (area moves)
        public float aiMpBelow;       // > 0: only chosen while current MP is below this fraction of Max MP

        public bool StatusOnly => hits == 0 && !self;
        // MP is consumed only by Skills: a Basic Attack is a free, untelegraphed damaging move, always affordable.
        public bool IsBasicAttack => mpCost == 0 && !self && hits > 0 && !telegraph;
        public bool UsesMagic => element != Element.Physical;
        public bool HasHeavy => isHeavy || heavyHit >= 0;
        public bool HeavyAt(int hit) => isHeavy || hit == heavyHit;
        public float PowerAt(int hit) => hitPower != null && hit < hitPower.Length ? hitPower[hit] : power;
        // Must be shown to the player before it lands (Bab 12 §6.4).
        public bool NeedsTelegraph => telegraph || HasHeavy || StatusOnly;
    }

    public class SkillDef
    {
        public string id, name, description;
        public SkillKind kind;
        public TargetType target;
        public int mpCost;
        public float power;
        public int hits = 1;
        public Element element = Element.Physical;
        public Ability? scalingAbility;    // optional override; defaults by SkillKind (STR/INT/WIS/CHA)
        public bool isRanged;              // uses RangedPower (DEX) and Ranged Accuracy (Hunter's Instinct)
        public int tier = 1;               // skill tier unlocked by character level
        public int maxRank = 3;            // max upgrade rank
        public string[] tags = Array.Empty<string>(); // e.g. "Arcane", "Pact", "Nature", "Cleric"
        public float breakMultiplier = 1f; // multiplier applied to Break Gauge reduction
        public float bonusVsBroken = 0f;   // extra damage multiplier against Broken targets
        public bool isFinisher;            // Finisher attacks consume combo and cannot critically hit (Bab 8 & 11)
        public string word; // typing QTE word for spells, null for physical skills
        public SkillDifficulty difficulty = SkillDifficulty.Medium; // typing QTE difficulty
        public int cooldown;      // own turns before reuse (0 = none)
        public float perfectPower; // overrides the Perfect QTE multiplier (critical hits), 0 = default
        public StatusApply[] apply;

        // Skill Progression & Equipment integration
        public string classId;
        public int branchIndex;
        public string[] prerequisites = Array.Empty<string>();
        public bool requiresShield;
        public WeaponType requiredWeaponType = WeaponType.None;
        public float critChanceBonus = 0f;
        public float powerPerRank = 0.25f;
        public float breakMultiplierPerRank = 0.2f;
        public int mpCostPerRank = 0;

        public float GetPower(int rank) => power + Math.Max(0, rank - 1) * powerPerRank;
        public float GetBreakMultiplier(int rank) => breakMultiplier + Math.Max(0, rank - 1) * breakMultiplierPerRank;
        public int GetMpCost(int rank) => Math.Max(0, mpCost + Math.Max(0, rank - 1) * mpCostPerRank);

        // Modular Damage System configuration (Prompt Sections 3, 8.3, 10, 11, 12).
        public int skillLevel = 1;
        public DamageComponent[] damageComponents = Array.Empty<DamageComponent>();
        public DamageSkillData damageSkillAsset;
        public bool appliesNullBreak;
        public AffinityDamageKey nullBreakTarget;

        public bool HasDamageSystemComponents =>
            (damageComponents != null && damageComponents.Length > 0) ||
            (damageSkillAsset != null && damageSkillAsset.GetComponentsForLevel(skillLevel).Count > 0);

        public IReadOnlyList<DamageComponent> GetDamageComponents(int? levelOverride = null)
        {
            int lvl = levelOverride ?? skillLevel;
            if (damageSkillAsset != null)
            {
                var fromAsset = damageSkillAsset.GetComponentsForLevel(lvl);
                if (fromAsset != null && fromAsset.Count > 0)
                    return fromAsset;
            }
            return damageComponents ?? Array.Empty<DamageComponent>();
        }

        public int GetDamageHitCount(int? levelOverride = null)
        {
            int lvl = levelOverride ?? skillLevel;
            if (damageSkillAsset != null)
                return damageSkillAsset.GetHitCountForLevel(lvl);
            return Math.Max(1, hits);
        }

        public bool HasTag(string tag) =>
            tags != null && tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }

    public class ItemDef
    {
        public string id, name, description;
        public string category; // "Potion", "Recovery Item", "Food", "Buff Item", "Revive Item", "Tactical Item", "Exploration Item"
        public EquipmentRarity rarity = EquipmentRarity.Common;
        public string itemEffect;
        public string target; // "Single ally", "One downed ally", "Party"
        public string usageRestriction; // "Combat / exploration", "Exploration only", "Combat only", "Dungeon map only", "Before combat"
        public int maxStack = 10;
        public int shopValue = 20;
        public string suggestedSource;

        // Effect values
        public int heal, mana;
        public bool isRevive;
        public float reviveHpPercent = 0.30f;
        public float healPercent;
        public float manaPercent;
    }

    public class EncounterDef
    {
        public string id, name, focus;
        public Tier tier;
        public string[] enemies;   // spawn order = array order (initiative tie-break)
        public int minFloor; // earliest floor this encounter can be rolled on
        public string music;  // MusicIds battle theme; null = the MusicLibrary's default (or random) battle theme
    }

    // Relic definition supporting both legacy stats and the modular Layer 1 Relic System.
    public class RelicDef
    {
        public string id, name, description;
        public Stats bonus;
        public AbilityScores abilityBonus;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
        public float victoryHeal; // fraction of max HP restored after each won battle

        // Modular Relic System properties (Felna_Layer1_Relic_List.xlsx)
        public RelicCategory category = RelicCategory.Utility;
        public RelicProposedRarity proposedRarity = RelicProposedRarity.Common;
        public string gameplayEffect;
        public string designIntent;
        public RelicTriggerType triggerType = RelicTriggerType.Combat;
        public RelicActivationLimit activationLimit = RelicActivationLimit.Passive;

        // Configurable numeric parameters
        public float value1;
        public float value2;
        public int intValue;
        public int maxPerBattle;

        public string demerit;
        public int buyPrice;
        public int sellPrice;
        public bool HasDemerit => !string.IsNullOrEmpty(demerit) && demerit != "— (tidak memiliki demerit)";

        public string Description => !string.IsNullOrEmpty(gameplayEffect) ? gameplayEffect : description;
    }

    // A permanent improvement for one character (rest sites, elite rewards).
    public class UpgradeDef
    {
        public string character, name;
        public Stats bonus;
        public AbilityScores abilityBonus;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
    }

    public enum RewardKind { Item, Relic, Upgrade, Gold, Equipment }

    public class RewardOption
    {
        public RewardKind kind;
        public string id;   // item / relic / character id
        public int amount;  // gold
    }

    // Gold is always granted; the player picks at most one of the choices.
    public class RewardOffer
    {
        public string title, text;
        public int gold;
        public List<RewardOption> choices = new List<RewardOption>();
    }

    public class EventChoice
    {
        public string label, outcome;
        public int goldCost;        // requirement and price
        public int gold;            // gained
        public int healPercent;     // of max HP, living members
        public int hurtPercent;     // of max HP, never below 1 HP
        public string item;
        public bool relic;          // grants the relic rolled for this event
        public StatusApply battleBuff; // applied to the party at the start of the next battle
    }

    public class EventDef
    {
        public string id, title, text;
        public EventChoice[] choices;
    }

    public class NodeDef
    {
        public string id;
        public NodeType type;
        public int floor, lane;
        public string[] next = Array.Empty<string>();
        public string encounter;   // combat nodes
        public string eventId;     // event nodes
        public bool IsCombat => type == NodeType.Battle || type == NodeType.Elite || type == NodeType.Boss;
    }

    public class MapDef
    {
        public string name;
        public int act = 1;
        public NodeDef[] nodes;
        public NodeDef Get(string id) => nodes.First(n => n.id == id);
        public int Floors => nodes.Max(n => n.floor) + 1;
    }
}
