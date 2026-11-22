using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    public class AnalysisSystemTests
    {
        static Combatant AdvanceTo(Battle b, Combatant target)
        {
            for (int guard = 0; guard < 100; guard++)
            {
                var c = b.NextTurn();
                if (c == target) return c;
                if (c == null) break;
                b.Perform(BattleAction.Guard(c));
            }
            Assert.Fail($"Never reached turn for {target.name}");
            return null;
        }

        static SkillDef MakeMagicSkill(string id, Element element, float power = 1.5f, TargetType target = TargetType.Enemy, StatusApply[] apply = null) =>
            new SkillDef
            {
                id = id,
                name = id,
                kind = SkillKind.Magic,
                element = element,
                power = power,
                mpCost = 0,
                hits = 1,
                target = target,
                apply = apply ?? Array.Empty<StatusApply>(),
            };

        [Test]
        public void Stage1_DrainHealsByNormalDamageCappedAtMissingHpAndNeverRevives()
        {
            var hero = new Unit(Database.Characters[Database.Party[0]]);
            var enemyDef = new EnemyDef
            {
                id = "test_drain_dummy",
                name = "Drain Dummy",
                shortName = "DRN",
                tag = "DRN",
                tier = Tier.Elite,
                stats = new Stats(200, 50, 10, 10, 10, 5),
                maxBreakGauge = 50,
                affinities = new Affinities { drain = new[] { Element.Dark } },
                attacks = new[] { new EnemyAttack { name = "Tap", hits = 1, hitDamage = new[] { 5 } } },
            };
            var b = new Battle(new[] { hero }, new[] { enemyDef }, new List<string>(), new Random(1));
            var h = b.Party[0];
            var e = b.Enemies[0];

            // Injure enemy slightly (missing 10 HP) -> Drain heal must cap at missing HP (10)
            e.unit.hp = e.Stats.maxHp - 10;
            int breakBefore = e.CurrentBreakGauge;
            var darkSkill = MakeMagicSkill("test_dark_bolt", Element.Dark, 20f,
                apply: new[] { new StatusApply { id = StatusId.Burn, chance = 1f, turns = 2, magnitude = 0.1f } });
            h.unit.skillLoadout.activeSlots[0] = darkSkill.id;

            AdvanceTo(b, h);
            var hits = b.Perform(BattleAction.Skill(h, darkSkill, e));
            Assert.AreEqual(e.Stats.maxHp, e.unit.hp, "Drain heals capped at missing HP");
            Assert.AreEqual(breakBefore, e.CurrentBreakGauge, "Drain deals 0 Break damage");
            Assert.IsFalse(e.Has(StatusId.Burn), "Drain suppresses status rider");
            Assert.IsTrue(hits.Any(x => x.drained && x.effect == Effectiveness.Drain));

            // Drain never revives a dead enemy
            e.unit.hp = 0;
            Assert.IsFalse(e.Alive);
            var h2 = new Unit(Database.Characters[Database.Party[0]]);
            var b2 = new Battle(new[] { h2 }, new[] { enemyDef }, new List<string>(), new Random(1));
            var e2 = b2.Enemies[0];
            e2.unit.hp = 0;
            var dmgRes = DamageCalculator.Calculate(b2.Party[0].CreateDamageContext(e2, new[] { new DamageComponent(DamageType.Elemental, ElementType.Shadow, 50f, 1f) }));
            Assert.IsTrue(dmgRes.FirstHit.anyComponentDrained);
            Assert.AreEqual(0, dmgRes.totalDamage);
        }

        [Test]
        public void Stage1_ReflectAppliesAttackerAffinityCapsAt35PctNoQteNoLoopNoBreakNoRider()
        {
            var hero = new Unit(Database.Characters[Database.Party[0]]);

            var enemyDef = new EnemyDef
            {
                id = "test_reflect_dummy",
                name = "Reflect Dummy",
                shortName = "RFL",
                tag = "RFL",
                tier = Tier.Elite,
                stats = new Stats(200, 50, 10, 5, 5, 5),
                maxBreakGauge = 60,
                affinities = new Affinities { reflect = new[] { Element.Arcane } },
                attacks = new[] { new EnemyAttack { name = "Tap", hits = 1, hitDamage = new[] { 5 } } },
            };
            var b = new Battle(new[] { hero }, new[] { enemyDef }, new List<string>(), new Random(1));
            var h = b.Party[0];
            // Give hero Reflect on Arcane too to prove reflection NEVER loops!
            h.AffinityProfile.SetArcaneAffinity(AffinityType.Reflect);
            var e = b.Enemies[0];

            int heroMaxHp = h.Stats.maxHp;
            int heroHpBefore = h.unit.hp;
            int heroPoiseBefore = h.CurrentBreakGauge;
            int enemyHpBefore = e.unit.hp;
            int enemyBreakBefore = e.CurrentBreakGauge;

            // Massive Arcane nuke so reflection hits the 35% cap
            var arcaneNuke = MakeMagicSkill("test_arcane_nuke", Element.Arcane, 20f,
                apply: new[] { new StatusApply { id = StatusId.Silence, chance = 1f, turns = 2, magnitude = 1f } });
            h.unit.skillLoadout.activeSlots[0] = arcaneNuke.id;

            AdvanceTo(b, h);
            var hits = b.Perform(BattleAction.Skill(h, arcaneNuke, e));

            Assert.AreEqual(enemyHpBefore, e.unit.hp, "Reflecting target takes 0 damage");
            Assert.AreEqual(enemyBreakBefore, e.CurrentBreakGauge, "Reflect deals 0 Break damage to target");
            Assert.IsFalse(e.Has(StatusId.Silence), "Reflect suppresses status rider");
            Assert.AreEqual(heroPoiseBefore, h.CurrentBreakGauge, "Reflect deals 0 Poise damage to attacker");

            int cap = Math.Max(1, (int)Math.Floor(heroMaxHp * AffinityResolver.ReflectCapPct));
            Assert.AreEqual(heroHpBefore - cap, h.unit.hp, "Reflected damage is capped at ReflectCapPct (35%) of attacker Max HP and never loops");
            Assert.IsTrue(h.Alive, "Hero at full HP survives a single reflected hit");
            Assert.IsTrue(hits.Any(x => x.reflected && x.effect == Effectiveness.Reflect && x.reflectedAmount == cap));

            // Attacker's own affinity applies (Immune -> 0 reflected damage)
            var immuneHero = new Unit(Database.Characters[Database.Party[0]]);
            var b2 = new Battle(new[] { immuneHero }, new[] { enemyDef }, new List<string>(), new Random(1));
            var h2 = b2.Party[0];
            h2.unit.skillLoadout.activeSlots[0] = arcaneNuke.id;
            h2.AffinityProfile.SetArcaneAffinity(AffinityType.Nullification);
            int h2Hp = h2.unit.hp;
            AdvanceTo(b2, h2);
            b2.Perform(BattleAction.Skill(h2, arcaneNuke, b2.Enemies[0]));
            Assert.AreEqual(h2Hp, h2.unit.hp, "Attacker immune to reflected element takes 0 damage");

            // No Defensive QTE on reflected damage: QteFactory.Defense returns null for hero's own Skill action
            Assert.IsNull(QteFactory.Defense(BattleAction.Skill(h, arcaneNuke, e), QteAssist.Standard, null, new Random(1)));
        }

        [Test]
        public void Stage1_NullBreakHasNoEffectOnDrainOrReflect()
        {
            var hero = new Unit(Database.Characters[Database.Party[0]]);
            var enemyDef = new EnemyDef
            {
                id = "test_nb_dr",
                name = "NB Dummy",
                shortName = "NBD",
                tag = "NBD",
                tier = Tier.Elite,
                stats = new Stats(200, 50, 10, 10, 10, 5),
                affinities = new Affinities { drain = new[] { Element.Fire }, reflect = new[] { Element.Ice } },
                attacks = new[] { new EnemyAttack { name = "Tap", hits = 1, hitDamage = new[] { 5 } } },
            };
            var b = new Battle(new[] { hero }, new[] { enemyDef }, new List<string>(), new Random(1));
            var e = b.Enemies[0];

            Assert.IsTrue(e.NullBreak.TryApply(AffinityDamageKey.Elemental(ElementType.Fire), 1));
            Assert.AreEqual(Effectiveness.Drain, AffinityResolver.Get(e, Element.Fire), "Null Break must NOT affect Drain");
            Assert.AreEqual(AffinityType.Drain, AffinityResolver.Get(e.AffinityProfile, DamageType.Elemental, ElementType.Fire, e.NullBreak));

            e.NullBreak.Clear();
            Assert.IsTrue(e.NullBreak.TryApply(AffinityDamageKey.Elemental(ElementType.Ice), 1));
            Assert.AreEqual(Effectiveness.Reflect, AffinityResolver.Get(e, Element.Ice), "Null Break must NOT affect Reflect");
            Assert.AreEqual(AffinityType.Reflect, AffinityResolver.Get(e.AffinityProfile, DamageType.Elemental, ElementType.Ice, e.NullBreak));
        }

        [Test]
        public void Stage1_AoeResolvesPerTargetAcrossDrainReflectAndNormal()
        {
            var hero = new Unit(Database.Characters[Database.Party[0]]);
            var drainDef = new EnemyDef
            {
                id = "aoe_drain", name = "Drainer", shortName = "DR", tag = "DR", tier = Tier.Elite,
                stats = new Stats(200, 50, 10, 5, 5, 5),
                affinities = new Affinities { drain = new[] { Element.Fire } },
                attacks = new[] { new EnemyAttack { name = "Tap", hits = 1, hitDamage = new[] { 5 } } },
            };
            var reflectDef = new EnemyDef
            {
                id = "aoe_reflect", name = "Reflector", shortName = "RF", tag = "RF", tier = Tier.Elite,
                stats = new Stats(200, 50, 10, 5, 5, 5),
                affinities = new Affinities { reflect = new[] { Element.Fire } },
                attacks = new[] { new EnemyAttack { name = "Tap", hits = 1, hitDamage = new[] { 5 } } },
            };
            var normalDef = new EnemyDef
            {
                id = "aoe_normal", name = "Normal", shortName = "NM", tag = "NM", tier = Tier.Normal,
                stats = new Stats(200, 50, 10, 5, 5, 5),
                affinities = Affinities.None,
                attacks = new[] { new EnemyAttack { name = "Tap", hits = 1, hitDamage = new[] { 5 } } },
            };

            var b = new Battle(new[] { hero }, new[] { drainDef, reflectDef, normalDef }, new List<string>(), new Random(1));
            var h = b.Party[0];
            var eDrain = b.Enemies[0];
            var eReflect = b.Enemies[1];
            var eNormal = b.Enemies[2];

            eDrain.unit.hp = 100;
            int heroHpBefore = h.unit.hp;
            int reflectHpBefore = eReflect.unit.hp;
            int normalHpBefore = eNormal.unit.hp;

            var fireAoe = MakeMagicSkill("test_fire_aoe", Element.Fire, 2.0f, TargetType.AllEnemies,
                apply: new[] { new StatusApply { id = StatusId.Burn, chance = 1f, turns = 2, magnitude = 0.1f } });
            h.unit.skillLoadout.activeSlots[0] = fireAoe.id;

            AdvanceTo(b, h);
            b.Perform(BattleAction.Skill(h, fireAoe));

            Assert.Greater(eDrain.unit.hp, 100, "Drainer heals on Fire AoE");
            Assert.IsFalse(eDrain.Has(StatusId.Burn), "Drainer receives no status rider");
            Assert.AreEqual(reflectHpBefore, eReflect.unit.hp, "Reflector takes 0 damage on Fire AoE");
            Assert.IsFalse(eReflect.Has(StatusId.Burn), "Reflector receives no status rider");
            Assert.Less(h.unit.hp, heroHpBefore, "Hero takes reflected Fire damage from Reflector");
            Assert.Less(eNormal.unit.hp, normalHpBefore, "Normal target takes Fire AoE damage");
            Assert.IsTrue(eNormal.Has(StatusId.Burn), "Normal target still receives status rider");
        }

        [Test]
        public void Stage2_PerStatusResistanceClampReductionsImmunityAndTierCutoffs()
        {
            var p = new StatusResistanceProfile { baseResistance = 0.30f };

            // Zero perStatus matches universal resistance
            Assert.AreEqual(0.30f, p.GetEffectiveResistance(StatusId.Burn), 1e-5f);
            Assert.AreEqual(StatusResistTier.Normal, p.GetTier(StatusId.Burn));

            // Positive perStatus clamps at 0.75 cap
            p.SetPerStatusResistance(StatusId.Fear, 0.60f); // 0.30 + 0.60 -> clamped to 0.75
            Assert.AreEqual(0.75f, p.GetEffectiveResistance(StatusId.Fear), 1e-5f);
            Assert.AreEqual(StatusResistTier.Resistant, p.GetTier(StatusId.Fear));

            // Negative perStatus reduces universal and floors at 0
            p.SetPerStatusResistance(StatusId.Bleed, -0.20f); // 0.30 - 0.20 = 0.10
            Assert.AreEqual(0.10f, p.GetEffectiveResistance(StatusId.Bleed), 1e-5f);
            Assert.AreEqual(StatusResistTier.Vulnerable, p.GetTier(StatusId.Bleed));

            p.SetPerStatusResistance(StatusId.Poison, -0.50f); // 0.30 - 0.50 -> floored to 0
            Assert.AreEqual(0.0f, p.GetEffectiveResistance(StatusId.Poison), 1e-5f);

            // Multiplicative reductions apply after clamp
            p.AddReduction(0.50f);
            Assert.AreEqual(0.375f, p.GetEffectiveResistance(StatusId.Fear), 1e-5f);
            Assert.AreEqual(0.05f, p.GetEffectiveResistance(StatusId.Bleed), 1e-5f);
            Assert.AreEqual(1f - 0.375f, p.CalculateFinalApplicationChance(1f, StatusId.Fear), 1e-5f);

            // Immunity stays separate and is never bypassed by reductions
            p.SetImmune(StatusId.Burn, true);
            Assert.IsTrue(p.IsImmune(StatusId.Burn));
            Assert.AreEqual(0f, p.CalculateFinalApplicationChance(1f, StatusId.Burn), 1e-5f);
            Assert.AreEqual(StatusResistTier.Immune, p.GetTier(StatusId.Burn));

            // Boss mental immunity still applies
            Assert.IsTrue(p.IsImmune(StatusId.Despair, isBoss: true));
            Assert.AreEqual(StatusResistTier.Immune, p.GetTier(StatusId.Despair, isBoss: true));

            // Exact tier cut-offs (-10% / +15%)
            var t = new StatusResistanceProfile();
            t.SetPerStatusResistance(StatusId.Bleed, -0.10f);
            Assert.AreEqual(StatusResistTier.Vulnerable, t.GetTier(StatusId.Bleed), "-10% is Vulnerable");
            t.SetPerStatusResistance(StatusId.Poison, -0.09f);
            Assert.AreEqual(StatusResistTier.Normal, t.GetTier(StatusId.Poison), "-9% is Normal");
            t.SetPerStatusResistance(StatusId.Stun, 0.15f);
            Assert.AreEqual(StatusResistTier.Normal, t.GetTier(StatusId.Stun), "+15% is Normal");
            t.SetPerStatusResistance(StatusId.Silence, 0.16f);
            Assert.AreEqual(StatusResistTier.Resistant, t.GetTier(StatusId.Silence), "+16% is Resistant");
        }

        [Test]
        public void Stage3_Dungeon1AffinityTablePassesTierLimitsAndClassCoverage()
        {
            // Collect playable classes per damaging element across Database.Skills
            var classesByElement = new Dictionary<Element, HashSet<string>>();
            foreach (var sk in Database.Skills.Values)
            {
                if (string.IsNullOrEmpty(sk.classId)) continue;
                if (sk.kind != SkillKind.Physical && sk.kind != SkillKind.Magic) continue;
                if (!classesByElement.TryGetValue(sk.element, out var set))
                    classesByElement[sk.element] = set = new HashSet<string>();
                set.Add(sk.classId);
            }

            Assert.AreEqual(16, Dungeon1Roster.Enemies.Length, "14 D1 enemies + 2 Zoroa phases");

            foreach (var e in Dungeon1Roster.Enemies.Concat(Dungeon1Roster.EnhancedVariants))
            {
                var aff = e.affinities ?? Affinities.None;
                int weakCount = aff.weak?.Length ?? 0;
                int resistCount = aff.resist?.Length ?? 0;
                int specialCount = (aff.immune?.Length ?? 0) + (aff.drain?.Length ?? 0) + (aff.reflect?.Length ?? 0);

                Assert.GreaterOrEqual(weakCount, 1, $"{e.id} must have at least 1 weakness");

                // No element may appear in more than one bucket
                var allListed = (aff.weak ?? Array.Empty<Element>())
                    .Concat(aff.resist ?? Array.Empty<Element>())
                    .Concat(aff.immune ?? Array.Empty<Element>())
                    .Concat(aff.drain ?? Array.Empty<Element>())
                    .Concat(aff.reflect ?? Array.Empty<Element>())
                    .ToList();
                Assert.AreEqual(allListed.Count, allListed.Distinct().Count(), $"{e.id} has duplicate affinity element");

                if (e.tier == Tier.Normal)
                {
                    Assert.LessOrEqual(weakCount, 2, $"Normal {e.id}: at most 2 weaknesses");
                    Assert.LessOrEqual(resistCount, 1, $"Normal {e.id}: at most 1 resistance");
                    Assert.AreEqual(0, specialCount, $"Normal {e.id}: never Null, Drain, or Reflect");
                }
                else if (e.tier == Tier.Elite)
                {
                    Assert.LessOrEqual(specialCount, 1, $"Elite/Miniboss {e.id}: at most 1 of Null/Drain/Reflect");
                }

                // Coverage rule: every weakness must be dealable by >= 2 playable classes in Database.Skills
                foreach (var w in aff.weak ?? Array.Empty<Element>())
                {
                    int clsCount = classesByElement.TryGetValue(w, out var set) ? set.Count : 0;
                    Assert.GreaterOrEqual(clsCount, 2, $"{e.id} weakness {w} must be dealable by >= 2 playable classes (found {clsCount})");
                }

                // Verify AffinityProfile matches legacy Affinities for all 10 elements
                var c = new Combatant(new Unit(e, 1), Side.Enemy, 0, e.shortName, e.tag, e);
                foreach (var el in AffinityResolver.AllElements)
                    Assert.AreEqual(aff.Against(el), AffinityResolver.GetRaw(c, el), $"{e.id} AffinityResolver mismatch on {el}");
            }

            // Enhanced forms inherit base affinities
            foreach (var enh in Dungeon1Roster.EnhancedVariants)
            {
                var baseDef = Database.Enemies[enh.baseEnemyId];
                CollectionAssert.AreEqual(baseDef.affinities.weak, enh.affinities.weak, $"{enh.id} inherits base weaknesses");
                CollectionAssert.AreEqual(baseDef.affinities.resist, enh.affinities.resist, $"{enh.id} inherits base resistances");
            }
        }

        [Test]
        public void Stage4_CodexSaveLoadSlotIsolationDefeatPersistenceAndSeparateEntries()
        {
            var prevStorage = SaveManager.Storage;
            var prevSnap = PersistenceCommitHandler.ActiveCommittedSnapshot;
            try
            {
                var mem = new MemorySaveStorage();
                SaveManager.Storage = mem;

                var partyDefs = Database.Party.Select(id => Database.Characters[id]).ToList();
                var snap1 = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(partyDefs, seed: 11);
                PersistenceCommitHandler.SetActiveCommittedSnapshot(snap1);

                var run1 = new RunState(MapGenerator.Generate(11, 1), partyDefs, 11);
                run1.RestoreFromCampaignSave(snap1);

                // Discover Fire (Weak) and Ice (Normal) and Fear (Resistant) on hollow_squire
                var squireDef = Database.Enemies["hollow_squire"];
                run1.Codex.RecordElementHit(squireDef, Element.Fire, Effectiveness.Weak);
                run1.Codex.RecordElementHit(squireDef, Element.Ice, Effectiveness.Neutral);
                run1.Codex.RecordStatusAttempt(squireDef, StatusId.Fear, StatusResistTier.Resistant);

                Assert.AreEqual(Effectiveness.Weak, run1.Codex.GetKnownElement("hollow_squire", Element.Fire));
                Assert.AreEqual(Effectiveness.Neutral, run1.Codex.GetKnownElement("hollow_squire", Element.Ice), "NORMAL is recorded too");
                Assert.AreEqual(StatusResistTier.Resistant, run1.Codex.GetKnownStatus("hollow_squire", StatusId.Fear));

                // Separate entry for Enhanced variant
                Assert.IsNull(run1.Codex.GetKnownElement("hollow_squire_enhanced", Element.Fire), "Enhanced is a separate Codex entry");

                // Commit on defeat / rollback -> discoveries survive across runs in same slot
                PersistenceCommitHandler.RollbackUncommitted(run1);
                Assert.AreEqual(Effectiveness.Weak, run1.Codex.GetKnownElement("hollow_squire", Element.Fire), "Codex survives rollback/defeat");

                // Save to Slot1 via SaveFileData JSON round-trip
                var camp1 = PersistenceCommitHandler.CreateCommittedSnapshot(run1);
                var sf1 = new SaveFileData { metadata = new SaveSlotMetadata { slotId = SaveSlotId.Slot1, isOccupied = true }, campaign = camp1 };
                Assert.IsTrue(mem.WriteSlotRaw(SaveSlotId.Slot1, UnityEngine.JsonUtility.ToJson(sf1), out _));

                // Slot2 starts fresh and does NOT see Slot1 discoveries
                var snap2 = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(partyDefs, seed: 22);
                var sf2 = new SaveFileData { metadata = new SaveSlotMetadata { slotId = SaveSlotId.Slot2, isOccupied = true }, campaign = snap2 };
                Assert.IsTrue(mem.WriteSlotRaw(SaveSlotId.Slot2, UnityEngine.JsonUtility.ToJson(sf2), out _));

                var loaded1 = UnityEngine.JsonUtility.FromJson<SaveFileData>(mem.ReadSlotRaw(SaveSlotId.Slot1));
                var loaded2 = UnityEngine.JsonUtility.FromJson<SaveFileData>(mem.ReadSlotRaw(SaveSlotId.Slot2));
                var codex1 = EnemyCodex.FromSaveData(loaded1.campaign.codex);
                var codex2 = EnemyCodex.FromSaveData(loaded2.campaign.codex);

                Assert.AreEqual(Effectiveness.Weak, codex1.GetKnownElement("hollow_squire", Element.Fire));
                Assert.AreEqual(Effectiveness.Neutral, codex1.GetKnownElement("hollow_squire", Element.Ice));
                Assert.AreEqual(StatusResistTier.Resistant, codex1.GetKnownStatus("hollow_squire", StatusId.Fear));
                Assert.IsNull(codex2.GetKnownElement("hollow_squire", Element.Fire), "Slot 2 must not share Slot 1's Codex");
            }
            finally
            {
                SaveManager.Storage = prevStorage;
                PersistenceCommitHandler.SetActiveCommittedSnapshot(prevSnap);
            }
        }

        [Test]
        public void Stage4_ConcealedBossRevealOnSecondTriggerPhaseCarryOverTransformAndKeenSenses()
        {
            // 1. Concealed affinity on Zoroa P1 (Drain Dark): 1st trigger -> ?!, stays ???; 2nd trigger -> revealed!
            var hero = new Unit(Database.Characters[Database.Party[0]]);
            var b = new Battle(new[] { hero }, new[] { Database.Enemies["zoroa_p1"] }, new List<string>(), new Random(7));
            var h = b.Party[0];
            var z = b.Enemies[0];

            var darkSkill = MakeMagicSkill("test_dark_conceal", Element.Dark, 1.5f);
            var fireSkill = MakeMagicSkill("test_fire_phase", Element.Fire, 1.5f);
            var physSkill = new SkillDef { id = "test_phys_phase", name = "Slash", kind = SkillKind.Physical, element = Element.Physical, target = TargetType.Enemy, power = 1f };
            h.unit.skillLoadout.activeSlots[0] = darkSkill.id;
            h.unit.skillLoadout.activeSlots[1] = fireSkill.id;
            h.unit.skillLoadout.activeSlots[2] = physSkill.id;

            var reveals = new List<CodexRevealEvent>();
            b.CodexRevealed += (_, ev) => reveals.Add(ev);
            bool shiftedEvent = false;
            b.BossAffinitiesShifted += _ => shiftedEvent = true;

            AdvanceTo(b, h);
            var hits1 = b.Perform(BattleAction.Skill(h, darkSkill, z));
            Assert.IsTrue(hits1.Any(x => x.concealedPing), "First concealed trigger flags ?!");
            Assert.IsNull(b.Codex.GetKnownElement("zoroa_p1", Element.Dark), "Concealed affinity stays ??? after 1st trigger");
            Assert.AreEqual(1, b.Codex.GetConcealedTriggerCount("zoroa_p1", Element.Dark));

            AdvanceTo(b, h);
            var hits2 = b.Perform(BattleAction.Skill(h, darkSkill, z));
            Assert.IsFalse(hits2.Any(x => x.concealedPing), "Second trigger reveals instead of ping");
            Assert.AreEqual(Effectiveness.Drain, b.Codex.GetKnownElement("zoroa_p1", Element.Dark), "Revealed on 2nd trigger");

            // Also discover Fire (Weak in both phases) and Physical (Resist in P1, Weak in P2)
            AdvanceTo(b, h);
            b.Perform(BattleAction.Skill(h, fireSkill, z));
            AdvanceTo(b, h);
            b.Perform(BattleAction.Skill(h, physSkill, z));
            Assert.AreEqual(Effectiveness.Weak, b.Codex.GetKnownElement("zoroa_p1", Element.Fire));
            Assert.AreEqual(Effectiveness.Resist, b.Codex.GetKnownElement("zoroa_p1", Element.Physical));

            // 2. Phase change Zoroa P1 -> P2: shows AFFINITIES SHIFTED, carries over unchanged Fire, NOT changed Physical/Dark
            b.Damage(z, z.unit.hp + 999);
            Assert.IsTrue(shiftedEvent, "BossAffinitiesShifted event fires on phase change");
            Assert.IsTrue(b.Log.Any(l => l.Contains("AFFINITIES SHIFTED")), "Log contains AFFINITIES SHIFTED");
            Assert.AreEqual("zoroa_p2", z.enemy.id);
            Assert.AreEqual(Effectiveness.Weak, b.Codex.GetKnownElement("zoroa_p2", Element.Fire), "Unchanged Fire weakness auto-revealed in P2");
            Assert.IsNull(b.Codex.GetKnownElement("zoroa_p2", Element.Physical), "Changed Physical (Resist->Weak) stays ??? in P2");
            Assert.IsNull(b.Codex.GetKnownElement("zoroa_p2", Element.Dark), "Changed Dark (Drain->Resist) stays ??? in P2");

            // 3. On-Defeat Transformation preserves base Codex and uses separate Enhanced entry
            var baseSquire = Database.Enemies["hollow_squire"];
            var enhSquire = Database.Enemies["hollow_squire_enhanced"];
            var bTrans = new Battle(new[] { new Unit(Database.Characters[Database.Party[0]]) }, new[] { baseSquire }, new List<string>(), new Random(3));
            bTrans.RecordCodexElement(bTrans.Enemies[0], Element.Fire);
            bTrans.Enemies[0].AdvanceToPhase(enhSquire, 1);
            Assert.AreEqual(Effectiveness.Weak, bTrans.Codex.GetKnownElement("hollow_squire", Element.Fire), "Base discovery survives transform");
            Assert.IsNull(bTrans.Codex.GetKnownElement("hollow_squire_enhanced", Element.Fire), "Enhanced starts with its own Codex entry");

            // 4. Elf Keen Senses reveals 1 unknown weakness once per dungeon
            var elfDef = new CharacterDef
            {
                id = "test_elf_ks", name = "Elf", role = "Rogue", raceId = "elf", classId = "rogue",
                backgroundId = Database.Characters[Database.Party[0]].backgroundId,
                baseScores = Database.Characters[Database.Party[0]].baseScores,
            };
            var ksRun = new RunState(MapGenerator.Generate(5, 1), new[] { elfDef }, 5);
            Assert.AreEqual(1, ksRun.ElfWeaknessRevealUsesRemaining);
            var bKs1 = new Battle(ksRun.Party, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(5), ksRun);
            Assert.AreEqual(0, ksRun.ElfWeaknessRevealUsesRemaining, "Keen Senses consumes its 1/dungeon charge");
            Assert.AreEqual(Effectiveness.Weak, ksRun.Codex.GetKnownElement("hollow_squire", Element.Fire), "Keen Senses revealed Fire weakness");

            // Second battle in same dungeon does NOT trigger Keen Senses again
            var bKs2 = new Battle(ksRun.Party, new[] { Database.Enemies["cinder_wisp"] }, new List<string>(), new Random(6), ksRun);
            Assert.IsNull(ksRun.Codex.GetKnownElement("cinder_wisp", Element.Physical), "Keen Senses does not fire a second time in the same dungeon");

            // 5. Seeded replay reproduces identical discoveries
            string RunReplay(int seed)
            {
                var u = new Unit(Database.Characters[Database.Party[0]]);
                var bt = new Battle(new[] { u }, new[] { Database.Enemies["hollow_squire"] }, new List<string>(), new Random(seed));
                var p = bt.Party[0];
                var sk = MakeMagicSkill("r_fire", Element.Fire, 1.0f, apply: new[] { new StatusApply { id = StatusId.Bleed, chance = 0.6f, turns = 2 } });
                p.unit.skillLoadout.activeSlots[0] = sk.id;
                AdvanceTo(bt, p);
                bt.Perform(BattleAction.Skill(p, sk, bt.Enemies[0]));
                return UnityEngine.JsonUtility.ToJson(bt.Codex.ToSaveData());
            }
            Assert.AreEqual(RunReplay(99), RunReplay(99), "Seeded replay reproduces identical Codex discoveries");
        }

        [Test]
        public void Stage5_AnalysisInspectorIsFreeMatchesCodexHidesRawEnemyStatsAndShowsAllAllyData()
        {
            var partyDefs = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var b = new Battle(partyDefs, new[] { Database.Enemies["hollow_squire"], Database.Enemies["soul_reaper"] }, new List<string>(), new Random(12));
            var hero = b.NextTurn();
            while (hero != null && hero.IsEnemy)
            {
                b.Perform(BattleAction.Guard(hero));
                hero = b.NextTurn();
            }
            Assert.IsNotNull(hero);
            Assert.IsTrue(hero.IsPlayer);

            // Capture battle state before ANALYSIS inspection
            var activeBefore = b.Active;
            var queueBefore = b.Upcoming.ToList();
            int mpBefore = hero.unit.mp;
            int roundBefore = b.Round;

            // Apply active status on enemy BEFORE any Codex reveal -> must STILL be visible
            var squire = b.Enemies[0];
            squire.AddStatus(StatusId.Burn, 2, 0.1f, hero);

            // Enemy view before discovery: all 10 elements and all status resistances must be ???
            var enemyViewBefore = AnalysisInspector.BuildEnemyView(b, squire);
            Assert.AreEqual(10, enemyViewBefore.elements.Count);
            Assert.IsTrue(enemyViewBefore.elements.All(r => r.label == "???"), "Undiscovered enemy elements must show ???");
            Assert.IsTrue(enemyViewBefore.statusResistances.All(r => r.label == "???"), "Undiscovered enemy status resistances must show ???");
            Assert.IsTrue(enemyViewBefore.activeEffects.Any(a => a.label == "BURN" && a.remainingTurns == 2), "Active statuses always visible even with empty Codex");
            Assert.AreEqual("???", AnalysisInspector.TargetPickerTag(b, squire, Element.Fire));

            // Discover Fire (Weak) and Fear (Resistant) on hollow_squire
            b.RecordCodexElement(squire, Element.Fire);
            b.RecordCodexStatus(squire, StatusId.Fear);

            var enemyViewAfter = AnalysisInspector.BuildEnemyView(b, squire);
            var fireRow = enemyViewAfter.elements.First(r => r.element == Element.Fire);
            Assert.AreEqual("WEAK", fireRow.label);
            Assert.AreEqual(AffinityResolver.GetRaw(squire, Element.Fire), fireRow.known);
            Assert.AreEqual("WEAK", AnalysisInspector.TargetPickerTag(b, squire, Element.Fire));
            Assert.AreEqual("???", AnalysisInspector.TargetPickerTag(b, squire, Element.Dark));

            var fearRow = enemyViewAfter.statusResistances.First(r => r.status == StatusId.Fear);
            Assert.AreEqual("RESISTANT", fearRow.label, "Enemies show status tier, never raw %");
            Assert.IsFalse(fearRow.label.Contains("%"), "Enemy status resistance must never leak raw %");

            // Ally view: always 100% visible, exact stats, exact status %, defensive effects
            hero.guarding = true;
            var allyView = AnalysisInspector.BuildAllyView(hero);
            Assert.AreEqual(10, allyView.elements.Count);
            Assert.IsFalse(allyView.elements.Any(r => r.label == "???"), "Allies never show ???");
            Assert.IsFalse(allyView.statusResistances.Any(r => r.label == "???"), "Allies never show ??? for statuses");
            Assert.IsTrue(allyView.statusResistances.All(r => r.label.Contains("%") || r.label == "IMMUNE"), "Allies show exact % or IMMUNE");
            Assert.AreEqual(hero.unit.FinalScores.dex, allyView.dex, "DEX (AGI) matches FinalScores.dex");
            Assert.AreEqual(hero.Atk, allyView.atk);
            Assert.AreEqual(hero.Def, allyView.def);
            Assert.IsTrue(allyView.defensiveEffects.Any(d => d.label == "GUARD"));

            // Battle state completely untouched after opening/building ANALYSIS views (free command)
            Assert.AreSame(activeBefore, b.Active, "ANALYSIS must not change Battle.Active");
            CollectionAssert.AreEqual(queueBefore, b.Upcoming.ToList(), "ANALYSIS must not change turn queue");
            Assert.AreEqual(mpBefore, hero.unit.mp, "ANALYSIS must not spend MP");
            Assert.AreEqual(roundBefore, b.Round, "ANALYSIS must not advance Round");
        }

        [Test]
        public void Stage6_AiAvoidsElementShownToBeDrainedOrReflectedByHeroWithoutPeeking()
        {
            var heroReflect = new Unit(Database.Characters[Database.Party[0]]);
            var heroDrain = new Unit(Database.Characters[Database.Party[1]]);
            var fireMove = new EnemyAttack { name = "Fire Bolt", element = Element.Fire, hits = 1, hitDamage = new[] { 20 }, aiPriority = 3 };
            var physMove = new EnemyAttack { name = "Club", element = Element.Physical, hits = 1, hitDamage = new[] { 15 }, aiPriority = 1 };
            var casterDef = new EnemyDef
            {
                id = "test_ai_elem", name = "Elem AI", shortName = "EAI", tag = "EAI", tier = Tier.Elite,
                ai = AiKind.Priority, aiRole = AiRole.ArcaneCaster, aiTier = AiTier.Elite,
                stats = new Stats(300, 100, 20, 10, 10, 15),
                attacks = new[] { fireMove, physMove },
            };

            var b = new Battle(new[] { heroReflect, heroDrain }, new[] { casterDef }, new List<string>(), new Random(1));
            var hRef = b.Party[0];
            var hDrn = b.Party[1];
            var enemy = b.Enemies[0];

            hRef.AffinityProfile.SetElementalAffinity(ElementType.Fire, AffinityType.Reflect);
            hDrn.AffinityProfile.SetElementalAffinity(ElementType.Fire, AffinityType.Drain);

            // Unseen: encounter memory does NOT know Fire Reflect/Drain yet (AI Spec v1.1 §5.6)
            Assert.IsNull(b.Blackboard.Memory.GetSeenEffectiveness(hRef, Element.Fire));
            Assert.IsNull(b.Blackboard.Memory.GetSeenEffectiveness(hDrn, Element.Fire));

            // Observe enemy hitting hRef (Reflect) and hDrn (Drain) with Fire Bolt
            AdvanceTo(b, enemy);
            b.Perform(BattleAction.Strike(enemy, fireMove, hRef), defense: new[] { QteResult.Good });
            Assert.AreEqual(Effectiveness.Reflect, b.Blackboard.Memory.GetSeenEffectiveness(hRef, Element.Fire), "Memory records observed Reflect");

            AdvanceTo(b, enemy);
            b.Perform(BattleAction.Strike(enemy, fireMove, hDrn), defense: new[] { QteResult.Good });
            Assert.AreEqual(Effectiveness.Drain, b.Blackboard.Memory.GetSeenEffectiveness(hDrn, Element.Fire), "Memory records observed Drain");

            // Now AI chooses Physical Club instead of Fire Bolt against heroes shown to Reflect/Drain Fire
            AdvanceTo(b, enemy);
            var chosen = EnemyAI.Choose(b, enemy);
            Assert.AreEqual("Club", chosen.attack.name, "AI avoids Fire once heroes are shown to Reflect/Drain Fire");
        }
    }
}
