using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Obscure.Tests
{
    // Comprehensive EditMode test suite verifying FELNA_Attribute_Progression_Derived_Stats_Spec_v1.0 (FINAL).
    public class AttributeProgressionAndDerivedStatsTests
    {
        [Test]
        public void AttributeProgression_MilestonesAndActiveCap15_MatchFinalSpec()
        {
            var cfg = Database.AsiConfig;
            CollectionAssert.AreEqual(new[] { 4, 7, 10, 13, 16, 19, 22, 25, 28 }, cfg.availableAtLevels);
            Assert.AreEqual(2, cfg.pointsGranted);
            Assert.AreEqual(20, cfg.maxAbilityScore);

            // Total AP across Level 1..30 is exactly 18 AP; Level 1 grants 0 AP
            int totalTo30 = 0;
            Assert.IsFalse(cfg.GrantsAtLevel(1));
            for (int lv = 1; lv <= 30; lv++)
                if (cfg.GrantsAtLevel(lv)) totalTo30 += cfg.pointsGranted;
            Assert.AreEqual(18, totalTo30);

            // Active Level Cap remains 15 for now -> 4 active milestones (4, 7, 10, 13) = 8 AP
            Assert.AreEqual(15, RunConfig.MaxLevel);
            var unit = new Unit(Database.Characters["knight"]);
            Assert.AreEqual(0, unit.unspentAsiPoints);
            for (int lv = 2; lv <= RunConfig.MaxLevel; lv++)
                unit.LevelUp();
            Assert.AreEqual(15, unit.level);
            Assert.AreEqual(8, unit.unspentAsiPoints, "Levels 4, 7, 10, 13 grant 8 AP under active Level Cap 15.");
        }

        [Test]
        public void FreeAllocation_AllowsStackingBankingAndPreservesApAtCap20()
        {
            var def = new CharacterDef
            {
                id = "liicht_cleric",
                name = "Cleric",
                raceId = "liichtian",
                classId = "cleric",
                racialBonusChoice = new[] { Ability.WIS }, // +2 WIS
                baseScores = new AbilityScores(10, 12, 12, 14, 15, 9) // 27 pts -> WIS 17
            };
            var unit = new Unit(def);
            Assert.AreEqual(17, unit.Score(Ability.WIS));

            // Level 4 grants +2 AP; both AP can be allocated to WIS (17 -> 19)
            unit.LevelUp(); unit.LevelUp(); unit.LevelUp();
            Assert.AreEqual(2, unit.unspentAsiPoints);
            Assert.IsTrue(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.IsTrue(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.AreEqual(19, unit.Score(Ability.WIS));
            Assert.AreEqual(0, unit.unspentAsiPoints);

            // Bank AP across Level 7 and Level 10 (+4 AP banked)
            for (int i = 0; i < 6; i++) unit.LevelUp(); // Lv 4 -> Lv 10
            Assert.AreEqual(4, unit.unspentAsiPoints);

            // Raise WIS 19 -> 20 (cap); attempting to spend again fails and NEVER loses AP
            Assert.IsTrue(unit.TrySpendAsiPoint(Ability.WIS));
            Assert.AreEqual(20, unit.Score(Ability.WIS));
            Assert.AreEqual(3, unit.unspentAsiPoints);

            Assert.IsFalse(unit.TrySpendAsiPoint(Ability.WIS), "Cannot allocate AP beyond normal cap 20.");
            Assert.AreEqual(3, unit.unspentAsiPoints, "AP must never be lost when blocked by Attribute Cap 20.");
        }

        [Test]
        public void AttributeCaps_Enforce20NormalAndAllowExplicitCapBreakingTempBuff()
        {
            var sheet = new AbilityScoreSheet
            {
                baseScores = new AbilityScores(15, 10, 10, 10, 10, 10),
                racialModifiers = new AbilityScores(2, 0, 0, 0, 0, 0),
                permanentBonuses = new AbilityScores(3, 0, 0, 0, 0, 0) // 15 + 2 + 3 = 20
            };
            Assert.AreEqual(20, sheet.Score(Ability.STR));

            // Standard equipment and standard temp buff cannot exceed 20
            sheet.equipmentBonuses = new AbilityScores(4, 0, 0, 0, 0, 0);
            Assert.AreEqual(20, sheet.Score(Ability.STR));

            sheet.temporaryModifiers = new AbilityScores(3, 0, 0, 0, 0, 0);
            Assert.AreEqual(20, sheet.Score(Ability.STR));

            // Debuff lowers cleanly from 20 to 18 (not masked by overflow)
            sheet.temporaryModifiers = new AbilityScores(-2, 0, 0, 0, 0, 0);
            Assert.AreEqual(18, sheet.Score(Ability.STR));

            // Explicit cap-breaking temp buff exceeds 20 (up to hard ceiling 30)
            sheet.temporaryModifiers = AbilityScores.Zero;
            sheet.capBreakingTemporaryModifiers = new AbilityScores(4, 0, 0, 0, 0, 0);
            Assert.AreEqual(24, sheet.Score(Ability.STR));
            Assert.AreEqual(7, sheet.Mod(Ability.STR));
        }

        [Test]
        public void WorkedExamples_AllSixAbilityScores_MatchExactSpecNumbers()
        {
            // 1. STR 14 with SkillPower 40, Scaling 1.5 -> 61 (Raw STR)
            Assert.AreEqual(61f, StatCalculator.CalculatePhysicalSkillBaseDamage(40f, 14, 1.5f), 1e-4f);
            var physComp = DamageComponent.Physical(40f, 1.5f);
            Assert.AreEqual(61f, physComp.CalculateBaseDamage(attackerStr: 14, attackerInt: 10), 1e-4f);

            // 2. DEX 16 (Mod +3) -> Ranged Acc +6%, Evasion +6%, QTE Window +9%
            int dex16Mod = AbilityScores.GetModifier(16);
            Assert.AreEqual(3, dex16Mod);
            Assert.AreEqual(6f, StatCalculator.CalculateRangedAccuracyBonus(dex16Mod), 1e-4f);
            Assert.AreEqual(6f, StatCalculator.CalculateEvasionBonus(dex16Mod), 1e-4f);
            Assert.AreEqual(9f, StatCalculator.CalculateQteWindowBonus(dex16Mod), 1e-4f);

            // 3. VIT 16 (Mod +3) with Base HP 1,000 -> 1,120
            int vit16Mod = AbilityScores.GetModifier(16);
            Assert.AreEqual(1120, StatCalculator.CalculateMaxHp(1000f, vit16Mod));

            // 4. INT 16 (Mod +3) with SkillPower 50, Scaling 1.5 -> 74; Base MP 200 -> 224
            Assert.AreEqual(74f, StatCalculator.CalculateElementalSkillBaseDamage(50f, 16, 1.5f), 1e-4f);
            var elemComp = DamageComponent.Elemental(ElementType.Holy, 50f, 1.5f);
            Assert.AreEqual(74f, elemComp.CalculateBaseDamage(attackerStr: 10, attackerInt: 16), 1e-4f);
            Assert.AreEqual(224, StatCalculator.CalculateMaxMp(200f, AbilityScores.GetModifier(16)));

            // 5. WIS 16 (Mod +3) with Base Healing 200 -> 224
            Assert.AreEqual(224, StatCalculator.CalculateHealingOutput(200f, AbilityScores.GetModifier(16)));

            // 6. CHA 16 (Mod +3) with Base Attack Buff +10% -> +11.2%
            int cha16Mod = AbilityScores.GetModifier(16);
            Assert.AreEqual(0.12f, StatCalculator.CalculateBuffEffectivenessBonus(cha16Mod), 1e-5f);
            Assert.AreEqual(0.112f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, cha16Mod), 1e-5f);
        }

        [Test]
        public void NegativeModifiersAndAntiCompounding_BehavePerFinalSpec()
        {
            // Score 8 -> Mod -1
            int negMod = AbilityScores.GetModifier(8);
            Assert.AreEqual(-1, negMod);

            Assert.AreEqual(960, StatCalculator.CalculateMaxHp(1000f, negMod));
            Assert.AreEqual(192, StatCalculator.CalculateMaxMp(200f, negMod));
            Assert.AreEqual(-2f, StatCalculator.CalculateRangedAccuracyBonus(negMod), 1e-4f);
            Assert.AreEqual(-2f, StatCalculator.CalculateEvasionBonus(negMod), 1e-4f);
            Assert.AreEqual(-3f, StatCalculator.CalculateQteWindowBonus(negMod), 1e-4f);
            Assert.AreEqual(192, StatCalculator.CalculateHealingOutput(200f, negMod));
            Assert.AreEqual(0.096f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, negMod), 1e-5f);

            // Non-compounding Max HP: VIT 16 (+12%) + Farmer V (+10%) on Base HP 1,000 = 1,220 (NOT 1,232)
            Assert.AreEqual(1220, StatCalculator.CalculateMaxHp(1000f, AbilityScores.GetModifier(16), extraBasePercentAdd: 0.10f));

            // Negative CHA modifier applies in Battle.SupportScaled for CHA-scaled Support skills (e.g., Blessing)
            var lowChaDef = new CharacterDef
            {
                id = "low_cha_cleric",
                name = "Cleric",
                raceId = "human",
                classId = "cleric",
                racialBonusChoice = new[] { Ability.WIS, Ability.VIT },
                baseScores = new AbilityScores(10, 10, 12, 12, 15, 8) // CHA 8 -> Mod -1
            };
            var caster = new Combatant(new Unit(lowChaDef), Side.Party, 0, "Cleric", "CLR");
            var blessingSkill = Database.Skills["blessing"]; // scalingAbility = CHA
            var buff = new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.10f, supportsChaScaling = true };
            var scaled = Battle.SupportScaled(buff, caster, blessingSkill);
            Assert.AreEqual(0.096f, scaled.magnitude, 1e-5f);
            Assert.AreEqual(2, scaled.turns, "CHA must never alter buff duration.");
            Assert.AreEqual(1f, scaled.chance, 1e-5f, "CHA must never alter status chance.");

            // Non-CHA support skill (e.g., Fighter Guard Stance scaled with VIT) is NOT scaled by CHA
            var highChaDef = new CharacterDef
            {
                id = "high_cha_fighter",
                name = "Fighter",
                raceId = "liichtian",
                classId = "fighter",
                racialBonusChoice = new[] { Ability.CHA },
                baseScores = new AbilityScores(12, 10, 12, 10, 10, 15) // CHA 17 -> Mod +3
            };
            var fighter = new Combatant(new Unit(highChaDef), Side.Party, 0, "Fighter", "FGT");
            var guardStance = Database.Skills["guard_stance"]; // scalingAbility = VIT
            var defBuff = new StatusApply { id = StatusId.DefenseUp, turns = 2, magnitude = 0.45f };
            var unscaled = Battle.SupportScaled(defBuff, fighter, guardStance);
            Assert.AreEqual(0.45f, unscaled.magnitude, 1e-5f, "CHA must not automatically scale non-CHA buffs.");
        }

        [Test]
        public void SupportAndStatSeparation_DecouplesShieldBlockCounterSpeedAndBreak()
        {
            var sheetLow = new AbilityScoreSheet { baseScores = new AbilityScores(8, 8, 8, 8, 8, 8) };
            var sheetHigh = new AbilityScoreSheet { baseScores = new AbilityScores(20, 20, 20, 20, 20, 20) };
            var cls = Database.Classes["fighter"];

            var dLow = StatCalculator.Calculate(sheetLow, cls, 1);
            var dHigh = StatCalculator.Calculate(sheetHigh, cls, 1);

            // Decoupled stats must remain identical regardless of all 6 Ability Scores
            Assert.AreEqual(dLow.maxShield, dHigh.maxShield, "Shield Power must stay separate from VIT/WIS/CHA.");
            Assert.AreEqual(dLow.blockPower, dHigh.blockPower, "Block Power must stay decoupled from STR.");
            Assert.AreEqual(dLow.counterPower, dHigh.counterPower, "Counter Power must stay decoupled from STR & DEX.");
            Assert.AreEqual(dLow.physicalBreakPower, dHigh.physicalBreakPower, "STR must not grant universal Break Power.");
            Assert.AreEqual(dLow.magicBreakPower, dHigh.magicBreakPower, "INT must not grant universal Break Power.");
            Assert.AreEqual(dLow.physicalDefense, dHigh.physicalDefense, "VIT must not alter Physical Defense.");
            Assert.AreEqual(dLow.magicDefense, dHigh.magicDefense, "WIS must not alter Magic Defense.");
            Assert.AreEqual(dLow.statusResistance, dHigh.statusResistance, "VIT/WIS must not alter Status Resistance.");
            Assert.AreEqual(dLow.staggerResistance, dHigh.staggerResistance, "VIT must not alter Stagger Resistance.");
            Assert.AreEqual(dLow.initiative, dHigh.initiative, "DEX must not alter Speed/Initiative.");
            Assert.AreEqual(dLow.actionSpeed, dHigh.actionSpeed, "DEX must not alter Action Speed.");
            Assert.AreEqual(dLow.criticalChance, dHigh.criticalChance, "DEX must not alter Critical Chance.");
            Assert.AreEqual(dLow.qteStability, dHigh.qteStability, "VIT must not alter QTE Stability.");

            // Coupled stats must scale strictly according to the 6 Ability Scores spec
            Assert.Greater(dHigh.maxHp, dLow.maxHp);
            Assert.Greater(dHigh.maxMp, dLow.maxMp);
            Assert.Greater(dHigh.physicalPower, dLow.physicalPower);
            Assert.Greater(dHigh.magicPower, dLow.magicPower);
            Assert.Greater(dHigh.healingWisMultiplier, dLow.healingWisMultiplier);
            Assert.Greater(dHigh.evasion, dLow.evasion);
            Assert.Greater(dHigh.qteTimingBonus, dLow.qteTimingBonus);
            Assert.Greater(dHigh.rangedAccuracyBonus, dLow.rangedAccuracyBonus);
            Assert.Greater(dHigh.buffEffectivenessBonus, dLow.buffEffectivenessBonus);
        }

        [Test]
        public void CharacterSimulation_LiichtianCleric_Level1To30MatchesSpecTable()
        {
            // Lv 1: STR 10(0), DEX 12(+1), VIT 12(+1), INT 14(+2), WIS 17(+3), CHA 9(-1)
            Assert.AreEqual(312, StatCalculator.CalculateMaxHp(300f, AbilityScores.GetModifier(12)));
            Assert.AreEqual(86, StatCalculator.CalculateMaxMp(80f, AbilityScores.GetModifier(14)));
            Assert.AreEqual(42f, StatCalculator.CalculatePhysicalSkillBaseDamage(30f, 10, 1.2f), 1e-4f);
            Assert.AreEqual(71f, StatCalculator.CalculateElementalSkillBaseDamage(50f, 14, 1.5f), 1e-4f);
            Assert.AreEqual(224, StatCalculator.CalculateHealingOutput(200f, AbilityScores.GetModifier(17)));
            Assert.AreEqual(0.096f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, AbilityScores.GetModifier(9)), 1e-5f);
            Assert.AreEqual(9, StatCalculator.CalculateSocialCheckValue(AbilityScores.GetModifier(9)));

            // Lv 30 (18 AP): STR 12(+1), DEX 14(+2), VIT 14(+2), INT 18(+4), WIS 20(+5), CHA 14(+2)
            Assert.AreEqual(1404, StatCalculator.CalculateMaxHp(1300f, AbilityScores.GetModifier(14)));
            Assert.AreEqual(301, StatCalculator.CalculateMaxMp(260f, AbilityScores.GetModifier(18)));
            Assert.AreEqual(44, StatCalculator.FloorInt(StatCalculator.CalculatePhysicalSkillBaseDamage(30f, 12, 1.2f)));
            Assert.AreEqual(77f, StatCalculator.CalculateElementalSkillBaseDamage(50f, 18, 1.5f), 1e-4f);
            Assert.AreEqual(4f, StatCalculator.CalculateRangedAccuracyBonus(AbilityScores.GetModifier(14)), 1e-4f);
            Assert.AreEqual(4f, StatCalculator.CalculateEvasionBonus(AbilityScores.GetModifier(14)), 1e-4f);
            Assert.AreEqual(6f, StatCalculator.CalculateQteWindowBonus(AbilityScores.GetModifier(14)), 1e-4f);
            Assert.AreEqual(240, StatCalculator.CalculateHealingOutput(200f, AbilityScores.GetModifier(20)));
            Assert.AreEqual(0.108f, StatCalculator.CalculateFinalBuffMagnitude(0.10f, AbilityScores.GetModifier(14)), 1e-5f);
            Assert.AreEqual(12, StatCalculator.CalculateSocialCheckValue(AbilityScores.GetModifier(14)));
        }
    }
}
