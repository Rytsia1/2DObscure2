using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Shared UI Toolkit helper for rendering active Dungeon Modifier, relic & event-blessing badges with tooltips (GDD Bab 15 §6.2).
    // Uses distinct border shapes + polarity symbols in addition to color so debuffs/drawbacks stay readable.
    public static class ModifierHudHelper
    {
        // Combat HUD strip: dungeon modifiers, then relics, then event blessings. Each badge has a hover tooltip.
        public static VisualElement BuildStrip(
            VisualElement host,
            RunState run,
            float left,
            float top,
            float maxWidth,
            VisualElement tooltipParent = null)
        {
            if (host == null || run == null) return null;
            var badges = new List<(VisualElement badge, string tip)>();

            if (run.ModifierManager != null)
                foreach (var g in run.ModifierManager.GetGroupSummaries())
                    badges.Add((CreateModifierBadge(g), run.ModifierManager.FormatGroupTooltip(g)));

            foreach (var rid in run.Relics)
            {
                if (!Database.Relics.TryGetValue(rid, out var r)) continue;
                badges.Add((Badge("◈", r.name, Palette.Gold1, Palette.Ink2, Palette.Gold1, 0f),
                    $"{r.name.ToUpperInvariant()} [RELIC]\n{(r.Description ?? "").ToUpperInvariant()}\nPERMANENT"));
            }

            foreach (var b in run.BattleBuffs)
            {
                bool buff = StatusInfo.IsBuff(b.id);
                string name = StatusInfo.Name(b.id);
                badges.Add((Badge("✦", name, buff ? Palette.Ok : Palette.Err, buff ? Palette.Ink2 : Palette.Hex(0x2B1115),
                        buff ? Palette.Mist1 : Palette.Err, buff ? 0f : 10f),
                    $"{name.ToUpperInvariant()} [EVENT {(buff ? "BLESSING" : "AFFLICTION")}]\n" +
                    $"EACH BATTLE: PARTY {name.ToUpperInvariant()} ({b.turns} {(b.turns == 1 ? "TURN" : "TURNS")})\n{DungeonModifierManager.UntilDungeonExit}"));
            }

            if (badges.Count == 0) return null;

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

            foreach (var (badge, tipText) in badges)
            {
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

            // Keyboard route to the same text: TAB lists every effect (see ToggleDetails).
            var hint = new UiLabel("TAB: DETAILS") { pickingMode = PickingMode.Ignore };
            hint.style.fontSize = 12;
            hint.style.color = Palette.Mist2;
            hint.style.marginLeft = 4;
            strip.Add(hint);
            strip.userData = (tooltip, string.Join("\n\n", badges.ConvertAll(b => b.tip)), left, top);

            host.Add(strip);
            return strip;
        }

        // Shows (or hides) every badge's tooltip at once, for players without a mouse.
        public static void ToggleDetails(VisualElement strip)
        {
            if (!(strip?.userData is ValueTuple<VisualElement, string, float, float> data)) return;
            var (tooltip, all, left, top) = data;
            bool show = tooltip.style.display != DisplayStyle.Flex;
            var lbl = tooltip.Q<UiLabel>("ModTipLabel");
            if (lbl != null) lbl.text = all;
            tooltip.style.left = Mathf.Clamp(left, 12f, 880f);
            tooltip.style.top = top + 34f;
            tooltip.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show) tooltip.BringToFront();
        }

        // Distinct shape + color per polarity (Bab 15 §6.2)
        static VisualElement CreateModifierBadge(DungeonModifierGroupSummary g)
        {
            string elemTag = g.def.isElementSpecific && g.element.IsValidElement()
                ? $":{g.element.ShortName()}"
                : "";
            string txt = $"{g.def.shortCode}{elemTag} ×{g.effectiveStacks}";
            return g.def.polarity switch
            {
                ModifierPolarity.BuffWithDrawback => Badge("◆", txt, Palette.Ember2, Palette.Hex(0x2A1A10), Palette.Ember1, 4f),
                ModifierPolarity.Debuff => Badge("▼", txt, Palette.Err, Palette.Hex(0x2B1115), Palette.Err, 10f),
                _ => Badge("▲", txt, g.isAtCap ? Palette.Gold1 : Palette.Ok, Palette.Ink2, Palette.Mist1, 0f)
            };
        }

        static VisualElement Badge(string prefix, string text, Color borderCol, Color bgCol, Color textCol, float radius)
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
            badge.style.backgroundColor = bgCol;
            badge.style.borderTopColor = borderCol;
            badge.style.borderBottomColor = borderCol;
            badge.style.borderLeftColor = borderCol;
            badge.style.borderRightColor = borderCol;
            badge.style.borderTopLeftRadius = radius;
            badge.style.borderTopRightRadius = radius;
            badge.style.borderBottomLeftRadius = radius;
            badge.style.borderBottomRightRadius = radius;

            var lbl = new UiLabel($"{prefix} {text}");
            lbl.pickingMode = PickingMode.Ignore;
            lbl.style.fontSize = 11;
            lbl.style.color = textCol;
            badge.Add(lbl);
            return badge;
        }

        static VisualElement CreateTooltipElement()
        {
            var box = new VisualElement { name = "DungeonModifierTooltip" };
            box.pickingMode = PickingMode.Ignore;
            box.style.position = Position.Absolute;
            box.style.width = 420;
            box.style.maxHeight = 290;
            box.style.overflow = Overflow.Hidden;
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
