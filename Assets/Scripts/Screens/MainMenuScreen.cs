using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Main Menu — Starlit Reliquary "A · Main Menu" board at 640x360:
    // night sky + tower silhouette, left-aligned gold title, flat menu list, key-prompt footer.
    public class MainMenuScreen : GameScreen
    {
        public Selectable NewGameButton { get; private set; }
        public Selectable ContinueButton { get; private set; }
        public Selectable LoadGameButton { get; private set; }
        public Selectable SettingsButton { get; private set; }
        public Selectable ExitButton { get; private set; }

        protected override void Build()
        {
            Backdrop.Create(Root, true, 16);

            // Tower silhouette with lit windows, then mountain ranges
            Color tower = Palette.Hex(0x0E1430);
            Ui.Rect(Root, 487, 120, 36, 180, tower, Ui.Back + 5);
            Ui.Rect(Root, 478, 165, 54, 135, tower, Ui.Back + 5);
            Ui.Rect(Root, 497, 104, 16, 16, tower, Ui.Back + 5);
            Ui.Rect(Root, 502, 112, 5, 7, Palette.Gold2, Ui.Back + 6);
            Ui.Rect(Root, 508, 153, 4, 6, Palette.Ember2, Ui.Back + 6);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 80, Palette.Hex(0x1A2146), 3), 0, 250, Ui.Back + 7);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 54, Palette.Hex(0x111733), 11), 0, 290, Ui.Back + 8);
            Ui.Rect(Root, 0, 330, Ui.Width, 30, Palette.Hex(0x0A0F22), Ui.Back + 9);

            // Title block
            Ui.Divider(Root, 48, 40, 210, Palette.Gold3, Palette.Gold2);
            Ui.Label(Root, "2D OBSCURE", 48, 54, Palette.Gold1, Align.Left, 3, FontKind.Serif);
            for (int i = 0; i < 8; i++) Ui.Image(Root, PixelArt.Diamond(7, Palette.Gold3, false), 48 + i * 10, 98, Ui.Text);
            Ui.Label(Root, "A PARTY ROGUELIKE ACROSS EIGHT DUNGEONS", 134, 98, Palette.Mist2);

            // Menu list
            var menu = Root.gameObject.AddComponent<Menu>();
            bool hasSave = SaveManager.HasAnySave(out _) || GameSave.HasSavedGame();
            NewGameButton = menu.Add(Ui.Button(Root, "NEW GAME", 61, 154, 190, () => Game?.StartNewGame(), 22, style: ButtonStyle.Flat));
            ContinueButton = menu.Add(Ui.Button(Root, "CONTINUE", 61, 178, 190, () => Game?.ContinueGame(), 22, interactable: hasSave, style: ButtonStyle.Flat));
            LoadGameButton = menu.Add(Ui.Button(Root, "LOAD GAME", 61, 202, 190, () => Game?.Go(GameState.Load), 22, interactable: hasSave, style: ButtonStyle.Flat));
            SettingsButton = menu.Add(Ui.Button(Root, "SETTINGS", 61, 226, 190, () => Game?.Go(GameState.Settings), 22, style: ButtonStyle.Flat));
            ExitButton = menu.Add(Ui.Button(Root, "EXIT", 61, 250, 190, () => Game?.Quit(), 22, style: ButtonStyle.Flat));

            // Footer: key prompts + build tag on an ink band
            Ui.Rect(Root, 0, 334, Ui.Width, 26, Palette.Hex(0x070A14, 220), Ui.Panels);
            int hx = Ui.KeyHint(Root, 48, 341, "SELECT", "ARROWS");
            Ui.KeyHint(Root, hx, 341, "CONFIRM", "ENTER");
            Ui.Label(Root, "V" + Application.version, 592, 343, Palette.Mist3, Align.Right);
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
            Backdrop.Create(Root, true, 12, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk, Palette.ArcaneViolet);

            Ui.Panel(Root, 16, 14, 608, 292, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Divider(Root, 36, 22, 568, Palette.DarkBorder, Palette.AntiqueGoldBright);
            Ui.Label(Root, "PROLOGUE: THE SEALED ABYSS", 320, 30, Palette.AntiqueGoldBright, Align.Center, 1, FontKind.Serif);
            Ui.Divider(Root, 36, 48, 568, Palette.DarkBorder, Palette.AntiqueGoldBright);

            string story =
                "BENEATH THE ASHEN CITADEL, THE ANCIENT SEAL WEAKENS. " +
                "FOUR SOULS BOUND BY OATH STEP BEYOND THE IRON GATE " +
                "TO DESCEND INTO THE SHIFTING CORRIDORS OF THE ABYSS.";
            StoryLabel = Ui.Label(Root, StoryText(story), 40, 58, Palette.WarmIvory);

            // 4 hero showcase cards across 608px panel
            var party = Game?.Roster != null ? Game.Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var hero = party[i];
                int cx = 28 + i * 146, cy = 110;
                Ui.Image(Root, PixelArt.Box(140, 176, Palette.MidnightNavy, Palette.DarkBorder), cx, cy, Ui.Panels + 1);

                // Portrait frame
                Ui.Image(Root, PixelArt.Box(36, 40, Palette.DeepCharcoal, Palette.AntiqueGoldDim), cx + 52, cy + 12, Ui.Panels + 2);
                if (hero.sprite != null)
                    Ui.Image(Root, PixelArt.FromRows(hero.sprite), cx + 58, cy + 46 - hero.sprite.Length, Ui.Widget);

                Ui.Divider(Root, cx + 12, cy + 62, 116, Palette.DarkBorder, Palette.AntiqueGoldDim);

                Ui.Label(Root, PixelFont.Fit((hero.name ?? "HERO").ToUpperInvariant(), 120), cx + 70, cy + 72, Palette.WarmIvory, Align.Center, 1, FontKind.Serif);
                Ui.Label(Root, PixelFont.Fit(RaceName(hero.raceId), 120), cx + 70, cy + 96, Palette.MutedGray, Align.Center);
                Ui.Label(Root, PixelFont.Fit(ClassName(hero.classId), 120), cx + 70, cy + 116, Palette.AntiqueGoldBright, Align.Center);
            }

            var menu = Root.gameObject.AddComponent<Menu>();
            BeginExpeditionButton = menu.Add(Ui.Button(Root, "BEGIN EXPEDITION", 230, 318, 180, () => Game?.BeginFirstExpedition(), 26));
        }

        static string StoryText(string s) => PixelFont.Wrap(s, 560);

        static string RaceName(string id) => (id != null && Database.Races.TryGetValue(id, out var r) ? r.name : id ?? "").ToUpperInvariant();
        static string ClassName(string id) => (id != null && Database.Classes.TryGetValue(id, out var c) ? c.name : id ?? "").ToUpperInvariant();
    }
}
