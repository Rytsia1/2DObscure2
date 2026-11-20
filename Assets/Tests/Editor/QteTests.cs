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
            // GDD Bab 12 §6.1: W_P / W_G total widths 90/240, 80/210, 72/180 ms -> half-widths; T_lead = windup.
            Assert.AreEqual((0.045f, 0.120f, 1.0f), (N.perfect, N.good, N.windup));
            Assert.AreEqual((0.040f, 0.105f, 0.9f), (E.perfect, E.good, E.windup));
            Assert.AreEqual((0.036f, 0.090f, 0.8f), (B.perfect, B.good, B.windup));
            Assert.AreEqual(0.35f, N.hitInterval, "Steady 350 ms");
            var relaxed = QteConfig.For(Tier.Boss, QteAssist.Relaxed);
            Assert.Greater(relaxed.good, B.good);
            Assert.Greater(relaxed.windup, B.windup);
            Assert.AreSame(B, QteConfig.For(Tier.Boss, QteAssist.Standard));
        }

        [Test]
        public void AttackTimingProducesPerfectGoodMiss()
        {
            Assert.AreEqual(QteResult.Perfect, TimingAt(N, 0f));
            Assert.AreEqual(QteResult.Perfect, TimingAt(N, -0.04f));
            Assert.AreEqual(QteResult.Good, TimingAt(N, 0.10f));
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
            // Windows are a few ms apart per tier, finer than the 5 ms frame steps of TimingAt: judge the error directly.
            Assert.AreEqual(QteResult.Perfect, QteConfig.Judge(0.042f, N));
            Assert.AreEqual(QteResult.Good, QteConfig.Judge(0.042f, E));
            Assert.AreEqual(QteResult.Good, QteConfig.Judge(0.042f, B));
            Assert.AreEqual(QteResult.Good, QteConfig.Judge(0.112f, N));
            Assert.AreEqual(QteResult.Miss, QteConfig.Judge(0.112f, E));
            Assert.AreEqual(QteResult.Miss, QteConfig.Judge(0.112f, B));
            Assert.AreEqual(QteResult.Perfect, TimingAt(N, 0f));
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
            // Multiplier applies before defense (GDD Bab 11 §2.2: Perfect is 1.2x).
            Assert.Less(perfect, (int)Math.Ceiling(good * QteConfig.PerfectPower) + 2, "defense still subtracts after the bonus");
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
            foreach (var p in b.Party) p.guarding = true; // a full parry needs Guard or a shield (Bab 12 §3.5)
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

        [Test]
        public void AllDirectDamageSkillsAcrossClassesTriggerOffensiveQte()
        {
            var rng = new Random(42);
            var b = NewBattle(42, "skeleton");
            var actor = b.Party[0];
            var target = b.Enemies[0];

            foreach (var skill in Database.Skills.Values)
            {
                bool isDirectDamage = skill.kind == SkillKind.Physical
                    || (skill.kind == SkillKind.Magic && (skill.power > 0f || skill.HasDamageSystemComponents));
                var qte = QteFactory.Offense(BattleAction.Skill(actor, skill, target), Tier.Normal, QteAssist.Standard, rng);
                if (isDirectDamage)
                {
                    Assert.IsNotNull(qte, $"Direct-damage skill '{skill.id}' ({skill.kind}) must trigger a QTE");
                    if (skill.kind == SkillKind.Magic && !string.IsNullOrEmpty(skill.word))
                        Assert.IsInstanceOf<TypingQte>(qte, $"Magic skill '{skill.id}' with word should trigger TypingQte");
                    var kind = QteFactory.KindFor(BattleAction.Skill(actor, skill, target));
                    Type expected = kind switch
                    {
                        OffenseQteKind.Typing => typeof(TypingQte),
                        OffenseQteKind.Charge => typeof(ChargeQte),
                        OffenseQteKind.MovingTarget => typeof(MovingTargetQte),
                        OffenseQteKind.Combo => typeof(ComboQte),
                        _ => typeof(TimingQte),
                    };
                    Assert.IsInstanceOf(expected, qte, $"Skill '{skill.id}' should trigger {expected.Name}");

                    if (skill.kind == SkillKind.Magic && !string.IsNullOrEmpty(skill.classId))
                        Assert.IsFalse(string.IsNullOrEmpty(skill.word), $"Playable class Magic skill '{skill.id}' should define a Typing QTE word");
                }
                else
                {
                    Assert.IsNull(qte, $"Non-damaging skill '{skill.id}' ({skill.kind}) must not trigger Offensive QTE");
                }
            }
        }

        [Test]
        public void CombatActiveSkillsReflectSkillLoadoutAndRelicQteHooksFire()
        {
            var map = MapGenerator.Generate(10, 1);
            var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
            var run = new RunState(map, partyDefs, 10);
            run.TryAddRelic("R002", out _);
            run.TryAddRelic("R019", out _);

            // Equip a newly unlocked Wizard skill into slot 0 and verify GetCombatActiveSkillIds picks it up
            var mageUnit = run.Party[2];
            mageUnit.skillLoadout.UnlockActive("lightning_strike");
            Assert.IsTrue(mageUnit.skillLoadout.TryEquipActive(0, "lightning_strike", out _));
            CollectionAssert.Contains(mageUnit.GetCombatActiveSkillIds(), "lightning_strike");

            var b = new Battle(run.Party, new[] { Database.Enemies["warden"] }, run.Items, new Random(10), run);
            var mage = AdvanceTo(b, c => c.name == "Mage");
            mage.unit.mp = 10;
            int mpBefore = mage.unit.mp;

            // 3 Perfect standard skill casts trigger R002 Soulglass Shard (+2% Max MP)
            var firebolt = Database.Skills["firebolt"];
            int cost = mage.GetEffectiveMpCost(firebolt, run);
            for (int i = 0; i < 3; i++)
            {
                b.Enemies[0].unit.hp = b.Enemies[0].Stats.maxHp;
                b.Perform(BattleAction.Skill(mage, firebolt, b.Enemies[0]), QteConfig.PerfectPower);
                if (i < 2)
                {
                    mage = AdvanceTo(b, c => c.name == "Mage");
                    mage.unit.mp = mpBefore;
                }
            }
            Assert.Greater(mage.unit.mp, mpBefore - cost, "3rd Perfect standard Magic skill QTE must trigger R002 MP restore");

            // Perfect Defensive QTE triggers R019 Black-Iron Rosary shield
            var sk = AdvanceTo(b, c => c.IsEnemy);
            var knight = b.Party[0];
            int shieldBefore = knight.unit.shield;
            b.Perform(BattleAction.Strike(sk, Move("warden", "Crushing Grip"), knight), 1f, new[] { QteResult.Perfect });
            Assert.Greater(knight.unit.shield, shieldBefore, "Perfect Defensive QTE must trigger R019 shield");

            // Non-Halfling must not have Second Chance retries
            Assert.IsFalse(knight.CanUseSecondChanceRetry, "Human Knight must not trigger Halfling Second Chance");
        }

        // --- Offensive QTE variety ---

        [Test]
        public void PhysicalSkillsRouteToQteStylesByTrait()
        {
            var b = NewBattle(1, "skeleton");
            var actor = b.Party[0];
            var target = b.Enemies[0];
            OffenseQteKind K(SkillDef s) => QteFactory.KindFor(BattleAction.Skill(actor, s, target));
            var phys = Database.Skills.Values.Where(s => s.kind == SkillKind.Physical).ToList();

            foreach (var s in phys.Where(s => s.isRanged))
                Assert.AreEqual(OffenseQteKind.MovingTarget, K(s), s.id);
            foreach (var s in phys.Where(s => !s.isRanged && s.power >= QteConfig.ChargePowerThreshold))
                Assert.AreEqual(OffenseQteKind.Charge, K(s), s.id);
            foreach (var s in phys.Where(s => !s.isRanged && s.power < QteConfig.ChargePowerThreshold && (s.target == TargetType.AllEnemies || s.hits > 1)))
                Assert.AreEqual(OffenseQteKind.Combo, K(s), s.id);

            Assert.AreEqual(OffenseQteKind.Timing, QteFactory.KindFor(BattleAction.Attack(actor, target)), "basic attack keeps the sweep bar");
            foreach (var kind in new[] { OffenseQteKind.MovingTarget, OffenseQteKind.Charge, OffenseQteKind.Combo, OffenseQteKind.Timing })
                Assert.IsTrue(phys.Any(s => K(s) == kind), $"at least one physical skill uses {kind}");
        }

        [Test]
        public void ChargeJudgesTheReleaseAgainstTheGoldZone()
        {
            QteResult HoldFor(float seconds)
            {
                var q = new ChargeQte(N, 0.7f);
                Wait(q, 0.2f);
                q.Press();
                Wait(q, seconds);
                q.Release();
                return q.Result;
            }
            float target = N.sweep * 0.7f;
            Assert.AreEqual(QteResult.Perfect, HoldFor(target));
            Assert.AreEqual(QteResult.Good, HoldFor(target + 0.10f));
            Assert.AreEqual(QteResult.Miss, HoldFor(target - 0.4f), "released too early");

            var over = new ChargeQte(N, 0.7f);
            over.Press();
            Wait(over, N.sweep + 0.02f);
            Assert.IsTrue(over.Done, "a full gauge overcharges");
            Assert.AreEqual(QteResult.Miss, over.Result);

            var stray = new ChargeQte(N, 0.7f);
            stray.Release();
            Assert.IsFalse(stray.Done, "releasing a key held from before the QTE does nothing");
            Wait(stray, N.sweep * QteConfig.ChargeIdleTimeout + 0.02f);
            Assert.IsTrue(stray.Done, "never charging times out");
            Assert.AreEqual(QteResult.Miss, stray.Result);
        }

        [Test]
        public void MovingTargetRewardsPressingWhenTheMarkMeetsTheZone()
        {
            // Find when the mark crosses the drifting zone, then press there and far from there.
            float CrossTime(float phase)
            {
                var probe = new MovingTargetQte(N, phase);
                float best = 0f, bestGap = float.MaxValue;
                for (int i = 0; i < 280 && !probe.Done; i++)
                {
                    probe.Tick(0.005f);
                    float gap = Math.Abs(probe.MarkFraction - probe.ZoneFraction);
                    if (gap < bestGap) { bestGap = gap; best = probe.T; }
                }
                return best;
            }
            foreach (float phase in new[] { 0f, 1.5f, 3f, 4.5f })
            {
                float cross = CrossTime(phase);
                var hit = new MovingTargetQte(N, phase);
                Wait(hit, cross);
                hit.Press();
                Assert.AreEqual(QteResult.Perfect, hit.Result, $"phase {phase}");

                var early = new MovingTargetQte(N, phase);
                Wait(early, 0.05f);
                early.Press();
                Assert.AreEqual(QteResult.Miss, early.Result, $"phase {phase}: mark nowhere near the zone");
            }

            var idle = new MovingTargetQte(N, 0f);
            Wait(idle, N.sweep + 0.02f);
            Assert.IsTrue(idle.Done);
            Assert.AreEqual(QteResult.Miss, idle.Result);
        }

        [Test]
        public void ComboAveragesEachZoneAndPunishesMashing()
        {
            var perfect = new ComboQte(N, 3, new Random(1));
            for (int i = 0; i < 3; i++)
            {
                Wait(perfect, perfect.zoneTimes[i] - perfect.T);
                perfect.Press();
            }
            Assert.IsTrue(perfect.Done);
            Assert.AreEqual(QteResult.Perfect, perfect.Result);
            Assert.AreEqual(QteConfig.PerfectPower, perfect.AveragePower(QteConfig.Offense), 1e-4f);

            var skipped = new ComboQte(N, 3, new Random(1));
            Wait(skipped, skipped.zoneTimes[0] - skipped.T); skipped.Press();
            Wait(skipped, skipped.zoneTimes[2] - skipped.T); skipped.Press(); // zone 2 already lapsed, so this lands on zone 3
            Assert.IsTrue(skipped.Done);
            Assert.AreEqual(QteResult.Miss, skipped.ZoneResult(1));
            Assert.AreEqual((QteConfig.PerfectPower + QteConfig.PoorPower + QteConfig.PerfectPower) / 3f, skipped.AveragePower(QteConfig.Offense), 1e-4f);

            var masher = new ComboQte(N, 3, new Random(1));
            Wait(masher, 0.05f);
            masher.Press(); masher.Press(); masher.Press();
            Assert.IsTrue(masher.Done, "early presses spend the zones");
            Assert.AreEqual(QteResult.Miss, masher.Result);
        }

        // --- Defensive QTE variety ---

        [Test]
        public void DodgeArrowMustMatchTheCue()
        {
            float impact = N.telegraph + N.windup;
            DefenseQte Q() => new DefenseQte(N, 1, cues: new[] { DodgeDir.Left });

            var right = Q(); Wait(right, impact); right.PressDirection(DodgeDir.Left);
            Assert.AreEqual(QteResult.Perfect, right.Result);
            Assert.IsFalse(right.WrongInput(0));
            var key = Q(); Wait(key, impact); key.Press();
            Assert.AreEqual(QteResult.Miss, key.Result, "the QTE key on an arrow cue is the wrong input");
            Assert.IsTrue(key.WrongInput(0), "the panel can say why it missed");
            var wrong = Q(); Wait(wrong, impact); wrong.PressDirection(DodgeDir.Up);
            Assert.AreEqual(QteResult.Miss, wrong.Result, "wrong arrow");
            Assert.IsTrue(wrong.WrongInput(0));
            var tap = new DefenseQte(N, 1); Wait(tap, impact); tap.PressDirection(DodgeDir.Left);
            Assert.AreEqual(QteResult.Miss, tap.Result, "an arrow on a key cue is the wrong input");
            Assert.IsTrue(tap.WrongInput(0));
            var late = Q(); Wait(late, impact + 1f);
            Assert.IsFalse(late.WrongInput(0), "a timeout is not a wrong input");
        }

        [Test]
        public void WardLeadInIsFreeReadingTime()
        {
            float lead = N.telegraph;
            WardQte W() => new WardQte("KLM", 3 * N.letterTime, 1, lead);

            var reader = W(); Wait(reader, lead * 0.9f);
            Assert.IsTrue(reader.InLead);
            Assert.AreEqual(0f, reader.T, 1e-5, "the clock waits while the player reads");
            Wait(reader, lead * 0.2f + reader.timeLimit * 0.9f);
            Assert.IsFalse(reader.Done, "the time limit counts from the end of the lead");
            Wait(reader, reader.timeLimit * 0.2f);
            Assert.AreEqual(QteResult.Miss, reader.Result);

            var eager = W(); Wait(eager, 0.1f); eager.Type('k');
            Assert.IsFalse(eager.InLead, "typing early starts the clock");
            Assert.AreEqual(1, eager.Index, "and the letter counts");

            var space = W(); space.Type(' ');
            Assert.IsTrue(space.InLead, "non-letters do not end the lead");

            Assert.AreEqual(N.telegraph, WardQte.For(N, 1, new System.Random(1)).LeadLeft, 1e-5, "real wards get the tier's telegraph");
        }

        [Test]
        public void FeintPushesTheRealHitBackAndPunishesBitingOnIt()
        {
            var plain = new DefenseQte(E, 2);
            var feinted = new DefenseQte(E, 2, feints: new[] { true, false });
            Assert.AreEqual(plain.impacts[0] + feinted.FeintTime, feinted.impacts[0], 1e-5);
            Assert.AreEqual(plain.impacts[1] + feinted.FeintTime, feinted.impacts[1], 1e-5, "later hits shift too");
            Assert.AreEqual(E.telegraph, feinted.FeintStart(0), 1e-5, "the fake shows up when the real cue would have");
            Assert.IsFalse(feinted.IsFeint(1));

            var bit = new DefenseQte(E, 1, feints: new[] { true });
            Wait(bit, bit.FeintStart(0) + E.windup * QteConfig.FeintStop);
            bit.Press();
            Assert.AreEqual(QteResult.Miss, bit.Result, "pressing on the fake spends the hit");

            var patient = new DefenseQte(E, 1, feints: new[] { true });
            Wait(patient, patient.impacts[0]);
            patient.Press();
            Assert.AreEqual(QteResult.Perfect, patient.Result);
        }

        [Test]
        public void WardIsTypedBeforeTheSpellLands()
        {
            WardQte W() => new WardQte("KLM", 3 * N.letterTime, 2);

            var fast = W(); Wait(fast, 0.3f); foreach (var c in "klm") fast.Type(c);
            Assert.AreEqual(QteResult.Perfect, fast.Result);
            CollectionAssert.AreEqual(new[] { QteResult.Perfect, QteResult.Perfect }, fast.Results, "one result per hit");

            var slow = W(); Wait(slow, slow.timeLimit * 0.8f); foreach (var c in "klm") slow.Type(c);
            Assert.AreEqual(QteResult.Good, slow.Result);

            var late = W(); Wait(late, late.timeLimit + 0.02f);
            Assert.IsTrue(late.Done);
            Assert.AreEqual(QteResult.Miss, late.Result);

            var fumbled = W(); foreach (var c in "xxx") fumbled.Type(c);
            Assert.AreEqual(QteResult.Miss, fumbled.Result, "three mistakes");
        }

        [Test]
        public void EnemyMovesRouteToDefenseStyles()
        {
            Qte D(string enemy, string move, int seed = 1)
            {
                var b = NewBattle(1, enemy);
                return QteFactory.Defense(BattleAction.Strike(b.Enemies[0], Move(enemy, move), b.Party[0]), QteAssist.Standard, null, new Random(seed));
            }

            var lance = D("zoroa_p1", "Soul Lance");
            Assert.IsInstanceOf<WardQte>(lance, "magic is warded");
            Assert.AreEqual(QteConfig.WardBaseLetters + B.runeLetters, ((WardQte)lance).sequence.Length, "boss wards are longer");
            Assert.IsInstanceOf<WardQte>(D("cinder_wisp", "Ember Bolt"));

            var cleave = (DefenseQte)D("hollow_squire", "Rusty Cleave");
            Assert.AreNotEqual(DodgeDir.None, cleave.cues[0], "a single physical hit carries an arrow");
            Assert.IsTrue(((DefenseQte)D("soul_reaper", "Accelerating Rend")).cues.All(c => c == DodgeDir.None), "multi-hit stays on the QTE key");
            Assert.IsTrue(((DefenseQte)D("zoroa_p1", "Final Reaping")).cues.All(c => c == DodgeDir.None));
            Assert.AreEqual(DodgeDir.None, ((DefenseQte)D("royal_ossuary_knight", "Graveward Charge")).cues[0], "Heavy stays on the QTE key");

            int Feints(string enemy, string move) => Enumerable.Range(0, 60).Count(s => ((DefenseQte)D(enemy, move, s)).IsFeint(0));
            Assert.AreEqual(0, Feints("hollow_squire", "Rusty Cleave"), "Normal enemies never feint");
            Assert.That(Feints("soul_reaper", "Harvesting Arc"), Is.InRange(5, 40), "Elite enemies feint sometimes");
        }
    }
}
