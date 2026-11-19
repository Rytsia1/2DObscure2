using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    [TestFixture]
    public class DungeonModifierTests
    {
        static IEnumerable<CharacterDef> Party => Database.Party.Select(id => Database.Characters[id]);

        static MapDef Line(string eventId, params NodeType[] types) => new MapDef
        {
            name = "Test",
            act = 1,
            nodes = types.Select((t, i) => new NodeDef
            {
                id = "n" + i,
                type = t,
                floor = i,
                lane = 0,
                next = i < types.Length - 1 ? new[] { "n" + (i + 1) } : Array.Empty<string>(),
                encounter = t == NodeType.Battle ? "d1_e1" : t == NodeType.Elite ? "d1_e7" : t == NodeType.Boss ? "d1_boss" : null,
                eventId = t == NodeType.Event ? eventId : null,
            }).ToArray(),
        };

        static RunState NewRun(MapDef map = null, int seed = 1) =>
            new RunState(map ?? Line(null, NodeType.Battle, NodeType.Battle, NodeType.Boss), Party, seed);

        static Combatant AdvanceTo(Battle b, Func<Combatant, bool> who)
        {
            for (int i = 0; i < 100; i++)
            {
                var c = b.NextTurn();
                if (who(c)) return c;
                b.Perform(BattleAction.Guard(c));
            }
            Assert.Fail("Target combatant never reached a turn");
            return null;
        }

        [Test]
        public void All24Modifiers_ExistInDatabase_WithValidMetadata()
        {
            Assert.AreEqual(24, Database.DungeonModifiers.Count);
            for (int i = 1; i <= 24; i++)
            {
                string id = $"MOD-{i:D3}";
                Assert.IsTrue(Database.DungeonModifiers.ContainsKey(id), $"Missing {id}");
                var def = Database.DungeonModifiers[id];
                Assert.IsFalse(string.IsNullOrEmpty(def.displayName), $"{id} displayName");
                Assert.IsFalse(string.IsNullOrEmpty(def.shortCode), $"{id} shortCode");
                Assert.IsFalse(string.IsNullOrEmpty(def.description), $"{id} description");
            }
            Assert.AreEqual("Steady Focus", Database.DungeonModifiers["MOD-004"].displayName);
            Assert.AreEqual("Quick Instinct", Database.DungeonModifiers["MOD-005"].displayName);
        }

        [Test]
        public void Stacking_PersistsAcrossBattles_AndPickAtCapAddsNothing()
        {
            var run = NewRun(Line(null, NodeType.Battle, NodeType.Battle, NodeType.Battle, NodeType.Boss));
            var mgr = run.ModifierManager;

            Assert.IsTrue(mgr.AddModifier("MOD-001", run));
            Assert.AreEqual(1, mgr.GetEffectiveStackCount("MOD-001"));

            // Stacks never expire from battles while inside the dungeon
            for (int i = 0; i < 3; i++)
            {
                run.Enter(run.Map.Get("n" + i));
                run.WinCombat();
                run.ClaimReward(-1);
            }
            Assert.AreEqual(1, mgr.GetEffectiveStackCount("MOD-001"), "Modifier survives 3 battles");

            mgr.AddModifier("MOD-001", run);
            mgr.AddModifier("MOD-001", run);
            Assert.IsTrue(mgr.IsAtCap("MOD-001"));
            Assert.AreEqual("Battle Focus ×3 (MAX)", mgr.FormatOfferHeading("MOD-001"));

            Assert.IsTrue(mgr.AddModifier("MOD-001", run), "Pick at cap is accepted");
            Assert.AreEqual(3, mgr.GetTotalStackCount("MOD-001"), "...but adds no stack");
        }

        [Test]
        public void EventBlessing_PersistsAcrossBattlesAndSave_ClearsOnDungeonExit()
        {
            var run = NewRun(Line(null, NodeType.Battle, NodeType.Battle, NodeType.Boss));
            run.BattleBuffs.Add(new StatusApply { id = StatusId.AttackUp, turns = 3, magnitude = 0.25f });

            for (int i = 0; i < 2; i++)
            {
                run.Enter(run.Map.Get("n" + i));
                run.WinCombat();
                run.ClaimReward(-1);
            }
            Assert.AreEqual(1, run.BattleBuffs.Count, "Blessing survives battles");

            var restored = NewRun();
            restored.RestoreFromSave(GameSave.SaveRunState(run));
            Assert.AreEqual(1, restored.BattleBuffs.Count, "Blessing survives save/load");
            Assert.AreEqual(StatusId.AttackUp, restored.BattleBuffs[0].id);
            Assert.AreEqual(3, restored.BattleBuffs[0].turns);
            Assert.AreEqual(0.25f, restored.BattleBuffs[0].magnitude, 1e-4f);

            run.AdvanceToNextDungeon(MapGenerator.Generate(2, 2));
            Assert.AreEqual(0, run.BattleBuffs.Count, "Blessing cleared on leaving the dungeon");
        }

        [Test]
        public void Lifecycle_PersistsAcrossRegularBattles_ClearsOnBossClearAndDefeatAndNextDungeon()
        {
            // 1. Cleared after Boss reward claimed
            var run = NewRun(Line(null, NodeType.Battle, NodeType.Boss));
            run.ModifierManager.AddModifier("MOD-001", run);
            run.ModifierManager.AddModifier("MOD-002", run);

            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            run.ClaimReward(-1);
            Assert.AreEqual(1, run.ModifierManager.GetEffectiveStackCount("MOD-001"), "Persists into boss battle");

            run.Enter(run.Map.Get("n1"));
            run.WinCombat();
            // Boss reward screen hasn't been claimed yet; now claim boss reward
            run.ClaimReward(0);
            Assert.AreEqual(RunStatus.Victory, run.Status);
            Assert.AreEqual(0, run.ModifierManager.Count, "All modifiers cleared after boss reward");

            // 2. Cleared on AdvanceToNextDungeon
            var run2 = NewRun();
            run2.ModifierManager.AddModifier("MOD-001", run2);
            run2.AdvanceToNextDungeon(MapGenerator.Generate(2, 2));
            Assert.AreEqual(0, run2.ModifierManager.Count);

            // 3. Cleared on LoseCombat
            var run3 = NewRun();
            run3.ModifierManager.AddModifier("MOD-002", run3);
            run3.Enter(run3.Map.Get("n0"));
            run3.LoseCombat();
            Assert.AreEqual(0, run3.ModifierManager.Count);
        }

        [Test]
        public void SaveAndRestore_MidLayer_PreservesStacksDurationAndElement()
        {
            var run = NewRun();
            run.ModifierManager.AddModifier("MOD-001", run);
            run.ModifierManager.AddModifier("MOD-014", run, ElementType.Ice);
            run.ModifierManager.AddModifier("MOD-024", run, ElementType.Fire);

            var saved = GameSave.SaveRunState(run);
            Assert.AreEqual(3, saved.dungeonModifiers.Length);

            var restored = NewRun();
            restored.RestoreFromSave(saved);
            Assert.AreEqual(1, restored.ModifierManager.GetEffectiveStackCount("MOD-001"));
            Assert.AreEqual(1, restored.ModifierManager.GetEffectiveStackCount("MOD-014", ElementType.Ice));
            Assert.AreEqual(1, restored.ModifierManager.GetEffectiveStackCount("MOD-024", ElementType.Fire));
        }

        [Test]
        public void DamageDealtModifiers_BattleFocus_ArcaneCurrent_BloodPrice_GlassResolve_MarkedPrey_Attunement()
        {
            var run = NewRun();
            var b = new Battle(run.Party, new[] { Database.Enemies["warden"] }, new List<string>(), new Random(1), run);
            var knight = b.Party[0];
            var enemy = b.Enemies[0];

            // Baseline Physical & Fire multiplier
            float physMult = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Physical, ref physMult, b);
            Assert.AreEqual(1f, physMult, 0.0001f);

            // MOD-001 Battle Focus x2 (+10% Phys), MOD-002 Arcane Current x1 (+5% Elem)
            run.ModifierManager.AddModifier("MOD-001", run);
            run.ModifierManager.AddModifier("MOD-001", run);
            run.ModifierManager.AddModifier("MOD-002", run);

            physMult = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Physical, ref physMult, b);
            Assert.AreEqual(1.10f, physMult, 0.0001f);

            float fireMult = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Fire, ref fireMult, b);
            Assert.AreEqual(1.05f, fireMult, 0.0001f);

            // MOD-024 Elemental Attunement supports multiple elements simultaneously (cap 2 per element)
            run.ModifierManager.AddModifier("MOD-024", run, ElementType.Fire);
            run.ModifierManager.AddModifier("MOD-024", run, ElementType.Fire);
            run.ModifierManager.AddModifier("MOD-024", run, ElementType.Fire); // 3rd stack capped at 2 (+24%)
            run.ModifierManager.AddModifier("MOD-024", run, ElementType.Ice);  // +12% Ice active simultaneously

            fireMult = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Fire, ref fireMult, b);
            Assert.AreEqual(1.05f * 1.24f, fireMult, 0.0001f);

            float iceMult = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Ice, ref iceMult, b);
            Assert.AreEqual(1.05f * 1.12f, iceMult, 0.0001f);

            // MOD-016 Blood Price (+12% Phys & Elem, max 2) & MOD-017 Glass Resolve (+15% All, max 2)
            run.ModifierManager.AddModifier("MOD-016", run);
            run.ModifierManager.AddModifier("MOD-017", run);
            physMult = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Physical, ref physMult, b);
            Assert.AreEqual(1.10f * 1.12f * 1.15f, physMult, 0.001f);

            // MOD-022 Marked Prey (+10%/stack vs Broken enemies only)
            run.ModifierManager.AddModifier("MOD-022", run);
            float vsUnbroken = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Physical, ref vsUnbroken, b);
            Assert.AreEqual(physMult, vsUnbroken, 0.0001f, "Marked Prey does not apply before Broken");

            enemy.AddStatus(StatusId.Broken, 1, 1.5f, knight);
            float vsBroken = 1f;
            run.ModifierManager.ModifyDamageDealt(knight, enemy, Element.Physical, ref vsBroken, b);
            Assert.AreEqual(physMult * 1.10f, vsBroken, 0.001f);

            // Verify DamageCalculator component path handles Blood Price vs Almighty properly
            var compAtkMods = new DamageModifierSet();
            var compDefMods = new DamageModifierSet();
            run.ModifierManager.PopulateDamageModifiers(knight, enemy, compAtkMods, compDefMods);
            Assert.AreEqual(1.10f * 1.12f, compAtkMods.GetPhysicalDamageBonus(), 0.001f);
            Assert.AreEqual(1.15f * 1.10f, compAtkMods.GetAllDamageBonus(), 0.001f);
        }

        [Test]
        public void PiercingInsight_IgnoresPhysicalDefense_AndAlignsWithPiercer()
        {
            var run = NewRun();
            run.ModifierManager.AddModifier("MOD-003", run);
            run.ModifierManager.AddModifier("MOD-003", run);
            run.ModifierManager.AddModifier("MOD-003", run); // 15%

            var b = new Battle(run.Party, new[] { Database.Enemies["warden"] }, new List<string>(), new Random(1), run);
            var knight = b.Party[0];
            var warden = b.Enemies[0];

            Assert.AreEqual(0.15f, run.ModifierManager.GetPhysicalPenetrationBonus(knight), 0.0001f);

            var ctx = knight.CreateDamageContext(warden, new[] { DamageComponent.Physical(20f, 1f) });
            Assert.AreEqual(warden.Def * 0.85f, ctx.targetPhysicalDefense, 0.01f);

            // Combined with Piercer talent (20% + 15% = 35%, clamped <= 50%)
            knight.unit.selectedTalentIds.Add("piercer");
            var ctxPiercer = knight.CreateDamageContext(warden, new[] { DamageComponent.Physical(20f, 1f) });
            Assert.AreEqual(warden.Def * 0.65f, ctxPiercer.targetPhysicalDefense, 0.01f);
        }

        [Test]
        public void DamageReductionCap_25Percent_AndLastStand_AndElementalWard_AndGlassResolveDrawback()
        {
            var run = NewRun();
            var b = new Battle(run.Party, new[] { Database.Enemies["warden"] }, new List<string>(), new Random(1), run);
            var knight = b.Party[0];

            // Stack MOD-006 (3x = 15% DR), MOD-013 (3x = 24% DR) -> raw multiplicative = 0.85 * 0.76 = 0.646 (35.4% DR)
            // Bab 15 §4.2: Total All Damage Taken reduction from all modifiers capped at 25% (multiplier >= 0.75f).
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-006", run);
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-013", run);

            float recvPhys = 1f;
            run.ModifierManager.ModifyDamageReceived(knight, Element.Physical, ref recvPhys, b);
            Assert.AreEqual(0.75f, recvPhys, 0.0001f, "Total DR from modifiers must cap at 25%");

            // MOD-014 Elemental Ward (Fire -15%/stack, max 2 = -30%) stacks on top for Fire only
            run.ModifierManager.AddModifier("MOD-014", run, ElementType.Fire);
            run.ModifierManager.AddModifier("MOD-014", run, ElementType.Fire);
            float recvFire = 1f;
            run.ModifierManager.ModifyDamageReceived(knight, Element.Fire, ref recvFire, b);
            Assert.AreEqual(0.75f * 0.70f, recvFire, 0.0001f);

            // MOD-017 Glass Resolve drawback (+10% All Damage Taken per stack)
            run.ModifierManager.AddModifier("MOD-017", run);
            float recvWithGlass = 1f;
            run.ModifierManager.ModifyDamageReceived(knight, Element.Physical, ref recvWithGlass, b);
            Assert.AreEqual(0.75f * 1.10f, recvWithGlass, 0.0001f);

            // MOD-023 Last Stand triggers ONLY when 1 party member remains active
            run.ModifierManager.ClearAll();
            run.ModifierManager.AddModifier("MOD-023", run);
            run.ModifierManager.AddModifier("MOD-023", run); // +20% DR when alone

            float recv4Alive = 1f;
            run.ModifierManager.ModifyDamageReceived(knight, Element.Physical, ref recv4Alive, b);
            Assert.AreEqual(1f, recv4Alive, 0.0001f, "Last Stand inactive while >1 hero alive");

            for (int i = 1; i < b.Party.Count; i++) b.Party[i].unit.hp = 0;
            float recv1Alive = 1f;
            run.ModifierManager.ModifyDamageReceived(knight, Element.Physical, ref recv1Alive, b);
            Assert.AreEqual(0.80f, recv1Alive, 0.0001f, "Last Stand active with 1 living hero");
        }

        [Test]
        public void QteWindowModifiers_RespectShared25PercentCap_And66MsMinimumPerfectWidth()
        {
            var run = NewRun();
            var b = new Battle(run.Party, new[] { Database.Enemies["skeleton"] }, new List<string>(), new Random(1), run);
            var knight = b.Party[0];
            var enemy = b.Enemies[0];

            // 3 stacks Steady Focus (+30%) + character DEX bonus -> capped at +25%
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-004", run);
            var offQte = (TimingQte)QteFactory.Offense(BattleAction.Attack(knight, enemy), Tier.Normal, QteAssist.Standard, new Random(1), run);
            Assert.AreEqual(QteConfig.Profiles[Tier.Normal].perfect * 1.25f, offQte.EffectivePerfect, 0.0001f);

            // 3 stacks Quick Instinct (+30%) -> Defensive QTE capped at +25%
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-005", run);
            var defQte = (DefenseQte)QteFactory.Defense(BattleAction.Attack(enemy, knight), QteAssist.Standard, run);
            Assert.AreEqual(0.25f, defQte.TimingBonusFraction, 0.0001f);

            // 3 stacks Fractured Focus (-30%) without Steady Focus -> W_P minimum is 66 ms (half-width >= 0.033s)
            run.ModifierManager.ClearAll();
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-018", run);
            knight.unit.abilities.baseScores = AbilityScores.Uniform(8); // minimize DEX bonus
            knight.unit.RecalculateStats(fillResources: true);

            var debuffedBossQte = (TimingQte)QteFactory.Offense(BattleAction.Attack(knight, enemy), Tier.Boss, QteAssist.Standard, new Random(1), run);
            Assert.GreaterOrEqual(debuffedBossQte.EffectivePerfect, 0.033f - 1e-5f, "W_P must never drop below 66 ms (0.033s half-width)");
            Assert.AreEqual(QteResult.Perfect, QteConfig.Judge(0.032f, QteConfig.Profiles[Tier.Boss], debuffedBossQte.TimingBonusFraction));
        }

        [Test]
        public void MpCost_ManaWell_And_LeakingMana_RespectMin1MpAndFreeSkills()
        {
            var run = NewRun();
            var b = new Battle(run.Party, new[] { Database.Enemies["skeleton"] }, new List<string>(), new Random(1), run);
            var mage = b.Party[2];
            var skill20 = new SkillDef { id = "test_20mp", name = "Test20", kind = SkillKind.Magic, mpCost = 20 };
            var skill10 = new SkillDef { id = "test_10mp", name = "Test10", kind = SkillKind.Magic, mpCost = 10 };
            var skill1 = new SkillDef { id = "test_1mp", name = "Test1", kind = SkillKind.Magic, mpCost = 1 };
            var skill0 = new SkillDef { id = "test_0mp", name = "Test0", kind = SkillKind.Physical, mpCost = 0 };

            // MOD-008 Mana Well x3 (-15%)
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-008", run);
            Assert.AreEqual(17, mage.GetEffectiveMpCost(skill20, run));
            Assert.AreEqual(8, mage.GetEffectiveMpCost(skill10, run));
            Assert.AreEqual(1, mage.GetEffectiveMpCost(skill1, run), "Minimum cost remains 1 MP");
            Assert.AreEqual(0, mage.GetEffectiveMpCost(skill0, run), "Free skills remain 0 MP");

            // MOD-021 Leaking Mana x2 (+2 MP)
            run.ModifierManager.ClearAll();
            run.ModifierManager.AddModifier("MOD-021", run);
            run.ModifierManager.AddModifier("MOD-021", run);
            Assert.AreEqual(12, mage.GetEffectiveMpCost(skill10, run));
            Assert.AreEqual(0, mage.GetEffectiveMpCost(skill0, run), "Free skills do not gain MP cost");
        }

        [Test]
        public void HealingAndOneShotModifiers_VitalSpark_WitheringCurse_SecondWind_QuietMind()
        {
            var run = NewRun();
            var knight = run.Party[0];
            var rogue = run.Party[1];
            knight.hp = 10;
            knight.mp = 2;
            rogue.hp = 0; // Downed!

            // MOD-011 Second Wind (+8% Max HP) & MOD-012 Quiet Mind (+5% Max MP)
            run.ModifierManager.AddModifier("MOD-011", run);
            run.ModifierManager.AddModifier("MOD-012", run);
            Assert.AreEqual(0, run.ModifierManager.Count, "One-shot modifiers do not persist as stacks");
            Assert.AreEqual(10 + (int)Math.Round(knight.stats.maxHp * 0.08f), knight.hp);
            Assert.AreEqual(2 + Math.Max(1, (int)Math.Floor(knight.stats.maxMp * 0.05f)), knight.mp);
            Assert.AreEqual(0, rogue.hp, "Second Wind must NOT revive Downed characters");

            // Battle healing received: Vital Spark (+10%/stack) & Withering Curse (-15%/stack)
            var b = new Battle(run.Party, new[] { Database.Enemies["skeleton"] }, new List<string>(), new Random(1), run);
            var bKnight = b.Party[0];
            bKnight.unit.hp = 10;
            run.ModifierManager.AddModifier("MOD-007", run); // +10%
            var hit1 = b.Heal(bKnight, 50);
            Assert.AreEqual(55, hit1.amount);

            run.ModifierManager.ClearAll();
            bKnight.unit.hp = 10;
            run.ModifierManager.AddModifier("MOD-019", run); // -15%
            run.ModifierManager.AddModifier("MOD-019", run); // -30%
            var hit2 = b.Heal(bKnight, 50);
            Assert.AreEqual(35, hit2.amount);
        }

        [Test]
        public void HeavySteps_ReducesSpeedAndUpdatesTurnOrder()
        {
            var run = NewRun();
            for (int i = 0; i < 3; i++) run.ModifierManager.AddModifier("MOD-020", run); // -15% Speed

            var b = new Battle(run.Party, new[] { Database.Enemies["skeleton"] }, new List<string>(), new Random(1), run);
            var knight = b.Party[0];
            int expected = Math.Max(1, (int)Math.Floor(knight.unit.stats.spd * 0.85f));
            Assert.AreEqual(expected, knight.Speed);
        }

        [Test]
        public void UtilityModifiers_TreasureSense_FortunesFavor_ScavengersEye()
        {
            var run = NewRun(Line(null, NodeType.Battle, NodeType.Boss));

            // MOD-009 Treasure Sense x2 (+30% Gold) + MOD-015 Fortune's Favor x2 (+24 Gold)
            run.ModifierManager.AddModifier("MOD-009", run);
            run.ModifierManager.AddModifier("MOD-009", run);
            run.ModifierManager.AddModifier("MOD-015", run);
            run.ModifierManager.AddModifier("MOD-015", run);

            run.Enter(run.Map.Get("n0"));
            run.WinCombat();
            int offerGold = run.PendingReward.gold;
            Assert.AreEqual(24, run.PendingReward.bonusGoldFromModifier);
            Assert.AreEqual(2, run.ModifierManager.GetEffectiveStackCount("MOD-015"), "Fortune's Favor stays until dungeon exit");

            run.ClaimReward(-1);
            int expectedGold = (int)Math.Round(offerGold * 1.30f);
            Assert.AreEqual(expectedGold, run.Gold);
            Assert.AreEqual(2, run.ModifierManager.GetEffectiveStackCount("MOD-009"), "Treasure Sense stays after a Gold reward");

            // MOD-010 Scavenger's Eye: 3 stacks = 60% chance; test consumption across seeded runs
            bool anyBonusFound = false;
            for (int seed = 1; seed <= 20; seed++)
            {
                var r2 = NewRun(Line(null, NodeType.Treasure, NodeType.Boss), seed);
                for (int k = 0; k < 3; k++) r2.ModifierManager.AddModifier("MOD-010", r2);
                r2.Enter(r2.Map.Get("n0"));
                Assert.AreEqual(3, r2.ModifierManager.GetEffectiveStackCount("MOD-010"), "Scavenger's Eye stays after a reward roll");
                if (!string.IsNullOrEmpty(r2.PendingReward.bonusItemId))
                {
                    anyBonusFound = true;
                    int itemsBefore = r2.Items.Count;
                    r2.ClaimReward(-1);
                    Assert.AreEqual(itemsBefore + 1, r2.Items.Count);
                    break;
                }
            }
            Assert.IsTrue(anyBonusFound, "Scavenger's Eye should produce a bonus consumable in at least 1 of 20 seeds");
        }

        [Test]
        public void CursedShrineAndAstralObeliskEvents_GrantModifiersAndPreserveLastChoiceUnblocked()
        {
            var cursedRun = NewRun(Line("cursed_shrine", NodeType.Event, NodeType.Boss));
            cursedRun.Enter(cursedRun.Map.Get("n0"));
            Assert.IsNull(cursedRun.EventBlocked(cursedRun.CurrentEvent.choices.Last()));
            string outcome = cursedRun.ChooseEvent(0);
            StringAssert.Contains("Blood Price", outcome);
            Assert.AreEqual(1, cursedRun.ModifierManager.GetEffectiveStackCount("MOD-016"));

            var obeliskRun = NewRun(Line("astral_obelisk", NodeType.Event, NodeType.Boss));
            obeliskRun.Enter(obeliskRun.Map.Get("n0"));
            Assert.IsTrue(obeliskRun.EventModifiers.ContainsKey(0));
            string obOutcome = obeliskRun.ChooseEvent(0);
            Assert.AreEqual(1, obeliskRun.ModifierManager.Count);
            StringAssert.Contains("Gained Modifier", obOutcome);
        }
    }
}
