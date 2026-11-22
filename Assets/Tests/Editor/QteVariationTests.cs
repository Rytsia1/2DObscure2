using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class QteVariationTests
    {
        static QteProfile N => QteConfig.Profiles[Tier.Normal];
        static QteProfile E => QteConfig.Profiles[Tier.Elite];
        static QteProfile B => QteConfig.Profiles[Tier.Boss];

        static void Wait(Qte q, float seconds)
        {
            const float step = 0.005f;
            for (float t = step; t <= seconds + 1e-5f; t += step) q.Tick(step);
        }

        [Test]
        public void Stage2_ParityWithoutVariantsMatchesEstablishedDefenseBehaviorForSameSeed()
        {
            var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var b = new Battle(party, new[] { Database.Enemies["forgotten_militia"] }, new List<string>(), new Random(1));
            var militia = b.Enemies[0];
            var move = militia.enemy.attacks[0]; // Two-Step Slash (unauthored)

            var q1 = QteFactory.Defense(BattleAction.Strike(militia, move, b.Party[0]), QteAssist.Standard, null, new Random(77)) as DefenseQte;
            var q2 = QteFactory.Defense(BattleAction.Strike(militia, move, b.Party[0]), QteAssist.Standard, null, new Random(77)) as DefenseQte;
            Assert.IsNotNull(q1);
            Assert.IsNotNull(q2);
            CollectionAssert.AreEqual(q1.cues, q2.cues);
            CollectionAssert.AreEqual(q1.impacts, q2.impacts);
            Assert.IsTrue(q1.variants.All(v => v == QteVariant.None));
        }

        [Test]
        public void Stage2_GuardCueGradesPerfectGoodPoorAndRejectsWrongInput()
        {
            var qPerf = new DefenseQte(N, 1, variants: new[] { QteVariant.GuardCue });
            Wait(qPerf, qPerf.impacts[0]);
            qPerf.PressGuard();
            Assert.AreEqual(QteResult.Perfect, qPerf.Result);

            var qGood = new DefenseQte(N, 1, variants: new[] { QteVariant.GuardCue });
            Wait(qGood, qGood.impacts[0] + 0.08f);
            qGood.PressGuard();
            Assert.AreEqual(QteResult.Good, qGood.Result);

            var qWrong = new DefenseQte(N, 1, variants: new[] { QteVariant.GuardCue });
            Wait(qWrong, qWrong.impacts[0]);
            qWrong.Press(); // QTE key instead of Guard
            Assert.IsTrue(qWrong.WrongInput(0));
            Assert.AreEqual(QteResult.Miss, qWrong.Result);
        }

        [Test]
        public void Stage2_SlipGradesReleaseAtImpactAndFailsEarlyOrNeverReleased()
        {
            // Perfect release at impact
            var qPerf = new DefenseQte(N, 1, variants: new[] { QteVariant.Slip });
            Wait(qPerf, qPerf.SpawnTime(0) + 0.1f);
            qPerf.PressGuard();
            Assert.IsTrue(qPerf.IsSlipping(0));
            Wait(qPerf, qPerf.impacts[0] - qPerf.T);
            qPerf.ReleaseGuard();
            Assert.AreEqual(QteResult.Perfect, qPerf.Result);

            // Released too early -> Miss
            var qEarly = new DefenseQte(N, 1, variants: new[] { QteVariant.Slip });
            Wait(qEarly, qEarly.SpawnTime(0) + 0.1f);
            qEarly.PressGuard();
            Wait(qEarly, 0.1f);
            qEarly.ReleaseGuard();
            Assert.IsTrue(qEarly.LetGo(0));
            Assert.AreEqual(QteResult.Miss, qEarly.Result);

            // Never released -> Miss
            var qHeld = new DefenseQte(N, 1, variants: new[] { QteVariant.Slip });
            Wait(qHeld, qHeld.SpawnTime(0) + 0.1f);
            qHeld.PressGuard();
            Wait(qHeld, qHeld.impacts[0] - qHeld.T + N.good + 0.05f);
            Assert.IsTrue(qHeld.Done);
            Assert.AreEqual(QteResult.Miss, qHeld.Result);
        }

        [Test]
        public void Stage2_FlurryTakesWorseOfTwoPressesAndFailsSinglePress()
        {
            // Both Perfect -> Perfect
            var qPerf = new DefenseQte(N, 1, variants: new[] { QteVariant.Flurry });
            Wait(qPerf, qPerf.impacts[0]);
            qPerf.PressGuard();
            Wait(qPerf, QteConfig.FlurryGap);
            qPerf.PressGuard();
            Assert.AreEqual(QteResult.Perfect, qPerf.Result);

            // First Perfect, second Good -> Good
            var qMixed = new DefenseQte(N, 1, variants: new[] { QteVariant.Flurry });
            Wait(qMixed, qMixed.impacts[0]);
            qMixed.PressGuard();
            Wait(qMixed, QteConfig.FlurryGap + 0.08f);
            qMixed.PressGuard();
            Assert.AreEqual(QteResult.Good, qMixed.Result);

            // Only 1 press -> Miss
            var qOne = new DefenseQte(N, 1, variants: new[] { QteVariant.Flurry });
            Wait(qOne, qOne.impacts[0]);
            qOne.PressGuard();
            Wait(qOne, QteConfig.FlurryGap + N.good + 0.05f);
            Assert.IsTrue(qOne.Done);
            Assert.AreEqual(QteResult.Miss, qOne.Result);
        }

        [Test]
        public void Stage2_RollRequiresArrowAndGuardWithinChordTolerance()
        {
            // Both within ChordTolerance at impact -> Perfect
            var qPerf = new DefenseQte(E, 1, cues: new[] { DodgeDir.Left }, variants: new[] { QteVariant.Roll });
            Wait(qPerf, qPerf.impacts[0] - 0.02f);
            qPerf.PressDirection(DodgeDir.Left);
            Wait(qPerf, 0.02f);
            qPerf.PressGuard();
            Assert.AreEqual(QteResult.Perfect, qPerf.Result);

            // Outside ChordTolerance -> Miss
            var qLate = new DefenseQte(E, 1, cues: new[] { DodgeDir.Left }, variants: new[] { QteVariant.Roll });
            Wait(qLate, qLate.impacts[0] - 0.10f);
            qLate.PressDirection(DodgeDir.Left);
            Wait(qLate, QteConfig.ChordTolerance + 0.02f);
            qLate.PressGuard();
            Assert.AreEqual(QteResult.Miss, qLate.Result);

            // Only arrow, no Guard -> Miss
            var qOnlyArrow = new DefenseQte(E, 1, cues: new[] { DodgeDir.Right }, variants: new[] { QteVariant.Roll });
            Wait(qOnlyArrow, qOnlyArrow.impacts[0]);
            qOnlyArrow.PressDirection(DodgeDir.Right);
            Wait(qOnlyArrow, E.good + 0.05f);
            Assert.IsTrue(qOnlyArrow.Done);
            Assert.AreEqual(QteResult.Miss, qOnlyArrow.Result);
        }

        [Test]
        public void Stage2_SingleKeyModeAcceptsQteKeyForAllGuardVariants()
        {
            var qGuard = new DefenseQte(N, 1, variants: new[] { QteVariant.GuardCue }, singleKeyMode: true);
            Wait(qGuard, qGuard.impacts[0]);
            qGuard.Press();
            Assert.AreEqual(QteResult.Perfect, qGuard.Result);

            var qSlip = new DefenseQte(N, 1, variants: new[] { QteVariant.Slip }, singleKeyMode: true);
            Wait(qSlip, qSlip.SpawnTime(0) + 0.1f);
            qSlip.Press();
            Wait(qSlip, qSlip.impacts[0] - qSlip.T);
            qSlip.Release();
            Assert.AreEqual(QteResult.Perfect, qSlip.Result);

            var qRoll = new DefenseQte(E, 1, cues: new[] { DodgeDir.Up }, variants: new[] { QteVariant.Roll }, singleKeyMode: true);
            Wait(qRoll, qRoll.impacts[0]);
            qRoll.PressDirection(DodgeDir.Up);
            qRoll.Press();
            Assert.AreEqual(QteResult.Perfect, qRoll.Result);
        }

        [Test]
        public void Stage2_ValidatorEnforcesComplexityBudgetsAndRhythmFloors()
        {
            var normal = new EnemyDef { id = "n", tier = Tier.Normal };
            var badFlurry = new EnemyAttack
            {
                name = "Bad", hits = 1,
                motif = new QteMotif { hits = new[] { new HitCue(QteVariant.Flurry) } },
            };
            Assert.IsFalse(QteMotifValidator.Validate(normal, badFlurry, out _), "Flurry disallowed on Normal");

            var badSyncopated = new EnemyAttack
            {
                name = "BadSync", hits = 4, intervals = new[] { 0.42f, 0.28f, 0.42f },
                motif = new QteMotif { hits = Enumerable.Repeat(new HitCue(QteVariant.None), 4).ToArray() },
            };
            var elite = new EnemyDef { id = "el", tier = Tier.Elite };
            Assert.IsFalse(QteMotifValidator.Validate(elite, badSyncopated, out _), "Syncopated allowed on Boss signatures only");
        }

        [Test]
        public void Stage3_AllDungeon1AuthoredMotifsPassValidatorAndPrologueRemainsUntouched()
        {
            foreach (var e in Dungeon1Roster.Enemies.Concat(Dungeon1Roster.EnhancedVariants))
            {
                foreach (var m in e.attacks)
                {
                    if (m.motif == null) continue;
                    bool ok = QteMotifValidator.Validate(e, m, out var errs);
                    Assert.IsTrue(ok, $"{e.id}.{m.name} motif invalid:\n" + string.Join("\n", errs));
                }
            }
            foreach (var pe in PrologueRoster.Enemies)
                foreach (var pm in pe.attacks)
                    Assert.IsNull(pm.motif, $"PrologueRoster {pe.id}.{pm.name} must remain untouched");
        }

        [Test]
        public void Stage3_MotifDeterminismGivesIdenticalQteWithoutRemixAndUnauthoredMovesKeepRandomPath()
        {
            var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var b = new Battle(party, new[] { Database.Enemies["hollow_squire"], Database.Enemies["forgotten_militia"] },
                new List<string>(), new Random(1));
            var squire = b.Enemies[0]; // Rusty Cleave has authored motif: Dodge Right (no remix on Normal)
            var cleave = squire.enemy.attacks[0];

            var q1 = QteFactory.Defense(BattleAction.Strike(squire, cleave, b.Party[0]), QteAssist.Standard, null, new Random(1)) as DefenseQte;
            var q2 = QteFactory.Defense(BattleAction.Strike(squire, cleave, b.Party[0]), QteAssist.Standard, null, new Random(999)) as DefenseQte;
            Assert.IsNotNull(q1);
            Assert.IsNotNull(q2);
            CollectionAssert.AreEqual(new[] { DodgeDir.Right }, q1.cues);
            CollectionAssert.AreEqual(q1.cues, q2.cues, "Authored motif without remix is deterministic across seeds");
            Assert.IsFalse(q1.isRemix, "Normal tier never remixes");

            // Unauthored move (Forgotten Militia Two-Step Slash) still uses random jitter across seeds
            var militia = b.Enemies[1];
            var slash = militia.enemy.attacks[0];
            Assert.IsNull(slash.motif);
            var m1 = QteFactory.Defense(BattleAction.Strike(militia, slash, b.Party[0]), QteAssist.Standard, null, new Random(1)) as DefenseQte;
            var m2 = QteFactory.Defense(BattleAction.Strike(militia, slash, b.Party[0]), QteAssist.Standard, null, new Random(2)) as DefenseQte;
            Assert.AreNotEqual(m1.impacts[1] - m1.impacts[0], m2.impacts[1] - m2.impacts[0], "Unauthored move keeps random jitter");
        }

        [Test]
        public void Stage4_WeaponStrikeSignaturesSetShapeOnlyAndKeepUnchangedFamiliesIdentical()
        {
            var hero = new Unit(Database.Characters[Database.Party[0]]);
            var b = new Battle(new[] { hero }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var c = b.Party[0];

            // Sword -> Linear, 1 pass
            var swordItem = Database.Equipment.Values.First(e => e.weaponType == WeaponType.Sword);
            hero.TryEquip(EquipmentSlot.MainHand, new EquipmentInstance(swordItem), out _);
            var swordQ = QteFactory.Offense(BattleAction.Attack(c, b.Enemies[0]), Tier.Normal, QteAssist.Standard, new Random(5)) as TimingQte;
            Assert.IsNotNull(swordQ);
            Assert.AreEqual(SweepCurve.Linear, swordQ.curve);
            Assert.AreEqual(1, swordQ.passes);

            // Dagger -> EaseIn, 1 pass, identical windows
            Assert.AreEqual((SweepCurve.EaseIn, 1, false), QteFactory.StrikeSignatureFor(WeaponType.Dagger));
            Assert.AreEqual((SweepCurve.Linear, 2, false), QteFactory.StrikeSignatureFor(WeaponType.DualDaggers));
            Assert.AreEqual((SweepCurve.Linear, 1, true), QteFactory.StrikeSignatureFor(WeaponType.Spear));

            var dagEq = Database.Equipment.Values.First(e => e.weaponType == WeaponType.Dagger);
            var dagUnit = Database.Characters.Values.Select(ch => new Unit(ch))
                .FirstOrDefault(u => u.TryEquip(EquipmentSlot.MainHand, new EquipmentInstance(dagEq), out _));
            if (dagUnit != null)
            {
                var db = new Battle(new[] { dagUnit }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
                var dagQ = QteFactory.Offense(BattleAction.Attack(db.Party[0], db.Enemies[0]), Tier.Normal, QteAssist.Standard, new Random(5)) as TimingQte;
                Assert.IsNotNull(dagQ);
                Assert.AreEqual(SweepCurve.EaseIn, dagQ.curve);
                Assert.AreEqual(swordQ.EffectivePerfect, dagQ.EffectivePerfect, 1e-6f, "Windows never change");
                Assert.AreEqual(swordQ.EffectiveGood, dagQ.EffectiveGood, 1e-6f, "Windows never change");
            }

            // DualDaggers -> Double-pass, either pass hits Perfect
            var dpQ = new TimingQte(N, 0.7f, curve: SweepCurve.Linear, passes: 2);
            Wait(dpQ, dpQ.IdealTime(secondPass: true));
            dpQ.Press();
            Assert.AreEqual(QteResult.Perfect, dpQ.Result, "Second pass of Double-pass sweep awards Perfect");

            // Spear -> Thrust ChargeQte
            var spearEq = Database.Equipment.Values.First(e => e.weaponType == WeaponType.Spear);
            var spearUnit = Database.Characters.Values.Select(ch => new Unit(ch))
                .FirstOrDefault(u => u.TryEquip(EquipmentSlot.MainHand, new EquipmentInstance(spearEq), out _));
            if (spearUnit != null)
            {
                var sb = new Battle(new[] { spearUnit }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
                var spearQ = QteFactory.Offense(BattleAction.Attack(sb.Party[0], sb.Enemies[0]), Tier.Normal, QteAssist.Standard, new Random(5)) as ChargeQte;
                Assert.IsNotNull(spearQ);
                Assert.IsTrue(spearQ.isThrust);
            }

            // Echo ComboQte mirrors zone 2 from zone 1
            var echoQ = new ComboQte(N, 2, new Random(3), echoZones: true);
            Assert.AreEqual(echoQ.duration - echoQ.zoneTimes[0], echoQ.zoneTimes[1], 1e-4f);
        }

        [Test]
        public void Stage5_EnhancedAlwaysRemixesAndNormalNeverRemixes()
        {
            var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var b = new Battle(party, new[]
            {
                Database.Enemies["hollow_squire"],
                Database.Enemies["hollow_squire_enhanced"],
            }, new List<string>(), new Random(1));

            var norm = b.Enemies[0];
            var enh = b.Enemies[1];
            var enhAlt = enh.enemy.attacks.First(a => !string.IsNullOrEmpty(a.patternId));

            var qNorm = QteFactory.Defense(BattleAction.Strike(norm, norm.enemy.attacks[0], b.Party[0]), QteAssist.Standard, null, new Random(1)) as DefenseQte;
            var qEnh = QteFactory.Defense(BattleAction.Strike(enh, enhAlt, b.Party[0]), QteAssist.Standard, null, new Random(1)) as DefenseQte;

            Assert.IsFalse(qNorm.isRemix, "Normal never remixes");
            Assert.IsTrue(qEnh.isRemix, "Enhanced always uses its remix on its alternative pattern");
            Assert.AreEqual(QteVariant.GuardCue, qEnh.VariantAt(0), "Upgrade remix converts Tap to GuardCue");
        }

        [Test]
        public void Stage6_StatusDistortionsNeverChangeJudgmentAndClearZoneIsUndistorted()
        {
            // Identical error produces identical judgment with and without Fear distortion
            var clean = new DefenseQte(N, 1);
            var distorted = new DefenseQte(N, 1) { Distortion = QteDistortion.Fear };
            Wait(clean, clean.impacts[0] + 0.02f);
            Wait(distorted, distorted.impacts[0] + 0.02f);
            clean.Press();
            distorted.Press();
            Assert.AreEqual(clean.Result, distorted.Result, "Distortion must never change QTE judgment");
            Assert.AreEqual(clean.Category, distorted.Category);

            // ClearZone check: within 150 ms of impact is always undistorted
            Assert.IsTrue(QteConfig.IsInClearZone(0.10f));
            Assert.IsFalse(QteConfig.IsInClearZone(0.25f));

            // Priority order & toggle
            var hero = new Combatant(new Unit(Database.Characters[Database.Party[0]]), Side.Party, 0, "F", "F");
            hero.AddStatus(StatusId.Slow, 2, 0.2f, hero);
            hero.AddStatus(StatusId.Fear, 2, 0.2f, hero);
            Assert.AreEqual(QteDistortion.Fear, QteConfig.ActiveDistortion(hero, reduceDistortions: false), "Fear takes priority over Slow");
            Assert.AreEqual(QteDistortion.None, QteConfig.ActiveDistortion(hero, reduceDistortions: true), "Reduce QTE distortions toggle disables all distortions");
        }
    }
}
