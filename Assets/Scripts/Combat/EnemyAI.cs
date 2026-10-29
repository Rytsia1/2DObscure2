using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // One decision strategy per AI kind (see EnemyBrains.cs). Decisions are ordinary BattleActions,
    // so enemy moves resolve through the same Battle.Perform rules as player moves.
    public interface IEnemyBrain
    {
        BattleAction Decide(Battle battle, Combatant self);
    }

    public static class EnemyAI
    {
        static readonly Dictionary<AiKind, IEnemyBrain> Brains = new Dictionary<AiKind, IEnemyBrain>
        {
            [AiKind.Basic] = new BasicBrain(),
            [AiKind.Tactical] = new TacticalBrain(),
            [AiKind.Boss] = new BossBrain(),
        };

        public static BattleAction Choose(Battle battle, Combatant self)
        {
            if (self.enemy?.attacks == null || self.enemy.attacks.Length == 0)
                return BattleAction.Attack(self, RandomTarget(battle, self));
            return Brains[self.enemy.ai].Decide(battle, self);
        }

        // ---- helpers shared by brains ----

        // Moves available in the enemy's current phase.
        public static List<EnemyAttack> Moves(Combatant self) =>
            self.enemy.attacks.Where(m => m.phase == 0 || m.phase == self.phase).ToList();

        public static List<EnemyAttack> Offensive(Combatant self) => Moves(self).Where(m => !m.self).ToList();

        public static Combatant RandomTarget(Battle b, Combatant self)
        {
            var living = b.Opponents(self).Where(c => c.Alive).ToList();
            return living[b.Rng.Next(living.Count)];
        }

        public static T Pick<T>(Battle b, IList<T> options) => options[b.Rng.Next(options.Count)];

        // Builds the action; single-target hits respect Interpose.
        public static BattleAction Use(Battle b, Combatant self, EnemyAttack move, Combatant target = null)
        {
            if (move.self || move.allTargets) return BattleAction.Strike(self, move, null);
            return BattleAction.Strike(self, move, b.Intercept(target ?? RandomTarget(b, self)));
        }

        // Telegraph wording: how hard the move hits overall.
        public static string Severity(EnemyAttack a)
        {
            float total = a.power * a.hits;
            return total < 1.1f ? "LIGHT" : total < 1.45f ? "HEAVY" : "DEADLY";
        }
    }
}
