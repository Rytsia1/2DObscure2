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
        public int exp; // progress toward the next level; 0 at the cap
        public bool IsMaxLevel => level >= RunConfig.MaxLevel;
        public int ExpToNext => RunConfig.ExpToNext(level);
        public int unspentAsiPoints;
        public readonly List<string> selectedTalentIds = new List<string>();
        public bool HasTalent(string talentId) => !string.IsNullOrEmpty(talentId) && selectedTalentIds.Contains(talentId);
        public int EarnedTalentSlots => TalentSystem.GetTalentSlotsAtLevel(level);
        public int AvailableTalentSelections => Math.Max(0, EarnedTalentSlots - selectedTalentIds.Count);
        public bool CanSelectTalent(string talentId, out string error) => TalentSystem.ValidateSelection(this, talentId, out error);
        public bool TrySelectTalent(string talentId, out string error) => TalentSystem.TrySelectTalent(this, talentId, out error);
        public void RespecTalents() => TalentSystem.Respec(this);

        public readonly AbilityScoreSheet abilities = new AbilityScoreSheet();
        public readonly SkillLoadout skillLoadout = new SkillLoadout();
        public readonly Dictionary<EquipmentSlot, EquipmentDef> equipped = new Dictionary<EquipmentSlot, EquipmentDef>();
        public readonly Dictionary<EquipmentSlot, EquipmentInstance> equippedInstances = new Dictionary<EquipmentSlot, EquipmentInstance>();
        public readonly List<PassiveDef> passives = new List<PassiveDef>();
        public readonly List<StatModifier> permanentModifiers = new List<StatModifier>();
        public readonly List<StatModifier> temporaryModifiers = new List<StatModifier>();
        public readonly ElementalResistanceProfile elementalResistances = new ElementalResistanceProfile();
        public readonly DamageModifierSet damageModifiers = new DamageModifierSet();

        Stats permanentLegacyBonuses;
        public DerivedStats Derived { get; private set; }
        public Stats stats;
        public string lineageId;
        public Ability[] racialBonusChoice = Array.Empty<Ability>();
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
            background = d.backgroundId != null && Database.Backgrounds.TryGetValue(d.backgroundId, out var bg) ? bg : (Database.Backgrounds.TryGetValue("guard", out var g) ? g : Database.Backgrounds.Values.First());

            abilities.baseScores = d.baseScores.Equals(default) ? AbilityScores.DefaultBase : d.baseScores;
            lineageId = !string.IsNullOrEmpty(d.lineageId) ? d.lineageId : (race.usesLineage ? (race.lineageId ?? "crimson") : null);
            racialBonusChoice = (d.racialBonusChoice != null && d.racialBonusChoice.Length > 0)
                ? d.racialBonusChoice.ToArray()
                : race.DefaultBonusChoice();
            abilities.racialModifiers = race.ResolveRacialModifiers(racialBonusChoice, lineageId);

            skillLoadout.InitializeForClass(classDef, level);
            if (d.unlockedSkills != null)
            {
                foreach (var sid in d.unlockedSkills)
                {
                    if (Database.Skills.ContainsKey(sid)) skillLoadout.UnlockActive(sid);
                    if (Database.Passives.ContainsKey(sid)) skillLoadout.UnlockPassive(sid);
                }
            }
            if (d.activeSkillLoadout != null && d.activeSkillLoadout.Length > 0)
            {
                for (int i = 0; i < Math.Min(SkillLoadout.ActiveSlotCount, d.activeSkillLoadout.Length); i++)
                    skillLoadout.activeSlots[i] = d.activeSkillLoadout[i];
            }
            else if (d.skills != null && d.skills.Length > 0)
            {
                Array.Clear(skillLoadout.activeSlots, 0, SkillLoadout.ActiveSlotCount);
                for (int i = 0; i < Math.Min(SkillLoadout.ActiveSlotCount, d.skills.Length); i++)
                {
                    if (!string.IsNullOrEmpty(d.skills[i]) && Database.Skills.ContainsKey(d.skills[i]))
                    {
                        skillLoadout.UnlockActive(d.skills[i]);
                        skillLoadout.activeSlots[i] = d.skills[i];
                    }
                }
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

            if (d.talentIds != null)
            {
                foreach (var tid in d.talentIds)
                    if (!string.IsNullOrEmpty(tid) && !selectedTalentIds.Contains(tid))
                        selectedTalentIds.Add(tid);
            }
            if (d.passives != null)
                foreach (var pid in d.passives)
                    if (Database.Passives.TryGetValue(pid, out var p) && !passives.Contains(p)) passives.Add(p);

            if (d.startingEquipment != null)
            {
                foreach (var eid in d.startingEquipment)
                    if (Database.Equipment.TryGetValue(eid, out var eq))
                    {
                        var inst = new EquipmentInstance(eq, eq.rarity, eq.equipmentLevel) { boundCharacterId = id, isEquipped = true };
                        var effDef = inst.ToEffectiveDef();
                        effDef.slot = eq.slot;
                        equipped[eq.slot] = effDef;
                        equippedInstances[eq.slot] = inst;
                    }
            }

            if (d.equipmentLoadout != null)
            {
                foreach (var assign in d.equipmentLoadout)
                    if (!string.IsNullOrEmpty(assign.equipmentId) && Database.Equipment.TryGetValue(assign.equipmentId, out var eq))
                    {
                        var inst = new EquipmentInstance(eq, eq.rarity, eq.equipmentLevel) { boundCharacterId = id, isEquipped = true };
                        var effDef = inst.ToEffectiveDef();
                        effDef.slot = assign.slot;
                        equipped[assign.slot] = effDef;
                        equippedInstances[assign.slot] = inst;
                    }
            }

            RecalculateStats(fillResources: true);
        }

        public Unit(EnemyDef d, int dungeonNumber = 1, int stage = 1)
        {
            id = d.id;
            name = d.name;
            role = d.tier.ToString();
            sprite = d.sprite;
            skills = Array.Empty<string>();
            affinities = d.affinities ?? Affinities.None;
            isEnemyUnit = true;

            abilities.baseScores = d.abilityScores.Equals(default) ? AbilityScores.Uniform(10) : d.abilityScores;
            stats = ScaleEnemyStats(d.stats, d.tier, dungeonNumber, d.magicDef, out int mDef);
            if (d.isEnhanced)
            {
                // Variant Spec §4.3: EnhancedStat = floor(NormalScaledStat * VariantModifier), applied once after Normal scaling.
                var vm = d.statMult;
                stats = new Stats(
                    ScaleVariantStat(stats.maxHp, vm.Hp),
                    ScaleVariantStat(stats.maxMp, vm.Mp),
                    ScaleVariantStat(stats.atk, vm.Atk),
                    ScaleVariantStat(stats.def, vm.Def),
                    ScaleVariantStat(stats.spd, vm.Spd),
                    ScaleVariantStat(stats.mag, vm.Matk));
                mDef = ScaleVariantStat(mDef, vm.Mdef);
            }
            var (stageHpMult, stageAtkMult, stageBreakBonus) = EnemyAffixSystem.GetStageMultipliers(stage);
            if (stage > 1)
            {
                stats = new Stats(
                    ScaleVariantStat(stats.maxHp, stageHpMult),
                    stats.maxMp,
                    ScaleVariantStat(stats.atk, stageAtkMult),
                    stats.def,
                    stats.spd,
                    stats.mag > 0 ? ScaleVariantStat(stats.mag, stageAtkMult) : 0);
            }
            var mods = abilities.AbilityModifiers;
            Derived = new DerivedStats
            {
                maxHp = stats.maxHp,
                maxMp = stats.maxMp,
                maxShield = 0,
                physicalPower = stats.atk,
                rangedPower = stats.atk,
                magicPower = stats.mag > 0 ? stats.mag : stats.atk,
                healingPower = stats.mag > 0 ? stats.mag : stats.atk,
                supportPower = 10 + mods.cha * 2,
                criticalChance = Math.Max(0, 5 + mods.dex * 2),
                criticalDamage = 150,
                armorPenetration = Math.Max(0, mods.str),
                magicPenetration = Math.Max(0, mods.intel),
                physicalDefense = stats.def,
                magicDefense = mDef,
                evasion = Math.Max(0, 5 + mods.dex * 2),
                blockPower = 50,
                statusResistance = Math.Max(0, (mods.vit + mods.wis) * 3),
                staggerResistance = d.breakResistance + Math.Max(0, mods.vit * 2),
                initiative = stats.spd,
                actionSpeed = 100 + mods.dex * 5,
                qteTimingBonus = 0,
                qteStability = 0,
                counterPower = 10,
                physicalBreakPower = Combatant.EnemyBaseBreakPower(d.tier) + stageBreakBonus + mods.str * 2,
                magicBreakPower = Combatant.EnemyBaseBreakPower(d.tier) + stageBreakBonus + mods.intel * 2,
            };
            hp = stats.maxHp;
            mp = stats.maxMp;
        }

        // Bab 10 §7: Linear Tier-Weighted Enemy Scaling.
        // FinalStat = FLOOR(BaseStat x (1 + GrowthRate[Tier, Stat] x (Dungeon - 1))), always from the D1 base, floored once.
        // Decimal keeps exact results (float 0.12 x 5 x 125 lands just under 200).
        public enum ScaledStat { HP, MP, ATK, MATK, DEF, MDEF, SPD }

        public static decimal GrowthRate(Tier tier, ScaledStat stat) => (tier, stat) switch
        {
            (Tier.Elite, ScaledStat.HP) => 0.13m, (Tier.Elite, ScaledStat.MP) => 0.08m,
            (Tier.Elite, ScaledStat.ATK) => 0.09m, (Tier.Elite, ScaledStat.MATK) => 0.09m,
            (Tier.Elite, ScaledStat.DEF) => 0.055m, (Tier.Elite, ScaledStat.MDEF) => 0.055m, (Tier.Elite, ScaledStat.SPD) => 0.03m,
            (Tier.Boss, ScaledStat.HP) => 0.14m, (Tier.Boss, ScaledStat.MP) => 0.10m,
            (Tier.Boss, ScaledStat.ATK) => 0.10m, (Tier.Boss, ScaledStat.MATK) => 0.10m,
            (Tier.Boss, ScaledStat.DEF) => 0.06m, (Tier.Boss, ScaledStat.MDEF) => 0.06m, (Tier.Boss, ScaledStat.SPD) => 0.04m,
            (_, ScaledStat.HP) => 0.12m, (_, ScaledStat.MP) => 0.06m,
            (_, ScaledStat.ATK) => 0.08m, (_, ScaledStat.MATK) => 0.08m,
            (_, ScaledStat.DEF) => 0.05m, (_, ScaledStat.MDEF) => 0.05m, _ => 0.02m, // SPD
        };

        public static int ScaleStat(int baseValue, Tier tier, ScaledStat stat, int dungeonNumber) =>
            (int)Math.Floor(baseValue * (1m + GrowthRate(tier, stat) * Math.Max(0, dungeonNumber - 1)));

        public static int ScaleVariantStat(int normalScaledValue, float variantMultiplier) =>
            variantMultiplier <= 0f || Math.Abs(variantMultiplier - 1f) < 1e-6f
                ? normalScaledValue
                : (int)Math.Floor(normalScaledValue * (decimal)variantMultiplier);

        public static Stats ScaleEnemyStats(Stats baseStats, Tier tier, int dungeonNumber, int baseMagicDef, out int scaledMagicDef)
        {
            int baseMag = baseStats.mag > 0 ? baseStats.mag : baseStats.atk;
            scaledMagicDef = ScaleStat(baseMagicDef >= 0 ? baseMagicDef : baseStats.def, tier, ScaledStat.MDEF, dungeonNumber);
            return new Stats(
                ScaleStat(baseStats.maxHp, tier, ScaledStat.HP, dungeonNumber),
                ScaleStat(baseStats.maxMp, tier, ScaledStat.MP, dungeonNumber),
                ScaleStat(baseStats.atk, tier, ScaledStat.ATK, dungeonNumber),
                ScaleStat(baseStats.def, tier, ScaledStat.DEF, dungeonNumber),
                ScaleStat(baseStats.spd, tier, ScaledStat.SPD, dungeonNumber),
                dungeonNumber <= 1 ? baseStats.mag : ScaleStat(baseMag, tier, ScaledStat.MATK, dungeonNumber));
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
            if (background != null) list.AddRange(background.GetActiveModifiers(level));
            foreach (var p in passives)
                if (p.modifiers != null) list.AddRange(p.modifiers);
            if (HasTalent("durable"))
                list.Add(StatModifier.ForDerived(DerivedStatId.MaxHP, TalentSystem.DurableHpBonus, ModifierKind.PercentAdd, ModifierSource.Talent, "Durable"));
            if (HasTalent("arcane_reserve"))
                list.Add(StatModifier.ForDerived(DerivedStatId.MaxMP, TalentSystem.ArcaneReserveMaxMpBonus, ModifierKind.Flat, ModifierSource.Talent, "Arcane Reserve"));
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
            abilities.racialModifiers = race != null ? race.ResolveRacialModifiers(racialBonusChoice, lineageId) : AbilityScores.Zero;

            int oldMaxHp = Derived.maxHp;
            int oldMaxMp = Derived.maxMp;

            Derived = StatCalculator.Calculate(
                abilities,
                classDef,
                level,
                equipped.Values,
                GatherAllDerivedModifiers(),
                permanentLegacyBonuses,
                passives,
                equipped);

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
            return StatCalculator.Explain(statId, abilities, classDef, level, equipped.Values, mods, permanentLegacyBonuses, passives, equipped);
        }

        public bool HasActiveSkill(string skillId) =>
            !string.IsNullOrEmpty(skillId) &&
            ((skills != null && skills.Contains(skillId)) || skillLoadout.activeSlots.Contains(skillId));

        public string[] GetCombatActiveSkillIds()
        {
            var equippedActives = skillLoadout != null ? skillLoadout.GetEquippedActives() : Array.Empty<string>();
            if (equippedActives.Length > 0) return equippedActives;
            return skills != null ? skills.Where(id => !string.IsNullOrEmpty(id) && Database.Skills.ContainsKey(id)).ToArray() : Array.Empty<string>();
        }

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

        public void RestoreMp(int amount)
        {
            if (Alive) mp = Math.Min(stats.maxMp, mp + amount);
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
            if (equippedInstances.TryGetValue(item.slot, out var oldInst) && oldInst != null)
            {
                oldInst.isEquipped = false;
                oldInst.boundCharacterId = null;
            }
            var inst = new EquipmentInstance(item, item.rarity, item.equipmentLevel) { boundCharacterId = id, isEquipped = true };
            equippedInstances[item.slot] = inst;
            var effDef = inst.ToEffectiveDef();
            effDef.slot = item.slot;
            equipped[item.slot] = effDef;
            RecalculateStats();
        }

        // Validated equip that enforces Strict Weapon Compatibility and item restrictions for any of the 8 slots.
        public bool TryEquip(EquipmentSlot slot, EquipmentDef item, out string error)
        {
            error = EquipmentValidator.Validate(item, slot, classDef, race, level, FinalScores, id);
            if (error != null) return false;

            if (equippedInstances.TryGetValue(slot, out var oldInst) && oldInst != null)
            {
                oldInst.isEquipped = false;
                oldInst.boundCharacterId = null;
            }

            var inst = new EquipmentInstance(item, item.rarity, item.equipmentLevel) { boundCharacterId = id, isEquipped = true };
            equippedInstances[slot] = inst;
            var effDef = inst.ToEffectiveDef();
            effDef.slot = slot;
            equipped[slot] = effDef;
            RecalculateStats();
            return true;
        }

        public bool TryEquip(EquipmentSlot slot, EquipmentInstance instance, out string error)
        {
            if (instance == null)
            {
                error = "Equipment instance is null.";
                return false;
            }
            var def = instance.Definition;
            if (def == null)
            {
                error = $"Unknown equipment definition '{instance.definitionId}'.";
                return false;
            }

            // 1. Exclusive Ownership Check across characters:
            if (instance.isEquipped && !string.IsNullOrEmpty(instance.boundCharacterId) && instance.boundCharacterId != id)
            {
                error = $"Item '{def.name}' is already equipped by another character ({instance.boundCharacterId}).";
                return false;
            }

            // 2. Duplicate Check on the same character across different slots:
            if (equippedInstances.Any(kv => kv.Key != slot && kv.Value != null && kv.Value.instanceId == instance.instanceId))
            {
                var otherSlot = equippedInstances.First(kv => kv.Key != slot && kv.Value != null && kv.Value.instanceId == instance.instanceId).Key;
                error = $"Item '{def.name}' is already equipped in another slot ({EquipmentSlots.DisplayName(otherSlot)}).";
                return false;
            }

            error = EquipmentValidator.Validate(def, slot, classDef, race, level, FinalScores, id);
            if (error != null) return false;

            if (equippedInstances.TryGetValue(slot, out var oldInst) && oldInst != null)
            {
                oldInst.isEquipped = false;
                oldInst.boundCharacterId = null;
            }

            instance.isEquipped = true;
            instance.boundCharacterId = id;
            equippedInstances[slot] = instance;
            var effDef = instance.ToEffectiveDef();
            effDef.slot = slot;
            equipped[slot] = effDef;
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

        public bool TryEquip(EquipmentInstance instance, out string error)
        {
            if (instance?.Definition == null)
            {
                error = "Equipment instance or definition is null.";
                return false;
            }
            return TryEquip(instance.Definition.slot, instance, out error);
        }

        public bool Unequip(EquipmentSlot slot)
        {
            bool removedInst = false;
            if (equippedInstances.TryGetValue(slot, out var inst) && inst != null)
            {
                inst.isEquipped = false;
                inst.boundCharacterId = null;
                equippedInstances.Remove(slot);
                removedInst = true;
            }
            bool removedDef = equipped.Remove(slot);
            if (!removedInst && !removedDef) return false;
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

        public void AddCapBreakingTemporaryAbilityModifier(Ability a, int delta)
        {
            abilities.capBreakingTemporaryModifiers = abilities.capBreakingTemporaryModifiers.Add(a, delta);
            RecalculateStats();
        }

        public void ClearTemporaryModifiers()
        {
            abilities.temporaryModifiers = AbilityScores.Zero;
            abilities.capBreakingTemporaryModifiers = AbilityScores.Zero;
            temporaryModifiers.Clear();
            RecalculateStats();
        }

        // Advances character level and grants Ability Score Improvement points when reaching configured levels.
        public void LevelUp(AbilityScoreImprovementConfig asiConfig = null)
        {
            if (IsMaxLevel) return;
            var cfg = asiConfig ?? Database.AsiConfig;
            level++;
            skillLoadout.SyncLevelUnlocks(classDef, level);
            if (cfg.GrantsAtLevel(level))
                unspentAsiPoints += cfg.pointsGranted;
            RecalculateStats();
        }

        // Adds EXP, levelling up as many times as it covers (remainder carries over). Returns levels gained.
        public int GainExp(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return 0;
            int before = level;
            exp += amount;
            while (!IsMaxLevel && exp >= ExpToNext)
            {
                exp -= ExpToNext;
                LevelUp();
            }
            if (IsMaxLevel) exp = 0;
            return level - before;
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

        // Respecs Point Buy base scores (and optionally racial bonus choice / Dragonborn lineage per GDD)
        // while preserving Race, Class, Background, equipment, and permanent rewards.
        public void Respec(AbilityScores newBaseScores, PointBuyConfig config = null, Ability[] newRacialBonusChoice = null, string newLineageId = null)
        {
            var cfg = config ?? PointBuyConfig.Standard;
            var err = cfg.Validate(newBaseScores);
            if (err != null) throw new InvalidOperationException(err);
            if (race != null && (newRacialBonusChoice != null || newLineageId != null))
            {
                var nextChoice = newRacialBonusChoice ?? racialBonusChoice;
                var nextLineage = newLineageId ?? lineageId;
                string raceErr = race.ValidateRacialChoice(nextChoice, nextLineage);
                if (raceErr != null) throw new InvalidOperationException(raceErr);
                racialBonusChoice = nextChoice != null ? nextChoice.ToArray() : Array.Empty<Ability>();
                lineageId = race.usesLineage ? nextLineage : null;
                abilities.racialModifiers = race.ResolveRacialModifiers(racialBonusChoice, lineageId);
            }
            abilities.baseScores = newBaseScores;
            RecalculateStats();
        }

        public bool HasEquippedShield()
        {
            if (equipped != null && equipped.TryGetValue(EquipmentSlot.SecondHand, out var eq) && eq != null)
                return eq.weaponType == WeaponType.Shield || eq.weaponType == WeaponType.Buckler;

            if (equipped == null || equipped.Count == 0)
            {
                if (classDef != null && classDef.secondHandWeapons != null)
                {
                    foreach (var wt in classDef.secondHandWeapons)
                    {
                        if (wt == WeaponType.Shield || wt == WeaponType.Buckler)
                            return true;
                    }
                }
            }
            return false;
        }

        public bool HasEquippedWeaponType(WeaponType type)
        {
            if (type == WeaponType.None) return true;
            if (equipped != null && equipped.TryGetValue(EquipmentSlot.MainHand, out var mh) && mh != null)
                if (mh.weaponType == type) return true;
            if (equipped != null && equipped.TryGetValue(EquipmentSlot.SecondHand, out var sh) && sh != null)
                if (sh.weaponType == type) return true;

            if (equipped == null || equipped.Count == 0)
            {
                if (classDef != null)
                {
                    if (classDef.mainHandWeapons != null && Array.IndexOf(classDef.mainHandWeapons, type) >= 0)
                        return true;
                    if (classDef.secondHandWeapons != null && Array.IndexOf(classDef.secondHandWeapons, type) >= 0)
                        return true;
                }
            }
            return false;
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
        // Seed of the current dungeon attempt: maps, node rewards and event risk rolls derive from it.
        // Starts equal to Seed; BeginDungeonAttempt replaces it so a retry is never a replay.
        public int DungeonSeed { get; private set; }
        public string RunId => Seed.ToString("X8");
        // Retry budget (failure model C): ending a dungeon attempt by wipe or abandon spends one.
        // A wipe with none left loses the expedition: its saves can no longer be loaded.
        public int RetriesLeft { get; private set; } = RunConfig.RetriesPerExpedition;
        public bool ExpeditionLost { get; private set; }
        public bool CanAbandon => RetriesLeft > 0;
        public int Act => Map != null ? Map.act : 1;
        public int Stage => Map != null ? Math.Max(1, Map.stage) : 1;
        public int TotalStages => Map != null ? Math.Max(1, Map.totalStages) : 1;
        public bool IsFinalStage => Map == null || Map.IsFinalStage;
        public bool IsAtFinalDungeonBoss => Current != null && Current.type == NodeType.Boss && IsFinalStage;
        public bool IsAtStageBoss => Current != null && Current.type == NodeType.Boss && !IsFinalStage;
        public MapDef Map { get; private set; }
        public ShopInventory CurrentShop { get; set; }
        public readonly List<Unit> Party;
        public readonly List<string> Items = new List<string>();
        public readonly List<string> Relics = new List<string>();
        public readonly HashSet<string> Cleared = new HashSet<string>();
        public readonly InventoryRuntime Inventory = new InventoryRuntime();
        public readonly RelicManager RelicManager = new RelicManager();
        public readonly DungeonModifierManager ModifierManager = new DungeonModifierManager();
        public Random Rng { get; private set; } // combat randomness, reseeded per dungeon attempt
        public readonly RunStats Stats = new RunStats();
        // Event blessings: applied to the party at the start of every battle until dungeon exit.
        public readonly List<StatusApply> BattleBuffs = new List<StatusApply>();
        public readonly DateTime StartedUtc = DateTime.UtcNow;
        public DateTime? EndedUtc { get; private set; }
        public int Gold;    // Run Gold: earned in the dungeon, spent at the Dungeon Shop, lost on a wipe
        public int HubGold; // Hub Gold: secured on dungeon clear, spent at Hub facilities, kept on a wipe (GDD Bab 16)
        public int RunGold => Gold;
        public DungeonShopData CurrentDungeonShop { get; set; }

        public struct EventChoiceStatus
        {
            public bool isVisible;
            public bool isBlocked;
            public string blockedReason;
            public bool usesAdaptabilityBorrow;
            public bool isImprovise;
            public float effectScale;
        }

        public struct AttributeCheckRollSummary
        {
            public Ability ability;
            public int difficultyClass;
            public int d20Roll;
            public int partyHighestModifier;
            public int checkResult;
            public AttributeCheckResultCategory category;

            public bool IsNatural20 => d20Roll == 20;
            public bool IsNatural1 => d20Roll == 1;

            public string FormatBanner()
            {
                string natTag = IsNatural20 ? " (NATURAL 20)" : IsNatural1 ? " (NATURAL 1)" : "";
                string catLabel = StatCalculator.FormatAttributeCheckCategory(category);
                return $"[{ability} CHECK · DC {difficultyClass} · D20: {d20Roll}{natTag} + MOD {AbilityScores.FormatMod(partyHighestModifier)} = {checkResult} · {catLabel}]";
            }
        }

        public RunStatus Status { get; private set; } = RunStatus.Active;
        public NodeDef Current { get; private set; }
        public RewardOffer PendingReward { get; private set; }
        public EventDef CurrentEvent { get; private set; }
        public string EventRelic { get; private set; } // the relic this event offers, rolled on entry
        public string EventEquipment { get; private set; } // equipment rolled on event entry
        public readonly Dictionary<int, RewardOption> EventModifiers = new Dictionary<int, RewardOption>();
        public readonly Dictionary<int, RewardOption> EventCurseModifiers = new Dictionary<int, RewardOption>();
        public readonly Dictionary<int, RewardOption> EventRiskFailModifiers = new Dictionary<int, RewardOption>();
        public readonly Dictionary<int, bool> EventChoiceRiskFailed = new Dictionary<int, bool>();
        public readonly Dictionary<int, int> EventChoiceD20Rolls = new Dictionary<int, int>();
        public AttributeCheckRollSummary? LastAttributeCheckResult { get; private set; }

        // Once-per-dungeon Racial Utility quotas (GDD Bab 6 §5 & §10.1; reset on next dungeon)
        public int HumanAdaptabilityUsesRemaining { get; private set; } = 1;
        public int HalflingSneakUsesRemaining { get; private set; } = 1;
        public int DragonbornShopDiscountUsesRemaining { get; private set; } = 1;
        public int ElfWeaknessRevealUsesRemaining { get; private set; } = 1;

        // Per-save-slot progressive enemy discovery Codex (Felna_Analysis_System_Spec_v1.md Part D)
        public EnemyCodex Codex { get; private set; } = new EnemyCodex();

        public bool TryConsumeElfWeaknessReveal()
        {
            if (!HasRace("elf") || ElfWeaknessRevealUsesRemaining <= 0) return false;
            ElfWeaknessRevealUsesRemaining--;
            return true;
        }

        public void ResetDungeonUtilityCharges()
        {
            HumanAdaptabilityUsesRemaining = 1;
            HalflingSneakUsesRemaining = 1;
            DragonbornShopDiscountUsesRemaining = 1;
            ElfWeaknessRevealUsesRemaining = 1;
        }

        public bool HasRace(string raceId) =>
            !string.IsNullOrEmpty(raceId) &&
            Party != null &&
            Party.Any(u => string.Equals(u.race?.id, raceId, StringComparison.OrdinalIgnoreCase));

        public bool HasHalflingHazardReduction => HasRace("halfling");
        public bool RevealsRelicEffects => HasRace("dragonborn");
        public bool RevealsCurses => HasRace("liichtian");
        public bool RevealsSupernaturalSource(EventDef e) =>
            HasRace("liichtian") && e != null && (e.HasTag(EventTags.Supernatural) || e.HasTag(EventTags.Corruption));
        public bool RevealsLiquidOutcomes(EventDef e) =>
            HasRace("merfolk") && e != null && e.HasTag(EventTags.Liquid);

        public bool CanSneakNode(NodeDef n) =>
            n != null && n.type == NodeType.Battle && CanEnter(n) && HasRace("halfling") && HalflingSneakUsesRemaining > 0;

        // What sneaking past a (Normal-tier) Battle node gives up, for the map to say before and after.
        public static string SneakForfeit =>
            $"its {RunConfig.BattleGold.min}-{RunConfig.BattleGold.max} spoils, {RunConfig.BattleExp} EXP per hero and the reward pick";

        // Set by SneakNode, shown once by the map, then cleared. Not saved.
        public string LastSneakNote;

        public void SneakNode(NodeDef n)
        {
            if (!CanSneakNode(n))
                throw new InvalidOperationException("Cannot sneak past this node.");
            HalflingSneakUsesRemaining = Math.Max(0, HalflingSneakUsesRemaining - 1);
            Current = n;
            Complete();
            LastSneakNote = $"Snuck past the fight. Gave up {SneakForfeit}.";
        }

        public bool CanUseDragonbornShopDiscount =>
            HasRace("dragonborn") && DragonbornShopDiscountUsesRemaining > 0;

        public int GetDungeonShopDiscountedPrice(int baseCost) =>
            CanUseDragonbornShopDiscount ? Math.Max(1, (int)Math.Round(baseCost * 0.85f)) : baseCost;

        public bool TryConsumeDragonbornShopDiscount()
        {
            if (!CanUseDragonbornShopDiscount) return false;
            DragonbornShopDiscountUsesRemaining = Math.Max(0, DragonbornShopDiscountUsesRemaining - 1);
            return true;
        }

        public RunState(MapDef map, IEnumerable<CharacterDef> party, int seed)
        {
            Map = map;
            Seed = seed;
            DungeonSeed = seed;
            Party = party.Select(c => new Unit(c)).ToList();
            Items.AddRange(Database.StartingItems);
            foreach (var itm in Database.StartingItems)
                Inventory.AddConsumable(itm, 1);
            Rng = new Random(seed);
            RestockHubShop();
            CurrentDungeonShop = DungeonShopData.Generate(Map?.act ?? 1, Rng);
            CurrentShop = CurrentDungeonShop.equipmentShop;
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

        // Per-node RNG: rewards and event rolls depend only on the run seed, stage, and node,
        // never on how combat went, so a seed reproduces them.
        Random NodeRng(NodeDef n, int salt = 0)
        {
            int h = 17;
            foreach (var c in n.id) h = unchecked(h * 31 + c);
            int stageSalt = (Stage - 1) * 1000003;
            return new Random(unchecked(DungeonSeed * 486187739 + h * 31 + stageSalt + salt));
        }

        public void Enter(NodeDef n)
        {
            if (!CanEnter(n)) throw new InvalidOperationException($"Node {n.id} is not reachable");
            Current = n;
            if (IsPrologue && n.IsCombat)
            {
                Tutorial.Floor = n.floor;
                Tutorial.Wipes = 0;
                SnapshotNodeEntry();
                Rng = Prologue.NodeRng(n.floor);
                return;
            }
            if (n.type == NodeType.Boss && IsFinalStage) Stats.bossReached = true;
            if (n.type == NodeType.Treasure)
            {
                PendingReward = RewardGenerator.ForTreasure(NodeRng(n), Relics);
                PendingReward.modifierChoices = RewardGenerator.GenerateModifierChoices(NodeRng(n, 3), count: 3, isMaxed: ModifierManager.IsAtCap);
                PendingReward.bonusItemId = ModifierManager.TryRollScavengerBonusConsumable(NodeRng(n, 4));
            }
            if (n.type == NodeType.Shop)
            {
                CurrentDungeonShop = DungeonShopData.Generate(Map?.act ?? 1, NodeRng(n));
                CurrentShop = CurrentDungeonShop.equipmentShop;
            }
            if (n.type == NodeType.Event)
            {
                CurrentEvent = Database.Events[n.eventId];
                EventRelic = RewardGenerator.RandomRelicId(NodeRng(n, 1), Relics);
                EventEquipment = RollEventEquipmentId(NodeRng(n, 2), Act);
                EventModifiers.Clear();
                EventCurseModifiers.Clear();
                EventRiskFailModifiers.Clear();
                EventChoiceRiskFailed.Clear();
                EventChoiceD20Rolls.Clear();
                LastAttributeCheckResult = null;
                if (CurrentEvent.choices != null)
                {
                    for (int i = 0; i < CurrentEvent.choices.Length; i++)
                    {
                        var ch = CurrentEvent.choices[i];
                        if (!string.IsNullOrEmpty(ch.modifierId))
                        {
                            var elem = ch.modifierElement;
                            if (!elem.IsValidElement() && Database.DungeonModifiers.TryGetValue(ch.modifierId, out var md) && md.isElementSpecific)
                                elem = ElementTypes.All[NodeRng(n, 10 + i).Next(ElementTypes.All.Length)];
                            EventModifiers[i] = new RewardOption { kind = RewardKind.Modifier, id = ch.modifierId, element = elem };
                        }
                        else if (ch.randomModifier)
                        {
                            var rolled = RewardGenerator.GenerateModifierChoices(NodeRng(n, 10 + i), count: 1, categoryFilter: ch.modifierCategoryFilter, isMaxed: ModifierManager.IsAtCap);
                            if (rolled.Count > 0) EventModifiers[i] = rolled[0];
                        }

                        if (!string.IsNullOrEmpty(ch.curseModifierId) && Database.DungeonModifiers.TryGetValue(ch.curseModifierId, out var cmd))
                        {
                            var celem = cmd.isElementSpecific ? ElementTypes.All[NodeRng(n, 20 + i).Next(ElementTypes.All.Length)] : ElementType.None;
                            EventCurseModifiers[i] = new RewardOption { kind = RewardKind.Modifier, id = ch.curseModifierId, element = celem };
                        }
                        if (!string.IsNullOrEmpty(ch.riskFailModifierId) && Database.DungeonModifiers.TryGetValue(ch.riskFailModifierId, out var fmd))
                        {
                            var felem = fmd.isElementSpecific ? ElementTypes.All[NodeRng(n, 25 + i).Next(ElementTypes.All.Length)] : ElementType.None;
                            EventRiskFailModifiers[i] = new RewardOption { kind = RewardKind.Modifier, id = ch.riskFailModifierId, element = felem };
                        }
                        if (ch.riskChancePercent > 0)
                        {
                            EventChoiceRiskFailed[i] = NodeRng(n, 30 + i).Next(100) < ch.riskChancePercent;
                        }
                        if (ch.isAttributeCheck)
                        {
                            EventChoiceD20Rolls[i] = NodeRng(n, 40 + i).Next(1, 21);
                        }
                    }
                }
            }
        }

        public EncounterDef CurrentEncounter => Current?.encounter == null ? null : Database.Encounter(Current.encounter);
        public string[] CurrentEnemies => Current?.enemies ?? CurrentEncounter?.enemies;

        bool Pending(NodeType type) => Status == RunStatus.Active && Current != null && Current.type == type && !Cleared.Contains(Current.id);

        // ---- combat ----

        // Same EXP to every standing hero (R031/R032 adjust it); results go on the offer for the reward screen.
        void AwardBattleExp(int amount, bool[] standing)
        {
            PendingReward.expGained = new int[Party.Count];
            PendingReward.levelsGained = new int[Party.Count];
            for (int i = 0; i < Party.Count; i++)
            {
                if (!standing[i] || Party[i].IsMaxLevel) continue;
                PendingReward.expGained[i] = amount;
                PendingReward.levelsGained[i] = Party[i].GainExp(amount);
            }
        }

        public void WinCombat()
        {
            var enc = CurrentEncounter;
            if (enc == null || !Current.IsCombat || Cleared.Contains(Current.id) || PendingReward != null || Status != RunStatus.Active)
                throw new InvalidOperationException("No combat to win");
            PersistenceCommitHandler.CommitCodexOnly(this);
            if (IsPrologue) { WinPrologueCombat(); return; }
            bool isFinalBoss = IsAtFinalDungeonBoss;
            bool isStageBoss = IsAtStageBoss;
            var standing = Party.Select(u => u.Alive).ToArray(); // only heroes standing at the end earn EXP
            Stats.battlesWon++;
            if (enc.tier == Tier.Elite) Stats.elitesWon++;
            if (isFinalBoss)
            {
                Stats.bossDefeated = true;
                Inventory.AddKeyItem("KEY002");
            }
            RelicManager.OnBattleWon(this);

            // Background Perk Rank V Post-battle Recovery (Farmer & Noble)
            foreach (var u in Party)
            {
                if (u.Alive && u.background != null)
                {
                    var rank = u.background.GetRank(u.level);
                    if (rank != null)
                    {
                        if (rank.postBattleHpRestorePercent > 0f)
                            u.Heal(Math.Max(1, (int)Math.Round(u.stats.maxHp * rank.postBattleHpRestorePercent)));
                        if (rank.postBattleMpRestorePercent > 0f)
                            u.RestoreMp(Math.Max(1, (int)Math.Round(u.stats.maxMp * rank.postBattleMpRestorePercent)));
                    }
                }
            }

            // Fallen heroes remain dead (HP == 0) after battle and must be revived with a Revive Item or at the Hub Temple (GDD Bab 16 §5-§6 & OD-03).
            float heal = Relics.Sum(id => Database.Relics[id].victoryHeal);
            if (heal > 0) foreach (var u in Party) u.Heal((int)Math.Round(u.stats.maxHp * heal));

            int fortuneBonus = ModifierManager.ConsumeBattleVictoryBonusGold();
            PendingReward = RewardGenerator.ForBattle(enc.tier, NodeRng(Current), Relics, Act, isStageBoss: isStageBoss);
            // Upgrade offers are keyed to the preset heroes (knight/rogue/...). Point any that match nobody in this party at a
            // real hero, so the card names who gets it instead of silently falling back to the first hero.
            var upgradeRng = NodeRng(Current, 5);
            foreach (var o in PendingReward.choices.Where(o => o.kind == RewardKind.Upgrade))
                if (!Party.Any(p => p.id == o.id || p.classDef?.id == o.id))
                    o.id = Party[upgradeRng.Next(Party.Count)].id;
            int exp = RunConfig.ExpFor(enc.tier);
            RelicManager.ModifyExpReward(ref exp);
            AwardBattleExp(exp, standing);
            if (isStageBoss)
            {
                PendingReward.title = $"Stage {Stage} Cleared";
                PendingReward.text = $"{enc.name} defeated. Claim a prize before Stage {Stage + 1}.";
            }
            if (fortuneBonus > 0)
            {
                PendingReward.bonusGoldFromModifier = fortuneBonus;
                PendingReward.gold += fortuneBonus;
            }
            // Every non-final-boss battle offers 3 modifier choices; the final dungeon boss excludes them
            // because all modifiers expire immediately on dungeon clear (GDD Bab 15 §3.1).
            if (!isFinalBoss)
            {
                PendingReward.modifierChoices = RewardGenerator.GenerateModifierChoices(NodeRng(Current, 3), count: 3, isMaxed: ModifierManager.IsAtCap);
            }
            PendingReward.bonusItemId = ModifierManager.TryRollScavengerBonusConsumable(NodeRng(Current, 4));
        }

        // Spends one retry for the attempt that just ended; with none left the expedition is lost.
        // Mirrored into the committed baseline so a rollback restores the spent value, not the old one.
        void SpendRetry()
        {
            if (RetriesLeft > 0) RetriesLeft--;
            else ExpeditionLost = true;
            var snap = PersistenceCommitHandler.ActiveCommittedSnapshot;
            if (snap != null)
            {
                snap.retriesLeft = RetriesLeft;
                snap.expeditionLost = ExpeditionLost;
            }
        }

        public void LoseCombat()
        {
            if (Status != RunStatus.Active) return;
            PersistenceCommitHandler.CommitCodexOnly(this);
            if (IsPrologue) { RetryPrologueNode(); return; }
            SpendRetry();
            Status = RunStatus.Defeat;
            EndedUtc = DateTime.UtcNow;
            Gold = 0;
            Current = null;
            Cleared.Clear();
            PendingReward = null;
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            // GDD Bab 14 §2: Relics are permanent player progression and remain owned on run failure
            HubEconomy.HandleRunFailure(this);
            GameSave.SaveRunState(this);
        }

        // Section 5.4: Voluntary abandonment terminates active dungeon, discards uncommitted data, preserves committed progress
        public void AbandonExpedition()
        {
            if (!CanAbandon) throw new InvalidOperationException("No retries left: the dungeon cannot be abandoned");
            SpendRetry();
            Status = RunStatus.Defeat;
            EndedUtc = DateTime.UtcNow;
            Gold = 0;
            Current = null;
            Cleared.Clear();
            PendingReward = null;
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            ResetDungeonUtilityCharges();
            PersistenceCommitHandler.RollbackUncommitted(this);
            int checkpointStage = PersistenceCommitHandler.ActiveCommittedSnapshot != null
                && PersistenceCommitHandler.ActiveCommittedSnapshot.nextDungeonAct == Act
                ? PersistenceCommitHandler.ActiveCommittedSnapshot.NormalizedNextStage()
                : 1;
            if (Map != null && Map.stage != checkpointStage)
                Map = MapGenerator.Generate(DungeonSeed, Act, checkpointStage);
            Status = RunStatus.Active;
        }


        // ---- Tutorial Prologue (Run/Prologue.cs, Tutorial_Prologue_Design.md) ----

        public PrologueScript Tutorial { get; private set; }
        public bool IsPrologue => Map != null && Map.isPrologue;
        MapDef dungeonMapAfterPrologue;
        int[] entryHp, entryMp;
        readonly List<string> entryItems = new List<string>();
        readonly Dictionary<string, int> entryBag = new Dictionary<string, int>();

        // Swaps in the 3-node Prologue map; the Dungeon 1 map comes back when the Prologue ends.
        public void BeginPrologue()
        {
            dungeonMapAfterPrologue = Map;
            Map = Prologue.Map();
            Tutorial = new PrologueScript();
            Current = null;
            Cleared.Clear();
            PendingReward = null;
            ModifierManager.ClearAll();
        }

        // Skipping grants exactly what playing grants (design §4.2).
        public void SkipPrologue()
        {
            foreach (var u in Party) u.GainExp(Prologue.TotalExp);
            foreach (var id in Prologue.Node2Items) TryAddConsumable(id);
            EndPrologue();
        }

        // Both paths end here, in the same state: full HP/MP, Prologue Gold banked, committed as the New Game baseline.
        void EndPrologue()
        {
            if (dungeonMapAfterPrologue != null) Map = dungeonMapAfterPrologue;
            dungeonMapAfterPrologue = null;
            Tutorial = null;
            Current = null;
            Cleared.Clear();
            PendingReward = null;
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            Stats.nodesCompleted = Stats.battlesWon = 0;
            foreach (var u in Party) { u.hp = u.stats.maxHp; u.mp = u.stats.maxMp; u.shield = 0; }
            HubGold += Prologue.ClearGold;
            Rng = new Random(Seed);
            PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateCommittedSnapshot(this));
        }

        void SnapshotNodeEntry()
        {
            entryHp = Party.Select(u => u.hp).ToArray();
            entryMp = Party.Select(u => u.mp).ToArray();
            entryItems.Clear();
            entryItems.AddRange(Items);
            entryBag.Clear();
            foreach (var c in Inventory.consumables) entryBag[c.itemId] = c.count;
        }

        // A Prologue wipe restarts the node as it was on entry (design §4.1); the hardcore rule starts at Dungeon 1.
        void RetryPrologueNode()
        {
            Tutorial.Wipes++;
            for (int i = 0; i < Party.Count; i++)
            {
                Party[i].hp = entryHp[i];
                Party[i].mp = entryMp[i];
                Party[i].shield = 0;
            }
            Items.Clear();
            Items.AddRange(entryItems);
            foreach (var id in entryBag.Keys.Union(Inventory.consumables.Select(c => c.itemId)).ToList())
            {
                int diff = (entryBag.TryGetValue(id, out var n) ? n : 0) - Inventory.GetConsumableCount(id);
                if (diff > 0) Inventory.AddConsumable(id, diff);
                else if (diff < 0) Inventory.RemoveConsumable(id, -diff);
            }
            PendingReward = null;
            BattleBuffs.Clear();
            Rng = Prologue.NodeRng(Current.floor);
        }

        void WinPrologueCombat()
        {
            var standing = Party.Select(u => u.Alive).ToArray();
            Stats.battlesWon++;
            foreach (var u in Party) if (!u.Alive) u.hp = 1;
            bool boss = Current.type == NodeType.Boss;
            bool isSecondFight = Current.encounter == "tut_n2";
            PendingReward = new RewardOffer
            {
                title = boss ? "The Threshold Opens" : "Victory",
                text = boss ? $"Aetherion yields. The path to Layer 1 is open. +{Prologue.ClearGold} Hub Gold."
                    : isSecondFight ? "Supplies recovered: 2 Draughts and 1 Ether."
                    : "The rift residue fades.",
            };
            AwardBattleExp(Prologue.NodeExp[Math.Min(Current.floor, Prologue.NodeExp.Length - 1)], standing);
            if (isSecondFight) foreach (var id in Prologue.Node2Items) TryAddConsumable(id);
            // Non-boss Prologue battles offer 3 combat/recovery/QTE modifiers (Utility gold/loot mods have no effect in Prologue).
            // The Prologue boss (p2) excludes modifiers because EndPrologue clears them before entering the Hub.
            if (!boss)
            {
                PendingReward.modifierChoices = RewardGenerator.GenerateModifierChoices(
                    NodeRng(Current, 3), count: 3, isMaxed: ModifierManager.IsAtCap,
                    filter: m => m.category != ModifierCategory.Utility);
            }
        }

        // ---- rewards ----

        // choice: index into PendingReward.choices, or -1 to take only the gold. Applied exactly once.
        public void ClaimReward(int choice, int modifierChoice = -1, ElementType? attunementElement = null)
        {
            var r = PendingReward ?? throw new InvalidOperationException("No reward pending");
            if (choice < -1 || choice >= r.choices.Count) throw new ArgumentOutOfRangeException(nameof(choice));
            if (modifierChoice < -1 || modifierChoice >= r.modifierChoices.Count) throw new ArgumentOutOfRangeException(nameof(modifierChoice));
            PendingReward = null;
            AddGold(r.gold);
            if (!string.IsNullOrEmpty(r.bonusItemId))
                Grant(new RewardOption { kind = RewardKind.Item, id = r.bonusItemId });
            if (choice >= 0) Grant(r.choices[choice]);
            if (modifierChoice >= 0)
            {
                var modOpt = r.modifierChoices[modifierChoice];
                var elem = attunementElement.HasValue && attunementElement.Value.IsValidElement()
                    ? attunementElement.Value
                    : modOpt.element;
                Grant(new RewardOption { kind = RewardKind.Modifier, id = modOpt.id, element = elem });
            }
            Complete();
        }

        public void Grant(RewardOption o)
        {
            switch (o.kind)
            {
                case RewardKind.Item:
                    if (Database.Equipment.ContainsKey(o.id))
                    {
                        var eq = Database.Equipment[o.id];
                        var inst = EquipmentLootGenerator.CreateInstance(eq, eq.rarity, eq.equipmentLevel, Rng);
                        Inventory.TryAddEquipment(inst, out _);
                    }
                    else
                    {
                        TryAddConsumable(o.id); // at the carry cap the item is not added (the reward screen says so)
                    }
                    break;
                case RewardKind.Equipment:
                    if (Database.Equipment.TryGetValue(o.id, out var eqDef))
                    {
                        var inst = EquipmentLootGenerator.CreateInstance(eqDef, eqDef.rarity, eqDef.equipmentLevel, Rng);
                        Inventory.TryAddEquipment(inst, out _);
                    }
                    break;
                case RewardKind.Relic: AddRelic(o.id); break;
                case RewardKind.Upgrade: Upgrade(o.id); break;
                case RewardKind.Modifier: ModifierManager.AddModifier(o.id, this, o.element, Rng); break;
                default: AddGold(o.amount); break;
            }
        }

        // ---- consumables: carry cap and Hub Shop stock (design decision 2026-11-20) ----
        // Each consumable stacks up to its ItemDef.maxStack (Draught and Ether: 10). Items (the battle list) and Inventory
        // (the bag) both hold consumables; the larger count is used, so a drift between them can never slip past the cap.
        public int CarryCount(string itemId) =>
            Math.Max(Items.Count(i => i == itemId), Inventory?.GetConsumableCount(itemId) ?? 0);

        public int CarrySpace(string itemId) =>
            Database.Items.TryGetValue(itemId, out var def) ? Math.Max(0, def.maxStack - CarryCount(itemId)) : int.MaxValue;

        // Adds one consumable if there is room; false (nothing added) at the carry cap.
        public bool TryAddConsumable(string itemId)
        {
            if (CarrySpace(itemId) <= 0) return false;
            Items.Add(itemId);
            Inventory?.AddConsumable(itemId, 1);
            return true;
        }

        // Remaining Hub Shop stock for limited items (HubEconomy.HubRestock); refilled when a dungeon is cleared.
        public readonly Dictionary<string, int> HubShopStock = new Dictionary<string, int>();

        public void RestockHubShop()
        {
            HubShopStock.Clear();
            foreach (var kv in HubEconomy.HubRestock) HubShopStock[kv.Key] = kv.Value;
        }

        public void AddGold(int amount)
        {
            if (amount > 0)
            {
                ModifierManager.ModifyGoldReward(ref amount);
                RelicManager.ModifyGoldReward(ref amount);
                Stats.goldEarned += amount;
            }
            Gold += amount;
        }

        public bool TryAddRelic(string id, out string error)
        {
            if (!RelicManager.TryAddRelic(id, out error))
                return false;

            if (!Relics.Contains(id))
                Relics.Add(id);

            if (Database.Relics.TryGetValue(id, out var relic))
            {
                foreach (var u in Party)
                {
                    if (!relic.abilityBonus.IsZero) u.AddPermanentAbilityBonuses(relic.abilityBonus);
                    if (relic.modifiers != null)
                        foreach (var m in relic.modifiers) u.AddPermanentModifier(m);
                    if (!relic.bonus.IsZero) u.Improve(relic.bonus);
                }
            }
            return true;
        }

        public void AddRelic(string id)
        {
            TryAddRelic(id, out _);
        }

        public bool TryReplaceRelic(string oldId, string newId, out string error)
        {
            if (!RelicManager.ReplaceRelic(oldId, newId, out error))
                return false;

            Relics.Remove(oldId);
            if (!Relics.Contains(newId))
                Relics.Add(newId);

            return true;
        }

        public static UpgradeDef GetUpgradeFor(Unit u)
        {
            if (u != null && Database.Upgrades.TryGetValue(u.id, out var up)) return up;
            if (u?.classDef != null && Database.Upgrades.TryGetValue(u.classDef.id, out var cup)) return cup;
            return Database.Upgrades["knight"];
        }

        // Who an upgrade reward lands on and what it grants. Shared by the grant and the reward card so they always agree.
        public static (Unit hero, UpgradeDef up) ResolveUpgrade(IList<Unit> party, string character)
        {
            var u = party.FirstOrDefault(p => p.id == character)
                    ?? party.FirstOrDefault(p => p.classDef?.id == character)
                    ?? party[0];
            return (u, Database.Upgrades.TryGetValue(character, out var def) ? def : GetUpgradeFor(u));
        }

        void Upgrade(string character)
        {
            var (u, up) = ResolveUpgrade(Party, character);
            if (!up.abilityBonus.IsZero) u.AddPermanentAbilityBonuses(up.abilityBonus);
            if (up.modifiers != null)
                foreach (var m in up.modifiers) u.AddPermanentModifier(m);
            u.Improve(up.bonus);
        }

        // ---- rest ----

        public static int RecoverAmount(Unit u) => u.Alive ? (int)Math.Round((u.stats.maxHp - u.hp) * RunConfig.RestRecover) : 0;
        public static int RecoverMpAmount(Unit u) => u.Alive ? (int)Math.Round((u.stats.maxMp - u.mp) * RunConfig.RestRecover) : 0;

        public void RestRecover()
        {
            if (!Pending(NodeType.Rest)) throw new InvalidOperationException("Not at an unused rest site");
            foreach (var u in Party)
            {
                if (!u.Alive) continue;
                u.hp += RecoverAmount(u);
                u.mp += RecoverMpAmount(u);
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

        public void RestApplyModifier(string modifierId)
        {
            if (!Pending(NodeType.Rest)) throw new InvalidOperationException("Not at an unused rest site");
            if (!Database.DungeonModifiers.ContainsKey(modifierId)) throw new ArgumentException(modifierId);
            ModifierManager.AddModifier(modifierId, this, rng: Rng);
            Complete();
        }

        // ---- events ----

        static string RollEventEquipmentId(Random rng, int act)
        {
            int maxLvl = Math.Max(2, act * 2);
            var pool = Database.Equipment.Values
                .Where(eq => !string.IsNullOrEmpty(eq.id) && eq.equipmentLevel <= maxLvl && eq.rarity <= EquipmentRarity.Rare)
                .ToList();
            if (pool.Count == 0)
                pool = Database.Equipment.Values.Where(eq => !string.IsNullOrEmpty(eq.id)).ToList();
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)].id;
        }

        static string RaceDisplayName(string raceId)
        {
            if (string.IsNullOrEmpty(raceId)) return "Race";
            return Database.Races.TryGetValue(raceId, out var r) ? r.name : raceId;
        }

        bool HasConsumableForEvent(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return (Inventory != null && Inventory.GetConsumableCount(itemId) > 0) || Items.Contains(itemId);
        }

        void ConsumeEventItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            Inventory?.RemoveConsumable(itemId, 1);
            Items.Remove(itemId);
        }

        public EventChoiceStatus GetEventChoiceStatus(int index)
        {
            if (CurrentEvent?.choices == null || index < 0 || index >= CurrentEvent.choices.Length)
            {
                return new EventChoiceStatus { isVisible = false, isBlocked = true, blockedReason = "Invalid choice", effectScale = 1f };
            }
            return EvaluateEventChoice(CurrentEvent, CurrentEvent.choices[index]);
        }

        EventChoiceStatus EvaluateEventChoice(EventDef ev, EventChoice c)
        {
            var status = new EventChoiceStatus { isVisible = true, effectScale = 1f };
            if (c == null)
            {
                status.isVisible = false;
                status.isBlocked = true;
                status.blockedReason = "Invalid choice";
                return status;
            }

            if (c.isImprovise)
            {
                status.isImprovise = true;
                bool hasOtherRacial = ev != null && ev.HasAnyRacialChoice();
                bool hasHuman = HasRace("human");
                status.isVisible = hasHuman && !hasOtherRacial;
                if (!hasHuman)
                {
                    status.isBlocked = true;
                    status.blockedReason = "Requires Human (Adaptability)";
                    return status;
                }
                if (hasOtherRacial)
                {
                    status.isBlocked = true;
                    status.blockedReason = "Improvise unavailable on racial events";
                    return status;
                }
            }
            else if (!string.IsNullOrEmpty(c.requiredRace))
            {
                if (!HasRace(c.requiredRace))
                {
                    if (HasRace("human") && HumanAdaptabilityUsesRemaining > 0)
                    {
                        status.usesAdaptabilityBorrow = true;
                        status.effectScale = 0.75f;
                    }
                    else
                    {
                        status.isBlocked = true;
                        status.blockedReason = $"Requires {RaceDisplayName(c.requiredRace)}";
                        return status;
                    }
                }
            }

            if (c.goldCost > Gold)
            {
                status.isBlocked = true;
                status.blockedReason = $"Need {c.goldCost} spoils";
                return status;
            }

            if (!string.IsNullOrEmpty(c.requiredItem) && !HasConsumableForEvent(c.requiredItem))
            {
                string itemName = Database.Items.TryGetValue(c.requiredItem, out var itm) ? itm.name : c.requiredItem;
                status.isBlocked = true;
                status.blockedReason = $"Need {itemName}";
                return status;
            }

            if (!string.IsNullOrEmpty(c.requiredMaterialId))
            {
                int reqAmt = Math.Max(1, c.requiredMaterialAmount);
                int ownedAmt = Inventory != null ? Inventory.GetMaterialCount(c.requiredMaterialId) : 0;
                if (ownedAmt < reqAmt)
                {
                    string matName = Database.Materials.TryGetValue(c.requiredMaterialId, out var mat) ? mat.name : c.requiredMaterialId;
                    status.isBlocked = true;
                    status.blockedReason = $"Need {reqAmt}x {matName}";
                    return status;
                }
            }

            if (c.MayGrantRelic && RelicManager.IsFull)
            {
                status.isBlocked = true;
                status.blockedReason = "Relic capacity full (5/5)";
                return status;
            }

            if (c.MayGrantRelic && EventRelic == null)
            {
                status.isBlocked = true;
                status.blockedReason = "Nothing left to gain";
                return status;
            }

            if (c.MayGrantEquipment && Inventory != null && Inventory.IsEquipmentFull)
            {
                status.isBlocked = true;
                status.blockedReason = "Equipment inventory full";
                return status;
            }

            return status;
        }

        // Returns the highest Attribute Modifier for the requested Ability across all active/living party members (u.Alive).
        public int GetPartyHighestAttributeModifier(Ability ability)
        {
            if (Party == null || Party.Count == 0) return 0;
            var living = Party.Where(u => u != null && u.Alive).ToList();
            if (living.Count == 0) living = Party.Where(u => u != null).ToList();
            return living.Count == 0 ? 0 : living.Max(u => u.Mod(ability));
        }

        // Deterministic D20 roll (1..20) for an event choice, or an explicit override for unit testing.
        public int GetEventChoiceD20Roll(int index)
        {
            if (EventChoiceD20Rolls.TryGetValue(index, out int preRolled) && preRolled >= 1 && preRolled <= 20)
                return preRolled;
            if (Current != null)
            {
                int rolled = NodeRng(Current, 40 + index).Next(1, 21);
                EventChoiceD20Rolls[index] = rolled;
                return rolled;
            }
            return Rng != null ? Rng.Next(1, 21) : 10;
        }

        public void SetEventChoiceD20RollOverride(int index, int d20Roll)
        {
            EventChoiceD20Rolls[index] = Math.Clamp(d20Roll, 1, 20);
        }

        // Null when the choice can be taken, otherwise why not.
        public string EventBlocked(EventChoice c) => EvaluateEventChoice(CurrentEvent, c).blockedReason;

        static int ScaleEffect(int val, float scale) => val <= 0 ? 0 : (scale >= 0.999f ? val : Math.Max(1, (int)Math.Round(val * scale)));

        // Gold the choice pays on success, exactly as ChooseEvent -> AddGold would land it (scale, modifiers, relics).
        public int PreviewEventGold(int index)
        {
            var c = CurrentEvent?.choices?[index];
            if (c == null || c.gold <= 0) return 0;
            int g = ScaleEffect(RunConfig.ScaleEventGold(c.gold), EvaluateEventChoice(CurrentEvent, c).effectScale);
            ModifierManager.ModifyGoldReward(ref g);
            RelicManager.ModifyGoldReward(ref g);
            return g;
        }

        public string ChooseEvent(int index)
        {
            var e = CurrentEvent;
            if (e == null || !Pending(NodeType.Event)) throw new InvalidOperationException("No event pending");
            if (e.choices == null || index < 0 || index >= e.choices.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            var c = e.choices[index];
            var status = EvaluateEventChoice(e, c);
            if (status.isBlocked) throw new InvalidOperationException(status.blockedReason);

            NodeDef eventNode = Current;
            CurrentEvent = null;
            LastAttributeCheckResult = null;
            if (status.usesAdaptabilityBorrow)
                HumanAdaptabilityUsesRemaining = Math.Max(0, HumanAdaptabilityUsesRemaining - 1);

            float scale = status.effectScale;
            int ScalePositive(int val) => ScaleEffect(val, scale);

            Gold -= c.goldCost;
            if (!string.IsNullOrEmpty(c.requiredItem))
                ConsumeEventItem(c.requiredItem);
            if (!string.IsNullOrEmpty(c.requiredMaterialId))
                Inventory?.RemoveMaterial(c.requiredMaterialId, Math.Max(1, c.requiredMaterialAmount));

            if (c.isAttributeCheck)
            {
                return ResolveAttributeCheckEventChoice(e, c, index, eventNode);
            }

            bool riskFailed = EventChoiceRiskFailed.TryGetValue(index, out var rf) && rf;
            bool grantSuccessRewards = !riskFailed || c.riskGrantsRewardOnFail;

            var notes = new List<string>();
            if (status.usesAdaptabilityBorrow)
                notes.Add("[Human Adaptability: 75% effect]");

            int goldGain = grantSuccessRewards ? ScalePositive(RunConfig.ScaleEventGold(c.gold)) : 0;
            if (goldGain > 0)
            {
                int before = Gold;
                AddGold(goldGain); // modifiers/relics may change the amount; report what actually landed
                notes.Add($"Gained {Gold - before} spoils.");
            }

            int healPct = grantSuccessRewards ? ScalePositive(c.healPercent) : 0;
            int mpPct = grantSuccessRewards ? ScalePositive(c.mpRestorePercent) : 0;
            int directHurtPct = c.hurtPercent + (riskFailed ? c.riskFailHurtPercent : 0);
            int rawHazardPct = c.hazardDamagePercent + (riskFailed ? c.riskFailHazardDamagePercent : 0);

            string trapNegatedNote = null;
            if (rawHazardPct > 0 && c.isTrapChoice && RelicManager != null && RelicManager.TryNegateMinorChestTrap(out var negatedMsg))
            {
                rawHazardPct = 0;
                trapNegatedNote = negatedMsg;
            }

            float hazardFactor = (rawHazardPct > 0 && HasHalflingHazardReduction) ? 0.5f : 1f;
            if (rawHazardPct > 0 && HasHalflingHazardReduction)
                notes.Add("[Halfling Small and Nimble: -50% hazard damage]");
            if (trapNegatedNote != null)
                notes.Add(trapNegatedNote);

            foreach (var u in Party.Where(u => u.Alive))
            {
                if (healPct > 0) u.Heal(u.stats.maxHp * healPct / 100);
                if (mpPct > 0) u.RestoreMp(Math.Max(1, u.stats.maxMp * mpPct / 100));
                if (directHurtPct > 0) u.hp = Math.Max(1, u.hp - u.stats.maxHp * directHurtPct / 100);
                if (rawHazardPct > 0)
                {
                    int dmg = Math.Max(1, (int)Math.Round(u.stats.maxHp * (rawHazardPct / 100f) * hazardFactor));
                    u.hp = Math.Max(1, u.hp - dmg);
                }
            }

            if (grantSuccessRewards && !string.IsNullOrEmpty(c.item))
            {
                bool added = TryAddConsumable(c.item);
                if (Database.Items.TryGetValue(c.item, out var itemDef))
                    notes.Add(added ? $"Gained {itemDef.name}." : $"No room for another {itemDef.name} (carry limit {itemDef.maxStack}).");
            }

            if (grantSuccessRewards && !string.IsNullOrEmpty(c.materialId))
            {
                int matAmt = ScalePositive(Math.Max(1, c.materialAmount));
                if (e.HasTag(EventTags.Mineral) && HasRace("dwarf"))
                {
                    matAmt += 1;
                    notes.Add("[Dwarf Stonecraft: +1 upgrade material]");
                }
                Inventory?.AddMaterial(c.materialId, matAmt);
                string matName = Database.Materials.TryGetValue(c.materialId, out var matDef) ? matDef.name : c.materialId;
                notes.Add($"Gained {matAmt}x {matName}.");
            }

            if (grantSuccessRewards && (c.randomEquipment || !string.IsNullOrEmpty(c.equipmentId)))
            {
                string eqId = !string.IsNullOrEmpty(c.equipmentId) ? c.equipmentId : EventEquipment;
                if (!string.IsNullOrEmpty(eqId) && Database.Equipment.TryGetValue(eqId, out var eqDef))
                {
                    var inst = EquipmentLootGenerator.CreateInstance(eqDef, eqDef.rarity, eqDef.equipmentLevel, Rng);
                    if (Inventory == null || Inventory.TryAddEquipment(inst, out _))
                        notes.Add($"Gained {eqDef.name}.");
                }
            }

            string relicNote = null;
            if (grantSuccessRewards && c.relic && !string.IsNullOrEmpty(EventRelic))
            {
                AddRelic(EventRelic);
                relicNote = $"Gained {Database.Relics[EventRelic].name}.";
            }

            if (grantSuccessRewards && c.battleBuff != null)
            {
                var buff = scale < 0.999f
                    ? new StatusApply { id = c.battleBuff.id, turns = c.battleBuff.turns, magnitude = c.battleBuff.magnitude * scale }
                    : c.battleBuff;
                BattleBuffs.Add(buff);
            }

            if (grantSuccessRewards && EventModifiers.TryGetValue(index, out var evMod) && !string.IsNullOrEmpty(evMod.id))
            {
                ModifierManager.AddModifier(evMod.id, this, evMod.element, Rng);
                if (Database.DungeonModifiers.TryGetValue(evMod.id, out var mdef))
                {
                    string modName = mdef.isElementSpecific && evMod.element.IsValidElement()
                        ? $"{mdef.displayName} ({evMod.element})"
                        : mdef.displayName;
                    notes.Add($"Gained Modifier: {modName}.");
                }
            }

            if (EventCurseModifiers.TryGetValue(index, out var curseMod) && !string.IsNullOrEmpty(curseMod.id))
            {
                ModifierManager.AddModifier(curseMod.id, this, curseMod.element, Rng);
                if (Database.DungeonModifiers.TryGetValue(curseMod.id, out var cdef))
                    notes.Add($"Afflicted: {cdef.displayName}.");
            }

            if (riskFailed && EventRiskFailModifiers.TryGetValue(index, out var failMod) && !string.IsNullOrEmpty(failMod.id))
            {
                ModifierManager.AddModifier(failMod.id, this, failMod.element, Rng);
                if (Database.DungeonModifiers.TryGetValue(failMod.id, out var fdef))
                    notes.Add($"Afflicted: {fdef.displayName}.");
            }

            if (grantSuccessRewards && !string.IsNullOrEmpty(c.bonusInfoText))
                notes.Add(c.bonusInfoText);

            Complete();
            string baseOutcome = riskFailed && !string.IsNullOrEmpty(c.riskFailOutcome) ? c.riskFailOutcome : c.outcome;
            string outcomeText = relicNote != null ? $"{baseOutcome} {relicNote}" : baseOutcome;
            if (notes.Count > 0)
                outcomeText = $"{outcomeText} {string.Join(" ", notes)}";
            return outcomeText;
        }

        string ResolveAttributeCheckEventChoice(EventDef e, EventChoice c, int index, NodeDef eventNode)
        {
            int d20 = GetEventChoiceD20Roll(index);
            int highestMod = GetPartyHighestAttributeModifier(c.checkAbility);
            var category = StatCalculator.EvaluateD20AttributeCheck(d20, highestMod, c.difficultyClass, out int checkResult);

            var summary = new AttributeCheckRollSummary
            {
                ability = c.checkAbility,
                difficultyClass = c.difficultyClass,
                d20Roll = d20,
                partyHighestModifier = highestMod,
                checkResult = checkResult,
                category = category
            };
            LastAttributeCheckResult = summary;

            var tierDef = c.GetOutcomeForCategory(category);
            var notes = new List<string> { summary.FormatBanner() };

            if (tierDef != null)
            {
                int tierGold = RunConfig.ScaleEventGold(tierDef.gold);
                if (tierGold > 0)
                {
                    int before = Gold;
                    AddGold(tierGold);
                    notes.Add($"Gained {Gold - before} spoils.");
                }

                int healPct = tierDef.healPercent;
                int mpPct = tierDef.mpRestorePercent;
                int directHurtPct = tierDef.hurtPercent;
                int rawHazardPct = tierDef.hazardDamagePercent;

                string trapNegatedNote = null;
                if (rawHazardPct > 0 && c.isTrapChoice && RelicManager != null && RelicManager.TryNegateMinorChestTrap(out var negatedMsg))
                {
                    rawHazardPct = 0;
                    trapNegatedNote = negatedMsg;
                }

                float hazardFactor = (rawHazardPct > 0 && HasHalflingHazardReduction) ? 0.5f : 1f;
                if (rawHazardPct > 0 && HasHalflingHazardReduction)
                    notes.Add("[Halfling Small and Nimble: -50% hazard damage]");
                if (trapNegatedNote != null)
                    notes.Add(trapNegatedNote);

                foreach (var u in Party.Where(u => u.Alive))
                {
                    if (healPct > 0) u.Heal(u.stats.maxHp * healPct / 100);
                    if (mpPct > 0) u.RestoreMp(Math.Max(1, u.stats.maxMp * mpPct / 100));
                    if (directHurtPct > 0) u.hp = Math.Max(1, u.hp - u.stats.maxHp * directHurtPct / 100);
                    if (rawHazardPct > 0)
                    {
                        int dmg = Math.Max(1, (int)Math.Round(u.stats.maxHp * (rawHazardPct / 100f) * hazardFactor));
                        u.hp = Math.Max(1, u.hp - dmg);
                    }
                }

                if (!string.IsNullOrEmpty(tierDef.item))
                {
                    bool added = TryAddConsumable(tierDef.item);
                    if (Database.Items.TryGetValue(tierDef.item, out var itemDef))
                        notes.Add(added ? $"Gained {itemDef.name}." : $"No room for another {itemDef.name} (carry limit {itemDef.maxStack}).");
                }

                if (!string.IsNullOrEmpty(tierDef.materialId))
                {
                    int matAmt = Math.Max(1, tierDef.materialAmount);
                    if (e.HasTag(EventTags.Mineral) && HasRace("dwarf"))
                    {
                        matAmt += 1;
                        notes.Add("[Dwarf Stonecraft: +1 upgrade material]");
                    }
                    Inventory?.AddMaterial(tierDef.materialId, matAmt);
                    string matName = Database.Materials.TryGetValue(tierDef.materialId, out var matDef) ? matDef.name : tierDef.materialId;
                    notes.Add($"Gained {matAmt}x {matName}.");
                }

                if (tierDef.randomEquipment || !string.IsNullOrEmpty(tierDef.equipmentId))
                {
                    string eqId = !string.IsNullOrEmpty(tierDef.equipmentId) ? tierDef.equipmentId : EventEquipment;
                    if (!string.IsNullOrEmpty(eqId) && Database.Equipment.TryGetValue(eqId, out var eqDef))
                    {
                        var inst = EquipmentLootGenerator.CreateInstance(eqDef, eqDef.rarity, eqDef.equipmentLevel, Rng);
                        if (Inventory == null || Inventory.TryAddEquipment(inst, out _))
                            notes.Add($"Gained {eqDef.name}.");
                    }
                }

                if (tierDef.relic && !string.IsNullOrEmpty(EventRelic))
                {
                    AddRelic(EventRelic);
                    notes.Add($"Gained {Database.Relics[EventRelic].name}.");
                }

                if (tierDef.battleBuff != null)
                {
                    BattleBuffs.Add(tierDef.battleBuff);
                }

                if (!string.IsNullOrEmpty(tierDef.modifierId) && Database.DungeonModifiers.TryGetValue(tierDef.modifierId, out var mdef))
                {
                    var elem = tierDef.modifierElement;
                    if (!elem.IsValidElement() && mdef.isElementSpecific)
                    {
                        var modRng = eventNode != null ? NodeRng(eventNode, 50 + index) : (Rng ?? new Random(Seed));
                        elem = ElementTypes.All[modRng.Next(ElementTypes.All.Length)];
                    }
                    ModifierManager.AddModifier(tierDef.modifierId, this, elem, Rng);
                    string modName = mdef.isElementSpecific && elem.IsValidElement()
                        ? $"{mdef.displayName} ({elem})"
                        : mdef.displayName;
                    notes.Add($"Gained Modifier: {modName}.");
                }

                if (!string.IsNullOrEmpty(tierDef.curseModifierId) && Database.DungeonModifiers.TryGetValue(tierDef.curseModifierId, out var cdef))
                {
                    var celem = cdef.isElementSpecific
                        ? ElementTypes.All[(eventNode != null ? NodeRng(eventNode, 60 + index) : (Rng ?? new Random(Seed))).Next(ElementTypes.All.Length)]
                        : ElementType.None;
                    ModifierManager.AddModifier(tierDef.curseModifierId, this, celem, Rng);
                    notes.Add($"Afflicted: {cdef.displayName}.");
                }

                if (!string.IsNullOrEmpty(tierDef.bonusInfoText))
                    notes.Add(tierDef.bonusInfoText);
            }

            Complete();
            string baseOutcome = tierDef != null && !string.IsNullOrEmpty(tierDef.outcome) ? tierDef.outcome : (c.outcome ?? "");
            return string.IsNullOrEmpty(baseOutcome)
                ? string.Join(" ", notes)
                : $"{baseOutcome} {string.Join(" ", notes)}";
        }

        // ---- node completion ----

        void Complete()
        {
            if (IsPrologue && Current.type == NodeType.Boss) { EndPrologue(); return; }
            Cleared.Add(Current.id);
            Stats.nodesCompleted++;
            if (Current.type == NodeType.Boss)
            {
                if (IsFinalStage)
                {
                    Status = RunStatus.Victory;
                    EndedUtc = DateTime.UtcNow;
                    // GDD Bab 15 §3.1: All dungeon modifiers expire upon dungeon clear (after boss reward screen).
                    ModifierManager.ClearAll();
                }
                else
                {
                    AdvanceToNextStage();
                }
            }
        }

        // Advances to the next stage within the same dungeon, generating a fresh node map
        // and placing the party back at floor 0 while preserving run state (HP/MP, Gold, Relics, Modifiers, Stats).
        public bool AdvanceToNextStage(MapDef overrideNextStageMap = null)
        {
            if (Map == null || IsFinalStage) return false;
            int nextStage = Stage + 1;
            var nextMap = overrideNextStageMap ?? MapGenerator.Generate(DungeonSeed, Act, nextStage);
            if (nextMap == null || nextMap.act != Act || nextMap.stage != nextStage) return false;

            Map = nextMap;
            Current = null;
            Cleared.Clear();
            CurrentEvent = null;
            EventRelic = null;
            EventEquipment = null;
            EventModifiers.Clear();
            EventCurseModifiers.Clear();
            EventRiskFailModifiers.Clear();
            EventChoiceRiskFailed.Clear();
            EventChoiceD20Rolls.Clear();
            LastAttributeCheckResult = null;
            return true;
        }

        public bool CanReturnToHubAtStageCheckpoint =>
            Status == RunStatus.Active && !IsPrologue && Stage > 1 && Current == null && Cleared.Count == 0;

        // Optional Hub extraction after clearing a Stage Mini-Boss (Stage 1 -> 2, Stage 2 -> 3, etc.).
        // Commits party progression & Spoils (1:1 to Hub Gold), records the next stage as checkpoint,
        // clears all Dungeon Modifiers and Event Battle Buffs, spends 0 retries, and does not restock Hub Shop.
        public int ReturnToHubAtStageCheckpoint()
        {
            if (!CanReturnToHubAtStageCheckpoint)
                throw new InvalidOperationException("Cannot extract to Hub: not at a stage checkpoint.");

            int transferredSpoils = Gold;
            ModifierManager.ClearAll();
            BattleBuffs.Clear();
            ResetDungeonUtilityCharges();
            PersistenceCommitHandler.CommitStageCheckpoint(this);
            return transferredSpoils;
        }

        // Every entry into a dungeon from the Hub is a new attempt with its own seed: the map, node rewards,
        // event risk rolls, dungeon shop and combat rolls all re-roll, so a retry after a wipe or abandon
        // (or reloading a Hub save) is a fresh dungeon, not a replay of a known one.
        public void BeginDungeonAttempt(int act, int dungeonSeed, int stage = 1)
        {
            int safeAct = Math.Clamp(act, 1, 8);
            int safeStage = Math.Clamp(stage, 1, MapGenerator.GetStageCount(safeAct));
            DungeonSeed = dungeonSeed;
            Rng = new Random(dungeonSeed);
            Gold = 0; // a new run starts with 0 Run Gold; Hub Gold stays in the Hub
            RestartCurrentDungeon(MapGenerator.Generate(dungeonSeed, safeAct, safeStage));
        }

        // Resets the current dungeon run cleanly back to Stage 1 floor 0.
        public void RestartCurrentDungeon(MapDef stageOneMap = null)
        {
            int act = Act;
            Map = stageOneMap ?? MapGenerator.Generate(DungeonSeed, act, 1);
            Current = null;
            Cleared.Clear();
            Status = RunStatus.Active;
            EndedUtc = null;
            PendingReward = null;
            CurrentEvent = null;
            EventRelic = null;
            EventEquipment = null;
            EventModifiers.Clear();
            EventCurseModifiers.Clear();
            EventRiskFailModifiers.Clear();
            EventChoiceRiskFailed.Clear();
            EventChoiceD20Rolls.Clear();
            LastAttributeCheckResult = null;
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            ResetDungeonUtilityCharges();
            CurrentDungeonShop = DungeonShopData.Generate(Map?.act ?? 1, Rng);
            CurrentShop = CurrentDungeonShop.equipmentShop;
        }

        // Restores progression and per-character runtime state from a saved game (Section 2 & Section 12).
        public void RestoreFromSave(GameSaveData data)
        {
            if (data == null) return;
            Gold = data.gold;
            HubGold = data.hubGold;

            Items.Clear();
            if (data.items != null) Items.AddRange(data.items);
            Relics.Clear();
            RelicManager.ClearAll();
            if (data.relicInstances != null && data.relicInstances.Length > 0)
            {
                RelicManager.RestoreFromSave(data.relicInstances);
                foreach (var inst in RelicManager.ActiveRelics)
                {
                    if (!Relics.Contains(inst.relicId)) Relics.Add(inst.relicId);
                }
            }
            else if (data.relics != null)
            {
                foreach (var rid in data.relics)
                {
                    TryAddRelic(rid, out _);
                }
            }
            Cleared.Clear();
            if (data.clearedNodeIds != null)
            {
                foreach (var cid in data.clearedNodeIds)
                    if (!string.IsNullOrEmpty(cid)) Cleared.Add(cid);
            }
            ModifierManager.RestoreFromSave(data.dungeonModifiers);
            BattleBuffs.Clear();
            if (data.battleBuffs != null)
                foreach (var b in data.battleBuffs)
                    if (b != null) BattleBuffs.Add(new StatusApply { id = b.id, turns = b.turns, magnitude = b.magnitude });
            if (!string.IsNullOrEmpty(data.currentNodeId) && Map.nodes.Any(n => n.id == data.currentNodeId))
                Current = Map.Get(data.currentNodeId);

            if (data.inventoryEquipment != null || data.inventoryConsumables != null || data.inventoryMaterials != null || data.inventoryKeyItems != null)
            {
                var invData = new InventorySaveData
                {
                    equipment = data.inventoryEquipment ?? Array.Empty<EquipmentInstanceSaveData>(),
                    consumables = data.inventoryConsumables ?? Array.Empty<ConsumableStackSaveData>(),
                    materials = data.inventoryMaterials ?? Array.Empty<MaterialEntrySaveData>(),
                    keyItems = data.inventoryKeyItems ?? Array.Empty<string>()
                };
                Inventory.RestoreFromSave(invData);
            }

            if (data.party != null)
            {
                for (int i = 0; i < Math.Min(Party.Count, data.party.Length); i++)
                    RestorePartyMember(Party[i], data.party[i]);
            }

            if (data.currentShopStock != null && data.currentShopStock.Length > 0)
            {
                CurrentShop = new ShopInventory(Act);
                foreach (var s in data.currentShopStock)
                {
                    var item = ShopItem.FromSaveData(s);
                    if (item != null) CurrentShop.items.Add(item);
                }
            }
            else
            {
                CurrentShop = EquipmentShopManager.GenerateShop(Act, Rng);
            }
        }

        static void RestorePartyMember(Unit u, SavedCharacterData sc)
        {
            if (u == null || sc == null) return;
            u.level = Math.Max(1, sc.level);
            u.exp = u.IsMaxLevel ? 0 : Math.Max(0, sc.exp);
            u.selectedTalentIds.Clear();
            if (sc.selectedTalents != null)
            {
                foreach (var tid in sc.selectedTalents)
                    if (!string.IsNullOrEmpty(tid) && !u.selectedTalentIds.Contains(tid))
                        u.selectedTalentIds.Add(tid);
            }
            u.unspentAsiPoints = sc.unspentAsiPoints;
            u.abilities.permanentBonuses = sc.permanentAbilityBonuses;

            u.skillLoadout.InitializeForClass(u.classDef, u.level);
            if (sc.unlockedSkills != null)
            {
                foreach (var sid in sc.unlockedSkills)
                {
                    if (Database.Skills.ContainsKey(sid)) u.skillLoadout.UnlockActive(sid);
                    if (Database.Passives.ContainsKey(sid)) u.skillLoadout.UnlockPassive(sid);
                }
            }
            // Ranks and SP: InitializeForClass resets ranks to 1 and leaves SP untouched, so both come from the save.
            if (sc.skillRankIds != null && sc.skillRankValues != null)
            {
                for (int j = 0; j < Math.Min(sc.skillRankIds.Length, sc.skillRankValues.Length); j++)
                    if (!string.IsNullOrEmpty(sc.skillRankIds[j]) && sc.skillRankValues[j] > 0)
                        u.skillLoadout.SkillRanks[sc.skillRankIds[j]] = sc.skillRankValues[j];
            }
            u.skillLoadout.SkillPoints = Math.Max(0, sc.skillPoints);
            if (sc.activeSkillLoadout != null && sc.activeSkillLoadout.Length > 0)
            {
                for (int j = 0; j < Math.Min(SkillLoadout.ActiveSlotCount, sc.activeSkillLoadout.Length); j++)
                    u.skillLoadout.activeSlots[j] = sc.activeSkillLoadout[j];
            }
            if (sc.passiveSkillLoadout != null && sc.passiveSkillLoadout.Length > 0)
            {
                for (int j = 0; j < Math.Min(SkillLoadout.PassiveSlotCount, sc.passiveSkillLoadout.Length); j++)
                    u.skillLoadout.passiveSlots[j] = sc.passiveSkillLoadout[j];
                u.passives.Clear();
                foreach (var pid in u.skillLoadout.GetEquippedPassives())
                    if (Database.Passives.TryGetValue(pid, out var p) && !u.passives.Contains(p))
                        u.passives.Add(p);
            }

            if (sc.equippedInstances != null && sc.equippedInstances.Length > 0)
            {
                u.equipped.Clear();
                u.equippedInstances.Clear();
                foreach (var eqData in sc.equippedInstances)
                {
                    var inst = EquipmentInstance.FromSaveData(eqData);
                    if (inst != null && eqData.slot >= 0)
                    {
                        var slot = (EquipmentSlot)eqData.slot;
                        u.TryEquip(slot, inst, out _);
                    }
                }
            }

            u.RecalculateStats(fillResources: true);
            if (sc.hp > 0) u.hp = Math.Min(u.stats.maxHp, sc.hp);
            if (sc.mp > 0) u.mp = Math.Min(u.stats.maxMp, sc.mp);
        }

        public void RestoreFromCampaignSave(CampaignSaveData data)
        {
            if (data == null) return;
            Codex = EnemyCodex.FromSaveData(data.codex);
            HubGold = data.hubGold;
            Gold = 0; // the committed baseline is a Hub state: no Run Gold
            RetriesLeft = data.retriesLeft;
            if (data.hubShopStock != null && data.hubShopStock.Length > 0)
            {
                HubShopStock.Clear();
                foreach (var s in data.hubShopStock) HubShopStock[s.itemId] = s.count;
            }
            else RestockHubShop(); // saves from before Hub Shop stock existed
            ExpeditionLost = data.expeditionLost;
            Current = null;
            Cleared.Clear();
            PendingReward = null;
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            int targetStage = data.committedAct == data.nextDungeonAct ? data.NormalizedNextStage() : 1;
            if (Map != null && Map.stage != targetStage)
                Map = MapGenerator.Generate(DungeonSeed, Act, targetStage);

            Relics.Clear();
            RelicManager.ClearAll();
            if (data.relicInstances != null && data.relicInstances.Length > 0)
            {
                RelicManager.RestoreFromSave(data.relicInstances);
                foreach (var inst in RelicManager.ActiveRelics)
                {
                    if (!Relics.Contains(inst.relicId)) Relics.Add(inst.relicId);
                }
            }
            else if (data.relics != null)
            {
                foreach (var rid in data.relics)
                {
                    TryAddRelic(rid, out _);
                }
            }

            if (data.inventoryEquipment != null || data.inventoryConsumables != null || data.inventoryMaterials != null || data.inventoryKeyItems != null)
            {
                var invData = new InventorySaveData
                {
                    equipment = data.inventoryEquipment ?? Array.Empty<EquipmentInstanceSaveData>(),
                    consumables = data.inventoryConsumables ?? Array.Empty<ConsumableStackSaveData>(),
                    materials = data.inventoryMaterials ?? Array.Empty<MaterialEntrySaveData>(),
                    keyItems = data.inventoryKeyItems ?? Array.Empty<string>()
                };
                Inventory.RestoreFromSave(invData);
            }

            if (data.party != null)
            {
                for (int i = 0; i < Math.Min(Party.Count, data.party.Length); i++)
                    RestorePartyMember(Party[i], data.party[i]);
            }

            if (data.hubArmoryStock != null && data.hubArmoryStock.Length > 0)
            {
                CurrentShop = new ShopInventory(Act);
                foreach (var s in data.hubArmoryStock)
                {
                    var item = ShopItem.FromSaveData(s);
                    if (item != null) CurrentShop.items.Add(item);
                }
            }
        }

        // Advances progression to the next sequential dungeon (Act 1 -> Act 8).
        // Preserves party levels, HP/MP, ability scores, equipped items, inventory equipment, consumables, and gold.
        // Regenerates the shop stock for the new dungeon tier.
        public bool AdvanceToNextDungeon(MapDef nextMap)
        {
            if (nextMap == null) return false;
            if (Map != null && nextMap.act != Map.act + 1) return false;
            Map = nextMap;
            Current = null;
            Cleared.Clear();
            Status = RunStatus.Active;
            EndedUtc = null;
            PendingReward = null;
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            ResetDungeonUtilityCharges();
            CurrentDungeonShop = DungeonShopData.Generate(nextMap.act, Rng);
            CurrentShop = CurrentDungeonShop.equipmentShop;
            // GDD Bab 14 §2: Relics are permanent player progression and remain owned upon dungeon clear
            return true;
        }

        public int TransferRunGoldToHub()
        {
            return HubEconomy.TransferRunGoldToHub(this);
        }

        // Resets the entire run progression following a party defeat or new game start.
        // Wipes all run-acquired equipment clean from both inventory and party slots, restoring starting equipment only.
        public void ResetToNewGame(IReadOnlyList<CharacterDef> initialPartyDefs, MapDef startingMap = null)
        {
            Status = RunStatus.Active;
            EndedUtc = null;
            Current = null;
            PendingReward = null;
            Gold = 0;
            HubGold = 0;
            Items.Clear();
            Items.AddRange(Database.StartingItems);
            Relics.Clear();
            RelicManager.ClearAll();
            Cleared.Clear();
            BattleBuffs.Clear();
            ModifierManager.ClearAll();
            ResetDungeonUtilityCharges();

            Inventory.equipment.Clear();
            Inventory.consumables.Clear();
            foreach (var itm in Database.StartingItems)
                Inventory.AddConsumable(itm, 1);
            Inventory.materials.Clear();
            Inventory.keyItems.Clear();
            Inventory.AddKeyItem("KEY001");

            Party.Clear();
            if (initialPartyDefs != null)
            {
                foreach (var def in initialPartyDefs)
                    Party.Add(new Unit(def));
            }

            if (startingMap != null)
                Map = startingMap;

            CurrentShop = EquipmentShopManager.GenerateShop(Map != null ? Map.act : 1, Rng);
        }
    }
}
