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
        // Text for a status a skill/enemy move applies: the effect size where it is a plain stat % (Combatant: stat x (1 + up - down)),
        // then the apply chance only when it isn't certain. The old chips printed the chance alone, so a sure Defense Down
        // read "DEFENSE DOWN 100%" (a 100% defense cut) when it is really -25%.
        public static string ChipText(StatusApply ap)
        {
            float mag = ap.magnitude != 0f ? ap.magnitude : StatusEffectDefs.Get(ap.id).defaultMagnitude; // same fallback as applying
            string sign = ap.id switch
            {
                StatusId.AttackUp or StatusId.DefenseUp or StatusId.MagicDefenseUp => "+",
                StatusId.AttackDown or StatusId.DefenseDown or StatusId.MagicDefenseDown or StatusId.Slow or StatusId.Freeze => "-",
                _ => null,
            };
            var s = Name(ap.id).ToUpperInvariant();
            if (sign != null && mag > 0f) s += $" {sign}{System.Math.Round(mag * 100f)}%";
            if (ap.chance < 1f) s += $" · {System.Math.Round(ap.chance * 100f)}% CHANCE";
            return ap.turns > 0 ? $"{s} · {ap.turns}T" : s;
        }

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

        public static bool IsControl(StatusId id) =>
            id == StatusId.Stun ||
            id == StatusId.Freeze ||
            id == StatusId.Shock ||
            IsMental(id);

        public static string Description(StatusId id) => id switch
        {
            StatusId.DefenseUp => "Physical defense increased.",
            StatusId.DefenseDown => "Physical defense reduced.",
            StatusId.AttackUp => "Attack power increased.",
            StatusId.AttackDown => "Attack power reduced.",
            StatusId.Slow => "Initiative and turn speed reduced.",
            StatusId.Poison => "Takes poison damage at turn start.",
            StatusId.Guardian => "Intercepts attacks targeting protected ally.",
            StatusId.Evasion => "Increased chance to evade incoming strikes.",
            StatusId.MagicDefenseUp => "Magic defense increased.",
            StatusId.MagicDefenseDown => "Magic defense reduced.",
            StatusId.Broken => "Staggered — takes extra damage (+50% foes, +25% heroes) and skips turn.",
            StatusId.Exhausted => "Skills cost more MP and output is reduced.",
            StatusId.Burn => "Takes fire damage at turn start.",
            StatusId.Bleed => "Takes bleed damage over time.",
            StatusId.Freeze => "Speed reduced, delaying turn order.",
            StatusId.Shock => "Lightning disruption — risk of losing action.",
            StatusId.Curse => "Healing received reduced by 50% and takes 25% more damage.",
            StatusId.Stun => "Incapacitated — skips the next turn.",
            StatusId.ResistanceDown => "Elemental & status resistance reduced.",
            StatusId.MindControl => "Compelled to attack own allies.",
            StatusId.Confusion => "Actions target randomly.",
            StatusId.Fear => "Hesitates and deals reduced damage.",
            StatusId.Despair => "Cannot restore MP or gain buffs, and actions may fail.",
            StatusId.Charm => "Unable to attack the charmer.",
            StatusId.Sleep => "Asleep — skips turn until awakened.",
            StatusId.Silence => "Cannot cast magic skills.",
            StatusId.Madness => "Erratic high-damage strikes without control.",
            _ => "Active combat status effect.",
        };
    }
}
