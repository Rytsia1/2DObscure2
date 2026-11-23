using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Normal enemies: any offensive move at a random living party member.
    public class BasicBrain : IEnemyBrain
    {
        public BattleAction Decide(Battle b, Combatant self) =>
            EnemyAI.Use(b, self, EnemyAI.Pick(b, EnemyAI.Offensive(self)));
    }

    // Elite enemies: brace when hurt, weaken an unweakened party, otherwise hunt the weakest member.
    public class TacticalBrain : IEnemyBrain
    {
        public const float BraceBelow = 0.5f, DebuffChance = 0.35f;

        public BattleAction Decide(Battle b, Combatant self)
        {
            var moves = EnemyAI.Moves(self);
            var stance = moves.FirstOrDefault(m => m.self);
            if (stance != null && self.unit.hp <= self.Stats.maxHp * BraceBelow && !self.Has(StatusId.DefenseUp))
                return EnemyAI.Use(b, self, stance);

            var debuff = moves.FirstOrDefault(m => m.allTargets && m.apply != null);
            if (debuff != null && !b.Party.Any(p => p.Alive && p.Has(StatusId.AttackDown)) && b.Rng.NextDouble() < DebuffChance)
                return EnemyAI.Use(b, self, debuff);

            var weakest = b.Party.Where(p => p.Alive).OrderBy(p => p.unit.hp).First();
            var strikes = moves.Where(m => !m.self && !m.allTargets).ToList();
            return EnemyAI.Use(b, self, EnemyAI.Pick(b, strikes), weakest);
        }
    }

    // Roster enemies: deterministic, data-configured policy.
    // Committed Intents (Charge, Heavy, StatusOnly, Summons, NeedsTelegraph) are locked at round preparation (Telegraph Lock).
    // Provisional Actions (ordinary untelegraphed moves) are published for coordination at round start and re-evaluated on turn arrival
    // strictly over non-NeedsTelegraph moves so telegraphs stay 100% honest while enemies react to mid-round state.
    public class PriorityBrain : IEnemyBrain
    {
        public BattleAction Decide(Battle b, Combatant self)
        {
            // 1. Pending Charge release — always Committed (Attack Commitment / Telegraph Lock).
            if (self.Charge != null)
            {
                var t = self.Charge.target;
                if (t != null && !t.Alive) t = NextInPartyOrder(b, t); // telegraphed target fell: deterministic redirect (Bab 12 §6.4)
                return t == null && !self.Charge.move.allTargets ? null : EnemyAI.Use(b, self, self.Charge.move, t);
            }

            var plan = self.Intent;

            // 2. Committed Intent (Charge prep, Heavy, StatusOnly, Summon, or any NeedsTelegraph move):
            // Telegraph Lock — execute as published while legal and NEVER re-score.
            if (plan != null && plan.IsCommitted)
            {
                bool moveLegal = self.unit.mp >= plan.move.mpCost
                    && self.Cooldown(Combatant.MoveKey(plan.move)) == 0
                    && !(plan.move.telegraph && self.JustReleasedCharge);

                if (moveLegal && plan.target != null && !plan.target.Alive && !plan.move.SupportsAlly)
                {
                    var redirect = NextInPartyOrder(b, plan.target);
                    if (redirect != null)
                    {
                        plan = new EnemyIntent
                        {
                            move = plan.move,
                            target = redirect,
                            charging = plan.charging,
                            commitment = IntentCommitment.Committed,
                        };
                        return plan.charging ? BattleAction.Charge(self, plan.move, plan.target) : EnemyAI.Use(b, self, plan.move, plan.target);
                    }
                }

                if (moveLegal && (plan.target == null || plan.target.Alive))
                    return plan.charging ? BattleAction.Charge(self, plan.move, plan.target) : EnemyAI.Use(b, self, plan.move, plan.target);

                // Committed intent became genuinely illegal: fall back to a legal action (preferring untelegraphed first).
                b.Blackboard?.BeginReevaluation(b, self);
                var fbSw = System.Diagnostics.Stopwatch.StartNew();
                plan = Select(b, self, out var fbTrace, allowTelegraph: false) ?? Select(b, self, out fbTrace, allowTelegraph: true);
                fbSw.Stop();
                self.Intent = plan;
                b.Blackboard?.Publish(b, self, plan);
                if (fbTrace != null)
                {
                    fbTrace.usedFallback = true;
                    fbTrace.decisionMs = fbSw.Elapsed.TotalMilliseconds;
                    b.AiTrace?.Record(fbTrace);
                }
                if (plan == null) return null;
                return plan.charging ? BattleAction.Charge(self, plan.move, plan.target) : EnemyAI.Use(b, self, plan.move, plan.target);
            }

            // 3. Provisional Action (ordinary untelegraphed move, or no round-start plan):
            // Re-evaluate live battlefield on turn arrival, strictly restricted to non-NeedsTelegraph moves
            // so enemies never spring an unannounced Heavy, Charge, StatusOnly, or Summon mid-round.
            b.Blackboard?.BeginReevaluation(b, self);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var fresh = Select(b, self, out var reevalTrace, allowTelegraph: false);
            bool usedFallback = false;
            if (fresh == null)
            {
                if (plan != null && (plan.target == null || plan.target.Alive)
                    && self.unit.mp >= plan.move.mpCost && self.Cooldown(Combatant.MoveKey(plan.move)) == 0
                    && !(plan.move.telegraph && self.JustReleasedCharge))
                {
                    fresh = plan;
                }
                else
                {
                    fresh = Select(b, self, out reevalTrace, allowTelegraph: true);
                    usedFallback = true;
                }
            }
            sw.Stop();
            self.Intent = fresh;
            b.Blackboard?.Publish(b, self, fresh);
            if (reevalTrace != null)
            {
                reevalTrace.isReevaluation = true;
                reevalTrace.usedFallback = usedFallback;
                reevalTrace.reevalChangedDecision = plan == null || fresh == null || plan.move != fresh.move || plan.target != fresh.target;
                reevalTrace.decisionMs = sw.Elapsed.TotalMilliseconds;
                b.AiTrace?.Record(reevalTrace);
            }
            if (fresh == null) return null;
            return fresh.charging ? BattleAction.Charge(self, fresh.move, fresh.target) : EnemyAI.Use(b, self, fresh.move, fresh.target);
        }

        public static EnemyIntent Select(Battle b, Combatant self, bool allowTelegraph = true) => Select(b, self, out _, allowTelegraph);

        public static EnemyIntent Select(Battle b, Combatant self, out AiDecisionTrace trace, bool allowTelegraph = true)
        {
            int traceTurnIdx = (b.Active == self && self.turnsTakenThisRound > 0)
                ? self.turnsTakenThisRound - 1
                : self.turnsTakenThisRound;
            trace = (b.AiTrace != null && b.AiTrace.Enabled && self.enemy != null)
                ? new AiDecisionTrace
                {
                    round = b.Round,
                    turnIndex = traceTurnIdx,
                    enemyId = self.enemy.id,
                    enemyName = self.name,
                    enemySlot = self.slot,
                    enemyTier = self.enemy.tier,
                }
                : null;

            var ctx = BuildContext(b, self);
            if (ctx.foes.Count == 0)
            {
                if (trace != null) trace.reason = "no_living_foes";
                return null;
            }

            var moves = EnemyAI.Moves(self);
            var legal = new List<(EnemyAttack move, int dataIndex)>();
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                if (IsLegal(b, self, m, ctx, out string reject, allowTelegraph))
                {
                    legal.Add((m, i));
                }
                else if (trace != null)
                {
                    trace.candidates.Add(new AiCandidateTrace
                    {
                        moveName = m.name,
                        aiPriority = m.aiPriority,
                        mpCost = m.mpCost,
                        legal = false,
                        rejectReason = reject,
                    });
                }
            }

            if (legal.Count == 0)
            {
                if (trace != null) trace.reason = "no_legal_action";
                return null;
            }

            // Mandatory layer (Spec §3.2, Architecture §8.2): pending charge release, threshold heal/MP restore, or top-priority summon.
            var forced = ForcedChoice(b, self, legal, ctx, out string forcedReason);
            if (forced != null)
                return RecordChoice(trace, forced, forcedReason, wasForced: true);

            // Utility layer (Spec §3.3, Architecture §8.4): scores only legal candidates and resolves targets deterministically (zero Rng).
            var best = UtilityScorer.SelectBest(b, self, legal, ctx, trace);
            if (best == null)
            {
                if (trace != null) trace.reason = "no_scored_candidate";
                return null;
            }

            var intent = new EnemyIntent
            {
                move = best.move,
                target = best.target,
                charging = best.charging,
                commitment = EnemyIntent.Classify(best.move, best.charging),
            };
            string pickReason = best.move.Summons ? "utility_summon"
                : best.move.SupportsAlly ? (best.move.Heals ? "utility_heal" : "utility_buff")
                : best.move.self ? "utility_self"
                : best.move.allTargets ? "utility_aoe"
                : "utility_single";
            if (trace != null) trace.adaptationChangedDecision = best.adaptationShifted;
            // Readable Adaptation & Instincts (Spec §5.7, Arch §11.5): log a one-line "read" the first time a response shifts a decision.
            if (best.adaptationShifted && !string.IsNullOrEmpty(best.readKey))
                b.Blackboard?.Memory?.TryLogRead(b, best.readKey, best.readText);
            else if (best.instinctShifted && !string.IsNullOrEmpty(best.instinctReadKey))
                b.Blackboard?.Memory?.TryLogRead(b, best.instinctReadKey, best.instinctReadText);
            return RecordChoice(trace, intent, pickReason, wasForced: false);
        }

        public static AiPlanContext BuildContext(Battle b, Combatant self)
        {
            var foes = b.Opponents(self).Where(c => c.Alive).ToList();
            bool heavyTaken = self.UsedHeavyThisRound
                || (b.Blackboard != null && b.Blackboard.HeavyReservedTargets.Count > 0)
                || b.Enemies.Any(e => e != self && e.Alive && (e.UsedHeavyThisRound || (e.Intent?.move.HasHeavy ?? false) || (e.Charge?.move.HasHeavy ?? false)));
            int livingSummons = b.Enemies.Count(e => e.Alive && e.IsSummon);
            var hurt = b.Allies(self).Where(c => c.Alive)
                .OrderBy(c => (c.unit.hp + (b.Blackboard?.ExpectedHeal(c) ?? 0f)) / c.Stats.maxHp)
                .ThenBy(c => c.slot).FirstOrDefault();
            float hurtFrac = hurt != null ? (hurt.unit.hp + (b.Blackboard?.ExpectedHeal(hurt) ?? 0f)) / hurt.Stats.maxHp : 1f;
            return new AiPlanContext
            {
                foes = foes,
                heavyTaken = heavyTaken,
                livingSummons = livingSummons,
                hurtAlly = hurt,
                hurtAllyFrac = hurtFrac,
            };
        }

        public static bool HealReady(EnemyAttack m, AiPlanContext ctx) =>
            m != null && m.Heals && ctx != null && ctx.hurtAlly != null && ctx.hurtAllyFrac <= m.aiAllyHpBelow;

        // Mandatory hard gates (Spec §3.2): every move must pass all applicable gates before entering UtilityScorer.
        public static bool IsLegal(Battle b, Combatant self, EnemyAttack m, AiPlanContext ctx, out string rejectReason, bool allowTelegraph = true)
        {
            rejectReason = null;
            if (!allowTelegraph && m.NeedsTelegraph) { rejectReason = "provisional_no_telegraph"; return false; }
            string moveKey = Combatant.MoveKey(m);
            if (self.Cooldown(moveKey) > 0 || (!allowTelegraph && self.cooldownsJustExpiredThisTurn.Contains(moveKey)))
            {
                rejectReason = "cooldown";
                return false;
            }
            if (self.unit.mp < m.mpCost) { rejectReason = "mp"; return false; }
            if (m.telegraph && self.JustReleasedCharge) { rejectReason = "charge_gap"; return false; } // ponytail: bool = gap of 1; counter if ChargeGapTurns > 1
            if (m.Summons)
            {
                if (ctx.livingSummons >= m.summonCount) { rejectReason = "summon_cap"; return false; }
                return true;
            }
            if (m.aiMpBelow > 0f && self.unit.mp >= self.Stats.maxMp * m.aiMpBelow) { rejectReason = "mp_above_threshold"; return false; }
            if (m.aiUnlessAllyHpBelow > 0f && ctx.hurtAllyFrac <= m.aiUnlessAllyHpBelow) { rejectReason = "ally_critical_block"; return false; }
            if (m.SupportsAlly)
            {
                bool healOk = HealReady(m, ctx);
                bool buffOk = m.BuffsAlly && UtilityScorer.SupportTarget(b, self, m) != null;
                if (!healOk && !buffOk)
                {
                    rejectReason = m.Heals ? "heal_threshold" : "no_buff_target";
                    return false;
                }
                return true;
            }
            if (m.HasHeavy && ctx.heavyTaken) { rejectReason = "heavy_taken"; return false; }
            if (m.aiBreakLeftBelow > 0f && self.HasBreakGauge && b.Round < m.aiBreakGateRound
                && self.CurrentBreakGauge > self.MaxBreakGauge * m.aiBreakLeftBelow) { rejectReason = "break_gate"; return false; }
            if (m.self) return true;
            if (m.allTargets)
            {
                if (ctx.foes.Count < m.minTargets) { rejectReason = "min_targets"; return false; }
                if (b.Blackboard != null && b.Blackboard.WouldViolateStunHeavy(m, null, ctx.foes))
                {
                    rejectReason = "stun_heavy_conflict";
                    return false;
                }
                return true;
            }
            if (m.StatusOnly && m.apply != null && !ctx.foes.Any(f => !m.apply.Any(ap => f.Has(ap.id))))
            {
                rejectReason = "no_fresh_status_target";
                return false;
            }
            if (b.Blackboard != null && (m.HasHeavy || (m.apply != null && m.apply.Any(ap => ap.id == StatusId.Stun))))
            {
                if (!ctx.foes.Any(f => !b.Blackboard.WouldViolateStunHeavy(m, f, ctx.foes)))
                {
                    rejectReason = "stun_heavy_conflict";
                    return false;
                }
            }
            return ctx.foes.Count > 0;
        }

        // Mandatory forced choices (Handoff Architecture table): pending charge release, threshold heal, threshold MP restore,
        // or summon when allowed and not outranked by a higher-priority legal move.
        public static EnemyIntent ForcedChoice(Battle b, Combatant self, List<(EnemyAttack move, int dataIndex)> legal,
            AiPlanContext ctx, out string reason)
        {
            reason = null;
            if (self.Charge != null)
            {
                reason = "forced_charge_release";
                return new EnemyIntent { move = self.Charge.move, target = self.Charge.target, commitment = IntentCommitment.Committed };
            }
            int topPriority = legal.Max(x => x.move.aiPriority);
            foreach (var (m, _) in legal.OrderByDescending(x => x.move.aiPriority).ThenBy(x => x.dataIndex))
            {
                if (HealReady(m, ctx))
                {
                    reason = "forced_heal";
                    return new EnemyIntent { move = m, target = ctx.hurtAlly, commitment = EnemyIntent.Classify(m, false) };
                }
                if (m.aiMpBelow > 0f && self.unit.mp < self.Stats.maxMp * m.aiMpBelow)
                {
                    reason = "forced_mp_restore";
                    return new EnemyIntent { move = m, commitment = EnemyIntent.Classify(m, false) };
                }
                if (m.Summons && ctx.livingSummons < m.summonCount && m.aiPriority >= topPriority)
                {
                    reason = "forced_summon";
                    return new EnemyIntent { move = m, commitment = IntentCommitment.Committed };
                }
            }
            return null;
        }

        static EnemyIntent RecordChoice(AiDecisionTrace trace, EnemyIntent intent, string reason, bool wasForced)
        {
            if (trace != null && intent != null)
            {
                trace.chosenMove = intent.move.name;
                trace.chosenTarget = intent.target?.name;
                trace.chosenTargetSlot = intent.target?.slot ?? -1;
                trace.charging = intent.charging;
                trace.telegraphed = intent.move.NeedsTelegraph;
                trace.commitment = intent.commitment;
                trace.wasForced = wasForced;
                trace.reason = reason;
                if (wasForced)
                {
                    trace.candidates.Add(new AiCandidateTrace
                    {
                        moveName = intent.move.name,
                        targetName = intent.target?.name,
                        targetSlot = intent.target?.slot ?? -1,
                        aiPriority = intent.move.aiPriority,
                        mpCost = intent.move.mpCost,
                        charging = intent.charging,
                        legal = true,
                        forced = true,
                        baseScore = intent.move.aiPriority * 10f,
                        totalScore = intent.move.aiPriority * 10f,
                    });
                }
            }
            return intent;
        }

        static Combatant NextInPartyOrder(Battle b, Combatant fallen)
        {
            var party = b.Party;
            return party.Where(p => p.Alive).OrderBy(p => (p.slot - fallen.slot + party.Count) % party.Count).FirstOrDefault();
        }
    }

    // Bosses: the move list is gated by phase (EnemyAttack.phase); phase 2 is triggered in Battle at an HP threshold.
    // In phase 2 it favors its area attack while three or more party members still stand.
    public class BossBrain : IEnemyBrain
    {
        public BattleAction Decide(Battle b, Combatant self)
        {
            var moves = EnemyAI.Offensive(self);
            var area = moves.FirstOrDefault(m => m.allTargets);
            if (self.phase >= 2 && area != null && b.Party.Count(p => p.Alive) >= 3 && b.Rng.NextDouble() < 0.5)
                return EnemyAI.Use(b, self, area);
            return EnemyAI.Use(b, self, EnemyAI.Pick(b, moves));
        }
    }
}
