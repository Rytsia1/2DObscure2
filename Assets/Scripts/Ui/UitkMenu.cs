using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    // Keyboard, gamepad, and pointer navigation controller for UI Toolkit screens.
    // Maintains ActiveMenu stacking for modals cleanly and avoids UITK's built-in NavigationSubmit double-firing.
    public class UitkMenu : MonoBehaviour
    {
        public static UitkMenu ActiveMenu;

        public sealed class Item
        {
            public VisualElement element;
            public string text;
            public string tooltip;
            public Action onClick;
            public Action<int> onAdjust;
            public bool interactable = true;
            public bool selected;
            public Action<bool> visual;
            public UitkMenu menu;
        }

        public readonly List<Item> items = new List<Item>();
        public Action onBack;
        public Action<Item> onFocus;

        int index;
        int bornFrame;
        UitkMenu previousMenu;
        VisualElement registeredRoot;

        void Awake()
        {
            bornFrame = Time.frameCount;
            if (ActiveMenu != null && ActiveMenu != this) previousMenu = ActiveMenu;
            ActiveMenu = this;
        }

        void OnEnable()
        {
            if (ActiveMenu != null && ActiveMenu != this) previousMenu = ActiveMenu;
            ActiveMenu = this;
        }

        void OnDestroy()
        {
            if (ActiveMenu == this) ActiveMenu = previousMenu != null ? previousMenu : null;
        }

        public void AttachRoot(VisualElement root)
        {
            if (root == null || registeredRoot == root) return;
            registeredRoot = root;
            // Suppress built-in UITK navigation events so Controls.Up/Down/Confirm remain authoritative.
            root.RegisterCallback<NavigationMoveEvent>(e => e.StopPropagation(), TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(e => e.StopPropagation(), TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(e => e.StopPropagation(), TrickleDown.TrickleDown);
        }

        public Item Current => index >= 0 && index < items.Count ? items[index] : null;
        public int CurrentIndex => index;

        public Item Add(VisualElement el, string text, Action onClick, bool interactable = true, bool selected = false, Action<bool> visual = null, Action<int> onAdjust = null, string tooltip = null)
        {
            if (el == null) return null;
            el.focusable = false;
            el.pickingMode = PickingMode.Position;
            if (el is Button btn) btn.clickable = null;

            var item = new Item
            {
                element = el,
                text = text ?? "",
                tooltip = tooltip,
                onClick = onClick,
                onAdjust = onAdjust,
                interactable = interactable,
                selected = selected,
                visual = visual ?? (focused => FigmaUi.ApplyDefaultFocusVisual(el, focused, selected, interactable)),
                menu = this,
            };

            el.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (Controls.Locked || (ActiveMenu != null && ActiveMenu != this)) return;
                if (item.interactable)
                    Focus(item, false);
            });

            el.RegisterCallback<ClickEvent>(_ =>
            {
                if (Controls.Locked || (ActiveMenu != null && ActiveMenu != this)) return;
                if (!item.interactable)
                {
                    Sfx.Play(Sound.Deny);
                    return;
                }
                Focus(item, false);
                if (item.onClick != null)
                {
                    Sfx.Play(Sound.Confirm);
                    item.onClick();
                }
            });

            items.Add(item);
            item.visual(false);
            if (items.Count - 1 == index) Focus(index, false, true);
            return item;
        }

        public void Clear()
        {
            items.Clear();
            index = 0;
        }

        public void Focus(int i, bool sound = true, bool force = false)
        {
            if (i < 0 || i >= items.Count) return;
            if (i == index && !force) return;
            Current?.visual?.Invoke(false);
            index = i;
            Current?.visual?.Invoke(true);
            ActiveMenu = this;
            if (sound) Sfx.Play(Sound.Move);
            onFocus?.Invoke(Current);
        }

        public void Focus(Item item, bool sound = true, bool force = false)
        {
            int idx = items.IndexOf(item);
            if (idx >= 0) Focus(idx, sound, force);
        }

        void Update()
        {
            if (Controls.Locked || items.Count == 0 || Time.frameCount == bornFrame) return;
            if (ActiveMenu != null && ActiveMenu != this) return;

            var adjust = Current?.onAdjust;
            if (adjust != null && (Controls.Left || Controls.Right))
            {
                Sfx.Play(Sound.Move);
                adjust(Controls.Left ? -1 : 1);
            }
            else if (Controls.Up || Controls.Left)
            {
                Focus((index - 1 + items.Count) % items.Count);
            }
            else if (Controls.Down || Controls.Right)
            {
                Focus((index + 1) % items.Count);
            }

            if (Controls.Confirm) Activate();
            else if (Controls.Back && onBack != null)
            {
                Sfx.Play(Sound.Back);
                onBack();
            }
        }

        public void Activate()
        {
            if (Current == null || Current.onClick == null) return;
            if (!Current.interactable)
            {
                Sfx.Play(Sound.Deny);
                return;
            }
            Sfx.Play(Sound.Confirm);
            Current.onClick();
        }
    }
}
