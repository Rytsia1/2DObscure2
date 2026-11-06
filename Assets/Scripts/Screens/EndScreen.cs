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
            if (!victory) { BuildDefeat(); return; }
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

            // Title block per FELNA — UI System (Page 35: Dungeon Clear / Page 36: Run Failure)
            Ui.Label(Root, victory ? "EXPEDITION COMPLETE" : "THE PARTY HAS FALLEN. THIS RUN HAS ENDED.", 320, 14, victory ? Palette.Gold2 : Palette.Mist1, Align.Center, font: FontKind.DotGothic);
            Ui.Label(Root, victory ? "THE ABYSS IS SEALED" : "YOU HAVE FALLEN", 320, 28, victory ? Palette.Gold1 : Palette.Hex(0xFF8A7A), Align.Center, 2, FontKind.DotGothic);
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

        // Starlit Reliquary "S · Run Failure (party wipe)": DEFEAT title, Lost (dashed ember, not committed)
        // vs Kept (gold, committed by earlier clears) panels, one gold "Return to Main Menu".
        void BuildDefeat()
        {
            Backdrop.Create(Root, false, 4, Palette.Hex(0x2A1016), Palette.Hex(0x0C0A14), Palette.Hex(0x05060C));
            int act = Run != null ? Run.Act : 1, floor = Run != null ? Run.Floor : 1;
            var s = Run != null ? Run.Stats : new RunStats();

            ResultArt.Title(Root, "DEFEAT", 320, 18, Palette.Hex(0xFF8A7A));
            Ui.Label(Root, "The party has fallen. This run has ended.", 320, 56, Palette.Mist1, Align.Center);
            Ui.Label(Root, $"FELL IN DUNGEON {act} OF 8 · NODE {floor} · PROGRESS FROM EARLIER CLEARS IS SAFE", 320, 71, Palette.Mist2, Align.Center, font: FontKind.Interface);

            // Lost: gained in this dungeon, not committed
            const int lx = 55, y = 86, w = 260, h = 196;
            Ui.Image(Root, PixelArt.DashedBadge(w, h, Palette.Hex(0x22120A, 180), Palette.Ember2), lx, y, Ui.Panels);
            int tx = lx + 8 + Ui.Tag(Root, "LOST", lx + 8, y + 7, TagKind.Temp, 12) + 6;
            Ui.Label(Root, $"Gained in Dungeon {act}", tx, y + 7, Palette.Ember1);
            Ui.Label(Root, "NOT COMMITTED", lx + w - 8, y + 9, Palette.Ember2, Align.Right, font: FontKind.Interface);
            for (int x = lx + 4; x < lx + w - 4; x += 4) Ui.Rect(Root, x, y + 24, 2, 1, Palette.Ember3, Ui.Panels + 1);
            int gw = Mathf.Max(84, PixelFont.Measure($"{s.goldEarned:#,##0} RUN GOLD", FontKind.DotGothic) + 16);
            Ui.CurrencyTag(Root, s.goldEarned, false, lx + 10, y + 34);
            Ui.Rect(Root, lx + 12, y + 42, gw - 4, 2, Palette.Ember1, Ui.Widget + 2); // struck through
            Ui.Label(Root, $"RUN GOLD HELD IN DUNGEON {act}.", lx + gw + 16, y + 39, Palette.Mist2, font: FontKind.Interface);
            int ly = y + 64;
            foreach (var line in new[] { $"EQUIPMENT FOUND IN DUNGEON {act}", "EXP AND LEVELS GAINED HERE", "SP GAINED HERE, AND SKILLS BOUGHT WITH IT", "CONSUMABLES, MATERIALS AND KEY ITEMS FOUND HERE" })
            {
                Ui.Image(Root, PixelArt.Diamond(5, Palette.Ember2), lx + 12, ly + 1, Ui.Text);
                Ui.Label(Root, line, lx + 22, ly, Palette.Mist1, font: FontKind.Interface);
                ly += 14;
            }

            // Kept: committed by earlier clears
            const int kx = 325;
            Ui.Panel(Root, kx, y, w, h, Palette.Hex(0x1D1608, 220), Palette.Gold3);
            tx = kx + 8 + Ui.Tag(Root, "KEPT", kx + 8, y + 7, TagKind.Keep, 12) + 6;
            Ui.Label(Root, act > 1 ? $"Committed by Dungeons 1-{act - 1}" : "Committed progress", tx, y + 7, Palette.Gold1);
            Ui.Rect(Root, kx + 2, y + 24, w - 4, 1, Palette.Gold4, Ui.Panels + 1);
            for (int i = 1; i <= 8; i++)
            {
                var pip = i < act ? PixelArt.Diamond(9, Palette.Gold2) : i == act ? PixelArt.Diamond(9, Palette.Ember2, false) : PixelArt.Diamond(9, Palette.Ink5, false);
                Ui.Image(Root, pip, kx + 10 + (i - 1) * 12, y + 34, Ui.Text);
            }
            Ui.Label(Root, $"{act - 1} / 8 cleared", kx + 112, y + 33, Palette.Gold1);
            Ui.Label(Root, PixelFont.Wrap($"DUNGEON PROGRESS STAYS. DUNGEON {act} IS YOUR NEXT DUNGEON AGAIN.", w - 20), kx + 10, y + 50, Palette.Mist2, font: FontKind.Interface);
            int hub = PersistenceCommitHandler.ActiveCommittedSnapshot?.hubGold ?? (Run != null ? Run.Gold : 0);
            Ui.CurrencyTag(Root, hub, true, kx + 10, y + 76);
            int rx = kx + 10 + Mathf.Max(84, PixelFont.Measure($"{hub:#,##0} HUB GOLD", FontKind.DotGothic) + 16) + 8;
            foreach (var id in (Run?.Relics ?? new System.Collections.Generic.List<string>()).Take(5))
            {
                if (!Database.Relics.TryGetValue(id, out var r)) continue;
                Ui.Relic(Root, rx, y + 75, 18, r);
                rx += 21;
            }
            Ui.Label(Root, PixelFont.Wrap("LEVELS, SKILLS, EQUIPMENT AND ITEMS FROM EARLIER CLEARS, PLUS OWNED RELICS.", w - 20), kx + 10, y + 102, Palette.Mist1, font: FontKind.Interface);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "Return to Main Menu", 320 - 80, 300, 160, () => Game.EndRun(), 24, style: ButtonStyle.Primary));
            Ui.Label(Root, $"FROM THE MAIN MENU, START A NEW EXPEDITION INTO DUNGEON {act} OR LOAD A SAVE.", 320, 332, Palette.Mist2, Align.Center, font: FontKind.Interface);
        }
    }
}
