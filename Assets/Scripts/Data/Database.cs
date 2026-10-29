using System.Collections.Generic;

// All vertical-slice content in one place. Swap for ScriptableObjects if designers need to edit in the Inspector.
namespace Obscure
{
    public static class Database
    {
        static StatusApply Status(StatusId id, int turns, float magnitude, float chance = 1f, bool onSelf = false) =>
            new StatusApply { id = id, turns = turns, magnitude = magnitude, chance = chance, onSelf = onSelf };

        // Physical skills use the timing QTE; Magic/Heal skills with a word use the typing QTE; Support skills need none.
        public static readonly Dictionary<string, SkillDef> Skills = Index(new[]
        {
            // Knight
            new SkillDef { id = "shield_bash", name = "Shield Bash", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 1.0f,
                apply = new[] { Status(StatusId.DefenseDown, 2, 0.3f) }, description = "Bash a foe and crack its guard (DEF-)." },
            new SkillDef { id = "fortify", name = "Fortify", kind = SkillKind.Support, target = TargetType.Self, mpCost = 5, cooldown = 3,
                apply = new[] { Status(StatusId.DefenseUp, 2, 0.6f) }, description = "Brace behind the shield (DEF+ 2 turns)." },
            new SkillDef { id = "interpose", name = "Interpose", kind = SkillKind.Support, target = TargetType.Ally, mpCost = 4, cooldown = 2,
                apply = new[] { Status(StatusId.Guardian, 1, 1f, onSelf: true) }, description = "Take an ally's single-target hits until next turn." },
            // Rogue
            new SkillDef { id = "quick_strike", name = "Quick Strike", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 3, power = 0.9f, perfectPower = 1.75f,
                description = "Perfect timing lands a critical." },
            new SkillDef { id = "expose", name = "Expose", kind = SkillKind.Physical, target = TargetType.Enemy, mpCost = 4, power = 0.5f,
                apply = new[] { Status(StatusId.DefenseDown, 2, 0.4f) }, description = "Find the gap in its armor (DEF-)." },
            new SkillDef { id = "evasion", name = "Evasion", kind = SkillKind.Support, target = TargetType.Self, mpCost = 3, cooldown = 2,
                apply = new[] { Status(StatusId.Evasion, 2, 1.75f) }, description = "Wider guard windows for 2 turns." },
            // Mage
            new SkillDef { id = "firebolt", name = "Firebolt", kind = SkillKind.Magic, element = Element.Fire, target = TargetType.Enemy, mpCost = 6, power = 1.6f,
                word = "IGNIS", description = "A bolt of fire." },
            new SkillDef { id = "frost_shard", name = "Frost Shard", kind = SkillKind.Magic, element = Element.Ice, target = TargetType.Enemy, mpCost = 7, power = 1.3f,
                word = "FROST", apply = new[] { Status(StatusId.Slow, 2, 0.4f, 0.6f) }, description = "Ice that may slow. Perfect: sure." },
            new SkillDef { id = "arcane_burst", name = "Arcane Burst", kind = SkillKind.Magic, element = Element.Arcane, target = TargetType.AllEnemies, mpCost = 14, power = 1.3f,
                word = "AETHERIS", difficulty = SkillDifficulty.Hard, cooldown = 2, description = "Raw arcana on all foes." },
            // Healer
            new SkillDef { id = "heal", name = "Heal", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 5, power = 1.5f,
                word = "SANA", difficulty = SkillDifficulty.Easy, description = "Mend an ally's wounds." },
            new SkillDef { id = "mend", name = "Mend", kind = SkillKind.Heal, element = Element.Holy, target = TargetType.Ally, mpCost = 11, power = 2.6f,
                word = "VITALIS", description = "A deep restoration." },
            new SkillDef { id = "blessing", name = "Blessing", kind = SkillKind.Support, element = Element.Holy, target = TargetType.AllAllies, mpCost = 9, cooldown = 3,
                apply = new[] { Status(StatusId.DefenseUp, 2, 0.3f) }, description = "Ward the party (DEF+ 2 turns)." },
        }, s => s.id);

        public static readonly Dictionary<string, ItemDef> Items = Index(new[]
        {
            new ItemDef { id = "draught", name = "Draught", heal = 40, description = "Restores 40 HP." },
            new ItemDef { id = "ether", name = "Ether", mana = 20, description = "Restores 20 MP." },
            new ItemDef { id = "elixir", name = "Grave Elixir", heal = 80, mana = 30, description = "Restores 80 HP and 30 MP." },
        }, i => i.id);

