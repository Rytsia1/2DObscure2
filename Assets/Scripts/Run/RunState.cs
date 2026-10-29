using System;
using System.Collections.Generic;
using System.Linq;

// Runtime state for one run. Pure logic: no rendering, no input.
// Combat state (Battle/Combatant) is separate and temporary; only HP/MP on the Units carry back.
namespace Obscure
{
    public class Unit
    {
        public readonly string id, name, role;
        public readonly string[] sprite, skills;
        public readonly Affinities affinities;
        public readonly bool isEnemyUnit;

        public RaceDef race;
        public ClassDef classDef;
        public BackgroundDef background;
        public string starterPassiveId;
        public int level = 1;
        public int unspentAsiPoints;

        public readonly AbilityScoreSheet abilities = new AbilityScoreSheet();
        public readonly SkillLoadout skillLoadout = new SkillLoadout();
        public readonly Dictionary<EquipmentSlot, EquipmentDef> equipped = new Dictionary<EquipmentSlot, EquipmentDef>();
        public readonly List<PassiveDef> passives = new List<PassiveDef>();
        public readonly List<StatModifier> permanentModifiers = new List<StatModifier>();
        public readonly List<StatModifier> temporaryModifiers = new List<StatModifier>();

        Stats permanentLegacyBonuses;
        public DerivedStats Derived { get; private set; }
        public Stats stats;
        public int hp, mp, shield;
        public bool Alive => hp > 0;

        public Unit(CharacterDef d)
        {
            id = d.id;
            name = d.name;
            role = d.role;
            sprite = d.sprite;
            skills = d.skills ?? Array.Empty<string>();
            affinities = d.affinities ?? Affinities.None;
            isEnemyUnit = false;

            race = d.raceId != null && Database.Races.TryGetValue(d.raceId, out var r) ? r : Database.Races["human"];
            classDef = d.classId != null && Database.Classes.TryGetValue(d.classId, out var c) ? c : Database.Classes["knight"];
            background = d.backgroundId != null && Database.Backgrounds.TryGetValue(d.backgroundId, out var bg) ? bg : Database.Backgrounds["oathsworn"];

            abilities.baseScores = d.baseScores.Equals(default) ? AbilityScores.DefaultBase : d.baseScores;
            abilities.racialModifiers = race.abilityModifiers;

            skillLoadout.InitializeForClass(classDef, level);
            if (d.unlockedSkills != null)
            {
                foreach (var sid in d.unlockedSkills)
                {
                    if (Database.Skills.ContainsKey(sid)) skillLoadout.UnlockActive(sid);
                    if (Database.Passives.ContainsKey(sid)) skillLoadout.UnlockPassive(sid);
                }
            }
            if (d.activeSkillLoadout != null)
            {
                for (int i = 0; i < Math.Min(SkillLoadout.ActiveSlotCount, d.activeSkillLoadout.Length); i++)
                    skillLoadout.activeSlots[i] = d.activeSkillLoadout[i];
            }
            if (d.passiveSkillLoadout != null)
            {
                for (int i = 0; i < Math.Min(SkillLoadout.PassiveSlotCount, d.passiveSkillLoadout.Length); i++)
                    skillLoadout.passiveSlots[i] = d.passiveSkillLoadout[i];
            }

            starterPassiveId = d.starterPassiveId ?? (classDef != null && classDef.isPlayable ? classDef.starterPassiveId : null);
            if (!string.IsNullOrEmpty(starterPassiveId))
            {
                skillLoadout.UnlockPassive(starterPassiveId);
                if (!skillLoadout.passiveSlots.Contains(starterPassiveId) && string.IsNullOrEmpty(skillLoadout.passiveSlots[0]))
                    skillLoadout.passiveSlots[0] = starterPassiveId;
                if (Database.Passives.TryGetValue(starterPassiveId, out var sp) && !passives.Contains(sp))
                    passives.Add(sp);
            }

            bool includeLoadoutPassives = (classDef != null && classDef.isPlayable) ||
                                          !string.IsNullOrEmpty(d.starterPassiveId) ||
                                          (d.passiveSkillLoadout != null && d.passiveSkillLoadout.Length > 0);
            if (includeLoadoutPassives)
            {
                foreach (var pid in skillLoadout.GetEquippedPassives())
                    if (Database.Passives.TryGetValue(pid, out var lp) && !passives.Contains(lp))
                        passives.Add(lp);
            }
            else
            {
                Array.Clear(skillLoadout.passiveSlots, 0, skillLoadout.passiveSlots.Length);
            }

            if (d.passives != null)
                foreach (var pid in d.passives)
                    if (Database.Passives.TryGetValue(pid, out var p) && !passives.Contains(p)) passives.Add(p);

            if (d.startingEquipment != null)
                foreach (var eid in d.startingEquipment)
                    if (Database.Equipment.TryGetValue(eid, out var eq)) equipped[eq.slot] = eq;

            if (d.equipmentLoadout != null)
                foreach (var assign in d.equipmentLoadout)
                    if (!string.IsNullOrEmpty(assign.equipmentId) && Database.Equipment.TryGetValue(assign.equipmentId, out var eq))
                        equipped[assign.slot] = eq;

            RecalculateStats(fillResources: true);
        }

