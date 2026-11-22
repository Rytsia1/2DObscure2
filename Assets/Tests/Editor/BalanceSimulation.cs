using System;
using System.Collections.Generic;
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
            public int rounds, playerTurns, skillTurns, guardTurns, damageDealt, damageTaken, partyDeaths, summonsSpawned;
            public float hpLeft;
        }

        public static Result Fight(string encounterId, int level, Build build, Execution ex, int seed, Gear gear = Gear.Starter,
            Policy policy = Policy.Tactical)
        {
            var party = PartyPlan.Select(p => MakeHero(p, build, level, gear)).ToList();
            var enc = Database.Encounters[encounterId];
            var qteRng = new Random(seed);
            var b = new Battle(party, enc.enemies.Select(id => Database.Enemies[id]), new List<string>(), new Random(unchecked(seed * 31 + 7)));
            var res = new Result();

            for (int steps = 0; steps < 10000 && b.Outcome == Outcome.None && b.Round <= MaxRounds; steps++)
            {
                var c = b.NextTurn();
                if (c == null) continue;
                if (c.IsEnemy)
                {
                    var act = EnemyAI.Choose(b, c);
                    if (act == null) continue;
                    var qte = QteFactory.Defense(act, QteAssist.Standard, null, qteRng);
                    List<QteResult> defense = null;
                    if (qte is WardQte ward) defense = Enumerable.Repeat(ex.Roll(qteRng), ward.Results.Count).ToList(); // one typed ward covers every hit
                    else if (qte is DefenseQte d) defense = Enumerable.Range(0, d.Hits).Select(_ => ex.Roll(qteRng)).ToList();
                    else if (qte is StruggleQte st) defense = Enumerable.Repeat(ex.Roll(qteRng), st.hits).ToList(); // one gauge covers every hit
                    int enemiesBefore = b.Enemies.Count;
                    var hits = b.Perform(act, 1f, defense);
                    res.summonsSpawned += Math.Max(0, b.Enemies.Count - enemiesBefore);
                    res.damageTaken += hits.Where(h => !h.heal && h.target != null && h.target.IsPlayer).Sum(h => Math.Max(0, h.amount));
                }
                else
                {
                    var act = Choose(b, c, policy);
                    res.playerTurns++;
                    if (act.kind == ActionKind.Skill) res.skillTurns++;
                    if (act.kind == ActionKind.Guard) res.guardTurns++;
                    float power = QteFactory.KindFor(act) == OffenseQteKind.None ? 1f : Battle.QtePower(act, ex.Roll(qteRng));
                    res.damageDealt += b.Perform(act, power).Where(h => !h.heal && h.target != null && h.target.IsEnemy).Sum(h => Math.Max(0, h.amount));
                }
            }

            res.won = b.Outcome == Outcome.Victory;
            res.timedOut = b.Outcome == Outcome.None;
            res.rounds = b.Round;
            res.partyDeaths = b.Party.Count(p => !p.Alive);
            res.hpLeft = (float)b.Party.Sum(p => Math.Max(0, p.unit.hp)) / b.Party.Sum(p => p.Stats.maxHp);
            return res;
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
        }    }
}
