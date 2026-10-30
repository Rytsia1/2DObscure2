using System.Linq;

namespace Obscure
{
    // Presents RunState.PendingReward (combat or treasure). Gold is automatic; the player picks one prize or skips.
    public class RewardScreen : GameScreen
    {
        protected override void Build()
        {
            var r = Run.PendingReward;
            Backdrop.Create(Root, false, 10, Palette.Void, Palette.Night, Palette.Blood);
            Ui.Panel(Root, 30, 22, 260, 142);
            Ui.Label(Root, r.title.ToUpper(), 160, 30, Palette.Gold, Align.Center, 2);
            Ui.Label(Root, PixelFont.Wrap(r.text.ToUpper(), 236), 42, 50, Palette.Parchment);
            Ui.Label(Root, $"+{r.gold} GOLD", 160, 70, Palette.Gold, Align.Center);

            var hint = Ui.Label(Root, "", 160, 150, Palette.Ash, Align.Center);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => hint.Set(s.tooltip ?? "");
            for (int i = 0; i < r.choices.Count; i++)
            {
                int choice = i;
                var o = r.choices[i];
                var (label, detail) = RewardGenerator.Describe(o);
                if (o.kind == RewardKind.Upgrade) // preview against this run's stats, not the base sheet
                {
                    var u = Run.Party.FirstOrDefault(p => p.id == o.id || p.classDef?.id == o.id) ?? Run.Party[0];
                    detail = u.stats.Preview(Database.Upgrades[o.id].bonus);
                }
                var b = Ui.Button(Root, label.ToUpper(), 50, 84 + i * 15, 220, () => Claim(choice));
                b.tooltip = detail.ToUpper();
                menu.Add(b);
            }
            var skip = Ui.Button(Root, r.choices.Count > 0 ? "SKIP PRIZE" : "CONTINUE", 50, 84 + r.choices.Count * 15, 220, () => Claim(-1));
            skip.tooltip = "TAKE ONLY THE GOLD";
            menu.Add(skip);
        }

        static void Claim(int choice)
        {
            Run.ClaimReward(choice);
            if (choice >= 0) Sfx.Play(Sound.Success); // a prize taken, not skipped
            Game.Go(Run.Won ? GameState.Victory : GameState.Map);
        }
    }
}
