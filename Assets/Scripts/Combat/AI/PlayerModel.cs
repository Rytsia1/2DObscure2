using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Obscure
{
    // Seven behavioral categories tracked per hero (Architecture §4.1).
    public enum BehaviorCategory
    {
        Aggressive,
        Defensive,
        BurstDamage,
        ElementalSpecialist,
        SupportDependent,
        FocusFire,
        BreakSpecialist,
    }

    public readonly struct BehaviorObservation
    {
        public readonly int round;
        public readonly BehaviorCategory category;
        public readonly float weight;
        public readonly bool isContradiction;

        public BehaviorObservation(int round, BehaviorCategory category, float weight, bool isContradiction)
        {
            this.round = round;
            this.category = category;
            this.weight = weight;
            this.isContradiction = isContradiction;
        }
    }

    // Per-hero encounter-local behavioral model (Architecture §4.1 v1.1, §14.2).
    // Only fed from observable combat events (Spec §5.6); never reads hidden skills, inventory counts, or unseen affinities.
    public sealed class HeroBehaviorProfile
    {
        // posture-B starting value (untested).
        // Pass test: 2 rounds of attacking after guarding drops Defensive below 0.5. If it fails: 1.5f.
        public const float ContradictionWeight = 1.0f;

        // posture-B starting value (untested).
        // Pass test: Burst heroes raise pressure weight in prediction so supports protect low allies earlier.
        public const float BurstPressureBoost = 1.35f;

        public readonly Combatant hero;
        public readonly List<BehaviorObservation> observations = new List<BehaviorObservation>();

        // Observed output & targeting (used by Focus Fire adaptation and Stage 4 Predictor pressure scenario).
        public int offensiveTurns;
        public int totalDamageDealt;
        public Combatant lastAttackedEnemy;
        public readonly Dictionary<Combatant, int> attackCountsByEnemy = new Dictionary<Combatant, int>();
        public readonly HashSet<Combatant> exploitedWeaknessEnemies = new HashSet<Combatant>();
        public readonly Dictionary<Element, int> elementUseCounts = new Dictionary<Element, int>();

        // Observable context-sensitive action tracking (healing, spellcasting, counter stances, same-round activity).
        public int healActionsUsed;
        public int lastHealRound;
        public Combatant lastHealedAlly;
        public bool healedThisRound;
        public int spellCastCount;
        public int lastSpellCastRound;
        public bool castSpellThisRound;
        public int counterStanceCount;
        public int lastCounterRound;
        public int lastGuardedRound;
        public int sameRoundDamageDealt;

        public void BeginRoundReset()
        {
            healedThisRound = false;
            castSpellThisRound = false;
            sameRoundDamageDealt = 0;
        }

        public HeroBehaviorProfile(Combatant hero)
        {
            this.hero = hero;
        }

        public float AverageDamagePerTurn => offensiveTurns > 0 ? (float)totalDamageDealt / offensiveTurns : 0f;

        public Combatant MostFrequentTarget
        {
            get
            {
                Combatant best = null;
                int bestCount = 0;
                foreach (var kv in attackCountsByEnemy)
                {
                    if (kv.Key == null || !kv.Key.Alive) continue;
                    if (kv.Value > bestCount || (kv.Value == bestCount && best != null && kv.Key.slot < best.slot))
                    {
                        best = kv.Key;
                        bestCount = kv.Value;
                    }
                }
                return best;
            }
        }

        public void AddObservation(int round, BehaviorCategory category, float weight = 1f)
        {
            observations.Add(new BehaviorObservation(round, category, weight, isContradiction: false));
            if (TryContradictingCategory(category, out var opposite))
                observations.Add(new BehaviorObservation(round, opposite, ContradictionWeight, isContradiction: true));
        }

        public void AddContradiction(int round, BehaviorCategory category, float weight = ContradictionWeight)
        {
            observations.Add(new BehaviorObservation(round, category, weight, isContradiction: true));
        }

        // Contradicting pairs (Architecture §4.1 v1.1): Aggressive <-> Defensive, SupportDependent <-> BurstDamage.
        public static bool TryContradictingCategory(BehaviorCategory cat, out BehaviorCategory opposite)
        {
            switch (cat)
            {
                case BehaviorCategory.Aggressive: opposite = BehaviorCategory.Defensive; return true;
                case BehaviorCategory.Defensive: opposite = BehaviorCategory.Aggressive; return true;
                case BehaviorCategory.SupportDependent: opposite = BehaviorCategory.BurstDamage; return true;
                case BehaviorCategory.BurstDamage: opposite = BehaviorCategory.SupportDependent; return true;
                default: opposite = default; return false;
            }
        }

        // Concrete confidence model (Architecture §4.1 v1.1):
        // s = Sum(w * decay^age) - Sum(contradiction * decay^age)
        // confidence = clamp(s / (s + k), 0, 1)
        public float GetConfidence(BehaviorCategory category, int currentRound, float decay, float k = AiProfile.ConfidenceK)
        {
            float s = GetWeightedScore(category, currentRound, decay);
            if (s <= 0f) return 0f;
            return Math.Clamp(s / (s + k), 0f, 1f);
        }

        public float GetWeightedScore(BehaviorCategory category, int currentRound, float decay)
        {
            float s = 0f;
            for (int i = 0; i < observations.Count; i++)
            {
                var obs = observations[i];
                if (obs.category != category) continue;
                int age = Math.Max(0, currentRound - obs.round);
                float decayed = obs.weight * (float)Math.Pow(decay, age);
                s += obs.isContradiction ? -decayed : decayed;
            }
            return s;
        }

        public bool IsRecognized(BehaviorCategory category, int currentRound, AiProfile profile) =>
            GetConfidence(category, currentRound, profile.memoryDecay) >= profile.recognizeAt;
    }

    // Single shared encounter tactical memory owned by EncounterBlackboard (Spec §5.3 v1.1, Arch §4.2 v1.1, §14.3).
    // Shared by every enemy in the encounter (including summons), persists across boss phases, destroyed when Battle ends.
    public sealed class EncounterTacticalMemory
    {
        // posture-B starting value (untested).
        // Pass test: Repeated Action Rate is below Stage 0 baseline. If it fails: -5f.
        public const float RepetitionPenaltyPerStreak = 3f;

        // posture-B starting value (untested).
        // Pass test: Telegraph Response Rate (interrupted / started) 20-50%. If it fails: +/-0.10f.
        public const float ChargeShieldGate = 0.70f;

        // posture-B starting values (untested) for adaptation utility modifiers (all inside UtilityClamp = +/-15).
        // Pass test: Adaptation visibly shifts decisions without causing immunity or spiking deaths > baseline + 15%.
        // If it fails: reduce inside clamp before touching stats.
        public const float FocusFireAdaptationBonus = 4f;
        public const float DefensiveGuardPenalty = 4f;
        public const float DefensiveStatusBonus = 4f;
        public const float SupportDependentBonus = 3f;
        public const float ElementalSpecialistTargetBonus = 4f;
        public const float ElementalSpecialistHealBonus = 4f;
        // posture-B starting value (untested): sized so starting a charge when shield < ChargeShieldGate drops 1+ priority level (inside UtilityClamp = +/-15).
        // Pass test: Knight holds Graveward Charge when Break gauge < 70% against a recognized Break Specialist, and charges when >= 70%.
        // If it fails: adjust within UtilityClamp or ChargeShieldGate (+/-0.10f).
        public const float BreakSpecialistChargePenalty = 18f;
        public const float BurstTargetBonus = 3f;

        // Context-sensitive behavioral punishment constants (posture-B starting values, untested; all bounded inside UtilityClamp = +/-15).
        // Pass test: repeated healing/casting/guarding/countering draws readable counter-selection without hard immunity or spiking Normal deaths > baseline + 15%.
        public const float AntiHealStatusBonus = 4.5f;
        public const float AntiCasterStatusBonus = 4.5f;
        public const float SameRoundHealerPressureBonus = 2.5f;
        public const float CounterDisruptBonus = 3.0f;
        public const float SameRoundGuardRiskPenalty = 3.0f;
        public const float ActiveCounterRiskPenalty = 3.5f;

        public readonly Dictionary<Combatant, HeroBehaviorProfile> Profiles = new Dictionary<Combatant, HeroBehaviorProfile>();

        // Observed effectiveness only (Spec §5.6: enemies never peek at unseen affinities).
        readonly Dictionary<(Combatant target, Element element), Effectiveness> seenEffectiveness = new Dictionary<(Combatant, Element), Effectiveness>();

        // Consecutive ineffective uses of (enemy, moveName, targetSlot) -> streak count.
        readonly Dictionary<(Combatant enemy, string moveName, int targetSlot), int> ineffectiveStreaks = new Dictionary<(Combatant, string, int), int>();

        // Reads already logged in this encounter so each adaptation read fires at most once (Spec §5.7, Arch §11.5).
        public readonly HashSet<string> TriggeredReads = new HashSet<string>();

        public int ChargeInterruptCount { get; private set; }
        public double LastMemoryUpdateMs { get; private set; }

        public bool IsEmpty => Profiles.Count == 0 && seenEffectiveness.Count == 0 && ineffectiveStreaks.Count == 0 && TriggeredReads.Count == 0 && ChargeInterruptCount == 0;

        public HeroBehaviorProfile GetOrCreateProfile(Combatant hero)
        {
            if (hero == null || !hero.IsPlayer) return null;
            if (!Profiles.TryGetValue(hero, out var prof))
            {
                prof = new HeroBehaviorProfile(hero);
                Profiles[hero] = prof;
            }
            return prof;
        }

        public HeroBehaviorProfile GetProfile(Combatant hero) =>
            hero != null && Profiles.TryGetValue(hero, out var prof) ? prof : null;

        public Effectiveness? GetSeenEffectiveness(Combatant target, Element element) =>
            target != null && seenEffectiveness.TryGetValue((target, element), out var eff) ? eff : (Effectiveness?)null;

        public int GetIneffectiveStreak(Combatant enemy, string moveName, int targetSlot) =>
            enemy != null && !string.IsNullOrEmpty(moveName) && ineffectiveStreaks.TryGetValue((enemy, moveName, targetSlot), out int c) ? c : 0;

        // Observable-only threat score for TargetRule.HighestThreat (Variant Spec §8 v1.1):
        // derived strictly from observed impact in shared encounter memory; before any player turn, falls back to HUD offensive stats.
        public float GetObservedThreatScore(Combatant hero, int currentRound, float decay = AiProfile.StandardDecayPerRound)
        {
            if (hero == null || !hero.Alive) return 0f;
            bool anyHeroObserved = Profiles.Values.Any(p => p.observations.Count > 0 || p.totalDamageDealt > 0);
            if (!anyHeroObserved)
                return Math.Max(hero.Atk, hero.Magic);
            if (!Profiles.TryGetValue(hero, out var prof))
                return 0f;
            float score = prof.totalDamageDealt
                + prof.offensiveTurns * 5f
                + prof.exploitedWeaknessEnemies.Count * 15f
                + prof.GetConfidence(BehaviorCategory.BurstDamage, currentRound, decay) * 25f
                + prof.GetConfidence(BehaviorCategory.ElementalSpecialist, currentRound, decay) * 20f
                + prof.GetConfidence(BehaviorCategory.SupportDependent, currentRound, decay) * 20f
                + prof.GetConfidence(BehaviorCategory.BreakSpecialist, currentRound, decay) * 20f
                + prof.GetConfidence(BehaviorCategory.Aggressive, currentRound, decay) * 15f;
            return score;
        }

        // Logs an adaptation read at most once per encounter (Spec §5.7, Arch §11.5).
        public bool TryLogRead(Battle b, string readKey, string readText)
        {
            if (b == null || string.IsNullOrEmpty(readKey) || string.IsNullOrEmpty(readText)) return false;
            if (!TriggeredReads.Add(readKey)) return false;
            b.Say(readText);
            return true;
        }

        // ---- Observe-only event hooks (called from Battle.cs; zero rule changes) ----

        public void BeginRound(int round)
        {
            foreach (var prof in Profiles.Values)
                prof.BeginRoundReset();
        }

        public void ObserveGuard(Combatant actor, int round)
        {
            if (actor == null || !actor.IsPlayer) return;
            var sw = Stopwatch.StartNew();
            var prof = GetOrCreateProfile(actor);
            prof.lastGuardedRound = round;
            prof.AddObservation(round, BehaviorCategory.Defensive, 1f);
            sw.Stop();
            LastMemoryUpdateMs = sw.Elapsed.TotalMilliseconds;
        }

        public void ObserveBreak(Combatant attacker, Combatant brokenTarget, bool interruptedCharge, int round)
        {
            var sw = Stopwatch.StartNew();
            if (attacker != null && attacker.IsPlayer && brokenTarget != null && brokenTarget.IsEnemy)
            {
                var prof = GetOrCreateProfile(attacker);
                prof.AddObservation(round, BehaviorCategory.BreakSpecialist, 1f);
                if (interruptedCharge)
                    ChargeInterruptCount++;
            }
            else if (interruptedCharge && brokenTarget != null && brokenTarget.IsEnemy)
            {
                ChargeInterruptCount++;
            }
            sw.Stop();
            LastMemoryUpdateMs = sw.Elapsed.TotalMilliseconds;
        }

        public void ObserveAction(Battle b, BattleAction a, List<Hit> hits, bool statusAppliedThisAction, int round)
        {
            if (a?.actor == null) return;
            var sw = Stopwatch.StartNew();

            if (a.actor.IsPlayer)
                ObservePlayerAction(a, hits, round);
            else if (a.actor.IsEnemy)
                ObserveEnemyAction(a, hits, statusAppliedThisAction);

            sw.Stop();
            LastMemoryUpdateMs = sw.Elapsed.TotalMilliseconds;
        }

        void ObservePlayerAction(BattleAction a, List<Hit> hits, int round)
        {
            var actor = a.actor;
            var prof = GetOrCreateProfile(actor);
            if (prof == null) return;

            if (a.kind == ActionKind.Item)
            {
                // Items are observed strictly as actions performed, never reading inventory counts (Spec §5.6).
                if (a.item != null && (a.item.heal > 0 || a.item.healPercent > 0f || a.item.isRevive))
                {
                    prof.healActionsUsed++;
                    prof.lastHealRound = round;
                    prof.healedThisRound = true;
                    prof.lastHealedAlly = a.target;
                }
                prof.AddObservation(round, BehaviorCategory.SupportDependent, 1f);
                return;
            }

            if (a.kind == ActionKind.Skill && a.skill != null)
            {
                var sk = a.skill;
                if (sk.mpCost > 0 || sk.kind == SkillKind.Magic || sk.kind == SkillKind.Heal || sk.kind == SkillKind.Support)
                {
                    prof.spellCastCount++;
                    prof.lastSpellCastRound = round;
                    prof.castSpellThisRound = true;
                }
                if (sk.counterMultiplier > 0f)
                {
                    prof.counterStanceCount++;
                    prof.lastCounterRound = round;
                    prof.AddObservation(round, BehaviorCategory.Defensive, 1f);
                }
                if (sk.kind == SkillKind.Heal || sk.kind == SkillKind.Support)
                {
                    if (sk.kind == SkillKind.Heal)
                    {
                        prof.healActionsUsed++;
                        prof.lastHealRound = round;
                        prof.healedThisRound = true;
                        prof.lastHealedAlly = a.target;
                    }
                    prof.AddObservation(round, BehaviorCategory.SupportDependent, 1f);
                    return;
                }
            }

            // Offensive player action (Attack or Physical/Magic Skill).
            if (a.kind == ActionKind.Attack || a.kind == ActionKind.Skill)
            {
                prof.AddObservation(round, BehaviorCategory.Aggressive, 1f);
                prof.offensiveTurns++;

                int dmg = 0;
                int maxTargetHp = 0;
                bool anyWeak = false;
                bool anyCrit = false;
                Combatant primaryEnemyHit = null;
                Element usedElement = a.kind == ActionKind.Skill && a.skill != null ? a.skill.element : Element.Physical;

                if (hits != null)
                {
                    for (int i = 0; i < hits.Count; i++)
                    {
                        var h = hits[i];
                        if ((h.heal && !h.drained) || h.target == null || !h.target.IsEnemy) continue;
                        if (!h.drained) dmg += Math.Max(0, h.amount);
                        if (h.target.Stats.maxHp > maxTargetHp) maxTargetHp = h.target.Stats.maxHp;
                        if (primaryEnemyHit == null) primaryEnemyHit = h.target;
                        if (h.critical) anyCrit = true;

                        // Record seen effectiveness for elemental hits (only way AI learns affinities).
                        seenEffectiveness[(h.target, usedElement)] = h.effect;
                        if (h.effect == Effectiveness.Weak)
                        {
                            anyWeak = true;
                            prof.exploitedWeaknessEnemies.Add(h.target);
                        }
                    }
                }
                prof.totalDamageDealt += dmg;
                prof.sameRoundDamageDealt += dmg;

                // Focus Fire tracking: targeting the same enemy builds confidence; switching to a different living enemy contradicts.
                Combatant tgtEnemy = (a.target != null && a.target.IsEnemy) ? a.target : primaryEnemyHit;
                if (tgtEnemy != null)
                {
                    prof.attackCountsByEnemy[tgtEnemy] = (prof.attackCountsByEnemy.TryGetValue(tgtEnemy, out int prevCnt) ? prevCnt : 0) + 1;
                    if (prof.lastAttackedEnemy == null || prof.lastAttackedEnemy == tgtEnemy || !prof.lastAttackedEnemy.Alive)
                        prof.AddObservation(round, BehaviorCategory.FocusFire, 1f);
                    else
                        prof.AddContradiction(round, BehaviorCategory.FocusFire, HeroBehaviorProfile.ContradictionWeight);
                    prof.lastAttackedEnemy = tgtEnemy;
                }

                // Elemental Specialist tracking: non-physical / magic skill usage.
                bool isElemental = usedElement != Element.Physical || (a.skill != null && a.skill.kind == SkillKind.Magic);
                if (isElemental)
                {
                    prof.elementUseCounts[usedElement] = (prof.elementUseCounts.TryGetValue(usedElement, out int ec) ? ec : 0) + 1;
                    prof.AddObservation(round, BehaviorCategory.ElementalSpecialist, anyWeak ? 1.25f : 1f);
                }

                // Burst Damage tracking: high single-turn damage or multi-hit / high-power offensive skill / crit.
                bool isBurst = anyCrit
                    || (maxTargetHp > 0 && dmg >= maxTargetHp * 0.25f)
                    || (a.skill != null && (a.skill.power >= 1.35f || a.skill.hits >= 2 || a.skill.isFinisher));
                if (isBurst)
                    prof.AddObservation(round, BehaviorCategory.BurstDamage, 1f);
            }
        }

        void ObserveEnemyAction(BattleAction a, List<Hit> hits, bool statusAppliedThisAction)
        {
            var mv = a.attack;
            if (mv == null || a.charging || mv.self || mv.SupportsAlly || mv.Summons) return;

            int totalDmg = 0;
            int totalBreak = 0;
            bool sawIneffectiveAffinity = false;
            if (hits != null)
            {
                for (int i = 0; i < hits.Count; i++)
                {
                    var h = hits[i];
                    if ((h.heal && !h.drained) || h.target == null || !h.target.IsPlayer) continue;
                    if (!h.drained) totalDmg += Math.Max(0, h.amount);
                    totalBreak += Math.Max(0, h.breakAmount);
                    seenEffectiveness[(h.target, mv.element)] = h.effect;
                    if (h.effect == Effectiveness.Resist || h.effect == Effectiveness.Immune || h.effect == Effectiveness.Drain || h.effect == Effectiveness.Reflect)
                        sawIneffectiveAffinity = true;
                }
            }

            var key = (a.actor, mv.name, a.target?.slot ?? -1);
            bool ineffective = (totalDmg == 0 && totalBreak == 0 && !statusAppliedThisAction) || sawIneffectiveAffinity;
            if (ineffective)
                ineffectiveStreaks[key] = (ineffectiveStreaks.TryGetValue(key, out int prev) ? prev : 0) + 1;
            else
                ineffectiveStreaks.Remove(key);
        }

        public void ReplaceEnemy(Combatant oldEnemy, Combatant newEnemy)
        {
            if (oldEnemy == null || newEnemy == null) return;
            foreach (var prof in Profiles.Values)
            {
                if (prof.lastAttackedEnemy == oldEnemy) prof.lastAttackedEnemy = newEnemy;
                if (prof.attackCountsByEnemy.TryGetValue(oldEnemy, out int cnt))
                {
                    prof.attackCountsByEnemy.Remove(oldEnemy);
                    prof.attackCountsByEnemy[newEnemy] = cnt;
                }
                if (prof.exploitedWeaknessEnemies.Remove(oldEnemy))
                    prof.exploitedWeaknessEnemies.Add(newEnemy);
            }
            foreach (var kv in seenEffectiveness.Where(k => k.Key.target == oldEnemy).ToList())
            {
                seenEffectiveness.Remove(kv.Key);
                seenEffectiveness[(newEnemy, kv.Key.element)] = kv.Value;
            }
            foreach (var kv in ineffectiveStreaks.Where(k => k.Key.enemy == oldEnemy).ToList())
            {
                ineffectiveStreaks.Remove(kv.Key);
                ineffectiveStreaks[(newEnemy, kv.Key.moveName, kv.Key.targetSlot)] = kv.Value;
            }
        }
    }
}
