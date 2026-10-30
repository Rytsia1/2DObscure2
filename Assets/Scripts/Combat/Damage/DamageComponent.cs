using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Represents a single damage component within a skill hit (Prompt Section 3, 10, 12).
    // A skill hit can have one component (single-type) or multiple components (multi-type, e.g., Physical + Fire).
    [Serializable]
    public struct DamageComponent
    {
        public DamageType damageType;
        public ElementType elementType;
        public float skillPower;
        public float scaling;

        public DamageComponent(DamageType damageType, ElementType elementType, float skillPower, float scaling)
        {
            this.damageType = damageType;
            this.elementType = damageType == DamageType.Elemental ? elementType : ElementType.None;
            this.skillPower = skillPower;
            this.scaling = scaling;
        }

        public static DamageComponent Physical(float skillPower, float scaling) =>
            new DamageComponent(DamageType.Physical, ElementType.None, skillPower, scaling);

        public static DamageComponent Elemental(ElementType element, float skillPower, float scaling)
        {
            if (!element.IsValidElement())
                throw new ArgumentException($"Elemental DamageComponent requires a valid ElementType (got {element}).", nameof(element));
            return new DamageComponent(DamageType.Elemental, element, skillPower, scaling);
        }

        public static DamageComponent Almighty(float skillPower, float scaling) =>
            new DamageComponent(DamageType.Almighty, ElementType.None, skillPower, scaling);

        // Prompt Section 3:
        // Physical: BaseDamage = SkillPower + (STR * Scaling)
        // Elemental: BaseDamage = SkillPower + (INT * Scaling)
        // Almighty: BaseDamage = SkillPower + (INT * Scaling)
        public float CalculateBaseDamage(int attackerStr, int attackerInt)
        {
            return damageType switch
            {
                DamageType.Physical => skillPower + (attackerStr * scaling),
                DamageType.Elemental => skillPower + (attackerInt * scaling),
                DamageType.Almighty => skillPower + (attackerInt * scaling),
                _ => 0f
            };
        }

        public Ability ScalingStat =>
            damageType == DamageType.Physical ? Ability.STR : Ability.INT;

        public bool IsValid(out string error)
        {
            if (skillPower < 0f)
            {
                error = "SkillPower cannot be negative.";
                return false;
            }
            if (scaling < 0f)
            {
                error = "Scaling cannot be negative.";
                return false;
            }
            if (damageType == DamageType.Elemental && !elementType.IsValidElement())
            {
                error = "Elemental damage component must specify one of the 8 valid ElementType values.";
                return false;
            }
            error = null;
            return true;
        }
    }

    // Per-level skill damage configuration (Lv1, Lv2, Lv3) supporting SkillPower, Scaling, HitCount, and Null Break (Prompt Section 8.3 & 12).
    [Serializable]
    public class SkillDamageLevelData
    {
        [Min(1)] public int level = 1;
        [Min(1)] public int hitCount = 1;
        public List<DamageComponent> components = new List<DamageComponent>();
        public bool appliesNullBreak;
        public AffinityDamageKey nullBreakTarget;
    }

    // ScriptableObject asset for authoring skills in the Unity Inspector (Prompt Section 12 & 14).
    [CreateAssetMenu(menuName = "Obscure/Combat/Damage Skill Data", fileName = "NewDamageSkillData")]
    public class DamageSkillData : ScriptableObject
    {
        public string skillId;
        public string displayName;
        [Min(1)] public int defaultHitCount = 1;
        public List<DamageComponent> defaultComponents = new List<DamageComponent>();
        public List<SkillDamageLevelData> levelData = new List<SkillDamageLevelData>();

        public IReadOnlyList<DamageComponent> GetComponentsForLevel(int skillLevel)
        {
            var lvl = GetLevelEntry(skillLevel);
            if (lvl != null && lvl.components != null && lvl.components.Count > 0)
                return lvl.components;
            return defaultComponents;
        }

        public int GetHitCountForLevel(int skillLevel)
        {
            var lvl = GetLevelEntry(skillLevel);
            if (lvl != null && lvl.hitCount > 0)
                return lvl.hitCount;
            return Mathf.Max(1, defaultHitCount);
        }

        public SkillDamageLevelData GetLevelEntry(int skillLevel)
        {
            int clamped = Mathf.Clamp(skillLevel, 1, 3);
            if (levelData != null)
            {
                for (int i = 0; i < levelData.Count; i++)
                {
                    if (levelData[i] != null && levelData[i].level == clamped)
                        return levelData[i];
                }
            }
            return null;
        }
    }
}
