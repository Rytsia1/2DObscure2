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
        public int breakAmount;
        public bool heal, killed, blocked, brokeTarget, critical;
        public Effectiveness effect;
    }

    // Combat calculation pipeline (PRD Section 8).
    // Uses Ability Scores -> Derived Stats -> Skill Scaling -> Buffs/Debuffs -> Defense Mitigation.
    public static class Rules
    {
        public const float GuardMultiplier = 0.5f;

        // Physical damage uses PhysicalPower (WeaponPower + STRModifier * WeaponScaling) against PhysicalDefense.
        public static int Physical(Combatant a, Combatant t, float power, Random rng)
        {
            float raw = a.Atk * power * Variance(rng);
            float afterDef = StatCalculator.MitigateByDefense(raw, t.Def, a.ArmorPen);
            return Mitigate(t, afterDef);
        }

        // Ranged physical damage uses RangedPower (RangedWeaponPower + DEXModifier * RangedScaling) against PhysicalDefense.
        public static int Ranged(Combatant a, Combatant t, float power, Random rng)
        {
            float raw = a.RangedAtk * power * Variance(rng);
            float afterDef = StatCalculator.MitigateByDefense(raw, t.Def, a.ArmorPen);
            return Mitigate(t, afterDef);
        }

        // Magic damage uses MagicPower (SpellPower + INTModifier * SpellScaling) against MagicDefense (separate from PhysicalDefense).
        public static int Magic(Combatant a, Combatant t, float power, Random rng)
        {
            float raw = a.Magic * power * Variance(rng);
            float afterDef = StatCalculator.MitigateByDefense(raw, t.MagicDef, a.MagicPen);
            return Mitigate(t, afterDef);
        }

        // Healing uses HealingPower (BaseHealing + WISModifier * HealingScaling).
        public static int Healing(Combatant a, float power, Random rng) =>
            Math.Max(1, (int)Math.Round(a.HealPower * power * Variance(rng)));

        // Modular Damage System entry points (Prompt Sections 3-14).
        public static DamageResult CalculateDamage(DamageContext context) =>
            DamageCalculator.Calculate(context);

        public static DamageResult CalculateSkillDamage(
            Combatant attacker,
            Combatant target,
            SkillDef skill,
            QteResult qteResult = QteResult.Good,
            Random rng = null,
            int? levelOverride = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            var components = skill.GetDamageComponents(levelOverride);
            int hitCount = skill.GetDamageHitCount(levelOverride);
            Func<float> rollProvider = rng != null ? () => (float)rng.NextDouble() : null;
            var context = attacker.CreateDamageContext(target, components, hitCount, qteResult, rollProvider);
            return DamageCalculator.Calculate(context);
        }

        // Break damage scaling: Physical -> STR (PhysicalBreak), Magic -> INT (MagicBreak).
        public static int BreakDamage(Combatant a, SkillDef skill = null, Effectiveness effect = Effectiveness.Neutral)
        {
            if (effect == Effectiveness.Immune) return 0;
            bool isMagic = skill != null && skill.kind == SkillKind.Magic;
            int baseBreak = isMagic ? a.MagicBreak : a.PhysicalBreak;
            float skillMult = skill != null ? skill.breakMultiplier : 1f;
            float elemMult = effect == Effectiveness.Weak ? 1.5f : effect == Effectiveness.Resist ? 0.75f : 1f;
            return Math.Max(1, (int)Math.Round(baseBreak * skillMult * elemMult));
        }

        static float Variance(Random rng) => rng == null ? 1f : 0.9f + (float)rng.NextDouble() * 0.2f;

        static int Mitigate(Combatant t, float raw)
        {
            raw = Math.Max(1f, raw);
            if (t.IsBroken) raw *= t.BreakDamageMultiplier;
            if (t.guarding) raw *= GuardMultiplier;
            return Math.Max(1, (int)Math.Round(raw));
        }
    }
}
