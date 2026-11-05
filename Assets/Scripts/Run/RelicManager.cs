using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Authoritative Relic Manager supporting the 31 Layer 1 Relics (5 November 2026 GDD Bab 14 & Framework)
    public class RelicManager
    {
        // GDD Bab 14 §2: Maximum 5 Relics shared across the party; permanent player progression.
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

        // GDD Bab 14 §2: Max 5 capacity and no duplicate Relic IDs
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
                error = $"Relic capacity is full ({MaxRelicCapacity}/{MaxRelicCapacity}). Must replace an existing relic or reject.";
                return false;
            }

            var instance = new RelicInstance(def);
            activeRelics.Add(instance);
            error = null;
            return true;
        }

        // GDD Bab 14 §4.2: When at 5 relics, player can replace an existing relic with the new one
        public bool ReplaceRelic(string oldRelicId, string newRelicId, out string error)
        {
            if (string.IsNullOrEmpty(oldRelicId) || string.IsNullOrEmpty(newRelicId))
            {
                error = "Relic ID is invalid.";
                return false;
            }

            if (!HasRelic(oldRelicId))
            {
                error = $"Old relic '{oldRelicId}' is not owned.";
                return false;
            }

            if (HasRelic(newRelicId))
            {
                error = $"Party already owns relic '{newRelicId}'. Duplicates are not permitted.";
                return false;
            }

            if (!Database.Relics.TryGetValue(newRelicId, out var newDef))
            {
                error = $"Relic '{newRelicId}' not found in database.";
                return false;
            }

            RemoveRelic(oldRelicId);
            var newInstance = new RelicInstance(newDef);
            activeRelics.Add(newInstance);
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

        // GDD Bab 14 §2: Relics are permanent player progression and NOT wiped on dungeon clear
        public void ClearDungeonRelics()
        {
            // Retained for compatibility but relics are permanent per Bab 14 §2
        }

        // Resets all relics (e.g. for complete game reset or explicit wipe)
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
            Battle battle = null,
            bool isSkill = false,
            SkillDef skill = null)
        {
            if (actor == null || !actor.IsPlayer) return;

            // R001 — Ashen Signet: Target HP > 80% -> Damage +5%
            if (HasRelic("R001") && target != null && target.Stats.maxHp > 0)
            {
                float targetHpPct = (float)target.unit.hp / target.Stats.maxHp;
                if (targetHpPct > 0.80f)
                {
                    damageMultiplier *= 1.05f;
                    battle?.Say("[Relic: Ashen Signet] +5% Damage against healthy foe!");
                }
            }

            // R006 — Crown of the Unremembered: Target has status -> Damage +6%
            if (HasRelic("R006") && target != null && ((target.StatusController != null && target.StatusController.ActiveInstances.Count > 0) || (target.statuses != null && target.statuses.Count > 0)))
            {
                damageMultiplier *= 1.06f;
                battle?.Say("[Relic: Crown of the Unremembered] +6% Damage against afflicted foe!");
            }

            // R010 — Executioner's Bell: First party strike in battle -> Break Power +10%
            if (isFirstPartyAttack && HasRelic("R010"))
            {
                var r010 = GetInstance("R010");
                if (r010 != null && !r010.battleTriggered)
                {
                    r010.battleTriggered = true;
                    bonusBreakPower += Math.Max(1, (int)Math.Round(bonusBreakPower * 0.10f));
                    battle?.Say("[Relic: Executioner's Bell] First strike of battle gains +10% Break Power!");
                }
            }

            // R012 — Last King's Seal: Target is Boss -> Damage +5%
            if (HasRelic("R012") && target != null && target.enemy != null && target.enemy.tier == Tier.Boss)
            {
                damageMultiplier *= 1.05f;
                battle?.Say("[Relic: Last King's Seal] +5% Damage against Boss!");
            }

            // R017 — Oathstone of the Fallen: Undamaged since start of previous turn -> Damage +5%
            if (HasRelic("R017") && !actor.tookDamageSinceLastTurn)
            {
                damageMultiplier *= 1.05f;
                battle?.Say("[Relic: Oathstone of the Fallen] Undamaged since last turn: +5% Damage!");
            }

            // R021 — Coffin Nail: Target is Broken -> Damage +7%
            if (HasRelic("R021") && target != null && target.IsBroken)
            {
                damageMultiplier *= 1.07f;
                battle?.Say("[Relic: Coffin Nail] +7% Damage against Broken foe!");
            }

            // R022 — Zoroa's Dull Crown: After 3 kills in battle -> Party gets +3% All Damage Bonus
            if (HasRelic("R022"))
            {
                var r022 = GetInstance("R022");
                if (r022 != null && r022.isBattleBuffActive)
                {
                    damageMultiplier *= 1.03f;
                }
            }

            // R023 — Crimson Testament: Party +18% Damage Bonus for all direct-damage
            if (HasRelic("R023"))
            {
                damageMultiplier *= 1.18f;
            }

            // R024 — Hollow Saint's Aegis (Demerit): Party -15% Damage Bonus
            if (HasRelic("R024"))
            {
                damageMultiplier *= 0.85f;
            }

            // R025 — Arcanum Unbound: Direct damage Skill -> +25% Damage Bonus
            if (HasRelic("R025") && isSkill)
            {
                damageMultiplier *= 1.25f;
            }

            // R027 — Breaker's Due: Party +30% Break Power, Demerit: -12% Damage Bonus
            if (HasRelic("R027"))
            {
                bonusBreakPower += Math.Max(1, (int)Math.Round(bonusBreakPower * 0.30f));
                damageMultiplier *= 0.88f;
            }

            // R028 — Wound-Crowned Idol: Character HP <= 50% -> +25% Damage Bonus
            if (HasRelic("R028") && actor.Stats.maxHp > 0)
            {
                if ((float)actor.unit.hp / actor.Stats.maxHp <= 0.50f)
                {
                    damageMultiplier *= 1.25f;
                }
            }

            // R029 — Bell of the Last Stand: >= 2 party Down -> active characters gain +25% Damage Bonus
            if (HasRelic("R029") && battle != null)
            {
                int downCount = battle.Allies(actor).Count(c => !c.Alive);
                if (downCount >= 2)
                {
                    damageMultiplier *= 1.25f;
                }
            }

            // R030 — The Butcher's Ledger: +6% Damage Bonus per kill stack (max 4x = +24%)
            if (HasRelic("R030"))
            {
                var r030 = GetInstance("R030");
                if (r030 != null && r030.counter > 0)
                {
                    int stacks = Math.Min(4, r030.counter);
                    damageMultiplier *= (1f + (0.06f * stacks));
                }
            }

            // ---- Cursed Equipment Boons & Curses (Attacker) ----
            if (actor.unit != null && actor.unit.equipped != null)
            {
                // EQ073 Oathbreaker: +15% damage against enemies > 70% HP
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ073") && target != null && target.Stats.maxHp > 0)
                {
                    if ((float)target.unit.hp / target.Stats.maxHp > 0.70f)
                        damageMultiplier *= 1.15f;
                }

                // EQ074 Maw of the War God: Up to +18% damage scaling with missing HP (max at <= 30% HP)
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ074") && actor.Stats.maxHp > 0)
                {
                    float missingPct = 1f - ((float)actor.unit.hp / actor.Stats.maxHp);
                    float mawBonus = Math.Clamp(missingPct / 0.70f, 0f, 1f) * 0.18f;
                    damageMultiplier *= (1f + mawBonus);
                }

                // EQ075 Fists of the Hollow Saint: Combo attacks +12% damage, +3 Break Power
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ075") && actor.CurrentCombo > 0)
                {
                    damageMultiplier *= 1.12f;
                    bonusBreakPower += 3;
                }

                // EQ077 Last Hunt: +16% against enemies that haven't taken a turn, +8% against Bosses, -12% to Broken
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ077") && target != null)
                {
                    if (!target.hasActedInBattle) damageMultiplier *= 1.16f;
                    if (target.enemy != null && target.enemy.tier == Tier.Boss) damageMultiplier *= 1.08f;
                    if (target.IsBroken) damageMultiplier *= 0.88f;
                }

                // EQ078 Mercy's End: Holy Skill damage +14%
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ078") && isSkill && skill != null && skill.element == Element.Holy)
                {
                    damageMultiplier *= 1.14f;
                }

                // EQ079 Star-Eater's Scepter: Direct skill damage +16%
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ079") && isSkill)
                {
                    damageMultiplier *= 1.16f;
                }

                // EQ080 Testament of the Damned: +12% damage to enemies with a Status Effect
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ080") && target != null && ((target.StatusController != null && target.StatusController.ActiveInstances.Count > 0) || (target.statuses != null && target.statuses.Count > 0)))
                {
                    damageMultiplier *= 1.12f;
                }

                // EQ081 Root of the Forsaken: Earth & elemental skill damage +12%
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ081") && isSkill && skill != null && (skill.element == Element.Earth || skill.element == Element.Fire || skill.element == Element.Ice || skill.element == Element.Lightning))
                {
                    damageMultiplier *= 1.12f;
                }

                // EQ083 Crown of the Drowned King: Skill damage +10%
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ083") && isSkill)
                {
                    damageMultiplier *= 1.10f;
                }

                // EQ086 Steps Beyond the Grave: First attack each battle deals +18% damage
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ086") && isFirstPartyAttack)
                {
                    damageMultiplier *= 1.18f;
                }

                // EQ087 Eye of the Unblinking: +12% damage against enemies at full HP
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ087") && target != null && target.Stats.maxHp > 0 && target.unit.hp >= target.Stats.maxHp)
                {
                    damageMultiplier *= 1.12f;
                }

                // EQ089 Covenant of a Thousand Wounds: All damage +8%, Break Power +3
                if (actor.unit.equipped.Values.Any(e => e.id == "EQ089"))
                {
                    damageMultiplier *= 1.08f;
                    bonusBreakPower += 3;
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

            // R003 — Rustbound Medal: HP <= 30% -> All damage taken -8%
            if (HasRelic("R003") && target.Stats.maxHp > 0)
            {
                float hpPct = (float)target.unit.hp / target.Stats.maxHp;
                if (hpPct <= 0.30f)
                {
                    damageMultiplier *= 0.92f;
                    battle?.Say($"[Relic: Rustbound Medal] -8% damage taken by {target.name} under 30% HP!");
                }
            }

            // R011 — Hollow Knight's Buckle: >= 2 party Down -> All damage taken -8%
            if (HasRelic("R011") && battle != null)
            {
                int downCount = battle.Allies(target).Count(c => !c.Alive);
                if (downCount >= 2)
                {
                    damageMultiplier *= 0.92f;
                    battle?.Say($"[Relic: Hollow Knight's Buckle] -8% damage taken (2+ allies down)!");
                }
            }

            // R023 — Crimson Testament (Demerit): Party receives +12% All Damage Taken
            if (HasRelic("R023"))
            {
                damageMultiplier *= 1.12f;
            }

            // R024 — Hollow Saint's Aegis: Party receives -22% All Damage Taken
            if (HasRelic("R024"))
            {
                damageMultiplier *= 0.78f;
            }

            // R029 — Bell of the Last Stand (Demerit): Active party receives +15% All Damage Taken while active
            if (HasRelic("R029") && battle != null)
            {
                int downCount = battle.Allies(target).Count(c => !c.Alive);
                if (downCount >= 2)
                {
                    damageMultiplier *= 1.15f;
                }
            }

            // R030 — The Butcher's Ledger (Demerit): +4% All Damage Taken per kill stack (max +16%)
            if (HasRelic("R030"))
            {
                var r030 = GetInstance("R030");
                if (r030 != null && r030.counter > 0)
                {
                    int stacks = Math.Min(4, r030.counter);
                    damageMultiplier *= (1f + (0.04f * stacks));
                }
            }

            // ---- Cursed Equipment Boons & Curses (Target) ----
            if (target.unit != null && target.unit.equipped != null)
            {
                // EQ074 Maw of the War God (Curse): While above 70% HP, takes +12% damage
                if (target.unit.equipped.Values.Any(e => e.id == "EQ074") && target.Stats.maxHp > 0)
                {
                    if ((float)target.unit.hp / target.Stats.maxHp > 0.70f)
                        damageMultiplier *= 1.12f;
                }

                // EQ076 The Widow's Kiss (Curse): Takes +12% Physical damage
                if (target.unit.equipped.Values.Any(e => e.id == "EQ076") && isDirectDamage)
                {
                    damageMultiplier *= 1.12f;
                }

                // EQ079 Star-Eater's Scepter (Curse): When MP <= 30% Max MP, takes +15% damage
                if (target.unit.equipped.Values.Any(e => e.id == "EQ079") && target.Stats.maxMp > 0)
                {
                    if ((float)target.unit.mp / target.Stats.maxMp <= 0.30f)
                        damageMultiplier *= 1.15f;
                }

                // EQ082 The Unyielding Coffin (Boon): Reduce Physical Damage Taken by 8%
                if (target.unit.equipped.Values.Any(e => e.id == "EQ082") && isDirectDamage)
                {
                    damageMultiplier *= 0.92f;
                }

                // EQ084 Armor of the Last Martyr (Boon): Reduce all incoming damage by 8%
                if (target.unit.equipped.Values.Any(e => e.id == "EQ084"))
                {
                    damageMultiplier *= 0.92f;
                }

                // EQ085 Shackles of the Fallen (Boon): While <= 50% HP, gain additional 6% Physical Damage reduction
                if (target.unit.equipped.Values.Any(e => e.id == "EQ085") && isDirectDamage && target.Stats.maxHp > 0)
                {
                    if ((float)target.unit.hp / target.Stats.maxHp <= 0.50f)
                        damageMultiplier *= 0.94f;
                }
            }
        }

        public void ModifyMpCost(Combatant actor, SkillDef skill, ref int mpCost)
        {
            if (actor == null || skill == null || mpCost <= 0) return;

            // R025 — Arcanum Unbound (Demerit): +20% MP cost
            if (HasRelic("R025"))
            {
                mpCost = (int)Math.Round(mpCost * 1.20f);
            }

            // R026 — The Empty Chalice: -30% MP cost
            if (HasRelic("R026"))
            {
                mpCost = (int)Math.Round(mpCost * 0.70f);
            }

            // EQ075 Fists of the Hollow Saint (Curse): All Skills cost +1 MP
            if (actor.unit != null && actor.unit.equipped != null && actor.unit.equipped.Values.Any(e => e.id == "EQ075"))
            {
                mpCost += 1;
            }

            if (mpCost < 1) mpCost = 1;
        }

        public void ModifyHealing(Combatant healer, Combatant target, ref float healMultiplier)
        {
            // R026 — The Empty Chalice (Demerit): -25% healing from skills and HP consumables
            if (HasRelic("R026"))
            {
                healMultiplier *= 0.75f;
            }

            if (healer != null && healer.unit != null && healer.unit.equipped != null)
            {
                // EQ078 Mercy's End (Curse): Healing provided to allies is reduced by 20%
                if (healer.unit.equipped.Values.Any(e => e.id == "EQ078"))
                    healMultiplier *= 0.80f;

                // EQ081 Root of the Forsaken (Curse): Healing provided is reduced by 18%
                if (healer.unit.equipped.Values.Any(e => e.id == "EQ081"))
                    healMultiplier *= 0.82f;
            }

            if (target != null && target.unit != null && target.unit.equipped != null)
            {
                // EQ073 Oathbreaker (Curse): Healing received -15%
                if (target.unit.equipped.Values.Any(e => e.id == "EQ073"))
                    healMultiplier *= 0.85f;

                // EQ089 Covenant of a Thousand Wounds (Curse): Healing received -20%
                if (target.unit.equipped.Values.Any(e => e.id == "EQ089"))
                    healMultiplier *= 0.80f;
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

            // EQ088 Heart of the Hollow: Once per battle survive lethal hit at 1 HP; Max MP reduced by 50% until battle ends
            if (target.unit != null && target.unit.equipped != null && target.unit.equipped.Values.Any(e => e.id == "EQ088"))
            {
                if (target.unit.hp - incomingDamage <= 0)
                {
                    target.unit.hp = 1;
                    target.unit.mp = Math.Min(target.unit.mp, target.Stats.maxMp / 2);
                    battle?.Say($"[Cursed: Heart of the Hollow] Lethal blow prevented! {target.name} survives at 1 HP; MP halved.");
                    return true;
                }
            }

            return false;
        }

        public void OnQteResolved(Combatant actor, bool isOffensive, QteResult grade, Battle battle = null)
        {
            if (actor == null || !actor.IsPlayer) return;

            // R002 — Soulglass Shard: Every 3 Perfect Offensive QTEs -> +2% Max MP to active character
            if (isOffensive && grade == QteResult.Perfect && HasRelic("R002"))
            {
                var r002 = GetInstance("R002");
                if (r002 != null)
                {
                    r002.counter++;
                    if (r002.counter % 3 == 0)
                    {
                        int before = actor.unit.mp;
                        int mpGained = Math.Max(1, (int)Math.Round(actor.Stats.maxMp * 0.02f));
                        actor.unit.mp = Math.Min(actor.Stats.maxMp, actor.unit.mp + mpGained);
                        int gained = actor.unit.mp - before;
                        battle?.Say($"[Relic: Soulglass Shard] 3rd Perfect Offensive QTE! {actor.name} restores {gained} MP.");
                    }
                }
            }

            // R019 — Black-Iron Rosary: Perfect Defensive QTE -> Shield = 1.5% Max HP
            if (!isOffensive && grade == QteResult.Perfect && HasRelic("R019"))
            {
                var r019 = GetInstance("R019");
                if (r019 != null && !r019.turnTriggered)
                {
                    r019.turnTriggered = true;
                    int shieldAmt = Math.Max(1, (int)Math.Round(actor.Stats.maxHp * 0.015f));
                    actor.AddShield(shieldAmt);
                    battle?.Say($"[Relic: Black-Iron Rosary] Perfect Defense! {actor.name} gains +{shieldAmt} Barrier Shield (max 1/turn).");
                }
            }
        }

        public void OnEnemyDefeated(Combatant killer, Combatant enemy, Battle battle = null)
        {
            if (enemy == null || !enemy.IsEnemy) return;

            // R013 — Dirge-Eater's Fang: Killer restores 1% Max MP (max 2 per battle)
            if (killer != null && killer.IsPlayer && killer.Alive && HasRelic("R013"))
            {
                var r013 = GetInstance("R013");
                if (r013 != null && r013.battleUsageCount < 2)
                {
                    r013.battleUsageCount++;
                    int before = killer.unit.mp;
                    int mpGain = Math.Max(1, (int)Math.Round(killer.Stats.maxMp * 0.01f));
                    killer.unit.mp = Math.Min(killer.Stats.maxMp, killer.unit.mp + mpGain);
                    int gained = killer.unit.mp - before;
                    battle?.Say($"[Relic: Dirge-Eater's Fang] Killing blow! {killer.name} restores {gained} MP ({r013.battleUsageCount}/2 this battle).");
                }
            }

            // R022 — Zoroa's Dull Crown: Defeating 3 enemies in battle -> Party gets +3% damage until battle ends
            if (HasRelic("R022"))
            {
                var r022 = GetInstance("R022");
                if (r022 != null)
                {
                    r022.counter++;
                    if (r022.counter >= 3 && !r022.isBattleBuffActive)
                    {
                        r022.isBattleBuffActive = true;
                        battle?.Say("[Relic: Zoroa's Dull Crown] 3 enemies defeated! Party damage +3% for remainder of battle!");
                    }
                }
            }

            // R030 — The Butcher's Ledger: Each kill grants 1 stack of +6% Damage Bonus (max 4 stacks = +24%)
            if (HasRelic("R030"))
            {
                var r030 = GetInstance("R030");
                if (r030 != null && r030.counter < 4)
                {
                    r030.counter++;
                    battle?.Say($"[Relic: The Butcher's Ledger] Kill momentum stack {r030.counter}/4 (+{r030.counter * 6}% Dmg, +{r030.counter * 4}% Dmg Taken)!");
                }
            }
        }

        public void OnBossHpChanged(Combatant boss, Battle battle = null)
        {
            if (boss == null || boss.enemy == null || boss.enemy.tier != Tier.Boss) return;

            // R020 — Sovereign's Last Breath: Boss <= 25% HP -> Party restores 2% Max MP (once per battle)
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
                                int mpGain = Math.Max(1, (int)Math.Round(member.Stats.maxMp * 0.02f));
                                member.unit.mp = Math.Min(member.Stats.maxMp, member.unit.mp + mpGain);
                            }
                            battle.Say("[Relic: Sovereign's Last Breath] Boss HP fallen below 25%! Entire party restores 2% Max MP!");
                        }
                    }
                }
            }
        }

        public void OnBattleWon(RunState run)
        {
            if (run == null || run.Party == null) return;

            // R004 — Gravekeeper's Thread: Lowest HP member restores 2% Max HP
            if (HasRelic("R004"))
            {
                var lowest = run.Party
                    .Where(u => u.Alive && u.stats.maxHp > 0)
                    .OrderBy(u => (float)u.hp / u.stats.maxHp)
                    .FirstOrDefault();

                if (lowest != null)
                {
                    int healAmt = Math.Max(1, (int)Math.Round(lowest.stats.maxHp * 0.02f));
                    lowest.hp = Math.Min(lowest.stats.maxHp, lowest.hp + healAmt);
                }
            }
        }

        public void ModifyGoldReward(ref int gold)
        {
            if (gold <= 0) return;

            // R031 — Pilgrim's Burden (Demerit): Run Gold -15%
            if (HasRelic("R031"))
            {
                int penalty = (int)Math.Round(gold * 0.15f);
                gold = Math.Max(1, gold - penalty);
            }

            // R032 — Merchant's Burden: Run Gold +15%
            if (HasRelic("R032"))
            {
                int bonus = (int)Math.Round(gold * 0.15f);
                gold += bonus;
            }
        }

        public void ModifyExpReward(ref int exp)
        {
            if (exp <= 0) return;

            // R031 — Pilgrim's Burden: EXP +15%
            if (HasRelic("R031"))
            {
                int bonus = (int)Math.Round(exp * 0.15f);
                exp += bonus;
            }

            // R032 — Merchant's Burden (Demerit): EXP -15%
            if (HasRelic("R032"))
            {
                int penalty = (int)Math.Round(exp * 0.15f);
                exp = Math.Max(1, exp - penalty);
            }
        }

        public void ModifyConsumableHeal(ref float healMultiplier)
        {
            // R016 — Royal Gravewax: HP recovery items restore +5% additional base value
            if (HasRelic("R016"))
            {
                healMultiplier *= 1.05f;
            }

            // R026 — The Empty Chalice (Demerit): Consumable healing -25%
            if (HasRelic("R026"))
            {
                healMultiplier *= 0.75f;
            }
        }

        public float GetStatusResistanceBonus(StatusId statusId)
        {
            // R015 — Veil of Dust: Burn, Poison, and Bleed resistance +8%
            if (HasRelic("R015"))
            {
                if (statusId == StatusId.Burn || statusId == StatusId.Poison || statusId == StatusId.Bleed)
                {
                    return 0.08f;
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
