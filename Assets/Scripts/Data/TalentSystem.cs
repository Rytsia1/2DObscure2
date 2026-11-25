using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // =========================================================================
    // Talent System — Character Progression & Specialization
    // Primary Source of Truth: Assets/Docs/Talents_20System.md
    // =========================================================================

    public enum TalentCategory
    {
        Shared,
        ClassSpecific
    }

    [Serializable]
    public class TalentDef
    {
        public string id;
        public string name;
        public string description;
        public TalentCategory category;
        public string targetClassId;
        public bool isDesignFinalized = true;

        // --- Shared Talent Effect Parameters ---
        // Alert: −10 percentage points Critical Chance against attacking enemy
        public float critChanceReduction = 0f;

        // Healer: +15% healing dealt
        public float healingBonusPercent = 0f;

        // Durable: +10% Max HP and 5% Damage Reduction
        public float maxHpBonusPercent = 0f;
        public float damageReductionPercent = 0f;

        // Keen Mind: −10% MP cost, minimum 1 MP
        public float mpCostReductionPercent = 0f;
        public int minMpFloor = 1;

        // Arcane Reserve: +Max MP (TBD baseline)
        public int maxMpBonusFlat = 0;

        // Poisoner: Increases Poison effectiveness (TBD baseline +25% potency)
        public float poisonPotencyBonus = 0f;

        // Crusher: Increases Stun application chance (TBD baseline +15%)
        public float stunChanceBonus = 0f;

        // Slasher: Increases Bleed application chance (TBD baseline +15%)
        public float bleedChanceBonus = 0f;

        // Piercer: Ignores physical defense (TBD baseline 20% penetration)
        public float physicalResistancePenetration = 0f;

        // Lucky: Retry failed QTE (20% activation chance, max 1/QTE, max 3/battle)
        public float qteRetryChance = 0f;
        public int maxRetriesPerQte = 1;
        public int maxRetriesPerBattle = 3;

        // Inspiring Leader: 10% party Damage Reduction for first 3 turns while alive
        public float partyDamageReduction = 0f;
        public int partyDamageReductionTurns = 0;

        public bool MatchesClass(string classId) => IsCompatibleWithClass(classId);

        public bool IsCompatibleWithClass(string classId)
        {
            if (category == TalentCategory.Shared) return true;
            if (string.IsNullOrEmpty(targetClassId)) return true;
            if (string.IsNullOrEmpty(classId)) return false;

            string normTarget = NormalizeClassId(targetClassId);
            string normClass = NormalizeClassId(classId);
            return normTarget == normClass;
        }

        public static string NormalizeClassId(string cid)
        {
            if (string.IsNullOrEmpty(cid)) return "";
            string lower = cid.ToLowerInvariant();
            return lower switch
            {
                "knight" => "fighter",
                "berserker" => "barbarian",
                "monk" => "martial_artist",
                "thief" => "rogue",
                "priest" => "cleric",
                "mage" => "wizard",
                _ => lower
            };
        }
    }

    public static class TalentSystem
    {
        // Level milestones for Talent selection (GDD Section 2)
        public static readonly int[] MilestoneLevels = { 3, 6, 9, 12, 15 };

        // Balancing defaults / proposed values (GDD Section 3 & 6)
        public const float AlertCritReduction = 0.10f;             // -10 percentage points
        public const float HealerBonus = 0.15f;                    // +15% healing
        public const float DurableHpBonus = 0.10f;                 // +10% Max HP
        public const float DurableDamageReduction = 0.05f;         // 5% Damage Reduction (multiplier 0.95f)
        public const float KeenMindMpReduction = 0.10f;            // -10% MP cost
        public const int KeenMindMinFloor = 1;                     // Minimum floor 1 MP
        public const int ArcaneReserveMaxMpBonus = 15;             // TBD proposed flat Max MP
        public const float PoisonerPotencyBonus = 0.25f;           // TBD proposed +25% Poison DoT potency
        public const float CrusherStunBonus = 0.15f;               // TBD proposed +15% Stun application chance
        public const float SlasherBleedBonus = 0.15f;              // TBD proposed +15% Bleed application chance
        public const float PiercerPenetration = 0.20f;             // TBD proposed 20% Physical Resistance penetration
        public const float LuckyActivationChance = 0.20f;          // 20% retry chance on QTE failure
        public const int LuckyMaxPerQte = 1;                       // Max 1 retry per QTE
        public const int LuckyMaxPerBattle = 3;                    // Max 3 activations per battle
        public const float InspiringLeaderReduction = 0.10f;       // 10% party Damage Reduction (multiplier 0.90f)
        public const int InspiringLeaderDuration = 3;              // 3 turns duration

        // Additional Shared Talents (3.12 - 3.20)
        public const float HardyConstitutionMaxHpFlat = 45f;           // +45 flat Max HP
        public const float HardyConstitutionHealReceivedBonus = 0.10f; // +10% Healing Received
        public const float CombatMomentumInitiative = 8f;              // +8 Initiative
        public const float CombatMomentumActionSpeed = 10f;            // +10 Action Speed
        public const float UnshakeableMentalResist = 15f;              // +15% Mental Resistance
        public const float UnshakeableStaggerResist = 10f;             // +10% Stagger Resistance
        public const float SpellcraftMasteryMagicPowerPercent = 0.12f; // +12% Magic Power
        public const float SpellcraftMasteryMagicPen = 10f;            // +10 Magic Penetration
        public const float QuickReflexDefensiveQteBonus = 0.10f;       // +10% Defensive QTE window
        public const float SteadyHandOffensiveQteBonus = 0.10f;        // +10% Offensive QTE window
        public const float SpoilsSeekerGoldBonus = 0.15f;              // +15% Spoils (Gold) while alive
        public const float PinpointFocusCritChance = 6f;               // +6% Critical Chance
        public const float PinpointFocusCritDamage = 15f;              // +15% Critical Damage
        public const float DesperateGritHpThreshold = 0.35f;           // Active at HP <= 35% Max HP
        public const float DesperateGritDamageBonus = 0.15f;           // +15% All Damage Dealt
        public const float DesperateGritDamageReduction = 0.15f;       // -15% All Damage Taken

        public static int GetTalentSlotsAtLevel(int level) =>
            MilestoneLevels.Count(m => level >= m);

        public static bool IsMilestoneLevel(int level) =>
            MilestoneLevels.Contains(level);

        public static readonly Dictionary<string, TalentDef> AllTalents = new Dictionary<string, TalentDef>();

        static TalentSystem()
        {
            RegisterSharedTalents();
            RegisterAdditionalSharedTalents();
            RegisterAllClassSpecificTalents();
        }

        static void Register(TalentDef def)
        {
            if (def != null && !string.IsNullOrEmpty(def.id))
                AllTalents[def.id] = def;
        }

        static void RegisterSharedTalents()
        {
            // 3.1 Alert
            Register(new TalentDef
            {
                id = "alert",
                name = "Alert",
                description = "Reduces enemy Critical Chance by 10 percentage points when attacking this character.",
                category = TalentCategory.Shared,
                critChanceReduction = AlertCritReduction
            });

            // 3.2 Healer
            Register(new TalentDef
            {
                id = "healer",
                name = "Healer",
                description = "Increases the amount of HP restored by the character by +15% (applies to Skills and Items).",
                category = TalentCategory.Shared,
                healingBonusPercent = HealerBonus
            });

            // 3.3 Durable
            Register(new TalentDef
            {
                id = "durable",
                name = "Durable",
                description = "Improves survival: increases Max HP by +10% and grants 5% Damage Reduction.",
                category = TalentCategory.Shared,
                maxHpBonusPercent = DurableHpBonus,
                damageReductionPercent = DurableDamageReduction
            });

            // 3.4 Keen Mind
            Register(new TalentDef
            {
                id = "keen_mind",
                name = "Keen Mind",
                description = "Reduces MP consumption for Skills by 10% (minimum cost floor of 1 MP).",
                category = TalentCategory.Shared,
                mpCostReductionPercent = KeenMindMpReduction,
                minMpFloor = KeenMindMinFloor
            });

            // 3.5 Arcane Reserve (TBD)
            Register(new TalentDef
            {
                id = "arcane_reserve",
                name = "Arcane Reserve",
                description = "Expands magical capacity: increases the character's Max MP by +15.",
                category = TalentCategory.Shared,
                maxMpBonusFlat = ArcaneReserveMaxMpBonus
            });

            // 3.6 Poisoner (TBD)
            Register(new TalentDef
            {
                id = "poisoner",
                name = "Poisoner",
                description = "Master of toxins: increases the damage potency of Poison applied by this character by +25%.",
                category = TalentCategory.Shared,
                poisonPotencyBonus = PoisonerPotencyBonus
            });

            // 3.7 Crusher (TBD)
            Register(new TalentDef
            {
                id = "crusher",
                name = "Crusher",
                description = "Heavy impact: increases the character's chance to inflict Stun on enemies by +15%.",
                category = TalentCategory.Shared,
                stunChanceBonus = CrusherStunBonus
            });

            // 3.8 Slasher (TBD)
            Register(new TalentDef
            {
                id = "slasher",
                name = "Slasher",
                description = "Deep lacerations: increases the character's chance to inflict Bleed on enemies by +15%.",
                category = TalentCategory.Shared,
                bleedChanceBonus = SlasherBleedBonus
            });

            // 3.9 Piercer (TBD)
            Register(new TalentDef
            {
                id = "piercer",
                name = "Piercer",
                description = "Armor penetration: ignores 20% of the target's Physical Resistance when attacking.",
                category = TalentCategory.Shared,
                physicalResistancePenetration = PiercerPenetration
            });

            // 3.10 Lucky
            Register(new TalentDef
            {
                id = "lucky",
                name = "Lucky",
                description = "Gives a 20% chance to retry a failed QTE. Max 1 retry per QTE, max 3 activations per battle.",
                category = TalentCategory.Shared,
                qteRetryChance = LuckyActivationChance,
                maxRetriesPerQte = LuckyMaxPerQte,
                maxRetriesPerBattle = LuckyMaxPerBattle
            });

            // 3.11 Inspiring Leader
            Register(new TalentDef
            {
                id = "inspiring_leader",
                name = "Inspiring Leader",
                description = "Inspires the party: grants 10% Damage Reduction to the entire party for the first 3 turns while alive.",
                category = TalentCategory.Shared,
                partyDamageReduction = InspiringLeaderReduction,
                partyDamageReductionTurns = InspiringLeaderDuration
            });
        }

        static void RegisterAdditionalSharedTalents()
        {
            // 3.12 Hardy Constitution (formerly Vitality)
            Register(new TalentDef
            {
                id = "hardy_constitution",
                name = "Hardy Constitution",
                description = "Increases Max HP by +45 and increases Healing Received by +10%.",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.13 Combat Momentum (formerly Battle Rhythm)
            Register(new TalentDef
            {
                id = "combat_momentum",
                name = "Combat Momentum",
                description = "Increases Initiative by +8 and Action Speed by +10 in the turn order.",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.14 Unshakeable (formerly Iron Will)
            Register(new TalentDef
            {
                id = "unshakeable",
                name = "Unshakeable",
                description = "Increases Mental Resistance by +15% and Stagger Resistance by +10%.",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.15 Spellcraft Mastery (formerly Efficient Casting)
            Register(new TalentDef
            {
                id = "spellcraft_mastery",
                name = "Spellcraft Mastery",
                description = "Increases Magic Power by +12% and Magic Penetration by +10.",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.16 Quick Reflex
            Register(new TalentDef
            {
                id = "quick_reflex",
                name = "Quick Reflex",
                description = "Widens Defensive QTE timing windows by +10% (stacks up to the +25% shared cap).",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.17 Spoils Seeker (formerly Fortune Seeker)
            Register(new TalentDef
            {
                id = "spoils_seeker",
                name = "Spoils Seeker",
                description = "Increases Spoils (Gold) gained from battles and treasures by +15% while alive (non-stacking).",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.18 Pinpoint Focus (formerly Keen Eye)
            Register(new TalentDef
            {
                id = "pinpoint_focus",
                name = "Pinpoint Focus",
                description = "Increases Critical Chance by +6% and Critical Damage by +15%.",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.19 Steady Hand
            Register(new TalentDef
            {
                id = "steady_hand",
                name = "Steady Hand",
                description = "Widens Offensive QTE timing windows by +10% (stacks up to the +25% shared cap).",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });

            // 3.20 Desperate Grit (formerly Survivor's Instinct)
            Register(new TalentDef
            {
                id = "desperate_grit",
                name = "Desperate Grit",
                description = "When HP is at or below 35% Max HP, deals +15% All Damage and takes -15% All Damage.",
                category = TalentCategory.Shared,
                isDesignFinalized = true
            });
        }

        static void RegisterAllClassSpecificTalents()
        {
            // Section 5.1: Proposed Class-Specific Talent Names (9 Classes x 5 Talents = 45 Talents)
            var classTalents = new (string classId, string[] names)[]
            {
                ("fighter", new[] { "Weapon Adept", "Guard Specialist", "Tactical Opening", "Weapon Precision", "Master of Arms" }),
                ("barbarian", new[] { "Bloodlust", "Unbreakable Rage", "Berserk Fury", "Raging Momentum", "Last Stand" }),
                ("martial_artist", new[] { "Flowing Strikes", "Perfect Timing", "Combo Discipline", "Finisher Mastery", "Unbroken Rhythm" }),
                ("rogue", new[] { "Opportunist", "Exploit Weakness", "Dirty Fighting", "Shadow Step", "Executioner" }),
                ("ranger", new[] { "Hunter's Instinct", "Marked Prey", "Deadly Precision", "Longshot", "Predator's Patience" }),
                ("cleric", new[] { "Divine Grace", "Sanctuary", "Holy Retribution", "Guardian's Resolve", "Last Benediction" }),
                ("wizard", new[] { "Elemental Adept", "Focused Casting", "Elemental Insight", "Arcane Efficiency", "Elemental Mastery" }),
                ("warlock", new[] { "Lingering Malice", "Curse Potency", "Dark Knowledge", "Soul Harvester", "Doom Weaver" }),
                ("druid", new[] { "Nature's Embrace", "Wild Growth", "Root Mastery", "Nature's Wrath", "Primal Balance" })
            };

            foreach (var (cid, names) in classTalents)
            {
                foreach (var tName in names)
                {
                    string id = $"{cid}_{tName.ToLowerInvariant().Replace(" ", "_").Replace("'", "")}";
                    Register(new TalentDef
                    {
                        id = id,
                        name = tName,
                        description = $"{tName} ({cid.ToUpperInvariant()} Class Talent — pending detailed design review).",
                        category = TalentCategory.ClassSpecific,
                        targetClassId = cid,
                        isDesignFinalized = false
                    });
                }
            }
        }

        public static TalentDef GetTalent(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return AllTalents.TryGetValue(id, out var t) ? t : null;
        }

        // Returns all valid, unselected talents for a character (Shared + matching Class-Specific)
        public static List<TalentDef> GetAvailableTalentsFor(Unit unit)
        {
            if (unit == null) return new List<TalentDef>();
            string classId = unit.classDef?.id ?? "";

            return AllTalents.Values
                .Where(t => !unit.HasTalent(t.id) && t.IsCompatibleWithClass(classId))
                .OrderBy(t => t.category)
                .ThenBy(t => t.name)
                .ToList();
        }

        // Section 2.1: Generates a 3-choice draft offer (1 Shared + 2 Class-Specific)
        // Proposal pending confirmation; gracefully falls back if remaining pool is limited.
        public static List<TalentDef> GenerateDraftOffer(Unit unit, System.Random rng = null)
        {
            if (unit == null) return new List<TalentDef>();
            var rand = rng ?? new System.Random();

            var available = GetAvailableTalentsFor(unit);
            var sharedAvailable = available.Where(t => t.category == TalentCategory.Shared).OrderBy(_ => rand.Next()).ToList();
            var classAvailable = available.Where(t => t.category == TalentCategory.ClassSpecific).OrderBy(_ => rand.Next()).ToList();

            var offer = new List<TalentDef>();

            // 1 Shared Talent
            if (sharedAvailable.Count > 0)
            {
                offer.Add(sharedAvailable[0]);
                sharedAvailable.RemoveAt(0);
            }

            // 2 Class-Specific Talents
            for (int i = 0; i < 2 && classAvailable.Count > 0; i++)
            {
                offer.Add(classAvailable[0]);
                classAvailable.RemoveAt(0);
            }

            // Fallback: Fill up to 3 choices from any remaining available talents if needed
            while (offer.Count < 3 && (sharedAvailable.Count > 0 || classAvailable.Count > 0))
            {
                if (classAvailable.Count > 0)
                {
                    offer.Add(classAvailable[0]);
                    classAvailable.RemoveAt(0);
                }
                else if (sharedAvailable.Count > 0)
                {
                    offer.Add(sharedAvailable[0]);
                    sharedAvailable.RemoveAt(0);
                }
            }

            return offer;
        }

        public static bool ValidateSelection(Unit unit, string talentId, out string error)
        {
            if (unit == null)
            {
                error = "Unit is null.";
                return false;
            }
            if (string.IsNullOrEmpty(talentId))
            {
                error = "Talent ID is empty.";
                return false;
            }
            if (!AllTalents.TryGetValue(talentId, out var talent))
            {
                error = $"Talent '{talentId}' does not exist.";
                return false;
            }
            if (unit.HasTalent(talentId))
            {
                error = $"Character '{unit.name}' already has Talent '{talent.name}'. Duplicate selections are forbidden.";
                return false;
            }
            if (unit.AvailableTalentSelections <= 0)
            {
                error = $"Character '{unit.name}' has no available Talent selection opportunities (Level {unit.level}).";
                return false;
            }
            if (!talent.IsCompatibleWithClass(unit.classDef?.id))
            {
                error = $"Talent '{talent.name}' is not compatible with class '{unit.classDef?.name}'.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TrySelectTalent(Unit unit, string talentId, out string error)
        {
            if (!ValidateSelection(unit, talentId, out error))
                return false;

            unit.selectedTalentIds.Add(talentId);
            unit.RecalculateStats();
            error = null;
            return true;
        }

        public static void Respec(Unit unit)
        {
            if (unit == null) return;
            unit.selectedTalentIds.Clear();
            unit.RecalculateStats();
        }
    }
}

