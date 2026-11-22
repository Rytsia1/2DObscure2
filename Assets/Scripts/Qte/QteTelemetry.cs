using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Obscure
{
    // QTE telemetry (QTE_Variation_Handoff.md Stage 0): tracks Good+ and Perfect rates by motif/move, variant, and tier,
    // plus exposure count so mastery (1st vs 3rd exposure) and variant pass tests can be measured.
    public sealed class QteTelemetryBucket
    {
        public int exposures;
        public int totalCues;
        public int goodPlus;
        public int perfect;

        public double GoodPlusRate => totalCues > 0 ? 100.0 * goodPlus / totalCues : 0.0;
        public double PerfectRate => totalCues > 0 ? 100.0 * perfect / totalCues : 0.0;
    }

    public sealed class QteTelemetry
    {
        public readonly Dictionary<Tier, QteTelemetryBucket> ByTier = new Dictionary<Tier, QteTelemetryBucket>();
        public readonly Dictionary<string, QteTelemetryBucket> ByVariant = new Dictionary<string, QteTelemetryBucket>();
        public readonly Dictionary<string, QteTelemetryBucket> ByMotif = new Dictionary<string, QteTelemetryBucket>();

        public void Record(Tier tier, string motifKey, string variantKey, QteResult result)
        {
            RecordBucket(ByTier, tier, result);
            if (!string.IsNullOrEmpty(variantKey)) RecordBucket(ByVariant, variantKey, result);
            if (!string.IsNullOrEmpty(motifKey)) RecordBucket(ByMotif, motifKey, result);
        }

        public void RecordExposure(Tier tier, string motifKey, string variantKey)
        {
            GetOrCreate(ByTier, tier).exposures++;
            if (!string.IsNullOrEmpty(variantKey)) GetOrCreate(ByVariant, variantKey).exposures++;
            if (!string.IsNullOrEmpty(motifKey)) GetOrCreate(ByMotif, motifKey).exposures++;
        }

        static void RecordBucket<TKey>(Dictionary<TKey, QteTelemetryBucket> map, TKey key, QteResult r)
        {
            var b = GetOrCreate(map, key);
            b.totalCues++;
            if (r == QteResult.Perfect) { b.perfect++; b.goodPlus++; }
            else if (r == QteResult.Good) { b.goodPlus++; }
        }

        static QteTelemetryBucket GetOrCreate<TKey>(Dictionary<TKey, QteTelemetryBucket> map, TKey key)
        {
            if (!map.TryGetValue(key, out var b)) map[key] = b = new QteTelemetryBucket();
            return b;
        }

        public string Summary()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            foreach (var kv in ByTier)
                sb.AppendLine(string.Format(inv, "Tier {0}: exp={1}, cues={2}, Good+={3:F1}%, Perfect={4:F1}%",
                    kv.Key, kv.Value.exposures, kv.Value.totalCues, kv.Value.GoodPlusRate, kv.Value.PerfectRate));
            foreach (var kv in ByVariant)
                sb.AppendLine(string.Format(inv, "Variant {0}: exp={1}, cues={2}, Good+={3:F1}%, Perfect={4:F1}%",
                    kv.Key, kv.Value.exposures, kv.Value.totalCues, kv.Value.GoodPlusRate, kv.Value.PerfectRate));
            return sb.ToString();
        }
    }
}
