using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    using UiLabel = UnityEngine.UIElements.Label;
    // Settings — Figma "01 — Settings" rendered via UI Toolkit:
    // header (Back, title, dirty badge), left category menu, right settings rows
    // (slider, toggle, segmented control), and footer (Reset to Default, Revert, Apply).
    public class SettingsScreen : GameScreen
    {
        enum Category { Audio, Graphics, Controls, Qte }
        static readonly string[] CategoryNames = { "Audio", "Graphics", "Controls", "QTE" };
        static Category? pendingCategory;
        public static void RequestQteTab() => pendingCategory = Category.Qte;

        UitkMenu menu;
        VisualElement screenContainer;
        VisualElement rowsContainer;
        VisualElement dirtyBadge;
        UiLabel dirtyBadgeLabel;
        UiLabel sectionHeaderLabel;
        VisualElement resetBtn, revertBtn, applyBtn, backBtn;
        UitkMenu.Item revertItem, applyItem;
        readonly List<VisualElement> categoryEls = new List<VisualElement>();

        Category category = Category.Audio;
        GameSettings s;
        int[] saved;
        int focusIndex = 5;
        bool rebuilding;

        protected override void Build()
        {
            if (pendingCategory.HasValue)
            {
                category = pendingCategory.Value;
                pendingCategory = null;
            }
            UiRoot = FigmaUi.Mount(Root.gameObject, "01---Settings");
            screenContainer = UiRoot.Q<VisualElement>(className: "_01-settings") ?? UiRoot;

            s = Game?.Settings ?? GameSettings.Load();
            saved = Snapshot();

            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = Leave;
            menu.onFocus = item =>
            {
                if (!rebuilding && item != null)
                    focusIndex = menu.items.IndexOf(item);
            };

            StripStaticRows();
            EnsureDirtyBadge();
            BindStaticChrome();
            Rebuild();
        }

        int[] Snapshot() => new[] { s.volume, s.musicVolume, s.musicMuted ? 1 : 0, s.fullscreen ? 1 : 0, s.qteAssist, s.qteKey };
        void Restore(int[] v) { s.volume = v[0]; s.musicVolume = v[1]; s.musicMuted = v[2] == 1; s.fullscreen = v[3] == 1; s.qteAssist = v[4]; s.qteKey = v[5]; }
        int Dirty(int i) => Snapshot()[i] != saved[i] ? 1 : 0;
        int DirtyCount => Enumerable.Range(0, saved.Length).Sum(Dirty);

        void StripStaticRows()
        {
            var header = screenContainer.Q<VisualElement>(className: "section-header__i");
            var footerHint = FigmaUi.FindLabel(screenContainer, "LEAVING WITH UNSAVED CHANGES ASKS: APPLY · DISCARD.");
            if (header != null && footerHint != null)
            {
                int startIdx = screenContainer.IndexOf(header) + 1;
                int endIdx = screenContainer.IndexOf(footerHint);
                var toRemove = new List<VisualElement>();
                for (int i = startIdx; i < endIdx; i++)
                    toRemove.Add(screenContainer[i]);
                foreach (var el in toRemove)
                    el.RemoveFromHierarchy();
            }
            if (footerHint != null) footerHint.style.display = DisplayStyle.None; // only needed as a layout anchor

            rowsContainer = new VisualElement { name = "SettingsRows" };
            rowsContainer.style.position = Position.Absolute;
            rowsContainer.style.left = 300;
            rowsContainer.style.top = 124;
            rowsContainer.style.width = 936;
            rowsContainer.style.height = 500;
            screenContainer.Add(rowsContainer);
        }

        void EnsureDirtyBadge()
        {
            dirtyBadge = new VisualElement { name = "DirtyBadge" };
            dirtyBadge.AddToClassList("uitk-badge-gold");
            dirtyBadge.style.position = Position.Absolute;
            dirtyBadge.style.right = 28;
            dirtyBadge.style.top = 22;
            dirtyBadge.style.flexDirection = FlexDirection.Row;
            dirtyBadge.style.alignItems = UnityEngine.UIElements.Align.Center;

            var dot = new VisualElement();
            dot.AddToClassList("uitk-dirty-dot");
            dirtyBadge.Add(dot);

            dirtyBadgeLabel = new UiLabel();
            dirtyBadgeLabel.style.fontSize = 14;
            dirtyBadgeLabel.style.color = Palette.Gold1;
            dirtyBadge.Add(dirtyBadgeLabel);

            screenContainer.Add(dirtyBadge);
        }

        void BindStaticChrome()
        {
            backBtn = FigmaUi.FindButton(screenContainer, "Back");
            sectionHeaderLabel = screenContainer.Q<VisualElement>(className: "section-header__i")?.Q<UiLabel>("text-0");

            categoryEls.Clear();
            var audioInst = FigmaUi.FindInstanceWithText(screenContainer, "text-1", "Audio");
            if (audioInst != null) categoryEls.Add(audioInst);
            var gfxBtn = FigmaUi.FindButton(screenContainer, "Graphics");
            if (gfxBtn != null) categoryEls.Add(gfxBtn);
            var ctrlBtn = FigmaUi.FindButton(screenContainer, "Controls");
            if (ctrlBtn != null) categoryEls.Add(ctrlBtn);
            var qteBtn = FigmaUi.FindButton(screenContainer, "QTE  ›");
            if (qteBtn != null) categoryEls.Add(qteBtn);

            resetBtn = FigmaUi.FindButton(screenContainer, "Reset to Default");
            revertBtn = FigmaUi.FindButton(screenContainer, "Revert");
            applyBtn = FigmaUi.FindButton(screenContainer, "Apply");
        }

        void Rebuild()
        {
            if (Root == null || UiRoot == null || rebuilding) return;
            rebuilding = true;
            try
            {
                menu.Clear();
                rowsContainer.Clear();
                BuildPage();
                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        void BuildPage()
        {
            if (backBtn != null)
                menu.Add(backBtn, "Back", Leave);

            for (int i = 0; i < categoryEls.Count && i < CategoryNames.Length; i++)
            {
                var c = (Category)i;
                int menuIdx = i + 1;
                var el = categoryEls[i];
                bool sel = c == category;
                var inner = el is TemplateContainer && el.childCount == 1 ? el[0] : el;
                menu.Add(el, CategoryNames[i], () => { category = c; focusIndex = menuIdx; Rebuild(); }, selected: sel, visual: focused =>
                {
                    el.EnableInClassList("uitk-focus", focused);
                    inner.EnableInClassList("uitk-selected", sel);
                    if (inner.ClassListContains("button-flat"))
                    {
                        inner.EnableInClassList("uitk-unfocused", !focused && !sel);
                        var arrow = inner.Q<UiLabel>("text-0");
                        if (arrow != null) arrow.style.visibility = (focused || sel) ? Visibility.Visible : Visibility.Hidden;
                    }
                    else if (el is Button b)
                    {
                        string baseText = c == Category.Qte ? "QTE  ›" : CategoryNames[(int)c];
                        b.text = (focused || sel) ? $"▸  {baseText}" : baseText;
                    }
                });
            }

            if (sectionHeaderLabel != null)
            {
                sectionHeaderLabel.text = category switch
                {
                    Category.Audio => "Audio",
                    Category.Graphics => "Graphics",
                    Category.Controls => "Controls",
                    _ => "QTE assists & cues",
                };
            }

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
                    InfoRow("Navigation", "ARROWS / WASD / D-PAD");
                    InfoRow("Confirm / Interact", "ENTER / SPACE / Z / A-BUTTON");
                    InfoRow("Back / Pause", "ESC / BACKSPACE / X / B-BUTTON");
                    InfoRow("Switch Party Member", "Q / E / LB / RB");
                    break;
                default:
                    SegRow(4, "QTE assist", "STANDARD, RELAXED WINDOWS, OR AUTO (QTES OFF)", new[] { "Standard", "Relaxed", "Auto" }, s.qteAssist, v => s.qteAssist = v);
                    SegRow(5, "QTE key", "KEY USED FOR REACTION PROMPTS", GameSettings.QteKeys.Select(k => k.ToString()).ToArray(), s.qteKey, v => s.qteKey = v);
                    break;
            }

            revertItem = null;
            applyItem = null;
            if (resetBtn != null) menu.Add(resetBtn, "Reset to Default", ResetDefaults);
            if (revertBtn != null)
                revertItem = menu.Add(revertBtn, "Revert", () => { Restore(saved); Rebuild(); }, interactable: DirtyCount > 0);
            if (applyBtn != null)
                applyItem = menu.Add(applyBtn, "Apply", Apply, interactable: DirtyCount > 0);
            RefreshDirtyChrome();
        }

        void RefreshDirtyChrome()
        {
            int dirty = DirtyCount;
            if (dirtyBadge != null)
            {
                dirtyBadge.EnableInClassList("uitk-hidden", dirty == 0);
                if (dirty > 0 && dirtyBadgeLabel != null)
                    dirtyBadgeLabel.text = $"{dirty} UNSAVED CHANGE{(dirty > 1 ? "S" : "")}";
            }
            if (revertBtn is Button rb) rb.SetEnabled(dirty > 0);
            if (revertItem != null)
            {
                revertItem.interactable = dirty > 0;
                revertItem.visual?.Invoke(menu.Current == revertItem);
            }
            if (applyBtn is Button ab) ab.SetEnabled(dirty > 0);
            if (applyItem != null)
            {
                applyItem.interactable = dirty > 0;
                applyItem.visual?.Invoke(menu.Current == applyItem);
            }
        }

        (VisualElement row, VisualElement dot) RowShellWithDot(int dirtyIndex, string name, string desc)
        {
            var row = new VisualElement();
            row.AddToClassList("uitk-settings-row");

            var left = new VisualElement();
            left.style.flexDirection = FlexDirection.Row;
            left.style.alignItems = UnityEngine.UIElements.Align.Center;

            var dot = new VisualElement();
            dot.AddToClassList(Dirty(dirtyIndex) == 1 ? "uitk-dirty-dot" : "uitk-dirty-dot-off");
            left.Add(dot);

            var textCol = new VisualElement();
            var titleLbl = new UiLabel(name);
            titleLbl.style.fontSize = 20;
            titleLbl.style.color = Palette.Mist1;
            var descLbl = new UiLabel(desc);
            descLbl.style.fontSize = 13;
            descLbl.style.letterSpacing = 0.45f;
            descLbl.style.color = Palette.Mist3;
            textCol.Add(titleLbl);
            textCol.Add(descLbl);
            left.Add(textCol);

            row.Add(left);
            rowsContainer.Add(row);
            return (row, dot);
        }

        VisualElement RowShell(int dirtyIndex, string name, string desc) => RowShellWithDot(dirtyIndex, name, desc).row;

        void InfoRow(string name, string bindingText)
        {
            var row = new VisualElement();
            row.AddToClassList("uitk-settings-row");
            row.Add(new UiLabel(name) { style = { fontSize = 18, color = Palette.Mist2, marginLeft = 18 } });
            row.Add(new UiLabel(bindingText) { style = { fontSize = 14, color = Palette.Gold2, letterSpacing = 0.5f } });
            rowsContainer.Add(row);
        }

        void SliderRow(int index, string name, string desc, int value, Action<int> set)
        {
            var (row, dot) = RowShellWithDot(index, name, desc);

            var right = new VisualElement();
            right.style.flexDirection = FlexDirection.Row;
            right.style.alignItems = UnityEngine.UIElements.Align.Center;

            var track = new VisualElement();
            track.style.width = 280;
            track.style.height = 12;
            track.style.backgroundColor = Palette.Ink0;
            track.style.borderTopWidth = 2;
            track.style.borderRightWidth = 2;
            track.style.borderBottomWidth = 2;
            track.style.borderLeftWidth = 2;
            track.style.borderTopColor = Palette.Ink5;
            track.style.borderRightColor = Palette.Ink5;
            track.style.borderBottomColor = Palette.Ink5;
            track.style.borderLeftColor = Palette.Ink5;
            track.style.paddingLeft = 2;
            track.style.paddingTop = 2;
            track.style.paddingBottom = 2;
            track.style.paddingRight = 2;
            track.style.flexDirection = FlexDirection.Row;

            var fill = new VisualElement();
            fill.style.width = new Length(value * 10f, LengthUnit.Percent);
            fill.style.backgroundColor = Palette.Gold2;
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);

            var thumb = new VisualElement();
            thumb.style.width = 10;
            thumb.style.height = 24;
            thumb.style.backgroundColor = Palette.Gold1;
            thumb.style.position = Position.Absolute;
            thumb.style.top = -8;
            thumb.style.left = Mathf.RoundToInt(value / 10f * 268f);
            thumb.pickingMode = PickingMode.Ignore;
            track.Add(thumb);

            var valLbl = new UiLabel($"{value * 10}%");
            valLbl.style.fontSize = 20;
            valLbl.style.color = Palette.Mist1;
            valLbl.style.width = 68;
            valLbl.style.marginLeft = 18;
            valLbl.style.unityTextAlign = TextAnchor.MiddleRight;

            int currentVal = value;
            bool dragging = false;

            void ApplyValue(int next, bool playSound)
            {
                next = Mathf.Clamp(next, 0, 10);
                if (next == currentVal) return;
                currentVal = next;
                set(next);
                fill.style.width = new Length(next * 10f, LengthUnit.Percent);
                thumb.style.left = Mathf.RoundToInt(next / 10f * 268f);
                valLbl.text = $"{next * 10}%";
                bool dirty = Dirty(index) == 1;
                dot.EnableInClassList("uitk-dirty-dot", dirty);
                dot.EnableInClassList("uitk-dirty-dot-off", !dirty);
                if (playSound) Sfx.Play(Sound.Move);
                RefreshDirtyChrome();
            }

            track.pickingMode = PickingMode.Position;
            track.RegisterCallback<PointerDownEvent>(e =>
            {
                e.StopPropagation();
                dragging = true;
                track.CapturePointer(e.pointerId);
                ApplyValue(Mathf.RoundToInt(e.localPosition.x / 280f * 10f), true);
            });
            track.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging || !track.HasPointerCapture(e.pointerId)) return;
                e.StopPropagation();
                ApplyValue(Mathf.RoundToInt(e.localPosition.x / 280f * 10f), true);
            });
            track.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!dragging) return;
                dragging = false;
                if (track.HasPointerCapture(e.pointerId)) track.ReleasePointer(e.pointerId);
                e.StopPropagation();
            });
            track.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            track.RegisterCallback<ClickEvent>(e => e.StopPropagation());

            right.Add(track);
            right.Add(valLbl);
            row.Add(right);

            menu.Add(row, name, () => ApplyValue((currentVal + 1) % 11, false),
                visual: on => row.EnableInClassList("uitk-focus", on),
                onAdjust: d => ApplyValue(currentVal + d, false));
        }

        void ToggleRow(int index, string name, string desc, bool value, Action<bool> set)
        {
            var row = RowShell(index, name, desc);

            var right = new VisualElement();
            right.style.flexDirection = FlexDirection.Row;
            right.style.alignItems = UnityEngine.UIElements.Align.Center;

            var box = new VisualElement();
            box.style.width = 50;
            box.style.height = 26;
            box.style.backgroundColor = Palette.Ink0;
            box.style.borderTopWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderTopColor = value ? Palette.Gold3 : Palette.Ink5;
            box.style.borderRightColor = value ? Palette.Gold3 : Palette.Ink5;
            box.style.borderBottomColor = value ? Palette.Gold3 : Palette.Ink5;
            box.style.borderLeftColor = value ? Palette.Gold3 : Palette.Ink5;
            box.style.justifyContent = Justify.Center;

            var knob = new VisualElement();
            knob.style.width = 14;
            knob.style.height = 14;
            knob.style.position = Position.Absolute;
            knob.style.left = value ? 28 : 6;
            knob.style.backgroundColor = value ? Palette.Gold2 : Palette.Mist3;
            box.Add(knob);

            var valLbl = new UiLabel(value ? "ON" : "OFF");
            valLbl.style.fontSize = 20;
            valLbl.style.color = value ? Palette.Gold1 : Palette.Mist2;
            valLbl.style.width = 68;
            valLbl.style.marginLeft = 18;
            valLbl.style.unityTextAlign = TextAnchor.MiddleRight;

            right.Add(box);
            right.Add(valLbl);
            row.Add(right);

            menu.Add(row, name, () => { set(!value); Rebuild(); },
                visual: on => row.EnableInClassList("uitk-focus", on),
                onAdjust: _ => { set(!value); Rebuild(); });
        }

        void SegRow(int index, string name, string desc, string[] options, int value, Action<int> set)
        {
            var row = RowShell(index, name, desc);

            var wrap = new VisualElement();
            wrap.AddToClassList("uitk-seg-wrap");
            for (int i = 0; i < options.Length; i++)
            {
                int optIdx = i;
                var pill = new UiLabel(options[i].ToUpperInvariant());
                pill.AddToClassList("uitk-seg-opt");
                if (i == value) pill.AddToClassList("uitk-selected");
                pill.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    Sfx.Play(Sound.Confirm);
                    set(optIdx);
                    Rebuild();
                });
                wrap.Add(pill);
            }
            row.Add(wrap);

            menu.Add(row, name, () => { set((value + 1) % options.Length); Rebuild(); },
                visual: on =>
                {
                    row.EnableInClassList("uitk-focus", on);
                    wrap.EnableInClassList("uitk-focus", on);
                },
                onAdjust: d => { set((value + d + options.Length) % options.Length); Rebuild(); });
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
            FigmaUi.ShowConfirmationModal(UiRoot, "UNSAVED CHANGES", "Apply your changes before leaving?\nCancel discards them.", "APPLY",
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
