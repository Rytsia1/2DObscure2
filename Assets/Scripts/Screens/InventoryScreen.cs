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
            UiRoot = FigmaUi.Mount(Root.gameObject, "14---Inventory");
            if (menu != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(menu);
                else UnityEngine.Object.DestroyImmediate(menu);
            }
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Go(returnState);

            var backBtn = FigmaUi.FindButton(UiRoot, "Back");
            if (backBtn != null) FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", () => Game?.Go(returnState)));

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
                var btn = FigmaUi.CreateButton($"{name} {CategoryCount(c)}", "button-slab", null);
                btn.style.height = 38;
                btn.style.paddingLeft = 14;
                btn.style.paddingRight = 14;
                btn.style.marginRight = 4;
                btn.style.fontSize = 18;
                btn.style.borderTopWidth = active ? 3 : 2;
                btn.style.borderBottomWidth = active ? 0 : 2;
                tabRow.Add(btn);
                var it = menu.Add(btn, name, () => SelectCategory(c), selected: active);
                it.visual = on =>
                {
                    btn.style.backgroundColor = on || active ? Palette.Ink3 : Palette.Ink2;
                    btn.style.borderTopColor = on ? Palette.Gold1 : active ? Palette.Gold2 : Palette.Ink4;
                    var side = on ? Palette.Gold1 : active ? Palette.Gold3 : Palette.Ink4;
                    btn.style.borderLeftColor = side; btn.style.borderRightColor = side;
                    btn.style.borderBottomColor = active ? Palette.Ink3 : side;
                    btn.style.color = on || active ? Palette.Gold1 : Palette.Mist2;
                };
                it.visual(false);
            }
            screen.Add(tabRow);

            // Sort / Filter bar
            var leftHost = new VisualElement { style = { position = Position.Absolute, left = 44, top = 122, width = 798, height = 560, flexDirection = FlexDirection.Column } };
            screen.Add(leftHost);

            var topCtrl = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12 } };
            var sortBtn = FigmaUi.CreateButton($"SORT: {currentSortMode.ToString().ToUpperInvariant()}", "button-slab", 270f);
            sortBtn.style.marginRight = 10;
            string fltName = currentSlotFilter.HasValue ? EquipmentSlots.DisplayName(currentSlotFilter.Value).ToUpperInvariant() : "ALL";
            var filterBtn = FigmaUi.CreateButton($"FILTER: {fltName}", "button-slab", 270f);
            topCtrl.Add(sortBtn);
            topCtrl.Add(filterBtn);
            leftHost.Add(topCtrl);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(sortBtn, "Sort", CycleSortMode));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(filterBtn, "Filter", CycleFilter));

            // 10x4 Item Grid
            int total = CategoryCount(currentCategory);
            int totalPages = Math.Max(1, (total + ItemsPerPage - 1) / ItemsPerPage);
            if (pageIndex >= totalPages) pageIndex = totalPages - 1;

            var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, width = 680, height = 270 } };
            for (int i = 0; i < ItemsPerPage; i++)
            {
                int slotIdx = i;
                int itemIdx = pageIndex * ItemsPerPage + i;
                var info = CellContent(itemIdx);
                bool has = info.has, sel = has && i == selectedIndex;
                var cell = FigmaUi.InstantiateTemplate(sel ? "Item-Slot-Selected" : has ? "Item-Slot-Filled" : "Item-Slot-Empty") ?? new VisualElement();
                cell.style.width = 56;
                cell.style.height = 56;
                cell.style.marginRight = 8;
                cell.style.marginBottom = 8;
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
                    var it = menu.Add(cell, info.title, () => SelectItem(slotIdx), selected: sel);
                    it.visual = on =>
                    {
                        var border = on ? Palette.Gold1 : sel ? Palette.Gold2 : (info.tint ?? Palette.Ink4);
                        cell.style.borderTopColor = border; cell.style.borderBottomColor = border;
                        cell.style.borderLeftColor = border; cell.style.borderRightColor = border;
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
            leftHost.Add(new UiLabel("CONSUMABLES AND MATERIALS STACK — ONE SLOT PER ITEM TYPE.") { style = { fontSize = 13, color = Palette.Mist2 } });

            BuildDetailPanel(screen, pageIndex * ItemsPerPage + selectedIndex);
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
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column } };
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

            if (dName != "")
            {
                host.Add(new UiLabel(dName) { style = { fontSize = 20, color = nameCol, whiteSpace = WhiteSpace.Normal, marginTop = 14 } });
                host.Add(new UiLabel(dSub) { style = { fontSize = 12, color = Palette.Mist3, marginBottom = 10 } });
                foreach (var (k, v) in stats)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, height = 26, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 } };
                    row.Add(new UiLabel(k) { style = { fontSize = 13, color = Palette.Mist2 } });
                    row.Add(new UiLabel(v) { style = { fontSize = 13, color = Palette.Mist1 } });
                    host.Add(row);
                }
                if (dAffix != "") host.Add(new UiLabel(dAffix) { style = { fontSize = 13, color = Palette.Xp, whiteSpace = WhiteSpace.Normal, marginTop = 12 } });
                if (dSpec != "") host.Add(new UiLabel(dSpec) { style = { fontSize = 13, color = Palette.Gold2, whiteSpace = WhiteSpace.Normal, marginTop = 6 } });
                if (dRest != "") host.Add(new UiLabel(dRest) { style = { fontSize = 12, color = Palette.Mist3, whiteSpace = WhiteSpace.Normal, marginTop = 8 } });
                if (dVal != "") host.Add(new UiLabel(dVal) { style = { fontSize = 13, color = Palette.Gold1, marginTop = 6 } });
            }
            else
            {
                host.Add(new UiLabel("SELECT AN ITEM.") { style = { fontSize = 14, color = Palette.Mist3, marginTop = 20 } });
            }

            host.Add(new VisualElement { style = { flexGrow = 1 } });

            bool isEq = currentCategory == InventoryCategory.Equipment;
            var r1 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
            var eqBtn = FigmaUi.CreateButton("Equip", "button-slab", 110f); eqBtn.SetEnabled(isEq);
            eqBtn.style.paddingLeft = 8; eqBtn.style.paddingRight = 8;
            var upBtn = FigmaUi.CreateButton("Upgrade", "button-slab", 110f); upBtn.SetEnabled(isEq);
            upBtn.style.paddingLeft = 8; upBtn.style.paddingRight = 8;
            var sellBtn = FigmaUi.CreateButton(confirmSellActive ? "Confirm?" : "Sell", confirmSellActive ? "button-danger" : "button-slab", 110f); sellBtn.SetEnabled(isEq);
            sellBtn.style.paddingLeft = 8; sellBtn.style.paddingRight = 8;
            r1.Add(eqBtn); r1.Add(upBtn); r1.Add(sellBtn);
            host.Add(r1);

            var r2 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var disBtn = FigmaUi.CreateButton("Discard", "button-danger", 160f); disBtn.SetEnabled(currentCategory != InventoryCategory.KeyItems);
            var useBtn = FigmaUi.CreateButton("Use on...", currentCategory == InventoryCategory.Consumables ? "button-primary" : "button-slab", 174f); useBtn.SetEnabled(currentCategory == InventoryCategory.Consumables);
            r2.Add(disBtn); r2.Add(useBtn);
            host.Add(r2);

            FigmaUi.ApplyDefaultFocusVisual(menu.Add(eqBtn, "Equip", OnEquipClicked, interactable: isEq));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(upBtn, "Upgrade", OnUpgradeClicked, interactable: isEq));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(sellBtn, "Sell", OnSellClicked, interactable: isEq));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(disBtn, "Discard", OnDiscardClicked, interactable: currentCategory != InventoryCategory.KeyItems));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(useBtn, "Use on...", OnUseClicked, interactable: currentCategory == InventoryCategory.Consumables));
        }

        void SelectCategory(InventoryCategory cat) { currentCategory = cat; selectedIndex = 0; pageIndex = 0; confirmSellActive = false; Refresh(); }

        void CycleSortMode()
        {
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

        void ChangePage(int delta) { pageIndex = Math.Max(0, pageIndex + delta); selectedIndex = 0; Refresh(); }
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
            int goldCost = eq.GetUpgradeCostGold(), matCost = eq.GetUpgradeCostMaterials(), playerGold = Game.I?.Run?.Gold ?? 9999, playerMat = inventory.GetMaterialCount("iron_ingot");
            if (!eq.CanUpgrade(out string reason)) { Sfx.Play(Sound.Deny); Say(reason); return; }
            if (Game.I?.Run != null && Game.I.Run.Gold < goldCost) { Sfx.Play(Sound.Deny); Say($"NEED {goldCost}G TO UPGRADE (HAVE {playerGold}G)"); return; }
            if (playerMat < matCost && inventory.materials.Count > 0) { Sfx.Play(Sound.Deny); Say($"NEED {matCost} UPGRADE MATERIALS (HAVE {playerMat})"); return; }
            if (Game.I?.Run != null) Game.I.Run.Gold -= goldCost;
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
            if (Game.I?.Run != null) Game.I.Run.Gold += val;
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
                if (eq.isEquipped) { Say("CANNOT DISCARD EQUIPPED ITEM. UNEQUIP IT FIRST."); return; }
                inventory.RemoveEquipment(eq.instanceId);
                Say($"DISCARDED {eq.Definition?.name}.");
            }
            else if (currentCategory == InventoryCategory.Consumables && SelectedStackIndex < inventory.consumables.Count)
            {
                var c = inventory.consumables[SelectedStackIndex];
                inventory.RemoveConsumable(c.itemId, 1);
                Say($"DISCARDED 1 {c.Definition?.name ?? c.itemId}.");
            }
        }

        void OnUseClicked()
        {
            if (currentCategory != InventoryCategory.Consumables) return;
            if (SelectedStackIndex >= inventory.consumables.Count || party == null || party.Count == 0) return;
            var c = inventory.consumables[SelectedStackIndex]; var itemDef = c.Definition; if (itemDef == null) return;
            if (itemDef.isRevive)
            {
                var deadTarget = party.FirstOrDefault(u => !u.Alive);
                if (deadTarget == null) { Say("NO FALLEN ALLY TO REVIVE."); return; }
                float pct = itemDef.reviveHpPercent > 0 ? itemDef.reviveHpPercent : 0.30f;
                deadTarget.hp = Math.Max(1, (int)Math.Round(deadTarget.stats.maxHp * pct));
                deadTarget.RecalculateStats();
                inventory.RemoveConsumable(c.itemId, 1); Game.I?.Run?.Items?.Remove(c.itemId);
                Sfx.Play(Sound.Heal);
                Say($"REVIVED {deadTarget.name} WITH {deadTarget.hp} HP!");
                return;
            }
            var target = party.FirstOrDefault(u => u.Alive && (u.hp < u.stats.maxHp || u.mp < u.stats.maxMp)) ?? party[0];
            if (itemDef.heal > 0) target.Heal(itemDef.heal);
            if (itemDef.mana > 0) target.mp = Math.Min(target.stats.maxMp, target.mp + itemDef.mana);
            inventory.RemoveConsumable(c.itemId, 1); Game.I?.Run?.Items?.Remove(c.itemId);
            Sfx.Play(Sound.Heal);
            Say($"USED {itemDef.name} ON {target.name}.");
        }
    }
}
