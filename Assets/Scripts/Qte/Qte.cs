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
        public bool RetriedWithSecondChance { get; protected set; }

        public virtual void Tick(float dt) { if (!Done) T += dt; }
        public virtual void Press() { }
        public virtual void Release() { }
        public virtual void PressDirection(DodgeDir d) { }
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
        public float EffectivePerfect => QteConfig.ScalePerfect(profile, TimingBonusFraction);
        public float EffectiveGood => QteConfig.ScaleGood(profile, TimingBonusFraction);

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

        // Halfling Racial Feat — Second Chance: allows a failed QTE (Result == Miss) to be retried once
        // if the actor has Second Chance available. Re-judges the new press time through QteConfig.Judge
        // so a retry NEVER automatically becomes a success.
        public bool TrySecondChanceRetry(Combatant actor, float retryPressTime)
        {
            if (!Done || Cancelled || Result != QteResult.Miss || RetriedWithSecondChance) return false;
            if (actor == null || !actor.ConsumeSecondChanceRetry()) return false;

            RetriedWithSecondChance = true;
            T = retryPressTime;
            float error = retryPressTime - target;
            var res = QteConfig.Judge(error, profile, TimingBonusFraction);
            var cat = QteConfig.JudgeCategory(error, profile, TimingBonusFraction);
            Finish(res, cat);
            return true;
        }
    }

    // Heavy skills: hold the key, the gauge fills, release in the gold zone. A full gauge is an overcharge (Poor).
    public class ChargeQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float target;   // ideal hold duration, seconds
        float holdStart = -1f;

        public bool Charging => holdStart >= 0f && !Done;
        public float Hold => holdStart >= 0f ? T - holdStart : 0f;
        public float Fill => Math.Clamp(Hold / profile.sweep, 0f, 1f);
        public float ZoneFraction => target / profile.sweep;
        public float EffectivePerfect => QteConfig.ScalePerfect(profile, TimingBonusFraction);
        public float EffectiveGood => QteConfig.ScaleGood(profile, TimingBonusFraction);

        public ChargeQte(QteProfile profile, float zoneFraction, float timingBonusFraction = 0f)
        {
            this.profile = profile;
            target = profile.sweep * zoneFraction;
            TimingBonusFraction = timingBonusFraction;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Done) return;
            if (Charging && Hold >= profile.sweep) Finish(QteResult.Miss, QteOutcomeCategory.Failure);      // overcharged
            else if (!Charging && T > profile.sweep * QteConfig.ChargeIdleTimeout) Finish(QteResult.Miss, QteOutcomeCategory.Failure); // never started
        }

        public override void Press()
        {
            if (Done || Charging) return;
            holdStart = T;
        }

        public override void Release()
        {
            if (!Charging) return; // releasing a key held from before the QTE started does nothing
            float error = Hold - target;
            Finish(QteConfig.Judge(error, profile, TimingBonusFraction), QteConfig.JudgeCategory(error, profile, TimingBonusFraction));
        }
    }

    // Ranged skills: the mark sweeps the bar while the gold zone drifts; press when they line up.
    public class MovingTargetQte : Qte
    {
        public readonly QteProfile profile;
        readonly float phase;

        public float MarkFraction => Math.Clamp(T / profile.sweep, 0f, 1f);
        // Zone stays within [Center-Amp, Center+Amp], so the mark (0 -> 1) always crosses it.
        public float ZoneFraction => QteConfig.MovingZoneCenter
            + QteConfig.MovingZoneAmplitude * (float)Math.Sin(2 * Math.PI * T / profile.sweep + phase);
        public float EffectivePerfect => QteConfig.ScalePerfect(profile, TimingBonusFraction);
        public float EffectiveGood => QteConfig.ScaleGood(profile, TimingBonusFraction);

        public MovingTargetQte(QteProfile profile, float phase, float timingBonusFraction = 0f)
        {
            this.profile = profile;
            this.phase = phase;
            TimingBonusFraction = timingBonusFraction;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!Done && T > profile.sweep) Finish(QteResult.Miss, QteOutcomeCategory.Failure);
        }

        public override void Press()
        {
            if (Done) return;
            float error = Math.Abs(MarkFraction - ZoneFraction) * profile.sweep; // distance, in seconds of mark travel
            Finish(QteConfig.Judge(error, profile, TimingBonusFraction), QteConfig.JudgeCategory(error, profile, TimingBonusFraction));
        }
    }

    // AoE / multi-hit skills: one sweep with several zones; press on each. Power is the average of the zones.
    public class ComboQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float duration;
        public readonly float[] zoneTimes;
        readonly QteResult?[] results;

        public int Zones => zoneTimes.Length;
        public float MarkFraction => Math.Clamp(T / duration, 0f, 1f);
        public float ZoneFraction(int i) => zoneTimes[i] / duration;
        public QteResult? ZoneResult(int i) => results[i];
        public IReadOnlyList<QteResult> Results => results.Select(r => r ?? QteResult.Miss).ToList();
        float Good => QteConfig.ScaleGood(profile, TimingBonusFraction);

        public ComboQte(QteProfile profile, int zones, Random rng, float timingBonusFraction = 0f)
        {
            this.profile = profile;
            TimingBonusFraction = timingBonusFraction;
            duration = profile.sweep * QteConfig.ComboSweepScale;
            zoneTimes = new float[zones];
            results = new QteResult?[zones];
            float span = QteConfig.ComboLast - QteConfig.ComboFirst;
            for (int i = 0; i < zones; i++)
            {
                float f = QteConfig.ComboFirst + span * (zones == 1 ? 0.5f : i / (float)(zones - 1));
                f += ((float)rng.NextDouble() * 2f - 1f) * QteConfig.ComboJitter;
                zoneTimes[i] = duration * f;
            }
        }

        public float AveragePower(Func<QteResult, float> power) => (float)Results.Average(r => power(r));

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Done) return;
            for (int i = 0; i < results.Length; i++)
                if (results[i] == null && T > zoneTimes[i] + Good) results[i] = QteResult.Miss;
            CheckDone();
        }

        // A press commits to the next unresolved zone, so early presses spend that zone (no mashing).
        public override void Press()
        {
            if (Done) return;
            int i = Array.FindIndex(results, r => r == null);
            if (i < 0) return;
            results[i] = QteConfig.Judge(T - zoneTimes[i], profile, TimingBonusFraction);
            CheckDone();
        }

        void CheckDone()
        {
            if (results.Any(r => r == null)) return;
            float avg = AveragePower(QteConfig.Offense);
            // Same thresholds Battle uses to map power back to a QTE result.
            var r = avg > 1.05f ? QteResult.Perfect : avg < 0.95f ? QteResult.Miss : QteResult.Good;
            Finish(r);
        }
    }

    public enum OffenseQteKind { None, Timing, Typing, Charge, MovingTarget, Combo }

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

    // Enemy magic: type a short rune ward before it lands. Same Perfect/Good/Poor rules as spell typing.
    // A lead-in (the tier's telegraph) lets the player read the runes before the clock runs; the first keystroke ends it.
    public class WardQte : TypingQte
    {
        public readonly int hits;
        public float LeadLeft { get; private set; }
        public bool InLead => LeadLeft > 0f && !Done;
        public IReadOnlyList<QteResult> Results => Enumerable.Repeat(Result, hits).ToList();

        public WardQte(string runes, float timeLimit, int hits, float lead = 0f) : base(runes, 0, timeLimit)
        {
            this.hits = Math.Max(1, hits);
            LeadLeft = Math.Max(0f, lead);
        }

        public static WardQte For(QteProfile p, int hits, Random rng)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < QteConfig.WardBaseLetters + p.runeLetters; i++)
                sb.Append(QteConfig.RuneLetters[rng.Next(QteConfig.RuneLetters.Length)]);
            return new WardQte(sb.ToString(), sb.Length * p.letterTime, hits, p.telegraph);
        }

        public override void Tick(float dt)
        {
            if (InLead) { LeadLeft -= dt; return; } // ponytail: leftover dt past the lead is dropped (< 1 frame)
            base.Tick(dt);
        }

        public override void Type(char c)
        {
            if (InLead && char.IsLetter(c)) LeadLeft = 0f; // typing early just starts the clock
            base.Type(c);
        }
    }

    public enum DodgeDir { None, Up, Down, Left, Right }

    // Enemy telegraph, then one cue per hit sliding toward the impact line. Press as each lands.
    // A cue may carry a dodge arrow (press that arrow, not the QTE key) or open with a feint (a fake cue that recoils).
    // A stutter cue changes speed on its way in (impact time unchanged); a Heavy hit must be held (braced) through impact.
    public class DefenseQte : Qte
    {
        public readonly QteProfile profile;
        public readonly float[] impacts, delays;  // impact time and extra hold per hit
        public readonly bool[] heavy;             // Heavy marker per hit: brace (press and hold through impact)
        public readonly DodgeDir[] cues;          // None = press the QTE key; otherwise press this arrow
        readonly (float time, float dist)[] stutter; // per hit speed break; time 0 = steady cue
        readonly float[] feintStarts;             // when the fake cue appears; < 0 = no feint
        readonly QteResult?[] results;
        readonly QteOutcomeCategory?[] categories;
        readonly bool[] wrong;                    // hit spent by the wrong input (key vs arrow, or wrong arrow)
        readonly bool[] bracing, letGo;           // Heavy hit pressed and still held / released before impact

        // intervals: seconds between consecutive markers (Accelerando etc.); null = profile.hitInterval (Steady).
        public DefenseQte(QteProfile profile, int hits, float firstHitDelay = 0f, float timingBonusFraction = 0f,
            float[] intervals = null, bool[] heavy = null, DodgeDir[] cues = null, bool[] feints = null,
            (float time, float dist)[] stutter = null)
        {
            this.profile = profile;
            TimingBonusFraction = timingBonusFraction;
            impacts = new float[hits];
            delays = new float[hits];
            this.heavy = heavy ?? new bool[hits];
            this.cues = cues ?? new DodgeDir[hits];
            this.stutter = stutter ?? new (float, float)[hits];
            feintStarts = Enumerable.Repeat(-1f, hits).ToArray();
            results = new QteResult?[hits];
            categories = new QteOutcomeCategory?[hits];
            wrong = new bool[hits];
            bracing = new bool[hits];
            letGo = new bool[hits];
            delays[0] = firstHitDelay;
            float t = profile.telegraph + profile.windup + firstHitDelay;
            for (int i = 0; i < hits; i++)
            {
                if (feints != null && i < feints.Length && feints[i])
                {
                    feintStarts[i] = t - profile.windup - delays[i]; // the fake shows up when the real cue would have
                    t += FeintTime;                                // and the real cue (and every later hit) comes after it
                }
                impacts[i] = t;
                t += intervals != null && i < intervals.Length ? intervals[i] : profile.hitInterval;
            }
        }

        public float FeintTime => profile.windup * QteConfig.FeintStop + QteConfig.FeintRecoil;
        public bool IsFeint(int i) => feintStarts[i] >= 0f;
        public float FeintStart(int i) => feintStarts[i];
        public bool InTelegraph => T < profile.telegraph;
        public int Hits => impacts.Length;
        public QteResult? HitResult(int i) => results[i];
        public QteOutcomeCategory? HitCategory(int i) => categories[i];
        public bool WrongInput(int i) => wrong[i];
        public bool LetGo(int i) => letGo[i];
        public bool Stutters(int i) => stutter[i].time > 0f;

        // Visual progress of hit i's cue for a linear run fraction x (0 = spawn, 1 = impact). Steady cues return x;
        // a stutter cue covers `dist` of the track in `time` of its run, then the rest. Past 1 it keeps the last speed.
        public float CueProgress(int i, float x)
        {
            var (bt, bd) = stutter[i];
            if (bt <= 0f || bt >= 1f) return x;
            return x <= bt ? x / bt * bd : bd + (x - bt) / (1f - bt) * (1f - bd);
        }
        public IReadOnlyList<QteResult> Results => results.Select(r => r ?? QteResult.Miss).ToList();
        public IReadOnlyList<QteOutcomeCategory> Categories => categories.Select(c => c ?? QteOutcomeCategory.Failure).ToList();

        // When hit i's cue appears on the track.
        public float SpawnTime(int i) => impacts[i] - profile.windup - delays[i];

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Done) return;
            float effectiveGood = QteConfig.ScaleGood(profile, TimingBonusFraction);
            for (int i = 0; i < results.Length; i++)
                if (results[i] == null && T > impacts[i] + effectiveGood)
                {
                    results[i] = QteResult.Miss;
                    categories[i] = QteOutcomeCategory.Failure;
                }
            CheckDone();
        }

        // Presses during the telegraph are ignored; a press commits to the next unresolved hit,
        // so pressing too early (or on a feint) counts as a miss for that hit (no free mashing).
        public override void Press() => Commit(DodgeDir.None);

        // Arrow input. The wrong input for the cue (arrow on a key cue, key or wrong arrow on an arrow cue) is a miss.
        public override void PressDirection(DodgeDir d) => Commit(d);

        // Brace: letting go of a Heavy hit before it lands drops that hit to Poor.
        public override void Release()
        {
            if (Done) return;
            for (int i = 0; i < bracing.Length; i++)
            {
                if (!bracing[i]) continue;
                bracing[i] = false;
                if (T >= impacts[i]) continue;
                letGo[i] = true;
                results[i] = QteResult.Miss;
                categories[i] = QteOutcomeCategory.Failure;
            }
            CheckDone();
        }

        void Commit(DodgeDir input)
        {
            if (Done || InTelegraph) return;
            int i = Array.FindIndex(results, r => r == null);
            if (i < 0) return;
            if (input != cues[i])
            {
                wrong[i] = true;
                results[i] = QteResult.Miss;
                categories[i] = QteOutcomeCategory.Failure;
            }
            else
            {
                float err = T - impacts[i];
                results[i] = QteConfig.Judge(err, profile, TimingBonusFraction);
                categories[i] = QteConfig.JudgeCategory(err, profile, TimingBonusFraction);
                if (heavy[i] && input == DodgeDir.None && err < 0f && results[i] != QteResult.Miss) bracing[i] = true;
            }
            CheckDone();
        }

        void CheckDone()
        {
            // A braced Heavy hit stays open until it lands, so an early release can still break the brace.
            for (int i = 0; i < bracing.Length; i++)
                if (bracing[i] && T < impacts[i]) return;
            if (results.All(r => r != null))
                Finish(Results.Max(), Categories.Max()); // overall = worst hit (Miss > Good > Perfect)
        }
    }

    // Which QTE an action gets. The one place this decision lives.
    public static class QteFactory
    {
        // GDD Bab 12: Only Basic Attack and skills with direct damage use Offensive QTE.
        // Heals, buffs, debuffs without direct damage do NOT use QTE.
        // Which offensive QTE an action gets (ignoring Auto assist). Physical skills pick a style from data they already have.
        public static OffenseQteKind KindFor(BattleAction a)
        {
            if (a.kind == ActionKind.Attack) return OffenseQteKind.Timing;
            var s = a.kind == ActionKind.Skill ? a.skill : null;
            if (s == null) return OffenseQteKind.None;
            if (s.kind == SkillKind.Magic)
            {
                if (!(s.power > 0f || s.HasDamageSystemComponents)) return OffenseQteKind.None;
                return string.IsNullOrEmpty(s.word) ? OffenseQteKind.Timing : OffenseQteKind.Typing;
            }
            if (s.kind != SkillKind.Physical) return OffenseQteKind.None;
            if (s.isRanged) return OffenseQteKind.MovingTarget;
            if (s.power >= QteConfig.ChargePowerThreshold) return OffenseQteKind.Charge;
            if (s.target == TargetType.AllEnemies || s.hits > 1) return OffenseQteKind.Combo;
            return OffenseQteKind.Timing;
        }

        public static Qte Offense(BattleAction a, Tier tier, QteAssist assist, Random rng, RunState run = null)
        {
            var kind = KindFor(a);
            if (kind == OffenseQteKind.None) return null;
            if (assist == QteAssist.Auto) return new AutoQte();
            var profile = QteConfig.For(tier, assist);
            var r = run ?? a.actor?.run;
            float charBonus = a.actor != null ? a.actor.unit.Derived.QteTimingBonusFraction : 0f;
            float modDelta = (a.actor != null && a.actor.IsPlayer && r?.ModifierManager != null)
                ? r.ModifierManager.GetOffensiveQteWindowDelta()
                : 0f;
            float windowBonus = Math.Clamp(charBonus + modDelta, -0.50f, DungeonModifierManager.MaxQteWindowBonus);
            if (r?.Tutorial != null && r.Tutorial.FirstTime("offense_" + kind)) profile = profile.Scaled(1f, Prologue.FirstTimeSlow);
            return kind switch
            {
                OffenseQteKind.Typing => TypingQte.For(a.skill, profile, rng),
                OffenseQteKind.Charge => new ChargeQte(profile, QteConfig.ZoneMin + (float)rng.NextDouble() * (QteConfig.ZoneMax - QteConfig.ZoneMin), windowBonus),
                OffenseQteKind.MovingTarget => new MovingTargetQte(profile, (float)(rng.NextDouble() * 2 * Math.PI), windowBonus),
                OffenseQteKind.Combo => new ComboQte(profile, a.skill.hits > 1 ? Math.Clamp(a.skill.hits, 2, 4) : QteConfig.ComboZones, rng, windowBonus),
                _ => TimingQte.Random(profile, rng, windowBonus),
            };
        }

        // Enemy hits: reaction QTE at the attacker's tier. Self stances need none (null).
        // GDD Bab 12 §8.2: No attributes scale QTE. Window bonuses come from equipment/talents/passives/modifiers, clamped to +25%.
        public static Qte Defense(BattleAction a, QteAssist assist, RunState run = null, Random rng = null)
        {
            rng ??= new Random();
            var move = a.attack;
            // Stances, charge turns and status-only actions have no direct damage, so no Defensive QTE (Bab 11 §2.3).
            if (a.charging || (move != null && (move.self || move.StatusOnly || move.Heals))) return null;
            int hits = move?.hits ?? 1;
            if (a.target != null && a.target.CannotReact) return new AutoQte(hits, QteResult.Poor); // Battle.Deal forces Poor anyway
            if (assist == QteAssist.Auto) return new AutoQte(hits);
            var profile = QteConfig.For(a.actor.enemy?.tier ?? Tier.Normal, assist);
            var evasion = a.target?.Get(StatusId.Evasion);
            if (evasion != null) profile = profile.Scaled(evasion.magnitude, 1f);

            var r = run ?? a.target?.run;
            float statTimingBonus = a.target != null ? a.target.unit.Derived.QteTimingBonusFraction : 0f;
            float modDelta = (a.target != null && a.target.IsPlayer && r?.ModifierManager != null)
                ? r.ModifierManager.GetDefensiveQteWindowDelta()
                : 0f;
            float totalTimingBonus = Math.Clamp(statTimingBonus + modDelta, -0.50f, DungeonModifierManager.MaxQteWindowBonus);
            var tier = a.actor.enemy?.tier ?? Tier.Normal;
            // Tutorial Prologue: feint/stutter ramp, one scripted feint, and each QTE type runs slower the first time.
            var tut = r?.Tutorial;
            var ramp = tut?.Ramp(a.actor, move);
            if (tut != null && tut.FirstTime(ramp?.forceFeint == true ? "feint" : PrologueScript.DefenseKind(move, hits)))
                profile = profile.Scaled(1f, Prologue.FirstTimeSlow);
            // Drain attacks are fought off by mashing (Struggle); other magic is warded by typing runes.
            if (move != null && move.drain > 0f) return new StruggleQte(profile, hits, move.delay);
            if (move != null && move.UsesMagic) return WardQte.For(profile, hits, rng);

            // Physical hits: Heavy hits are braced (press and hold); single hits and Elite/Boss multi-hit chains are
            // dodged by arrow; any tier may feint one hit, stutter a cue, and untimed multi-hits land on uneven beats.
            var heavy = move == null ? new bool[hits] : Enumerable.Range(0, hits).Select(move.HeavyAt).ToArray();
            DodgeDir[] cues = null;
            if (hits == 1 || tier >= Tier.Elite)
            {
                cues = new DodgeDir[hits];
                for (int i = 0; i < hits; i++)
                    if (!heavy[i]) cues[i] = (DodgeDir)(1 + rng.Next(4));
            }
            bool[] feints = null;
            if (ramp?.forceFeint == true || rng.NextDouble() < (ramp?.feint ?? QteConfig.FeintChance(tier)))
            {
                feints = new bool[hits];
                feints[rng.Next(hits)] = true;
            }
            var stutter = new (float, float)[hits];
            for (int i = 0; i < hits; i++)
                if (rng.NextDouble() < (ramp?.stutter ?? QteConfig.StutterChance(tier)))
                    stutter[i] = rng.Next(2) == 0
                        ? (QteConfig.StutterSlowTime, QteConfig.StutterSlowDistance)
                        : (QteConfig.StutterFastTime, QteConfig.StutterFastDistance);
            float[] intervals = move?.intervals;
            if (intervals == null && hits > 1)
            {
                intervals = new float[hits - 1];
                for (int i = 0; i < intervals.Length; i++)
                    intervals[i] = Math.Max(QteConfig.MinHitInterval,
                        profile.hitInterval + ((float)rng.NextDouble() * 2f - 1f) * QteConfig.IntervalJitter);
            }
            return new DefenseQte(profile, hits, move?.delay ?? 0f, totalTimingBonus, intervals, heavy, cues, feints, stutter);
        }
    }

    // Drain attacks: mash the QTE key to fill a gauge that keeps draining, before the siphon lands.
    // Filling it completely ends early as a Perfect; at the deadline >= StruggleGood is a Good, else Poor.
    // One grade covers every hit of the attack.
    public class StruggleQte : Qte
    {
        public readonly QteProfile profile;
        public readonly int hits;
        public readonly float deadline;
        public float Gauge { get; private set; }

        public StruggleQte(QteProfile profile, int hits, float delay = 0f)
        {
            this.profile = profile;
            this.hits = Math.Max(1, hits);
            deadline = profile.telegraph + profile.windup * QteConfig.StruggleWindup + delay;
        }

        public bool InTelegraph => T < profile.telegraph;
        public float TimeLeft => Math.Max(0f, deadline - T);
        public IReadOnlyList<QteResult> Results => Enumerable.Repeat(Result, hits).ToList();

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Done) return;
            if (!InTelegraph) Gauge = Math.Max(0f, Gauge - QteConfig.MashDecay * dt);
            if (T >= deadline) Finish(Gauge >= QteConfig.StruggleGood ? QteResult.Good : QteResult.Miss);
        }

        public override void Press()
        {
            if (Done || InTelegraph) return;
            Gauge = Math.Min(1f, Gauge + QteConfig.MashFill);
            if (Gauge >= QteConfig.StrugglePerfect) Finish(QteResult.Perfect);
        }
    }

    // QTEs switched off in settings: resolve instantly with a predictable Good.
    public class AutoQte : Qte
    {
        public readonly int hits;
        // grade Poor = the defender cannot react (Stun/Sleep/Mind Control): no bar is shown at all.
        public AutoQte(int hits = 1, QteResult grade = QteResult.Good) { this.hits = hits; Finish(grade); }
        public IReadOnlyList<QteResult> Results => Enumerable.Repeat(Result, hits).ToList();
    }
}
