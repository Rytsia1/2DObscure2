using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Character Status Screen & Stat Detail Tooltip viewer (PRD Section 12.3 & 12.4).
    // Displays all 6 Primary Stats (Score, Modifier, Sources), all required Derived Stats, and detailed calculation tooltips.
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

        SpriteRenderer portrait;
        Label vitals;

        // The 15 Derived Stats required on the Character Status Screen (Prompt Section 9 / PRD Section 12.3), plus Magic Break.
        public static readonly DerivedStatId[] DisplayedDerivedStats =
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
            DerivedStatId.MagicBreakPower,
            DerivedStatId.StatusResistance,
            DerivedStatId.QteTimingBonus,
        };

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

            returnState = backTo ?? (Game.I?.Run != null ? GameState.Map : GameState.MainMenu);
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 6, Palette.Void, Palette.Night, Palette.Dusk);

            // Top bar: Hero selector & Class/Race/Background summary
            Ui.Panel(Root, 0, 0, Ui.Width, 18);
            HeaderLabel = Ui.Label(Root, "", 6, 5, Palette.Gold);
            var gearBtn = Ui.Button(Root, "GEAR", 175, 2, 40, () => Game?.Go(GameState.Equipment), 14, showCursor: false);
            gearBtn.tooltip = "VIEW & MODIFY HERO EQUIPMENT LOADOUT";
            var itemsBtn = Ui.Button(Root, "ITEMS", 220, 2, 44, () => Game?.Go(GameState.Inventory), 14, showCursor: false);
            itemsBtn.tooltip = "INVENTORY (EQUIPMENT, CONSUMABLES, MATERIALS, KEYS)";
            IdentityLabel = Ui.Label(Root, "", 314, 5, Palette.Parchment, Align.Right);

            // Left panel: 6 Primary Ability Scores
            Ui.Panel(Root, 2, 19, 108, 108);
            Ui.Label(Root, "PRIMARY STATS", 10, 23, Palette.Gold);

            // Right panel: 16 Derived Stats in two columns
            Ui.Panel(Root, 112, 19, 206, 108);
            Ui.Label(Root, "DERIVED STATS", 120, 23, Palette.Gold);
            vitals = Ui.Label(Root, "", 312, 23, Palette.Parchment, Align.Right);

            // Bottom panel: Portrait + 4-line Stat Detail Tooltip
            Ui.Panel(Root, 0, 128, Ui.Width, 52);
            portrait = Ui.Image(Root, PixelArt.FromRows(CurrentUnit.sprite), 8, 136, Ui.Widget);
            TooltipLabel = Ui.Label(Root, "", 28, 132, Palette.Bone);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(gearBtn);
            menu.Add(itemsBtn);
            menu.onFocus = s => TooltipLabel.Set(PixelFont.Wrap(s?.tooltip ?? "", 284));
            menu.onBack = () => Game?.Go(returnState);

            // Hero switch button (spaced so '>' cursor never overlaps border or adjacent button)
            var switchBtn = Ui.Button(Root, "NEXT", 10, 112, 44, () => SwitchHero(1), 12);
            switchBtn.onAdjust = SwitchHero;
            switchBtn.tooltip = "LEFT/RIGHT OR CLICK: INSPECT ANOTHER PARTY MEMBER";
            menu.Add(switchBtn);

            var backBtn = Ui.Button(Root, "BACK", 64, 112, 42, () => Game?.Go(returnState), 12);
            backBtn.tooltip = "RETURN TO PREVIOUS SCREEN";
            menu.Add(backBtn);

            // Primary Ability Score interactive rows (x=10 keeps '>' cursor at x=3 inside left panel border)
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var a = AbilityScores.AllAbilities[i];
                int y = 34 + i * 12;
                var btn = Ui.Button(Root, "", 10, y, 96, () => TooltipLabel.Set(PixelFont.Wrap(CurrentUnit.ExplainAbility(a), 284)), 11);
                PrimaryStatLabels[a] = btn.label;
                menu.Add(btn);
            }

            // Derived Stat interactive rows (2 columns x 8 rows, with 10px gap between columns for '>' cursor)
            for (int i = 0; i < DisplayedDerivedStats.Length; i++)
            {
                var st = DisplayedDerivedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 121 + col * 100;
                int y = 34 + row * 11;
                var btn = Ui.Button(Root, "", x, y, 90, () => ShowStatTooltip(st), 10);
                DerivedStatLabels[st] = btn.label;
                menu.Add(btn);
            }

            Refresh();
        }

        public void SwitchHero(int delta)
        {
            currentIndex = (currentIndex + delta + units.Count) % units.Count;
            Refresh();
        }

        public string GetStatTooltip(DerivedStatId st) =>
            CurrentUnit.ExplainStat(st).FormatTooltip();

        public string GetEquipmentSummary()
        {
            var u = CurrentUnit;
            var parts = EquipmentSlots.All.Select(slot =>
            {
                string itemName = u.equipped.TryGetValue(slot, out var eq) && eq != null ? eq.name : "Empty";
                return $"{EquipmentSlots.ShortName(slot)}:{itemName}";
            });
            return string.Join(" | ", parts);
        }

        void ShowStatTooltip(DerivedStatId st) =>
            TooltipLabel.Set(PixelFont.Wrap(GetStatTooltip(st), 284));

        public void Refresh()
        {
            var u = CurrentUnit;
            u.RecalculateStats();

            HeaderLabel.Set(PixelFont.Fit($"{u.name ?? "HERO"} LV{u.level}".ToUpperInvariant(), 140));
            vitals.Set($"HP {u.hp}/{u.stats.maxHp}  MP {u.mp}/{u.stats.maxMp}");
            string raceName = u.race?.name ?? "Human";
            string className = u.classDef?.name ?? u.role;
            IdentityLabel.Set(PixelFont.Fit($"{raceName} {className}".ToUpperInvariant(), 160));

            if (portrait != null && u.sprite != null)
                portrait.sprite = PixelArt.FromRows(u.sprite);

            var menu = Root.GetComponent<Menu>();

            foreach (var a in AbilityScores.AllAbilities)
            {
                int score = u.Score(a);
                int mod = u.Mod(a);
                int bonus = score - u.BaseScores[a];
                string bonusTag = bonus != 0 ? $"({(bonus > 0 ? "+" : "")}{bonus})" : "";
                PrimaryStatLabels[a].Set($"{a} {score,2} {AbilityScores.FormatMod(mod),2} {bonusTag}");

                var btn = PrimaryStatLabels[a].GetComponentInParent<Selectable>();
                if (btn != null) btn.tooltip = u.ExplainAbility(a);
            }

            foreach (var st in DisplayedDerivedStats)
            {
                var def = Database.DerivedStatDefs[st];
                DerivedStatLabels[st].Set($"{def.shortName,-4} {u.Derived.FormatValue(st)}");
                var btn = DerivedStatLabels[st].GetComponentInParent<Selectable>();
                if (btn != null) btn.tooltip = GetStatTooltip(st);
            }

            if (menu?.Current != null)
                TooltipLabel.Set(PixelFont.Wrap(menu.Current.tooltip ?? "", 284));
        }
    }
}

