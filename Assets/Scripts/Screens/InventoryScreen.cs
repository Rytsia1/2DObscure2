using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Inventory Screen — migrated to Figma 14---Inventory.uxml (1280x720 UI Toolkit).
    public class InventoryScreen : GameScreen
    {
        readonly GameState returnState;
        readonly InventoryRuntime inventory;
        readonly List<Unit> party;

        InventoryCategory currentCategory = InventoryCategory.Equipment;
        EquipmentSortMode currentSortMode = EquipmentSortMode.RarityDesc;
        EquipmentSlot? currentSlotFilter = null;
        int selectedIndex = 0;
        int pageIndex = 0;
        const int Cols = 10, GridRows = 4, ItemsPerPage = Cols * GridRows;

        bool confirmSellActive;
        string feedbackText = "";
        int focusIndex;
        bool rebuilding;
        UitkMenu menu;

        public InventoryScreen() : this(null, null, null) { }

        public InventoryScreen(InventoryRuntime inv = null, IEnumerable<Unit> partyUnits = null, GameState? backTo = null)
        {
            inventory = inv ?? (Game.I?.Run?.Inventory ?? new InventoryRuntime());
            if (partyUnits != null) party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null) party = Game.I.Run.Party;
            else if (Game.I?.Roster != null) party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            returnState = backTo ?? ReturnTarget();
        }

        protected override void Build()
        {
            MountFigma("14---Inventory");
            Refresh();
        }

        int CategoryCount(InventoryCategory c) => c switch
        {
            InventoryCategory.Equipment => inventory.EquipmentCount,
            InventoryCategory.Consumables => inventory.consumables.Count,
            InventoryCategory.Materials => inventory.materials.Count,
            _ => inventory.keyItems.Count(),
        };

        List<EquipmentInstance> GetCurrentEquipmentList() =>
            inventory.GetSortedEquipment(currentSortMode, currentSlotFilter);

        void Refresh()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                UiRoot = FigmaUi.Mount(Root.gameObject, "14---Inventory");
                FigmaUi.DestroyMenu(ref menu);
                menu = Root.gameObject.AddComponent<UitkMenu>();
                menu.AttachRoot(UiRoot);
                menu.onBack = () => Game?.Back(returnState);
                menu.onPrevPage = () => { if (pageIndex > 0) ChangePage(-1); };
                menu.onNextPage = () => ChangePage(1);
                menu.onFocus = item => { if (!rebuilding && item != null) focusIndex = menu.items.IndexOf(item); };

            var backBtn = FigmaUi.FindButton(UiRoot, "Back");
            if (backBtn != null) FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", () => Game?.Back(returnState)));

            // Bag capacity bar
            int eqCount = inventory.EquipmentCount;
            bool nearlyFull = eqCount >= InventoryRuntime.MaxEquipmentCapacity - 10;
            FigmaUi.SetGaugeFill(UiRoot.Q<VisualElement>(className: "gauge-xp"), (float)eqCount / InventoryRuntime.MaxEquipmentCapacity,
                inventory.IsEquipmentFull ? Palette.HpLow : nearlyFull ? Palette.Warn : Palette.Gold2);
            var capLbl = UiRoot.Q<UiLabel>(className: "_12-100-vars");
            if (capLbl != null)
            {
                capLbl.text = $"{eqCount} / {InventoryRuntime.MaxEquipmentCapacity}";
                capLbl.style.color = inventory.IsEquipmentFull ? Palette.HpLow : Palette.Gold1;
            }

            var screen = UiRoot.Q<VisualElement>(className: "_14-inventory") ?? UiRoot;
            var tv = UiRoot.Q<TabView>(); if (tv != null) tv.style.display = DisplayStyle.None;

            // Hide baked static grid & detail panel children
            var mainPanel = UiRoot.Q<VisualElement>(className: "panel-default__i-24");
            var detailPanel = UiRoot.Q<VisualElement>(className: "panel-gold__i-10");
            if (mainPanel != null)
            {
                int mIdx = screen.IndexOf(mainPanel), dIdx = detailPanel != null ? screen.IndexOf(detailPanel) : screen.childCount;
                for (int i = mIdx + 1; i < screen.childCount; i++)
                    if (i != dIdx) screen[i].style.display = DisplayStyle.None;
            }

            // Dynamic Category Tabs
            var tabRow = new VisualElement { style = { position = Position.Absolute, left = 24, top = 68, height = 38, flexDirection = FlexDirection.Row } };
            foreach (var (cat, name) in new[]
            {
                (InventoryCategory.Equipment, "Equipment"),
                (InventoryCategory.Consumables, "Consumables"),
                (InventoryCategory.Materials, "Materials"),
                (InventoryCategory.KeyItems, "Key Items"),
            })
            {
                var c = cat;
                bool active = currentCategory == c;
                var btn = FigmaUi.CreateTabButton($"{name} {CategoryCount(c)}", active, null, 38f, 18f);
                btn.style.paddingLeft = 14;
                btn.style.paddingRight = 14;
                btn.style.marginRight = 6;
                tabRow.Add(btn);
                var it = menu.Add(btn, name, () => SelectCategory(c), selected: active);
                it.visual = on => FigmaUi.ApplyTabVisual(btn, active, on, true);
                it.visual(false);
            }
            screen.Add(tabRow);

            // Sort / Filter bar
            var leftHost = new VisualElement { style = { position = Position.Absolute, left = 44, top = 122, width = 798, height = 560, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(leftHost);

            bool isEqTab = currentCategory == InventoryCategory.Equipment;
            var topCtrl = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12 } };
            string sortLbl = currentSortMode switch
            {
                EquipmentSortMode.RarityDesc => "RARITY",
                EquipmentSortMode.LevelDesc => "LEVEL",
                EquipmentSortMode.Slot => "SLOT",
                EquipmentSortMode.Name => "NAME",
                _ => "VALUE"
            };
            var sortBtn = FigmaUi.CreateCompactButton($"SORT: {sortLbl}", 240f);
            sortBtn.style.marginRight = 10;
            sortBtn.SetEnabled(isEqTab);
            string fltName = currentSlotFilter.HasValue ? EquipmentSlots.DisplayName(currentSlotFilter.Value).ToUpperInvariant() : "ALL";
            var filterBtn = FigmaUi.CreateCompactButton($"FILTER: {fltName}", 240f);
            filterBtn.SetEnabled(isEqTab);
            topCtrl.Add(sortBtn);
            topCtrl.Add(filterBtn);
            leftHost.Add(topCtrl);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(sortBtn, "Sort", CycleSortMode, interactable: isEqTab));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(filterBtn, "Filter", CycleFilter, interactable: isEqTab));

            // 10x4 Item Grid
            int total = isEqTab ? GetCurrentEquipmentList().Count : CategoryCount(currentCategory);
            int totalPages = Math.Max(1, (total + ItemsPerPage - 1) / ItemsPerPage);
            if (pageIndex >= totalPages) pageIndex = totalPages - 1;
            int pageCount = Math.Max(0, Math.Min(ItemsPerPage, total - pageIndex * ItemsPerPage));
            selectedIndex = pageCount > 0 ? Mathf.Clamp(selectedIndex, 0, pageCount - 1) : 0;

            var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, width = 780, height = 316 } };
            int gridStartMenuIdx = menu.items.Count;
            var gridMenuItems = new List<UitkMenu.Item>();
            for (int i = 0; i < ItemsPerPage; i++)
            {
                int slotIdx = i;
                int itemIdx = pageIndex * ItemsPerPage + i;
                var info = CellContent(itemIdx);
                bool has = info.has, sel = has && i == selectedIndex;
                var cell = FigmaUi.InstantiateTemplate(sel ? "Item-Slot-Selected" : has ? "Item-Slot-Filled" : "Item-Slot-Empty") ?? new VisualElement();
                cell.style.width = 68;
                cell.style.height = 68;
                cell.style.marginRight = (i % Cols) < Cols - 1 ? 10 : 0;
                cell.style.marginBottom = 10;
                FigmaUi.SetInstanceText(cell, "text-0", has ? info.badge : "");
                if (has && info.tint.HasValue)
                {
                    var lbl = cell.Q<UiLabel>("text-0");
                    if (lbl != null) lbl.style.color = info.tint.Value;
                }
                IconArt.Overlay(cell, info.icon);
                grid.Add(cell);
                if (has)
                {
                    int gIdx = gridMenuItems.Count;
                    var it = menu.Add(cell, info.title, () => SelectItem(slotIdx), selected: sel);
                    gridMenuItems.Add(it);
                    it.onNavigate = dir =>
                    {
                        if (dir == 0 && gIdx >= Cols) return gridStartMenuIdx + gIdx - Cols;
                        if (dir == 2 && gIdx + Cols < gridMenuItems.Count) return gridStartMenuIdx + gIdx + Cols;
                        return -1;
                    };
                    it.visual = on =>
                    {
                        var border = on ? Palette.Gold1 : sel ? Palette.Gold2 : (info.tint ?? Palette.Ink4);
                        cell.style.borderTopColor = border; cell.style.borderBottomColor = border;
                        cell.style.borderLeftColor = border; cell.style.borderRightColor = border;
                        cell.style.backgroundColor = on ? Palette.Hex(0x2B2210) : StyleKeyword.Null;
                    };
                    it.visual(false);
                }
            }
            leftHost.Add(grid);

            // Paging bar
            var pageRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginTop = 10 } };
            var prevBtn = FigmaUi.CreateButton("‹ Prev", "button-slab", 120f);
            prevBtn.SetEnabled(pageIndex > 0);
            var pageLbl = new UiLabel($"PAGE {pageIndex + 1} / {totalPages}") { style = { width = 140, unityTextAlign = TextAnchor.MiddleCenter, fontSize = 14, color = Palette.Mist3 } };
            var nextBtn = FigmaUi.CreateButton("Next ›", "button-slab", 120f);
            nextBtn.SetEnabled(pageIndex < totalPages - 1);
            pageRow.Add(prevBtn); pageRow.Add(pageLbl); pageRow.Add(nextBtn);
            leftHost.Add(pageRow);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(prevBtn, "Prev", () => ChangePage(-1), interactable: pageIndex > 0));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(nextBtn, "Next", () => ChangePage(1), interactable: pageIndex < totalPages - 1));

            leftHost.Add(new VisualElement { style = { flexGrow = 1 } });
            if (!string.IsNullOrEmpty(feedbackText))
                leftHost.Add(new UiLabel(feedbackText) { style = { fontSize = 14, color = Palette.Gold1, marginBottom = 6 } });

            BuildDetailPanel(screen, pageIndex * ItemsPerPage + selectedIndex);

                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        (bool has, string badge, Color? tint, string title, Texture2D icon) CellContent(int itemIdx)
        {
            switch (currentCategory)
            {
                case InventoryCategory.Equipment:
                {
                    var list = GetCurrentEquipmentList();
                    if (itemIdx >= list.Count) break;
                    var eq = list[itemIdx];
                    string b = eq.upgradeLevel > 0 ? $"+{eq.upgradeLevel}" : Palette.RarityBadge(eq.rarity);
                    return (true, b, EquipmentRarities.GetColor(eq.rarity), eq.Definition?.name ?? "Gear", IconArt.Gear(eq.Definition));
                }
                case InventoryCategory.Consumables:
                    if (itemIdx >= inventory.consumables.Count) break;
                    var c = inventory.consumables[itemIdx];
                    return (true, c.count.ToString(), Palette.Gold1, c.Definition?.name ?? c.itemId, IconArt.Item(c.itemId));
                case InventoryCategory.Materials:
                {
                    var mats = inventory.materials.ToList();
                    if (itemIdx >= mats.Count) break;
                    return (true, mats[itemIdx].Value.ToString(), Palette.ElementIce, mats[itemIdx].Key, IconArt.Item(mats[itemIdx].Key));
                }
                default:
                {
                    var keys = inventory.keyItems.ToList();
                    if (itemIdx >= keys.Count) break;
                    return (true, "K", Palette.Gold2, keys[itemIdx], IconArt.Item(keys[itemIdx]));
                }
            }
            return (false, "", null, "", null);
        }

        void BuildDetailPanel(VisualElement screen, int selIdx)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);
            host.Add(FigmaUi.CreateSectionHeader("Item details", 346f));

            string dName = "", dSub = "", dAffix = "", dSpec = "", dRest = "", dVal = "";
            Color nameCol = Palette.Gold2;
            var stats = new List<(string k, string v)>();

            switch (currentCategory)
            {
                case InventoryCategory.Equipment:
                {
                    var eqList = GetCurrentEquipmentList();
                    if (selIdx < eqList.Count)
                    {
                        var eq = eqList[selIdx]; var def = eq.Definition;
                        dName = eq.upgradeLevel > 0 ? $"{def?.name} +{eq.upgradeLevel}" : def?.name ?? "Equipment";
                        nameCol = EquipmentRarities.GetColor(eq.rarity);
                        dSub = $"{eq.rarity.ToString().ToUpperInvariant()} · {(def != null ? EquipmentSlots.DisplayName(def.slot).ToUpperInvariant() : "GEAR")} · LV {eq.level}";
                        if (eq.EffectiveWeaponPower > 0) stats.Add(("ATK", $"+{eq.EffectiveWeaponPower}"));
                        if (eq.EffectivePhysicalDefense > 0) stats.Add(("DEF", $"+{eq.EffectivePhysicalDefense}"));
                        if (eq.EffectiveMagicDefense > 0) stats.Add(("MDEF", $"+{eq.EffectiveMagicDefense}"));
                        if (eq.EffectiveBreakPowerModifier > 0) stats.Add(("BRK", $"+{eq.EffectiveBreakPowerModifier}"));
                        stats.Add(("UPGRADE", $"+{eq.upgradeLevel}"));
                        dAffix = eq.affixes != null && eq.affixes.Count > 0 ? string.Join("\n", eq.affixes.Select(a => "• " + a.GetDescription())).ToUpperInvariant() : "NO AFFIXES";
                        var se = eq.SpecialEffect;
                        if (se != null) dSpec = $"★ {se.name}: {se.description}".ToUpperInvariant();
                        var r = def?.restrictions;
                        dRest = r != null && r.allowedClasses != null && r.allowedClasses.Length > 0 ? $"CLASS: {string.Join("/", r.allowedClasses).ToUpperInvariant()}" : "ALL CLASSES COMPATIBLE";
                        dVal = $"SELL {eq.CalculateSellValue()}G · UPGRADE {eq.GetUpgradeCostGold()}G";
                    }
                    break;
                }
                case InventoryCategory.Consumables:
                    if (selIdx < inventory.consumables.Count)
                    {
                        var c = inventory.consumables[selIdx]; var d = c.Definition;
                        dName = d?.name ?? c.itemId;
                        nameCol = d != null ? EquipmentRarities.GetColor(d.rarity) : Palette.Emerald;
                        dSub = $"CONSUMABLE · STACK {c.count}/{c.maxStack}";
                        dAffix = (d?.description ?? "Consumable item.").ToUpperInvariant();
                        dRest = $"TARGET: {(d?.target ?? "ONE ALLY").ToUpperInvariant()}";
                        dVal = $"SELL VALUE: {Math.Max(1, (d?.shopValue ?? 20) / 2)}G EACH";
                    }
                    break;
                case InventoryCategory.Materials:
                {
                    var mats = inventory.materials.ToList();
                    if (selIdx < mats.Count)
                    {
                        dName = mats[selIdx].Key; nameCol = Palette.WarmIvory;
                        dSub = $"MATERIAL · QUANTITY {mats[selIdx].Value}";
                        dAffix = "UPGRADE MATERIAL USED AT THE HUB BLACKSMITH.";
                    }
                    break;
                }
                default:
                {
                    var keys = inventory.keyItems.ToList();
                    if (selIdx < keys.Count)
                    {
                        dName = keys[selIdx]; nameCol = Palette.Gold2;
                        dSub = "KEY ITEM · BOUND TO HEROES";
                        dAffix = "UNIQUE PROGRESSION ITEM FOR DUNGEON AND STORY EXPLORATION.";
                        dVal = "CANNOT BE SOLD";
                    }
                    break;
                }
            }

            var detailScroll = FigmaUi.CreateVerticalScrollView();
            detailScroll.style.flexGrow = 1;
            detailScroll.style.flexBasis = 0;
            detailScroll.style.minHeight = 0;
            detailScroll.style.marginTop = 8;
            detailScroll.style.marginBottom = 10;
            host.Add(detailScroll);

            if (dName != "")
            {
                detailScroll.Add(new UiLabel(dName) { style = { fontSize = 20, color = nameCol, whiteSpace = WhiteSpace.Normal, marginTop = 4 } });
                detailScroll.Add(new UiLabel(dSub) { style = { fontSize = 12, color = Palette.Mist3, whiteSpace = WhiteSpace.Normal, marginBottom = 10 } });
                foreach (var (k, v) in stats)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, height = 26, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 } };
                    row.Add(new UiLabel(k) { style = { fontSize = 13, color = Palette.Mist2 } });
                    row.Add(new UiLabel(v) { style = { fontSize = 13, color = Palette.Mist1 } });
                    detailScroll.Add(row);
                }
                if (dAffix != "") detailScroll.Add(new UiLabel(dAffix) { style = { fontSize = 13, color = Palette.Xp, whiteSpace = WhiteSpace.Normal, marginTop = 12 } });
                if (dSpec != "") detailScroll.Add(new UiLabel(dSpec) { style = { fontSize = 13, color = Palette.Gold2, whiteSpace = WhiteSpace.Normal, marginTop = 6 } });
                if (dRest != "") detailScroll.Add(new UiLabel(dRest) { style = { fontSize = 12, color = Palette.Mist3, whiteSpace = WhiteSpace.Normal, marginTop = 8 } });
                if (dVal != "") detailScroll.Add(new UiLabel(dVal) { style = { fontSize = 13, color = Palette.Gold1, marginTop = 6 } });
            }
            else
            {
                detailScroll.Add(new UiLabel("SELECT AN ITEM.") { style = { fontSize = 14, color = Palette.Mist3, marginTop = 20 } });
            }

            bool hasSel = dName != "";
            bool isEq = hasSel && currentCategory == InventoryCategory.Equipment;
            bool canDiscard = hasSel && currentCategory != InventoryCategory.KeyItems;
            bool canUse = hasSel && currentCategory == InventoryCategory.Consumables;
            var r1 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8, flexShrink = 0 } };
            var eqBtn = FigmaUi.CreateCompactButton("Equip", 110f); eqBtn.SetEnabled(isEq);
            var upBtn = FigmaUi.CreateCompactButton("Upgrade", 110f); upBtn.SetEnabled(isEq);
            var sellBtn = FigmaUi.CreateCompactButton(confirmSellActive ? "Confirm?" : "Sell", 110f, confirmSellActive ? "button-danger" : "button-slab"); sellBtn.SetEnabled(isEq);
            r1.Add(eqBtn); r1.Add(upBtn); r1.Add(sellBtn);
            host.Add(r1);

            var r2 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, flexShrink = 0 } };
            var disBtn = FigmaUi.CreateButton("Discard", "button-danger", 160f); disBtn.SetEnabled(canDiscard);
            var useBtn = FigmaUi.CreateButton("Use on...", canUse ? "button-primary" : "button-slab", 174f); useBtn.SetEnabled(canUse);
            r2.Add(disBtn); r2.Add(useBtn);
            host.Add(r2);

            FigmaUi.ApplyDefaultFocusVisual(menu.Add(eqBtn, "Equip", OnEquipClicked, interactable: isEq));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(upBtn, "Upgrade", OnUpgradeClicked, interactable: isEq));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(sellBtn, "Sell", OnSellClicked, interactable: isEq));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(disBtn, "Discard", OnDiscardClicked, interactable: canDiscard));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(useBtn, "Use on...", OnUseClicked, interactable: canUse));
        }

        void SelectCategory(InventoryCategory cat) { currentCategory = cat; selectedIndex = 0; pageIndex = 0; confirmSellActive = false; Refresh(); }

        void CycleSortMode()
        {
            confirmSellActive = false;
            currentSortMode = currentSortMode switch
            {
                EquipmentSortMode.RarityDesc => EquipmentSortMode.LevelDesc,
                EquipmentSortMode.LevelDesc => EquipmentSortMode.Slot,
                EquipmentSortMode.Slot => EquipmentSortMode.Name,
                EquipmentSortMode.Name => EquipmentSortMode.SellValueDesc,
                _ => EquipmentSortMode.RarityDesc
            };
            Refresh();
        }

        void CycleFilter()
        {
            confirmSellActive = false;
            currentSlotFilter = currentSlotFilter switch
            {
                null => EquipmentSlot.MainHand,
                EquipmentSlot.MainHand => EquipmentSlot.SecondHand,
                EquipmentSlot.SecondHand => EquipmentSlot.HeadArmor,
                EquipmentSlot.HeadArmor => EquipmentSlot.BodyArmor,
                EquipmentSlot.BodyArmor => EquipmentSlot.LegArmor,
                EquipmentSlot.LegArmor => EquipmentSlot.FootArmor,
                EquipmentSlot.FootArmor => EquipmentSlot.Accessory1,
                EquipmentSlot.Accessory1 => EquipmentSlot.Accessory2,
                _ => null
            };
            Refresh();
        }

        void ChangePage(int delta) { confirmSellActive = false; pageIndex = Math.Max(0, pageIndex + delta); selectedIndex = 0; Refresh(); }
        void SelectItem(int slotIndex) { selectedIndex = slotIndex; confirmSellActive = false; Refresh(); }

        EquipmentInstance GetSelectedEquipment()
        {
            var list = GetCurrentEquipmentList();
            int idx = pageIndex * ItemsPerPage + selectedIndex;
            return idx < list.Count ? list[idx] : null;
        }

        int SelectedStackIndex => pageIndex * ItemsPerPage + selectedIndex;
        void Say(string text) { feedbackText = (text ?? "").ToUpperInvariant(); Refresh(); }

        void OnEquipClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment();
            if (eq == null || party == null || party.Count == 0) return;
            var targetHero = party.FirstOrDefault(u => EquipmentValidator.Validate(eq.Definition, eq.Definition.slot, u.classDef, u.race, u.level, u.FinalScores, u.id) == null) ?? party[0];
            if (EquipmentManager.QuickEquip(targetHero, eq, inventory, out string err)) { Sfx.Play(Sound.Equip); Say($"EQUIPPED {eq.Definition?.name} ON {targetHero.name}"); }
            else { Sfx.Play(Sound.Deny); Say($"CANNOT EQUIP: {err}"); }
        }

        void OnUpgradeClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment(); if (eq == null) return;
            int goldCost = eq.GetUpgradeCostGold(), matCost = eq.GetUpgradeCostMaterials(), playerGold = Game.I?.Run?.HubGold ?? 9999, playerMat = inventory.GetMaterialCount("iron_ingot");
            if (!eq.CanUpgrade(out string reason)) { Sfx.Play(Sound.Deny); Say(reason); return; }
            if (Game.I?.Run != null && Game.I.Run.HubGold < goldCost) { Sfx.Play(Sound.Deny); Say($"NEED {goldCost}G TO UPGRADE (HAVE {playerGold}G)"); return; }
            if (matCost > 0 && playerMat < matCost) { Sfx.Play(Sound.Deny); Say($"NEED {matCost} UPGRADE MATERIALS (HAVE {playerMat})"); return; }
            if (Game.I?.Run != null) Game.I.Run.HubGold -= goldCost;
            inventory.RemoveMaterial("iron_ingot", matCost);
            eq.TryUpgrade(out _);
            Sfx.Play(Sound.BlacksmithForge);
            Say($"UPGRADED {eq.Definition?.name} TO +{eq.upgradeLevel}!");
        }

        void OnSellClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment(); if (eq == null) return;
            if (eq.isEquipped) { Sfx.Play(Sound.Deny); Say("CANNOT SELL EQUIPPED ITEM. UNEQUIP IT FIRST."); return; }
            if (EquipmentRarities.HasSpecialEffect(eq.rarity) && !confirmSellActive)
            {
                confirmSellActive = true;
                Sfx.Play(Sound.ModalOpen);
                Say($"SELL {eq.rarity} ITEM? PRESS SELL AGAIN.");
                return;
            }
            int val = eq.CalculateSellValue();
            inventory.RemoveEquipment(eq.instanceId);
            if (Run != null) { if (InDungeon) Run.Gold += val; else Run.HubGold += val; }
            confirmSellActive = false;
            Sfx.Play(Sound.Coin);
            Say($"SOLD {eq.Definition?.name} FOR {val} GOLD.");
        }

        void OnDiscardClicked()
        {
            if (currentCategory == InventoryCategory.KeyItems) { Say("KEY ITEMS CANNOT BE DISCARDED."); return; }
            if (currentCategory == InventoryCategory.Equipment)
            {
                var eq = GetSelectedEquipment(); if (eq == null) return;
                if (eq.isEquipped) { Sfx.Play(Sound.Deny); Say("CANNOT DISCARD EQUIPPED ITEM. UNEQUIP IT FIRST."); return; }
                FigmaUi.ShowConfirmationModal(UiRoot, "DISCARD EQUIPMENT",
                    $"Permanently discard {eq.Definition?.name ?? "item"}?\nThis cannot be undone.",
                    "DISCARD",
                    () =>
                    {
                        inventory.RemoveEquipment(eq.instanceId);
                        Say($"DISCARDED {eq.Definition?.name}.");
                    });
            }
            else if (currentCategory == InventoryCategory.Consumables && SelectedStackIndex < inventory.consumables.Count)
            {
                var c = inventory.consumables[SelectedStackIndex];
                inventory.RemoveConsumable(c.itemId, 1);
                Say($"DISCARDED 1 {c.Definition?.name ?? c.itemId}.");
            }
            else if (currentCategory == InventoryCategory.Materials)
            {
                var mats = inventory.materials.ToList();
                if (SelectedStackIndex < mats.Count)
                {
                    var m = mats[SelectedStackIndex];
                    inventory.RemoveMaterial(m.Key, 1);
                    Say($"DISCARDED 1 {m.Key.ToUpperInvariant()}.");
                }
            }
        }

        void OnUseClicked()
        {
            if (currentCategory != InventoryCategory.Consumables) return;
            if (SelectedStackIndex >= inventory.consumables.Count || party == null || party.Count == 0) return;
            var c = inventory.consumables[SelectedStackIndex]; var itemDef = c.Definition; if (itemDef == null) return;

            OpenHeroTargetModal($"Use {itemDef.name}", u =>
            {
                if (itemDef.isRevive) return !u.Alive ? "FAINTED · REVIVE" : "ALREADY ALIVE";
                if (!u.Alive) return "FAINTED";
                bool needsHp = itemDef.heal > 0 && u.hp < u.stats.maxHp;
                bool needsMp = itemDef.mana > 0 && u.mp < u.stats.maxMp;
                if (!needsHp && !needsMp && (itemDef.heal > 0 || itemDef.mana > 0)) return "FULL HP/MP";
                return $"HP {u.hp}/{u.stats.maxHp} · MP {u.mp}/{u.stats.maxMp}";
            }, u =>
            {
                if (itemDef.isRevive) return !u.Alive;
                if (!u.Alive) return false;
                if (itemDef.heal > 0 && u.hp < u.stats.maxHp) return true;
                if (itemDef.mana > 0 && u.mp < u.stats.maxMp) return true;
                return itemDef.heal <= 0 && itemDef.mana <= 0;
            }, target =>
            {
                if (itemDef.isRevive)
                {
                    float pct = itemDef.reviveHpPercent > 0 ? itemDef.reviveHpPercent : 0.30f;
                    target.hp = Math.Max(1, (int)Math.Round(target.stats.maxHp * pct));
                    target.RecalculateStats();
                    inventory.RemoveConsumable(c.itemId, 1); Game.I?.Run?.Items?.Remove(c.itemId);
                    Sfx.Play(Sound.Heal);
                    Say($"REVIVED {target.name} WITH {target.hp} HP!");
                    return;
                }
                if (itemDef.heal > 0) target.Heal(itemDef.heal);
                if (itemDef.mana > 0) target.mp = Math.Min(target.stats.maxMp, target.mp + itemDef.mana);
                inventory.RemoveConsumable(c.itemId, 1); Game.I?.Run?.Items?.Remove(c.itemId);
                Sfx.Play(Sound.Heal);
                Say($"USED {itemDef.name} ON {target.name}.");
            });
        }

        void OpenHeroTargetModal(string title, Func<Unit, string> subOf, Func<Unit, bool> canUseOn, Action<Unit> onPick)
        {
            if (UiRoot == null || party == null || party.Count == 0) return;
            Sfx.Play(Sound.ModalOpen);
            var backdrop = new VisualElement { name = "InventoryHeroPickerModal" };
            backdrop.AddToClassList("uitk-modal-backdrop");
            backdrop.pickingMode = PickingMode.Position;

            var panel = new VisualElement();
            panel.AddToClassList("uitk-modal-panel");
            panel.style.width = 520;

            var tLbl = new UiLabel(title.ToUpperInvariant());
            tLbl.AddToClassList("uitk-modal-title");
            panel.Add(tLbl);
            var div = new VisualElement();
            div.AddToClassList("uitk-modal-divider");
            panel.Add(div);

            var modalGo = new GameObject("InventoryHeroPickerMenu");
            var modalMenu = modalGo.AddComponent<UitkMenu>();
            modalMenu.AttachRoot(backdrop);
            bool closed = false;
            void Close()
            {
                if (closed) return;
                closed = true;
                backdrop.RemoveFromHierarchy();
                if (modalGo == null) return;
                if (Application.isPlaying) UnityEngine.Object.Destroy(modalGo);
                else UnityEngine.Object.DestroyImmediate(modalGo);
            }
            backdrop.RegisterCallback<DetachFromPanelEvent>(_ => Close());
            modalMenu.onBack = Close;

            foreach (var u in party)
            {
                var unit = u;
                bool ok = canUseOn(unit);
                var row = FigmaUi.CreateFlatRow($"{unit.name}  ({subOf(unit)})", ok ? "SELECT" : "NO", ok ? "uitk-badge-ok" : "uitk-badge-err", 464f);
                row.style.marginBottom = 6;
                panel.Add(row);
                FigmaUi.ApplyDefaultFocusVisual(modalMenu.Add(row, unit.name, () => { Close(); onPick(unit); }, interactable: ok), row);
            }

            var cancelBtn = FigmaUi.CreateButton("CANCEL", "button-slab", 160f);
            cancelBtn.style.marginTop = 10;
            cancelBtn.style.alignSelf = UnityEngine.UIElements.Align.Center;
            panel.Add(cancelBtn);
            FigmaUi.ApplyDefaultFocusVisual(modalMenu.Add(cancelBtn, "CANCEL", Close), cancelBtn);

            backdrop.Add(panel);
            UiRoot.Add(backdrop);
        }
    }
}
