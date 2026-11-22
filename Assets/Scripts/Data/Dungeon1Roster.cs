using System.Linq;

// Dungeon 1 enemy roster, skills and encounters.
// Source of truth: Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx (Enemy Roster, Skill Database, Encounter Plan,
// Zoroa Boss Phases, Elite Design Lock).
// All stats are Dungeon 1 base values; Unit.ScaleEnemyStats applies dungeon/tier scaling exactly once.
namespace Obscure
{
    public static class Dungeon1Roster
    {
        // Gold is not part of the roster workbook; these mirror the legacy per-tier placeholders.
        const int NormalGold = 8, EliteGold = 30, BossGold = 100;

        // ponytail: these multipliers diverge from Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx,
        // and the workbook should be updated if they stick.
        // D1 Elite stat multiplier (HP, ATK, MATK): posture-B starting value (untested).
        // Pass test: the median Elite fight takes about 4-6 rounds. If it fails: too long -> x1.15.
        public const float EliteStatMult = 1.25f;
        // D1 Boss stat multiplier: posture-B starting value (untested).
        // Pass test: Zoroa takes about 10-14 rounds for a level 10-11 party, and at least one hero drops if QTEs are sloppy.
        // If it fails: adjust the HP multiplier first, then ATK.
        public const float BossHpMult = 1.30f;
        public const float BossAtkMult = 1.20f;

        // Per-move breakPower (flat, added per target hit): posture-B starting values (untested).
        // Pass test: Final Reaping with all QTEs missed breaks a 60-Poise hero; with all Good QTEs it does not.
        // If it fails: adjust the Final Reaping value first.
        public const int FinalReapingP1BreakPower = 20;      // posture-B starting value (untested)
        public const int FinalReapingP2BreakPower = 25;      // posture-B starting value (untested)
        public const int SoulstormBreakPower = 10;           // posture-B starting value (untested)
        public const int TripleFeintBreakPower = 8;          // posture-B starting value (untested)
        public const int FuneralChorusBreakPower = 8;        // posture-B starting value (untested)
        public const int StageBossHeavyBreakPower = 15;      // posture-B starting value (untested)

        static readonly float[] ReaperAccelerando = { 0.350f, 0.310f, 0.310f }; // Elite minimum 310 ms
        static readonly float[] ZoroaAccelerando = { 0.350f, 0.298f, 0.280f };  // Boss minimum 280 ms

        internal static StatusApply Chance(StatusId id, float chance) => new StatusApply { id = id, chance = chance };

        public static int ScaleD1Stat(int value, float mult) => (int)System.Math.Round(value * mult);

        // Stats(HP, MP, ATK, DEF, SPD, MATK) + MDEF.
        internal static EnemyDef Enemy(string id, string name, string shortName, string tag, Tier tier, string type, string role,
            Stats stats, int mdef, string[] sprite, string description, params EnemyAttack[] attacks) =>
            Enemy(id, name, shortName, tag, tier, type, role, stats, mdef, sprite, description, applyD1TierMult: false, attacks);

        static EnemyDef D1Enemy(string id, string name, string shortName, string tag, Tier tier, string type, string role,
            Stats stats, int mdef, string[] sprite, string description, params EnemyAttack[] attacks) =>
            Enemy(id, name, shortName, tag, tier, type, role, stats, mdef, sprite, description, applyD1TierMult: true, attacks);

        static EnemyDef Enemy(string id, string name, string shortName, string tag, Tier tier, string type, string role,
            Stats stats, int mdef, string[] sprite, string description, bool applyD1TierMult, params EnemyAttack[] attacks)
        {
            // Authored damage -> coefficient on the D1 base ATK (Physical) or MATK (Elemental).
            foreach (var a in attacks.Where(a => a.hitDamage != null))
            {
                float stat = a.UsesMagic ? stats.mag : stats.atk;
                a.hits = a.hitDamage.Length;
                a.hitPower = a.hitDamage.Select(dmg => dmg / stat).ToArray();
                a.power = a.hitPower.Length > 0 ? a.hitPower[0] : 0f;
            }
            if (applyD1TierMult)
            {
                if (tier == Tier.Elite)
                {
                    stats = new Stats(
                        ScaleD1Stat(stats.maxHp, EliteStatMult),
                        stats.maxMp,
                        ScaleD1Stat(stats.atk, EliteStatMult),
                        stats.def,
                        stats.spd,
                        ScaleD1Stat(stats.mag, EliteStatMult));
                }
                else if (tier == Tier.Boss)
                {
                    stats = new Stats(
                        ScaleD1Stat(stats.maxHp, BossHpMult),
                        stats.maxMp,
                        ScaleD1Stat(stats.atk, BossAtkMult),
                        stats.def,
                        stats.spd,
                        ScaleD1Stat(stats.mag, BossAtkMult));
                }
            }
            var d = new EnemyDef
            {
                id = id, name = name, shortName = shortName, tag = tag, tier = tier, type = type, role = role,
                ai = AiKind.Priority, stats = stats, magicDef = mdef,
                maxBreakGauge = tier == Tier.Boss ? 200 : 100, // not scaled by dungeon
                gold = tier == Tier.Boss ? BossGold : tier == Tier.Elite ? EliteGold : NormalGold,
                targetRule = tier == Tier.Elite ? TargetRule.LowestHp : TargetRule.Random,
                sprite = sprite, description = description, attacks = attacks,
            };
            return d;
        }

