using UnityEngine;

namespace Obscure
{
    // Settings — Starlit Reliquary "T · Settings" board at 640x360:
    // gold title, one panel of rows (name + description left, value control right), key prompts + Back below.
    public class SettingsScreen : GameScreen
    {
        Menu menu;
        int rowY = 66;

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);

            Ui.Label(Root, "SETTINGS", 14, 12, Palette.Gold2, Align.Left, 2, FontKind.Serif);
            Ui.Panel(Root, 12, 40, 616, 262);
            Ui.Header(Root, "AUDIO, DISPLAY & COMBAT", 26, 49, 604);

            var s = Game?.Settings ?? GameSettings.Load();
            menu = Root.gameObject.AddComponent<Menu>();

            var volume = Row("MASTER VOLUME", "ALL GAME SOUND");
            void ShowVolume() => volume.label.Set($"<  {s.volume}  >");
            volume.onAdjust = d => { s.volume = Mathf.Clamp(s.volume + d, 0, 10); s.Save(); ShowVolume(); };
            volume.onClick = () => { s.volume = (s.volume + 1) % 11; s.Save(); ShowVolume(); };
            ShowVolume();

            var music = Row("MUSIC VOLUME", "ENTER MUTES OR UNMUTES THE MUSIC");
            void ShowMusic() => music.label.Set(s.musicMuted ? "<  MUTED  >" : $"<  {s.musicVolume}  >");
            music.onAdjust = d => { s.musicMuted = false; s.musicVolume = Mathf.Clamp(s.musicVolume + d, 0, 10); s.Save(); ShowMusic(); };
            music.onClick = () => { s.musicMuted = !s.musicMuted; s.Save(); ShowMusic(); };
            music.tooltip = "LEFT/RIGHT: ADJUST MUSIC   ENTER: TOGGLE MUTE";
            ShowMusic();

            var fullscreen = Row("DISPLAY MODE", "WINDOWED OR FULLSCREEN");
            void ShowFullscreen() => fullscreen.label.Set("<  " + (s.fullscreen ? "FULLSCREEN" : "WINDOWED") + "  >");
            fullscreen.onClick = () => { s.fullscreen = !s.fullscreen; s.Save(); ShowFullscreen(); };
            fullscreen.onAdjust = _ => fullscreen.onClick();
            ShowFullscreen();

            var assist = Row("QTE ASSIST", "TIMING HELP FOR REACTION PROMPTS");
            void ShowAssist() => assist.label.Set("<  " + s.Assist.ToString().ToUpperInvariant() + "  >");
            assist.onAdjust = d => { s.qteAssist = (s.qteAssist + d + 3) % 3; s.Save(); ShowAssist(); };
            assist.onClick = () => assist.onAdjust(1);
            ShowAssist();

            var key = Row("QTE BINDING", "KEY USED FOR REACTION PROMPTS");
            void ShowKey() => key.label.Set("<  " + s.QteKey.ToString().ToUpperInvariant() + "  >");
            key.onAdjust = d => { s.qteKey = (s.qteKey + d + GameSettings.QteKeys.Length) % GameSettings.QteKeys.Length; s.Save(); ShowKey(); };
            key.onClick = () => key.onAdjust(1);
            ShowKey();

            menu.Add(Ui.Button(Root, "BACK", 520, 314, 108, Back, 22));
            menu.onBack = Back;

            int hx = Ui.KeyHint(Root, 14, 316, "ADJUST", "LEFT", "RIGHT");
            hx = Ui.KeyHint(Root, hx, 316, "SELECT", "ENTER");
            Ui.KeyHint(Root, hx, 316, "BACK", "ESC");
            var help = Ui.Label(Root, "", 14, 336, Palette.Mist3);
            menu.onFocus = sel => help.Set(sel.tooltip ?? "");
        }

        // One settings row: name + description on the left, value control on the right, ink rule beneath.
        Selectable Row(string name, string description)
        {
            Ui.Label(Root, name, 34, rowY + 5, Palette.Mist1);
            Ui.Label(Root, description, 34, rowY + 16, Palette.Mist3);
            Ui.Rect(Root, 24, rowY + 29, 592, 1, Palette.Ink3);
            var b = menu.Add(Ui.Button(Root, "", 420, rowY + 4, 190, null, 20));
            rowY += 32;
            return b;
        }

        static void Back() => Game.Go(GameState.MainMenu);
    }
}
