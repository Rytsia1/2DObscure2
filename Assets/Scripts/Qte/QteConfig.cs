using System;
using System.Collections.Generic;

// Every QTE tuning number and stat-interaction rule lives here. All windows are half-widths in seconds (±).
namespace Obscure
{
    public enum QteResult { Perfect, Good, Miss }

    // Four-tier QTE result categories defined in PRD Section 10.4.
    public enum QteOutcomeCategory { Perfect, Success, PartialSuccess, Failure }

    public enum QteAssist { Standard, Relaxed, Auto }
    public enum SkillDifficulty { Easy, Medium, Hard }

    public class QteProfile
    {
        public float perfect, good;   // timing windows around the ideal moment
        public float sweep;           // attack bar: seconds for the indicator to cross the bar (= input timeout)
        public float telegraph;       // defense: seconds the enemy telegraph shows before the cue appears
        public float windup;          // defense: seconds from cue to impact
        public float hitInterval;     // defense: seconds between hits of a multi-hit attack
        public float letterTime;      // typing: seconds allowed per letter
        public int runeLetters;       // typing: extra letters appended to the incantation

        public QteProfile Scaled(float window, float time) => new QteProfile
        {
            perfect = perfect * window, good = good * window,
            sweep = sweep * time, telegraph = telegraph * time, windup = windup * time,
            hitInterval = hitInterval * time, letterTime = letterTime * time, runeLetters = runeLetters,
        };
    }

    public static class QteConfig
    {
        public static readonly Dictionary<Tier, QteProfile> Profiles = new Dictionary<Tier, QteProfile>
        {
            [Tier.Normal] = new QteProfile { perfect = 0.100f, good = 0.220f, sweep = 1.4f, telegraph = 1.2f, windup = 1.0f, hitInterval = 0.70f, letterTime = 0.75f, runeLetters = 0 },
            [Tier.Elite] = new QteProfile { perfect = 0.065f, good = 0.160f, sweep = 1.1f, telegraph = 0.8f, windup = 0.85f, hitInterval = 0.55f, letterTime = 0.60f, runeLetters = 1 },
            [Tier.Boss] = new QteProfile { perfect = 0.040f, good = 0.110f, sweep = 0.9f, telegraph = 0.6f, windup = 0.75f, hitInterval = 0.50f, letterTime = 0.50f, runeLetters = 2 },
        };

        // Relaxed assist: wider windows and more time. Never touches unit stats.
        public const float RelaxedWindow = 1.6f, RelaxedTime = 1.35f;

        // Result -> multiplier. Offense scales damage/healing; defense scales damage taken.
        public const float PerfectPower = 1.25f, GoodPower = 1f, PartialPower = 0.85f, MissPower = 0.7f;
        public const float PerfectDefense = 0f, GoodDefense = 0.5f, PartialDefense = 0.75f, MissDefense = 1f;
        public const float PartialWindowMultiplier = 1.35f;

        // Typing: Perfect = no mistakes and finished within this fraction of the time limit.
        public const float TypingPerfectFraction = 0.6f;
        public const int TypingMaxMistakes = 3;
        public const string RuneLetters = "KLMNRTV";

        // Skill difficulty: extra rune letters and time-per-letter scale.
        public static (int runes, float time) Typing(SkillDifficulty d) => d switch
        {
            SkillDifficulty.Easy => (0, 1.2f),
            SkillDifficulty.Hard => (1, 0.9f),
            _ => (0, 1f),
        };

        // Attack bar: target zone center lands somewhere in this fraction range.
        public const float ZoneMin = 0.55f, ZoneMax = 0.85f;

        public static QteProfile For(Tier tier, QteAssist assist) =>
            assist == QteAssist.Relaxed ? Profiles[tier].Scaled(RelaxedWindow, RelaxedTime) : Profiles[tier];

        // error = pressTime - idealTime, in seconds.
        public static QteResult Judge(float error, QteProfile p, float timingBonusFraction = 0f)
        {
            float scale = Math.Clamp(1f + timingBonusFraction, 0.85f, 1.25f);
            float e = Math.Abs(error);
            return e <= p.perfect * scale ? QteResult.Perfect : e <= p.good * scale ? QteResult.Good : QteResult.Miss;
        }

        // Evaluates into the 4 PRD categories (Perfect, Success, PartialSuccess, Failure).
        public static QteOutcomeCategory JudgeCategory(float error, QteProfile p, float timingBonusFraction = 0f)
        {
            float scale = Math.Clamp(1f + timingBonusFraction, 0.85f, 1.25f);
            float e = Math.Abs(error);
            if (e <= p.perfect * scale) return QteOutcomeCategory.Perfect;
            if (e <= p.good * scale) return QteOutcomeCategory.Success;
            if (e <= p.good * scale * PartialWindowMultiplier) return QteOutcomeCategory.PartialSuccess;
            return QteOutcomeCategory.Failure;
        }

