using System;
using System.Collections.Generic;
using System.Linq;

// Runtime state for one run. Pure logic: no rendering, no input.
// Combat state (Battle/Combatant) is separate and temporary; only HP/MP on the Units carry back.
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

        // Permanent stat gain (relics, upgrades). Max HP/MP gains also fill the new capacity.
        public void Improve(Stats bonus)
        {
            stats = stats.Plus(bonus);
            if (Alive) hp = Math.Min(stats.maxHp, hp + bonus.maxHp);
            mp = Math.Min(stats.maxMp, mp + bonus.maxMp);
        }
    }

    public enum RunStatus { Active, Victory, Defeat }

    public class RunStats
    {
        public int nodesCompleted, battlesWon, elitesWon, goldEarned;
        public bool bossReached, bossDefeated;
    }

    public class RunState
    {
        public readonly int Seed;
        public string RunId => Seed.ToString("X8");
        public int Act => Map.act;
        public readonly MapDef Map;
        public readonly List<Unit> Party;
        public readonly List<string> Items = new List<string>();
        public readonly List<string> Relics = new List<string>();
        public readonly HashSet<string> Cleared = new HashSet<string>();
        public readonly Random Rng; // combat randomness
        public readonly RunStats Stats = new RunStats();
        public readonly List<StatusApply> BattleBuffs = new List<StatusApply>();
        public readonly DateTime StartedUtc = DateTime.UtcNow;
        public DateTime? EndedUtc { get; private set; }
        public int Gold;

        public RunStatus Status { get; private set; } = RunStatus.Active;
        public NodeDef Current { get; private set; }
        public RewardOffer PendingReward { get; private set; }
        public EventDef CurrentEvent { get; private set; }
        public string EventRelic { get; private set; } // the relic this event offers, rolled on entry

        public RunState(MapDef map, IEnumerable<CharacterDef> party, int seed)
        {
            Map = map;
            Seed = seed;
            Party = party.Select(c => new Unit(c)).ToList();
            Items.AddRange(Database.StartingItems);
            Rng = new Random(seed);
        }

        public int Floor => Current == null ? 0 : Current.floor + 1;
        public TimeSpan Duration => (EndedUtc ?? DateTime.UtcNow) - StartedUtc;
        public bool OnMap => Status == RunStatus.Active && (Current == null || Cleared.Contains(Current.id));
        public bool Won => Status == RunStatus.Victory;
        public bool PartyDefeated => Party.All(u => !u.Alive);

        public IEnumerable<NodeDef> Available =>
            !OnMap ? Enumerable.Empty<NodeDef>()
            : Current == null ? Map.nodes.Where(n => n.floor == 0)
            : Current.next.Select(Map.Get);

        public bool CanEnter(NodeDef n) => Available.Contains(n);

        // Per-node RNG: rewards and event rolls depend only on the run seed and the node,
        // never on how combat went, so a seed reproduces them.
        Random NodeRng(NodeDef n, int salt = 0)
        {
            int h = 17;
            foreach (var c in n.id) h = unchecked(h * 31 + c);
            return new Random(unchecked(Seed * 486187739 + h * 31 + salt));
        }

        public void Enter(NodeDef n)
        {
            if (!CanEnter(n)) throw new InvalidOperationException($"Node {n.id} is not reachable");
            Current = n;
            if (n.type == NodeType.Boss) Stats.bossReached = true;
            if (n.type == NodeType.Treasure) PendingReward = RewardGenerator.ForTreasure(NodeRng(n), Relics);
            if (n.type == NodeType.Event)
            {
                CurrentEvent = Database.Events[n.eventId];
                EventRelic = RewardGenerator.RandomRelicId(NodeRng(n, 1), Relics);
            }
        }

        public EncounterDef CurrentEncounter => Current?.encounter == null ? null : Database.Encounters[Current.encounter];

        bool Pending(NodeType type) => Status == RunStatus.Active && Current != null && Current.type == type && !Cleared.Contains(Current.id);

        // ---- combat ----

        public void WinCombat()
        {
            var enc = CurrentEncounter;
            if (enc == null || !Current.IsCombat || Cleared.Contains(Current.id) || PendingReward != null || Status != RunStatus.Active)
                throw new InvalidOperationException("No combat to win");
            Stats.battlesWon++;
            if (enc.tier == Tier.Elite) Stats.elitesWon++;
            if (enc.tier == Tier.Boss) Stats.bossDefeated = true;
            foreach (var u in Party) if (!u.Alive) u.hp = 1; // the fallen rise after a won battle
            float heal = Relics.Sum(id => Database.Relics[id].victoryHeal);
            if (heal > 0) foreach (var u in Party) u.Heal((int)Math.Round(u.stats.maxHp * heal));
            PendingReward = RewardGenerator.ForBattle(enc.tier, NodeRng(Current), Relics);
        }

        public void LoseCombat()
        {
            if (Status != RunStatus.Active) return;
            Status = RunStatus.Defeat;
            EndedUtc = DateTime.UtcNow;
        }

        // Battle-start buffs from events, consumed by the next battle only.
        public List<StatusApply> TakeBattleBuffs()
        {
            var buffs = BattleBuffs.ToList();
            BattleBuffs.Clear();
            return buffs;
        }

        // ---- rewards ----

        // choice: index into PendingReward.choices, or -1 to take only the gold. Applied exactly once.
        public void ClaimReward(int choice)
        {
            var r = PendingReward ?? throw new InvalidOperationException("No reward pending");
            if (choice < -1 || choice >= r.choices.Count) throw new ArgumentOutOfRangeException(nameof(choice));
            PendingReward = null;
            AddGold(r.gold);
            if (choice >= 0) Grant(r.choices[choice]);
            Complete();
        }

        public void Grant(RewardOption o)
        {
            switch (o.kind)
            {
                case RewardKind.Item: Items.Add(o.id); break;
                case RewardKind.Relic: AddRelic(o.id); break;
                case RewardKind.Upgrade: Upgrade(o.id); break;
                default: AddGold(o.amount); break;
            }
        }

        void AddGold(int amount)
        {
            Gold += amount;
            if (amount > 0) Stats.goldEarned += amount;
        }

        void AddRelic(string id)
        {
            if (Relics.Contains(id)) return;
            Relics.Add(id);
            foreach (var u in Party) u.Improve(Database.Relics[id].bonus);
        }

        void Upgrade(string character) => Party.First(u => u.id == character).Improve(Database.Upgrades[character].bonus);

        // ---- rest ----

        public static int RecoverAmount(Unit u) => u.Alive ? (int)Math.Round((u.stats.maxHp - u.hp) * RunConfig.RestRecover) : 0;

        public void RestRecover()
        {
            if (!Pending(NodeType.Rest)) throw new InvalidOperationException("Not at an unused rest site");
            foreach (var u in Party)
            {
                if (!u.Alive) continue;
                u.hp += RecoverAmount(u);
                u.mp += (int)Math.Round((u.stats.maxMp - u.mp) * RunConfig.RestRecover);
            }
            Complete();
        }

        public void RestImprove(string character)
        {
            if (!Pending(NodeType.Rest)) throw new InvalidOperationException("Not at an unused rest site");
            if (!Database.Upgrades.ContainsKey(character)) throw new ArgumentException(character);
            Upgrade(character);
            Complete();
        }

        // ---- events ----

        // Null when the choice can be taken, otherwise why not.
        public string EventBlocked(EventChoice c) =>
            c.goldCost > Gold ? $"Need {c.goldCost} gold" : c.relic && EventRelic == null ? "Nothing left to gain" : null;

        public string ChooseEvent(int index)
        {
            var e = CurrentEvent;
            if (e == null || !Pending(NodeType.Event)) throw new InvalidOperationException("No event pending");
            var c = e.choices[index];
            var blocked = EventBlocked(c);
            if (blocked != null) throw new InvalidOperationException(blocked);

            CurrentEvent = null;
            Gold -= c.goldCost;
            AddGold(c.gold);
            foreach (var u in Party.Where(u => u.Alive))
            {
                if (c.healPercent > 0) u.Heal(u.stats.maxHp * c.healPercent / 100);
                if (c.hurtPercent > 0) u.hp = Math.Max(1, u.hp - u.stats.maxHp * c.hurtPercent / 100);
            }
            if (c.item != null) Items.Add(c.item);
            if (c.relic) AddRelic(EventRelic);
            if (c.battleBuff != null) BattleBuffs.Add(c.battleBuff);
            Complete();
            return c.relic ? $"{c.outcome} Gained {Database.Relics[EventRelic].name}." : c.outcome;
        }

        // ---- node completion ----

        void Complete()
        {
            Cleared.Add(Current.id);
            Stats.nodesCompleted++;
            if (Current.type == NodeType.Boss)
            {
                Status = RunStatus.Victory;
                EndedUtc = DateTime.UtcNow;
            }
        }
    }
}
