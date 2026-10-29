using System;
using System.Collections.Generic;
using System.Linq;

// Runtime state for one run. Pure logic: no rendering, no input.
namespace Obscure
{
    public class Unit
    {
        public readonly string id, name, role;
        public readonly string[] sprite, skills;
        public readonly Affinities affinities;
        public Stats stats;
        public int hp, mp;
        public bool Alive => hp > 0;

        public Unit(CharacterDef d) : this(d.id, d.name, d.role, d.stats, d.sprite, d.skills, d.affinities) { }
        public Unit(EnemyDef d) : this(d.id, d.name, d.tier.ToString(), d.stats, d.sprite, Array.Empty<string>(), d.affinities) { }

        Unit(string id, string name, string role, Stats stats, string[] sprite, string[] skills, Affinities affinities)
        {
            this.id = id; this.name = name; this.role = role; this.stats = stats; this.sprite = sprite;
            this.skills = skills; this.affinities = affinities ?? Affinities.None;
            hp = stats.maxHp; mp = stats.maxMp;
        }

        public void Heal(int amount) { if (Alive) hp = Math.Min(stats.maxHp, hp + amount); }
    }

    public class RunState
    {
        public readonly MapDef Map;
        public readonly List<Unit> Party;
        public readonly List<string> Items = new List<string>();
        public readonly HashSet<string> Cleared = new HashSet<string>();
        public readonly Random Rng;
        public int Gold;
        public NodeDef Current { get; private set; }
        public RewardDef PendingReward { get; private set; }

        public RunState(MapDef map, IEnumerable<CharacterDef> party, int seed)
        {
            Map = map;
            Party = party.Select(c => new Unit(c)).ToList();
            Items.AddRange(Database.StartingItems);
            Rng = new Random(seed);
        }

        public bool OnMap => Current == null || Cleared.Contains(Current.id);
        public bool Won => Current != null && Current.type == NodeType.Boss && Cleared.Contains(Current.id);
        public bool PartyDefeated => Party.All(u => !u.Alive);

        public IEnumerable<NodeDef> Available =>
            !OnMap ? Enumerable.Empty<NodeDef>()
            : Current == null ? Map.nodes.Where(n => n.floor == 0)
            : Current.next.Select(Map.Get);

        public bool CanEnter(NodeDef n) => Available.Contains(n);

        public void Enter(NodeDef n)
        {
            if (!CanEnter(n)) throw new InvalidOperationException($"Node {n.id} is not reachable");
            Current = n;
            if (!n.IsCombat) PendingReward = n.reward;
        }

        public EncounterDef CurrentEncounter => Current?.encounter == null ? null : Database.Encounters[Current.encounter];

        // Combat victory: gold from enemies, elites drop an item.
        public void WinCombat()
        {
            var enc = CurrentEncounter ?? throw new InvalidOperationException("Not in combat");
            foreach (var u in Party) if (!u.Alive) u.hp = 1; // the fallen rise after a won battle
            var items = enc.tier == Tier.Elite ? new[] { Database.Items.Keys.ElementAt(Rng.Next(Database.Items.Count)) } : Array.Empty<string>();
            PendingReward = new RewardDef
            {
                title = enc.tier == Tier.Boss ? "The Warden Falls" : "Victory",
                text = enc.tier == Tier.Boss ? "The abyss shudders and goes silent." : "The dead lie still once more.",
                gold = enc.enemies.Sum(id => Database.Enemies[id].gold),
                items = items,
            };
        }

        public void ClaimReward()
        {
            var r = PendingReward ?? throw new InvalidOperationException("No reward pending");
            Gold += r.gold;
            Items.AddRange(r.items);
            if (r.healPercent > 0) foreach (var u in Party) u.Heal(u.stats.maxHp * r.healPercent / 100);
            Cleared.Add(Current.id);
            PendingReward = null;
        }
    }
}
