using System;

using System.Collections.Generic;

using System.Linq;

using UnityEngine;



// All vertical-slice content and data-driven stat/character-creation definitions in one place.

namespace Obscure

{

    [Serializable]

    public class QteStatInteractionDef

    {

        public Ability ability;

        public string effectName;

        public string description;

        public float scalingPerModifier;

        public float maxBonus;

    }



    [Serializable]

    public class StatSystemConfigData

    {

        public PointBuyConfig pointBuy = PointBuyConfig.Standard;

        public AbilityScoreImprovementConfig asi = new AbilityScoreImprovementConfig();

        public AbilityScoreDef[] abilities;

        public DerivedStatDef[] derivedStats;

        public FormulaDef[] formulas;

    }



    public static class Database

    {

        static StatusApply Status(StatusId id, int turns, float magnitude, float chance = 1f, bool onSelf = false) =>

            new StatusApply { id = id, turns = turns, magnitude = magnitude, chance = chance, onSelf = onSelf };



        static EquipmentSlotAssignment Eq(EquipmentSlot slot, string id) => new EquipmentSlotAssignment(slot, id);



        // ---- 1. Ability Score Definitions (PRD Section 3 & 13.1) ----

        public static readonly Dictionary<Ability, AbilityScoreDef> AbilityDefs = new Dictionary<Ability, AbilityScoreDef>

        {

            [Ability.STR] = new AbilityScoreDef

            {

                abilityId = "STR", ability = Ability.STR, displayName = "Strength",

                description = "Physical power, melee damage, physical break, and block power.",

                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true

            },

            [Ability.DEX] = new AbilityScoreDef

            {

                abilityId = "DEX", ability = Ability.DEX, displayName = "Dexterity",

                description = "Accuracy, evasion, agility, initiative, ranged combat, and QTE timing.",

                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true

            },

            [Ability.VIT] = new AbilityScoreDef

            {

                abilityId = "VIT", ability = Ability.VIT, displayName = "Vitality",

                description = "HP, durability, physical defense, resistance, and QTE stability.",

                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true

            },

            [Ability.INT] = new AbilityScoreDef

            {

                abilityId = "INT", ability = Ability.INT, displayName = "Intelligence",

                description = "Magic power, spell damage, magical knowledge, and magic break.",

                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true

            },

            [Ability.WIS] = new AbilityScoreDef

            {

                abilityId = "WIS", ability = Ability.WIS, displayName = "Wisdom",

                description = "Healing, perception, spiritual abilities, max MP, and magic defense.",

                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true

            },

            [Ability.CHA] = new AbilityScoreDef

            {

                abilityId = "CHA", ability = Ability.CHA, displayName = "Charisma",

                description = "Social abilities, support power, certain magical effects, and class mechanics.",

                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true

            },

        };



        // ---- 2. Formula Configurations (PRD Section 8 & 13.3) ----

        public static readonly Dictionary<string, FormulaDef> Formulas = Index(new[]

        {

            new FormulaDef { formulaId = "max_hp_v1", inputs = new[] { "baseHp", "VITModifier", "hpGrowth" }, calculation = "BaseHP + VITMod * HPGrowth", scalingAbilities = new[] { Ability.VIT } },

            new FormulaDef { formulaId = "max_mp_v1", inputs = new[] { "baseMp", "WISModifier", "mpGrowth" }, calculation = "BaseMP + WISMod * MPGrowth", scalingAbilities = new[] { Ability.WIS } },

            new FormulaDef { formulaId = "max_shield_v1", inputs = new[] { "baseShield", "VITModifier", "shieldGrowth" }, calculation = "BaseShield + VITMod * ShieldGrowth", scalingAbilities = new[] { Ability.VIT } },

            new FormulaDef { formulaId = "physical_damage_v1", inputs = new[] { "weaponPower", "STRModifier", "weaponScaling" }, calculation = "WeaponPower + STRMod * WeaponScaling", scalingAbilities = new[] { Ability.STR } },

            new FormulaDef { formulaId = "ranged_damage_v1", inputs = new[] { "rangedPower", "DEXModifier", "rangedScaling" }, calculation = "RangedPower + DEXMod * RangedScaling", scalingAbilities = new[] { Ability.DEX } },

            new FormulaDef { formulaId = "magic_damage_v1", inputs = new[] { "spellPower", "INTModifier", "spellScaling" }, calculation = "SpellPower + INTMod * SpellScaling", scalingAbilities = new[] { Ability.INT } },

            new FormulaDef { formulaId = "healing_v1", inputs = new[] { "baseHealing", "WISModifier", "healingScaling" }, calculation = "BaseHealing + WISMod * HealingScaling", scalingAbilities = new[] { Ability.WIS } },

            new FormulaDef { formulaId = "support_v1", inputs = new[] { "baseSupport", "CHAModifier", "supportScaling" }, calculation = "BaseSupport + CHAMod * SupportScaling", scalingAbilities = new[] { Ability.CHA } },

            new FormulaDef { formulaId = "crit_chance_v1", inputs = new[] { "baseCrit", "DEXModifier", "critScaling" }, calculation = "BaseCrit + DEXMod * CritScaling", scalingAbilities = new[] { Ability.DEX } },

            new FormulaDef { formulaId = "crit_damage_v1", inputs = new[] { "baseCritDamage" }, calculation = "BaseCritDamage + Bonuses" },

            new FormulaDef { formulaId = "armor_pen_v1", inputs = new[] { "baseArmorPen", "STRModifier", "penScaling" }, calculation = "BasePen + STRMod * PenScaling", scalingAbilities = new[] { Ability.STR } },

            new FormulaDef { formulaId = "magic_pen_v1", inputs = new[] { "baseMagicPen", "INTModifier", "penScaling" }, calculation = "BasePen + INTMod * PenScaling", scalingAbilities = new[] { Ability.INT } },

            new FormulaDef { formulaId = "phys_def_v1", inputs = new[] { "baseDef", "VITModifier", "defScaling", "armor" }, calculation = "BaseDef + Armor + VITMod * DefScaling", scalingAbilities = new[] { Ability.VIT } },

            new FormulaDef { formulaId = "magic_def_v1", inputs = new[] { "baseMagDef", "WISModifier", "magDefScaling", "equip" }, calculation = "BaseMagDef + Equip + WISMod * MagDefScaling", scalingAbilities = new[] { Ability.WIS } },

            new FormulaDef { formulaId = "evasion_v1", inputs = new[] { "baseEvasion", "DEXModifier", "evasionScaling" }, calculation = "BaseEva + DEXMod * EvaScaling", scalingAbilities = new[] { Ability.DEX } },

            new FormulaDef { formulaId = "block_power_v1", inputs = new[] { "baseBlock", "STRModifier", "blockScaling" }, calculation = "BaseBlock + STRMod * BlockScaling", scalingAbilities = new[] { Ability.STR } },

            new FormulaDef { formulaId = "status_res_v1", inputs = new[] { "baseRes", "VITModifier", "WISModifier" }, calculation = "BaseRes + (VITMod + WISMod) * ResScaling", scalingAbilities = new[] { Ability.VIT, Ability.WIS } },

            new FormulaDef { formulaId = "stagger_res_v1", inputs = new[] { "baseStaggerRes", "VITModifier", "staggerScaling" }, calculation = "BaseStagger + VITMod * StaggerScaling", scalingAbilities = new[] { Ability.VIT } },

            new FormulaDef { formulaId = "initiative_v1", inputs = new[] { "baseInitiative", "DEXModifier", "bonuses" }, calculation = "BaseInit + DEXMod + Bonuses", scalingAbilities = new[] { Ability.DEX } },

            new FormulaDef { formulaId = "action_speed_v1", inputs = new[] { "baseSpeed", "DEXModifier", "speedScaling" }, calculation = "BaseSpeed + DEXMod * SpeedScaling", scalingAbilities = new[] { Ability.DEX } },

            new FormulaDef { formulaId = "qte_timing_v1", inputs = new[] { "baseQte", "DEXModifier", "qteScaling" }, calculation = "BaseTiming + DEXMod * TimingScaling", scalingAbilities = new[] { Ability.DEX } },

            new FormulaDef { formulaId = "qte_stability_v1", inputs = new[] { "baseStability", "VITModifier", "stabilityScaling" }, calculation = "BaseStability + VITMod * StabilityScaling", scalingAbilities = new[] { Ability.VIT } },

            new FormulaDef { formulaId = "counter_power_v1", inputs = new[] { "baseCounter", "STRModifier", "DEXModifier" }, calculation = "BaseCounter + (STRMod + DEXMod) * Scaling", scalingAbilities = new[] { Ability.STR, Ability.DEX } },

            new FormulaDef { formulaId = "phys_break_v1", inputs = new[] { "baseBreak", "STRModifier", "breakScaling" }, calculation = "BaseBreak + STRMod * BreakScaling", scalingAbilities = new[] { Ability.STR } },

            new FormulaDef { formulaId = "magic_break_v1", inputs = new[] { "baseBreak", "INTModifier", "breakScaling" }, calculation = "BaseBreak + INTMod * BreakScaling", scalingAbilities = new[] { Ability.INT } },

            new FormulaDef { formulaId = "defense_mitigation_v1", inputs = new[] { "rawDamage", "defense", "penetration" }, calculation = "RawDamage * (100 / (100 + Defense))" },

            new FormulaDef { formulaId = "hit_chance_v1", inputs = new[] { "baseHitChance", "attackerAccuracy", "targetEvasion" }, calculation = "Clamp(BaseHitChance + AttackerAccuracy - TargetEvasion, MinHit, MaxHit)" },

        }, f => f.formulaId);



        // ---- 3. Derived Stat Definitions (PRD Section 7 & 13.2) ----

        public static readonly Dictionary<DerivedStatId, DerivedStatDef> DerivedStatDefs = new Dictionary<DerivedStatId, DerivedStatDef>

        {

            [DerivedStatId.MaxHP] = new DerivedStatDef { statId = "MAX_HP", id = DerivedStatId.MaxHP, displayName = "Max HP", shortName = "HP", primarySources = new[] { "VIT" }, formulaId = "max_hp_v1", description = "Maximum health capacity." },

            [DerivedStatId.MaxMP] = new DerivedStatDef { statId = "MAX_MP", id = DerivedStatId.MaxMP, displayName = "Max MP", shortName = "MP", primarySources = new[] { "WIS" }, formulaId = "max_mp_v1", description = "Maximum magic resource." },

            [DerivedStatId.MaxShield] = new DerivedStatDef { statId = "MAX_SHIELD", id = DerivedStatId.MaxShield, displayName = "Max Shield", shortName = "SHD", primarySources = new[] { "VIT" }, formulaId = "max_shield_v1", description = "Maximum temporary shield capacity." },

            [DerivedStatId.PhysicalPower] = new DerivedStatDef { statId = "PHYS_PWR", id = DerivedStatId.PhysicalPower, displayName = "Physical Power", shortName = "ATK", primarySources = new[] { "STR" }, formulaId = "physical_damage_v1", description = "Melee physical damage scaling." },

            [DerivedStatId.RangedPower] = new DerivedStatDef { statId = "RNG_PWR", id = DerivedStatId.RangedPower, displayName = "Ranged Power", shortName = "RNG", primarySources = new[] { "DEX" }, formulaId = "ranged_damage_v1", description = "Precision and ranged damage scaling." },

            [DerivedStatId.MagicPower] = new DerivedStatDef { statId = "MAG_PWR", id = DerivedStatId.MagicPower, displayName = "Magic Power", shortName = "MAG", primarySources = new[] { "INT" }, formulaId = "magic_damage_v1", description = "Spell damage scaling." },

            [DerivedStatId.HealingPower] = new DerivedStatDef { statId = "HEAL_PWR", id = DerivedStatId.HealingPower, displayName = "Healing Power", shortName = "HEL", primarySources = new[] { "WIS" }, formulaId = "healing_v1", description = "Restorative spell effectiveness." },

            [DerivedStatId.SupportPower] = new DerivedStatDef { statId = "SUP_PWR", id = DerivedStatId.SupportPower, displayName = "Support Power", shortName = "SUP", primarySources = new[] { "CHA" }, formulaId = "support_v1", description = "Buff and command effectiveness." },

            [DerivedStatId.CriticalChance] = new DerivedStatDef { statId = "CRIT_CHC", id = DerivedStatId.CriticalChance, displayName = "Critical Chance", shortName = "CRT", primarySources = new[] { "DEX" }, formulaId = "crit_chance_v1", description = "Probability to land a critical hit." },

            [DerivedStatId.CriticalDamage] = new DerivedStatDef { statId = "CRIT_DMG", id = DerivedStatId.CriticalDamage, displayName = "Critical Damage", shortName = "CDM", primarySources = new[] { "DEX", "STR" }, formulaId = "crit_damage_v1", description = "Damage multiplier on critical hits." },

            [DerivedStatId.ArmorPenetration] = new DerivedStatDef { statId = "ARM_PEN", id = DerivedStatId.ArmorPenetration, displayName = "Armor Penetration", shortName = "APN", primarySources = new[] { "STR" }, formulaId = "armor_pen_v1", description = "Ignores enemy Physical Defense." },

            [DerivedStatId.MagicPenetration] = new DerivedStatDef { statId = "MAG_PEN", id = DerivedStatId.MagicPenetration, displayName = "Magic Penetration", shortName = "MPN", primarySources = new[] { "INT" }, formulaId = "magic_pen_v1", description = "Ignores enemy Magic Defense." },

            [DerivedStatId.PhysicalDefense] = new DerivedStatDef { statId = "PHYS_DEF", id = DerivedStatId.PhysicalDefense, displayName = "Physical Defense", shortName = "DEF", primarySources = new[] { "VIT" }, formulaId = "phys_def_v1", description = "Mitigates incoming physical damage." },

            [DerivedStatId.MagicDefense] = new DerivedStatDef { statId = "MAG_DEF", id = DerivedStatId.MagicDefense, displayName = "Magic Defense", shortName = "MDF", primarySources = new[] { "WIS" }, formulaId = "magic_def_v1", description = "Mitigates incoming magical damage." },

            [DerivedStatId.Evasion] = new DerivedStatDef { statId = "EVASION", id = DerivedStatId.Evasion, displayName = "Evasion", shortName = "EVA", primarySources = new[] { "DEX" }, formulaId = "evasion_v1", description = "Agility and reaction avoidance." },

            [DerivedStatId.BlockPower] = new DerivedStatDef { statId = "BLK_PWR", id = DerivedStatId.BlockPower, displayName = "Block Power", shortName = "BLK", primarySources = new[] { "STR" }, formulaId = "block_power_v1", description = "Damage reduction when guarding or blocking." },

            [DerivedStatId.StatusResistance] = new DerivedStatDef { statId = "STAT_RES", id = DerivedStatId.StatusResistance, displayName = "Status Resistance", shortName = "SRS", primarySources = new[] { "VIT", "WIS" }, formulaId = "status_res_v1", description = "Chance to resist harmful status effects." },

            [DerivedStatId.StaggerResistance] = new DerivedStatDef { statId = "STG_RES", id = DerivedStatId.StaggerResistance, displayName = "Stagger Resistance", shortName = "STRS", primarySources = new[] { "VIT" }, formulaId = "stagger_res_v1", description = "Reduces incoming stagger and break damage." },

            [DerivedStatId.Initiative] = new DerivedStatDef { statId = "INIT", id = DerivedStatId.Initiative, displayName = "Initiative", shortName = "INI", primarySources = new[] { "DEX" }, formulaId = "initiative_v1", description = "Determines individual turn order each round." },

            [DerivedStatId.ActionSpeed] = new DerivedStatDef { statId = "ACT_SPD", id = DerivedStatId.ActionSpeed, displayName = "Action Speed", shortName = "SPD", primarySources = new[] { "DEX" }, formulaId = "action_speed_v1", description = "Tempo rating for speed-scaling skills." },

            [DerivedStatId.QteTimingBonus] = new DerivedStatDef { statId = "QTE_TIME", id = DerivedStatId.QteTimingBonus, displayName = "QTE Timing Bonus", shortName = "QTE", primarySources = new[] { "DEX" }, formulaId = "qte_timing_v1", description = "Small bonus to reaction timing windows." },

            [DerivedStatId.QteStability] = new DerivedStatDef { statId = "QTE_STAB", id = DerivedStatId.QteStability, displayName = "QTE Stability", shortName = "QST", primarySources = new[] { "VIT" }, formulaId = "qte_stability_v1", description = "Reduces penalties on partial/failed reactions." },

            [DerivedStatId.CounterPower] = new DerivedStatDef { statId = "CNT_PWR", id = DerivedStatId.CounterPower, displayName = "Counter Power", shortName = "CNT", primarySources = new[] { "STR", "DEX" }, formulaId = "counter_power_v1", description = "Effectiveness of physical counterattacks." },

            [DerivedStatId.PhysicalBreakPower] = new DerivedStatDef { statId = "P_BREAK", id = DerivedStatId.PhysicalBreakPower, displayName = "Physical Break", shortName = "PBR", primarySources = new[] { "STR" }, formulaId = "phys_break_v1", description = "Break Gauge damage dealt by physical hits." },

            [DerivedStatId.MagicBreakPower] = new DerivedStatDef { statId = "M_BREAK", id = DerivedStatId.MagicBreakPower, displayName = "Magic Break", shortName = "MBR", primarySources = new[] { "INT" }, formulaId = "magic_break_v1", description = "Break Gauge damage dealt by magic hits." },

        };



        // ---- 4. Playable Races (Section 4 of Character Creation System: 7 Playable Races) ----

        public static readonly string[] PlayableRaceIds =

        {

            "human", "elf", "dwarf", "halfling", "dragonborn", "liichtian", "merfolk"

        };



        public static readonly Dictionary<string, RaceDef> Races = Index(new[]

        {

            new RaceDef

            {

                id = "human", name = "Human", isPlayable = true,

                description = "Adaptable survivors of the surface kingdoms.",

                abilityModifiers = new AbilityScores(1, 0, 1, 0, 0, 1),

                racialFeat = new RacialFeatDef { id = "determination", name = "Determination", description = "Steadfast resolve in dire moments." },

                racialUtility = new RacialUtilityDef { id = "versatile_adaptation", name = "Versatile Adaptation", description = "Adaptable across varied expedition challenges.", utilityTag = "adaptable" }

            },

            new RaceDef

            {

                id = "elf", name = "Elf", isPlayable = true,

                description = "Swift hunters attuned to the twilight beneath the world.",

                abilityModifiers = new AbilityScores(0, 2, 0, 1, 0, 0),

                racialFeat = new RacialFeatDef { id = "fey_step", name = "Fey Step", description = "Graceful repositioning that evades danger." },

                racialUtility = new RacialUtilityDef { id = "keen_senses", name = "Keen Senses", description = "Heightened perception in dark corridors.", utilityTag = "perception" }

            },

            new RaceDef

            {

                id = "dwarf", name = "Dwarf", isPlayable = true,

                description = "Stout delve-folk forged for endurance in the deep.",

                abilityModifiers = new AbilityScores(1, 0, 2, 0, 0, 0),

                racialFeat = new RacialFeatDef { id = "iron_resolve", name = "Iron Resolve", description = "Unshakeable resilience against stagger and blows." },

                racialUtility = new RacialUtilityDef { id = "stonecunning", name = "Stonecunning", description = "Instinctive knowledge of subterranean structures.", utilityTag = "delver" }

            },

            new RaceDef

            {

                id = "halfling", name = "Halfling", isPlayable = true,

                description = "Nimble and fortunate wanderers who slip past peril.",

                abilityModifiers = new AbilityScores(0, 2, 0, 0, 0, 1),

                racialFeat = new RacialFeatDef

                {

                    id = "second_chance", name = "Second Chance",

                    canRetryFailedQte = true, maxQteRetriesPerBattle = 1,

                    description = "Allows retrying a failed QTE once under specific conditions without guaranteeing success."

                },

                racialUtility = new RacialUtilityDef { id = "nimble_passage", name = "Nimble Passage", description = "Slips past larger foes and hazards.", utilityTag = "stealth" }

            },

            new RaceDef

            {

                id = "dragonborn", name = "Dragonborn", isPlayable = true,

                description = "Proud warriors carrying the burning blood of ancient dragons.",

                abilityModifiers = new AbilityScores(2, 0, 0, 0, 0, 1),

                racialFeat = new RacialFeatDef

                {

                    id = "draconic_overdrive", name = "Draconic Overdrive",

                    overdrivePowerBonus = 0.25f, overdriveDurationTurns = 2, exhaustDurationTurns = 1,

                    description = "Temporarily increases power, followed by the Exhausted condition."

                },

                racialUtility = new RacialUtilityDef { id = "draconic_presence", name = "Draconic Presence", description = "Commanding aura born of ancient dragonblood.", utilityTag = "intimidation" }

            },

            new RaceDef

            {

                id = "liichtian", name = "Liichtian", isPlayable = true,

                description = "Mortals of celestial heritage who carry astral light into the abyss.",

                abilityModifiers = new AbilityScores(0, 0, 0, 1, 1, 1),

                racialFeat = new RacialFeatDef { id = "lightbound", name = "Lightbound", description = "Channels celestial heritage to illuminate the dark." },

                racialUtility = new RacialUtilityDef { id = "celestial_guidance", name = "Celestial Guidance", description = "Attuned to astral signs and sacred wards.", utilityTag = "celestial" }

            },

            new RaceDef

            {

                id = "merfolk", name = "Merfolk", isPlayable = true,

                description = "Denizens of the sunken depths who command protective tides.",

                abilityModifiers = new AbilityScores(0, 1, 1, 0, 1, 0),

                racialFeat = new RacialFeatDef

                {

                    id = "oceans_embrace", name = "Ocean's Embrace",

                    allyShieldAmount = 15,

                    description = "Provides a protective aqueous shield effect to an ally."

                },

                racialUtility = new RacialUtilityDef { id = "tidal_grace", name = "Tidal Grace", description = "Fluid movement and calm restoration.", utilityTag = "aquatic" }

            },



            // Legacy preset aliases preserved for backward compatibility

            new RaceDef

            {

                id = "highborn", name = "Highborn", isPlayable = false,

                description = "Scions of ancient citadels steeped in arcane study.",

                abilityModifiers = new AbilityScores(0, 0, 0, 2, 1, 0),

                racialFeat = new RacialFeatDef { id = "arcane_blood", name = "Arcane Blood", description = "Innate magical resonance." },

                racialUtility = new RacialUtilityDef { id = "lorekeeper", name = "Lorekeeper", description = "Deep knowledge of ancient texts." }

            },

            new RaceDef

            {

                id = "sanctified", name = "Sanctified", isPlayable = false,

                description = "Mortals touched by fading embers of sacred light.",

                abilityModifiers = new AbilityScores(0, 0, 0, 0, 2, 1),

                racialFeat = new RacialFeatDef { id = "bright_soul", name = "Bright Soul", description = "Inner radiance against the dark." },

                racialUtility = new RacialUtilityDef { id = "serene_mind", name = "Serene Mind", description = "Calm focus under pressure." }

            },

            new RaceDef

            {

                id = "ironbound", name = "Ironbound", isPlayable = false,

                description = "Stout delve-folk forged for endurance in the deep.",

                abilityModifiers = new AbilityScores(1, 0, 2, 0, 0, 0),

                racialFeat = new RacialFeatDef { id = "stoneborn", name = "Stoneborn", description = "Enduring fortitude." },

                racialUtility = new RacialUtilityDef { id = "unyielding", name = "Unyielding", description = "Refuses to yield ground." }

            },

        }, r => r.id);



        // ---- 5. Playable Classes (Section 5 of Character Creation System: 9 Playable Classes) ----

        public static readonly string[] PlayableClassIds =

        {

            "fighter", "barbarian", "martial_artist", "rogue", "ranger", "cleric", "wizard", "warlock", "druid"

        };



        public static readonly Dictionary<string, ClassDef> Classes = Index(new[]

        {

            // 1. Fighter

            new ClassDef

            {

                id = "fighter", name = "Fighter", role = "Defender", isPlayable = true,

                gameplayIdentity = "Weapon Techniques",

                starterPassiveId = "weapon_proficiency",

                branchNames = new[] { "Weapon Mastery", "Shield Defense", "Tactical Vanguard" },

                description = "Disciplined master of arms who excels at weapon techniques and frontline defense.",

                startingSkills = new[] { "shield_bash", "power_strike", "guard_stance" },

                activeSkillPool = new[]

                {

                    "shield_bash", "power_strike", "guard_stance",

                    "cleaving_strike", "shield_wall", "weapon_disarm",

                    "counterattack", "armor_break", "tactical_strike",

                    "whirlwind", "iron_fortress", "execution_strike",

                    "blade_tempest", "unbreakable", "final_verdict",

                    "fortify", "interpose"

                },

                passiveSkillPool = new[]

                {

                    "weapon_proficiency", "defensive_training",

                    "combat_discipline", "shield_mastery",

                    "tactical_awareness", "weapon_adaptation",

                    "iron_will", "counter_specialist",

                    "master_of_arms", "unyielding_defense",

                    "battle_focus"

                },

                skillUnlockProgression = new[]

                {

                    // Tier 1

                    new SkillUnlockNode { skillId = "defensive_training", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 1 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "cleaving_strike", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "power_strike" } },

                    new SkillUnlockNode { skillId = "shield_wall", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "guard_stance" } },

                    new SkillUnlockNode { skillId = "weapon_disarm", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2 },

                    new SkillUnlockNode { skillId = "combat_discipline", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "shield_mastery", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "counterattack", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "weapon_disarm" } },

                    new SkillUnlockNode { skillId = "armor_break", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "cleaving_strike" } },

                    new SkillUnlockNode { skillId = "tactical_strike", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "weapon_disarm" } },

                    new SkillUnlockNode { skillId = "tactical_awareness", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2 },

