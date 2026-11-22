using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Enemy display tier for per-status resistance (Felna_Analysis_System_Spec_v1.md Part F).
    public enum StatusResistTier { Vulnerable, Normal, Resistant, Immune }

    // Universal + Per-Status Effect Resistance profile (GDD Section 9 & 13; Analysis System Spec Part F).
    // Universal sources stack additively; per-status offsets stack on top and clamp to [0, 75%].
    // Resistance Reduction debuffs stack multiplicatively as percentage reductions.
    // Bosses are 100% immune to Mental Effects; resistance reduction does NOT bypass immunity.
    [Serializable]
    public class StatusResistanceProfile
    {
        public const float MaxUniversalResistance = 0.75f; // 75% cap
        public const float MinUniversalResistance = 0.0f;  // 0% floor
        public const float MinPerStatusResistance = -0.50f;
        public const float MaxPerStatusResistance = 0.75f;

        // Posture-B starting values (Felna_Analysis_System_Spec_v1.md Part F / Starting values):
        // Status tier cut-offs: <= -10% Vulnerable, > +15% Resistant, between = Normal, immunity flag = Immune.
        // Pass test: Players predict application success from the tier with >= 70% accuracy.
        // If it fails: move the cut-offs.
        public const float VulnerableCutoff = -0.10f;
        public const float ResistantCutoff = 0.15f;

        public float baseResistance;
        public float equipmentResistance;
        public float statsResistance;
        public float buffResistance;
        public float passivesResistance;

        // Per-status resistance offsets in [-0.50, +0.75] (Spec Part F).
        public Dictionary<StatusId, float> perStatus = new Dictionary<StatusId, float>();

        // Explicit per-status immunity flags (separate from perStatus; never bypassed by reductions).
        public HashSet<StatusId> immunities = new HashSet<StatusId>();

        [SerializeField] private List<float> resistanceReductions = new List<float>();

        public IReadOnlyList<float> ResistanceReductions => resistanceReductions;

        public float TotalUncappedAdditive =>
            baseResistance + equipmentResistance + statsResistance + buffResistance + passivesResistance;

        // R_initial = min(R_base + R_equipment + R_stats + R_buffs + R_passives, 75%)
        public float InitialResistance =>
            Mathf.Clamp(TotalUncappedAdditive, MinUniversalResistance, MaxUniversalResistance);

        public float GetPerStatusResistance(StatusId id) =>
            perStatus != null && perStatus.TryGetValue(id, out var v) ? Mathf.Clamp(v, MinPerStatusResistance, MaxPerStatusResistance) : 0f;

        public void SetPerStatusResistance(StatusId id, float value)
        {
            perStatus ??= new Dictionary<StatusId, float>();
            perStatus[id] = Mathf.Clamp(value, MinPerStatusResistance, MaxPerStatusResistance);
        }

        public void SetImmune(StatusId id, bool immune = true)
        {
            immunities ??= new HashSet<StatusId>();
            if (immune) immunities.Add(id);
            else immunities.Remove(id);
        }

        public void AddReduction(float percentReduction)
        {
            if (percentReduction > 0f)
                resistanceReductions.Add(Mathf.Clamp01(percentReduction));
        }

        public bool RemoveReduction(float percentReduction) =>
            resistanceReductions.Remove(percentReduction);

        public void ClearReductions() => resistanceReductions.Clear();

        float ReductionProduct()
        {
            float reductionProduct = 1f;
            for (int i = 0; i < resistanceReductions.Count; i++)
            {
                reductionProduct *= Mathf.Max(0f, 1f - resistanceReductions[i]);
            }
            return reductionProduct;
        }

        // Universal effective resistance: R_after = max(0, R_initial * Product(1 - D_i)) (GDD Section 9)
        public float GetEffectiveResistance()
        {
            float r = InitialResistance;
            if (r <= 0f) return 0f;
            return Mathf.Max(0f, r * ReductionProduct());
        }

        // Per-status effective resistance (Spec Part F):
        // effective(S) = clamp(universal + perStatus[S], 0, 0.75) * Product(1 - reductions)
        public float GetEffectiveResistance(StatusId id)
        {
            float r = Mathf.Clamp(TotalUncappedAdditive + GetPerStatusResistance(id), MinUniversalResistance, MaxUniversalResistance);
            if (r <= 0f) return 0f;
            return Mathf.Max(0f, r * ReductionProduct());
        }

        // GDD Section 13 & Spec Part F: Bosses are immune to all Mental Effects; explicit per-status immunity flags stay separate.
        // Status Effect Resistance Reduction does NOT bypass immunity.
        public bool IsImmune(StatusId id, bool isBoss = false)
        {
            if (isBoss && StatusInfo.IsMental(id))
                return true;
            if (immunities != null && immunities.Contains(id))
                return true;
            return false;
        }

        // Enemy display tier for a status (Spec Part F): Vulnerable (<= -10%), Normal, Resistant (> +15%), Immune.
        public StatusResistTier GetTier(StatusId id, bool isBoss = false)
        {
            if (IsImmune(id, isBoss))
                return StatusResistTier.Immune;
            float ps = GetPerStatusResistance(id);
            if (ps <= VulnerableCutoff + 1e-5f)
                return StatusResistTier.Vulnerable;
            if (ps > ResistantCutoff + 1e-5f)
                return StatusResistTier.Resistant;
            return StatusResistTier.Normal;
        }

        // P_final = P_base * (1 - R_effective(S)) (GDD Section 9 & Spec Part F)
        public float CalculateFinalApplicationChance(float baseChance, StatusId id, bool isBoss = false)
        {
            if (IsImmune(id, isBoss))
                return 0f;

            float effectiveRes = GetEffectiveResistance(id);
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
            if (perStatus != null)
                foreach (var kv in perStatus) copy.perStatus[kv.Key] = kv.Value;
            if (immunities != null)
                foreach (var im in immunities) copy.immunities.Add(im);
            return copy;
        }
    }
}

