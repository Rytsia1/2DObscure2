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
            public Func<int, int> onNavigate; // 0=Up, 1=Right, 2=Down, 3=Left -> target item index, or -1 for default
            public bool interactable = true;
            public bool selected;
            public Action<bool> visual;
            public UitkMenu menu;
            internal EventCallback<PointerEnterEvent> pointerEnterCb;
            internal EventCallback<ClickEvent> clickCb;
        }

        public readonly List<Item> items = new List<Item>();
        public Action onBack;
        public Action<Item> onFocus;
        public Action onPrevPage;
        public Action onNextPage;

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
            Clear();
            Controls.SuppressKeyboardNavigation = false;
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
            root.RegisterCallback<FocusInEvent>(e =>
            {
                if (e.target is TextField) Controls.SuppressKeyboardNavigation = true;
            });
            root.RegisterCallback<FocusOutEvent>(e =>
            {
                if (e.target is TextField) Controls.SuppressKeyboardNavigation = false;
            });
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

            EventCallback<PointerEnterEvent> onEnter = _ =>
            {
                if (Controls.Locked || (ActiveMenu != null && ActiveMenu != this)) return;
                if (item.interactable)
                    Focus(item, false);
            };
            EventCallback<ClickEvent> onClickCb = _ =>
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
            };
            item.pointerEnterCb = onEnter;
            item.clickCb = onClickCb;
            el.RegisterCallback(onEnter);
            el.RegisterCallback(onClickCb);

            items.Add(item);
            item.visual(false);
            if (items.Count - 1 == index) Focus(index, false, true);
            return item;
        }

        public void Clear()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it?.element == null) continue;
                if (it.pointerEnterCb != null) it.element.UnregisterCallback(it.pointerEnterCb);
                if (it.clickCb != null) it.element.UnregisterCallback(it.clickCb);
            }
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
            ScrollIntoView(Current?.element);
            if (sound) Sfx.Play(Sound.Move);
            onFocus?.Invoke(Current);
        }

        static void ScrollIntoView(VisualElement el)
        {
            if (el == null || el.layout.height <= 0f) return;
            for (var p = el.parent; p != null; p = p.parent)
            {
                if (p is ScrollView sv && sv.contentViewport != null && sv.contentViewport.layout.height > 0f)
                {
                    sv.ScrollTo(el);
                    break;
                }
            }
        }

        public void Focus(Item item, bool sound = true, bool force = false)
        {
            int idx = items.IndexOf(item);
            if (idx >= 0) Focus(idx, sound, force);
        }

        void Update()
        {
            if (Controls.Locked || Time.frameCount == bornFrame) return;
            if (ActiveMenu != null && ActiveMenu != this) return;
            if (items.Count == 0)
            {
                // An empty list must still let the player leave.
                if (Controls.Back && onBack != null) { Sfx.Play(Sound.Back); onBack(); }
                return;
            }

            if (onPrevPage != null && Controls.PrevPage)
            {
                Sfx.Play(Sound.Move);
                onPrevPage();
                return;
            }
            if (onNextPage != null && Controls.NextPage)
            {
                Sfx.Play(Sound.Move);
                onNextPage();
                return;
            }

            var nav = Current?.onNavigate;
            var adjust = Current?.onAdjust;
            if (adjust != null && (Controls.Left || Controls.Right))
            {
                Sfx.Play(Sound.Move);
                adjust(Controls.Left ? -1 : 1);
            }
            else if (nav != null && (Controls.Up || Controls.Right || Controls.Down || Controls.Left))
            {
                int dir = Controls.Up ? 0 : Controls.Right ? 1 : Controls.Down ? 2 : 3;
                int target = nav(dir);
                if (target >= 0 && target < items.Count)
                    Focus(target);
                else if (dir == 0 || dir == 3)
                    Focus((index - 1 + items.Count) % items.Count);
                else
                    Focus((index + 1) % items.Count);
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
