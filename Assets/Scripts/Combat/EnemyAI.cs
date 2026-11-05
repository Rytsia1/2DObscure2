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

    // What an enemy plans to do this round. charging = this turn only charges (no damage); the release follows next turn.
    public class EnemyIntent
    {
        public EnemyAttack move;
        public Combatant target; // null for area / self moves
        public bool charging;
    }

    public static class EnemyAI
    {
        static readonly Dictionary<AiKind, IEnemyBrain> Brains = new Dictionary<AiKind, IEnemyBrain>
        {
            [AiKind.Basic] = new BasicBrain(),
            [AiKind.Tactical] = new TacticalBrain(),
            [AiKind.Boss] = new BossBrain(),
            [AiKind.Priority] = new PriorityBrain(),
        };

        // Null only when a Priority enemy has no legal action (Battle.NextTurn skips those turns first).
        public static BattleAction Choose(Battle battle, Combatant self)
        {
            if (self.enemy?.attacks == null || self.enemy.attacks.Length == 0)
                return BattleAction.Attack(self, RandomTarget(battle, self));
            return Brains[self.enemy.ai].Decide(battle, self);
        }

        // Round preparation (Bab 10 §8.9): Priority enemies commit to an action so the HUD can telegraph it.
        // Initiative decides when they act; this decides what they do.
        public static void Plan(Battle b, Combatant self)
        {
            if (self.enemy?.ai != AiKind.Priority) return;
            self.Intent = null;
            if (!self.Alive || self.stunTurns > 0 || self.StatusController.HasStatus(StatusId.Stun) || self.StatusController.HasStatus(StatusId.Sleep)) return;
            self.Intent = self.Charge != null
                ? new EnemyIntent { move = self.Charge.move, target = self.Charge.target }
                : PriorityBrain.Select(b, self);
        }

        public static bool HasLegalAction(Battle b, Combatant self) =>
            self.enemy?.ai != AiKind.Priority || self.Charge != null || self.Intent != null || PriorityBrain.Select(b, self) != null;

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

        // Telegraph wording: how hard the move hits overall. "HEAVY" is reserved for the real Heavy mechanic.
        public static string Severity(EnemyAttack a)
        {
            float total = Enumerable.Range(0, a.hits).Sum(a.PowerAt);
            string size = a.StatusOnly ? "STATUS" : total < 1.1f ? "LIGHT" : total < 1.45f ? "STRONG" : "DEADLY";
            return a.HasHeavy ? size + " HEAVY" : size;
        }

        // HUD intent line for actions that must be telegraphed (Heavy, charge, signature, status-only); null otherwise.
        public static string IntentText(Combatant c)
        {
            var i = c.Intent ?? c.Charge;
            if (i == null || !i.move.NeedsTelegraph) return null;
            var m = i.move;
            string stage = c.Intent == null ? "CHARGED" : c.Charge != null ? "RELEASE" : i.charging ? "CHARGING" : m.StatusOnly ? "STATUS" : "HEAVY";
            string hits = m.hits > 1 ? $" x{m.hits}" : "";
            string heavy = m.HasHeavy ? " HEAVY" : "";
            string status = m.apply == null ? "" : " " + string.Join(" ", m.apply.Select(ap => $"{StatusInfo.Name(ap.id)} {ap.chance * 100:0}%"));
            string who = m.allTargets ? "ALL" : i.target?.tag ?? "?";
            string when = i.charging || c.Intent == null ? " NEXT TURN" : "";
            return $"{stage}: {m.name}{hits}{heavy}{status} > {who}{when}".ToUpperInvariant();
        }
    }
}
