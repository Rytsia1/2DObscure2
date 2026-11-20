// Generated from Assets/Docs/Felna_Layer1_Relic_Framework_31_Relics_No_Mourners_Coin.xlsx
// Authoritative 31 Layer 1 Relics (5 November 2026)
using System;
using System.Collections.Generic;

namespace Obscure
{
    public static partial class Database
    {
        public static readonly Dictionary<string, RelicDef> Layer1Relics = Index(new[]
        {
            new RelicDef
            {
                id = "R001",
                name = "Ashen Signet",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Common,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Party attacks against enemies above 80% HP gain +5% Damage Bonus.",
                designIntent = "Helps open fights with damage pressure without raising damage for the whole battle.",
                description = "Party attacks against enemies above 80% HP gain +5% Damage Bonus.",
                demerit = "— (no demerit)",
                buyPrice = 1000,
                sellPrice = 500
            },
            new RelicDef
            {
                id = "R002",
                name = "Soulglass Shard",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Qte,
                activationLimit = RelicActivationLimit.Counter,
                gameplayEffect = "Each time the party lands 3 Perfect Offensive QTEs, the character who scored the third Perfect restores 2% Max MP.",
                designIntent = "Rewards good QTE execution and slightly helps the MP economy.",
                description = "Each time the party lands 3 Perfect Offensive QTEs, the character who scored the third Perfect restores 2% Max MP.",
                demerit = "— (no demerit)",
                buyPrice = 1500,
                sellPrice = 750
            },
            new RelicDef
            {
                id = "R003",
                name = "Rustbound Medal",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Party members at 30% HP or less have 8% less All Damage Taken.",
                designIntent = "Helps characters in critical condition survive without protecting the whole party all the time.",
                description = "Party members at 30% HP or less have 8% less All Damage Taken.",
                demerit = "— (no demerit)",
                buyPrice = 1500,
                sellPrice = 750
            },
            new RelicDef
            {
                id = "R004",
                name = "Gravekeeper's Thread",
                category = RelicCategory.Survival,
                proposedRarity = RelicProposedRarity.Common,
                triggerType = RelicTriggerType.PostBattle,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "After a battle ends, the living character with the lowest HP percentage restores 2% Max HP.",
                designIntent = "Gives a small recovery between battles without replacing items or the Recovery service.",
                description = "After a battle ends, the living character with the lowest HP percentage restores 2% Max HP.",
                demerit = "— (no demerit)",
                buyPrice = 900,
                sellPrice = 450
            },
            new RelicDef
            {
                id = "R005",
                name = "Warden's Broken Key",
                category = RelicCategory.Utility,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Event,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "At events tagged hidden or trap, show one extra hint if the event provides extra information.",
                designIntent = "Rewards players who pay attention to information and take risks knowingly.",
                description = "At events tagged hidden or trap, show one extra hint if the event provides extra information.",
                demerit = "— (no demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R006",
                name = "Crown of the Unremembered",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The party deals +6% Damage Bonus against enemies affected by at least one Status Effect.",
                designIntent = "Encourages synergy between debuffs, status application and damage dealers.",
                description = "The party deals +6% Damage Bonus against enemies affected by at least one Status Effect.",
                demerit = "— (no demerit)",
                buyPrice = 1900,
                sellPrice = 950
            },
            new RelicDef
            {
                id = "R008",
                name = "Pale Lantern",
                category = RelicCategory.Exploration,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Exploration,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "On entering a dungeon, reveal the type of one hidden node still on the map path, if available.",
                designIntent = "Gives information for navigation and risk-taking.",
                description = "On entering a dungeon, reveal the type of one hidden node still on the map path, if available.",
                demerit = "— (no demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R009",
                name = "Soul-Tether Ring",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Once per battle, when direct damage would put a living character Down, that character survives at 1 HP.",
                designIntent = "Gives one second chance in a battle that is going badly.",
                description = "Once per battle, when direct damage would put a living character Down, that character survives at 1 HP.",
                demerit = "— (no demerit)",
                buyPrice = 0,
                sellPrice = 1800
            },
            new RelicDef
            {
                id = "R010",
                name = "Executioner's Bell",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "The party's first attack in each battle gains +10% Break Power.",
                designIntent = "Encourages planned battle openings and gives faster access to Break.",
                description = "The party's first attack in each battle gains +10% Break Power.",
                demerit = "— (no demerit)",
                buyPrice = 1700,
                sellPrice = 850
            },
            new RelicDef
            {
                id = "R011",
                name = "Hollow Knight's Buckle",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "While at least two party members are Down, all party members still active have 8% less All Damage Taken.",
                designIntent = "Supports the Brutal but Recoverable principle when the party is under heavy pressure.",
                description = "While at least two party members are Down, all party members still active have 8% less All Damage Taken.",
                demerit = "— (no demerit)",
                buyPrice = 1600,
                sellPrice = 800
            },
            new RelicDef
            {
                id = "R012",
                name = "Last King's Seal",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The party deals +5% Damage Bonus against Bosses.",
                designIntent = "Gives a small, consistent boost for the toughest encounters.",
                description = "The party deals +5% Damage Bonus against Bosses.",
                demerit = "— (no demerit)",
                buyPrice = 1900,
                sellPrice = 950
            },
            new RelicDef
            {
                id = "R013",
                name = "Dirge-Eater's Fang",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.CapPerBattle,
                gameplayEffect = "The character who lands a killing blow restores 1% Max MP. Can trigger at most twice per battle.",
                designIntent = "Turns finishing off enemies into a small resource recovery.",
                description = "The character who lands a killing blow restores 1% Max MP. Can trigger at most twice per battle.",
                demerit = "— (no demerit)",
                buyPrice = 1700,
                sellPrice = 850
            },
            new RelicDef
            {
                id = "R014",
                name = "Pallbearer's Gloves",
                category = RelicCategory.Utility,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Exploration,
                activationLimit = RelicActivationLimit.OncePerDungeon,
                gameplayEffect = "Negates the first avoidable Minor Chest Trap in the dungeon.",
                designIntent = "Removes one exploration risk without making every treasure safe.",
                description = "Negates the first avoidable Minor Chest Trap in the dungeon.",
                demerit = "— (no demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R015",
                name = "Veil of Dust",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The party's Status Effect Resistance against Burn, Poison and Bleed increases by 8 percentage points.",
                designIntent = "Helps the party handle damage-over-time and certain status pressure.",
                description = "The party's Status Effect Resistance against Burn, Poison and Bleed increases by 8 percentage points.",
                demerit = "— (no demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R016",
                name = "Royal Gravewax",
                category = RelicCategory.Survival,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Consumable,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "HP restored by HP consumables increases by 5% of the consumable's base value.",
                designIntent = "Makes healing supplies more efficient without making recovery free.",
                description = "HP restored by HP consumables increases by 5% of the consumable's base value.",
                demerit = "— (no demerit)",
                buyPrice = 1700,
                sellPrice = 850
            },
            new RelicDef
            {
                id = "R017",
                name = "Oathstone of the Fallen",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "If a character has taken no damage since the start of their previous turn, their next direct-damage attack gains +5% Damage Bonus.",
                designIntent = "Rewards turn planning and protection against incoming damage.",
                description = "If a character has taken no damage since the start of their previous turn, their next direct-damage attack gains +5% Damage Bonus.",
                demerit = "— (no demerit)",
                buyPrice = 1600,
                sellPrice = 800
            },
            new RelicDef
            {
                id = "R018",
                name = "Whispering Reliquary",
                category = RelicCategory.Exploration,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Event,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "At events tagged relic or cursed_reward, show one extra risk indicator if risk data is available.",
                designIntent = "Reinforces Knowledge Is Power and helps players weigh trade-offs.",
                description = "At events tagged relic or cursed_reward, show one extra risk indicator if risk data is available.",
                demerit = "— (no demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R019",
                name = "Black-Iron Rosary",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Qte,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "After a character lands a Perfect Defensive QTE, they gain a Shield equal to 1.5% Max HP.",
                designIntent = "Gives a small reward for good defensive execution.",
                description = "After a character lands a Perfect Defensive QTE, they gain a Shield equal to 1.5% Max HP.",
                demerit = "— (no demerit)",
                buyPrice = 1600,
                sellPrice = 800
            },
            new RelicDef
            {
                id = "R020",
                name = "Sovereign's Last Breath",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Boss,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "The first time a Boss drops to 25% HP or less, each living party member restores 2% Max MP.",
                designIntent = "Gives a little room to use skills in a boss's final phase.",
                description = "The first time a Boss drops to 25% HP or less, each living party member restores 2% Max MP.",
                demerit = "— (no demerit)",
                buyPrice = 0,
                sellPrice = 1500
            },
            new RelicDef
            {
                id = "R021",
                name = "Coffin Nail",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The party deals +7% Damage Bonus against Broken enemies.",
                designIntent = "Encourages the party to exploit the attack window after a Break.",
                description = "The party deals +7% Damage Bonus against Broken enemies.",
                demerit = "— (no demerit)",
                buyPrice = 1900,
                sellPrice = 950
            },
            new RelicDef
            {
                id = "R022",
                name = "Zoroa's Dull Crown",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.StackTrigger,
                gameplayEffect = "After the party defeats three enemies in one battle, the party gains +3% All Damage Bonus until the battle ends.",
                designIntent = "Builds momentum in long encounters and keeps Zoroa's identity.",
                description = "After the party defeats three enemies in one battle, the party gains +3% All Damage Bonus until the battle ends.",
                demerit = "— (no demerit)",
                buyPrice = 1500,
                sellPrice = 750
            },
            new RelicDef
            {
                id = "R023",
                name = "Crimson Testament",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The whole party gains +18% Damage Bonus on all direct-damage attacks.",
                designIntent = "A high-risk offensive pick that speeds up every encounter.",
                description = "The whole party gains +18% Damage Bonus on all direct-damage attacks.",
                demerit = "The whole party has +12% All Damage Taken.",
                buyPrice = 2300,
                sellPrice = 1150
            },
            new RelicDef
            {
                id = "R024",
                name = "Hollow Saint's Aegis",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The whole party has 22% less All Damage Taken.",
                designIntent = "Builds a very durable party at the cost of a slower damage tempo.",
                description = "The whole party has 22% less All Damage Taken.",
                demerit = "The whole party gets 15% less Damage Bonus.",
                buyPrice = 2300,
                sellPrice = 1150
            },
            new RelicDef
            {
                id = "R025",
                name = "Arcanum Unbound",
                category = RelicCategory.Hybrid,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Skills that deal direct damage gain +25% Damage Bonus.",
                designIntent = "Encourages builds focused on skills and burst damage.",
                description = "Skills that deal direct damage gain +25% Damage Bonus.",
                demerit = "MP cost of all Skills increases by 20%.",
                buyPrice = 0,
                sellPrice = 2000
            },
            new RelicDef
            {
                id = "R026",
                name = "The Empty Chalice",
                category = RelicCategory.Hybrid,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "MP cost of all Skills is reduced by 30%.",
                designIntent = "Lets the party use skills more often and extends its staying power in combat.",
                description = "MP cost of all Skills is reduced by 30%.",
                demerit = "HP restored by Skills and HP Consumables is reduced by 25%; does not affect Temple Recovery, Relic healing effects, or Racial Feat healing that has special rules.",
                buyPrice = 2200,
                sellPrice = 1100
            },
            new RelicDef
            {
                id = "R027",
                name = "Breaker’s Due",
                category = RelicCategory.Break,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "The whole party gains +30% Break Power.",
                designIntent = "Builds a party focused on breaking enemy defenses and opening Broken windows faster.",
                description = "The whole party gains +30% Break Power.",
                demerit = "The whole party gets 12% less Damage Bonus.",
                buyPrice = 2000,
                sellPrice = 1000
            },
            new RelicDef
            {
                id = "R028",
                name = "Wound-Crowned Idol",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "While a character is at 50% HP or less, they gain +25% Damage Bonus.",
                designIntent = "Encourages a risky playstyle that turns low HP into big damage.",
                description = "While a character is at 50% HP or less, they gain +25% Damage Bonus.",
                demerit = "Max HP of all party members is reduced by 12% while the relic is owned.",
                buyPrice = 0,
                sellPrice = 2000
            },
            new RelicDef
            {
                id = "R029",
                name = "Bell of the Last Stand",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "While at least two party members are Down, all party members still active gain +25% Damage Bonus.",
                designIntent = "Gives an offensive comeback chance when the party is on the brink of defeat.",
                description = "While at least two party members are Down, all party members still active gain +25% Damage Bonus.",
                demerit = "While the effect is active, party members still active have +15% All Damage Taken.",
                buyPrice = 0,
                sellPrice = 1800
            },
            new RelicDef
            {
                id = "R030",
                name = "The Butcher's Ledger",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.StackTrigger,
                gameplayEffect = "Each time the party defeats an enemy, the whole party gains an extra 6% Damage Bonus. Stacks up to 4 times and lasts until the battle ends.",
                designIntent = "Turns multi-enemy encounters into ever-growing offensive momentum.",
                description = "Each time the party defeats an enemy, the whole party gains an extra 6% Damage Bonus. Stacks up to 4 times and lasts until the battle ends.",
                demerit = "Each stack also increases the party's All Damage Taken by 4% (up to +16% at 4 stacks).",
                buyPrice = 0,
                sellPrice = 1800
            },
            new RelicDef
            {
                id = "R031",
                name = "Pilgrim's Burden",
                category = RelicCategory.Economy,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Reward,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "EXP earned by the party during the dungeon increases by 15%.",
                designIntent = "Speeds up character growth at the cost of spoils.",
                description = "EXP earned by the party during the dungeon increases by 15%.",
                demerit = "Spoils earned during the dungeon are reduced by 15%.",
                buyPrice = 2800,
                sellPrice = 1400
            },
            new RelicDef
            {
                id = "R032",
                name = "Merchant's Burden",
                category = RelicCategory.Economy,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Reward,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Spoils earned by the party during the dungeon increase by 15%.",
                designIntent = "Strengthens the expedition economy and buying power for the run, at the cost of character growth.",
                description = "Spoils earned by the party during the dungeon increase by 15%.",
                demerit = "EXP earned by the party during the dungeon is reduced by 15%.",
                buyPrice = 2800,
                sellPrice = 1400
            },
        }, r => r.id);

        static void RegisterLayer1Relics()
        {
            foreach (var kv in Layer1Relics)
            {
                Relics[kv.Key] = kv.Value;
            }
        }
    }
}
