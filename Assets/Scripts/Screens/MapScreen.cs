using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Dungeon Map Screen conforming to UI/UX Overhaul Specification (Section 4.3)
    // 640x360 Reference Resolution, Constellation Path Visuals, Bottom Party Deck with Vitality Gauges.
    public class MapScreen : GameScreen
    {
        const int StartX = 36, X0 = 80, CY = 145, DY = 44;

        Label tooltip;
        MapDef currentMap;
        readonly Dictionary<Selectable, NodeDef> nodeOf = new Dictionary<Selectable, NodeDef>();

        protected override void Build()
        {
            var map = currentMap = Run?.Map ?? MapGenerator.Generate(1);
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // 1. Top Command Header (640x26)
            Ui.Panel(Root, 0, 0, Ui.Width, 26, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Label(Root, $"ACT {map.act}  •  FLOOR {(Run != null ? Run.Floor : 0)}/{map.Floors}", 14, 7, Palette.AntiqueGoldBright, Align.Left, 1, FontKind.Serif);

            var invBtn = Ui.Button(Root, "INVENTORY", 220, 4, 88, () => Game.Go(GameState.Inventory), 18, showCursor: false);
            invBtn.tooltip = "INSPECT INVENTORY & EQUIPMENT LOADOUT";

            int goldVal = Run != null ? Run.Gold : 0;
            int itemsVal = Run?.Items != null ? Run.Items.Count : 0;
            int relicsVal = Run?.Relics != null ? Run.Relics.Count : 0;
            Ui.Label(Root, $"GOLD: {goldVal}G    ITEMS: {itemsVal}    RELICS: {relicsVal}", 626, 7, Palette.WarmIvory, Align.Right);

            var available = Run != null ? Run.Available.ToList() : map.nodes.Where(n => n.floor == 0).ToList();
            var ahead = Ahead(available);
            var current = Run?.Current;
            var cleared = Run?.Cleared ?? new HashSet<string>();

            // Calculate floor X step to spread across 640px
            int dx = map.Floors > 0 ? (Ui.Width - 140) / Mathf.Max(1, map.Floors) : 48;

            // Constellation Entrance: where the expedition begins
            Ui.Image(Root, PixelArt.Disc(5, current == null ? Palette.AntiqueGoldBright : Palette.WarmIvory), StartX - 5, CY - 5, Ui.World + 1);
            Ui.Label(Root, "START", StartX, CY + 8, Palette.MutedGray, Align.Center);
            foreach (var n in map.nodes.Where(n => n.floor == 0))
            {
                var nCenter = Center(n, dx);
                Dots(new Vector2Int(StartX, CY), nCenter, current == null ? Palette.AntiqueGoldBright : cleared.Contains(n.id) ? Palette.WarmIvory : Palette.DarkBorder);
            }

            // Constellation Paths between nodes
            foreach (var n in map.nodes)
            {
                var nCenter = Center(n, dx);
                foreach (var id in n.next)
                {
                    var m = map.Get(id);
                    var mCenter = Center(m, dx);
                    var color = cleared.Contains(n.id) && cleared.Contains(m.id) ? Palette.WarmIvory
                        : n == current && available.Contains(m) ? Palette.AntiqueGoldBright
                        : ahead.Contains(n) && ahead.Contains(m) ? Palette.AntiqueGoldDim
                        : Palette.DarkBorder;
                    Dots(nCenter, mCenter, color);
                }
            }

            // 2. Floating Node Dossier / Tooltip Card
            Ui.Image(Root, PixelArt.Box(420, 20, Palette.DeepCharcoal, Palette.AntiqueGold), 110, 266, Ui.Panels);
            tooltip = Ui.Label(Root, "SELECT A PATH FORWARD TO PROCEED", 320, 271, Palette.WarmIvory, Align.Center, order: Ui.Text);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => tooltip.Set(nodeOf.TryGetValue(s, out var nd) ? Describe(nd) : (s.tooltip ?? ""));

            // IMPORTANT: Add available nodes first so keyboard Enter triggers immediate forward progression
            foreach (var n in map.nodes)
            {
                var view = NodeView(n, available.Contains(n), ahead.Contains(n), dx);
                nodeOf[view] = n;
                if (available.Contains(n)) menu.Add(view);
            }

            menu.Add(invBtn);

            // 3. Modern Bottom Party Deck (640x64)
            Ui.Panel(Root, 0, 294, Ui.Width, 66, Palette.DeepCharcoal, Palette.AntiqueGold);
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var u = party[i];
                int bx = 14 + i * 136;

                // Hero miniature card container
                Ui.Image(Root, PixelArt.Box(130, 56, Palette.MidnightNavy, Palette.DarkBorder), bx, 299, Ui.Panels + 1);

                // Hero name & vitality values
                Color nameColor = u.Alive ? Palette.WarmIvory : Palette.SpecCrimson;
                Ui.Label(Root, PixelFont.Fit(u.name.ToUpperInvariant(), 118), bx + 8, 303, nameColor, Align.Left, 1, FontKind.Serif);

                Ui.Label(Root, $"HP {u.hp}/{u.stats.maxHp}", bx + 8, 318, u.Alive ? Palette.WarmIvory : Palette.SpecCrimson);
                Ui.Label(Root, $"MP {u.mp}", bx + 82, 318, Palette.ArcaneViolet);

                // Emerald HP gauge & Arcane Violet MP gauge
                int hpWidth = Mathf.RoundToInt(68f * Mathf.Clamp01((float)u.hp / Mathf.Max(1, u.stats.maxHp)));
                Ui.Image(Root, PixelArt.GaugeBar(70, 7, Palette.Emerald, (float)u.hp / Mathf.Max(1, u.stats.maxHp)), bx + 8, 332, Ui.Widget);

                int mpWidth = Mathf.RoundToInt(38f * Mathf.Clamp01((float)u.mp / Mathf.Max(1, u.stats.maxMp)));
                Ui.Image(Root, PixelArt.GaugeBar(40, 7, Palette.ArcaneViolet, (float)u.mp / Mathf.Max(1, u.stats.maxMp)), bx + 82, 332, Ui.Widget);
            }

            var statusBtn = Ui.Button(Root, "STATUS", 562, 310, 66, () => Game.Go(GameState.CharacterStatus), 24);
            statusBtn.tooltip = "INSPECT PARTY PRIMARY STATS, DERIVED STATS, AND ATTRIBUTES";
            menu.Add(statusBtn);
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

        void Dots(Vector2Int a, Vector2Int b, Color color)
        {
            float len = Vector2.Distance(a, b);
            for (float d = 12; d < len - 10; d += 6)
            {
                var p = Vector2.Lerp(a, b, d / len);
                Ui.Image(Root, PixelArt.Pixel, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Ui.World, color);
            }
        }

        Selectable NodeView(NodeDef n, bool available, bool ahead, int dx)
        {
            bool boss = n.type == NodeType.Boss;
            int size = boss ? 24 : 18;
            var c = Center(n, dx);
            bool cleared = Run != null && Run.Cleared.Contains(n.id);
            bool current = Run != null && n == Run.Current;

            var go = new GameObject("Node:" + n.id);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(c.x - size / 2, -(c.y - size / 2), 0);

            var box = Ui.Image(go.transform, null, 0, 0, Ui.Widget);
            var tint = available || current ? Color.white : cleared ? Palette.MutedGray : ahead ? Palette.WarmIvory : Palette.Shade;
            Ui.Image(go.transform, PixelArt.FromRows(Art.Icon(n.type)), (size - 9) / 2, (size - 9) / 2, Ui.Widget + 1, tint);
            if (current) Ui.Image(go.transform, PixelArt.FromRows(Art.Marker), size / 2 - 2, -6, Ui.Widget + 2);
            if (boss) Ui.Label(go.transform, "BOSS", size / 2, size + 2, Palette.SpecCrimson, Align.Center, 1, FontKind.Serif);

            var fill = cleared ? Palette.DeepCharcoal : Palette.MidnightNavy;
            var border = current ? Palette.AntiqueGoldBright : available ? Palette.AntiqueGold : boss ? Palette.SpecCrimson : ahead ? Palette.DarkBorder : Palette.Dusk;
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(size, size);
            sel.visual = on => box.sprite = PixelArt.Box(size, size, on ? Palette.MidnightNavy : fill, on ? Palette.AntiqueGoldBright : border);
            sel.visual(false);
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
            NodeType.Event => "EVENT: SOMETHING STIRS IN THE DARK",
            NodeType.Treasure => "TREASURE: CHOOSE A PRIZE",
            NodeType.Rest => "REST: RECOVER OR IMPROVE A HERO",
            NodeType.Elite => "ELITE: " + EnemyNames(Database.Encounters[n.encounter]) + " — VALUABLE BOUNTY",
            _ => n.type.ToString().ToUpper() + ": " + EnemyNames(Database.Encounters[n.encounter]),
        };
    }
}
