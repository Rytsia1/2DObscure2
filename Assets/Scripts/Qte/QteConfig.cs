using System;
using System.Collections.Generic;

// Every QTE tuning number lives here. All windows are half-widths in seconds (±).
namespace Obscure
{
    public enum QteResult { Perfect, Good, Miss }
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
        public const float PerfectPower = 1.25f, GoodPower = 1f, MissPower = 0.7f;
        public const float PerfectDefense = 0f, GoodDefense = 0.5f, MissDefense = 1f;

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
        public static QteResult Judge(float error, QteProfile p)
        {
            float e = Math.Abs(error);
            return e <= p.perfect ? QteResult.Perfect : e <= p.good ? QteResult.Good : QteResult.Miss;
        }

        public static float Offense(QteResult r) => r == QteResult.Perfect ? PerfectPower : r == QteResult.Good ? GoodPower : MissPower;
        public static float Defense(QteResult r) => r == QteResult.Perfect ? PerfectDefense : r == QteResult.Good ? GoodDefense : MissDefense;
    }
}
