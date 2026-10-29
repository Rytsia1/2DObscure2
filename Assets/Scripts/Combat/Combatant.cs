using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public enum Side { Party, Enemy }

    // A unit's per-battle wrapper. HP/MP live on the Unit so party damage persists across the run;
    // statuses, cooldowns and boss phase live here, so they end with the battle.
    public class Combatant
    {
        public readonly Unit unit;
        public readonly Side side;
        public readonly int slot;          // formation index, also the turn-order tiebreaker
        public readonly string name, tag;  // log name ("Skeleton B"), turn-bar tag ("SKB")
        public readonly EnemyDef enemy;    // null for party members

        public bool guarding;   // halves incoming damage until this combatant's next turn starts
        public int stunTurns;   // turns to skip; the skip consumes the turn
        public int phase = 1;   // boss phase
        public readonly List<StatusEffect> statuses = new List<StatusEffect>();
        public readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>();

        public Combatant(Unit unit, Side side, int slot, string name, string tag, EnemyDef enemy = null)
        {
            this.unit = unit; this.side = side; this.slot = slot; this.name = name; this.tag = tag; this.enemy = enemy;
        }

        public bool Alive => unit.Alive;
        public bool IsEnemy => side == Side.Enemy;
        public Stats Stats => unit.stats;
        public Affinities Affinities => unit.affinities;

        // Effective stats: base stats modified by statuses. Speed changes apply from the next round.
        public int Atk => Scale(unit.stats.atk, 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown));
        public int Magic => Scale(unit.stats.mag, 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown));
        public int Def => Scale(unit.stats.def, 1 + Mag(StatusId.DefenseUp) - Mag(StatusId.DefenseDown));
        public int Speed => Scale(unit.stats.spd, 1 - Mag(StatusId.Slow));

        static int Scale(int value, float factor) => (int)Math.Round(value * Math.Max(0f, factor));
        float Mag(StatusId id) => Get(id)?.magnitude ?? 0f;

        public StatusEffect Get(StatusId id) => statuses.Find(s => s.id == id);
        public bool Has(StatusId id) => Get(id) != null;
        public int Cooldown(string skillId) => cooldowns.TryGetValue(skillId, out var n) ? n : 0;

        // Reapplying refreshes: the longer duration and the stronger magnitude win. No stacking.
        public void AddStatus(StatusId id, int turns, float magnitude, Combatant source)
        {
            var s = Get(id);
            if (s == null) statuses.Add(new StatusEffect { id = id, turns = turns, magnitude = magnitude, source = source });
            else
            {
                s.turns = Math.Max(s.turns, turns);
                s.magnitude = Math.Max(s.magnitude, magnitude);
                s.source = source;
            }
        }

        public string StatusTags => (guarding ? "GD" : "") + string.Concat(statuses.Select(s => StatusInfo.Tag(s.id)));
        public bool HasDebuff => statuses.Any(s => !StatusInfo.IsBuff(s.id));
    }

    public static class TurnOrder
    {
        // Highest Speed first; ties go party before enemies, then by formation slot. No randomness.
        public static List<Combatant> Build(IEnumerable<Combatant> combatants) =>
            combatants.Where(c => c.Alive)
                .OrderByDescending(c => c.Speed)
                .ThenBy(c => c.side)
                .ThenBy(c => c.slot)
                .ToList();
    }
}
