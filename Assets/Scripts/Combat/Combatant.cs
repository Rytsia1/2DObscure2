using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public enum Side { Party, Enemy }

    // A unit's per-battle wrapper. HP/MP live on the Unit so party damage persists across the run;
    // statuses, cooldowns, break gauge and boss phase live here, so they end with the battle.
    public class Combatant
    {
        public Unit unit;                  // replaced only by a boss phase transition (AdvanceToPhase)
        public readonly Side side;
        public readonly int slot;          // formation index, also the turn-order tiebreaker
        public readonly string name, tag;  // log name ("Skeleton B"), turn-bar tag ("SKB")
        public EnemyDef enemy;             // null for party members; the current phase's def for bosses
        public RunState run;               // active run context for modifier/relic queries

        public bool guarding;   // halves incoming damage until this combatant's next turn starts
        public int stunTurns;   // turns to skip; the skip consumes the turn
        public int CounterTurns;          // counter skill readiness, ticks at the start of own turn (Bab 12 §6.5)
        public float CounterMultiplier = 1f;
        public int CounterComboGain;
        public bool perfectDefenseThisAction; // set by Battle.Deal, consumed when the enemy action ends
        public int phase = 1;   // boss phase
        public readonly List<StatusEffect> statuses = new List<StatusEffect>();
        public readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>();
        // Move keys whose cooldown ticked from 1 -> 0 during this turn's StartTurn; kept blocked during this turn's
        // provisional re-evaluation so cooldown = N preserves its exact N-turn duration.
        public readonly HashSet<string> cooldownsJustExpiredThisTurn = new HashSet<string>();

        // Enemy intent (Bab 10 §8.9, Bab 12 §6.4): planned at round start; a charged move is released on the next turn.
        public EnemyIntent Intent;
        public EnemyIntent Charge;
        // Charge gap: own turns after a charged release before another charge can start (posture-B starting value, untested).
        // Pass test: in a Zoroa fight, 50-66% of Zoroa's turns are charge or release, and Soul Lance shows up at least once per 4 turns.
        // If it fails: still feels charge-heavy -> 2; Zoroa feels flat and has no signature moments -> 0 for Boss tier only.
        public const int ChargeGapTurns = 1; // posture-B starting value (untested)
        // ponytail: bool = gap of 1; counter if ChargeGapTurns > 1
        public bool JustReleasedCharge;
        public bool UsedHeavyThisRound;
        public bool IsSummon;
        public bool transformRolled; // On-Defeat Transformation: evaluated at most once per enemy instance (Transformation Spec §4)
        // Observable per-battle targeting memory used by InstinctLayer & TacticalSynergy (never reads hidden info).
        public Combatant LastAttacker;     // last living opponent who dealt damage to this combatant
        public Combatant LastTargetedHero; // last living hero targeted by this enemy's single-target attack
        public int LastDamagedRound;       // battle round when this combatant last took damage
        // Hidden Stage 2-3 Elite & Boss combat affixes (not exposed in Analysis or HUD).
        public readonly List<EnemyAffix> EnemyAffixes = new List<EnemyAffix>();
        public bool HasAffix(EnemyAffix affix) => EnemyAffixes.Contains(affix);
        // Double-turn support (Zoroa rework): SecondTurnSpeedFactor is a posture-B starting value (untested).
        // Pass test: at least 2 of 4 heroes act between Zoroa's A and B turns in a typical party.
        // If it fails: too few -> 0.4; turns feel disconnected -> 0.6.
        public const float SecondTurnSpeedFactor = 0.5f; // posture-B starting value (untested)
        public int TurnsPerRound => Math.Max(1, enemy?.turnsPerRound ?? 1);
        public int turnsTakenThisRound;
        public float TurnSpeed(int turnIndex) => turnIndex <= 0 ? Speed : Speed * SecondTurnSpeedFactor;
        public bool IsCharging => Charge != null || (Intent != null && Intent.charging);
        public static string MoveKey(EnemyAttack m) =>
            string.IsNullOrEmpty(m.patternId) ? "move:" + m.name : $"move:{m.name}:{m.patternId}";

        // Break/Stagger System state (PRD Section 9)
        public int MaxBreakGauge;
        public int CurrentBreakGauge;
        public int BreakResistance;
        public int BreakDuration = 1;
        public int BreakStunTurns = 1;
        public float BreakDamageMultiplier = 1.5f;

        // Player Poise (party-side Break) tuning knobs.
        public const int PlayerPoiseGauge = 60;
        public const float PlayerBrokenDamageMultiplier = 1.25f;
        public const float PlayerPoiseMitigatedMult = 0.5f;  // Good defensive QTE or Guarding
        public const float PlayerPoiseHeavyMult = 1.5f;
        public const int PlayerPoiseRecovery = 20;           // per turn, only if not hit since last turn
        // Enemy base break power by tier (posture-B starting value, untested).
        // Pass test: in a D1 playtest, a hero breaks 1-2 times per Elite/Zoroa fight when QTEs are missed, and about 0 times when they are hit.
        // If it fails: too many breaks -> lower Elite/Boss by 4; still no breaks -> raise by 4.
        public const int NormalEnemyBaseBreakPower = 12;     // posture-B starting value (untested)
        public const int EliteEnemyBaseBreakPower = 20;      // posture-B starting value (untested)
        public const int BossEnemyBaseBreakPower = 28;       // posture-B starting value (untested)
        public static int EnemyBaseBreakPower(Tier tier) => tier switch
        {
            Tier.Boss => BossEnemyBaseBreakPower,
            Tier.Elite => EliteEnemyBaseBreakPower,
            _ => NormalEnemyBaseBreakPower,
        };
        // GDD Bab 10 §8.11: the Break gauge drops 20 at the start of its owner's turn. Drawn as a shield that drains toward Broken,
        // so here it refills by the same amount.
        public const int EnemyShieldRecovery = 20;

        // Player-facing explanation of an enemy's shield (its Break gauge), with this enemy's own numbers.
        public string ShieldHelp =>
            $"SHIELD: BREAK DAMAGE WEARS IT DOWN. AT 0 IT IS BROKEN FOR {BreakDuration} TURN{(BreakDuration == 1 ? "" : "S")} " +
            $"AND TAKES {Math.Round(BreakDamageMultiplier * 100)}% DAMAGE. REGAINS {EnemyShieldRecovery} AT THE START OF ITS TURN.";

        // Racial Feat per-battle state (GDD Bab 6 & Bab 12)
        public bool DeterminationUsed;
        public int DeterminationTurns;
        public int FeyStepCooldown;
        public bool IsPhased;
        public bool FeyStepBonusPending;
        public int IronResolveCooldown;
        public bool IronResolveActive;
        public int SecondChanceRetriesRemaining = 0;
        public bool UsedDraconicOverdrive;
        public int DraconicOverdriveTurns;
        public bool OverdriveExhaustPending;
        public int DraconicExhaustedTurns;
        public bool UsedLightbound;
        public int LightboundTurns;
        public float LightboundResistBonus;
        public int OceansEmbraceCooldown;
        public int OceansEmbraceTurns;
        public bool OceansEmbraceShieldActive;
        public Combatant OceansEmbraceTarget;
        public bool UsedOceansEmbrace;

        // Martial Artist Combo Flow state (GDD Bab 8 §5.1 & Bab 12 §7)
        public const int MaxComboNormal = 10;
        public int CurrentCombo = 0;
        public bool AttackedThisTurn = false;

        // Background System per-battle and per-turn state (Background System Section 3)
        public readonly HashSet<Combatant> charlatanFirstAttackedTargets = new HashSet<Combatant>();
        public bool soldierDamagedThisTurn;
        public bool soldierRetaliationReady;
        public bool hermitResistedThisTurn;
        public int wayfarerSpeedBuffTurns;
        public readonly Dictionary<Combatant, int> scribeAttackCounts = new Dictionary<Combatant, int>();
        public bool hasActedInBattle;

        // Relic System combat state (e.g. R017 Oathstone of the Fallen)
        public bool tookDamageSinceLastTurn;

        public void AddShield(int amount)
        {
            if (unit != null)
            {
                unit.shield += amount;
            }
        }
        public int luckyActivationsRemaining = TalentSystem.LuckyMaxPerBattle;
        public bool luckyUsedForCurrentQte = false;
        public bool CanTriggerLuckyRetry(QteResult result, Func<float> rngProvider = null)
        {
            if (!Alive || unit == null || !unit.HasTalent("lucky")) return false;
            if (luckyActivationsRemaining <= 0 || luckyUsedForCurrentQte) return false;
            if (result != QteResult.Miss) return false;
            float roll = rngProvider != null ? rngProvider() : UnityEngine.Random.value;
            return roll < TalentSystem.LuckyActivationChance;
        }
        public void RecordLuckyRetry()
        {
            if (luckyActivationsRemaining > 0) luckyActivationsRemaining--;
            luckyUsedForCurrentQte = true;
        }
        public void ResetQteLuckyFlag() => luckyUsedForCurrentQte = false;

        public void ResetTurnBackgroundPerks()
        {
            soldierDamagedThisTurn = false;
            hermitResistedThisTurn = false;
        }

        // Modular Damage System per-battle state (Prompt Sections 5, 7, 8, 9)
        public EnemyAffinityProfile AffinityProfile { get; private set; }
        public readonly NullBreakController NullBreak = new NullBreakController();
        public readonly ElementalResistanceProfile ElementalResistances;
        public readonly DamageModifierSet DamageModifiers;

        // Status Effect System controller (GDD Sections 8-13)
        public readonly StatusEffectController StatusController = new StatusEffectController();

        public Combatant(Unit unit, Side side, int slot, string name, string tag, EnemyDef enemy = null)
        {
            this.unit = unit;
            this.side = side;
            this.slot = slot;
            this.name = name;
            this.tag = tag;
            this.enemy = enemy;

            AffinityProfile = enemy?.affinityProfile != null
                ? enemy.affinityProfile.Clone()
                : EnemyAffinityProfile.FromLegacyAffinities(unit?.affinities);
            ElementalResistances = unit?.elementalResistances != null
                ? unit.elementalResistances.Clone()
                : new ElementalResistanceProfile();
            DamageModifiers = unit?.damageModifiers != null
                ? unit.damageModifiers.Clone()
                : new DamageModifierSet();

            StatusController.isBoss = enemy != null && enemy.tier == Tier.Boss;
            if (unit != null)
            {
                StatusController.ResistanceProfile.statsResistance = unit.Derived.StatusResistFraction;
                StatusController.ResistanceProfile.mentalStatsResistance = unit.Derived.MentalResistFraction;
            }
            if (enemy != null && enemy.tier == Tier.Boss)
                StatusController.ResistanceProfile.baseResistance = 0.50f; // High base resistance for Bosses (GDD Section 13)
            ApplyEnemyStatusResistances(enemy);

            if (unit != null && unit.HasTalent("durable"))
                DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 1f - TalentSystem.DurableDamageReduction, sourceId: "durable");
            if (unit?.race?.racialFeat != null && unit.race.racialFeat.canRetryFailedQte)
                SecondChanceRetriesRemaining = Math.Max(0, unit.race.racialFeat.maxQteRetriesPerBattle);

            if (enemy != null)
            {
                MaxBreakGauge = Math.Max(0, enemy.maxBreakGauge);
                CurrentBreakGauge = MaxBreakGauge;
                BreakResistance = Math.Max(0, enemy.breakResistance + Math.Max(0, unit.Mod(Ability.VIT)));
                BreakDuration = Math.Max(1, enemy.breakDuration);
                BreakStunTurns = Math.Max(0, enemy.breakStunTurns);
                BreakDamageMultiplier = Math.Max(1f, enemy.breakDamageMultiplier);
                ResetMoveCooldowns();
            }
            else if (side == Side.Party)
            {
                MaxBreakGauge = CurrentBreakGauge = PlayerPoiseGauge;
                BreakResistance = 0; // players use StaggerRes as a % reduction instead (ApplyBreakDamage)
                BreakDuration = 1;
                BreakStunTurns = 1;
                BreakDamageMultiplier = PlayerBrokenDamageMultiplier;
            }
        }

        void ApplyEnemyStatusResistances(EnemyDef def)
        {
            StatusController.isBoss = def != null && def.tier == Tier.Boss;
            StatusController.ResistanceProfile.perStatus.Clear();
            StatusController.ResistanceProfile.immunities.Clear();
            if (def == null) return;
            if (def.statusResist != null)
            {
                foreach (var kv in def.statusResist)
                    StatusController.ResistanceProfile.SetPerStatusResistance(kv.Key, kv.Value);
            }
            if (def.statusImmune != null)
            {
                foreach (var st in def.statusImmune)
                    StatusController.ResistanceProfile.SetImmune(st, true);
            }
        }

        void ResetMoveCooldowns()
        {
            foreach (var key in cooldowns.Keys.Where(k => k.StartsWith("move:")).ToList()) cooldowns.Remove(key);
            foreach (var m in enemy.attacks ?? Array.Empty<EnemyAttack>())
                if (m.initialCooldown > 0) cooldowns[MoveKey(m)] = m.initialCooldown;
        }

        // Boss HP bar depleted: the same combatant continues as the next phase with a fresh bar and MP pool.
        // Statuses, stun and queue position are kept; planned/charged actions belong to the old phase and are dropped.
        public void AdvanceToPhase(EnemyDef next, int dungeonNumber, int stage = 1)
        {
            bool wasBroken = IsBroken;
            enemy = next;
            unit = new Unit(next, dungeonNumber, stage);
            AffinityProfile = next?.affinityProfile != null
                ? next.affinityProfile.Clone()
                : EnemyAffinityProfile.FromLegacyAffinities(unit?.affinities);
            ApplyEnemyStatusResistances(next);
            phase++;
            Intent = Charge = null;
            JustReleasedCharge = false;
            MaxBreakGauge = Math.Max(0, next.maxBreakGauge);
            BreakResistance = Math.Max(0, next.breakResistance + Math.Max(0, unit.Mod(Ability.VIT)));
            EnemyAffixSystem.ApplyInitialModifiers(this, resetCurrentBreakGauge: false);
            if (wasBroken && next.bossBreakRecoverOnPhase2) RecoverBreakGauge();
            else CurrentBreakGauge = Math.Min(CurrentBreakGauge, MaxBreakGauge);
            var carriedCooldowns = new Dictionary<string, int>(cooldowns);
            ResetMoveCooldowns();
            foreach (var m in next.attacks ?? Array.Empty<EnemyAttack>())
                if (m.Summons && carriedCooldowns.TryGetValue(MoveKey(m), out var cd) && cd > 0)
                    cooldowns[MoveKey(m)] = cd; // Harvest Call cooldown carries over the phase change
        }

        // Resets state after an On-Defeat Transformation into an Enhanced Common Variant (Transformation Spec §7 v1.1).
        // Full Enhanced HP & MP, Break refilled & Broken removed, all statuses/DoTs/stun/intent/charge cleared, cooldowns reset.
        public void ResetForTransform()
        {
            phase = 1;
            statuses.Clear();
            StatusController.ClearAll();
            stunTurns = 0;
            Intent = null;
            Charge = null;
            JustReleasedCharge = false;
            guarding = false;
            LastAttacker = null;
            LastTargetedHero = null;
            LastDamagedRound = 0;
            RecoverBreakGauge();
            if (unit != null)
            {
                unit.hp = unit.stats.maxHp;
                unit.mp = unit.stats.maxMp;
                unit.shield = 0;
                StatusController.ResistanceProfile.statsResistance = unit.Derived.StatusResistFraction;
                StatusController.ResistanceProfile.mentalStatsResistance = unit.Derived.MentalResistFraction;
            }
            cooldowns.Clear();
            cooldownsJustExpiredThisTurn.Clear();
            ResetMoveCooldowns();
        }

        public bool Alive => unit.Alive;
        public bool IsEnemy => side == Side.Enemy;
        public bool IsPlayer => side == Side.Party;
        public Stats Stats => unit.stats;
        public DerivedStats BaseDerived => unit.Derived;
        public Affinities Affinities => unit.affinities;

        public bool HasBreakGauge => MaxBreakGauge > 0;
        public bool IsBroken => Has(StatusId.Broken);
        // Stun, Sleep and Mind Control: any Defensive QTE auto-resolves as Poor (GDD Bab 11 & 12).
        public bool CannotReact => stunTurns > 0 || StatusController.HasStatus(StatusId.Stun)
            || StatusController.HasStatus(StatusId.Sleep) || StatusController.HasStatus(StatusId.MindControl);
        public bool IsMindControlled => StatusController.HasStatus(StatusId.MindControl);

        // Barbarian Starter Passive — Savage Instinct: grants Physical Power bonus when HP is below 50%.
        public int LowHpPhysicalPowerBonus
        {
            get
            {
                if (unit?.passives == null || unit.stats.maxHp <= 0) return 0;
                int bonus = 0;
                float hpRatio = (float)unit.hp / unit.stats.maxHp;
                foreach (var p in unit.passives)
                {
                    if (p != null && p.lowHpPhysicalPowerBonus != 0 && hpRatio < p.lowHpThreshold)
                        bonus += p.lowHpPhysicalPowerBonus;
                }
                return bonus;
            }
        }

        // Effective stats: base/derived stats modified by combat statuses. Speed/Initiative changes apply from the next round.
        public int Atk => Scale(unit.stats.atk + LowHpPhysicalPowerBonus, (1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown) - Mag(StatusId.Exhausted)) * StatusController.GetDamageDealtMultiplier());
        public int RangedAtk => Scale(unit.Derived.rangedPower, (1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown) - Mag(StatusId.Exhausted)) * StatusController.GetDamageDealtMultiplier());
        public int Magic => Scale(unit.stats.mag, (1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown) - Mag(StatusId.Exhausted)) * StatusController.GetDamageDealtMultiplier());
        public int UnscaledHealBase =>
            unit.Derived.unscaledHealingBase > 0 ? unit.Derived.unscaledHealingBase : (unit.Derived.healingPower > 0 ? unit.Derived.healingPower : unit.stats.mag);

        public float EffectiveHealMultiplier
        {
            get
            {
                float bgBonus = 0f;
                if (unit?.background != null)
                {
                    var rank = unit.background.GetRank(unit.level);
                    if (rank != null) bgBonus = rank.healingDealtBonus;
                }
                float wisPct = AttributeSystemConfig.Default.wisHealingPowerPerMod * unit.Mod(Ability.WIS);
                float statusMult = Math.Max(0f, 1f + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown));
                return Math.Max(0f, 1f + wisPct + bgBonus) * statusMult;
            }
        }

        public int HealPower => Math.Max(0, StatCalculator.FloorInt(UnscaledHealBase * EffectiveHealMultiplier));
        public int SupportPower => unit.Derived.supportPower;

        // Physical Defense and Magic Defense are separate (PRD Section 8.8).
        public int Def => Scale(unit.stats.def, 1 + Mag(StatusId.DefenseUp) - Mag(StatusId.DefenseDown));
        public int MagicDef => Scale(
            unit.Derived.magicDefense > 0 || !IsEnemy ? unit.Derived.magicDefense : unit.stats.def,
            1 + Mag(StatusId.MagicDefenseUp) - Mag(StatusId.MagicDefenseDown));

        public int Speed
        {
            get
            {
                // Bab 10 §8.2 Effective Speed Formula: clamp(1 + TotalSpeedBonus - TotalSpeedPenalty, 0.50, 1.50)
                float bgSpeedBonus = wayfarerSpeedBuffTurns > 0 ? 0.10f : 0f;
                float legacyPenalty = (StatusController != null && StatusController.ActiveInstances.Count > 0) ? 0f : (Mag(StatusId.Slow) + Mag(StatusId.Freeze));
                float statusMult = StatusController != null ? StatusController.GetSpeedMultiplier() : 1f;
                float modPenalty = (IsPlayer && run?.ModifierManager != null) ? run.ModifierManager.GetSpeedPenalty() : 0f;
                float totalBonus = bgSpeedBonus + Math.Max(0f, statusMult - 1f);
                float totalPenalty = legacyPenalty + Math.Max(0f, 1f - statusMult) + modPenalty;
                float speedMod = Math.Clamp(1f + totalBonus - totalPenalty, 0.50f, 1.50f);
                return Math.Max(1, (int)Math.Floor(unit.stats.spd * speedMod));
            }
        }
        public int Initiative => Speed;
        public int ActionSpeed => Scale(unit.Derived.actionSpeed, (1 - Mag(StatusId.Slow) - Mag(StatusId.Freeze)) * StatusController.GetSpeedMultiplier());

        public int ArmorPen => unit.Derived.armorPenetration;
        public int MagicPen => unit.Derived.magicPenetration;
        public int CritChance => unit.Derived.criticalChance;
        public int CritDamage => unit.Derived.criticalDamage;
        public int PhysicalBreak
        {
            get
            {
                float mult = 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown);
                if (DraconicOverdriveTurns > 0) mult *= 1.50f;
                else if (DraconicExhaustedTurns > 0) mult *= 0.70f;
                return Scale(Math.Max(1, unit.Derived.physicalBreakPower), mult);
            }
        }
        public int MagicBreak
        {
            get
            {
                float mult = 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown);
                if (DraconicOverdriveTurns > 0) mult *= 1.50f;
                else if (DraconicExhaustedTurns > 0) mult *= 0.70f;
                return Scale(Math.Max(1, unit.Derived.magicBreakPower), mult);
            }
        }
        public int StaggerRes => unit.Derived.staggerResistance;
        public int StatusRes => unit.Derived.statusResistance;

        // Rogue Starter Passive — Opportunist: increases Critical Chance against enemies with debuffs.
        public int GetCritChanceAgainst(Combatant target)
        {
            int crit = CritChance;
            if (target != null && target.HasDebuff && unit?.passives != null)
            {
                foreach (var p in unit.passives)
                    if (p != null && p.critChanceVsDebuffedBonus != 0)
                        crit += p.critChanceVsDebuffedBonus;
            }
            if (target != null && unit?.background != null)
            {
                var rank = unit.background.GetRank(unit.level);
                if (rank != null && rank.scribeNextAttackCritBonus > 0f)
                {
                    if (scribeAttackCounts.TryGetValue(target, out int count) && count >= rank.scribeTargetAttackThreshold)
                        crit += (int)Math.Round(rank.scribeNextAttackCritBonus * 100f);
                }
            }
            if (target != null && target.unit != null && target.unit.HasTalent("alert"))
                crit -= (int)Math.Round(TalentSystem.AlertCritReduction * 100f);
            return Math.Clamp(crit, 0, 100);
        }

        public CriticalHitConfig GetCriticalHitConfig(Combatant target = null)
        {
            float chance = GetCritChanceAgainst(target) / 100f;
            float dmg = CritDamage > 0 ? CritDamage / 100f : CriticalHitConfig.BaseCriticalDamage;
            return CriticalHitConfig.FromTotals(chance, dmg);
        }

        public DamageContext CreateDamageContext(
            Combatant target,
            IReadOnlyList<DamageComponent> components,
            int hitCount = 1,
            QteResult qteResult = QteResult.Good,
            Func<float> criticalRollProvider = null,
            RunState runOverride = null)
        {
            var r = runOverride ?? run;
            var scores = unit != null ? unit.FinalScores : AbilityScores.Uniform(10);
            var attackerMods = DamageModifiers != null ? DamageModifiers.Clone() : new DamageModifierSet();
            var targetMods = target?.DamageModifiers != null ? target.DamageModifiers.Clone() : new DamageModifierSet();
            if (target?.StatusController != null && target.StatusController.HasStatus(StatusId.Curse))
                targetMods.AddMultiplier(DamageModifierScope.AllDamageTaken, target.StatusController.GetDamageTakenMultiplier(), sourceId: "status_curse");

            float physPen = (unit != null && unit.HasTalent("piercer")) ? TalentSystem.PiercerPenetration : 0f;
            if (IsPlayer && r?.ModifierManager != null)
            {
                physPen = Math.Clamp(physPen + r.ModifierManager.GetPhysicalPenetrationBonus(this), 0f, DungeonModifierManager.MaxPhysicalPenetration);
                r.ModifierManager.PopulateDamageModifiers(this, target, attackerMods, targetMods);
            }

            float targetDef = target != null ? (float)Math.Max(0.0, target.Def * (1f - physPen)) : 0f;

            return new DamageContext
            {
                attackerStr = scores.str,
                attackerInt = scores.intel,
                attackerModifiers = attackerMods,
                criticalHitConfig = GetCriticalHitConfig(target),
                targetPhysicalDefense = targetDef,
                targetModifiers = targetMods,
                targetEnemyAffinity = target?.AffinityProfile ?? new EnemyAffinityProfile(),
                targetNullBreak = target?.NullBreak,
                targetElementalResistance = target?.ElementalResistances ?? new ElementalResistanceProfile(),
                components = components ?? Array.Empty<DamageComponent>(),
                hitCount = Math.Max(1, hitCount),
                qteResult = qteResult,
                criticalRollProvider = criticalRollProvider
            };
        }

        // Wizard Starter Passive — Arcane Affinity: reduces the MP cost of Arcane Spells.
        // Sage Background Perk — Learned Mind: percentage reduction with min 1 MP floor.
        public int GetEffectiveMpCost(SkillDef skill, RunState run = null)
        {
            if (skill == null) return 0;
            int baseCost = skill.mpCost;
            if (baseCost <= 0) return 0;
            int cost = baseCost;
            if (skill.HasTag("Arcane") && unit?.passives != null)
            {
                foreach (var p in unit.passives)
                    if (p != null && p.arcaneMpCostReduction != 0)
                        cost -= p.arcaneMpCostReduction;
            }
            if (unit?.background != null)
            {
                var rank = unit.background.GetRank(unit.level);
                if (rank != null && rank.mpCostReductionPercent > 0f)
                {
                    cost = (int)Math.Round(cost * (1f - rank.mpCostReductionPercent));
                    if (cost < 1) cost = 1; // "An Active Skill with an MP cost cannot become free through this perk; its minimum cost remains 1 MP."
                }
            }
            if (unit != null && unit.HasTalent("keen_mind"))
            {
                cost = (int)Math.Round(cost * (1f - TalentSystem.KeenMindMpReduction));
                if (cost < TalentSystem.KeenMindMinFloor) cost = TalentSystem.KeenMindMinFloor;
            }

            // Relic modifiers (R025 Arcanum Unbound: +20% MP, R026 The Empty Chalice: -30% MP)
            var r = run ?? this.run;
            if (r?.RelicManager != null)
            {
                r.RelicManager.ModifyMpCost(this, skill, ref cost);
            }

            // Dungeon Modifier MP cost effects (MOD-008 Mana Well -5%/stack, MOD-021 Leaking Mana +1 MP/stack)
            if (IsPlayer && r?.ModifierManager != null)
            {
                r.ModifierManager.ModifyMpCost(this, skill, ref cost);
            }

            // Cursed Equipment: EQ075 Fists of the Hollow Saint: +1 MP cost
            if (unit != null && unit.equipped != null && unit.equipped.Values.Any(e => e.id == "EQ075"))
            {
                cost += 1;
            }

            return Math.Max(1, cost);
        }

        // Human Racial Feat — Determination: Shield 20% Max HP + All Damage Bonus +15% for 2 turns when dropped to <= 30% HP (1/battle)
        public bool CheckDeterminationTrigger(int hpBefore, int hpAfter)
        {
            if (!Alive || DeterminationUsed || unit?.race == null || unit.race.id != "human")
                return false;
            float threshold = Stats.maxHp * 0.30f;
            if (hpBefore > threshold && hpAfter <= threshold && hpAfter > 0)
            {
                DeterminationUsed = true;
                DeterminationTurns = 2;
                int shieldAmt = Math.Max(1, (int)Math.Round(Stats.maxHp * 0.20f));
                unit.shield += shieldAmt;
                DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageBonus, 1.15f, sourceId: "determination");
                return true;
            }
            return false;
        }

        public enum RacialFeatAvailability { None, Ready, OnCooldown, Used }

        public readonly struct RacialFeatInfo
        {
            public readonly string id;
            public readonly string name;
            public readonly string description;
            public readonly RacialFeatAvailability availability;
            public readonly int cooldownRemaining;

            public RacialFeatInfo(string id, string name, string description, RacialFeatAvailability availability, int cooldownRemaining)
            {
                this.id = id;
                this.name = name;
                this.description = description;
                this.availability = availability;
                this.cooldownRemaining = cooldownRemaining;
            }
        }

        public bool HasActiveRacialFeat
        {
            get
            {
                var id = unit?.race?.id;
                return id == "elf" || id == "dwarf" || id == "dragonborn" || id == "liichtian" || id == "merfolk";
            }
        }

        public RacialFeatInfo RacialFeatState
        {
            get
            {
                var raceId = unit?.race?.id;
                var feat = unit?.race?.racialFeat;
                if (feat == null || !HasActiveRacialFeat)
                    return new RacialFeatInfo(feat?.id ?? "", feat?.name ?? "", feat?.description ?? "", RacialFeatAvailability.None, 0);

                return raceId switch
                {
                    "elf" => new RacialFeatInfo(feat.id, feat.name, feat.description,
                        FeyStepCooldown > 0 ? RacialFeatAvailability.OnCooldown : RacialFeatAvailability.Ready, FeyStepCooldown),
                    "dwarf" => new RacialFeatInfo(feat.id, feat.name, feat.description,
                        IronResolveCooldown > 0 ? RacialFeatAvailability.OnCooldown : RacialFeatAvailability.Ready, IronResolveCooldown),
                    "dragonborn" => new RacialFeatInfo(feat.id, feat.name, feat.description,
                        UsedDraconicOverdrive ? RacialFeatAvailability.Used : RacialFeatAvailability.Ready, 0),
                    "liichtian" => new RacialFeatInfo(feat.id, feat.name, feat.description,
                        UsedLightbound ? RacialFeatAvailability.Used : RacialFeatAvailability.Ready, 0),
                    "merfolk" => new RacialFeatInfo(feat.id, feat.name, feat.description,
                        OceansEmbraceCooldown > 0 ? RacialFeatAvailability.OnCooldown : RacialFeatAvailability.Ready, OceansEmbraceCooldown),
                    _ => new RacialFeatInfo(feat.id, feat.name, feat.description, RacialFeatAvailability.None, 0),
                };
            }
        }

        public bool CanUseRacialFeat(out string reason)
        {
            reason = null;
            if (!Alive) { reason = "Actor is down."; return false; }
            if (!HasActiveRacialFeat) { reason = "No active racial feat."; return false; }
            var st = RacialFeatState;
            switch (st.availability)
            {
                case RacialFeatAvailability.Ready:
                    return true;
                case RacialFeatAvailability.OnCooldown:
                    reason = $"On cooldown ({st.cooldownRemaining}t).";
                    return false;
                case RacialFeatAvailability.Used:
                    reason = "Already used this battle (1/battle).";
                    return false;
                default:
                    reason = "Unavailable.";
                    return false;
            }
        }

        // Elf Racial Feat — Fey Step: Phased (cannot be targeted by single-target attacks); +20% damage next attack (CD 4 -> 4 blocked owner turns)
        public bool ActivateFeyStep()
        {
            if (!Alive || FeyStepCooldown > 0) return false;
            IsPhased = true;
            FeyStepCooldown = 5; // N+1 because Battle.StartTurn decrements at start of owner's next turn
            FeyStepBonusPending = true;
            return true;
        }

        // Dwarf Racial Feat — Iron Resolve: All Damage Taken -30%, Break Power received -50% until next turn (CD 3 -> 3 blocked owner turns)
        public bool ActivateIronResolve()
        {
            if (!Alive || IronResolveCooldown > 0) return false;
            IronResolveActive = true;
            IronResolveCooldown = 4; // N+1 because Battle.StartTurn decrements at start of owner's next turn
            DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 0.70f, sourceId: "iron_resolve");
            return true;
        }

        // Halfling Racial Feat — Second Chance
        public bool CanUseSecondChanceRetry => Alive && SecondChanceRetriesRemaining > 0 && (unit?.race?.racialFeat?.canRetryFailedQte ?? false);

        public bool ConsumeSecondChanceRetry()
        {
            if (!CanUseSecondChanceRetry) return false;
            SecondChanceRetriesRemaining--;
            return true;
        }

        // Dragonborn Racial Feat — Draconic Overdrive: All Damage +35%, Lineage Element +25%, Break Power +50%, counter mult 1.25x for 3 turns.
        // Afterward Exhausted (-20% damage, -30% break) for 2 turns.
        public Element GetDragonbornLineageElement()
        {
            string lid = !string.IsNullOrEmpty(unit?.lineageId) ? unit.lineageId : unit?.race?.lineageId;
            if (!string.IsNullOrEmpty(lid) && Database.DragonbornLineages.TryGetValue(lid, out var lin))
                return lin.element;
            return Element.Fire;
        }

        public bool ActivateDraconicOverdrive()
        {
            if (!Alive || UsedDraconicOverdrive) return false;
            UsedDraconicOverdrive = true;
            DraconicOverdriveTurns = 3;
            OverdriveExhaustPending = true;
            DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageBonus, 1.35f, sourceId: "draconic_overdrive");
            Element lin = GetDragonbornLineageElement();
            ElementType elemType = lin switch
            {
                Element.Fire => ElementType.Fire,
                Element.Ice => ElementType.Ice,
                Element.Lightning => ElementType.Lightning,
                _ => ElementType.None
            };
            if (elemType != ElementType.None)
                DamageModifiers.AddMultiplier(DamageModifierScope.ElementSpecificDamageBonus, 1.25f, elemType, "draconic_lineage");
            else
                DamageModifiers.AddMultiplier(DamageModifierScope.PhysicalDamageBonus, 1.25f, sourceId: "draconic_lineage");

            return true;
        }

        public void TriggerDraconicExhaustion()
        {
            if (!OverdriveExhaustPending) return;
            OverdriveExhaustPending = false;
            DraconicOverdriveTurns = 0;
            DamageModifiers.RemoveBySource("draconic_overdrive");
            DamageModifiers.RemoveBySource("draconic_lineage");
            DraconicExhaustedTurns = 2;
            DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageBonus, 0.80f, sourceId: "draconic_exhausted");
            AddStatus(StatusId.Exhausted, 2, 0.20f, this);
        }

        // Liichtian Racial Feat — Lightbound: Heal 10% Max HP all active party, All Damage Taken -10%, Status Resist +20% for 2 turns (1/battle)
        public bool ActivateLightbound(IEnumerable<Combatant> allies)
        {
            if (!Alive || UsedLightbound) return false;
            UsedLightbound = true;
            if (allies != null)
            {
                foreach (var a in allies.Where(c => c.Alive))
                {
                    float healMult = a.StatusController != null ? a.StatusController.GetHealingReceivedMultiplier() : 1f;
                    int healAmt = Math.Max(1, (int)Math.Round(a.Stats.maxHp * 0.10f * healMult));
                    a.unit.hp = Math.Min(a.Stats.maxHp, a.unit.hp + healAmt);
                    a.LightboundTurns = 2;
                    a.DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 0.90f, sourceId: "lightbound");
                    if (a.LightboundResistBonus <= 0f)
                    {
                        a.LightboundResistBonus = 0.20f;
                        a.StatusController.ResistanceProfile.buffResistance += 0.20f;
                    }
                }
            }
            return true;
        }

        // Merfolk Racial Feat — Ocean's Embrace: Shield 25% Max HP on target ally, +10% defensive QTE window for 3 turns; heals 10% on end/break (CD 4 -> 4 blocked owner turns)
        public bool ActivateOceansEmbrace(Combatant ally)
        {
            if (!Alive || ally == null || !ally.Alive || ally.side != side || OceansEmbraceCooldown > 0)
                return false;
            UsedOceansEmbrace = true;
            OceansEmbraceCooldown = 5; // N+1 because Battle.StartTurn decrements at start of owner's next turn
            OceansEmbraceTarget = ally;
            OceansEmbraceTurns = 3;
            int shieldAmt = Math.Max(1, (int)Math.Round(ally.Stats.maxHp * 0.25f));
            ally.unit.shield += shieldAmt;
            ally.OceansEmbraceShieldActive = true;
            return true;
        }

        public void EndOceansEmbrace(Combatant target)
        {
            if (target != null && target.OceansEmbraceShieldActive)
            {
                target.OceansEmbraceShieldActive = false;
                if (OceansEmbraceTarget == target)
                {
                    OceansEmbraceTurns = 0;
                    OceansEmbraceTarget = null;
                }
                float healMult = target.StatusController != null ? target.StatusController.GetHealingReceivedMultiplier() : 1f;
                int healAmt = Math.Max(1, (int)Math.Round(target.Stats.maxHp * 0.10f * healMult));
                target.unit.hp = Math.Min(target.Stats.maxHp, target.unit.hp + healAmt);
            }
        }

        // Martial Artist Combo Flow (GDD Bab 8 §5.1 & Bab 12 §7)
        public void ApplyMartialArtistCombo(QteResult qte, bool isFinisher)
        {
            if (unit?.classDef == null || unit.classDef.id != "martial_artist") return;
            AttackedThisTurn = true;
            if (isFinisher)
            {
                CurrentCombo = 0; // Finisher consumes all combo
                return;
            }
            if (qte == QteResult.Poor)
            {
                CurrentCombo = 0; // Poor resets combo
            }
            else if (qte == QteResult.Good)
            {
                CurrentCombo = Math.Min(MaxComboNormal, CurrentCombo + 1);
            }
            else if (qte == QteResult.Perfect)
            {
                CurrentCombo = Math.Min(MaxComboNormal, CurrentCombo + 2); // Perfect adds +1 on top (+2 total)
            }
        }

        // Returns the full DerivedStats snapshot including active per-battle buffs and debuffs.
        public DerivedStats EffectiveDerived
        {
            get
            {
                var d = unit.Derived;
                d.maxHp = unit.stats.maxHp;
                d.maxMp = unit.stats.maxMp;
                d.physicalPower = Atk;
                d.rangedPower = RangedAtk;
                d.magicPower = Magic;
                d.healingPower = HealPower;
                d.physicalDefense = Def;
                d.magicDefense = MagicDef;
                d.initiative = Initiative;
                d.actionSpeed = ActionSpeed;
                d.physicalBreakPower = PhysicalBreak;
                d.magicBreakPower = MagicBreak;
                if (Has(StatusId.Evasion))
                    d.evasion = Math.Clamp((int)Math.Round(d.evasion * Get(StatusId.Evasion).magnitude), 0, 75);
                return d;
            }
        }

        // Reduces the Break Gauge (enemy) or Poise (player). Returns true if this hit caused the Broken state.
        public bool ApplyBreakDamage(int rawBreakPower, Combatant source, out int gaugeReduced)
        {
            gaugeReduced = 0;
            if (!Alive || !HasBreakGauge || IsBroken || rawBreakPower <= 0) return false;

            EnemyAffixSystem.ModifyBreakPower(source, ref rawBreakPower);

            if (IronResolveActive)
                rawBreakPower = Math.Max(1, (int)Math.Round(rawBreakPower * 0.5f)); // Dwarf Iron Resolve: Break Power received -50%

            // Players: StaggerRes is a % reduction (flat would floor every enemy hit to 1). Enemies: flat BreakResistance.
            int netBreak = IsPlayer
                ? Math.Max(1, (int)Math.Round(rawBreakPower * 100f / (100f + Math.Max(0, StaggerRes))))
                : Math.Max(1, rawBreakPower - Math.Max(0, BreakResistance));
            int before = CurrentBreakGauge;
            CurrentBreakGauge = Math.Max(0, CurrentBreakGauge - netBreak);
            gaugeReduced = before - CurrentBreakGauge;

            if (CurrentBreakGauge == 0)
            {
                Intent = Charge = null; // Broken interrupts a charge (Bab 12 §6.5) and any planned action
                AddStatus(StatusId.Broken, BreakDuration, BreakDamageMultiplier, source);
                CurrentCombo = 0; // Broken resets combo
                if (BreakStunTurns > 0)
                    stunTurns = Math.Max(stunTurns, BreakStunTurns);
                return true;
            }
            return false;
        }

        public void RecoverBreakGauge()
        {
            statuses.RemoveAll(s => s.id == StatusId.Broken);
            StatusController.RemoveStatus(StatusId.Broken);
            if (HasBreakGauge)
                CurrentBreakGauge = MaxBreakGauge;
        }

        static int Scale(int value, float factor) => (int)Math.Round(value * Math.Max(0f, factor));
        float Mag(StatusId id) => Get(id)?.magnitude ?? 0f;

        public StatusEffect Get(StatusId id) => statuses.Find(x => x.id == id);

        public bool Has(StatusId id) => statuses.Any(s => s.id == id) || StatusController.HasStatus(id);
        public int Cooldown(string skillId) => cooldowns.TryGetValue(skillId, out var n) ? n : 0;

        // Reapplying refreshes: the longer duration and the stronger magnitude win. No stacking.
        public void AddStatus(StatusId id, int turns, float magnitude, Combatant source)
        {
            if (StatusInfo.IsBuff(id) && StatusController != null && !StatusController.CanGainBuffs())
                return;

            var s = statuses.Find(x => x.id == id);
            if (s == null) statuses.Add(new StatusEffect { id = id, turns = turns, magnitude = magnitude, source = source });
            else
            {
                s.turns = Math.Max(s.turns, turns);
                s.magnitude = Math.Max(s.magnitude, magnitude);
                s.source = source;
            }

            // Also register in StatusController if not already tracked there
            if (!StatusController.HasStatus(id))
            {
                StatusController.TryApplyStatus(new StatusApply { id = id, turns = turns, magnitude = magnitude, chance = 1f }, source, this, out _);
            }
        }

        public string StatusTags => (guarding ? "GD" : "") + string.Concat(statuses.Select(s => StatusInfo.Tag(s.id)));
        public bool HasDebuff => statuses.Any(s => !StatusInfo.IsBuff(s.id)) || StatusController.ActiveInstances.Any(i => !StatusInfo.IsBuff(i.statusId));
    }

    public static class TurnOrder
    {
        // Highest Initiative/Speed first; ties go party before enemies, then by formation slot. No randomness.
        // Multi-turn enemies (TurnsPerRound > 1) emit one entry per turn, sorted by TurnSpeed(turnIndex).
        public static List<Combatant> Build(IEnumerable<Combatant> combatants) =>
            combatants.Where(c => c.Alive)
                .SelectMany(c => Enumerable.Range(0, c.TurnsPerRound).Select(ti => (c, ti, spd: c.TurnSpeed(ti))))
                .OrderByDescending(x => x.spd)
                .ThenBy(x => x.c.side)
                .ThenBy(x => x.c.slot)
                .ThenBy(x => x.ti)
                .Select(x => x.c)
                .ToList();
    }
}
