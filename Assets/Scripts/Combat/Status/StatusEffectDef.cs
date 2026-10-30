using System;
using System.Collections.Generic;

namespace Obscure
{
    // Reusable status effect definition (Prompt Section 4 & GDD Sections 8, 10, 11, 12).
    [Serializable]
    public class StatusEffectDef
    {
        public StatusId id;
        public string name;
        public string tag;
        public string description;
        public StatusCategory category;
        public StatusStackingRule stackingRule = StatusStackingRule.Independent;
        public StatusTickTiming tickTiming = StatusTickTiming.TurnStart;
        public int defaultDuration = 2;
        public float defaultMagnitude = 1f;

        // DoT configuration (GDD Section 10 & 11)
        public bool isDoT;
        public DamageType dotDamageType = DamageType.Physical;
        public ElementType dotElementType = ElementType.None;
        public Ability dotScalingStat = Ability.STR;
        public float dotSkillPower;
        public float dotScaling;

        public bool IsMental => StatusInfo.IsMental(id);
        public bool IsBuff => StatusInfo.IsBuff(id);
    }

    public static class StatusEffectDefs
    {
        private static readonly Dictionary<StatusId, StatusEffectDef> defs = new Dictionary<StatusId, StatusEffectDef>();

        static StatusEffectDefs()
        {
            Register(new StatusEffectDef
            {
                id = StatusId.Burn, name = "Burn", tag = "BN", category = StatusCategory.DamageOverTime,
                description = "Elemental Fire DoT; ticks dynamically at start of turn.",
                stackingRule = StatusStackingRule.Independent, tickTiming = StatusTickTiming.TurnStart,
                isDoT = true, dotDamageType = DamageType.Elemental, dotElementType = ElementType.Fire,
                dotScalingStat = Ability.INT, dotSkillPower = 15f, dotScaling = 1.0f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Poison, name = "Poison", tag = "PS", category = StatusCategory.DamageOverTime,
                description = "Poison DoT; ticks dynamically at start of turn.",
                stackingRule = StatusStackingRule.Independent, tickTiming = StatusTickTiming.TurnStart,
                isDoT = true, dotDamageType = DamageType.Elemental, dotElementType = ElementType.Shadow,
                dotScalingStat = Ability.INT, dotSkillPower = 12f, dotScaling = 0.8f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Bleed, name = "Bleed", tag = "BL", category = StatusCategory.DamageOverTime,
                description = "Physical DoT; ticks dynamically at start of turn.",
                stackingRule = StatusStackingRule.Independent, tickTiming = StatusTickTiming.TurnStart,
                isDoT = true, dotDamageType = DamageType.Physical, dotElementType = ElementType.None,
                dotScalingStat = Ability.STR, dotSkillPower = 15f, dotScaling = 1.0f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Freeze, name = "Freeze", tag = "FZ", category = StatusCategory.CrowdControl,
                description = "Reduces target speed, delaying its turn order.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.35f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Shock, name = "Shock", tag = "SK", category = StatusCategory.CrowdControl,
                description = "Target has a chance to fail performing an action.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.30f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Curse, name = "Curse", tag = "CU", category = StatusCategory.Debuff,
                description = "Debilitates target combat capabilities.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.25f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Stun, name = "Stun", tag = "ST", category = StatusCategory.CrowdControl,
                description = "Completely prevents target from performing any action while active.",
                stackingRule = StatusStackingRule.RefreshDuration, defaultDuration = 1
            });

            Register(new StatusEffectDef
            {
                id = StatusId.ResistanceDown, name = "Resistance Down", tag = "RD", category = StatusCategory.Debuff,
                description = "Reduces Universal Status Effect Resistance by a percentage.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.20f
            });

            // Mental Effects (GDD Section 12)
            Register(new StatusEffectDef
            {
                id = StatusId.MindControl, name = "Mind Control", tag = "MC", category = StatusCategory.MentalEffect,
                description = "Target loses control; controller dictates target's action.",
                stackingRule = StatusStackingRule.RefreshDuration, defaultDuration = 1
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Confusion, name = "Confusion", tag = "CF", category = StatusCategory.MentalEffect,
                description = "Target still acts, but has a chance to perform the wrong action or target.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.50f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Fear, name = "Fear", tag = "FR", category = StatusCategory.MentalEffect,
                description = "Combat debuff: target still acts, but its damage dealt is reduced.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.30f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Despair, name = "Despair", tag = "DP", category = StatusCategory.MentalEffect,
                description = "Action disruption: target has a chance to fail performing an action.",
                stackingRule = StatusStackingRule.Independent, defaultMagnitude = 0.35f
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Charm, name = "Charm", tag = "CH", category = StatusCategory.MentalEffect,
                description = "Target cannot attack the charmer, but can perform other valid actions.",
                stackingRule = StatusStackingRule.RefreshDuration, defaultDuration = 2
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Sleep, name = "Sleep", tag = "SLP", category = StatusCategory.MentalEffect,
                description = "Target cannot act. Breaks immediately upon receiving direct damage or DoT.",
                stackingRule = StatusStackingRule.RefreshDuration, defaultDuration = 2
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Silence, name = "Silence", tag = "SIL", category = StatusCategory.MentalEffect,
                description = "Target cannot use Skills; Basic Attack, Items, and Guard remain usable.",
                stackingRule = StatusStackingRule.RefreshDuration, defaultDuration = 2
            });

            Register(new StatusEffectDef
            {
                id = StatusId.Madness, name = "Madness", tag = "MD", category = StatusCategory.MentalEffect,
                description = "Target chooses a random valid action and target; can attack own party.",
                stackingRule = StatusStackingRule.RefreshDuration, defaultDuration = 2
            });

            // Legacy Buffs & Debuffs
            Register(new StatusEffectDef { id = StatusId.DefenseUp, name = "Defense Up", tag = "D+", category = StatusCategory.Buff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.DefenseDown, name = "Defense Down", tag = "D-", category = StatusCategory.Debuff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.AttackUp, name = "Attack Up", tag = "A+", category = StatusCategory.Buff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.AttackDown, name = "Attack Down", tag = "A-", category = StatusCategory.Debuff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.Slow, name = "Slow", tag = "SL", category = StatusCategory.Debuff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.Guardian, name = "Guardian", tag = "GU", category = StatusCategory.Buff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.Evasion, name = "Evasion", tag = "EV", category = StatusCategory.Buff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.MagicDefenseUp, name = "Magic Defense Up", tag = "M+", category = StatusCategory.Buff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.MagicDefenseDown, name = "Magic Defense Down", tag = "M-", category = StatusCategory.Debuff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.Broken, name = "Broken", tag = "BK", category = StatusCategory.Debuff, stackingRule = StatusStackingRule.RefreshDuration });
            Register(new StatusEffectDef { id = StatusId.Exhausted, name = "Exhausted", tag = "EX", category = StatusCategory.Debuff, stackingRule = StatusStackingRule.RefreshDuration });
        }

        public static void Register(StatusEffectDef def)
        {
            if (def == null) return;
            defs[def.id] = def;
        }

        public static StatusEffectDef Get(StatusId id)
        {
            if (defs.TryGetValue(id, out var def)) return def;
            return new StatusEffectDef
            {
                id = id,
                name = StatusInfo.Name(id),
                tag = StatusInfo.Tag(id),
                category = StatusInfo.IsMental(id) ? StatusCategory.MentalEffect : (StatusInfo.IsBuff(id) ? StatusCategory.Buff : StatusCategory.Debuff)
            };
        }
    }
}