        public static readonly Dictionary<string, CharacterDef> Characters = Index(new[]
        {
            new CharacterDef { id = "knight", name = "Knight", role = "Defender", stats = new Stats(125, 20, 14, 13, 6, 4),
                skills = new[] { "shield_bash", "fortify", "interpose" }, sprite = Art.Knight,
                affinities = new Affinities { resist = new[] { Element.Dark } },
                description = "Oathbound shield. Outlasts anything and stands between the dark and the weak." },
            new CharacterDef { id = "rogue", name = "Rogue", role = "Striker", stats = new Stats(85, 25, 17, 7, 14, 5),
                skills = new[] { "quick_strike", "expose", "evasion" }, sprite = Art.Rogue,
                description = "Acts first and often. Precise timing turns strikes into criticals." },
            new CharacterDef { id = "mage", name = "Mage", role = "Caster", stats = new Stats(60, 60, 7, 4, 9, 20),
                skills = new[] { "firebolt", "frost_shard", "arcane_burst" }, sprite = Art.Mage,
                affinities = new Affinities { weak = new[] { Element.Dark }, resist = new[] { Element.Arcane } },
                description = "Fragile, but every foe has an element it fears." },
            new CharacterDef { id = "healer", name = "Healer", role = "Support", stats = new Stats(80, 55, 7, 6, 10, 14),
                skills = new[] { "heal", "mend", "blessing" }, sprite = Art.Healer,
                affinities = new Affinities { resist = new[] { Element.Dark } },
                description = "Keeps the party standing through long fights." },
        }, c => c.id);

        public static readonly string[] Party = { "knight", "rogue", "mage", "healer" };
        public static readonly string[] StartingItems = { "draught", "draught", "ether" };

        public static readonly Dictionary<string, EnemyDef> Enemies = Index(new[]
        {
            new EnemyDef { id = "skeleton", name = "Skeleton Warrior", shortName = "Skeleton", tag = "SK", tier = Tier.Normal, ai = AiKind.Basic,
                stats = new Stats(40, 0, 9, 3, 8, 0), gold = 8, sprite = Art.Skeleton,
                affinities = new Affinities { weak = new[] { Element.Fire, Element.Holy }, resist = new[] { Element.Ice } },
                attacks = new[] { new EnemyAttack { name = "Rusty Slash" } },
                description = "Rattling bones bound to a rusted blade." },
            new EnemyDef { id = "cursed_knight", name = "Cursed Knight", shortName = "Cursed Knight", tag = "CK", tier = Tier.Elite, ai = AiKind.Tactical,
                stats = new Stats(120, 0, 16, 9, 6, 6), gold = 30, sprite = Art.CursedKnight,
                affinities = new Affinities { weak = new[] { Element.Ice }, resist = new[] { Element.Physical, Element.Fire }, immune = new[] { Element.Dark } },
                attacks = new[]
                {
                    new EnemyAttack { name = "Heavy Cleave", power = 1.3f },
                    new EnemyAttack { name = "Twin Strike", hits = 2, power = 0.7f },
                    new EnemyAttack { name = "Delayed Thrust", power = 1.1f, delay = 0.6f },
                    new EnemyAttack { name = "Iron Vow", self = true, apply = new[] { Status(StatusId.DefenseUp, 2, 0.5f) } },
                    new EnemyAttack { name = "Dread Howl", power = 0.4f, element = Element.Dark, allTargets = true, apply = new[] { Status(StatusId.AttackDown, 2, 0.25f) } },
                },
                description = "A fallen oathkeeper. Braces when hurt and preys on the weakest." },
            new EnemyDef { id = "warden", name = "Abyssal Warden", shortName = "Warden", tag = "WDN", tier = Tier.Boss, ai = AiKind.Boss,
                stats = new Stats(340, 0, 20, 10, 9, 12), gold = 100, sprite = Art.Warden,
                affinities = new Affinities { weak = new[] { Element.Arcane }, resist = new[] { Element.Fire, Element.Ice }, immune = new[] { Element.Dark } },
                attacks = new[]
                {
                    new EnemyAttack { name = "Chain Lash", hits = 3, power = 0.5f, phase = 1 },
                    new EnemyAttack { name = "Crushing Grip", power = 1.5f, delay = 0.8f },
                    new EnemyAttack { name = "Abyssal Gaze", power = 0.7f, element = Element.Dark, allTargets = true, delay = 0.5f, phase = 2 },
                    new EnemyAttack { name = "Soul Rend", hits = 2, power = 0.8f, element = Element.Dark, phase = 2, apply = new[] { Status(StatusId.Poison, 3, 6f) } },
                },
                phase2At = 0.5f, phase2Text = "The Warden's eye burns red! (Phase 2)",
                phase2Buff = new StatusApply { id = StatusId.AttackUp, turns = 99, magnitude = 0.3f },
                description = "Jailer of the deep. Enrages at half health." },
        }, e => e.id);

