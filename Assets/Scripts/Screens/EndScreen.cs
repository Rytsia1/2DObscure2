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
            int total = Run != null ? Run.Gold : 0;

            var dLbl = UiRoot.Q<UiLabel>(className: "dungeon-1-of-8-vars-2");
            if (dLbl != null) dLbl.text = "EXPEDITION COMPLETE";

            var titleLbl = UiRoot.Q<UiLabel>(className: "dungeon-clear-vars");
            if (titleLbl != null) titleLbl.text = "THE ABYSS IS SEALED";

            var mapLbl = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-3");
            if (mapLbl != null) mapLbl.text = Run?.Map?.name ?? "Abyss";

            var pipLbl = UiRoot.Q<UiLabel>(className: "node-vars-13");
            if (pipLbl != null) pipLbl.text = "◆◆◆◆◆◆◆◆";

            var runTags = UiRoot.Query<VisualElement>(className: "currency-tag-run").ToList();
            if (runTags.Count > 0) FigmaUi.SetInstanceText(runTags[0], "text-0", $"{s.goldEarned} RUN GOLD");

            var hubTags = UiRoot.Query<VisualElement>(className: "currency-tag-hub").ToList();
            if (hubTags.Count > 0) FigmaUi.SetInstanceText(hubTags[0], "text-0", $"{s.goldEarned} HUB GOLD");

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

            var keepTags = UiRoot.Query<VisualElement>(className: "tag-keep").ToList();
            if (keepTags.Count >= 2)
                FigmaUi.SetInstanceText(keepTags[1], "text-0", $"{Run?.Relics.Count ?? 0} RELICS STAY");

            var newRunInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-9");
            if (newRunInst != null)
            {
                FigmaUi.SetInstanceText(newRunInst, "text-1", "NEW RUN");
                var btn = newRunInst.Q<Button>() ?? newRunInst;
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(btn, "NEW RUN", () => Game.StartRun()), btn);
            }

            // Add secondary TITLE button next to NEW RUN
            var titleBtn = FigmaUi.CreateButton("TITLE", "button-slab", 180);
            titleBtn.style.position = Position.Absolute;
            titleBtn.style.left = 850;
            titleBtn.style.top = 590;
            (UiRoot.Q<VisualElement>(className: "_23-dungeon-clear") ?? UiRoot).Add(titleBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(titleBtn, "TITLE", () => Game.EndRun()), titleBtn);
        }

        void BuildDefeat()
        {
            Sfx.Play(Sound.DefeatStinger);
            MountFigma("24---Run-Failure---Defeat");
            FigmaUi.FindLabel(UiRoot, "DEFEAT")?.AddToClassList("uitk-font-display");
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            int act = Run != null ? Run.Act : 1;
            int floor = Run != null ? Run.Floor : 1;
            var s = Run != null ? Run.Stats : new RunStats();

            var subLbl = UiRoot.Q<UiLabel>(className: "fell-in-dungeon-1-of-8-node-4-progress-from-earlier-clears-is-safe-vars");
            if (subLbl != null)
                subLbl.text = $"FELL IN DUNGEON {act} OF 8 · NODE {floor} · PROGRESS FROM EARLIER CLEARS IS SAFE";

            var gainedTitle = UiRoot.Q<UiLabel>(className: "gained-in-dungeon-1-vars");
            if (gainedTitle != null) gainedTitle.text = $"Gained in Dungeon {act}";

            var runGoldTag = UiRoot.Q<VisualElement>(className: "currency-tag-run__i");
            if (runGoldTag != null) FigmaUi.SetInstanceText(runGoldTag, "text-0", $"{s.goldEarned} RUN GOLD");

            var lostGoldNote = UiRoot.Q<UiLabel>(className: "run-gold-held-in-dungeon-1-vars");
            if (lostGoldNote != null) lostGoldNote.text = $"RUN GOLD HELD IN DUNGEON {act}.";

            var equipLost = UiRoot.Q<UiLabel>(className: "equipment-found-in-dungeon-1-vars");
            if (equipLost != null) equipLost.text = $"EQUIPMENT FOUND IN DUNGEON {act}";

            var committedTitle = UiRoot.Q<UiLabel>(className: "committed-progress-vars");
            if (committedTitle != null)
                committedTitle.text = act > 1 ? $"Committed by Dungeons 1-{act - 1}" : "Committed progress";

            var clearedLbl = UiRoot.Q<UiLabel>(className: "_0-8-cleared-vars-2");
            if (clearedLbl != null)
            {
                string pips = string.Concat(Enumerable.Range(1, 8).Select(i => i < act ? "◆" : "◇"));
                clearedLbl.text = $"{pips}   {act - 1} / 8 CLEARED";
            }

            var nextNote = UiRoot.Q<UiLabel>(className: "dungeon-progress-stays-dungeon-1-is-your-next-dungeon-again-vars");
            if (nextNote != null)
                nextNote.text = $"DUNGEON PROGRESS STAYS. DUNGEON {act} IS YOUR NEXT DUNGEON AGAIN.";

            int hub = PersistenceCommitHandler.ActiveCommittedSnapshot?.hubGold ?? (Run != null ? Run.Gold : 0);
            var hubTag = UiRoot.Q<VisualElement>(className: "currency-tag-hub");
            if (hubTag != null) FigmaUi.SetInstanceText(hubTag, "text-0", $"{hub} HUB GOLD");

            var menuInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-10");
            if (menuInst != null)
            {
                FigmaUi.SetInstanceText(menuInst, "text-1", "Return to Main Menu");
                var btn = menuInst.Q<Button>() ?? menuInst;
                var item = menu.Add(btn, "Return to Main Menu", () => Game.EndRun());
                item.visual = on =>
                {
                    FigmaUi.SetInstanceText(menuInst, "text-0", on ? "▸" : "");
                    btn.style.borderTopColor = on ? Palette.Mist1 : Palette.Gold2;
                    btn.style.borderBottomColor = on ? Palette.Mist1 : Palette.Gold2;
                    btn.style.borderLeftColor = on ? Palette.Mist1 : Palette.Gold2;
                    btn.style.borderRightColor = on ? Palette.Mist1 : Palette.Gold2;
                };
                item.visual(false);
            }

            var footerNote = UiRoot.Q<UiLabel>(className: "from-the-main-menu-start-a-new-expedition-into-dungeon-1-or-load-a-save-vars");
            if (footerNote != null)
                footerNote.text = $"FROM THE MAIN MENU, START A NEW EXPEDITION INTO DUNGEON {act} OR LOAD A SAVE.";
        }
    }
}
