namespace Obscure
{
    // Shared Game Over / Victory screen.
    public class EndScreen : GameScreen
    {
        readonly bool victory;
        public EndScreen(bool victory) { this.victory = victory; }

        protected override void Build()
        {
            if (victory) Backdrop.Create(Root, true, 24, Palette.Void, Palette.Night, Palette.Dusk, Palette.Arcane);
            else Backdrop.Create(Root, false, 4, Palette.Void, Palette.Void, Palette.Blood);

            Ui.Label(Root, victory ? "THE ABYSS IS SEALED" : "YOU HAVE FALLEN", 160, 40, victory ? Palette.Gold : Palette.Crimson, Align.Center, 2);
            Ui.Label(Root, victory ? "THE WARDEN'S EYE CLOSES AT LAST." : "THE DARK CLAIMS ANOTHER PARTY.", 160, 64, Palette.Parchment, Align.Center);
            Ui.Label(Root, $"ROOMS CLEARED {Run.Cleared.Count}   GOLD {Run.Gold}", 160, 80, Palette.Ash, Align.Center);

            if (victory)
                for (int i = 0; i < Run.Party.Count; i++)
                    Ui.Image(Root, PixelArt.FromRows(Run.Party[i].sprite), 122 + i * 20, 96, Ui.World);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "RETURN TO TITLE", 105, 130, 110, () => Game.EndRun()));
        }
    }
}
