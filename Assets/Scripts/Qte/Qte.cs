using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// QTE state machines. Pure logic driven by Tick(dt) + input calls, so they test without Unity.
namespace Obscure
{
    public abstract class Qte
    {
        public float T { get; protected set; }          // seconds since start
        public bool Done { get; protected set; }
        public bool Cancelled { get; private set; }
        public QteResult Result { get; protected set; } = QteResult.Miss;
        public QteOutcomeCategory Category { get; protected set; } = QteOutcomeCategory.Failure;
        public float TimingBonusFraction { get; set; }  // small window bonus from DEX / WIS

        public virtual void Tick(float dt) { if (!Done) T += dt; }
        public virtual void Press() { }
        public virtual void Type(char c) { }

        // Ends the QTE without a result being applied (e.g. battle ended underneath it).
        public void Cancel()
        {
            if (Done) return;
            Cancelled = true;
            Done = true;
        }

        protected void Finish(QteResult r, QteOutcomeCategory? category = null)
        {
            Result = r;
            Category = category ?? QteConfig.ToCategory(r);
            Done = true;
        }
    }

    // Indicator sweeps the bar; press while it is over the target zone.
    public class TimingQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float target;   // ideal press time, seconds
        public float Sweep => profile.sweep;
        public float EffectivePerfect => profile.perfect * Math.Clamp(1f + TimingBonusFraction, 0.85f, 1.25f);
        public float EffectiveGood => profile.good * Math.Clamp(1f + TimingBonusFraction, 0.85f, 1.25f);

        public TimingQte(QteProfile profile, float zoneFraction, float timingBonusFraction = 0f)
        {
            this.profile = profile;
            target = profile.sweep * zoneFraction;
            TimingBonusFraction = timingBonusFraction;
        }

