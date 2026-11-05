using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class CursedEquipmentAndSystemRevisionTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            // Ensure Database static constructor has run
            _ = Database.Equipment.Count;
        }

        [Test]
        public void All17CursedEquipment_ExistInDatabase_WithBoonsAndCurses()
        {
            string[] cursedIds = {
                "EQ073", "EQ074", "EQ075", "EQ076", "EQ077",
                "EQ078", "EQ079", "EQ080", "EQ081", "EQ082",
                "EQ083", "EQ084", "EQ085", "EQ086", "EQ087",
                "EQ088", "EQ089"
            };

            Assert.AreEqual(17, cursedIds.Length);

            foreach (var id in cursedIds)
            {
                Assert.IsTrue(Database.Equipment.ContainsKey(id), $"Database.Equipment must contain {id}");
                var eq = Database.Equipment[id];

                Assert.AreEqual(EquipmentTrait.Cursed, eq.trait, $"{id} trait must be Cursed");
                Assert.IsTrue(eq.IsCursed, $"{id} IsCursed must be true");
                Assert.IsFalse(string.IsNullOrEmpty(eq.boon), $"{id} must have a non-empty boon");
                Assert.IsFalse(string.IsNullOrEmpty(eq.curse), $"{id} must have a non-empty curse");
                Assert.IsFalse(string.IsNullOrEmpty(eq.name), $"{id} must have a name");
            }

            // Verify specific examples
            var eq73 = Database.Equipment["EQ073"];
            Assert.AreEqual("Oathbreaker", eq73.name);
            Assert.AreEqual(EquipmentSlot.MainHand, eq73.slot);
            Assert.AreEqual(38, eq73.weaponPower);

            var eq75 = Database.Equipment["EQ075"];
            Assert.AreEqual("Fists of the Hollow Saint", eq75.name);

            var eq78 = Database.Equipment["EQ078"];
            Assert.AreEqual("Mercy's End", eq78.name);
            Assert.AreEqual(EquipmentSlot.MainHand, eq78.slot);

            var eq89 = Database.Equipment["EQ089"];
            Assert.AreEqual("Covenant of a Thousand Wounds", eq89.name);
            Assert.AreEqual(EquipmentSlot.Accessory, eq89.slot);
        }

        [Test]
        public void CursedEquipmentInstance_RetainsCursedProperties()
        {
            var def = Database.Equipment["EQ073"];
            var inst = new EquipmentInstance(def, EquipmentRarity.Epic, 1);

            Assert.IsTrue(inst.IsCursed);
            Assert.AreEqual(def.boon, inst.Boon);
            Assert.AreEqual(def.curse, inst.Curse);
            Assert.AreEqual(EquipmentTrait.Cursed, inst.Definition.trait);
        }

        [Test]
        public void CursedEquipment_CombatHooks_ApplyBoonsAndCurses()
        {
            var manager = new RelicManager();
            var warrior = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Warrior", "WAR");
            var enemyDef = Database.Enemies.Values.First();
            var target = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            // Equip EQ073 (Oathbreaker): +15% damage against enemies > 70% HP
            var eq73Def = Database.Equipment["EQ073"];
            var eq73 = new EquipmentInstance(eq73Def);
            warrior.unit.equipped[EquipmentSlot.MainHand] = eq73Def;
            warrior.unit.equippedInstances[EquipmentSlot.MainHand] = eq73;

            // Target at 90% HP (> 70%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.90f);
            float physMult1 = 1f;
            int brk1 = 0;
            manager.ModifyDamageDealt(warrior, target, ref physMult1, ref brk1, false, isSkill: false);
            Assert.AreEqual(1.15f, physMult1, 0.001f, "EQ073 must boost damage by 15% when target > 70% HP");

            // Target at 50% HP (<= 70%)
            target.unit.hp = (int)(target.Stats.maxHp * 0.50f);
            float physMult2 = 1f;
            int brk2 = 0;
            manager.ModifyDamageDealt(warrior, target, ref physMult2, ref brk2, false, isSkill: false);
            Assert.AreEqual(1.00f, physMult2, 0.001f, "EQ073 must NOT boost damage when target <= 70% HP");

            // EQ073 Curse: Healing received reduced by 15% (healMultiplier = 0.85)
            float healMult = 1f;
            manager.ModifyHealing(warrior, warrior, ref healMult);
            Assert.AreEqual(0.85f, healMult, 0.001f, "EQ073 curse must reduce healing received by 15%");

            // EQ075 (Fists of the Hollow Saint): Skills cost 1 additional MP
            var martialArtist = new Combatant(new Unit(Database.Characters["rogue"]), Side.Party, 1, "Rogue", "ROG");
            var eq75Def = Database.Equipment["EQ075"];
            var eq75 = new EquipmentInstance(eq75Def);
            martialArtist.unit.equipped[EquipmentSlot.MainHand] = eq75Def;
            martialArtist.unit.equippedInstances[EquipmentSlot.MainHand] = eq75;
            var skill = new SkillDef { id = "test_strike", name = "Test Strike", mpCost = 4 };
            int effectiveCost = martialArtist.GetEffectiveMpCost(skill, null);
            Assert.AreEqual(5, effectiveCost, "EQ075 must increase MP cost by 1");

            // EQ089 (Covenant of a Thousand Wounds): +8% damage dealt, +3 Break Power, -20% healing received
            var eq89Def = Database.Equipment["EQ089"];
            var eq89 = new EquipmentInstance(eq89Def);
            warrior.unit.equipped[EquipmentSlot.Accessory] = eq89Def;
            warrior.unit.equippedInstances[EquipmentSlot.Accessory] = eq89;
            float covenantDmg = 1f;
            int covenantBrk = 0;
            manager.ModifyDamageDealt(warrior, target, ref covenantDmg, ref covenantBrk, false);
            Assert.AreEqual(1.08f, covenantDmg, 0.001f);
            Assert.AreEqual(3, covenantBrk);

            float covenantHeal = 1f;
            manager.ModifyHealing(warrior, warrior, ref covenantHeal);
            // EQ073 (-15%) and EQ089 (-20%): 0.85 * 0.80 = 0.68
            Assert.AreEqual(0.68f, covenantHeal, 0.001f);
        }

        [Test]
        public void EnemyStatScaling_FollowsBab10Section7Formulas()
        {
            var baseStats = new Stats
            {
                maxHp = 100,
                maxMp = 50,
                atk = 20,
                mag = 15,
                def = 10,
                spd = 10
            };
            int baseMdef = 8;

            // Normal Enemy in Dungeon 1: no scaling (1 + 0 = 1)
            var d1Stats = Unit.ScaleEnemyStats(baseStats, Tier.Normal, 1, baseMdef, out int d1Mdef);
            Assert.AreEqual(100, d1Stats.maxHp);
            Assert.AreEqual(50, d1Stats.maxMp);
            Assert.AreEqual(20, d1Stats.atk);
            Assert.AreEqual(15, d1Stats.mag);
            Assert.AreEqual(10, d1Stats.def);
            Assert.AreEqual(8, d1Mdef);
            Assert.AreEqual(10, d1Stats.spd);

            // Normal Enemy in Dungeon 2 (Dungeon - 1 = 1)
            // Growth rates: HP +12%, MP +6%, ATK/MAG +8%, DEF/MDEF +5%, SPD +2%
            var d2Stats = Unit.ScaleEnemyStats(baseStats, Tier.Normal, 2, baseMdef, out int d2Mdef);
            Assert.AreEqual(112, d2Stats.maxHp); // floor(100 * 1.12) = 112
            Assert.AreEqual(53, d2Stats.maxMp);  // floor(50 * 1.06) = 53
            Assert.AreEqual(21, d2Stats.atk);   // floor(20 * 1.08) = 21
            Assert.AreEqual(16, d2Stats.mag);   // floor(15 * 1.08) = 16
            Assert.AreEqual(10, d2Stats.def);   // floor(10 * 1.05) = 10
            Assert.AreEqual(8, d2Mdef);         // floor(8 * 1.05) = 8
            Assert.AreEqual(10, d2Stats.spd);   // floor(10 * 1.02) = 10

            // Elite Enemy in Dungeon 3 (Dungeon - 1 = 2)
            // Growth rates: HP +13%, MP +8%, ATK/MATK +9%, DEF/MDEF +5.5%, SPD +3%
            // HP: 100 * (1 + 0.13 * 2) = 100 * 1.26 = 126
            // ATK: 20 * (1 + 0.09 * 2) = 20 * 1.18 = 23.6 -> floor = 23
            var d3EliteStats = Unit.ScaleEnemyStats(baseStats, Tier.Elite, 3, baseMdef, out int _);
            Assert.AreEqual(126, d3EliteStats.maxHp);
            Assert.AreEqual(23, d3EliteStats.atk);

            // Boss Enemy in Dungeon 3 (Dungeon - 1 = 2)
            // Growth rates: HP +14%, MP +10%, ATK/MATK +10%, DEF/MDEF +6%, SPD +4%
            // HP: 100 * (1 + 0.14 * 2) = 100 * 1.28 = 128
            // ATK: 20 * (1 + 0.10 * 2) = 20 * 1.20 = 24
            var d3BossStats = Unit.ScaleEnemyStats(baseStats, Tier.Boss, 3, baseMdef, out int _);
            Assert.AreEqual(128, d3BossStats.maxHp);
            Assert.AreEqual(24, d3BossStats.atk);
        }

        [Test]
        public void DynamicInitiative_EffectiveSpeedFormula_ClampsBetween050And150()
        {
            var unit = new Unit(Database.Characters["knight"]);
            unit.stats.spd = 10;
            var c = new Combatant(unit, Side.Party, 0, "Knight", "KNT");

            // No modifiers: SPD = 10
            Assert.AreEqual(10, c.Speed);

            // Wayfarer speed buff (+10%): 10 * 1.10 = 11
            c.wayfarerSpeedBuffTurns = 1;
            Assert.AreEqual(11, c.Speed);

            // Remove buff, add Slow (-25%): 10 * 0.75 = 7
            c.wayfarerSpeedBuffTurns = 0;
            c.AddStatus(StatusId.Slow, 2, 0.25f, c);
            Assert.AreEqual(7, c.Speed);
        }

        [Test]
        public void ControlEffects_Bab11Section8_CannotStack_AndGrant1TurnImmunity()
        {
            var target = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNT");
            var enemyDef = Database.Enemies.Values.First();
            var caster = new Combatant(new Unit(enemyDef), Side.Enemy, 0, enemyDef.name, enemyDef.tag);

            target.StatusController.ResistanceProfile.baseResistance = 0f;
            target.StatusController.ResistanceProfile.statsResistance = 0f;

            // 1. Apply Stun
            var stunApply = new StatusApply { id = StatusId.Stun, turns = 1, chance = 1f };
            bool applied1 = target.StatusController.TryApplyStatus(stunApply, caster, target, out var inst1);
            Assert.IsTrue(applied1, "Initial Stun should be applied successfully");
            Assert.IsTrue(target.StatusController.HasStatus(StatusId.Stun));

            // 2. Second Stun application must fail per Bab 11 §8.1 (Control effects cannot stack or refresh while active)
            bool applied2 = target.StatusController.TryApplyStatus(stunApply, caster, target, out _);
            Assert.IsFalse(applied2, "Cannot stack or re-apply active Control Effect");

            // 3. Process turn until Stun expires -> grants 1 turn Control Immunity
            target.StatusController.ProcessTurnStart(null, target, null);
            Assert.IsFalse(target.StatusController.HasStatus(StatusId.Stun), "Stun should have expired");
            Assert.IsTrue(target.StatusController.HasControlImmunity(StatusId.Stun), "Target must gain 1-turn Stun Immunity");

            // 4. Attempting to apply Stun while immune must fail
            bool appliedImmune = target.StatusController.TryApplyStatus(stunApply, caster, target, out _);
            Assert.IsFalse(appliedImmune, "Cannot apply Stun during Control Immunity window");

            // 5. Decrement control immunities (turn passes)
            target.StatusController.DecrementControlImmunities();
            Assert.IsFalse(target.StatusController.HasControlImmunity(StatusId.Stun), "Immunity expires after 1 turn");

            // 6. Now Stun can be applied again
            bool appliedAgain = target.StatusController.TryApplyStatus(stunApply, caster, target, out _);
            Assert.IsTrue(appliedAgain, "Stun can be applied again once immunity has expired");
        }

        [Test]
        public void TalentRespec_ScrollOfEmptiness_RefundsAllocatedPoints()
        {
            var unit = new Unit(Database.Characters["knight"]);
            unit.level = 6; // Milestone 3 and 6: 2 slots
            Assert.AreEqual(2, unit.EarnedTalentSlots);
            Assert.AreEqual(2, unit.AvailableTalentSelections);

            // Learn alert
            bool selected = unit.TrySelectTalent("alert", out string selectErr);
            Assert.IsTrue(selected, selectErr);
            Assert.IsTrue(unit.HasTalent("alert"));
            Assert.AreEqual(1, unit.selectedTalentIds.Count);
            Assert.AreEqual(1, unit.AvailableTalentSelections);

            // Add Scroll of Emptiness to inventory
            var inv = new InventoryRuntime();
            inv.AddConsumable("CON_SCROLL_OF_EMPTINESS", 1);
            Assert.AreEqual(1, inv.GetConsumableCount("CON_SCROLL_OF_EMPTINESS"));

            // Use Scroll of Emptiness
            bool used = inv.UseScrollOfEmptiness(unit, out string error);
            Assert.IsTrue(used, error);

            // Verify points reset and refunded
            Assert.AreEqual(0, unit.selectedTalentIds.Count, "All allocated talents must be cleared");
            Assert.IsFalse(unit.HasTalent("alert"));
            Assert.AreEqual(2, unit.AvailableTalentSelections, "Available slots must be fully refunded");
            Assert.AreEqual(0, inv.GetConsumableCount("CON_SCROLL_OF_EMPTINESS"), "Scroll must be consumed on use");
        }
    }
}
