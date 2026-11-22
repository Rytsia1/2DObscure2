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

        public static readonly string[] DungeonNames =
        {
            "The Sunken Crypt",      // Dungeon 1
            "The Ashen Catacombs",   // Dungeon 2
            "The Forgotten Mines",   // Dungeon 3
            "The Abyssal Labyrinth", // Dungeon 4
            "The Sunless Spire",     // Dungeon 5
            "The Cursed Citadel",    // Dungeon 6
            "The Shadow Bastion",    // Dungeon 7
            "The Void Sanctum"       // Dungeon 8
        };

        // Data-driven per-dungeon stage plans. Each dungeon can configure a different number of stages
        // and its own stage bosses / mid-map Elite rules.
        public static readonly DungeonPlanDef[] DungeonPlans =
        {
            new DungeonPlanDef
            {
                act = 1,
                name = DungeonNames[0],
                stages = new[]
                {
                    // Optional Elite nodes from stage 2 on draw from the v1.9 random Elite pool. Stage bosses d1_e6 / d1_e7
                    // are the special Miniboss nodes and never appear in random pools.
                    new DungeonStageDef { stage = 1, stageName = "Outer Crypt", bossEncounterId = "d1_e6", allowEliteNodes = false },
                    new DungeonStageDef { stage = 2, stageName = "Ossuary Gate", bossEncounterId = "d1_e7", allowEliteNodes = true },
                    new DungeonStageDef { stage = 3, stageName = "Inner Sanctum", bossEncounterId = "d1_boss", allowEliteNodes = true },
                }
            },
            DefaultSingleStagePlan(2, DungeonNames[1]),
            DefaultSingleStagePlan(3, DungeonNames[2]),
            DefaultSingleStagePlan(4, DungeonNames[3]),
            DefaultSingleStagePlan(5, DungeonNames[4]),
            DefaultSingleStagePlan(6, DungeonNames[5]),
            DefaultSingleStagePlan(7, DungeonNames[6]),
            DefaultSingleStagePlan(8, DungeonNames[7]),
        };

        static DungeonPlanDef DefaultSingleStagePlan(int act, string name) => new DungeonPlanDef
        {
            act = act,
            name = name,
            stages = new[]
            {
                new DungeonStageDef { stage = 1, stageName = name, bossEncounterId = "d1_boss", allowEliteNodes = true }
            }
        };

        public static DungeonPlanDef GetDungeonPlan(int act)
        {
            int safeAct = Math.Clamp(act, 1, DungeonPlans.Length);
            return DungeonPlans[safeAct - 1];
        }

        public static int GetStageCount(int act) => GetDungeonPlan(act).TotalStages;

        public static MapDef Generate(int seed, int act = 1, int stage = 1)
        {
            int safeAct = Math.Clamp(act, 1, 8);
            var plan = GetDungeonPlan(safeAct);
            int totalStages = plan.TotalStages;
            int safeStage = Math.Clamp(stage, 1, totalStages);
            var stageDef = plan.GetStage(safeStage);

            for (int attempt = 0; attempt < 100; attempt++)
            {
                var map = Build(
                    new Random(unchecked(seed * 7919 + attempt + safeAct * 1013 + (safeStage - 1) * 2677)),
                    safeAct,
                    safeStage,
                    totalStages,
                    stageDef);
                if (Validate(map) == null) return map;
            }
            throw new InvalidOperationException($"No valid map for seed {seed} act {act} stage {safeStage}");
        }

        static MapDef Build(Random rng, int act, int stage, int totalStages, DungeonStageDef stageDef)
        {
            bool allowEliteNodes = stageDef == null || stageDef.allowEliteNodes;
            var floors = new List<List<NodeDef>>();
            for (int f = 0; f < Floors; f++)
            {
                int count = f == Floors - 1 ? 1 : f == 0 ? StartLanes : rng.Next(MinLanes, MaxLanes + 1);
                floors.Add(Enumerable.Range(0, count).Select(l => new NodeDef { id = $"f{f}n{l}", floor = f, lane = l }).ToList());
            }
            for (int f = 0; f < Floors - 1; f++) Connect(floors[f], floors[f + 1], rng);

            var eventPool = act == 1
                ? Database.Events.Values.Where(e => e.dungeonAct == 1).ToList()
                : Database.Events.Values.Where(e => e.dungeonAct == 0 || e.dungeonAct == act).ToList();
            if (eventPool.Count == 0)
                eventPool = Database.Events.Values.ToList();
            var events = eventPool.OrderBy(_ => rng.Next()).ToList();
            int nextEvent = 0;
            // Stage bosses (D1 Minibosses) are special-node only: they stay out of the random pools (roster v1.9).
            var stageBosses = new HashSet<string>(DungeonPlans.SelectMany(p => p.stages).Select(s => s.bossEncounterId));
            for (int f = 0; f < Floors; f++)
                foreach (var n in floors[f])
                {
                    var parents = f == 0 ? new List<NodeDef>() : floors[f - 1].Where(p => p.next.Contains(n.id)).ToList();
                    n.type = PickType(f, parents, rng, allowEliteNodes);
                    if (n.IsCombat) n.encounter = PickEncounter(n, rng, stageDef, stageBosses);
                    if (n.type == NodeType.Event) n.eventId = events[nextEvent++ % events.Count].id; // spread events before repeating
                }
            string dungeonName = (act >= 1 && act <= DungeonNames.Length) ? DungeonNames[act - 1] : $"Dungeon {act}";
            return new MapDef
            {
                name = dungeonName,
                act = act,
                stage = stage,
                totalStages = totalStages,
                stageName = stageDef?.stageName,
                allowEliteNodes = allowEliteNodes,
                nodes = floors.SelectMany(x => x).ToArray()
            };
        }

        // Each source gets a contiguous range of targets; consecutive ranges touch but never cross,
        // and together they cover every target, so no node is orphaned and no paths intersect.
        // Ranges start at 0, end at T-1, and the next one starts on (shared) or right after the previous end, so
        // sum(extra width) + sum(gaps) = T-1. That budget is spent first on giving as many sources as possible a
        // second exit (at most T-1 can have one without crossing), on randomly chosen sources so it is never always
        // the same lane that gets a single way forward; what is left widens ranges or opens gaps.
        static void Connect(List<NodeDef> from, List<NodeDef> to, Random rng)
        {
            int F = from.Count, T = to.Count;
            var width = new int[F];                 // extra targets beyond the first
            var gap = new int[F];                   // 1: next range starts after this one's end instead of sharing it
            int budget = T - 1;
            foreach (int i in Enumerable.Range(0, F).OrderBy(_ => rng.Next()).Take(Math.Min(F, budget)))
                width[i] = 1;
            budget -= Math.Min(F, budget);
            while (budget > 0)
            {
                int i = rng.Next(F);
                if (i < F - 1 && gap[i] == 0 && rng.NextDouble() < 0.5) gap[i] = 1;
                else width[i]++;
                budget--;
            }

            int start = 0;
            for (int i = 0; i < F; i++)
            {
                int end = start + width[i];
                from[i].next = Enumerable.Range(start, end - start + 1).Select(j => to[j].id).ToArray();
                start = end + gap[i];
            }
        }

        static NodeType PickType(int floor, List<NodeDef> parents, Random rng, bool allowEliteNodes)
        {
            if (floor == 0) return NodeType.Battle;
            if (floor == Floors - 1) return NodeType.Boss;
            if (floor == Floors - 2) return NodeType.Rest;
            for (int tries = 0; tries < 10; tries++)
            {
                double r = rng.NextDouble();
                var t = r < 0.45 ? NodeType.Battle
                    : r < 0.65 ? NodeType.Event
                    : r < 0.78 ? (allowEliteNodes && floor >= 2 ? NodeType.Elite : NodeType.Battle)
                    : r < 0.88 ? NodeType.Treasure
                    : r < 0.94 && floor >= 1 && floor <= Floors - 3 ? NodeType.Shop
                    : floor >= 2 && floor <= Floors - 4 ? NodeType.Rest : NodeType.Event;
                // No back-to-back elites, rests, or shops along any path.
                if ((t == NodeType.Elite || t == NodeType.Rest || t == NodeType.Shop) && parents.Any(p => p.type == t)) continue;
                return t;
            }
            return NodeType.Battle;
        }

        static string PickEncounter(NodeDef n, Random rng, DungeonStageDef stageDef, ICollection<string> stageBosses)
        {
            if (n.type == NodeType.Boss && !string.IsNullOrEmpty(stageDef?.bossEncounterId) && Database.Encounters.ContainsKey(stageDef.bossEncounterId))
                return stageDef.bossEncounterId;

            var tier = n.type == NodeType.Boss ? Tier.Boss : n.type == NodeType.Elite ? Tier.Elite : Tier.Normal;
            var pool = Database.Encounters.Values
                .Where(e => e.tier == tier && e.minFloor <= n.floor && (n.type == NodeType.Boss || !stageBosses.Contains(e.id))).ToList();
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
                    if ((n.type == NodeType.Elite || n.type == NodeType.Rest || n.type == NodeType.Shop) && m.type == n.type) return $"{n.id}->{id} repeats {n.type}";
                }
                // No forced corridors: a single way forward never leads straight into another one. Non-crossing paths make
                // some single exits unavoidable when a floor narrows; the rest floor -> boss approach is single by design.
                if (n.next.Length == 1 && byId[n.next[0]] is var only && only.floor < last - 1 && only.next.Length == 1)
                    return $"{n.id}->{only.id} is a forced corridor";
            }

            // Edges between consecutive floors never cross (by lane order).
            for (int f = 0; f < last; f++)
            {
                var edges = map.nodes.Where(n => n.floor == f).SelectMany(n => n.next.Select(id => (n.lane, byId[id].lane))).ToList();
                if (edges.Any(a => edges.Any(b => a.Item1 < b.Item1 && a.Item2 > b.Item2))) return $"crossing paths on floor {f}";
            }

            if (map.allowEliteNodes)
            {
                if (!map.nodes.Any(n => n.type == NodeType.Elite)) return "no elite";
                if (!PathAvoiding(map, NodeType.Elite)) return "elites are unavoidable";
            }
            else if (map.nodes.Any(n => n.type == NodeType.Elite))
            {
                return "unexpected elite node";
            }

            if (!map.nodes.Any(n => n.type == NodeType.Treasure)) return "no treasure";
            if (!map.nodes.Any(n => n.type == NodeType.Event)) return "no event";
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