        internal static EnemyAttack Move(string name, Element element, params int[] damage) =>
            new EnemyAttack { name = name, element = element, hitDamage = damage, aiPriority = 1 };

        // Generic 0-MP Basic Attack for roster enemies whose workbook kit has only MP Skills
        // (placeholder: 1 Physical hit at 100% of base ATK; not in workbook v1.7).
        internal static EnemyAttack BasicAttack(int atk) =>
            new EnemyAttack { name = "Basic Attack", element = Element.Physical, hitDamage = new[] { atk }, aiPriority = 0 };

        // Reusable MP Restoration Skill. Cost 0, self, restores 50% Max MP (capped), cooldown 4 turns, no damage, no QTE.
        // Assigned per enemy; usage is configured per instance (priority, MP threshold, cooldowns).
        public static EnemyAttack ArcaneCharge(int priority, float useBelowMpFraction = 0.5f, int initialCooldown = 0, float unlessAllyHpBelow = 0f) => new EnemyAttack
        {
            name = "Arcane Charge", self = true, hits = 0, power = 0f, restoreMp = 0.50f, mpCost = 0, cooldown = 4,
            aiPriority = priority, aiMpBelow = useBelowMpFraction, initialCooldown = initialCooldown, aiUnlessAllyHpBelow = unlessAllyHpBelow,
        };

