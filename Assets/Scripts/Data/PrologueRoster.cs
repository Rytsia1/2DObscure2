using static Obscure.Dungeon1Roster;

// Tutorial Prologue roster. Source of truth: Tutorial_20Stage.xlsx (Enemy Roster, Skill Design, Boss & Balance).
// Fixed stats: the Prologue never applies dungeon scaling (it runs as act 1, where scaling is x1.0).
// Affinities, AI cooldowns and the signature gate are new [B] starting values: Tutorial_Prologue_Design.md §3, tuning §9.
namespace Obscure
{
    public static class PrologueRoster
    {
        static readonly float[] CollapseAccelerando = { 0.350f, 0.298f, 0.280f }; // Boss minimum 280 ms

        // Stats(HP, MP, ATK, DEF, SPD, MATK) + MDEF.
        public static readonly EnemyDef[] Enemies =
        {
            Enemy("riftling", "Riftling", "Riftling", "RFT", Tier.Normal, "Creature", "Physical",
                new Stats(80, 0, 14, 4, 13, 0), 4, Art.Skeleton,
                "A small creature formed from rift energy. Fast, with simple single-target strikes.",
                Move("Rift Claw", Element.Physical, 14)),

            Enemy("shard_wisp", "Shard Wisp", "Wisp", "SWP", Tier.Normal, "Spirit", "Magic Attacker",
                new Stats(70, 40, 6, 3, 12, 16), 5, Art.Skeleton,
                "A spirit of shattered magic. Its crystal body breaks easily.",
                new EnemyAttack { name = "Shard Bolt", element = Element.Lightning, hitDamage = new[] { 16 }, mpCost = 8, aiPriority = 1 },
                BasicAttack(6)),

            Enemy("veil_stalker", "Veil Stalker", "Stalker", "VST", Tier.Normal, "Aberration", "Disruptor",
                new Stats(95, 35, 12, 5, 10, 12), 5, Art.Skeleton,
                "It hides behind distorted space and unsettles the mind.",
                new EnemyAttack { name = "Dread Pulse", hits = 0, power = 0f, element = Element.Shadow, mpCost = 8, aiPriority = 2, cooldown = 2,
                    apply = new[] { Chance(StatusId.Fear, 0.20f) } }, // status-only: telegraphed from the start of the round
                Move("Veil Strike", Element.Physical, 12)),

            Enemy("aetherion", "Aetherion, the Threshold Guardian", "Aetherion", "AET", Tier.Boss, "Guardian", "Controller",
                new Stats(600, 120, 20, 8, 12, 20), 8, Art.Warden,
                "Guardian of the boundary between Felna and The World's Between. It tests, it does not hate.",
                new EnemyAttack { name = "Threshold Collapse", element = Element.Physical, hitDamage = new[] { 14, 14, 16, 22 }, intervals = CollapseAccelerando,
                    heavyHit = 3, telegraph = true, mpCost = 25, aiPriority = 4, cooldown = 3, initialCooldown = 2,
                    aiBreakLeftBelow = 0.40f, aiBreakGateRound = 5 },
                new EnemyAttack { name = "Rift Pulse", element = Element.Shadow, hitDamage = new[] { 11 }, allTargets = true, phase = 2,
                    mpCost = 20, aiPriority = 3, cooldown = 2, minTargets = 2 }, // the one new Phase 2 attack; shared QTE
                new EnemyAttack { name = "Aether Bolt", element = Element.Lightning, hitDamage = new[] { 20 }, mpCost = 10, aiPriority = 2, cooldown = 1 },
                Move("Dimensional Strike", Element.Physical, 20)),
        };

        public static readonly EncounterDef[] Encounters =
        {
            new EncounterDef { id = "tut_n1", name = "First Combat", tier = Tier.Normal, focus = "Basic combat; turn order; Defensive QTE",
                enemies = new[] { "riftling", "riftling" } },
            new EncounterDef { id = "tut_n2", name = "Mixed Encounter", tier = Tier.Normal, focus = "Elemental damage; status effect; target priority",
                enemies = new[] { "ossuary_acolyte", "veil_stalker" } },
            new EncounterDef { id = "tut_boss", name = "Aetherion", tier = Tier.Boss, focus = "Break Gauge; telegraph; Heavy; Defensive QTE; resources",
                enemies = new[] { "aetherion" }, music = MusicIds.Aetherion },
        };

        static PrologueRoster()
        {
            Enemies[0].instinct = InstinctKind.Predator;
            Enemies[1].instinct = InstinctKind.Skittish;
            Enemies[2].instinct = InstinctKind.Predator;
            Enemies[3].instinct = InstinctKind.Guardian;
            Enemies[1].affinities = new Affinities { weak = new[] { Element.Physical }, resist = new[] { Element.Lightning } };
            Enemies[2].affinities = new Affinities { weak = new[] { Element.Holy, Element.Fire }, resist = new[] { Element.Shadow } };
            Enemies[3].phase2At = 0.5f;
            Enemies[3].phase2Text = "The Threshold buckles - Aetherion opens the rift! (Phase 2)";
        }
    }
}
