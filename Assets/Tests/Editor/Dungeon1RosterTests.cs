using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    // Dungeon 1 enemy roster (workbook v1.7): data, scaling, initiative, QTE, skills, AI, Zoroa phases, encounters.
    public class Dungeon1RosterTests
    {
        // ---- helpers ----

        static Battle NewBattle(int seed, params string[] enemies) => new Battle(
            Database.Party.Select(id => new Unit(Database.Characters[id])), enemies.Select(id => Database.Enemies[id]),
            new List<string>(Database.StartingItems), new Random(seed));

        // Everyone else guards until `who` is up.
        static Combatant AdvanceTo(Battle b, Func<Combatant, bool> who)
        {
            for (int i = 0; i < 200; i++)
            {
                var c = b.NextTurn();
                if (c != null && who(c)) return c;
                if (c != null) b.Perform(BattleAction.Guard(c));
            }
            throw new Exception("never reached");
        }

        static EnemyDef E(string id) => Database.Enemies[id];
        static EnemyAttack Move(string enemy, string name) => E(enemy).attacks.Single(a => a.name == name);

        static void NoResistance(Combatant c)
        {
            var r = c.StatusController.ResistanceProfile;
            r.baseResistance = r.equipmentResistance = r.statsResistance = r.buffResistance = r.passivesResistance = 0f;
        }

        static readonly string[] RosterIds =
        {
            "hollow_squire", "gravebound_archer", "cinder_wisp", "soulbound_hound", "possessed_royal_guard", "crypt_leech",
            "forgotten_militia", "soul_reaper", "royal_ossuary_knight", "zoroa_p1", "zoroa_p2",
        };

        // ---- enemy data ----

        // id, tier, HP, MP, ATK, DEF, MATK, MDEF, SPD, Break
        [TestCase("hollow_squire", Tier.Normal, 145, 35, 18, 8, 8, 6, 9, 100)]
        [TestCase("gravebound_archer", Tier.Normal, 112, 45, 20, 5, 10, 7, 13, 100)]
        [TestCase("cinder_wisp", Tier.Normal, 105, 70, 8, 4, 22, 8, 14, 100)]
        [TestCase("soulbound_hound", Tier.Normal, 155, 40, 17, 7, 9, 6, 16, 100)]
        [TestCase("possessed_royal_guard", Tier.Normal, 190, 30, 23, 12, 7, 9, 7, 100)]
        [TestCase("crypt_leech", Tier.Normal, 125, 55, 15, 5, 13, 7, 11, 100)]
        [TestCase("forgotten_militia", Tier.Normal, 135, 35, 16, 6, 8, 6, 12, 100)]
        [TestCase("soul_reaper", Tier.Elite, 430, 130, 32, 13, 29, 14, 17, 100)]
        [TestCase("royal_ossuary_knight", Tier.Elite, 520, 90, 36, 22, 18, 18, 9, 100)]
        [TestCase("zoroa_p1", Tier.Boss, 1840, 600, 38, 20, 42, 22, 14, 200)]
        [TestCase("zoroa_p2", Tier.Boss, 1840, 850, 43, 18, 50, 24, 17, 200)]
        public void RosterBaseStatsMatchWorkbook(string id, Tier tier, int hp, int mp, int atk, int def, int matk, int mdef, int spd, int brk)
        {
            var d = E(id);
            Assert.AreEqual(tier, d.tier);
            Assert.AreEqual(AiKind.Priority, d.ai);
            Assert.AreEqual(new Stats(hp, mp, atk, def, spd, matk), d.stats);
            Assert.AreEqual(mdef, d.magicDef);
            Assert.AreEqual(brk, d.maxBreakGauge);

            var c = new Combatant(new Unit(d), Side.Enemy, 0, d.shortName, d.tag, d);
            Assert.AreEqual(hp, c.unit.hp);
            Assert.AreEqual(mp, c.unit.mp, "starts with full MP");
            Assert.AreEqual(mdef, c.MagicDef);
            Assert.AreEqual(spd, c.Initiative);
            Assert.AreEqual(brk, c.MaxBreakGauge);
            Assert.IsNotNull(d.sprite, "visual reference");
        }

        [Test]
        public void RosterIdsAreUniqueAndZoroaPhasesAreSeparateDefinitions()
        {
            Assert.AreEqual(RosterIds.Length, Dungeon1Roster.Enemies.Length);
            CollectionAssert.AreEquivalent(RosterIds, Dungeon1Roster.Enemies.Select(e => e.id));
            CollectionAssert.AllItemsAreUnique(Dungeon1Roster.Enemies.Select(e => e.id));
            foreach (var id in RosterIds) Assert.IsTrue(Database.Enemies.ContainsKey(id), id);
            Assert.AreNotSame(E("zoroa_p1"), E("zoroa_p2"));
            Assert.AreEqual("zoroa_p2", E("zoroa_p1").nextPhase);
            Assert.IsNull(E("zoroa_p2").nextPhase);
        }

        // ---- skill database ----

        // enemy, skill, element, all targets, damage per hit (empty = status-only), MP, Heavy hit (-1 none), telegraph, status, chance
        static IEnumerable<TestCaseData> Skills()
        {
            TestCaseData S(string enemy, string skill, Element el, bool all, int[] dmg, int mp, int heavy, bool tele, StatusId? st = null, float chance = 0f) =>
                new TestCaseData(enemy, skill, el, all, dmg, mp, heavy, tele, st, chance).SetName($"Skill_{enemy}_{skill.Replace(' ', '_')}");
            yield return S("hollow_squire", "Rusty Cleave", Element.Physical, false, new[] { 18 }, 0, -1, false);
            yield return S("gravebound_archer", "Grave Arrow", Element.Physical, false, new[] { 20 }, 0, -1, false);
            yield return S("cinder_wisp", "Ember Bolt", Element.Fire, false, new[] { 22 }, 8, -1, false, StatusId.Burn, 0.20f);
            yield return S("cinder_wisp", "Basic Attack", Element.Physical, false, new[] { 8 }, 0, -1, false);
            yield return S("soulbound_hound", "Rending Bite", Element.Physical, false, new[] { 17 }, 0, -1, false);
            yield return S("soulbound_hound", "Soul Snarl", Element.Shadow, false, new[] { 12 }, 10, -1, false, StatusId.Fear, 0.20f);
            yield return S("possessed_royal_guard", "Royal Halberd", Element.Physical, false, new[] { 23 }, 0, -1, false);
            yield return S("crypt_leech", "Vital Siphon", Element.Shadow, false, new[] { 15 }, 7, -1, false);
            yield return S("crypt_leech", "Basic Attack", Element.Physical, false, new[] { 15 }, 0, -1, false);
            yield return S("forgotten_militia", "Two-Step Slash", Element.Physical, false, new[] { 10, 10 }, 0, -1, false);
            yield return S("soul_reaper", "Harvesting Arc", Element.Physical, false, new[] { 27 }, 0, -1, false);
            yield return S("soul_reaper", "Accelerating Rend", Element.Physical, false, new[] { 15, 15, 15, 15 }, 16, -1, false, StatusId.Bleed, 0.25f);
            yield return S("soul_reaper", "Soul Brand", Element.Shadow, false, new[] { 20 }, 20, -1, false, StatusId.Despair, 0.20f);
            yield return S("royal_ossuary_knight", "Ossuary Slam", Element.Physical, false, new[] { 32 }, 0, -1, false);
            yield return S("royal_ossuary_knight", "Graveward Charge", Element.Physical, false, new[] { 38 }, 18, 0, true);
            yield return S("zoroa_p1", "Soul Rend", Element.Physical, false, new[] { 38 }, 0, -1, false);
            yield return S("zoroa_p1", "Soul Lance", Element.Shadow, false, new[] { 42 }, 18, -1, false);
            yield return S("zoroa_p1", "Soulbind", Element.Shadow, false, new int[0], 24, -1, true, StatusId.Silence, 0.20f);
            yield return S("zoroa_p1", "Final Reaping", Element.Physical, false, new[] { 30, 30, 30, 45 }, 35, 3, true);
            yield return S("zoroa_p2", "Soul Rend", Element.Physical, false, new[] { 43 }, 0, -1, false);
            yield return S("zoroa_p2", "Soul Lance", Element.Shadow, false, new[] { 50 }, 22, -1, false);
            yield return S("zoroa_p2", "Soulbind", Element.Shadow, false, new int[0], 28, -1, true, StatusId.Silence, 0.25f);
            yield return S("zoroa_p2", "Final Reaping", Element.Physical, false, new[] { 34, 34, 34, 50 }, 40, 3, true);
            yield return S("zoroa_p2", "Soulstorm", Element.Shadow, true, new[] { 24 }, 45, -1, true);
        }

        [TestCaseSource(nameof(Skills))]
        public void SkillMatchesWorkbook(string enemy, string skill, Element element, bool all, int[] damage, int mp, int heavy, bool telegraph, StatusId? status, float chance)
        {
            var d = E(enemy);
            var m = Move(enemy, skill);
            Assert.AreEqual(element, m.element);
            Assert.AreEqual(all, m.allTargets);
            Assert.AreEqual(damage.Length, m.hits, "hit count");
            Assert.AreEqual(mp, m.mpCost);
            Assert.AreEqual(heavy, m.heavyHit);
            Assert.IsFalse(m.isHeavy, "no all-hits Heavy in the roster");
            Assert.AreEqual(telegraph, m.telegraph);
            Assert.IsFalse(m.self);
            if (damage.Length == 0) Assert.IsTrue(m.StatusOnly);
            else CollectionAssert.AreEqual(damage, m.hitDamage);

            // The coefficient maps the authored D1 damage onto the D1 ATK / MATK (no extra scaling layer).
            float stat = m.UsesMagic ? d.stats.mag : d.stats.atk;
            for (int i = 0; i < damage.Length; i++) Assert.AreEqual(damage[i], stat * m.PowerAt(i), 1e-3, $"hit {i + 1}");

            if (status == null) Assert.IsTrue(m.apply == null || m.apply.Length == 0, "no status");
            else
            {
                Assert.AreEqual(1, m.apply.Length);
                Assert.AreEqual(status.Value, m.apply[0].id);
                Assert.AreEqual(chance, m.apply[0].chance, 1e-6);
            }
        }

        [Test]
        public void EveryRosterSkillIsListedAndNothingExtraExists()
        {
            int arcane = Dungeon1Roster.Enemies.Sum(e => e.attacks.Count(a => a.name == "Arcane Charge"));
            Assert.AreEqual(Skills().Count() + arcane, Dungeon1Roster.Enemies.Sum(e => e.attacks.Length), "no undocumented enemy attacks");
            Assert.IsFalse(Dungeon1Roster.Enemies.SelectMany(e => e.attacks).Any(a => a.apply != null && a.apply.Any(ap => ap.id == StatusId.Stun)),
                "no roster attack applies Stun, so Stun + Heavy on one target cannot happen");
        }

        [Test]
        public void AttackPatternsFollowTierRules()
        {
            foreach (var e in Dungeon1Roster.Enemies.Where(e => e.tier == Tier.Normal))
                foreach (var a in e.attacks)
                {
                    Assert.IsNull(a.intervals, $"{a.name}: Normal enemies are Steady only");
                    Assert.IsFalse(a.HasHeavy, $"{a.name}: Normal enemies have no Heavy");
                    Assert.IsFalse(a.telegraph, a.name);
                }
            Assert.AreEqual(2, Move("forgotten_militia", "Two-Step Slash").hits);
            CollectionAssert.AreEqual(new[] { 0.350f, 0.310f, 0.310f }, Move("soul_reaper", "Accelerating Rend").intervals);
            Assert.IsTrue(Move("soul_reaper", "Accelerating Rend").intervals.All(i => i >= 0.310f - 1e-6f), "Elite minimum 310 ms");
            Assert.IsFalse(E("soul_reaper").attacks.Any(a => a.HasHeavy), "Soul Reaper has no Heavy marker");
            Assert.LessOrEqual(E("soul_reaper").attacks.Where(a => a.hits > 1).Select(a => a.intervals == null ? "Steady" : "Accelerando").Distinct().Count(), 2);
            foreach (var id in new[] { "zoroa_p1", "zoroa_p2" })
            {
                var fr = Move(id, "Final Reaping");
                CollectionAssert.AreEqual(new[] { 0.350f, 0.298f, 0.280f }, fr.intervals);
                Assert.IsTrue(fr.intervals.All(i => i >= 0.280f - 1e-6f), "Boss minimum 280 ms");
                CollectionAssert.AreEqual(new[] { false, false, false, true }, Enumerable.Range(0, 4).Select(fr.HeavyAt), "Heavy on hit 4 only");
                Assert.AreEqual(1, E(id).attacks.Count(a => a.HasHeavy), "exactly one Heavy-marked attack");
            }
        }

        [Test]
        public void PhaseTwoAddsExactlyOneNewAttack_Soulstorm()
        {
            var p1 = E("zoroa_p1").attacks.Select(a => a.name).ToList();
            var p2 = E("zoroa_p2").attacks.Select(a => a.name).ToList();
            CollectionAssert.DoesNotContain(p1, "Soulstorm");
            CollectionAssert.AreEqual(new[] { "Soulstorm" }, p2.Except(p1).ToArray());
            CollectionAssert.IsSubsetOf(p1, p2, "Phase 2 keeps every Phase 1 attack");
            Assert.LessOrEqual(Move("zoroa_p2", "Soulstorm").hitDamage[0], Move("zoroa_p1", "Soul Lance").hitDamage[0] * 0.60f, "multi-target coefficient cap");
        }

        // ---- scaling ----

        static readonly Dictionary<Tier, decimal[]> Rates = new Dictionary<Tier, decimal[]>
        {
            // HP, MP, ATK, MATK, DEF, MDEF, SPD
            [Tier.Normal] = new[] { 0.12m, 0.06m, 0.08m, 0.08m, 0.05m, 0.05m, 0.02m },
            [Tier.Elite] = new[] { 0.13m, 0.08m, 0.09m, 0.09m, 0.055m, 0.055m, 0.03m },
            [Tier.Boss] = new[] { 0.14m, 0.10m, 0.10m, 0.10m, 0.06m, 0.06m, 0.04m },
        };

        static int Expected(int b, Tier t, int stat, int dungeon) => (int)Math.Floor(b * (1m + Rates[t][stat] * (dungeon - 1)));

        [Test]
        public void ScalingUsesDungeonOneBaseTierRatesAndOneFloor([Values(1, 2, 3, 4, 5, 6, 7, 8)] int dungeon)
        {
            foreach (var d in Dungeon1Roster.Enemies)
            {
                var u = new Unit(d, dungeon);
                var s = d.stats;
                Assert.AreEqual(Expected(s.maxHp, d.tier, 0, dungeon), u.stats.maxHp, $"{d.id} HP D{dungeon}");
                Assert.AreEqual(Expected(s.maxMp, d.tier, 1, dungeon), u.stats.maxMp, $"{d.id} MP");
                Assert.AreEqual(Expected(s.atk, d.tier, 2, dungeon), u.stats.atk, $"{d.id} ATK");
                Assert.AreEqual(Expected(s.mag, d.tier, 3, dungeon), u.stats.mag, $"{d.id} MATK");
                Assert.AreEqual(Expected(s.def, d.tier, 4, dungeon), u.stats.def, $"{d.id} DEF");
                Assert.AreEqual(Expected(d.magicDef, d.tier, 5, dungeon), u.Derived.magicDefense, $"{d.id} MDEF");
                Assert.AreEqual(Expected(s.spd, d.tier, 6, dungeon), u.stats.spd, $"{d.id} SPD");
                Assert.AreEqual(u.stats.maxHp, u.hp);
                Assert.AreEqual(u.stats.maxMp, u.mp);

                var c = new Combatant(u, Side.Enemy, 0, d.shortName, d.tag, d);
                Assert.AreEqual(d.maxBreakGauge, c.MaxBreakGauge, "Break Gauge never scales");
            }
        }

        [Test]
        public void ScalingNeverChainsAndMatchesWorkbookZoroaDungeon8()
        {
            var p1 = new Unit(E("zoroa_p1"), 8);
            Assert.AreEqual(new Stats(3643, 1020, 64, 28, 17, 71), p1.stats);
            Assert.AreEqual(31, p1.Derived.magicDefense);
            var p2 = new Unit(E("zoroa_p2"), 8);
            Assert.AreEqual(new Stats(3643, 1445, 73, 25, 21, 85), p2.stats);
            Assert.AreEqual(34, p2.Derived.magicDefense);

            // Exact-decimal floor: 125 x (1 + 0.12 x 5) is exactly 200 (float math gave 199).
            Assert.AreEqual(200, new Unit(E("crypt_leech"), 6).stats.maxHp);
            // Each dungeon is computed from the D1 base, never from the previous dungeon's rounded value.
            for (int n = 2; n <= 8; n++)
                Assert.AreEqual((int)Math.Floor(145 * (1m + 0.12m * (n - 1))), new Unit(E("hollow_squire"), n).stats.maxHp, $"D{n}");
        }

        [Test]
        public void ScaledDamageComesOnlyFromScaledAtkNotFromTheSkill()
        {
            var d = E("hollow_squire");
            var move = Move("hollow_squire", "Rusty Cleave");
            var c8 = new Combatant(new Unit(d, 8), Side.Enemy, 0, "Squire", "HSQ", d);
            Assert.AreEqual(Unit.ScaleStat(18, Tier.Normal, Unit.ScaledStat.ATK, 8), c8.Atk * move.PowerAt(0), 1e-3);
            Assert.AreEqual(18, move.hitDamage[0], "authored damage is never rewritten");
            Assert.AreEqual(1f, move.power, 1e-6, "skill coefficient is not scaled");
            Assert.AreEqual(8, Move("cinder_wisp", "Ember Bolt").mpCost, "MP cost does not scale");
        }

        // ---- initiative ----

        [Test]
        public void QueueSortsBySpeedPartyWinsTiesThenSlotThenSpawnOrder()
        {
            var b = NewBattle(1, "hollow_squire", "hollow_squire"); // both SPD 9
            int[] spd = { 9, 20, 9, 5 };
            for (int i = 0; i < 4; i++) b.Party[i].unit.stats.spd = spd[i];
            CollectionAssert.AreEqual(new[] { b.Party[1], b.Party[0], b.Party[2], b.Enemies[0], b.Enemies[1], b.Party[3] }, b.NextRoundOrder);
            Assert.AreEqual(0, b.Enemies[0].slot);
            Assert.AreEqual(1, b.Enemies[1].slot);
        }

        [Test]
        public void SpeedChangesMidRoundWaitForNextRoundAndNeverGrantExtraTurns()
        {
            var b = NewBattle(2, "soulbound_hound"); // SPD 16
            foreach (var p in b.Party) p.unit.stats.spd = 1;
            var hound = b.Enemies[0];
            var first = b.NextTurn();
            Assert.AreEqual(hound, first);
            var queueBefore = b.Upcoming.ToList();
            b.Party[3].unit.stats.spd = 99; // mid-round change
            CollectionAssert.AreEqual(queueBefore, b.Upcoming.ToList(), "active queue is not reordered");
            Assert.AreEqual(b.Party[3], b.NextRoundOrder.First(), "applies from the next round");

            var turns = new Dictionary<Combatant, int> { [first] = 1 };
            b.Perform(BattleAction.Guard(first));
            int round = b.Round;
            while (true)
            {
                var c = b.NextTurn();
                if (b.Round != round) break;
                turns[c] = turns.TryGetValue(c, out var n) ? n + 1 : 1;
                b.Perform(BattleAction.Guard(c));
            }
            Assert.AreEqual(5, turns.Count);
            Assert.IsTrue(turns.Values.All(n => n == 1), "one turn per unit per round");
        }

        [Test]
        public void RevivedUnitsRejoinOnlyNextRound()
        {
            var b = NewBattle(3, "hollow_squire");
            foreach (var p in b.Party) p.unit.stats.spd = 1;
            var slowest = b.Party[3];
            var squire = b.NextTurn(); // SPD 9 acts first
            Assert.AreEqual(b.Enemies[0], squire);
            b.Damage(slowest, 9999);
            slowest.unit.hp = 10; // revived before its queue position
            b.Perform(BattleAction.Guard(squire));
            int round = b.Round;
            var actedThisRound = new List<Combatant>();
            Combatant c;
            while ((c = b.NextTurn()) != null && b.Round == round) { actedThisRound.Add(c); b.Perform(BattleAction.Guard(c)); }
            CollectionAssert.DoesNotContain(actedThisRound, slowest);
            Assert.AreEqual(3, actedThisRound.Count);
            Assert.IsTrue(b.Upcoming.Contains(slowest) || c == slowest, "back in the next round");
        }

        // ---- QTE ----

        [Test]
        public void DefensiveQteUsesTheAttackersTierProfile()
        {
            foreach (var (id, tier) in new[] { ("hollow_squire", Tier.Normal), ("soul_reaper", Tier.Elite), ("zoroa_p1", Tier.Boss) })
            {
                var b = NewBattle(1, id);
                var e = b.Enemies[0];
                var move = e.enemy.attacks.First(m => !m.StatusOnly);
                var q = (DefenseQte)QteFactory.Defense(BattleAction.Strike(e, move, b.Party[2]), QteAssist.Standard);
                Assert.AreSame(QteConfig.Profiles[tier], q.profile, id);
            }
        }

        [Test]
        public void MultiHitAttacksGetOneMarkerPerHitWithTheirIntervals()
        {
            DefenseQte Q(string enemy, string move)
            {
                var b = NewBattle(1, enemy);
                return (DefenseQte)QteFactory.Defense(BattleAction.Strike(b.Enemies[0], Move(enemy, move), b.Party[2]), QteAssist.Standard);
            }
            float[] Gaps(DefenseQte q) => Enumerable.Range(1, q.Hits - 1).Select(i => (float)Math.Round(q.impacts[i] - q.impacts[i - 1], 3)).ToArray();

            var militia = Q("forgotten_militia", "Two-Step Slash");
            Assert.AreEqual(2, militia.Hits);
            CollectionAssert.AreEqual(new[] { 0.35f }, Gaps(militia), "Steady 350 ms");

            var rend = Q("soul_reaper", "Accelerating Rend");
            Assert.AreEqual(4, rend.Hits);
            CollectionAssert.AreEqual(new[] { 0.35f, 0.31f, 0.31f }, Gaps(rend));
            Assert.IsFalse(rend.heavy.Any(h => h));

            var reaping = Q("zoroa_p1", "Final Reaping");
            Assert.AreEqual(4, reaping.Hits);
            CollectionAssert.AreEqual(new[] { 0.35f, 0.298f, 0.28f }, Gaps(reaping));
            CollectionAssert.AreEqual(new[] { false, false, false, true }, reaping.heavy);

            var auto = (AutoQte)QteFactory.Defense(BattleAction.Strike(NewBattle(1, "zoroa_p1").Enemies[0], Move("zoroa_p1", "Final Reaping"), null), QteAssist.Auto);
            Assert.AreEqual(4, auto.Results.Count);
        }

        [Test]
        public void StatusOnlyAndChargeTurnsHaveNoDefensiveQte()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            Assert.IsNull(QteFactory.Defense(BattleAction.Strike(z, Move("zoroa_p1", "Soulbind"), b.Party[0]), QteAssist.Standard));
            Assert.IsNull(QteFactory.Defense(BattleAction.Strike(z, Move("zoroa_p1", "Soulbind"), b.Party[0]), QteAssist.Auto));
            Assert.IsNull(QteFactory.Defense(BattleAction.Charge(z, Move("zoroa_p1", "Final Reaping"), b.Party[0]), QteAssist.Standard));
        }

        static List<Hit> FinalReaping(QteResult reaction, int seed)
        {
            var b = NewBattle(seed, "zoroa_p1");
            var z = AdvanceTo(b, c => c.IsEnemy);
            b.Party[0].unit.hp = 9999;
            return b.Perform(BattleAction.Strike(z, Move("zoroa_p1", "Final Reaping"), b.Party[0]), 1f, Enumerable.Repeat(reaction, 4).ToList());
        }

        [Test]
        public void PerfectOnlyHalvesTheHeavyFourthHit()
        {
            var perfect = FinalReaping(QteResult.Perfect, 5);
            var poor = FinalReaping(QteResult.Poor, 5);
            Assert.AreEqual(4, perfect.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(perfect[i].blocked, $"hit {i + 1} fully parried");
                Assert.AreEqual(0, perfect[i].amount);
            }
            Assert.IsFalse(perfect[3].blocked, "Heavy is never fully parried");
            Assert.Greater(perfect[3].amount, 0);
            Assert.AreEqual(poor[3].amount * 0.5f, perfect[3].amount, 1.01f, "Heavy Perfect = 50% reduction");
        }

        // ---- skills ----

        [Test]
        public void ElementalMovesUseMatkVersusMdef()
        {
            var b = NewBattle(1, "cinder_wisp");
            var wisp = b.Enemies[0];
            var mage = b.Party[2];
            var bolt = Move("cinder_wisp", "Ember Bolt");
            Assert.AreEqual(22, wisp.Magic);
            Assert.Greater(Rules.Magic(wisp, mage, bolt.PowerAt(0), null), Rules.Physical(wisp, mage, bolt.PowerAt(0), null), "MATK 22, not ATK 8");
            Assert.IsTrue(bolt.UsesMagic);
            Assert.IsFalse(Move("hollow_squire", "Rusty Cleave").UsesMagic);
        }

        [Test]
        public void MultiHitStatusRollsOncePerAttackNotPerHit()
        {
            var b = NewBattle(1, "soul_reaper");
            var reaper = AdvanceTo(b, c => c.IsEnemy);
            var target = b.Party[0];
            NoResistance(target);
            var rend = Move("soul_reaper", "Accelerating Rend");
            var sure = new EnemyAttack { name = rend.name, hits = 4, hitPower = rend.hitPower, power = rend.power, intervals = rend.intervals,
                apply = new[] { new StatusApply { id = StatusId.Bleed, chance = 1f } } };
            target.unit.hp = 9999;
            b.Perform(BattleAction.Strike(reaper, sure, target), 1f, Enumerable.Repeat(QteResult.Poor, 4).ToList());
            Assert.AreEqual(1, target.StatusController.CountStatus(StatusId.Bleed), "one roll for four hits");
        }

        [Test]
        public void EnemyMpIsSpentAndNeverRegenerates()
        {
            var b = NewBattle(1, "cinder_wisp");
            var wisp = AdvanceTo(b, c => c.IsEnemy);
            b.Perform(BattleAction.Strike(wisp, Move("cinder_wisp", "Ember Bolt"), b.Party[0]));
            Assert.AreEqual(62, wisp.unit.mp);
            AdvanceTo(b, c => c == wisp);
            Assert.AreEqual(62, wisp.unit.mp, "no regeneration across turns");
            wisp.unit.mp = 7;
            Assert.AreEqual("Not enough MP.", b.Validate(BattleAction.Strike(wisp, Move("cinder_wisp", "Ember Bolt"), b.Party[0])));
        }

        [Test]
        public void EveryEnemyHasAFreeBasicAttackAndNeverSkipsForLackOfMp([ValueSource(nameof(RosterIds))] string id)
        {
            var d = E(id);
            Assert.IsTrue(d.attacks.Any(a => a.IsBasicAttack), $"{id} needs a 0-MP Basic Attack");
            Assert.IsTrue(d.attacks.Where(a => a.mpCost > 0).All(a => !a.IsBasicAttack), "MP is consumed only by Skills");

            var b = NewBattle(1, id);
            var e = b.Enemies[0];
            e.unit.mp = 0;
            AdvanceTo(b, x => x == e); // reached: never skipped
            var act = EnemyAI.Choose(b, e);
            Assert.IsNotNull(act);
            Assert.IsNull(b.Validate(act), "the chosen action is affordable");
            Assert.AreEqual(0, act.attack.mpCost);
            Assert.IsFalse(b.Log.Any(l => l.Contains("has no usable action")));
        }

        [Test]
        public void SkillsNeedMpButTheBasicAttackRemains()
        {
            var b = NewBattle(1, "cinder_wisp");
            var wisp = AdvanceTo(b, c => c.IsEnemy);
            wisp.unit.mp = 7; // one short of Ember Bolt
            Assert.AreEqual("Not enough MP.", b.Validate(BattleAction.Strike(wisp, Move("cinder_wisp", "Ember Bolt"), b.Party[0])));
            var act = EnemyAI.Choose(b, wisp);
            Assert.AreEqual("Basic Attack", act.attack.name);
            b.Perform(act);
            Assert.AreEqual(7, wisp.unit.mp, "the Basic Attack costs nothing");
        }

        // ---- Arcane Charge (MP restoration) ----

        [Test]
        public void ArcaneChargeIsAssignedOnlyToDesignatedEnemies()
        {
            var holders = Dungeon1Roster.Enemies.Where(e => e.attacks.Any(a => a.name == "Arcane Charge")).Select(e => e.id).ToList();
            CollectionAssert.AreEquivalent(new[] { "soul_reaper", "royal_ossuary_knight", "zoroa_p1", "zoroa_p2" }, holders);
            var instances = Dungeon1Roster.Enemies.SelectMany(e => e.attacks).Where(a => a.name == "Arcane Charge").ToList();
            CollectionAssert.AllItemsAreUnique(instances, "configured per enemy, never shared");
            foreach (var a in instances)
            {
                Assert.IsTrue(a.self);
                Assert.AreEqual(0, a.hits, "no damage");
                Assert.AreEqual(0, a.mpCost);
                Assert.AreEqual(0.50f, a.restoreMp, 1e-6);
                Assert.AreEqual(4, a.cooldown);
                Assert.IsTrue(a.apply == null || a.apply.Length == 0);
                Assert.IsFalse(a.IsBasicAttack);
            }
        }

        [Test]
        public void ArcaneChargeRestoresHalfMaxMpCappedAndHasNoQte()
        {
            var b = NewBattle(1, "soul_reaper");
            var reaper = AdvanceTo(b, c => c.IsEnemy);
            var charge = Move("soul_reaper", "Arcane Charge");
            var action = BattleAction.Strike(reaper, charge, null);
            Assert.IsNull(QteFactory.Defense(action, QteAssist.Standard), "no defensive QTE");
            Assert.IsNull(QteFactory.Defense(action, QteAssist.Auto));

            reaper.unit.mp = 10;
            int partyHp = b.Party.Sum(p => p.unit.hp);
            var hits = b.Perform(action);
            Assert.IsEmpty(hits, "no damage");
            Assert.AreEqual(partyHp, b.Party.Sum(p => p.unit.hp));
            Assert.AreEqual(10 + 65, reaper.unit.mp, "50% of 130 Max MP");
            Assert.IsNull(b.Active, "consumes the enemy's turn");
            Assert.AreEqual(4, reaper.Cooldown(Combatant.MoveKey(charge)));

            var b2 = NewBattle(1, "soul_reaper");
            var r2 = AdvanceTo(b2, c => c.IsEnemy);
            r2.unit.mp = 100;
            b2.Perform(BattleAction.Strike(r2, Move("soul_reaper", "Arcane Charge"), null));
            Assert.AreEqual(130, r2.unit.mp, "never above Max MP");
        }

        [Test]
        public void ArcaneChargeAiUsesItOnlyWhenLowAndRespectsTheFourTurnCooldown()
        {
            var b = NewBattle(1, "soul_reaper");
            var reaper = b.Enemies[0];
            AdvanceTo(b, c => c == reaper);
            Assert.AreNotEqual("Arcane Charge", EnemyAI.Choose(b, reaper).attack.name, "full MP: no restoration");
            b.Perform(BattleAction.Guard(reaper));
            reaper.unit.mp = 0; // kept starved: only Arcane Charge or the free Basic Attack are possible

            var picks = new List<string>();
            for (int i = 0; i < 300 && picks.Count < 6; i++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c == reaper)
                {
                    var act = EnemyAI.Choose(b, reaper); // planned at round start with 0 MP
                    picks.Add(act.attack.name);
                    b.Perform(act, 1f, Enumerable.Repeat(QteResult.Poor, Math.Max(1, act.attack.hits)).ToList());
                    reaper.unit.mp = 0;
                }
                else { c.unit.hp = c.Stats.maxHp; b.Perform(BattleAction.Guard(c)); }
            }
            Assert.AreEqual("Arcane Charge", picks[0]);
            CollectionAssert.AreEqual(Enumerable.Repeat("Harvesting Arc", 4).ToList(), picks.Skip(1).Take(4).ToList(), "cooldown 4 turns; the Basic Attack fills in");
            Assert.AreEqual("Arcane Charge", picks[5], "available again on the fifth turn after");
        }

        [Test]
        public void ZoroaCarriesArcaneChargeIntoPhaseTwo()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            b.Damage(z, 1840);
            Assert.AreEqual(2, z.phase);
            Assert.IsTrue(EnemyAI.Moves(z).Any(m => m.name == "Arcane Charge"));
            z.unit.mp = 100;
            AdvanceTo(b, c => c == z);
            EnemyAI.Plan(b, z);
            var act = EnemyAI.Choose(b, z);
            Assert.AreEqual("Arcane Charge", act.attack.name, "below half of 850");
            b.Perform(act);
            Assert.AreEqual(100 + 425, z.unit.mp);
        }

        [Test]
        public void VitalSiphonHealsThirtyPercentOfFinalDamageDealt()
        {
            var b = NewBattle(4, "crypt_leech");
            var leech = AdvanceTo(b, c => c.IsEnemy);
            leech.unit.hp = 50;
            var hits = b.Perform(BattleAction.Strike(leech, Move("crypt_leech", "Vital Siphon"), b.Party[2]), 1f, new[] { QteResult.Poor });
            int dealt = hits.Where(h => !h.heal).Sum(h => h.amount);
            Assert.Greater(dealt, 0);
            Assert.AreEqual(50 + (int)Math.Floor(dealt * 0.30f), leech.unit.hp);

            var b2 = NewBattle(4, "crypt_leech");
            var leech2 = AdvanceTo(b2, c => c.IsEnemy);
            leech2.unit.hp = 50;
            b2.Perform(BattleAction.Strike(leech2, Move("crypt_leech", "Vital Siphon"), b2.Party[2]), 1f, new[] { QteResult.Perfect });
            Assert.AreEqual(50, leech2.unit.hp, "nothing dealt, nothing healed");
        }

        [Test]
        public void SoulstormHitsAllFourPartyMembersWithOneSharedReaction()
        {
            var b = NewBattle(1, "zoroa_p2");
            var z = AdvanceTo(b, c => c.IsEnemy);
            var hits = b.Perform(BattleAction.Strike(z, Move("zoroa_p2", "Soulstorm"), null), 1f, new[] { QteResult.Perfect });
            CollectionAssert.AreEquivalent(b.Party, hits.Select(h => h.target));
            Assert.IsTrue(hits.All(h => h.blocked), "one shared QTE result for every target");
            Assert.AreEqual(850 - 45, z.unit.mp);
        }

        // ---- charge / Break ----

        [Test]
        public void OssuaryKnightTelegraphsChargesThenReleasesOneHeavyHit()
        {
            var b = NewBattle(1, "royal_ossuary_knight");
            var knight = b.Enemies[0];
            AdvanceTo(b, c => c == knight);
            Assert.AreEqual("Ossuary Slam", knight.Intent.move.name, "round 1: normal pressure (charge on initial cooldown)");
            b.Perform(EnemyAI.Choose(b, knight), 1f, new[] { QteResult.Poor });

            AdvanceTo(b, c => c == knight);
            Assert.AreEqual(2, b.Round);
            Assert.AreEqual("Graveward Charge", knight.Intent.move.name);
            Assert.IsTrue(knight.Intent.charging);
            StringAssert.Contains("CHARGING: GRAVEWARD CHARGE HEAVY", EnemyAI.IntentText(knight));
            var charge = EnemyAI.Choose(b, knight);
            Assert.IsTrue(charge.charging);
            var target = charge.target;
            int hpBefore = b.Party.Sum(p => p.unit.hp), mpBefore = knight.unit.mp;
            Assert.IsEmpty(b.Perform(charge), "the charge turn deals no damage");
            Assert.AreEqual(hpBefore, b.Party.Sum(p => p.unit.hp));
            Assert.AreEqual(mpBefore, knight.unit.mp, "MP is paid on release");
            StringAssert.Contains("NEXT TURN", EnemyAI.IntentText(knight));

            AdvanceTo(b, c => c == knight);
            StringAssert.StartsWith("RELEASE: GRAVEWARD CHARGE", EnemyAI.IntentText(knight));
            var release = EnemyAI.Choose(b, knight);
            Assert.IsFalse(release.charging);
            Assert.AreEqual(target, release.target, "the telegraphed target");
            target.unit.hp = 9999;
            var hits = b.Perform(release, 1f, new[] { QteResult.Perfect });
            Assert.AreEqual(1, hits.Count);
            Assert.IsFalse(hits[0].blocked, "Heavy: Perfect only halves");
            Assert.AreEqual(mpBefore - 18, knight.unit.mp);
            Assert.IsNull(knight.Charge);

            AdvanceTo(b, c => c == knight);
            Assert.AreEqual("Ossuary Slam", knight.Intent.move.name, "no back-to-back charge");
        }

        [Test]
        public void BreakingAChargingKnightCancelsTheAttack()
        {
            var b = NewBattle(1, "royal_ossuary_knight");
            var knight = b.Enemies[0];
            AdvanceTo(b, c => c == knight);
            b.Perform(BattleAction.Charge(knight, Move("royal_ossuary_knight", "Graveward Charge"), b.Party[0]));
            Assert.IsTrue(knight.IsCharging);

            var rogue = AdvanceTo(b, c => c.name == "Rogue");
            knight.CurrentBreakGauge = 1;
            b.Perform(BattleAction.Attack(rogue, knight));
            Assert.IsTrue(knight.IsBroken);
            Assert.IsFalse(knight.IsCharging);
            Assert.IsNull(EnemyAI.IntentText(knight), "telegraph removed");
            Assert.IsTrue(b.Log.Any(l => l.Contains("charge is interrupted")));

            // The Broken knight loses its next turn; its first action afterwards is never the cancelled release.
            for (int i = 0; i < 40; i++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c == knight)
                {
                    var act = EnemyAI.Choose(b, knight);
                    Assert.IsFalse(act.attack.name == "Graveward Charge" && !act.charging, "the cancelled charge is never released");
                    return;
                }
                b.Perform(BattleAction.Guard(c));
            }
            Assert.Fail("the knight never acted again");
        }

        [Test]
        public void AtMostOneHeavyAttackIsPlannedPerRound()
        {
            var b = NewBattle(1, "royal_ossuary_knight", "royal_ossuary_knight");
            foreach (var k in b.Enemies) k.cooldowns.Clear();
            foreach (var k in b.Enemies) EnemyAI.Plan(b, k);
            Assert.AreEqual(1, b.Enemies.Count(k => k.Intent.move.HasHeavy));
        }

        [Test]
        public void TelegraphedTargetThatFallsIsRedirectedInPartyOrder()
        {
            var b = NewBattle(1, "royal_ossuary_knight");
            var knight = b.Enemies[0];
            AdvanceTo(b, c => c == knight);
            b.Perform(BattleAction.Charge(knight, Move("royal_ossuary_knight", "Graveward Charge"), b.Party[1]));
            b.Party[1].unit.hp = 0;
            AdvanceTo(b, c => c == knight);
            Assert.AreEqual(b.Party[2], EnemyAI.Choose(b, knight).target);
        }

        // ---- Zoroa ----

        [Test]
        public void ZoroaHasTwoSequentialHealthBars()
        {
            var b = NewBattle(1, "zoroa_p1");
            Assert.AreEqual(1, b.Enemies.Count);
            var z = b.Enemies[0];
            Assert.AreEqual(1, z.phase);
            Assert.AreEqual("zoroa_p1", z.enemy.id);
            Assert.AreEqual(1840, z.unit.hp);
            var partyHp = b.Party.Select(p => p.unit.hp).ToList();
            b.Party[0].AddStatus(StatusId.DefenseUp, 3, 0.2f, b.Party[0]);

            b.Damage(z, 1839);
            Assert.AreEqual(1, z.phase, "Phase 2 only once the first bar is empty");
            var hit = b.Damage(z, 500);
            Assert.IsFalse(hit.killed);
            Assert.IsTrue(z.Alive);
            Assert.AreEqual(2, z.phase);
            Assert.AreEqual("zoroa_p2", z.enemy.id);
            Assert.AreEqual(1840, z.unit.hp, "fresh bar; overkill does not carry over");
            Assert.AreEqual(850, z.unit.mp);
            Assert.AreEqual(200, z.MaxBreakGauge);
            Assert.AreEqual(17, z.Initiative);
            Assert.AreEqual(1, b.Enemies.Count, "never two Zoroas at once");
            Assert.AreEqual(Outcome.None, b.Outcome);
            Assert.AreEqual(1, b.Log.Count(l => l.Contains("(Phase 2)")));
            CollectionAssert.AreEqual(partyHp, b.Party.Select(p => p.unit.hp), "party untouched by the transition");
            Assert.IsTrue(b.Party[0].Has(StatusId.DefenseUp));
            CollectionAssert.Contains(EnemyAI.Moves(z).Select(m => m.name).ToList(), "Soulstorm");

            b.Damage(z, 1840);
            Assert.IsFalse(z.Alive);
            Assert.AreEqual(Outcome.Victory, b.Outcome, "3680 effective HP in total");
        }

        [Test]
        public void ZoroaIsImmuneToMentalEffectsInBothPhases()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            foreach (var phase in new[] { 1, 2 })
            {
                foreach (StatusId id in Enum.GetValues(typeof(StatusId)))
                {
                    if (!StatusInfo.IsMental(id)) continue;
                    b.Apply(z, new StatusApply { id = id, chance = 1f }, b.Party[0], 1f);
                    Assert.IsFalse(z.Has(id), $"phase {phase}: {id}");
                }
                if (phase == 1) b.Damage(z, 1840);
            }
            Assert.AreEqual(2, z.phase);
        }

        [Test]
        public void ZoroaGetsOneTurnPerRoundAcrossThePhaseChange()
        {
            var b = NewBattle(1, "zoroa_p1");
            foreach (var p in b.Party) p.unit.stats.spd = 1;
            var z = b.Enemies[0];
            var turns = new Dictionary<int, int>();
            for (int i = 0; i < 40 && b.Round <= 4; i++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c == z)
                {
                    turns[b.Round] = turns.TryGetValue(b.Round, out var n) ? n + 1 : 1;
                    if (b.Round == 2) b.Damage(z, 1840); // phase change after it already acted this round
                }
                b.Perform(BattleAction.Guard(c));
            }
            Assert.GreaterOrEqual(turns.Count, 4);
            Assert.IsTrue(turns.Values.All(n => n == 1), string.Join(",", turns.Select(kv => $"R{kv.Key}:{kv.Value}")));
            Assert.AreEqual(2, z.phase);
        }

        [Test]
        public void ZoroaAiUsesTelegraphedSkillsWithinItsMp()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            var seen = new List<string>();
            for (int i = 0; i < 80 && b.Round <= 10 && b.Outcome == Outcome.None; i++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c == z)
                {
                    var act = EnemyAI.Choose(b, z);
                    Assert.IsNull(b.Validate(act), "AI never picks an illegal action");
                    seen.Add((act.charging ? "charge:" : "") + act.attack.name);
                    b.Perform(act, 1f, Enumerable.Repeat(QteResult.Poor, Math.Max(1, act.attack.hits)).ToList());
                }
                else
                {
                    c.unit.hp = c.Stats.maxHp; // keep the party standing
                    b.Perform(BattleAction.Guard(c));
                }
            }
            CollectionAssert.AreEqual(new[] { "charge:Soulbind", "Soulbind", "charge:Final Reaping", "Final Reaping" }, seen.Take(4),
                "Final Reaping waits out its opening cooldown; each telegraphed move charges one turn first");
            Assert.IsTrue(seen.All(s => !s.Contains("Soulstorm")), "Soulstorm is Phase 2 only");
            Assert.GreaterOrEqual(z.unit.mp, 0);
        }

        [Test]
        public void ZoroaIntentShowsFinalReapingOneTurnAhead()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            z.cooldowns.Clear(); // Final Reaping ready now
            b.NextTurn(); // round 1 preparation plans every enemy
            Assert.IsTrue(z.Intent.charging);
            Assert.AreEqual("Final Reaping", z.Intent.move.name);
            StringAssert.Contains("FINAL REAPING X4 HEAVY", EnemyAI.IntentText(z));
            StringAssert.Contains("NEXT TURN", EnemyAI.IntentText(z));
        }

        // ---- encounters ----

        [Test]
        public void DungeonOneEncountersMatchThePlan()
        {
            var expected = new Dictionary<string, (Tier tier, string[] enemies)>
            {
                ["d1_e1"] = (Tier.Normal, new[] { "hollow_squire", "hollow_squire" }),
                ["d1_e2"] = (Tier.Normal, new[] { "gravebound_archer", "hollow_squire" }),
                ["d1_e3"] = (Tier.Normal, new[] { "cinder_wisp", "forgotten_militia" }),
                ["d1_e4"] = (Tier.Normal, new[] { "soulbound_hound", "crypt_leech" }),
                ["d1_e5"] = (Tier.Normal, new[] { "possessed_royal_guard", "hollow_squire", "hollow_squire" }),
                ["d1_e6"] = (Tier.Elite, new[] { "soul_reaper", "forgotten_militia" }),
                ["d1_e7"] = (Tier.Elite, new[] { "royal_ossuary_knight" }),
                ["d1_boss"] = (Tier.Boss, new[] { "zoroa_p1" }),
            };
            CollectionAssert.AreEquivalent(expected.Keys, Database.Encounters.Keys);
            foreach (var kv in expected)
            {
                var enc = Database.Encounters[kv.Key];
                Assert.AreEqual(kv.Value.tier, enc.tier, kv.Key);
                CollectionAssert.AreEqual(kv.Value.enemies, enc.enemies, kv.Key);
                Assert.IsFalse(string.IsNullOrEmpty(enc.name), kv.Key);

                var b = new Battle(Database.Party.Select(id => new Unit(Database.Characters[id])), enc.enemies.Select(E), new List<string>(), new Random(1));
                Assert.AreEqual(kv.Value.enemies.Length, b.Enemies.Count, kv.Key);
                CollectionAssert.AreEqual(Enumerable.Range(0, b.Enemies.Count).ToList(), b.Enemies.Select(e => e.slot).ToList(), "spawn order");
                CollectionAssert.AllItemsAreUnique(b.Enemies.Select(e => e.name));
            }
            Assert.IsFalse(Database.Encounters.Values.Any(e => e.enemies.Contains("zoroa_p2")), "Phase 2 never spawns on its own");
        }

        [Test]
        public void ActiveRunPoolIsDungeonOneOnlyAndLegacyEnemiesStayFixturesOnly()
        {
            var legacy = new[] { "skeleton", "cursed_knight", "warden" };
            foreach (var id in legacy) Assert.IsTrue(Database.Enemies.ContainsKey(id), $"{id} still available to tests");
            CollectionAssert.AreEquivalent(Dungeon1Roster.Encounters.Select(e => e.id), Database.Encounters.Keys);
            for (int seed = 1; seed <= 25; seed++)
                foreach (var n in MapGenerator.Generate(seed).nodes.Where(n => n.IsCombat))
                {
                    StringAssert.StartsWith("d1_", n.encounter);
                    Assert.IsFalse(Database.Encounters[n.encounter].enemies.Intersect(legacy).Any(), n.encounter);
                }
        }

        [Test]
        public void EliteSpeedComesFromRosterV17NotTheStaleInitiativeSheet()
        {
            Assert.AreEqual(17, new Unit(E("soul_reaper"), 1).stats.spd, "not 15");
            Assert.AreEqual(9, new Unit(E("royal_ossuary_knight"), 1).stats.spd, "not 8");
            Assert.AreEqual(20, new Unit(E("soul_reaper"), 8).stats.spd, "floor(17 x 1.21), not 18");
            Assert.AreEqual(10, new Unit(E("royal_ossuary_knight"), 8).stats.spd, "floor(9 x 1.21), not 9");
        }

        // ---- regression: fixes that must stay fixed ----

        [Test]
        public void Regression_EnemyHeavyIsAppliedInCombat()
        {
            Hit Release(QteResult r)
            {
                var b = NewBattle(9, "royal_ossuary_knight");
                var knight = AdvanceTo(b, c => c.IsEnemy);
                b.Party[0].unit.hp = 9999;
                return b.Perform(BattleAction.Strike(knight, Move("royal_ossuary_knight", "Graveward Charge"), b.Party[0]), 1f, new[] { r })[0];
            }
            var perfect = Release(QteResult.Perfect);
            var poor = Release(QteResult.Poor);
            Assert.IsFalse(perfect.blocked, "Heavy cannot be fully parried");
            Assert.Greater(perfect.amount, 0);
            Assert.AreEqual(poor.amount * 0.5f, perfect.amount, 1.01f, "Perfect vs Heavy = 50% reduction");

            var b2 = NewBattle(9, "royal_ossuary_knight");
            var k2 = AdvanceTo(b2, c => c.IsEnemy);
            var slam = b2.Perform(BattleAction.Strike(k2, Move("royal_ossuary_knight", "Ossuary Slam"), b2.Party[0]), 1f, new[] { QteResult.Perfect })[0];
            Assert.IsTrue(slam.blocked, "non-Heavy Perfect still negates");
            Assert.AreEqual(0, slam.amount);
        }

        [Test]
        public void Regression_ElementalAndDarkEnemyAttacksUseMatk()
        {
            int Dealt(string enemy, string move, int atk, int matk)
            {
                var b = NewBattle(3, enemy);
                var e = AdvanceTo(b, c => c.IsEnemy);
                e.unit.stats.atk = atk;
                e.unit.stats.mag = matk;
                foreach (var p in b.Party) p.unit.hp = 9999;
                var m = e.enemy.attacks.First(a => a.name == move);
                var target = m.allTargets ? null : b.Party[2];
                return b.Perform(BattleAction.Strike(e, m, target), 1f, Enumerable.Repeat(QteResult.Poor, m.hits).ToList())
                    .Where(h => !h.heal).Max(h => h.amount);
            }
            Assert.Greater(Dealt("cinder_wisp", "Ember Bolt", 0, 200), 20, "Fire: MATK drives damage");
            Assert.LessOrEqual(Dealt("cinder_wisp", "Ember Bolt", 200, 0), 3, "Fire: ATK is ignored (minimum-damage hit)");
            Assert.Greater(Dealt("crypt_leech", "Vital Siphon", 0, 200), 20, "Shadow: MATK");
            Assert.Greater(Dealt("cursed_knight", "Dread Howl", 0, 200), 5, "legacy Dark move: MATK");
            Assert.LessOrEqual(Dealt("cursed_knight", "Dread Howl", 200, 0), 3, "legacy Dark move: ATK ignored");
            Assert.Greater(Dealt("hollow_squire", "Rusty Cleave", 200, 0), 20, "Physical still uses ATK");
        }

        [Test]
        public void Regression_ScalingKeepsExactDecimalPrecision()
        {
            Assert.AreEqual(200, new Unit(E("crypt_leech"), 6).stats.maxHp, "125 x 1.60 = 200, not 199");
            Assert.AreEqual(200, Unit.ScaleStat(125, Tier.Normal, Unit.ScaledStat.HP, 6));
            Assert.AreEqual(0.12m, Unit.GrowthRate(Tier.Normal, Unit.ScaledStat.HP), "rates are exact decimals");
            Assert.AreEqual(0.055m, Unit.GrowthRate(Tier.Elite, Unit.ScaledStat.DEF));
            foreach (var d in Dungeon1Roster.Enemies)
                for (int n = 1; n <= 8; n++)
                    Assert.AreEqual(Expected(d.stats.maxHp, d.tier, 0, n), Unit.ScaleStat(d.stats.maxHp, d.tier, Unit.ScaledStat.HP, n), $"{d.id} D{n}");
        }

        [Test]
        public void Regression_RecoverBreakGaugeClearsBroken()
        {
            var b = NewBattle(1, "royal_ossuary_knight");
            var knight = b.Enemies[0];
            Assert.IsTrue(knight.ApplyBreakDamage(10000, b.Party[0], out _));
            Assert.IsTrue(knight.IsBroken);
            Assert.IsTrue(knight.StatusController.HasStatus(StatusId.Broken));
            knight.RecoverBreakGauge();
            Assert.IsFalse(knight.IsBroken, "cleared from both status stores");
            Assert.IsFalse(knight.StatusController.HasStatus(StatusId.Broken));
            Assert.AreEqual(knight.MaxBreakGauge, knight.CurrentBreakGauge);

            // Established mechanic: Broken lasts until the owner's next turn, which it loses; then the gauge is full again.
            var b2 = NewBattle(1, "royal_ossuary_knight");
            var k2 = b2.Enemies[0];
            k2.ApplyBreakDamage(10000, b2.Party[0], out _);
            for (int i = 0; i < 20 && !b2.Log.Any(l => l.Contains("Ossuary Knight is stunned")); i++)
            {
                var c = b2.NextTurn();
                if (c != null) b2.Perform(BattleAction.Guard(c));
            }
            Assert.IsTrue(b2.Log.Any(l => l.Contains("Ossuary Knight is stunned")));
            Assert.IsFalse(k2.IsBroken);
            Assert.AreEqual(100, k2.CurrentBreakGauge);

            // A Broken boss is recovered through the same method on its phase change.
            var b3 = NewBattle(1, "zoroa_p1");
            var z = b3.Enemies[0];
            z.ApplyBreakDamage(10000, b3.Party[0], out _);
            Assert.IsTrue(z.IsBroken);
            b3.Damage(z, 1840);
            Assert.IsFalse(z.IsBroken);
            Assert.AreEqual(200, z.CurrentBreakGauge);
        }
    }
}
