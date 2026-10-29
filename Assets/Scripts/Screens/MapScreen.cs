using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    public class MapScreen : GameScreen
    {
        const int X0 = 44, DX = 35, CY = 82, DY = 27, StartX = 16;

        Label tooltip;
        readonly Dictionary<Selectable, NodeDef> nodeOf = new Dictionary<Selectable, NodeDef>();

        protected override void Build()
        {
            var map = Run.Map;
            Backdrop.Create(Root, false, 8, Palette.Void, Palette.Night, Palette.Dusk);

            Ui.Panel(Root, 0, 0, Ui.Width, 17);
            Ui.Label(Root, $"ACT {map.act}  FLOOR {Run.Floor}/{map.Floors}", 6, 5, Palette.Bone);
            Ui.Label(Root, $"GOLD {Run.Gold}  ITEMS {Run.Items.Count}  RELICS {Run.Relics.Count}", 314, 5, Palette.Gold, Align.Right);

            var available = Run.Available.ToList();
            var ahead = Ahead(available);

            // The entrance: where the run begins, linked to every first-floor node.
            Ui.Image(Root, PixelArt.Disc(3, Run.Current == null ? Palette.Gold : Palette.Parchment), StartX - 3, CY - 3, Ui.World + 1);
            Ui.Label(Root, "START", StartX, CY + 6, Palette.Ash, Align.Center);
            foreach (var n in map.nodes.Where(n => n.floor == 0))
                Dots(new Vector2Int(StartX, CY), Center(n), Run.Current == null ? Palette.Gold : Run.Cleared.Contains(n.id) ? Palette.Parchment : Palette.Dusk);

            foreach (var n in map.nodes)
                foreach (var id in n.next)
                {
                    var m = map.Get(id);
                    var color = Run.Cleared.Contains(n.id) && Run.Cleared.Contains(m.id) ? Palette.Parchment // path taken
                        : n == Run.Current && available.Contains(m) ? Palette.Gold                          // choose next
                        : ahead.Contains(n) && ahead.Contains(m) ? Palette.Ash                              // still reachable
                        : Palette.Dusk;                                                                     // closed off
                    Dots(Center(n), Center(m), color);
                }

            tooltip = Ui.Label(Root, "", 160, 141, Palette.Bone, Align.Center);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => tooltip.Set(Describe(nodeOf[s]));
            foreach (var n in map.nodes)
            {
                var view = NodeView(n, available.Contains(n), ahead.Contains(n));
                nodeOf[view] = n;
                if (available.Contains(n)) menu.Add(view);
            }

            Ui.Panel(Root, 0, 153, Ui.Width, 27);
            for (int i = 0; i < Run.Party.Count; i++)
            {
                var u = Run.Party[i];
                Ui.Label(Root, u.name.ToUpper(), 8 + i * 80, 157, Palette.Bone);
                Ui.Label(Root, $"{u.hp}/{u.stats.maxHp}", 8 + i * 80, 167, u.Alive ? Palette.Parchment : Palette.Crimson);
                Ui.Label(Root, $"MP{u.mp}", 52 + i * 80, 167, Palette.Lilac);
            }
        }

        // Every node still reachable from the current choices (including them).
        HashSet<NodeDef> Ahead(List<NodeDef> from)
        {
            var seen = new HashSet<NodeDef>();
            var stack = new Stack<NodeDef>(from);
            while (stack.Count > 0)
            {
                var n = stack.Pop();
                if (!seen.Add(n)) continue;
                foreach (var id in n.next) stack.Push(Run.Map.Get(id));
            }
            return seen;
        }

        Vector2Int Center(NodeDef n)
        {
            int count = Run.Map.nodes.Count(m => m.floor == n.floor);
            return new Vector2Int(X0 + n.floor * DX, CY + Mathf.RoundToInt((n.lane - (count - 1) / 2f) * DY));
        }

        void Dots(Vector2Int a, Vector2Int b, Color color)
        {
            float len = Vector2.Distance(a, b);
            for (float d = 9; d < len - 8; d += 4)
            {
                var p = Vector2.Lerp(a, b, d / len);
                Ui.Image(Root, PixelArt.Pixel, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Ui.World, color);
            }
        }

        Selectable NodeView(NodeDef n, bool available, bool ahead)
        {
            bool boss = n.type == NodeType.Boss;
            int size = boss ? 19 : 15;
            var c = Center(n);
            bool cleared = Run.Cleared.Contains(n.id), current = n == Run.Current;

            var go = new GameObject("Node:" + n.id);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(c.x - size / 2, -(c.y - size / 2), 0);

            var box = Ui.Image(go.transform, null, 0, 0, Ui.Widget);
            var tint = available || current ? Color.white : cleared ? Palette.Ash : ahead ? Palette.Parchment : Palette.Shade;
            Ui.Image(go.transform, PixelArt.FromRows(Art.Icon(n.type)), (size - 9) / 2, (size - 9) / 2, Ui.Widget + 1, tint);
            if (current) Ui.Image(go.transform, PixelArt.FromRows(Art.Marker), size / 2 - 2, -5, Ui.Widget + 2);
            if (boss) Ui.Label(go.transform, "BOSS", size / 2, size + 2, Palette.Crimson, Align.Center);

            var fill = cleared ? Palette.Void : Palette.Night;
            var border = current ? Palette.Ember : available ? Palette.Bone : boss ? Palette.Crimson : ahead ? Palette.Shade : Palette.Dusk;
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(size, size);
            sel.visual = on => box.sprite = PixelArt.Box(size, size, on ? Palette.Dusk : fill, on ? Palette.Gold : border);
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
            NodeType.Elite => "ELITE: " + EnemyNames(Database.Encounters[n.encounter]) + " - BETTER LOOT",
            _ => n.type.ToString().ToUpper() + ": " + EnemyNames(Database.Encounters[n.encounter]),
        };
    }
}
