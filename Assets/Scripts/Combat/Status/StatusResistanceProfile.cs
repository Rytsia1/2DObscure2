using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Universal Status Effect Resistance profile (GDD Section 9 & 13).
    // Sources stack additively to a 75% cap.
    // Resistance Reduction debuffs stack multiplicatively as percentage reductions.
    // Bosses are 100% immune to Mental Effects; resistance reduction does NOT bypass immunity.
    [Serializable]
    public class StatusResistanceProfile
    {
        public const float MaxUniversalResistance = 0.75f; // 75% cap
        public const float MinUniversalResistance = 0.0f;  // 0% floor

        public float baseResistance;
        public float equipmentResistance;
        public float statsResistance;
        public float buffResistance;
        public float passivesResistance;

        [SerializeField] private List<float> resistanceReductions = new List<float>();

        public IReadOnlyList<float> ResistanceReductions => resistanceReductions;

        public float TotalUncappedAdditive =>
            baseResistance + equipmentResistance + statsResistance + buffResistance + passivesResistance;

        // R_initial = min(R_base + R_equipment + R_stats + R_buffs + R_passives, 75%)
        public float InitialResistance =>
            Mathf.Clamp(TotalUncappedAdditive, MinUniversalResistance, MaxUniversalResistance);

        public void AddReduction(float percentReduction)
        {
            if (percentReduction > 0f)
                resistanceReductions.Add(Mathf.Clamp01(percentReduction));
        }

        public bool RemoveReduction(float percentReduction) =>
            resistanceReductions.Remove(percentReduction);

        public void ClearReductions() => resistanceReductions.Clear();

        // R_after = max(0, R_initial * Product(1 - D_i)) (GDD Section 9)
        public float GetEffectiveResistance()
        {
            float r = InitialResistance;
            if (r <= 0f) return 0f;

            float reductionProduct = 1f;
            for (int i = 0; i < resistanceReductions.Count; i++)
            {
                reductionProduct *= Mathf.Max(0f, 1f - resistanceReductions[i]);
            }
            return Mathf.Max(0f, r * reductionProduct);
        }

        // GDD Section 13: Bosses are immune to all Mental Effects.
        // Status Effect Resistance Reduction does NOT bypass Mental Effect immunity.
        public bool IsImmune(StatusId id, bool isBoss = false)
        {
            if (isBoss && StatusInfo.IsMental(id))
                return true;
            return false;
        }

        // P_final = P_base * (1 - R_effective) (GDD Section 9)
        public float CalculateFinalApplicationChance(float baseChance, StatusId id, bool isBoss = false)
        {
            if (IsImmune(id, isBoss))
                return 0f;

            float effectiveRes = GetEffectiveResistance();
            float finalChance = baseChance * (1f - effectiveRes);
            return Mathf.Clamp01(finalChance);
        }

        public bool RollApplication(float baseChance, StatusId id, bool isBoss = false, Func<float> rngZeroToOne = null)
        {
            if (IsImmune(id, isBoss)) return false;

            float pFinal = CalculateFinalApplicationChance(baseChance, id, isBoss);
            if (pFinal <= 0f) return false;
            if (pFinal >= 1f) return true;

            float roll = rngZeroToOne != null ? rngZeroToOne() : UnityEngine.Random.value;
            return roll < pFinal;
        }

        public StatusResistanceProfile Clone()
        {
            var copy = new StatusResistanceProfile
            {
                baseResistance = baseResistance,
                equipmentResistance = equipmentResistance,
                statsResistance = statsResistance,
                buffResistance = buffResistance,
                passivesResistance = passivesResistance
            };
            copy.resistanceReductions.AddRange(resistanceReductions);
            return copy;
        }
    }
}

