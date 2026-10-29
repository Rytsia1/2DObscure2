using System;

namespace Obscure
{
    public enum ActionKind { Attack, Skill, Item, Guard }

    public class BattleAction
    {
        public Combatant actor, target;
        public ActionKind kind;
        public SkillDef skill;
        public ItemDef item;
        public EnemyAttack attack; // enemy move pattern; null = a plain single-hit attack

        public static BattleAction Strike(Combatant actor, EnemyAttack attack, Combatant target) => new BattleAction { actor = actor, kind = ActionKind.Attack, attack = attack, target = attack.allTargets || attack.self ? null : target };
        public static BattleAction Attack(Combatant actor, Combatant target) => new BattleAction { actor = actor, kind = ActionKind.Attack, target = target };
        public static BattleAction Guard(Combatant actor) => new BattleAction { actor = actor, kind = ActionKind.Guard };
        public static BattleAction Skill(Combatant actor, SkillDef skill, Combatant target = null) => new BattleAction { actor = actor, kind = ActionKind.Skill, skill = skill, target = target };
        public static BattleAction Item(Combatant actor, ItemDef item, Combatant target) => new BattleAction { actor = actor, kind = ActionKind.Item, item = item, target = target };
    }

    // One effect landing on one combatant; the UI turns these into numbers and flashes.
    public struct Hit
    {
        public Combatant target;
        public int amount;
        public bool heal, killed, blocked;
        public Effectiveness effect;
    }

    public static class Rules
    {
        public const float GuardMultiplier = 0.5f;

        public static int Physical(Combatant a, Combatant t, float power, Random rng) =>
            Mitigate(t, a.Atk * power * Variance(rng) - t.Def * 0.5f);

        // Magic leans on Mag and only partly respects armor.
        public static int Magic(Combatant a, Combatant t, float power, Random rng) =>
            Mitigate(t, a.Magic * power * Variance(rng) - t.Def * 0.25f);

        public static int Healing(Combatant a, float power, Random rng) =>
            Math.Max(1, (int)Math.Round(a.Magic * power * Variance(rng)));

        static float Variance(Random rng) => 0.9f + (float)rng.NextDouble() * 0.2f;

        static int Mitigate(Combatant t, float raw)
        {
            raw = Math.Max(1f, raw);
            if (t.guarding) raw *= GuardMultiplier;
            return Math.Max(1, (int)Math.Round(raw));
        }
    }
}
