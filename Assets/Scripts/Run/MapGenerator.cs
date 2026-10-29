using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Seeded Slay-the-Spire-style act map: floor 0 battles, a rest floor before the boss, choices in between.
    // Same seed -> same map. Invalid rolls are retried with a derived seed, so the result stays deterministic.
    public static class MapGenerator
    {
        public const int Floors = 8;          // 0..6 regular, 7 = boss
        public const int MinLanes = 2, MaxLanes = 4, StartLanes = 3;

        public static MapDef Generate(int seed)
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                var map = Build(new Random(unchecked(seed * 7919 + attempt)));
                if (Validate(map) == null) return map;
            }
            throw new InvalidOperationException($"No valid map for seed {seed}");
        }

        static MapDef Build(Random rng)
        {
            var floors = new List<List<NodeDef>>();
            for (int f = 0; f < Floors; f++)
            {
                int count = f == Floors - 1 ? 1 : f == 0 ? StartLanes : rng.Next(MinLanes, MaxLanes + 1);
                floors.Add(Enumerable.Range(0, count).Select(l => new NodeDef { id = $"f{f}n{l}", floor = f, lane = l }).ToList());
            }
            for (int f = 0; f < Floors - 1; f++) Connect(floors[f], floors[f + 1], rng);

            var events = Database.Events.Keys.OrderBy(_ => rng.Next()).ToList();
            int nextEvent = 0;
            for (int f = 0; f < Floors; f++)
                foreach (var n in floors[f])
                {
                    var parents = f == 0 ? new List<NodeDef>() : floors[f - 1].Where(p => p.next.Contains(n.id)).ToList();
                    n.type = PickType(f, parents, rng);
                    if (n.IsCombat) n.encounter = PickEncounter(n, rng);
                    if (n.type == NodeType.Event) n.eventId = events[nextEvent++ % events.Count]; // spread events before repeating
                }
            return new MapDef { name = "The Sunken Crypt", act = 1, nodes = floors.SelectMany(x => x).ToArray() };
        }

        // Each source gets a contiguous range of targets; consecutive ranges touch but never cross,
        // and together they cover every target, so no node is orphaned and no paths intersect.
        static void Connect(List<NodeDef> from, List<NodeDef> to, Random rng)
        {
            int start = 0;
            for (int i = 0; i < from.Count; i++)
            {
                int end = i == from.Count - 1
                    ? to.Count - 1
                    : Math.Min(to.Count - 1, Math.Max(start, (i + 1) * to.Count / from.Count - 1 + (rng.NextDouble() < 0.4 ? 1 : 0)));
                from[i].next = Enumerable.Range(start, end - start + 1).Select(j => to[j].id).ToArray();
                if (i < from.Count - 1) start = rng.NextDouble() < 0.35 ? end : Math.Min(end + 1, to.Count - 1);
            }
        }

        static NodeType PickType(int floor, List<NodeDef> parents, Random rng)
        {
            if (floor == 0) return NodeType.Battle;
            if (floor == Floors - 1) return NodeType.Boss;
            if (floor == Floors - 2) return NodeType.Rest;
            for (int tries = 0; tries < 10; tries++)
            {
                double r = rng.NextDouble();
                var t = r < 0.45 ? NodeType.Battle
                    : r < 0.68 ? NodeType.Event
                    : r < 0.83 ? (floor >= 2 ? NodeType.Elite : NodeType.Battle)
                    : r < 0.93 ? NodeType.Treasure
                    : floor >= 2 && floor <= Floors - 4 ? NodeType.Rest : NodeType.Event;
                // No back-to-back elites or rests along any path.
                if ((t == NodeType.Elite || t == NodeType.Rest) && parents.Any(p => p.type == t)) continue;
                return t;
            }
            return NodeType.Battle;
        }

        static string PickEncounter(NodeDef n, Random rng)
        {
            var tier = n.type == NodeType.Boss ? Tier.Boss : n.type == NodeType.Elite ? Tier.Elite : Tier.Normal;
            var pool = Database.Encounters.Values.Where(e => e.tier == tier && e.minFloor <= n.floor).ToList();
            return pool[rng.Next(pool.Count)].id;
        }

        // Null when the map is valid, otherwise the first problem found.
        public static string Validate(MapDef map)
        {
            var byId = map.nodes.ToDictionary(n => n.id);
            int last = map.Floors - 1;
            var bosses = map.nodes.Where(n => n.type == NodeType.Boss).ToList();
            if (bosses.Count != 1 || bosses[0].floor != last) return "exactly one boss, on the last floor";
            var boss = bosses[0];

            foreach (var n in map.nodes)
            {
                if (n != boss && n.next.Length == 0) return $"{n.id} is a dead end";
                if (n.next.Any(id => !byId.TryGetValue(id, out var m) || m.floor != n.floor + 1)) return $"{n.id} has an invalid connection";
                if (n.floor > 0 && !map.nodes.Any(p => p.next.Contains(n.id))) return $"{n.id} is unreachable";
                if (n.IsCombat && (n.encounter == null || !Database.Encounters.ContainsKey(n.encounter))) return $"{n.id} has no encounter";
                if (n.type == NodeType.Event && !Database.Events.ContainsKey(n.eventId ?? "")) return $"{n.id} has no event";
                foreach (var id in n.next)
                {
                    var m = byId[id];
                    if ((n.type == NodeType.Elite || n.type == NodeType.Rest) && m.type == n.type) return $"{n.id}->{id} repeats {n.type}";
                }
            }

            // Edges between consecutive floors never cross (by lane order).
            for (int f = 0; f < last; f++)
            {
                var edges = map.nodes.Where(n => n.floor == f).SelectMany(n => n.next.Select(id => (n.lane, byId[id].lane))).ToList();
                if (edges.Any(a => edges.Any(b => a.Item1 < b.Item1 && a.Item2 > b.Item2))) return $"crossing paths on floor {f}";
            }

            if (!map.nodes.Any(n => n.type == NodeType.Elite)) return "no elite";
            if (!map.nodes.Any(n => n.type == NodeType.Treasure)) return "no treasure";
            if (!map.nodes.Any(n => n.type == NodeType.Event)) return "no event";
            if (!PathAvoiding(map, NodeType.Elite)) return "elites are unavoidable";
            return null;
        }

        // True if some route from floor 0 to the boss never visits the given type.
        static bool PathAvoiding(MapDef map, NodeType avoid)
        {
            var byId = map.nodes.ToDictionary(n => n.id);
            var frontier = map.nodes.Where(n => n.floor == 0 && n.type != avoid).ToList();
            while (frontier.Count > 0)
            {
                if (frontier.Any(n => n.type == NodeType.Boss)) return true;
                frontier = frontier.SelectMany(n => n.next).Distinct().Select(id => byId[id]).Where(n => n.type != avoid).ToList();
            }
            return false;
        }
    }
}
