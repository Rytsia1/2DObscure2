using UnityEngine;

namespace Obscure
{
    public class SettingsScreen : GameScreen
    {
        protected override void Build()
        {
            Backdrop.Create(Root, false, 6, Palette.Void, Palette.Night, Palette.Dusk);
            Ui.Label(Root, "SETTINGS", 160, 28, Palette.Bone, Align.Center, 2);
            Ui.Panel(Root, 70, 50, 180, 104);

            var s = Game.Settings;
            var menu = Root.gameObject.AddComponent<Menu>();

            var volume = menu.Add(Ui.Button(Root, "", 90, 58, 140, null));
            void ShowVolume() => volume.label.Set($"VOLUME  < {s.volume} >");
            volume.onAdjust = d => { s.volume = Mathf.Clamp(s.volume + d, 0, 10); s.Save(); ShowVolume(); };
            volume.onClick = () => { s.volume = (s.volume + 1) % 11; s.Save(); ShowVolume(); };
            ShowVolume();

            var fullscreen = menu.Add(Ui.Button(Root, "", 90, 76, 140, null));
            void ShowFullscreen() => fullscreen.label.Set("FULLSCREEN  " + (s.fullscreen ? "ON" : "OFF"));
            fullscreen.onClick = () => { s.fullscreen = !s.fullscreen; s.Save(); ShowFullscreen(); };
            fullscreen.onAdjust = _ => fullscreen.onClick();
            ShowFullscreen();

            // QTE accessibility: Relaxed widens windows, Auto turns QTEs off (fixed Good result).
            var assist = menu.Add(Ui.Button(Root, "", 90, 94, 140, null));
            void ShowAssist() => assist.label.Set("QTE  < " + s.Assist.ToString().ToUpperInvariant() + " >");
            assist.onAdjust = d => { s.qteAssist = (s.qteAssist + d + 3) % 3; s.Save(); ShowAssist(); };
            assist.onClick = () => assist.onAdjust(1);
            ShowAssist();

            var key = menu.Add(Ui.Button(Root, "", 90, 112, 140, null));
            void ShowKey() => key.label.Set("QTE KEY  < " + s.QteKey.ToString().ToUpperInvariant() + " >");
            key.onAdjust = d => { s.qteKey = (s.qteKey + d + GameSettings.QteKeys.Length) % GameSettings.QteKeys.Length; s.Save(); ShowKey(); };
            key.onClick = () => key.onAdjust(1);
            ShowKey();

            menu.Add(Ui.Button(Root, "BACK", 90, 134, 140, Back));
            menu.onBack = Back;

            Ui.Label(Root, "LEFT/RIGHT: ADJUST   ESC: BACK", 160, 170, Palette.Ash, Align.Center);
        }

        static void Back() => Game.Go(GameState.MainMenu);
    }
}