        public static readonly EnemyDef[] Enemies =
        {
            D1Enemy("hollow_squire", "Hollow Squire", "Squire", "HSQ", Tier.Normal, "Undead", "Vanguard",
                new Stats(145, 35, 18, 8, 9, 8), 6, Art.Skeleton,
                "A royal soldier risen without a soul. Presses one target with plain strikes.",
                Move("Rusty Cleave", Element.Physical, 18)),

            D1Enemy("gravebound_archer", "Gravebound Archer", "Archer", "GAR", Tier.Normal, "Undead", "Ranged",
                new Stats(112, 45, 20, 5, 13, 10), 7, Art.Skeleton,
                "A tomb archer guarding the ruined halls. Aims at poorly armoured targets.",
                Move("Grave Arrow", Element.Physical, 20)),

            D1Enemy("cinder_wisp", "Cinder Wisp", "Wisp", "CWS", Tier.Normal, "Spirit", "Elemental",
                new Stats(105, 70, 8, 4, 14, 22), 8, Art.Skeleton,
                "An ember spirit drifting through the rubble. Burns with fire.",
                new EnemyAttack { name = "Ember Bolt", element = Element.Fire, hitDamage = new[] { 22 }, mpCost = 8, aiPriority = 1,
                    apply = new[] { Chance(StatusId.Burn, 0.20f) } },
                BasicAttack(8)),

            D1Enemy("soulbound_hound", "Soulbound Hound", "Hound", "SBH", Tier.Normal, "Beast", "Disruptor",
                new Stats(155, 40, 17, 7, 16, 9), 6, Art.Skeleton,
                "A guardian beast bound to lost souls. Fast bites and light status pressure.",
                new EnemyAttack { name = "Soul Snarl", element = Element.Shadow, hitDamage = new[] { 12 }, mpCost = 10, aiPriority = 2, cooldown = 2,
                    apply = new[] { Chance(StatusId.Fear, 0.20f) } },
                Move("Rending Bite", Element.Physical, 17)),

            D1Enemy("possessed_royal_guard", "Possessed Royal Guard", "Royal Guard", "PRG", Tier.Normal, "Undead", "Bruiser",
                new Stats(190, 30, 23, 12, 7, 7), 9, Art.Skeleton,
                "A royal guard whose body is ruled by a lingering spirit. Durable and steady.",
                Move("Royal Halberd", Element.Physical, 23)), // heavy-looking, but no Heavy marker

            D1Enemy("crypt_leech", "Crypt Leech", "Leech", "CLE", Tier.Normal, "Aberration", "Attrition",
                new Stats(125, 55, 15, 5, 11, 13), 7, Art.Skeleton,
                "A crypt parasite that drinks vitality from what it strikes.",
                new EnemyAttack { name = "Vital Siphon", element = Element.Shadow, hitDamage = new[] { 15 }, mpCost = 7, aiPriority = 1, drain = 0.30f },
                BasicAttack(15)),

            D1Enemy("forgotten_militia", "Forgotten Militia", "Militia", "FML", Tier.Normal, "Undead", "Skirmisher",
                new Stats(135, 35, 16, 6, 12, 8), 6, Art.Skeleton,
                "A militia remnant still following its last order. Strikes in two steps.",
                Move("Two-Step Slash", Element.Physical, 10, 10)), // Steady 350 ms, one Defensive QTE per hit

            D1Enemy("soul_reaper", "Soul Reaper", "Soul Reaper", "SRP", Tier.Elite, "Wraith", "Executioner",
                new Stats(430, 130, 32, 13, 17, 29), 14, Art.CursedKnight,
                "An elite soul harvester. Fast, with heavy physical and magical pressure.",
                new EnemyAttack { name = "Accelerating Rend", element = Element.Physical, hitDamage = new[] { 15, 15, 15, 15 }, intervals = ReaperAccelerando,
                    breakPower = StageBossHeavyBreakPower, mpCost = 16, aiPriority = 3, cooldown = 1, apply = new[] { Chance(StatusId.Bleed, 0.25f) } },
                new EnemyAttack { name = "Soul Brand", element = Element.Shadow, hitDamage = new[] { 20 }, mpCost = 20, aiPriority = 2, cooldown = 2,
                    apply = new[] { Chance(StatusId.Despair, 0.20f) } },
                Move("Harvesting Arc", Element.Physical, 27),
                ArcaneCharge(priority: 4)),

            D1Enemy("royal_ossuary_knight", "Royal Ossuary Knight", "Ossuary Knight", "ROK", Tier.Elite, "Undead", "Defender",
                new Stats(520, 90, 36, 22, 9, 18), 18, Art.CursedKnight,
                "An elite bone knight guarding the gate. Charges a Heavy blow that Break can interrupt.",
                new EnemyAttack { name = "Graveward Charge", element = Element.Physical, hitDamage = new[] { 38 }, heavyHit = 0, telegraph = true,
                    breakPower = StageBossHeavyBreakPower, mpCost = 18, aiPriority = 2, cooldown = 1, initialCooldown = 1 },
                Move("Ossuary Slam", Element.Physical, 32),
                ArcaneCharge(priority: 3)),

            // v1.9 FINAL: five random-pool Elites (Elite Design Lock sheet). Art: Resources/Enemies/<id>_* (Tools/PixelArt/enemies.js).
            D1Enemy("reliquary_confessor", "Reliquary Confessor", "Confessor", "RCF", Tier.Elite, "Undead", "Cleric",
                new Stats(380, 150, 15, 11, 12, 30), 20, Art.CursedKnight,
                "A former royal priest guarding a tainted relic. Mends its allies and hoards MP with Arcane Charge.",
                new EnemyAttack { name = "Mending Litany", hits = 0, power = 0f, healAlly = 0.15f, aiAllyHpBelow = 0.65f,
                    mpCost = 24, cooldown = 3, aiPriority = 3 },
                ArcaneCharge(priority: 2, useBelowMpFraction: 0.30f, unlessAllyHpBelow: 0.40f),
                Move("Reliquary Staff", Element.Physical, 15)),

            D1Enemy("graveglass_arcanist", "Graveglass Arcanist", "Arcanist", "GGA", Tier.Elite, "Wraith", "Arcane Caster",
                new Stats(360, 180, 12, 9, 16, 38), 17, Art.CursedKnight,
                "A royal sorcerer's ghost fused with stained-glass shards. Casts Shadow lances and recharges its MP.",
                ArcaneCharge(priority: 3, useBelowMpFraction: 0.25f),
                new EnemyAttack { name = "Umbral Lance", element = Element.Shadow, hitDamage = new[] { 38 }, mpCost = 22, aiPriority = 2 },
                Move("Glass Shard", Element.Physical, 12)),

            D1Enemy("crownless_duelist", "Crownless Duelist", "Duelist", "CDU", Tier.Elite, "Undead", "Skirmisher",
                new Stats(410, 100, 36, 13, 18, 12), 12, Art.CursedKnight,
                "A palace swordsman still bound by the royal dueling code. Presses one target with three quick cuts.",
                new EnemyAttack { name = "Triple Feint", element = Element.Physical, hitDamage = new[] { 12, 12, 12 }, breakPower = TripleFeintBreakPower, mpCost = 18, aiPriority = 1 }, // Steady 350 ms
                Move("Crownless Cut", Element.Physical, 36)),

            D1Enemy("tombstone_colossus", "Tombstone Colossus", "Colossus", "TSC", Tier.Elite, "Undead", "Construct",
                new Stats(620, 70, 38, 24, 7, 9), 16, Art.CursedKnight,
                "A giant of gravestones and royal armour. Extremely sturdy; Gravefall crushes the party's poise.",
                new EnemyAttack { name = "Gravefall", element = Element.Physical, hitDamage = new[] { 38 }, breakPower = 30,
                    mpCost = 18, cooldown = 3, aiPriority = 1 }, // no Heavy marker
                Move("Monument Fist", Element.Physical, 38)),

            D1Enemy("mourning_bell_wraith", "Mourning Bell Wraith", "Bell Wraith", "MBW", Tier.Elite, "Wraith", "Controller",
                new Stats(400, 145, 15, 12, 14, 33), 22, Art.CursedKnight,
                "The spirit of a funeral bell. Its toll disturbs the soul and washes the whole party in Shadow.",
                new EnemyAttack { name = "Funeral Chorus", element = Element.Shadow, hitDamage = new[] { 20 }, allTargets = true, telegraph = true,
                    breakPower = FuneralChorusBreakPower, mpCost = 32, cooldown = 3, minTargets = 3, aiPriority = 2 }, // shared QTE, telegraphed 1 turn prior
                new EnemyAttack { name = "Tolling Note", element = Element.Shadow, hitDamage = new[] { 34 }, mpCost = 20, aiPriority = 1 },
                Move("Echo Claw", Element.Physical, 15)),

            D1Enemy("zoroa_p1", "Zoroa, Harvester of Souls", "Zoroa", "ZOR", Tier.Boss, "Soul Entity", "Controller",
                new Stats(1840, 600, 38, 20, 14, 42), 22, Art.Warden,
                "Zoroa gathers the souls of Felna and tears them from the living. Health bar 1/2.",
                new EnemyAttack { name = "Final Reaping", element = Element.Physical, hitDamage = new[] { 30, 30, 30, 45 }, intervals = ZoroaAccelerando,
                    heavyHit = 3, telegraph = true, breakPower = FinalReapingP1BreakPower, mpCost = 35, aiPriority = 4, cooldown = 3, initialCooldown = 2 },
                // ponytail: Soulbind is no longer telegraphed (instant status move), which diverges from Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx.
                new EnemyAttack { name = "Soulbind", hits = 0, power = 0f, element = Element.Shadow, mpCost = 24, aiPriority = 3, cooldown = 3,
                    apply = new[] { Chance(StatusId.Silence, 0.20f) } },
                new EnemyAttack { name = "Soul Lance", element = Element.Shadow, hitDamage = new[] { 42 }, mpCost = 18, aiPriority = 2, cooldown = 1 },
                Move("Soul Rend", Element.Physical, 38),
                ArcaneCharge(priority: 5)),

            D1Enemy("zoroa_p2", "Zoroa, Harvester of Souls", "Zoroa", "ZOR", Tier.Boss, "Soul Entity", "Controller",
                new Stats(1840, 850, 43, 18, 17, 50), 24, Art.Warden,
                "With the first bar shattered, Zoroa rises with a fresh one. Health bar 2/2.",
                new EnemyAttack { name = "Final Reaping", element = Element.Physical, hitDamage = new[] { 34, 34, 34, 50 }, intervals = ZoroaAccelerando,
                    heavyHit = 3, telegraph = true, breakPower = FinalReapingP2BreakPower, mpCost = 40, aiPriority = 4, cooldown = 3, initialCooldown = 1 },
                new EnemyAttack { name = "Soulstorm", element = Element.Shadow, hitDamage = new[] { 24 }, allTargets = true, telegraph = true,
                    breakPower = SoulstormBreakPower, mpCost = 45, aiPriority = 3, cooldown = 3, minTargets = 2 }, // the one new Phase 2 attack; shared QTE
                // ponytail: Soulbind is no longer telegraphed (instant status move), which diverges from Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx.
                new EnemyAttack { name = "Soulbind", hits = 0, power = 0f, element = Element.Shadow, mpCost = 28, aiPriority = 2, cooldown = 3,
                    apply = new[] { Chance(StatusId.Silence, 0.25f) } },
                new EnemyAttack { name = "Soul Lance", element = Element.Shadow, hitDamage = new[] { 50 }, mpCost = 22, aiPriority = 1, cooldown = 1 },
                new EnemyAttack { name = "Soul Rend", element = Element.Physical, hitDamage = new[] { 43 }, aiPriority = 0 },
                ArcaneCharge(priority: 5)),
        };

