using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    [Serializable]
    public struct CodexElementRecord
    {
        public Element element;
        public Effectiveness effectiveness;
    }

    [Serializable]
    public struct CodexStatusRecord
    {
        public StatusId status;
        public StatusResistTier tier;
    }

    [Serializable]
    public struct CodexConcealedRecord
    {
        public Element element;
        public int triggers;
    }

    [Serializable]
    public class CodexEntrySaveData
    {
        public string enemyId;
        public CodexElementRecord[] elements = Array.Empty<CodexElementRecord>();
        public CodexStatusRecord[] statuses = Array.Empty<CodexStatusRecord>();
        public CodexConcealedRecord[] concealedTriggers = Array.Empty<CodexConcealedRecord>();
    }

    [Serializable]
    public class CodexSaveData
    {
        public CodexEntrySaveData[] entries = Array.Empty<CodexEntrySaveData>();
    }

    public struct CodexRevealEvent
    {
        public string enemyId;
        public string enemyName;
        public bool isElement;
        public Element element;
        public Effectiveness effectiveness;
        public StatusId status;
        public StatusResistTier statusTier;
        public bool concealedPing; // true on concealed trigger < threshold ("?!")
        public bool newlyRevealed; // true on first permanent reveal in this Codex
    }

    // Per-save-slot progressive enemy discovery Codex (Felna_Analysis_System_Spec_v1.md Part D).
    // Keyed by EnemyDef.id so base forms, Enhanced forms, and each boss phase are separate entries;
    // summons share their definition's entry.
    public class EnemyCodex
    {
        // Posture-B starting value (Felna_Analysis_System_Spec_v1.md Part D / Starting values):
        // ConcealedRevealThreshold = 2 triggers before a concealed boss affinity is revealed.
        // Pass test: Players describe the reveal as "earned, not random".
        // If it fails: tune down to 1 trigger.
        public const int ConcealedRevealThreshold = 2;

        sealed class Entry
        {
            public readonly Dictionary<Element, Effectiveness> Elements = new Dictionary<Element, Effectiveness>();
            public readonly Dictionary<StatusId, StatusResistTier> Statuses = new Dictionary<StatusId, StatusResistTier>();
            public readonly Dictionary<Element, int> ConcealedTriggers = new Dictionary<Element, int>();
        }

        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        Entry GetOrCreate(string enemyId)
        {
            if (!entries.TryGetValue(enemyId, out var e))
                entries[enemyId] = e = new Entry();
            return e;
        }

        static Element CanonicalElement(Element e) => e == Element.Shadow ? Element.Dark : e;

        public Effectiveness? GetKnownElement(string enemyId, Element element)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;
            var el = CanonicalElement(element);
            return entries.TryGetValue(enemyId, out var e) && e.Elements.TryGetValue(el, out var eff)
                ? eff
                : (Effectiveness?)null;
        }

        public StatusResistTier? GetKnownStatus(string enemyId, StatusId status)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;
            return entries.TryGetValue(enemyId, out var e) && e.Statuses.TryGetValue(status, out var tier)
                ? tier
                : (StatusResistTier?)null;
        }

        public int GetConcealedTriggerCount(string enemyId, Element element)
        {
            if (string.IsNullOrEmpty(enemyId)) return 0;
            var el = CanonicalElement(element);
            return entries.TryGetValue(enemyId, out var e) && e.ConcealedTriggers.TryGetValue(el, out var cnt) ? cnt : 0;
        }

        public bool HasEntry(string enemyId) => !string.IsNullOrEmpty(enemyId) && entries.ContainsKey(enemyId);

        public static bool IsConcealed(EnemyDef def, Element element)
        {
            if (def?.concealedAffinities == null || def.concealedAffinities.Length == 0) return false;
            var el = CanonicalElement(element);
            for (int i = 0; i < def.concealedAffinities.Length; i++)
                if (CanonicalElement(def.concealedAffinities[i]) == el) return true;
            return false;
        }

        // Called whenever a damaging hit of element resolves against an enemy (attack, skill, counter, or elemental DoT).
        // NORMAL is recorded too, so tested-and-normal displays NORMAL instead of ???.
        public CodexRevealEvent RecordElementHit(EnemyDef def, Element element, Effectiveness rawEffectiveness)
        {
            var el = CanonicalElement(element);
            var ev = new CodexRevealEvent
            {
                enemyId = def?.id ?? "",
                enemyName = def?.name ?? "",
                isElement = true,
                element = el,
                effectiveness = rawEffectiveness,
            };
            if (def == null || string.IsNullOrEmpty(def.id)) return ev;

            var entry = GetOrCreate(def.id);
            if (entry.Elements.ContainsKey(el))
                return ev;

            if (IsConcealed(def, el))
            {
                int count = (entry.ConcealedTriggers.TryGetValue(el, out var prev) ? prev : 0) + 1;
                entry.ConcealedTriggers[el] = count;
                if (count < ConcealedRevealThreshold)
                {
                    ev.concealedPing = true;
                    return ev;
                }
            }

            entry.Elements[el] = rawEffectiveness;
            ev.newlyRevealed = true;
            return ev;
        }

        // Called whenever a status application attempt resolves against an enemy (succeeded, resisted, or immune).
        public CodexRevealEvent RecordStatusAttempt(EnemyDef def, StatusId status, StatusResistTier tier)
        {
            var ev = new CodexRevealEvent
            {
                enemyId = def?.id ?? "",
                enemyName = def?.name ?? "",
                isElement = false,
                status = status,
                statusTier = tier,
            };
            if (def == null || string.IsNullOrEmpty(def.id)) return ev;

            var entry = GetOrCreate(def.id);
            if (entry.Statuses.ContainsKey(status))
                return ev;

            entry.Statuses[status] = tier;
            ev.newlyRevealed = true;
            return ev;
        }

        // Elf Keen Senses (Spec Part D): reveals one unknown weakness of `def`.
        public bool TryRevealWeaknessWithKeenSenses(EnemyDef def, out CodexRevealEvent reveal)
        {
            reveal = default;
            if (def == null || string.IsNullOrEmpty(def.id) || def.affinities?.weak == null) return false;
            var entry = GetOrCreate(def.id);
            for (int i = 0; i < def.affinities.weak.Length; i++)
            {
                var el = CanonicalElement(def.affinities.weak[i]);
                if (entry.Elements.ContainsKey(el)) continue;
                entry.Elements[el] = Effectiveness.Weak;
                reveal = new CodexRevealEvent
                {
                    enemyId = def.id,
                    enemyName = def.name,
                    isElement = true,
                    element = el,
                    effectiveness = Effectiveness.Weak,
                    newlyRevealed = true,
                };
                return true;
            }
            return false;
        }

        // Reserved hook for future "Insight" items or skills (Spec Part D).
        public CodexRevealEvent RevealInsight(EnemyDef def, Element element, Effectiveness rawEffectiveness)
        {
            var el = CanonicalElement(element);
            var ev = new CodexRevealEvent
            {
                enemyId = def?.id ?? "",
                enemyName = def?.name ?? "",
                isElement = true,
                element = el,
                effectiveness = rawEffectiveness,
            };
            if (def == null || string.IsNullOrEmpty(def.id)) return ev;
            var entry = GetOrCreate(def.id);
            if (entry.Elements.ContainsKey(el)) return ev;
            entry.Elements[el] = rawEffectiveness;
            ev.newlyRevealed = true;
            return ev;
        }

        // Boss phase transition (Spec Part D): auto-reveal any element known in oldPhase whose affinity
        // is identical in newPhase (and not concealed in newPhase).
        public List<Element> CarryOverPhaseKnowledge(EnemyDef oldPhase, EnemyDef newPhase)
        {
            var carried = new List<Element>();
            if (oldPhase == null || newPhase == null || string.IsNullOrEmpty(oldPhase.id) || string.IsNullOrEmpty(newPhase.id))
                return carried;
            if (!entries.TryGetValue(oldPhase.id, out var oldEntry))
                return carried;

            var newEntry = GetOrCreate(newPhase.id);
            var oldAff = oldPhase.affinities ?? Affinities.None;
            var newAff = newPhase.affinities ?? Affinities.None;

            foreach (var el in AffinityResolver.AllElements)
            {
                if (!oldEntry.Elements.ContainsKey(el)) continue;
                if (newEntry.Elements.ContainsKey(el)) continue;
                if (IsConcealed(newPhase, el)) continue;
                var oldEff = oldAff.Against(el);
                var newEff = newAff.Against(el);
                if (oldEff == newEff)
                {
                    newEntry.Elements[el] = newEff;
                    carried.Add(el);
                }
            }
            return carried;
        }

        public void MergeFrom(EnemyCodex other)
        {
            if (other == null) return;
            foreach (var kv in other.entries)
            {
                var dst = GetOrCreate(kv.Key);
                foreach (var el in kv.Value.Elements)
                    dst.Elements[el.Key] = el.Value;
                foreach (var st in kv.Value.Statuses)
                    dst.Statuses[st.Key] = st.Value;
                foreach (var ct in kv.Value.ConcealedTriggers)
                {
                    int cur = dst.ConcealedTriggers.TryGetValue(ct.Key, out var v) ? v : 0;
                    dst.ConcealedTriggers[ct.Key] = Math.Max(cur, ct.Value);
                }
            }
        }

        public CodexSaveData ToSaveData()
        {
            var list = new List<CodexEntrySaveData>(entries.Count);
            foreach (var kv in entries.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                list.Add(new CodexEntrySaveData
                {
                    enemyId = kv.Key,
                    elements = kv.Value.Elements
                        .OrderBy(e => (int)e.Key)
                        .Select(e => new CodexElementRecord { element = e.Key, effectiveness = e.Value })
                        .ToArray(),
                    statuses = kv.Value.Statuses
                        .OrderBy(s => (int)s.Key)
                        .Select(s => new CodexStatusRecord { status = s.Key, tier = s.Value })
                        .ToArray(),
                    concealedTriggers = kv.Value.ConcealedTriggers
                        .OrderBy(c => (int)c.Key)
                        .Select(c => new CodexConcealedRecord { element = c.Key, triggers = c.Value })
                        .ToArray(),
                });
            }
            return new CodexSaveData { entries = list.ToArray() };
        }

        public static EnemyCodex FromSaveData(CodexSaveData data)
        {
            var codex = new EnemyCodex();
            if (data?.entries == null) return codex;
            for (int i = 0; i < data.entries.Length; i++)
            {
                var raw = data.entries[i];
                if (raw == null || string.IsNullOrEmpty(raw.enemyId)) continue;
                var entry = codex.GetOrCreate(raw.enemyId);
                if (raw.elements != null)
                    foreach (var rec in raw.elements) entry.Elements[CanonicalElement(rec.element)] = rec.effectiveness;
                if (raw.statuses != null)
                    foreach (var rec in raw.statuses) entry.Statuses[rec.status] = rec.tier;
                if (raw.concealedTriggers != null)
                    foreach (var rec in raw.concealedTriggers) entry.ConcealedTriggers[CanonicalElement(rec.element)] = rec.triggers;
            }
            return codex;
        }

        public EnemyCodex Clone() => FromSaveData(ToSaveData());

        public static string FormatEffectivenessTag(Effectiveness? eff) => eff.HasValue ? eff.Value switch
        {
            Effectiveness.Weak => "WEAK",
            Effectiveness.Resist => "RESIST",
            Effectiveness.Immune => "NULL",
            Effectiveness.Drain => "DRAIN",
            Effectiveness.Reflect => "REFLECT",
            _ => "NORMAL",
        } : "???";

        public static string FormatFloatingWord(Effectiveness eff) => eff switch
        {
            Effectiveness.Weak => "WEAK!",
            Effectiveness.Resist => "RESIST",
            Effectiveness.Immune => "NULL",
            Effectiveness.Drain => "DRAINED!",
            Effectiveness.Reflect => "REFLECTED!",
            _ => "",
        };

        public static string FormatStatusTier(StatusResistTier? tier) => tier.HasValue ? tier.Value switch
        {
            StatusResistTier.Vulnerable => "VULNERABLE",
            StatusResistTier.Resistant => "RESISTANT",
            StatusResistTier.Immune => "IMMUNE",
            _ => "NORMAL",
        } : "???";
    }
}
