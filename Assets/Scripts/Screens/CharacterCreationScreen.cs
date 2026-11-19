using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Character Creation & 4-Hero Party Assembly (Sections 2-10).
    // Uses Figma "02 — Character Creation — Race" 3-column shell for all 7 active pages
    // (Race, Class, Abilities, Background, Passive, Gear, Review).
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

        public enum Page { Race, Class, Abilities, Background, Passive, Gear, Review }
        static readonly string[] PageNames = { "RACE", "CLASS", "ABILITIES", "BACKGROUND", "PASSIVE", "GEAR", "REVIEW" };
        public Page CurrentPage { get; private set; } = Page.Race;

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

        UitkMenu menu, bgModalMenu;
        Transform bindingsRoot;
        VisualElement screenContainer;
        VisualElement middlePanelEl, goldPanelEl;
        VisualElement middleBody, rightBody, abilitiesHost, costTableHost, nextPreviewHost;
        VisualElement backBtn, nextBtn, keyHintRow;
        UiLabel previewMemberTag, previewNameLbl, previewNoteLbl, scoresHeaderLbl;
        VisualElement previewSpriteEl, previewRaceTag;
        readonly VisualElement[] stepChips = new VisualElement[7];
        readonly VisualElement[] abilityPreviewTags = new VisualElement[6];
        readonly Dictionary<UitkMenu.Item, Action> focusActions = new Dictionary<UitkMenu.Item, Action>();

        Ability focusedAbility = Ability.STR;
        int templateIndex = -1;
        int focusIndex;
        bool rebuilding;

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
            UiRoot = FigmaUi.Mount(Root.gameObject, "02---Character-Creation---Race");
            screenContainer = UiRoot.Q<VisualElement>(className: "_02-character-creation-race") ?? UiRoot;

            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = OnBackPressed;
            menu.onFocus = OnMenuFocus;

            BuildOffscreenBindings();
            StripMiddleAndRightBakedContent();
            CacheShellElements();

            if (InReview) CurrentPage = Page.Review;
            Builder.Changed += Refresh;
            Refresh();
        }

        bool InReview => Builder.FlowState == NewGameFlowState.PendingPartyReview && !Builder.EditingExistingMember;

        void BuildOffscreenBindings()
        {
            var hidden = new GameObject("Bindings");
            hidden.transform.SetParent(Root, false);
            hidden.SetActive(false);
            bindingsRoot = hidden.transform;
            Label Bind() => Ui.Label(bindingsRoot, "", 0, 0, Palette.Mist1);
            RaceLabel = Bind(); ClassLabel = Bind(); ClassIdentityLabel = Bind(); RaceFeatLabel = Bind();
            BackgroundLabel = Bind(); BackgroundPerkLabel = Bind(); StarterPassiveLabel = Bind(); EquipmentSlotLabel = Bind();
            MethodLabel = Bind(); PartySummaryLabel = Bind(); HintLabel = Bind(); RemainingPointsLabel = Bind(); StepLabel = Bind();
            foreach (var st in PreviewedStats) PreviewStatLabels[st] = Bind();
            foreach (var a in AbilityScores.AllAbilities) AbilityRowLabels[a] = Ui.Label(bindingsRoot, a.ToString(), 0, 0, Palette.Mist1);
        }

        void StripMiddleAndRightBakedContent()
        {
            middlePanelEl = screenContainer.Q<VisualElement>(className: "panel-default__i-4");
            goldPanelEl = screenContainer.Q<VisualElement>(className: "panel-gold__i");
            var footerBack = FigmaUi.FindButton(screenContainer, "Back");

            if (middlePanelEl != null && goldPanelEl != null && footerBack != null)
            {
                int midStart = screenContainer.IndexOf(middlePanelEl) + 1;
                int goldIdx = screenContainer.IndexOf(goldPanelEl);
                int footerIdx = screenContainer.IndexOf(footerBack);

                var toRemove = new List<VisualElement>();
                for (int i = midStart; i < goldIdx; i++) toRemove.Add(screenContainer[i]);
                for (int i = goldIdx + 1; i < footerIdx; i++) toRemove.Add(screenContainer[i]);
                foreach (var el in toRemove) el.RemoveFromHierarchy();
            }

            middleBody = new VisualElement { name = "MiddleBody" };
            middleBody.style.position = Position.Absolute;
            middleBody.style.left = 310;
            middleBody.style.top = 78;
            middleBody.style.width = 272;
            middleBody.style.height = 550;
            middleBody.style.overflow = Overflow.Hidden;
            screenContainer.Add(middleBody);

            rightBody = new VisualElement { name = "RightBody" };
            rightBody.style.position = Position.Absolute;
            rightBody.style.left = 630;
            rightBody.style.top = 78;
            rightBody.style.width = 606;
            rightBody.style.height = 550;
            rightBody.style.overflow = Overflow.Hidden;
            screenContainer.Add(rightBody);

            // 02b Point Buy merged gold panel host (shown only on Page.Abilities)
            abilitiesHost = new VisualElement
            {
                name = "AbilitiesPointBuyHost",
                style =
                {
                    position = Position.Absolute,
                    left = 296,
                    top = 70,
                    width = 956,
                    height = 566,
                    backgroundColor = Palette.Ink2,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = Palette.Gold3, borderBottomColor = Palette.Gold3,
                    borderLeftColor = Palette.Gold3, borderRightColor = Palette.Gold3,
                    overflow = Overflow.Hidden,
                    display = DisplayStyle.None
                }
            };
            screenContainer.Add(abilitiesHost);

            // 02b Left panel COST PER SCORE table + Class Key Stats (replaces score tags on Page.Abilities)
            costTableHost = new VisualElement
            {
                name = "CostPerScoreHost",
                style =
                {
                    position = Position.Absolute,
                    left = 44,
                    top = 404,
                    width = 218,
                    height = 224,
                    flexDirection = FlexDirection.Column,
                    display = DisplayStyle.None
                }
            };
            screenContainer.Add(costTableHost);
        }

        void CacheShellElements()
        {
            string[] defaultStepTexts = { "1 RACE", "2 CLASS", "3 ABILITIES", "4 BACKGROUND", "5 PASSIVE", "6 GEAR", "7 REVIEW" };
            for (int i = 0; i < 7; i++)
            {
                stepChips[i] = FigmaUi.FindInstanceWithText(screenContainer, "text-0", defaultStepTexts[i]);
                if (stepChips[i] != null)
                {
                    int pageIdx = i;
                    stepChips[i].pickingMode = PickingMode.Position;
                    stepChips[i].RegisterCallback<ClickEvent>(_ =>
                    {
                        if (!InReview) GoToPage((Page)pageIdx);
                    });
                }
            }

            previewMemberTag = FigmaUi.FindInstanceWithText(screenContainer, "text-0", "MEMBER 1 / 4")?.Q<UiLabel>("text-0");
            previewSpriteEl = screenContainer.Q<VisualElement>(className: "sprite-knight");
            previewNameLbl = FigmaUi.FindLabel(screenContainer, "Fighter");
            previewRaceTag = FigmaUi.FindInstanceWithText(screenContainer, "text-0", "HUMAN");
            scoresHeaderLbl = FigmaUi.FindLabel(screenContainer, "SCORES AFTER RACIAL BONUS");
            previewNoteLbl = FigmaUi.FindLabel(screenContainer, "CAP 15 BEFORE BONUS · 17 AFTER · 20 BY PROGRESSION");

            string[] abDefaults = { "STR 9", "DEX 8", "VIT 9", "INT 8", "WIS 8", "CHA 8" };
            for (int i = 0; i < 6; i++)
                abilityPreviewTags[i] = FigmaUi.FindInstanceWithText(screenContainer, "text-0", abDefaults[i]);

            backBtn = FigmaUi.FindButton(screenContainer, "Back");
            nextBtn = FigmaUi.FindButton(screenContainer, "Next: Class");
            if (nextBtn != null)
            {
                nextBtn.style.width = StyleKeyword.Auto;
                nextBtn.style.minWidth = 192;
                nextBtn.style.left = StyleKeyword.Auto;
                nextBtn.style.right = 28;
                nextBtn.style.paddingLeft = 18;
                nextBtn.style.paddingRight = 18;
            }
            keyHintRow = FigmaUi.FindInstanceWithText(screenContainer, "text-0", "DN")?.parent;
        }

        public void Refresh()
        {
            if (Root == null || UiRoot == null || rebuilding) return;
            rebuilding = true;
            try { Rebuild(); }
            finally { rebuilding = false; }
        }

        void Rebuild()
        {
            menu.Clear();
            focusActions.Clear();
            middleBody.Clear();
            rightBody.Clear();
            abilitiesHost?.Clear();
            costTableHost?.Clear();
            nextPreviewHost = null;

            bool isAbilities = CurrentPage == Page.Abilities;
            if (middlePanelEl != null) middlePanelEl.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;
            if (goldPanelEl != null) goldPanelEl.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;
            if (middleBody != null) middleBody.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;
            if (rightBody != null) rightBody.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;
            if (abilitiesHost != null) abilitiesHost.style.display = isAbilities ? DisplayStyle.Flex : DisplayStyle.None;
            if (costTableHost != null) costTableHost.style.display = isAbilities ? DisplayStyle.Flex : DisplayStyle.None;
            if (scoresHeaderLbl != null) scoresHeaderLbl.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;
            if (previewNoteLbl != null) previewNoteLbl.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var tag in abilityPreviewTags)
                if (tag != null) tag.style.display = isAbilities ? DisplayStyle.None : DisplayStyle.Flex;

            UpdateStepChips();
            UpdateLeftPreview();

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

            UpdateFooter();
            FillBindings();

            if (menu.items.Count > 0)
                menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            if (IsBackgroundModalOpen)
                RenderBackgroundModal();
        }

        void UpdateStepChips()
        {
            for (int i = 0; i < 7; i++)
            {
                var chip = stepChips[i];
                if (chip == null) continue;
                bool done = i < (int)CurrentPage;
                bool now = i == (int)CurrentPage;
                string txt = (done ? "+" : (i + 1).ToString()) + " " + PageNames[i];
                var lbl = chip.Q<UiLabel>("text-0");
                if (lbl != null)
                {
                    lbl.text = txt;
                    lbl.style.color = now ? Palette.Gold1 : done ? Palette.Mist2 : Palette.Mist3;
                }
                var inner = chip.childCount > 0 ? chip[0] : chip;
                inner.style.backgroundColor = now ? new Color(0.17f, 0.13f, 0.06f, 1f) : done ? Palette.Ink2 : Color.clear;
                inner.style.borderTopColor = now ? Palette.Gold2 : done ? Palette.Ink4 : Palette.Ink5;
                inner.style.borderRightColor = now ? Palette.Gold2 : done ? Palette.Ink4 : Palette.Ink5;
                inner.style.borderBottomColor = now ? Palette.Gold2 : done ? Palette.Ink4 : Palette.Ink5;
                inner.style.borderLeftColor = now ? Palette.Gold2 : done ? Palette.Ink4 : Palette.Ink5;
            }
        }

        void UpdateLeftPreview()
        {
            if (previewMemberTag != null)
                previewMemberTag.text = $"MEMBER {Builder.CurrentMemberIndex + 1} / {CharacterRoster.PartySize}";

            if (previewSpriteEl != null && Session.Class?.sprite != null)
            {
                var tex = FigmaUi.GetSpriteTexture(Session.Class.sprite, "cc_cls_" + Session.Class.id);
                if (tex != null)
                    FigmaUi.SetBackgroundContain(previewSpriteEl, tex);
            }

            if (previewNameLbl != null) previewNameLbl.text = HeroName;
            if (previewRaceTag != null)
                FigmaUi.SetInstanceText(previewRaceTag, "text-0", $"{Session.Race.name} · {Session.Class.name}".ToUpperInvariant());

            var fin = Session.FinalScores;
            var race = Session.RacialModifiers;
            for (int i = 0; i < AbilityScores.AllAbilities.Length && i < abilityPreviewTags.Length; i++)
            {
                var a = AbilityScores.AllAbilities[i];
                var chip = abilityPreviewTags[i];
                if (chip == null) continue;
                bool bonus = race[a] > 0 && CurrentPage == Page.Race;
                var lbl = chip.Q<UiLabel>("text-0");
                if (lbl != null)
                {
                    lbl.text = $"{a} {fin[a]}";
                    lbl.style.color = bonus ? Palette.Gold1 : Palette.Mist1;
                }
                var inner = chip.childCount > 0 ? chip[0] : chip;
                inner.style.borderTopColor = bonus ? Palette.Gold2 : Palette.Ink4;
                inner.style.borderRightColor = bonus ? Palette.Gold2 : Palette.Ink4;
                inner.style.borderBottomColor = bonus ? Palette.Gold2 : Palette.Ink4;
                inner.style.borderLeftColor = bonus ? Palette.Gold2 : Palette.Ink4;
            }

            if (previewNoteLbl != null)
            {
                previewNoteLbl.text = CurrentPage == Page.Background
                    ? "BACKGROUND ADDS ONE PASSIVE PERK. IT NEVER CHANGES RACE, CLASS OR SKILLS."
                    : "CAP 15 BEFORE BONUS · 17 AFTER · 20 BY PROGRESSION";
            }
        }

        void UpdateFooter()
        {
            if (backBtn != null)
                menu.Add(backBtn, "Back", OnBackPressed, tooltip: "PREVIOUS STEP OR RETURN TO TITLE SCREEN");

            var dnInst = keyHintRow != null && keyHintRow.childCount > 1 ? keyHintRow[1] : null;
            if (dnInst != null)
            {
                FigmaUi.SetInstanceText(dnInst, "text-1", CurrentPage switch
                {
                    Page.Race => "BROWSE RACES",
                    Page.Class => "BROWSE CLASSES",
                    Page.Abilities => "ADJUST SCORE",
                    Page.Gear => "CHANGE ITEM",
                    Page.Review => "SELECT HERO",
                    _ => "BROWSE"
                });
            }

            if (CurrentPage == Page.Review)
            {
                if (nextBtn is Button nb)
                {
                    bool canDo = InReview ? Builder.CanConfirmParty : Session.CanConfirm;
                    nb.text = InReview ? "Confirm Party" : "Add Hero";
                    nb.SetEnabled(canDo);
                    menu.Add(nb, nb.text, InReview ? () => TryConfirmPartyAndBegin(out _) : (Action)ConfirmAndAddToParty, interactable: canDo);
                }
                return;
            }

            if (nextBtn is Button nxt)
            {
                string nextText = "Next: " + (CurrentPage == Page.Background ? "Starter Passive" : ((Page)((int)CurrentPage + 1)).ToString());
                nxt.text = nextText;
                nxt.SetEnabled(true);
                menu.Add(nxt, nextText, () => GoToPage((Page)((int)CurrentPage + 1)), tooltip: "CONTINUE TO THE NEXT STEP");
            }
        }

        string HeroName => string.IsNullOrEmpty(Session.CharacterName) ? $"Hero {Builder.CurrentMemberIndex + 1}" : Session.CharacterName;

        void AddColumnHeader(VisualElement parent, string title, string rightNote = null)
        {
            float colWidth = parent == rightBody ? 606f : 272f;
            var hdr = FigmaUi.InstantiateTemplate("Section-Header");
            hdr.style.width = colWidth;
            hdr.style.height = 36;
            hdr.style.flexShrink = 0;
            hdr.style.marginBottom = 6;

            var innerHdr = hdr.Q<VisualElement>(className: "section-header") ?? hdr;
            innerHdr.style.width = colWidth;
            innerHdr.style.height = 36;
            innerHdr.style.flexShrink = 0;

            var topRow = hdr.Q<VisualElement>(className: "section-header__e") ?? innerHdr;
            topRow.style.width = colWidth;
            topRow.style.alignItems = UnityEngine.UIElements.Align.Center;

            FigmaUi.SetInstanceText(hdr, "text-0", title);
            var titleLbl = hdr.Q<UiLabel>("text-0");
            if (titleLbl != null)
            {
                titleLbl.style.marginLeft = 6;
                titleLbl.style.flexGrow = 1;
                titleLbl.style.flexShrink = 1;
                titleLbl.style.overflow = Overflow.Hidden;
            }

            if (!string.IsNullOrEmpty(rightNote))
            {
                var note = new UiLabel(rightNote);
                note.style.paddingLeft = 8;
                note.style.flexShrink = 0;
                note.style.fontSize = 12;
                note.style.color = Palette.Mist2;
                note.style.whiteSpace = WhiteSpace.NoWrap;
                note.style.unityTextAlign = TextAnchor.MiddleRight;
                topRow.Add(note);
            }
            parent.Add(hdr);
        }

        UitkMenu.Item AddCard(VisualElement parent, string title, string rightText, string subText, bool selected, Action select, Action<int> onAdjust = null, bool selectOnFocus = true)
        {
            var card = new VisualElement();
            card.AddToClassList("uitk-card");
            card.style.paddingTop = 4;
            card.style.paddingBottom = 4;
            card.style.marginBottom = 4;
            card.style.flexShrink = 0;

            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.SpaceBetween;
            top.style.alignItems = UnityEngine.UIElements.Align.Center;
            top.style.flexShrink = 0;

            var titleLbl = new UiLabel(title);
            titleLbl.AddToClassList("uitk-card-title");
            titleLbl.style.fontSize = 16;
            top.Add(titleLbl);

            if (!string.IsNullOrEmpty(rightText))
            {
                var rightLbl = new UiLabel(rightText);
                rightLbl.style.fontSize = 13;
                rightLbl.style.color = Palette.Gold2;
                top.Add(rightLbl);
            }

            card.Add(top);

            if (!string.IsNullOrEmpty(subText))
            {
                var sub = new UiLabel(subText);
                sub.AddToClassList("uitk-card-sub");
                sub.style.fontSize = 11;
                sub.style.flexShrink = 0;
                card.Add(sub);
            }

            parent.Add(card);
            var item = menu.Add(card, title, select, selected: selected, onAdjust: onAdjust);
            if (select != null && selectOnFocus) focusActions[item] = select;
            return item;
        }

        void AddStatRow(VisualElement parent, string label, string value, Color? valueColor = null)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = Palette.Ink3;
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;
            row.style.marginBottom = 3;

            var k = new UiLabel(label);
            k.style.fontSize = 14;
            k.style.color = Palette.Mist2;
            var v = new UiLabel(value);
            v.style.fontSize = 14;
            v.style.color = valueColor ?? Palette.Mist1;
            row.Add(k);
            row.Add(v);
            parent.Add(row);
        }

        static UiLabel AddParagraph(VisualElement parent, string text, Color? col = null)
        {
            var l = new UiLabel((text ?? "").ToUpperInvariant());
            l.style.fontSize = 14;
            l.style.color = col ?? Palette.Mist2;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginBottom = 10;
            parent.Add(l);
            return l;
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

        void BuildRacePage()
        {
            AddColumnHeader(middleBody, "Choose a Race", $"{RaceIds.Length} RACES");
            for (int i = 0; i < RaceIds.Length; i++)
            {
                var r = Database.Races[RaceIds[i]];
                AddCard(middleBody, r.name, BonusText(r), FirstClause(r.description).ToUpperInvariant(), r.id == Session.Race.id, () => SelectRaceById(r.id));
            }

            var race = Session.Race;
            AddColumnHeader(rightBody, race.name, BonusText(race));
            AddParagraph(rightBody, race.description);

            var bonusTitle = new UiLabel("RACIAL ABILITY BONUS");
            bonusTitle.style.fontSize = 14;
            bonusTitle.style.color = Palette.Gold2;
            bonusTitle.style.marginBottom = 6;
            rightBody.Add(bonusTitle);

            var bonusRow = new VisualElement();
            bonusRow.style.flexDirection = FlexDirection.Row;
            bonusRow.style.alignItems = UnityEngine.UIElements.Align.Center;
            bonusRow.style.marginBottom = 14;
            foreach (var a in AbilityScores.AllAbilities.Where(a => race.abilityModifiers[a] != 0))
            {
                var tag = new UiLabel($"+{race.abilityModifiers[a]} {a}");
                tag.AddToClassList("uitk-badge-gold");
                tag.style.marginRight = 6;
                bonusRow.Add(tag);
            }
            var afterLbl = new UiLabel("APPLIED AFTER POINT BUY");
            afterLbl.style.fontSize = 13;
            afterLbl.style.color = Palette.Mist3;
            bonusRow.Add(afterLbl);
            rightBody.Add(bonusRow);

            var feat = race.racialFeat;
            var featHeader = new UiLabel($"RACIAL FEAT · {feat?.name ?? "None"}");
            featHeader.style.fontSize = 18;
            featHeader.style.color = Palette.Gold1;
            featHeader.style.marginBottom = 6;
            rightBody.Add(featHeader);
            AddParagraph(rightBody, feat?.description ?? "");

            var util = race.racialUtility;
            var utilHeader = new UiLabel($"RACIAL UTILITY · {util?.name ?? "None"}" + (string.IsNullOrEmpty(util?.utilityTag) ? "" : $" [{util.utilityTag}]"));
            utilHeader.style.fontSize = 18;
            utilHeader.style.color = Palette.Gold1;
            utilHeader.style.marginTop = 8;
            utilHeader.style.marginBottom = 6;
            rightBody.Add(utilHeader);
            AddParagraph(rightBody, util?.description ?? "");
        }

        void BuildClassPage()
        {
            AddColumnHeader(middleBody, "Choose a Class", $"{ClassIds.Length} CLASSES");
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;
            middleBody.Add(grid);

            for (int i = 0; i < ClassIds.Length; i++)
            {
                var c = Database.Classes[ClassIds[i]];
                var card = new VisualElement();
                card.AddToClassList("uitk-card");
                card.style.width = 130;
                card.style.marginBottom = 6;
                var titleLbl = new UiLabel(c.name);
                titleLbl.AddToClassList("uitk-card-title");
                titleLbl.style.fontSize = 15;
                var roleLbl = new UiLabel((c.role ?? "").ToUpperInvariant());
                roleLbl.AddToClassList("uitk-card-sub");
                roleLbl.style.fontSize = 11;
                card.Add(titleLbl);
                card.Add(roleLbl);
                grid.Add(card);
                var item = menu.Add(card, c.name, () => SelectClassById(c.id), selected: c.id == Session.Class.id);
                focusActions[item] = () => SelectClassById(c.id);
            }

            var cls = Session.Class;
            var guide = Session.GetStatGuide();
            AddColumnHeader(rightBody, cls.name, (cls.role ?? "").ToUpperInvariant());
            AddParagraph(rightBody, cls.description);

            var keySection = new VisualElement { name = "ClassKeyAbilitiesSection", style = { flexDirection = FlexDirection.Column, marginBottom = 8 } };
            keySection.Add(new UiLabel("KEY ABILITIES FOR THIS CLASS")
            {
                style = { fontSize = 14, color = Palette.Gold2, marginBottom = 5 }
            });

            var keyBadgeRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 5 }
            };
            foreach (var ab in guide.primaryAbilities ?? Array.Empty<Ability>())
            {
                var badge = MakePriorityBadge($"★ PRIMARY · {ab}", AbilityPriority.Primary, large: true);
                badge.name = $"ClassKeyBadge_{ab}";
                badge.style.marginRight = 6;
                badge.style.marginBottom = 4;
                keyBadgeRow.Add(badge);
            }
            foreach (var ab in guide.secondaryAbilities ?? Array.Empty<Ability>())
            {
                var badge = MakePriorityBadge($"◆ SECONDARY · {ab}", AbilityPriority.Secondary, large: true);
                badge.name = $"ClassKeyBadge_{ab}";
                badge.style.marginRight = 6;
                badge.style.marginBottom = 4;
                keyBadgeRow.Add(badge);
            }
            keySection.Add(keyBadgeRow);

            if (!string.IsNullOrEmpty(guide.summary))
            {
                keySection.Add(new UiLabel(guide.summary.ToUpperInvariant())
                {
                    style = { fontSize = 12, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal }
                });
            }
            rightBody.Add(keySection);

            var dsHead = new UiLabel("DERIVED STATS WITH THIS CLASS");
            dsHead.style.fontSize = 14;
            dsHead.style.color = Palette.Gold2;
            dsHead.style.marginBottom = 6;
            rightBody.Add(dsHead);

            var preview = Session.PreviewDerivedStats;
            foreach (var st in new[] { DerivedStatId.MaxHP, DerivedStatId.MaxMP, DerivedStatId.PhysicalPower, DerivedStatId.MagicPower, DerivedStatId.PhysicalDefense })
                AddStatRow(rightBody, Database.DerivedStatDefs[st].shortName, preview.FormatValue(st));

            var skillHead = new UiLabel("STARTER LOADOUT · PASSIVE IN STEP 6");
            skillHead.style.fontSize = 14;
            skillHead.style.color = Palette.Gold2;
            skillHead.style.marginTop = 10;
            skillHead.style.marginBottom = 6;
            rightBody.Add(skillHead);

            var skillWrap = new VisualElement();
            skillWrap.style.flexDirection = FlexDirection.Row;
            skillWrap.style.flexWrap = Wrap.Wrap;
            foreach (var id in cls.startingSkills ?? Array.Empty<string>())
            {
                if (!Database.Skills.TryGetValue(id, out var sk)) continue;
                var chip = new UiLabel(sk.name.ToUpperInvariant());
                chip.AddToClassList("uitk-badge-gold");
                chip.style.marginRight = 6;
                chip.style.marginBottom = 6;
                skillWrap.Add(chip);
            }
            rightBody.Add(skillWrap);
        }

        static VisualElement MakePriorityBadge(string text, AbilityPriority priority, bool large = false)
        {
            bool isPri = priority == AbilityPriority.Primary;
            Color borderCol = isPri ? Palette.Gold2 : Palette.Mp;
            Color textCol = isPri ? Palette.Gold1 : Palette.Shield;
            Color bgCol = isPri ? Palette.Hex(0x2B2210) : Palette.Hex(0x142238);

            var badge = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    paddingLeft = large ? 7 : 5,
                    paddingRight = large ? 7 : 5,
                    paddingTop = large ? 2 : 1,
                    paddingBottom = large ? 2 : 1,
                    backgroundColor = bgCol,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = borderCol, borderBottomColor = borderCol,
                    borderLeftColor = borderCol, borderRightColor = borderCol,
                    flexShrink = 0,
                    justifyContent = Justify.Center,
                    alignItems = UnityEngine.UIElements.Align.Center
                }
            };
            badge.Add(new UiLabel(text)
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    fontSize = large ? 11 : 10,
                    letterSpacing = 0.4f,
                    color = textCol,
                    whiteSpace = WhiteSpace.NoWrap
                }
            });
            return badge;
        }

        static string AbilityFullName(Ability a) => a switch
        {
            Ability.STR => "STR · STRENGTH",
            Ability.DEX => "DEX · DEXTERITY",
            Ability.VIT => "VIT · VITALITY",
            Ability.INT => "INT · INTELLIGENCE",
            Ability.WIS => "WIS · WISDOM",
            _ => "CHA · CHARISMA"
        };

        static string AbilitySubtitle(Ability a) => a switch
        {
            Ability.STR => "PHYSICAL POWER · BREAK",
            Ability.DEX => "INITIATIVE · EVASION · RANGED",
            Ability.VIT => "MAX HP · PHYSICAL DEFENSE",
            Ability.INT => "MAGIC POWER · MAX MP",
            Ability.WIS => "MAGIC DEFENSE · HEALING",
            _ => "SHOP PRICES · SUPPORT"
        };

        static DerivedStatId[] PreviewStatsFor(Ability a) => a switch
        {
            Ability.STR => new[] { DerivedStatId.PhysicalPower, DerivedStatId.PhysicalBreakPower, DerivedStatId.MaxHP },
            Ability.DEX => new[] { DerivedStatId.Initiative, DerivedStatId.Evasion, DerivedStatId.RangedPower },
            Ability.VIT => new[] { DerivedStatId.MaxHP, DerivedStatId.PhysicalDefense, DerivedStatId.StatusResistance },
            Ability.INT => new[] { DerivedStatId.MagicPower, DerivedStatId.MaxMP, DerivedStatId.MagicBreakPower },
            Ability.WIS => new[] { DerivedStatId.MagicDefense, DerivedStatId.HealingPower, DerivedStatId.MaxMP },
            _ => new[] { DerivedStatId.SupportPower, DerivedStatId.CriticalChance, DerivedStatId.StatusResistance }
        };

        void BuildAbilitiesPage()
        {
            var pb = Session.PointBuy;
            var guide = Session.GetStatGuide();
            string clsUpper = (Session.Class?.name ?? "CLASS").ToUpperInvariant();
            int rem = pb.RemainingPoints;
            int budget = pb.Config.budget;
            int minScore = pb.Config.minScore;
            int maxScore = pb.Config.maxScoreBeforeRacial;

            // Left panel: COST PER SCORE 2x4 table (02b design) + Class Key Stats summary
            costTableHost.Add(new UiLabel("COST PER SCORE")
            {
                style = { fontSize = 12, letterSpacing = 0.48f, color = Palette.Mist3, marginBottom = 6 }
            });
            var costGrid = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, width = 224, marginBottom = 6 }
            };
            for (int sc = minScore; sc <= maxScore; sc++)
            {
                var cell = new VisualElement
                {
                    style =
                    {
                        width = 50, height = 28, marginRight = 6, marginBottom = 6,
                        paddingLeft = 5, paddingRight = 5,
                        flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center,
                        backgroundColor = Palette.Ink1,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        borderTopColor = sc >= 14 ? Palette.Gold4 : Palette.Ink4,
                        borderBottomColor = sc >= 14 ? Palette.Gold4 : Palette.Ink4,
                        borderLeftColor = sc >= 14 ? Palette.Gold4 : Palette.Ink4,
                        borderRightColor = sc >= 14 ? Palette.Gold4 : Palette.Ink4
                    }
                };
                cell.Add(new UiLabel(sc.ToString()) { style = { fontSize = 13, color = Palette.Mist1 } });
                cell.Add(new UiLabel(pb.Config.GetCost(sc).ToString()) { style = { fontSize = 11, color = sc >= 14 ? Palette.Gold2 : Palette.Mist3 } });
                costGrid.Add(cell);
            }
            costTableHost.Add(costGrid);
            costTableHost.Add(new UiLabel("14 AND 15 COST MORE: SPREAD POINTS OR SPECIALISE.")
            {
                style = { fontSize = 11, letterSpacing = 0.48f, color = Palette.Mist3, whiteSpace = WhiteSpace.Normal, marginBottom = 8 }
            });

            var keyStatsBlock = new VisualElement { name = "AbilitiesClassKeyStats", style = { flexDirection = FlexDirection.Column } };
            keyStatsBlock.Add(new UiLabel($"{clsUpper} KEY STATS")
            {
                style = { fontSize = 11, letterSpacing = 0.48f, color = Palette.Gold2, marginBottom = 4 }
            });
            var keyChips = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            foreach (var ab in guide.primaryAbilities ?? Array.Empty<Ability>())
            {
                var chip = MakePriorityBadge($"★ {ab}", AbilityPriority.Primary);
                chip.style.marginRight = 5;
                chip.style.marginBottom = 4;
                keyChips.Add(chip);
            }
            foreach (var ab in guide.secondaryAbilities ?? Array.Empty<Ability>())
            {
                var chip = MakePriorityBadge($"◆ {ab}", AbilityPriority.Secondary);
                chip.style.marginRight = 5;
                chip.style.marginBottom = 4;
                keyChips.Add(chip);
            }
            keyStatsBlock.Add(keyChips);
            costTableHost.Add(keyStatsBlock);

            // Top Header inside Gold Panel
            var hdr = FigmaUi.CreateSectionHeader("Ability Scores · Point Buy", 560f);
            if (hdr != null)
            {
                hdr.style.position = Position.Absolute;
                hdr.style.left = 16;
                hdr.style.top = 14;
                hdr.style.height = 36;
                abilitiesHost.Add(hdr);
            }

            // POINTS LEFT meter (02b top-right)
            var meter = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 610, top = 8, width = 330, height = 52,
                    paddingLeft = 10, paddingRight = 10, paddingTop = 4, paddingBottom = 6,
                    backgroundColor = Palette.Hex(0x1D1608),
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = rem > 0 ? Palette.Gold2 : Palette.Ok,
                    borderBottomColor = rem > 0 ? Palette.Gold2 : Palette.Ok,
                    borderLeftColor = rem > 0 ? Palette.Gold2 : Palette.Ok,
                    borderRightColor = rem > 0 ? Palette.Gold2 : Palette.Ok,
                    flexDirection = FlexDirection.Column, justifyContent = Justify.SpaceBetween
                }
            };
            var meterTop = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center } };
            meterTop.Add(new UiLabel("POINTS LEFT") { style = { fontSize = 13, letterSpacing = 0.56f, color = Palette.Gold2 } });
            meterTop.Add(new UiLabel($"{rem} / {budget}") { style = { fontSize = 22, color = rem > 0 ? Palette.Gold1 : Palette.Ok } });
            meter.Add(meterTop);

            var meterTrack = new VisualElement
            {
                style =
                {
                    width = 306, height = 8, paddingTop = 1, paddingBottom = 1, paddingLeft = 1, paddingRight = 1,
                    backgroundColor = Palette.Ink0,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4
                }
            };
            float remRatio = budget > 0 ? Mathf.Clamp01((float)rem / budget) : 0f;
            meterTrack.Add(new VisualElement { style = { width = Length.Percent(remRatio * 100f), height = Length.Percent(100), backgroundColor = rem > 0 ? Palette.Gold2 : Palette.Ok } });
            meter.Add(meterTrack);
            abilitiesHost.Add(meter);

            // Table column headers (left = 16, top = 74)
            void AddColHdr(string text, float leftPx)
            {
                abilitiesHost.Add(new UiLabel(text)
                {
                    style = { position = Position.Absolute, left = leftPx, top = 74, fontSize = 12, letterSpacing = 0.48f, color = Palette.Mist3 }
                });
            }
            AddColHdr("ABILITY", 16);
            AddColHdr("BASE SCORE", 248);
            AddColHdr("COST", 388);
            AddColHdr("RACIAL", 444);
            AddColHdr("FINAL", 518);
            AddColHdr("MOD", 574);

            abilitiesHost.Add(new VisualElement
            {
                style = { position = Position.Absolute, left = 16, top = 92, width = 588, height = 2, backgroundColor = Palette.Ink4 }
            });

            // Vertical separator & Right sub-column (NEXT POINT PREVIEW)
            abilitiesHost.Add(new VisualElement
            {
                style = { position = Position.Absolute, left = 620, top = 74, width = 2, height = 434, backgroundColor = Palette.Ink4 }
            });

            nextPreviewHost = new VisualElement
            {
                name = "NextPointPreviewHost",
                style = { position = Position.Absolute, left = 636, top = 74, width = 304, height = 390, flexDirection = FlexDirection.Column }
            };
            abilitiesHost.Add(nextPreviewHost);

            var bases = Session.BaseScores; var race = Session.RacialModifiers; var fin = Session.FinalScores; var mods = Session.AbilityModifiers;
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var ab = AbilityScores.AllAbilities[i];
                var priority = guide.GetPriority(ab);
                int baseVal = bases[ab];
                int rBonus = race[ab];
                int finVal = fin[ab];
                int modVal = mods[ab];
                int costVal = pb.GetCost(ab);
                int incCost = pb.GetIncreaseCost(ab);
                bool atMin = baseVal <= minScore;
                bool atMax = baseVal >= maxScore;
                bool canAfford = !atMax && incCost > 0 && incCost <= rem;
                float rowTop = 98 + i * 62;

                var row = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = 8, top = rowTop, width = 604, height = 58,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        borderTopColor = Color.clear, borderBottomColor = Color.clear, borderLeftColor = Color.clear, borderRightColor = Color.clear,
                        backgroundColor = Color.clear,
                        overflow = Overflow.Hidden
                    }
                };

                if (priority != AbilityPriority.Optional)
                {
                    row.Add(new VisualElement
                    {
                        pickingMode = PickingMode.Ignore,
                        style =
                        {
                            position = Position.Absolute, left = 0, top = 4, bottom = 4, width = 3,
                            backgroundColor = priority == AbilityPriority.Primary ? Palette.Gold2 : Palette.Mp
                        }
                    });
                }

                var cursorLbl = new UiLabel("▸")
                {
                    pickingMode = PickingMode.Ignore,
                    style = { position = Position.Absolute, left = 5, top = 16, fontSize = 18, color = Palette.Gold1, visibility = Visibility.Hidden }
                };
                row.Add(cursorLbl);

                var titleRow = new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute, left = 20, top = 5, width = 208,
                        flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center
                    }
                };
                Color defaultTitleCol = priority == AbilityPriority.Primary ? Palette.Gold1 : Palette.Mist1;
                var abTitle = new UiLabel(AbilityFullName(ab))
                {
                    pickingMode = PickingMode.Ignore,
                    style = { fontSize = 15, color = defaultTitleCol, flexShrink = 1, overflow = Overflow.Hidden }
                };
                titleRow.Add(abTitle);

                if (priority != AbilityPriority.Optional)
                {
                    var pBadge = MakePriorityBadge(priority == AbilityPriority.Primary ? "★ PRI" : "◆ SEC", priority);
                    pBadge.name = $"AbilityPriorityBadge_{ab}";
                    pBadge.style.marginLeft = 4;
                    titleRow.Add(pBadge);
                }
                row.Add(titleRow);

                row.Add(new UiLabel(AbilitySubtitle(ab))
                {
                    pickingMode = PickingMode.Ignore,
                    style = { position = Position.Absolute, left = 20, top = 30, width = 208, fontSize = 11, letterSpacing = 0.42f, color = Palette.Mist3 }
                });

                string stepperTpl = atMax ? "Stat-Stepper-At-Max"
                    : atMin ? "Stat-Stepper-At-Min"
                    : !canAfford ? "Stat-Stepper-No-Points"
                    : "Stat-Stepper";
                var stepper = FigmaUi.InstantiateTemplate(stepperTpl) ?? new VisualElement();
                stepper.style.position = Position.Absolute;
                stepper.style.left = 232;
                stepper.style.top = 7;
                FigmaUi.SetInstanceText(stepper, "text-0", "−");
                FigmaUi.SetInstanceText(stepper, "text-1", baseVal.ToString());
                FigmaUi.SetInstanceText(stepper, "text-2", "+");

                var minusEl = stepper.Q<UiLabel>("text-0")?.parent;
                if (minusEl != null)
                {
                    minusEl.pickingMode = PickingMode.Position;
                    minusEl.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        AdjustAbility(ab, -1);
                    });
                }
                var plusEl = stepper.Q<UiLabel>("text-2")?.parent;
                if (plusEl != null)
                {
                    plusEl.pickingMode = PickingMode.Position;
                    plusEl.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        AdjustAbility(ab, 1);
                    });
                }
                row.Add(stepper);

                row.Add(new UiLabel(costVal.ToString())
                {
                    pickingMode = PickingMode.Ignore,
                    style = { position = Position.Absolute, left = 388, top = 16, width = 32, fontSize = 14, color = Palette.Mist2, unityTextAlign = TextAnchor.MiddleCenter }
                });

                if (rBonus != 0)
                {
                    var rTag = FigmaUi.InstantiateTemplate("Tag-Gold");
                    if (rTag != null)
                    {
                        rTag.pickingMode = PickingMode.Ignore;
                        rTag.style.position = Position.Absolute;
                        rTag.style.left = 438;
                        rTag.style.top = 13;
                        FigmaUi.SetInstanceText(rTag, "text-0", rBonus > 0 ? $"+{rBonus}" : rBonus.ToString());
                        row.Add(rTag);
                    }
                }
                else
                {
                    row.Add(new UiLabel("—")
                    {
                        pickingMode = PickingMode.Ignore,
                        style = { position = Position.Absolute, left = 450, top = 16, fontSize = 14, color = Palette.Mist3 }
                    });
                }

                row.Add(new UiLabel(finVal.ToString())
                {
                    pickingMode = PickingMode.Ignore,
                    style = { position = Position.Absolute, left = 510, top = 11, width = 40, fontSize = 22, color = rBonus > 0 ? Palette.Gold1 : Palette.Mist1, unityTextAlign = TextAnchor.MiddleCenter }
                });

                row.Add(new UiLabel(AbilityScores.FormatMod(modVal))
                {
                    pickingMode = PickingMode.Ignore,
                    style = { position = Position.Absolute, left = 566, top = 16, width = 32, fontSize = 14, color = modVal > 0 ? Palette.Ok : modVal < 0 ? Palette.Err : Palette.Mist2, unityTextAlign = TextAnchor.MiddleCenter }
                });

                abilitiesHost.Add(row);

                // Row separator
                abilitiesHost.Add(new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style = { position = Position.Absolute, left = 16, top = rowTop + 58, width = 588, height = 2, backgroundColor = Palette.Ink3 }
                });

                var item = menu.Add(row, ab.ToString(), () => AdjustAbility(ab, 1), onAdjust: d => AdjustAbility(ab, d),
                    tooltip: $"{AbilityFullName(ab)}: LEFT/RIGHT OR CLICK -/+ TO ADJUST");
                item.visual = on =>
                {
                    row.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Color.clear;
                    var b = on ? Palette.Gold2 : Color.clear;
                    row.style.borderTopColor = b; row.style.borderBottomColor = b;
                    row.style.borderLeftColor = b; row.style.borderRightColor = b;
                    cursorLbl.style.visibility = on ? Visibility.Visible : Visibility.Hidden;
                    abTitle.style.color = on ? Palette.Gold1 : defaultTitleCol;
                    if (on)
                    {
                        focusedAbility = ab;
                        UpdateNextPointPreview();
                    }
                };
                item.visual(false);
            }

            // Bottom-left warning note inside Gold Panel
            abilitiesHost.Add(new UiLabel(rem > 0 ? "REMAINING POINTS ARE LOST IF UNSPENT." : "ALL POINTS ALLOCATED · READY TO CONTINUE.")
            {
                style =
                {
                    position = Position.Absolute, left = 16, top = 524,
                    fontSize = 12, letterSpacing = 0.48f, color = rem > 0 ? Palette.Ember1 : Palette.Ok
                }
            });

            // Bottom-right controls inside Gold Panel (Mode + Reset + Use Recommended)
            var modeBtn = FigmaUi.CreateButton(MethodText(), "button-slab", 304f);
            modeBtn.style.position = Position.Absolute;
            modeBtn.style.left = 636;
            modeBtn.style.top = 468;
            modeBtn.style.height = 36;
            abilitiesHost.Add(modeBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(modeBtn, "MODE", OnActivateCreationMethodButton, onAdjust: CycleCreationMethodOrTemplate,
                tooltip: "L/R: MANUAL, TEMPLATE OR RANDOM. ENTER: REROLL"), modeBtn);

            var resetBtn = FigmaUi.CreateButton("Reset", "button-slab", 108f);
            resetBtn.style.position = Position.Absolute;
            resetBtn.style.left = 636;
            resetBtn.style.top = 514;
            resetBtn.style.height = 38;
            abilitiesHost.Add(resetBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(resetBtn, "Reset", OnResetOrEditClicked, tooltip: "REFUND ALL ABILITY POINTS TO BASE 8"), resetBtn);

            var recBtn = FigmaUi.CreateButton("Use Recommended", "button-slab", 188f);
            recBtn.style.position = Position.Absolute;
            recBtn.style.left = 752;
            recBtn.style.top = 514;
            recBtn.style.height = 38;
            var recLbl = recBtn.Q<UiLabel>("uitkLabel");
            if (recLbl != null) recLbl.style.fontSize = 15;
            abilitiesHost.Add(recBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(recBtn, "Use Recommended", ApplyRecommendedPointBuy,
                tooltip: $"APPLY RECOMMENDED {clsUpper} SPREAD ({guide.FormatSpreadSummary()})"), recBtn);

            UpdateNextPointPreview();
        }

        void ApplyRecommendedPointBuy()
        {
            EnsureEditingMode();
            Session.ApplyRecommendedPointBuy(out var msg);
            SetHint(msg);
            Sfx.Play(Sound.Confirm);
            Refresh();
        }

        void UpdateNextPointPreview()
        {
            if (nextPreviewHost == null) return;
            nextPreviewHost.Clear();

            var pb = Session.PointBuy;
            var guide = Session.GetStatGuide();
            var priority = guide.GetPriority(focusedAbility);
            string clsName = Session.Class?.name ?? "Class";
            string clsUpper = clsName.ToUpperInvariant();
            int rem = pb.RemainingPoints;
            int cur = Session.BaseScores[focusedAbility];
            int max = pb.Config.maxScoreBeforeRacial;
            int nextCost = pb.GetIncreaseCost(focusedAbility);
            bool atMax = cur >= max;
            bool canAfford = !atMax && nextCost > 0 && nextCost <= rem;

            // Class-aware stat guidance box for the focused ability
            Color guideBorder = priority == AbilityPriority.Primary ? Palette.Gold2
                : priority == AbilityPriority.Secondary ? Palette.Mp : Palette.Ink4;
            Color guideTitleCol = priority == AbilityPriority.Primary ? Palette.Gold1
                : priority == AbilityPriority.Secondary ? Palette.Shield : Palette.Mist3;
            string guideHeading = priority == AbilityPriority.Primary ? $"★ PRIMARY FOR {clsUpper}"
                : priority == AbilityPriority.Secondary ? $"◆ SECONDARY FOR {clsUpper}"
                : $"OPTIONAL FOR {clsUpper}";

            var guideBox = new VisualElement
            {
                name = "ClassStatGuideBox",
                style =
                {
                    paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    marginBottom = 10,
                    backgroundColor = Palette.Ink1,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = guideBorder, borderBottomColor = guideBorder,
                    borderLeftColor = guideBorder, borderRightColor = guideBorder,
                    flexDirection = FlexDirection.Column
                }
            };
            guideBox.Add(new UiLabel(guideHeading)
            {
                name = "ClassStatGuideHeading",
                style = { fontSize = 12, letterSpacing = 0.48f, color = guideTitleCol, marginBottom = 3 }
            });
            guideBox.Add(new UiLabel(guide.GetReason(focusedAbility, clsName).ToUpperInvariant())
            {
                name = "ClassStatGuideBody",
                style = { fontSize = 11, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal }
            });
            nextPreviewHost.Add(guideBox);

            nextPreviewHost.Add(new UiLabel("NEXT POINT PREVIEW")
            {
                style = { fontSize = 13, letterSpacing = 0.56f, color = Palette.Gold2, marginBottom = 4 }
            });

            string subLine = atMax
                ? $"{focusedAbility} {cur}  ·  AT BASE CAP ({max})"
                : $"{focusedAbility} {cur} › {cur + 1}  ·  COSTS {nextCost} {(nextCost == 1 ? "POINT" : "POINTS")}";
            nextPreviewHost.Add(new UiLabel(subLine)
            {
                style = { fontSize = 13, letterSpacing = 0.48f, color = atMax ? Palette.Mist3 : canAfford ? Palette.Mist1 : Palette.Ember1, marginBottom = 8 }
            });

            var nowDerived = Session.PreviewDerivedStats;
            var nextDerived = atMax ? nowDerived : Session.PreviewDerivedStatsWithDelta(focusedAbility, 1);
            int curMod = Session.AbilityModifiers[focusedAbility];
            int nextMod = atMax ? curMod : AbilityScores.GetModifier(Session.FinalScores[focusedAbility] + 1);

            void AddDeltaRow(string label, string before, string after, bool changed)
            {
                var r = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween,
                        paddingTop = 4, paddingBottom = 5, marginBottom = 4,
                        borderBottomWidth = 2, borderBottomColor = Palette.Ink3
                    }
                };
                r.Add(new UiLabel(label) { style = { fontSize = 13, color = Palette.Mist2 } });
                r.Add(new UiLabel($"{before} › {after}") { style = { fontSize = 13, color = changed ? Palette.Ok : Palette.Mist3 } });
                nextPreviewHost.Add(r);
            }

            foreach (var st in PreviewStatsFor(focusedAbility))
            {
                string bStr = nowDerived.FormatValue(st);
                string aStr = nextDerived.FormatValue(st);
                AddDeltaRow(Database.DerivedStatDefs[st].displayName.ToUpperInvariant(), bStr, aStr, bStr != aStr);
            }
            AddDeltaRow($"{focusedAbility} MODIFIER", AbilityScores.FormatMod(curMod), AbilityScores.FormatMod(nextMod), curMod != nextMod);

            string toastMsg = null;
            if (!atMax && !canAfford)
                toastMsg = $"{focusedAbility} {cur} › {cur + 1} NEEDS {nextCost} POINTS. YOU HAVE {rem}. FREE ONE BY LOWERING ANOTHER SCORE.";
            else if (atMax)
                toastMsg = $"{focusedAbility} IS AT THE BASE CAP OF {max} BEFORE RACIAL BONUS.";
            else if (rem == 0)
                toastMsg = "ALL 27 POINTS SPENT. YOU CAN PROCEED TO BACKGROUND OR ADJUST SCORES.";

            if (toastMsg != null)
            {
                var toast = FigmaUi.InstantiateTemplate("Toast-Info");
                if (toast != null)
                {
                    toast.style.marginTop = 10;
                    toast.style.width = 300;
                    FigmaUi.SetInstanceText(toast, "text-0", "!");
                    FigmaUi.SetInstanceText(toast, "text-1", toastMsg);
                    nextPreviewHost.Add(toast);
                }
            }
        }

        void BuildBackgroundPage()
        {
            AddColumnHeader(middleBody, "Choose a Background", $"{BackgroundIds.Length} TOTAL");
            for (int i = 0; i < BackgroundIds.Length; i++)
            {
                var b = Database.Backgrounds[BackgroundIds[i]];
                var item = AddCard(middleBody, b.name, null, (b.narrativeIdentity ?? "").ToUpperInvariant(), b.id == Session.Background.id, () => SelectBackgroundById(b.id));
                if (item?.element != null)
                {
                    item.element.style.height = 42;
                    item.element.style.paddingTop = 2;
                    item.element.style.paddingBottom = 2;
                    item.element.style.marginBottom = 3;
                    item.element.style.justifyContent = Justify.Center;
                    item.element.style.overflow = Overflow.Hidden;
                    var tLbl = item.element.Q<UiLabel>(className: "uitk-card-title");
                    if (tLbl != null) tLbl.style.fontSize = 13;
                    var sLbl = item.element.Q<UiLabel>(className: "uitk-card-sub");
                    if (sLbl != null)
                    {
                        sLbl.style.fontSize = 10;
                        sLbl.style.marginTop = 0;
                        sLbl.style.whiteSpace = WhiteSpace.NoWrap;
                        sLbl.style.overflow = Overflow.Hidden;
                    }
                }
            }

            var bg = Session.Background;
            AddColumnHeader(rightBody, $"{bg.name} · {bg.perkName}", "DRAFT VALUES");
            AddParagraph(rightBody, bg.description);

            var perkHead = new UiLabel("PERK RANKS · UNLOCK BY LEVEL");
            perkHead.style.fontSize = 14;
            perkHead.style.color = Palette.Gold2;
            perkHead.style.marginBottom = 6;
            rightBody.Add(perkHead);

            string[] numerals = { "I", "II", "III", "IV", "V" };
            for (int r = 1; r <= 5; r++)
            {
                var rank = bg.ranks != null && bg.ranks.Length >= r ? bg.ranks[r - 1] : null;
                var box = new VisualElement();
                box.AddToClassList("uitk-card");
                if (r == 1) box.AddToClassList("uitk-selected");
                var rowTitle = new UiLabel($"{numerals[r - 1]} · LV {rank?.requiredLevel ?? (r * 3 - 2)} · {(rank?.perkName ?? "").ToUpperInvariant()}");
                rowTitle.style.fontSize = 14;
                rowTitle.style.color = r == 1 ? Palette.Gold1 : Palette.Mist2;
                var desc = new UiLabel((rank?.perkDescription ?? "").ToUpperInvariant());
                desc.style.fontSize = 12;
                desc.style.color = Palette.Mist3;
                desc.style.whiteSpace = WhiteSpace.Normal;
                box.Add(rowTitle);
                box.Add(desc);
                rightBody.Add(box);
            }
        }

        void BuildPassivePage()
        {
            AddColumnHeader(middleBody, "Starter Passive", "FROM CLASS");
            var sp = Session.StarterPassive;
            AddCard(middleBody, sp?.name ?? "None", null, (sp?.description ?? "").ToUpperInvariant(), true, null);

            var also = new UiLabel("ALSO ACTIVE AT LEVEL 1");
            also.style.fontSize = 13;
            also.style.color = Palette.Mist3;
            also.style.marginTop = 8;
            also.style.marginBottom = 6;
            middleBody.Add(also);

            var feat = Session.Race?.racialFeat;
            AddCard(middleBody, "Racial Feat · " + (feat?.name ?? "None"), null, (feat?.description ?? "").ToUpperInvariant(), false, null);

            var bgRank = Session.Background?.GetRank(1);
            AddCard(middleBody, "Perk · " + (bgRank?.perkName ?? Session.Background?.perkName ?? ""), null, (bgRank?.perkDescription ?? "").ToUpperInvariant(), false, null);

            AddColumnHeader(rightBody, sp?.name ?? "Passive");
            AddParagraph(rightBody, sp?.description ?? "No starter passive.");
            AddParagraph(rightBody, "STARTER PASSIVES ARE ALWAYS ON AND DO NOT USE A SKILL SLOT.", Palette.Mist3);
        }

        void BuildGearPage()
        {
            AddColumnHeader(middleBody, "Starting Gear", "8 SLOTS");
            foreach (var slot in EquipmentSlots.All)
            {
                var s = slot;
                var eq = Session.GetEquipped(s);
                AddCard(middleBody, EquipmentSlots.DisplayName(s), null, eq != null ? eq.name.ToUpperInvariant() : "EMPTY",
                    s == SelectedEquipmentSlot, () => SelectSlot(s), d => { SelectedEquipmentSlot = s; CycleEquipmentItem(d); });
            }

            var sel = Session.GetEquipped(SelectedEquipmentSlot);
            AddColumnHeader(rightBody, EquipmentSlots.DisplayName(SelectedEquipmentSlot));
            var itemTitle = new UiLabel(sel != null ? sel.name : "Empty");
            itemTitle.style.fontSize = 22;
            itemTitle.style.color = sel != null ? Palette.Gold1 : Palette.Mist3;
            itemTitle.style.marginBottom = 8;
            rightBody.Add(itemTitle);

            AddParagraph(rightBody, sel?.description ?? "NOTHING EQUIPPED IN THIS SLOT.");
            int count = Session.GetCompatibleEquipmentForSlot(SelectedEquipmentSlot).Count();
            AddParagraph(rightBody, $"{count} COMPATIBLE ITEMS · LEFT/RIGHT TO CHANGE. TWO-HANDED WEAPONS USE MAIN HAND ONLY.", Palette.Mist3);
        }

        void BuildReviewPage()
        {
            bool inReview = InReview;
            AddColumnHeader(middleBody, "Party", $"{Builder.PendingParty.Count} / {CharacterRoster.PartySize}");

            for (int i = 0; i < CharacterRoster.PartySize; i++)
            {
                int idx = i;
                if (i < Builder.PendingParty.Count)
                {
                    var c = Builder.PendingParty[i];
                    string r = Database.Races.TryGetValue(c.raceId ?? "", out var rd) ? rd.name : c.raceId;
                    string cl = Database.Classes.TryGetValue(c.classId ?? "", out var cd) ? cd.name : c.classId;
                    AddCard(middleBody, c.name, null, $"{r} · {cl}".ToUpperInvariant(), inReview && i == Builder.SelectedReviewIndex,
                        inReview ? () => { if (Builder.SelectedReviewIndex != idx) { Builder.SelectReviewMember(idx); Session.LoadFromCharacterDef(Builder.PendingParty[idx]); Refresh(); } } : (Action)null);
                }
                else
                {
                    AddCard(middleBody, i == Builder.PendingParty.Count && !inReview ? "THIS HERO" : "EMPTY SLOT", null, "CONFIGURE AND ADD HERO", false, null);
                }
            }

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;
            btnRow.style.marginTop = 8;
            var editBtn = FigmaUi.CreateButton(inReview ? "Edit" : "Reset", "button-slab", 84f);
            var randBtn = FigmaUi.CreateButton(inReview ? "Reroll" : "Random", "button-slab", 88f);
            var randAllBtn = FigmaUi.CreateButton("Rand All", "button-slab", 92f);
            btnRow.Add(editBtn);
            btnRow.Add(randBtn);
            btnRow.Add(randAllBtn);
            middleBody.Add(btnRow);
            menu.Add(editBtn, inReview ? "Edit" : "Reset", OnResetOrEditClicked);
            menu.Add(randBtn, inReview ? "Reroll" : "Random", () => OnRandomOrRerollSelected());
            menu.Add(randAllBtn, "Rand All", () => RandomizeAll());

            AddColumnHeader(rightBody, HeroName);
            foreach (var (k, v) in new[]
            {
                ("RACE", Session.Race.name),
                ("CLASS", Session.Class.name),
                ("BACKGROUND", Session.Background.name),
                ("FEAT", Session.Race.racialFeat?.name ?? "-"),
                ("PASSIVE", Session.StarterPassive?.name ?? "-"),
                ("POINTS LEFT", Session.PointBuy.RemainingPoints.ToString()),
            })
                AddStatRow(rightBody, k, v.ToUpperInvariant());

            string err = inReview ? null : Session.Validate();
            var statusLbl = new UiLabel(err != null
                ? err.ToUpperInvariant()
                : (inReview ? "CONFIRM THE PARTY TO RECEIVE 600 HUB GOLD AND ENTER GEISTWALD CITY." : "READY. ADD THIS HERO TO THE PARTY."));
            statusLbl.style.fontSize = 15;
            statusLbl.style.color = err != null ? Palette.HpLow : Palette.Hp;
            statusLbl.style.whiteSpace = WhiteSpace.Normal;
            statusLbl.style.marginTop = 12;
            rightBody.Add(statusLbl);
        }

        string MethodText() =>
            Session.Method == CreationMethod.Template && !string.IsNullOrEmpty(Session.ActiveTemplateId) && Database.Templates.TryGetValue(Session.ActiveTemplateId, out var tpl)
                ? Selector("TEMPLATE", tpl.name) : Selector("MODE", Session.Method.ToString());

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

        void OnMenuFocus(UitkMenu.Item s)
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
            focusIndex = p switch
            {
                Page.Race => Math.Max(0, Array.IndexOf(RaceIds, Session.Race.id)),
                Page.Class => Math.Max(0, Array.IndexOf(ClassIds, Session.Class.id)),
                Page.Background => Math.Max(0, Array.IndexOf(BackgroundIds, Session.Background.id)),
                _ => 0,
            };
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

        VisualElement backgroundModalEl;
        bool backgroundModalOpen;
        public bool IsBackgroundModalOpen => backgroundModalOpen;

        public void ToggleBackgroundDetailsModal()
        {
            if (IsBackgroundModalOpen) CloseBackgroundDetailsModal();
            else OpenBackgroundDetailsModal();
        }

        public void OpenBackgroundDetailsModal()
        {
            backgroundModalOpen = true;
            RenderBackgroundModal();
        }

        public void CloseBackgroundDetailsModal()
        {
            backgroundModalOpen = false;
            FigmaUi.DestroyMenu(ref bgModalMenu);
            backgroundModalEl?.RemoveFromHierarchy();
            backgroundModalEl = null;
        }

        void RenderBackgroundModal()
        {
            if (UiRoot == null || Root == null) return;
            FigmaUi.DestroyMenu(ref bgModalMenu);
            backgroundModalEl?.RemoveFromHierarchy();
            var bg = Session.Background;
            if (bg == null) return;

            backgroundModalEl = new VisualElement { name = "BackgroundModal" };
            backgroundModalEl.AddToClassList("uitk-modal-backdrop");
            backgroundModalEl.pickingMode = PickingMode.Position;
            var panel = new VisualElement();
            panel.AddToClassList("uitk-modal-panel");
            var title = new UiLabel($"{bg.name} · {bg.perkName}");
            title.AddToClassList("uitk-modal-title");
            panel.Add(title);
            panel.Add(new UiLabel((bg.narrativeIdentity ?? "").ToUpperInvariant()) { style = { color = Palette.Mist2, marginBottom = 10, fontSize = 14 } });

            string[] numerals = { "I", "II", "III", "IV", "V" };
            for (int r = 1; r <= 5; r++)
            {
                var rDef = bg.GetRank(r == 1 ? 1 : r == 2 ? 4 : r == 3 ? 7 : r == 4 ? 10 : 13);
                var line = new UiLabel($"RANK {numerals[r - 1]} (LV {rDef?.requiredLevel ?? 1}) — {rDef?.perkName?.ToUpperInvariant()} — {rDef?.perkDescription?.ToUpperInvariant()}");
                line.style.fontSize = 13;
                line.style.whiteSpace = WhiteSpace.Normal;
                line.style.color = r == 1 ? Palette.Gold1 : Palette.Mist2;
                line.style.marginBottom = 6;
                panel.Add(line);
            }

            var closeBtn = FigmaUi.CreateButton("CLOSE", "button-slab", 160f);
            panel.Add(closeBtn);
            backgroundModalEl.Add(panel);
            UiRoot.Add(backgroundModalEl);

            bgModalMenu = Root.gameObject.AddComponent<UitkMenu>();
            bgModalMenu.AttachRoot(backgroundModalEl);
            bgModalMenu.onBack = CloseBackgroundDetailsModal;
            FigmaUi.ApplyDefaultFocusVisual(bgModalMenu.Add(closeBtn, "CLOSE", CloseBackgroundDetailsModal));
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
