using System;
using System.Collections.Generic;

// Every QTE tuning number and stat-interaction rule lives here. All windows are half-widths in seconds (±).
namespace Obscure
{
    public enum QteResult
    {
        Perfect,
        Good,
        Poor,
        Miss = Poor // Backward compatibility alias (GDD: "Miss" replaced with "Poor")
    }

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
        // GDD Bab 12 §6.1 (Dungeon 1-3): W_P / W_G total widths are judged at +/- half, windup = T_lead,
        // Steady interval 350 ms (§6.2). Normal 90/240 ms lead 1000; Elite 80/210 lead 900; Boss 72/180 lead 800.
        // Never scaled by enemy stats.
        public static readonly Dictionary<Tier, QteProfile> Profiles = new Dictionary<Tier, QteProfile>
        {
            [Tier.Normal] = new QteProfile { perfect = 0.045f, good = 0.120f, sweep = 1.4f, telegraph = 1.2f, windup = 1.0f, hitInterval = 0.35f, letterTime = 0.75f, runeLetters = 0 },
            [Tier.Elite] = new QteProfile { perfect = 0.040f, good = 0.105f, sweep = 1.1f, telegraph = 0.8f, windup = 0.9f, hitInterval = 0.35f, letterTime = 0.60f, runeLetters = 1 },
            [Tier.Boss] = new QteProfile { perfect = 0.036f, good = 0.090f, sweep = 0.9f, telegraph = 0.6f, windup = 0.8f, hitInterval = 0.35f, letterTime = 0.50f, runeLetters = 2 },
        };

        // Relaxed assist: wider windows and more time. Never touches unit stats.
        public const float RelaxedWindow = 1.6f, RelaxedTime = 1.35f;

        // Result -> multiplier (GDD Bab 11 §2.2, Bab 12). Offense damage: Perfect 1.2x, Good 1.0x, Poor 0.75x.
        // The damage spread is narrow so build stats stay visible; a Perfect pays in tactical currencies instead:
        // Break keeps the wide 1.5 / 1.0 / 0.5 spread (DamageCalculator.Qte*Multiplier) and Perfect refunds MP.
        // Starting values, untested: GDD Bab 12 §2.4 has the test and the tuning direction.
        public const float PerfectPower = 1.2f, GoodPower = 1.0f, PartialPower = 0.875f, PoorPower = 0.75f, MissPower = 0.75f;
        public const int PerfectMpRefund = 1; // MP back on an offensive Perfect (basic attack or damaging skill)

        // Defense damage taken scales with the defender's Block Power (Derived.BlockFraction, base 50% + STR), so the
        // reaction grade multiplies a stat instead of replacing it (GDD Bab 12 §3.5, revisi 21 Nov 2026):
        //   Perfect (non-Heavy): 1 - min(PerfectBlockCap, block)   -> Block 50: 0.5x, Block 75+: 0.25x floor
        //   Good / Heavy Perfect: 1 - block * GoodBlockScale        -> Block 50: 0.75x, Block 80: 0.6x
        //   Partial: halfway between Good and 1.0; Poor: 1.0x.
        // A full parry (0 damage) needs Guard: a non-Heavy Perfect while Guarding. Shields only add Block Power.
        // Starting values, untested: GDD Bab 12 §3.5 has the test and the tuning direction.
        public const float PerfectBlockCap = 0.75f, GoodBlockScale = 0.5f, PoorDefense = 1f;
        public const float DefaultBlockFraction = 0.5f; // defenders whose derived stats are not computed (base Block Power 50)
        public const float PartialWindowMultiplier = 1.35f;

        public static float DefenseTaken(QteOutcomeCategory c, bool heavy, float blockFraction)
        {
            float block = Math.Clamp(blockFraction, 0f, 0.9f);
            float good = 1f - block * GoodBlockScale;
            return c switch
            {
                QteOutcomeCategory.Perfect => heavy ? good : 1f - Math.Min(PerfectBlockCap, block),
                QteOutcomeCategory.Success => good,
                QteOutcomeCategory.PartialSuccess => (good + 1f) / 2f,
                _ => PoorDefense,
            };
        }

        public static float DefenseTaken(QteResult r, bool heavy, float blockFraction) => DefenseTaken(ToCategory(r), heavy, blockFraction);

