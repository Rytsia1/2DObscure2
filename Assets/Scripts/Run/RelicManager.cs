using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public class RelicManager
    {
        // Confirmed Rule 1: Maximum 5 Relics shared across the party
        public const int MaxRelicCapacity = 5;

        private readonly List<RelicInstance> activeRelics = new List<RelicInstance>();

        public int Count => activeRelics.Count;
        public bool IsFull => activeRelics.Count >= MaxRelicCapacity;
        public bool CanAddRelic => !IsFull;
        public IReadOnlyList<RelicInstance> ActiveRelics => activeRelics;

        public bool HasRelic(string relicId) =>
            !string.IsNullOrEmpty(relicId) && activeRelics.Any(r => r.relicId == relicId);

        public RelicInstance GetInstance(string relicId) =>
            activeRelics.FirstOrDefault(r => r.relicId == relicId);

        // Confirmed Rules 1 & 3: Max 5 capacity and no duplicate Relic IDs
        public bool TryAddRelic(string relicId, out string error)
        {
            if (string.IsNullOrEmpty(relicId))
            {
                error = "Relic ID is null or empty.";
                return false;
            }

            if (!Database.Relics.TryGetValue(relicId, out var def))
            {
                error = $"Relic '{relicId}' not found in database.";
                return false;
            }

            if (HasRelic(relicId))
            {
                error = $"Party already owns {def.name} ({relicId}). Duplicate relics are not permitted.";
                return false;
            }

            if (IsFull)
            {
                error = $"Relic capacity is full ({MaxRelicCapacity}/{MaxRelicCapacity}). Cannot acquire more relics.";
                return false;
            }

            var instance = new RelicInstance(def);
            activeRelics.Add(instance);
            error = null;
            return true;
        }

        public bool RemoveRelic(string relicId)
        {
            int idx = activeRelics.FindIndex(r => r.relicId == relicId);
            if (idx >= 0)
            {
                activeRelics.RemoveAt(idx);
                return true;
            }
            return false;
        }

        // Confirmed Rule 4: Dungeon Persistence - all relics removed on dungeon clear
        public void ClearDungeonRelics()
        {
            activeRelics.Clear();
        }

        // Confirmed Rule 5: Run Failure / Reset - cleared completely
        public void ClearAll()
        {
            activeRelics.Clear();
        }

        // ---- Lifecycle Event Hooks ----

        public void OnBattleStart(Battle battle)
        {
            foreach (var r in activeRelics)
            {
                r.ResetBattleState();
            }
        }

        public void OnTurnStart(Combatant actor)
        {
            foreach (var r in activeRelics)
            {
                r.ResetTurnState();
            }
        }

        public void ModifyDamageDealt(
            Combatant actor,
            Combatant target,
            ref float damageMultiplier,
            ref int bonusBreakPower,
            bool isFirstPartyAttack,
            Battle battle = null)
        {
            if (actor == null || !actor.IsPlayer) return;

            // R001 — Ashen Signet: HP > 80% -> Damage +8%
            if (HasRelic("R001") && target != null && target.Stats.maxHp > 0)
            {
                float targetHpPct = (float)target.unit.hp / target.Stats.maxHp;
                if (targetHpPct > 0.80f)
                {
                    damageMultiplier *= 1.08f;
                    battle?.Say("[Relic: Ashen Signet] +8% Damage against healthy foe!");
                }
            }

            // R006 — Crown of the Unremembered: Target has status -> Damage +7%
            if (HasRelic("R006") && target != null && ((target.StatusController != null && target.StatusController.ActiveInstances.Count > 0) || (target.statuses != null && target.statuses.Count > 0)))
            {
                damageMultiplier *= 1.07f;
                battle?.Say("[Relic: Crown of the Unremembered] +7% Damage against afflicted foe!");
            }

            // R010 — Executioner's Bell: First party attack in battle -> Break Power +15%
            if (isFirstPartyAttack && HasRelic("R010"))
            {
                var r010 = GetInstance("R010");
                if (r010 != null && !r010.battleTriggered)
                {
                    r010.battleTriggered = true;
                    bonusBreakPower += Math.Max(1, (int)Math.Round(bonusBreakPower * 0.15f));
                    battle?.Say("[Relic: Executioner's Bell] First strike of battle gains +15% Break Power!");
                }
            }

            // R012 — Last King's Seal: Target is Boss -> Damage +5%
            if (HasRelic("R012") && target != null && target.enemy != null && target.enemy.tier == Tier.Boss)
            {
                damageMultiplier *= 1.05f;
                battle?.Say("[Relic: Last King's Seal] +5% Damage against Boss!");
            }

            // R017 — Oathstone of the Fallen: Not damaged since last turn -> Damage +6%
            if (HasRelic("R017") && !actor.tookDamageSinceLastTurn)
            {
                damageMultiplier *= 1.06f;
                battle?.Say("[Relic: Oathstone of the Fallen] Undamaged since last turn: +6% Damage!");
            }

            // R021 — Coffin Nail: Target is Broken -> Damage +10%
            if (HasRelic("R021") && target != null && target.IsBroken)
            {
                damageMultiplier *= 1.10f;
                battle?.Say("[Relic: Coffin Nail] +10% Damage against Broken foe!");
            }

            // R022 — Zoroa's Dull Crown: Active buff after 5 kills -> Damage +4%
            if (HasRelic("R022"))
            {
                var r022 = GetInstance("R022");
                if (r022 != null && r022.isBattleBuffActive)
                {
                    damageMultiplier *= 1.04f;
                }
            }
        }

        public void ModifyDamageReceived(
            Combatant target,
            ref float damageMultiplier,
            bool isDirectDamage,
            Battle battle = null)
        {
            if (target == null || !target.IsPlayer) return;

            // R003 — Rustbound Medal: HP < 30% -> Direct damage taken -10%
            if (HasRelic("R003") && isDirectDamage && target.Stats.maxHp > 0)
            {
                float hpPct = (float)target.unit.hp / target.Stats.maxHp;
                if (hpPct < 0.30f)
                {
                    damageMultiplier *= 0.90f;
                    battle?.Say($"[Relic: Rustbound Medal] -10% damage taken by {target.name} under 30% HP!");
                }
            }

            // R011 — Hollow Knight's Buckle: >= 2 party members Down -> All damage taken -8%
            if (HasRelic("R011") && battle != null)
            {
                int downCount = battle.Allies(target).Count(c => !c.Alive);
                if (downCount >= 2)
                {
                    damageMultiplier *= 0.92f;
                    battle?.Say($"[Relic: Hollow Knight's Buckle] -8% damage taken (2+ allies down)!");
                }
            }
        }

        public bool CheckLethalDamageProtection(Combatant target, int incomingDamage, Battle battle = null)
        {
            if (target == null || !target.IsPlayer || !target.Alive) return false;

            // R009 — Soul-Tether Ring: Once per battle survive lethal hit at 1 HP
            if (HasRelic("R009") && target.unit.hp - incomingDamage <= 0)
            {
                var r009 = GetInstance("R009");
                if (r009 != null && !r009.battleTriggered)
                {
                    r009.battleTriggered = true;
                    target.unit.hp = 1;
                    battle?.Say($"[Relic: Soul-Tether Ring] Lethal blow prevented! {target.name} survives at 1 HP (once per battle).");
                    return true;
                }
            }

            return false;
        }

        public void OnQteResolved(Combatant actor, bool isOffensive, QteResult grade, Battle battle = null)
        {
            if (actor == null || !actor.IsPlayer) return;

            // R002 — Soulglass Shard: Every 3 Perfect Offensive QTEs -> +2 MP to active character
            if (isOffensive && grade == QteResult.Perfect && HasRelic("R002"))
            {
                var r002 = GetInstance("R002");
                if (r002 != null)
                {
                    r002.counter++;
                    if (r002.counter % 3 == 0)
                    {
                        int before = actor.unit.mp;
                        actor.unit.mp = Math.Min(actor.Stats.maxMp, actor.unit.mp + 2);
                        int gained = actor.unit.mp - before;
                        battle?.Say($"[Relic: Soulglass Shard] 3rd Perfect Offensive QTE! {actor.name} restores {gained} MP.");
                    }
                }
            }

            // R019 — Black-Iron Rosary: Perfect Defensive QTE -> Shield = 2% Max HP (max once/turn)
            if (!isOffensive && grade == QteResult.Perfect && HasRelic("R019"))
            {
                var r019 = GetInstance("R019");
                if (r019 != null && !r019.turnTriggered)
                {
                    r019.turnTriggered = true;
                    int shieldAmt = Math.Max(1, (int)Math.Round(actor.Stats.maxHp * 0.02f));
                    actor.AddShield(shieldAmt);
                    battle?.Say($"[Relic: Black-Iron Rosary] Perfect Defense! {actor.name} gains +{shieldAmt} Barrier Shield (max 1/turn).");
                }
            }
        }

        public void OnEnemyDefeated(Combatant killer, Combatant enemy, Battle battle = null)
        {
            if (enemy == null || !enemy.IsEnemy) return;

            // R013 — Dirge-Eater's Fang: Killer restores 1 MP (max 3 per battle)
            if (killer != null && killer.IsPlayer && killer.Alive && HasRelic("R013"))
            {
                var r013 = GetInstance("R013");
                if (r013 != null && r013.battleUsageCount < 3)
                {
                    r013.battleUsageCount++;
                    int before = killer.unit.mp;
                    killer.unit.mp = Math.Min(killer.Stats.maxMp, killer.unit.mp + 1);
                    int gained = killer.unit.mp - before;
                    battle?.Say($"[Relic: Dirge-Eater's Fang] Killing blow! {killer.name} restores {gained} MP ({r013.battleUsageCount}/3 this battle).");
                }
            }

            // R022 — Zoroa's Dull Crown: Defeating 5 enemies in battle -> Party gets +4% damage until battle ends
            if (HasRelic("R022"))
            {
                var r022 = GetInstance("R022");
                if (r022 != null)
                {
                    r022.counter++;
                    if (r022.counter >= 5 && !r022.isBattleBuffActive)
                    {
                        r022.isBattleBuffActive = true;
                        battle?.Say("[Relic: Zoroa's Dull Crown] 5 enemies defeated! Party damage +4% for remainder of battle!");
                    }
                }
            }
        }

        public void OnBossHpChanged(Combatant boss, Battle battle = null)
        {
            if (boss == null || boss.enemy == null || boss.enemy.tier != Tier.Boss) return;

            // R020 — Sovereign's Last Breath: Boss <= 25% HP -> Party restores 3 MP (once per battle)
            if (HasRelic("R020") && boss.Stats.maxHp > 0)
            {
                float hpPct = (float)boss.unit.hp / boss.Stats.maxHp;
                if (hpPct <= 0.25f)
                {
                    var r020 = GetInstance("R020");
                    if (r020 != null && !r020.battleTriggered)
                    {
                        r020.battleTriggered = true;
                        if (battle != null)
                        {
                            foreach (var member in battle.Party.Where(c => c.Alive))
                            {
                                int before = member.unit.mp;
                                member.unit.mp = Math.Min(member.Stats.maxMp, member.unit.mp + 3);
                            }
                            battle.Say("[Relic: Sovereign's Last Breath] Boss HP fallen below 25%! Entire party restores 3 MP!");
                        }
                    }
                }
            }
        }

        public void OnBattleWon(RunState run)
        {
            if (run == null || run.Party == null) return;

            // R004 — Gravekeeper's Thread: Lowest HP member restores 3% Max HP
            if (HasRelic("R004"))
            {
                var lowest = run.Party
                    .Where(u => u.Alive && u.stats.maxHp > 0)
                    .OrderBy(u => (float)u.hp / u.stats.maxHp)
                    .FirstOrDefault();

                if (lowest != null)
                {
                    int healAmt = Math.Max(1, (int)Math.Round(lowest.stats.maxHp * 0.03f));
                    lowest.hp = Math.Min(lowest.stats.maxHp, lowest.hp + healAmt);
                }
            }
        }

        public void ModifyGoldReward(ref int gold)
        {
            if (gold <= 0) return;

            // R007 — Mourner's Coin: Run Gold +10% (floored)
            if (HasRelic("R007"))
            {
                int bonus = (int)(gold * 0.10f);
                gold += bonus;
            }
        }

        public void ModifyConsumableHeal(ref float healMultiplier)
        {
            // R016 — Royal Gravewax: HP recovery items restore +5% additional base value
            if (HasRelic("R016"))
            {
                healMultiplier *= 1.05f;
            }
        }

        public float GetStatusResistanceBonus(StatusId statusId)
        {
            // R015 — Veil of Dust: Burn, Poison, and Bleed resistance +10%
            if (HasRelic("R015"))
            {
                if (statusId == StatusId.Burn || statusId == StatusId.Poison || statusId == StatusId.Bleed)
                {
                    return 0.10f;
                }
            }
            return 0f;
        }

        public bool TryNegateMinorChestTrap(out string message)
        {
            // R014 — Pallbearer's Gloves: Negate first minor chest trap per dungeon
            if (HasRelic("R014"))
            {
                var r014 = GetInstance("R014");
                if (r014 != null && !r014.dungeonTriggered)
                {
                    r014.dungeonTriggered = true;
                    message = "[Relic: Pallbearer's Gloves] Minor chest trap safely disarmed!";
                    return true;
                }
            }

            message = null;
            return false;
        }

        public string GetAdditionalEventClue(string eventTag)
        {
            if (string.IsNullOrEmpty(eventTag)) return null;

            // R005 — Warden's Broken Key: clue for hidden or trap events
            if (HasRelic("R005") && (eventTag == "hidden" || eventTag == "trap"))
            {
                return "[Warden's Broken Key] A hidden latch reveals safe passage.";
            }

            // R018 — Whispering Reliquary: risk warning for relic or cursed_reward events
            if (HasRelic("R018") && (eventTag == "relic" || eventTag == "cursed_reward"))
            {
                return "[Whispering Reliquary] A dark risk hums from this choice.";
            }

            return null;
        }

        public bool HasPaleLantern() => HasRelic("R008");

        // ---- Save / Load Serialization ----

        public RelicInstanceSaveData[] ToSaveData() =>
            activeRelics.Select(r => r.ToSaveData()).ToArray();

        public void RestoreFromSave(RelicInstanceSaveData[] data)
        {
            activeRelics.Clear();
            if (data == null) return;

            foreach (var d in data)
            {
                if (activeRelics.Count >= MaxRelicCapacity) break; // Enforce 5 cap
                var inst = RelicInstance.FromSaveData(d);
                if (inst != null && !HasRelic(inst.relicId)) // Enforce unique restriction
                {
                    activeRelics.Add(inst);
                }
            }
        }
    }
}

