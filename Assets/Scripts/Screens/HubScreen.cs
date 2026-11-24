using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure
{
    using UiLabel = UnityEngine.UIElements.Label;
    // Hub — Figma "04 — Hub — Geistwald City" rendered via UI Toolkit:
    // night town silhouette, top bar (auto-save tag, Save/Load/Main Menu, Hub Gold),
    // interactive facility list + Dungeon Entrance, tooltip card, run progress chips, and party strip.
    public class HubScreen : GameScreen
    {
        UitkMenu menu;
        UiLabel tipTitleLbl, tipBodyLbl, feedbackLbl;

        protected override void Build()
        {
            // Auto-save on Hub arrival if not already auto-saved or returning from dungeon map
            if (Run != null && PersistenceCommitHandler.ActiveCommittedSnapshot == null)
            {
                PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateCommittedSnapshot(Run));
                SaveManager.AutoSave(out _);
            }
            else if (Run != null && (Game?.PreviousState == GameState.Map || Game?.PreviousState == GameState.CharacterCreation
                || Game?.PreviousState == GameState.OpeningSequence || Game?.PreviousState == GameState.PrologueClear
                || Game?.PreviousState == GameState.StageClear || Game?.PreviousState == GameState.DungeonClear))
            {
                SaveManager.AutoSave(out _);
            }

            UiRoot = FigmaUi.Mount(Root.gameObject, "04---Hub---Geistwald-City");
            FigmaUi.HideByClass(UiRoot, "a-wipe-resets-run-progress-equipment-is-always-kept-vars");
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onFocus = ShowTip;
            menu.onBack = () => Game?.Go(GameState.Settings); // Esc opens Settings; its Back returns here (SettingsScreen.GoBack)

            BindTopBar();
            BindFacilityMenu();
            BindProgress();
            BindPartyStrip();
            EnsureFeedbackLabel();

            if (Game?.Settings != null && !Game.Settings.hubWelcomeShown)
            {
                PrologueBanner.Attach(UiRoot, "THE HUB: Prologue complete! Prepare your party (Shop, Skills, Gear, Talents) — Dungeon 1 has real stakes and limited retries.", top: 86f, left: 376f, width: 880f);
                Game.Settings.hubWelcomeShown = true;
                Game.Settings.Save();
            }
        }

        void BindTopBar()
        {
            var autoTag = FigmaUi.FindInstanceWithText(UiRoot, "text-0", "AUTO SAVED · 10:35");
            if (autoTag != null)
            {
                var auto = SaveManager.GetSlotMetadata(SaveSlotId.AutoSave);
                string when = auto != null && auto.isOccupied && auto.lastSavedTicks > 0
                    ? new DateTime(auto.lastSavedTicks).ToString("HH:mm")
                    : "HUB";
                FigmaUi.SetInstanceText(autoTag, "text-0", $"AUTO SAVED · {when}");
            }

            int gold = Run != null ? Run.HubGold : 0;
            var goldTag = FigmaUi.FindInstanceWithText(UiRoot, "text-0", "0 GOLD");
            if (goldTag != null)
            {
                FigmaUi.SetInstanceText(goldTag, "text-0", $"{gold:#,##0} GOLD");
                FigmaUi.AnchorRight(goldTag, 24f);
            }

            var saveBtn = FigmaUi.FindButton(UiRoot, "Save");
            if (saveBtn?.parent != null)
            {
                var btnRow = saveBtn.parent;
                btnRow.style.left = StyleKeyword.Auto;
                btnRow.style.right = 210;
            }
            if (saveBtn != null)
            {
                saveBtn.style.marginRight = 8;
                menu.Add(saveBtn, "Save", () => Game?.Go(GameState.Save), tooltip: "SAVE COMMITTED CAMPAIGN PROGRESS TO A SLOT.");
            }
            var loadBtn = FigmaUi.FindButton(UiRoot, "Load");
            if (loadBtn != null)
            {
                loadBtn.style.marginRight = 8;
                menu.Add(loadBtn, "Load", () => Game?.Go(GameState.Load), tooltip: "LOAD A SAVED CAMPAIGN.");
            }
            if (saveBtn?.parent != null)
            {
                var settingsBtn = FigmaUi.CreateButton("Settings", "button-slab");
                settingsBtn.style.marginRight = 8;
                saveBtn.parent.Insert(saveBtn.parent.childCount - 1, settingsBtn);
                menu.Add(settingsBtn, "Settings", () => Game?.Go(GameState.Settings), tooltip: "AUDIO, GRAPHICS, CONTROLS AND QTE SETTINGS (ESC).");
            }
            var mainMenuBtn = FigmaUi.FindButton(UiRoot, "Main Menu");
            if (mainMenuBtn != null) menu.Add(mainMenuBtn, "Main Menu", OnTitleClicked, tooltip: "RETURN TO THE TITLE SCREEN.");
        }

        void BindFacilityMenu()
        {
            var party = Run?.Party ?? new List<Unit>();
            int sp = party.Sum(u => u.skillLoadout?.SkillPoints ?? 0);
            int dead = party.Count(u => !u.Alive);
            int relics = Run?.Relics.Count ?? 0;

            BindFacilityRow("Character / Party", () => Game?.Go(GameState.Party), "VIEW THE PARTY, STATUS AND LINEUP.");
            BindFacilityRow("Equipment", () => Game?.Go(GameState.Equipment), "EQUIP GEAR ON EACH MEMBER AND COMPARE STATS.");
            BindFacilityRow("Inventory", () => Game?.Go(GameState.Inventory), "EQUIPMENT BAG, CONSUMABLES, MATERIALS AND KEY ITEMS.");
            BindFacilityRow("Shop", () => Game?.Go(GameState.HubShop), "CONSUMABLES FOR GOLD. UNLIMITED STOCK.");
            BindFacilityRow("Armory", () => Game?.Go(GameState.Armory), "WEAPONS AND ARMOR FOR GOLD.");
            BindFacilityRow("Blacksmith", () => Game?.Go(GameState.Blacksmith), "UPGRADE GEAR. UPGRADES RAISE BASE STATS ONLY.");
            BindFacilityRow("Training Ground", () => Game?.Go(GameState.Skills), "SPEND SKILL POINTS AND CHOOSE TALENTS.",
                badgeText: sp > 0 ? $"{sp} SP" : null, badgeClass: "uitk-badge-ok");
            BindFacilityRow("Temple", () => Game?.Go(GameState.Temple), "REVIVE FALLEN MEMBERS AND RECOVER HP / MP.",
                badgeText: dead > 0 ? $"{dead} DEAD" : null, badgeClass: "uitk-badge-err");
            BindFacilityRow("Magic Shop", () => Game?.Go(GameState.MagicShop), "BUY AND SELL PERMANENT RELICS.",
                replaceRightText: $"RELICS {relics}/5");

            tipTitleLbl = UiRoot.Q<UiLabel>(className: "dungeon-entrance-vars");
            tipBodyLbl = FigmaUi.FindLabel(UiRoot, "BEGIN DUNGEON 1 OF 8. A NEW RUN STARTS WITH 0 SPOILS.");
            var tipPanel = UiRoot.Q<VisualElement>(className: "panel-gold__i-2");
            if (tipPanel != null)
            {
                tipPanel.style.width = 280;
                tipPanel.style.height = 152;
                tipPanel.style.overflow = Overflow.Hidden;
            }
            if (tipBodyLbl != null)
            {
                tipBodyLbl.style.width = 252;
                tipBodyLbl.style.whiteSpace = WhiteSpace.Normal;
            }

            int act = PersistenceCommitHandler.ActiveCommittedSnapshot?.nextDungeonAct ?? Run?.Act ?? 1;
            int totalStages = MapGenerator.GetStageCount(act);
            int targetStage = ResolveTargetStage(act);
            var gate = FigmaUi.FindInstanceWithText(UiRoot, "text-1", "Dungeon Entrance");
            var enterTag = FigmaUi.FindInstanceWithText(UiRoot, "text-0", "ENTER");
            if (gate != null)
            {
                var gateRoot = gate.parent is TemplateContainer ? gate.parent : gate;
                gateRoot.style.left = 44;
                gateRoot.style.width = 298;
                gate.style.width = 298;
                var gateInner = gate.childCount > 0 ? gate[0] : gate;
                if (enterTag != null)
                {
                    if (targetStage > 1 && totalStages > 1)
                        FigmaUi.SetInstanceText(enterTag, "text-0", $"STAGE {targetStage}/{totalStages}");
                    var enterTc = enterTag.GetFirstAncestorOfType<TemplateContainer>() ?? enterTag;
                    enterTc.RemoveFromHierarchy();
                    enterTc.pickingMode = PickingMode.Ignore;
                    enterTc.style.position = Position.Relative;
                    enterTc.style.left = StyleKeyword.Auto;
                    enterTc.style.top = StyleKeyword.Auto;
                    enterTc.style.marginLeft = StyleKeyword.Auto;
                    gateInner.Add(enterTc);
                }
                int retries = Run?.RetriesLeft ?? RunConfig.RetriesPerExpedition;
                string tip;
                if (act > 8)
                {
                    tip = "ALL EIGHT LAYERS ARE CLEAR.";
                }
                else if (targetStage > 1 && totalStages > 1)
                {
                    string stageName = MapGenerator.GetDungeonPlan(act).GetStage(targetStage)?.stageName;
                    string stageSuffix = !string.IsNullOrEmpty(stageName) ? $" ({stageName.ToUpperInvariant()})" : "";
                    tip = $"RESUME DUNGEON {act} OF 8 · STAGE {targetStage} OF {totalStages}{stageSuffix}.\nSTARTS WITH NO SPOILS AND NO MODIFIERS.\nRETRIES LEFT: {retries}. A WIPE OR ABANDON SPENDS ONE.";
                }
                else
                {
                    tip = $"BEGIN DUNGEON {act} OF 8. A NEW RUN STARTS WITH NO SPOILS.\nRETRIES LEFT: {retries}. A WIPE OR ABANDON SPENDS ONE.";
                }
                var gateItem = menu.Add(gate, "Dungeon Entrance", OnEmbarkClicked, tooltip: tip);
                menu.Focus(gateItem, false, true);
            }
        }

        void BindFacilityRow(string labelText, Action go, string tip, string badgeText = null, string badgeClass = null, string replaceRightText = null)
        {
            var lbl = FigmaUi.FindLabel(UiRoot, labelText);
            var row = lbl?.parent;
            if (row == null) return;

            row.AddToClassList("uitk-flat-item");
            if (row.parent != null)
            {
                row.parent.style.left = 42;
                row.parent.style.width = 302;
                row.parent.style.overflow = Overflow.Hidden;
            }
            row.style.left = 0;
            row.style.width = 302;
            row.style.height = 38;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.paddingRight = 10;
            row.style.overflow = Overflow.Hidden;
            var cursor = new UiLabel("▸") { name = "uitkCursor" };
            cursor.AddToClassList("uitk-flat-cursor");
            cursor.AddToClassList("uitk-cursor-hidden");
            row.Insert(0, cursor);
            lbl.AddToClassList("uitk-flat-label");
            lbl.style.flexGrow = 1;
            lbl.style.flexShrink = 1;
            lbl.style.minWidth = 0;

            if (row.childCount > 2 && row[2] is UiLabel rightLbl)
            {
                if (!string.IsNullOrEmpty(replaceRightText))
                    rightLbl.text = replaceRightText;
                rightLbl.style.fontSize = 11;
                rightLbl.style.marginLeft = 6;
                rightLbl.style.flexShrink = 0;
            }

            if (!string.IsNullOrEmpty(badgeText))
            {
                var badge = new UiLabel(badgeText);
                badge.AddToClassList(badgeClass);
                badge.style.marginLeft = 8;
                row.Add(badge);
            }

            menu.Add(row, labelText, go, tooltip: tip);
        }

        void ShowTip(UitkMenu.Item item)
        {
            if (item == null) return;
            if (tipTitleLbl != null) tipTitleLbl.text = item.text;
            if (tipBodyLbl != null) tipBodyLbl.text = item.tooltip ?? "";
        }

        void BindProgress()
        {
            int act = PersistenceCommitHandler.ActiveCommittedSnapshot?.nextDungeonAct ?? Run?.Act ?? 1;
            int cleared = Math.Clamp(act - 1, 0, 8);
            var clearedLbl = FigmaUi.FindLabel(UiRoot, "0/8 CLEARED");
            if (clearedLbl != null) clearedLbl.text = $"{cleared}/8 CLEARED";

            for (int i = 1; i <= 8; i++)
            {
                var chip = FigmaUi.FindInstanceWithText(UiRoot, "text-0", i.ToString())?.parent;
                if (chip == null) continue;
                bool done = i < act, now = i == act;
                var inner = chip.childCount > 0 ? chip[0] : chip;
                inner.style.borderTopColor = now ? Palette.Gold2 : done ? Palette.Mist3 : Palette.Ink5;
                inner.style.borderRightColor = now ? Palette.Gold2 : done ? Palette.Mist3 : Palette.Ink5;
                inner.style.borderBottomColor = now ? Palette.Gold2 : done ? Palette.Mist3 : Palette.Ink5;
                inner.style.borderLeftColor = now ? Palette.Gold2 : done ? Palette.Mist3 : Palette.Ink5;
                inner.style.backgroundColor = now ? new Color(0.17f, 0.13f, 0.06f, 1f) : done ? Palette.Ink4 : Color.clear;
                var txt = chip.Q<UiLabel>("text-0");
                if (txt != null) txt.style.color = now ? Palette.Gold1 : done ? Palette.Mist2 : Palette.Ink5;
            }
        }

        void BindPartyStrip()
        {
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var cards = UiRoot.Query<VisualElement>(className: "hero-card").ToList();
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (i >= party.Count)
                {
                    card.style.display = DisplayStyle.None;
                    continue;
                }
                card.style.display = DisplayStyle.Flex;
                var u = party[i];

                var portraitBox = card[0];
                if (portraitBox != null)
                {
                    portraitBox.style.width = 44;
                    portraitBox.style.height = 44;
                    portraitBox.style.marginRight = 8;
                    if (portraitBox.childCount > 0 && u.sprite != null)
                        FigmaUi.SetBackgroundContain(portraitBox[0], FigmaUi.GetSpriteTexture(u.sprite, "unit_" + (u.classDef?.id ?? u.name)));
                }

                var infoCol = card.childCount > 1 ? card[1] : null;
                if (infoCol == null) continue;
                var labels = infoCol.Query<UiLabel>().ToList();
                if (labels.Count > 0)
                {
                    labels[0].text = u.name;
                    labels[0].style.color = u.Alive ? Palette.Mist1 : Palette.Mist3;
                    labels[0].style.maxWidth = 76;
                    labels[0].style.flexShrink = 1;
                    labels[0].style.marginRight = 6;
                    labels[0].style.overflow = Overflow.Hidden;
                }
                if (labels.Count > 1)
                {
                    labels[1].text = $"LV {u.level}";
                    labels[1].style.flexShrink = 0;
                }

                var hpGauge = infoCol.Q<VisualElement>(className: "gauge-hp");
                var mpGauge = infoCol.Q<VisualElement>(className: "gauge-mp");
                if (hpGauge != null) { hpGauge.style.width = Length.Percent(100); hpGauge.style.height = 8; hpGauge.style.marginTop = 4; }
                if (mpGauge != null) { mpGauge.style.width = Length.Percent(100); mpGauge.style.height = 8; mpGauge.style.marginTop = 4; }
                float hpRatio = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
                float mpRatio = (float)u.mp / Mathf.Max(1, u.stats.maxMp);
                FigmaUi.SetGaugeFill(hpGauge, hpRatio, hpRatio < 0.25f ? Palette.HpLow : Palette.Hp);
                FigmaUi.SetGaugeFill(mpGauge, mpRatio, Palette.Mp);
            }
        }

        void EnsureFeedbackLabel()
        {
            feedbackLbl = new UiLabel { name = "HubFeedback" };
            feedbackLbl.style.position = Position.Absolute;
            feedbackLbl.style.left = 380;
            feedbackLbl.style.top = 560;
            feedbackLbl.style.fontSize = 16;
            feedbackLbl.style.color = Palette.HpLow;
            (UiRoot.Q<VisualElement>(className: "_04-hub-geistwald-city") ?? UiRoot).Add(feedbackLbl);
        }

        void OnEmbarkClicked()
        {
            if (Run == null) return;
            if (Run.Party.All(u => !u.Alive))
            {
                Sfx.Play(Sound.Deny);
                if (feedbackLbl != null) feedbackLbl.text = "ALL HEROES ARE DOWN — REVIVE AT THE TEMPLE FIRST.";
                return;
            }

            SaveManager.AutoSave(out _);

            int targetAct = PersistenceCommitHandler.ActiveCommittedSnapshot != null
                ? PersistenceCommitHandler.ActiveCommittedSnapshot.nextDungeonAct
                : Run.Act;

            if (targetAct > 8)
            {
                Game?.Go(GameState.Victory);
                return;
            }

            int targetStage = ResolveTargetStage(targetAct);
            Run.BeginDungeonAttempt(targetAct, System.Environment.TickCount, targetStage);
            Game?.Go(GameState.Map);
        }

        int ResolveTargetStage(int targetAct)
        {
            var snap = PersistenceCommitHandler.ActiveCommittedSnapshot;
            if (snap != null)
                return snap.nextDungeonAct == targetAct ? snap.NormalizedNextStage() : 1;
            return Run != null && Run.Act == targetAct ? Math.Clamp(Run.Stage, 1, MapGenerator.GetStageCount(targetAct)) : 1;
        }

        void OnTitleClicked()
        {
            FigmaUi.ShowConfirmationModal(UiRoot, "RETURN TO TITLE",
                "Return to the Main Menu?\nEnsure your progress is saved before exiting the Hub.",
                "RETURN TO TITLE",
                () => Game?.Go(GameState.MainMenu));
        }
    }
}
