using System;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Rest site: Recover (heal part of missing HP/MP) or Improve one hero. One choice per site.
    // Conforming to 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers.
    public class RestScreen : GameScreen
    {
        Transform phase;
        Label hint;
        readonly Label[] hpRows = new Label[4];
        readonly SpriteRenderer[] hpGauges = new SpriteRenderer[4];

        protected override void Build()
        {
            Backdrop.Create(Root, false, 32, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.SpecCrimson);

            Ui.Label(Root, "CAMPFIRE SANCTUARY", 320, 18, Palette.AntiqueGoldBright, Align.Center, 2, FontKind.Serif);
            Ui.Divider(Root, 160, 42, 320, Palette.DarkBorder, Palette.AntiqueGoldBright);
            Ui.Image(Root, PixelArt.FromRows(Art.IconRest), 305, 52, Ui.World).transform.localScale = new Vector3(3, 3, 1);

            // Left panel (270x190): Party Vitality
            Ui.Panel(Root, 28, 108, 274, 192, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, "PARTY CONDITION", 38, 114, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(Root, 38, 126, 254, Palette.DarkBorder, Palette.AntiqueGoldDim);

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < hpRows.Length && i < party.Count; i++)
            {
                var u = party[i];
                int y = 136 + i * 38;
                Ui.Label(Root, PixelFont.Fit(u.name.ToUpperInvariant(), 130), 40, y, u.Alive ? Palette.WarmIvory : Palette.SpecCrimson, Align.Left, 1, FontKind.Serif);
                hpRows[i] = Ui.Label(Root, "", 290, y, Palette.WarmIvory, Align.Right);
                hpGauges[i] = Ui.Image(Root, PixelArt.GaugeBar(250, 7, Palette.Emerald, (float)u.hp / Mathf.Max(1, u.stats.maxHp)), 40, y + 16, Ui.Widget);
            }

            // Right panel (274x192): Actions & Options
            Ui.Panel(Root, 338, 108, 274, 192, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Label(Root, "CAMP ACTIONS", 348, 114, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(Root, 348, 126, 254, Palette.DarkBorder, Palette.AntiqueGoldDim);

            hint = Ui.Label(Root, "", 320, 318, Palette.WarmIvory, Align.Center);
            ShowParty();
            ShowMain();
        }

        void ShowParty()
        {
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < hpRows.Length && i < party.Count; i++)
            {
                var u = party[i];
                if (hpRows[i] != null) hpRows[i].Set($"HP {u.hp}/{u.stats.maxHp}");
                if (hpGauges[i] != null) hpGauges[i].sprite = PixelArt.GaugeBar(250, 7, Palette.Emerald, (float)u.hp / Mathf.Max(1, u.stats.maxHp));
            }
        }

        Menu NewMenu()
        {
            if (phase != null) UnityEngine.Object.Destroy(phase.gameObject);
            phase = new GameObject("Phase").transform;
            phase.SetParent(Root, false);
            var m = phase.gameObject.AddComponent<Menu>();
            m.onFocus = s => hint.Set(s.tooltip ?? "");
            return m;
        }

        void Option(Menu m, string text, Action onClick, string tooltip)
        {
            var b = Ui.Button(phase, text, 348, 136 + m.items.Count * 36, 254, onClick, 24);
            b.tooltip = tooltip;
            m.Add(b);
        }

        void ShowMain()
        {
            var m = NewMenu();
            int total = Run != null ? Run.Party.Sum(RunState.RecoverAmount) : 0;
            Option(m, $"RECOVER +{total} HP", Recover, $"HEAL {(int)(RunConfig.RestRecover * 100)}% OF MISSING HP AND MP ACROSS ALL HEROES");
            Option(m, "IMPROVE A HERO", ShowImprove, "HONOR ONE HERO WITH A PERMANENT ATTRIBUTE OR MASTERY UPGRADE");
        }

        void ShowImprove()
        {
            var m = NewMenu();
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            foreach (var u in party)
            {
                var up = RunState.GetUpgradeFor(u);
                Option(m, u.name.ToUpperInvariant(), () => Confirm(u), $"{up.name.ToUpperInvariant()}: {u.stats.Preview(up.bonus)}");
            }
            Option(m, "BACK", ShowMain, "");
            m.onBack = ShowMain;
        }

        void Confirm(Unit u)
        {
            var up = RunState.GetUpgradeFor(u);
            string preview = $"{up.name.ToUpperInvariant()}: {u.stats.Preview(up.bonus)}";
            var m = NewMenu();
            Option(m, $"CONFIRM {u.name.ToUpperInvariant()}", () => { Run.RestImprove(u.id); Done($"{u.name.ToUpperInvariant()} LEARNS {up.name.ToUpperInvariant()}."); }, preview);
            Option(m, "BACK", ShowImprove, preview);
            m.onBack = ShowImprove;
        }

        void Recover()
        {
            Run.RestRecover();
            Done("THE PARTY RESTS BY THE HEARTH, STRENGTH RESTORED.");
        }

        void Done(string text)
        {
            Sfx.Play(Sound.Success);
            ShowParty();
            var m = NewMenu();
            Option(m, "CONTINUE EXPEDITION", () => Game.Go(GameState.Map), text);
        }
    }
}
