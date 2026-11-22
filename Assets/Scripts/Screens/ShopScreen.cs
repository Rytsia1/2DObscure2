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
                {
                    backBtn.text = "Leave";
                    FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Leave", OnLeaveShop));
                }

                int currentRunGold = Run != null ? Run.Gold : 0;
                var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
                if (runGold != null)
                {
                    FigmaUi.SetInstanceText(runGold, "text-0", $"{currentRunGold:#,##0} SPOILS");
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
                        tag.AddToClassList("uitk-filter-chip");
                        tag.EnableInClassList("uitk-filter-active", active);
                        tag.style.marginRight = 8;
                        tabRow.Add(tag);
                        var it = menu.Add(tag, lbl, () => SwitchTab(t), selected: active);
                        it.visual = on =>
                        {
                            tag.EnableInClassList("uitk-focus", on);
                            tag.EnableInClassList("uitk-selected", active);
                            tag.EnableInClassList("uitk-filter-active", active);
                        };
                        it.visual(false);
                    }
                }
                screen.Add(tabRow);

                var listHost = new VisualElement { style = { position = Position.Absolute, left = 44, top = 122, width = 802, height = 560, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
                screen.Add(listHost);

                var scroll = FigmaUi.CreateVerticalScrollView();
                scroll.style.flexGrow = 1;
                scroll.style.flexBasis = 0;
                scroll.style.minHeight = 0;
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
                            overflow = Overflow.Hidden,
                            borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        }
                    };
                    var ptr = new UiLabel(chosen ? "▸" : "") { style = { width = 18, fontSize = 18, color = Palette.Gold2, flexShrink = 0 } };
                    row.Add(ptr);
                    var icon = IconArt.Inline(e.icon, 3);
                    if (icon != null) { if (e.dim) icon.style.opacity = 0.5f; row.Add(icon); }
                    var col = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, justifyContent = Justify.Center, overflow = Overflow.Hidden, marginRight = 8 } };
                    col.Add(new UiLabel(e.title) { style = { fontSize = 18, color = e.dim ? Palette.Mist3 : (e.titleCol ?? Palette.Mist1) } });
                    col.Add(new UiLabel(e.sub) { style = { fontSize = 12, color = Palette.Mist3 } });
                    row.Add(col);
                    row.Add(new UiLabel(e.stock) { style = { width = 90, marginRight = 8, flexShrink = 0, fontSize = 13, color = Palette.Mist2, unityTextAlign = TextAnchor.MiddleRight } });
                    row.Add(new UiLabel(e.priceText) { style = { width = 120, flexShrink = 0, fontSize = 15, color = e.dim ? Palette.Mist3 : Palette.Gold1, unityTextAlign = TextAnchor.MiddleRight } });
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
                        ptr.text = on || chosen ? "▸" : "";
                    };
                    it.visual(false);
                }

                if (entries.Count == 0)
                {
                    string emptyMsg = currentTab == ShopTab.Sell ? "NO EQUIPMENT IN INVENTORY TO SELL."
                        : currentTab == ShopTab.BuyEquipment ? "NO EQUIPMENT AVAILABLE." : "NO SUPPLIES AVAILABLE.";
                    scroll.Add(new UiLabel(emptyMsg) { style = { fontSize = 15, color = Palette.Mist3, alignSelf = UnityEngine.UIElements.Align.Center, marginTop = 80 } });
                }

                if (!string.IsNullOrEmpty(feedback))
                {
                    listHost.Add(new UiLabel(feedback)
                    {
                        style = { fontSize = 13, color = feedbackBad ? Palette.Err : Palette.Gold1, marginTop = 8, whiteSpace = WhiteSpace.Normal }
                    });
                }
                listHost.Add(new UiLabel("SPOILS ARE SPENT HERE AND LOST IF THE PARTY FALLS.") { style = { fontSize = 12, color = Palette.Mist3, marginTop = 6 } });

                BuildDetail(screen, entries.Count > 0 ? entries[selectedIndex] : null, currentRunGold);

                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        class Entry
        {
            public string title, sub, desc, stock, priceText, btnText, tagText, discountNote;
            public Texture2D icon;
            public Color? titleCol;
            public EquipmentInstance eqInstance;
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
                    int space = Run?.CarrySpace(item.itemId) ?? 99;
                    int effectivePrice = Run != null ? Run.GetDungeonShopDiscountedPrice(item.cost) : item.cost;
                    bool hasDiscount = effectivePrice < item.cost;
                    list.Add(new Entry
                    {
                        title = item.name,
                        icon = IconArt.Item(item.itemId),
                        sub = item.description.ToUpperInvariant(),
                        desc = item.description.ToUpperInvariant(),
                        stock = item.isUnlimitedStock ? "UNLIMITED" : $"× {item.stockCount}",
                        priceText = soldOut ? "SOLD" : hasDiscount ? $"{effectivePrice:#,##0} (-15%)" : effectivePrice.ToString("#,##0"),
                        btnText = soldOut ? "Sold Out" : space <= 0 ? "Carry Cap" : $"Buy · {effectivePrice:#,##0} Spoils",
                        tagText = item.isReviveItem ? "REVIVE ITEM" : "CONSUMABLE",
                        discountNote = hasDiscount ? "DRACONIC -15% APPLIED" : null,
                        owned = own,
                        price = effectivePrice,
                        dim = soldOut,
                        canAct = !soldOut && space > 0 && (Run?.Gold ?? 0) >= effectivePrice,
                        action = () => BuyConsumable(item),
                    });
                }
            }
            else if (currentTab == ShopTab.BuyEquipment)
            {
                foreach (var eq in shopData.equipmentShop.items)
                {
                    var item = eq;
                    var inst = item.item;
                    string name = inst?.Definition?.name ?? "Equipment";
                    int effectivePrice = Run != null ? Run.GetDungeonShopDiscountedPrice(item.cost) : item.cost;
                    bool hasDiscount = effectivePrice < item.cost;
                    list.Add(new Entry
                    {
                        title = inst != null && inst.upgradeLevel > 0 ? $"{name} +{inst.upgradeLevel}" : name,
                        icon = IconArt.Gear(inst?.Definition),
                        titleCol = inst != null ? EquipmentRarities.GetColor(inst.rarity) : (Color?)null,
                        eqInstance = inst,
                        sub = inst?.Definition != null ? $"{inst.rarity.ToString().ToUpperInvariant()} · {EquipmentSlots.DisplayName(inst.Definition.slot).ToUpperInvariant()}" : "GEAR",
                        desc = inst?.Definition?.description?.ToUpperInvariant() ?? "",
                        stock = item.isSold ? "SOLD OUT" : "× 1",
                        priceText = item.isSold ? "—" : hasDiscount ? $"{effectivePrice:#,##0} (-15%)" : effectivePrice.ToString("#,##0"),
                        btnText = item.isSold ? "Sold Out" : $"Buy · {effectivePrice:#,##0} Spoils",
                        tagText = inst != null ? inst.rarity.ToString().ToUpperInvariant() : "EQUIPMENT",
                        discountNote = hasDiscount ? "DRACONIC -15% APPLIED" : null,
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
                    string baseName = item.Definition?.name ?? "Equipment";
                    list.Add(new Entry
                    {
                        title = item.upgradeLevel > 0 ? $"{baseName} +{item.upgradeLevel}" : baseName,
                        icon = IconArt.Gear(item.Definition),
                        titleCol = EquipmentRarities.GetColor(item.rarity),
                        eqInstance = item,
                        sub = item.Definition != null ? $"{item.rarity.ToString().ToUpperInvariant()} · {EquipmentSlots.DisplayName(item.Definition.slot).ToUpperInvariant()}" : "GEAR",
                        desc = $"SELL FOR {val:#,##0} SPOILS.",
                        stock = "OWNED",
                        priceText = $"+{val:#,##0}",
                        btnText = $"Sell · {val:#,##0} Spoils",
                        tagText = item.isEquipped ? "EQUIPPED" : "BAG",
                        owned = 1,
                        price = -val,
                        dim = item.isEquipped,
                        canAct = !item.isEquipped,
                        action = () =>
                        {
                            if (EquipmentRarities.HasSpecialEffect(item.rarity))
                                FigmaUi.ShowConfirmationModal(UiRoot, "CONFIRM SELL", $"Sell {item.rarity} {baseName} for {val:#,##0} Spoils?", "SELL", () => SellEquipment(item));
                            else
                                SellEquipment(item);
                        },
                    });
                }
            }
            return list;
        }

        void BuildDetail(VisualElement screen, Entry e, int runGold)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);
            if (e == null) return;

            host.Add(FigmaUi.CreateSectionHeader(e.title, 346f));

            var detailScroll = FigmaUi.CreateVerticalScrollView();
            detailScroll.style.flexGrow = 1;
            detailScroll.style.flexBasis = 0;
            detailScroll.style.minHeight = 0;
            detailScroll.style.marginTop = 8;
            detailScroll.style.marginBottom = 10;
            host.Add(detailScroll);

            var tagRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 10 } };
            var t = FigmaUi.InstantiateTemplate("Tag-Temp");
            if (t != null) { FigmaUi.SetInstanceText(t, "text-0", e.tagText); t.style.marginRight = 6; tagRow.Add(t); }
            if (!string.IsNullOrEmpty(e.discountNote))
            {
                var dt = FigmaUi.InstantiateTemplate("Tag-Gold");
                if (dt != null) { FigmaUi.SetInstanceText(dt, "text-0", e.discountNote); tagRow.Add(dt); }
            }
            detailScroll.Add(tagRow);

            detailScroll.Add(new UiLabel(e.desc) { style = { fontSize = 14, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 14 } });

            VisualElement Line(string l, string r, Color? rc = null)
            {
                var v = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, height = 28, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 } };
                v.Add(new UiLabel(l) { style = { fontSize = 13, color = Palette.Mist2 } });
                v.Add(new UiLabel(r) { style = { fontSize = 13, color = rc ?? Palette.Mist1 } });
                return v;
            }
            if (e.eqInstance != null)
            {
                var eq = e.eqInstance;
                if (eq.EffectiveWeaponPower > 0) detailScroll.Add(Line("ATK", $"+{eq.EffectiveWeaponPower}"));
                if (eq.EffectivePhysicalDefense > 0) detailScroll.Add(Line("DEF", $"+{eq.EffectivePhysicalDefense}"));
                if (eq.EffectiveMagicDefense > 0) detailScroll.Add(Line("MDEF", $"+{eq.EffectiveMagicDefense}"));
                if (eq.EffectiveBreakPowerModifier > 0) detailScroll.Add(Line("BRK", $"+{eq.EffectiveBreakPowerModifier}"));
                if (eq.affixes != null && eq.affixes.Count > 0)
                    detailScroll.Add(new UiLabel(string.Join("\n", eq.affixes.Select(a => "• " + a.GetDescription())).ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Xp, whiteSpace = WhiteSpace.Normal, marginTop = 6, marginBottom = 6 } });
                if (eq.SpecialEffect != null)
                    detailScroll.Add(new UiLabel($"★ {eq.SpecialEffect.name}: {eq.SpecialEffect.description}".ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Gold2, whiteSpace = WhiteSpace.Normal, marginBottom = 6 } });
                var party = Run?.Party;
                if (party != null && party.Count > 0 && eq.Definition != null)
                {
                    var usable = party.Where(u => EquipmentValidator.Validate(eq.Definition, eq.Definition.slot, u.classDef, u.race, u.level, u.FinalScores, u.id) == null).Select(u => u.name).ToList();
                    detailScroll.Add(new UiLabel(usable.Count > 0 ? $"CAN USE: {string.Join(", ", usable).ToUpperInvariant()}" : "NO PARTY MEMBER CAN EQUIP") { style = { fontSize = 12, color = usable.Count > 0 ? Palette.Ok : Palette.Err, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });
                }
            }
            detailScroll.Add(Line("IN STOCK", e.stock));
            detailScroll.Add(Line("YOU OWN", e.owned.ToString()));
            detailScroll.Add(Line("PRICE", e.price >= 0 ? $"{e.price:#,##0} SPOILS" : $"+{-e.price:#,##0} SPOILS"));
            if (!e.dim)
            {
                int after = runGold - e.price;
                detailScroll.Add(Line("SPOILS AFTER", $"{runGold:#,##0} › {after:#,##0}", after < 0 ? Palette.Err : Palette.Mist1));
            }

            var btn = FigmaUi.CreateButton(e.btnText, e.canAct ? "button-primary" : "button-slab", 346f);
            btn.style.flexShrink = 0;
            btn.SetEnabled(e.canAct);
            host.Add(btn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(btn, e.btnText, e.action, interactable: e.canAct));
        }

        void BuyConsumable(ConsumableShopEntry c)
        {
            if (Run == null) return;
            int paid = Run.GetDungeonShopDiscountedPrice(c.cost);
            if (shopData.TryBuyConsumable(Run, c.itemId, out _, out string err))
            {
                Sfx.Play(Sound.Coin);
                feedback = $"PURCHASED {c.name.ToUpperInvariant()} FOR {paid:#,##0} SPOILS!";
                feedbackBad = false;
                Rebuild();
            }
            else { Sfx.Play(Sound.Deny); feedback = err.ToUpperInvariant(); feedbackBad = true; Rebuild(); }
        }

        void BuyEquipment(ShopItem eqItem)
        {
            if (Run == null) return;
            int paid = Run.GetDungeonShopDiscountedPrice(eqItem.cost);
            if (shopData.TryBuyEquipment(Run, eqItem.shopItemId, out var bought, out string err))
            {
                Sfx.Play(Sound.Coin);
                feedback = $"PURCHASED {bought.Definition?.name.ToUpperInvariant()} FOR {paid:#,##0} SPOILS!";
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
                feedback = $"SOLD {eq.Definition?.name.ToUpperInvariant()} FOR {goldGained} SPOILS!";
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
