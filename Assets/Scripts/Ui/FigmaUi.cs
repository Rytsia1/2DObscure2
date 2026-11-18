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
            if (ps != null) doc.panelSettings = ps;

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
            {
                root.AddToClassList("uitk-screen-root");
                root.style.width = new Length(100, LengthUnit.Percent);
                root.style.height = new Length(100, LengthUnit.Percent);
                root.style.flexGrow = 1;
            }
            return root;
        }

        public static VisualElement InstantiateTemplate(string templateKey)
        {
            var catalog = FigmaScreenCatalog.Instance;
            var entry = catalog != null ? catalog.Get(templateKey) : LoadEditorEntry(templateKey);
            if (entry?.uxml == null) return new VisualElement();
            var container = entry.uxml.CloneTree();
            var root = container.childCount == 1 ? container[0] : container;
            AttachSharedStyles(root, catalog, entry);
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
            if (width.HasValue) btn.style.width = width.Value;
            var lbl = new UiLabel(text) { name = "uitkLabel" };
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

        public static void ShowConfirmationModal(VisualElement screenRoot, string title, string message, string confirmText, Action onConfirm, Action onCancel = null)
        {
            if (screenRoot == null) return;
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

            modalMenu.onBack = () => { CloseModal(); onCancel?.Invoke(); };

            var cancelEl = CreateButton("CANCEL", "button-slab", 200f);
            cancelEl.AddToClassList("uitk-modal-btn");
            row.Add(cancelEl);

            var confirmEl = CreateButton(confirmText.ToUpperInvariant(), "button-danger", 200f);
            confirmEl.AddToClassList("uitk-modal-btn");
            row.Add(confirmEl);

            modalMenu.Add(cancelEl, "CANCEL", () => { CloseModal(); onCancel?.Invoke(); });
            modalMenu.Add(confirmEl, confirmText.ToUpperInvariant(), () => { CloseModal(); onConfirm?.Invoke(); });

            panel.Add(row);
            backdrop.Add(panel);
            screenRoot.Add(backdrop);
        }
    }
}
