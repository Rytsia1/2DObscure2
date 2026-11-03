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
            var swordInstMage = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
            Assert.IsFalse(mage.TryEquip(EquipmentSlot.MainHand, swordInstMage, out string err2), "Mage cannot equip Broadsword.");
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

        // =====================================================================
        // 8. Data Model Validation: 9 Classes & 8 Slots
        // =====================================================================

        [Test]
        public void EquipmentDataModel_NineClassWeaponMatrix_IsEnforcedStrictly()
        {
            var session = new CharacterCreationSession();

            // 1. Martial Artist: Gauntlet in MainHand, FistWeapon in SecondHand
            session.SelectClass("martial_artist");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "iron_gauntlets", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "iron_knuckle", out _));
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "iron_broadsword", out string maErr));
            StringAssert.Contains("Incompatible Main Hand weapon", maErr);

            // 2. Rogue: Rapier in MainHand, Dagger/Buckler in SecondHand
            session.SelectClass("rogue");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "duelist_rapier", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "iron_dagger", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "iron_buckler", out _));
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "battle_greataxe", out _));

            // 3. Wizard: Wand in MainHand, Orb in SecondHand
            session.SelectClass("wizard");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "apprentice_wand", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "arcane_orb", out _));
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "sanctified_mace", out _));

            // 4. Cleric: Mace in MainHand, Holy Symbol / Shield in SecondHand
            session.SelectClass("cleric");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "sanctified_mace", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "silver_holy_symbol", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "iron_shield", out _));
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "hunting_bow", out _));

            // 5. Warlock: Ritual Blade in MainHand, Grimoire in SecondHand
            session.SelectClass("warlock");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "ritual_dagger", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "pact_grimoire", out _));
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "battle_greataxe", out _));

            // 6. Druid: Staff in MainHand, Totem in SecondHand
            session.SelectClass("druid");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "oak_staff", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "verdant_totem", out _));
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "heavy_crossbow", out _));
        }

        [Test]
        public void Barbarian_CanEquip_OneHandedAxeInSecondHand_ForDualWielding()
        {
            var session = new CharacterCreationSession();
            session.SelectClass("barbarian");

            // Barbarian main hand: Great Axe or 1H Axe
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "battle_greataxe", out _));

            // Barbarian second hand: One-Handed Axe (Dual-Wield exception)
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "steel_axe", out string errOffAxe), errOffAxe);

            // Fighter cannot equip Axe in SecondHand (Fighter only allows Shield / Buckler in Second Hand)
            session.SelectClass("fighter");
            Assert.IsFalse(session.TryEquip(EquipmentSlot.SecondHand, "steel_axe", out string errFighterAxe));
            StringAssert.Contains("cannot be equipped in Second Hand", errFighterAxe);
        }

        [Test]
        public void EquipmentRestriction_EnforcesAllowedCharacterIds()
        {
            var heroDef = Database.Characters["knight"];
            var hero = new Unit(heroDef); // id = "knight"

            var boundItemDef = new EquipmentDef
            {
                id = "hero_exclusive_ring",
                name = "Heirloom Ring",
                slot = EquipmentSlot.Accessory1,
                restrictions = new EquipmentRestriction
                {
                    allowedCharacterIds = new[] { "knight" }
                }
            };

            // Hero with matching id succeeds
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.Accessory1, boundItemDef, out string err1), err1);

            // Another hero with different id is rejected
            var otherDef = Database.Characters["mage"]; // id = "mage"
            var otherHero = new Unit(otherDef);
            Assert.IsFalse(otherHero.TryEquip(EquipmentSlot.Accessory1, boundItemDef, out string err2));
            StringAssert.Contains("restricted to specific characters", err2);
        }

        // =====================================================================
        // 9. Stage 4 — Equipment Core Logic Unit Tests
        // =====================================================================

        [Test]
        public void EquipmentCoreLogic_Equip_Unequip_And_SlotReplacement()
        {
            var hero = new Unit(Database.Characters["knight"]);
            hero.Unequip(EquipmentSlot.MainHand);
            int baseAtk = hero.Derived.physicalPower;

            var swordDef1 = Database.Equipment["iron_broadsword"];
            var swordDef2 = Database.Equipment["sanctified_mace"];

            var item1 = new EquipmentInstance(swordDef1, EquipmentRarity.Common, 1);
            var item2 = new EquipmentInstance(swordDef2, EquipmentRarity.Rare, 1);

            // 1. Equip item1
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.MainHand, item1, out string err1), err1);
            Assert.IsTrue(item1.isEquipped);
            Assert.AreEqual(hero.id, item1.boundCharacterId);
            Assert.AreEqual(item1, hero.equippedInstances[EquipmentSlot.MainHand]);
            Assert.Greater(hero.Derived.physicalPower, baseAtk);

            // 2. Replacing slot with item2 unbinds item1
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.MainHand, item2, out string err2), err2);
            Assert.IsFalse(item1.isEquipped);
            Assert.IsNull(item1.boundCharacterId);
            Assert.IsTrue(item2.isEquipped);
            Assert.AreEqual(hero.id, item2.boundCharacterId);
            Assert.AreEqual(item2, hero.equippedInstances[EquipmentSlot.MainHand]);

            // 3. Unequip unbinds item2 and restores base stats
            Assert.IsTrue(hero.Unequip(EquipmentSlot.MainHand));
            Assert.IsFalse(item2.isEquipped);
            Assert.IsNull(item2.boundCharacterId);
            Assert.IsFalse(hero.equippedInstances.ContainsKey(EquipmentSlot.MainHand));
            Assert.IsFalse(hero.equipped.ContainsKey(EquipmentSlot.MainHand));
            Assert.AreEqual(baseAtk, hero.Derived.physicalPower);
        }

        [Test]
        public void EquipmentCoreLogic_ExclusiveOwnership_PreventsMultipleCharactersEquippingSameInstance()
        {
            var heroA = new Unit(Database.Characters["knight"]);
            var charB = new CharacterDef { id = "knight_clone", name = "Knight Clone", classId = "fighter", raceId = "human" };
            var heroB = new Unit(charB);

            var swordDef = Database.Equipment["iron_broadsword"];
            var uniqueSword = new EquipmentInstance(swordDef, EquipmentRarity.Epic, 1);

            // Hero A equips unique sword
            Assert.IsTrue(heroA.TryEquip(EquipmentSlot.MainHand, uniqueSword, out string errA), errA);
            Assert.IsTrue(uniqueSword.isEquipped);
            Assert.AreEqual(heroA.id, uniqueSword.boundCharacterId);

            // Hero B attempts to equip the exact same instance -> must be rejected
            Assert.IsFalse(heroB.TryEquip(EquipmentSlot.MainHand, uniqueSword, out string errB));
            StringAssert.Contains("already equipped by another character", errB);

            // Hero A unequips it
            Assert.IsTrue(heroA.Unequip(EquipmentSlot.MainHand));
            Assert.IsFalse(uniqueSword.isEquipped);
            Assert.IsNull(uniqueSword.boundCharacterId);

            // Now Hero B can equip it
            Assert.IsTrue(heroB.TryEquip(EquipmentSlot.MainHand, uniqueSword, out string errBSuccess), errBSuccess);
            Assert.IsTrue(uniqueSword.isEquipped);
            Assert.AreEqual(heroB.id, uniqueSword.boundCharacterId);
        }

        [Test]
        public void EquipmentCoreLogic_SameCharacter_CannotEquipSameInstanceInMultipleSlots()
        {
            var hero = new Unit(Database.Characters["knight"]);

            var ringDef = new EquipmentDef
            {
                id = "silver_ring",
                name = "Silver Ring",
                slot = EquipmentSlot.Accessory1,
                equipmentType = EquipmentType.Accessory
            };
            var ringInstance = new EquipmentInstance(ringDef, EquipmentRarity.Common, 1);

            // Equip to Accessory 1
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.Accessory1, ringInstance, out string err1), err1);

            // Attempt to equip same instance to Accessory 2 -> must be rejected
            Assert.IsFalse(hero.TryEquip(EquipmentSlot.Accessory2, ringInstance, out string err2));
            StringAssert.Contains("already equipped in another slot", err2);
        }

        [Test]
        public void EquipmentCoreLogic_UnequipToInventory_Enforces100SlotLimit()
        {
            var hero = new Unit(Database.Characters["knight"]);
            var inv = new InventoryRuntime();

            var swordDef = Database.Equipment["iron_broadsword"];
            var equippedSword = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.MainHand, equippedSword, out _));

            // Fill inventory up to 100 capacity
            for (int i = 0; i < InventoryRuntime.MaxEquipmentCapacity; i++)
            {
                var dummy = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
                Assert.IsTrue(inv.TryAddEquipment(dummy, out _));
            }
            Assert.IsTrue(inv.IsEquipmentFull);

            // Unequipping to full inventory must fail
            Assert.IsFalse(EquipmentManager.UnequipToInventory(hero, EquipmentSlot.MainHand, inv, out string errFull));
            StringAssert.Contains("Inventory is full", errFull);

            // Item remains equipped
            Assert.IsTrue(hero.equippedInstances.ContainsKey(EquipmentSlot.MainHand));
            Assert.IsTrue(equippedSword.isEquipped);

            // Free 1 inventory slot
            var firstId = inv.equipment.First(e => !e.isEquipped).instanceId;
            inv.RemoveEquipment(firstId);
            Assert.IsFalse(inv.IsEquipmentFull);

            // Now UnequipToInventory succeeds
            Assert.IsTrue(EquipmentManager.UnequipToInventory(hero, EquipmentSlot.MainHand, inv, out string errSuccess), errSuccess);
            Assert.IsFalse(hero.equippedInstances.ContainsKey(EquipmentSlot.MainHand));
            Assert.IsFalse(equippedSword.isEquipped);
            Assert.IsTrue(inv.equipment.Contains(equippedSword));
        }

        [Test]
        public void EquipmentCoreLogic_SwapEquipmentBetweenUnits_PerformsAtomicExchange()
        {
            var charFighter = new CharacterDef { id = "hero_fighter", name = "Fighter Hero", classId = "fighter", raceId = "human" };
            var fighter = new Unit(charFighter);
            var charBarbarian = new CharacterDef { id = "hero_barbarian", name = "Barbarian Hero", classId = "barbarian", raceId = "human" };
            var barbarian = new Unit(charBarbarian);

            var sword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Common, 1);
            var axe = new EquipmentInstance(Database.Equipment["battle_greataxe"], EquipmentRarity.Common, 1);

            fighter.TryEquip(EquipmentSlot.MainHand, sword, out _);
            barbarian.TryEquip(EquipmentSlot.MainHand, axe, out _);

            // Incompatible swap fails atomically
            Assert.IsFalse(EquipmentManager.SwapEquipmentBetweenUnits(fighter, EquipmentSlot.MainHand, barbarian, EquipmentSlot.MainHand, out string swapErr));
            StringAssert.Contains("Cannot equip", swapErr);

            // Verify neither character had their gear swapped
            Assert.AreEqual(sword, fighter.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(axe, barbarian.equippedInstances[EquipmentSlot.MainHand]);

            // Compatible swap between two Fighters
            var charFighter2 = new CharacterDef { id = "hero_fighter2", name = "Fighter 2", classId = "fighter", raceId = "human" };
            var fighter2 = new Unit(charFighter2);
            var mace = new EquipmentInstance(Database.Equipment["sanctified_mace"], EquipmentRarity.Rare, 1);
            fighter2.TryEquip(EquipmentSlot.MainHand, mace, out _);

            Assert.IsTrue(EquipmentManager.SwapEquipmentBetweenUnits(fighter, EquipmentSlot.MainHand, fighter2, EquipmentSlot.MainHand, out string okErr), okErr);
            Assert.AreEqual(mace, fighter.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(sword, fighter2.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(fighter.id, mace.boundCharacterId);
            Assert.AreEqual(fighter2.id, sword.boundCharacterId);
        }

        [Test]
        public void EquipmentCoreLogic_AutoEquipAccessory_FillsEmptySlotFirst()
        {
            var hero = new Unit(Database.Characters["knight"]);
            hero.Unequip(EquipmentSlot.Accessory1);
            hero.Unequip(EquipmentSlot.Accessory2);

            var inv = new InventoryRuntime();

            var ringDef = new EquipmentDef
            {
                id = "gold_ring",
                name = "Gold Ring",
                slot = EquipmentSlot.Accessory1,
                equipmentType = EquipmentType.Accessory
            };
            var acc1 = new EquipmentInstance(ringDef, EquipmentRarity.Common, 1);
            var acc2 = new EquipmentInstance(ringDef, EquipmentRarity.Uncommon, 1);
            var acc3 = new EquipmentInstance(ringDef, EquipmentRarity.Rare, 1);

            // 1. First accessory goes to Accessory 1
            Assert.IsTrue(EquipmentManager.AutoEquipAccessory(hero, acc1, inv, out string err1), err1);
            Assert.AreEqual(acc1, hero.equippedInstances[EquipmentSlot.Accessory1]);
            Assert.IsFalse(hero.equippedInstances.ContainsKey(EquipmentSlot.Accessory2));

            // 2. Second accessory goes to empty Accessory 2
            Assert.IsTrue(EquipmentManager.AutoEquipAccessory(hero, acc2, inv, out string err2), err2);
            Assert.AreEqual(acc1, hero.equippedInstances[EquipmentSlot.Accessory1]);
            Assert.AreEqual(acc2, hero.equippedInstances[EquipmentSlot.Accessory2]);

            // 3. Third accessory replaces Accessory 1 by default
            Assert.IsTrue(EquipmentManager.AutoEquipAccessory(hero, acc3, inv, out string err3), err3);
            Assert.AreEqual(acc3, hero.equippedInstances[EquipmentSlot.Accessory1]);
            Assert.AreEqual(acc2, hero.equippedInstances[EquipmentSlot.Accessory2]);
        }

        [Test]
        public void EquipmentCoreLogic_StatDeltaCalculations()
        {
            var hero = new Unit(Database.Characters["knight"]);
            hero.Unequip(EquipmentSlot.MainHand);

            var swordDef = new EquipmentDef
            {
                id = "high_power_sword",
                name = "High Power Sword",
                slot = EquipmentSlot.MainHand,
                weaponType = WeaponType.Sword,
                weaponPower = 30,
                abilityBonuses = AbilityScores.Zero.Add(Ability.STR, 5)
            };
            var swordInst = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);

            int deltaPower = EquipmentManager.CalculateStatDelta(hero, EquipmentSlot.MainHand, swordInst, DerivedStatId.PhysicalPower);
            Assert.Greater(deltaPower, 0);

            var allDeltas = EquipmentManager.CalculateAllStatDeltas(hero, EquipmentSlot.MainHand, swordInst);
            Assert.AreEqual(deltaPower, allDeltas[DerivedStatId.PhysicalPower]);

            int deltaStr = EquipmentManager.CalculateAbilityScoreDelta(hero, EquipmentSlot.MainHand, swordInst, Ability.STR);
            Assert.AreEqual(5, deltaStr);
        }
    }
}
