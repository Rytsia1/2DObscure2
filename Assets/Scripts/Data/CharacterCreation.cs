using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Race definition: configurable Ability Score modifiers (applied after Point Buy), passive bonuses, traits, and racial abilities.
    [Serializable]
    public class RaceDef
    {
        public string id, name, description;
        public AbilityScores abilityModifiers;
        public StatModifier[] passiveBonuses = Array.Empty<StatModifier>();
        public string[] traits = Array.Empty<string>();
        public string[] racialAbilities = Array.Empty<string>();
    }

    // Class definition: combat role, starting skills, proficiencies, stat scaling, and class-specific HP/MP growth.
    // Selecting a Class never overwrites Point Buy Ability Score allocations.
    [Serializable]
    public class ClassDef
    {
        public string id, name, role, description;
        public string[] startingSkills = Array.Empty<string>();
        public string[] weaponProficiencies = Array.Empty<string>();
        public string[] armorProficiencies = Array.Empty<string>();
        public string[] classMechanics = Array.Empty<string>();
        public Affinities affinities = Affinities.None;
        public string[] sprite;

        // Resource scaling
        public int baseHp = 80;
        public float hpGrowth = 10f;       // multiplied by VITModifier
        public int levelHpGrowth = 8;      // per level above 1
        public int baseMp = 30;
        public float mpGrowth = 5f;        // multiplied by WISModifier
        public int levelMpGrowth = 4;      // per level above 1
        public int baseShield = 0;
        public float shieldGrowth = 2f;    // multiplied by max(0, VITModifier)

        // Offensive scaling
        public int baseWeaponPower = 10;
        public float weaponScaling = 2f;   // multiplied by STRModifier
        public int baseRangedPower = 10;
        public float rangedScaling = 2f;   // multiplied by DEXModifier
        public int baseSpellPower = 8;
        public float spellScaling = 2f;    // multiplied by INTModifier
        public int baseHealing = 8;
        public float healingScaling = 2f;  // multiplied by WISModifier
        public int baseSupportPower = 10;
        public float supportScaling = 2f;  // multiplied by CHAModifier
        public int baseCritChance = 5;
        public float critChanceScaling = 2f;
        public int baseCritDamage = 150;
        public int baseArmorPen = 0;
        public float armorPenScaling = 2f;
        public int baseMagicPen = 0;
        public float magicPenScaling = 2f;

        // Defensive scaling
        public int basePhysicalDefense = 5;
        public float physicalDefenseScaling = 2f; // multiplied by VITModifier
        public int baseMagicDefense = 5;
        public float magicDefenseScaling = 2f;    // multiplied by WISModifier
        public int baseEvasion = 5;
        public float evasionScaling = 2f;         // multiplied by DEXModifier
        public int baseBlockPower = 50;
        public float blockScaling = 3f;           // multiplied by STRModifier
        public int baseStatusResistance = 5;
        public float statusResistanceScaling = 3f; // multiplied by (VITModifier + WISModifier)
        public int baseStaggerResistance = 10;
        public float staggerResistanceScaling = 4f; // multiplied by VITModifier

        // Turn scaling
        public int baseInitiative = 8;            // + DEXModifier + bonuses
        public int baseActionSpeed = 100;
        public float actionSpeedScaling = 5f;

        // QTE scaling
        public int baseQteTimingBonus = 0;
        public float qteTimingScaling = 3f;       // +3% window per DEXModifier
        public int baseQteStability = 0;
        public float qteStabilityScaling = 4f;    // +4% stability per positive VITModifier
        public int baseCounterPower = 10;
        public float counterPowerScaling = 2f;    // multiplied by (STRModifier + DEXModifier)

        // Break scaling
        public int baseBreakPower = 12;
        public float breakPowerScaling = 3f;      // Physical -> STRModifier, Magic -> INTModifier
    }

    // Background definition: narrative identity, proficiencies, passive effects, roguelike event interactions, and social bonuses.
    // Background bonuses never modify Point Buy costs.
    [Serializable]
    public class BackgroundDef
    {
        public string id, name, narrativeIdentity, description;
        public string[] proficiencies = Array.Empty<string>();
        public StatModifier[] passiveEffects = Array.Empty<StatModifier>();
        public string[] eventInteractions = Array.Empty<string>();
        public int socialBonus;
    }

    // Interactive session implementing the full Character Creation flow:
    // Choose Race -> Choose Class -> Choose Background -> Allocate Point Buy -> Apply Racial Modifiers
    // -> Calculate Ability Modifiers -> Calculate Derived Stats -> Preview Character -> Confirm Character.
    public class CharacterCreationSession
    {
        public string CharacterName { get; set; }
        public RaceDef Race { get; private set; }
        public ClassDef Class { get; private set; }
        public BackgroundDef Background { get; private set; }
        public PointBuyAllocation PointBuy { get; }

        public event Action Changed;

        public CharacterCreationSession(PointBuyConfig config = null)
        {
            PointBuy = new PointBuyAllocation(config);
            PointBuy.Changed += () => Changed?.Invoke();
            Race = Database.Races.Values.First();
            Class = Database.Classes.Values.First();
            Background = Database.Backgrounds.Values.First();
            CharacterName = Class.name;
        }

        public void SelectRace(string raceId) => SelectRace(Database.Races[raceId]);
        public void SelectRace(RaceDef race)
        {
            Race = race ?? throw new ArgumentNullException(nameof(race));
            Changed?.Invoke();
        }

        // Selecting a Class does NOT alter Point Buy Ability Score allocations.
        public void SelectClass(string classId) => SelectClass(Database.Classes[classId]);
        public void SelectClass(ClassDef classDef)
        {
            bool defaultName = string.IsNullOrWhiteSpace(CharacterName) || (Class != null && CharacterName == Class.name);
            Class = classDef ?? throw new ArgumentNullException(nameof(classDef));
            if (defaultName) CharacterName = Class.name;
            Changed?.Invoke();
        }

        // Selecting a Background does NOT alter Point Buy costs or scores.
        public void SelectBackground(string backgroundId) => SelectBackground(Database.Backgrounds[backgroundId]);
        public void SelectBackground(BackgroundDef bg)
        {
            Background = bg ?? throw new ArgumentNullException(nameof(bg));
            Changed?.Invoke();
        }

        public bool IncreaseStat(Ability a) => PointBuy.TryIncrease(a);
        public bool DecreaseStat(Ability a) => PointBuy.TryDecrease(a);
        public void ResetPointBuy() => PointBuy.Reset();

        public AbilityScores BaseScores => PointBuy.Scores;
        public AbilityScores RacialModifiers => Race?.abilityModifiers ?? AbilityScores.Zero;
        public AbilityScores FinalScores => BaseScores.Plus(RacialModifiers);
        public AbilityScores AbilityModifiers => FinalScores.Modifiers;

        public AbilityScoreSheet BuildAbilitySheet() => new AbilityScoreSheet
        {
            baseScores = BaseScores,
            racialModifiers = RacialModifiers
        };

        public List<StatModifier> CollectCreationModifiers()
        {
            var mods = new List<StatModifier>();
            if (Race?.passiveBonuses != null) mods.AddRange(Race.passiveBonuses);
            if (Background?.passiveEffects != null) mods.AddRange(Background.passiveEffects);
            return mods;
        }

        // Real-time preview of Derived Stats based on current Race, Class, Background, and Point Buy allocation.
        public DerivedStats PreviewDerivedStats =>
            StatCalculator.Calculate(BuildAbilitySheet(), Class, 1, null, CollectCreationModifiers());

        public string Validate()
        {
            if (Race == null) return "Race must be selected.";
            if (Class == null) return "Class must be selected.";
            if (Background == null) return "Background must be selected.";
            if (string.IsNullOrWhiteSpace(CharacterName)) return "Character name is required.";
            return PointBuy.ValidationError;
        }

        public bool CanConfirm => Validate() == null;

        // Confirms character creation, optionally saving to the given Roster. Throws if allocation is invalid.
        public CharacterDef Confirm(CharacterRoster roster = null, string customId = null)
        {
            var err = Validate();
            if (err != null) throw new InvalidOperationException(err);

            var derived = PreviewDerivedStats;
            var skills = new List<string>(Class.startingSkills ?? Array.Empty<string>());
            if (Race.racialAbilities != null)
                foreach (var ra in Race.racialAbilities)
                    if (!skills.Contains(ra) && Database.Skills.ContainsKey(ra))
                        skills.Add(ra);

            string id = customId ?? $"{Class.id}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            var def = new CharacterDef
            {
                id = id,
                name = CharacterName.Trim(),
                role = Class.role,
                description = $"{Race.name} {Class.name} ({Background.name}). {Class.description}",
                raceId = Race.id,
                classId = Class.id,
                backgroundId = Background.id,
                baseScores = BaseScores,
                affinities = Class.affinities ?? Affinities.None,
                stats = derived.ToLegacyStats(),
                skills = skills.ToArray(),
                sprite = Class.sprite ?? Art.Knight
            };

            roster?.AddCharacter(def);
            return def;
        }
    }

    // Manages the roster of created/available characters and selection of the 4-member party for a run.
    public class CharacterRoster
    {
        public const int PartySize = 4;
        public readonly List<CharacterDef> Characters = new List<CharacterDef>();
        public readonly List<string> SelectedPartyIds = new List<string>();

        public CharacterRoster()
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            Characters.Clear();
            SelectedPartyIds.Clear();
            foreach (var id in Database.Party)
            {
                Characters.Add(Database.Characters[id]);
                SelectedPartyIds.Add(id);
            }
        }

        public void AddCharacter(CharacterDef def, bool selectInPartyIfRoom = false)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            int existing = Characters.FindIndex(c => c.id == def.id);
            if (existing >= 0) Characters[existing] = def;
            else Characters.Add(def);

            if (selectInPartyIfRoom && SelectedPartyIds.Count < PartySize && !SelectedPartyIds.Contains(def.id))
                SelectedPartyIds.Add(def.id);
        }

        // Replaces a party slot (0..3) with a character from the roster.
        public void SetPartySlot(int slot, string characterId)
        {
            if (slot < 0 || slot >= PartySize) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!Characters.Any(c => c.id == characterId)) throw new ArgumentException($"Character {characterId} is not in roster.");
            while (SelectedPartyIds.Count <= slot) SelectedPartyIds.Add(Database.Party[SelectedPartyIds.Count]);
            int otherIdx = SelectedPartyIds.IndexOf(characterId);
            if (otherIdx >= 0 && otherIdx != slot)
                SelectedPartyIds[otherIdx] = SelectedPartyIds[slot];
            SelectedPartyIds[slot] = characterId;
        }

        public List<CharacterDef> GetActiveParty()
        {
            if (SelectedPartyIds.Count != PartySize)
                return Database.Party.Select(id => Database.Characters[id]).ToList();
            return SelectedPartyIds.Select(id => Characters.FirstOrDefault(c => c.id == id) ?? Database.Characters[id]).ToList();
        }
    }
}

