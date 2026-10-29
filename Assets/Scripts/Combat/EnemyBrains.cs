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
