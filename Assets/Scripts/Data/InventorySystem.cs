using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    public enum AffixCategory
    {
        PrimaryStats,
        SecondaryStats,
        Physical,
        Elemental,
        Defense,
        ElementalResistance,
        StatusResistance
    }

    public enum AffixModifierType
    {
        Flat,
        Percent
    }

    [Serializable]
    public class AffixDef
    {
        public string id;
        public string name;
        public AffixCategory category;
        public AffixModifierType modifierType = AffixModifierType.Flat;
        public bool isAbility;
        public Ability ability;
        public DerivedStatId derivedStat;
        public float baseMinValue;
        public float baseMaxValue;
        public int weight = 10;
        public EquipmentRarity minRarity = EquipmentRarity.Uncommon;
        public EquipmentSlot[] validSlots; // null = any slot
        public string descriptionFormat;   // e.g. "+{0} STR" or "+{0}% Crit Chance"

        public bool IsValidForSlot(EquipmentSlot slot)
        {
            if (validSlots == null || validSlots.Length == 0) return true;
            return validSlots.Contains(slot);
        }
    }

    [Serializable]
    public class GeneratedAffix
    {
        public string affixId;
        public float value;

        public AffixDef Definition => Database.Affixes.TryGetValue(affixId, out var def) ? def : null;

        public GeneratedAffix() { }

        public GeneratedAffix(string affixId, float value)
        {
            this.affixId = affixId;
            this.value = value;
        }

        public StatModifier ToStatModifier(string sourceName = "Affix")
        {
            var def = Definition;
            if (def == null) return null;

            if (def.isAbility)
            {
                return StatModifier.ForAbility(def.ability, (int)Math.Round(value), ModifierSource.Equipment, sourceName);
            }
            else
            {
                var kind = def.modifierType == AffixModifierType.Percent ? ModifierKind.PercentAdd : ModifierKind.Flat;
                float modVal = def.modifierType == AffixModifierType.Percent ? (value / 100f) : value;
                return StatModifier.ForDerived(def.derivedStat, modVal, kind, ModifierSource.Equipment, sourceName);
            }
        }

        public string GetDescription()
        {
            var def = Definition;
            if (def == null) return $"Affix ({affixId}): +{value}";
            string valStr = def.modifierType == AffixModifierType.Percent ? $"{Math.Round(value, 1)}%" : $"{Math.Round(value)}";
            if (!string.IsNullOrEmpty(def.descriptionFormat))
                return string.Format(def.descriptionFormat, valStr);
            return $"+{valStr} {def.name}";
        }
    }

    [Serializable]
    public class SpecialEffectDef
    {
        public string id;
        public string name;
        public string description;
        public EquipmentRarity rarity = EquipmentRarity.Legendary;
        public StatModifier[] statModifiers = Array.Empty<StatModifier>();
        public AbilityScores abilityBonuses;

        // Combat triggers/hooks
        public float burnChance;
        public float freezeBonusDamage;
        public float startBattleHealPercent;
        public int bonusBreakPower;
        public int qteTimingWindowBonus;
    }

    [Serializable]
    public class EquipmentInstance
    {
        public string instanceId;
        public string definitionId;
        public int level = 1;
        public EquipmentRarity rarity = EquipmentRarity.Common;
        public int upgradeLevel = 0;
        public List<GeneratedAffix> affixes = new List<GeneratedAffix>();
        public string specialEffectId;
        public string boundCharacterId;
        public bool isEquipped;

        [System.NonSerialized]
        private EquipmentDef _localDef;

        public EquipmentDef Definition =>
            (!string.IsNullOrEmpty(definitionId) && Database.Equipment.TryGetValue(definitionId, out var def))
                ? def
                : _localDef;

        public SpecialEffectDef SpecialEffect =>
            !string.IsNullOrEmpty(specialEffectId) && Database.SpecialEffects.TryGetValue(specialEffectId, out var se)
                ? se
                : null;

        public EquipmentInstance()
        {
            instanceId = Guid.NewGuid().ToString("N");
        }

        public EquipmentInstance(EquipmentDef def, EquipmentRarity rarity = EquipmentRarity.Common, int level = 1)
        {
            instanceId = Guid.NewGuid().ToString("N");
            definitionId = def != null ? def.id : "";
            _localDef = def;
            this.level = Math.Max(1, level);
            this.rarity = rarity;
            this.upgradeLevel = 0;
        }

        // Upgrade increases Base Stats by +5% per upgrade level (+0..+5) per GDD Section 9.2
        public float UpgradeMultiplier => 1f + (upgradeLevel * 0.05f);

        public int EffectiveWeaponPower
        {
            get
            {
                var def = Definition;
                if (def == null) return 0;
                return (int)Math.Round(def.weaponPower * UpgradeMultiplier);
            }
        }

        public int EffectivePhysicalDefense
        {
            get
            {
                var def = Definition;
                if (def == null) return 0;
                return (int)Math.Round(def.physicalDefense * UpgradeMultiplier);
            }
        }

        public int EffectiveMagicDefense
        {
            get
            {
                var def = Definition;
                if (def == null) return 0;
                return (int)Math.Round(def.magicDefense * UpgradeMultiplier);
            }
        }

        public int EffectiveBreakPowerModifier
        {
            get
            {
                var def = Definition;
                if (def == null) return 0;
                int baseVal = (int)Math.Round(def.breakPowerModifier * UpgradeMultiplier);
                if (SpecialEffect != null) baseVal += SpecialEffect.bonusBreakPower;
                return baseVal;
            }
        }

        public AbilityScores EffectiveAbilityBonuses
        {
            get
            {
                var def = Definition;
                var bonuses = def != null ? def.abilityBonuses : AbilityScores.Zero;
                if (upgradeLevel > 0)
                {
                    // Base stat multiplier applies to base ability bonuses
                    bonuses = new AbilityScores(
                        (int)Math.Round(bonuses.str * UpgradeMultiplier),
                        (int)Math.Round(bonuses.dex * UpgradeMultiplier),
                        (int)Math.Round(bonuses.vit * UpgradeMultiplier),
                        (int)Math.Round(bonuses.intel * UpgradeMultiplier),
                        (int)Math.Round(bonuses.wis * UpgradeMultiplier),
                        (int)Math.Round(bonuses.cha * UpgradeMultiplier));
                }

                // Add affix ability bonuses (affixes are NOT scaled by upgrades per GDD Section 9)
                foreach (var af in affixes)
                {
                    var adef = af.Definition;
                    if (adef != null && adef.isAbility)
                    {
                        bonuses = bonuses.Add(adef.ability, (int)Math.Round(af.value));
                    }
                }

                if (SpecialEffect != null && !SpecialEffect.abilityBonuses.IsZero)
                    bonuses = bonuses.Plus(SpecialEffect.abilityBonuses);

                return bonuses;
            }
        }

        public List<StatModifier> GetAllModifiers()
        {
            var list = new List<StatModifier>();
            var def = Definition;
            if (def != null && def.modifiers != null)
            {
                foreach (var m in def.modifiers)
                {
                    if (m.kind == ModifierKind.Flat && !m.isAbilityModifier)
                    {
                        float upgraded = (float)Math.Round(m.value * UpgradeMultiplier);
                        list.Add(StatModifier.ForDerived(m.derivedStat, upgraded, m.kind, ModifierSource.Equipment, def.name));
                    }
                    else
                    {
                        list.Add(m);
                    }
                }
            }

            foreach (var af in affixes)
            {
                var sm = af.ToStatModifier(def != null ? def.name : "Equipment");
                if (sm != null && !sm.isAbilityModifier) list.Add(sm);
            }

            if (SpecialEffect != null && SpecialEffect.statModifiers != null)
            {
                list.AddRange(SpecialEffect.statModifiers);
            }

            return list;
        }

        public int CalculateSellValue()
        {
            var def = Definition;
            int baseVal = def != null && def.baseValue > 0 ? def.baseValue : 50;
            float rarityMult = EquipmentRarities.GetSellMultiplier(rarity);
            float levelMult = 1f + Math.Max(0, level - 1) * 0.1f;
            float upgradeMult = 1f + (upgradeLevel * 0.15f);
            return Math.Max(1, (int)Math.Round(baseVal * rarityMult * levelMult * upgradeMult));
        }

        public bool CanUpgrade(out string reason)
        {
            var def = Definition;
            int maxLvl = def != null ? def.maxUpgradeLevel : 5;
            if (upgradeLevel >= maxLvl)
            {
                reason = $"Already at maximum upgrade level (+{maxLvl}).";
                return false;
            }
            reason = null;
            return true;
        }

        public int GetUpgradeCostGold()
        {
            int baseCost = 100 * level;
            return (int)Math.Round(baseCost * (1f + upgradeLevel * 0.5f));
        }

        public int GetUpgradeCostMaterials()
        {
            return 1 + upgradeLevel;
        }

        public bool TryUpgrade(out string error)
        {
            if (!CanUpgrade(out error)) return false;
            upgradeLevel++;
            return true;
        }

        public EquipmentInstanceSaveData ToSaveData(EquipmentSlot? slot = null)
        {
            return new EquipmentInstanceSaveData
            {
                instanceId = instanceId,
                definitionId = definitionId,
                level = level,
                rarity = (int)rarity,
                upgradeLevel = upgradeLevel,
                affixes = affixes != null ? affixes.Select(a => new GeneratedAffixSaveData { affixId = a.affixId, value = a.value }).ToArray() : Array.Empty<GeneratedAffixSaveData>(),
                specialEffectId = specialEffectId,
                boundCharacterId = boundCharacterId,
                isEquipped = isEquipped,
                slot = slot.HasValue ? (int)slot.Value : -1
            };
        }

        public static EquipmentInstance FromSaveData(EquipmentInstanceSaveData data)
        {
            if (data == null) return null;
            var inst = new EquipmentInstance
            {
                instanceId = !string.IsNullOrEmpty(data.instanceId) ? data.instanceId : Guid.NewGuid().ToString("N"),
                definitionId = data.definitionId,
                level = Math.Max(1, data.level),
                rarity = (EquipmentRarity)data.rarity,
                upgradeLevel = Math.Max(0, data.upgradeLevel),
                specialEffectId = data.specialEffectId,
                boundCharacterId = data.boundCharacterId,
                isEquipped = data.isEquipped
            };
            if (data.affixes != null)
            {
                foreach (var a in data.affixes)
                {
                    if (a != null && !string.IsNullOrEmpty(a.affixId))
                        inst.affixes.Add(new GeneratedAffix(a.affixId, a.value));
                }
            }
            return inst;
        }

        // Produces an effective EquipmentDef for full backward compatibility with legacy systems
        public EquipmentDef ToEffectiveDef()
        {
            var def = Definition;
            if (def == null) return null;

            var copy = new EquipmentDef
            {
                id = def.id,
                name = upgradeLevel > 0 ? $"{def.name} +{upgradeLevel}" : def.name,
                description = def.description,
                slot = def.slot,
                weaponType = def.weaponType,
                attackRangeType = def.attackRangeType,
                isUnarmed = def.isUnarmed,
                rarity = rarity,
                requiredProficiency = def.requiredProficiency,
                weaponPower = EffectiveWeaponPower,
                weaponScaling = def.weaponScaling,
                accuracyModifier = def.accuracyModifier,
                breakPowerModifier = EffectiveBreakPowerModifier,
                weaponProperties = def.weaponProperties,
                weaponEffects = def.weaponEffects,
                physicalDefense = EffectivePhysicalDefense,
                magicDefense = EffectiveMagicDefense,
                abilityBonuses = EffectiveAbilityBonuses,
                modifiers = GetAllModifiers().ToArray(),
                restrictions = def.restrictions,
                equipmentLevel = level,
                maxUpgradeLevel = def.maxUpgradeLevel,
                baseValue = CalculateSellValue(),
                legendaryEffectId = def.legendaryEffectId,
                mythicEffectId = def.mythicEffectId,
                affixPoolId = def.affixPoolId
            };
            return copy;
        }
    }

    [Serializable]
    public class ConsumableStack
    {
        public string itemId;
        public int count;
        public int maxStack = 99;

        public ItemDef Definition =>
            !string.IsNullOrEmpty(itemId) && Database.Items.TryGetValue(itemId, out var def) ? def : null;

        public ConsumableStack() { }

        public ConsumableStack(string itemId, int count, int maxStack = 99)
        {
            this.itemId = itemId;
            this.count = count;
            this.maxStack = maxStack;
        }
    }

    public enum InventoryCategory
    {
        Equipment,
        Consumables,
        Materials,
        KeyItems
    }

    public enum EquipmentSortMode
    {
        RarityDesc,
        RarityAsc,
        LevelDesc,
        Slot,
        Name,
        SellValueDesc
    }

    [Serializable]
    public class InventoryRuntime
    {
        public const int MaxEquipmentCapacity = 100;

        public readonly List<EquipmentInstance> equipment = new List<EquipmentInstance>();
        public readonly List<ConsumableStack> consumables = new List<ConsumableStack>();
        public readonly Dictionary<string, int> materials = new Dictionary<string, int>();
        public readonly HashSet<string> keyItems = new HashSet<string>();

        public int EquipmentCount => equipment.Count(e => !e.isEquipped);
        public bool IsEquipmentFull => EquipmentCount >= MaxEquipmentCapacity;

        public bool CanAddEquipment => !IsEquipmentFull;

        public bool TryAddEquipment(EquipmentInstance item, out string error)
        {
            if (item == null)
            {
                error = "Item is null.";
                return false;
            }
            if (IsEquipmentFull)
            {
                error = "Inventory is full (100/100 slots).";
                return false;
            }
            item.isEquipped = false;
            equipment.Add(item);
            error = null;
            return true;
        }

        public bool RemoveEquipment(string instanceId)
        {
            int idx = equipment.FindIndex(e => e.instanceId == instanceId);
            if (idx >= 0)
            {
                equipment.RemoveAt(idx);
                return true;
            }
            return false;
        }

        public EquipmentInstance FindEquipment(string instanceId) =>
            equipment.FirstOrDefault(e => e.instanceId == instanceId);

        public void AddConsumable(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
            var existing = consumables.FirstOrDefault(c => c.itemId == itemId && c.count < c.maxStack);
            if (existing != null)
            {
                int space = existing.maxStack - existing.count;
                if (amount <= space)
                {
                    existing.count += amount;
                    return;
                }
                existing.count = existing.maxStack;
                amount -= space;
            }
            consumables.Add(new ConsumableStack(itemId, amount));
        }

        public bool RemoveConsumable(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
            var stack = consumables.FirstOrDefault(c => c.itemId == itemId);
            if (stack == null || stack.count < amount) return false;
            stack.count -= amount;
            if (stack.count <= 0) consumables.Remove(stack);
            return true;
        }

        public int GetConsumableCount(string itemId) =>
            consumables.Where(c => c.itemId == itemId).Sum(c => c.count);

        public void AddMaterial(string materialId, int amount = 1)
        {
            if (string.IsNullOrEmpty(materialId) || amount <= 0) return;
            if (materials.ContainsKey(materialId))
                materials[materialId] += amount;
            else
                materials[materialId] = amount;
        }

        public bool RemoveMaterial(string materialId, int amount = 1)
        {
            if (string.IsNullOrEmpty(materialId) || amount <= 0) return false;
            if (!materials.TryGetValue(materialId, out int current) || current < amount) return false;
            materials[materialId] -= amount;
            if (materials[materialId] <= 0) materials.Remove(materialId);
            return true;
        }

        public int GetMaterialCount(string materialId) =>
            materials.TryGetValue(materialId, out int val) ? val : 0;

        public bool AddKeyItem(string keyItemId)
        {
            if (string.IsNullOrEmpty(keyItemId)) return false;
            return keyItems.Add(keyItemId);
        }

        public bool HasKeyItem(string keyItemId) =>
            !string.IsNullOrEmpty(keyItemId) && keyItems.Contains(keyItemId);

        public IEnumerable<EquipmentInstance> GetFilteredEquipment(EquipmentSlot? slotFilter = null, EquipmentRarity? rarityFilter = null)
        {
            var query = equipment.Where(e => !e.isEquipped);
            if (slotFilter.HasValue)
            {
                query = query.Where(e => e.Definition != null && EquipmentValidator.SlotMatches(e.Definition, slotFilter.Value));
            }
            if (rarityFilter.HasValue)
            {
                query = query.Where(e => e.rarity == rarityFilter.Value);
            }
            return query;
        }

        public List<EquipmentInstance> GetSortedEquipment(EquipmentSortMode mode, EquipmentSlot? slotFilter = null, EquipmentRarity? rarityFilter = null)
        {
            var list = GetFilteredEquipment(slotFilter, rarityFilter).ToList();
            switch (mode)
            {
                case EquipmentSortMode.RarityDesc:
                    list.Sort((a, b) => b.rarity.CompareTo(a.rarity));
                    break;
                case EquipmentSortMode.RarityAsc:
                    list.Sort((a, b) => a.rarity.CompareTo(b.rarity));
                    break;
                case EquipmentSortMode.LevelDesc:
                    list.Sort((a, b) => b.level.CompareTo(a.level));
                    break;
                case EquipmentSortMode.Slot:
                    list.Sort((a, b) => (a.Definition?.slot ?? EquipmentSlot.MainHand).CompareTo(b.Definition?.slot ?? EquipmentSlot.MainHand));
                    break;
                case EquipmentSortMode.Name:
                    list.Sort((a, b) => string.Compare(a.Definition?.name ?? "", b.Definition?.name ?? "", StringComparison.OrdinalIgnoreCase));
                    break;
                case EquipmentSortMode.SellValueDesc:
                    list.Sort((a, b) => b.CalculateSellValue().CompareTo(a.CalculateSellValue()));
                    break;
            }
            return list;
        }

        public InventorySaveData ToSaveData()
        {
            return new InventorySaveData
            {
                equipment = equipment.Select(e => e.ToSaveData()).ToArray(),
                consumables = consumables.Select(c => new ConsumableStackSaveData
                {
                    itemId = c.itemId,
                    count = c.count,
                    maxStack = c.maxStack
                }).ToArray(),
                materials = materials.Select(kv => new MaterialEntrySaveData
                {
                    materialId = kv.Key,
                    count = kv.Value
                }).ToArray(),
                keyItems = keyItems.ToArray()
            };
        }

        public void RestoreFromSave(InventorySaveData data)
        {
            if (data == null) return;
            equipment.Clear();
            if (data.equipment != null)
            {
                foreach (var eqSave in data.equipment)
                {
                    var inst = EquipmentInstance.FromSaveData(eqSave);
                    if (inst != null) equipment.Add(inst);
                }
            }

            consumables.Clear();
            if (data.consumables != null)
            {
                foreach (var cSave in data.consumables)
                {
                    if (cSave != null && !string.IsNullOrEmpty(cSave.itemId))
                        consumables.Add(new ConsumableStack(cSave.itemId, cSave.count, cSave.maxStack > 0 ? cSave.maxStack : 99));
                }
            }

            materials.Clear();
            if (data.materials != null)
            {
                foreach (var mSave in data.materials)
                {
                    if (mSave != null && !string.IsNullOrEmpty(mSave.materialId))
                        materials[mSave.materialId] = mSave.count;
                }
            }

            keyItems.Clear();
            if (data.keyItems != null)
            {
                foreach (var k in data.keyItems)
                {
                    if (!string.IsNullOrEmpty(k)) keyItems.Add(k);
                }
            }
        }
    }

    public static class EquipmentManager
    {
        public static bool EquipFromInventory(Unit unit, EquipmentSlot slot, EquipmentInstance instance, InventoryRuntime inventory, out string error)
        {
            if (unit == null) { error = "Unit is null."; return false; }
            if (instance == null) { error = "Item is null."; return false; }

            if (!EquipmentValidator.SlotMatches(instance.Definition, slot))
            {
                error = $"{instance.Definition?.name ?? "Item"} cannot be equipped in {EquipmentSlots.DisplayName(slot)}.";
                return false;
            }

            var valErr = EquipmentValidator.Validate(instance.Definition, slot, unit.classDef, unit.race, unit.level, unit.FinalScores);
            if (valErr != null)
            {
                error = valErr;
                return false;
            }

            bool hasOld = unit.equippedInstances.TryGetValue(slot, out var oldItem) && oldItem != null;

            if (hasOld && oldItem != null)
            {
                oldItem.isEquipped = false;
                oldItem.boundCharacterId = null;
                if (inventory != null && !inventory.equipment.Contains(oldItem))
                {
                    inventory.equipment.Add(oldItem);
                }
            }

            unit.TryEquip(slot, instance, out error);
            if (inventory != null && !inventory.equipment.Contains(instance))
            {
                inventory.equipment.Add(instance);
            }
            return true;
        }

        public static bool UnequipToInventory(Unit unit, EquipmentSlot slot, InventoryRuntime inventory, out string error)
        {
            if (unit == null) { error = "Unit is null."; return false; }
            if (!unit.equippedInstances.TryGetValue(slot, out var current) || current == null)
            {
                if (!unit.equipped.TryGetValue(slot, out var def) || def == null)
                {
                    error = "Slot is empty.";
                    return false;
                }
                current = new EquipmentInstance(def, def.rarity, def.equipmentLevel);
            }

            if (inventory != null && inventory.IsEquipmentFull)
            {
                error = "Cannot unequip: Inventory is full (100/100 slots).";
                return false;
            }

            unit.Unequip(slot);
            if (inventory != null)
            {
                current.isEquipped = false;
                current.boundCharacterId = null;
                if (!inventory.equipment.Contains(current))
                    inventory.equipment.Add(current);
            }
            error = null;
            return true;
        }

        public static int CalculateStatDelta(Unit unit, EquipmentSlot slot, EquipmentInstance candidate, DerivedStatId statId)
        {
            if (unit == null) return 0;
            int currentVal = unit.Derived.Get(statId);

            var candidateDef = candidate != null ? candidate.ToEffectiveDef() : null;
            var simEquipped = new Dictionary<EquipmentSlot, EquipmentDef>(unit.equipped);
            if (candidateDef != null)
                simEquipped[slot] = candidateDef;
            else
                simEquipped.Remove(slot);

            var simAbil = AbilityScores.Zero;
            foreach (var eq in simEquipped.Values)
                if (eq != null) simAbil = simAbil.Plus(eq.abilityBonuses);
            foreach (var p in unit.passives)
                if (p != null) simAbil = simAbil.Plus(p.abilityBonuses);

            var tempSheet = new AbilityScoreSheet
            {
                baseScores = unit.BaseScores,
                racialModifiers = unit.RacialModifiers,
                permanentBonuses = unit.PermanentAbilityBonuses,
                equipmentBonuses = simAbil,
                temporaryModifiers = unit.TemporaryAbilityModifiers
            };

            var simDerived = StatCalculator.Calculate(
                tempSheet,
                unit.classDef,
                unit.level,
                simEquipped.Values,
                unit.GatherAllDerivedModifiers(),
                default,
                unit.passives);

            return simDerived.Get(statId) - currentVal;
        }
    }

    public static class EquipmentLootGenerator
    {
        public static EquipmentInstance GenerateLoot(
            int dungeonTier,
            Tier enemyTier = Tier.Normal,
            System.Random rng = null,
            EquipmentSlot? targetSlot = null,
            EquipmentRarity? guaranteedMinRarity = null)
        {
            var rand = rng ?? new System.Random();

            int minLvl = dungeonTier switch
            {
                1 => 1,
                2 => 3,
                3 => 6,
                4 => 9,
                5 => 12,
                _ => 1
            };
            int maxLvl = dungeonTier switch
            {
                1 => 2,
                2 => 5,
                3 => 8,
                4 => 11,
                5 => 15,
                _ => 2
            };
            int level = rand.Next(minLvl, maxLvl + 1);

            var rarity = RollRarity(dungeonTier, enemyTier, rand, guaranteedMinRarity);

            var candidates = Database.Equipment.Values.Where(e => !e.isUnarmed).ToList();
            if (targetSlot.HasValue)
            {
                candidates = candidates.Where(e => EquipmentValidator.SlotMatches(e, targetSlot.Value)).ToList();
            }
            if (candidates.Count == 0) candidates = Database.Equipment.Values.ToList();
            var baseDef = candidates[rand.Next(candidates.Count)];

            return CreateInstance(baseDef, rarity, level, rand);
        }

        public static EquipmentRarity RollRarity(int dungeonTier, Tier enemyTier, System.Random rand, EquipmentRarity? minRarity = null)
        {
            int[] weights = enemyTier switch
            {
                Tier.Boss => new[] { 0, 5, 40, 35, 15, 5 },
                Tier.Elite => new[] { 5, 25, 45, 20, 5, 0 },
                _ => new[] { 50, 35, 12, 3, 0, 0 }
            };

            if (dungeonTier >= 3)
            {
                weights[2] += 5;
                weights[3] += 5;
                if (dungeonTier >= 4) weights[4] += 5;
                if (dungeonTier >= 5) weights[5] += 2;
            }

            int total = weights.Sum();
            int roll = rand.Next(total);
            int accum = 0;
            EquipmentRarity rolled = EquipmentRarity.Common;
            for (int i = 0; i < weights.Length; i++)
            {
                accum += weights[i];
                if (roll < accum)
                {
                    rolled = (EquipmentRarity)i;
                    break;
                }
            }

            if (minRarity.HasValue && rolled < minRarity.Value)
                rolled = minRarity.Value;

            return rolled;
        }

        public static EquipmentInstance CreateInstance(EquipmentDef baseDef, EquipmentRarity rarity, int level, System.Random rand)
        {
            var inst = new EquipmentInstance(baseDef, rarity, level);

            int affixCount = EquipmentRarities.GetAffixCount(rarity);
            if (affixCount > 0)
            {
                var availableAffixes = Database.Affixes.Values
                    .Where(a => a.IsValidForSlot(baseDef.slot) && rarity >= a.minRarity)
                    .ToList();

                var chosen = new HashSet<string>();
                for (int i = 0; i < affixCount && availableAffixes.Count > chosen.Count; i++)
                {
                    var pool = availableAffixes.Where(a => !chosen.Contains(a.id)).ToList();
                    int weightSum = pool.Sum(a => a.weight);
                    int roll = rand.Next(weightSum);
                    int acc = 0;
                    AffixDef selected = pool[0];
                    foreach (var a in pool)
                    {
                        acc += a.weight;
                        if (roll < acc)
                        {
                            selected = a;
                            break;
                        }
                    }
                    chosen.Add(selected.id);

                    float rarityScale = 1f + ((int)rarity * 0.15f);
                    float levelScale = 1f + ((level - 1) * 0.10f);
                    float valRoll = (float)(rand.NextDouble() * (selected.baseMaxValue - selected.baseMinValue) + selected.baseMinValue);
                    float finalVal = (float)Math.Round(valRoll * rarityScale * levelScale);
                    finalVal = Math.Max(selected.baseMinValue, finalVal);

                    inst.affixes.Add(new GeneratedAffix(selected.id, finalVal));
                }
            }

            if (rarity == EquipmentRarity.Legendary)
            {
                if (!string.IsNullOrEmpty(baseDef.legendaryEffectId) && Database.SpecialEffects.ContainsKey(baseDef.legendaryEffectId))
                    inst.specialEffectId = baseDef.legendaryEffectId;
                else
                {
                    var pool = Database.SpecialEffects.Values.Where(s => s.rarity == EquipmentRarity.Legendary).ToList();
                    if (pool.Count > 0) inst.specialEffectId = pool[rand.Next(pool.Count)].id;
                }
            }
            else if (rarity == EquipmentRarity.Mythic)
            {
                if (!string.IsNullOrEmpty(baseDef.mythicEffectId) && Database.SpecialEffects.ContainsKey(baseDef.mythicEffectId))
                    inst.specialEffectId = baseDef.mythicEffectId;
                else
                {
                    var pool = Database.SpecialEffects.Values.Where(s => s.rarity == EquipmentRarity.Mythic).ToList();
                    if (pool.Count > 0) inst.specialEffectId = pool[rand.Next(pool.Count)].id;
                }
            }

            return inst;
        }
    }

    // ---- Save Data Structures (GDD Section 16) ----
    [Serializable]
    public class GeneratedAffixSaveData
    {
        public string affixId;
        public float value;
    }

    [Serializable]
    public class EquipmentInstanceSaveData
    {
        public string instanceId;
        public string definitionId;
        public int level;
        public int rarity;
        public int upgradeLevel;
        public GeneratedAffixSaveData[] affixes;
        public string specialEffectId;
        public string boundCharacterId;
        public bool isEquipped;
        public int slot = -1;
    }

    [Serializable]
    public class ConsumableStackSaveData
    {
        public string itemId;
        public int count;
        public int maxStack;
    }

    [Serializable]
    public class MaterialEntrySaveData
    {
        public string materialId;
        public int count;
    }

    [Serializable]
    public class InventorySaveData
    {
        public EquipmentInstanceSaveData[] equipment;
        public ConsumableStackSaveData[] consumables;
        public MaterialEntrySaveData[] materials;
        public string[] keyItems;
    }
}
