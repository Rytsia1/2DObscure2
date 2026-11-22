using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    // Verification suite for Felna_Enhanced_Common_Variant_Spec_v1_1.md, Felna_On_Defeat_Transformation_Spec_v1_1.md,
    // and Enhanced_Variant_Transformation_Handoff.md.
    public class EnhancedVariantTransformationTests
    {
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

        static RunState MakeRun(int seed = 1) =>
            new RunState(MapGenerator.Generate(seed, 1), Database.Party.Select(id => Database.Characters[id]).ToList(), seed);

        // Fixed RNG returning a scripted sequence of NextDouble values (defaulting to the last value when exhausted)
        // so variance rolls in Battle ctor / Perform don't exhaust the scripted roll before transformation is checked.
        sealed class ScriptedRandom : Random
        {
            readonly Queue<double> scriptedDoubles;
            readonly double fallback;
            public ScriptedRandom(params double[] values) : base(1)
            {
                scriptedDoubles = new Queue<double>(values);
                fallback = values != null && values.Length > 0 ? values[values.Length - 1] : 0.99;
            }
            public override double NextDouble() =>
                scriptedDoubles.Count > 0 ? scriptedDoubles.Dequeue() : fallback;
        }

        // ---- 1. Data Validator (Variant Spec §13) ----

        [Test]
        public void DataValidator_AllPilotVariantsPassVariantSpecSection13()
        {
            Assert.AreEqual(3, Dungeon1Roster.EnhancedVariants.Length, "3 pilot Enhanced Common Variants");
            var expectedIds = new[] { "hollow_squire_enhanced", "soulbound_hound_enhanced", "cinder_wisp_enhanced" };
            CollectionAssert.AreEquivalent(expectedIds, Dungeon1Roster.EnhancedVariants.Select(v => v.id));

            foreach (var v in Dungeon1Roster.EnhancedVariants)
            {
                Assert.IsTrue(Database.Enemies.ContainsKey(v.id), $"{v.id} registered in Database.Enemies");
                bool ok = Dungeon1Roster.ValidateEnhancedVariant(v, out var errors);
                Assert.IsTrue(ok, $"{v.id} failed Variant Spec §13 validation:\n" + string.Join("\n", errors));

                // Verify QTE profile stays Normal-tier timing window (90ms Perfect / 240ms Good, 1000ms lead)
                var b = new Battle(DefaultParty(), new[] { v }, new List<string>(), new Random(1));
                var enemy = b.Enemies[0];
                foreach (var atk in v.attacks.Where(a => a.hits > 0))
                {
                    var qte = QteFactory.Defense(BattleAction.Strike(enemy, atk, b.Party[0]), QteAssist.Standard, null, new Random(1));
                    Assert.IsNotNull(qte, $"{v.id}.{atk.name} produces a defensive QTE");
                    if (qte is DefenseQte dq)
                    {
                        Assert.AreEqual(QteConfig.Profiles[Tier.Normal].perfect, dq.profile.perfect, 1e-6f, "Normal QTE Perfect window");
                        Assert.AreEqual(QteConfig.Profiles[Tier.Normal].good, dq.profile.good, 1e-6f, "Normal QTE Good window");
                    }
                }
            }
        }

        [Test]
        public void DataValidator_AlternativeTurnDamageStaysWithinOnePointFiveTimesBaseTurn()
        {
            foreach (var v in Dungeon1Roster.EnhancedVariants)
            {
                var baseDef = Database.Enemies[v.baseEnemyId];
                var bBase = new Battle(DefaultParty(level: 4), new[] { baseDef }, new List<string>(), new Random(1));
                var bEnh = new Battle(DefaultParty(level: 4), new[] { v }, new List<string>(), new Random(1));

                var altMove = v.attacks.First(a => !string.IsNullOrEmpty(a.patternId));
                var baseMove = baseDef.attacks.First(a => a.name == altMove.name);

                float baseRaw = Enumerable.Range(0, baseMove.hits).Sum(i => baseMove.PowerAt(i)) * (baseMove.UsesMagic ? bBase.Enemies[0].Magic : bBase.Enemies[0].Atk);
                float enhRaw = Enumerable.Range(0, altMove.hits).Sum(i => altMove.PowerAt(i)) * (altMove.UsesMagic ? bEnh.Enemies[0].Magic : bEnh.Enemies[0].Atk);
                // With Good defensive QTE on multi-hit patterns, effective turn damage stays <= 1.5x base turn (and raw stays within pilot bounds).
                float baseMitigated = UtilityScorer.EstimateExpectedDamage(bBase.Enemies[0], baseMove, bBase.Party[0]);
                float enhMitigatedGood = UtilityScorer.EstimateExpectedDamage(bEnh.Enemies[0], altMove, bEnh.Party[0])
                    * QteConfig.Defense(QteResult.Good, false, bEnh.Party[0]);

                Assert.LessOrEqual(enhMitigatedGood, baseMitigated * 1.50f,
                    $"{v.id}: Enhanced alternative turn ({enhMitigatedGood:0.1}) must be <= 1.5x base turn ({baseMitigated:0.1})");
                Assert.LessOrEqual(enhRaw, baseRaw * 1.60f,
                    $"{v.id}: raw unmitigated sum ({enhRaw:0.1}) vs base ({baseRaw:0.1})");
            }
        }

        // ---- 2. Roll Rules (Transformation Spec §3, §4, §5) ----

        [Test]
        public void RollRules_ExactlyOneRollPerInstanceAndSecondLethalDefeatsNormally()
        {
            // First roll 0.01 (< 0.10) succeeds -> transforms; second lethal hit must NOT roll again and must defeat the Enhanced unit.
            var rng = new ScriptedRandom(0.01, 0.01, 0.01);
            var b = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), rng);
            var squire = b.Enemies[0];

            var hit1 = b.Damage(squire, 999);
            Assert.IsTrue(hit1.transformed, "First lethal hit with roll 0.01 triggers transformation");
            Assert.IsTrue(squire.Alive, "Transformed enemy is alive");
            Assert.IsTrue(squire.transformRolled, "transformRolled is latched");
            Assert.IsTrue(squire.enemy.isEnhanced, "Enemy definition swapped to Enhanced variant");
            Assert.AreEqual("hollow_squire_enhanced", squire.enemy.id);

            var hit2 = b.Damage(squire, 999);
            Assert.IsFalse(hit2.transformed, "Second lethal hit never rolls transformation");
            Assert.IsFalse(squire.Alive, "Enhanced enemy is defeated permanently on second lethal hit");
            Assert.AreEqual(Outcome.Victory, b.Outcome);
        }

        [Test]
        public void RollRules_DoTLethalTriggersTransformationAndClearsTriggeringDoT()
        {
            var rng = new ScriptedRandom(0.01);
            var b = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), rng);
            var squire = b.Enemies[0];
            squire.unit.hp = 1;

            // Inflict lethal Burn DoT and advance to Squire's turn start
            var burn = new StatusApply { id = StatusId.Burn, turns = 3, magnitude = 10f, chance = 1f };
            squire.StatusController.TryApplyStatus(burn, b.Party[0], squire, out _, new ScriptedRandom(0.0), 1);
            squire.AddStatus(StatusId.Burn, 3, 10f, b.Party[0]);

            // Step turns until Squire's turn starts and the DoT ticks
            for (int i = 0; i < 10; i++)
            {
                var c = b.NextTurn();
                if (c == squire) break;
                if (c != null) b.Perform(BattleAction.Guard(c));
            }

            Assert.IsTrue(squire.Alive, "DoT lethal hit triggered transformation");
            Assert.AreEqual("hollow_squire_enhanced", squire.enemy.id);
            Assert.AreEqual(squire.Stats.maxHp, squire.unit.hp, "Restored to full Enhanced Max HP");
            Assert.IsFalse(squire.Has(StatusId.Burn), "Triggering DoT is cleared on transformation");
            Assert.AreEqual(0, squire.StatusController.ActiveInstances.Count, "All StatusController instances cleared");
            Assert.IsNotNull(squire.Intent, "Transformed enemy immediately planned an Enhanced intent for its pending turn");
        }

        [Test]
        public void RollRules_ExclusionsBlockSummonsPrologueAndBossNodeEncounters()
        {
            // 1. Summoned enemy never rolls
            var bSummon = new Battle(DefaultParty(), new[] { Database.Enemies["soul_reaper"] }, new List<string>(), new ScriptedRandom(0.01, 0.01));
            bSummon.Summon(bSummon.Enemies[0], Database.Enemies["hollow_squire"]);
            var summonedSquire = bSummon.Enemies[1];
            Assert.IsTrue(summonedSquire.IsSummon);
            var sumHit = bSummon.Damage(summonedSquire, 999);
            Assert.IsFalse(sumHit.transformed, "Summoned enemies must never transform");
            Assert.IsFalse(summonedSquire.Alive);

            // 2. Boss-node encounter enemy never rolls
            var bBoss = new Battle(DefaultParty(), new[] { Database.Enemies["zoroa_p1"], Database.Enemies["hollow_squire"] },
                new List<string>(), new ScriptedRandom(0.01, 0.01));
            var bossAdd = bBoss.Enemies[1];
            var bossHit = bBoss.Damage(bossAdd, 999);
            Assert.IsFalse(bossHit.transformed, "Enemies in a Boss-node encounter must never transform");
            Assert.IsFalse(bossAdd.Alive);

            // 3. Prologue run enemy never rolls
            var prologueRun = MakeRun();
            prologueRun.BeginPrologue();
            var bPrologue = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new ScriptedRandom(0.01), prologueRun);
            var proHit = bPrologue.Damage(bPrologue.Enemies[0], 999);
            Assert.IsFalse(proHit.transformed, "Tutorial Prologue enemies must never transform");
            Assert.IsFalse(bPrologue.Enemies[0].Alive);

            // 4. Forced removal (TryContinueAfterLethal with forced: true) never rolls
            var bForced = new Battle(DefaultParty(), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new ScriptedRandom(0.01));
            var fSquire = bForced.Enemies[0];
            fSquire.unit.hp = 0;
            Assert.IsFalse(bForced.TryContinueAfterLethal(fSquire, forced: true), "Forced removal must never roll transformation");
            Assert.IsFalse(fSquire.transformRolled);
        }

        [Test]
        public void RollRules_TenThousandSeededTrialsMatchTenPercentRate()
        {
            const int trials = 10000;
            var rng = new Random(20261122);
            int transformed = 0;
            var squireDef = Database.Enemies["hollow_squire"];
            var hero = DefaultParty(level: 1).Take(1).ToList();

            for (int i = 0; i < trials; i++)
            {
                var b = new Battle(hero, new[] { squireDef }, new List<string>(), rng);
                if (b.Damage(b.Enemies[0], 999).transformed)
                    transformed++;
            }

            double rate = (double)transformed / trials;
            Assert.AreEqual(Dungeon1Roster.PilotTransformChance, rate, 0.01,
                $"Observed transformation rate {rate:P2} over {trials} trials must be 10% +/- 1%");
        }

        // ---- 3. State After Transform & Multi-Hit / AoE Edge Cases (Transformation Spec §6.1, §7) ----

        [Test]
        public void StateAfterTransform_FullHpMpStatusesBrokenStunIntentChargeAndCooldownsReset()
        {
            var rng = new ScriptedRandom(0.01);
            var b = new Battle(DefaultParty(), new[] { Database.Enemies["cinder_wisp"] }, new List<string>(), rng);
            var wisp = b.Enemies[0];

            // Dirty every combat state on the base enemy before the lethal hit
            wisp.unit.mp = 0;
            wisp.ApplyBreakDamage(10000, b.Party[0], out _);
            Assert.IsTrue(wisp.IsBroken);
            wisp.stunTurns = 2;
            wisp.AddStatus(StatusId.Poison, 3, 5f, b.Party[0]);
            wisp.AddStatus(StatusId.DefenseDown, 2, 0.25f, b.Party[0]);
            wisp.Intent = new EnemyIntent { move = wisp.enemy.attacks[0], target = b.Party[0] };
            wisp.Charge = new EnemyIntent { move = wisp.enemy.attacks[0], target = b.Party[0] };
            wisp.JustReleasedCharge = true;
            wisp.cooldowns["move:Ember Bolt"] = 3;

            var hit = b.Damage(wisp, 5000); // Massive overkill
            Assert.IsTrue(hit.transformed);

            // Verify full Enhanced HP/MP, overkill discarded, Break refilled, Broken/statuses/stun/intent/charge/cooldowns cleared
            var expectedEnhUnit = new Unit(Database.Enemies["cinder_wisp_enhanced"], 1);
            Assert.AreEqual(expectedEnhUnit.stats.maxHp, wisp.unit.hp, "Full Enhanced Max HP (overkill discarded)");
            Assert.AreEqual(expectedEnhUnit.stats.maxMp, wisp.unit.mp, "Full Enhanced Max MP");
            Assert.AreEqual(wisp.MaxBreakGauge, wisp.CurrentBreakGauge, "Break Gauge refilled to max");
            Assert.IsFalse(wisp.IsBroken, "Broken removed");
            Assert.AreEqual(0, wisp.stunTurns, "Stun cleared");
            Assert.AreEqual(0, wisp.statuses.Count, "All legacy statuses cleared");
            Assert.AreEqual(0, wisp.StatusController.ActiveInstances.Count, "All StatusController instances cleared");
            Assert.IsNull(wisp.Charge, "Charge cleared");
            Assert.IsFalse(wisp.JustReleasedCharge, "JustReleasedCharge cleared");
            Assert.AreEqual(0, wisp.cooldowns.Count, "Move cooldowns reset");
            Assert.IsTrue(b.Log.Any(l => l == "The Cinder Wisp refuses to fall. It rises, Enhanced!"), "Unmistakable log line");
        }

        [Test]
        public void EdgeCases_RemainingHitsStopOnTransformedEnemyAndAoECoTargetsAreUnaffected()
        {
            // Multi-hit attack on Squire that transforms on Hit 1: Hit 2 must stop and not chip Enhanced HP or Break!
            var b = new Battle(DefaultParty(level: 10), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new ScriptedRandom(0.01));
            var squire = b.Enemies[0];
            squire.unit.hp = 1; // Hit 1 is lethal

            // Give Fighter a 2-hit skill or enemy-style 2-hit attack
            var twoHitMove = new EnemyAttack { name = "Double Strike", element = Element.Physical, hits = 2, power = 1.0f };
            var actor = b.NextTurn();
            while (actor != b.Party[0]) { b.Perform(BattleAction.Guard(actor)); actor = b.NextTurn(); }

            var hits = b.Perform(BattleAction.Strike(actor, twoHitMove, squire), 1f);
            Assert.AreEqual(1, hits.Count, "Remaining hits of a multi-hit action must stop on the enemy that just transformed");
            Assert.IsTrue(hits[0].transformed);
            Assert.AreEqual(squire.Stats.maxHp, squire.unit.hp, "Second hit did not damage the newly transformed Enhanced form");
            Assert.AreEqual(squire.MaxBreakGauge, squire.CurrentBreakGauge, "Killing hit did not chip the newly refilled Enhanced Break gauge");

            // AoE attack on 2 enemies where Enemy 0 transforms and Enemy 1 does not: Enemy 1 still takes full AoE hits!
            var bAoE = new Battle(DefaultParty(level: 10),
                new[] { Database.Enemies["hollow_squire"], Database.Enemies["possessed_royal_guard"] },
                new List<string>(), new ScriptedRandom(0.01));
            bAoE.Enemies[0].unit.hp = 1;
            int guardHpBefore = bAoE.Enemies[1].unit.hp;
            var aoeMove = new EnemyAttack { name = "Sweep", element = Element.Physical, hits = 2, power = 0.6f, allTargets = true };
            var aoeActor = bAoE.NextTurn();
            while (aoeActor != bAoE.Party[0]) { bAoE.Perform(BattleAction.Guard(aoeActor)); aoeActor = bAoE.NextTurn(); }

            var aoeHits = bAoE.Perform(BattleAction.Strike(aoeActor, aoeMove, null), 1f);
            Assert.IsTrue(bAoE.Enemies[0].enemy.isEnhanced, "Enemy 0 transformed");
            Assert.AreEqual(bAoE.Enemies[0].Stats.maxHp, bAoE.Enemies[0].unit.hp, "Enemy 0 kept full Enhanced HP after transforming on hit 1");
            Assert.AreEqual(2, aoeHits.Count(h => h.target == bAoE.Enemies[1]), "AoE co-target still received both hits");
            Assert.Less(bAoE.Enemies[1].unit.hp, guardHpBefore, "AoE co-target took damage normally");
        }

        // ---- 4. Initiative, Intent, Victory & Rewards (Transformation Spec §8, §11) ----

        [Test]
        public void InitiativeAndIntent_PendingTurnReplansImmediatelyAndAlreadyActedWaitsForNextRound()
        {
            // Case 1: Enemy has NOT acted yet this round -> re-plans immediately with Enhanced kit
            var bPending = new Battle(DefaultParty(level: 8), new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new ScriptedRandom(0.01));
            var firstActor = bPending.NextTurn(); // Fastest hero acts first; Hollow Squire (SPD 9) is still pending in queue
            var squire = bPending.Enemies[0];
            Assert.IsFalse(bPending.HasActed(squire), "Squire has not acted yet this round");
            int traceBefore = bPending.AiTrace.Entries.Count;

            bPending.Damage(squire, 999);
            Assert.IsTrue(squire.enemy.isEnhanced);
            Assert.IsNotNull(squire.Intent, "Pending transformed enemy immediately re-plans and publishes a new Enhanced intent");
            Assert.AreEqual(traceBefore + 1, bPending.AiTrace.Entries.Count, "AiTrace records the immediate re-plan");
            Assert.AreEqual("hollow_squire_enhanced", bPending.AiTrace.Entries.Last().enemyId);

            // Case 2: Enemy ALREADY acted this round -> does not get an extra turn and waits until next StartRound to plan
            var bActed = new Battle(DefaultParty(level: 8), new[] { Database.Enemies["soulbound_hound"] }, new List<string>(), new ScriptedRandom(0.01));
            var hound = bActed.Enemies[0];
            // Advance until Hound takes its turn
            var c = bActed.NextTurn();
            while (c != hound)
            {
                bActed.Perform(BattleAction.Guard(c));
                c = bActed.NextTurn();
            }
            bActed.Perform(EnemyAI.Choose(bActed, hound));
            Assert.IsTrue(bActed.HasActed(hound), "Hound has already acted in Round 1");

            // Now kill Hound later in Round 1 -> it transforms, waits for Round 2 (no extra turn in Round 1!)
            bActed.Damage(hound, 999);
            Assert.IsTrue(hound.enemy.isEnhanced);
            Assert.IsNull(hound.Intent, "Already-acted enemy clears intent and waits for next round preparation");

            // Finish remaining Round 1 turns: Hound must NOT act again in Round 1
            while (bActed.Round == 1)
            {
                var next = bActed.NextTurn();
                if (bActed.Round > 1) break;
                Assert.AreNotEqual(hound, next, "Transformed enemy that already acted must not receive an extra turn in the same round");
                bActed.Perform(BattleAction.Guard(next));
            }
            Assert.AreEqual(2, bActed.Round);
            Assert.IsNotNull(hound.Intent, "At Round 2 StartRound, the Enhanced enemy plans normally");
        }

        [Test]
        public void VictoryAndRewards_LastEnemyTransformingContinuesBattleAndPaysRewardsOnce()
        {
            var runWith = MakeRun(1);
            var runWithout = MakeRun(1);
            // Equip R013 (Dirge-Eater's Fang: triggers on enemy defeated) to verify relic hook fires once on final death only
            runWith.RelicManager.TryAddRelic("R013", out _);
            runWithout.RelicManager.TryAddRelic("R013", out _);

            var bTransform = new Battle(runWith.Party, new[] { Database.Enemies["hollow_squire"] }, runWith.Items, new ScriptedRandom(0.01), runWith);
            var bNormal = new Battle(runWithout.Party, new[] { Database.Enemies["hollow_squire"] }, runWithout.Items, new ScriptedRandom(0.99), runWithout);

            var sqT = bTransform.Enemies[0];
            var sqN = bNormal.Enemies[0];

            // First lethal hit: sqT transforms (battle continues, no relic trigger), sqN dies (Victory!)
            bTransform.NextTurn();
            bNormal.NextTurn();
            bTransform.Party[0].unit.hp = 50;
            bNormal.Party[0].unit.hp = 50;

            bTransform.Damage(sqT, 999);
            bNormal.Damage(sqN, 999);

            Assert.AreEqual(Outcome.None, bTransform.Outcome, "Last living enemy transforming must continue the battle");
            Assert.AreEqual(50, bTransform.Party[0].unit.hp, "OnEnemyDefeated relic hook must not fire when transformation intercepts defeat");
            Assert.AreEqual(Outcome.Victory, bNormal.Outcome);
            int hpAfterOneKill = bNormal.Party[0].unit.hp;

            // Final defeat of sqT triggers Victory and fires OnEnemyDefeated exactly once
            bTransform.Damage(sqT, 999);
            Assert.AreEqual(Outcome.Victory, bTransform.Outcome, "Victory triggers when the Enhanced form is finally defeated");
            Assert.AreEqual(hpAfterOneKill, bTransform.Party[0].unit.hp, "OnEnemyDefeated relic hook fires exactly once on final defeat");
            Assert.AreEqual(Database.Enemies["hollow_squire"].gold, sqT.enemy.gold, "Enhanced variant grants identical gold");
        }

        // ---- 5. AI Integration & Replay (Variant Spec §7.5, §8) ----

        [Test]
        public void AiIntegration_EnhancedCountsTowardFocusCapNormalAndPreservesSharedMemory()
        {
            // 1. FocusCapNormal: 2 Enhanced Hollow Squires + 1 Normal Hollow Squire must still obey FocusCapNormal = 2!
            var bFocus = new Battle(DefaultParty(), new[]
            {
                Database.Enemies["hollow_squire_enhanced"],
                Database.Enemies["hollow_squire_enhanced"],
                Database.Enemies["hollow_squire"],
            }, new List<string>(), new Random(1));
            bFocus.NextTurn();

            var targetCounts = bFocus.Enemies.Where(e => e.Intent?.target != null)
                .GroupBy(e => e.Intent.target)
                .Select(g => g.Count())
                .ToList();
            Assert.LessOrEqual(targetCounts.Max(), EncounterBlackboard.FocusCapNormal,
                "AiTier.Enhanced must count as Normal for FocusCapNormal");

            // 2. Capability matrix (Variant Spec §7.5): Enhanced has 1-turn resource awareness, no H=2 prediction/objectives/synergy setup
            var enhProfile = AiProfile.For(Database.Enemies["soulbound_hound_enhanced"]);
            Assert.AreEqual(AiTier.Enhanced, enhProfile.tier);
            Assert.IsTrue(enhProfile.tier.IsNormalTier());
            Assert.IsTrue(enhProfile.usesNextTurnResourceAwareness);
            Assert.IsFalse(enhProfile.usesPrediction);
            Assert.IsFalse(enhProfile.usesStrategicObjectives);
            Assert.IsFalse(enhProfile.usesSynergySetup);
            Assert.IsFalse(enhProfile.usesConditionList);

            // 3. TargetRule.HighestThreat on Enhanced Soulbound Hound reads shared encounter memory from before the transformation!
            var bThreat = new Battle(DefaultParty(level: 6), new[] { Database.Enemies["soulbound_hound"] }, new List<string>(), new ScriptedRandom(0.01));
            var hound = bThreat.Enemies[0];
            var highThreatHero = bThreat.Party[2]; // Wizard (slot 2)

            // Record observed high-impact attacks by Wizard before Hound transforms
            var prof = bThreat.Blackboard.Memory.GetOrCreateProfile(highThreatHero);
            prof.totalDamageDealt = 250;
            prof.offensiveTurns = 3;
            prof.AddObservation(1, BehaviorCategory.BurstDamage, 1f);
            prof.AddObservation(2, BehaviorCategory.BurstDamage, 1f);

            // Transform Hound -> its Enhanced form uses TargetRule.HighestThreat and immediately targets Wizard (slot 2)!
            bThreat.NextTurn();
            bThreat.Damage(hound, 999);
            Assert.AreEqual("soulbound_hound_enhanced", hound.enemy.id);
            var intent = PriorityBrain.Select(bThreat, hound);
            Assert.AreEqual(highThreatHero, intent.target, "Enhanced Soulbound Hound (HighestThreat) targets hero with highest observed impact in shared memory");
        }

        [Test]
        public void Replay_BattleWithOnDefeatTransformationReplaysIdentically()
        {
            // Find a seed in d1_e5 (which has 2 Hollow Squires) that triggers an On-Defeat Transformation and verify BattleReplay reproduces it identically.
            int transformSeed = -1;
            BattleRecord rec = null;
            BalanceSimulation.Result orig = null;

            for (int s = 1; s <= 100; s++)
            {
                var candidateRec = new BattleRecord();
                var r = BalanceSimulation.Fight("d1_e5", 4, BalanceSimulation.Build.Recommended,
                    BalanceSimulation.Executions[1], seed: s, gear: BalanceSimulation.Gear.D1WeaponArmor,
                    recordOut: candidateRec, collectSignatures: true);
                if (r.transformsTriggered > 0)
                {
                    transformSeed = s;
                    rec = candidateRec;
                    orig = r;
                    break;
                }
            }

            Assert.Greater(transformSeed, 0, "Expected at least one seed in 1..100 to trigger a transformation in d1_e5");
            int replayedTransforms = 0;
            var replayed = BattleReplay.Replay(rec);
            Assert.IsTrue(replayed.Log.Any(l => l.Contains("refuses to fall. It rises, Enhanced!")), "Replayed battle triggered transformation");

            var replaySigs = replayed.AiTrace.Entries.Select(e => e.Signature()).ToList();
            Assert.AreEqual(orig.decisionSignatures.Count, replaySigs.Count, $"Seed {transformSeed}: replay trace count matches");
            for (int i = 0; i < orig.decisionSignatures.Count; i++)
                Assert.AreEqual(orig.decisionSignatures[i], replaySigs[i], $"Seed {transformSeed}: trace entry {i} matches replay");
            _ = replayedTransforms;
        }
    }
}
