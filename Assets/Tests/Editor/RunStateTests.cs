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
                encounter = t == NodeType.Battle ? "skel2" : t == NodeType.Elite ? "elite1" : t == NodeType.Boss ? "boss" : null,
                eventId = t == NodeType.Event ? eventId : null,
            }).ToArray(),
        };

        static RunState LineRun(params NodeType[] types) => NewRun(Line(null, types));

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
            for (int seed = 1; seed <= 300; seed++)
            {
                var map = MapGenerator.Generate(seed);
                Assert.IsNull(MapGenerator.Validate(map), $"seed {seed}");
                Assert.AreEqual(MapGenerator.Floors, map.Floors);
                Assert.AreEqual(MapGenerator.StartLanes, map.nodes.Count(n => n.floor == 0));
                Assert.GreaterOrEqual(Paths(map), 3, $"seed {seed}: needs real route choices");

                var boss = map.nodes.Single(n => n.type == NodeType.Boss);
                foreach (var n in map.nodes)
                {
                    var seen = new HashSet<string>();
                    var stack = new Stack<NodeDef>(new[] { n });
                    while (stack.Count > 0)
                    {
                        var m = stack.Pop();
                        if (seen.Add(m.id)) foreach (var id in m.next) stack.Push(map.Get(id));
                    }
                    Assert.IsTrue(seen.Contains(boss.id), $"seed {seed}: {n.id} cannot reach the boss");

                    if (n.IsCombat)
                    {
                        var enc = Database.Encounters[n.encounter];
                        var want = n.type == NodeType.Boss ? Tier.Boss : n.type == NodeType.Elite ? Tier.Elite : Tier.Normal;
                        Assert.AreEqual(want, enc.tier, $"seed {seed}: {n.id}");
                        Assert.LessOrEqual(enc.minFloor, n.floor);
                    }
                }
                Assert.IsTrue(map.nodes.Where(n => n.floor == map.Floors - 2).All(n => n.type == NodeType.Rest), "rest before the boss");
            }
        }

        [Test]
        public void SameSeedSameMap()
        {
            string Shape(MapDef m) => string.Join(";", m.nodes.Select(n => $"{n.id}:{n.type}:{n.encounter}:{n.eventId}:{string.Join(",", n.next)}"));
            Assert.AreEqual(Shape(MapGenerator.Generate(42)), Shape(MapGenerator.Generate(42)));
            Assert.AreNotEqual(Shape(MapGenerator.Generate(42)), Shape(MapGenerator.Generate(43)));
        }

        [Test]
        public void NodeTypesAreReasonablyDistributed()
        {
            var all = Enumerable.Range(1, 100).SelectMany(s => MapGenerator.Generate(s).nodes).ToList();
            int Count(NodeType t) => all.Count(n => n.type == t);
            Assert.Greater(Count(NodeType.Battle), Count(NodeType.Elite));
            Assert.Greater(Count(NodeType.Battle), Count(NodeType.Event));
            Assert.Greater(Count(NodeType.Event), Count(NodeType.Treasure));
            Assert.AreEqual(100, Count(NodeType.Boss));
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
            Assert.AreEqual(2, offer.choices.Count);
            Assert.IsTrue(offer.choices.All(c => c.kind == RewardKind.Item), "normal battles: modest consumables");
            int items = run.Items.Count;

            run.ClaimReward(0);
            Assert.AreEqual(offer.gold, run.Gold);
            Assert.AreEqual(items + 1, run.Items.Count);
            Assert.Throws<InvalidOperationException>(() => run.ClaimReward(0), "claimed once");
            Assert.Throws<InvalidOperationException>(() => run.WinCombat(), "no second victory");
            Assert.AreEqual(offer.gold, run.Gold);
            Assert.AreEqual(1, run.Stats.battlesWon);
            Assert.AreEqual(1, run.Stats.nodesCompleted);
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
            Assert.AreEqual($"MAX HP {knight.stats.maxHp}>{knight.stats.maxHp + 15}, DEF 13>15", knight.stats.Preview(Database.Upgrades["knight"].bonus));
            run.Grant(new RewardOption { kind = RewardKind.Upgrade, id = "knight" });
            Assert.AreEqual(15, knight.stats.def);
        }

        [Test]
        public void RestRecoverHealsPartOfMissingOnce()
        {
            var run = LineRun(NodeType.Rest, NodeType.Boss);
            var knight = run.Party[0];
            var mage = run.Party[2];
            knight.hp = 25;   // missing 100
            mage.mp = 0;      // missing 60
            run.Enter(run.Map.Get("n0"));
            Assert.AreEqual(50, RunState.RecoverAmount(knight));
            run.RestRecover();
            Assert.AreEqual(75, knight.hp);
            Assert.AreEqual(30, mage.mp);
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
            Assert.AreEqual("Need 30 gold", shrine.EventBlocked(shrine.CurrentEvent.choices[0]));
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
            Assert.AreEqual(1, anoint.TakeBattleBuffs().Count);
            Assert.AreEqual(0, anoint.TakeBattleBuffs().Count, "the blessing lasts one battle");

            var pray = AtEvent("shrine");
            pray.Party[0].hp = 10;
            pray.ChooseEvent(2);
            Assert.AreEqual(10 + 125 * 25 / 100, pray.Party[0].hp);

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
            Assert.AreEqual(Database.BossRelic, run.PendingReward.choices.Single().id);
            Assert.AreEqual(RunStatus.Active, run.Status, "not over until the reward is taken");
            run.ClaimReward(0);
            Assert.AreEqual(RunStatus.Victory, run.Status);
            Assert.IsTrue(run.Won && run.Stats.bossDefeated);
            CollectionAssert.IsEmpty(run.Available);
            Assert.IsNotNull(run.EndedUtc);
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
        public void FallenRiseAndEmberHeartHealsAfterVictory()
        {
            var run = LineRun(NodeType.Battle, NodeType.Boss);
            run.Grant(new RewardOption { kind = RewardKind.Relic, id = "ember_heart" });
            run.Party[2].hp = 0;
            run.Party[0].hp = 50;
            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            Assert.AreEqual(1 + (int)Math.Round(run.Party[2].stats.maxHp * 0.08f), run.Party[2].hp);
            Assert.AreEqual(50 + (int)Math.Round(125 * 0.08f), run.Party[0].hp);
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
    }
}
