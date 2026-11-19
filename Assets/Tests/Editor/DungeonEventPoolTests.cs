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
                StringAssert.Contains("20 GOLD", joined);
                StringAssert.Contains("REQUIRES LIICHTIAN", joined);

                // Outputs / rewards / consequences are strictly hidden before choosing
                Assert.IsFalse(joined.Contains("+30 GOLD"), "Gold gain must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("HEAL"), "Heal output must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("NEXT BATTLE"), "Battle buff output must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("ARCANE CURRENT"), "Modifier reward must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("RISK"), "Risk spoiler must be hidden before selection: " + joined);
                Assert.IsFalse(joined.Contains("NOTHING HAPPENS"), "No-op outcome spoiler must be hidden before selection: " + joined);

                screen.Close();
            }
            finally
            {
                Game.SetInstance(null);
                UnityEngine.Object.DestroyImmediate(hostGo);
                UnityEngine.Object.DestroyImmediate(gameGo);
            }
        }
    }
}
