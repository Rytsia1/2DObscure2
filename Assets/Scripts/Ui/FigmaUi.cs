using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    using UiLabel = UnityEngine.UIElements.Label;
    // Shared UI Toolkit runtime bridge for mounting imported Figma UXML/USS screens and binding data.
    public static class FigmaUi
    {
        static readonly Dictionary<string, Texture2D> spriteTextureCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        static UIDocument fadeDoc;
        static VisualElement fadeElement;

        public static VisualElement Mount(GameObject host, string screenKey, int sortingOrder = 10)
        {
            var catalog = FigmaScreenCatalog.Instance;
            var entry = catalog != null ? catalog.Get(screenKey) : LoadEditorEntry(screenKey);
            var ps = catalog != null ? catalog.GetPanelSettings() : LoadEditorPanelSettings();

            var doc = host.GetComponent<UIDocument>();
            if (doc == null) doc = host.AddComponent<UIDocument>();
            doc.sortingOrder = sortingOrder;
            if (ps != null)
            {
                if (ps.screenMatchMode != PanelScreenMatchMode.Expand)
                    ps.screenMatchMode = PanelScreenMatchMode.Expand;
                doc.panelSettings = ps;
            }

            VisualElement root = null;
            if (entry?.uxml != null)
            {
                doc.visualTreeAsset = entry.uxml;
                root = doc.rootVisualElement;
                if (root == null || root.childCount == 0)
                {
                    if (root != null)
                    {
                        root.Clear();
                        entry.uxml.CloneTree(root);
                    }
                    else
                    {
                        root = entry.uxml.CloneTree();
                    }
                }
            }
            else
            {
                root = doc.rootVisualElement ?? new VisualElement();
            }

            AttachSharedStyles(root, catalog, entry);
            if (root != null)
                root.AddToClassList("uitk-screen-root");
            KeepRowsClear(root);
            RenameCurrencyTerms(root);
            return root;
        }

        // The Figma screens still bake the old currency names. The game calls the carried pocket Spoils and the
        // secured one Gold, so baked text is renamed on load. Text lookups (FindLabel, TitleRuleTags) use the new names.
        // ponytail: runtime patch until the FELNA Figma file and its UXML export are renamed; delete it then.
        static void RenameCurrencyTerms(VisualElement root)
        {
            root?.Query<TextElement>().ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.text) || t.text.IndexOf(" GOLD", StringComparison.OrdinalIgnoreCase) < 0) return;
                t.text = t.text.Replace("RUN GOLD IS ", "SPOILS ARE ").Replace("RUN GOLD", "SPOILS").Replace("HUB GOLD", "GOLD")
                               .Replace("Run Gold", "Spoils").Replace("Hub Gold", "Gold");
            });
        }

        // Hides baked Figma elements the game doesn't use (display: none keeps sibling indices stable).
        public static void HideByClass(VisualElement root, params string[] classes)
        {
            if (root == null) return;
            foreach (var cls in classes)
                foreach (var el in root.Query<VisualElement>(className: cls).ToList())
                    el.style.display = DisplayStyle.None;
        }

        // Hides baked Figma tag/toast instances by their text (they carry no class of their own).
        public static void HideInstancesWithText(VisualElement root, params string[] texts)
        {
            foreach (var t in texts)
            {
                var inst = FindLabel(root, t)?.GetFirstAncestorOfType<TemplateContainer>();
                if (inst != null && inst != root) inst.style.display = DisplayStyle.None;
            }
        }

        // Rule/descriptor tags baked beside screen titles; they only restate the title or a rule.
        public static readonly string[] TitleRuleTags =
        {
            "HUB SHOP · CONSUMABLES · PAYS IN GOLD", "EQUIPMENT · PAYS IN GOLD",
            "REVIVE · RECOVERY · PAYS IN GOLD", "RELICS · PAYS IN GOLD",
            "PRICES ARE PLACEHOLDERS · TBD", "HUB ONLY", "SPOILS · LOST IF THE PARTY FALLS",
        };

        // Runs once, after the element's first layout. Use it to read exported Figma positions:
        // UXML inline styles are NOT visible through element.style (it reads 0), only via layout.
        public static void OnFirstLayout(VisualElement el, Action action)
        {
            if (el == null || action == null) return;
            EventCallback<GeometryChangedEvent> cb = null;
            cb = _ => { el.UnregisterCallback(cb); action(); };
            el.RegisterCallback(cb);
        }

        // Figma exports rows (screen title + tag, combat banner + round + turn bar) as absolutely
        // positioned siblings spaced for the design font. The game's fonts and runtime strings are
        // often wider, so a single-line label runs into what follows it. After layout, push those
        // followers right just enough to clear the text; rows that already fit are left untouched.
        // Only row leaders are handled: Display-font titles automatically, other labels via KeepRowClear.
        // (Applying it to every label shoves body text that is meant to clip, e.g. shop descriptions.)
        const float RowGap = 8f;
        const string RowClearClass = "uitk-row-clear";
        const string RowLeaderClass = "uitk-row-leader";

        public static void KeepRowClear(UiLabel label)
        {
            if (label == null) return;
            label.AddToClassList(RowLeaderClass);
            if (label.ClassListContains(RowClearClass)) return;
            label.AddToClassList(RowClearClass);
            label.RegisterCallback<GeometryChangedEvent>(_ => PushFollowers(label));
        }
        sealed class DesignX { public float left; public float applied = float.NaN; }
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, DesignX> designX =
            new System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, DesignX>();

        static void KeepRowsClear(VisualElement root)
        {
            if (root == null) return;
            root.Query<UiLabel>().ForEach(l =>
            {
                if (l.ClassListContains(RowClearClass)) return;
                l.AddToClassList(RowClearClass);
                l.RegisterCallback<GeometryChangedEvent>(_ => PushFollowers(l));
            });
        }

        // The element's authored left, remembered across our own pushes; re-read if other code moved it.
        static float DesignLeft(VisualElement el)
        {
            var d = designX.GetOrCreateValue(el);
            float now = el.resolvedStyle.left;
            if (float.IsNaN(d.applied) || Mathf.Abs(now - d.applied) > 0.5f) d.left = now;
            return d.left;
        }

        static void PushFollowers(UiLabel l)
        {
            var parent = l.parent;
            var rs = l.resolvedStyle;
            if (parent == null || string.IsNullOrEmpty(l.text) || rs.display == DisplayStyle.None
                || rs.position != Position.Absolute || rs.whiteSpace != WhiteSpace.NoWrap)
                return;
            var font = rs.unityFontDefinition.fontAsset;
            if (!l.ClassListContains(RowLeaderClass) && (font == null || font.name != "FelnaDisplayFont")) return;
            // Only left-aligned text grows rightward; centred/right-aligned labels own their box.
            var align = rs.unityTextAlign;
            if (align != TextAnchor.UpperLeft && align != TextAnchor.MiddleLeft && align != TextAnchor.LowerLeft) return;

            var r = l.layout;
            float myLeft = DesignLeft(l);
            var row = new List<VisualElement>();
            foreach (var s in parent.Children())
            {
                if (s == l || s.resolvedStyle.position != Position.Absolute || s.resolvedStyle.display == DisplayStyle.None) continue;
                if (s.style.left.keyword == StyleKeyword.Auto) continue; // right-anchored, e.g. AnchorRight
                var sr = s.layout;
                // Same row = vertical centres within half the smaller height; a big title's tall
                // box merely overlapping the subtitle line below it doesn't count.
                if (Mathf.Abs(sr.center.y - r.center.y) > 0.5f * Mathf.Min(sr.height, r.height)) continue;
                if (DesignLeft(s) <= myLeft) continue;                    // behind/under the label
                row.Add(s);
            }
            if (row.Count == 0) return;
            row.Sort((a, b) => DesignLeft(a).CompareTo(DesignLeft(b)));

            float cursor = r.xMin + l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x + RowGap;
            foreach (var s in row)
            {
                float left = Mathf.Max(DesignLeft(s), cursor);
                if (Mathf.Abs(s.resolvedStyle.left - left) > 0.5f)
                {
                    s.style.left = left;
                    designX.GetOrCreateValue(s).applied = left;
                }
                cursor = left + s.layout.width + RowGap;
            }
        }

        public static VisualElement InstantiateTemplate(string templateKey)
        {
            var catalog = FigmaScreenCatalog.Instance;
            var entry = catalog != null ? catalog.Get(templateKey) : LoadEditorEntry(templateKey);
            if (entry?.uxml == null) return new VisualElement();
            var container = entry.uxml.CloneTree();
            var root = container.childCount == 1 ? container[0] : container;
            AttachSharedStyles(root, catalog, entry);
            RenameCurrencyTerms(root);
            return root;
        }

        public static void SetFadeAlpha(float alpha)
        {
            if (!Application.isPlaying) return;
            EnsureFadeOverlay();
            if (fadeElement == null) return;
            fadeElement.style.opacity = alpha;
            bool visible = alpha > 0.001f;
            fadeElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (fadeDoc != null && fadeDoc.rootVisualElement != null)
                fadeDoc.rootVisualElement.pickingMode = PickingMode.Ignore;
        }

        static void EnsureFadeOverlay()
        {
            if (fadeDoc != null && fadeElement != null) return;
            var go = new GameObject("UiFadeOverlay");
            UnityEngine.Object.DontDestroyOnLoad(go);
            fadeDoc = go.AddComponent<UIDocument>();
            fadeDoc.sortingOrder = 1000;
            var catalog = FigmaScreenCatalog.Instance;
            var ps = catalog != null ? catalog.GetPanelSettings() : LoadEditorPanelSettings();
            if (ps != null) fadeDoc.panelSettings = ps;
            fadeElement = new VisualElement();
            fadeElement.AddToClassList("uitk-fade-overlay");
            fadeElement.pickingMode = PickingMode.Ignore;
            fadeElement.style.display = DisplayStyle.None;
            var tokens = catalog != null ? catalog.GetTokens() : null;
            var runtime = catalog != null ? catalog.GetRuntimeStylesheet() : null;
            if (tokens != null) fadeDoc.rootVisualElement.styleSheets.Add(tokens);
            if (runtime != null) fadeDoc.rootVisualElement.styleSheets.Add(runtime);
            fadeDoc.rootVisualElement.pickingMode = PickingMode.Ignore;
            fadeDoc.rootVisualElement.Add(fadeElement);
        }

        static void AttachSharedStyles(VisualElement root, FigmaScreenCatalog catalog, FigmaScreenCatalog.Entry entry)
        {
            if (root == null) return;
            var tokens = catalog != null ? catalog.GetTokens() : LoadEditorStyle($"{FigmaScreenCatalog.FigmaFolder}/tokens.uss");
            var runtime = catalog != null ? catalog.GetRuntimeStylesheet() : LoadEditorStyle("Assets/UI/felna-runtime.uss");
            if (tokens != null && !root.styleSheets.Contains(tokens)) root.styleSheets.Add(tokens);
            if (runtime != null && !root.styleSheets.Contains(runtime)) root.styleSheets.Add(runtime);
            if (entry?.uss != null && !root.styleSheets.Contains(entry.uss)) root.styleSheets.Add(entry.uss);
        }

        static FigmaScreenCatalog.Entry LoadEditorEntry(string key)
        {
#if UNITY_EDITOR
            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{FigmaScreenCatalog.FigmaFolder}/{key}.uxml");
            var uss = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>($"{FigmaScreenCatalog.FigmaFolder}/{key}.uss");
            return new FigmaScreenCatalog.Entry { key = key, uxml = uxml, uss = uss };
#else
            return null;
#endif
        }

        static PanelSettings LoadEditorPanelSettings()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/FigmaPanelSettings.asset");
