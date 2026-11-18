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
            choiceHost = new VisualElement { name = "EventChoiceHost" };
            choiceHost.style.position = Position.Absolute;
            choiceHost.style.left = 544;
            choiceHost.style.top = 370;
            choiceHost.style.width = 620;
            choiceHost.style.height = 280;
            choiceHost.style.flexDirection = FlexDirection.Column;
            screen.Add(choiceHost);

            ShowChoices();
        }

        void ResetMenu()
        {
            if (menu != null)
            {
                if (Application.isPlaying) Object.Destroy(menu);
                else Object.DestroyImmediate(menu);
            }
            choiceHost.Clear();
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
        }

        void ShowChoices()
        {
            ResetMenu();
            for (int i = 0; i < currentEvent.choices.Length; i++)
            {
                int index = i;
                var c = currentEvent.choices[i];
                var blocked = Run != null ? Run.EventBlocked(c) : null;
                AddChoiceCard(i + 1, c, index, blocked, () => Choose(index));
            }
        }

        void AddChoiceCard(int number, EventChoice c, int choiceIndex, string blocked, System.Action choose)
        {
            bool enabled = blocked == null;
            var card = new VisualElement();
            card.style.height = 58;
            card.style.marginBottom = 8;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = UnityEngine.UIElements.Align.Center;
            card.style.borderTopWidth = 2;
            card.style.borderBottomWidth = 2;
            card.style.borderLeftWidth = 2;
            card.style.borderRightWidth = 2;

            var numLbl = new UiLabel(number.ToString());
            numLbl.style.fontSize = 20;
            numLbl.style.width = 28;
            numLbl.style.color = enabled ? Palette.Mist2 : Palette.Mist3;
            card.Add(numLbl);

            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            textCol.style.flexDirection = FlexDirection.Column;
            textCol.style.justifyContent = Justify.Center;

            var titleLbl = new UiLabel(c.label);
            titleLbl.style.fontSize = 20;
            titleLbl.style.color = enabled ? Palette.Mist1 : Palette.Mist3;
            textCol.Add(titleLbl);

            string sub = enabled ? Consequences(c, choiceIndex) : blocked.ToUpperInvariant();
            var subLbl = new UiLabel(sub);
            subLbl.style.fontSize = 13;
            subLbl.style.color = enabled ? Palette.Mist2 : Palette.Mist3;
            textCol.Add(subLbl);
            card.Add(textCol);

            bool hasDrawbackMod = Run != null && Run.EventModifiers.TryGetValue(choiceIndex, out var evModOpt)
                && Database.DungeonModifiers.TryGetValue(evModOpt.id, out var evModDef) && (evModDef.HasDrawback || evModDef.IsDebuff);
            string tagText = !enabled ? blocked.ToUpperInvariant() : c.goldCost > 0 ? $"-{c.goldCost} RUN GOLD" : hasDrawbackMod ? "CURSED" : null;
            if (tagText != null)
            {
                var tag = FigmaUi.InstantiateTemplate(enabled ? "Tag-Temp" : "Tag-Cond");
                if (tag != null)
                {
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

        string Consequences(EventChoice c, int choiceIndex)
        {
            var parts = new List<string>();
            if (c.goldCost > 0) parts.Add($"-{c.goldCost} GOLD");
            if (c.gold > 0) parts.Add($"+{c.gold} GOLD");
            if (c.healPercent > 0) parts.Add($"HEAL {c.healPercent}% HP");
            if (c.hurtPercent > 0) parts.Add($"LOSE {c.hurtPercent}% MAX HP");
            if (c.item != null) parts.Add("+" + Database.Items[c.item].name.ToUpperInvariant());
            if (c.relic && Run?.EventRelic != null) parts.Add("+" + Database.Relics[Run.EventRelic].name.ToUpperInvariant());
            if (c.battleBuff != null) parts.Add("NEXT BATTLE: " + StatusInfo.Name(c.battleBuff.id).ToUpperInvariant());
            if (Run != null && Run.EventModifiers.TryGetValue(choiceIndex, out var evMod))
            {
                var (mLabel, mDetail) = RewardGenerator.Describe(evMod, Run.ModifierManager);
                parts.Add($"+{mLabel.ToUpperInvariant()} ({mDetail.ToUpperInvariant()})");
            }
            if (c.relic && Run?.RelicManager != null)
            {
                var clue = Run.RelicManager.GetAdditionalEventClue("relic");
                if (clue != null) parts.Add(clue.ToUpperInvariant());
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
    }
}
