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

        protected void Finish(QteResult r)
        {
            Result = r;
            Done = true;
        }
    }

    // Indicator sweeps the bar; press while it is over the target zone.
    public class TimingQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float target;   // ideal press time, seconds
        public float Sweep => profile.sweep;

        public TimingQte(QteProfile profile, float zoneFraction)
        {
            this.profile = profile;
            target = profile.sweep * zoneFraction;
        }

        public static TimingQte Random(QteProfile p, Random rng) =>
            new TimingQte(p, QteConfig.ZoneMin + (float)rng.NextDouble() * (QteConfig.ZoneMax - QteConfig.ZoneMin));

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!Done && T > profile.sweep) Finish(QteResult.Miss); // indicator ran off the bar
        }

        public override void Press()
        {
            if (!Done) Finish(QteConfig.Judge(T - target, profile));
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

        public static TypingQte For(SkillDef skill, QteProfile p, Random rng)
        {
            var (extra, timeScale) = QteConfig.Typing(skill.difficulty);
            var sb = new StringBuilder(skill.word.ToUpperInvariant());
            for (int i = 0; i < p.runeLetters + extra; i++) sb.Append(QteConfig.RuneLetters[rng.Next(QteConfig.RuneLetters.Length)]);
            return new TypingQte(sb.ToString(), skill.word.Length, sb.Length * p.letterTime * timeScale);
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!Done && T > timeLimit) Finish(QteResult.Miss);
        }

        public override void Type(char c)
        {
            if (Done || !char.IsLetter(c)) return;
            if (char.ToUpperInvariant(c) != sequence[Index])
            {
                if (++Mistakes >= QteConfig.TypingMaxMistakes) Finish(QteResult.Miss);
                return;
            }
            if (++Index == sequence.Length)
                Finish(Mistakes == 0 && T <= PerfectTime ? QteResult.Perfect : QteResult.Good);
        }
    }

    // Enemy telegraph, then one cue per hit sliding toward the impact line. Press as each lands.
    public class DefenseQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float[] impacts, delays;  // impact time and extra hold per hit
        readonly QteResult?[] results;

        public DefenseQte(QteProfile profile, int hits, float firstHitDelay = 0f)
        {
            this.profile = profile;
            impacts = new float[hits];
            delays = new float[hits];
            results = new QteResult?[hits];
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
        public IReadOnlyList<QteResult> Results => results.Select(r => r ?? QteResult.Miss).ToList();

        // When hit i's cue appears on the track.
        public float SpawnTime(int i) => impacts[i] - profile.windup - delays[i];

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Done) return;
            for (int i = 0; i < results.Length; i++)
                if (results[i] == null && T > impacts[i] + profile.good) results[i] = QteResult.Miss;
            CheckDone();
        }

        // Presses during the telegraph are ignored; a press commits to the next unresolved hit,
        // so pressing too early counts as a miss for that hit (no free mashing).
        public override void Press()
        {
            if (Done || InTelegraph) return;
            int i = Array.FindIndex(results, r => r == null);
            if (i < 0) return;
            results[i] = QteConfig.Judge(T - impacts[i], profile);
            CheckDone();
        }

        void CheckDone()
        {
            if (results.All(r => r != null)) Finish(Results.Max()); // overall = worst hit (Miss > Good > Perfect)
        }
    }

    // Which QTE an action gets. The one place this decision lives.
    public static class QteFactory
    {
        // Physical attacks/skills: timing bar. Magic and Heal skills with an incantation: typing. Others: none (null).
        public static Qte Offense(BattleAction a, Tier tier, QteAssist assist, Random rng)
        {
            bool timing = a.kind == ActionKind.Attack || (a.kind == ActionKind.Skill && a.skill.kind == SkillKind.Physical);
            bool typing = a.kind == ActionKind.Skill && (a.skill.kind == SkillKind.Magic || a.skill.kind == SkillKind.Heal) && a.skill.word != null;
            if (!timing && !typing) return null;
            if (assist == QteAssist.Auto) return new AutoQte();
            var profile = QteConfig.For(tier, assist);
            return timing ? TimingQte.Random(profile, rng) : (Qte)TypingQte.For(a.skill, profile, rng);
        }

        // Enemy hits: reaction QTE at the attacker's tier. Self stances need none (null).
        // Evasion on the target widens the windows by its magnitude.
        public static Qte Defense(BattleAction a, QteAssist assist)
        {
            var move = a.attack;
            if (move != null && move.self) return null;
            int hits = move?.hits ?? 1;
            if (assist == QteAssist.Auto) return new AutoQte(hits);
            var profile = QteConfig.For(a.actor.enemy?.tier ?? Tier.Normal, assist);
            var evasion = a.target?.Get(StatusId.Evasion);
            if (evasion != null) profile = profile.Scaled(evasion.magnitude, 1f);
            return new DefenseQte(profile, hits, move?.delay ?? 0f);
        }
    }

    // QTEs switched off in settings: resolve instantly with a predictable Good.
    public class AutoQte : Qte
    {
        public readonly int hits;
        public AutoQte(int hits = 1) { this.hits = hits; Finish(QteResult.Good); }
        public IReadOnlyList<QteResult> Results => Enumerable.Repeat(QteResult.Good, hits).ToList();
    }
}
