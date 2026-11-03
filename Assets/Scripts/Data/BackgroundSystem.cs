using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Background Perk Rank milestones (Background System Section 2).
    public static class BackgroundRankResolver
    {
        public const int Rank1UnlockLevel = 1;
        public const int Rank2UnlockLevel = 4;
        public const int Rank3UnlockLevel = 7;
        public const int Rank4UnlockLevel = 10;
        public const int Rank5UnlockLevel = 13;
        public const int MaxRank = 5;

        // Determines the highest unlocked Background Rank based on Character Level (1-15).
        // Only the highest unlocked Rank is active; previous Ranks do NOT stack.
        public static int GetRankForLevel(int level)
        {
            if (level >= Rank5UnlockLevel) return 5;
            if (level >= Rank4UnlockLevel) return 4;
            if (level >= Rank3UnlockLevel) return 3;
            if (level >= Rank2UnlockLevel) return 2;
            return 1; // Rank I is unlocked at Level 1
        }
    }

    // Specification for a single Rank of a Background Perk (Background System Section 3).
    [Serializable]
    public class BackgroundRankDef
    {
        public int rank = 1;
        public int requiredLevel = 1;
        public string perkName = "";
        public string perkDescription = "";

        // Stat modifiers contributing to StatCalculator derived stats (e.g. MaxHP, MaxMP, StatusResistance, Speed)
        public StatModifier[] modifiers = Array.Empty<StatModifier>();

        // 1. Acolyte
        public float healingDealtBonus;             // Rank I: 0.05, II: 0.08, III: 0.10, IV: 0.12, V: 0.15
        public float healingReceivedBonus;          // Rank III: 0.05, IV: 0.08, V: 0.10

        // 2. Charlatan
        public float damageVsDebuffedBonus;         // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.10, V: 0.12
        public float firstAttackUnactedBonus;       // Rank V: 0.05

        // 3. Guard
        public float physicalDamageTakenReduction;  // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.09, V: 0.10
        public float lowHpPhysicalDamageTakenReduction; // Rank V: 0.05 (when HP < 30%)

        // 4. Hermit
        public int statusResistanceBonus;           // Rank I: 5, II: 8, III: 10, IV: 12, V: 15
        public float statusResistMpRestorePercent;  // Rank V: 0.03 (restore 3% Max MP on resist, max once/turn)

        // 5. Farmer
        public float maxHpPercentBonus;             // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.09, V: 0.10
        public float postBattleHpRestorePercent;    // Rank V: 0.03 (restore 3% Max HP after battle)

        // 6. Noble
        public float maxMpPercentBonus;             // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.09, V: 0.10
        public float postBattleMpRestorePercent;    // Rank V: 0.05 (restore 5% Max MP after battle)

        // 7. Sage
        public float mpCostReductionPercent;        // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.09, V: 0.10
        public float skillMpRefundChance;           // Rank V: 0.10 (10% chance)
        public float skillMpRefundPercent;          // Rank V: 0.05 (restore 5% Max MP)

        // 8. Soldier
        public float physicalDamageBonus;           // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.09, V: 0.10
        public float onDamagedNextPhysicalDamageBonus; // Rank V: 0.05 (next physical damage instance +5%)

        // 9. Wayfarer
        public float speedPercentBonus;             // Rank I: 0.03, II: 0.05, III: 0.07, IV: 0.09, V: 0.10
        public float battleStartSpeedBuffChance;    // Rank V: 0.10 (10% chance)
        public float battleStartSpeedBuffPercent;   // Rank V: 0.10 (+10% Speed)
        public int battleStartSpeedBuffDuration;    // Rank V: 2 turns

        // 10. Scribe
        public int accuracyBonus;                   // Rank I: 3, II: 5, III: 7, IV: 9, V: 10
        public int scribeTargetAttackThreshold;     // Rank V: 3 attacks
        public float scribeNextAttackCritBonus;     // Rank V: 0.05 (+5% Crit Chance)
    }

    // Complete Background Definition (Background System Section 3).
    [Serializable]
    public class BackgroundDef
    {
        public string id = "";
        public string name = "";
        public string narrativeIdentity = "";
        public string description = "";
        public string perkName = "";
        public string perkDescription = "";

        // The 5 rank milestones (Rank I at Lv 1, II at Lv 4, III at Lv 7, IV at Lv 10, V at Lv 13)
        public BackgroundRankDef[] ranks = Array.Empty<BackgroundRankDef>();

        // Legacy compatibility fields
        public string[] proficiencies = Array.Empty<string>();
        public string[] eventInteractions = Array.Empty<string>();
        public int socialBonus;
        public StatModifier[] passiveEffects = Array.Empty<StatModifier>();

        // Resolves the active rank definition for a given character level (1-15).
        public BackgroundRankDef GetRank(int level)
        {
            if (ranks == null || ranks.Length == 0) return null;
            int rankNum = BackgroundRankResolver.GetRankForLevel(level);
            int idx = Math.Clamp(rankNum - 1, 0, ranks.Length - 1);
            return ranks[idx];
        }

        public int GetActiveRankNumber(int level) => BackgroundRankResolver.GetRankForLevel(level);

        // Dynamically returns the active stat modifiers for the character level.
        // Ensures only the highest unlocked Rank is active and old ranks do NOT stack.
        public StatModifier[] GetActiveModifiers(int level)
        {
            var rankDef = GetRank(level);
            if (rankDef?.modifiers != null && rankDef.modifiers.Length > 0)
                return rankDef.modifiers;
            return passiveEffects ?? Array.Empty<StatModifier>();
        }
    }
}

