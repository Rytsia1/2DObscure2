using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    using UiLabel = UnityEngine.UIElements.Label;
    static class SaveSlotUitkHelper
    {
        static readonly string[] SlotDefaultLabels =
        {
            "AUTO SAVE", "MANUAL SLOT 1", "MANUAL SLOT 2", "MANUAL SLOT 3", "MANUAL SLOT 4"
        };

        public static VisualElement[] FindSlotCards(VisualElement root)
        {
            var cards = new VisualElement[5];
            for (int i = 0; i < 5; i++)
                cards[i] = FigmaUi.FindInstanceWithText(root, "text-0", SlotDefaultLabels[i])?.parent;
            return cards;
        }

        public static void BindSlotCard(VisualElement card, SaveSlotId slot, bool selected, string emptyText)
        {
            if (card == null) return;
            var meta = SaveManager.GetSlotMetadata(slot);
            var titleLbl = card.Q<UiLabel>("text-0");
            var placeLbl = card.Q<UiLabel>("text-1");
            var partyLbl = card.Q<UiLabel>("text-2");
            var detailLbl = card.Q<UiLabel>("text-3");

            if (titleLbl != null)
            {
                titleLbl.text = SaveSlotMetadata.FormatSlotName(slot);
                titleLbl.style.color = selected ? Palette.Gold1 : Palette.Mist1;
            }

            if (!meta.isOccupied)
            {
                if (placeLbl != null) { placeLbl.text = "EMPTY"; placeLbl.style.color = Palette.Mist3; }
                if (partyLbl != null) { partyLbl.text = emptyText; partyLbl.style.color = Palette.Mist3; }
                if (detailLbl != null) detailLbl.text = "";
            }
            else
            {
                bool isCorrupted = meta.partySummary == "[CORRUPTED SAVE]";
                bool isLost = meta.expeditionLost;
                if (placeLbl != null)
                {
                    placeLbl.text = isCorrupted ? "CORRUPTED" : isLost ? "EXPEDITION LOST" : $"ACT {meta.committedAct} · {meta.committedDungeonName}".ToUpperInvariant();
                    placeLbl.style.color = (isCorrupted || isLost) ? Palette.Err : Palette.Ok;
                }
                if (partyLbl != null)
                {
                    partyLbl.text = meta.partySummary.ToUpperInvariant();
                    partyLbl.style.color = (isCorrupted || isLost) ? Palette.Mist3 : Palette.Mist1;
                }
                if (detailLbl != null)
                {
                    detailLbl.text = $"{meta.lastSavedAtIso} · {meta.hubGold:#,##0} GOLD · {meta.GetFormattedPlaytime()}".ToUpperInvariant();
                    detailLbl.style.color = Palette.Mist3;
                }
            }

            card.AddToClassList("uitk-slot-interactive");
            card.EnableInClassList("uitk-selected", selected);
        }

        public static void DockSystemBadge(VisualElement root, VisualElement autoCard)
        {
            if (root == null || autoCard == null) return;
            var sysTag = FigmaUi.FindInstanceWithText(root, "text-0", "SYSTEM");
            var sysContainer = sysTag?.GetFirstAncestorOfType<TemplateContainer>() ?? sysTag;
            var titleLbl = autoCard.Q<UiLabel>("text-0");
            if (sysContainer != null && titleLbl?.parent != null)
            {
                sysContainer.RemoveFromHierarchy();
                sysContainer.pickingMode = PickingMode.Ignore;
                sysContainer.style.position = Position.Relative;
                sysContainer.style.left = StyleKeyword.Auto;
                sysContainer.style.top = StyleKeyword.Auto;
                sysContainer.style.marginLeft = 10;
                sysContainer.style.flexShrink = 0;
                var row = titleLbl.parent;
                row.Insert(row.IndexOf(titleLbl) + 1, sysContainer);
            }
        }
    }

    public class SaveScreen : GameScreen
    {
        SaveSlotId selectedSlot = SaveSlotId.Slot1;
        VisualElement[] slotCards;
        VisualElement backBtn, actionRow;
        UiLabel detailTitleLbl, detailBodyLbl, feedbackLabel;
        UitkMenu menu;
        int focusIndex;
        bool rebuilding;
        readonly List<VisualElement> actionEls = new List<VisualElement>();

        protected override void Build()
        {
            UiRoot = FigmaUi.Mount(Root.gameObject, "11---Save-Game");
            FigmaUi.HideByClass(UiRoot, "saves-progress-committed-by-your-last-dungeon-clear-vars");
            FigmaUi.HideInstancesWithText(UiRoot, FigmaUi.TitleRuleTags);
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Go(GameState.Hub);
            menu.onFocus = item => { if (!rebuilding && item != null) focusIndex = menu.items.IndexOf(item); };

            backBtn = FigmaUi.FindButton(UiRoot, "Back");
            slotCards = SaveSlotUitkHelper.FindSlotCards(UiRoot);
            SaveSlotUitkHelper.DockSystemBadge(UiRoot, slotCards[0]);
            detailTitleLbl = FigmaUi.FindLabel(UiRoot, "SELECTED: MANUAL SLOT 1");
            detailBodyLbl = FigmaUi.FindLabel(UiRoot, "This manual slot is empty.\nSave to record your current campaign.");
            feedbackLabel = FigmaUi.FindLabel(UiRoot, "Select a slot to save campaign progress.");
            if (feedbackLabel != null) feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            var savePrimary = FigmaUi.FindInstanceWithText(UiRoot, "text-1", "SAVE GAME");
            actionRow = savePrimary?.parent?.parent;

            RefreshAll(focusAction: true);
        }

        void RefreshAll(bool focusAction)
        {
            for (int i = 0; i < 5; i++)
                SaveSlotUitkHelper.BindSlotCard(slotCards[i], (SaveSlotId)i, selectedSlot == (SaveSlotId)i, "NO SAVED CAMPAIGN IN THIS SLOT.");

            var meta = SaveManager.GetSlotMetadata(selectedSlot);
            if (detailTitleLbl != null)
                detailTitleLbl.text = $"SELECTED: {SaveSlotMetadata.FormatSlotName(selectedSlot)}";

            actionEls.Clear();
            btnActions.Clear();
            if (actionRow != null) actionRow.Clear();

            if (selectedSlot == SaveSlotId.AutoSave)
            {
                if (detailBodyLbl != null)
                    detailBodyLbl.text = "The Auto Save slot is managed automatically by the Sanctuary Hub.\nManual saves cannot overwrite this slot.";
                AddActionButton("BACK TO HUB", "button-slab", () => Game?.Go(GameState.Hub));
            }
            else if (!meta.isOccupied)
            {
                if (detailBodyLbl != null)
                    detailBodyLbl.text = "This manual slot is empty.\nSave to record your current campaign.";
                AddActionButton("SAVE GAME", "button-primary", OnSaveClicked);
                AddActionButton("BACK", "button-slab", () => Game?.Go(GameState.Hub));
            }
            else
            {
                if (detailBodyLbl != null)
                    detailBodyLbl.text = $"Occupied by {meta.partySummary}.\nOverwrite will replace this campaign.";
                AddActionButton("OVERWRITE", "button-primary", OnOverwriteClicked);
                AddActionButton("DELETE", "button-danger", OnDeleteClicked);
                AddActionButton("BACK", "button-slab", () => Game?.Go(GameState.Hub));
            }

            RebuildMenu(focusAction);
        }

        void AddActionButton(string text, string styleClass, Action onClick)
        {
            if (actionRow == null) return;
            var el = FigmaUi.CreateButton(text, styleClass, actionEls.Count > 0 ? 160f : 190f);
            el.style.marginRight = 12;
            el.userData = text;
            actionRow.Add(el);
            actionEls.Add(el);
            el.pickingMode = PickingMode.Position;
            btnActions[el] = onClick;
        }

        readonly Dictionary<VisualElement, Action> btnActions = new Dictionary<VisualElement, Action>();

        void RebuildMenu(bool focusAction)
        {
            rebuilding = true;
            try
            {
                menu.Clear();
                int cardCount = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (slotCards[i] == null) continue;
                    var slot = (SaveSlotId)i;
                    cardCount++;
                    menu.Add(slotCards[i], SaveSlotMetadata.FormatSlotName(slot), () =>
                    {
                        selectedSlot = slot;
                        if (feedbackLabel != null)
                        {
                            feedbackLabel.text = "Select a slot to save campaign progress.";
                            feedbackLabel.style.color = Palette.Mist3;
                        }
                        RefreshAll(false);
                    }, selected: selectedSlot == slot);
                }

                foreach (var el in actionEls)
                {
                    string label = el.userData as string ?? "ACTION";
                    menu.Add(el, label, btnActions.TryGetValue(el, out var act) ? act : null);
                }

                if (backBtn != null)
                    menu.Add(backBtn, "Back", () => Game?.Go(GameState.Hub));

                if (focusAction && actionEls.Count > 0)
                    focusIndex = cardCount;
                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        void SetFeedback(string msg, bool bad)
        {
            if (feedbackLabel == null) return;
            feedbackLabel.text = msg;
            feedbackLabel.style.color = bad ? Palette.Err : Palette.Ok;
        }

        void OnSaveClicked()
        {
            SetFeedback("Saving campaign progress...", false);
            if (SaveManager.Save(selectedSlot, out string err))
            {
                SetFeedback($"Successfully saved to {SaveSlotMetadata.FormatSlotName(selectedSlot)}!", false);
                RefreshAll(false);
            }
            else
            {
                SetFeedback($"Save failed: {err}", true);
            }
        }

        void OnOverwriteClicked()
        {
            FigmaUi.ShowConfirmationModal(UiRoot, "CONFIRM OVERWRITE",
                $"Overwrite {SaveSlotMetadata.FormatSlotName(selectedSlot)}?\nExisting campaign data will be permanently replaced.",
                "CONFIRM OVERWRITE",
                () =>
                {
                    if (SaveManager.Save(selectedSlot, out string err))
                    {
                        SetFeedback($"Successfully overwritten {SaveSlotMetadata.FormatSlotName(selectedSlot)}!", false);
                        RefreshAll(false);
                    }
                    else
                    {
                        SetFeedback($"Overwrite failed: {err}", true);
                    }
                });
        }

        void OnDeleteClicked()
        {
            FigmaUi.ShowConfirmationModal(UiRoot, "CONFIRM DELETE",
                $"Delete save in {SaveSlotMetadata.FormatSlotName(selectedSlot)}?\nThis campaign will be permanently erased.",
                "CONFIRM DELETE",
                () =>
                {
                    if (SaveManager.Delete(selectedSlot, out string err))
                    {
                        SetFeedback($"Deleted {SaveSlotMetadata.FormatSlotName(selectedSlot)}.", false);
                        RefreshAll(false);
                    }
                    else
                    {
                        SetFeedback($"Delete failed: {err}", true);
                    }
                });
        }
    }

    public class LoadScreen : GameScreen
    {
        readonly GameState returnState;
        SaveSlotId selectedSlot = SaveSlotId.AutoSave;
        VisualElement[] slotCards;
        VisualElement backBtn, actionRow;
        UiLabel detailTitleLbl, detailBodyLbl, feedbackLabel;
        UitkMenu menu;
        int focusIndex;
        bool rebuilding;
        readonly List<(VisualElement el, string text, Action action)> actionEls = new List<(VisualElement, string, Action)>();

        public LoadScreen(GameState returnState = GameState.MainMenu)
        {
            this.returnState = returnState;
        }

        protected override void Build()
        {
            UiRoot = FigmaUi.Mount(Root.gameObject, "03---Load-Game");
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Go(returnState);
            menu.onFocus = item => { if (!rebuilding && item != null) focusIndex = menu.items.IndexOf(item); };

            backBtn = FigmaUi.FindButton(UiRoot, "Back");
            slotCards = SaveSlotUitkHelper.FindSlotCards(UiRoot);
            SaveSlotUitkHelper.DockSystemBadge(UiRoot, slotCards[0]);
            detailTitleLbl = FigmaUi.FindLabel(UiRoot, "SELECTED: AUTO SAVE");
            detailBodyLbl = UiRoot.Query<UiLabel>().ToList().FirstOrDefault(l => l.text != null && l.text.StartsWith("Campaign:", StringComparison.Ordinal));
            feedbackLabel = FigmaUi.FindLabel(UiRoot, "Loading restores your campaign and returns to the Sanctuary Hub.");
            if (feedbackLabel != null) feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            var loadPrimary = FigmaUi.FindInstanceWithText(UiRoot, "text-1", "LOAD CAMPAIGN");
            actionRow = loadPrimary?.parent?.parent;

            RefreshAll(focusAction: true);
        }

        void RefreshAll(bool focusAction)
        {
            for (int i = 0; i < 5; i++)
                SaveSlotUitkHelper.BindSlotCard(slotCards[i], (SaveSlotId)i, selectedSlot == (SaveSlotId)i, "EMPTY SLOT — NOTHING TO LOAD.");

            var meta = SaveManager.GetSlotMetadata(selectedSlot);
            if (detailTitleLbl != null)
                detailTitleLbl.text = $"SELECTED: {SaveSlotMetadata.FormatSlotName(selectedSlot)}";

            actionEls.Clear();
            if (actionRow != null) actionRow.Clear();

            bool isCorrupted = meta.isOccupied && meta.partySummary == "[CORRUPTED SAVE]";
            bool isLost = meta.isOccupied && meta.expeditionLost;
            if (!meta.isOccupied)
            {
                if (detailBodyLbl != null)
                    detailBodyLbl.text = "This slot is empty and cannot be loaded.\nSelect an occupied slot.";
                AddAction("BACK", "button-slab", () => Game?.Go(returnState));
            }
            else if (isCorrupted || isLost)
            {
                if (detailBodyLbl != null)
                    detailBodyLbl.text = isCorrupted
                        ? "This save file is corrupted and cannot be loaded."
                        : "This expedition was lost with 0 retries remaining.\nStart a New Game or choose another slot.";
                AddAction("BACK", "button-slab", () => Game?.Go(returnState));
            }
            else
            {
                if (detailBodyLbl != null)
                    detailBodyLbl.text = $"Campaign: {meta.committedDungeonName}\nParty: {meta.partySummary}\nGold: {meta.hubGold:#,##0}";
                AddAction("LOAD CAMPAIGN", "button-primary", OnLoadClicked);
                AddAction("BACK", "button-slab", () => Game?.Go(returnState));
            }

            RebuildMenu(focusAction);
        }

        void AddAction(string text, string styleClass, Action onClick)
        {
            if (actionRow == null) return;
            var el = FigmaUi.CreateButton(text, styleClass, 220f);
            el.style.marginRight = 14;
            actionRow.Add(el);
            actionEls.Add((el, text, onClick));
        }

        void RebuildMenu(bool focusAction)
        {
            rebuilding = true;
            try
            {
                menu.Clear();
                int cardCount = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (slotCards[i] == null) continue;
                    var slot = (SaveSlotId)i;
                    cardCount++;
                    menu.Add(slotCards[i], SaveSlotMetadata.FormatSlotName(slot), () =>
                    {
                        selectedSlot = slot;
                        if (feedbackLabel != null)
                        {
                            feedbackLabel.text = "Loading restores your campaign and returns to the Sanctuary Hub.";
                            feedbackLabel.style.color = Palette.Mist3;
                        }
                        RefreshAll(false);
                    }, selected: selectedSlot == slot);
                }

                foreach (var (el, text, act) in actionEls)
                    menu.Add(el, text, act);

                if (backBtn != null)
                    menu.Add(backBtn, "Back", () => Game?.Go(returnState));

                if (focusAction && actionEls.Count > 0)
                    focusIndex = cardCount;
                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        void SetLoadFeedback(string msg, bool bad)
        {
            if (feedbackLabel == null) return;
            feedbackLabel.text = msg;
            feedbackLabel.style.color = bad ? Palette.Err : Palette.Ok;
        }

        void OnLoadClicked()
        {
            if (Game?.Run != null)
            {
                FigmaUi.ShowConfirmationModal(UiRoot, "CONFIRM LOAD",
                    $"Load {SaveSlotMetadata.FormatSlotName(selectedSlot)}?\nAny unsaved Hub changes will be lost.",
                    "CONFIRM LOAD",
                    ExecuteLoad);
            }
            else
            {
                ExecuteLoad();
            }
        }

        void ExecuteLoad()
        {
            SetLoadFeedback("Loading campaign...", false);
            if (SaveManager.Load(selectedSlot, out string err))
                SetLoadFeedback("Campaign loaded successfully!", false);
            else
                SetLoadFeedback($"Load failed: {err}", true);
        }
    }
}
