using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    // Runtime registry of imported Figma UXML/USS assets and shared PanelSettings.
    // Built by FigmaCatalogBuilder into Assets/Resources/FigmaScreenCatalog.asset so runtime
    // loading works identically in Editor, PlayMode tests, and builds without moving Figma files.
    public class FigmaScreenCatalog : ScriptableObject
    {
        public const string ResourceName = "FigmaScreenCatalog";
        public const string FigmaFolder = "Assets/Figma/FELNA---UI-UX-Design";

        [Serializable]
        public class Entry
        {
            public string key;
            public VisualTreeAsset uxml;
            public StyleSheet uss;
        }

        public PanelSettings panelSettings;
        public StyleSheet tokensStylesheet;
        public StyleSheet runtimeStylesheet;
        public List<Entry> entries = new List<Entry>();

        Dictionary<string, Entry> lookup;
        static FigmaScreenCatalog cached;

        public static FigmaScreenCatalog Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<FigmaScreenCatalog>(ResourceName);
                return cached;
            }
        }

        public static void ClearCache() => cached = null;

        public Entry Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            EnsureLookup();
            if (lookup.TryGetValue(key, out var e)) return e;
#if UNITY_EDITOR
            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{FigmaFolder}/{key}.uxml");
            var uss = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>($"{FigmaFolder}/{key}.uss");
            if (uxml != null || uss != null)
            {
                var created = new Entry { key = key, uxml = uxml, uss = uss };
                lookup[key] = created;
                return created;
            }
#endif
            return null;
        }

        public PanelSettings GetPanelSettings()
        {
            if (panelSettings != null) return panelSettings;
            var ps = Resources.Load<PanelSettings>("FigmaPanelSettings");
            if (ps != null) return ps;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/FigmaPanelSettings.asset");
#else
            return null;
#endif
        }

        public StyleSheet GetTokens()
        {
            if (tokensStylesheet != null) return tokensStylesheet;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>($"{FigmaFolder}/tokens.uss");
#else
            return null;
#endif
        }

        public StyleSheet GetRuntimeStylesheet()
        {
            if (runtimeStylesheet != null) return runtimeStylesheet;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/felna-runtime.uss");
#else
            return null;
#endif
        }

        void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in entries)
            {
                if (e != null && !string.IsNullOrEmpty(e.key))
                    lookup[e.key] = e;
            }
        }
    }
}
