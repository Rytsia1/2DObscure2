using System;
using System.Collections.Generic;
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
        // Zoroa damage multiplier (per hit, both turns): posture-B starting value (untested), replacing the x1.20 BossAtkMult for Zoroa only
        // (0.85 for the double turn, minus 0.05 to make room for summons from Part 2).
        // Pass test: fight lasts 10-14 rounds; with Good QTEs, the party ends with 1+ hero above 30% HP; with sloppy QTEs, at least 2 heroes drop.
        // If it fails: too lethal -> 0.70; still easy -> 0.90.
        public const float ZoroaAtkMult = 0.80f;

        // Zoroa double-turn and Harvest Call summon knobs: posture-B starting values (untested).
        // If P1 feels unfair, set P1 = 1 and keep the double turn as the Phase 2 escalation.
        public const int ZoroaTurnsPerRound = 2;             // posture-B starting value (untested)
        public const int HarvestCallSummonCount = 2;         // posture-B starting value (untested)
        public const int HarvestCallCooldown = 5;            // posture-B starting value (untested, user-set 5 rounds)
        // Pass test: the party gets 2 rounds of pure Zoroa before the first summon. If it fails: too early -> 3.
        public const int HarvestCallInitialCooldown = 2;     // posture-B starting value (untested)
        // Pass test: Arcane Charge still at most about once per phase. If it fails: lower the cost, or raise Zoroa MP by 25%.
        public const int HarvestCallP1MpCost = 30;           // posture-B starting value (untested)
        public const int HarvestCallP2MpCost = 35;           // posture-B starting value (untested)
        // Pass test: killing a common summon takes about 1-2 party turns, and an Elite summon about 3-4.
        // If it fails: too tanky -> 0.60f; ignored freely -> 0.80f.
        public const float SummonHpMult = 0.70f;             // posture-B starting value (untested)
        public const float HarvestCallEliteChance = 0.20f;   // posture-B starting value (untested, user-set 20%)

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

        // On-Defeat Transformation & Enhanced Common Variant pilot constants: posture-B starting values (untested).
        // Pass test: 1-3 transformations per D1 run on average (200-map sim), and playtesters call it "a surprise", not "routine".
        // If it fails: Routine -> 0.05f; Never noticed -> 0.15f.
        public const float PilotTransformChance = 0.10f; // posture-B starting value (untested)

        // Enhanced Common HP / stat multipliers: posture-B starting values (untested).
        // Pass test: a transform adds <= 2 rounds to the encounter on average (level-matched sim party);
        // an Enhanced turn deals <= 1.5x the base form's average turn.
        // If it fails: lower the HP multiplier first, then lower the per-hit damage of the alternative pattern.
        public static readonly VariantStatMult SquireEnhancedMult = new VariantStatMult(hp: 1.60f, def: 1.30f, atk: 1.15f); // posture-B starting value (untested)
        public static readonly VariantStatMult HoundEnhancedMult  = new VariantStatMult(hp: 1.40f, spd: 1.25f, atk: 1.20f); // posture-B starting value (untested)
        public static readonly VariantStatMult WispEnhancedMult   = new VariantStatMult(hp: 1.40f, matk: 1.25f, mp: 1.50f); // posture-B starting value (untested)

        // Approved Accelerando exception for Soulbound Hound (Enhanced) alternative pattern (Variant Spec §6.6 v1.1):
        // posture-B starting value (untested). Pass test: Good/Perfect rate within 10 pp of Soul Reaper's Accelerando among playtesters.
        // If it fails: slow to { 0.350f, 0.330f }.
        public static readonly float[] HoundEnhancedAccelerando = { 0.350f, 0.310f }; // posture-B starting value (untested, >= 310 ms Elite floor)

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
                    float atkMult = id.StartsWith("zoroa_") ? ZoroaAtkMult : BossAtkMult;
                    stats = new Stats(
                        ScaleD1Stat(stats.maxHp, BossHpMult),
                        stats.maxMp,
                        ScaleD1Stat(stats.atk, atkMult),
                        stats.def,
                        stats.spd,
                        ScaleD1Stat(stats.mag, atkMult));
                }
            }
            var d = new EnemyDef
            {
                id = id, name = name, shortName = shortName, tag = tag, tier = tier, type = type, role = role,
                ai = AiKind.Priority, stats = stats, magicDef = mdef,
                maxBreakGauge = tier == Tier.Boss ? 200 : 100, // not scaled by dungeon
                gold = tier == Tier.Boss ? BossGold : tier == Tier.Elite ? EliteGold : NormalGold,
                targetRule = tier == Tier.Elite ? TargetRule.LowestHp : TargetRule.Random,
                aiTier = tier == Tier.Boss ? AiTier.Boss : tier == Tier.Elite ? AiTier.Elite : AiTier.Normal,
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

            // ponytail: common support enemies are not in Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx; add them if they stick.
            // Kits and stats: posture-B starting values (untested). Pass test: in its encounter, killing the support first ends the fight
            // >= 1 round sooner than killing it last (200-seed sim). If it fails: more MP (casts), or lower the partner's HP.
            D1Enemy("ossuary_acolyte", "Ossuary Acolyte", "Acolyte", "OAC", Tier.Normal, "Undead", "Healer",
                new Stats(120, 30, 12, 6, 10, 14), 9, Art.Skeleton,
                "A novice of the crypt chapel. Mends a wounded ally with bone-dust prayers.",
                new EnemyAttack { name = "Bone Mending", hits = 0, power = 0f, healAlly = 0.08f, aiAllyHpBelow = 0.50f,
                    mpCost = 14, cooldown = 3, aiPriority = 3 },
                Move("Censer Swing", Element.Physical, 12)),

            D1Enemy("dirge_chanter", "Dirge Chanter", "Chanter", "DCH", Tier.Normal, "Spirit", "Buffer",
                new Stats(110, 40, 10, 5, 13, 16), 8, Art.Skeleton,
                "A mourning spirit whose dirge drives its allies into a fury.",
                new EnemyAttack { name = "War Dirge", hits = 0, power = 0f, mpCost = 12, cooldown = 3, aiPriority = 2,
                    buffAlly = new[] { new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.20f } } },
                new EnemyAttack { name = "Mournful Wail", element = Element.Shadow, hitDamage = new[] { 14 }, mpCost = 6, aiPriority = 1 },
                BasicAttack(10)),

            D1Enemy("hollow_banneret", "Hollow Banneret", "Banneret", "HBN", Tier.Normal, "Undead", "Buffer",
                new Stats(150, 30, 15, 10, 8, 8), 7, Art.Skeleton,
                "A standard-bearer of the fallen guard. Its banner hardens the line around it.",
                new EnemyAttack { name = "Rally Banner", hits = 0, power = 0f, mpCost = 10, cooldown = 3, aiPriority = 2,
                    buffAlly = new[] { new StatusApply { id = StatusId.DefenseUp, turns = 2, magnitude = 0.25f } } },
                Move("Banner Pole", Element.Physical, 15)),

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
                "A former royal priest guarding a tainted relic. Mends and wards its allies, and hoards MP with Arcane Charge.",
                // ponytail: heal 0.15 -> 0.20 and Reliquary Ward diverge from Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx.
                // posture-B starting values (untested). Pass test: the median d1_e8 fight takes 4-6 rounds.
                // If it fails: heal back to 0.15f first, keep the Ward.
                new EnemyAttack { name = "Mending Litany", hits = 0, power = 0f, healAlly = 0.20f, aiAllyHpBelow = 0.65f,
                    mpCost = 24, cooldown = 3, aiPriority = 3 },
                ArcaneCharge(priority: 2, useBelowMpFraction: 0.30f, unlessAllyHpBelow: 0.40f),
                new EnemyAttack { name = "Reliquary Ward", hits = 0, power = 0f, mpCost = 18, cooldown = 3, aiPriority = 2,
                    buffAlly = new[]
                    {
                        new StatusApply { id = StatusId.DefenseUp, turns = 2, magnitude = 0.30f },
                        new StatusApply { id = StatusId.MagicDefenseUp, turns = 2, magnitude = 0.30f },
                    } },
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
                // Harvest Call: instant summon move (no damage, no QTE). P1 priority 3, above Soulbind (2) and Soul Lance (1).
                new EnemyAttack { name = "Harvest Call", hits = 0, power = 0f, element = Element.Shadow, summonCount = HarvestCallSummonCount,
                    mpCost = HarvestCallP1MpCost, aiPriority = 3, cooldown = HarvestCallCooldown, initialCooldown = HarvestCallInitialCooldown },
                // ponytail: Soulbind is no longer telegraphed (instant status move), which diverges from Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx.
                new EnemyAttack { name = "Soulbind", hits = 0, power = 0f, element = Element.Shadow, mpCost = 24, aiPriority = 2, cooldown = 3,
                    apply = new[] { Chance(StatusId.Silence, 0.20f) } },
                new EnemyAttack { name = "Soul Lance", element = Element.Shadow, hitDamage = new[] { 42 }, mpCost = 18, aiPriority = 1, cooldown = 1 },
                new EnemyAttack { name = "Soul Rend", element = Element.Physical, hitDamage = new[] { 38 }, aiPriority = 0 },
                ArcaneCharge(priority: 5)),

            D1Enemy("zoroa_p2", "Zoroa, Harvester of Souls", "Zoroa", "ZOR", Tier.Boss, "Soul Entity", "Controller",
                new Stats(1840, 850, 43, 18, 17, 50), 24, Art.Warden,
                "With the first bar shattered, Zoroa rises with a fresh one. Health bar 2/2.",
                new EnemyAttack { name = "Final Reaping", element = Element.Physical, hitDamage = new[] { 34, 34, 34, 50 }, intervals = ZoroaAccelerando,
                    heavyHit = 3, telegraph = true, breakPower = FinalReapingP2BreakPower, mpCost = 40, aiPriority = 5, cooldown = 3, initialCooldown = 1 },
                new EnemyAttack { name = "Soulstorm", element = Element.Shadow, hitDamage = new[] { 24 }, allTargets = true, telegraph = true,
                    breakPower = SoulstormBreakPower, mpCost = 45, aiPriority = 4, cooldown = 3, minTargets = 2 }, // the one new Phase 2 attack; shared QTE
                // Harvest Call: placed between Soulstorm (4) and Soulbind (2), always below Final Reaping (5).
                new EnemyAttack { name = "Harvest Call", hits = 0, power = 0f, element = Element.Shadow, summonCount = HarvestCallSummonCount,
                    mpCost = HarvestCallP2MpCost, aiPriority = 3, cooldown = HarvestCallCooldown, initialCooldown = HarvestCallInitialCooldown },
                // ponytail: Soulbind is no longer telegraphed (instant status move), which diverges from Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx.
                new EnemyAttack { name = "Soulbind", hits = 0, power = 0f, element = Element.Shadow, mpCost = 28, aiPriority = 2, cooldown = 3,
                    apply = new[] { Chance(StatusId.Silence, 0.25f) } },
                new EnemyAttack { name = "Soul Lance", element = Element.Shadow, hitDamage = new[] { 50 }, mpCost = 22, aiPriority = 1, cooldown = 1 },
                new EnemyAttack { name = "Soul Rend", element = Element.Physical, hitDamage = new[] { 43 }, aiPriority = 0 },
                ArcaneCharge(priority: 6)),
        };

        // Harvest Call summon pools: 80% D1 Normal enemies, 20% v1.9 random Elite pool (never stage bosses soul_reaper / royal_ossuary_knight).
        // Common supports are left out: heals scale with the target's Max HP, so a summoned healer would mend Zoroa for ~190 HP a cast.
        public static readonly string[] SummonNormalPool =
            Enemies.Where(e => e.tier == Tier.Normal && e.attacks.All(a => !a.SupportsAlly)).Select(e => e.id).ToArray();
        public static readonly string[] SummonElitePool =
        {
            "reliquary_confessor", "graveglass_arcanist", "crownless_duelist", "tombstone_colossus", "mourning_bell_wraith",
        };

        public static EnemyDef RollHarvestCallSummon(System.Random rng)
        {
            var pool = rng.NextDouble() < HarvestCallEliteChance ? SummonElitePool : SummonNormalPool;
            string id = pool[rng.Next(pool.Length)];
            return Database.Enemies.TryGetValue(id, out var d) ? d : Enemies.First(e => e.id == id);
        }

        static Dungeon1Roster()
        {
            var p1 = Enemies.First(e => e.id == "zoroa_p1");
            var p2 = Enemies.First(e => e.id == "zoroa_p2");
            p1.nextPhase = "zoroa_p2";
            p1.phase2Text = "Zoroa's first soul-shell shatters - the Harvester rises anew! (Phase 2)";
            p1.turnsPerRound = ZoroaTurnsPerRound;
            p2.turnsPerRound = ZoroaTurnsPerRound;
            // Confirmed targeting: Elites -> lowest current HP, Archer -> lowest DEF, everyone else random.
            Enemies.First(e => e.id == "gravebound_archer").targetRule = TargetRule.LowestDef;

            // Spec §6.2 tactical personalities, Architecture §12 v1.1 intelligence tiers, and Instinct Layer archetypes for all 15 D1 roster enemies (16 defs incl. Zoroa P2).
            SetAi("hollow_squire", AiRole.Vanguard, AiTier.Normal, InstinctKind.Relentless);
            SetAi("gravebound_archer", AiRole.RangedOpportunist, AiTier.Normal, InstinctKind.Territorial);
            SetAi("cinder_wisp", AiRole.ElementalAttacker, AiTier.Normal, InstinctKind.Skittish);
            SetAi("soulbound_hound", AiRole.Disruptor, AiTier.Normal, InstinctKind.Predator);
            SetAi("possessed_royal_guard", AiRole.Bruiser, AiTier.Normal, InstinctKind.Guardian);
            SetAi("crypt_leech", AiRole.Attrition, AiTier.Normal, InstinctKind.Predator);
            SetAi("forgotten_militia", AiRole.ComboAttacker, AiTier.Normal, InstinctKind.Relentless);
            SetAi("ossuary_acolyte", AiRole.Support, AiTier.Normal, InstinctKind.Guardian);
            SetAi("dirge_chanter", AiRole.Support, AiTier.Normal, InstinctKind.Guardian);
            SetAi("hollow_banneret", AiRole.Support, AiTier.Normal, InstinctKind.Guardian);
            SetAi("soul_reaper", AiRole.Executioner, AiTier.Miniboss, InstinctKind.Predator);
            SetAi("royal_ossuary_knight", AiRole.DefensivePunisher, AiTier.Miniboss, InstinctKind.Guardian);
            SetAi("reliquary_confessor", AiRole.Support, AiTier.Elite, InstinctKind.Guardian);
            SetAi("graveglass_arcanist", AiRole.ArcaneCaster, AiTier.Elite, InstinctKind.Territorial);
            SetAi("crownless_duelist", AiRole.Skirmisher, AiTier.Elite, InstinctKind.Relentless);
            SetAi("tombstone_colossus", AiRole.BreakBruiser, AiTier.Elite, InstinctKind.Territorial);
            SetAi("mourning_bell_wraith", AiRole.AreaController, AiTier.Elite, InstinctKind.Territorial);
            SetAi("zoroa_p1", AiRole.AdaptiveController, AiTier.Boss, InstinctKind.Territorial);
            SetAi("zoroa_p2", AiRole.AdaptiveController, AiTier.Boss, InstinctKind.Territorial);

            // Authored QTE motifs (QTE Variation Spec Part D, Handoff §Example motifs)
            SetMotif("hollow_squire", "Rusty Cleave", new QteMotif
            {
                hits = new[] { new HitCue(QteVariant.None, DodgeDir.Right) },
            });
            SetMotif("gravebound_archer", "Grave Arrow", new QteMotif
            {
                hits = new[] { new HitCue(QteVariant.None, DodgeDir.Up) },
            });
            SetMotif("soulbound_hound", "Rending Bite", new QteMotif
            {
                hits = new[] { new HitCue(QteVariant.None, DodgeDir.Left) },
            });
            SetMotif("possessed_royal_guard", "Royal Halberd", new QteMotif
            {
                hits = new[] { new HitCue(QteVariant.Slip) },
            });
            SetMotif("soul_reaper", "Accelerating Rend", new QteMotif
            {
                hits = new[]
                {
                    new HitCue(QteVariant.None, DodgeDir.Right),
                    new HitCue(QteVariant.None, DodgeDir.Left),
                    new HitCue(QteVariant.Roll, DodgeDir.Up),
                    new HitCue(QteVariant.None, DodgeDir.Down),
                },
                remixes = new[] { RemixKind.Mirror },
            });
            SetMotif("royal_ossuary_knight", "Graveward Charge", new QteMotif
            {
                hits = new[] { new HitCue(QteVariant.None) },
            });
            SetMotif("crownless_duelist", "Triple Feint", new QteMotif
            {
                hits = new[]
                {
                    new HitCue(QteVariant.None),
                    new HitCue(QteVariant.Flurry),
                    new HitCue(QteVariant.Roll, DodgeDir.Right),
                },
                feintHit = 0,
            });
            foreach (var zid in new[] { "zoroa_p1", "zoroa_p2" })
            {
                SetMotif(zid, "Final Reaping", new QteMotif
                {
                    hits = new[]
                    {
                        new HitCue(QteVariant.None, DodgeDir.Up),
                        new HitCue(QteVariant.None, DodgeDir.Down),
                        new HitCue(QteVariant.Roll, DodgeDir.Left),
                        new HitCue(QteVariant.None), // Heavy hit 4 = Brace
                    },
                    remixes = new[] { RemixKind.Mirror, RemixKind.Upgrade },
                });
            }

            // Authored Dungeon 1 affinities, concealed boss affinities, and per-status resistances
            // (Felna_Analysis_System_Spec_v1.md Part F; adjusted per Stage 3 coverage audit against Database.Skills
            // so every weakness is dealable by >= 2 playable classes: Physical, Fire, Dark).
            void SetAnalysisProfile(
                string enemyId,
                Element[] weak = null,
                Element[] resist = null,
                Element[] immune = null,
                Element[] drain = null,
                Element[] reflect = null,
                Element[] concealed = null,
                Dictionary<StatusId, float> statusResist = null,
                StatusId[] statusImmune = null)
            {
                var def = Array.Find(Enemies, e => e.id == enemyId);
                if (def == null) return;
                def.affinities = new Affinities
                {
                    weak = weak ?? Array.Empty<Element>(),
                    resist = resist ?? Array.Empty<Element>(),
                    immune = immune ?? Array.Empty<Element>(),
                    drain = drain ?? Array.Empty<Element>(),
                    reflect = reflect ?? Array.Empty<Element>(),
                };
                def.affinityProfile = EnemyAffinityProfile.FromLegacyAffinities(def.affinities);
                def.concealedAffinities = concealed ?? Array.Empty<Element>();
                def.statusResist = statusResist;
                def.statusImmune = statusImmune ?? Array.Empty<StatusId>();
            }

            SetAnalysisProfile("hollow_squire",
                weak: new[] { Element.Fire },
                resist: new[] { Element.Dark },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Fear] = 0.25f, [StatusId.Bleed] = -0.20f });

            SetAnalysisProfile("gravebound_archer",
                weak: new[] { Element.Fire, Element.Physical },
                resist: new[] { Element.Dark },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Poison] = 0.25f });

            SetAnalysisProfile("cinder_wisp",
                weak: new[] { Element.Physical, Element.Dark },
                resist: new[] { Element.Fire },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Freeze] = -0.20f },
                statusImmune: new[] { StatusId.Burn });

            SetAnalysisProfile("soulbound_hound",
                weak: new[] { Element.Fire },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Fear] = 0.25f });

            SetAnalysisProfile("possessed_royal_guard",
                weak: new[] { Element.Fire },
                resist: new[] { Element.Physical },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Stun] = 0.25f });

            SetAnalysisProfile("crypt_leech",
                weak: new[] { Element.Fire },
                resist: new[] { Element.Water },
                statusImmune: new[] { StatusId.Poison });

            SetAnalysisProfile("forgotten_militia",
                weak: new[] { Element.Fire, Element.Physical },
                resist: new[] { Element.Dark },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Bleed] = -0.20f });

            SetAnalysisProfile("ossuary_acolyte",
                weak: new[] { Element.Dark },
                resist: new[] { Element.Holy },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Silence] = -0.20f });

            SetAnalysisProfile("dirge_chanter",
                weak: new[] { Element.Physical, Element.Fire },
                resist: new[] { Element.Dark },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Silence] = -0.20f });

            SetAnalysisProfile("hollow_banneret",
                weak: new[] { Element.Fire },
                resist: new[] { Element.Physical },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Fear] = 0.25f });

            SetAnalysisProfile("soul_reaper",
                weak: new[] { Element.Fire },
                drain: new[] { Element.Dark },
                statusImmune: new[] { StatusId.Fear });

            SetAnalysisProfile("royal_ossuary_knight",
                weak: new[] { Element.Fire, Element.Dark },
                resist: new[] { Element.Physical },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Stun] = 0.25f });

            SetAnalysisProfile("reliquary_confessor",
                weak: new[] { Element.Dark },
                drain: new[] { Element.Holy },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Silence] = 0.25f });

            SetAnalysisProfile("graveglass_arcanist",
                weak: new[] { Element.Physical, Element.Dark },
                reflect: new[] { Element.Lightning },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Silence] = -0.20f });

            SetAnalysisProfile("crownless_duelist",
                weak: new[] { Element.Fire, Element.Dark },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Bleed] = 0.25f });

            SetAnalysisProfile("tombstone_colossus",
                weak: new[] { Element.Fire, Element.Dark },
                resist: new[] { Element.Physical, Element.Earth },
                statusResist: new Dictionary<StatusId, float> { [StatusId.Stun] = 0.25f },
                statusImmune: new[] { StatusId.Fear });

            SetAnalysisProfile("mourning_bell_wraith",
                weak: new[] { Element.Physical, Element.Fire },
                resist: new[] { Element.Dark },
                statusImmune: new[] { StatusId.Fear });

            SetAnalysisProfile("zoroa_p1",
                weak: new[] { Element.Fire },
                resist: new[] { Element.Physical },
                drain: new[] { Element.Dark },
                concealed: new[] { Element.Dark });

            SetAnalysisProfile("zoroa_p2",
                weak: new[] { Element.Fire, Element.Physical },
                resist: new[] { Element.Dark },
                reflect: new[] { Element.Lightning },
                concealed: new[] { Element.Lightning });

            // Build and link the 3 Dungeon 1 Enhanced Common Variant pilot families (Variant Spec §12.3, §15.1; Transformation Spec §3.1).
            EnhancedVariants = new[]
            {
                Enhanced("hollow_squire", new VariantProfile
                {
                    statMult = SquireEnhancedMult,
                    targetRule = TargetRule.LowestDef,
                    transformChance = PilotTransformChance,
                    alternativeSkillName = "Rusty Cleave",
                    alternativeHitDamage = new[] { 12, 12 }, // Repeated Assault: 2 x 12, Steady (posture-B starting value, untested)
                    alternativeCooldown = 1,
                    alternativePriority = 2,
                    alternativeMotif = new QteMotif
                    {
                        hits = new[] { new HitCue(QteVariant.None), new HitCue(QteVariant.Flurry) },
                        remixes = new[] { RemixKind.Upgrade },
                    },
                }),
                Enhanced("soulbound_hound", new VariantProfile
                {
                    statMult = HoundEnhancedMult,
                    targetRule = TargetRule.HighestThreat,
                    transformChance = PilotTransformChance,
                    alternativeSkillName = "Rending Bite",
                    alternativeHitDamage = new[] { 8, 7, 7 }, // 3 hits 8/7/7, Accelerando 350/310 ms (posture-B starting value, untested)
                    alternativeIntervals = HoundEnhancedAccelerando,
                    alternativeCooldown = 1,
                    alternativePriority = 2,
                    alternativeMotif = new QteMotif
                    {
                        hits = new[] { new HitCue(QteVariant.None), new HitCue(QteVariant.GuardCue), new HitCue(QteVariant.None) },
                        remixes = new[] { RemixKind.Tempo },
                    },
                }),
                Enhanced("cinder_wisp", new VariantProfile
                {
                    statMult = WispEnhancedMult,
                    targetRule = TargetRule.LowestMdef,
                    transformChance = PilotTransformChance,
                    alternativeSkillName = "Ember Bolt",
                    alternativeHitDamage = new[] { 13, 13 }, // 2 x 13, Steady, Burn 30% once per attack, MP 8 (posture-B starting value, untested)
                    alternativeMpCost = 8,
                    alternativeApply = new[] { Chance(StatusId.Burn, 0.30f) },
                    alternativeCooldown = 1,
                    alternativePriority = 2,
                }),
            };
        }

        static void SetMotif(string enemyId, string moveName, QteMotif motif)
        {
            var e = Enemies.First(x => x.id == enemyId);
            var m = e.attacks.First(a => a.name == moveName);
            m.motif = motif;
        }

        public sealed class VariantProfile
        {
            public VariantStatMult statMult;
            public TargetRule targetRule;
            public float transformChance = PilotTransformChance;
            public string alternativeSkillName;
            public int[] alternativeHitDamage;
            public float[] alternativeIntervals;
            public int? alternativeMpCost;
            public int alternativeCooldown = 1;
            public int alternativePriority = 2;
            public StatusApply[] alternativeApply;
            public QteMotif alternativeMotif;
        }

        public static readonly EnemyDef[] EnhancedVariants;

        // Builds an Enhanced Common Variant EnemyDef from the base common enemy definition (Variant Spec §12.3, Handoff §Changes.2).
        public static EnemyDef Enhanced(string baseId, VariantProfile p)
        {
            var baseDef = Enemies.First(e => e.id == baseId);
            var attacks = new List<EnemyAttack>();

            foreach (var src in baseDef.attacks)
            {
                if (src.name == p.alternativeSkillName)
                {
                    // Alternative pattern of the existing skill identity (selected when off cooldown / affordable).
                    var alt = CloneAttack(src);
                    alt.patternId = "alt";
                    alt.hitDamage = (int[])p.alternativeHitDamage.Clone();
                    alt.intervals = p.alternativeIntervals != null ? (float[])p.alternativeIntervals.Clone() : null;
                    if (p.alternativeMpCost.HasValue) alt.mpCost = p.alternativeMpCost.Value;
                    if (p.alternativeApply != null)
                        alt.apply = p.alternativeApply.Select(CloneStatusApply).ToArray();
                    alt.cooldown = p.alternativeCooldown;
                    alt.aiPriority = p.alternativePriority;
                    if (p.alternativeMotif != null) alt.motif = p.alternativeMotif;
                    DeriveHitPower(alt, baseDef.stats);
                    attacks.Add(alt);
                }

                var std = CloneAttack(src);
                DeriveHitPower(std, baseDef.stats);
                attacks.Add(std);
            }

            var variant = new EnemyDef
            {
                id = baseId + "_enhanced",
                baseEnemyId = baseId,
                name = baseDef.name + " (Enhanced)",
                shortName = baseDef.shortName,
                tag = baseDef.tag,
                tier = Tier.Normal,
                ai = AiKind.Priority,
                aiRole = baseDef.aiRole,
                aiTier = AiTier.Enhanced,
                instinct = baseDef.instinct,
                isEnhanced = true,
                statMult = p.statMult,
                targetRule = p.targetRule,
                type = baseDef.type,
                role = baseDef.role,
                affinities = baseDef.affinities,
                affinityProfile = baseDef.affinityProfile != null ? baseDef.affinityProfile.Clone() : null,
                statusResist = baseDef.statusResist != null ? new Dictionary<StatusId, float>(baseDef.statusResist) : null,
                statusImmune = baseDef.statusImmune != null ? (StatusId[])baseDef.statusImmune.Clone() : Array.Empty<StatusId>(),
                concealedAffinities = baseDef.concealedAffinities != null ? (Element[])baseDef.concealedAffinities.Clone() : Array.Empty<Element>(),
                abilityScores = baseDef.abilityScores,
                stats = baseDef.stats,
                magicDef = baseDef.magicDef,
                maxBreakGauge = baseDef.maxBreakGauge,
                breakResistance = baseDef.breakResistance,
                breakDuration = baseDef.breakDuration,
                breakStunTurns = baseDef.breakStunTurns,
                breakDamageMultiplier = baseDef.breakDamageMultiplier,
                gold = baseDef.gold,
                sprite = baseDef.sprite,
                description = baseDef.description,
                turnsPerRound = 1,
                attacks = attacks.ToArray(),
            };

            baseDef.enhancedVariant = variant.id;
            baseDef.transformChance = p.transformChance;
            return variant;
        }

        static EnemyAttack CloneAttack(EnemyAttack a) => new EnemyAttack
        {
            name = a.name,
            patternId = a.patternId,
            hits = a.hits,
            power = a.power,
            element = a.element,
            allTargets = a.allTargets,
            self = a.self,
            isHeavy = a.isHeavy,
            delay = a.delay,
            phase = a.phase,
            apply = a.apply != null ? a.apply.Select(CloneStatusApply).ToArray() : null,
            hitDamage = a.hitDamage != null ? (int[])a.hitDamage.Clone() : null,
            hitPower = a.hitPower != null ? (float[])a.hitPower.Clone() : null,
            intervals = a.intervals != null ? (float[])a.intervals.Clone() : null,
            heavyHit = a.heavyHit,
            mpCost = a.mpCost,
            telegraph = a.telegraph,
            drain = a.drain,
            restoreMp = a.restoreMp,
            healAlly = a.healAlly,
            buffAlly = a.buffAlly != null ? a.buffAlly.Select(CloneStatusApply).ToArray() : null,
            summonCount = a.summonCount,
            breakPower = a.breakPower,
            aiPriority = a.aiPriority,
            cooldown = a.cooldown,
            initialCooldown = a.initialCooldown,
            minTargets = a.minTargets,
            aiMpBelow = a.aiMpBelow,
            aiAllyHpBelow = a.aiAllyHpBelow,
            aiUnlessAllyHpBelow = a.aiUnlessAllyHpBelow,
            aiBreakLeftBelow = a.aiBreakLeftBelow,
            aiBreakGateRound = a.aiBreakGateRound,
        };

        static StatusApply CloneStatusApply(StatusApply s) => new StatusApply
        {
            id = s.id,
            turns = s.turns,
            magnitude = s.magnitude,
            chance = s.chance,
            onSelf = s.onSelf,
            dotDamageType = s.dotDamageType,
            dotElementType = s.dotElementType,
            dotSkillPower = s.dotSkillPower,
            dotScaling = s.dotScaling,
            dotScalingStat = s.dotScalingStat,
        };

        static void DeriveHitPower(EnemyAttack a, Stats baseStats)
        {
            if (a.hitDamage == null) return;
            float stat = a.UsesMagic ? baseStats.mag : baseStats.atk;
            a.hits = a.hitDamage.Length;
            a.hitPower = a.hitDamage.Select(dmg => dmg / stat).ToArray();
            a.power = a.hitPower.Length > 0 ? a.hitPower[0] : 0f;
        }

        // Enforces every validation rule in Variant Spec §13 on an Enhanced Common Variant definition.
        public static bool ValidateEnhancedVariant(EnemyDef variant, out List<string> errors)
        {
            errors = new List<string>();
            if (variant == null) { errors.Add("variant is null"); return false; }
            if (!variant.isEnhanced) errors.Add($"{variant.id}: isEnhanced must be true");
            if (variant.tier != Tier.Normal) errors.Add($"{variant.id}: tier must remain Tier.Normal");
            if (variant.aiTier != AiTier.Enhanced) errors.Add($"{variant.id}: aiTier must be AiTier.Enhanced");
            if (!variant.aiTier.IsNormalTier()) errors.Add($"{variant.id}: AiTier.Enhanced must count as Normal tier");
            if (variant.turnsPerRound != 1) errors.Add($"{variant.id}: turnsPerRound must be 1");
            if (!string.IsNullOrEmpty(variant.enhancedVariant) || variant.transformChance > 0f)
                errors.Add($"{variant.id}: an Enhanced variant cannot transform again");

            var baseDef = Enemies.FirstOrDefault(e => e.id == variant.baseEnemyId);
            if (baseDef == null)
            {
                errors.Add($"{variant.id}: baseEnemyId '{variant.baseEnemyId}' not found in base roster");
                return false;
            }
            if (variant.id != baseDef.id + "_enhanced") errors.Add($"{variant.id}: id must be '{baseDef.id}_enhanced'");
            if (variant.name != baseDef.name + " (Enhanced)") errors.Add($"{variant.id}: name must be '{baseDef.name} (Enhanced)'");
            if (!ReferenceEquals(variant.sprite, baseDef.sprite)) errors.Add($"{variant.id}: must reuse base sprite");
            if (variant.tag != baseDef.tag) errors.Add($"{variant.id}: must reuse base tag");
            if (variant.aiRole != baseDef.aiRole) errors.Add($"{variant.id}: must preserve base aiRole");
            if (baseDef.tier != Tier.Normal) errors.Add($"{variant.id}: base enemy must be Tier.Normal");

            // Pattern count & skill identity validation (Variant Spec §5, §6, §13)
            if (variant.attacks == null || variant.attacks.Length == 0)
            {
                errors.Add($"{variant.id}: has no attacks");
                return false;
            }
            if (!variant.attacks.Any(a => a.IsBasicAttack))
                errors.Add($"{variant.id}: must have a 0-MP Basic Attack fallback");

            int altPatterns = variant.attacks.Count(a => !string.IsNullOrEmpty(a.patternId));
            if (altPatterns > 1) errors.Add($"{variant.id}: at most 1 alternative pattern (<= 2 total patterns)");

            foreach (var a in variant.attacks)
            {
                var baseMoves = baseDef.attacks.Where(bm => bm.name == a.name).ToList();
                if (baseMoves.Count == 0)
                {
                    errors.Add($"{variant.id}: move '{a.name}' is not an existing skill identity on '{baseDef.id}'");
                    continue;
                }
                var bm0 = baseMoves[0];
                if (a.element != bm0.element) errors.Add($"{variant.id}.{a.name}: element must match base skill");
                if (a.hits > 4) errors.Add($"{variant.id}.{a.name}: hits ({a.hits}) exceeds D1 limit of 4");
                if (a.HasHeavy) errors.Add($"{variant.id}.{a.name}: Enhanced Common cannot have Heavy markers");
                if (a.telegraph) errors.Add($"{variant.id}.{a.name}: Charge patterns are disallowed on Enhanced Commons");
                if (a.apply != null && a.apply.Length > 0)
                {
                    if (a.hits > 3) errors.Add($"{variant.id}.{a.name}: status-applying move cannot exceed 3 hits");
                    var baseStatuses = new HashSet<StatusId>((bm0.apply ?? Array.Empty<StatusApply>()).Select(s => s.id));
                    foreach (var ap in a.apply)
                        if (!baseStatuses.Contains(ap.id))
                            errors.Add($"{variant.id}.{a.name}: introduces unapproved status '{ap.id}'");
                }
                if (string.IsNullOrEmpty(a.patternId))
                {
                    if (a.intervals != null)
                        errors.Add($"{variant.id}.{a.name}: standard pattern must be Steady (intervals == null)");
                }
                else if (a.intervals != null)
                {
                    if (a.intervals.Length != Math.Max(0, a.hits - 1))
                        errors.Add($"{variant.id}.{a.name}: Accelerando intervals length must equal hits - 1");
                    if (a.intervals.Any(iv => iv < 0.310f - 1e-6f))
                        errors.Add($"{variant.id}.{a.name}: Accelerando interval below 310 ms Elite floor");
                    for (int i = 1; i < a.intervals.Length; i++)
                        if (a.intervals[i] > a.intervals[i - 1] + 1e-6f)
                            errors.Add($"{variant.id}.{a.name}: Syncopated patterns are disallowed (intervals must be non-increasing)");
                }
            }

            // Stat scaling validation across dungeons 1..8: EnhancedStat = floor(NormalScaledStat * mult) applied once (§4.3).
            for (int d = 1; d <= 8; d++)
            {
                var baseUnit = new Unit(baseDef, d);
                var enhUnit = new Unit(variant, d);
                var m = variant.statMult;
                if (enhUnit.stats.maxHp != Unit.ScaleVariantStat(baseUnit.stats.maxHp, m.Hp))
                    errors.Add($"{variant.id} D{d}: HP mismatch");
                if (enhUnit.stats.maxMp != Unit.ScaleVariantStat(baseUnit.stats.maxMp, m.Mp))
                    errors.Add($"{variant.id} D{d}: MP mismatch");
                if (enhUnit.stats.atk != Unit.ScaleVariantStat(baseUnit.stats.atk, m.Atk))
                    errors.Add($"{variant.id} D{d}: ATK mismatch");
                if (enhUnit.stats.mag != Unit.ScaleVariantStat(baseUnit.stats.mag, m.Matk))
                    errors.Add($"{variant.id} D{d}: MATK mismatch");
                if (enhUnit.stats.def != Unit.ScaleVariantStat(baseUnit.stats.def, m.Def))
                    errors.Add($"{variant.id} D{d}: DEF mismatch");
                if (enhUnit.Derived.magicDefense != Unit.ScaleVariantStat(baseUnit.Derived.magicDefense, m.Mdef))
                    errors.Add($"{variant.id} D{d}: MDEF mismatch");
                if (enhUnit.stats.spd != Unit.ScaleVariantStat(baseUnit.stats.spd, m.Spd))
                    errors.Add($"{variant.id} D{d}: SPD mismatch");
            }

            return errors.Count == 0;
        }

        static void SetAi(string id, AiRole role, AiTier tier, InstinctKind instinct)
        {
            var e = Enemies.First(x => x.id == id);
            e.aiRole = role;
            e.aiTier = tier;
            e.instinct = instinct;
        }

        // Procedural encounter filler pools (never Enhanced variants or stage bosses).
        public static readonly string[] AllCommonPool =
            Enemies.Where(e => e.tier == Tier.Normal && !e.isEnhanced).Select(e => e.id).ToArray();
        public static readonly string[] NonSupportCommonPool =
            Enemies.Where(e => e.tier == Tier.Normal && !e.isEnhanced && e.attacks.All(a => !a.SupportsAlly)).Select(e => e.id).ToArray();

        public static readonly EncounterDef[] Encounters =
        {
            new EncounterDef { id = "d1_e1", name = "Ruined Gate", tier = Tier.Normal, minFloor = 0, focus = "Single-target physical; Defensive QTE introduction",
                enemies = new[] { "hollow_squire", "hollow_squire" },
                anchors = new[] { "hollow_squire" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 2 },
            new EncounterDef { id = "d1_e2", name = "Fallen Watch", tier = Tier.Normal, minFloor = 0, focus = "Target priority; ranged pressure",
                enemies = new[] { "gravebound_archer", "hollow_squire" },
                anchors = new[] { "gravebound_archer" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 2 },
            new EncounterDef { id = "d1_e3", name = "Ash Courtyard", tier = Tier.Normal, minFloor = 1, focus = "Elemental damage, Burn, multi-hit Defensive QTE",
                enemies = new[] { "cinder_wisp", "forgotten_militia" },
                anchors = new[] { "cinder_wisp" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 2 },
            new EncounterDef { id = "d1_e4", name = "Kennel of Lost Souls", tier = Tier.Normal, minFloor = 1, focus = "Status pressure; enemy sustain",
                enemies = new[] { "soulbound_hound", "crypt_leech" },
                anchors = new[] { "soulbound_hound" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 2 },
            new EncounterDef { id = "d1_e5", name = "Royal Remnant", tier = Tier.Normal, minFloor = 2, focus = "Enemy durability; target selection",
                enemies = new[] { "possessed_royal_guard", "hollow_squire", "hollow_squire" },
                anchors = new[] { "possessed_royal_guard" }, pool = NonSupportCommonPool, minCount = 3, maxCount = 4 },
            new EncounterDef { id = "d1_e6", name = "Reaper's Passage", tier = Tier.Elite, minFloor = 2, focus = "Stage 1 Boss: Soul Reaper with 3 allies; Accelerando",
                enemies = new[] { "soul_reaper", "forgotten_militia", "soulbound_hound", "crypt_leech" },
                anchors = new[] { "soul_reaper" }, pool = AllCommonPool, minCount = 3, maxCount = 4, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e7", name = "Ossuary Threshold", tier = Tier.Elite, minFloor = 2, focus = "Stage 2 Boss: Royal Ossuary Knight with 3 allies; Heavy & Break interrupt",
                enemies = new[] { "royal_ossuary_knight", "possessed_royal_guard", "gravebound_archer", "hollow_squire" },
                anchors = new[] { "royal_ossuary_knight" }, pool = AllCommonPool, minCount = 3, maxCount = 4, music = MusicIds.Battle2 },
            // v1.9 FINAL random Elite pool (sheet E6-E10). d1_e6 / d1_e7 above are the special Miniboss nodes (stage bosses).
            new EncounterDef { id = "d1_e8", name = "Reliquary Chamber", tier = Tier.Elite, minFloor = 2, focus = "Healing / sustain support; focus the Confessor",
                enemies = new[] { "reliquary_confessor", "gravebound_archer" },
                anchors = new[] { "reliquary_confessor" }, pool = AllCommonPool, minCount = 2, maxCount = 3, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e9", name = "Shattered Chapel", tier = Tier.Elite, minFloor = 2, focus = "Magic damage / MP management",
                enemies = new[] { "graveglass_arcanist", "cinder_wisp" },
                anchors = new[] { "graveglass_arcanist" }, pool = AllCommonPool, minCount = 2, maxCount = 3, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e10", name = "Duelist's Gallery", tier = Tier.Elite, minFloor = 2, focus = "Physical multi-hit pressure",
                enemies = new[] { "crownless_duelist", "forgotten_militia" },
                anchors = new[] { "crownless_duelist" }, pool = AllCommonPool, minCount = 2, maxCount = 3, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e11", name = "Fallen Mausoleum", tier = Tier.Elite, minFloor = 2, focus = "Durability / Break pressure",
                enemies = new[] { "tombstone_colossus", "hollow_squire" },
                anchors = new[] { "tombstone_colossus" }, pool = AllCommonPool, minCount = 2, maxCount = 3, music = MusicIds.Battle2 },
            new EncounterDef { id = "d1_e12", name = "Mourning Bell Hall", tier = Tier.Elite, minFloor = 2, focus = "AoE / control pressure; telegraphed Funeral Chorus",
                enemies = new[] { "mourning_bell_wraith", "soulbound_hound" },
                anchors = new[] { "mourning_bell_wraith" }, pool = AllCommonPool, minCount = 2, maxCount = 3, music = MusicIds.Battle2 },
            // ponytail: support encounters are not in the v1.9 workbook Encounter Plan.
            new EncounterDef { id = "d1_e13", name = "Chapel of Bones", tier = Tier.Normal, minFloor = 1, focus = "Enemy healer; kill the Acolyte first",
                enemies = new[] { "ossuary_acolyte", "possessed_royal_guard" },
                anchors = new[] { "ossuary_acolyte" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 3 },
            new EncounterDef { id = "d1_e14", name = "Dirge Hall", tier = Tier.Normal, minFloor = 2, focus = "Attack Up support on a multi-hit ally",
                enemies = new[] { "dirge_chanter", "forgotten_militia", "hollow_squire" },
                anchors = new[] { "dirge_chanter" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 4 },
            new EncounterDef { id = "d1_e15", name = "Banner Line", tier = Tier.Normal, minFloor = 1, focus = "Defense Up support shielding the focused ally",
                enemies = new[] { "hollow_banneret", "gravebound_archer" },
                anchors = new[] { "hollow_banneret" }, pool = NonSupportCommonPool, minCount = 2, maxCount = 3 },
            new EncounterDef { id = "d1_boss", name = "Zoroa", tier = Tier.Boss, focus = "Signature multi-hit, resources, Break timing, Phase 2 escalation",
                enemies = new[] { "zoroa_p1" }, music = MusicIds.ZoroaP1 }, // Phase 2 replaces Phase 1; never spawned alongside it
        };
    }
}