        public Unit(EnemyDef d)
        {
            id = d.id;
            name = d.name;
            role = d.tier.ToString();
            sprite = d.sprite;
            skills = Array.Empty<string>();
            affinities = d.affinities ?? Affinities.None;
            isEnemyUnit = true;

            abilities.baseScores = d.abilityScores.Equals(default) ? AbilityScores.Uniform(10) : d.abilityScores;
            stats = d.stats;
            var mods = abilities.AbilityModifiers;
            int mDef = d.magicDef >= 0 ? d.magicDef : d.stats.def;
            Derived = new DerivedStats
            {
                maxHp = d.stats.maxHp,
                maxMp = d.stats.maxMp,
                maxShield = 0,
                physicalPower = d.stats.atk,
                rangedPower = d.stats.atk,
                magicPower = d.stats.mag > 0 ? d.stats.mag : d.stats.atk,
                healingPower = d.stats.mag > 0 ? d.stats.mag : d.stats.atk,
                supportPower = 10 + mods.cha * 2,
                criticalChance = Math.Max(0, 5 + mods.dex * 2),
                criticalDamage = 150,
                armorPenetration = Math.Max(0, mods.str),
                magicPenetration = Math.Max(0, mods.intel),
                physicalDefense = d.stats.def,
                magicDefense = mDef,
                evasion = Math.Max(0, 5 + mods.dex * 2),
                blockPower = 50,
                statusResistance = Math.Max(0, (mods.vit + mods.wis) * 3),
                staggerResistance = d.breakResistance + Math.Max(0, mods.vit * 2),
                initiative = d.stats.spd,
                actionSpeed = 100 + mods.dex * 5,
                qteTimingBonus = 0,
                qteStability = 0,
                counterPower = 10,
                physicalBreakPower = 10 + mods.str * 2,
                magicBreakPower = 10 + mods.intel * 2,
            };
            hp = stats.maxHp;
            mp = stats.maxMp;
        }

        public AbilityScores BaseScores => abilities.baseScores;
        public AbilityScores RacialModifiers => abilities.racialModifiers;
        public AbilityScores PermanentAbilityBonuses => abilities.permanentBonuses;
        public AbilityScores EquipmentAbilityBonuses => abilities.equipmentBonuses;
        public AbilityScores TemporaryAbilityModifiers => abilities.temporaryModifiers;
        public AbilityScores FinalScores => abilities.FinalScores;
        public AbilityScores AbilityModifiers => abilities.AbilityModifiers;

        public int Score(Ability a) => abilities.Score(a);
        public int Mod(Ability a) => abilities.Mod(a);

