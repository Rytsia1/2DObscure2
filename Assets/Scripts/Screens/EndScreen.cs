using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Run summary for Victory and Defeat — migrated to Figma 23---Dungeon-Clear & 24---Run-Failure---Defeat (1280x720 UI Toolkit).
    public class EndScreen : GameScreen
    {
        readonly bool victory;
        public EndScreen() : this(true) { }
        public EndScreen(bool victory) { this.victory = victory; }

        protected override void Build()
        {
            if (!victory) { BuildDefeat(); return; }
            BuildVictory();
        }

        void BuildVictory()
        {
            Sfx.Play(Sound.VictoryStinger);
            MountFigma("23---Dungeon-Clear");
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            var s = Run != null ? Run.Stats : new RunStats();
            int act = Run != null ? Run.Act : 8;
            int total = Run != null ? Run.HubGold : 0;

            static void CenterAcross(UiLabel lbl)
            {
                if (lbl == null) return;
                lbl.style.left = 0;
                lbl.style.right = 0;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            var dLbl = UiRoot.Q<UiLabel>(className: "dungeon-1-of-8-vars-2");
            if (dLbl != null) { dLbl.text = "EXPEDITION COMPLETE"; CenterAcross(dLbl); }

            var titleLbl = UiRoot.Q<UiLabel>(className: "dungeon-clear-vars");
            if (titleLbl != null) { titleLbl.text = "THE ABYSS IS SEALED"; CenterAcross(titleLbl); }

            var mapLbl = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-3");
            if (mapLbl != null) { mapLbl.text = Run?.Map?.name ?? "Abyss"; CenterAcross(mapLbl); }

            var pipLbl = UiRoot.Q<UiLabel>(className: "node-vars-13");
            if (pipLbl != null) { pipLbl.text = "◆◆◆◆◆◆◆◆"; CenterAcross(pipLbl); }

            var runTags = UiRoot.Query<VisualElement>(className: "currency-tag-run").ToList();
            if (runTags.Count > 0) FigmaUi.SetInstanceText(runTags[0], "text-0", $"{s.goldEarned} SPOILS");

            var hubTags = UiRoot.Query<VisualElement>(className: "currency-tag-hub").ToList();
            if (hubTags.Count > 0) FigmaUi.SetInstanceText(hubTags[0], "text-0", $"+{s.goldEarned} GOLD");

            var totalLbl = UiRoot.Q<UiLabel>(className: "_745-vars");
            if (totalLbl != null) totalLbl.text = total.ToString("#,##0");

            var encLbl = UiRoot.Q<UiLabel>(className: "_6-vars-3");
            if (encLbl != null) encLbl.text = s.battlesWon.ToString();

            var eliteLbl = UiRoot.Q<UiLabel>(className: "_1-vars-9");
            if (eliteLbl != null) eliteLbl.text = s.elitesWon.ToString();

            var chLbl = UiRoot.Q<UiLabel>(className: "_11-vars-2");
            if (chLbl != null) chLbl.text = s.nodesCompleted.ToString();

            var relLbl = UiRoot.Q<UiLabel>(className: "_1-vars-10");
            if (relLbl != null) relLbl.text = (Run?.Relics.Count ?? 0).ToString();

            var nextLbl = UiRoot.Q<UiLabel>(className: "next-dungeon-2-of-8-unlocked-vars");
            if (nextLbl != null)
            {
                var dur = Run != null ? Run.Duration : System.TimeSpan.Zero;
                nextLbl.text = $"TIME {(int)dur.TotalMinutes}:{dur.Seconds:00} · ACT {act} CLEARED";
            }

            DungeonClearScreen.HideDungeonClearReminders(UiRoot);

            var newRunInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-9");
            if (newRunInst != null)
            {
                FigmaUi.SetInstanceText(newRunInst, "text-1", "NEW RUN");
                var btn = newRunInst.Q<Button>() ?? newRunInst;
                var newRunItem = menu.Add(btn, "NEW RUN", () => Game.StartRun(), visual: on =>
                {
                    FigmaUi.SetInstanceText(newRunInst, "text-0", on ? "▸" : "");
                    FigmaUi.ApplyDefaultFocusVisual(btn, on, false, true);
                });
                newRunItem.visual(true);
            }

            // Add secondary TITLE button aligned with right edge of content panels (1090px)
            var titleBtn = FigmaUi.CreateButton("TITLE", "button-slab", 180);
            titleBtn.style.position = Position.Absolute;
            titleBtn.style.left = 910;
            titleBtn.style.top = 590;
            (UiRoot.Q<VisualElement>(className: "_23-dungeon-clear") ?? UiRoot).Add(titleBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(titleBtn, "TITLE", () => Game.EndRun()), titleBtn);
        }

        void BuildDefeat()
        {
            Sfx.Play(Sound.DefeatStinger);
            MountFigma("24---Run-Failure---Defeat");
            static void CenterAcross(UiLabel lbl)
            {
                if (lbl == null) return;
                lbl.style.left = 0;
                lbl.style.right = 0;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            var defeatTitle = FigmaUi.FindLabel(UiRoot, "DEFEAT");
            defeatTitle?.AddToClassList("uitk-font-display");
            CenterAcross(defeatTitle);
            CenterAcross(UiRoot.Q<UiLabel>(className: "expedition-ended-vars"));
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            int act = Run != null ? Run.Act : 1;
            int floor = Run != null ? Run.Floor : 1;
            string stageText = Run != null && Run.TotalStages > 1 ? $" · STAGE {Run.Stage}/{Run.TotalStages}" : "";
            var s = Run != null ? Run.Stats : new RunStats();
            bool lost = Run != null && Run.ExpeditionLost;

            var subLbl = UiRoot.Q<UiLabel>(className: "fell-in-dungeon-1-of-8-node-4-progress-from-earlier-clears-is-safe-vars");
            if (subLbl != null)
            {
                subLbl.text = $"FELL IN DUNGEON {act} OF 8{stageText} · NODE {floor}";
                CenterAcross(subLbl);
            }

            var gainedTitle = UiRoot.Q<UiLabel>(className: "gained-in-dungeon-1-vars");
            if (gainedTitle != null)
            {
                gainedTitle.text = $"Gained in Dungeon {act}";
                gainedTitle.style.flexGrow = 1;
                gainedTitle.style.marginRight = 10;
                var hdrRow = gainedTitle.parent;
                if (hdrRow != null && hdrRow.childCount > 0)
                    hdrRow[0].style.marginRight = 10;
            }

            var runGoldTag = UiRoot.Q<VisualElement>(className: "currency-tag-run__i");
            var lostGoldNote = UiRoot.Q<UiLabel>(className: "run-gold-held-in-dungeon-1-vars");
            var strike = UiRoot.Q<VisualElement>(className: "strike-vars");
            if (runGoldTag != null) FigmaUi.SetInstanceText(runGoldTag, "text-0", $"{s.goldEarned} SPOILS");
            if (lostGoldNote != null) lostGoldNote.text = $"SPOILS HELD IN DUNGEON {act}.";
            if (runGoldTag != null && lostGoldNote != null && runGoldTag.parent != null)
            {
                if (strike != null)
                {
                    strike.RemoveFromHierarchy();
                    strike.style.left = 4;
                    strike.style.right = 4;
                    strike.style.width = StyleKeyword.Auto;
                    strike.style.top = Length.Percent(50);
                    runGoldTag.Add(strike);
                }
                var goldRow = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = 14, top = 70, right = 14,
                        flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center
                    }
                };
                runGoldTag.parent.Add(goldRow);
                runGoldTag.RemoveFromHierarchy();
                lostGoldNote.RemoveFromHierarchy();
                runGoldTag.style.position = Position.Relative;
                runGoldTag.style.left = StyleKeyword.Auto;
                runGoldTag.style.top = StyleKeyword.Auto;
                runGoldTag.style.marginRight = 12;
                lostGoldNote.style.position = Position.Relative;
                lostGoldNote.style.left = StyleKeyword.Auto;
                lostGoldNote.style.top = StyleKeyword.Auto;
                goldRow.Add(runGoldTag);
                goldRow.Add(lostGoldNote);
            }

            var equipLost = UiRoot.Q<UiLabel>(className: "equipment-found-in-dungeon-1-vars");
            if (equipLost != null)
            {
                equipLost.text = $"EQUIPMENT FOUND IN DUNGEON {act}";
                var panelLeft = equipLost.parent;
                if (panelLeft != null)
                {
                    var bulletCol = new VisualElement
                    {
                        style = { position = Position.Absolute, left = 16, top = 130, right = 16, flexDirection = FlexDirection.Column }
                    };
                    var bullets = new[]
                    {
                        equipLost,
                        UiRoot.Q<UiLabel>(className: "character-levels-and-sp-earned-here-vars"),
                        UiRoot.Q<UiLabel>(className: "relics-claimed-in-dungeon-1-vars"),
                        UiRoot.Q<UiLabel>(className: "consumables-materials-and-key-items-found-here-vars"),
                    };
                    panelLeft.Add(bulletCol);
                    foreach (var b in bullets)
                    {
                        if (b == null) continue;
                        b.RemoveFromHierarchy();
                        b.style.position = Position.Relative;
                        b.style.left = StyleKeyword.Auto;
                        b.style.top = StyleKeyword.Auto;
                        b.style.width = StyleKeyword.Auto;
                        b.style.marginBottom = 10;
                        b.style.whiteSpace = WhiteSpace.Normal;
                        bulletCol.Add(b);
                    }
                }
            }

            var committedTitle = UiRoot.Q<UiLabel>(className: "committed-progress-vars");
            if (committedTitle != null)
            {
                committedTitle.text = lost ? "Expedition ended" : (act > 1 ? $"Committed by Dungeons 1-{act - 1}" : "Committed progress");
                var hdrRow = committedTitle.parent;
                if (hdrRow != null && hdrRow.childCount > 0)
                {
                    hdrRow[0].style.marginRight = 10;
                    if (lost) FigmaUi.SetInstanceText(hdrRow[0], "text-0", "LOST");
                }
            }

            var clearedLbl = UiRoot.Q<UiLabel>(className: "_0-8-cleared-vars-2");
            if (clearedLbl != null)
            {
                string pips = string.Concat(Enumerable.Range(1, 8).Select(i => i < act ? "◆" : "◇"));
                clearedLbl.text = $"{pips}   {act - 1} / 8 CLEARED";
            }

            var nextNote = UiRoot.Q<UiLabel>(className: "dungeon-progress-stays-dungeon-1-is-your-next-dungeon-again-vars");
            if (nextNote != null)
            {
                int retries = Run?.RetriesLeft ?? 0;
                var snap = PersistenceCommitHandler.ActiveCommittedSnapshot;
                int checkpointStage = snap != null && snap.nextDungeonAct == act ? snap.NormalizedNextStage() : 1;
                string checkpointSuffix = checkpointStage > 1 ? $" (STAGE {checkpointStage})" : "";
                nextNote.text = lost
                    ? "NO RETRIES WERE LEFT. THE EXPEDITION IS LOST — START A NEW GAME."
                    : $"DUNGEON {act}{checkpointSuffix} IS YOUR NEXT DUNGEON AGAIN · {retries} {(retries == 1 ? "RETRY" : "RETRIES")} LEFT";
                nextNote.style.color = lost ? Palette.HpLow : Palette.Mist2;
            }

            int hub = Run != null ? Run.HubGold : 0;
            var hubTag = UiRoot.Q<VisualElement>(className: "currency-tag-hub");
            if (hubTag != null)
            {
                FigmaUi.SetInstanceText(hubTag, "text-0", $"{hub} GOLD");
                if (lost) hubTag.style.opacity = 0.45f;
            }
            if (lost)
            {
                var keptSummary = FigmaUi.FindLabel(UiRoot, "LEVELS, SKILLS, EQUIPMENT AND ITEMS FROM EARLIER CLEARS, PLUS OWNED RELICS.");
                if (keptSummary != null) keptSummary.text = "ALL RETRIES EXHAUSTED. THIS SAVE CANNOT BE CONTINUED.";
            }

            var menuInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-10");
            if (menuInst != null)
            {
                FigmaUi.SetInstanceText(menuInst, "text-1", "RETURN TO MAIN MENU");
                var btn = menuInst.Q<Button>() ?? menuInst;
                var item = menu.Add(btn, "RETURN TO MAIN MENU", () => Game.EndRun(), visual: on =>
                {
                    FigmaUi.SetInstanceText(menuInst, "text-0", on ? "▸" : "");
                    FigmaUi.ApplyDefaultFocusVisual(btn, on, false, true);
                });
                item.visual(true);
            }

            var footerNote = UiRoot.Q<UiLabel>(className: "from-the-main-menu-start-a-new-expedition-into-dungeon-1-or-load-a-save-vars");
            if (footerNote != null) footerNote.style.display = DisplayStyle.None;
        }
    }
}
