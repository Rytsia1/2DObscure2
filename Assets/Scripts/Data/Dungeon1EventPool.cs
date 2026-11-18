using System;
using System.Collections.Generic;

namespace Obscure
{
    // Authoritative Dungeon 1 Event Pool (Assets/Docs/Felna_Dungeon1_Event_Pool_Final.xlsx).
    // 20 exclusive Layer 1 (Ruined Kingdom) events across 8 categories with GDD Bab 6 §5 & §10 Racial Utility integration.
    public static partial class Database
    {
        public static EventDef[] BuildDungeon1Events() => new EventDef[]
        {
            // ---- 1. SHRINE (3) ----
            new EventDef
            {
                id = "D1_EVT_001",
                title = "Chapel of Unclaimed Names",
                category = EventCategory.Shrine,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Sacred, EventTags.Supernatural },
                text = "A small chapel is lined with nameless gravestones. At its center, an altar still holds lingering spiritual power.",
                supernaturalSourceClue = "Residual prayers cling to the nameless stones—no hostile entity anchors the altar.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Pray for a blessing",
                        goldCost = 20,
                        healPercent = 15,
                        battleBuff = new StatusApply { id = StatusId.AttackUp, turns = 3, magnitude = 0.25f },
                        outcome = "A quiet warmth answers your offering. Your party feels steadied for the road ahead."
                    },
                    new EventChoice
                    {
                        label = "Investigate the altar",
                        gold = 30,
                        modifierId = "MOD-002",
                        riskChancePercent = 50,
                        riskFailHurtPercent = 10,
                        riskFailModifierId = "MOD-018",
                        outcome = "Behind the weathered stone, you uncover offerings and lingering arcane resonance.",
                        riskFailOutcome = "A dormant ward snaps back across the party as you pry into the altar."
                    },
                    new EventChoice
                    {
                        label = "Channel sacred radiance",
                        requiredRace = "liichtian",
                        healPercent = 25,
                        mpRestorePercent = 15,
                        modifierId = "MOD-007",
                        outcome = "Your celestial resonance awakens the altar's purest blessing at no cost."
                    },
                    new EventChoice
                    {
                        label = "Leave the chapel",
                        outcome = "You bow in silence and step back into the corridor."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_002",
                title = "Sepulcher of the Crown",
                category = EventCategory.Shrine,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Sacred, EventTags.Relic, EventTags.CursedReward },
                text = "A royal sepulcher holds weathered emblems of sovereignty and a sealed offering coffer.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Pay respects to the tomb",
                        gold = 15,
                        healPercent = 10,
                        outcome = "You honor the fallen house. A modest offering rests beside the plinth."
                    },
                    new EventChoice
                    {
                        label = "Open the offering coffer",
                        goldCost = 25,
                        gold = 40,
                        materialId = "MAT001",
                        materialAmount = 2,
                        outcome = "Inside the coffer lie intact coins and usable ironwork."
                    },
                    new EventChoice
                    {
                        label = "Take the abandoned heirloom",
                        relic = true,
                        curseModifierId = "MOD-019",
                        hasHiddenCurse = true,
                        outcome = "You claim the heirloom from the dais, though a cold weight settles over the party."
                    },
                    new EventChoice
                    {
                        label = "Offer a sacred litany",
                        requiredRace = "liichtian",
                        modifierId = "MOD-006",
                        mpRestorePercent = 15,
                        outcome = "Sacred light answers the tomb's wards, bolstering the party's resolve."
                    },
                    new EventChoice
                    {
                        label = "Claim the heirloom by draconic right",
                        requiredRace = "dragonborn",
                        relic = true,
                        gold = 20,
                        outcome = "The sepulcher's ward yields to your commanding presence—you take the prize uncursed."
                    },
                    new EventChoice
                    {
                        label = "Leave the sepulcher",
                        outcome = "You leave the royal remains undisturbed."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_003",
                title = "Bell of the Last Vigil",
                category = EventCategory.Shrine,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Sacred, EventTags.Supernatural },
                text = "An ancient bell hangs inside a crumbling tower. Its chime could echo far through the ruins.",
                supernaturalSourceClue = "The bell resonates with vigil wards—ringing it draws a protective cadence, yet shakes the air with spiritual backlash.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Ring the vigil bell",
                        modifierId = "MOD-004",
                        battleBuff = new StatusApply { id = StatusId.AttackUp, turns = 3, magnitude = 0.25f },
                        riskChancePercent = 50,
                        riskFailHurtPercent = 10,
                        riskFailModifierId = "MOD-020",
                        riskGrantsRewardOnFail = true,
                        outcome = "A clear, resonant note steadies your focus for battles ahead.",
                        riskFailOutcome = "The tolling bell wakes a heavy spiritual tremor that rattles the party even as its blessing lands."
                    },
                    new EventChoice
                    {
                        label = "Attune the sacred chime",
                        requiredRace = "liichtian",
                        modifierId = "MOD-013",
                        mpRestorePercent = 20,
                        outcome = "You temper the bell's resonance with astral light, drawing pure protection without backlash."
                    },
                    new EventChoice
                    {
                        label = "Leave it silent and take the lower path",
                        healPercent = 10,
                        outcome = "You bypass the bell tower safely and catch your breath along the quiet passage."
                    }
                }
            },

            // ---- 2. NPC ENCOUNTER (2) ----
            new EventDef
            {
                id = "D1_EVT_004",
                title = "The Last Sentry",
                category = EventCategory.NpcEncounter,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Npc },
                text = "A lone royal sentry still holds its post in the ruined hall, treating your party as trespassers.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Parley and offer passage coin",
                        goldCost = 20,
                        item = "CON011",
                        materialId = "MAT001",
                        materialAmount = 1,
                        bonusInfoText = "The sentry gestures toward the safer stone arches ahead.",
                        outcome = "Satisfied with your tribute, the sentry lowers its halberd and shares spare rations."
                    },
                    new EventChoice
                    {
                        label = "Endure its challenge to prove intent",
                        hurtPercent = 10,
                        gold = 25,
                        modifierId = "MOD-006",
                        outcome = "You weather the sentry's testing strike without breaking rank. Impressed, it grants passage."
                    },
                    new EventChoice
                    {
                        label = "Intimidate the sentry",
                        requiredRace = "dragonborn",
                        gold = 30,
                        item = "CON002",
                        bonusInfoText = "The sentry divulges patrol gaps along the ruin's corridors.",
                        outcome = "Your draconic presence overwhelms the sentry's resolve—it yields supplies and steps aside."
                    },
                    new EventChoice
                    {
                        label = "Withdraw before conflict",
                        outcome = "You back away slowly while the sentry resumes its silent watch."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_005",
                title = "The Ash Collector",
                category = EventCategory.NpcEncounter,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Npc, EventTags.Shop },
                text = "A veiled figure gathers relics and ash from the fallen kingdom, offering silent barters from a heavy pack.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Buy salvaged supplies",
                        goldCost = 25,
                        item = "CON002",
                        modifierId = "MOD-012",
                        outcome = "You trade coin for a sealed potion and calming incense."
                    },
                    new EventChoice
                    {
                        label = "Trade Iron Scrap for Soul Ash",
                        requiredMaterialId = "MAT001",
                        requiredMaterialAmount = 1,
                        materialId = "MAT002",
                        materialAmount = 1,
                        gold = 25,
                        outcome = "The collector weighs your scrap, nods once, and exchanges a pouch of Soul Ash and coin."
                    },
                    new EventChoice
                    {
                        label = "Purchase a salvaged relic",
                        goldCost = 45,
                        relic = true,
                        outcome = "The collector unwraps an ancient keepsake from ash-stained cloth."
                    },
                    new EventChoice
                    {
                        label = "Intimidate the collector",
                        requiredRace = "dragonborn",
                        materialId = "MAT002",
                        materialAmount = 1,
                        item = "CON011",
                        outcome = "Flinching under your draconic gaze, the collector surrenders Soul Ash and rations."
                    },
                    new EventChoice
                    {
                        label = "Decline and move on",
                        outcome = "You refuse the exchange and leave the collector to the ashes."
                    }
                }
            },

            // ---- 3. TREASURE (3) ----
            new EventDef
            {
                id = "D1_EVT_006",
                title = "The Royal Treasury",
                category = EventCategory.Treasure,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Hidden, EventTags.Trap, EventTags.Hazard },
                text = "An old royal vault still holds several chests—some pried open, others sealed behind warded latches.",
                hiddenClue = "Hairline dart slits line the lockplate of the heavy chest.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Take the unlocked chest",
                        gold = 25,
                        outcome = "You gather loose coin from the unsealed chest without touching the wards."
                    },
                    new EventChoice
                    {
                        label = "Use coin weights to safely release the lock",
                        goldCost = 20,
                        gold = 55,
                        item = "CON001",
                        outcome = "The weighted coins balance the latch cleanly, revealing gold and a preserved potion."
                    },
                    new EventChoice
                    {
                        label = "Force the locked chest open",
                        gold = 65,
                        materialId = "MAT001",
                        materialAmount = 2,
                        isTrapChoice = true,
                        riskChancePercent = 50,
                        riskFailHazardDamagePercent = 16,
                        riskGrantsRewardOnFail = true,
                        outcome = "The lock snaps cleanly! Inside rest coins and ironwork.",
                        riskFailOutcome = "A spring-loaded hazard triggers as the lid pops open, though you still seize the spoils."
                    },
                    new EventChoice
                    {
                        label = "Detect and disarm the chest trap",
                        requiredRace = "elf",
                        gold = 65,
                        materialId = "MAT001",
                        materialAmount = 2,
                        outcome = "With keen senses, you unhook the hidden needle wire and empty the chest unharmed."
                    },
                    new EventChoice
                    {
                        label = "Leave the treasury",
                        outcome = "You leave the remaining chests behind."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_007",
                title = "Soulglass Reliquary",
                category = EventCategory.Treasure,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Relic, EventTags.Supernatural, EventTags.CursedReward },
                text = "A crystalline vessel glows with a pale, soul-like luminescence, humming with unstable energy.",
                supernaturalSourceClue = "Fractured containment runes leak volatile energy—touching the vessel bare-handed carries a draining curse.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Take the reliquary despite the risk",
                        relic = true,
                        hurtPercent = 10,
                        curseModifierId = "MOD-021",
                        hasHiddenCurse = true,
                        outcome = "The crystal chills your hands as you claim what rests within, draining your mana reserves."
                    },
                    new EventChoice
                    {
                        label = "Stabilize the vessel with coin and wards",
                        goldCost = 35,
                        gold = 15,
                        modifierId = "MOD-010",
                        outcome = "You ground the vessel's volatile charge and glean sharp treasure instincts."
                    },
                    new EventChoice
                    {
                        label = "Claim the reliquary with draconic presence",
                        requiredRace = "dragonborn",
                        relic = true,
                        outcome = "Your draconic aura suppresses the backlash, letting you secure the relic intact."
                    },
                    new EventChoice
                    {
                        label = "Leave the reliquary untouched",
                        outcome = "You step away from the humming crystal."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_008",
                title = "The Sealed Armory",
                category = EventCategory.Treasure,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.AncientMechanism, EventTags.Destructible },
                text = "Heavy stone doors guard an old armory, held fast by an ancient geared mechanism.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Breach the door with a Smoke Bomb",
                        requiredItem = "CON018",
                        randomEquipment = true,
                        materialId = "MAT001",
                        materialAmount = 2,
                        outcome = "The blast cracks the brittle masonry, exposing racked arms and iron scrap."
                    },
                    new EventChoice
                    {
                        label = "Strain the geared door open by force",
                        hurtPercent = 10,
                        randomEquipment = true,
                        outcome = "Bruised and winded, the party heaves the stone slab aside long enough to claim a weapon."
                    },
                    new EventChoice
                    {
                        label = "Trigger the ancient mechanism & breach shortcut",
                        requiredRace = "dwarf",
                        randomEquipment = true,
                        materialId = "MAT001",
                        materialAmount = 2,
                        gold = 20,
                        outcome = "Reading the stonework at a glance, you unlock the mechanism and clear a shortcut effortlessly."
                    },
                    new EventChoice
                    {
                        label = "Pass the sealed armory",
                        outcome = "You leave the stone doors shut and press onward."
                    }
                }
            },

            // ---- 4. TRAP & HAZARD (3) ----
            new EventDef
            {
                id = "D1_EVT_009",
                title = "Gallery of Arrows",
                category = EventCategory.TrapAndHazard,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Trap, EventTags.Hidden, EventTags.Hazard },
                text = "A long stone gallery bristles with active arrow slits and sunken pressure plates.",
                hiddenClue = "Every third flagstone sits a fraction higher—stepping on the mortar seams avoids the trigger.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Dash through the arrow volley",
                        hazardDamagePercent = 18,
                        gold = 35,
                        outcome = "You sprint across the gallery under snapping bolts, snatching a pouch along the wall."
                    },
                    new EventChoice
                    {
                        label = "Pay to wedge and jam the pressure plates",
                        goldCost = 20,
                        item = "CON007",
                        outcome = "Using iron spikes and coin, you wedge the plates shut and recover a field dressing."
                    },
                    new EventChoice
                    {
                        label = "Detect and disarm the gallery traps",
                        requiredRace = "elf",
                        gold = 40,
                        materialId = "MAT001",
                        materialAmount = 1,
                        outcome = "Keen senses spot every tripwire—you disarm the volley mechanism and salvage intact parts."
                    },
                    new EventChoice
                    {
                        label = "Take the longer outer corridor",
                        outcome = "You detour around the gallery unharmed, though empty-handed."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_010",
                title = "The Crumbling Walkway",
                category = EventCategory.TrapAndHazard,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Hazard, EventTags.Narrow, EventTags.Destructible },
                text = "A weathered stone bridge groans and sheds rubble into the chasm the moment you step onto it.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Sprint across the collapsing span",
                        hazardDamagePercent = 16,
                        gold = 30,
                        outcome = "Stones give way underfoot as you scramble across, grabbing dropped coin near the far pier."
                    },
                    new EventChoice
                    {
                        label = "Reinforce the supports before crossing",
                        goldCost = 20,
                        modifierId = "MOD-015",
                        outcome = "You brace the keystone safely and cross with renewed confidence."
                    },
                    new EventChoice
                    {
                        label = "Slip along the narrow side ledge",
                        requiredRace = "halfling",
                        gold = 25,
                        outcome = "Small and nimble, you thread the narrow stone ledge as a clean shortcut."
                    },
                    new EventChoice
                    {
                        label = "Drop the destructible pier to form a ramp",
                        requiredRace = "dwarf",
                        materialId = "MAT001",
                        materialAmount = 2,
                        outcome = "Striking the weak pillar creates a solid stone shortcut and yields usable iron scrap."
                    },
                    new EventChoice
                    {
                        label = "Take the long detour around the chasm",
                        outcome = "You spend extra time circling the drop, arriving safely on the far side."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_011",
                title = "Ashfall Courtyard",
                category = EventCategory.TrapAndHazard,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Hazard },
                text = "Hot ash and choking cinders swirl across a ruined courtyard, stinging eyes and lungs.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Rush straight through the ashfall",
                        hazardDamagePercent = 14,
                        curseModifierId = "MOD-020",
                        materialId = "MAT002",
                        materialAmount = 1,
                        gold = 25,
                        outcome = "Searing ash slows your stride, though you snatch Soul Ash and coin from the center."
                    },
                    new EventChoice
                    {
                        label = "Soak cloths with a Draught for protection",
                        requiredItem = "draught",
                        materialId = "MAT002",
                        materialAmount = 1,
                        outcome = "Dampened wraps filter the worst of the cinders, letting you cross and collect Soul Ash safely."
                    },
                    new EventChoice
                    {
                        label = "Detect calm pockets through the ash",
                        requiredRace = "elf",
                        materialId = "MAT002",
                        materialAmount = 1,
                        gold = 30,
                        outcome = "Keen senses read the drafts—you weave cleanly through clear air and gather Soul Ash."
                    },
                    new EventChoice
                    {
                        label = "Skirt the outer courtyard wall",
                        outcome = "You take the sheltered perimeter and cross without injury."
                    }
                }
            },

            // ---- 5. SUPERNATURAL (3) ----
            new EventDef
            {
                id = "D1_EVT_012",
                title = "The Whispering Census",
                category = EventCategory.Supernatural,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Supernatural, EventTags.Corruption },
                text = "Disembodied voices recite unfamiliar names in the dark. The longer you listen, the clearer they grow.",
                supernaturalSourceClue = "An old ward-echo replays a spectral ledger—it drains mental focus rather than flesh. Choose how to respond.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Listen closely for useful clues",
                        gold = 35,
                        modifierId = "MOD-003",
                        curseModifierId = "MOD-018",
                        hasHiddenCurse = true,
                        outcome = "Patterns emerge among the murmured names, sharpening your strike—though your focus fractures."
                    },
                    new EventChoice
                    {
                        label = "Dispel the voices with an offering",
                        goldCost = 25,
                        modifierId = "MOD-008",
                        mpRestorePercent = 15,
                        outcome = "The echoes quiet cleanly, leaving calm arcane clarity behind."
                    },
                    new EventChoice
                    {
                        label = "Ignore the voices and move on",
                        outcome = "You tune out the murmurs and leave the hall behind."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_013",
                title = "Mirror of Borrowed Faces",
                category = EventCategory.Supernatural,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Supernatural, EventTags.Hidden },
                text = "An ornate mirror reflects your party back—yet every reflection wears a different expression.",
                supernaturalSourceClue = "An illusionary ward mimics your party's silhouettes to lure physical contact.",
                hiddenClue = "Fine fractures spiderweb from the lower frame where a ward seal sits.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Touch the mirror's surface",
                        modifierId = "MOD-024",
                        item = "CON005",
                        riskChancePercent = 50,
                        riskFailHurtPercent = 12,
                        riskFailModifierId = "MOD-021",
                        outcome = "Elemental light ripples outward from the glass, attuning your party.",
                        riskFailOutcome = "Cold glass bites back at your fingertips, draining mana through the ward."
                    },
                    new EventChoice
                    {
                        label = "Shatter the mirror",
                        hurtPercent = 10,
                        materialId = "MAT002",
                        materialAmount = 1,
                        item = "CON019",
                        outcome = "Shards burst apart in a brief sting, leaving behind Soul Ash and a Soul Lantern."
                    },
                    new EventChoice
                    {
                        label = "Turn away and leave",
                        outcome = "You refuse to meet the reflections' gaze and walk past."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_014",
                title = "Procession of Lost Souls",
                category = EventCategory.Supernatural,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Supernatural, EventTags.Hidden },
                text = "Drifting lights glide silently down an ancient road toward the heart of the ruins.",
                supernaturalSourceClue = "These drifting soul-lights follow a residual ritual path; snatching one risks spiritual recoil.",
                hiddenClue = "A side alcove mirrors the procession's path without crossing its center.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Follow the procession at a distance",
                        gold = 20,
                        modifierId = "MOD-009",
                        outcome = "Trailing the lights leads you past an overlooked cache before the glow fades."
                    },
                    new EventChoice
                    {
                        label = "Snatch a lingering light",
                        relic = true,
                        riskChancePercent = 50,
                        riskFailHurtPercent = 12,
                        riskFailModifierId = "MOD-019",
                        riskGrantsRewardOnFail = true,
                        outcome = "The captured light coalesces cleanly into a tangible relic.",
                        riskFailOutcome = "The disturbed mote lashes back with a withering chill as you claim its remnant."
                    },
                    new EventChoice
                    {
                        label = "Step aside and let them pass",
                        outcome = "You wait in stillness until the procession vanishes down the hall."
                    }
                }
            },

            // ---- 6. PUZZLE & EXPLORATION (2) ----
            new EventDef
            {
                id = "D1_EVT_015",
                title = "The Three Royal Seals",
                category = EventCategory.PuzzleAndExploration,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.AncientMechanism, EventTags.Hidden },
                text = "A heavy stone portal bears three rotating royal seals that must be engaged in sequence.",
                hiddenClue = "Wear marks on the stone ring show the outer seal turns last.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Work out the seal sequence",
                        gold = 45,
                        modifierId = "MOD-004",
                        riskChancePercent = 50,
                        riskFailHazardDamagePercent = 10,
                        riskGrantsRewardOnFail = true,
                        outcome = "The tumblers click into place, revealing an intact alcove of coin.",
                        riskFailOutcome = "A mis-set seal snaps a stone bolt across the party before the latch finally gives."
                    },
                    new EventChoice
                    {
                        label = "Force the stone seals with wedges",
                        goldCost = 25,
                        gold = 40,
                        materialId = "MAT001",
                        materialAmount = 1,
                        outcome = "Brute leverage cracks the locking pin, granting access to the chamber beyond."
                    },
                    new EventChoice
                    {
                        label = "Align the ancient mechanism effortlessly",
                        requiredRace = "dwarf",
                        gold = 55,
                        materialId = "MAT001",
                        materialAmount = 2,
                        modifierId = "MOD-004",
                        outcome = "Your stonecraft reads every counterweight—the three seals align on the first turn."
                    },
                    new EventChoice
                    {
                        label = "Take the side passage",
                        outcome = "You bypass the sealed door and keep moving."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_016",
                title = "The Forgotten Archive",
                category = EventCategory.PuzzleAndExploration,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Hidden, EventTags.AncientMechanism },
                text = "Dusty shelves and locked stone drawers fill a hidden archive left behind by the fallen kingdom.",
                hiddenClue = "A hollow stone panel behind the center lectern conceals a geared latch.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Study the surviving records",
                        gold = 15,
                        modifierId = "MOD-003",
                        bonusInfoText = "Field sketches reveal weak joints in old ruin armor.",
                        outcome = "Tactical notes etched into parchment sharpen your party's eye for weak points."
                    },
                    new EventChoice
                    {
                        label = "Gather intact materials",
                        materialId = "MAT010",
                        materialAmount = 2,
                        outcome = "You pack dried Moonroot and salvageable fittings from the shelves."
                    },
                    new EventChoice
                    {
                        label = "Spend resources to open the secret vault",
                        goldCost = 20,
                        item = "CON019",
                        materialId = "MAT002",
                        materialAmount = 1,
                        outcome = "The vault latch yields, revealing a Soul Lantern and sealed Soul Ash."
                    },
                    new EventChoice
                    {
                        label = "Trip the ancient mechanism into the vault",
                        requiredRace = "dwarf",
                        item = "CON019",
                        materialId = "MAT002",
                        materialAmount = 1,
                        gold = 25,
                        outcome = "Stonecraft spots the recessed gear immediately, opening the vault without spending a coin."
                    },
                    new EventChoice
                    {
                        label = "Leave the archive",
                        outcome = "You close the dusty ledger and step back out."
                    }
                }
            },

            // ---- 7. SURVIVAL (2) ----
            new EventDef
            {
                id = "D1_EVT_017",
                title = "The Forgotten Cistern",
                category = EventCategory.Survival,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Liquid },
                text = "An underground cistern still holds clear-looking water, though its purity is uncertain.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Purify the water before drinking",
                        goldCost = 15,
                        healPercent = 25,
                        mpRestorePercent = 15,
                        outcome = "After filtering and boiling, the clean water restores the party's vigor."
                    },
                    new EventChoice
                    {
                        label = "Drink the raw cistern water",
                        hasHiddenOutcome = true,
                        healPercent = 20,
                        mpRestorePercent = 15,
                        riskChancePercent = 50,
                        riskFailHurtPercent = 12,
                        riskFailModifierId = "MOD-020",
                        outcome = "The spring proves unexpectedly pure, refreshing HP and MP.",
                        riskFailOutcome = "Stagnant taint turns your stomach, leaving the party sluggish."
                    },
                    new EventChoice
                    {
                        label = "Purify the cistern with Ocean's Gift",
                        requiredRace = "merfolk",
                        healPercent = 30,
                        mpRestorePercent = 20,
                        modifierId = "MOD-011",
                        outcome = "You draw away every trace of silt and toxin, restoring the entire party cleanly."
                    },
                    new EventChoice
                    {
                        label = "Conserve rations and move on",
                        healPercent = 5,
                        outcome = "You take only a cautious sip from your own flasks and continue."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_018",
                title = "The Abandoned Expedition Cache",
                category = EventCategory.Survival,
                rarity = EventRarity.Uncommon,
                dungeonAct = 1,
                tags = new[] { EventTags.Hidden },
                text = "Abandoned packs and camp gear rest in a relatively sheltered chamber.",
                hiddenClue = "Chalk arrows scratched beneath a crate point toward stashed supplies.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Rest and recover supplies",
                        healPercent = 15,
                        mpRestorePercent = 10,
                        outcome = "A brief respite by the shelter steadies breathing and mends minor wounds."
                    },
                    new EventChoice
                    {
                        label = "Take rations and scrap",
                        item = "CON011",
                        materialId = "MAT001",
                        materialAmount = 1,
                        outcome = "You pack an Emergency Ration and salvageable ironwork."
                    },
                    new EventChoice
                    {
                        label = "Investigate the previous camp's trail",
                        goldCost = 15,
                        gold = 30,
                        item = "CON002",
                        outcome = "Following the faint trail uncovers a stashed Health Potion and scattered coin."
                    },
                    new EventChoice
                    {
                        label = "Improvise with salvaged camp tools",
                        requiredRace = "human",
                        isImprovise = true,
                        gold = 30,
                        item = "CON002",
                        outcome = "Human adaptability turns spare straps and chalk marks straight to the best stash—no cost spent."
                    },
                    new EventChoice
                    {
                        label = "Leave the cache untouched",
                        outcome = "You leave the camp as you found it and move on."
                    }
                }
            },

            // ---- 8. CURSED & RISK (2) ----
            new EventDef
            {
                id = "D1_EVT_019",
                title = "Crown of the Nameless",
                category = EventCategory.CursedAndRisk,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.CursedReward, EventTags.Corruption, EventTags.Relic },
                text = "An iron-and-gold crown sits upon a cracked altar. It promises strength, yet radiates unmistakable peril.",
                supernaturalSourceClue = "A lingering corruption clings to the crown, ready to sap vitality if seized carelessly.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Take the crown's power",
                        relic = true,
                        modifierId = "MOD-016",
                        hurtPercent = 12,
                        curseModifierId = "MOD-019",
                        hasHiddenCurse = true,
                        outcome = "Power surges into your party as you seize the crown, though its curse bites deep."
                    },
                    new EventChoice
                    {
                        label = "Purge the altar's corruption",
                        goldCost = 30,
                        gold = 25,
                        modifierId = "MOD-001",
                        outcome = "The dark residue disperses, leaving clean martial focus and reclaimed coin."
                    },
                    new EventChoice
                    {
                        label = "Command the crown with draconic presence",
                        requiredRace = "dragonborn",
                        relic = true,
                        modifierId = "MOD-001",
                        outcome = "Your draconic authority bends the crown's residual will—you claim its strength uncursed."
                    },
                    new EventChoice
                    {
                        label = "Leave the crown on the altar",
                        outcome = "You step back from the tempting circlet and move on."
                    }
                }
            },
            new EventDef
            {
                id = "D1_EVT_020",
                title = "The Soul Bargain",
                category = EventCategory.CursedAndRisk,
                rarity = EventRarity.Rare,
                dungeonAct = 1,
                tags = new[] { EventTags.Supernatural, EventTags.Corruption },
                text = "An unseen voice offers a sudden surge of power in exchange for a steep toll from the party.",
                supernaturalSourceClue = "A disembodied pact-echo bound to these ruins offers strength for vitality—it has no physical body here.",
                choices = new[]
                {
                    new EventChoice
                    {
                        label = "Accept the bargain",
                        hurtPercent = 15,
                        relic = true,
                        gold = 40,
                        curseModifierId = "MOD-017",
                        hasHiddenCurse = true,
                        outcome = "Vitality drains from your limbs as power and spoils materialize before you."
                    },
                    new EventChoice
                    {
                        label = "Break the pact tether",
                        goldCost = 25,
                        modifierId = "MOD-013",
                        item = "CON005",
                        outcome = "You sever the echo's anchor, claiming protective resonance and a Mana Tonic."
                    },
                    new EventChoice
                    {
                        label = "Improvise to disrupt the echo freely",
                        requiredRace = "human",
                        isImprovise = true,
                        modifierId = "MOD-013",
                        item = "CON005",
                        outcome = "Reading the room's acoustics, you shatter the echo's anchor without spending a single coin."
                    },
                    new EventChoice
                    {
                        label = "Refuse the bargain",
                        outcome = "You reject the offer cleanly and leave the voice behind."
                    }
                }
            }
        };
    }
}
