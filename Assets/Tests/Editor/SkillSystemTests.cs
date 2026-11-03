using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    [TestFixture]
    public class SkillSystemTests
    {
        [Test]
        public void TierGating_MatchesFiveTierSpecification()
        {
            Assert.AreEqual(1, SkillLoadout.GetMaxUnlockedTier(1));
            Assert.AreEqual(1, SkillLoadout.GetMaxUnlockedTier(3));
            Assert.AreEqual(2, SkillLoadout.GetMaxUnlockedTier(4));
            Assert.AreEqual(2, SkillLoadout.GetMaxUnlockedTier(6));
            Assert.AreEqual(3, SkillLoadout.GetMaxUnlockedTier(7));
            Assert.AreEqual(3, SkillLoadout.GetMaxUnlockedTier(9));
            Assert.AreEqual(4, SkillLoadout.GetMaxUnlockedTier(10));
            Assert.AreEqual(4, SkillLoadout.GetMaxUnlockedTier(12));
            Assert.AreEqual(5, SkillLoadout.GetMaxUnlockedTier(13));
            Assert.AreEqual(5, SkillLoadout.GetMaxUnlockedTier(15));
        }

        [Test]
        public void AllNineClasses_HaveCompleteTwentyFiveSkillDefinitionsAndProgressionTrees()
        {
            string[] classIds = { "fighter", "barbarian", "martial_artist", "rogue", "ranger", "cleric", "wizard", "warlock", "druid" };

            foreach (var cid in classIds)
            {
                Assert.IsTrue(Database.Classes.ContainsKey(cid), $"Class '{cid}' missing from Database.Classes");
                var cDef = Database.Classes[cid];

                // 3 Branch names
                Assert.AreEqual(3, cDef.branchNames.Length, $"Class '{cid}' should have 3 branch names");

                // 3 Starting active skills
                Assert.AreEqual(3, cDef.startingSkills.Length, $"Class '{cid}' should have 3 starting skills");
                foreach (var sid in cDef.startingSkills)
                {
                    Assert.IsTrue(Database.Skills.ContainsKey(sid), $"Starting skill '{sid}' of class '{cid}' missing from Database.Skills");
                }

                // 1 Starter passive
                Assert.IsFalse(string.IsNullOrEmpty(cDef.starterPassiveId), $"Class '{cid}' missing starter passive");
                Assert.IsTrue(Database.Passives.ContainsKey(cDef.starterPassiveId), $"Starter passive '{cDef.starterPassiveId}' of class '{cid}' missing from Database.Passives");

                // 15 Active skills in pool
                Assert.GreaterOrEqual(cDef.activeSkillPool.Length, 15, $"Class '{cid}' should have at least 15 active skills");
                foreach (var sid in cDef.activeSkillPool)
                {
                    Assert.IsTrue(Database.Skills.ContainsKey(sid), $"Active skill '{sid}' of class '{cid}' missing from Database.Skills");
                    var s = Database.Skills[sid];
                    Assert.AreEqual(cid, s.classId, $"Skill '{sid}' classId mismatch");
                    Assert.GreaterOrEqual(s.tier, 1);
                    Assert.LessOrEqual(s.tier, 5);
                    Assert.GreaterOrEqual(s.branchIndex, 0);
                    Assert.LessOrEqual(s.branchIndex, 2);
                }

                // 10 Passives in pool
                Assert.GreaterOrEqual(cDef.passiveSkillPool.Length, 10, $"Class '{cid}' should have at least 10 passives");
                foreach (var pid in cDef.passiveSkillPool)
                {
                    Assert.IsTrue(Database.Passives.ContainsKey(pid), $"Passive '{pid}' of class '{cid}' missing from Database.Passives");
                    var p = Database.Passives[pid];
                    if (!p.isStarterPassive)
                        Assert.AreEqual(cid, p.classId, $"Passive '{pid}' classId mismatch");
                }

                // 21 Unlock progression nodes (25 total skills - 3 starting actives - 1 starter passive)
                Assert.AreEqual(21, cDef.skillUnlockProgression.Length, $"Class '{cid}' should have exactly 21 unlock nodes");
                foreach (var node in cDef.skillUnlockProgression)
                {
                    if (node.isPassive)
                        Assert.IsTrue(Database.Passives.ContainsKey(node.skillId), $"Node passive '{node.skillId}' of class '{cid}' missing from Database.Passives");
                    else
                        Assert.IsTrue(Database.Skills.ContainsKey(node.skillId), $"Node active '{node.skillId}' of class '{cid}' missing from Database.Skills");

                    if (node.prerequisites != null && node.prerequisites.Length > 0)
                    {
                        foreach (var prereq in node.prerequisites)
                        {
                            bool exists = Database.Skills.ContainsKey(prereq) || Database.Passives.ContainsKey(prereq);
                            Assert.IsTrue(exists, $"Prerequisite '{prereq}' for node '{node.skillId}' in class '{cid}' does not exist");
                        }
                    }
                }
            }
        }

        [Test]
        public void FighterClass_HasCompleteTwentyFiveSkillDefinitions()
        {
            var fighterDef = Database.Classes["fighter"];
            Assert.IsNotNull(fighterDef);

            // Starting skills
            Assert.AreEqual(3, fighterDef.startingSkills.Length);
            CollectionAssert.Contains(fighterDef.startingSkills, "shield_bash");
            CollectionAssert.Contains(fighterDef.startingSkills, "power_strike");
            CollectionAssert.Contains(fighterDef.startingSkills, "guard_stance");

            // Starter passive
            Assert.AreEqual("weapon_proficiency", fighterDef.starterPassiveId);

            // Verify all 15 active skills exist in Database.Skills
            string[] expectedActives =
            {
                "shield_bash", "power_strike", "guard_stance",
                "cleaving_strike", "shield_wall", "weapon_disarm",
                "counterattack", "armor_break", "tactical_strike",
                "whirlwind", "iron_fortress", "execution_strike",
                "blade_tempest", "unbreakable", "final_verdict"
            };
            foreach (var sid in expectedActives)
            {
                Assert.IsTrue(Database.Skills.ContainsKey(sid), $"Missing active skill: {sid}");
                var s = Database.Skills[sid];
                Assert.AreEqual("fighter", s.classId);
            }

            // Verify all 10 passives exist in Database.Passives
            string[] expectedPassives =
            {
                "weapon_proficiency", "defensive_training",
                "combat_discipline", "shield_mastery",
                "tactical_awareness", "weapon_adaptation",
                "iron_will", "counter_specialist",
                "master_of_arms", "unyielding_defense"
            };
            foreach (var pid in expectedPassives)
            {
                Assert.IsTrue(Database.Passives.ContainsKey(pid), $"Missing passive skill: {pid}");
                var p = Database.Passives[pid];
                Assert.AreEqual("fighter", p.classId);
            }

            // Total progression nodes
            Assert.GreaterOrEqual(fighterDef.skillUnlockProgression.Length, 20);
        }

        [Test]
        public void LevelUp_AwardsTwoSkillPoints()
        {
            var fighterDef = Database.Classes["fighter"];
            var loadout = new SkillLoadout();
            loadout.InitializeForClass(fighterDef, 1);
            loadout.SkillPoints = 0;

            loadout.SyncLevelUnlocks(fighterDef, 2);
            Assert.AreEqual(2, loadout.SkillPoints);

            loadout.SyncLevelUnlocks(fighterDef, 3);
            Assert.AreEqual(4, loadout.SkillPoints);
        }

        [Test]
        public void TryUnlockSkill_EnforcesLevelAndTierGating()
        {
            var fighterDef = Database.Classes["fighter"];
            var loadout = new SkillLoadout();
            loadout.InitializeForClass(fighterDef, 1);
            loadout.SkillPoints = 5;

            // cleaving_strike is Tier 2 (requires level 4)
            bool unlockedAtLevel1 = loadout.TryUnlockSkill("cleaving_strike", 1, fighterDef, out string err1);
            Assert.IsFalse(unlockedAtLevel1);
            Assert.IsTrue(err1.Contains("Requires character level 4"));

            // At level 4 with power_strike unlocked, it should succeed
            bool unlockedAtLevel4 = loadout.TryUnlockSkill("cleaving_strike", 4, fighterDef, out string err2);
            Assert.IsTrue(unlockedAtLevel4, err2);
            Assert.IsTrue(loadout.UnlockedActiveSkills.Contains("cleaving_strike"));
            Assert.AreEqual(1, loadout.GetRank("cleaving_strike"));
            Assert.AreEqual(4, loadout.SkillPoints); // 5 - 1 = 4
        }

        [Test]
        public void TryUnlockSkill_EnforcesPrerequisites()
        {
            var fighterDef = Database.Classes["fighter"];
            var loadout = new SkillLoadout();
            loadout.InitializeForClass(fighterDef, 1);
            loadout.SkillPoints = 10;

            // Cleaving strike requires power_strike
            // Remove power_strike from unlocked to simulate missing prerequisite
            loadout.UnlockedActiveSkills.Remove("power_strike");

            bool unlocked = loadout.TryUnlockSkill("cleaving_strike", 4, fighterDef, out string err);
            Assert.IsFalse(unlocked);
            Assert.IsTrue(err.Contains("Requires prerequisite skill"));

            // Re-add prerequisite
            loadout.UnlockActive("power_strike");
            bool unlockedAfter = loadout.TryUnlockSkill("cleaving_strike", 4, fighterDef, out string errAfter);
            Assert.IsTrue(unlockedAfter, errAfter);
        }

        [Test]
        public void TryUpgradeRank_ProgressionCostsAndCaps()
        {
            var fighterDef = Database.Classes["fighter"];
            var loadout = new SkillLoadout();
            loadout.InitializeForClass(fighterDef, 1);
            loadout.SkillPoints = 5;

            // power_strike starts at Rank 1
            Assert.AreEqual(1, loadout.GetRank("power_strike"));

            // Upgrade to Rank 2 costs 1 SP
            bool up1 = loadout.TryUpgradeRank("power_strike", out string err1);
            Assert.IsTrue(up1, err1);
            Assert.AreEqual(2, loadout.GetRank("power_strike"));
            Assert.AreEqual(4, loadout.SkillPoints); // 5 - 1 = 4

            // Upgrade to Rank 3 costs 2 SP
            bool up2 = loadout.TryUpgradeRank("power_strike", out string err2);
            Assert.IsTrue(up2, err2);
            Assert.AreEqual(3, loadout.GetRank("power_strike"));
            Assert.AreEqual(2, loadout.SkillPoints); // 4 - 2 = 2

            // Upgrade past Rank 3 should fail
            bool up3 = loadout.TryUpgradeRank("power_strike", out string err3);
            Assert.IsFalse(up3);
            Assert.IsTrue(err3.Contains("already at max rank"));
        }

        [Test]
        public void SkillLoadout_EquipEnforcesSlotLimitsAndUniqueAssignments()
        {
            var fighterDef = Database.Classes["fighter"];
            var loadout = new SkillLoadout();
            loadout.InitializeForClass(fighterDef, 1);

            // Starting skills: shield_bash, power_strike, guard_stance
            Assert.AreEqual("shield_bash", loadout.ActiveSlots[0]);
            Assert.AreEqual("power_strike", loadout.ActiveSlots[1]);
            Assert.AreEqual("guard_stance", loadout.ActiveSlots[2]);

            // Equipping power_strike into Slot 0 should shift it and clear Slot 1
            bool ok = loadout.TryEquipActive(0, "power_strike", out string err);
            Assert.IsTrue(ok, err);
            Assert.AreEqual("power_strike", loadout.ActiveSlots[0]);
            Assert.IsNull(loadout.ActiveSlots[1]); // unequipped from previous slot to prevent duplicates
            Assert.AreEqual("guard_stance", loadout.ActiveSlots[2]);

            // Cannot equip locked skill
            bool lockedOk = loadout.TryEquipActive(1, "blade_tempest", out string lockedErr);
            Assert.IsFalse(lockedOk);
            Assert.IsTrue(lockedErr.Contains("not unlocked"));
        }

        [Test]
        public void Respec_RefundsAllSkillPointsAndResetsTree()
        {
            var fighterDef = Database.Classes["fighter"];
            var loadout = new SkillLoadout();
            loadout.InitializeForClass(fighterDef, 5); // Level 5: 2 base + 4*2 = 10 SP
            loadout.SkillPoints = 10;

            // Spend 1 SP to unlock cleaving_strike
            Assert.IsTrue(loadout.TryUnlockSkill("cleaving_strike", 5, fighterDef, out _));
            // Spend 1 SP to upgrade power_strike to Rank 2
            Assert.IsTrue(loadout.TryUpgradeRank("power_strike", out _));
            // Spend 2 SP to upgrade power_strike to Rank 3
            Assert.IsTrue(loadout.TryUpgradeRank("power_strike", out _));
            Assert.AreEqual(6, loadout.SkillPoints); // 10 - 4 = 6

            // Respec at Level 5
            loadout.Respec(fighterDef, 5);
            Assert.AreEqual(10, loadout.SkillPoints); // 100% refunded
            Assert.AreEqual(1, loadout.GetRank("power_strike")); // Reset to Rank 1
            Assert.IsFalse(loadout.UnlockedActiveSkills.Contains("cleaving_strike")); // Locked again
        }

        [Test]
        public void WeaponRequirements_ShieldBashEnforcesEquippedShield()
        {
            var cDef = Database.Characters["knight"];
            var hero = new Unit(cDef);

            // Ensure hero has shield_bash equipped
            hero.skillLoadout.UnlockActive("shield_bash");
            hero.skillLoadout.ActiveSlots[0] = "shield_bash";

            // Equip weapon in MainHand and shield in SecondHand
            hero.TryEquip(EquipmentSlot.MainHand, Database.Equipment["iron_broadsword"], out _);
            hero.TryEquip(EquipmentSlot.SecondHand, Database.Equipment["iron_shield"], out _);
            Assert.IsTrue(hero.HasEquippedShield());

            // Boost speed so hero always acts first
            hero.stats = new Stats(hero.stats.maxHp, hero.stats.maxMp, hero.stats.atk, hero.stats.def, 99, hero.stats.mag);

            var enemyDef = Database.Enemies["skeleton"];
            var battle = new Battle(new[] { hero }, new[] { enemyDef }, new List<string>(), new System.Random(42));
            var heroCombatant = battle.NextTurn();
            Assert.AreEqual(heroCombatant.unit, hero);
            var enemyCombatant = battle.Enemies[0];

            var actionWithShield = BattleAction.Skill(heroCombatant, Database.Skills["shield_bash"], enemyCombatant);
            Assert.IsNull(battle.Validate(actionWithShield));

            // Unequip shield (leaving broadsword in MainHand so unit is in equipment mode)
            hero.Unequip(EquipmentSlot.SecondHand);
            Assert.IsFalse(hero.HasEquippedShield());

            var actionWithoutShield = BattleAction.Skill(heroCombatant, Database.Skills["shield_bash"], enemyCombatant);
            string validationError = battle.Validate(actionWithoutShield);
            Assert.IsNotNull(validationError);
            Assert.IsTrue(validationError.Contains("Requires an equipped shield"));
        }

        [Test]
        public void SkillPowerAndBreak_ScaleWithRank()
        {
            var skill = Database.Skills["shield_bash"];
            Assert.IsNotNull(skill);

            float p1 = skill.GetPower(1);
            float p2 = skill.GetPower(2);
            float p3 = skill.GetPower(3);

            Assert.Less(p1, p2);
            Assert.Less(p2, p3);

            float b1 = skill.GetBreakMultiplier(1);
            float b2 = skill.GetBreakMultiplier(2);
            float b3 = skill.GetBreakMultiplier(3);

            Assert.Less(b1, b2);
            Assert.Less(b2, b3);
        }
    }
}