        public List<StatModifier> GatherAllDerivedModifiers()
        {
            var list = new List<StatModifier>();
            if (race?.passiveBonuses != null) list.AddRange(race.passiveBonuses);
            if (background?.passiveEffects != null) list.AddRange(background.passiveEffects);
            foreach (var p in passives)
                if (p.modifiers != null) list.AddRange(p.modifiers);
            list.AddRange(permanentModifiers);
            list.AddRange(temporaryModifiers);
            return list;
        }

        // Recomputes Equipment Ability Bonuses, Final Ability Scores, Ability Modifiers, and all Derived Stats.
        public void RecalculateStats(bool fillResources = false)
        {
            if (isEnemyUnit)
            {
                var mods = abilities.AbilityModifiers;
                var d = Derived;
                d.maxHp = stats.maxHp;
                d.maxMp = stats.maxMp;
                d.physicalPower = stats.atk;
                d.physicalDefense = stats.def;
                d.initiative = stats.spd;
                if (stats.mag > 0) d.magicPower = stats.mag;
                d.staggerResistance = Math.Max(0, d.staggerResistance);
                Derived = d;
                return;
            }

            var eqAbil = AbilityScores.Zero;
            foreach (var eq in equipped.Values)
                if (eq != null) eqAbil = eqAbil.Plus(eq.abilityBonuses);
            foreach (var p in passives)
                if (p != null) eqAbil = eqAbil.Plus(p.abilityBonuses);
            abilities.equipmentBonuses = eqAbil;
            abilities.racialModifiers = race != null ? race.abilityModifiers : AbilityScores.Zero;

            int oldMaxHp = Derived.maxHp;
            int oldMaxMp = Derived.maxMp;

            Derived = StatCalculator.Calculate(
                abilities,
                classDef,
                level,
                equipped.Values,
                GatherAllDerivedModifiers(),
                permanentLegacyBonuses,
                passives);

            stats = Derived.ToLegacyStats();

            if (fillResources)
            {
                hp = stats.maxHp;
                mp = stats.maxMp;
            }
            else
            {
                if (Alive && stats.maxHp > oldMaxHp && oldMaxHp > 0)
                    hp = Math.Min(stats.maxHp, hp + (stats.maxHp - oldMaxHp));
                else
                    hp = Math.Min(hp, stats.maxHp);

                if (stats.maxMp > oldMaxMp && oldMaxMp > 0)
                    mp = Math.Min(stats.maxMp, mp + (stats.maxMp - oldMaxMp));
                else
                    mp = Math.Min(mp, stats.maxMp);
            }
        }

        public StatBreakdown ExplainStat(DerivedStatId statId, IEnumerable<StatModifier> extraCombatModifiers = null)
        {
            var mods = GatherAllDerivedModifiers();
            if (extraCombatModifiers != null) mods.AddRange(extraCombatModifiers);
            return StatCalculator.Explain(statId, abilities, classDef, level, equipped.Values, mods, permanentLegacyBonuses, passives);
        }

        public bool HasActiveSkill(string skillId) =>
            !string.IsNullOrEmpty(skillId) &&
            ((skills != null && skills.Contains(skillId)) || skillLoadout.activeSlots.Contains(skillId));

        public string ExplainAbility(Ability a)
        {
            var def = Database.AbilityDefs[a];
            int b = abilities.baseScores[a];
            int r = abilities.racialModifiers[a];
            int p = abilities.permanentBonuses[a];
            int eq = abilities.equipmentBonuses[a];
            int tmp = abilities.temporaryModifiers[a];
            int fin = abilities.Score(a);
            int mod = abilities.Mod(a);
            return $"{def.displayName.ToUpperInvariant()} ({a}): {fin} (MOD {AbilityScores.FormatMod(mod)})\n" +
                   $"{def.description.ToUpperInvariant()}\n" +
                   $"SOURCES: BASE {b}  RACE {(r >= 0 ? "+" : "")}{r}  PERM {(p >= 0 ? "+" : "")}{p}  EQP {(eq >= 0 ? "+" : "")}{eq}  BUFF {(tmp >= 0 ? "+" : "")}{tmp}";
        }

