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
        public readonly Unit unit;
        public readonly Side side;
        public readonly int slot;          // formation index, also the turn-order tiebreaker
        public readonly string name, tag;  // log name ("Skeleton B"), turn-bar tag ("SKB")
        public readonly EnemyDef enemy;    // null for party members

        public bool guarding;   // halves incoming damage until this combatant's next turn starts
        public int stunTurns;   // turns to skip; the skip consumes the turn
        public int phase = 1;   // boss phase
        public readonly List<StatusEffect> statuses = new List<StatusEffect>();
        public readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>();

        // Break/Stagger System state (PRD Section 9)
        public int MaxBreakGauge;
        public int CurrentBreakGauge;
        public int BreakResistance;
        public int BreakDuration = 1;
        public int BreakStunTurns = 1;
        public float BreakDamageMultiplier = 1.5f;

        // Racial Feat per-battle state (GDD Bab 6 & Bab 12)
        public bool DeterminationUsed;
        public int DeterminationTurns;
        public int FeyStepCooldown;
        public bool IsPhased;
        public bool FeyStepBonusPending;
        public int IronResolveCooldown;
        public bool IronResolveActive;
        public int SecondChanceRetriesRemaining = 2;
        public bool UsedDraconicOverdrive;
        public int DraconicOverdriveTurns;
        public bool OverdriveExhaustPending;
        public int DraconicExhaustedTurns;
        public bool UsedLightbound;
        public int LightboundTurns;
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
        public readonly EnemyAffinityProfile AffinityProfile;
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
                StatusController.ResistanceProfile.statsResistance = Math.Clamp(unit.Derived.statusResistance / 100f, 0f, 0.75f);
            if (enemy != null && enemy.tier == Tier.Boss)
                StatusController.ResistanceProfile.baseResistance = 0.50f; // High base resistance for Bosses (GDD Section 13)

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
            }
        }

        public bool Alive => unit.Alive;
        public bool IsEnemy => side == Side.Enemy;
        public bool IsPlayer => side == Side.Party;
        public Stats Stats => unit.stats;
        public DerivedStats BaseDerived => unit.Derived;
        public Affinities Affinities => unit.affinities;

        public bool HasBreakGauge => MaxBreakGauge > 0;
        public bool IsBroken => Has(StatusId.Broken);

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
        public int HealPower
        {
            get
            {
                float bgBonus = 0f;
                if (unit?.background != null)
                {
                    var rank = unit.background.GetRank(unit.level);
                    if (rank != null) bgBonus = rank.healingDealtBonus;
                }
                return Scale(
                    unit.Derived.healingPower > 0 ? unit.Derived.healingPower : unit.stats.mag,
                    (1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown)) * (1f + bgBonus));
            }
        }
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
                float bgSpeedMult = wayfarerSpeedBuffTurns > 0 ? 1.10f : 1.0f;
                return Scale(
                    unit.stats.spd,
                    (1 - Mag(StatusId.Slow) - Mag(StatusId.Freeze)) * StatusController.GetSpeedMultiplier() * bgSpeedMult);
            }
        }
        public int Initiative => Speed;
        public int ActionSpeed => Scale(unit.Derived.actionSpeed, (1 - Mag(StatusId.Slow) - Mag(StatusId.Freeze)) * StatusController.GetSpeedMultiplier());

        public int ArmorPen => unit.Derived.armorPenetration;
        public int MagicPen => unit.Derived.magicPenetration;
        public int CritChance => unit.Derived.criticalChance;
        public int CritDamage => unit.Derived.criticalDamage;
        public int PhysicalBreak => Scale(Math.Max(1, unit.Derived.physicalBreakPower), 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown));
        public int MagicBreak => Scale(Math.Max(1, unit.Derived.magicBreakPower), 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown));
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
            Func<float> criticalRollProvider = null)
        {
            var scores = unit != null ? unit.FinalScores : AbilityScores.Uniform(10);
            return new DamageContext
            {
                attackerStr = scores.str,
                attackerInt = scores.intel,
                attackerModifiers = DamageModifiers ?? new DamageModifierSet(),
                criticalHitConfig = GetCriticalHitConfig(target),
                targetPhysicalDefense = target != null ? (unit != null && unit.HasTalent("piercer") ? (float)Math.Max(0.0, target.Def * (1f - TalentSystem.PiercerPenetration)) : target.Def) : 0f,
                targetModifiers = target?.DamageModifiers ?? new DamageModifierSet(),
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
        public int GetEffectiveMpCost(SkillDef skill)
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
            return Math.Max(0, cost);
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

        // Elf Racial Feat — Fey Step: Phased (cannot be targeted by single-target attacks); +20% damage next attack (CD 4)
        public bool ActivateFeyStep()
        {
            if (!Alive || FeyStepCooldown > 0) return false;
            IsPhased = true;
            FeyStepCooldown = 4;
            FeyStepBonusPending = true;
            return true;
        }

        // Dwarf Racial Feat — Iron Resolve: All Damage Taken -30%, Break Power received -50% until next turn (CD 3)
        public bool ActivateIronResolve()
        {
            if (!Alive || IronResolveCooldown > 0) return false;
            IronResolveActive = true;
            IronResolveCooldown = 3;
            DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 0.70f, sourceId: "iron_resolve");
            return true;
        }

        // Halfling Racial Feat — Second Chance
        public bool CanUseSecondChanceRetry => Alive && SecondChanceRetriesRemaining > 0;

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
            if (unit?.race?.lineageId != null && Database.DragonbornLineages.TryGetValue(unit.race.lineageId, out var lin))
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

            AddStatus(StatusId.AttackUp, 3, 0.35f, this);
            return true;
        }

        public void TriggerDraconicExhaustion()
        {
            if (!OverdriveExhaustPending) return;
            OverdriveExhaustPending = false;
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
                    int healAmt = Math.Max(1, (int)Math.Round(a.Stats.maxHp * 0.10f));
                    a.unit.hp = Math.Min(a.Stats.maxHp, a.unit.hp + healAmt);
                    a.LightboundTurns = 2;
                    a.DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, 0.90f, sourceId: "lightbound");
                    a.StatusController.ResistanceProfile.buffResistance = Math.Min(0.75f, a.StatusController.ResistanceProfile.buffResistance + 0.20f);
                }
            }
            return true;
        }

        // Merfolk Racial Feat — Ocean's Embrace: Shield 25% Max HP on target ally, +10% defensive QTE window for 3 turns; heals 10% on end/break (CD 4)
        public bool ActivateOceansEmbrace(Combatant ally)
        {
            if (!Alive || ally == null || !ally.Alive || ally.side != side || OceansEmbraceCooldown > 0)
                return false;
            UsedOceansEmbrace = true;
            OceansEmbraceCooldown = 4;
            OceansEmbraceTarget = ally;
            OceansEmbraceTurns = 3;
            int shieldAmt = Math.Max(1, (int)Math.Round(ally.Stats.maxHp * 0.25f));
            ally.unit.shield += shieldAmt;
            ally.OceansEmbraceShieldActive = true;
            ally.AddStatus(StatusId.DefenseUp, 3, 0.20f, this);
            return true;
        }

        public void EndOceansEmbrace(Combatant target)
        {
            if (target != null && target.OceansEmbraceShieldActive)
            {
                target.OceansEmbraceShieldActive = false;
                int healAmt = Math.Max(1, (int)Math.Round(target.Stats.maxHp * 0.10f));
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

        // Reduces the Break Gauge on an enemy. Returns true if this hit caused the enemy to enter the Broken state.
        public bool ApplyBreakDamage(int rawBreakPower, Combatant source, out int gaugeReduced)
        {
            gaugeReduced = 0;
            if (!Alive || !HasBreakGauge || IsBroken || rawBreakPower <= 0) return false;

            if (IronResolveActive)
                rawBreakPower = Math.Max(1, (int)Math.Round(rawBreakPower * 0.5f)); // Dwarf Iron Resolve: Break Power received -50%

            int resist = Math.Max(0, BreakResistance);
            int netBreak = Math.Max(1, rawBreakPower - resist);
            int before = CurrentBreakGauge;
            CurrentBreakGauge = Math.Max(0, CurrentBreakGauge - netBreak);
            gaugeReduced = before - CurrentBreakGauge;

            if (CurrentBreakGauge == 0)
            {
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
        public static List<Combatant> Build(IEnumerable<Combatant> combatants) =>
            combatants.Where(c => c.Alive)
                .OrderByDescending(c => c.Speed)
                .ThenBy(c => c.side)
                .ThenBy(c => c.slot)
                .ToList();
    }
}
