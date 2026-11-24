using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    /// <summary>
    /// Compact non-blocking [TUTORIAL] banner and step indicator for Prologue screens and the first Hub arrival.
    /// </summary>
    public static class PrologueBanner
    {
        public static bool ShouldShowPrologue => Game.I?.Run?.IsPrologue ?? false;

        public static string StepLabel()
        {
            var run = Game.I?.Run;
            if (run == null || !run.IsPrologue || run.Map?.nodes == null) return "TUTORIAL";
            int total = run.Map.nodes.Length;
            int step = Mathf.Clamp(run.Cleared.Count + 1, 1, total);
            return $"TUTORIAL · STEP {step}/{total}";
        }

        public static void TryAttachPrologue(VisualElement root, string text, float top = 56f, float left = 140f, float width = 1000f)
        {
            if (!ShouldShowPrologue) return;
            AttachStepBadge(root);
            Attach(root, text, top, left, width);
        }

        public static VisualElement AttachStepBadge(VisualElement root, string customLabel = null)
        {
            if (root == null) return null;
            var badge = new UiLabel(customLabel ?? StepLabel())
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    right = 16,
                    top = 12,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 4,
                    paddingBottom = 4,
                    fontSize = 12,
                    color = Palette.Ink0,
                    backgroundColor = Palette.Gold1,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    unityFontStyleAndWeight = FontStyle.Bold,
                },
            };
            root.Add(badge);
            return badge;
        }

        public static VisualElement Attach(VisualElement root, string text, float top = 56f, float left = 140f, float width = 1000f)
        {
            if (root == null || string.IsNullOrEmpty(text)) return null;
            var banner = new UiLabel(text.ToUpperInvariant())
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = left,
                    top = top,
                    width = width,
                    paddingLeft = 14,
                    paddingRight = 14,
                    paddingTop = 6,
                    paddingBottom = 6,
                    fontSize = 13,
                    color = Palette.Gold1,
                    backgroundColor = Palette.Hex(0x070A14, 234),
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = Palette.Gold2,
                    borderBottomColor = Palette.Gold2,
                    borderLeftColor = Palette.Gold2,
                    borderRightColor = Palette.Gold2,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    whiteSpace = WhiteSpace.Normal,
                },
            };
            root.Add(banner);
            return banner;
        }
    }
}