        public void Heal(int amount)
        {
            if (Alive) hp = Math.Min(stats.maxHp, hp + amount);
        }

        // Permanent stat gain (relics, upgrades). Max HP/MP gains also fill the new capacity.
        public void Improve(Stats bonus)
        {
            if (isEnemyUnit)
            {
                stats = stats.Plus(bonus);
                if (Alive) hp = Math.Min(stats.maxHp, hp + bonus.maxHp);
                mp = Math.Min(stats.maxMp, mp + bonus.maxMp);
                RecalculateStats();
                return;
            }
            permanentLegacyBonuses = permanentLegacyBonuses.Plus(bonus);
            RecalculateStats();
        }

        public void AddPermanentAbilityBonus(Ability a, int delta)
        {
            abilities.permanentBonuses = abilities.permanentBonuses.Add(a, delta);
            RecalculateStats();
        }

        public void AddPermanentAbilityBonuses(AbilityScores bonus)
        {
            abilities.permanentBonuses = abilities.permanentBonuses.Plus(bonus);
            RecalculateStats();
        }

        public void SetTemporaryAbilityModifiers(AbilityScores temp)
        {
            abilities.temporaryModifiers = temp;
            RecalculateStats();
        }

        public void Equip(EquipmentDef item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            equipped[item.slot] = item;
            RecalculateStats();
        }

        // Validated equip that enforces Strict Weapon Compatibility and item restrictions for any of the 8 slots.
        public bool TryEquip(EquipmentSlot slot, EquipmentDef item, out string error)
        {
            error = EquipmentValidator.Validate(item, slot, classDef, race, level, FinalScores);
            if (error != null) return false;
            equipped[slot] = item;
            RecalculateStats();
            return true;
        }

        public bool TryEquip(EquipmentDef item, out string error)
        {
            if (item == null)
            {
                error = "Equipment item is null.";
                return false;
            }
            return TryEquip(item.slot, item, out error);
        }

        public bool Unequip(EquipmentSlot slot)
        {
            if (!equipped.Remove(slot)) return false;
            RecalculateStats();
            return true;
        }

        public void AddPassive(PassiveDef passive)
        {
            if (passive == null || passives.Contains(passive)) return;
            passives.Add(passive);
            RecalculateStats();
        }

        public void AddPermanentModifier(StatModifier mod)
        {
            if (mod == null) return;
            if (mod.isAbilityModifier)
                abilities.permanentBonuses = abilities.permanentBonuses.Add(mod.ability, (int)Math.Round(mod.value));
            else
                permanentModifiers.Add(mod);
            RecalculateStats();
        }

        public void AddTemporaryModifier(StatModifier mod)
        {
            if (mod == null) return;
            if (mod.isAbilityModifier)
                abilities.temporaryModifiers = abilities.temporaryModifiers.Add(mod.ability, (int)Math.Round(mod.value));
            else
                temporaryModifiers.Add(mod);
            RecalculateStats();
        }

        public void ClearTemporaryModifiers()
        {
            abilities.temporaryModifiers = AbilityScores.Zero;
            temporaryModifiers.Clear();
            RecalculateStats();
        }

        // Advances character level and grants Ability Score Improvement points when reaching configured levels.
        public void LevelUp(AbilityScoreImprovementConfig asiConfig = null)
        {
            var cfg = asiConfig ?? Database.AsiConfig;
            level++;
            skillLoadout.SyncLevelUnlocks(classDef, level);
            if (cfg.GrantsAtLevel(level))
                unspentAsiPoints += cfg.pointsGranted;
            RecalculateStats();
        }

