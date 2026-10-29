namespace Obscure
{
    public class RewardScreen : GameScreen
    {
        protected override void Build()
        {
            var r = Run.PendingReward;
            Backdrop.Create(Root, false, 10, Palette.Void, Palette.Night, Palette.Blood);
            Ui.Panel(Root, 50, 30, 220, 120);
            Ui.Label(Root, r.title.ToUpper(), 160, 40, Palette.Gold, Align.Center, 2);
            Ui.Label(Root, PixelFont.Wrap(r.text.ToUpper(), 196), 62, 62, Palette.Parchment);

            int y = 94;
            if (r.gold > 0) { Ui.Label(Root, $"+{r.gold} GOLD", 160, y, Palette.Gold, Align.Center); y += 10; }
            foreach (var id in r.items) { Ui.Label(Root, "+ " + Database.Items[id].name.ToUpper(), 160, y, Palette.Frost, Align.Center); y += 10; }
            if (r.healPercent > 0) Ui.Label(Root, $"PARTY RECOVERS {r.healPercent}% HP", 160, y, Palette.Moss, Align.Center);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "CONTINUE", 120, 130, 80, Claim));
        }

        static void Claim()
        {
            Run.ClaimReward();
            Game.Go(Run.Won ? GameState.Victory : GameState.Map);
        }
    }
}
