using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    /// <summary>
    /// Rolls procedural enemy compositions from an EncounterDef template while keeping total threat
    /// within ±10% of the encounter's authored reference lineup.
    /// </summary>
    public static class EncounterRoller
    {
        public const float Tolerance = 0.10f;
        public const int MaxEnemies = 4;
        public const int MaxSupports = 1;
        public const int MaxTries = 50;

        public static float Threat(EnemyDef e)
        {
            if (e == null) return 0f;
            if (e.threatOverride > 0f) return e.threatOverride;
            float dmg = Math.Max(1f, Math.Max(e.stats.atk, e.stats.mag));
            float baseThreat = (float)Math.Sqrt(Math.Max(1, e.stats.maxHp) * dmg);
            return IsSupportDef(e) ? baseThreat * 1.25f : baseThreat;
        }

        public static float Threat(string enemyId) =>
            !string.IsNullOrEmpty(enemyId) && Database.Enemies.TryGetValue(enemyId, out var e) ? Threat(e) : 0f;

        public static float Budget(EncounterDef enc) =>
            enc?.enemies == null ? 0f : enc.enemies.Sum(Threat);

        public static bool IsSupport(string enemyId) =>
            !string.IsNullOrEmpty(enemyId) && Database.Enemies.TryGetValue(enemyId, out var e) && IsSupportDef(e);

        static bool IsSupportDef(EnemyDef e) =>
            e.aiRole == AiRole.Support || (e.attacks != null && e.attacks.Any(a => a.SupportsAlly));

        public static string[] Roll(EncounterDef enc, Random rng)
        {
            if (enc?.enemies == null) return Array.Empty<string>();
            if (!enc.Procedural || rng == null) return (string[])enc.enemies.Clone();

            var anchors = enc.anchors ?? Array.Empty<string>();
            int minCount = Math.Clamp(enc.minCount > 0 ? enc.minCount : enc.enemies.Length, Math.Max(1, anchors.Length), MaxEnemies);
            int maxCount = Math.Clamp(enc.maxCount > 0 ? enc.maxCount : enc.enemies.Length, minCount, MaxEnemies);

            float budget = Budget(enc);
            float minThreat = budget * (1f - Tolerance);
            float maxThreat = budget * (1f + Tolerance);

            var nonSupportPool = enc.pool.Where(id => !IsSupport(id)).ToArray();

            for (int attempt = 0; attempt < MaxTries; attempt++)
            {
                int targetCount = rng.Next(minCount, maxCount + 1);
                var picked = new List<string>(targetCount);
                picked.AddRange(anchors);
                int supports = picked.Count(IsSupport);

                while (picked.Count < targetCount)
                {
                    var activePool = supports >= MaxSupports ? nonSupportPool : enc.pool;
                    if (activePool.Length == 0) break;
                    string candidate = activePool[rng.Next(activePool.Length)];
                    picked.Add(candidate);
                    if (IsSupport(candidate)) supports++;
                }

                if (picked.Count != targetCount) continue;

                float total = picked.Sum(Threat);
                if (total >= minThreat && total <= maxThreat)
                    return picked.ToArray();
            }

            return (string[])enc.enemies.Clone();
        }
    }
}
