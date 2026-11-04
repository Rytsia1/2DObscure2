using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class ShopSystemTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            _ = Database.Items;
            _ = Database.Characters;
            _ = Database.Equipment;
        }

        static RunState CreateTestRun(int seed = 42, int act = 1)
        {
            var party = Database.Party.Select(id => Database.Characters[id]).ToList();
            var map = MapGenerator.Generate(seed, act);
            return new RunState(map, party, seed);
        }

        // =====================================================================
        // 1. Dual-Currency Economy Rules
        // =====================================================================

        [Test]
        public void Economy_DungeonClear_RetainsGold1to1WithoutDeduction()
        {
            var run = CreateTestRun();
            run.Gold = 1250;

            int transferred = run.TransferRunGoldToHub();

            Assert.AreEqual(1250, transferred, "Transferred amount must equal exact remaining Gold.");
            Assert.AreEqual(1250, run.Gold, "Gold is retained 100% across dungeon clears.");
            Assert.AreEqual(1250, run.HubGold, "HubGold alias matches Gold.");
        }

        [Test]
        public void Economy_RunFailure_WipesAllGold()
        {
            var run = CreateTestRun();
            run.Gold = 850;

            run.LoseCombat();

            Assert.AreEqual(0, run.Gold, "Gold must be completely lost on run failure.");
            Assert.AreEqual(0, run.HubGold, "HubGold alias must also be wiped on run failure.");
            Assert.AreEqual(RunStatus.Defeat, run.Status);
        }

        // =====================================================================
        // 2. Dungeon Shop Rules
        // =====================================================================

        [Test]
        public void DungeonShop_OffersConsumablesAndRareExpensiveReviveItem()
        {
            var shop = DungeonShopData.Generate(1);

            Assert.IsNotNull(shop.consumables);
            Assert.GreaterOrEqual(shop.consumables.Count, 3, "Dungeon shop must offer standard consumables.");

            var reviveItem = shop.consumables.FirstOrDefault(c => c.itemId == "phoenix_down");
            Assert.IsNotNull(reviveItem, "Dungeon shop must stock the Phoenix Plume revive item.");
            Assert.IsTrue(reviveItem.isReviveItem);
            Assert.AreEqual(250, reviveItem.cost, "Revive item must be expensive.");
            Assert.IsFalse(reviveItem.isUnlimitedStock, "Revive item in Dungeon Shop must NOT have unlimited stock.");
            Assert.AreEqual(1, reviveItem.stockCount, "Revive item should have limited stock (1).");
        }

        [Test]
        public void DungeonShop_BuyConsumableAndEquipment_ConsumesRunGold()
        {
            var run = CreateTestRun();
            run.Gold = 300;
            var shop = DungeonShopData.Generate(1);

            // Purchase Draught (cost: 20)
            bool ok = shop.TryBuyConsumable(run, "draught", out var bought, out string err);
            Assert.IsTrue(ok, $"Buy Draught should succeed: {err}");
            Assert.AreEqual(280, run.Gold, "Purchasing consumable must deduct Run Gold.");
            Assert.IsTrue(run.Items.Contains("draught"));

            // Attempt to buy Phoenix Plume (cost: 250, current gold: 280)
            ok = shop.TryBuyConsumable(run, "phoenix_down", out var boughtRevive, out err);
            Assert.IsTrue(ok, $"Buy Revive should succeed: {err}");
            Assert.AreEqual(30, run.Gold);
            Assert.AreEqual(0, boughtRevive.stockCount, "Stock must decrement.");

            // Attempt second buy (out of stock)
            ok = shop.TryBuyConsumable(run, "phoenix_down", out _, out err);
            Assert.IsFalse(ok, "Second buy should fail because item is out of stock.");
            StringAssert.Contains("out of stock", err.ToLowerInvariant());

            // Attempt buy with insufficient Run Gold
            ok = shop.TryBuyConsumable(run, "elixir", out _, out err); // Elixir costs 50, run has 30
            Assert.IsFalse(ok, "Buy with insufficient gold should fail.");
            StringAssert.Contains("not enough gold", err.ToLowerInvariant());
        }

        [Test]
        public void DungeonShop_SellEquipment_GrantsRunGold()
        {
            var run = CreateTestRun();
            run.Gold = 50;

            var swordDef = Database.Equipment["iron_broadsword"];
            var instance = EquipmentLootGenerator.CreateInstance(swordDef, EquipmentRarity.Common, 1, new System.Random(1));
            run.Inventory.TryAddEquipment(instance, out _);

            var shop = DungeonShopData.Generate(1);
            bool ok = shop.TrySellEquipment(run, instance.instanceId, out int gained, out string err);

            Assert.IsTrue(ok, $"Selling should succeed: {err}");
            Assert.Greater(gained, 0);
            Assert.AreEqual(50 + gained, run.Gold, "Selling equipment must increase Run Gold.");
            Assert.IsFalse(run.Inventory.equipment.Any(e => e.instanceId == instance.instanceId));
        }

        // =====================================================================
        // 3. Hub Facilities: Shop, Armory, Blacksmith, Temple
        // =====================================================================

        [Test]
        public void HubShop_ConsumablesHaveUnlimitedStock_UsesHubGold()
        {
            var run = CreateTestRun();
            run.HubGold = 500;

            // Verify catalog has no revive item
            var catalog = HubEconomy.GenerateHubShopCatalog();
            Assert.IsFalse(catalog.Any(c => c.itemId == "phoenix_down"), "Hub Shop must NOT sell the Revive Item.");

            // Buy 3 Draughts (20G each = 60G)
            bool ok = HubEconomy.TryBuyHubConsumable(run, "draught", 3, out string err);
            Assert.IsTrue(ok, $"Hub buy should succeed: {err}");
            Assert.AreEqual(440, run.HubGold, "Hub Shop purchases must deduct Hub Gold.");

            // Buy another 10 Draughts (unlimited stock)
            ok = HubEconomy.TryBuyHubConsumable(run, "draught", 10, out err);
            Assert.IsTrue(ok, "Hub Shop stock is unlimited, repeated purchase must succeed.");
            Assert.AreEqual(240, run.HubGold);

            // Rejects if insufficient Hub Gold
            ok = HubEconomy.TryBuyHubConsumable(run, "elixir", 10, out err); // 50 * 10 = 500 > 240
            Assert.IsFalse(ok);
            StringAssert.Contains("not enough gold", err.ToLowerInvariant());
        }

        [Test]
        public void HubArmory_BuyEquipment_ConsumesHubGold()
        {
            var run = CreateTestRun();
            run.HubGold = 1000;

            var armory = EquipmentShopManager.GenerateShop(1, new System.Random(1));
            var targetItem = armory.items[0];
            int cost = targetItem.cost;

            bool ok = HubEconomy.TryBuyHubEquipment(run, armory, targetItem.shopItemId, out var bought, out string err);
            Assert.IsTrue(ok, $"Armory purchase should succeed: {err}");
            Assert.AreEqual(1000 - cost, run.HubGold, "Purchasing from Armory must deduct Hub Gold.");
            Assert.IsTrue(targetItem.isSold, "Purchased item must be marked sold out.");
            Assert.IsTrue(run.Inventory.equipment.Any(e => e.instanceId == bought.instanceId));
        }

        [Test]
        public void HubBlacksmith_UpgradeEquipment_IncreasesStatsAndConsumesHubGold()
        {
            var run = CreateTestRun();
            run.HubGold = 500;

            var swordDef = new EquipmentDef
            {
                id = "blacksmith_test_sword",
                name = "Test Sword",
                slot = EquipmentSlot.MainHand,
                weaponType = WeaponType.Sword,
                weaponPower = 20,
                maxUpgradeLevel = 5,
                            };
            var instance = new EquipmentInstance(swordDef, EquipmentRarity.Common, 1);
            run.Inventory.TryAddEquipment(instance, out _);

            int startAtk = instance.EffectiveWeaponPower;
            int cost = instance.GetUpgradeCostGold();

            bool ok = HubEconomy.TryBlacksmithUpgrade(run, instance, out string err);
            Assert.IsTrue(ok, $"Blacksmith upgrade should succeed: {err}");
            Assert.AreEqual(1, instance.upgradeLevel);
            Assert.AreEqual(21, instance.EffectiveWeaponPower, "Upgraded weapon must scale base weapon power (20 * 1.05 = 21).");
            Assert.AreEqual(500 - cost, run.HubGold, "Upgrading must deduct Hub Gold.");
        }

        [Test]
        public void HubTemple_ReviveDeadPartyMembers_RestoresHpAndConsumesHubGold()
        {
            var run = CreateTestRun();
            run.HubGold = 250;
            var knight = run.Party[0];

            // Knight is currently alive -> Temple should reject
            bool ok = HubEconomy.TryTempleRevive(run, knight, out string err);
            Assert.IsFalse(ok, "Cannot revive an ally who is already alive.");
            StringAssert.Contains("already alive", err.ToLowerInvariant());

            // Knight falls in combat
            knight.hp = 0;
            Assert.IsFalse(knight.Alive);

            // Temple revive
            ok = HubEconomy.TryTempleRevive(run, knight, out err);
            Assert.IsTrue(ok, $"Temple revive should succeed: {err}");
            Assert.IsTrue(knight.Alive, "Hero must be alive after Temple revival.");
            Assert.AreEqual(Math.Max(1, (int)Math.Round(knight.stats.maxHp * 0.50f)), knight.hp);
            Assert.AreEqual(150, run.HubGold, "Temple must deduct 100 Hub Gold.");

            // Rejects if insufficient Hub Gold
            run.HubGold = 50;
            knight.hp = 0;
            ok = HubEconomy.TryTempleRevive(run, knight, out err);
            Assert.IsFalse(ok, "Temple revive must fail if Hub Gold is below 100.");
            StringAssert.Contains("not enough gold", err.ToLowerInvariant());
        }

        // =====================================================================
        // 4. Revive Item In-Combat and Out-of-Combat Mechanics
        // =====================================================================

        [Test]
        public void ReviveItem_InCombat_RevivesFallenAlly_ConsumesTurn()
        {
            var run = CreateTestRun();
            var heroA = run.Party[0];
            var heroB = run.Party[1];

            var items = new List<string> { "phoenix_down" };
            var battle = new Battle(new[] { heroA, heroB }, new[] { Database.Enemies["skeleton"] }, items, new System.Random(42));

            var combatantA = battle.Party[0];
            var combatantB = battle.Party[1];

            // Ally B dies
            combatantB.unit.hp = 0;
            Assert.IsFalse(combatantB.Alive);

            // Ally A takes their turn to use Phoenix Plume on Ally B
            combatantA.unit.stats.spd = 99;
            var current = battle.NextTurn();
            Assert.AreEqual(combatantA, current);

            var action = BattleAction.Item(combatantA, Database.Items["phoenix_down"], combatantB);
            var hits = battle.Perform(action);

            Assert.IsTrue(combatantB.Alive, "Ally B must be revived in-combat!");
            Assert.AreEqual(Math.Max(1, (int)Math.Round(combatantB.Stats.maxHp * 0.30f)), combatantB.unit.hp);
            Assert.IsFalse(battle.Items.Contains("phoenix_down"), "Revive Item must be consumed from battle inventory.");
        }

        [Test]
        public void ReviveItem_OutOfCombat_RevivesFallenHero()
        {
            var run = CreateTestRun();
            run.Items.Add("phoenix_down");
            run.Inventory.AddConsumable("phoenix_down", 1);

            var mage = run.Party[2];
            mage.hp = 0;
            Assert.IsFalse(mage.Alive);

            bool ok = HubEconomy.TryUseReviveItemOutOfCombat(run, mage, out string err);
            Assert.IsTrue(ok, $"Out of combat revive should succeed: {err}");
            Assert.IsTrue(mage.Alive);
            Assert.AreEqual(Math.Max(1, (int)Math.Round(mage.stats.maxHp * 0.30f)), mage.hp);
            Assert.IsFalse(run.Items.Contains("phoenix_down"));
        }

        // =====================================================================
        // 5. Boss Mutual Kill & Sequential Progression
        // =====================================================================

        [Test]
        public void MutualKill_OnBossFight_YieldsVictoryAndTransfersRunGold()
        {
            var run = CreateTestRun();
            run.Gold = 600;


            var hero = run.Party[0];
            var boss = Database.Enemies["warden"]; // Abyssal Warden (Tier.Boss)

            var battle = new Battle(new[] { hero }, new[] { boss }, new List<string>(), new System.Random(1));
            var combatHero = battle.Party[0];
            var combatBoss = battle.Enemies[0];

            // Set both HP to 0 (Mutual Kill)
            combatHero.unit.hp = 0;
            combatBoss.unit.hp = 0;

            // In Battle.Outcome: Enemies.All(!Alive) is checked first
            Assert.AreEqual(Outcome.Victory, battle.Outcome, "Mutual kill on boss must evaluate as Victory (GDD Section 8.3 & Resolved Decision).");

            // On victory, Gold is retained 1:1
            run.TransferRunGoldToHub();
            Assert.AreEqual(600, run.Gold);
            Assert.AreEqual(600, run.HubGold);
        }

        // =====================================================================
        // 6. Persistence of Hub Gold and Run Gold
        // =====================================================================

        [Test]
        public void Persistence_SavesAndLoadsGold()
        {
            var run = CreateTestRun(seed: 77);
            run.Gold = 340;

            var saved = GameSave.SaveRunState(run);
            Assert.AreEqual(340, saved.gold);
            Assert.AreEqual(340, saved.hubGold);

            var freshRun = CreateTestRun(seed: 77);
            freshRun.RestoreFromSave(saved);

            Assert.AreEqual(340, freshRun.Gold);
            Assert.AreEqual(340, freshRun.HubGold);
        }

        // =====================================================================
        // 7. Procedural Map Generator Includes Semi-Random Shop Nodes
        // =====================================================================

        [Test]
        public void MapGeneration_IncludesSemiRandomShopNodes()
        {
            var allNodes = Enumerable.Range(1, 20).SelectMany(s => MapGenerator.Generate(s).nodes).ToList();
            int shopCount = allNodes.Count(n => n.type == NodeType.Shop);

            Assert.Greater(shopCount, 0, "Procedural map generator must generate semi-random Shop nodes across dungeons.");

            // Verify no consecutive shop nodes along connected paths
            foreach (var seed in Enumerable.Range(1, 10))
            {
                var map = MapGenerator.Generate(seed);
                var byId = map.nodes.ToDictionary(n => n.id);
                foreach (var n in map.nodes.Where(n => n.type == NodeType.Shop))
                {
                    foreach (var nextId in n.next)
                    {
                        var next = byId[nextId];
                        Assert.AreNotEqual(NodeType.Shop, next.type, $"Shop node {n.id} cannot connect directly to another Shop node {nextId}.");
                    }
                }
            }
        }
    }
}
