using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Obscure.Tests
{
    [TestFixture]
    public class DungeonEventPoolTests
    {
        static CharacterDef MakeHero(string id, string raceId) => new CharacterDef
        {
            id = id,
            name = id,
            role = "Adventurer",
            raceId = raceId,
            classId = "fighter",
            backgroundId = "oathsworn",
            baseScores = AbilityScores.DefaultBase,
            skills = new[] { "shield_bash" },
            stats = new Stats(100, 40, 12, 10, 10, 10)
        };

        static RunState RunWithRaces(params string[] raceIds)
        {
            var party = raceIds.Select((r, idx) => MakeHero($"h{idx}_{r}", r)).ToList();
            var map = new MapDef
            {
                name = "Test",
                act = 1,
                nodes = new[]
                {
                    new NodeDef { id = "n0", type = NodeType.Event, floor = 0, eventId = "D1_EVT_001", next = new[] { "n1" } },
                    new NodeDef { id = "n1", type = NodeType.Boss, floor = 1, encounter = "d1_zoroa" },
                }
            };
            return new RunState(map, party, seed: 42);
        }

        static RunState AtD1Event(string eventId, int gold = 0, params string[] raceIds)
        {
            if (raceIds == null || raceIds.Length == 0)
                raceIds = new[] { "elf", "dwarf", "halfling", "liichtian" };

            var party = raceIds.Select((r, idx) => MakeHero($"h{idx}_{r}", r)).ToList();
            var map = new MapDef
            {
                name = "Ruined Kingdom",
                act = 1,
                nodes = new[]
                {
                    new NodeDef { id = "n0", type = NodeType.Event, floor = 0, eventId = eventId, next = new[] { "n1" } },
                    new NodeDef { id = "n1", type = NodeType.Boss, floor = 1, encounter = "d1_zoroa" },
                }
            };
            var run = new RunState(map, party, seed: 101) { Gold = gold };
            run.Enter(map.Get("n0"));
            return run;
        }

        [Test]
        public void Dungeon1EventPool_HasExact20EventsAndCategoryComposition()
        {
            var d1 = Database.Events.Values.Where(e => e.dungeonAct == 1).ToList();
            Assert.AreEqual(20, d1.Count, "Dungeon 1 must contain exactly 20 events.");

            for (int i = 1; i <= 20; i++)
            {
                string id = $"D1_EVT_{i:D3}";
                Assert.IsTrue(Database.Events.ContainsKey(id), $"Missing {id}");
            }

            Assert.AreEqual(3, d1.Count(e => e.category == EventCategory.Shrine));
            Assert.AreEqual(2, d1.Count(e => e.category == EventCategory.NpcEncounter));
            Assert.AreEqual(3, d1.Count(e => e.category == EventCategory.Treasure));
            Assert.AreEqual(3, d1.Count(e => e.category == EventCategory.TrapAndHazard));
            Assert.AreEqual(3, d1.Count(e => e.category == EventCategory.Supernatural));
            Assert.AreEqual(2, d1.Count(e => e.category == EventCategory.PuzzleAndExploration));
            Assert.AreEqual(2, d1.Count(e => e.category == EventCategory.Survival));
            Assert.AreEqual(2, d1.Count(e => e.category == EventCategory.CursedAndRisk));

            foreach (var ev in d1)
            {
                Assert.GreaterOrEqual(ev.choices.Length, 2, ev.id);
                var run = AtD1Event(ev.id, gold: 0, "elf", "dwarf", "halfling", "liichtian");
                Assert.IsNull(run.EventBlocked(ev.choices.Last()), $"{ev.id}: last choice must always be open");
                foreach (var c in ev.choices)
                {
                    if (!string.IsNullOrEmpty(c.item)) Assert.IsTrue(Database.Items.ContainsKey(c.item), c.item);
                    if (!string.IsNullOrEmpty(c.requiredItem)) Assert.IsTrue(Database.Items.ContainsKey(c.requiredItem), c.requiredItem);
                    if (!string.IsNullOrEmpty(c.materialId)) Assert.IsTrue(Database.Materials.ContainsKey(c.materialId), c.materialId);
                    if (!string.IsNullOrEmpty(c.requiredMaterialId)) Assert.IsTrue(Database.Materials.ContainsKey(c.requiredMaterialId), c.requiredMaterialId);
                    if (!string.IsNullOrEmpty(c.modifierId)) Assert.IsTrue(Database.DungeonModifiers.ContainsKey(c.modifierId), c.modifierId);
                    if (!string.IsNullOrEmpty(c.curseModifierId)) Assert.IsTrue(Database.DungeonModifiers.ContainsKey(c.curseModifierId), c.curseModifierId);
                    if (!string.IsNullOrEmpty(c.riskFailModifierId)) Assert.IsTrue(Database.DungeonModifiers.ContainsKey(c.riskFailModifierId), c.riskFailModifierId);
                }
            }
        }

        [Test]
        public void Dungeon1Events_AreExclusiveToDungeon1InMapGenerator()
        {
            for (int seed = 1; seed <= 10; seed++)
            {
                var d1Map = MapGenerator.Generate(seed, act: 1);
                var d1Events = d1Map.nodes.Where(n => n.type == NodeType.Event).Select(n => n.eventId).ToList();
                Assert.IsNotEmpty(d1Events);
                Assert.IsTrue(d1Events.All(id => Database.Events[id].dungeonAct == 1),
                    $"Seed {seed} Act 1 spawned non-D1 event: {string.Join(", ", d1Events)}");

                var d2Map = MapGenerator.Generate(seed, act: 2);
                var d2Events = d2Map.nodes.Where(n => n.type == NodeType.Event).Select(n => n.eventId).ToList();
                Assert.IsNotEmpty(d2Events);
                Assert.IsTrue(d2Events.All(id => Database.Events[id].dungeonAct != 1),
                    $"Seed {seed} Act 2 illegally reused Dungeon 1 event: {string.Join(", ", d2Events)}");
            }
        }

        [Test]
        public void GoldCostChoice_ChargesListedCost_AndReportsGoldReward()
        {
            // Soulglass Reliquary "Stabilize": costs 35, pays back 15 -> net -20 must be explained in the outcome.
            var run = AtD1Event("D1_EVT_007", gold: 603);
            int idx = Array.FindIndex(run.CurrentEvent.choices, c => c.goldCost == 35);
            Assert.AreEqual(15, run.PreviewEventGold(idx), "card preview matches what ChooseEvent pays");
            string outcome = run.ChooseEvent(idx);
            Assert.AreEqual(603 - 35 + 15, run.Gold);
            StringAssert.Contains("Gained 15 spoils.", outcome);
        }

        [Test]
        public void HumanAdaptability_BorrowsOncePerDungeonAt75Percent_AndImproviseIsFreeWithoutRacialOptions()
        {
            // Party has Human only (no Elf): D1_EVT_006 has Elf option (65 gold, 2 MAT001)
            var run = AtD1Event("D1_EVT_006", gold: 0, "human");
            int elfIdx = Array.FindIndex(run.CurrentEvent.choices, c => c.requiredRace == "elf");
            Assert.GreaterOrEqual(elfIdx, 0);

            var status = run.GetEventChoiceStatus(elfIdx);
            Assert.IsFalse(status.isBlocked);
            Assert.IsTrue(status.usesAdaptabilityBorrow);
            Assert.AreEqual(0.75f, status.effectScale, 0.001f);

            string outcome = run.ChooseEvent(elfIdx);
            Assert.AreEqual(0, run.HumanAdaptabilityUsesRemaining);
            Assert.AreEqual((int)Math.Round(65 * 0.75f), run.Gold);
            Assert.AreEqual(2, run.Inventory.GetMaterialCount("MAT001")); // round(2 * 0.75) = 2
            StringAssert.Contains("Adaptability", outcome);

            // Second event in same dungeon cannot borrow again
            var nextNode = new NodeDef { id = "n_ev2", type = NodeType.Event, floor = 0, eventId = "D1_EVT_009" };
            var map2 = new MapDef
            {
                name = "Test",
                act = 1,
                nodes = new[] { nextNode, new NodeDef { id = "b", type = NodeType.Boss, floor = 1, encounter = "d1_zoroa" } }
            };
            nextNode.next = new[] { "b" };
            var run2 = new RunState(map2, new[] { MakeHero("h", "human") }, 1);
            // Exhaust quota first on D1_EVT_006 style via AdvanceToNextDungeon verification below;
            // verify D1_EVT_018 Improvise is free and does NOT consume Adaptability borrow quota!
            var cacheRun = AtD1Event("D1_EVT_018", gold: 0, "human");
            Assert.IsFalse(cacheRun.CurrentEvent.HasAnyRacialChoice());
            int impIdx = Array.FindIndex(cacheRun.CurrentEvent.choices, c => c.isImprovise);
            Assert.GreaterOrEqual(impIdx, 0);
            var impStatus = cacheRun.GetEventChoiceStatus(impIdx);
            Assert.IsTrue(impStatus.isVisible);
            Assert.IsFalse(impStatus.isBlocked);
            Assert.IsFalse(impStatus.usesAdaptabilityBorrow);
            cacheRun.ChooseEvent(impIdx);
            Assert.AreEqual(1, cacheRun.HumanAdaptabilityUsesRemaining, "Improvise must not consume Adaptability borrow quota.");
            Assert.AreEqual(30, cacheRun.Gold);
            Assert.AreEqual(1, cacheRun.Inventory.GetConsumableCount("CON002"));

            // Non-human party cannot see or use Improvise
            var nonHuman = AtD1Event("D1_EVT_018", gold: 0, "elf");
            Assert.IsFalse(nonHuman.GetEventChoiceStatus(impIdx).isVisible);
            Assert.IsTrue(nonHuman.GetEventChoiceStatus(impIdx).isBlocked);
        }

        [Test]
        public void ElfKeenSenses_AvoidsTrapAndHazard_DoesNotTriggerOnHiddenOnlyEvent()
        {
            var treasury = AtD1Event("D1_EVT_006", gold: 0, "elf");
            int hpBefore = treasury.Party[0].hp;
            int elfIdx = Array.FindIndex(treasury.CurrentEvent.choices, c => c.requiredRace == "elf");
            treasury.ChooseEvent(elfIdx);
            Assert.AreEqual(hpBefore, treasury.Party[0].hp, "Elf Detect avoids trap damage completely.");
            Assert.AreEqual(65, treasury.Gold);

            var ashfall = AtD1Event("D1_EVT_011", gold: 0, "elf");
            int ashHpBefore = ashfall.Party[0].hp;
            int ashElfIdx = Array.FindIndex(ashfall.CurrentEvent.choices, c => c.requiredRace == "elf");
            ashfall.ChooseEvent(ashElfIdx);
            Assert.AreEqual(ashHpBefore, ashfall.Party[0].hp);
            Assert.AreEqual(1, ashfall.Inventory.GetMaterialCount("MAT002"));

            // D1_EVT_013 has tag "hidden" without trap/hazard -> no Elf option
            var mirror = AtD1Event("D1_EVT_013", gold: 0, "elf");
            Assert.IsFalse(mirror.CurrentEvent.choices.Any(c => c.requiredRace == "elf"));
        }

        [Test]
        public void DwarfStonecraft_SucceedsMechanismsAndDestructible_AndMineralTagGrantsBonus()
        {
            var armory = AtD1Event("D1_EVT_008", gold: 0, "dwarf");
            int dwarfIdx = Array.FindIndex(armory.CurrentEvent.choices, c => c.requiredRace == "dwarf");
            Assert.IsFalse(armory.GetEventChoiceStatus(dwarfIdx).isBlocked);
            armory.ChooseEvent(dwarfIdx);
            Assert.AreEqual(1, armory.Inventory.equipment.Count, "Dwarf opens armory without Smoke Bomb.");
            Assert.AreEqual(2, armory.Inventory.GetMaterialCount("MAT001"));

            // Duplicate Dwarf does NOT stack mineral +1 bonus
            var mineralEvent = new EventDef
            {
                id = "test_mineral",
                title = "Mineral Vein",
                text = "Ore glints.",
                tags = new[] { EventTags.Mineral },
                choices = new[]
                {
                    new EventChoice { label = "Mine ore", materialId = "MAT001", materialAmount = 1, outcome = "Mined." },
                    new EventChoice { label = "Leave", outcome = "Left." }
                }
            };
            Database.Events["test_mineral"] = mineralEvent;
            try
            {
                var twoDwarves = AtD1Event("test_mineral", gold: 0, "dwarf", "dwarf");
                twoDwarves.ChooseEvent(0);
                Assert.AreEqual(2, twoDwarves.Inventory.GetMaterialCount("MAT001"), "Duplicate Dwarf must not stack +1 mineral bonus.");
            }
            finally
            {
                Database.Events.Remove("test_mineral");
            }
        }

        [Test]
        public void HalflingSmallAndNimble_ReducesHazardDamage50PercentWithoutStacking_AndSneaksNormalBattle()
        {
            // Compare 0 Halflings vs 1 Halfling vs 2 Halflings on D1_EVT_009 (18% hazard damage)
            var noHalf = AtD1Event("D1_EVT_009", gold: 0, "elf");
            int maxHpNoHalf = noHalf.Party[0].stats.maxHp;
            noHalf.ChooseEvent(0);
            Assert.AreEqual(maxHpNoHalf - (int)Math.Round(maxHpNoHalf * 0.18f), noHalf.Party[0].hp);

            var oneHalf = AtD1Event("D1_EVT_009", gold: 0, "halfling");
            int maxHpOneHalf = oneHalf.Party[0].stats.maxHp;
            oneHalf.ChooseEvent(0);
            Assert.AreEqual(maxHpOneHalf - (int)Math.Round(maxHpOneHalf * 0.09f), oneHalf.Party[0].hp);

            var twoHalf = AtD1Event("D1_EVT_009", gold: 0, "halfling", "halfling");
            twoHalf.ChooseEvent(0);
            Assert.AreEqual(oneHalf.Party[0].hp, twoHalf.Party[0].hp, "Two Halflings must NOT stack hazard reduction.");

            // Walkway has separate Halfling narrow and Dwarf destructible options
            var walkway = AtD1Event("D1_EVT_010", gold: 0, "halfling", "dwarf");
            Assert.IsTrue(walkway.CurrentEvent.choices.Any(c => c.requiredRace == "halfling"));
            Assert.IsTrue(walkway.CurrentEvent.choices.Any(c => c.requiredRace == "dwarf"));

            // Sneak once per dungeon on normal Battle, not Elite/Boss
            var sneakMap = new MapDef
            {
                name = "Sneak Map",
                act = 1,
                nodes = new[]
                {
                    new NodeDef { id = "b0", type = NodeType.Battle, floor = 0, encounter = "d1_ashbound_skirmisher", next = new[] { "e1" } },
                    new NodeDef { id = "e1", type = NodeType.Elite, floor = 1, encounter = "d1_crypt_warden", next = new[] { "boss" } },
                    new NodeDef { id = "boss", type = NodeType.Boss, floor = 2, encounter = "d1_zoroa" }
                }
            };
            var sneakRun = new RunState(sneakMap, new[] { MakeHero("h0", "halfling") }, 5);
            var b0 = sneakMap.Get("b0");
            Assert.IsTrue(sneakRun.CanSneakNode(b0));
            sneakRun.SneakNode(b0);
            Assert.IsTrue(sneakRun.Cleared.Contains("b0"));
            Assert.AreEqual(0, sneakRun.Gold, "Sneak gives no combat rewards.");
            // ...and says so: the map shows this once after sneaking.
            StringAssert.Contains($"{RunConfig.BattleGold.min}-{RunConfig.BattleGold.max} spoils", sneakRun.LastSneakNote);
            StringAssert.Contains($"{RunConfig.BattleExp} EXP", sneakRun.LastSneakNote);
            StringAssert.Contains("reward pick", sneakRun.LastSneakNote);
            Assert.AreEqual(0, sneakRun.HalflingSneakUsesRemaining);
            Assert.IsFalse(sneakRun.CanSneakNode(sneakMap.Get("e1")), "Cannot Sneak Elite or second time.");
        }

        [Test]
        public void DragonbornDraconicPresence_IntimidatesNpc_RevealsRelic_AndShopDiscountOncePerDungeon()
        {
            var sentry = AtD1Event("D1_EVT_004", gold: 0, "dragonborn");
            int intimIdx = Array.FindIndex(sentry.CurrentEvent.choices, c => c.requiredRace == "dragonborn");
            sentry.ChooseEvent(intimIdx);
            Assert.AreEqual(30, sentry.Gold);
            Assert.AreEqual(1, sentry.Inventory.GetConsumableCount("CON002"));

            // Ash Collector (npc; shop) does NOT auto-discount gold prices, uses Intimidate instead
            var collector = AtD1Event("D1_EVT_005", gold: 24, "dragonborn");
            Assert.IsNotNull(collector.EventBlocked(collector.CurrentEvent.choices[0]),
                "Dragonborn 15% shop discount applies ONLY to Dungeon Shop, NOT The Ash Collector event.");

            // Dungeon Shop applies 15% discount once per dungeon
            collector.Gold = 100;
            var shop = DungeonShopData.Generate(1, new Random(1));
            Assert.IsTrue(shop.TryBuyConsumable(collector, "draught", out _, out _)); // 20 * 0.85 = 17
            Assert.AreEqual(83, collector.Gold);
            Assert.AreEqual(0, collector.DragonbornShopDiscountUsesRemaining);
            Assert.IsTrue(shop.TryBuyConsumable(collector, "draught", out _, out _)); // full 20
            Assert.AreEqual(63, collector.Gold);

            // Relic reveal flag
            var crown = AtD1Event("D1_EVT_019", gold: 0, "dragonborn");
            Assert.IsTrue(crown.RevealsRelicEffects);
        }

        [Test]
        public void LiichtianAndMerfolkUtilities_RevealCursesSupernaturalSourceAndLiquidOutcomes()
        {
            // Liichtian reveals curses, supernatural source without auto-completing, and sacred blessing
            var census = AtD1Event("D1_EVT_012", gold: 0, "liichtian");
            Assert.IsTrue(census.RevealsSupernaturalSource(census.CurrentEvent));
            Assert.IsNotNull(census.CurrentEvent, "Revealing supernatural source must NOT auto-complete the event.");
            Assert.IsFalse(census.Cleared.Contains("n0"));

            var sepulcher = AtD1Event("D1_EVT_002", gold: 0, "liichtian");
            Assert.IsTrue(sepulcher.RevealsCurses);
            int liiIdx = Array.FindIndex(sepulcher.CurrentEvent.choices, c => c.requiredRace == "liichtian");
            sepulcher.ChooseEvent(liiIdx);
            Assert.AreEqual(1, sepulcher.ModifierManager.GetEffectiveStackCount("MOD-006"));

            // Merfolk reveals liquid outcomes and purifies cistern
            var cistern = AtD1Event("D1_EVT_017", gold: 0, "merfolk");
            Assert.IsTrue(cistern.RevealsLiquidOutcomes(cistern.CurrentEvent));
            cistern.Party[0].hp = 40;
            cistern.Party[0].mp = 5;
            int maxHp = cistern.Party[0].stats.maxHp;
            int oneShotHeal = Math.Max(1, (int)Math.Round(maxHp * 0.08f)); // MOD-011 Second Wind
            int expectedHp = Math.Min(maxHp, 40 + maxHp * 30 / 100 + oneShotHeal);
            int expectedMp = Math.Min(cistern.Party[0].stats.maxMp, 5 + Math.Max(1, cistern.Party[0].stats.maxMp * 20 / 100));
            int merIdx = Array.FindIndex(cistern.CurrentEvent.choices, c => c.requiredRace == "merfolk");
            string merOutcome = cistern.ChooseEvent(merIdx);
            Assert.AreEqual(expectedHp, cistern.Party[0].hp);
            Assert.AreEqual(expectedMp, cistern.Party[0].mp);
            StringAssert.Contains("Second Wind", merOutcome);
        }

        [Test]
        public void OncePerDungeonUtilities_ResetOnAdvancingToNextDungeon()
        {
            var run = RunWithRaces("human", "halfling", "dragonborn", "elf");
            run.Gold = 100;
            run.Enter(run.Map.Get("n0")); // D1_EVT_001 (has liichtian option)
            int liiIdx = Array.FindIndex(run.CurrentEvent.choices, c => c.requiredRace == "liichtian");
            run.ChooseEvent(liiIdx); // borrows via Human Adaptability
            Assert.AreEqual(0, run.HumanAdaptabilityUsesRemaining);

            var shop = DungeonShopData.Generate(1, new Random(1));
            shop.TryBuyConsumable(run, "draught", out _, out _);
            Assert.AreEqual(0, run.DragonbornShopDiscountUsesRemaining);

            var nextMap = MapGenerator.Generate(99, act: 2);
            Assert.IsTrue(run.AdvanceToNextDungeon(nextMap));
            Assert.AreEqual(1, run.HumanAdaptabilityUsesRemaining);
            Assert.AreEqual(1, run.HalflingSneakUsesRemaining);
            Assert.AreEqual(1, run.DragonbornShopDiscountUsesRemaining);
            Assert.AreEqual(1, run.ElfWeaknessRevealUsesRemaining);
        }

        [Test]
        public void MapScreen_NodeClickOnlySelects_TravelCommitsToThePickedNode()
        {
            var gameGo = new UnityEngine.GameObject("MapScreenTestGame");
            var hostGo = new UnityEngine.GameObject("MapScreenTestHost");
            try
            {
                var game = gameGo.AddComponent<Game>();
                Game.SetInstance(game);
                var map = new MapDef
                {
                    name = "Fork", act = 1,
                    nodes = new[]
                    {
                        new NodeDef { id = "a", type = NodeType.Event, floor = 0, lane = 0, eventId = "D1_EVT_001", next = new[] { "boss" } },
                        new NodeDef { id = "b", type = NodeType.Rest, floor = 0, lane = 1, next = new[] { "boss" } },
                        new NodeDef { id = "boss", type = NodeType.Boss, floor = 1, encounter = "d1_zoroa" },
                    }
                };
                var run = new RunState(map, new[] { MakeHero("h0", "human") }, 3);
                game.ApplyLoadedCampaign(new CharacterRoster(startWithEmptyParty: true), run);

                var screen = new MapScreen();
                screen.Open(hostGo.transform);
                var menu = hostGo.GetComponentInChildren<UitkMenu>();
                UitkMenu.Item Item(string text) => menu.items.First(i => i.text == text);

                Item("Rest").onClick();                       // pick the Rest path
                Assert.IsNull(run.Current, "clicking a node must not travel");
                Assert.AreSame(Item("Travel"), menu.Current, "focus moves to Travel to confirm");

                menu.Focus(Item("Event"), false);             // pointer drifts over the other path
                Item("Travel").onClick();
                Assert.AreEqual("b", run.Current?.id, "Travel goes to the picked node, not the last hovered one");
                screen.Close();
            }
            finally
            {
                Game.SetInstance(null);
                UnityEngine.Object.DestroyImmediate(hostGo);
                UnityEngine.Object.DestroyImmediate(gameGo);
            }
        }

        [Test]
        public void EventScreen_HidesChoiceOutputsBeforeSelection_WhileKeepingCostsAndBlockedReasons()
        {
            var gameGo = new UnityEngine.GameObject("EventScreenTestGame");
            var hostGo = new UnityEngine.GameObject("EventScreenTestHost");
            try
            {
                var game = gameGo.AddComponent<Game>();
                Game.SetInstance(game);
                var run = AtD1Event("D1_EVT_001", gold: 50, "elf", "dwarf", "halfling", "dragonborn");
                game.ApplyLoadedCampaign(new CharacterRoster(startWithEmptyParty: true), run);

                var screen = new EventScreen();
                screen.Open(hostGo.transform);

                var choiceHost = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(screen.UiRoot, "EventChoiceHost");
                Assert.IsNotNull(choiceHost);

                var allTexts = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(choiceHost)
                    .ToList()
                    .Select(l => l.text ?? "")
                    .ToList();
                string joined = string.Join(" | ", allTexts).ToUpperInvariant();

                // Choice labels remain visible
                StringAssert.Contains("PRAY FOR A BLESSING", joined);
                StringAssert.Contains("INVESTIGATE THE ALTAR", joined);
                StringAssert.Contains("LEAVE THE CHAPEL", joined);

                // Upfront costs & blocked requirements remain visible
                StringAssert.Contains("20 SPOILS", joined);
                StringAssert.Contains("REQUIRES LIICHTIAN", joined);

                // Outputs / rewards / consequences are strictly hidden before choosing
                Assert.IsFalse(joined.Contains("+30 SPOILS"), "Spoils gain must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("HEAL"), "Heal output must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("NEXT BATTLE"), "Battle buff output must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("ARCANE CURRENT"), "Modifier reward must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("RISK"), "Risk spoiler must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("NOTHING HAPPENS"), "No-op outcome spoiler must be hidden before selection: " + joined);

                // D20 Attribute Check pre-roll requirements (Attribute, DC, Party Highest Modifier, D20 Roll info) MUST be visible before selection
                StringAssert.Contains("WIS CHECK · DC 12", joined);
                StringAssert.Contains("D20 ROLL", joined);
                StringAssert.Contains("PARTY HIGHEST WIS MOD:", joined);

                screen.Close();
            }
            finally
            {
                Game.SetInstance(null);
                UnityEngine.Object.DestroyImmediate(hostGo);
                UnityEngine.Object.DestroyImmediate(gameGo);
            }
        }

        [Test]
        public void PartyHighestAttributeModifier_UsesHighestLivingMemberOnly_AndExcludesDeadMembers()
        {
            var run = RunWithRaces("human", "elf", "dwarf", "halfling");
            // Configure distinct STR scores: Character A (+1), Character B (+3), Character C (+0), Character D (+2)
            run.Party[0].abilities.baseScores = AbilityScores.Uniform(10).With(Ability.STR, 12);
            run.Party[0].abilities.racialModifiers = AbilityScores.Zero;
            run.Party[0].RecalculateStats(fillResources: true);

            run.Party[1].abilities.baseScores = AbilityScores.Uniform(10).With(Ability.STR, 16);
            run.Party[1].abilities.racialModifiers = AbilityScores.Zero;
            run.Party[1].RecalculateStats(fillResources: true);

            run.Party[2].abilities.baseScores = AbilityScores.Uniform(10).With(Ability.STR, 10);
            run.Party[2].abilities.racialModifiers = AbilityScores.Zero;
            run.Party[2].RecalculateStats(fillResources: true);

            run.Party[3].abilities.baseScores = AbilityScores.Uniform(10).With(Ability.STR, 14);
            run.Party[3].abilities.racialModifiers = AbilityScores.Zero;
            run.Party[3].RecalculateStats(fillResources: true);

            Assert.AreEqual(1, run.Party[0].Mod(Ability.STR));
            Assert.AreEqual(3, run.Party[1].Mod(Ability.STR));
            Assert.AreEqual(0, run.Party[2].Mod(Ability.STR));
            Assert.AreEqual(2, run.Party[3].Mod(Ability.STR));

            // All alive -> Party Highest STR Modifier is +3 (neither summed to +6 nor averaged)
            Assert.AreEqual(3, run.GetPartyHighestAttributeModifier(Ability.STR));

            // Character B (+3) dies in battle and stays at 0 HP -> Party Highest STR Modifier drops to Character D (+2)
            run.Party[1].hp = 0;
            Assert.IsFalse(run.Party[1].Alive);
            Assert.AreEqual(2, run.GetPartyHighestAttributeModifier(Ability.STR));

            // Character D (+2) also dies -> drops to Character A (+1)
            run.Party[3].hp = 0;
            Assert.AreEqual(1, run.GetPartyHighestAttributeModifier(Ability.STR));
        }

        [Test]
        public void D20AttributeCheck_EvaluationRules_MatchPromptExamplesAndNaturalOverrides()
        {
            // Prompt §2.D worked example: DC = 15, Party Highest STR Modifier = +3
            Assert.AreEqual(
                AttributeCheckResultCategory.CriticalFailure,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 1, partyHighestModifier: 3, difficultyClass: 15, out int r1));
            Assert.AreEqual(4, r1);

            Assert.AreEqual(
                AttributeCheckResultCategory.Failure,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 5, partyHighestModifier: 3, difficultyClass: 15, out int r5));
            Assert.AreEqual(8, r5);

            Assert.AreEqual(
                AttributeCheckResultCategory.Success,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 12, partyHighestModifier: 3, difficultyClass: 15, out int r12));
            Assert.AreEqual(15, r12);

            Assert.AreEqual(
                AttributeCheckResultCategory.Success,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 19, partyHighestModifier: 3, difficultyClass: 15, out int r19));
            Assert.AreEqual(22, r19);

            Assert.AreEqual(
                AttributeCheckResultCategory.CriticalSuccess,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 20, partyHighestModifier: 3, difficultyClass: 15, out int r20));
            Assert.AreEqual(23, r20);

            // Natural 20 always Critical Success even if 20 + Mod < DC
            Assert.AreEqual(
                AttributeCheckResultCategory.CriticalSuccess,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 20, partyHighestModifier: -1, difficultyClass: 25, out int rNat20Low));
            Assert.AreEqual(19, rNat20Low);

            // Natural 1 always Critical Failure even if 1 + Mod >= DC
            Assert.AreEqual(
                AttributeCheckResultCategory.CriticalFailure,
                StatCalculator.EvaluateD20AttributeCheck(d20Roll: 1, partyHighestModifier: 10, difficultyClass: 5, out int rNat1High));
            Assert.AreEqual(11, rNat1High);
        }

        [Test]
        public void D20AttributeCheck_AllSixAttributesExistInDungeon1_AndExecuteFourDistinctOutcomeTiers()
        {
            var d1 = Database.Events.Values.Where(e => e.dungeonAct == 1).ToList();
            var checkChoices = d1.SelectMany(e => e.choices.Where(c => c.isAttributeCheck).Select(c => (ev: e, choice: c))).ToList();
            Assert.GreaterOrEqual(checkChoices.Count, 6, "Dungeon 1 should feature D20 Attribute Checks across all 6 attributes.");

            foreach (var ab in AbilityScores.AllAbilities)
            {
                Assert.IsTrue(checkChoices.Any(x => x.choice.checkAbility == ab), $"Missing D20 Attribute Check for {ab}");
            }

            // Test all 4 outcome tiers on D1_EVT_008 (STR Check, DC 13) with Party Highest STR Mod = +3
            int FindStrCheckIndex(RunState r) => Array.FindIndex(r.CurrentEvent.choices, c => c.isAttributeCheck && c.checkAbility == Ability.STR);

            RunState SetupArmoryWithStrMod3()
            {
                var r = AtD1Event("D1_EVT_008", gold: 0, "human", "elf", "dwarf", "halfling");
                r.Party[0].abilities.baseScores = AbilityScores.Uniform(10).With(Ability.STR, 16);
                r.Party[0].abilities.racialModifiers = AbilityScores.Zero;
                r.Party[0].RecalculateStats(fillResources: true);
                return r;
            }

            // 1. Critical Success (Natural 20): +1 equipment, +2 MAT001, +25 spoils
            var critSuccRun = SetupArmoryWithStrMod3();
            int strIdx = FindStrCheckIndex(critSuccRun);
            Assert.GreaterOrEqual(strIdx, 0);
            critSuccRun.SetEventChoiceD20RollOverride(strIdx, 20);
            string csOut = critSuccRun.ChooseEvent(strIdx);
            Assert.IsNotNull(critSuccRun.LastAttributeCheckResult);
            Assert.AreEqual(AttributeCheckResultCategory.CriticalSuccess, critSuccRun.LastAttributeCheckResult.Value.category);
            Assert.AreEqual(23, critSuccRun.LastAttributeCheckResult.Value.checkResult);
            Assert.AreEqual(1, critSuccRun.Inventory.equipment.Count);
            Assert.AreEqual(2, critSuccRun.Inventory.GetMaterialCount("MAT001"));
            Assert.AreEqual(25, critSuccRun.Gold);
            StringAssert.Contains("CRITICAL SUCCESS", csOut);

            // 2. Success (Roll 10 + 3 = 13 >= DC 13): +1 equipment, +1 MAT001, 0 gold, no HP lost
            var succRun = SetupArmoryWithStrMod3();
            int hpBeforeSucc = succRun.Party[0].hp;
            succRun.SetEventChoiceD20RollOverride(strIdx, 10);
            string sOut = succRun.ChooseEvent(strIdx);
            Assert.AreEqual(AttributeCheckResultCategory.Success, succRun.LastAttributeCheckResult.Value.category);
            Assert.AreEqual(13, succRun.LastAttributeCheckResult.Value.checkResult);
            Assert.AreEqual(1, succRun.Inventory.equipment.Count);
            Assert.AreEqual(1, succRun.Inventory.GetMaterialCount("MAT001"));
            Assert.AreEqual(0, succRun.Gold);
            Assert.AreEqual(hpBeforeSucc, succRun.Party[0].hp);
            StringAssert.Contains("SUCCESS", sOut);

            // 3. Failure (Roll 5 + 3 = 8 < DC 13): 10% hurt, +1 equipment
            var failRun = SetupArmoryWithStrMod3();
            int maxHpFail = failRun.Party[0].stats.maxHp;
            failRun.SetEventChoiceD20RollOverride(strIdx, 5);
            string fOut = failRun.ChooseEvent(strIdx);
            Assert.AreEqual(AttributeCheckResultCategory.Failure, failRun.LastAttributeCheckResult.Value.category);
            Assert.AreEqual(8, failRun.LastAttributeCheckResult.Value.checkResult);
            Assert.AreEqual(maxHpFail - maxHpFail * 10 / 100, failRun.Party[0].hp);
            Assert.AreEqual(1, failRun.Inventory.equipment.Count);
            StringAssert.Contains("FAILURE", fOut);

            // 4. Critical Failure (Natural 1): 18% hurt, MOD-020 Heavy Steps curse, 0 equipment
            var critFailRun = SetupArmoryWithStrMod3();
            int maxHpCritFail = critFailRun.Party[0].stats.maxHp;
            critFailRun.SetEventChoiceD20RollOverride(strIdx, 1);
            string cfOut = critFailRun.ChooseEvent(strIdx);
            Assert.AreEqual(AttributeCheckResultCategory.CriticalFailure, critFailRun.LastAttributeCheckResult.Value.category);
            Assert.AreEqual(4, critFailRun.LastAttributeCheckResult.Value.checkResult);
            Assert.AreEqual(maxHpCritFail - maxHpCritFail * 18 / 100, critFailRun.Party[0].hp);
            Assert.AreEqual(0, critFailRun.Inventory.equipment.Count);
            Assert.AreEqual(1, critFailRun.ModifierManager.GetEffectiveStackCount("MOD-020"));
            StringAssert.Contains("CRITICAL FAILURE", cfOut);
        }

        [Test]
        public void D20AttributeCheck_FailureHazardDamage_RespectsHalflingSmallAndNimble_AndPallbearersGlovesR014()
        {
            // D1_EVT_006 DEX Check (DC 13, isTrapChoice = true) deals 14% hazard damage on Failure
            var noHalf = AtD1Event("D1_EVT_006", gold: 0, "human");
            int dexIdx = Array.FindIndex(noHalf.CurrentEvent.choices, c => c.isAttributeCheck && c.checkAbility == Ability.DEX);
            Assert.GreaterOrEqual(dexIdx, 0);
            noHalf.SetEventChoiceD20RollOverride(dexIdx, 3); // Failure
            int maxHp0 = noHalf.Party[0].stats.maxHp;
            noHalf.ChooseEvent(dexIdx);
            Assert.AreEqual(maxHp0 - (int)Math.Round(maxHp0 * 0.14f), noHalf.Party[0].hp);

            // With Halfling in party -> 50% hazard damage reduction (14% -> 7%)
            var withHalf = AtD1Event("D1_EVT_006", gold: 0, "halfling");
            withHalf.SetEventChoiceD20RollOverride(dexIdx, 3); // Failure
            int maxHp1 = withHalf.Party[0].stats.maxHp;
            string halfOut = withHalf.ChooseEvent(dexIdx);
            Assert.AreEqual(maxHp1 - (int)Math.Round(maxHp1 * 0.07f), withHalf.Party[0].hp);
            StringAssert.Contains("Small and Nimble", halfOut);

            // With Relic R014 Pallbearer's Gloves -> trap hazard damage completely negated on first trap choice
            var withR014 = AtD1Event("D1_EVT_006", gold: 0, "human");
            withR014.AddRelic("R014");
            withR014.SetEventChoiceD20RollOverride(dexIdx, 1); // Critical Failure (22% hazard damage)
            int maxHp2 = withR014.Party[0].stats.maxHp;
            string r014Out = withR014.ChooseEvent(dexIdx);
            Assert.AreEqual(maxHp2, withR014.Party[0].hp, "R014 negates minor chest trap hazard damage even on D20 Critical Failure.");
            StringAssert.Contains("Pallbearer's Gloves", r014Out);
        }

        [Test]
        public void D20AttributeCheck_NeverLocksOnNegativeModifier_AndRacialAndAlternativeOptionsStayIndependent()
        {
            // Party has all 8s (Modifier -1) and no Elf/Dwarf/Liichtian/Dragonborn
            var run = AtD1Event("D1_EVT_006", gold: 50, "human");
            foreach (var u in run.Party)
            {
                u.abilities.baseScores = AbilityScores.Uniform(8);
                u.abilities.racialModifiers = AbilityScores.Zero;
                u.RecalculateStats(fillResources: true);
            }
            Assert.AreEqual(-1, run.GetPartyHighestAttributeModifier(Ability.DEX));

            int dexIdx = Array.FindIndex(run.CurrentEvent.choices, c => c.isAttributeCheck && c.checkAbility == Ability.DEX);
            int altGoldIdx = Array.FindIndex(run.CurrentEvent.choices, c => c.goldCost == 20);
            int elfIdx = Array.FindIndex(run.CurrentEvent.choices, c => c.requiredRace == "elf");

            // Attribute Check is not blocked despite -1 modifier, and does not borrow Human Adaptability
            var dexStatus = run.GetEventChoiceStatus(dexIdx);
            Assert.IsTrue(dexStatus.isVisible);
            Assert.IsFalse(dexStatus.isBlocked, "Attribute Check must never lock due to low modifier.");
            Assert.IsFalse(dexStatus.usesAdaptabilityBorrow, "Attribute Check must not use Human Adaptability borrow.");

            // Alternative solution (20 spoils) and Racial option (via Adaptability) remain independently selectable
            Assert.IsFalse(run.GetEventChoiceStatus(altGoldIdx).isBlocked);
            Assert.IsFalse(run.GetEventChoiceStatus(elfIdx).isBlocked);
            Assert.IsTrue(run.GetEventChoiceStatus(elfIdx).usesAdaptabilityBorrow);
        }
    }
}
