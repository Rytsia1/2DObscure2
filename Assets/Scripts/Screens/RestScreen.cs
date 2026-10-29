using System;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Rest site: Recover (heal part of missing HP/MP) or Improve one hero. One choice per site.
    public class RestScreen : GameScreen
    {
        Transform phase;
        Label hint;
        readonly Label[] hpRows = new Label[4];

        protected override void Build()
        {
            Backdrop.Create(Root, false, 30, Palette.Void, Palette.Night, Palette.Blood);
            Ui.Label(Root, "CAMPFIRE", 160, 14, Palette.Gold, Align.Center, 2);
            Ui.Image(Root, PixelArt.FromRows(Art.IconRest), 146, 36, Ui.World).transform.localScale = new Vector3(3, 3, 1);

            Ui.Panel(Root, 16, 72, 128, 66);
            for (int i = 0; i < hpRows.Length; i++)
            {
                Ui.Label(Root, Run.Party[i].name.ToUpper(), 24, 79 + i * 14, Palette.Bone);
                hpRows[i] = Ui.Label(Root, "", 136, 79 + i * 14, Palette.Parchment, Align.Right);
            }
            hint = Ui.Label(Root, "", 160, 150, Palette.Ash, Align.Center);
            ShowParty();
            ShowMain();
        }

        void ShowParty()
        {
            for (int i = 0; i < hpRows.Length; i++) hpRows[i].Set($"{Run.Party[i].hp}/{Run.Party[i].stats.maxHp}");
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
            var b = Ui.Button(phase, text, 158, 76 + m.items.Count * 15, 146, onClick);
            b.tooltip = tooltip;
            m.Add(b);
        }

        void ShowMain()
        {
            var m = NewMenu();
            int total = Run.Party.Sum(RunState.RecoverAmount);
            Option(m, $"RECOVER +{total} HP", Recover, $"HEAL {(int)(RunConfig.RestRecover * 100)}% OF MISSING HP AND MP");
            Option(m, "IMPROVE A HERO", ShowImprove, "A PERMANENT UPGRADE FOR ONE CHARACTER");
        }

        void ShowImprove()
        {
            var m = NewMenu();
            foreach (var u in Run.Party)
            {
                var up = RunState.GetUpgradeFor(u);
                Option(m, u.name.ToUpper(), () => Confirm(u), $"{up.name.ToUpper()}: {u.stats.Preview(up.bonus)}");
            }
            Option(m, "BACK", ShowMain, "");
            m.onBack = ShowMain;
        }

        // Shows exactly what changes before it is applied.
        void Confirm(Unit u)
        {
            var up = RunState.GetUpgradeFor(u);
            string preview = $"{up.name.ToUpper()}: {u.stats.Preview(up.bonus)}";
            var m = NewMenu();
            Option(m, $"CONFIRM {u.name.ToUpper()}", () => { Run.RestImprove(u.id); Done($"{u.name.ToUpper()} LEARNS {up.name.ToUpper()}."); }, preview);
            Option(m, "BACK", ShowImprove, preview);
            m.onBack = ShowImprove;
        }

        void Recover()
        {
            Run.RestRecover();
            Done("THE PARTY RESTS BY THE FIRE.");
        }

        void Done(string text)
        {
            ShowParty();
            var m = NewMenu();
            Option(m, "CONTINUE", () => Game.Go(GameState.Map), text);
        }
    }
}
