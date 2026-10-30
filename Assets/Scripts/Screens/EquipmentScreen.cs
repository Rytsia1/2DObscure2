using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Equipment Screen conforming to UI/UX Overhaul Specification (Section 4.2)
    // 640x360 Reference Resolution, 5 Core Equipment Slots, Dual-Panel Comparison, Octopath Traveler II Aesthetics.
    public class EquipmentScreen : GameScreen
    {
        readonly GameState returnState;
        readonly InventoryRuntime inventory;
        readonly List<Unit> party;

        int heroIndex = 0;
        int selectedSlotIndex = 0;
        int candidateIndex = -1;

        public static readonly EquipmentSlot[] CoreSlots =
        {
            EquipmentSlot.MainHand,
            EquipmentSlot.HeadArmor,
            EquipmentSlot.BodyArmor,
            EquipmentSlot.Accessory1,
            EquipmentSlot.Accessory2
        };

        public static string GetCoreSlotName(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.MainHand => "WEAPON",
            EquipmentSlot.HeadArmor => "HEAD",
            EquipmentSlot.BodyArmor => "BODY",
            EquipmentSlot.Accessory1 => "ACC 1",
            EquipmentSlot.Accessory2 => "ACC 2",
            _ => slot.ToString()
        };

        public Unit CurrentHero => party[Math.Clamp(heroIndex, 0, party.Count - 1)];
        public EquipmentSlot SelectedSlot => CoreSlots[Math.Clamp(selectedSlotIndex, 0, CoreSlots.Length - 1)];

        Label titleLabel;
        Label heroLabel;
        Label feedbackLabel;

        // Slot buttons (left panel top)
        readonly List<Selectable> slotButtons = new List<Selectable>();
        // Compatible inventory candidate buttons (left panel bottom)
        readonly List<Selectable> candidateButtons = new List<Selectable>();

        // Right panel labels (Comparison & Details)
        Label detailTitle;
        Label detailSub;
        readonly Dictionary<DerivedStatId, Label> comparisonLabels = new Dictionary<DerivedStatId, Label>();
        Label candidateAffixesLabel;
        Label candidateSpecialLabel;

        Selectable equipActionBtn;
        Selectable unequipActionBtn;

        static readonly DerivedStatId[] ComparedStats =
        {
            DerivedStatId.MaxHP,
            DerivedStatId.PhysicalPower,
            DerivedStatId.MagicPower,
            DerivedStatId.PhysicalDefense,
            DerivedStatId.MagicDefense,
            DerivedStatId.CriticalChance,
            DerivedStatId.Initiative,
            DerivedStatId.StatusResistance
        };

        public EquipmentScreen() : this(null, null, 0, null) { }

        public EquipmentScreen(InventoryRuntime inv = null, IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
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

            heroIndex = Math.Clamp(initialHero, 0, Math.Max(0, party.Count - 1));
            returnState = backTo ?? (Game.I?.Run != null ? GameState.Map : GameState.MainMenu);
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // 1. Top bar (640x26): Header Title, Hero Switcher, Navigation hint
            Ui.Panel(Root, 0, 0, Ui.Width, 26, Palette.DeepCharcoal, Palette.AntiqueGold);
            titleLabel = Ui.Label(Root, "EQUIPMENT LOADOUT", 12, 7, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);

            var prevHeroBtn = Ui.Button(Root, "<", 220, 4, 24, () => SwitchHero(-1), 18, showCursor: false);
            heroLabel = Ui.Label(Root, "", 320, 7, Palette.WarmIvory, Align.Center, 1, FontKind.Serif);
            var nextHeroBtn = Ui.Button(Root, ">", 396, 4, 24, () => SwitchHero(1), 18, showCursor: false);

            Ui.Badge(Root, "TAB/ESC: CLOSE", 530, 6, 98, 14, Palette.MidnightNavy, Palette.DarkBorder, Palette.MutedGray, Align.Center);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);
            menu.Add(prevHeroBtn);
            menu.Add(nextHeroBtn);

            // 2. Left Panel (350x296): Equipped Slots (Top) & Compatible Inventory Candidates (Bottom)
            Ui.Panel(Root, 8, 28, 350, 296, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, "EQUIPPED SLOTS", 18, 33, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(Root, 18, 45, 330, Palette.DarkBorder, Palette.AntiqueGoldDim);

            slotButtons.Clear();
            for (int i = 0; i < CoreSlots.Length; i++)
            {
                int sIdx = i;
                int y = 50 + i * 22;
                var btn = Ui.Button(Root, "", 16, y, 334, () => SelectSlot(sIdx), 19);
                slotButtons.Add(btn);
                menu.Add(btn);
            }

            Ui.Divider(Root, 18, 166, 330, Palette.DarkBorder, Palette.AntiqueGoldDim);
            Ui.Label(Root, "COMPATIBLE GEAR IN BAG", 18, 172, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);

            candidateButtons.Clear();
            for (int i = 0; i < 5; i++)
            {
                int cIdx = i;
                int y = 188 + i * 22;
                var btn = Ui.Button(Root, "", 16, y, 334, () => SelectCandidate(cIdx), 19);
                candidateButtons.Add(btn);
                menu.Add(btn);
            }

            // 3. Right Panel (270x296): Side-by-Side Comparison Modal & Item Details
            Ui.Panel(Root, 362, 28, 270, 296, Palette.DeepCharcoal, Palette.DarkBorder);
            detailTitle = Ui.Label(Root, "", 372, 33, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            detailSub = Ui.Label(Root, "", 372, 47, Palette.MutedGray);
            Ui.Divider(Root, 372, 59, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);

            comparisonLabels.Clear();
            for (int i = 0; i < ComparedStats.Length; i++)
            {
                var st = ComparedStats[i];
                int col = i / 4;
                int row = i % 4;
                int x = 372 + col * 128;
                int y = 64 + row * 18;
                comparisonLabels[st] = Ui.Label(Root, "", x, y, Palette.WarmIvory);
            }

            Ui.Divider(Root, 372, 142, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);
            Ui.Label(Root, "ENCHANTMENTS & AFFIXES", 372, 147, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            candidateAffixesLabel = Ui.Label(Root, "", 372, 162, Palette.ArcaneViolet);

            Ui.Divider(Root, 372, 230, 250, Palette.DarkBorder, Palette.AntiqueGoldDim);
            candidateSpecialLabel = Ui.Label(Root, "", 372, 236, Palette.AntiqueGoldBright);

            // 4. Bottom Panel (640x32): Actions & Live Feedback
            Ui.Panel(Root, 0, 328, Ui.Width, 32, Palette.DeepCharcoal, Palette.AntiqueGold);
            feedbackLabel = Ui.Label(Root, "", 14, 333, Palette.WarmIvory);

            equipActionBtn = Ui.Button(Root, "EQUIP", 16, 336, 68, OnEquipClicked, 18, showCursor: false);
            unequipActionBtn = Ui.Button(Root, "UNEQUIP", 90, 336, 80, OnUnequipClicked, 18, showCursor: false);
            var invBtn = Ui.Button(Root, "INVENTORY", 176, 336, 96, () => Game.Go(GameState.Inventory), 18, showCursor: false);
            var backBtn = Ui.Button(Root, "BACK", 564, 336, 64, () => Game.Go(returnState), 18, showCursor: false);

            menu.Add(equipActionBtn);
            menu.Add(unequipActionBtn);
            menu.Add(invBtn);
            menu.Add(backBtn);

            Refresh();
        }

        void SwitchHero(int delta)
        {
            if (party.Count == 0) return;
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            selectedSlotIndex = 0;
            candidateIndex = -1;
            Refresh();
        }

        void SelectSlot(int index)
        {
            selectedSlotIndex = index;
            candidateIndex = -1;
            Refresh();
        }

        void SelectCandidate(int index)
        {
            candidateIndex = index;
            Refresh();
        }

        List<EquipmentInstance> GetCompatibleInventoryItems()
        {
            var hero = CurrentHero;
            var slot = SelectedSlot;
            return inventory.equipment
                .Where(e => !e.isEquipped &&
                            EquipmentValidator.SlotMatches(e.Definition, slot) &&
                            EquipmentValidator.Validate(e.Definition, slot, hero.classDef, hero.race, hero.level, hero.FinalScores) == null)
                .ToList();
        }

        void Refresh()
        {
            var hero = CurrentHero;
            heroLabel?.Set(PixelFont.Fit($"{hero.name.ToUpperInvariant()}  —  {hero.classDef?.name.ToUpperInvariant()}", 160));

            // Populate 5 core slots
            for (int i = 0; i < CoreSlots.Length; i++)
            {
                var s = CoreSlots[i];
                string slotName = GetCoreSlotName(s);
                EquipmentInstance inst = hero.equippedInstances.TryGetValue(s, out var ei) ? ei : null;
                EquipmentDef legacyDef = hero.equipped.TryGetValue(s, out var ld) ? ld : null;

                string itemName = "EMPTY";
                Color itemColor = Palette.MutedGray;
                if (inst != null)
                {
                    itemName = inst.upgradeLevel > 0 ? $"{inst.Definition?.name} +{inst.upgradeLevel}" : inst.Definition?.name ?? "ITEM";
                    itemColor = EquipmentRarities.GetColor(inst.rarity);
                }
                else if (legacyDef != null)
                {
                    itemName = legacyDef.name;
                    itemColor = EquipmentRarities.GetColor(legacyDef.rarity);
                }

                string prefix = (i == selectedSlotIndex) ? "> " : "  ";
                slotButtons[i].label.Set(PixelFont.Fit($"{prefix}{slotName,-7} : {itemName}".ToUpperInvariant(), 320));
                slotButtons[i].label.Color = itemColor;
            }

            // Populate compatible inventory candidates
            var compatible = GetCompatibleInventoryItems();
            for (int i = 0; i < candidateButtons.Count; i++)
            {
                if (i < compatible.Count)
                {
                    var item = compatible[i];
                    string prefix = (i == candidateIndex) ? "> " : "  ";
                    string name = item.upgradeLevel > 0 ? $"{item.Definition?.name} +{item.upgradeLevel}" : item.Definition?.name ?? "ITEM";
                    candidateButtons[i].label.Set(PixelFont.Fit($"{prefix}{name}".ToUpperInvariant(), 320));
                    candidateButtons[i].label.Color = EquipmentRarities.GetColor(item.rarity);
                }
                else
                {
                    candidateButtons[i].label.Set("  ---");
                    candidateButtons[i].label.Color = Palette.Shade;
                }
            }

            // Right Panel: Comparison or Current Slot Details
            EquipmentInstance selectedCandidate = (candidateIndex >= 0 && candidateIndex < compatible.Count) ? compatible[candidateIndex] : null;
            EquipmentInstance currentEquipped = hero.equippedInstances.TryGetValue(SelectedSlot, out var cInst) ? cInst : null;

            if (selectedCandidate != null)
            {
                // Comparison Mode
                string candName = selectedCandidate.upgradeLevel > 0 ? $"{selectedCandidate.Definition?.name} +{selectedCandidate.upgradeLevel}" : selectedCandidate.Definition?.name ?? "ITEM";
                detailTitle?.Set(PixelFont.Fit($"COMPARE: {candName}".ToUpperInvariant(), 250));
                detailTitle.Color = EquipmentRarities.GetColor(selectedCandidate.rarity);
                detailSub?.Set($"SLOT: {GetCoreSlotName(SelectedSlot)}  •  LEVEL {selectedCandidate.level}  •  {selectedCandidate.rarity.ToString().ToUpperInvariant()}");

                foreach (var st in ComparedStats)
                {
                    int delta = EquipmentManager.CalculateStatDelta(hero, SelectedSlot, selectedCandidate, st);
                    int currentVal = hero.Derived.Get(st);
                    string deltaStr = delta > 0 ? $"+{delta}" : delta.ToString();
                    Color deltaColor = delta > 0 ? Palette.Emerald : delta < 0 ? Palette.SpecCrimson : Palette.WarmIvory;
                    string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                    comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct} ({deltaStr})", 124));
                    comparisonLabels[st].Color = deltaColor;
                }

                if (selectedCandidate.affixes != null && selectedCandidate.affixes.Count > 0)
                    candidateAffixesLabel?.Set(PixelFont.Wrap(string.Join("\n", selectedCandidate.affixes.Select(a => $"• {a.GetDescription()}")).ToUpperInvariant(), 250));
                else
                    candidateAffixesLabel?.Set("NO AFFIXES");

                if (selectedCandidate.SpecialEffect != null)
                {
                    candidateSpecialLabel?.Set(PixelFont.Wrap($"★ {selectedCandidate.SpecialEffect.name}: {selectedCandidate.SpecialEffect.description}".ToUpperInvariant(), 250));
                    candidateSpecialLabel.Color = selectedCandidate.rarity == EquipmentRarity.Mythic ? Palette.SpecCrimson : Palette.AntiqueGold;
                }
                else
                {
                    candidateSpecialLabel?.Set("");
                }

                equipActionBtn.label.Color = Palette.Emerald;
            }
            else
            {
                // Current Equipped Inspect Mode
                if (currentEquipped != null)
                {
                    string curName = currentEquipped.upgradeLevel > 0 ? $"{currentEquipped.Definition?.name} +{currentEquipped.upgradeLevel}" : currentEquipped.Definition?.name ?? "ITEM";
                    detailTitle?.Set(PixelFont.Fit(curName.ToUpperInvariant(), 250));
                    detailTitle.Color = EquipmentRarities.GetColor(currentEquipped.rarity);
                    detailSub?.Set($"EQUIPPED {GetCoreSlotName(SelectedSlot)}  •  LEVEL {currentEquipped.level}  •  {currentEquipped.rarity.ToString().ToUpperInvariant()}");

                    foreach (var st in ComparedStats)
                    {
                        int currentVal = hero.Derived.Get(st);
                        string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                        comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct}", 124));
                        comparisonLabels[st].Color = Palette.WarmIvory;
                    }

                    if (currentEquipped.affixes != null && currentEquipped.affixes.Count > 0)
                        candidateAffixesLabel?.Set(PixelFont.Wrap(string.Join("\n", currentEquipped.affixes.Select(a => $"• {a.GetDescription()}")).ToUpperInvariant(), 250));
                    else
                        candidateAffixesLabel?.Set("NO AFFIXES");

                    if (currentEquipped.SpecialEffect != null)
                    {
                        candidateSpecialLabel?.Set(PixelFont.Wrap($"★ {currentEquipped.SpecialEffect.name}: {currentEquipped.SpecialEffect.description}".ToUpperInvariant(), 250));
                        candidateSpecialLabel.Color = currentEquipped.rarity == EquipmentRarity.Mythic ? Palette.SpecCrimson : Palette.AntiqueGold;
                    }
                    else
                    {
                        candidateSpecialLabel?.Set("");
                    }
                }
                else
                {
                    detailTitle?.Set("SLOT EMPTY");
                    detailTitle.Color = Palette.MutedGray;
                    detailSub?.Set($"NO ITEM EQUIPPED IN {GetCoreSlotName(SelectedSlot)}");
                    foreach (var st in ComparedStats)
                    {
                        int currentVal = hero.Derived.Get(st);
                        string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                        comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct}", 124));
                        comparisonLabels[st].Color = Palette.WarmIvory;
                    }
                    candidateAffixesLabel?.Set("");
                    candidateSpecialLabel?.Set("");
                }

                equipActionBtn.label.Color = Palette.Shade;
            }

            unequipActionBtn.label.Color = currentEquipped != null ? Palette.WarmIvory : Palette.Shade;
        }

        void OnEquipClicked()
        {
            var compatible = GetCompatibleInventoryItems();
            if (candidateIndex >= 0 && candidateIndex < compatible.Count)
            {
                var candidate = compatible[candidateIndex];
                if (EquipmentManager.EquipFromInventory(CurrentHero, SelectedSlot, candidate, inventory, out string err))
                {
                    feedbackLabel?.Set(PixelFont.Fit($"EQUIPPED {candidate.Definition?.name}!", 610));
                    candidateIndex = -1;
                    Refresh();
                }
                else
                {
                    feedbackLabel?.Set(PixelFont.Fit($"CANNOT EQUIP: {err}", 610));
                }
            }
        }

        void OnUnequipClicked()
        {
            if (EquipmentManager.UnequipToInventory(CurrentHero, SelectedSlot, inventory, out string err))
            {
                feedbackLabel?.Set(PixelFont.Fit($"UNEQUIPPED {GetCoreSlotName(SelectedSlot)} TO INVENTORY.", 610));
                Refresh();
            }
            else
            {
                feedbackLabel?.Set(PixelFont.Fit($"CANNOT UNEQUIP: {err}", 610));
            }
        }
    }
}
