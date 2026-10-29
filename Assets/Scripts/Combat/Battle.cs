using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public enum Outcome { None, Victory, Defeat }

    // Turn-based battle simulation. No Unity, no UI: callers ask NextTurn(), then Perform() an action.
    public class Battle
    {
        public readonly List<Combatant> Party, Enemies;
        public readonly List<string> Items; // shared with the run, consumed in place
        public readonly Random Rng;
        public readonly List<string> Log = new List<string>();
        public event Action<string> Logged;

        public int Round { get; private set; }
        public Combatant Active { get; private set; }

        readonly List<Combatant> queue = new List<Combatant>();
        readonly HashSet<Combatant> acted = new HashSet<Combatant>();

        public Battle(IEnumerable<Unit> party, IEnumerable<EnemyDef> enemies, List<string> items, Random rng)
        {
            Party = party.Select((u, i) => new Combatant(u, Side.Party, i, u.name, u.name.Substring(0, Math.Min(3, u.name.Length)).ToUpperInvariant())).ToList();
            var defs = enemies.ToList();
            Enemies = defs.Select((d, i) =>
            {
                bool dup = defs.Count(x => x == d) > 1;
                char letter = (char)('A' + defs.Take(i).Count(x => x == d));
                return new Combatant(new Unit(d), Side.Enemy, i, dup ? $"{d.shortName} {letter}" : d.shortName, dup ? d.tag + letter : d.tag, d);
            }).ToList();
            Items = items;
            Rng = rng;
        }

        public IEnumerable<Combatant> All => Party.Concat(Enemies);
        public Tier Tier => Enemies.Max(e => e.enemy.tier); // encounter difficulty, drives player QTEs
        public IEnumerable<Combatant> Opponents(Combatant c) => c.IsEnemy ? Party : Enemies;
        public IEnumerable<Combatant> Allies(Combatant c) => c.IsEnemy ? Enemies : Party;

        public Outcome Outcome =>
            Enemies.All(e => !e.Alive) ? Outcome.Victory
            : Party.All(p => !p.Alive) ? Outcome.Defeat
            : Outcome.None;

        // Living combatants still to act this round, in order (excludes Active).
        public IEnumerable<Combatant> Upcoming => queue.Where(c => c.Alive);
        public List<Combatant> NextRoundOrder => TurnOrder.Build(All);
        public bool HasActed(Combatant c) => acted.Contains(c);

        // QTE result -> offense multiplier. Skills may override Perfect (critical hits).
        public static float QtePower(BattleAction a, QteResult r) =>
            r == QteResult.Perfect && a.kind == ActionKind.Skill && a.skill.perfectPower > 0 ? a.skill.perfectPower : QteConfig.Offense(r);

        // Advances to the next combatant able to act, starting new rounds as needed.
        // Returns null when the battle is over, or when a full cycle passed with nobody able to act
        // (e.g. everyone stunned); calling again simply continues, since skips count stuns down.
        public Combatant NextTurn()
        {
            Active = null;
            int budget = (Party.Count + Enemies.Count) * 2 + 2;
            while (Outcome == Outcome.None && budget-- > 0)
            {
                if (queue.Count == 0) StartRound();
                var c = queue[0];
                queue.RemoveAt(0);
                if (!c.Alive || !acted.Add(c)) continue;
                c.guarding = false;
                StartTurn(c);
                if (!c.Alive) continue; // poison finished it
                if (c.stunTurns > 0)
                {
                    c.stunTurns--;
                    Say($"{c.name} is stunned and cannot act.");
                    continue;
                }
                return Active = c;
            }
            return null;
        }

        void StartRound()
        {
            Round++;
            acted.Clear();
            queue.AddRange(TurnOrder.Build(All));
        }

        // Start of the bearer's turn: cooldowns and status durations tick, poison bites, Break recovers.
        void StartTurn(Combatant c)
        {
            foreach (var id in c.cooldowns.Keys.ToList())
                if (--c.cooldowns[id] <= 0) c.cooldowns.Remove(id);

            var poison = c.Get(StatusId.Poison);
            if (poison != null)
            {
                int dmg = Math.Max(1, (int)poison.magnitude);
                c.unit.hp = Math.Max(0, c.unit.hp - dmg);
                Say($"{c.name} suffers {dmg} poison damage.");
                if (!c.Alive) Say(c.IsEnemy ? $"{c.name} is destroyed." : $"{c.name} falls!");
            }

            foreach (var s in c.statuses.ToList())
                if (--s.turns <= 0)
                {
                    c.statuses.Remove(s);
                    if (s.id == StatusId.Broken && c.HasBreakGauge)
                        c.CurrentBreakGauge = c.MaxBreakGauge;
                    if (s.id == StatusId.AttackUp && c.OverdriveExhaustPending)
                        c.TriggerDraconicExhaustion();
                    Say($"{c.name}'s {StatusInfo.Name(s.id)} wears off.");
                }
        }

        // Null when the action is legal right now, otherwise a reason for the player.
        public string Validate(BattleAction a)
        {
            if (Outcome != Outcome.None) return "The battle is over.";
            if (a?.actor == null || a.actor != Active) return "It is not that combatant's turn.";
            switch (a.kind)
            {
                case ActionKind.Attack:
                    if (a.attack != null && a.attack.self) return null;
                    if (a.attack != null && a.attack.allTargets) return Opponents(a.actor).Any(c => c.Alive) ? null : "Invalid target.";
                    return IsFoe(a.actor, a.target) ? null : "Invalid target.";
                case ActionKind.Guard:
                    return null;
                case ActionKind.Skill:
                    if (a.skill == null || !a.actor.unit.HasActiveSkill(a.skill.id)) return "Unknown skill.";
                    if (a.actor.Cooldown(a.skill.id) > 0) return "On cooldown.";
                    if (a.actor.unit.mp < a.actor.GetEffectiveMpCost(a.skill)) return "Not enough MP.";
                    return a.skill.target switch
                    {
                        TargetType.Enemy => IsFoe(a.actor, a.target) ? null : "Invalid target.",
                        TargetType.Ally => IsAlly(a.actor, a.target) ? null : "Invalid target.",
                        TargetType.Self => a.target == null || a.target == a.actor ? null : "Invalid target.",
                        _ => null,
                    };
                case ActionKind.Item:
                    if (a.item == null || !Items.Contains(a.item.id)) return "No such item.";
                    return IsAlly(a.actor, a.target) ? null : "Invalid target.";
            }
            return "Unknown action.";
        }

        static bool IsFoe(Combatant a, Combatant t) => t != null && t.Alive && t.side != a.side;
        static bool IsAlly(Combatant a, Combatant t) => t != null && t.Alive && t.side == a.side;

        // Interpose: a Guardian takes single-target hits aimed at the ally it protects.
        public Combatant Intercept(Combatant target)
        {
            if (target == null) return null;
            var guard = Allies(target).FirstOrDefault(c => c != target && c.Alive && c.Get(StatusId.Guardian)?.source == target);
            if (guard == null) return target;
            Say($"{guard.name} interposes for {target.name}!");
            return guard;
        }

        // Resolves a validated action and ends the actor's turn. Throws without side effects if illegal.
        // power: offense multiplier from the actor's QTE (damage, healing; >1 also guarantees statuses).
        // defense: the defender's reaction per hit of an attack; null = no reaction (full damage).
        public List<Hit> Perform(BattleAction a, float power = 1f, IReadOnlyList<QteResult> defense = null)
        {
            var error = Validate(a);
            if (error != null) throw new InvalidOperationException(error);

            var actor = a.actor;
            var hits = new List<Hit>();
            switch (a.kind)
            {
                case ActionKind.Attack:
                    var move = a.attack;
                    if (move != null && move.self)
                    {
                        Say($"{actor.name} uses {move.name}.");
                        foreach (var ap in move.apply ?? Array.Empty<StatusApply>()) Apply(actor, ap, actor, 1f);
                        break;
                    }
                    var targets = move != null && move.allTargets ? Opponents(actor).Where(c => c.Alive).ToList() : new List<Combatant> { a.target };
                    Say(move == null ? $"{actor.name} attacks {a.target.name}."
                        : move.allTargets ? $"{actor.name} unleashes {move.name}!"
                        : $"{actor.name} uses {move.name} on {a.target.name}.");
                    var element = move?.element ?? Element.Physical;
                    var struck = new HashSet<Combatant>();
                    for (int i = 0; i < (move?.hits ?? 1); i++)
                    {
                        var reaction = defense != null && i < defense.Count ? defense[i] : (QteResult?)null;
                        foreach (var t in targets.Where(c => c.Alive))
                        {
                            var hit = Deal(actor, t, Rules.Physical(actor, t, (move?.power ?? 1f) * power, Rng), element, reaction, null);
                            hits.Add(hit);
                            if (!hit.blocked) struck.Add(t);
                        }
                    }
                    foreach (var t in struck)
                        foreach (var ap in move?.apply ?? Array.Empty<StatusApply>()) Apply(t, ap, actor, 1f);
                    break;

                case ActionKind.Guard:
                    actor.guarding = true;
                    Say($"{actor.name} raises a guard.");
                    break;

                case ActionKind.Skill:
                    var s = a.skill;
                    actor.unit.mp -= actor.GetEffectiveMpCost(s);
                    if (s.cooldown > 0) actor.cooldowns[s.id] = s.cooldown;
                    Say($"{actor.name} uses {s.name}.{(power > QteConfig.PerfectPower ? " Critical!" : "")}");
                    foreach (var t in Targets(actor, s.target, a.target))
                    {
                        for (int i = 0; i < s.hits && t.Alive; i++)
                        {
                            float skillPwr = s.power * power;
                            switch (s.kind)
                            {
                                case SkillKind.Physical:
                                    int pDmg = s.isRanged ? Rules.Ranged(actor, t, skillPwr, Rng) : Rules.Physical(actor, t, skillPwr, Rng);
                                    hits.Add(Deal(actor, t, pDmg, s.element, null, s));
                                    break;
                                case SkillKind.Magic:
                                    hits.Add(Deal(actor, t, Rules.Magic(actor, t, skillPwr, Rng), s.element, null, s));
                                    break;
                                case SkillKind.Heal:
                                    hits.Add(Heal(t, Rules.Healing(actor, skillPwr, Rng)));
                                    break;
                            }
                        }
                        // Self-applied statuses link to the target (Interpose remembers whom it guards).
                        foreach (var ap in s.apply ?? Array.Empty<StatusApply>()) Apply(ap.onSelf ? actor : t, ap, ap.onSelf ? t : actor, power);
                    }
                    break;

                case ActionKind.Item:
                    var item = a.item;
                    Items.Remove(item.id);
                    Say($"{actor.name} uses {item.name} on {a.target.name}.");
                    if (item.heal > 0) hits.Add(Heal(a.target, item.heal));
                    if (item.mana > 0)
                    {
                        int before = a.target.unit.mp;
                        a.target.unit.mp = Math.Min(a.target.Stats.maxMp, before + item.mana);
                        Say($"{a.target.name} recovers {a.target.unit.mp - before} MP.");
                    }
                    break;
            }
            Active = null;
            return hits;
        }

        IEnumerable<Combatant> Targets(Combatant actor, TargetType type, Combatant chosen) => type switch
        {
            TargetType.AllEnemies => Opponents(actor).Where(c => c.Alive).ToList(),
            TargetType.AllAllies => Allies(actor).Where(c => c.Alive).ToList(),
            TargetType.Self => new[] { actor },
            _ => new[] { chosen },
        };

        void Apply(Combatant t, StatusApply ap, Combatant source, float power)
        {
            if (!t.Alive) return;
            bool sure = ap.chance >= 1f || power > QteConfig.GoodPower; // a Perfect QTE guarantees the effect
            if (!sure && Rng.NextDouble() >= ap.chance) return;
            t.AddStatus(ap.id, ap.turns, ap.magnitude, source);
            Say(StatusInfo.IsBuff(ap.id) ? $"{t.name} gains {StatusInfo.Name(ap.id)}." : $"{t.name} suffers {StatusInfo.Name(ap.id)}.");
        }

        // All damage goes through here: reaction, then element, then HP, then Break Gauge reduction.
        Hit Deal(Combatant attacker, Combatant t, int amount, Element element, QteResult? reaction, SkillDef skill)
        {
            if (reaction == QteResult.Perfect)
            {
                Say($"{t.name} perfectly parries!");
                return new Hit { target = t, amount = 0, blocked = true };
            }
            var effect = t.Affinities.Against(element);
            if (effect == Effectiveness.Immune)
            {
                Say($"{t.name} is immune to {element}!");
                return new Hit { target = t, amount = 0, effect = effect };
            }
            amount = Math.Max(1, (int)Math.Round(amount * Affinities.Multiplier(effect)));
            string note = effect == Effectiveness.Weak ? " - weakness!" : effect == Effectiveness.Resist ? " - resisted" : "";
            if (reaction == QteResult.Good)
            {
                amount = Math.Max(1, (int)Math.Round(amount * QteConfig.GoodDefense));
                note += " (blocked)";
            }
            var hit = Damage(t, amount, note);
            hit.effect = effect;

            // Break Gauge reduction on enemy targets when struck by an attack or offensive skill.
            if (t.Alive && t.HasBreakGauge && attacker != null && attacker.side != t.side)
            {
                int rawBreak = Rules.BreakDamage(attacker, skill, effect);
                bool broke = t.ApplyBreakDamage(rawBreak, attacker, out int reduced);
                hit.breakAmount = reduced;
                hit.brokeTarget = broke;
                if (broke)
                    Say($"{t.name}'s guard shatters! (BROKEN)");
            }

            return hit;
        }

        Hit Damage(Combatant t, int amount, string note = "")
        {
            t.unit.hp = Math.Max(0, t.unit.hp - amount);
            Say($"{t.name} takes {amount} damage{(t.guarding ? " (guarded)" : "")}{note}.");
            if (!t.Alive) Say(t.IsEnemy ? $"{t.name} is destroyed." : $"{t.name} falls!");
            else CheckPhase(t);
            return new Hit { target = t, amount = amount, killed = !t.Alive };
        }

        // Boss phase 2 begins the first time HP drops to the threshold.
        void CheckPhase(Combatant t)
        {
            var d = t.enemy;
            if (d == null || d.phase2At <= 0 || t.phase != 1 || t.unit.hp > t.Stats.maxHp * d.phase2At) return;
            t.phase = 2;
            if (d.bossBreakRecoverOnPhase2 && t.IsBroken) t.RecoverBreakGauge();
            Say(d.phase2Text ?? $"{t.name} changes its stance!");
            if (d.phase2Buff != null) Apply(t, d.phase2Buff, t, 1f);
        }

        Hit Heal(Combatant t, int amount)
        {
            int before = t.unit.hp;
            t.unit.Heal(amount);
            Say($"{t.name} recovers {t.unit.hp - before} HP.");
            return new Hit { target = t, amount = t.unit.hp - before, heal = true };
        }

        void Say(string line)
        {
            Log.Add(line);
            Logged?.Invoke(line);
        }
    }
}
