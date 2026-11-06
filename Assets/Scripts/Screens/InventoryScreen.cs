using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Inventory Screen — Starlit Reliquary "G · Inventory" board at 640x360:
    // header (Back · title · equipment-bag capacity bar), nearly-full toast, category tabs over a 10x4 slot grid,
    // gold item-details panel with large slot, rarity chip, stat rows and actions.
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
        const int Cols = 10, GridRows = 4, ItemsPerPage = Cols * GridRows, Cell = 28, Gap = 4;
        const int GridX = 20, GridY = 106, DetailX = 438, DetailW = 190, PanelY = 76, DetailTextW = 172;

        Label headerTitle;
        SpriteRenderer capacityBar;
        Label capacityLabel;
        Label pageIndicator;
        Label feedbackLabel;
        GameObject toast;

        // Details panel
        Label detailName;
        Label detailTypeRarity;
        Label detailBaseStats;
        Label detailAffixes;
        Label detailSpecialEffect;
        Label detailRestrictions;
        Label detailValue;
        Transform detailSlot;

        class SlotView { public Selectable sel; public SpriteRenderer box, ring, icon, chip; public Label letter, count; public bool focused; }
        readonly List<SlotView> slots = new List<SlotView>();
        readonly Dictionary<InventoryCategory, Selectable> tabs = new Dictionary<InventoryCategory, Selectable>();
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

            returnState = backTo ?? ReturnTarget();
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);

            // 1. Header: Back · title · equipment bag capacity
            var backBtn = Ui.Button(Root, "Back", 12, 9, 40, () => Game.Go(returnState), 18, showCursor: false);
            headerTitle = Ui.Label(Root, "Inventory", 62, 13, Palette.Gold2);
            Ui.Label(Root, "EQUIPMENT BAG", 463, 15, Palette.Mist3, font: FontKind.Interface);
            capacityBar = Ui.Image(Root, null, 524, 14, Ui.Widget);
            capacityLabel = Ui.Label(Root, "", 628, 14, Palette.Gold1, Align.Right, font: FontKind.Interface);

            // 2. Nearly-full toast (.toast.gold)
            toast = new GameObject("Toast");
            toast.transform.SetParent(Root, false);
            const string toastText = "EQUIPMENT BAG NEARLY FULL — NEW DROPS CAN'T BE PICKED UP AT 100.";
            int tw = PixelFont.Measure(toastText) + 30;
            Ui.Image(toast.transform, PixelArt.Box(tw, 18, Palette.Ink2, Palette.Gold3), 12, 33, Ui.Panels);
            Ui.Image(toast.transform, PixelArt.Badge(14, 14, Palette.Gold2, Palette.Gold2), 14, 35, Ui.Widget);
            Ui.Label(toast.transform, "!", 21, 39, Palette.Ink0, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
            Ui.Label(toast.transform, toastText, 34, 39, Palette.Mist1, font: FontKind.Interface);

            // 3. Category tabs + grid panel
            int tx = 12;
            foreach (var (cat, name) in new[] { (InventoryCategory.Equipment, "Equipment"), (InventoryCategory.Consumables, "Consumables"), (InventoryCategory.Materials, "Materials"), (InventoryCategory.KeyItems, "Key Items") })
            {
                var c = cat;
                int w = PixelFont.Measure(name, FontKind.DotGothic) + 30;
                var t = Ui.Button(Root, name, tx, PanelY - 19, w, () => SelectCategory(c), 20, showCursor: false);
                var box = t.GetComponentInChildren<SpriteRenderer>();
                t.label.Init(t.label.GetComponent<SpriteRenderer>(), 8, 5, Align.Left, 1, FontKind.DotGothic);
                t.label.Set(name);
                var countLabel = Ui.Label(t.transform, "", w - 6, 7, Palette.Mist3, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
                t.visual = focus =>
                {
                    bool on = currentCategory == c;
                    box.sprite = TabBox(w, 20, on, focus);
                    t.label.Color = on || focus ? Palette.Gold1 : Palette.Mist2;
                    countLabel.Set(CategoryCount(c).ToString());
                    countLabel.Color = on ? Palette.Mist1 : Palette.Mist3;
                };
                tabs[c] = t;
                menu.Add(t);
                tx += w + 2;
            }
            Ui.Panel(Root, 12, PanelY, DetailX - 12 - 7, 351 - PanelY);
            sortBtn = Ui.Button(Root, "SORT: RARITY", GridX, PanelY + 8, 150, CycleSortMode, 18, showCursor: false);
            filterBtn = Ui.Button(Root, "FILTER: ALL", GridX + 156, PanelY + 8, 160, CycleFilter, 18, showCursor: false);
            menu.Add(sortBtn);
            menu.Add(filterBtn);

            for (int i = 0; i < ItemsPerPage; i++)
            {
                int idx = i;
                var v = CreateSlot(i);
                v.sel.onClick = () => SelectItem(idx);
                slots.Add(v);
                menu.Add(v.sel);
            }

            int py = GridY + GridRows * (Cell + Gap) + 2;
            var prevPageBtn = Ui.Button(Root, "< Prev", GridX, py, 60, () => ChangePage(-1), 18, showCursor: false);
            pageIndicator = Ui.Label(Root, "PAGE 1 / 1", GridX + 158, py + 6, Palette.Mist3, Align.Center, font: FontKind.Interface);
            var nextPageBtn = Ui.Button(Root, "Next >", GridX + 256, py, 60, () => ChangePage(1), 18, showCursor: false);
            menu.Add(prevPageBtn);
            menu.Add(nextPageBtn);
            Ui.Label(Root, "CONSUMABLES AND MATERIALS STACK — ONE SLOT PER ITEM TYPE.", GridX, 318, Palette.Mist2, font: FontKind.Interface);
            int hx = Ui.KeyHint(Root, GridX, 331, "USE", "Z");
            Ui.KeyHint(Root, hx, 331, "DISCARD", "DEL");
            feedbackLabel = Ui.Label(Root, "", GridX + 150, 333, Palette.Gold1, font: FontKind.Interface);

            // 4. Item details (gold frame)
            Ui.Panel(Root, DetailX, 33, DetailW, 318, Palette.Ink2, Palette.Gold3);
            Ui.Header(Root, "Item details", DetailX + 8, 39, DetailW - 4);
            detailSlot = new GameObject("DetailSlot").transform;
            detailSlot.SetParent(Root, false);
            detailName = Ui.Label(Root, "", DetailX + 50, 64, Palette.Gold2);
            detailTypeRarity = Ui.Label(Root, "", DetailX + 50, 78, Palette.Mist3, font: FontKind.Interface);
            Ui.Rect(Root, DetailX + 2, 101, DetailW - 4, 1, Palette.Ink4);
            detailBaseStats = Ui.Label(Root, "", DetailX + 8, 108, Palette.Mist1, font: FontKind.Interface);
            Ui.Label(Root, "AFFIXES & EFFECTS", DetailX + 8, 152, Palette.Mist3, font: FontKind.Interface);
            detailAffixes = Ui.Label(Root, "", DetailX + 8, 164, Palette.Xp, font: FontKind.Interface);
            detailSpecialEffect = Ui.Label(Root, "", DetailX + 8, 214, Palette.Gold2, font: FontKind.Interface);
            detailRestrictions = Ui.Label(Root, "", DetailX + 8, 262, Palette.Mist3, font: FontKind.Interface);
            detailValue = Ui.Label(Root, "", DetailX + 8, 276, Palette.Gold1, font: FontKind.Interface);

            // Actions (footer of the details panel)
            Ui.Rect(Root, DetailX + 2, 290, DetailW - 4, 1, Palette.Ink4);
            actionEquipBtn = Ui.Button(Root, "Equip", DetailX + 8, 296, 54, OnEquipClicked, 18, showCursor: false);
            actionUpgradeBtn = Ui.Button(Root, "Upgrade", DetailX + 66, 296, 66, OnUpgradeClicked, 18, showCursor: false);
            actionSellBtn = Ui.Button(Root, "Sell", DetailX + 136, 296, 46, OnSellClicked, 18, showCursor: false);
            actionDiscardBtn = Ui.Button(Root, "Discard", DetailX + 8, 322, 66, OnDiscardClicked, 20, showCursor: false, style: ButtonStyle.Danger);
            actionUseBtn = Ui.Button(Root, "Use on...", DetailX + 80, 322, DetailW - 88, OnUseClicked, 20, showCursor: false, style: ButtonStyle.Primary);

            menu.Add(actionEquipBtn);
            menu.Add(actionUpgradeBtn);
            menu.Add(actionSellBtn);
            menu.Add(actionDiscardBtn);
            menu.Add(actionUseBtn);
            menu.Add(backBtn);

            Refresh();
        }

        // Tab (.tab): open at the bottom; active tab raised with a gold top stripe.
        static Sprite TabBox(int w, int h, bool on, bool focus)
        {
            var fill = on || focus ? Palette.Ink3 : Palette.Ink2;
            var border = on ? Palette.Gold3 : focus ? Palette.Mist3 : Palette.Ink4;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || x == w - 1 || py == 0;
                    px[y * w + x] = edge ? border : on && py <= 2 ? Palette.Gold2 : fill;
                }
            }
            return PixelArt.SpriteFrom(px, w, h, PixelArt.TopLeft, "tab");
        }

        int CategoryCount(InventoryCategory c) => c switch
        {
            InventoryCategory.Equipment => inventory.EquipmentCount,
            InventoryCategory.Consumables => inventory.consumables.Count,
            InventoryCategory.Materials => inventory.materials.Count,
            _ => inventory.keyItems.Count(),
        };

        SlotView CreateSlot(int i)
        {
            var go = new GameObject("Slot:" + i);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(GridX + (i % Cols) * (Cell + Gap), -(GridY + (i / Cols) * (Cell + Gap)), 0);
            var v = new SlotView
            {
                ring = Ui.Image(go.transform, null, -2, -2, Ui.Widget),
                box = Ui.Image(go.transform, null, 0, 0, Ui.Widget + 1),
                icon = Ui.Image(go.transform, null, 9, 9, Ui.Widget + 2),
                chip = Ui.Image(go.transform, null, 0, 0, Ui.Widget + 3),
                letter = Ui.Label(go.transform, "", 1, 1, Palette.Ink0, order: Ui.Widget + 4, font: FontKind.Interface),
                count = Ui.Label(go.transform, "", Cell - 2, Cell - 9, Palette.Mist1, Align.Right, order: Ui.Widget + 4, font: FontKind.Interface),
            };
            v.sel = go.AddComponent<Selectable>();
            v.sel.size = new Vector2Int(Cell, Cell);
            if (!Selectable.All.Contains(v.sel)) Selectable.All.Add(v.sel);
            v.sel.visual = on => { v.focused = on; PaintSlot(i); };
            return v;
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
            sortBtn?.label?.Set(PixelFont.Fit($"SORT: {currentSortMode.ToString().ToUpperInvariant()}", 140, FontKind.DotGothic));
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
            string fltText = currentSlotFilter.HasValue ? EquipmentSlots.DisplayName(currentSlotFilter.Value) : "ALL";
            filterBtn?.label?.Set(PixelFont.Fit($"FILTER: {fltText.ToUpperInvariant()}", 150, FontKind.DotGothic));
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

        // What one grid cell shows: icon sprite + tint, rarity, and stack count / upgrade level.
        (Sprite icon, Color tint, EquipmentRarity? rarity, string count) CellContent(int itemIdx)
        {
            switch (currentCategory)
            {
                case InventoryCategory.Equipment:
                {
                    var list = GetCurrentEquipmentList();
                    if (itemIdx >= list.Count) break;
                    var eq = list[itemIdx];
                    var def = eq.Definition;
                    return (PixelArt.FromRows(Art.GearIcon(def?.slot ?? EquipmentSlot.MainHand, def)), EquipmentRarities.GetColor(eq.rarity), eq.rarity, eq.upgradeLevel > 0 ? $"+{eq.upgradeLevel}" : null);
                }
                case InventoryCategory.Consumables:
                {
                    if (itemIdx >= inventory.consumables.Count) break;
                    var c = inventory.consumables[itemIdx];
                    var d = c.Definition;
                    return (PixelArt.ItemIcon(ConsumableKind(d), ConsumableTint(d)), Color.white, d?.rarity, c.count.ToString());
                }
                case InventoryCategory.Materials:
                {
                    var list = inventory.materials.ToList();
                    if (itemIdx >= list.Count) break;
                    return (PixelArt.ItemIcon("ore", Palette.ElementIce), Color.white, EquipmentRarity.Common, list[itemIdx].Value.ToString());
                }
                default:
                {
                    var list = inventory.keyItems.ToList();
                    if (itemIdx >= list.Count) break;
                    return (PixelArt.ItemIcon("scroll", Palette.Gold2), Color.white, null, null);
                }
            }
            return (null, Color.white, null, null);
        }

        static string ConsumableKind(ItemDef d) => d?.category switch
        {
            "Food" => "herb",
            "Tactical Item" or "Exploration Item" => "scroll",
            _ => "potion",
        };

        static Color ConsumableTint(ItemDef d) =>
            d == null ? Palette.Mist2
            : d.isRevive ? Palette.Gold1
            : d.mana > 0 || d.manaPercent > 0 ? Palette.Mp
            : d.heal > 0 || d.healPercent > 0 ? Palette.Hex(0xFF6A7A)
            : d.category == "Buff Item" ? Palette.Xp
            : Palette.ElementWind;

        void PaintSlot(int i)
        {
            var v = slots[i];
            var (icon, tint, rarity, count) = CellContent(pageIndex * ItemsPerPage + i);
            bool has = icon != null, selected = has && i == selectedIndex;
            Color rc = rarity.HasValue ? EquipmentRarities.GetColor(rarity.Value) : Palette.Ink4;
            Color border = selected ? Palette.Gold2 : v.focused ? Palette.Mist3 : has ? Color.Lerp(rc, Palette.Ink4, 0.45f) : Palette.Ink4;
            v.box.sprite = PixelArt.Box(Cell, Cell, Palette.Ink0, border);
            v.ring.sprite = selected ? PixelArt.Badge(Cell + 4, Cell + 4, Color.clear, Palette.Gold3) : null;
            v.icon.sprite = icon;
            v.icon.color = tint;
            if (icon != null) v.icon.transform.localPosition = new Vector3((Cell - (int)icon.rect.width) / 2, -(Cell - (int)icon.rect.height) / 2, 0);
            bool showRarity = has && rarity.HasValue && currentCategory == InventoryCategory.Equipment;
            v.chip.sprite = showRarity ? PixelArt.Badge(7, 8, rc, rc) : null;
            v.letter.Set(showRarity ? Palette.RarityBadge(rarity.Value) : "");
            v.count.Set(count ?? "");
            v.sel.interactable = has;
        }

        void Refresh()
        {
            confirmSellActive = false;
            feedbackLabel?.Set("");

            foreach (var t in tabs.Values) t.visual(t.menu != null && t.menu.Current == t);

            int eqCount = inventory.EquipmentCount;
            bool nearlyFull = eqCount >= InventoryRuntime.MaxEquipmentCapacity - 10;
            capacityLabel?.Set($"{eqCount} / {InventoryRuntime.MaxEquipmentCapacity}");
            capacityLabel.Color = inventory.IsEquipmentFull ? Palette.HpLow : Palette.Gold1;
            capacityBar.sprite = PixelArt.GaugeBar(60, 7, inventory.IsEquipmentFull ? Palette.HpLow : nearlyFull ? Palette.Warn : Palette.Gold2, (float)eqCount / InventoryRuntime.MaxEquipmentCapacity);
            toast.SetActive(nearlyFull);

            int total = CategoryCount(currentCategory);
            int totalPages = Math.Max(1, (total + ItemsPerPage - 1) / ItemsPerPage);
            if (pageIndex >= totalPages) pageIndex = totalPages - 1;
            pageIndicator?.Set($"PAGE {pageIndex + 1} / {totalPages}");
            for (int i = 0; i < slots.Count; i++) PaintSlot(i);

            int selIdx = pageIndex * ItemsPerPage + selectedIndex;
            switch (currentCategory)
            {
                case InventoryCategory.Equipment:
                    var eqList = GetCurrentEquipmentList();
                    if (selIdx < eqList.Count) DisplayEquipmentDetails(eqList[selIdx]); else ClearDetails();
                    break;
                case InventoryCategory.Consumables:
                    if (selIdx < inventory.consumables.Count) DisplayConsumableDetails(inventory.consumables[selIdx]); else ClearDetails();
                    break;
                case InventoryCategory.Materials:
                    var mats = inventory.materials.ToList();
                    if (selIdx < mats.Count) DisplayMaterialDetails(mats[selIdx].Key, mats[selIdx].Value); else ClearDetails();
                    break;
                default:
                    var keys = inventory.keyItems.ToList();
                    if (selIdx < keys.Count) DisplayKeyItemDetails(keys[selIdx]); else ClearDetails();
                    break;
            }
            PaintDetailSlot(selIdx);

            bool equipment = currentCategory == InventoryCategory.Equipment;
            SetAction(actionEquipBtn, equipment);
            SetAction(actionUpgradeBtn, equipment);
            SetAction(actionSellBtn, equipment);
            SetAction(actionDiscardBtn, currentCategory != InventoryCategory.KeyItems);
            SetAction(actionUseBtn, currentCategory == InventoryCategory.Consumables);
        }

        static void SetAction(Selectable b, bool on)
        {
            b.interactable = on;
            b.visual(b.menu != null && b.menu.Current == b);
        }

        // Large slot (.slot 72px → 36) beside the item name, with the rarity chip (.rar) under the name.
        void PaintDetailSlot(int itemIdx)
        {
            foreach (var c in detailSlot.Cast<Transform>().ToList())
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(c.gameObject);
                else UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
            var (icon, tint, rarity, count) = CellContent(itemIdx);
            Color rc = rarity.HasValue ? EquipmentRarities.GetColor(rarity.Value) : Palette.Ink4;
            Ui.Image(detailSlot, PixelArt.Box(36, 36, Palette.Ink0, icon != null ? Color.Lerp(rc, Palette.Ink4, 0.45f) : Palette.Ink4), DetailX + 8, 60, Ui.Widget);
            if (icon == null) return;
            var sr = Ui.Image(detailSlot, icon, 0, 0, Ui.Widget + 1, tint);
            sr.transform.localScale = new Vector3(2, 2, 1);
            sr.transform.localPosition = new Vector3(DetailX + 8 + (36 - 2 * (int)icon.rect.width) / 2, -(60 + (36 - 2 * (int)icon.rect.height) / 2), 0);
            if (!string.IsNullOrEmpty(count)) Ui.Label(detailSlot, count.StartsWith("+") ? count : "X" + count, DetailX + 42, 87, Palette.Mist1, Align.Right, order: Ui.Widget + 2, font: FontKind.Interface);
            if (rarity.HasValue && currentCategory != InventoryCategory.KeyItems)
            {
                string r = rarity.Value.ToString().ToUpperInvariant();
                int w = PixelFont.Measure(r) + 16;
                var chip = rarity.Value >= EquipmentRarity.Legendary ? PixelArt.DoubleBorderBox(w, 11, Palette.Ink0, rc) : PixelArt.Badge(w, 11, Palette.Ink0, rc);
                Ui.Image(detailSlot, chip, DetailX + 50, 89, Ui.Widget);
                Ui.Image(detailSlot, PixelArt.Badge(7, 7, rc, rc), DetailX + 52, 91, Ui.Widget + 1);
                Ui.Label(detailSlot, Palette.RarityBadge(rarity.Value), DetailX + 53, 91, Palette.Ink0, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(detailSlot, r, DetailX + 62, 91, rc, order: Ui.Widget + 1, font: FontKind.Interface);
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
            detailName?.Set(PixelFont.Fit(eqName, 132, FontKind.DotGothic));
            detailName.Color = EquipmentRarities.GetColor(eq.rarity);

            string slotName = def != null ? EquipmentSlots.DisplayName(def.slot) : "UNKNOWN";
            detailTypeRarity?.Set(PixelFont.Fit($"{slotName.ToUpperInvariant()} · LVL {eq.level}", 132));
            detailTypeRarity.Color = Palette.MutedGray;

            var statParts = new List<string>();
            if (eq.EffectiveWeaponPower > 0) statParts.Add($"ATK +{eq.EffectiveWeaponPower}");
            if (eq.EffectivePhysicalDefense > 0) statParts.Add($"DEF +{eq.EffectivePhysicalDefense}");
            if (eq.EffectiveMagicDefense > 0) statParts.Add($"MDEF +{eq.EffectiveMagicDefense}");
            if (eq.EffectiveBreakPowerModifier > 0) statParts.Add($"BRK +{eq.EffectiveBreakPowerModifier}");
            detailBaseStats?.Set(PixelFont.Wrap(statParts.Count > 0 ? string.Join("   ", statParts) : "NO BASE STATS", DetailTextW));

            if (eq.affixes != null && eq.affixes.Count > 0)
                detailAffixes?.Set(PixelFont.Wrap(string.Join("\n", eq.affixes.Select(a => $"- {a.GetDescription()}")).ToUpperInvariant(), DetailTextW));
            else
                detailAffixes?.Set("NO AFFIXES");

            var se = eq.SpecialEffect;
            if (se != null)
            {
                detailSpecialEffect?.Set(PixelFont.Wrap($"* {se.name}: {se.description}".ToUpperInvariant(), DetailTextW));
                detailSpecialEffect.Color = eq.rarity == EquipmentRarity.Mythic ? Palette.RarityMythic : Palette.RarityLegendary;
            }
            else detailSpecialEffect?.Set("");

            var r = def?.restrictions;
            if (r != null && r.allowedClasses != null && r.allowedClasses.Length > 0)
                detailRestrictions?.Set(PixelFont.Fit($"CLASS: {string.Join("/", r.allowedClasses).ToUpperInvariant()}", DetailTextW));
            else
                detailRestrictions?.Set("ALL CLASSES COMPATIBLE");

            detailValue?.Set(PixelFont.Fit($"SELL {eq.CalculateSellValue()}G · UPGRADE {eq.GetUpgradeCostGold()}G", DetailTextW));
        }

        void DisplayConsumableDetails(ConsumableStack c)
        {
            var def = c.Definition;
            detailName?.Set(PixelFont.Fit(def?.name ?? c.itemId, 132, FontKind.DotGothic));
            detailName.Color = def != null ? EquipmentRarities.GetColor(def.rarity) : Palette.Emerald;
            detailTypeRarity?.Set($"CONSUMABLE · STACK {c.count}/{c.maxStack}");
            detailBaseStats?.Set(PixelFont.Wrap((def?.description ?? "Consumable recovery or buff item.").ToUpperInvariant(), DetailTextW));
            detailAffixes?.Set(PixelFont.Wrap($"TARGET: {(def?.target ?? "ONE ALLY").ToUpperInvariant()}", DetailTextW));
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set(PixelFont.Wrap((def?.usageRestriction ?? "").ToUpperInvariant(), DetailTextW));
            detailValue?.Set($"SELL VALUE: {Math.Max(1, (def?.shopValue ?? 20) / 2)}G EACH");
        }

        void DisplayMaterialDetails(string materialId, int count)
        {
            detailName?.Set(PixelFont.Fit(materialId, 132, FontKind.DotGothic));
            detailName.Color = Palette.WarmIvory;
            detailTypeRarity?.Set($"MATERIAL · QUANTITY {count}");
            detailBaseStats?.Set(PixelFont.Wrap("UPGRADE MATERIAL USED AT THE HUB BLACKSMITH TO HONE EQUIPMENT.", DetailTextW));
            detailAffixes?.Set("");
            detailSpecialEffect?.Set("");
            detailRestrictions?.Set("");
            detailValue?.Set("USED FOR EQUIPMENT UPGRADES");
        }

        void DisplayKeyItemDetails(string keyId)
        {
            detailName?.Set(PixelFont.Fit(keyId, 132, FontKind.DotGothic));
            detailName.Color = Palette.AntiqueGoldBright;
            detailTypeRarity?.Set("KEY ITEM · BOUND TO HEROES");
            detailBaseStats?.Set(PixelFont.Wrap("UNIQUE PROGRESSION ITEM FOR DUNGEON AND STORY EXPLORATION.", DetailTextW));
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

        int SelectedStackIndex => pageIndex * ItemsPerPage + selectedIndex;

        void Say(string text) => feedbackLabel?.Set(PixelFont.Fit((text ?? "").ToUpperInvariant(), 250));

        void OnEquipClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment();
            if (eq == null || party == null || party.Count == 0) return;

            var targetHero = party.FirstOrDefault(u => EquipmentValidator.Validate(eq.Definition, eq.Definition.slot, u.classDef, u.race, u.level, u.FinalScores, u.id) == null) ?? party[0];
            if (EquipmentManager.QuickEquip(targetHero, eq, inventory, out string err))
            {
                Refresh();
                Say($"EQUIPPED {eq.Definition?.name} ON {targetHero.name}");
            }
            else Say($"CANNOT EQUIP: {err}");
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

            if (!eq.CanUpgrade(out string reason)) { Say(reason); return; }
            if (Game.I?.Run != null && Game.I.Run.Gold < goldCost) { Say($"NEED {goldCost}G TO UPGRADE (HAVE {playerGold}G)"); return; }
            if (playerMat < matCost && inventory.materials.Count > 0) { Say($"NEED {matCost} UPGRADE MATERIALS (HAVE {playerMat})"); return; }

            if (Game.I?.Run != null) Game.I.Run.Gold -= goldCost;
            inventory.RemoveMaterial("iron_ingot", matCost);
            eq.TryUpgrade(out _);
            Refresh();
            Say($"UPGRADED {eq.Definition?.name} TO +{eq.upgradeLevel}!");
        }

        void OnSellClicked()
        {
            if (currentCategory != InventoryCategory.Equipment) return;
            var eq = GetSelectedEquipment();
            if (eq == null) return;
            if (eq.isEquipped) { Say("CANNOT SELL EQUIPPED ITEM. UNEQUIP IT FIRST."); return; }

            if (EquipmentRarities.HasSpecialEffect(eq.rarity) && !confirmSellActive)
            {
                confirmSellActive = true;
                Say($"SELL {eq.rarity} ITEM? PRESS SELL AGAIN.");
                actionSellBtn.label.Color = Palette.SpecCrimson;
                return;
            }

            int val = eq.CalculateSellValue();
            inventory.RemoveEquipment(eq.instanceId);
            if (Game.I?.Run != null) Game.I.Run.Gold += val;
            Refresh();
            Say($"SOLD {eq.Definition?.name} FOR {val} GOLD.");
        }

        void OnDiscardClicked()
        {
            if (currentCategory == InventoryCategory.KeyItems) { Say("KEY ITEMS CANNOT BE DISCARDED."); return; }
            if (currentCategory == InventoryCategory.Equipment)
            {
                var eq = GetSelectedEquipment();
                if (eq == null) return;
                if (eq.isEquipped) { Say("CANNOT DISCARD EQUIPPED ITEM. UNEQUIP IT FIRST."); return; }
                inventory.RemoveEquipment(eq.instanceId);
                Refresh();
                Say($"DISCARDED {eq.Definition?.name}.");
            }
            else if (currentCategory == InventoryCategory.Consumables && SelectedStackIndex < inventory.consumables.Count)
            {
                var c = inventory.consumables[SelectedStackIndex];
                inventory.RemoveConsumable(c.itemId, 1);
                Refresh();
                Say($"DISCARDED 1 {c.Definition?.name ?? c.itemId}.");
            }
        }

        void OnUseClicked()
        {
            if (currentCategory != InventoryCategory.Consumables) return;
            if (SelectedStackIndex >= inventory.consumables.Count || party == null || party.Count == 0) return;
            var c = inventory.consumables[SelectedStackIndex];
            var itemDef = c.Definition;
            if (itemDef == null) return;

            if (itemDef.isRevive)
            {
                var deadTarget = party.FirstOrDefault(u => !u.Alive);
                if (deadTarget == null) { Say("NO FALLEN ALLY TO REVIVE."); return; }
                float pct = itemDef.reviveHpPercent > 0 ? itemDef.reviveHpPercent : 0.30f;
                deadTarget.hp = Math.Max(1, (int)Math.Round(deadTarget.stats.maxHp * pct));
                deadTarget.RecalculateStats();
                inventory.RemoveConsumable(c.itemId, 1);
                Game.I?.Run?.Items?.Remove(c.itemId);
                Refresh();
                Say($"REVIVED {deadTarget.name} WITH {deadTarget.hp} HP!");
                return;
            }

            var target = party.FirstOrDefault(u => u.Alive && (u.hp < u.stats.maxHp || u.mp < u.stats.maxMp)) ?? party[0];
            if (itemDef.heal > 0) target.Heal(itemDef.heal);
            if (itemDef.mana > 0) target.mp = Math.Min(target.stats.maxMp, target.mp + itemDef.mana);
            inventory.RemoveConsumable(c.itemId, 1);
            Game.I?.Run?.Items?.Remove(c.itemId);
            Refresh();
            Say($"USED {itemDef.name} ON {target.name}.");
        }
    }
}