        public static TimingQte Random(QteProfile p, Random rng, float timingBonusFraction = 0f) =>
            new TimingQte(p, QteConfig.ZoneMin + (float)rng.NextDouble() * (QteConfig.ZoneMax - QteConfig.ZoneMin), timingBonusFraction);

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!Done && T > profile.sweep) Finish(QteResult.Miss, QteOutcomeCategory.Failure); // indicator ran off the bar
        }

        public override void Press()
        {
            if (Done) return;
            float error = T - target;
            var res = QteConfig.Judge(error, profile, TimingBonusFraction);
            var cat = QteConfig.JudgeCategory(error, profile, TimingBonusFraction);
            Finish(res, cat);
        }
    }

    // Type the shown sequence in order before time runs out.
    public class TypingQte : Qte
    {
        public readonly string sequence;
        public readonly int wordLength;    // letters before the rune suffix
        public readonly float timeLimit;
        public int Index { get; private set; }
        public int Mistakes { get; private set; }
        public float PerfectTime => timeLimit * QteConfig.TypingPerfectFraction;

        public TypingQte(string sequence, int wordLength, float timeLimit)
        {
            this.sequence = sequence.ToUpperInvariant();
            this.wordLength = wordLength;
            this.timeLimit = timeLimit;
        }

        public static TypingQte For(SkillDef skill, QteProfile p, Random rng, float intBonusFraction = 0f)
        {
            var (extra, timeScale) = QteConfig.Typing(skill.difficulty);
            var sb = new StringBuilder(skill.word.ToUpperInvariant());
            for (int i = 0; i < p.runeLetters + extra; i++) sb.Append(QteConfig.RuneLetters[rng.Next(QteConfig.RuneLetters.Length)]);
            float limit = sb.Length * p.letterTime * timeScale * Math.Clamp(1f + intBonusFraction, 0.9f, 1.2f);
            return new TypingQte(sb.ToString(), skill.word.Length, limit);
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!Done && T > timeLimit)
            {
                var cat = Index > 0 && Index >= sequence.Length / 2 ? QteOutcomeCategory.PartialSuccess : QteOutcomeCategory.Failure;
                Finish(QteResult.Miss, cat);
            }
        }

        public override void Type(char c)
        {
            if (Done || !char.IsLetter(c)) return;
            if (char.ToUpperInvariant(c) != sequence[Index])
            {
                if (++Mistakes >= QteConfig.TypingMaxMistakes)
                {
                    var cat = Index >= sequence.Length / 2 ? QteOutcomeCategory.PartialSuccess : QteOutcomeCategory.Failure;
                    Finish(QteResult.Miss, cat);
                }
                return;
            }
            if (++Index == sequence.Length)
            {
                bool isPerf = Mistakes == 0 && T <= PerfectTime;
                Finish(isPerf ? QteResult.Perfect : QteResult.Good, isPerf ? QteOutcomeCategory.Perfect : QteOutcomeCategory.Success);
            }
        }
    }

    // Enemy telegraph, then one cue per hit sliding toward the impact line. Press as each lands.
    public class DefenseQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float[] impacts, delays;  // impact time and extra hold per hit
        readonly QteResult?[] results;
        readonly QteOutcomeCategory?[] categories;

        public DefenseQte(QteProfile profile, int hits, float firstHitDelay = 0f, float timingBonusFraction = 0f)
        {
            this.profile = profile;
            TimingBonusFraction = timingBonusFraction;
            impacts = new float[hits];
            delays = new float[hits];
            results = new QteResult?[hits];
            categories = new QteOutcomeCategory?[hits];
            delays[0] = firstHitDelay;
            float t = profile.telegraph + profile.windup + firstHitDelay;
            for (int i = 0; i < hits; i++)
            {
                impacts[i] = t;
                t += profile.hitInterval;
            }
        }

        public bool InTelegraph => T < profile.telegraph;
        public int Hits => impacts.Length;
        public QteResult? HitResult(int i) => results[i];
        public QteOutcomeCategory? HitCategory(int i) => categories[i];
        public IReadOnlyList<QteResult> Results => results.Select(r => r ?? QteResult.Miss).ToList();
        public IReadOnlyList<QteOutcomeCategory> Categories => categories.Select(c => c ?? QteOutcomeCategory.Failure).ToList();

        // When hit i's cue appears on the track.
        public float SpawnTime(int i) => impacts[i] - profile.windup - delays[i];

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Done) return;
            float windowScale = Math.Clamp(1f + TimingBonusFraction, 0.85f, 1.25f);
            for (int i = 0; i < results.Length; i++)
                if (results[i] == null && T > impacts[i] + profile.good * windowScale)
                {
                    results[i] = QteResult.Miss;
                    categories[i] = QteOutcomeCategory.Failure;
                }
            CheckDone();
        }

        // Presses during the telegraph are ignored; a press commits to the next unresolved hit,
        // so pressing too early counts as a miss for that hit (no free mashing).
        public override void Press()
        {
            if (Done || InTelegraph) return;
            int i = Array.FindIndex(results, r => r == null);
            if (i < 0) return;
            float err = T - impacts[i];
            results[i] = QteConfig.Judge(err, profile, TimingBonusFraction);
            categories[i] = QteConfig.JudgeCategory(err, profile, TimingBonusFraction);
            CheckDone();
        }

        void CheckDone()
        {
            if (results.All(r => r != null))
                Finish(Results.Max(), Categories.Max()); // overall = worst hit (Miss > Good > Perfect)
        }
    }

    // Which QTE an action gets. The one place this decision lives.
    public static class QteFactory
    {
        // Physical attacks/skills: timing bar. Magic and Heal skills with an incantation: typing. Others: none (null).
        // Applies DEX timing bonus to TimingQte, while keeping base QteProfile intact.
        public static Qte Offense(BattleAction a, Tier tier, QteAssist assist, Random rng)
        {
            bool timing = a.kind == ActionKind.Attack || (a.kind == ActionKind.Skill && a.skill.kind == SkillKind.Physical);
            bool typing = a.kind == ActionKind.Skill && (a.skill.kind == SkillKind.Magic || a.skill.kind == SkillKind.Heal) && a.skill.word != null;
            if (!timing && !typing) return null;
            if (assist == QteAssist.Auto) return new AutoQte();
            var profile = QteConfig.For(tier, assist);
            float dexBonus = a.actor != null ? a.actor.unit.Derived.QteTimingBonusFraction : 0f;
            return timing ? TimingQte.Random(profile, rng, dexBonus) : (Qte)TypingQte.For(a.skill, profile, rng);
        }

        // Enemy hits: reaction QTE at the attacker's tier. Self stances need none (null).
        // Evasion on the target widens the windows by its magnitude; DEX and WIS provide TimingBonusFraction.
        public static Qte Defense(BattleAction a, QteAssist assist)
        {
            var move = a.attack;
            if (move != null && move.self) return null;
            int hits = move?.hits ?? 1;
            if (assist == QteAssist.Auto) return new AutoQte(hits);
            var profile = QteConfig.For(a.actor.enemy?.tier ?? Tier.Normal, assist);
            var evasion = a.target?.Get(StatusId.Evasion);
            if (evasion != null) profile = profile.Scaled(evasion.magnitude, 1f);

            float statTimingBonus = 0f;
            if (a.target != null)
            {
                statTimingBonus += a.target.unit.Derived.QteTimingBonusFraction;
                if (move != null && move.element != Element.Physical)
                {
                    int wisMod = Math.Max(0, a.target.unit.Mod(Ability.WIS));
                    var wisCfg = Database.QteStatInteractions[Ability.WIS];
                    statTimingBonus += Math.Clamp(wisMod * wisCfg.scalingPerModifier, 0f, wisCfg.maxBonus);
                }
            }
            return new DefenseQte(profile, hits, move?.delay ?? 0f, statTimingBonus);
        }
    }

    // QTEs switched off in settings: resolve instantly with a predictable Good.
    public class AutoQte : Qte
    {
        public readonly int hits;
        public AutoQte(int hits = 1) { this.hits = hits; Finish(QteResult.Good, QteOutcomeCategory.Success); }
        public IReadOnlyList<QteResult> Results => Enumerable.Repeat(QteResult.Good, hits).ToList();
    }
}
