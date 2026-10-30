using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Additive resistance sources for a single element before the 75% cap (Prompt Section 9.1).
    [Serializable]
    public struct ElementalResistanceSources
    {
        public ElementType element;
        public float equipmentResistance;
        public float statsResistance;
        public float buffResistance;
        public float otherResistance;

        public float TotalUncapped =>
            equipmentResistance + statsResistance + buffResistance + otherResistance;

        public float FinalResistance =>
            Mathf.Clamp(TotalUncapped, 0f, ElementalResistanceProfile.MaxElementalResistance);
    }

    // Stores and evaluates percentage-based Elemental Resistance per element (Prompt Section 9 & GDD Section 4).
    // Sources stack additively and are clamped to [0f, 0.75f] (75% max resistance per element).
    // Applies ONLY to Elemental Damage (ignored by Physical and Almighty Damage).
    [Serializable]
    public class ElementalResistanceProfile
    {
        public const float MaxElementalResistance = 0.75f; // 75% cap

        [SerializeField] private List<ElementalResistanceSources> entries = new List<ElementalResistanceSources>();

        public ElementalResistanceSources GetSources(ElementType element)
        {
            if (!element.IsValidElement()) return default;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].element == element)
                    return entries[i];
            }
            return new ElementalResistanceSources { element = element };
        }

        public void SetSources(ElementType element, float equipment = 0f, float stats = 0f, float buff = 0f, float other = 0f)
        {
            if (!element.IsValidElement())
                throw new ArgumentException($"SetSources requires a valid ElementType (got {element}).", nameof(element));

            var updated = new ElementalResistanceSources
            {
                element = element,
                equipmentResistance = equipment,
                statsResistance = stats,
                buffResistance = buff,
                otherResistance = other
            };

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].element == element)
                {
                    entries[i] = updated;
                    return;
                }
            }
            entries.Add(updated);
        }

        public void AddEquipmentResistance(ElementType element, float delta)
        {
            var src = GetSources(element);
            SetSources(element, src.equipmentResistance + delta, src.statsResistance, src.buffResistance, src.otherResistance);
        }

        public void AddStatsResistance(ElementType element, float delta)
        {
            var src = GetSources(element);
            SetSources(element, src.equipmentResistance, src.statsResistance + delta, src.buffResistance, src.otherResistance);
        }

        public void AddBuffResistance(ElementType element, float delta)
        {
            var src = GetSources(element);
            SetSources(element, src.equipmentResistance, src.statsResistance, src.buffResistance + delta, src.otherResistance);
        }

        public void AddOtherResistance(ElementType element, float delta)
        {
            var src = GetSources(element);
            SetSources(element, src.equipmentResistance, src.statsResistance, src.buffResistance, src.otherResistance + delta);
        }

        public float GetUncappedResistance(ElementType element) => GetSources(element).TotalUncapped;

        // FinalResistance = Mathf.Clamp(EquipmentResistance + StatsResistance + BuffResistance + OtherResistance, 0f, 0.75f)
        public float GetFinalResistance(ElementType element)
        {
            if (!element.IsValidElement()) return 0f;
            return GetSources(element).FinalResistance;
        }

        // DamageReceived = IncomingDamage * (1f - FinalResistance)
        public float ApplyResistance(ElementType element, float incomingDamage)
        {
            if (!element.IsValidElement()) return incomingDamage;
            float finalRes = GetFinalResistance(element);
            return incomingDamage * (1f - finalRes);
        }

        public void Clear() => entries.Clear();

        public ElementalResistanceProfile Clone()
        {
            var copy = new ElementalResistanceProfile();
            copy.entries.AddRange(entries);
            return copy;
        }
    }
}

