using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Equipment Screen: 5 core slots, compatible gear list, live comparison.
    // Starlit Reliquary "F · Equipment" board at 640x360: slots + paper doll | compatible gear | gold compare panel.
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
        SpriteRenderer heroSprite;

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
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);

            // 1. Header: title, hero switcher, close hint
            titleLabel = Ui.Label(Root, "EQUIPMENT", 12, 10, Palette.Gold2, Align.Left, 2, FontKind.Serif);
            var prevHeroBtn = Ui.Button(Root, "<", 230, 8, 22, () => SwitchHero(-1), 20, showCursor: false);
            heroLabel = Ui.Label(Root, "", 332, 14, Palette.Mist1, Align.Center);
            var nextHeroBtn = Ui.Button(Root, ">", 412, 8, 22, () => SwitchHero(1), 20, showCursor: false);
            Ui.KeyHint(Root, 568, 12, "CLOSE", "ESC");

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);
            menu.Add(prevHeroBtn);
            menu.Add(nextHeroBtn);

            // 2. Equipped slots (paper doll column)
            Ui.Panel(Root, 12, 36, 166, 262);
            Ui.Header(Root, "EQUIPPED", 26, 44, 154);
            slotButtons.Clear();
            for (int i = 0; i < CoreSlots.Length; i++)
            {
                int sIdx = i;
                var btn = Ui.Button(Root, "", 20, 62 + i * 24, 150, () => SelectSlot(sIdx), 20);
                slotButtons.Add(btn);
                menu.Add(btn);
            }
            Ui.Image(Root, PixelArt.Gradient(150, 96, Palette.Hex(0x1A2550), Palette.Ink2), 20, 192, Ui.Panels + 1);
            Ui.Rect(Root, 76, 278, 40, 5, Palette.Ink4, Ui.Panels + 2);
            heroSprite = Ui.Image(Root, null, 75, 234, Ui.Widget);
            heroSprite.transform.localScale = new Vector3(2, 2, 1);

            // 3. Compatible gear for the selected slot
            Ui.Panel(Root, 184, 36, 220, 262);
            Ui.Header(Root, "COMPATIBLE GEAR", 198, 44, 208);
            candidateButtons.Clear();
            for (int i = 0; i < 5; i++)
            {
                int cIdx = i;
                var btn = Ui.Button(Root, "", 192, 62 + i * 24, 204, () => SelectCandidate(cIdx), 20);
                candidateButtons.Add(btn);
                menu.Add(btn);
            }
            feedbackLabel = Ui.Label(Root, "", 194, 186, Palette.Mist2);

            // 4. Compare panel (gold frame): title, stat deltas, affixes, special effect
            Ui.Panel(Root, 410, 36, 218, 262, Palette.Ink2, Palette.Gold3);
            detailTitle = Ui.Label(Root, "", 420, 46, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            detailSub = Ui.Label(Root, "", 420, 60, Palette.Mist3);
            Ui.Rect(Root, 416, 72, 206, 1, Palette.Ink4);

            comparisonLabels.Clear();
            for (int i = 0; i < ComparedStats.Length; i++)
            {
                var st = ComparedStats[i];
                comparisonLabels[st] = Ui.Label(Root, "", 420 + (i / 4) * 104, 78 + (i % 4) * 15, Palette.Mist1);
            }

            Ui.Rect(Root, 416, 140, 206, 1, Palette.Ink4);
            Ui.Label(Root, "AFFIXES & EFFECTS", 420, 146, Palette.Mist3);
            candidateAffixesLabel = Ui.Label(Root, "", 420, 158, Palette.ArcaneViolet);
            Ui.Rect(Root, 416, 226, 206, 1, Palette.Ink4);
            candidateSpecialLabel = Ui.Label(Root, "", 420, 232, Palette.Gold2);

            // 5. Actions
            equipActionBtn = Ui.Button(Root, "EQUIP", 410, 306, 104, OnEquipClicked, 22, showCursor: false, style: ButtonStyle.Primary);
            unequipActionBtn = Ui.Button(Root, "UNEQUIP", 520, 306, 108, OnUnequipClicked, 22, showCursor: false);
            var invBtn = Ui.Button(Root, "INVENTORY", 12, 306, 100, () => Game.Go(GameState.Inventory), 22, showCursor: false);
            var backBtn = Ui.Button(Root, "BACK", 118, 306, 70, () => Game.Go(returnState), 22, showCursor: false);
            int hx = Ui.KeyHint(Root, 12, 337, "HERO", "<", ">");
            Ui.KeyHint(Root, hx, 337, "SELECT", "ENTER");

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
                            EquipmentValidator.SlotMatches(e.Definition, slot, hero.classDef) &&
                            EquipmentValidator.Validate(e.Definition, slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id) == null)
                .ToList();
        }

        void Refresh()
        {
            var hero = CurrentHero;
            heroLabel?.Set(PixelFont.Fit($"{hero.name.ToUpperInvariant()}  -  {hero.classDef?.name.ToUpperInvariant()}", 150));
            if (hero.sprite != null) heroSprite.sprite = PixelArt.FromRows(hero.sprite);

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
                slotButtons[i].label.Set(PixelFont.Fit($"{prefix}{slotName,-6}{itemName}".ToUpperInvariant(), 140));
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
                    candidateButtons[i].label.Set(PixelFont.Fit($"{prefix}{name}".ToUpperInvariant(), 194));
                    candidateButtons[i].label.Color = EquipmentRarities.GetColor(item.rarity);
                }
                else
                {
                    candidateButtons[i].label.Set("  ---");
                    candidateButtons[i].label.Color = Palette.Ink5;
                }
            }

            // Right Panel: Comparison or Current Slot Details
            EquipmentInstance selectedCandidate = (candidateIndex >= 0 && candidateIndex < compatible.Count) ? compatible[candidateIndex] : null;
            EquipmentInstance currentEquipped = hero.equippedInstances.TryGetValue(SelectedSlot, out var cInst) ? cInst : null;

            if (selectedCandidate != null)
            {
                // Comparison Mode
                string candName = selectedCandidate.upgradeLevel > 0 ? $"{selectedCandidate.Definition?.name} +{selectedCandidate.upgradeLevel}" : selectedCandidate.Definition?.name ?? "ITEM";
                detailTitle?.Set(PixelFont.Fit($"COMPARE: {candName}".ToUpperInvariant(), 196));
                detailTitle.Color = EquipmentRarities.GetColor(selectedCandidate.rarity);
                detailSub?.Set(PixelFont.Fit($"{GetCoreSlotName(SelectedSlot)} - LV {selectedCandidate.level} - {selectedCandidate.rarity.ToString().ToUpperInvariant()}", 196));

                foreach (var st in ComparedStats)
                {
                    int delta = EquipmentManager.CalculateStatDelta(hero, SelectedSlot, selectedCandidate, st);
                    int currentVal = hero.Derived.Get(st);
                    string deltaStr = delta > 0 ? $"+{delta}" : delta.ToString();
                    Color deltaColor = delta > 0 ? Palette.Emerald : delta < 0 ? Palette.SpecCrimson : Palette.WarmIvory;
                    string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                    comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct} ({deltaStr})", 98));
                    comparisonLabels[st].Color = deltaColor;
                }

                if (selectedCandidate.affixes != null && selectedCandidate.affixes.Count > 0)
                    candidateAffixesLabel?.Set(PixelFont.Wrap(string.Join("\n", selectedCandidate.affixes.Select(a => $"- {a.GetDescription()}")).ToUpperInvariant(), 196));
                else
                    candidateAffixesLabel?.Set("NO AFFIXES");

                if (selectedCandidate.SpecialEffect != null)
                {
                    candidateSpecialLabel?.Set(PixelFont.Wrap($"* {selectedCandidate.SpecialEffect.name}: {selectedCandidate.SpecialEffect.description}".ToUpperInvariant(), 196));
                    candidateSpecialLabel.Color = selectedCandidate.rarity == EquipmentRarity.Mythic ? Palette.RarityMythic : Palette.RarityLegendary;
                }
                else
                {
                    candidateSpecialLabel?.Set("");
                }

                equipActionBtn.interactable = true;
                equipActionBtn.visual(Root.GetComponent<Menu>()?.Current == equipActionBtn);
            }
            else
            {
                // Current Equipped Inspect Mode
                if (currentEquipped != null)
                {
                    string curName = currentEquipped.upgradeLevel > 0 ? $"{currentEquipped.Definition?.name} +{currentEquipped.upgradeLevel}" : currentEquipped.Definition?.name ?? "ITEM";
                    detailTitle?.Set(PixelFont.Fit(curName.ToUpperInvariant(), 196));
                    detailTitle.Color = EquipmentRarities.GetColor(currentEquipped.rarity);
                    detailSub?.Set(PixelFont.Fit($"{GetCoreSlotName(SelectedSlot)} - LV {currentEquipped.level} - {currentEquipped.rarity.ToString().ToUpperInvariant()}", 196));

                    foreach (var st in ComparedStats)
                    {
                        int currentVal = hero.Derived.Get(st);
                        string isPct = DerivedStats.IsPercentage(st) ? "%" : "";
                        comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct}", 98));
                        comparisonLabels[st].Color = Palette.WarmIvory;
                    }

                    if (currentEquipped.affixes != null && currentEquipped.affixes.Count > 0)
                        candidateAffixesLabel?.Set(PixelFont.Wrap(string.Join("\n", currentEquipped.affixes.Select(a => $"- {a.GetDescription()}")).ToUpperInvariant(), 196));
                    else
                        candidateAffixesLabel?.Set("NO AFFIXES");

                    if (currentEquipped.SpecialEffect != null)
                    {
                        candidateSpecialLabel?.Set(PixelFont.Wrap($"* {currentEquipped.SpecialEffect.name}: {currentEquipped.SpecialEffect.description}".ToUpperInvariant(), 196));
                        candidateSpecialLabel.Color = currentEquipped.rarity == EquipmentRarity.Mythic ? Palette.RarityMythic : Palette.RarityLegendary;
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
                        comparisonLabels[st].Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName}: {currentVal}{isPct}", 98));
                        comparisonLabels[st].Color = Palette.WarmIvory;
                    }
                    candidateAffixesLabel?.Set("");
                    candidateSpecialLabel?.Set("");
                }

                equipActionBtn.interactable = false;
                equipActionBtn.visual(Root.GetComponent<Menu>()?.Current == equipActionBtn);
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
                    feedbackLabel?.Set(PixelFont.Fit($"EQUIPPED {candidate.Definition?.name}!", 200));
                    candidateIndex = -1;
                    Refresh();
                }
                else
                {
                    feedbackLabel?.Set(PixelFont.Fit($"CANNOT EQUIP: {err}", 200));
                }
            }
        }

        void OnUnequipClicked()
        {
            if (EquipmentManager.UnequipToInventory(CurrentHero, SelectedSlot, inventory, out string err))
            {
                feedbackLabel?.Set(PixelFont.Fit($"UNEQUIPPED {GetCoreSlotName(SelectedSlot)} TO INVENTORY.", 200));
                Refresh();
            }
            else
            {
                feedbackLabel?.Set(PixelFont.Fit($"CANNOT UNEQUIP: {err}", 200));
            }
        }
    }
}
