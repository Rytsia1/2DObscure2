using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Rest site — migrated to Figma 19---Rest---Campfire-Sanctuary.uxml (1280x720 UI Toolkit).
    public class RestScreen : GameScreen
    {
        UitkMenu menu;
        VisualElement actionHost;
        UiLabel feedbackLbl;
        readonly List<VisualElement> hpGauges = new List<VisualElement>();
        readonly List<VisualElement> mpGauges = new List<VisualElement>();
        readonly List<UiLabel> hpPctLabels = new List<UiLabel>();
        readonly List<UiLabel> mpPctLabels = new List<UiLabel>();
        readonly List<UiLabel> nameLabels = new List<UiLabel>();

        protected override void Build()
        {
            MountFigma("19---Rest---Campfire-Sanctuary");

            // Left panel hero vitals bindings
            string[] spriteClasses = { "sprite-knight", "sprite-rogue", "sprite-mage", "sprite-healer" };
            string[] nameClasses = { "knight-vars-5", "rogue-vars-4", "mage-vars-4", "healer-vars-5" };
            string[] hpValClasses = { "_55-vars-2", "_30-vars-2", "_80-vars-2", "_65-vars" };
            string[] mpValClasses = { "_70-vars", "_50-vars", "_20-vars-2", "_90-vars" };

            var allHpGauges = UiRoot.Query<VisualElement>(className: "gauge-hp").ToList();
            var lowHpGauge = UiRoot.Q<VisualElement>(className: "gauge-hp-low");
            // Ordered top-to-bottom: knight (hp[0]), rogue (lowHpGauge), mage (hp[1]), healer (hp[2])
            VisualElement[] hpOrder =
            {
                allHpGauges.Count > 0 ? allHpGauges[0] : null,
                lowHpGauge ?? (allHpGauges.Count > 3 ? allHpGauges[3] : null),
                allHpGauges.Count > 1 ? allHpGauges[1] : null,
                allHpGauges.Count > 2 ? allHpGauges[2] : null,
            };
            var mpOrder = UiRoot.Query<VisualElement>(className: "gauge-mp").ToList();

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < 4; i++)
            {
                var fig = UiRoot.Q<VisualElement>(className: spriteClasses[i]);
                var nameLbl = UiRoot.Q<UiLabel>(className: nameClasses[i]);
                var hpVal = UiRoot.Q<UiLabel>(className: hpValClasses[i]);
                var mpVal = UiRoot.Q<UiLabel>(className: mpValClasses[i]);
                var hpG = i < hpOrder.Length ? hpOrder[i] : null;
                var mpG = i < mpOrder.Count ? mpOrder[i] : null;

                if (i >= party.Count)
                {
                    if (fig != null && fig.parent != null) fig.parent.style.display = DisplayStyle.None;
                    if (nameLbl != null) nameLbl.style.display = DisplayStyle.None;
                    if (hpG != null) hpG.style.display = DisplayStyle.None;
                    if (mpG != null) mpG.style.display = DisplayStyle.None;
                    if (hpVal != null) hpVal.style.display = DisplayStyle.None;
                    if (mpVal != null) mpVal.style.display = DisplayStyle.None;
                    continue;
                }

                var u = party[i];
                if (fig != null && u.sprite != null && u.sprite.Length > 0)
                {
                    fig.style.width = 64;
                    fig.style.height = 80;
                    FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "rest_" + (u.classDef?.id ?? u.name)));
                }
                if (nameLbl != null) nameLabels.Add(nameLbl);
                if (hpG != null) hpGauges.Add(hpG);
                if (mpG != null) mpGauges.Add(mpG);
                if (hpVal != null) hpPctLabels.Add(hpVal);
                if (mpVal != null) mpPctLabels.Add(mpVal);
            }

            // Hide baked static action rows inside right panel
            foreach (var cls in new[] { "list-row__i-6", "list-row-2__i-18", "list-row-2__i-19", "list-row-2__i-20" })
            {
                var r = UiRoot.Q<VisualElement>(className: cls);
                if (r != null) r.style.display = DisplayStyle.None;
            }

            var screen = UiRoot.Q<VisualElement>(className: "_19-rest-campfire-sanctuary") ?? UiRoot;
            actionHost = new VisualElement { name = "CampActionHost" };
            actionHost.style.position = Position.Absolute;
            actionHost.style.left = 676;
            actionHost.style.top = 286;
            actionHost.style.width = 528;
            actionHost.style.height = 330;
            actionHost.style.flexDirection = FlexDirection.Column;
            screen.Add(actionHost);

            feedbackLbl = new UiLabel { name = "CampFeedback" };
            feedbackLbl.style.position = Position.Absolute;
            feedbackLbl.style.left = 660;
            feedbackLbl.style.top = 656;
            feedbackLbl.style.width = 560;
            feedbackLbl.style.fontSize = 14;
            feedbackLbl.style.color = Palette.Gold1;
            feedbackLbl.style.whiteSpace = WhiteSpace.Normal;
            screen.Add(feedbackLbl);

            ShowParty();
            ShowMain();
        }

        void ShowParty()
        {
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < party.Count && i < nameLabels.Count; i++)
            {
                var u = party[i];
                nameLabels[i].text = u.name;
                nameLabels[i].style.color = u.Alive ? Palette.Mist1 : Palette.HpLow;
                float hpRatio = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
                float mpRatio = (float)u.mp / Mathf.Max(1, u.stats.maxMp);
                if (i < hpGauges.Count) FigmaUi.SetGaugeFill(hpGauges[i], hpRatio, hpRatio < 0.25f ? Palette.HpLow : Palette.Hp);
                if (i < mpGauges.Count) FigmaUi.SetGaugeFill(mpGauges[i], mpRatio, Palette.Mp);
                if (i < hpPctLabels.Count) hpPctLabels[i].text = $"{Mathf.RoundToInt(hpRatio * 100)}%";
                if (i < mpPctLabels.Count) mpPctLabels[i].text = $"{Mathf.RoundToInt(mpRatio * 100)}%";
            }
        }

        void NewMenu()
        {
            if (menu != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(menu);
                else UnityEngine.Object.DestroyImmediate(menu);
            }
            actionHost.Clear();
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
        }

        void Option(string title, string sub, Action onClick)
        {
            var row = FigmaUi.InstantiateTemplate("List-Row");
            if (row == null) return;
            row.style.width = Length.Percent(100);
            row.style.marginBottom = 8;
            FigmaUi.SetInstanceText(row, "text-0", title);
            FigmaUi.SetInstanceText(row, "text-1", sub ?? "");
            FigmaUi.SetInstanceText(row, "text-2", "");
            actionHost.Add(row);

            var inner = row.Q<VisualElement>(className: "list-row-2") ?? row;
            if (inner.childCount > 0) inner[0].style.display = DisplayStyle.None;
            var item = menu.Add(row, title, onClick, tooltip: sub ?? "");
            item.visual = on =>
            {
                inner.style.backgroundColor = on ? Palette.Hex(0x2B2210) : Palette.Ink2;
                var border = on ? Palette.Gold2 : Palette.Ink5;
                inner.style.borderTopColor = border;
                inner.style.borderBottomColor = border;
                inner.style.borderLeftColor = border;
                inner.style.borderRightColor = border;
            };
            item.visual(false);
        }

        void ShowMain()
        {
            NewMenu();
            feedbackLbl.text = "";
            int total = Run != null ? Run.Party.Sum(RunState.RecoverAmount) : 0;
            Option($"RECOVER +{total} HP", $"HEAL {(int)(RunConfig.RestRecover * 100)}% OF MISSING HP AND MP ACROSS ALL HEROES", Recover);
            Option("IMPROVE A HERO", "HONOR ONE HERO WITH A PERMANENT ATTRIBUTE OR MASTERY UPGRADE", ShowImprove);
            Option("CAMP BOON (MODIFIER)", "PREPARE A TEMPORARY DUNGEON MODIFIER FOR THE LAYER", ShowCampModifiers);
            Option("DEVELOP SKILLS", "VIEW HERO SKILL TREES AND ALLOCATE SKILL POINTS", () => Game?.Go(GameState.Skills));
        }

        void ShowCampModifiers()
        {
            NewMenu();
            menu.onBack = ShowMain;
            string[] campIds = { "MOD-011", "MOD-012", "MOD-007" };
            foreach (var mid in campIds)
            {
                if (!Database.DungeonModifiers.TryGetValue(mid, out var def)) continue;
                string id = mid;
                string title = Run?.ModifierManager != null ? Run.ModifierManager.FormatOfferHeading(id) : def.displayName;
                Option(title.ToUpperInvariant(), def.description.ToUpperInvariant(), () =>
                {
                    Run.RestApplyModifier(id);
                    Done($"CAMP BOON GRANTED: {def.displayName.ToUpperInvariant()}.");
                });
            }
            Option("BACK", "RETURN TO CAMP ACTIONS", ShowMain);
        }

        void ShowImprove()
        {
            NewMenu();
            menu.onBack = ShowMain;
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            foreach (var u in party)
            {
                var unit = u;
                var up = RunState.GetUpgradeFor(unit);
                Option(unit.name.ToUpperInvariant(), $"{up.name.ToUpperInvariant()}: {unit.stats.Preview(up.bonus)}", () => Confirm(unit));
            }
            Option("BACK", "RETURN TO CAMP ACTIONS", ShowMain);
        }

        void Confirm(Unit u)
        {
            var up = RunState.GetUpgradeFor(u);
            string preview = $"{up.name.ToUpperInvariant()}: {u.stats.Preview(up.bonus)}";
            NewMenu();
            menu.onBack = ShowImprove;
            Option($"CONFIRM {u.name.ToUpperInvariant()}", preview, () =>
            {
                Run.RestImprove(u.id);
                Done($"{u.name.ToUpperInvariant()} LEARNS {up.name.ToUpperInvariant()}.");
            });
            Option("BACK", preview, ShowImprove);
        }

        void Recover()
        {
            Run.RestRecover();
            Done("THE PARTY RESTS BY THE HEARTH, STRENGTH RESTORED.");
        }

        void Done(string text)
        {
            Sfx.Play(Sound.RestCampfire);
            ShowParty();
            NewMenu();
            feedbackLbl.text = text;
            Option("CONTINUE EXPEDITION", "LEAVE THE CAMP", () => Game.Go(GameState.Map));
        }
    }
}
