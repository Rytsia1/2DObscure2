using System;
using System.Collections.Generic;

namespace Obscure
{
    // Hidden Stage 2-3 Elite & Boss combat affixes.
    // Intentionally NOT exposed in AnalysisInspector, CombatAnalysisModal, HUD badges, or combat log.
    public enum EnemyAffix
    {
        Fortified,     // -10% damage taken (counters stacked offensive modifiers)
        Relentless,    // +10% damage dealt (counters -25% DR modifier cap)
        PoiseBreaker,  // +25% Break/Poise damage dealt (counters +25% Defensive QTE window)
        Resolute,      // +20% Max Break Gauge (counters rapid Break + Marked Prey loops)
        Vigor          // Restores 4% Max HP at own turn start while not Broken
    }

    // Stage-based enemy escalation & hidden Stage 2-3 Elite/Boss affixes.
    // Balances enemies against the expected Dungeon Modifier accumulation across Stages 2-3
    // without directly penalizing the player's own modifier count.
    public static class EnemyAffixSystem
    {
        static readonly EnemyAffix[] AllAffixes =
        {
            EnemyAffix.Fortified,
            EnemyAffix.Relentless,
            EnemyAffix.PoiseBreaker,
            EnemyAffix.Resolute,
            EnemyAffix.Vigor
        };

        // Stage escalation multipliers (posture-B starting values; validated via BalanceSimulation.ModifierBalanceReport).
        // Pass test: with realistic accumulated modifiers, Stage 2 Elites take ~4-6 rounds, Stage 2 Boss ~5-8 rounds,
        // and Stage 3 Zoroa ~10-14 rounds (Good QTE wins cleanly; Poor QTE drops 1+ heroes).
        // If it fails: adjust HP multipliers first, then ATK multipliers.
        public const float Stage2HpMult = 1.18f;      // posture-B starting value
        public const float Stage2AtkMult = 1.10f;     // posture-B starting value
        public const int Stage2BreakPowerBonus = 2;   // posture-B starting value
        public const float Stage3HpMult = 1.35f;      // posture-B starting value
        public const float Stage3AtkMult = 1.18f;     // posture-B starting value
        public const int Stage3BreakPowerBonus = 4;   // posture-B starting value

        // Hidden affix magnitudes (posture-B starting values).
        public const float FortifiedDamageTakenMult = 0.90f;   // -10% damage taken
        public const float RelentlessDamageDealtMult = 1.10f;  // +10% damage dealt
        public const float PoiseBreakerMult = 1.25f;           // +25% Break/Poise damage dealt
        public const float ResoluteBreakGaugeMult = 1.20f;     // +20% Max Break Gauge
        public const float VigorTurnStartHealFraction = 0.04f; // +4% Max HP on own turn start while not Broken

        public const string FortifiedModifierSourceId = "enemy_affix_fortified";

        public static (float hpMult, float atkMult, int breakPowerBonus) GetStageMultipliers(int stage)
        {
            if (stage <= 1) return (1f, 1f, 0);
            if (stage == 2) return (Stage2HpMult, Stage2AtkMult, Stage2BreakPowerBonus);
            return (Stage3HpMult, Stage3AtkMult, Stage3BreakPowerBonus);
        }

        public static int AffixCountFor(Tier tier, int stage, bool isSummon)
        {
            if (stage <= 1 || isSummon || tier == Tier.Normal) return 0;
            if (tier == Tier.Boss) return stage >= 3 ? 2 : 1;
            return 1; // Tier.Elite on Stage 2+
        }

        // Deterministic roll using a derived RNG so Battle.Rng is never disturbed.
        public static List<EnemyAffix> RollAffixes(EnemyDef def, int stage, int slot, int seed, bool isSummon)
        {
            var result = new List<EnemyAffix>();
            if (def == null) return result;
            int count = AffixCountFor(def.tier, stage, isSummon);
            if (count <= 0) return result;

            int idHash = 17;
            if (!string.IsNullOrEmpty(def.id))
                foreach (char ch in def.id) idHash = unchecked(idHash * 31 + ch);

            int derivedSeed = unchecked(seed * 486187739 ^ stage * 1000003 ^ slot * 97 ^ idHash);
            var rng = new Random(derivedSeed);

            var pool = new List<EnemyAffix>(AllAffixes);
            // Skip Resolute if the enemy has no Break Gauge so the affix is never wasted.
            if (def.maxBreakGauge <= 0)
                pool.Remove(EnemyAffix.Resolute);

            int target = Math.Min(count, pool.Count);
            for (int i = 0; i < target; i++)
            {
                int pick = rng.Next(pool.Count);
                result.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            return result;
        }

        // Applies passive start-of-battle affix effects (Fortified on DamageModifiers + Resolute on Break Gauge).
        public static void ApplyInitialModifiers(Combatant c, bool resetCurrentBreakGauge = true)
        {
            if (c == null || !c.IsEnemy || c.EnemyAffixes.Count == 0) return;

            if (c.HasAffix(EnemyAffix.Fortified))
            {
                c.DamageModifiers.RemoveBySource(FortifiedModifierSourceId);
                c.DamageModifiers.AddMultiplier(DamageModifierScope.AllDamageTaken, FortifiedDamageTakenMult, sourceId: FortifiedModifierSourceId);
            }

            if (c.HasAffix(EnemyAffix.Resolute) && c.MaxBreakGauge > 0)
            {
                int baseMax = c.enemy != null ? Math.Max(0, c.enemy.maxBreakGauge) : c.MaxBreakGauge;
                int boosted = Math.Max(baseMax + 1, (int)Math.Round(baseMax * ResoluteBreakGaugeMult));
                c.MaxBreakGauge = boosted;
                if (resetCurrentBreakGauge)
                    c.CurrentBreakGauge = boosted;
                else
                    c.CurrentBreakGauge = Math.Min(c.CurrentBreakGauge, boosted);
            }
        }

        public static void ModifyDamageDealt(Combatant attacker, ref int amount)
        {
            if (attacker == null || !attacker.IsEnemy || amount <= 0) return;
            if (attacker.HasAffix(EnemyAffix.Relentless))
                amount = Math.Max(1, (int)Math.Round(amount * RelentlessDamageDealtMult));
        }

        // Legacy Battle.Deal path only; DamageCalculator already applies Fortified via Combatant.DamageModifiers.
        public static void ModifyDamageReceivedLegacy(Combatant target, ref int amount)
        {
            if (target == null || !target.IsEnemy || amount <= 0) return;
            if (target.HasAffix(EnemyAffix.Fortified))
                amount = Math.Max(1, (int)Math.Round(amount * FortifiedDamageTakenMult));
        }

        public static void ModifyBreakPower(Combatant source, ref int rawBreakPower)
        {
            if (source == null || !source.IsEnemy || rawBreakPower <= 0) return;
            if (source.HasAffix(EnemyAffix.PoiseBreaker))
                rawBreakPower = Math.Max(1, (int)Math.Round(rawBreakPower * PoiseBreakerMult));
        }

        // Silent turn-start heal while not Broken (once per round, never twice on double-turn bosses; no combat log output).
        public static void OnTurnStart(Combatant c)
        {
            if (c == null || !c.IsEnemy || !c.Alive || c.IsBroken || c.turnsTakenThisRound > 1) return;
            if (!c.HasAffix(EnemyAffix.Vigor)) return;
            int heal = Math.Max(1, (int)Math.Round(c.Stats.maxHp * VigorTurnStartHealFraction));
            c.unit.Heal(heal);
        }
    }
}
