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

        // --- Player Poise (party-side Break) ---

        [Test]
        public void PartyMembersHavePoiseGaugeThatStunsOnBreak()
        {
            var b = NewBattle("skeleton");
            foreach (var p in b.Party)
            {
                Assert.AreEqual(Combatant.PlayerPoiseGauge, p.MaxBreakGauge, p.name);
                Assert.AreEqual(p.MaxBreakGauge, p.CurrentBreakGauge, p.name);
                Assert.AreEqual(1, p.BreakStunTurns, p.name);
                Assert.AreEqual(Combatant.PlayerBrokenDamageMultiplier, p.BreakDamageMultiplier, p.name);
            }
        }

        [Test]
        public void StaggerResistanceReducesPoiseDamageByPercent()
        {
            var b = NewBattle("warden");
            var knight = P(b, "Knight");
            Assert.Greater(knight.StaggerRes, 0);
            int expected = (int)Math.Round(30 * 100f / (100f + knight.StaggerRes));
            knight.ApplyBreakDamage(30, b.Enemies[0], out int reduced);
            Assert.AreEqual(expected, reduced);
            Assert.Less(reduced, 30);
        }

        [Test]
        public void IronResolveHalvesPoiseDamage()
        {
            var b = NewBattle("warden");
            var knight = P(b, "Knight");
            knight.IronResolveActive = true;
            int expected = (int)Math.Round(15 * 100f / (100f + knight.StaggerRes));
            knight.ApplyBreakDamage(30, b.Enemies[0], out int reduced);
            Assert.AreEqual(expected, reduced);
        }

        [Test]
        public void BrokenPlayerSkipsExactlyOneTurnThenActsWithFullPoise()
        {
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");
            knight.CurrentBreakGauge = 1;
            Assert.IsTrue(knight.ApplyBreakDamage(5, b.Enemies[0], out _));
            Assert.IsTrue(knight.IsBroken);
            Assert.AreEqual(1, knight.stunTurns);

            AdvanceTo(b, knight);
            Assert.AreEqual(1, b.Log.Count(l => l.Contains($"{knight.name} is stunned")), "exactly one skipped turn");
            Assert.IsFalse(knight.IsBroken);
            Assert.AreEqual(knight.MaxBreakGauge, knight.CurrentBreakGauge, "poise refills after Broken ends");
        }

        [Test]
        public void GoodDefensiveQteHalvesPoiseDamageAndPerfectParryDealsNone()
        {
            int PoiseLost(QteResult r, bool guard = false)
            {
                var b = NewBattle("warden");
                var knight = P(b, "Knight");
                knight.MaxBreakGauge = knight.CurrentBreakGauge = 10000; // never breaks, so the whole amount shows
                var warden = b.Enemies[0];
                AdvanceTo(b, warden);
                knight.guarding = guard;
                b.Perform(BattleAction.Attack(warden, knight), 1f, new[] { r });
                return 10000 - knight.CurrentBreakGauge;
            }
            int poor = PoiseLost(QteResult.Poor), good = PoiseLost(QteResult.Good);
            Assert.Greater(poor, 1);
            Assert.AreEqual(poor / 2f, good, 1f);
            Assert.AreEqual(0, PoiseLost(QteResult.Perfect, guard: true), "a full parry takes no Poise damage");
            Assert.LessOrEqual(PoiseLost(QteResult.Perfect), good, "a plain Perfect is never worse than Good");
        }

        [Test]
        public void FullParryNeedsGuardOrShield_PlainPerfectOnlyDeflects()
        {
            Hit Struck(QteResult r, Action<Combatant> setup)
            {
                var b = NewBattle("warden");
                var warden = b.Enemies[0];
                AdvanceTo(b, warden);
                var target = b.Party.First(p => !p.unit.HasEquippedShield());
                target.unit.hp = 9999;
                target.guarding = false; // AdvanceTo makes everyone Guard while waiting
                setup?.Invoke(target);
                return b.Perform(BattleAction.Attack(warden, target), 1f, new[] { r })[0];
            }
            var poor = Struck(QteResult.Poor, null);
            var plain = Struck(QteResult.Perfect, null);
            Assert.IsFalse(plain.blocked, "a Perfect alone is no longer a full parry");
            Assert.Greater(plain.amount, 0);
            Assert.AreEqual(poor.amount * QteConfig.PerfectDefense, plain.amount, 1.01f);

            Assert.IsTrue(Struck(QteResult.Perfect, t => t.guarding = true).blocked, "Guard + Perfect fully parries");
            var shielded = Struck(QteResult.Perfect, t => t.unit.Equip(Database.Equipment["EQ019"]));
            Assert.IsTrue(shielded.blocked, "a shield + Perfect fully parries");
            Assert.AreEqual(0, shielded.amount);
        }

        [Test]
        public void ReadyCounterRetaliatesOnlyOnDefensivePerfect()
        {
            int WardenHpLost(QteResult r, int counterTurns)
            {
                var b = NewBattle("warden");
                var knight = P(b, "Knight");
                knight.CounterTurns = counterTurns;
                var warden = b.Enemies[0];
                AdvanceTo(b, warden);
                int before = warden.unit.hp;
                b.Perform(BattleAction.Attack(warden, knight), 1f, new[] { r });
                return before - warden.unit.hp;
            }
            Assert.Greater(WardenHpLost(QteResult.Perfect, 1), 0, "a ready counter strikes back after a Perfect");
            Assert.AreEqual(0, WardenHpLost(QteResult.Good, 1), "Good never triggers a counter");
            Assert.AreEqual(0, WardenHpLost(QteResult.Perfect, 0), "no counter skill active, no counter");
            Assert.Greater(Database.Skills["counterattack"].counterMultiplier, 0f);
            Assert.Greater(Database.Skills["perfect_counter"].counterMultiplier, Database.Skills["counterstrike"].counterMultiplier);
        }

        [Test]
        public void OffensivePerfectPaysInBreakAndMp_NotMuchExtraDamage()
        {
            (int dmg, int brk, int mp) Hit(float power)
            {
                var b = NewBattle("warden");
                var knight = P(b, "Knight");
                var warden = b.Enemies[0];
                AdvanceTo(b, knight);
                warden.MaxBreakGauge = warden.CurrentBreakGauge = 100000; // never breaks, so the whole Break amount shows
                knight.unit.mp = 0;
                int hp = warden.unit.hp;
                b.Perform(BattleAction.Attack(knight, warden), power);
                return (hp - warden.unit.hp, 100000 - warden.CurrentBreakGauge, knight.unit.mp);
            }
            var perfect = Hit(QteConfig.PerfectPower);
            var good = Hit(QteConfig.GoodPower);
            var poor = Hit(QteConfig.PoorPower);

            Assert.Greater(perfect.dmg, good.dmg);
            Assert.LessOrEqual(perfect.dmg, (int)Math.Ceiling(good.dmg * 1.25f), "damage spread stays narrow");
            Assert.Greater(perfect.brk, good.brk, "Break keeps the wide spread");
            Assert.Greater(good.brk, poor.brk);
            Assert.AreEqual(QteConfig.PerfectMpRefund, perfect.mp, "Perfect refunds MP");
            Assert.AreEqual(0, good.mp);
        }

        [Test]
        public void CounterHitsAtLeastAsHardAsAGoodBasicAttack_AndScalesWithMultiplier()
        {
            var b = NewBattle("warden");
            var knight = P(b, "Knight");
            var warden = b.Enemies[0];
            int basicGood = Rules.Physical(knight, warden, 1f, null);
            int counter = QteConfig.CalculateCounterDamage(knight, warden, 1f, null);
            int perfectCounter = QteConfig.CalculateCounterDamage(knight, warden, 1.5f, null);
            Assert.GreaterOrEqual(counter, basicGood, "Counter Power only ever adds on top of a Good-equivalent strike");
            Assert.Greater(perfectCounter, counter);
        }

        [Test]
        public void MartialArtistCounterGrantsComboWithoutCountingAsAttacking()
        {
            var ma = new Unit(new CharacterDef { id = "ma", name = "Ma", raceId = "human", classId = "martial_artist", backgroundId = "guard", baseScores = AbilityScores.DefaultBase });
            var b = new Battle(new[] { ma }, new[] { Database.Enemies["warden"] }, new List<string>(), new Random(1));
            var hero = b.Party[0];
            var warden = b.Enemies[0];
            AdvanceTo(b, warden);

            hero.CounterTurns = 1;
            hero.CounterComboGain = Database.Skills["perfect_counter"].counterComboGain;
            hero.CurrentCombo = 3;
            hero.AttackedThisTurn = false;
            b.Perform(BattleAction.Attack(warden, hero), 1f, new[] { QteResult.Perfect });

            Assert.AreEqual(5, hero.CurrentCombo, "Perfect Counter grants +2 combo");
            Assert.IsFalse(hero.AttackedThisTurn, "a counter is not an attack on the Martial Artist's own turn");
            Assert.AreEqual(1, Database.Skills["counterstrike"].counterComboGain);

            AdvanceTo(b, warden);
            hero.CurrentCombo = Combatant.MaxComboNormal;
            hero.CounterTurns = 1;
            b.Perform(BattleAction.Attack(warden, hero), 1f, new[] { QteResult.Perfect });
            Assert.AreEqual(Combatant.MaxComboNormal, hero.CurrentCombo, "gain above the cap is lost");
        }

        [Test]
        public void SupportBuffsScaleWithCasterCha_OtherStatusesDoNot()
        {
            var b = NewBattle("warden");
            var caster = b.Party[0];
            var attackUp = new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.25f };
            var evasion = new StatusApply { id = StatusId.Evasion, turns = 2, magnitude = 1.5f };
            caster.unit.AddPermanentAbilityBonus(Ability.CHA, 20 - caster.unit.Score(Ability.CHA)); // CHA 20 -> +5 modifier

            Assert.Greater(Battle.SupportScaled(attackUp, caster).magnitude, 0.25f);
            Assert.LessOrEqual(Battle.SupportScaled(attackUp, caster).magnitude, 0.25f * 1.2001f, "capped by the CHA maxBonus");
            Assert.AreSame(evasion, Battle.SupportScaled(evasion, caster), "QTE window statuses stay data-driven");
        }
    }
}
