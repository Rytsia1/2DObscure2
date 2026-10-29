using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Full Character Creation & 4-Hero Party Assembly screen (Character Creation System Specification Sections 2-10).
    // Supports:
    // 1. Create New Character -> 2. Select Race (7 playable races) -> 3. Select Class (9 playable classes)
    // -> 4. Allocate Ability Scores (27-Point Buy) -> 5. Review Character Stats -> 6. Review Starter Passive
    // -> 7. Configure Equipment (8 slots with Strict Weapon Compatibility) -> 8. Confirm Character
    // -> 9. Add Character to Party -> 10. Repeat until Party has 4 Heroes -> 11. Begin Expedition.
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

        // Ability table columns (centers) inside the 160px rows at x=11: BASE, RACE, TOTAL, MOD, COST.
        static readonly int[] AbilityColumnX = { 39, 65, 93, 120, 146 };
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

        public CharacterCreationScreen(CharacterCreationSession session = null, CharacterRoster roster = null, NewGamePartyBuilder builder = null)
        {
            if (builder != null)
                Builder = builder;
            else if (session != null)
                Builder = new NewGamePartyBuilder(session);
            else if (Game?.NewGameBuilder != null)
                Builder = Game.NewGameBuilder;
            else
                Builder = new NewGamePartyBuilder();

            Roster = roster ?? (Game != null ? Game.Roster : new CharacterRoster(startWithEmptyParty: true));
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.Void, Palette.Night, Palette.Dusk);

            // Top bar: Title, Current Step & Remaining Point Buy Budget
            Ui.Panel(Root, 0, 0, Ui.Width, 16);
            Ui.Label(Root, "CHARACTER CREATION", 6, 4, Palette.Gold);
            StepLabel = Ui.Label(Root, "", 162, 4, Palette.Parchment, Align.Center);
            RemainingPointsLabel = Ui.Label(Root, "", 314, 4, Palette.Gold, Align.Right);

            // Left panel: Creation Method, Race, Class, Equipment (8 slots), and 6 Point Buy Ability Rows
            Ui.Panel(Root, 2, 17, 174, 134);
            // Right panel: Live Character, Starter Passive & Derived Stats Preview
            Ui.Panel(Root, 178, 17, 140, 134);
            // Bottom panel: Validation Feedback / Party Bar (X/4) & Action buttons
            Ui.Panel(Root, 0, 152, Ui.Width, 28);

            var menu = Root.gameObject.AddComponent<Menu>();
            HintLabel = Ui.Label(Root, "", 6, 155, Palette.Parchment);
            PartySummaryLabel = Ui.Label(Root, "", 6, 165, Palette.Ash);
            menu.onFocus = s => SetHint(s?.tooltip ?? "LEFT/RIGHT: ADJUST   ENTER: SELECT");

            // 1. Creation Method / Template / Random Selector (Section 4)
            var methodBtn = Ui.Button(Root, "", 11, 19, 160, OnActivateCreationMethodButton, 9);
            methodBtn.onAdjust = CycleCreationMethodOrTemplate;
            methodBtn.tooltip = "L/R: MANUAL, TEMPLATE OR RANDOM. ENTER: REROLL";
            MethodLabel = methodBtn.label;
            menu.Add(methodBtn);

            // 2. Race Selector (7 Playable Races) — x=11 keeps '>' cursor at x=4 inside left panel border
            var raceBtn = Ui.Button(Root, "", 11, 29, 160, () => CycleRace(1), 9);
            raceBtn.onAdjust = CycleRace;
            raceBtn.tooltip = "STEP 2: LEFT/RIGHT SELECT RACE (BONUS+FEAT+UTIL)";
            RaceLabel = raceBtn.label;
            menu.Add(raceBtn);

            // 3. Class Selector (9 Playable Classes)
            var classBtn = Ui.Button(Root, "", 11, 39, 160, () => CycleClass(1), 9);
            classBtn.onAdjust = CycleClass;
            classBtn.tooltip = "STEP 3: LEFT/RIGHT SELECT CLASS & STARTER PASSIVE";
            ClassLabel = classBtn.label;
            menu.Add(classBtn);

            // 4. Equipment Slot Configurator (8 Slots with Strict Weapon Compatibility)
            var eqBtn = Ui.Button(Root, "", 11, 49, 160, () => CycleEquipmentSlot(1), 9);
            eqBtn.onAdjust = CycleEquipmentItem;
            eqBtn.tooltip = "STEP 7: ENTER NEXT SLOT (1-8), L/R EQUIP ITEM";
            EquipmentSlotLabel = eqBtn.label;
            menu.Add(eqBtn);

            BackgroundLabel = Ui.Label(Root, "", 11, 49, Color.clear); // retained for compatibility

            // Ability table: one label per column so the proportional font still lines up.
            Ui.Label(Root, "STAT", 17, 59, Palette.Ash);
            string[] headers = { "BASE", "RACE", "TOTAL", "MOD", "COST" };
            for (int c = 0; c < headers.Length; c++)
                Ui.Label(Root, headers[c], 11 + AbilityColumnX[c], 59, Palette.Ash, Align.Center);

            // 5. Six Primary Ability Score Point-Buy rows
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var ability = AbilityScores.AllAbilities[i];
                int y = 67 + i * 11;
                var btn = Ui.Button(Root, "", 11, y, 160, () => AdjustAbility(ability, 1), 10);
                btn.onAdjust = d => AdjustAbility(ability, d);
                btn.tooltip = GetAbilityShortTooltip(ability);
                AbilityRowLabels[ability] = Ui.Label(btn.transform, ability.ToString(), 6, 2, Palette.Bone, order: Ui.Widget + 1);
                abilityColumns[ability] = AbilityColumnX.Select(x => Ui.Label(btn.transform, "", x, 2, Palette.Parchment, Align.Center, order: Ui.Widget + 1)).ToArray();
                menu.Add(btn);
            }

            // 6. Action Buttons spaced with >=8px gaps so the '>' focus cursor never overlaps adjacent buttons
            resetOrEditBtn = Ui.Button(Root, "RESET", 10, 135, 34, OnResetOrEditClicked, 12);
            resetOrEditBtn.tooltip = "RESET ABILITY SCORES TO 8 (OR EDIT SELECTED HERO)";
            menu.Add(resetOrEditBtn);

            randOrRerollBtn = Ui.Button(Root, "RND 1", 52, 135, 34, () => OnRandomOrRerollSelected(), 12);
            randOrRerollBtn.tooltip = "RANDOMIZE CURRENT HERO OR REROLL SELECTED PARTY HERO";
            menu.Add(randOrRerollBtn);

            confirmBtn = Ui.Button(Root, "ADD HERO", 94, 135, 48, ConfirmAndAddToParty, 12);
            confirmBtn.onAdjust = d => CycleReviewSlot(d);
            confirmBtn.tooltip = "CONFIRM HERO (1-4) OR SELECT PARTY MEMBER IN REVIEW";
            menu.Add(confirmBtn);

            var backBtn = Ui.Button(Root, "BACK", 150, 135, 24, OnBackPressed, 12);
            backBtn.tooltip = "PREVIOUS STEP OR RETURN TO TITLE SCREEN";
            menu.Add(backBtn);
            menu.onBack = OnBackPressed;

            // Right Panel: Portrait (16x24 at y=19..43) + Identity/Feat/Passive header + Live Derived Stats (y=55..124)
            portrait = Ui.Image(Root, PixelArt.FromRows(Session.Class.sprite), 182, 19, Ui.Widget);
            classTitle = Ui.Label(Root, "", 202, 19, Palette.Gold);
            ClassIdentityLabel = Ui.Label(Root, "", 202, 28, Palette.Parchment);
            RaceFeatLabel = Ui.Label(Root, "", 202, 37, Palette.Ash);
            StarterPassiveLabel = Ui.Label(Root, "", 183, 46, Palette.Moss);

            for (int i = 0; i < PreviewedStats.Length; i++)
            {
                var st = PreviewedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 183 + col * 68;
                int y = 56 + row * 9;
                PreviewStatLabels[st] = Ui.Label(Root, "", x, y, Palette.Bone);
            }

            randAllBtn = Ui.Button(Root, "RND ALL", 188, 135, 48, () => RandomizeAll(), 12);
            randAllBtn.tooltip = "RANDOMIZE ALL 4 PENDING PARTY HEROES FOR REVIEW";
            menu.Add(randAllBtn);

            startBtn = Ui.Button(Root, "CONFIRM 4", 244, 135, 70, () => TryConfirmPartyAndBegin(out _), 12);
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

        // "RACE < HUMAN >", with the value trimmed so the whole row fits a 160px selector.
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

        // Cycles between Manual -> Predefined Templates (0..N-1) -> Random creation methods (Section 4).
        public void CycleCreationMethodOrTemplate(int dir)
        {
            EnsureEditingMode();
            int totalModes = 2 + TemplateIds.Length; // 0 = Manual, 1..TemplateIds.Length = Templates, last = Random
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

        // In PendingPartyReview, rerolls ONLY the selected party member (Section 5.2).
        // In PendingCharacterCreation, randomizes the current character being configured (Section 5).
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
            RemainingPointsLabel.Color = rem < 0 ? Palette.Crimson : rem == 0 ? Palette.Moss : Palette.Gold;

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
                cols[1].Color = r > 0 ? Palette.Moss : r < 0 ? Palette.Crimson : Palette.Ash;
                cols[2].Set(fin.ToString());
                cols[2].Color = Palette.Bone;
                cols[3].Set(AbilityScores.FormatMod(mod));
                cols[3].Color = mod > 0 ? Palette.Moss : mod < 0 ? Palette.Crimson : Palette.Parchment;
                cols[4].Set(cost.ToString());
                cols[4].Color = Palette.Gold;
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
