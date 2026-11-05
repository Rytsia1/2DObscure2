using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Normal enemies: any offensive move at a random living party member.
    public class BasicBrain : IEnemyBrain
    {
        public BattleAction Decide(Battle b, Combatant self) =>
            EnemyAI.Use(b, self, EnemyAI.Pick(b, EnemyAI.Offensive(self)));
    }

    // Elite enemies: brace when hurt, weaken an unweakened party, otherwise hunt the weakest member.
    public class TacticalBrain : IEnemyBrain
    {
        public const float BraceBelow = 0.5f, DebuffChance = 0.35f;

        public BattleAction Decide(Battle b, Combatant self)
        {
            var moves = EnemyAI.Moves(self);
            var stance = moves.FirstOrDefault(m => m.self);
            if (stance != null && self.unit.hp <= self.Stats.maxHp * BraceBelow && !self.Has(StatusId.DefenseUp))
                return EnemyAI.Use(b, self, stance);

            var debuff = moves.FirstOrDefault(m => m.allTargets && m.apply != null);
            if (debuff != null && !b.Party.Any(p => p.Alive && p.Has(StatusId.AttackDown)) && b.Rng.NextDouble() < DebuffChance)
                return EnemyAI.Use(b, self, debuff);

            var weakest = b.Party.Where(p => p.Alive).OrderBy(p => p.unit.hp).First();
            var strikes = moves.Where(m => !m.self && !m.allTargets).ToList();
            return EnemyAI.Use(b, self, EnemyAI.Pick(b, strikes), weakest);
        }
    }

    // Roster enemies: deterministic, data-configured policy. The highest-priority move that is off cooldown, affordable,
    // has a legal target and respects the one-Heavy-per-round rule is chosen (EnemyAttack.aiPriority / cooldown / minTargets).
    // Charged moves spend one turn charging and release on the next; Broken cancels the charge (Combatant.ApplyBreakDamage).
    public class PriorityBrain : IEnemyBrain
    {
        public BattleAction Decide(Battle b, Combatant self)
        {
            if (self.Charge != null)
            {
                var t = self.Charge.target;
                if (t != null && !t.Alive) t = NextInPartyOrder(b, t); // telegraphed target fell: deterministic redirect (Bab 12 §6.4)
                return t == null && !self.Charge.move.allTargets ? null : EnemyAI.Use(b, self, self.Charge.move, t);
            }
            var plan = self.Intent;
            if (plan == null || (plan.target != null && !plan.target.Alive)) plan = Select(b, self); // planned action became invalid
            if (plan == null) return null;
            return plan.charging ? BattleAction.Charge(self, plan.move, plan.target) : EnemyAI.Use(b, self, plan.move, plan.target);
        }

        public static EnemyIntent Select(Battle b, Combatant self)
        {
            var foes = b.Opponents(self).Where(c => c.Alive).ToList();
            if (foes.Count == 0) return null;
            bool heavyTaken = b.Enemies.Any(e => e != self && e.Alive && ((e.Intent?.move.HasHeavy ?? false) || (e.Charge?.move.HasHeavy ?? false)));
            foreach (var m in EnemyAI.Moves(self).OrderByDescending(m => m.aiPriority)) // stable: ties keep data order
            {
                if (self.Cooldown(Combatant.MoveKey(m)) > 0 || self.unit.mp < m.mpCost) continue;
                if (m.aiMpBelow > 0f && self.unit.mp >= self.Stats.maxMp * m.aiMpBelow) continue; // e.g. Arcane Charge only when low
                if (m.HasHeavy && heavyTaken) continue; // at most one Heavy-marked attack per round
                if (m.self) return new EnemyIntent { move = m };
                if (m.allTargets)
                {
                    if (foes.Count >= m.minTargets) return new EnemyIntent { move = m, charging = m.telegraph };
                    continue;
                }
                // Status moves aim at someone not already afflicted; a status-only move with nobody left is pointless.
                var fresh = m.apply == null ? foes : foes.Where(f => !m.apply.Any(ap => f.Has(ap.id))).ToList();
                if (m.StatusOnly && fresh.Count == 0) continue;
                return new EnemyIntent { move = m, target = PickTarget(b, self, fresh.Count > 0 ? fresh : foes), charging = m.telegraph };
            }
            return null;
        }

        static Combatant PickTarget(Battle b, Combatant self, List<Combatant> pool) => self.enemy.targetRule switch
        {
            TargetRule.LowestHp => pool.OrderBy(p => p.unit.hp).ThenBy(p => p.slot).First(),
            TargetRule.LowestDef => pool.OrderBy(p => p.Def).ThenBy(p => p.slot).First(),
            _ => EnemyAI.Pick(b, pool),
        };

        static Combatant NextInPartyOrder(Battle b, Combatant fallen)
        {
            var party = b.Party;
            return party.Where(p => p.Alive).OrderBy(p => (p.slot - fallen.slot + party.Count) % party.Count).FirstOrDefault();
        }
    }

    // Bosses: the move list is gated by phase (EnemyAttack.phase); phase 2 is triggered in Battle at an HP threshold.
    // In phase 2 it favors its area attack while three or more party members still stand.
    public class BossBrain : IEnemyBrain
    {
        public BattleAction Decide(Battle b, Combatant self)
        {
            var moves = EnemyAI.Offensive(self);
            var area = moves.FirstOrDefault(m => m.allTargets);
            if (self.phase >= 2 && area != null && b.Party.Count(p => p.Alive) >= 3 && b.Rng.NextDouble() < 0.5)
                return EnemyAI.Use(b, self, area);
            return EnemyAI.Use(b, self, EnemyAI.Pick(b, moves));
        }
    }
}
