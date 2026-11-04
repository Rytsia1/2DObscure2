using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Hero Talent Management & Milestone Progression Screen (640x360).
    // Features 5 Milestone tiers (Levels 3, 6, 9, 12, 15), 1 Shared + 2 Class-Specific draft offers,
    // active talent inspection, and full respec capabilities.
    public class TalentScreen : GameScreen
    {
        readonly GameState returnState;
        readonly List<Unit> party;
        int heroIndex = 0;

        public Unit CurrentHero => party != null && party.Count > 0 ? party[Math.Clamp(heroIndex, 0, party.Count - 1)] : null;

        Transform contentRoot;
        Label heroLabel;
        Label pointsLabel;
        Label feedbackLabel;

        readonly Dictionary<string, List<TalentDef>> cachedOffers = new Dictionary<string, List<TalentDef>>();
        string feedbackMessage = "";
        Color feedbackColor = Palette.Mist2;
        bool confirmRespec = false;
        string inspectedTalentId;

        public TalentScreen() : this(null, 0, null) { }

        public TalentScreen(IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
        {
            if (partyUnits != null)
                party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null)
                party = Game.I.Run.Party;
            else if (Game.I?.Roster != null)
                party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else
                party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            heroIndex = Math.Clamp(initialHero, 0, Math.Max(0, (party?.Count ?? 1) - 1));
            returnState = backTo ?? (Game.I?.Run != null ? GameState.Map : GameState.MainMenu);
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);

            // 1. Static Header: title, hero switcher, navigation shortcuts, talent points badge
            Ui.Label(Root, "HERO TALENTS", 12, 10, Palette.Gold2, Align.Left, 2, FontKind.Serif);

            Ui.Button(Root, "<", 146, 8, 20, () => SwitchHero(-1), 20, showCursor: false);
            heroLabel = Ui.Label(Root, "", 256, 14, Palette.Mist2, Align.Center);
            Ui.Button(Root, ">", 348, 8, 20, () => SwitchHero(1), 20, showCursor: false);

            var statusBtn = Ui.Button(Root, "STATUS", 374, 8, 62, () => Game?.Go(GameState.CharacterStatus), 20, showCursor: false);
            statusBtn.tooltip = "VIEW CHARACTER ATTRIBUTES & DERIVED STATS";
            var skillsBtn = Ui.Button(Root, "SKILLS", 440, 8, 62, () => Game?.Go(GameState.Skills), 20, showCursor: false);
            skillsBtn.tooltip = "VIEW & UPGRADE SKILL TREES AND SP";

            Ui.Image(Root, PixelArt.Badge(118, 20, Palette.Hex(0x1D1608), Palette.Gold3), 508, 8, Ui.Widget);
            pointsLabel = Ui.Label(Root, "", 567, 15, Palette.Gold1, Align.Center, order: Ui.Widget + 1);

            // Static Footer key hints
            feedbackLabel = Ui.Label(Root, "", 20, 332, Palette.Mist2);
            int hx = Ui.KeyHint(Root, 12, 344, "SELECT", "CLICK");
            Ui.KeyHint(Root, hx, 344, "BACK", "ESC");

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game?.Go(returnState);

            RebuildContent();
        }

        void SwitchHero(int delta)
        {
            if (party == null || party.Count <= 1) return;
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            confirmRespec = false;
            feedbackMessage = "";
            inspectedTalentId = null;
            RebuildContent();
        }

        void RebuildContent()
        {
            if (contentRoot != null)
                UnityEngine.Object.Destroy(contentRoot.gameObject);

            contentRoot = new GameObject("TalentContent").transform;
            contentRoot.SetParent(Root, false);

            var hero = CurrentHero;
            if (hero == null) return;

            heroLabel.Set($"{hero.name.ToUpper()} (LV {hero.level} {hero.classDef?.name?.ToUpper()})");
            pointsLabel.Set($"TALENT PTS: {hero.AvailableTalentSelections}");
            feedbackLabel.Set(feedbackMessage);
            feedbackLabel.Color = feedbackColor;

            // 2. Left Column: Milestones (Levels 3, 6, 9, 12, 15) and Respec
            Ui.Panel(contentRoot, 12, 34, 280, 292, Palette.Ink2, Palette.Gold3);
            Ui.Header(contentRoot, "TALENT MILESTONES", 26, 42, 252);
            Ui.Label(contentRoot, $"PROGRESS: {hero.selectedTalentIds.Count}/{hero.EarnedTalentSlots} CHOSEN (MAX 5)", 22, 58, Palette.WarmIvory);

            for (int i = 0; i < TalentSystem.MilestoneLevels.Length; i++)
            {
                int lvl = TalentSystem.MilestoneLevels[i];
                int y = 74 + (i * 38);

                if (i < hero.selectedTalentIds.Count)
                {
                    string tid = hero.selectedTalentIds[i];
                    var tDef = TalentSystem.GetTalent(tid);
                    string catTag = tDef?.category == TalentCategory.Shared ? "[SHARED]" : "[CLASS]";
                    string label = $"{catTag} {tDef?.name?.ToUpper() ?? tid.ToUpper()}";

                    var btn = Ui.Button(contentRoot, PixelFont.Fit(label, 250), 20, y, 264, () => InspectTalent(tid), 22, showCursor: false);
                    btn.tooltip = tDef?.description ?? "";
                }
                else if (i < hero.EarnedTalentSlots)
                {
                    var btn = Ui.Button(contentRoot, $"[LV {lvl}] * TALENT AVAILABLE *", 20, y, 264, () => FocusSelection(), 22, showCursor: false, style: ButtonStyle.Primary);
                    btn.tooltip = "TALENT POINT UNLOCKED! CHOOSE FROM THE OFFER ON THE RIGHT.";
                }
                else
                {
                    Ui.Panel(contentRoot, 20, y, 264, 22, Palette.Ink1, Palette.Ink3);
                    Ui.Label(contentRoot, $"[LV {lvl}] LOCKED (REQUIRES LVL {lvl})", 26, y + 5, Palette.Mist3);
                }
            }

            // Respec button
            string respecLabel = confirmRespec ? "CONFIRM RESPEC? (CLICK HERE)" : "RESPEC TALENTS";
            var respecBtn = Ui.Button(contentRoot, respecLabel, 20, 276, 264, OnRespecClicked, 24, showCursor: false, style: confirmRespec ? ButtonStyle.Primary : ButtonStyle.Slab);
            respecBtn.tooltip = "REFUNDS ALL TALENT CHOICES AND RESTORES POINTS FOR THIS HERO.";

            // 3. Right Column: Draft Offer or Detailed Inspector
            Ui.Panel(contentRoot, 298, 34, 330, 292);

            if (hero.AvailableTalentSelections > 0)
            {
                RenderDraftOffer(hero);
            }
            else
            {
                RenderInspectorOrOverview(hero);
            }
        }

        void RenderDraftOffer(Unit hero)
        {
            Ui.Header(contentRoot, "SELECT A TALENT (1 OF 3)", 312, 42, 302);
            Ui.Label(contentRoot, "CHOOSE 1 PERK TO EMPOWER THIS HERO", 312, 58, Palette.WarmIvory);

            var offer = GetDraftOffer(hero);
            if (offer == null || offer.Count == 0)
            {
                Ui.Label(contentRoot, "NO MORE TALENTS AVAILABLE IN CATALOG.", 312, 90, Palette.HpLow);
                return;
            }

            for (int i = 0; i < offer.Count; i++)
            {
                var t = offer[i];
                int cardY = 74 + (i * 70);

                Ui.Image(contentRoot, PixelArt.Box(314, 66, Palette.Ink2, Palette.Gold3), 306, cardY, Ui.Panels + 1);

                Ui.Label(contentRoot, t.name.ToUpper(), 314, cardY + 6, Palette.Gold2, Align.Left, 1, FontKind.Serif, Ui.Widget);

                string cat = t.category == TalentCategory.Shared ? "[SHARED TALENT]" : $"[CLASS: {hero.classDef?.name?.ToUpper()}]";
                Ui.Label(contentRoot, cat, 314, cardY + 20, Palette.Mist3, Align.Left, 1, FontKind.Interface, Ui.Widget);

                string wrappedDesc = PixelFont.Wrap(t.description, 230);
                Ui.Label(contentRoot, wrappedDesc, 314, cardY + 34, Palette.Mist1, Align.Left, 1, FontKind.Interface, Ui.Widget);

                string tid = t.id;
                var learnBtn = Ui.Button(contentRoot, "LEARN", 546, cardY + 20, 66, () => OnLearnTalent(tid), 26, showCursor: false, style: ButtonStyle.Primary);
                learnBtn.tooltip = $"LEARN {t.name.ToUpper()}";
            }
        }

        void RenderInspectorOrOverview(Unit hero)
        {
            if (!string.IsNullOrEmpty(inspectedTalentId) && Database.Talents.TryGetValue(inspectedTalentId, out var insT))
            {
                Ui.Header(contentRoot, "TALENT DETAILS", 312, 42, 302);
                Ui.Label(contentRoot, insT.name.ToUpper(), 312, 64, Palette.Gold2, Align.Left, 2, FontKind.Serif);

                string cat = insT.category == TalentCategory.Shared ? "CATEGORY: SHARED TALENT" : $"CATEGORY: CLASS-SPECIFIC ({hero.classDef?.name?.ToUpper()})";
                Ui.Label(contentRoot, cat, 312, 90, Palette.Gold3);

                Ui.Image(contentRoot, PixelArt.Box(314, 114, Palette.Ink2, Palette.Mist3), 306, 110, Ui.Panels + 1);
                Ui.Label(contentRoot, PixelFont.Wrap(insT.description, 298), 314, 120, Palette.Mist1, Align.Left, 1, FontKind.Interface, Ui.Widget);

                string prog = hero.EarnedTalentSlots < 5
                    ? $"NEXT TALENT MILESTONE AT LEVEL {TalentSystem.MilestoneLevels[hero.EarnedTalentSlots]}."
                    : "ALL 5 TALENT MILESTONES UNLOCKED (MAX LEVEL).";
                Ui.Label(contentRoot, prog, 312, 240, Palette.WarmIvory);
            }
            else
            {
                Ui.Header(contentRoot, "TALENT PROGRESSION", 312, 42, 302);
                Ui.Label(contentRoot, "ALL EARNED TALENT POINTS ARE CURRENTLY ALLOCATED.", 312, 66, Palette.Mist1);

                string nextInfo = hero.EarnedTalentSlots < 5
                    ? $"NEXT TALENT POINT UNLOCKS AT LEVEL {TalentSystem.MilestoneLevels[hero.EarnedTalentSlots]}."
                    : "ALL 5 TALENT MILESTONES COMPLETED (LEVEL 15 CAP)!";
                Ui.Label(contentRoot, nextInfo, 312, 88, Palette.Gold2);

                Ui.Label(contentRoot, "CLICK ANY LEARNED TALENT ON THE LEFT TO INSPECT DETAILS.", 312, 120, Palette.Mist2);
                Ui.Label(contentRoot, "OR CLICK 'RESPEC TALENTS' TO REDISTRIBUTE CHOICES.", 312, 138, Palette.Mist2);
            }
        }

        List<TalentDef> GetDraftOffer(Unit hero)
        {
            if (hero == null) return new List<TalentDef>();
            if (!cachedOffers.TryGetValue(hero.name, out var offer) || offer == null || offer.Count == 0)
            {
                offer = TalentSystem.GenerateDraftOffer(hero);
                cachedOffers[hero.name] = offer;
            }
            return offer;
        }

        void OnLearnTalent(string talentId)
        {
            var hero = CurrentHero;
            if (hero == null) return;

            if (hero.TrySelectTalent(talentId, out string error))
            {
                Sfx.Play(Sound.Success);
                cachedOffers.Remove(hero.name);
                var t = TalentSystem.GetTalent(talentId);
                feedbackMessage = $"LEARNED TALENT: {t?.name?.ToUpper() ?? talentId.ToUpper()}!";
                feedbackColor = Palette.Gold1;
                confirmRespec = false;
                inspectedTalentId = talentId;
                RebuildContent();
            }
            else
            {
                Sfx.Play(Sound.Deny);
                feedbackMessage = error?.ToUpper() ?? "CANNOT SELECT TALENT.";
                feedbackColor = Palette.HpLow;
                feedbackLabel.Set(feedbackMessage);
                feedbackLabel.Color = feedbackColor;
            }
        }

        void OnRespecClicked()
        {
            var hero = CurrentHero;
            if (hero == null) return;

            if (!confirmRespec)
            {
                confirmRespec = true;
                feedbackMessage = "CLICK 'CONFIRM RESPEC' TO RESET ALL TALENTS FOR THIS HERO.";
                feedbackColor = Palette.Gold3;
                RebuildContent();
                return;
            }

            confirmRespec = false;
            hero.RespecTalents();
            cachedOffers.Remove(hero.name);
            inspectedTalentId = null;
            Sfx.Play(Sound.Success);
            feedbackMessage = $"ALL TALENTS RESET FOR {hero.name.ToUpper()}. POINTS RESTORED.";
            feedbackColor = Palette.Gold1;
            RebuildContent();
        }

        void InspectTalent(string talentId)
        {
            inspectedTalentId = talentId;
            confirmRespec = false;
            RebuildContent();
        }

        void FocusSelection()
        {
            inspectedTalentId = null;
            confirmRespec = false;
            RebuildContent();
        }
    }
}

