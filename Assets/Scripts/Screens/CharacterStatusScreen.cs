using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Character Status Screen & Stat Detail Tooltip viewer conforming to UI/UX Overhaul Specification
    // 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers, Tabular Interface Readouts.
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

            returnState = backTo ?? (Game.I?.Run != null ? GameState.Map : GameState.MainMenu);
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // 1. Top bar: Hero Selector & Class/Race summary, plus GEAR & ITEMS shortcuts
            Ui.Panel(Root, 0, 0, Ui.Width, 26, Palette.DeepCharcoal, Palette.AntiqueGold);
            HeaderLabel = Ui.Label(Root, "", 14, 7, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            IdentityLabel = Ui.Label(Root, "", 120, 7, Palette.WarmIvory);
            vitals = Ui.Label(Root, "", 370, 7, Palette.MutedGray);

            var gearBtn = Ui.Button(Root, "LOADOUT", 480, 4, 72, () => Game?.Go(GameState.Equipment), 18, showCursor: false);
            gearBtn.tooltip = "VIEW & MODIFY HERO EQUIPMENT LOADOUT";
            var itemsBtn = Ui.Button(Root, "SATCHEL", 558, 4, 72, () => Game?.Go(GameState.Inventory), 18, showCursor: false);
            itemsBtn.tooltip = "INVENTORY (EQUIPMENT, CONSUMABLES, MATERIALS, KEYS)";

            // 2. Left Panel: 6 Primary Ability Scores
            Ui.Panel(Root, 12, 28, 192, 222, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, "PRIMARY STATS", 22, 33, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(Root, 22, 45, 172, Palette.DarkBorder, Palette.AntiqueGoldDim);

            // 3. Right Panel: 16 Derived Stats in two columns
            Ui.Panel(Root, 208, 28, 420, 222, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, "DERIVED ATTRIBUTES", 218, 33, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(Root, 218, 45, 400, Palette.DarkBorder, Palette.AntiqueGoldDim);

            // 4. Bottom Panel: Portrait + Detailed Formula Tooltip Card
            Ui.Panel(Root, 0, 254, Ui.Width, 106, Palette.DeepCharcoal, Palette.AntiqueGold);

            // Portrait well (38x44)
            Ui.Image(Root, PixelArt.Box(40, 46, Palette.DeepCharcoal, Palette.AntiqueGoldDim), 14, 260, Ui.Panels + 1);
            portrait = Ui.Image(Root, PixelArt.FromRows(CurrentUnit.sprite), 18, 264, Ui.Widget);

            // Tooltip card
            Ui.Image(Root, PixelArt.Box(564, 62, Palette.MidnightNavy, Palette.DarkBorder), 62, 260, Ui.Panels + 1);
            TooltipLabel = Ui.Label(Root, "", 72, 266, Palette.WarmIvory);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => TooltipLabel.Set(PixelFont.Wrap(s?.tooltip ?? "", 544));
            menu.onBack = () => Game?.Go(returnState);

            var switchBtn = Ui.Button(Root, "NEXT HERO", 62, 328, 92, () => SwitchHero(1), 22);
            switchBtn.onAdjust = SwitchHero;
            switchBtn.tooltip = "LEFT/RIGHT OR CLICK: INSPECT ANOTHER PARTY MEMBER";
            menu.Add(switchBtn);

            var backBtn = Ui.Button(Root, "BACK", 552, 328, 74, () => Game?.Go(returnState), 22);
            backBtn.tooltip = "RETURN TO PREVIOUS SCREEN";
            menu.Add(backBtn);

            menu.Add(gearBtn);
            menu.Add(itemsBtn);

            // Primary Ability Score interactive rows
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var a = AbilityScores.AllAbilities[i];
                int y = 52 + i * 28;
                var btn = Ui.Button(Root, "", 20, y, 176, () => TooltipLabel.Set(PixelFont.Wrap(CurrentUnit.ExplainAbility(a), 544)), 22);
                PrimaryStatLabels[a] = btn.label;
                menu.Add(btn);
            }

            // Derived Stat interactive rows (2 columns x 8 rows)
            for (int i = 0; i < DisplayedDerivedStats.Length; i++)
            {
                var st = DisplayedDerivedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 216 + col * 202;
                int y = 52 + row * 21;
                var btn = Ui.Button(Root, "", x, y, 196, () => ShowStatTooltip(st), 19);
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
            TooltipLabel.Set(PixelFont.Wrap(GetStatTooltip(st), 544));

        public void Refresh()
        {
            var u = CurrentUnit;
            u.RecalculateStats();

            HeaderLabel.Set(PixelFont.Fit($"{u.name ?? "HERO"} LV{u.level}".ToUpperInvariant(), 84));
            vitals.Set($"HP: {u.hp}/{u.stats.maxHp}   MP: {u.mp}/{u.stats.maxMp}");
            string raceName = u.race?.name ?? "Human";
            string className = u.classDef?.name ?? u.role;
            IdentityLabel.Set(PixelFont.Fit($"{raceName} {className}".ToUpperInvariant(), 125));

            if (portrait != null && u.sprite != null)
                portrait.sprite = PixelArt.FromRows(u.sprite);

            var menu = Root.GetComponent<Menu>();

            foreach (var a in AbilityScores.AllAbilities)
            {
                int score = u.Score(a);
                int mod = u.Mod(a);
                int bonus = score - u.BaseScores[a];
                string bonusTag = bonus != 0 ? $"({(bonus > 0 ? "+" : "")}{bonus})" : "";
                PrimaryStatLabels[a].Set($"{a} {score,2}  {AbilityScores.FormatMod(mod),2}  {bonusTag}");

                var btn = PrimaryStatLabels[a].GetComponentInParent<Selectable>();
                if (btn != null) btn.tooltip = u.ExplainAbility(a);
            }

            foreach (var st in DisplayedDerivedStats)
            {
                var def = Database.DerivedStatDefs[st];
                DerivedStatLabels[st].Set($"{def.shortName,-4} : {u.Derived.FormatValue(st)}");
                var btn = DerivedStatLabels[st].GetComponentInParent<Selectable>();
                if (btn != null) btn.tooltip = GetStatTooltip(st);
            }

            if (menu?.Current != null)
                TooltipLabel.Set(PixelFont.Wrap(menu.Current.tooltip ?? "", 544));
        }
    }
}
