using System;

namespace Obscure
{
    // Single source of truth for resolving elemental affinities across Battle.Deal, DamageCalculator,
    // Null Break, ANALYSIS, and AI (Felna_Analysis_System_Spec_v1.md Part E; Analysis_System_Handoff.md Stage 0).
    public static class AffinityResolver
    {
        // Posture-B starting value (Felna_Analysis_System_Spec_v1.md Part E / Starting values):
        // ReflectCapPct = 35% of attacker Max HP per hit.
        // Pass test: No hero at full HP dies to a single reflected hit (simulation).
        // If it fails: tune down to 0.25f.
        public const float ReflectCapPct = 0.35f;

        // Posture-B starting value (Felna_Analysis_System_Spec_v1.md Part E / Starting values):
        // DrainHealPct = 100% of NORMAL-equivalent damage, capped at target's missing HP.
        // Pass test: A drain-capable fight lasts <= 2 extra rounds when the party ignores the hint.
        // If it fails: tune down to 0.50f (cap at 50% of NORMAL damage).
        public const float DrainHealPct = 1.0f;

        // Canonical 9 display/combat elements (Stage 3a: Arcane removed). Shadow aliases Dark.
        public static readonly Element[] AllElements =
        {
            Element.Physical,
            Element.Fire,
            Element.Ice,
            Element.Lightning,
            Element.Holy,
            Element.Dark,
            Element.Earth,
            Element.Wind,
            Element.Water,
        };

        public static Effectiveness ToEffectiveness(AffinityType affinity) => affinity switch
        {
            AffinityType.Weakness => Effectiveness.Weak,
            AffinityType.Resistant => Effectiveness.Resist,
            AffinityType.Nullification => Effectiveness.Immune,
            AffinityType.Drain => Effectiveness.Drain,
            AffinityType.Reflect => Effectiveness.Reflect,
            _ => Effectiveness.Neutral,
        };

        public static AffinityType ToAffinityType(Effectiveness eff) => eff switch
        {
            Effectiveness.Weak => AffinityType.Weakness,
            Effectiveness.Resist => AffinityType.Resistant,
            Effectiveness.Immune => AffinityType.Nullification,
            Effectiveness.Drain => AffinityType.Drain,
            Effectiveness.Reflect => AffinityType.Reflect,
            _ => AffinityType.Normal,
        };

        public static float DamageMultiplier(Effectiveness eff) => Affinities.Multiplier(eff);

        // Resolves raw affinity (ignoring Null Break) for a Combatant and legacy Element.
        // Checks AffinityProfile first, falling back to legacy Affinities when AffinityProfile is Normal
        // (so runtime edits to either model stay consistent).
        public static Effectiveness GetRaw(Combatant target, Element element)
        {
            if (target == null) return Effectiveness.Neutral;

            AffinityType fromProfile = AffinityType.Normal;
            if (target.AffinityProfile != null)
            {
                if (element == Element.Physical)
                    fromProfile = target.AffinityProfile.GetRawPhysicalAffinity();
                else
                {
                    var mapped = ElementTypes.FromLegacyElement(element);
                    if (mapped.IsValidElement())
                        fromProfile = target.AffinityProfile.GetRawElementalAffinity(mapped);
                }
            }

            if (fromProfile != AffinityType.Normal)
                return ToEffectiveness(fromProfile);

            return target.Affinities != null ? target.Affinities.Against(element) : Effectiveness.Neutral;
        }

        // Resolves effective affinity for a Combatant and Element, applying Null Break (Nullification -> Neutral only).
        public static Effectiveness Get(Combatant target, Element element)
        {
            var raw = GetRaw(target, element);
            if (raw == Effectiveness.Immune && target?.NullBreak != null && IsNullBroken(target.NullBreak, element))
                return Effectiveness.Neutral;
            return raw;
        }

        public static AffinityType GetAffinityType(Combatant target, Element element) =>
            ToAffinityType(Get(target, element));

        // Resolves raw AffinityType from an EnemyAffinityProfile (used by DamageCalculator).
        public static AffinityType GetRaw(EnemyAffinityProfile profile, DamageType damageType, ElementType element = ElementType.None)
        {
            if (profile == null || damageType == DamageType.Almighty)
                return AffinityType.Normal;
            return profile.GetRawAffinity(damageType, element);
        }

        // Resolves effective AffinityType from an EnemyAffinityProfile + NullBreakController (used by DamageCalculator).
        public static AffinityType Get(EnemyAffinityProfile profile, DamageType damageType, ElementType element = ElementType.None, NullBreakController nullBreak = null)
        {
            if (profile == null || damageType == DamageType.Almighty)
                return AffinityType.Normal;
            return profile.GetEffectiveAffinity(damageType, element, nullBreak);
        }

        static bool IsNullBroken(NullBreakController nullBreak, Element element)
        {
            if (nullBreak == null || !nullBreak.HasActiveNullBreak) return false;
            if (element == Element.Physical)
                return nullBreak.IsNullBreakActiveFor(DamageType.Physical, ElementType.None);
            var mapped = ElementTypes.FromLegacyElement(element);
            return mapped.IsValidElement() && nullBreak.IsNullBreakActiveFor(DamageType.Elemental, mapped);
        }
    }
}
