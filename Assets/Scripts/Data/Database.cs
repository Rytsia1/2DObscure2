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
                description = "Disciplined master of arms who excels at weapon techniques and frontline defense.",
                startingSkills = new[] { "shield_bash", "fortify", "interpose" },
                activeSkillPool = new[] { "shield_bash", "fortify", "interpose", "cleaving_strike" },
                passiveSkillPool = new[] { "weapon_proficiency", "iron_will", "battle_focus" },
                skillUnlockProgression = new[]
                {
                    new SkillUnlockNode { skillId = "cleaving_strike", tier = 2, requiredLevel = 3, skillPointCost = 1 },
                    new SkillUnlockNode { skillId = "iron_will", isPassive = true, tier = 2, requiredLevel = 3, skillPointCost = 1 }
                },
                mainHandWeapons = new[] { WeaponType.Sword, WeaponType.Greatsword, WeaponType.Axe, WeaponType.Mace, WeaponType.Spear },
                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.Buckler, WeaponType.ParryingDagger },
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
            // 2. Barbarian
            new ClassDef
            {
                id = "barbarian", name = "Barbarian", role = "Bruiser", isPlayable = true,
                gameplayIdentity = "Berserk State",
                starterPassiveId = "savage_instinct",
                description = "Ferocious warrior who grows deadlier as wounds mount.",
                startingSkills = new[] { "reckless_cleave", "war_cry", "shield_bash" },
                activeSkillPool = new[] { "reckless_cleave", "war_cry", "shield_bash" },
                passiveSkillPool = new[] { "savage_instinct", "battle_focus" },
                mainHandWeapons = new[] { WeaponType.Greataxe, WeaponType.Axe, WeaponType.Greatsword, WeaponType.Mace },
                secondHandWeapons = new[] { WeaponType.Warhorn, WeaponType.Buckler },
                startingEquipment = new[]
                {
                    Eq(EquipmentSlot.MainHand, "battle_greataxe"),
                    Eq(EquipmentSlot.SecondHand, "bone_warhorn"),
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
                sprite = Art.Knight,
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
                description = "Unarmed combatant whose flowing strikes shatter enemy guards.",
                startingSkills = new[] { "palm_strike", "quick_strike", "evasion" },
                activeSkillPool = new[] { "palm_strike", "quick_strike", "evasion" },
                passiveSkillPool = new[] { "martial_discipline", "battle_focus" },
                mainHandWeapons = new[] { WeaponType.Fist, WeaponType.Staff },
                secondHandWeapons = new[] { WeaponType.MartialFocus, WeaponType.ParryingDagger },
                startingEquipment = new[]
                {
                    Eq(EquipmentSlot.MainHand, "hand_wraps"),
                    Eq(EquipmentSlot.SecondHand, "ki_beads"),
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
                sprite = Art.Rogue,
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
                description = "Acts first and often. Exploits enemy weaknesses for lethal criticals.",
                startingSkills = new[] { "quick_strike", "expose", "evasion" },
                activeSkillPool = new[] { "quick_strike", "expose", "evasion" },
                passiveSkillPool = new[] { "opportunist", "battle_focus" },
                mainHandWeapons = new[] { WeaponType.Dagger, WeaponType.Sword, WeaponType.Crossbow },
                secondHandWeapons = new[] { WeaponType.ParryingDagger, WeaponType.Buckler },
                startingEquipment = new[]
                {
                    Eq(EquipmentSlot.MainHand, "iron_dagger"),
                    Eq(EquipmentSlot.SecondHand, "parrying_dagger"),
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
                description = "Deadly ranged marksman who marks quarry and strikes from afar.",
                startingSkills = new[] { "aimed_shot", "piercing_volley", "expose" },
                activeSkillPool = new[] { "aimed_shot", "piercing_volley", "expose" },
                passiveSkillPool = new[] { "hunters_instinct", "battle_focus" },
                mainHandWeapons = new[] { WeaponType.Bow, WeaponType.Crossbow, WeaponType.Spear, WeaponType.Dagger },
                secondHandWeapons = new[] { WeaponType.Quiver, WeaponType.ParryingDagger, WeaponType.Buckler },
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
                sprite = Art.Rogue,
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
                description = "Sacred conduit who mends wounds and wards the party against darkness.",
                startingSkills = new[] { "heal", "mend", "blessing" },
                activeSkillPool = new[] { "heal", "mend", "blessing" },
                passiveSkillPool = new[] { "divine_grace", "iron_will" },
                mainHandWeapons = new[] { WeaponType.Mace, WeaponType.Staff, WeaponType.Rod },
                secondHandWeapons = new[] { WeaponType.Shield, WeaponType.RelicSymbol, WeaponType.ArcaneTome },
                startingEquipment = new[]
                {
                    Eq(EquipmentSlot.MainHand, "sanctified_mace"),
                    Eq(EquipmentSlot.SecondHand, "sacred_chime"),
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
                description = "Master of arcane incantations who exploits elemental and runic weaknesses.",
                startingSkills = new[] { "firebolt", "frost_shard", "arcane_burst" },
                activeSkillPool = new[] { "firebolt", "frost_shard", "arcane_burst", "arcane_missile" },
                passiveSkillPool = new[] { "arcane_affinity", "iron_will" },
                mainHandWeapons = new[] { WeaponType.Staff, WeaponType.Wand, WeaponType.Rod },
                secondHandWeapons = new[] { WeaponType.ArcaneTome, WeaponType.RelicSymbol },
                startingEquipment = new[]
                {
                    Eq(EquipmentSlot.MainHand, "oak_staff"),
                    Eq(EquipmentSlot.SecondHand, "apprentice_tome"),
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
                description = "Wielder of forbidden pacts who channels dark and eldritch power.",
                startingSkills = new[] { "eldritch_pact", "soul_hex", "firebolt" },
                activeSkillPool = new[] { "eldritch_pact", "soul_hex", "firebolt" },
                passiveSkillPool = new[] { "pact_affinity", "iron_will" },
                mainHandWeapons = new[] { WeaponType.Rod, WeaponType.Wand, WeaponType.Staff, WeaponType.Dagger },
                secondHandWeapons = new[] { WeaponType.PactGrimoire, WeaponType.ArcaneTome },
                startingEquipment = new[]
                {
                    Eq(EquipmentSlot.MainHand, "eldritch_rod"),
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
                sprite = Art.Mage,
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
                description = "Primal guardian who weaves nature magic and restorative vitality.",
                startingSkills = new[] { "thorn_whip", "regrowth", "blessing" },
                activeSkillPool = new[] { "thorn_whip", "regrowth", "blessing" },
                passiveSkillPool = new[] { "natures_attunement", "iron_will" },
                mainHandWeapons = new[] { WeaponType.Staff, WeaponType.Spear, WeaponType.Mace },
                secondHandWeapons = new[] { WeaponType.NatureTotem, WeaponType.Buckler },
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
                sprite = Art.Healer,
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
                mainHandWeapons = new[] { WeaponType.Mace, WeaponType.Staff, WeaponType.Rod },
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

        // ---- 6. Backgrounds ----
        public static readonly Dictionary<string, BackgroundDef> Backgrounds = Index(new[]
        {
            new BackgroundDef
            {
                id = "oathsworn", name = "Oathsworn Guard",
                narrativeIdentity = "Former sentinel of the outer bastion.",
                description = "Trained to hold the line against abyssal incursions.",
                proficiencies = new[] { "Athletics", "Intimidation" },
                eventInteractions = new[] { "shrine", "fallen" },
                socialBonus = 1,
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.BlockPower, 4, ModifierKind.Flat, ModifierSource.Background, "Oathsworn Stance") }
            },
            new BackgroundDef
            {
                id = "outcast", name = "Crypt Outcast",
                narrativeIdentity = "Survivor of the sunken slums.",
                description = "Knows every trap, shadow, and hidden cache.",
                proficiencies = new[] { "Stealth", "Sleight of Hand" },
                eventInteractions = new[] { "fallen", "merchant" },
                socialBonus = 0,
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 3, ModifierKind.Flat, ModifierSource.Background, "Tunnel Reflexes") }
            },
            new BackgroundDef
            {
                id = "acolyte", name = "Abyssal Scholar",
                narrativeIdentity = "Keeper of forbidden inscriptions.",
                description = "Studied the runes of the deep before the seal cracked.",
                proficiencies = new[] { "Arcana", "History" },
                eventInteractions = new[] { "altar", "shrine" },
                socialBonus = 1,
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.MagicDefense, 2, ModifierKind.Flat, ModifierSource.Background, "Runic Ward") }
            },
            new BackgroundDef
            {
                id = "pilgrim", name = "Wandering Pilgrim",
                narrativeIdentity = "Bearer of the last lantern.",
                description = "Walked from shrine to shrine offering solace and counsel.",
                proficiencies = new[] { "Insight", "Persuasion" },
                eventInteractions = new[] { "shrine", "merchant" },
                socialBonus = 2,
                passiveEffects = new[] { StatModifier.ForDerived(DerivedStatId.SupportPower, 2, ModifierKind.Flat, ModifierSource.Background, "Inspiring Word") }
            },
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
                weaponType = WeaponType.PactGrimoire, magicDefense = 1,
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPenetration, 2, ModifierKind.Flat, ModifierSource.Equipment, "Pact Grimoire") },
                description = "Off-hand Pact Grimoire: +2 Magic Penetration, +1 Magic Defense."
            },
            new EquipmentDef
            {
                id = "verdant_totem", name = "Verdant Totem", slot = EquipmentSlot.SecondHand,
                weaponType = WeaponType.NatureTotem, magicDefense = 2,
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

            // Additional unlockable passives
            new PassiveDef
            {
                id = "iron_will", name = "Iron Will", tier = 2,
                description = "+1 VIT, +5 Status Resistance.",
                abilityBonuses = new AbilityScores(0, 0, 1, 0, 0, 0),
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Passive, "Iron Will") }
            },
            new PassiveDef
            {
                id = "battle_focus", name = "Battle Focus", tier = 2,
                description = "+3 Physical Power, +3 Physical Break.",
                modifiers = new[]
                {
                    StatModifier.ForDerived(DerivedStatId.PhysicalPower, 3, ModifierKind.Flat, ModifierSource.Passive, "Battle Focus"),
                    StatModifier.ForDerived(DerivedStatId.PhysicalBreakPower, 3, ModifierKind.Flat, ModifierSource.Passive, "Battle Focus")
                }
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
            // Fighter / Knight
            new SkillDef { id = "shield_bash", name = "Shield Bash", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.0f,
                scalingAbility = Ability.STR, breakMultiplier = 1.35f, bonusVsBroken = 0.25f,
                apply = new[] { Status(StatusId.DefenseDown, 2, 0.3f) }, description = "Bash a foe and crack its guard (DEF-)." },
            new SkillDef { id = "fortify", name = "Fortify", kind = SkillKind.Support, target = TargetType.Self, mpCost = 5, cooldown = 3,
                scalingAbility = Ability.CHA,
                apply = new[] { Status(StatusId.DefenseUp, 2, 0.6f) }, description = "Brace behind the shield (DEF+ 2 turns)." },
            new SkillDef { id = "interpose", name = "Interpose", kind = SkillKind.Support, target = TargetType.Ally, mpCost = 4, cooldown = 2,
                scalingAbility = Ability.CHA,
                apply = new[] { Status(StatusId.Guardian, 1, 1f, onSelf: true) }, description = "Take an ally's single-target hits until next turn." },
            new SkillDef { id = "cleaving_strike", name = "Cleaving Strike", kind = SkillKind.Physical, target = TargetType.AllEnemies, mpCost = 6, power = 0.9f,
                scalingAbility = Ability.STR, tier = 2, breakMultiplier = 1.2f, description = "Sweeping weapon technique hitting all foes." },

            // Barbarian
            new SkillDef { id = "reckless_cleave", name = "Reckless Cleave", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.35f,
                scalingAbility = Ability.STR, breakMultiplier = 1.4f, bonusVsBroken = 0.3f, description = "Heavy berserk chop that crushes enemy posture." },
            new SkillDef { id = "war_cry", name = "War Cry", kind = SkillKind.Support, target = TargetType.Self, mpCost = 4, cooldown = 3,
                scalingAbility = Ability.STR, apply = new[] { Status(StatusId.AttackUp, 2, 0.35f) }, description = "Unleash a furious roar (ATK+ 2 turns)." },

            // Martial Artist
            new SkillDef { id = "palm_strike", name = "Splitting Palm", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.0f,
                scalingAbility = Ability.STR, breakMultiplier = 1.65f, bonusVsBroken = 0.25f,
                tags = new[] { "Unarmed", "Combo" }, description = "Focused unarmed strike dealing high Break damage." },

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

            // Wizard / Mage (Arcane Spells)
            new SkillDef { id = "firebolt", name = "Firebolt", kind = SkillKind.Magic, element = Element.Fire, target = TargetType.Enemy, mpCost = 6, power = 1.6f,
                scalingAbility = Ability.INT, breakMultiplier = 1.15f, tags = new[] { "Arcane" },
                word = "IGNIS", description = "A bolt of fire." },
            new SkillDef { id = "frost_shard", name = "Frost Shard", kind = SkillKind.Magic, element = Element.Ice, target = TargetType.Enemy, mpCost = 7, power = 1.3f,
                scalingAbility = Ability.INT, breakMultiplier = 1.25f, tags = new[] { "Arcane" },
                word = "FROST", apply = new[] { Status(StatusId.Slow, 2, 0.4f, 0.6f) }, description = "Ice that may slow. Perfect: sure." },
            new SkillDef { id = "arcane_burst", name = "Arcane Burst", kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.AllEnemies, mpCost = 14, power = 1.3f,
                scalingAbility = Ability.INT, breakMultiplier = 1.4f, bonusVsBroken = 0.25f, tags = new[] { "Arcane" },
                word = "AETHERIS", difficulty = SkillDifficulty.Hard, cooldown = 2, description = "Raw arcana on all foes." },
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
            new SkillDef { id = "regrowth", name = "Regrowth", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 6, power = 1.6f,
                scalingAbility = Ability.WIS, tags = new[] { "Nature" },
                word = "FLORA", difficulty = SkillDifficulty.Easy, description = "Restorative nature magic that knits wounds." },

            // Cleric / Healer
            new SkillDef { id = "heal", name = "Heal", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 5, power = 1.5f,
                scalingAbility = Ability.WIS, tags = new[] { "Cleric" },
                word = "SANA", difficulty = SkillDifficulty.Easy, description = "Mend an ally's wounds." },
            new SkillDef { id = "mend", name = "Mend", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 11, power = 2.6f,
                scalingAbility = Ability.WIS, tags = new[] { "Cleric" },
                word = "VITALIS", description = "A deep restoration." },
            new SkillDef { id = "blessing", name = "Blessing", kind = SkillKind.Support, element = Element.Holy, target = TargetType.AllAllies, mpCost = 9, cooldown = 3,
                scalingAbility = Ability.CHA, tags = new[] { "Cleric" },
                apply = new[] { Status(StatusId.DefenseUp, 2, 0.3f) }, description = "Ward the party (DEF+ 2 turns)." },
        }, s => s.id);

        public static readonly Dictionary<string, ItemDef> Items = Index(new[]
        {
            new ItemDef { id = "draught", name = "Draught", heal = 40, description = "Restores 40 HP." },
            new ItemDef { id = "ether", name = "Ether", mana = 20, description = "Restores 20 MP." },
            new ItemDef { id = "elixir", name = "Grave Elixir", heal = 80, mana = 30, description = "Restores 80 HP and 30 MP." },
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
            new EncounterDef { id = "elite1", tier = Tier.Elite, enemies = new[] { "cursed_knight" } },
            new EncounterDef { id = "elite2", tier = Tier.Elite, enemies = new[] { "skeleton", "cursed_knight" }, minFloor = 4 },
            new EncounterDef { id = "boss", tier = Tier.Boss, enemies = new[] { "warden" } },
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