        public static QteOutcomeCategory ToCategory(QteResult r) => r switch
        {
            QteResult.Perfect => QteOutcomeCategory.Perfect,
            QteResult.Good => QteOutcomeCategory.Success,
            _ => QteOutcomeCategory.Failure
        };

        public static QteResult ToResult(QteOutcomeCategory c) => c switch
        {
            QteOutcomeCategory.Perfect => QteResult.Perfect,
            QteOutcomeCategory.Success => QteResult.Good,
            _ => QteResult.Miss
        };

        public static float Offense(QteResult r) => r == QteResult.Perfect ? PerfectPower : r == QteResult.Good ? GoodPower : MissPower;

        public static float OffenseCategory(QteOutcomeCategory c, Combatant actor = null, SkillKind? kind = null)
        {
            float baseMult = c switch
            {
                QteOutcomeCategory.Perfect => PerfectPower,
                QteOutcomeCategory.Success => GoodPower,
                QteOutcomeCategory.PartialSuccess => PartialPower,
                _ => MissPower
            };

            if (actor == null) return baseMult;

            // STR improves physical Perfect QTE results; INT improves magic QTE results; VIT reduces failure penalty.
            if (c == QteOutcomeCategory.Perfect)
            {
                if (kind == null || kind == SkillKind.Physical)
                    baseMult += Math.Clamp(Math.Max(0, actor.unit.Mod(Ability.STR)) * Database.QteStatInteractions[Ability.STR].scalingPerModifier, 0f, Database.QteStatInteractions[Ability.STR].maxBonus);
                else if (kind == SkillKind.Magic)
                    baseMult += Math.Clamp(Math.Max(0, actor.unit.Mod(Ability.INT)) * Database.QteStatInteractions[Ability.INT].scalingPerModifier, 0f, Database.QteStatInteractions[Ability.INT].maxBonus);
            }
            else if (c == QteOutcomeCategory.Failure || c == QteOutcomeCategory.PartialSuccess)
            {
                float stability = actor.unit.Derived.QteStabilityFraction;
                baseMult = Math.Min(1f, baseMult + (1f - baseMult) * stability);
            }
            return baseMult;
        }

        public static float Defense(QteResult r) => r == QteResult.Perfect ? PerfectDefense : r == QteResult.Good ? GoodDefense : MissDefense;

        // Evaluates defense damage multiplier for a QteOutcomeCategory, incorporating VIT stability and WIS mental/magic reaction bonus.
        public static float DefenseCategory(QteOutcomeCategory c, Combatant defender = null, Element attackElement = Element.Physical)
        {
            if (c == QteOutcomeCategory.Perfect) return PerfectDefense;
            float mult = c switch
            {
                QteOutcomeCategory.Success => GoodDefense,
                QteOutcomeCategory.PartialSuccess => PartialDefense,
                _ => MissDefense
            };
            if (defender != null)
            {
                float reduction = 0f;
                // VIT reduces failure/partial penalties via QTE Stability.
                if (c == QteOutcomeCategory.PartialSuccess || c == QteOutcomeCategory.Failure)
                    reduction += defender.unit.Derived.QteStabilityFraction;
                // WIS helps against magical/mental reactions.
                if (attackElement != Element.Physical)
                    reduction += Math.Clamp(Math.Max(0, defender.unit.Mod(Ability.WIS)) * Database.QteStatInteractions[Ability.WIS].scalingPerModifier, 0f, Database.QteStatInteractions[Ability.WIS].maxBonus);
                mult = Math.Max(0.15f, mult * (1f - Math.Clamp(reduction, 0f, 0.40f)));
            }
            return mult;
        }

        // Calculates physical counterattack damage on a Perfect reaction (scales with STR + DEX CounterPower).
        public static int CalculateCounterDamage(Combatant defender, Combatant attacker)
        {
            if (defender == null || attacker == null) return 0;
            int counterPwr = defender.unit.Derived.counterPower;
            int strBonus = Math.Max(0, defender.unit.Mod(Ability.STR)) * 2;
            float raw = counterPwr + strBonus;
            return Math.Max(1, (int)Math.Round(StatCalculator.MitigateByDefense(raw, attacker.Def, defender.ArmorPen)));
        }

        // Calculates CHA command/support reaction multiplier for support skills and buffs.
        public static float SupportPotencyMultiplier(Combatant actor)
        {
            if (actor == null) return 1f;
            var cfg = Database.QteStatInteractions[Ability.CHA];
            int chaMod = Math.Max(0, actor.unit.Mod(Ability.CHA));
            return 1f + Math.Clamp(chaMod * cfg.scalingPerModifier, 0f, cfg.maxBonus);
        }
    }
}
