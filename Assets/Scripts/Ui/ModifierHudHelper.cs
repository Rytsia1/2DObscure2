using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Shared UI Toolkit helper for rendering active Dungeon Modifier badges & tooltips (GDD Bab 15 §6.2).
    // Uses distinct border shapes + polarity symbols in addition to color so debuffs/drawbacks stay readable.
    public static class ModifierHudHelper
    {
        public static VisualElement BuildStrip(
            VisualElement host,
            RunState run,
            float left,
            float top,
            float maxWidth,
            VisualElement tooltipParent = null)
        {
            if (host == null || run?.ModifierManager == null) return null;
            var summaries = run.ModifierManager.GetGroupSummaries();
            if (summaries.Count == 0) return null;

            var strip = new VisualElement { name = "DungeonModifierStrip" };
            strip.style.position = Position.Absolute;
            strip.style.left = left;
            strip.style.top = top;
            strip.style.maxWidth = maxWidth;
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.flexWrap = Wrap.Wrap;
            strip.style.alignItems = UnityEngine.UIElements.Align.Center;
            strip.pickingMode = PickingMode.Ignore;

            var tipHost = tooltipParent ?? host;
            var tooltip = CreateTooltipElement();
            tipHost.Add(tooltip);

            foreach (var g in summaries)
            {
                var badge = CreateBadge(g);
                string tipText = run.ModifierManager.FormatGroupTooltip(g);
                badge.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    var lbl = tooltip.Q<UiLabel>("ModTipLabel");
                    if (lbl != null) lbl.text = tipText;
                    tooltip.style.left = Mathf.Clamp(left, 12f, 880f);
                    tooltip.style.top = top + 34f;
                    tooltip.style.display = DisplayStyle.Flex;
                    tooltip.BringToFront();
                });
                badge.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    tooltip.style.display = DisplayStyle.None;
                });
                strip.Add(badge);
            }

            host.Add(strip);
            return strip;
        }

        static VisualElement CreateBadge(DungeonModifierGroupSummary g)
        {
            var badge = new VisualElement();
            badge.pickingMode = PickingMode.Position;
            badge.style.flexDirection = FlexDirection.Row;
            badge.style.alignItems = UnityEngine.UIElements.Align.Center;
            badge.style.paddingLeft = 6;
            badge.style.paddingRight = 6;
            badge.style.paddingTop = 2;
            badge.style.paddingBottom = 2;
            badge.style.marginRight = 5;
            badge.style.marginBottom = 4;
            badge.style.borderTopWidth = 2;
            badge.style.borderBottomWidth = 2;
            badge.style.borderLeftWidth = 2;
            badge.style.borderRightWidth = 2;

            // Distinct shape + color per polarity (Bab 15 §6.2)
            Color borderCol;
            Color bgCol;
            string prefix;
            float radius;
            switch (g.def.polarity)
            {
                case ModifierPolarity.BuffWithDrawback:
                    borderCol = Palette.Ember2;
                    bgCol = Palette.Hex(0x2A1A10);
                    prefix = "◆";
                    radius = 4f;
                    break;
                case ModifierPolarity.Debuff:
                    borderCol = Palette.Err;
                    bgCol = Palette.Hex(0x2B1115);
                    prefix = "▼";
                    radius = 10f;
                    break;
                default:
                    borderCol = g.isAtCap ? Palette.Gold1 : Palette.Ok;
                    bgCol = Palette.Ink2;
                    prefix = "▲";
                    radius = 0f;
                    break;
            }

            badge.style.backgroundColor = bgCol;
            badge.style.borderTopColor = borderCol;
            badge.style.borderBottomColor = borderCol;
            badge.style.borderLeftColor = borderCol;
            badge.style.borderRightColor = borderCol;
            badge.style.borderTopLeftRadius = radius;
            badge.style.borderTopRightRadius = radius;
            badge.style.borderBottomLeftRadius = radius;
            badge.style.borderBottomRightRadius = radius;

            string elemTag = g.def.isElementSpecific && g.element.IsValidElement()
                ? $":{g.element.ShortName()}"
                : "";
            string dur = DungeonModifierManager.FormatShortDuration(g.def.durationUnit, g.shortestRemainingDuration);
            string txt = $"{prefix} {g.def.shortCode}{elemTag} ×{g.effectiveStacks} ({dur})";

            var lbl = new UiLabel(txt);
            lbl.pickingMode = PickingMode.Ignore;
            lbl.style.fontSize = 11;
            lbl.style.color = g.def.polarity == ModifierPolarity.Debuff ? Palette.Err
                : g.def.polarity == ModifierPolarity.BuffWithDrawback ? Palette.Ember1
                : Palette.Mist1;
            badge.Add(lbl);
            return badge;
        }

        static VisualElement CreateTooltipElement()
        {
            var box = new VisualElement { name = "DungeonModifierTooltip" };
            box.pickingMode = PickingMode.Ignore;
            box.style.position = Position.Absolute;
            box.style.width = 360;
            box.style.paddingLeft = 10;
            box.style.paddingRight = 10;
            box.style.paddingTop = 8;
            box.style.paddingBottom = 8;
            box.style.backgroundColor = Palette.Ink0;
            box.style.borderTopWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderTopColor = Palette.Gold3;
            box.style.borderBottomColor = Palette.Gold3;
            box.style.borderLeftColor = Palette.Gold3;
            box.style.borderRightColor = Palette.Gold3;
            box.style.display = DisplayStyle.None;

            var lbl = new UiLabel("") { name = "ModTipLabel" };
            lbl.pickingMode = PickingMode.Ignore;
            lbl.style.fontSize = 11;
            lbl.style.color = Palette.Mist1;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            box.Add(lbl);
            return box;
        }
    }
}
