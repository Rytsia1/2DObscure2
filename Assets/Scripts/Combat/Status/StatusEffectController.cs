using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Manages active status effects, universal resistance, ticking, and lifecycle on a Combatant (Prompt Section 4 & 5).
    [Serializable]
    public class StatusEffectController
    {
        [SerializeField] private List<StatusEffectInstance> activeInstances = new List<StatusEffectInstance>();
        public readonly StatusResistanceProfile ResistanceProfile = new StatusResistanceProfile();

        public bool isBoss;

        // Lifecycle events (Prompt Section 11)
        public event Action<StatusEffectInstance> OnStatusApplied;
        public event Action<StatusEffectInstance, int> OnStatusTicked;
        public event Action<StatusEffectInstance> OnStatusExpired;
        public event Action<StatusEffectInstance> OnStatusRemoved;

        public IReadOnlyList<StatusEffectInstance> ActiveInstances => activeInstances;

        public bool HasStatus(StatusId id) => activeInstances.Any(i => i.statusId == id && !i.IsExpired);

        public int CountStatus(StatusId id) => activeInstances.Count(i => i.statusId == id && !i.IsExpired);

        public StatusEffectInstance GetFirst(StatusId id) => activeInstances.FirstOrDefault(i => i.statusId == id && !i.IsExpired);

        public IEnumerable<StatusEffectInstance> GetAll(StatusId id) => activeInstances.Where(i => i.statusId == id && !i.IsExpired);

        // Attempts to apply a status effect per GDD Section 8, 9, 11, 12, 13.
        public bool TryApplyStatus(
            StatusApply apply,
            Combatant source,
            Combatant target,
            out StatusEffectInstance resultInstance,
            System.Random rng = null,
            int skillLevel = 1)
        {
            resultInstance = null;
            if (apply == null || target == null || !target.Alive)
                return false;

            // 1. Check Immunity (GDD Section 13: Bosses are 100% immune to all Mental Effects)
            if (ResistanceProfile.IsImmune(apply.id, isBoss))
                return false;

            // 2. Roll Universal Status Effect Resistance (GDD Section 9)
            // A perfect QTE (chance >= 1f with high power) may guarantee unless immune or resisted
            float rollProvider() => rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;
            if (!ResistanceProfile.RollApplication(apply.chance, apply.id, isBoss, rollProvider))
                return false;

            var def = StatusEffectDefs.Get(apply.id);
            int duration = apply.turns > 0 ? apply.turns : def.defaultDuration;
            float mag = apply.magnitude != 0f ? apply.magnitude : def.defaultMagnitude;

            // 3. Stacking Rules (GDD Section 8 & Section 11/12)
            if (def.stackingRule == StatusStackingRule.NoStack && HasStatus(apply.id))
            {
                return false;
            }

            if (def.stackingRule == StatusStackingRule.RefreshDuration)
            {
                var existing = GetFirst(apply.id);
                if (existing != null)
                {
                    existing.remainingTurns = Math.Max(existing.remainingTurns, duration);
                    existing.magnitude = Math.Max(existing.magnitude, mag);
                    existing.source = source;
                    resultInstance = existing;
                    OnStatusApplied?.Invoke(existing);
                    return true;
                }
            }

            // GDD Section 8 Default: Independent instances
            var newInst = StatusEffectInstance.Create(apply, source, target, skillLevel);
            activeInstances.Add(newInst);

            if (apply.id == StatusId.ResistanceDown)
            {
                ResistanceProfile.AddReduction(newInst.magnitude);
            }

            resultInstance = newInst;
            OnStatusApplied?.Invoke(newInst);
            return true;
        }

        // Ticks DoTs dynamically, decrements durations, and cleans up expired instances at turn start (GDD Section 10).
        public void ProcessTurnStart(Battle battle, Combatant owner, Action<string> log)
        {
            if (owner == null || !owner.Alive) return;

            // 1. Process DoTs dynamically at start of turn (GDD Section 10 & 11)
            var dots = activeInstances.Where(i => i.IsDoT && !i.IsExpired).ToList();
            bool damageDealt = false;

            for (int i = 0; i < dots.Count; i++)
            {
                if (!owner.Alive) break;
                var dot = dots[i];
                int dmg = Math.Max(1, dot.CalculateDynamicDoTDamage());
                owner.unit.hp = Math.Max(0, owner.unit.hp - dmg);
                damageDealt = true;

                log?.Invoke($"{owner.name} suffers {dmg} {dot.Definition.name} damage.");
                OnStatusTicked?.Invoke(dot, dmg);

                if (!owner.Alive)
                {
                    log?.Invoke(owner.IsEnemy ? $"{owner.name} is destroyed." : $"{owner.name} falls!");
                    break;
                }
            }

            // GDD Section 12: Sleep ends when target receives direct damage or DoT
            if (damageDealt && owner.Alive && HasStatus(StatusId.Sleep))
            {
                RemoveStatus(StatusId.Sleep);
                log?.Invoke($"{owner.name} wakes up from taking damage!");
            }

            // 2. Decrement remaining durations
            for (int i = activeInstances.Count - 1; i >= 0; i--)
            {
                var inst = activeInstances[i];
                inst.remainingTurns--;
                if (inst.IsExpired)
                {
                    activeInstances.RemoveAt(i);
                    if (inst.statusId == StatusId.ResistanceDown)
                    {
                        ResistanceProfile.RemoveReduction(inst.magnitude);
                    }
                    log?.Invoke($"{owner.name}'s {inst.Definition.name} wears off.");
                    OnStatusExpired?.Invoke(inst);
                }
            }
        }

        // Direct damage received callback (wakes from Sleep per GDD Section 12).
        public void OnDamageReceived(int damage, bool isDoT, Action<string> log = null)
        {
            if (damage > 0 && HasStatus(StatusId.Sleep))
            {
                RemoveStatus(StatusId.Sleep);
                log?.Invoke("The sleeping target is awakened by taking damage!");
            }
        }

        // Validates whether an action is prohibited by active control/mental effects (GDD Section 11 & 12).
        public bool CanPerformAction(BattleAction action, out string failureReason)
        {
            failureReason = null;
            if (HasStatus(StatusId.Stun))
            {
                failureReason = "Stunned: cannot take any action.";
                return false;
            }

            if (HasStatus(StatusId.Sleep))
            {
                failureReason = "Asleep: cannot take any action.";
                return false;
            }

            if (HasStatus(StatusId.Silence) && action.kind == ActionKind.Skill)
            {
                failureReason = "Silenced: cannot use skills.";
                return false;
            }

            if (HasStatus(StatusId.Charm))
            {
                var charm = GetFirst(StatusId.Charm);
                if (charm != null && charm.forbiddenTarget != null && action.target == charm.forbiddenTarget)
                {
                    failureReason = "Charmed: cannot attack the charmer.";
                    return false;
                }
            }

            return true;
        }

        public bool TryApplyStatus(
            StatusApply apply,
            Combatant source,
            Combatant target,
            System.Random rng = null,
            int skillLevel = 1) =>
            TryApplyStatus(apply, source, target, out _, rng, skillLevel);

        // Evaluates action disruption failure chance for Shock and Despair (GDD Section 11 & 12).
        public bool CheckActionDisruption(BattleAction action, System.Random rng, out string reason)
        {
            reason = null;

            // Shock (GDD Section 11): target has a chance to fail performing an action
            var shock = GetFirst(StatusId.Shock);
            if (shock != null)
            {
                float chance = shock.magnitude > 0f ? shock.magnitude : 0.30f;
                float roll = rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;
                if (roll < chance)
                {
                    reason = "Action failed due to Shock paralysis!";
                    return true; // Disrupted!
                }
            }

            // Despair (GDD Section 12): action disruption chance
            var despair = GetFirst(StatusId.Despair);
            if (despair != null)
            {
                float chance = despair.magnitude > 0f ? despair.magnitude : 0.35f;
                float roll = rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;
                if (roll < chance)
                {
                    reason = "Action failed due to overwhelming Despair!";
                    return true; // Disrupted!
                }
            }

            return false;
        }

        // Evaluates Confusion and Madness redirection (GDD Section 12).
        public bool TryRedirectAction(
            BattleAction original,
            Battle battle,
            System.Random rng,
            out BattleAction redirected)
        {
            redirected = original;

            // Madness (GDD Section 12.1): AI/Random action and random valid target
            if (HasStatus(StatusId.Madness))
            {
                var allCombatants = battle.All.Where(c => c.Alive).ToList();
                if (allCombatants.Count > 0)
                {
                    var randomTarget = allCombatants[rng != null ? rng.Next(allCombatants.Count) : UnityEngine.Random.Range(0, allCombatants.Count)];
                    redirected = new BattleAction
                    {
                        actor = original.actor,
                        kind = ActionKind.Attack,
                        target = randomTarget
                    };
                    return true;
                }
            }

            // Confusion (GDD Section 12): chance to hit the wrong target
            var conf = GetFirst(StatusId.Confusion);
            if (conf != null)
            {
                float chance = conf.magnitude > 0f ? conf.magnitude : 0.50f;
                float roll = rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value;
                if (roll < chance && original.target != null)
                {
                    var allTargets = battle.All.Where(c => c.Alive && c != original.target).ToList();
                    if (allTargets.Count > 0)
                    {
                        var wrongTarget = allTargets[rng != null ? rng.Next(allTargets.Count) : UnityEngine.Random.Range(0, allTargets.Count)];
                        redirected = new BattleAction
                        {
                            actor = original.actor,
                            kind = original.kind,
                            skill = original.skill,
                            item = original.item,
                            attack = original.attack,
                            target = wrongTarget
                        };
                        return true;
                    }
                }
            }

            return false;
        }

        // Multipliers for combat calculations
        public float GetSpeedMultiplier()
        {
            float mult = 1f;
            foreach (var inst in activeInstances)
            {
                if (inst.IsExpired) continue;
                if (inst.statusId == StatusId.Slow)
                    mult *= Mathf.Max(0.1f, 1f - inst.magnitude);
                else if (inst.statusId == StatusId.Freeze)
                    mult *= Mathf.Max(0.1f, 1f - (inst.magnitude > 0f ? inst.magnitude : 0.35f));
            }
            return mult;
        }

        public float GetDamageDealtMultiplier()
        {
            float mult = 1f;
            foreach (var inst in activeInstances)
            {
                if (inst.IsExpired) continue;
                if (inst.statusId == StatusId.Fear)
                    mult *= Mathf.Max(0.1f, 1f - (inst.magnitude > 0f ? inst.magnitude : 0.30f));
            }
            return mult;
        }

        // Cleansing (Prompt Section 10)
        public int RemoveStatus(StatusId id)
        {
            int removed = activeInstances.RemoveAll(i => i.statusId == id);
            if (id == StatusId.ResistanceDown)
                ResistanceProfile.ClearReductions();
            return removed;
        }

        public int CleanseDebuffs(int maxCount = -1)
        {
            var debuffs = activeInstances.Where(i => StatusInfo.IsDebuff(i.statusId)).ToList();
            int toRemove = maxCount > 0 ? Math.Min(maxCount, debuffs.Count) : debuffs.Count;
            for (int i = 0; i < toRemove; i++)
            {
                var d = debuffs[i];
                activeInstances.Remove(d);
                if (d.statusId == StatusId.ResistanceDown)
                    ResistanceProfile.RemoveReduction(d.magnitude);
                OnStatusRemoved?.Invoke(d);
            }
            return toRemove;
        }

        public int CleanseMentalEffects(int maxCount = -1)
        {
            var mentals = activeInstances.Where(i => StatusInfo.IsMental(i.statusId)).ToList();
            int toRemove = maxCount > 0 ? Math.Min(maxCount, mentals.Count) : mentals.Count;
            for (int i = 0; i < toRemove; i++)
            {
                var m = mentals[i];
                activeInstances.Remove(m);
                OnStatusRemoved?.Invoke(m);
            }
            return toRemove;
        }

        public void ClearAll()
        {
            activeInstances.Clear();
            ResistanceProfile.ClearReductions();
        }
    }
}
