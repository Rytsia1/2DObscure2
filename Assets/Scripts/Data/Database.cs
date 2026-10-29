using System;
using System.Collections.Generic;
using UnityEngine;

// All vertical-slice content and data-driven stat definitions in one place.
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

        // ---- 1. Ability Score Definitions (PRD Section 3 & 13.1) ----
        public static readonly Dictionary<Ability, AbilityScoreDef> AbilityDefs = new Dictionary<Ability, AbilityScoreDef>
        {
            [Ability.STR] = new AbilityScoreDef
            {
                abilityId = "STR", ability = Ability.STR, displayName = "Strength",
                description = "Physical power, heavy weapon scaling, physical break, and block power.",
                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true
            },
            [Ability.DEX] = new AbilityScoreDef
            {
                abilityId = "DEX", ability = Ability.DEX, displayName = "Dexterity",
                description = "Agility, evasion, initiative, ranged power, criticals, and QTE timing.",
                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true
            },
            [Ability.VIT] = new AbilityScoreDef
            {
                abilityId = "VIT", ability = Ability.VIT, displayName = "Vitality",
                description = "Endurance, max HP, physical defense, status/stagger resist, and QTE stability.",
                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true
            },
            [Ability.INT] = new AbilityScoreDef
            {
                abilityId = "INT", ability = Ability.INT, displayName = "Intelligence",
                description = "Arcane knowledge, magic power, spell damage, magic break, and penetration.",
                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true
            },
            [Ability.WIS] = new AbilityScoreDef
            {
                abilityId = "WIS", ability = Ability.WIS, displayName = "Wisdom",
                description = "Spiritual awareness, max MP, magic defense, healing power, and mental resist.",
                baseScore = 8, minimumScore = 8, maximumScore = 20, pointBuyEnabled = true
            },
            [Ability.CHA] = new AbilityScoreDef
            {
                abilityId = "CHA", ability = Ability.CHA, displayName = "Charisma",
                description = "Presence, leadership, support power, buff potency, and event social checks.",
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

        // ---- 4. Races (PRD Section 6.2) ----
        public static readonly Dictionary<string, RaceDef> Races = Index(new[]
        {
            new RaceDef
            {
                id = "human", name = "Human",
                description = "Adaptable survivors of the surface kingdoms.",
                abilityModifiers = new AbilityScores(1, 0, 1, 0, 0, 1),
                traits = new[] { "Versatile", "Dauntless" },
                passiveBonuses = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 3, ModifierKind.Flat, ModifierSource.Race, "Human Grit") }
            },
            new RaceDef
            {
                id = "elf", name = "Dusk Elf",
                description = "Swift hunters attuned to the twilight beneath the world.",
                abilityModifiers = new AbilityScores(0, 2, 0, 1, 0, 0),
                traits = new[] { "Keen Senses", "Twilight Step" },
                passiveBonuses = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 2, ModifierKind.Flat, ModifierSource.Race, "Keen Senses") }
            },
            new RaceDef
            {
                id = "highborn", name = "Highborn",
                description = "Scions of ancient citadels steeped in arcane study.",
                abilityModifiers = new AbilityScores(0, 0, 0, 2, 1, 0),
                traits = new[] { "Arcane Blood", "Lorekeeper" },
                passiveBonuses = new[] { StatModifier.ForDerived(DerivedStatId.MagicPenetration, 2, ModifierKind.Flat, ModifierSource.Race, "Arcane Blood") }
            },
            new RaceDef
            {
                id = "sanctified", name = "Sanctified",
                description = "Mortals touched by fading embers of sacred light.",
                abilityModifiers = new AbilityScores(0, 0, 0, 0, 2, 1),
                traits = new[] { "Bright Soul", "Serene Mind" },
                passiveBonuses = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 4, ModifierKind.Flat, ModifierSource.Race, "Bright Soul") }
            },
            new RaceDef
            {
                id = "ironbound", name = "Ironbound",
                description = "Stout delve-folk forged for endurance in the deep.",
                abilityModifiers = new AbilityScores(1, 0, 2, 0, 0, 0),
                traits = new[] { "Stoneborn", "Unyielding" },
                passiveBonuses = new[] { StatModifier.ForDerived(DerivedStatId.StaggerResistance, 5, ModifierKind.Flat, ModifierSource.Race, "Stoneborn") }
            },
        }, r => r.id);

        // ---- 5. Classes (PRD Section 6.3) ----
        public static readonly Dictionary<string, ClassDef> Classes = Index(new[]
        {
            new ClassDef
            {
                id = "knight", name = "Knight", role = "Defender",
                description = "Oathbound shield. Outlasts anything and stands between the dark and the weak.",
                startingSkills = new[] { "shield_bash", "fortify", "interpose" },
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
                id = "rogue", name = "Rogue", role = "Striker",
                description = "Acts first and often. Precise timing turns strikes into criticals.",
                startingSkills = new[] { "quick_strike", "expose", "evasion" },
                weaponProficiencies = new[] { "Dagger", "Shortblade", "Bow" },
                armorProficiencies = new[] { "Light" },
                classMechanics = new[] { "Precision Criticals", "Evasive Reflexes" },
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
            new ClassDef
            {
                id = "mage", name = "Mage", role = "Caster",
                description = "Fragile, but every foe has an element it fears.",
                startingSkills = new[] { "firebolt", "frost_shard", "arcane_burst" },
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
                id = "healer", name = "Healer", role = "Support",
                description = "Keeps the party standing through long fights.",
                startingSkills = new[] { "heal", "mend", "blessing" },
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

        // ---- 6. Backgrounds (PRD Section 6.4) ----
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

        // ---- 7. Equipment & Passives (PRD Section 8 & 11) ----
        public static readonly Dictionary<string, EquipmentDef> Equipment = Index(new[]
        {
            new EquipmentDef
            {
                id = "iron_broadsword", name = "Iron Broadsword", slot = EquipmentSlot.Weapon,
                requiredProficiency = "Sword", weaponPower = 3, weaponScaling = 0.5f,
                description = "+3 Weapon Power, +0.5 STR scaling."
            },
            new EquipmentDef
            {
                id = "assassin_dirk", name = "Assassin Dirk", slot = EquipmentSlot.Weapon,
                requiredProficiency = "Dagger", weaponPower = 2,
                abilityBonuses = new AbilityScores(0, 1, 0, 0, 0, 0),
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.CriticalChance, 5, ModifierKind.Flat, ModifierSource.Equipment, "Assassin Dirk") },
                description = "+2 Weapon Power, +1 DEX, +5% Crit Chance."
            },
            new EquipmentDef
            {
                id = "runed_staff", name = "Runed Staff", slot = EquipmentSlot.Weapon,
                requiredProficiency = "Staff", weaponPower = 1,
                abilityBonuses = new AbilityScores(0, 0, 0, 2, 0, 0),
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.MagicPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Runed Staff") },
                description = "+2 INT, +3 Magic Power."
            },
            new EquipmentDef
            {
                id = "steel_cuirass", name = "Steel Cuirass", slot = EquipmentSlot.Armor,
                requiredProficiency = "Heavy", physicalDefense = 4, magicDefense = 1,
                abilityBonuses = new AbilityScores(0, 0, 1, 0, 0, 0),
                description = "+4 Physical Defense, +1 Magic Defense, +1 VIT."
            },
            new EquipmentDef
            {
                id = "warded_vestments", name = "Warded Vestments", slot = EquipmentSlot.Armor,
                requiredProficiency = "Cloth", physicalDefense = 1, magicDefense = 4,
                abilityBonuses = new AbilityScores(0, 0, 0, 0, 1, 0),
                description = "+1 Physical Defense, +4 Magic Defense, +1 WIS."
            },
            new EquipmentDef
            {
                id = "signet_of_command", name = "Signet of Command", slot = EquipmentSlot.Accessory,
                abilityBonuses = new AbilityScores(0, 0, 0, 0, 0, 2),
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.SupportPower, 3, ModifierKind.Flat, ModifierSource.Equipment, "Signet of Command") },
                description = "+2 CHA, +3 Support Power."
            },
        }, e => e.id);

        public static readonly Dictionary<string, PassiveDef> Passives = Index(new[]
        {
            new PassiveDef
            {
                id = "iron_will", name = "Iron Will",
                description = "+1 VIT, +5 Status Resistance.",
                abilityBonuses = new AbilityScores(0, 0, 1, 0, 0, 0),
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.StatusResistance, 5, ModifierKind.Flat, ModifierSource.Passive, "Iron Will") }
            },
            new PassiveDef
            {
                id = "battle_focus", name = "Battle Focus",
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

        // ---- 8. QTE Stat Interactions (PRD Section 10.3) ----
        public static readonly Dictionary<Ability, QteStatInteractionDef> QteStatInteractions = new Dictionary<Ability, QteStatInteractionDef>
        {
            [Ability.DEX] = new QteStatInteractionDef { ability = Ability.DEX, effectName = "Timing Window Bonus", description = "Small bonus to applicable QTE timing windows.", scalingPerModifier = 0.03f, maxBonus = 0.15f },
            [Ability.VIT] = new QteStatInteractionDef { ability = Ability.VIT, effectName = "Reaction Stability", description = "Reduces selected failure and partial reaction penalties.", scalingPerModifier = 0.04f, maxBonus = 0.25f },
            [Ability.STR] = new QteStatInteractionDef { ability = Ability.STR, effectName = "Physical Counter Force", description = "Improves physical counterattack and perfect strike potency.", scalingPerModifier = 0.04f, maxBonus = 0.25f },
            [Ability.INT] = new QteStatInteractionDef { ability = Ability.INT, effectName = "Incantation Focus", description = "Grants slight extra letter time on spell reaction QTEs.", scalingPerModifier = 0.03f, maxBonus = 0.15f },
            [Ability.WIS] = new QteStatInteractionDef { ability = Ability.WIS, effectName = "Mental Resilience", description = "Helps against magical or mental enemy attack reactions.", scalingPerModifier = 0.03f, maxBonus = 0.15f },
            [Ability.CHA] = new QteStatInteractionDef { ability = Ability.CHA, effectName = "Command Presence", description = "Improves command and support skill effectiveness.", scalingPerModifier = 0.04f, maxBonus = 0.20f },
        };

        // Physical skills use the timing QTE; Magic/Heal skills with a word use the typing QTE; Support skills need none.
        public static readonly Dictionary<string, SkillDef> Skills = Index(new[]
        {
            // Knight
            new SkillDef { id = "shield_bash", name = "Shield Bash", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.0f,
                scalingAbility = Ability.STR, breakMultiplier = 1.35f, bonusVsBroken = 0.25f,
                apply = new[] { Status(StatusId.DefenseDown, 2, 0.3f) }, description = "Bash a foe and crack its guard (DEF-)." },
            new SkillDef { id = "fortify", name = "Fortify", kind = SkillKind.Support, target = TargetType.Self, mpCost = 5, cooldown = 3,
                scalingAbility = Ability.CHA,
                apply = new[] { Status(StatusId.DefenseUp, 2, 0.6f) }, description = "Brace behind the shield (DEF+ 2 turns)." },
            new SkillDef { id = "interpose", name = "Interpose", kind = SkillKind.Support, target = TargetType.Ally, mpCost = 4, cooldown = 2,
                scalingAbility = Ability.CHA,
                apply = new[] { Status(StatusId.Guardian, 1, 1f, onSelf: true) }, description = "Take an ally's single-target hits until next turn." },
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
            // Mage
            new SkillDef { id = "firebolt", name = "Firebolt", kind = SkillKind.Magic, element = Element.Fire, target = TargetType.Enemy, mpCost = 6, power = 1.6f,
                scalingAbility = Ability.INT, breakMultiplier = 1.15f,
                word = "IGNIS", description = "A bolt of fire." },
            new SkillDef { id = "frost_shard", name = "Frost Shard", kind = SkillKind.Magic, element = Element.Ice, target = TargetType.Enemy, mpCost = 7, power = 1.3f,
                scalingAbility = Ability.INT, breakMultiplier = 1.25f,
                word = "FROST", apply = new[] { Status(StatusId.Slow, 2, 0.4f, 0.6f) }, description = "Ice that may slow. Perfect: sure." },
            new SkillDef { id = "arcane_burst", name = "Arcane Burst", kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.AllEnemies, mpCost = 14, power = 1.3f,
                scalingAbility = Ability.INT, breakMultiplier = 1.4f, bonusVsBroken = 0.25f,
                word = "AETHERIS", difficulty = SkillDifficulty.Hard, cooldown = 2, description = "Raw arcana on all foes." },
            // Healer
            new SkillDef { id = "heal", name = "Heal", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 5, power = 1.5f,
                scalingAbility = Ability.WIS,
                word = "SANA", difficulty = SkillDifficulty.Easy, description = "Mend an ally's wounds." },
            new SkillDef { id = "mend", name = "Mend", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 11, power = 2.6f,
                scalingAbility = Ability.WIS,
                word = "VITALIS", description = "A deep restoration." },
            new SkillDef { id = "blessing", name = "Blessing", kind = SkillKind.Support, element = Element.Holy, target = TargetType.AllAllies, mpCost = 9, cooldown = 3,
                scalingAbility = Ability.CHA,
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

        // Encounter pools: map generation rolls from these by tier and floor.
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

        public const string BossRelic = "warden_eye"; // never rolled from pools

        public static readonly Dictionary<string, UpgradeDef> Upgrades = Index(new[]
        {
            new UpgradeDef { character = "knight", name = "Bulwark Drill", bonus = new Stats(15, 0, 0, 2, 0, 0) },
            new UpgradeDef { character = "rogue", name = "Honed Blades", bonus = new Stats(0, 0, 3, 0, 1, 0) },
            new UpgradeDef { character = "mage", name = "Deeper Focus", bonus = new Stats(0, 10, 0, 0, 0, 3) },
            new UpgradeDef { character = "healer", name = "Litany of Light", bonus = new Stats(10, 8, 0, 0, 0, 2) },
        }, u => u.character);

        // The last choice of every event is always available, so an event can never trap the player.
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
