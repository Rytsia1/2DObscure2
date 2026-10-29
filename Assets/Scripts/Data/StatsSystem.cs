using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // The six Primary Ability Scores defined in the Character Stats PRD.
    public enum Ability
    {
        STR,
        DEX,
        VIT,
        INT,
        WIS,
        CHA
    }

    // All Derived Stats defined in the Character Stats PRD.
    public enum DerivedStatId
    {
        // Resource Stats
        MaxHP,
        MaxMP,
        MaxShield,
        // Offensive Stats
        PhysicalPower,
        RangedPower,
        MagicPower,
        HealingPower,
        SupportPower,
        CriticalChance,
        CriticalDamage,
        ArmorPenetration,
        MagicPenetration,
        // Defensive Stats
        PhysicalDefense,
        MagicDefense,
        Evasion,
        BlockPower,
        StatusResistance,
        StaggerResistance,
        // Turn Stats
        Initiative,
        ActionSpeed,
        // QTE Stats
        QteTimingBonus,
        QteStability,
        CounterPower,
        // Break Stats
        PhysicalBreakPower,
        MagicBreakPower
    }

    public enum ModifierKind
    {
        Flat,
        PercentAdd,
        Multiplier
    }

    public enum ModifierOrder
    {
        FlatBeforePercent,
        PercentBeforeFlat
    }

    public enum ModifierSource
    {
        Base,
        Class,
        Race,
        Background,
        Equipment,
        Passive,
        Upgrade,
        Relic,
        Buff,
        Debuff
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Accessory
    }

    // Holds the six Primary Ability Scores (or modifiers/bonuses for each of the six).
    [Serializable]
    public struct AbilityScores : IEquatable<AbilityScores>
    {
        public int str, dex, vit, intel, wis, cha;

        public static readonly Ability[] AllAbilities =
        {
            Ability.STR, Ability.DEX, Ability.VIT, Ability.INT, Ability.WIS, Ability.CHA
        };

        public AbilityScores(int str, int dex, int vit, int intel, int wis, int cha)
        {
            this.str = str;
            this.dex = dex;
            this.vit = vit;
            this.intel = intel;
            this.wis = wis;
            this.cha = cha;
        }

        public static AbilityScores Uniform(int value) =>
            new AbilityScores(value, value, value, value, value, value);

        public static AbilityScores Zero => new AbilityScores(0, 0, 0, 0, 0, 0);
        public static AbilityScores DefaultBase => Uniform(PointBuyConfig.DefaultBaseScore);

        public int this[Ability ability]
        {
            get => ability switch
            {
                Ability.STR => str,
                Ability.DEX => dex,
                Ability.VIT => vit,
                Ability.INT => intel,
                Ability.WIS => wis,
                Ability.CHA => cha,
                _ => 0
            };
            set
            {
                switch (ability)
                {
                    case Ability.STR: str = value; break;
                    case Ability.DEX: dex = value; break;
                    case Ability.VIT: vit = value; break;
                    case Ability.INT: intel = value; break;
                    case Ability.WIS: wis = value; break;
                    case Ability.CHA: cha = value; break;
                }
            }
        }

        public AbilityScores With(Ability ability, int value)
        {
            var copy = this;
            copy[ability] = value;
            return copy;
        }

        public AbilityScores Add(Ability ability, int delta)
        {
            var copy = this;
            copy[ability] += delta;
            return copy;
        }

        public AbilityScores Plus(AbilityScores b) =>
            new AbilityScores(str + b.str, dex + b.dex, vit + b.vit, intel + b.intel, wis + b.wis, cha + b.cha);

        public AbilityScores Clamp(int min, int max) =>
            new AbilityScores(
                Math.Clamp(str, min, max),
                Math.Clamp(dex, min, max),
                Math.Clamp(vit, min, max),
                Math.Clamp(intel, min, max),
                Math.Clamp(wis, min, max),
                Math.Clamp(cha, min, max));

        // Reusable DnD-inspired Ability Modifier formula: floor((AbilityScore - 10) / 2).
        // Uses mathematical floor so negative odd values floor toward negative infinity (e.g., 9 -> -1, 7 -> -2).
        public static int GetModifier(int abilityScore) =>
            (int)Math.Floor((abilityScore - 10) / 2.0);

        public int Modifier(Ability ability) => GetModifier(this[ability]);

        public AbilityScores Modifiers =>
            new AbilityScores(
                GetModifier(str),
                GetModifier(dex),
                GetModifier(vit),
                GetModifier(intel),
                GetModifier(wis),
                GetModifier(cha));

        public static string FormatMod(int mod) => mod >= 0 ? $"+{mod}" : mod.ToString();

        public bool IsZero => str == 0 && dex == 0 && vit == 0 && intel == 0 && wis == 0 && cha == 0;

        public string Summary()
        {
            var parts = new List<string>();
            foreach (var a in AllAbilities)
            {
                int v = this[a];
                if (v != 0) parts.Add($"{a} {(v > 0 ? "+" + v : v.ToString())}");
            }
            return parts.Count == 0 ? "NONE" : string.Join(", ", parts);
        }

        public bool Equals(AbilityScores other) =>
            str == other.str && dex == other.dex && vit == other.vit &&
            intel == other.intel && wis == other.wis && cha == other.cha;

        public override bool Equals(object obj) => obj is AbilityScores other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(str, dex, vit, intel, wis, cha);
    }

    // Separate Primary Stat sources so recalculation is always clean and non-destructive.
    [Serializable]
    public class AbilityScoreSheet
    {
        public AbilityScores baseScores = AbilityScores.DefaultBase;
        public AbilityScores racialModifiers = AbilityScores.Zero;
        public AbilityScores permanentBonuses = AbilityScores.Zero;
        public AbilityScores equipmentBonuses = AbilityScores.Zero;
        public AbilityScores temporaryModifiers = AbilityScores.Zero;
        public int minFinalScore = 1;
        public int maxFinalScore = 30;

        public AbilityScores UnmodifiedWithRace => baseScores.Plus(racialModifiers);

        public AbilityScores FinalScores =>
            baseScores
                .Plus(racialModifiers)
                .Plus(permanentBonuses)
                .Plus(equipmentBonuses)
                .Plus(temporaryModifiers)
                .Clamp(minFinalScore, maxFinalScore);

        public AbilityScores AbilityModifiers => FinalScores.Modifiers;

        public int Score(Ability a) => FinalScores[a];
        public int Mod(Ability a) => AbilityScores.GetModifier(Score(a));
    }

    // Data-driven Point Buy configuration and cost table.
    [Serializable]
    public class PointBuyConfig
    {
        public const int DefaultBudget = 27;
        public const int DefaultBaseScore = 8;
        public const int DefaultMinScore = 8;
        public const int DefaultMaxScoreBeforeRacial = 15;
        public const int DefaultMaxProgressionScore = 20;

        public int budget = DefaultBudget;
        public int baseScore = DefaultBaseScore;
        public int minScore = DefaultMinScore;
        public int maxScoreBeforeRacial = DefaultMaxScoreBeforeRacial;

        // Score -> cumulative Point Buy cost (8->0, 9->1, 10->2, 11->3, 12->4, 13->5, 14->7, 15->9).
        public static readonly IReadOnlyDictionary<int, int> DefaultCostTable = new Dictionary<int, int>
        {
            [8] = 0,
            [9] = 1,
            [10] = 2,
            [11] = 3,
            [12] = 4,
            [13] = 5,
            [14] = 7,
            [15] = 9
        };

        public static readonly PointBuyConfig Standard = new PointBuyConfig();

        public bool IsValidScore(int score) => score >= minScore && score <= maxScoreBeforeRacial && DefaultCostTable.ContainsKey(score);

        public int GetCost(int score)
        {
            if (!IsValidScore(score))
                throw new ArgumentOutOfRangeException(nameof(score), $"Ability score {score} is outside valid Point Buy range ({minScore}-{maxScoreBeforeRacial}).");
            return DefaultCostTable[score];
        }

        public int GetTotalCost(AbilityScores baseScores)
        {
            int total = 0;
            foreach (var a in AbilityScores.AllAbilities)
                total += GetCost(baseScores[a]);
            return total;
        }

        // Null when the allocation is valid; otherwise an explanation of why it is rejected.
        public string Validate(AbilityScores baseScores)
        {
            int total = 0;
            foreach (var a in AbilityScores.AllAbilities)
            {
                int s = baseScores[a];
                if (s < minScore) return $"{a} score ({s}) cannot be below {minScore}.";
                if (s > maxScoreBeforeRacial) return $"{a} score ({s}) cannot exceed {maxScoreBeforeRacial} before racial modifiers.";
                if (!DefaultCostTable.TryGetValue(s, out int cost)) return $"No Point Buy cost defined for score {s}.";
                total += cost;
            }
            if (total > budget) return $"Point Buy cost ({total}) exceeds budget ({budget}).";
            return null;
        }
    }

    // Manages Point Buy allocation and enforces all validation rules in the underlying system.
    [Serializable]
    public class PointBuyAllocation
    {
        public readonly PointBuyConfig Config;
        public AbilityScores Scores { get; private set; }

        public event Action Changed;

        public PointBuyAllocation(PointBuyConfig config = null)
        {
            Config = config ?? PointBuyConfig.Standard;
            Scores = AbilityScores.Uniform(Config.baseScore);
        }

        public int TotalSpent => Config.GetTotalCost(Scores);
        public int RemainingPoints => Config.budget - TotalSpent;
        public string ValidationError => Config.Validate(Scores);
        public bool IsValid => ValidationError == null && RemainingPoints >= 0;

        public int GetScore(Ability a) => Scores[a];
        public int GetCost(Ability a) => Config.GetCost(Scores[a]);

        // Additional points required to raise this ability by 1, or -1 if already at max (15).
        public int GetIncreaseCost(Ability a)
        {
            int cur = Scores[a];
            if (cur >= Config.maxScoreBeforeRacial) return -1;
            return Config.GetCost(cur + 1) - Config.GetCost(cur);
        }

        // Points refunded when lowering this ability by 1, or 0 if already at min (8).
        public int GetDecreaseRefund(Ability a)
        {
            int cur = Scores[a];
            if (cur <= Config.minScore) return 0;
            return Config.GetCost(cur) - Config.GetCost(cur - 1);
        }

        public bool CanIncrease(Ability a)
        {
            int cost = GetIncreaseCost(a);
            return cost >= 0 && cost <= RemainingPoints;
        }

        public bool CanDecrease(Ability a) => Scores[a] > Config.minScore;

        public bool TryIncrease(Ability a)
        {
            if (!CanIncrease(a)) return false;
            Scores = Scores.Add(a, 1);
            Changed?.Invoke();
            return true;
        }

        public bool TryDecrease(Ability a)
        {
            if (!CanDecrease(a)) return false;
            Scores = Scores.Add(a, -1);
            Changed?.Invoke();
            return true;
        }

        public bool TrySetScore(Ability a, int score)
        {
            if (!Config.IsValidScore(score)) return false;
            var candidate = Scores.With(a, score);
            if (Config.Validate(candidate) != null) return false;
            Scores = candidate;
            Changed?.Invoke();
            return true;
        }

        public bool TrySetAll(AbilityScores candidate)
        {
            if (Config.Validate(candidate) != null) return false;
            Scores = candidate;
            Changed?.Invoke();
            return true;
        }

        public void SetAllOrThrow(AbilityScores candidate)
        {
            var err = Config.Validate(candidate);
            if (err != null) throw new InvalidOperationException(err);
            Scores = candidate;
            Changed?.Invoke();
        }

        public void Reset()
        {
            Scores = AbilityScores.Uniform(Config.baseScore);
            Changed?.Invoke();
        }

        public AbilityScores PreviewFinalScores(AbilityScores racialModifiers) =>
            Scores.Plus(racialModifiers);

        public AbilityScores PreviewModifiers(AbilityScores racialModifiers) =>
            PreviewFinalScores(racialModifiers).Modifiers;
    }

    // Complete set of Derived Stats calculated from Primary Ability Scores, Class, Race, Background, Equipment, and Effects.
    [Serializable]
    public struct DerivedStats
    {
        // Resource Stats
        public int maxHp;
        public int maxMp;
        public int maxShield;

        // Offensive Stats
        public int physicalPower;
        public int rangedPower;
        public int magicPower;
        public int healingPower;
        public int supportPower;
        public int criticalChance;   // percentage (e.g. 5 = 5%)
        public int criticalDamage;   // percentage (e.g. 150 = 150%)
        public int armorPenetration;
        public int magicPenetration;

        // Defensive Stats
        public int physicalDefense;
        public int magicDefense;
        public int evasion;          // percentage (e.g. 5 = 5%)
        public int blockPower;       // percentage (e.g. 50 = 50%)
        public int statusResistance; // percentage
        public int staggerResistance;

        // Turn Stats
        public int initiative;
        public int actionSpeed;

        // QTE Stats
        public int qteTimingBonus;   // percentage (e.g. 6 = +6% timing window)
        public int qteStability;     // percentage (e.g. 8 = 8% reduced failure penalty)
        public int counterPower;

        // Break Stats
        public int physicalBreakPower;
        public int magicBreakPower;

        public float CritChanceFraction => Mathf.Clamp01(criticalChance / 100f);
        public float CritMultiplier => Math.Max(1f, criticalDamage / 100f);
        public float EvasionFraction => Mathf.Clamp(evasion / 100f, 0f, 0.75f);
        public float BlockFraction => Mathf.Clamp(blockPower / 100f, 0f, 0.90f);
        public float StatusResistFraction => Mathf.Clamp(statusResistance / 100f, 0f, 0.90f);
        public float QteTimingBonusFraction => Mathf.Clamp(qteTimingBonus / 100f, -0.15f, 0.25f);
        public float QteStabilityFraction => Mathf.Clamp(qteStability / 100f, 0f, 0.35f);

        public int Get(DerivedStatId id) => id switch
        {
            DerivedStatId.MaxHP => maxHp,
            DerivedStatId.MaxMP => maxMp,
            DerivedStatId.MaxShield => maxShield,
            DerivedStatId.PhysicalPower => physicalPower,
            DerivedStatId.RangedPower => rangedPower,
            DerivedStatId.MagicPower => magicPower,
            DerivedStatId.HealingPower => healingPower,
            DerivedStatId.SupportPower => supportPower,
            DerivedStatId.CriticalChance => criticalChance,
            DerivedStatId.CriticalDamage => criticalDamage,
            DerivedStatId.ArmorPenetration => armorPenetration,
            DerivedStatId.MagicPenetration => magicPenetration,
            DerivedStatId.PhysicalDefense => physicalDefense,
            DerivedStatId.MagicDefense => magicDefense,
            DerivedStatId.Evasion => evasion,
            DerivedStatId.BlockPower => blockPower,
            DerivedStatId.StatusResistance => statusResistance,
            DerivedStatId.StaggerResistance => staggerResistance,
            DerivedStatId.Initiative => initiative,
            DerivedStatId.ActionSpeed => actionSpeed,
            DerivedStatId.QteTimingBonus => qteTimingBonus,
            DerivedStatId.QteStability => qteStability,
            DerivedStatId.CounterPower => counterPower,
            DerivedStatId.PhysicalBreakPower => physicalBreakPower,
            DerivedStatId.MagicBreakPower => magicBreakPower,
            _ => 0
        };

        public void Set(DerivedStatId id, int v)
        {
            switch (id)
            {
                case DerivedStatId.MaxHP: maxHp = v; break;
                case DerivedStatId.MaxMP: maxMp = v; break;
                case DerivedStatId.MaxShield: maxShield = v; break;
                case DerivedStatId.PhysicalPower: physicalPower = v; break;
                case DerivedStatId.RangedPower: rangedPower = v; break;
                case DerivedStatId.MagicPower: magicPower = v; break;
                case DerivedStatId.HealingPower: healingPower = v; break;
                case DerivedStatId.SupportPower: supportPower = v; break;
                case DerivedStatId.CriticalChance: criticalChance = v; break;
                case DerivedStatId.CriticalDamage: criticalDamage = v; break;
                case DerivedStatId.ArmorPenetration: armorPenetration = v; break;
                case DerivedStatId.MagicPenetration: magicPenetration = v; break;
                case DerivedStatId.PhysicalDefense: physicalDefense = v; break;
                case DerivedStatId.MagicDefense: magicDefense = v; break;
                case DerivedStatId.Evasion: evasion = v; break;
                case DerivedStatId.BlockPower: blockPower = v; break;
                case DerivedStatId.StatusResistance: statusResistance = v; break;
                case DerivedStatId.StaggerResistance: staggerResistance = v; break;
                case DerivedStatId.Initiative: initiative = v; break;
                case DerivedStatId.ActionSpeed: actionSpeed = v; break;
                case DerivedStatId.QteTimingBonus: qteTimingBonus = v; break;
                case DerivedStatId.QteStability: qteStability = v; break;
                case DerivedStatId.CounterPower: counterPower = v; break;
                case DerivedStatId.PhysicalBreakPower: physicalBreakPower = v; break;
                case DerivedStatId.MagicBreakPower: magicBreakPower = v; break;
            }
        }

        public static bool IsPercentage(DerivedStatId id) =>
            id == DerivedStatId.CriticalChance ||
            id == DerivedStatId.CriticalDamage ||
            id == DerivedStatId.Evasion ||
            id == DerivedStatId.BlockPower ||
            id == DerivedStatId.StatusResistance ||
            id == DerivedStatId.QteTimingBonus ||
            id == DerivedStatId.QteStability;

        public string FormatValue(DerivedStatId id)
        {
            int val = Get(id);
            if (id == DerivedStatId.QteTimingBonus)
                return (val >= 0 ? "+" : "") + val + "%";
            return IsPercentage(id) ? val + "%" : val.ToString();
        }

        // Synchronizes with the legacy 6-stat struct used across existing combat/run code.
        public Stats ToLegacyStats() =>
            new Stats(maxHp, maxMp, physicalPower, physicalDefense, initiative, magicPower);
    }

    // A single modifier affecting either a Primary Ability Score or a Derived Stat.
    [Serializable]
    public class StatModifier
    {
        public ModifierSource source = ModifierSource.Base;
        public string sourceName = "";
        public bool isAbilityModifier;
        public Ability ability;
        public DerivedStatId derivedStat;
        public ModifierKind kind = ModifierKind.Flat;
        public float value;

        public static StatModifier ForAbility(Ability a, int flatBonus, ModifierSource src = ModifierSource.Equipment, string name = "") =>
            new StatModifier { isAbilityModifier = true, ability = a, kind = ModifierKind.Flat, value = flatBonus, source = src, sourceName = name };

        public static StatModifier ForDerived(DerivedStatId stat, float val, ModifierKind kind = ModifierKind.Flat, ModifierSource src = ModifierSource.Equipment, string name = "") =>
            new StatModifier { isAbilityModifier = false, derivedStat = stat, kind = kind, value = val, source = src, sourceName = name };
    }

    // Data-driven schema for an Ability Score definition (Section 13.1 of PRD).
    [Serializable]
    public class AbilityScoreDef
    {
        public string abilityId;
        public Ability ability;
        public string displayName;
        public string description;
        public int baseScore = 8;
        public int minimumScore = 8;
        public int maximumScore = 20;
        public bool pointBuyEnabled = true;
    }

    // Data-driven schema for a Derived Stat definition (Section 13.2 of PRD).
    [Serializable]
    public class DerivedStatDef
    {
        public string statId;
        public DerivedStatId id;
        public string displayName;
        public string shortName;
        public string description;
        public string[] primarySources;
        public string calculationType = "formula";
        public string formulaId;
    }

    // Data-driven schema for configurable combat and derived stat formulas (Section 13.3 of PRD).
    [Serializable]
    public class FormulaDef
    {
        public string formulaId;
        public string[] inputs;
        public string calculation;
        public Ability[] scalingAbilities = Array.Empty<Ability>();
        public float[] scalingWeights = Array.Empty<float>();
        public float baseConstant;
        public int minValue;
        public int maxValue = 9999;
        public ModifierOrder modifierOrder = ModifierOrder.FlatBeforePercent;
    }

    // Configurable Ability Score Improvement progression rules (Section 8 / PRD 11.2).
    [Serializable]
    public class AbilityScoreImprovementConfig
    {
        public int[] availableAtLevels = { 4, 8, 12, 16, 19 };
        public int pointsGranted = 2;
        public int maxAbilityScore = 20;

        public bool GrantsAtLevel(int level) =>
            availableAtLevels != null && Array.IndexOf(availableAtLevels, level) >= 0;
    }

    // Data-driven Equipment definition supporting primary & derived stat bonuses, weapon power, and defense.
    [Serializable]
    public class EquipmentDef
    {
        public string id, name, description;
        public EquipmentSlot slot;
        public string requiredProficiency;
        public int weaponPower;
        public float weaponScaling;
        public int physicalDefense;
        public int magicDefense;
        public AbilityScores abilityBonuses;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
    }

    // Data-driven Passive Skill definition.
    [Serializable]
    public class PassiveDef
    {
        public string id, name, description;
        public AbilityScores abilityBonuses;
        public StatModifier[] modifiers = Array.Empty<StatModifier>();
    }

    // Detailed breakdown of how a Derived Stat was calculated, used by UI tooltips and tests.
    public class StatBreakdown
    {
        public DerivedStatId statId;
        public string displayName;
        public string description;
        public string primarySources;
        public string formulaSummary;
        public int baseAndClassValue;
        public int abilityModifierContribution;
        public int raceAndBackgroundBonus;
        public int equipmentBonus;
        public int permanentBonus;
        public int temporaryBonus;
        public int finalValue;

        public string FormatTooltip()
        {
            string valStr = DerivedStats.IsPercentage(statId) ? $"{finalValue}%" : finalValue.ToString();
            var parts = new List<string>
            {
                $"{displayName.ToUpperInvariant()}: {valStr} (SRC: {primarySources})",
                description.ToUpperInvariant(),
                $"FORMULA: {formulaSummary.ToUpperInvariant()}"
            };
            var contrib = new List<string>();
            contrib.Add($"BASE {baseAndClassValue}");
            if (abilityModifierContribution != 0) contrib.Add($"MOD {(abilityModifierContribution >= 0 ? "+" : "")}{abilityModifierContribution}");
            if (raceAndBackgroundBonus != 0) contrib.Add($"TRAIT {(raceAndBackgroundBonus >= 0 ? "+" : "")}{raceAndBackgroundBonus}");
            if (equipmentBonus != 0) contrib.Add($"EQP {(equipmentBonus >= 0 ? "+" : "")}{equipmentBonus}");
            if (permanentBonus != 0) contrib.Add($"PERM {(permanentBonus >= 0 ? "+" : "")}{permanentBonus}");
            if (temporaryBonus != 0) contrib.Add($"BUFF {(temporaryBonus >= 0 ? "+" : "")}{temporaryBonus}");
            parts.Add("SOURCES: " + string.Join(" ", contrib));
            return string.Join("\n", parts);
        }
    }

    // Reusable, safe calculation engine for Derived Stats and Combat Resolution.
    public static class StatCalculator
    {
        public static ModifierOrder DefaultModifierOrder = ModifierOrder.FlatBeforePercent;

        // Evaluates a base value with flat, percentage, and multiplier modifiers in the configured order.
        public static int EvaluateModifiers(
            float baseValue,
            IEnumerable<StatModifier> modifiers,
            ModifierOrder order,
            int minValue = 0,
            int maxValue = 9999)
        {
            float flat = 0f;
            float percentAdd = 0f;
            float mult = 1f;

            if (modifiers != null)
            {
                foreach (var m in modifiers)
                {
                    switch (m.kind)
                    {
                        case ModifierKind.Flat: flat += m.value; break;
                        case ModifierKind.PercentAdd: percentAdd += m.value; break;
                        case ModifierKind.Multiplier: mult *= m.value; break;
                    }
                }
            }

            float result = order == ModifierOrder.FlatBeforePercent
                ? (baseValue + flat) * Math.Max(0f, 1f + percentAdd) * mult
                : (baseValue * Math.Max(0f, 1f + percentAdd) + flat) * mult;

            return Math.Clamp((int)Math.Round(result), minValue, maxValue);
        }

        // Calculates all Derived Stats from an AbilityScoreSheet, ClassDef, level, equipment, passives, and modifiers.
        public static DerivedStats Calculate(
            AbilityScoreSheet abilities,
            ClassDef classDef,
            int level = 1,
            IEnumerable<EquipmentDef> equipment = null,
            IEnumerable<StatModifier> modifiers = null,
            Stats permanentLegacyBonuses = default)
        {
            var mods = abilities.AbilityModifiers;
            var c = classDef ?? Database.Classes["knight"];
            int lvl = Math.Max(1, level);
            var eqList = equipment?.Where(e => e != null).ToList() ?? new List<EquipmentDef>();
            var allMods = new List<StatModifier>();
            if (modifiers != null) allMods.AddRange(modifiers);
            foreach (var eq in eqList)
                if (eq.modifiers != null) allMods.AddRange(eq.modifiers);

            int eqWeaponPower = eqList.Sum(e => e.weaponPower);
            float eqWeaponScaling = eqList.Sum(e => e.weaponScaling);
            int eqPhysDef = eqList.Sum(e => e.physicalDefense);
            int eqMagDef = eqList.Sum(e => e.magicDefense);

            int Eval(DerivedStatId id, float baseVal, int minVal = 0, int maxVal = 9999)
            {
                var statMods = allMods.Where(m => !m.isAbilityModifier && m.derivedStat == id);
                var order = Database.Formulas.TryGetValue(Database.DerivedStatDefs[id].formulaId, out var f)
                    ? f.modifierOrder
                    : DefaultModifierOrder;
                return EvaluateModifiers(baseVal, statMods, order, minVal, maxVal);
            }

            // 1. Resource Stats
            // MaxHP = BaseHP + (VITModifier * HPGrowth) + Level growth + bonuses
            float rawHp = c.baseHp + (lvl - 1) * c.levelHpGrowth + (mods.vit * c.hpGrowth) + permanentLegacyBonuses.maxHp;
            // MaxMP = BaseMP + (WISModifier * MPGrowth) + Level growth + bonuses
            float rawMp = c.baseMp + (lvl - 1) * c.levelMpGrowth + (mods.wis * c.mpGrowth) + permanentLegacyBonuses.maxMp;
            float rawShield = c.baseShield + Math.Max(0, mods.vit * c.shieldGrowth);

            // 2. Offensive Stats
            // PhysicalDamage/Power = WeaponPower + (STRModifier * WeaponScaling)
            float weaponPower = c.baseWeaponPower + eqWeaponPower + permanentLegacyBonuses.atk;
            float weaponScaling = c.weaponScaling + eqWeaponScaling;
            float rawPhysPower = weaponPower + (mods.str * weaponScaling);

            // RangedPower = RangedWeaponPower + (DEXModifier * RangedScaling)
            float rawRangedPower = c.baseRangedPower + eqWeaponPower + (mods.dex * c.rangedScaling);

            // MagicDamage/Power = SpellPower + (INTModifier * SpellScaling)
            float spellPower = c.baseSpellPower + permanentLegacyBonuses.mag;
            float rawMagPower = spellPower + (mods.intel * c.spellScaling);

            // HealingAmount/Power = BaseHealing + (WISModifier * HealingScaling)
            float rawHealPower = c.baseHealing + permanentLegacyBonuses.mag + (mods.wis * c.healingScaling);

            // SupportPower = BaseSupport + (CHAModifier * SupportScaling)
            float rawSupportPower = c.baseSupportPower + (mods.cha * c.supportScaling);

            float rawCritChance = c.baseCritChance + (mods.dex * c.critChanceScaling);
            float rawCritDamage = c.baseCritDamage;
            float rawArmorPen = c.baseArmorPen + Math.Max(0, mods.str * c.armorPenScaling);
            float rawMagicPen = c.baseMagicPen + Math.Max(0, mods.intel * c.magicPenScaling);

            // 3. Defensive Stats
            // PhysicalDefense = BaseDef + Armor + (VITModifier * DefScaling)
            float rawPhysDef = c.basePhysicalDefense + eqPhysDef + permanentLegacyBonuses.def + (mods.vit * c.physicalDefenseScaling);
            // MagicDefense = BaseMagDef + Equipment + (WISModifier * MagDefScaling)
            float rawMagDef = c.baseMagicDefense + eqMagDef + (mods.wis * c.magicDefenseScaling);
            float rawEvasion = c.baseEvasion + (mods.dex * c.evasionScaling);
            float rawBlock = c.baseBlockPower + (mods.str * c.blockScaling);
            float rawStatusRes = c.baseStatusResistance + ((mods.vit + mods.wis) * c.statusResistanceScaling);
            float rawStaggerRes = c.baseStaggerResistance + (mods.vit * c.staggerResistanceScaling);

            // 4. Turn Stats
            // Initiative = BaseInitiative + DEXModifier + InitiativeBonuses
            float rawInitiative = c.baseInitiative + mods.dex + permanentLegacyBonuses.spd;
            float rawActionSpeed = c.baseActionSpeed + (mods.dex * c.actionSpeedScaling);

            // 5. QTE Stats
            float rawQteTiming = c.baseQteTimingBonus + (mods.dex * c.qteTimingScaling);
            float rawQteStability = c.baseQteStability + Math.Max(0, mods.vit * c.qteStabilityScaling);
            float rawCounterPower = c.baseCounterPower + ((mods.str + mods.dex) * c.counterPowerScaling);

            // 6. Break Stats
            // Physical Break Power -> STR, Magic Break Power -> INT
            float rawPhysBreak = c.baseBreakPower + (mods.str * c.breakPowerScaling);
            float rawMagBreak = c.baseBreakPower + (mods.intel * c.breakPowerScaling);

            return new DerivedStats
            {
                maxHp = Eval(DerivedStatId.MaxHP, rawHp, 1, 9999),
                maxMp = Eval(DerivedStatId.MaxMP, rawMp, 0, 9999),
                maxShield = Eval(DerivedStatId.MaxShield, rawShield, 0, 9999),
                physicalPower = Eval(DerivedStatId.PhysicalPower, rawPhysPower, 1, 9999),
                rangedPower = Eval(DerivedStatId.RangedPower, rawRangedPower, 1, 9999),
                magicPower = Eval(DerivedStatId.MagicPower, rawMagPower, 1, 9999),
                healingPower = Eval(DerivedStatId.HealingPower, rawHealPower, 1, 9999),
                supportPower = Eval(DerivedStatId.SupportPower, rawSupportPower, 1, 9999),
                criticalChance = Eval(DerivedStatId.CriticalChance, rawCritChance, 0, 100),
                criticalDamage = Eval(DerivedStatId.CriticalDamage, rawCritDamage, 100, 500),
                armorPenetration = Eval(DerivedStatId.ArmorPenetration, rawArmorPen, 0, 999),
                magicPenetration = Eval(DerivedStatId.MagicPenetration, rawMagicPen, 0, 999),
                physicalDefense = Eval(DerivedStatId.PhysicalDefense, rawPhysDef, 0, 999),
                magicDefense = Eval(DerivedStatId.MagicDefense, rawMagDef, 0, 999),
                evasion = Eval(DerivedStatId.Evasion, rawEvasion, 0, 75),
                blockPower = Eval(DerivedStatId.BlockPower, rawBlock, 0, 90),
                statusResistance = Eval(DerivedStatId.StatusResistance, rawStatusRes, 0, 90),
                staggerResistance = Eval(DerivedStatId.StaggerResistance, rawStaggerRes, 0, 999),
                initiative = Eval(DerivedStatId.Initiative, rawInitiative, 1, 999),
                actionSpeed = Eval(DerivedStatId.ActionSpeed, rawActionSpeed, 10, 999),
                qteTimingBonus = Eval(DerivedStatId.QteTimingBonus, rawQteTiming, -15, 25),
                qteStability = Eval(DerivedStatId.QteStability, rawQteStability, 0, 35),
                counterPower = Eval(DerivedStatId.CounterPower, rawCounterPower, 1, 999),
                physicalBreakPower = Eval(DerivedStatId.PhysicalBreakPower, rawPhysBreak, 1, 999),
                magicBreakPower = Eval(DerivedStatId.MagicBreakPower, rawMagBreak, 1, 999),
            };
        }

        // Builds a complete StatBreakdown for tooltips on the Character Status Screen.
        public static StatBreakdown Explain(
            DerivedStatId statId,
            AbilityScoreSheet abilities,
            ClassDef classDef,
            int level = 1,
            IEnumerable<EquipmentDef> equipment = null,
            IEnumerable<StatModifier> modifiers = null,
            Stats permanentLegacyBonuses = default)
        {
            var def = Database.DerivedStatDefs[statId];
            var formula = Database.Formulas[def.formulaId];
            var c = classDef ?? Database.Classes["knight"];
            var mods = abilities.AbilityModifiers;
            int lvl = Math.Max(1, level);
            var eqList = equipment?.Where(e => e != null).ToList() ?? new List<EquipmentDef>();
            var allMods = new List<StatModifier>();
            if (modifiers != null) allMods.AddRange(modifiers);
            foreach (var eq in eqList)
                if (eq.modifiers != null) allMods.AddRange(eq.modifiers);

            var derived = Calculate(abilities, c, lvl, eqList, modifiers, permanentLegacyBonuses);

            int baseVal = 0;
            int abilityContrib = 0;
            int eqContrib = 0;
            int permContrib = 0;

            switch (statId)
            {
                case DerivedStatId.MaxHP:
                    baseVal = c.baseHp + (lvl - 1) * c.levelHpGrowth;
                    abilityContrib = (int)Math.Round(mods.vit * c.hpGrowth);
                    permContrib = permanentLegacyBonuses.maxHp;
                    break;
                case DerivedStatId.MaxMP:
                    baseVal = c.baseMp + (lvl - 1) * c.levelMpGrowth;
                    abilityContrib = (int)Math.Round(mods.wis * c.mpGrowth);
                    permContrib = permanentLegacyBonuses.maxMp;
                    break;
                case DerivedStatId.MaxShield:
                    baseVal = c.baseShield;
                    abilityContrib = Math.Max(0, (int)Math.Round(mods.vit * c.shieldGrowth));
                    break;
                case DerivedStatId.PhysicalPower:
                    baseVal = c.baseWeaponPower;
                    eqContrib = eqList.Sum(e => e.weaponPower);
                    abilityContrib = (int)Math.Round(mods.str * (c.weaponScaling + eqList.Sum(e => e.weaponScaling)));
                    permContrib = permanentLegacyBonuses.atk;
                    break;
                case DerivedStatId.RangedPower:
                    baseVal = c.baseRangedPower;
                    eqContrib = eqList.Sum(e => e.weaponPower);
                    abilityContrib = (int)Math.Round(mods.dex * c.rangedScaling);
                    break;
                case DerivedStatId.MagicPower:
                    baseVal = c.baseSpellPower;
                    abilityContrib = (int)Math.Round(mods.intel * c.spellScaling);
                    permContrib = permanentLegacyBonuses.mag;
                    break;
                case DerivedStatId.HealingPower:
                    baseVal = c.baseHealing;
                    abilityContrib = (int)Math.Round(mods.wis * c.healingScaling);
                    permContrib = permanentLegacyBonuses.mag;
                    break;
                case DerivedStatId.SupportPower:
                    baseVal = c.baseSupportPower;
                    abilityContrib = (int)Math.Round(mods.cha * c.supportScaling);
                    break;
                case DerivedStatId.CriticalChance:
                    baseVal = c.baseCritChance;
                    abilityContrib = (int)Math.Round(mods.dex * c.critChanceScaling);
                    break;
                case DerivedStatId.CriticalDamage:
                    baseVal = c.baseCritDamage;
                    break;
                case DerivedStatId.ArmorPenetration:
                    baseVal = c.baseArmorPen;
                    abilityContrib = Math.Max(0, (int)Math.Round(mods.str * c.armorPenScaling));
                    break;
                case DerivedStatId.MagicPenetration:
                    baseVal = c.baseMagicPen;
                    abilityContrib = Math.Max(0, (int)Math.Round(mods.intel * c.magicPenScaling));
                    break;
                case DerivedStatId.PhysicalDefense:
                    baseVal = c.basePhysicalDefense;
                    eqContrib = eqList.Sum(e => e.physicalDefense);
                    abilityContrib = (int)Math.Round(mods.vit * c.physicalDefenseScaling);
                    permContrib = permanentLegacyBonuses.def;
                    break;
                case DerivedStatId.MagicDefense:
                    baseVal = c.baseMagicDefense;
                    eqContrib = eqList.Sum(e => e.magicDefense);
                    abilityContrib = (int)Math.Round(mods.wis * c.magicDefenseScaling);
                    break;
                case DerivedStatId.Evasion:
                    baseVal = c.baseEvasion;
                    abilityContrib = (int)Math.Round(mods.dex * c.evasionScaling);
                    break;
                case DerivedStatId.BlockPower:
                    baseVal = c.baseBlockPower;
                    abilityContrib = (int)Math.Round(mods.str * c.blockScaling);
                    break;
                case DerivedStatId.StatusResistance:
                    baseVal = c.baseStatusResistance;
                    abilityContrib = (int)Math.Round((mods.vit + mods.wis) * c.statusResistanceScaling);
                    break;
                case DerivedStatId.StaggerResistance:
                    baseVal = c.baseStaggerResistance;
                    abilityContrib = (int)Math.Round(mods.vit * c.staggerResistanceScaling);
                    break;
                case DerivedStatId.Initiative:
                    baseVal = c.baseInitiative;
                    abilityContrib = mods.dex;
                    permContrib = permanentLegacyBonuses.spd;
                    break;
                case DerivedStatId.ActionSpeed:
                    baseVal = c.baseActionSpeed;
                    abilityContrib = (int)Math.Round(mods.dex * c.actionSpeedScaling);
                    break;
                case DerivedStatId.QteTimingBonus:
                    baseVal = c.baseQteTimingBonus;
                    abilityContrib = (int)Math.Round(mods.dex * c.qteTimingScaling);
                    break;
                case DerivedStatId.QteStability:
                    baseVal = c.baseQteStability;
                    abilityContrib = Math.Max(0, (int)Math.Round(mods.vit * c.qteStabilityScaling));
                    break;
                case DerivedStatId.CounterPower:
                    baseVal = c.baseCounterPower;
                    abilityContrib = (int)Math.Round((mods.str + mods.dex) * c.counterPowerScaling);
                    break;
                case DerivedStatId.PhysicalBreakPower:
                    baseVal = c.baseBreakPower;
                    abilityContrib = (int)Math.Round(mods.str * c.breakPowerScaling);
                    break;
                case DerivedStatId.MagicBreakPower:
                    baseVal = c.baseBreakPower;
                    abilityContrib = (int)Math.Round(mods.intel * c.breakPowerScaling);
                    break;
            }

            var relevantMods = allMods.Where(m => !m.isAbilityModifier && m.derivedStat == statId).ToList();
            int raceBgContrib = (int)Math.Round(relevantMods.Where(m => m.source == ModifierSource.Race || m.source == ModifierSource.Background).Sum(m => m.value));
            eqContrib += (int)Math.Round(relevantMods.Where(m => m.source == ModifierSource.Equipment).Sum(m => m.value));
            permContrib += (int)Math.Round(relevantMods.Where(m => m.source == ModifierSource.Passive || m.source == ModifierSource.Upgrade || m.source == ModifierSource.Relic).Sum(m => m.value));
            int tempContrib = (int)Math.Round(relevantMods.Where(m => m.source == ModifierSource.Buff || m.source == ModifierSource.Debuff).Sum(m => m.value));

            return new StatBreakdown
            {
                statId = statId,
                displayName = def.displayName,
                description = def.description,
                primarySources = string.Join("+", def.primarySources),
                formulaSummary = formula.calculation,
                baseAndClassValue = baseVal,
                abilityModifierContribution = abilityContrib,
                raceAndBackgroundBonus = raceBgContrib,
                equipmentBonus = eqContrib,
                permanentBonus = permContrib,
                temporaryBonus = tempContrib,
                finalValue = derived.Get(statId)
            };
        }

        // Defense mitigation formula from PRD Section 8.8:
        // DamageAfterDefense = RawDamage * (100 / (100 + EffectiveDefense))
        public static float MitigateByDefense(float rawDamage, int defense, int penetration = 0)
        {
            int effectiveDefense = Math.Max(0, defense - Math.Max(0, penetration));
            return rawDamage * (100f / (100f + effectiveDefense));
        }
    }
}

