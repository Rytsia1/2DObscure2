using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Dungeon Shop Screen:
    // Conforms to 640x360 reference resolution, Serif headers, and retro JRPG aesthetics.
    // Uses RUN GOLD (temporary dungeon currency). Sells consumables, equipment, and rare Phoenix Plumes (Revive Item).
    public class ShopScreen : GameScreen
    {
        enum ShopTab { BuyConsumables, BuyEquipment, Sell }

        ShopTab currentTab = ShopTab.BuyConsumables;
        DungeonShopData shopData;
        Transform contentRoot;
        Label feedbackLabel;
        Label goldBadgeLabel;
        readonly Label[] partyHpRows = new Label[4];
        readonly SpriteRenderer[] partyHpGauges = new SpriteRenderer[4];

        protected override void Build()
        {
            shopData = Run?.CurrentDungeonShop ?? DungeonShopData.Generate(Run?.Map?.act ?? 1);
            Backdrop.Create(Root, false, 28, Palette.Hex(0x131722), Palette.Hex(0x0C0E17), Palette.Gold3);

            // Header banner
            Ui.Label(Root, "DUNGEON OUTPOST CARAVAN", 320, 16, Palette.AntiqueGoldBright, Align.Center, 2, FontKind.Serif);
            Ui.Divider(Root, 140, 38, 360, Palette.DarkBorder, Palette.AntiqueGoldBright);

            // Run Gold Badge
            int currentRunGold = Run != null ? Run.Gold : 0;
            Ui.Panel(Root, 480, 12, 140, 24, Palette.Hex(0x22120A), Palette.Gold1);
            goldBadgeLabel = Ui.Label(Root, $"{currentRunGold} RUN GOLD", 550, 18, Palette.Gold1, Align.Center);

            // Left panel (370x240): Shop Tabs & Inventory Items
            Ui.Panel(Root, 20, 52, 380, 250, Palette.DeepCharcoal, Palette.DarkBorder);

            // Tab Buttons
            var tabMenu = Root.gameObject.AddComponent<Menu>();
            var consTabBtn = Ui.Button(Root, "SUPPLIES", 24, 56, 110, () => SwitchTab(ShopTab.BuyConsumables), 20, style: currentTab == ShopTab.BuyConsumables ? ButtonStyle.Primary : ButtonStyle.Slab);
            var equipTabBtn = Ui.Button(Root, "EQUIPMENT", 140, 56, 120, () => SwitchTab(ShopTab.BuyEquipment), 20, style: currentTab == ShopTab.BuyEquipment ? ButtonStyle.Primary : ButtonStyle.Slab);
            var sellTabBtn = Ui.Button(Root, "SELL GEAR", 266, 56, 120, () => SwitchTab(ShopTab.Sell), 20, style: currentTab == ShopTab.Sell ? ButtonStyle.Primary : ButtonStyle.Slab);
            tabMenu.Add(consTabBtn);
            tabMenu.Add(equipTabBtn);
            tabMenu.Add(sellTabBtn);

            Ui.Divider(Root, 24, 80, 372, Palette.DarkBorder, Palette.AntiqueGoldDim);

            // Right panel (210x250): Party Status
            Ui.Panel(Root, 410, 52, 210, 250, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, "PARTY VITALITY", 420, 60, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(Root, 420, 74, 190, Palette.DarkBorder, Palette.AntiqueGoldDim);

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < partyHpRows.Length && i < party.Count; i++)
            {
                var u = party[i];
                int y = 84 + i * 44;
                Ui.Label(Root, PixelFont.Fit(u.name.ToUpperInvariant(), 100), 420, y, u.Alive ? Palette.WarmIvory : Palette.SpecCrimson, Align.Left, 1, FontKind.Serif);
                partyHpRows[i] = Ui.Label(Root, u.Alive ? $"HP {u.hp}/{u.stats.maxHp}" : "FALLEN (DEAD)", 610, y, u.Alive ? Palette.WarmIvory : Palette.SpecCrimson, Align.Right);
                partyHpGauges[i] = Ui.Image(Root, PixelArt.GaugeBar(190, 6, u.Alive ? Palette.Emerald : Palette.HpLow, (float)u.hp / Mathf.Max(1, u.stats.maxHp)), 420, y + 16, Ui.Widget);
            }

            // Bottom bar: feedback and leave button
            feedbackLabel = Ui.Label(Root, "WELCOME TO THE CARAVAN. TRADE RUN GOLD TO PREPARE FOR DEEPER FLOORS.", 24, 314, Palette.WarmIvory, Align.Left);
            var leaveBtn = Ui.Button(Root, "LEAVE CARAVAN", 480, 310, 140, OnLeaveShop, 24, style: ButtonStyle.Primary);
            tabMenu.Add(leaveBtn);

            RefreshContent();
        }

        void SwitchTab(ShopTab tab)
        {
            currentTab = tab;
            RefreshContent();
        }

        void RefreshContent()
        {
            if (contentRoot != null) UnityEngine.Object.Destroy(contentRoot.gameObject);
            contentRoot = new GameObject("ShopContent").transform;
            contentRoot.SetParent(Root, false);

            int currentRunGold = Run != null ? Run.Gold : 0;
            goldBadgeLabel?.Set($"{currentRunGold} RUN GOLD");

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < partyHpRows.Length && i < party.Count; i++)
            {
                var u = party[i];
                if (partyHpRows[i] != null) partyHpRows[i].Set(u.Alive ? $"HP {u.hp}/{u.stats.maxHp}" : "FALLEN (DEAD)");
                if (partyHpGauges[i] != null) partyHpGauges[i].sprite = PixelArt.GaugeBar(190, 6, u.Alive ? Palette.Emerald : Palette.HpLow, (float)u.hp / Mathf.Max(1, u.stats.maxHp));
            }

            var menu = contentRoot.gameObject.AddComponent<Menu>();

            if (currentTab == ShopTab.BuyConsumables)
            {
                int y = 92;
                foreach (var c in shopData.consumables)
                {
                    string stockStr = c.isUnlimitedStock ? "UNLIMITED" : $"x{c.stockCount}";
                    string text = $"{c.name.ToUpperInvariant()} ({stockStr}) - {c.cost}G";
                    var btn = Ui.Button(contentRoot, PixelFont.Fit(text, 360), 28, y, 364, () => BuyConsumable(c), 24);
                    btn.tooltip = c.description.ToUpperInvariant();
                    if (!c.isUnlimitedStock && c.stockCount <= 0)
                    {
                        btn.label.Color = Palette.Mist3;
                    }
                    else if (c.isReviveItem)
                    {
                        btn.label.Color = Palette.Gold2;
                    }
                    menu.Add(btn);
                    y += 30;
                }
            }
            else if (currentTab == ShopTab.BuyEquipment)
            {
                int y = 92;
                var equipList = shopData.equipmentShop.items;
                for (int i = 0; i < equipList.Count && i < 6; i++)
                {
                    var eqItem = equipList[i];
                    string status = eqItem.isSold ? "[SOLD OUT]" : $"{eqItem.cost}G";
                    string name = eqItem.item?.Definition?.name ?? "Equipment";
                    string text = $"{name.ToUpperInvariant()} - {status}";
                    var btn = Ui.Button(contentRoot, PixelFont.Fit(text, 360), 28, y, 364, () => BuyEquipment(eqItem), 24);
                    btn.tooltip = eqItem.item?.Definition?.description?.ToUpperInvariant() ?? "";
                    if (eqItem.isSold) btn.label.Color = Palette.Mist3;
                    menu.Add(btn);
                    y += 30;
                }
            }
            else // Sell tab
            {
                int y = 92;
                var inventory = Run?.Inventory?.equipment ?? new List<EquipmentInstance>();
                if (inventory.Count == 0)
                {
                    Ui.Label(contentRoot, "NO EQUIPMENT IN INVENTORY TO SELL.", 210, 150, Palette.Mist3, Align.Center);
                }
                else
                {
                    for (int i = 0; i < inventory.Count && i < 6; i++)
                    {
                        var eq = inventory[i];
                        int sellVal = eq.CalculateSellValue();
                        string text = $"{eq.Definition?.name.ToUpperInvariant()} - SELL FOR {sellVal}G";
                        var btn = Ui.Button(contentRoot, PixelFont.Fit(text, 360), 28, y, 364, () => SellEquipment(eq), 24);
                        btn.tooltip = $"SELL {eq.Definition?.name.ToUpperInvariant()} FOR {sellVal} RUN GOLD";
                        menu.Add(btn);
                        y += 30;
                    }
                }
            }
        }

        void BuyConsumable(ConsumableShopEntry c)
        {
            if (Run == null) return;
            if (shopData.TryBuyConsumable(Run, c.itemId, out _, out string err))
            {
                Sfx.Play(Sound.Success);
                feedbackLabel?.Set(PixelFont.Fit($"PURCHASED {c.name.ToUpperInvariant()} FOR {c.cost} RUN GOLD!", 520));
                RefreshContent();
            }
            else
            {
                Sfx.Play(Sound.Deny);
                feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
            }
        }

        void BuyEquipment(ShopItem eqItem)
        {
            if (Run == null) return;
            if (shopData.TryBuyEquipment(Run, eqItem.shopItemId, out var bought, out string err))
            {
                Sfx.Play(Sound.Success);
                feedbackLabel?.Set(PixelFont.Fit($"PURCHASED {bought.Definition?.name.ToUpperInvariant()} FOR {eqItem.cost} RUN GOLD!", 520));
                RefreshContent();
            }
            else
            {
                Sfx.Play(Sound.Deny);
                feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
            }
        }

        void SellEquipment(EquipmentInstance eq)
        {
            if (Run == null) return;
            if (shopData.TrySellEquipment(Run, eq.instanceId, out int goldGained, out string err))
            {
                Sfx.Play(Sound.Success);
                feedbackLabel?.Set(PixelFont.Fit($"SOLD {eq.Definition?.name.ToUpperInvariant()} FOR {goldGained} RUN GOLD!", 520));
                RefreshContent();
            }
            else
            {
                Sfx.Play(Sound.Deny);
                feedbackLabel?.Set(PixelFont.Fit(err.ToUpperInvariant(), 520));
            }
        }

        void OnLeaveShop()
        {
            if (Run != null && Run.Current != null)
            {
                Run.Cleared.Add(Run.Current.id);
            }
            Game.Go(GameState.Map);
        }
    }
}