        static Dungeon1Roster()
        {
            var p1 = Enemies.First(e => e.id == "zoroa_p1");
            p1.nextPhase = "zoroa_p2";
            p1.phase2Text = "Zoroa's first soul-shell shatters - the Harvester rises anew! (Phase 2)";
            // Confirmed targeting: Elites -> lowest current HP, Archer -> lowest DEF, everyone else random.
            Enemies.First(e => e.id == "gravebound_archer").targetRule = TargetRule.LowestDef;
        }

        public static readonly EncounterDef[] Encounters =
        {
            new EncounterDef { id = "d1_e1", name = "Ruined Gate", tier = Tier.Normal, minFloor = 0, focus = "Single-target physical; Defensive QTE introduction",
                enemies = new[] { "hollow_squire", "hollow_squire" } },
            new EncounterDef { id = "d1_e2", name = "Fallen Watch", tier = Tier.Normal, minFloor = 0, focus = "Target priority; ranged pressure",
                enemies = new[] { "gravebound_archer", "hollow_squire" } },
            new EncounterDef { id = "d1_e3", name = "Ash Courtyard", tier = Tier.Normal, minFloor = 1, focus = "Elemental damage, Burn, multi-hit Defensive QTE",
                enemies = new[] { "cinder_wisp", "forgotten_militia" } },
            new EncounterDef { id = "d1_e4", name = "Kennel of Lost Souls", tier = Tier.Normal, minFloor = 1, focus = "Status pressure; enemy sustain",
                enemies = new[] { "soulbound_hound", "crypt_leech" } },
            new EncounterDef { id = "d1_e5", name = "Royal Remnant", tier = Tier.Normal, minFloor = 2, focus = "Enemy durability; target selection",
                enemies = new[] { "possessed_royal_guard", "hollow_squire", "hollow_squire" } },
            new EncounterDef { id = "d1_e6", name = "Reaper's Passage", tier = Tier.Elite, minFloor = 2, focus = "Stage 1 Boss: Soul Reaper with 3 allies; Accelerando",
                enemies = new[] { "soul_reaper", "forgotten_militia", "soulbound_hound", "crypt_leech" }, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e7", name = "Ossuary Threshold", tier = Tier.Elite, minFloor = 2, focus = "Stage 2 Boss: Royal Ossuary Knight with 3 allies; Heavy & Break interrupt",
                enemies = new[] { "royal_ossuary_knight", "possessed_royal_guard", "gravebound_archer", "hollow_squire" }, music = MusicIds.Battle2 },
            // v1.9 FINAL random Elite pool (sheet E6-E10). d1_e6 / d1_e7 above are the special Miniboss nodes (stage bosses).
            new EncounterDef { id = "d1_e8", name = "Reliquary Chamber", tier = Tier.Elite, minFloor = 2, focus = "Healing / sustain support; focus the Confessor",
                enemies = new[] { "reliquary_confessor", "gravebound_archer" }, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e9", name = "Shattered Chapel", tier = Tier.Elite, minFloor = 2, focus = "Magic damage / MP management",
                enemies = new[] { "graveglass_arcanist", "cinder_wisp" }, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e10", name = "Duelist's Gallery", tier = Tier.Elite, minFloor = 2, focus = "Physical multi-hit pressure",
                enemies = new[] { "crownless_duelist", "forgotten_militia" }, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e11", name = "Fallen Mausoleum", tier = Tier.Elite, minFloor = 2, focus = "Durability / Break pressure",
                enemies = new[] { "tombstone_colossus", "hollow_squire" }, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e12", name = "Mourning Bell Hall", tier = Tier.Elite, minFloor = 2, focus = "AoE / control pressure; telegraphed Funeral Chorus",
                enemies = new[] { "mourning_bell_wraith", "soulbound_hound" }, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_boss", name = "Zoroa", tier = Tier.Boss, focus = "Signature multi-hit, resources, Break timing, Phase 2 escalation",
                enemies = new[] { "zoroa_p1" }, music = MusicIds.ZoroaP1 }, // Phase 2 replaces Phase 1; never spawned alongside it
        };
    }
}
