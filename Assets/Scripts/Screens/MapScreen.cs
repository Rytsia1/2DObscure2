using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    public class MapScreen : GameScreen
    {
        Label tooltip;
        readonly Dictionary<Selectable, NodeDef> nodeOf = new Dictionary<Selectable, NodeDef>();

        protected override void Build()
        {
            var map = Run.Map;
            Backdrop.Create(Root, false, 8, Palette.Void, Palette.Night, Palette.Dusk);

            Ui.Panel(Root, 0, 0, Ui.Width, 17);
            int depth = Run.Current == null ? 0 : Run.Current.floor + 1;
            Ui.Label(Root, $"{map.name.ToUpper()}  -  DEPTH {depth}/{map.Floors}", 6, 5, Palette.Bone);
            Ui.Label(Root, $"GOLD {Run.Gold}  ITEMS {Run.Items.Count}", 314, 5, Palette.Gold, Align.Right);

            var available = Run.Available.ToList();
            foreach (var n in map.nodes)
                foreach (var id in n.next)
                {
                    var m = map.Get(id);
                    var color = Run.Cleared.Contains(n.id) && Run.Cleared.Contains(m.id) ? Palette.Parchment
                        : available.Contains(m) && n == Run.Current ? Palette.Gold
                        : Palette.Shade;
                    Dots(Center(n), Center(m), color);
                }

            tooltip = Ui.Label(Root, "", 160, 141, Palette.Bone, Align.Center);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => tooltip.Set(Describe(nodeOf[s]));
            foreach (var n in map.nodes)
            {
                var view = NodeView(n, available.Contains(n));
                nodeOf[view] = n;
                if (available.Contains(n)) menu.Add(view);
            }

            Ui.Panel(Root, 0, 153, Ui.Width, 27);
            for (int i = 0; i < Run.Party.Count; i++)
            {
                var u = Run.Party[i];
                Ui.Label(Root, u.name.ToUpper(), 8 + i * 80, 157, Palette.Bone);
                Ui.Label(Root, $"HP {u.hp}/{u.stats.maxHp}", 8 + i * 80, 167, u.Alive ? Palette.Parchment : Palette.Crimson);
            }
        }

        Vector2Int Center(NodeDef n)
        {
            var map = Run.Map;
            int count = map.nodes.Count(m => m.floor == n.floor);
            int x = 28 + n.floor * (264 / (map.Floors - 1));
            int y = 82 + Mathf.RoundToInt((n.lane - (count - 1) / 2f) * 36);
            return new Vector2Int(x, y);
        }

        void Dots(Vector2Int a, Vector2Int b, Color color)
        {
            float len = Vector2.Distance(a, b);
            for (float d = 11; d < len - 10; d += 4)
            {
                var p = Vector2.Lerp(a, b, d / len);
                Ui.Image(Root, PixelArt.Pixel, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Ui.World, color);
            }
        }

        Selectable NodeView(NodeDef n, bool available)
        {
            int size = n.type == NodeType.Boss ? 19 : 15;
            var c = Center(n);
            bool cleared = Run.Cleared.Contains(n.id), current = n == Run.Current;

            var go = new GameObject("Node:" + n.id);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = new Vector3(c.x - size / 2, -(c.y - size / 2), 0);

            var box = Ui.Image(go.transform, null, 0, 0, Ui.Widget);
            Ui.Image(go.transform, PixelArt.FromRows(Art.Icon(n.type)), (size - 9) / 2, (size - 9) / 2, Ui.Widget + 1,
                available || current ? Color.white : Palette.Ash);
            if (current) Ui.Image(go.transform, PixelArt.FromRows(Art.Marker), size / 2 - 2, -5, Ui.Widget + 2);

            var fill = cleared ? Palette.Void : Palette.Night;
            var border = current ? Palette.Ember : available ? Palette.Bone : Palette.Dusk;
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
            Game.Go(n.IsCombat ? GameState.Combat : GameState.Reward);
        }

        public static string EnemyNames(EncounterDef e) => string.Join(", ", e.enemies.GroupBy(id => id)
            .Select(g => (g.Count() > 1 ? g.Count() + "X " : "") + Database.Enemies[g.Key].name.ToUpper()));

        static string Describe(NodeDef n) => n.type switch
        {
            NodeType.Event => "EVENT: SOMETHING STIRS IN THE DARK",
            NodeType.Treasure => "TREASURE: AN UNOPENED CACHE",
            NodeType.Rest => "REST: A FADING CAMPFIRE",
            _ => n.type.ToString().ToUpper() + ": " + EnemyNames(Database.Encounters[n.encounter]),
        };
    }
}