        // Spends an ASI point to permanently raise an Ability Score (up to MaxAbilityScore, default 20).
        public bool TrySpendAsiPoint(Ability ability, AbilityScoreImprovementConfig asiConfig = null)
        {
            var cfg = asiConfig ?? Database.AsiConfig;
            if (unspentAsiPoints <= 0) return false;
            int currentUnbuffed = abilities.baseScores[ability] + abilities.racialModifiers[ability] + abilities.permanentBonuses[ability];
            if (currentUnbuffed >= cfg.maxAbilityScore) return false;
            unspentAsiPoints--;
            abilities.permanentBonuses = abilities.permanentBonuses.Add(ability, 1);
            RecalculateStats();
            return true;
        }

        // Respecs Point Buy base scores while preserving Race, Class, Background, equipment, and permanent rewards.
        public void Respec(AbilityScores newBaseScores, PointBuyConfig config = null)
        {
            var cfg = config ?? PointBuyConfig.Standard;
            var err = cfg.Validate(newBaseScores);
            if (err != null) throw new InvalidOperationException(err);
            abilities.baseScores = newBaseScores;
            RecalculateStats();
        }
    }

    public enum RunStatus { Active, Victory, Defeat }

    public class RunStats
    {
        public int nodesCompleted, battlesWon, elitesWon, goldEarned;
        public bool bossReached, bossDefeated;
    }

    public class RunState
    {
        public readonly int Seed;
        public string RunId => Seed.ToString("X8");
        public int Act => Map.act;
        public readonly MapDef Map;
        public readonly List<Unit> Party;
        public readonly List<string> Items = new List<string>();
        public readonly List<string> Relics = new List<string>();
        public readonly HashSet<string> Cleared = new HashSet<string>();
        public readonly Random Rng; // combat randomness
        public readonly RunStats Stats = new RunStats();
        public readonly List<StatusApply> BattleBuffs = new List<StatusApply>();
        public readonly DateTime StartedUtc = DateTime.UtcNow;
        public DateTime? EndedUtc { get; private set; }
        public int Gold;

        public RunStatus Status { get; private set; } = RunStatus.Active;
        public NodeDef Current { get; private set; }
        public RewardOffer PendingReward { get; private set; }
        public EventDef CurrentEvent { get; private set; }
        public string EventRelic { get; private set; } // the relic this event offers, rolled on entry

        public RunState(MapDef map, IEnumerable<CharacterDef> party, int seed)
        {
            Map = map;
            Seed = seed;
            Party = party.Select(c => new Unit(c)).ToList();
            Items.AddRange(Database.StartingItems);
            Rng = new Random(seed);
        }

        public int Floor => Current == null ? 0 : Current.floor + 1;
        public TimeSpan Duration => (EndedUtc ?? DateTime.UtcNow) - StartedUtc;
        public bool OnMap => Status == RunStatus.Active && (Current == null || Cleared.Contains(Current.id));
        public bool Won => Status == RunStatus.Victory;
        public bool PartyDefeated => Party.All(u => !u.Alive);

        public IEnumerable<NodeDef> Available =>
            !OnMap ? Enumerable.Empty<NodeDef>()
            : Current == null ? Map.nodes.Where(n => n.floor == 0)
            : Current.next.Select(Map.Get);

        public bool CanEnter(NodeDef n) => Available.Contains(n);

        // Per-node RNG: rewards and event rolls depend only on the run seed and the node,
        // never on how combat went, so a seed reproduces them.
        Random NodeRng(NodeDef n, int salt = 0)
        {
            int h = 17;
            foreach (var c in n.id) h = unchecked(h * 31 + c);
            return new Random(unchecked(Seed * 486187739 + h * 31 + salt));
        }

        public void Enter(NodeDef n)
        {
            if (!CanEnter(n)) throw new InvalidOperationException($"Node {n.id} is not reachable");
            Current = n;
            if (n.type == NodeType.Boss) Stats.bossReached = true;
            if (n.type == NodeType.Treasure) PendingReward = RewardGenerator.ForTreasure(NodeRng(n), Relics);
            if (n.type == NodeType.Event)
            {
                CurrentEvent = Database.Events[n.eventId];
                EventRelic = RewardGenerator.RandomRelicId(NodeRng(n, 1), Relics);
            }
        }