#else
            return Resources.Load<PanelSettings>("FigmaPanelSettings");
#endif
        }

        static StyleSheet LoadEditorStyle(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
#else
            return null;
#endif
        }

        // Finds the first UiLabel whose exact text matches (trimmed/case-insensitive option).
        public static UiLabel FindLabel(VisualElement root, string exactText, bool ignoreCase = false)
        {
            if (root == null) return null;
            var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return root.Query<UiLabel>().ToList().FirstOrDefault(l => string.Equals(l.text?.Trim(), exactText, comp));
        }

        // Finds the first Button whose text matches.
        public static Button FindButton(VisualElement root, string exactText, bool ignoreCase = false)
        {
            if (root == null) return null;
            var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return root.Query<Button>().ToList().FirstOrDefault(b => string.Equals(b.text?.Trim(), exactText, comp));
        }

        // Finds a TemplateContainer (Figma <ui:Instance>) whose child UiLabel text-0 or text-1 matches.
        public static VisualElement FindInstanceWithText(VisualElement root, string childName, string exactText, bool ignoreCase = false)
        {
            if (root == null) return null;
            var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (var l in root.Query<UiLabel>(childName).ToList())
            {
                if (string.Equals(l.text?.Trim(), exactText, comp))
                    return l.parent;
            }
            return null;
        }

        public static void SetInstanceText(VisualElement instanceOrWrapper, string childName, string value)
        {
            var lbl = instanceOrWrapper?.Q<UiLabel>(childName);
            if (lbl != null) lbl.text = value ?? "";
        }

        public static void ApplyDefaultFocusVisual(UitkMenu.Item item, VisualElement el = null)
        {
            if (item == null) return;
            var target = el ?? item.element;
            item.visual = on => ApplyDefaultFocusVisual(target, on, item.selected, item.interactable);
            item.visual(false);
        }

        public static void ApplyDefaultFocusVisual(VisualElement el, bool focused, bool selected, bool interactable)
        {
            if (el == null) return;
            el.EnableInClassList("uitk-focus", focused);
            el.EnableInClassList("uitk-selected", selected);
            el.EnableInClassList("uitk-disabled", !interactable);

            var inner = el.childCount == 1 && el is TemplateContainer ? el[0] : el;
            if (inner.ClassListContains("button-flat"))
                inner.EnableInClassList("uitk-unfocused", !focused);
            else if (inner.ClassListContains("button-primary-2"))
                inner.EnableInClassList("uitk-unfocused", !focused);
            else if (inner.ClassListContains("button-slab-2"))
                inner.EnableInClassList("uitk-unfocused", !focused);
            else if (inner.ClassListContains("button-primary") && !(inner is Button))
            {
                var bg = focused ? Palette.Gold1 : Palette.Gold2;
                inner.style.backgroundColor = bg;
                inner.style.borderTopColor = bg;
                inner.style.borderBottomColor = bg;
                inner.style.borderLeftColor = bg;
                inner.style.borderRightColor = bg;
            }

            var cursor = el.Q<VisualElement>("uitkCursor");
            if (cursor != null)
            {
                cursor.EnableInClassList("uitk-cursor-visible", focused);
                cursor.EnableInClassList("uitk-cursor-hidden", !focused);
            }
        }

        public static VisualElement CreateFlatRow(string title, string badgeText = null, string badgeClass = null, float width = 286f)
        {
            var row = new VisualElement();
            row.AddToClassList("uitk-flat-item");
            row.style.width = width;

            var cursor = new UiLabel("▸") { name = "uitkCursor" };
            cursor.AddToClassList("uitk-flat-cursor");
            cursor.AddToClassList("uitk-cursor-hidden");
            row.Add(cursor);

            var lbl = new UiLabel(title) { name = "uitkLabel" };
            lbl.AddToClassList("uitk-flat-label");
            row.Add(lbl);

            if (!string.IsNullOrEmpty(badgeText))
            {
                var badge = new UiLabel(badgeText) { name = "uitkBadge" };
                if (!string.IsNullOrEmpty(badgeClass)) badge.AddToClassList(badgeClass);
                row.Add(badge);
            }
            return row;
        }

        public static VisualElement CreateButton(string text, string styleClass = "button-slab", float? width = null)
        {
            var btn = new VisualElement();
            btn.AddToClassList(styleClass);
            btn.style.justifyContent = Justify.Center;
            btn.style.alignItems = UnityEngine.UIElements.Align.Center;
            if (width.HasValue) btn.style.width = width.Value;
            var lbl = new UiLabel(text) { name = "uitkLabel" };
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (styleClass == "button-primary")
            {
                btn.style.backgroundColor = Palette.Gold2;
                btn.style.borderTopWidth = 2;
                btn.style.borderBottomWidth = 2;
                btn.style.borderLeftWidth = 2;
                btn.style.borderRightWidth = 2;
                btn.style.borderTopColor = Palette.Gold2;
                btn.style.borderBottomColor = Palette.Gold2;
                btn.style.borderLeftColor = Palette.Gold2;
                btn.style.borderRightColor = Palette.Gold2;
                btn.style.paddingTop = 6;
                btn.style.paddingBottom = 6;
                lbl.style.color = Palette.Ink0;
                lbl.style.fontSize = 20;
            }
            btn.Add(lbl);
            return btn;
        }

        public static UiLabel CreateHeader(string text, float width)
        {
            var container = InstantiateTemplate("Section-Header");
            container.style.width = width;
            var lbl = container.Q<UiLabel>("text-0");
            if (lbl != null) lbl.text = text;
            return lbl;
        }

        public static VisualElement CreateSectionHeader(string text, float width = 400f)
        {
            var container = InstantiateTemplate("Section-Header");
            if (container != null)
            {
                container.style.width = width;
                var lbl = container.Q<UiLabel>("text-0");
                if (lbl != null) lbl.text = text;
            }
            return container;
        }

        // Converts an ASCII pixel sprite from Database into a crisp Texture2D for UI Toolkit backgrounds.
        public static Texture2D GetSpriteTexture(string[] rows, string cacheKey)
        {
            if (rows == null || rows.Length == 0) return null;
            var idle = UnitArt.Idle(rows);
            if (idle != null) return idle[0];
            if (!string.IsNullOrEmpty(cacheKey) && spriteTextureCache.TryGetValue(cacheKey, out var cachedTex) && cachedTex != null)
                return cachedTex;

            var sprite = PixelArt.FromRows(rows);
            if (sprite == null || sprite.texture == null) return null;
            sprite.texture.filterMode = FilterMode.Point;
            if (!string.IsNullOrEmpty(cacheKey)) spriteTextureCache[cacheKey] = sprite.texture;
            return sprite.texture;
        }

        public static void SetGaugeFill(VisualElement gaugeContainer, float ratio, Color? overrideColor = null)
        {
            if (gaugeContainer == null || gaugeContainer.childCount == 0) return;
            float clamped = Mathf.Clamp01(ratio);
            gaugeContainer.style.flexDirection = FlexDirection.Row;
            gaugeContainer.style.paddingRight = 1;
            var fill = gaugeContainer[0];
            fill.style.flexBasis = StyleKeyword.Auto;
            fill.style.flexGrow = 0;
            fill.style.height = new Length(100f, LengthUnit.Percent);
            fill.style.width = new Length(clamped * 100f, LengthUnit.Percent);
            if (overrideColor.HasValue)
                fill.style.backgroundColor = overrideColor.Value;
        }

        public static void SetBackgroundContain(VisualElement el, Texture2D tex)
        {
            if (el == null || tex == null) return;
            el.style.backgroundImage = new StyleBackground(tex);
            el.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            el.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            el.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            el.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        }

        // Back/Esc runs onDismiss when given (e.g. "stay here"), otherwise the same as the cancel button.
        public static void ShowConfirmationModal(VisualElement screenRoot, string title, string message, string confirmText, Action onConfirm, Action onCancel = null,
            string cancelText = "CANCEL", Action onDismiss = null)
        {
            if (screenRoot == null) return;
            Sfx.Play(Sound.ModalOpen);
            var backdrop = new VisualElement { name = "UitkConfirmationModal" };
            backdrop.AddToClassList("uitk-modal-backdrop");
            backdrop.pickingMode = PickingMode.Position;

            var panel = new VisualElement();
            panel.AddToClassList("uitk-modal-panel");

            var titleLbl = new UiLabel(title.ToUpperInvariant());
            titleLbl.AddToClassList("uitk-modal-title");
            panel.Add(titleLbl);

            var divider = new VisualElement();
            divider.AddToClassList("uitk-modal-divider");
            panel.Add(divider);

            var msgLbl = new UiLabel(message);
            msgLbl.AddToClassList("uitk-modal-message");
            panel.Add(msgLbl);

            var row = new VisualElement();
            row.AddToClassList("uitk-modal-buttons");

            var menuGo = new GameObject("UitkConfirmationModalMenu");
            var modalMenu = menuGo.AddComponent<UitkMenu>();
            modalMenu.AttachRoot(backdrop);

            void CloseModal()
            {
                backdrop.RemoveFromHierarchy();
                if (Application.isPlaying) UnityEngine.Object.Destroy(menuGo);
                else UnityEngine.Object.DestroyImmediate(menuGo);
            }

            modalMenu.onBack = () => { CloseModal(); (onDismiss ?? onCancel)?.Invoke(); };

            var cancelEl = CreateButton(cancelText, "button-slab", 200f);
            cancelEl.AddToClassList("uitk-modal-btn");
            row.Add(cancelEl);

            var confirmEl = CreateButton(confirmText.ToUpperInvariant(), "button-danger", 200f);
            confirmEl.AddToClassList("uitk-modal-btn");
            row.Add(confirmEl);

            modalMenu.Add(cancelEl, cancelText, () => { CloseModal(); onCancel?.Invoke(); });
            modalMenu.Add(confirmEl, confirmText.ToUpperInvariant(), () => { CloseModal(); onConfirm?.Invoke(); });

            panel.Add(row);
            backdrop.Add(panel);
            screenRoot.Add(backdrop);
        }

        public static ScrollView CreateVerticalScrollView()
        {
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            sv.contentContainer.style.flexShrink = 0;
            sv.AddToClassList("uitk-scroll-view");
            return sv;
        }

        public static void AnchorRight(VisualElement el, float rightPx = 24f)
        {
            if (el == null) return;
            el.style.left = StyleKeyword.Auto;
            el.style.right = rightPx;
        }

        public static void DestroyMenu(ref UitkMenu m)
        {
            if (m == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(m);
            else UnityEngine.Object.DestroyImmediate(m);
            m = null;
        }
    }
}
