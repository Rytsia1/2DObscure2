using System;
using System.Collections.Generic;
using NUnit.Framework;
using Obscure;

namespace Obscure.Tests
{
    // Comprehensive verification suite for FELNA Attribute-Driven Derived Stats Integration & Balancing (FELNA-SYS-ATTR-002 v2.0 FINAL).
    public class AttributeProgressionAndDerivedStatsTests
    {
        [Test]
        public void AttributeProgression_MilestonesAndActiveCap15_MatchFinalSpec()
        {
            var expectedMilestones = new[] { 4, 7, 10, 13, 16, 19, 22, 25, 28 };
            CollectionAssert.AreEqual(expectedMilestones, Database.AsiConfig.availableAtLevels);
            Assert.AreEqual(2, Database.AsiConfig.pointsGranted);
            Assert.AreEqual(20, Database.AsiConfig.maxAbilityScore);

            // Active build phase keeps Level Cap at 15 (4 active milestones: Lv 4, 7, 10, 13 = 8 AP)
            Assert.AreEqual(15, RunConfig.MaxLevel);

            int totalApAt15 = 0;
            int totalApAt30 = 0;
            for (int lv = 1; lv <= 30; lv++)
            {
                if (Array.IndexOf(Database.AsiConfig.availableAtLevels, lv) >= 0)
                {
                    if (lv <= 15) totalApAt15 += Database.AsiConfig.pointsGranted;
                    totalApAt30 += Database.AsiConfig.pointsGranted;
                }
            }
            Assert.AreEqual(8, totalApAt15, "Total AP up to active Level Cap 15 must be 8 AP.");
            Assert.AreEqual(18, totalApAt30, "Total AP up to target Level Cap 30 must be 18 AP.");
            Assert.IsTrue(Array.IndexOf(Database.AsiConfig.availableAtLevels, 1) < 0, "Level 1 grants 0 AP (Point Buy used at Creation).");
        }

