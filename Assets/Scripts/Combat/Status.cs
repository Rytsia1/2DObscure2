namespace Obscure
{
    // Magnitude meaning: Up/Down/Slow/Freeze = fraction of the stat; Poison/Burn/Bleed = DoT magnitude/power;
    // Evasion = defense-QTE window scale; Guardian = bearer takes single-target hits aimed at the linked ally (source);
    // Broken = enemy stagger/break state (vulnerability multiplier).
    // GDD Section 11 & 12: Burn, Poison, Bleed, Freeze, Shock, Curse, Stun, MindControl, Confusion, Fear, Despair, Charm, Sleep, Silence, Madness, ResistanceDown.
    public enum StatusId
    {
        DefenseUp,
        DefenseDown,
        AttackUp,
        AttackDown,
        Slow,
        Poison,
        Guardian,
        Evasion,
        MagicDefenseUp,
        MagicDefenseDown,
        Broken,
        Exhausted,
        // Non-Mental Status Effects (GDD Section 11)
        Burn,
        Bleed,
        Freeze,
        Shock,
        Curse,
        Stun,
        ResistanceDown,
        // Mental Effects (GDD Section 12)
        MindControl,
        Confusion,
        Fear,
        Despair,
        Charm,
        Sleep,
        Silence,
        Madness
    }

    // What a skill or enemy move applies. Duration counts the bearer's turns: it ticks down at the
    // start of each of the bearer's turns and fades at 0.
    public class StatusApply
    {
        public StatusId id;
        public int turns = 2;
        public float magnitude;
        public float chance = 1f;  // a Perfect QTE on the action guarantees it
        public bool onSelf;        // apply to the user rather than the target

        // Optional DoT parameters (GDD Section 10 & 11)
        public DamageType? dotDamageType;
        public ElementType? dotElementType;
        public float? dotSkillPower;
        public float? dotScaling;
        public Ability? dotScalingStat;
    }

    public class StatusEffect
    {
        public StatusId id;
        public int turns;
        public float magnitude;
        public Combatant source;   // who applied it; for Guardian, the ally being protected
    }

    public static class StatusInfo
    {
        public static string Tag(StatusId id) => id switch
        {
            StatusId.DefenseUp => "D+",
            StatusId.DefenseDown => "D-",
            StatusId.AttackUp => "A+",
            StatusId.AttackDown => "A-",
            StatusId.Slow => "SL",
            StatusId.Poison => "PS",
            StatusId.Guardian => "GU",
            StatusId.Evasion => "EV",
            StatusId.MagicDefenseUp => "M+",
            StatusId.MagicDefenseDown => "M-",
            StatusId.Broken => "BK",
            StatusId.Exhausted => "EX",
            StatusId.Burn => "BN",
            StatusId.Bleed => "BL",
            StatusId.Freeze => "FZ",
            StatusId.Shock => "SK",
            StatusId.Curse => "CU",
            StatusId.Stun => "ST",
            StatusId.ResistanceDown => "RD",
            StatusId.MindControl => "MC",
            StatusId.Confusion => "CF",
            StatusId.Fear => "FR",
            StatusId.Despair => "DP",
            StatusId.Charm => "CH",
            StatusId.Sleep => "SLP",
            StatusId.Silence => "SIL",
            StatusId.Madness => "MD",
            _ => "EV",
        };

        public static string Name(StatusId id) => id switch
        {
            StatusId.DefenseUp => "Defense Up",
            StatusId.DefenseDown => "Defense Down",
            StatusId.AttackUp => "Attack Up",
            StatusId.AttackDown => "Attack Down",
            StatusId.Slow => "Slow",
            StatusId.Poison => "Poison",
            StatusId.Guardian => "Guardian",
            StatusId.Evasion => "Evasion",
            StatusId.MagicDefenseUp => "Magic Defense Up",
            StatusId.MagicDefenseDown => "Magic Defense Down",
            StatusId.Broken => "Broken",
            StatusId.Exhausted => "Exhausted",
            StatusId.Burn => "Burn",
            StatusId.Bleed => "Bleed",
            StatusId.Freeze => "Freeze",
            StatusId.Shock => "Shock",
            StatusId.Curse => "Curse",
            StatusId.Stun => "Stun",
            StatusId.ResistanceDown => "Resistance Down",
            StatusId.MindControl => "Mind Control",
            StatusId.Confusion => "Confusion",
            StatusId.Fear => "Fear",
            StatusId.Despair => "Despair",
            StatusId.Charm => "Charm",
            StatusId.Sleep => "Sleep",
            StatusId.Silence => "Silence",
            StatusId.Madness => "Madness",
            _ => "Evasion",
        };

        public static bool IsBuff(StatusId id) =>
            id == StatusId.DefenseUp ||
            id == StatusId.AttackUp ||
            id == StatusId.Guardian ||
            id == StatusId.Evasion ||
            id == StatusId.MagicDefenseUp;

        public static bool IsDebuff(StatusId id) => !IsBuff(id);

        // GDD Section 12: Mental Effect System
        public static bool IsMental(StatusId id) =>
            id == StatusId.MindControl ||
            id == StatusId.Confusion ||
            id == StatusId.Fear ||
            id == StatusId.Despair ||
            id == StatusId.Charm ||
            id == StatusId.Sleep ||
            id == StatusId.Silence ||
            id == StatusId.Madness;

        // GDD Section 10 & 11: Damage Over Time
        public static bool IsDoT(StatusId id) =>
            id == StatusId.Burn ||
            id == StatusId.Poison ||
            id == StatusId.Bleed;
    }
}