        public static float BlockOf(Combatant c) =>
            c?.unit != null && c.unit.Derived.blockPower > 0 ? c.unit.Derived.BlockFraction : DefaultBlockFraction;

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

        // Offensive QTE variety (physical skills). Same result multipliers as the sweep bar.
        public const float ChargePowerThreshold = 1.8f;   // skill power at or above this -> Charge (hold + release)
        public const float ChargeIdleTimeout = 1.5f;      // x sweep: never starting the charge is a Poor
        public const float MovingZoneCenter = 0.6f, MovingZoneAmplitude = 0.15f; // ranged: drifting zone range
        public const int ComboZones = 3;                  // AoE skills; multi-hit skills use their hit count (2-4)
        public const float ComboSweepScale = 1.4f, ComboFirst = 0.25f, ComboLast = 0.9f, ComboJitter = 0.04f;

        // Defensive QTE variety (enemy -> player). Same damage-taken multipliers as the tap reaction.
        public const int WardBaseLetters = 3;             // magic: rune ward length = this + tier runeLetters (3/4/5)
        // Every enemy can be unreliable (Dheo playtest 19 Nov). Starting values, untested: GDD Bab 12 §6.6 has the test.
        public static float FeintChance(Tier t) => t == Tier.Boss ? 0.40f : t == Tier.Elite ? 0.30f : 0.15f; // one hit per attack
        public static float StutterChance(Tier t) => t == Tier.Boss ? 0.50f : t == Tier.Elite ? 0.35f : 0.20f; // per physical hit
        public const float FeintStop = 0.7f, FeintRecoil = 0.25f; // fake cue stops at 70% of its run, then slides back
        public const float IntervalJitter = 0.08f, MinHitInterval = 0.2f; // untimed multi-hits: gap = hitInterval +/- jitter
        // Stutter cue: the cue covers StutterDistance of the track in StutterTime of its run, then the rest.
        // Slow-then-fast = (0.6 time, 0.3 distance); fast-then-slow = (0.4 time, 0.7 distance).
        public const float StutterSlowTime = 0.6f, StutterSlowDistance = 0.3f, StutterFastTime = 0.4f, StutterFastDistance = 0.7f;

        // QTE Variation starting values: posture-B starting values (untested, QTE_Variation_Spec_v1.md).
        // FlurryGap: Pass test = Good+ rate on Flurry cues within 15 pp of Tap cues at the same tier. If it fails: 0.180f.
        public const float FlurryGap = 0.150f;
        // ChordTolerance: Pass test = Roll Good+ rate >= 60%. If it fails: 0.110f.
        public const float ChordTolerance = 0.080f;
        // ClearZone: Pass test = Good+ rate on distorted cues within 5 pp of undistorted ones. If it fails: 0.220f.
        public const float ClearZone = 0.150f;
        // ShakePx: Pass test = same as ClearZone. If it fails: 2f.
        public const float ShakePx = 4f;
        // Remix chances: Pass test = >= 70% of players call remixes "noticeable but fair". If it fails: +/- 0.10f.
        public const float EliteRemixChance = 0.25f;
        public const float BossRemixChance = 0.35f;
        // Rhythm vocabulary examples (Spec §D.2): Ritardando (growing gaps, Elite/Boss) & Syncopated (Boss signature only).
        public static readonly float[] RitardandoExample = { 0.350f, 0.420f, 0.490f };
        public static readonly float[] SyncopatedExample = { 0.420f, 0.280f, 0.420f };

        // Status distortions (QTE Variation Spec Part E): presentation-only, priority order Fear > Confusion > Despair > Shock > Slow > Freeze > DoT.
        public static QteDistortion ActiveDistortion(Combatant c, bool reduceDistortions = false)
        {
            if (reduceDistortions || c == null) return QteDistortion.None;
            if (c.Has(StatusId.Fear)) return QteDistortion.Fear;
            if (c.Has(StatusId.Confusion)) return QteDistortion.Confusion;
            if (c.Has(StatusId.Despair)) return QteDistortion.Despair;
            if (c.Has(StatusId.Shock)) return QteDistortion.Shock;
            if (c.Has(StatusId.Slow)) return QteDistortion.Slow;
            if (c.Has(StatusId.Freeze)) return QteDistortion.Freeze;
            if (c.Has(StatusId.Burn) || c.Has(StatusId.Poison) || c.Has(StatusId.Bleed)) return QteDistortion.DoT;
            return QteDistortion.None;
        }

