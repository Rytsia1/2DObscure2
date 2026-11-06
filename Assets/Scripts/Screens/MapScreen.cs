using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Dungeon Map — Starlit Reliquary "K · Dungeon Map" board at 640x360:
    // run header (Dungeon n of 8 · name · 8 dungeon diamonds · Run progress tag · Run Gold), node map on bricks
    // (cleared ✓ / current gold / choosable white / locked dashed / final diamond) with legend strip,
    // side column (focused node info, party vitals, Party + Travel). Esc offers to abandon the expedition.
    public class MapScreen : GameScreen
    {
        const int MapX = 12, MapY = 38, MapW = 451, MapH = 278;
        const int StartX = 32, X0 = 72, CY = 162, DY = 46, Node = 26;
        const int SideX = 470, SideW = 158;

        MapDef currentMap;
        readonly Dictionary<Selectable, NodeDef> nodeOf = new Dictionary<Selectable, NodeDef>();
        Transform info;
        SpriteRenderer arrow;
        Selectable travelBtn;
        NodeDef focused;

        protected override void Build()
        {
            var map = currentMap = Run?.Map ?? MapGenerator.Generate(1);
            Backdrop.Create(Root, false, 0, Palette.Hex(0x141A30), Palette.Hex(0x0B1022));
            Bricks();

            var available = Run != null ? Run.Available.ToList() : map.nodes.Where(n => n.floor == 0).ToList();
            var ahead = Ahead(available);
            var current = Run?.Current;
            var cleared = Run?.Cleared ?? new HashSet<string>();

            // 1. Run header
            Ui.Label(Root, $"DUNGEON {map.act} OF 8", 14, 9, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(Root, map.name, 14, 18, Palette.Gold2);
            int dxp = 14 + PixelFont.Measure(map.name, FontKind.DotGothic) + 14;
            for (int i = 1; i <= 8; i++)
            {
                var pip = i < map.act ? PixelArt.Diamond(9, Palette.Mist3)
                    : i == map.act ? PixelArt.Diamond(9, Palette.Gold2)
                    : PixelArt.Diamond(9, Palette.Ink5, false);
                Ui.Image(Root, pip, dxp + (i - 1) * 11, 19, Ui.Text);
            }
            int gold = Run != null ? Run.Gold : 0;
            int gw = Mathf.Max(84, PixelFont.Measure($"{gold:#,##0} RUN GOLD", FontKind.DotGothic) + 16);
            Ui.CurrencyTag(Root, gold, false, 628 - gw, 11);
            int tw = PixelFont.Measure("RUN PROGRESS") + 8;
            Ui.Tag(Root, "RUN PROGRESS", 628 - gw - 6 - tw, 14, TagKind.Temp, 13);

            // 2. Map panel
            Ui.Panel(Root, MapX, MapY, MapW, MapH, Palette.Hex(0x070A14, 224));
            int dx = (MapW - (X0 - MapX) - 34) / Mathf.Max(1, map.Floors - 1);

            // Entrance diamond + links
            Ui.Image(Root, PixelArt.Diamond(13, current == null ? Palette.Gold2 : Palette.Mist3), StartX - 6, CY - 6, Ui.World + 1);
            foreach (var n in map.nodes.Where(n => n.floor == 0))
                Link(new Vector2Int(StartX, CY), Center(n, dx), current == null ? LinkState.Open : cleared.Contains(n.id) ? LinkState.Done : LinkState.Locked);
            foreach (var n in map.nodes)
                foreach (var id in n.next)
                {
                    var m = map.Get(id);
                    var state = cleared.Contains(n.id) && cleared.Contains(m.id) ? LinkState.Done
                        : n == current && available.Contains(m) ? LinkState.Open
                        : LinkState.Locked;
                    Link(Center(n, dx), Center(m, dx), state);
                }

            // Legend strip
            int ly = MapY + MapH - 24;
            Ui.Rect(Root, MapX + 2, ly, MapW - 4, 1, Palette.Ink4, Ui.Panels + 1);
            Ui.Rect(Root, MapX + 2, ly + 1, MapW - 4, 21, Palette.Ink1, Ui.Panels + 1);
            int lx = MapX + 8;
            foreach (var (glyph, name, dashed) in new[] { ("VS", "COMBAT", false), ("?", "EVENT", false), ("SH", "SHOP", false), ("ZZ", "REST", false), ("TR", "TREASURE", false) })
            {
                int bw = PixelFont.Measure(glyph) + 6;
                Ui.Image(Root, dashed ? PixelArt.DashedBadge(bw, 11, Color.clear, Palette.Mist3) : PixelArt.Badge(bw, 11, Color.clear, Palette.Mist3), lx, ly + 6, Ui.Widget);
                Ui.Label(Root, glyph, lx + 3, ly + 8, Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                Ui.Label(Root, name, lx + bw + 3, ly + 8, Palette.Mist2, font: FontKind.Interface);
                lx += bw + 6 + PixelFont.Measure(name) + 8;
            }
            Ui.Image(Root, PixelArt.Diamond(11, Palette.Ink4), lx, ly + 6, Ui.Widget);
            Ui.Label(Root, "FINAL", lx + 14, ly + 8, Palette.Mist2, font: FontKind.Interface);
            Ui.Label(Root, "GOLD = CURRENT · WHITE = CHOOSABLE · DASHED = LOCKED", MapX + MapW - 8, ly + 8, Palette.Mist3, Align.Right, font: FontKind.Interface);

            // 3. Side column: node info (gold), party, actions
            info = new GameObject("NodeInfo").transform;
            info.SetParent(Root, false);
            Ui.Panel(Root, SideX, MapY, SideW, 132, Palette.Ink2, Palette.Gold3);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = OnAbandonClicked;
            arrow = Ui.Image(Root, PixelArt.FromRows(new[] { "GGGGGGG", ".GGGGG.", "..GGG..", "...G..." }), 0, 0, Ui.Overlay - 1, Palette.Mist1);
            arrow.gameObject.SetActive(false);

            foreach (var n in map.nodes)
            {
                var view = NodeView(n, available.Contains(n), dx);
                nodeOf[view] = n;
                if (available.Contains(n)) menu.Add(view);
            }

            // Party panel
            int py = MapY + 140;
            Ui.Panel(Root, SideX, py, SideW, 114);
            Ui.Header(Root, "Party", SideX + 8, py + 6, SideW - 4);
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var u = party[i];
                int y = py + 24 + i * 22;
                Color c = Palette.ForClass(u.classDef?.id);
                Ui.Image(Root, PixelArt.Box(16, 16, Color.Lerp(c, Palette.Ink1, 0.7f), Palette.Ink5), SideX + 7, y, Ui.Widget);
                Ui.Rect(Root, SideX + 13, y + 3, 4, 4, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 1);
                Ui.Rect(Root, SideX + 10, y + 10, 10, 3, c, Ui.Widget + 1);
                Ui.Label(Root, PixelFont.Fit(u.name, 48, FontKind.DotGothic), SideX + 28, y + 2, u.Alive ? Palette.Mist1 : Palette.HpLow);
                float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp);
                Ui.Image(Root, PixelArt.GaugeBar(SideW - 88, 4, Mathf.Clamp01(hp), hp < 0.25f ? Palette.HpLow : Palette.Hp), SideX + 80, y + 3, Ui.Widget);
                Ui.Image(Root, PixelArt.GaugeBar(SideW - 88, 4, (float)u.mp / Mathf.Max(1, u.stats.maxMp), Palette.Mp), SideX + 80, y + 9, Ui.Widget);
            }

            // Actions
            var partyBtn = Ui.Button(Root, "Party", SideX, 324, 50, () => Game.Go(GameState.Party), 22, showCursor: false);
            partyBtn.tooltip = "PARTY, STATUS AND EQUIPMENT";
            menu.Add(partyBtn);
            travelBtn = Ui.Button(Root, "Travel", SideX + 56, 324, SideW - 56, () => { if (focused != null) Enter(focused); }, 22, showCursor: false, style: ButtonStyle.Primary);
            menu.Add(travelBtn);

            int hx = Ui.KeyHint(Root, MapX, 328, "CHOOSE PATH", "UP", "DN");
            hx = Ui.KeyHint(Root, hx, 328, "TRAVEL", "ENTER");
            Ui.KeyHint(Root, hx, 328, "PAUSE", "ESC");

            menu.onFocus = s => { if (nodeOf.TryGetValue(s, out var nd)) ShowNode(nd, s); };
            if (available.Count > 0) ShowNode(available[0], nodeOf.First(kv => kv.Value == available[0]).Key);
            else ShowNode(null, null);
        }

        // Brick wall texture behind the map (.bricks)
        void Bricks()
        {
            var col = Palette.Hex(0x0B1022);
            for (int y = 15; y < Ui.Height; y += 16) Ui.Rect(Root, 0, y, Ui.Width, 1, col, Ui.Back + 1);
            for (int y = 0; y < Ui.Height; y += 16)
                for (int x = (y / 16) % 2 * 16; x < Ui.Width; x += 32) Ui.Rect(Root, x, y, 1, 15, col, Ui.Back + 1);
        }

        void ShowNode(NodeDef n, Selectable view)
        {
            focused = n;
            foreach (Transform c in info.Cast<Transform>().ToList())
            {
                if (Application.isPlaying) Object.Destroy(c.gameObject);
                else Object.DestroyImmediate(c.gameObject);
            }
            int x = SideX + 8, w = SideW - 16, y = MapY + 24;
            Ui.Header(info, n == null ? "Select a path" : $"{TypeName(n.type)} node", SideX + 8, MapY + 6, SideW - 4);
            string desc = PixelFont.Wrap(n == null ? "NO PATH IS OPEN." : Describe(n), w);
            Ui.Label(info, desc, x, y, Palette.Mist1, font: FontKind.Interface);
            y += (desc.Count(c => c == '\n') + 1) * PixelFont.LineHeight + 4;
            if (n != null)
            {
                Ui.Label(info, "CURRENCY", x, y + 2, Palette.Mist2, font: FontKind.Interface);
                if (n.type == NodeType.Shop) Ui.Tag(info, "RUN GOLD", x + w - PixelFont.Measure("RUN GOLD") - 8, y, TagKind.Temp);
                else Ui.Label(info, "—", x + w, y + 2, Palette.Mist3, Align.Right, font: FontKind.Interface);
                Ui.Rect(info, x, y + 12, w, 1, Palette.Ink3, Ui.Panels + 2);
                y += 15;
                Ui.Label(info, "RISK", x, y + 2, Palette.Mist2, font: FontKind.Interface);
                Ui.Label(info, Risk(n.type), x + w, y + 2, n.type == NodeType.Boss || n.type == NodeType.Elite ? Palette.Err : Palette.Mist1, Align.Right, font: FontKind.Interface);
                Ui.Rect(info, x, y + 12, w, 1, Palette.Ink3, Ui.Panels + 2);
            }
            Ui.Image(info, PixelArt.Badge(w, 22, Palette.Ink0, Palette.Ink4), x, MapY + 104, Ui.Panels + 2);
            Ui.Label(info, "!", x + 5, MapY + 111, Palette.Gold2, font: FontKind.Interface);
            Ui.Label(info, PixelFont.Wrap("ONE-WAY: OTHER PATHS CLOSE ONCE YOU MOVE.", w - 16), x + 12, MapY + 107, Palette.Mist2, font: FontKind.Interface);

            if (travelBtn != null)
            {
                travelBtn.interactable = n != null;
                travelBtn.label.Set(n == null ? "Travel" : PixelFont.Fit($"Travel to {TypeName(n.type)}", SideW - 66, FontKind.DotGothic));
                travelBtn.visual(travelBtn.menu != null && travelBtn.menu.Current == travelBtn);
            }
            if (view != null)
            {
                arrow.gameObject.SetActive(true);
                var p = view.transform.localPosition;
                arrow.transform.localPosition = new Vector3(p.x + view.size.x / 2 - 3, p.y + 10, 0);
            }
            else arrow.gameObject.SetActive(false);
        }

        static string TypeName(NodeType t) => t switch
        {
            NodeType.Battle => "Combat", NodeType.Elite => "Elite", NodeType.Event => "Event", NodeType.Treasure => "Treasure",
            NodeType.Shop => "Shop", NodeType.Rest => "Rest", NodeType.Boss => "Final", _ => t.ToString(),
        };

        static string Risk(NodeType t) => t switch
        {
            NodeType.Battle => "COMBAT",
            NodeType.Elite => "HIGH · ELITE FOES",
            NodeType.Boss => "FINAL ENCOUNTER",
            NodeType.Event => "UNKNOWN",
            _ => "NONE KNOWN",
        };

        static string Glyph(NodeType t) => t switch
        {
            NodeType.Battle => "VS", NodeType.Elite => "VS", NodeType.Event => "?", NodeType.Shop => "SH",
            NodeType.Rest => "ZZ", NodeType.Treasure => "TR", _ => "!",
        };

        // ------------------------------------------------------------ U · Pause

        GameObject pause;
        bool abandonOpen;

        // Esc on the map opens the pause menu (Starlit Reliquary "U · Pause Menu (dungeon)").
        void OnAbandonClicked() { abandonOpen = false; OpenPause(); }

        void ClosePause()
        {
            if (pause == null) return;
            if (Application.isPlaying) Object.Destroy(pause);
            else Object.DestroyImmediate(pause);
            pause = null;
        }

        void OpenPause()
        {
            ClosePause();
            pause = new GameObject("Pause");
            pause.transform.SetParent(Root, false);
            var t = pause.transform;
            int O = Ui.Overlay;
            Ui.Rect(t, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 204), O - 6);

            // Pause panel (gold)
            const int px = 75, py = 45, pw = 210, ph = 252;
            Ui.Image(t, PixelArt.Box(pw, ph, Palette.Ink2, Palette.Gold3), px, py, O - 5);
            Ui.Label(t, "Paused", px + 10, py + 10, Palette.Gold2, Align.Left, 2, FontKind.DotGothic, O);
            var menu = pause.AddComponent<Menu>();
            menu.onBack = () => { if (abandonOpen) { abandonOpen = false; OpenPause(); } else ClosePause(); };

            int y = py + 38;
            Selectable Item(string text, System.Action go, bool enabled = true, string note = null, Color? ink = null)
            {
                var b = Ui.Button(t, text, px + 22, y, pw - 30, go, 18, interactable: enabled, style: ButtonStyle.Flat);
                foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += O;
                if (ink.HasValue && enabled)
                {
                    var baseVisual = b.visual;
                    b.visual = on => { baseVisual(on); if (!on) b.label.Color = ink.Value; };
                    b.visual(false);
                }
                if (note != null) Ui.Label(b.transform, note, pw - 34, 6, Palette.Mist3, Align.Right, order: O + Ui.Widget + 1, font: FontKind.Interface);
                y += 20;
                if (enabled) menu.Add(b);
                return b;
            }
            var resume = Item("Resume", ClosePause);
            Ui.Badge(resume.transform, "ESC", pw - 54, 3, 20, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, order: O + Ui.Widget + 1, font: FontKind.Interface);
            Item("QTE Assists", () => Game.Go(GameState.Settings));
            Item("Settings", () => Game.Go(GameState.Settings));
            Item("Save Game", null, false, "HUB ONLY");
            Item("Load Game", null, false, "HUB OR MAIN MENU");
            y += 4;
            Ui.Rect(t, px + 10, y - 2, pw - 20, 1, Palette.Ink5, O - 4);
            y += 4;
            Item("Return to Hub...", () => { abandonOpen = true; OpenPause(); }, true, "ABANDON", Palette.Hex(0xFFC7BB));

            // Footer: where you are, Run Gold, assist note
            int fy = py + ph - 62;
            Ui.Rect(t, px + 2, fy, pw - 4, 1, Palette.Ink4, O - 4);
            Ui.Label(t, $"DUNGEON {Run?.Act ?? 1} OF 8 · NODE {Run?.Floor ?? 1}", px + 10, fy + 8, Palette.Mist2, order: O, font: FontKind.Interface);
            int gold = Run != null ? Run.Gold : 0;
            Ui.Image(t, PixelArt.Coin(7, false), px + pw - 18 - PixelFont.Measure(gold.ToString()), fy + 8, O);
            Ui.Label(t, gold.ToString(), px + pw - 10, fy + 8, Palette.Ember1, Align.Right, order: O, font: FontKind.Interface);
            var assist = (Game?.Settings ?? GameSettings.Load()).Assist;
            if (assist != QteAssist.Standard)
            {
                int aw = Ui.Tag(t, "ASSIST", px + 10, fy + 20, TagKind.Temp, 11, O);
                Ui.Label(t, $"{assist.ToString().ToUpperInvariant()} ACTIVE · NOTED ON RUN SUMMARY", px + 14 + aw, fy + 22, Palette.Mist2, order: O, font: FontKind.Interface);
            }
            Ui.Label(t, PixelFont.Wrap("DUNGEON RUNS CAN'T BE SAVED OR RESUMED. PROGRESS IS COMMITTED ONLY BY DUNGEON CLEAR.", pw - 20), px + 10, fy + 36, Palette.Mist3, order: O, font: FontKind.Interface);

            if (abandonOpen) BuildAbandon(t, O);
        }

        // "Return to Hub?" dialog (.dlg, ember frame): what is lost vs kept.
        void BuildAbandon(Transform t, int O)
        {
            const int dx = 320, dy = 105, dw = 270, dh = 118;
            Ui.Image(t, PixelArt.Box(dw, dh, Palette.Ink2, Palette.Ember2), dx, dy, O + 50);
            Ui.Label(t, "Return to Hub?", dx + 10, dy + 9, Palette.Gold1, order: O + 51);
            int act = Run?.Act ?? 1, gold = Run != null ? Run.Gold : 0;
            Ui.Label(t, $"DUNGEON {act} ENDS AND CAN'T BE RESUMED.", dx + 10, dy + 26, Palette.Mist1, order: O + 51, font: FontKind.Interface);
            int lw = Ui.Tag(t, "LOST", dx + 10, dy + 39, TagKind.Temp, 11, O + 51);
            Ui.Label(t, PixelFont.Wrap($"RUN GOLD {gold}, AND THE EXP, SP, SKILLS AND ITEMS GAINED IN THIS DUNGEON.", dw - lw - 26), dx + 16 + lw, dy + 41, Palette.Mist2, order: O + 51, font: FontKind.Interface);
            int kw = Ui.Tag(t, "KEPT", dx + 10, dy + 62, TagKind.Keep, 11, O + 51);
            Ui.Label(t, PixelFont.Wrap(act > 1 ? $"EVERYTHING COMMITTED BY DUNGEONS 1-{act - 1}, HUB GOLD AND RELICS." : "HUB GOLD AND RELICS.", dw - kw - 26), dx + 16 + kw, dy + 64, Palette.Mist2, order: O + 51, font: FontKind.Interface);
            Ui.Rect(t, dx + 2, dy + dh - 30, dw - 4, 28, Palette.Ink1, O + 51);
            Ui.Rect(t, dx + 2, dy + dh - 30, dw - 4, 1, Palette.Ink4, O + 51);

            var go = new GameObject("AbandonDialog");
            go.transform.SetParent(t, false);
            var menu = go.AddComponent<Menu>();
            menu.onBack = () => { abandonOpen = false; OpenPause(); };
            var leave = Ui.Button(t, "Return to Hub", dx + dw - 226, dy + dh - 25, 96, () =>
            {
                Run?.AbandonExpedition();
                SaveManager.AutoSave(out _);
                Game.Go(GameState.Hub);
            }, 20, showCursor: false, style: ButtonStyle.Danger);
            var stay = Ui.Button(t, "Continue Expedition", dx + dw - 124, dy + dh - 25, 114, () => { abandonOpen = false; ClosePause(); }, 20, showCursor: false, style: ButtonStyle.Primary);
            foreach (var b in new[] { leave, stay })
                foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += O + 50;
            menu.Add(leave);
            menu.Add(stay);
            menu.Focus(1, false, true);
        }

        HashSet<NodeDef> Ahead(List<NodeDef> from)
        {
            var seen = new HashSet<NodeDef>();
            var stack = new Stack<NodeDef>(from);
            while (stack.Count > 0)
            {
                var n = stack.Pop();
                if (!seen.Add(n)) continue;
                foreach (var id in n.next) stack.Push(currentMap.Get(id));
            }
            return seen;
        }

        Vector2Int Center(NodeDef n, int dx)
        {
            int count = currentMap.nodes.Count(m => m.floor == n.floor);
            return new Vector2Int(X0 + n.floor * dx, CY + Mathf.RoundToInt((n.lane - (count - 1) / 2f) * DY));
        }

        enum LinkState { Locked, Open, Done }

        // Path between nodes (.lnk): done = solid mist, open = dashed white, locked = dashed ink.
        void Link(Vector2Int a, Vector2Int b, LinkState state)
        {
            float len = Vector2.Distance(a, b);
            Color color = state == LinkState.Done ? Palette.Mist3 : state == LinkState.Open ? Palette.Mist1 : Palette.Ink5;
            for (float d = 15; d < len - 14; d += 1)
            {
                if (state != LinkState.Done && (int)d % 7 >= 4) continue;
                var p = Vector2.Lerp(a, b, d / len);
                Ui.Image(Root, PixelArt.Pixel, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Ui.World, color).transform.localScale = new Vector3(2, 2, 1);
            }
        }

        // Map node (.nd): cleared = muted + ✓, current = gold + YOU, choosable = white frame, ahead/locked = dashed ink; final = diamond.
        Selectable NodeView(NodeDef n, bool available, int dx)
        {
            bool boss = n.type == NodeType.Boss;
            int size = boss ? 34 : Node;
            var c = Center(n, dx);
            bool cleared = Run != null && Run.Cleared.Contains(n.id);
            bool current = Run != null && n == Run.Current;

            var go = new GameObject("Node:" + n.id);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(c.x - size / 2, -(c.y - size / 2), 0);

            var box = Ui.Image(go.transform, null, 0, 0, Ui.Widget);
            Color ink = current ? Palette.Gold1 : available ? Palette.Mist1 : cleared ? Palette.Mist3 : Palette.Ink5;
            if (boss) Ui.Image(go.transform, PixelArt.FromRows(Art.Icon(n.type)), (size - 9) / 2, (size - 9) / 2, Ui.Widget + 1, available || current ? Palette.Mist1 : Palette.Mist3);
            else Ui.Label(go.transform, Glyph(n.type), size / 2, (size - PixelFont.GlyphHeight) / 2, ink, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
            if (n.type == NodeType.Elite && !boss) Ui.Rect(go.transform, size / 2 - 4, size - 6, 8, 1, ink, Ui.Widget + 1);
            if (cleared)
            {
                Ui.Image(go.transform, PixelArt.Badge(9, 9, Palette.Mist3, Palette.Mist3), size - 5, -4, Ui.Widget + 2);
                Ui.Label(go.transform, "✓", size - 3, -3, Palette.Ink0, order: Ui.Widget + 3, font: FontKind.Interface);
            }
            if (current) Ui.Badge(go.transform, "YOU", size / 2 - 12, -15, 24, 11, Palette.Gold2, Palette.Gold2, Palette.Ink0, font: FontKind.Interface);

            Color fill = current ? Palette.Hex(0x2B2210) : available ? Palette.Ink4 : cleared ? Palette.Ink3 : boss ? Palette.Ink4 : Palette.Ink1;
            Color border = current ? Palette.Gold2 : available ? Palette.Mist1 : cleared ? Palette.Mist3 : Palette.Ink5;
            var ring = Ui.Image(go.transform, null, -2, -2, Ui.Widget - 1);
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(size, size);
            if (!Selectable.All.Contains(sel)) Selectable.All.Add(sel);
            sel.visual = on =>
            {
                if (boss) box.sprite = PixelArt.Diamond(size, on ? Palette.Gold2 : fill);
                else box.sprite = available || current || cleared ? PixelArt.Badge(size, size, fill, on ? Palette.Gold1 : border) : PixelArt.DashedBadge(size, size, fill, border);
                ring.sprite = current && !boss ? PixelArt.Badge(size + 4, size + 4, Color.clear, Palette.Gold3) : null;
            };
            sel.visual(false);
            sel.interactable = available;
            if (available) sel.onClick = () => Enter(n);
            return sel;
        }

        static void Enter(NodeDef n)
        {
            Run.Enter(n);
            Game.Go(Game.StateFor(n));
        }

        public static string EnemyNames(EncounterDef e) => string.Join(", ", e.enemies.GroupBy(id => id)
            .Select(g => (g.Count() > 1 ? g.Count() + "X " : "") + Database.Enemies[g.Key].name.ToUpper()));

        static string Describe(NodeDef n) => n.type switch
        {
            NodeType.Shop => "A WANDERING MERCHANT HAS SET UP CAMP. BUY SUPPLIES WITH RUN GOLD.",
            NodeType.Event => "SOMETHING STIRS IN THE DARK. OUTCOME UNKNOWN.",
            NodeType.Treasure => "A CACHE OF SPOILS. CHOOSE ONE PRIZE.",
            NodeType.Rest => "A QUIET ALCOVE. RECOVER THE PARTY OR IMPROVE A HERO.",
            NodeType.Elite => EnemyNames(Database.Encounters[n.encounter]) + ". DANGEROUS — VALUABLE BOUNTY.",
            NodeType.Boss => "THE FINAL ENCOUNTER OF THIS DUNGEON. " + EnemyNames(Database.Encounters[n.encounter]) + ".",
            _ => EnemyNames(Database.Encounters[n.encounter]),
        };
    }
}