        [Test]
        public void FreeAllocation_AllowsStackingBankingAndPreservesApAtCap20()
        {
            var def = new CharacterDef
            {
                id = "test_cleric",
                name = "Test Cleric",
                raceId = "liichtian",
                classId = "cleric",
                baseScores = new AbilityScores(10, 12, 12, 14, 15, 9), // WIS 15 + 2 racial = 17
                stats = new Stats(75, 42, 8, 8, 10, 10)
            };
            var unit = new Unit(def);
            Assert.AreEqual(17, unit.FinalScores.wis);
            Assert.AreEqual(0, unit.unspentAsiPoints);

            // Level up 1 -> 4 (+2 AP) and bank them through Level 7 (+2 AP = 4 banked AP)
            while (unit.level < 7) unit.LevelUp();
            Assert.AreEqual(4, unit.unspentAsiPoints, "Player can bank AP across milestones.");

            // Spend 3 AP on the same ability (WIS 17 -> 20)
            Assert.IsTrue(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.IsTrue(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.IsTrue(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.AreEqual(20, unit.FinalScores.wis);
            Assert.AreEqual(1, unit.unspentAsiPoints);

            // Attempting to spend the remaining 1 AP on WIS at Cap 20 must fail and MUST NOT burn the AP
            Assert.IsFalse(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.AreEqual(20, unit.FinalScores.wis);
            Assert.AreEqual(1, unit.unspentAsiPoints, "AP that cannot be used due to Attribute Cap 20 must never be lost.");
        }

        [Test]
        public void AttributeCaps_Enforce20NormalAndAllowExplicitCapBreakingTempBuff()
        {
            var def = new CharacterDef
            {
                id = "test_cap",
                name = "Test Cap",
                raceId = "dwarf", // +2 VIT
                classId = "fighter",
                baseScores = new AbilityScores(15, 10, 15, 10, 12, 9), // VIT 15 + 2 = 17
                stats = new Stats(95, 15, 8, 11, 8, 6)
            };
            var unit = new Unit(def);
            unit.AddPermanentAbilityBonus(Ability.VIT, 3); // VIT -> 20
            Assert.AreEqual(20, unit.FinalScores.vit);

            // Standard temporary buff is clamped at 20
            unit.SetTemporaryAbilityModifiers(AbilityScores.Zero.Add(Ability.VIT, 4));
            Assert.AreEqual(20, unit.FinalScores.vit, "Standard temp buff cannot exceed normal progression cap 20.");
            unit.ClearTemporaryModifiers();

            // Explicit cap-breaking temporary buff may exceed 20
            unit.AddCapBreakingTemporaryAbilityModifier(Ability.VIT, 2);
            Assert.AreEqual(22, unit.FinalScores.vit, "Explicit cap-breaking temporary buff can exceed 20.");
            Assert.AreEqual(6, unit.Mod(Ability.VIT), "Modifier at score 22 is floor((22-10)/2) = +6.");
            unit.ClearTemporaryModifiers();
            Assert.AreEqual(20, unit.FinalScores.vit);

            // Negative temporary modifier lowers from capped 20 cleanly
            unit.SetTemporaryAbilityModifiers(AbilityScores.Zero.Add(Ability.VIT, -4));
            Assert.AreEqual(16, unit.FinalScores.vit);
            Assert.AreEqual(3, unit.Mod(Ability.VIT));
        }

        [Test]
        public void WorkedExamples_AllSixAbilityScoresAndDerivedStats_MatchExactSpecNumbers()
        {
            // 1. STR 16 (+3), VIT 14 (+2), DEX 12 (+1)
            Assert.AreEqual(64f, StatCalculator.CalculatePhysicalSkillBaseDamage(40f, 16, 1.5f), 1e-4f);
            Assert.AreEqual(6, StatCalculator.CalculatePhysicalDefenseBonus(3, 2));      // floor(3*1.5 + 2*0.75) = 6
            Assert.AreEqual(8, StatCalculator.CalculateBlockPowerBonus(3, 2));           // floor(3*2.0 + 2*1.0) = 8
            Assert.AreEqual(5, StatCalculator.CalculateUniversalBreakPowerBonus(3, 1));  // floor(3*1.5 + 1*0.75) = 5
            Assert.AreEqual(4, StatCalculator.CalculateArmorPenetrationBonus(3, 1));     // floor(3*1.2 + 1*0.6) = 4

            // 2. DEX 16 (+3), STR 14 (+2), INT 12 (+1), WIS 12 (+1)
            Assert.AreEqual(6f, StatCalculator.CalculateRangedAccuracyBonus(3), 1e-4f);
            Assert.AreEqual(6f, StatCalculator.CalculateEvasionBonus(3), 1e-4f);
            Assert.AreEqual(9f, StatCalculator.CalculateQteWindowBonus(3), 1e-4f);
            Assert.AreEqual(7, StatCalculator.CalculateQteStabilityBonus(3, 1));         // floor(3*2.0 + 1*1.0) = 7
            Assert.AreEqual(5, StatCalculator.CalculatePhysicalCritChanceBonus(3, 2));   // floor(3*1.5 + 2*0.5) = 5
            Assert.AreEqual(10, StatCalculator.CalculateCounterPowerBonus(3, 2));        // floor(3*2.5 + 2*1.5) = 10
            Assert.AreEqual(3, StatCalculator.CalculateInitiativeBonus(3, 1));           // floor(3*1.0 + 1*0.5) = 3

            // 3. VIT 16 (+3), STR 14 (+2), WIS 14 (+2)
            Assert.AreEqual(1120, StatCalculator.CalculateMaxHp(1000f, 3));
            Assert.AreEqual(0.04f, StatCalculator.CalculateDamageReductionBonus(3, 2), 1e-4f); // 3*1% + 2*0.5% = 4%
            Assert.AreEqual(8, StatCalculator.CalculateStatusResistanceBonus(3, 2));     // floor(3*2.0 + 2*1.0) = 8
            Assert.AreEqual(8, StatCalculator.CalculateStaggerResistanceBonus(3, 2));    // floor(3*2.0 + 2*1.0) = 8

            // 4. INT 16 (+3), WIS 14 (+2)
            Assert.AreEqual(74f, StatCalculator.CalculateElementalSkillBaseDamage(50f, 16, 1.5f), 1e-4f);
            Assert.AreEqual(224, StatCalculator.CalculateMaxMp(200f, 3));
            Assert.AreEqual(6, StatCalculator.CalculateMagicDefenseBonus(3, 2));         // floor(3*1.5 + 2*0.75) = 6
            Assert.AreEqual(4, StatCalculator.CalculateMagicPenetrationBonus(3, 2));     // floor(3*1.2 + 2*0.6) = 4

            // 5. WIS 16 (+3), CHA 14 (+2)
            Assert.AreEqual(224, StatCalculator.CalculateHealingOutput(200f, 3));
            Assert.AreEqual(0.135f, StatCalculator.CalculateShieldPowerBonus(3, 2), 1e-4f); // 3*3.5% + 2*1.5% = 13.5%
            Assert.AreEqual(170, StatCalculator.CalculateShieldOutput(150f, 3, 2));         // floor(150 * 1.135) = 170

            // 6. CHA 16 (+3), WIS 14 (+2), INT 12 (+1)
            Assert.AreEqual(0.15f, StatCalculator.CalculateBuffEffectivenessBonus(3, 2), 1e-4f);    // 3*4% + 2*1.5% = 15%
            Assert.AreEqual(0.115f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, 3, 2), 1e-5f); // 10% * 1.15 = 11.5%
            Assert.AreEqual(0.12f, StatCalculator.CalculateDebuffEffectivenessBonus(3, 1), 1e-4f);  // 3*3.5% + 1*1.5% = 12%
            Assert.AreEqual(0.224f, StatCalculator.CalculateFinalDebuffMagnitude(0.20f, 3, 1), 1e-5f);
            Assert.AreEqual(12, StatCalculator.CalculateSupportSkillPowerBonus(3, 2));   // floor(3*3.0 + 2*1.5) = 12
        }

        [Test]
        public void PrimaryDominatesSecondary_AndNegativeModifiersApplyCleanly()
        {
            // Verify w1 > w2 > 0 for every composite Derived Stat at equal modifier (+4)
            Assert.Greater(StatCalculator.CalculateShieldPowerBonus(4, 0), StatCalculator.CalculateShieldPowerBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateDamageReductionBonus(4, 0), StatCalculator.CalculateDamageReductionBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateStatusResistanceBonus(4, 0), StatCalculator.CalculateStatusResistanceBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateStaggerResistanceBonus(4, 0), StatCalculator.CalculateStaggerResistanceBonus(0, 4));
            Assert.Greater(StatCalculator.CalculatePhysicalDefenseBonus(4, 0), StatCalculator.CalculatePhysicalDefenseBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateMagicDefenseBonus(4, 0), StatCalculator.CalculateMagicDefenseBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateBlockPowerBonus(4, 0), StatCalculator.CalculateBlockPowerBonus(0, 4));
            Assert.Greater(StatCalculator.CalculatePhysicalCritChanceBonus(4, 0), StatCalculator.CalculatePhysicalCritChanceBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateArmorPenetrationBonus(4, 0), StatCalculator.CalculateArmorPenetrationBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateMagicPenetrationBonus(4, 0), StatCalculator.CalculateMagicPenetrationBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateCounterPowerBonus(4, 0), StatCalculator.CalculateCounterPowerBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateUniversalBreakPowerBonus(4, 0), StatCalculator.CalculateUniversalBreakPowerBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateInitiativeBonus(4, 0), StatCalculator.CalculateInitiativeBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateActionSpeedBonus(4, 0), StatCalculator.CalculateActionSpeedBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateQteStabilityBonus(4, 0), StatCalculator.CalculateQteStabilityBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateBuffEffectivenessBonus(4, 0), StatCalculator.CalculateBuffEffectivenessBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateDebuffEffectivenessBonus(4, 0), StatCalculator.CalculateDebuffEffectivenessBonus(0, 4));
            Assert.Greater(StatCalculator.CalculateSupportSkillPowerBonus(4, 0), StatCalculator.CalculateSupportSkillPowerBonus(0, 4));

            // Negative modifiers apply cleanly
            Assert.AreEqual(960, StatCalculator.CalculateMaxHp(1000f, -1));
            Assert.AreEqual(-0.05f, StatCalculator.CalculateShieldPowerBonus(-1, -1), 1e-4f);
            Assert.AreEqual(1220, StatCalculator.CalculateMaxHp(1000f, 3, extraBasePercentAdd: 0.10f), "Non-compounding: 1000 * (1 + 0.12 + 0.10) = 1220.");
        }

        [Test]
        public void CharismaThreePillars_AndPhysicalOnlyCritical_AndQteStabilityMechanism()
        {
            // 1. Critical Chance is strictly Physical-only (Elemental has no Critical Chance)
            Assert.IsTrue(CriticalHitSystem.CanCriticalHit(DamageType.Physical, false));
            Assert.IsFalse(CriticalHitSystem.CanCriticalHit(DamageType.Elemental, false), "Elemental Damage never Critically Hits.");
            Assert.IsFalse(CriticalHitSystem.CanCriticalHit(DamageType.Almighty, false));

            // 2. QTE Stability (Primary DEX, Secondary WIS) mitigates Freeze penalty & widens PartialSuccess without altering W_P/W_G
            var normalProfile = QteConfig.Profiles[Tier.Normal];
            float wp0 = QteConfig.ScalePerfect(normalProfile, 0f);
            float wg0 = QteConfig.ScaleGood(normalProfile, 0f);
            Assert.AreEqual(0.1125f, QteConfig.EffectiveFreezeWindowPenalty(0.15f, 0.25f), 1e-4f, "25% QTE Stability reduces 15% Freeze shrink by 25% -> 11.25%.");
            Assert.Greater(QteConfig.EffectivePartialWindowMultiplier(0.25f), QteConfig.PartialWindowMultiplier);
            Assert.AreEqual(wp0, QteConfig.ScalePerfect(normalProfile, 0f));
            Assert.AreEqual(wg0, QteConfig.ScaleGood(normalProfile, 0f));

            // 3. Charisma 3 Pillars in Battle: Commanding (Buff Eff CHA+WIS) & Disruptive (Debuff Eff CHA+INT)
            var warlockDef = new CharacterDef
            {
                id = "cha_warlock",
                name = "Cha Warlock",
                raceId = "human",
                classId = "warlock",
                baseScores = new AbilityScores(10, 10, 10, 14, 14, 16), // INT 14 (+2), WIS 14 (+2), CHA 16 (+3)
                stats = new Stats(80, 50, 10, 8, 10, 15)
            };
            var caster = new Combatant(new Unit(warlockDef), Side.Party, 0, "Warlock", "WLK");

            var buffAp = new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.20f, chance = 1f, supportsChaScaling = true };
            var scaledBuff = Battle.SupportScaled(buffAp, caster);
            // CHA +3 (+12%) + WIS +2 (+3%) = +15% -> 0.20 * 1.15 = 0.23
            Assert.AreEqual(0.23f, scaledBuff.magnitude, 1e-4f);
            Assert.AreEqual(2, scaledBuff.turns, "CHA/WIS must never alter buff duration.");

            var debuffAp = new StatusApply { id = StatusId.AttackDown, turns = 2, magnitude = 0.20f, chance = 1f, supportsChaScaling = true };
            var scaledDebuff = Battle.DebuffScaled(debuffAp, caster);
            // CHA +3 (+10.5%) + INT +2 (+3%) = +13.5% -> 0.20 * 1.135 = 0.227
            Assert.AreEqual(0.227f, scaledDebuff.magnitude, 1e-4f);
            Assert.AreEqual(2, scaledDebuff.turns, "CHA/INT must never alter debuff duration.");
        }

        [Test]
        public void CharacterSimulation_LiichtianCleric_Level1To30MatchesSpecTable()
        {
            // Level 1: STR 10 (0), DEX 12 (+1), VIT 12 (+1), INT 14 (+2), WIS 17 (+3), CHA 9 (-1)
            Assert.AreEqual(312, StatCalculator.CalculateMaxHp(300f, 1));
            Assert.AreEqual(86, StatCalculator.CalculateMaxMp(80f, 2));
            Assert.AreEqual(224, StatCalculator.CalculateHealingOutput(200f, 3));
            Assert.AreEqual(163, StatCalculator.CalculateShieldOutput(150f, 3, -1)); // +10.5% - 1.5% = +9.0% -> floor(150 * 1.09) = 163
            Assert.AreEqual(0.1005f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, -1, 3), 1e-5f); // -4% + 4.5% = +0.5%

            // Level 15 (8 AP — Active Cap 15): STR 10 (0), DEX 12 (+1), VIT 14 (+2), INT 16 (+3), WIS 20 (+5), CHA 10 (0)
            Assert.AreEqual(777, StatCalculator.CalculateMaxHp(720f, 2));
            Assert.AreEqual(179, StatCalculator.CalculateMaxMp(160f, 3));
            Assert.AreEqual(240, StatCalculator.CalculateHealingOutput(200f, 5));
            Assert.AreEqual(176, StatCalculator.CalculateShieldOutput(150f, 5, 0)); // +17.5% -> floor(150 * 1.175) = 176
            Assert.AreEqual(0.1075f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, 0, 5), 1e-5f); // +7.5%

            // Level 30 (18 AP — Target Cap 30): STR 12 (+1), DEX 14 (+2), VIT 14 (+2), INT 18 (+4), WIS 20 (+5), CHA 14 (+2)
            Assert.AreEqual(1404, StatCalculator.CalculateMaxHp(1300f, 2));
            Assert.AreEqual(301, StatCalculator.CalculateMaxMp(260f, 4));
            Assert.AreEqual(240, StatCalculator.CalculateHealingOutput(200f, 5));
            Assert.AreEqual(180, StatCalculator.CalculateShieldOutput(150f, 5, 2)); // +17.5% + 3.0% = +20.5% -> floor(150 * 1.205) = 180
            Assert.AreEqual(0.1155f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, 2, 5), 1e-5f); // +8% + 7.5% = +15.5%
        }

