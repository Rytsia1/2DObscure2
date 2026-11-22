using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Training Ground / Skill Tree — migrated to Figma 16---Training-Ground---Skill-Tree.uxml (1280x720 UI Toolkit).
    public class SkillTreeScreen : GameScreen
    {
        readonly GameState returnState;
        readonly List<Unit> party;
        int heroIndex;

        public Unit CurrentHero => party[Math.Clamp(heroIndex, 0, party.Count - 1)];
        static readonly int[] TierLevel = { 1, 4, 7, 10, 13 };

        UitkMenu menu;
        string selectedSkillId;
        bool isSelectedPassive;
        bool confirmRespecActive;
        string feedback;
        bool feedbackBad;
        int focusIndex;
        bool rebuilding;
        readonly Dictionary<UitkMenu.Item, int> portraitItems = new Dictionary<UitkMenu.Item, int>();

        class Node { public string id; public bool passive; public int tier; public int row; public float x, y; }

        public SkillTreeScreen() : this(null, 0, null) { }

        public SkillTreeScreen(IEnumerable<Unit> partyUnits = null, int initialHero = 0, GameState? backTo = null)
        {
            if (partyUnits != null) party = partyUnits.ToList();
            else if (Game.I?.Run?.Party != null) party = Game.I.Run.Party;
            else if (Game.I?.Roster != null) party = Game.I.Roster.GetActiveParty().Select(d => new Unit(d)).ToList();
            else party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            heroIndex = Math.Clamp(partyUnits == null && initialHero == 0 ? PartyScreen.SelectedIndex : initialHero, 0, Math.Max(0, party.Count - 1));
            returnState = backTo ?? ReturnTarget();
        }

        protected override void Build()
        {
            MountFigma("16---Training-Ground---Skill-Tree");
            Rebuild();
        }

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

            const float colW = 161f;
            const float topY = 48f, spanH = 446f;
            foreach (var g in list.GroupBy(n => n.tier))
            {
                var ordered = g.OrderBy(n => n.passive).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].row = i;
                    ordered[i].x = (g.Key - 1) * colW + colW / 2f;
                    ordered[i].y = topY + spanH * (i + 1) / (ordered.Count + 1);
                }
            }
            return list;
        }

        bool Unlocked(Node n) => n.passive ? CurrentHero.skillLoadout.UnlockedPassiveSkills.Contains(n.id) : CurrentHero.skillLoadout.UnlockedActiveSkills.Contains(n.id);
        bool Available(Node n) => !Unlocked(n) && CurrentHero.skillLoadout.CanUnlockSkill(n.id, CurrentHero.level, CurrentHero.classDef, out _);

        void SwitchHero(int delta)
        {
            if (party == null || party.Count == 0) return;
            bool wasPortraitFocused = menu != null && menu.Current != null && portraitItems.ContainsKey(menu.Current);
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            if (wasPortraitFocused)
                focusIndex = FindPortraitFocusIndex(heroIndex, focusIndex);
            selectedSkillId = null;
            feedback = null;
            Rebuild();
        }

        int FindPortraitFocusIndex(int targetHero, int fallback)
        {
            foreach (var kv in portraitItems)
                if (kv.Value == targetHero) return menu.items.IndexOf(kv.Key);
            return fallback;
        }

        static string NameOf(string id, bool passive) =>
            passive ? (Database.Passives.TryGetValue(id, out var p) ? p.name : id) : (Database.Skills.TryGetValue(id, out var s) ? s.name : id);

        static string Initials(string name)
        {
            var w = (name ?? "?").Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (w.Length == 0) return "?";
            return (w.Length >= 2 ? $"{w[0][0]}{w[1][0]}" : w[0].Substring(0, Math.Min(2, w[0].Length))).ToUpperInvariant();
        }

        void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                UiRoot = FigmaUi.Mount(Root.gameObject, "16---Training-Ground---Skill-Tree");
                FigmaUi.DestroyMenu(ref menu);
                portraitItems.Clear();
                menu = Root.gameObject.AddComponent<UitkMenu>();
                menu.AttachRoot(UiRoot);
                menu.onBack = () => Game?.Back(returnState);
                menu.onFocus = s =>
                {
                    if (rebuilding || s == null) return;
                    focusIndex = menu.items.IndexOf(s);
                };
                menu.onPrevPage = () => SwitchHero(-1);
                menu.onNextPage = () => SwitchHero(1);

                var nodes = LayoutNodes();
                if (selectedSkillId == null && nodes.Count > 0) { selectedSkillId = nodes[0].id; isSelectedPassive = nodes[0].passive; }

                BuildHeader();

                var screen = UiRoot.Q<VisualElement>(className: "_16-training-ground-skill-tree") ?? UiRoot;
                var pLeft = UiRoot.Q<VisualElement>(className: "panel-default__i-27");
                var pRight = UiRoot.Q<VisualElement>(className: "panel-gold__i-12");
                if (pLeft != null)
                {
                    int i1 = screen.IndexOf(pLeft);
                    for (int i = i1 + 1; i < screen.childCount; i++)
                    {
                        if (screen[i] == pRight) continue;
                        screen[i].style.display = DisplayStyle.None;
                    }
                }

                BuildTree(screen, nodes);
                BuildDetail(screen);

                if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
            }
            finally { rebuilding = false; }
        }

        void BuildHeader()
        {
            var hero = CurrentHero;
            var backBtn = FigmaUi.FindButton(UiRoot, "Back");
            if (backBtn != null) FigmaUi.ApplyDefaultFocusVisual(menu.Add(backBtn, "Back", () => Game?.Back(returnState)));

            string[] frames = { "frame-vars-22", "frame-vars-23", "frame-vars-24", "frame-vars-25" };
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
                    FigmaUi.SetBackgroundContain(fr[0], FigmaUi.GetSpriteTexture(u.sprite, "sk_hdr_" + (u.classDef?.id ?? u.name)));
                var it = menu.Add(fr, u.name, () => { if (heroIndex != idx) { heroIndex = idx; selectedSkillId = null; feedback = null; Rebuild(); } }, selected: on, onAdjust: d => SwitchHero(d));
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

            var heroLbl = UiRoot.Q<UiLabel>(className: "knight-knight-lv-1-vars");
            if (heroLbl != null)
            {
                heroLbl.text = $"{hero.name} · {hero.classDef?.name} · Lv {hero.level}";
                heroLbl.style.fontSize = 16;
                heroLbl.style.maxWidth = 300;
                heroLbl.style.overflow = Overflow.Hidden;
            }

            var talBtn = FigmaUi.FindButton(UiRoot, "Talents ›");
            if (talBtn != null)
            {
                FigmaUi.AnchorRight(talBtn, 212f);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(talBtn, "Talents", () => Game?.Go(GameState.Talents)));
            }

            var spVal = UiRoot.Q<UiLabel>(className: "_0-vars-11");
            if (spVal != null)
            {
                spVal.text = hero.skillLoadout.SkillPoints.ToString();
                spVal.style.left = StyleKeyword.Auto;
                spVal.style.right = 10;
                spVal.style.width = 48;
                spVal.style.unityTextAlign = TextAnchor.MiddleRight;
            }
        }

        void BuildTree(VisualElement screen, List<Node> nodes)
        {
            var hero = CurrentHero;
            var host = new VisualElement { style = { position = Position.Absolute, left = 40, top = 84, width = 806, height = 602, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);

            var topBar = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, flexShrink = 0 } };
            var hdr = FigmaUi.CreateSectionHeader($"{hero.classDef?.name ?? "Class"} skill tree", 806f);
            hdr.Add(new UiLabel("□ ACTIVE   ○ PASSIVE") { style = { position = Position.Absolute, right = 0, top = 4, fontSize = 13, color = Palette.Mist2 } });
            topBar.Add(hdr);
            host.Add(topBar);

            var canvas = new VisualElement { style = { flexGrow = 1, marginTop = 8, marginBottom = 8, overflow = Overflow.Hidden } };
            host.Add(canvas);

            int maxTier = SkillLoadout.GetMaxUnlockedTier(hero.level);
            const float colW = 161f;
            for (int t = 1; t <= 5; t++)
            {
                bool open = t <= maxTier;
                var band = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = (t - 1) * colW, top = 0, width = colW, bottom = 0,
                        backgroundColor = open ? Color.clear : Palette.Hex(0x0F1529),
                        borderRightWidth = t < 5 ? 2 : 0, borderRightColor = Palette.Ink3,
                        alignItems = UnityEngine.UIElements.Align.Center, paddingTop = 8,
                    }
                };
                band.Add(new UiLabel($"TIER {t}") { style = { fontSize = 14, color = open ? Palette.Gold1 : Palette.Mist3 } });
                band.Add(new UiLabel(open ? $"LV {TierLevel[t - 1]}" : $"LV {TierLevel[t - 1]} · LOCKED") { style = { fontSize = 12, color = open ? Palette.Gold2 : Palette.Mist3 } });
                canvas.Add(band);
            }

            // Connection lines via Painter2D using actual skill prerequisites
            var edgeLayer = new VisualElement { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0 } };
            edgeLayer.generateVisualContent += ctx =>
            {
                var p = ctx.painter2D;
                p.lineWidth = 2.5f;
                var byId = nodes.ToDictionary(x => x.id, x => x);
                foreach (var b in nodes)
                {
                    var nodeMeta = hero.classDef?.skillUnlockProgression?.FirstOrDefault(u => u.skillId == b.id);
                    string[] prereqs = nodeMeta?.prerequisites;
                    if ((prereqs == null || prereqs.Length == 0) && !b.passive && Database.Skills.TryGetValue(b.id, out var skDef))
                        prereqs = skDef.prerequisites;

                    var targets = new List<Node>();
                    if (prereqs != null && prereqs.Length > 0)
                    {
                        foreach (var pid in prereqs)
                            if (!string.IsNullOrEmpty(pid) && byId.TryGetValue(pid, out var pa))
                                targets.Add(pa);
                    }

                    foreach (var a in targets)
                    {
                        var pStart = new Vector2(a.x + 32f, a.y);
                        var pEnd = new Vector2(b.x - 32f, b.y);
                        float dist = Vector2.Distance(pStart, pEnd);
                        if (dist < 4f) continue;

                        bool ua = Unlocked(a), ub = Unlocked(b);
                        p.strokeColor = ua && ub ? Palette.Gold3 : ua && Available(b) ? Palette.Mist1 : Palette.Ink5;
                        if (ua && ub)
                        {
                            p.BeginPath(); p.MoveTo(pStart); p.LineTo(pEnd); p.Stroke();
                        }
                        else
                        {
                            for (float d = 0f; d < dist; d += 12f)
                            {
                                float d2 = Mathf.Min(d + 7f, dist);
                                p.BeginPath();
                                p.MoveTo(Vector2.Lerp(pStart, pEnd, d / dist));
                                p.LineTo(Vector2.Lerp(pStart, pEnd, d2 / dist));
                                p.Stroke();
                            }
                        }
                    }
                }
            };
            canvas.Add(edgeLayer);

            foreach (var n in nodes) SpawnSkillNode(canvas, n);

            // Bottom Loadout strip
            var strip = new VisualElement { style = { height = 48, borderTopWidth = 2, borderTopColor = Palette.Ink4, flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
            var lo = hero.skillLoadout;
            strip.Add(new UiLabel("LOADOUT") { style = { fontSize = 13, color = Palette.Mist3, marginRight = 18 } });
            strip.Add(new UiLabel($"ACTIVE {lo.ActiveSlots.Count(s => !string.IsNullOrEmpty(s))}/3") { style = { fontSize = 13, color = Palette.Mist2, marginRight = 10 } });
            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
            {
                string id = lo.ActiveSlots[i];
                var tag = FigmaUi.InstantiateTemplate("Tag-Gold");
                if (tag != null)
                {
                    FigmaUi.SetInstanceText(tag, "text-0", string.IsNullOrEmpty(id) ? "--" : Initials(NameOf(id, false)));
                    tag.style.marginRight = 6;
                    if (string.IsNullOrEmpty(id)) tag.style.opacity = 0.45f;
                    strip.Add(tag);
                }
            }
            string passText = string.Join(" ", Enumerable.Range(0, SkillLoadout.PassiveSlotCount).Select(i =>
            {
                string id = lo.PassiveSlots[i];
                return string.IsNullOrEmpty(id) ? "◇" : $"◆{Initials(NameOf(id, true))}";
            }));
            strip.Add(new UiLabel($"PASSIVE {lo.PassiveSlots.Count(s => !string.IsNullOrEmpty(s))}/3   {passText}") { style = { fontSize = 14, color = Palette.Mist2, marginLeft = 20 } });
            host.Add(strip);
        }

        void SpawnSkillNode(VisualElement canvas, Node n)
        {
            var hero = CurrentHero;
            bool learned = Unlocked(n), avail = Available(n), sel = n.id == selectedSkillId;
            float size = 60f;
            var el = FigmaUi.InstantiateTemplate(learned ? "Map-Node-Current" : avail ? "Map-Node-Choosable" : "Map-Node-Locked") ?? new VisualElement();
            el.style.position = Position.Absolute;
            el.style.left = n.x - size / 2f;
            el.style.top = n.y - size / 2f;
            el.style.width = size;
            el.style.height = size;
            el.style.justifyContent = Justify.Center;
            el.style.alignItems = UnityEngine.UIElements.Align.Center;
            if (n.passive)
            {
                el.style.borderTopLeftRadius = 24; el.style.borderTopRightRadius = 24;
                el.style.borderBottomLeftRadius = 24; el.style.borderBottomRightRadius = 24;
            }
            var nodeIcon = IconArt.SkillOrPassive(n.id, n.passive);
            FigmaUi.SetInstanceText(el, "text-0", nodeIcon != null ? "" : Initials(NameOf(n.id, n.passive)));
            IconArt.Overlay(el, nodeIcon, 3, learned || avail ? 1f : 0.45f);
            canvas.Add(el);

            if (learned)
            {
                int max = n.passive ? (Database.Passives.TryGetValue(n.id, out var p) ? p.maxRank : 3) : (Database.Skills.TryGetValue(n.id, out var s) ? s.maxRank : 3);
                int rank = hero.skillLoadout.GetRank(n.id);
                float totalW = max * 12f - 4f;
                var pips = new VisualElement { style = { position = Position.Absolute, left = n.x - totalW / 2f, top = n.y + size / 2f + 4, flexDirection = FlexDirection.Row } };
                for (int i = 0; i < max; i++)
                    pips.Add(new VisualElement { style = { width = 8, height = 6, marginRight = i < max - 1 ? 4 : 0, backgroundColor = i < rank ? Palette.Gold2 : Palette.Ink5 } });
                canvas.Add(pips);
            }

            var it = menu.Add(el, NameOf(n.id, n.passive), () =>
            {
                selectedSkillId = n.id; isSelectedPassive = n.passive; confirmRespecActive = false; feedback = null; Rebuild();
            }, selected: sel);
            it.visual = on =>
            {
                var b = on ? Palette.Hex(0xFFF4D2) : sel ? Palette.Gold1 : learned ? Palette.Gold2 : avail ? Palette.Mist1 : Palette.Ink5;
                el.style.borderTopColor = b; el.style.borderBottomColor = b; el.style.borderLeftColor = b; el.style.borderRightColor = b;
                el.style.backgroundColor = on ? Palette.Hex(0x382D14) : sel ? Palette.Hex(0x2B2210) : StyleKeyword.Null;
            };
            it.visual(false);
        }

        void BuildDetail(VisualElement screen)
        {
            var hero = CurrentHero;
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 602, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);
            if (string.IsNullOrEmpty(selectedSkillId)) { host.Add(FigmaUi.CreateSectionHeader("Select a skill", 346f)); return; }

            SkillDef a = null; PassiveDef pv = null;
            if (isSelectedPassive) Database.Passives.TryGetValue(selectedSkillId, out pv);
            else Database.Skills.TryGetValue(selectedSkillId, out a);
            string name = a?.name ?? pv?.name ?? selectedSkillId;
            int tier = a?.tier ?? pv?.tier ?? 1, maxRank = a?.maxRank ?? pv?.maxRank ?? 3;
            var lo = hero.skillLoadout;
            bool learned = isSelectedPassive ? lo.UnlockedPassiveSkills.Contains(selectedSkillId) : lo.UnlockedActiveSkills.Contains(selectedSkillId);
            int rank = lo.GetRank(selectedSkillId);
            var node = hero.classDef?.skillUnlockProgression?.FirstOrDefault(n => n.skillId == selectedSkillId);

            host.Add(FigmaUi.CreateSectionHeader(name, 346f));

            var detailScroll = FigmaUi.CreateVerticalScrollView();
            detailScroll.style.flexGrow = 1;
            detailScroll.style.flexBasis = 0;
            detailScroll.style.minHeight = 0;
            detailScroll.style.marginTop = 8;
            detailScroll.style.marginBottom = 8;
            host.Add(detailScroll);

            var tagRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 10 } };
            var tType = FigmaUi.InstantiateTemplate("Tag-Cls"); if (tType != null) { FigmaUi.SetInstanceText(tType, "text-0", isSelectedPassive ? "PASSIVE" : "ACTIVE"); tType.style.marginRight = 6; tagRow.Add(tType); }
            var tTier = FigmaUi.InstantiateTemplate("Tag-Cls"); if (tTier != null) { FigmaUi.SetInstanceText(tTier, "text-0", $"TIER {tier}"); tTier.style.marginRight = 6; tagRow.Add(tTier); }
            if (a != null)
            {
                string code = Palette.ElementCode(a.element);
                var chip = FigmaUi.InstantiateTemplate($"Element-Chip-{code}");
                if (chip != null) { FigmaUi.SetInstanceText(chip, "text-0", code); chip.style.marginRight = 6; tagRow.Add(chip); }
                tagRow.Add(new UiLabel(a.element.ToString().ToUpperInvariant()) { style = { fontSize = 13, color = Palette.Mist2 } });
            }
            detailScroll.Add(tagRow);

            detailScroll.Add(new UiLabel(((a?.description ?? pv?.description) ?? "").ToUpperInvariant()) { style = { fontSize = 13, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 12 } });

            VisualElement Line(string l, string r)
            {
                var v = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, height = 26, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 } };
                v.Add(new UiLabel(l) { style = { fontSize = 13, color = Palette.Mist2 } });
                v.Add(new UiLabel(r) { style = { fontSize = 13, color = Palette.Mist1 } });
                return v;
            }
            int shown = Math.Max(1, rank);
            if (a != null)
            {
                detailScroll.Add(Line("MP COST", a.GetMpCost(shown).ToString()));
                detailScroll.Add(Line("TARGET", Palette.FormatTarget(a.target)));
                detailScroll.Add(Line("POWER", $"{a.GetPower(shown):0.##}X"));
                if (a.cooldown > 0) detailScroll.Add(Line("COOLDOWN", $"{a.cooldown} {(a.cooldown == 1 ? "TURN" : "TURNS")}"));
            }
            detailScroll.Add(Line("RANK", learned ? $"{rank} / {maxRank}" : $"LOCKED · MAX {maxRank}"));

            if (a?.apply != null && a.apply.Length > 0)
            {
                detailScroll.Add(new UiLabel("STATUS EFFECTS") { style = { fontSize = 12, color = Palette.Mist3, marginTop = 8, marginBottom = 4 } });
                var statusRow = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = UnityEngine.UIElements.Align.Center } };
                foreach (var ap in a.apply)
                {
                    var chip = IconArt.StatusChip(ap.id, StatusInfo.ChipText(ap), 1, 5f);
                    if (chip != null) statusRow.Add(chip);
                }
                detailScroll.Add(statusRow);
            }

            detailScroll.Add(new UiLabel("REQUIREMENTS") { style = { fontSize = 12, color = Palette.Mist3, marginTop = 10, marginBottom = 4 } });
            int req = node?.requiredLevel ?? TierLevel[Mathf.Clamp(tier - 1, 0, 4)];
            bool lvOk = hero.level >= req;
            detailScroll.Add(new UiLabel($"{(lvOk ? "✓" : "X")} LEVEL {req} · {hero.name.ToUpperInvariant()} IS LV {hero.level}") { style = { fontSize = 12, color = lvOk ? Palette.Ok : Palette.Err, whiteSpace = WhiteSpace.Normal } });
            var allPre = (node?.prerequisites ?? Array.Empty<string>()).Concat(a?.prerequisites ?? Array.Empty<string>()).Distinct();
            foreach (var pre in allPre)
            {
                bool met = lo.UnlockedActiveSkills.Contains(pre) || lo.UnlockedPassiveSkills.Contains(pre);
                detailScroll.Add(new UiLabel($"{(met ? "✓" : "X")} {NameOf(pre, !Database.Skills.ContainsKey(pre)).ToUpperInvariant()} LEARNED") { style = { fontSize = 12, color = met ? Palette.Ok : Palette.Err, whiteSpace = WhiteSpace.Normal } });
            }

            var utilRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8, flexShrink = 0 } };
            var respecBtn = FigmaUi.CreateButton(confirmRespecActive ? "Confirm?" : "Respec SP", confirmRespecActive ? "button-danger" : "button-slab", 160f);
            utilRow.Add(respecBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(respecBtn, "Respec SP", Respec));

            bool equipped = isSelectedPassive ? lo.PassiveSlots.Contains(selectedSkillId) : lo.ActiveSlots.Contains(selectedSkillId);
            if (learned)
            {
                var unBtn = FigmaUi.CreateButton("Unequip", "button-slab", 160f);
                unBtn.SetEnabled(equipped);
                utilRow.Add(unBtn);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(unBtn, "Unequip", Unequip, interactable: equipped));
            }
            host.Add(utilRow);

            if (learned)
            {
                var eqRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 8, flexShrink = 0 } };
                eqRow.Add(new UiLabel(equipped ? "EQUIPPED" : "EQUIP TO") { style = { fontSize = 12, color = Palette.Mist3, width = 92 } });
                var slotArr = isSelectedPassive ? lo.PassiveSlots : lo.ActiveSlots;
                for (int i = 0; i < 3; i++)
                {
                    int slot = i;
                    bool inThisSlot = slot < slotArr.Length && slotArr[slot] == selectedSkillId;
                    var sBtn = FigmaUi.CreateCompactButton((i + 1).ToString(), 52f, inThisSlot ? "button-primary" : "button-slab");
                    sBtn.style.marginRight = 8;
                    eqRow.Add(sBtn);
                    FigmaUi.ApplyDefaultFocusVisual(menu.Add(sBtn, $"Slot {i + 1}", () => Equip(slot), selected: inThisSlot));
                }
                host.Add(eqRow);
            }

            if (!string.IsNullOrEmpty(feedback))
                host.Add(new UiLabel(feedback.ToUpperInvariant()) { style = { fontSize = 12, color = feedbackBad ? Palette.Err : Palette.Ok, whiteSpace = WhiteSpace.Normal, marginBottom = 8, flexShrink = 0 } });

            int sp = lo.SkillPoints;
            int cost = !learned ? node?.skillPointCost ?? 1 : rank < maxRank ? lo.GetUpgradeCost(selectedSkillId, rank) : 0;
            bool canAct = !learned ? lo.CanUnlockSkill(selectedSkillId, hero.level, hero.classDef, out _) : rank < maxRank && sp >= cost;
            var foot = new VisualElement { style = { borderTopWidth = 2, borderTopColor = Palette.Ink4, paddingTop = 10, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, flexShrink = 0 } };
            foot.Add(new UiLabel(cost > 0 ? $"{cost} SP" : "-") { style = { fontSize = 22, color = Palette.Gold1 } });
            var (spText, spShort) = SpPreview(sp, cost);
            foot.Add(new UiLabel(spText) { style = { fontSize = 13, color = spShort ? Palette.Err : Palette.Mist2 } });
            string actLbl = !learned ? "Learn" : rank < maxRank ? "Upgrade" : "Maxed";
            var actBtn = FigmaUi.CreateButton(actLbl, canAct ? "button-primary" : "button-slab", 136f);
            actBtn.SetEnabled(canAct);
            foot.Add(actBtn);
            host.Add(foot);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(actBtn, actLbl, !learned ? (Action)Learn : Upgrade, interactable: canAct));
        }

        // Footer next to the SP cost: what's left after paying, or how many more SP are needed (never a negative balance).
        public static (string text, bool isShort) SpPreview(int sp, int cost) =>
            cost <= 0 ? ("MAX RANK", false)
            : sp < cost ? ($"NEED {cost - sp} MORE SP", true)
            : ($"{sp} › {sp - cost} LEFT", false);

        void Say(string text, bool bad, Sound okSound = Sound.SkillUnlock) { feedback = text; feedbackBad = bad; Sfx.Play(bad ? Sound.Deny : okSound); Rebuild(); }
        void Learn() { var h = CurrentHero; if (h.skillLoadout.TryUnlockSkill(selectedSkillId, h.level, h.classDef, out string e)) Say($"LEARNED {NameOf(selectedSkillId, isSelectedPassive)}.", false); else Say(e ?? "CANNOT LEARN.", true); }
        void Upgrade() { var h = CurrentHero; if (h.skillLoadout.TryUpgradeRank(selectedSkillId, out string e)) Say($"UPGRADED TO RANK {h.skillLoadout.GetRank(selectedSkillId)}.", false); else Say(e ?? "CANNOT UPGRADE.", true); }
        void Equip(int slot) { var lo = CurrentHero.skillLoadout; bool ok = isSelectedPassive ? lo.TryEquipPassive(slot, selectedSkillId, out var e) : lo.TryEquipActive(slot, selectedSkillId, out e); Say(ok ? $"EQUIPPED IN SLOT {slot + 1}." : e ?? "FAILED.", !ok, Sound.Equip); }
        void Unequip() { var lo = CurrentHero.skillLoadout; var s = isSelectedPassive ? lo.PassiveSlots : lo.ActiveSlots; for (int i = 0; i < s.Length; i++) if (s[i] == selectedSkillId) s[i] = null; Say("SKILL UNEQUIPPED.", false, Sound.Equip); }
        void Respec() { if (!confirmRespecActive) { confirmRespecActive = true; Say("PRESS RESPEC AGAIN TO REFUND ALL SP.", false, Sound.ModalOpen); return; } CurrentHero.skillLoadout.Respec(CurrentHero.classDef, CurrentHero.level); confirmRespecActive = false; selectedSkillId = null; Say("ALL SP REFUNDED AND THE TREE RESET.", false); }
    }
}
