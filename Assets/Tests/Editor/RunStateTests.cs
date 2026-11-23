using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class RunStateTests
    {
        static IEnumerable<CharacterDef> Party => Database.Party.Select(id => Database.Characters[id]);
        static RunState NewRun(MapDef map, int seed = 1) => new RunState(map, Party, seed);

        // A straight corridor of the given node types, for exercising one node kind at a time.
        static MapDef Line(string eventId, params NodeType[] types) => new MapDef
        {
            name = "Test",
            nodes = types.Select((t, i) => new NodeDef
            {
                id = "n" + i, type = t, floor = i, lane = 0,
                next = i < types.Length - 1 ? new[] { "n" + (i + 1) } : Array.Empty<string>(),
                encounter = t == NodeType.Battle ? "d1_e1" : t == NodeType.Elite ? "d1_e7" : t == NodeType.Boss ? "d1_boss" : null,
                eventId = t == NodeType.Event ? eventId : null,
            }).ToArray(),
        };

        static RunState LineRun(params NodeType[] types) => NewRun(Line(null, types));

        // Max level means no EXP, so a level-up (which raises HP) cannot blur HP and healing checks.
        static void PinLevels(RunState run)
        {
            foreach (var u in run.Party) u.level = RunConfig.MaxLevel;
        }

        static RunState AtEvent(string id, int gold = 0)
        {
            var run = NewRun(Line(id, NodeType.Event, NodeType.Boss));
            run.Gold = gold;
            run.Enter(run.Map.Get("n0"));
            return run;
        }

        static int Paths(MapDef map)
        {
            var counts = new Dictionary<string, int>();
            foreach (var n in map.nodes.OrderByDescending(n => n.floor))
                counts[n.id] = n.type == NodeType.Boss ? 1 : n.next.Sum(id => counts[id]);
            return map.nodes.Where(n => n.floor == 0).Sum(n => counts[n.id]);
        }

        // ---- map generation ----

        [Test]
        public void GeneratedMapsAreValidAcrossManySeeds()
        {
            string[] d1Bosses = { "d1_e6", "d1_e7", "d1_boss" };
            for (int seed = 1; seed <= 100; seed++)
            {
                for (int stage = 1; stage <= 3; stage++)
                {
                    var map = MapGenerator.Generate(seed, act: 1, stage: stage);
                    Assert.IsNull(MapGenerator.Validate(map), $"seed {seed} stage {stage}");
                    Assert.AreEqual(MapGenerator.Floors, map.Floors);
                    Assert.AreEqual(MapGenerator.StartLanes, map.nodes.Count(n => n.floor == 0));
                    Assert.AreEqual(stage, map.stage);
                    Assert.AreEqual(3, map.totalStages);
                    Assert.AreEqual(stage == 3, map.IsFinalStage);
                    Assert.AreEqual(stage >= 2, map.allowEliteNodes, "D1 elites from stage 2 on");
                    if (stage == 1)
                        Assert.AreEqual(0, map.nodes.Count(n => n.type == NodeType.Elite), $"seed {seed} stage 1 has no mid-map elites");
                    foreach (var elite in map.nodes.Where(n => n.type == NodeType.Elite))
                        CollectionAssert.DoesNotContain(d1Bosses, elite.encounter, $"seed {seed} stage {stage}: stage bosses are special-node only");
                    Assert.GreaterOrEqual(Paths(map), 3, $"seed {seed} stage {stage}: needs real route choices");

                    var boss = map.nodes.Single(n => n.type == NodeType.Boss);
                    Assert.AreEqual(d1Bosses[stage - 1], boss.encounter, $"seed {seed} stage {stage} boss encounter");

                    foreach (var n in map.nodes)
                    {
                        var seen = new HashSet<string>();
                        var stack = new Stack<NodeDef>(new[] { n });
                        while (stack.Count > 0)
                        {
                            var m = stack.Pop();
                            if (seen.Add(m.id)) foreach (var id in m.next) stack.Push(map.Get(id));
                        }
                        Assert.IsTrue(seen.Contains(boss.id), $"seed {seed} stage {stage}: {n.id} cannot reach the boss");

                        if (n.IsCombat)
                        {
                            var enc = Database.Encounters[n.encounter];
                            if (n.type == NodeType.Boss)
                                Assert.AreEqual(stage == 3 ? Tier.Boss : Tier.Elite, enc.tier, $"seed {seed} stage {stage}: {n.id}");
                            else
                                Assert.AreEqual(n.type == NodeType.Elite ? Tier.Elite : Tier.Normal, enc.tier, $"seed {seed} stage {stage}: {n.id}");
                            Assert.LessOrEqual(enc.minFloor, n.floor);
                        }
                    }
                    Assert.IsTrue(map.nodes.Where(n => n.floor == map.Floors - 2).All(n => n.type == NodeType.Rest), "rest before the boss");
                }

                // Also verify a single-stage dungeon with mid-map Elite nodes enabled (Act 2)
                var act2Map = MapGenerator.Generate(seed, act: 2, stage: 1);
                Assert.IsNull(MapGenerator.Validate(act2Map), $"seed {seed} act 2");
                Assert.IsTrue(act2Map.allowEliteNodes);
                Assert.IsTrue(act2Map.nodes.Any(n => n.type == NodeType.Elite), $"seed {seed} act 2: has elite");
            }
        }

        [Test]
        public void MapsNeverForceTwoMovesInARow_AndTheTopLaneBranchesLikeTheRest()
        {
            int topNodes = 0, topBranching = 0, allNodes = 0, allBranching = 0;
            for (int seed = 1; seed <= 150; seed++)
                for (int act = 1; act <= 8; act++)
                    for (int stage = 1; stage <= MapGenerator.GetStageCount(act); stage++)
                    {
                        var map = MapGenerator.Generate(seed, act, stage);
                        int last = map.Floors - 1;
                        foreach (var n in map.nodes.Where(n => n.floor < last - 1)) // the rest floor -> boss is single by design
                        {
                            bool branches = n.next.Length >= 2;
                            allNodes++; if (branches) allBranching++;
                            if (n.lane == 0) { topNodes++; if (branches) topBranching++; }
                            if (!branches && map.Get(n.next[0]) is var only && only.floor < last - 1)
                                Assert.GreaterOrEqual(only.next.Length, 2, $"seed {seed} act {act} stage {stage}: {n.id} -> {only.id} forces two moves in a row");
                        }
                    }
            float top = (float)topBranching / topNodes, all = (float)allBranching / allNodes;
            TestContext.WriteLine($"branching: top lane {top:P0}, all nodes {all:P0}");
            Assert.Greater(top, all - 0.10f, $"top lane branches {top:P0} of the time vs {all:P0} overall: no lane is a corridor");
            Assert.Greater(all, 0.6f, $"most nodes offer a choice ({all:P0})");
        }

        [Test]
        public void SameSeedSameMap()
        {
            string Shape(MapDef m) => string.Join(";", m.nodes.Select(n => $"{n.id}:{n.type}:{n.encounter}:{n.eventId}:{string.Join(",", n.next)}"));
            Assert.AreEqual(Shape(MapGenerator.Generate(42)), Shape(MapGenerator.Generate(42)));
            Assert.AreNotEqual(Shape(MapGenerator.Generate(42)), Shape(MapGenerator.Generate(43)));
            Assert.AreEqual(Shape(MapGenerator.Generate(42, 1, 2)), Shape(MapGenerator.Generate(42, 1, 2)));
            Assert.AreNotEqual(Shape(MapGenerator.Generate(42, 1, 1)), Shape(MapGenerator.Generate(42, 1, 2)));
            Assert.AreNotEqual(Shape(MapGenerator.Generate(42, 1, 2)), Shape(MapGenerator.Generate(42, 1, 3)));
        }

        [Test]
        public void Dungeon1HasThreeStagesWithConfiguredBossesAndFourEnemyStageBosses()
        {
            var plan = MapGenerator.GetDungeonPlan(1);
            Assert.AreEqual(3, plan.TotalStages);
            Assert.AreEqual(3, MapGenerator.GetStageCount(1));
            Assert.AreEqual("d1_e6", plan.GetStage(1).bossEncounterId);
            Assert.AreEqual("d1_e7", plan.GetStage(2).bossEncounterId);
            Assert.AreEqual("d1_boss", plan.GetStage(3).bossEncounterId);

            // Stage 1 (Soul Reaper) and Stage 2 (Royal Ossuary Knight) each fight with 3 allies (4 enemies total)
            var s1Enc = Database.Encounters["d1_e6"];
            Assert.AreEqual(4, s1Enc.enemies.Length);
            Assert.AreEqual("soul_reaper", s1Enc.enemies[0]);

            var s2Enc = Database.Encounters["d1_e7"];
            Assert.AreEqual(4, s2Enc.enemies.Length);
            Assert.AreEqual("royal_ossuary_knight", s2Enc.enemies[0]);

            var s3Enc = Database.Encounters["d1_boss"];
            Assert.AreEqual(1, s3Enc.enemies.Length);
            Assert.AreEqual("zoroa_p1", s3Enc.enemies[0]);
        }

        [Test]
        public void Stage1AndStage2BossesAdvanceToNewMapAtFloor0PreservingRunState()
        {
            var run = NewRun(MapGenerator.Generate(17, act: 1, stage: 1), seed: 17);
            Assert.AreEqual(1, run.Stage);
            Assert.AreEqual(3, run.TotalStages);
            Assert.IsFalse(run.IsFinalStage);

            // Add persistent run state that must carry across stages
            run.Gold = 55;
            run.Party[0].hp = Math.Max(1, run.Party[0].stats.maxHp - 20);
            int expectedHp = run.Party[0].hp;
            run.ModifierManager.AddModifier("MOD-001", run); // 2 battles duration
            run.ModifierManager.AddModifier("MOD-006", run); // Inspiring Formation (2 battles)

            void ClearCurrentStage()
            {
                int startStage = run.Stage;
                while (run.Status == RunStatus.Active && run.Stage == startStage)
                {
                    var next = run.Available.First();
                    run.Enter(next);
                    switch (next.type)
                    {
                        case NodeType.Battle:
                        case NodeType.Elite:
                        case NodeType.Boss:
                            run.WinCombat();
                            if (next.type == NodeType.Boss && startStage < 3)
                            {
                                Assert.IsTrue(run.IsAtStageBoss);
                                Assert.IsFalse(run.Stats.bossDefeated, "Stage boss does not complete dungeon");
                                Assert.Greater(run.PendingReward.modifierChoices.Count, 0, "Stage boss still offers modifiers");
                            }
                            run.ClaimReward(next.type == NodeType.Boss && startStage == 3 ? 0 : -1);
                            break;
                        case NodeType.Treasure:
                            run.ClaimReward(-1);
                            break;
                        case NodeType.Rest:
                            run.RestRecover();
                            break;
                        case NodeType.Event:
                            run.ChooseEvent(run.CurrentEvent.choices.Length - 1);
                            break;
                        case NodeType.Shop:
                            run.Cleared.Add(next.id);
                            run.Stats.nodesCompleted++;
                            break;
                    }
                }
            }

            // Stage 1 -> clears Soul Reaper -> automatically generates Stage 2 map at floor 0
            string stage1Boss = run.Map.nodes.Single(n => n.type == NodeType.Boss).encounter;
            Assert.AreEqual("d1_e6", stage1Boss);
            ClearCurrentStage();

            Assert.AreEqual(RunStatus.Active, run.Status, "Stage 1 boss does not end the run");
            Assert.AreEqual(2, run.Stage);
            Assert.IsNull(run.Current, "Player starts at entrance of Stage 2 map");
            Assert.IsEmpty(run.Cleared, "Stage 2 cleared set starts fresh");
            CollectionAssert.AreEquivalent(run.Map.nodes.Where(n => n.floor == 0), run.Available, "Floor 0 nodes available on Stage 2");
            Assert.AreEqual(8, run.Stats.nodesCompleted);
            Assert.Greater(run.Gold, 55, "Run gold carried over and increased");
            Assert.IsFalse(run.Stats.bossDefeated);
            Assert.IsFalse(run.Inventory.HasKeyItem("KEY002"));

            // Add a fresh modifier before Stage 2 boss to verify modifiers carry over into Stage 3
            run.ModifierManager.AddModifier("MOD-001", run); // 2 battles
            int goldBeforeS2 = run.Gold;

            // Stage 2 -> clears Royal Ossuary Knight -> automatically generates Stage 3 map at floor 0
            string stage2Boss = run.Map.nodes.Single(n => n.type == NodeType.Boss).encounter;
            Assert.AreEqual("d1_e7", stage2Boss);
            ClearCurrentStage();

            Assert.AreEqual(RunStatus.Active, run.Status, "Stage 2 boss does not end the run");
            Assert.AreEqual(3, run.Stage);
            Assert.IsTrue(run.IsFinalStage);
            Assert.IsNull(run.Current, "Player starts at entrance of Stage 3 map");
            Assert.IsEmpty(run.Cleared, "Stage 3 cleared set starts fresh");
            CollectionAssert.AreEquivalent(run.Map.nodes.Where(n => n.floor == 0), run.Available);
            Assert.AreEqual(16, run.Stats.nodesCompleted);
            Assert.Greater(run.Gold, goldBeforeS2);
            Assert.IsFalse(run.Stats.bossDefeated);

            // Ensure a modifier added on Stage 3 is cleared upon defeating Zoroa
            run.ModifierManager.AddModifier("MOD-002", run);
            string stage3Boss = run.Map.nodes.Single(n => n.type == NodeType.Boss).encounter;
            Assert.AreEqual("d1_boss", stage3Boss);
            ClearCurrentStage();

            Assert.AreEqual(RunStatus.Victory, run.Status);
            Assert.IsTrue(run.Won && run.Stats.bossDefeated && run.Stats.bossReached);
            Assert.IsTrue(run.Inventory.HasKeyItem("KEY002"));
            CollectionAssert.Contains(run.Relics, Database.BossRelic);
            Assert.AreEqual(24, run.Stats.nodesCompleted);
            Assert.AreEqual(0, run.ModifierManager.Count, "All modifiers cleared after final boss");
            Assert.LessOrEqual(expectedHp, run.Party[0].stats.maxHp);
        }

        [Test]
        public void AbandoningMidStageResetsBackToStageOne()
        {
            var run = NewRun(MapGenerator.Generate(21, act: 1, stage: 1), seed: 21);
            Assert.IsTrue(run.AdvanceToNextStage());
            Assert.AreEqual(2, run.Stage);

            run.AbandonExpedition();
            Assert.AreEqual(1, run.Stage);
            Assert.AreEqual(1, run.Map.stage);
            Assert.IsNull(run.Current);
            Assert.IsEmpty(run.Cleared);
            Assert.AreEqual(RunStatus.Active, run.Status);
        }

        [Test]
        public void DungeonPlanDefSupportsVariableStageCountsPerDungeon()
        {
            var customPlan = new DungeonPlanDef
            {
                act = 2,
                name = "Custom Multi-Stage",
                stages = new[]
                {
                    new DungeonStageDef { stage = 1, bossEncounterId = "d1_e6" },
                    new DungeonStageDef { stage = 2, bossEncounterId = "d1_boss" },
                }
            };
            Assert.AreEqual(2, customPlan.TotalStages);
            Assert.AreEqual("d1_e6", customPlan.GetStage(1).bossEncounterId);
            Assert.AreEqual("d1_boss", customPlan.GetStage(2).bossEncounterId);
            Assert.AreEqual(3, MapGenerator.GetStageCount(1));
            Assert.AreEqual(1, MapGenerator.GetStageCount(2));
        }

        [Test]
        public void StageNodeRngProducesDistinctRollsPerStage()
        {
            string RollAt(int stage)
            {
                var map = new MapDef
                {
                    name = "Test",
                    act = 1,
                    stage = stage,
                    totalStages = 3,
                    nodes = new[] { new NodeDef { id = "f0n0", type = NodeType.Treasure, floor = 0, lane = 0 } }
                };
                var run = NewRun(map, seed: 99);
                run.Enter(run.Map.Get("f0n0"));
                return run.PendingReward.gold + ":" + string.Join(",", run.PendingReward.choices.Select(c => $"{c.kind}:{c.id}:{c.amount}"));
            }

            string s1 = RollAt(1);
            string s2 = RollAt(2);
            string s3 = RollAt(3);
            Assert.AreEqual(s1, RollAt(1), "Same stage and seed stays deterministic");
            Assert.IsTrue(s1 != s2 || s2 != s3, "Stages must not reuse identical node RNG streams");
        }

        [Test]
        public void NodeTypesAreReasonablyDistributed()
        {
            var allD1 = Enumerable.Range(1, 100).SelectMany(s => MapGenerator.Generate(s, act: 1, stage: 1).nodes).ToList();
            int CountD1(NodeType t) => allD1.Count(n => n.type == t);
            Assert.AreEqual(0, CountD1(NodeType.Elite), "Dungeon 1 has no mid-map elites");
            Assert.Greater(CountD1(NodeType.Battle), CountD1(NodeType.Event));
            Assert.Greater(CountD1(NodeType.Event), CountD1(NodeType.Treasure));
            Assert.AreEqual(100, CountD1(NodeType.Boss));

            var allD2 = Enumerable.Range(1, 100).SelectMany(s => MapGenerator.Generate(s, act: 2, stage: 1).nodes).ToList();
            int CountD2(NodeType t) => allD2.Count(n => n.type == t);
            Assert.Greater(CountD2(NodeType.Battle), CountD2(NodeType.Elite));
            Assert.Greater(CountD2(NodeType.Elite), 0);
        }

        // ---- flow ----

        [Test]
        public void OnlyReachableNodesCanBeEnteredAndNeverTwice()
        {
            var run = NewRun(MapGenerator.Generate(5));
            CollectionAssert.AreEquivalent(run.Map.nodes.Where(n => n.floor == 0), run.Available);
            Assert.Throws<InvalidOperationException>(() => run.Enter(run.Map.nodes.First(n => n.floor == 1)));

            var first = run.Available.First();
            run.Enter(first);
            CollectionAssert.IsEmpty(run.Available, "nothing selectable mid-encounter");
            run.WinCombat();
            run.ClaimReward(-1);
            CollectionAssert.AreEquivalent(first.next.Select(run.Map.Get), run.Available);
            Assert.Throws<InvalidOperationException>(() => run.Enter(first), "completed nodes cannot be re-entered");
            Assert.AreEqual(1, run.Floor);
        }

        [Test]
        public void CombatRewardsApplyExactlyOnce()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            var offer = run.PendingReward;
            Assert.Greater(offer.gold, 0);
            CollectionAssert.AreEqual(new[] { RewardKind.Item, RewardKind.Equipment, RewardKind.Gold }, offer.choices.Select(c => c.kind),
                "normal battles: a consumable, gear or gold");
            Assert.AreEqual(3, offer.modifierChoices.Count, "normal battles offer 3 modifiers");
            int modIdx = offer.modifierChoices.FindIndex(m => !Database.DungeonModifiers[m.id].IsOneShot);
            Assert.GreaterOrEqual(modIdx, 0, "expected at least one lasting modifier option");
            string pickedModId = offer.modifierChoices[modIdx].id;
            int items = run.Items.Count;

            run.ClaimReward(0, modIdx);
            Assert.AreEqual(offer.gold, run.Gold);
            Assert.AreEqual(items + 1, run.Items.Count);
            Assert.IsTrue(run.ModifierManager.ActiveModifiers.Any(m => m.modifierId == pickedModId),
                "claiming a normal battle reward with a modifier grants that modifier");
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(0), "claimed once");
            Assert.Throws<InvalidOperationException>(() => run.WinCombat(), "no second victory");
            Assert.AreEqual(offer.gold, run.Gold);
            Assert.AreEqual(1, run.Stats.battlesWon);
            Assert.AreEqual(1, run.Stats.nodesCompleted);
        }

        [Test]
        public void NonFinalBossBattles_OfferThreeModifiers_AndFinalBossOffersNone()
        {
            // Normal & Elite nodes on a regular map
            var run = LineRun(NodeType.Battle, NodeType.Elite, NodeType.Boss);
            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            Assert.AreEqual(3, run.PendingReward.modifierChoices.Count, "Normal battle offers 3 modifiers");
            run.ClaimReward(-1, 0);

            run.Enter(run.Map.Get("n1"));
            run.WinCombat();
            Assert.AreEqual(3, run.PendingReward.modifierChoices.Count, "Elite battle offers 3 modifiers");
            run.ClaimReward(-1, 0);

            // Final boss (single-stage test map -> IsFinalStage is true)
            run.Enter(run.Map.Get("n2"));
            run.WinCombat();
            CollectionAssert.IsEmpty(run.PendingReward.modifierChoices, "Final dungeon boss offers no modifiers");
            run.ClaimReward(0);
            Assert.AreEqual(0, run.ModifierManager.Count, "Modifiers clear on final boss clear");
        }

        [Test]
        public void RewardsAreDeterministicPerSeedRegardlessOfCombat()
        {
            string Offer(int seed, int rngNoise)
            {
                var run = NewRun(Line(null, NodeType.Elite, NodeType.Boss), seed);
                for (int i = 0; i < rngNoise; i++) run.Rng.Next(); // combat consumed randomness differently
                run.Enter(run.Map.Get("n0"));
                run.WinCombat();
                return run.PendingReward.gold + ":" + string.Join(",", run.PendingReward.choices.Select(c => $"{c.kind}{c.id}{c.amount}"));
            }
            Assert.AreEqual(Offer(7, 0), Offer(7, 13));
            Assert.AreNotEqual(Offer(7, 0), Offer(8, 0));
        }

        [Test]
        public void EachDungeonAttemptRerollsTheDungeon()
        {
            string Signature(RunState r) => string.Join(",", r.Map.nodes.Select(n => $"{n.id}{n.type}{n.encounter}{n.eventId}"));
            var run = NewRun(MapGenerator.Generate(5), seed: 5);

            run.BeginDungeonAttempt(1, 100);
            string first = Signature(run);
            run.BeginDungeonAttempt(1, 100);
            Assert.AreEqual(first, Signature(run), "the same attempt seed rebuilds the same dungeon");

            run.BeginDungeonAttempt(1, 101);
            Assert.AreNotEqual(first, Signature(run), "a retry must not replay the previous attempt's dungeon");
            Assert.AreEqual(101, run.DungeonSeed);
            Assert.AreEqual(1, run.Stage);

            run.BeginDungeonAttempt(2, 102);
            Assert.AreEqual(2, run.Act, "embarking to the next dungeon uses the same entry point");
        }

        [Test]
        public void WipesAndAbandonsSpendRetriesUntilTheExpeditionIsLost()
        {
            var run = NewRun(MapGenerator.Generate(5), seed: 5);
            PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateNewGameCommittedSnapshot(Party.ToList(), 5));
            try
            {
                Assert.AreEqual(RunConfig.RetriesPerExpedition, run.RetriesLeft);
                run.AbandonExpedition();
                Assert.AreEqual(RunConfig.RetriesPerExpedition - 1, run.RetriesLeft, "abandon spends a retry, and the rollback keeps it spent");

                while (run.RetriesLeft > 0)
                {
                    run.BeginDungeonAttempt(1, 200 + run.RetriesLeft);
                    run.LoseCombat();
                }
                Assert.IsFalse(run.ExpeditionLost, "spending the last retry still lets the expedition continue");
                Assert.IsFalse(run.CanAbandon);
                Assert.Throws<InvalidOperationException>(() => run.AbandonExpedition());

                run.BeginDungeonAttempt(1, 300);
                run.LoseCombat();
                Assert.IsTrue(run.ExpeditionLost, "a wipe with no retries left loses the expedition");
                Assert.IsTrue(PersistenceCommitHandler.ActiveCommittedSnapshot.expeditionLost);
            }
            finally
            {
                PersistenceCommitHandler.SetActiveCommittedSnapshot(null);
            }
        }

        [Test]
        public void EliteRewardsAreBetter()
        {
            var rng = new Random(3);
            for (int i = 0; i < 50; i++)
            {
                var elite = RewardGenerator.ForBattle(Tier.Elite, rng, new List<string>());
                Assert.Greater(elite.gold, RunConfig.BattleGold.max);
                Assert.IsTrue(elite.choices.Any(c => c.kind == RewardKind.Relic));
                Assert.IsTrue(elite.choices.Any(c => c.kind == RewardKind.Upgrade));
                Assert.IsFalse(elite.choices.Any(c => c.id == Database.BossRelic));
            }
        }

        [Test]
        public void TreasureOffersAChoiceAppliedOnce()
        {
            var run = LineRun(NodeType.Treasure, NodeType.Boss);
            run.Enter(run.Map.Get("n0"));
            var offer = run.PendingReward;
            Assert.AreEqual(3, offer.choices.Count);
            var relic = offer.choices.First(c => c.kind == RewardKind.Relic).id;
            run.ClaimReward(offer.choices.FindIndex(c => c.kind == RewardKind.Relic));
            CollectionAssert.Contains(run.Relics, relic);
            Assert.IsTrue(run.Cleared.Contains("n0"));
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(0));
        }

        [Test]
        public void RelicsAndUpgradesChangeStats()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            var before = run.Party.Select(u => (u.stats.maxHp, u.hp)).ToList();
            run.Grant(new RewardOption { kind = RewardKind.Relic, id = "iron_locket" });
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(before[i].maxHp + 12, run.Party[i].stats.maxHp);
                Assert.AreEqual(before[i].hp + 12, run.Party[i].hp);
            }
            run.Grant(new RewardOption { kind = RewardKind.Relic, id = "iron_locket" });
            Assert.AreEqual(before[0].maxHp + 12, run.Party[0].stats.maxHp, "a relic only counts once");

            var knight = run.Party[0];
            int defBefore = knight.stats.def;
            Assert.AreEqual($"MAX HP {knight.stats.maxHp}>{knight.stats.maxHp + 15}, DEF {defBefore}>{defBefore + 2}", knight.stats.Preview(Database.Upgrades["knight"].bonus));
            run.Grant(new RewardOption { kind = RewardKind.Upgrade, id = "knight" });
            Assert.AreEqual(defBefore + 2, knight.stats.def);
        }

        [Test]
        public void UpgradeRewardWithCreatedPartyNamesAndBoostsARealHero()
        {
            CharacterDef Hero(string id, string name) => new CharacterDef
            {
                id = id, name = name, classId = "fighter", raceId = "human", backgroundId = "oathsworn",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5),
            };
            var party = new[] { Hero("hero_a", "Aldric"), Hero("hero_b", "Brann"), Hero("hero_c", "Cass"), Hero("hero_d", "Dara") };
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = new RunState(Line(null, NodeType.Elite, NodeType.Boss), party, seed);
                run.Enter(run.Map.Get("n0"));
                run.WinCombat();
                int idx = run.PendingReward.choices.FindIndex(c => c.kind == RewardKind.Upgrade);
                var opt = run.PendingReward.choices[idx];

                var hero = run.Party.SingleOrDefault(p => p.id == opt.id);
                Assert.IsNotNull(hero, $"seed {seed}: upgrade targets a party hero, not preset '{opt.id}'");
                var (label, _) = RewardGenerator.Describe(opt, run.ModifierManager, run.Party);
                StringAssert.StartsWith(hero.name + ":", label);
                StringAssert.DoesNotContain("Knight", label);

                var before = run.Party.Select(p => p.stats.maxHp).ToList();
                run.ClaimReward(idx);
                for (int i = 0; i < run.Party.Count; i++)
                    if (run.Party[i] == hero) Assert.Greater(run.Party[i].stats.maxHp, before[i]);
                    else Assert.AreEqual(before[i], run.Party[i].stats.maxHp, "only the named hero is boosted");
            }
        }

        [Test]
        public void RestRecoverHealsPartOfMissingOnce()
        {
            var run = LineRun(NodeType.Rest, NodeType.Boss);
            var knight = run.Party[0];
            var mage = run.Party[2];
            knight.hp = knight.stats.maxHp - 100; // missing 100
            mage.mp = 0;                          // missing full maxMp
            int expectedMpRecover = (int)Math.Round(mage.stats.maxMp * RunConfig.RestRecover);
            run.Enter(run.Map.Get("n0"));
            Assert.AreEqual(50, RunState.RecoverAmount(knight));
            run.RestRecover();
            Assert.AreEqual(knight.stats.maxHp - 50, knight.hp);
            Assert.AreEqual(expectedMpRecover, mage.mp);
            Assert.AreEqual(run.Party[1].stats.maxHp, run.Party[1].hp, "never above max");
            Assert.Throws<InvalidOperationException>(() => run.RestRecover());
            Assert.Throws<InvalidOperationException>(() => run.RestImprove("mage"), "one choice per rest site");
        }

        [Test]
        public void RestImproveUpgradesOneHero()
        {
            var run = LineRun(NodeType.Rest, NodeType.Boss);
            var mage = run.Party[2];
            int mag = mage.stats.mag, maxMp = mage.stats.maxMp;
            run.Enter(run.Map.Get("n0"));
            run.RestImprove("mage");
            Assert.AreEqual(mag + 3, mage.stats.mag);
            Assert.AreEqual(maxMp + 10, mage.stats.maxMp);
            Assert.AreEqual(run.Party[0].stats.maxHp, Database.Characters["knight"].stats.maxHp, "others untouched");
        }

        [Test]
        public void EventChoicesApplyTheirConsequencesOnce()
        {
            var shrine = AtEvent("shrine", gold: 0);
            Assert.AreEqual("Need 30 spoils", shrine.EventBlocked(shrine.CurrentEvent.choices[0]));
            Assert.Throws<InvalidOperationException>(() => shrine.ChooseEvent(0));
            Assert.IsFalse(shrine.Cleared.Contains("n0"), "a refused choice changes nothing");

            var paid = AtEvent("shrine", gold: 40);
            var relic = paid.EventRelic;
            StringAssert.Contains(Database.Relics[relic].name, paid.ChooseEvent(0));
            Assert.AreEqual(10, paid.Gold);
            CollectionAssert.Contains(paid.Relics, relic);
            Assert.Throws<InvalidOperationException>(() => paid.ChooseEvent(2), "only once");

            var anoint = AtEvent("shrine");
            anoint.ChooseEvent(1);
            Assert.AreEqual(1, anoint.BattleBuffs.Count, "the blessing lasts until dungeon exit");

            var pray = AtEvent("shrine");
            pray.Party[0].hp = 10;
            pray.ChooseEvent(2);
            Assert.AreEqual(10 + pray.Party[0].stats.maxHp * 25 / 100, pray.Party[0].hp);

            var shop = AtEvent("merchant", gold: 20);
            int draughts = shop.Items.Count(i => i == "draught");
            shop.ChooseEvent(0);
            Assert.AreEqual(5, shop.Gold);
            Assert.AreEqual(draughts + 1, shop.Items.Count(i => i == "draught"));

            var altar = AtEvent("altar");
            altar.Party[1].hp = 5;
            altar.ChooseEvent(0);
            Assert.AreEqual(1, altar.Party[1].hp, "the altar never kills");
            Assert.AreEqual(1, altar.Relics.Count);

            var refuse = AtEvent("altar");
            refuse.ChooseEvent(1);
            Assert.IsEmpty(refuse.Relics);
            Assert.IsTrue(refuse.Cleared.Contains("n0"));

            var fallen = AtEvent("fallen");
            fallen.ChooseEvent(0);
            Assert.AreEqual(25, fallen.Gold);
            Assert.AreEqual(25, fallen.Stats.goldEarned);

            foreach (var e in Database.Events.Values)
                Assert.IsNull(AtEvent(e.id).EventBlocked(e.choices.Last()), $"{e.id}: the last choice is always open");
        }

        [Test]
        public void BossVictoryEndsTheRun()
        {
            var run = LineRun(NodeType.Boss);
            run.Enter(run.Map.Get("n0"));
            Assert.IsTrue(run.Stats.bossReached);
            run.WinCombat();
            Assert.AreEqual(RunConfig.BossGold, run.PendingReward.gold);
            var choices = run.PendingReward.choices;
            Assert.AreEqual(3, choices.Count, "the boss offers a real choice");
            Assert.AreEqual(Database.BossRelic, choices[0].id);
            Assert.AreEqual(RewardKind.Relic, choices[1].kind);
            Assert.AreEqual(RewardKind.Equipment, choices[2].kind);
            Assert.GreaterOrEqual(Database.Equipment[choices[2].id].rarity, EquipmentRarity.Rare, "boss gear is Rare or better");
            Assert.AreEqual(RunStatus.Active, run.Status, "not over until the reward is taken");
            run.ClaimReward(0);
            Assert.AreEqual(RunStatus.Victory, run.Status);
            Assert.IsTrue(run.Won && run.Stats.bossDefeated);
            CollectionAssert.IsEmpty(run.Available);
            Assert.IsNotNull(run.EndedUtc);
        }

        [Test]
        public void ModifierOffersSkipModifiersAlreadyAtTheirCap()
        {
            var allowed = Database.DungeonModifiers.Values.First(m => m.polarity == ModifierPolarity.Buff).modifierId;
            var offers = RewardGenerator.GenerateModifierChoices(new Random(1), count: 3, isMaxed: (id, _) => id != allowed);
            Assert.AreEqual(1, offers.Count, "every other modifier is maxed, so only one can be offered");
            Assert.AreEqual(allowed, offers[0].id);
        }

        [Test]
        public void DefeatEndsTheRun()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            run.Enter(run.Map.Get("n0"));
            run.LoseCombat();
            Assert.AreEqual(RunStatus.Defeat, run.Status);
            CollectionAssert.IsEmpty(run.Available);
            Assert.Throws<InvalidOperationException>(() => run.WinCombat());
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(-1));
        }

        [Test]
        public void PartyHpResourcesAndItemsPersistBetweenNodes()
        {
            var run = LineRun(NodeType.Battle, NodeType.Battle, NodeType.Boss);
            PinLevels(run);
            run.Party[0].hp = 50;
            run.Party[2].mp = 10;
            run.Items.Remove("ether");
            foreach (var id in new[] { "n0", "n1" })
            {
                run.Enter(run.Map.Get(id));
                run.WinCombat();
                run.ClaimReward(-1);
            }
            Assert.AreEqual(50, run.Party[0].hp);
            Assert.AreEqual(10, run.Party[2].mp);
            CollectionAssert.DoesNotContain(run.Items, "ether");
        }

        [Test]
        public void FallenRemainDeadAndEmberHeartHealsLivingAfterVictory()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            PinLevels(run);
            run.Grant(new RewardOption { kind = RewardKind.Relic, id = "ember_heart" });
            run.Party[2].hp = 0;
            run.Party[0].hp = 50;
            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            Assert.AreEqual(0, run.Party[2].hp, "fallen heroes remain at 0 HP after dungeon battle until revived");
            Assert.IsFalse(run.Party[2].Alive);
            Assert.AreEqual(50 + (int)Math.Round(run.Party[0].stats.maxHp * 0.08f), run.Party[0].hp);
        }

        [Test]
        public void ANewRunStartsFresh()
        {
            var old = NewRun(MapGenerator.Generate(3), 3);
            old.Gold = 99;
            old.Party[0].hp = 1;
            old.Grant(new RewardOption { kind = RewardKind.Relic, id = "whetstone" });
            old.LoseCombat();

            var fresh = NewRun(MapGenerator.Generate(4), 4);
            Assert.AreEqual(0, fresh.Gold);
            Assert.IsEmpty(fresh.Cleared);
            Assert.IsEmpty(fresh.Relics);
            Assert.AreEqual(RunStatus.Active, fresh.Status);
            CollectionAssert.AreEqual(Database.StartingItems, fresh.Items);
            Assert.IsTrue(fresh.Party.All(u => u.hp == u.stats.maxHp));
            Assert.AreEqual(Database.Characters["knight"].stats.atk, fresh.Party[0].stats.atk);
            Assert.AreEqual(RunStatus.Defeat, old.Status, "the old summary stays intact");
        }

        [Test]
        public void ContentReferencesResolve()
        {
            foreach (var c in Database.Characters.Values)
            {
                foreach (var s in c.skills) Assert.IsTrue(Database.Skills.ContainsKey(s), s);
                Assert.IsTrue(Database.Upgrades.ContainsKey(c.id), c.id + " has an upgrade");
            }
            foreach (var e in Database.Encounters.Values)
                foreach (var id in e.enemies) Assert.IsTrue(Database.Enemies.ContainsKey(id), id);
            foreach (var e in Database.Events.Values)
            {
                Assert.GreaterOrEqual(e.choices.Length, 2, e.id);
                foreach (var c in e.choices) if (c.item != null) Assert.IsTrue(Database.Items.ContainsKey(c.item), c.item);
            }
            Assert.IsTrue(Database.Relics.ContainsKey(Database.BossRelic));
            Assert.IsTrue(Database.Encounters.Values.Any(e => e.tier == Tier.Normal && e.minFloor == 0));
            Assert.IsTrue(Database.Encounters.Values.Any(e => e.tier == Tier.Elite && e.minFloor <= 2));
        }

        // ---- EXP & levelling ----

        [Test]
        public void ExpCurveLevelsUpCarriesRemainderAndCapsAtFifteen()
        {
            Assert.AreEqual(100, RunConfig.ExpToNext(1));
            Assert.AreEqual(125, RunConfig.ExpToNext(2));
            Assert.AreEqual(425, RunConfig.ExpToNext(14));

            var u = new Unit(Party.First());
            int sp = u.skillLoadout.SkillPoints;
            Assert.AreEqual(2, u.GainExp(300), "100 + 125 = two levels");
            Assert.AreEqual(3, u.level);
            Assert.AreEqual(75, u.exp, "remainder carries over");
            Assert.AreEqual(sp + 4, u.skillLoadout.SkillPoints, "+2 SP per level");

            u.GainExp(100000);
            Assert.AreEqual(RunConfig.MaxLevel, u.level);
            Assert.AreEqual(0, u.exp, "no EXP banked at the cap");
            Assert.AreEqual(0, u.GainExp(500));
            u.LevelUp();
            Assert.AreEqual(RunConfig.MaxLevel, u.level, "LevelUp is a no-op at the cap");
        }

        [Test]
        public void WinningGivesExpToStandingHeroesOnly()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            run.Enter(run.Map.Get("n0"));
            run.Party[1].hp = 0; // KO'd when the battle ends

            run.WinCombat();
            var offer = run.PendingReward;
            Assert.AreEqual(run.Party.Count, offer.expGained.Length);
            Assert.AreEqual(RunConfig.BattleExp, offer.expGained[0]);
            int levels = 0, remainder = RunConfig.BattleExp;
            while (remainder >= RunConfig.ExpToNext(1 + levels)) remainder -= RunConfig.ExpToNext(1 + levels++);
            Assert.AreEqual(remainder, run.Party[0].exp, "remainder after any level-ups");
            Assert.AreEqual(levels, offer.levelsGained[0]);
            Assert.AreEqual(0, offer.expGained[1]);
            Assert.AreEqual(0, run.Party[1].exp);
            Assert.AreEqual(0, offer.levelsGained[1], "a fallen hero gains nothing");
            Assert.IsFalse(run.Party[1].Alive, "fallen heroes remain dead (0 HP) after a dungeon battle until revived");
            Assert.AreEqual(0, run.Party[1].hp);
        }

        [Test]
        public void PilgrimsBurdenRaisesBattleExp()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            run.AddRelic("R031"); // EXP +15%
            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            int baseExp = RunConfig.BattleExp; // a local, so the bonus is computed at runtime in float like RelicManager does
            Assert.AreEqual(baseExp + (int)Math.Round(baseExp * 0.15f), run.PendingReward.expGained[0], "EXP x 1.15, bonus rounded");
        }
    }
}
