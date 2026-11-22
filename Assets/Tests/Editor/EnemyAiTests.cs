using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class EnemyAiTests
    {
        [Test]
        public void Stage0_RecordedBattleReplaysIdenticalAiTrace()
        {
            foreach (var (encId, level) in new[] { ("d1_e5", 4), ("d1_e8", 7), ("d1_e7", 9), ("d1_boss", 11) })
            {
                var rec = new BattleRecord();
                var orig = BalanceSimulation.Fight(encId, level, BalanceSimulation.Build.Recommended,
                    BalanceSimulation.Executions[1], seed: 42, gear: BalanceSimulation.Gear.D1WeaponArmor,
                    recordOut: rec, collectSignatures: true);

                Assert.Greater(orig.aiDecisions, 0, $"{encId}: expected AI decisions in trace");

                var replayed = BattleReplay.Replay(rec);
                var replaySigs = replayed.AiTrace.Entries.Select(e => e.Signature()).ToList();

                Assert.AreEqual(orig.decisionSignatures.Count, replaySigs.Count, $"{encId}: trace length matches replay");
                for (int i = 0; i < orig.decisionSignatures.Count; i++)
                    Assert.AreEqual(orig.decisionSignatures[i], replaySigs[i], $"{encId}: trace entry {i} matches replay");
            }
        }

        sealed class CountingRandom : Random
        {
            public int Calls;
            public CountingRandom(int seed) : base(seed) { }
            public override int Next() { Calls++; return base.Next(); }
            public override int Next(int maxValue) { Calls++; return base.Next(maxValue); }
            public override int Next(int minValue, int maxValue) { Calls++; return base.Next(minValue, maxValue); }
            public override double NextDouble() { Calls++; return base.NextDouble(); }
            public override void NextBytes(byte[] buffer) { Calls++; base.NextBytes(buffer); }
        }

        static List<Unit> DefaultParty(int level = 5)
        {
            var plan = new[]
            {
                ("human", "fighter", "guard"),
                ("dwarf", "cleric", "acolyte"),
                ("elf", "wizard", "sage"),
                ("halfling", "rogue", "charlatan"),
            };
            return plan.Select(p => BalanceSimulation.MakeHero(p, BalanceSimulation.Build.Recommended, level, BalanceSimulation.Gear.D1WeaponArmor)).ToList();
        }

        [Test]
        public void Stage1b_AllFifteenRosterEnemiesHaveAssignedRoleAndTier()
        {
            Assert.AreEqual(16, Dungeon1Roster.Enemies.Length, "15 roster enemies (Zoroa has P1 + P2)");
            var expected = new Dictionary<string, (AiRole role, AiTier tier)>
            {
                ["hollow_squire"] = (AiRole.Vanguard, AiTier.Normal),
                ["gravebound_archer"] = (AiRole.RangedOpportunist, AiTier.Normal),
                ["cinder_wisp"] = (AiRole.ElementalAttacker, AiTier.Normal),
                ["soulbound_hound"] = (AiRole.Disruptor, AiTier.Normal),
                ["possessed_royal_guard"] = (AiRole.Bruiser, AiTier.Normal),
                ["crypt_leech"] = (AiRole.Attrition, AiTier.Normal),
                ["forgotten_militia"] = (AiRole.ComboAttacker, AiTier.Normal),
                ["soul_reaper"] = (AiRole.Executioner, AiTier.Miniboss),
                ["royal_ossuary_knight"] = (AiRole.DefensivePunisher, AiTier.Miniboss),
                ["reliquary_confessor"] = (AiRole.Support, AiTier.Elite),
                ["graveglass_arcanist"] = (AiRole.ArcaneCaster, AiTier.Elite),
                ["crownless_duelist"] = (AiRole.Skirmisher, AiTier.Elite),
                ["tombstone_colossus"] = (AiRole.BreakBruiser, AiTier.Elite),
                ["mourning_bell_wraith"] = (AiRole.AreaController, AiTier.Elite),
                ["zoroa_p1"] = (AiRole.AdaptiveController, AiTier.Boss),
                ["zoroa_p2"] = (AiRole.AdaptiveController, AiTier.Boss),
            };
            foreach (var def in Dungeon1Roster.Enemies)
            {
                Assert.IsTrue(expected.TryGetValue(def.id, out var exp), $"unexpected enemy id {def.id}");
                Assert.AreEqual(exp.role, def.aiRole, $"{def.id} aiRole");
                Assert.AreEqual(exp.tier, def.aiTier, $"{def.id} aiTier");
            }
        }

        [Test]
        public void Stage1b_RosterAiPathNeverCallsRng()
        {
            foreach (var def in Dungeon1Roster.Enemies)
            {
                var rng = new CountingRandom(123);
                var b = new Battle(DefaultParty(), new[] { def }, new List<string>(), rng);
                rng.Calls = 0; // reset after Battle ctor (Wayfarer perk may roll at battle start)

                var enemy = b.Enemies[0];
                EnemyAI.Plan(b, enemy);
                var act = EnemyAI.Choose(b, enemy);

                Assert.IsNotNull(act, $"{def.id} produced a legal action");
                Assert.AreEqual(0, rng.Calls, $"{def.id}: roster AI path must never call Battle.Rng");
            }
        }

        [Test]
        public void Stage1b_RoleIdentityExceedsSixtyPercentAcrossTiers()
        {
            foreach (var (label, encId, level) in BalanceSimulation.AiTierBenchmarks)
            {
                var summary = BalanceSimulation.EvaluateAiTier(label, encId, level, BalanceSimulation.Executions[1], seeds: 30);
                Assert.GreaterOrEqual(summary.roleIdentityRate, 60.0, $"{label}: role identity rate must be >= 60%");
            }
        }

        [Test]
        public void Stage2_PlanningOrderIsSupportThenControllerThenOthersAndSpeedBuffInvariant()
        {
            // Spawn order puts Bruiser (0), Controller (1), Support (2), Vanguard (3) — Support must plan first, then Controller, then others by slot.
            var defs = new[]
            {
                Database.Enemies["possessed_royal_guard"],
                Database.Enemies["mourning_bell_wraith"],
                Database.Enemies["reliquary_confessor"],
                Database.Enemies["hollow_squire"],
            };

            var bNormal = new Battle(DefaultParty(), defs, new List<string>(), new Random(77));
            var bBuffed = new Battle(DefaultParty(), defs, new List<string>(), new Random(77));

            // Apply a massive Slow on Confessor and speed disparity in bBuffed — initiative changes, planning order MUST NOT.
            bBuffed.Enemies[2].AddStatus(StatusId.Slow, 3, 0.5f, bBuffed.Party[0]);
            bBuffed.Enemies[0].unit.stats = new Stats(bBuffed.Enemies[0].Stats.maxHp, bBuffed.Enemies[0].Stats.maxMp,
                bBuffed.Enemies[0].Stats.atk, bBuffed.Enemies[0].Stats.def, 99, bBuffed.Enemies[0].Stats.mag);

            bNormal.NextTurn(); // triggers StartRound()
            bBuffed.NextTurn(); // triggers StartRound()

            var orderNormal = bNormal.AiTrace.Entries.Select(e => e.enemyId).ToList();
            var orderBuffed = bBuffed.AiTrace.Entries.Select(e => e.enemyId).ToList();

            CollectionAssert.AreEqual(
                new[] { "reliquary_confessor", "mourning_bell_wraith", "possessed_royal_guard", "hollow_squire" },
                orderNormal,
                "Planning order must be Support -> Controller -> others by slot");
            CollectionAssert.AreEqual(orderNormal, orderBuffed, "Planning order must be invariant to Speed changes");

            Assert.IsTrue(bNormal.AiTrace.Matches(bBuffed.AiTrace, out var diff),
                $"AI decisions must be identical with and without Speed changes: {diff}");
        }

        [Test]
        public void Stage2_FocusCapNormalPreventsThreeNormalsStackingOnOneHero()
        {
            var b = new Battle(DefaultParty(), new[]
            {
                Database.Enemies["possessed_royal_guard"],
                Database.Enemies["hollow_squire"],
                Database.Enemies["hollow_squire"],
            }, new List<string>(), new Random(11));

            b.NextTurn(); // StartRound plans all 3 Normal enemies

            var targetCounts = b.Enemies.Where(e => e.Intent?.target != null)
                .GroupBy(e => e.Intent.target)
                .Select(g => g.Count())
                .ToList();

            Assert.Greater(targetCounts.Count, 1, "3 Normal enemies must not all stack on 1 hero when FocusCapNormal = 2");
            Assert.LessOrEqual(targetCounts.Max(), EncounterBlackboard.FocusCapNormal, "No hero exceeds FocusCapNormal");
        }

        [Test]
        public void Stage2_LethalReservationSkipsDoomedHeroIncludingLockedLowestHp()
        {
            // Two Elites (locked LowestHp) vs party where hero 0 has 1 HP (lethal-covered by Elite A), hero 1 has 40 HP, hero 2/3 have full HP.
            var party = DefaultParty(level: 5);
            party[0].hp = 1;
            party[1].hp = 40;
            var b = new Battle(party, new[]
            {
                Database.Enemies["crownless_duelist"],
                Database.Enemies["graveglass_arcanist"],
            }, new List<string>(), new Random(21));

            b.NextTurn(); // StartRound plans Duelist (slot 0) then Arcanist (slot 1)

            Assert.AreEqual(b.Party[0], b.Enemies[0].Intent.target, "First Elite locks onto lowest-HP hero (slot 0)");
            Assert.IsTrue(b.Blackboard.IsLethalReserved(b.Party[0]), "Hero 0 must be reserved as lethal after first Elite plans");
            Assert.AreEqual(b.Party[1], b.Enemies[1].Intent.target,
                "Second Elite (locked LowestHp) skips lethal-reserved hero 0 and picks next lowest-HP hero (slot 1) by the same rule");
        }

        [Test]
        public void Stage2_StunAndHeavyNeverTargetSameHeroInSameRound()
        {
            // Synthetic stun-applying enemy + Royal Ossuary Knight (Heavy) vs 2 living heroes
            var stunner = new EnemyDef
            {
                id = "test_stunner", name = "Stunner", shortName = "Stunner", tag = "STN",
                tier = Tier.Normal, ai = AiKind.Priority, aiRole = AiRole.AreaController, aiTier = AiTier.Normal,
                stats = new Stats(150, 50, 15, 10, 10, 10),
                attacks = new[]
                {
                    new EnemyAttack { name = "Concuss", element = Element.Physical, hitDamage = new[] { 10 }, power = 1f, hits = 1, aiPriority = 2,
                        apply = new[] { new StatusApply { id = StatusId.Stun, chance = 1f, turns = 1 } } },
                },
            };
            var heavy = new EnemyDef
            {
                id = "test_heavy", name = "Heavy", shortName = "Heavy", tag = "HVY",
                tier = Tier.Elite, ai = AiKind.Priority, aiRole = AiRole.DefensivePunisher, aiTier = AiTier.Elite,
                targetRule = TargetRule.LowestHp,
                stats = new Stats(200, 50, 20, 10, 10, 10),
                attacks = new[]
                {
                    new EnemyAttack { name = "Big Slam", element = Element.Physical, hitDamage = new[] { 20 }, power = 1f, hits = 1, isHeavy = true, aiPriority = 2 },
                },
            };

            var party = DefaultParty().Take(2).ToList();
            party[0].hp = 50; // Lowest HP so Heavy would normally want slot 0
            party[1].hp = 80;

            var b = new Battle(party, new[] { stunner, heavy }, new List<string>(), new Random(3));
            b.NextTurn(); // Controller (Stunner) plans first -> targets slot 0 (or 1); Heavy plans second -> must pick the other hero!

            Assert.IsNotNull(b.Enemies[0].Intent);
            Assert.IsNotNull(b.Enemies[1].Intent);
            Assert.AreNotEqual(b.Enemies[0].Intent.target, b.Enemies[1].Intent.target,
                "Stun and Heavy must never target the same hero in the same round");
        }

        [Test]
        public void Stage2_PublishedIntentIsNeverRescoredOnExecution()
        {
            var b = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(9));
            var c = b.NextTurn();
            // Advance until it is Hollow Squire's turn
            while (c != null && !c.IsEnemy)
            {
                b.Perform(BattleAction.Guard(c));
                c = b.NextTurn();
            }
            Assert.IsNotNull(c);
            var published = c.Intent;
            Assert.IsNotNull(published);

            // Change hero HP dramatically before execution to tempt a re-score — Decide MUST execute the published intent without re-scoring!
            var otherHero = b.Party.First(p => p != published.target);
            otherHero.unit.hp = 1;
            int traceCountBefore = b.AiTrace.Entries.Count;

            var act = EnemyAI.Choose(b, c);
            Assert.AreEqual(published.move, act.attack, "Published move must execute unchanged");
            Assert.AreEqual(published.target, act.target, "Published target must execute unchanged while alive");
            Assert.AreEqual(traceCountBefore, b.AiTrace.Entries.Count, "Decide must not re-score a legal published intent");
        }

        [Test]
        public void Stage3_ConfidenceMathMatchesArchitectureAndOneActionNeverRecognizes()
        {
            var b = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var hero = b.Party[0];
            var prof = b.Blackboard.Memory.GetOrCreateProfile(hero);
            var normalProfile = AiProfile.Build(AiRole.Vanguard, AiTier.Normal);
            var bossProfile = AiProfile.Build(AiRole.AdaptiveController, AiTier.Boss);

            // 1 observation at round 1: s = 1, conf = 1 / (1 + 2) = 1/3 = 0.3333 -> never recognizes even for Boss (0.45)
            prof.AddObservation(1, BehaviorCategory.Defensive, 1f);
            float c1 = prof.GetConfidence(BehaviorCategory.Defensive, 1, normalProfile.memoryDecay);
            Assert.AreEqual(1f / 3f, c1, 0.001f);
            Assert.IsFalse(prof.IsRecognized(BehaviorCategory.Defensive, 1, normalProfile), "1 action never recognizes (Normal)");
            Assert.IsFalse(prof.IsRecognized(BehaviorCategory.Defensive, 1, bossProfile), "1 action never recognizes (Boss)");

            // 3 consecutive rounds of Guard: s = 1 + 0.7 + 0.49 = 2.19, conf = 2.19 / 4.19 = 0.5227 -> recognizes!
            prof.AddObservation(2, BehaviorCategory.Defensive, 1f);
            prof.AddObservation(3, BehaviorCategory.Defensive, 1f);
            float c3 = prof.GetConfidence(BehaviorCategory.Defensive, 3, normalProfile.memoryDecay);
            Assert.AreEqual(2.19f / 4.19f, c3, 0.001f);
            Assert.IsTrue(prof.IsRecognized(BehaviorCategory.Defensive, 3, normalProfile), "3 recent consistent actions recognize");

            // Decay: 3 rounds after stopping (round 6), Normal influence fades below 0.5; Boss fades ~5 rounds after stopping (round 8).
            Assert.IsFalse(prof.IsRecognized(BehaviorCategory.Defensive, 6, normalProfile), "Normal influence fades ~3 rounds after stopping");
            Assert.IsFalse(prof.IsRecognized(BehaviorCategory.Defensive, 8, bossProfile), "Boss influence fades ~5 rounds after stopping");

            // Contradiction: 2 rounds of attacking (Aggressive contradicts Defensive) drops Defensive below 0.5.
            prof.AddObservation(4, BehaviorCategory.Aggressive, 1f);
            prof.AddObservation(5, BehaviorCategory.Aggressive, 1f);
            Assert.Less(prof.GetConfidence(BehaviorCategory.Defensive, 5, normalProfile.memoryDecay), 0.5f,
                "2 rounds of attacking after guarding drops Defensive below 0.5");
        }

        [Test]
        public void Stage3_MemoryIsEmptyAtBattleStartAndResetsBetweenEncounters()
        {
            var b1 = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(2));
            Assert.IsTrue(b1.Blackboard.Memory.IsEmpty, "Memory must be empty at the start of battle 1");

            // Feed observations in battle 1
            b1.Blackboard.Memory.ObserveGuard(b1.Party[0], 1);
            b1.Blackboard.Memory.ObserveGuard(b1.Party[0], 2);
            b1.Blackboard.Memory.ObserveGuard(b1.Party[0], 3);
            Assert.IsFalse(b1.Blackboard.Memory.IsEmpty);

            // Battle 2 starts completely fresh
            var b2 = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(2));
            Assert.IsTrue(b2.Blackboard.Memory.IsEmpty, "New encounter must never inherit learned player profiles");
        }

        [Test]
        public void Stage3_SharedMemoryReachesNewSummonAndSurvivesZoroaPhaseChange()
        {
            // Construct equal-stat heroes so SupportDependent recognition cleanly decides the target.
            var party = Enumerable.Range(0, 2).Select(i =>
            {
                var u = BalanceSimulation.MakeHero(("human", "fighter", "guard"), BalanceSimulation.Build.Recommended, 5, BalanceSimulation.Gear.D1WeaponArmor);
                u.hp = u.stats.maxHp;
                return u;
            }).ToList();

            var b = new Battle(party, new[] { Database.Enemies["zoroa_p1"] }, new List<string>(), new Random(15));
            var zoroa = b.Enemies[0];
            var healerHero = b.Party[1]; // slot 1 (slot 0 would normally win slot tie-break)

            // Observe healerHero acting as SupportDependent for 3 rounds BEFORE Zoroa summons or transitions phase.
            var prof = b.Blackboard.Memory.GetOrCreateProfile(healerHero);
            prof.AddObservation(1, BehaviorCategory.SupportDependent, 1f);
            prof.AddObservation(2, BehaviorCategory.SupportDependent, 1f);
            prof.AddObservation(3, BehaviorCategory.SupportDependent, 1f);

            // Advance battle round to 3 via NextTurn / StartRound
            b.NextTurn(); // Round 1
            // Summon a Soulbound Hound (Disruptor: counts SupportDependent double!) AFTER the behavior was observed
            b.Summon(zoroa, Database.Enemies["soulbound_hound"]);
            var summon = b.Enemies[1];
            Assert.IsTrue(summon.IsSummon);

            // Summon immediately reads shared encounter memory and targets slot 1 (healerHero) instead of slot 0!
            var intent = PriorityBrain.Select(b, summon, out var trace);
            Assert.IsNotNull(intent);
            Assert.AreEqual(healerHero, intent.target, "Newly summoned enemy must react to behavior observed before it joined");
            Assert.IsTrue(trace.adaptationChangedDecision, "Adaptation should be recorded as having changed the summon's decision");

            // Transition Zoroa from Phase 1 to Phase 2 -> shared encounter memory survives intact!
            int p1Hp = zoroa.unit.hp;
            b.Damage(zoroa, p1Hp);
            Assert.AreEqual("zoroa_p2", zoroa.enemy.id, "Zoroa transitioned to Phase 2");
            var zProfile = AiProfile.For(zoroa.enemy);
            Assert.IsTrue(b.Blackboard.Memory.GetProfile(healerHero).IsRecognized(BehaviorCategory.SupportDependent, 3, zProfile),
                "Shared encounter memory must survive Zoroa's phase change");
        }

        [Test]
        public void Stage3_AdaptationReadLogsExactlyOncePerEncounter()
        {
            var party = Enumerable.Range(0, 2).Select(_ =>
                BalanceSimulation.MakeHero(("human", "fighter", "guard"), BalanceSimulation.Build.Recommended, 5, BalanceSimulation.Gear.D1WeaponArmor)
            ).ToList();

            var b = new Battle(party, new[] { Database.Enemies["hollow_squire"], Database.Enemies["hollow_squire"] }, new List<string>(), new Random(19));
            var healerHero = b.Party[1];

            // Pre-populate SupportDependent recognition on hero 1 before planning
            var prof = b.Blackboard.Memory.GetOrCreateProfile(healerHero);
            prof.AddObservation(1, BehaviorCategory.SupportDependent, 1f);
            prof.AddObservation(1, BehaviorCategory.SupportDependent, 1f);
            prof.AddObservation(1, BehaviorCategory.SupportDependent, 1f);

            b.NextTurn(); // Round 1 StartRound: BOTH Hollow Squires adapt to target healerHero

            string expectedRead = $"They mark the healer, {healerHero.name}.";
            int readCount = b.Log.Count(line => line == expectedRead);
            Assert.AreEqual(1, readCount, "Each adaptation read must log exactly once per encounter even when multiple enemies react");

            // Plan again in a second call — must still not duplicate the read!
            EnemyAI.Plan(b, b.Enemies[0]);
            Assert.AreEqual(1, b.Log.Count(line => line == expectedRead), "Read must not log a second time later in the encounter");
        }

        [Test]
        public void Stage3_BreakSpecialistGatesChargeWhenShieldBelowChargeShieldGate()
        {
            var b = new Battle(DefaultParty(level: 6), new[] { Database.Enemies["royal_ossuary_knight"] }, new List<string>(), new Random(5));
            var knight = b.Enemies[0];
            // Put Graveward Charge off initialCooldown so it is legal
            knight.cooldowns.Clear();

            // Recognize BreakSpecialist on hero 0
            var prof = b.Blackboard.Memory.GetOrCreateProfile(b.Party[0]);
            prof.AddObservation(1, BehaviorCategory.BreakSpecialist, 1f);
            prof.AddObservation(1, BehaviorCategory.BreakSpecialist, 1f);
            prof.AddObservation(1, BehaviorCategory.BreakSpecialist, 1f);

            // With Break gauge chipped below ChargeShieldGate (70%), Knight holds back Graveward Charge and uses Ossuary Slam
            knight.CurrentBreakGauge = (int)(knight.MaxBreakGauge * 0.50f);
            var lowShieldIntent = PriorityBrain.Select(b, knight);
            Assert.AreEqual("Ossuary Slam", lowShieldIntent.move.name,
                "Break Specialist adaptation delays starting Graveward Charge when Break gauge < 70%");

            // With Break gauge full (>= 70%), Knight starts Graveward Charge normally
            knight.CurrentBreakGauge = knight.MaxBreakGauge;
            var fullShieldIntent = PriorityBrain.Select(b, knight);
            Assert.AreEqual("Graveward Charge", fullShieldIntent.move.name,
                "Knight starts Graveward Charge when Break gauge >= ChargeShieldGate (70%)");
        }

        [Test]
        public void Stage3_ObservableInfoOnlyUnseenImmunityIsNeverPeekedUntilHit()
        {
            var immuneHeroDef = new CharacterDef
            {
                id = "immune_hero", name = "Aegis", role = "Knight", raceId = "human", classId = "fighter", backgroundId = "guard",
                affinities = new Affinities { immune = new[] { Element.Fire } },
            };
            var normalHeroDef = new CharacterDef
            {
                id = "normal_hero", name = "Bram", role = "Knight", raceId = "human", classId = "fighter", backgroundId = "guard",
                affinities = Affinities.None,
            };
            var b = new Battle(new[] { new Unit(immuneHeroDef), new Unit(normalHeroDef) },
                new[] { Database.Enemies["cinder_wisp"] }, new List<string>(), new Random(7));
            var wisp = b.Enemies[0];

            // Before any Fire hit has been observed, Cinder Wisp MUST NOT peek at Aegis's hidden Fire immunity:
            // slot 0 (Aegis) wins deterministic tie-break over identical slot 1 (Bram).
            var before = PriorityBrain.Select(b, wisp);
            Assert.AreEqual(b.Party[0], before.target, "AI must not peek at unseen affinities before they are observed");

            // Execute Ember Bolt on Aegis so the Fire Immunity is OBSERVED via combat event
            b.NextTurn();
            while (b.Active != wisp) { b.Perform(BattleAction.Guard(b.Active)); b.NextTurn(); }
            b.Perform(BattleAction.Strike(wisp, wisp.enemy.attacks[0], b.Party[0]));

            Assert.AreEqual(Effectiveness.Immune, b.Blackboard.Memory.GetSeenEffectiveness(b.Party[0], Element.Fire),
                "Fire immunity is now observed in encounter memory");

            // Now Cinder Wisp switches away from immune Aegis to Bram!
            var after = PriorityBrain.Select(b, wisp);
            Assert.AreEqual(b.Party[1], after.target, "After observing Fire immunity, AI adapts and targets Bram");
        }
    }
}
