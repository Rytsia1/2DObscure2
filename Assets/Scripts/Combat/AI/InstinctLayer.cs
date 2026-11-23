using System;
using System.Linq;

namespace Obscure
{
    // Separate Instinct Behavior Layer (distinct from deliberate tactical scoring).
    // Expresses consistent, recognizable creature-family impulses (Predator, Relentless, Skittish, Territorial, Guardian)
    // strictly within existing skill kits, using only observable combat state and zero Battle.Rng calls.
    public static class InstinctLayer
    {
        // posture-B starting values (untested).
        // Pass test: each instinct produces recognizable, consistent behavior in unit tests without breaking role identity >= 60%
        // or raising Normal-encounter deaths > Stage 0 baseline + 15%. If it fails: scale InstinctClamp down to 4f.
        public const float InstinctClamp = 6f;

        public const float PredatorPreyHpFrac = 0.60f;
        public const float PredatorWoundedHpFrac = 0.55f;
        public const float PredatorHungerHpFrac = 0.75f;
        public const float PredatorPreyBonus = 3.0f;
        public const float PredatorFeralBonus = 2.5f;
        public const float PredatorHungerBonus = 3.0f;

        public const float RelentlessFixationBonus = 3.0f;

        public const float SkittishWoundedHpFrac = 0.55f;
        public const float SkittishLowBreakFrac = 0.45f;
        public const float SkittishSelfPreserveBonus = 3.5f;
        public const float SkittishAvoidThreatPenalty = 2.5f;

        public const float TerritorialIntruderBonus = 3.0f;
        public const float TerritorialCrowdBonus = 2.0f;

        public const float GuardianEndangeredAllyHpFrac = 0.55f;
        public const float GuardianHealBonus = 3.5f;
        public const float GuardianRetaliateBonus = 3.0f;

        public const float FearHesitationShift = 1.5f;

        public static float Evaluate(Battle b, Combatant self, EnemyAttack m, Combatant target,
            AiProfile profile, AiPlanContext ctx, Combatant topThreat,
            out string readKey, out string readText)
        {
            readKey = null;
            readText = null;
            if (b == null || self == null || m == null || profile == null) return 0f;

            float selfHpFrac = (float)self.unit.hp / Math.Max(1, self.Stats.maxHp);
            float total = 0f;
            float bestMag = 0f;
            string bestKey = null;
            string bestText = null;

            void Add(float delta, string key = null, string text = null)
            {
                if (delta == 0f) return;
                total += delta;
                float mag = Math.Abs(delta);
                if (mag > bestMag && !string.IsNullOrEmpty(key))
                {
                    bestMag = mag;
                    bestKey = key;
                    bestText = text;
                }
            }

            switch (profile.instinct)
            {
                case InstinctKind.Predator:
                    EvaluatePredator(self, m, target, selfHpFrac, Add);
                    break;
                case InstinctKind.Relentless:
                    EvaluateRelentless(b, self, m, target, Add);
                    break;
                case InstinctKind.Skittish:
                    EvaluateSkittish(self, m, target, ctx, selfHpFrac, Add);
                    break;
                case InstinctKind.Territorial:
                    EvaluateTerritorial(b, self, m, target, ctx, Add);
                    break;
                case InstinctKind.Guardian:
                    EvaluateGuardian(b, self, m, target, Add);
                    break;
            }

            // Universal Fear impulse: any frightened creature slightly favors self-preservation/deterrence
            // and hesitates to rush directly at the top observable threat when other targets stand.
            if (self.Has(StatusId.Fear))
            {
                if (IsSelfPreservingOrDeterring(self, m, target))
                    Add(FearHesitationShift);
                else if (target != null && target == topThreat && ctx != null && ctx.foes.Count > 1
                    && m.hits > 0 && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons)
                    Add(-FearHesitationShift);
            }

            if (total == 0f) return 0f;

            float clamped = Math.Clamp(total, -InstinctClamp, InstinctClamp) * profile.instinctWeight;
            readKey = bestKey;
            readText = bestText;
            return clamped;
        }

        static void EvaluatePredator(Combatant self, EnemyAttack m, Combatant target, float selfHpFrac,
            Action<float, string, string> add)
        {
            bool isSingleOffense = target != null && target.IsPlayer && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons;
            if (isSingleOffense && m.hits > 0)
            {
                float targetHpFrac = (float)target.unit.hp / Math.Max(1, target.Stats.maxHp);
                if (targetHpFrac <= PredatorPreyHpFrac || target.IsBroken || target.Has(StatusId.Bleed) || target.Has(StatusId.Fear))
                {
                    add(PredatorPreyBonus,
                        $"instinct_pred_prey:{self.enemy?.id}:{target.slot}",
                        $"{self.name} senses weakness and lunges at {target.name}!");
                }
            }

            // Wounded predator becomes more aggressive (presses multi-hit, high-impact, or MP offense skills).
            if (selfHpFrac <= PredatorWoundedHpFrac && m.hits > 0 && !m.self && !m.SupportsAlly && !m.Summons)
            {
                if (m.hits >= 2 || m.PowerAt(0) >= 1.1f || m.mpCost > 0 || m.drain > 0f)
                {
                    add(PredatorFeralBonus,
                        $"instinct_pred_feral:{self.enemy?.id}",
                        $"Wounded, {self.name} grows ferocious!");
                }
            }

            // Parasitic hunger (e.g. Crypt Leech): when injured, hungers for life-draining attacks.
            if (m.drain > 0f && selfHpFrac < PredatorHungerHpFrac)
            {
                add(PredatorHungerBonus,
                    $"instinct_pred_hunger:{self.enemy?.id}",
                    $"{self.name} hungers for vitality!");
            }
        }

