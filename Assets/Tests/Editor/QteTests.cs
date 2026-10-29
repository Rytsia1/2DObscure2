using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class QteTests
    {
        static QteProfile N => QteConfig.Profiles[Tier.Normal];
        static QteProfile E => QteConfig.Profiles[Tier.Elite];
        static QteProfile B => QteConfig.Profiles[Tier.Boss];

        // Advance a QTE clock in small steps, as frames would.
        static void Wait(Qte q, float seconds)
        {
            const float step = 0.005f;
            for (float t = step; t <= seconds + 1e-5f; t += step) q.Tick(step);
        }

        static QteResult TimingAt(QteProfile p, float error)
        {
            var q = new TimingQte(p, 0.6f);
            Wait(q, q.target + error);
            q.Press();
            return q.Result;
        }

        [Test]
        public void ProfilesMatchTheTuningTable()
        {
            Assert.AreEqual((0.100f, 0.220f), (N.perfect, N.good));
            Assert.AreEqual((0.065f, 0.160f), (E.perfect, E.good));
            Assert.AreEqual((0.040f, 0.110f), (B.perfect, B.good));
            var relaxed = QteConfig.For(Tier.Boss, QteAssist.Relaxed);
            Assert.Greater(relaxed.good, B.good);
            Assert.Greater(relaxed.windup, B.windup);
            Assert.AreSame(B, QteConfig.For(Tier.Boss, QteAssist.Standard));
        }

        [Test]
        public void AttackTimingProducesPerfectGoodMiss()
        {
            Assert.AreEqual(QteResult.Perfect, TimingAt(N, 0f));
            Assert.AreEqual(QteResult.Perfect, TimingAt(N, -0.09f));
            Assert.AreEqual(QteResult.Good, TimingAt(N, 0.15f));
            Assert.AreEqual(QteResult.Miss, TimingAt(N, -0.3f));

            var idle = new TimingQte(N, 0.6f);
            Wait(idle, N.sweep + 0.02f);
            Assert.IsTrue(idle.Done, "times out when the indicator leaves the bar");
            Assert.AreEqual(QteResult.Miss, idle.Result);
            idle.Press();
            Assert.AreEqual(QteResult.Miss, idle.Result, "no input after it ends");
        }

        [Test]
        public void TimingDifficultyScalesWithEnemyTier()
        {
            Assert.AreEqual(QteResult.Perfect, TimingAt(N, 0.08f));
            Assert.AreEqual(QteResult.Good, TimingAt(E, 0.08f));
            Assert.AreEqual(QteResult.Good, TimingAt(B, 0.08f));
            Assert.AreEqual(QteResult.Good, TimingAt(N, 0.18f));
            Assert.AreEqual(QteResult.Miss, TimingAt(E, 0.18f));
            Assert.AreEqual(QteResult.Miss, TimingAt(B, 0.18f));
            Assert.Less(B.sweep, N.sweep, "boss bar moves faster");
        }

        [Test]
        public void TypingAcceptsTheSequenceAndRejectsWrongLetters()
        {
            var perfect = new TypingQte("IGNIS", 5, 5f);
            foreach (var c in "ignis") perfect.Type(c); // case-insensitive
            Assert.AreEqual(QteResult.Perfect, perfect.Result);

            var good = new TypingQte("IGNIS", 5, 5f);
            good.Type('I');
            good.Type('X');
            Assert.AreEqual(1, good.Index, "wrong letter does not advance");
            Assert.AreEqual(1, good.Mistakes);
            good.Type(' ');
            Assert.AreEqual(1, good.Mistakes, "non-letters are ignored");
            foreach (var c in "GNIS") good.Type(c);
            Assert.AreEqual(QteResult.Good, good.Result);

            var slow = new TypingQte("IGNIS", 5, 5f);
            Wait(slow, 3.5f); // past the perfect mark
            foreach (var c in "IGNIS") slow.Type(c);
            Assert.AreEqual(QteResult.Good, slow.Result);

            var fumble = new TypingQte("IGNIS", 5, 5f);
            foreach (var c in "QQQ") fumble.Type(c);
            Assert.IsTrue(fumble.Done);
            Assert.AreEqual(QteResult.Miss, fumble.Result);

            var timeout = new TypingQte("IGNIS", 5, 2f);
            Wait(timeout, 2.05f);
            Assert.AreEqual(QteResult.Miss, timeout.Result);
        }

        [Test]
        public void TypingLengthAndTimeScaleWithTierAndSkill()
        {
            var rng = new Random(1);
            var ignis = Database.Skills["firebolt"];
            Assert.AreEqual(5, TypingQte.For(ignis, N, rng).sequence.Length);
            Assert.AreEqual(6, TypingQte.For(ignis, E, rng).sequence.Length);
            Assert.AreEqual(7, TypingQte.For(ignis, B, rng).sequence.Length);
            var burst = TypingQte.For(Database.Skills["arcane_burst"], B, rng);
            Assert.AreEqual(8 + 2 + 1, burst.sequence.Length, "hard spell vs boss: word + boss runes + hard rune");
            StringAssert.StartsWith("AETHERIS", burst.sequence);
            Assert.IsTrue(burst.sequence.Substring(8).All(c => QteConfig.RuneLetters.Contains(c)));

            var heal = TypingQte.For(Database.Skills["heal"], N, rng);
            Assert.AreEqual(4 * N.letterTime * 1.2f, heal.timeLimit, 1e-4, "easy spells get more time per letter");
            Assert.Less(TypingQte.For(ignis, B, rng).timeLimit / 7, TypingQte.For(ignis, N, rng).timeLimit / 5);
        }

        [Test]
        public void DefenseJudgesEachHitAgainstItsImpact()
        {
            var q = new DefenseQte(N, 1);
            float impact = N.telegraph + N.windup;
            Assert.AreEqual(impact, q.impacts[0], 1e-5);
            Wait(q, 0.5f);
            q.Press();
            Assert.IsNull(q.HitResult(0), "presses during the telegraph are ignored");
            Wait(q, impact - 0.5f);
            q.Press();
            Assert.AreEqual(QteResult.Perfect, q.Result);

            var early = new DefenseQte(N, 1);
            Wait(early, N.telegraph + 0.2f);
            early.Press();
            Assert.AreEqual(QteResult.Miss, early.Result, "early press commits and misses");

            var idle = new DefenseQte(N, 1);
            Wait(idle, impact + N.good + 0.02f);
            Assert.IsTrue(idle.Done);
            Assert.AreEqual(QteResult.Miss, idle.Result);
        }

        [Test]
        public void DefenseMultiHitAndDelayedAttacks()
        {
            var q = new DefenseQte(B, 3, 0.5f);
            Assert.AreEqual(B.telegraph + B.windup + 0.5f, q.impacts[0], 1e-5, "delay pushes the first impact back");
            Assert.AreEqual(B.hitInterval, q.impacts[1] - q.impacts[0], 1e-5);

            Wait(q, q.impacts[0]); q.Press();
            Wait(q, q.impacts[1] - q.T + 0.06f); q.Press();      // inside good, outside perfect
            Wait(q, q.impacts[2] + B.good + 0.02f - q.T);        // never pressed
            CollectionAssert.AreEqual(new[] { QteResult.Perfect, QteResult.Good, QteResult.Miss }, q.Results);
            Assert.AreEqual(QteResult.Miss, q.Result, "overall result is the worst hit");
        }

        [Test]
        public void CancelEndsWithoutResultAndAutoIsPredictable()
        {
            var q = new TimingQte(N, 0.6f);
            q.Cancel();
            Assert.IsTrue(q.Done && q.Cancelled);
            q.Press();
            Assert.IsTrue(q.Cancelled);

            var auto = new AutoQte(3);
            Assert.IsTrue(auto.Done);
            Assert.AreEqual(QteResult.Good, auto.Result);
            CollectionAssert.AreEqual(Enumerable.Repeat(QteResult.Good, 3), auto.Results);
        }

        // ---- combat integration ----

        static Battle NewBattle(int seed, params string[] enemies) => new Battle(
            Database.Party.Select(id => new Unit(Database.Characters[id])),
            enemies.Select(id => Database.Enemies[id]), new List<string>(Database.StartingItems), new Random(seed));

        static Combatant AdvanceTo(Battle b, Func<Combatant, bool> who)
        {
            for (int i = 0; i < 100; i++)
            {
                var c = b.NextTurn();
                if (who(c)) return c;
                b.Perform(BattleAction.Guard(c));
            }
            throw new Exception("never reached");
        }

        static EnemyAttack Move(string enemy, string name) => Database.Enemies[enemy].attacks.First(a => a.name == name);

        int DefendedDamage(QteResult? reaction)
        {
            var b = NewBattle(7, "skeleton");
            var sk = AdvanceTo(b, c => c.IsEnemy);
            var knight = b.Party[0];
            int before = knight.unit.hp;
            b.Perform(BattleAction.Strike(sk, Move("skeleton", "Rusty Slash"), knight), 1f, reaction == null ? null : new[] { reaction.Value });
            return before - knight.unit.hp;
        }

        [Test]
        public void DefenseResultsScaleIncomingDamage()
        {
            int full = DefendedDamage(QteResult.Miss);
            Assert.AreEqual(full, DefendedDamage(null), "no reaction = full damage");
            Assert.AreEqual(0, DefendedDamage(QteResult.Perfect), "perfect guard negates");
            Assert.AreEqual(Math.Max(1, (int)Math.Round(full * 0.5f)), DefendedDamage(QteResult.Good));
        }

        int OffenseDamage(float power)
        {
            var b = NewBattle(7, "warden");
            var rogue = AdvanceTo(b, c => c.name == "Rogue");
            var warden = b.Enemies[0];
            b.Perform(BattleAction.Attack(rogue, warden), power);
            return warden.Stats.maxHp - warden.unit.hp;
        }

        [Test]
        public void AttackResultScalesDamageThroughTheNormalFormula()
        {
            int perfect = OffenseDamage(QteConfig.Offense(QteResult.Perfect));
            int good = OffenseDamage(QteConfig.Offense(QteResult.Good));
            int miss = OffenseDamage(QteConfig.Offense(QteResult.Miss));
            Assert.Greater(perfect, good);
            Assert.Greater(good, miss);
            // Multiplier applies before defense: 16 Atk vs 10 Def.
            Assert.Less(perfect, (int)Math.Ceiling(good * 1.25f) + 2, "defense still subtracts after the bonus");
        }

        [Test]
        public void SpellResultScalesHealing()
        {
            int Healed(float power)
            {
                var b = NewBattle(7, "skeleton");
                var healer = AdvanceTo(b, c => c.name == "Healer");
                var knight = b.Party[0];
                knight.unit.hp = 1;
                b.Perform(BattleAction.Skill(healer, Database.Skills["heal"], knight), power);
                return knight.unit.hp - 1;
            }
            Assert.Greater(Healed(1.25f), Healed(1f));
            Assert.Greater(Healed(1f), Healed(0.7f));
        }

        [Test]
        public void MultiHitResolvesEachHitOnce()
        {
            var b = NewBattle(7, "warden");
            var warden = AdvanceTo(b, c => c.IsEnemy);
            var mage = b.Party[2];
            int before = mage.unit.hp;
            var hits = b.Perform(BattleAction.Strike(warden, Move("warden", "Chain Lash"), mage), 1f,
                new[] { QteResult.Perfect, QteResult.Good, QteResult.Miss });
            Assert.AreEqual(3, hits.Count);
            Assert.IsTrue(hits[0].blocked);
            Assert.AreEqual(0, hits[0].amount);
            Assert.AreEqual(before - hits.Sum(h => h.amount), mage.unit.hp);
            Assert.IsNull(b.Active, "exactly one turn consumed");
            Assert.IsNotNull(b.Validate(BattleAction.Guard(warden)));
        }

        [Test]
        public void MultiTargetAttackHitsEachLivingPartyMemberOnce()
        {
            var b = NewBattle(7, "warden");
            b.Party[1].unit.hp = 0;
            var warden = AdvanceTo(b, c => c.IsEnemy);
            var gaze = Move("warden", "Abyssal Gaze");
            var action = BattleAction.Strike(warden, gaze, b.Party[0]);
            Assert.IsNull(action.target, "area attacks carry no single target");
            Assert.IsNull(b.Validate(action));
            var hits = b.Perform(action, 1f, new[] { QteResult.Perfect });
            CollectionAssert.AreEquivalent(b.Party.Where(p => p.Alive), hits.Select(h => h.target));
            Assert.IsTrue(hits.All(h => h.blocked), "one shared reaction applies to every target");
        }

        [Test]
        public void EnemyAiPicksMovesFromItsPattern()
        {
            var b = NewBattle(3, "warden");
            var warden = AdvanceTo(b, c => c.IsEnemy);
            var names = new HashSet<string>();
            for (int i = 0; i < 40; i++) names.Add(EnemyAI.Choose(b, warden).attack.name);
            CollectionAssert.AreEquivalent(new[] { "Chain Lash", "Crushing Grip" }, names, "phase-1 moves only");
        }
    }
}
