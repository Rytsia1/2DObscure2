using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Character Status — Starlit Reliquary "E · Character Status" board at 640x360:
    // header (Back · title · Q portraits E · Hub Gold), gold identity column, tab strip,
    // Overview grid (Attributes · Derived · Resistances / Status effects · Equipment · Skills).
    // Attributes and Combat tabs list every row with its formula breakdown.
    public class CharacterStatusScreen : GameScreen
    {
        readonly List<Unit> units;
        readonly GameState returnState;
        int currentIndex;

        public Unit CurrentUnit => units[Math.Clamp(currentIndex, 0, units.Count - 1)];
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

        const int Left = 12, RightEdge = 628, BodyY = 33, BodyH = 318, IdW = 135, MainX = Left + IdW + 7, MainW = RightEdge - MainX;
        const int TabH = 20, PanelY = BodyY + TabH - 1;

        Menu menu;
        Transform page;
        int focusIndex;

        // Floating tooltip (.tip) for focused Overview attribute rows; fixed detail pane on the list tabs.
        readonly HashSet<Selectable> attributeRows = new HashSet<Selectable>();
        SpriteRenderer tipBox;
        Label tipText, detail;
        int detailWidth;

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
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game?.Go(returnState);
            menu.onFocus = OnFocus;
            Refresh();
        }

        public void SwitchHero(int delta)
        {
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

        public void Refresh()
        {
            if (Root == null) return;
            var u = CurrentUnit;
            u.RecalculateStats();

            if (page != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(page.gameObject);
                else UnityEngine.Object.DestroyImmediate(page.gameObject);
            }
            page = new GameObject("Status").transform;
            page.SetParent(Root, false);
            menu.items.Clear();
            attributeRows.Clear();
            PrimaryStatLabels.Clear();
            DerivedStatLabels.Clear();
            detail = null;

            // Binding labels (tests + summaries), kept off-screen.
            var hidden = new GameObject("Bindings").transform;
            hidden.SetParent(page, false);
            hidden.gameObject.SetActive(false);
            HeaderLabel = Ui.Label(hidden, PixelFont.Fit($"{u.name ?? "HERO"} LV{u.level}".ToUpperInvariant(), 84), 0, 0, Palette.Mist1);
            IdentityLabel = Ui.Label(hidden, PixelFont.Fit($"{u.race?.name ?? "Human"} {u.classDef?.name ?? u.role}".ToUpperInvariant(), 125), 0, 0, Palette.Mist1);
            TooltipLabel = Ui.Label(hidden, "", 0, 0, Palette.Mist1);
            foreach (var a in AbilityScores.AllAbilities) PrimaryStatLabels[a] = Ui.Label(hidden, StatLine(u, a), 0, 0, Palette.Mist1);
            foreach (var st in DisplayedDerivedStats) DerivedStatLabels[st] = Ui.Label(hidden, $"{Database.DerivedStatDefs[st].shortName,-4} : {u.Derived.FormatValue(st)}", 0, 0, Palette.Mist1);

            BuildHeader();
            BuildIdentity(u);
            BuildTabs();
            Ui.Panel(page, MainX, PanelY, MainW, BodyY + BodyH - PanelY);
            switch (CurrentTab)
            {
                case Tab.Attributes: BuildAttributeList(u); break;
                case Tab.Combat: BuildCombatList(u); break;
                case Tab.StatusEffects: BuildStatusList(); break;
                default: BuildOverview(u); break;
            }

            tipBox = Ui.Image(page, null, 0, 0, Ui.Overlay - 2);
            tipText = Ui.Label(page, "", 0, 0, Palette.Mist2, order: Ui.Overlay - 1, font: FontKind.Interface);
            tipBox.gameObject.SetActive(false);
            tipText.gameObject.SetActive(false);

            if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
        }

        // ------------------------------------------------------------------ header

        void BuildHeader()
        {
            menu.Add(Ui.Button(page, "Back", Left, 9, 40, () => Game?.Go(returnState), 18, showCursor: false)).tooltip = "RETURN TO PREVIOUS SCREEN";
            Ui.Label(page, "Status", Left + 50, 13, Palette.Gold2);

            // Party switcher: Q · portraits · E
            int x = Left + 50 + PixelFont.Measure("Status", FontKind.DotGothic) + 24;
            Ui.Badge(page, "Q", x, 13, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);
            x += 16;
            for (int i = 0; i < units.Count; i++)
            {
                int idx = i;
                bool on = i == currentIndex;
                var p = Ui.Button(page, "", x, 9, 18, () => { if (currentIndex != idx) { currentIndex = idx; Refresh(); } }, 18, showCursor: false, style: ButtonStyle.Card);
                p.selected = on;
                p.visual(false);
                p.onAdjust = SwitchHero;
                p.tooltip = "LEFT/RIGHT OR CLICK: INSPECT ANOTHER PARTY MEMBER";
                Portrait(p.transform, units[i], 18, on);
                menu.Add(p);
                x += 22;
            }
            Ui.Badge(page, "E", x + 2, 13, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);

            int gold = Run != null ? Run.Gold : 0;
            int gw = Mathf.Max(84, PixelFont.Measure($"{gold:#,##0} HUB GOLD", FontKind.DotGothic) + 16);
            Ui.CurrencyTag(page, gold, true, RightEdge - gw, 9);
        }

        // Portrait chip (.pt): class-tinted well with a head-and-shoulders silhouette.
        static void Portrait(Transform t, Unit u, int size, bool on)
        {
            Color c = Palette.ForClass(u.classDef?.id);
            Ui.Image(t, PixelArt.Box(size, size, Color.Lerp(c, Palette.Ink1, 0.7f), on ? Palette.Gold2 : Palette.Ink5), 0, 0, Ui.Widget + 1);
            Ui.Rect(t, size / 2 - 3, 4, 6, 6, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 2);
            Ui.Rect(t, 3, size - 6, size - 6, 4, c, Ui.Widget + 2);
        }

        // ---------------------------------------------------------------- identity

        void BuildIdentity(Unit u)
        {
            Ui.Panel(page, Left, BodyY, IdW, BodyH, null, Palette.Gold3);
            const int wellH = 115;
            Ui.Image(page, PixelArt.Gradient(IdW - 4, wellH, Palette.Hex(0x16204A), Palette.Ink1), Left + 2, BodyY + 2, Ui.Panels + 1);
            Ui.Rect(page, Left + 2, BodyY + 2 + wellH, IdW - 4, 1, Palette.Ink4, Ui.Panels + 2);
            if (u.sprite != null && u.sprite.Length > 0)
            {
                int scale = Mathf.Max(1, (wellH - 18) / u.sprite.Length);
                var fig = Ui.Image(page, PixelArt.FromRows(u.sprite), 0, 0, Ui.Widget);
                fig.transform.localScale = new Vector3(scale, scale, 1);
                fig.transform.localPosition = new Vector3(Left + (IdW - u.sprite[0].Length * scale) / 2, -(BodyY + wellH - 5 - u.sprite.Length * scale), 0);
            }

            int x = Left + 7, w = IdW - 14, y = BodyY + wellH + 9;
            Ui.Label(page, PixelFont.Fit(u.name ?? "Hero", w - 36, FontKind.DotGothic), x, y, Palette.Mist1);
            Ui.Label(page, $"LV {u.level}", x + w, y + 1, Palette.Gold2, Align.Right);
            y += 15;
            int tx = x;
            foreach (var tag in new[] { u.race?.name ?? "Human", u.classDef?.name ?? u.role, u.background?.name }.Where(s => !string.IsNullOrEmpty(s)))
            {
                string t = PixelFont.Fit(tag.ToUpperInvariant(), x + w - tx - 8);
                if (t.Length < 2) break;
                tx += Ui.Tag(page, t, tx, y) + 3;
            }
            y += 17;
            float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp);
            BarRow(x, y, w, "HP", hp, hp < 0.25f ? Palette.HpLow : Palette.Hp, $"{u.hp}", $"/{u.stats.maxHp}");
            BarRow(x, y + 12, w, "MP", (float)u.mp / Mathf.Max(1, u.stats.maxMp), Palette.Mp, $"{u.mp}", $"/{u.stats.maxMp}");
            y += 30;

            var bg = u.background;
            if (bg != null)
            {
                var rank = bg.GetRank(u.level);
                string[] numerals = { "I", "II", "III", "IV", "V" };
                int r = Mathf.Clamp(BackgroundRankResolver.GetRankForLevel(u.level), 1, 5);
                string desc = PixelFont.Wrap((rank?.perkDescription ?? "").ToUpperInvariant(), w - 8);
                int h = 28 + (desc.Count(c => c == '\n') + 1) * PixelFont.LineHeight;
                h = Mathf.Min(h, BodyY + BodyH - 6 - y);
                Ui.Image(page, PixelArt.Badge(w, h, Palette.Ink0, Palette.Ink3), x, y, Ui.Panels + 2);
                Ui.Label(page, $"BACKGROUND PERK · RANK {numerals[r - 1]}", x + 4, y + 4, Palette.Mist3, font: FontKind.Interface);
                Ui.Label(page, PixelFont.Fit(rank?.perkName ?? bg.perkName, w - 8, FontKind.DotGothic), x + 4, y + 13, Palette.Gold1);
                Ui.Label(page, desc, x + 4, y + 26, Palette.Mist2, font: FontKind.Interface);
            }
        }

        // Bar row (.br): label · segmented bar · value with muted max.
        void BarRow(int x, int y, int w, string label, float ratio, Color fill, string value, string max)
        {
            Ui.Label(page, label, x, y + 1, Palette.Mist3, font: FontKind.Interface);
            int vw = PixelFont.Measure(value + max);
            Ui.Image(page, PixelArt.GaugeBar(w - 20 - vw - 6, 7, Mathf.Clamp01(ratio), fill), x + 14, y, Ui.Widget);
            Ui.Label(page, max, x + w, y + 1, Palette.Mist3, Align.Right, font: FontKind.Interface);
            Ui.Label(page, value, x + w - PixelFont.Measure(max), y + 1, Palette.Mist1, Align.Right, font: FontKind.Interface);
        }

        // -------------------------------------------------------------------- tabs

        void BuildTabs()
        {
            int x = MainX;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tab = (Tab)i;
                bool on = tab == CurrentTab;
                int w = PixelFont.Measure(TabNames[i], FontKind.DotGothic) + 16;
                Action go = tab == Tab.Equipment ? () => Game?.Go(GameState.Equipment)
                    : tab == Tab.Skills ? () => Game?.Go(GameState.Skills)
                    : (Action)(() => ShowTab(tab));
                var b = Ui.Button(page, TabNames[i], x, BodyY, w, go, TabH, showCursor: false);
                var box = b.GetComponentInChildren<SpriteRenderer>();
                b.visual = focus =>
                {
                    box.sprite = TabBox(w, TabH, on, focus);
                    b.label.Color = on || focus ? Palette.Gold1 : Palette.Mist2;
                };
                b.visual(false);
                b.tooltip = $"{TabNames[i].ToUpperInvariant()} TAB";
                menu.Add(b);
                x += w + 2;
            }
        }

        // Tab (.tab): ink slab open at the bottom; the active tab is raised with a gold top stripe.
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

        // ---------------------------------------------------------------- overview

        void BuildOverview(Unit u)
        {
            int pad = 8, gap = 8, colW = (MainW - 2 * pad - 2 * gap) / 3;
            int x0 = MainX + pad, y0 = PanelY + 8, row2 = PanelY + 160;

            // Attributes (focusable rows; the tooltip floats over the column)
            H2(x0, y0, "Attributes");
            int y = y0 + 16;
            foreach (var a in AbilityScores.AllAbilities)
            {
                var ab = a;
                var row = Ui.Button(page, "", x0, y, colW, () => ShowTip(u.ExplainAbility(ab)), 16, showCursor: false, style: ButtonStyle.Flat);
                row.tooltip = u.ExplainAbility(ab);
                Ui.Label(row.transform, $"{a} · {AbilityName(a)}", 3, 5, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                int score = u.Score(ab), bonus = score - u.BaseScores[ab];
                if (bonus != 0) Ui.Label(row.transform, $"{(bonus > 0 ? "+" : "")}{bonus}", colW - 20, 5, Palette.Mist3, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Label(row.transform, score.ToString(), colW - 3, 5, Palette.Mist1, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Rect(page, x0, y + 16, colW, 1, Palette.Ink3, Ui.Panels + 2);
                attributeRows.Add(row);
                menu.Add(row);
                y += 18;
            }

            // Derived combat stats
            int x1 = x0 + colW + gap;
            H2(x1, y0, "Derived Stats");
            y = y0 + 16;
            foreach (var st in OverviewDerived)
            {
                var def = Database.DerivedStatDefs[st];
                Ui.Label(page, PixelFont.Fit((def.displayName ?? def.shortName).ToUpperInvariant(), colW - 30), x1, y + 5, Palette.Mist2, font: FontKind.Interface);
                Ui.Label(page, u.Derived.FormatValue(st), x1 + colW, y + 5, Palette.Mist1, Align.Right, font: FontKind.Interface);
                Ui.Rect(page, x1, y + 16, colW, 1, Palette.Ink3, Ui.Panels + 2);
                y += 18;
            }

            // Resistances: 2-column grid of element chips
            int x2 = x1 + colW + gap;
            H2(x2, y0, "Resistances");
            var entries = new List<(Element el, float v, string note)> { (Element.Physical, 0f, ""), (Element.Arcane, 0f, "FIXED") };
            foreach (ElementType et in Enum.GetValues(typeof(ElementType)))
            {
                if (et == ElementType.None) continue;
                float v = u.elementalResistances.GetFinalResistance(et);
                entries.Add((ToElement(et), v, v > 0 ? "RESIST" : v < 0 ? "WEAK" : ""));
            }
            for (int i = 0; i < entries.Count; i++)
            {
                var (el, v, note) = entries[i];
                int ex = x2 + (i % 2) * (colW / 2), ey = y0 + 16 + (i / 2) * 15;
                PartyScreen.ElementChip(page, el, ex, ey);
                string pct = v == 0 ? "0%" : $"{(v > 0 ? "+" : "-")}{Mathf.RoundToInt(Mathf.Abs(v) * 100)}%";
                Ui.Label(page, pct, ex + 17, ey + 3, v > 0 ? Palette.Ok : v < 0 ? Palette.Err : Palette.Mist1, font: FontKind.Interface);
                if (note != "") Ui.Label(page, note, ex + 20 + PixelFont.Measure(pct), ey + 3, Palette.Mist3, font: FontKind.Interface);
            }

            // Status effects
            H2(x0, row2, "Status Effects");
            Ui.Label(page, PixelFont.Wrap("NONE ACTIVE. BUFFS AND DEBUFFS SHOW HERE DURING A DUNGEON RUN.", colW), x0, row2 + 18, Palette.Mist3, font: FontKind.Interface);

            // Equipment: 4x2 slots
            H2(x1, row2, "Equipment");
            int filled = 0;
            for (int i = 0; i < EquipmentSlots.All.Length; i++)
            {
                var slot = EquipmentSlots.All[i];
                u.equipped.TryGetValue(slot, out var eq);
                if (eq != null) filled++;
                Ui.ItemSlot(page, x1 + (i % 4) * 23, row2 + 17 + (i / 4) * 23, 20, eq, slot);
            }
            var empty = EquipmentSlots.All.Where(s => !u.equipped.TryGetValue(s, out var e) || e == null).Select(EquipmentSlots.DisplayName).ToList();
            string cap = $"{filled} / 8 SLOTS" + (empty.Count > 0 ? $" · {empty[0].ToUpperInvariant()} EMPTY" : "");
            Ui.Label(page, PixelFont.Fit(cap, colW), x1, row2 + 66, Palette.Mist2, font: FontKind.Interface);

            // Skills: active squares, passive diamonds
            H2(x2, row2, "Skills");
            Ui.Label(page, "ACTIVE", x2, row2 + 23, Palette.Mist3, font: FontKind.Interface);
            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
            {
                string id = u.skillLoadout.ActiveSlots[i];
                int sx = x2 + 36 + i * 23, sy = row2 + 17;
                if (!string.IsNullOrEmpty(id))
                {
                    Ui.Image(page, PixelArt.Box(19, 19, Palette.Hex(0x2B2210), Palette.Gold2), sx, sy, Ui.Widget);
                    Ui.Label(page, Initials(id), sx + 10, sy + 6, Palette.Gold1, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
                }
                else Ui.Image(page, PixelArt.DashedBadge(19, 19, Palette.Ink3, Palette.Ink5), sx, sy, Ui.Widget);
            }
            Ui.Label(page, "PASSIVE", x2, row2 + 47, Palette.Mist3, font: FontKind.Interface);
            for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
            {
                string id = u.skillLoadout.PassiveSlots[i];
                int sx = x2 + 38 + i * 21, sy = row2 + 41;
                bool has = !string.IsNullOrEmpty(id);
                Ui.Image(page, PixelArt.Diamond(17, has ? Palette.Hex(0x2B2210) : Palette.Ink3), sx, sy, Ui.Widget);
                Ui.Image(page, PixelArt.Diamond(17, has ? Palette.Gold2 : Palette.Ink5, false), sx, sy, Ui.Widget + 1);
                if (has) Ui.Label(page, Initials(id), sx + 9, sy + 5, Palette.Gold1, Align.Center, order: Ui.Widget + 2, font: FontKind.Interface);
            }
        }

        void H2(int x, int y, string text) => Ui.Label(page, text, x, y, Palette.Gold2);

        static string Initials(string skillId)
        {
            string name = Database.Skills.TryGetValue(skillId, out var s) ? s.name
                : Database.Passives.TryGetValue(skillId, out var p) ? p.name : skillId;
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

        // ------------------------------------------------------- detail tab lists

        // Attributes tab: every ability with score and modifier; focus shows its source breakdown.
        void BuildAttributeList(Unit u)
        {
            int x = MainX + 10, w = MainW / 2 - 16, y = PanelY + 10;
            H2(x, y, "Attributes");
            y += 16;
            foreach (var a in AbilityScores.AllAbilities)
            {
                var ab = a;
                var row = Ui.Button(page, "", x, y, w, () => ShowTip(u.ExplainAbility(ab)), 20, showCursor: false, style: ButtonStyle.Flat);
                row.tooltip = u.ExplainAbility(ab);
                Ui.Label(row.transform, $"{a} · {AbilityName(a)}", 4, 7, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Label(row.transform, $"{u.Score(ab)}   {AbilityScores.FormatMod(u.Mod(ab))}", w - 4, 7, Palette.Mist1, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Rect(page, x, y + 20, w, 1, Palette.Ink3, Ui.Panels + 2);
                menu.Add(row);
                y += 23;
            }
            DetailPane(x + w + 16, PanelY + 10, MainW - w - 46);
        }

        // Combat tab: all 16 derived stats; focus shows the formula breakdown.
        void BuildCombatList(Unit u)
        {
            int x = MainX + 10, colW = (MainW - 30) / 2, y0 = PanelY + 26;
            H2(x, PanelY + 10, "Derived Combat Stats");
            for (int i = 0; i < DisplayedDerivedStats.Length; i++)
            {
                var st = DisplayedDerivedStats[i];
                var def = Database.DerivedStatDefs[st];
                int rx = x + (i / 8) * (colW + 10), ry = y0 + (i % 8) * 19;
                var row = Ui.Button(page, "", rx, ry, colW, () => ShowTip(GetStatTooltip(st)), 17, showCursor: false, style: ButtonStyle.Flat);
                row.tooltip = GetStatTooltip(st);
                Ui.Label(row.transform, PixelFont.Fit((def.displayName ?? def.shortName).ToUpperInvariant(), colW - 40), 4, 5, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Label(row.transform, u.Derived.FormatValue(st), colW - 4, 5, Palette.Mist1, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Rect(page, rx, ry + 17, colW, 1, Palette.Ink3, Ui.Panels + 2);
                menu.Add(row);
            }
            DetailPane(x, y0 + 8 * 19 + 8, MainW - 20);
        }

        void BuildStatusList()
        {
            int x = MainX + 10;
            H2(x, PanelY + 10, "Status Effects");
            Ui.Label(page, PixelFont.Wrap("NO ACTIVE EFFECTS. BUFFS AND DEBUFFS APPLIED IN COMBAT ARE LISTED HERE WITH THEIR REMAINING TURNS.", MainW - 20), x, PanelY + 28, Palette.Mist3, font: FontKind.Interface);
        }

        // Fixed formula pane for the Attributes / Combat tabs (.tip without the arrow).
        void DetailPane(int x, int y, int w)
        {
            int h = BodyY + BodyH - y - 8;
            Ui.Image(page, PixelArt.Box(w, h, Palette.Ink2, Palette.Mist3), x, y, Ui.Panels + 2);
            detail = Ui.Label(page, "", x + 8, y + 8, Palette.Mist2, order: Ui.Text, font: FontKind.Interface);
            detailWidth = w - 16;
        }

        // ------------------------------------------------------------- behaviour

        void ShowTip(string text)
        {
            TooltipLabel?.Set(PixelFont.Wrap(text ?? "", 448));
            detail?.Set(PixelFont.Wrap((text ?? "").ToUpperInvariant(), detailWidth));
        }

        void OnFocus(Selectable s)
        {
            if (s == null) return;
            focusIndex = menu.items.IndexOf(s);
            ShowTip(s.tooltip);
            if (tipBox == null) return;

            // Overview attribute rows float a tooltip (.tip) just below the focused row.
            bool show = attributeRows.Contains(s);
            tipBox.gameObject.SetActive(show);
            tipText.gameObject.SetActive(show);
            if (!show) return;
            string text = PixelFont.Wrap((s.tooltip ?? "").ToUpperInvariant(), 170);
            int h = 10 + (text.Count(c => c == '\n') + 1) * PixelFont.LineHeight;
            var p = s.transform.localPosition;
            int x = (int)p.x + 20, y = (int)-p.y + 19;
            tipBox.sprite = PixelArt.Box(180, h, Palette.Ink2, Palette.Mist3);
            tipBox.transform.localPosition = new Vector3(x, -y, 0);
            tipText.Init(tipText.GetComponent<SpriteRenderer>(), x + 6, y + 5, Align.Left, 1, FontKind.Interface);
            tipText.Set(text);
        }
    }
}
