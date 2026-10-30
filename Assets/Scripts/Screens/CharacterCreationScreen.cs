using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Full Character Creation & 4-Hero Party Assembly screen (Character Creation System Specification Sections 2-10).
    // Conforming to 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers.
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
        public Label StarterPassiveLabel { get; private set; }
        public Label EquipmentSlotLabel { get; private set; }
        public Label PartySummaryLabel { get; private set; }
        public Label HintLabel { get; private set; }

        public readonly Dictionary<Ability, Label> AbilityRowLabels = new Dictionary<Ability, Label>();
        public readonly Dictionary<DerivedStatId, Label> PreviewStatLabels = new Dictionary<DerivedStatId, Label>();
        readonly Dictionary<Ability, Label[]> abilityColumns = new Dictionary<Ability, Label[]>();
        Label classTitle;

        // Ability table columns (centers) inside the 320px rows at x=14: BASE, RACE, TOTAL, MOD, COST.
        static readonly int[] AbilityColumnX = { 95, 145, 195, 245, 290 };
        const int RightTextWidth = 112, HintWidth = 308;

        public EquipmentSlot SelectedEquipmentSlot { get; private set; } = EquipmentSlot.MainHand;

        Selectable resetOrEditBtn;
        Selectable randOrRerollBtn;
        Selectable confirmBtn;
        Selectable randAllBtn;
        Selectable startBtn;
        SpriteRenderer portrait;
        int templateIndex = -1;

        static readonly string[] RaceIds = Database.PlayableRaceIds;
        static readonly string[] ClassIds = Database.PlayableClassIds;
        static readonly string[] BackgroundIds = Database.Backgrounds.Keys.ToArray();
        static readonly string[] TemplateIds = Database.TemplateIds;

        static readonly DerivedStatId[] PreviewedStats =
        {
            DerivedStatId.MaxHP,
            DerivedStatId.MaxMP,
            DerivedStatId.PhysicalPower,
            DerivedStatId.RangedPower,
            DerivedStatId.MagicPower,
            DerivedStatId.HealingPower,
            DerivedStatId.SupportPower,
            DerivedStatId.PhysicalDefense,
            DerivedStatId.MagicDefense,
            DerivedStatId.Evasion,
            DerivedStatId.CriticalChance,
            DerivedStatId.Initiative,
            DerivedStatId.PhysicalBreakPower,
            DerivedStatId.StatusResistance,
            DerivedStatId.QteTimingBonus,
        };

        public CharacterCreationScreen() : this(null, null, null) { }

        public CharacterCreationScreen(CharacterCreationSession session = null, CharacterRoster roster = null, NewGamePartyBuilder builder = null)
        {
            if (builder != null)
                Builder = builder;
            else if (session != null)
                Builder = new NewGamePartyBuilder(session);
            else if (Game?.NewGameBuilder != null && Application.isPlaying)
                Builder = Game.NewGameBuilder;
            else
                Builder = new NewGamePartyBuilder();

            Roster = roster ?? (Game != null ? Game.Roster : new CharacterRoster(startWithEmptyParty: true));
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // 1. Top bar: Title, Current Step & Remaining Point Buy Budget
            Ui.Panel(Root, 0, 0, Ui.Width, 26, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Label(Root, "CHARACTER CREATION & RECRUITMENT", 14, 7, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            StepLabel = Ui.Label(Root, "", 320, 7, Palette.WarmIvory, Align.Center);
            RemainingPointsLabel = Ui.Label(Root, "", 626, 7, Palette.AntiqueGoldBright, Align.Right);

            // 2. Left panel (334x294): Creation Method, Race, Class, Equipment, and 6 Ability Rows
            Ui.Panel(Root, 10, 28, 336, 294, Palette.DeepCharcoal, Palette.DarkBorder);

            // 3. Right panel (274x294): Live Character, Starter Passive & Derived Stats Preview
            Ui.Panel(Root, 354, 28, 276, 294, Palette.DeepCharcoal, Palette.DarkBorder);

            // 4. Bottom panel (640x36): Validation Feedback / Party Bar (X/4) & Action buttons
            Ui.Panel(Root, 0, 324, Ui.Width, 36, Palette.DeepCharcoal, Palette.AntiqueGold);

            var menu = Root.gameObject.AddComponent<Menu>();
            HintLabel = Ui.Label(Root, "", 14, 328, Palette.WarmIvory);
            PartySummaryLabel = Ui.Label(Root, "", 14, 342, Palette.MutedGray);
            menu.onFocus = s => SetHint(s?.tooltip ?? "LEFT/RIGHT: ADJUST   ENTER: SELECT");

            // Creation Method / Template / Random Selector
            var methodBtn = Ui.Button(Root, "", 18, 32, 320, OnActivateCreationMethodButton, 19);
            methodBtn.onAdjust = CycleCreationMethodOrTemplate;
            methodBtn.tooltip = "L/R: MANUAL, TEMPLATE OR RANDOM. ENTER: REROLL";
            MethodLabel = methodBtn.label;
            menu.Add(methodBtn);

            // Race Selector (7 Playable Races)
            var raceBtn = Ui.Button(Root, "", 18, 54, 320, () => CycleRace(1), 19);
            raceBtn.onAdjust = CycleRace;
            raceBtn.tooltip = "STEP 2: LEFT/RIGHT SELECT RACE (BONUS+FEAT+UTIL)";
            RaceLabel = raceBtn.label;
            menu.Add(raceBtn);

            // Class Selector (9 Playable Classes)
            var classBtn = Ui.Button(Root, "", 18, 76, 320, () => CycleClass(1), 19);
            classBtn.onAdjust = CycleClass;
            classBtn.tooltip = "STEP 3: LEFT/RIGHT SELECT CLASS & STARTER PASSIVE";
            ClassLabel = classBtn.label;
            menu.Add(classBtn);

            // Equipment Slot Configurator
            var eqBtn = Ui.Button(Root, "", 18, 98, 320, () => CycleEquipmentSlot(1), 19);
            eqBtn.onAdjust = CycleEquipmentItem;
            eqBtn.tooltip = "STEP 7: ENTER NEXT SLOT (1-8), L/R EQUIP ITEM";
            EquipmentSlotLabel = eqBtn.label;
            menu.Add(eqBtn);

            BackgroundLabel = Ui.Label(Root, "", 18, 98, Color.clear); // retained for compatibility

            // Ability table header
            Ui.Divider(Root, 18, 120, 320, Palette.DarkBorder, Palette.AntiqueGoldDim);
            Ui.Label(Root, "STAT", 26, 124, Palette.MutedGray);
            string[] headers = { "BASE", "RACE", "TOTAL", "MOD", "COST" };
            for (int c = 0; c < headers.Length; c++)
                Ui.Label(Root, headers[c], 18 + AbilityColumnX[c], 124, Palette.MutedGray, Align.Center);

            // Six Primary Ability Score Point-Buy rows
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var ability = AbilityScores.AllAbilities[i];
                int y = 136 + i * 20;
                var btn = Ui.Button(Root, "", 18, y, 320, () => AdjustAbility(ability, 1), 18);
                btn.onAdjust = d => AdjustAbility(ability, d);
                btn.tooltip = GetAbilityShortTooltip(ability);
                AbilityRowLabels[ability] = Ui.Label(btn.transform, ability.ToString(), 8, 2, Palette.WarmIvory, order: Ui.Widget + 1);
                abilityColumns[ability] = AbilityColumnX.Select(x => Ui.Label(btn.transform, "", x, 2, Palette.WarmIvory, Align.Center, order: Ui.Widget + 1)).ToArray();
                menu.Add(btn);
            }

            // Left panel bottom actions
            Ui.Divider(Root, 18, 260, 320, Palette.DarkBorder, Palette.AntiqueGoldDim);
            resetOrEditBtn = Ui.Button(Root, "RESET", 18, 266, 70, OnResetOrEditClicked, 22, showCursor: false);
            resetOrEditBtn.tooltip = "RESET ABILITY SCORES TO 8 (OR EDIT SELECTED HERO)";
            menu.Add(resetOrEditBtn);

            randOrRerollBtn = Ui.Button(Root, "RND 1", 94, 266, 70, () => OnRandomOrRerollSelected(), 22, showCursor: false);
            randOrRerollBtn.tooltip = "RANDOMIZE CURRENT HERO OR REROLL SELECTED PARTY HERO";
            menu.Add(randOrRerollBtn);

            confirmBtn = Ui.Button(Root, "ADD HERO", 170, 266, 88, ConfirmAndAddToParty, 22, showCursor: false);
            confirmBtn.onAdjust = d => CycleReviewSlot(d);
            confirmBtn.tooltip = "CONFIRM HERO (1-4) OR SELECT PARTY MEMBER IN REVIEW";
            menu.Add(confirmBtn);

            var backBtn = Ui.Button(Root, "BACK", 264, 266, 74, OnBackPressed, 22, showCursor: false);
            backBtn.tooltip = "PREVIOUS STEP OR RETURN TO TITLE SCREEN";
            menu.Add(backBtn);
            menu.onBack = OnBackPressed;

            // Right Panel: Portrait well + Identity/Feat/Passive header + Live Derived Stats
            Ui.Image(Root, PixelArt.Box(40, 46, Palette.DeepCharcoal, Palette.AntiqueGoldDim), 364, 32, Ui.Panels + 1);
            portrait = Ui.Image(Root, PixelArt.FromRows(Session.Class.sprite), 368, 36, Ui.Widget);
            classTitle = Ui.Label(Root, "", 412, 33, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            ClassIdentityLabel = Ui.Label(Root, "", 412, 47, Palette.WarmIvory);
            RaceFeatLabel = Ui.Label(Root, "", 412, 61, Palette.MutedGray);
            StarterPassiveLabel = Ui.Label(Root, "", 364, 82, Palette.Emerald);

            Ui.Divider(Root, 364, 96, 256, Palette.DarkBorder, Palette.AntiqueGoldDim);

            for (int i = 0; i < PreviewedStats.Length; i++)
            {
                var st = PreviewedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 364 + col * 130;
                int y = 104 + row * 19;
                PreviewStatLabels[st] = Ui.Label(Root, "", x, y, Palette.WarmIvory);
            }

            Ui.Divider(Root, 364, 260, 256, Palette.DarkBorder, Palette.AntiqueGoldDim);
            randAllBtn = Ui.Button(Root, "RND ALL", 364, 266, 110, () => RandomizeAll(), 22, showCursor: false);
            randAllBtn.tooltip = "RANDOMIZE ALL 4 PENDING PARTY HEROES FOR REVIEW";
            menu.Add(randAllBtn);

            startBtn = Ui.Button(Root, "CONFIRM 4", 484, 266, 136, () => TryConfirmPartyAndBegin(out _), 22, showCursor: false);
            startBtn.tooltip = "CONFIRM 4-HERO PARTY, SAVE GAME & START PROLOGUE";
            menu.Add(startBtn);

            Builder.Changed += Refresh;
            Refresh();
        }

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

        static string Truncate(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= maxChars ? s : s.Substring(0, maxChars);
        }

        void SetHint(string text)
        {
            HintLabel?.Set(PixelFont.Fit((text ?? "").ToUpperInvariant(), HintWidth));
        }

        public void CycleCreationMethodOrTemplate(int dir)
        {
            EnsureEditingMode();
            int totalModes = 2 + TemplateIds.Length;
            int currentMode = Session.Method == CreationMethod.Manual
                ? 0
                : Session.Method == CreationMethod.Random
                    ? totalModes - 1
                    : Math.Max(1, templateIndex + 1);

            currentMode = (currentMode + dir + totalModes) % totalModes;
            if (currentMode == 0)
            {
                templateIndex = -1;
                Session.SetCreationMethod(CreationMethod.Manual);
                SetHint(Session.LastFeedbackMessage);
            }
            else if (currentMode == totalModes - 1)
            {
                templateIndex = -1;
                Session.TryRandomize(null, out _);
                SetHint(Session.LastFeedbackMessage);
            }
            else
            {
                templateIndex = currentMode - 1;
                Session.TryApplyTemplate(TemplateIds[templateIndex], out _);
                SetHint(Session.LastFeedbackMessage);
            }
            Refresh();
        }

        void OnActivateCreationMethodButton()
        {
            if (Builder.FlowState == NewGameFlowState.PendingPartyReview)
            {
                CycleReviewSlot(1);
                return;
            }
            if (Session.Method == CreationMethod.Random)
            {
                Session.TryRandomize(null, out _);
                SetHint(Session.LastFeedbackMessage);
                Refresh();
            }
            else
            {
                CycleCreationMethodOrTemplate(1);
            }
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
            idx = (idx + dir + RaceIds.Length) % RaceIds.Length;
            Session.SelectRace(RaceIds[idx]);
        }

        public void CycleClass(int dir)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.SelectClass);
            int idx = Array.IndexOf(ClassIds, Session.Class.id);
            if (idx < 0) idx = 0;
            idx = (idx + dir + ClassIds.Length) % ClassIds.Length;
            Session.SelectClass(ClassIds[idx]);
        }

        public void CycleBackground(int dir)
        {
            EnsureEditingMode();
            int idx = Array.IndexOf(BackgroundIds, Session.Background.id);
            if (idx < 0) idx = 0;
            idx = (idx + dir + BackgroundIds.Length) % BackgroundIds.Length;
            Session.SelectBackground(BackgroundIds[idx]);
        }

        public void CycleEquipmentSlot(int dir)
        {
            EnsureEditingMode();
            Session.GoToStep(CreationStep.ConfigureEquipment);
            int idx = Array.IndexOf(EquipmentSlots.All, SelectedEquipmentSlot);
            if (idx < 0) idx = 0;
            idx = (idx + dir + EquipmentSlots.All.Length) % EquipmentSlots.All.Length;
            SelectedEquipmentSlot = EquipmentSlots.All[idx];
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
            idx = (idx + dir + compatible.Count) % compatible.Count;
            Session.TryEquip(SelectedEquipmentSlot, compatible[idx], out _);
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
            }
            else
            {
                Session.ResetPointBuy();
                SetHint(Session.LastFeedbackMessage);
            }
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
            if (Builder.FlowState == NewGameFlowState.PendingCharacterCreation && Session.CurrentStep > CreationStep.SelectRace)
            {
                Session.PreviousStep();
                SetHint($"RETURNED TO STEP {(int)Session.CurrentStep}/7.");
                Refresh();
                return;
            }
            Game?.Go(GameState.MainMenu);
        }

        public void ConfirmAndAddToParty()
        {
            if (Builder.FlowState == NewGameFlowState.PendingPartyReview && !Builder.EditingExistingMember)
            {
                CycleReviewSlot(1);
                return;
            }

            if (!Builder.TryConfirmCurrentCharacter(out var confirmed, out string err))
            {
                Sfx.Play(Sound.Deny);
                SetHint(err ?? "INVALID CHARACTER CONFIGURATION");
                return;
            }

            Sfx.Play(Sound.Success);
            SetHint(Builder.LastFeedback);
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
            Refresh();
            Game?.BeginOpeningSequence();
            return true;
        }

        public bool TryBeginExpedition(out string error)
        {
            var activeRoster = Game != null ? Game.Roster : Roster;
            if (Builder.CanConfirmParty && !Builder.IsPartyConfirmed)
            {
                if (!Builder.TryConfirmParty(activeRoster, out error))
                {
                    Sfx.Play(Sound.Deny);
                    SetHint(error);
                    Refresh();
                    return false;
                }
            }
            error = activeRoster.ValidateExpeditionStart();
            if (error != null)
            {
                Sfx.Play(Sound.Deny);
                SetHint(error);
                Refresh();
                return false;
            }
            Game?.BeginFirstExpedition();
            return true;
        }

        public void Refresh()
        {
            if (RemainingPointsLabel == null) return;

            int rem = Session.PointBuy.RemainingPoints;
            RemainingPointsLabel.Set($"POINTS {rem}/{Session.PointBuy.Config.budget}");
            RemainingPointsLabel.Color = rem < 0 ? Palette.SpecCrimson : rem == 0 ? Palette.Emerald : Palette.AntiqueGold;

            if (Builder.FlowState == NewGameFlowState.PendingPartyReview && !Builder.EditingExistingMember)
                StepLabel?.Set($"REVIEW ({Builder.SelectedReviewIndex + 1}/4)");
            else
                StepLabel?.Set($"H{Builder.CurrentMemberIndex + 1}/4 STEP {(int)Session.CurrentStep}/7");

            if (MethodLabel != null)
            {
                if (Session.Method == CreationMethod.Template && !string.IsNullOrEmpty(Session.ActiveTemplateId) &&
                    Database.Templates.TryGetValue(Session.ActiveTemplateId, out var tpl))
                {
                    MethodLabel.Set(Selector("TEMPLATE", tpl.name));
                }
                else
                {
                    MethodLabel.Set(Selector("MODE", Session.Method.ToString()));
                }
            }

            string featName = Session.Race?.racialFeat?.name ?? "NONE";
            RaceLabel.Set(Selector("RACE", Session.Race.name));
            string identity = Session.Class?.gameplayIdentity ?? Session.Class?.role ?? "";
            ClassLabel.Set(Selector("CLASS", Session.Class.name));
            BackgroundLabel?.Set($"BG < {Session.Background.name.ToUpperInvariant()} >");

            classTitle?.Set(PixelFont.Fit($"{Session.Race.name} {Session.Class.name}".ToUpperInvariant(), RightTextWidth));
            ClassIdentityLabel?.Set(PixelFont.Fit(identity.ToUpperInvariant(), RightTextWidth));
            RaceFeatLabel?.Set(PixelFont.Fit($"FEAT {featName.ToUpperInvariant()}", RightTextWidth));

            var sp = Session.StarterPassive;
            StarterPassiveLabel?.Set(PixelFont.Fit($"PASSIVE {(sp != null ? sp.name.ToUpperInvariant() : "NONE")}", 131));

            var eq = Session.GetEquipped(SelectedEquipmentSlot);
            string eqName = eq != null ? eq.name.ToUpperInvariant() : "EMPTY";
            EquipmentSlotLabel?.Set(Selector(EquipmentSlots.ShortName(SelectedEquipmentSlot), eqName));

            if (portrait != null && Session.Class?.sprite != null)
                portrait.sprite = PixelArt.FromRows(Session.Class.sprite);

            var baseScores = Session.BaseScores;
            var raceMods = Session.RacialModifiers;
            var finalScores = Session.FinalScores;
            var abilMods = Session.AbilityModifiers;

            foreach (var a in AbilityScores.AllAbilities)
            {
                int b = baseScores[a];
                int r = raceMods[a];
                int fin = finalScores[a];
                int mod = abilMods[a];
                int cost = Session.PointBuy.GetCost(a);
                var cols = abilityColumns[a];
                cols[0].Set(b.ToString());
                cols[1].Set(r > 0 ? $"+{r}" : r < 0 ? r.ToString() : "-");
                cols[1].Color = r > 0 ? Palette.Emerald : r < 0 ? Palette.SpecCrimson : Palette.MutedGray;
                cols[2].Set(fin.ToString());
                cols[2].Color = Palette.WarmIvory;
                cols[3].Set(AbilityScores.FormatMod(mod));
                cols[3].Color = mod > 0 ? Palette.Emerald : mod < 0 ? Palette.SpecCrimson : Palette.WarmIvory;
                cols[4].Set(cost.ToString());
                cols[4].Color = Palette.AntiqueGold;
            }

            var preview = Session.PreviewDerivedStats;
            foreach (var kv in PreviewStatLabels)
            {
                var def = Database.DerivedStatDefs[kv.Key];
                kv.Value.Set($"{def.shortName} {preview.FormatValue(kv.Key)}");
            }

            bool inReview = Builder.FlowState == NewGameFlowState.PendingPartyReview && !Builder.EditingExistingMember;
            if (resetOrEditBtn != null)
                resetOrEditBtn.label.Set(inReview ? "EDIT" : "RESET");
            if (randOrRerollBtn != null)
                randOrRerollBtn.label.Set(inReview ? "REROLL" : "RND 1");
            if (confirmBtn != null)
            {
                confirmBtn.label.Set(inReview ? $"HERO {Builder.SelectedReviewIndex + 1}" : "ADD HERO");
                confirmBtn.interactable = inReview || Session.CanConfirm;
            }

            if (startBtn != null)
                startBtn.interactable = Builder.CanConfirmParty;

            if (PartySummaryLabel != null)
            {
                if (Builder.PendingParty.Count > 0)
                {
                    var members = Builder.PendingParty.Select((c, i) =>
                    {
                        string mark = inReview && i == Builder.SelectedReviewIndex ? "*" : "";
                        return $"{i + 1}{mark}:{Truncate(c.name.ToUpperInvariant(), 7)}";
                    });
                    PartySummaryLabel.Set(PixelFont.Fit($"PARTY ({Builder.PendingParty.Count}/{CharacterRoster.PartySize}): " + string.Join(" ", members), HintWidth));
                }
                else
                {
                    PartySummaryLabel.Set($"PARTY (0/{CharacterRoster.PartySize}): CREATE 4 HEROES TO BEGIN");
                }
            }
        }
    }
}
