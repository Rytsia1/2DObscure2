using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Run summary for Victory and Game Over. The finished run stays readable until the player starts another.
    // Starlit Reliquary "R · Dungeon Clear" (victory) and "S · Run Failure" (defeat) boards at 640x360:
    // big centered title, dungeon-progress diamonds, stat tiles, detail rows, one gold primary action.
    public class EndScreen : GameScreen
    {
        readonly bool victory;
        public EndScreen() : this(true) { }
        public EndScreen(bool victory) { this.victory = victory; }

        protected override void Build()
        {
            if (victory)
            {
                Backdrop.Create(Root, true, 24);
                Ui.Image(Root, PixelArt.Ridge(Ui.Width, 80, Palette.Hex(0x1A2146), 3), 0, 280, Ui.Back + 5);
                Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 140), Ui.Back + 6);
            }
            else Backdrop.Create(Root, false, 4, Palette.Hex(0x2A1016), Palette.Hex(0x0C0A14), Palette.Hex(0x05060C));

            var s = Run != null ? Run.Stats : new RunStats();
            int act = Run != null ? Run.Act : 1;
            int floor = Run != null ? Run.Floor : 1;
            int floors = Run?.Map != null ? Run.Map.Floors : 8;

            // Title block
            Ui.Label(Root, victory ? "EXPEDITION COMPLETE" : "THE PARTY HAS FALLEN. THIS RUN HAS ENDED.", 320, 14, victory ? Palette.Gold2 : Palette.Mist1, Align.Center);
            Ui.Label(Root, victory ? "THE ABYSS IS SEALED" : "YOU HAVE FALLEN", 320, 28, victory ? Palette.Gold1 : Palette.Hex(0xFF8A7A), Align.Center, 2, FontKind.Serif);
            int px = 320 - floors * 6;
            for (int i = 0; i < floors; i++)
            {
                bool cleared = victory || i < floor - 1;
                var pip = cleared ? PixelArt.Diamond(9, victory ? Palette.Gold2 : Palette.Ember2) : PixelArt.Diamond(9, victory ? Palette.Ink5 : Palette.Ember3, false);
                Ui.Image(Root, pip, px + i * 12, 58, Ui.Text);
            }

            // Stat tiles
            var tiles = new (string label, string value, string note)[]
            {
                ("BATTLES", s.battlesWon.ToString(), "WON"),
                ("ELITES", s.elitesWon.ToString(), "VANQUISHED"),
                ("CHAMBERS", s.nodesCompleted.ToString(), "CLEARED"),
                ("GOLD", s.goldEarned.ToString(), "EARNED"),
            };
            for (int i = 0; i < tiles.Length; i++)
            {
                int tx = 90 + i * 118;
                Ui.Panel(Root, tx, 78, 110, 46, Palette.Ink2, victory ? Palette.Gold3 : Palette.Ink5);
                Ui.Label(Root, tiles[i].label, tx + 8, 84, Palette.Mist3);
                Ui.Label(Root, tiles[i].value, tx + 8, 95, Palette.Mist1, Align.Left, 2);
                Ui.Label(Root, tiles[i].note, tx + 102, 111, Palette.Mist3, Align.Right);
            }

            // Detail rows
            string boss = s.bossDefeated ? "DEFEATED" : s.bossReached ? "FELL IN BATTLE" : "NOT REACHED";
            string relics = Run == null || Run.Relics.Count == 0 ? "NONE" : string.Join(", ", Run.Relics.Select(id => Database.Relics[id].name.ToUpperInvariant()));
            var duration = Run != null ? Run.Duration : System.TimeSpan.Zero;
            var lines = new (string label, string value)[]
            {
                ("EXPEDITION RESULT", victory ? "VICTORY" : "DEFEAT"),
                ("ACT / FLOOR", $"{act} / {floor} OF {floors}"),
                ("ABYSSAL WARDEN", boss),
                ("EXPEDITION DURATION", $"{(int)duration.TotalMinutes}:{duration.Seconds:00}"),
                ("EXPEDITION SEED", Run != null ? Run.RunId : "DEMO"),
            };

            Ui.Panel(Root, 90, 132, 460, 150);
            Ui.Header(Root, victory ? "EXPEDITION SUMMARY" : "WHAT THIS RUN LEAVES BEHIND", 104, 140, 448);
            for (int i = 0; i < lines.Length; i++)
            {
                int ly = 160 + i * 16;
                Ui.Label(Root, lines[i].label, 106, ly, Palette.Mist3);
                Color valColor = i == 0 ? (victory ? Palette.Gold1 : Palette.HpLow) : Palette.Mist1;
                Ui.Label(Root, lines[i].value, 534, ly, valColor, Align.Right);
                Ui.Rect(Root, 98, ly + 11, 444, 1, Palette.Ink3);
            }
            Ui.Label(Root, PixelFont.Fit("RELICS: " + relics, 428), 106, 244, Palette.ArcaneViolet);
            if (!victory) Ui.Label(Root, "RUN GOLD AND DUNGEON PROGRESS ARE LOST.", 106, 260, Palette.Ember1);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "NEW RUN", 190, 300, 130, () => Game.StartRun(), 24, style: ButtonStyle.Primary));
            menu.Add(Ui.Button(Root, "TITLE", 330, 300, 120, () => Game.EndRun(), 24));
            Ui.Label(Root, victory ? "START A FRESH EXPEDITION OR RETURN TO THE TITLE." : "CHOOSE NEW RUN TO BEGIN AGAIN FROM THE FIRST FLOOR.", 320, 334, Palette.Mist2, Align.Center);
        }
    }
}
