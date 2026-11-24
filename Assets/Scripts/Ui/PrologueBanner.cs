using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    /// <summary>
    /// Compact non-blocking [TUTORIAL] banner for non-combat Prologue screens and the first Hub arrival.
    /// </summary>
    public static class PrologueBanner
    {
        public static bool ShouldShowPrologue =>
            (Game.I?.Run?.IsPrologue ?? false) && !(Game.I?.Settings?.prologueCleared ?? false);

        public static void TryAttachPrologue(VisualElement root, string text, float top = 56f)
        {
            if (ShouldShowPrologue) Attach(root, text, top);
        }

        public static VisualElement Attach(VisualElement root, string text, float top = 56f)
        {
            if (root == null || string.IsNullOrEmpty(text)) return null;
            var banner = new UiLabel("[TUTORIAL] " + text.ToUpperInvariant())
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 140,
                    top = top,
                    width = 1000,
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
