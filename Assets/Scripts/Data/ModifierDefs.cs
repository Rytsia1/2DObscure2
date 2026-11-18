using System;

namespace Obscure
{
    public enum ModifierCategory
    {
        Offense,
        Defense,
        QTE,
        Recovery,
        Utility
    }

    public enum ModifierPolarity
    {
        Buff,
        BuffWithDrawback,
        Debuff
    }

    public enum ModifierRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public enum ModifierDurationUnit
    {
        Battles,
        NextGoldReward,
        NextRewardRoll,
        OneShot
    }

    public enum ModifierTargetScope
    {
        Party,
        Run,
        LastActiveMember
    }

    public enum ModifierEffectType
    {
        PhysicalDamageBonus,
        ElementalDamageBonus,
        PhysicalDefensePenetration,
        OffensiveQteWindow,
        DefensiveQteWindow,
        AllDamageReduction,
        HealingReceived,
        SkillMpCostPercent,
        GoldRewardBonus,
        ConsumableDropChance,
        ImmediateHealPercent,
        ImmediateMpRestorePercent,
        ElementalDamageTakenReduction,
        BattleVictoryBonusGold,
        OffenseDamageAndHealingDrawback,
        AllDamageAndTakenDrawback,
        SpeedModifier,
        SkillMpCostFlat,
        BonusDamageVsBroken,
        ElementalDamageAttunement
    }

    // Static definition for a Dungeon Modifier (GDD Bab 15 & Felna_Dungeon_Modifier_List).
    [Serializable]
    public class DungeonModifierDef
    {
        public string modifierId;
        public string displayName;
        public string shortCode;
        public ModifierCategory category;
        public ModifierPolarity polarity;
        public ModifierRarity rarity;
        public ModifierEffectType effectType;

        // Per-stack numeric parameters (e.g. +0.05f = +5%, -0.10f = -10%, 1f = +1 MP).
        public float valuePerStack;
        public float drawbackValuePerStack;

        public int duration;
        public ModifierDurationUnit durationUnit;
        public int stackCap; // 0 = unlimited (for OneShot)
        public string categoryCapGroup;
        public ModifierTargetScope target;

        public bool isElementSpecific;
        public bool isPlayerElementChoice;

        public string sourceNodes;
        public string description;
        public string drawbackDescription;

        public bool IsOneShot => durationUnit == ModifierDurationUnit.OneShot;
        public bool HasDrawback => polarity == ModifierPolarity.BuffWithDrawback;
        public bool IsDebuff => polarity == ModifierPolarity.Debuff;
    }
}
