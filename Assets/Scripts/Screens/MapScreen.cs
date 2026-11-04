using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Dungeon Map — Starlit Reliquary "K · Dungeon Map" board at 640x360:
    // run header with floor diamonds + Run Gold, node map panel with legend strip, side column (node info, party, actions).
    public class MapScreen : GameScreen
    {
        const int MapX = 12, MapY = 40, MapW = 444, MapH = 270;
        const int StartX = 30, X0 = 66, CY = 158, DY = 44;

        Label tooltip, tooltipTitle;
        MapDef currentMap;
        readonly Dictionary<Selectable, NodeDef> nodeOf = new Dictionary<Selectable, NodeDef>();

        protected override void Build()
        {
            var map = currentMap = Run?.Map ?? MapGenerator.Generate(1);
            Backdrop.Create(Root, false, 0, Palette.Hex(0x171E38), Palette.Hex(0x0B1022));

            var available = Run != null ? Run.Available.ToList() : map.nodes.Where(n => n.floor == 0).ToList();
            var ahead = Ahead(available);
            var current = Run?.Current;
            var cleared = Run?.Cleared ?? new HashSet<string>();
            int floor = Run != null ? Run.Floor : 0;

            // 1. Run header
            Ui.Label(Root, $"DUNGEON {map.act} OF 8: {map.name.ToUpperInvariant()} (FLOOR {floor}/{map.Floors})", 12, 8, Palette.Mist3);
            Ui.Label(Root, $"ACT {map.act}", 12, 19, Palette.Gold2, Align.Left, 1, FontKind.Serif);
            for (int i = 0; i < map.Floors; i++)
            {
                var pip = i < floor - 1 ? PixelArt.Diamond(9, Palette.Mist3)
                    : i == floor - 1 ? PixelArt.Diamond(9, Palette.Gold2)
                    : PixelArt.Diamond(9, Palette.Ink5, false);
                Ui.Image(Root, pip, 70 + i * 12, 18, Ui.Text);
            }
            int goldVal = Run != null ? Run.Gold : 0;
            int itemsVal = Run?.Items != null ? Run.Items.Count : 0;
            int relicsVal = Run?.Relics != null ? Run.Relics.Count : 0;
            Ui.Label(Root, $"ITEMS {itemsVal}   RELICS {relicsVal}/{RelicManager.MaxRelicCapacity}", 516, 19, Palette.Mist2, Align.Right);
            Ui.Badge(Root, $"{goldVal} RUN GOLD", 528, 14, 100, 17, Palette.Hex(0x22120A), Palette.Ember2, Palette.Ember1);

            // 2. Map panel
            Ui.Panel(Root, MapX, MapY, MapW, MapH, Palette.Hex(0x070A14, 230));
            int dx = (MapW - (X0 - MapX) - 30) / Mathf.Max(1, map.Floors - 1);

            Ui.Image(Root, PixelArt.Diamond(11, current == null ? Palette.Gold2 : Palette.Mist3), StartX - 5, CY - 5, Ui.World + 1);
            foreach (var n in map.nodes.Where(n => n.floor == 0))
                Dots(new Vector2Int(StartX, CY), Center(n, dx), current == null ? Palette.Mist1 : cleared.Contains(n.id) ? Palette.Mist3 : Palette.Ink4);

            foreach (var n in map.nodes)
                foreach (var id in n.next)
                {
                    var m = map.Get(id);
                    var color = cleared.Contains(n.id) && cleared.Contains(m.id) ? Palette.Mist3
                        : n == current && available.Contains(m) ? Palette.Mist1
                        : ahead.Contains(n) && ahead.Contains(m) ? Palette.Ink5
                        : Palette.Ink4;
                    Dots(Center(n, dx), Center(m, dx), color);
                }

            // Legend strip
            Ui.Rect(Root, MapX + 1, MapY + MapH - 24, MapW - 2, 1, Palette.Ink4, Ui.Panels + 1);
            Ui.Rect(Root, MapX + 1, MapY + MapH - 23, MapW - 2, 21, Palette.Ink1, Ui.Panels + 1);
            var legend = new[] { (NodeType.Battle, "BATTLE"), (NodeType.Elite, "ELITE"), (NodeType.Event, "EVENT"), (NodeType.Treasure, "TREASURE"), (NodeType.Shop, "SHOP"), (NodeType.Rest, "REST"), (NodeType.Boss, "BOSS") };
            int lx = MapX + 10;
            foreach (var (type, name) in legend)
            {
                Ui.Image(Root, PixelArt.FromRows(Art.Icon(type)), lx, MapY + MapH - 17, Ui.Widget, Palette.Mist2);
                Ui.Label(Root, name, lx + 12, MapY + MapH - 16, Palette.Mist2);
                lx += 16 + PixelFont.Measure(name) + 12;
            }

            // 3. Side column: node info
            Ui.Panel(Root, 466, MapY, 162, 112, Palette.Ink2, Palette.Gold3);
            tooltipTitle = Ui.Header(Root, "SELECT A PATH", 480, MapY + 8, 150);
            tooltip = Ui.Label(Root, PixelFont.Wrap("ONE-WAY: THE OTHER PATHS CLOSE ONCE YOU MOVE.", 140), 474, MapY + 28, Palette.Mist2);

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s =>
            {
                if (nodeOf.TryGetValue(s, out var nd))
                {
                    tooltipTitle.Set(nd.type.ToString().ToUpperInvariant() + " NODE");
                    tooltip.Set(PixelFont.Wrap(Describe(nd), 140));
                }
                else
                {
                    tooltipTitle.Set("PARTY");
                    tooltip.Set(PixelFont.Wrap(s.tooltip ?? "", 140));
                }
            };

            // Available nodes first so keyboard Enter triggers immediate forward progression
            foreach (var n in map.nodes)
            {
                var view = NodeView(n, available.Contains(n), ahead.Contains(n), dx);
                nodeOf[view] = n;
                if (available.Contains(n)) menu.Add(view);
            }

            // Party panel
            Ui.Panel(Root, 466, 160, 162, 114);
            Ui.Header(Root, "PARTY", 480, 168, 150);
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var u = party[i];
                int py = 186 + i * 21;
                float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp);
                Ui.Label(Root, PixelFont.Fit(u.name.ToUpperInvariant(), 58), 474, py + 3, u.Alive ? Palette.Mist1 : Palette.HpLow);
                Ui.Image(Root, PixelArt.GaugeBar(84, 5, hp < 0.25f ? Palette.HpLow : Palette.Hp, hp), 536, py, Ui.Widget);
                Ui.Image(Root, PixelArt.GaugeBar(84, 5, Palette.Mp, (float)u.mp / Mathf.Max(1, u.stats.maxMp)), 536, py + 7, Ui.Widget);
            }

            // Actions
            var invBtn = Ui.Button(Root, "ITEMS", 466, 282, 78, () => Game.Go(GameState.Inventory), 22);
            invBtn.tooltip = "INSPECT INVENTORY & EQUIPMENT LOADOUT";
            menu.Add(invBtn);
            var statusBtn = Ui.Button(Root, "STATUS", 550, 282, 78, () => Game.Go(GameState.CharacterStatus), 22);
            statusBtn.tooltip = "INSPECT PARTY PRIMARY STATS, DERIVED STATS, AND ATTRIBUTES";
            menu.Add(statusBtn);

            int hx = Ui.KeyHint(Root, 12, 326, "CHOOSE", "ARROWS");
            Ui.KeyHint(Root, hx, 326, "TRAVEL", "ENTER");
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

        // Dashed path between nodes (.lnk)
        void Dots(Vector2Int a, Vector2Int b, Color color)
        {
            float len = Vector2.Distance(a, b);
            for (float d = 12; d < len - 10; d += 1)
            {
                if ((int)d % 6 >= 3) continue;
                var p = Vector2.Lerp(a, b, d / len);
                Ui.Image(Root, PixelArt.Pixel, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Ui.World, color);
            }
        }

        // Map node (.nd): cleared = muted, current = gold, choosable = white frame, ahead = ink, unreachable = dim.
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
            var tint = current ? Palette.Gold1 : available ? Palette.Mist1 : cleared ? Palette.Mist3 : ahead ? Palette.Mist2 : Palette.Ink5;
            Ui.Image(go.transform, PixelArt.FromRows(Art.Icon(n.type)), (size - 9) / 2, (size - 9) / 2, Ui.Widget + 1, tint);
            if (current) Ui.Badge(go.transform, "YOU", size / 2 - 12, -14, 24, 11, Palette.Gold2, Palette.Gold2, Palette.Ink0);
            if (boss) Ui.Label(go.transform, "BOSS", size / 2, size + 3, Palette.HpLow, Align.Center);

            var fill = current ? Palette.Hex(0x2B2210) : available ? Palette.Ink4 : cleared ? Palette.Ink3 : Palette.Ink2;
            var border = current ? Palette.Gold2 : available ? Palette.Mist1 : cleared ? Palette.Mist3 : boss ? Palette.HpLow : Palette.Ink5;
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(size, size);
            sel.visual = on => box.sprite = PixelArt.Box(size, size, fill, on ? Palette.Gold1 : border);
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
            NodeType.Shop => "A WANDERING OUTPOST. TRADE GOLD FOR SUPPLIES AND GEAR.",
            NodeType.Event => "SOMETHING STIRS IN THE DARK. OUTCOME UNKNOWN.",
            NodeType.Treasure => "A CACHE OF SPOILS. CHOOSE ONE PRIZE.",
            NodeType.Rest => "RECOVER THE PARTY OR IMPROVE A HERO.",
            NodeType.Elite => EnemyNames(Database.Encounters[n.encounter]) + ". DANGEROUS - VALUABLE BOUNTY.",
            _ => EnemyNames(Database.Encounters[n.encounter]),
        };
    }
}