        // Last ClearZone (150 ms) before impact is always undistorted (Spec Part E).
        public static bool IsInClearZone(float timeToImpact) => timeToImpact >= 0f && timeToImpact <= ClearZone;

        // Struggle (drain attacks): mash to fill a draining gauge before impact. Starting values, untested.
        // Normal (1.5 s of mashing): Perfect ~6 presses/s, Good ~4.3/s; Boss (1.2 s): Perfect ~7/s.
        public const float MashFill = 0.15f, MashDecay = 0.25f, StruggleWindup = 1.5f; // window = telegraph + windup * this
        public const float StrugglePerfect = 1.0f, StruggleGood = 0.6f;

        public static QteProfile For(Tier tier, QteAssist assist) =>
            assist == QteAssist.Relaxed ? Profiles[tier].Scaled(RelaxedWindow, RelaxedTime) : Profiles[tier];

        public const float MinPerfectHalfWidth = 0.033f; // 66 ms minimum W_P (Bab 12 §1.2, §3.4 & Bab 15 §4.2)

        public static float ScalePerfect(QteProfile p, float timingBonusFraction = 0f)
        {
            float scale = Math.Clamp(1f + timingBonusFraction, 0.50f, 1.25f);
            return Math.Max(MinPerfectHalfWidth, p.perfect * scale);
        }

        public static float ScaleGood(QteProfile p, float timingBonusFraction = 0f)
        {
            float scale = Math.Clamp(1f + timingBonusFraction, 0.50f, 1.25f);
            return p.good * scale;
        }

        // error = pressTime - idealTime, in seconds.
        public static QteResult Judge(float error, QteProfile p, float timingBonusFraction = 0f)
        {
            float e = Math.Abs(error);
            return e <= ScalePerfect(p, timingBonusFraction) ? QteResult.Perfect
                : e <= ScaleGood(p, timingBonusFraction) ? QteResult.Good
                : QteResult.Miss;
        }

        // Evaluates into the 4 PRD categories (Perfect, Success, PartialSuccess, Failure).
        public static QteOutcomeCategory JudgeCategory(float error, QteProfile p, float timingBonusFraction = 0f)
        {
            float e = Math.Abs(error);
            float good = ScaleGood(p, timingBonusFraction);
            if (e <= ScalePerfect(p, timingBonusFraction)) return QteOutcomeCategory.Perfect;
            if (e <= good) return QteOutcomeCategory.Success;
            if (e <= good * PartialWindowMultiplier) return QteOutcomeCategory.PartialSuccess;
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

        public static float Offense(QteResult r) => r == QteResult.Perfect ? PerfectPower : r == QteResult.Good ? GoodPower : PoorPower;

        public static float OffenseCategory(QteOutcomeCategory c, Combatant actor = null, SkillKind? kind = null)
        {
            return c switch
            {
                QteOutcomeCategory.Perfect => PerfectPower,
                QteOutcomeCategory.Success => GoodPower,
                QteOutcomeCategory.PartialSuccess => PartialPower,
                _ => PoorPower
            };
        }

        public static float Defense(QteResult r, bool isHeavy = false, Combatant defender = null) =>
            DefenseTaken(r, isHeavy, BlockOf(defender));

        public static float DefenseCategory(QteOutcomeCategory c, Combatant defender = null, Element attackElement = Element.Physical, bool isHeavy = false) =>
            DefenseTaken(c, isHeavy, BlockOf(defender));

        // Counter damage (Bab 12 §6.5): a physical strike at Good-equivalent power, times the counter multiplier,
        // raised by Counter Power read as a percentage (Counter Power 18 = +18%). It goes through Physical Power, so
        // weapon, level and Attack Up all scale it; enemy DEF mitigates it as usual.
        // Starting values, untested: see GDD Bab 12 §6.5 for the pass/fail test and tuning direction.
        public static int CalculateCounterDamage(Combatant defender, Combatant attacker, float multiplier = 1f, Random rng = null)
        {
            if (defender == null || attacker == null) return 0;
            float counterBonus = Math.Max(0, defender.unit.Derived.counterPower) / 100f;
            return Rules.Physical(defender, attacker, multiplier * (1f + counterBonus), rng);
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
