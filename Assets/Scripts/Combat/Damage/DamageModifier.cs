using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Allowed Damage Modifier scopes (Prompt Section 5.1 & 5.2).
    // Note: Almighty Damage has NO dedicated bonus or damage-taken scope; it only uses AllDamageBonus and AllDamageTaken (Prompt Section 3.3).
    public enum DamageModifierScope
    {
        PhysicalDamageBonus = 0,
        ElementalDamageBonus = 1,
        ElementSpecificDamageBonus = 2,
        AllDamageBonus = 3,
        PhysicalDamageTaken = 4,
        ElementalDamageTaken = 5,
        ElementSpecificDamageTaken = 6,
        AllDamageTaken = 7
    }

    // Represents an individual multiplicative damage modifier (Prompt Section 5.3).
    // Example: +20% bonus is stored as multiplier = 1.2f; -20% reduction is stored as multiplier = 0.8f.
    [Serializable]
    public struct DamageModifier
    {
        public string sourceId;
        public DamageModifierScope scope;
        public ElementType elementType;
        public float multiplier;

        public DamageModifier(DamageModifierScope scope, float multiplier, ElementType elementType = ElementType.None, string sourceId = null)
        {
            this.sourceId = sourceId ?? string.Empty;
            this.scope = scope;
            this.elementType = elementType;
            this.multiplier = Mathf.Max(0f, multiplier);
        }

        // Creates a multiplicative modifier from a signed percentage delta (e.g., +0.20f -> 1.20f, +0.50f -> 1.50f, -0.25f -> 0.75f).
        public static DamageModifier FromPercentDelta(DamageModifierScope scope, float percentDelta, ElementType elementType = ElementType.None, string sourceId = null) =>
            new DamageModifier(scope, Mathf.Max(0f, 1f + percentDelta), elementType, sourceId);
    }

    // Stores and evaluates damage modifiers for a combatant using strict multiplicative stacking (Prompt Section 5.3).
    // Example: 1.2f (+20%) and 1.5f (+50%) stack as 1.2f * 1.5f = 1.8f.
    [Serializable]
    public class DamageModifierSet
    {
        [SerializeField] private List<DamageModifier> modifiers = new List<DamageModifier>();

        public IReadOnlyList<DamageModifier> Modifiers => modifiers;

        public void Clear() => modifiers.Clear();

        public void AddModifier(DamageModifier modifier)
        {
            if ((modifier.scope == DamageModifierScope.ElementSpecificDamageBonus ||
                 modifier.scope == DamageModifierScope.ElementSpecificDamageTaken) &&
                !modifier.elementType.IsValidElement())
            {
                throw new ArgumentException($"Scope {modifier.scope} requires a valid ElementType.", nameof(modifier));
            }
            modifiers.Add(modifier);
        }

        public void AddMultiplier(DamageModifierScope scope, float multiplier, ElementType element = ElementType.None, string sourceId = null) =>
            AddModifier(new DamageModifier(scope, multiplier, element, sourceId));

        public void AddPercentBonus(DamageModifierScope scope, float percentDelta, ElementType element = ElementType.None, string sourceId = null) =>
            AddModifier(DamageModifier.FromPercentDelta(scope, percentDelta, element, sourceId));

        public int RemoveBySource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return 0;
            return modifiers.RemoveAll(m => m.sourceId == sourceId);
        }

        // Evaluates the product of all modifiers matching the specified scope (and element when element-specific).
        // Returns 1.0f when no matching modifiers are present.
        public float GetMultiplier(DamageModifierScope scope, ElementType element = ElementType.None)
        {
            float product = 1f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];
                if (mod.scope != scope) continue;

                if (scope == DamageModifierScope.ElementSpecificDamageBonus ||
                    scope == DamageModifierScope.ElementSpecificDamageTaken)
                {
                    if (mod.elementType != element) continue;
                }

                product *= Mathf.Max(0f, mod.multiplier);
            }
            return product;
        }

        public float GetPhysicalDamageBonus() => GetMultiplier(DamageModifierScope.PhysicalDamageBonus);
        public float GetElementalDamageBonus() => GetMultiplier(DamageModifierScope.ElementalDamageBonus);
        public float GetElementSpecificDamageBonus(ElementType element) => GetMultiplier(DamageModifierScope.ElementSpecificDamageBonus, element);
        public float GetAllDamageBonus() => GetMultiplier(DamageModifierScope.AllDamageBonus);

        public float GetPhysicalDamageTaken() => GetMultiplier(DamageModifierScope.PhysicalDamageTaken);
        public float GetElementalDamageTaken() => GetMultiplier(DamageModifierScope.ElementalDamageTaken);
        public float GetElementSpecificDamageTaken(ElementType element) => GetMultiplier(DamageModifierScope.ElementSpecificDamageTaken, element);
        public float GetAllDamageTaken() => GetMultiplier(DamageModifierScope.AllDamageTaken);

        public DamageModifierSet Clone()
        {
            var copy = new DamageModifierSet();
            copy.modifiers.AddRange(modifiers);
            return copy;
        }
    }
}

