using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Dungeon Map — migrated to Figma 12---Dungeon-Map.uxml + 12b---Dungeon-Map---Run-Effects--TAB.uxml
    // with procedural Painter2D edges, RUN EFFECTS top bar, TAB Run Effects drawer, and Pause / Abandon modals.
    public class MapScreen : GameScreen
    {
        const float CanvasW = 894f, CanvasH = 462f;
        const float StartX = 40f, X0 = 124f, XEnd = 828f, CenterY = 231f, StepY = 86f;

        MapDef currentMap;
        readonly Dictionary<UitkMenu.Item, NodeDef> nodeOf = new Dictionary<UitkMenu.Item, NodeDef>();
        readonly Dictionary<NodeDef, VisualElement> nodeElOf = new Dictionary<NodeDef, VisualElement>();
        readonly List<(Vector2 a, Vector2 b, LinkState state)> edges = new List<(Vector2, Vector2, LinkState)>();

        UitkMenu menu;
        VisualElement mapCanvas;
        VisualElement edgeLayer;
        UiLabel arrowLbl;
        UitkMenu.Item travelItem;
        VisualElement travelInst;
        UitkMenu.Item sneakItem;
        VisualElement sneakBtn;
        // Picked by clicking (or Enter on) a node; Travel/Sneak commit to it. Hover only previews, because the menu
        // moves focus on hover and the pointer crosses other nodes on its way to the Travel button.
        NodeDef selected;
        readonly List<UitkMenu.Item> nodeItems = new List<UitkMenu.Item>();

        VisualElement pauseOverlay;
        UitkMenu pauseModalMenu;
        VisualElement effectsOverlay;
        UitkMenu effectsModalMenu;

        class TabWatcher : MonoBehaviour
        {
            public Action onTab;
            void Update() { if (Controls.Details) onTab?.Invoke(); }
        }

        enum LinkState { Locked, Open, Done }

        protected override void Build()
        {
            var map = currentMap = Run?.Map ?? MapGenerator.Generate(1);
            MountFigma("12---Dungeon-Map");
            StripStaticNodesAndPreview();
            FigmaUi.HideByClass(UiRoot, "toast-info__i-2", "final-dashed-locked-vars"); // baked rule note & dashed-locked legend
            FigmaUi.HideInstancesWithText(UiRoot, "RUN PROGRESS");

            var available = Run != null ? Run.Available.ToList() : map.nodes.Where(n => n.floor == 0).ToList();
            var current = Run?.Current;
            var cleared = Run?.Cleared ?? new HashSet<string>();

            // Header
            var actLbl = UiRoot.Q<UiLabel>(className: "dungeon-1-of-8-vars");
            if (actLbl != null)
            {
                actLbl.text = map.totalStages > 1
                    ? $"DUNGEON {map.act} OF 8 · STAGE {map.stage} OF {map.totalStages}"
                    : $"DUNGEON {map.act} OF 8";
            }

            var titleLbl = UiRoot.Q<UiLabel>(className: "the-sunken-crypt-vars");
            if (titleLbl != null) titleLbl.text = map.name;

            PrologueBanner.TryAttachPrologue(UiRoot, "THE THRESHOLD: Click or select the highlighted node to advance. Icons show Battle, Event, and Boss.", top: 132f, left: 42f, width: 852f, badgeRight: 180f, badgeTop: 16f);

            var pipLbl = UiRoot.Q<UiLabel>(className: "node-vars-5");
            if (pipLbl != null) pipLbl.text = string.Concat(Enumerable.Range(1, 8).Select(i => i <= map.act ? "◆" : "◇"));

            int gold = Run != null ? Run.Gold : 0;
            var runGold = UiRoot.Q<VisualElement>(className: "currency-tag-run");
            if (runGold != null)
            {
                FigmaUi.SetInstanceText(runGold, "text-0", $"{gold:#,##0} SPOILS");
                FigmaUi.AnchorRight(runGold, 24f);
            }

            BuildRunEffectsBar();

            var watcher = Root.gameObject.AddComponent<TabWatcher>();
            watcher.onTab = () =>
            {
                // Not over the pause menu or a confirmation dialog (Abandon / No Retries).
                if (pauseOverlay != null || (UitkMenu.ActiveMenu != null && UitkMenu.ActiveMenu != menu && UitkMenu.ActiveMenu != effectsModalMenu)) return;
                ToggleRunEffectsModal();
            };

            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = OpenPause;

            // Build procedural edges & nodes
            float dx = (XEnd - X0) / Mathf.Max(1, map.Floors - 1);
            edges.Clear();

            var ent = new Vector2(StartX, CenterY);
            SpawnEntrance(ent, current == null);
            foreach (var n in map.nodes.Where(n => n.floor == 0))
            {
                var state = current == null ? LinkState.Open : cleared.Contains(n.id) ? LinkState.Done : LinkState.Locked;
                edges.Add((ent, Center(n, dx), state));
            }
            foreach (var n in map.nodes)
            {
                foreach (var id in n.next)
                {
                    var m = map.Get(id);
                    var state = cleared.Contains(n.id) && cleared.Contains(m.id) ? LinkState.Done
                        : n == current && available.Contains(m) ? LinkState.Open
                        : LinkState.Locked;
                    edges.Add((Center(n, dx), Center(m, dx), state));
                }
            }
            edgeLayer.MarkDirtyRepaint();

            nodeOf.Clear();
            nodeElOf.Clear();
            nodeItems.Clear();
            selected = available.Count == 1 ? available[0] : null; // one open path: nothing to choose
            foreach (var n in map.nodes)
            {
                bool avail = available.Contains(n);
                var el = SpawnNode(n, avail, dx, cleared.Contains(n.id), n == current);
                nodeElOf[n] = el;
                if (avail)
                {
                    var nd = n;
                    var item = menu.Add(el, TypeName(nd.type), () => Select(nd));
                    nodeItems.Add(item);
                    var nodeEl = el;
                    item.visual = on =>
                    {
                        bool sel = nd == selected;
                        var col = on ? Palette.Hex(0xFFF4D2) : sel ? Palette.Gold2 : Palette.Mist1;
                        nodeEl.style.borderTopColor = col;
                        nodeEl.style.borderBottomColor = col;
                        nodeEl.style.borderLeftColor = col;
                        nodeEl.style.borderRightColor = col;
                        nodeEl.style.backgroundColor = sel ? Palette.Hex(0x2B2210) : on ? Palette.Ink4 : Palette.Ink3;
                    };
                    item.visual(false);
                    nodeOf[item] = nd;
                }
            }

            // Right party list
            BindPartyMini();

            // Footer buttons
            var partyBtn = FigmaUi.FindButton(UiRoot, "Party");
            if (partyBtn != null)
            {
                if (partyBtn.parent != null)
                    FigmaUi.AnchorRight(partyBtn.parent, 24f);
                partyBtn.style.marginRight = 10;
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(partyBtn, "Party", () => Game.Go(GameState.Party), tooltip: "PARTY, STATUS AND EQUIPMENT"), partyBtn);
            }

            // Add clean horizontal spacing to the baked bottom map legend row (outer container holds [chip, label] pairs)
            var combatLegend = FigmaUi.FindLabel(UiRoot, "COMBAT");
            var legendRow = combatLegend?.parent?.parent;
            if (legendRow != null)
            {
                for (int i = 0; i < legendRow.childCount; i++)
                {
                    var grp = legendRow[i];
                    grp.style.marginRight = 14;
                    if (grp.childCount >= 2) grp[0].style.marginRight = 6;
                }
            }

            travelInst = UiRoot.Q<VisualElement>(className: "button-primary-2");
            // In 12---Dungeon-Map.uxml the primary button instance holds an inner clickable visual
            var travelHost = UiRoot.Q<VisualElement>(className: "button-primary-focus") ?? FindPrimaryInstance();
            if (travelHost != null)
            {
                travelInst = travelHost;
                travelItem = menu.Add(travelHost, "Travel", () => { if (selected != null) Enter(selected); });
                travelItem.visual = on =>
                {
                    FigmaUi.SetInstanceText(travelHost, "text-0", on ? "▸" : "");
                    FigmaUi.ApplyDefaultFocusVisual(travelHost, on, false, travelItem.interactable);
                };
                travelItem.visual(false);
            }

            if (Run != null && Run.HasRace("halfling"))
            {
                sneakBtn = FigmaUi.CreateCompactButton("Sneak", null, "button-slab");
                sneakBtn.style.marginRight = 10;
                if (partyBtn?.parent != null)
                    partyBtn.parent.Insert(0, sneakBtn);
                else
                {
                    sneakBtn.style.position = Position.Absolute;
                    sneakBtn.style.left = 736;
                    sneakBtn.style.top = 656;
                    (UiRoot.Q<VisualElement>(className: "_12-dungeon-map") ?? UiRoot).Add(sneakBtn);
                }
                sneakItem = menu.Add(sneakBtn, "Sneak", () =>
                {
                    if (selected != null && Run != null && Run.CanSneakNode(selected))
                    {
                        Sfx.Play(Sound.MapStep);
                        Run.SneakNode(selected);
                        Game.Go(GameState.Map);
                    }
                }, interactable: false,
                tooltip: $"HALFLING SMALL AND NIMBLE: BYPASS ONE NORMAL COMBAT NODE PER DUNGEON. YOU GIVE UP {RunState.SneakForfeit.ToUpperInvariant()}.");
                FigmaUi.ApplyDefaultFocusVisual(sneakItem, sneakBtn);
            }

            menu.onFocus = item =>
            {
                if (item != null && nodeOf.TryGetValue(item, out var nd))
                    ShowNode(nd, nodeElOf[nd]);
                else if ((item == travelItem || item == sneakItem) && selected != null && nodeElOf.TryGetValue(selected, out var selEl))
                    ShowNode(selected, selEl);
            };

            if (available.Count > 0) ShowNode(available[0], nodeElOf[available[0]]);
            else ShowNode(null, null);

            // One-shot note after a sneak, in the toast slot the layout reserves.
            if (Run?.LastSneakNote != null)
            {
                var toast = UiRoot.Q<VisualElement>(className: "toast-info__i-2");
                if (toast != null)
                {
                    FigmaUi.SetInstanceText(toast, "text-1", Run.LastSneakNote.ToUpperInvariant());
                    toast.style.display = DisplayStyle.Flex;
                }
                Run.LastSneakNote = null;
            }
        }

        void Select(NodeDef n)
        {
            selected = n;
            ShowNode(n, nodeElOf[n]);
            foreach (var it in nodeItems) it.visual?.Invoke(it == menu.Current);
            if (travelItem != null) menu.Focus(travelItem, false); // confirm with Travel
        }

        VisualElement FindPrimaryInstance()
        {
            foreach (var l in UiRoot.Query<UiLabel>().ToList())
                if (l.text == "Travel to Combat") return l.parent;
            return null;
        }

        void StripStaticNodesAndPreview()
        {
            var screen = UiRoot.Q<VisualElement>(className: "_12-dungeon-map") ?? UiRoot;
            var mapPanel = UiRoot.Q<VisualElement>(className: "map-field-vars");

            // Hide baked edge image wrapper and baked static nodes inside Figma UXML
            foreach (var cls in new[] { "map-node", "map-node-2", "map-node-3", "sprite-iconboss", "node-vars-6" })
            {
                foreach (var el in UiRoot.Query<VisualElement>(className: cls).ToList())
                    el.style.display = DisplayStyle.None;
            }

            // Hide sibling baked PNG wrappers / entrance / boss diamond SVGs between map-field-vars (idx+1) and rectangle-vars-90
            if (mapPanel != null)
            {
                int startIdx = screen.IndexOf(mapPanel) + 1;
                var sep = UiRoot.Q<VisualElement>(className: "rectangle-vars-90");
                int endIdx = sep != null ? screen.IndexOf(sep) : startIdx;
                for (int i = startIdx; i < endIdx; i++)
                    screen[i].style.display = DisplayStyle.None;
            }

            mapCanvas = new VisualElement { name = "DynamicMapCanvas" };
            mapCanvas.style.position = Position.Absolute;
            mapCanvas.style.left = 28;
            mapCanvas.style.top = 120;
            mapCanvas.style.width = CanvasW;
            mapCanvas.style.height = CanvasH;
            mapCanvas.style.overflow = Overflow.Hidden;

            edgeLayer = new VisualElement { name = "MapEdges" };
            edgeLayer.style.position = Position.Absolute;
            edgeLayer.style.left = 0;
            edgeLayer.style.top = 0;
            edgeLayer.style.width = CanvasW;
            edgeLayer.style.height = CanvasH;
            edgeLayer.generateVisualContent += DrawEdges;
            mapCanvas.Add(edgeLayer);

            arrowLbl = new UiLabel("▾");
            arrowLbl.style.position = Position.Absolute;
            arrowLbl.style.fontSize = 20;
            arrowLbl.style.color = Palette.Mist1;
            arrowLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            arrowLbl.style.width = 24;
            arrowLbl.style.display = DisplayStyle.None;
            mapCanvas.Add(arrowLbl);

            // Insert mapCanvas right above map-field-vars so side panels and legend stay on top cleanly
            if (mapPanel != null)
                screen.Insert(screen.IndexOf(mapPanel) + 1, mapCanvas);
            else
                screen.Add(mapCanvas);
        }

        void DrawEdges(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.lineWidth = 2.5f;
            foreach (var (a, b, state) in edges)
            {
                float len = Vector2.Distance(a, b);
                if (len < 30f) continue;
                p.strokeColor = state == LinkState.Done ? Palette.Mist3 : state == LinkState.Open ? Palette.Mist1 : Palette.Ink5;

                if (state == LinkState.Done)
                {
                    var p0 = Vector2.Lerp(a, b, 18f / len);
                    var p1 = Vector2.Lerp(a, b, (len - 18f) / len);
                    p.BeginPath();
                    p.MoveTo(p0);
                    p.LineTo(p1);
                    p.Stroke();
                }
                else
                {
                    for (float d = 18f; d < len - 22f; d += 12f)
                    {
                        float d2 = Mathf.Min(d + 6f, len - 18f);
                        p.BeginPath();
                        p.MoveTo(Vector2.Lerp(a, b, d / len));
                        p.LineTo(Vector2.Lerp(a, b, d2 / len));
                        p.Stroke();
                    }
                }
            }
        }

        void SpawnEntrance(Vector2 c, bool isStart)
        {
            var dia = new UiLabel("▶");
            dia.style.position = Position.Absolute;
            dia.style.left = c.x - 12;
            dia.style.top = c.y - 14;
            dia.style.width = 24;
            dia.style.height = 24;
            dia.style.fontSize = 24;
            dia.style.unityTextAlign = TextAnchor.MiddleCenter;
            dia.style.color = isStart ? Palette.Gold2 : Palette.Mist3;
            mapCanvas.Add(dia);
        }

        VisualElement SpawnNode(NodeDef n, bool available, float dx, bool cleared, bool current)
        {
            bool boss = n.type == NodeType.Boss;
            float size = boss ? 62f : 50f;
            var c = Center(n, dx);

            string templateKey = current ? "Map-Node-Current" : available ? "Map-Node-Choosable" : "Map-Node-Locked";
            var el = FigmaUi.InstantiateTemplate(templateKey) ?? new VisualElement();
            el.style.position = Position.Absolute;
            el.style.left = c.x - size / 2f;
            el.style.top = c.y - size / 2f;
            el.style.width = size;
            el.style.height = size;
            el.style.justifyContent = Justify.Center;
            el.style.alignItems = UnityEngine.UIElements.Align.Center;

            FigmaUi.SetInstanceText(el, "text-0", Glyph(n.type));
            var txtLbl = el.Q<UiLabel>("text-0");
            if (txtLbl != null)
            {
                // Elites read as danger on the map itself, not only after selecting the node.
                bool elite = n.type == NodeType.Elite;
                txtLbl.style.color = current ? Palette.Gold1 : available ? (elite ? Palette.CrimsonBright : Palette.Mist1)
                    : cleared ? Palette.Mist3 : elite ? Palette.SpecCrimson : Palette.Ink5;
                if (boss) txtLbl.style.fontSize = 20;
            }

            if (boss)
            {
                el.style.backgroundColor = available ? Palette.Ink3 : current ? Palette.Hex(0x2B2210) : Palette.Ink2;
                el.style.borderTopWidth = 2;
                el.style.borderBottomWidth = 2;
                el.style.borderLeftWidth = 2;
                el.style.borderRightWidth = 2;
                var col = available ? Palette.Gold1 : Palette.Ink5;
                el.style.borderTopColor = col;
                el.style.borderBottomColor = col;
                el.style.borderLeftColor = col;
                el.style.borderRightColor = col;
            }
            else if (cleared && !current)
            {
                el.style.borderTopColor = Palette.Mist3;
                el.style.borderBottomColor = Palette.Mist3;
                el.style.borderLeftColor = Palette.Mist3;
                el.style.borderRightColor = Palette.Mist3;
                var chk = new UiLabel("✓");
                chk.style.position = Position.Absolute;
                chk.style.right = -6;
                chk.style.top = -6;
                chk.style.width = 18;
                chk.style.height = 18;
                chk.style.fontSize = 12;
                chk.style.unityTextAlign = TextAnchor.MiddleCenter;
                chk.style.backgroundColor = Palette.Mist3;
                chk.style.color = Palette.Ink0;
                el.Add(chk);
            }

            if (current)
            {
                var you = new UiLabel("YOU");
                you.style.position = Position.Absolute;
                you.style.top = -18;
                you.style.paddingLeft = 5;
                you.style.paddingRight = 5;
                you.style.fontSize = 10;
                you.style.backgroundColor = Palette.Gold2;
                you.style.color = Palette.Ink0;
                el.Add(you);
            }

            mapCanvas.Add(el);
            return el;
        }

        Vector2 Center(NodeDef n, float dx)
        {
            int count = currentMap.nodes.Count(m => m.floor == n.floor);
            return new Vector2(X0 + n.floor * dx, CenterY + (n.lane - (count - 1) / 2f) * StepY);
        }

        void ShowNode(NodeDef n, VisualElement view)
        {
            var hdr = UiRoot.Q<VisualElement>(className: "section-header__i-12");
            if (hdr != null) FigmaUi.SetInstanceText(hdr, "text-0", n == null ? "Select a path" : $"{TypeName(n.type)} node");

            var descLbl = UiRoot.Q<UiLabel>(className: "gravebound-archer-hollow-squire-vars");
            if (descLbl != null) descLbl.text = n == null ? "NO PATH IS OPEN." : Describe(n);

            var curLbl = UiRoot.Q<UiLabel>(className: "node-vars-7");
            if (curLbl != null)
            {
                curLbl.text = n != null && n.type == NodeType.Shop ? "SPOILS" : "—";
                curLbl.style.color = n != null && n.type == NodeType.Shop ? Palette.Ember1 : Palette.Mist3;
                curLbl.style.position = Position.Relative;
                curLbl.style.left = StyleKeyword.Auto;
                curLbl.style.top = StyleKeyword.Auto;
            }

            var riskLbl = UiRoot.Q<UiLabel>(className: "combat-vars-2");
            if (riskLbl != null)
            {
                riskLbl.text = n == null ? "—" : Risk(n.type);
                riskLbl.style.color = n != null && (n.type == NodeType.Boss || n.type == NodeType.Elite) ? Palette.Err : Palette.Mist1;
                riskLbl.style.position = Position.Relative;
                riskLbl.style.left = StyleKeyword.Auto;
                riskLbl.style.top = StyleKeyword.Auto;
            }

            // Travel/Sneak follow the picked node, not the previewed one.
            if (travelInst != null)
            {
                string label = selected == null ? "Pick a path" : $"Travel to {TypeName(selected.type)}";
                FigmaUi.SetInstanceText(travelInst, "text-1", label);
            }
            if (travelItem != null)
            {
                travelItem.interactable = selected != null;
                travelItem.visual?.Invoke(menu != null && menu.Current == travelItem);
            }
            if (sneakItem != null && sneakBtn != null)
            {
                bool canSneak = Run != null && Run.CanSneakNode(selected);
                sneakItem.interactable = canSneak;
                sneakBtn.style.display = canSneak ? DisplayStyle.Flex : DisplayStyle.None;
                sneakItem.visual?.Invoke(menu != null && menu.Current == sneakItem);
            }

            if (view != null && arrowLbl != null)
            {
                arrowLbl.style.display = DisplayStyle.Flex;
                arrowLbl.style.left = view.style.left.value.value + view.style.width.value.value / 2f - 12f;
                arrowLbl.style.top = view.style.top.value.value - 22f;
                arrowLbl.BringToFront();
            }
            else if (arrowLbl != null)
            {
                arrowLbl.style.display = DisplayStyle.None;
            }
        }

        void BindPartyMini()
        {
            string[] spriteClasses = { "sprite-knight", "sprite-rogue", "sprite-mage", "sprite-healer" };
            string[] nameClasses = { "knight-vars-3", "rogue-vars-3", "mage-vars-3", "healer-vars-3" };
            var hpGauges = UiRoot.Query<VisualElement>(className: "gauge-hp").ToList();
            var mpGauges = UiRoot.Query<VisualElement>(className: "gauge-mp").ToList();
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

            for (int i = 0; i < 4; i++)
            {
                var fig = UiRoot.Q<VisualElement>(className: spriteClasses[i]);
                var nLbl = UiRoot.Q<UiLabel>(className: nameClasses[i]);
                var hpG = i < hpGauges.Count ? hpGauges[i] : null;
                var mpG = i < mpGauges.Count ? mpGauges[i] : null;
                if (i >= party.Count)
                {
                    if (fig?.parent != null) fig.parent.style.display = DisplayStyle.None;
                    if (nLbl != null) nLbl.style.display = DisplayStyle.None;
                    if (hpG != null) hpG.style.display = DisplayStyle.None;
                    if (mpG != null) mpG.style.display = DisplayStyle.None;
                    continue;
                }
                var u = party[i];
                if (fig != null && u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "map_mini_" + (u.classDef?.id ?? u.name)));
                if (nLbl != null)
                {
                    nLbl.text = u.name;
                    nLbl.style.color = u.Alive ? Palette.Mist1 : Palette.HpLow;
                    nLbl.style.maxWidth = 122;
                    nLbl.style.fontSize = 16;
                    nLbl.style.overflow = Overflow.Hidden;
                }
                float hp = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
                FigmaUi.SetGaugeFill(hpG, hp, hp < 0.25f ? Palette.HpLow : Palette.Hp);
                FigmaUi.SetGaugeFill(mpG, (float)u.mp / Mathf.Max(1, u.stats.maxMp), Palette.Mp);
            }
        }

        // ------------------------------------------------------------ 12b Run Effects Bar & TAB Drawer

        void BuildRunEffectsBar()
        {
            var screen = UiRoot.Q<VisualElement>(className: "_12-dungeon-map") ?? UiRoot;

            // Add TAB DETAILS key-hint to bottom hint row if present
            var escHint = FigmaUi.FindInstanceWithText(UiRoot, "text-0", "ESC");
            if (escHint?.parent != null)
            {
                var tabHint = FigmaUi.InstantiateTemplate("Key-Hint");
                if (tabHint != null)
                {
                    tabHint.AddToClassList("uitk-clickable-bar");
                    tabHint.style.flexShrink = 0;
                    tabHint.style.marginLeft = 10;
                    tabHint.style.paddingLeft = 4;
                    tabHint.style.paddingRight = 6;
                    FigmaUi.SetInstanceText(tabHint, "text-0", "TAB");
                    FigmaUi.SetInstanceText(tabHint, "text-1", "DETAILS");
                    tabHint.pickingMode = PickingMode.Position;
                    tabHint.RegisterCallback<ClickEvent>(_ => ToggleRunEffectsModal());
                    escHint.parent.Add(tabHint);
                }
            }

            var bar = new VisualElement
            {
                name = "RunEffectsBar",
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    left = 28,
                    top = 80,
                    width = 894,
                    height = 40,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 4,
                    paddingBottom = 4,
                    flexDirection = FlexDirection.Row,
                    alignItems = UnityEngine.UIElements.Align.Center,
                    backgroundColor = Palette.Ink1,
                    borderBottomWidth = 2,
                    borderBottomColor = Palette.Ink4,
                    overflow = Overflow.Hidden
                }
            };
            bar.AddToClassList("uitk-clickable-bar");
            bar.RegisterCallback<ClickEvent>(_ => ToggleRunEffectsModal());

            bar.Add(new UiLabel("RUN EFFECTS")
            {
                pickingMode = PickingMode.Ignore,
                style = { flexShrink = 0, fontSize = 13, letterSpacing = 0.56f, color = Palette.Mist3, marginRight = 10 }
            });

            var chips = CollectRunEffectChips();
            if (chips.Count == 0)
            {
                bar.Add(new UiLabel("NONE ACTIVE · PRESS TAB TO INSPECT")
                {
                    pickingMode = PickingMode.Ignore,
                    style = { fontSize = 12, color = Palette.Mist3 }
                });
            }
            else
            {
                var chipsRow = new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = UnityEngine.UIElements.Align.Center,
                        flexGrow = 1,
                        flexShrink = 1,
                        overflow = Overflow.Hidden,
                        marginRight = 10
                    }
                };
                int shown = Math.Min(2, chips.Count);
                for (int i = 0; i < shown; i++)
                {
                    var compactMeta = chips[i].meta == "DUNGEON" || chips[i].meta == "RELIC" ? "" : chips[i].meta;
                    var chip = InstantiateEffectChip(chips[i].template, chips[i].glyph, chips[i].name, compactMeta);
                    if (chip != null)
                    {
                        chip.style.marginRight = 8;
                        chipsRow.Add(chip);
                    }
                }
                bar.Add(chipsRow);
                if (chips.Count > shown)
                {
                    var ov = FigmaUi.InstantiateTemplate("Effect-Chip-Overflow");
                    if (ov != null)
                    {
                        ov.style.flexShrink = 0;
                        ov.style.marginRight = 10;
                        FigmaUi.SetInstanceText(ov, "text-0", "+");
                        FigmaUi.SetInstanceText(ov, "text-1", $"+{chips.Count - shown}");
                        bar.Add(ov);
                    }
                }
            }

            bar.Add(new UiLabel("TAB › DETAILS")
            {
                pickingMode = PickingMode.Ignore,
                style = { marginLeft = StyleKeyword.Auto, flexShrink = 0, fontSize = 11, color = Palette.Gold2 }
            });

            screen.Add(bar);
        }

        List<(string template, string glyph, string name, string meta)> CollectRunEffectChips()
        {
            var list = new List<(string, string, string, string)>();
            if (Run == null) return list;

            if (Run.ModifierManager != null)
            {
                foreach (var g in Run.ModifierManager.GetGroupSummaries())
                {
                    string tpl = g.def.polarity switch
                    {
                        ModifierPolarity.Debuff => "Effect-Chip-Debuff",
                        ModifierPolarity.BuffWithDrawback => "Effect-Chip-Drawback",
                        _ => "Effect-Chip-Buff"
                    };
                    string glyph = g.def.polarity switch
                    {
                        ModifierPolarity.Debuff => "▼",
                        ModifierPolarity.BuffWithDrawback => "◆",
                        _ => "▲"
                    };
                    string meta = g.effectiveStacks > 1 ? $"×{g.effectiveStacks}" : "DUNGEON";
                    list.Add((tpl, glyph, g.def.displayName, meta));
                }
            }

            if (Run.Relics != null)
            {
                foreach (var rid in Run.Relics)
                {
                    if (Database.Relics.TryGetValue(rid, out var rel))
                        list.Add(("Effect-Chip-Relic", "◈", rel.name, "RELIC"));
                }
            }

            if (Run.BattleBuffs != null)
            {
                foreach (var b in Run.BattleBuffs)
                    list.Add((EventChipTemplate(b.id), "✦", StatusInfo.Name(b.id), "DUNGEON"));
            }
            return list;
        }

        // Event blessings use the event chip; event afflictions (debuff statuses) use the debuff chip.
        static string EventChipTemplate(StatusId id) => StatusInfo.IsBuff(id) ? "Effect-Chip-Event" : "Effect-Chip-Debuff";

        static VisualElement InstantiateEffectChip(string templateKey, string glyph, string name, string meta)
        {
            var chip = FigmaUi.InstantiateTemplate(templateKey);
            if (chip == null) return null;
            chip.style.flexShrink = 0;
            chip.pickingMode = PickingMode.Ignore;
            FigmaUi.SetInstanceText(chip, "text-0", glyph);
            FigmaUi.SetInstanceText(chip, "text-1", name);
            FigmaUi.SetInstanceText(chip, "text-2", meta);
            var l1 = chip.Q<UiLabel>("text-1");
            if (l1 != null) { l1.style.marginLeft = 6; l1.style.marginRight = 6; }
            return chip;
        }

        public bool IsRunEffectsModalOpen => effectsOverlay != null;

        public void ToggleRunEffectsModal()
        {
            if (effectsOverlay != null) CloseRunEffectsModal();
            else OpenRunEffectsModal();
        }

        void CloseRunEffectsModal()
        {
            if (effectsModalMenu != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(effectsModalMenu);
                else UnityEngine.Object.DestroyImmediate(effectsModalMenu);
                effectsModalMenu = null;
            }
            effectsOverlay?.RemoveFromHierarchy();
            effectsOverlay = null;
        }

        void OpenRunEffectsModal()
        {
            CloseModals();
            Sfx.Play(Sound.ModalOpen);

            effectsOverlay = new VisualElement
            {
                name = "MapRunEffectsOverlay",
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    left = 0, top = 0, right = 0, bottom = 0,
                    backgroundColor = new Color(5f / 255f, 8f / 255f, 15f / 255f, 0.72f),
                    justifyContent = Justify.Center,
                    alignItems = UnityEngine.UIElements.Align.Center
                }
            };

            var card = new VisualElement
            {
                style =
                {
                    width = 560, height = 676,
                    paddingLeft = 16, paddingRight = 16, paddingTop = 14, paddingBottom = 14,
                    backgroundColor = Palette.Ink2,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = Palette.Gold3, borderBottomColor = Palette.Gold3,
                    borderLeftColor = Palette.Gold3, borderRightColor = Palette.Gold3,
                    flexDirection = FlexDirection.Column
                }
            };

            var hdr = FigmaUi.CreateSectionHeader("Run Effects", 524f);
            if (hdr != null)
            {
                hdr.style.height = 36;
                hdr.style.flexShrink = 0;
                hdr.style.marginBottom = 4;
                card.Add(hdr);
            }

            var scroll = FigmaUi.CreateVerticalScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.marginBottom = 10;

            void AddSectionLabel(string text)
            {
                scroll.Add(new UiLabel(text)
                {
                    style = { fontSize = 13, letterSpacing = 0.56f, color = Palette.Gold2, marginTop = 6, marginBottom = 6 }
                });
            }

            void AddEffectCard(VisualElement chip, string rightMeta, string desc, StatusId? statusIcon = null)
            {
                var box = new VisualElement
                {
                    style =
                    {
                        paddingLeft = 8, paddingRight = 10, paddingTop = 6, paddingBottom = 8,
                        marginBottom = 6,
                        backgroundColor = Palette.Ink1,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4,
                        borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4,
                        flexDirection = FlexDirection.Column
                    }
                };
                var topRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, justifyContent = Justify.SpaceBetween, marginBottom = 6 }
                };
                var leftGroup = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, flexShrink = 1, overflow = Overflow.Hidden, marginRight = 8 } };
                if (chip != null) leftGroup.Add(chip);
                if (statusIcon.HasValue)
                {
                    var ic = IconArt.Inline(IconArt.Status(statusIcon.Value), 1, 0f);
                    if (ic != null) { ic.style.marginLeft = 6; leftGroup.Add(ic); }
                }
                topRow.Add(leftGroup);
                topRow.Add(new UiLabel(rightMeta.ToUpperInvariant())
                {
                    style = { fontSize = 11, letterSpacing = 0.48f, color = Palette.Mist3, whiteSpace = WhiteSpace.NoWrap, flexShrink = 0 }
                });
                box.Add(topRow);
                box.Add(new UiLabel((desc ?? "").ToUpperInvariant())
                {
                    style = { fontSize = 13, letterSpacing = 0.48f, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal }
                });
                scroll.Add(box);
            }

            int totalCount = 0;
            var mods = Run?.ModifierManager?.GetGroupSummaries();
            if (mods != null && mods.Count > 0)
            {
                AddSectionLabel("MODIFIERS");
                foreach (var g in mods)
                {
                    totalCount++;
                    string tpl = g.def.polarity == ModifierPolarity.Debuff ? "Effect-Chip-Debuff"
                        : g.def.polarity == ModifierPolarity.BuffWithDrawback ? "Effect-Chip-Drawback" : "Effect-Chip-Buff";
                    string glyph = g.def.polarity == ModifierPolarity.Debuff ? "▼"
                        : g.def.polarity == ModifierPolarity.BuffWithDrawback ? "◆" : "▲";
                    string meta = g.effectiveStacks > 1 ? $"×{g.effectiveStacks}" : "DUNGEON";
                    string right = g.effectiveStacks > 1
                        ? $"{g.effectiveStacks} STACKS · {DungeonModifierManager.UntilDungeonExit}"
                        : DungeonModifierManager.UntilDungeonExit;
                    AddEffectCard(InstantiateEffectChip(tpl, glyph, g.def.displayName, meta), right, g.def.description);
                }
            }

            if (Run?.Relics != null && Run.Relics.Count > 0)
            {
                AddSectionLabel("RELICS");
                foreach (var rid in Run.Relics)
                {
                    if (!Database.Relics.TryGetValue(rid, out var r)) continue;
                    totalCount++;
                    AddEffectCard(InstantiateEffectChip("Effect-Chip-Relic", "◈", r.name, "RELIC"),
                        "PERMANENT", r.gameplayEffect ?? r.description);
                }
            }

            if (Run?.BattleBuffs != null && Run.BattleBuffs.Count > 0)
            {
                AddSectionLabel("EVENT EFFECTS");
                foreach (var b in Run.BattleBuffs)
                {
                    totalCount++;
                    AddEffectCard(InstantiateEffectChip(EventChipTemplate(b.id), "✦", StatusInfo.Name(b.id), "DUNGEON"),
                        DungeonModifierManager.UntilDungeonExit, $"EACH BATTLE: PARTY {StatusInfo.Name(b.id)} ({b.turns} {(b.turns == 1 ? "TURN" : "TURNS")}).", b.id);
                }
            }

            if (totalCount == 0)
            {
                scroll.Add(new UiLabel("NO ACTIVE MODIFIERS, RELICS, OR EVENT EFFECTS IN THIS RUN YET.")
                {
                    style = { fontSize = 14, color = Palette.Mist3, whiteSpace = WhiteSpace.Normal, marginTop = 12 }
                });
            }

            card.Add(scroll);

            var footRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, flexShrink = 0 }
            };
            var kh = FigmaUi.InstantiateTemplate("Key-Hint");
            if (kh != null)
            {
                FigmaUi.SetInstanceText(kh, "text-0", "TAB");
                FigmaUi.SetInstanceText(kh, "text-1", "CLOSE");
                footRow.Add(kh);
            }
            var closeBtn = FigmaUi.CreateButton("Close", "button-slab", 130f);
            footRow.Add(closeBtn);
            card.Add(footRow);

            effectsModalMenu = Root.gameObject.AddComponent<UitkMenu>();
            effectsModalMenu.AttachRoot(effectsOverlay);
            effectsModalMenu.onBack = CloseRunEffectsModal;
            FigmaUi.ApplyDefaultFocusVisual(effectsModalMenu.Add(closeBtn, "Close", CloseRunEffectsModal), closeBtn);

            effectsOverlay.Add(card);
            UiRoot.Add(effectsOverlay);
        }

        // ------------------------------------------------------------ Pause & Abandon modals

        void CloseModals()
        {
            CloseRunEffectsModal();
            FigmaUi.DestroyMenu(ref pauseModalMenu);
            pauseOverlay?.RemoveFromHierarchy();
            pauseOverlay = null;
        }

        void OpenPause()
        {
            CloseModals();
            Sfx.Play(Sound.ModalOpen);
            pauseOverlay = new VisualElement { name = "MapPauseOverlay" };
            pauseOverlay.style.position = Position.Absolute;
            pauseOverlay.style.left = 0;
            pauseOverlay.style.top = 0;
            pauseOverlay.style.right = 0;
            pauseOverlay.style.bottom = 0;
            pauseOverlay.style.backgroundColor = Palette.Hex(0x05070F, 204);
            pauseOverlay.style.justifyContent = Justify.Center;
            pauseOverlay.style.alignItems = UnityEngine.UIElements.Align.Center;

            var card = new VisualElement();
            card.style.width = 430;
            card.style.paddingLeft = 22;
            card.style.paddingRight = 22;
            card.style.paddingTop = 20;
            card.style.paddingBottom = 18;
            card.style.backgroundColor = Palette.Ink2;
            card.style.borderTopWidth = 2;
            card.style.borderBottomWidth = 2;
            card.style.borderLeftWidth = 2;
            card.style.borderRightWidth = 2;
            card.style.borderTopColor = Palette.Gold3;
            card.style.borderBottomColor = Palette.Gold3;
            card.style.borderLeftColor = Palette.Gold3;
            card.style.borderRightColor = Palette.Gold3;

            var title = new UiLabel("Paused");
            title.style.fontSize = 30;
            title.style.color = Palette.Gold2;
            title.style.marginBottom = 14;
            card.Add(title);

            pauseModalMenu = Root.gameObject.AddComponent<UitkMenu>();
            pauseModalMenu.AttachRoot(pauseOverlay);
            Action closePause = CloseModals;
            pauseModalMenu.onBack = closePause;

            void AddPauseRow(string text, Action go, bool enabled = true, string note = null, Color? tint = null)
            {
                var row = new VisualElement();
                row.style.height = 36;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                row.style.marginBottom = 4;
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = UnityEngine.UIElements.Align.Center;
                row.style.borderTopWidth = 1;
                row.style.borderBottomWidth = 1;
                row.style.borderLeftWidth = 1;
                row.style.borderRightWidth = 1;

                var lbl = new UiLabel(text)
                {
                    style = { flexGrow = 1, fontSize = 18, color = !enabled ? Palette.Mist3 : (tint ?? Palette.Mist1) }
                };
                row.Add(lbl);
                if (!string.IsNullOrEmpty(note))
                    row.Add(new UiLabel(note) { style = { fontSize = 12, color = Palette.Mist3 } });
                card.Add(row);

                var item = pauseModalMenu.Add(row, text, go, interactable: enabled);
                item.visual = on =>
                {
                    row.style.backgroundColor = on && enabled ? Palette.Hex(0x2B2210) : Color.clear;
                    var b = on && enabled ? Palette.Gold2 : Color.clear;
                    row.style.borderTopColor = b;
                    row.style.borderBottomColor = b;
                    row.style.borderLeftColor = b;
                    row.style.borderRightColor = b;
                    if (enabled) lbl.style.color = on ? Palette.Gold1 : (tint ?? Palette.Mist1);
                };
                item.visual(false);
            }

            AddPauseRow("Resume", closePause, true, "ESC");
            AddPauseRow("QTE Assists", () => { closePause(); SettingsScreen.RequestQteTab(); Game.Go(GameState.Settings); });
            AddPauseRow("Settings", () => { closePause(); Game.Go(GameState.Settings); });
            AddPauseRow("Save Game", null, false, "HUB ONLY");
            AddPauseRow("Load Game", null, false, "HUB OR MAIN MENU");

            var sep = new VisualElement { style = { height = 2, backgroundColor = Palette.Ink5, marginTop = 6, marginBottom = 6 } };
            card.Add(sep);

            if (Run == null || !Run.IsPrologue) // the Prologue has no Hub to return to yet
                AddPauseRow("Return to Hub...", () => { closePause(); OpenAbandon(); }, true, "ABANDON", Palette.Hex(0xFFC7BB));

            int gold = Run != null ? Run.Gold : 0;
            var footer = new VisualElement { style = { marginTop = 10, paddingTop = 8, borderTopWidth = 1, borderTopColor = Palette.Ink4 } };
            string stageText = Run != null && Run.TotalStages > 1 ? $" · STAGE {Run.Stage}/{Run.TotalStages}" : "";
            footer.Add(new UiLabel($"DUNGEON {Run?.Act ?? 1} OF 8{stageText} · NODE {Run?.Floor ?? 1} · {gold} SPOILS")
            {
                style = { fontSize = 12, color = Palette.Mist2, marginBottom = 4 }
            });
            card.Add(footer);

            pauseOverlay.Add(card);
            UiRoot.Add(pauseOverlay);
        }

        void OpenAbandon()
        {
            int act = Run?.Act ?? 1, gold = Run != null ? Run.Gold : 0, retries = Run?.RetriesLeft ?? 0;
            if (Run != null && !Run.CanAbandon)
            {
                FigmaUi.ShowConfirmationModal(UiRoot, "No Retries Left",
                    $"THE EXPEDITION HAS NO RETRIES LEFT.\n\nDUNGEON {act} CAN ONLY BE CLEARED OR LOST.",
                    "Back", OpenPause, OpenPause);
                return;
            }
            string body = $"DUNGEON {act} ENDS AND CAN'T BE RESUMED. COSTS 1 RETRY ({retries} LEFT).\n\n" +
                $"LOST: SPOILS {gold}, EXP, SP, SKILLS AND ITEMS GAINED IN THIS DUNGEON.\n" +
                (act > 1 ? $"KEPT: EVERYTHING COMMITTED BY DUNGEONS 1-{act - 1}, GOLD AND RELICS." : "KEPT: GOLD AND RELICS.");

            FigmaUi.ShowConfirmationModal(
                UiRoot,
                "Return to Hub?",
                body,
                "Return to Hub",
                () =>
                {
                    Run?.AbandonExpedition();
                    SaveManager.SyncRetryState(Run);
                    Game.Go(GameState.Hub);
                },
                OpenPause);
        }

        string TypeName(NodeType t) => t switch
        {
            NodeType.Battle => "Combat", NodeType.Elite => "Elite", NodeType.Event => "Event", NodeType.Treasure => "Treasure",
            NodeType.Shop => "Shop", NodeType.Rest => "Rest",
            NodeType.Boss => currentMap != null && !currentMap.IsFinalStage ? "Stage Boss" : "Final",
            _ => t.ToString(),
        };

        string Risk(NodeType t) => t switch
        {
            NodeType.Battle => "COMBAT",
            NodeType.Elite => "HIGH · ELITE FOES",
            NodeType.Boss => currentMap != null && !currentMap.IsFinalStage ? $"STAGE {currentMap.stage} BOSS" : "FINAL ENCOUNTER",
            NodeType.Event => "UNKNOWN",
            _ => "NONE KNOWN",
        };

        static string Glyph(NodeType t) => t switch
        {
            NodeType.Battle => "VS", NodeType.Elite => "EL", NodeType.Event => "?", NodeType.Shop => "SH",
            NodeType.Rest => "ZZ", NodeType.Treasure => "TR", _ => "◆",
        };

        static void Enter(NodeDef n)
        {
            Sfx.Play(Sound.MapStep);
            Run.Enter(n);
            Game.Go(Game.StateFor(n));
        }

        public static string EnemyNames(EncounterDef e) => EnemyNames(e.enemies);

        public static string EnemyNames(IEnumerable<string> enemyIds) => string.Join(", ", enemyIds.GroupBy(id => id)
            .Select(g => (g.Count() > 1 ? g.Count() + "X " : "") + Database.Enemies[g.Key].name.ToUpper()));

        string Describe(NodeDef n)
        {
            string[] nodeEnemies = n.IsCombat ? (n.enemies ?? Database.Encounter(n.encounter).enemies) : null;
            return n.type switch
            {
                NodeType.Shop => "A WANDERING MERCHANT HAS SET UP CAMP. BUY SUPPLIES WITH SPOILS.",
                NodeType.Event => "SOMETHING STIRS IN THE DARK. OUTCOME UNKNOWN.",
                NodeType.Treasure => "A CACHE OF SPOILS. CHOOSE ONE PRIZE.",
                NodeType.Rest => "A QUIET ALCOVE. RECOVER THE PARTY OR IMPROVE A HERO.",
                NodeType.Elite => EnemyNames(nodeEnemies) + ". DANGEROUS — VALUABLE BOUNTY.",
                NodeType.Boss => currentMap != null && !currentMap.IsFinalStage
                    ? $"STAGE {currentMap.stage} BOSS. " + EnemyNames(nodeEnemies) + $". DEFEAT TO ADVANCE TO STAGE {currentMap.stage + 1}."
                    : "THE FINAL ENCOUNTER OF THIS DUNGEON. " + EnemyNames(nodeEnemies) + ".",
                _ => EnemyNames(nodeEnemies),
            };
        }
    }
}
