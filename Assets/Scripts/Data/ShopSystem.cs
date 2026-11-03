using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public enum ShopFacilityKind
    {
        DungeonShop,
        HubShop,
        Armory,
        Blacksmith,
        TrainingGround,
        Temple
    }

    [Serializable]
    public class ConsumableShopEntry
    {
        public string itemId;
        public string name;
        public string description;
        public int cost;
        public bool isReviveItem;
        public bool isUnlimitedStock;
        public int stockCount;

        public ConsumableShopEntry() { }

        public ConsumableShopEntry(ItemDef def, int cost, bool isUnlimited = false, int stock = 1)
        {
            if (def != null)
            {
                itemId = def.id;
                name = def.name;
                description = def.description;
                isReviveItem = def.isRevive;
            }
            this.cost = Math.Max(1, cost);
            this.isUnlimitedStock = isUnlimited;
            this.stockCount = isUnlimited ? 999 : Math.Max(0, stock);
        }
    }

    [Serializable]
    public class DungeonShopData
    {
        public int dungeonTier = 1;
        public List<ConsumableShopEntry> consumables = new List<ConsumableShopEntry>();
        public ShopInventory equipmentShop = new ShopInventory();

        public static DungeonShopData Generate(int tier, System.Random rng = null)
        {
            var rand = rng ?? new System.Random();
            var data = new DungeonShopData
            {
                dungeonTier = Math.Clamp(tier, 1, 8),
                equipmentShop = EquipmentShopManager.GenerateShop(tier, rand, itemCount: 6)
            };

            // Standard Consumables available in Dungeon Shop (uses Run Gold)
            data.consumables.Add(new ConsumableShopEntry(Database.Items["draught"], 20, isUnlimited: true));
            data.consumables.Add(new ConsumableShopEntry(Database.Items["ether"], 25, isUnlimited: true));
            data.consumables.Add(new ConsumableShopEntry(Database.Items["elixir"], 50, isUnlimited: false, stock: 2));

            // Exclusive, Rare & Expensive Revive Item (Phoenix Plume) - 1 in stock
            if (Database.Items.ContainsKey("phoenix_down"))
            {
                data.consumables.Add(new ConsumableShopEntry(Database.Items["phoenix_down"], 250, isUnlimited: false, stock: 1));
            }

            return data;
        }

        public bool TryBuyConsumable(RunState run, string itemId, out ConsumableShopEntry bought, out string error)
        {
            bought = null;
            if (run == null) { error = "RunState is null."; return false; }

            var entry = consumables.FirstOrDefault(c => c.itemId == itemId);
            if (entry == null)
            {
                error = "Item not found in Dungeon Shop.";
                return false;
            }

            if (!entry.isUnlimitedStock && entry.stockCount <= 0)
            {
                error = $"{entry.name} is out of stock.";
                return false;
            }

            if (run.Gold < entry.cost)
            {
                error = $"Not enough Run Gold. Requires {entry.cost} Run Gold, but only have {run.Gold} Run Gold.";
                return false;
            }

            run.Gold -= entry.cost;
            if (!entry.isUnlimitedStock)
                entry.stockCount--;

            run.Items.Add(entry.itemId);
            if (run.Inventory != null)
                run.Inventory.AddConsumable(entry.itemId, 1);

            bought = entry;
            error = null;
            return true;
        }

        public bool TryBuyEquipment(RunState run, string shopItemId, out EquipmentInstance bought, out string error)
        {
            return EquipmentShopManager.TryBuyEquipment(run, equipmentShop, shopItemId, out bought, out error);
        }

        public bool TrySellEquipment(RunState run, string instanceId, out int goldGained, out string error)
        {
            return EquipmentShopManager.TrySellEquipment(run, instanceId, out goldGained, out error);
        }
    }

    public static class HubEconomy
    {
        public const int TempleReviveCost = 100;

        // 1:1 Transfer without any deductions or conversion fees (GDD Section 3.4)
        public static int TransferRunGoldToHub(RunState run)
        {
            if (run == null) return 0;
            int transferred = run.Gold;
            run.HubGold += transferred;
            run.Gold = 0;
            return transferred;
        }

        // On run failure, Run Gold is lost 100%, Hub Gold is 100% safe (GDD Section 3.1 & 3.2)
        public static void HandleRunFailure(RunState run)
        {
            if (run == null) return;
            run.Gold = 0;
        }

        public static List<ConsumableShopEntry> GenerateHubShopCatalog()
        {
            // Hub Shop sells consumables with UNLIMITED STOCK (GDD Section 5.1 & Resolved Decision)
            // Does NOT sell Revive Item (exclusive to Dungeon Shop)
            return new List<ConsumableShopEntry>
            {
                new ConsumableShopEntry(Database.Items["draught"], 20, isUnlimited: true),
                new ConsumableShopEntry(Database.Items["ether"], 25, isUnlimited: true),
                new ConsumableShopEntry(Database.Items["elixir"], 50, isUnlimited: true)
            };
        }

        public static bool TryBuyHubConsumable(RunState run, string itemId, int count, out string error)
        {
            if (run == null) { error = "RunState is null."; return false; }
            if (count <= 0) { error = "Invalid item count."; return false; }

            var catalog = GenerateHubShopCatalog();
            var entry = catalog.FirstOrDefault(c => c.itemId == itemId);
            if (entry == null)
            {
                error = "Item not offered at the Hub Shop.";
                return false;
            }

            int totalCost = entry.cost * count;
            if (run.HubGold < totalCost)
            {
                error = $"Not enough Hub Gold. Requires {totalCost} Hub Gold, but party only has {run.HubGold} Hub Gold.";
                return false;
            }

            run.HubGold -= totalCost;
            for (int i = 0; i < count; i++)
            {
                run.Items.Add(entry.itemId);
                if (run.Inventory != null)
                    run.Inventory.AddConsumable(entry.itemId, 1);
            }

            error = null;
            return true;
        }

        public static bool TryBuyHubEquipment(RunState run, ShopInventory armory, string shopItemId, out EquipmentInstance bought, out string error)
        {
            bought = null;
            if (run == null) { error = "RunState is null."; return false; }
            if (armory == null) { error = "Armory inventory is null."; return false; }

            var shopItem = armory.FindItem(shopItemId);
            if (shopItem == null)
            {
                error = "Item not found in Armory.";
                return false;
            }

            if (shopItem.isSold)
            {
                error = "Item is already sold out.";
                return false;
            }

            if (run.HubGold < shopItem.cost)
            {
                error = $"Not enough Hub Gold. Requires {shopItem.cost} Hub Gold, but party only has {run.HubGold} Hub Gold.";
                return false;
            }

            if (run.Inventory != null && run.Inventory.IsEquipmentFull)
            {
                error = "Cannot buy: Inventory is full (100/100 slots).";
                return false;
            }

            run.HubGold -= shopItem.cost;
            shopItem.isSold = true;
            bought = shopItem.item;

            if (run.Inventory != null)
            {
                run.Inventory.TryAddEquipment(bought, out _);
            }

            error = null;
            return true;
        }

        public static bool TryBlacksmithUpgrade(RunState run, EquipmentInstance eq, out string error)
        {
            if (run == null) { error = "RunState is null."; return false; }
            if (eq == null) { error = "Equipment is null."; return false; }

            if (!eq.CanUpgrade(out error))
                return false;

            int cost = eq.GetUpgradeCostGold();
            if (run.HubGold < cost)
            {
                error = $"Not enough Hub Gold. Requires {cost} Hub Gold, but party only has {run.HubGold} Hub Gold.";
                return false;
            }

            run.HubGold -= cost;
            if (!eq.TryUpgrade(out error))
                return false;

            // Recalculate stats for bound character if equipped
            if (!string.IsNullOrEmpty(eq.boundCharacterId))
            {
                var unit = run.Party.FirstOrDefault(u => u.id == eq.boundCharacterId);
                unit?.RecalculateStats();
            }

            error = null;
            return true;
        }

        public static bool TryTempleRevive(RunState run, Unit unit, out string error)
        {
            if (run == null) { error = "RunState is null."; return false; }
            if (unit == null) { error = "Target unit is null."; return false; }

            if (unit.Alive && unit.hp > 0)
            {
                error = $"{unit.name} is already alive and does not require revival.";
                return false;
            }

            if (run.HubGold < TempleReviveCost)
            {
                error = $"Not enough Hub Gold. Temple revival requires {TempleReviveCost} Hub Gold, but party only has {run.HubGold} Hub Gold.";
                return false;
            }

            run.HubGold -= TempleReviveCost;
            unit.hp = Math.Max(1, (int)Math.Round(unit.stats.maxHp * 0.50f));
            unit.RecalculateStats();

            error = null;
            return true;
        }

        public static bool TryUseReviveItemOutOfCombat(RunState run, Unit target, out string error)
        {
            if (run == null) { error = "RunState is null."; return false; }
            if (target == null) { error = "Target unit is null."; return false; }

            if (target.Alive && target.hp > 0)
            {
                error = $"{target.name} is alive and cannot be revived.";
                return false;
            }

            if (!run.Items.Contains("phoenix_down") && (run.Inventory == null || run.Inventory.GetConsumableCount("phoenix_down") <= 0))
            {
                error = "Party does not possess a Phoenix Plume (Revive Item).";
                return false;
            }

            run.Items.Remove("phoenix_down");
            if (run.Inventory != null)
                run.Inventory.RemoveConsumable("phoenix_down", 1);

            int restoreHp = Math.Max(1, (int)Math.Round(target.stats.maxHp * 0.30f));
            target.hp = restoreHp;
            target.RecalculateStats();

            error = null;
            return true;
        }
    }
}
 
