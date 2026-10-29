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

        public static readonly Dictionary<string, EncounterDef> Encounters = Index(new[]
        {
            new EncounterDef { id = "skel2", tier = Tier.Normal, enemies = new[] { "skeleton", "skeleton" } },
            new EncounterDef { id = "skel3", tier = Tier.Normal, enemies = new[] { "skeleton", "skeleton", "skeleton" } },
            new EncounterDef { id = "elite1", tier = Tier.Elite, enemies = new[] { "cursed_knight" } },
            new EncounterDef { id = "elite2", tier = Tier.Elite, enemies = new[] { "skeleton", "cursed_knight" } },
            new EncounterDef { id = "boss", tier = Tier.Boss, enemies = new[] { "warden" } },
        }, e => e.id);

        static RewardDef Rest => new RewardDef { title = "Campfire", text = "Embers crackle against the dark. The party rests.", healPercent = 30 };

        // Hand-authored branching map, floor 0 (left) to boss (right).
        public static readonly MapDef Map = new MapDef
        {
            name = "The Sunken Crypt",
            nodes = new[]
            {
                new NodeDef { id = "a", type = NodeType.Battle, floor = 0, lane = 0, encounter = "skel2", next = new[] { "c", "d" } },
                new NodeDef { id = "b", type = NodeType.Battle, floor = 0, lane = 1, encounter = "skel2", next = new[] { "d", "e" } },
                new NodeDef { id = "c", type = NodeType.Event, floor = 1, lane = 0, next = new[] { "f", "g" }, reward = new RewardDef { title = "Whispering Shrine", text = "A ghost presses a coin into your palm and fades.", gold = 15 } },
                new NodeDef { id = "d", type = NodeType.Battle, floor = 1, lane = 1, encounter = "skel3", next = new[] { "g" } },
                new NodeDef { id = "e", type = NodeType.Treasure, floor = 1, lane = 2, next = new[] { "g", "h" }, reward = new RewardDef { title = "Ossuary Chest", text = "Beneath the bones, a cache of supplies.", gold = 25, items = new[] { "draught" } } },
                new NodeDef { id = "f", type = NodeType.Elite, floor = 2, lane = 0, encounter = "elite1", next = new[] { "i" } },
                new NodeDef { id = "g", type = NodeType.Battle, floor = 2, lane = 1, encounter = "skel3", next = new[] { "i", "j" } },
                new NodeDef { id = "h", type = NodeType.Rest, floor = 2, lane = 2, next = new[] { "j", "k" }, reward = Rest },
                new NodeDef { id = "i", type = NodeType.Battle, floor = 3, lane = 0, encounter = "skel3", next = new[] { "l" } },
                new NodeDef { id = "j", type = NodeType.Event, floor = 3, lane = 1, next = new[] { "l", "m" }, reward = new RewardDef { title = "Drowned Altar", text = "Black water recedes, revealing an offering.", gold = 10, items = new[] { "ether" } } },
                new NodeDef { id = "k", type = NodeType.Treasure, floor = 3, lane = 2, next = new[] { "m" }, reward = new RewardDef { title = "Reliquary", text = "A sealed reliquary yields to the knight's blade.", gold = 40, items = new[] { "elixir" } } },
                new NodeDef { id = "l", type = NodeType.Elite, floor = 4, lane = 0, encounter = "elite2", next = new[] { "n" } },
                new NodeDef { id = "m", type = NodeType.Rest, floor = 4, lane = 1, next = new[] { "n" }, reward = Rest },
                new NodeDef { id = "n", type = NodeType.Rest, floor = 5, lane = 0, next = new[] { "boss" }, reward = Rest },
                new NodeDef { id = "boss", type = NodeType.Boss, floor = 6, lane = 0, encounter = "boss" },
            },
        };

        static Dictionary<string, T> Index<T>(T[] items, System.Func<T, string> key)
        {
            var d = new Dictionary<string, T>();
            foreach (var i in items) d.Add(key(i), i);
            return d;
        }
    }
}
