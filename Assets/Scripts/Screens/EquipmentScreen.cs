using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Equipment Screen — Starlit Reliquary "F · Equipment" board at 640x360:
    // header (back, party portraits, bag count) | paper doll with all 8 slots |
    // gear list for the selected slot (locked rows say why) | gold compare panel with old > new stats.
    public class EquipmentScreen : GameScreen
    {
        const int RowCount = 9, RowX = 192, RowY = 60, RowW = 234, RowH = 25, RowStep = 27;

        readonly GameState returnState;
        readonly InventoryRuntime inventory;
        readonly List<Unit> party;

        int heroIndex = 0;
        int selectedSlotIndex = 0;
        int candidateIndex = -1;
        string feedback;

        public static readonly EquipmentSlot[] CoreSlots = EquipmentSlots.All;

        public static string GetCoreSlotName(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.MainHand => "MAIN HAND",
            EquipmentSlot.SecondHand => "OFF HAND",
            EquipmentSlot.HeadArmor => "HEAD",
            EquipmentSlot.BodyArmor => "BODY",
            EquipmentSlot.LegArmor => "LEGS",
            EquipmentSlot.FootArmor => "FEET",
            EquipmentSlot.Accessory1 => "ACC 1",
            EquipmentSlot.Accessory2 => "ACC 2",
            _ => slot.ToString().ToUpperInvariant()
        };

        // Paper doll positions, in CoreSlots order: hands and accessories down the right, armor down the left.
        static readonly Vector2Int[] SlotPos =
        {
            new Vector2Int(130, 62), new Vector2Int(130, 120),                          // Main Hand, Second Hand
            new Vector2Int(20, 62), new Vector2Int(20, 120),                            // Head, Body
            new Vector2Int(20, 178), new Vector2Int(20, 236),                           // Leg, Foot
            new Vector2Int(130, 178), new Vector2Int(130, 236),                         // Accessory 1, Accessory 2
        };

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

        static readonly Color Up = Palette.Hex(0x7FF0AD), Down = Palette.Hex(0xFF9A8C);

        public Unit CurrentHero => party[Math.Clamp(heroIndex, 0, party.Count - 1)];
        public EquipmentSlot SelectedSlot => CoreSlots[Math.Clamp(selectedSlotIndex, 0, CoreSlots.Length - 1)];

        class SlotView { public Selectable sel; public SpriteRenderer box, icon, chip; public Label letter, name; public bool focused; }
        class RowView { public Selectable sel; public SpriteRenderer box, well, icon, pointer; public Label name, sub, right; public bool focused; }
        class RowData { public EquipmentInstance item; public EquipmentDef def; public bool equipped; public string lockReason; }

        Menu menu;
        Label heroLabel, slotCountLabel, listTitle, moreLabel, note;
        SpriteRenderer heroSprite;
        Selectable bagBtn, equipActionBtn, unequipActionBtn;
        readonly SpriteRenderer[] portraitBoxes = new SpriteRenderer[4];
        readonly List<SlotView> slotViews = new List<SlotView>();
        readonly List<RowView> rowViews = new List<RowView>();
        List<RowData> rows = new List<RowData>();

        // Compare panel
        Label eqName, candName, eqLetter, candLetter, eqRarity, candRarity, effectName, effectDesc;
        SpriteRenderer eqChip, candChip, effectOuter, effectInner;
        readonly Label[] statA = new Label[8], statB = new Label[8], statD = new Label[8];
        readonly Label[] affixLines = new Label[2];

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
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);

            // 1. Header: back, title, party portrait switcher, equipment bag per FELNA — UI System (Page 11)
            var backBtn = Ui.Button(Root, "BACK", 12, 8, 46, () => Game.Go(returnState), 20, showCursor: false, font: FontKind.DotGothic);
            Ui.Label(Root, "EQUIPMENT", 66, 10, Palette.Gold2, Align.Left, 2, FontKind.DotGothic);
            var prevHeroBtn = Ui.Button(Root, "<", 214, 9, 20, () => SwitchHero(-1), 20, showCursor: false);
            for (int i = 0; i < party.Count && i < portraitBoxes.Length; i++)
            {
                int px = 238 + i * 26;
                portraitBoxes[i] = Ui.Image(Root, null, px, 6, Ui.Widget);
                var face = party[i].sprite;
                if (face == null) continue;
                var s = PixelArt.FromRows(face);
                Ui.Image(Root, s, px + (22 - (int)s.rect.width) / 2, 30 - (int)s.rect.height, Ui.Widget + 1);
            }
            var nextHeroBtn = Ui.Button(Root, ">", 342, 9, 20, () => SwitchHero(1), 20, showCursor: false);
            bagBtn = Ui.Button(Root, "", 520, 8, 108, () => Game.Go(GameState.Inventory), 20, showCursor: false);

            // 2. Paper doll: all 8 slots around the hero
            Ui.Panel(Root, 12, 36, 166, 300);
            heroLabel = Ui.Header(Root, "", 26, 44, 154);
            slotCountLabel = Ui.Label(Root, "", 172, 44, Palette.Mist2, Align.Right);
            Ui.Image(Root, PixelArt.Disc(40, Color.white), 89, 150, Ui.Panels + 1).color = new Color(0.45f, 0.55f, 1f, 0.12f);
            Ui.Rect(Root, 69, 180, 40, 4, Palette.Ink4, Ui.Panels + 2);
            heroSprite = Ui.Image(Root, null, 0, 0, Ui.Widget);
            heroSprite.transform.localScale = new Vector3(3, 3, 1);
            for (int i = 0; i < CoreSlots.Length; i++) slotViews.Add(CreateSlot(i));
            Ui.Rect(Root, 14, 298, 162, 1, Palette.Ink4);
            unequipActionBtn = Ui.Button(Root, "UNEQUIP", 20, 306, 150, OnUnequipClicked, 22, showCursor: false);

            // 3. Gear list for the selected slot
            Ui.Panel(Root, 184, 36, 248, 300);
            listTitle = Ui.Header(Root, "", 198, 44, 236);
            Ui.Label(Root, "SORT: RARITY", 426, 44, Palette.Mist3, Align.Right);
            for (int i = 0; i < RowCount; i++) rowViews.Add(CreateRow(i));
            moreLabel = Ui.Label(Root, "", 198, 312, Palette.Mist3);

            // 4. Compare panel (gold frame)
            Ui.Panel(Root, 438, 36, 190, 300, Palette.Ink2, Palette.Gold3);
            Ui.Header(Root, "COMPARE", 452, 44, 178);
            Ui.Label(Root, "EQUIPPED", 446, 62, Palette.Mist3);
            Ui.Label(Root, "CANDIDATE", 536, 62, Palette.Mist3);
            eqName = Ui.Label(Root, "", 446, 73, Palette.Mist1);
            candName = Ui.Label(Root, "", 536, 73, Palette.Mist1);
            eqChip = Ui.Image(Root, null, 446, 84, Ui.Widget);
            eqLetter = Ui.Label(Root, "", 448, 85, Palette.Ink0, order: Ui.Widget + 1);
            eqRarity = Ui.Label(Root, "", 458, 85, Palette.Mist2);
            candChip = Ui.Image(Root, null, 536, 84, Ui.Widget);
            candLetter = Ui.Label(Root, "", 538, 85, Palette.Ink0, order: Ui.Widget + 1);
            candRarity = Ui.Label(Root, "", 548, 85, Palette.Mist2);
            Ui.Rect(Root, 440, 98, 186, 1, Palette.Ink4);

            Ui.Label(Root, "BASE STATS", 446, 103, Palette.Mist3);
            for (int i = 0; i < ComparedStats.Length; i++)
            {
                int y = 115 + i * 12;
                Ui.Label(Root, PixelFont.Fit(Database.DerivedStatDefs[ComparedStats[i]].shortName.ToUpperInvariant(), 64), 446, y, Palette.Mist2);
                statA[i] = Ui.Label(Root, "", 548, y, Palette.Mist3, Align.Right);
                Ui.Label(Root, ">", 551, y, Palette.Mist3);
                statB[i] = Ui.Label(Root, "", 590, y, Palette.Mist1, Align.Right);
                statD[i] = Ui.Label(Root, "", 620, y, Palette.Mist3, Align.Right);
                Ui.Rect(Root, 444, y + 10, 178, 1, Palette.Ink3);
            }
            Ui.Label(Root, "AFFIXES & EFFECTS", 446, 214, Palette.Mist3);
            for (int i = 0; i < affixLines.Length; i++) affixLines[i] = Ui.Label(Root, "", 446, 226 + i * 10, Palette.Mist2);
            effectOuter = Ui.Image(Root, null, 444, 248, Ui.Widget);
            effectInner = Ui.Image(Root, null, 446, 250, Ui.Widget + 1);
            effectName = Ui.Label(Root, "", 450, 254, Palette.RarityLegendary, order: Ui.Widget + 2);
            effectDesc = Ui.Label(Root, "", 450, 265, Palette.Mist1, order: Ui.Widget + 2);
            note = Ui.Label(Root, "", 446, 288, Palette.Mist3);
            equipActionBtn = Ui.Button(Root, "EQUIP", 446, 304, 174, OnEquipClicked, 24, showCursor: false, style: ButtonStyle.Primary);

            // Keyboard order: slots, gear list, actions, header controls
            foreach (var v in slotViews) menu.Add(v.sel);
            foreach (var r in rowViews) menu.Add(r.sel);
            menu.Add(equipActionBtn);
            menu.Add(unequipActionBtn);
            menu.Add(prevHeroBtn);
            menu.Add(nextHeroBtn);
            menu.Add(bagBtn);
            menu.Add(backBtn);

            Refresh();
        }

        SlotView CreateSlot(int index)
        {
            var p = SlotPos[index];
            var go = new GameObject("Slot:" + CoreSlots[index]);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(p.x, -p.y, 0);
            var v = new SlotView
            {
                box = Ui.Image(go.transform, null, 0, 0, Ui.Widget),
                icon = Ui.Image(go.transform, null, 8, 8, Ui.Widget + 1),
                chip = Ui.Image(go.transform, null, 0, 0, Ui.Widget + 2),
                letter = Ui.Label(go.transform, "", 2, 1, Palette.Ink0, order: Ui.Widget + 3),
                name = Ui.Label(Root, GetCoreSlotName(CoreSlots[index]), p.x + 14, p.y + 31, Palette.Mist3, Align.Center),
            };
            v.sel = go.AddComponent<Selectable>();
            v.sel.size = new Vector2Int(28, 28);
            v.sel.onClick = () => SelectSlot(index);
            v.sel.visual = on => { v.focused = on; PaintSlot(index); };
            return v;
        }

        RowView CreateRow(int index)
        {
            var go = new GameObject("GearRow:" + index);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(RowX, -(RowY + index * RowStep), 0);
            var r = new RowView
            {
                box = Ui.Image(go.transform, null, 0, 0, Ui.Widget),
                well = Ui.Image(go.transform, null, 2, 2, Ui.Widget + 1),
                icon = Ui.Image(go.transform, null, 6, 6, Ui.Widget + 2),
                pointer = Ui.Image(go.transform, PixelArt.Pointer, -10, 9, Ui.Widget + 2),
                name = Ui.Label(go.transform, "", 28, 4, Palette.Mist1, order: Ui.Widget + 2),
                sub = Ui.Label(go.transform, "", 28, 14, Palette.Mist3, order: Ui.Widget + 2),
                right = Ui.Label(go.transform, "", RowW - 4, 9, Palette.Mist3, Align.Right, order: Ui.Widget + 2),
            };
            r.sel = go.AddComponent<Selectable>();
            r.sel.size = new Vector2Int(RowW, RowH);
            r.sel.onClick = () => SelectCandidate(index);
            r.sel.visual = on => { r.focused = on; PaintRow(index); };
            return r;
        }

        void SwitchHero(int delta)
        {
            if (party.Count == 0) return;
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            selectedSlotIndex = 0;
            candidateIndex = -1;
            feedback = null;
            Refresh();
        }

        void SelectSlot(int index)
        {
            selectedSlotIndex = index;
            candidateIndex = -1;
            feedback = null;
            Refresh();
        }

        void SelectCandidate(int index)
        {
            candidateIndex = index < rows.Count && !rows[index].equipped && rows[index].lockReason == null ? index : -1;
            feedback = null;
            Refresh();
        }

        (EquipmentInstance inst, EquipmentDef def) Equipped(EquipmentSlot slot)
        {
            var hero = CurrentHero;
            var inst = hero.equippedInstances.TryGetValue(slot, out var ei) ? ei : null;
            var def = inst?.Definition ?? (hero.equipped.TryGetValue(slot, out var ld) ? ld : null);
            return (inst, def);
        }

        static string ItemName(EquipmentInstance inst, EquipmentDef def)
        {
            string name = inst?.Definition?.name ?? def?.name ?? "ITEM";
            return (inst != null && inst.upgradeLevel > 0 ? $"{name} +{inst.upgradeLevel}" : name).ToUpperInvariant();
        }

        static string RarityLetter(EquipmentRarity r) => r.ToString().Substring(0, 1);

        // Equipped item first, then compatible bag items by rarity, then items this hero cannot use (with the reason).
        List<RowData> BuildRows()
        {
            var hero = CurrentHero;
            var slot = SelectedSlot;
            var list = new List<RowData>();
            var (eqInst, eqDef) = Equipped(slot);
            if (eqDef != null) list.Add(new RowData { item = eqInst, def = eqDef, equipped = true });

            var bag = inventory.equipment
                .Where(e => !e.isEquipped && e.Definition != null && EquipmentValidator.SlotMatches(e.Definition, slot, hero.classDef))
                .Select(e => new RowData
                {
                    item = e,
                    def = e.Definition,
                    lockReason = EquipmentValidator.Validate(e.Definition, slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id),
                })
                .OrderBy(d => d.lockReason != null)
                .ThenByDescending(d => d.item.rarity)
                .ThenBy(d => d.def.name);
            list.AddRange(bag);
            return list;
        }

        // The stat a row's change column reports: the hero's best power for hands, defense for armor, the biggest swing for accessories.
        DerivedStatId KeyStat(EquipmentSlot slot, EquipmentInstance candidate)
        {
            var hero = CurrentHero;
            if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.SecondHand)
                return new[] { DerivedStatId.PhysicalPower, DerivedStatId.RangedPower, DerivedStatId.MagicPower }.OrderByDescending(s => hero.Derived.Get(s)).First();
            if (!EquipmentValidator.IsAccessorySlot(slot)) return DerivedStatId.PhysicalDefense;
            if (candidate == null) return DerivedStatId.MaxHP;
            return ComparedStats.OrderByDescending(s => Math.Abs(EquipmentManager.CalculateStatDelta(hero, slot, candidate, s))).First();
        }

        static string StatValue(DerivedStatId st, int v) => DerivedStats.IsPercentage(st) ? $"{v}%" : v.ToString();

        void Refresh()
        {
            var hero = CurrentHero;
            hero.RecalculateStats();
            rows = BuildRows();
            if (candidateIndex >= rows.Count) candidateIndex = -1;

            // Header
            for (int i = 0; i < portraitBoxes.Length; i++)
                if (portraitBoxes[i] != null)
                    portraitBoxes[i].sprite = PixelArt.Badge(22, 26, Palette.Ink1, i == heroIndex ? Palette.Gold2 : Palette.Ink5);
            bagBtn.label.Set($"BAG {inventory.EquipmentCount}/{InventoryRuntime.MaxEquipmentCapacity}");

            // Paper doll
            heroLabel.Set(PixelFont.Fit($"{hero.name} - {hero.classDef?.name ?? hero.role}".ToUpperInvariant(), 110));
            slotCountLabel.Set($"{CoreSlots.Count(s => Equipped(s).def != null)} / {CoreSlots.Length}");
            if (hero.sprite != null)
            {
                heroSprite.sprite = PixelArt.FromRows(hero.sprite);
                heroSprite.transform.localPosition = new Vector3(89 - 3 * (int)heroSprite.sprite.rect.width / 2, -(178 - 3 * (int)heroSprite.sprite.rect.height), 0);
            }
            for (int i = 0; i < slotViews.Count; i++) PaintSlot(i);
            unequipActionBtn.interactable = Equipped(SelectedSlot).def != null;
            unequipActionBtn.visual(menu.Current == unequipActionBtn);

            // Gear list
            listTitle.Set(PixelFont.Fit($"{GetCoreSlotName(SelectedSlot)} FOR {hero.name}".ToUpperInvariant(), 140));
            for (int i = 0; i < rowViews.Count; i++)
            {
                var d = i < rows.Count ? rows[i] : null;
                rowViews[i].sel.interactable = d != null && !d.equipped && d.lockReason == null;
                PaintRow(i);
            }
            // ponytail: fixed 9-row window; add paging when a slot regularly holds more than 9 pieces
            moreLabel.Set(rows.Count > RowCount ? $"+{rows.Count - RowCount} MORE IN THE BAG" : rows.Count == 0 ? "NOTHING IN THE BAG FITS THIS SLOT." : "");

            PaintCompare();
        }

        void PaintSlot(int index)
        {
            var v = slotViews[index];
            var slot = CoreSlots[index];
            var (inst, def) = Equipped(slot);
            bool selected = index == selectedSlotIndex;
            Color rc = def != null ? EquipmentRarities.GetColor(inst?.rarity ?? def.rarity) : Palette.Ink5;
            Color border = v.focused ? Palette.Gold1 : selected ? Palette.Gold2 : def != null ? Color.Lerp(rc, Palette.Ink4, 0.45f) : Palette.Ink4;
            v.box.sprite = PixelArt.Badge(28, 28, selected ? Palette.Hex(0x2B2210) : Palette.Ink0, border);
            v.icon.sprite = PixelArt.FromRows(Art.GearIcon(slot, def));
            v.icon.color = def != null ? rc : Palette.Ink4;
            v.chip.sprite = def != null ? PixelArt.Badge(9, 9, rc, rc) : null;
            v.letter.Set(def != null ? RarityLetter(inst?.rarity ?? def.rarity) : "");
            v.name.Color = selected ? Palette.Gold1 : Palette.Mist3;
        }

        void PaintRow(int index)
        {
            var r = rowViews[index];
            var d = index < rows.Count ? rows[index] : null;
            r.pointer.enabled = r.focused && d != null;
            if (d == null)
            {
                r.box.sprite = r.well.sprite = r.icon.sprite = null;
                r.name.Set(""); r.sub.Set(""); r.right.Set("");
                return;
            }

            var hero = CurrentHero;
            var slot = SelectedSlot;
            var rarity = d.item?.rarity ?? d.def.rarity;
            Color rc = EquipmentRarities.GetColor(rarity);
            bool chosen = index == candidateIndex;
            r.box.sprite = r.focused || chosen ? PixelArt.Badge(RowW, RowH, Palette.Hex(0x2B2210), r.focused ? Palette.Gold1 : Palette.Gold2)
                : d.equipped ? PixelArt.Badge(RowW, RowH, Palette.Ink3, Palette.Ink4)
                : d.lockReason != null ? PixelArt.Badge(RowW, RowH, Palette.Ink1, Palette.Ink5)
                : null;
            r.well.sprite = PixelArt.Badge(21, 21, Palette.Ink0, Color.Lerp(rc, Palette.Ink4, 0.45f));
            r.icon.sprite = PixelArt.FromRows(Art.GearIcon(slot, d.def));
            r.icon.color = d.lockReason != null ? Color.Lerp(rc, Palette.Ink2, 0.5f) : rc;
            r.name.Set(PixelFont.Fit(ItemName(d.item, d.def), 150));
            r.name.Color = d.lockReason != null ? Color.Lerp(rc, Palette.Mist3, 0.5f) : rc;

            if (d.equipped)
            {
                var st = KeyStat(slot, null);
                r.sub.Set(PixelFont.Fit($"EQUIPPED - {Database.DerivedStatDefs[st].shortName} {StatValue(st, hero.Derived.Get(st))}".ToUpperInvariant(), 160));
                r.sub.Color = Palette.Mist1;
                r.right.Set("E");
                r.right.Color = Palette.Mist1;
            }
            else if (d.lockReason != null)
            {
                r.sub.Set(PixelFont.Fit(d.lockReason.ToUpperInvariant(), 170));
                r.sub.Color = Down;
                r.right.Set("LOCKED");
                r.right.Color = Palette.Mist3;
            }
            else
            {
                var st = KeyStat(slot, d.item);
                int delta = EquipmentManager.CalculateStatDelta(hero, slot, d.item, st);
                int affixes = d.item.affixes?.Count ?? 0;
                string extra = affixes > 0 ? $" - {affixes} AFFIX{(affixes > 1 ? "ES" : "")}" : "";
                r.sub.Set(PixelFont.Fit($"{Database.DerivedStatDefs[st].shortName} {StatValue(st, hero.Derived.Get(st) + delta)}{extra}".ToUpperInvariant(), 160));
                r.sub.Color = Palette.Mist3;
                r.right.Set(delta > 0 ? $"+{delta}" : delta < 0 ? delta.ToString() : "=0");
                r.right.Color = delta > 0 ? Up : delta < 0 ? Down : Palette.Mist3;
            }
        }

        void PaintCompare()
        {
            var hero = CurrentHero;
            var slot = SelectedSlot;
            var (eqInst, eqDef) = Equipped(slot);
            var cand = candidateIndex >= 0 ? rows[candidateIndex].item : null;

            SetItemColumn(eqName, eqChip, eqLetter, eqRarity, eqInst, eqDef, "EMPTY");
            SetItemColumn(candName, candChip, candLetter, candRarity, cand, cand?.Definition, "SELECT AN ITEM");

            for (int i = 0; i < ComparedStats.Length; i++)
            {
                var st = ComparedStats[i];
                int a = hero.Derived.Get(st);
                statA[i].Set(StatValue(st, a));
                if (cand == null)
                {
                    statB[i].Set("-");
                    statD[i].Set("");
                    continue;
                }
                int delta = EquipmentManager.CalculateStatDelta(hero, slot, cand, st);
                statB[i].Set(StatValue(st, a + delta));
                statD[i].Set(delta > 0 ? $"+{delta}" : delta < 0 ? delta.ToString() : "=0");
                statD[i].Color = delta > 0 ? Up : delta < 0 ? Down : Palette.Mist3;
            }

            // Affixes: what the swap loses and gains; without a candidate, what is equipped.
            var lines = new List<(string text, Color color)>();
            var eqAffixes = eqInst?.affixes?.Select(x => x.GetDescription().ToUpperInvariant()).ToList() ?? new List<string>();
            if (cand != null)
            {
                var candAffixes = cand.affixes?.Select(x => x.GetDescription().ToUpperInvariant()).ToList() ?? new List<string>();
                lines.AddRange(eqAffixes.Except(candAffixes).Select(x => ("LOSE " + x, Down)));
                lines.AddRange(candAffixes.Except(eqAffixes).Select(x => ("GAIN " + x, Up)));
            }
            else lines.AddRange(eqAffixes.Select(x => ("- " + x, Palette.ArcaneViolet)));
            if (lines.Count == 0) lines.Add(("NO AFFIXES", Palette.Mist3));
            for (int i = 0; i < affixLines.Length; i++)
            {
                string text = i < lines.Count ? lines[i].text : "";
                if (i == affixLines.Length - 1 && lines.Count > affixLines.Length) text = $"+{lines.Count - i} MORE";
                affixLines[i].Set(PixelFont.Fit(text, 178));
                if (i < lines.Count) affixLines[i].Color = lines[i].color;
            }

            // Legendary / Mythic special effect in a double frame
            var effectOwner = cand ?? eqInst;
            var effect = effectOwner?.SpecialEffect;
            Color ec = effectOwner != null && effectOwner.rarity == EquipmentRarity.Mythic ? Palette.RarityMythic : Palette.RarityLegendary;
            effectOuter.sprite = effect != null ? PixelArt.Badge(178, 32, Palette.Hex(0x241707), ec) : null;
            effectInner.sprite = effect != null ? PixelArt.Badge(174, 28, Palette.Hex(0x241707), ec) : null;
            effectName.Set(effect != null ? PixelFont.Fit($"{effectOwner.rarity} - {effect.name}".ToUpperInvariant(), 168) : "");
            effectName.Color = ec;
            effectDesc.Set(effect != null ? PixelFont.Fit(effect.description.ToUpperInvariant(), 168) : "");

            note.Set(PixelFont.Fit(feedback
                ?? (cand == null ? "PICK GEAR FROM THE LIST TO COMPARE."
                : eqDef != null ? $"{ItemName(eqInst, eqDef)} RETURNS TO THE BAG."
                : "THE SLOT IS EMPTY - NOTHING RETURNS."), 178));

            equipActionBtn.label.Set(cand != null ? PixelFont.Fit("EQUIP " + ItemName(cand, cand.Definition), 160) : "EQUIP");
            equipActionBtn.interactable = cand != null;
            equipActionBtn.visual(menu.Current == equipActionBtn);
        }

        static void SetItemColumn(Label name, SpriteRenderer chip, Label letter, Label rarityLabel, EquipmentInstance inst, EquipmentDef def, string empty)
        {
            if (def == null)
            {
                name.Set(empty);
                name.Color = Palette.Mist3;
                chip.sprite = null;
                letter.Set("");
                rarityLabel.Set("");
                return;
            }
            var rarity = inst?.rarity ?? def.rarity;
            Color rc = EquipmentRarities.GetColor(rarity);
            name.Set(PixelFont.Fit(ItemName(inst, def), 84));
            name.Color = rc;
            chip.sprite = PixelArt.Badge(9, 9, rc, rc);
            letter.Set(RarityLetter(rarity));
            rarityLabel.Set(rarity.ToString().ToUpperInvariant());
            rarityLabel.Color = rc;
        }

        void OnEquipClicked()
        {
            if (candidateIndex < 0) return;
            var candidate = rows[candidateIndex].item;
            if (EquipmentManager.EquipFromInventory(CurrentHero, SelectedSlot, candidate, inventory, out string err))
            {
                candidateIndex = -1;
                feedback = $"EQUIPPED {ItemName(candidate, candidate.Definition)}.";
            }
            else feedback = $"CANNOT EQUIP: {err}".ToUpperInvariant();
            Refresh();
        }

        void OnUnequipClicked()
        {
            feedback = EquipmentManager.UnequipToInventory(CurrentHero, SelectedSlot, inventory, out string err)
                ? $"UNEQUIPPED {GetCoreSlotName(SelectedSlot)} TO THE BAG."
                : $"CANNOT UNEQUIP: {err}".ToUpperInvariant();
            candidateIndex = -1;
            Refresh();
        }
    }
}
