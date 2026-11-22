using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Obscure
{
    // Per-candidate scoring breakdown recorded during enemy AI planning (Architecture §17, Handoff Stage 0).
    public sealed class AiCandidateTrace
    {
        public string moveName;
        public string targetName;
        public int targetSlot = -1;
        public int aiPriority;
        public int mpCost;
        public bool charging;
        public bool legal = true;
        public bool forced;
        public string rejectReason;

        public float baseScore;
        public float immediate;
        public float future;
        public float role;
        public float synergy;
        public float objective;
        public float adaptation;
        public float resource;
        public float repetition;
        public float risk;
        public float rawModifier;
        public float clampedModifier;
        public float totalScore;

        public string Signature()
        {
            var inv = CultureInfo.InvariantCulture;
            if (!legal)
                return $"[X]{moveName}@{targetSlot}:{rejectReason}";
            return string.Format(inv,
                "{0}@{1}(p={2},mp={3},chg={4},f={5})={6:0.###}[b={7:0.###},im={8:0.###},fu={9:0.###},ro={10:0.###},sy={11:0.###},ob={12:0.###},ad={13:0.###},re={14:0.###},rp={15:0.###},rk={16:0.###},cl={17:0.###}]",
                moveName, targetSlot, aiPriority, mpCost, charging ? 1 : 0, forced ? 1 : 0,
                totalScore, baseScore, immediate, future, role, synergy, objective, adaptation,
                resource, repetition, risk, clampedModifier);
        }
    }

    // Complete record of one enemy AI decision (planning or execution fallback).
    public sealed class AiDecisionTrace
    {
        public int round;
        public int turnIndex;
        public string enemyId;
        public string enemyName;
        public int enemySlot;
        public Tier enemyTier;

        public readonly List<AiCandidateTrace> candidates = new List<AiCandidateTrace>();

        public string chosenMove;
        public string chosenTarget;
        public int chosenTargetSlot = -1;
        public bool charging;
        public bool telegraphed;
        public bool wasForced;
        public bool usedFallback;
        public bool adaptationChangedDecision;
        public string reason;

        // Wall-clock timing for Architecture §16.5 performance checks; intentionally excluded from Signature().
        public double decisionMs;

        public string Signature()
        {
            var sb = new StringBuilder();
            sb.Append($"R{round}.T{turnIndex}:{enemyId}#{enemySlot}->{chosenMove ?? "NONE"}@{chosenTargetSlot}");
            if (charging) sb.Append("[CHG]");
            if (telegraphed) sb.Append("[TEL]");
            if (wasForced) sb.Append("[FRC]");
            if (usedFallback) sb.Append("[FBK]");
            if (!string.IsNullOrEmpty(reason)) sb.Append($"({reason})");
            sb.Append("|");
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i > 0) sb.Append(";");
                sb.Append(candidates[i].Signature());
            }
            return sb.ToString();
        }
    }

    // Encounter-scoped log of every AI decision in a Battle.
    public sealed class AiTraceLog
    {
        public bool Enabled = true;
        public readonly List<AiDecisionTrace> Entries = new List<AiDecisionTrace>();

        public void Record(AiDecisionTrace trace)
        {
            if (Enabled && trace != null) Entries.Add(trace);
        }

        public bool Matches(AiTraceLog other, out string firstDiff)
        {
            firstDiff = null;
            if (other == null) { firstDiff = "other trace log is null"; return false; }
            if (Entries.Count != other.Entries.Count)
            {
                firstDiff = $"entry count differs: {Entries.Count} vs {other.Entries.Count}";
                return false;
            }
            for (int i = 0; i < Entries.Count; i++)
            {
                string a = Entries[i].Signature();
                string b = other.Entries[i].Signature();
                if (!string.Equals(a, b, StringComparison.Ordinal))
                {
                    firstDiff = $"entry {i} differs:\n  A: {a}\n  B: {b}";
                    return false;
                }
            }
            return true;
        }
    }

    // Serializable snapshot of a hero Unit at battle start for deterministic replay (Handoff Stage 0).
    public sealed class UnitSnapshot
    {
        public string id, name, role, raceId, classId, backgroundId, starterPassiveId;
        public int level, hp, mp;
        public AbilityScores baseScores;
        public AbilityScores permanentBonuses;
        public AbilityScores temporaryModifiers;
        public string[] sprite, skills;
        public Affinities affinities;
        public List<string> unlockedActives = new List<string>();
        public List<string> unlockedPassives = new List<string>();
        public Dictionary<string, int> skillRanks = new Dictionary<string, int>();
        public string[] activeSlots;
        public string[] passiveSlots;
        public List<string> selectedTalentIds = new List<string>();
        public List<StatModifier> permanentModifiers = new List<StatModifier>();
        public List<(EquipmentSlot slot, EquipmentInstance instance)> equipped = new List<(EquipmentSlot, EquipmentInstance)>();

        public static UnitSnapshot Capture(Unit u)
        {
            var snap = new UnitSnapshot
            {
                id = u.id,
                name = u.name,
                role = u.role,
                raceId = u.race?.id ?? "human",
                classId = u.classDef?.id ?? "knight",
                backgroundId = u.background?.id ?? "guard",
                starterPassiveId = u.starterPassiveId,
                level = u.level,
                hp = u.hp,
                mp = u.mp,
                baseScores = u.abilities.baseScores,
                permanentBonuses = u.abilities.permanentBonuses,
                temporaryModifiers = u.abilities.temporaryModifiers,
                sprite = u.sprite,
                skills = u.skills != null ? (string[])u.skills.Clone() : Array.Empty<string>(),
                affinities = u.affinities,
                activeSlots = (string[])u.skillLoadout.activeSlots.Clone(),
                passiveSlots = (string[])u.skillLoadout.passiveSlots.Clone(),
                selectedTalentIds = new List<string>(u.selectedTalentIds),
                permanentModifiers = new List<StatModifier>(u.permanentModifiers),
            };
            foreach (var sid in u.skillLoadout.UnlockedActiveSkills)
            {
                snap.unlockedActives.Add(sid);
                snap.skillRanks[sid] = u.skillLoadout.GetRank(sid);
            }
            foreach (var pid in u.skillLoadout.UnlockedPassiveSkills)
                snap.unlockedPassives.Add(pid);
            foreach (var kv in u.equippedInstances)
            {
                if (kv.Value != null)
                    snap.equipped.Add((kv.Key, CloneEquipment(kv.Value)));
            }
            return snap;
        }

        static EquipmentInstance CloneEquipment(EquipmentInstance src)
        {
            var copy = new EquipmentInstance(src.Definition, src.rarity, src.level)
            {
                definitionId = src.definitionId,
                upgradeLevel = src.upgradeLevel,
                specialEffectId = src.specialEffectId,
                boundCharacterId = src.boundCharacterId,
            };
            if (src.affixes != null) copy.affixes.AddRange(src.affixes);
            return copy;
        }

        public Unit ToUnit()
        {
            var def = new CharacterDef
            {
                id = id,
                name = name,
                role = role,
                raceId = raceId,
                classId = classId,
                backgroundId = backgroundId,
                starterPassiveId = starterPassiveId,
                baseScores = baseScores,
                sprite = sprite,
                skills = skills,
                affinities = affinities ?? Affinities.None,
            };
            var u = new Unit(def);
            u.level = level;
            u.abilities.permanentBonuses = permanentBonuses;
            u.abilities.temporaryModifiers = temporaryModifiers;
            foreach (var m in permanentModifiers) u.permanentModifiers.Add(m);
            foreach (var tid in selectedTalentIds) u.selectedTalentIds.Add(tid);
            foreach (var sid in unlockedActives) u.skillLoadout.UnlockActive(sid);
            foreach (var pid in unlockedPassives) u.skillLoadout.UnlockPassive(pid);
            foreach (var kv in skillRanks)
                u.skillLoadout.SkillRanks[kv.Key] = kv.Value;
            if (activeSlots != null)
                for (int i = 0; i < Math.Min(u.skillLoadout.activeSlots.Length, activeSlots.Length); i++)
                    u.skillLoadout.activeSlots[i] = activeSlots[i];
            if (passiveSlots != null)
                for (int i = 0; i < Math.Min(u.skillLoadout.passiveSlots.Length, passiveSlots.Length); i++)
                    u.skillLoadout.passiveSlots[i] = passiveSlots[i];
            foreach (var (slot, inst) in equipped)
                u.TryEquip(slot, CloneEquipment(inst), out _);
            u.RecalculateStats(fillResources: false);
            u.hp = Math.Clamp(hp, 0, u.stats.maxHp);
            u.mp = Math.Clamp(mp, 0, u.stats.maxMp);
            return u;
        }
    }

    public sealed class RecordedStep
    {
        public bool isPlayer;
        public int actorSlot;
        public ActionKind kind;
        public string skillId;
        public string itemId;
        public bool hasTarget;
        public bool targetIsEnemy;
        public int targetSlot;
        public float offensePower = 1f;
        public List<QteResult> defenseGrades;
    }

    public sealed class BattleRecord
    {
        public int battleSeed;
        public readonly List<UnitSnapshot> party = new List<UnitSnapshot>();
        public readonly List<string> enemyIds = new List<string>();
        public readonly List<string> initialItems = new List<string>();
        public readonly List<RecordedStep> steps = new List<RecordedStep>();
    }

    // Deterministic recorder and replayer for Battle + AiTrace (Architecture §17, Handoff Stage 0).
    public static class BattleReplay
    {
        public static BattleRecord BeginRecord(int battleSeed, IEnumerable<Unit> party, IEnumerable<EnemyDef> enemies, IEnumerable<string> items)
        {
            var rec = new BattleRecord { battleSeed = battleSeed };
            foreach (var u in party) rec.party.Add(UnitSnapshot.Capture(u));
            foreach (var e in enemies) rec.enemyIds.Add(e.id);
            if (items != null) rec.initialItems.AddRange(items);
            return rec;
        }

        public static void RecordPlayerStep(BattleRecord rec, BattleAction act, float power)
        {
            rec.steps.Add(new RecordedStep
            {
                isPlayer = true,
                actorSlot = act.actor.slot,
                kind = act.kind,
                skillId = act.skill?.id,
                itemId = act.item?.id,
                hasTarget = act.target != null,
                targetIsEnemy = act.target != null && act.target.IsEnemy,
                targetSlot = act.target?.slot ?? -1,
                offensePower = power,
            });
        }

        public static void RecordEnemyStep(BattleRecord rec, Combatant enemy, IReadOnlyList<QteResult> defense)
        {
            rec.steps.Add(new RecordedStep
            {
                isPlayer = false,
                actorSlot = enemy.slot,
                defenseGrades = defense != null ? new List<QteResult>(defense) : null,
            });
        }

        public static Battle Replay(BattleRecord rec, int maxSteps = 10000)
        {
            var party = rec.party.Select(p => p.ToUnit()).ToList();
            var enemies = rec.enemyIds.Select(id => Database.Enemies[id]).ToList();
            var items = new List<string>(rec.initialItems);
            var b = new Battle(party, enemies, items, new Random(rec.battleSeed));

            int stepIdx = 0;
            for (int guard = 0; guard < maxSteps && b.Outcome == Outcome.None && stepIdx < rec.steps.Count; guard++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                var step = rec.steps[stepIdx++];
                if (step.isPlayer)
                {
                    if (c.IsEnemy || c.slot != step.actorSlot)
                        throw new InvalidOperationException($"Replay desync at step {stepIdx - 1}: expected player slot {step.actorSlot}, got {(c.IsEnemy ? "enemy" : "player")} slot {c.slot}");
                    Combatant target = null;
                    if (step.hasTarget)
                    {
                        var pool = step.targetIsEnemy ? b.Enemies : b.Party;
                        target = pool.FirstOrDefault(x => x.slot == step.targetSlot);
                    }
                    BattleAction act = step.kind switch
                    {
                        ActionKind.Guard => BattleAction.Guard(c),
                        ActionKind.Skill => BattleAction.Skill(c, Database.Skills[step.skillId], target),
                        ActionKind.Item => BattleAction.Item(c, Database.Items[step.itemId], target),
                        _ => BattleAction.Attack(c, target),
                    };
                    b.Perform(act, step.offensePower);
                }
                else
                {
                    if (!c.IsEnemy || c.slot != step.actorSlot)
                        throw new InvalidOperationException($"Replay desync at step {stepIdx - 1}: expected enemy slot {step.actorSlot}, got {(c.IsEnemy ? "enemy" : "player")} slot {c.slot}");
                    var act = EnemyAI.Choose(b, c);
                    if (act == null) continue;
                    b.Perform(act, 1f, step.defenseGrades);
                }
            }
            return b;
        }
    }
}
