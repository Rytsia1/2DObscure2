using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    using UiLabel = UnityEngine.UIElements.Label;
    // Main Menu — Figma "00 — Main Menu" rendered via UI Toolkit:
    // night sky + tower silhouette, left-aligned gold title, flat menu list, key-prompt footer.
    public class MainMenuScreen : GameScreen
    {
        public UitkMenu.Item NewGameButton { get; private set; }
        public UitkMenu.Item ContinueButton { get; private set; }
        public UitkMenu.Item LoadGameButton { get; private set; }
        public UitkMenu.Item SettingsButton { get; private set; }
        public UitkMenu.Item ExitButton { get; private set; }

        protected override void Build()
        {
            UiRoot = FigmaUi.Mount(Root.gameObject, "00---Main-Menu");
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            bool hasSave = SaveManager.HasAnySave(out var best);
            bool legacySave = !hasSave && GameSave.HasSavedGame();
            bool canContinue = hasSave || legacySave;

            var continueInst = FigmaUi.FindInstanceWithText(UiRoot, "text-1", "CONTINUE");
            var continueSub = FigmaUi.FindLabel(UiRoot, "AUTO SAVE · GEISTWALD CITY · 0 OF 8 CLEARED");
            if (continueSub != null)
            {
                if (hasSave)
                {
                    var m = SaveManager.GetSlotMetadata(best);
                    string when = m.lastSavedTicks > 0 ? new DateTime(m.lastSavedTicks).ToString("d MMM HH:mm") : "";
                    continueSub.text = $"{SaveSlotMetadata.FormatSlotName(best)} · GEISTWALD CITY · {Mathf.Max(0, m.committedAct - 1)} OF 8 CLEARED{(string.IsNullOrEmpty(when) ? "" : " · " + when)}".ToUpperInvariant();
                    continueSub.EnableInClassList("uitk-disabled", false);
                }
                else
                {
                    continueSub.text = legacySave ? "LEGACY SAVE READY" : "NO SAVED CAMPAIGN";
                    continueSub.EnableInClassList("uitk-disabled", !canContinue);
                }
            }

            if (continueInst != null)
            {
                ContinueButton = menu.Add(continueInst, "CONTINUE", () => Game?.ContinueGame(), interactable: canContinue);
            }

            NewGameButton = AddFlatButton(menu, UiRoot, "NEW GAME", () => Game?.StartNewGame(), true);
            LoadGameButton = AddFlatButton(menu, UiRoot, "LOAD GAME", () => Game?.Go(GameState.Load), hasSave);
            SettingsButton = AddFlatButton(menu, UiRoot, "SETTINGS", () => Game?.Go(GameState.Settings), true);
            ExitButton = AddFlatButton(menu, UiRoot, "EXIT", () => Game?.Quit(), true);

            if (!canContinue && NewGameButton != null)
                menu.Focus(NewGameButton, false);
            else if (ContinueButton != null)
                menu.Focus(ContinueButton, false, true);

            var versionLbl = FigmaUi.FindLabel(UiRoot, "V1.0");
            if (versionLbl != null)
                versionLbl.text = "V" + Application.version;
        }

        static UitkMenu.Item AddFlatButton(UitkMenu menu, VisualElement root, string text, Action onClick, bool interactable)
        {
            var btn = FigmaUi.FindButton(root, text);
            if (btn == null) return null;
            btn.SetEnabled(interactable);
            return menu.Add(btn, text, onClick, interactable: interactable, visual: focused =>
            {
                btn.EnableInClassList("uitk-focus", focused);
                btn.EnableInClassList("uitk-disabled", !interactable);
                btn.text = focused ? $"▸  {text}" : text;
            });
        }
    }

    // Opening story sequence shown after confirming New Game party before the first expedition.
    public class OpeningSequenceScreen : GameScreen
    {
        public UitkMenu.Item BeginExpeditionButton { get; private set; }

        protected override void Build()
        {
            UiRoot = new VisualElement();
            var catalog = FigmaScreenCatalog.Instance;
            var ps = catalog != null ? catalog.GetPanelSettings() : null;
            var doc = Root.gameObject.AddComponent<UIDocument>();
            doc.sortingOrder = 10;
            if (ps != null) doc.panelSettings = ps;

            UiRoot = doc.rootVisualElement ?? new VisualElement();
            var tokens = catalog != null ? catalog.GetTokens() : null;
            var runtime = catalog != null ? catalog.GetRuntimeStylesheet() : null;
            if (tokens != null) UiRoot.styleSheets.Add(tokens);
            if (runtime != null) UiRoot.styleSheets.Add(runtime);

            UiRoot.style.flexGrow = 1;
            UiRoot.style.backgroundColor = Palette.Ink1;
            UiRoot.style.paddingLeft = 48;
            UiRoot.style.paddingRight = 48;
            UiRoot.style.paddingTop = 36;
            UiRoot.style.paddingBottom = 36;
            UiRoot.style.justifyContent = Justify.SpaceBetween;

            var panel = new VisualElement();
            panel.style.flexGrow = 1;
            panel.style.backgroundColor = Palette.Ink2;
            panel.style.borderTopWidth = 2;
            panel.style.borderRightWidth = 2;
            panel.style.borderBottomWidth = 2;
            panel.style.borderLeftWidth = 2;
            panel.style.borderTopColor = Palette.Gold3;
            panel.style.borderRightColor = Palette.Gold3;
            panel.style.borderBottomColor = Palette.Gold3;
            panel.style.borderLeftColor = Palette.Gold3;
            panel.style.paddingLeft = 36;
            panel.style.paddingRight = 36;
            panel.style.paddingTop = 28;
            panel.style.paddingBottom = 28;
            UiRoot.Add(panel);

            var title = new UiLabel("PROLOGUE: THE SEALED ABYSS");
            title.style.fontSize = 28;
            title.style.color = Palette.Gold1;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 18;
            panel.Add(title);

            var div = new VisualElement();
            div.AddToClassList("uitk-modal-divider");
            panel.Add(div);

            var story = new UiLabel(
                "BENEATH THE ASHEN CITADEL, THE ANCIENT SEAL WEAKENS. " +
                "FOUR SOULS BOUND BY OATH STEP BEYOND THE IRON GATE " +
                "TO DESCEND INTO THE SHIFTING CORRIDORS OF THE ABYSS.");
            story.style.fontSize = 20;
            story.style.color = Palette.Mist1;
            story.style.whiteSpace = WhiteSpace.Normal;
            story.style.unityTextAlign = TextAnchor.MiddleCenter;
            story.style.marginBottom = 28;
            panel.Add(story);

            var cardsRow = new VisualElement();
            cardsRow.style.flexDirection = FlexDirection.Row;
            cardsRow.style.justifyContent = Justify.SpaceBetween;
            cardsRow.style.flexGrow = 1;
            panel.Add(cardsRow);

            var party = Game?.Roster != null ? Game.Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var hero = party[i];
                var card = new VisualElement();
                card.AddToClassList("uitk-card");
                card.style.width = 250;
                card.style.alignItems = UnityEngine.UIElements.Align.Center;
                card.style.paddingTop = 20;
                card.style.paddingBottom = 20;

                var portrait = new VisualElement();
                portrait.style.width = 96;
                portrait.style.height = 96;
                portrait.style.marginBottom = 16;
                if (hero.sprite != null)
                    FigmaUi.SetBackgroundContain(portrait, FigmaUi.GetSpriteTexture(hero.sprite, "hero_" + hero.id));
                card.Add(portrait);

                var nameLbl = new UiLabel((hero.name ?? "HERO").ToUpperInvariant());
                nameLbl.style.fontSize = 22;
                nameLbl.style.color = Palette.Mist1;
                nameLbl.style.marginBottom = 6;
                card.Add(nameLbl);

                var raceLbl = new UiLabel(Database.Races.TryGetValue(hero.raceId ?? "", out var r) ? r.name.ToUpperInvariant() : (hero.raceId ?? "").ToUpperInvariant());
                raceLbl.style.fontSize = 14;
                raceLbl.style.color = Palette.Mist3;
                raceLbl.style.marginBottom = 4;
                card.Add(raceLbl);

                var clsLbl = new UiLabel(Database.Classes.TryGetValue(hero.classId ?? "", out var c) ? c.name.ToUpperInvariant() : (hero.classId ?? "").ToUpperInvariant());
                clsLbl.style.fontSize = 16;
                clsLbl.style.color = Palette.Gold1;
                card.Add(clsLbl);

                cardsRow.Add(card);
            }

            var footer = new VisualElement();
            footer.style.alignItems = UnityEngine.UIElements.Align.Center;
            footer.style.marginTop = 24;
            UiRoot.Add(footer);

            var btnEl = FigmaUi.CreateButton("BEGIN EXPEDITION", "button-primary", 320f);
            footer.Add(btnEl);

            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            BeginExpeditionButton = menu.Add(btnEl, "BEGIN EXPEDITION", () => Game?.BeginFirstExpedition());
        }
    }
}
