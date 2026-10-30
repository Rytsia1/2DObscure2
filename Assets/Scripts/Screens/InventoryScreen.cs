using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Inventory Screen conforming to Inventory & Equipment System GDD v1.0 (Section 12)
    // 320x180 Virtual Pixel Resolution, 4 categories, 100 equipment capacity, rarity color-coding, sorting & filtering.
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
        const int ItemsPerPage = 6;

        Label headerTitle;
        Label capacityLabel;
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
            Backdrop.Create(Root, false, 8, Palette.Void, Palette.Night, Palette.Dusk);

            // 1. Top bar: Title, Category Tabs, Capacity
            Ui.Panel(Root, 0, 0, Ui.Width, 18);
            headerTitle = Ui.Label(Root, "INVENTORY", 6, 5, Palette.Gold);

            tabEquipBtn = Ui.Button(Root, "EQP", 80, 2, 34, () => SelectCategory(InventoryCategory.Equipment), 14, showCursor: false);
            tabConsumableBtn = Ui.Button(Root, "USE", 118, 2, 34, () => SelectCategory(InventoryCategory.Consumables), 14, showCursor: false);
            tabMaterialBtn = Ui.Button(Root, "MAT", 156, 2, 34, () => SelectCategory(InventoryCategory.Materials), 14, showCursor: false);
            tabKeyBtn = Ui.Button(Root, "KEY", 194, 2, 34, () => SelectCategory(InventoryCategory.KeyItems), 14, showCursor: false);

            capacityLabel = Ui.Label(Root, "", 314, 5, Palette.Bone, Align.Right);

            // 2. Left Panel: Item list, Pagination, Sorting & Filtering
            Ui.Panel(Root, 2, 19, 150, 132);
            sortBtn = Ui.Button(Root, "SORT: RAR", 6, 21, 68, CycleSortMode, 11, showCursor: false);
            filterBtn = Ui.Button(Root, "FILTER: ALL", 78, 21, 70, CycleFilter, 11, showCursor: false);

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
                int y = 35 + i * 16;
                var btn = Ui.Button(Root, "", 6, y, 142, () => SelectItem(slotIdx), 14);
                itemSlotButtons.Add(btn);
                menu.Add(btn);
            }

            var prevPageBtn = Ui.Button(Root, "< PREV", 6, 133, 44, () => ChangePage(-1), 14, showCursor: false);
            var nextPageBtn = Ui.Button(Root, "NEXT >", 104, 133, 44, () => ChangePage(1), 14, showCursor: false);
            menu.Add(prevPageBtn);
            menu.Add(nextPageBtn);

            // 3. Right Panel: Selected Item Details
            Ui.Panel(Root, 154, 19, 164, 132);
            detailName = Ui.Label(Root, "", 158, 23, Palette.Gold);
            detailTypeRarity = Ui.Label(Root, "", 158, 33, Palette.Parchment);
            detailBaseStats = Ui.Label(Root, "", 158, 45, Palette.Bone);
            detailAffixes = Ui.Label(Root, "", 158, 65, Palette.Lilac);
            detailSpecialEffect = Ui.Label(Root, "", 158, 97, Palette.Gold);
            detailRestrictions = Ui.Label(Root, "", 158, 117, Palette.Ash);
            detailValue = Ui.Label(Root, "", 158, 135, Palette.Gold);

            // 4. Bottom Panel: Actions & Feedback
            Ui.Panel(Root, 0, 152, Ui.Width, 28);
            feedbackLabel = Ui.Label(Root, "", 6, 154, Palette.Parchment);

            actionEquipBtn = Ui.Button(Root, "EQUIP", 6, 163, 42, OnEquipClicked, 13, showCursor: false);
            actionUpgradeBtn = Ui.Button(Root, "UPGRADE", 52, 163, 52, OnUpgradeClicked, 13, showCursor: false);
            actionSellBtn = Ui.Button(Root, "SELL", 108, 163, 40, OnSellClicked, 13, showCursor: false);
            actionDiscardBtn = Ui.Button(Root, "DISCARD", 152, 163, 50, OnDiscardClicked, 13, showCursor: false);
            actionUseBtn = Ui.Button(Root, "USE", 206, 163, 34, OnUseClicked, 13, showCursor: false);
            var backBtn = Ui.Button(Root, "BACK", 276, 163, 38, () => Game.Go(returnState), 13, showCursor: false);

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
            sortBtn?.label?.Set(PixelFont.Fit($"SORT: {currentSortMode.ToString().ToUpperInvariant().Substring(0, 3)}", 66));
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
            string fltText = currentSlotFilter.HasValue ? EquipmentSlots.ShortName(currentSlotFilter.Value) : "ALL";
            filterBtn?.label?.Set(PixelFont.Fit($"FLT: {fltText}", 68));
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
            tabEquipBtn.label.Color = currentCategory == InventoryCategory.Equipment ? Palette.Gold : Palette.Ash;
            tabConsumableBtn.label.Color = currentCategory == InventoryCategory.Consumables ? Palette.Gold : Palette.Ash;
            tabMaterialBtn.label.Color = currentCategory == InventoryCategory.Materials ? Palette.Gold : Palette.Ash;
            tabKeyBtn.label.Color = currentCategory == InventoryCategory.KeyItems ? Palette.Gold : Palette.Ash;

            int eqCount = inventory.EquipmentCount;
            capacityLabel?.Set($"EQP: {eqCount}/{InventoryRuntime.MaxEquipmentCapacity}");
            capacityLabel.Color = inventory.IsEquipmentFull ? Palette.Crimson : Palette.Bone;

            // Clear item slot buttons
            foreach (var b in itemSlotButtons)
            {
                b.label.Set("");
                b.label.Color = Palette.Bone;
            }

            if (currentCategory == InventoryCategory.Equipment)
            {
                var list = GetCurrentEquipmentList();
                int totalPages = Math.Max(1, (list.Count + ItemsPerPage - 1) / ItemsPerPage);
                if (pageIndex >= totalPages) pageIndex = totalPages - 1;

                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        var eq = list[itemIdx];
                        string eqName = eq.upgradeLevel > 0 ? $"{eq.Definition?.name} +{eq.upgradeLevel}" : eq.Definition?.name ?? "Equipment";
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{eqName}".ToUpperInvariant(), 138));
                        itemSlotButtons[i].label.Color = EquipmentRarities.GetColor(eq.rarity);
                    }
                }

                int selIdx = start + selectedIndex;
                if (selIdx < list.Count)
                    DisplayEquipmentDetails(list[selIdx]);
                else
                    ClearDetails();

                actionEquipBtn.label.Color = Palette.Bone;
                actionUpgradeBtn.label.Color = Palette.Bone;
                actionSellBtn.label.Color = Palette.Bone;
                actionDiscardBtn.label.Color = Palette.Bone;
                actionUseBtn.label.Color = Palette.Shade;
            }
            else if (currentCategory == InventoryCategory.Consumables)
            {
                var list = inventory.consumables;
                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        var c = list[itemIdx];
                        string name = c.Definition?.name ?? c.itemId;
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{name} x{c.count}".ToUpperInvariant(), 138));
                        itemSlotButtons[i].label.Color = Palette.Moss;
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
                actionDiscardBtn.label.Color = Palette.Bone;
                actionUseBtn.label.Color = Palette.Bone;
            }
            else if (currentCategory == InventoryCategory.Materials)
            {
                var list = inventory.materials.ToList();
                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        var m = list[itemIdx];
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{m.Key} x{m.Value}".ToUpperInvariant(), 138));
                        itemSlotButtons[i].label.Color = Palette.Wood;
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
                actionDiscardBtn.label.Color = Palette.Bone;
                actionUseBtn.label.Color = Palette.Shade;
            }
            else // KeyItems
            {
                var list = inventory.keyItems.ToList();
                int start = pageIndex * ItemsPerPage;
                for (int i = 0; i < ItemsPerPage; i++)
                {
                    int itemIdx = start + i;
                    if (itemIdx < list.Count)
                    {
                        string k = list[itemIdx];
                        string prefix = (i == selectedIndex) ? "> " : "  ";
                        itemSlotButtons[i].label.Set(PixelFont.Fit($"{prefix}{k}".ToUpperInvariant(), 138));
                        itemSlotButtons[i].label.Color = Palette.Gold;
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
            detailName?.Set(PixelFont.Fit(eqName.ToUpperInvariant(), 160));
            detailName.Color = EquipmentRarities.GetColor(eq.rarity);

            string slotName = def != null ? EquipmentSlots.DisplayName(def.slot) : "UNKNOWN";
            detailTypeRarity?.Set(PixelFont.Fit($"{eq.rarity.ToString().ToUpperInvariant()} {slotName} (LVL {eq.level})", 160));
            detailTypeRarity.Color = Palette.Parchment;

            // Base Stats
            var statParts = new List<string>();
            if (eq.EffectiveWeaponPower > 0) statParts.Add($"ATK +{eq.EffectiveWeaponPower}");
            if (eq.EffectivePhysicalDefense > 0) statParts.Add($"DEF +{eq.EffectivePhysicalDefense}");
            if (eq.EffectiveMagicDefense > 0) statParts.Add($"MDEF +{eq.EffectiveMagicDefense}");
            if (eq.EffectiveBreakPowerModifier > 0) statParts.Add($"BRK +{eq.EffectiveBreakPowerModifier}");
            detailBaseStats?.Set(PixelFont.Fit(statParts.Count > 0 ? string.Join("  ", statParts) : "NO BASE STATS", 160));

            // Affixes
            if (eq.affixes != null && eq.affixes.Count > 0)
            {
                var affList = eq.affixes.Select(a => $"• {a.GetDescription()}");
                detailAffixes?.Set(PixelFont.Wrap(string.Join("\n", affList).ToUpperInvariant(), 160));
            }
            else
            {
                detailAffixes?.Set("NO AFFIXES");
            }

            // Special Effect
            var se = eq.SpecialEffect;
            if (se != null)
            {
                detailSpecialEffect?.Set(PixelFont.Wrap($"★ {se.name}: {se.description}".ToUpperInvariant(), 160));
                detailSpecialEffect.Color = eq.rarity == EquipmentRarity.Mythic ? Palette.Crimson : Palette.Gold;
            }
            else
            {
                detailSpecialEffect?.Set("");
            }

            // Restrictions
            var r = def?.restrictions;
            if (r != null && r.allowedClasses != null && r.allowedClasses.Length > 0)
                detailRestrictions?.Set(PixelFont.Fit($"CLASS: {string.Join("/", r.allowedClasses).ToUpperInvariant()}", 160));
            else
                detailRestrictions?.Set("ALL CLASSES");

            detailValue?.Set($"SELL: {eq.CalculateSellValue()}G  UPGRADE COST: {eq.GetUpgradeCostGold()}G");
        }

        void DisplayConsumableDetails(ConsumableStack c)
        {
            var def = c.Definition;
            detailName?.Set(PixelFont.Fit(def?.name ?? c.itemId, 160));
            detailName.Color = Palette.Moss;
            detailTypeRarity?.Set($"CONSUMABLE  STACK: {c.count}/{c.maxStack}");
            detailBaseStats?.Set(PixelFont.Wrap(def?.description ?? "Consumable recovery or buff item.", 160));
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set($"SELL: 10G");
        }

        void DisplayMaterialDetails(string materialId, int count)
        {
            detailName?.Set(PixelFont.Fit(materialId.ToUpperInvariant(), 160));
            detailName.Color = Palette.Wood;
            detailTypeRarity?.Set($"MATERIAL  QUANTITY: {count}");
            detailBaseStats?.Set("Upgrade material used at the Hub Blacksmith to hone equipment.");
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set("USED FOR EQUIPMENT UPGRADES");
        }

        void DisplayKeyItemDetails(string keyId)
        {
            detailName?.Set(PixelFont.Fit(keyId.ToUpperInvariant(), 160));
            detailName.Color = Palette.Gold;
            detailTypeRarity?.Set("KEY ITEM  (CANNOT SELL / DISCARD)");
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

            // Open equipment screen or equip onto first compatible party member
            var targetHero = party.FirstOrDefault(u => EquipmentValidator.Validate(eq.Definition, eq.Definition.slot, u.classDef, u.race, u.level, u.FinalScores) == null) ?? party[0];
            if (EquipmentManager.EquipFromInventory(targetHero, eq.Definition.slot, eq, inventory, out string err))
            {
                feedbackLabel?.Set(PixelFont.Fit($"EQUIPPED {eq.Definition?.name} ON {targetHero.name.ToUpperInvariant()}", 308));
                Refresh();
            }
            else
            {
                feedbackLabel?.Set(PixelFont.Fit($"CANNOT EQUIP: {err.ToUpperInvariant()}", 308));
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
                feedbackLabel?.Set(PixelFont.Fit(reason.ToUpperInvariant(), 308));
                return;
            }

            if (Game.I?.Run != null && Game.I.Run.Gold < goldCost)
            {
                feedbackLabel?.Set(PixelFont.Fit($"NEED {goldCost}G TO UPGRADE (HAVE {playerGold}G)", 308));
                return;
            }

            if (inventory.GetMaterialCount("iron_ingot") < matCost)
            {
                // If in dev/test or player doesn't have materials, grant or verify
                if (playerMat < matCost && inventory.materials.Count > 0)
                {
                    feedbackLabel?.Set(PixelFont.Fit($"NEED {matCost} UPGRADE MATERIALS (HAVE {playerMat})", 308));
                    return;
                }
            }

            if (Game.I?.Run != null) Game.I.Run.Gold -= goldCost;
            inventory.RemoveMaterial("iron_ingot", matCost);
            eq.TryUpgrade(out _);
            feedbackLabel?.Set(PixelFont.Fit($"UPGRADED {eq.Definition?.name} TO +{eq.upgradeLevel}!", 308));
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
                    feedbackLabel?.Set(PixelFont.Fit($"SELL {eq.rarity.ToString().ToUpperInvariant()} ITEM? CLICK SELL AGAIN TO CONFIRM.", 308));
                    actionSellBtn.label.Color = Palette.Crimson;
                    return;
                }

                int val = eq.CalculateSellValue();
                inventory.RemoveEquipment(eq.instanceId);
                if (Game.I?.Run != null) Game.I.Run.Gold += val;
                feedbackLabel?.Set(PixelFont.Fit($"SOLD {eq.Definition?.name} FOR {val} GOLD.", 308));
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
                feedbackLabel?.Set(PixelFont.Fit($"DISCARDED {eq.Definition?.name}.", 308));
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
                    feedbackLabel?.Set(PixelFont.Fit($"USED {itemDef.name} ON {target.name.ToUpperInvariant()}.", 308));
                    Refresh();
                }
            }
        }
    }
}
