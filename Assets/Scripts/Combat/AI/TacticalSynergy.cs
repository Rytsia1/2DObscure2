using System;
using System.Linq;

namespace Obscure
{
    // Derived Tactical Synergy & Coordination evaluator (Architecture §7 & §9.4).
    // Strictly derived from real existing combat mechanics (StatusEffectDefs, Break/Poise, Sleep wake, Blackboard reservations).
    // Never modifies damage, hit counts, status chances, or QTE rules; never calls Battle.Rng.
    public static class TacticalSynergy
    {
        // posture-B starting values (untested).
        // Pass test: coordinated follow-ups and status spreading occur visibly without inflating Normal deaths > baseline + 15%.
        // If it fails: reduce SynergyClamp to 4f.
        public const float SynergyClamp = 6f;

        public const float BrokenFollowUpBonus = 3.5f;
        public const float ProjectedBreakFollowUpBonus = 2.5f;
        public const float BreakSetupBonus = 2.5f;
        public const float DebuffExploitBonus = 2.5f;
        public const float DisabledControlFollowUpBonus = 1.5f;
        public const float CoordinatedKillAssistBonus = 3.0f;
        public const float StatusOverlapPenalty = 4.0f;
        public const float WakeSleepPenalty = 5.0f;
        public const float SupportProtectionBonus = 2.5f;

        public static float Evaluate(Battle b, Combatant self, EnemyAttack m, Combatant target,
            AiProfile profile, AiPlanContext ctx, float expectedDmg)
        {
            if (b == null || self == null || m == null || profile == null) return 0f;
            var bb = b.Blackboard;
            if (bb == null) return 0f;

            float total = 0f;

            // Single-target hero-directed coordination & synergy.
            if (target != null && target.IsPlayer && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons)
            {
                // 1. Broken / Projected-Break exploit (real rule: Broken targets take 1.25x+ dmg and cannot Perfect-parry).
                if (m.hits > 0)
                {
                    if (target.IsBroken)
                    {
                        total += BrokenFollowUpBonus;
                    }
                    else if (target.HasBreakGauge)
                    {
                        int plannedBreak = bb.CommittedBreak(target);
                        if (plannedBreak > 0 && plannedBreak >= target.CurrentBreakGauge)
                            total += ProjectedBreakFollowUpBonus;
                    }
                }

                // 2. Break setup: Elite+ or BreakBruiser sets up Break when another living ally can follow up.
                if ((profile.usesSynergySetup || profile.role == AiRole.BreakBruiser)
                    && m.breakPower > 0 && target.HasBreakGauge && !target.IsBroken
                    && HasLivingFollowUpAlly(b, self))
                {
                    total += BreakSetupBonus;
                }

                // 3. Derived Status follow-ups (StatusEffectDefs real multipliers & control states).
                if (m.hits > 0)
                {
                    // Curse: +25% all damage taken, -50% healing received.
                    if (target.Has(StatusId.Curse) || bb.HasPlannedStatus(target, StatusId.Curse))
                        total += DebuffExploitBonus;

                    // DefenseDown / MagicDefenseDown: match damage school.
                    if (!m.UsesMagic && (target.Has(StatusId.DefenseDown) || bb.HasPlannedStatus(target, StatusId.DefenseDown)))
                        total += DebuffExploitBonus;
                    else if (m.UsesMagic && (target.Has(StatusId.MagicDefenseDown) || bb.HasPlannedStatus(target, StatusId.MagicDefenseDown)))
                        total += DebuffExploitBonus;

                    // Silence / Despair: target cannot cast skills or restore MP/buffs -> opportune pressure.
                    if (target.Has(StatusId.Silence) || target.Has(StatusId.Despair)
                        || bb.HasPlannedStatus(target, StatusId.Silence) || bb.HasPlannedStatus(target, StatusId.Despair))
                        total += DisabledControlFollowUpBonus;
                }

                // 4. Coordinated Kill Assist: ally's pending damage leaves target alive, and our hit finishes the kill (no overkill).
                if (m.hits > 0 && expectedDmg > 0f && !bb.IsLethalReserved(target))
                {
                    float priorDmg = bb.CommittedDamage(target);
                    if (priorDmg > 0f && priorDmg < target.unit.hp && priorDmg + expectedDmg >= target.unit.hp)
                        total += CoordinatedKillAssistBonus;
                }

                // 5. Status Non-Overlap: avoid stacking identical non-stacking control/mental statuses on the same hero this round.
                if (m.apply != null && m.apply.Length > 0)
                {
                    for (int i = 0; i < m.apply.Length; i++)
                    {
                        var st = m.apply[i].id;
                        if (IsSpreadPreferredStatus(st) && bb.HasPlannedStatus(target, st))
                        {
                            total -= StatusOverlapPenalty;
                            break;
                        }
                    }
                }

                // 6. Sleep Protection: direct damage immediately wakes a Sleeping hero (Battle.Damage L1424).
                if (m.hits > 0 && target.Has(StatusId.Sleep) && ctx != null && ctx.foes.Any(f => !f.Has(StatusId.Sleep)))
                    total -= WakeSleepPenalty;

                // 7. Support Protection: allies pressure a hero actively pressuring a living enemy Support.
                if (m.hits > 0 && profile.role != AiRole.Support)
                {
                    var mem = bb.Memory;
                    var prof = mem?.GetProfile(target);
                    foreach (var ally in b.Allies(self))
                    {
                        if (ally == null || ally == self || !ally.Alive) continue;
                        if (ally.enemy?.aiRole != AiRole.Support) continue;
                        bool struckSupport = ally.LastAttacker == target && (b.Round - ally.LastDamagedRound <= 1);
                        bool focusedSupport = prof != null && prof.IsRecognized(BehaviorCategory.FocusFire, b.Round, profile)
                            && prof.MostFrequentTarget == ally;
                        if (struckSupport || focusedSupport)
                        {
                            total += SupportProtectionBonus;
                            break;
                        }
                    }
                }
            }

            return Math.Clamp(total, -SynergyClamp, SynergyClamp);
        }

        static bool HasLivingFollowUpAlly(Battle b, Combatant self)
        {
            foreach (var ally in b.Allies(self))
            {
                if (ally == null || ally == self || !ally.Alive) continue;
                if (ally.IsBroken || ally.stunTurns > 0 || ally.StatusController.HasStatus(StatusId.Stun)
                    || ally.StatusController.HasStatus(StatusId.Sleep)) continue;
                var role = ally.enemy?.aiRole ?? AiRole.Vanguard;
                if (role == AiRole.Executioner || role == AiRole.ComboAttacker || role == AiRole.Skirmisher
                    || role == AiRole.Vanguard || role == AiRole.Bruiser || role == AiRole.RangedOpportunist)
                    return true;
            }
            return false;
        }

        static bool IsSpreadPreferredStatus(StatusId id) =>
            id == StatusId.Silence || id == StatusId.Fear || id == StatusId.Despair
            || id == StatusId.Stun || id == StatusId.Sleep || id == StatusId.Confusion
            || id == StatusId.Curse || id == StatusId.Shock;
    }
}
