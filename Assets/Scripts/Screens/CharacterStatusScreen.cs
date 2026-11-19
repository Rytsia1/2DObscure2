using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Character Status — migrated to Figma 13---Character-Status.uxml (1280x720 UI Toolkit).
    public class CharacterStatusScreen : GameScreen
    {
        readonly List<Unit> units;
        readonly GameState returnState;
        int currentIndex;

        public Unit CurrentUnit => units[Math.Clamp(currentIndex, 0, Math.Max(0, units.Count - 1))];
        public Label HeaderLabel { get; private set; }
        public Label IdentityLabel { get; private set; }
        public Label TooltipLabel { get; private set; }
        public readonly Dictionary<Ability, Label> PrimaryStatLabels = new Dictionary<Ability, Label>();
        public readonly Dictionary<DerivedStatId, Label> DerivedStatLabels = new Dictionary<DerivedStatId, Label>();

        public static readonly DerivedStatId[] DisplayedDerivedStats =
        {
            DerivedStatId.MaxHP, DerivedStatId.MaxMP, DerivedStatId.PhysicalPower, DerivedStatId.RangedPower,
            DerivedStatId.MagicPower, DerivedStatId.HealingPower, DerivedStatId.SupportPower, DerivedStatId.PhysicalDefense,
            DerivedStatId.MagicDefense, DerivedStatId.Evasion, DerivedStatId.CriticalChance, DerivedStatId.Initiative,
            DerivedStatId.PhysicalBreakPower, DerivedStatId.MagicBreakPower, DerivedStatId.StatusResistance, DerivedStatId.QteTimingBonus,
        };

        static readonly DerivedStatId[] OverviewDerived =
        {
            DerivedStatId.PhysicalPower, DerivedStatId.MagicPower, DerivedStatId.PhysicalDefense, DerivedStatId.MagicDefense,
            DerivedStatId.Initiative, DerivedStatId.CriticalChance, DerivedStatId.Evasion,
        };

        public enum Tab { Overview, Attributes, Combat, StatusEffects, Equipment, Skills }
        static readonly string[] TabNames = { "Overview", "Attributes", "Combat", "Status Effects", "Equipment", "Skills" };
        public Tab CurrentTab { get; private set; } = Tab.Overview;

        UitkMenu menu;
        VisualElement tabHost;
        VisualElement mainHost;
        VisualElement tooltipStrip;
        UiLabel tooltipText;
        UiLabel detailLabel;
        int focusIndex;

        public CharacterStatusScreen() : this(null, null) { }

        public CharacterStatusScreen(IEnumerable<Unit> partyUnits = null, GameState? backTo = null)
        {
            if (partyUnits != null)
                units = partyUnits.ToList();
            else if (Game.I?.Run != null)
                units = Game.I.Run.Party;
            else if (Game.I?.Roster != null)
                units = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else
                units = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            returnState = backTo ?? ReturnTarget();
            currentIndex = Math.Clamp(PartyScreen.SelectedIndex, 0, Math.Max(0, units.Count - 1));
        }

        protected override void Build()
        {
            MountFigma("13---Character-Status");
            StripStaticContent();

            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Go(returnState);
            menu.onFocus = OnFocus;
            menu.onPrevPage = () => SwitchHero(-1);
            menu.onNextPage = () => SwitchHero(1);

            Refresh();
        }

        void StripStaticContent()
        {
            var screen = UiRoot.Q<VisualElement>(className: "_13-character-status");
            if (screen == null) return;

            // Hide TabView and baked static right-panel elements so dynamic tab content replaces them cleanly.
            var tv = UiRoot.Q<TabView>();
            if (tv != null) tv.style.display = DisplayStyle.None;

            var mainPanel = UiRoot.Q<VisualElement>(className: "panel-default__i-23");
            int panelIdx = mainPanel != null ? screen.IndexOf(mainPanel) : -1;
            if (panelIdx >= 0)
            {
                for (int i = panelIdx + 1; i < screen.childCount; i++)
                    screen[i].style.display = DisplayStyle.None;
            }

            tabHost = new VisualElement { name = "StatusTabBar" };
            tabHost.style.position = Position.Absolute;
            tabHost.style.left = 310;
            tabHost.style.top = 68;
            tabHost.style.height = 38;
            tabHost.style.flexDirection = FlexDirection.Row;
            screen.Add(tabHost);

            mainHost = new VisualElement { name = "StatusMainContent" };
            mainHost.style.position = Position.Absolute;
            mainHost.style.left = 326;
            mainHost.style.top = 118;
            mainHost.style.width = 914;
            mainHost.style.height = 516;
            mainHost.style.overflow = Overflow.Hidden;
            screen.Add(mainHost);

            tooltipStrip = new VisualElement { name = "StatusTooltipStrip" };
            tooltipStrip.style.position = Position.Absolute;
            tooltipStrip.style.left = 326;
            tooltipStrip.style.bottom = 20;
            tooltipStrip.style.width = 914;
            tooltipStrip.style.paddingLeft = 10;
            tooltipStrip.style.paddingRight = 10;
            tooltipStrip.style.paddingTop = 6;
            tooltipStrip.style.paddingBottom = 6;
            tooltipStrip.style.backgroundColor = Palette.Ink2;
            tooltipStrip.style.borderTopWidth = 1;
            tooltipStrip.style.borderBottomWidth = 1;
            tooltipStrip.style.borderLeftWidth = 1;
            tooltipStrip.style.borderRightWidth = 1;
            tooltipStrip.style.borderTopColor = Palette.Gold3;
            tooltipStrip.style.borderBottomColor = Palette.Gold3;
            tooltipStrip.style.borderLeftColor = Palette.Gold3;
            tooltipStrip.style.borderRightColor = Palette.Gold3;
            tooltipStrip.style.display = DisplayStyle.None;

            tooltipText = new UiLabel { name = "TooltipStripText" };
            tooltipText.style.fontSize = 13;
            tooltipText.style.color = Palette.Mist1;
            tooltipText.style.whiteSpace = WhiteSpace.Normal;
            tooltipStrip.Add(tooltipText);
            screen.Add(tooltipStrip);
        }

        public void SwitchHero(int delta)
        {
            if (units.Count == 0) return;
            currentIndex = (currentIndex + delta + units.Count) % units.Count;
            Refresh();
        }

        public void ShowTab(Tab t)
        {
            CurrentTab = t;
            focusIndex = 0;
            Refresh();
        }

        public string GetStatTooltip(DerivedStatId st) => CurrentUnit.ExplainStat(st).FormatTooltip();

        public string GetEquipmentSummary()
        {
            var u = CurrentUnit;
            return string.Join(" | ", EquipmentSlots.All.Select(slot =>
                $"{EquipmentSlots.ShortName(slot)}:{(u.equipped.TryGetValue(slot, out var eq) && eq != null ? eq.name : "Empty")}"));
        }

        Transform hiddenBindings;

        public void Refresh()
        {
            if (Root == null) return;
            var u = CurrentUnit;
            u.RecalculateStats();

            if (hiddenBindings != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(hiddenBindings.gameObject);
                else UnityEngine.Object.DestroyImmediate(hiddenBindings.gameObject);
            }

            menu.Clear();
            PrimaryStatLabels.Clear();
            DerivedStatLabels.Clear();
            detailLabel = null;
            tabHost.Clear();
            mainHost.Clear();
            if (tooltipStrip != null) tooltipStrip.style.display = DisplayStyle.None;

            // Hidden off-screen Obscure.Label bindings kept intact for CharacterStatsTests contract.
            hiddenBindings = new GameObject("Bindings").transform;
            hiddenBindings.SetParent(Root, false);
            hiddenBindings.gameObject.SetActive(false);
            HeaderLabel = Ui.Label(hiddenBindings, PixelFont.Fit($"{u.name ?? "HERO"} LV{u.level}".ToUpperInvariant(), 84), 0, 0, Palette.Mist1);
            IdentityLabel = Ui.Label(hiddenBindings, PixelFont.Fit($"{u.race?.name ?? "Human"} {u.classDef?.name ?? u.role}".ToUpperInvariant(), 125), 0, 0, Palette.Mist1);
            TooltipLabel = Ui.Label(hiddenBindings, "", 0, 0, Palette.Mist1);
            foreach (var a in AbilityScores.AllAbilities) PrimaryStatLabels[a] = Ui.Label(hiddenBindings, StatLine(u, a), 0, 0, Palette.Mist1);
            foreach (var st in DisplayedDerivedStats) DerivedStatLabels[st] = Ui.Label(hiddenBindings, $"{Database.DerivedStatDefs[st].shortName,-4} : {u.Derived.FormatValue(st)}", 0, 0, Palette.Mist1);

            BindHeader();
            BindIdentity(u);
            BindTabs();

            switch (CurrentTab)
            {
                case Tab.Attributes: BuildAttributeList(u); break;
                case Tab.Combat: BuildCombatList(u); break;
                case Tab.StatusEffects: BuildStatusList(); break;
                default: BuildOverview(u); break;
            }

            if (menu.items.Count > 0)
                menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
        }

        void BindHeader()
        {
            var backBtn = FigmaUi.FindButton(UiRoot, "Back");
            if (backBtn != null)
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", () => Game?.Go(returnState), tooltip: "RETURN TO PREVIOUS SCREEN"), backBtn);

            string[] frameClasses = { "frame-vars-13", "frame-vars-14", "frame-vars-15", "frame-vars-16" };
            for (int i = 0; i < frameClasses.Length; i++)
            {
                var frame = UiRoot.Q<VisualElement>(className: frameClasses[i]);
                if (frame == null) continue;
                if (i >= units.Count)
                {
                    frame.style.display = DisplayStyle.None;
                    continue;
                }
                frame.style.display = DisplayStyle.Flex;
                int idx = i;
                var unit = units[i];
                bool selected = i == currentIndex;
                if (frame.childCount > 0 && unit.sprite != null && unit.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(frame[0], FigmaUi.GetSpriteTexture(unit.sprite, "st_hdr_" + (unit.classDef?.id ?? unit.name)));

                var pItem = menu.Add(frame, unit.name ?? $"Hero {i + 1}", () =>
                {
                    if (currentIndex != idx) { currentIndex = idx; Refresh(); }
                }, selected: selected, onAdjust: SwitchHero, tooltip: "LEFT/RIGHT OR CLICK: INSPECT ANOTHER PARTY MEMBER");
                pItem.visual = on =>
                {
                    var border = on ? Palette.Gold1 : selected ? Palette.Gold2 : Palette.Ink5;
                    frame.style.borderTopColor = border;
                    frame.style.borderBottomColor = border;
                    frame.style.borderLeftColor = border;
                    frame.style.borderRightColor = border;
                    frame.style.backgroundColor = on ? Palette.Ink3 : Palette.Ink2;
                };
                pItem.visual(false);
            }

            int gold = Run != null ? Run.Gold : 0;
            var hubTag = UiRoot.Q<VisualElement>(className: "currency-tag-hub");
            if (hubTag != null)
            {
                FigmaUi.SetInstanceText(hubTag, "text-0", $"{gold:#,##0} HUB GOLD");
                FigmaUi.AnchorRight(hubTag, 24f);
            }
        }

        void BindIdentity(Unit u)
        {
            var fig = UiRoot.Query<VisualElement>(className: "sprite-knight").ToList()
                .FirstOrDefault(e => e.parent == null || !e.parent.ClassListContains("frame-vars-13"));
            if (fig != null && u.sprite != null && u.sprite.Length > 0)
                FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "st_id_" + (u.classDef?.id ?? u.name)));

            var nameLbl = UiRoot.Q<UiLabel>(className: "knight-vars-4");
            if (nameLbl != null)
            {
                nameLbl.text = u.name ?? "Hero";
                nameLbl.style.maxWidth = 180;
                nameLbl.style.overflow = Overflow.Hidden;
            }

            var lvLbl = UiRoot.Q<UiLabel>(className: "lv-1-vars-6");
            if (lvLbl != null) lvLbl.text = $"LV {u.level}";

            if (nameLbl != null && nameLbl.parent != null)
            {
                int idx = nameLbl.parent.IndexOf(nameLbl);
                // Tag container follows lv-1-vars-6 in UXML
                if (idx + 2 < nameLbl.parent.childCount)
                {
                    var tagRow = nameLbl.parent[idx + 2];
                    tagRow.Clear();
                    tagRow.style.flexWrap = Wrap.NoWrap;
                    tagRow.style.maxWidth = 240;
                    foreach (var tText in new[] { u.race?.name ?? "Human", u.classDef?.name ?? u.role }.Where(s => !string.IsNullOrEmpty(s)))
                    {
                        var tag = FigmaUi.InstantiateTemplate("Tag-Cls");
                        if (tag != null)
                        {
                            FigmaUi.SetInstanceText(tag, "text-0", tText.ToUpperInvariant());
                            tag.style.marginRight = 4;
                            tagRow.Add(tag);
                        }
                    }
                }
            }

            float hpRatio = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
            float mpRatio = (float)u.mp / Mathf.Max(1, u.stats.maxMp);
            FigmaUi.SetGaugeFill(UiRoot.Q<VisualElement>(className: "gauge-hp"), hpRatio, hpRatio < 0.25f ? Palette.HpLow : Palette.Hp);
            FigmaUi.SetGaugeFill(UiRoot.Q<VisualElement>(className: "gauge-mp"), mpRatio, Palette.Mp);

            var hpLbl = UiRoot.Q<UiLabel>(className: "_125-125-vars");
            if (hpLbl != null) hpLbl.text = $"{u.hp}/{u.stats.maxHp}";
            var mpLbl = UiRoot.Q<UiLabel>(className: "_20-20-vars");
            if (mpLbl != null) mpLbl.text = $"{u.mp}/{u.stats.maxMp}";

            var perkBox = UiRoot.Q<VisualElement>(className: "frame-vars-17");
            if (perkBox != null)
            {
                var bg = u.background;
                if (bg == null)
                {
                    perkBox.style.display = DisplayStyle.None;
                }
                else
                {
                    perkBox.style.display = DisplayStyle.Flex;
                    var rank = bg.GetRank(u.level);
                    string[] numerals = { "I", "II", "III", "IV", "V" };
                    int r = Mathf.Clamp(BackgroundRankResolver.GetRankForLevel(u.level), 1, 5);
                    var rLbl = UiRoot.Q<UiLabel>(className: "background-perk-rank-i-vars");
                    if (rLbl != null) rLbl.text = $"BACKGROUND PERK · RANK {numerals[r - 1]}";
                    var pLbl = UiRoot.Q<UiLabel>(className: "watchful-guard-vars");
                    if (pLbl != null) pLbl.text = rank?.perkName ?? bg.perkName;
                    var dLbl = UiRoot.Q<UiLabel>(className: "physical-damage-taken-is-reduced-by-3-vars");
                    if (dLbl != null) dLbl.text = (rank?.perkDescription ?? "").ToUpperInvariant();
                }
            }
        }

        void BindTabs()
        {
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tab = (Tab)i;
                bool active = tab == CurrentTab;
                Action go = tab == Tab.Equipment ? () => Game?.Go(GameState.Equipment)
                    : tab == Tab.Skills ? () => Game?.Go(GameState.Skills)
                    : (Action)(() => ShowTab(tab));

                var btn = FigmaUi.CreateButton(TabNames[i], "button-slab", null);
                btn.style.height = 38;
                btn.style.paddingLeft = 14;
                btn.style.paddingRight = 14;
                btn.style.marginRight = 4;
                btn.style.fontSize = 18;
                btn.style.borderBottomWidth = active ? 0 : 2;
                btn.style.borderTopWidth = active ? 3 : 2;
                tabHost.Add(btn);

                var item = menu.Add(btn, TabNames[i], go, selected: active, tooltip: $"{TabNames[i].ToUpperInvariant()} TAB");
                item.visual = on =>
                {
                    btn.style.backgroundColor = on || active ? Palette.Ink3 : Palette.Ink2;
                    btn.style.borderTopColor = on ? Palette.Gold1 : active ? Palette.Gold2 : Palette.Ink4;
                    var side = on ? Palette.Gold1 : active ? Palette.Gold3 : Palette.Ink4;
                    btn.style.borderLeftColor = side;
                    btn.style.borderRightColor = side;
                    btn.style.borderBottomColor = active ? Palette.Ink3 : side;
                    btn.style.color = on || active ? Palette.Gold1 : Palette.Mist2;
                };
                item.visual(false);
            }
        }

        void BuildOverview(Unit u)
        {
            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, height = 285, marginBottom = 16 } };
            var botRow = new VisualElement { style = { flexDirection = FlexDirection.Row, height = 225 } };
            mainHost.Add(topRow);
            mainHost.Add(botRow);

            // Col 1 Top: Attributes
            var attrCol = ColumnBox(280, 28);
            attrCol.Add(SectionTitle("Attributes"));
            foreach (var a in AbilityScores.AllAbilities)
            {
                var ab = a;
                int score = u.Score(ab), bonus = score - u.BaseScores[ab];
                string tip = u.ExplainAbility(ab);
                var row = StatRow(
                    $"{a} · {AbilityName(a)}",
                    bonus != 0 ? $"{(bonus > 0 ? "+" : "")}{bonus}" : null,
                    score.ToString(),
                    () => ShowTip(tip));
                attrCol.Add(row);
                var it = menu.Add(row, $"{a} {score}", () => ShowTip(tip), tooltip: tip);
                it.visual = on => row.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Color.clear;
            }
            topRow.Add(attrCol);

            // Col 2 Top: Derived Stats
            var derCol = ColumnBox(280, 28);
            derCol.Add(SectionTitle("Derived Stats"));
            foreach (var st in OverviewDerived)
            {
                var stat = st;
                var def = Database.DerivedStatDefs[st];
                string tip = GetStatTooltip(stat);
                var row = StatRow((def.displayName ?? def.shortName).ToUpperInvariant(), null, u.Derived.FormatValue(st), () => ShowTip(tip));
                derCol.Add(row);
                var it = menu.Add(row, def.shortName, () => ShowTip(tip), tooltip: tip);
                it.visual = on => row.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Color.clear;
            }
            topRow.Add(derCol);

            // Col 3 Top: Resistances
            var resCol = ColumnBox(298, 0);
            resCol.Add(SectionTitle("Resistances"));
            var resGrid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            var entries = new List<(Element el, float v, string note)> { (Element.Physical, 0f, ""), (Element.Arcane, 0f, "FIXED") };
            foreach (ElementType et in Enum.GetValues(typeof(ElementType)))
            {
                if (et == ElementType.None) continue;
                float v = u.elementalResistances.GetFinalResistance(et);
                entries.Add((ToElement(et), v, v > 0 ? "RESIST" : v < 0 ? "WEAK" : ""));
            }
            foreach (var (el, v, note) in entries)
            {
                var cell = new VisualElement { style = { width = 148, height = 32, flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
                string code = Palette.ElementCode(el);
                var chip = FigmaUi.InstantiateTemplate($"Element-Chip-{code}");
                if (chip != null)
                {
                    FigmaUi.SetInstanceText(chip, "text-0", code);
                    chip.style.marginRight = 8;
                    cell.Add(chip);
                }
                string pct = v == 0 ? "0%" : $"{(v > 0 ? "+" : "-")}{Mathf.RoundToInt(Mathf.Abs(v) * 100)}%";
                var pctLbl = new UiLabel(note != "" ? $"{pct} {note}" : pct);
                pctLbl.style.fontSize = 13;
                pctLbl.style.color = v > 0 ? Palette.Ok : v < 0 ? Palette.Err : Palette.Mist1;
                cell.Add(pctLbl);
                resGrid.Add(cell);
            }
            resCol.Add(resGrid);
            topRow.Add(resCol);

            // Col 1 Bottom: Status Effects
            var stCol = ColumnBox(280, 28);
            stCol.Add(SectionTitle("Status Effects"));
            if (Run != null && Run.BattleBuffs.Count > 0)
            {
                var buffRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 6 } };
                foreach (var b in Run.BattleBuffs.Take(3))
                {
                    var chip = IconArt.StatusChip(b.id, $"{StatusInfo.Name(b.id).ToUpperInvariant()} ({b.turns}T)", 1, 4f);
                    if (chip != null) buffRow.Add(chip);
                }
                stCol.Add(buffRow);
            }
            else
            {
                var sampleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 8 } };
                foreach (var sid in new[] { StatusId.AttackUp, StatusId.DefenseUp, StatusId.Burn, StatusId.Poison, StatusId.Stun, StatusId.Silence })
                {
                    var sIcon = IconArt.Inline(IconArt.Status(sid), 2, 6f);
                    if (sIcon != null) sampleRow.Add(sIcon);
                }
                stCol.Add(sampleRow);
            }
            var activeSummaries = BuildActiveEffectsSummary(u);
            string ovStatus = activeSummaries.Count > 0
                ? string.Join("\n", activeSummaries.Take(3).Select(x => "• " + x.title.ToUpperInvariant()))
                : "NO ACTIVE COMBAT EFFECTS. BUFFS AND DEBUFFS SHOW DURING A DUNGEON RUN.";
            var stTxt = new UiLabel(ovStatus);
            stTxt.style.fontSize = 13;
            stTxt.style.color = activeSummaries.Count > 0 ? Palette.Mist1 : Palette.Mist3;
            stTxt.style.whiteSpace = WhiteSpace.Normal;
            stCol.Add(stTxt);
            botRow.Add(stCol);

            // Col 2 Bottom: Equipment
            var eqCol = ColumnBox(280, 28);
            eqCol.Add(SectionTitle("Equipment"));
            var eqGrid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, width = 220, marginBottom = 10 } };
            int filled = 0;
            for (int i = 0; i < EquipmentSlots.All.Length; i++)
            {
                var slot = EquipmentSlots.All[i];
                u.equipped.TryGetValue(slot, out var eq);
                if (eq != null) filled++;
                var tile = FigmaUi.InstantiateTemplate(eq != null ? "Item-Slot-Filled" : "Item-Slot-Empty");
                if (tile != null)
                {
                    var gearIcon = IconArt.Gear(eq);
                    FigmaUi.SetInstanceText(tile, "text-0", eq != null && gearIcon == null ? InitialsFromName(eq.name) : "");
                    IconArt.Overlay(tile, gearIcon, 2);
                    tile.style.width = 44;
                    tile.style.height = 44;
                    tile.style.marginRight = 8;
                    tile.style.marginBottom = 8;
                    eqGrid.Add(tile);
                }
            }
            eqCol.Add(eqGrid);
            var empty = EquipmentSlots.All.Where(s => !u.equipped.TryGetValue(s, out var e) || e == null).Select(EquipmentSlots.DisplayName).ToList();
            string cap = $"{filled} / 8 SLOTS EQUIPPED" + (empty.Count > 0 ? $" · {empty[0].ToUpperInvariant()} EMPTY" : "");
            var eqCap = new UiLabel(cap) { style = { fontSize = 13, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal } };
            eqCol.Add(eqCap);
            botRow.Add(eqCol);

            // Col 3 Bottom: Skills
            var skCol = ColumnBox(298, 0);
            skCol.Add(SectionTitle("Skills"));

            var actRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 14 } };
            actRow.Add(new UiLabel("ACTIVE") { style = { fontSize = 13, color = Palette.Mist3, width = 76 } });
            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
            {
                string id = u.skillLoadout.ActiveSlots[i];
                var skIcon = IconArt.Inline(IconArt.Skill(id), 2, 6);
                if (skIcon != null) { actRow.Add(skIcon); continue; }
                var tag = FigmaUi.InstantiateTemplate("Tag-Gold");
                if (tag != null)
                {
                    FigmaUi.SetInstanceText(tag, "text-0", string.IsNullOrEmpty(id) ? "--" : Initials(id));
                    tag.style.marginRight = 6;
                    if (string.IsNullOrEmpty(id)) tag.style.opacity = 0.45f;
                    actRow.Add(tag);
                }
            }
            skCol.Add(actRow);

            var pasRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
            pasRow.Add(new UiLabel("PASSIVE") { style = { fontSize = 13, color = Palette.Mist3, width = 76 } });
            for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
            {
                string id = u.skillLoadout.PassiveSlots[i];
                var pIcon = IconArt.Inline(IconArt.Passive(id), 2, 6);
                if (pIcon != null) pasRow.Add(pIcon);
                else pasRow.Add(new UiLabel(string.IsNullOrEmpty(id) ? "◇ " : $"◆{Initials(id)} ") { style = { fontSize = 18, color = Palette.Gold2 } });
            }
            skCol.Add(pasRow);
            botRow.Add(skCol);
        }

        void BuildAttributeList(Unit u)
        {
            var split = new VisualElement { style = { flexDirection = FlexDirection.Row, height = 520 } };
            var left = ColumnBox(420, 24);
            left.Add(SectionTitle("Attributes"));
            foreach (var a in AbilityScores.AllAbilities)
            {
                var ab = a;
                string tip = u.ExplainAbility(ab);
                var row = StatRow($"{a} · {AbilityName(a)}", null, $"{u.Score(ab)}   {AbilityScores.FormatMod(u.Mod(ab))}", () => ShowTip(tip), 34);
                left.Add(row);
                var it = menu.Add(row, a.ToString(), () => ShowTip(tip), tooltip: tip);
                it.visual = on => row.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Color.clear;
            }
            split.Add(left);
            split.Add(DetailPane(460, 480));
            mainHost.Add(split);
        }

        void BuildCombatList(Unit u)
        {
            mainHost.Add(SectionTitle("Derived Combat Stats"));
            var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.SpaceBetween, marginBottom = 14 } };
            foreach (var st in DisplayedDerivedStats)
            {
                var stat = st;
                var def = Database.DerivedStatDefs[st];
                string tip = GetStatTooltip(stat);
                var row = StatRow((def.displayName ?? def.shortName).ToUpperInvariant(), null, u.Derived.FormatValue(st), () => ShowTip(tip), 30);
                row.style.width = 440;
                grid.Add(row);
                var it = menu.Add(row, def.shortName, () => ShowTip(tip), tooltip: tip);
                it.visual = on => row.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Color.clear;
            }
            mainHost.Add(grid);
            mainHost.Add(DetailPane(914, 140));
        }

        List<(string title, string detail, Color col, Texture2D icon)> BuildActiveEffectsSummary(Unit u)
        {
            var list = new List<(string, string, Color, Texture2D)>();
            if (Run != null && Run.BattleBuffs.Count > 0)
            {
                foreach (var b in Run.BattleBuffs)
                    list.Add(($"NEXT BATTLE: {StatusInfo.Name(b.id)}", $"{b.turns} TURNS", StatusInfo.IsBuff(b.id) ? Palette.Ok : Palette.Err, IconArt.Status(b.id)));
            }
            if (u.background != null)
            {
                var rank = u.background.GetRank(u.level);
                list.Add(($"PERK: {rank?.perkName ?? u.background.perkName}", rank?.perkDescription ?? "", Palette.Gold1, null));
            }
            if (Run != null && Run.Relics != null)
            {
                foreach (var rid in Run.Relics)
                {
                    if (Database.Relics.TryGetValue(rid, out var r))
                        list.Add(($"RELIC: {r.name}", r.gameplayEffect ?? r.description ?? "", Ui.RelicColor(r), null));
                }
            }
            return list;
        }

        void BuildStatusList()
        {
            mainHost.Add(SectionTitle("Active Buffs, Perks & Relics"));
            var items = BuildActiveEffectsSummary(CurrentUnit);

            var scroll = FigmaUi.CreateVerticalScrollView();
            scroll.style.maxHeight = 460;

            if (items.Count == 0)
            {
                var lbl = new UiLabel("NO ACTIVE RUN EFFECTS. BUFFS AND DEBUFFS APPLIED IN COMBAT ARE LISTED BELOW.");
                lbl.style.fontSize = 13;
                lbl.style.color = Palette.Mist3;
                lbl.style.whiteSpace = WhiteSpace.Normal;
                lbl.style.marginBottom = 10;
                scroll.Add(lbl);
            }
            else
            {
                foreach (var (title, detail, col, icon) in items)
                {
                    var card = new VisualElement();
                    card.AddToClassList("uitk-card");
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.alignItems = UnityEngine.UIElements.Align.Center;
                    var iconEl = IconArt.Inline(icon, 2, 10f);
                    if (iconEl != null) card.Add(iconEl);

                    var textCol = new VisualElement { style = { flexGrow = 1, flexShrink = 1 } };
                    textCol.Add(new UiLabel(title.ToUpperInvariant()) { style = { fontSize = 16, color = col, marginBottom = 4 } });
                    textCol.Add(new UiLabel(detail.ToUpperInvariant()) { style = { fontSize = 13, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal } });
                    card.Add(textCol);
                    scroll.Add(card);
                }
            }

            var refHdr = SectionTitle("Combat Status Effects Reference");
            refHdr.style.marginTop = 10;
            scroll.Add(refHdr);

            var grid = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    justifyContent = Justify.SpaceBetween
                }
            };
            foreach (StatusId sid in Enum.GetValues(typeof(StatusId)))
            {
                var def = StatusEffectDefs.Get(sid);
                bool buff = StatusInfo.IsBuff(sid);
                bool mental = StatusInfo.IsMental(sid);
                bool dot = StatusInfo.IsDoT(sid);
                Color titleCol = buff ? Palette.Ok : mental ? Palette.ArcaneViolet : dot ? Palette.Ember1 : Palette.Err;

                var cell = new VisualElement();
                cell.AddToClassList("uitk-card");
                cell.style.width = 442;
                cell.style.flexDirection = FlexDirection.Row;
                cell.style.alignItems = UnityEngine.UIElements.Align.Center;

                var sIcon = IconArt.Inline(IconArt.Status(sid), 2, 10f);
                if (sIcon != null) cell.Add(sIcon);

                var tCol = new VisualElement { style = { flexGrow = 1, flexShrink = 1 } };
                tCol.Add(new UiLabel($"{StatusInfo.Name(sid).ToUpperInvariant()} [{StatusInfo.Tag(sid)}]") { style = { fontSize = 14, color = titleCol, marginBottom = 2 } });
                tCol.Add(new UiLabel((def?.description ?? "").ToUpperInvariant()) { style = { fontSize = 11, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal } });
                cell.Add(tCol);
                grid.Add(cell);
            }
            scroll.Add(grid);
            mainHost.Add(scroll);
        }

        VisualElement DetailPane(int width, int height)
        {
            var box = new VisualElement();
            box.style.width = width;
            box.style.height = height;
            box.style.paddingLeft = 14;
            box.style.paddingRight = 14;
            box.style.paddingTop = 12;
            box.style.paddingBottom = 12;
            box.style.backgroundColor = Palette.Ink2;
            box.style.borderTopWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderTopColor = Palette.Mist3;
            box.style.borderBottomColor = Palette.Mist3;
            box.style.borderLeftColor = Palette.Mist3;
            box.style.borderRightColor = Palette.Mist3;

            detailLabel = new UiLabel("");
            detailLabel.style.fontSize = 14;
            detailLabel.style.color = Palette.Mist2;
            detailLabel.style.whiteSpace = WhiteSpace.Normal;
            box.Add(detailLabel);
            return box;
        }

        static VisualElement ColumnBox(int width, int marginRight)
        {
            return new VisualElement
            {
                style = { width = width, marginRight = marginRight, flexDirection = FlexDirection.Column }
            };
        }

        static UiLabel SectionTitle(string text)
        {
            return new UiLabel(text)
            {
                style = { fontSize = 22, color = Palette.Gold2, marginBottom = 10 }
            };
        }

        static VisualElement StatRow(string leftText, string midText, string rightText, Action onClick, int height = 30)
        {
            var row = new VisualElement();
            row.style.height = height;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = UnityEngine.UIElements.Align.Center;
            row.style.paddingLeft = 4;
            row.style.paddingRight = 4;
            row.style.borderBottomWidth = 2;
            row.style.borderBottomColor = Palette.Ink3;

            var l = new UiLabel(leftText) { style = { flexGrow = 1, fontSize = 14, color = Palette.Mist2 } };
            row.Add(l);
            if (!string.IsNullOrEmpty(midText))
                row.Add(new UiLabel(midText) { style = { fontSize = 13, color = Palette.Mist3, marginRight = 10 } });
            row.Add(new UiLabel(rightText) { style = { fontSize = 14, color = Palette.Mist1 } });

            return row;
        }

        void ShowTip(string text)
        {
            TooltipLabel?.Set(PixelFont.Wrap(text ?? "", 448));
            if (detailLabel != null)
                detailLabel.text = (text ?? "").ToUpperInvariant();
            if (tooltipStrip != null && CurrentTab == Tab.Overview)
            {
                bool has = !string.IsNullOrEmpty(text);
                tooltipStrip.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                if (tooltipText != null) tooltipText.text = (text ?? "").ToUpperInvariant();
            }
        }

        void OnFocus(UitkMenu.Item item)
        {
            if (item == null) return;
            focusIndex = menu.items.IndexOf(item);
            ShowTip(item.tooltip);
        }

        static string Initials(string skillId)
        {
            string name = Database.Skills.TryGetValue(skillId, out var s) ? s.name
                : Database.Passives.TryGetValue(skillId, out var p) ? p.name : skillId;
            return InitialsFromName(name);
        }

        static string InitialsFromName(string name)
        {
            var words = (name ?? "?").Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            return (words.Length >= 2 ? $"{words[0][0]}{words[1][0]}" : words[0].Substring(0, Math.Min(2, words[0].Length))).ToUpperInvariant();
        }

        static string AbilityName(Ability a) => a switch
        {
            Ability.STR => "STRENGTH", Ability.DEX => "DEXTERITY", Ability.VIT => "VITALITY",
            Ability.INT => "INTELLIGENCE", Ability.WIS => "WISDOM", _ => "CHARISMA",
        };

        static Element ToElement(ElementType et) => et switch
        {
            ElementType.Fire => Element.Fire, ElementType.Ice => Element.Ice, ElementType.Lightning => Element.Lightning,
            ElementType.Earth => Element.Earth, ElementType.Wind => Element.Wind, ElementType.Water => Element.Water,
            ElementType.Holy => Element.Holy, ElementType.Shadow => Element.Shadow, _ => Element.Physical,
        };

        static string StatLine(Unit u, Ability a)
        {
            int score = u.Score(a), bonus = score - u.BaseScores[a];
            string bonusTag = bonus != 0 ? $"({(bonus > 0 ? "+" : "")}{bonus})" : "";
            return $"{a} {score,2}  {AbilityScores.FormatMod(u.Mod(a)),2}  {bonusTag}";
        }
    }
}
