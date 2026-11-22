using System;
using System.Collections.Generic;

namespace Obscure
{
    // Data-declared enemy AI profile (Spec §6.2, Architecture §12 v1.1, §14.1, §18).
    public sealed class AiProfile
    {
        // posture-B starting values (untested). Pass test: Role identity >= 60% of turns in simulation.
        // If it fails: retune per role.
        public const float SupportHealWeight = 8f;
        public const float ExecutionerLowHpWeight = 6f;
        public const float DisruptorThreatWeight = 6f;
        public const float ControllerAoEPerExtraTarget = 2f;
        public const float BreakBruiserBreakWeight = 6f;

        // posture-B starting values (untested) for remaining Spec §6.2 role identities.
        // Pass test: Role identity >= 60% of turns (sim). If it fails: retune per role.
        public const float AttritionDrainWeight = 5f;
        public const float MultiHitRoleWeight = 4f;
        public const float MagicRoleWeight = 4f;
        public const float DefensivePunisherChargeWeight = 5f;
        public const float RangedOpportunistLowDefWeight = 4f;
        public const float VanguardPressureWeight = 3f;

        // posture-B starting values (untested).
        // Pass test: behavior influence fades ~3 rounds after stopping (boss ~5). If it fails: adjust decay.
        public const float StandardDecayPerRound = 0.70f;
        public const float BossDecayPerRound = 0.80f;

        // posture-B starting values (untested).
        // Pass test: 1 action never recognizes; 3 recent consistent actions do. If it fails: adjust k / RecognizeAt.
        public const float ConfidenceK = 2.0f;
        public const float StandardRecognizeAt = 0.50f;
        public const float BossRecognizeAt = 0.45f;

        public AiRole role;
        public AiTier tier;
        public float memoryDecay;
        public float recognizeAt;
        public bool usesPrediction;
        public bool usesStrategicObjectives;
        public bool usesSynergySetup;
        public bool usesConditionList;

        // Fixed planning order group (Spec §4.5, Architecture §13.2): 0 = Support, 1 = Controller, 2 = all other roles.
        public static int PlanningGroup(AiRole role) => role switch
        {
            AiRole.Support => 0,
            AiRole.AreaController => 1,
            AiRole.AdaptiveController => 1,
            _ => 2,
        };

        public static AiProfile For(EnemyDef def)
        {
            if (def == null)
                return Build(AiRole.Vanguard, AiTier.Normal);
            return Build(def.aiRole, def.aiTier);
        }

        public static AiProfile Build(AiRole role, AiTier tier)
        {
            bool isBoss = tier == AiTier.Boss;
            bool elitePlus = tier == AiTier.Elite || tier == AiTier.Miniboss || tier == AiTier.Boss;
            bool minibossPlus = tier == AiTier.Miniboss || tier == AiTier.Boss;
            return new AiProfile
            {
                role = role,
                tier = tier,
                memoryDecay = isBoss ? BossDecayPerRound : StandardDecayPerRound,
                recognizeAt = isBoss ? BossRecognizeAt : StandardRecognizeAt,
                usesPrediction = elitePlus,
                usesStrategicObjectives = elitePlus,
                usesSynergySetup = elitePlus,
                usesConditionList = minibossPlus,
            };
        }

        // Evaluates whether a chosen (move, target) aligns with the enemy's tactical role identity (for Spec §17 / Handoff Stage 1b gate).
        public static bool IsRoleAligned(Combatant self, EnemyAttack move, Combatant target, IReadOnlyList<Combatant> foes, Combatant hurtAlly)
        {
            if (self?.enemy == null || move == null) return false;
            switch (self.enemy.aiRole)
            {
                case AiRole.Support:
                    if (move.Heals || move.restoreMp > 0f) return true;
                    // If no heal is currently ready/affordable (e.g. Mending Litany on 3-round cooldown), attacking is role-aligned fallback.
                    bool healReady = self.enemy.attacks != null && Array.Exists(self.enemy.attacks,
                        a => a.Heals && self.Cooldown(Combatant.MoveKey(a)) == 0 && self.unit.mp >= a.mpCost);
                    return !healReady || hurtAlly == null || (float)hurtAlly.unit.hp / Math.Max(1, hurtAlly.Stats.maxHp) > 0.65f;
                case AiRole.Executioner:
                    if (move.restoreMp > 0f) return true;
                    if (target == null || foes == null || foes.Count == 0) return true;
                    int minHp = int.MaxValue;
                    for (int i = 0; i < foes.Count; i++) if (foes[i].unit.hp < minHp) minHp = foes[i].unit.hp;
                    return target.unit.hp <= minHp || (float)target.unit.hp / Math.Max(1, target.Stats.maxHp) <= 0.50f;
                case AiRole.Disruptor:
                    if (move.apply != null && move.apply.Length > 0) return true;
                    if (target == null || foes == null || foes.Count == 0) return true;
                    int maxOff = 0;
                    for (int i = 0; i < foes.Count; i++)
                    {
                        int off = Math.Max(foes[i].Atk, foes[i].Magic);
                        if (off > maxOff) maxOff = off;
                    }
                    return Math.Max(target.Atk, target.Magic) >= maxOff;
                case AiRole.AreaController:
                    return move.allTargets || move.UsesMagic || (move.apply != null && move.apply.Length > 0);
                case AiRole.AdaptiveController:
                    return move.telegraph || move.allTargets || move.Summons || move.restoreMp > 0f
                        || move.UsesMagic || (move.apply != null && move.apply.Length > 0);
                case AiRole.BreakBruiser:
                    return move.breakPower > 0 || move.IsBasicAttack;
                case AiRole.DefensivePunisher:
                    return move.telegraph || move.HasHeavy || move.restoreMp > 0f || move.IsBasicAttack;
                case AiRole.RangedOpportunist:
                    if (target == null || foes == null || foes.Count == 0) return true;
                    int minDef = int.MaxValue;
                    for (int i = 0; i < foes.Count; i++) if (foes[i].Def < minDef) minDef = foes[i].Def;
                    return target.Def <= minDef;
                case AiRole.ElementalAttacker:
                case AiRole.ArcaneCaster:
                    return move.UsesMagic || move.restoreMp > 0f || (self.unit.mp < 8 && move.IsBasicAttack);
                case AiRole.Attrition:
                    return move.drain > 0f || (self.unit.mp < 7 && move.IsBasicAttack);
                case AiRole.ComboAttacker:
                case AiRole.Skirmisher:
                    return move.hits >= 2 || (self.unit.mp < 18 && move.IsBasicAttack);
                case AiRole.Vanguard:
                case AiRole.Bruiser:
                default:
                    return move.hits > 0 && !move.self;
            }
        }
    }
}
