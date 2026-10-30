using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Main Menu conforming to UI/UX Overhaul Specification (Section 4.5)
    // 640x360 Reference Resolution, Serif Display Typography, Antique Gold Filigree, Octopath Traveler II Aesthetics.
    public class MainMenuScreen : GameScreen
    {
        public Selectable NewGameButton { get; private set; }
        public Selectable ContinueButton { get; private set; }
        public Selectable SettingsButton { get; private set; }
        public Selectable ExitButton { get; private set; }

        protected override void Build()
        {
            Backdrop.Create(Root, true, 16, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk, Palette.SpecCrimson);

            // Distant mountain silhouettes in 640x360
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 70, Palette.MidnightNavy, 3), 0, 240, Ui.Back + 5);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 50, Palette.DeepCharcoal, 11), 0, 280, Ui.Back + 6);

            // Party heroes resting along the cliff edge
            int x = 28;
            foreach (var id in Database.Party)
            {
                if (Database.Characters.TryGetValue(id, out var c) && c.sprite != null)
                {
                    Ui.Image(Root, PixelArt.FromRows(c.sprite), x, 285 - c.sprite.Length, Ui.World);
                    x += 24;
                }
            }

            // Top version / fantasy lore crest
            Ui.Badge(Root, "• CHRONICLES OF THE FALLEN REALM •", 205, 20, 230, 16, Palette.DeepCharcoal, Palette.AntiqueGoldDim, Palette.AntiqueGoldBright, Align.Center);

            // Grand Title with modern ornaments & serif display
            Ui.Divider(Root, 140, 44, 360, Palette.AntiqueGoldDim, Palette.AntiqueGoldBright);
            Ui.Label(Root, "2D OBSCURE", 320, 52, Palette.WarmIvory, Align.Center, 3, FontKind.Serif);
            Ui.Divider(Root, 140, 88, 360, Palette.DarkBorder, Palette.AntiqueGoldDim);
            Ui.Badge(Root, "A DESCENT INTO THE ABYSS", 225, 96, 190, 16, Palette.DeepCharcoal, Palette.SpecCrimson, Palette.WarmIvory, Align.Center);

            // Center Command Deck Panel (640x360 centered)
            Ui.Panel(Root, 235, 126, 170, 154, Palette.DeepCharcoal, Palette.AntiqueGold);

            var menu = Root.gameObject.AddComponent<Menu>();
            NewGameButton = menu.Add(Ui.Button(Root, "NEW GAME", 245, 138, 150, () => Game?.StartNewGame(), 22));
            ContinueButton = menu.Add(Ui.Button(Root, "CONTINUE", 245, 170, 150, () => Game?.ContinueGame(), 22, interactable: GameSave.HasSavedGame()));
            SettingsButton = menu.Add(Ui.Button(Root, "SETTINGS", 245, 202, 150, () => Game?.Go(GameState.Settings), 22));
            ExitButton = menu.Add(Ui.Button(Root, "EXIT", 245, 234, 150, () => Game?.Quit(), 22));

            // Modern keycaps footer
            Ui.Badge(Root, "[ARROWS / MOUSE] NAVIGATE   •   [ENTER / CLICK] SELECT   •   [ESC] BACK", 120, 328, 400, 18, Palette.DeepCharcoal, Palette.DarkBorder, Palette.MutedGray, Align.Center);
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
