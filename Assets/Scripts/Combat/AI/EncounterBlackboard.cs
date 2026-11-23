using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Obscure
{
    // One enemy's published plan on the Shared Blackboard (Spec §12, Architecture §14.6).
    public sealed class PublishedIntent
    {
        public Combatant enemy;
        public EnemyAttack move;
        public Combatant target;
        public bool charging;
        public IntentCommitment commitment;
        public bool executed;
        public float expectedDamage;
        public int expectedBreak;
        public StatusId[] plannedStatuses;
        public int planningRound;
        public int turnIndex;
        public bool hasHeavy;
        public bool appliesStun;
    }

    // Shared Encounter Blackboard (Spec §4, Architecture §7, §14.4).
    // Exactly one instance per Battle, created in Battle ctor and discarded when the encounter ends.
    // Shared by all enemies (including summons) and survives boss phase transitions.
    public sealed class EncounterBlackboard
    {
        // posture-B starting value (untested).
        // Pass test: Normal-encounter party deaths <= pre-AI baseline + 15%. If it fails: 1.
        public const int FocusCapNormal = 2;

        // posture-B starting value (untested).
        // Pass test: Overkill Rate drops below Stage 0 baseline while enemies still secure kills.
        // If it fails: adjust inside UtilityClamp (+/-15).
        public const float LethalReservationPenalty = 10f;

        public int CurrentRound { get; private set; }
        public double LastUpdateMs { get; private set; }

        // Single shared encounter tactical memory & player behavior profiles (Spec §5.3 v1.1, Arch §4.2 v1.1).
        public readonly EncounterTacticalMemory Memory = new EncounterTacticalMemory();

        public readonly List<PublishedIntent> PlannedActions = new List<PublishedIntent>();
        public readonly Dictionary<Combatant, float> CommittedDamageByTarget = new Dictionary<Combatant, float>();
        public readonly Dictionary<Combatant, int> CommittedBreakByTarget = new Dictionary<Combatant, int>();
        public readonly Dictionary<Combatant, HashSet<StatusId>> PlannedStatusesByTarget = new Dictionary<Combatant, HashSet<StatusId>>();
        public readonly HashSet<Combatant> LethalReservations = new HashSet<Combatant>();
        public readonly Dictionary<Combatant, int> NormalSingleTargetCountByHero = new Dictionary<Combatant, int>();
        public readonly HashSet<Combatant> HeavyReservedTargets = new HashSet<Combatant>();
        public readonly HashSet<Combatant> StunReservedTargets = new HashSet<Combatant>();
        public readonly Dictionary<Combatant, float> ExpectedHealByEnemy = new Dictionary<Combatant, float>();

        // Fixed planning order (Spec §4.5, Architecture §13.2): Support -> Controller -> other roles, slot ties.
        // Independent of initiative / Speed buffs.
        public static List<Combatant> PlanningOrder(IEnumerable<Combatant> enemies) =>
            enemies.Where(e => e != null && e.Alive)
                .OrderBy(e => AiProfile.PlanningGroup(e.enemy?.aiRole ?? AiRole.Vanguard))
                .ThenBy(e => e.slot)
                .ToList();

        // Reservation expiration: end of round / rebuilt each planning pass (Architecture §18).
        public void BeginRound(Battle b)
        {
            var sw = Stopwatch.StartNew();
            CurrentRound = b.Round;
            PlannedActions.Clear();
            CommittedDamageByTarget.Clear();
            CommittedBreakByTarget.Clear();
            PlannedStatusesByTarget.Clear();
            LethalReservations.Clear();
            NormalSingleTargetCountByHero.Clear();
            HeavyReservedTargets.Clear();
            StunReservedTargets.Clear();
            ExpectedHealByEnemy.Clear();
            Memory.BeginRound(b.Round);
            sw.Stop();
            LastUpdateMs = sw.Elapsed.TotalMilliseconds;
        }

        public void Publish(Battle b, Combatant enemy, EnemyIntent intent)
        {
            if (enemy == null) return;
            var sw = Stopwatch.StartNew();

            // Replace any prior unexecuted reservation for this enemy (round-start provisional or mid-round fallback/reeval).
            // Already-executed entries from earlier turns this round (e.g. Zoroa turn A) stay recorded for Heavy/Stun/FocusCap.
            bool removed = PlannedActions.RemoveAll(p => p.enemy == enemy && !p.executed) > 0;
            if (removed) RebuildAggregates(b);

            if (intent?.move != null)
            {
                var m = intent.move;
                bool appliesStun = m.apply != null && m.apply.Any(ap => ap.id == StatusId.Stun);
                float dmg = (!intent.charging && m.hits > 0 && !m.allTargets && !m.self && !m.SupportsAlly && !m.Summons && intent.target != null)
                    ? UtilityScorer.EstimateExpectedDamage(enemy, m, intent.target)
                    : 0f;
                int expectedBreak = (!intent.charging && m.hits > 0 && !m.self && !m.SupportsAlly && !m.Summons)
                    ? (Combatant.EnemyBaseBreakPower(enemy.enemy?.tier ?? Tier.Normal) + m.breakPower)
                    : 0;
                StatusId[] plannedStatuses = (!intent.charging && m.apply != null && m.apply.Length > 0)
                    ? m.apply.Select(ap => ap.id).Distinct().ToArray()
                    : null;

                var pub = new PublishedIntent
                {
                    enemy = enemy,
                    move = m,
                    target = intent.target,
                    charging = intent.charging,
                    commitment = intent.commitment,
                    executed = false,
                    expectedDamage = dmg,
                    expectedBreak = expectedBreak,
                    plannedStatuses = plannedStatuses,
                    planningRound = b.Round,
                    turnIndex = (b.Active == enemy && enemy.turnsTakenThisRound > 0)
                        ? enemy.turnsTakenThisRound - 1
                        : enemy.turnsTakenThisRound,
                    hasHeavy = m.HasHeavy,
                    appliesStun = appliesStun,
                };
                PlannedActions.Add(pub);
                ApplyAggregate(b, pub);
            }

            sw.Stop();
            LastUpdateMs += sw.Elapsed.TotalMilliseconds;
        }

        // Clears this enemy's unexecuted provisional entry right before turn-arrival re-evaluation so it never blocks itself.
        public void BeginReevaluation(Battle b, Combatant enemy) => ClearPendingForEnemy(b, enemy);

        // Marks an enemy's current pending plan as executed after Battle.Perform: retires projected damage/heal/break/statuses
        // (already reflected in live battlefield state) while preserving round-wide Heavy/Stun/FocusCap constraints.
        public void MarkExecuted(Battle b, Combatant enemy)
        {
            if (enemy == null) return;
            bool changed = false;
            for (int i = 0; i < PlannedActions.Count; i++)
            {
                if (PlannedActions[i].enemy == enemy && !PlannedActions[i].executed)
                {
                    PlannedActions[i].executed = true;
                    changed = true;
                }
            }
            if (changed) RebuildAggregates(b);
        }

        // Clears only unexecuted pending reservations for an enemy (e.g. on death, Break, Mind Control, Transform).
        public void ClearPendingForEnemy(Battle b, Combatant enemy)
        {
            if (enemy == null) return;
            if (PlannedActions.RemoveAll(p => p.enemy == enemy && !p.executed) > 0)
                RebuildAggregates(b);
        }

        public void ClearEnemy(Battle b, Combatant enemy) => ClearPendingForEnemy(b, enemy);

        public void ReplaceEnemy(Combatant oldEnemy, Combatant newEnemy)
        {
            if (oldEnemy == null || newEnemy == null) return;
            for (int i = 0; i < PlannedActions.Count; i++)
            {
                if (PlannedActions[i].enemy == oldEnemy) PlannedActions[i].enemy = newEnemy;
                if (PlannedActions[i].target == oldEnemy) PlannedActions[i].target = newEnemy;
            }
            if (ExpectedHealByEnemy.TryGetValue(oldEnemy, out float h))
            {
                ExpectedHealByEnemy.Remove(oldEnemy);
                ExpectedHealByEnemy[newEnemy] = h;
            }
            Memory.ReplaceEnemy(oldEnemy, newEnemy);
        }

        public int NormalSingleTargetCount(Combatant hero) =>
            hero != null && NormalSingleTargetCountByHero.TryGetValue(hero, out int c) ? c : 0;

        public float CommittedDamage(Combatant hero) =>
            hero != null && CommittedDamageByTarget.TryGetValue(hero, out float d) ? d : 0f;

        public int CommittedBreak(Combatant hero) =>
            hero != null && CommittedBreakByTarget.TryGetValue(hero, out int brk) ? brk : 0;

        public bool HasPlannedStatus(Combatant hero, StatusId status) =>
            hero != null && PlannedStatusesByTarget.TryGetValue(hero, out var set) && set.Contains(status);

        public float ExpectedHeal(Combatant ally) =>
            ally != null && ExpectedHealByEnemy.TryGetValue(ally, out float h) ? h : 0f;

        public bool IsLethalReserved(Combatant hero) => hero != null && LethalReservations.Contains(hero);

        public bool WouldViolateStunHeavy(EnemyAttack move, Combatant target, List<Combatant> foes)
        {
            if (move == null) return false;
            bool isHeavy = move.HasHeavy;
            bool isStun = move.apply != null && move.apply.Any(ap => ap.id == StatusId.Stun);
            if (!isHeavy && !isStun) return false;

            if (move.allTargets)
            {
                for (int i = 0; i < foes.Count; i++)
                {
                    var f = foes[i];
                    if (isHeavy && (StunReservedTargets.Contains(f) || f.Has(StatusId.Stun))) return true;
                    if (isStun && HeavyReservedTargets.Contains(f)) return true;
                }
                return false;
            }
            if (target == null) return false;
            if (isHeavy && (StunReservedTargets.Contains(target) || target.Has(StatusId.Stun))) return true;
            if (isStun && HeavyReservedTargets.Contains(target)) return true;
            return false;
        }

        void RebuildAggregates(Battle b)
        {
            CommittedDamageByTarget.Clear();
            CommittedBreakByTarget.Clear();
            PlannedStatusesByTarget.Clear();
            LethalReservations.Clear();
            NormalSingleTargetCountByHero.Clear();
            HeavyReservedTargets.Clear();
            StunReservedTargets.Clear();
            ExpectedHealByEnemy.Clear();
            for (int i = 0; i < PlannedActions.Count; i++)
                ApplyAggregate(b, PlannedActions[i]);
        }

        void ApplyAggregate(Battle b, PublishedIntent pub)
        {
            var m = pub.move;
            if (m == null) return;

            if (m.SupportsAlly)
            {
                if (!pub.executed && m.Heals && pub.target != null && pub.target.Alive)
                {
                    float healAmt = (float)Math.Floor(pub.target.Stats.maxHp * m.healAlly);
                    ExpectedHealByEnemy[pub.target] = ExpectedHeal(pub.target) + healAmt;
                }
                return;
            }

            if (pub.charging) return; // charge lands next turn, not this round

            if (m.allTargets)
            {
                for (int i = 0; i < b.Party.Count; i++)
                {
                    var p = b.Party[i];
                    if (!p.Alive) continue;
                    if (!pub.executed)
                    {
                        if (m.hits > 0)
                        {
                            float d = UtilityScorer.EstimateExpectedDamage(pub.enemy, m, p);
                            AddCommittedDamage(p, d);
                        }
                        if (pub.expectedBreak > 0)
                            CommittedBreakByTarget[p] = CommittedBreak(p) + pub.expectedBreak;
                        AddPlannedStatuses(p, pub.plannedStatuses);
                    }
                    if (pub.hasHeavy) HeavyReservedTargets.Add(p);
                    if (pub.appliesStun) StunReservedTargets.Add(p);
                }
                return;
            }

            if (pub.target != null && pub.target.IsPlayer && pub.target.Alive)
            {
                if (!pub.executed)
                {
                    if (pub.expectedDamage > 0f)
                        AddCommittedDamage(pub.target, pub.expectedDamage);
                    if (pub.expectedBreak > 0)
                        CommittedBreakByTarget[pub.target] = CommittedBreak(pub.target) + pub.expectedBreak;
                    AddPlannedStatuses(pub.target, pub.plannedStatuses);
                }
                if (pub.enemy.enemy != null && pub.enemy.enemy.aiTier.IsNormalTier() && m.hits > 0 && !m.self && !m.SupportsAlly && !m.Summons)
                    NormalSingleTargetCountByHero[pub.target] = NormalSingleTargetCount(pub.target) + 1;
                if (pub.hasHeavy) HeavyReservedTargets.Add(pub.target);
                if (pub.appliesStun) StunReservedTargets.Add(pub.target);
            }
        }

        void AddCommittedDamage(Combatant hero, float dmg)
        {
            float total = CommittedDamage(hero) + dmg;
            CommittedDamageByTarget[hero] = total;
            if (total >= hero.unit.hp)
                LethalReservations.Add(hero);
        }

        void AddPlannedStatuses(Combatant hero, StatusId[] statuses)
        {
            if (hero == null || statuses == null || statuses.Length == 0) return;
            if (!PlannedStatusesByTarget.TryGetValue(hero, out var set))
            {
                set = new HashSet<StatusId>();
                PlannedStatusesByTarget[hero] = set;
            }
            for (int i = 0; i < statuses.Length; i++)
                set.Add(statuses[i]);
        }
    }
}