        [Test]
        public void AttributeAllocationDraft_StageIncreaseDecreaseResetAndCap20_DoesNotMutateUnit()
        {
            var def = new CharacterDef
            {
                id = "draft_fighter",
                name = "Draft Fighter",
                raceId = "dwarf", // +2 VIT
                classId = "fighter",
                baseScores = new AbilityScores(15, 10, 15, 8, 10, 10), // VIT 17, STR 15
                stats = new Stats(95, 15, 8, 11, 8, 6)
            };
            var unit = new Unit(def);
            while (unit.level < 7) unit.LevelUp(); // Lv 4 (+2) + Lv 7 (+2) = 4 AP
            Assert.AreEqual(4, unit.unspentAsiPoints);
            Assert.AreEqual(10, unit.NextAttributeMilestoneLevel());

            var draft = new AttributeAllocationDraft(unit, Database.AsiConfig);
            Assert.AreEqual(4, draft.RemainingPoints);
            Assert.IsFalse(draft.HasPendingChanges);

            // Stage 3 points into VIT (17 -> 20 cap)
            Assert.IsTrue(draft.TryIncrease(Ability.VIT));
            Assert.IsTrue(draft.TryIncrease(Ability.VIT));
            Assert.IsTrue(draft.TryIncrease(Ability.VIT));
            Assert.AreEqual(20, draft.GetPreviewUnbuffedScore(Ability.VIT));
            Assert.IsTrue(draft.IsAtCap(Ability.VIT));
            Assert.IsFalse(draft.CanIncrease(Ability.VIT));
            Assert.IsFalse(draft.TryIncrease(Ability.VIT), "Cannot stage past normal cap 20.");
            Assert.AreEqual(1, draft.RemainingPoints);

            // Unit must remain unmutated while staged
            Assert.AreEqual(17, unit.FinalScores.vit);
            Assert.AreEqual(4, unit.unspentAsiPoints);

            // Decrease and Reset work cleanly
            Assert.IsTrue(draft.TryDecrease(Ability.VIT));
            Assert.AreEqual(2, draft.RemainingPoints);
            draft.Reset();
            Assert.AreEqual(0, draft.TotalPending);
            Assert.AreEqual(4, draft.RemainingPoints);
            Assert.IsFalse(draft.CanDecrease(Ability.VIT));
        }

