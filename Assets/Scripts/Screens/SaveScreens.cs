using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
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
        Label feedbackLabel;
        Transform actionArea;

        protected override void Build()
        {
            Backdrop.Create(Root, false, 24, Palette.DeepCharcoal, Palette.Hex(0x1B2133), Palette.Gold2);

            Ui.Button(Root, "< BACK", 16, 10, 56, () => Game.Go(GameState.Hub), 20, style: ButtonStyle.Slab, showCursor: false);
            Ui.Label(Root, "SAVE GAME", 80, 12, Palette.AntiqueGoldBright, Align.Left, 2, FontKind.DotGothic);
            Ui.Badge(Root, "HUB ONLY", 210, 12, 70, 18, Palette.Hex(0x131826), Palette.Gold3, Palette.Gold2);
            Ui.Label(Root, "Saves progress committed by your last Dungeon Clear", 290, 15, Palette.Mist2, font: FontKind.DotGothic);
            Ui.Divider(Root, 16, 36, 608, Palette.DarkBorder, Palette.AntiqueGoldBright);

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

            var cardGo = new GameObject($"SlotCard_{slot}");
            cardGo.transform.SetParent(Root, false);
            slotObjects[idx] = cardGo;

            bool isSelected = selectedSlot == slot;
            var meta = SaveManager.GetSlotMetadata(slot);

            Color borderColor = isSelected ? Palette.AntiqueGoldBright : Palette.DarkBorder;
            Color fillColor = slot == SaveSlotId.AutoSave ? Palette.Hex(0x131826) : Palette.DeepCharcoal;

            Ui.Panel(cardGo.transform, x, y, 290, 56, fillColor, borderColor);

            // Clickable button covering the card
            var btn = Ui.Button(cardGo.transform, "", x, y, 290, () =>
            {
                selectedSlot = slot;
                RefreshCards();
                RefreshActionArea();
            }, 56, showCursor: false);
            var menu = Root.gameObject.GetComponent<Menu>() ?? Root.gameObject.AddComponent<Menu>();
            menu.Add(btn);

            string slotName = SaveSlotMetadata.FormatSlotName(slot);
            if (slot == SaveSlotId.AutoSave) slotName += " [SYSTEM]";
            Ui.Label(cardGo.transform, slotName, x + 10, y + 8, isSelected ? Palette.Gold1 : Palette.AntiqueGold, Align.Left, 1, FontKind.Serif);

            if (!meta.isOccupied)
            {
                Ui.Label(cardGo.transform, "[EMPTY]", x + 280, y + 8, Palette.Mist3, Align.Right);
                Ui.Label(cardGo.transform, "No saved campaign recorded in this slot.", x + 10, y + 26, Palette.Mist2);
            }
            else
            {
                string badge = $"ACT {meta.committedAct}: {meta.committedDungeonName}";
                Ui.Label(cardGo.transform, PixelFont.Fit(badge, 140), x + 280, y + 8, Palette.Emerald, Align.Right);
                Ui.Label(cardGo.transform, PixelFont.Fit(meta.partySummary, 270), x + 10, y + 24, Palette.WarmIvory);
                string details = $"{meta.lastSavedAtIso} | {meta.hubGold}G | TIME: {meta.GetFormattedPlaytime()}";
                Ui.Label(cardGo.transform, details, x + 10, y + 38, Palette.Mist2);
            }
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

            var meta = SaveManager.GetSlotMetadata(selectedSlot);
            int ax = 340, ay = 178;

            Ui.Label(actionArea, $"SELECTED: {SaveSlotMetadata.FormatSlotName(selectedSlot)}", ax, ay, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(actionArea, ax, ay + 18, 270, Palette.DarkBorder, Palette.AntiqueGoldDim);

            var menu = actionArea.gameObject.AddComponent<Menu>();

            if (selectedSlot == SaveSlotId.AutoSave)
            {
                string info = "The Auto Save slot is managed automatically by the Sanctuary Hub.\n\nManual saves cannot overwrite this slot.";
                Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 26, Palette.Mist2);

                var backBtn = Ui.Button(actionArea, "BACK TO HUB", ax + 50, ay + 78, 170, () => Game.Go(GameState.Hub), 22, style: ButtonStyle.Slab);
                menu.Add(backBtn);
            }
            else
            {
                if (!meta.isOccupied)
                {
                    string info = "This manual slot is empty.\nClick Save to record your current campaign.";
                    Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 26, Palette.Mist2);

                    var saveBtn = Ui.Button(actionArea, "SAVE GAME", ax + 10, ay + 78, 120, OnSaveClicked, 22, style: ButtonStyle.Primary);
                    var backBtn = Ui.Button(actionArea, "BACK", ax + 140, ay + 78, 120, () => Game.Go(GameState.Hub), 22, style: ButtonStyle.Slab);
                    menu.Add(saveBtn);
                    menu.Add(backBtn);
                }
                else
                {
                    string info = $"Occupied by {meta.partySummary}.\nOverwrite will replace this campaign.";
                    Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 24, Palette.Mist2);

                    var overwriteBtn = Ui.Button(actionArea, "OVERWRITE", ax + 5, ay + 62, 125, OnOverwriteClicked, 22, style: ButtonStyle.Primary);
                    var deleteBtn = Ui.Button(actionArea, "DELETE", ax + 140, ay + 62, 125, OnDeleteClicked, 22, style: ButtonStyle.Slab);
                    var backBtn = Ui.Button(actionArea, "BACK TO HUB", ax + 70, ay + 90, 130, () => Game.Go(GameState.Hub), 20, style: ButtonStyle.Slab);
                    menu.Add(overwriteBtn);
                    menu.Add(deleteBtn);
                    menu.Add(backBtn);
                }
            }
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
        Label feedbackLabel;
        Transform actionArea;

        public LoadScreen(GameState returnState = GameState.MainMenu)
        {
            this.returnState = returnState;
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 24, Palette.DeepCharcoal, Palette.Hex(0x1B2133), Palette.Gold2);

            Ui.Button(Root, "< BACK", 16, 10, 56, () => Game.Go(returnState), 20, style: ButtonStyle.Slab, showCursor: false);
            Ui.Label(Root, "LOAD GAME", 80, 12, Palette.AntiqueGoldBright, Align.Left, 2, FontKind.DotGothic);
            Ui.Badge(Root, "RESTORES CAMPAIGN", 210, 12, 116, 18, Palette.Hex(0x131826), Palette.Gold3, Palette.Gold2);
            Ui.Label(Root, "Restores your campaign and returns to the Sanctuary Hub", 336, 15, Palette.Mist2, font: FontKind.DotGothic);
            Ui.Divider(Root, 16, 36, 608, Palette.DarkBorder, Palette.AntiqueGoldBright);

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

            var cardGo = new GameObject($"LoadSlotCard_{slot}");
            cardGo.transform.SetParent(Root, false);
            slotObjects[idx] = cardGo;

            bool isSelected = selectedSlot == slot;
            var meta = SaveManager.GetSlotMetadata(slot);

            Color borderColor = isSelected ? Palette.AntiqueGoldBright : Palette.DarkBorder;
            Color fillColor = slot == SaveSlotId.AutoSave ? Palette.Hex(0x131826) : Palette.DeepCharcoal;

            Ui.Panel(cardGo.transform, x, y, 290, 56, fillColor, borderColor);

            var btn = Ui.Button(cardGo.transform, "", x, y, 290, () =>
            {
                selectedSlot = slot;
                RefreshCards();
                RefreshActionArea();
            }, 56, showCursor: false);
            var menu = Root.gameObject.GetComponent<Menu>() ?? Root.gameObject.AddComponent<Menu>();
            menu.Add(btn);

            string slotName = SaveSlotMetadata.FormatSlotName(slot);
            if (slot == SaveSlotId.AutoSave) slotName += " [SYSTEM]";
            Ui.Label(cardGo.transform, slotName, x + 10, y + 8, isSelected ? Palette.Gold1 : Palette.AntiqueGold, Align.Left, 1, FontKind.Serif);

            if (!meta.isOccupied)
            {
                Ui.Label(cardGo.transform, "[EMPTY]", x + 280, y + 8, Palette.Mist3, Align.Right);
                Ui.Label(cardGo.transform, "Empty slot — cannot be loaded.", x + 10, y + 26, Palette.Mist3);
            }
            else
            {
                string badge = $"ACT {meta.committedAct}: {meta.committedDungeonName}";
                Ui.Label(cardGo.transform, PixelFont.Fit(badge, 140), x + 280, y + 8, Palette.Emerald, Align.Right);
                Ui.Label(cardGo.transform, PixelFont.Fit(meta.partySummary, 270), x + 10, y + 24, Palette.WarmIvory);
                string details = $"{meta.lastSavedAtIso} | {meta.hubGold}G | TIME: {meta.GetFormattedPlaytime()}";
                Ui.Label(cardGo.transform, details, x + 10, y + 38, Palette.Mist2);
            }
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

            var meta = SaveManager.GetSlotMetadata(selectedSlot);
            int ax = 340, ay = 178;

            Ui.Label(actionArea, $"SELECTED: {SaveSlotMetadata.FormatSlotName(selectedSlot)}", ax, ay, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);
            Ui.Divider(actionArea, ax, ay + 18, 270, Palette.DarkBorder, Palette.AntiqueGoldDim);

            var menu = actionArea.gameObject.AddComponent<Menu>();

            if (!meta.isOccupied)
            {
                string info = "This slot is empty and cannot be loaded.\nSelect an occupied slot.";
                Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 26, Palette.Mist3);

                var backBtn = Ui.Button(actionArea, "BACK", ax + 70, ay + 78, 130, () => Game.Go(returnState), 22, style: ButtonStyle.Slab);
                menu.Add(backBtn);
            }
            else
            {
                string info = $"Campaign: {meta.committedDungeonName}\nParty: {meta.partySummary}\nGold: {meta.hubGold} G";
                Ui.Label(actionArea, PixelFont.Wrap(info, 270), ax, ay + 24, Palette.WarmIvory);

                var loadBtn = Ui.Button(actionArea, "LOAD CAMPAIGN", ax + 10, ay + 78, 120, OnLoadClicked, 22, style: ButtonStyle.Primary);
                var backBtn = Ui.Button(actionArea, "BACK", ax + 140, ay + 78, 120, () => Game.Go(returnState), 22, style: ButtonStyle.Slab);
                menu.Add(loadBtn);
                menu.Add(backBtn);
            }
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

