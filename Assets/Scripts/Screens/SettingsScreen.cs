using UnityEngine;

namespace Obscure
{
    // Settings Screen conforming to UI/UX Overhaul Specification
    // 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers.
    public class SettingsScreen : GameScreen
    {
        protected override void Build()
        {
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // Title & Filigree Header
            Ui.Label(Root, "SYSTEM SETTINGS & AUDIO", 320, 22, Palette.AntiqueGoldBright, Align.Center, 2, FontKind.Serif);
            Ui.Divider(Root, 160, 44, 320, Palette.DarkBorder, Palette.AntiqueGoldBright);

            // Centered Settings Container Panel (300x246)
            Ui.Panel(Root, 170, 56, 300, 246, Palette.DeepCharcoal, Palette.AntiqueGold);

            var s = Game?.Settings ?? GameSettings.Load();
            var menu = Root.gameObject.AddComponent<Menu>();

            // 1. Master Volume
            var volume = menu.Add(Ui.Button(Root, "", 190, 72, 260, null, 22));
            void ShowVolume() => volume.label.Set($"MASTER VOLUME  < {s.volume} >");
            volume.onAdjust = d => { s.volume = Mathf.Clamp(s.volume + d, 0, 10); s.Save(); ShowVolume(); };
            volume.onClick = () => { s.volume = (s.volume + 1) % 11; s.Save(); ShowVolume(); };
            ShowVolume();

            // 2. Music Volume
            var music = menu.Add(Ui.Button(Root, "", 190, 104, 260, null, 22));
            void ShowMusic() => music.label.Set(s.musicMuted ? "MUSIC  < MUTED >" : $"MUSIC VOLUME  < {s.musicVolume} >");
            music.onAdjust = d => { s.musicMuted = false; s.musicVolume = Mathf.Clamp(s.musicVolume + d, 0, 10); s.Save(); ShowMusic(); };
            music.onClick = () => { s.musicMuted = !s.musicMuted; s.Save(); ShowMusic(); };
            music.tooltip = "LEFT/RIGHT: ADJUST MUSIC   ENTER: TOGGLE MUTE";
            ShowMusic();

            // 3. Fullscreen Display
            var fullscreen = menu.Add(Ui.Button(Root, "", 190, 136, 260, null, 22));
            void ShowFullscreen() => fullscreen.label.Set("DISPLAY MODE  < " + (s.fullscreen ? "FULLSCREEN" : "WINDOWED") + " >");
            fullscreen.onClick = () => { s.fullscreen = !s.fullscreen; s.Save(); ShowFullscreen(); };
            fullscreen.onAdjust = _ => fullscreen.onClick();
            ShowFullscreen();

            // 4. QTE Accessibility Assist
            var assist = menu.Add(Ui.Button(Root, "", 190, 168, 260, null, 22));
            void ShowAssist() => assist.label.Set("QTE ASSIST  < " + s.Assist.ToString().ToUpperInvariant() + " >");
            assist.onAdjust = d => { s.qteAssist = (s.qteAssist + d + 3) % 3; s.Save(); ShowAssist(); };
            assist.onClick = () => assist.onAdjust(1);
            ShowAssist();

            // 5. QTE Trigger Key Binding
            var key = menu.Add(Ui.Button(Root, "", 190, 200, 260, null, 22));
            void ShowKey() => key.label.Set("QTE BINDING  < " + s.QteKey.ToString().ToUpperInvariant() + " >");
            key.onAdjust = d => { s.qteKey = (s.qteKey + d + GameSettings.QteKeys.Length) % GameSettings.QteKeys.Length; s.Save(); ShowKey(); };
            key.onClick = () => key.onAdjust(1);
            ShowKey();

            // 6. Return Button
            menu.Add(Ui.Button(Root, "BACK", 190, 248, 260, Back, 24));
            menu.onBack = Back;

            const string Help = "[ARROWS] ADJUST   •   [ENTER] SELECT   •   [ESC] BACK";
            Ui.Badge(Root, Help, 150, 318, 340, 18, Palette.DeepCharcoal, Palette.DarkBorder, Palette.MutedGray, Align.Center);
            var help = Ui.Label(Root, Help, 320, 322, Palette.MutedGray, Align.Center);
            menu.onFocus = sel => help.Set(sel.tooltip ?? Help);
        }

        static void Back() => Game.Go(GameState.MainMenu);
    }
}
