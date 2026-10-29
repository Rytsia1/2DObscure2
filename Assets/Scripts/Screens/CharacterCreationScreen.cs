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
        public CharacterCreationSession Session { get; private set; }
        public CharacterRoster Roster { get; private set; }

        public Label RemainingPointsLabel { get; private set; }
        public Label StepLabel { get; private set; }
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

        public EquipmentSlot SelectedEquipmentSlot { get; private set; } = EquipmentSlot.MainHand;

        Selectable confirmBtn;
        Selectable startBtn;
        SpriteRenderer portrait;
        int editingPartySlot;

        static readonly string[] RaceIds = Database.PlayableRaceIds;
        static readonly string[] ClassIds = Database.PlayableClassIds;
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

        public CharacterCreationScreen(CharacterCreationSession session = null, CharacterRoster roster = null)
        {
            Session = session ?? new CharacterCreationSession();
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

            // Left panel: Race, Class, Starter Passive, Equipment (8 slots), and 6 Point Buy Ability Rows
            Ui.Panel(Root, 2, 17, 174, 134);
            // Right panel: Live Character & Derived Stats Preview
            Ui.Panel(Root, 178, 17, 140, 134);
            // Bottom panel: Validation Feedback / Party Bar (X/4) & Action buttons
            Ui.Panel(Root, 0, 152, Ui.Width, 28);

            var menu = Root.gameObject.AddComponent<Menu>();
            HintLabel = Ui.Label(Root, "", 6, 155, Palette.Parchment);
            PartySummaryLabel = Ui.Label(Root, "", 6, 165, Palette.Ash);
            menu.onFocus = s => SetHint(s?.tooltip ?? "LEFT/RIGHT: ADJUST   ENTER: SELECT");

            // 1. Race Selector (7 Playable Races) — x=11 keeps '>' cursor at x=4 inside left panel border
            var raceBtn = Ui.Button(Root, "", 11, 19, 160, () => CycleRace(1), 10);
            raceBtn.onAdjust = CycleRace;
            raceBtn.tooltip = "STEP 2: LEFT/RIGHT SELECT RACE (BONUS+FEAT+UTIL)";
            RaceLabel = raceBtn.label;
            menu.Add(raceBtn);

            // 2. Class Selector (9 Playable Classes)
            var classBtn = Ui.Button(Root, "", 11, 29, 160, () => CycleClass(1), 10);
            classBtn.onAdjust = CycleClass;
            classBtn.tooltip = "STEP 3: LEFT/RIGHT SELECT CLASS & STARTER PASSIVE";
            ClassLabel = classBtn.label;
            menu.Add(classBtn);

            // 3. Starter Passive & Background Display
            StarterPassiveLabel = Ui.Label(Root, "", 11, 40, Palette.Moss);
            BackgroundLabel = Ui.Label(Root, "", 11, 40, Color.clear); // retained for compatibility

            // 4. Equipment Slot Configurator (8 Slots with Strict Weapon Compatibility)
            var eqBtn = Ui.Button(Root, "", 11, 48, 160, () => CycleEquipmentSlot(1), 10);
            eqBtn.onAdjust = CycleEquipmentItem;
            eqBtn.tooltip = "STEP 7: ENTER NEXT SLOT (1-8), L/R EQUIP ITEM";
            EquipmentSlotLabel = eqBtn.label;
            menu.Add(eqBtn);

            // Centered column header aligned with 25-char ability rows inside x=11..171 (center=91)
            Ui.Label(Root, "STAT BS  RAC FIN  MOD CST", 91, 59, Palette.Ash, Align.Center);

            // 5. Six Primary Ability Score Point-Buy rows
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var ability = AbilityScores.AllAbilities[i];
                int y = 67 + i * 11;
                var btn = Ui.Button(Root, "", 11, y, 160, () => AdjustAbility(ability, 1), 10);
                btn.onAdjust = d => AdjustAbility(ability, d);
                btn.tooltip = GetAbilityShortTooltip(ability);
                AbilityRowLabels[ability] = btn.label;
                menu.Add(btn);
            }

            // 6. Action Buttons spaced with >=8px gaps so the '>' focus cursor never overlaps adjacent buttons
            var resetBtn = Ui.Button(Root, "RESET", 10, 135, 34, () => { Session.ResetPointBuy(); Refresh(); }, 12);
            resetBtn.tooltip = "RESET ALL ABILITY SCORES TO 8 (27 POINTS LEFT)";
            menu.Add(resetBtn);

            var newBtn = Ui.Button(Root, "NEW", 52, 135, 26, () => { Session.CreateNewCharacter(); Refresh(); }, 12);
            newBtn.tooltip = "CREATE A NEW CHARACTER BUILD FROM STEP 1";
            menu.Add(newBtn);

            confirmBtn = Ui.Button(Root, "ADD HERO", 86, 135, 52, ConfirmAndAddToParty, 12);
            confirmBtn.tooltip = "CONFIRM HERO AND ADD TO EXPEDITION PARTY (4 REQ)";
            menu.Add(confirmBtn);

            var backBtn = Ui.Button(Root, "BACK", 146, 135, 28, () => Game?.Go(GameState.MainMenu), 12);
            backBtn.tooltip = "RETURN TO TITLE SCREEN";
            menu.Add(backBtn);
            menu.onBack = () => Game?.Go(GameState.MainMenu);

            // Right Panel: Portrait (16x24 at y=19..43) + Identity/Feat header + Live Derived Stats (y=46..123)
            portrait = Ui.Image(Root, PixelArt.FromRows(Session.Class.sprite), 182, 19, Ui.Widget);
            Ui.Label(Root, "DERIVED PREVIEW", 202, 19, Palette.Gold);
            ClassIdentityLabel = Ui.Label(Root, "", 202, 28, Palette.Parchment);
            RaceFeatLabel = Ui.Label(Root, "", 202, 37, Palette.Ash);

            for (int i = 0; i < PreviewedStats.Length; i++)
            {
                var st = PreviewedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 183 + col * 68;
                int y = 46 + row * 10;
                PreviewStatLabels[st] = Ui.Label(Root, "", x, y, Palette.Bone);
            }

            var statusBtn = Ui.Button(Root, "STATUS", 188, 135, 52, () => Game?.Go(GameState.CharacterStatus), 12);
            statusBtn.tooltip = "INSPECT FULL CHARACTER STATUS & STAT FORMULAS";
            menu.Add(statusBtn);

            startBtn = Ui.Button(Root, "EXPEDITION", 249, 135, 66, () => TryBeginExpedition(out _), 12);
            startBtn.tooltip = "BEGIN EXPEDITION (REQUIRES COMPLETE 4-HERO PARTY)";
            menu.Add(startBtn);

            Session.Changed += Refresh;
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

        static string Truncate(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= maxChars ? s : s.Substring(0, maxChars);
        }

        void SetHint(string text)
        {
            HintLabel?.Set(Truncate((text ?? "").ToUpperInvariant(), 50));
        }

        public void CycleRace(int dir)
        {
            Session.GoToStep(CreationStep.SelectRace);
            int idx = Array.IndexOf(RaceIds, Session.Race.id);
            if (idx < 0) idx = 0;
            idx = (idx + dir + RaceIds.Length) % RaceIds.Length;
            Session.SelectRace(RaceIds[idx]);
        }

        public void CycleClass(int dir)
        {
            Session.GoToStep(CreationStep.SelectClass);
            int idx = Array.IndexOf(ClassIds, Session.Class.id);
            if (idx < 0) idx = 0;
            idx = (idx + dir + ClassIds.Length) % ClassIds.Length;
            Session.SelectClass(ClassIds[idx]);
        }

        public void CycleBackground(int dir)
        {
            int idx = Array.IndexOf(BackgroundIds, Session.Background.id);
            if (idx < 0) idx = 0;
            idx = (idx + dir + BackgroundIds.Length) % BackgroundIds.Length;
            Session.SelectBackground(BackgroundIds[idx]);
        }

        public void CycleEquipmentSlot(int dir)
        {
            Session.GoToStep(CreationStep.ConfigureEquipment);
            int idx = Array.IndexOf(EquipmentSlots.All, SelectedEquipmentSlot);
            if (idx < 0) idx = 0;
            idx = (idx + dir + EquipmentSlots.All.Length) % EquipmentSlots.All.Length;
            SelectedEquipmentSlot = EquipmentSlots.All[idx];
            Refresh();
        }

        public void CycleEquipmentItem(int dir)
        {
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
            Session.GoToStep(CreationStep.AllocateAbilityScores);
            bool ok = delta > 0 ? Session.IncreaseStat(ability) : Session.DecreaseStat(ability);
            if (!ok) Sfx.Play(Sound.Deny);
            SetHint(Session.LastFeedbackMessage);
            Refresh();
        }

        public void ConfirmAndAddToParty()
        {
            if (!Session.CanConfirm)
            {
                Sfx.Play(Sound.Deny);
                SetHint(Session.Validate() ?? "INVALID CHARACTER CONFIGURATION");
                return;
            }

            var activeRoster = Game != null ? Game.Roster : Roster;
            // If the roster was preloaded with the 4 default heroes and the user is building a new party,
            // allow replacing slots sequentially or adding to an open party.
            if (activeRoster.IsPartyComplete && editingPartySlot < CharacterRoster.PartySize)
            {
                string customId = $"hero_{editingPartySlot + 1}_{Session.Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
                Session.CharacterName = $"{Session.Class.name}";
                var created = Session.Confirm(activeRoster, customId);
                activeRoster.SetPartySlot(editingPartySlot, created.id);
                editingPartySlot = (editingPartySlot + 1) % CharacterRoster.PartySize;
                SetHint($"ADDED {created.name} TO PARTY ({activeRoster.PartyCount}/{CharacterRoster.PartySize})!");
                Session.CreateNewCharacter();
                Refresh();
                return;
            }

            string newId = $"hero_{activeRoster.PartyCount + 1}_{Session.Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            Session.CharacterName = $"{Session.Class.name}";
            var confirmed = Session.Confirm(null, newId);
            if (!activeRoster.TryAddToParty(confirmed, out string partyErr))
            {
                Sfx.Play(Sound.Deny);
                SetHint(partyErr);
                return;
            }

            SetHint($"ADDED {confirmed.name} ({activeRoster.PartyCount}/{CharacterRoster.PartySize})!");
            if (!activeRoster.IsPartyComplete)
                Session.CreateNewCharacter();
            Refresh();
        }

        public bool TryBeginExpedition(out string error)
        {
            var activeRoster = Game != null ? Game.Roster : Roster;
            error = activeRoster.ValidateExpeditionStart();
            if (error != null)
            {
                Sfx.Play(Sound.Deny);
                SetHint(error);
                Refresh();
                return false;
            }
            Game?.StartRun();
            return true;
        }

        public void Refresh()
        {
            if (RemainingPointsLabel == null) return;

            int rem = Session.PointBuy.RemainingPoints;
            RemainingPointsLabel.Set($"POINTS {rem}/{Session.PointBuy.Config.budget}");
            RemainingPointsLabel.Color = rem < 0 ? Palette.Crimson : rem == 0 ? Palette.Moss : Palette.Gold;

            StepLabel?.Set($"STEP {(int)Session.CurrentStep}/7");

            string featName = Session.Race?.racialFeat?.name ?? "NONE";
            RaceLabel.Set($"RACE < {Truncate(Session.Race.name.ToUpperInvariant(), 16)} >");
            string identity = Session.Class?.gameplayIdentity ?? Session.Class?.role ?? "";
            ClassLabel.Set($"CLASS < {Truncate(Session.Class.name.ToUpperInvariant(), 15)} >");
            BackgroundLabel?.Set($"BG < {Session.Background.name.ToUpperInvariant()} >");

            ClassIdentityLabel?.Set(Truncate($"ID:{identity.ToUpperInvariant()}", 18));
            RaceFeatLabel?.Set(Truncate($"FT:{featName.ToUpperInvariant()}", 18));

            var sp = Session.StarterPassive;
            StarterPassiveLabel?.Set(Truncate($"PASSIVE: {(sp != null ? sp.name.ToUpperInvariant() : "NONE")}", 26));

            var eq = Session.GetEquipped(SelectedEquipmentSlot);
            string eqName = eq != null ? eq.name.ToUpperInvariant() : "EMPTY";
            EquipmentSlotLabel?.Set($"[{EquipmentSlots.ShortName(SelectedEquipmentSlot)}] < {Truncate(eqName, 15)} >");

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

            var activeRoster = Game != null ? Game.Roster : Roster;
            if (startBtn != null && activeRoster != null)
                startBtn.interactable = activeRoster.CanBeginExpedition;

            if (activeRoster != null && PartySummaryLabel != null)
            {
                var members = activeRoster.SelectedPartyIds
                    .Select((id, i) =>
                    {
                        var c = activeRoster.Characters.FirstOrDefault(x => x.id == id)
                                ?? (Database.Characters.TryGetValue(id, out var dc) ? dc : null);
                        string shortName = Truncate((c != null ? c.name : id).ToUpperInvariant(), 8);
                        return $"{i + 1}:{shortName}";
                    });
                PartySummaryLabel.Set(Truncate($"PARTY ({activeRoster.PartyCount}/{CharacterRoster.PartySize}): " + string.Join(" ", members), 50));
            }
        }
    }
}