        public EncounterDef CurrentEncounter => Current?.encounter == null ? null : Database.Encounters[Current.encounter];

        bool Pending(NodeType type) => Status == RunStatus.Active && Current != null && Current.type == type && !Cleared.Contains(Current.id);

        // ---- combat ----

        public void WinCombat()
        {
            var enc = CurrentEncounter;
            if (enc == null || !Current.IsCombat || Cleared.Contains(Current.id) || PendingReward != null || Status != RunStatus.Active)
                throw new InvalidOperationException("No combat to win");
            Stats.battlesWon++;
            if (enc.tier == Tier.Elite) Stats.elitesWon++;
            if (enc.tier == Tier.Boss) Stats.bossDefeated = true;
            foreach (var u in Party) if (!u.Alive) u.hp = 1; // the fallen rise after a won battle
            float heal = Relics.Sum(id => Database.Relics[id].victoryHeal);
            if (heal > 0) foreach (var u in Party) u.Heal((int)Math.Round(u.stats.maxHp * heal));
            PendingReward = RewardGenerator.ForBattle(enc.tier, NodeRng(Current), Relics);
        }

        public void LoseCombat()
        {
            if (Status != RunStatus.Active) return;
            Status = RunStatus.Defeat;
            EndedUtc = DateTime.UtcNow;
        }

        // Battle-start buffs from events, consumed by the next battle only.
        public List<StatusApply> TakeBattleBuffs()
        {
            var buffs = BattleBuffs.ToList();
            BattleBuffs.Clear();
            return buffs;
        }

        // ---- rewards ----

        // choice: index into PendingReward.choices, or -1 to take only the gold. Applied exactly once.
        public void ClaimReward(int choice)
        {
            var r = PendingReward ?? throw new InvalidOperationException("No reward pending");
            if (choice < -1 || choice >= r.choices.Count) throw new ArgumentOutOfRangeException(nameof(choice));
            PendingReward = null;
            AddGold(r.gold);
            if (choice >= 0) Grant(r.choices[choice]);
            Complete();
        }

        public void Grant(RewardOption o)
        {
            switch (o.kind)
            {
                case RewardKind.Item: Items.Add(o.id); break;
                case RewardKind.Relic: AddRelic(o.id); break;
                case RewardKind.Upgrade: Upgrade(o.id); break;
                default: AddGold(o.amount); break;
            }
        }

        void AddGold(int amount)
        {
            Gold += amount;
            if (amount > 0) Stats.goldEarned += amount;
        }

        void AddRelic(string id)
        {
            if (Relics.Contains(id)) return;
            Relics.Add(id);
            var relic = Database.Relics[id];
            foreach (var u in Party)
            {
                if (!relic.abilityBonus.IsZero) u.AddPermanentAbilityBonuses(relic.abilityBonus);
                if (relic.modifiers != null)
                    foreach (var m in relic.modifiers) u.AddPermanentModifier(m);
                u.Improve(relic.bonus);
            }
        }

        public static UpgradeDef GetUpgradeFor(Unit u)
        {
            if (u != null && Database.Upgrades.TryGetValue(u.id, out var up)) return up;
            if (u?.classDef != null && Database.Upgrades.TryGetValue(u.classDef.id, out var cup)) return cup;
            return Database.Upgrades["knight"];
        }

        void Upgrade(string character)
        {
            var u = Party.FirstOrDefault(p => p.id == character)
                    ?? Party.FirstOrDefault(p => p.classDef?.id == character)
                    ?? Party[0];
            var up = Database.Upgrades.TryGetValue(character, out var def) ? def : GetUpgradeFor(u);
            if (!up.abilityBonus.IsZero) u.AddPermanentAbilityBonuses(up.abilityBonus);
            if (up.modifiers != null)
                foreach (var m in up.modifiers) u.AddPermanentModifier(m);
            u.Improve(up.bonus);
        }

        // ---- rest ----

