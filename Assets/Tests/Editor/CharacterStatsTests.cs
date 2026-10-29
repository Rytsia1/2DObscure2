using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    public class CharacterStatsTests
    {
        // =====================================================================
        // 1. Primary Ability Scores & Modifiers
        // =====================================================================

        [Test]
        public void AllSixPrimaryAbilityScoresAreImplemented()
        {
            var expected = new[] { Ability.STR, Ability.DEX, Ability.VIT, Ability.INT, Ability.WIS, Ability.CHA };
            CollectionAssert.AreEqual(expected, AbilityScores.AllAbilities);
            foreach (var a in expected)
            {
                Assert.IsTrue(Database.AbilityDefs.ContainsKey(a), $"Missing AbilityDef for {a}");
                Assert.AreEqual(a.ToString(), Database.AbilityDefs[a].abilityId);
                Assert.AreEqual(8, Database.AbilityDefs[a].baseScore);
                Assert.AreEqual(8, Database.AbilityDefs[a].minimumScore);
                Assert.AreEqual(20, Database.AbilityDefs[a].maximumScore);
                Assert.IsTrue(Database.AbilityDefs[a].pointBuyEnabled);
            }
        }

        [Test]
        public void AbilityModifierFormulaMatchesPrdAndUsesMathematicalFloor()
        {
            // Exact PRD table (8..20)
            var table = new (int score, int expectedMod)[]
            {
                (8, -1), (9, -1), (10, 0), (11, 0), (12, 1), (13, 1),
                (14, 2), (15, 2), (16, 3), (17, 3), (18, 4), (19, 4), (20, 5)
            };
            foreach (var (score, expectedMod) in table)
                Assert.AreEqual(expectedMod, AbilityScores.GetModifier(score), $"Score {score}");

            // Mathematical floor check on lower negative odd values (truncation toward zero would fail on 9, 7, 5)
            Assert.AreEqual(-2, AbilityScores.GetModifier(7));
            Assert.AreEqual(-2, AbilityScores.GetModifier(6));
            Assert.AreEqual(-3, AbilityScores.GetModifier(5));
        }

        [Test]
        public void AbilityScoreSourcesStaySeparateAndRecalculateCleanly()
        {
            var unit = new Unit(Database.Characters["knight"]);
            var baseBefore = unit.BaseScores;
            var raceBefore = unit.RacialModifiers;
            Assert.AreEqual(new AbilityScores(15, 10, 15, 8, 12, 11), baseBefore);
            Assert.AreEqual(new AbilityScores(1, 0, 1, 0, 0, 1), raceBefore); // Human: +1 STR, +1 VIT, +1 CHA
            Assert.AreEqual(new AbilityScores(16, 10, 16, 8, 12, 12), unit.FinalScores);

            int atkBefore = unit.Derived.physicalPower;
            unit.Equip(Database.Equipment["steel_cuirass"]); // +1 VIT, +4 PhysDef, +1 MagDef
            Assert.AreEqual(1, unit.EquipmentAbilityBonuses.vit);
            Assert.AreEqual(baseBefore, unit.BaseScores, "Base scores untouched by equipment");
            Assert.AreEqual(17, unit.Score(Ability.VIT));

            unit.AddPermanentAbilityBonus(Ability.STR, 2); // STR 16 -> 18 (mod +3 -> +4)
            Assert.AreEqual(2, unit.PermanentAbilityBonuses.str);
            Assert.AreEqual(18, unit.Score(Ability.STR));
            Assert.AreEqual(4, unit.Mod(Ability.STR));
            Assert.Greater(unit.Derived.physicalPower, atkBefore);

            unit.SetTemporaryAbilityModifiers(new AbilityScores(-2, 0, 0, 0, 0, 0));
            Assert.AreEqual(-2, unit.TemporaryAbilityModifiers.str);
            Assert.AreEqual(16, unit.Score(Ability.STR));

            unit.ClearTemporaryModifiers();
            Assert.AreEqual(18, unit.Score(Ability.STR));
            unit.Unequip(EquipmentSlot.Armor);
            Assert.AreEqual(0, unit.EquipmentAbilityBonuses.vit);
            Assert.AreEqual(16, unit.Score(Ability.VIT));
        }

        // =====================================================================
        // 2. Point Buy System & Validation
        // =====================================================================

        [Test]
        public void PointBuyInitialStateAndCostTableMatchPrd()
        {
            var pb = new PointBuyAllocation();
            Assert.AreEqual(27, pb.Config.budget);
            Assert.AreEqual(27, pb.RemainingPoints);
            Assert.AreEqual(0, pb.TotalSpent);
            Assert.AreEqual(AbilityScores.Uniform(8), pb.Scores);

            var expectedCosts = new Dictionary<int, int>
            {
                [8] = 0, [9] = 1, [10] = 2, [11] = 3, [12] = 4, [13] = 5, [14] = 7, [15] = 9
            };
            foreach (var kv in expectedCosts)
                Assert.AreEqual(kv.Value, pb.Config.GetCost(kv.Key), $"Cost for score {kv.Key}");
        }

        [Test]
        public void PointBuyExampleWarriorBerserkerBuildCostsExactly27()
        {
            // Prompt Section 2 Validation build: STR=15, DEX=12, VIT=15, INT=8, WIS=10, CHA=11 -> 9+4+9+0+2+3 = 27
            var session = new CharacterCreationSession();
            var build = new AbilityScores(15, 12, 15, 8, 10, 11);
            Assert.IsTrue(session.PointBuy.TrySetAll(build));
            Assert.AreEqual(9, session.PointBuy.GetCost(Ability.STR));
            Assert.AreEqual(4, session.PointBuy.GetCost(Ability.DEX));
            Assert.AreEqual(9, session.PointBuy.GetCost(Ability.VIT));
            Assert.AreEqual(0, session.PointBuy.GetCost(Ability.INT));
            Assert.AreEqual(2, session.PointBuy.GetCost(Ability.WIS));
            Assert.AreEqual(3, session.PointBuy.GetCost(Ability.CHA));
            Assert.AreEqual(27, session.PointBuy.TotalSpent);
            Assert.AreEqual(0, session.PointBuy.RemainingPoints);
            Assert.IsTrue(session.PointBuy.IsValid);
            Assert.IsTrue(session.CanConfirm);

            // Cannot spend even 1 more point when remaining is 0
            Assert.IsFalse(session.IncreaseStat(Ability.INT));
            Assert.AreEqual(8, session.BaseScores.intel);
            Assert.AreEqual(0, session.PointBuy.RemainingPoints);
        }

        [Test]
        public void PointBuyRejectsScoresOutsideRangeAndOverBudget()
        {
            var pb = new PointBuyAllocation();

            // Cannot decrease below 8
            foreach (var a in AbilityScores.AllAbilities)
            {
                Assert.IsFalse(pb.CanDecrease(a));
                Assert.IsFalse(pb.TryDecrease(a));
                Assert.AreEqual(8, pb.GetScore(a));
            }
            Assert.IsFalse(pb.TrySetScore(Ability.STR, 7));
            Assert.IsNotNull(pb.Config.Validate(new AbilityScores(7, 8, 8, 8, 8, 8)));

            // Cannot increase above 15 before racial modifiers
            for (int s = 8; s < 15; s++) Assert.IsTrue(pb.TryIncrease(Ability.STR));
            Assert.AreEqual(15, pb.GetScore(Ability.STR));
            Assert.AreEqual(18, pb.RemainingPoints); // 27 - 9 = 18 points still left, but STR is at cap 15
            Assert.IsFalse(pb.CanIncrease(Ability.STR));
            Assert.IsFalse(pb.TryIncrease(Ability.STR));
            Assert.IsFalse(pb.TrySetScore(Ability.STR, 16));
            Assert.IsNotNull(pb.Config.Validate(new AbilityScores(16, 8, 8, 8, 8, 8)));

            // Cannot exceed 27 points total
            Assert.IsTrue(pb.TrySetAll(new AbilityScores(15, 15, 15, 8, 8, 8))); // 9 + 9 + 9 = 27
            Assert.AreEqual(0, pb.RemainingPoints);
            Assert.IsFalse(pb.TryIncrease(Ability.INT));
            Assert.IsFalse(pb.TrySetAll(new AbilityScores(15, 15, 15, 9, 8, 8))); // 28 > 27
            Assert.Throws<InvalidOperationException>(() => pb.SetAllOrThrow(new AbilityScores(15, 15, 15, 15, 8, 8)));

            // Reset restores all scores to 8 and remaining points to 27
            pb.Reset();
            Assert.AreEqual(AbilityScores.Uniform(8), pb.Scores);
            Assert.AreEqual(27, pb.RemainingPoints);
        }

        // =====================================================================
        // 3. Character Creation Flow (Race, Class, Background, Roster)
        // =====================================================================

        [Test]
        public void CharacterCreationFlowAppliesRaceAfterPointBuyAndPreservesUnconventionalBuilds()
        {
            var roster = new CharacterRoster();
            var session = new CharacterCreationSession();

            // Allocate an unconventional INT-focused Warrior before choosing class/race
            session.PointBuy.SetAllOrThrow(new AbilityScores(10, 12, 12, 15, 13, 11)); // 2+4+4+9+5+3 = 27
            var allocated = session.BaseScores;

            session.SelectRace("elf");       // +2 DEX, +1 INT
            session.SelectClass("knight");   // Class selection must NOT overwrite Point Buy scores
            session.SelectBackground("acolyte");

            Assert.AreEqual(allocated, session.BaseScores, "Class/Background selection never overwrites Point Buy scores");
            Assert.AreEqual(27, session.PointBuy.TotalSpent, "Background/Race never alter Point Buy costs");
            // Elf +1 INT raises 15 -> 16 after Point Buy
            Assert.AreEqual(16, session.FinalScores.intel);
            Assert.AreEqual(14, session.FinalScores.dex);
            Assert.AreEqual(3, session.AbilityModifiers.intel);
            Assert.AreEqual(2, session.AbilityModifiers.dex);

            // Live Derived Stats preview updates immediately when scores change
            int hpBefore = session.PreviewDerivedStats.maxHp;
            session.DecreaseStat(Ability.CHA); // CHA 11 -> 10 (+1 pt)
            session.DecreaseStat(Ability.WIS); // WIS 13 -> 12 (+1 pt)
            session.IncreaseStat(Ability.VIT); // VIT 12 -> 13 (-1 pt)
            session.IncreaseStat(Ability.VIT); // VIT 13 -> 14 (-2 pts -> need 1 more, so let's check!)
            session.DecreaseStat(Ability.STR); // STR 10 -> 9 (+1 pt)
            Assert.IsTrue(session.IncreaseStat(Ability.VIT)); // VIT 13 -> 14 (mod +1 -> +2)
            Assert.Greater(session.PreviewDerivedStats.maxHp, hpBefore, "Preview Max HP updates immediately");

            var created = session.Confirm(roster, "custom_spellblade");
            CollectionAssert.Contains(roster.Characters, created);
            roster.SetPartySlot(0, created.id);
            Assert.AreEqual("custom_spellblade", roster.GetActiveParty()[0].id);
        }

        // =====================================================================
        // 4. Derived Stats & Recalculation
        // =====================================================================

        [Test]
        public void DerivedStatsFormulasAndRecalculationAreAccurate()
        {
            var knight = new Unit(Database.Characters["knight"]);
            // Knight: Human (+1 STR, +1 VIT, +1 CHA) + Base(15,10,15,8,12,11) = Final(16,10,16,8,12,12)
            // Modifiers: STR +3, DEX 0, VIT +3, INT -1, WIS +1, CHA +1
            Assert.AreEqual(125, knight.Derived.maxHp);           // 95 + 3 * 10
            Assert.AreEqual(20, knight.Derived.maxMp);            // 15 + 1 * 5
            Assert.AreEqual(14, knight.Derived.physicalPower);    // 8 + 3 * 2
            Assert.AreEqual(4, knight.Derived.magicPower);        // 6 + (-1) * 2
            Assert.AreEqual(8, knight.Derived.healingPower);      // 6 + 1 * 2
            Assert.AreEqual(10, knight.Derived.supportPower);     // 8 + 1 * 2
            Assert.AreEqual(13, knight.Derived.physicalDefense);  // 7 + 3 * 2
            Assert.AreEqual(7, knight.Derived.magicDefense);      // 5 + 1 * 2
            Assert.AreEqual(6, knight.Derived.initiative);        // 6 + 0

            // Changing VIT updates MaxHP, PhysicalDefense, StatusResistance, StaggerResistance, QteStability
            knight.AddPermanentAbilityBonus(Ability.VIT, 2); // VIT 16 -> 18 (mod +3 -> +4)
            Assert.AreEqual(135, knight.Derived.maxHp);
            Assert.AreEqual(15, knight.Derived.physicalDefense);

            // Changing WIS updates MaxMP, MagicDefense, HealingPower
            knight.AddPermanentAbilityBonus(Ability.WIS, 2); // WIS 12 -> 14 (mod +1 -> +2)
            Assert.AreEqual(25, knight.Derived.maxMp);
            Assert.AreEqual(9, knight.Derived.magicDefense);
            Assert.AreEqual(10, knight.Derived.healingPower);

            // Changing DEX updates Initiative, RangedPower, CritChance, Evasion, QteTimingBonus
            knight.AddPermanentAbilityBonus(Ability.DEX, 4); // DEX 10 -> 14 (mod 0 -> +2)
            Assert.AreEqual(8, knight.Derived.initiative);
            Assert.Greater(knight.Derived.qteTimingBonus, 0);
            Assert.Greater(knight.Derived.rangedPower, 7);
        }

        [Test]
        public void ProgressionLevelUpAsiAndRespecWorkCleanly()
        {
            var mage = new Unit(Database.Characters["mage"]);
            int hp1 = mage.Derived.maxHp;
            int mp1 = mage.Derived.maxMp;

            // Level up 1 -> 4 triggers ASI (configured at level 4)
            mage.LevelUp(); // Lv 2
            mage.LevelUp(); // Lv 3
            mage.LevelUp(); // Lv 4 -> +2 ASI points
            Assert.AreEqual(4, mage.level);
            Assert.AreEqual(2, mage.unspentAsiPoints);
            Assert.Greater(mage.Derived.maxHp, hp1);
            Assert.Greater(mage.Derived.maxMp, mp1);

            // Spend ASI points on INT (17 -> 19, mod +3 -> +4)
            int magBefore = mage.Derived.magicPower;
            Assert.IsTrue(mage.TrySpendAsiPoint(Ability.INT));
            Assert.IsTrue(mage.TrySpendAsiPoint(Ability.INT));
            Assert.AreEqual(0, mage.unspentAsiPoints);
            Assert.AreEqual(19, mage.Score(Ability.INT));
            Assert.Greater(mage.Derived.magicPower, magBefore);

            // Cannot exceed MaxAbilityScore (20) via ASI
            mage.unspentAsiPoints = 5;
            Assert.IsTrue(mage.TrySpendAsiPoint(Ability.INT)); // 19 -> 20
            Assert.IsFalse(mage.TrySpendAsiPoint(Ability.INT), "Capped at 20");
            Assert.AreEqual(20, mage.Score(Ability.INT));

            // Respec updates Point Buy base scores and recalculates Derived Stats while preserving identity
            mage.Respec(new AbilityScores(15, 12, 15, 8, 10, 11));
            Assert.AreEqual("mage", mage.classDef.id);
            Assert.AreEqual("highborn", mage.race.id);
            Assert.AreEqual(15, mage.BaseScores.str);
        }

        // =====================================================================
        // 5. Combat Integration, Separate Defenses, Break System & QTE Stats
        // =====================================================================

        [Test]
        public void PhysicalMagicAndHealingScaleWithRespectiveAbilityScoresAndSeparateDefenses()
        {
            var b = new Battle(
                Database.Party.Select(id => new Unit(Database.Characters[id])),
                new[] { Database.Enemies["cursed_knight"] },
                new List<string>(),
                new System.Random(10));

            var knight = b.Party[0];
            var mage = b.Party[2];
            var healer = b.Party[3];
            var target = b.Enemies[0];

            // STR increases Physical damage
            int physBase = Rules.Physical(knight, target, 1f, new System.Random(5));
            knight.unit.AddPermanentAbilityBonus(Ability.STR, 4); // +2 mod
            int physBoosted = Rules.Physical(knight, target, 1f, new System.Random(5));
            Assert.Greater(physBoosted, physBase);

            // INT increases Magic damage
            int magBase = Rules.Magic(mage, target, 1f, new System.Random(5));
            mage.unit.AddPermanentAbilityBonus(Ability.INT, 4); // +2 mod
            int magBoosted = Rules.Magic(mage, target, 1f, new System.Random(5));
            Assert.Greater(magBoosted, magBase);

            // WIS increases Healing amount
            int healBase = Rules.Healing(healer, 1.5f, new System.Random(5));
            healer.unit.AddPermanentAbilityBonus(Ability.WIS, 4); // +2 mod
            int healBoosted = Rules.Healing(healer, 1.5f, new System.Random(5));
            Assert.Greater(healBoosted, healBase);

            // Physical Defense and Magic Defense are completely separate
            int pDefBefore = knight.Def;
            int mDefBefore = knight.MagicDef;
            knight.AddStatus(StatusId.MagicDefenseUp, 2, 1.0f, knight);
            Assert.AreEqual(pDefBefore, knight.Def, "MagicDefenseUp does not change PhysicalDefense");
            Assert.Greater(knight.MagicDef, mDefBefore, "MagicDefenseUp increases MagicDefense");

            int magDmgBuffed = Rules.Magic(target, knight, 3f, new System.Random(5));
            knight.statuses.Clear();
            int magDmgUnbuffed = Rules.Magic(target, knight, 3f, new System.Random(5));
            Assert.Less(magDmgBuffed, magDmgUnbuffed, "MagicDefense mitigates magic damage");
        }

        [Test]
        public void BreakGaugeDepletesTriggersBrokenStateAndIncreasesDamageTaken()
        {
            var b = new Battle(
                Database.Party.Select(id => new Unit(Database.Characters[id])),
                new[] { Database.Enemies["skeleton"] },
                new List<string>(),
                new System.Random(1));

            var sk = b.Enemies[0];
            sk.unit.hp = 500; // keep alive to test full break cycle
            sk.MaxBreakGauge = 25;
            sk.CurrentBreakGauge = 25;

            var rogue = b.NextTurn(); // Rogue acts first
            int dmgUnbroken = Rules.Physical(rogue, sk, 1f, new System.Random(4));

            var hit1 = b.Perform(BattleAction.Attack(rogue, sk))[0];
            Assert.Greater(hit1.breakAmount, 0);
            Assert.Less(sk.CurrentBreakGauge, 25);

            // Next party member finishes breaking the skeleton
            var healer = b.NextTurn();
            sk.CurrentBreakGauge = 5;
            var hit2 = b.Perform(BattleAction.Attack(healer, sk))[0];
            Assert.IsTrue(hit2.brokeTarget);
            Assert.AreEqual(0, sk.CurrentBreakGauge);
            Assert.IsTrue(sk.IsBroken);
            Assert.GreaterOrEqual(sk.stunTurns, 1, "Broken applies action penalty");

            // Broken enemy takes increased damage
            int dmgBroken = Rules.Physical(rogue, sk, 1f, new System.Random(4));
            Assert.Greater(dmgBroken, dmgUnbroken);
        }

        [Test]
        public void QteStatInteractionsAndOutcomeCategoriesWorkWithoutRemovingChallenge()
        {
            var profile = QteConfig.Profiles[Tier.Elite]; // perfect = 0.065, good = 0.160

            // An error of 0.165s is just outside the base Elite Good window (0.160s), so base Judge is Miss / PartialSuccess
            Assert.AreEqual(QteResult.Miss, QteConfig.Judge(0.165f, profile, 0f));
            Assert.AreEqual(QteOutcomeCategory.PartialSuccess, QteConfig.JudgeCategory(0.165f, profile, 0f));

            // With DEX QTE Timing Bonus (+6% = 0.06f), effective good window is 0.160 * 1.06 = 0.1696s -> Good / Success!
            Assert.AreEqual(QteResult.Good, QteConfig.Judge(0.165f, profile, 0.06f));
            Assert.AreEqual(QteOutcomeCategory.Success, QteConfig.JudgeCategory(0.165f, profile, 0.06f));

            // Large errors still Miss even with max DEX bonus (player reaction still required!)
            Assert.AreEqual(QteResult.Miss, QteConfig.Judge(0.35f, profile, 0.25f));
            Assert.AreEqual(QteOutcomeCategory.Failure, QteConfig.JudgeCategory(0.35f, profile, 0.25f));

            // VIT Stability reduces PartialSuccess / Failure defense damage multiplier
            var knight = new Combatant(new Unit(Database.Characters["knight"]), Side.Party, 0, "Knight", "KNI");
            float partialMult = QteConfig.DefenseCategory(QteOutcomeCategory.PartialSuccess, knight, Element.Physical);
            Assert.Less(partialMult, QteConfig.PartialDefense);

            // STR + DEX Counter Power calculates positive counterattack damage
            var sk = new Combatant(new Unit(Database.Enemies["skeleton"]), Side.Enemy, 0, "Skeleton", "SK", Database.Enemies["skeleton"]);
            Assert.Greater(QteConfig.CalculateCounterDamage(knight, sk), 0);

            // CHA Support potency multiplier scales with positive CHA modifier
            Assert.Greater(QteConfig.SupportPotencyMultiplier(knight), 1f);
        }

        // =====================================================================
        // 6. UI Screens & Data-Driven Config Serialization
        // =====================================================================

        [Test]
        public void CharacterCreationAndStatusUiRenderAndUpdateRealTime()
        {
            var root = new GameObject("TestUiRoot").transform;
            try
            {
                // 1. Character Creation Screen
                var creation = new CharacterCreationScreen();
                creation.Open(root);
                Assert.AreEqual("POINTS 27/27", creation.RemainingPointsLabel.Text);
                Assert.AreEqual(6, creation.AbilityRowLabels.Count);

                int hpBefore = creation.Session.PreviewDerivedStats.maxHp;
                creation.Session.IncreaseStat(Ability.VIT);
                creation.Session.IncreaseStat(Ability.VIT); // 8 -> 10 (mod -1 -> 0)
                Assert.AreEqual("POINTS 25/27", creation.RemainingPointsLabel.Text);
                Assert.Greater(creation.Session.PreviewDerivedStats.maxHp, hpBefore);
                StringAssert.Contains(creation.Session.PreviewDerivedStats.maxHp.ToString(), creation.PreviewStatLabels[DerivedStatId.MaxHP].Text);

                // Verify UI labels never overflow 160px buttons (<=26 chars) or 320px bottom bar (<=50 chars)
                for (int r = 0; r < Database.PlayableRaceIds.Length; r++)
                {
                    creation.CycleRace(1);
                    Assert.LessOrEqual(creation.RaceLabel.Text.Length, 26, $"RaceLabel overflow: {creation.RaceLabel.Text}");
                    Assert.LessOrEqual(creation.RaceFeatLabel.Text.Length, 18, $"RaceFeatLabel overflow: {creation.RaceFeatLabel.Text}");
                }
                for (int c = 0; c < Database.PlayableClassIds.Length; c++)
                {
                    creation.CycleClass(1);
                    Assert.LessOrEqual(creation.ClassLabel.Text.Length, 26, $"ClassLabel overflow: {creation.ClassLabel.Text}");
                    Assert.LessOrEqual(creation.ClassIdentityLabel.Text.Length, 18, $"ClassIdentityLabel overflow: {creation.ClassIdentityLabel.Text}");
                    Assert.LessOrEqual(creation.StarterPassiveLabel.Text.Length, 26, $"StarterPassiveLabel overflow: {creation.StarterPassiveLabel.Text}");
                    for (int s = 0; s < EquipmentSlots.All.Length; s++)
                    {
                        creation.CycleEquipmentSlot(1);
                        Assert.LessOrEqual(creation.EquipmentSlotLabel.Text.Length, 26, $"EquipmentSlotLabel overflow: {creation.EquipmentSlotLabel.Text}");
                    }
                }
                for (int m = 0; m < Database.TemplateIds.Length + 3; m++)
                {
                    creation.CycleCreationMethodOrTemplate(1);
                    Assert.LessOrEqual(creation.MethodLabel.Text.Length, 26, $"MethodLabel overflow: {creation.MethodLabel.Text}");
                }
                Assert.LessOrEqual(creation.HintLabel.Text.Length, 50);
                Assert.LessOrEqual(creation.PartySummaryLabel.Text.Length, 50);
                creation.Close();

                // 2. Character Status Screen & Tooltips
                var party = Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
                party[0].Equip(Database.Equipment["steel_cuirass"]);
                var status = new CharacterStatusScreen(party);
                status.Open(root);

                Assert.AreEqual(6, status.PrimaryStatLabels.Count);
                Assert.AreEqual(CharacterStatusScreen.DisplayedDerivedStats.Length, status.DerivedStatLabels.Count);

                string defTooltip = status.GetStatTooltip(DerivedStatId.PhysicalDefense);
                StringAssert.Contains("PHYSICAL DEFENSE", defTooltip);
                StringAssert.Contains("SRC: VIT", defTooltip);
                StringAssert.Contains("FORMULA:", defTooltip);
                StringAssert.Contains("EQP +4", defTooltip);

                status.SwitchHero(1);
                Assert.AreEqual("Rogue", status.CurrentUnit.name);
                status.Close();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void DataDrivenConfigSerializesAndDeserializesViaJson()
        {
            string json = Database.ExportStatConfigJson();
            Assert.IsNotEmpty(json);
            StringAssert.Contains("\"abilityId\": \"STR\"", json);
            StringAssert.Contains("\"statId\": \"MAX_HP\"", json);
            StringAssert.Contains("\"formulaId\": \"physical_damage_v1\"", json);

            var parsed = Database.ImportStatConfigJson(json);
            Assert.IsNotNull(parsed);
            Assert.AreEqual(6, parsed.abilities.Length);
            Assert.AreEqual(27, parsed.pointBuy.budget);
            Assert.AreEqual(20, parsed.asi.maxAbilityScore);
        }

        // =====================================================================
        // 7. Character Creation System — 7 Races, 9 Classes, Step Navigation & Starter Passives
        // =====================================================================

        [Test]
        public void AllSevenPlayableRacesHaveThreeComponentsAndSelectCleanly()
        {
            var expectedRaces = new[] { "human", "elf", "dwarf", "halfling", "dragonborn", "liichtian", "merfolk" };
            CollectionAssert.AreEqual(expectedRaces, Database.PlayableRaceIds);

            var session = new CharacterCreationSession();
            foreach (var raceId in expectedRaces)
            {
                session.SelectRace(raceId);
                var r = session.Race;
                Assert.AreEqual(raceId, r.id);
                Assert.IsTrue(r.isPlayable);
                Assert.IsNotEmpty(r.name);
                Assert.IsNotEmpty(r.description);
                // 3 Conceptual Components: Racial Ability Bonus, Racial Feat, Racial Utility
                Assert.IsFalse(r.abilityModifiers.IsZero, $"Race {raceId} should have Racial Ability Bonuses");
                Assert.IsNotNull(r.racialFeat, $"Race {raceId} must have a Racial Feat");
                Assert.IsNotEmpty(r.racialFeat.name);
                Assert.IsNotNull(r.racialUtility, $"Race {raceId} must have a Racial Utility");
                Assert.IsNotEmpty(r.racialUtility.name);
                // No separate passive-trait system on playable races
                Assert.AreEqual(0, r.traits.Length);
                Assert.AreEqual(0, r.passiveBonuses.Length);
            }
        }

        [Test]
        public void AllNinePlayableClassesHaveIdentityStarterPassiveSkillLoadoutAndCompatibleWeapons()
        {
            var expectedClasses = new (string id, string identity, string starterPassive)[]
            {
                ("fighter", "Weapon Techniques", "weapon_proficiency"),
                ("barbarian", "Berserk State", "savage_instinct"),
                ("martial_artist", "Combo Flow", "martial_discipline"),
                ("rogue", "Exploit", "opportunist"),
                ("ranger", "Hunt Mark", "hunters_instinct"),
                ("cleric", "Divine Protection", "divine_grace"),
                ("wizard", "Spell Specialization", "arcane_affinity"),
                ("warlock", "Pact Effects", "pact_affinity"),
                ("druid", "Nature Forms", "natures_attunement"),
            };

            CollectionAssert.AreEqual(expectedClasses.Select(x => x.id).ToArray(), Database.PlayableClassIds);

            var session = new CharacterCreationSession();
            foreach (var (id, identity, starterPassive) in expectedClasses)
            {
                session.CreateNewCharacter();
                session.SelectClass(id);
                var c = session.Class;
                Assert.AreEqual(id, c.id);
                Assert.IsTrue(c.isPlayable);
                Assert.AreEqual(identity, c.gameplayIdentity);
                Assert.AreEqual(starterPassive, c.starterPassiveId);
                Assert.IsNotNull(session.StarterPassive);
                Assert.AreEqual(starterPassive, session.StarterPassive.id);

                // Starter passive unlocked and equipped in Passive Slot 0
                Assert.IsTrue(session.Loadout.UnlockedPassiveSkills.Contains(starterPassive));
                Assert.AreEqual(starterPassive, session.Loadout.passiveSlots[0]);
                Assert.AreEqual(3, session.Loadout.activeSlots.Length);
                Assert.AreEqual(3, session.Loadout.passiveSlots.Length);

                // Every class has Main Hand and at least one compatible Second Hand weapon option
                Assert.IsNotEmpty(c.mainHandWeapons, $"{c.name} must define compatible Main Hand weapons");
                Assert.IsNotEmpty(c.secondHandWeapons, $"{c.name} must have at least one compatible Second Hand weapon");

                // All 8 equipment slots are initialized with valid compatible starting equipment
                Assert.AreEqual(8, session.Equipped.Count, $"{c.name} should have 8 starting equipment slots configured");
                foreach (var slot in EquipmentSlots.All)
                {
                    var item = session.GetEquipped(slot);
                    Assert.IsNotNull(item, $"{c.name} missing starting equipment in {slot}");
                    Assert.IsNull(EquipmentValidator.Validate(item, slot, c, session.Race, 1, session.FinalScores));
                }
            }
        }

        [Test]
        public void CharacterCreationStepNavigationAndStarterPassiveEffectsWorkAcrossClasses()
        {
            var session = new CharacterCreationSession();
            Assert.AreEqual(CreationStep.SelectRace, session.CurrentStep);
            Assert.IsTrue(session.NextStep());
            Assert.AreEqual(CreationStep.SelectClass, session.CurrentStep);
            Assert.IsTrue(session.NextStep());
            Assert.AreEqual(CreationStep.AllocateAbilityScores, session.CurrentStep);
            Assert.IsTrue(session.PreviousStep(), "Can return to previous steps before confirming");
            Assert.AreEqual(CreationStep.SelectClass, session.CurrentStep);
            Assert.IsTrue(session.GoToStep(CreationStep.ReviewStarterPassive));
            Assert.AreEqual(CreationStep.ReviewStarterPassive, session.CurrentStep);

            // 1. Fighter — Weapon Proficiency increases Physical Power when a weapon is equipped
            session.SelectClass("fighter");
            session.PointBuy.SetAllOrThrow(new AbilityScores(15, 12, 15, 8, 10, 11));
            var fighterDef = session.Confirm(null, "test_fighter");
            var fighterUnit = new Unit(fighterDef);
            int powerWithWeapon = fighterUnit.Derived.physicalPower;
            fighterUnit.Unequip(EquipmentSlot.MainHand);
            Assert.Greater(powerWithWeapon, fighterUnit.Derived.physicalPower);

            // 2. Barbarian — Savage Instinct grants Physical Power bonus when HP is below 50%
            session.CreateNewCharacter();
            session.SelectClass("barbarian");
            session.PointBuy.SetAllOrThrow(new AbilityScores(15, 12, 15, 8, 10, 11));
            var barbUnit = new Unit(session.Confirm(null, "test_barbarian"));
            var barbCombatant = new Combatant(barbUnit, Side.Party, 0, "Barbarian", "BRB");
            int atkFullHp = barbCombatant.Atk;
            barbUnit.hp = barbUnit.stats.maxHp / 2 - 1; // below 50% HP
            Assert.Greater(barbCombatant.Atk, atkFullHp, "Savage Instinct boosts Physical Power below 50% HP");

            // 3. Martial Artist — Martial Discipline increases Physical Break Power from unarmed attacks
            session.CreateNewCharacter();
            session.SelectClass("martial_artist");
            session.PointBuy.SetAllOrThrow(new AbilityScores(15, 14, 13, 8, 12, 10));
            var monkUnit = new Unit(session.Confirm(null, "test_monk"));
            Assert.IsTrue(monkUnit.equipped[EquipmentSlot.MainHand].isUnarmed);
            Assert.Greater(monkUnit.Derived.physicalBreakPower, monkUnit.classDef.baseBreakPower);

            // 4. Rogue — Opportunist increases Critical Chance against debuffed enemies
            session.CreateNewCharacter();
            session.SelectClass("rogue");
            session.PointBuy.SetAllOrThrow(new AbilityScores(10, 15, 11, 10, 12, 14));
            var rogueCombatant = new Combatant(new Unit(session.Confirm(null, "test_rogue")), Side.Party, 0, "Rogue", "ROG");
            var enemy = new Combatant(new Unit(Database.Enemies["skeleton"]), Side.Enemy, 0, "Skeleton", "SK", Database.Enemies["skeleton"]);
            int critClean = rogueCombatant.GetCritChanceAgainst(enemy);
            enemy.AddStatus(StatusId.DefenseDown, 2, 0.25f, rogueCombatant);
            int critDebuffed = rogueCombatant.GetCritChanceAgainst(enemy);
            Assert.Greater(critDebuffed, critClean, "Opportunist increases Crit Chance against debuffed targets");

            // 5. Wizard — Arcane Affinity reduces MP cost of Arcane Spells
            session.CreateNewCharacter();
            session.SelectClass("wizard");
            session.PointBuy.SetAllOrThrow(new AbilityScores(8, 12, 12, 15, 14, 11));
            var wizCombatant = new Combatant(new Unit(session.Confirm(null, "test_wizard")), Side.Party, 0, "Wizard", "WIZ");
            var firebolt = Database.Skills["firebolt"]; // tagged "Arcane"
            Assert.Less(wizCombatant.GetEffectiveMpCost(firebolt), firebolt.mpCost, "Arcane Affinity reduces MP cost of Arcane spells");
        }

        [Test]
        public void RacialFeatConceptsSecondChanceDraconicOverdriveAndOceansEmbraceWorkProperly()
        {
            // 1. Halfling — Second Chance: retry a failed QTE once; does NOT auto-succeed!
            var session = new CharacterCreationSession();
            session.SelectRace("halfling");
            session.SelectClass("rogue");
            session.PointBuy.SetAllOrThrow(new AbilityScores(10, 15, 11, 10, 12, 14));
            var halfling = new Combatant(new Unit(session.Confirm(null, "halfling_rogue")), Side.Party, 0, "Halfling", "HLF");
            Assert.AreEqual(1, halfling.SecondChanceRetriesRemaining);

            var profile = QteConfig.Profiles[Tier.Normal];
            var qte1 = new TimingQte(profile, 0.5f); // target = sweep * 0.5
            qte1.Tick(0.05f);
            qte1.Press(); // way too early -> Miss
            Assert.AreEqual(QteResult.Miss, qte1.Result);

            // Retry with a STILL bad timing -> retry is consumed, and it STILL misses (does NOT auto-succeed!)
            Assert.IsTrue(qte1.TrySecondChanceRetry(halfling, 0.05f));
            Assert.AreEqual(QteResult.Miss, qte1.Result, "Second Chance retry must not automatically succeed");
            Assert.AreEqual(0, halfling.SecondChanceRetriesRemaining);
            Assert.IsFalse(qte1.TrySecondChanceRetry(halfling, qte1.target), "Cannot retry a second time");

            // In a new battle, retrying at the target window succeeds
            var halfling2 = new Combatant(halfling.unit, Side.Party, 0, "Halfling", "HLF");
            var qte2 = new TimingQte(profile, 0.5f);
            qte2.Tick(0.05f);
            qte2.Press();
            Assert.AreEqual(QteResult.Miss, qte2.Result);
            Assert.IsTrue(qte2.TrySecondChanceRetry(halfling2, qte2.target));
            Assert.AreEqual(QteResult.Perfect, qte2.Result);

            // 2. Dragonborn — Draconic Overdrive: temporary power boost followed by Exhausted consequence
            session.CreateNewCharacter();
            session.SelectRace("dragonborn");
            session.SelectClass("fighter");
            session.PointBuy.SetAllOrThrow(new AbilityScores(15, 12, 15, 8, 10, 11));
            var dragonborn = new Combatant(new Unit(session.Confirm(null, "dragon_fighter")), Side.Party, 0, "Dragonborn", "DRG");
            int baseAtk = dragonborn.Atk;
            Assert.IsTrue(dragonborn.ActivateDraconicOverdrive());
            Assert.Greater(dragonborn.Atk, baseAtk, "Draconic Overdrive boosts power");
            dragonborn.statuses.RemoveAll(s => s.id == StatusId.AttackUp);
            dragonborn.TriggerDraconicExhaustion();
            Assert.IsTrue(dragonborn.Has(StatusId.Exhausted), "Applies Exhausted afterward");
            Assert.Less(dragonborn.Atk, baseAtk, "Exhausted reduces power");

            // 3. Merfolk — Ocean's Embrace: grants shield and protection to an ally
            session.CreateNewCharacter();
            session.SelectRace("merfolk");
            session.SelectClass("cleric");
            session.PointBuy.SetAllOrThrow(new AbilityScores(10, 10, 14, 10, 15, 13));
            var merfolk = new Combatant(new Unit(session.Confirm(null, "merfolk_cleric")), Side.Party, 1, "Merfolk", "MER");
            int shieldBefore = dragonborn.unit.shield;
            Assert.IsTrue(merfolk.ActivateOceansEmbrace(dragonborn));
            Assert.Greater(dragonborn.unit.shield, shieldBefore);
            Assert.IsTrue(dragonborn.Has(StatusId.DefenseUp));
        }

        // =====================================================================
        // 8. Equipment Compatibility, Legendary Strictness & Item Restrictions
        // =====================================================================

        [Test]
        public void StrictWeaponCompatibilityAndItemRestrictionsAreEnforcedWithNoLegendaryExceptions()
        {
            var session = new CharacterCreationSession();
            session.SelectRace("elf");
            session.SelectClass("wizard"); // MainHand: Staff, Wand, Dagger | SecondHand: ArcaneTome, Buckler
            session.PointBuy.SetAllOrThrow(new AbilityScores(8, 12, 12, 15, 14, 11));

            // 1. Equipping compatible Main Hand & Second Hand weapons succeeds
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, "oak_staff", out string err1), err1);
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, "apprentice_tome", out string err2), err2);

            // 2. Rejecting incompatible Main Hand weapons (e.g. Greataxe or Longbow on Wizard)
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "battle_greataxe", out string mainErr));
            StringAssert.Contains("Incompatible Main Hand weapon", mainErr);
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, "hunting_bow", out _));

            // 3. Rejecting incompatible Second Hand weapons (e.g. Quiver or Heavy Shield on Wizard)
            Assert.IsFalse(session.TryEquip(EquipmentSlot.SecondHand, "fletched_quiver", out string offErr));
            StringAssert.Contains("Incompatible Second Hand weapon", offErr);
            Assert.IsFalse(session.TryEquip(EquipmentSlot.SecondHand, "iron_shield", out _));

            // 4. Rejecting incompatible Legendary weapons (NO exceptions for Legendary rarity!)
            var legendaryGreatsword = Database.Equipment["legendary_sunblade"];
            var legendaryShield = Database.Equipment["legendary_aegis"];
            Assert.AreEqual(EquipmentRarity.Legendary, legendaryGreatsword.rarity);
            Assert.AreEqual(EquipmentRarity.Legendary, legendaryShield.rarity);
            Assert.IsFalse(session.TryEquip(EquipmentSlot.MainHand, legendaryGreatsword, out string legErr1));
            StringAssert.Contains("Incompatible Main Hand weapon", legErr1);
            Assert.IsFalse(session.TryEquip(EquipmentSlot.SecondHand, legendaryShield, out string legErr2));
            StringAssert.Contains("Incompatible Second Hand weapon", legErr2);

            // Meanwhile, a Fighter CAN equip the Legendary Greatsword and Legendary Shield
            session.SelectClass("fighter");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.MainHand, legendaryGreatsword, out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.SecondHand, legendaryShield, out _));

            // 5. Cross-class Armor & Accessories work across classes when unrestricted
            session.SelectClass("wizard");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.HeadArmor, "iron_helm", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.BodyArmor, "steel_cuirass", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.LegArmor, "chain_greaves", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.FootArmor, "strider_boots", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.Accessory1, "vanguard_ring", out _));
            Assert.IsTrue(session.TryEquip(EquipmentSlot.Accessory2, "soldier_medal", out _));

            // 6. Item-specific restrictions (Class, Race, Level, Ability Score requirements)
            // Class-restricted armor (Cleric only)
            Assert.IsFalse(session.TryEquip(EquipmentSlot.BodyArmor, "cleric_vestment_only", out string classRestrErr));
            StringAssert.Contains("requires class", classRestrErr);

            // Race-restricted armor (Dwarf only)
            Assert.IsFalse(session.TryEquip(EquipmentSlot.HeadArmor, "dwarven_ancestral_helm", out string raceRestrErr));
            StringAssert.Contains("requires race", raceRestrErr);
            session.SelectRace("dwarf");
            Assert.IsTrue(session.TryEquip(EquipmentSlot.HeadArmor, "dwarven_ancestral_helm", out _));

            // Level-restricted armor (Min Level 5)
            Assert.IsFalse(session.TryEquip(EquipmentSlot.LegArmor, "veteran_greaves", out string lvlRestrErr));
            StringAssert.Contains("requires level 5", lvlRestrErr);

            // Ability Score-restricted accessory (Requires STR 15; Dwarf Wizard has STR 8+1=9)
            Assert.IsFalse(session.TryEquip(EquipmentSlot.Accessory1, "titan_girdle", out string statRestrErr));
            StringAssert.Contains("requires STR 15", statRestrErr);
        }

        // =====================================================================
        // 9. Party Assembly (4 Characters) & Expedition Gate
        // =====================================================================

        [Test]
        public void PartyRequiresFourIndependentlyConfiguredCharactersAndRejectsDuplicatesOrIncompleteStart()
        {
            var roster = new CharacterRoster(startWithEmptyParty: true);
            Assert.AreEqual(0, roster.PartyCount);
            Assert.IsFalse(roster.CanBeginExpedition);
            Assert.Throws<InvalidOperationException>(() => roster.BeginExpedition(42));

            var builds = new (string id, string race, string cls, AbilityScores scores)[]
            {
                ("hero_1", "human", "fighter", new AbilityScores(15, 12, 15, 8, 10, 11)),
                ("hero_2", "halfling", "ranger", new AbilityScores(10, 15, 13, 10, 14, 9)),
                ("hero_3", "liichtian", "wizard", new AbilityScores(8, 12, 12, 15, 14, 10)),
                ("hero_4", "merfolk", "cleric", new AbilityScores(10, 10, 14, 10, 15, 12)),
            };

            var session = new CharacterCreationSession();
            for (int i = 0; i < builds.Length; i++)
            {
                var b = builds[i];
                session.CreateNewCharacter();
                session.SelectRace(b.race);
                session.SelectClass(b.cls);
                session.PointBuy.SetAllOrThrow(b.scores);

                var hero = session.Confirm(null, b.id);
                Assert.IsTrue(roster.TryAddToParty(hero, out string addErr), addErr);
                Assert.AreEqual(i + 1, roster.PartyCount);

                // Duplicate character instance or ID is strictly rejected
                Assert.IsFalse(roster.TryAddToParty(hero, out string dupErr));
                StringAssert.Contains("already in the party", dupErr);

                if (i < 3)
                {
                    Assert.IsFalse(roster.IsPartyComplete, $"Party with {i + 1}/4 heroes must be incomplete");
                    Assert.IsFalse(roster.CanBeginExpedition);
                    Assert.Throws<InvalidOperationException>(() => roster.BeginExpedition(42));
                }
            }

            // Party now has 4 distinct characters
            Assert.AreEqual(4, roster.PartyCount);
            Assert.IsTrue(roster.IsPartyComplete);
            Assert.IsTrue(roster.CanBeginExpedition);

            // Cannot add a 5th character
            session.CreateNewCharacter();
            session.SelectRace("dwarf");
            session.SelectClass("barbarian");
            session.PointBuy.SetAllOrThrow(new AbilityScores(15, 12, 15, 8, 10, 11));
            var fifth = session.Confirm(null, "hero_5");
            Assert.IsFalse(roster.TryAddToParty(fifth, out string fullErr));
            StringAssert.Contains("Party is already full", fullErr);

            // Start expedition and verify all 4 independent characters are initialized in RunState.Party
            var run = roster.BeginExpedition(42);
            Assert.IsNotNull(run);
            Assert.AreEqual(4, run.Party.Count);
            Assert.AreEqual("fighter", run.Party[0].classDef.id);
            Assert.AreEqual("ranger", run.Party[1].classDef.id);
            Assert.AreEqual("wizard", run.Party[2].classDef.id);
            Assert.AreEqual("cleric", run.Party[3].classDef.id);
            Assert.AreEqual(8, run.Party[0].equipped.Count);
            Assert.AreEqual("weapon_proficiency", run.Party[0].starterPassiveId);
            Assert.AreEqual("hunters_instinct", run.Party[1].starterPassiveId);
            Assert.AreEqual("arcane_affinity", run.Party[2].starterPassiveId);
            Assert.AreEqual("divine_grace", run.Party[3].starterPassiveId);
        }

        // =====================================================================
        // 10. Accuracy, QTE Independence & Ranger Hunter's Instinct
        // =====================================================================

        [Test]
        public void AccuracyAndQteAreCalculatedIndependentlyAndRangerHuntersInstinctIsRangedOnly()
        {
            var session = new CharacterCreationSession();
            session.SelectRace("elf");
            session.SelectClass("ranger"); // Starter Passive: Hunter's Instinct (+10 Ranged Accuracy)
            session.PointBuy.SetAllOrThrow(new AbilityScores(10, 15, 12, 10, 14, 10));
            var rangerUnit = new Unit(session.Confirm(null, "test_ranger"));
            var ranger = new Combatant(rangerUnit, Side.Party, 0, "Ranger", "RNG");
            var target = new Combatant(new Unit(Database.Enemies["skeleton"]), Side.Enemy, 0, "Skeleton", "SK", Database.Enemies["skeleton"]);

            // 1. Hunter's Instinct applies ONLY to Ranged Basic Attacks and Ranged Active Skills
            // Ranger's starting MainHand is hunting_bow (AttackRangeType.Ranged)
            Assert.AreEqual(AttackCategory.Ranged, AccuracySystem.DetermineCategory(ranger, null));
            float rangedBasicAcc = AccuracySystem.CalculateAttackerAccuracy(ranger, AttackCategory.Ranged);
            float meleeAcc = AccuracySystem.CalculateAttackerAccuracy(ranger, AttackCategory.Melee);
            float magicAcc = AccuracySystem.CalculateAttackerAccuracy(ranger, AttackCategory.Magic);

            int expectedHuntersBonus = Database.Passives["hunters_instinct"].rangedAccuracyBonus;
            Assert.Greater(expectedHuntersBonus, 0);
            Assert.AreEqual(meleeAcc + expectedHuntersBonus, rangedBasicAcc, 0.001f, "Hunter's Instinct applies to Ranged attacks");
            Assert.AreEqual(meleeAcc, magicAcc, 0.001f, "Hunter's Instinct does NOT apply to Melee or Magic attacks");

            // Check with explicit skills against an evasive target: Ranged Active Skill vs Melee Skill vs Magic Skill
            target.AddStatus(StatusId.Evasion, 2, 4.0f, target);
            var rangedSkill = Database.Skills["aimed_shot"];  // Physical, isRanged = true
            var meleeSkill = Database.Skills["shield_bash"];  // Physical, isRanged = false
            var magicSkill = Database.Skills["firebolt"];     // Magic
            Assert.AreEqual(AttackCategory.Ranged, AccuracySystem.DetermineCategory(ranger, rangedSkill));
            Assert.AreEqual(AttackCategory.Melee, AccuracySystem.DetermineCategory(ranger, meleeSkill));
            Assert.AreEqual(AttackCategory.Magic, AccuracySystem.DetermineCategory(ranger, magicSkill));

            float hitRangedSkill = AccuracySystem.CalculateHitChance(ranger, target, AttackCategory.Ranged, null, rangedSkill);
            float hitMeleeSkill = AccuracySystem.CalculateHitChance(ranger, target, AttackCategory.Melee, null, meleeSkill);
            float hitMagicSkill = AccuracySystem.CalculateHitChance(ranger, target, AttackCategory.Magic, null, magicSkill);
            Assert.Greater(hitRangedSkill, hitMeleeSkill, "Ranged skill gets Hunter's Instinct accuracy bonus");
            Assert.AreEqual(hitMeleeSkill, hitMagicSkill, 0.001f, "Melee and Magic skills do not get Hunter's Instinct bonus");

            // 2. Accuracy and QTE Performance are calculated independently; Perfect QTE NEVER guarantees a hit
            var evalPerfectHit = AccuracySystem.EvaluateAttack(ranger, target, rangedSkill, QteResult.Perfect, rollPercent: 50f);
            var evalPerfectMiss = AccuracySystem.EvaluateAttack(ranger, target, rangedSkill, QteResult.Perfect, rollPercent: 99f);
            var evalMissHit = AccuracySystem.EvaluateAttack(ranger, target, rangedSkill, QteResult.Miss, rollPercent: 50f);

            // Hit chance is capped at maxHitChance (95%), so a roll of 99% misses even on a Perfect QTE!
            Assert.LessOrEqual(evalPerfectHit.hitChance, AccuracyConfig.Default.maxHitChance);
            Assert.IsTrue(evalPerfectHit.isHit);
            Assert.IsFalse(evalPerfectMiss.isHit, "A successful/Perfect QTE never guarantees a 100% hit");
            Assert.IsTrue(evalMissHit.isHit, "An attack with a failed QTE can still hit based on Accuracy");
            Assert.Greater(evalPerfectHit.qteDamageMultiplier, evalMissHit.qteDamageMultiplier, "QTE independently controls action performance");
        }

        // =====================================================================
        // 11. New Game Character Creation System (Manual, Template, Random,
        //     Randomize All, Reroll Selected, Save/Continue & Opening Flow)
        // =====================================================================

        [Test]
        public void ManualTemplateAndRandomCreationProduceValidCharactersAndEnforceAllRules()
        {
            var session = new CharacterCreationSession();

            // 1. Manual Creation
            session.CreateNewCharacter();
            session.SetCreationMethod(CreationMethod.Manual);
            Assert.AreEqual(CreationMethod.Manual, session.Method);
            session.SelectRace("dragonborn");
            session.SelectClass("fighter");
            session.PointBuy.SetAllOrThrow(new AbilityScores(15, 12, 15, 8, 10, 11));
            Assert.IsTrue(session.CanConfirm, session.Validate());
            session.CharacterName = "Vanguard";
            var manualDef = session.Confirm(null, "manual_fighter");
            Assert.AreEqual(CreationMethod.Manual, manualDef.creationMethod);
            Assert.IsNull(CharacterCreationSession.ValidateCharacterDef(manualDef));

            // 2. Template Creation — all 9 class templates must be valid and produce valid CharacterDefs
            Assert.AreEqual(9, Database.TemplateIds.Length);
            foreach (var templateId in Database.TemplateIds)
            {
                var template = Database.Templates[templateId];
                Assert.IsNull(CharacterCreationSession.ValidateTemplate(template), $"Template {templateId} invalid");
                Assert.IsTrue(session.TryApplyTemplate(templateId, out string applyErr), applyErr);
                Assert.AreEqual(CreationMethod.Template, session.Method);
                Assert.AreEqual(templateId, session.ActiveTemplateId);
                Assert.AreEqual(0, session.PointBuy.RemainingPoints, $"Template {templateId} must spend all 27 points");
                Assert.IsTrue(session.CanConfirm, session.Validate());

                var tplChar = session.Confirm(null, $"tpl_{templateId}");
                Assert.AreEqual(CreationMethod.Template, tplChar.creationMethod);
                Assert.AreEqual(templateId, tplChar.templateId);
                Assert.IsNull(CharacterCreationSession.ValidateCharacterDef(tplChar));
            }

            // Players can edit a character after applying a template before confirming
            Assert.IsTrue(session.TryApplyTemplate("frontline_fighter", out _));
            session.SelectRace("dwarf");
            Assert.AreEqual("dwarf", session.Race.id);
            Assert.IsTrue(session.CanConfirm);

            // Invalid templates are rejected with clear feedback
            var invalidPointBuyTpl = new CharacterTemplateDef
            {
                id = "bad_points", name = "Bad Points", raceId = "human", classId = "fighter",
                baseScores = new AbilityScores(15, 15, 15, 15, 15, 15) // exceeds 27 points
            };
            string badPtsReason = CharacterCreationSession.ValidateTemplate(invalidPointBuyTpl);
            Assert.IsNotNull(badPtsReason);
            StringAssert.Contains("Point Buy", badPtsReason);

            var invalidWeaponTpl = new CharacterTemplateDef
            {
                id = "bad_weapon", name = "Bad Weapon", raceId = "elf", classId = "wizard",
                baseScores = new AbilityScores(8, 12, 12, 15, 14, 11),
                startingEquipment = new[]
                {
                    new EquipmentSlotAssignment(EquipmentSlot.MainHand, "battle_greataxe") // incompatible with Wizard
                }
            };
            string badWpnReason = CharacterCreationSession.ValidateTemplate(invalidWeaponTpl);
            Assert.IsNotNull(badWpnReason);
            StringAssert.Contains("Incompatible Main Hand weapon", badWpnReason);

            // 3. Random Creation — generates valid characters across multiple seeds without infinite loops
            for (int seed = 1; seed <= 25; seed++)
            {
                var rng = new System.Random(seed * 7919);
                Assert.IsTrue(session.TryRandomize(rng, out string randErr), $"Seed {seed} failed: {randErr}");
                Assert.AreEqual(CreationMethod.Random, session.Method);
                Assert.IsTrue(session.Race.isPlayable);
                Assert.IsTrue(session.Class.isPlayable);
                Assert.AreEqual(0, session.PointBuy.RemainingPoints);
                Assert.IsTrue(session.PointBuy.IsValidForConfirmation(out _));
                Assert.AreEqual(8, session.Equipped.Count);
                Assert.IsTrue(session.CanConfirm, session.Validate());

                session.CharacterName = $"Rand_{seed}";
                var randDef = session.Confirm(null, $"rand_{seed}");
                Assert.AreEqual(CreationMethod.Random, randDef.creationMethod);
                Assert.IsNull(CharacterCreationSession.ValidateCharacterDef(randDef));
            }
        }

        [Test]
        public void RandomizeAllGeneratesFourValidPendingCharactersWithoutAutoConfirmingOrOverwritingSave()
        {
            // Seed an existing confirmed save first to verify RandomizeAll does NOT overwrite it
            GameSave.ClearSave();
            var savedRoster = new CharacterRoster(startWithEmptyParty: false);
            GameSave.SaveNewGame(savedRoster.AssembledParty, seed: 101);
            Assert.IsTrue(GameSave.HasSavedGame());

            var builder = new NewGamePartyBuilder();
            builder.StartNewGame();
            Assert.AreEqual(NewGameFlowState.PendingCharacterCreation, builder.FlowState);
            Assert.AreEqual(0, builder.PendingParty.Count);
            Assert.IsFalse(builder.IsPartyConfirmed);

            // Execute Randomize All
            var rng = new System.Random(4242);
            Assert.IsTrue(builder.TryRandomizeAll(rng, out string err), err);

            // 1. Generates 4 valid randomized characters in PendingParty
            Assert.AreEqual(4, builder.PendingParty.Count);
            Assert.IsTrue(builder.CanConfirmParty);
            Assert.AreEqual(NewGameFlowState.PendingPartyReview, builder.FlowState);

            for (int i = 0; i < CharacterRoster.PartySize; i++)
            {
                var hero = builder.PendingParty[i];
                Assert.IsNotNull(hero);
                Assert.AreEqual(CreationMethod.Random, hero.creationMethod);
                Assert.IsNull(CharacterCreationSession.ValidateCharacterDef(hero), $"Slot {i} invalid");
            }

            // 2. Does NOT auto-confirm the party and does NOT start the expedition
            Assert.IsFalse(builder.IsPartyConfirmed, "RandomizeAll must NOT auto-confirm the party");
            Assert.IsFalse(builder.TryBeginOpeningSequence(out string notConfirmedErr));
            StringAssert.Contains("must be confirmed", notConfirmedErr);

            // 3. Existing save remains untouched while in pending creation/review
            Assert.IsTrue(GameSave.TryLoadContinue(out var loadedRoster, out var loadedRun));
            Assert.AreEqual("knight", loadedRoster.AssembledParty[0].id, "Existing save must NOT be overwritten by RandomizeAll before party confirmation");
            Assert.AreEqual(101, loadedRun.Seed);

            // 4. Player can still reroll or edit individual members after RandomizeAll before confirming
            Assert.IsTrue(builder.ReturnToEditCharacter(2));
            Assert.AreEqual(NewGameFlowState.PendingCharacterCreation, builder.FlowState);
            Assert.AreEqual(2, builder.CurrentMemberIndex);

            GameSave.ClearSave();
        }

        [Test]
        public void RerollSelectedModifiesOnlySelectedMemberAndLeavesOtherThreeUnchanged()
        {
            var builder = new NewGamePartyBuilder();
            builder.StartNewGame();
            Assert.IsTrue(builder.TryRandomizeAll(new System.Random(12345), out string initErr), initErr);
            Assert.AreEqual(4, builder.PendingParty.Count);
            Assert.AreEqual(NewGameFlowState.PendingPartyReview, builder.FlowState);

            // Snapshot the 4 party members before rerolling slot 1 (Character 2)
            var before0 = builder.PendingParty[0];
            var before1 = builder.PendingParty[1];
            var before2 = builder.PendingParty[2];
            var before3 = builder.PendingParty[3];

            // Reroll ONLY slot 1 (Character 2)
            Assert.IsTrue(builder.TryRerollSelected(1, new System.Random(99999), out string rerollErr), rerollErr);

            // 1. Other 3 party members are completely unchanged (same instances & data)
            Assert.AreSame(before0, builder.PendingParty[0], "Slot 0 must not be modified by RerollSelected(1)");
            Assert.AreSame(before2, builder.PendingParty[2], "Slot 2 must not be modified by RerollSelected(1)");
            Assert.AreSame(before3, builder.PendingParty[3], "Slot 3 must not be modified by RerollSelected(1)");

            // 2. Selected member (slot 1) has a newly generated valid character
            var after1 = builder.PendingParty[1];
            Assert.IsNotNull(after1);
            Assert.AreNotSame(before1, after1, "Slot 1 must be replaced with a new randomized CharacterDef");
            Assert.AreEqual(CreationMethod.Random, after1.creationMethod);
            Assert.IsNull(CharacterCreationSession.ValidateCharacterDef(after1));

            // 3. Party is not reset (still 4 members) and is NOT auto-confirmed
            Assert.AreEqual(4, builder.PendingParty.Count);
            Assert.IsFalse(builder.IsPartyConfirmed, "RerollSelected must NOT auto-confirm the party");
            Assert.AreEqual(NewGameFlowState.PendingPartyReview, builder.FlowState);
        }

        [Test]
        public void MainMenuNewGameContinueSaveLoadOpeningSequenceAndSubsequentExpeditionFlowWorkProperly()
        {
            GameSave.ClearSave();
            Assert.IsFalse(GameSave.HasSavedGame());

            var roster = new CharacterRoster(startWithEmptyParty: true);
            var builder = new NewGamePartyBuilder();

            // 1. New Game starts in PendingCharacterCreation; cannot confirm or begin with < 4 heroes
            builder.StartNewGame();
            Assert.AreEqual(NewGameFlowState.PendingCharacterCreation, builder.FlowState);
            Assert.IsFalse(builder.TryConfirmParty(roster, out string incErr));
            StringAssert.Contains("incomplete", incErr.ToLowerInvariant());

            // 2. Create 4 heroes (mix of Manual, Template, and Random)
            // Hero 1: Manual
            builder.CurrentSession.SelectRace("human");
            builder.CurrentSession.SelectClass("fighter");
            builder.CurrentSession.PointBuy.SetAllOrThrow(new AbilityScores(15, 12, 15, 8, 10, 11));
            Assert.IsTrue(builder.TryConfirmCurrentCharacter(out var h1, out string err1), err1);
            h1.name = "Aldric";
            Assert.AreEqual(1, builder.PendingParty.Count);

            // Hero 2: Template
            Assert.IsTrue(builder.CurrentSession.TryApplyTemplate("sharpshooter_ranger", out string err2), err2);
            Assert.IsTrue(builder.TryConfirmCurrentCharacter(out var h2, out err2), err2);
            h2.name = "Sylva";
            Assert.AreEqual(2, builder.PendingParty.Count);

            // Hero 3: Template
            Assert.IsTrue(builder.CurrentSession.TryApplyTemplate("elemental_wizard", out string err3), err3);
            Assert.IsTrue(builder.TryConfirmCurrentCharacter(out var h3, out err3), err3);
            h3.name = "Lyra";
            Assert.AreEqual(3, builder.PendingParty.Count);

            // Hero 4: Random
            Assert.IsTrue(builder.CurrentSession.TryRandomize(new System.Random(77), out string err4), err4);
            Assert.IsTrue(builder.TryConfirmCurrentCharacter(out var h4, out err4), err4);
            h4.name = "Kael";
            Assert.AreEqual(4, builder.PendingParty.Count);

            // 3. Now in PendingPartyReview; review summary lists all 4 members, and save does NOT exist yet
            Assert.AreEqual(NewGameFlowState.PendingPartyReview, builder.FlowState);
            string summary = builder.GetPartyReviewSummary();
            StringAssert.Contains("Aldric", summary);
            StringAssert.Contains("Sylva", summary);
            StringAssert.Contains("Lyra", summary);
            StringAssert.Contains("Kael", summary);
            Assert.IsFalse(GameSave.HasSavedGame(), "Save must not be committed before party confirmation");

            // 4. Confirm Party & Begin Opening Sequence -> commits party to Roster, transitions to OpeningSequence, and writes GameSave
            Assert.IsTrue(builder.TryConfirmParty(roster, out string confirmErr, initialSeed: 555), confirmErr);
            Assert.IsTrue(builder.IsPartyConfirmed);
            Assert.AreEqual(NewGameFlowState.OpeningSequence, builder.FlowState);
            Assert.AreEqual(4, roster.PartyCount);
            Assert.IsTrue(GameSave.HasSavedGame());

            // Initialize first expedition and persist save
            var firstRun = builder.BeginFirstExpedition(roster, seed: 555);
            Assert.AreEqual(NewGameFlowState.FirstExpedition, builder.FlowState);
            Assert.AreEqual(4, firstRun.Party.Count);
            Assert.AreEqual("Aldric", firstRun.Party[0].name);
            Assert.AreEqual("Sylva", firstRun.Party[1].name);
            Assert.AreEqual("Lyra", firstRun.Party[2].name);
            Assert.AreEqual("Kael", firstRun.Party[3].name);

            // Progress in the run (e.g. gain gold and lose some HP) and save
            firstRun.Gold = 250;
            firstRun.Party[0].hp -= 12;
            int expectedHp0 = firstRun.Party[0].hp;
            GameSave.SaveRunState(firstRun, roster.AssembledParty);
            Assert.IsTrue(GameSave.HasSavedGame());

            // 5. Continue loads the saved party (races, classes, ability scores, starter passives, skills, 8 equipment slots) and run progression
            Assert.IsTrue(GameSave.TryLoadContinue(out var restoredRoster, out var restoredRun));
            Assert.AreEqual(4, restoredRoster.AssembledParty.Count);
            Assert.AreEqual("Aldric", restoredRoster.AssembledParty[0].name);
            Assert.AreEqual("human", restoredRoster.AssembledParty[0].raceId);
            Assert.AreEqual("fighter", restoredRoster.AssembledParty[0].classId);
            Assert.AreEqual("weapon_proficiency", restoredRoster.AssembledParty[0].starterPassiveId);
            Assert.AreEqual(8, restoredRoster.AssembledParty[0].startingEquipment.Length);
            Assert.AreEqual(250, restoredRun.Gold);
            Assert.AreEqual(expectedHp0, restoredRun.Party[0].hp);
            Assert.AreEqual(4, restoredRun.Party.Count);

            // 6. Starting a subsequent expedition reuses the confirmed party in Roster without repeating Character Creation
            var secondExpedition = restoredRoster.BeginExpedition(seed: 888);
            Assert.AreEqual(4, secondExpedition.Party.Count);
            Assert.AreEqual("Aldric", secondExpedition.Party[0].name);
            Assert.AreEqual("Sylva", secondExpedition.Party[1].name);
            Assert.AreEqual("Lyra", secondExpedition.Party[2].name);
            Assert.AreEqual("Kael", secondExpedition.Party[3].name);

            GameSave.ClearSave();
        }
    }
}
