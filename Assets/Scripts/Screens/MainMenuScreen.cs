using System.Linq;

namespace Obscure
{
    public class MainMenuScreen : GameScreen
    {
        public Selectable NewGameButton { get; private set; }
        public Selectable ContinueButton { get; private set; }
        public Selectable SettingsButton { get; private set; }
        public Selectable ExitButton { get; private set; }

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

            Ui.Label(Root, "OBSCURE", 160, 22, Palette.Bone, Align.Center, 3);
            Ui.Label(Root, "A DESCENT INTO THE ABYSS", 160, 48, Palette.Crimson, Align.Center);

            var menu = Root.gameObject.AddComponent<Menu>();
            NewGameButton = menu.Add(Ui.Button(Root, "NEW GAME", 120, 68, 80, () => Game?.StartNewGame()));
            ContinueButton = menu.Add(Ui.Button(Root, "CONTINUE", 120, 84, 80, () => Game?.ContinueGame(), interactable: GameSave.HasSavedGame()));
            SettingsButton = menu.Add(Ui.Button(Root, "SETTINGS", 120, 100, 80, () => Game?.Go(GameState.Settings)));
            ExitButton = menu.Add(Ui.Button(Root, "EXIT", 120, 116, 80, () => Game?.Quit()));

            Ui.Label(Root, "ARROWS/MOUSE: SELECT   ENTER: CONFIRM", 160, 170, Palette.Ash, Align.Center);
        }
    }

    // Opening story sequence shown after the player confirms their 4-character New Game party
    // and before the first expedition begins (Section 2 & Section 12).
    public class OpeningSequenceScreen : GameScreen
    {
        public Selectable BeginExpeditionButton { get; private set; }
        public Label StoryLabel { get; private set; }

        protected override void Build()
        {
            Backdrop.Create(Root, true, 12, Palette.Void, Palette.Night, Palette.Dusk, Palette.Arcane);

            Ui.Panel(Root, 16, 12, 288, 128);
            Ui.Label(Root, "PROLOGUE: THE SEALED ABYSS", 160, 20, Palette.Gold, Align.Center);

            string story =
                "BENEATH THE ASHEN CITADEL, THE ANCIENT SEAL WEAKENS.\n" +
                "FOUR SOULS BOUND BY OATH STEP BEYOND THE IRON GATE\n" +
                "TO DESCEND INTO THE SHIFTING CORRIDORS OF THE ABYSS.";
            StoryLabel = Ui.Label(Root, StoryText(story), 26, 36, Palette.Parchment);

            var party = Game?.Roster != null ? Game.Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            for (int i = 0; i < party.Count; i++)
            {
                var hero = party[i];
                int hx = 30 + i * 68;
                if (hero.sprite != null)
                    Ui.Image(Root, PixelArt.FromRows(hero.sprite), hx + 18, 76, Ui.Widget);
                string hName = hero.name != null && hero.name.Length > 10 ? hero.name.Substring(0, 10) : (hero.name ?? "HERO");
                Ui.Label(Root, hName.ToUpperInvariant(), hx + 26, 104, Palette.Bone, Align.Center);
                Ui.Label(Root, $"{(hero.raceId ?? "").ToUpperInvariant()}", hx + 26, 114, Palette.Ash, Align.Center);
                Ui.Label(Root, $"{(hero.classId ?? "").ToUpperInvariant()}", hx + 26, 124, Palette.Gold, Align.Center);
            }

            var menu = Root.gameObject.AddComponent<Menu>();
            BeginExpeditionButton = menu.Add(Ui.Button(Root, "BEGIN EXPEDITION", 100, 148, 120, () => Game?.BeginFirstExpedition(), 14));
        }

        static string StoryText(string s) => PixelFont.Wrap(s, 268);
    }
}
