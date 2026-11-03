using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Character Status Screen & Stat Detail Tooltip viewer.
    // Starlit Reliquary "E · Character Status" board at 640x360: identity column | attributes | derived stats, tooltip card below.
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

        const int TipWidth = 448;
        SpriteRenderer portrait, hpBar, mpBar;
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
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);

            // 1. Header: title + service shortcuts
            Ui.Label(Root, "STATUS", 12, 10, Palette.Gold2, Align.Left, 2, FontKind.Serif);
            var skillsBtn = Ui.Button(Root, "SKILLS", 400, 8, 72, () => Game?.Go(GameState.Skills), 20, showCursor: false);
            skillsBtn.tooltip = "VIEW & UPGRADE SKILL TREES AND SP";
            var gearBtn = Ui.Button(Root, "LOADOUT", 478, 8, 72, () => Game?.Go(GameState.Equipment), 20, showCursor: false);
            gearBtn.tooltip = "VIEW & MODIFY HERO EQUIPMENT LOADOUT";
            var itemsBtn = Ui.Button(Root, "SATCHEL", 556, 8, 72, () => Game?.Go(GameState.Inventory), 20, showCursor: false);
            itemsBtn.tooltip = "INVENTORY (EQUIPMENT, CONSUMABLES, MATERIALS, KEYS)";

            // 2. Identity column (gold frame): portrait, name, race/class, vitals
            Ui.Panel(Root, 12, 36, 140, 290, Palette.Ink2, Palette.Gold3);
            Ui.Image(Root, PixelArt.Gradient(136, 100, Palette.Hex(0x16204A), Palette.Hex(0x0C1122)), 14, 38, Ui.Panels + 1);
            Ui.Rect(Root, 14, 138, 136, 1, Palette.Ink4);
            portrait = Ui.Image(Root, PixelArt.FromRows(CurrentUnit.sprite), 62, 84, Ui.Widget);
            portrait.transform.localScale = new Vector3(2, 2, 1);
            HeaderLabel = Ui.Label(Root, "", 20, 146, Palette.Mist1, Align.Left, 1, FontKind.Serif);
            IdentityLabel = Ui.Label(Root, "", 20, 162, Palette.Mist2);
            Ui.Label(Root, "HP", 20, 180, Palette.Mist3);
            hpBar = Ui.Image(Root, null, 36, 180, Ui.Widget);
            Ui.Label(Root, "MP", 20, 194, Palette.Mist3);
            mpBar = Ui.Image(Root, null, 36, 194, Ui.Widget);
            vitals = Ui.Label(Root, "", 20, 208, Palette.Mist1);

            // 3. Attributes panel: 6 primary ability scores
            Ui.Panel(Root, 160, 36, 150, 200);
            Ui.Header(Root, "ATTRIBUTES", 174, 44, 138);

            // 4. Derived combat stats panel: 16 stats in two columns
            Ui.Panel(Root, 316, 36, 312, 200);
            Ui.Header(Root, "DERIVED COMBAT STATS", 330, 44, 300);

            // 5. Tooltip card (.tip): formula / explanation for the focused row
            Ui.Image(Root, PixelArt.Box(468, 84, Palette.Ink2, Palette.Mist3), 160, 242, Ui.Panels + 1);
            TooltipLabel = Ui.Label(Root, "", 170, 250, Palette.Mist2);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => TooltipLabel.Set(PixelFont.Wrap(s?.tooltip ?? "", TipWidth));
            menu.onBack = () => Game?.Go(returnState);

            var switchBtn = Ui.Button(Root, "NEXT HERO", 20, 296, 124, () => SwitchHero(1), 22);
            switchBtn.onAdjust = SwitchHero;
            switchBtn.tooltip = "LEFT/RIGHT OR CLICK: INSPECT ANOTHER PARTY MEMBER";
            menu.Add(switchBtn);

            var backBtn = Ui.Button(Root, "BACK", 548, 332, 80, () => Game?.Go(returnState), 22);
            backBtn.tooltip = "RETURN TO PREVIOUS SCREEN";
            menu.Add(backBtn);

            menu.Add(gearBtn);
            menu.Add(itemsBtn);

            // Primary Ability Score interactive rows
            for (int i = 0; i < AbilityScores.AllAbilities.Length; i++)
            {
                var a = AbilityScores.AllAbilities[i];
                int y = 62 + i * 27;
                var btn = Ui.Button(Root, "", 174, y, 128, () => TooltipLabel.Set(PixelFont.Wrap(CurrentUnit.ExplainAbility(a), TipWidth)), 22);
                PrimaryStatLabels[a] = btn.label;
                menu.Add(btn);
            }

            // Derived Stat interactive rows (2 columns x 8 rows)
            for (int i = 0; i < DisplayedDerivedStats.Length; i++)
            {
                var st = DisplayedDerivedStats[i];
                int col = i / 8;
                int row = i % 8;
                int x = 328 + col * 150;
                int y = 62 + row * 21;
                var btn = Ui.Button(Root, "", x, y, 146, () => ShowStatTooltip(st), 19);
                DerivedStatLabels[st] = btn.label;
                menu.Add(btn);
            }

            int hx = Ui.KeyHint(Root, 160, 337, "HERO", "LEFT", "RIGHT");
            Ui.KeyHint(Root, hx, 337, "BACK", "ESC");

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
            TooltipLabel.Set(PixelFont.Wrap(GetStatTooltip(st), TipWidth));

        public void Refresh()
        {
            var u = CurrentUnit;
            u.RecalculateStats();

            HeaderLabel.Set(PixelFont.Fit($"{u.name ?? "HERO"} LV{u.level}".ToUpperInvariant(), 84));
            vitals.Set($"{u.hp}/{u.stats.maxHp} HP   {u.mp}/{u.stats.maxMp} MP");
            float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp);
            hpBar.sprite = PixelArt.GaugeBar(108, 7, hp < 0.25f ? Palette.HpLow : Palette.Hp, hp);
            mpBar.sprite = PixelArt.GaugeBar(108, 7, Palette.Mp, (float)u.mp / Mathf.Max(1, u.stats.maxMp));
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
                TooltipLabel.Set(PixelFont.Wrap(menu.Current.tooltip ?? "", TipWidth));
        }
    }
}
