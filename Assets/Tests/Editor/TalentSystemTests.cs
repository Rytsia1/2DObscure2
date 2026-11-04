using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class TalentSystemTests
    {
        [SetUp]
        public void Setup()
        {
            _ = Database.Classes;
            _ = Database.Characters;
            _ = Database.Enemies;
            _ = Database.Talents;

            // Reset character definitions talentIds to avoid cross-test contamination
            foreach (var c in Database.Characters.Values)
            {
                c.talentIds = Array.Empty<string>();
            }
        }

        static Battle NewBattle(params string[] enemies) =>
            new Battle(
                Database.Party.Select(id => new Unit(Database.Characters[id])),
                enemies.Select(id => Database.Enemies[id]),
                new List<string>(Database.StartingItems),
                new System.Random(42));

        static Combatant P(Battle b, string name) => b.Party.First(c => c.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // =====================================================================
        // 1. Progression & Milestone Slots
        // =====================================================================

        [Test]
        public void TalentProgression_EarnedSlotsAtMilestones_MatchesLevelProgression()
        {
            // Milestone levels: 3, 6, 9, 12, 15
            Assert.AreEqual(0, TalentSystem.GetTalentSlotsAtLevel(1));
            Assert.AreEqual(0, TalentSystem.GetTalentSlotsAtLevel(2));
            Assert.AreEqual(1, TalentSystem.GetTalentSlotsAtLevel(3));
            Assert.AreEqual(1, TalentSystem.GetTalentSlotsAtLevel(4));
            Assert.AreEqual(1, TalentSystem.GetTalentSlotsAtLevel(5));
            Assert.AreEqual(2, TalentSystem.GetTalentSlotsAtLevel(6));
            Assert.AreEqual(2, TalentSystem.GetTalentSlotsAtLevel(8));
            Assert.AreEqual(3, TalentSystem.GetTalentSlotsAtLevel(9));
            Assert.AreEqual(3, TalentSystem.GetTalentSlotsAtLevel(11));
            Assert.AreEqual(4, TalentSystem.GetTalentSlotsAtLevel(12));
            Assert.AreEqual(4, TalentSystem.GetTalentSlotsAtLevel(14));
            Assert.AreEqual(5, TalentSystem.GetTalentSlotsAtLevel(15));
            Assert.AreEqual(5, TalentSystem.GetTalentSlotsAtLevel(16)); // capped at 5

            CollectionAssert.AreEqual(new[] { 3, 6, 9, 12, 15 }, TalentSystem.MilestoneLevels);
        }

        [Test]
        public void TalentSelection_PreventDuplicatesAndLimitToEarnedSlots()
        {
            var unit = new Unit(Database.Characters["knight"]) { level = 3 };
            unit.selectedTalentIds.Clear();
            Assert.AreEqual(1, unit.EarnedTalentSlots);
            Assert.AreEqual(1, unit.AvailableTalentSelections);

            // Select Alert
            bool ok = unit.TrySelectTalent("alert", out string err);
            Assert.IsTrue(ok, $"Selection of alert should succeed: {err}");
            Assert.IsTrue(unit.HasTalent("alert"));
            Assert.AreEqual(0, unit.AvailableTalentSelections);

            // Cannot select duplicate
            bool dupOk = unit.TrySelectTalent("alert", out string dupErr);
            Assert.IsFalse(dupOk, "Selecting duplicate talent must fail");
            StringAssert.Contains("already has", dupErr.ToLowerInvariant());

            // Cannot select beyond earned slots
            bool extraOk = unit.TrySelectTalent("healer", out string extraErr);
            Assert.IsFalse(extraOk, "Selecting without available slots must fail");
            StringAssert.Contains("no available talent", extraErr.ToLowerInvariant());

            // Level up to 6 -> opens second slot
            unit.level = 6;
            Assert.AreEqual(2, unit.EarnedTalentSlots);
            Assert.AreEqual(1, unit.AvailableTalentSelections);

            // Duplicate still blocked
            Assert.IsFalse(unit.TrySelectTalent("alert", out _));

            // New talent succeeds
            bool ok2 = unit.TrySelectTalent("healer", out _);
            Assert.IsTrue(ok2);
            Assert.AreEqual(2, unit.selectedTalentIds.Count);
            Assert.AreEqual(0, unit.AvailableTalentSelections);
        }

        [Test]
        public void TalentSelection_ClassSpecificTalents_CannotBeSelectedByOtherClasses()
        {
            var mageUnit = new Unit(Database.Characters["mage"]) { level = 6 };
            mageUnit.selectedTalentIds.Clear();

            // "fighter_weapon_adept" is Fighter/Knight-specific
            bool ok = mageUnit.TrySelectTalent("fighter_weapon_adept", out string err);
            Assert.IsFalse(ok, "Mage cannot select Fighter-specific talent");
            StringAssert.Contains("class", err.ToLowerInvariant());

            // "wizard_elemental_adept" is Wizard/Mage-specific -> should succeed
            bool mageOk = mageUnit.TrySelectTalent("wizard_elemental_adept", out string mageErr);
            Assert.IsTrue(mageOk, $"Mage should be able to select wizard_elemental_adept: {mageErr}");
        }

        // =====================================================================
        // 2. Alert Talent
        // =====================================================================

        [Test]
        public void Talent_Alert_ReducesEnemyCritChanceAgainstOwnerOnly()
        {
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");
            var rogue = P(b, "Rogue");
            var enemy = b.Enemies.First();

            // Rogue has 17% baseline crit chance
            int baseCritAgainstEnemy = rogue.GetCritChanceAgainst(enemy);
            Assert.Greater(baseCritAgainstEnemy, 10, "Attacker must have >10% baseline crit");

            knight.unit.selectedTalentIds.Add("alert");

            int critAgainstKnight = rogue.GetCritChanceAgainst(knight);
            Assert.AreEqual(baseCritAgainstEnemy - 10, critAgainstKnight, "Alert must reduce crit chance against owner by 10 percentage points");

            // Verify other target without alert is unaffected
            int critAgainstEnemyAfter = rogue.GetCritChanceAgainst(enemy);
            Assert.AreEqual(baseCritAgainstEnemy, critAgainstEnemyAfter, "Other targets must retain baseline crit chance");
        }

        // =====================================================================
        // 3. Healer Talent
        // =====================================================================

        [Test]
        public void Talent_Healer_IncreasesHealingDealtByFifteenPercent()
        {
            var b = NewBattle("skeleton");
            var healer = P(b, "Healer");
            var knight = P(b, "Knight");
            knight.unit.hp = 10;

            // Baseline heal without talent
            var hitNormal = b.Heal(knight, 100, healer);
            Assert.AreEqual(100, hitNormal.amount);

            // Add healer talent
            healer.unit.selectedTalentIds.Add("healer");
            knight.unit.hp = 10;

            var hitEmpowered = b.Heal(knight, 100, healer);
            Assert.AreEqual(115, hitEmpowered.amount, "Healer talent must grant +15% healing dealt");
        }

        // =====================================================================
        // 4. Durable Talent
        // =====================================================================

        [Test]
        public void Talent_Durable_GrantsTenPercentMaxHpAndFivePercentDamageReduction()
        {
            var unit = new Unit(Database.Characters["knight"]);
            unit.selectedTalentIds.Clear();
            unit.RecalculateStats();
            int baseMaxHp = unit.stats.maxHp;

            unit.selectedTalentIds.Add("durable");
            unit.RecalculateStats();

            int expectedHp = (int)Math.Round(baseMaxHp * 1.10f);
            Assert.AreEqual(expectedHp, unit.stats.maxHp, "Durable must grant +10% Max HP");

            // Test in combatant damage reduction
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");
            knight.unit.selectedTalentIds.Add("durable");
            var combatantWithDurable = new Combatant(knight.unit, Side.Party, 0, "Knight", "KNI");

            float reductionMult = combatantWithDurable.DamageModifiers.GetMultiplier(DamageModifierScope.AllDamageTaken);
            Assert.AreEqual(0.95f, reductionMult, 0.001f, "Durable must register 5% damage reduction (0.95 multiplier)");
        }

        // =====================================================================
        // 5. Keen Mind Talent
        // =====================================================================

        [Test]
        public void Talent_KeenMind_ReducesMpCostByTenPercentWithMinimumFloorOfOne()
        {
            var b = NewBattle("skeleton");
            var mage = P(b, "Mage");

            var skill20 = new SkillDef { mpCost = 20 };
            var skill10 = new SkillDef { mpCost = 10 };
            var skill1 = new SkillDef { mpCost = 1 };
            var skill0 = new SkillDef { mpCost = 0 };

            Assert.AreEqual(20, mage.GetEffectiveMpCost(skill20));

            mage.unit.selectedTalentIds.Add("keen_mind");

            // 20 MP * 0.90 = 18 MP
            Assert.AreEqual(18, mage.GetEffectiveMpCost(skill20));

            // 10 MP * 0.90 = 9 MP
            Assert.AreEqual(9, mage.GetEffectiveMpCost(skill10));

            // 1 MP with floor of 1 -> 1 MP
            Assert.AreEqual(1, mage.GetEffectiveMpCost(skill1), "1 MP cost must not reduce to 0 (minimum floor of 1)");

            // 0 MP remains 0
            Assert.AreEqual(0, mage.GetEffectiveMpCost(skill0), "0 MP skills remain 0");
        }

        // =====================================================================
        // 6. Arcane Reserve Talent
        // =====================================================================

        [Test]
        public void Talent_ArcaneReserve_GrantsFifteenFlatMaxMp()
        {
            var unit = new Unit(Database.Characters["mage"]);
            unit.selectedTalentIds.Clear();
            unit.RecalculateStats();
            int baseMaxMp = unit.stats.maxMp;

            unit.selectedTalentIds.Add("arcane_reserve");
            unit.RecalculateStats();

            Assert.AreEqual(baseMaxMp + 15, unit.stats.maxMp, "Arcane Reserve must grant +15 flat Max MP");
        }

        // =====================================================================
        // 7. Poisoner Talent
        // =====================================================================

        [Test]
        public void Talent_Poisoner_IncreasesPoisonDoTDamageByTwentyFivePercent()
        {
            var b = NewBattle("skeleton");
            var rogue = P(b, "Rogue");
            var skeleton = b.Enemies.First();

            var poisonApply = new StatusApply
            {
                id = StatusId.Poison,
                turns = 3,
                chance = 1f,
                dotSkillPower = 20f,
                dotScaling = 1.0f,
                dotDamageType = DamageType.Physical
            };

            skeleton.StatusController.TryApplyStatus(poisonApply, rogue, skeleton, out var instNormal);
            int normalDmg = instNormal.CalculateDynamicDoTDamage();

            rogue.unit.selectedTalentIds.Add("poisoner");
            skeleton.StatusController.TryApplyStatus(poisonApply, rogue, skeleton, out var instPoisoner);
            int bonusDmg = instPoisoner.CalculateDynamicDoTDamage();

            int expected = (int)Math.Max(1, Math.Round(normalDmg * 1.25f));
            Assert.AreEqual(expected, bonusDmg, "Poisoner must increase Poison DoT damage by 25%");
        }

        // =====================================================================
        // 8. Crusher & Slasher Talents
        // =====================================================================

        [Test]
        public void Talent_CrusherAndSlasher_IncreaseApplicationChanceByFifteenPercent()
        {
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");

            knight.unit.selectedTalentIds.Add("crusher");
            knight.unit.selectedTalentIds.Add("slasher");

            Assert.IsTrue(knight.unit.HasTalent("crusher"));
            Assert.IsTrue(knight.unit.HasTalent("slasher"));
            Assert.AreEqual(0.15f, TalentSystem.CrusherStunBonus, 0.001f);
            Assert.AreEqual(0.15f, TalentSystem.SlasherBleedBonus, 0.001f);
        }

        // =====================================================================
        // 9. Piercer Talent
        // =====================================================================

        [Test]
        public void Talent_Piercer_ReducesTargetPhysicalDefenseByTwentyPercent()
        {
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");
            var skeleton = b.Enemies.First();
            skeleton.unit.stats.def = 50;

            var ctxNormal = knight.CreateDamageContext(skeleton, new[] { DamageComponent.Physical(skillPower: 30f, scaling: 1.0f) });
            Assert.AreEqual(50f, ctxNormal.targetPhysicalDefense, 0.001f);

            knight.unit.selectedTalentIds.Add("piercer");

            var ctxPiercer = knight.CreateDamageContext(skeleton, new[] { DamageComponent.Physical(skillPower: 30f, scaling: 1.0f) });
            // 50 * (1 - 0.20) = 40
            Assert.AreEqual(40f, ctxPiercer.targetPhysicalDefense, 0.001f, "Piercer must penetrate 20% of target physical defense");
        }

        // =====================================================================
        // 10. Lucky Talent
        // =====================================================================

        [Test]
        public void Talent_Lucky_RetryRules_MaxOnePerQte_MaxThreePerBattle_ResetsPerBattle()
        {
            var b = NewBattle("skeleton");
            var rogue = P(b, "Rogue");

            Assert.IsFalse(rogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.10f), "Cannot retry without Lucky talent");

            rogue.unit.selectedTalentIds.Add("lucky");
            Assert.AreEqual(3, rogue.luckyActivationsRemaining);

            // Roll 0.10f <= 0.20f threshold on Miss -> can trigger
            Assert.IsTrue(rogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.10f));

            // Roll 0.50f > 0.20f -> cannot trigger
            Assert.IsFalse(rogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.50f));

            // Good / Perfect QTE cannot trigger retry
            Assert.IsFalse(rogue.CanTriggerLuckyRetry(QteResult.Good, () => 0.10f));
            Assert.IsFalse(rogue.CanTriggerLuckyRetry(QteResult.Perfect, () => 0.10f));

            // Use 1 activation
            rogue.RecordLuckyRetry();
            Assert.AreEqual(2, rogue.luckyActivationsRemaining);
            Assert.IsTrue(rogue.luckyUsedForCurrentQte);

            // Max 1 retry per QTE: second attempt on same QTE fails even with successful roll
            Assert.IsFalse(rogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.10f));

            // Next QTE arrives
            rogue.ResetQteLuckyFlag();
            Assert.IsFalse(rogue.luckyUsedForCurrentQte);
            Assert.IsTrue(rogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.10f));

            // Use activation 2
            rogue.RecordLuckyRetry();
            Assert.AreEqual(1, rogue.luckyActivationsRemaining);

            // Use activation 3
            rogue.ResetQteLuckyFlag();
            rogue.RecordLuckyRetry();
            Assert.AreEqual(0, rogue.luckyActivationsRemaining);

            // Exhausted: 4th attempt fails
            rogue.ResetQteLuckyFlag();
            Assert.IsFalse(rogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.10f), "Exhausted 3 activations cannot retry");

            // New combatant (new battle) resets activations
            var nextBattleRogue = new Combatant(rogue.unit, Side.Party, 1, "Rogue", "ROG");
            Assert.AreEqual(3, nextBattleRogue.luckyActivationsRemaining, "New battle must reset Lucky activations to 3");
            Assert.IsTrue(nextBattleRogue.CanTriggerLuckyRetry(QteResult.Miss, () => 0.10f));
        }

        // =====================================================================
        // 11. Inspiring Leader Talent
        // =====================================================================

        [Test]
        public void Talent_InspiringLeader_AppliesTenPercentDamageReductionFirstThreeTurns_ExpiresOnTurnFourOrDeath()
        {
            var b = NewBattle("skeleton");
            var knight = P(b, "Knight");
            var rogue = P(b, "Rogue");

            knight.unit.selectedTalentIds.Add("inspiring_leader");

            // Advance to start Round 1
            b.NextTurn();
            Assert.AreEqual(1, b.Round);
            Assert.IsTrue(b.IsInspiringLeaderActive(Side.Party));

            // Party member takes reduced damage
            rogue.unit.hp = 100;
            b.Damage(rogue, 50); // 50 * 0.90 = 45 damage taken
            Assert.AreEqual(55, rogue.unit.hp, "Inspiring Leader must reduce incoming party damage by 10%");

            // Advance to Round 4 by completing rounds
            while (b.Round < 4)
            {
                var cur = b.NextTurn();
                b.Perform(BattleAction.Guard(cur));
            }
            Assert.AreEqual(4, b.Round);
            Assert.IsFalse(b.IsInspiringLeaderActive(Side.Party), "Inspiring Leader must expire on round 4");

            // If leader dies in Round 1-3, expires immediately
            var b2 = NewBattle("skeleton");
            var k2 = P(b2, "Knight");
            k2.unit.selectedTalentIds.Add("inspiring_leader");
            b2.NextTurn();
            Assert.IsTrue(b2.IsInspiringLeaderActive(Side.Party));

            k2.unit.hp = 0; // Leader dies
            Assert.IsFalse(b2.IsInspiringLeaderActive(Side.Party), "Inspiring Leader must immediately deactivate if leader is dead");
        }

        // =====================================================================
        // 12. Respec Functionality
        // =====================================================================

        [Test]
        public void Talent_Respec_RefundsAllSelectedTalentsAndRestoresPoints()
        {
            var unit = new Unit(Database.Characters["knight"]) { level = 15 };
            unit.selectedTalentIds.Clear();
            Assert.AreEqual(5, unit.EarnedTalentSlots);

            unit.TrySelectTalent("alert", out _);
            unit.TrySelectTalent("durable", out _);
            unit.TrySelectTalent("arcane_reserve", out _);
            unit.TrySelectTalent("healer", out _);
            unit.TrySelectTalent("keen_mind", out _);

            Assert.AreEqual(5, unit.selectedTalentIds.Count);
            Assert.AreEqual(0, unit.AvailableTalentSelections);

            // Respec
            unit.RespecTalents();

            Assert.AreEqual(0, unit.selectedTalentIds.Count, "All talents must be cleared on respec");
            Assert.AreEqual(5, unit.AvailableTalentSelections, "All 5 talent points must be restored on respec");
            Assert.IsFalse(unit.HasTalent("alert"));
            Assert.IsFalse(unit.HasTalent("durable"));
            Assert.IsFalse(unit.HasTalent("arcane_reserve"));
        }

        // =====================================================================
        // 13. Persistence (Save & Load)
        // =====================================================================

        [Test]
        public void Talent_SaveAndLoad_PreservesSelectedTalents()
        {
            var def = Database.Characters["knight"];
            def.talentIds = new[] { "alert", "durable", "healer" };

            // Convert to SavedCharacterData
            var saved = SavedCharacterData.FromCharacterDef(def);
            Assert.IsNotNull(saved.selectedTalents);
            CollectionAssert.AreEqual(new[] { "alert", "durable", "healer" }, saved.selectedTalents);

            // Reconstruct CharacterDef
            var restoredDef = saved.ToCharacterDef();
            Assert.IsNotNull(restoredDef.talentIds);
            CollectionAssert.AreEqual(new[] { "alert", "durable", "healer" }, restoredDef.talentIds);

            // Construct Unit
            var unit = new Unit(restoredDef);
            Assert.IsTrue(unit.HasTalent("alert"));
            Assert.IsTrue(unit.HasTalent("durable"));
            Assert.IsTrue(unit.HasTalent("healer"));
        }

        // =====================================================================
        // 14. Draft Offer Generation Integrity
        // =====================================================================

        [Test]
        public void Talent_DraftOffer_GeneratesOneSharedAndTwoClassSpecificTalents()
        {
            var unit = new Unit(Database.Characters["knight"]) { level = 6 };
            unit.selectedTalentIds.Clear();
            var offer = TalentSystem.GenerateDraftOffer(unit, new System.Random(123));

            Assert.AreEqual(3, offer.Count, "Draft offer must produce exactly 3 talents when pool is available");

            int sharedCount = offer.Count(t => t.category == TalentCategory.Shared);
            int classCount = offer.Count(t => t.category == TalentCategory.ClassSpecific);

            Assert.AreEqual(1, sharedCount, "Offer should have 1 Shared Talent");
            Assert.AreEqual(2, classCount, "Offer should have 2 Class-Specific Talents");
            Assert.IsTrue(offer.All(t => t.category == TalentCategory.Shared || t.MatchesClass("knight")),
                "Class-specific talents must belong to the knight class");
        }

        [Test]
        public void TalentCatalog_ContainsRequiredSharedAndClassTalents()
        {
            // Verify core shared talents
            string[] coreShared = { "alert", "healer", "durable", "keen_mind", "arcane_reserve",
                                    "poisoner", "crusher", "slasher", "piercer", "lucky", "inspiring_leader" };
            foreach (var id in coreShared)
            {
                Assert.IsTrue(Database.Talents.ContainsKey(id), $"Database must contain core shared talent '{id}'");
                var t = Database.Talents[id];
                Assert.AreEqual(TalentCategory.Shared, t.category);
            }

            // Verify each of the 9 classes defined in Talent System.md has 5 registered class-specific talents
            string[] classes = { "fighter", "barbarian", "martial_artist", "rogue", "ranger", "cleric", "wizard", "warlock", "druid" };
            foreach (var c in classes)
            {
                int count = Database.Talents.Values.Count(t => t.category == TalentCategory.ClassSpecific && t.MatchesClass(c));
                Assert.AreEqual(5, count, $"Class '{c}' must have exactly 5 class-specific talents registered in catalog.");
            }
        }
    }
}
