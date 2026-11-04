using System;

namespace Obscure
{
    // Authoritative Item Definitions for Felna Dungeon 1 (Catalogue Spec 3.A, 3.D).
    
    [Serializable]
    public class ConsumableDef : ItemDef
    {
        // Inherits id, name, description, category, rarity, itemEffect, target,
        // usageRestriction, maxStack, shopValue, suggestedSource, heal, mana, isRevive, reviveHpPercent
    }

    [Serializable]
    public class MaterialDef
    {
        public string id;
        public string name;
        public string description;
        public string category; // "Upgrade Material", "Crafting Material"
        public EquipmentRarity rarity = EquipmentRarity.Common;
        public string primaryUse;
        public string suggestedSource;
        public int baseValue = 10;
        public int maxStack = 99;
    }

    [Serializable]
    public class KeyItemDef
    {
        public string id;
        public string name;
        public string description;
        public string category; // "Progression Key", "Boss Key", "Lore Item", "Final Objective"
        public string progressionFunction;
        public string loreDescription;
        public string suggestedSource;
        public string restriction; // "Not sellable; not normally discardable"
    }
}

