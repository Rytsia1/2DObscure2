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
        public enum TrainingView { Skills, Attributes }
        public TrainingView CurrentView { get; private set; } = TrainingView.Skills;
        public AttributeAllocationDraft CurrentDraft => EnsureDraft();
        static readonly int[] TierLevel = { 1, 4, 7, 10, 13 };
        static readonly int[] ActiveApMilestones = { 4, 7, 10, 13 };

        UitkMenu menu;
        string selectedSkillId;
        bool isSelectedPassive;
        bool confirmRespecActive;
        Ability selectedAbility = Ability.STR;
        AttributeAllocationDraft draft;
        string feedback;
        bool feedbackBad;
        int focusIndex;
        bool rebuilding;
        readonly Dictionary<UitkMenu.Item, int> portraitItems = new Dictionary<UitkMenu.Item, int>();

        // Hub and Campfire (Rest Site) only — matches Skill Tree / Loadout rules in GDD.
        public static bool CanAllocateAttributesAt(Game game, RunState run)
        {
            if (game == null) return true;
            if (game.LastBase == GameState.Hub || game.LastBase == GameState.MainMenu) return true;
            return run != null && run.Current != null && run.Current.type == NodeType.Rest && !run.OnMap;
        }

        public bool CanAllocateAttributesNow => CanAllocateAttributesAt(Game, Run);

        AttributeAllocationDraft EnsureDraft()
        {
            var hero = CurrentHero;
            if (draft == null || draft.Unit != hero)
                draft = new AttributeAllocationDraft(hero, Database.AsiConfig);
            return draft;
        }

        public void ShowView(TrainingView view)
        {
            if (CurrentView == view) return;
            draft?.Reset();
            CurrentView = view;
            confirmRespecActive = false;
            feedback = null;
            focusIndex = 0;
            Rebuild();
        }

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

        int EquippedSlotIndex(string id, bool passive)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            var slots = passive ? CurrentHero.skillLoadout.PassiveSlots : CurrentHero.skillLoadout.ActiveSlots;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == id) return i;
            return -1;
        }

        void SwitchHero(int delta)
        {
            if (party == null || party.Count == 0) return;
            bool wasPortraitFocused = menu != null && menu.Current != null && portraitItems.ContainsKey(menu.Current);
            heroIndex = (heroIndex + delta + party.Count) % party.Count;
            if (wasPortraitFocused)
                focusIndex = FindPortraitFocusIndex(heroIndex, focusIndex);
            selectedSkillId = null;
            draft = null;
            confirmRespecActive = false;
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

                EnsureDraft();
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

                if (CurrentView == TrainingView.Attributes)
                {
                    BuildAttributesPanel(screen);
                    BuildAttributesDetail(screen);
                }
                else
                {
                    BuildTree(screen, nodes);
                    BuildDetail(screen);
                }

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
                var it = menu.Add(fr, u.name, () => { if (heroIndex != idx) { heroIndex = idx; selectedSkillId = null; draft = null; confirmRespecActive = false; feedback = null; Rebuild(); } }, selected: on, onAdjust: d => SwitchHero(d), tooltip: $"{u.name.ToUpperInvariant()} · {u.skillLoadout.SkillPoints} SP · {u.unspentAsiPoints} AP");
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
                string cls = hero.classDef?.name;
                heroLbl.text = string.IsNullOrEmpty(cls) || string.Equals(hero.name?.Trim(), cls.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? $"{hero.name} · Lv {hero.level}"
                    : $"{hero.name} · {cls} · Lv {hero.level}";
                heroLbl.style.fontSize = 16;
                heroLbl.style.maxWidth = 205;
                heroLbl.style.overflow = Overflow.Hidden;
                if (switcherRow != null)
                {
                    FigmaUi.OnFirstLayout(switcherRow, () =>
                    {
                        float minLeft = switcherRow.layout.xMax + 14f;
                        if (minLeft > heroLbl.resolvedStyle.left) heroLbl.style.left = minLeft;
                    });
                }
            }

            var talBtn = FigmaUi.FindButton(UiRoot, "Talents ›");
            bool inAttr = CurrentView == TrainingView.Attributes;
            int apCount = hero.unspentAsiPoints;
            string modeLabel = inAttr ? "‹ Skill Tree" : apCount > 0 ? $"Attributes ({apCount})" : "Attributes";
            var modeBtn = FigmaUi.CreateButton(modeLabel, "button-slab", 150f);
            modeBtn.name = "TrainingModeButton";
            modeBtn.style.position = Position.Absolute;
            modeBtn.style.top = 18;
            modeBtn.style.height = 36;
            modeBtn.style.paddingLeft = 10;
            modeBtn.style.paddingRight = 10;
            var modeLbl = modeBtn.Q<UiLabel>("uitkLabel");
            if (modeLbl != null)
            {
                modeLbl.style.fontSize = 16;
                if (!inAttr && apCount > 0) modeLbl.style.color = Palette.Gold1;
            }
            if (!inAttr && apCount > 0)
            {
                modeBtn.style.borderTopColor = Palette.Gold2;
                modeBtn.style.borderBottomColor = Palette.Gold2;
                modeBtn.style.borderLeftColor = Palette.Gold2;
                modeBtn.style.borderRightColor = Palette.Gold2;
            }
            if (talBtn?.parent != null)
                talBtn.parent.Insert(talBtn.parent.IndexOf(talBtn), modeBtn);
            else
                (UiRoot.Q<VisualElement>(className: "_16-training-ground-skill-tree") ?? UiRoot).Add(modeBtn);
            FigmaUi.AnchorRight(modeBtn, 352f);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(modeBtn, inAttr ? "Skill Tree" : "Attributes",
                () => ShowView(inAttr ? TrainingView.Skills : TrainingView.Attributes),
                tooltip: inAttr ? "RETURN TO CLASS SKILL TREE" : "ALLOCATE ATTRIBUTE POINTS (AP)"));

            if (talBtn != null)
            {
                FigmaUi.AnchorRight(talBtn, 212f);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(talBtn, "Talents", () => Game?.Go(GameState.Talents)));
            }

            var ptsCaption = UiRoot.Q<UiLabel>(className: "skill-points-vars");
            if (ptsCaption != null)
                ptsCaption.text = inAttr ? "ATTR POINTS" : "SKILL POINTS";

            var spVal = UiRoot.Q<UiLabel>(className: "_0-vars-11");
            if (spVal != null)
            {
                int shownPts = inAttr ? EnsureDraft().RemainingPoints : hero.skillLoadout.SkillPoints;
                spVal.text = shownPts.ToString();
                spVal.style.color = shownPts > 0 ? Palette.Gold1 : Palette.Mist2;
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
            hdr.Add(new UiLabel("□ ACTIVE   ○ PASSIVE   ✓ EQUIPPED") { style = { position = Position.Absolute, right = 0, top = 4, fontSize = 13, color = Palette.Mist2 } });
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

            // Bottom Loadout strip — icon-only sockets for Active (square) and Passive (circle) skills
            var strip = new VisualElement();
            strip.AddToClassList("uitk-loadout-strip");
            var lo = hero.skillLoadout;

            var loadoutHdr = new UiLabel("LOADOUT");
            loadoutHdr.AddToClassList("uitk-loadout-heading");
            strip.Add(loadoutHdr);

            var activeLbl = new UiLabel($"ACTIVE {lo.ActiveSlots.Count(s => !string.IsNullOrEmpty(s))}/{SkillLoadout.ActiveSlotCount}");
            activeLbl.AddToClassList("uitk-loadout-group-lbl");
            strip.Add(activeLbl);

            for (int i = 0; i < SkillLoadout.ActiveSlotCount; i++)
                strip.Add(CreateLoadoutSlot(lo.ActiveSlots[i], i, passive: false));

            var passiveLbl = new UiLabel($"PASSIVE {lo.PassiveSlots.Count(s => !string.IsNullOrEmpty(s))}/{SkillLoadout.PassiveSlotCount}");
            passiveLbl.AddToClassList("uitk-loadout-group-lbl");
            passiveLbl.AddToClassList("uitk-loadout-group-lbl-passive");
            strip.Add(passiveLbl);

            for (int i = 0; i < SkillLoadout.PassiveSlotCount; i++)
                strip.Add(CreateLoadoutSlot(lo.PassiveSlots[i], i, passive: true));

            host.Add(strip);
        }

        VisualElement CreateLoadoutSlot(string skillId, int slotIndex, bool passive)
        {
            bool filled = !string.IsNullOrEmpty(skillId);
            bool isSelected = filled && skillId == selectedSkillId && isSelectedPassive == passive;

            var slot = new VisualElement { pickingMode = filled ? PickingMode.Position : PickingMode.Ignore };
            slot.AddToClassList("uitk-loadout-slot");
            if (passive) slot.AddToClassList("uitk-loadout-slot-passive");
            if (filled) slot.AddToClassList("uitk-loadout-slot-filled");
            if (isSelected) slot.AddToClassList("uitk-loadout-slot-selected");

            if (filled)
            {
                var tex = IconArt.SkillOrPassive(skillId, passive);
                if (tex != null)
                {
                    IconArt.Overlay(slot, tex, 2);
                }
                else
                {
                    var fallback = new UiLabel(Initials(NameOf(skillId, passive)));
                    fallback.AddToClassList("uitk-loadout-slot-empty-num");
                    fallback.style.opacity = 1f;
                    fallback.style.color = Palette.Gold1;
                    slot.Add(fallback);
                }

                string capturedId = skillId;
                slot.RegisterCallback<ClickEvent>(_ =>
                {
                    selectedSkillId = capturedId;
                    isSelectedPassive = passive;
                    confirmRespecActive = false;
                    feedback = null;
                    Sfx.Play(Sound.Move);
                    Rebuild();
                });
            }
            else
            {
                var num = new UiLabel((slotIndex + 1).ToString()) { pickingMode = PickingMode.Ignore };
                num.AddToClassList("uitk-loadout-slot-empty-num");
                slot.Add(num);
            }
            return slot;
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
            el.style.overflow = Overflow.Visible;

            int eqSlot = EquippedSlotIndex(n.id, n.passive);
            if (eqSlot >= 0)
            {
                var eqBadge = new UiLabel($"✓{eqSlot + 1}") { pickingMode = PickingMode.Ignore };
                eqBadge.AddToClassList("uitk-skill-equipped-badge");
                el.Add(eqBadge);
            }
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
            int selectedEqSlot = EquippedSlotIndex(selectedSkillId, isSelectedPassive);
            if (selectedEqSlot >= 0)
            {
                var eqPill = new UiLabel($"✓ EQUIPPED · SLOT {selectedEqSlot + 1}");
                eqPill.AddToClassList("uitk-badge-ok");
                eqPill.style.marginRight = 6;
                tagRow.Add(eqPill);
            }
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
                eqRow.Add(new UiLabel(selectedEqSlot >= 0 ? $"✓ SLOT {selectedEqSlot + 1}" : "EQUIP TO")
                {
                    style = { fontSize = 12, color = selectedEqSlot >= 0 ? Palette.Ok : Palette.Mist3, width = 92 }
                });
                var slotArr = isSelectedPassive ? lo.PassiveSlots : lo.ActiveSlots;
                for (int i = 0; i < 3; i++)
                {
                    int slot = i;
                    bool inThisSlot = slot < slotArr.Length && slotArr[slot] == selectedSkillId;
                    var sBtn = FigmaUi.CreateCompactButton(inThisSlot ? $"✓{i + 1}" : (i + 1).ToString(), 52f, inThisSlot ? "button-primary" : "button-slab");
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
        void Equip(int slot)
        {
            if (EquippedSlotIndex(selectedSkillId, isSelectedPassive) == slot)
            {
                Say($"ALREADY EQUIPPED IN SLOT {slot + 1}.", false, Sound.Move);
                return;
            }
            var lo = CurrentHero.skillLoadout;
            bool ok = isSelectedPassive ? lo.TryEquipPassive(slot, selectedSkillId, out var e) : lo.TryEquipActive(slot, selectedSkillId, out e);
            Say(ok ? $"EQUIPPED IN SLOT {slot + 1}." : e ?? "FAILED.", !ok, Sound.Equip);
        }
        void Unequip() { var lo = CurrentHero.skillLoadout; var s = isSelectedPassive ? lo.PassiveSlots : lo.ActiveSlots; for (int i = 0; i < s.Length; i++) if (s[i] == selectedSkillId) s[i] = null; Say("SKILL UNEQUIPPED.", false, Sound.Equip); }
        void Respec() { if (!confirmRespecActive) { confirmRespecActive = true; Say("PRESS RESPEC AGAIN TO REFUND ALL SP.", false, Sound.ModalOpen); return; } CurrentHero.skillLoadout.Respec(CurrentHero.classDef, CurrentHero.level); confirmRespecActive = false; selectedSkillId = null; Say("ALL SP REFUNDED AND THE TREE RESET.", false); }

        // ===================================================================== Attributes View (Hub & Campfire)

        void BuildAttributesPanel(VisualElement screen)
        {
            var hero = CurrentHero;
            var d = EnsureDraft();
            bool editable = CanAllocateAttributesNow;
            var guide = ClassStatGuidance.ForClass(hero.classDef);
            var previewScores = d.PreviewFinalScores();
            var previewMods = d.PreviewModifiers();

            var host = new VisualElement { name = "AttributesLeftPanel", style = { position = Position.Absolute, left = 40, top = 84, width = 806, height = 602, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);

            var hdr = FigmaUi.CreateSectionHeader($"{hero.name} attributes", 806f);
            string rightStatus;
            Color rightCol;
            if (!editable)
            {
                rightStatus = "VIEW ONLY · HUB OR CAMPFIRE ONLY";
                rightCol = Palette.Ember1;
            }
            else if (d.RemainingPoints > 0 || d.HasPendingChanges)
            {
                rightStatus = d.HasPendingChanges
                    ? $"{d.RemainingPoints} AP LEFT · +{d.TotalPending} STAGED"
                    : $"{d.RemainingPoints} AP AVAILABLE";
                rightCol = Palette.Gold1;
            }
            else
            {
                int nextLv = hero.NextAttributeMilestoneLevel();
                rightStatus = nextLv > 0 ? $"NEXT +2 AP AT LV {nextLv}" : "ALL CAP-15 MILESTONES EARNED";
                rightCol = Palette.Mist2;
            }
            hdr.Add(new UiLabel(rightStatus) { style = { position = Position.Absolute, right = 0, top = 4, fontSize = 13, color = rightCol } });
            host.Add(hdr);

            // Milestone strip (Levels 4, 7, 10, 13 under active Level Cap 15)
            var msRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center,
                    justifyContent = Justify.SpaceBetween, height = 36, marginTop = 8, marginBottom = 8,
                    paddingLeft = 10, paddingRight = 10, backgroundColor = Palette.Ink1,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4,
                    flexShrink = 0
                }
            };
            var msLeft = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
            msLeft.Add(new UiLabel("AP MILESTONES") { style = { fontSize = 12, color = Palette.Mist3, marginRight = 12 } });
            foreach (int lv in ActiveApMilestones)
            {
                bool reached = hero.level >= lv;
                var chip = new UiLabel(reached ? $"✓ LV {lv} (+2)" : $"LV {lv} (+2)")
                {
                    style =
                    {
                        fontSize = 12, color = reached ? Palette.Gold1 : Palette.Mist3,
                        marginRight = 10, paddingLeft = 6, paddingRight = 6, paddingTop = 2, paddingBottom = 2,
                        backgroundColor = reached ? Palette.Hex(0x2B2210) : Palette.Ink2,
                        borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                        borderTopColor = reached ? Palette.Gold3 : Palette.Ink4, borderBottomColor = reached ? Palette.Gold3 : Palette.Ink4,
                        borderLeftColor = reached ? Palette.Gold3 : Palette.Ink4, borderRightColor = reached ? Palette.Gold3 : Palette.Ink4,
                    }
                };
                msLeft.Add(chip);
            }
            msRow.Add(msLeft);
            msRow.Add(new UiLabel($"NORMAL CAP {d.Config.maxAbilityScore}") { style = { fontSize = 12, color = Palette.Mist2 } });
            host.Add(msRow);

            // 6 Attribute allocation rows
            var rowsCol = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            host.Add(rowsCol);

            foreach (var a in AbilityScores.AllAbilities)
            {
                var ab = a;
                bool sel = ab == selectedAbility;
                int curFinal = hero.Score(ab);
                int curMod = hero.Mod(ab);
                int prevFinal = previewScores[ab];
                int prevMod = previewMods[ab];
                int pend = d.GetPending(ab);
                int bScore = hero.BaseScores[ab];
                int rScore = hero.RacialModifiers[ab];
                int pScore = hero.PermanentAbilityBonuses[ab];
                int eqScore = hero.EquipmentAbilityBonuses[ab];
                bool atCap = d.IsAtCap(ab);
                bool canDec = editable && d.CanDecrease(ab);
                bool canInc = editable && d.CanIncrease(ab);
                var priority = guide.GetPriority(ab);

                var row = new VisualElement
                {
                    name = $"AttrRow_{ab}",
                    style =
                    {
                        flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center,
                        height = 70, marginBottom = 8, flexShrink = 0, overflow = Overflow.Hidden
                    }
                };

                // Left clickable info block (separate sibling from -/+ buttons so bounds never overlap)
                var infoBox = new VisualElement
                {
                    style =
                    {
                        flexGrow = 1, flexShrink = 1, minWidth = 0, height = 70, marginRight = 8,
                        flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center,
                        paddingLeft = 12, paddingRight = 12,
                        backgroundColor = sel ? Palette.Hex(0x2B2210) : Palette.Ink2,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        borderTopColor = sel ? Palette.Gold2 : Palette.Ink5, borderBottomColor = sel ? Palette.Gold2 : Palette.Ink5,
                        borderLeftColor = sel ? Palette.Gold2 : Palette.Ink5, borderRightColor = sel ? Palette.Gold2 : Palette.Ink5,
                    }
                };

                var leftText = new VisualElement { style = { flexGrow = 1, flexShrink = 1, minWidth = 0, flexDirection = FlexDirection.Column } };
                var topLine = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 4 } };
                topLine.Add(new UiLabel($"{ab} · {AbilityName(ab)}") { style = { fontSize = 16, color = sel ? Palette.Gold1 : Palette.Mist1, marginRight = 8 } });
                if (priority == AbilityPriority.Primary)
                    topLine.Add(MakePill("PRIMARY", Palette.Gold1, Palette.Gold3, Palette.Hex(0x2B2210)));
                else if (priority == AbilityPriority.Secondary)
                    topLine.Add(MakePill("SECONDARY", Palette.Mist2, Palette.Ink5, Palette.Ink1));
                if (atCap)
                    topLine.Add(MakePill($"MAX {d.Config.maxAbilityScore}", Palette.Gold2, Palette.Gold3, Palette.Ink1));
                leftText.Add(topLine);

                string srcText = $"BASE {bScore} · RACE {FmtSigned(rScore)} · PERM {FmtSigned(pScore)}"
                    + (pend > 0 ? $" · STAGED +{pend}" : "")
                    + (eqScore != 0 ? $" · EQP {FmtSigned(eqScore)}" : "");
                leftText.Add(new UiLabel(srcText) { style = { fontSize = 12, color = pend > 0 ? Palette.Ok : Palette.Mist3 } });
                infoBox.Add(leftText);

                var scoreCol = new VisualElement { style = { width = 186, flexShrink = 0, flexDirection = FlexDirection.Column, alignItems = UnityEngine.UIElements.Align.FlexEnd } };
                string scoreTxt = pend > 0 ? $"{curFinal} › {prevFinal}" : curFinal.ToString();
                scoreCol.Add(new UiLabel(scoreTxt) { style = { fontSize = 20, color = pend > 0 ? Palette.Ok : Palette.Mist1 } });
                string modTxt = pend > 0
                    ? (prevMod != curMod
                        ? $"MOD {AbilityScores.FormatMod(curMod)} › {AbilityScores.FormatMod(prevMod)}"
                        : $"MOD {AbilityScores.FormatMod(curMod)} (ODD SCORE)")
                    : $"MOD {AbilityScores.FormatMod(curMod)}";
                Color modCol = pend > 0 ? (prevMod != curMod ? Palette.Ok : Palette.Gold2) : Palette.Mist2;
                scoreCol.Add(new UiLabel(modTxt) { style = { fontSize = 12, color = modCol } });
                infoBox.Add(scoreCol);

                row.Add(infoBox);

                var rowItem = menu.Add(infoBox, $"{ab} {AbilityName(ab)}", () =>
                {
                    if (selectedAbility != ab) { selectedAbility = ab; feedback = null; Rebuild(); }
                }, selected: sel, tooltip: hero.ExplainAbility(ab));
                rowItem.visual = on =>
                {
                    infoBox.style.backgroundColor = on || sel ? Palette.Hex(0x2B2210) : Palette.Ink2;
                    var b = on ? Palette.Gold1 : sel ? Palette.Gold2 : Palette.Ink5;
                    infoBox.style.borderTopColor = b; infoBox.style.borderBottomColor = b;
                    infoBox.style.borderLeftColor = b; infoBox.style.borderRightColor = b;
                };
                rowItem.visual(false);

                var decBtn = FigmaUi.CreateCompactButton("-", 46f, "button-slab");
                decBtn.name = $"Dec_{ab}";
                decBtn.style.height = 46;
                decBtn.style.marginRight = 6;
                decBtn.SetEnabled(canDec);
                row.Add(decBtn);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(decBtn, $"- {ab}", () => AdjustPendingAttribute(ab, -1), interactable: canDec, tooltip: $"REMOVE 1 STAGED POINT FROM {ab}"));

                var incBtn = FigmaUi.CreateCompactButton("+", 46f, canInc ? "button-primary" : "button-slab");
                incBtn.name = $"Inc_{ab}";
                incBtn.style.height = 46;
                incBtn.SetEnabled(canInc);
                row.Add(incBtn);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(incBtn, $"+ {ab}", () => AdjustPendingAttribute(ab, +1), interactable: canInc, tooltip: $"STAGE +1 AP INTO {ab}"));

                rowsCol.Add(row);
            }

            host.Add(new UiLabel("1 AP = +1 SCORE · STACKABLE & BANKABLE · CAP 20 (EXPLICIT TEMP BUFFS MAY EXCEED 20).")
            {
                style = { fontSize = 11, color = Palette.Mist3, flexShrink = 0, marginTop = 2 }
            });
        }

        void BuildAttributesDetail(VisualElement screen)
        {
            var hero = CurrentHero;
            var d = EnsureDraft();
            bool editable = CanAllocateAttributesNow;
            var guide = ClassStatGuidance.ForClass(hero.classDef);
            var before = hero.Derived;
            var after = d.PreviewDerived();

            var host = new VisualElement { name = "AttributesDetailPanel", style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 602, flexDirection = FlexDirection.Column, overflow = Overflow.Hidden } };
            screen.Add(host);

            string title = d.HasPendingChanges ? $"Preview (+{d.TotalPending} AP)" : $"{selectedAbility} · {AbilityName(selectedAbility)}";
            host.Add(FigmaUi.CreateSectionHeader(title, 346f));

            var detailScroll = FigmaUi.CreateVerticalScrollView();
            detailScroll.style.flexGrow = 1;
            detailScroll.style.flexBasis = 0;
            detailScroll.style.minHeight = 0;
            detailScroll.style.marginTop = 8;
            detailScroll.style.marginBottom = 8;
            host.Add(detailScroll);

            if (!editable)
            {
                var lockBox = new UiLabel("ATTRIBUTE ALLOCATION IS ONLY AVAILABLE AT THE HUB OR A CAMPFIRE.")
                {
                    style =
                    {
                        fontSize = 12, color = Palette.Ember1, whiteSpace = WhiteSpace.Normal,
                        paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6, marginBottom = 10,
                        backgroundColor = Palette.Hex(0x2A160A),
                        borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                        borderTopColor = Palette.Ember1, borderBottomColor = Palette.Ember1, borderLeftColor = Palette.Ember1, borderRightColor = Palette.Ember1,
                    }
                };
                detailScroll.Add(lockBox);
            }

            if (Database.AbilityDefs.TryGetValue(selectedAbility, out var abDef) && !string.IsNullOrEmpty(abDef.description))
                detailScroll.Add(new UiLabel(abDef.description.ToUpperInvariant()) { style = { fontSize = 13, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

            string reason = guide.GetReason(selectedAbility, hero.classDef?.name);
            if (!string.IsNullOrEmpty(reason))
                detailScroll.Add(new UiLabel(reason.ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Gold2, whiteSpace = WhiteSpace.Normal, marginBottom = 12 } });

            var previewRows = BuildDerivedPreviewRows(hero, d, before, after, selectedAbility);
            if (d.HasPendingChanges)
            {
                detailScroll.Add(new UiLabel("DERIVED STAT CHANGES") { style = { fontSize = 12, color = Palette.Gold2, marginBottom = 6 } });
                var changed = previewRows.Where(r => r.changed).ToList();
                if (changed.Count == 0)
                {
                    detailScroll.Add(new UiLabel("NO MODIFIER THRESHOLD CROSSED YET. ADD 1 MORE AP TO AN ODD SCORE TO RAISE ITS MODIFIER.")
                    {
                        style = { fontSize = 12, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal, marginBottom = 8 }
                    });
                }
                else
                {
                    foreach (var r in changed)
                        detailScroll.Add(PreviewStatLine(r.label, $"{r.beforeText} › {r.afterText} ({r.deltaText})", Palette.Ok));
                }
            }
            else
            {
                detailScroll.Add(new UiLabel($"SCALES WITH {selectedAbility}") { style = { fontSize = 12, color = Palette.Mist3, marginBottom = 6 } });
                foreach (var r in previewRows.Where(x => x.related))
                    detailScroll.Add(PreviewStatLine(r.label, r.beforeText, Palette.Mist1));
            }

            if (!string.IsNullOrEmpty(feedback))
                host.Add(new UiLabel(feedback.ToUpperInvariant()) { style = { fontSize = 12, color = feedbackBad ? Palette.Err : Palette.Ok, whiteSpace = WhiteSpace.Normal, marginBottom = 8, flexShrink = 0 } });

            bool canReset = editable && d.HasPendingChanges;
            bool canConfirm = editable && d.HasPendingChanges;
            var foot = new VisualElement { style = { borderTopWidth = 2, borderTopColor = Palette.Ink4, paddingTop = 10, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, flexShrink = 0 } };

            var resetBtn = FigmaUi.CreateButton("Reset", "button-slab", 116f);
            resetBtn.name = "ResetPendingButton";
            resetBtn.SetEnabled(canReset);
            foot.Add(resetBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(resetBtn, "Reset", ResetPendingAttributes, interactable: canReset, tooltip: "CLEAR STAGED ATTRIBUTE POINTS"));

            string confirmLabel = d.HasPendingChanges ? $"Confirm (+{d.TotalPending})" : "Confirm";
            var confirmBtn = FigmaUi.CreateButton(confirmLabel, canConfirm ? "button-primary" : "button-slab", 218f);
            confirmBtn.name = "ConfirmAttributesButton";
            confirmBtn.SetEnabled(canConfirm);
            foot.Add(confirmBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(confirmBtn, "Confirm", ConfirmPendingAttributes, interactable: canConfirm, tooltip: "PERMANENTLY APPLY STAGED ATTRIBUTE POINTS"));

            host.Add(foot);
        }

        void AdjustPendingAttribute(Ability a, int delta)
        {
            if (!CanAllocateAttributesNow)
            {
                Say("ATTRIBUTE ALLOCATION IS ONLY AVAILABLE AT THE HUB OR A CAMPFIRE.", true);
                return;
            }
            var d = EnsureDraft();
            selectedAbility = a;
            bool ok = delta > 0 ? d.TryIncrease(a) : d.TryDecrease(a);
            if (!ok)
            {
                Sfx.Play(Sound.Deny);
                return;
            }
            feedback = null;
            Sfx.Play(delta > 0 ? Sound.Confirm : Sound.Move);
            Rebuild();
        }

        void ResetPendingAttributes()
        {
            var d = EnsureDraft();
            if (!d.HasPendingChanges) return;
            d.Reset();
            Say("STAGED ATTRIBUTE POINTS CLEARED.", false, Sound.Move);
        }

        void ConfirmPendingAttributes()
        {
            if (!CanAllocateAttributesNow)
            {
                Say("ATTRIBUTE ALLOCATION IS ONLY AVAILABLE AT THE HUB OR A CAMPFIRE.", true);
                return;
            }
            var d = EnsureDraft();
            string summary = d.Pending.Summary();
            if (d.TryCommit(out int spent, out string err))
                Say($"CONFIRMED {spent} AP: {summary}.", false, Sound.SkillUnlock);
            else
                Say(err ?? "CANNOT APPLY ATTRIBUTE POINTS.", true);
        }

        static VisualElement MakePill(string text, Color textColor, Color borderColor, Color bgColor) =>
            new UiLabel(text)
            {
                style =
                {
                    fontSize = 10, color = textColor, backgroundColor = bgColor,
                    marginRight = 6, paddingLeft = 5, paddingRight = 5, paddingTop = 1, paddingBottom = 1,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = borderColor, borderBottomColor = borderColor, borderLeftColor = borderColor, borderRightColor = borderColor,
                }
            };

        static VisualElement PreviewStatLine(string label, string value, Color valueColor)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, minHeight = 26, borderBottomWidth = 1, borderBottomColor = Palette.Ink3 } };
            row.Add(new UiLabel(label) { style = { fontSize = 12, color = Palette.Mist2, marginRight = 6 } });
            row.Add(new UiLabel(value) { style = { fontSize = 12, color = valueColor, unityTextAlign = TextAnchor.MiddleRight } });
            return row;
        }

        static List<(string label, string beforeText, string afterText, string deltaText, bool changed, bool related)> BuildDerivedPreviewRows(
            Unit hero, AttributeAllocationDraft d, DerivedStats b, DerivedStats a, Ability focused)
        {
            var list = new List<(string, string, string, string, bool, bool)>();
            void AddInt(string label, int bv, int av, string suffix, params Ability[] gov)
            {
                int diff = av - bv;
                string dStr = (diff > 0 ? "+" : "") + diff + suffix;
                list.Add((label, $"{bv}{suffix}", $"{av}{suffix}", dStr, diff != 0, Array.IndexOf(gov, focused) >= 0));
            }
            void AddPctFloat(string label, float bv, float av, params Ability[] gov)
            {
                float bp = Mathf.Round(bv * 1000f) / 10f;
                float ap = Mathf.Round(av * 1000f) / 10f;
                float dp = Mathf.Round((ap - bp) * 10f) / 10f;
                string Fmt(float v) => (v >= 0f ? "+" : "") + v.ToString("0.#") + "%";
                list.Add((label, Fmt(bp), Fmt(ap), Fmt(dp), Mathf.Abs(dp) > 0.01f, Array.IndexOf(gov, focused) >= 0));
            }

            // Direct raw skill scaling indicators (STR / INT scale skills even on odd scores)
            var prevFinal = d.PreviewFinalScores();
            AddInt("RAW STR (PHYS SKILLS)", hero.Score(Ability.STR), prevFinal.str, "", Ability.STR);
            AddInt("RAW INT (ELEM SKILLS)", hero.Score(Ability.INT), prevFinal.intel, "", Ability.INT);

            // Resource & Restorative
            AddInt("MAX HP", b.maxHp, a.maxHp, "", Ability.VIT);
            AddInt("MAX MP", b.maxMp, a.maxMp, "", Ability.INT);
            AddInt("HEALING POWER", b.healingPower, a.healingPower, "", Ability.WIS);
            AddPctFloat("SHIELD POWER", b.shieldPowerBonus, a.shieldPowerBonus, Ability.WIS, Ability.CHA);

            // Defensive
            AddInt("PHYSICAL DEFENSE", b.physicalDefense, a.physicalDefense, "", Ability.STR, Ability.VIT);
            AddInt("MAGIC DEFENSE", b.magicDefense, a.magicDefense, "", Ability.INT, Ability.WIS);
            AddPctFloat("DAMAGE REDUCTION", b.damageReductionBonus, a.damageReductionBonus, Ability.VIT, Ability.STR);
            AddInt("BLOCK POWER", b.blockPower, a.blockPower, "%", Ability.STR, Ability.VIT);
            AddInt("EVASION", b.evasion, a.evasion, "%", Ability.DEX);
            AddInt("STATUS RESISTANCE", b.statusResistance, a.statusResistance, "%", Ability.VIT, Ability.WIS);
            AddInt("MENTAL RESISTANCE", b.mentalResistance, a.mentalResistance, "%", Ability.WIS, Ability.INT);
            AddInt("STAGGER RESISTANCE", b.staggerResistance, a.staggerResistance, "%", Ability.VIT, Ability.STR);

            // Offensive
            AddInt("PHYSICAL POWER", b.physicalPower, a.physicalPower, "", Ability.STR);
            AddInt("MAGIC POWER", b.magicPower, a.magicPower, "", Ability.INT);
            AddInt("RANGED ACCURACY", b.rangedAccuracyBonus, a.rangedAccuracyBonus, "%", Ability.DEX);
            AddInt("PHYS CRIT CHANCE", b.criticalChance, a.criticalChance, "%", Ability.DEX, Ability.STR);
            AddInt("ARMOR PENETRATION", b.armorPenetration, a.armorPenetration, "", Ability.STR, Ability.DEX);
            AddInt("MAGIC PENETRATION", b.magicPenetration, a.magicPenetration, "", Ability.INT, Ability.WIS);
            AddInt("COUNTER POWER", b.counterPower, a.counterPower, "%", Ability.DEX, Ability.STR);
            AddInt("BREAK POWER", b.physicalBreakPower, a.physicalBreakPower, "", Ability.STR, Ability.DEX);

            // Execution & Support
            AddInt("INITIATIVE (SPD)", b.initiative, a.initiative, "", Ability.DEX, Ability.INT);
            AddInt("ACTION SPEED", b.actionSpeed, a.actionSpeed, "", Ability.DEX, Ability.INT);
            AddInt("QTE WINDOW", b.qteTimingBonus, a.qteTimingBonus, "%", Ability.DEX);
            AddInt("QTE STABILITY", b.qteStability, a.qteStability, "%", Ability.DEX, Ability.WIS);
            AddPctFloat("BUFF EFFECTIVENESS", b.buffEffectivenessBonus, a.buffEffectivenessBonus, Ability.CHA, Ability.WIS);
            AddPctFloat("DEBUFF EFFECTIVENESS", b.debuffEffectivenessBonus, a.debuffEffectivenessBonus, Ability.CHA, Ability.INT);
            AddInt("SUPPORT POWER", b.supportPower, a.supportPower, "%", Ability.CHA, Ability.WIS);

            return list;
        }

        static string AbilityName(Ability a) => a switch
        {
            Ability.STR => "STRENGTH",
            Ability.DEX => "DEXTERITY",
            Ability.VIT => "VITALITY",
            Ability.INT => "INTELLIGENCE",
            Ability.WIS => "WISDOM",
            _ => "CHARISMA",
        };

        static string FmtSigned(int v) => v >= 0 ? $"+{v}" : v.ToString();
    }
}
