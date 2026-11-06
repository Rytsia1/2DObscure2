using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Character Creation & 4-Hero Party Assembly (Character Creation System Specification Sections 2-10).
    // Starlit Reliquary boards B1 (Race) · B2 (Class) · B3 (Background) at 640x360:
    // title + 8-step chip strip, three columns (preview | choices | gold detail), footer with Back / hints / Next.
    // Abilities, Passive, Gear and Review pages reuse the same three-column frame.
    public class CharacterCreationScreen : GameScreen
    {
        public NewGamePartyBuilder Builder { get; private set; }
        public CharacterCreationSession Session => Builder.CurrentSession;
        public CharacterRoster Roster { get; private set; }

        public Label RemainingPointsLabel { get; private set; }
        public Label StepLabel { get; private set; }
        public Label MethodLabel { get; private set; }
        public Label RaceLabel { get; private set; }
        public Label ClassLabel { get; private set; }
        public Label ClassIdentityLabel { get; private set; }
        public Label RaceFeatLabel { get; private set; }
        public Label BackgroundLabel { get; private set; }
        public Label BackgroundPerkLabel { get; private set; }
        public Label StarterPassiveLabel { get; private set; }
        public Label EquipmentSlotLabel { get; private set; }
        public Label PartySummaryLabel { get; private set; }
        public Label HintLabel { get; private set; }

        public readonly Dictionary<Ability, Label> AbilityRowLabels = new Dictionary<Ability, Label>();
        public readonly Dictionary<DerivedStatId, Label> PreviewStatLabels = new Dictionary<DerivedStatId, Label>();

        public EquipmentSlot SelectedEquipmentSlot { get; private set; } = EquipmentSlot.MainHand;

        // Pages follow the board's step strip. "Name" is always shown done (no text entry in this build).
        public enum Page { Name, Race, Class, Abilities, Background, Passive, Gear, Review }
        static readonly string[] PageNames = { "NAME", "RACE", "CLASS", "ABILITIES", "BACKGROUND", "PASSIVE", "GEAR", "REVIEW" };
        public Page CurrentPage { get; private set; } = Page.Race;

        // Frame geometry (1280x720 board halved).
        const int Top = 35, Bottom = 319, ColH = Bottom - Top, FooterY = 327, Left = 14, RightEdge = 626;

        static readonly string[] RaceIds = Database.PlayableRaceIds;
        static readonly string[] ClassIds = Database.PlayableClassIds;
        static readonly string[] BackgroundIds = Database.PlayableBackgroundIds;
        static readonly string[] TemplateIds = Database.TemplateIds;

        static readonly DerivedStatId[] PreviewedStats =
        {
            DerivedStatId.MaxHP, DerivedStatId.MaxMP, DerivedStatId.PhysicalPower, DerivedStatId.RangedPower,
            DerivedStatId.MagicPower, DerivedStatId.HealingPower, DerivedStatId.SupportPower, DerivedStatId.PhysicalDefense,
            DerivedStatId.MagicDefense, DerivedStatId.Evasion, DerivedStatId.CriticalChance, DerivedStatId.Initiative,
            DerivedStatId.PhysicalBreakPower, DerivedStatId.StatusResistance, DerivedStatId.QteTimingBonus,
        };

        Menu menu;
        Transform page;
        int templateIndex = -1;
        int focusIndex;
        bool rebuilding;
        readonly Dictionary<Selectable, Action> focusActions = new Dictionary<Selectable, Action>();

        public CharacterCreationScreen() : this(null, null, null) { }

        public CharacterCreationScreen(CharacterCreationSession session = null, CharacterRoster roster = null, NewGamePartyBuilder builder = null)
        {
            if (builder != null) Builder = builder;
            else if (session != null) Builder = new NewGamePartyBuilder(session);
            else if (Game?.NewGameBuilder != null && Application.isPlaying) Builder = Game.NewGameBuilder;
            else Builder = new NewGamePartyBuilder();

            Roster = roster ?? (Game != null ? Game.Roster : new CharacterRoster(startWithEmptyParty: true));
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = OnBackPressed;
            menu.onFocus = OnMenuFocus;
            if (InReview) CurrentPage = Page.Review;
            Builder.Changed += Refresh;
            Refresh();
        }

        bool InReview => Builder.FlowState == NewGameFlowState.PendingPartyReview && !Builder.EditingExistingMember;

        // ------------------------------------------------------------------ build

        public void Refresh()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try { Rebuild(); }
            finally { rebuilding = false; }
        }

        void Rebuild()
        {
            if (page != null) SafeDestroy(page.gameObject);
            page = new GameObject("Page:" + CurrentPage).transform;
            page.SetParent(Root, false);
            menu.items.Clear();
            focusActions.Clear();
            AbilityRowLabels.Clear();
            PreviewStatLabels.Clear();

            // Off-screen binding labels: tests and the hint band read these on every page.
            var hidden = new GameObject("Bindings").transform;
            hidden.SetParent(page, false);
            hidden.gameObject.SetActive(false);
            Label Bind() => Ui.Label(hidden, "", 0, 0, Palette.Mist1);
            RaceLabel = Bind(); ClassLabel = Bind(); ClassIdentityLabel = Bind(); RaceFeatLabel = Bind();
            BackgroundLabel = Bind(); BackgroundPerkLabel = Bind(); StarterPassiveLabel = Bind(); EquipmentSlotLabel = Bind();
            MethodLabel = Bind(); PartySummaryLabel = Bind(); HintLabel = Bind(); RemainingPointsLabel = Bind(); StepLabel = Bind();
            foreach (var st in PreviewedStats) PreviewStatLabels[st] = Bind();
            foreach (var a in AbilityScores.AllAbilities) AbilityRowLabels[a] = Ui.Label(hidden, a.ToString(), 0, 0, Palette.Mist1);

            BuildHeader();
            switch (CurrentPage)
            {
                case Page.Race: BuildRacePage(); break;
                case Page.Class: BuildClassPage(); break;
                case Page.Abilities: BuildAbilitiesPage(); break;
                case Page.Background: BuildBackgroundPage(); break;
                case Page.Passive: BuildPassivePage(); break;
                case Page.Gear: BuildGearPage(); break;
                default: BuildReviewPage(); break;
            }
            BuildFooter();
            FillBindings();

            if (menu.items.Count > 0)
                menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
        }

        void BuildHeader()
        {
            Ui.Label(page, "Create Character", Left, 14, Palette.Gold2);

            // Step chips, right-aligned: done = ink, now = gold, next = dashed.
            var texts = PageNames.Select((n, i) => (i < (int)CurrentPage ? "+" : (i + 1).ToString()) + " " + n).ToArray();
            var widths = texts.Select(t => PixelFont.Measure(t) + 10).ToArray();
            int x = RightEdge - widths.Sum() - 2 * (widths.Length - 1);
            for (int i = 0; i < texts.Length; i++)
            {
                bool done = i < (int)CurrentPage, now = i == (int)CurrentPage;
                var sprite = done ? PixelArt.Badge(widths[i], 16, Palette.Ink2, Palette.Ink4)
                    : now ? PixelArt.Badge(widths[i], 16, Palette.Hex(0x2B2210), Palette.Gold2)
                    : PixelArt.DashedBadge(widths[i], 16, Color.clear, Palette.Ink5);
                Ui.Image(page, sprite, x, 11, Ui.Panels);
                Ui.Label(page, texts[i], x + 5, 15, done ? Palette.Mist2 : now ? Palette.Gold1 : Palette.Mist3, order: Ui.Text, font: FontKind.Interface);
                x += widths[i] + 2;
            }
        }

        void BuildFooter()
        {
            menu.Add(Ui.Button(page, "Back", Left, FooterY, 52, OnBackPressed, 22, showCursor: false)).tooltip = "PREVIOUS STEP OR RETURN TO TITLE SCREEN";
            int hx = Left + 62, hy = FooterY + 5;
            switch (CurrentPage)
            {
                case Page.Race: Ui.KeyHint(page, hx, hy, "BROWSE RACES", "UP", "DN"); break;
                case Page.Class: Ui.KeyHint(page, hx, hy, "BROWSE CLASSES", "LT", "RT"); break;
                case Page.Abilities: Ui.KeyHint(page, hx, hy, "ADJUST SCORE", "LT", "RT"); break;
                case Page.Gear: Ui.KeyHint(page, hx, hy, "CHANGE ITEM", "LT", "RT"); break;
                case Page.Review: Ui.KeyHint(page, hx, hy, "SELECT HERO", "UP", "DN"); break;
                default: Ui.KeyHint(page, hx, hy, "BROWSE", "UP", "DN", "LT", "RT"); break;
            }

            if (CurrentPage == Page.Review)
            {
                var confirm = InReview
                    ? Ui.Button(page, "Confirm Party", RightEdge - 110, FooterY, 110, () => TryConfirmPartyAndBegin(out _), 22, interactable: Builder.CanConfirmParty, showCursor: false, style: ButtonStyle.Primary)
                    : Ui.Button(page, "Add Hero", RightEdge - 110, FooterY, 110, ConfirmAndAddToParty, 22, interactable: Session.CanConfirm, showCursor: false, style: ButtonStyle.Primary);
                confirm.tooltip = InReview ? "CONFIRM 4-HERO PARTY, RECEIVE 600 STARTER GOLD & ENTER SANCTUARY HUB" : "CONFIRM HERO AND ADD TO PARTY";
                menu.Add(confirm);
                return;
            }

            string next = "Next: " + (CurrentPage == Page.Background ? "Starter Passive" : ((Page)((int)CurrentPage + 1)).ToString());
            int nw = PixelFont.Measure(next, FontKind.DotGothic) + 24;
            menu.Add(Ui.Button(page, next, RightEdge - nw, FooterY, nw, () => GoToPage((Page)((int)CurrentPage + 1)), 22, showCursor: false, style: ButtonStyle.Primary)).tooltip = "CONTINUE TO THE NEXT STEP";
            Ui.Button(page, "Confirm", RightEdge - nw - 72, FooterY, 64, null, 22, interactable: false, showCursor: false);
            const string lockText = "CONFIRM UNLOCKS ON REVIEW";
            int lx = RightEdge - nw - 84 - PixelFont.Measure(lockText);
            Ui.Image(page, LockGlyph, lx - 9, FooterY + 7, Ui.Text);
            Ui.Label(page, lockText, lx, FooterY + 8, Palette.Mist2, font: FontKind.Interface);
        }

        static Sprite LockGlyph => PixelArt.FromRows(new[] { ".aa.", "a..a", "aaaa", "aaaa", "aaaa" });

        // ---------------------------------------------------------------- columns

        // Left column (.pnl): figure preview over a summary of the hero being built.
        void BuildPreview(int w, int previewH, string topTag, IEnumerable<(string text, TagKind kind)> tags, string note = null)
        {
            Ui.Panel(page, Left, Top, w, ColH);
            Ui.Image(page, PixelArt.Gradient(w - 4, previewH, Palette.Hex(0x16204A), Palette.Ink1), Left + 2, Top + 2, Ui.Panels + 1);
            Ui.Rect(page, Left + 2, Top + 2 + previewH, w - 4, 1, Palette.Ink4, Ui.Panels + 2);
            int plinthW = w / 2 + 6;
            Ui.Rect(page, Left + (w - plinthW) / 2, Top + previewH - 10, plinthW, 6, Palette.Ink4, Ui.Panels + 2);
            var rows = Session.Class?.sprite;
            if (rows != null && rows.Length > 0)
            {
                int scale = Mathf.Max(1, (previewH - 26) / rows.Length);
                var fig = Ui.Image(page, PixelArt.FromRows(rows), 0, 0, Ui.Widget);
                fig.transform.localScale = new Vector3(scale, scale, 1);
                int fw = rows[0].Length * scale;
                fig.transform.localPosition = new Vector3(Left + (w - fw) / 2, -(Top + previewH - 10 - rows.Length * scale), 0);
            }
            if (topTag != null) Ui.Tag(page, topTag, Left + 6, Top + 6);
            if (note != null) Ui.Label(page, PixelFont.Fit(note, w - 12), Left + 6, Top + 7, Palette.Mist3, font: FontKind.Interface);

            int y = Top + previewH + 9;
            Ui.Label(page, HeroName, Left + 8, y, Palette.Mist1);
            Ui.Label(page, "LV 1", Left + w - 8, y + 2, Palette.Mist2, Align.Right, font: FontKind.Interface);
            y += 15;
            int tx = Left + 8;
            foreach (var (text, kind) in tags)
            {
                string t = PixelFont.Fit(text.ToUpperInvariant(), Left + w - 8 - tx - 8);
                if (t.Length < 2) break;
                tx += Ui.Tag(page, t, tx, y, kind) + 3;
            }
            y += 16;

            if (CurrentPage == Page.Race)
            {
                Ui.Label(page, "SCORES AFTER RACIAL BONUS", Left + 8, y, Palette.Mist3, font: FontKind.Interface);
                y += 11;
            }
            AbilityGrid(Left + 8, y, w - 16);
            y += 37;
            if (CurrentPage == Page.Race)
                Ui.Label(page, PixelFont.Wrap("CAP 15 BEFORE BONUS · 17 AFTER · 20 BY PROGRESSION", w - 16), Left + 8, y, Palette.Mist2, font: FontKind.Interface);
            else if (CurrentPage == Page.Background)
                Ui.Label(page, PixelFont.Wrap("BACKGROUND ADDS ONE PASSIVE PERK. IT NEVER CHANGES RACE, CLASS OR SKILLS.", w - 16), Left + 8, y, Palette.Mist2, font: FontKind.Interface);
        }

        string HeroName => string.IsNullOrEmpty(Session.CharacterName) ? $"Hero {Builder.CurrentMemberIndex + 1}" : Session.CharacterName;

        void AbilityGrid(int x, int y, int w)
        {
            int cw = (w - 6) / 3;
            var fin = Session.FinalScores;
            var race = Session.RacialModifiers;
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var a = AbilityScores.AllAbilities[i];
                int bx = x + (i % 3) * (cw + 3), by = y + (i / 3) * 17;
                bool bonus = race[a] > 0 && CurrentPage == Page.Race;
                Ui.Image(page, PixelArt.Badge(cw, 15, Palette.Ink0, bonus ? Palette.Gold3 : Palette.Ink3), bx, by, Ui.Panels + 2);
                Ui.Label(page, a.ToString(), bx + 3, by + 4, Palette.Mist3, order: Ui.Text, font: FontKind.Interface);
                Ui.Label(page, fin[a].ToString(), bx + cw - 3, by + 4, bonus ? Palette.Gold1 : Palette.Mist1, Align.Right, order: Ui.Text, font: FontKind.Interface);
            }
        }

        // Panel header (.ph) with an optional muted note on the right.
        void PanelHeader(int x, int w, string title, string note = null)
        {
            Ui.Header(page, title, x + 8, Top + 6, w - 4);
            if (note != null) Ui.Label(page, note, x + w - 8, Top + 8, Palette.Mist2, Align.Right, font: FontKind.Interface);
        }

        // Selectable card (.pnl.flat → .pnl.gold): focusing a card with a select action chooses it, like the board's ↑↓ browse.
        Selectable Card(int x, int y, int w, int h, string title, bool chosen, Action select)
        {
            var c = Ui.Button(page, title, x, y, w, select, h, style: ButtonStyle.Card);
            c.selected = chosen;
            c.visual(false);
            if (select != null) focusActions[c] = select;
            menu.Add(c);
            return c;
        }

        void Rule(int x, int y, int w) => Ui.Rect(page, x, y, w, 1, Palette.Ink5, Ui.Panels + 2);

        int SectionTitle(int x, int y, string label, string name, string tag = null)
        {
            Ui.Label(page, label, x, y + 2, Palette.Gold2, font: FontKind.Interface);
            int nx = x + PixelFont.Measure(label) + 6;
            if (name != null) { Ui.Label(page, name, nx, y, Palette.Gold1); nx += PixelFont.Measure(name, FontKind.DotGothic) + 6; }
            if (!string.IsNullOrEmpty(tag)) Ui.Tag(page, tag, nx, y);
            return y + 15;
        }

        int Paragraph(int x, int y, int w, string text, Color? color = null)
        {
            string wrapped = PixelFont.Wrap((text ?? "").ToUpperInvariant(), w);
            Ui.Label(page, wrapped, x, y, color ?? Palette.Mist2, font: FontKind.Interface);
            return y + (wrapped.Count(c => c == '\n') + 1) * PixelFont.LineHeight;
        }

        static string FirstClause(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int dot = s.IndexOf('.');
            return dot > 0 ? s.Substring(0, dot) : s;
        }

        static string BonusText(RaceDef r) =>
            string.Join(" ", AbilityScores.AllAbilities.Where(a => r.abilityModifiers[a] != 0).Select(a => $"+{r.abilityModifiers[a]} {a}"));

        // ------------------------------------------------------------------ pages

        // B1 · Race: 250 | 300 | 1fr
        void BuildRacePage()
        {
            BuildPreview(125, 118, $"MEMBER {Builder.CurrentMemberIndex + 1} / 4", new[] { (Session.Race.name, TagKind.Gold) });

            int mx = 147, mw = 150;
            Ui.Panel(page, mx, Top, mw, ColH);
            PanelHeader(mx, mw, "Choose a Race", $"{RaceIds.Length} RACES");
            for (int i = 0; i < RaceIds.Length; i++)
            {
                var r = Database.Races[RaceIds[i]];
                var c = Card(mx + 17, Top + 23 + i * 29, mw - 22, 27, r.name, r.id == Session.Race.id, () => SelectRaceById(r.id));
                Ui.Label(c.transform, BonusText(r), mw - 28, 6, Palette.Gold2, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Label(c.transform, PixelFont.Fit(FirstClause(r.description).ToUpperInvariant(), mw - 34), 6, 17, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
            }

            int rx = 305, rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            var race = Session.Race;
            PanelHeader(rx, rw, race.name, PixelFont.Fit(FirstClause(race.description).ToUpperInvariant(), rw - 90));
            int x = rx + 10, y = Top + 25, tw = rw - 20;

            y = SectionTitle(x, y, "RACIAL ABILITY BONUS", null);
            Ui.Label(page, "BONUS:", x, y + 4, Palette.Mist2, font: FontKind.Interface);
            int sx = x + 34;
            foreach (var a in AbilityScores.AllAbilities.Where(a => race.abilityModifiers[a] != 0))
            {
                string seg = $"+{race.abilityModifiers[a]} {a}";
                int sw = PixelFont.Measure(seg) + 12;
                Ui.Image(page, PixelArt.Badge(sw, 14, Palette.Gold2, Palette.Ink5), sx, y + 1, Ui.Panels + 2);
                Ui.Label(page, seg, sx + 6, y + 4, Palette.Hex(0x1D1404), order: Ui.Text, font: FontKind.Interface);
                sx += sw - 1;
            }
            Ui.Label(page, "APPLIED AFTER POINT BUY", sx + 8, y + 4, Palette.Mist2, font: FontKind.Interface);
            y += 22;
            Rule(x, y, tw); y += 7;

            var feat = race.racialFeat;
            y = SectionTitle(x, y, "RACIAL FEAT", feat?.name ?? "None");
            y = Paragraph(x, y, tw, feat?.description ?? "") + 6;
            Rule(x, y, tw); y += 7;

            var util = race.racialUtility;
            y = SectionTitle(x, y, "RACIAL UTILITY", util?.name ?? "None", util?.utilityTag);
            Paragraph(x, y, tw, util?.description ?? "");
        }

        // B2 · Class: 290 | 1fr | 360
        void BuildClassPage()
        {
            BuildPreview(145, 135, null, new[] { (Session.Race.name, TagKind.Cls), (Session.Class.name, TagKind.Gold) }, "PREVIEW UPDATES WITH RACE & CLASS");

            int mx = 167, rx = 446, mw = rx - 8 - mx;
            Ui.Panel(page, mx, Top, mw, ColH);
            PanelHeader(mx, mw, "Choose a Class", $"{ClassIds.Length} CLASSES");
            int gx = mx + 18, gw = (mw - 18 - 7 - 10) / 3;
            for (int i = 0; i < ClassIds.Length; i++)
            {
                var c = Database.Classes[ClassIds[i]];
                var card = Card(gx + (i % 3) * (gw + 5), Top + 24 + (i / 3) * 57, gw, 52, "", c.id == Session.Class.id, () => SelectClassById(c.id));
                card.label.Init(card.label.GetComponent<SpriteRenderer>(), 26, 9, Align.Left, 1, FontKind.DotGothic);
                card.label.Set(PixelFont.Fit(c.name, gw - 30, FontKind.DotGothic));
                // Portrait swatch (.pt) in the class accent.
                Ui.Image(card.transform, PixelArt.Box(18, 18, Color.Lerp(Palette.ForClass(c.id), Palette.Ink1, 0.6f), Palette.Ink5), 5, 5, Ui.Widget + 1);
                Ui.Rect(card.transform, 9, 15, 10, 6, Palette.ForClass(c.id), Ui.Widget + 2);
                Ui.Label(card.transform, PixelFont.Wrap((c.role ?? "").ToUpperInvariant(), gw - 10), 5, 28, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
            }

            int rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            var cls = Session.Class;
            PanelHeader(rx, rw, cls.name);
            int tx = rx + 8, y = Top + 24, tw = rw - 16;
            int tagX = tx + Ui.Tag(page, PixelFont.Fit((cls.role ?? "").ToUpperInvariant(), 80), tx, y) + 3;
            string ident = PixelFont.Fit((cls.gameplayIdentity ?? "").ToUpperInvariant(), tw - (tagX - tx) - 8);
            if (ident.Length > 1) Ui.Tag(page, ident, tagX, y);
            y += 16;
            y = Paragraph(tx, y, tw, cls.description) + 6;

            Ui.Label(page, "DERIVED STATS WITH THIS CLASS", tx, y, Palette.Mist3, font: FontKind.Interface);
            y += 11;
            var preview = Session.PreviewDerivedStats;
            foreach (var st in new[] { DerivedStatId.MaxHP, DerivedStatId.MaxMP, DerivedStatId.PhysicalPower, DerivedStatId.MagicPower, DerivedStatId.PhysicalDefense })
                y = StatRow(tx, y, tw, Database.DerivedStatDefs[st].shortName, preview.FormatValue(st));
            y += 6;
            Ui.Label(page, "STARTER LOADOUT · PASSIVE IN STEP 6", tx, y, Palette.Mist3, font: FontKind.Interface);
            y += 11;
            int sx = tx;
            foreach (var id in cls.startingSkills ?? Array.Empty<string>())
            {
                if (!Database.Skills.TryGetValue(id, out var sk)) continue;
                string name = PixelFont.Fit(sk.name.ToUpperInvariant(), 70);
                int w = PixelFont.Measure(name) + 22;
                if (sx + w > tx + tw) { sx = tx; y += 15; }
                Ui.Image(page, PixelArt.Badge(w, 13, Palette.Ink3, Palette.Ink5), sx, y, Ui.Widget);
                Ui.Image(page, PixelArt.Badge(9, 9, Palette.ForElement(sk.element), Palette.Ink0), sx + 2, y + 2, Ui.Widget + 1);
                Ui.Label(page, name, sx + 14, y + 3, Palette.Mist1, order: Ui.Widget + 1, font: FontKind.Interface);
                sx += w + 3;
            }
        }

        // Stat row (.sr): label left, value right, ink rule under.
        int StatRow(int x, int y, int w, string label, string value, Color? valueColor = null)
        {
            Ui.Label(page, label, x, y + 2, Palette.Mist2, font: FontKind.Interface);
            Ui.Label(page, value, x + w, y + 2, valueColor ?? Palette.Mist1, Align.Right, font: FontKind.Interface);
            Ui.Rect(page, x, y + 12, w, 1, Palette.Ink3, Ui.Panels + 2);
            return y + 15;
        }

        // Step 4 · Abilities (point buy) in the B2 frame.
        void BuildAbilitiesPage()
        {
            BuildPreview(145, 135, null, new[] { (Session.Race.name, TagKind.Cls), (Session.Class.name, TagKind.Cls) });

            int mx = 167, rx = 446, mw = rx - 8 - mx;
            Ui.Panel(page, mx, Top, mw, ColH);
            int rem = Session.PointBuy.RemainingPoints;
            PanelHeader(mx, mw, "Point Buy", $"{rem} / {Session.PointBuy.Config.budget} POINTS LEFT");

            int x = mx + 18, w = mw - 26, y = Top + 24;
            var methodBtn = Ui.Button(page, "", x, y, w, OnActivateCreationMethodButton, 18);
            methodBtn.onAdjust = CycleCreationMethodOrTemplate;
            methodBtn.tooltip = "L/R: MANUAL, TEMPLATE OR RANDOM. ENTER: REROLL";
            menu.Add(methodBtn);
            var methodText = methodBtn.label;
            y += 24;

            int[] colX = { 70, 112, 150, 186, 222 };
            string[] heads = { "BASE", "RACE", "TOTAL", "MOD", "COST" };
            Ui.Label(page, "STAT", x + 8, y, Palette.Mist3, font: FontKind.Interface);
            for (int c = 0; c < heads.Length; c++) Ui.Label(page, heads[c], x + colX[c], y, Palette.Mist3, Align.Center, font: FontKind.Interface);
            y += 11;

            var bases = Session.BaseScores; var race = Session.RacialModifiers; var fin = Session.FinalScores; var mods = Session.AbilityModifiers;
            foreach (var a in AbilityScores.AllAbilities)
            {
                var ab = a;
                var row = Ui.Button(page, "", x, y, w, () => AdjustAbility(ab, 1), 18);
                row.onAdjust = d => AdjustAbility(ab, d);
                row.tooltip = GetAbilityShortTooltip(ab);
                AbilityRowLabels[ab] = Ui.Label(row.transform, ab.ToString(), 8, 5, Palette.Mist1, order: Ui.Widget + 1, font: FontKind.Interface);
                int r = race[ab], m = mods[ab];
                void Col(int c, string s, Color col) => Ui.Label(row.transform, s, colX[c], 5, col, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
                Col(0, bases[ab].ToString(), Palette.Mist1);
                Col(1, r > 0 ? $"+{r}" : r < 0 ? r.ToString() : "-", r > 0 ? Palette.Hp : r < 0 ? Palette.HpLow : Palette.Mist3);
                Col(2, fin[ab].ToString(), Palette.Mist1);
                Col(3, AbilityScores.FormatMod(m), m > 0 ? Palette.Hp : m < 0 ? Palette.HpLow : Palette.Mist1);
                Col(4, Session.PointBuy.GetCost(ab).ToString(), Palette.Gold2);
                menu.Add(row);
                y += 20;
            }
            y += 4;
            menu.Add(Ui.Button(page, "Reset", x, y, 70, OnResetOrEditClicked, 20, showCursor: false)).tooltip = "RESET ABILITY SCORES TO 8";
            menu.Add(Ui.Button(page, "Randomize", x + 76, y, 90, () => OnRandomOrRerollSelected(), 20, showCursor: false)).tooltip = "RANDOMIZE THE CURRENT HERO";
            Paragraph(x, y + 28, w, "27 POINTS · SCORES 8 TO 15 BEFORE RACIAL BONUS. SPEND EVERY POINT TO CONFIRM.");

            int rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            PanelHeader(rx, rw, "Derived Stats");
            var preview = Session.PreviewDerivedStats;
            int ty = Top + 24;
            foreach (var st in PreviewedStats)
                ty = StatRow(rx + 8, ty, rw - 16, Database.DerivedStatDefs[st].shortName, preview.FormatValue(st)) + 1;

            // Visible method selector mirrors the binding label filled in FillBindings.
            methodText.Set(MethodText());
        }

        // B3 · Background: 260 | 1fr | 400
        void BuildBackgroundPage()
        {
            BuildPreview(130, 120, null, new[] { (Session.Race.name, TagKind.Cls), (Session.Class.name, TagKind.Cls), (Session.Background.name, TagKind.Gold) });

            int mx = 152, rx = 426, mw = rx - 8 - mx;
            Ui.Panel(page, mx, Top, mw, ColH);
            PanelHeader(mx, mw, "Choose a Background", $"{BackgroundIds.Length} BACKGROUNDS");
            int gw = (mw - 17 - 6 - 4) / 2;
            for (int i = 0; i < BackgroundIds.Length; i++)
            {
                var b = Database.Backgrounds[BackgroundIds[i]];
                var c = Card(mx + 17 + (i % 2) * (gw + 4), Top + 23 + (i / 2) * 36, gw, 32, b.name, b.id == Session.Background.id, () => SelectBackgroundById(b.id));
                Ui.Label(c.transform, PixelFont.Fit((b.narrativeIdentity ?? "").ToUpperInvariant(), gw - 10), 6, 19, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
            }

            int rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            var bg = Session.Background;
            PanelHeader(rx, rw, PixelFont.Fit($"{bg.name} · {bg.perkName}", rw - 80, FontKind.DotGothic));
            int dw = PixelFont.Measure("DRAFT VALUES") + 8;
            Ui.Image(page, PixelArt.DashedBadge(dw, 11, Color.clear, Palette.Mist3), rx + rw - dw - 8, Top + 7, Ui.Widget);
            Ui.Label(page, "DRAFT VALUES", rx + rw - dw - 4, Top + 9, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);

            int tx = rx + 8, tw = rw - 16, y = Top + 24;
            y = Paragraph(tx, y, tw, bg.description) + 6;
            Ui.Label(page, "PERK RANKS · UNLOCK BY LEVEL", tx, y, Palette.Gold2, font: FontKind.Interface);
            y += 11;
            string[] numerals = { "I", "II", "III", "IV", "V" };
            for (int r = 1; r <= 5; r++)
            {
                var rank = bg.ranks != null && bg.ranks.Length >= r ? bg.ranks[r - 1] : null;
                bool on = r == 1;
                string fx = PixelFont.Wrap((rank?.perkDescription ?? "").ToUpperInvariant(), tw - 52);
                int h = 14 + (fx.Count(ch => ch == '\n') + 1) * PixelFont.LineHeight;
                if (y + h > Bottom - 4) break;
                Ui.Image(page, on ? PixelArt.Badge(tw, h, Palette.Hex(0x2B2210), Palette.Gold2) : PixelArt.DashedBadge(tw, h, Color.clear, Palette.Ink4), tx, y, Ui.Panels + 2);
                Ui.Label(page, numerals[r - 1], tx + 5, y + 4, on ? Palette.Gold1 : Palette.Mist2, order: Ui.Text, font: FontKind.Interface);
                Ui.Label(page, $"LV {rank?.requiredLevel ?? (r * 3 - 2)}", tx + 20, y + 4, Palette.Mist3, order: Ui.Text, font: FontKind.Interface);
                Ui.Label(page, PixelFont.Fit((rank?.perkName ?? "").ToUpperInvariant(), tw - 56), tx + 50, y + 3, on ? Palette.Gold1 : Palette.Mist2, order: Ui.Text, font: FontKind.Interface);
                Ui.Label(page, fx, tx + 50, y + 12, Palette.Mist2, order: Ui.Text, font: FontKind.Interface);
                y += h + 2;
            }
            if (y < Bottom - 24)
                Paragraph(tx, y + 3, tw, "! EACH NEW RANK REPLACES THE ONE BEFORE — RANKS DON'T STACK.");
        }

        // Step 6 · Starter Passive in the B2 frame.
        void BuildPassivePage()
        {
            BuildPreview(145, 135, null, new[] { (Session.Race.name, TagKind.Cls), (Session.Class.name, TagKind.Cls), (Session.Background.name, TagKind.Cls) });

            int mx = 167, rx = 446, mw = rx - 8 - mx, cw = mw - 26;
            Ui.Panel(page, mx, Top, mw, ColH);
            PanelHeader(mx, mw, "Starter Passive", "FROM CLASS");
            var sp = Session.StarterPassive;
            var main = Card(mx + 18, Top + 24, cw, 64, sp?.name ?? "None", true, null);
            Ui.Label(main.transform, PixelFont.Wrap((sp?.description ?? "").ToUpperInvariant(), cw - 12), 6, 20, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
            int y = Top + 96;
            Ui.Label(page, "ALSO ACTIVE AT LEVEL 1", mx + 18, y, Palette.Mist3, font: FontKind.Interface);
            y += 12;
            var feat = Session.Race?.racialFeat;
            var c2 = Card(mx + 18, y, cw, 44, "Racial Feat · " + (feat?.name ?? "None"), false, null);
            Ui.Label(c2.transform, PixelFont.Wrap((feat?.description ?? "").ToUpperInvariant(), cw - 12), 6, 18, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
            y += 48;
            var bgRank = Session.Background?.GetRank(1);
            var c3 = Card(mx + 18, y, cw, 44, "Perk · " + (bgRank?.perkName ?? Session.Background?.perkName ?? ""), false, null);
            Ui.Label(c3.transform, PixelFont.Wrap((bgRank?.perkDescription ?? "").ToUpperInvariant(), cw - 12), 6, 18, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);

            int rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            PanelHeader(rx, rw, PixelFont.Fit(sp?.name ?? "Passive", rw - 24, FontKind.DotGothic));
            int ty = Paragraph(rx + 8, Top + 24, rw - 16, sp?.description ?? "No starter passive.") + 8;
            Paragraph(rx + 8, ty, rw - 16, "STARTER PASSIVES ARE ALWAYS ON AND DO NOT USE A SKILL SLOT.", Palette.Mist3);
        }

        // Step 7 · Gear (8 equipment slots) in the B2 frame.
        void BuildGearPage()
        {
            BuildPreview(145, 135, null, new[] { (Session.Race.name, TagKind.Cls), (Session.Class.name, TagKind.Cls) });

            int mx = 167, rx = 446, mw = rx - 8 - mx;
            Ui.Panel(page, mx, Top, mw, ColH);
            PanelHeader(mx, mw, "Starting Gear", "8 SLOTS");
            int y = Top + 24;
            foreach (var slot in EquipmentSlots.All)
            {
                var s = slot;
                var eq = Session.GetEquipped(s);
                var row = Card(mx + 18, y, mw - 26, 28, EquipmentSlots.DisplayName(s), s == SelectedEquipmentSlot, () => SelectSlot(s));
                row.onAdjust = d => { SelectedEquipmentSlot = s; CycleEquipmentItem(d); };
                Ui.Label(row.transform, PixelFont.Fit(eq != null ? eq.name.ToUpperInvariant() : "EMPTY", mw - 40), 6, 17, eq != null ? Palette.Mist2 : Palette.Mist3, order: Ui.Widget + 1, font: FontKind.Interface);
                y += 31;
            }

            int rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            var sel = Session.GetEquipped(SelectedEquipmentSlot);
            PanelHeader(rx, rw, EquipmentSlots.DisplayName(SelectedEquipmentSlot));
            int ty = Top + 24;
            Ui.Label(page, PixelFont.Fit(sel != null ? sel.name : "Empty", rw - 16, FontKind.DotGothic), rx + 8, ty, sel != null ? Palette.Gold1 : Palette.Mist3);
            ty = Paragraph(rx + 8, ty + 16, rw - 16, sel?.description ?? "NOTHING EQUIPPED IN THIS SLOT.") + 8;
            int count = Session.GetCompatibleEquipmentForSlot(SelectedEquipmentSlot).Count();
            Paragraph(rx + 8, ty, rw - 16, $"{count} COMPATIBLE ITEMS · LEFT/RIGHT TO CHANGE. TWO-HANDED WEAPONS USE MAIN HAND ONLY.", Palette.Mist3);
        }

        // Step 8 · Review: party slots + hero summary; Confirm unlocks here.
        void BuildReviewPage()
        {
            BuildPreview(145, 135, null, new[] { (Session.Race.name, TagKind.Cls), (Session.Class.name, TagKind.Cls), (Session.Background.name, TagKind.Cls) });

            int mx = 167, rx = 446, mw = rx - 8 - mx;
            bool inReview = InReview;
            Ui.Panel(page, mx, Top, mw, ColH);
            PanelHeader(mx, mw, "Party", $"{Builder.PendingParty.Count} / {CharacterRoster.PartySize}");
            int y = Top + 24;
            for (int i = 0; i < CharacterRoster.PartySize; i++)
            {
                int idx = i;
                if (i < Builder.PendingParty.Count)
                {
                    var c = Builder.PendingParty[i];
                    string race = Database.Races.TryGetValue(c.raceId ?? "", out var rd) ? rd.name : c.raceId;
                    string cls = Database.Classes.TryGetValue(c.classId ?? "", out var cd) ? cd.name : c.classId;
                    var card = Card(mx + 18, y, mw - 26, 30, c.name, inReview && i == Builder.SelectedReviewIndex,
                        inReview ? () => { if (Builder.SelectedReviewIndex != idx) { Builder.SelectReviewMember(idx); Session.LoadFromCharacterDef(Builder.PendingParty[idx]); Refresh(); } } : (Action)null);
                    Ui.Label(card.transform, $"{race} · {cls}".ToUpperInvariant(), 6, 18, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                }
                else
                {
                    Ui.Image(page, PixelArt.DashedBadge(mw - 26, 30, Palette.Ink1, Palette.Ink5), mx + 18, y, Ui.Panels + 2);
                    Ui.Label(page, i == Builder.PendingParty.Count && !inReview ? "THIS HERO" : "EMPTY SLOT", mx + 24, y + 11, Palette.Mist3, font: FontKind.Interface);
                }
                y += 34;
            }
            y += 4;
            menu.Add(Ui.Button(page, inReview ? "Edit" : "Reset", mx + 18, y, 60, OnResetOrEditClicked, 20, showCursor: false)).tooltip = inReview ? "EDIT SELECTED HERO" : "RESET ABILITY SCORES";
            menu.Add(Ui.Button(page, inReview ? "Reroll" : "Random", mx + 82, y, 64, () => OnRandomOrRerollSelected(), 20, showCursor: false)).tooltip = "RANDOMIZE OR REROLL";
            menu.Add(Ui.Button(page, "Random All", mx + 150, y, 80, () => RandomizeAll(), 20, showCursor: false)).tooltip = "RANDOMIZE ALL 4 HEROES FOR REVIEW";

            int rw = RightEdge - rx;
            Ui.Panel(page, rx, Top, rw, ColH, null, Palette.Gold3);
            PanelHeader(rx, rw, PixelFont.Fit(HeroName, rw - 24, FontKind.DotGothic));
            int tx = rx + 8, tw = rw - 16, ty = Top + 24;
            foreach (var (k, v) in new[]
            {
                ("RACE", Session.Race.name), ("CLASS", Session.Class.name), ("BACKGROUND", Session.Background.name),
                ("FEAT", Session.Race.racialFeat?.name ?? "-"), ("PASSIVE", Session.StarterPassive?.name ?? "-"),
                ("POINTS LEFT", Session.PointBuy.RemainingPoints.ToString()),
            })
                ty = StatRow(tx, ty, tw, k, PixelFont.Fit(v.ToUpperInvariant(), tw - 70));
            ty += 6;
            string err = inReview ? null : Session.Validate();
            if (err != null) Paragraph(tx, ty, tw, err, Palette.HpLow);
            else Paragraph(tx, ty, tw, inReview ? "CONFIRM THE PARTY TO RECEIVE 600 HUB GOLD AND ENTER GEISTWALD CITY." : "READY. ADD THIS HERO TO THE PARTY.", Palette.Hp);
        }

        string MethodText() =>
            Session.Method == CreationMethod.Template && !string.IsNullOrEmpty(Session.ActiveTemplateId) && Database.Templates.TryGetValue(Session.ActiveTemplateId, out var tpl)
                ? Selector("TEMPLATE", tpl.name) : Selector("MODE", Session.Method.ToString());

        // Keep the off-screen binding labels in step with the session (tests and tooltips read them).
        void FillBindings()
        {
            int rem = Session.PointBuy.RemainingPoints;
            RemainingPointsLabel.Set($"POINTS {rem}/{Session.PointBuy.Config.budget}");
            StepLabel.Set(InReview ? $"REVIEW ({Builder.SelectedReviewIndex + 1}/4)" : $"H{Builder.CurrentMemberIndex + 1}/4 STEP {(int)Session.CurrentStep}/7");
            RaceLabel.Set(Selector("RACE", Session.Race.name));
            ClassLabel.Set(Selector("CLASS", Session.Class.name));
            BackgroundLabel.Set(Selector("BG", Session.Background.name));
            ClassIdentityLabel.Set(PixelFont.Fit((Session.Class?.gameplayIdentity ?? Session.Class?.role ?? "").ToUpperInvariant(), 112));
            RaceFeatLabel.Set(PixelFont.Fit($"FEAT {(Session.Race?.racialFeat?.name ?? "NONE").ToUpperInvariant()}", 112));
            var sp = Session.StarterPassive;
            StarterPassiveLabel.Set(PixelFont.Fit($"PASSIVE {(sp != null ? sp.name.ToUpperInvariant() : "NONE")}", 125));
            var bgRank = Session.Background?.GetRank(1);
            BackgroundPerkLabel.Set(PixelFont.Fit($"PERK {(bgRank?.perkName ?? Session.Background?.perkName ?? "").ToUpperInvariant()}", 125));
            var eq = Session.GetEquipped(SelectedEquipmentSlot);
            EquipmentSlotLabel.Set(Selector(EquipmentSlots.ShortName(SelectedEquipmentSlot), eq != null ? eq.name : "EMPTY"));
            MethodLabel.Set(MethodText());

            var preview = Session.PreviewDerivedStats;
            foreach (var kv in PreviewStatLabels)
                kv.Value.Set($"{Database.DerivedStatDefs[kv.Key].shortName} {preview.FormatValue(kv.Key)}");

            if (Builder.PendingParty.Count > 0)
            {
                var members = Builder.PendingParty.Select((c, i) => $"{i + 1}{(InReview && i == Builder.SelectedReviewIndex ? "*" : "")}:{Truncate(c.name.ToUpperInvariant(), 7)}");
                PartySummaryLabel.Set(PixelFont.Fit($"PARTY ({Builder.PendingParty.Count}/{CharacterRoster.PartySize}): " + string.Join(" ", members), 308));
            }
            else PartySummaryLabel.Set($"PARTY (0/{CharacterRoster.PartySize}): CREATE 4 HEROES TO BEGIN");
        }

        // ------------------------------------------------------------- behaviour

        void OnMenuFocus(Selectable s)
        {
            if (rebuilding || s == null) return;
            focusIndex = menu.items.IndexOf(s);
            if (!string.IsNullOrEmpty(s.tooltip)) SetHint(s.tooltip);
            if (!s.selected && focusActions.TryGetValue(s, out var select)) select();
        }

        public void GoToPage(Page p)
        {
            if (p < Page.Race) p = Page.Race;
            if (p > Page.Review) p = Page.Review;
            CurrentPage = p;
            focusIndex = 0;
            var step = p switch
            {
                Page.Race => CreationStep.SelectRace,
                Page.Class => CreationStep.SelectClass,
                Page.Abilities => CreationStep.AllocateAbilityScores,
                Page.Background => CreationStep.ReviewStats,
                Page.Passive => CreationStep.ReviewStarterPassive,
                Page.Gear => CreationStep.ConfigureEquipment,
                _ => CreationStep.ConfirmCharacter,
            };
            if (Session.CurrentStep != step) Session.GoToStep(step);
            Refresh();
        }

        void SelectRaceById(string id) { if (Session.Race.id != id) { Session.SelectRace(id); Refresh(); } }
        void SelectClassById(string id) { if (Session.Class.id != id) { Session.SelectClass(id); Refresh(); } }
        void SelectBackgroundById(string id) { if (Session.Background.id != id) { Session.SelectBackground(id); Refresh(); } }
        void SelectSlot(EquipmentSlot s) { if (SelectedEquipmentSlot != s) { SelectedEquipmentSlot = s; Refresh(); } }

        static string GetAbilityShortTooltip(Ability ability) => ability switch
        {
            Ability.STR => "STRENGTH: MELEE POWER, BREAK POWER, BLOCK POWER",
            Ability.DEX => "DEXTERITY: RANGED POWER, ACCURACY, EVASION, CRIT",
            Ability.VIT => "VITALITY: MAX HP, PHYSICAL DEFENSE, STAGGER RES",
            Ability.INT => "INTELLIGENCE: MAGIC POWER, MAX MP, MAGIC BREAK",
            Ability.WIS => "WISDOM: HEALING POWER, MAGIC DEFENSE, STATUS RES",
            Ability.CHA => "CHARISMA: SUPPORT POWER, SOCIAL & PACT EFFECTS",
            _ => ability.ToString(),
        };

        static string Selector(string prefix, string value)
        {
            prefix = prefix.ToUpperInvariant();
            return $"{prefix} < {PixelFont.Fit(value.ToUpperInvariant(), 150 - PixelFont.Measure(prefix + " <  >"))} >";
        }

        static string Truncate(string s, int maxChars) => string.IsNullOrEmpty(s) ? "" : s.Length <= maxChars ? s : s.Substring(0, maxChars);

        void SetHint(string text) => HintLabel?.Set(PixelFont.Fit((text ?? "").ToUpperInvariant(), 308));

        public void CycleCreationMethodOrTemplate(int dir)
        {
            EnsureEditingMode();
            int totalModes = 2 + TemplateIds.Length;
            int currentMode = Session.Method == CreationMethod.Manual ? 0
                : Session.Method == CreationMethod.Random ? totalModes - 1
                : Math.Max(1, templateIndex + 1);

            currentMode = (currentMode + dir + totalModes) % totalModes;
            if (currentMode == 0) { templateIndex = -1; Session.SetCreationMethod(CreationMethod.Manual); }
            else if (currentMode == totalModes - 1) { templateIndex = -1; Session.TryRandomize(null, out _); }
            else { templateIndex = currentMode - 1; Session.TryApplyTemplate(TemplateIds[templateIndex], out _); }
            SetHint(Session.LastFeedbackMessage);
            Refresh();
        }

        void OnActivateCreationMethodButton()
        {
            if (Builder.FlowState == NewGameFlowState.PendingPartyReview) { CycleReviewSlot(1); return; }
            if (Session.Method == CreationMethod.Random)
            {
                Session.TryRandomize(null, out _);
                SetHint(Session.LastFeedbackMessage);
                Refresh();
            }
            else CycleCreationMethodOrTemplate(1);
        }

        void EnsureEditingMode()
        {
            if (Builder.FlowState == NewGameFlowState.PendingPartyReview && Builder.PendingParty.Count > 0)
                Builder.ReturnToEditCharacter(Builder.SelectedReviewIndex);
        }

        public void CycleRace(int dir)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.SelectRace);
            int idx = Array.IndexOf(RaceIds, Session.Race.id);
            if (idx < 0) idx = 0;
            Session.SelectRace(RaceIds[(idx + dir + RaceIds.Length) % RaceIds.Length]);
            Refresh();
        }

        public void CycleClass(int dir)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.SelectClass);
            int idx = Array.IndexOf(ClassIds, Session.Class.id);
            if (idx < 0) idx = 0;
            Session.SelectClass(ClassIds[(idx + dir + ClassIds.Length) % ClassIds.Length]);
            Refresh();
        }

        GameObject backgroundModalRoot;
        public bool IsBackgroundModalOpen => backgroundModalRoot != null && backgroundModalRoot.activeSelf;

        public void ToggleBackgroundDetailsModal()
        {
            if (IsBackgroundModalOpen) CloseBackgroundDetailsModal();
            else OpenBackgroundDetailsModal();
        }

        static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        public void OpenBackgroundDetailsModal()
        {
            if (backgroundModalRoot != null) SafeDestroy(backgroundModalRoot);
            backgroundModalRoot = new GameObject("BackgroundDetailsModal");
            backgroundModalRoot.transform.SetParent(Root, false);
            RenderBackgroundModal();
        }

        public void CloseBackgroundDetailsModal()
        {
            if (backgroundModalRoot == null) return;
            SafeDestroy(backgroundModalRoot);
            backgroundModalRoot = null;
        }

        // Dialog (.dlg) over a scrim listing every perk rank of the current background.
        void RenderBackgroundModal()
        {
            if (backgroundModalRoot == null) return;
            var children = new List<GameObject>();
            foreach (Transform child in backgroundModalRoot.transform) children.Add(child.gameObject);
            foreach (var ch in children) SafeDestroy(ch);

            var bg = Session.Background;
            if (bg == null) return;
            var t = backgroundModalRoot.transform;
            Ui.Rect(t, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 184), Ui.Overlay - 2);
            Ui.Image(t, PixelArt.Box(520, 260, Palette.Ink2, Palette.Gold3), 60, 50, Ui.Overlay - 1);
            Ui.Label(t, $"{bg.name} · {bg.perkName}", 72, 58, Palette.Gold1, order: Ui.Overlay);
            Ui.Label(t, PixelFont.Fit((bg.narrativeIdentity ?? "").ToUpperInvariant(), 496), 72, 74, Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);
            string[] numerals = { "I", "II", "III", "IV", "V" };
            for (int r = 1; r <= 5; r++)
            {
                var rDef = bg.GetRank(r == 1 ? 1 : r == 2 ? 4 : r == 3 ? 7 : r == 4 ? 10 : 13);
                int y = 90 + (r - 1) * 36;
                Ui.Label(t, PixelFont.Fit($"RANK {numerals[r - 1]} (LV {rDef?.requiredLevel ?? 1}) — {(rDef?.perkName ?? "").ToUpperInvariant()}", 496), 72, y, r == 1 ? Palette.Gold1 : Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);
                Ui.Label(t, PixelFont.Fit((rDef?.perkDescription ?? "").ToUpperInvariant(), 486), 82, y + 12, Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);
            }
            Ui.Rect(t, 62, 282, 516, 26, Palette.Ink1, Ui.Overlay);
            Ui.Rect(t, 62, 282, 516, 1, Palette.Ink4, Ui.Overlay);
            Ui.Label(t, "LEFT/RIGHT: CYCLE BACKGROUND    ENTER/ESC: CLOSE", 320, 292, Palette.Mist3, Align.Center, order: Ui.Overlay, font: FontKind.Interface);
        }

        public void CycleBackground(int dir)
        {
            EnsureEditingMode();
            int idx = Array.IndexOf(BackgroundIds, Session.Background.id);
            if (idx < 0) idx = 0;
            Session.SelectBackground(BackgroundIds[(idx + dir + BackgroundIds.Length) % BackgroundIds.Length]);
            if (IsBackgroundModalOpen) RenderBackgroundModal();
            Refresh();
        }

        public void CycleEquipmentSlot(int dir)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.ConfigureEquipment);
            int idx = Array.IndexOf(EquipmentSlots.All, SelectedEquipmentSlot);
            if (idx < 0) idx = 0;
            SelectedEquipmentSlot = EquipmentSlots.All[(idx + dir + EquipmentSlots.All.Length) % EquipmentSlots.All.Length];
            Refresh();
        }

        public void CycleEquipmentItem(int dir)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.ConfigureEquipment);
            var compatible = Session.GetCompatibleEquipmentForSlot(SelectedEquipmentSlot).ToList();
            if (compatible.Count == 0)
            {
                Sfx.Play(Sound.Deny);
                SetHint($"NO COMPATIBLE ITEMS FOR {EquipmentSlots.DisplayName(SelectedEquipmentSlot)}.");
                return;
            }
            var current = Session.GetEquipped(SelectedEquipmentSlot);
            int idx = current != null ? compatible.FindIndex(e => e.id == current.id) : -1;
            Session.TryEquip(SelectedEquipmentSlot, compatible[(idx + dir + compatible.Count) % compatible.Count], out _);
            Refresh();
        }

        void AdjustAbility(Ability ability, int delta)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.AllocateAbilityScores);
            bool ok = delta > 0 ? Session.IncreaseStat(ability) : Session.DecreaseStat(ability);
            if (!ok) Sfx.Play(Sound.Deny);
            SetHint(Session.LastFeedbackMessage);
            Refresh();
        }

        void OnResetOrEditClicked()
        {
            if (Builder.FlowState == NewGameFlowState.PendingPartyReview)
            {
                Builder.ReturnToEditCharacter(Builder.SelectedReviewIndex);
                SetHint(Builder.LastFeedback);
                GoToPage(Page.Race);
                return;
            }
            Session.ResetPointBuy();
            SetHint(Session.LastFeedbackMessage);
            Refresh();
        }

        public void OnRandomOrRerollSelected(System.Random rng = null)
        {
            if (Builder.FlowState == NewGameFlowState.PendingPartyReview && Builder.PendingParty.Count > 0)
            {
                Builder.TryRerollSelected(Builder.SelectedReviewIndex, rng, out _);
                SetHint(Builder.LastFeedback);
            }
            else
            {
                Session.TryRandomize(rng, out _);
                SetHint(Session.LastFeedbackMessage);
            }
            Refresh();
        }

        public bool RandomizeAll(System.Random rng = null)
        {
            bool ok = Builder.TryRandomizeAll(rng, out _);
            SetHint(Builder.LastFeedback);
            if (InReview) CurrentPage = Page.Review;
            Refresh();
            return ok;
        }

        public bool RerollSelected(int slotIndex, System.Random rng = null)
        {
            bool ok = Builder.TryRerollSelected(slotIndex, rng, out _);
            SetHint(Builder.LastFeedback);
            Refresh();
            return ok;
        }

        public void CycleReviewSlot(int dir)
        {
            if (Builder.PendingParty.Count == 0) return;
            int next = (Builder.SelectedReviewIndex + dir + Builder.PendingParty.Count) % Builder.PendingParty.Count;
            Builder.SelectReviewMember(next);
            Session.LoadFromCharacterDef(Builder.PendingParty[next]);
            SetHint($"REVIEWING HERO {next + 1}/4: {Builder.PendingParty[next].name}");
            Refresh();
        }

        void OnBackPressed()
        {
            if (IsBackgroundModalOpen) { CloseBackgroundDetailsModal(); return; }
            if (CurrentPage > Page.Race && !InReview) { GoToPage((Page)((int)CurrentPage - 1)); return; }
            Game?.Go(GameState.MainMenu);
        }

        public void ConfirmAndAddToParty()
        {
            if (InReview) { CycleReviewSlot(1); return; }

            if (!Builder.TryConfirmCurrentCharacter(out _, out string err))
            {
                Sfx.Play(Sound.Deny);
                SetHint(err ?? "INVALID CHARACTER CONFIGURATION");
                Refresh();
                return;
            }

            Sfx.Play(Sound.Success);
            SetHint(Builder.LastFeedback);
            CurrentPage = InReview ? Page.Review : Page.Race;
            focusIndex = 0;
            Refresh();
        }

        public bool TryConfirmPartyAndBegin(out string error)
        {
            var activeRoster = Game != null ? Game.Roster : Roster;
            if (!Builder.TryConfirmParty(activeRoster, out error))
            {
                Sfx.Play(Sound.Deny);
                SetHint(error);
                Refresh();
                return false;
            }
            Sfx.Play(Sound.Success);
            SetHint(Builder.LastFeedback);
            if (Game != null) Game.EnterHubFromCreation();
            return true;
        }

        public bool TryBeginExpedition(out string error)
        {
            var activeRoster = Game != null ? Game.Roster : Roster;
            if (Builder.CanConfirmParty && !Builder.IsPartyConfirmed && !Builder.TryConfirmParty(activeRoster, out error))
            {
                Sfx.Play(Sound.Deny);
                SetHint(error);
                Refresh();
                return false;
            }
            error = activeRoster.ValidateExpeditionStart();
            if (error != null)
            {
                Sfx.Play(Sound.Deny);
                SetHint(error);
                Refresh();
                return false;
            }
            if (Game != null) Game.EnterHubFromCreation();
            return true;
        }
    }
}
