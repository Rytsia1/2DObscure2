using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Talents — Starlit Reliquary "J2 · Talent Selection" and "J3 · Talent Respec" boards at 640x360.
    // J2: header (Back · portrait · "<hero> reached Lv n" · Choose a Talent · milestone strip Lv 3/6/9/12/15 · scroll count),
    // full talent pool (All / Shared / Class filter; owned greyed), gold detail with the hero's talents and Take.
    // J3: "Use Scroll of Emptiness?" dialog — pick 1 character, shows what is cleared; re-picks happen here right away.
    public class TalentScreen : GameScreen
    {
        readonly GameState returnState;
        readonly List<Unit> party;
        int heroIndex;

        public Unit CurrentHero => party != null && party.Count > 0 ? party[Math.Clamp(heroIndex, 0, party.Count - 1)] : null;

        const int Left = 12, RightEdge = 628, BodyY = 33, BodyBottom = 351, PoolW = 419, DetailX = 438;
        static readonly Color SharedBorder = Palette.Mist3, ClassBorder = Palette.Hex(0xD9634C), ClassInk = Palette.Hex(0xFFC7BB);

        Menu menu;
        Transform page;
        int filter; // 0 All, 1 Shared, 2 Class
        string selectedId;
        bool respecOpen;
        int respecTarget;
        string feedback;
        bool feedbackBad;
        int focusIndex;
        bool rebuilding;
        Menu dialogMenu;

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

            heroIndex = Math.Clamp(partyUnits == null && initialHero == 0 ? PartyScreen.SelectedIndex : initialHero, 0, Math.Max(0, (party?.Count ?? 1) - 1));
            returnState = backTo ?? ReturnTarget();
        }

        static InventoryRuntime Bag => Game.I?.Run?.Inventory;
        static int Scrolls => Bag == null ? 0 : Bag.GetConsumableCount("CON_SCROLL_OF_EMPTINESS") + Bag.GetConsumableCount("scroll_of_emptiness");

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => { if (respecOpen) { respecOpen = false; Rebuild(); } else Game?.Go(returnState); };
            menu.onFocus = s =>
            {
                if (rebuilding || s == null) return;
                focusIndex = menu.items.IndexOf(s);
                if (s.tooltip != null && s.tooltip.StartsWith("talent:") && !s.selected) { selectedId = s.tooltip.Substring(7); Rebuild(); }
            };
            Rebuild();
        }

        void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                if (page != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(page.gameObject);
                    else UnityEngine.Object.DestroyImmediate(page.gameObject);
                }
                page = new GameObject("Talents").transform;
                page.SetParent(Root, false);
                menu.items.Clear();
                dialogMenu = null;

                var hero = CurrentHero;
                if (hero == null) return;
                var pool = Pool(hero);
                if (selectedId == null || pool.All(t => t.id != selectedId))
                    selectedId = pool.FirstOrDefault(t => !hero.HasTalent(t.id))?.id ?? pool.FirstOrDefault()?.id;

                BuildHeader(hero);
                BuildPool(hero, pool);
                BuildDetail(hero);
                if (respecOpen) BuildRespecDialog();
                if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
                if (dialogMenu != null) dialogMenu.Focus(dialogMenu.items.Count - 1, false, true);
            }
            finally { rebuilding = false; }
        }

        // Every talent this class may take (owned ones stay listed, greyed).
        List<TalentDef> Pool(Unit hero)
        {
            string cid = hero.classDef?.id ?? "";
            return TalentSystem.AllTalents.Values.Where(t => t.IsCompatibleWithClass(cid))
                .OrderBy(t => hero.HasTalent(t.id)).ThenBy(t => t.category).ThenBy(t => t.name).ToList();
        }

        string ClassName(Unit hero) => hero.classDef?.name ?? "Class";

        // ---------------------------------------------------------------- header

        void BuildHeader(Unit hero)
        {
            menu.Add(Ui.Button(page, "Back", Left, 11, 40, () => Game?.Go(returnState), 18, showCursor: false));

            // Portrait doubles as the hero switcher (click / left-right)
            var p = Ui.Button(page, "", Left + 48, 8, 22, () => Switch(1), 22, showCursor: false, style: ButtonStyle.Card);
            p.selected = true;
            p.visual(false);
            p.onAdjust = Switch;
            Color c = Palette.ForClass(hero.classDef?.id);
            Ui.Image(p.transform, PixelArt.Box(22, 22, Color.Lerp(c, Palette.Ink1, 0.7f), Palette.Gold2), 0, 0, Ui.Widget + 1);
            Ui.Rect(p.transform, 8, 4, 6, 7, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 2);
            Ui.Rect(p.transform, 4, 15, 14, 5, c, Ui.Widget + 2);
            string code = (hero.classDef?.name ?? "").Substring(0, Math.Min(3, (hero.classDef?.name ?? "").Length)).ToUpperInvariant();
            Ui.Image(p.transform, PixelArt.Badge(PixelFont.Measure(code) + 3, 8, Palette.Ink0, Palette.Ink0), 1, 13, Ui.Widget + 3);
            Ui.Label(p.transform, code, 2, 14, c, order: Ui.Widget + 4, font: FontKind.Interface);
            menu.Add(p);

            int x = Left + 78;
            string reached = hero.AvailableTalentSelections > 0 ? $"{hero.name} reached Lv {hero.level}" : $"{hero.name} · Lv {hero.level}";
            Ui.Label(page, reached.ToUpperInvariant(), x, 8, Palette.Gold2, font: FontKind.Interface);
            Ui.Label(page, "Choose a Talent", x, 17, Palette.Gold2);

            // Milestones: done = ink, the one being chosen = gold, later = dashed
            int mx = x + PixelFont.Measure("Choose a Talent", FontKind.DotGothic) + 14;
            int chosen = hero.selectedTalentIds.Count;
            for (int i = 0; i < TalentSystem.MilestoneLevels.Length; i++)
            {
                int lv = TalentSystem.MilestoneLevels[i];
                bool done = i < chosen, now = !done && i < hero.EarnedTalentSlots;
                var box = done ? PixelArt.Badge(25, 13, Palette.Ink4, Palette.Mist3)
                    : now ? PixelArt.Badge(25, 13, Palette.Hex(0x2B2210), Palette.Gold2)
                    : PixelArt.DashedBadge(25, 13, Color.clear, Palette.Ink5);
                Ui.Image(page, box, mx, 8, Ui.Widget);
                Ui.Label(page, $"LV {lv}", mx + 13, 11, done ? Palette.Mist2 : now ? Palette.Gold1 : Palette.Mist3, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
                string under = done ? Short(TalentSystem.GetTalent(hero.selectedTalentIds[i])?.name) : now ? "CHOOSING" : "";
                if (under != "") Ui.Label(page, under, mx + 13, 23, Palette.Mist3, Align.Center, font: FontKind.Interface);
                mx += 28;
            }

            // Scroll of Emptiness count opens the respec dialog (J3)
            string scroll = $"SCROLL OF EMPTINESS X{Scrolls}";
            int sw = PixelFont.Measure(scroll) + 12;
            var sb = Ui.Button(page, "", RightEdge - sw, 11, sw, () => { respecOpen = true; respecTarget = heroIndex; Rebuild(); }, 16, showCursor: false);
            var sbox = sb.GetComponentInChildren<SpriteRenderer>();
            Ui.Label(sb.transform, scroll, 6, 5, Palette.Mist1, order: Ui.Widget + 1, font: FontKind.Interface);
            sb.visual = f => sbox.sprite = PixelArt.Badge(sw, 16, f ? Palette.Ink4 : Palette.Ink3, f ? Palette.Gold2 : Palette.Ink5);
            sb.visual(false);
            menu.Add(sb);
        }

        static string Short(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string s = name.ToUpperInvariant();
            return PixelFont.Measure(s) <= 46 ? s : PixelFont.Fit(s.Split(' ')[0], 46);
        }

        void Switch(int d)
        {
            if (party.Count <= 1) return;
            heroIndex = (heroIndex + d + party.Count) % party.Count;
            selectedId = null;
            feedback = null;
            Rebuild();
        }

        // ----------------------------------------------------------------- pool

        void BuildPool(Unit hero, List<TalentDef> pool)
        {
            Ui.Panel(page, Left, BodyY, PoolW, BodyBottom - BodyY);
            // Category filter (.seg)
            int shared = pool.Count(t => t.category == TalentCategory.Shared), cls = pool.Count - shared;
            string[] segs = { $"ALL {pool.Count}", $"SHARED {shared}", $"{ClassName(hero)} {cls}".ToUpperInvariant() };
            int sx = Left + 8;
            for (int i = 0; i < segs.Length; i++)
            {
                int f = i;
                bool on = filter == i;
                int w = PixelFont.Measure(segs[i]) + 12;
                var seg = Ui.Button(page, "", sx, BodyY + 6, w, () => { filter = f; Rebuild(); }, 14, showCursor: false);
                var box = seg.GetComponentInChildren<SpriteRenderer>();
                Ui.Label(seg.transform, segs[i], 6, 4, on ? Palette.Hex(0x1D1404) : Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                seg.visual = focus => box.sprite = PixelArt.Badge(w, 14, on ? Palette.Gold2 : focus ? Palette.Ink3 : Palette.Ink1, Palette.Ink5);
                seg.visual(false);
                menu.Add(seg);
                sx += w - 1;
            }
            Ui.Rect(page, Left + 2, BodyY + 25, PoolW - 4, 1, Palette.Ink4, Ui.Panels + 2);

            var shown = pool.Where(t => filter == 0 || (filter == 1) == (t.category == TalentCategory.Shared)).ToList();
            int cw = (PoolW - 17 - 6 - 3) / 2, y0 = BodyY + 31;
            for (int i = 0; i < shown.Count; i++)
            {
                int row = i / 2;
                int y = y0 + row * 28;
                if (y + 25 > BodyBottom - 4) break;
                var t = shown[i];
                bool owned = hero.HasTalent(t.id), sel = t.id == selectedId;
                var card = Ui.Button(page, "", Left + 17 + (i % 2) * (cw + 3), y, cw, () => { selectedId = t.id; Rebuild(); }, 25, style: ButtonStyle.Card);
                card.selected = sel;
                var box = card.GetComponentInChildren<SpriteRenderer>();
                card.visual = focus =>
                {
                    box.sprite = owned && !(focus || sel) ? Hatched(cw, 25) : PixelArt.Box(cw, 25, Palette.Ink2, focus || sel ? Palette.Gold3 : Palette.Ink4);
                };
                card.visual(false);
                card.tooltip = "talent:" + t.id;
                Color ink = owned ? Palette.Mist3 : sel ? Palette.Gold1 : Palette.Mist1;
                Ui.Label(card.transform, PixelFont.Fit(t.name, cw - 58, FontKind.DotGothic), 6, 2, ink, order: Ui.Widget + 1);
                bool isShared = t.category == TalentCategory.Shared;
                string cat = isShared ? "SHARED" : ClassName(hero).ToUpperInvariant();
                int tw = PixelFont.Measure(cat) + 8;
                Ui.Image(card.transform, PixelArt.Badge(tw, 10, Palette.Ink0, isShared ? SharedBorder : ClassBorder), cw - tw - 4, 3, Ui.Widget + 1);
                Ui.Label(card.transform, cat, cw - tw, 5, isShared ? Palette.Mist1 : ClassInk, order: Ui.Widget + 2, font: FontKind.Interface);
                string fx = (owned ? "OWNED · " : "") + (t.description ?? "");
                Ui.Label(card.transform, PixelFont.Fit(fx.ToUpperInvariant(), cw - 12), 6, 15, Palette.Mist3, order: Ui.Widget + 1, font: FontKind.Interface);
                menu.Add(card);
            }
        }

        static Sprite Hatched(int w, int h)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    px[y * w + x] = edge ? (Color32)Palette.Ink4 : (x + y) % 8 < 2 ? (Color32)Color.Lerp(Palette.Ink2, Palette.Ink4, 0.5f) : (Color32)Palette.Ink2;
                }
            return PixelArt.SpriteFrom(px, w, h, PixelArt.TopLeft, "owned");
        }

        // --------------------------------------------------------------- detail

        void BuildDetail(Unit hero)
        {
            int dw = RightEdge - DetailX, x = DetailX + 8, w = dw - 16;
            Ui.Panel(page, DetailX, BodyY, dw, BodyBottom - BodyY, null, Palette.Gold3);
            var t = selectedId != null ? TalentSystem.GetTalent(selectedId) : null;
            if (t == null) { Ui.Header(page, "No talents", DetailX + 8, BodyY + 6, dw - 4); return; }

            bool isShared = t.category == TalentCategory.Shared;
            string cat = isShared ? "SHARED" : ClassName(hero).ToUpperInvariant();
            int cw = PixelFont.Measure(cat) + 8;
            Ui.Header(page, PixelFont.Fit(t.name, dw - cw - 30, FontKind.DotGothic), DetailX + 8, BodyY + 6, dw - 4);
            Ui.Image(page, PixelArt.Badge(cw, 11, Palette.Ink0, isShared ? SharedBorder : ClassBorder), DetailX + dw - cw - 8, BodyY + 7, Ui.Widget);
            Ui.Label(page, cat, DetailX + dw - cw - 4, BodyY + 9, isShared ? Palette.Mist1 : ClassInk, order: Ui.Widget + 1, font: FontKind.Interface);

            int y = BodyY + 26;
            string desc = PixelFont.Wrap((t.description ?? "").ToUpperInvariant(), w);
            Ui.Label(page, desc, x, y, Palette.Mist1, font: FontKind.Interface);
            y += (desc.Count(ch => ch == '\n') + 1) * PixelFont.LineHeight + 6;
            if (!t.isDesignFinalized) { Ui.Tag(page, "PROPOSED VALUES", x, y, TagKind.Cond); y += 16; }
            Ui.Rect(page, x, y, w, 1, Palette.Ink5, Ui.Panels + 2);
            y += 6;

            Ui.Label(page, $"{hero.name}'S TALENTS".ToUpperInvariant(), x, y, Palette.Mist3, font: FontKind.Interface);
            y += 11;
            for (int i = 0; i < TalentSystem.MilestoneLevels.Length; i++)
            {
                int lv = TalentSystem.MilestoneLevels[i];
                bool owned = i < hero.selectedTalentIds.Count, current = !owned && i < hero.EarnedTalentSlots && i == hero.selectedTalentIds.Count;
                if (!owned && !current) break;
                string value;
                if (owned)
                {
                    var o = TalentSystem.GetTalent(hero.selectedTalentIds[i]);
                    value = $"{o?.name} · {(o?.category == TalentCategory.Shared ? "SHARED" : ClassName(hero))}";
                }
                else value = $"{t.name} ?";
                Ui.Label(page, $"LV {lv}", x, y + 2, current ? Palette.Gold2 : Palette.Mist2, font: FontKind.Interface);
                Ui.Label(page, PixelFont.Fit(value.ToUpperInvariant(), w - 34), x + w, y + 2, current ? Palette.Gold1 : Palette.Mist1, Align.Right, font: FontKind.Interface);
                Ui.Rect(page, x, y + 12, w, 1, current ? Palette.Gold3 : Palette.Ink3, Ui.Panels + 2);
                y += 15;
            }

            if (!string.IsNullOrEmpty(feedback))
                Ui.Label(page, PixelFont.Wrap(feedback.ToUpperInvariant(), w), x, BodyBottom - 76, feedbackBad ? Palette.Err : Palette.Ok, font: FontKind.Interface);

            Ui.Rect(page, DetailX + 2, BodyBottom - 52, dw - 4, 1, Palette.Ink4, Ui.Panels + 2);
            Ui.Label(page, PixelFont.Wrap($"PICK FREELY FROM SHARED OR {ClassName(hero)} — NO QUOTA. OWNED TALENTS CAN'T BE TAKEN TWICE.".ToUpperInvariant(), w), x, BodyBottom - 46, Palette.Mist2, font: FontKind.Interface);
            bool can = hero.AvailableTalentSelections > 0 && TalentSystem.ValidateSelection(hero, t.id, out _);
            string label = hero.HasTalent(t.id) ? "Already owned" : hero.AvailableTalentSelections <= 0 ? "No pick available" : $"Take {t.name}";
            menu.Add(Ui.Button(page, PixelFont.Fit(label, w - 12, FontKind.DotGothic), x, BodyBottom - 27, w, () => Take(t), 20, interactable: can, showCursor: false, style: ButtonStyle.Primary));
        }

        // ------------------------------------------------------- J3 · respec dialog

        void BuildRespecDialog()
        {
            const int w = 300, h = 214;
            int x = (Ui.Width - w) / 2, y = 70;
            var target = party[Mathf.Clamp(respecTarget, 0, party.Count - 1)];
            Ui.Rect(page, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 209), Ui.Overlay - 4);
            Ui.Image(page, PixelArt.Box(w, h, Palette.Ink2, Palette.Gold3), x, y, Ui.Overlay - 3);
            Ui.Image(page, PixelArt.Box(22, 22, Palette.Ink0, Color.Lerp(Palette.RarityRare, Palette.Ink4, 0.45f)), x + 10, y + 8, Ui.Overlay - 2);
            var icon = PixelArt.ItemIcon("scroll", Palette.Mist2);
            Ui.Image(page, icon, x + 10 + (22 - (int)icon.rect.width) / 2, y + 8 + (22 - (int)icon.rect.height) / 2, Ui.Overlay - 1);
            Ui.Label(page, "Use Scroll of Emptiness?", x + 38, y + 14, Palette.Gold1, order: Ui.Overlay);

            Ui.Label(page, "CHOOSE 1 CHARACTER", x + 10, y + 38, Palette.Mist3, order: Ui.Overlay, font: FontKind.Interface);
            var go = new GameObject("RespecDialog");
            go.transform.SetParent(page, false);
            var dm = go.AddComponent<Menu>();
            dm.onBack = () => { respecOpen = false; Rebuild(); };
            for (int i = 0; i < party.Count; i++)
            {
                int idx = i;
                var u = party[i];
                int cx = x + 10 + i * 70;
                var card = Ui.Button(page, "", cx, y + 49, 66, () => { respecTarget = idx; Rebuild(); }, 46, showCursor: false, style: ButtonStyle.Card);
                card.selected = i == respecTarget;
                card.visual(false);
                Color c = Palette.ForClass(u.classDef?.id);
                Ui.Image(card.transform, PixelArt.Box(20, 20, Color.Lerp(c, Palette.Ink1, 0.7f), Palette.Ink5), 23, 4, Ui.Widget + 1);
                Ui.Rect(card.transform, 30, 8, 6, 6, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 2);
                Ui.Rect(card.transform, 27, 17, 12, 4, c, Ui.Widget + 2);
                Ui.Label(card.transform, PixelFont.Fit(u.name, 60), 33, 27, Palette.Mist1, Align.Center, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(card.transform, $"{u.selectedTalentIds.Count} TALENTS", 33, 36, Palette.Mist3, Align.Center, order: Ui.Widget + 2, font: FontKind.Interface);
                Lift(card);
                dm.Add(card);
            }

            Ui.Label(page, "THESE ARE CLEARED", x + 10, y + 102, Palette.Mist3, order: Ui.Overlay, font: FontKind.Interface);
            int tx = x + 10, ty = y + 113;
            for (int i = 0; i < target.selectedTalentIds.Count; i++)
            {
                string s = $"LV {TalentSystem.MilestoneLevels[Math.Min(i, 4)]} · {TalentSystem.GetTalent(target.selectedTalentIds[i])?.name}".ToUpperInvariant();
                int tw = PixelFont.Measure(s) + 8;
                if (tx + tw > x + w - 10) { tx = x + 10; ty += 14; }
                Ui.Image(page, PixelArt.Badge(tw, 11, Palette.Ink3, Palette.Ink5), tx, ty, Ui.Overlay - 2);
                Ui.Label(page, s, tx + 4, ty + 2, Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);
                Ui.Rect(page, tx + 3, ty + 5, tw - 6, 1, Palette.Mist2, Ui.Overlay + 1); // struck through
                tx += tw + 3;
            }
            if (target.selectedTalentIds.Count == 0) Ui.Label(page, "NO TALENTS CHOSEN YET.", x + 10, ty + 2, Palette.Mist3, order: Ui.Overlay, font: FontKind.Interface);
            string note = $"{target.name} PICKS {target.EarnedTalentSlots} TALENTS AGAIN RIGHT AWAY, ONE FOR EACH MILESTONE REACHED. THE SCROLL IS USED UP.";
            Ui.Label(page, PixelFont.Wrap(note.ToUpperInvariant(), w - 20), x + 10, ty + 18, Palette.Mist1, order: Ui.Overlay, font: FontKind.Interface);
            if (Scrolls == 0) Ui.Label(page, "YOU HAVE NO SCROLL OF EMPTINESS.", x + 10, y + h - 46, Palette.Err, order: Ui.Overlay, font: FontKind.Interface);

            Ui.Rect(page, x + 2, y + h - 32, w - 4, 30, Palette.Ink1, Ui.Overlay - 2);
            Ui.Rect(page, x + 2, y + h - 32, w - 4, 1, Palette.Ink4, Ui.Overlay - 2);
            var cancel = Ui.Button(page, "Cancel", x + w - 190, y + h - 26, 60, () => { respecOpen = false; Rebuild(); }, 20, showCursor: false);
            var use = Ui.Button(page, PixelFont.Fit($"Use on {target.name}", 110, FontKind.DotGothic), x + w - 124, y + h - 26, 114, () => UseScroll(target), 20, interactable: Scrolls > 0 && Bag != null, showCursor: false, style: ButtonStyle.Primary);
            Lift(cancel);
            Lift(use);
            dm.Add(cancel);
            dm.Add(use);
            dialogMenu = dm;
        }

        static void Lift(Selectable b)
        {
            foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += Ui.Overlay;
        }

        // ---------------------------------------------------------------- actions

        void Take(TalentDef t)
        {
            var hero = CurrentHero;
            if (hero.TrySelectTalent(t.id, out string error))
            {
                feedback = $"{hero.name} LEARNED {t.name}.";
                feedbackBad = false;
                Sfx.Play(Sound.Success);
            }
            else
            {
                feedback = error ?? "CANNOT TAKE THIS TALENT.";
                feedbackBad = true;
                Sfx.Play(Sound.Deny);
            }
            Rebuild();
        }

        void UseScroll(Unit target)
        {
            if (Bag != null && Bag.UseScrollOfEmptiness(target, out string err))
            {
                Game.I?.Run?.Items.Remove("scroll_of_emptiness");
                heroIndex = party.IndexOf(target);
                respecOpen = false;
                selectedId = null;
                feedback = $"{target.name}'S TALENTS WERE CLEARED. CHOOSE {target.EarnedTalentSlots} AGAIN.";
                feedbackBad = false;
                Sfx.Play(Sound.Success);
            }
            else
            {
                feedback = "NO SCROLL OF EMPTINESS IN THE INVENTORY.";
                feedbackBad = true;
                Sfx.Play(Sound.Deny);
            }
            Rebuild();
        }
    }
}
