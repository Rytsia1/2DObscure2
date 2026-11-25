using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class RacialFeatCombatTests
    {
        static Unit MakeHero(string raceId, string classId = "fighter")
        {
            var baseChar = Database.Characters[Database.Party[0]];
            var custom = new CharacterDef
            {
                id = $"test_{raceId}",
                name = $"Test {raceId}",
                role = "Fighter",
                raceId = raceId,
                classId = Database.Classes.ContainsKey(classId) ? classId : baseChar.classId,
                backgroundId = baseChar.backgroundId,
                baseScores = baseChar.baseScores,
                activeSkillLoadout = baseChar.activeSkillLoadout,
                skills = baseChar.skills,
                passives = baseChar.passives,
            };
            return new Unit(custom);
        }

        static void NextTurnFor(Battle b, Combatant target) => AdvanceUntilOwnerTurns(b, target, 1);

        static void AdvanceUntilOwnerTurns(Battle b, Combatant target, int ownerTurns)
        {
            int seen = 0;
            int safety = 0;
            while (seen < ownerTurns && safety++ < 200)
            {
                var next = b.NextTurn();
                if (next == null) break;
                if (next == target)
                {
                    seen++;
                    if (seen == ownerTurns) return;
                }
                if (next.IsPlayer)
                    b.Perform(BattleAction.Guard(next));
                else
                {
                    var foe = b.Opponents(next).FirstOrDefault(f => f.Alive);
                    if (foe != null) b.Perform(BattleAction.Strike(next, next.enemy.attacks[0], foe));
                }
            }
            Assert.AreEqual(ownerTurns, seen, $"Failed to advance {ownerTurns} turns for {target.name}");
        }

        [Test]
        public void ActiveFeatsAppearOnlyForTheFiveActiveRaces()
        {
            var units = new[]
            {
                MakeHero("human"),
                MakeHero("halfling"),
                MakeHero("elf"),
                MakeHero("dwarf"),
                MakeHero("dragonborn"),
                MakeHero("liichtian"),
                MakeHero("merfolk"),
            };
            var b = new Battle(units, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));

            Assert.IsFalse(b.Party[0].HasActiveRacialFeat, "Human Determination is passive");
            Assert.IsFalse(b.Party[1].HasActiveRacialFeat, "Halfling Second Chance is passive");
            for (int i = 2; i < b.Party.Count; i++)
            {
                Assert.IsTrue(b.Party[i].HasActiveRacialFeat, $"{b.Party[i].unit.race.id} should have active feat");
                Assert.IsTrue(b.Party[i].CanUseRacialFeat(out _));
            }
        }

        [Test]
        public void RacialFeatUsesTurnCostsNoMpHasNoQteAndWorksWhileSilenced()
        {
            foreach (var raceId in new[] { "elf", "dwarf", "dragonborn", "liichtian", "merfolk" })
            {
                var u1 = MakeHero(raceId);
                var u2 = MakeHero("human");
                var b = new Battle(new[] { u1, u2 }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
                var hero = b.Party[0];
                var ally = b.Party[1];

                int mpBefore = hero.unit.mp;
                hero.AddStatus(StatusId.Silence, 2, 1f, hero);

                var action = raceId == "merfolk"
                    ? BattleAction.RacialFeat(hero, ally)
                    : BattleAction.RacialFeat(hero);

                NextTurnFor(b, hero);
                Assert.IsNull(b.Validate(action), $"{raceId} feat should be valid while Silenced");
                Assert.IsNull(QteFactory.Offense(action, Tier.Normal, QteAssist.Standard, new Random(1)), "RacialFeat never uses Offensive QTE");
                Assert.IsNull(QteFactory.Defense(action, QteAssist.Standard, null, new Random(1)), "RacialFeat never uses Defensive QTE");

                int turnsBefore = hero.turnsTakenThisRound;
                b.Perform(action);

                Assert.AreEqual(mpBefore, hero.unit.mp, $"{raceId} feat must spend no MP");
                Assert.AreEqual(turnsBefore, hero.turnsTakenThisRound, "RacialFeat does not grant extra turn");
            }
        }

        [Test]
        public void Bug1_CooldownsBlockFullNTurnsOfOwner()
        {
            // Elf Fey Step: CD 4 -> blocked on T+1..T+4, ready on T+5
            var bElf = new Battle(new[] { MakeHero("elf") }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var elf = bElf.Party[0];
            bElf.Enemies[0].unit.hp = 9999;
            AdvanceUntilOwnerTurns(bElf, elf, 1);
            bElf.Perform(BattleAction.RacialFeat(elf));
            for (int t = 1; t <= 4; t++)
            {
                AdvanceUntilOwnerTurns(bElf, elf, 1);
                Assert.IsFalse(elf.CanUseRacialFeat(out _), $"Elf Fey Step must be blocked on T+{t}");
                bElf.Perform(BattleAction.Guard(elf));
            }
            AdvanceUntilOwnerTurns(bElf, elf, 1);
            Assert.IsTrue(elf.CanUseRacialFeat(out _), "Elf Fey Step must be ready on T+5");

            // Dwarf Iron Resolve: CD 3 -> blocked on T+1..T+3, ready on T+4
            var bDwarf = new Battle(new[] { MakeHero("dwarf") }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var dwarf = bDwarf.Party[0];
            bDwarf.Enemies[0].unit.hp = 9999;
            AdvanceUntilOwnerTurns(bDwarf, dwarf, 1);
            bDwarf.Perform(BattleAction.RacialFeat(dwarf));
            for (int t = 1; t <= 3; t++)
            {
                AdvanceUntilOwnerTurns(bDwarf, dwarf, 1);
                Assert.IsFalse(dwarf.CanUseRacialFeat(out _), $"Dwarf Iron Resolve must be blocked on T+{t}");
                bDwarf.Perform(BattleAction.Guard(dwarf));
            }
            AdvanceUntilOwnerTurns(bDwarf, dwarf, 1);
            Assert.IsTrue(dwarf.CanUseRacialFeat(out _), "Dwarf Iron Resolve must be ready on T+4");

            // Merfolk Ocean's Embrace: CD 4 -> blocked on T+1..T+4, ready on T+5
            var bMer = new Battle(new[] { MakeHero("merfolk") }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var mer = bMer.Party[0];
            bMer.Enemies[0].unit.hp = 9999;
            AdvanceUntilOwnerTurns(bMer, mer, 1);
            bMer.Perform(BattleAction.RacialFeat(mer, mer));
            for (int t = 1; t <= 4; t++)
            {
                AdvanceUntilOwnerTurns(bMer, mer, 1);
                Assert.IsFalse(mer.CanUseRacialFeat(out _), $"Merfolk Ocean's Embrace must be blocked on T+{t}");
                bMer.Perform(BattleAction.Guard(mer));
            }
            AdvanceUntilOwnerTurns(bMer, mer, 1);
            Assert.IsTrue(mer.CanUseRacialFeat(out _), "Merfolk Ocean's Embrace must be ready on T+5");
        }

        [Test]
        public void Bug2_LightboundRaisesStatusResistanceAndRemovesOnExpiry()
        {
            var liichtU = MakeHero("liichtian");
            var allyU = MakeHero("human");
            var b = new Battle(new[] { liichtU, allyU }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var liicht = b.Party[0];
            var ally = b.Party[1];

            float baseRes = ally.StatusController.ResistanceProfile.GetEffectiveResistance();
            NextTurnFor(b, liicht);
            b.Perform(BattleAction.RacialFeat(liicht));

            float boostedRes = ally.StatusController.ResistanceProfile.GetEffectiveResistance();
            Assert.AreEqual(Math.Min(0.85f, baseRes + 0.20f), boostedRes, 1e-4f, "Lightbound adds +20% status resistance under 85% cap");

            AdvanceUntilOwnerTurns(b, ally, 1);
            Assert.AreEqual(1, ally.LightboundTurns);
            b.Perform(BattleAction.Guard(ally));

            AdvanceUntilOwnerTurns(b, ally, 1);
            Assert.AreEqual(0, ally.LightboundTurns);
            Assert.AreEqual(baseRes, ally.StatusController.ResistanceProfile.GetEffectiveResistance(), 1e-4f, "Status resistance bonus removed on expiry");

            // 85% cap check & second battle non-stacking check
            var b2 = new Battle(new[] { liichtU, allyU }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(2));
            var liicht2 = b2.Party[0];
            var ally2 = b2.Party[1];
            Assert.AreEqual(0f, ally2.LightboundResistBonus, 1e-4f, "New battle starts with no leftover Lightbound bonus");
            ally2.StatusController.ResistanceProfile.equipmentResistance = 0.75f;
            NextTurnFor(b2, liicht2);
            b2.Perform(BattleAction.RacialFeat(liicht2));
            Assert.AreEqual(0.85f, ally2.StatusController.ResistanceProfile.GetEffectiveResistance(), 1e-4f, "Lightbound stays under 85% cap");
            Assert.AreEqual(0f, ally2.StatusController.ResistanceProfile.mentalBuffResistance, 1e-4f, "Lightbound does not touch Mental Resistance");
        }

        [Test]
        public void Bug3_OceansEmbraceGrantsDefensiveQteWindowBonusNotDefenseUpAndHealsOnceOnBreak()
        {
            var merU = MakeHero("merfolk");
            var allyU = MakeHero("human");
            var b = new Battle(new[] { merU, allyU }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var mer = b.Party[0];
            var ally = b.Party[1];
            var enemy = b.Enemies[0];

            ally.unit.hp = Math.Max(1, ally.Stats.maxHp * 3 / 4);
            int hpBefore = ally.unit.hp;

            var enemyMove = enemy.enemy.attacks[0];
            var qBefore = QteFactory.Defense(BattleAction.Strike(enemy, enemyMove, ally), QteAssist.Standard, null, new Random(1)) as DefenseQte;

            AdvanceUntilOwnerTurns(b, mer, 1);
            b.Perform(BattleAction.RacialFeat(mer, ally));
            Assert.IsFalse(ally.Has(StatusId.DefenseUp), "Ocean's Embrace must NOT apply DefenseUp");
            Assert.IsTrue(ally.OceansEmbraceShieldActive);
            Assert.Greater(ally.unit.shield, 0);

            var qAfter = QteFactory.Defense(BattleAction.Strike(enemy, enemyMove, ally), QteAssist.Standard, null, new Random(1)) as DefenseQte;
            Assert.AreEqual(qBefore.TimingBonusFraction + 0.10f, qAfter.TimingBonusFraction, 1e-4f, "Ocean's Embrace adds +10% Defensive QTE window");

            // Break the shield via an enemy strike
            ally.unit.shield = 5;
            AdvanceUntilOwnerTurns(b, enemy, 1);
            var shieldBreaker = new EnemyAttack { name = "Crush", hits = 1, hitDamage = new[] { 40 } };
            b.Perform(BattleAction.Strike(enemy, shieldBreaker, ally), defense: new[] { QteResult.Miss });
            Assert.IsFalse(ally.OceansEmbraceShieldActive, "Shield break ends Ocean's Embrace immediately");
            Assert.AreEqual(0, ally.unit.shield, "Shield must be broken");
            Assert.Greater(ally.unit.hp, hpBefore - 30, "Shield break triggers 10% Max HP heal once");

            int hpAfterBreak = ally.unit.hp;
            // Expiring 3 turns on Merfolk must NOT heal a second time
            for (int t = 0; t < 3; t++)
            {
                AdvanceUntilOwnerTurns(b, mer, 1);
                b.Perform(BattleAction.Guard(mer));
            }
            Assert.AreEqual(hpAfterBreak, ally.unit.hp, "Must not double-heal after shield already broke");
        }

        [Test]
        public void Bug4_DraconicOverdriveNoDoubleDipBreakBonusAndUncleansableExhaustion()
        {
            var dragU = MakeHero("dragonborn");
            var b = new Battle(new[] { dragU }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var drag = b.Party[0];

            int baseBreak = drag.PhysicalBreak;
            NextTurnFor(b, drag);
            b.Perform(BattleAction.RacialFeat(drag));

            Assert.IsFalse(drag.Has(StatusId.AttackUp), "Draconic Overdrive must not double-dip with AttackUp");
            Assert.AreEqual((int)Math.Round(baseBreak * 1.50f), drag.PhysicalBreak, 1, "Overdrive gives +50% Break Power");

            // 3 turns expire -> Exhausted
            for (int t = 0; t < 3; t++)
            {
                AdvanceUntilOwnerTurns(b, drag, 1);
                if (t < 2) b.Perform(BattleAction.Guard(drag));
            }
            Assert.AreEqual(0, drag.DraconicOverdriveTurns);
            Assert.AreEqual(2, drag.DraconicExhaustedTurns);
            Assert.IsTrue(drag.Has(StatusId.Exhausted));
            Assert.AreEqual((int)Math.Round(baseBreak * 0.70f), drag.PhysicalBreak, 1, "Exhausted gives -30% Break Power");

            // Exhausted cannot be cleansed
            drag.StatusController.CleanseDebuffs();
            Assert.IsTrue(drag.Has(StatusId.Exhausted), "Draconic Exhausted cannot be cleansed");
        }

        [Test]
        public void AiTargetingSkipsPhasedForSingleTargetButHitsWithAoeAndRedirectsTelegraph()
        {
            var elfU = MakeHero("elf");
            var allyU = MakeHero("human");
            var b = new Battle(new[] { elfU, allyU }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(1));
            var elf = b.Party[0];
            var ally = b.Party[1];
            var enemy = b.Enemies[0];

            NextTurnFor(b, elf);
            b.Perform(BattleAction.RacialFeat(elf));
            Assert.IsTrue(elf.IsPhased);

            var ctx = PriorityBrain.BuildContext(b, enemy);
            var stMove = new EnemyAttack { name = "Single", hits = 1, hitDamage = new[] { 10 } };
            var candidates = UtilityScorer.CandidateTargets(b, enemy, stMove, ctx);
            Assert.IsFalse(candidates.Contains(elf), "Single-target AI skips Phased hero");
            Assert.IsTrue(candidates.Contains(ally));

            var aoeMove = new EnemyAttack { name = "Sweep", hits = 1, hitDamage = new[] { 10 }, allTargets = true };
            Assert.IsTrue(PriorityBrain.IsLegal(b, enemy, aoeMove, ctx, out _), "AoE still hits Phased hero");

            var redirected = b.Intercept(elf);
            Assert.AreEqual(ally, redirected, "Telegraphed attack redirects away from Phased hero");
        }
    }
}
