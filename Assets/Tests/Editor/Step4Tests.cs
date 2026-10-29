using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    // Characters, skills, elements, statuses, enemy brains and boss phases.
    public class Step4Tests
    {
        static Battle NewBattle(int seed, params string[] enemies) => NewBattle(seed, enemies.Select(id => Database.Enemies[id]));

        static Battle NewBattle(int seed, IEnumerable<EnemyDef> enemies) => new Battle(
            Database.Party.Select(id => new Unit(Database.Characters[id])), enemies,
            new List<string>(Database.StartingItems), new Random(seed));

        // Everyone guards (no damage, no RNG) until `who` is up.
        static Combatant AdvanceTo(Battle b, Func<Combatant, bool> who)
        {
            for (int i = 0; i < 100; i++)
            {
                var c = b.NextTurn();
                if (c != null && who(c)) return c;
                if (c != null) b.Perform(BattleAction.Guard(c));
            }
            throw new Exception("never reached");
        }

        static Combatant AdvanceTo(Battle b, Combatant who) => AdvanceTo(b, c => c == who);
        static Combatant P(Battle b, string name) => b.Party.First(c => c.name == name);
        static SkillDef S(string id) => Database.Skills[id];
        static EnemyAttack Move(string enemy, string name) => Database.Enemies[enemy].attacks.First(a => a.name == name);

        static EnemyDef Dummy(Affinities affinities) => new EnemyDef
        {
            id = "dummy", shortName = "Dummy", tag = "DM", tier = Tier.Normal, stats = new Stats(200, 0, 1, 0, 1, 0),
            affinities = affinities, sprite = Art.Skeleton, attacks = new[] { new EnemyAttack { name = "Poke" } },
        };

        [Test]
        public void FourCharactersWithDistinctRolesAndUsableSkills()
        {
            var chars = Database.Party.Select(id => Database.Characters[id]).ToList();
            Assert.AreEqual(4, chars.Select(c => c.role).Distinct().Count());
            Assert.AreEqual("knight", chars.OrderByDescending(c => c.stats.maxHp).First().id, "knight has the most HP");
            Assert.AreEqual("knight", chars.OrderByDescending(c => c.stats.def).First().id, "and the most defense");
            Assert.AreEqual("rogue", chars.OrderByDescending(c => c.stats.spd).First().id, "rogue is fastest");
            Assert.AreEqual("mage", chars.OrderByDescending(c => c.stats.mag).First().id, "mage hits hardest with magic");
            Assert.IsTrue(Database.Characters["healer"].skills.Select(S).Count(s => s.kind == SkillKind.Heal) >= 2);

            foreach (var who in new[] { "Knight", "Rogue", "Mage", "Healer" })
            {
                var b = NewBattle(1, "skeleton");
                var actor = AdvanceTo(b, c => c.name == who);
                Assert.IsNotEmpty(Database.Characters[actor.unit.id].description);
                Assert.AreEqual(3, actor.unit.skills.Length);
                foreach (var s in actor.unit.skills.Select(S))
                {
                    var target = s.target == TargetType.Enemy ? b.Enemies[0] : s.target == TargetType.Ally ? b.Party[0] : null;
                    Assert.IsNull(b.Validate(BattleAction.Skill(actor, s, target)), $"{who}: {s.id}");
                }
            }
        }

        [Test]
        public void SkillsUseTheExistingQteTypes()
        {
            var b = NewBattle(1, "skeleton");
            var rng = new Random(1);
            Qte For(string skill) => QteFactory.Offense(BattleAction.Skill(b.Party[0], S(skill), b.Enemies[0]), Tier.Normal, QteAssist.Standard, rng);
            foreach (var id in new[] { "shield_bash", "quick_strike", "expose" }) Assert.IsInstanceOf<TimingQte>(For(id), id);
            foreach (var id in new[] { "firebolt", "frost_shard", "arcane_burst", "heal", "mend" }) Assert.IsInstanceOf<TypingQte>(For(id), id);
            foreach (var id in new[] { "fortify", "interpose", "evasion", "blessing" }) Assert.IsNull(For(id), id + " needs no QTE");
            Assert.IsInstanceOf<TimingQte>(QteFactory.Offense(BattleAction.Attack(b.Party[0], b.Enemies[0]), Tier.Normal, QteAssist.Standard, rng));
            Assert.IsInstanceOf<AutoQte>(QteFactory.Offense(BattleAction.Skill(b.Party[0], S("firebolt"), b.Enemies[0]), Tier.Normal, QteAssist.Auto, rng));
        }

        Hit CastOn(string skill, EnemyDef enemy, out int raw)
        {
            var b = NewBattle(9, new[] { enemy });
            var mage = AdvanceTo(b, c => c.name == "Mage");
            var t = b.Enemies[0];
            raw = Rules.Magic(mage, t, S(skill).power, new Random(9)); // same first roll the battle will make
            return b.Perform(BattleAction.Skill(mage, S(skill), t))[0];
        }

        [Test]
        public void ElementalWeaknessResistanceAndImmunity()
        {
            Assert.AreEqual(1.5f, ElementConfig.Weak);
            Assert.AreEqual(0.5f, ElementConfig.Resist);
            Assert.AreEqual(0f, ElementConfig.Immune);

            var weak = CastOn("firebolt", Database.Enemies["skeleton"], out int raw);
            Assert.AreEqual(Effectiveness.Weak, weak.effect);
            Assert.AreEqual(Math.Max(1, (int)Math.Round(raw * 1.5f)), weak.amount);

            var resist = CastOn("frost_shard", Database.Enemies["skeleton"], out raw);
            Assert.AreEqual(Effectiveness.Resist, resist.effect);
            Assert.AreEqual(Math.Max(1, (int)Math.Round(raw * 0.5f)), resist.amount);

            var neutral = CastOn("firebolt", Dummy(new Affinities()), out raw);
            Assert.AreEqual(Effectiveness.Neutral, neutral.effect);
            Assert.AreEqual(raw, neutral.amount);

            var immune = CastOn("firebolt", Dummy(new Affinities { immune = new[] { Element.Fire } }), out _);
            Assert.AreEqual(Effectiveness.Immune, immune.effect);
            Assert.AreEqual(0, immune.amount);
            Assert.AreEqual(200, immune.target.unit.hp);

            // Party affinities too: Dread Howl is Dark; the Knight resists it, the Mage is weak to it.
            var b = NewBattle(1, "cursed_knight");
            var ck = AdvanceTo(b, c => c.IsEnemy);
            var hits = b.Perform(BattleAction.Strike(ck, Move("cursed_knight", "Dread Howl"), null));
            Assert.AreEqual(Effectiveness.Resist, hits.First(h => h.target.name == "Knight").effect);
            Assert.AreEqual(Effectiveness.Weak, hits.First(h => h.target.name == "Mage").effect);
            Assert.IsTrue(b.Log.Any(l => l.Contains("weakness")));
        }

        [Test]
        public void BuffsApplyAndExpireOnTheBearersTurns()
        {
            var b = NewBattle(1, "skeleton");
            var knight = AdvanceTo(b, c => c.name == "Knight");
            int baseDef = knight.Def;
            b.Perform(BattleAction.Skill(knight, S("fortify")));
            Assert.IsTrue(knight.Has(StatusId.DefenseUp));
            Assert.AreEqual((int)Math.Round(baseDef * 1.6f), knight.Def);

            AdvanceTo(b, knight);
            Assert.IsTrue(knight.Has(StatusId.DefenseUp), "2 turns: still up on the first of them");
            b.Perform(BattleAction.Guard(knight));
            AdvanceTo(b, knight);
            Assert.IsFalse(knight.Has(StatusId.DefenseUp), "gone at the start of the second");
            Assert.AreEqual(baseDef, knight.Def);
            Assert.IsTrue(b.Log.Any(l => l.Contains("Defense Up wears off")));
        }

        [Test]
        public void DebuffsLowerStatsAndSlowReordersTheNextRound()
        {
            var b = NewBattle(1, "skeleton");
            var sk = b.Enemies[0];
            var mage = AdvanceTo(b, c => c.name == "Mage");
            b.Perform(BattleAction.Skill(mage, S("frost_shard"), sk), QteConfig.PerfectPower); // Perfect guarantees the slow
            Assert.IsTrue(sk.Has(StatusId.Slow));
            Assert.Less(sk.Speed, sk.Stats.spd);
            var order = b.NextRoundOrder.Select(c => c.tag).ToList();
            Assert.Greater(order.IndexOf("SK"), order.IndexOf("KNI"), "slowed below the knight");

            var knight = AdvanceTo(b, c => c.name == "Knight");
            int def = sk.Def;
            b.Perform(BattleAction.Skill(knight, S("shield_bash"), sk));
            Assert.IsTrue(sk.Has(StatusId.DefenseDown));
            Assert.Less(sk.Def, def);

            var b2 = NewBattle(1, "cursed_knight");
            var ck = AdvanceTo(b2, c => c.IsEnemy);
            var k2 = P(b2, "Knight");
            int atk = k2.Atk;
            b2.Perform(BattleAction.Strike(ck, Move("cursed_knight", "Dread Howl"), null), 1f, new[] { QteResult.Miss });
            Assert.IsTrue(k2.Has(StatusId.AttackDown));
            Assert.Less(k2.Atk, atk);
        }

        [Test]
        public void PerfectParryAlsoBlocksTheStatus()
        {
            var b = NewBattle(1, "cursed_knight");
            var ck = AdvanceTo(b, c => c.IsEnemy);
            b.Perform(BattleAction.Strike(ck, Move("cursed_knight", "Dread Howl"), null), 1f, new[] { QteResult.Perfect });
            Assert.IsFalse(b.Party.Any(p => p.Has(StatusId.AttackDown)));
        }

        [Test]
        public void PoisonBitesAtTurnStartThenExpires()
        {
            var b = NewBattle(1, "skeleton");
            var sk = b.Enemies[0];
            sk.AddStatus(StatusId.Poison, 2, 6f, null);
            AdvanceTo(b, sk);
            Assert.AreEqual(34, sk.unit.hp);
            b.Perform(BattleAction.Guard(sk));
            AdvanceTo(b, sk);
            Assert.AreEqual(28, sk.unit.hp);
            Assert.IsFalse(sk.Has(StatusId.Poison));
            b.Perform(BattleAction.Guard(sk));
            AdvanceTo(b, sk);
            Assert.AreEqual(28, sk.unit.hp, "no damage after it wears off");

            var b2 = NewBattle(1, "skeleton");
            b2.Enemies[0].unit.hp = 3;
            b2.Enemies[0].AddStatus(StatusId.Poison, 3, 6f, null);
            for (int i = 0; i < 20 && b2.Outcome == Outcome.None; i++)
            {
                var c = b2.NextTurn();
                Assert.IsFalse(c != null && c.IsEnemy, "poisoned to death before acting");
                if (c != null) b2.Perform(BattleAction.Guard(c));
            }
            Assert.AreEqual(Outcome.Victory, b2.Outcome);
        }

        [Test]
        public void CooldownsBlockReuseUntilTheyExpire()
        {
            var b = NewBattle(1, "skeleton");
            var mage = AdvanceTo(b, c => c.name == "Mage");
            b.Perform(BattleAction.Skill(mage, S("arcane_burst")));
            AdvanceTo(b, mage);
            Assert.AreEqual("On cooldown.", b.Validate(BattleAction.Skill(mage, S("arcane_burst"))));
            Assert.IsNull(b.Validate(BattleAction.Skill(mage, S("firebolt"), b.Enemies[0])), "other skills still work");
            b.Perform(BattleAction.Guard(mage));
            AdvanceTo(b, mage);
            Assert.IsNull(b.Validate(BattleAction.Skill(mage, S("arcane_burst"))));
        }

        [Test]
        public void InterposeRedirectsSingleTargetHitsUntilTheKnightsNextTurn()
        {
            var b = NewBattle(1, "skeleton");
            var knight = AdvanceTo(b, c => c.name == "Knight");
            var mage = P(b, "Mage");
            var rogue = P(b, "Rogue");
            b.Perform(BattleAction.Skill(knight, S("interpose"), mage));
            Assert.IsTrue(knight.Has(StatusId.Guardian));

            var sk = AdvanceTo(b, c => c.IsEnemy); // round 2: the mage acts first, protection still holds
            Assert.AreEqual(knight, b.Intercept(mage));
            Assert.AreEqual(rogue, b.Intercept(rogue));
            Assert.AreEqual(knight, EnemyAI.Use(b, sk, Move("skeleton", "Rusty Slash"), mage).target);

            b.Perform(BattleAction.Guard(sk));
            AdvanceTo(b, knight);
            Assert.AreEqual(mage, b.Intercept(mage), "ends at the knight's next turn");
        }

        [Test]
        public void EvasionWidensTheRoguesReactionWindows()
        {
            var b = NewBattle(1, "skeleton");
            var rogue = AdvanceTo(b, c => c.name == "Rogue");
            b.Perform(BattleAction.Skill(rogue, S("evasion")));
            var sk = b.Enemies[0];
            var q = (DefenseQte)QteFactory.Defense(BattleAction.Strike(sk, Move("skeleton", "Rusty Slash"), rogue), QteAssist.Standard);
            Assert.AreEqual(QteConfig.Profiles[Tier.Normal].good * 1.75f, q.profile.good, 1e-5);
            var plain = (DefenseQte)QteFactory.Defense(BattleAction.Strike(sk, Move("skeleton", "Rusty Slash"), P(b, "Mage")), QteAssist.Standard);
            Assert.AreSame(QteConfig.Profiles[Tier.Normal], plain.profile);
        }

        [Test]
        public void QuickStrikeCritsOnPerfect()
        {
            var b = NewBattle(1, "skeleton");
            var rogue = b.Party[1];
            Assert.AreEqual(1.75f, Battle.QtePower(BattleAction.Skill(rogue, S("quick_strike"), b.Enemies[0]), QteResult.Perfect));
            Assert.AreEqual(1f, Battle.QtePower(BattleAction.Skill(rogue, S("quick_strike"), b.Enemies[0]), QteResult.Good));
            Assert.AreEqual(QteConfig.PerfectPower, Battle.QtePower(BattleAction.Attack(rogue, b.Enemies[0]), QteResult.Perfect));
        }

        [Test]
        public void MendOutHealsHealAndBlessingWardsTheLiving()
        {
            int Healed(string skill)
            {
                var b = NewBattle(1, "skeleton");
                var healer = AdvanceTo(b, c => c.name == "Healer");
                var knight = P(b, "Knight");
                knight.unit.hp = 1;
                b.Perform(BattleAction.Skill(healer, S(skill), knight));
                Assert.AreEqual(healer.Stats.maxMp - S(skill).mpCost, healer.unit.mp);
                return knight.unit.hp - 1;
            }
            Assert.Greater(Healed("mend"), Healed("heal"));

            var b2 = NewBattle(1, "skeleton");
            P(b2, "Rogue").unit.hp = 0;
            var h2 = AdvanceTo(b2, c => c.name == "Healer");
            b2.Perform(BattleAction.Skill(h2, S("blessing")));
            Assert.IsTrue(b2.Party.Where(p => p.Alive).All(p => p.Has(StatusId.DefenseUp)));
            Assert.IsFalse(P(b2, "Rogue").Has(StatusId.DefenseUp));
        }

        [Test]
        public void NormalEliteAndBossBrainsBehaveDifferently()
        {
            // Normal: plain attacks at random targets.
            var b = NewBattle(2, "skeleton");
            var sk = AdvanceTo(b, c => c.IsEnemy);
            for (int i = 0; i < 20; i++) Assert.AreEqual("Rusty Slash", EnemyAI.Choose(b, sk).attack.name);

            // Elite: hunts the weakest, braces when hurt (once), never braces at full health.
            var b2 = NewBattle(2, "cursed_knight");
            var ck = AdvanceTo(b2, c => c.IsEnemy);
            var mage = P(b2, "Mage");
            mage.unit.hp = 5;
            for (int i = 0; i < 30; i++)
            {
                var a = EnemyAI.Choose(b2, ck);
                Assert.IsFalse(a.attack.self, "no stance at full HP");
                if (!a.attack.allTargets) Assert.AreEqual(mage, a.target, "targets the weakest");
            }
            ck.unit.hp = 50;
            Assert.AreEqual("Iron Vow", EnemyAI.Choose(b2, ck).attack.name);
            b2.Perform(EnemyAI.Choose(b2, ck));
            Assert.IsTrue(ck.Has(StatusId.DefenseUp));
            for (int i = 0; i < 20; i++) Assert.IsFalse(EnemyAI.Choose(b2, ck).attack.self, "already braced");

            // Boss: phase 1 move set, then phase 2 at half HP with new moves and an Attack Up.
            var b3 = NewBattle(2, "warden");
            var warden = b3.Enemies[0];
            var phase1 = new HashSet<string>();
            AdvanceTo(b3, c => c.IsEnemy);
            for (int i = 0; i < 40; i++) phase1.Add(EnemyAI.Choose(b3, warden).attack.name);
            CollectionAssert.AreEquivalent(new[] { "Chain Lash", "Crushing Grip" }, phase1);
            b3.Perform(BattleAction.Guard(warden));

            var rogue = AdvanceTo(b3, c => c.name == "Rogue");
            warden.unit.hp = warden.Stats.maxHp / 2 + 3;
            b3.Perform(BattleAction.Attack(rogue, warden));
            Assert.AreEqual(2, warden.phase);
            Assert.IsTrue(warden.Has(StatusId.AttackUp));
            Assert.AreEqual(1, b3.Log.Count(l => l.Contains("Phase 2")));

            var phase2 = new HashSet<string>();
            AdvanceTo(b3, c => c.IsEnemy);
            for (int i = 0; i < 60; i++) phase2.Add(EnemyAI.Choose(b3, warden).attack.name);
            CollectionAssert.DoesNotContain(phase2, "Chain Lash");
            CollectionAssert.IsSubsetOf(phase2, new[] { "Crushing Grip", "Abyssal Gaze", "Soul Rend" });
            Assert.IsTrue(phase2.Contains("Abyssal Gaze") && phase2.Contains("Soul Rend"));

            var hero = AdvanceTo(b3, c => c.name == "Rogue");
            b3.Perform(BattleAction.Attack(hero, warden));
            Assert.AreEqual(1, b3.Log.Count(l => l.Contains("Phase 2")), "the transition happens once");
        }

        [Test]
        public void EnemyReactionQtesMatchTheirTier()
        {
            foreach (var id in new[] { "skeleton", "cursed_knight", "warden" })
            {
                var b = NewBattle(1, id);
                var e = b.Enemies[0];
                var move = e.enemy.attacks.First(m => !m.self && !m.allTargets);
                var q = (DefenseQte)QteFactory.Defense(BattleAction.Strike(e, move, b.Party[0]), QteAssist.Standard);
                Assert.AreSame(QteConfig.Profiles[e.enemy.tier], q.profile, id);
            }
            var ckb = NewBattle(1, "cursed_knight");
            Assert.IsNull(QteFactory.Defense(BattleAction.Strike(ckb.Enemies[0], Move("cursed_knight", "Iron Vow"), null), QteAssist.Standard),
                "a stance has nothing to react to");
        }

        [Test]
        public void StatusesEndWithTheBattle()
        {
            var units = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var first = new Battle(units, new[] { Database.Enemies["skeleton"] }, new List<string>(), new Random(1));
            first.Party[0].AddStatus(StatusId.DefenseUp, 5, 0.5f, null);
            first.Party[0].cooldowns["fortify"] = 3;
            var second = new Battle(units, new[] { Database.Enemies["skeleton"] }, new List<string>(), new Random(1));
            Assert.IsEmpty(second.Party[0].statuses);
            Assert.AreEqual(0, second.Party[0].Cooldown("fortify"));
            Assert.AreEqual(units[0].stats.def, second.Party[0].Def);
        }
    }
}
