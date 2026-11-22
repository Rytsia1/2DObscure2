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
            "reliquary_confessor", "graveglass_arcanist", "crownless_duelist", "tombstone_colossus", "mourning_bell_wraith",
        };

        // ---- enemy data ----

        static int ZoroaHp => Dungeon1Roster.ScaleD1Stat(1840, Dungeon1Roster.BossHpMult);

        // id, tier, workbook base HP, MP, ATK, DEF, MATK, MDEF, SPD, Break
        [TestCase("hollow_squire", Tier.Normal, 145, 35, 18, 8, 8, 6, 9, 100)]
        [TestCase("gravebound_archer", Tier.Normal, 112, 45, 20, 5, 10, 7, 13, 100)]
        [TestCase("cinder_wisp", Tier.Normal, 105, 70, 8, 4, 22, 8, 14, 100)]
        [TestCase("soulbound_hound", Tier.Normal, 155, 40, 17, 7, 9, 6, 16, 100)]
        [TestCase("possessed_royal_guard", Tier.Normal, 190, 30, 23, 12, 7, 9, 7, 100)]
        [TestCase("crypt_leech", Tier.Normal, 125, 55, 15, 5, 13, 7, 11, 100)]
        [TestCase("forgotten_militia", Tier.Normal, 135, 35, 16, 6, 8, 6, 12, 100)]
        [TestCase("soul_reaper", Tier.Elite, 430, 130, 32, 13, 29, 14, 17, 100)]
        [TestCase("royal_ossuary_knight", Tier.Elite, 520, 90, 36, 22, 18, 18, 9, 100)]
        [TestCase("reliquary_confessor", Tier.Elite, 380, 150, 15, 11, 30, 20, 12, 100)]
        [TestCase("graveglass_arcanist", Tier.Elite, 360, 180, 12, 9, 38, 17, 16, 100)]
        [TestCase("crownless_duelist", Tier.Elite, 410, 100, 36, 13, 12, 12, 18, 100)]
        [TestCase("tombstone_colossus", Tier.Elite, 620, 70, 38, 24, 9, 16, 7, 100)]
        [TestCase("mourning_bell_wraith", Tier.Elite, 400, 145, 15, 12, 33, 22, 14, 100)]
        [TestCase("zoroa_p1", Tier.Boss, 1840, 600, 38, 20, 42, 22, 14, 200)]
        [TestCase("zoroa_p2", Tier.Boss, 1840, 850, 43, 18, 50, 24, 17, 200)]
        public void RosterBaseStatsMatchWorkbook(string id, Tier tier, int hp, int mp, int atk, int def, int matk, int mdef, int spd, int brk)
        {
            float hpMult = tier == Tier.Boss ? Dungeon1Roster.BossHpMult : tier == Tier.Elite ? Dungeon1Roster.EliteStatMult : 1f;
            float atkMult = id.StartsWith("zoroa_") ? Dungeon1Roster.ZoroaAtkMult : tier == Tier.Boss ? Dungeon1Roster.BossAtkMult : tier == Tier.Elite ? Dungeon1Roster.EliteStatMult : 1f;
            int scaledHp = Dungeon1Roster.ScaleD1Stat(hp, hpMult);
            int scaledAtk = Dungeon1Roster.ScaleD1Stat(atk, atkMult);
            int scaledMatk = Dungeon1Roster.ScaleD1Stat(matk, atkMult);

            var d = E(id);
            Assert.AreEqual(tier, d.tier);
            Assert.AreEqual(AiKind.Priority, d.ai);
            Assert.AreEqual(new Stats(scaledHp, mp, scaledAtk, def, spd, scaledMatk), d.stats);
            Assert.AreEqual(mdef, d.magicDef);
            Assert.AreEqual(brk, d.maxBreakGauge);

            var c = new Combatant(new Unit(d), Side.Enemy, 0, d.shortName, d.tag, d);
            Assert.AreEqual(scaledHp, c.unit.hp);
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
            yield return S("zoroa_p1", "Soulbind", Element.Shadow, false, new int[0], 24, -1, false, StatusId.Silence, 0.20f);
            yield return S("zoroa_p1", "Harvest Call", Element.Shadow, false, new int[0], 30, -1, false);
            yield return S("zoroa_p1", "Final Reaping", Element.Physical, false, new[] { 30, 30, 30, 45 }, 35, 3, true);
            yield return S("zoroa_p2", "Soul Rend", Element.Physical, false, new[] { 43 }, 0, -1, false);
            yield return S("zoroa_p2", "Soul Lance", Element.Shadow, false, new[] { 50 }, 22, -1, false);
            yield return S("zoroa_p2", "Soulbind", Element.Shadow, false, new int[0], 28, -1, false, StatusId.Silence, 0.25f);
            yield return S("zoroa_p2", "Harvest Call", Element.Shadow, false, new int[0], 35, -1, false);
            yield return S("zoroa_p2", "Final Reaping", Element.Physical, false, new[] { 34, 34, 34, 50 }, 40, 3, true);
            yield return S("zoroa_p2", "Soulstorm", Element.Shadow, true, new[] { 24 }, 45, -1, true);
            yield return S("reliquary_confessor", "Reliquary Staff", Element.Physical, false, new[] { 15 }, 0, -1, false);
            yield return S("reliquary_confessor", "Mending Litany", Element.Physical, false, new int[0], 24, -1, false);
            yield return S("graveglass_arcanist", "Glass Shard", Element.Physical, false, new[] { 12 }, 0, -1, false);
            yield return S("graveglass_arcanist", "Umbral Lance", Element.Shadow, false, new[] { 38 }, 22, -1, false);
            yield return S("crownless_duelist", "Crownless Cut", Element.Physical, false, new[] { 36 }, 0, -1, false);
            yield return S("crownless_duelist", "Triple Feint", Element.Physical, false, new[] { 12, 12, 12 }, 18, -1, false);
            yield return S("tombstone_colossus", "Monument Fist", Element.Physical, false, new[] { 38 }, 0, -1, false);
            yield return S("tombstone_colossus", "Gravefall", Element.Physical, false, new[] { 38 }, 18, -1, false);
            yield return S("mourning_bell_wraith", "Echo Claw", Element.Physical, false, new[] { 15 }, 0, -1, false);
            yield return S("mourning_bell_wraith", "Tolling Note", Element.Shadow, false, new[] { 34 }, 20, -1, false);
            yield return S("mourning_bell_wraith", "Funeral Chorus", Element.Shadow, true, new[] { 20 }, 32, -1, true);
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
            if (damage.Length == 0) Assert.IsTrue(m.StatusOnly || m.Heals || m.Summons);
            else CollectionAssert.AreEqual(damage, m.hitDamage);

            // The coefficient maps the authored D1 damage onto the D1 ATK / MATK (scaled automatically by the tier stat multiplier).
            float stat = m.UsesMagic ? d.stats.mag : d.stats.atk;
            float atkMult = d.id.StartsWith("zoroa_") ? Dungeon1Roster.ZoroaAtkMult : d.tier == Tier.Boss ? Dungeon1Roster.BossAtkMult : d.tier == Tier.Elite ? Dungeon1Roster.EliteStatMult : 1f;
            for (int i = 0; i < damage.Length; i++) Assert.AreEqual(damage[i] * atkMult, stat * m.PowerAt(i), 0.75, $"hit {i + 1}");

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
            int p1Hp = Expected(ZoroaHp, Tier.Boss, 0, 8);
            int p1Atk = Expected(Dungeon1Roster.ScaleD1Stat(38, Dungeon1Roster.ZoroaAtkMult), Tier.Boss, 2, 8);
            int p1Matk = Expected(Dungeon1Roster.ScaleD1Stat(42, Dungeon1Roster.ZoroaAtkMult), Tier.Boss, 3, 8);
            var p1 = new Unit(E("zoroa_p1"), 8);
            Assert.AreEqual(new Stats(p1Hp, 1020, p1Atk, 28, 17, p1Matk), p1.stats);
            Assert.AreEqual(31, p1.Derived.magicDefense);

            int p2Hp = Expected(ZoroaHp, Tier.Boss, 0, 8);
            int p2Atk = Expected(Dungeon1Roster.ScaleD1Stat(43, Dungeon1Roster.ZoroaAtkMult), Tier.Boss, 2, 8);
            int p2Matk = Expected(Dungeon1Roster.ScaleD1Stat(50, Dungeon1Roster.ZoroaAtkMult), Tier.Boss, 3, 8);
            var p2 = new Unit(E("zoroa_p2"), 8);
            Assert.AreEqual(new Stats(p2Hp, 1445, p2Atk, 25, 21, p2Matk), p2.stats);
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
            // First seeded roll without a feint: feints shift impacts and are tested in QteTests; here the intervals are.
            DefenseQte Q(string enemy, string move)
            {
                var b = NewBattle(1, enemy);
                for (int seed = 0; ; seed++)
                {
                    var q = (DefenseQte)QteFactory.Defense(BattleAction.Strike(b.Enemies[0], Move(enemy, move), b.Party[2]), QteAssist.Standard, null, new Random(seed));
                    if (!Enumerable.Range(0, q.Hits).Any(q.IsFeint)) return q;
                }
            }
            float[] Gaps(DefenseQte q) => Enumerable.Range(1, q.Hits - 1).Select(i => (float)Math.Round(q.impacts[i] - q.impacts[i - 1], 3)).ToArray();

            var militia = Q("forgotten_militia", "Two-Step Slash");
            Assert.AreEqual(2, militia.Hits);
            // Steady (no authored intervals) is jittered +/- IntervalJitter around 350 ms (Bab 12 §6.6); a feint can push it later.
            if (!militia.IsFeint(1))
                Assert.That(Gaps(militia)[0], Is.InRange(0.35f - QteConfig.IntervalJitter - 0.001f, 0.35f + QteConfig.IntervalJitter + 0.001f), "Steady 350 ms +/- jitter");

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
            b.Party[0].guarding = true; // a full parry needs Guard or a shield (Bab 12 §3.5)
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
            float heavyTaken = QteConfig.DefenseTaken(QteResult.Perfect, true, QteConfig.BlockOf(perfect[3].target));
            Assert.AreEqual(poor[3].amount * heavyTaken, perfect[3].amount, 1.01f, "Heavy Perfect = Good-level reduction by Block Power");
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
            CollectionAssert.AreEquivalent(new[] { "soul_reaper", "royal_ossuary_knight", "zoroa_p1", "zoroa_p2", "reliquary_confessor", "graveglass_arcanist" }, holders);
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
            b.Damage(z, ZoroaHp);
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
        public void TurnOrderTagsNeverClash()
        {
            var cleric = new CharacterDef
            {
                id = "hero_cleric", name = "Cleric", classId = "cleric", raceId = "human", backgroundId = "oathsworn",
                baseScores = AbilityScores.DefaultBase, stats = new Stats(100, 20, 10, 10, 5, 5),
            };
            var b = new Battle(new[] { new Unit(Database.Characters["knight"]), new Unit(cleric) },
                new[] { Database.Enemies["crypt_leech"] }, new List<string>(), new Random(4));
            var tags = b.All.Select(c => c.tag).ToList();
            CollectionAssert.AllItemsAreUnique(tags);
            Assert.AreEqual("CLR", b.Party.First(c => c.name == "Cleric").tag, "Cleric yields CLE to the Crypt Leech");

            var used = new HashSet<string> { "CLE", "CLR", "CLI", "CLC" };
            Assert.AreEqual("CL2", Battle.UniqueTag("Cleric", used));
            Assert.AreEqual("BO", Battle.UniqueTag("Bo", used));
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
            b2.Party[2].guarding = true; // full parry needs Guard or a shield (Bab 12 §3.5)
            b2.Perform(BattleAction.Strike(leech2, Move("crypt_leech", "Vital Siphon"), b2.Party[2]), 1f, new[] { QteResult.Perfect });
            Assert.AreEqual(50, leech2.unit.hp, "nothing dealt, nothing healed");
        }

        [Test]
        public void SoulstormHitsAllFourPartyMembersWithOneSharedReaction()
        {
            var b = NewBattle(1, "zoroa_p2");
            var z = AdvanceTo(b, c => c.IsEnemy);
            foreach (var p in b.Party) p.guarding = true; // full parry needs Guard or a shield (Bab 12 §3.5)
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
            Assert.AreEqual(ZoroaHp, z.unit.hp);
            var partyHp = b.Party.Select(p => p.unit.hp).ToList();
            b.Party[0].AddStatus(StatusId.DefenseUp, 3, 0.2f, b.Party[0]);

            b.Damage(z, ZoroaHp - 1);
            Assert.AreEqual(1, z.phase, "Phase 2 only once the first bar is empty");
            var hit = b.Damage(z, 500);
            Assert.IsFalse(hit.killed);
            Assert.IsTrue(z.Alive);
            Assert.AreEqual(2, z.phase);
            Assert.AreEqual("zoroa_p2", z.enemy.id);
            Assert.AreEqual(ZoroaHp, z.unit.hp, "fresh bar; overkill does not carry over");
            Assert.AreEqual(850, z.unit.mp);
            Assert.AreEqual(200, z.MaxBreakGauge);
            Assert.AreEqual(17, z.Initiative);
            Assert.AreEqual(1, b.Enemies.Count, "never two Zoroas at once");
            Assert.AreEqual(Outcome.None, b.Outcome);
            Assert.AreEqual(1, b.Log.Count(l => l.Contains("(Phase 2)")));
            CollectionAssert.AreEqual(partyHp, b.Party.Select(p => p.unit.hp), "party untouched by the transition");
            Assert.IsTrue(b.Party[0].Has(StatusId.DefenseUp));
            CollectionAssert.Contains(EnemyAI.Moves(z).Select(m => m.name).ToList(), "Soulstorm");

            b.Damage(z, ZoroaHp);
            Assert.IsFalse(z.Alive);
            Assert.AreEqual(Outcome.Victory, b.Outcome, $"{2 * ZoroaHp} effective HP in total");
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
                if (phase == 1) b.Damage(z, ZoroaHp);
            }
            Assert.AreEqual(2, z.phase);
        }

        [Test]
        public void ZoroaGetsTwoTurnsPerRoundAcrossThePhaseChange()
        {
            var b = NewBattle(1, "zoroa_p1");
            foreach (var p in b.Party) p.unit.stats.spd = 1;
            var z = b.Enemies[0];
            var turns = new Dictionary<int, int>();
            for (int i = 0; i < 60 && b.Round <= 4; i++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c == z)
                {
                    turns[b.Round] = turns.TryGetValue(b.Round, out var n) ? n + 1 : 1;
                    if (b.Round == 2 && turns[b.Round] == 1) b.Damage(z, ZoroaHp); // phase change after Turn A: Turn B still belongs to this round, no extra turns
                }
                b.Perform(BattleAction.Guard(c));
            }
            Assert.GreaterOrEqual(turns.Count, 4);
            Assert.IsTrue(turns.Take(4).All(kv => kv.Value == Dungeon1Roster.ZoroaTurnsPerRound), string.Join(",", turns.Select(kv => $"R{kv.Key}:{kv.Value}")));
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
            CollectionAssert.AreEqual(new[] { "Soulbind", "Soul Lance", "Soul Rend", "charge:Final Reaping", "Final Reaping" }, seen.Take(5),
                "Final Reaping waits out its opening cooldown; Soulbind is instant; telegraphed moves charge one turn first");
            Assert.IsTrue(seen.All(s => !s.Contains("Soulstorm")), "Soulstorm is Phase 2 only");
            Assert.GreaterOrEqual(z.unit.mp, 0);
        }

        [Test]
        public void ZoroaAppearsTwiceInTurnOrderWithSecondTurnAtHalfSpeedAndOrdinaryEnemiesOnce()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0]; // SPD 14 -> Turn A at 14, Turn B at 7
            int[] spd = { 16, 12, 9, 5 }; // 2 heroes (12, 9) between 14 and 7
            for (int i = 0; i < 4; i++) b.Party[i].unit.stats.spd = spd[i];

            var order = b.NextRoundOrder;
            Assert.AreEqual(2, order.Count(c => c == z), "Zoroa appears twice");
            CollectionAssert.AreEqual(new[] { b.Party[0], z, b.Party[1], b.Party[2], z, b.Party[3] }, order);

            var bNormal = NewBattle(1, "hollow_squire", "soul_reaper");
            foreach (var e in bNormal.Enemies)
                Assert.AreEqual(1, bNormal.NextRoundOrder.Count(c => c == e), $"{e.enemy.id} appears once");
        }

        [Test]
        public void ZoroaFinalReapingIsUsedAtMostOnceEveryThreeRoundsOverTwelveRounds()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            var releaseRounds = new List<int>();
            while (b.Round <= 12 && b.Outcome == Outcome.None)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c.IsEnemy)
                {
                    var act = EnemyAI.Choose(b, c);
                    if (c == z && !act.charging && act.attack.name == "Final Reaping")
                        releaseRounds.Add(b.Round);
                    b.Perform(act, 1f, Enumerable.Repeat(QteResult.Poor, Math.Max(1, act.attack.hits)).ToList());
                }
                else
                {
                    c.unit.hp = c.Stats.maxHp;
                    b.Perform(BattleAction.Guard(c));
                }
            }
            Assert.LessOrEqual(releaseRounds.Count, 4, $"Final Reaping released on rounds: {string.Join(",", releaseRounds)}");
            for (int i = 1; i < releaseRounds.Count; i++)
                Assert.GreaterOrEqual(releaseRounds[i] - releaseRounds[i - 1], 3, "cooldown 3 is counted in rounds, not individual turns");
        }

        [Test]
        public void ZoroaStunRemovesBothTurnsThatRound()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            z.stunTurns = 1;
            int zoroaActions = 0;
            while (b.Round <= 1)
            {
                var c = b.NextTurn();
                if (c == null) break;
                if (b.Round > 1) break;
                if (c == z) zoroaActions++;
                b.Perform(BattleAction.Guard(c));
            }
            Assert.AreEqual(0, zoroaActions, "1-turn stun costs both of Zoroa's turns in that round");
            Assert.AreEqual(0, z.stunTurns, "stunTurns decremented once for the round");
        }

        [Test]
        public void ZoroaChargesReleaseOnNextTurnWithinOrAcrossRoundsAndCanBeBrokenInBetween()
        {
            // 1. Charge on Turn A -> releases on Turn B of the same round, and Turn B's intent is planned right after Turn A.
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            z.cooldowns.Clear();
            AdvanceTo(b, c => c == z);
            int roundA = b.Round;
            var actA = EnemyAI.Choose(b, z);
            Assert.IsTrue(actA.charging && actA.attack.name == "Final Reaping");
            b.Perform(actA);
            Assert.IsNotNull(z.Intent, "Turn B intent is planned right after Turn A");
            StringAssert.StartsWith("RELEASE: FINAL REAPING", EnemyAI.IntentText(z));

            AdvanceTo(b, c => c == z);
            Assert.AreEqual(roundA, b.Round, "Turn B is in the same round");
            var actB = EnemyAI.Choose(b, z);
            Assert.IsFalse(actB.charging);
            Assert.AreEqual("Final Reaping", actB.attack.name);
            foreach (var p in b.Party) p.unit.hp = 9999;
            b.Perform(actB, 1f, Enumerable.Repeat(QteResult.Poor, 4).ToList());
            Assert.IsTrue(z.JustReleasedCharge);

            // 2. Charge on Turn B -> releases on next round's Turn A.
            var b2 = NewBattle(1, "zoroa_p1");
            var z2 = b2.Enemies[0];
            AdvanceTo(b2, c => c == z2); // R1 Turn A: Soulbind
            b2.Perform(EnemyAI.Choose(b2, z2));
            z2.cooldowns.Remove(Combatant.MoveKey(Move("zoroa_p1", "Final Reaping"))); // ready on R1 Turn B
            EnemyAI.Plan(b2, z2);
            AdvanceTo(b2, c => c == z2); // R1 Turn B
            Assert.AreEqual(1, b2.Round);
            var chargeB = EnemyAI.Choose(b2, z2);
            Assert.IsTrue(chargeB.charging && chargeB.attack.name == "Final Reaping");
            b2.Perform(chargeB);

            AdvanceTo(b2, c => c == z2); // R2 Turn A
            Assert.AreEqual(2, b2.Round);
            var releaseA = EnemyAI.Choose(b2, z2);
            Assert.IsFalse(releaseA.charging);
            Assert.AreEqual("Final Reaping", releaseA.attack.name);

            // 3. Breaking Zoroa between Turn A and Turn B cancels the charge and skips Turn B.
            var b3 = NewBattle(1, "zoroa_p1");
            var z3 = b3.Enemies[0];
            z3.cooldowns.Clear();
            AdvanceTo(b3, c => c == z3); // Turn A charges Final Reaping
            b3.Perform(EnemyAI.Choose(b3, z3));
            Assert.IsTrue(z3.IsCharging);
            var hero = AdvanceTo(b3, c => c.IsPlayer);
            z3.CurrentBreakGauge = 1;
            b3.Perform(BattleAction.Attack(hero, z3));
            Assert.IsTrue(z3.IsBroken);
            Assert.IsFalse(z3.IsCharging);
        }

        [Test]
        public void ZoroaNeverUsesTwoHeavyMovesInOneRound()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            z.cooldowns.Clear();
            AdvanceTo(b, c => c == z);
            var actA = EnemyAI.Choose(b, z);
            Assert.IsTrue(actA.attack.HasHeavy);
            b.Perform(actA);
            // Force charge/cooldown off as if another Heavy move were available on Turn B: heavyTaken must block it.
            z.Charge = null;
            z.JustReleasedCharge = false;
            z.cooldowns.Clear();
            var intentB = PriorityBrain.Select(b, z);
            Assert.IsFalse(intentB.move.HasHeavy, "second turn in the same round cannot pick another Heavy move");
        }

        [Test]
        public void HarvestCallFillsUpToTwoSummonsWith8020NormalEliteSplitAndNeverSummonsStageBosses()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            var hc = Move("zoroa_p1", "Harvest Call");
            z.cooldowns.Clear();
            AdvanceTo(b, c => c == z);

            // 0 summons alive -> spawns 2
            Assert.IsNull(QteFactory.Defense(BattleAction.Strike(z, hc, null), QteAssist.Standard), "no QTE on Harvest Call");
            b.Perform(BattleAction.Strike(z, hc, null));
            var summons = b.Enemies.Where(e => e.IsSummon && e.Alive).ToList();
            Assert.AreEqual(2, summons.Count, "0 alive -> spawns 2");
            foreach (var s in summons)
            {
                int baseHp = new Unit(s.enemy, 1).stats.maxHp;
                Assert.AreEqual((int)Math.Round(baseHp * Dungeon1Roster.SummonHpMult), s.Stats.maxHp, "70% summon HP");
                Assert.IsFalse(b.Upcoming.Contains(s), "summons never act in the round they appear");
            }

            // 2 summons alive -> AI skips Harvest Call even when off cooldown
            z.cooldowns.Clear();
            Assert.AreNotEqual("Harvest Call", PriorityBrain.Select(b, z).move.name);

            // 1 summon alive -> spawns 1 to refill to 2
            summons[0].unit.hp = 0;
            Assert.AreEqual(1, b.Enemies.Count(e => e.IsSummon && e.Alive));
            AdvanceTo(b, c => c == z);
            b.Perform(BattleAction.Strike(z, hc, null));
            Assert.AreEqual(2, b.Enemies.Count(e => e.IsSummon && e.Alive), "1 alive -> spawns 1");

            // 1000 seeded rolls: 20% +/- 3% Elite, and stage bosses never appear
            var rng = new Random(42);
            int elites = 0;
            for (int i = 0; i < 1000; i++)
            {
                var def = Dungeon1Roster.RollHarvestCallSummon(rng);
                Assert.AreNotEqual("soul_reaper", def.id);
                Assert.AreNotEqual("royal_ossuary_knight", def.id);
                Assert.AreNotEqual(Tier.Boss, def.tier);
                if (def.tier == Tier.Elite) elites++;
            }
            Assert.That(elites / 1000.0, Is.InRange(0.17, 0.23), $"Elite roll rate: {elites}/1000");
        }

        [Test]
        public void SummonsPersistAcrossZoroaPhaseChangeCarryHarvestCallCooldownAndDieWithZoroaForZeroRewards()
        {
            var b = NewBattle(1, "zoroa_p1");
            var z = b.Enemies[0];
            var hc1 = Move("zoroa_p1", "Harvest Call");
            z.cooldowns.Clear();
            AdvanceTo(b, c => c == z);
            b.Perform(BattleAction.Strike(z, hc1, null));
            Assert.AreEqual(2, b.Enemies.Count(e => e.IsSummon && e.Alive));
            Assert.AreEqual(Dungeon1Roster.HarvestCallCooldown, z.Cooldown(Combatant.MoveKey(hc1)));

            // Phase transition: summons stay alive, Harvest Call cooldown carries over to Phase 2
            b.Damage(z, ZoroaHp);
            Assert.AreEqual(2, z.phase);
            Assert.AreEqual(2, b.Enemies.Count(e => e.IsSummon && e.Alive), "summons persist into Phase 2");
            Assert.AreEqual(Dungeon1Roster.HarvestCallCooldown, z.Cooldown(Combatant.MoveKey(Move("zoroa_p2", "Harvest Call"))), "cooldown carries over");

            // Killing a summon does not trigger enemy-defeated relic hooks or grant extra EXP/gold
            var run = new RunState(MapGenerator.Generate(1, 1, 3), Database.Party.Select(id => Database.Characters[id]), 1);
            run.TryAddRelic("R013", out _); // Dirge-Eater's Fang: triggers on enemy defeated
            b.run = run;
            var firstSummon = b.Enemies.First(e => e.IsSummon && e.Alive);
            b.Party[0].unit.hp = 10;
            b.Damage(firstSummon, 9999);
            Assert.IsFalse(firstSummon.Alive);
            Assert.AreEqual(10, b.Party[0].unit.hp, "R013 did not heal on summon kill");

            // Breaking Zoroa's P2 HP bar kills remaining living summons and ends the battle in Victory
            b.Damage(z, ZoroaHp);
            Assert.IsFalse(z.Alive);
            Assert.IsTrue(b.Enemies.Where(e => e.IsSummon).All(e => !e.Alive), "remaining summons die with Zoroa");
            Assert.AreEqual(Outcome.Victory, b.Outcome);
        }

        [Test]
        public void ZoroaChargeGapPreventsBackToBackChargesAndCapsChargeShareAtTwoThirds()
        {
            foreach (var zoroaId in new[] { "zoroa_p1", "zoroa_p2" })
            {
                var b = NewBattle(1, zoroaId);
                var z = b.Enemies[0];
                var turns = new List<(bool charging, bool released, string move)>();
                while (turns.Count < 12 && b.Outcome == Outcome.None)
                {
                    var c = b.NextTurn();
                    if (c == null) continue;
                    if (c == z)
                    {
                        bool wasCharged = z.Charge != null;
                        // HUD intent at round prep must match the action actually chosen.
                        if (z.Intent != null)
                            Assert.AreEqual(z.Intent.charging, !wasCharged && z.Intent.move.telegraph, $"{zoroaId}: HUD intent matches charge state");
                        var act = EnemyAI.Choose(b, z);
                        Assert.IsNull(b.Validate(act));
                        bool released = wasCharged && !act.charging && act.attack.telegraph;
                        turns.Add((act.charging, released, act.attack.name));
                        b.Perform(act, 1f, Enumerable.Repeat(QteResult.Poor, Math.Max(1, act.attack.hits)).ToList());
                    }
                    else
                    {
                        c.unit.hp = c.Stats.maxHp;
                        c.statuses.RemoveAll(s => s.id == StatusId.Silence);
                        c.StatusController.RemoveStatus(StatusId.Silence);
                        b.Perform(BattleAction.Guard(c));
                    }
                }

                Assert.AreEqual(12, turns.Count, zoroaId);
                for (int i = 1; i < turns.Count; i++)
                {
                    if (turns[i - 1].released)
                        Assert.IsFalse(turns[i].charging, $"{zoroaId} turn {i + 1}: cannot start a charge on the turn right after a release");
                }
                int chargeOrRelease = turns.Count(t => t.charging || t.released);
                Assert.LessOrEqual(chargeOrRelease, 8, $"{zoroaId}: share of charge or release turns over 12 turns is at most 66% (8/12)");
            }
        }

        [Test]
        public void OssuaryKnightCyclesChargeReleaseAndOssuarySlam()
        {
            var b = NewBattle(1, "royal_ossuary_knight");
            var knight = b.Enemies[0];
            knight.cooldowns.Clear(); // start at the charge-ready state to test the repeating 3-turn cycle
            var seen = new List<string>();
            while (seen.Count < 6 && b.Outcome == Outcome.None)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c == knight)
                {
                    var act = EnemyAI.Choose(b, knight);
                    Assert.IsNull(b.Validate(act));
                    seen.Add((act.charging ? "charge:" : "") + act.attack.name);
                    b.Perform(act, 1f, Enumerable.Repeat(QteResult.Poor, Math.Max(1, act.attack.hits)).ToList());
                }
                else
                {
                    c.unit.hp = c.Stats.maxHp;
                    b.Perform(BattleAction.Guard(c));
                }
            }
            CollectionAssert.AreEqual(
                new[] { "charge:Graveward Charge", "Graveward Charge", "Ossuary Slam", "charge:Graveward Charge", "Graveward Charge", "Ossuary Slam" },
                seen);
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

        // ---- v1.9 random Elites ----

        [Test]
        public void ConfessorHealsTheMostHurtAllyOnlyAtOrBelow65Percent()
        {
            var b = NewBattle(1, "reliquary_confessor", "gravebound_archer");
            var conf = b.Enemies[0];
            var archer = b.Enemies[1];
            AdvanceTo(b, c => c == conf);
            archer.unit.hp = (int)(archer.Stats.maxHp * 0.70f);
            Assert.AreEqual("Reliquary Staff", PriorityBrain.Select(b, conf).move.name, "70% HP is not low enough");

            archer.unit.hp = (int)(archer.Stats.maxHp * 0.60f);
            var plan = PriorityBrain.Select(b, conf);
            Assert.AreEqual("Mending Litany", plan.move.name);
            Assert.AreSame(archer, plan.target, "lowest HP percentage ally");

            var act = EnemyAI.Use(b, conf, plan.move, plan.target);
            Assert.IsNull(b.Validate(act));
            Assert.IsNull(QteFactory.Defense(act, QteAssist.Standard), "healing has no Defensive QTE");
            int before = archer.unit.hp, mp = conf.unit.mp;
            b.Perform(act);
            Assert.AreEqual(before + (int)Math.Floor(archer.Stats.maxHp * 0.15f), archer.unit.hp, "15% Max HP");
            Assert.AreEqual(mp - 24, conf.unit.mp);
            Assert.AreEqual(3, conf.Cooldown(Combatant.MoveKey(plan.move)));
        }

        [Test]
        public void ConfessorHoldsArcaneChargeWhileAnAllyIsCritical()
        {
            var b = NewBattle(1, "reliquary_confessor", "gravebound_archer");
            var conf = b.Enemies[0];
            var archer = b.Enemies[1];
            conf.unit.mp = 20; // <= 30% Max MP, and too little for Mending Litany
            Assert.AreEqual("Arcane Charge", PriorityBrain.Select(b, conf).move.name);
            archer.unit.hp = (int)(archer.Stats.maxHp * 0.35f);
            Assert.AreEqual("Reliquary Staff", PriorityBrain.Select(b, conf).move.name, "ally <= 40% HP blocks Arcane Charge");
        }

        [Test]
        public void ArcanistLancesWhileItCanAndChargesAtAQuarterMp()
        {
            var b = NewBattle(1, "graveglass_arcanist");
            var arc = b.Enemies[0];
            Assert.AreEqual("Umbral Lance", PriorityBrain.Select(b, arc).move.name);
            arc.unit.mp = 44; // < 25% of 180
            Assert.AreEqual("Arcane Charge", PriorityBrain.Select(b, arc).move.name);
            arc.unit.mp = 50;
            Assert.AreEqual("Umbral Lance", PriorityBrain.Select(b, arc).move.name);
        }

        [Test]
        public void GravefallAddsThirtyBreakPowerOnTopOfTheHit()
        {
            int BreakTaken(string move)
            {
                var b = NewBattle(3, "tombstone_colossus");
                var col = AdvanceTo(b, c => c.IsEnemy);
                var t = b.Party[0];
                t.unit.hp = 9999;
                int before = t.CurrentBreakGauge;
                b.Perform(BattleAction.Strike(col, Move("tombstone_colossus", move), t), 1f, new[] { QteResult.Poor });
                Assert.IsFalse(t.IsBroken, "keep the comparison inside the gauge");
                return before - t.CurrentBreakGauge;
            }
            // The extra 30 Break Power is raw: the player's Stagger Resistance (% reduction) mitigates it like any enemy break.
            int stagger = NewBattle(3, "tombstone_colossus").Party[0].StaggerRes;
            int extra = Math.Max(1, (int)Math.Round(30 * 100f / (100f + Math.Max(0, stagger))));
            Assert.AreEqual(BreakTaken("Monument Fist") + extra, BreakTaken("Gravefall"));
            Assert.IsFalse(Move("tombstone_colossus", "Gravefall").HasHeavy);
        }

        [Test]
        public void BellWraithTelegraphsFuneralChorusOnlyWithThreeActiveMembers()
        {
            var b = NewBattle(1, "mourning_bell_wraith");
            var w = b.Enemies[0];
            var plan = PriorityBrain.Select(b, w);
            Assert.AreEqual("Funeral Chorus", plan.move.name);
            Assert.IsTrue(plan.charging, "telegraphed 1 turn prior");
            Assert.LessOrEqual(Move("mourning_bell_wraith", "Funeral Chorus").hitDamage[0],
                Move("mourning_bell_wraith", "Tolling Note").hitDamage[0] * 0.60f, "multi-target coefficient cap");
            foreach (var p in b.Party.Skip(2)) p.unit.hp = 0;
            Assert.AreEqual("Tolling Note", PriorityBrain.Select(b, w).move.name, "fewer than 3 active");
        }

        [Test]
        public void RandomElitePoolIsTheFiveNewElitesAndMinibossesStayOnSpecialNodes()
        {
            var pool = new[] { "d1_e8", "d1_e9", "d1_e10", "d1_e11", "d1_e12" };
            var seen = new HashSet<string>();
            for (int seed = 1; seed <= 60; seed++)
                for (int stage = 2; stage <= 3; stage++)
                    foreach (var n in MapGenerator.Generate(seed, 1, stage).nodes.Where(n => n.type == NodeType.Elite))
                    {
                        CollectionAssert.Contains(pool, n.encounter, $"seed {seed} stage {stage}");
                        seen.Add(n.encounter);
                    }
            CollectionAssert.AreEquivalent(pool, seen, "every new Elite encounter can appear");
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
                ["d1_e6"] = (Tier.Elite, new[] { "soul_reaper", "forgotten_militia", "soulbound_hound", "crypt_leech" }),
                ["d1_e7"] = (Tier.Elite, new[] { "royal_ossuary_knight", "possessed_royal_guard", "gravebound_archer", "hollow_squire" }),
                ["d1_e8"] = (Tier.Elite, new[] { "reliquary_confessor", "gravebound_archer" }),
                ["d1_e9"] = (Tier.Elite, new[] { "graveglass_arcanist", "cinder_wisp" }),
                ["d1_e10"] = (Tier.Elite, new[] { "crownless_duelist", "forgotten_militia" }),
                ["d1_e11"] = (Tier.Elite, new[] { "tombstone_colossus", "hollow_squire" }),
                ["d1_e12"] = (Tier.Elite, new[] { "mourning_bell_wraith", "soulbound_hound" }),
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
            float heavyTaken = QteConfig.DefenseTaken(QteResult.Perfect, true, QteConfig.BlockOf(perfect.target));
            Assert.AreEqual(poor.amount * heavyTaken, perfect.amount, 1.01f, "Perfect vs Heavy = Good-level reduction by Block Power");

            var b2 = NewBattle(9, "royal_ossuary_knight");
            var k2 = AdvanceTo(b2, c => c.IsEnemy);
            b2.Party[0].guarding = true; // a full parry needs Guard or a shield (Bab 12 §3.5)
            var slam = b2.Perform(BattleAction.Strike(k2, Move("royal_ossuary_knight", "Ossuary Slam"), b2.Party[0]), 1f, new[] { QteResult.Perfect })[0];
            Assert.IsTrue(slam.blocked, "non-Heavy Perfect while guarding still negates");
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
            b3.Damage(z, ZoroaHp);
            Assert.IsFalse(z.IsBroken);
            Assert.AreEqual(200, z.CurrentBreakGauge);
        }

        [Test]
        public void ZoroaFinalReapingBreaksDefaultHeroOnAllMissAndDoesNotOnAllGood()
        {
            Combatant RunReaping(QteResult qte)
            {
                var b = NewBattle(1, "zoroa_p1");
                var z = AdvanceTo(b, c => c.IsEnemy);
                var hero = b.Party[0];
                hero.unit.hp = 9999;
                hero.guarding = false;
                Assert.AreEqual(Combatant.PlayerPoiseGauge, hero.CurrentBreakGauge);
                b.Perform(BattleAction.Strike(z, Move("zoroa_p1", "Final Reaping"), hero), 1f, Enumerable.Repeat(qte, 4).ToList());
                return hero;
            }

            var missed = RunReaping(QteResult.Miss);
            Assert.IsTrue(missed.IsBroken, "all-Miss Final Reaping must break a 60-Poise hero");
            Assert.AreEqual(0, missed.CurrentBreakGauge);

            var good = RunReaping(QteResult.Good);
            Assert.IsFalse(good.IsBroken, "all-Good Final Reaping must not break a 60-Poise hero");
            Assert.Greater(good.CurrentBreakGauge, 0);
        }
    }
}
