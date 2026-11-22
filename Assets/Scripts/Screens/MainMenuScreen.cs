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
                continueSub.style.left = 117f;
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
                ContinueButton = menu.Add(continueInst, "CONTINUE", () => Game?.ContinueGame(), interactable: canContinue, visual: focused =>
                {
                    var arrow = continueInst.Q<UiLabel>("text-0");
                    if (arrow != null) arrow.style.display = focused ? DisplayStyle.Flex : DisplayStyle.None;
                    FigmaUi.ApplyDefaultFocusVisual(continueInst, focused, false, canContinue);
                });
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

    // Prologue intro, shown after confirming the New Game party (Tutorial_Prologue_Design.md §2, §10).
    // Offers Skip once this profile has cleared the Prologue (§4.2).
    public class OpeningSequenceScreen : GameScreen
    {
        public UitkMenu.Item BeginExpeditionButton { get; private set; }
        public UitkMenu.Item SkipButton { get; private set; }

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

            UiRoot.AddToClassList("uitk-opening-root");

            var panel = new VisualElement();
            panel.AddToClassList("uitk-opening-panel");
            UiRoot.Add(panel);

            var title = new UiLabel("PROLOGUE: THE THRESHOLD");
            title.AddToClassList("uitk-opening-title");
            panel.Add(title);

            var div = new VisualElement();
            div.AddToClassList("uitk-modal-divider");
            panel.Add(div);

            var story = new UiLabel(
                "1631 AR. THE FULL MOON OPENS THE PORTAL IN GEISTWALD. " +
                "BY ROYAL DECREE EVERY FAMILY SENDS ONE TO THE EXPEDITION. " +
                "YOUR FOUR STEP THROUGH, INTO THE THRESHOLD OF THE WORLD'S BETWEEN.");
            story.AddToClassList("uitk-opening-story");
            panel.Add(story);

            var cardsRow = new VisualElement();
            cardsRow.AddToClassList("uitk-opening-cards");
            panel.Add(cardsRow);

            var party = Game?.Roster != null ? Game.Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var hero = party[i];
                var card = new VisualElement();
                card.AddToClassList("uitk-card");
                card.AddToClassList("uitk-opening-card");

                var portrait = new VisualElement();
                portrait.AddToClassList("uitk-opening-portrait");
                if (hero.sprite != null)
                    FigmaUi.SetBackgroundContain(portrait, FigmaUi.GetSpriteTexture(hero.sprite, "hero_" + hero.id));
                card.Add(portrait);

                var nameLbl = new UiLabel((hero.name ?? "HERO").ToUpperInvariant());
                nameLbl.AddToClassList("uitk-opening-name");
                card.Add(nameLbl);

                var raceLbl = new UiLabel(Database.Races.TryGetValue(hero.raceId ?? "", out var r) ? r.name.ToUpperInvariant() : (hero.raceId ?? "").ToUpperInvariant());
                raceLbl.AddToClassList("uitk-opening-race");
                card.Add(raceLbl);

                var clsLbl = new UiLabel(Database.Classes.TryGetValue(hero.classId ?? "", out var c) ? c.name.ToUpperInvariant() : (hero.classId ?? "").ToUpperInvariant());
                clsLbl.AddToClassList("uitk-opening-class");
                card.Add(clsLbl);

                cardsRow.Add(card);
            }

            var footer = new VisualElement();
            footer.AddToClassList("uitk-opening-footer");
            UiRoot.Add(footer);

            var btnEl = FigmaUi.CreateButton("BEGIN PROLOGUE", "button-primary", 320f);
            footer.Add(btnEl);
            bool canSkip = Game?.Settings != null && Game.Settings.prologueCleared;
            var skipEl = canSkip ? FigmaUi.CreateButton("SKIP PROLOGUE", "button-slab", 320f) : null;
            if (skipEl != null) footer.Add(skipEl);

            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => { }; // the party is already committed; there is no way back into creation
            BeginExpeditionButton = menu.Add(btnEl, "BEGIN PROLOGUE", () => Game?.BeginPrologue());
            if (skipEl != null) SkipButton = menu.Add(skipEl, "SKIP PROLOGUE", () => Game?.SkipPrologue());
        }
    }

    // After Aetherion: the tutorial-only retry ends here, so say so before the Hub (Tutorial_Prologue_Design.md §4.1).
    public class PrologueClearScreen : GameScreen
    {
        protected override void Build()
        {
            var catalog = FigmaScreenCatalog.Instance;
            var doc = Root.gameObject.AddComponent<UIDocument>();
            doc.sortingOrder = 10;
            if (catalog != null && catalog.GetPanelSettings() != null) doc.panelSettings = catalog.GetPanelSettings();
            UiRoot = doc.rootVisualElement ?? new VisualElement();
            if (catalog?.GetTokens() != null) UiRoot.styleSheets.Add(catalog.GetTokens());
            if (catalog?.GetRuntimeStylesheet() != null) UiRoot.styleSheets.Add(catalog.GetRuntimeStylesheet());
            UiRoot.AddToClassList("uitk-opening-root");

            var panel = new VisualElement();
            panel.AddToClassList("uitk-opening-panel");
            UiRoot.Add(panel);
            var title = new UiLabel("THE THRESHOLD OPENS");
            title.AddToClassList("uitk-opening-title");
            panel.Add(title);
            var div = new VisualElement();
            div.AddToClassList("uitk-modal-divider");
            panel.Add(div);
            var story = new UiLabel(
                "AETHERION YIELDS AND THE PATH TO LAYER 1 STANDS OPEN. " +
                "THE THRESHOLD SENT YOU BACK WHEN YOU FELL. THE WORLD'S BETWEEN WILL NOT. " +
                "FROM HERE ON, IF THE WHOLE PARTY FALLS, THE EXPEDITION ENDS.");
            story.AddToClassList("uitk-opening-story");
            panel.Add(story);

            var footer = new VisualElement();
            footer.AddToClassList("uitk-opening-footer");
            UiRoot.Add(footer);
            var btnEl = FigmaUi.CreateButton("RETURN TO GEISTWALD", "button-primary", 320f);
            footer.Add(btnEl);
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => { };
            menu.Add(btnEl, "RETURN TO GEISTWALD", () => Game?.FinishPrologue());
        }
    }
}
