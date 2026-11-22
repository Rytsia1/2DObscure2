using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    // Tutorial Prologue (Assets/Docs/Tutorial_Prologue_Design.md): roster, flow, rewards, retry, boss gate, QTE ramp.
    public class PrologueTests
    {
        static RunState NewRun(int seed = 7) =>
            new RunState(MapGenerator.Generate(seed, 1), Database.Party.Select(id => Database.Characters[id]).ToList(), seed);

        static void ClearNode(RunState run)
        {
            run.Enter(run.Available.Single());
            run.WinCombat();
            run.ClaimReward(-1);
        }

        static EnemyAttack Move(string enemy, string name) => Database.Enemies[enemy].attacks.Single(a => a.name == name);

        static Battle BattleFor(RunState run) =>
            new Battle(run.Party, run.CurrentEncounter.enemies.Select(id => Database.Enemies[id]), run.Items, new Random(1), run);

        [SetUp]
        public void ResetCommitBaseline() => PersistenceCommitHandler.SetActiveCommittedSnapshot(null);

        [Test]
        public void Roster_MatchesTutorialWorkbook()
        {
            var e = Database.Enemies;
            Assert.AreEqual((80, 14, 13), (e["riftling"].stats.maxHp, e["riftling"].stats.atk, e["riftling"].stats.spd));
            Assert.AreEqual((70, 40, 16), (e["shard_wisp"].stats.maxHp, e["shard_wisp"].stats.maxMp, e["shard_wisp"].stats.mag));
            Assert.AreEqual((95, 35, 10), (e["veil_stalker"].stats.maxHp, e["veil_stalker"].stats.maxMp, e["veil_stalker"].stats.spd));
            var boss = e["aetherion"];
            Assert.AreEqual((600, 120, 200, 0.5f), (boss.stats.maxHp, boss.stats.maxMp, boss.maxBreakGauge, boss.phase2At));
            var collapse = Move("aetherion", "Threshold Collapse");
            CollectionAssert.AreEqual(new[] { 14, 14, 16, 22 }, collapse.hitDamage);
            Assert.IsTrue(collapse.telegraph && collapse.HeavyAt(3) && !collapse.HeavyAt(2));
            Assert.AreEqual(2, Move("aetherion", "Rift Pulse").phase);
            Assert.IsTrue(Move("veil_stalker", "Dread Pulse").StatusOnly);
            Assert.AreEqual(Effectiveness.Weak, e["shard_wisp"].affinities.Against(Element.Physical));
            Assert.AreEqual("tut_boss", Database.Encounter("tut_boss").id);
            Assert.IsFalse(Database.Encounters.ContainsKey("tut_n1"), "dungeon maps must never roll tutorial encounters");
        }

        [Test]
        public void PlayThrough_EndsAtLevel2_AndMatchesSkip()
        {
            var run = NewRun();
            var dungeonMap = run.Map;
            int gold = run.HubGold, draughts = run.CarryCount("draught"), ethers = run.CarryCount("ether");

            run.BeginPrologue();
            Assert.IsTrue(run.IsPrologue);
            Assert.AreEqual(3, run.Map.nodes.Length);
            ClearNode(run);
            Assert.IsTrue(run.Party.All(u => u.level == 1 && u.exp == Prologue.NodeExp[0]));
            ClearNode(run);
            Assert.IsTrue(run.Party.All(u => u.level == 2), "Node 2 brings the party to Level 2");
            Assert.AreEqual(draughts + 2, run.CarryCount("draught"));
            ClearNode(run);

            Assert.IsFalse(run.IsPrologue);
            Assert.IsNull(run.Tutorial);
            Assert.AreSame(dungeonMap, run.Map);
            Assert.AreEqual(RunStatus.Active, run.Status);
            Assert.AreEqual(gold + Prologue.ClearGold, run.HubGold);
            Assert.AreEqual(ethers + 1, run.CarryCount("ether"));
            Assert.IsTrue(run.Party.All(u => u.hp == u.stats.maxHp && u.mp == u.stats.maxMp));
            Assert.AreEqual(run.HubGold, PersistenceCommitHandler.ActiveCommittedSnapshot.hubGold, "the Prologue result is the committed baseline");

            var skip = NewRun();
            skip.SkipPrologue();
            CollectionAssert.AreEqual(run.Party.Select(u => (u.level, u.exp, u.stats.maxHp)), skip.Party.Select(u => (u.level, u.exp, u.stats.maxHp)));
            CollectionAssert.AreEquivalent(run.Items, skip.Items);
            Assert.AreEqual(run.HubGold, skip.HubGold);
        }

        [Test]
        public void Wipe_RestartsTheNodeAsEntered_WithoutSpendingARetry()
        {
            var run = NewRun();
            run.BeginPrologue();
            ClearNode(run);
            run.Party[0].hp = 5;
            run.Enter(run.Available.Single());
            var hp = run.Party.Select(u => u.hp).ToList();
            var items = run.Items.ToList();
            int bag = run.Inventory.GetConsumableCount("draught"), retries = run.RetriesLeft;

            foreach (var u in run.Party) u.hp = 0;
            run.Items.Remove("draught");
            run.Inventory.RemoveConsumable("draught", 1);
            run.LoseCombat();

            Assert.AreEqual(RunStatus.Active, run.Status);
            Assert.IsTrue(run.IsPrologue);
            Assert.AreEqual("p1", run.Current.id);
            CollectionAssert.AreEqual(hp, run.Party.Select(u => u.hp));
            CollectionAssert.AreEqual(items, run.Items);
            Assert.AreEqual(bag, run.Inventory.GetConsumableCount("draught"));
            Assert.AreEqual(retries, run.RetriesLeft);
            Assert.AreEqual(1, run.Tutorial.Wipes);
        }

        [Test]
        public void ThresholdCollapse_WaitsForBreakPressureOrRound5()
        {
            var collapse = Move("aetherion", "Threshold Collapse");
            var b = new Battle(Database.Party.Select(id => new Unit(Database.Characters[id])), new[] { Database.Enemies["aetherion"] },
                new List<string>(), new Random(3));
            var boss = b.Enemies[0];
            void ToRound(int r)
            {
                while (b.Round < r)
                {
                    var c = b.NextTurn();
                    if (c != null) b.Perform(BattleAction.Guard(c));
                }
            }

            ToRound(3);
            Assert.AreNotSame(collapse, PriorityBrain.Select(b, boss)?.move, "full gauge before round 5: no signature yet");
            boss.CurrentBreakGauge = (int)(boss.MaxBreakGauge * 0.4f);
            Assert.AreSame(collapse, PriorityBrain.Select(b, boss)?.move, "pressured to 40%: it charges, so an interrupt is reachable");

            boss.CurrentBreakGauge = boss.MaxBreakGauge;
            ToRound(5);
            Assert.AreSame(collapse, PriorityBrain.Select(b, boss)?.move, "from round 5 it charges regardless");
        }

        [Test]
        public void DefenseQte_RampsFromCleanToScriptedFeint_AndFirstSightRunsSlower()
        {
            var run = NewRun();
            run.BeginPrologue();
            run.Enter(run.Available.Single());
            var b = BattleFor(run);
            var claw = BattleAction.Strike(b.Enemies[0], Move("riftling", "Rift Claw"), b.Party[0]);

            var first = (DefenseQte)QteFactory.Defense(claw, QteAssist.Standard, run, new Random(0));
            var second = (DefenseQte)QteFactory.Defense(claw, QteAssist.Standard, run, new Random(0));
            Assert.AreEqual(second.profile.windup * Prologue.FirstTimeSlow, first.profile.windup, 1e-4f);
            for (int seed = 0; seed < 200; seed++)
            {
                var q = (DefenseQte)QteFactory.Defense(claw, QteAssist.Standard, run, new Random(seed));
                Assert.IsFalse(Enumerable.Range(0, q.Hits).Any(q.IsFeint), "Node 1 never feints");
            }

            run.WinCombat();
            run.ClaimReward(-1);
            run.Enter(run.Available.Single());
            b = BattleFor(run);
            var strike = BattleAction.Strike(b.Enemies[1], Move("veil_stalker", "Veil Strike"), b.Party[0]);
            var scripted = (DefenseQte)QteFactory.Defense(strike, QteAssist.Standard, run, new Random(0));
            Assert.IsTrue(scripted.IsFeint(0), "the first physical hit in Node 2 is the scripted feint");
            for (int seed = 0; seed < 100; seed++)
            {
                var q = (DefenseQte)QteFactory.Defense(strike, QteAssist.Standard, run, new Random(seed));
                Assert.IsFalse(q.IsFeint(0), "only one feint in Node 2");
            }
        }

        [Test]
        public void FirstDreadPulse_AlwaysLands()
        {
            var run = NewRun();
            run.BeginPrologue();
            ClearNode(run);
            run.Enter(run.Available.Single());
            var b = BattleFor(run);
            var target = b.Party[0];
            var stalker = b.Enemies[1];
            Combatant c;
            while ((c = b.NextTurn()) != stalker) if (c != null) b.Perform(BattleAction.Guard(c)); // Perform needs the stalker's turn
            b.Perform(BattleAction.Strike(stalker, Move("veil_stalker", "Dread Pulse"), target));
            Assert.IsTrue(target.Has(StatusId.Fear));
            Assert.IsTrue(run.Tutorial.FearScripted);
        }
    }
}
