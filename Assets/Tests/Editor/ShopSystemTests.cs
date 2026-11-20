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
            Assert.AreEqual(0, run.Gold, "Run Gold is emptied into the Hub.");
            Assert.AreEqual(1250, run.HubGold, "Hub Gold gains 100% of the Run Gold.");
        }

        [Test]
        public void Economy_RunFailure_WipesAllGold()
        {
            var run = CreateTestRun();
            run.HubGold = 200;
            run.Gold = 850;

            run.LoseCombat();

            Assert.AreEqual(0, run.Gold, "Run Gold must be completely lost on run failure.");
            Assert.AreEqual(200, run.HubGold, "Secured Hub Gold must survive run failure.");
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
            StringAssert.Contains("not enough spoils", err.ToLowerInvariant());
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
        public void HubShop_DraughtAndEtherHaveLimitedStockAndACarryCap_UsesHubGold()
        {
            var run = CreateTestRun();
            run.HubGold = 1000;

            var catalog = HubEconomy.GenerateHubShopCatalog(run);
            Assert.IsFalse(catalog.Any(c => c.itemId == "phoenix_down"), "Hub Shop must NOT sell the Revive Item.");
            Assert.IsFalse(catalog.First(c => c.itemId == "draught").isUnlimitedStock);
            Assert.IsFalse(catalog.First(c => c.itemId == "ether").isUnlimitedStock);
            Assert.IsFalse(catalog.First(c => c.itemId == "elixir").isUnlimitedStock);
            Assert.AreEqual(5, catalog.First(c => c.itemId == "elixir").stockCount);

            int start = run.CarryCount("draught"); // starting items
            Assert.IsTrue(HubEconomy.TryBuyHubConsumable(run, "draught", 3, out string err), err);
            Assert.AreEqual(940, run.HubGold, "Hub Shop purchases must deduct Hub Gold.");
            Assert.AreEqual(HubEconomy.HubRestock["draught"] - 3, HubEconomy.HubStockLeft(run, "draught"));

            int room = run.CarrySpace("draught");
            Assert.AreEqual(10 - start - 3, room, "carry cap is 10");
            Assert.IsFalse(HubEconomy.TryBuyHubConsumable(run, "draught", room + 1, out err), "cannot buy past the carry cap");
            Assert.AreEqual(940, run.HubGold, "a refused purchase costs nothing");
            Assert.IsTrue(HubEconomy.TryBuyHubConsumable(run, "draught", room, out err), err);
            Assert.AreEqual(10, run.CarryCount("draught"));

            run.HubGold = 10;
            Assert.IsFalse(HubEconomy.TryBuyHubConsumable(run, "elixir", 1, out err));
            StringAssert.Contains("not enough gold", err.ToLowerInvariant());
        }

        [Test]
        public void HubShop_SellsOutThenRestocksOnDungeonClear_StockSurvivesSave()
        {
            var run = CreateTestRun();
            run.HubGold = 5000;
            void Drink() // empty the bag so stock, not the carry cap, is what runs out
            {
                run.Items.RemoveAll(i => i == "draught");
                run.Inventory.RemoveConsumable("draught", run.Inventory.GetConsumableCount("draught"));
            }

            Drink();
            Assert.IsTrue(HubEconomy.TryBuyHubConsumable(run, "draught", 10, out string err), err);
            Drink();
            Assert.AreEqual(0, HubEconomy.HubStockLeft(run, "draught"));
            Assert.IsFalse(HubEconomy.TryBuyHubConsumable(run, "draught", 1, out err));
            StringAssert.Contains("sold out", err.ToLowerInvariant());

            try
            {
                var snapshot = PersistenceCommitHandler.CreateCommittedSnapshot(run);
                var reloaded = CreateTestRun();
                reloaded.RestoreFromCampaignSave(snapshot);
                Assert.AreEqual(0, HubEconomy.HubStockLeft(reloaded, "draught"), "sold-out stock survives a save");

                PersistenceCommitHandler.CommitDungeonClear(run);
                Assert.AreEqual(HubEconomy.HubRestock["draught"], HubEconomy.HubStockLeft(run, "draught"), "restocked when a dungeon is cleared");
            }
            finally
            {
                PersistenceCommitHandler.SetActiveCommittedSnapshot(null);
            }
        }

        [Test]
        public void CarryCapAppliesToRewardsAndTheDungeonShop()
        {
            var run = CreateTestRun();
            run.Gold = 1000;
            for (int i = 0; i < 20; i++) run.Grant(new RewardOption { kind = RewardKind.Item, id = "ether" });
            Assert.AreEqual(10, run.Items.Count(i => i == "ether"));
            Assert.AreEqual(10, run.Inventory.GetConsumableCount("ether"));

            var shop = DungeonShopData.Generate(1);
            Assert.IsFalse(shop.TryBuyConsumable(run, "ether", out _, out string err));
            StringAssert.Contains("carry at most", err.ToLowerInvariant());
            Assert.AreEqual(1000, run.Gold, "a refused purchase costs nothing");
        }

        [Test]
        public void UsingAnItemInBattleAlsoRemovesItFromTheBag()
        {
            var run = CreateTestRun();
            var b = new Battle(run.Party, new[] { Database.Enemies["warden"] }, run.Items, new System.Random(1), run);
            Combatant hero = null;
            for (int i = 0; i < 50 && hero == null; i++)
            {
                var c = b.NextTurn();
                if (c.IsPlayer) hero = c;
                else b.Perform(BattleAction.Guard(c));
            }
            hero.unit.hp -= 10;
            int listBefore = run.Items.Count(i => i == "draught"), bagBefore = run.Inventory.GetConsumableCount("draught");

            b.Perform(BattleAction.Item(hero, Database.Items["draught"], hero));

            Assert.AreEqual(listBefore - 1, run.Items.Count(i => i == "draught"));
            Assert.AreEqual(bagBefore - 1, run.Inventory.GetConsumableCount("draught"), "bag and battle list stay in step");
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
            Assert.AreEqual(0, run.Gold);
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
            run.HubGold = 560;

            var saved = GameSave.SaveRunState(run);
            Assert.AreEqual(340, saved.gold);
            Assert.AreEqual(560, saved.hubGold);

            var freshRun = CreateTestRun(seed: 77);
            freshRun.RestoreFromSave(saved);

            Assert.AreEqual(340, freshRun.Gold);
            Assert.AreEqual(560, freshRun.HubGold);
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
