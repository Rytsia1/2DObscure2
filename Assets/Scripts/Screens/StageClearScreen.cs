using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Shown after claiming rewards from a Stage Mini-Boss (e.g., Dungeon 1 Stage 1 or Stage 2).
    // Lets the party either continue straight into the next stage (keeping all modifiers & spoils)
    // or safely extract to the Hub with a Stage Checkpoint (securing progress & spoils 1:1, but clearing modifiers).
    public class StageClearScreen : GameScreen
    {
        protected override void Build()
        {
            Sfx.Play(Sound.VictoryStinger);
            MountFigma("23---Dungeon-Clear");
            var screen = UiRoot.Q<VisualElement>(className: "_23-dungeon-clear") ?? UiRoot;

            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => { }; // Explicit choice required; Back never triggers a destructive action.

            int act = Run != null ? Run.Act : 1;
            int totalStages = Math.Max(2, Run != null ? Run.TotalStages : MapGenerator.GetStageCount(act));
            int nextStage = Math.Clamp(Run != null ? Run.Stage : 2, 2, totalStages);
            int clearedStage = Math.Max(1, nextStage - 1);

            var plan = MapGenerator.GetDungeonPlan(act);
            string dungeonName = Run?.Map?.name ?? plan.name ?? $"Dungeon {act}";
            string nextStageName = Run?.Map?.stageName ?? plan.GetStage(nextStage)?.stageName ?? $"Stage {nextStage}";

            int spoils = Run != null ? Run.Gold : 0;
            int hubGold = Run != null ? Run.HubGold : 0;
            int modCount = Run?.ModifierManager?.Count ?? 0;
            int buffCount = Run?.BattleBuffs?.Count ?? 0;
            int partyCount = Run?.Party?.Count ?? 4;
            int standing = Run?.Party != null ? Run.Party.Count(u => u.Alive) : partyCount;
            int relics = Run?.Relics?.Count ?? 0;

            static void CenterAcross(UiLabel lbl)
            {
                if (lbl == null) return;
                lbl.style.left = 0;
                lbl.style.right = 0;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            var dLbl = UiRoot.Q<UiLabel>(className: "dungeon-1-of-8-vars-2");
            if (dLbl != null) { dLbl.text = $"DUNGEON {act} OF 8 · STAGE {clearedStage} OF {totalStages}"; CenterAcross(dLbl); }

            var titleLbl = UiRoot.Q<UiLabel>(className: "dungeon-clear-vars");
            if (titleLbl != null) { titleLbl.text = $"STAGE {clearedStage} CLEARED"; CenterAcross(titleLbl); }

            var mapLbl = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-3");
            if (mapLbl != null) { mapLbl.text = $"{dungeonName} · Next: {nextStageName}"; CenterAcross(mapLbl); }

            var pipLbl = UiRoot.Q<UiLabel>(className: "node-vars-13");
            if (pipLbl != null)
            {
                pipLbl.text = string.Concat(Enumerable.Range(1, totalStages).Select(i => i <= clearedStage ? "◆" : "◇"));
                CenterAcross(pipLbl);
            }

            // Replace baked single-path gold-transfer elements inside panel-gold__i-17 with a two-option comparison panel.
            HideGoldTransferElements();
            BuildChoiceComparisonPanel(screen, nextStage, totalStages, spoils, hubGold, modCount, buffCount);

            // Repurpose the 4 bottom stat cards (190..1090, top 446).
            SetStatCard("encounters-vars", "_6-vars-3", "cleared-vars",
                "SPOILS", spoils.ToString("#,##0"), "KEEP OR BANK 1:1", Palette.Gold1);
            SetStatCard("elites-vars", "_1-vars-9", "vanquished-vars",
                "MODIFIERS", modCount.ToString(), modCount > 0 ? "LOST IF RETURNING" : "NONE ACTIVE",
                modCount > 0 ? Palette.Ember1 : Palette.Mist2);
            SetStatCard("chambers-vars", "_11-vars-2", "visited-vars",
                "PARTY", $"{standing}/{partyCount}", standing == partyCount ? "ALL STANDING" : $"{partyCount - standing} FALLEN",
                standing < partyCount ? Palette.HpLow : Palette.Mist2);
            SetStatCard("relics-vars", "_1-vars-10", "owned-kept-vars",
                "RELICS", relics.ToString(), "OWNED · ALWAYS KEPT", Palette.Mist2);

            DungeonClearScreen.HideDungeonClearReminders(UiRoot);

            var nextNote = UiRoot.Q<UiLabel>(className: "next-dungeon-2-of-8-unlocked-vars");
            if (nextNote != null)
            {
                nextNote.text = "HUB SHOP RESTOCKS ONLY AFTER FINAL BOSS CLEAR.";
                nextNote.style.left = 724;
                nextNote.style.top = 602;
                nextNote.style.width = 366;
                nextNote.style.fontSize = 12;
                nextNote.style.color = Palette.Mist3;
                nextNote.style.whiteSpace = WhiteSpace.Normal;
            }

            // Primary action (default focus): Continue straight to next stage map.
            var contInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-9");
            UitkMenu.Item continueItem = null;
            if (contInst != null)
            {
                contInst.style.width = 276;
                FigmaUi.SetInstanceText(contInst, "text-1", $"Continue to Stage {nextStage}");
                var btn = contInst.Q<Button>() ?? contInst;
                continueItem = menu.Add(btn, $"Continue to Stage {nextStage}", () => Game?.Go(GameState.Map), visual: on =>
                {
                    FigmaUi.SetInstanceText(contInst, "text-0", on ? "▸" : "");
                    FigmaUi.ApplyDefaultFocusVisual(btn, on, false, true);
                });
            }

            // Secondary action: Safe extraction to Hub with Stage Checkpoint & modifier-loss warning.
            var hubBtn = FigmaUi.CreateButton("Return to Hub", "button-slab", 220f);
            hubBtn.style.position = Position.Absolute;
            hubBtn.style.left = 484;
            hubBtn.style.top = 590;
            screen.Add(hubBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(hubBtn, "Return to Hub", () => OpenReturnToHubConfirm(act, nextStage, totalStages, spoils, modCount, buffCount)), hubBtn);

            if (continueItem != null)
                menu.Focus(continueItem, false, true);
        }

        void HideGoldTransferElements()
        {
            FigmaUi.HideByClass(UiRoot,
                "section-header__i-26",
                "rate-1-1-vars",
                "run-gold-cleared-vars",
                "run-gold-now-0-vars",
                "_1-1-vars-2",
                "hub-gold-gained-vars",
                "committed-kept-if-a-later-run-fails-vars",
                "node-vars-14",
                "new-hub-gold-total-vars",
                "_745-vars");

            foreach (var cls in new[] { "currency-tag-run", "currency-tag-hub" })
            {
                foreach (var el in UiRoot.Query<VisualElement>(className: cls).ToList())
                {
                    var host = el.GetFirstAncestorOfType<TemplateContainer>() ?? el;
                    host.style.display = DisplayStyle.None;
                }
            }
        }

        void BuildChoiceComparisonPanel(VisualElement screen, int nextStage, int totalStages, int spoils, int hubGold, int modCount, int buffCount)
        {
            var container = new VisualElement
            {
                name = "StageClearChoicePanel",
                style =
                {
                    position = Position.Absolute,
                    left = 206, top = 232, width = 868, height = 176,
                    flexDirection = FlexDirection.Column,
                    overflow = Overflow.Hidden
                }
            };

            var topBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = UnityEngine.UIElements.Align.Center,
                    marginBottom = 8,
                    paddingBottom = 6,
                    borderBottomWidth = 1,
                    borderBottomColor = Palette.Ink4
                }
            };
            topBar.Add(new UiLabel("OPTIONAL EXTRACTION CHECKPOINT")
            {
                style = { fontSize = 14, letterSpacing = 0.56f, color = Palette.Gold2 }
            });
            topBar.Add(new UiLabel($"STAGE {nextStage} OF {totalStages} READY · 0 RETRIES TO EXTRACT")
            {
                style = { fontSize = 12, letterSpacing = 0.48f, color = Palette.Mist2 }
            });
            container.Add(topBar);

            var cols = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexGrow = 1, alignItems = UnityEngine.UIElements.Align.Stretch }
            };

            string modLabel = modCount == 1 ? "1 ACTIVE MODIFIER" : $"{modCount} ACTIVE MODIFIERS";
            string buffSuffix = buffCount > 0 ? $" & {buffCount} EVENT {(buffCount == 1 ? "EFFECT" : "EFFECTS")}" : "";

            // Left column: Continue
            var contCol = CreateOptionCard($"CONTINUE TO STAGE {nextStage}", Palette.Gold2, Palette.Ink1);
            contCol.style.marginRight = 12;
            contCol.Add(MakeBullet($"KEEP {modLabel}{buffSuffix}.", Palette.Ok));
            contCol.Add(MakeBullet($"CARRY {spoils:#,##0} SPOILS FOR DUNGEON SHOPS.", Palette.Mist1));
            contCol.Add(MakeBullet("PROGRESS STAYS UNCOMMITTED UNTIL HUB OR CLEAR.", Palette.Mist3));
            cols.Add(contCol);

            // Right column: Return to Hub
            var hubCol = CreateOptionCard($"RETURN TO HUB (STAGE {nextStage} CHECKPOINT)", Palette.Ember1, Palette.Ink1);
            hubCol.Add(MakeBullet($"COMMIT EXP, LOOT & BANK +{spoils:#,##0} GOLD ({hubGold + spoils:#,##0} TOTAL).", Palette.Ok));
            hubCol.Add(MakeBullet($"RESUME AT STAGE {nextStage} LATER · 0 RETRIES SPENT.", Palette.Mist1));
            hubCol.Add(MakeBullet(
                modCount > 0 || buffCount > 0
                    ? $"WARNING: ALL {modLabel}{buffSuffix} WILL BE LOST!"
                    : "RE-ENTERING ALWAYS STARTS WITH 0 MODIFIERS & 0 SPOILS.",
                modCount > 0 || buffCount > 0 ? Palette.HpLow : Palette.Ember1));
            cols.Add(hubCol);

            container.Add(cols);
            screen.Add(container);
        }

        static VisualElement CreateOptionCard(string heading, Color borderCol, Color bgCol)
        {
            var box = new VisualElement
            {
                style =
                {
                    flexGrow = 1, flexBasis = 0, minWidth = 0,
                    paddingLeft = 12, paddingRight = 12, paddingTop = 8, paddingBottom = 8,
                    backgroundColor = bgCol,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = borderCol, borderBottomColor = borderCol,
                    borderLeftColor = borderCol, borderRightColor = borderCol,
                    flexDirection = FlexDirection.Column,
                    overflow = Overflow.Hidden
                }
            };
            box.Add(new UiLabel(heading)
            {
                style = { fontSize = 13, letterSpacing = 0.52f, color = borderCol, marginBottom = 6 }
            });
            return box;
        }

        static UiLabel MakeBullet(string text, Color color) => new UiLabel($"▸ {text}")
        {
            style = { fontSize = 12, color = color, whiteSpace = WhiteSpace.Normal, marginBottom = 4 }
        };

        void SetStatCard(string titleClass, string valueClass, string subClass, string title, string value, string sub, Color subColor)
        {
            var t = UiRoot.Q<UiLabel>(className: titleClass);
            if (t != null) t.text = title;
            var v = UiRoot.Q<UiLabel>(className: valueClass);
            if (v != null) v.text = value;
            var s = UiRoot.Q<UiLabel>(className: subClass);
            if (s != null) { s.text = sub; s.style.color = subColor; }
        }

        void OpenReturnToHubConfirm(int act, int nextStage, int totalStages, int spoils, int modCount, int buffCount)
        {
            string modLine = modCount > 0 || buffCount > 0
                ? $"LOST ON EXIT: ALL {modCount} DUNGEON MODIFIERS" + (buffCount > 0 ? $" AND {buffCount} EVENT EFFECTS" : "") + " (PER GDD RULES)."
                : "RE-ENTERING STARTS WITH 0 SPOILS AND 0 MODIFIERS.";

            string body =
                $"RETURN TO GEISTWALD CITY WITH A STAGE {nextStage} OF {totalStages} CHECKPOINT FOR DUNGEON {act}?\n\n" +
                $"KEPT & COMMITTED: EXP, LEVELS, EQUIPMENT, RELICS, ITEMS, AND +{spoils:#,##0} GOLD (1:1 SPOILS).\n" +
                $"COST: 0 RETRIES SPENT. HUB SHOP DOES NOT RESTOCK YET.\n\n" +
                modLine;

            FigmaUi.ShowConfirmationModal(
                UiRoot,
                "RETURN TO HUB?",
                body,
                "RETURN TO HUB",
                () =>
                {
                    if (Run != null && Run.CanReturnToHubAtStageCheckpoint)
                        Run.ReturnToHubAtStageCheckpoint();
                    Game?.Go(GameState.Hub);
                },
                cancelText: "STAY",
                destructive: modCount > 0 || buffCount > 0);
        }
    }
}