        static void EvaluateRelentless(Battle b, Combatant self, EnemyAttack m, Combatant target,
            Action<float, string, string> add)
        {
            if (self.LastTargetedHero == null || !self.LastTargetedHero.Alive) return;
            if (target == null || target != self.LastTargetedHero || !target.IsPlayer) return;
            if (m.allTargets || m.self || m.SupportsAlly || m.Summons || m.hits <= 0) return;

            var bb = b.Blackboard;
            if (bb != null)
            {
                if (bb.IsLethalReserved(target)) return;
                var seen = bb.Memory?.GetSeenEffectiveness(target, m.element);
                if (seen == Effectiveness.Immune || seen == Effectiveness.Drain || seen == Effectiveness.Reflect) return;
            }

            add(RelentlessFixationBonus,
                $"instinct_relentless:{self.enemy?.id}:{target.slot}",
                $"{self.name} relentlessly presses {target.name}.");
        }

        static void EvaluateSkittish(Combatant self, EnemyAttack m, Combatant target, AiPlanContext ctx,
            float selfHpFrac, Action<float, string, string> add)
        {
            bool lowBreak = self.HasBreakGauge && (float)self.CurrentBreakGauge / Math.Max(1, self.MaxBreakGauge) <= SkittishLowBreakFrac;
            bool threatened = selfHpFrac <= SkittishWoundedHpFrac || lowBreak || self.Has(StatusId.Fear);
            if (!threatened) return;

            if (IsSelfPreservingOrDeterring(self, m, target))
            {
                add(SkittishSelfPreserveBonus,
                    $"instinct_skittish_self:{self.enemy?.id}",
                    $"Shaken, {self.name} looks to protect itself.");
                return;
            }

            if (target != null && target.IsPlayer && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons
                && ctx != null && ctx.foes.Count > 1)
            {
                if (target == self.LastAttacker || target.CounterTurns > 0)
                {
                    add(-SkittishAvoidThreatPenalty,
                        $"instinct_skittish_recoil:{self.enemy?.id}:{target.slot}",
                        $"Shaken, {self.name} recoils from {target.name}.");
                }
            }
        }

        static void EvaluateTerritorial(Battle b, Combatant self, EnemyAttack m, Combatant target, AiPlanContext ctx,
            Action<float, string, string> add)
        {
            // Retaliate against the intruder who recently struck this creature.
            if (target != null && target.IsPlayer && target == self.LastAttacker && target.Alive
                && (b.Round - self.LastDamagedRound <= 1)
                && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons)
            {
                add(TerritorialIntruderBonus,
                    $"instinct_territorial:{self.enemy?.id}:{target.slot}",
                    $"{self.name} turns furiously on {target.name}!");
            }

            // Push back grouped intruders with sweeping or posture-breaking skills.
            if ((m.allTargets || m.breakPower > 0) && ctx != null && ctx.foes.Count >= 3)
            {
                add(TerritorialCrowdBonus, null, null);
            }
        }

        static void EvaluateGuardian(Battle b, Combatant self, EnemyAttack m, Combatant target,
            Action<float, string, string> add)
        {
            if (m.SupportsAlly && target != null && target.Alive)
            {
                float allyHpFrac = (float)target.unit.hp / Math.Max(1, target.Stats.maxHp);
                if (allyHpFrac <= GuardianEndangeredAllyHpFrac)
                {
                    add(GuardianHealBonus,
                        $"instinct_guardian_heal:{self.enemy?.id}",
                        $"{self.name} rushes to protect {target.name}!");
                }
                return;
            }

            if (target == null || !target.IsPlayer || m.allTargets || m.self || m.SupportsAlly || m.Summons) return;

            var mem = b.Blackboard?.Memory;
            var prof = mem?.GetProfile(target);
            var allies = b.Allies(self);
            foreach (var ally in allies)
            {
                if (ally == null || ally == self || !ally.Alive) continue;
                float allyHpFrac = (float)ally.unit.hp / Math.Max(1, ally.Stats.maxHp);
                bool recentlyStruckByTarget = ally.LastAttacker == target && (b.Round - ally.LastDamagedRound <= 1);
                bool endangered = allyHpFrac <= GuardianEndangeredAllyHpFrac;
                bool focusedByTarget = prof != null && prof.MostFrequentTarget == ally
                    && prof.GetWeightedScore(BehaviorCategory.FocusFire, b.Round, AiProfile.StandardDecayPerRound) > 0f;

                if ((endangered && (recentlyStruckByTarget || focusedByTarget)) || (recentlyStruckByTarget && ally.enemy?.aiRole == AiRole.Support))
                {
                    add(GuardianRetaliateBonus,
                        $"instinct_guardian_protect:{self.enemy?.id}:{target.slot}",
                        $"{self.name} steps up to shield {ally.name} from {target.name}!");
                    break;
                }
            }
        }

        static bool IsSelfPreservingOrDeterring(Combatant self, EnemyAttack m, Combatant target)
        {
            if (m.self || m.drain > 0f || (m.Heals && target == self)) return true;
            if (m.apply != null && m.apply.Length > 0)
            {
                for (int i = 0; i < m.apply.Length; i++)
                {
                    var st = m.apply[i].id;
                    if (st == StatusId.Fear || st == StatusId.Silence || st == StatusId.Stun
                        || st == StatusId.Sleep || st == StatusId.Confusion || st == StatusId.Despair)
                        return true;
                }
            }
            return false;
        }
    }
}
