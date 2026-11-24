using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class EncounterRollerTests
    {
        static readonly string[] LegacyIds = { "skeleton", "cursed_knight", "warden" };

        [Test]
        public void MapRollsStayWithinBudgetAndRespectConstraints()
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                for (int stage = 1; stage <= 3; stage++)
                {
                    var map = MapGenerator.Generate(seed, 1, stage);
                    foreach (var n in map.nodes.Where(x => x.IsCombat))
                    {
                        var enc = Database.Encounters[n.encounter];
                        Assert.IsNotNull(n.enemies, $"seed {seed} s{stage} {n.id} ({enc.id}) missing rolled enemies");
                        Assert.GreaterOrEqual(n.enemies.Length, 1, enc.id);
                        Assert.LessOrEqual(n.enemies.Length, EncounterRoller.MaxEnemies, enc.id);

                        int supports = n.enemies.Count(EncounterRoller.IsSupport);
                        Assert.LessOrEqual(supports, EncounterRoller.MaxSupports, $"seed {seed} {enc.id} has {supports} supports");

                        if (enc.anchors != null)
                        {
                            foreach (var anchor in enc.anchors)
                                CollectionAssert.Contains(n.enemies, anchor, $"seed {seed} {enc.id} missing anchor {anchor}");
                        }

                        foreach (var id in n.enemies)
                        {
                            Assert.IsTrue(Database.Enemies.ContainsKey(id), $"unknown enemy {id}");
                            Assert.IsFalse(Database.Enemies[id].isEnhanced, $"enhanced variant {id} rolled directly");
                            CollectionAssert.DoesNotContain(LegacyIds, id, $"legacy enemy {id} rolled");
                            if (enc.id != "d1_boss")
                                Assert.IsFalse(id.StartsWith("zoroa_"), $"boss {id} rolled in {enc.id}");
                        }

                        float budget = EncounterRoller.Budget(enc);
                        float total = n.enemies.Sum(EncounterRoller.Threat);
                        Assert.GreaterOrEqual(total, budget * (1f - EncounterRoller.Tolerance) - 0.01f, $"seed {seed} {enc.id} under budget");
                        Assert.LessOrEqual(total, budget * (1f + EncounterRoller.Tolerance) + 0.01f, $"seed {seed} {enc.id} over budget");
                    }
                }
            }
        }

        [Test]
        public void SameSeedProducesIdenticalRolledEnemies()
        {
            for (int seed = 1; seed <= 10; seed++)
            {
                var a = MapGenerator.Generate(seed, 1, 2);
                var b = MapGenerator.Generate(seed, 1, 2);
                Assert.AreEqual(a.nodes.Length, b.nodes.Length);
                for (int i = 0; i < a.nodes.Length; i++)
                {
                    Assert.AreEqual(a.nodes[i].id, b.nodes[i].id);
                    Assert.AreEqual(a.nodes[i].type, b.nodes[i].type);
                    Assert.AreEqual(a.nodes[i].encounter, b.nodes[i].encounter);
                    CollectionAssert.AreEqual(a.nodes[i].enemies, b.nodes[i].enemies, $"seed {seed} node {a.nodes[i].id}");
                }
            }
        }

        [Test]
        public void ProceduralTemplatesProduceVarietyAndVariableCounts()
        {
            var procedural = Database.Encounters.Values.Where(e => e.Procedural).ToList();
            Assert.AreEqual(15, procedural.Count, "d1_e1..d1_e15 are procedural");

            foreach (var enc in procedural)
            {
                var distinct = new HashSet<string>();
                var counts = new HashSet<int>();
                for (int i = 0; i < 120; i++)
                {
                    var rolled = EncounterRoller.Roll(enc, new Random(1000 + i * 37));
                    distinct.Add(string.Join(",", rolled));
                    counts.Add(rolled.Length);
                }
                // d1_e13's reference (ossuary_acolyte + possessed_royal_guard) sits in the gap between 2-common and 3-common budgets at +/-10%.
                int minDistinct = enc.id == "d1_e13" ? 1 : 3;
                Assert.GreaterOrEqual(distinct.Count, minDistinct, $"{enc.id} produced only {distinct.Count} distinct compositions");
                if (enc.id is "d1_e6" or "d1_e7")
                {
                    Assert.IsTrue(counts.Contains(3) && counts.Contains(4),
                        $"{enc.id} should vary between 3 and 4 enemies (saw: {string.Join(",", counts)})");
                }
            }
        }

        [Test]
        public void PrologueAndBossEncountersStayFixedWithSupportSwap()
        {
            var tut1 = Database.Encounter("tut_n1");
            var tut2 = Database.Encounter("tut_n2");
            var tutBoss = Database.Encounter("tut_boss");
            var d1Boss = Database.Encounter("d1_boss");

            Assert.IsFalse(tut1.Procedural);
            Assert.IsFalse(tut2.Procedural);
            Assert.IsFalse(tutBoss.Procedural);
            Assert.IsFalse(d1Boss.Procedural);

            CollectionAssert.AreEqual(new[] { "riftling", "riftling" }, tut1.enemies);
            CollectionAssert.AreEqual(new[] { "ossuary_acolyte", "veil_stalker" }, tut2.enemies);
            CollectionAssert.AreEqual(new[] { "aetherion" }, tutBoss.enemies);
            CollectionAssert.AreEqual(new[] { "zoroa_p1" }, d1Boss.enemies);
        }
    }
}