        public static int RecoverAmount(Unit u) => u.Alive ? (int)Math.Round((u.stats.maxHp - u.hp) * RunConfig.RestRecover) : 0;

        public void RestRecover()
        {
            if (!Pending(NodeType.Rest)) throw new InvalidOperationException("Not at an unused rest site");
            foreach (var u in Party)
            {
                if (!u.Alive) continue;
                u.hp += RecoverAmount(u);
                u.mp += (int)Math.Round((u.stats.maxMp - u.mp) * RunConfig.RestRecover);
            }
            Complete();
        }

        public void RestImprove(string character)
        {
            if (!Pending(NodeType.Rest)) throw new InvalidOperationException("Not at an unused rest site");
            bool knownUpgrade = Database.Upgrades.ContainsKey(character);
            bool inParty = Party.Any(p => p.id == character || p.classDef?.id == character);
            if (!knownUpgrade && !inParty) throw new ArgumentException(character);
            Upgrade(character);
            Complete();
        }

        // ---- events ----

        // Null when the choice can be taken, otherwise why not.
        public string EventBlocked(EventChoice c) =>
            c.goldCost > Gold ? $"Need {c.goldCost} gold" : c.relic && EventRelic == null ? "Nothing left to gain" : null;

        public string ChooseEvent(int index)
        {
            var e = CurrentEvent;
            if (e == null || !Pending(NodeType.Event)) throw new InvalidOperationException("No event pending");
            var c = e.choices[index];
            var blocked = EventBlocked(c);
            if (blocked != null) throw new InvalidOperationException(blocked);

            CurrentEvent = null;
            Gold -= c.goldCost;
            AddGold(c.gold);
            foreach (var u in Party.Where(u => u.Alive))
            {
                if (c.healPercent > 0) u.Heal(u.stats.maxHp * c.healPercent / 100);
                if (c.hurtPercent > 0) u.hp = Math.Max(1, u.hp - u.stats.maxHp * c.hurtPercent / 100);
            }
            if (c.item != null) Items.Add(c.item);
            if (c.relic) AddRelic(EventRelic);
            if (c.battleBuff != null) BattleBuffs.Add(c.battleBuff);
            Complete();
            return c.relic ? $"{c.outcome} Gained {Database.Relics[EventRelic].name}." : c.outcome;
        }

        // ---- node completion ----

        void Complete()
        {
            Cleared.Add(Current.id);
            Stats.nodesCompleted++;
            if (Current.type == NodeType.Boss)
            {
                Status = RunStatus.Victory;
                EndedUtc = DateTime.UtcNow;
            }
        }

        // Restores progression and per-character runtime state from a saved game (Section 2 & Section 12).
        public void RestoreFromSave(GameSaveData data)
        {
            if (data == null) return;
            Gold = data.gold;
            Items.Clear();
            if (data.items != null) Items.AddRange(data.items);
            Relics.Clear();
            if (data.relics != null) Relics.AddRange(data.relics);
            Cleared.Clear();
            if (data.clearedNodeIds != null)
            {
                foreach (var cid in data.clearedNodeIds)
                    if (!string.IsNullOrEmpty(cid)) Cleared.Add(cid);
            }
            if (!string.IsNullOrEmpty(data.currentNodeId) && Map.nodes.Any(n => n.id == data.currentNodeId))
                Current = Map.Get(data.currentNodeId);

            if (data.party != null)
            {
                for (int i = 0; i < Math.Min(Party.Count, data.party.Length); i++)
                {
                    var sc = data.party[i];
                    var u = Party[i];
                    u.level = Math.Max(1, sc.level);
                    u.unspentAsiPoints = sc.unspentAsiPoints;
                    u.abilities.permanentBonuses = sc.permanentAbilityBonuses;
                    u.RecalculateStats(fillResources: true);
                    if (sc.hp > 0) u.hp = Math.Min(u.stats.maxHp, sc.hp);
                    if (sc.mp > 0) u.mp = Math.Min(u.stats.maxMp, sc.mp);
                }
            }
        }
    }
}
