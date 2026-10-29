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

            Ui.Panel(Root, 8, 6, 304, 138);
            Ui.Label(Root, "PROLOGUE: THE SEALED ABYSS", 160, 12, Palette.Gold, Align.Center);

            string story =
                "BENEATH THE ASHEN CITADEL, THE ANCIENT SEAL WEAKENS. " +
                "FOUR SOULS BOUND BY OATH STEP BEYOND THE IRON GATE " +
                "TO DESCEND INTO THE SHIFTING CORRIDORS OF THE ABYSS.";
            StoryLabel = Ui.Label(Root, StoryText(story), 16, 26, Palette.Parchment);

            // 2x2 hero cards: sprite on the left, name / race / class stacked beside it.
            var party = Game?.Roster != null ? Game.Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var hero = party[i];
                int cx = 14 + i % 2 * 148, cy = 70 + i / 2 * 36;
                Ui.Image(Root, PixelArt.Box(144, 34, Palette.Void, Palette.Dusk), cx, cy, Ui.Panels + 1);
                if (hero.sprite != null)
                    Ui.Image(Root, PixelArt.FromRows(hero.sprite), cx + 6, cy + 34 - 2 - hero.sprite.Length, Ui.Widget);
                int tx = cx + 28, tw = 112;
                Ui.Label(Root, PixelFont.Fit((hero.name ?? "HERO").ToUpperInvariant(), tw), tx, cy + 4, Palette.Bone);
                Ui.Label(Root, PixelFont.Fit(RaceName(hero.raceId), tw), tx, cy + 14, Palette.Ash);
                Ui.Label(Root, PixelFont.Fit(ClassName(hero.classId), tw), tx, cy + 23, Palette.Gold);
            }

            var menu = Root.gameObject.AddComponent<Menu>();
            BeginExpeditionButton = menu.Add(Ui.Button(Root, "BEGIN EXPEDITION", 100, 154, 120, () => Game?.BeginFirstExpedition(), 14));
        }

        static string StoryText(string s) => PixelFont.Wrap(s, 288);

        static string RaceName(string id) => (id != null && Database.Races.TryGetValue(id, out var r) ? r.name : id ?? "").ToUpperInvariant();
        static string ClassName(string id) => (id != null && Database.Classes.TryGetValue(id, out var c) ? c.name : id ?? "").ToUpperInvariant();
    }
}
