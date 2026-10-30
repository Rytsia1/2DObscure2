using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Run summary for Victory and Game Over. The finished run stays readable until the player starts another.
    // Conforming to 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers.
    public class EndScreen : GameScreen
    {
        readonly bool victory;
        public EndScreen() : this(true) { }
        public EndScreen(bool victory) { this.victory = victory; }

        protected override void Build()
        {
            if (victory) Backdrop.Create(Root, false, 24, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk, Palette.ArcaneViolet);
            else Backdrop.Create(Root, false, 4, Palette.DeepCharcoal, Palette.DeepCharcoal, Palette.SpecCrimson);

            Ui.Label(Root, victory ? "THE ABYSS IS SEALED" : "YOU HAVE FALLEN", 320, 18, victory ? Palette.AntiqueGoldBright : Palette.SpecCrimson, Align.Center, 2, FontKind.Serif);
            Ui.Divider(Root, 140, 42, 360, victory ? Palette.AntiqueGoldDim : Palette.SpecCrimson, victory ? Palette.AntiqueGoldBright : Palette.SpecCrimson);

            var s = Run != null ? Run.Stats : new RunStats();
            string boss = s.bossDefeated ? "DEFEATED" : s.bossReached ? "FELL IN BATTLE" : "NOT REACHED";
            string relics = Run == null || Run.Relics.Count == 0 ? "NONE" : string.Join(", ", Run.Relics.Select(id => Database.Relics[id].name.ToUpperInvariant()));
            int act = Run != null ? Run.Act : 1;
            int floor = Run != null ? Run.Floor : 1;
            int floors = Run?.Map != null ? Run.Map.Floors : 8;
            string runId = Run != null ? Run.RunId : "DEMO";
            var duration = Run != null ? Run.Duration : System.TimeSpan.Zero;
            var lines = new (string label, string value)[]
            {
                ("EXPEDITION RESULT", victory ? "VICTORY" : "DEFEAT"),
                ("ACT / FLOOR", $"{act} / {floor} OF {floors}"),
                ("CHAMBERS CLEARED", s.nodesCompleted.ToString()),
                ("BATTLES WON", s.battlesWon.ToString()),
                ("ELITES VANQUISHED", s.elitesWon.ToString()),
                ("ABYSSAL WARDEN", boss),
                ("TOTAL GOLD EARNED", s.goldEarned.ToString()),
                ("EXPEDITION DURATION", $"{(int)duration.TotalMinutes}:{duration.Seconds:00}"),
                ("EXPEDITION SEED", runId),
            };

            Ui.Panel(Root, 110, 52, 420, 248, Palette.DeepCharcoal, victory ? Palette.AntiqueGold : Palette.DarkBorder);
            for (int i = 0; i < lines.Length; i++)
            {
                Ui.Label(Root, lines[i].label, 130, 64 + i * 18, Palette.MutedGray);
                Color valColor = i == 0 ? (victory ? Palette.AntiqueGoldBright : Palette.SpecCrimson) : Palette.WarmIvory;
                Ui.Label(Root, lines[i].value, 510, 64 + i * 18, valColor, Align.Right);
            }
            Ui.Divider(Root, 130, 230, 380, Palette.DarkBorder, Palette.AntiqueGoldDim);
            Ui.Label(Root, PixelFont.Wrap("RELICS COLLECTED: " + relics, 380), 130, 238, Palette.ArcaneViolet);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "NEW RUN", 180, 312, 130, () => Game.StartRun(), 24));
            menu.Add(Ui.Button(Root, "TITLE", 330, 312, 130, () => Game.EndRun(), 24));
        }
    }
}
