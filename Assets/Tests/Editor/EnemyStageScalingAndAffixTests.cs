using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    [TestFixture]
    public class EnemyStageScalingAndAffixTests
    {
        static IEnumerable<CharacterDef> Party => Database.Party.Select(id => Database.Characters[id]);

        static RunState RunAtStage(int stage, int seed = 42, bool prologue = false)
        {
            var map = prologue
                ? Prologue.Map()
                : new MapDef
                {
                    name = "Test",
                    act = 1,
                    stage = stage,
                    totalStages = 3,
                    nodes = new[] { new NodeDef { id = "n0", type = NodeType.Battle, floor = 0, next = Array.Empty<string>() } }
                };
            return new RunState(map, Party, seed);
        }

        [Test]
        public void Stage1AndPrologue_ApplyNoStageScalingAndZeroAffixes()
        {
            var s1Run = RunAtStage(1);
            var prologueRun = RunAtStage(1, prologue: true);
            var elite = Database.Enemies["soul_reaper"];
            var boss = Database.Enemies["zoroa_p1"];

            var baseElite = new Unit(elite, 1);
            var s1Battle = new Battle(s1Run.Party, new[] { elite, boss }, new List<string>(), new Random(1), s1Run);
            Assert.AreEqual(1, s1Battle.Stage);
            Assert.AreEqual(baseElite.stats.maxHp, s1Battle.Enemies[0].Stats.maxHp);
            Assert.AreEqual(baseElite.stats.atk, s1Battle.Enemies[0].Stats.atk);
            Assert.AreEqual(baseElite.Derived.physicalBreakPower, s1Battle.Enemies[0].BaseDerived.physicalBreakPower);
            CollectionAssert.IsEmpty(s1Battle.Enemies[0].EnemyAffixes, "Stage 1 Elite gets 0 affixes");
            CollectionAssert.IsEmpty(s1Battle.Enemies[1].EnemyAffixes, "Stage 1 Boss gets 0 affixes");

            var proBattle = new Battle(prologueRun.Party, new[] { Database.Enemies["aetherion"] }, new List<string>(), new Random(1), prologueRun);
            Assert.AreEqual(1, proBattle.Stage);
            CollectionAssert.IsEmpty(proBattle.Enemies[0].EnemyAffixes, "Prologue boss gets 0 affixes");
        }

        [Test]
        public void Stage2AndStage3_ScaleHpAtkAndBreakPower_LeavingDefAndSpdUnchanged()
        {
            var def = Database.Enemies["soul_reaper"];
            var s1 = new Unit(def, 1, stage: 1);
            var s2 = new Unit(def, 1, stage: 2);
            var s3 = new Unit(def, 1, stage: 3);

            Assert.AreEqual(Unit.ScaleVariantStat(s1.stats.maxHp, EnemyAffixSystem.Stage2HpMult), s2.stats.maxHp);
            Assert.AreEqual(Unit.ScaleVariantStat(s1.stats.atk, EnemyAffixSystem.Stage2AtkMult), s2.stats.atk);
            Assert.AreEqual(s1.stats.def, s2.stats.def, "DEF unchanged by stage escalation");
            Assert.AreEqual(s1.stats.spd, s2.stats.spd, "SPD unchanged by stage escalation");
            Assert.AreEqual(s1.Derived.physicalBreakPower + EnemyAffixSystem.Stage2BreakPowerBonus, s2.Derived.physicalBreakPower);

            Assert.AreEqual(Unit.ScaleVariantStat(s1.stats.maxHp, EnemyAffixSystem.Stage3HpMult), s3.stats.maxHp);
            Assert.AreEqual(Unit.ScaleVariantStat(s1.stats.atk, EnemyAffixSystem.Stage3AtkMult), s3.stats.atk);
            Assert.AreEqual(s1.stats.def, s3.stats.def);
            Assert.AreEqual(s1.stats.spd, s3.stats.spd);
            Assert.AreEqual(s1.Derived.physicalBreakPower + EnemyAffixSystem.Stage3BreakPowerBonus, s3.Derived.physicalBreakPower);
        }

        [Test]
        public void AffixCounts_AreDeterministicAndRespectTierStageAndSummonRules()
        {
            var normal = Database.Enemies["hollow_squire"];
            var elite = Database.Enemies["soul_reaper"];
            var boss = Database.Enemies["zoroa_p1"];

            var s2RunA = RunAtStage(2, seed: 77);
            var s2RunB = RunAtStage(2, seed: 77);
            var b2A = new Battle(s2RunA.Party, new[] { normal, elite }, new List<string>(), new Random(1), s2RunA);
            var b2B = new Battle(s2RunB.Party, new[] { normal, elite }, new List<string>(), new Random(1), s2RunB);
            CollectionAssert.IsEmpty(b2A.Enemies[0].EnemyAffixes, "Stage 2 Normal enemy gets 0 affixes");
            Assert.AreEqual(1, b2A.Enemies[1].EnemyAffixes.Count, "Stage 2 Elite gets 1 affix");
            CollectionAssert.AreEqual(b2A.Enemies[1].EnemyAffixes, b2B.Enemies[1].EnemyAffixes, "Same seed rolls identical affixes");

            var s3Run = RunAtStage(3, seed: 77);
            var b3 = new Battle(s3Run.Party, new[] { normal, elite, boss }, new List<string>(), new Random(1), s3Run);
            CollectionAssert.IsEmpty(b3.Enemies[0].EnemyAffixes, "Stage 3 Normal enemy gets 0 affixes");
            Assert.AreEqual(1, b3.Enemies[1].EnemyAffixes.Count, "Stage 3 Elite gets 1 affix");
            Assert.AreEqual(2, b3.Enemies[2].EnemyAffixes.Count, "Stage 3 Boss gets 2 distinct affixes");
            Assert.AreEqual(2, b3.Enemies[2].EnemyAffixes.Distinct().Count(), "Boss affixes must be distinct");

            // Summons inherit Stage HP scaling but receive 0 affixes
            b3.Summon(b3.Enemies[2], elite);
            var summoned = b3.Enemies.Last();
            Assert.IsTrue(summoned.IsSummon);
            CollectionAssert.IsEmpty(summoned.EnemyAffixes, "Summons must never receive affixes");
            int expectedSummonHp = (int)Math.Round(new Unit(elite, 1, stage: 3).stats.maxHp * Dungeon1Roster.SummonHpMult);
            Assert.AreEqual(expectedSummonHp, summoned.Stats.maxHp, "Summons inherit Stage 3 HP scaling");
        }

        [Test]
        public void BossPhase2_PreservesStageScalingAndAffixes()
        {
            var s3Run = RunAtStage(3, seed: 12);
            var b = new Battle(s3Run.Party, new[] { Database.Enemies["zoroa_p1"] }, new List<string>(), new Random(1), s3Run);
            var zoroa = b.Enemies[0];
            var p1Affixes = zoroa.EnemyAffixes.ToList();
            Assert.AreEqual(2, p1Affixes.Count);

            zoroa.AdvanceToPhase(Database.Enemies["zoroa_p2"], b.Dungeon, b.Stage);
            Assert.AreEqual(new Unit(Database.Enemies["zoroa_p2"], 1, stage: 3).stats.maxHp, zoroa.Stats.maxHp,
                "Boss Phase 2 preserves Stage 3 HP scaling");
            CollectionAssert.AreEqual(p1Affixes, zoroa.EnemyAffixes, "Boss Phase 2 keeps its affixes");
            if (zoroa.HasAffix(EnemyAffix.Resolute))
            {
                int expectedBreak = (int)Math.Round(Database.Enemies["zoroa_p2"].maxBreakGauge * EnemyAffixSystem.ResoluteBreakGaugeMult);
                Assert.AreEqual(expectedBreak, zoroa.MaxBreakGauge);
            }
        }

        [Test]
        public void AllFiveAffixes_ApplyTheirExactCombatMechanics()
        {
            var run = RunAtStage(1);
            var b = new Battle(run.Party, new[] { Database.Enemies["soul_reaper"] }, new List<string>(), new Random(1), run);
            var hero = b.Party[0];
            var foe = b.Enemies[0];

            // 1. Fortified: -10% damage taken on both legacy Deal and DamageModifiers (never double-applied)
            foe.EnemyAffixes.Add(EnemyAffix.Fortified);
            EnemyAffixSystem.ApplyInitialModifiers(foe, resetCurrentBreakGauge: false);
            Assert.AreEqual(EnemyAffixSystem.FortifiedDamageTakenMult, foe.DamageModifiers.GetMultiplier(DamageModifierScope.AllDamageTaken), 1e-4f);
            int legacyAmt = 100;
            EnemyAffixSystem.ModifyDamageReceivedLegacy(foe, ref legacyAmt);
            Assert.AreEqual(90, legacyAmt);

            // 2. Relentless: +10% damage dealt on enemy attacks
            foe.EnemyAffixes.Add(EnemyAffix.Relentless);
            int enemyDmg = 100;
            EnemyAffixSystem.ModifyDamageDealt(foe, ref enemyDmg);
            Assert.AreEqual(110, enemyDmg);

            // 3. PoiseBreaker: +25% Break/Poise damage dealt
            foe.EnemyAffixes.Add(EnemyAffix.PoiseBreaker);
            int rawBreak = 20;
            EnemyAffixSystem.ModifyBreakPower(foe, ref rawBreak);
            Assert.AreEqual(25, rawBreak);

            // 4. Resolute: +20% Max Break Gauge
            int baseBreak = foe.enemy.maxBreakGauge;
            foe.EnemyAffixes.Add(EnemyAffix.Resolute);
            EnemyAffixSystem.ApplyInitialModifiers(foe, resetCurrentBreakGauge: true);
            Assert.AreEqual((int)Math.Round(baseBreak * EnemyAffixSystem.ResoluteBreakGaugeMult), foe.MaxBreakGauge);
            Assert.AreEqual(foe.MaxBreakGauge, foe.CurrentBreakGauge);

            // 5. Vigor: heals 4% Max HP once per round while not Broken (skips Turn B on double-turn bosses and pauses while Broken)
            foe.EnemyAffixes.Add(EnemyAffix.Vigor);
            foe.unit.hp = foe.Stats.maxHp / 2;
            int beforeHeal = foe.unit.hp;
            foe.turnsTakenThisRound = 1;
            EnemyAffixSystem.OnTurnStart(foe);
            int expectedHeal = (int)Math.Round(foe.Stats.maxHp * EnemyAffixSystem.VigorTurnStartHealFraction);
            Assert.AreEqual(beforeHeal + expectedHeal, foe.unit.hp, "Vigor heals 4% Max HP on Turn A when not Broken");

            int hpAfterTurnA = foe.unit.hp;
            foe.turnsTakenThisRound = 2;
            EnemyAffixSystem.OnTurnStart(foe);
            Assert.AreEqual(hpAfterTurnA, foe.unit.hp, "Vigor must not heal a second time on Turn B of a double-turn boss");

            foe.turnsTakenThisRound = 1;
            foe.AddStatus(StatusId.Broken, 1, 1.5f, hero);
            int hpWhileBroken = foe.unit.hp;
            EnemyAffixSystem.OnTurnStart(foe);
            Assert.AreEqual(hpWhileBroken, foe.unit.hp, "Vigor must not heal while Broken");
        }

        [Test]
        public void HiddenAffixes_AreNotExposedInAnalysisInspector()
        {
            var s3Run = RunAtStage(3, seed: 5);
            var b = new Battle(s3Run.Party, new[] { Database.Enemies["zoroa_p1"] }, new List<string>(), new Random(1), s3Run);
            var zoroa = b.Enemies[0];
            Assert.AreEqual(2, zoroa.EnemyAffixes.Count);

            var view = AnalysisInspector.BuildEnemyView(b, zoroa);
            Assert.IsNotNull(view);
            foreach (EnemyAffix af in Enum.GetValues(typeof(EnemyAffix)))
            {
                string afName = af.ToString();
                Assert.IsFalse(view.activeEffects.Any(e =>
                    (e.label != null && e.label.IndexOf(afName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (e.subtitle != null && e.subtitle.IndexOf(afName, StringComparison.OrdinalIgnoreCase) >= 0)),
                    $"Hidden affix '{afName}' must not appear in Analysis activeEffects");
                Assert.IsFalse((view.tierBadge ?? "").IndexOf(afName, StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsFalse((view.role ?? "").IndexOf(afName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
    }
}
