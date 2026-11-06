using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Training Ground / Skill Tree — Starlit Reliquary "J1 · Training Ground / Skill Tree" board at 640x360:
    // header (Back · title · Q portraits E · hero · Skill Points), tree panel with 5 tier columns
    // (Active = square node, Passive = diamond, rank pips, links, locked tiers hatched), loadout strip,
    // gold detail panel (tags, stats, requirements, SP cost, Learn / Upgrade / Equip).
    public class SkillTreeScreen : GameScreen
    {
        readonly GameState returnState;
        readonly List<Unit> party;
        int heroIndex;

        public Unit CurrentHero => party[Math.Clamp(heroIndex, 0, party.Count - 1)];

        const int Left = 12, RightEdge = 628, BodyY = 33, BodyBottom = 351, TreeW = 419, DetailX = 438;
        const int TreeTop = BodyY + 22, TreeBottom = BodyBottom - 34;
        static readonly int[] TierLevel = { 1, 4, 7, 10, 13 };

        Menu menu;
        Transform page;
        string selectedSkillId;
        bool isSelectedPassive;
        bool confirmRespecActive;
        string feedback;
        bool feedbackBad;
        int focusIndex;
        bool rebuilding;

        class Node { public string id; public bool passive; public int tier; public int row; public int x, y; }

        public SkillTreeScreen() : this(null, 0, null) { }

        public SkillTreeScreen(IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
        {
            if (partyUnits != null)
                party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null)
                party = Game.I.Run.Party;
            else if (Game.I?.Roster != null)
                party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else
                party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            heroIndex = Math.Clamp(partyUnits == null && initialHero == 0 ? PartyScreen.SelectedIndex : initialHero, 0, Math.Max(0, party.Count - 1));
            returnState = backTo ?? ReturnTarget();
        }

        protected override void Build()
        {
            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game?.Go(returnState);
            menu.onFocus = s => { if (!rebuilding && s != null) focusIndex = menu.items.IndexOf(s); };
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
                page = new GameObject("SkillTree").transform;
                page.SetParent(Root, false);
                menu.items.Clear();

                var nodes = LayoutNodes();
                if (selectedSkillId == null && nodes.Count > 0) { selectedSkillId = nodes[0].id; isSelectedPassive = nodes[0].passive; }
                BuildHeader();
                BuildTree(nodes);
                BuildDetail();
                if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        // ------------------------------------------------------------------ data

        List<Node> LayoutNodes()
        {
            var hero = CurrentHero;
            var c = hero.classDef;
            var list = new List<Node>();
            if (c == null) return list;
            foreach (var sid in c.startingSkills ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(sid) && Database.Skills.ContainsKey(sid)) list.Add(new Node { id = sid, tier = 1 });
            if (!string.IsNullOrEmpty(c.starterPassiveId) && Database.Passives.ContainsKey(c.starterPassiveId))
                list.Add(new Node { id = c.starterPassiveId, passive = true, tier = 1 });
            foreach (var n in c.skillUnlockProgression ?? Array.Empty<SkillUnlockNode>())
                if (list.All(x => x.id != n.skillId)) list.Add(new Node { id = n.skillId, passive = n.isPassive, tier = Mathf.Clamp(n.tier, 1, 5) });

            int colW = (TreeW - 4) / 5, h = TreeBottom - TreeTop - 22;
            foreach (var g in list.GroupBy(n => n.tier))
            {
                var ordered = g.OrderBy(n => n.passive).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].row = i;
                    ordered[i].x = Left + 2 + (g.Key - 1) * colW + colW / 2;
                    ordered[i].y = TreeTop + 22 + h * (i + 1) / (ordered.Count + 1);
                }
            }
            return list;
        }

        bool Unlocked(Node n) => n.passive ? CurrentHero.skillLoadout.UnlockedPassiveSkills.Contains(n.id) : CurrentHero.skillLoadout.UnlockedActiveSkills.Contains(n.id);
        bool Available(Node n) => !Unlocked(n) && CurrentHero.skillLoadout.CanUnlockSkill(n.id, CurrentHero.level, CurrentHero.classDef, out _);

        static string NameOf(string id, bool passive) =>
            passive ? (Database.Passives.TryGetValue(id, out var p) ? p.name : id) : (Database.Skills.TryGetValue(id, out var s) ? s.name : id);

        static string Initials(string name)
        {
            var w = (name ?? "?").Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (w.Length == 0) return "?";
            return (w.Length >= 2 ? $"{w[0][0]}{w[1][0]}" : w[0].Substring(0, Math.Min(2, w[0].Length))).ToUpperInvariant();
        }

        // ---------------------------------------------------------------- header

        void BuildHeader()
        {
            var hero = CurrentHero;
            menu.Add(Ui.Button(page, "Back", Left, 9, 40, () => Game?.Go(returnState), 18, showCursor: false));
            Ui.Label(page, "Training Ground", Left + 50, 13, Palette.Gold2);
            int x = Left + 50 + PixelFont.Measure("Training Ground", FontKind.DotGothic) + 16;
            Ui.Badge(page, "Q", x, 13, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);
            x += 15;
            for (int i = 0; i < party.Count; i++)
            {
                int idx = i;
                var p = Ui.Button(page, "", x, 9, 18, () => { if (heroIndex != idx) { heroIndex = idx; selectedSkillId = null; feedback = null; Rebuild(); } }, 18, showCursor: false, style: ButtonStyle.Card);
                p.selected = i == heroIndex;
                p.visual(false);
                p.onAdjust = d => { heroIndex = (heroIndex + d + party.Count) % party.Count; selectedSkillId = null; feedback = null; Rebuild(); };
                Color c = Palette.ForClass(party[i].classDef?.id);
                Ui.Image(p.transform, PixelArt.Box(18, 18, Color.Lerp(c, Palette.Ink1, 0.7f), i == heroIndex ? Palette.Gold2 : Palette.Ink5), 0, 0, Ui.Widget + 1);
                Ui.Rect(p.transform, 6, 4, 6, 6, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 2);
                Ui.Rect(p.transform, 3, 12, 12, 4, c, Ui.Widget + 2);
                menu.Add(p);
                x += 21;
            }
            Ui.Badge(page, "E", x + 1, 13, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);
            x += 20;
            Ui.Label(page, PixelFont.Fit($"{hero.name} · {hero.classDef?.name} · Lv {hero.level}", 150, FontKind.DotGothic), x, 13, Palette.Mist2);

            // Skill Points box (gold frame)
            int sp = hero.skillLoadout.SkillPoints;
            const int bw = 92;
            Ui.Image(page, PixelArt.Badge(bw, 20, Palette.Hex(0x1D1608), Palette.Gold3), RightEdge - bw, 8, Ui.Widget);
            Ui.Label(page, "SKILL POINTS", RightEdge - bw + 6, 15, Palette.Gold2, order: Ui.Widget + 1, font: FontKind.Interface);
            Ui.Label(page, sp.ToString(), RightEdge - 6, 12, Palette.Gold1, Align.Right, order: Ui.Widget + 1);
            menu.Add(Ui.Button(page, "Talents ›", RightEdge - bw - 70, 9, 64, () => Game?.Go(GameState.Talents), 18, showCursor: false, style: ButtonStyle.Flat));
        }

        // ------------------------------------------------------------------ tree

        void BuildTree(List<Node> nodes)
        {
            var hero = CurrentHero;
            Ui.Panel(page, Left, BodyY, TreeW, BodyBottom - BodyY);
            Ui.Header(page, $"{hero.classDef?.name ?? "Class"} skill tree", Left + 8, BodyY + 6, TreeW - 4);
            // Legend: square = Active, diamond = Passive
            int lx = Left + TreeW - 8 - PixelFont.Measure("PASSIVE");
            Ui.Label(page, "PASSIVE", lx, BodyY + 8, Palette.Mist2, font: FontKind.Interface);
            Ui.Image(page, PixelArt.Diamond(7, Palette.Mist2, false), lx - 10, BodyY + 8, Ui.Widget);
            lx -= 14 + PixelFont.Measure("ACTIVE") + 6;
            Ui.Label(page, "ACTIVE", lx, BodyY + 8, Palette.Mist2, font: FontKind.Interface);
            Ui.Image(page, PixelArt.Badge(6, 6, Color.clear, Palette.Mist2), lx - 9, BodyY + 9, Ui.Widget);

            // Tier bands
            int maxTier = SkillLoadout.GetMaxUnlockedTier(hero.level);
            int colW = (TreeW - 4) / 5;
            for (int t = 1; t <= 5; t++)
            {
                int cx = Left + 2 + (t - 1) * colW;
                bool open = t <= maxTier;
                if (!open) Hatch(cx, TreeTop, colW, TreeBottom - TreeTop);
                if (t < 5) Ui.Rect(page, cx + colW - 1, TreeTop, 1, TreeBottom - TreeTop, Palette.Ink3, Ui.Panels + 2);
                Color col = open ? Palette.Gold1 : Palette.Mist3;
                Ui.Label(page, $"TIER {t}", cx + colW / 2, TreeTop + 4, col, Align.Center, font: FontKind.Interface);
                Ui.Label(page, open ? $"LV {TierLevel[t - 1]}" : $"LV {TierLevel[t - 1]} · LOCKED", cx + colW / 2, TreeTop + 13, col, Align.Center, font: FontKind.Interface);
            }

            // Links between same-row nodes of consecutive tiers
            foreach (var a in nodes)
            {
                var b = nodes.FirstOrDefault(n => n.tier == a.tier + 1 && n.row == a.row);
                if (b == null) continue;
                int x0 = a.x + 16, x1 = b.x - 16;
                if (x1 <= x0) continue;
                bool ua = Unlocked(a), ub = Unlocked(b);
                if (ua && ub) Ui.Rect(page, x0, a.y - 1, x1 - x0, 2, Palette.Gold3, Ui.Panels + 3);
                else
                {
                    Color c = ua && Available(b) ? Palette.Mist1 : Palette.Ink5;
                    for (int x = x0; x < x1; x += 7) Ui.Rect(page, x, a.y - 1, Math.Min(4, x1 - x), 2, c, Ui.Panels + 3);
                }
            }

            foreach (var n in nodes) NodeButton(n);

            // Tooltip (.tip) for a selected locked node
            var sel = nodes.FirstOrDefault(n => n.id == selectedSkillId);
            if (sel != null && !Unlocked(sel) && !Available(sel))
            {
                hero.skillLoadout.CanUnlockSkill(sel.id, hero.level, hero.classDef, out string why);
                string body = PixelFont.Wrap((why ?? "LOCKED").ToUpperInvariant(), 104);
                int h = 22 + (body.Count(c => c == '\n') + 1) * PixelFont.LineHeight;
                int tx = Mathf.Clamp(sel.x - 30, Left + 4, Left + TreeW - 120), ty = sel.y + 22;
                if (ty + h > TreeBottom) ty = sel.y - 22 - h;
                Ui.Image(page, PixelArt.Box(116, h, Palette.Ink2, Palette.Mist3), tx, ty, Ui.Overlay - 2);
                Ui.Label(page, PixelFont.Fit($"{NameOf(sel.id, sel.passive)} · locked", 104, FontKind.DotGothic), tx + 6, ty + 5, Palette.Mist1, order: Ui.Overlay - 1);
                Ui.Label(page, body, tx + 6, ty + 18, Palette.Mist2, order: Ui.Overlay - 1, font: FontKind.Interface);
            }

            // Loadout strip
            int sy = TreeBottom;
            Ui.Rect(page, Left + 2, sy, TreeW - 4, 1, Palette.Ink4, Ui.Panels + 2);
            Ui.Label(page, "LOADOUT", Left + 10, sy + 13, Palette.Mist3, font: FontKind.Interface);
            var lo = hero.skillLoadout;
            int ax = Left + 60;
            Ui.Label(page, $"ACTIVE {lo.ActiveSlots.Count(s => !string.IsNullOrEmpty(s))}/3", ax, sy + 13, Palette.Mist2, font: FontKind.Interface);
            ax += 58;
            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
            {
                string id = lo.ActiveSlots[i];
                if (!string.IsNullOrEmpty(id))
                {
                    Ui.Image(page, PixelArt.Box(17, 17, Palette.Hex(0x2B2210), Palette.Gold2), ax, sy + 8, Ui.Widget);
                    Ui.Label(page, Initials(NameOf(id, false)), ax + 9, sy + 13, Palette.Gold1, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
                }
                else Ui.Image(page, PixelArt.DashedBadge(17, 17, Palette.Ink3, Palette.Ink5), ax, sy + 8, Ui.Widget);
                ax += 21;
            }
            ax += 16;
            Ui.Label(page, $"PASSIVE {lo.PassiveSlots.Count(s => !string.IsNullOrEmpty(s))}/3", ax, sy + 13, Palette.Mist2, font: FontKind.Interface);
            ax += 64;
            for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
            {
                string id = lo.PassiveSlots[i];
                bool has = !string.IsNullOrEmpty(id);
                Ui.Image(page, PixelArt.Diamond(15, has ? Palette.Hex(0x2B2210) : Palette.Ink3), ax, sy + 9, Ui.Widget);
                Ui.Image(page, PixelArt.Diamond(15, has ? Palette.Gold2 : Palette.Ink5, false), ax, sy + 9, Ui.Widget + 1);
                if (has) Ui.Label(page, Initials(NameOf(id, true)), ax + 8, sy + 13, Palette.Gold1, Align.Center, order: Ui.Widget + 2, font: FontKind.Interface);
                ax += 19;
            }
        }

        // Locked tier hatch: faint diagonal stripes.
        void Hatch(int x, int y, int w, int h)
        {
            var px = new Color32[w * h];
            Color32 c = Palette.Hex(0x25315A, 64);
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if ((i + j) % 12 < 2) px[j * w + i] = c;
            Ui.Image(page, PixelArt.SpriteFrom(px, w, h, PixelArt.TopLeft, "hatch"), x, y, Ui.Panels + 1);
        }

        // Skill node (.sk): learned = gold, available = bright border, locked = dashed; passives are diamonds.
        void NodeButton(Node n)
        {
            var hero = CurrentHero;
            bool learned = Unlocked(n), avail = Available(n), sel = n.id == selectedSkillId;
            int size = n.passive ? 25 : 30;
            int x = n.x - size / 2, y = n.y - size / 2;
            var b = Ui.Button(page, "", x, y, size, () => { selectedSkillId = n.id; isSelectedPassive = n.passive; confirmRespecActive = false; feedback = null; Rebuild(); }, size, showCursor: false);
            var box = b.GetComponentInChildren<SpriteRenderer>();
            Color fill = learned ? Palette.Hex(0x2B2210) : avail ? Palette.Ink4 : Palette.Ink1;
            Color border = learned ? Palette.Gold2 : avail ? Palette.Mist1 : Palette.Ink5;
            Color ink = learned ? Palette.Gold1 : avail ? Palette.Mist1 : Palette.Mist3;
            var ring = Ui.Image(b.transform, null, -3, -3, Ui.Widget - 1);
            b.visual = focus =>
            {
                if (n.passive)
                {
                    box.sprite = PixelArt.Diamond(size, fill);
                    ring.sprite = sel || focus ? PixelArt.Diamond(size + 6, sel ? Palette.Gold1 : Palette.Mist3, false) : null;
                }
                else
                {
                    box.sprite = learned || avail ? PixelArt.Badge(size, size, fill, focus && !sel ? Palette.Mist1 : border) : PixelArt.DashedBadge(size, size, fill, border);
                    ring.sprite = sel || focus ? PixelArt.Badge(size + 6, size + 6, Color.clear, sel ? Palette.Gold1 : Palette.Mist3) : null;
                }
            };
            b.visual(false);
            if (n.passive) Ui.Image(b.transform, PixelArt.Diamond(size, border, false), 0, 0, Ui.Widget + 1);
            Ui.Label(b.transform, Initials(NameOf(n.id, n.passive)), size / 2, size / 2 - 3, ink, Align.Center, order: Ui.Widget + 2, font: FontKind.Interface);
            b.tooltip = NameOf(n.id, n.passive);

            // Rank pips (.pips) under learned nodes
            if (learned)
            {
                int max = n.passive ? (Database.Passives.TryGetValue(n.id, out var p) ? p.maxRank : 3) : (Database.Skills.TryGetValue(n.id, out var s) ? s.maxRank : 3);
                int rank = hero.skillLoadout.GetRank(n.id);
                int px = n.x - (max * 6 - 2) / 2;
                for (int i = 0; i < max; i++) Ui.Rect(page, px + i * 6, n.y + size / 2 + 4, 4, 4, i < rank ? Palette.Gold2 : Palette.Ink5, Ui.Widget);
            }
            menu.Add(b);
        }

        // ---------------------------------------------------------------- detail

        void BuildDetail()
        {
            var hero = CurrentHero;
            int dw = RightEdge - DetailX, x = DetailX + 8, w = dw - 16;
            Ui.Panel(page, DetailX, BodyY, dw, BodyBottom - BodyY, null, Palette.Gold3);
            if (string.IsNullOrEmpty(selectedSkillId)) { Ui.Header(page, "Select a skill", DetailX + 8, BodyY + 6, dw - 4); return; }

            SkillDef a = null;
            PassiveDef pv = null;
            if (isSelectedPassive) Database.Passives.TryGetValue(selectedSkillId, out pv);
            else Database.Skills.TryGetValue(selectedSkillId, out a);
            string name = a?.name ?? pv?.name ?? selectedSkillId;
            int tier = a?.tier ?? pv?.tier ?? 1, maxRank = a?.maxRank ?? pv?.maxRank ?? 3;
            var lo = hero.skillLoadout;
            bool learned = isSelectedPassive ? lo.UnlockedPassiveSkills.Contains(selectedSkillId) : lo.UnlockedActiveSkills.Contains(selectedSkillId);
            int rank = lo.GetRank(selectedSkillId);
            var node = hero.classDef?.skillUnlockProgression?.FirstOrDefault(n => n.skillId == selectedSkillId);

            Ui.Header(page, PixelFont.Fit(name, dw - 24, FontKind.DotGothic), DetailX + 8, BodyY + 6, dw - 4);
            int y = BodyY + 24, tx = x;
            tx += Ui.Tag(page, isSelectedPassive ? "PASSIVE" : "ACTIVE", tx, y) + 3;
            tx += Ui.Tag(page, $"TIER {tier}", tx, y) + 3;
            if (a != null)
            {
                PartyScreen.ElementChip(page, a.element, tx, y - 1);
                Ui.Label(page, a.element == Element.Physical ? "PHYSICAL" : "ELEMENTAL", tx + 16, y + 2, Palette.Mist2, font: FontKind.Interface);
            }
            y += 16;
            string desc = PixelFont.Wrap(((a?.description ?? pv?.description) ?? "").ToUpperInvariant(), w);
            Ui.Label(page, desc, x, y, Palette.Mist1, font: FontKind.Interface);
            y += (desc.Count(c => c == '\n') + 1) * PixelFont.LineHeight + 6;

            int shown = Math.Max(1, rank);
            if (a != null)
            {
                y = Row(x, y, w, "MP COST", a.GetMpCost(shown).ToString());
                y = Row(x, y, w, "TARGET", a.target.ToString().ToUpperInvariant());
                y = Row(x, y, w, "POWER", $"{a.GetPower(shown):0.##}X");
                if (a.cooldown > 0) y = Row(x, y, w, "COOLDOWN", $"{a.cooldown} TURNS");
            }
            y = Row(x, y, w, "RANK", learned ? $"{rank} / {maxRank}" : $"LOCKED · MAX {maxRank}");

            y += 4;
            Ui.Label(page, "REQUIREMENTS", x, y, Palette.Mist3, font: FontKind.Interface);
            y += 11;
            int req = node?.requiredLevel ?? TierLevel[Mathf.Clamp(tier - 1, 0, 4)];
            Check(x, ref y, hero.level >= req, $"LEVEL {req} · {hero.name} IS {hero.level}");
            foreach (var pre in node?.prerequisites ?? Array.Empty<string>())
            {
                bool met = lo.UnlockedActiveSkills.Contains(pre) || lo.UnlockedPassiveSkills.Contains(pre);
                Check(x, ref y, met, $"{NameOf(pre, !Database.Skills.ContainsKey(pre))} LEARNED");
            }

            // Equip / unequip row
            bool equipped = isSelectedPassive ? lo.PassiveSlots.Contains(selectedSkillId) : lo.ActiveSlots.Contains(selectedSkillId);
            if (learned)
            {
                int ey = BodyBottom - 80;
                Ui.Label(page, equipped ? "EQUIPPED" : "EQUIP TO", x, ey + 5, Palette.Mist3, font: FontKind.Interface);
                for (int i = 0; i < 3; i++)
                {
                    int slot = i;
                    menu.Add(Ui.Button(page, (i + 1).ToString(), x + 50 + i * 22, ey, 20, () => Equip(slot), 18, showCursor: false));
                }
                menu.Add(Ui.Button(page, "Unequip", x + w - 54, ey, 54, Unequip, 18, interactable: equipped, showCursor: false));
            }

            if (!string.IsNullOrEmpty(feedback))
                Ui.Label(page, PixelFont.Wrap(feedback.ToUpperInvariant(), w), x, BodyBottom - 56, feedbackBad ? Palette.Err : Palette.Ok, font: FontKind.Interface);

            // Footer: SP cost, SP after, Learn / Upgrade
            Ui.Rect(page, DetailX + 2, BodyBottom - 34, dw - 4, 1, Palette.Ink4, Ui.Panels + 2);
            int sp = lo.SkillPoints;
            int cost = !learned ? node?.skillPointCost ?? 1 : rank < maxRank ? lo.GetUpgradeCost(selectedSkillId, rank) : 0;
            bool canAct = !learned ? lo.CanUnlockSkill(selectedSkillId, hero.level, hero.classDef, out _) : rank < maxRank && sp >= cost;
            Ui.Label(page, cost > 0 ? $"{cost} SP" : "-", x, BodyBottom - 25, Palette.Gold1);
            Ui.Label(page, cost > 0 ? $"{sp} › {sp - cost} LEFT" : "MAX RANK", x + 34, BodyBottom - 23, Palette.Mist2, font: FontKind.Interface);
            string label = !learned ? "Learn" : rank < maxRank ? "Upgrade" : "Maxed";
            menu.Add(Ui.Button(page, label, x + w - 64, BodyBottom - 28, 64, !learned ? (Action)Learn : Upgrade, 20, interactable: canAct, showCursor: false, style: ButtonStyle.Primary));
            menu.Add(Ui.Button(page, confirmRespecActive ? "Confirm?" : "Respec SP", x, BodyBottom - 102, 70, Respec, 16, showCursor: false, style: ButtonStyle.Flat));
        }

        int Row(int x, int y, int w, string label, string value)
        {
            Ui.Label(page, label, x, y + 2, Palette.Mist2, font: FontKind.Interface);
            Ui.Label(page, value, x + w, y + 2, Palette.Mist1, Align.Right, font: FontKind.Interface);
            Ui.Rect(page, x, y + 12, w, 1, Palette.Ink3, Ui.Panels + 2);
            return y + 15;
        }

        void Check(int x, ref int y, bool ok, string text)
        {
            Ui.Label(page, PixelFont.Fit($"{(ok ? "✓" : "X")} {text}".ToUpperInvariant(), 170), x, y, ok ? Palette.Ok : Palette.Err, font: FontKind.Interface);
            y += 11;
        }

        // ---------------------------------------------------------------- actions

        void Say(string text, bool bad)
        {
            feedback = text;
            feedbackBad = bad;
            Sfx.Play(bad ? Sound.Deny : Sound.Confirm);
            Rebuild();
        }

        void Learn()
        {
            var hero = CurrentHero;
            if (hero.skillLoadout.TryUnlockSkill(selectedSkillId, hero.level, hero.classDef, out string error)) Say($"LEARNED {NameOf(selectedSkillId, isSelectedPassive)}.", false);
            else Say(error ?? "CANNOT LEARN THIS SKILL.", true);
        }

        void Upgrade()
        {
            var hero = CurrentHero;
            if (hero.skillLoadout.TryUpgradeRank(selectedSkillId, out string error)) Say($"UPGRADED TO RANK {hero.skillLoadout.GetRank(selectedSkillId)}.", false);
            else Say(error ?? "CANNOT UPGRADE RANK.", true);
        }

        void Equip(int slot)
        {
            var lo = CurrentHero.skillLoadout;
            string err;
            bool ok = isSelectedPassive ? lo.TryEquipPassive(slot, selectedSkillId, out err) : lo.TryEquipActive(slot, selectedSkillId, out err);
            Say(ok ? $"EQUIPPED IN SLOT {slot + 1}." : err ?? "FAILED TO EQUIP.", !ok);
        }

        void Unequip()
        {
            var lo = CurrentHero.skillLoadout;
            var slots = isSelectedPassive ? lo.PassiveSlots : lo.ActiveSlots;
            for (int i = 0; i < slots.Length; i++) if (slots[i] == selectedSkillId) slots[i] = null;
            Say("SKILL UNEQUIPPED.", false);
        }

        void Respec()
        {
            var hero = CurrentHero;
            if (!confirmRespecActive) { confirmRespecActive = true; Say("PRESS RESPEC AGAIN TO REFUND ALL SP.", false); return; }
            hero.skillLoadout.Respec(hero.classDef, hero.level);
            confirmRespecActive = false;
            selectedSkillId = null;
            Say("ALL SP REFUNDED AND THE TREE RESET.", false);
        }
    }
}
