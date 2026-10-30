using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Inventory Screen conforming to UI/UX Overhaul Specification (Section 4.2)
    // 640x360 Reference Resolution, 4 categories, 100 equipment capacity, rarity color-coding, sorting & filtering.
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
        const int ItemsPerPage = 10;

        Label headerTitle;
        Label capacityLabel;
        Label pageIndicator;
        Label feedbackLabel;

        // Details panel labels
        Label detailName;
        Label detailTypeRarity;
        Label detailBaseStats;
        Label detailAffixes;
        Label detailSpecialEffect;
        Label detailRestrictions;
        Label detailValue;

        readonly List<Selectable> itemSlotButtons = new List<Selectable>();
        Selectable tabEquipBtn;
        Selectable tabConsumableBtn;
        Selectable tabMaterialBtn;
        Selectable tabKeyBtn;
        Selectable sortBtn;
        Selectable filterBtn;
        Selectable actionEquipBtn;
        Selectable actionUpgradeBtn;
        Selectable actionSellBtn;
        Selectable actionDiscardBtn;
        Selectable actionUseBtn;

        bool confirmSellActive = false;

        public InventoryScreen() : this(null, null, null) { }

        public InventoryScreen(InventoryRuntime inv = null, IEnumerable<Unit> partyUnits = null, GameState? backTo = null)
        {
            inventory = inv ?? (Game.I?.Run?.Inventory ?? new InventoryRuntime());
            if (partyUnits != null)
                party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null)
                party = Game.I.Run.Party;
            else if (Game.I?.Roster != null)
                party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else
                party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            returnState = backTo ?? (Game.I?.Run != null ? GameState.Map : GameState.MainMenu);
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // 1. Top bar: Title, Category Tabs, Capacity
            Ui.Panel(Root, 0, 0, Ui.Width, 26, Palette.DeepCharcoal, Palette.AntiqueGold);
            headerTitle = Ui.Label(Root, "INVENTORY & SATCHEL", 12, 7, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);

            tabEquipBtn = Ui.Button(Root, "EQUIPMENT", 170, 4, 82, () => SelectCategory(InventoryCategory.Equipment), 18, showCursor: false);
            tabConsumableBtn = Ui.Button(Root, "CONSUMABLES", 256, 4, 94, () => SelectCategory(InventoryCategory.Consumables), 18, showCursor: false);
            tabMaterialBtn = Ui.Button(Root, "MATERIALS", 354, 4, 84, () => SelectCategory(InventoryCategory.Materials), 18, showCursor: false);
            tabKeyBtn = Ui.Button(Root, "KEY ITEMS", 442, 4, 80, () => SelectCategory(InventoryCategory.KeyItems), 18, showCursor: false);

            capacityLabel = Ui.Label(Root, "", 628, 7, Palette.WarmIvory, Align.Right);

            // 2. Left Panel (350x296): Item list, Pagination, Sorting & Filtering
            Ui.Panel(Root, 8, 28, 350, 296, Palette.DeepCharcoal, Palette.DarkBorder);
            sortBtn = Ui.Button(Root, "SORT: RARITY", 16, 32, 160, CycleSortMode, 18, showCursor: false);
            filterBtn = Ui.Button(Root, "FILTER: ALL", 182, 32, 168, CycleFilter, 18, showCursor: false);
            Ui.Divider(Root, 16, 52, 334, Palette.DarkBorder, Palette.AntiqueGoldDim);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);
            menu.Add(tabEquipBtn);
            menu.Add(tabConsumableBtn);
            menu.Add(tabMaterialBtn);
            menu.Add(tabKeyBtn);
            menu.Add(sortBtn);
            menu.Add(filterBtn);

            itemSlotButtons.Clear();
            for (int i = 0; i < ItemsPerPage; i++)
            {
                int slotIdx = i;
                int y = 56 + i * 22;
                var btn = Ui.Button(Root, "", 16, y, 334, () => SelectItem(slotIdx), 19);
                itemSlotButtons.Add(btn);
                menu.Add(btn);
            }

            Ui.Divider(Root, 16, 280, 334, Palette.DarkBorder, Palette.AntiqueGoldDim);
            var prevPageBtn = Ui.Button(Root, "< PREV", 16, 288, 80, () => ChangePage(-1), 18, showCursor: false);
            pageIndicator = Ui.Label(Root, "PAGE 1 / 1", 183, 292, Palette.MutedGray, Align.Center);
            var nextPageBtn = Ui.Button(Root, "NEXT >", 270, 288, 80, () => ChangePage(1), 18, showCursor: false);
            menu.Add(prevPageBtn);
            menu.Add(nextPageBtn);

            // 3. Right Panel (270x296): Selected Item Details & Lore
            Ui.Panel(Root, 362, 28, 270, 296, Palette.DeepCharcoal, Palette.DarkBorder);
            detailName = Ui.Label(Root, "", 372, 33, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            detailTypeRarity = Ui.Label(Root, "", 372, 47, Palette.MutedGray);
            Ui.Divider(Root, 372, 59, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);

            detailBaseStats = Ui.Label(Root, "", 372, 65, Palette.WarmIvory);
            Ui.Divider(Root, 372, 108, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);

            Ui.Label(Root, "AFFIXES & ATTRIBUTES", 372, 114, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            detailAffixes = Ui.Label(Root, "", 372, 128, Palette.ArcaneViolet);

            Ui.Divider(Root, 372, 182, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);
            detailSpecialEffect = Ui.Label(Root, "", 372, 188, Palette.AntiqueGoldBright);
            detailRestrictions = Ui.Label(Root, "", 372, 248, Palette.MutedGray);

            Ui.Divider(Root, 372, 266, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);
            detailValue = Ui.Label(Root, "", 372, 274, Palette.AntiqueGoldBright);

            // 4. Bottom Panel (640x32): Actions & Feedback
            Ui.Panel(Root, 0, 328, Ui.Width, 32, Palette.DeepCharcoal, Palette.AntiqueGold);
            feedbackLabel = Ui.Label(Root, "", 14, 333, Palette.WarmIvory);

            actionEquipBtn = Ui.Button(Root, "EQUIP", 16, 336, 64, OnEquipClicked, 18, showCursor: false);
            actionUpgradeBtn = Ui.Button(Root, "UPGRADE", 84, 336, 76, OnUpgradeClicked, 18, showCursor: false);
            actionSellBtn = Ui.Button(Root, "SELL", 164, 336, 56, OnSellClicked, 18, showCursor: false);
            actionDiscardBtn = Ui.Button(Root, "DISCARD", 224, 336, 72, OnDiscardClicked, 18, showCursor: false);
            actionUseBtn = Ui.Button(Root, "USE", 300, 336, 52, OnUseClicked, 18, showCursor: false);
            var backBtn = Ui.Button(Root, "BACK", 564, 336, 64, () => Game.Go(returnState), 18, showCursor: false);

            menu.Add(actionEquipBtn);
            menu.Add(actionUpgradeBtn);
            menu.Add(actionSellBtn);
            menu.Add(actionDiscardBtn);
            menu.Add(actionUseBtn);
            menu.Add(backBtn);

            Refresh();
        }

        void SelectCategory(InventoryCategory cat)
        {
            currentCategory = cat;
            selectedIndex = 0;
            pageIndex = 0;
            confirmSellActive = false;
            Refresh();
        }

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
            sortBtn?.label?.Set(PixelFont.Fit($"SORT: {currentSortMode.ToString().ToUpperInvariant()}", 150));
            Refresh();
        }

        void CycleFilter()
        {
            currentSlotFilter = currentSlotFilter switch
            {
                null => EquipmentSlot.MainHand,
                EquipmentSlot.MainHand => EquipmentSlot.HeadArmor,
                EquipmentSlot.HeadArmor => EquipmentSlot.BodyArmor,
                EquipmentSlot.BodyArmor => EquipmentSlot.Accessory1,
                EquipmentSlot.Accessory1 => EquipmentSlot.Accessory2,
                _ => null
            };
            string fltText = currentSlotFilter.HasValue ? EquipmentSlots.DisplayName(currentSlotFilter.Value) : "ALL";
            filterBtn?.label?.Set(PixelFont.Fit($"FILTER: {fltText.ToUpperInvariant()}", 158));
            Refresh();
        }

        void ChangePage(int delta)
        {
            pageIndex = Math.Max(0, pageIndex + delta);
            selectedIndex = 0;
            Refresh();
        }

        void SelectItem(int slotIndex)
        {
            selectedIndex = slotIndex;
            confirmSellActive = false;
            Refresh();
        }

        List<EquipmentInstance> GetCurrentEquipmentList() =>
            inventory.GetSortedEquipment(currentSortMode, currentSlotFilter);

        void Refresh()
        {
            confirmSellActive = false;
            feedbackLabel?.Set("");

            // Update Tab Highlights
            tabEquipBtn.label.Color = currentCategory == InventoryCategory.Equipment ? Palette.AntiqueGoldBright : Palette.MutedGray;
            tabConsumableBtn.label.Color = currentCategory == InventoryCategory.Consumables ? Palette.AntiqueGoldBright : Palette.MutedGray;
            tabMaterialBtn.label.Color = currentCategory == InventoryCategory.Materials ? Palette.AntiqueGoldBright : Palette.MutedGray;
            tabKeyBtn.label.Color = currentCategory == InventoryCategory.KeyItems ? Palette.AntiqueGoldBright : Palette.MutedGray;

            int eqCount = inventory.EquipmentCount;
            capacityLabel?.Set($"GEAR: {eqCount}/{InventoryRuntime.MaxEquipmentCapacity}");
            capacityLabel.Color = inventory.IsEquipmentFull ? Palette.SpecCrimson : Palette.WarmIvory;

            // Clear item slot buttons
            foreach (var b in itemSlotButtons)
            {
                b.label.Set("");
                b.label.Color = Palette.WarmIvory;
            }

            if (currentCategory == InventoryCategory.Equipment)
            {
                var list = GetCurrentEquipmentList();
                int totalPages = Math.Max(1, (list.Count + ItemsPerPage - 1) / ItemsPerPage);
                if (pageIndex >= totalPages) pageIndex = totalPages - 1;
                pageIndicator?.Set($"PAGE {pageIndex + 1} / {totalPages}");

                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        var eq = list[itemIdx];
                        string eqName = eq.upgradeLevel > 0 ? $"{eq.Definition?.name} +{eq.upgradeLevel}" : eq.Definition?.name ?? "Equipment";
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{eqName}".ToUpperInvariant(), 320));
                        itemSlotButtons[i].label.Color = EquipmentRarities.GetColor(eq.rarity);
                    }
                }

                int selIdx = start + selectedIndex;
                if (selIdx < list.Count)
                    DisplayEquipmentDetails(list[selIdx]);
                else
                    ClearDetails();

                actionEquipBtn.label.Color = Palette.WarmIvory;
                actionUpgradeBtn.label.Color = Palette.WarmIvory;
                actionSellBtn.label.Color = Palette.WarmIvory;
                actionDiscardBtn.label.Color = Palette.WarmIvory;
                actionUseBtn.label.Color = Palette.Shade;
            }
            else if (currentCategory == InventoryCategory.Consumables)
            {
                var list = inventory.consumables;
                int totalPages = Math.Max(1, (list.Count + ItemsPerPage - 1) / ItemsPerPage);
                if (pageIndex >= totalPages) pageIndex = totalPages - 1;
                pageIndicator?.Set($"PAGE {pageIndex + 1} / {totalPages}");

                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        var c = list[itemIdx];
                        string name = c.Definition?.name ?? c.itemId;
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{name} x{c.count}".ToUpperInvariant(), 320));
                        itemSlotButtons[i].label.Color = Palette.Emerald;
                    }
                }

                int selIdx = start + selectedIndex;
                if (selIdx < list.Count)
                    DisplayConsumableDetails(list[selIdx]);
                else
                    ClearDetails();

                actionEquipBtn.label.Color = Palette.Shade;
                actionUpgradeBtn.label.Color = Palette.Shade;
                actionSellBtn.label.Color = Palette.Shade;
                actionDiscardBtn.label.Color = Palette.WarmIvory;
                actionUseBtn.label.Color = Palette.Emerald;
            }
            else if (currentCategory == InventoryCategory.Materials)
            {
                var list = inventory.materials.ToList();
                int totalPages = Math.Max(1, (list.Count + ItemsPerPage - 1) / ItemsPerPage);
                if (pageIndex >= totalPages) pageIndex = totalPages - 1;
                pageIndicator?.Set($"PAGE {pageIndex + 1} / {totalPages}");

                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        var m = list[itemIdx];
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{m.Key} x{m.Value}".ToUpperInvariant(), 320));
                        itemSlotButtons[i].label.Color = Palette.WarmIvory;
                    }
                }

                int selIdx = start + selectedIndex;
                if (selIdx < list.Count)
                    DisplayMaterialDetails(list[selIdx].Key, list[selIdx].Value);
                else
                    ClearDetails();

                actionEquipBtn.label.Color = Palette.Shade;
                actionUpgradeBtn.label.Color = Palette.Shade;
                actionSellBtn.label.Color = Palette.Shade;
                actionDiscardBtn.label.Color = Palette.WarmIvory;
                actionUseBtn.label.Color = Palette.Shade;
            }
            else // KeyItems
            {
                var list = inventory.keyItems.ToList();
                int totalPages = Math.Max(1, (list.Count + ItemsPerPage - 1) / ItemsPerPage);
                if (pageIndex >= totalPages) pageIndex = totalPages - 1;
                pageIndicator?.Set($"PAGE {pageIndex + 1} / {totalPages}");

                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        string k = list[itemIdx];
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{k}".ToUpperInvariant(), 320));
                        itemSlotButtons[i].label.Color = Palette.AntiqueGoldBright;
                    }
                }

                int selIdx = start + selectedIndex;
                if (selIdx < list.Count)
                    DisplayKeyItemDetails(list[selIdx]);
                else
                    ClearDetails();

                actionEquipBtn.label.Color = Palette.Shade;
                actionUpgradeBtn.label.Color = Palette.Shade;
                actionSellBtn.label.Color = Palette.Shade;
                actionDiscardBtn.label.Color = Palette.Shade;
                actionUseBtn.label.Color = Palette.Shade;
            }
        }

        void ClearDetails()
        {
            detailName?.Set("");
            detailTypeRarity?.Set("");
            detailBaseStats?.Set("");
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set("");
        }

        void DisplayEquipmentDetails(EquipmentInstance eq)
        {
            var def = eq.Definition;
            string eqName = eq.upgradeLevel > 0 ? $"{def?.name} +{eq.upgradeLevel}" : def?.name ?? "Equipment";
            detailName?.Set(PixelFont.Fit(eqName.ToUpperInvariant(), 250));
            detailName.Color = EquipmentRarities.GetColor(eq.rarity);

            string slotName = def != null ? EquipmentSlots.DisplayName(def.slot) : "UNKNOWN";
            detailTypeRarity?.Set(PixelFont.Fit($"{eq.rarity.ToString().ToUpperInvariant()}  •  {slotName.ToUpperInvariant()}  (LVL {eq.level})", 250));
            detailTypeRarity.Color = Palette.MutedGray;

            // Base Stats
            var statParts = new List<string>();
            if (eq.EffectiveWeaponPower > 0) statParts.Add($"ATK +{eq.EffectiveWeaponPower}");
            if (eq.EffectivePhysicalDefense > 0) statParts.Add($"DEF +{eq.EffectivePhysicalDefense}");
            if (eq.EffectiveMagicDefense > 0) statParts.Add($"MDEF +{eq.EffectiveMagicDefense}");
            if (eq.EffectiveBreakPowerModifier > 0) statParts.Add($"BRK +{eq.EffectiveBreakPowerModifier}");
            detailBaseStats?.Set(PixelFont.Fit(statParts.Count > 0 ? string.Join("   ", statParts) : "NO BASE STATS", 250));

            // Affixes
            if (eq.affixes != null && eq.affixes.Count > 0)
            {
                var affList = eq.affixes.Select(a => $"• {a.GetDescription()}");
                detailAffixes?.Set(PixelFont.Wrap(string.Join("\n", affList).ToUpperInvariant(), 250));
            }
            else
            {
                detailAffixes?.Set("NO AFFIXES");
            }

            // Special Effect
            var se = eq.SpecialEffect;
            if (se != null)
            {
                detailSpecialEffect?.Set(PixelFont.Wrap($"★ {se.name}: {se.description}".ToUpperInvariant(), 250));
                detailSpecialEffect.Color = eq.rarity == EquipmentRarity.Mythic ? Palette.SpecCrimson : Palette.AntiqueGold;
            }
            else
            {
                detailSpecialEffect?.Set("");
            }

            // Restrictions
            var r = def?.restrictions;
            if (r != null && r.allowedClasses != null && r.allowedClasses.Length > 0)
                detailRestrictions?.Set(PixelFont.Fit($"CLASS: {string.Join("/", r.allowedClasses).ToUpperInvariant()}", 250));
            else
                detailRestrictions?.Set("ALL CLASSES COMPATIBLE");

            detailValue?.Set($"SELL VALUE: {eq.CalculateSellValue()}G   UPGRADE: {eq.GetUpgradeCostGold()}G");
        }

        void DisplayConsumableDetails(ConsumableStack c)
        {
            var def = c.Definition;
            detailName?.Set(PixelFont.Fit(def?.name ?? c.itemId, 250));
            detailName.Color = Palette.Emerald;
            detailTypeRarity?.Set($"CONSUMABLE  •  STACK: {c.count}/{c.maxStack}");
            detailBaseStats?.Set(PixelFont.Wrap(def?.description ?? "Consumable recovery or buff item.", 250));
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set($"SELL VALUE: 10G");
        }

        void DisplayMaterialDetails(string materialId, int count)
        {
            detailName?.Set(PixelFont.Fit(materialId.ToUpperInvariant(), 250));
            detailName.Color = Palette.WarmIvory;
            detailTypeRarity?.Set($"MATERIAL  •  QUANTITY: {count}");
            detailBaseStats?.Set("Upgrade material used at the Hub Blacksmith to hone equipment.");
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set("USED FOR EQUIPMENT UPGRADES");
        }

        void DisplayKeyItemDetails(string keyId)
        {
            detailName?.Set(PixelFont.Fit(keyId.ToUpperInvariant(), 250));
            detailName.Color = Palette.AntiqueGoldBright;
            detailTypeRarity?.Set("KEY ITEM  •  BOUND TO HEROES");
            detailBaseStats?.Set("Unique progression item for dungeon and story exploration.");
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set("CANNOT BE SOLD");
        }

        EquipmentInstance GetSelectedEquipment()
        {
            var list = GetCurrentEquipmentList();
            int idx = pageIndex * ItemsPerPage + selectedIndex;
            return idx < list.Count ? list[idx] : null;
        }

        void OnEquipClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment();
            if (eq == null || party == null || party.Count == 0) return;

            var targetHero = party.FirstOrDefault(u => EquipmentValidator.Validate(eq.Definition, eq.Definition.slot, u.classDef, u.race, u.level, u.FinalScores) == null) ?? party[0];
            if (EquipmentManager.EquipFromInventory(targetHero, eq.Definition.slot, eq, inventory, out string err))
            {
                feedbackLabel?.Set(PixelFont.Fit($"EQUIPPED {eq.Definition?.name} ON {targetHero.name.ToUpperInvariant()}", 610));
                Refresh();
            }
            else
            {
                feedbackLabel?.Set(PixelFont.Fit($"CANNOT EQUIP: {err.ToUpperInvariant()}", 610));
            }
        }

        void OnUpgradeClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment();
            if (eq == null) return;

            int goldCost = eq.GetUpgradeCostGold();
            int matCost = eq.GetUpgradeCostMaterials();
            int playerGold = Game.I?.Run?.Gold ?? 9999;
            int playerMat = inventory.GetMaterialCount("iron_ingot");

            if (!eq.CanUpgrade(out string reason))
            {
                feedbackLabel?.Set(PixelFont.Fit(reason.ToUpperInvariant(), 610));
                return;
            }

            if (Game.I?.Run != null && Game.I.Run.Gold < goldCost)
            {
                feedbackLabel?.Set(PixelFont.Fit($"NEED {goldCost}G TO UPGRADE (HAVE {playerGold}G)", 610));
                return;
            }

            if (inventory.GetMaterialCount("iron_ingot") < matCost)
            {
                if (playerMat < matCost && inventory.materials.Count > 0)
                {
                    feedbackLabel?.Set(PixelFont.Fit($"NEED {matCost} UPGRADE MATERIALS (HAVE {playerMat})", 610));
                    return;
                }
            }

            if (Game.I?.Run != null) Game.I.Run.Gold -= goldCost;
            inventory.RemoveMaterial("iron_ingot", matCost);
            eq.TryUpgrade(out _);
            feedbackLabel?.Set(PixelFont.Fit($"UPGRADED {eq.Definition?.name} TO +{eq.upgradeLevel}!", 610));
            Refresh();
        }

        void OnSellClicked()
        {
            if (currentCategory != InventoryCategory.Equipment && currentCategory != InventoryCategory.Consumables) return;
            if (currentCategory == InventoryCategory.Equipment)
            {
                var eq = GetSelectedEquipment();
                if (eq == null) return;

                if (EquipmentRarities.HasSpecialEffect(eq.rarity) && !confirmSellActive)
                {
                    confirmSellActive = true;
                    feedbackLabel?.Set(PixelFont.Fit($"SELL {eq.rarity.ToString().ToUpperInvariant()} ITEM? CLICK SELL AGAIN TO CONFIRM.", 610));
                    actionSellBtn.label.Color = Palette.SpecCrimson;
                    return;
                }

                int val = eq.CalculateSellValue();
                inventory.RemoveEquipment(eq.instanceId);
                if (Game.I?.Run != null) Game.I.Run.Gold += val;
                feedbackLabel?.Set(PixelFont.Fit($"SOLD {eq.Definition?.name} FOR {val} GOLD.", 610));
                confirmSellActive = false;
                Refresh();
            }
        }

        void OnDiscardClicked()
        {
            if (currentCategory == InventoryCategory.KeyItems)
            {
                feedbackLabel?.Set("KEY ITEMS CANNOT BE DISCARDED.");
                return;
            }
            if (currentCategory == InventoryCategory.Equipment)
            {
                var eq = GetSelectedEquipment();
                if (eq == null) return;
                inventory.RemoveEquipment(eq.instanceId);
                feedbackLabel?.Set(PixelFont.Fit($"DISCARDED {eq.Definition?.name}.", 610));
                Refresh();
            }
            else if (currentCategory == InventoryCategory.Consumables)
            {
                if (selectedIndex < inventory.consumables.Count)
                {
                    var c = inventory.consumables[selectedIndex];
                    inventory.RemoveConsumable(c.itemId, 1);
                    feedbackLabel?.Set($"DISCARDED 1 {c.Definition?.name ?? c.itemId}.");
                    Refresh();
                }
            }
        }

        void OnUseClicked()
        {
            if (currentCategory != InventoryCategory.Consumables) return;
            if (selectedIndex < inventory.consumables.Count && party != null && party.Count > 0)
            {
                var c = inventory.consumables[selectedIndex];
                var target = party.FirstOrDefault(u => u.Alive && (u.hp < u.stats.maxHp || u.mp < u.stats.maxMp)) ?? party[0];
                var itemDef = c.Definition;
                if (itemDef != null)
                {
                    if (itemDef.heal > 0) target.Heal(itemDef.heal);
                    if (itemDef.mana > 0) target.mp = Math.Min(target.stats.maxMp, target.mp + itemDef.mana);
                    inventory.RemoveConsumable(c.itemId, 1);
                    feedbackLabel?.Set(PixelFont.Fit($"USED {itemDef.name} ON {target.name.ToUpperInvariant()}.", 610));
                    Refresh();
                }
            }
        }
    }
}