        [Test]
        public void AttributeAllocationDraft_PreviewDerivedAndTryCommit_UpdatesStatsAndPersists()
        {
            var def = new CharacterDef
            {
                id = "draft_commit",
                name = "Draft Commit",
                raceId = "human",
                classId = "fighter",
                baseScores = new AbilityScores(14, 12, 14, 10, 10, 10),
                stats = new Stats(95, 15, 8, 11, 8, 6)
            };
            var unit = new Unit(def);
            while (unit.level < 4) unit.LevelUp(); // +2 AP
            int beforeHp = unit.Derived.maxHp;
            int beforeDef = unit.Derived.physicalDefense;
            int beforeVit = unit.FinalScores.vit;

            var draft = new AttributeAllocationDraft(unit, Database.AsiConfig);
            Assert.IsTrue(draft.TryIncrease(Ability.VIT));
            Assert.IsTrue(draft.TryIncrease(Ability.VIT));

            var preview = draft.PreviewDerived();
            Assert.Greater(preview.maxHp, beforeHp, "Preview MaxHP must reflect +2 VIT (+1 mod).");
            Assert.GreaterOrEqual(preview.physicalDefense, beforeDef);
            Assert.AreEqual(beforeHp, unit.Derived.maxHp, "Preview must not mutate Unit.Derived.");

            Assert.IsTrue(draft.TryCommit(out int spent, out string err), err);
            Assert.AreEqual(2, spent);
            Assert.AreEqual(0, unit.unspentAsiPoints);
            Assert.AreEqual(beforeVit + 2, unit.FinalScores.vit);
            Assert.AreEqual(preview.maxHp, unit.Derived.maxHp, "Committed Derived stats must match live preview.");
            Assert.IsFalse(draft.HasPendingChanges);

            // Verify persistence payload captures both permanent bonuses and remaining unspent AP
            var saved = SavedCharacterData.FromCharacterDef(def, unit);
            Assert.AreEqual(0, saved.unspentAsiPoints);
            Assert.AreEqual(2, saved.permanentAbilityBonuses.vit);
        }

