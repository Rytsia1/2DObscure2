namespace Obscure
{
    // Magnitude meaning: Up/Down/Slow = fraction of the stat; Poison = HP lost per turn;
    // Evasion = defense-QTE window scale; Guardian = bearer takes single-target hits aimed at the linked ally (source);
    // Broken = enemy stagger/break state (vulnerability multiplier).
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
        Broken
    }

    // What a skill or enemy move applies. Duration counts the bearer's turns: it ticks down at the
    // start of each of the bearer's turns and fades at 0. Statuses live on the per-battle Combatant,
    // so nothing survives the end of combat.
    public class StatusApply
    {
        public StatusId id;
        public int turns = 2;
        public float magnitude;
        public float chance = 1f;  // a Perfect QTE on the action guarantees it
        public bool onSelf;        // apply to the user rather than the target
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
            _ => "Evasion",
        };

        public static bool IsBuff(StatusId id) =>
            id == StatusId.DefenseUp ||
            id == StatusId.AttackUp ||
            id == StatusId.Guardian ||
            id == StatusId.Evasion ||
            id == StatusId.MagicDefenseUp;
    }
}
