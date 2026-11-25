using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Enemy display tier for per-status resistance (Felna_Analysis_System_Spec_v1.md Part F).
    public enum StatusResistTier { Vulnerable, Normal, Resistant, Immune }

    // Status Resistance + Mental Resistance profile (GDD Bab 11 §9 & §13; Analysis System Spec Part F).
    // Two separate pools: Mental Effects roll against Mental Resistance only, every other status against Status Resistance only.
    // Each pool: sources stack additively, per-status offsets stack on top, the total clamps to [-85%, +85%].
    // Resistance Reduction debuffs stack multiplicatively on both pools and only shrink a positive value.
    // Bosses are 100% immune to Mental Effects; neither Mental Resistance nor reductions bypass immunity.
    [Serializable]
    public class StatusResistanceProfile
    {
        // Posture B (designer decision, 2026-11-25): +/-85% per pool, neutral 0.
        // Test: a fully stacked endgame build is still hit by a 100%-chance status ~15% of the time; if "never affected", lower the cap.
        public const float MaxResistance = 0.85f;
        public const float MinResistance = -0.85f;

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

        // Mental pool. Equipment & passives already arrive through the derived stat (mentalStatsResistance).
        public float mentalStatsResistance;
        public float mentalBuffResistance;

        // Per-status resistance offsets in [-0.85, +0.85] (Spec Part F).
        public Dictionary<StatusId, float> perStatus = new Dictionary<StatusId, float>();

        // Explicit per-status immunity flags (separate from perStatus; never bypassed by reductions).
        public HashSet<StatusId> immunities = new HashSet<StatusId>();

        [SerializeField] private List<float> resistanceReductions = new List<float>();

        public IReadOnlyList<float> ResistanceReductions => resistanceReductions;

        // Status pool (non-Mental statuses).
        public float TotalUncappedAdditive =>
            baseResistance + equipmentResistance + statsResistance + buffResistance + passivesResistance;

        public float TotalMentalUncapped => mentalStatsResistance + mentalBuffResistance;

        float PoolTotal(StatusId id) => StatusInfo.IsMental(id) ? TotalMentalUncapped : TotalUncappedAdditive;

        // R_initial = clamp(R_base + R_equipment + R_stats + R_buffs + R_passives, -85%, +85%)
        public float InitialResistance =>
            Mathf.Clamp(TotalUncappedAdditive, MinResistance, MaxResistance);

        public float GetPerStatusResistance(StatusId id) =>
            perStatus != null && perStatus.TryGetValue(id, out var v) ? Mathf.Clamp(v, MinResistance, MaxResistance) : 0f;

        public void SetPerStatusResistance(StatusId id, float value)
        {
            perStatus ??= new Dictionary<StatusId, float>();
            perStatus[id] = Mathf.Clamp(value, MinResistance, MaxResistance);
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

        // Reductions only shrink a positive resistance; a negative value is left as is (otherwise a debuff would help the target).
        float ApplyReductions(float raw) => raw > 0f ? raw * ReductionProduct() : raw;

        // Status Resistance (no per-status offset), for display: R_after = R_initial * Product(1 - D_i) when positive.
        public float GetEffectiveResistance() => ApplyReductions(InitialResistance);

        // Mental Resistance (no per-status offset), for display.
        public float GetEffectiveMentalResistance() =>
            ApplyReductions(Mathf.Clamp(TotalMentalUncapped, MinResistance, MaxResistance));

        // Per-status effective resistance (Spec Part F):
        // effective(S) = clamp(pool(S) + perStatus[S], -0.85, 0.85), times Product(1 - reductions) when positive.
        public float GetEffectiveResistance(StatusId id) =>
            ApplyReductions(Mathf.Clamp(PoolTotal(id) + GetPerStatusResistance(id), MinResistance, MaxResistance));

        // GDD Section 13 & Spec Part F: Bosses are immune to all Mental Effects; explicit per-status immunity flags stay separate.
        // Mental Resistance and Resistance Reduction never bypass immunity.
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
                passivesResistance = passivesResistance,
                mentalStatsResistance = mentalStatsResistance,
                mentalBuffResistance = mentalBuffResistance
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

