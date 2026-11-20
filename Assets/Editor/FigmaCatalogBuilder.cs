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
        const string DisplayTtfPath = "Assets/UI/Fonts/FelnaDisplay.ttf";
        const string DisplayAssetPath = "Assets/UI/Fonts/FelnaDisplayFont.asset";
        const string GothicTtfPath = "Assets/UI/Fonts/FelnaGothic.ttf";
        const string GothicAssetPath = "Assets/UI/Fonts/FelnaGothicFont.asset";

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

            var symbolAsset = EnsureSymbolFallbackFontAsset();
            var fontAsset = EnsureFontAsset(FontTtfPath, FontAssetPath, symbolAsset, null);
            EnsureFontAsset(DisplayTtfPath, DisplayAssetPath, fontAsset, symbolAsset);
            EnsureFontAsset(GothicTtfPath, GothicAssetPath, fontAsset, symbolAsset, samplingSize: GothicSamplingSize);
            var textSettings = EnsureTextSettings(fontAsset, symbolAsset);
            // USS/TSS bake font url() refs at import time; reimport both so refs to just-created font assets resolve.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(RuntimeUssPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceUpdate);

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

        // DotGothic16 as a high-res SDF, NOT a RASTER bitmap atlas. With RASTER, UI Toolkit layout floors each
        // glyph advance to whole screen pixels but draws it unrounded, so at sizes whose advance is fractional
        // (14/18/22px at 1.5x panel scale) the last glyphs wrapped onto a 2nd line and overlapped the text below
        // ("+1" read as "¥}"), and point-sampled downscaling made strokes randomly 1px or 2px.
        // 64pt keeps the square dots square; SDF scales evenly to any size and measures what it draws.
        const int GothicSamplingSize = 64;

        static bool IsPixelFontStale(FontAsset fa) =>
            fa == null || fa.atlasRenderMode != UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA
            || fa.faceInfo.pointSize != GothicSamplingSize;

        static FontAsset EnsureFontAsset(string ttfPath, string assetPath, FontAsset fallbackA, FontAsset fallbackB, int samplingSize = 0)
        {
            var fallbacks = new[] { fallbackA, fallbackB };
            var fa = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);
            if (samplingSize > 0 && fa != null && IsPixelFontStale(fa))
            {
                AssetDatabase.DeleteAsset(assetPath);
                fa = null;
            }
            if (fa == null || fa.sourceFontFile == null)
            {
                AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceUpdate);
                var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (ttf == null) return null;

                fa = samplingSize > 0
                    ? FontAsset.CreateFontAsset(ttf, samplingSize, samplingSize / 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                        1024, 1024, AtlasPopulationMode.Dynamic, true)
                    : FontAsset.CreateFontAsset(ttf);
                if (fa == null) return null;
                AssetDatabase.CreateAsset(fa, assetPath);
                if (fa.material != null) AssetDatabase.AddObjectToAsset(fa.material, fa);
                if (fa.atlasTextures != null)
                {
                    foreach (var tex in fa.atlasTextures)
                        if (tex != null) AssetDatabase.AddObjectToAsset(tex, fa);
                }
            }

            {
                if (fa.fallbackFontAssetTable == null)
                    fa.fallbackFontAssetTable = new System.Collections.Generic.List<FontAsset>();
                bool changed = false;
                foreach (var fb in fallbacks)
                {
                    if (fb != null && fb != fa && !fa.fallbackFontAssetTable.Contains(fb))
                    {
                        fa.fallbackFontAssetTable.Add(fb);
                        changed = true;
                    }
                }
                if (changed) EditorUtility.SetDirty(fa);
            }
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static PanelTextSettings EnsureTextSettings(FontAsset defaultFont, FontAsset symbolFa)
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
            var dispFa = AssetDatabase.LoadAssetAtPath<FontAsset>(DisplayAssetPath);
            var gothFa = AssetDatabase.LoadAssetAtPath<FontAsset>(GothicAssetPath);
            string diskFolder = Path.Combine(Application.dataPath, "Figma", "FELNA---UI-UX-Design");
            int diskCount = Directory.Exists(diskFolder) ? Directory.GetFiles(diskFolder, "*.uxml", SearchOption.TopDirectoryOnly).Length : 0;
            if (catalog == null || ps == null || ps.textSettings == null || dispFa == null || gothFa == null || catalog.entries == null || catalog.entries.Count == 0 || catalog.entries.Count != diskCount || ps.themeStyleSheet == null || IsPixelFontStale(gothFa) || HasUnresolvedRuntimeStylesheetRefs(dispFa, gothFa))
                BuildAll();
        }

        static bool HasUnresolvedRuntimeStylesheetRefs(FontAsset dispFa, FontAsset gothFa)
        {
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(RuntimeUssPath);
            if (uss == null) return true;
            var so = new SerializedObject(uss);
            var strings = so.FindProperty("strings");
            if (strings != null)
            {
                for (int i = 0; i < strings.arraySize; i++)
                {
                    if (strings.GetArrayElementAtIndex(i).stringValue.Contains("project://database/"))
                        return true;
                }
            }
            var assets = so.FindProperty("assets");
            if (assets == null) return true;
            bool hasDisp = false, hasGoth = false;
            for (int i = 0; i < assets.arraySize; i++)
            {
                var obj = assets.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj == dispFa) hasDisp = true;
                if (obj == gothFa) hasGoth = true;
            }
            return !hasDisp || !hasGoth;
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
