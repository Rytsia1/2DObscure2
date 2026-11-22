using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Talents & Talent Respec — migrated to Figma 17---Talent-Selection.uxml (1280x720 UI Toolkit).
    public class TalentScreen : GameScreen
    {
        readonly GameState returnState;
        readonly List<Unit> party;
        int heroIndex;

        public Unit CurrentHero => party != null && party.Count > 0 ? party[Math.Clamp(heroIndex, 0, party.Count - 1)] : null;

        UitkMenu menu;
        UitkMenu dialogMenu;
        int filter; // 0 All, 1 Shared, 2 Class
        string selectedId;
        bool respecOpen;
        int respecTarget;
        string feedback;
        bool feedbackBad;
        int focusIndex;
        int dialogFocusIndex = -1;
        float savedScrollY;
        ScrollView poolScroll;
        bool rebuilding;
        readonly Dictionary<UitkMenu.Item, int> portraitItems = new Dictionary<UitkMenu.Item, int>();

        public TalentScreen() : this(null, 0, null) { }

        public TalentScreen(IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
        {
            if (partyUnits != null) party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null) party = Game.I.Run.Party;
            else if (Game.I?.Roster != null) party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            heroIndex = Math.Clamp(partyUnits == null && initialHero == 0 ? PartyScreen.SelectedIndex : initialHero, 0, Math.Max(0, (party?.Count ?? 1) - 1));
            returnState = backTo ?? ReturnTarget();
        }

        static InventoryRuntime Bag => Game.I?.Run?.Inventory;
        static int Scrolls => Bag == null ? 0 : Bag.GetConsumableCount("CON_SCROLL_OF_EMPTINESS") + Bag.GetConsumableCount("scroll_of_emptiness");

        protected override void Build()
        {
            MountFigma("17---Talent-Selection");
            Rebuild();
        }

        List<TalentDef> Pool(Unit hero)
        {
            string cid = hero.classDef?.id ?? "";
            return TalentSystem.AllTalents.Values.Where(t => t.IsCompatibleWithClass(cid) && t.isDesignFinalized)
                .OrderBy(t => hero.HasTalent(t.id)).ThenBy(t => t.category).ThenBy(t => t.name).ToList();
        }

        void SwitchHero(int delta)
        {
            if (party == null || party.Count == 0 || respecOpen) return;
            bool wasPortraitFocused = menu != null && menu.Current != null && portraitItems.ContainsKey(menu.Current);
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            if (wasPortraitFocused)
                focusIndex = FindPortraitFocusIndex(heroIndex, focusIndex);
            selectedId = null;
            feedback = null;
            Rebuild();
        }

        int FindPortraitFocusIndex(int targetHero, int fallback)
        {
            foreach (var kv in portraitItems)
                if (kv.Value == targetHero) return menu.items.IndexOf(kv.Key);
            return fallback;
        }

        string ClassName(Unit hero) => hero.classDef?.name ?? "Class";

        void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                if (poolScroll != null) savedScrollY = poolScroll.scrollOffset.y;
                UiRoot = FigmaUi.Mount(Root.gameObject, "17---Talent-Selection");
                FigmaUi.DestroyMenu(ref dialogMenu);
                FigmaUi.DestroyMenu(ref menu);
                portraitItems.Clear();
                menu = Root.gameObject.AddComponent<UitkMenu>();
                menu.AttachRoot(UiRoot);
                menu.onBack = () => { if (respecOpen) { respecOpen = false; Rebuild(); } else Game?.Back(returnState); };
                menu.onFocus = s =>
                {
                    if (rebuilding || s == null) return;
                    focusIndex = menu.items.IndexOf(s);
                };
                menu.onPrevPage = () => SwitchHero(-1);
                menu.onNextPage = () => SwitchHero(1);

                var hero = CurrentHero;
                if (hero == null) return;
                var pool = Pool(hero);
                if (selectedId == null || pool.All(t => t.id != selectedId))
                    selectedId = pool.FirstOrDefault(t => !hero.HasTalent(t.id))?.id ?? pool.FirstOrDefault()?.id;

                BuildHeader(hero);
                var screen = UiRoot.Q<VisualElement>(className: "_17-talent-selection") ?? UiRoot;
                var tv = UiRoot.Q<TabView>(); if (tv != null) tv.style.display = DisplayStyle.None;

                // Strip baked static milestones, cards & detail items while keeping backing panels
                var pTop = UiRoot.Q<VisualElement>(className: "panel-default__i-28");
                var pGrid = UiRoot.Q<VisualElement>(className: "panel-default__i-29");
                var pDetail = UiRoot.Q<VisualElement>(className: "panel-gold__i-13");
                if (pTop != null)
                {
                    for (int i = screen.IndexOf(pTop) + 1; i < screen.childCount; i++)
                    {
                        var ch = screen[i];
                        if (ch == pGrid || ch == pDetail) continue;
                        ch.style.display = DisplayStyle.None;
                    }
                }

                BuildMilestones(screen, hero);
                BuildPool(screen, hero, pool);
                BuildDetail(screen, hero);
                if (respecOpen) BuildRespecDialog();

                if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
                if (dialogMenu != null && dialogMenu.items.Count > 0)
                {
                    int target = dialogFocusIndex >= 0
                        ? Mathf.Clamp(dialogFocusIndex, 0, dialogMenu.items.Count - 1)
                        : (Scrolls > 0 ? dialogMenu.items.Count - 1 : Math.Max(0, dialogMenu.items.Count - 2));
                    dialogMenu.Focus(target, false, true);
                }
            }
            finally { rebuilding = false; }
        }

        void BuildHeader(Unit hero)
        {
            var backBtn = FigmaUi.FindButton(UiRoot, "Back");
            if (backBtn != null) FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", () => Game?.Back(returnState)));

            string[] frames = { "frame-vars-27", "frame-vars-28", "frame-vars-29", "frame-vars-30" };
            VisualElement switcherRow = null;
            for (int i = 0; i < frames.Length; i++)
            {
                var fr = UiRoot.Q<VisualElement>(className: frames[i]);
                if (fr == null) continue;
                switcherRow ??= fr.parent;
                if (i >= party.Count) { fr.style.display = DisplayStyle.None; continue; }
                fr.AddToClassList("uitk-portrait-btn");
                fr.style.marginRight = 6;
                int idx = i; var u = party[i]; bool on = i == heroIndex;
                if (fr.childCount > 0 && u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fr[0], FigmaUi.GetSpriteTexture(u.sprite, "tal_hdr_" + (u.classDef?.id ?? u.name)));
                var it = menu.Add(fr, u.name, () => { if (heroIndex != idx) { heroIndex = idx; selectedId = null; feedback = null; Rebuild(); } }, selected: on, onAdjust: d => SwitchHero(d));
                portraitItems[it] = idx;
                it.visual = f =>
                {
                    fr.EnableInClassList("uitk-focus", f);
                    fr.EnableInClassList("uitk-selected", on);
                    var b = f ? Palette.Gold1 : on ? Palette.Gold2 : Palette.Ink5;
                    fr.style.borderTopColor = b; fr.style.borderBottomColor = b; fr.style.borderLeftColor = b; fr.style.borderRightColor = b;
                };
                it.visual(false);
            }
            if (switcherRow != null)
            {
                var qCap = FigmaUi.FindLabel(switcherRow, "Q")?.parent;
                var eCap = FigmaUi.FindLabel(switcherRow, "E")?.parent;
                if (party.Count > 1)
                {
                    if (qCap != null && qCap != switcherRow) FigmaUi.StyleKeyButton(qCap, () => SwitchHero(-1), 6f);
                    if (eCap != null && eCap != switcherRow) FigmaUi.StyleKeyButton(eCap, () => SwitchHero(1), 0f);
                }
                else
                {
                    if (qCap != null && qCap != switcherRow) qCap.style.display = DisplayStyle.None;
                    if (eCap != null && eCap != switcherRow) eCap.style.display = DisplayStyle.None;
                }
            }

            var heroLbl = new UiLabel($"{hero.name} · {ClassName(hero)} · Lv {hero.level}")
            {
                style = { position = Position.Absolute, left = 460, top = 26, fontSize = 16, color = Palette.Mist1, maxWidth = 340, overflow = Overflow.Hidden }
            };
            UiRoot.Add(heroLbl);

            var scrollTag = FigmaUi.FindInstanceWithText(UiRoot, "text-0", "SCROLL OF EMPTINESS × 1");
            if (scrollTag != null)
            {
                scrollTag.AddToClassList("uitk-tag-btn");
                FigmaUi.SetInstanceText(scrollTag, "text-0", $"RESPEC · SCROLL × {Scrolls}");
                FigmaUi.AnchorRight(scrollTag, 24f);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(scrollTag, "Scroll of Emptiness", () => { respecOpen = true; respecTarget = heroIndex; dialogFocusIndex = -1; Rebuild(); }));
            }
        }

        void BuildMilestones(VisualElement screen, Unit hero)
        {
            var strip = new VisualElement { style = { position = Position.Absolute, left = 44, top = 80, width = 1180, height = 72, flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
            strip.Add(new UiLabel("TALENT SLOTS") { style = { fontSize = 14, color = Palette.Mist2, width = 176 } });
            int chosen = hero.selectedTalentIds.Count;
            for (int i = 0; i < TalentSystem.MilestoneLevels.Length; i++)
            {
                int lv = TalentSystem.MilestoneLevels[i];
                bool done = i < chosen, now = !done && i < hero.EarnedTalentSlots;
                var cell = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, width = 190 } };
                var node = FigmaUi.InstantiateTemplate(done || now ? "Map-Node-Current" : "Map-Node-Locked") ?? new VisualElement();
                node.style.width = 48; node.style.height = 48; node.style.marginRight = 10;
                FigmaUi.SetInstanceText(node, "text-0", done ? Short(TalentSystem.GetTalent(hero.selectedTalentIds[i])?.name) : now ? "!" : "?");
                cell.Add(node);
                var col = new VisualElement();
                col.Add(new UiLabel($"LV {lv}") { style = { fontSize = 12, color = now ? Palette.Gold1 : done ? Palette.Mist1 : Palette.Mist3 } });
                col.Add(new UiLabel(done ? (TalentSystem.GetTalent(hero.selectedTalentIds[i])?.name ?? "OWNED") : now ? "CHOOSING" : "LOCKED")
                {
                    style = { fontSize = 12, color = now ? Palette.Gold2 : Palette.Mist3, maxWidth = 125, maxHeight = 32, whiteSpace = WhiteSpace.Normal, overflow = Overflow.Hidden }
                });
                cell.Add(col);
                strip.Add(cell);
            }
            screen.Add(strip);
        }

        static string Short(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var w = name.Split(' ');
            return (w.Length >= 2 ? $"{w[0][0]}{w[1][0]}" : name.Substring(0, Math.Min(2, name.Length))).ToUpperInvariant();
        }

        void BuildPool(VisualElement screen, Unit hero, List<TalentDef> pool)
        {
            int shared = pool.Count(t => t.category == TalentCategory.Shared), cls = pool.Count - shared;
            var tabRow = new VisualElement { style = { position = Position.Absolute, left = 24, top = 178, height = 38, flexDirection = FlexDirection.Row } };
            string[] segs = { $"ALL {pool.Count}", $"SHARED {shared}", $"{ClassName(hero)} {cls}".ToUpperInvariant() };
            for (int i = 0; i < segs.Length; i++)
            {
                int f = i; bool active = filter == i;
                var btn = FigmaUi.CreateTabButton(segs[i], active, null, 38f, 16f);
                btn.style.paddingLeft = 14; btn.style.paddingRight = 14; btn.style.marginRight = 6;
                tabRow.Add(btn);
                var it = menu.Add(btn, segs[i], () => { filter = f; Rebuild(); }, selected: active);
                it.visual = on => FigmaUi.ApplyTabVisual(btn, active, on, true);
                it.visual(false);
            }
            screen.Add(tabRow);

            var scroll = poolScroll = FigmaUi.CreateVerticalScrollView();
            scroll.style.position = Position.Absolute;
            scroll.style.left = 44;
            scroll.style.top = 232;
            scroll.style.width = 804;
            scroll.style.height = 450;

            var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            scroll.Add(grid);

            var shown = pool.Where(t => filter == 0 || (filter == 1) == (t.category == TalentCategory.Shared)).ToList();
            int firstCardMenuIdx = menu.items.Count;
            const int cols = 3;
            for (int i = 0; i < shown.Count; i++)
            {
                var td = shown[i];
                int cardIdx = i;
                bool owned = hero.HasTalent(td.id), sel = td.id == selectedId;
                var card = new VisualElement
                {
                    style =
                    {
                        width = 246, minHeight = 166, marginRight = 8, marginBottom = 10,
                        paddingLeft = 12, paddingRight = 12, paddingTop = 10, paddingBottom = 10,
                        overflow = Overflow.Hidden,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    }
                };
                card.Add(new UiLabel(td.name) { style = { fontSize = 18, color = owned ? Palette.Mist3 : sel ? Palette.Gold1 : Palette.Mist1, marginBottom = 6 } });
                var tag = FigmaUi.InstantiateTemplate("Tag-Cls");
                if (tag != null)
                {
                    FigmaUi.SetInstanceText(tag, "text-0", td.category == TalentCategory.Shared ? "SHARED" : ClassName(hero).ToUpperInvariant());
                    tag.style.marginBottom = 6;
                    card.Add(tag);
                }
                string fx = (owned ? "OWNED · " : "") + (td.description ?? "");
                card.Add(new UiLabel(fx.ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal } });
                grid.Add(card);

                var it = menu.Add(card, td.name, () => { selectedId = td.id; Rebuild(); }, selected: sel);
                it.onNavigate = dir =>
                {
                    if (dir == 0 && cardIdx >= cols) return firstCardMenuIdx + cardIdx - cols;
                    if (dir == 2 && cardIdx + cols < shown.Count) return firstCardMenuIdx + cardIdx + cols;
                    if (dir == 3 && cardIdx % cols > 0) return firstCardMenuIdx + cardIdx - 1;
                    if (dir == 1 && cardIdx % cols < cols - 1 && cardIdx + 1 < shown.Count) return firstCardMenuIdx + cardIdx + 1;
                    return -1;
                };
                it.visual = on =>
                {
                    card.style.backgroundColor = on || sel ? Palette.Hex(0x2B2210) : owned ? Palette.Ink1 : Palette.Ink2;
                    var b = on ? Palette.Gold1 : sel ? Palette.Gold2 : Palette.Ink4;
                    card.style.borderTopColor = b; card.style.borderBottomColor = b; card.style.borderLeftColor = b; card.style.borderRightColor = b;
                };
                it.visual(false);
            }
            screen.Add(scroll);
            if (savedScrollY > 0f)
                FigmaUi.OnFirstLayout(scroll, () => { if (poolScroll == scroll) scroll.scrollOffset = new Vector2(0, savedScrollY); });
        }

        void BuildDetail(VisualElement screen, Unit hero)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 194, width = 346, height = 490, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);

            var t = selectedId != null ? TalentSystem.GetTalent(selectedId) : null;
            if (t == null) { host.Add(FigmaUi.CreateSectionHeader("No talents", 346f)); return; }

            host.Add(FigmaUi.CreateSectionHeader(t.name, 346f));

            var detailScroll = FigmaUi.CreateVerticalScrollView();
            detailScroll.style.flexGrow = 1;
            detailScroll.style.flexBasis = 0;
            detailScroll.style.minHeight = 0;
            detailScroll.style.marginTop = 10;
            detailScroll.style.marginBottom = 10;
            host.Add(detailScroll);

            detailScroll.Add(new UiLabel((t.description ?? "").ToUpperInvariant()) { style = { fontSize = 14, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 14 } });

            detailScroll.Add(new UiLabel($"{hero.name}'S TALENTS".ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Mist3, marginBottom = 6 } });
            for (int i = 0; i < TalentSystem.MilestoneLevels.Length; i++)
            {
                int lv = TalentSystem.MilestoneLevels[i];
                bool owned = i < hero.selectedTalentIds.Count, cur = !owned && i < hero.EarnedTalentSlots && i == hero.selectedTalentIds.Count;
                if (!owned && !cur) break;
                string value = owned ? (TalentSystem.GetTalent(hero.selectedTalentIds[i])?.name ?? "") : $"{t.name} ?";
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, height = 26, borderBottomWidth = 2, borderBottomColor = cur ? Palette.Gold3 : Palette.Ink3 } };
                row.Add(new UiLabel($"LV {lv}") { style = { fontSize = 13, color = cur ? Palette.Gold2 : Palette.Mist2 } });
                row.Add(new UiLabel(value.ToUpperInvariant()) { style = { fontSize = 13, color = cur ? Palette.Gold1 : Palette.Mist1 } });
                detailScroll.Add(row);
            }

            if (!string.IsNullOrEmpty(feedback))
                detailScroll.Add(new UiLabel(feedback.ToUpperInvariant()) { style = { fontSize = 13, color = feedbackBad ? Palette.Err : Palette.Ok, whiteSpace = WhiteSpace.Normal, marginTop = 12 } });

            bool can = hero.AvailableTalentSelections > 0 && TalentSystem.ValidateSelection(hero, t.id, out _);
            string label = hero.HasTalent(t.id) ? "Already owned" : hero.AvailableTalentSelections <= 0 ? "No pick available" : $"Choose {t.name}";
            var btn = FigmaUi.CreateButton(label, can ? "button-primary" : "button-slab", 346f);
            btn.style.flexShrink = 0;
            btn.SetEnabled(can);
            host.Add(btn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(btn, label, () => Take(t), interactable: can));
        }

        void BuildRespecDialog()
        {
            var target = party[Mathf.Clamp(respecTarget, 0, party.Count - 1)];
            var backdrop = new VisualElement { name = "TalentRespecModal" };
            backdrop.AddToClassList("uitk-modal-backdrop");
            var card = new VisualElement();
            card.AddToClassList("uitk-modal-panel");
            card.style.width = 660;
            card.style.borderTopColor = Palette.Gold3; card.style.borderBottomColor = Palette.Gold3;
            card.style.borderLeftColor = Palette.Gold3; card.style.borderRightColor = Palette.Gold3;

            card.Add(new UiLabel("Use Scroll of Emptiness?") { style = { fontSize = 24, color = Palette.Gold1, marginBottom = 12 } });
            card.Add(new UiLabel("CHOOSE 1 CHARACTER") { style = { fontSize = 12, color = Palette.Mist3, marginBottom = 6 } });

            dialogMenu = Root.gameObject.AddComponent<UitkMenu>();
            dialogMenu.AttachRoot(backdrop);
            dialogMenu.onFocus = s => { if (!rebuilding && s != null) dialogFocusIndex = dialogMenu.items.IndexOf(s); };
            dialogMenu.onBack = () => { respecOpen = false; dialogFocusIndex = -1; Rebuild(); };

            var charRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 14 } };
            for (int i = 0; i < party.Count; i++)
            {
                int idx = i; var u = party[i]; bool sel = i == respecTarget;
                var cCard = FigmaUi.CreateCompactButton($"{u.name} ({u.selectedTalentIds.Count})", 144f);
                cCard.style.marginRight = i < party.Count - 1 ? 8 : 0;
                charRow.Add(cCard);
                var it = dialogMenu.Add(cCard, u.name, () => { respecTarget = idx; Rebuild(); }, selected: sel);
                FigmaUi.ApplyDefaultFocusVisual(it);
            }
            card.Add(charRow);

            string cleared = target.selectedTalentIds.Count == 0
                ? "NO TALENTS CHOSEN YET."
                : "CLEARED: " + string.Join(", ", target.selectedTalentIds.Select(id => TalentSystem.GetTalent(id)?.name ?? id)).ToUpperInvariant();
            card.Add(new UiLabel(cleared) { style = { fontSize = 13, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal, marginBottom = 10 } });
            card.Add(new UiLabel($"{target.name} PICKS {target.EarnedTalentSlots} TALENTS AGAIN RIGHT AWAY.".ToUpperInvariant()) { style = { fontSize = 13, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 16 } });
            if (Scrolls == 0) card.Add(new UiLabel("YOU HAVE NO SCROLL OF EMPTINESS.") { style = { fontSize = 13, color = Palette.Err, marginBottom = 10 } });

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            var cancel = FigmaUi.CreateButton("Cancel", "button-slab", 140f); cancel.style.marginRight = 10;
            bool canUse = Scrolls > 0 && Bag != null && target.selectedTalentIds.Count > 0;
            var use = FigmaUi.CreateButton($"Use on {target.name}", canUse ? "button-primary" : "button-slab", 220f);
            use.SetEnabled(canUse);
            btnRow.Add(cancel); btnRow.Add(use);
            card.Add(btnRow);
            backdrop.Add(card);
            UiRoot.Add(backdrop);

            FigmaUi.ApplyDefaultFocusVisual(dialogMenu.Add(cancel, "Cancel", () => { respecOpen = false; dialogFocusIndex = -1; Rebuild(); }));
            FigmaUi.ApplyDefaultFocusVisual(dialogMenu.Add(use, "Use", () => UseScroll(target), interactable: canUse));
        }

        void Take(TalentDef t)
        {
            var hero = CurrentHero;
            if (hero.TrySelectTalent(t.id, out string error)) { feedback = $"{hero.name} LEARNED {t.name}."; feedbackBad = false; Sfx.Play(Sound.SkillUnlock); }
            else { feedback = error ?? "CANNOT TAKE THIS TALENT."; feedbackBad = true; Sfx.Play(Sound.Deny); }
            Rebuild();
        }

        void UseScroll(Unit target)
        {
            dialogFocusIndex = -1;
            if (Bag != null && Bag.UseScrollOfEmptiness(target, out _))
            {
                Game.I?.Run?.Items.Remove("scroll_of_emptiness");
                heroIndex = party.IndexOf(target);
                respecOpen = false; selectedId = null;
                feedback = $"{target.name}'S TALENTS WERE CLEARED. CHOOSE {target.EarnedTalentSlots} AGAIN.";
                feedbackBad = false; Sfx.Play(Sound.SkillUnlock);
            }
            else { feedback = "NO SCROLL OF EMPTINESS IN THE INVENTORY."; feedbackBad = true; Sfx.Play(Sound.Deny); }
            Rebuild();
        }
    }
}
