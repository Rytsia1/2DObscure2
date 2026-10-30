using System;
using System.Collections.Generic;
using System.Linq;

// Static gameplay definitions. Runtime state lives in Run/, never here.
namespace Obscure
{
    public enum Tier { Normal, Elite, Boss }
    public enum NodeType { Battle, Elite, Event, Treasure, Rest, Boss }
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

    public enum AiKind { Basic, Tactical, Boss }

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
    }

    // One enemy move. Each hit gets its own defense reaction; allTargets hits every living party member.
    public class EnemyAttack
    {
        public string name;
        public int hits = 1;
        public float power = 1f;      // per hit, multiplies Atk
        public Element element = Element.Physical;
        public bool allTargets;
        public bool self;             // a stance/buff on the user: no damage, no reaction QTE
        public float delay;           // extra hold before the first hit lands (a delayed swing)
        public int phase;             // 0 = any phase, otherwise only in that boss phase
        public StatusApply[] apply;   // applied to each target not perfectly parried (or to self if self)
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
        public string word; // typing QTE word for spells, null for physical skills
        public SkillDifficulty difficulty = SkillDifficulty.Medium; // typing QTE difficulty
        public int cooldown;      // own turns before reuse (0 = none)
        public float perfectPower; // overrides the Perfect QTE multiplier (critical hits), 0 = default
        public StatusApply[] apply;

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
        public int heal, mana;
    }

    public class EncounterDef
    {
        public string id;
        public Tier tier;
        public string[] enemies;
        public int minFloor; // earliest floor this encounter can be rolled on
        public string music;  // MusicIds battle theme; null = the MusicLibrary's default (or random) battle theme
    }

    // Passive run-long bonus: stats added to every party member, plus optional after-battle healing.
    public class RelicDef
    {
        public string id, name, description;
        public Stats bonus;
        public AbilityScores abilityBonus;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
        public float victoryHeal; // fraction of max HP restored after each won battle
    }

    // A permanent improvement for one character (rest sites, elite rewards).
    public class UpgradeDef
    {
        public string character, name;
        public Stats bonus;
        public AbilityScores abilityBonus;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
    }

    public enum RewardKind { Item, Relic, Upgrade, Gold }

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
