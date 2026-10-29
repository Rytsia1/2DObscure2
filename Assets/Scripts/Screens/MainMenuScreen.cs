namespace Obscure
{
    public class MainMenuScreen : GameScreen
    {
        protected override void Build()
        {
            Backdrop.Create(Root, true, 14, Palette.Void, Palette.Night, Palette.Dusk, Palette.Blood);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 50, Palette.Night, 3), 0, 118, Ui.Back + 5);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 34, Palette.Void, 11), 0, 146, Ui.Back + 6);

            int x = 14;
            foreach (var id in Database.Party)
            {
                Ui.Image(Root, PixelArt.FromRows(Database.Characters[id].sprite), x, 142, Ui.World);
                x += 18;
            }

            Ui.Label(Root, "OBSCURE", 160, 24, Palette.Bone, Align.Center, 3);
            Ui.Label(Root, "A DESCENT INTO THE ABYSS", 160, 52, Palette.Crimson, Align.Center);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "START RUN", 120, 76, 80, () => Game.StartRun()));
            menu.Add(Ui.Button(Root, "SETTINGS", 120, 93, 80, () => Game.Go(GameState.Settings)));
            menu.Add(Ui.Button(Root, "EXIT", 120, 110, 80, Game.Quit));

            Ui.Label(Root, "ARROWS/MOUSE: SELECT   ENTER: CONFIRM", 160, 170, Palette.Ash, Align.Center);
        }
    }
}
