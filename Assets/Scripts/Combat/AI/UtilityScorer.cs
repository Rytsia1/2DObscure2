using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Shared evaluation context built once per enemy decision in PriorityBrain.
    public sealed class AiPlanContext
    {
        public List<Combatant> foes;
        public bool heavyTaken;
        public int livingSummons;
        public Combatant hurtAlly;
        public float hurtAllyFrac;
    }

    // Scored candidate (move, target) produced by UtilityScorer.
    public sealed class ScoredCandidate
    {
        public EnemyAttack move;
        public int dataIndex;
        public Combatant target;
        public bool charging;
        public float expectedDamage;
        public float baseScore;
        public float immediate;
        public float future;
        public float role;
        public float synergy;
        public float objective;
        public float adaptation;
        public float resource;
        public float repetition;
        public float risk;
        public float rawModifier;
        public float clampedModifier;
        public float totalScore;
        public float noAdaptTotalScore;
        public int repetitionCount;
        public bool adaptationShifted;
        public string readKey;
        public string readText;
    }

    // Hybrid Utility AI scorer (Spec §3.3, Architecture §8.4, §8.5).
    // Formula: score = aiPriority * 10 + clamp(Immediate + Future + Role + Synergy + Objective + Adaptation - Resource - Repetition - Risk, -UtilityClamp, +UtilityClamp).
    // Only candidates that already passed PriorityBrain.IsLegal enter scoring. Never calls Battle.Rng.
    public static class UtilityScorer
    {
        // posture-B starting value (untested).
        // Pass test: Wasted Action Rate <= 5%; each role's move mix stays recognisable (200-seed sim).
        // If it fails: Erratic -> +/-10f; Rigid -> +/-20f.
        public const float UtilityClamp = 15f;

        // posture-B starting values (untested) for Immediate Value & Resource Cost normalization.
        // Pass test: Role identity >= 60%, Wasted Action Rate <= 5%, MP skills preferred over basic attacks when affordable.
        // If it fails: tune ImmediateDamageScale / ResourceCostWeight inside UtilityClamp.
        public const float ImmediateDamageScale = 10f;
        public const float ImmediateDamageCap = 8f;
        public const float ImmediateKillBonus = 4f;
        public const float ImmediateBreakScale = 0.10f;
        public const float ImmediateStatusValue = 3f;
        public const float ImmediateHealScale = 8f;
        public const float ImmediateSummonPerSlot = 4.5f;
        public const float ResourceCostWeight = 3f;

        // Expected mean damage (no variance, no crit, zero Rng calls, observable info only — Spec §5.6).
        // Unknown affinities are treated as Neutral (1.0x) until observed via combat events.
        public static float EstimateExpectedDamage(Combatant attacker, EnemyAttack move, Combatant target, Effectiveness? knownEffectiveness = null)
        {
            if (attacker == null || move == null || target == null || !target.Alive || move.hits <= 0) return 0f;
            float effMult = knownEffectiveness.HasValue ? Affinities.Multiplier(knownEffectiveness.Value) : 1f;
            if (effMult <= 0f) return 0f;
            float total = 0f;
            for (int i = 0; i < move.hits; i++)
            {
                float p = move.PowerAt(i);
                int raw = move.UsesMagic
                    ? Rules.Magic(attacker, target, p, rng: null)
                    : Rules.Physical(attacker, target, p, rng: null);
                total += Math.Max(1f, (float)Math.Round(raw * effMult));
            }
            return total;
        }

        // Resolves legal candidate targets for a single-target move while enforcing:
        // 1. Locked Targeting (Spec §3.5, Arch §7.4): LowestHp / LowestDef skip lethal-reserved targets then pick next by same rule.
        // 2. FocusCapNormal (Spec §4.3 v1.1): Normal-tier enemies cap single-target damage at FocusCapNormal per hero when alternatives exist.
        // 3. Stun + Heavy check (Arch §15): never combine Stun and Heavy on the same target in the same round.
        public static List<Combatant> CandidateTargets(Battle b, Combatant self, EnemyAttack move, AiPlanContext ctx)
        {
            var pool = ctx.foes;
            if (move.apply != null && move.apply.Length > 0)
            {
                var fresh = pool.Where(f => !move.apply.Any(ap => f.Has(ap.id))).ToList();
                if (fresh.Count > 0) pool = fresh;
            }
            if (pool.Count == 0) return pool;

            var bb = b?.Blackboard;
            if (bb != null)
            {
                // Filter out targets that would violate Stun + Heavy on the same target this round.
                var noStunHeavy = pool.Where(p => !bb.WouldViolateStunHeavy(move, p, ctx.foes)).ToList();
                if (noStunHeavy.Count > 0) pool = noStunHeavy;
                else if (move.HasHeavy || (move.apply != null && move.apply.Any(ap => ap.id == StatusId.Stun)))
                    return new List<Combatant>(); // hard rule: Stun + Heavy never allowed on same target
            }

            var rule = self.enemy != null ? self.enemy.targetRule : TargetRule.Random;
            var lethalReserved = bb?.LethalReservations;
            if (rule == TargetRule.LowestHp)
            {
                var unreserved = lethalReserved != null && lethalReserved.Count > 0
                    ? pool.Where(p => !lethalReserved.Contains(p)).ToList()
                    : pool;
                var activePool = unreserved.Count > 0 ? unreserved : pool;
                int minHp = activePool.Min(p => p.unit.hp);
                return activePool.Where(p => p.unit.hp == minHp).OrderBy(p => p.slot).ToList();
            }
            if (rule == TargetRule.LowestDef)
            {
                var unreserved = lethalReserved != null && lethalReserved.Count > 0
                    ? pool.Where(p => !lethalReserved.Contains(p)).ToList()
                    : pool;
                var activePool = unreserved.Count > 0 ? unreserved : pool;
                int minDef = activePool.Min(p => p.Def);
                return activePool.Where(p => p.Def == minDef).OrderBy(p => p.slot).ToList();
            }

            // FocusCapNormal (Spec §4.3 v1.1): at most FocusCapNormal Normal-tier enemies plan single-target damage on the same hero per round.
            // Elites, Minibosses, and Bosses are exempt.
            if (bb != null && self.enemy != null && self.enemy.aiTier == AiTier.Normal && move.hits > 0 && !move.telegraph)
            {
                var underCap = pool.Where(p => bb.NormalSingleTargetCount(p) < EncounterBlackboard.FocusCapNormal).ToList();
                if (underCap.Count > 0) pool = underCap;
            }

            // TargetRule.Random on roster path: evaluate every legal target deterministically (no Rng).
            return pool.OrderBy(p => p.slot).ToList();
        }

        public static ScoredCandidate SelectBest(Battle b, Combatant self, List<(EnemyAttack move, int dataIndex)> legalMoves,
            AiPlanContext ctx, AiDecisionTrace trace)
        {
            if (legalMoves == null || legalMoves.Count == 0) return null;

            var profile = AiProfile.For(self.enemy);
            Combatant topThreat = FindTopObservableThreat(ctx.foes);
            int lowestDef = ctx.foes.Count > 0 ? ctx.foes.Min(f => f.Def) : 0;

            ScoredCandidate best = null;
            ScoredCandidate bestNoAdapt = null;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                var (m, dataIdx) = legalMoves[i];

                if (m.self || m.Summons || m.allTargets || m.Heals)
                {
                    var tgt = m.Heals ? ctx.hurtAlly : null;
                    var cand = ScoreCandidate(b, self, m, dataIdx, tgt, profile, ctx, topThreat, lowestDef);
                    RecordCandidate(trace, cand);
                    if (best == null || IsBetter(cand, best)) best = cand;
                    if (bestNoAdapt == null || IsBetterNoAdapt(cand, bestNoAdapt)) bestNoAdapt = cand;
                    continue;
                }

                var targets = CandidateTargets(b, self, m, ctx);
                for (int t = 0; t < targets.Count; t++)
                {
                    var cand = ScoreCandidate(b, self, m, dataIdx, targets[t], profile, ctx, topThreat, lowestDef);
                    RecordCandidate(trace, cand);
                    if (best == null || IsBetter(cand, best)) best = cand;
                    if (bestNoAdapt == null || IsBetterNoAdapt(cand, bestNoAdapt)) bestNoAdapt = cand;
                }
            }

            if (best != null && bestNoAdapt != null && (best.move != bestNoAdapt.move || best.target != bestNoAdapt.target))
            {
                best.adaptationShifted = true;
                // If best itself didn't carry a read key (e.g. a penalty pushed the AI away from bestNoAdapt, like Defensive guard or Break charge gate),
                // borrow the read key/text from bestNoAdapt so the player sees why the enemy turned away!
                if (string.IsNullOrEmpty(best.readKey) && !string.IsNullOrEmpty(bestNoAdapt.readKey))
                {
                    best.readKey = bestNoAdapt.readKey;
                    best.readText = bestNoAdapt.readText;
                }
            }
            return best;
        }

        static ScoredCandidate ScoreCandidate(Battle b, Combatant self, EnemyAttack m, int dataIndex, Combatant target,
            AiProfile profile, AiPlanContext ctx, Combatant topThreat, int lowestDef)
        {
            var mem = b?.Blackboard?.Memory;
            float baseScore = m.aiPriority * 10f;
            float expectedDmg = 0f;
            float immediate = EvaluateImmediate(b, self, m, target, ctx, out expectedDmg);
            float future = 0f;
            float role = EvaluateRoleBonus(self, m, target, profile, ctx, topThreat, lowestDef);
            float synergy = 0f;
            float objective = 0f;
            float adaptation = EvaluateAdaptation(b, self, m, target, profile, ctx, out string readKey, out string readText);
            float resource = EvaluateResourceCost(self, m);
            int streak = mem != null ? mem.GetIneffectiveStreak(self, m.name, target?.slot ?? -1) : 0;
            float repetition = streak * EncounterTacticalMemory.RepetitionPenaltyPerStreak;
            float risk = EvaluateRisk(b, self, m, target);

            float rawNoAdapt = immediate + future + role + synergy + objective - resource - repetition - risk;
            float clampedNoAdapt = Math.Clamp(rawNoAdapt, -UtilityClamp, UtilityClamp);
            float raw = rawNoAdapt + adaptation;
            float clamped = Math.Clamp(raw, -UtilityClamp, UtilityClamp);

            return new ScoredCandidate
            {
                move = m,
                dataIndex = dataIndex,
                target = target,
                charging = m.telegraph && !m.self && !m.Heals && !m.Summons,
                expectedDamage = expectedDmg,
                baseScore = baseScore,
                immediate = immediate,
                future = future,
                role = role,
                synergy = synergy,
                objective = objective,
                adaptation = adaptation,
                resource = resource,
                repetition = repetition,
                risk = risk,
                rawModifier = raw,
                clampedModifier = clamped,
                totalScore = baseScore + clamped,
                noAdaptTotalScore = baseScore + clampedNoAdapt,
                repetitionCount = streak,
                readKey = readKey,
                readText = readText,
            };
        }

        // Evaluates the 7 adaptation responses from shared encounter memory (Spec §5.4, §5.7, Arch §4.1, Handoff §Adaptation responses).
        // All bonuses/penalties sit inside UtilityClamp and never modify stats, damage, QTE, or hidden info.
        static float EvaluateAdaptation(Battle b, Combatant self, EnemyAttack m, Combatant target, AiProfile profile,
            AiPlanContext ctx, out string readKey, out string readText)
        {
            readKey = null;
            readText = null;
            var mem = b?.Blackboard?.Memory;
            if (mem == null || mem.Profiles.Count == 0) return 0f;

            int round = b.Round;
            float bestMag = 0f;
            float total = 0f;
            string bestKey = null;
            string bestText = null;

            void Add(float delta, string key, string text)
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

            // 5. Break Specialist / repeated charge interrupts: delay starting a charge unless own Break gauge >= ChargeShieldGate.
            if (m.telegraph && !m.self && !m.Heals && !m.Summons && self.HasBreakGauge)
            {
                bool breakThreat = mem.ChargeInterruptCount >= 2;
                if (!breakThreat)
                {
                    foreach (var p in ctx.foes)
                    {
                        var prof = mem.GetProfile(p);
                        if (prof != null && prof.IsRecognized(BehaviorCategory.BreakSpecialist, round, profile))
                        {
                            breakThreat = true;
                            break;
                        }
                    }
                }
                if (breakThreat && self.CurrentBreakGauge < self.MaxBreakGauge * EncounterTacticalMemory.ChargeShieldGate)
                {
                    Add(-EncounterTacticalMemory.BreakSpecialistChargePenalty,
                        $"break_spec:{self.enemy?.id}",
                        $"{self.name} guards its strength before it gathers.");
                }
            }

            // 4a. Elemental Specialist exploiting an enemy weakness -> Support boosts heal on the exposed ally.
            if (m.Heals && target != null && profile.role == AiRole.Support)
            {
                foreach (var p in ctx.foes)
                {
                    var prof = mem.GetProfile(p);
                    if (prof != null && prof.IsRecognized(BehaviorCategory.ElementalSpecialist, round, profile)
                        && prof.exploitedWeaknessEnemies.Contains(target))
                    {
                        Add(EncounterTacticalMemory.ElementalSpecialistHealBonus,
                            $"elem_spec_heal:{p.slot}",
                            $"They shield what burns under {p.name}'s strikes.");
                        break;
                    }
                }
            }

            // Single-target hero-directed adaptation responses (1, 2, 3, 4b, 6).
            if (target != null && target.IsPlayer && !m.allTargets && !m.self && !m.Heals && !m.Summons)
            {
                var prof = mem.GetProfile(target);
                if (prof != null)
                {
                    // 1. Focus Fire by hero X on living enemy E -> E's allies pressure X.
                    if (prof.IsRecognized(BehaviorCategory.FocusFire, round, profile))
                    {
                        var focused = prof.MostFrequentTarget;
                        if (focused != null && focused.Alive && focused != self)
                        {
                            Add(EncounterTacticalMemory.FocusFireAdaptationBonus,
                                $"focus_fire:{target.slot}",
                                $"The crypt turns on {target.name}.");
                        }
                    }

                    // 2. Defensive hero X -> -Target while X is guarding; +Status moves on X.
                    if (prof.IsRecognized(BehaviorCategory.Defensive, round, profile))
                    {
                        bool hasStatus = m.apply != null && m.apply.Length > 0;
                        if (hasStatus)
                        {
                            Add(EncounterTacticalMemory.DefensiveStatusBonus,
                                $"defensive:{target.slot}",
                                $"The Hollow sense {target.name}'s guard. They look for an opening.");
                        }
                        else if (target.guarding && m.hits > 0)
                        {
                            Add(-EncounterTacticalMemory.DefensiveGuardPenalty,
                                $"defensive:{target.slot}",
                                $"The Hollow sense {target.name}'s guard. They turn elsewhere.");
                        }
                    }

                    // 3. Support Dependent hero X -> +Target on X (Disruptor / Executioner count double).
                    if (prof.IsRecognized(BehaviorCategory.SupportDependent, round, profile))
                    {
                        float mult = (profile.role == AiRole.Disruptor || profile.role == AiRole.Executioner) ? 2f : 1f;
                        Add(EncounterTacticalMemory.SupportDependentBonus * mult,
                            $"support_dep:{target.slot}",
                            $"They mark the healer, {target.name}.");
                    }

                    // 4b. Elemental Specialist hero X exploiting a weakness -> Attackers pressure X.
                    if (prof.IsRecognized(BehaviorCategory.ElementalSpecialist, round, profile) && prof.exploitedWeaknessEnemies.Count > 0)
                    {
                        Add(EncounterTacticalMemory.ElementalSpecialistTargetBonus,
                            $"elem_spec:{target.slot}",
                            $"They shield what burns — {target.name} draws their ire.");
                    }

                    // 6. Burst hero X -> prioritize pressuring X and bracing against burst output.
                    if (prof.IsRecognized(BehaviorCategory.BurstDamage, round, profile))
                    {
                        Add(EncounterTacticalMemory.BurstTargetBonus,
                            $"burst:{target.slot}",
                            $"They brace against {target.name}.");
                    }
                }
            }

            readKey = bestKey;
            readText = bestText;
            return total;
        }

        static float EvaluateRisk(Battle b, Combatant self, EnemyAttack m, Combatant target)
        {
            float risk = 0f;
            var bb = b?.Blackboard;
            if (bb != null && target != null && target.IsPlayer && !m.allTargets && !m.self && !m.Heals && !m.Summons)
            {
                // Lethal reservation (Spec §4.3, Arch §7.3): penalize overcommitting damage onto a hero already covered by earlier planners.
                if (bb.IsLethalReserved(target))
                    risk += EncounterBlackboard.LethalReservationPenalty;
            }
            return risk;
        }

        static float EvaluateImmediate(Battle b, Combatant self, EnemyAttack m, Combatant target, AiPlanContext ctx, out float expectedDmg)
        {
            expectedDmg = 0f;
            if (m.Summons)
                return Math.Max(0, m.summonCount - ctx.livingSummons) * ImmediateSummonPerSlot;
            if (m.Heals)
                return target != null ? (1f - (float)target.unit.hp / Math.Max(1, target.Stats.maxHp)) * ImmediateHealScale : 0f;
            if (m.self)
            {
                if (m.restoreMp > 0f)
                    return (1f - (float)self.unit.mp / Math.Max(1, self.Stats.maxMp)) * 6f;
                return 3f;
            }

            var mem = b?.Blackboard?.Memory;
            float val = 0f;
            if (m.allTargets)
            {
                for (int i = 0; i < ctx.foes.Count; i++)
                {
                    var f = ctx.foes[i];
                    var knownEff = mem?.GetSeenEffectiveness(f, m.element);
                    float d = EstimateExpectedDamage(self, m, f, knownEff);
                    expectedDmg += d;
                    val += Math.Min(ImmediateDamageCap * 0.5f, (d / Math.Max(1, f.Stats.maxHp)) * ImmediateDamageScale * 0.5f);
                    if (d >= f.unit.hp) val += ImmediateKillBonus * 0.5f;
                    if (m.breakPower > 0 && f.HasBreakGauge && !f.IsBroken) val += m.breakPower * ImmediateBreakScale * 0.5f;
                }
                val = Math.Min(ImmediateDamageCap + ImmediateKillBonus, val);
                return val;
            }

            if (target != null)
            {
                if (m.hits > 0)
                {
                    var knownEff = mem?.GetSeenEffectiveness(target, m.element);
                    expectedDmg = EstimateExpectedDamage(self, m, target, knownEff);
                    float hpFracDmg = expectedDmg / Math.Max(1, target.Stats.maxHp);
                    val += Math.Min(ImmediateDamageCap, hpFracDmg * ImmediateDamageScale);
                    // Prefer finishing wounded/low-HP targets deterministically without wasting damage on guarding targets.
                    float missingFrac = 1f - (float)target.unit.hp / Math.Max(1, target.Stats.maxHp);
                    val += missingFrac * 2f;
                    if (expectedDmg >= target.unit.hp) val += ImmediateKillBonus;
                    if (target.IsBroken) val += 1.5f;
                    if (m.drain > 0f && self.unit.hp < self.Stats.maxHp)
                        val += (1f - (float)self.unit.hp / Math.Max(1, self.Stats.maxHp)) * 2f;
                }
                if (m.breakPower > 0 && target.HasBreakGauge && !target.IsBroken)
                    val += m.breakPower * ImmediateBreakScale;
                if (m.apply != null && m.apply.Length > 0)
                {
                    for (int i = 0; i < m.apply.Length; i++)
                    {
                        var ap = m.apply[i];
                        if (!target.Has(ap.id))
                            val += ImmediateStatusValue * Math.Max(0.25f, ap.chance);
                    }
                }
            }
            return val;
        }

        static float EvaluateRoleBonus(Combatant self, EnemyAttack m, Combatant target, AiProfile profile,
            AiPlanContext ctx, Combatant topThreat, int lowestDef)
        {
            switch (profile.role)
            {
                case AiRole.Support:
                    if (m.Heals) return AiProfile.SupportHealWeight;
                    return 0f;
                case AiRole.Executioner:
                    if (target != null && m.hits > 0)
                    {
                        float hpFrac = (float)target.unit.hp / Math.Max(1, target.Stats.maxHp);
                        if (hpFrac <= 0.50f || target.IsBroken) return AiProfile.ExecutionerLowHpWeight;
                        return AiProfile.ExecutionerLowHpWeight * (1f - hpFrac) * 0.5f;
                    }
                    return 0f;
                case AiRole.Disruptor:
                    float disp = 0f;
                    if (m.apply != null && m.apply.Length > 0) disp += AiProfile.DisruptorThreatWeight * 0.5f;
                    if (target != null && target == topThreat) disp += AiProfile.DisruptorThreatWeight * 0.5f;
                    return disp;
                case AiRole.AreaController:
                    if (m.allTargets) return Math.Max(0, ctx.foes.Count - 1) * AiProfile.ControllerAoEPerExtraTarget;
                    if (m.UsesMagic) return AiProfile.MagicRoleWeight * 0.5f;
                    return 0f;
                case AiRole.AdaptiveController:
                    if (m.allTargets) return Math.Max(0, ctx.foes.Count - 1) * AiProfile.ControllerAoEPerExtraTarget;
                    if (m.apply != null && m.apply.Length > 0) return 2f;
                    if (m.Summons) return 2f;
                    return 0f;
                case AiRole.BreakBruiser:
                    return m.breakPower > 0 ? AiProfile.BreakBruiserBreakWeight : 0f;
                case AiRole.Attrition:
                    return m.drain > 0f ? AiProfile.AttritionDrainWeight : 0f;
                case AiRole.ComboAttacker:
                case AiRole.Skirmisher:
                    return m.hits >= 2 ? AiProfile.MultiHitRoleWeight : 0f;
                case AiRole.ElementalAttacker:
                case AiRole.ArcaneCaster:
                    return m.UsesMagic ? AiProfile.MagicRoleWeight : 0f;
                case AiRole.DefensivePunisher:
                    return (m.telegraph || m.HasHeavy) ? AiProfile.DefensivePunisherChargeWeight : 0f;
                case AiRole.RangedOpportunist:
                    return (target != null && target.Def <= lowestDef) ? AiProfile.RangedOpportunistLowDefWeight : 0f;
                case AiRole.Vanguard:
                case AiRole.Bruiser:
                default:
                    return m.hits > 0 && !m.self ? AiProfile.VanguardPressureWeight : 0f;
            }
        }

        static float EvaluateResourceCost(Combatant self, EnemyAttack m)
        {
            if (m.mpCost <= 0 || self.Stats.maxMp <= 0) return 0f;
            return ((float)m.mpCost / self.Stats.maxMp) * ResourceCostWeight;
        }

        // Observable-only threat proxy (HUD stats: ATK/MAG + current HP% — never reads hidden skills/inventory).
        static Combatant FindTopObservableThreat(List<Combatant> foes)
        {
            Combatant best = null;
            int bestPower = -1;
            for (int i = 0; i < foes.Count; i++)
            {
                var f = foes[i];
                int power = Math.Max(f.Atk, f.Magic);
                if (best == null || power > bestPower || (power == bestPower && f.slot < best.slot))
                {
                    best = f;
                    bestPower = power;
                }
            }
            return best;
        }

        // Deterministic tie-breaking (Architecture §8.5):
        // 1. Higher totalScore (which incorporates aiPriority*10 + clamped utility)
        // 2. Explicit mandatory priority (aiPriority)
        // 3. Higher tactical utility (clampedModifier)
        // 4. Lower MP cost (better resource efficiency)
        // 5. Lower repetition
        // 6. Stable action/target identifier (dataIndex, then target slot)
        static bool IsBetter(ScoredCandidate a, ScoredCandidate b)
        {
            const float eps = 0.0001f;
            if (Math.Abs(a.totalScore - b.totalScore) > eps) return a.totalScore > b.totalScore;
            if (a.move.aiPriority != b.move.aiPriority) return a.move.aiPriority > b.move.aiPriority;
            if (Math.Abs(a.clampedModifier - b.clampedModifier) > eps) return a.clampedModifier > b.clampedModifier;
            if (a.move.mpCost != b.move.mpCost) return a.move.mpCost < b.move.mpCost;
            if (a.repetitionCount != b.repetitionCount) return a.repetitionCount < b.repetitionCount;
            if (a.dataIndex != b.dataIndex) return a.dataIndex < b.dataIndex;
            int slotA = a.target?.slot ?? -1;
            int slotB = b.target?.slot ?? -1;
            return slotA < slotB;
        }

        static bool IsBetterNoAdapt(ScoredCandidate a, ScoredCandidate b)
        {
            const float eps = 0.0001f;
            if (Math.Abs(a.noAdaptTotalScore - b.noAdaptTotalScore) > eps) return a.noAdaptTotalScore > b.noAdaptTotalScore;
            if (a.move.aiPriority != b.move.aiPriority) return a.move.aiPriority > b.move.aiPriority;
            if (a.move.mpCost != b.move.mpCost) return a.move.mpCost < b.move.mpCost;
            if (a.repetitionCount != b.repetitionCount) return a.repetitionCount < b.repetitionCount;
            if (a.dataIndex != b.dataIndex) return a.dataIndex < b.dataIndex;
            int slotA = a.target?.slot ?? -1;
            int slotB = b.target?.slot ?? -1;
            return slotA < slotB;
        }

        static void RecordCandidate(AiDecisionTrace trace, ScoredCandidate cand)
        {
            if (trace == null || cand == null) return;
            trace.candidates.Add(new AiCandidateTrace
            {
                moveName = cand.move.name,
                targetName = cand.target?.name,
                targetSlot = cand.target?.slot ?? -1,
                aiPriority = cand.move.aiPriority,
                mpCost = cand.move.mpCost,
                charging = cand.charging,
                legal = true,
                forced = false,
                baseScore = cand.baseScore,
                immediate = cand.immediate,
                future = cand.future,
                role = cand.role,
                synergy = cand.synergy,
                objective = cand.objective,
                adaptation = cand.adaptation,
                resource = cand.resource,
                repetition = cand.repetition,
                risk = cand.risk,
                rawModifier = cand.rawModifier,
                clampedModifier = cand.clampedModifier,
                totalScore = cand.totalScore,
            });
        }
    }
}
