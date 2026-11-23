using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public struct AnalysisElementRow
    {
        public Element element;
        public string elementShort;
        public string label; // "???", "WEAK", "NORMAL", "RESIST", "NULL", "DRAIN", "REFLECT"
        public Effectiveness? known;
        public float resistancePct; // exact % for allies; unused (0) for enemies
    }

    public struct AnalysisStatusRow
    {
        public StatusId status;
        public string statusName;
        public string label; // Enemies: "???" or tier; Allies: exact "%" or "IMMUNE"
        public StatusResistTier? knownTier;
        public float exactResistancePct; // exact % for allies; unused for enemies
        public bool immune;
    }

    public struct AnalysisActiveEffectRow
    {
        public string label;
        public int remainingTurns; // <= 0 for stateless flags (Broken, Charging, Enhanced)
        public StatusId? statusId;
        public string subtitle; // optional detail line for defensive/racial effects
        public string iconKey;  // optional Figma template key (e.g. Icon-Racial-oceans_embrace)
    }

    // Enemy inspection snapshot (Felna_Analysis_System_Spec_v1.md Part B).
    // Strictly omits raw ATK/MATK/DEF/MDEF/SPD and raw status resistance %.
    public sealed class EnemyAnalysisView
    {
        public string enemyId;
        public string name;
        public string family;
        public string role;
        public string tierBadge;
        public string dungeonName;
        public string phaseLabel;
        public bool affinitiesShifted;

        public int hp, maxHp;
        public int breakCurrent, breakMax;
        public string telegraphedIntent;

        public readonly List<AnalysisElementRow> elements = new List<AnalysisElementRow>();
        public readonly List<AnalysisStatusRow> statusResistances = new List<AnalysisStatusRow>();
        public readonly List<AnalysisActiveEffectRow> activeEffects = new List<AnalysisActiveEffectRow>();
    }

    // Ally inspection snapshot (Felna_Analysis_System_Spec_v1.md Part C). Always 100% visible, never "???".
    public sealed class AllyAnalysisView
    {
        public string name;
        public string role;
        public int hp, maxHp;
        public int mp, maxMp;
        public int shield;
        public readonly List<string> shieldSources = new List<string>();
        public int poiseCurrent, poiseMax;

        // Attributes (AGI maps to DEX per Spec §0)
        public int str, dex, vit, intel, wis, cha;

        // Derived stats
        public int atk, matk, def, mdef, spd;
        public float critChancePct, critDamagePct;
        public int blockPower, staggerResistance;
        public float qteTimingBonusPct;

        public float universalStatusResistPct;
        public readonly List<AnalysisElementRow> elements = new List<AnalysisElementRow>();
        public readonly List<AnalysisStatusRow> statusResistances = new List<AnalysisStatusRow>();
        public readonly List<AnalysisActiveEffectRow> activeStatuses = new List<AnalysisActiveEffectRow>();
        public readonly List<AnalysisActiveEffectRow> defensiveEffects = new List<AnalysisActiveEffectRow>();
    }

    // Pure-C# data builder for the ANALYSIS panel and target-picker effectiveness tags
    // (Felna_Analysis_System_Spec_v1.md Parts A, B, C, D). Zero side effects — never modifies Battle state.
    public static class AnalysisInspector
    {
        public static readonly StatusId[] InspectableStatuses =
        {
            StatusId.Burn,
            StatusId.Bleed,
            StatusId.Freeze,
            StatusId.Poison,
            StatusId.Shock,
            StatusId.Curse,
            StatusId.Stun,
            StatusId.Silence,
            StatusId.Sleep,
            StatusId.Fear,
            StatusId.Confusion,
            StatusId.MindControl,
            StatusId.Despair,
        };

        // Target-picker tag (Spec Part A): returns WEAK, RESIST, NULL, DRAIN, REFLECT, NORMAL, or ??? for enemies.
        public static string TargetPickerTag(Battle battle, Combatant target, Element element)
        {
            if (battle == null || target == null || !target.IsEnemy || target.enemy == null) return "";
            var known = battle.Codex.GetKnownElement(target.enemy.id, element);
            return EnemyCodex.FormatEffectivenessTag(known);
        }

        // Codex-filtered summary for target dossiers/tooltips (never leaks unrevealed enemy affinities).
        public static string DescribeKnownEnemyAffinities(Battle battle, Combatant enemy)
        {
            if (battle == null || enemy == null || !enemy.IsEnemy || enemy.enemy == null) return "";
            var parts = new List<string>();
            var weak = new List<string>();
            var resist = new List<string>();
            var special = new List<string>();
            foreach (var el in AffinityResolver.AllElements)
            {
                var known = battle.Codex.GetKnownElement(enemy.enemy.id, el);
                if (!known.HasValue || known.Value == Effectiveness.Neutral) continue;
                string sh = ElementConfig.Short(el);
                switch (known.Value)
                {
                    case Effectiveness.Weak: weak.Add(sh); break;
                    case Effectiveness.Resist: resist.Add(sh); break;
                    case Effectiveness.Immune: special.Add("NULL:" + sh); break;
                    case Effectiveness.Drain: special.Add("DRN:" + sh); break;
                    case Effectiveness.Reflect: special.Add("RFL:" + sh); break;
                }
            }
            if (weak.Count > 0) parts.Add("WK:" + string.Join(",", weak));
            if (resist.Count > 0) parts.Add("RS:" + string.Join(",", resist));
            if (special.Count > 0) parts.Add(string.Join(" ", special));
            return parts.Count > 0 ? string.Join("  ", parts) : "AFFINITIES: ???";
        }

        public static EnemyAnalysisView BuildEnemyView(Battle battle, Combatant enemy)
        {
            if (enemy == null) return null;
            var def = enemy.enemy;
            var codex = battle?.Codex;
            string tierBadge = def == null ? "NORMAL"
                : def.isEnhanced ? "ENHANCED"
                : def.aiTier == AiTier.Miniboss ? "MINIBOSS"
                : def.tier.ToString().ToUpperInvariant();

            string intent = "";
            if (enemy.Charge?.move != null)
                intent = $"CHARGING: {enemy.Charge.move.name.ToUpperInvariant()}";
            else if (enemy.Intent?.move != null && enemy.Intent.IsCommitted)
                intent = enemy.Intent.charging
                    ? $"WILL CHARGE: {enemy.Intent.move.name.ToUpperInvariant()}"
                    : enemy.Intent.move.name.ToUpperInvariant();

            var view = new EnemyAnalysisView
            {
                enemyId = def?.id ?? "",
                name = enemy.name,
                family = string.IsNullOrEmpty(def?.type) ? "Unknown" : def.type,
                role = string.IsNullOrEmpty(def?.role) ? (def?.aiRole.ToString() ?? "Foe") : def.role,
                tierBadge = tierBadge,
                dungeonName = SaveSlotMetadata.GetDungeonName(battle?.Dungeon ?? 1),
                phaseLabel = def != null && def.tier == Tier.Boss ? $"PHASE {enemy.phase}" : "",
                affinitiesShifted = battle != null && battle.BossJustShiftedAffinities && def != null && def.tier == Tier.Boss,
                hp = enemy.unit.hp,
                maxHp = enemy.Stats.maxHp,
                breakCurrent = enemy.CurrentBreakGauge,
                breakMax = enemy.MaxBreakGauge,
                telegraphedIntent = intent,
            };

            // 10 Elemental affinity rows — strictly filtered by Codex (??? until discovered).
            foreach (var el in AffinityResolver.AllElements)
            {
                Effectiveness? known = (codex != null && def != null) ? codex.GetKnownElement(def.id, el) : null;
                // Keep view consistent with AffinityResolver when known (raw canonical affinity).
                if (known.HasValue)
                    known = AffinityResolver.GetRaw(enemy, el);
                view.elements.Add(new AnalysisElementRow
                {
                    element = el,
                    elementShort = ElementConfig.Short(el),
                    known = known,
                    label = EnemyCodex.FormatEffectivenessTag(known),
                });
            }

            // Status resistance rows — strictly filtered by Codex, tier labels only (never raw %).
            foreach (var st in InspectableStatuses)
            {
                StatusResistTier? known = (codex != null && def != null) ? codex.GetKnownStatus(def.id, st) : null;
                view.statusResistances.Add(new AnalysisStatusRow
                {
                    status = st,
                    statusName = StatusInfo.Name(st),
                    knownTier = known,
                    label = EnemyCodex.FormatStatusTier(known),
                    immune = known == StatusResistTier.Immune,
                });
            }

            // Active effects — ALWAYS visible (Spec Part B).
            if (enemy.IsBroken)
                view.activeEffects.Add(new AnalysisActiveEffectRow { label = "BROKEN", remainingTurns = enemy.Get(StatusId.Broken)?.turns ?? 1, statusId = StatusId.Broken });
            if (enemy.IsCharging)
                view.activeEffects.Add(new AnalysisActiveEffectRow { label = "CHARGING", remainingTurns = 1, subtitle = "Preparing a telegraphed heavy attack." });
            if (enemy.NullBreak != null && enemy.NullBreak.HasActiveNullBreak)
                view.activeEffects.Add(new AnalysisActiveEffectRow { label = $"NULL BREAK ({enemy.NullBreak.ActiveInstance.targetKey})", remainingTurns = enemy.NullBreak.ActiveInstance.remainingTurns, subtitle = "Nullification broken — element deals normal damage." });
            if (def != null && def.isEnhanced)
                view.activeEffects.Add(new AnalysisActiveEffectRow { label = "ENHANCED", remainingTurns = 0, subtitle = "Transformed enhanced variant with heightened power." });
            if (enemy.guarding)
                view.activeEffects.Add(new AnalysisActiveEffectRow { label = "GUARDING", remainingTurns = 1, statusId = StatusId.Guardian, subtitle = "Braced against incoming attacks until next turn." });

            AppendActiveStatuses(enemy, view.activeEffects);
            return view;
        }

        public static AllyAnalysisView BuildAllyView(Combatant ally)
        {
            if (ally == null) return null;
            var u = ally.unit;
            var d = u.Derived;
            var scores = u.FinalScores;
            var view = new AllyAnalysisView
            {
                name = ally.name,
                role = (u.role ?? u.classDef?.name ?? "Hero").ToUpperInvariant(),
                hp = u.hp,
                maxHp = ally.Stats.maxHp,
                mp = u.mp,
                maxMp = ally.Stats.maxMp,
                shield = u.shield,
                poiseCurrent = ally.CurrentBreakGauge,
                poiseMax = ally.MaxBreakGauge,
                str = scores.str,
                dex = scores.dex,
                vit = scores.vit,
                intel = scores.intel,
                wis = scores.wis,
                cha = scores.cha,
                atk = ally.Atk,
                matk = ally.Magic,
                def = ally.Def,
                mdef = ally.MagicDef,
                spd = ally.Speed,
                critChancePct = d.criticalChance,
                critDamagePct = d.criticalDamage,
                blockPower = d.blockPower,
                staggerResistance = ally.StaggerRes,
                qteTimingBonusPct = d.QteTimingBonusFraction * 100f,
                universalStatusResistPct = ally.StatusController.ResistanceProfile.GetEffectiveResistance() * 100f,
            };

            if (u.shield > 0)
            {
                if (ally.OceansEmbraceShieldActive) view.shieldSources.Add("Ocean's Embrace");
                if (ally.DeterminationTurns > 0) view.shieldSources.Add("Determination");
                if (view.shieldSources.Count == 0) view.shieldSources.Add("Barrier");
            }

            // Elemental affinities — exact values, never ???.
            foreach (var el in AffinityResolver.AllElements)
            {
                var eff = AffinityResolver.Get(ally, el);
                float resPct = 0f;
                var mapped = ElementTypes.FromLegacyElement(el);
                if (mapped.IsValidElement() && ally.ElementalResistances != null)
                    resPct = ally.ElementalResistances.GetFinalResistance(mapped) * 100f;
                string tag = EnemyCodex.FormatEffectivenessTag(eff);
                string lbl = resPct > 0.05f ? $"{tag} ({resPct:0}% RES)" : tag;
                view.elements.Add(new AnalysisElementRow
                {
                    element = el,
                    elementShort = ElementConfig.Short(el),
                    known = eff,
                    label = lbl,
                    resistancePct = resPct,
                });
            }

            // Status resistance — universal % and exact final % for every status.
            var rp = ally.StatusController.ResistanceProfile;
            foreach (var st in InspectableStatuses)
            {
                bool imm = rp.IsImmune(st, isBoss: false);
                float pct = imm ? 100f : rp.GetEffectiveResistance(st) * 100f;
                view.statusResistances.Add(new AnalysisStatusRow
                {
                    status = st,
                    statusName = StatusInfo.Name(st),
                    immune = imm,
                    exactResistancePct = pct,
                    label = imm ? "IMMUNE" : $"{pct:0}%",
                });
            }

            // Active statuses, buffs, and debuffs.
            AppendActiveStatuses(ally, view.activeStatuses);

            // Defensive & Racial Feat effects (Spec Part C).
            if (ally.guarding)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "GUARD", remainingTurns = 1, statusId = StatusId.Guardian, subtitle = "Incoming damage reduced until next turn" });
            if (ally.IsPhased)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "PHASED (FEY STEP)", remainingTurns = 1, statusId = StatusId.Evasion, subtitle = "Untargetable until next action" });
            if (ally.IronResolveActive)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "IRON RESOLVE", remainingTurns = 1, statusId = StatusId.DefenseUp, subtitle = "Endures lethal damage once" });
            if (ally.OceansEmbraceShieldActive)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "OCEAN'S EMBRACE (+10% DEF QTE)", remainingTurns = Math.Max(1, ally.OceansEmbraceTurns), iconKey = "Icon-Racial-oceans_embrace", subtitle = u.shield > 0 ? $"+10% DEF ON QTE · SHIELD {u.shield}" : "+10% DEF ON QTE" });
            if (ally.LightboundTurns > 0)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "LIGHTBOUND (-10% DMG, +20% RES)", remainingTurns = ally.LightboundTurns, statusId = StatusId.MagicDefenseUp, subtitle = $"-10% DMG TAKEN · +20% RES · {ally.LightboundTurns}T" });
            if (ally.DeterminationTurns > 0)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "DETERMINATION (+15% DMG)", remainingTurns = ally.DeterminationTurns, iconKey = "Icon-Racial-determination", subtitle = $"+15% DMG · {ally.DeterminationTurns}T" });
            if (ally.DraconicOverdriveTurns > 0)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "DRACONIC OVERDRIVE", remainingTurns = ally.DraconicOverdriveTurns, statusId = StatusId.AttackUp, subtitle = $"HEIGHTENED OUTPUT · {ally.DraconicOverdriveTurns}T" });
            if (ally.DraconicExhaustedTurns > 0)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = "EXHAUSTED", remainingTurns = ally.DraconicExhaustedTurns, statusId = StatusId.Exhausted, subtitle = $"ATK/MATK -20% · {ally.DraconicExhaustedTurns}T" });
            if (u.shield > 0)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = $"SHIELD: {u.shield} ({string.Join(", ", view.shieldSources)})", remainingTurns = 0, statusId = StatusId.DefenseUp, subtitle = $"ABSORBS {u.shield} DAMAGE" });
            var guardian = ally.Get(StatusId.Guardian);
            if (guardian != null)
                view.defensiveEffects.Add(new AnalysisActiveEffectRow { label = $"INTERPOSE (GUARDIAN)", remainingTurns = guardian.turns, statusId = StatusId.Guardian, subtitle = $"INTERCEPTS HITS · {guardian.turns}T" });

            return view;
        }

        static void AppendActiveStatuses(Combatant c, List<AnalysisActiveEffectRow> dst)
        {
            var seen = new HashSet<StatusId>();
            if (c.StatusController != null)
            {
                foreach (var inst in c.StatusController.ActiveInstances)
                {
                    if (inst.IsExpired) continue;
                    seen.Add(inst.statusId);
                    dst.Add(new AnalysisActiveEffectRow
                    {
                        label = StatusInfo.Name(inst.statusId).ToUpperInvariant(),
                        remainingTurns = inst.remainingTurns,
                        statusId = inst.statusId,
                    });
                }
            }
            if (c.statuses != null)
            {
                foreach (var s in c.statuses)
                {
                    if (s.turns <= 0 || seen.Contains(s.id)) continue;
                    seen.Add(s.id);
                    dst.Add(new AnalysisActiveEffectRow
                    {
                        label = StatusInfo.Name(s.id).ToUpperInvariant(),
                        remainingTurns = s.turns,
                        statusId = s.id,
                    });
                }
            }
        }
    }
}