        // Encounter pools: map generation rolls from these by tier and floor.
        public static readonly Dictionary<string, EncounterDef> Encounters = Index(new[]
        {
            new EncounterDef { id = "skel2", tier = Tier.Normal, enemies = new[] { "skeleton", "skeleton" } },
            new EncounterDef { id = "skel3", tier = Tier.Normal, enemies = new[] { "skeleton", "skeleton", "skeleton" }, minFloor = 2 },
            new EncounterDef { id = "elite1", tier = Tier.Elite, enemies = new[] { "cursed_knight" } },
            new EncounterDef { id = "elite2", tier = Tier.Elite, enemies = new[] { "skeleton", "cursed_knight" }, minFloor = 4 },
            new EncounterDef { id = "boss", tier = Tier.Boss, enemies = new[] { "warden" } },
        }, e => e.id);

        public static readonly Dictionary<string, RelicDef> Relics = Index(new[]
        {
            new RelicDef { id = "iron_locket", name = "Iron Locket", bonus = new Stats(12, 0, 0, 0, 0, 0), description = "+12 max HP to the party." },
            new RelicDef { id = "whetstone", name = "Whetstone", bonus = new Stats(0, 0, 2, 0, 0, 0), description = "+2 ATK to the party." },
            new RelicDef { id = "moon_sigil", name = "Moon Sigil", bonus = new Stats(0, 6, 0, 0, 0, 3), description = "+3 MAG and +6 max MP to the party." },
            new RelicDef { id = "warding_charm", name = "Warding Charm", bonus = new Stats(0, 0, 0, 2, 0, 0), description = "+2 DEF to the party." },
            new RelicDef { id = "ashen_boots", name = "Ashen Boots", bonus = new Stats(0, 0, 0, 0, 1, 0), description = "+1 SPD to the party." },
            new RelicDef { id = "ember_heart", name = "Ember Heart", victoryHeal = 0.08f, description = "Heal 8% max HP after each victory." },
            new RelicDef { id = "warden_eye", name = "Warden's Eye", bonus = new Stats(10, 5, 1, 1, 0, 1), description = "Trophy of the Abyssal Warden." },
        }, r => r.id);

        public const string BossRelic = "warden_eye"; // never rolled from pools

        public static readonly Dictionary<string, UpgradeDef> Upgrades = Index(new[]
        {
            new UpgradeDef { character = "knight", name = "Bulwark Drill", bonus = new Stats(15, 0, 0, 2, 0, 0) },
            new UpgradeDef { character = "rogue", name = "Honed Blades", bonus = new Stats(0, 0, 3, 0, 1, 0) },
            new UpgradeDef { character = "mage", name = "Deeper Focus", bonus = new Stats(0, 10, 0, 0, 0, 3) },
            new UpgradeDef { character = "healer", name = "Litany of Light", bonus = new Stats(10, 8, 0, 0, 0, 2) },
        }, u => u.character);

        // The last choice of every event is always available, so an event can never trap the player.
        public static readonly Dictionary<string, EventDef> Events = Index(new[]
        {
            new EventDef
            {
                id = "shrine", title = "Whispering Shrine", text = "A cracked shrine hums. Candles relight as you approach.",
                choices = new[]
                {
                    new EventChoice { label = "Offer 30 gold", goldCost = 30, relic = true, outcome = "The coins vanish. Something is left in their place." },
                    new EventChoice { label = "Anoint weapons", battleBuff = new StatusApply { id = StatusId.AttackUp, turns = 3, magnitude = 0.3f }, outcome = "Your blades drink the candlelight." },
                    new EventChoice { label = "Pray", healPercent = 25, outcome = "Warmth settles over the party." },
                },
            },
            new EventDef
            {
                id = "merchant", title = "Hooded Peddler", text = "A figure with too many pockets offers a trade.",
                choices = new[]
                {
                    new EventChoice { label = "Buy a Draught (15g)", goldCost = 15, item = "draught", outcome = "\"Pleasure doing business.\"" },
                    new EventChoice { label = "Buy a relic (45g)", goldCost = 45, relic = true, outcome = "The peddler grins and vanishes." },
                    new EventChoice { label = "Leave", outcome = "You keep your coin and your distance." },
                },
            },
            new EventDef
            {
                id = "altar", title = "Drowned Altar", text = "Black water fills a stone basin. It whispers of power.",
                choices = new[]
                {
                    new EventChoice { label = "Drink", hurtPercent = 20, relic = true, outcome = "It burns going down. Something stays with you." },
                    new EventChoice { label = "Refuse", outcome = "You walk away. The whispers fade." },
                },
            },
            new EventDef
            {
                id = "fallen", title = "Fallen Adventurer", text = "A skeleton in rotted leathers still clutches its pack.",
                choices = new[]
                {
                    new EventChoice { label = "Take the coin purse", gold = 25, outcome = "25 gold. They won't need it." },
                    new EventChoice { label = "Take the vial", item = "ether", outcome = "An Ether, still sealed." },
                },
            },
        }, e => e.id);

        static Dictionary<string, T> Index<T>(T[] items, System.Func<T, string> key)
        {
            var d = new Dictionary<string, T>();
            foreach (var i in items) d.Add(key(i), i);
            return d;
        }
    }
}
