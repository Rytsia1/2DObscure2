using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Equipment Screen — migrated to Figma 15---Equipment.uxml (1280x720 UI Toolkit).
    public class EquipmentScreen : GameScreen
    {
        readonly GameState returnState;
        readonly InventoryRuntime inventory;
        readonly List<Unit> party;

        int heroIndex = 0;
        int selectedSlotIndex = 0;
        int candidateIndex = -1;
        string feedback;
        UitkMenu menu;

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

        static readonly DerivedStatId[] ComparedStats =
        {
            DerivedStatId.MaxHP, DerivedStatId.PhysicalPower, DerivedStatId.MagicPower, DerivedStatId.PhysicalDefense,
            DerivedStatId.MagicDefense, DerivedStatId.CriticalChance, DerivedStatId.Initiative, DerivedStatId.StatusResistance
        };

        static readonly Color Up = Palette.Hex(0x7FF0AD), Down = Palette.Hex(0xFF9A8C);

        public Unit CurrentHero => party[Math.Clamp(heroIndex, 0, party.Count - 1)];
        public EquipmentSlot SelectedSlot => CoreSlots[Math.Clamp(selectedSlotIndex, 0, CoreSlots.Length - 1)];

        class RowData { public EquipmentInstance item; public EquipmentDef def; public bool equipped; public string lockReason; }

        public EquipmentScreen() : this(null, null, 0, null) { }

        public EquipmentScreen(InventoryRuntime inv = null, IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
        {
            inventory = inv ?? (Game.I?.Run?.Inventory ?? new InventoryRuntime());
            if (partyUnits != null) party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null) party = Game.I.Run.Party;
            else if (Game.I?.Roster != null) party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            heroIndex = Math.Clamp(initialHero, 0, Math.Max(0, party.Count - 1));
            returnState = backTo ?? ReturnTarget();
        }

        protected override void Build()
        {
            MountFigma("15---Equipment");
            Refresh();
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

        void SelectSlot(int index) { selectedSlotIndex = index; candidateIndex = -1; feedback = null; Refresh(); }

        void SelectCandidate(int index, List<RowData> rows)
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
                    item = e, def = e.Definition,
                    lockReason = EquipmentValidator.Validate(e.Definition, slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id),
                })
                .OrderBy(d => d.lockReason != null).ThenByDescending(d => d.item.rarity).ThenBy(d => d.def.name);
            list.AddRange(bag);
            return list;
        }

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
            UiRoot = FigmaUi.Mount(Root.gameObject, "15---Equipment");
            if (menu != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(menu);
                else UnityEngine.Object.DestroyImmediate(menu);
            }
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Go(returnState);

            var hero = CurrentHero;
            hero.RecalculateStats();
            var rows = BuildRows();
            if (candidateIndex >= rows.Count) candidateIndex = -1;

            var backBtn = FigmaUi.FindButton(UiRoot, "Back");
            if (backBtn != null) FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", () => Game?.Go(returnState)));

            // Header portrait switcher
            string[] hdrFrames = { "frame-vars-18", "frame-vars-19", "frame-vars-20", "frame-vars-21" };
            for (int i = 0; i < hdrFrames.Length; i++)
            {
                var fr = UiRoot.Q<VisualElement>(className: hdrFrames[i]);
                if (fr == null) continue;
                if (i >= party.Count) { fr.style.display = DisplayStyle.None; continue; }
                int idx = i; var u = party[i]; bool on = i == heroIndex;
                if (fr.childCount > 0 && u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fr[0], FigmaUi.GetSpriteTexture(u.sprite, "eq_hdr_" + (u.classDef?.id ?? u.name)));
                var it = menu.Add(fr, u.name, () => { if (heroIndex != idx) { heroIndex = idx; candidateIndex = -1; feedback = null; Refresh(); } }, selected: on, onAdjust: SwitchHero);
                it.visual = f => { var col = f ? Palette.Gold1 : on ? Palette.Gold2 : Palette.Ink5; fr.style.borderTopColor = col; fr.style.borderBottomColor = col; fr.style.borderLeftColor = col; fr.style.borderRightColor = col; };
                it.visual(false);
            }

            var bagBtn = FigmaUi.FindButton(UiRoot, "BAG 12 / 100");
            if (bagBtn != null)
            {
                bagBtn.text = $"BAG {inventory.EquipmentCount} / {InventoryRuntime.MaxEquipmentCapacity}";
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(bagBtn, "Bag", () => Game?.Go(GameState.Inventory)));
            }

            var screen = UiRoot.Q<VisualElement>(className: "_15-equipment") ?? UiRoot;
            var p1 = UiRoot.Q<VisualElement>(className: "panel-default__i-25");
            var p2 = UiRoot.Q<VisualElement>(className: "panel-default__i-26");
            var p3 = UiRoot.Q<VisualElement>(className: "panel-gold__i-11");
            if (p1 != null)
            {
                int i1 = screen.IndexOf(p1);
                for (int i = i1 + 1; i < screen.childCount; i++)
                {
                    var ch = screen[i];
                    if (ch == p2 || ch == p3) continue;
                    ch.style.display = DisplayStyle.None;
                }
            }

            BuildPaperDoll(screen, hero);
            BuildGearList(screen, hero, rows);
            BuildCompare(screen, hero, rows);
        }

        void BuildPaperDoll(VisualElement screen, Unit hero)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 40, top = 86, width = 300, height = 574, flexDirection = FlexDirection.Column } };
            screen.Add(host);

            var topBar = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center } };
            topBar.Add(FigmaUi.CreateSectionHeader($"{hero.name} · {hero.classDef?.name ?? hero.role}", 248f));
            topBar.Add(new UiLabel($"{CoreSlots.Count(s => Equipped(s).def != null)}/{CoreSlots.Length}") { style = { fontSize = 14, color = Palette.Mist2 } });
            host.Add(topBar);

            var doll = new VisualElement { style = { flexGrow = 1, marginTop = 10, justifyContent = Justify.SpaceBetween } };
            var fig = new VisualElement { style = { position = Position.Absolute, left = 86, top = 140, width = 128, height = 160 } };
            if (hero.sprite != null && hero.sprite.Length > 0)
                FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(hero.sprite, "eq_doll_" + (hero.classDef?.id ?? hero.name)));
            doll.Add(fig);

            EquipmentSlot[] leftSlots = { EquipmentSlot.HeadArmor, EquipmentSlot.BodyArmor, EquipmentSlot.LegArmor, EquipmentSlot.FootArmor };
            EquipmentSlot[] rightSlots = { EquipmentSlot.MainHand, EquipmentSlot.SecondHand, EquipmentSlot.Accessory1, EquipmentSlot.Accessory2 };

            for (int r = 0; r < 4; r++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                row.Add(MakePaperDollSlot(leftSlots[r]));
                row.Add(MakePaperDollSlot(rightSlots[r]));
                doll.Add(row);
            }
            host.Add(doll);

            bool hasEq = Equipped(SelectedSlot).def != null;
            var unBtn = FigmaUi.CreateButton("UNEQUIP", "button-slab", 300f);
            unBtn.SetEnabled(hasEq);
            unBtn.style.marginTop = 10;
            host.Add(unBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(unBtn, "UNEQUIP", OnUnequipClicked, interactable: hasEq));
        }

        VisualElement MakePaperDollSlot(EquipmentSlot slot)
        {
            int idx = Array.IndexOf(CoreSlots, slot);
            var (inst, def) = Equipped(slot);
            bool sel = idx == selectedSlotIndex;
            var wrap = new VisualElement { style = { width = 76, alignItems = UnityEngine.UIElements.Align.Center } };
            var box = FigmaUi.InstantiateTemplate(sel ? "Item-Slot-Selected" : def != null ? "Item-Slot-Filled" : "Item-Slot-Empty") ?? new VisualElement();
            box.style.width = 58; box.style.height = 58;
            var r = inst?.rarity ?? def?.rarity;
            FigmaUi.SetInstanceText(box, "text-0", def != null && r.HasValue ? Palette.RarityBadge(r.Value) : "");
            if (def != null && r.HasValue)
            {
                var lbl = box.Q<UiLabel>("text-0");
                if (lbl != null) lbl.style.color = EquipmentRarities.GetColor(r.Value);
            }
            wrap.Add(box);
            wrap.Add(new UiLabel(GetCoreSlotName(slot)) { style = { fontSize = 11, color = sel ? Palette.Gold1 : Palette.Mist3, marginTop = 2 } });

            var it = menu.Add(box, GetCoreSlotName(slot), () => SelectSlot(idx), selected: sel);
            it.visual = on =>
            {
                var b = on ? Palette.Gold1 : sel ? Palette.Gold2 : def != null && r.HasValue ? EquipmentRarities.GetColor(r.Value) : Palette.Ink4;
                box.style.borderTopColor = b; box.style.borderBottomColor = b; box.style.borderLeftColor = b; box.style.borderRightColor = b;
            };
            it.visual(false);
            return wrap;
        }

        void BuildGearList(VisualElement screen, Unit hero, List<RowData> rows)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 384, top = 86, width = 464, height = 574, flexDirection = FlexDirection.Column } };
            screen.Add(host);

            var top = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 10 } };
            top.Add(FigmaUi.CreateSectionHeader($"{GetCoreSlotName(SelectedSlot)} FOR {hero.name.ToUpperInvariant()}", 330f));
            top.Add(new UiLabel("SORT: RARITY") { style = { fontSize = 12, color = Palette.Mist3 } });
            host.Add(top);

            for (int i = 0; i < 8 && i < rows.Count; i++)
            {
                int rIdx = i;
                var d = rows[i];
                bool chosen = i == candidateIndex;
                var rarity = d.item?.rarity ?? d.def.rarity;
                Color rc = EquipmentRarities.GetColor(rarity);
                string sub, right;
                Color rightCol;
                if (d.equipped)
                {
                    var st = KeyStat(SelectedSlot, null);
                    sub = $"EQUIPPED · {Database.DerivedStatDefs[st].shortName} {StatValue(st, hero.Derived.Get(st))}".ToUpperInvariant();
                    right = "E"; rightCol = Palette.Mist1;
                }
                else if (d.lockReason != null)
                {
                    sub = d.lockReason.ToUpperInvariant();
                    right = "LOCKED"; rightCol = Down;
                }
                else
                {
                    var st = KeyStat(SelectedSlot, d.item);
                    int delta = EquipmentManager.CalculateStatDelta(hero, SelectedSlot, d.item, st);
                    sub = $"{Database.DerivedStatDefs[st].shortName} {StatValue(st, hero.Derived.Get(st) + delta)}".ToUpperInvariant();
                    right = delta > 0 ? $"+{delta}" : delta < 0 ? delta.ToString() : "=0";
                    rightCol = delta > 0 ? Up : delta < 0 ? Down : Palette.Mist3;
                }

                var row = new VisualElement
                {
                    style =
                    {
                        height = 56, marginBottom = 6, paddingLeft = 10, paddingRight = 10,
                        flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    }
                };
                var col = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
                col.Add(new UiLabel(ItemName(d.item, d.def)) { style = { fontSize = 16, color = d.lockReason != null ? Color.Lerp(rc, Palette.Mist3, 0.5f) : rc } });
                col.Add(new UiLabel(sub) { style = { fontSize = 11, color = d.lockReason != null ? Down : Palette.Mist3 } });
                row.Add(col);
                row.Add(new UiLabel(right) { style = { fontSize = 14, color = rightCol, minWidth = 54, unityTextAlign = TextAnchor.MiddleRight } });
                host.Add(row);

                bool canPick = !d.equipped && d.lockReason == null;
                var it = menu.Add(row, d.def.name, () => SelectCandidate(rIdx, rows), interactable: canPick, selected: chosen);
                it.visual = on =>
                {
                    row.style.backgroundColor = on || chosen ? Palette.Hex(0x2B2210) : d.equipped ? Palette.Ink3 : Palette.Ink1;
                    var b = on ? Palette.Gold1 : chosen ? Palette.Gold2 : Palette.Ink5;
                    row.style.borderTopColor = b; row.style.borderBottomColor = b; row.style.borderLeftColor = b; row.style.borderRightColor = b;
                };
                it.visual(false);
            }

            host.Add(new VisualElement { style = { flexGrow = 1 } });
            string foot = rows.Count > 8 ? $"+{rows.Count - 8} MORE IN THE BAG" : rows.Count == 0 ? "NOTHING IN THE BAG FITS THIS SLOT." : $"{rows.Count} ITEMS FIT THIS SLOT";
            host.Add(new UiLabel(foot) { style = { fontSize = 13, color = Palette.Mist3 } });
        }

        void BuildCompare(VisualElement screen, Unit hero, List<RowData> rows)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 892, top = 86, width = 348, height = 574, flexDirection = FlexDirection.Column } };
            screen.Add(host);
            host.Add(FigmaUi.CreateSectionHeader("COMPARE", 348f));

            var slot = SelectedSlot;
            var (eqInst, eqDef) = Equipped(slot);
            var cand = candidateIndex >= 0 ? rows[candidateIndex].item : null;

            var hdrRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 10, marginBottom = 8, paddingBottom = 8, borderBottomWidth = 2, borderBottomColor = Palette.Ink4 } };
            var leftH = new VisualElement { style = { width = 166 } };
            leftH.Add(new UiLabel("EQUIPPED") { style = { fontSize = 11, color = Palette.Mist3 } });
            leftH.Add(new UiLabel(eqDef != null ? ItemName(eqInst, eqDef) : "EMPTY") { style = { fontSize = 15, color = eqDef != null ? EquipmentRarities.GetColor(eqInst?.rarity ?? eqDef.rarity) : Palette.Mist3, whiteSpace = WhiteSpace.Normal } });
            var rightH = new VisualElement { style = { width = 166 } };
            rightH.Add(new UiLabel("CANDIDATE") { style = { fontSize = 11, color = Palette.Mist3 } });
            rightH.Add(new UiLabel(cand != null ? ItemName(cand, cand.Definition) : "SELECT ITEM") { style = { fontSize = 15, color = cand != null ? EquipmentRarities.GetColor(cand.rarity) : Palette.Mist3, whiteSpace = WhiteSpace.Normal } });
            hdrRow.Add(leftH); hdrRow.Add(rightH);
            host.Add(hdrRow);

            host.Add(new UiLabel("BASE STATS") { style = { fontSize = 12, color = Palette.Mist3, marginBottom = 4 } });
            foreach (var st in ComparedStats)
            {
                int a = hero.Derived.Get(st);
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, height = 24, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 } };
                row.Add(new UiLabel(Database.DerivedStatDefs[st].shortName.ToUpperInvariant()) { style = { width = 74, fontSize = 13, color = Palette.Mist2 } });
                row.Add(new UiLabel($"{StatValue(st, a)} ›") { style = { width = 84, fontSize = 13, color = Palette.Mist3, unityTextAlign = TextAnchor.MiddleRight } });
                if (cand != null)
                {
                    int delta = EquipmentManager.CalculateStatDelta(hero, slot, cand, st);
                    row.Add(new UiLabel(StatValue(st, a + delta)) { style = { flexGrow = 1, fontSize = 13, color = Palette.Mist1, unityTextAlign = TextAnchor.MiddleRight } });
                    row.Add(new UiLabel(delta > 0 ? $"+{delta}" : delta < 0 ? delta.ToString() : "=") { style = { width = 48, fontSize = 13, color = delta > 0 ? Up : delta < 0 ? Down : Palette.Mist3, unityTextAlign = TextAnchor.MiddleRight } });
                }
                else
                {
                    row.Add(new UiLabel("-") { style = { flexGrow = 1, fontSize = 13, color = Palette.Mist3, unityTextAlign = TextAnchor.MiddleRight } });
                }
                host.Add(row);
            }

            host.Add(new UiLabel("AFFIXES & EFFECTS") { style = { fontSize = 12, color = Palette.Mist3, marginTop = 10, marginBottom = 4 } });
            var lines = new List<(string text, Color color)>();
            var eqAff = eqInst?.affixes?.Select(x => x.GetDescription().ToUpperInvariant()).ToList() ?? new List<string>();
            if (cand != null)
            {
                var cAff = cand.affixes?.Select(x => x.GetDescription().ToUpperInvariant()).ToList() ?? new List<string>();
                lines.AddRange(eqAff.Except(cAff).Select(x => ("LOSE " + x, Down)));
                lines.AddRange(cAff.Except(eqAff).Select(x => ("GAIN " + x, Up)));
            }
            else lines.AddRange(eqAff.Select(x => ("• " + x, Palette.ArcaneViolet)));
            if (lines.Count == 0) lines.Add(("NO AFFIXES", Palette.Mist3));
            foreach (var (txt, col) in lines.Take(3))
                host.Add(new UiLabel(txt) { style = { fontSize = 12, color = col, whiteSpace = WhiteSpace.Normal } });

            host.Add(new VisualElement { style = { flexGrow = 1 } });
            string noteText = feedback ?? (cand == null ? "PICK GEAR FROM THE LIST TO COMPARE." : eqDef != null ? $"{ItemName(eqInst, eqDef)} RETURNS TO THE BAG." : "SLOT IS EMPTY.");
            host.Add(new UiLabel(noteText) { style = { fontSize = 12, color = Palette.Mist3, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

            var eqBtn = FigmaUi.CreateButton(cand != null ? $"EQUIP {cand.Definition?.name.ToUpperInvariant()}" : "EQUIP", cand != null ? "button-primary" : "button-slab", 348f);
            eqBtn.SetEnabled(cand != null);
            host.Add(eqBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(eqBtn, "EQUIP", () => OnEquipClicked(rows), interactable: cand != null));
        }

        void OnEquipClicked(List<RowData> rows)
        {
            if (candidateIndex < 0 || candidateIndex >= rows.Count) return;
            var candidate = rows[candidateIndex].item;
            if (EquipmentManager.EquipFromInventory(CurrentHero, SelectedSlot, candidate, inventory, out string err))
            {
                candidateIndex = -1;
                feedback = $"EQUIPPED {ItemName(candidate, candidate.Definition)}.";
                Sfx.Play(Sound.Equip);
            }
            else
            {
                feedback = $"CANNOT EQUIP: {err}".ToUpperInvariant();
                Sfx.Play(Sound.Deny);
            }
            Refresh();
        }

        void OnUnequipClicked()
        {
            if (EquipmentManager.UnequipToInventory(CurrentHero, SelectedSlot, inventory, out string err))
            {
                feedback = $"UNEQUIPPED {GetCoreSlotName(SelectedSlot)} TO THE BAG.";
                Sfx.Play(Sound.Equip);
            }
            else
            {
                feedback = $"CANNOT UNEQUIP: {err}".ToUpperInvariant();
                Sfx.Play(Sound.Deny);
            }
            candidateIndex = -1;
            Refresh();
        }
    }
}
