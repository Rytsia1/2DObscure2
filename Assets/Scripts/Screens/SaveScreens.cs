using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Shared Save / Load chrome, matching the hub service boards (Back · gold title · tag · note, ink rule at y 33).
    static class SaveSlotUi
    {
        public static Selectable Header(Transform root, string title, string tag, string note, Action back)
        {
            Backdrop.Create(root, false, 0, Palette.Ink1, Palette.Ink1);
            var backBtn = Ui.Button(root, "Back", 12, 9, 40, back, 18, showCursor: false);
            Ui.Label(root, title, 62, 13, Palette.Gold2);
            int x = 62 + PixelFont.Measure(title) + 10;
            x += Ui.Tag(root, tag, x, 13, TagKind.Keep, 13) + 8;
            Ui.Label(root, PixelFont.Fit(note, 628 - x, FontKind.Interface), x, 16, Palette.Mist2, font: FontKind.Interface);
            Ui.Rect(root, 12, 33, 616, 1, Palette.Ink4);
            return backBtn;
        }

        // Selectable slot card. Text sits above the card's own box (Widget layer), so it is never hidden by it.
        public static GameObject SlotCard(Transform root, SaveSlotId slot, int x, int y, bool selected, string emptyText, Action select, out Selectable button)
        {
            const int w = 290, h = 56, o = Ui.Widget + 2;
            var go = new GameObject($"SlotCard_{slot}");
            go.transform.SetParent(root, false);
            var t = go.transform;

            var btn = Ui.Button(t, "", x, y, w, select, h, showCursor: false, style: ButtonStyle.Card);
            btn.selected = selected;
            btn.visual(false);
            button = btn;

            var meta = SaveManager.GetSlotMetadata(slot);
            bool auto = slot == SaveSlotId.AutoSave;
            Ui.Label(t, SaveSlotMetadata.FormatSlotName(slot), x + 10, y + 7, selected ? Palette.Gold1 : Palette.Mist1, order: o);
            if (auto) Ui.Tag(t, "SYSTEM", x + 12 + PixelFont.Measure(SaveSlotMetadata.FormatSlotName(slot)), y + 7, TagKind.Cls, 11, o);

            if (!meta.isOccupied)
            {
                Ui.Label(t, "EMPTY", x + w - 10, y + 9, Palette.Mist3, Align.Right, order: o, font: FontKind.Interface);
                Ui.Label(t, emptyText, x + 10, y + 30, Palette.Mist3, order: o, font: FontKind.Interface);
                return go;
            }
            string place = PixelFont.Fit($"ACT {meta.committedAct} · {meta.committedDungeonName}".ToUpperInvariant(), 130, FontKind.Interface);
            Ui.Label(t, place, x + w - 10, y + 9, Palette.Ok, Align.Right, order: o, font: FontKind.Interface);
            Ui.Label(t, PixelFont.Fit(meta.partySummary.ToUpperInvariant(), w - 20, FontKind.Interface), x + 10, y + 26, Palette.Mist1, order: o, font: FontKind.Interface);
            string details = $"{meta.lastSavedAtIso} · {meta.hubGold:#,##0} HUB GOLD · {meta.GetFormattedPlaytime()}";
            Ui.Label(t, PixelFont.Fit(details.ToUpperInvariant(), w - 20, FontKind.Interface), x + 10, y + 39, Palette.Mist3, order: o, font: FontKind.Interface);
            return go;
        }
    }

    // Reusable confirmation modal for Overwrite, Delete, Load, and Abandon operations
    public class ConfirmationModal
    {
        readonly GameObject root;

        public ConfirmationModal(Transform parent, string title, string message, string confirmText, Action onConfirm, Action onCancel = null)
        {
            root = new GameObject("ConfirmationModal");
            root.transform.SetParent(parent, false);

            // Dark semi-transparent blocker over entire screen
            var blocker = Ui.Image(root.transform, PixelArt.Pixel, 0, 0, Ui.Overlay - 1, Palette.Hex(0x04060C, 230));
            blocker.transform.localScale = new Vector3(Ui.Width, Ui.Height, 1);

            int pw = 420, ph = 150;
            int px = (Ui.Width - pw) / 2;
            int py = (Ui.Height - ph) / 2;

            Ui.Panel(root.transform, px, py, pw, ph, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Label(root.transform, title.ToUpperInvariant(), px + pw / 2, py + 14, Palette.AntiqueGoldBright, Align.Center, 1, FontKind.Serif, Ui.Overlay);
            Ui.Divider(root.transform, px + 20, py + 34, pw - 40, Palette.DarkBorder, Palette.AntiqueGoldDim);

            string wrapped = PixelFont.Wrap(message, pw - 40);
            Ui.Label(root.transform, wrapped, px + 20, py + 46, Palette.WarmIvory, Align.Left, 1, FontKind.Interface, Ui.Overlay);

            var menu = root.AddComponent<Menu>();
            int btnW = 140, btnH = 22;
            int by = py + ph - 34;

            var cancelBtn = Ui.Button(root.transform, "CANCEL", px + 40, by, btnW, () =>
            {
                Close();
                onCancel?.Invoke();
            }, btnH, showCursor: false);

            var confirmBtn = Ui.Button(root.transform, confirmText.ToUpperInvariant(), px + pw - btnW - 40, by, btnW, () =>
            {
                Close();
                onConfirm?.Invoke();
            }, btnH, showCursor: false, style: ButtonStyle.Danger);

            menu.Add(cancelBtn);
            menu.Add(confirmBtn);
        }

        public void Close()
        {
            if (root != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    public class SaveScreen : GameScreen
    {
        SaveSlotId selectedSlot = SaveSlotId.Slot1;
        readonly GameObject[] slotObjects = new GameObject[5];
        readonly Selectable[] slotButtons = new Selectable[5];
        readonly List<Selectable> actionButtons = new List<Selectable>();
        Menu menu;
        Selectable headerBackBtn;
        Label feedbackLabel;
        Transform actionArea;

        protected override void Build()
        {
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(GameState.Hub);
            headerBackBtn = SaveSlotUi.Header(Root, "Save Game", "HUB ONLY", "SAVES PROGRESS COMMITTED BY YOUR LAST DUNGEON CLEAR.", () => Game.Go(GameState.Hub));

            // Left column (x: 20): AutoSave + Slot 1 + Slot 2
            RenderSlotCard(SaveSlotId.AutoSave, 20, 46);
            RenderSlotCard(SaveSlotId.Slot1, 20, 108);
            RenderSlotCard(SaveSlotId.Slot2, 20, 170);

            // Right column (x: 330): Slot 3 + Slot 4
            RenderSlotCard(SaveSlotId.Slot3, 330, 46);
            RenderSlotCard(SaveSlotId.Slot4, 330, 108);

            // Action / Info Box (x: 330, y: 170, w: 290, h: 130)
            Ui.Panel(Root, 330, 170, 290, 130, Palette.DeepCharcoal, Palette.DarkBorder);

            // Footer bar
            Ui.Rect(Root, 0, 314, Ui.Width, 46, Palette.Hex(0x0A0E18, 240), Ui.Panels);
            feedbackLabel = Ui.Label(Root, "Select a slot to save campaign progress.", 24, 324, Palette.WarmIvory);

            RefreshActionArea();
        }

        void RenderSlotCard(SaveSlotId slot, int x, int y)
        {
            int idx = (int)slot;
            if (slotObjects[idx] != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(slotObjects[idx]);
                else UnityEngine.Object.DestroyImmediate(slotObjects[idx]);
            }

            slotObjects[idx] = SaveSlotUi.SlotCard(Root, slot, x, y, selectedSlot == slot, "NO SAVED CAMPAIGN IN THIS SLOT.", () =>
            {
                selectedSlot = slot;
                RefreshCards();
                RefreshActionArea();
            }, out slotButtons[idx]);
        }

        void RefreshCards()
        {
            RenderSlotCard(SaveSlotId.AutoSave, 20, 46);
            RenderSlotCard(SaveSlotId.Slot1, 20, 108);
            RenderSlotCard(SaveSlotId.Slot2, 20, 170);
            RenderSlotCard(SaveSlotId.Slot3, 330, 46);
            RenderSlotCard(SaveSlotId.Slot4, 330, 108);
        }

        void RefreshActionArea()
        {
            if (actionArea != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(actionArea.gameObject);
                else UnityEngine.Object.DestroyImmediate(actionArea.gameObject);
            }

            actionArea = new GameObject("SaveActionArea").transform;
            actionArea.SetParent(Root, false);
            actionButtons.Clear();

            var meta = SaveManager.GetSlotMetadata(selectedSlot);
            int ax = 340, ay = 178;

            Ui.Label(actionArea, $"SELECTED: {SaveSlotMetadata.FormatSlotName(selectedSlot)}", ax, ay, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(actionArea, ax, ay + 18, 270, Palette.DarkBorder, Palette.AntiqueGoldDim);

            if (selectedSlot == SaveSlotId.AutoSave)
            {
                string info = "The Auto Save slot is managed automatically by the Sanctuary Hub.\n\nManual saves cannot overwrite this slot.";
                Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 26, Palette.Mist2);

                var backBtn = Ui.Button(actionArea, "BACK TO HUB", ax + 50, ay + 78, 170, () => Game.Go(GameState.Hub), 22, style: ButtonStyle.Slab);
                actionButtons.Add(backBtn);
            }
            else
            {
                if (!meta.isOccupied)
                {
                    string info = "This manual slot is empty.\nClick Save to record your current campaign.";
                    Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 26, Palette.Mist2);

                    var saveBtn = Ui.Button(actionArea, "SAVE GAME", ax + 10, ay + 78, 120, OnSaveClicked, 22, style: ButtonStyle.Primary);
                    var backBtn = Ui.Button(actionArea, "BACK", ax + 140, ay + 78, 120, () => Game.Go(GameState.Hub), 22, style: ButtonStyle.Slab);
                    actionButtons.Add(saveBtn);
                    actionButtons.Add(backBtn);
                }
                else
                {
                    string info = $"Occupied by {meta.partySummary}.\nOverwrite will replace this campaign.";
                    Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 24, Palette.Mist2);

                    var overwriteBtn = Ui.Button(actionArea, "OVERWRITE", ax + 5, ay + 62, 125, OnOverwriteClicked, 22, style: ButtonStyle.Primary);
                    var deleteBtn = Ui.Button(actionArea, "DELETE", ax + 140, ay + 62, 125, OnDeleteClicked, 22, style: ButtonStyle.Slab);
                    var backBtn = Ui.Button(actionArea, "BACK TO HUB", ax + 70, ay + 90, 130, () => Game.Go(GameState.Hub), 20, style: ButtonStyle.Slab);
                    actionButtons.Add(overwriteBtn);
                    actionButtons.Add(deleteBtn);
                    actionButtons.Add(backBtn);
                }
            }

            RebuildMenu();
        }

        void RebuildMenu()
        {
            menu.items.Clear();
            foreach (var b in slotButtons) if (b != null) menu.Add(b);
            foreach (var b in actionButtons) if (b != null) menu.Add(b);
            if (headerBackBtn != null) menu.Add(headerBackBtn);
            if (actionButtons.Count > 0) menu.Focus(slotButtons.Count(b => b != null), false, true);
        }

        void OnSaveClicked()
        {
            feedbackLabel.Set("Saving campaign progress...");
            if (SaveManager.Save(selectedSlot, out string err))
            {
                feedbackLabel.Set($"Successfully saved to {SaveSlotMetadata.FormatSlotName(selectedSlot)}!");
                RefreshCards();
                RefreshActionArea();
            }
            else
            {
                feedbackLabel.Set($"Save failed: {err}");
            }
        }

        void OnOverwriteClicked()
        {
            new ConfirmationModal(Root, "CONFIRM OVERWRITE",
                $"Overwrite {SaveSlotMetadata.FormatSlotName(selectedSlot)}?\nExisting campaign data will be permanently replaced.",
                "CONFIRM OVERWRITE",
                () =>
                {
                    if (SaveManager.Save(selectedSlot, out string err))
                    {
                        feedbackLabel.Set($"Successfully overwritten {SaveSlotMetadata.FormatSlotName(selectedSlot)}!");
                        RefreshCards();
                        RefreshActionArea();
                    }
                    else
                    {
                        feedbackLabel.Set($"Overwrite failed: {err}");
                    }
                });
        }

        void OnDeleteClicked()
        {
            new ConfirmationModal(Root, "CONFIRM DELETE",
                $"Delete save in {SaveSlotMetadata.FormatSlotName(selectedSlot)}?\nThis campaign will be permanently erased.",
                "CONFIRM DELETE",
                () =>
                {
                    if (SaveManager.Delete(selectedSlot, out string err))
                    {
                        feedbackLabel.Set($"Deleted {SaveSlotMetadata.FormatSlotName(selectedSlot)}.");
                        RefreshCards();
                        RefreshActionArea();
                    }
                    else
                    {
                        feedbackLabel.Set($"Delete failed: {err}");
                    }
                });
        }
    }

    public class LoadScreen : GameScreen
    {
        readonly GameState returnState;
        SaveSlotId selectedSlot = SaveSlotId.AutoSave;
        readonly GameObject[] slotObjects = new GameObject[5];
        readonly Selectable[] slotButtons = new Selectable[5];
        readonly List<Selectable> actionButtons = new List<Selectable>();
        Menu menu;
        Selectable headerBackBtn;
        Label feedbackLabel;
        Transform actionArea;

        public LoadScreen(GameState returnState = GameState.MainMenu)
        {
            this.returnState = returnState;
        }

        protected override void Build()
        {
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game.Go(returnState);
            headerBackBtn = SaveSlotUi.Header(Root, "Load Game", "RESTORES CAMPAIGN", "RETURNS YOU TO THE SANCTUARY HUB.", () => Game.Go(returnState));

            // Left column: AutoSave + Slot 1 + Slot 2
            RenderSlotCard(SaveSlotId.AutoSave, 20, 46);
            RenderSlotCard(SaveSlotId.Slot1, 20, 108);
            RenderSlotCard(SaveSlotId.Slot2, 20, 170);

            // Right column: Slot 3 + Slot 4
            RenderSlotCard(SaveSlotId.Slot3, 330, 46);
            RenderSlotCard(SaveSlotId.Slot4, 330, 108);

            // Action / Info Box
            Ui.Panel(Root, 330, 170, 290, 130, Palette.DeepCharcoal, Palette.DarkBorder);

            // Footer bar
            Ui.Rect(Root, 0, 314, Ui.Width, 46, Palette.Hex(0x0A0E18, 240), Ui.Panels);
            feedbackLabel = Ui.Label(Root, "Loading restores your campaign and returns to the Sanctuary Hub.", 24, 324, Palette.WarmIvory);

            RefreshActionArea();
        }

        void RenderSlotCard(SaveSlotId slot, int x, int y)
        {
            int idx = (int)slot;
            if (slotObjects[idx] != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(slotObjects[idx]);
                else UnityEngine.Object.DestroyImmediate(slotObjects[idx]);
            }

            slotObjects[idx] = SaveSlotUi.SlotCard(Root, slot, x, y, selectedSlot == slot, "EMPTY SLOT — NOTHING TO LOAD.", () =>
            {
                selectedSlot = slot;
                RefreshCards();
                RefreshActionArea();
            }, out slotButtons[idx]);
        }

        void RefreshCards()
        {
            RenderSlotCard(SaveSlotId.AutoSave, 20, 46);
            RenderSlotCard(SaveSlotId.Slot1, 20, 108);
            RenderSlotCard(SaveSlotId.Slot2, 20, 170);
            RenderSlotCard(SaveSlotId.Slot3, 330, 46);
            RenderSlotCard(SaveSlotId.Slot4, 330, 108);
        }

        void RefreshActionArea()
        {
            if (actionArea != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(actionArea.gameObject);
                else UnityEngine.Object.DestroyImmediate(actionArea.gameObject);
            }

            actionArea = new GameObject("LoadActionArea").transform;
            actionArea.SetParent(Root, false);
            actionButtons.Clear();

            var meta = SaveManager.GetSlotMetadata(selectedSlot);
            int ax = 340, ay = 178;

            Ui.Label(actionArea, $"SELECTED: {SaveSlotMetadata.FormatSlotName(selectedSlot)}", ax, ay, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(actionArea, ax, ay + 18, 270, Palette.DarkBorder, Palette.AntiqueGoldDim);

            if (!meta.isOccupied)
            {
                string info = "This slot is empty and cannot be loaded.\nSelect an occupied slot.";
                Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 26, Palette.Mist3);

                var backBtn = Ui.Button(actionArea, "BACK", ax + 70, ay + 78, 130, () => Game.Go(returnState), 22, style: ButtonStyle.Slab);
                actionButtons.Add(backBtn);
            }
            else
            {
                string info = $"Campaign: {meta.committedDungeonName}\nParty: {meta.partySummary}\nGold: {meta.hubGold} G";
                Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 24, Palette.WarmIvory);

                var loadBtn = Ui.Button(actionArea, "LOAD CAMPAIGN", ax + 10, ay + 78, 120, OnLoadClicked, 22, style: ButtonStyle.Primary);
                var backBtn = Ui.Button(actionArea, "BACK", ax + 140, ay + 78, 120, () => Game.Go(returnState), 22, style: ButtonStyle.Slab);
                actionButtons.Add(loadBtn);
                actionButtons.Add(backBtn);
            }

            RebuildMenu();
        }

        void RebuildMenu()
        {
            menu.items.Clear();
            foreach (var b in slotButtons) if (b != null) menu.Add(b);
            foreach (var b in actionButtons) if (b != null) menu.Add(b);
            if (headerBackBtn != null) menu.Add(headerBackBtn);
            if (actionButtons.Count > 0) menu.Focus(slotButtons.Count(b => b != null), false, true);
        }

        void OnLoadClicked()
        {
            // If in Hub, warn that current in-memory Hub state will be replaced
            if (Game?.Run != null)
            {
                new ConfirmationModal(Root, "CONFIRM LOAD",
                    $"Load {SaveSlotMetadata.FormatSlotName(selectedSlot)}?\nAny unsaved Hub changes will be lost.",
                    "CONFIRM LOAD",
                    () => ExecuteLoad());
            }
            else
            {
                ExecuteLoad();
            }
        }

        void ExecuteLoad()
        {
            feedbackLabel.Set("Loading campaign...");
            if (SaveManager.Load(selectedSlot, out string err))
            {
                feedbackLabel.Set("Campaign loaded successfully!");
            }
            else
            {
                feedbackLabel.Set($"Load failed: {err}");
            }
        }
    }
}

