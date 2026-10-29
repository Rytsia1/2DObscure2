using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Full Character Creation & Party Selection screen (PRD Section 6 & 12.1-12.2).
    // Supports: Race -> Class -> Background -> 27-Point Buy -> Real-time Derived Stats Preview -> Confirm to Roster -> Party Selection.
    public class CharacterCreationScreen : GameScreen
    {
        public CharacterCreationSession Session { get; private set; }
        public Label RemainingPointsLabel { get; private set; }
        public Label RaceLabel { get; private set; }
        public Label ClassLabel { get; private set; }
        public Label BackgroundLabel { get; private set; }
        public Label PartySummaryLabel { get; private set; }
        public Label HintLabel { get; private set; }

        public readonly Dictionary<Ability, Label> AbilityRowLabels = new Dictionary<Ability, Label>();
        public readonly Dictionary<DerivedStatId, Label> PreviewStatLabels = new Dictionary<DerivedStatId, Label>();

        Selectable confirmBtn;
        SpriteRenderer portrait;
        int editingPartySlot;

        static readonly string[] RaceIds = Database.Races.Keys.ToArray();
        static readonly string[] ClassIds = Database.Classes.Keys.ToArray();
        static readonly string[] BackgroundIds = Database.Backgrounds.Keys.ToArray();

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

        public CharacterCreationScreen(CharacterCreationSession session = null)
        {
            Session = session ?? new CharacterCreationSession();
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.Void, Palette.Night, Palette.Dusk);

            // Top bar: Title & Remaining Point Buy Budget
            Ui.Panel(Root, 0, 0, Ui.Width, 16);
            Ui.Label(Root, "CHARACTER CREATION", 6, 4, Palette.Gold);
            RemainingPointsLabel = Ui.Label(Root, "", 314, 4, Palette.Gold, Align.Right);

            // Left panel: Race, Class, Background, and 6 Point Buy Ability Rows
            Ui.Panel(Root, 2, 17, 174, 134);
            // Right panel: Live Character & Derived Stats Preview
            Ui.Panel(Root, 178, 17, 140, 134);
            // Bottom panel: Tooltip / Party Bar & Action buttons
            Ui.Panel(Root, 0, 152, Ui.Width, 28);

            var menu = Root.gameObject.AddComponent<Menu>();
            HintLabel = Ui.Label(Root, "", 6, 155, Palette.Parchment);
            PartySummaryLabel = Ui.Label(Root, "", 6, 165, Palette.Ash);
            menu.onFocus = s => HintLabel.Set(s?.tooltip ?? "LEFT/RIGHT: ADJUST   ENTER: SELECT");

            // 1. Race Selector
            var raceBtn = Ui.Button(Root, "", 8, 20, 162, () => CycleRace(1), 11);
            raceBtn.onAdjust = CycleRace;
            raceBtn.tooltip = "CHOOSE RACE (MODIFIES ABILITY SCORES AFTER POINT BUY)";
            RaceLabel = raceBtn.label;
            menu.Add(raceBtn);

            // 2. Class Selector
            var classBtn = Ui.Button(Root, "", 8, 32, 162, () => CycleClass(1), 11);
            classBtn.onAdjust = CycleClass;
            classBtn.tooltip = "CHOOSE CLASS (ROLE, STARTING SKILLS, STAT SCALING)";
            ClassLabel = classBtn.label;
            menu.Add(classBtn);

            // 3. Background Selector
            var bgBtn = Ui.Button(Root, "", 8, 44, 162, () => CycleBackground(1), 11);
            bgBtn.onAdjust = CycleBackground;
            bgBtn.tooltip = "CHOOSE BACKGROUND (IDENTITY, PROFICIENCIES, EVENT BONUSES)";
            BackgroundLabel = bgBtn.label;
            menu.Add(bgBtn);

            Ui.Label(Root, "STAT  BASE RACE FIN MOD COST", 8, 57, Palette.Ash);

            // 4. Six Primary Ability Score Point-Buy rows
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var ability = AbilityScores.AllAbilities[i];
                int y = 66 + i * 11;
                var btn = Ui.Button(Root, "", 8, y, 162, () => AdjustAbility(ability, 1), 10);
                btn.onAdjust = d => AdjustAbility(ability, d);
                btn.tooltip = $"{Database.AbilityDefs[ability].displayName.ToUpperInvariant()}: {Database.AbilityDefs[ability].description.ToUpperInvariant()}";
                AbilityRowLabels[ability] = btn.label;
                menu.Add(btn);
            }

            // 5. Action Buttons: Reset, Confirm to Party Slot, Status View, Start Run, Back
            var resetBtn = Ui.Button(Root, "RESET", 8, 135, 38, () => { Session.ResetPointBuy(); Refresh(); }, 12);
            resetBtn.tooltip = "RESET ALL ABILITY SCORES TO 8 (27 POINTS REMAINING)";
            menu.Add(resetBtn);

            var slotBtn = Ui.Button(Root, "", 48, 135, 42, null, 12);
            void UpdateSlotLabel() => slotBtn.label.Set($"SLOT {editingPartySlot + 1}");
            slotBtn.onClick = () => { editingPartySlot = (editingPartySlot + 1) % CharacterRoster.PartySize; UpdateSlotLabel(); Refresh(); };
            slotBtn.onAdjust = d => { editingPartySlot = (editingPartySlot + d + CharacterRoster.PartySize) % CharacterRoster.PartySize; UpdateSlotLabel(); Refresh(); };
            slotBtn.tooltip = "CHOOSE WHICH OF THE 4 PARTY SLOTS THIS CHARACTER TAKES";
            UpdateSlotLabel();
            menu.Add(slotBtn);

            confirmBtn = Ui.Button(Root, "SAVE HERO", 92, 135, 50, ConfirmToRoster, 12);
            confirmBtn.tooltip = "CONFIRM ALLOCATION AND SAVE CHARACTER TO ROSTER AND PARTY";
            menu.Add(confirmBtn);

            var backBtn = Ui.Button(Root, "BACK", 144, 135, 28, () => Game?.Go(GameState.MainMenu), 12);
            backBtn.tooltip = "RETURN TO TITLE SCREEN";
            menu.Add(backBtn);
            menu.onBack = () => Game?.Go(GameState.MainMenu);

            // Right Panel: Portrait + Live Derived Stats Preview
            portrait = Ui.Image(Root, PixelArt.FromRows(Session.Class.sprite), 184, 20, Ui.Widget);
            Ui.Label(Root, "DERIVED PREVIEW", 206, 22, Palette.Gold);

            for (int i = 0; i < PreviewedStats.Length; i++)
            {
                var st = PreviewedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 183 + col * 68;
                int y = 38 + row * 11;
                PreviewStatLabels[st] = Ui.Label(Root, "", x, y, Palette.Bone);
            }

            var statusBtn = Ui.Button(Root, "STATUS", 183, 135, 62, () => Game?.Go(GameState.CharacterStatus), 12);
            statusBtn.tooltip = "INSPECT FULL CHARACTER STATUS AND STAT FORMULA TOOLTIPS";
            menu.Add(statusBtn);

            var startBtn = Ui.Button(Root, "START RUN", 249, 135, 64, () => Game?.StartRun(), 12);
            startBtn.tooltip = "BEGIN RUN WITH THE SELECTED 4-HERO PARTY";
            menu.Add(startBtn);

            Session.Changed += Refresh;
            Refresh();
        }

        void CycleRace(int dir)
        {
            int idx = Array.IndexOf(RaceIds, Session.Race.id);
            idx = (idx + dir + RaceIds.Length) % RaceIds.Length;
            Session.SelectRace(RaceIds[idx]);
        }

        void CycleClass(int dir)
        {
            int idx = Array.IndexOf(ClassIds, Session.Class.id);
            idx = (idx + dir + ClassIds.Length) % ClassIds.Length;
            Session.SelectClass(ClassIds[idx]);
        }

        void CycleBackground(int dir)
        {
            int idx = Array.IndexOf(BackgroundIds, Session.Background.id);
            idx = (idx + dir + BackgroundIds.Length) % BackgroundIds.Length;
            Session.SelectBackground(BackgroundIds[idx]);
        }

        void AdjustAbility(Ability ability, int delta)
        {
            bool ok = delta > 0 ? Session.IncreaseStat(ability) : Session.DecreaseStat(ability);
            if (!ok) Sfx.Play(Sound.Deny);
            Refresh();
        }

        void ConfirmToRoster()
        {
            if (!Session.CanConfirm)
            {
                Sfx.Play(Sound.Deny);
                HintLabel.Set((Session.Validate() ?? "INVALID ALLOCATION").ToUpperInvariant());
                return;
            }
            var roster = Game != null ? Game.Roster : new CharacterRoster();
            string customName = $"{Session.Class.name}";
            Session.CharacterName = customName;
            var created = Session.Confirm(roster);
            roster.SetPartySlot(editingPartySlot, created.id);
            HintLabel.Set($"SAVED {created.name.ToUpperInvariant()} TO PARTY SLOT {editingPartySlot + 1}!");
            Refresh();
        }

        public void Refresh()
        {
            if (RemainingPointsLabel == null) return;

            int rem = Session.PointBuy.RemainingPoints;
            RemainingPointsLabel.Set($"POINTS {rem}/{Session.PointBuy.Config.budget}");
            RemainingPointsLabel.Color = rem < 0 ? Palette.Crimson : rem == 0 ? Palette.Moss : Palette.Gold;

            RaceLabel.Set($"RACE < {Session.Race.name.ToUpperInvariant()} >");
            ClassLabel.Set($"CLASS < {Session.Class.name.ToUpperInvariant()} ({Session.Class.role.ToUpperInvariant()}) >");
            BackgroundLabel.Set($"BG < {Session.Background.name.ToUpperInvariant()} >");

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
                string rStr = r > 0 ? $"+{r}" : r < 0 ? r.ToString() : "+0";
                string mStr = AbilityScores.FormatMod(mod);
                AbilityRowLabels[a].Set($"{a,-3} {b,2} {rStr,3} ={fin,2} ({mStr,2}) C:{cost}");
            }

            var preview = Session.PreviewDerivedStats;
            foreach (var kv in PreviewStatLabels)
            {
                var def = Database.DerivedStatDefs[kv.Key];
                kv.Value.Set($"{def.shortName} {preview.FormatValue(kv.Key)}");
            }

            if (confirmBtn != null)
                confirmBtn.interactable = Session.CanConfirm;

            var roster = Game != null ? Game.Roster : null;
            if (roster != null && PartySummaryLabel != null)
            {
                var partyNames = roster.GetActiveParty().Select((c, i) => $"{i + 1}:{c.name.ToUpperInvariant()}");
                PartySummaryLabel.Set("PARTY: " + string.Join("  ", partyNames));
            }
        }
    }
}

