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
            if (d1 != null) d1.text = $"D{Run?.Act ?? 1} ";

            var dung = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-2");
            if (dung != null) dung.text = Run?.Map?.name ?? "Dungeon";

            var node = UiRoot.Q<UiLabel>(className: "node-4-event-vars");
            if (node != null) node.text = $" · NODE {Run?.Floor ?? 1} · EVENT";

            int gold = Run != null ? Run.Gold : 0;
            var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
            if (runGold != null) FigmaUi.SetInstanceText(runGold, "text-0", $"{gold} RUN GOLD");

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
            if (body != null) body.text = currentEvent.text.ToUpperInvariant();

            // Clear baked static choice rows and replace with dynamic choice host
            foreach (var cls in new[] { "choice-1-vars", "choice-2-vars", "choice-3-vars" })
            {
                var c = UiRoot.Q<VisualElement>(className: cls);
                if (c != null) c.style.display = DisplayStyle.None;
            }

            var screen = UiRoot.Q<VisualElement>(className: "_18-event-whispering-shrine") ?? UiRoot;
            BuildTagAndInsightArea(screen);

            choiceHost = new VisualElement { name = "EventChoiceHost" };
            choiceHost.style.position = Position.Absolute;
            choiceHost.style.left = 544;
            choiceHost.style.top = 334;
            choiceHost.style.width = 622;
            choiceHost.style.height = 322;
            choiceHost.style.flexDirection = FlexDirection.Column;
            screen.Add(choiceHost);

            ShowChoices();
        }

        void BuildTagAndInsightArea(VisualElement screen)
        {
            var metaHost = new VisualElement { name = "EventMetaHost" };
            metaHost.style.position = Position.Absolute;
            metaHost.style.left = 544;
            metaHost.style.top = 236;
            metaHost.style.width = 622;
            metaHost.style.flexDirection = FlexDirection.Column;

            if (currentEvent.tags != null && currentEvent.tags.Length > 0)
            {
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
                var box = new VisualElement();
                box.style.paddingLeft = 10;
                box.style.paddingRight = 10;
                box.style.paddingTop = 6;
                box.style.paddingBottom = 6;
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

            screen.Add(metaHost);
        }

        List<string> GatherInsights()
        {
            var list = new List<string>();
            if (Run == null) return list;

            if (Run.RevealsSupernaturalSource(currentEvent) && !string.IsNullOrEmpty(currentEvent.supernaturalSourceClue))
                list.Add($"[LUMINESCENT INSIGHT] {currentEvent.supernaturalSourceClue.ToUpperInvariant()}");

            if (Run.RevealsLiquidOutcomes(currentEvent))
                list.Add("[OCEAN'S GIFT] ALL LIQUID OUTCOMES ARE REVEALED BELOW.");

            if (Run.HasHalflingHazardReduction && currentEvent.HasTag(EventTags.Hazard))
                list.Add("[SMALL AND NIMBLE] HALFLING REDUCES PARTY HAZARD DAMAGE BY 50%.");

            if (Run.RevealsRelicEffects && (currentEvent.HasTag(EventTags.Relic) || currentEvent.HasTag(EventTags.Dragon)))
                list.Add("[DRACONIC PRESENCE] RELIC REWARD DETAILS ARE REVEALED.");

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
                AddChoiceCard(vIdx + 1, entry.choice, index, entry.status, compact, () => Choose(index));
            }
        }

        void AddChoiceCard(int number, EventChoice c, int choiceIndex, RunState.EventChoiceStatus status, bool compact, Action choose)
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

            string sub = enabled ? Consequences(c, choiceIndex, status) : status.blockedReason.ToUpperInvariant();
            var subLbl = new UiLabel(sub);
            subLbl.style.fontSize = compact ? 11 : 12;
            subLbl.style.color = enabled ? Palette.Mist2 : Palette.Mist3;
            subLbl.style.whiteSpace = WhiteSpace.Normal;
            textCol.Add(subLbl);
            card.Add(textCol);

            if (c.battleBuff != null)
            {
                var buffIcon = IconArt.StatusBadge(c.battleBuff.id, c.battleBuff.turns, 1, 2, 8f);
                if (buffIcon != null)
                {
                    if (!enabled) buffIcon.style.opacity = 0.45f;
                    card.Add(buffIcon);
                }
            }

            string tagText = DetermineCardTag(c, choiceIndex, status);
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

        string DetermineCardTag(EventChoice c, int choiceIndex, RunState.EventChoiceStatus status)
        {
            if (status.isBlocked) return status.blockedReason.ToUpperInvariant();
            if (status.usesAdaptabilityBorrow) return "ADAPTABILITY 75%";
            if (c.isImprovise) return "HUMAN · IMPROVISE";
            if (!string.IsNullOrEmpty(c.requiredRace)) return c.requiredRace.ToUpperInvariant();
            if (c.goldCost > 0) return $"-{c.goldCost} RUN GOLD";
            if (!string.IsNullOrEmpty(c.requiredItem) && Database.Items.TryGetValue(c.requiredItem, out var reqItm))
                return $"USE {reqItm.name.ToUpperInvariant()}";

            bool revealCurse = Run == null || Run.RevealsCurses || (Run.RelicManager != null && Run.RelicManager.HasRelic("R018"));
            bool hasDrawbackMod = Run != null && Run.EventModifiers.TryGetValue(choiceIndex, out var evModOpt)
                && Database.DungeonModifiers.TryGetValue(evModOpt.id, out var evModDef) && (evModDef.HasDrawback || evModDef.IsDebuff);
            bool hasKnownCurse = (!c.hasHiddenCurse || revealCurse)
                && (hasDrawbackMod || (Run != null && Run.EventCurseModifiers.ContainsKey(choiceIndex)));

            if (hasKnownCurse) return "CURSED";
            if (c.riskChancePercent > 0 && !(c.hasHiddenOutcome && Run != null && Run.RevealsLiquidOutcomes(currentEvent)))
                return "RISK";
            return null;
        }

        string Consequences(EventChoice c, int choiceIndex, RunState.EventChoiceStatus status)
        {
            float scale = status.effectScale;
            int ScaleVal(int v) => v <= 0 ? 0 : (scale >= 0.999f ? v : Math.Max(1, (int)Math.Round(v * scale)));

            bool revealsLiquid = Run != null && Run.RevealsLiquidOutcomes(currentEvent);
            bool riskFailed = Run != null && Run.EventChoiceRiskFailed.TryGetValue(choiceIndex, out var rf) && rf;

            if (c.hasHiddenOutcome && !revealsLiquid)
            {
                return "OUTCOME UNKNOWN (UNCERTAIN LIQUID)";
            }

            if (c.hasHiddenOutcome && revealsLiquid)
            {
                if (riskFailed)
                {
                    var fParts = new List<string> { "TAINTED" };
                    if (c.riskFailHurtPercent > 0) fParts.Add($"LOSE {c.riskFailHurtPercent}% MAX HP");
                    if (Run.EventRiskFailModifiers.TryGetValue(choiceIndex, out var failMod) && Database.DungeonModifiers.TryGetValue(failMod.id, out var fmd))
                        fParts.Add(fmd.displayName.ToUpperInvariant());
                    return string.Join(", ", fParts);
                }
                else
                {
                    var sParts = new List<string> { "PURE SPRING" };
                    if (c.healPercent > 0) sParts.Add($"HEAL {ScaleVal(c.healPercent)}% HP");
                    if (c.mpRestorePercent > 0) sParts.Add($"+{ScaleVal(c.mpRestorePercent)}% MP");
                    return string.Join(", ", sParts);
                }
            }

            var parts = new List<string>();
            if (c.goldCost > 0) parts.Add($"-{c.goldCost} GOLD");
            if (!string.IsNullOrEmpty(c.requiredItem))
            {
                string itemName = Database.Items.TryGetValue(c.requiredItem, out var itm) ? itm.name : c.requiredItem;
                parts.Add($"USE {itemName.ToUpperInvariant()}");
            }
            if (!string.IsNullOrEmpty(c.requiredMaterialId))
            {
                string matName = Database.Materials.TryGetValue(c.requiredMaterialId, out var mat) ? mat.name : c.requiredMaterialId;
                parts.Add($"-{Math.Max(1, c.requiredMaterialAmount)} {matName.ToUpperInvariant()}");
            }

            int goldGain = ScaleVal(c.gold);
            if (goldGain > 0) parts.Add($"+{goldGain} GOLD");

            int healPct = ScaleVal(c.healPercent);
            if (healPct > 0) parts.Add($"HEAL {healPct}% HP");

            int mpPct = ScaleVal(c.mpRestorePercent);
            if (mpPct > 0) parts.Add($"+{mpPct}% MP");

            if (c.hurtPercent > 0) parts.Add($"LOSE {c.hurtPercent}% MAX HP");

            if (c.hazardDamagePercent > 0)
            {
                int shownDmg = (Run != null && Run.HasHalflingHazardReduction)
                    ? Math.Max(1, (int)Math.Round(c.hazardDamagePercent * 0.5f))
                    : c.hazardDamagePercent;
                parts.Add($"HAZARD {shownDmg}% HP" + (Run != null && Run.HasHalflingHazardReduction ? " (HALFLING -50%)" : ""));
            }

            if (c.item != null && Database.Items.TryGetValue(c.item, out var gainItm))
                parts.Add("+" + gainItm.name.ToUpperInvariant());

            if (!string.IsNullOrEmpty(c.materialId))
            {
                int matAmt = ScaleVal(Math.Max(1, c.materialAmount));
                if (currentEvent.HasTag(EventTags.Mineral) && Run != null && Run.HasRace("dwarf"))
                    matAmt += 1;
                string matName = Database.Materials.TryGetValue(c.materialId, out var matDef) ? matDef.name : c.materialId;
                parts.Add($"+{matAmt} {matName.ToUpperInvariant()}");
            }

            if (c.randomEquipment || !string.IsNullOrEmpty(c.equipmentId))
            {
                string eqId = !string.IsNullOrEmpty(c.equipmentId) ? c.equipmentId : Run?.EventEquipment;
                string eqName = !string.IsNullOrEmpty(eqId) && Database.Equipment.TryGetValue(eqId, out var eqDef) ? eqDef.name : "EQUIPMENT";
                parts.Add("+" + eqName.ToUpperInvariant());
            }

            if (c.relic && Run?.EventRelic != null)
            {
                bool revealRelic = Run.RevealsRelicEffects || currentEvent.dungeonAct == 0;
                parts.Add(revealRelic ? "+" + Database.Relics[Run.EventRelic].name.ToUpperInvariant() : "+MYSTERY RELIC");
            }

            if (c.battleBuff != null) parts.Add("NEXT BATTLE: " + StatusInfo.Name(c.battleBuff.id).ToUpperInvariant());

            if (Run != null && Run.EventModifiers.TryGetValue(choiceIndex, out var evMod))
            {
                var (mLabel, _) = RewardGenerator.Describe(evMod, Run.ModifierManager);
                parts.Add($"+{mLabel.ToUpperInvariant()}");
            }

            bool revealCurse = Run == null || Run.RevealsCurses;
            if ((!c.hasHiddenCurse || revealCurse) && Run != null && Run.EventCurseModifiers.TryGetValue(choiceIndex, out var cMod)
                && Database.DungeonModifiers.TryGetValue(cMod.id, out var cmd))
            {
                parts.Add($"CURSE: {cmd.displayName.ToUpperInvariant()}");
            }

            if (c.riskChancePercent > 0)
            {
                parts.Add($"{c.riskChancePercent}% RISK");
            }

            return parts.Count == 0 ? "NOTHING HAPPENS" : string.Join(", ", parts);
        }

        void Choose(int index)
        {
            string outcome = Run.ChooseEvent(index);
            int gold = Run != null ? Run.Gold : 0;
            var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
            if (runGold != null) FigmaUi.SetInstanceText(runGold, "text-0", $"{gold} RUN GOLD");

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
