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
        public float instinct;
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
        public float noInstinctTotalScore;
        public int repetitionCount;
        public bool adaptationShifted;
        public bool instinctShifted;
        public string readKey;
        public string readText;
        public string instinctReadKey;
        public string instinctReadText;
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

        // posture-B starting value (untested), per fresh ally buff.
        // Pass test: on turns with no heal needed, a common support buffs on 40-70% of turns (200-seed sim).
        // If it fails: too passive -> 4f; buffs every single turn -> 2f.
        public const float ImmediateBuffValue = 3f;

        // Ally-buff target (deterministic): allies lacking at least one of the move's buffs, others before the caster.
        // Attack Up -> highest observable offense; defensive buffs -> the ally the party has hit most, else lowest HP fraction.
        public static Combatant SupportTarget(Battle b, Combatant self, EnemyAttack move)
        {
            if (b == null || move == null || !move.BuffsAlly) return null;
            var pool = b.Allies(self).Where(c => c.Alive && move.buffAlly.Any(ap => !c.Has(ap.id))).ToList();
            if (pool.Count == 0) return null;
            var others = pool.Where(c => c != self).ToList();
            if (others.Count > 0) pool = others;

            if (move.buffAlly[0].id == StatusId.AttackUp)
                return pool.OrderByDescending(c => Math.Max(c.Atk, c.Magic)).ThenBy(c => c.slot).First();

            var mem = b.Blackboard?.Memory;
            int HitCount(Combatant c) => mem == null ? 0
                : mem.Profiles.Values.Sum(p => p.attackCountsByEnemy.TryGetValue(c, out int n) ? n : 0);
            int maxHits = pool.Max(HitCount);
            if (maxHits > 0)
                return pool.Where(c => HitCount(c) == maxHits).OrderBy(c => c.slot).First();
            return pool.OrderBy(c => (float)c.unit.hp / Math.Max(1, c.Stats.maxHp)).ThenBy(c => c.slot).First();
        }

        // Expected mean damage (no variance, no crit, zero Rng calls, observable info only — Spec §5.6).
        // Unknown affinities are treated as Neutral (1.0x) until observed via combat events.
        public static float EstimateExpectedDamage(Combatant attacker, EnemyAttack move, Combatant target, Effectiveness? knownEffectiveness = null)
        {
            if (attacker == null || move == null || target == null || !target.Alive || move.hits <= 0) return 0f;
            float effMult = knownEffectiveness.HasValue ? AffinityResolver.DamageMultiplier(knownEffectiveness.Value) : 1f;
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
            // Fey Step (Elf): untelegraphed single-target attacks skip Phased heroes when non-phased targets exist.
            if (!move.allTargets && !move.SupportsAlly && !move.self)
            {
                var nonPhased = pool.Where(f => !f.IsPhased).ToList();
                if (nonPhased.Count > 0) pool = nonPhased;
            }
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
            if (rule == TargetRule.LowestMdef)
            {
                var unreserved = lethalReserved != null && lethalReserved.Count > 0
                    ? pool.Where(p => !lethalReserved.Contains(p)).ToList()
                    : pool;
                var activePool = unreserved.Count > 0 ? unreserved : pool;
                int minMdef = activePool.Min(p => p.MagicDef);
                return activePool.Where(p => p.MagicDef == minMdef).OrderBy(p => p.slot).ToList();
            }
            if (rule == TargetRule.HighestThreat)
            {
                var unreserved = lethalReserved != null && lethalReserved.Count > 0
                    ? pool.Where(p => !lethalReserved.Contains(p)).ToList()
                    : pool;
                var activePool = unreserved.Count > 0 ? unreserved : pool;
                var mem = b?.Blackboard?.Memory;
                int round = b?.Round ?? 1;
                float decay = AiProfile.For(self.enemy).memoryDecay;
                float maxThreat = activePool.Max(p => mem != null ? mem.GetObservedThreatScore(p, round, decay) : Math.Max(p.Atk, p.Magic));
                return activePool
                    .Where(p => Math.Abs((mem != null ? mem.GetObservedThreatScore(p, round, decay) : Math.Max(p.Atk, p.Magic)) - maxThreat) <= 0.001f)
                    .OrderBy(p => p.slot)
                    .ToList();
            }

            // FocusCapNormal (Spec §4.3 v1.1, Variant Spec §7.5 v1.1): at most FocusCapNormal Normal-tier (incl. Enhanced) enemies
            // plan single-target damage on the same hero per round. Elites, Minibosses, and Bosses are exempt.
            if (bb != null && self.enemy != null && self.enemy.aiTier.IsNormalTier() && move.hits > 0 && !move.telegraph)
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
            ScoredCandidate bestNoInstinct = null;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                var (m, dataIdx) = legalMoves[i];

                if (m.self || m.Summons || m.allTargets || m.SupportsAlly)
                {
                    var tgt = PriorityBrain.HealReady(m, ctx) ? ctx.hurtAlly
                        : m.BuffsAlly ? SupportTarget(b, self, m)
                        : m.Heals ? ctx.hurtAlly : null;
                    var cand = ScoreCandidate(b, self, m, dataIdx, tgt, profile, ctx, topThreat, lowestDef);
                    RecordCandidate(trace, cand);
                    if (best == null || IsBetter(cand, best)) best = cand;
                    if (bestNoAdapt == null || IsBetterNoAdapt(cand, bestNoAdapt)) bestNoAdapt = cand;
                    if (bestNoInstinct == null || IsBetterNoInstinct(cand, bestNoInstinct)) bestNoInstinct = cand;
                    continue;
                }

                var targets = CandidateTargets(b, self, m, ctx);
                for (int t = 0; t < targets.Count; t++)
                {
                    var cand = ScoreCandidate(b, self, m, dataIdx, targets[t], profile, ctx, topThreat, lowestDef);
                    RecordCandidate(trace, cand);
                    if (best == null || IsBetter(cand, best)) best = cand;
                    if (bestNoAdapt == null || IsBetterNoAdapt(cand, bestNoAdapt)) bestNoAdapt = cand;
                    if (bestNoInstinct == null || IsBetterNoInstinct(cand, bestNoInstinct)) bestNoInstinct = cand;
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
            if (best != null && bestNoInstinct != null && (best.move != bestNoInstinct.move || best.target != bestNoInstinct.target))
            {
                best.instinctShifted = true;
                if (string.IsNullOrEmpty(best.instinctReadKey) && !string.IsNullOrEmpty(bestNoInstinct.instinctReadKey))
                {
                    best.instinctReadKey = bestNoInstinct.instinctReadKey;
                    best.instinctReadText = bestNoInstinct.instinctReadText;
                }
            }
            return best;
        }

        static ScoredCandidate ScoreCandidate(Battle b, Combatant self, EnemyAttack m, int dataIndex, Combatant target,
            AiProfile profile, AiPlanContext ctx, Combatant topThreat, int lowestDef)
        {
            var mem = b?.Blackboard?.Memory;
            float baseScore = m.aiPriority * 10f;
            // Stage 6 (Analysis System): enemies avoid an element a hero has been SHOWN (in encounter memory) to Drain or Reflect.
            if (mem != null && m.hits > 0 && !m.self && !m.SupportsAlly && !m.Summons)
            {
                if (m.allTargets)
                {
                    for (int i = 0; i < ctx.foes.Count; i++)
                    {
                        var seen = mem.GetSeenEffectiveness(ctx.foes[i], m.element);
                        if (seen == Effectiveness.Reflect) baseScore -= 35f;
                        else if (seen == Effectiveness.Drain) baseScore -= 30f;
                    }
                }
                else if (target != null)
                {
                    var seen = mem.GetSeenEffectiveness(target, m.element);
                    if (seen == Effectiveness.Reflect) baseScore -= 50f;
                    else if (seen == Effectiveness.Drain) baseScore -= 45f;
                }
            }
            float expectedDmg = 0f;
            float immediate = EvaluateImmediate(b, self, m, target, ctx, out expectedDmg);
            float future = 0f;
            float role = EvaluateRoleBonus(self, m, target, profile, ctx, topThreat, lowestDef);
            float instinct = InstinctLayer.Evaluate(b, self, m, target, profile, ctx, topThreat, out string instKey, out string instText);
            float synergy = TacticalSynergy.Evaluate(b, self, m, target, profile, ctx, expectedDmg);
            float objective = 0f;
            float adaptation = EvaluateAdaptation(b, self, m, target, profile, ctx, out string readKey, out string readText);
            float resource = EvaluateResourceCost(self, m);
            int streak = mem != null ? mem.GetIneffectiveStreak(self, m.name, target?.slot ?? -1) : 0;
            float repetition = streak * EncounterTacticalMemory.RepetitionPenaltyPerStreak;
            float risk = EvaluateRisk(b, self, m, target, ctx);

            float rawNoAdapt = immediate + future + role + instinct + synergy + objective - resource - repetition - risk;
            float clampedNoAdapt = Math.Clamp(rawNoAdapt, -UtilityClamp, UtilityClamp);
            float rawNoInstinct = immediate + future + role + synergy + objective + adaptation - resource - repetition - risk;
            float clampedNoInstinct = Math.Clamp(rawNoInstinct, -UtilityClamp, UtilityClamp);
            float raw = rawNoAdapt + adaptation;
            float clamped = Math.Clamp(raw, -UtilityClamp, UtilityClamp);

            return new ScoredCandidate
            {
                move = m,
                dataIndex = dataIndex,
                target = target,
                charging = m.telegraph && !m.self && !m.SupportsAlly && !m.Summons,
                expectedDamage = expectedDmg,
                baseScore = baseScore,
                immediate = immediate,
                future = future,
                role = role,
                instinct = instinct,
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
                noInstinctTotalScore = baseScore + clampedNoInstinct,
                repetitionCount = streak,
                readKey = readKey,
                readText = readText,
                instinctReadKey = instKey,
                instinctReadText = instText,
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
            if (m.telegraph && !m.self && !m.SupportsAlly && !m.Summons && self.HasBreakGauge)
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
            if (target != null && target.IsPlayer && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons)
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

                    // 3. Support Dependent hero X -> +Target on X (Disruptor / Executioner count double) + anti-heal disruption.
                    if (prof.IsRecognized(BehaviorCategory.SupportDependent, round, profile))
                    {
                        float mult = (profile.role == AiRole.Disruptor || profile.role == AiRole.Executioner) ? 2f : 1f;
                        Add(EncounterTacticalMemory.SupportDependentBonus * mult,
                            $"support_dep:{target.slot}",
                            $"They mark the healer, {target.name}.");
                        if (HasAnyStatus(m, StatusId.Silence, StatusId.Despair, StatusId.Curse))
                        {
                            Add(EncounterTacticalMemory.AntiHealStatusBonus,
                                $"anti_heal:{target.slot}",
                                $"They sense {target.name}'s mending and move to cut it off.");
                        }
                    }
                    else if (prof.healedThisRound && (profile.role == AiRole.Disruptor || profile.role == AiRole.Executioner || profile.tier >= AiTier.Elite))
                    {
                        if (HasAnyStatus(m, StatusId.Silence, StatusId.Despair, StatusId.Curse))
                        {
                            Add(EncounterTacticalMemory.AntiHealStatusBonus * 0.75f,
                                $"anti_heal:{target.slot}",
                                $"They sense {target.name}'s mending and move to cut it off.");
                        }
                        else if (m.hits > 0)
                        {
                            Add(EncounterTacticalMemory.SameRoundHealerPressureBonus,
                                $"same_round_heal:{target.slot}",
                                $"They react to {target.name}'s healing.");
                        }
                    }

                    // 4b. Elemental Specialist / repeated spellcaster X -> Attackers pressure X; disruptors silence/fear/despair X.
                    bool recognizedElem = prof.IsRecognized(BehaviorCategory.ElementalSpecialist, round, profile);
                    if (recognizedElem && prof.exploitedWeaknessEnemies.Count > 0)
                    {
                        Add(EncounterTacticalMemory.ElementalSpecialistTargetBonus,
                            $"elem_spec:{target.slot}",
                            $"They shield what burns — {target.name} draws their ire.");
                    }
                    if ((recognizedElem || (prof.spellCastCount >= 2 && prof.castSpellThisRound))
                        && HasAnyStatus(m, StatusId.Silence, StatusId.Despair, StatusId.Fear))
                    {
                        Add(EncounterTacticalMemory.AntiCasterStatusBonus,
                            $"anti_cast:{target.slot}",
                            $"They read {target.name}'s spellcraft and move to disrupt it.");
                    }

                    // 6. Burst hero X -> prioritize pressuring X and bracing against burst output.
                    if (prof.IsRecognized(BehaviorCategory.BurstDamage, round, profile))
                    {
                        Add(EncounterTacticalMemory.BurstTargetBonus,
                            $"burst:{target.slot}",
                            $"They brace against {target.name}.");
                    }

                    // 7. Repeated Counter-Stance hero X -> favor status disruption over walking into direct physical counters.
                    if (prof.counterStanceCount >= 2 && target.CounterTurns > 0
                        && HasAnyStatus(m, StatusId.Silence, StatusId.Fear, StatusId.Despair, StatusId.Stun))
                    {
                        Add(EncounterTacticalMemory.CounterDisruptBonus,
                            $"counter_read:{target.slot}",
                            $"They watch {target.name}'s counter stance carefully.");
                    }
                }
            }

            readKey = bestKey;
            readText = bestText;
            return total;
        }

        static bool HasAnyStatus(EnemyAttack m, params StatusId[] ids)
        {
            if (m?.apply == null || m.apply.Length == 0 || ids == null) return false;
            for (int i = 0; i < m.apply.Length; i++)
                for (int j = 0; j < ids.Length; j++)
                    if (m.apply[i].id == ids[j]) return true;
            return false;
        }

        static float EvaluateRisk(Battle b, Combatant self, EnemyAttack m, Combatant target, AiPlanContext ctx)
        {
            float risk = 0f;
            var bb = b?.Blackboard;
            if (target != null && target.IsPlayer && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons)
            {
                // Lethal reservation (Spec §4.3, Arch §7.3): penalize overcommitting damage onto a hero already covered by earlier planners.
                if (bb != null && bb.IsLethalReserved(target))
                    risk += EncounterBlackboard.LethalReservationPenalty;

                // Same-round observable Guard awareness: untelegraphed non-Heavy damage pivots away from a guarding hero when open targets exist.
                if (target.guarding && m.hits > 0 && !m.HasHeavy && ctx != null && ctx.foes.Any(f => !f.guarding))
                    risk += EncounterTacticalMemory.SameRoundGuardRiskPenalty;

                // Observable active Counter stance awareness: avoid feeding free counter hits when non-countering targets exist.
                if (target.CounterTurns > 0 && m.hits > 0 && ctx != null && ctx.foes.Any(f => f.CounterTurns <= 0))
                    risk += EncounterTacticalMemory.ActiveCounterRiskPenalty;
            }
            return risk;
        }

        static float EvaluateImmediate(Battle b, Combatant self, EnemyAttack m, Combatant target, AiPlanContext ctx, out float expectedDmg)
        {
            expectedDmg = 0f;
            if (m.Summons)
                return Math.Max(0, m.summonCount - ctx.livingSummons) * ImmediateSummonPerSlot;
            if (m.SupportsAlly)
            {
                if (target == null) return 0f;
                float support = m.Heals ? (1f - (float)target.unit.hp / Math.Max(1, target.Stats.maxHp)) * ImmediateHealScale : 0f;
                foreach (var ap in m.buffAlly ?? Array.Empty<StatusApply>())
                    if (!target.Has(ap.id)) support += ImmediateBuffValue;
                return support;
            }
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
                    if (knownEff == Effectiveness.Reflect) { val -= 12f; continue; }
                    if (knownEff == Effectiveness.Drain) { val -= 10f; continue; }
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
                    if (knownEff == Effectiveness.Reflect) return -20f;
                    if (knownEff == Effectiveness.Drain) return -16f;
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
                    if (PriorityBrain.HealReady(m, ctx)) return AiProfile.SupportHealWeight;
                    if (m.BuffsAlly) return AiProfile.SupportBuffWeight;
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
            float cost = ((float)m.mpCost / self.Stats.maxMp) * ResourceCostWeight;
            var profile = AiProfile.For(self.enemy);
            // Variant Spec §7.5: Enhanced+ has resource/cooldown awareness over its next turn opportunity.
            // If spending MP on a lower-priority skill now would prevent affording a higher-priority skill coming off cooldown next turn (cd == 1), penalize it.
            if (profile.usesNextTurnResourceAwareness && self.enemy?.attacks != null)
            {
                int mpLeft = self.unit.mp - m.mpCost;
                foreach (var nextMove in self.enemy.attacks)
                {
                    if (nextMove == m || nextMove.mpCost <= 0 || nextMove.aiPriority <= m.aiPriority) continue;
                    int cd = self.Cooldown(Combatant.MoveKey(nextMove));
                    if (cd == 1 && mpLeft < nextMove.mpCost && self.unit.mp >= nextMove.mpCost)
                    {
                        cost += 4f;
                        break;
                    }
                }
            }
            return cost;
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

        static bool IsBetterNoInstinct(ScoredCandidate a, ScoredCandidate b)
        {
            const float eps = 0.0001f;
            if (Math.Abs(a.noInstinctTotalScore - b.noInstinctTotalScore) > eps) return a.noInstinctTotalScore > b.noInstinctTotalScore;
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
                instinct = cand.instinct,
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
