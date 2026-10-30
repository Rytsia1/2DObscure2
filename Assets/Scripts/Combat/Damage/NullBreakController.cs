using System;
using UnityEngine;

namespace Obscure
{
    // Identifies a targetable enemy affinity channel (Physical or one of the 8 Elemental types, Prompt Section 8.2).
    [Serializable]
    public struct AffinityDamageKey : IEquatable<AffinityDamageKey>
    {
        public bool isPhysical;
        public ElementType elementType;

        public AffinityDamageKey(bool isPhysical, ElementType elementType)
        {
            this.isPhysical = isPhysical;
            this.elementType = isPhysical ? ElementType.None : elementType;
        }

        public static AffinityDamageKey Physical => new AffinityDamageKey(true, ElementType.None);

        public static AffinityDamageKey Elemental(ElementType element)
        {
            if (!element.IsValidElement())
                throw new ArgumentException($"Elemental AffinityDamageKey requires a valid ElementType (got {element}).", nameof(element));
            return new AffinityDamageKey(false, element);
        }

        public bool IsValid => isPhysical || elementType.IsValidElement();

        public bool Matches(DamageType damageType, ElementType element = ElementType.None)
        {
            if (damageType == DamageType.Physical) return isPhysical;
            if (damageType == DamageType.Elemental) return !isPhysical && elementType == element && element.IsValidElement();
            return false;
        }

        public bool Equals(AffinityDamageKey other) =>
            isPhysical == other.isPhysical && elementType == other.elementType;

        public override bool Equals(object obj) => obj is AffinityDamageKey other && Equals(other);

        public override int GetHashCode() => (isPhysical ? 1 : 0) * 397 ^ (int)elementType;

        public override string ToString() => isPhysical ? "Physical" : elementType.ToString();
    }

    [Serializable]
    public struct NullBreakInstance
    {
        public AffinityDamageKey targetKey;
        public int remainingTurns;
        public int appliedSkillLevel;

        public bool IsActive => remainingTurns > 0 && targetKey.IsValid;
    }

    // Manages the single active Null Break state on an enemy (Prompt Section 8.3 & GDD Section 3.3).
    // Rules:
    // - Temporarily converts targeted Nullification (0x) -> Normal (1.0x).
    // - Does NOT affect Weakness or Resistant.
    // - Only 1 active Null Break per enemy at a time.
    // - Does NOT stack, refresh, or extend while active.
    // - Duration by skill level: Lv1 = 1 turn, Lv2 = 2 turns, Lv3 = 3 turns.
    [Serializable]
    public class NullBreakController
    {
        [SerializeField] private NullBreakInstance activeInstance;

        public bool HasActiveNullBreak => activeInstance.IsActive;
        public NullBreakInstance ActiveInstance => activeInstance;
        public int RemainingTurns => activeInstance.IsActive ? activeInstance.remainingTurns : 0;

        public static int GetDurationForSkillLevel(int skillLevel) => skillLevel switch
        {
            <= 1 => 1,
            2 => 2,
            _ => 3
        };

        // Attempts to apply Null Break for the specified affinity key and skill level (1..3).
        // Returns false if a Null Break is already active (cannot stack, refresh, or overwrite/extend while active).
        public bool TryApply(AffinityDamageKey targetKey, int skillLevel = 1)
        {
            if (!targetKey.IsValid) return false;
            if (HasActiveNullBreak) return false;

            int duration = GetDurationForSkillLevel(skillLevel);
            activeInstance = new NullBreakInstance
            {
                targetKey = targetKey,
                remainingTurns = duration,
                appliedSkillLevel = Mathf.Clamp(skillLevel, 1, 3)
            };
            return true;
        }

        public bool TryApplyPhysical(int skillLevel = 1) => TryApply(AffinityDamageKey.Physical, skillLevel);

        public bool TryApplyElemental(ElementType element, int skillLevel = 1) =>
            TryApply(AffinityDamageKey.Elemental(element), skillLevel);

        public bool IsNullBreakActiveFor(AffinityDamageKey key) =>
            activeInstance.IsActive && activeInstance.targetKey.Equals(key);

        public bool IsNullBreakActiveFor(DamageType damageType, ElementType element = ElementType.None) =>
            activeInstance.IsActive && activeInstance.targetKey.Matches(damageType, element);

        // Advances the Null Break duration by 1 turn at the end of the enemy's turn.
        public void TickTurn()
        {
            if (!activeInstance.IsActive) return;
            activeInstance.remainingTurns--;
            if (activeInstance.remainingTurns <= 0)
            {
                Clear();
            }
        }

        public void Clear()
        {
            activeInstance = default;
        }
    }
}

