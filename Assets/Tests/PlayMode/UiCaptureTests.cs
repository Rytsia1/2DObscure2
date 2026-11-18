using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Obscure.Tests
{
    // UI review tool, not a regression test: boots the game, opens every screen and saves a 2x PNG of each.
    // Run explicitly: -runTests -testPlatform PlayMode -testFilter Obscure.Tests.UiCaptureTests  (UI_CAPTURE_DIR sets the folder).
    [Explicit]
    public class UiCaptureTests
    {
        static readonly GameState[] Screens =
        {
            GameState.MainMenu, GameState.Settings, GameState.CharacterCreation, GameState.Load,
            GameState.Hub, GameState.Party, GameState.HubShop, GameState.Armory, GameState.Blacksmith, GameState.Temple, GameState.MagicShop, GameState.Save,
            GameState.Map, GameState.CharacterStatus, GameState.Inventory, GameState.Equipment, GameState.Skills, GameState.Talents,
            GameState.Event, GameState.Rest, GameState.Shop, GameState.Reward, GameState.Combat, GameState.DungeonClear, GameState.GameOver,
        };

        [UnityTest]
        public IEnumerator CaptureEveryScreen()
        {
            string dir = Environment.GetEnvironmentVariable("UI_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Application.temporaryCachePath, "UiCaptures");
            Directory.CreateDirectory(dir);

            // Visiting the Hub auto-saves: keep that in memory so the player's real save files are never touched.
            var realStorage = SaveManager.Storage;
            SaveManager.Storage = new MemorySaveStorage();
            try
            {
                yield return Settle();
                Game.I.Settings.qteAssist = (int)QteAssist.Auto;
                Game.I.StartRun(1234);
                yield return Settle();

                for (int i = 0; i < Screens.Length; i++)
                {
                    Game.I.Go(Screens[i]);
                    yield return Settle();
                    // Combat needs a moment to reach the first player turn's command menu.
                    float wait = Screens[i] == GameState.Combat ? 2.5f : 0.3f;
                    for (float t = 0; t < wait; t += Time.unscaledDeltaTime) yield return null;
                    yield return new WaitForEndOfFrame();
                    Save(Path.Combine(dir, $"{i:00}-{Screens[i]}.png"));
                }
            }
            finally { SaveManager.Storage = realStorage; }
        }

        static IEnumerator Settle()
        {
            for (float t = 0; (Game.I == null || Controls.Locked) && t < 5f; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(Game.I, "game did not boot");
            yield return null;
        }

        static void Save(string path)
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }
    }
}
