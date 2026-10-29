using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class BattleTests
    {
        static Battle NewBattle(params string[] enemies) => NewBattle(new List<string>(Database.StartingItems), enemies);

        static Battle NewBattle(List<string> items, params string[] enemies) => new Battle(
            Database.Party.Select(id => new Unit(Database.Characters[id])),
            enemies.Select(id => Database.Enemies[id]), items, new Random(1));

        static Combatant P(Battle b, string name) => b.Party.First(c => c.name == name);

        // Everyone guards (no damage) until it is `who`'s turn.
        static void AdvanceTo(Battle b, Combatant who)
        {
            for (int i = 0; i < 100; i++)
            {
                var c = b.NextTurn();
                if (c == who) return;
                b.Perform(BattleAction.Guard(c));
            }
            Assert.Fail($"{who.name} never got a turn");
        }

        [Test]
        public void TurnOrderIsBySpeedWithDeterministicTies()
        {
            var b = NewBattle("skeleton", "skeleton");
            P(b, "Mage").unit.stats.spd = 8; // ties with both skeletons: party wins ties, then slot
            var order = TurnOrder.Build(b.All).Select(c => c.tag);
            CollectionAssert.AreEqual(new[] { "ROG", "HEA", "MAG", "SKA", "SKB", "KNI" }, order);
        }

        [Test]
        public void EachCombatantActsExactlyOncePerRound()
        {
            var b = NewBattle("skeleton", "skeleton", "skeleton");
            var byRound = new Dictionary<int, List<Combatant>>();
            while (true)
            {
                var c = b.NextTurn();
                if (b.Round > 3) break;
                if (!byRound.ContainsKey(b.Round)) byRound[b.Round] = new List<Combatant>();
                byRound[b.Round].Add(c);
                b.Perform(BattleAction.Guard(c));
            }
            for (int r = 1; r <= 3; r++)
            {
                Assert.AreEqual(byRound[r].Count, byRound[r].Distinct().Count(), $"someone acted twice in round {r}");
                CollectionAssert.AreEquivalent(b.All, byRound[r], $"everyone acts in round {r}");
            }
        }

        [Test]
        public void DeadCombatantsAreSkipped()
        {
            var b = NewBattle("skeleton", "skeleton");
            var first = b.NextTurn(); // Rogue, before any skeleton
            var skA = b.Enemies[0];
            skA.unit.hp = 0;
            b.Perform(BattleAction.Guard(first));

            var rest = new List<Combatant>();
            for (var c = b.NextTurn(); b.Round == 1; c = b.NextTurn()) { rest.Add(c); b.Perform(BattleAction.Guard(c)); }
            CollectionAssert.DoesNotContain(rest, skA);
            CollectionAssert.DoesNotContain(b.NextRoundOrder, skA);
        }

        [Test]
        public void TurnOrderIsRecalculatedAtEachNewRound()
        {
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");
            var rogue = b.NextTurn();
            knight.unit.stats.spd = 99; // mid-round change: must not reorder the current round
            b.Perform(BattleAction.Guard(rogue));

            var rest = new List<Combatant>();
            Combatant c;
            while ((c = b.NextTurn()) != null && b.Round == 1) { rest.Add(c); b.Perform(BattleAction.Guard(c)); }
            Assert.AreEqual(knight, rest.Last(), "knight keeps its round-1 slot");
            Assert.AreEqual(2, b.Round);
            Assert.AreEqual(knight, b.Active, "knight leads round 2");
        }

        [Test]
        public void InvalidActionsAreRejectedWithoutSideEffects()
        {
            var b = NewBattle(new List<string>(), "skeleton", "skeleton");
            var rogue = b.NextTurn();
            var mage = P(b, "Mage");
            var skA = b.Enemies[0];
            int hpBefore = skA.unit.hp;

            Assert.AreEqual("Invalid target.", b.Validate(BattleAction.Attack(rogue, mage)), "attack ally");
            skA.unit.hp = 0;
            Assert.AreEqual("Invalid target.", b.Validate(BattleAction.Attack(rogue, skA)), "attack dead foe");
            skA.unit.hp = hpBefore;
            Assert.IsNotNull(b.Validate(BattleAction.Attack(mage, skA)), "out of turn");
            Assert.AreEqual("Unknown skill.", b.Validate(BattleAction.Skill(rogue, Database.Skills["firebolt"], skA)));
            rogue.unit.mp = 0;
            Assert.AreEqual("Not enough MP.", b.Validate(BattleAction.Skill(rogue, Database.Skills["quick_strike"], skA)));
            Assert.AreEqual("No such item.", b.Validate(BattleAction.Item(rogue, Database.Items["draught"], rogue)));
            Assert.IsNotNull(b.Validate(null));

            Assert.Throws<InvalidOperationException>(() => b.Perform(BattleAction.Attack(rogue, mage)));
            Assert.AreEqual(rogue, b.Active, "turn not consumed");
            Assert.AreEqual(hpBefore, skA.unit.hp);
            Assert.IsNull(b.Validate(BattleAction.Attack(rogue, skA)), "valid action still accepted");
        }

        [Test]
        public void GuardHalvesDamageAndLastsUntilOwnNextTurn()
        {
            var b = NewBattle("warden");
            var knight = P(b, "Knight");
            var warden = b.Enemies[0];
            int open = Rules.Physical(warden, knight, 1f, new Random(3));
            knight.guarding = true;
            int guarded = Rules.Physical(warden, knight, 1f, new Random(3));
            Assert.AreEqual((int)Math.Round(open * Rules.GuardMultiplier), guarded, 1);
            knight.guarding = false;

            AdvanceTo(b, knight);
            b.Perform(BattleAction.Guard(knight));
            Assert.IsTrue(knight.guarding);
            var next = b.NextTurn();
            Assert.IsTrue(knight.guarding, "still guarding while others act");
            b.Perform(BattleAction.Guard(next));
            AdvanceTo(b, knight);
            Assert.IsFalse(knight.guarding, "guard ends when the knight's next turn starts");
        }

        [Test]
        public void EveryoneUnableToActNeverHangs()
        {
            var b = NewBattle("skeleton");
            foreach (var c in b.All) c.stunTurns = 1;
            var actor = b.NextTurn();
            Assert.IsNotNull(actor);
            Assert.AreEqual(2, b.Round);
            Assert.AreEqual(5, b.Log.Count(l => l.Contains("stunned")));

            foreach (var c in b.All) c.stunTurns = 50;
            b.Perform(BattleAction.Guard(actor));
            Combatant next = null;
            for (int i = 0; i < 200 && next == null; i++) next = b.NextTurn(); // null on idle cycles, never loops forever
            Assert.IsNotNull(next);
        }

        [Test]
        public void SkillsAndItemsResolve()
        {
            var b = NewBattle("skeleton", "skeleton", "skeleton");
            var mage = P(b, "Mage");
            AdvanceTo(b, mage);
            var hits = b.Perform(BattleAction.Skill(mage, Database.Skills["arcane_burst"]));
            Assert.AreEqual(3, hits.Count, "hits every enemy");
            Assert.AreEqual(mage.Stats.maxMp - 14, mage.unit.mp);

            var rogue = P(b, "Rogue");
            AdvanceTo(b, rogue);
            var target = b.Enemies.First(e => e.Alive);
            target.unit.hp = 999;
            Assert.AreEqual(1, b.Perform(BattleAction.Skill(rogue, Database.Skills["expose"], target)).Count);
            Assert.IsTrue(target.Has(StatusId.DefenseDown), "expose lowers defense");

            var healer = P(b, "Healer");
            var knight = P(b, "Knight");
            knight.unit.hp = 10;
            AdvanceTo(b, healer);
            b.Perform(BattleAction.Skill(healer, Database.Skills["heal"], knight));
            Assert.Greater(knight.unit.hp, 10);

            AdvanceTo(b, knight);
            int hp = knight.unit.hp, draughts = b.Items.Count(i => i == "draught");
            b.Perform(BattleAction.Item(knight, Database.Items["draught"], knight));
            Assert.AreEqual(Math.Min(knight.Stats.maxHp, hp + 40), knight.unit.hp);
            Assert.AreEqual(draughts - 1, b.Items.Count(i => i == "draught"));
        }

        [Test]
        public void FullBattleToVictory()
        {
            var b = NewBattle("skeleton", "skeleton", "skeleton");
            for (int turn = 0; turn < 500 && b.Outcome == Outcome.None; turn++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                b.Perform(c.IsEnemy ? EnemyAI.Choose(b, c) : BattleAction.Attack(c, b.Enemies.First(e => e.Alive)));
            }
            Assert.AreEqual(Outcome.Victory, b.Outcome);
            Assert.IsNull(b.NextTurn(), "no turns after the battle ends");
        }

        [Test]
        public void FullBattleToDefeat()
        {
            var b = NewBattle("warden");
            foreach (var p in b.Party) p.unit.hp = 5;
            for (int turn = 0; turn < 500 && b.Outcome == Outcome.None; turn++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                Assert.IsTrue(c.Alive, "dead combatants never act");
                b.Perform(c.IsEnemy ? EnemyAI.Choose(b, c) : BattleAction.Guard(c));
            }
            Assert.AreEqual(Outcome.Defeat, b.Outcome);
            Assert.IsNotNull(b.Validate(BattleAction.Guard(b.Enemies[0])), "actions refused after defeat");
        }
    }
}
