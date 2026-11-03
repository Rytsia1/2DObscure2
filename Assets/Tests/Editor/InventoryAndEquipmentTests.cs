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

        // =====================================================================
        // 10. Stage 5 — Inventory Integration Unit Tests
        // =====================================================================

        [Test]
        public void InventoryIntegration_Storage_Addition_And_Removal()
        {
            var inv = new InventoryRuntime();
            var swordDef = Database.Equipment["iron_broadsword"];
            var sword = new EquipmentInstance(swordDef, EquipmentRarity.Rare, 1);

            // Add equipment
            Assert.IsTrue(inv.TryAddEquipment(sword, out string errAdd), errAdd);
            Assert.AreEqual(1, inv.EquipmentCount);
            Assert.IsTrue(inv.ContainsInstance(sword.instanceId));
            Assert.AreEqual(sword, inv.FindEquipment(sword.instanceId));
            Assert.AreEqual(1, inv.GetUnequippedEquipment().Count());

            // Sell equipment
            Assert.IsTrue(inv.TrySellEquipment(sword.instanceId, out int goldValue, out string errSell), errSell);
            Assert.Greater(goldValue, 0);
            Assert.AreEqual(0, inv.EquipmentCount);
            Assert.IsFalse(inv.ContainsInstance(sword.instanceId));

            // Discard another item
            var axe = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            Assert.IsTrue(inv.TryAddEquipment(axe, out _));
            Assert.IsTrue(inv.TryDiscardEquipment(axe.instanceId, out string errDiscard), errDiscard);
            Assert.AreEqual(0, inv.EquipmentCount);
        }

        [Test]
        public void InventoryIntegration_PreventsDiscardingOrSelling_EquippedItems()
        {
            var inv = new InventoryRuntime();
            var charA = new CharacterDef { id = "hero_knight", name = "Knight Hero", classId = "fighter", raceId = "human" };
            var hero = new Unit(charA);

            var sword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Epic, 1);
            inv.TryAddEquipment(sword, out _);
            Assert.IsTrue(EquipmentManager.EquipFromInventory(hero, EquipmentSlot.MainHand, sword, inv, out _));
            Assert.IsTrue(sword.isEquipped);

            // Attempting to discard or sell equipped item fails
            Assert.IsFalse(inv.TryDiscardEquipment(sword.instanceId, out string errDiscard));
            StringAssert.Contains("currently equipped", errDiscard);

            Assert.IsFalse(inv.TrySellEquipment(sword.instanceId, out _, out string errSell));
            StringAssert.Contains("currently equipped", errSell);

            // Item remains safely equipped and in inventory
            Assert.IsTrue(sword.isEquipped);
            Assert.IsTrue(inv.ContainsInstance(sword.instanceId));

            // After unequip, discard succeeds
            Assert.IsTrue(EquipmentManager.UnequipToInventory(hero, EquipmentSlot.MainHand, inv, out _));
            Assert.IsFalse(sword.isEquipped);
            Assert.IsTrue(inv.TryDiscardEquipment(sword.instanceId, out _));
            Assert.IsFalse(inv.ContainsInstance(sword.instanceId));
        }

        [Test]
        public void InventoryIntegration_CapacityEnforcement_100SlotsExclusiveToEquipment()
        {
            var inv = new InventoryRuntime();

            // Consumables and materials do not consume 100 equipment capacity
            inv.AddConsumable("potion", 99);
            inv.AddConsumable("ether", 50);
            inv.AddMaterial("iron_ingot", 100);
            inv.AddKeyItem("dungeon_pass");
            Assert.AreEqual(0, inv.EquipmentCount);
            Assert.IsFalse(inv.IsEquipmentFull);

            // Fill 100 equipment slots
            var swordDef = Database.Equipment["iron_broadsword"];
            for (int i = 0; i < InventoryRuntime.MaxEquipmentCapacity; i++)
            {
                var item = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
                Assert.IsTrue(inv.TryAddEquipment(item, out _));
            }
            Assert.AreEqual(100, inv.EquipmentCount);
            Assert.IsTrue(inv.IsEquipmentFull);

            // 101st equipment addition rejected
            var overflowItem = new EquipmentInstance(swordDef, EquipmentRarity.Legendary, 1);
            Assert.IsFalse(inv.TryAddEquipment(overflowItem, out string errFull));
            StringAssert.Contains("Inventory is full", errFull);

            // Equipping an item from inventory frees up an unequipped inventory slot
            var charA = new CharacterDef { id = "hero_fighter", name = "Fighter Hero", classId = "fighter", raceId = "human" };
            var hero = new Unit(charA);
            var itemToEquip = inv.equipment[0];
            Assert.IsTrue(EquipmentManager.EquipFromInventory(hero, EquipmentSlot.MainHand, itemToEquip, inv, out _));
            Assert.AreEqual(99, inv.EquipmentCount, "Equipped item does not consume inventory capacity.");
            Assert.IsFalse(inv.IsEquipmentFull);

            // Now overflow item can be added
            Assert.IsTrue(inv.TryAddEquipment(overflowItem, out _));
            Assert.AreEqual(100, inv.EquipmentCount);
        }

        [Test]
        public void InventoryIntegration_AutoEquipWeapon_SelectsMainHandFirst_ThenSecondHand()
        {
            var charBarb = new CharacterDef { id = "hero_barb", name = "Barbarian Hero", classId = "barbarian", raceId = "human" };
            var barb = new Unit(charBarb);
            barb.Unequip(EquipmentSlot.MainHand);
            barb.Unequip(EquipmentSlot.SecondHand);

            var inv = new InventoryRuntime();
            var axe1 = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            var axe2 = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Uncommon, 1);

            // 1. First 1H Axe goes to empty Main Hand
            Assert.IsTrue(EquipmentManager.AutoEquipWeapon(barb, axe1, inv, out string err1), err1);
            Assert.AreEqual(axe1, barb.equippedInstances[EquipmentSlot.MainHand]);
            Assert.IsFalse(barb.equippedInstances.ContainsKey(EquipmentSlot.SecondHand));

            // 2. Second 1H Axe goes to empty Second Hand (Dual-wielding Barbarian)
            Assert.IsTrue(EquipmentManager.AutoEquipWeapon(barb, axe2, inv, out string err2), err2);
            Assert.AreEqual(axe1, barb.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(axe2, barb.equippedInstances[EquipmentSlot.SecondHand]);

            // 3. Great Axe (2H/Main Hand only) replaces Main Hand
            var greataxe = new EquipmentInstance(Database.Equipment["battle_greataxe"], EquipmentRarity.Rare, 1);
            Assert.IsTrue(EquipmentManager.AutoEquipWeapon(barb, greataxe, inv, out string err3), err3);
            Assert.AreEqual(greataxe, barb.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(axe2, barb.equippedInstances[EquipmentSlot.SecondHand]);
        }

        [Test]
        public void InventoryIntegration_QuickEquip_RoutesCorrectlyByItemCategory()
        {
            var charFighter = new CharacterDef { id = "hero_fighter", name = "Fighter Hero", classId = "fighter", raceId = "human" };
            var hero = new Unit(charFighter);
            hero.Unequip(EquipmentSlot.MainHand);
            hero.Unequip(EquipmentSlot.BodyArmor);
            hero.Unequip(EquipmentSlot.Accessory1);
            hero.Unequip(EquipmentSlot.Accessory2);

            var inv = new InventoryRuntime();

            // 1. Armor routes to BodyArmor slot
            var armorDef = new EquipmentDef
            {
                id = "plate_chest",
                name = "Plate Chest",
                slot = EquipmentSlot.BodyArmor,
                equipmentType = EquipmentType.Body,
                physicalDefense = 10
            };
            var armorInst = new EquipmentInstance(armorDef, EquipmentRarity.Common, 1);
            Assert.IsTrue(EquipmentManager.QuickEquip(hero, armorInst, inv, out string errArmor), errArmor);
            Assert.AreEqual(armorInst, hero.equippedInstances[EquipmentSlot.BodyArmor]);

            // 2. Weapon routes to MainHand
            var swordInst = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Common, 1);
            Assert.IsTrue(EquipmentManager.QuickEquip(hero, swordInst, inv, out string errWeapon), errWeapon);
            Assert.AreEqual(swordInst, hero.equippedInstances[EquipmentSlot.MainHand]);

            // 3. Accessory routes to Accessory 1 then Accessory 2
            var ringDef = new EquipmentDef
            {
                id = "ruby_ring",
                name = "Ruby Ring",
                slot = EquipmentSlot.Accessory1,
                equipmentType = EquipmentType.Accessory
            };
            var ring1 = new EquipmentInstance(ringDef, EquipmentRarity.Common, 1);
            var ring2 = new EquipmentInstance(ringDef, EquipmentRarity.Uncommon, 1);

            Assert.IsTrue(EquipmentManager.QuickEquip(hero, ring1, inv, out _));
            Assert.AreEqual(ring1, hero.equippedInstances[EquipmentSlot.Accessory1]);

            Assert.IsTrue(EquipmentManager.QuickEquip(hero, ring2, inv, out _));
            Assert.AreEqual(ring2, hero.equippedInstances[EquipmentSlot.Accessory2]);
        }

        [Test]
        public void InventoryIntegration_TransferEquipment_BetweenPartyMembers()
        {
            var charA = new CharacterDef { id = "hero_a", name = "Hero A", classId = "fighter", raceId = "human" };
            var charB = new CharacterDef { id = "hero_b", name = "Hero B", classId = "fighter", raceId = "human" };
            var heroA = new Unit(charA);
            var heroB = new Unit(charB);
            heroA.Unequip(EquipmentSlot.MainHand);
            heroB.Unequip(EquipmentSlot.MainHand);

            var inv = new InventoryRuntime();
            var sword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Common, 1);
            var mace = new EquipmentInstance(Database.Equipment["sanctified_mace"], EquipmentRarity.Rare, 1);

            heroA.TryEquip(EquipmentSlot.MainHand, sword, out _);

            // 1. One-way transfer: Hero A has sword, Hero B has empty Main Hand
            Assert.IsTrue(EquipmentManager.TransferEquipment(heroA, EquipmentSlot.MainHand, heroB, EquipmentSlot.MainHand, inv, out string errTransfer), errTransfer);
            Assert.IsFalse(heroA.equippedInstances.ContainsKey(EquipmentSlot.MainHand));
            Assert.AreEqual(sword, heroB.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(heroB.id, sword.boundCharacterId);

            // 2. Two-way swap: Hero A now equips mace, then transfer between A (mace) and B (sword)
            heroA.TryEquip(EquipmentSlot.MainHand, mace, out _);
            Assert.IsTrue(EquipmentManager.TransferEquipment(heroA, EquipmentSlot.MainHand, heroB, EquipmentSlot.MainHand, inv, out string errSwap), errSwap);
            Assert.AreEqual(sword, heroA.equippedInstances[EquipmentSlot.MainHand]);
            Assert.AreEqual(mace, heroB.equippedInstances[EquipmentSlot.MainHand]);

            // 3. Incompatible transfer rollback
            var charWizard = new CharacterDef { id = "hero_wiz", name = "Wizard Hero", classId = "wizard", raceId = "human" };
            var wizard = new Unit(charWizard);
            wizard.Unequip(EquipmentSlot.MainHand);

            Assert.IsFalse(EquipmentManager.TransferEquipment(heroA, EquipmentSlot.MainHand, wizard, EquipmentSlot.MainHand, inv, out string errFail));
            StringAssert.Contains("Cannot transfer", errFail);
            Assert.AreEqual(sword, heroA.equippedInstances[EquipmentSlot.MainHand], "Hero A must retain sword after failed transfer.");
            Assert.IsFalse(wizard.equippedInstances.ContainsKey(EquipmentSlot.MainHand));
        }

        // =====================================================================
        // 11. Combat Integration Tests (Tahap 6)
        // =====================================================================

        [Test]
        public void CombatIntegration_DualWielding_OffHandWeaponPowerPenalty_50Percent()
        {
            var charBarb = new CharacterDef { id = "hero_barb", name = "Barb Hero", classId = "barbarian", raceId = "human" };
            var barb = new Unit(charBarb);
            barb.Unequip(EquipmentSlot.MainHand);
            barb.Unequip(EquipmentSlot.SecondHand);

            int unarmedPower = barb.Derived.physicalPower;

            var axeMain = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            var axeOff = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);

            // 1. Equip Main Hand Steel Axe (weaponPower = 4)
            Assert.IsTrue(barb.TryEquip(EquipmentSlot.MainHand, axeMain, out string errMain), errMain);
            int mainOnlyPower = barb.Derived.physicalPower;
            Assert.AreEqual(unarmedPower + 4, mainOnlyPower, "Main hand weapon contributes 100% of weaponPower (+4).");

            // 2. Equip Second Hand Steel Axe (weaponPower = 4, off-hand penalty 50% = +2)
            Assert.IsTrue(barb.TryEquip(EquipmentSlot.SecondHand, axeOff, out string errOff), errOff);
            int dualWieldPower = barb.Derived.physicalPower;
            Assert.AreEqual(mainOnlyPower + 2, dualWieldPower, "Second hand weapon contributes exactly 50% of weaponPower (4 * 0.5 = +2).");
            Assert.AreEqual(unarmedPower + 6, dualWieldPower, "Dual wielding 2 steel axes (4 + 4) yields +6 total physical power.");

            // 3. Stat Delta calculation reflects the 50% off-hand penalty
            int deltaOff = EquipmentManager.CalculateStatDelta(barb, EquipmentSlot.SecondHand, axeOff, DerivedStatId.PhysicalPower);
            Assert.AreEqual(0, deltaOff, "Delta when replacing equipped off-hand with identical axe is 0.");

            barb.Unequip(EquipmentSlot.SecondHand);
            int deltaOffEmpty = EquipmentManager.CalculateStatDelta(barb, EquipmentSlot.SecondHand, axeOff, DerivedStatId.PhysicalPower);
            Assert.AreEqual(2, deltaOffEmpty, "Candidate off-hand axe delta must be +2 (50% of 4).");

            // 4. Combatant attack reflects dual wielding derived stat
            barb.TryEquip(EquipmentSlot.SecondHand, axeOff, out _);
            var combatant = new Combatant(barb, Side.Party, 0, "Barbarian", "BRB");
            Assert.AreEqual(dualWieldPower, combatant.Atk);
        }

        [Test]
        public void CombatIntegration_OffHandBreakPower_Contributes100Percent()
        {
            var charBarb = new CharacterDef { id = "hero_barb", name = "Barb Hero", classId = "barbarian", raceId = "human", baseScores = AbilityScores.Uniform(10) };
            var barb = new Unit(charBarb);
            barb.Unequip(EquipmentSlot.MainHand);
            barb.Unequip(EquipmentSlot.SecondHand);

            int baseBreak = barb.Derived.physicalBreakPower;

            // Greataxe (+3 break power)
            var greataxe = new EquipmentInstance(Database.Equipment["battle_greataxe"], EquipmentRarity.Common, 1);
            Assert.IsTrue(barb.TryEquip(EquipmentSlot.MainHand, greataxe, out string errMain), errMain);
            Assert.AreEqual(baseBreak + 3, barb.Derived.physicalBreakPower);

            // Off-hand steel axe (+2 break power)
            var offAxe = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            Assert.IsTrue(barb.TryEquip(EquipmentSlot.SecondHand, offAxe, out string errOff), errOff);

            // Off-hand break power is 100% full value (not penalized): 3 + 2 = 5
            Assert.AreEqual(baseBreak + 5, barb.Derived.physicalBreakPower, "Break power from off-hand must contribute 100% full value.");

            var combatant = new Combatant(barb, Side.Party, 0, "Barbarian", "BRB");
            Assert.AreEqual(barb.Derived.physicalBreakPower, combatant.PhysicalBreak);
        }

        [Test]
        public void CombatIntegration_EquipmentDefense_MitigatesPhysicalDamagePointForPoint()
        {
            var targetChar = new CharacterDef { id = "target_hero", name = "Target Hero", classId = "fighter", raceId = "human" };
            var targetUnit = new Unit(targetChar);
            targetUnit.Unequip(EquipmentSlot.BodyArmor);
            targetUnit.Unequip(EquipmentSlot.SecondHand);

            var targetCombatant = new Combatant(targetUnit, Side.Party, 0, "Target", "TGT");
            float unarmoredDef = targetCombatant.Def;

            var attackerChar = new CharacterDef { id = "attacker_hero", name = "Attacker Hero", classId = "fighter", raceId = "human" };
            var attackerUnit = new Unit(attackerChar);
            var attackerCombatant = new Combatant(attackerUnit, Side.Party, 1, "Attacker", "ATK");

            var slashComponent = new[] { DamageComponent.Physical(skillPower: 50f, scaling: 0f) };

            // Attack unarmored target
            var ctxUnarmored = attackerCombatant.CreateDamageContext(targetCombatant, slashComponent, hitCount: 1, qteResult: QteResult.Good);
            ctxUnarmored.forcedCriticalHit = false;
            var resUnarmored = DamageCalculator.Calculate(ctxUnarmored);

            // Equip Plate Armor with +15 physical defense
            var plateArmor = new EquipmentDef
            {
                id = "heavy_plate",
                name = "Heavy Plate",
                slot = EquipmentSlot.BodyArmor,
                equipmentType = EquipmentType.Body,
                physicalDefense = 15
            };
            targetUnit.TryEquip(EquipmentSlot.BodyArmor, new EquipmentInstance(plateArmor, EquipmentRarity.Rare, 1), out _);

            Assert.AreEqual(unarmoredDef + 15, targetCombatant.Def, "Target physical defense must increase by 15.");

            // Attack armored target
            var ctxArmored = attackerCombatant.CreateDamageContext(targetCombatant, slashComponent, hitCount: 1, qteResult: QteResult.Good);
            ctxArmored.forcedCriticalHit = false;
            var resArmored = DamageCalculator.Calculate(ctxArmored);

            // Physical defense subtracts flat from damage point-for-point
            Assert.AreEqual(resUnarmored.totalDamage - 15, resArmored.totalDamage, "Heavy Plate must mitigate exactly 15 damage point-for-point.");
        }

        [Test]
        public void CombatIntegration_EquipmentCriticalHitAndModifiers_ScaleCombatDamage()
        {
            var charRogue = new CharacterDef { id = "hero_rogue", name = "Rogue Hero", classId = "rogue", raceId = "human" };
            var rogue = new Unit(charRogue);
            rogue.Unequip(EquipmentSlot.Accessory1);

            int baseCritChance = rogue.Derived.criticalChance;
            int baseCritDamage = rogue.Derived.criticalDamage;

            // Custom accessory with +20% Crit Chance and +50% Crit Damage (200% total)
            var deadlyRing = new EquipmentDef
            {
                id = "deadly_ring",
                name = "Ring of the Assassin",
                slot = EquipmentSlot.Accessory1,
                equipmentType = EquipmentType.Accessory,
                modifiers = new[]
                {
                    StatModifier.ForDerived(DerivedStatId.CriticalChance, 20, ModifierKind.Flat, ModifierSource.Equipment, "Deadly Ring"),
                    StatModifier.ForDerived(DerivedStatId.CriticalDamage, 50, ModifierKind.Flat, ModifierSource.Equipment, "Deadly Ring")
                }
            };

            rogue.TryEquip(EquipmentSlot.Accessory1, new EquipmentInstance(deadlyRing, EquipmentRarity.Epic, 1), out _);

            Assert.AreEqual(baseCritChance + 20, rogue.Derived.criticalChance);
            Assert.AreEqual(baseCritDamage + 50, rogue.Derived.criticalDamage);

            var rogueCombatant = new Combatant(rogue, Side.Party, 0, "Rogue", "ROG");
            var enemy = new Combatant(new Unit(Database.Enemies["skeleton"]), Side.Enemy, 0, "Skeleton", "SK", Database.Enemies["skeleton"]);

            var ctx = rogueCombatant.CreateDamageContext(
                enemy,
                new[] { DamageComponent.Physical(skillPower: 100f, scaling: 0f) },
                hitCount: 1,
                qteResult: QteResult.Good);
            ctx.forcedCriticalHit = true;

            // 150% base -> 200% with +50% Crit Damage
            Assert.AreEqual(2.0f, ctx.criticalHitConfig.FinalCriticalDamage, 0.001f);

            var res = DamageCalculator.Calculate(ctx);
            // Base 100 * QTE Good (1.0) * Crit (2.0) = 200 - Skeleton Def (3) = 197
            Assert.AreEqual(197, res.totalDamage);
            Assert.IsTrue(res.FirstHit.anyComponentCriticalHit);
        }

        [Test]
        public void CombatIntegration_EquipmentAbilityScoreBonuses_ScaleAttackAndSpellPower()
        {
            var charHero = new CharacterDef { id = "hero_paladin", name = "Paladin", classId = "fighter", raceId = "human" };
            var hero = new Unit(charHero);
            hero.Unequip(EquipmentSlot.Accessory1);
            hero.Unequip(EquipmentSlot.Accessory2);

            int baseStr = hero.FinalScores.str;
            int baseInt = hero.FinalScores.intel;
            int baseAtk = hero.Derived.physicalPower;
            int baseMag = hero.Derived.magicPower;

            var strBracers = new EquipmentDef
            {
                id = "bracers_might",
                name = "Bracers of Might",
                slot = EquipmentSlot.Accessory1,
                equipmentType = EquipmentType.Accessory,
                abilityBonuses = AbilityScores.Zero.Add(Ability.STR, 4)
            };

            var intAmulet = new EquipmentDef
            {
                id = "amulet_lore",
                name = "Amulet of Lore",
                slot = EquipmentSlot.Accessory2,
                equipmentType = EquipmentType.Accessory,
                abilityBonuses = AbilityScores.Zero.Add(Ability.INT, 4)
            };

            hero.TryEquip(EquipmentSlot.Accessory1, new EquipmentInstance(strBracers, EquipmentRarity.Rare, 1), out _);
            hero.TryEquip(EquipmentSlot.Accessory2, new EquipmentInstance(intAmulet, EquipmentRarity.Rare, 1), out _);

            Assert.AreEqual(baseStr + 4, hero.FinalScores.str);
            Assert.AreEqual(baseInt + 4, hero.FinalScores.intel);

            // Fighter has weaponScaling = 1.0 for STR and spellScaling = 1.0 for INT
            // Ability mod increases by +2 for +4 score (e.g. 10->14 is mod 0->+2)
            Assert.Greater(hero.Derived.physicalPower, baseAtk);
            Assert.Greater(hero.Derived.magicPower, baseMag);

            var combatant = new Combatant(hero, Side.Party, 0, "Fighter", "FTR");
            var enemy = new Combatant(new Unit(Database.Enemies["skeleton"]), Side.Enemy, 0, "Skeleton", "SK", Database.Enemies["skeleton"]);

            var ctx = combatant.CreateDamageContext(enemy, new[] { DamageComponent.Physical(skillPower: 10f, scaling: 1f) });
            Assert.AreEqual(hero.FinalScores.str, ctx.attackerStr);
            Assert.AreEqual(hero.FinalScores.intel, ctx.attackerInt);
        }

        [Test]
        public void CombatIntegration_AccuracyAndEvasion_EquipmentModifiersApplied()
        {
            var charFighter = new CharacterDef { id = "hero_fighter_acc", name = "Fighter Hero", classId = "fighter", raceId = "human" };
            var fighter = new Unit(charFighter);
            fighter.Unequip(EquipmentSlot.MainHand);

            var charEnemy = new CharacterDef { id = "hero_dodger", name = "Dodger Hero", classId = "rogue", raceId = "human" };
            var dodger = new Unit(charEnemy);
            dodger.Unequip(EquipmentSlot.FootArmor);

            int baseAcc = (int)AccuracySystem.CalculateAttackerAccuracy(fighter, AttackCategory.Melee);
            int baseEva = dodger.Derived.evasion;

            // Accurate Sword (+12 accuracy)
            var accurateSword = new EquipmentDef
            {
                id = "accurate_blade",
                name = "Precision Blade",
                slot = EquipmentSlot.MainHand,
                equipmentType = EquipmentType.Weapon,
                weaponType = WeaponType.Sword,
                accuracyModifier = 12
            };
            Assert.IsTrue(fighter.TryEquip(EquipmentSlot.MainHand, new EquipmentInstance(accurateSword, EquipmentRarity.Rare, 1), out string errAcc), errAcc);

            // Elven Boots (+15 Evasion)
            var elvenBoots = new EquipmentDef
            {
                id = "elven_boots",
                name = "Elven Boots",
                slot = EquipmentSlot.FootArmor,
                equipmentType = EquipmentType.Feet,
                modifiers = new[] { StatModifier.ForDerived(DerivedStatId.Evasion, 15, ModifierKind.Flat, ModifierSource.Equipment, "Elven Boots") }
            };
            Assert.IsTrue(dodger.TryEquip(EquipmentSlot.FootArmor, new EquipmentInstance(elvenBoots, EquipmentRarity.Rare, 1), out string errEva), errEva);

            float newAcc = AccuracySystem.CalculateAttackerAccuracy(fighter, AttackCategory.Melee);
            Assert.AreEqual(baseAcc + 12f, newAcc, "Attacker accuracy must include +12 weapon accuracy modifier.");

            int newEva = dodger.Derived.evasion;
            Assert.AreEqual(baseEva + 15, newEva, "Target evasion must include +15 boots evasion modifier.");

            float hitChance = AccuracySystem.CalculateHitChance(fighter, newEva, AttackCategory.Melee);
            // Hit chance formula = base (80) + acc (baseAcc + 12) - eva (baseEva + 15)
            float expectedHitChance = Math.Clamp(AccuracyConfig.Default.baseHitChance + newAcc - newEva, AccuracyConfig.Default.minHitChance, AccuracyConfig.Default.maxHitChance);
            Assert.AreEqual(expectedHitChance, hitChance, 0.001f);
        }

        // =====================================================================
        // 12. Roguelike Persistence & Progression Tests (Tahap 7)
        // =====================================================================

        [Test]
        public void RoguelikePersistence_EquipmentShop_BuyingDeductsGoldAndAddsToInventory()
        {
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(MapGenerator.Generate(101), partyDefs, 101);
            run.Gold = 500;

            var shop = EquipmentShopManager.GenerateShop(1, run.Rng, itemCount: 4);
            Assert.AreEqual(4, shop.items.Count);
            var targetItem = shop.items[0];
            int itemCost = targetItem.cost;
            Assert.Greater(itemCost, 0);

            // 1. Successful purchase
            Assert.IsTrue(EquipmentShopManager.TryBuyEquipment(run, shop, targetItem.shopItemId, out var boughtItem, out string errBuy), errBuy);
            Assert.IsNotNull(boughtItem);
            Assert.AreEqual(targetItem.item, boughtItem);
            Assert.IsTrue(targetItem.isSold);
            Assert.AreEqual(500 - itemCost, run.Gold);
            Assert.IsTrue(run.Inventory.equipment.Contains(boughtItem));

            // 2. Buying sold-out item fails
            Assert.IsFalse(EquipmentShopManager.TryBuyEquipment(run, shop, targetItem.shopItemId, out _, out string errSoldOut));
            StringAssert.Contains("sold out", errSoldOut);

            // 3. Buying with insufficient gold fails
            run.Gold = 1;
            var item2 = shop.items[1];
            Assert.IsFalse(EquipmentShopManager.TryBuyEquipment(run, shop, item2.shopItemId, out _, out string errNoGold));
            StringAssert.Contains("Not enough gold", errNoGold);

            // 4. Buying when inventory is full fails
            run.Gold = 1000;
            while (!run.Inventory.IsEquipmentFull)
            {
                run.Inventory.equipment.Add(new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1));
            }
            Assert.IsFalse(EquipmentShopManager.TryBuyEquipment(run, shop, item2.shopItemId, out _, out string errFull));
            StringAssert.Contains("Inventory is full", errFull);
        }

        [Test]
        public void RoguelikePersistence_EquipmentShop_SellingGrantsGoldAndRemovesFromInventory()
        {
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(MapGenerator.Generate(101), partyDefs, 101);
            run.Gold = 100;

            var extraAxe = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Rare, 3);
            run.Inventory.TryAddEquipment(extraAxe, out _);
            int sellValue = extraAxe.CalculateSellValue();
            Assert.Greater(sellValue, 0);

            // 1. Sell unequipped item
            Assert.IsTrue(EquipmentShopManager.TrySellEquipment(run, extraAxe.instanceId, out int goldGained, out string errSell), errSell);
            Assert.AreEqual(sellValue, goldGained);
            Assert.AreEqual(100 + sellValue, run.Gold);
            Assert.IsFalse(run.Inventory.equipment.Contains(extraAxe));

            // 2. Cannot sell currently equipped item
            var knight = run.Party[0];
            var sword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Common, 1);
            Assert.IsTrue(knight.TryEquip(EquipmentSlot.MainHand, sword, out string errEqSword), errEqSword);
            var equippedWeapon = knight.equippedInstances[EquipmentSlot.MainHand];
            run.Inventory.equipment.Add(equippedWeapon); // placed in bag reference while equipped
            Assert.IsFalse(EquipmentShopManager.TrySellEquipment(run, equippedWeapon.instanceId, out _, out string errEquipped));
            StringAssert.Contains("Unequip it first", errEquipped);
            Assert.AreEqual(100 + sellValue, run.Gold, "Gold must not change when selling equipped item fails.");
        }

        [Test]
        public void RoguelikePersistence_SaveAndRestore_FullEquipmentAndShopAcrossSessions()
        {
            GameSave.ClearSave();
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(MapGenerator.Generate(101), partyDefs, 101);

            var hero = run.Party[0];
            var ring = new EquipmentDef
            {
                id = "ring_power",
                name = "Ring of Ancient Power",
                slot = EquipmentSlot.Accessory1,
                equipmentType = EquipmentType.Accessory,
                abilityBonuses = AbilityScores.Zero.Add(Ability.STR, 3),
                physicalDefense = 5
            };
            Database.Equipment["ring_power"] = ring;
            var ringInst = new EquipmentInstance(ring, EquipmentRarity.Epic, 3);
            ringInst.TryUpgrade(out _); // +1 upgrade
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.Accessory1, ringInst, out string errRing), errRing);

            // Add unequipped loot in inventory
            var bagWeapon = new EquipmentInstance(Database.Equipment["battle_greataxe"], EquipmentRarity.Rare, 2);
            run.Inventory.TryAddEquipment(bagWeapon, out _);

            // Setup custom shop item
            run.CurrentShop = new ShopInventory(1);
            var shopInst = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Uncommon, 1);
            run.CurrentShop.items.Add(new ShopItem(shopInst, 75));

            int heroAtk = hero.Derived.physicalPower;
            int heroDef = hero.Derived.physicalDefense;

            // Save run state
            GameSave.SaveRunState(run);

            // Restore in fresh session
            Assert.IsTrue(GameSave.TryLoadContinue(out var roster, out var restoredRun));
            Assert.IsNotNull(restoredRun);
            Assert.AreEqual(run.Party.Count, restoredRun.Party.Count);

            var restoredHero = restoredRun.Party[0];
            Assert.IsTrue(restoredHero.equippedInstances.ContainsKey(EquipmentSlot.Accessory1));
            var restoredRing = restoredHero.equippedInstances[EquipmentSlot.Accessory1];
            Assert.AreEqual("Ring of Ancient Power", restoredRing.Definition.name);
            Assert.AreEqual(1, restoredRing.upgradeLevel);
            Assert.AreEqual(EquipmentRarity.Epic, restoredRing.rarity);
            Assert.AreEqual(heroAtk, restoredHero.Derived.physicalPower);
            Assert.AreEqual(heroDef, restoredHero.Derived.physicalDefense);

            // Verify inventory bag equipment
            Assert.AreEqual(1, restoredRun.Inventory.EquipmentCount);
            Assert.AreEqual(bagWeapon.instanceId, restoredRun.Inventory.equipment[0].instanceId);

            // Verify shop persistence
            Assert.IsNotNull(restoredRun.CurrentShop);
            Assert.AreEqual(1, restoredRun.CurrentShop.items.Count);
            Assert.AreEqual(75, restoredRun.CurrentShop.items[0].cost);
        }

        [Test]
        public void RoguelikePersistence_EightDungeonProgression_EquipmentPreservedBetweenDungeons()
        {
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(MapGenerator.Generate(101, 1), partyDefs, 101);
            Assert.AreEqual(1, run.Act);
            Assert.AreEqual("The Sunken Crypt", run.Map.name);

            // Equip Dungeon 1 loot on hero and store item in bag
            var hero = run.Party[0];
            var lootSword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Rare, 2);
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.MainHand, lootSword, out string errLootEq), errLootEq);

            var bagItem = new EquipmentInstance(Database.Equipment["iron_shield"], EquipmentRarity.Uncommon, 1);
            run.Inventory.TryAddEquipment(bagItem, out _);

            int d1Atk = hero.Derived.physicalPower;
            int d1BagCount = run.Inventory.EquipmentCount;

            // Advance through all 8 sequential dungeons
            for (int act = 2; act <= 8; act++)
            {
                var nextDungeon = MapGenerator.Generate(101 + act, act);
                Assert.IsTrue(run.AdvanceToNextDungeon(nextDungeon));
                Assert.AreEqual(act, run.Act);
                Assert.AreEqual(MapGenerator.DungeonNames[act - 1], run.Map.name);
                Assert.AreEqual(RunStatus.Active, run.Status);
                Assert.IsNull(run.Current);
                Assert.AreEqual(0, run.Cleared.Count);

                // Verify party equipped items and derived stats remain 100% preserved
                Assert.AreEqual(lootSword.instanceId, hero.equippedInstances[EquipmentSlot.MainHand].instanceId);
                Assert.AreEqual(d1Atk, hero.Derived.physicalPower);

                // Verify bag equipment preserved
                Assert.AreEqual(d1BagCount, run.Inventory.EquipmentCount);
                Assert.AreEqual(bagItem.instanceId, run.Inventory.equipment[0].instanceId);

                // Verify shop was regenerated for the new dungeon tier
                Assert.IsNotNull(run.CurrentShop);
                Assert.AreEqual(act, run.CurrentShop.dungeonTier);
            }
        }

        [Test]
        public void RoguelikePersistence_RunDefeat_NewGameWipesRunEquipmentClean()
        {
            var customHeroDef = new CharacterDef
            {
                id = "hero_fighter",
                name = "Fighter Hero",
                classId = "fighter",
                raceId = "human",
                backgroundId = "oathsworn",
                baseScores = AbilityScores.DefaultBase,
                stats = new Stats(100, 20, 10, 10, 5, 5),
                startingEquipment = new[] { "iron_broadsword", "iron_shield" }
            };
            var customParty = new List<CharacterDef> { customHeroDef, customHeroDef, customHeroDef, customHeroDef };
            var run = new RunState(MapGenerator.Generate(101), customParty, 101);

            // Hero starts with initial starting equipment
            var hero = run.Party[0];
            Assert.IsTrue(hero.equippedInstances.ContainsKey(EquipmentSlot.MainHand));
            var initialSword = hero.equippedInstances[EquipmentSlot.MainHand];
            Assert.AreEqual("iron_broadsword", initialSword.definitionId);

            // Hero equips high-tier run loot and bags multiple items
            var epicWeapon = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Epic, 5);
            Assert.IsTrue(hero.TryEquip(EquipmentSlot.MainHand, epicWeapon, out string errEpic), errEpic);

            run.Inventory.TryAddEquipment(new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Rare, 3), out _);
            run.Inventory.TryAddEquipment(new EquipmentInstance(Database.Equipment["iron_shield"], EquipmentRarity.Rare, 3), out _);
            run.Gold = 350;

            Assert.AreEqual(2, run.Inventory.EquipmentCount);
            Assert.AreEqual(epicWeapon.instanceId, hero.equippedInstances[EquipmentSlot.MainHand].instanceId);

            // Run Defeat occurs
            run.LoseCombat();
            Assert.AreEqual(RunStatus.Defeat, run.Status);
            Assert.AreEqual(0, run.Gold);

            // Starting New Game resets progression and wipes all run equipment
            run.ResetToNewGame(customParty, MapGenerator.Generate(202, 1));
            Assert.AreEqual(RunStatus.Active, run.Status);
            Assert.AreEqual(0, run.Gold);
            Assert.AreEqual(1, run.Act);

            // All acquired run equipment in inventory bag is wiped clean
            Assert.AreEqual(0, run.Inventory.EquipmentCount, "Inventory bag equipment must be wiped clean upon New Game reset.");

            // Hero equipped items return strictly to initial starting equipment
            var resetHero = run.Party[0];
            Assert.AreNotEqual(epicWeapon.instanceId, resetHero.equippedInstances[EquipmentSlot.MainHand].instanceId);
            Assert.AreEqual("iron_broadsword", resetHero.equippedInstances[EquipmentSlot.MainHand].definitionId);
            Assert.AreEqual(EquipmentRarity.Common, resetHero.equippedInstances[EquipmentSlot.MainHand].rarity);
        }

        // =====================================================================
        // 13. Comprehensive Testing & Validation Suite (Tahap 8)
        // =====================================================================

        [Test]
        public void Validation_EquipValid_AllSlotsSuccessfullyEquipAndBind()
        {
            var heroDef = new CharacterDef
            {
                id = "val_fighter", name = "Validation Fighter", classId = "fighter", raceId = "human",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(heroDef);

            var items = new (EquipmentSlot slot, EquipmentDef def)[]
            {
                (EquipmentSlot.MainHand, Database.Equipment["iron_broadsword"]),
                (EquipmentSlot.SecondHand, Database.Equipment["iron_shield"]),
                (EquipmentSlot.HeadArmor, Database.Equipment["iron_helm"]),
                (EquipmentSlot.BodyArmor, Database.Equipment["chain_hauberk"]),
                (EquipmentSlot.LegArmor, Database.Equipment["chain_greaves"]),
                (EquipmentSlot.FootArmor, Database.Equipment["iron_sabatons"]),
                (EquipmentSlot.Accessory1, Database.Equipment["vanguard_ring"]),
                (EquipmentSlot.Accessory2, Database.Equipment["soldier_medal"])
            };

            foreach (var (slot, def) in items)
            {
                var inst = new EquipmentInstance(def, EquipmentRarity.Common, 1);
                Assert.IsTrue(unit.TryEquip(slot, inst, out string err), $"Failed on {slot}: {err}");
                Assert.IsTrue(unit.equippedInstances.ContainsKey(slot));
                Assert.AreEqual(inst.instanceId, unit.equippedInstances[slot].instanceId);
                Assert.IsTrue(inst.isEquipped);
                Assert.AreEqual(unit.id, inst.boundCharacterId);
            }

            Assert.AreEqual(8, unit.equippedInstances.Count);
            Assert.Greater(unit.Derived.physicalDefense, 10);
            Assert.Greater(unit.Derived.physicalPower, 10);
        }

        [Test]
        public void Validation_EquipIncompatible_ClassWeaponProficiencyStrictlyEnforced()
        {
            // Verify all 9 classes reject weapons outside their allowed pool
            var classesAndIncompatibleWeapons = new (string classId, string incompatibleEqId)[]
            {
                ("wizard", "battle_greataxe"),     // Wizard cannot equip Greataxe
                ("wizard", "hunting_bow"),         // Wizard cannot equip Bow
                ("wizard", "iron_broadsword"),     // Wizard cannot equip Sword
                ("rogue", "battle_greataxe"),      // Rogue cannot equip Greataxe
                ("ranger", "sanctified_mace"),     // Ranger cannot equip Mace
                ("cleric", "duelist_rapier"),      // Cleric cannot equip Rapier
                ("warlock", "heavy_crossbow"),     // Warlock cannot equip Crossbow
                ("druid", "duelist_rapier"),       // Druid cannot equip Rapier
                ("martial_artist", "hunting_bow")  // Martial Artist cannot equip Bow
            };

            foreach (var (clsId, eqId) in classesAndIncompatibleWeapons)
            {
                var heroDef = new CharacterDef
                {
                    id = $"hero_{clsId}", name = $"Hero {clsId}", classId = clsId, raceId = "human",
                    baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5)
                };
                var unit = new Unit(heroDef);
                var eq = Database.Equipment[eqId];
                var inst = new EquipmentInstance(eq, EquipmentRarity.Common, 1);

                Assert.IsFalse(unit.TryEquip(EquipmentSlot.MainHand, inst, out string error));
                StringAssert.Contains("Incompatible", error);
                Assert.IsFalse(inst.isEquipped);
                Assert.IsNull(inst.boundCharacterId);
            }
        }

        [Test]
        public void Validation_EquipWrongSlot_StrictlyRejected()
        {
            var heroDef = new CharacterDef
            {
                id = "val_hero", name = "Val Hero", classId = "fighter", raceId = "human",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(heroDef);

            // 1. Armor into Head slot
            var bodyArmor = new EquipmentInstance(Database.Equipment["chain_hauberk"], EquipmentRarity.Common, 1);
            Assert.IsFalse(unit.TryEquip(EquipmentSlot.HeadArmor, bodyArmor, out string err1));
            StringAssert.Contains("cannot be equipped in Head Armor", err1);

            // 2. Weapon into Foot slot
            var sword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Common, 1);
            Assert.IsFalse(unit.TryEquip(EquipmentSlot.FootArmor, sword, out string err2));
            StringAssert.Contains("cannot be equipped in Foot Armor", err2);

            // 3. Shield into Body slot
            var shield = new EquipmentInstance(Database.Equipment["iron_shield"], EquipmentRarity.Common, 1);
            Assert.IsFalse(unit.TryEquip(EquipmentSlot.BodyArmor, shield, out string err3));
            StringAssert.Contains("cannot be equipped in Body Armor", err3);
        }

        [Test]
        public void Validation_EquipmentSwapping_OldItemUnboundAndNewItemEquipped()
        {
            var heroDef = new CharacterDef
            {
                id = "swapper", name = "Swapper", classId = "fighter", raceId = "human",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(heroDef);

            var swordA = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Common, 1);
            var swordB = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Rare, 3);

            // Equip Sword A
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.MainHand, swordA, out _));
            Assert.IsTrue(swordA.isEquipped);
            Assert.AreEqual("swapper", swordA.boundCharacterId);
            Assert.AreEqual(swordA.instanceId, unit.equippedInstances[EquipmentSlot.MainHand].instanceId);
            int powerA = unit.Derived.physicalPower;

            // Swap with Sword B on the same slot
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.MainHand, swordB, out _));
            Assert.IsFalse(swordA.isEquipped, "Old item must be unbound when swapped.");
            Assert.IsNull(swordA.boundCharacterId, "Old item boundCharacterId must be cleared.");

            Assert.IsTrue(swordB.isEquipped, "New item must become equipped.");
            Assert.AreEqual("swapper", swordB.boundCharacterId);
            Assert.AreEqual(swordB.instanceId, unit.equippedInstances[EquipmentSlot.MainHand].instanceId);
            int powerB = unit.Derived.physicalPower;
            Assert.Greater(powerB, powerA, "Rare Steel Axe should grant greater power than Common Iron Broadsword.");
        }

        [Test]
        public void Validation_Unequip_RemovesFromSlotAndResetsStats()
        {
            var heroDef = new CharacterDef
            {
                id = "unequipper", name = "Unequipper", classId = "fighter", raceId = "human",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(heroDef);
            int baseDef = unit.Derived.physicalDefense;

            var shield = new EquipmentInstance(Database.Equipment["iron_shield"], EquipmentRarity.Common, 1);
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.SecondHand, shield, out _));
            Assert.Greater(unit.Derived.physicalDefense, baseDef);
            Assert.IsTrue(shield.isEquipped);
            Assert.AreEqual("unequipper", shield.boundCharacterId);

            // Unequip
            Assert.IsTrue(unit.Unequip(EquipmentSlot.SecondHand));
            Assert.IsFalse(unit.equippedInstances.ContainsKey(EquipmentSlot.SecondHand));
            Assert.IsFalse(unit.equipped.ContainsKey(EquipmentSlot.SecondHand));
            Assert.IsFalse(shield.isEquipped);
            Assert.IsNull(shield.boundCharacterId);
            Assert.AreEqual(baseDef, unit.Derived.physicalDefense, "Stats must return exactly to baseline upon unequip.");
        }

        [Test]
        public void Validation_StatRecalculation_ComprehensiveDerivedStatVerification()
        {
            var heroDef = new CharacterDef
            {
                id = "stat_hero", name = "Stat Hero", classId = "fighter", raceId = "human",
                baseScores = AbilityScores.Uniform(10), // modifiers = 0
                stats = new Stats(100, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(heroDef);

            // 1. Weapon Power & Scaling (Fighter with weapon also triggers starter passive Weapon Proficiency +3)
            int basePwr = unit.Derived.physicalPower;
            var weapon = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            unit.TryEquip(EquipmentSlot.MainHand, weapon, out _);
            int expectedPwr = basePwr + weapon.Definition.weaponPower + (unit.starterPassiveId == "weapon_proficiency" ? 3 : 0);
            Assert.AreEqual(expectedPwr, unit.Derived.physicalPower);

            // 2. Physical Defense from Armor
            int baseDef = unit.Derived.physicalDefense;
            var armor = new EquipmentInstance(Database.Equipment["chain_hauberk"], EquipmentRarity.Common, 1);
            unit.TryEquip(EquipmentSlot.BodyArmor, armor, out _);
            Assert.AreEqual(baseDef + armor.Definition.physicalDefense, unit.Derived.physicalDefense);

            // 3. Ability Score Increase from Accessory
            var ringDef = new EquipmentDef
            {
                id = "val_str_ring", name = "Strength Ring", slot = EquipmentSlot.Accessory1,
                abilityBonuses = AbilityScores.Zero.Add(Ability.STR, 4) // STR +4 -> MOD +2
            };
            Database.Equipment["val_str_ring"] = ringDef;
            var ring = new EquipmentInstance(ringDef, EquipmentRarity.Common, 1);
            unit.TryEquip(EquipmentSlot.Accessory1, ring, out _);

            Assert.AreEqual(15, unit.FinalScores.str, "Base 10 + Human Racial +1 + Ring +4 = 15 STR.");
            Assert.AreEqual(2, unit.AbilityModifiers.str);
            Assert.AreEqual(4, unit.Derived.armorPenetration, "STR modifier +2 with 2.0x scaling grants +4 Armor Penetration.");
        }

        [Test]
        public void Validation_ExclusiveOwnership_ItemEquippedByOneHeroCannotBeEquippedByAnother()
        {
            var defA = new CharacterDef { id = "hero_A", name = "Hero A", classId = "fighter", raceId = "human", baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5) };
            var defB = new CharacterDef { id = "hero_B", name = "Hero B", classId = "fighter", raceId = "human", baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5) };
            var unitA = new Unit(defA);
            var unitB = new Unit(defB);

            var sharedSword = new EquipmentInstance(Database.Equipment["iron_broadsword"], EquipmentRarity.Epic, 3);

            // Unit A equips shared sword
            Assert.IsTrue(unitA.TryEquip(EquipmentSlot.MainHand, sharedSword, out _));
            Assert.IsTrue(sharedSword.isEquipped);
            Assert.AreEqual("hero_A", sharedSword.boundCharacterId);

            // Unit B attempts to equip the same item while unit A is holding it
            Assert.IsFalse(unitB.TryEquip(EquipmentSlot.MainHand, sharedSword, out string errConflict));
            StringAssert.Contains("already equipped by another character", errConflict);
            StringAssert.Contains("hero_A", errConflict);

            // Unit A unequips the item
            Assert.IsTrue(unitA.Unequip(EquipmentSlot.MainHand));
            Assert.IsFalse(sharedSword.isEquipped);
            Assert.IsNull(sharedSword.boundCharacterId);

            // Unit B can now equip the item cleanly
            Assert.IsTrue(unitB.TryEquip(EquipmentSlot.MainHand, sharedSword, out string errSuccess), errSuccess);
            Assert.IsTrue(sharedSword.isEquipped);
            Assert.AreEqual("hero_B", sharedSword.boundCharacterId);
        }

        [Test]
        public void Validation_EquipmentDuplication_SameInstanceCannotBeEquippedInMultipleSlots()
        {
            var heroDef = new CharacterDef { id = "dup_hero", name = "Dup Hero", classId = "fighter", raceId = "human", baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5) };
            var unit = new Unit(heroDef);

            var ringDef = new EquipmentDef { id = "val_ring_dup", name = "Unique Ring", slot = EquipmentSlot.Accessory1 };
            Database.Equipment["val_ring_dup"] = ringDef;
            var ringInst = new EquipmentInstance(ringDef, EquipmentRarity.Legendary, 5);

            // Equip in Accessory 1
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.Accessory1, ringInst, out _));
            Assert.AreEqual(ringInst.instanceId, unit.equippedInstances[EquipmentSlot.Accessory1].instanceId);

            // Attempt to equip the EXACT same instance into Accessory 2
            Assert.IsFalse(unit.TryEquip(EquipmentSlot.Accessory2, ringInst, out string errDup));
            StringAssert.Contains("already equipped in another slot", errDup);
            Assert.IsFalse(unit.equippedInstances.ContainsKey(EquipmentSlot.Accessory2));
        }

        [Test]
        public void Validation_DualWielding_OffHandWeaponAppliesFiftyPercentPenaltyAndFullBreak()
        {
            var barbDef = new CharacterDef
            {
                id = "dual_barb", name = "Dual Barbarian", classId = "barbarian", raceId = "human",
                baseScores = AbilityScores.Uniform(10), // zero STR mod
                stats = new Stats(120, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(barbDef);
            int basePwr = unit.Derived.physicalPower;
            int baseBreak = unit.Derived.physicalBreakPower;

            // Main hand axe: weaponPower = 4, breakPowerModifier = 2
            var mainAxe = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.MainHand, mainAxe, out _));

            // Off hand axe: weaponPower = 4, breakPowerModifier = 2
            var offAxe = new EquipmentInstance(Database.Equipment["steel_axe"], EquipmentRarity.Common, 1);
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.SecondHand, offAxe, out _));

            // Main hand grants 100% (4). Off-hand grants 50% (2). Total weapon power = +6
            Assert.AreEqual(basePwr + 4 + 2, unit.Derived.physicalPower);

            // Break power from both axes contributes 100% (2 + 2 = 4)
            Assert.AreEqual(baseBreak + 2 + 2, unit.Derived.physicalBreakPower);
        }

        [Test]
        public void Validation_LevelCapScaling_StatProgressionAndRestrictionsUpToLevel15()
        {
            var heroDef = new CharacterDef
            {
                id = "lvl_hero", name = "High Level Hero", classId = "fighter", raceId = "human",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5)
            };
            var unit = new Unit(heroDef);

            var highLevelGearDef = new EquipmentDef
            {
                id = "endgame_blade", name = "Void Blade", slot = EquipmentSlot.MainHand,
                weaponType = WeaponType.Sword, weaponPower = 15,
                restrictions = new EquipmentRestriction { minLevel = 15 }
            };
            Database.Equipment["endgame_blade"] = highLevelGearDef;
            var gearInst = new EquipmentInstance(highLevelGearDef, EquipmentRarity.Mythic, 15);

            // Level 1 cannot equip Level 15 gear
            unit.level = 1;
            Assert.IsFalse(unit.TryEquip(EquipmentSlot.MainHand, gearInst, out string errLvl));
            StringAssert.Contains("requires level 15", errLvl);

            // Level up to Level Cap (15)
            unit.level = 15;
            unit.RecalculateStats();
            Assert.IsTrue(unit.TryEquip(EquipmentSlot.MainHand, gearInst, out string errOk), errOk);
            Assert.IsTrue(gearInst.isEquipped);
            Assert.Greater(unit.Derived.physicalPower, 20);
        }
    }
}
