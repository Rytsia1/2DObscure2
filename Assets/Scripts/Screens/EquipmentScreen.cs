using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Equipment Screen conforming to Inventory & Equipment System GDD v1.0 (Section 13)
    // 320x180 Virtual Pixel Resolution, 5 Core Equipment Slots, Live Stat Comparison, Equip & Unequip.
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
            Backdrop.Create(Root, false, 8, Palette.Void, Palette.Night, Palette.Dusk);

            // 1. Top bar: Title, Hero Switcher, Capacity
            Ui.Panel(Root, 0, 0, Ui.Width, 18);
            titleLabel = Ui.Label(Root, "EQUIPMENT LOADOUT", 6, 5, Palette.Gold);
            heroLabel = Ui.Label(Root, "", 160, 5, Palette.Parchment, Align.Center);

            var prevHeroBtn = Ui.Button(Root, "<", 100, 2, 18, () => SwitchHero(-1), 14, showCursor: false);
            var nextHeroBtn = Ui.Button(Root, ">", 220, 2, 18, () => SwitchHero(1), 14, showCursor: false);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);
            menu.Add(prevHeroBtn);
            menu.Add(nextHeroBtn);

            // 2. Left Panel: 5 Core Equipment Slots (Top) & Compatible Inventory Items (Bottom)
            Ui.Panel(Root, 2, 19, 150, 132);
            Ui.Label(Root, "EQUIPPED SLOTS", 6, 21, Palette.Gold);

            slotButtons.Clear();
            for (int i = 0; i < CoreSlots.Length; i++)
            {
                int sIdx = i;
                int y = 31 + i * 14;
                var btn = Ui.Button(Root, "", 6, y, 142, () => SelectSlot(sIdx), 12);
                slotButtons.Add(btn);
                menu.Add(btn);
            }

            Ui.Label(Root, "AVAILABLE GEAR", 6, 102, Palette.Gold);
            candidateButtons.Clear();
            for (int i = 0; i < 2; i++)
            {
                int cIdx = i;
                int y = 112 + i * 14;
                var btn = Ui.Button(Root, "", 6, y, 142, () => SelectCandidate(cIdx), 12);
                candidateButtons.Add(btn);
                menu.Add(btn);
            }

            // 3. Right Panel: Stat Comparison & Equipment Details
            Ui.Panel(Root, 154, 19, 164, 132);
            detailTitle = Ui.Label(Root, "", 158, 22, Palette.Gold);
            detailSub = Ui.Label(Root, "", 158, 31, Palette.Parchment);

            comparisonLabels.Clear();
            for (int i = 0; i < ComparedStats.Length; i++)
            {
                var st = ComparedStats[i];
                int col = i / 4;
                int row = i % 4;
                int x = 158 + col * 80;
                int y = 43 + row * 11;
                comparisonLabels[st] = Ui.Label(Root, "", x, y, Palette.Bone);
            }

            candidateAffixesLabel = Ui.Label(Root, "", 158, 88, Palette.Lilac);
            candidateSpecialLabel = Ui.Label(Root, "", 158, 122, Palette.Gold);

            // 4. Bottom Panel: Actions
            Ui.Panel(Root, 0, 152, Ui.Width, 28);
            feedbackLabel = Ui.Label(Root, "", 6, 154, Palette.Parchment);

            equipActionBtn = Ui.Button(Root, "EQUIP", 6, 163, 48, OnEquipClicked, 13, showCursor: false);
            unequipActionBtn = Ui.Button(Root, "UNEQUIP", 58, 163, 56, OnUnequipClicked, 13, showCursor: false);
            var invBtn = Ui.Button(Root, "INVENTORY", 118, 163, 68, () => Game.Go(GameState.Inventory), 13, showCursor: false);
            var backBtn = Ui.Button(Root, "BACK", 276, 163, 38, () => Game.Go(returnState), 13, showCursor: false);

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
            heroLabel?.Set(PixelFont.Fit($"{hero.name.ToUpperInvariant()} ({hero.classDef?.name.ToUpperInvariant()})", 118));

            // Populate 5 core slots
            for (int i = 0; i < CoreSlots.Length; i++)
            {
                var s = CoreSlots[i];
                string slotName = GetCoreSlotName(s);
                EquipmentInstance inst = hero.equippedInstances.TryGetValue(s, out var ei) ? ei : null;
                EquipmentDef legacyDef = hero.equipped.TryGetValue(s, out var ld) ? ld : null;

                string itemName = "EMPTY";
                Color itemColor = Palette.Ash;
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
                slotButtons[i].label.Set(PixelFont.Fit($"{prefix}{slotName}: {itemName}".ToUpperInvariant(), 138));
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
                    candidateButtons[i].label.Set(PixelFont.Fit($"{prefix}{name}".ToUpperInvariant(), 138));
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
                detailTitle?.Set(PixelFont.Fit($"COMPARE: {candName}".ToUpperInvariant(), 160));
                detailTitle.Color = EquipmentRarities.GetColor(selectedCandidate.rarity);
                detailSub?.Set($"SLOT: {GetCoreSlotName(SelectedSlot)}  LVL {selectedCandidate.level}");

                foreach (var st in ComparedStats)
                {
                    int delta = EquipmentManager.CalculateStatDelta(hero, SelectedSlot, selectedCandidate, st);
                    int currentVal = hero.Derived.Get(st);
                    string deltaStr = delta > 0 ? $"+{delta}" : delta.ToString();
                    Color deltaColor = delta > 0 ? Palette.Moss : delta < 0 ? Palette.Crimson : Palette.Bone;
                    string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                    comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct} ({deltaStr})", 78));
                    comparisonLabels[st].Color = deltaColor;
                }

                if (selectedCandidate.affixes != null && selectedCandidate.affixes.Count > 0)
                    candidateAffixesLabel?.Set(PixelFont.Wrap(string.Join("\n", selectedCandidate.affixes.Select(a => $"• {a.GetDescription()}")).ToUpperInvariant(), 160));
                else
                    candidateAffixesLabel?.Set("NO AFFIXES");

                if (selectedCandidate.SpecialEffect != null)
                {
                    candidateSpecialLabel?.Set(PixelFont.Wrap($"★ {selectedCandidate.SpecialEffect.name}: {selectedCandidate.SpecialEffect.description}".ToUpperInvariant(), 160));
                    candidateSpecialLabel.Color = selectedCandidate.rarity == EquipmentRarity.Mythic ? Palette.Crimson : Palette.Gold;
                }
                else
                {
                    candidateSpecialLabel?.Set("");
                }

                equipActionBtn.label.Color = Palette.Moss;
            }
            else
            {
                // Current Equipped Inspect Mode
                if (currentEquipped != null)
                {
                    string curName = currentEquipped.upgradeLevel > 0 ? $"{currentEquipped.Definition?.name} +{currentEquipped.upgradeLevel}" : currentEquipped.Definition?.name ?? "ITEM";
                    detailTitle?.Set(PixelFont.Fit(curName.ToUpperInvariant(), 160));
                    detailTitle.Color = EquipmentRarities.GetColor(currentEquipped.rarity);
                    detailSub?.Set($"EQUIPPED {GetCoreSlotName(SelectedSlot)} (LVL {currentEquipped.level})");

                    foreach (var st in ComparedStats)
                    {
                        int currentVal = hero.Derived.Get(st);
                        string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                        comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct}", 78));
                        comparisonLabels[st].Color = Palette.Bone;
                    }

                    if (currentEquipped.affixes != null && currentEquipped.affixes.Count > 0)
                        candidateAffixesLabel?.Set(PixelFont.Wrap(string.Join("\n", currentEquipped.affixes.Select(a => $"• {a.GetDescription()}")).ToUpperInvariant(), 160));
                    else
                        candidateAffixesLabel?.Set("NO AFFIXES");

                    if (currentEquipped.SpecialEffect != null)
                    {
                        candidateSpecialLabel?.Set(PixelFont.Wrap($"★ {currentEquipped.SpecialEffect.name}: {currentEquipped.SpecialEffect.description}".ToUpperInvariant(), 160));
                        candidateSpecialLabel.Color = currentEquipped.rarity == EquipmentRarity.Mythic ? Palette.Crimson : Palette.Gold;
                    }
                    else
                    {
                        candidateSpecialLabel?.Set("");
                    }
                }
                else
                {
                    detailTitle?.Set("SLOT EMPTY");
                    detailTitle.Color = Palette.Ash;
                    detailSub?.Set($"NO ITEM EQUIPPED IN {GetCoreSlotName(SelectedSlot)}");
                    foreach (var st in ComparedStats)
                    {
                        int currentVal = hero.Derived.Get(st);
                        string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                        comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct}", 78));
                        comparisonLabels[st].Color = Palette.Bone;
                    }
                    candidateAffixesLabel?.Set("");
                    candidateSpecialLabel?.Set("");
                }

                equipActionBtn.label.Color = Palette.Shade;
            }

            unequipActionBtn.label.Color = currentEquipped != null ? Palette.Bone : Palette.Shade;
        }

        void OnEquipClicked()
        {
            var compatible = GetCompatibleInventoryItems();
            if (candidateIndex >= 0 && candidateIndex < compatible.Count)
            {
                var candidate = compatible[candidateIndex];
                if (EquipmentManager.EquipFromInventory(CurrentHero, SelectedSlot, candidate, inventory, out string err))
                {
                    feedbackLabel?.Set(PixelFont.Fit($"EQUIPPED {candidate.Definition?.name}!", 308));
                    candidateIndex = -1;
                    Refresh();
                }
                else
                {
                    feedbackLabel?.Set(PixelFont.Fit($"CANNOT EQUIP: {err}", 308));
                }
            }
        }

        void OnUnequipClicked()
        {
            if (EquipmentManager.UnequipToInventory(CurrentHero, SelectedSlot, inventory, out string err))
            {
                feedbackLabel?.Set(PixelFont.Fit($"UNEQUIPPED {GetCoreSlotName(SelectedSlot)} TO INVENTORY.", 308));
                Refresh();
            }
            else
            {
                feedbackLabel?.Set(PixelFont.Fit($"CANNOT UNEQUIP: {err}", 308));
            }
        }
    }
}
