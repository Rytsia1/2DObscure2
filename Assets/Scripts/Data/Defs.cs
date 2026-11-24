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

    public enum DodgeDir { None, Up, Down, Left, Right }
    public enum QteVariant { None, GuardCue, Slip, Flurry, Roll }
    public enum RemixKind { Mirror, Upgrade, Tempo }

    [Serializable]
    public struct HitCue
    {
        public QteVariant variant;
        public DodgeDir dir;

        public HitCue(QteVariant variant, DodgeDir dir = DodgeDir.None)
        {
            this.variant = variant;
            this.dir = dir;
        }
    }

    // Authored per-move QTE motif (QTE Variation Spec Part D). null on EnemyAttack preserves established random behavior.
    public class QteMotif
    {
        public HitCue[] hits;
        public int feintHit = -1;
        public int stutterHit = -1;
        public bool stutterSlowFirst;
        public RemixKind[] remixes;
    }

    // Priority: data-driven, deterministic policy (EnemyAttack.aiPriority / cooldown) planned at round start (Bab 10 §8.9).
    public enum AiKind { Basic, Tactical, Boss, Priority }
    public enum TargetRule { Random, LowestHp, LowestDef, LowestMdef, HighestThreat }

    // Enemy tactical role (Spec §6.2) and intelligence tier (Architecture §12 v1.1, Variant Spec §7.5 v1.1).
    public enum AiRole
    {
        Vanguard,
        RangedOpportunist,
        ElementalAttacker,
        Disruptor,
        Bruiser,
        Attrition,
        ComboAttacker,
        Executioner,
        DefensivePunisher,
        Support,
        ArcaneCaster,
        Skirmisher,
        BreakBruiser,
        AreaController,
        AdaptiveController,
    }

    public enum AiTier { Normal, Enhanced, Elite, Miniboss, Boss }

    // Creature-family behavioral instinct evaluated by InstinctLayer (distinct from deliberate tactical scoring).
    public enum InstinctKind
    {
        None = 0,
        Predator = 1,    // Hunts wounded/debuffed prey; grows more aggressive when wounded (incl. parasitic hunger)
        Relentless = 2,  // Fixates on its last targeted hero until fallen
        Skittish = 3,    // Self-preserving when wounded/threatened; favors deterrence/self/drain and avoids retaliators
        Territorial = 4, // Retaliates against whoever just struck it; punishes grouped intruders
        Guardian = 5,    // Protects endangered allies via healing or pressuring whoever attacks them
    }

    public static class AiTierExt
    {
        // For every Normal-tier restriction (incl. FocusCapNormal), Enhanced counts as Normal (Variant Spec §7.5 v1.1).
        public static bool IsNormalTier(this AiTier t) => t == AiTier.Normal || t == AiTier.Enhanced;
    }

    // Per-family stat multipliers applied once after Normal dungeon scaling: EnhancedStat = floor(NormalScaledStat * mult) (Variant Spec §4.3).
    [Serializable]
    public struct VariantStatMult
    {
        public float hp, mp, atk, matk, def, mdef, spd;

        public VariantStatMult(float hp = 1f, float mp = 1f, float atk = 1f, float matk = 1f, float def = 1f, float mdef = 1f, float spd = 1f)
        {
            this.hp = hp; this.mp = mp; this.atk = atk; this.matk = matk; this.def = def; this.mdef = mdef; this.spd = spd;
        }

        public float Hp => hp > 0f ? hp : 1f;
        public float Mp => mp > 0f ? mp : 1f;
        public float Atk => atk > 0f ? atk : 1f;
        public float Matk => matk > 0f ? matk : 1f;
        public float Def => def > 0f ? def : 1f;
        public float Mdef => mdef > 0f ? mdef : 1f;
        public float Spd => spd > 0f ? spd : 1f;
    }

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
        public string lineageId;
        public Ability[] racialBonusChoice = Array.Empty<Ability>();
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
        public Ability[] racialBonusChoice = Array.Empty<Ability>();
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
        public Dictionary<StatusId, float> statusResist; // Per-status resistance offsets in [-0.50, +0.75] (Analysis Spec Part F)
        public StatusId[] statusImmune = Array.Empty<StatusId>(); // Explicit per-status immunity flags
        public Element[] concealedAffinities = Array.Empty<Element>(); // Boss affinities requiring 2 triggers to reveal (Analysis Spec Part D)
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
        public AiRole aiRole = AiRole.Vanguard; // Spec §6.2 tactical personality
        public AiTier aiTier = AiTier.Normal;   // Architecture §12 v1.1 intelligence capability tier
        public InstinctKind instinct = InstinctKind.None; // Instinct Behavior Layer archetype
        public string baseEnemyId;     // Enhanced Common Variant: id of the base common enemy definition (Variant Spec §3.1)
        public string enhancedVariant; // On-Defeat Transformation: id of the "<base>_enhanced" variant definition
        public float transformChance;  // On-Defeat Transformation: independent roll chance [0, 1] when HP reaches 0
        public bool isEnhanced;        // True for Enhanced Common Variants (Variant Spec §12.3)
        public VariantStatMult statMult; // Variant Stat Profile applied once after Normal scaling (Variant Spec §4.3)
        // Number of full turns per round (posture-B starting value, untested: Zoroa P1 = 2, P2 = 2; everyone else 1).
        // If it fails: if P1 feels unfair, set P1 = 1 and keep the double turn as the Phase 2 escalation.
        public int turnsPerRound = 1;
        public float threatOverride;   // optional override for EncounterRoller threat budget calculations
    }

    // One enemy move. Each hit gets its own defense reaction; allTargets hits every living party member
    // with one shared reaction per hit. Statuses roll once per target per attack (Bab 12 §4.3).
    public class EnemyAttack
    {
        public string name;
        public string patternId;      // Optional configuration id (e.g. "alt") when an Enhanced variant has 2 patterns of the same skill name
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
        public float healAlly;        // > 0: support skill healing one ally (incl. self) for this fraction of its Max HP; no damage, no QTE
        public StatusApply[] buffAlly; // support skill: statuses applied to one ally (incl. self); no damage, no QTE, no resist roll
        public int summonCount;       // > 0: summons up to this many living summon allies (Harvest Call = 2); no damage, no QTE
        public int breakPower;        // flat Break Power dealt to each target hit, on top of the normal hit break (Gravefall = 30)

        // Priority AI policy (configurable, deterministic). Highest ready priority wins.
        public int aiPriority;
        public int cooldown;          // rounds the move is unavailable after it executes
        public int initialCooldown;   // rounds unavailable at battle / phase start
        public int minTargets = 1;    // living opponents required (area moves)
        public float aiMpBelow;       // > 0: only chosen while current MP is below this fraction of Max MP
        public float aiAllyHpBelow;   // healAlly moves: only chosen while some ally is at or below this fraction of Max HP
        public float aiUnlessAllyHpBelow; // > 0: skipped while any ally is at or below this fraction of Max HP
        // > 0: only chosen while the user's Break gauge has at most this fraction left, or from round aiBreakGateRound on.
        // Gives the party a real interrupt window (Tutorial_Prologue_Design §3 Node 3).
        public float aiBreakLeftBelow;
        public int aiBreakGateRound;
        public QteMotif motif;        // optional authored QTE motif (QTE Variation Spec Part D); null = established random generator

        public bool StatusOnly => hits == 0 && !self && !SupportsAlly && !Summons;
        public bool Heals => healAlly > 0f;
        public bool BuffsAlly => buffAlly != null && buffAlly.Length > 0;
        // Targets one ally (heal and/or buff) instead of a foe.
        public bool SupportsAlly => Heals || BuffsAlly;
        public bool Summons => summonCount > 0;
        // MP is consumed only by Skills: a Basic Attack is a free, untelegraphed damaging move, always affordable.
        public bool IsBasicAttack => mpCost == 0 && !self && hits > 0 && !telegraph && !Summons;
        public bool UsesMagic => element != Element.Physical;
        public bool HasHeavy => isHeavy || heavyHit >= 0;
        public bool HeavyAt(int hit) => isHeavy || hit == heavyHit;
        public float PowerAt(int hit) => hitPower != null && hit < hitPower.Length ? hitPower[hit] : power;
        // Must be shown to the player before it lands (Bab 12 §6.4).
        public bool NeedsTelegraph => telegraph || HasHeavy || StatusOnly || Summons;
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
        // Counter skill (Bab 12 §6.5): > 0 readies a counter for counterTurns of the user's turns; a Defensive Perfect
        // while ready retaliates with this multiplier after the enemy action.
        public float counterMultiplier;
        public int counterTurns;
        public int counterComboGain; // Martial Artist combo gained each time the counter fires (Bab 8 §5.1)
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
        public string[] enemies;   // reference composition (threat budget & fallback), spawn order = array order (initiative tie-break)
        public string[] anchors;   // always spawned first when rolling procedurally (null = none)
        public string[] pool;      // candidate enemy ids for remaining slots (null = fixed encounter, no roll)
        public int minCount, maxCount; // inclusive rolled enemy count bounds (0 = enemies.Length)
        public bool Procedural => pool != null && pool.Length > 0;
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
        public bool HasDemerit => !string.IsNullOrEmpty(demerit) && demerit != "— (no demerit)";

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

    public enum RewardKind { Item, Relic, Upgrade, Gold, Equipment, Modifier }

    public class RewardOption
    {
        public RewardKind kind;
        public string id;   // item / relic / character / modifier id
        public int amount;  // gold
        public ElementType element = ElementType.None; // for element-specific modifiers (MOD-014, MOD-024)
    }

    // Gold is always granted; the player picks at most one of the choices.
    public class RewardOffer
    {
        public string title, text;
        public int gold;
        public int bonusGoldFromModifier;
        public string bonusItemId; // rolled by Scavenger's Eye (MOD-010)
        public int[] expGained = Array.Empty<int>();    // per party index; 0 for KO'd or max-level heroes
        public int[] levelsGained = Array.Empty<int>(); // per party index
        public List<RewardOption> choices = new List<RewardOption>();
        public List<RewardOption> modifierChoices = new List<RewardOption>();
    }

    public enum EventCategory
    {
        General,
        Shrine,
        NpcEncounter,
        Treasure,
        TrapAndHazard,
        Supernatural,
        PuzzleAndExploration,
        Survival,
        CursedAndRisk
    }

    public enum EventRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public static class EventTags
    {
        public const string Sacred = "sacred";
        public const string Supernatural = "supernatural";
        public const string Relic = "relic";
        public const string CursedReward = "cursed_reward";
        public const string Npc = "npc";
        public const string Shop = "shop";
        public const string Hidden = "hidden";
        public const string Trap = "trap";
        public const string Hazard = "hazard";
        public const string AncientMechanism = "ancient_mechanism";
        public const string Destructible = "destructible";
        public const string Narrow = "narrow";
        public const string Corruption = "corruption";
        public const string Liquid = "liquid";
        public const string Dragon = "dragon";
        public const string Mineral = "mineral";
        public const string Aquatic = "aquatic";
    }

    public enum AttributeCheckResultCategory
    {
        CriticalFailure = 0,
        Failure = 1,
        Success = 2,
        CriticalSuccess = 3
    }

    public class AttributeCheckOutcomeDef
    {
        public string outcome;
        public int gold;
        public int healPercent;
        public int mpRestorePercent;
        public int hurtPercent;
        public int hazardDamagePercent;
        public string item;
        public string materialId;
        public int materialAmount;
        public string equipmentId;
        public bool randomEquipment;
        public bool relic;
        public StatusApply battleBuff;
        public string modifierId;
        public ElementType modifierElement = ElementType.None;
        public string curseModifierId;
        public string bonusInfoText;
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
        public StatusApply battleBuff; // applied to the party at the start of every battle until dungeon exit
        public string modifierId;   // grants a specific dungeon modifier
        public ElementType modifierElement = ElementType.None;
        public bool randomModifier; // grants a modifier rolled on entering the event
        public ModifierCategory? modifierCategoryFilter;

        // Dungeon Event Pool & Racial Utility extensions
        public string requiredRace; // "human", "elf", "dwarf", "halfling", "dragonborn", "liichtian", "merfolk"
        public bool isImprovise;    // Human Adaptability option for events with no racial choices
        public string requiredItem; // consumable id consumed on selection
        public string requiredMaterialId;
        public int requiredMaterialAmount;
        public int mpRestorePercent;    // of max MP, living members
        public int hazardDamagePercent; // party HP damage reduced 50% by Halfling Small and Nimble
        public bool isTrapChoice;       // eligible for R014 Pallbearer's Gloves minor chest trap negation
        public string materialId;
        public int materialAmount;
        public string equipmentId;
        public bool randomEquipment;
        public string curseModifierId;
        public bool hasHiddenCurse;
        public bool hasHiddenOutcome;
        public int riskChancePercent;
        public int riskFailHurtPercent;
        public int riskFailHazardDamagePercent;
        public string riskFailModifierId;
        public string riskFailOutcome;
        public bool riskGrantsRewardOnFail;
        public string bonusInfoText;

        // D20 Attribute Check System (FELNA-SYS-EVT-D20-001)
        public bool isAttributeCheck;
        public Ability checkAbility;
        public int difficultyClass;
        public AttributeCheckOutcomeDef criticalSuccessOutcome;
        public AttributeCheckOutcomeDef successOutcome;
        public AttributeCheckOutcomeDef failureOutcome;
        public AttributeCheckOutcomeDef criticalFailureOutcome;

        public bool IsRacialOption => !string.IsNullOrEmpty(requiredRace) && !isImprovise;

        public AttributeCheckOutcomeDef GetOutcomeForCategory(AttributeCheckResultCategory category) => category switch
        {
            AttributeCheckResultCategory.CriticalSuccess => criticalSuccessOutcome ?? successOutcome,
            AttributeCheckResultCategory.Success => successOutcome,
            AttributeCheckResultCategory.Failure => failureOutcome,
            AttributeCheckResultCategory.CriticalFailure => criticalFailureOutcome ?? failureOutcome,
            _ => failureOutcome
        };

        public bool MayGrantRelic =>
            relic ||
            (isAttributeCheck && (
                (criticalSuccessOutcome != null && criticalSuccessOutcome.relic) ||
                (successOutcome != null && successOutcome.relic) ||
                (failureOutcome != null && failureOutcome.relic) ||
                (criticalFailureOutcome != null && criticalFailureOutcome.relic)));

        public bool MayGrantEquipment =>
            randomEquipment || !string.IsNullOrEmpty(equipmentId) ||
            (isAttributeCheck && (
                (criticalSuccessOutcome != null && (criticalSuccessOutcome.randomEquipment || !string.IsNullOrEmpty(criticalSuccessOutcome.equipmentId))) ||
                (successOutcome != null && (successOutcome.randomEquipment || !string.IsNullOrEmpty(successOutcome.equipmentId))) ||
                (failureOutcome != null && (failureOutcome.randomEquipment || !string.IsNullOrEmpty(failureOutcome.equipmentId))) ||
                (criticalFailureOutcome != null && (criticalFailureOutcome.randomEquipment || !string.IsNullOrEmpty(criticalFailureOutcome.equipmentId)))));
    }

    public class EventDef
    {
        public string id, title, text;
        public EventCategory category = EventCategory.General;
        public EventRarity rarity = EventRarity.Uncommon;
        public int dungeonAct = 0; // 0 = shared/legacy, 1+ = exclusive to that dungeon act
        public string[] tags = Array.Empty<string>();
        public string supernaturalSourceClue;
        public string hiddenClue;
        public EventChoice[] choices;

        public bool HasTag(string tag) =>
            !string.IsNullOrEmpty(tag) &&
            tags != null &&
            tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));

        public bool HasAnyRacialChoice() =>
            choices != null && choices.Any(c => c != null && c.IsRacialOption);
    }

    public class NodeDef
    {
        public string id;
        public NodeType type;
        public int floor, lane;
        public string[] next = Array.Empty<string>();
        public string encounter;   // combat nodes
        public string[] enemies;   // rolled enemy ids for this node (null = use encounter.enemies)
        public string eventId;     // event nodes
        public bool IsCombat => type == NodeType.Battle || type == NodeType.Elite || type == NodeType.Boss;
    }

    public class DungeonStageDef
    {
        public int stage = 1;
        public string stageName;
        public string bossEncounterId;
        public bool allowEliteNodes = true;
    }

    public class DungeonPlanDef
    {
        public int act = 1;
        public string name;
        public DungeonStageDef[] stages = Array.Empty<DungeonStageDef>();
        public int TotalStages => stages != null && stages.Length > 0 ? stages.Length : 1;

        public DungeonStageDef GetStage(int stage)
        {
            if (stages == null || stages.Length == 0) return null;
            int idx = Math.Clamp(stage, 1, stages.Length) - 1;
            return stages[idx];
        }
    }

    public class MapDef
    {
        public string name;
        public int act = 1;
        public int stage = 1;
        public int totalStages = 1;
        public string stageName;
        public bool allowEliteNodes = true;
        public bool isPrologue; // the 3-node Tutorial Prologue (Run/Prologue.cs)
        public NodeDef[] nodes;
        public NodeDef Get(string id) => nodes.First(n => n.id == id);
        public int Floors => nodes.Max(n => n.floor) + 1;
        public bool IsFinalStage => stage >= Math.Max(1, totalStages);
    }
}
