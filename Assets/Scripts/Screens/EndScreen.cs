using System.Linq;

namespace Obscure
{
    // Run summary for Victory and Game Over. The finished run stays readable until the player starts another.
    public class EndScreen : GameScreen
    {
        readonly bool victory;
        public EndScreen(bool victory) { this.victory = victory; }

        protected override void Build()
        {
            if (victory) Backdrop.Create(Root, false, 24, Palette.Void, Palette.Night, Palette.Dusk, Palette.Arcane);
            else Backdrop.Create(Root, false, 4, Palette.Void, Palette.Void, Palette.Blood);

            Ui.Label(Root, victory ? "THE ABYSS IS SEALED" : "YOU HAVE FALLEN", 160, 12, victory ? Palette.Gold : Palette.Crimson, Align.Center, 2);

            var s = Run.Stats;
            string boss = s.bossDefeated ? "DEFEATED" : s.bossReached ? "FELL TO IT" : "NOT REACHED";
            string relics = Run.Relics.Count == 0 ? "NONE" : string.Join(", ", Run.Relics.Select(id => Database.Relics[id].name.ToUpper()));
            var lines = new (string label, string value)[]
            {
                ("RESULT", victory ? "VICTORY" : "DEFEAT"),
                ("ACT / FLOOR", $"{Run.Act} / {Run.Floor} OF {Run.Map.Floors}"),
                ("NODES COMPLETED", s.nodesCompleted.ToString()),
                ("BATTLES WON", s.battlesWon.ToString()),
                ("ELITES DEFEATED", s.elitesWon.ToString()),
                ("ABYSSAL WARDEN", boss),
                ("GOLD EARNED", s.goldEarned.ToString()),
                ("TIME", $"{(int)Run.Duration.TotalMinutes}:{Run.Duration.Seconds:00}"),
                ("SEED", Run.RunId),
            };
            Ui.Panel(Root, 40, 34, 240, 112);
            for (int i = 0; i < lines.Length; i++)
            {
                Ui.Label(Root, lines[i].label, 50, 40 + i * 10, Palette.Ash);
                Ui.Label(Root, lines[i].value, 270, 40 + i * 10, i == 0 ? (victory ? Palette.Gold : Palette.Crimson) : Palette.Bone, Align.Right);
            }
            Ui.Label(Root, PixelFont.Wrap("RELICS: " + relics, 228), 50, 132, Palette.Lilac);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "NEW RUN", 66, 154, 86, () => Game.StartRun()));
            menu.Add(Ui.Button(Root, "TITLE", 168, 154, 86, () => Game.EndRun()));
        }
    }
}
