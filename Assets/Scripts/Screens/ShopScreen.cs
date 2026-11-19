using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Dungeon Outpost Caravan Shop (Run Gold) — migrated to Figma 20---Dungeon-Shop---Run-Gold.uxml.
    public class ShopScreen : GameScreen
    {
        enum ShopTab { BuyConsumables, BuyEquipment, Sell }

        ShopTab currentTab = ShopTab.BuyConsumables;
        DungeonShopData shopData;
        UitkMenu menu;
        int selectedIndex;
        int focusIndex;
        bool rebuilding;
        string feedback;
        bool feedbackBad;

        protected override void Build()
        {
            shopData = Run?.CurrentDungeonShop ?? DungeonShopData.Generate(Run?.Map?.act ?? 1);
            MountFigma("20---Dungeon-Shop---Run-Gold");
            Rebuild();
        }

        void SwitchTab(ShopTab tab)
        {
            currentTab = tab;
            selectedIndex = 0;
            Rebuild();
        }

        void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                UiRoot = FigmaUi.Mount(Root.gameObject, "20---Dungeon-Shop---Run-Gold");
                FigmaUi.HideInstancesWithText(UiRoot, FigmaUi.TitleRuleTags);
                FigmaUi.DestroyMenu(ref menu);
                menu = Root.gameObject.AddComponent<UitkMenu>();
                menu.AttachRoot(UiRoot);
                menu.onBack = OnLeaveShop;
                menu.onFocus = item => { if (!rebuilding && item != null) focusIndex = menu.items.IndexOf(item); };

                var backBtn = FigmaUi.FindButton(UiRoot, "Back");
                if (backBtn != null)
                    FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", OnLeaveShop));

                int currentRunGold = Run != null ? Run.Gold : 0;
                var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
                if (runGold != null)
                {
                    FigmaUi.SetInstanceText(runGold, "text-0", $"{currentRunGold:#,##0} RUN GOLD");
                    FigmaUi.AnchorRight(runGold, 24f);
                }

                var screen = UiRoot.Q<VisualElement>(className: "_20-dungeon-shop-run-gold") ?? UiRoot;
                var leftPanel = UiRoot.Q<VisualElement>(className: "panel-default__i-31");
                var rightPanel = UiRoot.Q<VisualElement>(className: "panel-gold__i-16");
                if (leftPanel != null)
                {
                    int lIdx = screen.IndexOf(leftPanel);
                    int rIdx = rightPanel != null ? screen.IndexOf(rightPanel) : screen.childCount;
                    for (int i = lIdx + 1; i < screen.childCount; i++)
                        if (i != rIdx) screen[i].style.display = DisplayStyle.None;
                }

                // Tab strip across top of left panel
                var tabRow = new VisualElement { style = { position = Position.Absolute, left = 44, top = 80, flexDirection = FlexDirection.Row } };
                foreach (var (tab, lbl) in new[] { (ShopTab.BuyConsumables, "SUPPLIES"), (ShopTab.BuyEquipment, "EQUIPMENT"), (ShopTab.Sell, "SELL GEAR") })
                {
                    var t = tab;
                    bool active = currentTab == t;
                    var tag = FigmaUi.InstantiateTemplate(active ? "Tag-Gold" : "Tag-Cls");
                    if (tag != null)
                    {
                        FigmaUi.SetInstanceText(tag, "text-0", lbl);
                        tag.style.marginRight = 8;
                        tabRow.Add(tag);
                        var it = menu.Add(tag, lbl, () => SwitchTab(t), selected: active);
                        it.visual = on => tag.style.opacity = on || active ? 1f : 0.72f;
                    }
                }
                screen.Add(tabRow);

                var listHost = new VisualElement { style = { position = Position.Absolute, left = 44, top = 122, width = 802, height = 560, flexDirection = FlexDirection.Column } };
                screen.Add(listHost);

                var scroll = FigmaUi.CreateVerticalScrollView();
                scroll.style.maxHeight = 490;
                listHost.Add(scroll);

                var entries = BuildEntries();
                selectedIndex = Mathf.Clamp(selectedIndex, 0, Math.Max(0, entries.Count - 1));

                for (int i = 0; i < entries.Count; i++)
                {
                    int idx = i;
                    var e = entries[i];
                    bool chosen = i == selectedIndex;
                    var row = new VisualElement
                    {
                        style =
                        {
                            height = 56, marginBottom = 6, paddingLeft = 12, paddingRight = 12,
                            flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center,
                            borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        }
                    };
                    row.Add(new UiLabel(chosen ? "▸" : "") { style = { width = 18, fontSize = 18, color = Palette.Gold2 } });
                    var icon = IconArt.Inline(e.icon, 3);
                    if (icon != null) { if (e.dim) icon.style.opacity = 0.5f; row.Add(icon); }
                    var col = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
                    col.Add(new UiLabel(e.title) { style = { fontSize = 18, color = e.dim ? Palette.Mist3 : Palette.Mist1 } });
                    col.Add(new UiLabel(e.sub) { style = { fontSize = 12, color = Palette.Mist3 } });
                    row.Add(col);
                    row.Add(new UiLabel(e.stock) { style = { width = 100, fontSize = 13, color = Palette.Mist2, unityTextAlign = TextAnchor.MiddleRight } });
                    row.Add(new UiLabel(e.priceText) { style = { width = 90, fontSize = 16, color = e.dim ? Palette.Mist3 : Palette.Gold1, unityTextAlign = TextAnchor.MiddleRight } });
                    scroll.Add(row);

                    var it = menu.Add(row, e.title, () =>
                    {
                        if (selectedIndex != idx) { selectedIndex = idx; Rebuild(); }
                        else e.action?.Invoke();
                    }, selected: chosen);
                    it.visual = on =>
                    {
                        row.style.backgroundColor = on || chosen ? Palette.Hex(0x2B2210) : Palette.Ink2;
                        var b = on ? Palette.Gold1 : chosen ? Palette.Gold2 : Palette.Ink5;
                        row.style.borderTopColor = b; row.style.borderBottomColor = b; row.style.borderLeftColor = b; row.style.borderRightColor = b;
                    };
                    it.visual(false);
                }

                if (entries.Count == 0)
                    scroll.Add(new UiLabel("NO EQUIPMENT IN INVENTORY TO SELL.") { style = { fontSize = 15, color = Palette.Mist3, alignSelf = UnityEngine.UIElements.Align.Center, marginTop = 80 } });

                listHost.Add(new UiLabel(feedback)
                {
                    style = { fontSize = 13, color = feedbackBad ? Palette.Err : Palette.Gold1, marginTop = 10, whiteSpace = WhiteSpace.Normal }
                });

                BuildDetail(screen, entries.Count > 0 ? entries[selectedIndex] : null, currentRunGold);

                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        class Entry
        {
            public string title, sub, desc, stock, priceText, btnText, tagText;
            public Texture2D icon;
            public int owned, price;
            public bool dim, canAct;
            public Action action;
        }

        List<Entry> BuildEntries()
        {
            var list = new List<Entry>();
            if (currentTab == ShopTab.BuyConsumables)
            {
                foreach (var c in shopData.consumables)
                {
                    var item = c;
                    bool soldOut = !item.isUnlimitedStock && item.stockCount <= 0;
                    int own = Run?.Inventory?.GetConsumableCount(item.itemId) ?? 0;
                    int effectivePrice = Run != null ? Run.GetDungeonShopDiscountedPrice(item.cost) : item.cost;
                    bool hasDiscount = effectivePrice < item.cost;
                    list.Add(new Entry
                    {
                        title = item.name,
                        icon = IconArt.Item(item.itemId),
                        sub = item.description.ToUpperInvariant(),
                        desc = item.description.ToUpperInvariant(),
                        stock = item.isUnlimitedStock ? "UNLIMITED" : $"× {item.stockCount}",
                        priceText = soldOut ? "SOLD" : hasDiscount ? $"{effectivePrice} (-15%)" : effectivePrice.ToString(),
                        btnText = soldOut ? "Sold Out" : hasDiscount ? $"Buy · {effectivePrice}G (-15% DRACONIC)" : $"Buy · {effectivePrice} Run Gold",
                        tagText = item.isReviveItem ? "REVIVE ITEM" : "CONSUMABLE",
                        owned = own,
                        price = effectivePrice,
                        dim = soldOut,
                        canAct = !soldOut && (Run?.Gold ?? 0) >= effectivePrice,
                        action = () => BuyConsumable(item),
                    });
                }
            }
            else if (currentTab == ShopTab.BuyEquipment)
            {
                foreach (var eq in shopData.equipmentShop.items)
                {
                    var item = eq;
                    string name = item.item?.Definition?.name ?? "Equipment";
                    int effectivePrice = Run != null ? Run.GetDungeonShopDiscountedPrice(item.cost) : item.cost;
                    bool hasDiscount = effectivePrice < item.cost;
                    list.Add(new Entry
                    {
                        title = name,
                        icon = IconArt.Gear(item.item?.Definition),
                        sub = item.item?.Definition != null ? EquipmentSlots.DisplayName(item.item.Definition.slot).ToUpperInvariant() : "GEAR",
                        desc = item.item?.Definition?.description?.ToUpperInvariant() ?? "",
                        stock = item.isSold ? "SOLD OUT" : "× 1",
                        priceText = item.isSold ? "—" : hasDiscount ? $"{effectivePrice} (-15%)" : effectivePrice.ToString(),
                        btnText = item.isSold ? "Sold Out" : hasDiscount ? $"Buy · {effectivePrice}G (-15% DRACONIC)" : $"Buy · {effectivePrice} Run Gold",
                        tagText = "EQUIPMENT",
                        owned = 0,
                        price = effectivePrice,
                        dim = item.isSold,
                        canAct = !item.isSold && (Run?.Gold ?? 0) >= effectivePrice,
                        action = () => BuyEquipment(item),
                    });
                }
            }
            else
            {
                var inv = Run?.Inventory?.equipment ?? new List<EquipmentInstance>();
                foreach (var eq in inv)
                {
                    var item = eq;
                    int val = item.CalculateSellValue();
                    list.Add(new Entry
                    {
                        title = item.Definition?.name ?? "Equipment",
                        icon = IconArt.Gear(item.Definition),
                        sub = item.Definition != null ? EquipmentSlots.DisplayName(item.Definition.slot).ToUpperInvariant() : "GEAR",
                        desc = $"SELL FOR {val} RUN GOLD.",
                        stock = "OWNED",
                        priceText = $"+{val}",
                        btnText = $"Sell · {val} Run Gold",
                        tagText = item.isEquipped ? "EQUIPPED" : "BAG",
                        owned = 1,
                        price = -val,
                        dim = item.isEquipped,
                        canAct = !item.isEquipped,
                        action = () => SellEquipment(item),
                    });
                }
            }
            return list;
        }

        void BuildDetail(VisualElement screen, Entry e, int runGold)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(host);
            if (e == null) return;

            host.Add(FigmaUi.CreateSectionHeader(e.title, 346f));
            var t = FigmaUi.InstantiateTemplate("Tag-Temp");
            if (t != null) { FigmaUi.SetInstanceText(t, "text-0", e.tagText); t.style.marginTop = 10; t.style.marginBottom = 10; host.Add(t); }

            host.Add(new UiLabel(e.desc) { style = { fontSize = 14, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 18 } });

            VisualElement Line(string l, string r)
            {
                var v = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, height = 28, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 } };
                v.Add(new UiLabel(l) { style = { fontSize = 13, color = Palette.Mist2 } });
                v.Add(new UiLabel(r) { style = { fontSize = 13, color = Palette.Mist1 } });
                return v;
            }
            host.Add(Line("IN STOCK", e.stock));
            host.Add(Line("YOU OWN", e.owned.ToString()));
            host.Add(Line("PRICE", e.price >= 0 ? $"{e.price} RUN GOLD" : $"+{-e.price} RUN GOLD"));
            host.Add(Line("RUN GOLD AFTER", $"{runGold} › {runGold - e.price}"));

            host.Add(new VisualElement { style = { flexGrow = 1 } });
            var btn = FigmaUi.CreateButton(e.btnText, e.canAct ? "button-primary" : "button-slab", 346f);
            btn.SetEnabled(e.canAct);
            host.Add(btn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(btn, e.btnText, e.action, interactable: e.canAct));
        }

        void BuyConsumable(ConsumableShopEntry c)
        {
            if (Run == null) return;
            if (shopData.TryBuyConsumable(Run, c.itemId, out _, out string err))
            {
                Sfx.Play(Sound.Coin);
                feedback = $"PURCHASED {c.name.ToUpperInvariant()} FOR {c.cost} GOLD!";
                feedbackBad = false;
                Rebuild();
            }
            else { Sfx.Play(Sound.Deny); feedback = err.ToUpperInvariant(); feedbackBad = true; Rebuild(); }
        }

        void BuyEquipment(ShopItem eqItem)
        {
            if (Run == null) return;
            if (shopData.TryBuyEquipment(Run, eqItem.shopItemId, out var bought, out string err))
            {
                Sfx.Play(Sound.Coin);
                feedback = $"PURCHASED {bought.Definition?.name.ToUpperInvariant()} FOR {eqItem.cost} GOLD!";
                feedbackBad = false;
                Rebuild();
            }
            else { Sfx.Play(Sound.Deny); feedback = err.ToUpperInvariant(); feedbackBad = true; Rebuild(); }
        }

        void SellEquipment(EquipmentInstance eq)
        {
            if (Run == null) return;
            if (shopData.TrySellEquipment(Run, eq.instanceId, out int goldGained, out string err))
            {
                Sfx.Play(Sound.Coin);
                feedback = $"SOLD {eq.Definition?.name.ToUpperInvariant()} FOR {goldGained} GOLD!";
                feedbackBad = false;
                Rebuild();
            }
            else { Sfx.Play(Sound.Deny); feedback = err.ToUpperInvariant(); feedbackBad = true; Rebuild(); }
        }

        void OnLeaveShop()
        {
            if (Run != null && Run.Current != null && Run.Cleared.Add(Run.Current.id))
                Run.Stats.nodesCompleted++;
            Game.Go(GameState.Map);
        }
    }
}
