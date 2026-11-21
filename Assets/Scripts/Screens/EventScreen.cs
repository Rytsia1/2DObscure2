using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Shows RunState.CurrentEvent — migrated to Figma 18---Event---Whispering-Shrine.uxml (1280x720 UI Toolkit).
    public class EventScreen : GameScreen
    {
        VisualElement choiceHost;
        UitkMenu menu;
        EventDef currentEvent;

        protected override void Build()
        {
            currentEvent = Run?.CurrentEvent ?? Database.Events.Values.First();
            MountFigma("18---Event---Whispering-Shrine");

            var d1 = UiRoot.Q<UiLabel>(className: "d1-vars");
            if (d1 != null)
            {
                d1.text = $"D{Run?.Act ?? 1}";
                d1.style.marginRight = 8;
            }

            var dung = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-2");
            if (dung != null) dung.text = Run?.Map?.name ?? "Dungeon";

            var node = UiRoot.Q<UiLabel>(className: "node-4-event-vars");
            if (node != null)
            {
                node.text = $"· NODE {Run?.Floor ?? 1} · EVENT";
                node.style.marginLeft = 8;
            }

            var artEl = UiRoot.Q<VisualElement>("EventArtwork");
            var sceneTex = IconArt.EventScene(currentEvent.id, currentEvent.category);
            if (artEl != null && sceneTex != null)
                FigmaUi.SetBackgroundContain(artEl, sceneTex);

            int gold = Run != null ? Run.Gold : 0;
            var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
            if (runGold != null) FigmaUi.SetInstanceText(runGold, "text-0", $"{gold} SPOILS");

            var eyebrow = UiRoot.Q<UiLabel>(className: "event-vars-2");
            if (eyebrow != null)
            {
                eyebrow.text = currentEvent.category != EventCategory.General
                    ? $"{FormatCategory(currentEvent.category)} · {currentEvent.rarity.ToString().ToUpperInvariant()}"
                    : "EVENT";
            }

            var title = UiRoot.Q<UiLabel>(className: "whispering-shrine-vars");
            if (title != null) title.text = currentEvent.title;

            var body = UiRoot.Q<UiLabel>(className: "a-cracked-shrine-hums-candles-relight-as-you-approach-and-a-voice-asks-what-you-will-give-vars");
            if (body != null) body.style.display = DisplayStyle.None;

            // Clear baked static choice rows and replace with a unified right-column layout inside panel-gold__i-14
            foreach (var cls in new[] { "choice-1-vars", "choice-2-vars", "choice-3-vars" })
            {
                var c = UiRoot.Q<VisualElement>(className: cls);
                if (c != null) c.style.display = DisplayStyle.None;
            }

            var screen = UiRoot.Q<VisualElement>(className: "_18-event-whispering-shrine") ?? UiRoot;

            var rightCol = new VisualElement
            {
                name = "EventRightColumn",
                style =
                {
                    position = Position.Absolute,
                    left = 544,
                    top = 172,
                    width = 622,
                    height = 478,
                    flexDirection = FlexDirection.Column,
                    overflow = Overflow.Hidden
                }
            };
            screen.Add(rightCol);

            var bodyCopy = new UiLabel(currentEvent.text.ToUpperInvariant())
            {
                style =
                {
                    fontSize = 15,
                    color = Palette.Mist1,
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 10,
                    flexShrink = 0
                }
            };
            rightCol.Add(bodyCopy);

            BuildTagAndInsightArea(rightCol);

            var choiceScroll = FigmaUi.CreateVerticalScrollView();
            choiceScroll.name = "EventChoiceHost";
            choiceScroll.style.flexGrow = 1;
            choiceScroll.style.flexBasis = 0;
            choiceScroll.style.minHeight = 0;
            choiceHost = choiceScroll;
            rightCol.Add(choiceHost);

            ShowChoices();
        }

        void BuildTagAndInsightArea(VisualElement container)
        {
            var metaHost = new VisualElement { name = "EventMetaHost" };
            metaHost.style.flexDirection = FlexDirection.Column;
            metaHost.style.flexShrink = 0;
            bool hasMeta = false;

            if (currentEvent.tags != null && currentEvent.tags.Length > 0)
            {
                hasMeta = true;
                var tagRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 6 } };
                foreach (var rawTag in currentEvent.tags)
                {
                    var chip = new UiLabel(rawTag.Replace("_", " ").ToUpperInvariant());
                    chip.style.fontSize = 11;
                    chip.style.color = Palette.Mist2;
                    chip.style.backgroundColor = Palette.Ink2;
                    chip.style.paddingLeft = 7;
                    chip.style.paddingRight = 7;
                    chip.style.paddingTop = 2;
                    chip.style.paddingBottom = 2;
                    chip.style.marginRight = 6;
                    chip.style.marginBottom = 4;
                    chip.style.borderTopWidth = 1;
                    chip.style.borderBottomWidth = 1;
                    chip.style.borderLeftWidth = 1;
                    chip.style.borderRightWidth = 1;
                    chip.style.borderTopColor = Palette.Ink5;
                    chip.style.borderBottomColor = Palette.Ink5;
                    chip.style.borderLeftColor = Palette.Ink5;
                    chip.style.borderRightColor = Palette.Ink5;
                    tagRow.Add(chip);
                }
                metaHost.Add(tagRow);
            }

            var insights = GatherInsights();
            if (insights.Count > 0)
            {
                hasMeta = true;
                var box = new VisualElement();
                box.style.paddingLeft = 10;
                box.style.paddingRight = 10;
                box.style.paddingTop = 6;
                box.style.paddingBottom = 6;
                box.style.marginBottom = 6;
                box.style.backgroundColor = Palette.Ink2;
                box.style.borderLeftWidth = 3;
                box.style.borderLeftColor = Palette.Gold2;
                foreach (var line in insights)
                {
                    var lbl = new UiLabel(line);
                    lbl.style.fontSize = 12;
                    lbl.style.color = Palette.Gold1;
                    lbl.style.whiteSpace = WhiteSpace.Normal;
                    lbl.style.marginBottom = 2;
                    box.Add(lbl);
                }
                metaHost.Add(box);
            }

            if (hasMeta)
            {
                metaHost.style.marginBottom = 6;
                container.Add(metaHost);
            }
        }

        List<string> GatherInsights()
        {
            var list = new List<string>();
            if (Run == null) return list;

            if (Run.RevealsSupernaturalSource(currentEvent) && !string.IsNullOrEmpty(currentEvent.supernaturalSourceClue))
                list.Add($"[LUMINESCENT INSIGHT] {currentEvent.supernaturalSourceClue.ToUpperInvariant()}");

            if (Run.HasHalflingHazardReduction && currentEvent.HasTag(EventTags.Hazard))
                list.Add("[SMALL AND NIMBLE] HALFLING REDUCES PARTY HAZARD DAMAGE BY 50%.");

            if (Run.HasRace("human"))
            {
                if (!currentEvent.HasAnyRacialChoice())
                    list.Add("[ADAPTABILITY] NO RACIAL OPTION PRESENT: IMPROVISE AVAILABLE.");
                else if (Run.HumanAdaptabilityUsesRemaining > 0)
                    list.Add("[ADAPTABILITY] 1 BORROW USE LEFT THIS DUNGEON (75% EFFECT).");
            }

            if (Run.RelicManager != null && currentEvent.tags != null)
            {
                var seen = new HashSet<string>();
                foreach (var tag in currentEvent.tags)
                {
                    var clue = Run.RelicManager.GetAdditionalEventClue(tag);
                    if (!string.IsNullOrEmpty(clue) && seen.Add(clue))
                        list.Add(clue.ToUpperInvariant());
                }
                if (Run.RelicManager.HasRelic("R005") && !string.IsNullOrEmpty(currentEvent.hiddenClue)
                    && (currentEvent.HasTag(EventTags.Hidden) || currentEvent.HasTag(EventTags.Trap)))
                {
                    list.Add($"[WARDEN'S BROKEN KEY] {currentEvent.hiddenClue.ToUpperInvariant()}");
                }
            }

            return list;
        }

        void ResetMenu()
        {
            if (menu != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(menu);
                else UnityEngine.Object.DestroyImmediate(menu);
            }
            choiceHost.Clear();
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
        }

        void ShowChoices()
        {
            ResetMenu();
            var visible = new List<(int index, EventChoice choice, RunState.EventChoiceStatus status)>();
            for (int i = 0; i < currentEvent.choices.Length; i++)
            {
                var c = currentEvent.choices[i];
                var status = Run != null
                    ? Run.GetEventChoiceStatus(i)
                    : new RunState.EventChoiceStatus { isVisible = !c.isImprovise, effectScale = 1f };
                if (status.isVisible)
                    visible.Add((i, c, status));
            }

            bool compact = visible.Count >= 5;
            for (int vIdx = 0; vIdx < visible.Count; vIdx++)
            {
                var entry = visible[vIdx];
                int index = entry.index;
                int goldBack = entry.choice.goldCost > 0 ? (Run?.PreviewEventGold(index) ?? entry.choice.gold) : 0;
                AddChoiceCard(vIdx + 1, entry.choice, entry.status, goldBack, compact, () => Choose(index));
            }
        }

        void AddChoiceCard(int number, EventChoice c, RunState.EventChoiceStatus status, int goldBack, bool compact, Action choose)
        {
            bool enabled = !status.isBlocked;
            var card = new VisualElement();
            card.style.minHeight = compact ? 46 : 56;
            card.style.marginBottom = compact ? 5 : 8;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.paddingTop = compact ? 4 : 6;
            card.style.paddingBottom = compact ? 4 : 6;
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = UnityEngine.UIElements.Align.Center;
            card.style.overflow = Overflow.Hidden;
            card.style.borderTopWidth = 2;
            card.style.borderBottomWidth = 2;
            card.style.borderLeftWidth = 2;
            card.style.borderRightWidth = 2;

            var numLbl = new UiLabel(number.ToString());
            numLbl.style.fontSize = compact ? 18 : 20;
            numLbl.style.width = 28;
            numLbl.style.color = enabled ? Palette.Mist2 : Palette.Mist3;
            card.Add(numLbl);

            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            textCol.style.flexShrink = 1;
            textCol.style.flexBasis = 0;
            textCol.style.marginRight = 10;
            textCol.style.flexDirection = FlexDirection.Column;
            textCol.style.justifyContent = Justify.Center;

            var titleLbl = new UiLabel(c.label);
            titleLbl.style.fontSize = compact ? 16 : 18;
            titleLbl.style.color = enabled ? Palette.Mist1 : Palette.Mist3;
            titleLbl.style.whiteSpace = WhiteSpace.Normal;
            textCol.Add(titleLbl);

            string sub = enabled ? ChoiceCostSubtitle(c, goldBack) : status.blockedReason.ToUpperInvariant();
            if (!string.IsNullOrEmpty(sub))
            {
                var subLbl = new UiLabel(sub);
                subLbl.style.fontSize = compact ? 11 : 12;
                subLbl.style.color = enabled ? Palette.Mist2 : Palette.Mist3;
                subLbl.style.whiteSpace = WhiteSpace.Normal;
                textCol.Add(subLbl);
            }
            card.Add(textCol);

            string tagText = DetermineCardTag(c, status, goldBack);
            if (tagText != null)
            {
                var tag = FigmaUi.InstantiateTemplate(enabled ? "Tag-Temp" : "Tag-Cond");
                if (tag != null)
                {
                    tag.style.flexShrink = 0;
                    if (tag.childCount > 0) tag[0].style.flexShrink = 0;
                    FigmaUi.SetInstanceText(tag, "text-0", tagText);
                    card.Add(tag);
                }
            }

            choiceHost.Add(card);
            var item = menu.Add(card, c.label, choose, interactable: enabled, tooltip: sub);
            item.visual = on =>
            {
                card.style.backgroundColor = !enabled ? Palette.Ink1 : on ? Palette.Hex(0x2B2210) : Palette.Ink2;
                var border = !enabled ? Palette.Ink5 : on ? Palette.Gold2 : Palette.Ink5;
                card.style.borderTopColor = border;
                card.style.borderBottomColor = border;
                card.style.borderLeftColor = border;
                card.style.borderRightColor = border;
                numLbl.style.color = !enabled ? Palette.Mist3 : on ? Palette.Gold2 : Palette.Mist2;
            };
            item.visual(false);
        }

        // Net is shown only when the return is guaranteed; on a risky choice it would spoil the hidden risk, so cost alone.
        static bool GoldBackGuaranteed(EventChoice c) => c.riskChancePercent <= 0 || c.riskGrantsRewardOnFail;

        static string DetermineCardTag(EventChoice c, RunState.EventChoiceStatus status, int goldBack)
        {
            if (status.isBlocked) return status.blockedReason.ToUpperInvariant();
            if (status.usesAdaptabilityBorrow) return "ADAPTABILITY 75%";
            if (c.isImprovise) return "HUMAN · IMPROVISE";
            if (!string.IsNullOrEmpty(c.requiredRace)) return c.requiredRace.ToUpperInvariant();
            if (c.goldCost > 0)
                return goldBack > 0 && GoldBackGuaranteed(c) ? $"NET {goldBack - c.goldCost:+0;-0;0} SPOILS" : $"-{c.goldCost} SPOILS";
            if (!string.IsNullOrEmpty(c.requiredItem) && Database.Items.TryGetValue(c.requiredItem, out var reqItm))
                return $"USE {reqItm.name.ToUpperInvariant()}";
            if (!string.IsNullOrEmpty(c.requiredMaterialId))
            {
                string matName = Database.Materials.TryGetValue(c.requiredMaterialId, out var mat) ? mat.name : c.requiredMaterialId;
                return $"-{Math.Max(1, c.requiredMaterialAmount)} {matName.ToUpperInvariant()}";
            }
            return null;
        }

        // Only upfront costs/requirements are shown before choosing; outputs stay hidden until chosen. Exception: a guaranteed
        // gold return on a paid choice is folded into the cost, so the card matches what the purse actually loses.
        static string ChoiceCostSubtitle(EventChoice c, int goldBack)
        {
            var parts = new List<string>();
            if (c.goldCost > 0)
                parts.Add(goldBack > 0 && GoldBackGuaranteed(c)
                    ? $"COST: {c.goldCost} SPOILS · RETURNS {goldBack} (NET {goldBack - c.goldCost:+0;-0;0})"
                    : $"COST: {c.goldCost} SPOILS");
            if (!string.IsNullOrEmpty(c.requiredItem))
            {
                string itemName = Database.Items.TryGetValue(c.requiredItem, out var itm) ? itm.name : c.requiredItem;
                parts.Add($"REQUIRES {itemName.ToUpperInvariant()}");
            }
            if (!string.IsNullOrEmpty(c.requiredMaterialId))
            {
                string matName = Database.Materials.TryGetValue(c.requiredMaterialId, out var mat) ? mat.name : c.requiredMaterialId;
                parts.Add($"COST: {Math.Max(1, c.requiredMaterialAmount)} {matName.ToUpperInvariant()}");
            }
            return parts.Count == 0 ? "" : string.Join(" · ", parts);
        }

        void Choose(int index)
        {
            string outcome = Run.ChooseEvent(index);
            int gold = Run != null ? Run.Gold : 0;
            var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
            if (runGold != null) FigmaUi.SetInstanceText(runGold, "text-0", $"{gold} SPOILS");

            ResetMenu();

            var box = new VisualElement();
            box.style.paddingLeft = 20;
            box.style.paddingRight = 20;
            box.style.paddingTop = 18;
            box.style.paddingBottom = 18;
            box.style.backgroundColor = Palette.Ink3;
            box.style.borderTopWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderTopColor = Palette.Gold3;
            box.style.borderBottomColor = Palette.Gold3;
            box.style.borderLeftColor = Palette.Gold3;
            box.style.borderRightColor = Palette.Gold3;

            var hdr = FigmaUi.CreateSectionHeader("Outcome", 560f);
            hdr.style.marginBottom = 12;
            box.Add(hdr);

            var txt = new UiLabel(outcome.ToUpperInvariant());
            txt.style.whiteSpace = WhiteSpace.Normal;
            txt.style.fontSize = 15;
            txt.style.color = Palette.Mist1;
            txt.style.marginBottom = 20;
            box.Add(txt);

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            var contBtn = FigmaUi.CreateButton("Continue", "button-primary", 180);
            btnRow.Add(contBtn);
            box.Add(btnRow);
            choiceHost.Add(box);

            FigmaUi.ApplyDefaultFocusVisual(menu.Add(contBtn, "Continue", () => Game.Go(GameState.Map)), contBtn);
        }

        static string FormatCategory(EventCategory cat) => cat switch
        {
            EventCategory.NpcEncounter => "NPC ENCOUNTER",
            EventCategory.TrapAndHazard => "TRAP & HAZARD",
            EventCategory.PuzzleAndExploration => "PUZZLE & EXPLORATION",
            EventCategory.CursedAndRisk => "CURSED & RISK",
            _ => cat.ToString().ToUpperInvariant(),
        };
    }
}
