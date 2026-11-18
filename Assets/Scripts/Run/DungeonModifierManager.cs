using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public struct DungeonModifierGroupSummary
    {
        public DungeonModifierDef def;
        public ElementType element;
        public int totalStacks;
        public int effectiveStacks;
        public int stackCap;
        public bool isAtCap;
        public int shortestRemainingDuration;
        public List<DungeonModifierInstance> instances;

        public string DisplayTitle => def.isElementSpecific && element.IsValidElement()
            ? $"{def.displayName} ({element})"
            : def.displayName;
    }

    // Manages active Dungeon Modifiers within a single dungeon layer (GDD Bab 15).
    // Temporary per dungeon run: cleared on dungeon clear (after boss reward) and run failure.
    public class DungeonModifierManager
    {
        public const float MaxAllDamageReduction = 0.25f; // Bab 15 §4.2
        public const float MaxQteWindowBonus = 0.25f;     // Bab 12 §3.4 & Bab 15 §4.2
        public const float MaxPhysicalPenetration = 0.50f;
        public const int FortuneFavorGoldPerStack = 12;
        public const float ScavengerEyeChancePerStack = 0.20f;

        private readonly List<DungeonModifierInstance> activeModifiers = new List<DungeonModifierInstance>();
        private int nextAcquiredOrder = 1;

        public IReadOnlyList<DungeonModifierInstance> ActiveModifiers => activeModifiers;
        public int Count => activeModifiers.Count;

        public bool HasModifier(string modifierId, ElementType element = ElementType.None)
        {
            if (string.IsNullOrEmpty(modifierId)) return false;
            return activeModifiers.Any(m => m.modifierId == modifierId &&
                (element == ElementType.None || m.element == element));
        }

        public int GetTotalStackCount(string modifierId, ElementType element = ElementType.None)
        {
            if (string.IsNullOrEmpty(modifierId)) return 0;
            if (!Database.DungeonModifiers.TryGetValue(modifierId, out var def)) return 0;

            return activeModifiers.Count(m =>
                m.modifierId == modifierId &&
                (!def.isElementSpecific || element == ElementType.None || m.element == element));
        }

        public int GetEffectiveStackCount(string modifierId, ElementType element = ElementType.None)
        {
            if (!Database.DungeonModifiers.TryGetValue(modifierId, out var def)) return 0;
            int raw = GetTotalStackCount(modifierId, element);
            if (def.stackCap <= 0) return raw;
            return Math.Min(raw, def.stackCap);
        }

        public bool IsAtCap(string modifierId, ElementType element = ElementType.None)
        {
            if (!Database.DungeonModifiers.TryGetValue(modifierId, out var def)) return false;
            if (def.stackCap <= 0) return false;
            return GetTotalStackCount(modifierId, element) >= def.stackCap;
        }

        public IEnumerable<ElementType> GetActiveElements(string modifierId) =>
            activeModifiers
                .Where(m => m.modifierId == modifierId && m.element.IsValidElement())
                .Select(m => m.element)
                .Distinct();

        public List<DungeonModifierGroupSummary> GetGroupSummaries()
        {
            var groups = activeModifiers
                .Where(m => m.Definition != null)
                .GroupBy(m => (m.modifierId, m.Definition.isElementSpecific ? m.element : ElementType.None))
                .Select(g =>
                {
                    var list = g.OrderBy(x => x.acquiredOrder).ToList();
                    var def = list[0].Definition;
                    var elem = g.Key.Item2;
                    int total = list.Count;
                    int eff = def.stackCap > 0 ? Math.Min(total, def.stackCap) : total;
                    return new DungeonModifierGroupSummary
                    {
                        def = def,
                        element = elem,
                        totalStacks = total,
                        effectiveStacks = eff,
                        stackCap = def.stackCap,
                        isAtCap = def.stackCap > 0 && total >= def.stackCap,
                        shortestRemainingDuration = list.Min(x => x.remainingDuration),
                        instances = list
                    };
                })
                .OrderBy(s => s.def.polarity)
                .ThenBy(s => s.def.modifierId)
                .ThenBy(s => s.element)
                .ToList();

            return groups;
        }

        // Acquires a modifier: OneShot effects apply immediately; duration-based modifiers add a new stack
        // with its own independent remaining duration (never refreshing older stacks per Bab 15 §4.1).
        public bool AddModifier(string modifierId, RunState run = null, ElementType element = ElementType.None, Random rng = null)
        {
            if (string.IsNullOrEmpty(modifierId) || !Database.DungeonModifiers.TryGetValue(modifierId, out var def))
                return false;

            if (def.IsOneShot)
            {
                ApplyOneShot(def, run);
                return true;
            }

            ElementType resolvedElement = ElementType.None;
            if (def.isElementSpecific)
            {
                if (element.IsValidElement())
                    resolvedElement = element;
                else if (!def.isPlayerElementChoice)
                    resolvedElement = ElementTypes.All[(rng ?? run?.Rng ?? new Random()).Next(ElementTypes.All.Length)];
                else
                    resolvedElement = ElementType.Fire;
            }

            var instance = new DungeonModifierInstance(def, def.duration, resolvedElement, nextAcquiredOrder++);
            activeModifiers.Add(instance);
            return true;
        }

        static void ApplyOneShot(DungeonModifierDef def, RunState run)
        {
            if (run?.Party == null) return;
            foreach (var u in run.Party)
            {
                if (u == null || !u.Alive) continue; // Bab 15 §5: does not revive Downed characters
                if (def.effectType == ModifierEffectType.ImmediateHealPercent && u.stats.maxHp > 0)
                {
                    int heal = Math.Max(1, (int)Math.Round(u.stats.maxHp * def.valuePerStack));
                    u.Heal(heal);
                }
                else if (def.effectType == ModifierEffectType.ImmediateMpRestorePercent && u.stats.maxMp > 0)
                {
                    int mpGain = Math.Max(1, (int)Math.Floor(u.stats.maxMp * def.valuePerStack));
                    u.RestoreMp(mpGain);
                }
            }
        }

        public void ClearAll()
        {
            activeModifiers.Clear();
        }

        // ---- Lifecycle Hooks ----

        public int ConsumeBattleVictoryBonusGold()
        {
            int stacks = GetEffectiveStackCount("MOD-015");
            return stacks > 0 ? stacks * FortuneFavorGoldPerStack : 0;
        }

        // Called when a battle is won: decrements Battles-based stacks independently and prunes expired ones.
        // Older stacks expire on schedule; any overflow stacks waiting behind the cap automatically take effect.
        public void OnBattleWon(RunState run = null)
        {
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                var inst = activeModifiers[i];
                var def = inst.Definition;
                if (def == null || def.durationUnit == ModifierDurationUnit.Battles)
                {
                    inst.remainingDuration--;
                    if (inst.remainingDuration <= 0)
                        activeModifiers.RemoveAt(i);
                }
            }
        }

        // Consumes MOD-009 Treasure Sense when any positive Gold reward is received.
        public void ModifyGoldReward(ref int gold)
        {
            if (gold <= 0) return;
            int stacks = GetEffectiveStackCount("MOD-009");
            if (stacks <= 0) return;

            var def = Database.DungeonModifiers["MOD-009"];
            float mult = 1f + def.valuePerStack * stacks;
            gold = Math.Max(1, (int)Math.Round(gold * mult));

            activeModifiers.RemoveAll(m => m.modifierId == "MOD-009");
        }

        // Consumes MOD-010 Scavenger's Eye when rolling a reward offer. Returns a bonus consumable id or null.
        public string TryRollScavengerBonusConsumable(Random rng)
        {
            int stacks = GetEffectiveStackCount("MOD-010");
            if (stacks <= 0) return null;

            activeModifiers.RemoveAll(m => m.modifierId == "MOD-010");

            float chance = Math.Clamp(stacks * ScavengerEyeChancePerStack, 0f, 0.75f);
            var rand = rng ?? new Random();
            if (rand.NextDouble() >= chance) return null;

            string[] pool = { "draught", "ether", "elixir" };
            return pool[rand.Next(pool.Length)];
        }

        // ---- Combat Evaluation Hooks ----

        // MOD-003 Piercing Insight: ignores 5% target Physical Defense per stack (max 15%).
        public float GetPhysicalPenetrationBonus(Combatant actor)
        {
            if (actor == null || !actor.IsPlayer) return 0f;
            int stacks = GetEffectiveStackCount("MOD-003");
            if (stacks <= 0) return 0f;
            return stacks * Database.DungeonModifiers["MOD-003"].valuePerStack;
        }

        // MOD-001, MOD-002, MOD-016, MOD-017, MOD-022, MOD-024 for the legacy Deal() combat path.
        public void ModifyDamageDealt(Combatant actor, Combatant target, Element element, ref float damageMultiplier, Battle battle = null)
        {
            if (actor == null || !actor.IsPlayer) return;

            bool isPhysical = element == Element.Physical;
            bool isElemental = !isPhysical;

            // MOD-001 Battle Focus: Physical Damage +5%/stack
            if (isPhysical)
            {
                int bf = GetEffectiveStackCount("MOD-001");
                if (bf > 0) damageMultiplier *= (1f + 0.05f * bf);
            }

            // MOD-002 Arcane Current: Elemental Damage +5%/stack
            if (isElemental)
            {
                int ac = GetEffectiveStackCount("MOD-002");
                if (ac > 0) damageMultiplier *= (1f + 0.05f * ac);
            }

            // MOD-024 Elemental Attunement: Chosen element Damage +12%/stack
            if (isElemental)
            {
                var elemType = ElementTypes.FromLegacyElement(element);
                if (elemType.IsValidElement())
                {
                    int ea = GetEffectiveStackCount("MOD-024", elemType);
                    if (ea > 0) damageMultiplier *= (1f + 0.12f * ea);
                }
            }

            // MOD-016 Blood Price: Physical & Elemental Damage +12%/stack
            int bp = GetEffectiveStackCount("MOD-016");
            if (bp > 0) damageMultiplier *= (1f + 0.12f * bp);

            // MOD-017 Glass Resolve: All Damage +15%/stack
            int gr = GetEffectiveStackCount("MOD-017");
            if (gr > 0) damageMultiplier *= (1f + 0.15f * gr);

            // MOD-022 Marked Prey: Damage vs Broken enemies +10%/stack
            if (target != null && target.IsBroken)
            {
                int mp = GetEffectiveStackCount("MOD-022");
                if (mp > 0) damageMultiplier *= (1f + 0.10f * mp);
            }
        }

        // Populates DamageModifierSet copies for the modular DamageCalculator path (avoids double-applying with Deal).
        public void PopulateDamageModifiers(Combatant actor, Combatant target, DamageModifierSet attackerMods, DamageModifierSet targetMods)
        {
            if (actor != null && actor.IsPlayer && attackerMods != null)
            {
                int bf = GetEffectiveStackCount("MOD-001");
                if (bf > 0) attackerMods.AddPercentBonus(DamageModifierScope.PhysicalDamageBonus, 0.05f * bf, sourceId: "MOD-001");

                int ac = GetEffectiveStackCount("MOD-002");
                if (ac > 0) attackerMods.AddPercentBonus(DamageModifierScope.ElementalDamageBonus, 0.05f * ac, sourceId: "MOD-002");

                foreach (var elem in GetActiveElements("MOD-024"))
                {
                    int ea = GetEffectiveStackCount("MOD-024", elem);
                    if (ea > 0) attackerMods.AddPercentBonus(DamageModifierScope.ElementSpecificDamageBonus, 0.12f * ea, elem, sourceId: $"MOD-024_{elem}");
                }

                int bp = GetEffectiveStackCount("MOD-016");
                if (bp > 0)
                {
                    attackerMods.AddPercentBonus(DamageModifierScope.PhysicalDamageBonus, 0.12f * bp, sourceId: "MOD-016_phys");
                    attackerMods.AddPercentBonus(DamageModifierScope.ElementalDamageBonus, 0.12f * bp, sourceId: "MOD-016_elem");
                }

                int gr = GetEffectiveStackCount("MOD-017");
                if (gr > 0) attackerMods.AddPercentBonus(DamageModifierScope.AllDamageBonus, 0.15f * gr, sourceId: "MOD-017");

                if (target != null && target.IsBroken)
                {
                    int mp = GetEffectiveStackCount("MOD-022");
                    if (mp > 0) attackerMods.AddPercentBonus(DamageModifierScope.AllDamageBonus, 0.10f * mp, sourceId: "MOD-022");
                }
            }
        }

        // MOD-006, MOD-013, MOD-014, MOD-017 (drawback), MOD-023.
        // Enforces Bab 15 §4.2: Total All Damage Taken reduction from all modifiers <= 25%.
        public void ModifyDamageReceived(Combatant target, Element element, ref float damageMultiplier, Battle battle = null)
        {
            if (target == null || !target.IsPlayer) return;

            float drProduct = 1f;

            // MOD-006 Inspiring Formation: -5% per stack
            int inf = GetEffectiveStackCount("MOD-006");
            if (inf > 0) drProduct *= (1f - 0.05f * inf);

            // MOD-013 Iron Guard: -8% per stack
            int ig = GetEffectiveStackCount("MOD-013");
            if (ig > 0) drProduct *= (1f - 0.08f * ig);

            // MOD-023 Last Stand: -10% per stack when only 1 party member remains alive
            if (target.Alive && battle != null && battle.Party.Count(p => p.Alive) == 1)
            {
                int ls = GetEffectiveStackCount("MOD-023");
                if (ls > 0) drProduct *= (1f - 0.10f * ls);
            }

            // Cap total DR from modifiers at 25% (multiplier >= 0.75f)
            float cappedDrMultiplier = Math.Max(1f - MaxAllDamageReduction, drProduct);
            damageMultiplier *= cappedDrMultiplier;

            // MOD-014 Elemental Ward: element-specific damage taken -15%/stack (max 2 per element)
            var elemType = ElementTypes.FromLegacyElement(element);
            if (elemType.IsValidElement())
            {
                int ew = GetEffectiveStackCount("MOD-014", elemType);
                if (ew > 0) damageMultiplier *= Math.Max(0f, 1f - 0.15f * ew);
            }

            // MOD-017 Glass Resolve drawback: All Damage Taken +10%/stack
            int gr = GetEffectiveStackCount("MOD-017");
            if (gr > 0) damageMultiplier *= (1f + 0.10f * gr);
        }

        // MOD-007 Vital Spark (+10%/stack), MOD-016 Blood Price drawback (-10%/stack), MOD-019 Withering Curse (-15%/stack).
        public float GetHealingReceivedMultiplier(Combatant target)
        {
            if (target == null || !target.IsPlayer) return 1f;
            float mult = 1f;

            int vs = GetEffectiveStackCount("MOD-007");
            if (vs > 0) mult *= (1f + 0.10f * vs);

            int bp = GetEffectiveStackCount("MOD-016");
            if (bp > 0) mult *= Math.Max(0f, 1f - 0.10f * bp);

            int wc = GetEffectiveStackCount("MOD-019");
            if (wc > 0) mult *= Math.Max(0f, 1f - 0.15f * wc);

            return mult;
        }

        // MOD-008 Mana Well (-5%/stack) & MOD-021 Leaking Mana (+1 MP/stack). Minimum 1 MP for MP-costing skills.
        public void ModifyMpCost(Combatant actor, SkillDef skill, ref int mpCost)
        {
            if (actor == null || !actor.IsPlayer || skill == null || mpCost <= 0) return;

            int mw = GetEffectiveStackCount("MOD-008");
            if (mw > 0)
            {
                decimal factor = Math.Max(0m, 1m - 0.05m * mw);
                mpCost = (int)Math.Round(mpCost * factor);
                if (mpCost < 1) mpCost = 1;
            }

            int lm = GetEffectiveStackCount("MOD-021");
            if (lm > 0)
            {
                mpCost += lm;
            }

            if (mpCost < 1) mpCost = 1;
        }

        // MOD-004 Steady Focus (+10%/stack) & MOD-018 Fractured Focus (-10%/stack).
        public float GetOffensiveQteWindowDelta()
        {
            float delta = 0f;
            int sf = GetEffectiveStackCount("MOD-004");
            if (sf > 0) delta += 0.10f * sf;

            int fc = GetEffectiveStackCount("MOD-018");
            if (fc > 0) delta -= 0.10f * fc;

            return delta;
        }

        // MOD-005 Quick Instinct (+10%/stack).
        public float GetDefensiveQteWindowDelta()
        {
            int qi = GetEffectiveStackCount("MOD-005");
            return qi > 0 ? 0.10f * qi : 0f;
        }

        // MOD-020 Heavy Steps (-5%/stack).
        public float GetSpeedPenalty()
        {
            int hs = GetEffectiveStackCount("MOD-020");
            return hs > 0 ? 0.05f * hs : 0f;
        }

        // ---- UI / Offer Formatting Helpers (Bab 15 §4.3 & §6.2) ----

        public string FormatOfferHeading(string modifierId, ElementType element = ElementType.None)
        {
            if (!Database.DungeonModifiers.TryGetValue(modifierId, out var def)) return modifierId;
            string name = def.isElementSpecific && element.IsValidElement()
                ? $"{def.displayName} ({element})"
                : def.displayName;

            if (def.IsOneShot) return name;

            int current = GetTotalStackCount(modifierId, element);
            if (def.stackCap > 0 && current >= def.stackCap)
                return $"{name} ×{current} (MAX)";

            return $"{name} ×{current} → ×{current + 1}";
        }

        public string FormatGroupTooltip(DungeonModifierGroupSummary g)
        {
            var lines = new List<string>();
            string polarity = g.def.polarity switch
            {
                ModifierPolarity.BuffWithDrawback => "BUFF + DRAWBACK",
                ModifierPolarity.Debuff => "DEBUFF",
                _ => "BUFF"
            };
            string capStr = g.stackCap > 0 ? $"{g.effectiveStacks}/{g.stackCap}" : $"{g.totalStacks}";
            lines.Add($"{g.DisplayTitle.ToUpperInvariant()} [{polarity}] ({capStr})");
            lines.Add(FormatCurrentTotalEffect(g));

            if (g.isAtCap && g.totalStacks > g.stackCap)
                lines.Add($"OVERFLOW STACKS RECORDED: {g.totalStacks} (CAPPED AT {g.stackCap})");

            for (int i = 0; i < g.instances.Count; i++)
            {
                var inst = g.instances[i];
                string dur = FormatDuration(g.def.durationUnit, inst.remainingDuration);
                string status = (g.stackCap > 0 && i >= g.stackCap) ? " [QUEUED BEHIND CAP]" : "";
                lines.Add($"• STACK {i + 1}: {dur}{status}");
            }
            return string.Join("\n", lines);
        }

        public static string FormatShortDuration(ModifierDurationUnit unit, int remaining) => unit switch
        {
            ModifierDurationUnit.Battles => $"{remaining}B",
            ModifierDurationUnit.NextGoldReward => "1G",
            ModifierDurationUnit.NextRewardRoll => "1R",
            _ => "1X"
        };

        public static string FormatDuration(ModifierDurationUnit unit, int remaining) => unit switch
        {
            ModifierDurationUnit.Battles => remaining == 1 ? "1 BATTLE LEFT" : $"{remaining} BATTLES LEFT",
            ModifierDurationUnit.NextGoldReward => "UNTIL NEXT GOLD REWARD",
            ModifierDurationUnit.NextRewardRoll => "NEXT REWARD ROLL",
            _ => "IMMEDIATE"
        };

        public static string FormatCurrentTotalEffect(DungeonModifierGroupSummary g)
        {
            int s = g.effectiveStacks;
            var d = g.def;
            return d.effectType switch
            {
                ModifierEffectType.PhysicalDamageBonus => $"TOTAL: PHYSICAL DAMAGE +{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.ElementalDamageBonus => $"TOTAL: ELEMENTAL DAMAGE +{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.PhysicalDefensePenetration => $"TOTAL: IGNORE {Math.Round(d.valuePerStack * s * 100)}% PHYSICAL DEF",
                ModifierEffectType.OffensiveQteWindow => d.valuePerStack >= 0
                    ? $"TOTAL: OFFENSIVE QTE WINDOW +{Math.Round(d.valuePerStack * s * 100)}%"
                    : $"TOTAL: OFFENSIVE QTE WINDOW {Math.Round(d.valuePerStack * s * 100)}% (MIN 66MS)",
                ModifierEffectType.DefensiveQteWindow => $"TOTAL: DEFENSIVE QTE WINDOW +{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.AllDamageReduction => d.modifierId == "MOD-023"
                    ? $"TOTAL: LAST HERO DR +{Math.Round(d.valuePerStack * s * 100)}% (MAX 25%)"
                    : $"TOTAL: DAMAGE REDUCTION +{Math.Round(d.valuePerStack * s * 100)}% (MAX 25%)",
                ModifierEffectType.HealingReceived => d.valuePerStack >= 0
                    ? $"TOTAL: HEALING RECEIVED +{Math.Round(d.valuePerStack * s * 100)}%"
                    : $"TOTAL: HEALING RECEIVED {Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.SkillMpCostPercent => $"TOTAL: SKILL MP COST -{Math.Round(d.valuePerStack * s * 100)}% (MIN 1)",
                ModifierEffectType.GoldRewardBonus => $"TOTAL: NEXT GOLD REWARD +{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.ConsumableDropChance => $"TOTAL: BONUS CONSUMABLE CHANCE +{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.ElementalDamageTakenReduction => $"TOTAL: {g.element.ToString().ToUpperInvariant()} DMG TAKEN -{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.BattleVictoryBonusGold => $"TOTAL: +{Math.Round(d.valuePerStack * s)} BONUS GOLD AFTER WIN",
                ModifierEffectType.OffenseDamageAndHealingDrawback =>
                    $"TOTAL: PHYS & ELEM DMG +{Math.Round(d.valuePerStack * s * 100)}%, HEALING {Math.Round(d.drawbackValuePerStack * s * 100)}%",
                ModifierEffectType.AllDamageAndTakenDrawback =>
                    $"TOTAL: ALL DMG +{Math.Round(d.valuePerStack * s * 100)}%, DMG TAKEN +{Math.Round(d.drawbackValuePerStack * s * 100)}%",
                ModifierEffectType.SpeedModifier => $"TOTAL: PARTY SPEED {Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.SkillMpCostFlat => $"TOTAL: SKILL MP COST +{Math.Round(d.valuePerStack * s)} MP",
                ModifierEffectType.BonusDamageVsBroken => $"TOTAL: DMG VS BROKEN +{Math.Round(d.valuePerStack * s * 100)}%",
                ModifierEffectType.ElementalDamageAttunement => $"TOTAL: {g.element.ToString().ToUpperInvariant()} DMG +{Math.Round(d.valuePerStack * s * 100)}%",
                _ => d.description.ToUpperInvariant()
            };
        }

        // ---- Save / Load ----

        public DungeonModifierSaveData[] ToSaveData() =>
            activeModifiers.Select(m => m.ToSaveData()).ToArray();

        public void RestoreFromSave(DungeonModifierSaveData[] data)
        {
            activeModifiers.Clear();
            nextAcquiredOrder = 1;
            if (data == null) return;

            foreach (var d in data)
            {
                var inst = DungeonModifierInstance.FromSaveData(d);
                if (inst != null && inst.remainingDuration > 0 && Database.DungeonModifiers.ContainsKey(inst.modifierId))
                {
                    activeModifiers.Add(inst);
                    if (inst.acquiredOrder >= nextAcquiredOrder)
                        nextAcquiredOrder = inst.acquiredOrder + 1;
                }
            }
        }
    }
}
