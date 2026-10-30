using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Enemy Affinity states (Prompt Section 8.1 & GDD Section 3.1).
    public enum AffinityType
    {
        Normal = 0,        // 1.0x (0%)
        Weakness = 1,      // 1.5x (+50%)
        Resistant = 2,     // 0.5x (-50%)
        Nullification = 3  // 0.0x (0 damage)
    }

    public static class AffinityTypes
    {
        public const float WeaknessMultiplier = 1.5f;
        public const float NormalMultiplier = 1.0f;
        public const float ResistantMultiplier = 0.5f;
        public const float NullificationMultiplier = 0.0f;

        public static float GetMultiplier(this AffinityType affinity) => affinity switch
        {
            AffinityType.Weakness => WeaknessMultiplier,
            AffinityType.Resistant => ResistantMultiplier,
            AffinityType.Nullification => NullificationMultiplier,
            _ => NormalMultiplier
        };
    }

    [Serializable]
    public struct EnemyAffinityEntry
    {
        public ElementType element;
        public AffinityType affinity;

        public EnemyAffinityEntry(ElementType element, AffinityType affinity)
        {
            this.element = element;
            this.affinity = affinity;
        }
    }

    // Stores an enemy's affinity profile across Physical and all 8 Elemental damage types (Prompt Section 8.2).
    [Serializable]
    public class EnemyAffinityProfile
    {
        public AffinityType physicalAffinity = AffinityType.Normal;
        [SerializeField] private List<EnemyAffinityEntry> elementalAffinities = new List<EnemyAffinityEntry>();

        public AffinityType GetRawPhysicalAffinity() => physicalAffinity;

        public void SetPhysicalAffinity(AffinityType affinity) => physicalAffinity = affinity;

        public AffinityType GetRawElementalAffinity(ElementType element)
        {
            if (!element.IsValidElement()) return AffinityType.Normal;
            for (int i = 0; i < elementalAffinities.Count; i++)
            {
                if (elementalAffinities[i].element == element)
                    return elementalAffinities[i].affinity;
            }
            return AffinityType.Normal;
        }

        public void SetElementalAffinity(ElementType element, AffinityType affinity)
        {
            if (!element.IsValidElement())
                throw new ArgumentException($"SetElementalAffinity requires a valid ElementType (got {element}).", nameof(element));

            for (int i = 0; i < elementalAffinities.Count; i++)
            {
                if (elementalAffinities[i].element == element)
                {
                    elementalAffinities[i] = new EnemyAffinityEntry(element, affinity);
                    return;
                }
            }
            elementalAffinities.Add(new EnemyAffinityEntry(element, affinity));
        }

        public AffinityType GetRawAffinity(DamageType damageType, ElementType element = ElementType.None) => damageType switch
        {
            DamageType.Physical => physicalAffinity,
            DamageType.Elemental => GetRawElementalAffinity(element),
            _ => AffinityType.Normal // Almighty ignores Enemy Affinity
        };

        // Resolves effective affinity accounting for Null Break (Prompt Section 8.3).
        // Null Break ONLY converts Nullification -> Normal (1.0x) for the targeted damage key.
        // Weakness (1.5x) and Resistant (0.5x) are unaffected by Null Break.
        public AffinityType GetEffectiveAffinity(DamageType damageType, ElementType element = ElementType.None, NullBreakController nullBreak = null)
        {
            if (damageType == DamageType.Almighty)
                return AffinityType.Normal;

            AffinityType raw = GetRawAffinity(damageType, element);
            if (raw == AffinityType.Nullification &&
                nullBreak != null &&
                nullBreak.IsNullBreakActiveFor(damageType, element))
            {
                return AffinityType.Normal;
            }
            return raw;
        }

        public float GetEffectiveMultiplier(DamageType damageType, ElementType element = ElementType.None, NullBreakController nullBreak = null) =>
            GetEffectiveAffinity(damageType, element, nullBreak).GetMultiplier();

        public bool IsNullified(DamageType damageType, ElementType element = ElementType.None, NullBreakController nullBreak = null) =>
            damageType != DamageType.Almighty &&
            GetEffectiveAffinity(damageType, element, nullBreak) == AffinityType.Nullification;

        public EnemyAffinityProfile Clone()
        {
            var copy = new EnemyAffinityProfile
            {
                physicalAffinity = physicalAffinity
            };
            copy.elementalAffinities.AddRange(elementalAffinities);
            return copy;
        }

        // Bridges legacy Affinities (weak/resist/immune Element[]) into EnemyAffinityProfile.
        public static EnemyAffinityProfile FromLegacyAffinities(Affinities legacy)
        {
            var profile = new EnemyAffinityProfile();
            if (legacy == null) return profile;

            void ApplyList(Element[] list, AffinityType affinity)
            {
                if (list == null) return;
                for (int i = 0; i < list.Length; i++)
                {
                    var el = list[i];
                    if (el == Element.Physical)
                    {
                        profile.SetPhysicalAffinity(affinity);
                    }
                    else
                    {
                        var mapped = ElementTypes.FromLegacyElement(el);
                        if (mapped.IsValidElement())
                            profile.SetElementalAffinity(mapped, affinity);
                    }
                }
            }

            ApplyList(legacy.resist, AffinityType.Resistant);
            ApplyList(legacy.weak, AffinityType.Weakness);
            ApplyList(legacy.immune, AffinityType.Nullification);
            return profile;
        }
    }
}

