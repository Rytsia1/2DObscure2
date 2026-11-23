using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace Obscure.Tests
{
    // Headless balance simulation for the QTE fix (GDD Bab 12 §2.5, §3.5, §6.5). Plays the real Battle code with a fixed
    // player policy and scripted QTE grades, so build quality and execution can be compared on Dungeon 1 encounters.
    // Report: Unity -batchmode -runTests -testPlatform EditMode -testFilter Obscure.Tests.BalanceSimulation.Report
    // It writes Logs/BalanceSim.md (gitignored).
    // ponytail: no consumables, relics, modifiers, Lucky/Second Chance or rests; every fight starts at full HP/MP.
    // Add them when tuning a whole stage rather than single encounters.
    public class BalanceSimulation
    {
        public enum Build { Recommended, Inverted }

        // Starter = creation gear only. Dungeon 1 shops sell any base item at equipment level 1-2 with a rolled rarity
        // (EquipmentLootGenerator, tier 1: 50% Common, 35% Uncommon, 12% Rare, 3% Epic), so the D1 tiers give each
        // upgraded slot the best fitting base item at Uncommon, level 2. Weapon + body armor is roughly affordable;
        // the full set is the upper bound.
        public enum Gear { Starter, D1WeaponArmor, D1FullSet }

        static EquipmentSlot[] Upgraded(Gear g) =>
            g == Gear.Starter ? Array.Empty<EquipmentSlot>()
            : g == Gear.D1WeaponArmor ? new[] { EquipmentSlot.MainHand, EquipmentSlot.BodyArmor }
            : AllSlots;

        // EquipmentSlot has alias names (Weapon/MainHand, Armor/BodyArmor...), so GetValues repeats values.
        static readonly EquipmentSlot[] AllSlots = ((EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot))).Distinct().ToArray();

        static EquipmentInstance D1Item(EquipmentDef def) =>
            EquipmentLootGenerator.CreateInstance(def, EquipmentRarity.Uncommon, 2, new Random(def.id.Sum(ch => ch))); // fixed affixes per item

        static float GearScore(Unit u)
        {
            var d = u.Derived;
            return d.physicalPower + d.rangedPower + d.magicPower + d.healingPower + d.physicalDefense + d.magicDefense + d.maxHp / 5f;
        }

        static readonly Dictionary<(string cls, Build build, EquipmentSlot slot), string> bestItem = new Dictionary<(string, Build, EquipmentSlot), string>();

        // Best base item for a slot, judged on a fresh level-1 hero. Weapons stay in the starter weapon's family,
        // so a Fighter keeps a sword rather than switching to whatever raises the most numbers.
        static string BestItem((string race, string cls, string bg) p, Build build, EquipmentSlot slot)
        {
            if (bestItem.TryGetValue((p.cls, build, slot), out var cached)) return cached;
            var baseline = new Unit(Def(p, build));
            baseline.equipped.TryGetValue(slot, out var current);
            float best = GearScore(baseline);
            string bestId = null;
            foreach (var def in Database.Equipment.Values.Where(e => !e.isUnarmed && !e.IsCursed && EquipmentValidator.SlotMatches(e, slot)))
            {
                if (slot == EquipmentSlot.MainHand && current != null && def.weaponType != current.weaponType) continue;
                var probe = new Unit(Def(p, build));
                if (!probe.TryEquip(slot, D1Item(def), out _)) continue;
                float score = GearScore(probe);
                if (score > best) { best = score; bestId = def.id; }
            }
            return bestItem[(p.cls, build, slot)] = bestId;
        }

        // Chance of Perfect and Good per QTE; the rest is Poor.
        public sealed class Execution
        {
            public string name;
            public double perfect, good;
            public QteResult Roll(Random rng)
            {
                double r = rng.NextDouble();
                return r < perfect ? QteResult.Perfect : r < perfect + good ? QteResult.Good : QteResult.Poor;
            }
        }

        public static readonly Execution[] Executions =
        {
            new Execution { name = "All Perfect", perfect = 1, good = 0 },
            new Execution { name = "All Good", perfect = 0, good = 1 },
            // ASSUMPTION: a typical player hits 25% Perfect / 55% Good / 20% Poor. Not measured; replace with playtest data.
            new Execution { name = "Typical 25/55/20", perfect = 0.25, good = 0.55 },
            new Execution { name = "All Poor", perfect = 0, good = 0 },
        };

        // Hero level per encounter, measured 2026-11-21: median level on first entering each encounter, 200 Random paths (ExpPacingReport, Logs/ExpPacing.md).
        // The boss also runs at 8 as an under-levelled check.
        public static readonly (string id, int level)[] Encounters = { ("d1_e5", 4), ("d1_e7", 9), ("d1_boss", 8), ("d1_boss", 12) };

        static readonly (string race, string cls, string bg)[] PartyPlan =
        {
            ("human", "fighter", "guard"), ("dwarf", "cleric", "acolyte"), ("elf", "wizard", "sage"), ("halfling", "rogue", "charlatan"),
        };

        public const int MaxRounds = 40; // a fight still going after this counts as a loss

        static readonly Dictionary<(string cls, Build build), CharacterDef> defs = new Dictionary<(string, Build), CharacterDef>();

        static CharacterDef Def((string race, string cls, string bg) p, Build build)
        {
            if (defs.TryGetValue((p.cls, build), out var d)) return d;
            var s = new CharacterCreationSession();
            s.SelectRace(p.race);
            s.SelectClass(p.cls);
            s.SelectBackground(p.bg);
            s.ApplyRecommendedPointBuy(out _);
            if (build == Build.Inverted) s.PointBuy.SetAllOrThrow(Invert(s.BaseScores));
            return defs[(p.cls, build)] = s.Confirm();
        }

        // Same 27 points, but the class's best ability gets the lowest score and so on.
        static AbilityScores Invert(AbilityScores s)
        {
            var byScore = ((Ability[])Enum.GetValues(typeof(Ability))).OrderByDescending(a => s[a]).ToArray();
            var values = byScore.Select(a => s[a]).Reverse().ToArray();
            var r = s;
            for (int i = 0; i < byScore.Length; i++) r = r.With(byScore[i], values[i]);
            return r;
        }

        public static Unit MakeHero((string race, string cls, string bg) p, Build build, int level, Gear gear = Gear.Starter)
        {
            var def = Def(p, build);
            var u = new Unit(def);
            foreach (var slot in Upgraded(gear))
            {
                var id = BestItem(p, build, slot);
                if (id != null) u.TryEquip(slot, D1Item(Database.Equipment[id]), out _);
            }
            for (int l = 1; l < level; l++) u.LevelUp();

            // ASI: the recommended build raises its best ability, the inverted build its worst.
            var order = ((Ability[])Enum.GetValues(typeof(Ability))).OrderByDescending(a => u.BaseScores[a]).ToArray();
            var asi = build == Build.Recommended ? order[0] : order[order.Length - 1];
            while (u.unspentAsiPoints > 0 && u.TrySpendAsiPoint(asi)) { }

            // Skills: unlock what SP allows (highest tier first), then equip the best three actives.
            var loadout = u.skillLoadout;
            foreach (var sk in Database.Skills.Values.Where(k => k.classId == p.cls).OrderByDescending(k => k.tier).ThenByDescending(SkillValue))
                loadout.TryUnlockSkill(sk.id, u.level, u.classDef, out _);
            var picks = loadout.UnlockedActiveSkills.Where(Database.Skills.ContainsKey).Select(id => Database.Skills[id])
                .OrderByDescending(SkillValue).Take(SkillLoadout.ActiveSlotCount).ToList();
            for (int i = 0; i < picks.Count; i++) loadout.TryEquipActive(i, picks[i].id, out _);

            u.RecalculateStats(fillResources: true);
            u.hp = u.stats.maxHp;
            u.mp = u.stats.maxMp;
            return u;
        }

        static float SkillValue(SkillDef k) =>
            k.kind == SkillKind.Heal ? 100f : (k.kind == SkillKind.Physical || k.kind == SkillKind.Magic) ? k.power * Math.Max(1, k.hits) : 0f;

        // Player policy, the same for every build and execution so only those two vary:
        // heal an ally below 40% HP, Guard against a telegraphed Heavy or charge aimed at this hero,
        // else the strongest affordable damaging skill, else a basic attack on the weakest enemy.
        // AttackOnly is Dheo's 19 Nov playtest line: "just attack and parry without using skill". Never Guard, heal or skill.
        public enum Policy { Tactical, AttackOnly }

        static BattleAction Choose(Battle b, Combatant c, Policy policy = Policy.Tactical)
        {
            var foes = b.Enemies.Where(e => e.Alive).ToList();
            var target = foes.OrderBy(e => e.unit.hp).First();
            if (policy == Policy.AttackOnly) return BattleAction.Attack(c, target);
            var skills = c.unit.GetCombatActiveSkillIds().Where(Database.Skills.ContainsKey).Select(id => Database.Skills[id]).ToList();

            var hurt = b.Party.Where(p => p.Alive).OrderBy(p => (float)p.unit.hp / p.Stats.maxHp).First();
            if ((float)hurt.unit.hp / hurt.Stats.maxHp < 0.4f)
                foreach (var s in skills.Where(s => s.kind == SkillKind.Heal))
                    if (Legal(b, BattleAction.Skill(c, s, s.target == TargetType.Ally ? hurt : null), out var heal)) return heal;

            if (foes.Any(e => Threatens(e, c))) return BattleAction.Guard(c);

            foreach (var s in skills.Where(s => (s.kind == SkillKind.Physical || s.kind == SkillKind.Magic) && s.power > 0).OrderByDescending(SkillValue))
                if (Legal(b, BattleAction.Skill(c, s, s.target == TargetType.Enemy ? target : null), out var act)) return act;

            return BattleAction.Attack(c, target);
        }

        static bool Legal(Battle b, BattleAction a, out BattleAction ok)
        {
            ok = b.Validate(a) == null ? a : null;
            return ok != null;
        }

        static bool Threatens(Combatant enemy, Combatant hero)
        {
            var intent = enemy.Charge ?? enemy.Intent;
            if (intent?.move == null) return false;
            bool aimed = intent.target == hero || intent.move.allTargets;
            // A charged all-target move (Soulstorm) only gets a Guard if it is Heavy: else all four heroes Guard and the round deals 0.
            return aimed && (intent.move.HasHeavy || enemy.IsCharging && !intent.move.allTargets);
        }

        public sealed class Result
        {
            public bool won, timedOut;
            public int rounds, playerTurns, skillTurns, guardTurns, damageDealt, damageTaken, partyDeaths, summonsSpawned, transformsTriggered;
            public float hpLeft;

            // Spec §14.6 & Architecture §16.5 AI telemetry
            public int enemyActions, wastedEnemyActions, repeatedEnemyActions;
            public int singleTargetAttacks;
            public readonly Dictionary<int, int> singleTargetByHeroSlot = new Dictionary<int, int>();
            public int chargesStarted, chargesInterrupted, telegraphedActions, telegraphedGuardedOrInterrupted;
            public int dangerousActions, dangerousTelegraphed;
            public int totalEnemyMpSpent, totalEnemyMaxMp;
            public int totalEnemyAliveRounds, totalEnemyCount;
            public int lethalRoundDamage, lethalRoundOverkill;
            public int healTurns, usefulHealTurns;
            public int aiDecisions, adaptationDecisions;
            public double totalDecisionMs, maxDecisionMs, maxRoundDecisionMs;
            public int roleAlignedDecisions, totalRoleDecisions;
            public readonly List<string> decisionSignatures = new List<string>();
        }

        public static Result Fight(string encounterId, int level, Build build, Execution ex, int seed, Gear gear = Gear.Starter,
            Policy policy = Policy.Tactical, BattleRecord recordOut = null, bool collectSignatures = false, bool allowTransforms = true,
            int stage = 1, IReadOnlyList<(string modId, ElementType elem)> modifiers = null)
        {
            var party = PartyPlan.Select(p => MakeHero(p, build, level, gear)).ToList();
            var enc = Database.Encounters[encounterId];
            var enemyDefs = enc.enemies.Select(id => Database.Enemies[id]).ToList();
            var qteRng = new Random(seed);
            int battleSeed = unchecked(seed * 31 + 7);
            if (recordOut != null)
            {
                var rec = BattleReplay.BeginRecord(battleSeed, party, enemyDefs, Array.Empty<string>());
                recordOut.battleSeed = rec.battleSeed;
                recordOut.party.Clear(); recordOut.party.AddRange(rec.party);
                recordOut.enemyIds.Clear(); recordOut.enemyIds.AddRange(rec.enemyIds);
                recordOut.initialItems.Clear(); recordOut.initialItems.AddRange(rec.initialItems);
                recordOut.steps.Clear();
            }
            RunState simRun = null;
            if (stage > 1 || (modifiers != null && modifiers.Count > 0))
            {
                var stubMap = new MapDef
                {
                    name = "Sim",
                    act = 1,
                    stage = Math.Max(1, stage),
                    totalStages = Math.Max(3, stage),
                    nodes = new[] { new NodeDef { id = "s0", type = NodeType.Battle, floor = 0, next = Array.Empty<string>() } }
                };
                simRun = new RunState(stubMap, PartyPlan.Select(p => Def(p, build)), battleSeed);
                if (modifiers != null)
                {
                    var modRng = new Random(battleSeed);
                    foreach (var (modId, elem) in modifiers)
                        simRun.ModifierManager.AddModifier(modId, simRun, elem, modRng);
                }
            }
            var b = new Battle(party, enemyDefs, new List<string>(), new Random(battleSeed), simRun);
            if (!allowTransforms)
            {
                foreach (var e in b.Enemies) e.transformRolled = true;
            }
            var res = new Result();
            b.EnemyTransformed += _ => res.transformsTriggered++;
            var lastMoveTargetByEnemy = new Dictionary<Combatant, (string move, int targetSlot)>();
            var heroHpAtRoundStart = new Dictionary<int, int>();
            var roundDmgToHero = new Dictionary<int, int>();
            int trackedRound = 0;

            void FlushRoundMetrics()
            {
                if (trackedRound <= 0) return;
                res.totalEnemyAliveRounds += b.Enemies.Count(e => e.Alive);
                foreach (var p in b.Party)
                {
                    if (!p.Alive && heroHpAtRoundStart.TryGetValue(p.slot, out int startHp) && startHp > 0
                        && roundDmgToHero.TryGetValue(p.slot, out int dmg) && dmg > 0)
                    {
                        res.lethalRoundDamage += dmg;
                        res.lethalRoundOverkill += Math.Max(0, dmg - startHp);
                    }
                }
            }

            void BeginRoundMetrics()
            {
                trackedRound = b.Round;
                heroHpAtRoundStart.Clear();
                roundDmgToHero.Clear();
                foreach (var p in b.Party) heroHpAtRoundStart[p.slot] = p.unit.hp;
            }

            int statusCountBefore = 0;
            b.StatusApplied += (_, _, _) => statusCountBefore++;

            for (int steps = 0; steps < 10000 && b.Outcome == Outcome.None && b.Round <= MaxRounds; steps++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (b.Round != trackedRound)
                {
                    FlushRoundMetrics();
                    BeginRoundMetrics();
                }
                if (c.IsEnemy)
                {
                    bool wasCharging = c.IsCharging;
                    var planned = c.Intent;
                    var act = EnemyAI.Choose(b, c);
                    if (act == null)
                    {
                        res.wastedEnemyActions++;
                        res.enemyActions++;
                        continue;
                    }
                    var qte = QteFactory.Defense(act, QteAssist.Standard, null, qteRng);
                    List<QteResult> defense = null;
                    if (qte is WardQte ward) defense = Enumerable.Repeat(ex.Roll(qteRng), ward.Results.Count).ToList();
                    else if (qte is DefenseQte d) defense = Enumerable.Range(0, d.Hits).Select(_ => ex.Roll(qteRng)).ToList();
                    else if (qte is StruggleQte st) defense = Enumerable.Repeat(ex.Roll(qteRng), st.hits).ToList();
                    if (recordOut != null) BattleReplay.RecordEnemyStep(recordOut, c, defense);

                    int enemiesBefore = b.Enemies.Count;
                    int mpBefore = c.unit.mp;
                    int statusesBefore = statusCountBefore;
                    bool targetWasGuarding = act.target != null && act.target.IsPlayer && act.target.guarding;
                    bool anyPartyGuarding = b.Party.Any(p => p.Alive && p.guarding);
                    float healTargetHpFrac = (act.attack != null && act.attack.Heals && act.target != null)
                        ? (float)act.target.unit.hp / Math.Max(1, act.target.Stats.maxHp) : 1f;

                    var hits = b.Perform(act, 1f, defense);

                    res.enemyActions++;
                    int mpSpent = Math.Max(0, mpBefore - c.unit.mp);
                    res.totalEnemyMpSpent += mpSpent;
                    int spawned = Math.Max(0, b.Enemies.Count - enemiesBefore);
                    res.summonsSpawned += spawned;

                    int dmgDone = 0;
                    int healDone = 0;
                    int breakDone = 0;
                    foreach (var h in hits)
                    {
                        if (h.heal) healDone += Math.Abs(h.amount);
                        else if (h.target != null && h.target.IsPlayer)
                        {
                            int dAmt = Math.Max(0, h.amount);
                            dmgDone += dAmt;
                            breakDone += Math.Max(0, h.breakAmount);
                            roundDmgToHero[h.target.slot] = (roundDmgToHero.TryGetValue(h.target.slot, out int prev) ? prev : 0) + dAmt;
                        }
                    }
                    res.damageTaken += dmgDone;

                    var mv = act.attack;
                    if (mv != null)
                    {
                        var livingFoes = b.Party.Where(p => p.Alive).ToList();
                        var hurtAlly = b.Enemies.Where(e => e.Alive).OrderBy(e => (float)e.unit.hp / Math.Max(1, e.Stats.maxHp)).FirstOrDefault();
                        res.totalRoleDecisions++;
                        if (AiProfile.IsRoleAligned(c, mv, act.target, livingFoes, hurtAlly))
                            res.roleAlignedDecisions++;
                        if (mv.NeedsTelegraph)
                        {
                            res.dangerousActions++;
                            if (planned != null || wasCharging) res.dangerousTelegraphed++;
                        }
                        if (act.charging)
                        {
                            res.chargesStarted++;
                            res.telegraphedActions++;
                        }
                        else if (mv.NeedsTelegraph)
                        {
                            res.telegraphedActions++;
                            if (mv.allTargets ? anyPartyGuarding : targetWasGuarding)
                                res.telegraphedGuardedOrInterrupted++;
                        }
                        if (mv.Heals)
                        {
                            res.healTurns++;
                            if (healTargetHpFrac <= 0.70f && healDone > 0) res.usefulHealTurns++;
                        }
                        if (!act.charging && !mv.allTargets && !mv.self && !mv.Heals && !mv.Summons && act.target != null && act.target.IsPlayer)
                        {
                            res.singleTargetAttacks++;
                            int sl = act.target.slot;
                            res.singleTargetByHeroSlot[sl] = (res.singleTargetByHeroSlot.TryGetValue(sl, out int cnt) ? cnt : 0) + 1;
                        }
                        bool nonWasted = act.charging || mv.Summons && spawned > 0 || mv.self || (mv.Heals && healDone > 0)
                            || dmgDone > 0 || breakDone > 0 || statusCountBefore > statusesBefore;
                        if (!nonWasted) res.wastedEnemyActions++;

                        var key = (mv.name, act.target?.slot ?? -1);
                        if (lastMoveTargetByEnemy.TryGetValue(c, out var prevKey) && prevKey == key)
                            res.repeatedEnemyActions++;
                        lastMoveTargetByEnemy[c] = key;
                    }
                }
                else
                {
                    var act = Choose(b, c, policy);
                    res.playerTurns++;
                    if (act.kind == ActionKind.Skill) res.skillTurns++;
                    if (act.kind == ActionKind.Guard) res.guardTurns++;
                    float power = QteFactory.KindFor(act) == OffenseQteKind.None ? 1f : Battle.QtePower(act, ex.Roll(qteRng));
                    if (recordOut != null) BattleReplay.RecordPlayerStep(recordOut, act, power);
                    var chargingBefore = b.Enemies.Where(e => e.Alive && e.IsCharging).ToList();
                    var hits = b.Perform(act, power);
                    res.damageDealt += hits.Where(h => !h.heal && h.target != null && h.target.IsEnemy).Sum(h => Math.Max(0, h.amount));
                    foreach (var ce in chargingBefore)
                    {
                        if (ce.Alive && !ce.IsCharging && ce.IsBroken)
                        {
                            res.chargesInterrupted++;
                            res.telegraphedGuardedOrInterrupted++;
                        }
                    }
                }
            }

            FlushRoundMetrics();
            res.totalEnemyCount = b.Enemies.Count;
            res.totalEnemyMaxMp = b.Enemies.Sum(e => e.Stats.maxMp);

            // Aggregate AI trace stats
            var roundMs = new Dictionary<int, double>();
            foreach (var tr in b.AiTrace.Entries)
            {
                res.aiDecisions++;
                if (tr.adaptationChangedDecision) res.adaptationDecisions++;
                res.totalDecisionMs += tr.decisionMs;
                if (tr.decisionMs > res.maxDecisionMs) res.maxDecisionMs = tr.decisionMs;
                roundMs[tr.round] = (roundMs.TryGetValue(tr.round, out double rm) ? rm : 0.0) + tr.decisionMs;
                if (collectSignatures) res.decisionSignatures.Add(tr.Signature());
            }
            if (roundMs.Count > 0) res.maxRoundDecisionMs = roundMs.Values.Max();

            res.won = b.Outcome == Outcome.Victory;
            res.timedOut = b.Outcome == Outcome.None;
            res.rounds = b.Round;
            res.partyDeaths = b.Party.Count(p => !p.Alive);
            res.hpLeft = (float)b.Party.Sum(p => Math.Max(0, p.unit.hp)) / b.Party.Sum(p => p.Stats.maxHp);
            return res;
        }

        // Per-tier encounters used by the AI stage benchmark (Stage 0 baseline & Stages 1-10 gates).
        public static readonly (string label, string encounterId, int level)[] AiTierBenchmarks =
        {
            ("Normal (d1_e5)", "d1_e5", 4),
            ("Elite (d1_e8)", "d1_e8", 7),
            ("Miniboss (d1_e7)", "d1_e7", 9),
            ("Boss (d1_boss)", "d1_boss", 11),
        };

        public sealed class AiTierSummary
        {
            public string label;
            public string execution;
            public int seeds;
            public double completionRate;
            public double avgDeaths;
            public double avgRounds;
            public double wastedActionRate;
            public double maxHeroTargetShare;
            public double repeatedActionRate;
            public double adaptationResponseRate;
            public double telegraphInterruptRate;
            public double telegraphCounterRate;
            public double mpEfficiency;
            public double enemySurvivalRounds;
            public double overkillRate;
            public double coordHealEfficiency;
            public double playerCounterplayRate;
            public double roleIdentityRate;
            public double avgDecisionMs;
            public double maxDecisionMs;
            public double maxRoundDecisionMs;
        }

        public static AiTierSummary EvaluateAiTier(string label, string encounterId, int level, Execution ex, int seeds = 200, Gear gear = Gear.D1WeaponArmor)
        {
            var rs = Enumerable.Range(1, seeds)
                .Select(s => Fight(encounterId, level, Build.Recommended, ex, s, gear))
                .ToList();

            int totalActions = Math.Max(1, rs.Sum(r => r.enemyActions));
            int totalSingle = rs.Sum(r => r.singleTargetAttacks);
            double avgMaxShare = rs.Where(r => r.singleTargetAttacks > 0)
                .Select(r => (double)r.singleTargetByHeroSlot.Values.Max() / r.singleTargetAttacks)
                .DefaultIfEmpty(0.0).Average();
            int chargesStarted = rs.Sum(r => r.chargesStarted);
            int telegraphed = rs.Sum(r => r.telegraphedActions);
            int totalMaxMp = Math.Max(1, rs.Sum(r => r.totalEnemyMaxMp));
            int totalEnemies = Math.Max(1, rs.Sum(r => r.totalEnemyCount));
            int lethalDmg = rs.Sum(r => r.lethalRoundDamage);
            int healTurns = rs.Sum(r => r.healTurns);
            int dangerous = rs.Sum(r => r.dangerousActions);
            int decisions = Math.Max(1, rs.Sum(r => r.aiDecisions));
            int roleDecisions = Math.Max(1, rs.Sum(r => r.totalRoleDecisions));

            return new AiTierSummary
            {
                label = label,
                execution = ex.name,
                seeds = seeds,
                completionRate = 100.0 * rs.Count(r => r.won) / seeds,
                avgDeaths = rs.Average(r => r.partyDeaths),
                avgRounds = rs.Average(r => r.rounds),
                wastedActionRate = 100.0 * rs.Sum(r => r.wastedEnemyActions) / totalActions,
                maxHeroTargetShare = 100.0 * avgMaxShare,
                repeatedActionRate = 100.0 * rs.Sum(r => r.repeatedEnemyActions) / totalActions,
                adaptationResponseRate = 100.0 * rs.Sum(r => r.adaptationDecisions) / decisions,
                telegraphInterruptRate = chargesStarted > 0 ? 100.0 * rs.Sum(r => r.chargesInterrupted) / chargesStarted : 0.0,
                telegraphCounterRate = telegraphed > 0 ? 100.0 * rs.Sum(r => r.telegraphedGuardedOrInterrupted) / telegraphed : 0.0,
                mpEfficiency = 100.0 * rs.Sum(r => r.totalEnemyMpSpent) / totalMaxMp,
                enemySurvivalRounds = (double)rs.Sum(r => r.totalEnemyAliveRounds) / totalEnemies,
                overkillRate = lethalDmg > 0 ? 100.0 * rs.Sum(r => r.lethalRoundOverkill) / lethalDmg : 0.0,
                coordHealEfficiency = healTurns > 0 ? 100.0 * rs.Sum(r => r.usefulHealTurns) / healTurns : 100.0,
                playerCounterplayRate = dangerous > 0 ? 100.0 * rs.Sum(r => r.dangerousTelegraphed) / dangerous : 100.0,
                roleIdentityRate = 100.0 * rs.Sum(r => r.roleAlignedDecisions) / roleDecisions,
                avgDecisionMs = rs.Sum(r => r.totalDecisionMs) / decisions,
                maxDecisionMs = rs.Max(r => r.maxDecisionMs),
                maxRoundDecisionMs = rs.Max(r => r.maxRoundDecisionMs),
            };
        }

        [Test, Explicit("AI stage benchmark (200 seeds per tier, All Good & All Poor): run via -testFilter")]
        public void AiMetricsReport()
        {
            const int seeds = 200;
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"# Enemy AI Metrics Report ({DateTime.Now:yyyy-MM-dd}, {seeds} seeds/row, D1WeaponArmor)");
            sb.AppendLine();
            sb.AppendLine("| Tier | QTE | Win% | Deaths | Rnds | Waste% | TopTgt% | Repeat% | Adapt% | ChgInt% | TelCnt% | MP% | SurvR | Overkill% | Counter% | PlanMs(avg/max) | RndMaxMs |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

            var scripts = new[] { Executions[1], Executions[3] }; // All Good, All Poor (Miss)
            foreach (var (label, encId, level) in AiTierBenchmarks)
            foreach (var ex in scripts)
            {
                var m = EvaluateAiTier(label, encId, level, ex, seeds);
                sb.AppendLine(string.Format(inv,
                    "| {0} | {1} | {2:0.0} | {3:0.00} | {4:0.0} | {5:0.0} | {6:0.0} | {7:0.0} | {8:0.0} | {9:0.0} | {10:0.0} | {11:0.0} | {12:0.0} | {13:0.0} | {14:0.0} | {15:0.000}/{16:0.000} | {17:0.000} |",
                    m.label, m.execution, m.completionRate, m.avgDeaths, m.avgRounds, m.wastedActionRate,
                    m.maxHeroTargetShare, m.repeatedActionRate, m.adaptationResponseRate, m.telegraphInterruptRate,
                    m.telegraphCounterRate, m.mpEfficiency, m.enemySurvivalRounds, m.overkillRate,
                    m.playerCounterplayRate, m.avgDecisionMs, m.maxDecisionMs, m.maxRoundDecisionMs));
            }
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "AiMetricsReport.md"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
            TestContext.WriteLine(sb.ToString());
        }

        [Test]
        public void ZoroaDoubleTurnAndSummonSimulation()
        {
            const int seeds = 50;
            const int level = 11;
            var good = Enumerable.Range(1, seeds).Select(s => Fight("d1_boss", level, Build.Recommended, Executions[1], s, Gear.D1WeaponArmor)).ToList();
            var miss = Enumerable.Range(1, seeds).Select(s => Fight("d1_boss", level, Build.Recommended, Executions[3], s, Gear.D1WeaponArmor)).ToList();

            TestContext.WriteLine($"Zoroa L{level} (D1WeaponArmor, {seeds} seeds):");
            TestContext.WriteLine($"  All Good: win={100f * good.Count(r => r.won) / seeds:0}%, avgRounds={good.Average(r => r.rounds):0.1}, avgDeaths={good.Average(r => r.partyDeaths):0.2}, dmgTaken/round={good.Average(r => (double)r.damageTaken / Math.Max(1, r.rounds)):0.1}, avgSummons={good.Average(r => r.summonsSpawned):0.2}");
            TestContext.WriteLine($"  All Miss: win={100f * miss.Count(r => r.won) / seeds:0}%, avgRounds={miss.Average(r => r.rounds):0.1}, avgDeaths={miss.Average(r => r.partyDeaths):0.2}, dmgTaken/round={miss.Average(r => (double)r.damageTaken / Math.Max(1, r.rounds)):0.1}, avgSummons={miss.Average(r => r.summonsSpawned):0.2}");

            Assert.Greater(miss.Average(r => r.partyDeaths), good.Average(r => r.partyDeaths));
            Assert.Greater(good.Average(r => r.summonsSpawned), 0);
        }

        // ---- EXP pacing (RunConfig) ----
        // Walks paths through the real generated maps for one dungeon (all its stages) and returns the EXP a standing
        // hero earns from every combat node on the way (stage bosses use their encounter's tier).
        public enum PathStyle { FightLight, Random, FightHeavy }

        static int PathExp(int act, int seed, PathStyle style) => PathTotals(act, seed, style).exp;

        // EXP and gold along one path. Gold uses the middle of each RunConfig range and counts fights and treasure
        // only (no events, modifiers, relic fallbacks or shop spending).
        // expBefore: if given, records the EXP held on first entering each encounter (feeds the sim's encounter levels).
        static (int exp, int gold) PathTotals(int act, int seed, PathStyle style, Dictionary<string, int> expBefore = null)
        {
            var rng = new Random(unchecked(seed * 977 + act));
            int exp = 0, gold = 0;
            for (int stage = 1; stage <= MapGenerator.GetStageCount(act); stage++)
            {
                var map = MapGenerator.Generate(seed, act, stage);
                var options = map.nodes.Where(n => n.floor == 0).ToList();
                while (options.Count > 0)
                {
                    var node = Pick(options, style, rng);
                    if (node.IsCombat)
                    {
                        var tier = Database.Encounters[node.encounter].tier;
                        if (expBefore != null && !expBefore.ContainsKey(node.encounter)) expBefore[node.encounter] = exp;
                        exp += RunConfig.ExpFor(tier);
                        gold += tier == Tier.Boss ? RunConfig.BossGold : Mid(tier == Tier.Elite ? RunConfig.EliteGold : RunConfig.BattleGold);
                    }
                    else if (node.type == NodeType.Treasure) gold += Mid(RunConfig.TreasureGold);
                    options = node.next.Select(map.Get).ToList();
                }
            }
            return (exp, gold);
        }

        static int Mid((int min, int max) range) => (range.min + range.max) / 2;

        static NodeDef Pick(List<NodeDef> options, PathStyle style, Random rng)
        {
            var preferred = style == PathStyle.FightHeavy ? options.Where(n => n.IsCombat).ToList()
                : style == PathStyle.FightLight ? options.Where(n => !n.IsCombat).ToList()
                : options;
            var pool = preferred.Count > 0 ? preferred : options;
            return pool[rng.Next(pool.Count)];
        }

        public static int LevelFor(int totalExp)
        {
            int level = 1;
            while (level < RunConfig.MaxLevel && totalExp >= RunConfig.ExpToNext(level))
                totalExp -= RunConfig.ExpToNext(level++);
            return level;
        }

        // Target (user decision, 2026-11-20): a random path through Dungeon 1 ends at level 13 or higher.
        [Test]
        public void RandomPathThroughDungeon1ReachesLevel13()
        {
            var levels = Enumerable.Range(1, 200).Select(seed => LevelFor(PathExp(1, seed, PathStyle.Random))).OrderBy(l => l).ToList();
            Assert.GreaterOrEqual(levels[levels.Count / 2], 13, "median random path through Dungeon 1");
        }

        [Test, Explicit("EXP pacing report: run on demand with -testFilter")]
        public void ExpPacingReport()
        {
            const int seeds = 200;
            var sb = new StringBuilder();
            sb.AppendLine("# EXP pacing — Dungeons 1 and 2");
            sb.AppendLine();
            sb.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}. {seeds} generated maps per row. EXP per fight: Normal {RunConfig.BattleExp}, " +
                          $"Elite {RunConfig.EliteExp}, Boss {RunConfig.BossExp}. Level 13 needs {Enumerable.Range(1, 12).Sum(RunConfig.ExpToNext)} EXP, " +
                          $"level 15 needs {Enumerable.Range(1, 14).Sum(RunConfig.ExpToNext)}. Assumes the hero is standing at the end of every fight.");
            sb.AppendLine();
            sb.AppendLine("| Path | D1 EXP (avg) | Level after D1 (min / median / max) | D2 EXP (avg) | Level after D2 (min / median / max) |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (PathStyle style in Enum.GetValues(typeof(PathStyle)))
            {
                var d1 = Enumerable.Range(1, seeds).Select(s => PathExp(1, s, style)).ToList();
                var d2 = Enumerable.Range(1, seeds).Select(s => PathExp(2, s, style)).ToList();
                var l1 = d1.Select(LevelFor).OrderBy(l => l).ToList();
                var l2 = d1.Zip(d2, (a, b) => LevelFor(a + b)).OrderBy(l => l).ToList();
                sb.AppendLine($"| {style} | {d1.Average():0} | {l1.First()} / {l1[seeds / 2]} / {l1.Last()} | {d2.Average():0} | {l2.First()} / {l2[seeds / 2]} / {l2.Last()} |");
            }

            // Source of BalanceSimulation.Encounters levels: the level a hero has on first entering each encounter.
            sb.AppendLine();
            sb.AppendLine("## Level on entering each sim encounter (Random path; paths that never meet it are left out)");
            sb.AppendLine();
            sb.AppendLine("| Encounter | Paths | Level (min / median / max) |");
            sb.AppendLine("|---|---:|---:|");
            var seen = Enumerable.Range(1, seeds).Select(s => { var d = new Dictionary<string, int>(); PathTotals(1, s, PathStyle.Random, d); return d; }).ToList();
            foreach (var id in Encounters.Select(e => e.id).Distinct())
            {
                var lv = seen.Where(d => d.ContainsKey(id)).Select(d => LevelFor(d[id])).OrderBy(l => l).ToList();
                sb.AppendLine(lv.Count == 0 ? $"| {id} | 0 | - |" : $"| {id} | {lv.Count} | {lv.First()} / {lv[lv.Count / 2]} / {lv.Last()} |");
            }

            sb.AppendLine();
            sb.AppendLine($"## Gold (fights and treasure only). Battle {RunConfig.BattleGold.min}-{RunConfig.BattleGold.max}, " +
                          $"Elite {RunConfig.EliteGold.min}-{RunConfig.EliteGold.max}, Treasure {RunConfig.TreasureGold.min}-{RunConfig.TreasureGold.max}, Boss {RunConfig.BossGold}");
            sb.AppendLine();
            sb.AppendLine($"For scale: full-party Temple Recovery {4 * HubEconomy.TempleRecoveryCost}, one Revive {HubEconomy.TempleReviveCost}, starting gold {PersistenceCommitHandler.StarterGold}.");
            sb.AppendLine();
            sb.AppendLine("| Path | D1 gold (min / avg / max) | D2 gold (avg) |");
            sb.AppendLine("|---|---:|---:|");
            foreach (PathStyle style in Enum.GetValues(typeof(PathStyle)))
            {
                var g1 = Enumerable.Range(1, seeds).Select(s => PathTotals(1, s, style).gold).ToList();
                var g2 = Enumerable.Range(1, seeds).Select(s => PathTotals(2, s, style).gold).ToList();
                sb.AppendLine($"| {style} | {g1.Min()} / {g1.Average():0} / {g1.Max()} | {g2.Average():0} |");
            }
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ExpPacing.md"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
            TestContext.WriteLine(sb.ToString());
        }

        [Test]
        public void SimulatedFightReachesAnOutcome()
        {
            var r = Fight("d1_e1", 1, Build.Recommended, Executions[1], 1);
            Assert.Greater(r.playerTurns, 0);
            Assert.IsTrue(r.won || r.rounds > 0);
        }

        [Test, Explicit("Balance report, slow: run on demand with -testFilter")]
        public void Report()
        {
            const int runs = 100;
            var sb = new StringBuilder();
            sb.AppendLine("# Balance simulation — Dungeon 1");
            sb.AppendLine();
            sb.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}. {runs} fights per row. Party: Human Fighter, Dwarf Cleric, Elf Wizard, Halfling Rogue.");
            sb.AppendLine("Every fight starts at full HP/MP with no items, relics or modifiers. Hero levels per encounter are estimates.");
            sb.AppendLine($"Current values: offense {QteConfig.PoorPower}/{QteConfig.GoodPower}/{QteConfig.PerfectPower}, " +
                          $"defense taken at Block 50: Poor {QteConfig.PoorDefense} / Good {QteConfig.DefenseTaken(QteResult.Good, false, 0.5f)} / " +
                          $"Perfect {QteConfig.DefenseTaken(QteResult.Perfect, false, 0.5f)} (scales with Block Power; full parry only with Guard), " +
                          $"Perfect MP refund {QteConfig.PerfectMpRefund}.");

            // Policy check (GDD Bab 12 §3.5): pass if AttackOnly does worse than Tactical with the same execution.
            sb.AppendLine();
            sb.AppendLine("## Policy: Tactical (skills, heals, Guard) vs AttackOnly (basic attack + parry), Recommended build, Starter gear");
            sb.AppendLine();
            sb.AppendLine("| Encounter | Level | Execution | Policy | Win % | Party HP left (wins) | Avg rounds | Guard turns |");
            sb.AppendLine("|---|---:|---|---|---:|---:|---:|---:|");
            foreach (var (id, level) in Encounters)
            foreach (var ex in Executions.Where(e => e.name == "All Perfect" || e.name.StartsWith("Typical")))
            foreach (Policy policy in Enum.GetValues(typeof(Policy)))
            {
                var rs = Enumerable.Range(1, runs).Select(seed => Fight(id, level, Build.Recommended, ex, seed, Gear.Starter, policy)).ToList();
                var wins = rs.Where(r => r.won).ToList();
                int turns = Math.Max(1, rs.Sum(r => r.playerTurns));
                sb.AppendLine($"| {id} | {level} | {ex.name} | {policy} | {100f * wins.Count / runs:0} | " +
                              $"{(wins.Count > 0 ? wins.Average(r => r.hpLeft) * 100f : 0f):0}% | {rs.Average(r => r.rounds):0.0} | " +
                              $"{100f * rs.Sum(r => r.guardTurns) / turns:0}% |");
            }
            foreach (var (id, level) in Encounters)
            {
                sb.AppendLine();
                sb.AppendLine($"## {Database.Encounters[id].name} ({id}), heroes at level {level}");
                sb.AppendLine();
                sb.AppendLine("| Gear | Build | Execution | Win % | Timeout % | Avg rounds | Party HP left (wins) | Damage per hero turn | Skill turns | Guard turns |");
                sb.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---:|---:|");
                foreach (Gear gear in Enum.GetValues(typeof(Gear)))
                foreach (Build build in Enum.GetValues(typeof(Build)))
                    foreach (var ex in Executions)
                    {
                        var rs = Enumerable.Range(1, runs).Select(seed => Fight(id, level, build, ex, seed, gear)).ToList();
                        var wins = rs.Where(r => r.won).ToList();
                        int turns = Math.Max(1, rs.Sum(r => r.playerTurns));
                        sb.AppendLine($"| {gear} | {build} | {ex.name} | {100f * wins.Count / runs:0} | {100f * rs.Count(r => r.timedOut) / runs:0} | {rs.Average(r => r.rounds):0.0} | " +
                                      $"{(wins.Count > 0 ? wins.Average(r => r.hpLeft) * 100f : 0f):0}% | {(float)rs.Sum(r => r.damageDealt) / turns:0} | " +
                                      $"{100f * rs.Sum(r => r.skillTurns) / turns:0}% | {100f * rs.Sum(r => r.guardTurns) / turns:0}% |");
                    }
            }

            sb.AppendLine();
            sb.AppendLine("## D1 gear picked per hero (Uncommon, level 2)");
            sb.AppendLine();
            foreach (var p in PartyPlan)
            {
                var picks = AllSlots
                    .Select(slot => (slot, id: BestItem(p, Build.Recommended, slot)))
                    .Where(x => x.id != null)
                    .Select(x => $"{x.slot}: {Database.Equipment[x.id].name}");
                sb.AppendLine($"- {p.cls}: {string.Join(", ", picks)}");
            }

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "BalanceSim.md"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
            TestContext.WriteLine(sb.ToString());
        }

        // ---- Modifier-loadout & Stage 2-3 escalation benchmarks ----

        // Realistic accumulated combat modifier stacks along a Dungeon 1 path (when every non-final battle offers 3 modifiers).
        public static readonly (string modId, ElementType elem)[] Stage2ModifierLoadout =
        {
            ("MOD-001", ElementType.None), ("MOD-001", ElementType.None), // Battle Focus x2 (+10% Phys)
            ("MOD-002", ElementType.None), ("MOD-002", ElementType.None), // Arcane Current x2 (+10% Elem)
            ("MOD-003", ElementType.None),                                // Piercing Insight x1 (-5% Phys DEF)
            ("MOD-006", ElementType.None), ("MOD-006", ElementType.None), // Inspiring Formation x2 (-10% DR)
            ("MOD-007", ElementType.None),                                // Vital Spark x1 (+10% Heal)
            ("MOD-008", ElementType.None),                                // Mana Well x1 (-5% MP)
            ("MOD-013", ElementType.None),                                // Iron Guard x1 (-8% DR)
        };

        public static readonly (string modId, ElementType elem)[] Stage3BossModifierLoadout =
        {
            ("MOD-001", ElementType.None), ("MOD-001", ElementType.None), ("MOD-001", ElementType.None), // Battle Focus x3 (+15% Phys)
            ("MOD-002", ElementType.None), ("MOD-002", ElementType.None), ("MOD-002", ElementType.None), // Arcane Current x3 (+15% Elem)
            ("MOD-003", ElementType.None), ("MOD-003", ElementType.None),                                // Piercing Insight x2 (-10% Phys DEF)
            ("MOD-006", ElementType.None), ("MOD-006", ElementType.None),                                // Inspiring Formation x2
            ("MOD-007", ElementType.None), ("MOD-007", ElementType.None),                                // Vital Spark x2 (+20% Heal)
            ("MOD-008", ElementType.None), ("MOD-008", ElementType.None),                                // Mana Well x2 (-10% MP)
            ("MOD-013", ElementType.None),                                                               // Iron Guard x1 (total DR ~17%)
            ("MOD-022", ElementType.None),                                                               // Marked Prey x1 (+10% vs Broken)
        };

        [Test]
        public void ZoroaWithStage3ModifiersAndEscalation_RemainsTactical()
        {
            const int seeds = 40;
            const int level = 12;
            var good = Enumerable.Range(1, seeds)
                .Select(s => Fight("d1_boss", level, Build.Recommended, Executions[1], s, Gear.D1FullSet, stage: 3, modifiers: Stage3BossModifierLoadout))
                .ToList();
            var poor = Enumerable.Range(1, seeds)
                .Select(s => Fight("d1_boss", level, Build.Recommended, Executions[3], s, Gear.D1FullSet, stage: 3, modifiers: Stage3BossModifierLoadout))
                .ToList();

            double goodWin = 100.0 * good.Count(r => r.won) / seeds;
            double goodRounds = good.Average(r => r.rounds);
            double goodDeaths = good.Average(r => r.partyDeaths);
            double poorDeaths = poor.Average(r => r.partyDeaths);

            TestContext.WriteLine($"Zoroa Stage 3 + {Stage3BossModifierLoadout.Length} mods (L{level}, D1FullSet, {seeds} seeds):");
            TestContext.WriteLine($"  All Good: win={goodWin:0}%, avgRounds={goodRounds:0.0}, avgDeaths={goodDeaths:0.00}");
            TestContext.WriteLine($"  All Poor: win={100.0 * poor.Count(r => r.won) / seeds:0}%, avgRounds={poor.Average(r => r.rounds):0.0}, avgDeaths={poorDeaths:0.00}");

            Assert.GreaterOrEqual(goodRounds, 8.0, "Stage 3 Zoroa must not fold into a trivial burst stomp");
            Assert.Greater(poorDeaths, goodDeaths, "Sloppy QTEs must still be punished even with 16 active modifiers");
        }

        [Test, Explicit("Modifier & Stage 2-3 escalation benchmark (50 seeds/row): run via -testFilter")]
        public void ModifierBalanceReport()
        {
            const int seeds = 50;
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"# Modifier & Stage Escalation Balance Report ({DateTime.Now:yyyy-MM-dd}, {seeds} seeds/row)");
            sb.AppendLine();
            sb.AppendLine($"Stage 2 Mult: HP x{EnemyAffixSystem.Stage2HpMult}, ATK x{EnemyAffixSystem.Stage2AtkMult}, Break +{EnemyAffixSystem.Stage2BreakPowerBonus} (Elites: 1 hidden affix)");
            sb.AppendLine($"Stage 3 Mult: HP x{EnemyAffixSystem.Stage3HpMult}, ATK x{EnemyAffixSystem.Stage3AtkMult}, Break +{EnemyAffixSystem.Stage3BreakPowerBonus} (Elites: 1, Boss: 2 hidden affixes)");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Enc | Lv | Gear | Stage | Mods | QTE | Win% | Avg Rnds | Avg Deaths | HP Left (wins) |");
            sb.AppendLine("|---|---|---:|---|---:|---:|---|---:|---:|---:|---:|");

            var scenarios = new (string name, string enc, int lv, Gear gear, int stage, (string, ElementType)[] mods)[]
            {
                ("Baseline (no mods)", "d1_e8", 7, Gear.D1WeaponArmor, 1, null),
                ("S2 Elite unscaled + mods", "d1_e8", 7, Gear.D1WeaponArmor, 1, Stage2ModifierLoadout),
                ("S2 Elite SCALED + mods", "d1_e8", 7, Gear.D1WeaponArmor, 2, Stage2ModifierLoadout),
                ("Baseline (no mods)", "d1_e7", 9, Gear.D1WeaponArmor, 1, null),
                ("S2 Boss unscaled + mods", "d1_e7", 9, Gear.D1WeaponArmor, 1, Stage2ModifierLoadout),
                ("S2 Boss SCALED + mods", "d1_e7", 9, Gear.D1WeaponArmor, 2, Stage2ModifierLoadout),
                ("Baseline (no mods)", "d1_boss", 12, Gear.D1FullSet, 1, null),
                ("S3 Zoroa unscaled + mods", "d1_boss", 12, Gear.D1FullSet, 1, Stage3BossModifierLoadout),
                ("S3 Zoroa SCALED + mods", "d1_boss", 12, Gear.D1FullSet, 3, Stage3BossModifierLoadout),
            };

            var scripts = new[] { Executions[1], Executions[2], Executions[3] }; // All Good, Typical, All Poor
            foreach (var sc in scenarios)
            foreach (var ex in scripts)
            {
                var rs = Enumerable.Range(1, seeds)
                    .Select(s => Fight(sc.enc, sc.lv, Build.Recommended, ex, s, sc.gear, stage: sc.stage, modifiers: sc.mods))
                    .ToList();
                var wins = rs.Where(r => r.won).ToList();
                double hpLeft = wins.Count > 0 ? wins.Average(r => r.hpLeft) * 100.0 : 0.0;
                sb.AppendLine(string.Format(inv,
                    "| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7:0.0} | {8:0.0} | {9:0.00} | {10:0}% |",
                    sc.name, sc.enc, sc.lv, sc.gear, sc.stage, sc.mods?.Length ?? 0, ex.name,
                    100.0 * wins.Count / seeds, rs.Average(r => r.rounds), rs.Average(r => r.partyDeaths), hpLeft));
            }

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ModifierBalanceSim.md"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
            if (TestContext.CurrentContext != null)
                TestContext.WriteLine(sb.ToString());
        }
    }
}
