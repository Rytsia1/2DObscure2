using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Obscure.Editor
{
    [InitializeOnLoad]
    public static class FigmaCatalogBuilder
    {
        const string PanelSettingsPath = "Assets/Resources/FigmaPanelSettings.asset";
        const string TextSettingsPath = "Assets/Resources/FigmaTextSettings.asset";
        const string CatalogPath = "Assets/Resources/FigmaScreenCatalog.asset";
        const string ThemePath = "Assets/Resources/FigmaRuntimeTheme.tss";
        const string RuntimeUssPath = "Assets/UI/felna-runtime.uss";
        const string FontTtfPath = "Assets/UI/Fonts/FelnaUI.ttf";
        const string FontAssetPath = "Assets/UI/Fonts/FelnaUIFont.asset";

        static FigmaCatalogBuilder()
        {
            EditorApplication.delayCall += EnsureCatalogUpToDate;
        }

        [MenuItem("FELNA/Build Figma UI Assets")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/UI");
            EnsureFolder("Assets/UI/Fonts");

            var fontAsset = EnsureFontAsset();
            var textSettings = EnsureTextSettings(fontAsset);

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceUpdate);
                theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            }

            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PanelSettingsPath);
            }

            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1280, 720);
            ps.screenMatchMode = PanelScreenMatchMode.Expand;
            ps.match = 0.5f;
            ps.sortingOrder = 10;
            ps.clearColor = false;
            ps.targetTexture = null;
            if (theme != null) ps.themeStyleSheet = theme;
            if (textSettings != null) ps.textSettings = textSettings;
            EditorUtility.SetDirty(ps);

            var catalog = AssetDatabase.LoadAssetAtPath<FigmaScreenCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FigmaScreenCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.panelSettings = ps;
            catalog.tokensStylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{FigmaScreenCatalog.FigmaFolder}/tokens.uss");
            catalog.runtimeStylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(RuntimeUssPath);
            catalog.entries.Clear();

            string diskFolder = Path.Combine(Application.dataPath, "Figma", "FELNA---UI-UX-Design");
            if (Directory.Exists(diskFolder))
            {
                var uxmlFiles = Directory.GetFiles(diskFolder, "*.uxml", SearchOption.TopDirectoryOnly)
                    .OrderBy(p => p)
                    .ToArray();
                foreach (string file in uxmlFiles)
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    string uxmlAsset = $"{FigmaScreenCatalog.FigmaFolder}/{key}.uxml";
                    string ussAsset = $"{FigmaScreenCatalog.FigmaFolder}/{key}.uss";
                    var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAsset);
                    var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussAsset);
                    if (vta != null)
                        catalog.entries.Add(new FigmaScreenCatalog.Entry { key = key, uxml = vta, uss = ss });
                }
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            FigmaScreenCatalog.ClearCache();
        }

        static FontAsset EnsureFontAsset()
        {
            var fa = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPath);
            if (fa != null && fa.sourceFontFile != null)
                return fa;

            AssetDatabase.ImportAsset(FontTtfPath, ImportAssetOptions.ForceUpdate);
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (ttf == null) return null;

            fa = FontAsset.CreateFontAsset(ttf);
            if (fa == null) return null;
            AssetDatabase.CreateAsset(fa, FontAssetPath);
            if (fa.material != null) AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (fa.atlasTextures != null)
            {
                foreach (var tex in fa.atlasTextures)
                    if (tex != null) AssetDatabase.AddObjectToAsset(tex, fa);
            }
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static PanelTextSettings EnsureTextSettings(FontAsset defaultFont)
        {
            var ts = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(TextSettingsPath);
            if (ts == null)
            {
                ts = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(ts, TextSettingsPath);
            }
            if (defaultFont != null)
            {
                ts.defaultFontAsset = defaultFont;
                var symbolFa = EnsureSymbolFallbackFontAsset();
                if (symbolFa != null)
                {
                    if (defaultFont.fallbackFontAssetTable == null)
                        defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<FontAsset>();
                    if (!defaultFont.fallbackFontAssetTable.Contains(symbolFa))
                        defaultFont.fallbackFontAssetTable.Add(symbolFa);
                    EditorUtility.SetDirty(defaultFont);
                }
            }
            EditorUtility.SetDirty(ts);
            return ts;
        }

        static FontAsset EnsureSymbolFallbackFontAsset()
        {
            const string symPath = "Assets/UI/Fonts/FelnaSymbolsFont.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(symPath);
            if (existing != null) return existing;

            var osFonts = Font.GetOSInstalledFontNames();
            string chosen = osFonts.FirstOrDefault(n => n == "Segoe UI Symbol")
                ?? osFonts.FirstOrDefault(n => n == "Segoe UI Emoji")
                ?? osFonts.FirstOrDefault(n => n == "Arial");
            if (string.IsNullOrEmpty(chosen)) return null;

            var osFont = Font.CreateDynamicFontFromOSFont(chosen, 36);
            if (osFont == null) return null;
            var fa = FontAsset.CreateFontAsset(osFont);
            if (fa == null) return null;
            AssetDatabase.CreateAsset(fa, symPath);
            if (fa.material != null) AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (fa.atlasTextures != null)
            {
                foreach (var tex in fa.atlasTextures)
                    if (tex != null) AssetDatabase.AddObjectToAsset(tex, fa);
            }
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static void EnsureCatalogUpToDate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var catalog = AssetDatabase.LoadAssetAtPath<FigmaScreenCatalog>(CatalogPath);
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (catalog == null || ps == null || ps.textSettings == null || catalog.entries == null || catalog.entries.Count == 0 || ps.themeStyleSheet == null)
                BuildAll();
        }

        static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
