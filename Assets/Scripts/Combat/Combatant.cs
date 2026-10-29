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

        // Racial Feat per-battle state (Character Creation Section 4)
        public int SecondChanceRetriesRemaining;
        public bool UsedDraconicOverdrive;
        public bool OverdriveExhaustPending;
        public bool UsedOceansEmbrace;

        public Combatant(Unit unit, Side side, int slot, string name, string tag, EnemyDef enemy = null)
        {
            this.unit = unit;
            this.side = side;
            this.slot = slot;
            this.name = name;
            this.tag = tag;
            this.enemy = enemy;

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
        public int Atk => Scale(unit.stats.atk + LowHpPhysicalPowerBonus, 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown) - Mag(StatusId.Exhausted));
        public int RangedAtk => Scale(unit.Derived.rangedPower, 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown) - Mag(StatusId.Exhausted));
        public int Magic => Scale(unit.stats.mag, 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown) - Mag(StatusId.Exhausted));
        public int HealPower => Scale(unit.Derived.healingPower > 0 ? unit.Derived.healingPower : unit.stats.mag, 1 + Mag(StatusId.AttackUp) - Mag(StatusId.AttackDown));
        public int SupportPower => unit.Derived.supportPower;

        // Physical Defense and Magic Defense are separate (PRD Section 8.8).
        public int Def => Scale(unit.stats.def, 1 + Mag(StatusId.DefenseUp) - Mag(StatusId.DefenseDown));
        public int MagicDef => Scale(
            unit.Derived.magicDefense > 0 || !IsEnemy ? unit.Derived.magicDefense : unit.stats.def,
            1 + Mag(StatusId.MagicDefenseUp) - Mag(StatusId.MagicDefenseDown));

        public int Speed => Scale(unit.stats.spd, 1 - Mag(StatusId.Slow));
        public int Initiative => Speed;
        public int ActionSpeed => Scale(unit.Derived.actionSpeed, 1 - Mag(StatusId.Slow));

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
            return Math.Clamp(crit, 0, 100);
        }

        // Wizard Starter Passive — Arcane Affinity: reduces the MP cost of Arcane Spells.
        public int GetEffectiveMpCost(SkillDef skill)
        {
            if (skill == null) return 0;
            int cost = skill.mpCost;
            if (skill.HasTag("Arcane") && unit?.passives != null)
            {
                foreach (var p in unit.passives)
                    if (p != null && p.arcaneMpCostReduction != 0)
                        cost -= p.arcaneMpCostReduction;
            }
            return Math.Max(0, cost);
        }

        // Halfling Racial Feat — Second Chance
        public bool CanUseSecondChanceRetry => Alive && SecondChanceRetriesRemaining > 0;

        public bool ConsumeSecondChanceRetry()
        {
            if (!CanUseSecondChanceRetry) return false;
            SecondChanceRetriesRemaining--;
            return true;
        }

        // Dragonborn Racial Feat — Draconic Overdrive: temporarily increases power with an Exhausted consequence.
        public bool ActivateDraconicOverdrive()
        {
            var feat = unit?.race?.racialFeat;
            if (!Alive || feat == null || feat.temporaryPowerMultiplier <= 0f || UsedDraconicOverdrive)
                return false;
            UsedDraconicOverdrive = true;
            OverdriveExhaustPending = feat.appliesExhaustedAfterward;
            AddStatus(StatusId.AttackUp, Math.Max(1, feat.overdriveDurationTurns), feat.temporaryPowerMultiplier, this);
            return true;
        }

        public void TriggerDraconicExhaustion()
        {
            if (!OverdriveExhaustPending) return;
            OverdriveExhaustPending = false;
            AddStatus(StatusId.Exhausted, 2, 0.25f, this);
        }

        // Merfolk Racial Feat — Ocean's Embrace: provides protection/shield effect to an ally.
        public bool ActivateOceansEmbrace(Combatant ally)
        {
            var feat = unit?.race?.racialFeat;
            if (!Alive || feat == null || feat.allyShieldAmount <= 0 || UsedOceansEmbrace)
                return false;
            if (ally == null || !ally.Alive || ally.side != side)
                return false;
            UsedOceansEmbrace = true;
            ally.unit.shield += feat.allyShieldAmount;
            ally.AddStatus(StatusId.DefenseUp, 2, 0.20f, this);
            return true;
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

            int resist = Math.Max(0, BreakResistance);
            int netBreak = Math.Max(1, rawBreakPower - resist);
            int before = CurrentBreakGauge;
            CurrentBreakGauge = Math.Max(0, CurrentBreakGauge - netBreak);
            gaugeReduced = before - CurrentBreakGauge;

            if (CurrentBreakGauge == 0)
            {
                AddStatus(StatusId.Broken, BreakDuration, BreakDamageMultiplier, source);
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

        public StatusEffect Get(StatusId id) => statuses.Find(s => s.id == id);
        public bool Has(StatusId id) => Get(id) != null;
        public int Cooldown(string skillId) => cooldowns.TryGetValue(skillId, out var n) ? n : 0;

        // Reapplying refreshes: the longer duration and the stronger magnitude win. No stacking.
        public void AddStatus(StatusId id, int turns, float magnitude, Combatant source)
        {
            var s = Get(id);
            if (s == null) statuses.Add(new StatusEffect { id = id, turns = turns, magnitude = magnitude, source = source });
            else
            {
                s.turns = Math.Max(s.turns, turns);
                s.magnitude = Math.Max(s.magnitude, magnitude);
                s.source = source;
            }
        }

        public string StatusTags => (guarding ? "GD" : "") + string.Concat(statuses.Select(s => StatusInfo.Tag(s.id)));
        public bool HasDebuff => statuses.Any(s => !StatusInfo.IsBuff(s.id));
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
