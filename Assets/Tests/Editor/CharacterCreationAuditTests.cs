using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    public class CharacterCreationAuditTests
    {
        // Standard 27-point-buy allocation (9 + 7 + 5 + 4 + 2 + 0 = 27)
        static readonly AbilityScores ValidPointBuy = new AbilityScores(15, 14, 13, 12, 10, 8);

        // =====================================================================
        // F1 — RACIAL ABILITY BONUS CHOICES & DRAGONBORN LINEAGE
        // =====================================================================

        [Test]
        public void F1_EveryRaceBonusMatchesGddForEveryOption()
        {
            var session = new CharacterCreationSession();

            // Human: any 2 distinct abilities at +1
            session.SelectRace("human");
            Assert.IsTrue(session.SelectRacialBonus(Ability.INT, Ability.WIS));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 1, 1, 0), session.RacialModifiers);
            Assert.IsTrue(session.SelectRacialBonus(Ability.DEX, Ability.CHA));
            Assert.AreEqual(new AbilityScores(0, 1, 0, 0, 0, 1), session.RacialModifiers);

            // Elf: DEX +2 or INT +2
            session.SelectRace("elf");
            Assert.IsTrue(session.SelectRacialBonus(Ability.DEX));
            Assert.AreEqual(new AbilityScores(0, 2, 0, 0, 0, 0), session.RacialModifiers);
            Assert.IsTrue(session.SelectRacialBonus(Ability.INT));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 2, 0, 0), session.RacialModifiers);
            Assert.IsFalse(session.SelectRacialBonus(Ability.STR), "Elf cannot pick STR.");

            // Liichtian: WIS +2 or CHA +2
            session.SelectRace("liichtian");
            Assert.IsTrue(session.SelectRacialBonus(Ability.WIS));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 0, 2, 0), session.RacialModifiers);
            Assert.IsTrue(session.SelectRacialBonus(Ability.CHA));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 0, 0, 2), session.RacialModifiers);
            Assert.IsFalse(session.SelectRacialBonus(Ability.DEX), "Liichtian cannot pick DEX.");

            // Merfolk: WIS +2 or DEX +2
            session.SelectRace("merfolk");
            Assert.IsTrue(session.SelectRacialBonus(Ability.WIS));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 0, 2, 0), session.RacialModifiers);
            Assert.IsTrue(session.SelectRacialBonus(Ability.DEX));
            Assert.AreEqual(new AbilityScores(0, 2, 0, 0, 0, 0), session.RacialModifiers);
            Assert.IsFalse(session.SelectRacialBonus(Ability.VIT), "Merfolk cannot pick VIT.");

            // Dragonborn: Crimson (+2 STR, Fire), Stone (+2 STR, Physical), Azure (+2 CHA, Lightning), Frost (+2 CHA, Ice)
            session.SelectRace("dragonborn");
            Assert.IsTrue(session.SelectLineage("crimson"));
            Assert.AreEqual(new AbilityScores(2, 0, 0, 0, 0, 0), session.RacialModifiers);
            Assert.IsTrue(session.SelectLineage("stone"));
            Assert.AreEqual(new AbilityScores(2, 0, 0, 0, 0, 0), session.RacialModifiers);
            Assert.IsTrue(session.SelectLineage("azure"));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 0, 0, 2), session.RacialModifiers);
            Assert.IsTrue(session.SelectLineage("frost"));
            Assert.AreEqual(new AbilityScores(0, 0, 0, 0, 0, 2), session.RacialModifiers);
            Assert.IsFalse(session.SelectLineage("obsidian"), "Unknown lineage must be rejected.");

            // Dwarf: fixed VIT +2
            session.SelectRace("dwarf");
            Assert.AreEqual(new AbilityScores(0, 0, 2, 0, 0, 0), session.RacialModifiers);

            // Halfling: fixed DEX +2
            session.SelectRace("halfling");
            Assert.AreEqual(new AbilityScores(0, 2, 0, 0, 0, 0), session.RacialModifiers);
        }

        [Test]
        public void F1_HumanCannotPickSameAbilityTwice()
        {
            var session = new CharacterCreationSession();
            session.SelectRace("human");

            Assert.IsFalse(session.SelectRacialBonus(Ability.STR, Ability.STR));
            Assert.IsNotEmpty(session.LastFeedbackMessage);

            // Toggle removes an already-selected ability instead of duplicating it
            Assert.IsTrue(session.SelectRacialBonus(Ability.STR, Ability.VIT));
            session.ToggleRacialBonus(Ability.STR);
            Assert.IsFalse(session.IsRacialChoiceComplete, "With only 1 of 2 abilities picked, Human choice must be incomplete.");
            Assert.IsNotNull(session.Validate(), "Validate must fail when Human has only 1 bonus picked.");
        }

        [Test]
        public void F1_DragonbornLineageSetsBonusAndBreathElement_AndTwoDragonbornsStayIndependent()
        {
            string sharedLineageBefore = Database.Races["dragonborn"].lineageId;

            var s1 = new CharacterCreationSession();
            s1.SelectRace("dragonborn");
            Assert.IsTrue(s1.SelectLineage("azure"));
            s1.SelectClass("wizard");
            s1.PointBuy.SetAllOrThrow(ValidPointBuy);
            var defAzure = s1.Confirm(null, "db_azure");

            var s2 = new CharacterCreationSession();
            s2.SelectRace("dragonborn");
            Assert.IsTrue(s2.SelectLineage("crimson"));
            s2.SelectClass("fighter");
            s2.PointBuy.SetAllOrThrow(ValidPointBuy);
            var defCrimson = s2.Confirm(null, "db_crimson");

            // Shared RaceDef must NEVER be mutated
            Assert.AreEqual(sharedLineageBefore, Database.Races["dragonborn"].lineageId,
                "Shared RaceDef.lineageId must never be mutated.");

            var uAzure = new Unit(defAzure);
            var uCrimson = new Unit(defCrimson);
            Assert.AreEqual("azure", uAzure.lineageId);
            Assert.AreEqual("crimson", uCrimson.lineageId);
            Assert.AreEqual(2, uAzure.abilities.racialModifiers[Ability.CHA]);
            Assert.AreEqual(0, uAzure.abilities.racialModifiers[Ability.STR]);
            Assert.AreEqual(2, uCrimson.abilities.racialModifiers[Ability.STR]);
            Assert.AreEqual(0, uCrimson.abilities.racialModifiers[Ability.CHA]);

            var cAzure = new Combatant(uAzure, Side.Party, 0, uAzure.name, "AZ");
            var cCrimson = new Combatant(uCrimson, Side.Party, 1, uCrimson.name, "CR");
            Assert.AreEqual(Element.Lightning, cAzure.GetDragonbornLineageElement());
            Assert.AreEqual(Element.Fire, cCrimson.GetDragonbornLineageElement());
        }

        [Test]
        public void F1_SaveLoadRoundTrip_AndOldSaveMigration_AndRespec()
        {
            // 1. Save/load round-trip preserves racialBonusChoice & lineageId
            var s = new CharacterCreationSession();
            s.SelectRace("elf");
            Assert.IsTrue(s.SelectRacialBonus(Ability.INT));
            s.SelectClass("wizard");
            s.PointBuy.SetAllOrThrow(ValidPointBuy);
            var elfDef = s.Confirm(null, "elf_wiz");

            var s2 = new CharacterCreationSession();
            s2.SelectRace("dragonborn");
            Assert.IsTrue(s2.SelectLineage("stone"));
            s2.SelectClass("fighter");
            s2.PointBuy.SetAllOrThrow(ValidPointBuy);
            var dbDef = s2.Confirm(null, "db_stone");

            var savedElf = SavedCharacterData.FromCharacterDef(elfDef, new Unit(elfDef));
            var savedDb = SavedCharacterData.FromCharacterDef(dbDef, new Unit(dbDef));

            var restoredElf = new Unit(JsonUtility.FromJson<SavedCharacterData>(JsonUtility.ToJson(savedElf)).ToCharacterDef());
            var restoredDb = new Unit(JsonUtility.FromJson<SavedCharacterData>(JsonUtility.ToJson(savedDb)).ToCharacterDef());

            CollectionAssert.AreEqual(new[] { Ability.INT }, restoredElf.racialBonusChoice);
            Assert.AreEqual(2, restoredElf.abilities.racialModifiers[Ability.INT]);
            Assert.AreEqual("stone", restoredDb.lineageId);
            Assert.AreEqual(2, restoredDb.abilities.racialModifiers[Ability.STR]);

            // 2. Old save without racialBonusChoice or lineageId loads with today's defaults
            var legacyHuman = new SavedCharacterData
            {
                id = "old_human",
                name = "Old Human",
                raceId = "human",
                classId = "fighter",
                baseScores = AbilityScores.DefaultBase,
                racialBonusChoice = null,
                lineageId = null,
            };
            var legacyElf = new SavedCharacterData
            {
                id = "old_elf",
                name = "Old Elf",
                raceId = "elf",
                classId = "ranger",
                baseScores = AbilityScores.DefaultBase,
                racialBonusChoice = null,
                lineageId = null,
            };
            var legacyDb = new SavedCharacterData
            {
                id = "old_db",
                name = "Old Dragonborn",
                raceId = "dragonborn",
                classId = "fighter",
                baseScores = AbilityScores.DefaultBase,
                racialBonusChoice = null,
                lineageId = null,
            };

            var uOldHuman = new Unit(legacyHuman.ToCharacterDef());
            Assert.AreEqual(new AbilityScores(1, 0, 1, 0, 0, 0), uOldHuman.abilities.racialModifiers);
            var uOldElf = new Unit(legacyElf.ToCharacterDef());
            Assert.AreEqual(new AbilityScores(0, 2, 0, 0, 0, 0), uOldElf.abilities.racialModifiers);
            var uOldDb = new Unit(legacyDb.ToCharacterDef());
            Assert.AreEqual("crimson", uOldDb.lineageId);
            Assert.AreEqual(new AbilityScores(2, 0, 0, 0, 0, 0), uOldDb.abilities.racialModifiers);

            // 3. Unit.Respec updates racial bonus choice and Dragonborn lineage
            restoredElf.Respec(restoredElf.abilities.baseScores, null, new[] { Ability.DEX }, null);
            Assert.AreEqual(2, restoredElf.abilities.racialModifiers[Ability.DEX]);
            Assert.AreEqual(0, restoredElf.abilities.racialModifiers[Ability.INT]);

            restoredDb.Respec(restoredDb.abilities.baseScores, null, null, "azure");
            Assert.AreEqual("azure", restoredDb.lineageId);
            Assert.AreEqual(2, restoredDb.abilities.racialModifiers[Ability.CHA]);
            Assert.AreEqual(0, restoredDb.abilities.racialModifiers[Ability.STR]);
        }

        [Test]
        public void F1_TemplatesValidationAndSmartRandomize()
        {
            var session = new CharacterCreationSession();

            // Invalid Human duplicate bonus in template
            var badHumanTpl = new CharacterTemplateDef
            {
                id = "bad_human",
                name = "Bad Human",
                raceId = "human",
                classId = "fighter",
                racialBonusChoice = new[] { Ability.STR, Ability.STR },
                baseScores = ValidPointBuy,
            };
            Assert.IsNotNull(CharacterCreationSession.ValidateTemplate(badHumanTpl), "Duplicate Human bonus must fail template validation.");

            // Invalid Dragonborn lineage in template
            var badDbTpl = new CharacterTemplateDef
            {
                id = "bad_db",
                name = "Bad DB",
                raceId = "dragonborn",
                classId = "fighter",
                lineageId = "invalid_lineage",
                baseScores = ValidPointBuy,
            };
            Assert.IsNotNull(CharacterCreationSession.ValidateTemplate(badDbTpl), "Unknown Dragonborn lineage must fail template validation.");

            // Smart randomize produces valid racial choices across many seeds
            var rng = new System.Random(12345);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(session.TryRandomize(rng, out string err), $"Randomize #{i} failed: {err}");
                Assert.IsTrue(session.IsRacialChoiceComplete, $"Randomize #{i} left incomplete racial choice for {session.Race.id}.");
                Assert.IsNull(session.Validate(), $"Randomize #{i} produced invalid session.");
            }
        }

        // =====================================================================
        // F2 — DUPLICATE HERO NAME SUFFIXES
        // =====================================================================

        [Test]
        public void F2_DuplicateClassNamesGetRomanSuffixes_RecomputedOnRemove_AndUsedInCombatLog()
        {
            var builder = new NewGamePartyBuilder();

            for (int i = 0; i < 3; i++)
            {
                builder.CurrentSession.SelectClass("fighter");
                builder.CurrentSession.PointBuy.SetAllOrThrow(ValidPointBuy);
                Assert.IsTrue(builder.TryConfirmCurrentCharacter(out _, out string err), err);
            }

            CollectionAssert.AreEqual(
                new[] { "Fighter", "Fighter II", "Fighter III" },
                builder.PendingParty.Select(m => m.name).ToArray());

            // Removing the first renames the remaining two to Fighter, Fighter II
            Assert.IsTrue(builder.RemovePendingMember(0));
            CollectionAssert.AreEqual(
                new[] { "Fighter", "Fighter II" },
                builder.PendingParty.Select(m => m.name).ToArray());

            // Verify combat log uses the suffixed names
            var units = builder.PendingParty.Select(d => new Unit(d)).ToList();
            var enemy = Database.Enemies.Values.First();
            var battle = new Battle(units, new[] { enemy }, new List<string>(), new System.Random(7));

            var f1 = battle.Party[0];
            var f2 = battle.Party[1];
            Assert.AreEqual("Fighter", f1.name);
            Assert.AreEqual("Fighter II", f2.name);

            // Advance one full round so both Fighter and Fighter II act and log their names
            for (int t = 0; t < 6 && battle.Outcome == Outcome.None; t++)
            {
                var actor = battle.NextTurn();
                if (actor == null) break;
                if (actor.side == Side.Party)
                    battle.Perform(BattleAction.Attack(actor, battle.Enemies[0]));
                else
                    battle.Perform(BattleAction.Guard(actor));
            }

            Assert.IsTrue(battle.Log.Any(line => line.Contains("Fighter")), "Combat log must include first hero name.");
            Assert.IsTrue(battle.Log.Any(line => line.Contains("Fighter II")), "Combat log must include suffixed second hero name.");
        }

        // =====================================================================
        // F3 + F4 + F7 — 5-STEP FLOW, STEP MAPPING, AUTO-EQUIPPED GEAR
        // =====================================================================

        [Test]
        public void F3_F4_FiveStepFlowMappingHeaderCountAndDefaultGear()
        {
            // Page -> Step mapping
            Assert.AreEqual(5, Enum.GetValues(typeof(CharacterCreationScreen.Page)).Length);
            Assert.AreEqual(5, CharacterCreationScreen.StepCount);
            Assert.AreEqual(CreationStep.SelectRace, CharacterCreationScreen.StepForPage(CharacterCreationScreen.Page.Race));
            Assert.AreEqual(CreationStep.SelectClass, CharacterCreationScreen.StepForPage(CharacterCreationScreen.Page.Class));
            Assert.AreEqual(CreationStep.AllocateAbilityScores, CharacterCreationScreen.StepForPage(CharacterCreationScreen.Page.Abilities));
            Assert.AreEqual(CreationStep.SelectBackground, CharacterCreationScreen.StepForPage(CharacterCreationScreen.Page.Background));
            Assert.AreEqual(CreationStep.CharacterSheet, CharacterCreationScreen.StepForPage(CharacterCreationScreen.Page.CharacterSheet));

            // No page maps to removed Passive or Gear steps
            foreach (CharacterCreationScreen.Page p in Enum.GetValues(typeof(CharacterCreationScreen.Page)))
            {
#pragma warning disable CS0618
                Assert.AreNotEqual(CreationStep.ReviewStarterPassive, CharacterCreationScreen.StepForPage(p));
                Assert.AreNotEqual(CreationStep.ConfigureEquipment, CharacterCreationScreen.StepForPage(p));
#pragma warning restore CS0618
            }

            // Session.NextStep never enters removed steps
            var session = new CharacterCreationSession();
            var visited = new List<CreationStep> { session.CurrentStep };
            while (session.NextStep())
                visited.Add(session.CurrentStep);
#pragma warning disable CS0618
            CollectionAssert.DoesNotContain(visited, CreationStep.ReviewStarterPassive);
            CollectionAssert.DoesNotContain(visited, CreationStep.ConfigureEquipment);
#pragma warning restore CS0618
            CollectionAssert.Contains(visited, CreationStep.SelectBackground);
            CollectionAssert.Contains(visited, CreationStep.CharacterSheet);

            // Screen header shows STEP n/5 and starting gear matches class default
            var root = new GameObject("StepAuditRoot").transform;
            try
            {
                var screen = new CharacterCreationScreen();
                screen.Open(root);
                StringAssert.Contains("STEP 1/5", screen.StepLabel.Text);

                screen.GoToPage(CharacterCreationScreen.Page.Background);
                Assert.AreEqual(CreationStep.SelectBackground, screen.Session.CurrentStep);
                StringAssert.Contains("STEP 4/5", screen.StepLabel.Text);

                // Auto-equipped gear matches class default
                foreach (var cls in Database.Classes.Values)
                {
                    screen.Session.SelectClass(cls);
                    foreach (var eqAssign in cls.startingEquipment)
                    {
                        Assert.AreEqual(eqAssign.equipmentId, screen.Session.GetEquipped(eqAssign.slot)?.id,
                            $"{cls.id} should auto-equip default {eqAssign.equipmentId} in {eqAssign.slot}.");
                    }
                }
                screen.Close();
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }

        // =====================================================================
        // F7 — CHARACTER SHEET MATCHES BATTLE UNIT
        // =====================================================================

        [Test]
        public void F7_CharacterSheetMatchesBattleStartUnitExactly()
        {
            var session = new CharacterCreationSession();
            session.SelectRace("elf");
            Assert.IsTrue(session.SelectRacialBonus(Ability.INT));
            session.SelectClass("wizard");
            session.PointBuy.SetAllOrThrow(new AbilityScores(8, 12, 14, 15, 13, 10)); // 0+4+7+9+5+2 = 27
            session.SelectBackground(Database.Backgrounds.Keys.First());

            var sheet = CharacterSheetBuilder.Build(session);
            var confirmedDef = session.Confirm(null, "sheet_check");
            var unit = new Unit(confirmedDef);

            Assert.AreEqual(unit.abilities.FinalScores, sheet.finalScores);
            Assert.AreEqual(unit.abilities.FinalScores.Modifiers, sheet.abilityModifiers);
            Assert.AreEqual(unit.Derived.maxHp, sheet.derived.maxHp);
            Assert.AreEqual(unit.Derived.maxMp, sheet.derived.maxMp);
            Assert.AreEqual(unit.Derived.physicalPower, sheet.derived.physicalPower);
            Assert.AreEqual(unit.Derived.rangedPower, sheet.derived.rangedPower);
            Assert.AreEqual(unit.Derived.magicPower, sheet.derived.magicPower);
            Assert.AreEqual(unit.Derived.physicalDefense, sheet.derived.physicalDefense);
            Assert.AreEqual(unit.Derived.magicDefense, sheet.derived.magicDefense);
            Assert.AreEqual(unit.Derived.initiative, sheet.derived.initiative);

            string unitStarterPassive = !string.IsNullOrEmpty(unit.starterPassiveId) && Database.Passives.TryGetValue(unit.starterPassiveId, out var sp)
                ? sp.name : "None";
            Assert.AreEqual(unitStarterPassive, sheet.starterPassiveName);
            Assert.AreEqual(unit.race?.racialFeat?.name ?? "None", sheet.racialFeatName);
            Assert.AreEqual(unit.background?.name ?? "-", sheet.backgroundName);

            foreach (var slot in EquipmentSlots.All)
            {
                var row = sheet.gear.First(g => g.slot == slot);
                string expectedName = unit.equipped.TryGetValue(slot, out var uEq) && uEq != null ? uEq.name : "Empty";
                Assert.AreEqual(expectedName, row.itemName, $"Mismatch in equipment slot {slot}.");
            }

            var unitActiveSkillIds = unit.skillLoadout.activeSlots.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var sheetSkillIds = sheet.activeSkills.Select(s => s.skillId).ToArray();
            CollectionAssert.AreEqual(unitActiveSkillIds, sheetSkillIds);
        }

        // =====================================================================
        // F8 — PRESET DIRECTLY ADDS HERO AND OPENS PARTY REVIEW
        // =====================================================================

        [Test]
        public void F8_PresetAddsHeroAndOpensPartyReview_InvalidPresetBlockedWithReason()
        {
            var root = new GameObject("PresetAuditRoot").transform;
            try
            {
                var screen = new CharacterCreationScreen();
                screen.Open(root);
                Assert.AreEqual(0, screen.Builder.PendingParty.Count);

                string validTemplateId = Database.Templates.Keys.First();
                Assert.IsTrue(screen.ApplyPreset(validTemplateId));

                Assert.AreEqual(1, screen.Builder.PendingParty.Count, "Applying a valid preset must add the hero immediately.");
                Assert.AreEqual(NewGameFlowState.PendingPartyReview, screen.Builder.FlowState, "Applying a valid preset must open party review.");
                Assert.AreEqual(CharacterCreationScreen.Page.CharacterSheet, screen.CurrentPage);

                // Invalid preset does not add a hero and reports reason
                Assert.IsFalse(screen.Builder.TryAddPresetHero("nonexistent_preset_id", out _, out string reason));
                Assert.IsNotEmpty(reason);
                Assert.AreEqual(1, screen.Builder.PendingParty.Count, "Invalid preset must not add a member.");

                screen.Close();
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }

        // =====================================================================
        // F5 — PARTY OVERVIEW SOFT GAP WARNINGS (NO ELEMENTS EVER)
        // =====================================================================

        [Test]
        public void F5_PartyOverviewFlagsNoHealer_BalancedShowsNoGaps_NeverMentionsElements()
        {
            static CharacterDef MakeHero(string classId, string raceId = "human")
            {
                var s = new CharacterCreationSession();
                s.SelectRace(raceId);
                s.SelectClass(classId);
                s.PointBuy.SetAllOrThrow(ValidPointBuy);
                return s.Confirm(null, $"hero_{classId}");
            }

            // Party with no healer flags "No healer"
            var noHealerParty = new List<CharacterDef>
            {
                MakeHero("fighter"),
                MakeHero("wizard"),
                MakeHero("rogue"),
                MakeHero("ranger"),
            };
            var noHealerGaps = PartyOverviewEvaluator.Evaluate(noHealerParty);
            CollectionAssert.Contains(noHealerGaps, "No healer");

            // Balanced party shows "No gaps found"
            var balancedParty = new List<CharacterDef>
            {
                MakeHero("fighter"),
                MakeHero("cleric"),
                MakeHero("wizard"),
                MakeHero("rogue"),
            };
            var balancedGaps = PartyOverviewEvaluator.Evaluate(balancedParty);
            Assert.AreEqual(0, balancedGaps.Count, $"Expected no gaps for balanced party, got: {string.Join(", ", balancedGaps)}");
            Assert.AreEqual("No gaps found", PartyOverviewEvaluator.FormatDisplay(balancedParty));

            // No element text ever appears in output across multiple compositions
            string[] forbiddenElements =
            {
                "Fire", "Ice", "Lightning", "Arcane", "Holy", "Dark", "Earth", "Wind", "Water", "Physical",
            };
            var samples = new[]
            {
                new List<CharacterDef>(),
                new List<CharacterDef> { MakeHero("wizard") },
                noHealerParty,
                balancedParty,
            };
            foreach (var sample in samples)
            {
                string display = PartyOverviewEvaluator.FormatDisplay(sample);
                foreach (var elem in forbiddenElements)
                {
                    Assert.IsFalse(display.IndexOf(elem, StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Party Overview must never mention element '{elem}', got: {display}");
                }
            }
        }

        // =====================================================================
        // F6 — RACE CHANGE KEEPS POINT BUY AND RECORDS FINAL SCORE DELTA
        // =====================================================================

        [Test]
        public void F6_ChangingRacePreservesPointBuyAndTracksScoreDeltas()
        {
            var session = new CharacterCreationSession();
            session.SelectRace("human");
            session.PointBuy.SetAllOrThrow(ValidPointBuy);
            var baseBefore = session.PointBuy.Scores;
            var finalBefore = session.FinalScores;

            session.SelectRace("dwarf");

            Assert.AreEqual(baseBefore, session.PointBuy.Scores, "Point Buy allocation must not be reset on race change.");
            Assert.IsTrue(session.HasRecentRaceDelta, "Changing race must flag a recent race delta.");
            Assert.AreEqual(finalBefore, session.LastFinalScoresBeforeRaceChange);
            Assert.AreNotEqual(finalBefore, session.FinalScores);
        }
    }
}