        [Test]
        public void SkillTreeScreen_CanAllocateAttributesAt_EnforcesHubAndCampfireOnly()
        {
            var map = MapGenerator.Generate(42, 1);
            var party = new List<CharacterDef> { Database.Characters["knight"] };
            var run = new RunState(map, party, 42);

            // Standalone (no Game instance) allows EditMode screen verification
            Assert.IsTrue(SkillTreeScreen.CanAllocateAttributesAt(null, run));

            // Create a temporary Game-like test setup via real RunState Rest node vs non-Rest node
            var restNode = Array.Find(map.nodes, n => n.type == NodeType.Rest);
            var battleNode = Array.Find(map.nodes, n => n.type == NodeType.Battle);
            Assert.IsNotNull(restNode, "Generated map should include a Rest node.");
            Assert.IsNotNull(battleNode, "Generated map should include a Battle node.");

            // While OnMap is true (not inside a Rest site), dungeon map cannot allocate
            var go = new UnityEngine.GameObject("TestGameGate");
            try
            {
                var game = go.AddComponent<Game>();
                // Force Hub state -> allowed
                game.Go(GameState.Hub);
                Assert.IsTrue(SkillTreeScreen.CanAllocateAttributesAt(game, run), "Hub must allow AP allocation.");

                // Force Map state while on map -> blocked
                game.Go(GameState.Map);
                Assert.IsFalse(SkillTreeScreen.CanAllocateAttributesAt(game, run), "Dungeon map outside a Campfire must block AP allocation.");

                // Enter a Rest node (not in Cleared -> OnMap is false) -> allowed
                var currentProp = typeof(RunState).GetProperty("Current");
                currentProp.SetValue(run, restNode);
                Assert.IsFalse(run.OnMap, "Unresolved Rest node must have OnMap == false.");
                Assert.IsTrue(SkillTreeScreen.CanAllocateAttributesAt(game, run), "Active Campfire / Rest node must allow AP allocation.");

                // After Rest node is cleared (back on Map) -> blocked
                run.Cleared.Add(restNode.id);
                Assert.IsTrue(run.OnMap);
                Assert.IsFalse(SkillTreeScreen.CanAllocateAttributesAt(game, run), "Cleared Rest node back on Map must block AP allocation.");

                // Non-Rest node -> blocked
                currentProp.SetValue(run, battleNode);
                Assert.IsFalse(SkillTreeScreen.CanAllocateAttributesAt(game, run), "Non-Rest dungeon node must block AP allocation.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
