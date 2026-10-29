using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class RunStateTests
    {
        static RunState NewRun() => new RunState(Database.Map, Database.Party.Select(id => Database.Characters[id]), 1);

        [Test]
        public void ContentReferencesResolve()
        {
            foreach (var c in Database.Characters.Values)
                foreach (var s in c.skills) Assert.IsTrue(Database.Skills.ContainsKey(s), s);
            foreach (var e in Database.Encounters.Values)
                foreach (var id in e.enemies) Assert.IsTrue(Database.Enemies.ContainsKey(id), id);
            foreach (var n in Database.Map.nodes)
            {
                foreach (var id in n.next) Assert.DoesNotThrow(() => Database.Map.Get(id), id);
                if (n.IsCombat) Assert.IsTrue(Database.Encounters.ContainsKey(n.encounter), n.id);
                else Assert.IsNotNull(n.reward, n.id);
            }
        }

        [Test]
        public void EveryNodeLeadsToTheBoss()
        {
            var map = Database.Map;
            foreach (var start in map.nodes)
            {
                var seen = new HashSet<string>();
                var stack = new Stack<NodeDef>(new[] { start });
                while (stack.Count > 0)
                {
                    var n = stack.Pop();
                    if (!seen.Add(n.id)) continue;
                    foreach (var id in n.next) stack.Push(map.Get(id));
                }
                Assert.IsTrue(seen.Contains("boss"), $"{start.id} cannot reach the boss");
            }
        }

        [Test]
        public void OnlyReachableNodesCanBeEntered()
        {
            var run = NewRun();
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, run.Available.Select(n => n.id));
            Assert.Throws<InvalidOperationException>(() => run.Enter(run.Map.Get("d")));

            run.Enter(run.Map.Get("a"));
            CollectionAssert.IsEmpty(run.Available, "nothing selectable mid-encounter");
            run.WinCombat();
            run.ClaimReward();
            CollectionAssert.AreEquivalent(new[] { "c", "d" }, run.Available.Select(n => n.id));
            Assert.Throws<InvalidOperationException>(() => run.Enter(run.Map.Get("e")));
        }

        [Test]
        public void FullPathToBossWinsTheRun()
        {
            var run = NewRun();
            foreach (var id in new[] { "b", "e", "h", "k", "m", "n", "boss" })
            {
                var node = run.Map.Get(id);
                run.Enter(node);
                if (node.IsCombat) run.WinCombat();
                Assert.IsFalse(run.Won);
                run.ClaimReward();
            }
            Assert.IsTrue(run.Won);
            Assert.AreEqual(16 + 25 + 40 + 100, run.Gold); // skel2 + chest + reliquary + warden
            CollectionAssert.AreEqual(Database.StartingItems.Concat(new[] { "draught", "elixir" }), run.Items);
        }

        [Test]
        public void FallenRiseWithOneHpAfterVictory()
        {
            var run = NewRun();
            run.Enter(run.Map.Get("a"));
            run.Party[2].hp = 0;
            run.WinCombat();
            Assert.AreEqual(1, run.Party[2].hp);
        }

        [Test]
        public void RestHealsLivingUnitsOnly()
        {
            var run = NewRun();
            var knight = run.Party[0];
            var rogue = run.Party[1];
            foreach (var id in new[] { "b", "e", "h" })
            {
                var node = run.Map.Get(id);
                run.Enter(node);
                if (node.IsCombat) run.WinCombat();
                if (id == "h") { knight.hp = 10; rogue.hp = 0; } // after the battle, which would revive the rogue
                run.ClaimReward();
            }
            Assert.AreEqual(10 + knight.stats.maxHp * 30 / 100, knight.hp);
            Assert.AreEqual(0, rogue.hp);
        }
    }
}
