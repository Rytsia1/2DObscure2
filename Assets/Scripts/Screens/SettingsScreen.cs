using System;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Settings — Starlit Reliquary "T1 · Settings" board at 640x360:
    // header (Back · title · "n unsaved changes"), category menu | rows (dirty dot, name + description, control:
    // segmented / toggle / slider), footer note with Reset to Default · Revert · Apply. Changes apply only on Apply.
    public class SettingsScreen : GameScreen
    {
        enum Category { Audio, Graphics, Controls, Qte }
        static readonly string[] CategoryNames = { "Audio", "Graphics", "Controls", "QTE" };

        Menu menu;
        Transform page;
        Category category = Category.Audio;
        GameSettings s;
        int[] saved;
        int focusIndex;
        bool rebuilding;

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            s = Game?.Settings ?? GameSettings.Load();
            saved = Snapshot();
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = Leave;
            menu.onFocus = sel => { if (!rebuilding && sel != null) focusIndex = menu.items.IndexOf(sel); };
            Rebuild();
        }

        int[] Snapshot() => new[] { s.volume, s.musicVolume, s.musicMuted ? 1 : 0, s.fullscreen ? 1 : 0, s.qteAssist, s.qteKey };
        void Restore(int[] v) { s.volume = v[0]; s.musicVolume = v[1]; s.musicMuted = v[2] == 1; s.fullscreen = v[3] == 1; s.qteAssist = v[4]; s.qteKey = v[5]; }
        int Dirty(int i) => Snapshot()[i] != saved[i] ? 1 : 0;
        int DirtyCount => Enumerable.Range(0, saved.Length).Sum(Dirty);

        void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                if (page != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(page.gameObject);
                    else UnityEngine.Object.DestroyImmediate(page.gameObject);
                }
                page = new GameObject("Settings").transform;
                page.SetParent(Root, false);
                menu.items.Clear();
                BuildPage();
                if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        void BuildPage()
        {
            // Header
            menu.Add(Ui.Button(page, "Back", 12, 9, 40, Leave, 18, showCursor: false));
            Ui.Label(page, "Settings", 62, 13, Palette.Gold2);
            int dirty = DirtyCount;
            if (dirty > 0)
            {
                string t = $"{dirty} UNSAVED CHANGE{(dirty > 1 ? "S" : "")}";
                int w = PixelFont.Measure(t) + 18;
                Ui.Image(page, PixelArt.Badge(w, 14, Color.clear, Palette.Gold3), 628 - w, 11, Ui.Widget);
                Ui.Rect(page, 628 - w + 5, 16, 4, 4, Palette.Gold1, Ui.Widget + 1);
                Ui.Label(page, t, 628 - w + 13, 15, Palette.Gold1, order: Ui.Widget + 1, font: FontKind.Interface);
            }

            // Category menu (.mi list; the open category is .is-sel)
            const int nx = 12, ny = 33, nw = 120, nh = 286;
            Ui.Panel(page, nx, ny, nw, nh);
            for (int i = 0; i < CategoryNames.Length; i++)
            {
                var c = (Category)i;
                int idx = i + 1; // menu index: Back is item 0
                var b = Ui.Button(page, CategoryNames[i], nx + 17, ny + 6 + i * 21, nw - 22, () => { category = c; focusIndex = idx; Rebuild(); }, 19, style: ButtonStyle.Flat);
                if (c == category)
                {
                    var baseVisual = b.visual;
                    var frame = Ui.Image(b.transform, PixelArt.Badge(nw - 22, 19, Palette.Hex(0xF2C25B, 20), Palette.Gold3), 0, 0, Ui.Widget - 1);
                    b.visual = on => { baseVisual(on); b.label.Color = Palette.Gold1; };
                    b.visual(false);
                }
                if (c == Category.Qte) Ui.Label(b.transform, "›", nw - 30, 5, Palette.Mist2, order: Ui.Widget + 1);
                menu.Add(b);
            }

            // Rows panel
            const int px = 139, pw = 489;
            Ui.Panel(page, px, ny, pw, nh);
            string title = category switch
            {
                Category.Audio => "Audio", Category.Graphics => "Graphics", Category.Controls => "Controls", _ => "QTE assists & cues",
            };
            Ui.Header(page, title, px + 8, ny + 6, pw - 4);
            rowY = ny + 24;
            switch (category)
            {
                case Category.Audio:
                    SliderRow(0, "Master volume", "MUSIC AND SOUND EFFECTS", s.volume, v => s.volume = v);
                    SliderRow(1, "Music volume", "MUSIC ONLY", s.musicVolume, v => s.musicVolume = v);
                    ToggleRow(2, "Mute music", "SILENCE THE SOUNDTRACK", s.musicMuted, v => s.musicMuted = v);
                    break;
                case Category.Graphics:
                    SegRow(3, "Display mode", "WINDOWED OR FULLSCREEN", new[] { "Windowed", "Fullscreen" }, s.fullscreen ? 1 : 0, v => s.fullscreen = v == 1);
                    break;
                case Category.Controls:
                    SegRow(5, "QTE key", "KEY USED FOR REACTION PROMPTS", GameSettings.QteKeys.Select(k => k.ToString()).ToArray(), s.qteKey, v => s.qteKey = v);
                    break;
                default:
                    SegRow(4, "QTE assist", "STANDARD, RELAXED WINDOWS, OR AUTO (QTES OFF)", new[] { "Standard", "Relaxed", "Auto" }, s.qteAssist, v => s.qteAssist = v);
                    SegRow(5, "QTE key", "KEY USED FOR REACTION PROMPTS", GameSettings.QteKeys.Select(k => k.ToString()).ToArray(), s.qteKey, v => s.qteKey = v);
                    break;
            }

            // Footer
            Ui.Label(page, "LEAVING WITH UNSAVED CHANGES ASKS: APPLY · DISCARD.", 12, 333, Palette.Mist2, font: FontKind.Interface);
            menu.Add(Ui.Button(page, "Reset to Default", 628 - 64 - 6 - 64 - 6 - 104, 327, 104, ResetDefaults, 22, showCursor: false, style: ButtonStyle.Flat));
            menu.Add(Ui.Button(page, "Revert", 628 - 64 - 6 - 64, 327, 64, () => { Restore(saved); Rebuild(); }, 22, interactable: dirty > 0, showCursor: false));
            menu.Add(Ui.Button(page, "Apply", 628 - 64, 327, 64, Apply, 22, interactable: dirty > 0, showCursor: false, style: ButtonStyle.Primary));
        }

        int rowY;

        // Row frame: dirty dot, name + description; the control sits at the right half.
        void RowFrame(int index, string name, string desc)
        {
            const int x = 139 + 23;
            if (Dirty(index) == 1) Ui.Rect(page, x - 10, rowY + 10, 4, 4, Palette.Gold2, Ui.Text);
            Ui.Label(page, name, x, rowY + 4, Palette.Mist1);
            Ui.Label(page, desc, x, rowY + 17, Palette.Mist3, font: FontKind.Interface);
            Ui.Rect(page, x - 14, rowY + 28, 489 - 22, 1, Palette.Ink3, Ui.Panels + 2);
        }

        const int ControlX = 139 + 250;

        // Segmented control (.seg): the chosen segment is gold.
        void SegRow(int index, string name, string desc, string[] options, int value, Action<int> set)
        {
            RowFrame(index, name, desc);
            var b = Ui.Button(page, "", ControlX, rowY + 6, 228, () => { set((value + 1) % options.Length); Rebuild(); }, 16, showCursor: true);
            b.onAdjust = d => { set((value + d + options.Length) % options.Length); Rebuild(); };
            var box = b.GetComponentInChildren<SpriteRenderer>();
            b.visual = on => box.sprite = on ? PixelArt.Badge(228, 16, Color.clear, Palette.Mist1) : null;
            b.visual(false);
            int sx = 2;
            for (int i = 0; i < options.Length; i++)
            {
                string t = options[i].ToUpperInvariant();
                int w = PixelFont.Measure(t) + 12;
                bool on = i == value;
                Ui.Image(b.transform, PixelArt.Badge(w, 12, on ? Palette.Gold2 : Palette.Ink1, Palette.Ink5), sx, 2, Ui.Widget + 1);
                Ui.Label(b.transform, t, sx + 6, 4, on ? Palette.Hex(0x1D1404) : Palette.Mist2, order: Ui.Widget + 2, font: FontKind.Interface);
                sx += w - 1;
            }
            menu.Add(b);
            rowY += 30;
        }

        // Toggle (.tog): knob right + gold when on.
        void ToggleRow(int index, string name, string desc, bool value, Action<bool> set)
        {
            RowFrame(index, name, desc);
            var b = Ui.Button(page, "", ControlX, rowY + 7, 60, () => { set(!value); Rebuild(); }, 14, showCursor: true);
            b.onAdjust = _ => { set(!value); Rebuild(); };
            var box = b.GetComponentInChildren<SpriteRenderer>();
            b.visual = on => box.sprite = PixelArt.Badge(26, 14, Palette.Ink0, on ? Palette.Mist1 : value ? Palette.Gold3 : Palette.Ink5);
            b.visual(false);
            Ui.Rect(b.transform, value ? 15 : 3, 3, 8, 8, value ? Palette.Gold2 : Palette.Mist3, Ui.Widget + 1);
            Ui.Label(b.transform, value ? "ON" : "OFF", 32, 4, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
            menu.Add(b);
            rowY += 30;
        }

        // Slider (.sld): ink track, gold fill, bright knob, value at the end.
        void SliderRow(int index, string name, string desc, int value, Action<int> set)
        {
            RowFrame(index, name, desc);
            const int w = 140;
            var b = Ui.Button(page, "", ControlX, rowY + 6, w + 44, () => { set((value + 1) % 11); Rebuild(); }, 16, showCursor: true);
            b.onAdjust = d => { set(Mathf.Clamp(value + d, 0, 10)); Rebuild(); };
            var box = b.GetComponentInChildren<SpriteRenderer>();
            b.visual = on => box.sprite = on ? PixelArt.Badge(w + 44, 16, Color.clear, Palette.Mist1) : null;
            b.visual(false);
            Ui.Image(b.transform, PixelArt.Badge(w, 6, Palette.Ink0, Palette.Ink5), 2, 5, Ui.Widget + 1);
            int fill = (w - 2) * value / 10;
            if (fill > 0) Ui.Rect(b.transform, 3, 6, fill, 4, Palette.Gold3, Ui.Widget + 2);
            Ui.Image(b.transform, PixelArt.Badge(6, 12, Palette.Gold1, Palette.Ink0), 2 + fill - 2, 2, Ui.Widget + 3);
            Ui.Label(b.transform, $"{value * 10}%", w + 10, 4, Palette.Mist1, order: Ui.Widget + 1, font: FontKind.Interface);
            menu.Add(b);
            rowY += 30;
        }

        void Apply()
        {
            s.Save();
            saved = Snapshot();
            Sfx.Play(Sound.Success);
            Rebuild();
        }

        void ResetDefaults()
        {
            var d = new GameSettings();
            s.volume = d.volume; s.musicVolume = d.musicVolume; s.musicMuted = d.musicMuted;
            s.fullscreen = d.fullscreen; s.qteAssist = d.qteAssist; s.qteKey = d.qteKey;
            Rebuild();
        }

        void Leave()
        {
            if (DirtyCount == 0) { GoBack(); return; }
            new ConfirmationModal(Root, "UNSAVED CHANGES", "Apply your changes before leaving?\nCancel discards them.", "APPLY",
                () => { s.Save(); GoBack(); },
                () => { Restore(saved); GoBack(); });
        }

        static void GoBack()
        {
            var prev = Game.I != null ? Game.I.PreviousState : GameState.MainMenu;
            Game?.Go(prev == GameState.Hub || prev == GameState.Map || prev == GameState.MainMenu ? prev : GameState.MainMenu);
        }
    }
}