                    new SkillUnlockNode { skillId = "weapon_adaptation", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "whirlwind", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "armor_break" } },

                    new SkillUnlockNode { skillId = "iron_fortress", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "shield_wall" } },

                    new SkillUnlockNode { skillId = "execution_strike", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "tactical_strike" } },

                    new SkillUnlockNode { skillId = "iron_will", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "counter_specialist", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "blade_tempest", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "whirlwind" } },

                    new SkillUnlockNode { skillId = "unbreakable", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, maxRank = 1, prerequisites = new[] { "iron_fortress" } },

                    new SkillUnlockNode { skillId = "final_verdict", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "execution_strike" } },

                    new SkillUnlockNode { skillId = "master_of_arms", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "unyielding_defense", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1 }

                },

                mainHandWeapons = new[] { WeaponType.Sword, WeaponType.Axe, WeaponType.Mace, WeaponType.Spear },

                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.Buckler },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "iron_broadsword"),

                    Eq(EquipmentSlot.SecondHand, "iron_shield"),

                    Eq(EquipmentSlot.HeadArmor, "iron_helm"),

                    Eq(EquipmentSlot.BodyArmor, "chain_hauberk"),

                    Eq(EquipmentSlot.LegArmor, "chain_greaves"),

                    Eq(EquipmentSlot.FootArmor, "iron_sabatons"),

                    Eq(EquipmentSlot.Accessory1, "vanguard_ring"),

                    Eq(EquipmentSlot.Accessory2, "soldier_medal")

                },

                weaponProficiencies = new[] { "Sword", "Greatsword", "Axe", "Mace", "Spear" },

                armorProficiencies = new[] { "Heavy", "Medium", "Shield" },

                classMechanics = new[] { "Weapon Techniques" },

                affinities = new Affinities { resist = new[] { Element.Dark } },

                sprite = Art.Fighter,

                baseHp = 95, hpGrowth = 10f, levelHpGrowth = 12,

                baseMp = 15, mpGrowth = 5f, levelMpGrowth = 3,

                baseWeaponPower = 8, weaponScaling = 2f,

                baseRangedPower = 7, rangedScaling = 1.5f,

                baseSpellPower = 6, spellScaling = 2f,

                baseHealing = 6, healingScaling = 2f,

                baseSupportPower = 8, supportScaling = 2f,

                basePhysicalDefense = 7, physicalDefenseScaling = 2f,

                baseMagicDefense = 5, magicDefenseScaling = 2f,

                baseInitiative = 6,

                baseBreakPower = 14, breakPowerScaling = 3f

            },

            // 2. Barbarian

            new ClassDef

            {

                id = "barbarian", name = "Barbarian", role = "Bruiser", isPlayable = true,

                gameplayIdentity = "Berserk State",

                starterPassiveId = "savage_instinct",

                branchNames = new[] { "Brutal Might", "Berserker Rage", "Juggernaut" },

                description = "Ferocious warrior who grows deadlier as wounds mount.",

                startingSkills = new[] { "savage_strike", "berserk", "intimidating_roar" },

                activeSkillPool = new[]

                {

                    "savage_strike", "berserk", "intimidating_roar", "crushing_blow", "reckless_swing", "blood_rush", "rampage", "blood_frenzy", "brutal_cleave", "devastating_smash", "death_rush", "barbarian_war_cry", "barbarian_cataclysm", "wrath_incarnate", "primal_carnage"

                },

                passiveSkillPool = new[]

                {

                    "savage_instinct", "bloodthirst", "unrelenting_fury", "pain_tolerance", "predators_instinct", "berserkers_momentum", "unstoppable_rage", "blood_pact", "avatar_of_destruction", "endless_fury"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "bloodthirst", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 1 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "crushing_blow", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "savage_strike" } },

                    new SkillUnlockNode { skillId = "reckless_swing", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "berserk" } },

                    new SkillUnlockNode { skillId = "blood_rush", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "intimidating_roar" } },

                    new SkillUnlockNode { skillId = "unrelenting_fury", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "pain_tolerance", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "rampage", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "crushing_blow" } },

                    new SkillUnlockNode { skillId = "blood_frenzy", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "reckless_swing" } },

                    new SkillUnlockNode { skillId = "brutal_cleave", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "blood_rush" } },

                    new SkillUnlockNode { skillId = "predators_instinct", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "berserkers_momentum", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "devastating_smash", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "rampage" } },

                    new SkillUnlockNode { skillId = "death_rush", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "blood_frenzy" } },

                    new SkillUnlockNode { skillId = "barbarian_war_cry", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "brutal_cleave" } },

                    new SkillUnlockNode { skillId = "unstoppable_rage", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "blood_pact", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "barbarian_cataclysm", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "devastating_smash" } },

                    new SkillUnlockNode { skillId = "wrath_incarnate", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, maxRank = 1, prerequisites = new[] { "death_rush" } },

                    new SkillUnlockNode { skillId = "primal_carnage", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "barbarian_war_cry" } },

                    new SkillUnlockNode { skillId = "avatar_of_destruction", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "endless_fury", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1 },

                },

                mainHandWeapons = new[] { WeaponType.Greataxe, WeaponType.Greatsword, WeaponType.Warhammer, WeaponType.Axe },

                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.Buckler, WeaponType.Axe },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "battle_greataxe"),

                    Eq(EquipmentSlot.SecondHand, "iron_buckler"),

                    Eq(EquipmentSlot.HeadArmor, "horned_helm"),

                    Eq(EquipmentSlot.BodyArmor, "hide_harness"),

                    Eq(EquipmentSlot.LegArmor, "fur_leggings"),

                    Eq(EquipmentSlot.FootArmor, "strider_boots"),

                    Eq(EquipmentSlot.Accessory1, "fang_talisman"),

                    Eq(EquipmentSlot.Accessory2, "vanguard_ring")

                },

                weaponProficiencies = new[] { "Greataxe", "Axe", "Greatsword", "Mace" },

                armorProficiencies = new[] { "Light", "Medium" },

                classMechanics = new[] { "Berserk State" },

                sprite = Art.Barbarian,

                baseHp = 105, hpGrowth = 12f, levelHpGrowth = 14,

                baseMp = 12, mpGrowth = 4f, levelMpGrowth = 2,

                baseWeaponPower = 11, weaponScaling = 2.5f,

                basePhysicalDefense = 6, physicalDefenseScaling = 2f,

                baseMagicDefense = 4, magicDefenseScaling = 1.5f,

                baseInitiative = 8,

                baseBreakPower = 16, breakPowerScaling = 3.5f

            },

            // 3. Martial Artist

            new ClassDef

            {

                id = "martial_artist", name = "Martial Artist", role = "Brawler", isPlayable = true,

                gameplayIdentity = "Combo Flow",

                starterPassiveId = "martial_discipline",

                branchNames = new[] { "Flowing Strikes", "Iron Body", "Dragon Finishing" },

                description = "Unarmed combatant whose flowing strikes shatter enemy guards.",

                startingSkills = new[] { "quick_jab", "focused_breathing", "rising_kick" },

                activeSkillPool = new[]

                {

                    "quick_jab", "focused_breathing", "rising_kick", "flowing_combo", "palm_strike", "sweep_kick", "spinning_kick", "counterstrike", "dragon_fist", "hundred_fists", "perfect_counter", "flying_knee", "ascendant_strike", "final_technique", "dragons_fury"

                },

                passiveSkillPool = new[]

                {

                    "martial_discipline", "combat_rhythm", "combo_mastery", "agile_defense", "inner_focus", "pressure_point", "unbroken_flow", "ki_control", "grandmaster", "limitless_potential"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "combat_rhythm", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 0 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "flowing_combo", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "quick_jab" } },

                    new SkillUnlockNode { skillId = "palm_strike", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "focused_breathing" } },

                    new SkillUnlockNode { skillId = "sweep_kick", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "rising_kick" } },

                    new SkillUnlockNode { skillId = "combo_mastery", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "agile_defense", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "spinning_kick", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "flowing_combo" } },

                    new SkillUnlockNode { skillId = "counterstrike", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "palm_strike" } },

                    new SkillUnlockNode { skillId = "dragon_fist", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "sweep_kick" } },

                    new SkillUnlockNode { skillId = "inner_focus", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "pressure_point", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "hundred_fists", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "spinning_kick" } },

                    new SkillUnlockNode { skillId = "perfect_counter", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "counterstrike" } },

                    new SkillUnlockNode { skillId = "flying_knee", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "dragon_fist" } },

                    new SkillUnlockNode { skillId = "unbroken_flow", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "ki_control", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "ascendant_strike", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "hundred_fists" } },

                    new SkillUnlockNode { skillId = "final_technique", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "perfect_counter" } },

                    new SkillUnlockNode { skillId = "dragons_fury", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "flying_knee" } },

                    new SkillUnlockNode { skillId = "grandmaster", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "limitless_potential", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2 },

                },

                mainHandWeapons = new[] { WeaponType.Gauntlet },

                secondHandWeapons = new[] { WeaponType.FistWeapon },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "iron_gauntlets"),

                    Eq(EquipmentSlot.SecondHand, "iron_knuckle"),

                    Eq(EquipmentSlot.HeadArmor, "monk_headband"),

                    Eq(EquipmentSlot.BodyArmor, "cloth_tunic"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "soft_boots"),

                    Eq(EquipmentSlot.Accessory1, "focus_band"),

                    Eq(EquipmentSlot.Accessory2, "swift_anklet")

                },

                weaponProficiencies = new[] { "Fist", "Staff" },

                armorProficiencies = new[] { "Cloth", "Light" },

                classMechanics = new[] { "Combo Flow" },

                sprite = Art.MartialArtist,

                baseHp = 82, hpGrowth = 9f, levelHpGrowth = 9,

                baseMp = 20, mpGrowth = 5f, levelMpGrowth = 4,

                baseWeaponPower = 10, weaponScaling = 2f,

                basePhysicalDefense = 5, physicalDefenseScaling = 2f,

                baseMagicDefense = 5, magicDefenseScaling = 2f,

                baseEvasion = 9, evasionScaling = 3f,

                baseInitiative = 11,

                baseBreakPower = 15, breakPowerScaling = 3.5f

            },

            // 4. Rogue

            new ClassDef

            {

                id = "rogue", name = "Rogue", role = "Striker", isPlayable = true,

                gameplayIdentity = "Exploit",

                starterPassiveId = "opportunist",

                branchNames = new[] { "Assassination", "Shadow & Trickery", "Debilitation" },

                description = "Acts first and often. Exploits enemy weaknesses for lethal criticals.",

                startingSkills = new[] { "quick_slash", "smoke_bomb", "backstab" },

                activeSkillPool = new[]

                {

                    "quick_slash", "smoke_bomb", "backstab", "feint", "shadow_strike", "poison_blade", "assassinate", "shadowstep", "weakpoint_strike", "death_mark", "vanishing_act", "phantom_dance", "thousand_cuts", "shadow_requiem", "final_ambush"

                },

                passiveSkillPool = new[]

                {

                    "opportunist", "evasive_instinct", "precision", "dirty_fighting", "lethal_focus", "exploit_weakness", "shadow_affinity", "executioner", "master_assassin", "deaths_embrace"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "evasive_instinct", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 1 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "feint", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "quick_slash" } },

                    new SkillUnlockNode { skillId = "shadow_strike", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "smoke_bomb" } },

                    new SkillUnlockNode { skillId = "poison_blade", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "backstab" } },

                    new SkillUnlockNode { skillId = "precision", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "dirty_fighting", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "assassinate", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "feint" } },

                    new SkillUnlockNode { skillId = "shadowstep", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "shadow_strike" } },

                    new SkillUnlockNode { skillId = "weakpoint_strike", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "poison_blade" } },

                    new SkillUnlockNode { skillId = "lethal_focus", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "exploit_weakness", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "death_mark", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "assassinate" } },

                    new SkillUnlockNode { skillId = "vanishing_act", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "shadowstep" } },

                    new SkillUnlockNode { skillId = "phantom_dance", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "weakpoint_strike" } },

                    new SkillUnlockNode { skillId = "shadow_affinity", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "executioner", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "thousand_cuts", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "death_mark" } },

                    new SkillUnlockNode { skillId = "shadow_requiem", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "vanishing_act" } },

                    new SkillUnlockNode { skillId = "final_ambush", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "phantom_dance" } },

                    new SkillUnlockNode { skillId = "master_assassin", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "deaths_embrace", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2 },

                },

                mainHandWeapons = new[] { WeaponType.Rapier },

                secondHandWeapons = new[] { WeaponType.Dagger, WeaponType.Buckler },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "duelist_rapier"),

                    Eq(EquipmentSlot.SecondHand, "iron_dagger"),

                    Eq(EquipmentSlot.HeadArmor, "shadow_hood"),

                    Eq(EquipmentSlot.BodyArmor, "leather_jerkin"),

                    Eq(EquipmentSlot.LegArmor, "leather_greaves"),

                    Eq(EquipmentSlot.FootArmor, "soft_boots"),

                    Eq(EquipmentSlot.Accessory1, "thief_ring"),

                    Eq(EquipmentSlot.Accessory2, "swift_anklet")

                },

                weaponProficiencies = new[] { "Dagger", "Shortblade", "Bow", "Crossbow" },

                armorProficiencies = new[] { "Light" },

                classMechanics = new[] { "Exploit" },

                affinities = Affinities.None,

                sprite = Art.Rogue,

                baseHp = 75, hpGrowth = 10f, levelHpGrowth = 8,

                baseMp = 20, mpGrowth = 5f, levelMpGrowth = 4,

                baseWeaponPower = 13, weaponScaling = 2f,

                baseRangedPower = 11, rangedScaling = 2f,

                baseSpellPower = 5, spellScaling = 2f,

                baseHealing = 5, healingScaling = 2f,

                baseSupportPower = 7, supportScaling = 2f,

                basePhysicalDefense = 5, physicalDefenseScaling = 2f,

                baseMagicDefense = 4, magicDefenseScaling = 2f,

                baseCritChance = 8, critChanceScaling = 3f,

                baseEvasion = 8, evasionScaling = 3f,

                baseInitiative = 11,

                baseBreakPower = 11, breakPowerScaling = 2.5f

            },

            // 5. Ranger

            new ClassDef

            {

                id = "ranger", name = "Ranger", role = "Marksman", isPlayable = true,

                gameplayIdentity = "Hunt Mark",

                starterPassiveId = "hunters_instinct",

                branchNames = new[] { "Sharpshooting", "Survival & Traps", "Volley & Archery" },

                description = "Deadly ranged marksman who marks quarry and strikes from afar.",

                startingSkills = new[] { "quick_shot", "hunter_mark", "evasive_shot" },

                activeSkillPool = new[]

                {

                    "quick_shot", "hunter_mark", "evasive_shot", "piercing_arrow", "crippling_shot", "volley", "snipe", "trap_setup", "explosive_arrow", "shadow_shot", "pinning_shot", "rain_of_arrows", "deadeye", "perfect_hunt", "natures_wrath_shot"

                },

                passiveSkillPool = new[]

                {

                    "hunters_instinct", "keen_eye", "marksmanship", "predators_focus", "deadly_aim", "improved_tracking", "eagle_eye", "tactical_hunter", "master_hunter", "apex_predator"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "keen_eye", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 0 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "piercing_arrow", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "hunter_mark" } },

                    new SkillUnlockNode { skillId = "crippling_shot", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "evasive_shot" } },

                    new SkillUnlockNode { skillId = "volley", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "quick_shot" } },

                    new SkillUnlockNode { skillId = "marksmanship", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "predators_focus", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "snipe", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "piercing_arrow" } },

                    new SkillUnlockNode { skillId = "trap_setup", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "crippling_shot" } },

                    new SkillUnlockNode { skillId = "explosive_arrow", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "volley" } },

                    new SkillUnlockNode { skillId = "deadly_aim", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "improved_tracking", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "shadow_shot", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "snipe" } },

                    new SkillUnlockNode { skillId = "pinning_shot", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "trap_setup" } },

                    new SkillUnlockNode { skillId = "rain_of_arrows", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "explosive_arrow" } },

                    new SkillUnlockNode { skillId = "eagle_eye", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "tactical_hunter", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "deadeye", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "shadow_shot" } },

                    new SkillUnlockNode { skillId = "perfect_hunt", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "pinning_shot" } },

                    new SkillUnlockNode { skillId = "natures_wrath_shot", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "rain_of_arrows" } },

                    new SkillUnlockNode { skillId = "master_hunter", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "apex_predator", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1 },

                },

                mainHandWeapons = new[] { WeaponType.Bow, WeaponType.Crossbow },

                secondHandWeapons = new[] { WeaponType.Quiver },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "hunting_bow"),

                    Eq(EquipmentSlot.SecondHand, "fletched_quiver"),

                    Eq(EquipmentSlot.HeadArmor, "scout_cap"),

                    Eq(EquipmentSlot.BodyArmor, "leather_jerkin"),

                    Eq(EquipmentSlot.LegArmor, "leather_greaves"),

                    Eq(EquipmentSlot.FootArmor, "strider_boots"),

                    Eq(EquipmentSlot.Accessory1, "hawk_feather"),

                    Eq(EquipmentSlot.Accessory2, "swift_anklet")

                },

                weaponProficiencies = new[] { "Bow", "Crossbow", "Spear", "Dagger" },

                armorProficiencies = new[] { "Light", "Medium" },

                classMechanics = new[] { "Hunt Mark" },

                sprite = Art.Ranger,

                baseHp = 78, hpGrowth = 9f, levelHpGrowth = 8,

                baseMp = 22, mpGrowth = 5f, levelMpGrowth = 4,

                baseWeaponPower = 9, weaponScaling = 2f,

                baseRangedPower = 13, rangedScaling = 2.5f,

                basePhysicalDefense = 5, physicalDefenseScaling = 2f,

                baseMagicDefense = 5, magicDefenseScaling = 2f,

                baseCritChance = 7, critChanceScaling = 2.5f,

                baseEvasion = 7, evasionScaling = 2.5f,

                baseInitiative = 10,

                baseBreakPower = 11, breakPowerScaling = 2.5f

            },

            // 6. Cleric

            new ClassDef

            {

                id = "cleric", name = "Cleric", role = "Support", isPlayable = true,

                gameplayIdentity = "Divine Protection",

                starterPassiveId = "divine_grace",

                branchNames = new[] { "Sacred Restoration", "Divine Aegis", "Holy Smite" },

                description = "Sacred conduit who mends wounds and wards the party against darkness.",

                startingSkills = new[] { "heal", "blessing", "holy_light" },

                activeSkillPool = new[]

                {

                    "heal", "blessing", "holy_light", "mend", "divine_shield", "purify", "prayer_of_healing_all", "sanctuary", "holy_smite", "resurrection", "guardian_angel", "judgment", "miracle", "divine_intervention", "heavens_wrath"

                },

                passiveSkillPool = new[]

                {

                    "divine_grace", "sacred_vitality", "healing_hands", "protective_faith", "divine_aegis", "spiritual_focus", "blessed_recovery", "unwavering_faith", "saints_resolve", "eternal_blessing"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "sacred_vitality", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 0 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "mend", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "heal" } },

                    new SkillUnlockNode { skillId = "divine_shield", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "blessing" } },

                    new SkillUnlockNode { skillId = "purify", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "heal" } },

                    new SkillUnlockNode { skillId = "healing_hands", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "protective_faith", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "prayer_of_healing_all", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "mend" } },

                    new SkillUnlockNode { skillId = "sanctuary", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "divine_shield" } },

                    new SkillUnlockNode { skillId = "holy_smite", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "holy_light" } },

                    new SkillUnlockNode { skillId = "divine_aegis", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "spiritual_focus", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "resurrection", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "prayer_of_healing_all" } },

                    new SkillUnlockNode { skillId = "guardian_angel", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "sanctuary" } },

                    new SkillUnlockNode { skillId = "judgment", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "holy_smite" } },

                    new SkillUnlockNode { skillId = "blessed_recovery", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "unwavering_faith", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "miracle", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "resurrection" } },

                    new SkillUnlockNode { skillId = "divine_intervention", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "guardian_angel" } },

                    new SkillUnlockNode { skillId = "heavens_wrath", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "judgment" } },

                    new SkillUnlockNode { skillId = "saints_resolve", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "eternal_blessing", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                },

                mainHandWeapons = new[] { WeaponType.Mace },

                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.HolySymbol },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "sanctified_mace"),

                    Eq(EquipmentSlot.SecondHand, "silver_holy_symbol"),

                    Eq(EquipmentSlot.HeadArmor, "acolyte_circlet"),

                    Eq(EquipmentSlot.BodyArmor, "linen_vestments"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "pilgrim_sandals"),

                    Eq(EquipmentSlot.Accessory1, "prayer_rosary"),

                    Eq(EquipmentSlot.Accessory2, "soldier_medal")

                },

                weaponProficiencies = new[] { "Mace", "Staff", "Rod" },

                armorProficiencies = new[] { "Cloth", "Light", "Medium", "Shield" },

                classMechanics = new[] { "Divine Protection" },

                affinities = new Affinities { resist = new[] { Element.Dark } },

                sprite = Art.Healer,

                baseHp = 75, hpGrowth = 10f, levelHpGrowth = 8,

                baseMp = 28, mpGrowth = 10f, levelMpGrowth = 7,

                baseWeaponPower = 8, weaponScaling = 2f,

                baseSpellPower = 10, spellScaling = 2.5f,

                baseHealing = 10, healingScaling = 2.5f,

                baseSupportPower = 10, supportScaling = 2.5f,

                basePhysicalDefense = 5, physicalDefenseScaling = 2f,

                baseMagicDefense = 7, magicDefenseScaling = 2.5f,

                baseInitiative = 9,

                baseBreakPower = 10, breakPowerScaling = 2f

            },

            // 7. Wizard

            new ClassDef

            {

                id = "wizard", name = "Wizard", role = "Caster", isPlayable = true,

                gameplayIdentity = "Spell Specialization",

                starterPassiveId = "arcane_affinity",

                branchNames = new[] { "Pyromancy & Storm", "Cryomancy & Control", "High Arcanum" },

                description = "Master of arcane incantations who exploits elemental and runic weaknesses.",

                startingSkills = new[] { "firebolt", "frost_nova", "arcane_bolt" },

                activeSkillPool = new[]

                {

                    "firebolt", "frost_nova", "arcane_bolt", "lightning_strike", "frost_shard", "arcane_burst", "meteor", "blizzard", "chain_lightning", "inferno", "absolute_zero", "arcane_overload", "cataclysm_spell", "apocalypse_spell", "grand_arcanum"

                },

                passiveSkillPool = new[]

                {

                    "arcane_affinity", "elemental_knowledge", "spell_mastery", "mana_efficiency", "elemental_focus", "arcane_precision", "spell_amplification", "magical_resilience", "archmage", "infinite_potential"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "elemental_knowledge", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 0 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "lightning_strike", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "firebolt" } },

                    new SkillUnlockNode { skillId = "frost_shard", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "frost_nova" } },

                    new SkillUnlockNode { skillId = "arcane_burst", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "arcane_bolt" } },

                    new SkillUnlockNode { skillId = "spell_mastery", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "mana_efficiency", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "meteor", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "lightning_strike" } },

                    new SkillUnlockNode { skillId = "blizzard", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "frost_shard" } },

                    new SkillUnlockNode { skillId = "chain_lightning", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "lightning_strike" } },

                    new SkillUnlockNode { skillId = "elemental_focus", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "arcane_precision", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "inferno", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "meteor" } },

                    new SkillUnlockNode { skillId = "absolute_zero", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "blizzard" } },

                    new SkillUnlockNode { skillId = "arcane_overload", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "arcane_burst" } },

                    new SkillUnlockNode { skillId = "spell_amplification", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "magical_resilience", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "cataclysm_spell", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "inferno" } },

                    new SkillUnlockNode { skillId = "apocalypse_spell", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "absolute_zero" } },

                    new SkillUnlockNode { skillId = "grand_arcanum", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "arcane_overload" } },

                    new SkillUnlockNode { skillId = "archmage", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2 },

                    new SkillUnlockNode { skillId = "infinite_potential", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2 },

                },

                mainHandWeapons = new[] { WeaponType.Wand },

                secondHandWeapons = new[] { WeaponType.Orb },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "apprentice_wand"),

                    Eq(EquipmentSlot.SecondHand, "arcane_orb"),

                    Eq(EquipmentSlot.HeadArmor, "scholar_hood"),

                    Eq(EquipmentSlot.BodyArmor, "cloth_tunic"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "pilgrim_sandals"),

                    Eq(EquipmentSlot.Accessory1, "mana_crystal"),

                    Eq(EquipmentSlot.Accessory2, "focus_band")

                },

                weaponProficiencies = new[] { "Staff", "Wand", "Rod" },

                armorProficiencies = new[] { "Cloth" },

                classMechanics = new[] { "Spell Specialization" },

                affinities = new Affinities { weak = new[] { Element.Dark }, resist = new[] { Element.Arcane } },

                sprite = Art.Mage,

                baseHp = 60, hpGrowth = 8f, levelHpGrowth = 6,

                baseMp = 32, mpGrowth = 10f, levelMpGrowth = 8,

                baseWeaponPower = 7, weaponScaling = 1.5f,

                baseSpellPower = 12, spellScaling = 3f,

                baseHealing = 6, healingScaling = 2f,

                baseSupportPower = 8, supportScaling = 2f,

                basePhysicalDefense = 4, physicalDefenseScaling = 1.5f,

                baseMagicDefense = 7, magicDefenseScaling = 2.5f,

                baseInitiative = 9,

                baseBreakPower = 12, breakPowerScaling = 3f

            },

            // 8. Warlock

            new ClassDef

            {

                id = "warlock", name = "Warlock", role = "Occultist", isPlayable = true,

                gameplayIdentity = "Pact Effects",

                starterPassiveId = "pact_affinity",

                branchNames = new[] { "Affliction & Curses", "Soul Magic", "Dark Eldritch" },

                description = "Wielder of forbidden pacts who channels dark and eldritch power.",

                startingSkills = new[] { "shadow_bolt", "curse_of_weakness", "corruption" },

                activeSkillPool = new[]

                {

                    "shadow_bolt", "curse_of_weakness", "corruption", "curse_of_vulnerability", "soul_drain", "shadow_burst", "curse_of_agony", "soul_harvest", "hex", "curse_of_doom", "soul_collapse", "nightfall", "absolute_damnation", "soul_annihilation", "apocalypse_curse"

                },

                passiveSkillPool = new[]

                {

                    "pact_affinity", "lingering_curse", "curse_mastery", "dark_knowledge", "malicious_intent", "dark_ritual", "eternal_affliction", "forbidden_power", "grand_curse_master", "abyssal_dominion"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "lingering_curse", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 0 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "curse_of_vulnerability", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "curse_of_weakness" } },

                    new SkillUnlockNode { skillId = "soul_drain", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "corruption" } },

                    new SkillUnlockNode { skillId = "shadow_burst", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "shadow_bolt" } },

                    new SkillUnlockNode { skillId = "curse_mastery", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "dark_knowledge", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "curse_of_agony", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "curse_of_vulnerability" } },

                    new SkillUnlockNode { skillId = "soul_harvest", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "soul_drain" } },

                    new SkillUnlockNode { skillId = "hex", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "shadow_burst" } },

                    new SkillUnlockNode { skillId = "malicious_intent", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "dark_ritual", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "curse_of_doom", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "curse_of_agony" } },

                    new SkillUnlockNode { skillId = "soul_collapse", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "soul_harvest" } },

                    new SkillUnlockNode { skillId = "nightfall", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "hex" } },

                    new SkillUnlockNode { skillId = "eternal_affliction", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "forbidden_power", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "absolute_damnation", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "curse_of_doom" } },

                    new SkillUnlockNode { skillId = "soul_annihilation", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "soul_collapse" } },

                    new SkillUnlockNode { skillId = "apocalypse_curse", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "nightfall" } },

                    new SkillUnlockNode { skillId = "grand_curse_master", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "abyssal_dominion", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1 },

                },

                mainHandWeapons = new[] { WeaponType.RitualBlade },

                secondHandWeapons = new[] { WeaponType.Grimoire },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "ritual_dagger"),

                    Eq(EquipmentSlot.SecondHand, "pact_grimoire"),

                    Eq(EquipmentSlot.HeadArmor, "cowl_of_whispers"),

                    Eq(EquipmentSlot.BodyArmor, "darkweave_robe"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "pilgrim_sandals"),

                    Eq(EquipmentSlot.Accessory1, "pact_seal"),

                    Eq(EquipmentSlot.Accessory2, "mana_crystal")

                },

                weaponProficiencies = new[] { "Rod", "Wand", "Staff", "Dagger" },

                armorProficiencies = new[] { "Cloth", "Light" },

                classMechanics = new[] { "Pact Effects" },

                affinities = new Affinities { resist = new[] { Element.Dark } },

                sprite = Art.Warlock,

                baseHp = 68, hpGrowth = 9f, levelHpGrowth = 7,

                baseMp = 28, mpGrowth = 9f, levelMpGrowth = 7,

                baseWeaponPower = 8, weaponScaling = 2f,

                baseSpellPower = 11, spellScaling = 3f,

                baseSupportPower = 9, supportScaling = 2.5f,

                basePhysicalDefense = 4, physicalDefenseScaling = 2f,

                baseMagicDefense = 6, magicDefenseScaling = 2.5f,

                baseInitiative = 9,

                baseBreakPower = 12, breakPowerScaling = 3f

            },

            // 9. Druid

            new ClassDef

            {

                id = "druid", name = "Druid", role = "Warden", isPlayable = true,

                gameplayIdentity = "Nature Forms",

                starterPassiveId = "natures_attunement",

                branchNames = new[] { "Verdant Growth", "Bramble & Thorns", "Primal Control" },

                description = "Primal guardian who weaves nature magic and restorative vitality.",

                startingSkills = new[] { "thorn_shot", "entangling_roots", "natures_embrace" },

                activeSkillPool = new[]

                {

                    "natures_embrace", "thorn_shot", "entangling_roots", "healing_bloom", "vine_lash", "barkskin", "regrowth", "natures_wrath", "overgrowth", "ancient_sanctuary", "thorn_prison", "wrath_of_the_forest", "world_tree_blessing", "primal_cataclysm", "natures_dominion"

                },

                passiveSkillPool = new[]

                {

                    "natures_attunement", "natural_recovery", "thorn_mastery", "deep_roots", "flourishing_life", "wild_resilience", "ancient_knowledge", "natures_protection", "master_of_the_wild", "eternal_bloom"

                },

                skillUnlockProgression = new[]

                {

                    new SkillUnlockNode { skillId = "natural_recovery", isPassive = true, tier = 1, requiredLevel = 1, skillPointCost = 1, branchIndex = 0 },



                    // Tier 2 (Level 4)

                    new SkillUnlockNode { skillId = "healing_bloom", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "natures_embrace" } },

                    new SkillUnlockNode { skillId = "vine_lash", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "thorn_shot" } },

                    new SkillUnlockNode { skillId = "barkskin", tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "natures_embrace" } },

                    new SkillUnlockNode { skillId = "thorn_mastery", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 1 },

                    new SkillUnlockNode { skillId = "deep_roots", isPassive = true, tier = 2, requiredLevel = 4, skillPointCost = 1, branchIndex = 2 },



                    // Tier 3 (Level 7)

                    new SkillUnlockNode { skillId = "regrowth", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "healing_bloom" } },

                    new SkillUnlockNode { skillId = "natures_wrath", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "vine_lash" } },

                    new SkillUnlockNode { skillId = "overgrowth", tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "entangling_roots" } },

                    new SkillUnlockNode { skillId = "flourishing_life", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "wild_resilience", isPassive = true, tier = 3, requiredLevel = 7, skillPointCost = 1, branchIndex = 1 },



                    // Tier 4 (Level 10)

                    new SkillUnlockNode { skillId = "ancient_sanctuary", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0, prerequisites = new[] { "regrowth" } },

                    new SkillUnlockNode { skillId = "thorn_prison", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1, prerequisites = new[] { "natures_wrath" } },

                    new SkillUnlockNode { skillId = "wrath_of_the_forest", tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 2, prerequisites = new[] { "overgrowth" } },

                    new SkillUnlockNode { skillId = "ancient_knowledge", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 0 },

                    new SkillUnlockNode { skillId = "natures_protection", isPassive = true, tier = 4, requiredLevel = 10, skillPointCost = 1, branchIndex = 1 },



                    // Tier 5 (Level 13)

                    new SkillUnlockNode { skillId = "world_tree_blessing", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0, prerequisites = new[] { "ancient_sanctuary" } },

                    new SkillUnlockNode { skillId = "primal_cataclysm", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 1, prerequisites = new[] { "thorn_prison" } },

                    new SkillUnlockNode { skillId = "natures_dominion", tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2, prerequisites = new[] { "wrath_of_the_forest" } },

                    new SkillUnlockNode { skillId = "master_of_the_wild", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 2 },

                    new SkillUnlockNode { skillId = "eternal_bloom", isPassive = true, tier = 5, requiredLevel = 13, skillPointCost = 2, branchIndex = 0 },

                },

                mainHandWeapons = new[] { WeaponType.Staff },

                secondHandWeapons = new[] { WeaponType.Totem },

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "oak_staff"),

                    Eq(EquipmentSlot.SecondHand, "verdant_totem"),

                    Eq(EquipmentSlot.HeadArmor, "antler_crown"),

                    Eq(EquipmentSlot.BodyArmor, "barkskin_vest"),

                    Eq(EquipmentSlot.LegArmor, "fur_leggings"),

                    Eq(EquipmentSlot.FootArmor, "strider_boots"),

                    Eq(EquipmentSlot.Accessory1, "amber_seed"),

                    Eq(EquipmentSlot.Accessory2, "prayer_rosary")

                },

                weaponProficiencies = new[] { "Staff", "Spear", "Mace" },

                armorProficiencies = new[] { "Cloth", "Light", "Medium" },

                classMechanics = new[] { "Nature Forms" },

                sprite = Art.Druid,

                baseHp = 80, hpGrowth = 10f, levelHpGrowth = 9,

                baseMp = 26, mpGrowth = 9f, levelMpGrowth = 6,

                baseWeaponPower = 8, weaponScaling = 2f,

                baseSpellPower = 10, spellScaling = 2.5f,

                baseHealing = 9, healingScaling = 2.5f,

                baseSupportPower = 9, supportScaling = 2f,

                basePhysicalDefense = 5, physicalDefenseScaling = 2f,

                baseMagicDefense = 6, magicDefenseScaling = 2.5f,

                baseInitiative = 9,

                baseBreakPower = 11, breakPowerScaling = 2.5f

            },



            // Legacy preset classes preserved for backward compatibility with existing tests

            new ClassDef

            {

                id = "knight", name = "Knight", role = "Defender", isPlayable = false,

                gameplayIdentity = "Weapon Techniques",

                starterPassiveId = "weapon_proficiency",

                description = "Oathbound shield. Outlasts anything and stands between the dark and the weak.",

                startingSkills = new[] { "shield_bash", "fortify", "interpose" },

                mainHandWeapons = new[] { WeaponType.Sword, WeaponType.Greatsword, WeaponType.Mace },

                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.Buckler },

                weaponProficiencies = new[] { "Sword", "Mace", "Heavy Blade" },

                armorProficiencies = new[] { "Heavy", "Medium", "Shield" },

                classMechanics = new[] { "Guardian Interpose", "Fortified Guard" },

                affinities = new Affinities { resist = new[] { Element.Dark } },

                sprite = Art.Knight,

                baseHp = 95, hpGrowth = 10f, levelHpGrowth = 12,

                baseMp = 15, mpGrowth = 5f, levelMpGrowth = 3,

                baseWeaponPower = 8, weaponScaling = 2f,

                baseRangedPower = 7, rangedScaling = 1.5f,

                baseSpellPower = 6, spellScaling = 2f,

                baseHealing = 6, healingScaling = 2f,

                baseSupportPower = 8, supportScaling = 2f,

                basePhysicalDefense = 7, physicalDefenseScaling = 2f,

                baseMagicDefense = 5, magicDefenseScaling = 2f,

                baseInitiative = 6,

                baseBreakPower = 14, breakPowerScaling = 3f

            },

            new ClassDef

            {

                id = "mage", name = "Mage", role = "Caster", isPlayable = false,

                gameplayIdentity = "Spell Specialization",

                starterPassiveId = "arcane_affinity",

                description = "Fragile, but every foe has an element it fears.",

                startingSkills = new[] { "firebolt", "frost_shard", "arcane_burst" },

                mainHandWeapons = new[] { WeaponType.Staff, WeaponType.Wand, WeaponType.Rod },

                secondHandWeapons = new[] { WeaponType.ArcaneTome, WeaponType.RelicSymbol },

                weaponProficiencies = new[] { "Staff", "Wand", "Tome" },

                armorProficiencies = new[] { "Cloth" },

                classMechanics = new[] { "Elemental Mastery", "Runic Incantations" },

                affinities = new Affinities { weak = new[] { Element.Dark }, resist = new[] { Element.Arcane } },

                sprite = Art.Mage,

                baseHp = 60, hpGrowth = 8f, levelHpGrowth = 6,

                baseMp = 30, mpGrowth = 10f, levelMpGrowth = 8,

                baseWeaponPower = 9, weaponScaling = 2f,

                baseRangedPower = 8, rangedScaling = 1.5f,

                baseSpellPower = 11, spellScaling = 3f,

                baseHealing = 7, healingScaling = 2f,

                baseSupportPower = 8, supportScaling = 2f,

                basePhysicalDefense = 4, physicalDefenseScaling = 2f,

                baseMagicDefense = 6, magicDefenseScaling = 2.5f,

                baseInitiative = 9,

                baseBreakPower = 12, breakPowerScaling = 3f

            },

            new ClassDef

            {

                id = "healer", name = "Healer", role = "Support", isPlayable = false,

                gameplayIdentity = "Divine Protection",

                starterPassiveId = "divine_grace",

                description = "Keeps the party standing through long fights.",

                startingSkills = new[] { "heal", "mend", "blessing" },

                mainHandWeapons = new[] { WeaponType.Mace },

                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.RelicSymbol },

                weaponProficiencies = new[] { "Mace", "Staff", "Chime" },

                armorProficiencies = new[] { "Cloth", "Light" },

                classMechanics = new[] { "Sacred Restoration", "Protective Litany" },

                affinities = new Affinities { resist = new[] { Element.Dark } },

                sprite = Art.Healer,

                baseHp = 70, hpGrowth = 10f, levelHpGrowth = 7,

                baseMp = 25, mpGrowth = 10f, levelMpGrowth = 7,

                baseWeaponPower = 9, weaponScaling = 2f,

                baseRangedPower = 8, rangedScaling = 1.5f,

                baseSpellPower = 11, spellScaling = 3f,

                baseHealing = 8, healingScaling = 2f,

                baseSupportPower = 10, supportScaling = 2.5f,

                basePhysicalDefense = 4, physicalDefenseScaling = 2f,

                baseMagicDefense = 6, magicDefenseScaling = 2.5f,

                baseInitiative = 10,

                baseBreakPower = 10, breakPowerScaling = 2f

            },

        }, c => c.id);



        // ---- 6. Backgrounds (Background System Section 3) ----

        public static readonly string[] PlayableBackgroundIds = new[]
        {
            "acolyte", "charlatan", "guard", "hermit", "farmer",
            "noble", "sage", "soldier", "wayfarer", "scribe"
        };

        public static readonly Dictionary<string, BackgroundDef> Backgrounds = Index(new[]
        {
            // 1. Acolyte — Faithful Devotion
            new BackgroundDef
            {
                id = "acolyte", name = "Acolyte",
                narrativeIdentity = "Spiritual devotion, protection, and recovery.",
                description = "Acolyte gains strength through faith and devotion. Its perk supports keeping the character and party alive.",
                perkName = "Faithful Devotion",
                perkDescription = "Empowers outgoing and incoming healing.",
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "Faithful Heart", perkDescription = "Healing dealt increases by 5%.", healingDealtBonus = 0.05f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Divine Resilience", perkDescription = "Healing dealt increases to 8%.", healingDealtBonus = 0.08f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Blessed Recovery", perkDescription = "Healing dealt increases to 10%. Healing received also increases by 5%.", healingDealtBonus = 0.10f, healingReceivedBonus = 0.05f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Sanctified Spirit", perkDescription = "Healing dealt increases to 12%, and healing received increases by 8%.", healingDealtBonus = 0.12f, healingReceivedBonus = 0.08f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Faith Unbroken", perkDescription = "Healing dealt increases to 15%, and healing received increases by 10%.", healingDealtBonus = 0.15f, healingReceivedBonus = 0.10f }
                }
            },

            // 2. Charlatan — Master of Deception
            new BackgroundDef
            {
                id = "charlatan", name = "Charlatan",
                narrativeIdentity = "Manipulation, deception, and exploiting enemy weaknesses.",
                description = "Turns deception into a combat advantage when enemies are impaired or affected by debuffs.",
                perkName = "Master of Deception",
                perkDescription = "Increases damage dealt to debuffed foes and strikes unprepared targets.",
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "False Promise", perkDescription = "Damage dealt to enemies affected by a debuff increases by 3%.", damageVsDebuffedBonus = 0.03f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Cunning Exploit", perkDescription = "The damage bonus increases to 5%.", damageVsDebuffedBonus = 0.05f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Deceptive Advantage", perkDescription = "The damage bonus increases to 7%.", damageVsDebuffedBonus = 0.07f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Master Manipulator", perkDescription = "The damage bonus increases to 10%.", damageVsDebuffedBonus = 0.10f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Perfect Deception", perkDescription = "The damage bonus increases to 12%. The character's first attack against an enemy that has not yet acted in the battle deals an additional 5% damage.", damageVsDebuffedBonus = 0.12f, firstAttackUnactedBonus = 0.05f }
                }
            },

            // 3. Guard — Guardian's Discipline
            new BackgroundDef
            {
                id = "guard", name = "Guard",
                narrativeIdentity = "Defense, vigilance, and protection.",
                description = "Experienced protector providing consistent physical defense for oneself and companions.",
                perkName = "Guardian's Discipline",
                perkDescription = "Reduces physical damage taken, with reinforced resistance at low health.",
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "Watchful Guard", perkDescription = "Physical Damage Taken is reduced by 3%.", physicalDamageTakenReduction = 0.03f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Steady Defense", perkDescription = "Physical Damage Taken is reduced by 5%.", physicalDamageTakenReduction = 0.05f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Defensive Training", perkDescription = "Physical Damage Taken is reduced by 7%.", physicalDamageTakenReduction = 0.07f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Guardian's Resolve", perkDescription = "Physical Damage Taken is reduced by 9%.", physicalDamageTakenReduction = 0.09f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Unyielding Guardian", perkDescription = "Physical Damage Taken is reduced by 10%. While HP is below 30%, Physical Damage Taken is reduced by an additional 5%.", physicalDamageTakenReduction = 0.10f, lowHpPhysicalDamageTakenReduction = 0.05f }
                }
            },

            // 4. Hermit — Solitary Wisdom
            new BackgroundDef
            {
                id = "hermit", name = "Hermit",
                narrativeIdentity = "Solitude, endurance, and self-control.",
                description = "Learned to endure difficult circumstances without relying on others, fortifying resistance against afflictions.",
                perkName = "Solitary Wisdom",
                perkDescription = "Increases Status Effect Resistance and restores MP when resisting negative effects.",
                ranks = new[]
                {
                    new BackgroundRankDef
                    {
                        rank = 1, requiredLevel = 1, perkName = "Quiet Endurance", perkDescription = "Status Effect Resistance increases by 5%.",
                        statusResistanceBonus = 5,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Background, "Quiet Endurance") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 2, requiredLevel = 4, perkName = "Solitary Resolve", perkDescription = "Status Effect Resistance increases to 8%.",
                        statusResistanceBonus = 8,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 8, ModifierKind.Flat, ModifierSource.Background, "Solitary Resolve") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 3, requiredLevel = 7, perkName = "Inner Balance", perkDescription = "Status Effect Resistance increases to 10%.",
                        statusResistanceBonus = 10,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Background, "Inner Balance") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 4, requiredLevel = 10, perkName = "Unshaken Mind", perkDescription = "Status Effect Resistance increases to 12%.",
                        statusResistanceBonus = 12,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 12, ModifierKind.Flat, ModifierSource.Background, "Unshaken Mind") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 5, requiredLevel = 13, perkName = "Tranquil Spirit", perkDescription = "Status Effect Resistance increases to 15%. When the character successfully resists a status effect, restore MP equal to 3% of Max MP, at most once per turn.",
                        statusResistanceBonus = 15,
                        statusResistMpRestorePercent = 0.03f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 15, ModifierKind.Flat, ModifierSource.Background, "Tranquil Spirit") }
                    }
                }
            },

            // 5. Farmer — Hardy Harvest
            new BackgroundDef
            {
                id = "farmer", name = "Farmer",
                narrativeIdentity = "Hard work, physical endurance, and a simple life.",
                description = "Accustomed to demanding work and harsh conditions, enhancing vitality and post-battle recovery.",
                perkName = "Hardy Harvest",
                perkDescription = "Increases Max HP and provides restorative recovery following combat encounters.",
                ranks = new[]
                {
                    new BackgroundRankDef
                    {
                        rank = 1, requiredLevel = 1, perkName = "Hardy Worker", perkDescription = "Max HP increases by 3%.",
                        maxHpPercentBonus = 0.03f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 0.03f, ModifierKind.PercentAdd, ModifierSource.Background, "Hardy Worker") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 2, requiredLevel = 4, perkName = "Sturdy Constitution", perkDescription = "Max HP increases to 5%.",
                        maxHpPercentBonus = 0.05f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 0.05f, ModifierKind.PercentAdd, ModifierSource.Background, "Sturdy Constitution") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 3, requiredLevel = 7, perkName = "Seasoned Laborer", perkDescription = "Max HP increases to 7%.",
                        maxHpPercentBonus = 0.07f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 0.07f, ModifierKind.PercentAdd, ModifierSource.Background, "Seasoned Laborer") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 4, requiredLevel = 10, perkName = "Resilient Farmer", perkDescription = "Max HP increases to 9%.",
                        maxHpPercentBonus = 0.09f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 0.09f, ModifierKind.PercentAdd, ModifierSource.Background, "Resilient Farmer") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 5, requiredLevel = 13, perkName = "Bountiful Harvest", perkDescription = "Max HP increases to 10%. After battle, restore HP equal to 3% of Max HP.",
                        maxHpPercentBonus = 0.10f,
                        postBattleHpRestorePercent = 0.03f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 0.10f, ModifierKind.PercentAdd, ModifierSource.Background, "Bountiful Harvest") }
                    }
                }
            },

            // 6. Noble — Noble Authority
            new BackgroundDef
            {
                id = "noble", name = "Noble",
                narrativeIdentity = "Social status, leadership, and confidence.",
                description = "Raised in an aristocratic environment where influence and leadership cultivate keen mental capacity.",
                perkName = "Noble Authority",
                perkDescription = "Increases Max MP and restores mental composure after battle.",
                ranks = new[]
                {
                    new BackgroundRankDef
                    {
                        rank = 1, requiredLevel = 1, perkName = "Noble Bearing", perkDescription = "Max MP increases by 3%.",
                        maxMpPercentBonus = 0.03f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 0.03f, ModifierKind.PercentAdd, ModifierSource.Background, "Noble Bearing") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 2, requiredLevel = 4, perkName = "Commanding Presence", perkDescription = "Max MP increases to 5%.",
                        maxMpPercentBonus = 0.05f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 0.05f, ModifierKind.PercentAdd, ModifierSource.Background, "Commanding Presence") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 3, requiredLevel = 7, perkName = "Royal Confidence", perkDescription = "Max MP increases to 7%.",
                        maxMpPercentBonus = 0.07f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 0.07f, ModifierKind.PercentAdd, ModifierSource.Background, "Royal Confidence") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 4, requiredLevel = 10, perkName = "Noble Authority", perkDescription = "Max MP increases to 9%.",
                        maxMpPercentBonus = 0.09f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 0.09f, ModifierKind.PercentAdd, ModifierSource.Background, "Noble Authority") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 5, requiredLevel = 13, perkName = "Born to Lead", perkDescription = "Max MP increases to 10%. After battle, restore MP equal to 5% of Max MP.",
                        maxMpPercentBonus = 0.10f,
                        postBattleMpRestorePercent = 0.05f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 0.10f, ModifierKind.PercentAdd, ModifierSource.Background, "Born to Lead") }
                    }
                }
            },

            // 7. Sage — Learned Mind
            new BackgroundDef
            {
                id = "sage", name = "Sage",
                narrativeIdentity = "Knowledge, research, and deep understanding.",
                description = "Spent considerable time studying arcane literature to maximize efficiency of skill mana expenditure.",
                perkName = "Learned Mind",
                perkDescription = "Reduces Active Skill MP Cost and grants a chance to recover MP upon skill use.",
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "Studied Knowledge", perkDescription = "Active Skill MP Cost is reduced by 3%.", mpCostReductionPercent = 0.03f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Efficient Casting", perkDescription = "Active Skill MP Cost is reduced by 5%.", mpCostReductionPercent = 0.05f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Deep Understanding", perkDescription = "Active Skill MP Cost is reduced by 7%.", mpCostReductionPercent = 0.07f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Masterful Insight", perkDescription = "Active Skill MP Cost is reduced by 9%.", mpCostReductionPercent = 0.09f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Enlightened Mind", perkDescription = "Active Skill MP Cost is reduced by 10%. After using an Active Skill, the character has a 10% chance to restore MP equal to 5% of Max MP.", mpCostReductionPercent = 0.10f, skillMpRefundChance = 0.10f, skillMpRefundPercent = 0.05f }
                }
            },

            // 8. Soldier — Veteran's Instinct
            new BackgroundDef
            {
                id = "soldier", name = "Soldier",
                narrativeIdentity = "War experience, discipline, and combat ability.",
                description = "Experienced facing dangerous frontline situations, improving physical damage output and retaliation strikes.",
                perkName = "Veteran's Instinct",
                perkDescription = "Increases Physical Damage and empowers strikes following enemy damage.",
                ranks = new[]
                {
                    new BackgroundRankDef
                    {
                        rank = 1, requiredLevel = 1, perkName = "Combat Training", perkDescription = "Physical Damage increases by 3%.",
                        physicalDamageBonus = 0.03f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 0.03f, ModifierKind.PercentAdd, ModifierSource.Background, "Combat Training") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 2, requiredLevel = 4, perkName = "Battle Experience", perkDescription = "Physical Damage increases to 5%.",
                        physicalDamageBonus = 0.05f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 0.05f, ModifierKind.PercentAdd, ModifierSource.Background, "Battle Experience") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 3, requiredLevel = 7, perkName = "Veteran's Instinct", perkDescription = "Physical Damage increases to 7%.",
                        physicalDamageBonus = 0.07f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 0.07f, ModifierKind.PercentAdd, ModifierSource.Background, "Veteran's Instinct") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 4, requiredLevel = 10, perkName = "Battle Hardened", perkDescription = "Physical Damage increases to 9%.",
                        physicalDamageBonus = 0.09f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 0.09f, ModifierKind.PercentAdd, ModifierSource.Background, "Battle Hardened") }
                    },
                    new BackgroundRankDef
                    {
                        rank = 5, requiredLevel = 13, perkName = "War Veteran", perkDescription = "Physical Damage increases to 10%. After the character receives damage from an enemy attack, its next Physical Damage instance increases by 5%, at most once per turn.",
                        physicalDamageBonus = 0.10f,
                        onDamagedNextPhysicalDamageBonus = 0.05f,
                        modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 0.10f, ModifierKind.PercentAdd, ModifierSource.Background, "War Veteran") }
                    }
                }
            },

            // 9. Wayfarer — Endless Journey
            new BackgroundDef
            {
                id = "wayfarer", name = "Wayfarer",
                narrativeIdentity = "Travel, exploration, and adaptation.",
                description = "Accustomed to long journeys and unfamiliar environments, moving with enhanced swiftness in combat.",
                perkName = "Endless Journey",
                perkDescription = "Increases Speed and can provide temporary turn-order surge at battle start.",
                ranks = new[]
                {
                    new BackgroundRankDef
                    {
                        rank = 1, requiredLevel = 1, perkName = "Traveler's Instinct", perkDescription = "Speed increases by 3%.",
                        speedPercentBonus = 0.03f,
                        modifiers = new[]
                        {
                            StatModifier.ForDerived(DerivedStatId.Initiative, 0.03f, ModifierKind.PercentAdd, ModifierSource.Background, "Traveler's Instinct"),
                            StatModifier.ForDerived(DerivedStatId.ActionSpeed, 0.03f, ModifierKind.PercentAdd, ModifierSource.Background, "Traveler's Instinct")
                        }
                    },
                    new BackgroundRankDef
                    {
                        rank = 2, requiredLevel = 4, perkName = "Roadwise", perkDescription = "Speed increases to 5%.",
                        speedPercentBonus = 0.05f,
                        modifiers = new[]
                        {
                            StatModifier.ForDerived(DerivedStatId.Initiative, 0.05f, ModifierKind.PercentAdd, ModifierSource.Background, "Roadwise"),
                            StatModifier.ForDerived(DerivedStatId.ActionSpeed, 0.05f, ModifierKind.PercentAdd, ModifierSource.Background, "Roadwise")
                        }
                    },
                    new BackgroundRankDef
                    {
                        rank = 3, requiredLevel = 7, perkName = "Seasoned Traveler", perkDescription = "Speed increases to 7%.",
                        speedPercentBonus = 0.07f,
                        modifiers = new[]
                        {
                            StatModifier.ForDerived(DerivedStatId.Initiative, 0.07f, ModifierKind.PercentAdd, ModifierSource.Background, "Seasoned Traveler"),
                            StatModifier.ForDerived(DerivedStatId.ActionSpeed, 0.07f, ModifierKind.PercentAdd, ModifierSource.Background, "Seasoned Traveler")
                        }
                    },
                    new BackgroundRankDef
                    {
                        rank = 4, requiredLevel = 10, perkName = "Pathfinder", perkDescription = "Speed increases to 9%.",
                        speedPercentBonus = 0.09f,
                        modifiers = new[]
                        {
                            StatModifier.ForDerived(DerivedStatId.Initiative, 0.09f, ModifierKind.PercentAdd, ModifierSource.Background, "Pathfinder"),
                            StatModifier.ForDerived(DerivedStatId.ActionSpeed, 0.09f, ModifierKind.PercentAdd, ModifierSource.Background, "Pathfinder")
                        }
                    },
                    new BackgroundRankDef
                    {
                        rank = 5, requiredLevel = 13, perkName = "Endless Journey", perkDescription = "Speed increases to 10%. At the start of battle, the character has a 10% chance to gain 10% Speed for 2 turns.",
                        speedPercentBonus = 0.10f,
                        battleStartSpeedBuffChance = 0.10f,
                        battleStartSpeedBuffPercent = 0.10f,
                        battleStartSpeedBuffDuration = 2,
                        modifiers = new[]
                        {
                            StatModifier.ForDerived(DerivedStatId.Initiative, 0.10f, ModifierKind.PercentAdd, ModifierSource.Background, "Endless Journey"),
                            StatModifier.ForDerived(DerivedStatId.ActionSpeed, 0.10f, ModifierKind.PercentAdd, ModifierSource.Background, "Endless Journey")
                        }
                    }
                }
            },

            // 10. Scribe — Keeper of Records
            new BackgroundDef
            {
                id = "scribe", name = "Scribe",
                narrativeIdentity = "Documentation, analysis, and recorded knowledge.",
                description = "Observes and records enemy traits, providing bonus accuracy and tactical critical strikes upon repeated study.",
                perkName = "Keeper of Records",
                perkDescription = "Improves Accuracy and rewards repeated attacks against the same target with critical chance.",
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "Careful Observation", perkDescription = "Accuracy increases by 3%.", accuracyBonus = 3 },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Detailed Records", perkDescription = "Accuracy increases to 5%.", accuracyBonus = 5 },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Enemy Analysis", perkDescription = "Accuracy increases to 7%.", accuracyBonus = 7 },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Tactical Notes", perkDescription = "Accuracy increases to 9%.", accuracyBonus = 9 },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Complete Record", perkDescription = "Accuracy increases to 10%. After attacking the same enemy 3 times, the character's next attack against that enemy gains 5% Critical Chance.", accuracyBonus = 10, scribeTargetAttackThreshold = 3, scribeNextAttackCritBonus = 0.05f }
                }
            },

            // Legacy backward-compatibility aliases
            new BackgroundDef
            {
                id = "oathsworn", name = "Oathsworn Guard",
                narrativeIdentity = "Former sentinel of the outer bastion.",
                description = "Trained to hold the line against abyssal incursions.",
                perkName = "Guardian's Discipline",
                perkDescription = "Reduces physical damage taken, with reinforced resistance at low health.",
                proficiencies = new[] { "Athletics", "Intimidation" },
                eventInteractions = new[] { "shrine", "fallen" },
                socialBonus = 1,
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "Watchful Guard", perkDescription = "Physical Damage Taken is reduced by 3%.", physicalDamageTakenReduction = 0.03f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Steady Defense", perkDescription = "Physical Damage Taken is reduced by 5%.", physicalDamageTakenReduction = 0.05f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Defensive Training", perkDescription = "Physical Damage Taken is reduced by 7%.", physicalDamageTakenReduction = 0.07f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Guardian's Resolve", perkDescription = "Physical Damage Taken is reduced by 9%.", physicalDamageTakenReduction = 0.09f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Unyielding Guardian", perkDescription = "Physical Damage Taken is reduced by 10%. While HP is below 30%, Physical Damage Taken is reduced by an additional 5%.", physicalDamageTakenReduction = 0.10f, lowHpPhysicalDamageTakenReduction = 0.05f }
                },
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.BlockPower, 4, ModifierKind.Flat, ModifierSource.Background, "Oathsworn Stance") }
            },
            new BackgroundDef
            {
                id = "outcast", name = "Crypt Outcast",
                narrativeIdentity = "Survivor of the sunken slums.",
                description = "Knows every trap, shadow, and hidden cache.",
                perkName = "Master of Deception",
                perkDescription = "Increases damage dealt to debuffed foes and strikes unprepared targets.",
                proficiencies = new[] { "Stealth", "Sleight of Hand" },
                eventInteractions = new[] { "fallen", "merchant" },
                socialBonus = 0,
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "False Promise", perkDescription = "Damage dealt to enemies affected by a debuff increases by 3%.", damageVsDebuffedBonus = 0.03f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Cunning Exploit", perkDescription = "The damage bonus increases to 5%.", damageVsDebuffedBonus = 0.05f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Deceptive Advantage", perkDescription = "The damage bonus increases to 7%.", damageVsDebuffedBonus = 0.07f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Master Manipulator", perkDescription = "The damage bonus increases to 10%.", damageVsDebuffedBonus = 0.10f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Perfect Deception", perkDescription = "The damage bonus increases to 12%. The character's first attack against an enemy that has not yet acted in the battle deals an additional 5% damage.", damageVsDebuffedBonus = 0.12f, firstAttackUnactedBonus = 0.05f }
                },
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 3, ModifierKind.Flat, ModifierSource.Background, "Tunnel Reflexes") }
            },
            new BackgroundDef
            {
                id = "pilgrim", name = "Wandering Pilgrim",
                narrativeIdentity = "Bearer of the last lantern.",
                description = "Walked from shrine to shrine offering solace and counsel.",
                perkName = "Faithful Devotion",
                perkDescription = "Empowers outgoing and incoming healing.",
                proficiencies = new[] { "Insight", "Persuasion" },
                eventInteractions = new[] { "shrine", "merchant" },
                socialBonus = 2,
                ranks = new[]
                {
                    new BackgroundRankDef { rank = 1, requiredLevel = 1, perkName = "Faithful Heart", perkDescription = "Healing dealt increases by 5%.", healingDealtBonus = 0.05f },
                    new BackgroundRankDef { rank = 2, requiredLevel = 4, perkName = "Divine Resilience", perkDescription = "Healing dealt increases to 8%.", healingDealtBonus = 0.08f },
                    new BackgroundRankDef { rank = 3, requiredLevel = 7, perkName = "Blessed Recovery", perkDescription = "Healing dealt increases to 10%. Healing received also increases by 5%.", healingDealtBonus = 0.10f, healingReceivedBonus = 0.05f },
                    new BackgroundRankDef { rank = 4, requiredLevel = 10, perkName = "Sanctified Spirit", perkDescription = "Healing dealt increases to 12%, and healing received increases by 8%.", healingDealtBonus = 0.12f, healingReceivedBonus = 0.08f },
                    new BackgroundRankDef { rank = 5, requiredLevel = 13, perkName = "Faith Unbroken", perkDescription = "Healing dealt increases to 15%, and healing received increases by 10%.", healingDealtBonus = 0.15f, healingReceivedBonus = 0.10f }
                },
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.SupportPower, 2, ModifierKind.Flat, ModifierSource.Background, "Inspiring Word") }
            }
        }, b => b.id);



        // ---- 7. Equipment Across All 8 Slots & Starter Passives (Sections 5 & 8) ----

        public static readonly Dictionary<string, EquipmentDef> Equipment = Index(new[]

        {

            // Main Hand Weapons

            new EquipmentDef

            {

                id = "iron_broadsword", name = "Iron Broadsword", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Sword, attackRangeType = AttackRangeType.Melee,

                requiredProficiency = "Sword", weaponPower = 3, weaponScaling = 0.5f, accuracyModifier = 2,

                weaponProperties = new[] { "Melee", "Versatile" },

                description = "+3 Weapon Power, +0.5 STR scaling."

            },

            new EquipmentDef

            {

                id = "steel_axe", name = "Steel Battleaxe", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.Axe, attackRangeType = AttackRangeType.Melee,

                weaponPower = 4, breakPowerModifier = 2,

                weaponProperties = new[] { "Melee", "Versatile", "Cleave" },

                description = "+4 Weapon Power, +2 Break Power."

            },

            new EquipmentDef

            {

                id = "iron_warhammer", name = "Iron Warhammer", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.Warhammer, attackRangeType = AttackRangeType.Melee,

                weaponPower = 5, breakPowerModifier = 4,

                weaponProperties = new[] { "Melee", "Heavy", "Blunt" },

                description = "+5 Weapon Power, +4 Break Power."

            },

            new EquipmentDef

            {

                id = "iron_spear", name = "Iron Spear", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.Spear, attackRangeType = AttackRangeType.Melee,

                weaponPower = 3, accuracyModifier = 3,

                weaponProperties = new[] { "Melee", "Reach", "Piercing" },

                description = "+3 Weapon Power, +3 Accuracy."

            },

            new EquipmentDef

            {

                id = "iron_gauntlets", name = "Iron Gauntlets", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.Gauntlet, attackRangeType = AttackRangeType.Melee, isUnarmed = true,

                weaponPower = 3, breakPowerModifier = 3, accuracyModifier = 2,

                weaponProperties = new[] { "Melee", "Gauntlet", "Strike" },

                description = "Martial gauntlets: +3 Weapon Power, +3 Break Power."

            },

            new EquipmentDef

            {

                id = "iron_knuckle", name = "Iron Knuckle", slot = EquipmentSlot.SecondHand,

                equipmentType = EquipmentType.OffHand,

                weaponType = WeaponType.FistWeapon, attackRangeType = AttackRangeType.Melee, isUnarmed = true,

                weaponPower = 2, breakPowerModifier = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Initiative, 1, ModifierKind.Flat, ModifierSource.Equipment, "Iron Knuckle") },

                description = "Off-hand Fist Weapon: +2 Weapon Power, +2 Break Power, +1 Initiative."

            },

            new EquipmentDef

            {

                id = "duelist_rapier", name = "Duelist Rapier", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.Rapier, attackRangeType = AttackRangeType.Melee,

                weaponPower = 3, accuracyModifier = 4,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Equipment, "Duelist Rapier") },

                weaponProperties = new[] { "Melee", "Finesse", "Piercing" },

                description = "Finesse rapier: +3 Weapon Power, +4 Accuracy, +5% Crit Chance."

            },

            new EquipmentDef

            {

                id = "apprentice_wand", name = "Apprentice Wand", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.Wand, attackRangeType = AttackRangeType.Ranged,

                weaponPower = 2, accuracyModifier = 3,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Apprentice Wand") },

                weaponProperties = new[] { "Ranged", "Wand", "Arcane" },

                description = "Arcane wand: +2 Weapon Power, +3 Magic Power."

            },

            new EquipmentDef

            {

                id = "arcane_orb", name = "Arcane Orb", slot = EquipmentSlot.SecondHand,

                equipmentType = EquipmentType.OffHand,

                weaponType = WeaponType.Orb, magicDefense = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Arcane Orb") },

                description = "Off-hand Orb: +2 Magic Power, +2 Magic Defense."

            },

            new EquipmentDef

            {

                id = "silver_holy_symbol", name = "Silver Holy Symbol", slot = EquipmentSlot.SecondHand,

                equipmentType = EquipmentType.OffHand,

                weaponType = WeaponType.HolySymbol, magicDefense = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Silver Holy Symbol") },

                description = "Off-hand Holy Symbol: +3 Healing Power, +2 Magic Defense."

            },

            new EquipmentDef

            {

                id = "ritual_dagger", name = "Ritual Blade", slot = EquipmentSlot.MainHand,

                equipmentType = EquipmentType.Weapon,

                weaponType = WeaponType.RitualBlade, attackRangeType = AttackRangeType.Melee,

                weaponPower = 3,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Ritual Blade") },

                weaponProperties = new[] { "Melee", "Pact", "Occult" },

                description = "Occult blade: +3 Weapon Power, +3 Magic Power."

            },

            new EquipmentDef

            {

                id = "battle_greataxe", name = "Battle Greataxe", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Greataxe, attackRangeType = AttackRangeType.Melee,

                weaponPower = 5, breakPowerModifier = 3,

                weaponProperties = new[] { "Melee", "Heavy", "Cleave" },

                description = "+5 Weapon Power, +3 Break Power."

            },

            new EquipmentDef

            {

                id = "hand_wraps", name = "Monk Hand Wraps", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Fist, attackRangeType = AttackRangeType.Melee, isUnarmed = true,

                weaponPower = 2, breakPowerModifier = 4, accuracyModifier = 4,

                weaponProperties = new[] { "Melee", "Unarmed", "Flurry" },

                description = "Unarmed wraps: +2 Weapon Power, +4 Break Power, +4 Accuracy."

            },

            new EquipmentDef

            {

                id = "iron_dagger", name = "Iron Dagger", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Dagger, attackRangeType = AttackRangeType.Melee,

                weaponPower = 2, accuracyModifier = 4,

                weaponProperties = new[] { "Melee", "Finesse", "Light" },

                description = "+2 Weapon Power, +4 Accuracy."

            },

            new EquipmentDef

            {

                id = "assassin_dirk", name = "Assassin Dirk", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Dagger, attackRangeType = AttackRangeType.Melee,

                requiredProficiency = "Dagger", weaponPower = 2, accuracyModifier = 3,

                abilityBonuses = new AbilityScores(0, 1, 0, 0, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Equipment, "Assassin Dirk") },

                weaponProperties = new[] { "Melee", "Finesse" },

                weaponEffects = new[] { "+5% Critical Chance" },

                description = "+2 Weapon Power, +1 DEX, +5% Crit Chance."

            },

            new EquipmentDef

            {

                id = "hunting_bow", name = "Hunting Bow", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Bow, attackRangeType = AttackRangeType.Ranged,

                weaponPower = 3, accuracyModifier = 5,

                weaponProperties = new[] { "Ranged", "Two-Handed" },

                description = "Ranged weapon: +3 Weapon Power, +5 Accuracy."

            },

            new EquipmentDef

            {

                id = "heavy_crossbow", name = "Heavy Crossbow", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Crossbow, attackRangeType = AttackRangeType.Ranged,

                weaponPower = 4, breakPowerModifier = 2, accuracyModifier = 3,

                weaponProperties = new[] { "Ranged", "Armor Piercing" },

                description = "Ranged weapon: +4 Weapon Power, +2 Break Power."

            },

            new EquipmentDef

            {

                id = "sanctified_mace", name = "Sanctified Mace", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Mace, attackRangeType = AttackRangeType.Melee,

                weaponPower = 2, breakPowerModifier = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Sanctified Mace") },

                weaponProperties = new[] { "Melee", "Blunt", "Sacred" },

                description = "+2 Weapon Power, +2 Healing Power."

            },

            new EquipmentDef

            {

                id = "oak_staff", name = "Oak Staff", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Staff, attackRangeType = AttackRangeType.Melee,

                weaponPower = 1, accuracyModifier = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Oak Staff") },

                weaponProperties = new[] { "Melee", "Arcane Focus" },

                description = "+1 Weapon Power, +2 Magic Power."

            },

            new EquipmentDef

            {

                id = "runed_staff", name = "Runed Staff", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Staff, attackRangeType = AttackRangeType.Melee,

                requiredProficiency = "Staff", weaponPower = 1,

                abilityBonuses = new AbilityScores(0, 0, 0, 2, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Runed Staff") },

                weaponProperties = new[] { "Melee", "Runic Focus" },

                description = "+2 INT, +3 Magic Power."

            },

            new EquipmentDef

            {

                id = "eldritch_rod", name = "Eldritch Rod", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Rod, attackRangeType = AttackRangeType.Melee,

                weaponPower = 1,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Eldritch Rod") },

                weaponProperties = new[] { "Melee", "Pact Conduit" },

                description = "+1 Weapon Power, +3 Magic Power."

            },

            new EquipmentDef

            {

                id = "legendary_sunblade", name = "Sunblade of Dawn", slot = EquipmentSlot.MainHand,

                weaponType = WeaponType.Greatsword, attackRangeType = AttackRangeType.Melee,

                rarity = EquipmentRarity.Legendary, weaponPower = 9, accuracyModifier = 6, breakPowerModifier = 5,

                weaponProperties = new[] { "Melee", "Legendary", "Radiant" },

                weaponEffects = new[] { "Solar Brand" },

                description = "Legendary Greatsword. Still obeys strict class weapon compatibility."

            },



            // Second Hand Weapons / Off-Hand Items (Every class has at least one compatible option)

            new EquipmentDef

            {

                id = "iron_shield", name = "Iron Shield", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Shield, physicalDefense = 3, magicDefense = 1,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.BlockPower, 8, ModifierKind.Flat, ModifierSource.Equipment, "Iron Shield") },

                description = "Off-hand Shield: +3 Physical Defense, +8% Block Power."

            },

            new EquipmentDef

            {

                id = "iron_buckler", name = "Iron Buckler", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Buckler, physicalDefense = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 2, ModifierKind.Flat, ModifierSource.Equipment, "Iron Buckler") },

                description = "Off-hand Buckler: +2 Physical Defense, +2% Evasion."

            },

            new EquipmentDef

            {

                id = "parrying_dagger", name = "Parrying Dagger", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.ParryingDagger, weaponPower = 1, accuracyModifier = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 3, ModifierKind.Flat, ModifierSource.Equipment, "Parrying Dagger") },

                description = "Off-hand Dagger: +1 Weapon Power, +3% Crit Chance."

            },

            new EquipmentDef

            {

                id = "bone_warhorn", name = "Bone Warhorn", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Warhorn, breakPowerModifier = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Bone Warhorn") },

                description = "Off-hand Warhorn: +2 Physical Power, +2 Break Power."

            },

            new EquipmentDef

            {

                id = "ki_beads", name = "Prayer Ki Beads", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.MartialFocus, isUnarmed = true, breakPowerModifier = 3,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Initiative, 1, ModifierKind.Flat, ModifierSource.Equipment, "Ki Beads") },

                description = "Off-hand Martial Focus: +3 Break Power, +1 Initiative."

            },

            new EquipmentDef

            {

                id = "fletched_quiver", name = "Fletched Quiver", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Quiver, accuracyModifier = 4,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.RangedPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Fletched Quiver") },

                description = "Off-hand Quiver: +2 Ranged Power, +4 Accuracy."

            },

            new EquipmentDef

            {

                id = "sacred_chime", name = "Sacred Chime", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.RelicSymbol, magicDefense = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Sacred Chime") },

                description = "Off-hand Relic Symbol: +3 Healing Power, +2 Magic Defense."

            },

            new EquipmentDef

            {

                id = "apprentice_tome", name = "Apprentice Tome", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.ArcaneTome, magicDefense = 1,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Apprentice Tome") },

                description = "Off-hand Arcane Tome: +2 Magic Power, +1 Magic Defense."

            },

            new EquipmentDef

            {

                id = "pact_grimoire", name = "Grimoire of Shadows", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Grimoire, magicDefense = 1,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPenetration, 2, ModifierKind.Flat, ModifierSource.Equipment, "Pact Grimoire") },

                description = "Off-hand Pact Grimoire: +2 Magic Penetration, +1 Magic Defense."

            },

            new EquipmentDef

            {

                id = "verdant_totem", name = "Verdant Totem", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Totem, magicDefense = 2,

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 8, ModifierKind.Flat, ModifierSource.Equipment, "Verdant Totem") },

                description = "Off-hand Nature Totem: +8 Max HP, +2 Magic Defense."

            },

            new EquipmentDef

            {

                id = "legendary_aegis", name = "Aegis of the Bulwark", slot = EquipmentSlot.SecondHand,

                weaponType = WeaponType.Shield, rarity = EquipmentRarity.Legendary,

                physicalDefense = 8, magicDefense = 5,

                description = "Legendary Shield. Still obeys strict Second Hand weapon compatibility."

            },



            // Head Armor (Slot 3) — Cross-class by default

            new EquipmentDef { id = "iron_helm", name = "Iron Helm", slot = EquipmentSlot.HeadArmor, physicalDefense = 2, description = "+2 Physical Defense." },

            new EquipmentDef { id = "horned_helm", name = "Horned Visor", slot = EquipmentSlot.HeadArmor, physicalDefense = 2, breakPowerModifier = 1, description = "+2 Physical Defense, +1 Break Power." },

            new EquipmentDef { id = "monk_headband", name = "Focused Headband", slot = EquipmentSlot.HeadArmor, physicalDefense = 1, magicDefense = 1, accuracyModifier = 2, description = "+1 DEF, +1 MDF, +2 Accuracy." },

            new EquipmentDef { id = "shadow_hood", name = "Shadow Hood", slot = EquipmentSlot.HeadArmor, physicalDefense = 1, accuracyModifier = 2, description = "+1 Physical Defense, +2 Accuracy." },

            new EquipmentDef { id = "scout_cap", name = "Scout Cap", slot = EquipmentSlot.HeadArmor, physicalDefense = 1, accuracyModifier = 3, description = "+1 Physical Defense, +3 Accuracy." },

            new EquipmentDef { id = "acolyte_circlet", name = "Acolyte Circlet", slot = EquipmentSlot.HeadArmor, magicDefense = 2, description = "+2 Magic Defense." },

            new EquipmentDef { id = "scholar_hood", name = "Scholar Hood", slot = EquipmentSlot.HeadArmor, magicDefense = 2, description = "+2 Magic Defense." },

            new EquipmentDef { id = "cowl_of_whispers", name = "Cowl of Whispers", slot = EquipmentSlot.HeadArmor, magicDefense = 2, description = "+2 Magic Defense." },

            new EquipmentDef { id = "antler_crown", name = "Antler Crown", slot = EquipmentSlot.HeadArmor, physicalDefense = 1, magicDefense = 2, description = "+1 DEF, +2 MDF." },



            // Body Armor (Slot 4) — Cross-class by default

            new EquipmentDef { id = "chain_hauberk", name = "Chain Hauberk", slot = EquipmentSlot.BodyArmor, physicalDefense = 3, magicDefense = 1, description = "+3 Physical Defense, +1 Magic Defense." },

            new EquipmentDef { id = "hide_harness", name = "Beast Hide Harness", slot = EquipmentSlot.BodyArmor, physicalDefense = 3, description = "+3 Physical Defense." },

            new EquipmentDef { id = "cloth_tunic", name = "Cloth Tunic", slot = EquipmentSlot.BodyArmor, physicalDefense = 1, magicDefense = 2, description = "+1 Physical Defense, +2 Magic Defense." },

            new EquipmentDef { id = "leather_jerkin", name = "Leather Jerkin", slot = EquipmentSlot.BodyArmor, physicalDefense = 2, magicDefense = 1, description = "+2 Physical Defense, +1 Magic Defense." },

            new EquipmentDef { id = "linen_vestments", name = "Linen Vestments", slot = EquipmentSlot.BodyArmor, physicalDefense = 1, magicDefense = 3, description = "+1 Physical Defense, +3 Magic Defense." },

            new EquipmentDef { id = "darkweave_robe", name = "Darkweave Robe", slot = EquipmentSlot.BodyArmor, physicalDefense = 1, magicDefense = 3, description = "+1 Physical Defense, +3 Magic Defense." },

            new EquipmentDef { id = "barkskin_vest", name = "Barkskin Vest", slot = EquipmentSlot.BodyArmor, physicalDefense = 2, magicDefense = 2, description = "+2 Physical Defense, +2 Magic Defense." },

            new EquipmentDef

            {

                id = "steel_cuirass", name = "Steel Cuirass", slot = EquipmentSlot.BodyArmor,

                requiredProficiency = "Heavy", physicalDefense = 4, magicDefense = 1,

                abilityBonuses = new AbilityScores(0, 0, 1, 0, 0, 0),

                description = "+4 Physical Defense, +1 Magic Defense, +1 VIT."

            },

            new EquipmentDef

            {

                id = "warded_vestments", name = "Warded Vestments", slot = EquipmentSlot.BodyArmor,

                requiredProficiency = "Cloth", physicalDefense = 1, magicDefense = 4,

                abilityBonuses = new AbilityScores(0, 0, 0, 0, 1, 0),

                description = "+1 Physical Defense, +4 Magic Defense, +1 WIS."

            },



            // Leg Armor (Slot 5) — Cross-class by default

            new EquipmentDef { id = "chain_greaves", name = "Chain Greaves", slot = EquipmentSlot.LegArmor, physicalDefense = 2, description = "+2 Physical Defense." },

            new EquipmentDef { id = "fur_leggings", name = "Fur Leggings", slot = EquipmentSlot.LegArmor, physicalDefense = 2, description = "+2 Physical Defense." },

            new EquipmentDef { id = "cloth_trousers", name = "Cloth Trousers", slot = EquipmentSlot.LegArmor, physicalDefense = 1, magicDefense = 1, description = "+1 DEF, +1 MDF." },

            new EquipmentDef { id = "leather_greaves", name = "Leather Greaves", slot = EquipmentSlot.LegArmor, physicalDefense = 1, magicDefense = 1, description = "+1 DEF, +1 MDF." },



            // Foot Armor (Slot 6) — Cross-class by default

            new EquipmentDef { id = "iron_sabatons", name = "Iron Sabatons", slot = EquipmentSlot.FootArmor, physicalDefense = 2, description = "+2 Physical Defense." },

            new EquipmentDef { id = "strider_boots", name = "Strider Boots", slot = EquipmentSlot.FootArmor, physicalDefense = 1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 2, ModifierKind.Flat, ModifierSource.Equipment, "Strider Boots") }, description = "+1 DEF, +2% Evasion." },

            new EquipmentDef { id = "soft_boots", name = "Soft Leather Boots", slot = EquipmentSlot.FootArmor, physicalDefense = 1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Initiative, 1, ModifierKind.Flat, ModifierSource.Equipment, "Soft Boots") }, description = "+1 DEF, +1 Initiative." },

            new EquipmentDef { id = "pilgrim_sandals", name = "Pilgrim Sandals", slot = EquipmentSlot.FootArmor, magicDefense = 1, description = "+1 Magic Defense." },



            // Accessories (Slots 7 & 8) — Cross-class by default

            new EquipmentDef { id = "vanguard_ring", name = "Vanguard Ring", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 5, ModifierKind.Flat, ModifierSource.Equipment, "Vanguard Ring") }, description = "+5 Max HP." },

            new EquipmentDef { id = "soldier_medal", name = "Soldier's Medal", slot = EquipmentSlot.Accessory2, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 3, ModifierKind.Flat, ModifierSource.Equipment, "Soldier's Medal") }, description = "+3% Status Resistance." },

            new EquipmentDef { id = "fang_talisman", name = "Fang Talisman", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 1, ModifierKind.Flat, ModifierSource.Equipment, "Fang Talisman") }, description = "+1 Physical Power." },

            new EquipmentDef { id = "focus_band", name = "Focus Band", slot = EquipmentSlot.Accessory1, accuracyModifier = 2, description = "+2 Accuracy." },

            new EquipmentDef { id = "swift_anklet", name = "Swift Anklet", slot = EquipmentSlot.Accessory2, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 2, ModifierKind.Flat, ModifierSource.Equipment, "Swift Anklet") }, description = "+2% Evasion." },

            new EquipmentDef { id = "thief_ring", name = "Thief's Ring", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 2, ModifierKind.Flat, ModifierSource.Equipment, "Thief's Ring") }, description = "+2% Crit Chance." },

            new EquipmentDef { id = "hawk_feather", name = "Hawk Feather Charm", slot = EquipmentSlot.Accessory1, accuracyModifier = 3, description = "+3 Accuracy." },

            new EquipmentDef { id = "prayer_rosary", name = "Prayer Rosary", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Prayer Rosary") }, description = "+2 Healing Power." },

            new EquipmentDef { id = "mana_crystal", name = "Mana Crystal", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 5, ModifierKind.Flat, ModifierSource.Equipment, "Mana Crystal") }, description = "+5 Max MP." },

            new EquipmentDef { id = "pact_seal", name = "Pact Seal", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 2, ModifierKind.Flat, ModifierSource.Equipment, "Pact Seal") }, description = "+2 Magic Power." },

            new EquipmentDef { id = "amber_seed", name = "Amber Seed", slot = EquipmentSlot.Accessory1, modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 6, ModifierKind.Flat, ModifierSource.Equipment, "Amber Seed") }, description = "+6 Max HP." },

            new EquipmentDef

            {

                id = "signet_of_command", name = "Signet of Command", slot = EquipmentSlot.Accessory1,

                abilityBonuses = new AbilityScores(0, 0, 0, 0, 0, 2),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.SupportPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Signet of Command") },

                description = "+2 CHA, +3 Support Power."

            },



            // Restricted Armor & Accessories for restriction validation tests (Class, Race, Level, Stat requirements)

            new EquipmentDef

            {

                id = "cleric_vestment_only", name = "ConsecratedChasuble", slot = EquipmentSlot.BodyArmor,

                physicalDefense = 2, magicDefense = 5,

                restrictions = new EquipmentRestriction { allowedClasses = new[] { "cleric" } },

                description = "Cleric-only vestment."

            },

            new EquipmentDef

            {

                id = "dwarven_ancestral_helm", name = "Dwarven Ancestral Helm", slot = EquipmentSlot.HeadArmor,

                physicalDefense = 4,

                restrictions = new EquipmentRestriction { allowedRaces = new[] { "dwarf" } },

                description = "Dwarf-only ancestral helmet."

            },

            new EquipmentDef

            {

                id = "veteran_greaves", name = "Veteran Greaves", slot = EquipmentSlot.LegArmor,

                physicalDefense = 5,

                restrictions = new EquipmentRestriction { minLevel = 5 },

                description = "Requires Character Level 5."

            },

            new EquipmentDef

            {

                id = "titan_girdle", name = "Titan Girdle", slot = EquipmentSlot.Accessory1,

                physicalDefense = 3,

                restrictions = new EquipmentRestriction { minAbilityScores = new AbilityScores(15, 0, 0, 0, 0, 0) },

                description = "Requires STR 15."

            },

        }, e => e.id);



        // ---- 8. Starter Passives & General Passives (Section 5) ----

        public static readonly Dictionary<string, PassiveDef> Passives = Index(new[]

        {

            // 1. Fighter — Weapon Proficiency

            new PassiveDef

            {

                id = "weapon_proficiency", name = "Weapon Proficiency", classId = "fighter", isStarterPassive = true,

                weaponAttackPowerBonus = 3,

                description = "Increases Physical Power from weapon attacks."

            },

            // 2. Barbarian — Savage Instinct

            new PassiveDef

            {

                id = "savage_instinct", name = "Savage Instinct", classId = "barbarian", isStarterPassive = true,

                lowHpPhysicalPowerBonus = 5, lowHpThreshold = 0.5f,

                description = "Grants a Physical Power bonus when HP is below 50%."

            },

            // 3. Martial Artist — Martial Discipline

            new PassiveDef

            {

                id = "martial_discipline", name = "Martial Discipline", classId = "martial_artist", isStarterPassive = true,

                unarmedBreakPowerBonus = 5,

                description = "Increases Physical Break Power from unarmed attacks."

            },

            // 4. Rogue — Opportunist

            new PassiveDef

            {

                id = "opportunist", name = "Opportunist", classId = "rogue", isStarterPassive = true,

                critChanceVsDebuffedBonus = 15,

                description = "Increases Critical Chance against enemies with debuffs."

            },

            // 5. Ranger — Hunter's Instinct

            new PassiveDef

            {

                id = "hunters_instinct", name = "Hunter's Instinct", classId = "ranger", isStarterPassive = true,

                rangedAccuracyBonus = 12,

                description = "Increases Ranged Accuracy for ranged basic attacks and ranged active skills."

            },

            // 6. Cleric — Divine Grace

            new PassiveDef

            {

                id = "divine_grace", name = "Divine Grace", classId = "cleric", isStarterPassive = true,

                clericHealingBonus = 4,

                description = "Increases Healing Power from Cleric skills."

            },

            // 7. Wizard — Arcane Affinity

            new PassiveDef

            {

                id = "arcane_affinity", name = "Arcane Affinity", classId = "wizard", isStarterPassive = true,

                arcaneMpCostReduction = 2,

                description = "Reduces MP cost of Arcane Spells."

            },

            // 8. Warlock — Pact Affinity

            new PassiveDef

            {

                id = "pact_affinity", name = "Pact Affinity", classId = "warlock", isStarterPassive = true,

                pactMagicPowerBonus = 4,

                description = "Increases Magic Power from Pact-tagged skills."

            },

            // 9. Druid — Nature's Attunement

            new PassiveDef

            {

                id = "natures_attunement", name = "Nature's Attunement", classId = "druid", isStarterPassive = true,

                natureMagicAndHealBonus = 3,

                description = "Increases Nature Magic Power and Healing Power from Nature-based skills."

            },



            // Additional unlockable passives — Fighter Tree

            new PassiveDef

            {

                id = "defensive_training", name = "Defensive Training", classId = "fighter", tier = 1,

                description = "+4 Physical Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 4, ModifierKind.Flat, ModifierSource.Passive, "Defensive Training") }

            },

            new PassiveDef

            {

                id = "combat_discipline", name = "Combat Discipline", classId = "fighter", tier = 2,

                description = "+5% QTE Timing Window bonus.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.QteTimingBonus, 5, ModifierKind.Flat, ModifierSource.Passive, "Combat Discipline") }

            },

            new PassiveDef

            {

                id = "shield_mastery", name = "Shield Mastery", classId = "fighter", tier = 2,

                description = "+15 Block Power, +3 Physical Defense.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.BlockPower, 15, ModifierKind.Flat, ModifierSource.Passive, "Shield Mastery"),

                    StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 3, ModifierKind.Flat, ModifierSource.Passive, "Shield Mastery")

                }

            },

            new PassiveDef

            {

                id = "tactical_awareness", name = "Tactical Awareness", classId = "fighter", tier = 3,

                description = "+4 Initiative, +5% Critical Chance.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.Initiative, 4, ModifierKind.Flat, ModifierSource.Passive, "Tactical Awareness"),

                    StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Passive, "Tactical Awareness")

                }

            },

            new PassiveDef

            {

                id = "weapon_adaptation", name = "Weapon Adaptation", classId = "fighter", tier = 3,

                description = "+5 Physical Power, +4 Physical Break.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.PhysicalPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Weapon Adaptation"),

                    StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Weapon Adaptation")

                }

            },

            new PassiveDef

            {

                id = "iron_will", name = "Iron Will", classId = "fighter", tier = 4,

                description = "+1 VIT, +10 Status Resistance.",

                abilityBonuses = new AbilityScores(0, 0, 1, 0, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Iron Will") }

            },

            new PassiveDef

            {

                id = "counter_specialist", name = "Counter Specialist", classId = "fighter", tier = 4,

                description = "+8 Counter Power, +4 Physical Power.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.CounterPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Counter Specialist"),

                    StatModifier.ForDerived(DerivedStatId.PhysicalPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Counter Specialist")

                }

            },

            new PassiveDef

            {

                id = "master_of_arms", name = "Master of Arms", classId = "fighter", tier = 5,

                description = "+10 Physical Power, +8 Physical Break, +5% Critical Chance.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.PhysicalPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Master of Arms"),

                    StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Master of Arms"),

                    StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Passive, "Master of Arms")

                }

            },

            new PassiveDef

            {

                id = "unyielding_defense", name = "Unyielding Defense", classId = "fighter", tier = 5,

                description = "+12 Physical Defense, +25 Max HP, +10 Status Resistance.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 12, ModifierKind.Flat, ModifierSource.Passive, "Unyielding Defense"),

                    StatModifier.ForDerived(DerivedStatId.MaxHP, 25, ModifierKind.Flat, ModifierSource.Passive, "Unyielding Defense"),

                    StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Unyielding Defense")

                }

            },

            new PassiveDef

            {

                id = "battle_focus", name = "Battle Focus", classId = "fighter", tier = 2,

                description = "+3 Physical Power, +3 Physical Break.",

                modifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.PhysicalPower, 3, ModifierKind.Flat, ModifierSource.Passive, "Battle Focus"),

                    StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 3, ModifierKind.Flat, ModifierSource.Passive, "Battle Focus")

                }

            },

        



            // ---- All Playable Classes Unlockable Passives ----

            new PassiveDef

            {

                id = "bloodthirst", name = "Bloodthirst", classId = "barbarian", tier = 1,

                description = "+4 Physical Power, heals 4 HP on defeating a foe.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Bloodthirst") }

            },

            new PassiveDef

            {

                id = "unrelenting_fury", name = "Unrelenting Fury", classId = "barbarian", tier = 2,

                description = "+5 Physical Power, +3 Physical Break.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Unrelenting Fury"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 3, ModifierKind.Flat, ModifierSource.Passive, "Unrelenting Fury") }

            },

            new PassiveDef

            {

                id = "pain_tolerance", name = "Pain Tolerance", classId = "barbarian", tier = 2,

                description = "+5 Physical Defense, +15 Max HP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 5, ModifierKind.Flat, ModifierSource.Passive, "Pain Tolerance"), StatModifier.ForDerived(DerivedStatId.MaxHP, 15, ModifierKind.Flat, ModifierSource.Passive, "Pain Tolerance") }

            },

            new PassiveDef

            {

                id = "predators_instinct", name = "Predator's Instinct", classId = "barbarian", tier = 3,

                description = "+8% Critical Chance, +4 Physical Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 8, ModifierKind.Flat, ModifierSource.Passive, "Predator's Instinct"), StatModifier.ForDerived(DerivedStatId.PhysicalPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Predator's Instinct") }

            },

            new PassiveDef

            {

                id = "berserkers_momentum", name = "Berserker's Momentum", classId = "barbarian", tier = 3,

                description = "+4 Initiative, +5 Physical Break.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Initiative, 4, ModifierKind.Flat, ModifierSource.Passive, "Berserker's Momentum"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Berserker's Momentum") }

            },

            new PassiveDef

            {

                id = "unstoppable_rage", name = "Unstoppable Rage", classId = "barbarian", tier = 4,

                description = "+15 Status Resistance, +6 Physical Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 15, ModifierKind.Flat, ModifierSource.Passive, "Unstoppable Rage"), StatModifier.ForDerived(DerivedStatId.PhysicalPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Unstoppable Rage") }

            },

            new PassiveDef

            {

                id = "blood_pact", name = "Blood Pact", classId = "barbarian", tier = 4,

                description = "+25 Max HP, +6 Physical Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 25, ModifierKind.Flat, ModifierSource.Passive, "Blood Pact"), StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 6, ModifierKind.Flat, ModifierSource.Passive, "Blood Pact") }

            },

            new PassiveDef

            {

                id = "avatar_of_destruction", name = "Avatar of Destruction", classId = "barbarian", tier = 5,

                description = "+12 Physical Power, +8 Physical Break.",

                abilityBonuses = new AbilityScores(1, 0, 0, 0, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 12, ModifierKind.Flat, ModifierSource.Passive, "Avatar of Destruction"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Avatar of Destruction") }

            },

            new PassiveDef

            {

                id = "endless_fury", name = "Endless Fury", classId = "barbarian", tier = 5,

                description = "+35 Max HP, +8 Physical Power, +10 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 35, ModifierKind.Flat, ModifierSource.Passive, "Endless Fury"), StatModifier.ForDerived(DerivedStatId.PhysicalPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Endless Fury"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Endless Fury") }

            },

            new PassiveDef

            {

                id = "combat_rhythm", name = "Combat Rhythm", classId = "martial_artist", tier = 1,

                description = "+5% QTE Timing Window bonus, +3 Initiative.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.QteTimingBonus, 5, ModifierKind.Flat, ModifierSource.Passive, "Combat Rhythm"), StatModifier.ForDerived(DerivedStatId.Initiative, 3, ModifierKind.Flat, ModifierSource.Passive, "Combat Rhythm") }

            },

            new PassiveDef

            {

                id = "combo_mastery", name = "Combo Mastery", classId = "martial_artist", tier = 2,

                description = "+4 Physical Power, +4 Physical Break.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Combo Mastery"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Combo Mastery") }

            },

            new PassiveDef

            {

                id = "agile_defense", name = "Agile Defense", classId = "martial_artist", tier = 2,

                description = "+6 Evasion, +3 Physical Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 6, ModifierKind.Flat, ModifierSource.Passive, "Agile Defense"), StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 3, ModifierKind.Flat, ModifierSource.Passive, "Agile Defense") }

            },

            new PassiveDef

            {

                id = "inner_focus", name = "Inner Focus", classId = "martial_artist", tier = 3,

                description = "+5 Status Resistance, +5% Critical Chance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Passive, "Inner Focus"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Passive, "Inner Focus") }

            },

            new PassiveDef

            {

                id = "pressure_point", name = "Pressure Point", classId = "martial_artist", tier = 3,

                description = "+6 Physical Break, +8% Critical Chance vs Broken.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Pressure Point"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 8, ModifierKind.Flat, ModifierSource.Passive, "Pressure Point") }

            },

            new PassiveDef

            {

                id = "unbroken_flow", name = "Unbroken Flow", classId = "martial_artist", tier = 4,

                description = "+8 Physical Power, +5 Initiative.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Unbroken Flow"), StatModifier.ForDerived(DerivedStatId.Initiative, 5, ModifierKind.Flat, ModifierSource.Passive, "Unbroken Flow") }

            },

            new PassiveDef

            {

                id = "ki_control", name = "Ki Control", classId = "martial_artist", tier = 4,

                description = "+10 Max MP, +4 Physical Defense, +5 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 10, ModifierKind.Flat, ModifierSource.Passive, "Ki Control"), StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 4, ModifierKind.Flat, ModifierSource.Passive, "Ki Control"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Passive, "Ki Control") }

            },

            new PassiveDef

            {

                id = "grandmaster", name = "Grandmaster", classId = "martial_artist", tier = 5,

                description = "+10 Physical Power, +10 Physical Break, +1 DEX.",

                abilityBonuses = new AbilityScores(0, 1, 0, 0, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Grandmaster"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Grandmaster") }

            },

            new PassiveDef

            {

                id = "limitless_potential", name = "Limitless Potential", classId = "martial_artist", tier = 5,

                description = "+12% Critical Chance, +8 Evasion, +10% QTE Timing.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 12, ModifierKind.Flat, ModifierSource.Passive, "Limitless Potential"), StatModifier.ForDerived(DerivedStatId.Evasion, 8, ModifierKind.Flat, ModifierSource.Passive, "Limitless Potential"), StatModifier.ForDerived(DerivedStatId.QteTimingBonus, 10, ModifierKind.Flat, ModifierSource.Passive, "Limitless Potential") }

            },

            new PassiveDef

            {

                id = "evasive_instinct", name = "Evasive Instinct", classId = "rogue", tier = 1,

                description = "+6 Evasion, +3 Initiative.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 6, ModifierKind.Flat, ModifierSource.Passive, "Evasive Instinct"), StatModifier.ForDerived(DerivedStatId.Initiative, 3, ModifierKind.Flat, ModifierSource.Passive, "Evasive Instinct") }

            },

            new PassiveDef

            {

                id = "precision", name = "Precision", classId = "rogue", tier = 2,

                description = "+8% Critical Chance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 8, ModifierKind.Flat, ModifierSource.Passive, "Precision") }

            },

            new PassiveDef

            {

                id = "dirty_fighting", name = "Dirty Fighting", classId = "rogue", tier = 2,

                description = "+4 Physical Power, +4 Physical Break vs debuffed foes.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Dirty Fighting"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Dirty Fighting") }

            },

            new PassiveDef

            {

                id = "lethal_focus", name = "Lethal Focus", classId = "rogue", tier = 3,

                description = "+6 Physical Power, +5% Critical Chance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Lethal Focus"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Passive, "Lethal Focus") }

            },

            new PassiveDef

            {

                id = "exploit_weakness", name = "Exploit Weakness", classId = "rogue", tier = 3,

                description = "+8% Critical Chance, +15% Damage vs Broken enemies.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 8, ModifierKind.Flat, ModifierSource.Passive, "Exploit Weakness") }

            },

            new PassiveDef

            {

                id = "shadow_affinity", name = "Shadow Affinity", classId = "rogue", tier = 4,

                description = "+8 Evasion, +5 Initiative, +10 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 8, ModifierKind.Flat, ModifierSource.Passive, "Shadow Affinity"), StatModifier.ForDerived(DerivedStatId.Initiative, 5, ModifierKind.Flat, ModifierSource.Passive, "Shadow Affinity"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Shadow Affinity") }

            },

            new PassiveDef

            {

                id = "executioner", name = "Executioner", classId = "rogue", tier = 4,

                description = "+20% Critical Chance against enemies below 35% HP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 20, ModifierKind.Flat, ModifierSource.Passive, "Executioner") }

            },

            new PassiveDef

            {

                id = "master_assassin", name = "Master Assassin", classId = "rogue", tier = 5,

                description = "+10 Physical Power, +12% Critical Chance, +1 DEX.",

                abilityBonuses = new AbilityScores(0, 1, 0, 0, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Master Assassin"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 12, ModifierKind.Flat, ModifierSource.Passive, "Master Assassin") }

            },

            new PassiveDef

            {

                id = "deaths_embrace", name = "Death's Embrace", classId = "rogue", tier = 5,

                description = "+10% Critical Chance, +4 Initiative, +20 Max HP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 10, ModifierKind.Flat, ModifierSource.Passive, "Death's Embrace"), StatModifier.ForDerived(DerivedStatId.Initiative, 4, ModifierKind.Flat, ModifierSource.Passive, "Death's Embrace"), StatModifier.ForDerived(DerivedStatId.MaxHP, 20, ModifierKind.Flat, ModifierSource.Passive, "Death's Embrace") }

            },

            new PassiveDef

            {

                id = "keen_eye", name = "Keen Eye", classId = "ranger", tier = 1,

                description = "+8 Ranged Accuracy, +4% Critical Chance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.ArmorPenetration, 8, ModifierKind.Flat, ModifierSource.Passive, "Keen Eye"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 4, ModifierKind.Flat, ModifierSource.Passive, "Keen Eye") }

            },

            new PassiveDef

            {

                id = "marksmanship", name = "Marksmanship", classId = "ranger", tier = 2,

                description = "+5 Ranged Power, +4 Ranged Accuracy.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.RangedPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Marksmanship"), StatModifier.ForDerived(DerivedStatId.ArmorPenetration, 4, ModifierKind.Flat, ModifierSource.Passive, "Marksmanship") }

            },

            new PassiveDef

            {

                id = "predators_focus", name = "Predator's Focus", classId = "ranger", tier = 2,

                description = "+6 Ranged Power vs Marked quarry.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.RangedPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Predator's Focus") }

            },

            new PassiveDef

            {

                id = "deadly_aim", name = "Deadly Aim", classId = "ranger", tier = 3,

                description = "+10% Critical Chance with ranged attacks.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 10, ModifierKind.Flat, ModifierSource.Passive, "Deadly Aim") }

            },

            new PassiveDef

            {

                id = "improved_tracking", name = "Improved Tracking", classId = "ranger", tier = 3,

                description = "+4 Initiative, +10 Ranged Accuracy.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Initiative, 4, ModifierKind.Flat, ModifierSource.Passive, "Improved Tracking"), StatModifier.ForDerived(DerivedStatId.ArmorPenetration, 10, ModifierKind.Flat, ModifierSource.Passive, "Improved Tracking") }

            },

            new PassiveDef

            {

                id = "eagle_eye", name = "Eagle Eye", classId = "ranger", tier = 4,

                description = "+15 Ranged Accuracy, +6 Ranged Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.ArmorPenetration, 15, ModifierKind.Flat, ModifierSource.Passive, "Eagle Eye"), StatModifier.ForDerived(DerivedStatId.RangedPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Eagle Eye") }

            },

            new PassiveDef

            {

                id = "tactical_hunter", name = "Tactical Hunter", classId = "ranger", tier = 4,

                description = "+8 Evasion, +5 Physical Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 8, ModifierKind.Flat, ModifierSource.Passive, "Tactical Hunter"), StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 5, ModifierKind.Flat, ModifierSource.Passive, "Tactical Hunter") }

            },

            new PassiveDef

            {

                id = "master_hunter", name = "Master Hunter", classId = "ranger", tier = 5,

                description = "+12 Ranged Power, +15 Ranged Accuracy, +1 DEX.",

                abilityBonuses = new AbilityScores(0, 1, 0, 0, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.RangedPower, 12, ModifierKind.Flat, ModifierSource.Passive, "Master Hunter"), StatModifier.ForDerived(DerivedStatId.ArmorPenetration, 15, ModifierKind.Flat, ModifierSource.Passive, "Master Hunter") }

            },

            new PassiveDef

            {

                id = "apex_predator", name = "Apex Predator", classId = "ranger", tier = 5,

                description = "+12% Critical Chance, +8 Ranged Power, +4 Initiative.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 12, ModifierKind.Flat, ModifierSource.Passive, "Apex Predator"), StatModifier.ForDerived(DerivedStatId.RangedPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Apex Predator"), StatModifier.ForDerived(DerivedStatId.Initiative, 4, ModifierKind.Flat, ModifierSource.Passive, "Apex Predator") }

            },

            new PassiveDef

            {

                id = "sacred_vitality", name = "Sacred Vitality", classId = "cleric", tier = 1,

                description = "+20 Max HP, +3 Healing Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxHP, 20, ModifierKind.Flat, ModifierSource.Passive, "Sacred Vitality"), StatModifier.ForDerived(DerivedStatId.HealingPower, 3, ModifierKind.Flat, ModifierSource.Passive, "Sacred Vitality") }

            },

            new PassiveDef

            {

                id = "healing_hands", name = "Healing Hands", classId = "cleric", tier = 2,

                description = "+6 Healing Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Healing Hands") }

            },

            new PassiveDef

            {

                id = "protective_faith", name = "Protective Faith", classId = "cleric", tier = 2,

                description = "+5 Physical Defense, +5 Magic Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 5, ModifierKind.Flat, ModifierSource.Passive, "Protective Faith"), StatModifier.ForDerived(DerivedStatId.MagicDefense, 5, ModifierKind.Flat, ModifierSource.Passive, "Protective Faith") }

            },

            new PassiveDef

            {

                id = "divine_aegis", name = "Divine Aegis", classId = "cleric", tier = 3,

                description = "+8 Block Power, +4 Magic Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.BlockPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Divine Aegis"), StatModifier.ForDerived(DerivedStatId.MagicDefense, 4, ModifierKind.Flat, ModifierSource.Passive, "Divine Aegis") }

            },

            new PassiveDef

            {

                id = "spiritual_focus", name = "Spiritual Focus", classId = "cleric", tier = 3,

                description = "+5 Spell Power, +5 Healing Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Spiritual Focus"), StatModifier.ForDerived(DerivedStatId.HealingPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Spiritual Focus") }

            },

            new PassiveDef

            {

                id = "blessed_recovery", name = "Blessed Recovery", classId = "cleric", tier = 4,

                description = "+10 Healing Power, +15 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Blessed Recovery"), StatModifier.ForDerived(DerivedStatId.MaxMP, 15, ModifierKind.Flat, ModifierSource.Passive, "Blessed Recovery") }

            },

            new PassiveDef

            {

                id = "unwavering_faith", name = "Unwavering Faith", classId = "cleric", tier = 4,

                description = "+15 Status Resistance, +6 Magic Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 15, ModifierKind.Flat, ModifierSource.Passive, "Unwavering Faith"), StatModifier.ForDerived(DerivedStatId.MagicDefense, 6, ModifierKind.Flat, ModifierSource.Passive, "Unwavering Faith") }

            },

            new PassiveDef

            {

                id = "saints_resolve", name = "Saint's Resolve", classId = "cleric", tier = 5,

                description = "+10 Magic Defense, +8 Physical Defense, +1 WIS.",

                abilityBonuses = new AbilityScores(0, 0, 0, 0, 1, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicDefense, 10, ModifierKind.Flat, ModifierSource.Passive, "Saint's Resolve"), StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 8, ModifierKind.Flat, ModifierSource.Passive, "Saint's Resolve") }

            },

            new PassiveDef

            {

                id = "eternal_blessing", name = "Eternal Blessing", classId = "cleric", tier = 5,

                description = "+12 Healing Power, +25 Max HP, +20 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 12, ModifierKind.Flat, ModifierSource.Passive, "Eternal Blessing"), StatModifier.ForDerived(DerivedStatId.MaxHP, 25, ModifierKind.Flat, ModifierSource.Passive, "Eternal Blessing"), StatModifier.ForDerived(DerivedStatId.MaxMP, 20, ModifierKind.Flat, ModifierSource.Passive, "Eternal Blessing") }

            },

            new PassiveDef

            {

                id = "elemental_knowledge", name = "Elemental Knowledge", classId = "wizard", tier = 1,

                description = "+4 Spell Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Elemental Knowledge") }

            },

            new PassiveDef

            {

                id = "spell_mastery", name = "Spell Mastery", classId = "wizard", tier = 2,

                description = "+6 Spell Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Spell Mastery") }

            },

            new PassiveDef

            {

                id = "mana_efficiency", name = "Mana Efficiency", classId = "wizard", tier = 2,

                description = "+10 Max MP, +3 Magic Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 10, ModifierKind.Flat, ModifierSource.Passive, "Mana Efficiency"), StatModifier.ForDerived(DerivedStatId.MagicDefense, 3, ModifierKind.Flat, ModifierSource.Passive, "Mana Efficiency") }

            },

            new PassiveDef

            {

                id = "elemental_focus", name = "Elemental Focus", classId = "wizard", tier = 3,

                description = "+8 Spell Power, +4% Critical Chance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Elemental Focus"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 4, ModifierKind.Flat, ModifierSource.Passive, "Elemental Focus") }

            },

            new PassiveDef

            {

                id = "arcane_precision", name = "Arcane Precision", classId = "wizard", tier = 3,

                description = "+10 Status Resistance, +5% Critical Chance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Arcane Precision"), StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Passive, "Arcane Precision") }

            },

            new PassiveDef

            {

                id = "spell_amplification", name = "Spell Amplification", classId = "wizard", tier = 4,

                description = "+10 Spell Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Spell Amplification") }

            },

            new PassiveDef

            {

                id = "magical_resilience", name = "Magical Resilience", classId = "wizard", tier = 4,

                description = "+8 Magic Defense, +15 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicDefense, 8, ModifierKind.Flat, ModifierSource.Passive, "Magical Resilience"), StatModifier.ForDerived(DerivedStatId.MaxMP, 15, ModifierKind.Flat, ModifierSource.Passive, "Magical Resilience") }

            },

            new PassiveDef

            {

                id = "archmage", name = "Archmage", classId = "wizard", tier = 5,

                description = "+15 Spell Power, +1 INT.",

                abilityBonuses = new AbilityScores(0, 0, 0, 1, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 15, ModifierKind.Flat, ModifierSource.Passive, "Archmage") }

            },

            new PassiveDef

            {

                id = "infinite_potential", name = "Infinite Potential", classId = "wizard", tier = 5,

                description = "+25 Max MP, +8 Spell Power, +10 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MaxMP, 25, ModifierKind.Flat, ModifierSource.Passive, "Infinite Potential"), StatModifier.ForDerived(DerivedStatId.MagicPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Infinite Potential"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Infinite Potential") }

            },

            new PassiveDef

            {

                id = "lingering_curse", name = "Lingering Curse", classId = "warlock", tier = 1,

                description = "+4 Dark Spell Power, +5 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Lingering Curse"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Passive, "Lingering Curse") }

            },

            new PassiveDef

            {

                id = "curse_mastery", name = "Curse Mastery", classId = "warlock", tier = 2,

                description = "+6 Spell Power for Curse abilities.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Curse Mastery") }

            },

            new PassiveDef

            {

                id = "dark_knowledge", name = "Dark Knowledge", classId = "warlock", tier = 2,

                description = "+5 Spell Power, +4 Magic Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Dark Knowledge"), StatModifier.ForDerived(DerivedStatId.MagicDefense, 4, ModifierKind.Flat, ModifierSource.Passive, "Dark Knowledge") }

            },

            new PassiveDef

            {

                id = "malicious_intent", name = "Malicious Intent", classId = "warlock", tier = 3,

                description = "+8% Critical Chance vs Cursed enemies.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 8, ModifierKind.Flat, ModifierSource.Passive, "Malicious Intent") }

            },

            new PassiveDef

            {

                id = "dark_ritual", name = "Dark Ritual", classId = "warlock", tier = 3,

                description = "+6 Spell Power, +10 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Dark Ritual"), StatModifier.ForDerived(DerivedStatId.MaxMP, 10, ModifierKind.Flat, ModifierSource.Passive, "Dark Ritual") }

            },

            new PassiveDef

            {

                id = "eternal_affliction", name = "Eternal Affliction", classId = "warlock", tier = 4,

                description = "+8 Spell Power, +15 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Eternal Affliction"), StatModifier.ForDerived(DerivedStatId.MaxMP, 15, ModifierKind.Flat, ModifierSource.Passive, "Eternal Affliction") }

            },

            new PassiveDef

            {

                id = "forbidden_power", name = "Forbidden Power", classId = "warlock", tier = 4,

                description = "+10 Dark Spell Power, +6 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Forbidden Power"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 6, ModifierKind.Flat, ModifierSource.Passive, "Forbidden Power") }

            },

            new PassiveDef

            {

                id = "grand_curse_master", name = "Grand Curse Master", classId = "warlock", tier = 5,

                description = "+12 Spell Power, +1 INT.",

                abilityBonuses = new AbilityScores(0, 0, 0, 1, 0, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 12, ModifierKind.Flat, ModifierSource.Passive, "Grand Curse Master") }

            },

            new PassiveDef

            {

                id = "abyssal_dominion", name = "Abyssal Dominion", classId = "warlock", tier = 5,

                description = "+10 Dark Spell Power, +25 Max HP, +15 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Abyssal Dominion"), StatModifier.ForDerived(DerivedStatId.MaxHP, 25, ModifierKind.Flat, ModifierSource.Passive, "Abyssal Dominion"), StatModifier.ForDerived(DerivedStatId.MaxMP, 15, ModifierKind.Flat, ModifierSource.Passive, "Abyssal Dominion") }

            },

            new PassiveDef

            {

                id = "natural_recovery", name = "Natural Recovery", classId = "druid", tier = 1,

                description = "+4 Healing Power, +10 Max HP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Natural Recovery"), StatModifier.ForDerived(DerivedStatId.MaxHP, 10, ModifierKind.Flat, ModifierSource.Passive, "Natural Recovery") }

            },

            new PassiveDef

            {

                id = "thorn_mastery", name = "Thorn Mastery", classId = "druid", tier = 2,

                description = "+5 Spell Power for Nature attacks.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 5, ModifierKind.Flat, ModifierSource.Passive, "Thorn Mastery") }

            },

            new PassiveDef

            {

                id = "deep_roots", name = "Deep Roots", classId = "druid", tier = 2,

                description = "+4 Break Power, +5 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 4, ModifierKind.Flat, ModifierSource.Passive, "Deep Roots"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Passive, "Deep Roots") }

            },

            new PassiveDef

            {

                id = "flourishing_life", name = "Flourishing Life", classId = "druid", tier = 3,

                description = "+6 Healing Power, +15 Max HP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Flourishing Life"), StatModifier.ForDerived(DerivedStatId.MaxHP, 15, ModifierKind.Flat, ModifierSource.Passive, "Flourishing Life") }

            },

            new PassiveDef

            {

                id = "wild_resilience", name = "Wild Resilience", classId = "druid", tier = 3,

                description = "+5 Physical Defense, +5 Magic Defense.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 5, ModifierKind.Flat, ModifierSource.Passive, "Wild Resilience"), StatModifier.ForDerived(DerivedStatId.MagicDefense, 5, ModifierKind.Flat, ModifierSource.Passive, "Wild Resilience") }

            },

            new PassiveDef

            {

                id = "ancient_knowledge", name = "Ancient Knowledge", classId = "druid", tier = 4,

                description = "+6 Spell Power, +6 Healing Power.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Ancient Knowledge"), StatModifier.ForDerived(DerivedStatId.HealingPower, 6, ModifierKind.Flat, ModifierSource.Passive, "Ancient Knowledge") }

            },

            new PassiveDef

            {

                id = "natures_protection", name = "Nature's Protection", classId = "druid", tier = 4,

                description = "+8 Physical Defense, +10 Status Resistance.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 8, ModifierKind.Flat, ModifierSource.Passive, "Nature's Protection"), StatModifier.ForDerived(DerivedStatId.StatusResistance, 10, ModifierKind.Flat, ModifierSource.Passive, "Nature's Protection") }

            },

            new PassiveDef

            {

                id = "master_of_the_wild", name = "Master of the Wild", classId = "druid", tier = 5,

                description = "+10 Spell Power, +8 Break Power, +1 WIS.",

                abilityBonuses = new AbilityScores(0, 0, 0, 0, 1, 0),

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 10, ModifierKind.Flat, ModifierSource.Passive, "Master of the Wild"), StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 8, ModifierKind.Flat, ModifierSource.Passive, "Master of the Wild") }

            },

            new PassiveDef

            {

                id = "eternal_bloom", name = "Eternal Bloom", classId = "druid", tier = 5,

                description = "+12 Healing Power, +20 Max HP, +15 Max MP.",

                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.HealingPower, 12, ModifierKind.Flat, ModifierSource.Passive, "Eternal Bloom"), StatModifier.ForDerived(DerivedStatId.MaxHP, 20, ModifierKind.Flat, ModifierSource.Passive, "Eternal Bloom"), StatModifier.ForDerived(DerivedStatId.MaxMP, 15, ModifierKind.Flat, ModifierSource.Passive, "Eternal Bloom") }

            },

}, p => p.id);



        public static readonly AbilityScoreImprovementConfig AsiConfig = new AbilityScoreImprovementConfig

        {

            availableAtLevels = new[] { 4, 8, 12, 16, 19 },

            pointsGranted = 2,

            maxAbilityScore = 20

        };



        // ---- 9. QTE Stat Interactions ----

        public static readonly Dictionary<Ability, QteStatInteractionDef> QteStatInteractions = new Dictionary<Ability, QteStatInteractionDef>

        {

            [Ability.DEX] = new QteStatInteractionDef { ability = Ability.DEX, effectName = "Timing Window Bonus", description = "Small bonus to applicable QTE timing windows.", scalingPerModifier = 0.03f, maxBonus = 0.15f },

            [Ability.VIT] = new QteStatInteractionDef { ability = Ability.VIT, effectName = "Reaction Stability", description = "Reduces selected failure and partial reaction penalties.", scalingPerModifier = 0.04f, maxBonus = 0.25f },

            [Ability.STR] = new QteStatInteractionDef { ability = Ability.STR, effectName = "Physical Counter Force", description = "Improves physical counterattack and perfect strike potency.", scalingPerModifier = 0.04f, maxBonus = 0.25f },

            [Ability.INT] = new QteStatInteractionDef { ability = Ability.INT, effectName = "Incantation Focus", description = "Grants slight extra letter time on spell reaction QTEs.", scalingPerModifier = 0.03f, maxBonus = 0.15f },

            [Ability.WIS] = new QteStatInteractionDef { ability = Ability.WIS, effectName = "Mental Resilience", description = "Helps against magical or mental enemy attack reactions.", scalingPerModifier = 0.03f, maxBonus = 0.15f },

            [Ability.CHA] = new QteStatInteractionDef { ability = Ability.CHA, effectName = "Command Presence", description = "Improves command and support skill effectiveness.", scalingPerModifier = 0.04f, maxBonus = 0.20f },

        };



        // ---- 10. Active Skills ----

        public static readonly Dictionary<string, SkillDef> Skills = Index(new[]

        {

            // Fighter / Knight (Complete 15-Skill Tree)

            // Tier 1 (Level 1)

            new SkillDef { id = "shield_bash", name = "Shield Bash", classId = "fighter", branchIndex = 1, tier = 1, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.0f,

                scalingAbility = Ability.STR, breakMultiplier = 1.4f, bonusVsBroken = 0.25f, requiresShield = true, cooldown = 1,

                apply = new[] { Status(StatusId.DefenseDown, 2, 0.25f), Status(StatusId.Stun, 1, 1f, chance: 0.35f) },

                description = "Strike with shield dealing Break damage with a chance to stun and reduce defense." },

            new SkillDef { id = "power_strike", name = "Power Strike", classId = "fighter", branchIndex = 0, tier = 1, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 3, power = 1.35f,

                scalingAbility = Ability.STR, breakMultiplier = 1.15f, perfectPower = 1.75f, critChanceBonus = 0.10f, cooldown = 0,

                description = "Committed weapon strike dealing heavy Physical damage with high critical potential." },

            new SkillDef { id = "guard_stance", name = "Guard Stance", classId = "fighter", branchIndex = 1, tier = 1, kind = SkillKind.Support, target = TargetType.Self, mpCost = 4, cooldown = 3,

                scalingAbility = Ability.VIT, apply = new[] { Status(StatusId.DefenseUp, 2, 0.45f) },

                description = "Brace in a defensive stance (DEF+ and mitigation for 2 turns)." },

            new SkillDef { id = "fortify", name = "Fortify", classId = "fighter", kind = SkillKind.Support, target = TargetType.Self, mpCost = 5, cooldown = 3,

                scalingAbility = Ability.CHA, apply = new[] { Status(StatusId.DefenseUp, 2, 0.6f) }, description = "Brace behind the shield (DEF+ 2 turns)." },

            new SkillDef { id = "interpose", name = "Interpose", classId = "fighter", kind = SkillKind.Support, target = TargetType.Ally, mpCost = 4, cooldown = 2,

                scalingAbility = Ability.CHA, apply = new[] { Status(StatusId.Guardian, 1, 1f, onSelf: true) }, description = "Take an ally's single-target hits until next turn." },



            // Tier 2 (Level 4)

            new SkillDef { id = "cleaving_strike", name = "Cleaving Strike", classId = "fighter", branchIndex = 0, tier = 2, kind = SkillKind.Physical, target = TargetType.AllEnemies, mpCost = 6, power = 1.0f,

                scalingAbility = Ability.STR, breakMultiplier = 1.2f, cooldown = 1, prerequisites = new[] { "power_strike" },

                description = "Sweeping weapon technique hitting all foes with crushing force." },

            new SkillDef { id = "shield_wall", name = "Shield Wall", classId = "fighter", branchIndex = 1, tier = 2, kind = SkillKind.Support, target = TargetType.AllAllies, mpCost = 6, cooldown = 3,

                scalingAbility = Ability.VIT, requiresShield = true, prerequisites = new[] { "guard_stance" },

                apply = new[] { Status(StatusId.DefenseUp, 2, 0.35f), Status(StatusId.Guardian, 1, 1f, onSelf: true) },

                description = "Plant shield to protect the party (DEF+ and intercept ally hits)." },

            new SkillDef { id = "weapon_disarm", name = "Weapon Disarm", classId = "fighter", branchIndex = 2, tier = 2, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 5, power = 0.9f,

                scalingAbility = Ability.STR, cooldown = 2,

                apply = new[] { Status(StatusId.AttackDown, 2, 0.40f, chance: 0.85f) },

                description = "Target enemy weapon arm, dealing damage and heavily reducing Physical attack (ATK-)." },



            // Tier 3 (Level 7)

            new SkillDef { id = "counterattack", name = "Counterattack", classId = "fighter", branchIndex = 2, tier = 3, kind = SkillKind.Support, target = TargetType.Self, mpCost = 5, cooldown = 2,

                scalingAbility = Ability.STR, prerequisites = new[] { "weapon_disarm" },

                apply = new[] { Status(StatusId.Evasion, 2, 1.5f), Status(StatusId.AttackUp, 2, 0.25f) },

                description = "Enter a retaliatory guard, widening reaction parry window and boosting counter power." },

            new SkillDef { id = "armor_break", name = "Armor Break", classId = "fighter", branchIndex = 0, tier = 3, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 6, power = 1.25f,

                scalingAbility = Ability.STR, breakMultiplier = 1.5f, cooldown = 2, prerequisites = new[] { "cleaving_strike" },

                apply = new[] { Status(StatusId.DefenseDown, 3, 0.45f) },

                description = "Shatter enemy armor dealing high damage and severely crippling Physical Defense (DEF- 3 turns)." },

            new SkillDef { id = "tactical_strike", name = "Tactical Strike", classId = "fighter", branchIndex = 2, tier = 3, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 5, power = 1.35f,

                scalingAbility = Ability.STR, bonusVsBroken = 0.5f, cooldown = 1, prerequisites = new[] { "weapon_disarm" },

                description = "Precise tactical strike dealing massive bonus damage against Broken or debuffed foes." },



            // Tier 4 (Level 10)

            new SkillDef { id = "whirlwind", name = "Whirlwind", classId = "fighter", branchIndex = 0, tier = 4, kind = SkillKind.Physical, target = TargetType.AllEnemies, mpCost = 9, power = 1.4f,

                scalingAbility = Ability.STR, breakMultiplier = 1.35f, cooldown = 2, prerequisites = new[] { "armor_break" },

                description = "Spinning 360-degree blade tempest dealing crushing Physical damage to all foes." },

            new SkillDef { id = "iron_fortress", name = "Iron Fortress", classId = "fighter", branchIndex = 1, tier = 4, kind = SkillKind.Support, target = TargetType.Self, mpCost = 8, cooldown = 4,

                scalingAbility = Ability.VIT, requiresShield = true, prerequisites = new[] { "shield_wall" },

                apply = new[] { Status(StatusId.DefenseUp, 2, 0.75f), Status(StatusId.Guardian, 2, 1f, onSelf: true) },

                description = "Become an immovable fortress: massive Physical Defense (+75%) and intercepts hits for 2 turns." },

            new SkillDef { id = "execution_strike", name = "Execution Strike", classId = "fighter", branchIndex = 2, tier = 4, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 7, power = 1.7f,

                scalingAbility = Ability.STR, breakMultiplier = 1.4f, bonusVsBroken = 0.6f, cooldown = 2, prerequisites = new[] { "tactical_strike" },

                description = "Brutal execution strike that annihilates vulnerable or Broken targets." },



            // Tier 5 (Level 13)

            new SkillDef { id = "blade_tempest", name = "Blade Tempest", classId = "fighter", branchIndex = 0, tier = 5, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 12, power = 0.6f, hits = 5,

                scalingAbility = Ability.STR, breakMultiplier = 1.6f, perfectPower = 2.0f, cooldown = 3, prerequisites = new[] { "whirlwind" },

                description = "Unleash a flurry of 5 rapid, devastating weapon strikes with high critical multiplier." },

            new SkillDef { id = "unbreakable", name = "Unbreakable", classId = "fighter", branchIndex = 1, tier = 5, kind = SkillKind.Support, target = TargetType.Self, mpCost = 10, cooldown = 5,

                scalingAbility = Ability.VIT, maxRank = 1, prerequisites = new[] { "iron_fortress" },

                apply = new[] { Status(StatusId.DefenseUp, 3, 1.0f) },

                description = "Apex defensive mastery: doubles Physical Defense (+100%) and withstands extreme pressure for 3 turns." },

            new SkillDef { id = "final_verdict", name = "Final Verdict", classId = "fighter", branchIndex = 2, tier = 5, kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 14, power = 2.8f,

                scalingAbility = Ability.STR, breakMultiplier = 2.0f, bonusVsBroken = 0.75f, cooldown = 4, prerequisites = new[] { "execution_strike" },

                description = "Cataclysmic single-target finisher dealing immense Physical and Break damage." },



            // Barbarian

            new SkillDef { id = "reckless_cleave", name = "Reckless Cleave", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.35f,

                scalingAbility = Ability.STR, breakMultiplier = 1.4f, bonusVsBroken = 0.3f, description = "Heavy berserk chop that crushes enemy posture." },

            new SkillDef { id = "war_cry", name = "War Cry", kind = SkillKind.Support, target = TargetType.Self, mpCost = 4, cooldown = 3,

                scalingAbility = Ability.STR, apply = new[] { Status(StatusId.AttackUp, 2, 0.35f) }, description = "Unleash a furious roar (ATK+ 2 turns)." },







            // Rogue

            new SkillDef { id = "quick_strike", name = "Quick Strike", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 3, power = 0.9f, perfectPower = 1.75f,

                scalingAbility = Ability.STR, breakMultiplier = 1.0f, bonusVsBroken = 0.3f,

                description = "Perfect timing lands a critical." },

            new SkillDef { id = "expose", name = "Expose", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 0.5f,

                scalingAbility = Ability.DEX, breakMultiplier = 1.25f,

                apply = new[] { Status(StatusId.DefenseDown, 2, 0.4f) }, description = "Find the gap in its armor (DEF-)." },

            new SkillDef { id = "evasion", name = "Evasion", kind = SkillKind.Support, target = TargetType.Self, mpCost = 3, cooldown = 2,

                scalingAbility = Ability.DEX,

                apply = new[] { Status(StatusId.Evasion, 2, 1.75f) }, description = "Wider guard windows for 2 turns." },



            // Ranger (Ranged Active Skills)

            new SkillDef { id = "aimed_shot", name = "Aimed Shot", kind = SkillKind.Physical, isRanged = true, target = TargetType.Enemy, mpCost = 4, power = 1.25f, perfectPower = 1.8f,

                scalingAbility = Ability.DEX, breakMultiplier = 1.15f, tags = new[] { "Ranged", "HuntMark" },

                description = "Precise ranged arrow shot scaling with Ranged Power and Ranged Accuracy." },

            new SkillDef { id = "piercing_volley", name = "Piercing Volley", kind = SkillKind.Physical, isRanged = true, target = TargetType.AllEnemies, mpCost = 7, power = 0.85f,

                scalingAbility = Ability.DEX, breakMultiplier = 1.1f, tags = new[] { "Ranged" },

                description = "Looses a volley of arrows across all enemies." },





            new SkillDef { id = "arcane_missile", name = "Arcane Missile", kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.Enemy, mpCost = 5, power = 1.2f,

                scalingAbility = Ability.INT, tier = 2, tags = new[] { "Arcane" },

                word = "LUX", description = "Unerring bolt of pure arcane energy." },



            // Warlock (Pact Skills)

            new SkillDef { id = "eldritch_pact", name = "Eldritch Blast", kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, mpCost = 6, power = 1.5f,

                scalingAbility = Ability.INT, breakMultiplier = 1.25f, tags = new[] { "Pact" },

                word = "UMBRA", description = "Pact-forged bolt of abyssal energy." },

            new SkillDef { id = "soul_hex", name = "Soul Hex", kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, mpCost = 7, power = 1.1f,

                scalingAbility = Ability.INT, tags = new[] { "Pact" },

                word = "MALEDICT", apply = new[] { Status(StatusId.MagicDefenseDown, 2, 0.35f) }, description = "Pact curse that erodes enemy Magic Defense." },



            // Druid (Nature Skills)

            new SkillDef { id = "thorn_whip", name = "Thorn Whip", kind = SkillKind.Magic, element = Element.Physical, target = TargetType.Enemy, mpCost = 5, power = 1.3f,

                scalingAbility = Ability.WIS, breakMultiplier = 1.3f, tags = new[] { "Nature" },

                word = "SYLVA", description = "Lashing briars empowered by Nature's Attunement." },







        



            // ---- Barbarian (barbarian) Complete 15-Skill Tree ----

            new SkillDef { id = "savage_strike", name = "Savage Strike", classId = "barbarian", branchIndex = 0, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 3, power = 1.30f, breakMultiplier = 1.20f, maxRank = 3, critChanceBonus = 0.10f, description = "A brutal basic strike dealing Physical damage." },

            new SkillDef { id = "berserk", name = "Berserk", classId = "barbarian", branchIndex = 1, tier = 1, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.STR, mpCost = 5, power = 1.00f, breakMultiplier = 1.00f, maxRank = 1, cooldown = 3, apply = new[] { Status(StatusId.AttackUp, 3, 0.35f) }, description = "Activates Berserk state granting +35% ATK for 3 turns. Special Core Skill." },

            new SkillDef { id = "intimidating_roar", name = "Intimidating Roar", classId = "barbarian", branchIndex = 2, tier = 1, kind = SkillKind.Support, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.STR, mpCost = 4, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.AttackDown, 2, 0.25f) }, description = "Roar fiercely to lower all enemies' Attack for 2 turns." },

            new SkillDef { id = "crushing_blow", name = "Crushing Blow", classId = "barbarian", branchIndex = 0, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 4, power = 1.45f, breakMultiplier = 1.60f, maxRank = 3, critChanceBonus = 0.05f, prerequisites = new[] { "savage_strike" }, description = "Heavy crushing strike designed to shatter enemy Break gauges." },

            new SkillDef { id = "reckless_swing", name = "Reckless Swing", classId = "barbarian", branchIndex = 1, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 5, power = 1.70f, breakMultiplier = 1.30f, maxRank = 3, critChanceBonus = 0.15f, prerequisites = new[] { "berserk" }, description = "A wild swing dealing massive damage with high critical potential." },

            new SkillDef { id = "blood_rush", name = "Blood Rush", classId = "barbarian", branchIndex = 2, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 4, power = 1.15f, breakMultiplier = 1.15f, maxRank = 3, apply = new[] { Status(StatusId.AttackUp, 2, 0.20f) }, prerequisites = new[] { "intimidating_roar" }, description = "Rush forward with momentum, attacking and boosting Speed for 2 turns." },

            new SkillDef { id = "rampage", name = "Rampage", classId = "barbarian", branchIndex = 0, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 6, power = 1.85f, breakMultiplier = 1.40f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, prerequisites = new[] { "crushing_blow" }, description = "Performs a furious barrage of aggressive attacks." },

            new SkillDef { id = "blood_frenzy", name = "Blood Frenzy", classId = "barbarian", branchIndex = 1, tier = 3, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.STR, mpCost = 6, power = 1.00f, breakMultiplier = 1.00f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, apply = new[] { Status(StatusId.AttackUp, 2, 0.40f) }, prerequisites = new[] { "reckless_swing" }, description = "Channel unbridled frenzy (ATK+ and +15% Crit for 2 turns)." },

            new SkillDef { id = "brutal_cleave", name = "Brutal Cleave", classId = "barbarian", branchIndex = 2, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.STR, mpCost = 6, power = 1.10f, breakMultiplier = 1.30f, maxRank = 3, cooldown = 1, prerequisites = new[] { "blood_rush" }, description = "Sweep your heavy weapon in a wide arc across all foes." },

            new SkillDef { id = "devastating_smash", name = "Devastating Smash", classId = "barbarian", branchIndex = 0, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 8, power = 2.15f, breakMultiplier = 2.00f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.35f) }, prerequisites = new[] { "rampage" }, description = "Crushing downward smash with extreme Break power and stun chance." },

            new SkillDef { id = "death_rush", name = "Death Rush", classId = "barbarian", branchIndex = 1, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 7, power = 1.95f, breakMultiplier = 1.45f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 1, prerequisites = new[] { "blood_frenzy" }, description = "Forceful strike that deals extra damage against Broken enemies." },

            new SkillDef { id = "barbarian_war_cry", name = "War Cry", classId = "barbarian", branchIndex = 2, tier = 4, kind = SkillKind.Support, element = Element.Physical, target = TargetType.AllAllies, scalingAbility = Ability.STR, mpCost = 6, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.AttackUp, 2, 0.30f) }, prerequisites = new[] { "brutal_cleave" }, description = "Bellow an inspiring war cry granting Attack Up to all allies." },

            new SkillDef { id = "barbarian_cataclysm", name = "Cataclysm Slam", classId = "barbarian", branchIndex = 0, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 12, power = 2.75f, breakMultiplier = 2.50f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 3, prerequisites = new[] { "devastating_smash" }, description = "Cataclysmic earth-shattering blow with ultimate Break power." },

            new SkillDef { id = "wrath_incarnate", name = "Wrath Incarnate", classId = "barbarian", branchIndex = 1, tier = 5, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.STR, mpCost = 10, power = 1.00f, breakMultiplier = 1.00f, maxRank = 1, critChanceBonus = 0.20f, cooldown = 4, apply = new[] { Status(StatusId.AttackUp, 3, 0.50f) }, prerequisites = new[] { "death_rush" }, description = "Become the avatar of pure wrath (+50% ATK, +20% Crit for 3 turns)." },

            new SkillDef { id = "primal_carnage", name = "Primal Carnage", classId = "barbarian", branchIndex = 2, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.STR, mpCost = 11, power = 1.65f, breakMultiplier = 1.85f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, prerequisites = new[] { "barbarian_war_cry" }, description = "Overwhelming display of raw physical devastation across all enemies." },



            // ---- Martial Artist (martial_artist) Complete 15-Skill Tree ----

            new SkillDef { id = "quick_jab", name = "Quick Jab", classId = "martial_artist", branchIndex = 0, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 2, power = 1.05f, breakMultiplier = 1.30f, maxRank = 3, critChanceBonus = 0.10f, tags = new[] { "Unarmed", "Combo" }, description = "Fast unarmed strike building combo flow." },

            new SkillDef { id = "focused_breathing", name = "Focused Breathing", classId = "martial_artist", branchIndex = 1, tier = 1, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.WIS, mpCost = 3, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseUp, 2, 0.25f) }, tags = new[] { "Unarmed" }, description = "Center ki and regulate breath (DEF+ 2 turns, restores focus)." },

            new SkillDef { id = "rising_kick", name = "Rising Kick", classId = "martial_artist", branchIndex = 2, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 3, power = 1.25f, breakMultiplier = 1.45f, maxRank = 3, critChanceBonus = 0.05f, tags = new[] { "Unarmed", "Combo" }, description = "Upward kick launching into the air with strong Break power." },

            new SkillDef { id = "flowing_combo", name = "Flowing Combo", classId = "martial_artist", branchIndex = 0, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 4, power = 1.35f, breakMultiplier = 1.35f, maxRank = 3, critChanceBonus = 0.10f, prerequisites = new[] { "quick_jab" }, tags = new[] { "Unarmed", "Combo" }, description = "Fluid sequence of rapid strikes that sustains combo momentum." },

            new SkillDef { id = "palm_strike", name = "Splitting Palm", classId = "martial_artist", branchIndex = 1, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 4, power = 1.15f, breakMultiplier = 1.75f, maxRank = 3, prerequisites = new[] { "focused_breathing" }, tags = new[] { "Unarmed" }, description = "Focused palm strike that shatters enemy defenses." },

            new SkillDef { id = "sweep_kick", name = "Sweep Kick", classId = "martial_artist", branchIndex = 2, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.DEX, mpCost = 5, power = 0.90f, breakMultiplier = 1.25f, maxRank = 3, cooldown = 1, apply = new[] { Status(StatusId.Slow, 2, 0.25f, chance: 0.40f) }, prerequisites = new[] { "rising_kick" }, tags = new[] { "Unarmed", "Combo" }, description = "Low sweeping kick that trips and slows all foes." },

            new SkillDef { id = "spinning_kick", name = "Spinning Kick", classId = "martial_artist", branchIndex = 0, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 5, power = 1.55f, breakMultiplier = 1.50f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, prerequisites = new[] { "flowing_combo" }, tags = new[] { "Unarmed", "Combo" }, description = "Whirling roundhouse kick with high critical timing window." },

            new SkillDef { id = "counterstrike", name = "Counterstrike", classId = "martial_artist", branchIndex = 1, tier = 3, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.DEX, mpCost = 5, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.Evasion, 2, 1.5f) }, prerequisites = new[] { "palm_strike" }, tags = new[] { "Unarmed" }, description = "Adopt an evasive counter stance ready to retaliate." },

            new SkillDef { id = "dragon_fist", name = "Dragon Fist", classId = "martial_artist", branchIndex = 2, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 6, power = 1.85f, breakMultiplier = 1.90f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.30f) }, prerequisites = new[] { "sweep_kick" }, tags = new[] { "Unarmed", "Combo" }, description = "Devastating direct punch infused with dragon spirit." },

            new SkillDef { id = "hundred_fists", name = "Hundred Fists", classId = "martial_artist", branchIndex = 0, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 8, power = 2.25f, breakMultiplier = 1.85f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "spinning_kick" }, tags = new[] { "Unarmed", "Combo" }, description = "Blinding flurry of punches delivering relentless hits." },

            new SkillDef { id = "perfect_counter", name = "Perfect Counter", classId = "martial_artist", branchIndex = 1, tier = 4, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.DEX, mpCost = 6, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.Evasion, 1, 2.0f) }, prerequisites = new[] { "counterstrike" }, tags = new[] { "Unarmed" }, description = "Supreme evasion technique guaranteeing counter-reaction." },

            new SkillDef { id = "flying_knee", name = "Flying Knee", classId = "martial_artist", branchIndex = 2, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 7, power = 2.10f, breakMultiplier = 2.20f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, prerequisites = new[] { "dragon_fist" }, tags = new[] { "Unarmed", "Combo" }, description = "Acrobatic leap delivering a crushing knee to the skull." },

            new SkillDef { id = "ascendant_strike", name = "Ascendant Strike", classId = "martial_artist", branchIndex = 0, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 10, power = 2.45f, breakMultiplier = 2.10f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "hundred_fists" }, tags = new[] { "Unarmed", "Combo" }, description = "Transcendental unarmed technique delivering peerless precision." },

            new SkillDef { id = "final_technique", name = "Final Technique", classId = "martial_artist", branchIndex = 1, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 11, power = 2.65f, breakMultiplier = 2.40f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "perfect_counter" }, tags = new[] { "Unarmed" }, description = "Secret pinnacle technique of the ancient monastery." },

            new SkillDef { id = "dragons_fury", name = "Dragon's Fury", classId = "martial_artist", branchIndex = 2, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.STR, mpCost = 12, power = 2.95f, breakMultiplier = 2.65f, maxRank = 3, critChanceBonus = 0.20f, cooldown = 3, prerequisites = new[] { "flying_knee" }, tags = new[] { "Unarmed", "Combo" }, description = "Ultimate combo finisher unleashing the wrath of dragons." },



            // ---- Rogue (rogue) Complete 15-Skill Tree ----

            new SkillDef { id = "quick_slash", name = "Quick Slash", classId = "rogue", branchIndex = 0, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 3, power = 1.10f, breakMultiplier = 1.05f, maxRank = 3, critChanceBonus = 0.15f, description = "A swift dagger strike with high critical strike potential." },

            new SkillDef { id = "smoke_bomb", name = "Smoke Bomb", classId = "rogue", branchIndex = 1, tier = 1, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.DEX, mpCost = 4, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.Evasion, 2, 1.75f) }, description = "Conceal in blinding smoke granting massive Evasion for 2 turns." },

            new SkillDef { id = "backstab", name = "Backstab", classId = "rogue", branchIndex = 2, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 4, power = 1.45f, breakMultiplier = 1.15f, maxRank = 3, critChanceBonus = 0.20f, description = "Vicious strike exploiting enemy blind spots." },

            new SkillDef { id = "feint", name = "Feint", classId = "rogue", branchIndex = 0, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 3, power = 0.85f, breakMultiplier = 1.25f, maxRank = 3, critChanceBonus = 0.05f, apply = new[] { Status(StatusId.DefenseDown, 2, 0.35f) }, prerequisites = new[] { "quick_slash" }, description = "A deceptive maneuver that lowers target's Physical Defense." },

            new SkillDef { id = "shadow_strike", name = "Shadow Strike", classId = "rogue", branchIndex = 1, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 5, power = 1.40f, breakMultiplier = 1.20f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, apply = new[] { Status(StatusId.Evasion, 1, 1.5f) }, prerequisites = new[] { "smoke_bomb" }, description = "Strike from darkness dealing damage and stepping into shadows." },

            new SkillDef { id = "poison_blade", name = "Poison Blade", classId = "rogue", branchIndex = 2, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 4, power = 1.05f, breakMultiplier = 1.00f, maxRank = 3, critChanceBonus = 0.05f, apply = new[] { Status(StatusId.Poison, 3, 0.25f) }, prerequisites = new[] { "backstab" }, description = "Coat weapon with lethal venom causing Poison for 3 turns." },

            new SkillDef { id = "assassinate", name = "Assassinate", classId = "rogue", branchIndex = 0, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 6, power = 1.95f, breakMultiplier = 1.35f, maxRank = 3, critChanceBonus = 0.25f, cooldown = 1, prerequisites = new[] { "feint" }, description = "Lethal execution strike with devastating critical damage." },

            new SkillDef { id = "shadowstep", name = "Shadowstep", classId = "rogue", branchIndex = 1, tier = 3, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.DEX, mpCost = 4, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.Evasion, 2, 1.5f) }, prerequisites = new[] { "shadow_strike" }, description = "Instantly reposition through shadow (Speed+ and Evasion+ 2 turns)." },

            new SkillDef { id = "weakpoint_strike", name = "Weakpoint Strike", classId = "rogue", branchIndex = 2, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 5, power = 1.35f, breakMultiplier = 1.80f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 1, apply = new[] { Status(StatusId.DefenseDown, 2, 0.40f) }, prerequisites = new[] { "poison_blade" }, description = "Strike exposed nerve centers dealing high Break damage." },

            new SkillDef { id = "death_mark", name = "Death Mark", classId = "rogue", branchIndex = 0, tier = 4, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 5, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.DefenseDown, 3, 0.50f) }, prerequisites = new[] { "assassinate" }, description = "Mark target for death, severely weakening its defense for 3 turns." },

            new SkillDef { id = "vanishing_act", name = "Vanishing Act", classId = "rogue", branchIndex = 1, tier = 4, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Self, scalingAbility = Ability.DEX, mpCost = 6, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.Evasion, 1, 2.5f) }, prerequisites = new[] { "shadowstep" }, description = "Disappear completely from sight, evading all attacks for 1 turn." },

            new SkillDef { id = "phantom_dance", name = "Phantom Dance", classId = "rogue", branchIndex = 2, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 7, power = 2.05f, breakMultiplier = 1.55f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 1, prerequisites = new[] { "weakpoint_strike" }, description = "Perform an evasive blade dance delivering three rapid strikes." },

            new SkillDef { id = "thousand_cuts", name = "Thousand Cuts", classId = "rogue", branchIndex = 0, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 10, power = 2.55f, breakMultiplier = 1.85f, maxRank = 3, critChanceBonus = 0.20f, cooldown = 2, prerequisites = new[] { "death_mark" }, description = "Unleash a flurry of slashes cutting the target to ribbons." },

            new SkillDef { id = "shadow_requiem", name = "Shadow Requiem", classId = "rogue", branchIndex = 1, tier = 5, kind = SkillKind.Physical, element = Element.Dark, target = TargetType.AllEnemies, scalingAbility = Ability.DEX, mpCost = 9, power = 1.55f, breakMultiplier = 1.45f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.Slow, 2, 0.35f) }, prerequisites = new[] { "vanishing_act" }, description = "Reap all foes in an explosion of lethal shadow blades." },

            new SkillDef { id = "final_ambush", name = "Final Ambush", classId = "rogue", branchIndex = 2, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 11, power = 2.85f, breakMultiplier = 2.25f, maxRank = 3, critChanceBonus = 0.25f, cooldown = 3, prerequisites = new[] { "phantom_dance" }, description = "Ultimate opportunistic ambush dealing catastrophic exploit damage." },



            // ---- Ranger (ranger) Complete 15-Skill Tree ----

            new SkillDef { id = "quick_shot", name = "Quick Shot", classId = "ranger", branchIndex = 2, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 3, power = 1.15f, breakMultiplier = 1.10f, maxRank = 3, critChanceBonus = 0.10f, tags = new[] { "Ranged" }, isRanged = true, description = "Fast arrow release with reliable accuracy." },

            new SkillDef { id = "hunter_mark", name = "Hunter's Mark", classId = "ranger", branchIndex = 0, tier = 1, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 3, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseDown, 3, 0.25f) }, tags = new[] { "Ranged", "HuntMark" }, isRanged = true, description = "Mark quarry enabling increased Ranger damage for 3 turns." },

            new SkillDef { id = "evasive_shot", name = "Evasive Shot", classId = "ranger", branchIndex = 1, tier = 1, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 3, power = 0.95f, breakMultiplier = 1.05f, maxRank = 3, critChanceBonus = 0.05f, apply = new[] { Status(StatusId.Evasion, 1, 1.5f) }, tags = new[] { "Ranged" }, isRanged = true, description = "Loose an arrow while leaping back into cover." },

            new SkillDef { id = "piercing_arrow", name = "Piercing Arrow", classId = "ranger", branchIndex = 0, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 4, power = 1.45f, breakMultiplier = 1.50f, maxRank = 3, critChanceBonus = 0.10f, prerequisites = new[] { "hunter_mark" }, tags = new[] { "Ranged" }, isRanged = true, description = "Fires a reinforced arrow piercing through enemy armor." },

            new SkillDef { id = "crippling_shot", name = "Crippling Shot", classId = "ranger", branchIndex = 1, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 4, power = 1.10f, breakMultiplier = 1.20f, maxRank = 3, critChanceBonus = 0.05f, apply = new[] { Status(StatusId.Slow, 2, 0.40f) }, prerequisites = new[] { "evasive_shot" }, tags = new[] { "Ranged" }, isRanged = true, description = "Aim for tendons to inflict Slow on target for 2 turns." },

            new SkillDef { id = "volley", name = "Volley", classId = "ranger", branchIndex = 2, tier = 2, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.DEX, mpCost = 6, power = 0.85f, breakMultiplier = 1.15f, maxRank = 3, cooldown = 1, prerequisites = new[] { "quick_shot" }, tags = new[] { "Ranged" }, isRanged = true, description = "Fire a spread of arrows blanketing all enemies." },

            new SkillDef { id = "snipe", name = "Snipe", classId = "ranger", branchIndex = 0, tier = 3, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 6, power = 1.95f, breakMultiplier = 1.45f, maxRank = 3, critChanceBonus = 0.20f, cooldown = 1, prerequisites = new[] { "piercing_arrow" }, tags = new[] { "Ranged" }, isRanged = true, description = "Deeply focused precision shot delivering tremendous single-target damage." },

            new SkillDef { id = "trap_setup", name = "Trap Setup", classId = "ranger", branchIndex = 1, tier = 3, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 5, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.50f) }, prerequisites = new[] { "crippling_shot" }, description = "Deploy a spring-loaded snare to trap and stun the enemy." },

            new SkillDef { id = "explosive_arrow", name = "Explosive Arrow", classId = "ranger", branchIndex = 2, tier = 3, kind = SkillKind.Physical, element = Element.Fire, target = TargetType.AllEnemies, scalingAbility = Ability.DEX, mpCost = 7, power = 1.15f, breakMultiplier = 1.40f, maxRank = 3, critChanceBonus = 0.05f, cooldown = 1, prerequisites = new[] { "volley" }, tags = new[] { "Ranged" }, isRanged = true, description = "Loose an arrow packed with alchemical powder that detonates on impact." },

            new SkillDef { id = "shadow_shot", name = "Shadow Shot", classId = "ranger", branchIndex = 0, tier = 4, kind = SkillKind.Physical, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 7, power = 1.85f, breakMultiplier = 1.65f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 1, apply = new[] { Status(StatusId.AttackDown, 2, 0.35f) }, prerequisites = new[] { "snipe" }, tags = new[] { "Ranged" }, isRanged = true, description = "Infuse arrow with shadow to weaken enemy combat prowess." },

            new SkillDef { id = "pinning_shot", name = "Pinning Shot", classId = "ranger", branchIndex = 1, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 6, power = 1.40f, breakMultiplier = 1.80f, maxRank = 3, critChanceBonus = 0.05f, cooldown = 1, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.40f) }, prerequisites = new[] { "trap_setup" }, tags = new[] { "Ranged" }, isRanged = true, description = "Pin target to the ground with overwhelming force." },

            new SkillDef { id = "rain_of_arrows", name = "Rain of Arrows", classId = "ranger", branchIndex = 2, tier = 4, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.DEX, mpCost = 9, power = 1.60f, breakMultiplier = 1.60f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, prerequisites = new[] { "explosive_arrow" }, tags = new[] { "Ranged" }, isRanged = true, description = "Rain down a dense storm of arrows striking every enemy repeatedly." },

            new SkillDef { id = "deadeye", name = "Deadeye", classId = "ranger", branchIndex = 0, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 11, power = 2.75f, breakMultiplier = 2.25f, maxRank = 3, critChanceBonus = 0.25f, cooldown = 2, prerequisites = new[] { "shadow_shot" }, tags = new[] { "Ranged" }, isRanged = true, description = "Flawless ranged shot striking the absolute critical center." },

            new SkillDef { id = "perfect_hunt", name = "Perfect Hunt", classId = "ranger", branchIndex = 1, tier = 5, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.DEX, mpCost = 8, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.DefenseDown, 3, 0.60f) }, prerequisites = new[] { "pinning_shot" }, tags = new[] { "Ranged", "HuntMark" }, isRanged = true, description = "The ultimate mark, exposing all flaws and sealing quarry's fate." },

            new SkillDef { id = "natures_wrath_shot", name = "Nature's Wrath Shot", classId = "ranger", branchIndex = 2, tier = 5, kind = SkillKind.Physical, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.DEX, mpCost = 11, power = 1.85f, breakMultiplier = 2.00f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 3, prerequisites = new[] { "rain_of_arrows" }, tags = new[] { "Ranged" }, isRanged = true, description = "Channel the wrath of the wild into an arrow storm that decimates all enemies." },



            // ---- Cleric (cleric) Complete 15-Skill Tree ----

            new SkillDef { id = "heal", name = "Divine Heal", classId = "cleric", branchIndex = 0, tier = 1, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 5, power = 1.50f, breakMultiplier = 0.00f, maxRank = 3, word = "SANA", difficulty = SkillDifficulty.Easy, tags = new[] { "Cleric" }, description = "Mend an ally's wounds with sacred radiance." },

            new SkillDef { id = "blessing", name = "Blessing", classId = "cleric", branchIndex = 1, tier = 1, kind = SkillKind.Support, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.CHA, mpCost = 7, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseUp, 2, 0.30f) }, tags = new[] { "Cleric" }, description = "Ward the entire party, increasing Defense for 2 turns." },

            new SkillDef { id = "holy_light", name = "Holy Light", classId = "cleric", branchIndex = 2, tier = 1, kind = SkillKind.Magic, element = Element.Holy, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 4, power = 1.25f, breakMultiplier = 1.20f, maxRank = 3, critChanceBonus = 0.05f, tags = new[] { "Cleric" }, description = "Cast a beam of searing Holy light upon a foe." },

            new SkillDef { id = "mend", name = "Prayer of Healing", classId = "cleric", branchIndex = 0, tier = 2, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 8, power = 2.20f, breakMultiplier = 0.00f, maxRank = 3, word = "VITALIS", prerequisites = new[] { "heal" }, tags = new[] { "Cleric" }, description = "Channel deep restorative faith to heavily heal one ally." },

            new SkillDef { id = "divine_shield", name = "Divine Shield", classId = "cleric", branchIndex = 1, tier = 2, kind = SkillKind.Support, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 6, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseUp, 2, 0.40f) }, prerequisites = new[] { "blessing" }, tags = new[] { "Cleric" }, description = "Enfold an ally in a divine barrier reducing incoming damage." },

            new SkillDef { id = "purify", name = "Purify", classId = "cleric", branchIndex = 0, tier = 2, kind = SkillKind.Support, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 4, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 1, prerequisites = new[] { "heal" }, tags = new[] { "Cleric" }, description = "Cleanse all negative status ailments from an ally." },

            new SkillDef { id = "prayer_of_healing_all", name = "Sanctified Circle", classId = "cleric", branchIndex = 0, tier = 3, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 11, power = 1.45f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 1, prerequisites = new[] { "mend" }, tags = new[] { "Cleric" }, description = "Bathe the entire party in restorative light." },

            new SkillDef { id = "sanctuary", name = "Sanctuary", classId = "cleric", branchIndex = 1, tier = 3, kind = SkillKind.Support, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 9, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.MagicDefenseUp, 2, 0.35f) }, prerequisites = new[] { "divine_shield" }, tags = new[] { "Cleric" }, description = "Create sacred ground boosting party Magic Defense for 2 turns." },

            new SkillDef { id = "holy_smite", name = "Holy Smite", classId = "cleric", branchIndex = 2, tier = 3, kind = SkillKind.Magic, element = Element.Holy, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 7, power = 1.85f, breakMultiplier = 1.60f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, prerequisites = new[] { "holy_light" }, tags = new[] { "Cleric" }, description = "Strike an enemy with righteous judgment dealing heavy Holy damage." },

            new SkillDef { id = "resurrection", name = "Resurrection", classId = "cleric", branchIndex = 0, tier = 4, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 14, power = 2.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 4, prerequisites = new[] { "prayer_of_healing_all" }, tags = new[] { "Cleric" }, description = "Call upon divine mercy to revive a fallen companion." },

            new SkillDef { id = "guardian_angel", name = "Guardian Angel", classId = "cleric", branchIndex = 1, tier = 4, kind = SkillKind.Support, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 10, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, apply = new[] { Status(StatusId.DefenseUp, 2, 0.60f) }, prerequisites = new[] { "sanctuary" }, tags = new[] { "Cleric" }, description = "Grant an ally supreme angelic protection (+60% DEF for 2 turns)." },

            new SkillDef { id = "judgment", name = "Judgment", classId = "cleric", branchIndex = 2, tier = 4, kind = SkillKind.Magic, element = Element.Holy, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 10, power = 2.25f, breakMultiplier = 2.00f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, apply = new[] { Status(StatusId.DefenseDown, 2, 0.35f) }, prerequisites = new[] { "holy_smite" }, tags = new[] { "Cleric" }, description = "Divine judgment that sunders darkness and weakens enemy defense." },

            new SkillDef { id = "miracle", name = "Miracle", classId = "cleric", branchIndex = 0, tier = 5, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 16, power = 2.80f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, prerequisites = new[] { "resurrection" }, tags = new[] { "Cleric" }, description = "Supreme miracle restoring immense HP to all companions." },

            new SkillDef { id = "divine_intervention", name = "Divine Intervention", classId = "cleric", branchIndex = 1, tier = 5, kind = SkillKind.Support, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 14, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 4, apply = new[] { Status(StatusId.DefenseUp, 3, 0.50f) }, prerequisites = new[] { "guardian_angel" }, tags = new[] { "Cleric" }, description = "Call down direct intervention protecting all allies for 3 turns." },

            new SkillDef { id = "heavens_wrath", name = "Heaven's Wrath", classId = "cleric", branchIndex = 2, tier = 5, kind = SkillKind.Magic, element = Element.Holy, target = TargetType.AllEnemies, scalingAbility = Ability.WIS, mpCost = 15, power = 1.85f, breakMultiplier = 2.00f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "judgment" }, tags = new[] { "Cleric" }, description = "Smiting pillars of radiant light burn away all enemies." },



            // ---- Wizard (wizard) Complete 15-Skill Tree ----

            new SkillDef { id = "firebolt", name = "Firebolt", classId = "wizard", branchIndex = 0, tier = 1, kind = SkillKind.Magic, element = Element.Fire, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 5, power = 1.45f, breakMultiplier = 1.15f, maxRank = 3, critChanceBonus = 0.05f, word = "IGNIS", tags = new[] { "Arcane" }, description = "A searing bolt of fire striking a single enemy." },

            new SkillDef { id = "frost_nova", name = "Frost Nova", classId = "wizard", branchIndex = 1, tier = 1, kind = SkillKind.Magic, element = Element.Ice, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 7, power = 0.90f, breakMultiplier = 1.25f, maxRank = 3, cooldown = 1, apply = new[] { Status(StatusId.Slow, 2, 0.35f) }, tags = new[] { "Arcane" }, description = "Freezing wave chilling all enemies and inflicting Slow." },

            new SkillDef { id = "arcane_bolt", name = "Arcane Bolt", classId = "wizard", branchIndex = 2, tier = 1, kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 4, power = 1.25f, breakMultiplier = 1.20f, maxRank = 3, critChanceBonus = 0.05f, tags = new[] { "Arcane" }, description = "Unerring bolt of raw mystic energy." },

            new SkillDef { id = "lightning_strike", name = "Lightning Strike", classId = "wizard", branchIndex = 0, tier = 2, kind = SkillKind.Magic, element = Element.Lightning, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 6, power = 1.65f, breakMultiplier = 1.35f, maxRank = 3, critChanceBonus = 0.10f, prerequisites = new[] { "firebolt" }, tags = new[] { "Arcane" }, description = "Call down a sudden lightning bolt with high shock damage." },

            new SkillDef { id = "frost_shard", name = "Frost Shard", classId = "wizard", branchIndex = 1, tier = 2, kind = SkillKind.Magic, element = Element.Ice, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 6, power = 1.35f, breakMultiplier = 1.30f, maxRank = 3, critChanceBonus = 0.05f, word = "FROST", apply = new[] { Status(StatusId.Slow, 2, 0.40f, chance: 0.60f) }, prerequisites = new[] { "frost_nova" }, tags = new[] { "Arcane" }, description = "Sharp piercing ice shard with guaranteed Slow on perfect timing." },

            new SkillDef { id = "arcane_burst", name = "Arcane Burst", classId = "wizard", branchIndex = 2, tier = 2, kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 14, power = 1.30f, breakMultiplier = 1.40f, bonusVsBroken = 0.25f, maxRank = 3, cooldown = 2, word = "AETHERIS", difficulty = SkillDifficulty.Hard, prerequisites = new[] { "arcane_bolt" }, tags = new[] { "Arcane" }, description = "Detonate raw arcane energy across all foes." },

            new SkillDef { id = "meteor", name = "Meteor", classId = "wizard", branchIndex = 0, tier = 3, kind = SkillKind.Magic, element = Element.Fire, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 11, power = 1.55f, breakMultiplier = 1.60f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, prerequisites = new[] { "lightning_strike" }, tags = new[] { "Arcane" }, description = "Summon a blazing meteor crashing into enemy ranks." },

            new SkillDef { id = "blizzard", name = "Blizzard", classId = "wizard", branchIndex = 1, tier = 3, kind = SkillKind.Magic, element = Element.Ice, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 10, power = 1.20f, breakMultiplier = 1.50f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.Slow, 2, 0.45f) }, prerequisites = new[] { "frost_shard" }, tags = new[] { "Arcane" }, description = "Unleash a howling blizzard numbing all enemies with cold." },

            new SkillDef { id = "chain_lightning", name = "Chain Lightning", classId = "wizard", branchIndex = 0, tier = 3, kind = SkillKind.Magic, element = Element.Lightning, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 9, power = 1.30f, breakMultiplier = 1.40f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, prerequisites = new[] { "lightning_strike" }, tags = new[] { "Arcane" }, description = "Discharge lightning that leaps between all enemy combatants." },

            new SkillDef { id = "inferno", name = "Inferno", classId = "wizard", branchIndex = 0, tier = 4, kind = SkillKind.Magic, element = Element.Fire, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 12, power = 1.75f, breakMultiplier = 1.80f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.DefenseDown, 2, 0.30f) }, prerequisites = new[] { "meteor" }, tags = new[] { "Arcane" }, description = "Engulf the battlefield in hellfire melting armor and posture." },

            new SkillDef { id = "absolute_zero", name = "Absolute Zero", classId = "wizard", branchIndex = 1, tier = 4, kind = SkillKind.Magic, element = Element.Ice, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 10, power = 2.15f, breakMultiplier = 2.20f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.50f) }, prerequisites = new[] { "blizzard" }, tags = new[] { "Arcane" }, description = "Flash-freeze a foe to absolute zero, inflicting Stun." },

            new SkillDef { id = "arcane_overload", name = "Arcane Overload", classId = "wizard", branchIndex = 2, tier = 4, kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 11, power = 2.50f, breakMultiplier = 2.05f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "arcane_burst" }, tags = new[] { "Arcane" }, description = "Supercharge arcane power to obliterate a single target." },

            new SkillDef { id = "cataclysm_spell", name = "Elemental Cataclysm", classId = "wizard", branchIndex = 0, tier = 5, kind = SkillKind.Magic, element = Element.Fire, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 15, power = 2.25f, breakMultiplier = 2.20f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 3, prerequisites = new[] { "inferno" }, tags = new[] { "Arcane" }, description = "Apocalyptic elemental storm ravaging all enemies with cataclysmic heat." },

            new SkillDef { id = "apocalypse_spell", name = "Glacial Apocalypse", classId = "wizard", branchIndex = 1, tier = 5, kind = SkillKind.Magic, element = Element.Ice, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 15, power = 2.05f, breakMultiplier = 2.40f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 3, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.40f) }, prerequisites = new[] { "absolute_zero" }, tags = new[] { "Arcane" }, description = "Entomb the battlefield in eternal permafrost." },

            new SkillDef { id = "grand_arcanum", name = "Grand Arcanum", classId = "wizard", branchIndex = 2, tier = 5, kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 16, power = 2.35f, breakMultiplier = 2.50f, maxRank = 3, critChanceBonus = 0.20f, cooldown = 3, prerequisites = new[] { "arcane_overload" }, tags = new[] { "Arcane" }, description = "The zenith of arcane spellcraft tearing through the fabric of reality." },



            // ---- Warlock (warlock) Complete 15-Skill Tree ----

            new SkillDef { id = "shadow_bolt", name = "Shadow Bolt", classId = "warlock", branchIndex = 2, tier = 1, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 4, power = 1.35f, breakMultiplier = 1.25f, maxRank = 3, critChanceBonus = 0.05f, tags = new[] { "Pact" }, description = "Hurl a missile of abyssal darkness at a single foe." },

            new SkillDef { id = "curse_of_weakness", name = "Curse of Weakness", classId = "warlock", branchIndex = 0, tier = 1, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 4, power = 0.85f, breakMultiplier = 1.00f, maxRank = 3, cooldown = 1, apply = new[] { Status(StatusId.AttackDown, 2, 0.35f) }, tags = new[] { "Pact" }, description = "Afflict target with weakness, sapping its attack power." },

            new SkillDef { id = "corruption", name = "Corruption", classId = "warlock", branchIndex = 0, tier = 1, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 4, power = 0.75f, breakMultiplier = 1.00f, maxRank = 3, apply = new[] { Status(StatusId.Poison, 3, 0.30f) }, tags = new[] { "Pact" }, description = "Corrupt target's lifeblood with dark damage over time." },

            new SkillDef { id = "curse_of_vulnerability", name = "Curse of Vulnerability", classId = "warlock", branchIndex = 0, tier = 2, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 5, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.MagicDefenseDown, 2, 0.35f) }, prerequisites = new[] { "curse_of_weakness" }, tags = new[] { "Pact" }, description = "Weaken target's warding, reducing its Magic Defense." },

            new SkillDef { id = "soul_drain", name = "Soul Drain", classId = "warlock", branchIndex = 1, tier = 2, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 5, power = 1.25f, breakMultiplier = 1.10f, maxRank = 3, critChanceBonus = 0.05f, cooldown = 1, prerequisites = new[] { "corruption" }, tags = new[] { "Pact" }, description = "Drain life force from an enemy to mend own wounds." },

            new SkillDef { id = "shadow_burst", name = "Shadow Burst", classId = "warlock", branchIndex = 2, tier = 2, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 7, power = 0.95f, breakMultiplier = 1.30f, maxRank = 3, cooldown = 1, prerequisites = new[] { "shadow_bolt" }, tags = new[] { "Pact" }, description = "Detonate dark energy harming all enemy combatants." },

            new SkillDef { id = "curse_of_agony", name = "Curse of Agony", classId = "warlock", branchIndex = 0, tier = 3, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 6, power = 1.10f, breakMultiplier = 1.20f, maxRank = 3, critChanceBonus = 0.05f, cooldown = 1, apply = new[] { Status(StatusId.Poison, 3, 0.45f) }, prerequisites = new[] { "curse_of_vulnerability" }, tags = new[] { "Pact" }, description = "Inflict escalating agony causing heavy damage over 3 turns." },

            new SkillDef { id = "soul_harvest", name = "Soul Harvest", classId = "warlock", branchIndex = 1, tier = 3, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 6, power = 1.75f, breakMultiplier = 1.45f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 1, prerequisites = new[] { "soul_drain" }, tags = new[] { "Pact" }, description = "Reap the soul of a cursed foe dealing bonus damage." },

            new SkillDef { id = "hex", name = "Hex", classId = "warlock", branchIndex = 2, tier = 3, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 5, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseDown, 2, 0.40f) }, prerequisites = new[] { "shadow_burst" }, tags = new[] { "Pact" }, description = "Afflict a foe with a debilitating hex reducing Physical Defense." },

            new SkillDef { id = "curse_of_doom", name = "Curse of Doom", classId = "warlock", branchIndex = 0, tier = 4, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 8, power = 2.45f, breakMultiplier = 2.00f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "curse_of_agony" }, tags = new[] { "Pact" }, description = "Inscribe imminent doom that unleashes catastrophic damage." },

            new SkillDef { id = "soul_collapse", name = "Soul Collapse", classId = "warlock", branchIndex = 1, tier = 4, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 9, power = 2.15f, breakMultiplier = 1.85f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "soul_harvest" }, tags = new[] { "Pact" }, description = "Implode target's soul, dealing immense damage with bonus vs Broken." },

            new SkillDef { id = "nightfall", name = "Nightfall", classId = "warlock", branchIndex = 2, tier = 4, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 10, power = 1.40f, breakMultiplier = 1.55f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.AttackDown, 2, 0.35f) }, prerequisites = new[] { "hex" }, tags = new[] { "Pact" }, description = "Blanket the battlefield in abyssal twilight weakening all enemies." },

            new SkillDef { id = "absolute_damnation", name = "Absolute Damnation", classId = "warlock", branchIndex = 0, tier = 5, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 13, power = 2.85f, breakMultiplier = 2.45f, maxRank = 3, critChanceBonus = 0.20f, cooldown = 3, apply = new[] { Status(StatusId.DefenseDown, 3, 0.45f) }, prerequisites = new[] { "curse_of_doom" }, tags = new[] { "Pact" }, description = "Condemn the target to eternal damnation with supreme curse power." },

            new SkillDef { id = "soul_annihilation", name = "Soul Annihilation", classId = "warlock", branchIndex = 1, tier = 5, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.Enemy, scalingAbility = Ability.INT, mpCost = 12, power = 2.75f, breakMultiplier = 2.25f, maxRank = 3, critChanceBonus = 0.20f, cooldown = 2, prerequisites = new[] { "soul_collapse" }, tags = new[] { "Pact" }, description = "Obliterate target's soul essence beyond mortal recovery." },

            new SkillDef { id = "apocalypse_curse", name = "Dark Apocalypse", classId = "warlock", branchIndex = 2, tier = 5, kind = SkillKind.Magic, element = Element.Dark, target = TargetType.AllEnemies, scalingAbility = Ability.INT, mpCost = 14, power = 1.95f, breakMultiplier = 2.25f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 3, prerequisites = new[] { "nightfall" }, tags = new[] { "Pact" }, description = "Open an abyssal rift devastating the entire enemy encounter." },



            // ---- Druid (druid) Complete 15-Skill Tree ----

            new SkillDef { id = "natures_embrace", name = "Nature's Embrace", classId = "druid", branchIndex = 0, tier = 1, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 4, power = 1.40f, breakMultiplier = 0.00f, maxRank = 3, tags = new[] { "Nature" }, description = "Channel restorative nature energies to heal an ally." },

            new SkillDef { id = "thorn_shot", name = "Thorn Shot", classId = "druid", branchIndex = 1, tier = 1, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 3, power = 1.20f, breakMultiplier = 1.30f, maxRank = 3, critChanceBonus = 0.05f, tags = new[] { "Nature" }, description = "Fire hardened briar thorns dealing physical nature damage." },

            new SkillDef { id = "entangling_roots", name = "Entangling Roots", classId = "druid", branchIndex = 2, tier = 1, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 4, power = 0.70f, breakMultiplier = 1.10f, maxRank = 3, cooldown = 1, apply = new[] { Status(StatusId.Slow, 2, 0.40f) }, tags = new[] { "Nature" }, description = "Grasp enemy feet in grasping roots, inflicting Slow." },

            new SkillDef { id = "healing_bloom", name = "Healing Bloom", classId = "druid", branchIndex = 0, tier = 2, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 7, power = 1.15f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 1, prerequisites = new[] { "natures_embrace" }, tags = new[] { "Nature" }, description = "Cause soothing blossoms to bloom across the entire party." },

            new SkillDef { id = "vine_lash", name = "Vine Lash", classId = "druid", branchIndex = 1, tier = 2, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 5, power = 1.45f, breakMultiplier = 1.55f, maxRank = 3, critChanceBonus = 0.05f, apply = new[] { Status(StatusId.Slow, 1, 0.30f) }, prerequisites = new[] { "thorn_shot" }, tags = new[] { "Nature" }, description = "Lash with thorny vines dealing Break damage and tripping foe." },

            new SkillDef { id = "barkskin", name = "Barkskin", classId = "druid", branchIndex = 0, tier = 2, kind = SkillKind.Support, element = Element.Physical, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 4, power = 0.00f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseUp, 2, 0.35f) }, prerequisites = new[] { "natures_embrace" }, tags = new[] { "Nature" }, description = "Harden ally's skin into tough ancient bark (DEF+ 2 turns)." },

            new SkillDef { id = "regrowth", name = "Regrowth", classId = "druid", branchIndex = 0, tier = 3, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, scalingAbility = Ability.WIS, mpCost = 6, power = 1.65f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 1, word = "FLORA", difficulty = SkillDifficulty.Easy, prerequisites = new[] { "healing_bloom" }, tags = new[] { "Nature" }, description = "Restorative nature magic that deeply knits wounds." },

            new SkillDef { id = "natures_wrath", name = "Nature's Wrath", classId = "druid", branchIndex = 1, tier = 3, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.WIS, mpCost = 8, power = 1.25f, breakMultiplier = 1.45f, maxRank = 3, critChanceBonus = 0.05f, cooldown = 1, prerequisites = new[] { "vine_lash" }, tags = new[] { "Nature" }, description = "Unleash swirling briars and gale force upon all enemies." },

            new SkillDef { id = "overgrowth", name = "Overgrowth", classId = "druid", branchIndex = 2, tier = 3, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.WIS, mpCost = 7, power = 0.85f, breakMultiplier = 1.35f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.Slow, 2, 0.45f) }, prerequisites = new[] { "entangling_roots" }, tags = new[] { "Nature" }, description = "Summon dense underbrush that slows and hampers all enemies." },

            new SkillDef { id = "ancient_sanctuary", name = "Ancient Sanctuary", classId = "druid", branchIndex = 0, tier = 4, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 11, power = 1.85f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 2, apply = new[] { Status(StatusId.DefenseUp, 2, 0.25f) }, prerequisites = new[] { "regrowth" }, tags = new[] { "Nature" }, description = "Create a sacred glade that heals and wards the party." },

            new SkillDef { id = "thorn_prison", name = "Thorn Prison", classId = "druid", branchIndex = 1, tier = 4, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.Enemy, scalingAbility = Ability.WIS, mpCost = 8, power = 1.95f, breakMultiplier = 1.95f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.40f) }, prerequisites = new[] { "natures_wrath" }, tags = new[] { "Nature" }, description = "Encase foe in a crushing cage of ironwood thorns." },

            new SkillDef { id = "wrath_of_the_forest", name = "Wrath of the Forest", classId = "druid", branchIndex = 2, tier = 4, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.WIS, mpCost = 10, power = 1.55f, breakMultiplier = 1.75f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 2, apply = new[] { Status(StatusId.Slow, 2, 0.40f) }, prerequisites = new[] { "overgrowth" }, tags = new[] { "Nature" }, description = "Command the ancient woods to rise up and batter all foes." },

            new SkillDef { id = "world_tree_blessing", name = "World Tree's Blessing", classId = "druid", branchIndex = 0, tier = 5, kind = SkillKind.Heal, element = Element.Holy, target = TargetType.AllAllies, scalingAbility = Ability.WIS, mpCost = 15, power = 2.55f, breakMultiplier = 0.00f, maxRank = 3, cooldown = 3, prerequisites = new[] { "ancient_sanctuary" }, tags = new[] { "Nature" }, description = "Channel the life-giving vitality of Yggdrasil to fully restore party." },

            new SkillDef { id = "primal_cataclysm", name = "Primal Cataclysm", classId = "druid", branchIndex = 1, tier = 5, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.WIS, mpCost = 14, power = 2.05f, breakMultiplier = 2.45f, maxRank = 3, critChanceBonus = 0.15f, cooldown = 2, prerequisites = new[] { "thorn_prison" }, tags = new[] { "Nature" }, description = "Release the untamed fury of the primal earth across all foes." },

            new SkillDef { id = "natures_dominion", name = "Nature's Dominion", classId = "druid", branchIndex = 2, tier = 5, kind = SkillKind.Magic, element = Element.Physical, target = TargetType.AllEnemies, scalingAbility = Ability.WIS, mpCost = 13, power = 1.75f, breakMultiplier = 2.25f, maxRank = 3, critChanceBonus = 0.10f, cooldown = 3, apply = new[] { Status(StatusId.Stun, 1, 1f, chance: 0.35f) }, prerequisites = new[] { "wrath_of_the_forest" }, tags = new[] { "Nature" }, description = "Establish absolute dominion over the battlefield with roots and thorns." },

}, s => s.id);



        public static readonly Dictionary<string, ItemDef> Items = Index(new[]

        {

            new ItemDef { id = "draught", name = "Draught", heal = 40, description = "Restores 40 HP." },

            new ItemDef { id = "ether", name = "Ether", mana = 20, description = "Restores 20 MP." },

            new ItemDef { id = "elixir", name = "Grave Elixir", heal = 80, mana = 30, description = "Restores 80 HP and 30 MP." },

            new ItemDef { id = "phoenix_down", name = "Phoenix Plume", heal = 0, mana = 0, description = "A rare feather that revives a fallen ally with 30% HP. Usable in or out of combat.", isRevive = true, reviveHpPercent = 0.30f },

        }, i => i.id);



        // Each preset character uses a valid 27-point Point Buy allocation + Race + Class + Background.

        public static readonly Dictionary<string, CharacterDef> Characters = Index(new[]

        {

            new CharacterDef

            {

                id = "knight", name = "Knight", role = "Defender",

                raceId = "human", classId = "knight", backgroundId = "oathsworn",

                baseScores = new AbilityScores(15, 10, 15, 8, 12, 11), // 9 + 2 + 9 + 0 + 4 + 3 = 27

                stats = new Stats(125, 20, 14, 13, 6, 4),

                skills = new[] { "shield_bash", "fortify", "interpose" }, sprite = Art.Knight,

                affinities = new Affinities { resist = new[] { Element.Dark } },

                description = "Oathbound shield. Outlasts anything and stands between the dark and the weak."

            },

            new CharacterDef

            {

                id = "rogue", name = "Rogue", role = "Striker",

                raceId = "elf", classId = "rogue", backgroundId = "outcast",

                baseScores = new AbilityScores(14, 15, 12, 9, 12, 10), // 7 + 9 + 4 + 1 + 4 + 2 = 27

                stats = new Stats(85, 25, 17, 7, 14, 5),

                skills = new[] { "quick_strike", "expose", "evasion" }, sprite = Art.Rogue,

                description = "Acts first and often. Precise timing turns strikes into criticals."

            },

            new CharacterDef

            {

                id = "mage", name = "Mage", role = "Caster",

                raceId = "highborn", classId = "mage", backgroundId = "acolyte",

                baseScores = new AbilityScores(8, 10, 10, 15, 15, 13), // 0 + 2 + 2 + 9 + 9 + 5 = 27

                stats = new Stats(60, 60, 7, 4, 9, 20),

                skills = new[] { "firebolt", "frost_shard", "arcane_burst" }, sprite = Art.Mage,

                affinities = new Affinities { weak = new[] { Element.Dark }, resist = new[] { Element.Arcane } },

                description = "Fragile, but every foe has an element it fears."

            },

            new CharacterDef

            {

                id = "healer", name = "Healer", role = "Support",

                raceId = "sanctified", classId = "healer", backgroundId = "pilgrim",

                baseScores = new AbilityScores(8, 10, 12, 13, 15, 14), // 0 + 2 + 4 + 5 + 9 + 7 = 27

                stats = new Stats(80, 55, 7, 6, 10, 14),

                skills = new[] { "heal", "mend", "blessing" }, sprite = Art.Healer,

                affinities = new Affinities { resist = new[] { Element.Dark } },

                description = "Keeps the party standing through long fights."

            },

        }, c => c.id);



        public static readonly string[] Party = { "knight", "rogue", "mage", "healer" };

        public static readonly string[] StartingItems = { "draught", "draught", "ether" };



        // ---- 10b. Data-Driven Character Creation Templates (Section 4.2) ----

        public static readonly string[] TemplateIds =

        {

            "frontline_fighter", "berserker_barbarian", "monk_martial_artist",

            "agile_rogue", "sharpshooter_ranger", "support_cleric",

            "elemental_wizard", "pact_warlock", "nature_druid"

        };



        public static readonly Dictionary<string, CharacterTemplateDef> Templates = Index(new[]

        {

            new CharacterTemplateDef

            {

                id = "frontline_fighter", name = "Frontline Fighter",

                description = "Durable sword-and-shield vanguard built for frontline defense and posture breaking.",

                raceId = "human", classId = "fighter", backgroundId = "oathsworn",

                baseScores = new AbilityScores(15, 12, 15, 8, 10, 11), // 9+4+9+0+2+3 = 27

                starterPassiveId = "weapon_proficiency",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "iron_broadsword"),

                    Eq(EquipmentSlot.SecondHand, "iron_shield"),

                    Eq(EquipmentSlot.HeadArmor, "iron_helm"),

                    Eq(EquipmentSlot.BodyArmor, "chain_hauberk"),

                    Eq(EquipmentSlot.LegArmor, "chain_greaves"),

                    Eq(EquipmentSlot.FootArmor, "iron_sabatons"),

                    Eq(EquipmentSlot.Accessory1, "vanguard_ring"),

                    Eq(EquipmentSlot.Accessory2, "soldier_medal")

                }

            },

            new CharacterTemplateDef

            {

                id = "berserker_barbarian", name = "Frenzied Berserker",

                description = "High-strength Dragonborn bruiser thriving in low-HP berserk exchanges.",

                raceId = "dragonborn", classId = "barbarian", backgroundId = "outcast",

                baseScores = new AbilityScores(15, 12, 15, 8, 10, 11), // 9+4+9+0+2+3 = 27

                starterPassiveId = "savage_instinct",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "battle_greataxe"),

                    Eq(EquipmentSlot.SecondHand, "iron_buckler"),

                    Eq(EquipmentSlot.HeadArmor, "horned_helm"),

                    Eq(EquipmentSlot.BodyArmor, "hide_harness"),

                    Eq(EquipmentSlot.LegArmor, "fur_leggings"),

                    Eq(EquipmentSlot.FootArmor, "strider_boots"),

                    Eq(EquipmentSlot.Accessory1, "fang_talisman"),

                    Eq(EquipmentSlot.Accessory2, "vanguard_ring")

                }

            },

            new CharacterTemplateDef

            {

                id = "monk_martial_artist", name = "Zen Striker",

                description = "Fast unarmed striker focused on Combo Flow and shattering enemy Break Gauges.",

                raceId = "liichtian", classId = "martial_artist", backgroundId = "pilgrim",

                baseScores = new AbilityScores(14, 15, 13, 8, 13, 9), // 7+9+5+0+5+1 = 27

                starterPassiveId = "martial_discipline",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "iron_gauntlets"),

                    Eq(EquipmentSlot.SecondHand, "iron_knuckle"),

                    Eq(EquipmentSlot.HeadArmor, "monk_headband"),

                    Eq(EquipmentSlot.BodyArmor, "cloth_tunic"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "soft_boots"),

                    Eq(EquipmentSlot.Accessory1, "focus_band"),

                    Eq(EquipmentSlot.Accessory2, "swift_anklet")

                }

            },

            new CharacterTemplateDef

            {

                id = "agile_rogue", name = "Agile Rogue",

                description = "High-initiative Halfling opportunist exploiting debuffed targets for critical hits.",

                raceId = "halfling", classId = "rogue", backgroundId = "outcast",

                baseScores = new AbilityScores(14, 15, 12, 9, 12, 10), // 7+9+4+1+4+2 = 27

                starterPassiveId = "opportunist",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "duelist_rapier"),

                    Eq(EquipmentSlot.SecondHand, "iron_dagger"),

                    Eq(EquipmentSlot.HeadArmor, "shadow_hood"),

                    Eq(EquipmentSlot.BodyArmor, "leather_jerkin"),

                    Eq(EquipmentSlot.LegArmor, "leather_greaves"),

                    Eq(EquipmentSlot.FootArmor, "soft_boots"),

                    Eq(EquipmentSlot.Accessory1, "thief_ring"),

                    Eq(EquipmentSlot.Accessory2, "swift_anklet")

                }

            },

            new CharacterTemplateDef

            {

                id = "sharpshooter_ranger", name = "Sharpshooter Ranger",

                description = "Precision Elven archer leveraging Hunter's Instinct for accurate ranged volleys.",

                raceId = "elf", classId = "ranger", backgroundId = "outcast",

                baseScores = new AbilityScores(12, 15, 13, 10, 13, 10), // 4+9+5+2+5+2 = 27

                starterPassiveId = "hunters_instinct",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "hunting_bow"),

                    Eq(EquipmentSlot.SecondHand, "fletched_quiver"),

                    Eq(EquipmentSlot.HeadArmor, "scout_cap"),

                    Eq(EquipmentSlot.BodyArmor, "leather_jerkin"),

                    Eq(EquipmentSlot.LegArmor, "leather_greaves"),

                    Eq(EquipmentSlot.FootArmor, "strider_boots"),

                    Eq(EquipmentSlot.Accessory1, "hawk_feather"),

                    Eq(EquipmentSlot.Accessory2, "swift_anklet")

                }

            },

            new CharacterTemplateDef

            {

                id = "support_cleric", name = "Support Cleric",

                description = "Resilient Dwarven healer and protector amplified by Divine Grace.",

                raceId = "dwarf", classId = "cleric", backgroundId = "pilgrim",

                baseScores = new AbilityScores(10, 10, 14, 10, 15, 13), // 2+2+7+2+9+5 = 27

                starterPassiveId = "divine_grace",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "sanctified_mace"),

                    Eq(EquipmentSlot.SecondHand, "silver_holy_symbol"),

                    Eq(EquipmentSlot.HeadArmor, "acolyte_circlet"),

                    Eq(EquipmentSlot.BodyArmor, "linen_vestments"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "pilgrim_sandals"),

                    Eq(EquipmentSlot.Accessory1, "prayer_rosary"),

                    Eq(EquipmentSlot.Accessory2, "soldier_medal")

                }

            },

            new CharacterTemplateDef

            {

                id = "elemental_wizard", name = "Elemental Wizard",

                description = "Arcane spellcaster with reduced spell MP costs and high elemental Break damage.",

                raceId = "elf", classId = "wizard", backgroundId = "acolyte",

                baseScores = new AbilityScores(8, 12, 13, 15, 13, 12), // 0+4+5+9+5+4 = 27

                starterPassiveId = "arcane_affinity",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "apprentice_wand"),

                    Eq(EquipmentSlot.SecondHand, "arcane_orb"),

                    Eq(EquipmentSlot.HeadArmor, "scholar_hood"),

                    Eq(EquipmentSlot.BodyArmor, "cloth_tunic"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "pilgrim_sandals"),

                    Eq(EquipmentSlot.Accessory1, "mana_crystal"),

                    Eq(EquipmentSlot.Accessory2, "focus_band")

                }

            },

            new CharacterTemplateDef

            {

                id = "pact_warlock", name = "Pact Warlock",

                description = "Occult caster channeling Pact Affinity for amplified dark magic and hexes.",

                raceId = "dragonborn", classId = "warlock", backgroundId = "acolyte",

                baseScores = new AbilityScores(8, 10, 13, 15, 10, 15), // 0+2+5+9+2+9 = 27

                starterPassiveId = "pact_affinity",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "ritual_dagger"),

                    Eq(EquipmentSlot.SecondHand, "pact_grimoire"),

                    Eq(EquipmentSlot.HeadArmor, "cowl_of_whispers"),

                    Eq(EquipmentSlot.BodyArmor, "darkweave_robe"),

                    Eq(EquipmentSlot.LegArmor, "cloth_trousers"),

                    Eq(EquipmentSlot.FootArmor, "pilgrim_sandals"),

                    Eq(EquipmentSlot.Accessory1, "pact_seal"),

                    Eq(EquipmentSlot.Accessory2, "mana_crystal")

                }

            },

            new CharacterTemplateDef

            {

                id = "nature_druid", name = "Nature Druid",

                description = "Balanced Merfolk primal caster combining Nature's Attunement magic and restorative healing.",

                raceId = "merfolk", classId = "druid", backgroundId = "pilgrim",

                baseScores = new AbilityScores(10, 12, 14, 10, 15, 11), // 2+4+7+2+9+3 = 27

                starterPassiveId = "natures_attunement",

                startingEquipment = new[]

                {

                    Eq(EquipmentSlot.MainHand, "oak_staff"),

                    Eq(EquipmentSlot.SecondHand, "verdant_totem"),

                    Eq(EquipmentSlot.HeadArmor, "antler_crown"),

                    Eq(EquipmentSlot.BodyArmor, "barkskin_vest"),

                    Eq(EquipmentSlot.LegArmor, "fur_leggings"),

                    Eq(EquipmentSlot.FootArmor, "strider_boots"),

                    Eq(EquipmentSlot.Accessory1, "amber_seed"),

                    Eq(EquipmentSlot.Accessory2, "prayer_rosary")

                }

            },

        }, t => t.id);



        public static readonly Dictionary<string, EnemyDef> Enemies = Index(new[]

        {

            new EnemyDef

            {

                id = "skeleton", name = "Skeleton Warrior", shortName = "Skeleton", tag = "SK", tier = Tier.Normal, ai = AiKind.Basic,

                abilityScores = new AbilityScores(12, 10, 10, 6, 8, 6),

                stats = new Stats(40, 0, 9, 3, 8, 0), magicDef = 2,

                maxBreakGauge = 45, breakResistance = 0, breakDuration = 1, breakStunTurns = 1, breakDamageMultiplier = 1.5f,

                gold = 8, sprite = Art.Skeleton,

                affinities = new Affinities { weak = new[] { Element.Fire, Element.Holy }, resist = new[] { Element.Ice } },

                attacks = new[] { new EnemyAttack { name = "Rusty Slash" } },

                description = "Rattling bones bound to a rusted blade."

            },

            new EnemyDef

            {

                id = "cursed_knight", name = "Cursed Knight", shortName = "Cursed Knight", tag = "CK", tier = Tier.Elite, ai = AiKind.Tactical,

                abilityScores = new AbilityScores(16, 10, 14, 10, 12, 10),

                stats = new Stats(120, 0, 16, 9, 6, 6), magicDef = 6,

                maxBreakGauge = 80, breakResistance = 3, breakDuration = 1, breakStunTurns = 1, breakDamageMultiplier = 1.5f,

                gold = 30, sprite = Art.CursedKnight,

                affinities = new Affinities { weak = new[] { Element.Ice }, resist = new[] { Element.Physical, Element.Fire }, immune = new[] { Element.Dark } },

                attacks = new[]

                {

                    new EnemyAttack { name = "Heavy Cleave", power = 1.3f },

                    new EnemyAttack { name = "Twin Strike", hits = 2, power = 0.7f },

                    new EnemyAttack { name = "Delayed Thrust", power = 1.1f, delay = 0.6f },

                    new EnemyAttack { name = "Iron Vow", self = true, apply = new[] { Status(StatusId.DefenseUp, 2, 0.5f) } },

                    new EnemyAttack { name = "Dread Howl", power = 0.4f, element = Element.Dark, allTargets = true, apply = new[] { Status(StatusId.AttackDown, 2, 0.25f) } },

                },

                description = "A fallen oathkeeper. Braces when hurt and preys on the weakest."

            },

            new EnemyDef

            {

                id = "warden", name = "Abyssal Warden", shortName = "Warden", tag = "WDN", tier = Tier.Boss, ai = AiKind.Boss,

                abilityScores = new AbilityScores(18, 12, 16, 14, 14, 14),

                stats = new Stats(340, 0, 20, 10, 9, 12), magicDef = 8,

                maxBreakGauge = 140, breakResistance = 5, breakDuration = 1, breakStunTurns = 1, breakDamageMultiplier = 1.35f,

                bossBreakRecoverOnPhase2 = true,

                gold = 100, sprite = Art.Warden,

                affinities = new Affinities { weak = new[] { Element.Arcane }, resist = new[] { Element.Fire, Element.Ice }, immune = new[] { Element.Dark } },

                attacks = new[]

                {

                    new EnemyAttack { name = "Chain Lash", hits = 3, power = 0.5f, phase = 1 },

                    new EnemyAttack { name = "Crushing Grip", power = 1.5f, delay = 0.8f },

                    new EnemyAttack { name = "Abyssal Gaze", power = 0.7f, element = Element.Dark, allTargets = true, delay = 0.5f, phase = 2 },

                    new EnemyAttack { name = "Soul Rend", hits = 2, power = 0.8f, element = Element.Dark, phase = 2, apply = new[] { Status(StatusId.Poison, 3, 6f) } },

                },

                phase2At = 0.5f, phase2Text = "The Warden's eye burns red! (Phase 2)",

                phase2Buff = new StatusApply { id = StatusId.AttackUp, turns = 99, magnitude = 0.3f },

                description = "Jailer of the deep. Enrages at half health."

            },

        }, e => e.id);



        public static readonly Dictionary<string, EncounterDef> Encounters = Index(new[]

        {

            new EncounterDef { id = "skel2", tier = Tier.Normal, enemies = new[] { "skeleton", "skeleton" } },

            new EncounterDef { id = "skel3", tier = Tier.Normal, enemies = new[] { "skeleton", "skeleton", "skeleton" }, minFloor = 2 },

            new EncounterDef { id = "elite1", tier = Tier.Elite, enemies = new[] { "cursed_knight" }, music = MusicIds.Battle2 },

            new EncounterDef { id = "elite2", tier = Tier.Elite, enemies = new[] { "skeleton", "cursed_knight" }, minFloor = 4, music = MusicIds.Battle2 },

            new EncounterDef { id = "boss", tier = Tier.Boss, enemies = new[] { "warden" }, music = MusicIds.Battle3 },

        }, e => e.id);



        public static readonly Dictionary<string, RelicDef> Relics = Index(new[]

        {

            new RelicDef { id = "iron_locket", name = "Iron Locket", bonus = new Stats(12, 0, 0, 0, 0, 0), description = "+12 max HP to the party." },

            new RelicDef { id = "whetstone", name = "Whetstone", bonus = new Stats(0, 0, 2, 0, 0, 0), description = "+2 ATK to the party." },

            new RelicDef { id = "moon_sigil", name = "Moon Sigil", bonus = new Stats(0, 6, 0, 0, 0, 3), description = "+3 MAG and +6 max MP to the party." },

            new RelicDef { id = "warding_charm", name = "Warding Charm", bonus = new Stats(0, 0, 0, 2, 0, 0), description = "+2 DEF to the party." },

            new RelicDef { id = "ashen_boots", name = "Ashen Boots", bonus = new Stats(0, 0, 0, 0, 1, 0), description = "+1 SPD to the party." },

            new RelicDef { id = "ember_heart", name = "Ember Heart", victoryHeal = 0.08f, description = "Heal 8% max HP after each victory." },

            new RelicDef { id = "warden_eye", name = "Warden's Eye", bonus = new Stats(10, 5, 1, 1, 0, 1), description = "Trophy of the Abyssal Warden." },

        }, r => r.id);



        public const string BossRelic = "warden_eye";



        public static readonly Dictionary<string, UpgradeDef> Upgrades = Index(new[]

        {

            new UpgradeDef { character = "knight", name = "Bulwark Drill", bonus = new Stats(15, 0, 0, 2, 0, 0) },

            new UpgradeDef { character = "rogue", name = "Honed Blades", bonus = new Stats(0, 0, 3, 0, 1, 0) },

            new UpgradeDef { character = "mage", name = "Deeper Focus", bonus = new Stats(0, 10, 0, 0, 0, 3) },

            new UpgradeDef { character = "healer", name = "Litany of Light", bonus = new Stats(10, 8, 0, 0, 0, 2) },

        }, u => u.character);



        public static readonly Dictionary<string, EventDef> Events = Index(new[]

        {

            new EventDef

            {

                id = "shrine", title = "Whispering Shrine", text = "A cracked shrine hums. Candles relight as you approach.",

                choices = new[]

                {

                    new EventChoice { label = "Offer 30 gold", goldCost = 30, relic = true, outcome = "The coins vanish. Something is left in their place." },

                    new EventChoice { label = "Anoint weapons", battleBuff = new StatusApply { id = StatusId.AttackUp, turns = 3, magnitude = 0.3f }, outcome = "Your blades drink the candlelight." },

                    new EventChoice { label = "Pray", healPercent = 25, outcome = "Warmth settles over the party." },

                },

            },

            new EventDef

            {

                id = "merchant", title = "Hooded Peddler", text = "A figure with too many pockets offers a trade.",

                choices = new[]

                {

                    new EventChoice { label = "Buy a Draught (15g)", goldCost = 15, item = "draught", outcome = "\"Pleasure doing business.\"" },

                    new EventChoice { label = "Buy a relic (45g)", goldCost = 45, relic = true, outcome = "The peddler grins and vanishes." },

                    new EventChoice { label = "Leave", outcome = "You keep your coin and your distance." },

                },

            },

            new EventDef

            {

                id = "altar", title = "Drowned Altar", text = "Black water fills a stone basin. It whispers of power.",

                choices = new[]

                {

                    new EventChoice { label = "Drink", hurtPercent = 20, relic = true, outcome = "It burns going down. Something stays with you." },

                    new EventChoice { label = "Refuse", outcome = "You walk away. The whispers fade." },

                },

            },

            new EventDef

            {

                id = "fallen", title = "Fallen Adventurer", text = "A skeleton in rotted leathers still clutches its pack.",

                choices = new[]

                {

                    new EventChoice { label = "Take the coin purse", gold = 25, outcome = "25 gold. They won't need it." },

                    new EventChoice { label = "Take the vial", item = "ether", outcome = "An Ether, still sealed." },

                },

            },

        }, e => e.id);



        // ---- 8. Affix Pool Registry (GDD Section 6) ----

        public static readonly Dictionary<string, AffixDef> Affixes = Index(new[]

        {

            // Primary Stats

            new AffixDef

            {

                id = "affix_str", name = "Strength", category = AffixCategory.PrimaryStats,

                isAbility = true, ability = Ability.STR, baseMinValue = 1, baseMaxValue = 3,

                descriptionFormat = "+{0} STR"

            },

            new AffixDef

            {

                id = "affix_dex", name = "Dexterity", category = AffixCategory.PrimaryStats,

                isAbility = true, ability = Ability.DEX, baseMinValue = 1, baseMaxValue = 3,

                descriptionFormat = "+{0} DEX"

            },

            new AffixDef

            {

                id = "affix_vit", name = "Vitality", category = AffixCategory.PrimaryStats,

                isAbility = true, ability = Ability.VIT, baseMinValue = 1, baseMaxValue = 3,

                descriptionFormat = "+{0} VIT"

            },

            new AffixDef

            {

                id = "affix_int", name = "Intelligence", category = AffixCategory.PrimaryStats,

                isAbility = true, ability = Ability.INT, baseMinValue = 1, baseMaxValue = 3,

                descriptionFormat = "+{0} INT"

            },

            new AffixDef

            {

                id = "affix_wis", name = "Wisdom", category = AffixCategory.PrimaryStats,

                isAbility = true, ability = Ability.WIS, baseMinValue = 1, baseMaxValue = 3,

                descriptionFormat = "+{0} WIS"

            },

            new AffixDef

            {

                id = "affix_cha", name = "Charisma", category = AffixCategory.PrimaryStats,

                isAbility = true, ability = Ability.CHA, baseMinValue = 1, baseMaxValue = 3,

                descriptionFormat = "+{0} CHA"

            },



            // Secondary Stats

            new AffixDef

            {

                id = "affix_crit_chance", name = "Critical Chance", category = AffixCategory.SecondaryStats,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.CriticalChance,

                baseMinValue = 3, baseMaxValue = 8, descriptionFormat = "+{0}% Critical Chance"

            },

            new AffixDef

            {

                id = "affix_crit_dmg", name = "Critical Damage", category = AffixCategory.SecondaryStats,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.CriticalDamage,

                baseMinValue = 10, baseMaxValue = 25, descriptionFormat = "+{0}% Critical Damage"

            },

            new AffixDef

            {

                id = "affix_action_spd", name = "Action Speed", category = AffixCategory.SecondaryStats,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.ActionSpeed,

                baseMinValue = 5, baseMaxValue = 15, descriptionFormat = "+{0} Action Speed"

            },

            new AffixDef

            {

                id = "affix_evasion", name = "Evasion", category = AffixCategory.SecondaryStats,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.Evasion,

                baseMinValue = 2, baseMaxValue = 6, descriptionFormat = "+{0}% Evasion"

            },

            new AffixDef

            {

                id = "affix_block", name = "Block Power", category = AffixCategory.SecondaryStats,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.BlockPower,

                baseMinValue = 5, baseMaxValue = 12, descriptionFormat = "+{0}% Block Power"

            },



            // Physical & Elemental Offense

            new AffixDef

            {

                id = "affix_phys_pwr", name = "Physical Power", category = AffixCategory.Physical,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.PhysicalPower,

                baseMinValue = 2, baseMaxValue = 6, descriptionFormat = "+{0} Physical Power"

            },

            new AffixDef

            {

                id = "affix_mag_pwr", name = "Magic Power", category = AffixCategory.Elemental,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.MagicPower,

                baseMinValue = 2, baseMaxValue = 6, descriptionFormat = "+{0} Magic Power"

            },

            new AffixDef

            {

                id = "affix_armor_pen", name = "Armor Penetration", category = AffixCategory.Physical,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.ArmorPenetration,

                baseMinValue = 1, baseMaxValue = 4, descriptionFormat = "+{0} Armor Penetration"

            },

            new AffixDef

            {

                id = "affix_mag_pen", name = "Magic Penetration", category = AffixCategory.Elemental,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.MagicPenetration,

                baseMinValue = 1, baseMaxValue = 4, descriptionFormat = "+{0} Magic Penetration"

            },

            new AffixDef

            {

                id = "affix_phys_break", name = "Break Power", category = AffixCategory.Physical,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.PhysicalBreakPower,

                baseMinValue = 2, baseMaxValue = 5, descriptionFormat = "+{0} Physical Break Power"

            },



            // Defenses & Resistances

            new AffixDef

            {

                id = "affix_phys_def", name = "Physical Defense", category = AffixCategory.Defense,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.PhysicalDefense,

                baseMinValue = 2, baseMaxValue = 6, descriptionFormat = "+{0} Physical Defense"

            },

            new AffixDef

            {

                id = "affix_mag_def", name = "Magic Defense", category = AffixCategory.Defense,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.MagicDefense,

                baseMinValue = 2, baseMaxValue = 6, descriptionFormat = "+{0} Magic Defense"

            },

            new AffixDef

            {

                id = "affix_status_res", name = "Status Resistance", category = AffixCategory.StatusResistance,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.StatusResistance,

                baseMinValue = 5, baseMaxValue = 15, descriptionFormat = "+{0}% Status Resistance"

            },

            new AffixDef

            {

                id = "affix_max_hp", name = "Max HP", category = AffixCategory.Defense,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.MaxHP,

                baseMinValue = 10, baseMaxValue = 30, descriptionFormat = "+{0} Max HP"

            },

            new AffixDef

            {

                id = "affix_max_mp", name = "Max MP", category = AffixCategory.Defense,

                modifierType = AffixModifierType.Flat, derivedStat = DerivedStatId.MaxMP,

                baseMinValue = 5, baseMaxValue = 15, descriptionFormat = "+{0} Max MP"

            }

        }, a => a.id);



        // ---- 9. Special Effects Registry (GDD Section 7) ----

        public static readonly Dictionary<string, SpecialEffectDef> SpecialEffects = Index(new[]

        {

            // Legendary Effects

            new SpecialEffectDef

            {

                id = "infernal_resonance", name = "Infernal Resonance",

                description = "Physical attacks have a 25% chance to apply Burn.",

                rarity = EquipmentRarity.Legendary,

                burnChance = 0.25f,

                statModifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Infernal Resonance") }

            },

            new SpecialEffectDef

            {

                id = "aegis_ward", name = "Aegis Ward",

                description = "Grants +10 Physical Defense and +10 Magic Defense.",

                rarity = EquipmentRarity.Legendary,

                statModifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 10, ModifierKind.Flat, ModifierSource.Equipment, "Aegis Ward"),

                    StatModifier.ForDerived(DerivedStatId.MagicDefense, 10, ModifierKind.Flat, ModifierSource.Equipment, "Aegis Ward")

                }

            },

            new SpecialEffectDef

            {

                id = "warlords_fury", name = "Warlord's Fury",

                description = "+6 Weapon Power and +4 Break Power.",

                rarity = EquipmentRarity.Legendary,

                bonusBreakPower = 4,

                statModifiers = new[] { StatModifier.ForDerived(DerivedStatId.PhysicalPower, 6, ModifierKind.Flat, ModifierSource.Equipment, "Warlord's Fury") }

            },

            new SpecialEffectDef

            {

                id = "wind_step", name = "Wind Step",

                description = "+10% Evasion and +10 Action Speed.",

                rarity = EquipmentRarity.Legendary,

                statModifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.Evasion, 10, ModifierKind.Flat, ModifierSource.Equipment, "Wind Step"),

                    StatModifier.ForDerived(DerivedStatId.ActionSpeed, 10, ModifierKind.Flat, ModifierSource.Equipment, "Wind Step")

                }

            },



            // Mythic Effects

            new SpecialEffectDef

            {

                id = "absolute_zero", name = "Absolute Zero",

                description = "Elemental damage dealt to Frozen targets gains +30% damage bonus.",

                rarity = EquipmentRarity.Mythic,

                freezeBonusDamage = 0.30f,

                statModifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 8, ModifierKind.Flat, ModifierSource.Equipment, "Absolute Zero") }

            },

            new SpecialEffectDef

            {

                id = "titans_bulwark", name = "Titan's Bulwark",

                description = "+20 Physical Defense, +20 Magic Defense, and +15 Stagger Resistance.",

                rarity = EquipmentRarity.Mythic,

                statModifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.PhysicalDefense, 20, ModifierKind.Flat, ModifierSource.Equipment, "Titan's Bulwark"),

                    StatModifier.ForDerived(DerivedStatId.MagicDefense, 20, ModifierKind.Flat, ModifierSource.Equipment, "Titan's Bulwark"),

                    StatModifier.ForDerived(DerivedStatId.StaggerResistance, 15, ModifierKind.Flat, ModifierSource.Equipment, "Titan's Bulwark")

                }

            },

            new SpecialEffectDef

            {

                id = "chronos_mastery", name = "Chronos Mastery",

                description = "+25 Action Speed and +10% QTE timing bonus.",

                rarity = EquipmentRarity.Mythic,

                qteTimingWindowBonus = 10,

                statModifiers = new[]

                {

                    StatModifier.ForDerived(DerivedStatId.ActionSpeed, 25, ModifierKind.Flat, ModifierSource.Equipment, "Chronos Mastery"),

                    StatModifier.ForDerived(DerivedStatId.QteTimingBonus, 10, ModifierKind.Flat, ModifierSource.Equipment, "Chronos Mastery")

                }

            }

        }, s => s.id);



        // Exports the stat system configuration as JSON matching the PRD schemas.

        public static string ExportStatConfigJson()

        {

            var data = new StatSystemConfigData

            {

                pointBuy = PointBuyConfig.Standard,

                asi = AsiConfig,

                abilities = new List<AbilityScoreDef>(AbilityDefs.Values).ToArray(),

                derivedStats = new List<DerivedStatDef>(DerivedStatDefs.Values).ToArray(),

                formulas = new List<FormulaDef>(Formulas.Values).ToArray()

            };

            return JsonUtility.ToJson(data, true);

        }



        public static StatSystemConfigData ImportStatConfigJson(string json) =>

            JsonUtility.FromJson<StatSystemConfigData>(json);



        static Dictionary<string, T> Index<T>(T[] items, Func<T, string> key)

        {

            var d = new Dictionary<string, T>();

            foreach (var i in items) d.Add(key(i), i);

            return d;

        }

    }

}

