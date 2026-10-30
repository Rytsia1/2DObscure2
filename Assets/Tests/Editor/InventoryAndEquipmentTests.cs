using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class InventoryAndEquipmentTests
    {
        [SetUp]
        public void SetUp()
        {
            GameSave.ClearSave();
        }

        [TearDown]
        public void TearDown()
        {
            GameSave.ClearSave();
        }

        // =====================================================================
        // 1. Equipment Rarity & Affix Generation Rules
        // =====================================================================

        [Test]
        public void EquipmentRarity_FixedAffixCount_And_SpecialEffects_FollowGDD()
        {
            Assert.AreEqual(0, EquipmentRarities.GetAffixCount(EquipmentRarity.Common));
            Assert.AreEqual(1, EquipmentRarities.GetAffixCount(EquipmentRarity.Uncommon));
            Assert.AreEqual(2, EquipmentRarities.GetAffixCount(EquipmentRarity.Rare));
            Assert.AreEqual(3, EquipmentRarities.GetAffixCount(EquipmentRarity.Epic));
            Assert.AreEqual(3, EquipmentRarities.GetAffixCount(EquipmentRarity.Legendary));
            Assert.AreEqual(4, EquipmentRarities.GetAffixCount(EquipmentRarity.Mythic));

            Assert.IsFalse(EquipmentRarities.HasSpecialEffect(EquipmentRarity.Common));
            Assert.IsFalse(EquipmentRarities.HasSpecialEffect(EquipmentRarity.Uncommon));
            Assert.IsFalse(EquipmentRarities.HasSpecialEffect(EquipmentRarity.Rare));
            Assert.IsFalse(EquipmentRarities.HasSpecialEffect(EquipmentRarity.Epic));
            Assert.IsTrue(EquipmentRarities.HasSpecialEffect(EquipmentRarity.Legendary));
            Assert.IsTrue(EquipmentRarities.HasSpecialEffect(EquipmentRarity.Mythic));
        }

        [Test]
        public void EquipmentLootGenerator_ProducesUniqueAffixes_And_SpecialEffects()
        {
            var rng = new System.Random(1337);
            var swordDef = Database.Equipment["iron_broadsword"];

            // 1. Rare equipment must roll exactly 2 unique affixes
            var rareItem = EquipmentLootGenerator.CreateInstance(swordDef, EquipmentRarity.Rare, level: 3, rng);
            Assert.AreEqual(EquipmentRarity.Rare, rareItem.rarity);
            Assert.AreEqual(2, rareItem.affixes.Count);
            Assert.AreNotEqual(rareItem.affixes[0].affixId, rareItem.affixes[1].affixId);
            Assert.IsNull(rareItem.SpecialEffect);

            // 2. Legendary equipment must roll exactly 3 unique affixes + 1 Legendary Special Effect
            var legendaryItem = EquipmentLootGenerator.CreateInstance(swordDef, EquipmentRarity.Legendary, level: 5, rng);
            Assert.AreEqual(EquipmentRarity.Legendary, legendaryItem.rarity);
            Assert.AreEqual(3, legendaryItem.affixes.Count);
            var affixIds = legendaryItem.affixes.Select(a => a.affixId).Distinct().ToList();
            Assert.AreEqual(3, affixIds.Count, "Affixes must be unique on a single item.");
            Assert.IsNotNull(legendaryItem.SpecialEffect);
            Assert.AreEqual(EquipmentRarity.Legendary, legendaryItem.SpecialEffect.rarity);

            // 3. Mythic equipment must roll exactly 4 unique affixes + 1 Mythic Special Effect
            var mythicItem = EquipmentLootGenerator.CreateInstance(swordDef, EquipmentRarity.Mythic, level: 10, rng);
            Assert.AreEqual(EquipmentRarity.Mythic, mythicItem.rarity);
            Assert.AreEqual(4, mythicItem.affixes.Count);
            var mythicAffixIds = mythicItem.affixes.Select(a => a.affixId).Distinct().ToList();
            Assert.AreEqual(4, mythicAffixIds.Count);
            Assert.IsNotNull(mythicItem.SpecialEffect);
            Assert.AreEqual(EquipmentRarity.Mythic, mythicItem.SpecialEffect.rarity);
        }

        // =====================================================================
        // 2. Inventory Capacity & Item Management
        // =====================================================================

        [Test]
        public void InventoryRuntime_Enforces100EquipmentCapacity_And_PreventsLoss()
        {
            var inv = new InventoryRuntime();
            var swordDef = Database.Equipment["iron_broadsword"];

            // Add 100 items
            for (int i = 0; i < InventoryRuntime.MaxEquipmentCapacity; i++)
            {
                var inst = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
                bool added = inv.TryAddEquipment(inst, out string err);
                Assert.IsTrue(added, $"Item {i} should be added.");
                Assert.IsNull(err);
            }

            Assert.AreEqual(100, inv.EquipmentCount);
            Assert.IsTrue(inv.IsEquipmentFull);

            // 101st item must fail with friendly feedback without corrupting inventory
            var overflowItem = new EquipmentInstance(swordDef, EquipmentRarity.Rare, 5);
            bool overflowAdded = inv.TryAddEquipment(overflowItem, out string overflowError);
            Assert.IsFalse(overflowAdded);
            Assert.IsNotNull(overflowError);
            StringAssert.Contains("full", overflowError);
            Assert.AreEqual(100, inv.EquipmentCount);

            // Remove an item and add again
            string firstId = inv.equipment[0].instanceId;
            Assert.IsTrue(inv.RemoveEquipment(firstId));
            Assert.AreEqual(99, inv.EquipmentCount);
            Assert.IsFalse(inv.IsEquipmentFull);

            Assert.IsTrue(inv.TryAddEquipment(overflowItem, out _));
            Assert.AreEqual(100, inv.EquipmentCount);
        }

        [Test]
        public void InventoryRuntime_ConsumablesStack_And_KeyItemsDoNotConsumeSlots()
        {
            var inv = new InventoryRuntime();

            // Stack consumables
            inv.AddConsumable("draught", 5);
            inv.AddConsumable("draught", 10);
            Assert.AreEqual(15, inv.GetConsumableCount("draught"));
            Assert.AreEqual(0, inv.EquipmentCount, "Consumables must not consume equipment slots.");

            Assert.IsTrue(inv.RemoveConsumable("draught", 5));
            Assert.AreEqual(10, inv.GetConsumableCount("draught"));

            // Key items
            Assert.IsTrue(inv.AddKeyItem("rusty_skeleton_key"));
            Assert.IsTrue(inv.HasKeyItem("rusty_skeleton_key"));
            Assert.IsFalse(inv.AddKeyItem("rusty_skeleton_key"), "Key items cannot duplicate.");
            Assert.AreEqual(0, inv.EquipmentCount, "Key items must not consume equipment slots.");
        }

        // =====================================================================
        // 3. Equipment Slot Restrictions & Compatibility
        // =====================================================================

        [Test]
        public void EquipmentCompatibility_EnforcesClass_Slot_And_LevelRequirements()
        {
            var knightDef = Database.Characters["knight"];
            var mageDef = Database.Characters["mage"];
            var knight = new Unit(knightDef);
            var mage = new Unit(mageDef);

            var swordDef = Database.Equipment["iron_broadsword"];
            var staffDef = Database.Equipment["oak_staff"];
            var helmDef = Database.Equipment["iron_helm"];

            // 1. Sword in MainHand for Knight succeeds; for Mage fails strict weapon compatibility
            var swordInst = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
            Assert.IsTrue(knight.TryEquip(EquipmentSlot.MainHand, swordInst, out string err1), err1);
            Assert.IsFalse(mage.TryEquip(EquipmentSlot.MainHand, swordInst, out string err2), "Mage cannot equip Broadsword.");
            StringAssert.Contains("Incompatible", err2);

            // Mage can equip Oak Staff
            var staffInst = new EquipmentInstance(staffDef, EquipmentRarity.Common, 1);
            Assert.IsTrue(mage.TryEquip(EquipmentSlot.MainHand, staffInst, out string errStaff), errStaff);

            // 2. Cannot equip HeadArmor into MainHand slot
            var helmInst = new EquipmentInstance(helmDef, EquipmentRarity.Common, 1);
            Assert.IsFalse(knight.TryEquip(EquipmentSlot.MainHand, helmInst, out string err3), "Helm cannot go into MainHand.");

            // 3. Level restrictions
            var highLevelArmorDef = new EquipmentDef
            {
                id = "dragon_scale_armor",
                name = "Dragon Scale Armor",
                slot = EquipmentSlot.BodyArmor,
                restrictions = new EquipmentRestriction { minLevel = 10 }
            };
            var highLevelInst = new EquipmentInstance(highLevelArmorDef, EquipmentRarity.Epic, 10);
            Assert.IsFalse(knight.TryEquip(EquipmentSlot.BodyArmor, highLevelInst, out string err4));
            StringAssert.Contains("requires level 10", err4);
        }

        // =====================================================================
        // 4. Stat Recalculation, Affix Integration & Upgrades
        // =====================================================================

        [Test]
        public void EquippingAndUnequipping_AppliesAndRemovesStatModifiersCleanly()
        {
            var knight = new Unit(Database.Characters["knight"]);
            int baseAtk = knight.Derived.physicalPower;
            int baseDef = knight.Derived.physicalDefense;

            // Unequip all items to establish clean baseline
            foreach (var slot in EquipmentSlots.All)
                knight.Unequip(slot);

            int unequippedAtk = knight.Derived.physicalPower;
            int unequippedDef = knight.Derived.physicalDefense;

            // Create weapon with +5 Base Weapon Power and a +3 STR affix
            var customSwordDef = new EquipmentDef
            {
                id = "custom_test_sword",
                name = "Custom Sword",
                slot = EquipmentSlot.MainHand,
                weaponType = WeaponType.Sword,
                weaponPower = 5
            };
            var weaponInst = new EquipmentInstance(customSwordDef, EquipmentRarity.Rare, 1);
            weaponInst.affixes.Add(new GeneratedAffix("affix_str", 3f)); // +3 STR

            Assert.IsTrue(knight.TryEquip(EquipmentSlot.MainHand, weaponInst, out string equipErr), equipErr);

            // +5 weapon power and +3 STR (giving at least +1 STR mod, scaling weapon power)
            Assert.Greater(knight.Derived.physicalPower, unequippedAtk);
            Assert.AreEqual(3, knight.abilities.equipmentBonuses[Ability.STR]);

            // Unequipping restores exactly to baseline without double-counting
            Assert.IsTrue(knight.Unequip(EquipmentSlot.MainHand));
            Assert.AreEqual(unequippedAtk, knight.Derived.physicalPower);
            Assert.AreEqual(0, knight.abilities.equipmentBonuses[Ability.STR]);
        }

        [Test]
        public void EquipmentUpgrades_IncreaseBaseStatsOnly_PreservingAffixesAndEffects()
        {
            var def = new EquipmentDef
            {
                id = "upgrade_test_blade",
                name = "Upgrade Blade",
                slot = EquipmentSlot.MainHand,
                weaponType = WeaponType.Sword,
                weaponPower = 20,
                maxUpgradeLevel = 5
            };
            var inst = new EquipmentInstance(def, EquipmentRarity.Epic, level: 3);
            inst.affixes.Add(new GeneratedAffix("affix_crit_chance", 7f)); // 7% crit chance
            inst.specialEffectId = "infernal_resonance";

            // +0 stats
            Assert.AreEqual(0, inst.upgradeLevel);
            Assert.AreEqual(20, inst.EffectiveWeaponPower);
            Assert.AreEqual(7f, inst.affixes[0].value);
            Assert.AreEqual("infernal_resonance", inst.specialEffectId);

            // +1 upgrade (+5% base stat) -> 20 * 1.05 = 21
            Assert.IsTrue(inst.TryUpgrade(out string err1), err1);
            Assert.AreEqual(1, inst.upgradeLevel);
            Assert.AreEqual(21, inst.EffectiveWeaponPower);
            Assert.AreEqual(7f, inst.affixes[0].value, "Affixes must not be altered by upgrades.");
            Assert.AreEqual("infernal_resonance", inst.specialEffectId);

            // Upgrade to +5 (max level) -> 20 * 1.25 = 25
            for (int i = 2; i <= 5; i++)
                Assert.IsTrue(inst.TryUpgrade(out _));

            Assert.AreEqual(5, inst.upgradeLevel);
            Assert.AreEqual(25, inst.EffectiveWeaponPower);

            // Exceeding max upgrade fails gracefully
            Assert.IsFalse(inst.TryUpgrade(out string errMax));
            StringAssert.Contains("maximum", errMax);
        }

        // =====================================================================
        // 5. Equipment Selling Economy Formula
        // =====================================================================

        [Test]
        public void EquipmentSelling_CalculatesValueAccurately()
        {
            var def = new EquipmentDef
            {
                id = "sell_sword",
                name = "Sell Sword",
                slot = EquipmentSlot.MainHand,
                weaponType = WeaponType.Sword,
                baseValue = 100
            };

            var commonInst = new EquipmentInstance(def, EquipmentRarity.Common, level: 1);
            Assert.AreEqual(100, commonInst.CalculateSellValue());

            var rareInst = new EquipmentInstance(def, EquipmentRarity.Rare, level: 1);
            // Rare multiplier is 2.2 -> 220
            Assert.AreEqual(220, rareInst.CalculateSellValue());

            // Upgrade increases value
            rareInst.upgradeLevel = 2;
            Assert.Greater(rareInst.CalculateSellValue(), 220);
        }

        // =====================================================================
        // 6. Persistence & Model C Run Defeat Retention
        // =====================================================================

        [Test]
        public void SaveLoadPersistence_PreservesInstances_Affixes_Upgrades_And_Inventory()
        {
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(MapGenerator.Generate(42), partyDefs, 42);

            // Add custom equipment to inventory
            var def = Database.Equipment["iron_broadsword"];
            var customItem = new EquipmentInstance(def, EquipmentRarity.Epic, level: 4)
            {
                upgradeLevel = 2
            };
            customItem.affixes.Add(new GeneratedAffix("affix_crit_chance", 9f));
            customItem.affixes.Add(new GeneratedAffix("affix_str", 4f));
            customItem.specialEffectId = "warlords_fury";
            run.Inventory.TryAddEquipment(customItem, out _);

            // Equip an item on hero 0
            var hero0 = run.Party[0];
            hero0.TryEquip(EquipmentSlot.MainHand, customItem, out _);

            // Save run state
            GameSave.SaveRunState(run);
            Assert.IsTrue(GameSave.HasSavedGame());

            // Restore from save
            Assert.IsTrue(GameSave.TryLoadContinue(out var restoredRoster, out var restoredRun));
            Assert.IsNotNull(restoredRun);

            // Verify equipped instance on hero 0
            var restoredHero = restoredRun.Party[0];
            Assert.IsTrue(restoredHero.equippedInstances.TryGetValue(EquipmentSlot.MainHand, out var restoredItem));
            Assert.IsNotNull(restoredItem);
            Assert.AreEqual(customItem.instanceId, restoredItem.instanceId);
            Assert.AreEqual(EquipmentRarity.Epic, restoredItem.rarity);
            Assert.AreEqual(2, restoredItem.upgradeLevel);
            Assert.AreEqual(2, restoredItem.affixes.Count);
            Assert.AreEqual("affix_crit_chance", restoredItem.affixes[0].affixId);
            Assert.AreEqual(9f, restoredItem.affixes[0].value);
            Assert.AreEqual("affix_str", restoredItem.affixes[1].affixId);
            Assert.AreEqual(4f, restoredItem.affixes[1].value);
            Assert.AreEqual("warlords_fury", restoredItem.specialEffectId);
        }

        [Test]
        public void RunDefeat_ResetsGold_But_PreservesPersistentEquipment_ModelC()
        {
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(MapGenerator.Generate(99), partyDefs, 99);
            run.Gold = 500;

            var swordDef = Database.Equipment["iron_broadsword"];
            var lootItem = new EquipmentInstance(swordDef, EquipmentRarity.Mythic, level: 7);
            run.Inventory.TryAddEquipment(lootItem, out _);

            Assert.AreEqual(500, run.Gold);
            Assert.AreEqual(1, run.Inventory.EquipmentCount);

            // Run Defeat occurs
            run.LoseCombat();
            Assert.AreEqual(RunStatus.Defeat, run.Status);
            Assert.AreEqual(0, run.Gold, "Run gold must be wiped on run failure.");

            // Equipment must remain persistent in the save
            var savedData = GameSave.LoadSaveData();
            Assert.IsNotNull(savedData);
            Assert.AreEqual(0, savedData.gold);
            Assert.AreEqual(1, savedData.inventoryEquipment.Length);
            Assert.AreEqual(lootItem.instanceId, savedData.inventoryEquipment[0].instanceId);
            Assert.AreEqual(EquipmentRarity.Mythic, (EquipmentRarity)savedData.inventoryEquipment[0].rarity);
        }

        // =====================================================================
        // 7. UI Screens Rendering & Interactions
        // =====================================================================

        [Test]
        public void InventoryAndEquipmentUiScreens_RenderAndInteractCleanly()
        {
            var root = new GameObject("TestUiRoot").transform;
            try
            {
                var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
                var run = new RunState(MapGenerator.Generate(101), partyDefs, 101);

                // 1. Inventory Screen
                var invScreen = new InventoryScreen(run.Inventory, run.Party);
                invScreen.Open(root);
                Assert.IsNotNull(invScreen);

                // 2. Equipment Screen
                var eqScreen = new EquipmentScreen(run.Inventory, run.Party);
                eqScreen.Open(root);
                Assert.IsNotNull(eqScreen);

                // Verify slot count equals 5
                Assert.AreEqual(5, EquipmentScreen.CoreSlots.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root.gameObject);
            }
        }
    }
}
