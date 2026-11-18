using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Presents RunState.PendingReward — migrated to Figma 21---Combat-Victory---Rewards.uxml (1280x720 UI Toolkit).
    public class RewardScreen : GameScreen
    {
        protected override void Build()
        {
            var r = Run?.PendingReward ?? new RewardOffer { title = "VICTORY", text = "SPOILS OF WAR", choices = new List<RewardOption>() };
            MountFigma("21---Combat-Victory---Rewards");

            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            var titleLbl = UiRoot.Q<UiLabel>(className: "victory-vars");
            if (titleLbl != null) titleLbl.text = (r.title ?? "VICTORY").ToUpperInvariant();

            var subLbl = UiRoot.Q<UiLabel>(className: "spoils-of-war-vars");
            if (subLbl != null) subLbl.text = (r.text ?? "SPOILS OF WAR").ToUpperInvariant();

            var goldDeltaLbl = UiRoot.Q<UiLabel>(className: "_24-run-gold-vars");
            if (goldDeltaLbl != null) goldDeltaLbl.text = $"◆ +{r.gold} RUN GOLD";

            var goldTotalLbl = UiRoot.Q<UiLabel>(className: "run-gold-120-144-vars");
            if (goldTotalLbl != null)
                goldTotalLbl.text = Run != null ? $"RUN GOLD {Run.Gold - r.gold} › {Run.Gold}" : $"RUN GOLD +{r.gold}";

            var prizeHdr = UiRoot.Q<UiLabel>(className: "choose-one-prize-vars");
            if (prizeHdr != null) prizeHdr.text = r.choices.Count > 0 ? "CHOOSE ONE PRIZE" : "NO PRIZE THIS TIME";

            var tipLbl = UiRoot.Q<UiLabel>(className: "or-take-only-the-gold-and-continue-vars");
            if (tipLbl != null) tipLbl.text = "";

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            // Prize cards in 21---Combat-Victory---Rewards.uxml
            string[] cardClasses = { "prize-draught-2-vars", "prize-tideglass-wand-vars", "prize-iron-scrap-3-vars" };
            for (int i = 0; i < cardClasses.Length; i++)
            {
                var card = UiRoot.Q<VisualElement>(className: cardClasses[i]);
                if (card == null) continue;
                if (i >= r.choices.Count)
                {
                    card.style.display = DisplayStyle.None;
                    continue;
                }

                int choiceIdx = i;
                var o = r.choices[i];
                var (label, detail) = RewardGenerator.Describe(o);
                if (o.kind == RewardKind.Upgrade)
                {
                    var u = party.FirstOrDefault(p => p.id == o.id || p.classDef?.id == o.id) ?? party[0];
                    detail = u.stats.Preview(Database.Upgrades[o.id].bonus);
                }

                card.Clear();
                card.style.justifyContent = Justify.SpaceBetween;

                var topGroup = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                var slot = FigmaUi.InstantiateTemplate("Item-Slot-Filled");
                if (slot != null)
                {
                    FigmaUi.SetInstanceText(slot, "text-0", PrizeSymbol(o));
                    slot.style.marginBottom = 8;
                    topGroup.Add(slot);
                }

                var nameEl = new UiLabel(label);
                nameEl.style.fontSize = 16;
                nameEl.style.color = Palette.Mist1;
                nameEl.style.whiteSpace = WhiteSpace.Normal;
                nameEl.style.marginBottom = 4;
                topGroup.Add(nameEl);

                var detEl = new UiLabel(detail.ToUpperInvariant());
                detEl.style.fontSize = 12;
                detEl.style.color = Palette.Mist2;
                detEl.style.whiteSpace = WhiteSpace.Normal;
                topGroup.Add(detEl);
                card.Add(topGroup);

                bool keep = o.kind == RewardKind.Relic || o.kind == RewardKind.Equipment || o.kind == RewardKind.Upgrade;
                var tag = FigmaUi.InstantiateTemplate(keep ? "Tag-Keep" : "Tag-Temp");
                if (tag != null)
                {
                    FigmaUi.SetInstanceText(tag, "text-0", keep ? "KEPT" : "RUN ONLY");
                    card.Add(tag);
                }

                var item = menu.Add(card, label, () => Claim(choiceIdx), tooltip: detail.ToUpperInvariant());
                item.visual = on =>
                {
                    card.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Palette.Ink2;
                    var border = on ? Palette.Gold2 : Palette.Ink5;
                    card.style.borderTopColor = border;
                    card.style.borderBottomColor = border;
                    card.style.borderLeftColor = border;
                    card.style.borderRightColor = border;
                };
                item.visual(false);
            }

            // Party panel
            string[] spriteClasses = { "sprite-knight", "sprite-rogue", "sprite-mage", "sprite-healer" };
            string[] nameClasses = { "knight-vars-6", "rogue-vars-5", "mage-vars-5", "healer-vars-6" };
            string[] lvClasses = { "lv-1-vars-8", "lv-1-vars-9", "lv-1-vars-10", "lv-1-vars-11" };
            string[] deltaClasses = { "_32-exp-vars", "_32-exp-vars-2", "_32-exp-vars-3", "_32-exp-vars-4" };
            string[] barTextClasses = { "_62-100-exp-vars", "_48-100-exp-vars", "_55-100-exp-vars", "_70-100-exp-vars" };
            var xpGauges = UiRoot.Query<VisualElement>(className: "gauge-xp").ToList();

            for (int i = 0; i < 4; i++)
            {
                var fig = UiRoot.Q<VisualElement>(className: spriteClasses[i]);
                var nLbl = UiRoot.Q<UiLabel>(className: nameClasses[i]);
                var lLbl = UiRoot.Q<UiLabel>(className: lvClasses[i]);
                var dLbl = UiRoot.Q<UiLabel>(className: deltaClasses[i]);
                var bLbl = UiRoot.Q<UiLabel>(className: barTextClasses[i]);
                var g = i < xpGauges.Count ? xpGauges[i] : null;

                if (i >= party.Count)
                {
                    if (fig != null && fig.parent != null) fig.parent.style.display = DisplayStyle.None;
                    if (nLbl != null) nLbl.style.display = DisplayStyle.None;
                    if (lLbl != null) lLbl.style.display = DisplayStyle.None;
                    if (dLbl != null) dLbl.style.display = DisplayStyle.None;
                    if (bLbl != null) bLbl.style.display = DisplayStyle.None;
                    if (g != null) g.style.display = DisplayStyle.None;
                    continue;
                }

                var u = party[i];
                if (fig != null && u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "rw_" + (u.classDef?.id ?? u.name)));
                if (nLbl != null) { nLbl.text = u.name; nLbl.style.color = u.Alive ? Palette.Mist1 : Palette.Mist3; }
                if (lLbl != null) lLbl.text = $"LV {u.level}";
                if (dLbl != null) { dLbl.text = u.Alive ? "STANDING" : "KO"; dLbl.style.color = u.Alive ? Palette.Ok : Palette.HpLow; }
                float hpRatio = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
                FigmaUi.SetGaugeFill(g, hpRatio, hpRatio < 0.25f ? Palette.HpLow : Palette.Hp);
                if (bLbl != null) bLbl.text = $"HP {u.hp} / {u.stats.maxHp}  ·  MP {u.mp} / {u.stats.maxMp}";
            }

            // Continue / Skip button
            var contInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-8");
            if (contInst != null)
            {
                string labelText = r.choices.Count > 0 ? "Skip prize" : "Continue to map";
                FigmaUi.SetInstanceText(contInst, "text-1", labelText);
                var contBtn = contInst.Q<Button>() ?? contInst;
                var item = menu.Add(contBtn, labelText, () => Claim(-1), tooltip: "TAKE ONLY THE GOLD AND CONTINUE");
                item.visual = on =>
                {
                    FigmaUi.SetInstanceText(contInst, "text-0", on ? "▸" : "");
                    contBtn.style.borderTopColor = on ? Palette.Mist1 : Palette.Gold2;
                    contBtn.style.borderBottomColor = on ? Palette.Mist1 : Palette.Gold2;
                    contBtn.style.borderLeftColor = on ? Palette.Mist1 : Palette.Gold2;
                    contBtn.style.borderRightColor = on ? Palette.Mist1 : Palette.Gold2;
                };
                item.visual(false);
            }

            menu.onFocus = s =>
            {
                if (tipLbl != null) tipLbl.text = s?.tooltip ?? "";
            };
        }

        static string PrizeSymbol(RewardOption o) => o.kind switch
        {
            RewardKind.Relic => "R",
            RewardKind.Item => "P",
            RewardKind.Equipment => "E",
            RewardKind.Upgrade => "U",
            _ => "+",
        };

        static void Claim(int choice)
        {
            int gainedGold = Run != null && Run.PendingReward != null ? Run.PendingReward.gold : 0;
            Run.ClaimReward(choice);
            if (choice >= 0) Sfx.Play(Sound.Success);
            else if (gainedGold > 0) Sfx.Play(Sound.Coin);
            if (Run.Won)
            {
                PersistenceCommitHandler.CommitDungeonClear(Run);
                SaveManager.AutoSave(out _);
                Game.Go(Run.Act >= 8 ? GameState.Victory : GameState.DungeonClear);
            }
            else
            {
                Game.Go(GameState.Map);
            }
        }
    }

    // Dungeon Clear — migrated to Figma 23---Dungeon-Clear.uxml (1280x720 UI Toolkit).
    public class DungeonClearScreen : GameScreen
    {
        protected override void Build()
        {
            Sfx.Play(Sound.VictoryStinger);
            MountFigma("23---Dungeon-Clear");
            var menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);

            int act = Run != null ? Run.Act : 1;
            var stats = Run != null ? Run.Stats : new RunStats();
            int earned = stats.goldEarned, total = Run != null ? Run.Gold : 0;

            var dLbl = UiRoot.Q<UiLabel>(className: "dungeon-1-of-8-vars-2");
            if (dLbl != null) dLbl.text = $"DUNGEON {act} OF 8";

            var mapLbl = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars-3");
            if (mapLbl != null) mapLbl.text = Run?.Map?.name ?? "Dungeon";

            var pipLbl = UiRoot.Q<UiLabel>(className: "node-vars-13");
            if (pipLbl != null) pipLbl.text = string.Concat(Enumerable.Range(1, 8).Select(i => i <= act ? "◆" : "◇"));

            var runTags = UiRoot.Query<VisualElement>(className: "currency-tag-run").ToList();
            if (runTags.Count > 0) FigmaUi.SetInstanceText(runTags[0], "text-0", $"{earned} RUN GOLD");

            var hubTags = UiRoot.Query<VisualElement>(className: "currency-tag-hub").ToList();
            if (hubTags.Count > 0) FigmaUi.SetInstanceText(hubTags[0], "text-0", $"{earned} HUB GOLD");

            var totalLbl = UiRoot.Q<UiLabel>(className: "_745-vars");
            if (totalLbl != null) totalLbl.text = total.ToString("#,##0");

            var encLbl = UiRoot.Q<UiLabel>(className: "_6-vars-3");
            if (encLbl != null) encLbl.text = stats.battlesWon.ToString();

            var eliteLbl = UiRoot.Q<UiLabel>(className: "_1-vars-9");
            if (eliteLbl != null) eliteLbl.text = stats.elitesWon.ToString();

            var chLbl = UiRoot.Q<UiLabel>(className: "_11-vars-2");
            if (chLbl != null) chLbl.text = stats.nodesCompleted.ToString();

            var relLbl = UiRoot.Q<UiLabel>(className: "_1-vars-10");
            if (relLbl != null) relLbl.text = (Run?.Relics.Count ?? 0).ToString();

            var nextLbl = UiRoot.Q<UiLabel>(className: "next-dungeon-2-of-8-unlocked-vars");
            if (nextLbl != null) nextLbl.text = $"NEXT: DUNGEON {Mathf.Min(8, act + 1)} OF 8 UNLOCKED";

            var keepTags = UiRoot.Query<VisualElement>(className: "tag-keep").ToList();
            if (keepTags.Count >= 2)
                FigmaUi.SetInstanceText(keepTags[1], "text-0", $"{Run?.Relics.Count ?? 0} RELICS STAY");

            var retInst = UiRoot.Q<VisualElement>(className: "button-primary-2__i-9");
            if (retInst != null)
            {
                FigmaUi.SetInstanceText(retInst, "text-1", "Return to Hub");
                var btn = retInst.Q<Button>() ?? retInst;
                var item = menu.Add(btn, "Return to Hub", () => Game.Go(GameState.Hub));
                item.visual = on =>
                {
                    FigmaUi.SetInstanceText(retInst, "text-0", on ? "▸" : "");
                    btn.style.borderTopColor = on ? Palette.Mist1 : Palette.Gold2;
                    btn.style.borderBottomColor = on ? Palette.Mist1 : Palette.Gold2;
                    btn.style.borderLeftColor = on ? Palette.Mist1 : Palette.Gold2;
                    btn.style.borderRightColor = on ? Palette.Mist1 : Palette.Gold2;
                };
                item.visual(false);
            }
        }
    }

    // Legacy shared result helpers kept public cleanly for any callers.
    public static class ResultArt
    {
        public static void Bricks(Transform root) { }
        public static void Ornament(Transform root, int cx, int y, int w, Color c) { }
        public static void Title(Transform root, string text, int cx, int y, Color c) { }
    }
}
