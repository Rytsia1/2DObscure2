using System;
using System.Linq;

// Static gameplay definitions. Runtime state lives in Run/, never here.
namespace Obscure
{
    public enum Tier { Normal, Elite, Boss }
    public enum NodeType { Battle, Elite, Event, Treasure, Rest, Boss }
    public enum SkillKind { Physical, Magic, Heal, Support }
    public enum TargetType { Enemy, AllEnemies, Ally, AllAllies, Self }

    [Serializable]
    public struct Stats
    {
        public int maxHp, maxMp, atk, def, spd, mag;
        public Stats(int maxHp, int maxMp, int atk, int def, int spd, int mag)
        {
            this.maxHp = maxHp; this.maxMp = maxMp; this.atk = atk; this.def = def; this.spd = spd; this.mag = mag;
        }
    }

    public enum AiKind { Basic, Tactical, Boss }

    // Resource is MP for every character for now; skills spend it, items restore it.
    public class CharacterDef
    {
        public string id, name, role, description;
        public Affinities affinities = Affinities.None;
        public Stats stats;
        public string[] skills;
        public string[] sprite;
    }

    public class EnemyDef
    {
        public string id, name, description;
        public string shortName, tag; // combat log name, 2-3 letter turn-order tag
        public Tier tier;             // also selects the defense QTE profile
        public AiKind ai;
        public Affinities affinities = Affinities.None;
        public Stats stats;
        public int gold;
        public string[] sprite;
        public EnemyAttack[] attacks;
        public float phase2At;         // boss: HP fraction that triggers phase 2 (0 = no phases)
        public string phase2Text;
        public StatusApply phase2Buff;
    }

    // One enemy move. Each hit gets its own defense reaction; allTargets hits every living party member.
    public class EnemyAttack
    {
        public string name;
        public int hits = 1;
        public float power = 1f;      // per hit, multiplies Atk
        public Element element = Element.Physical;
        public bool allTargets;
        public bool self;             // a stance/buff on the user: no damage, no reaction QTE
        public float delay;           // extra hold before the first hit lands (a delayed swing)
        public int phase;             // 0 = any phase, otherwise only in that boss phase
        public StatusApply[] apply;   // applied to each target not perfectly parried (or to self if self)
    }

    public class SkillDef
    {
        public string id, name, description;
        public SkillKind kind;
        public TargetType target;
        public int mpCost;
        public float power;
        public int hits = 1;
        public Element element = Element.Physical;
        public string word; // typing QTE word for spells, null for physical skills
        public SkillDifficulty difficulty = SkillDifficulty.Medium; // typing QTE difficulty
        public int cooldown;      // own turns before reuse (0 = none)
        public float perfectPower; // overrides the Perfect QTE multiplier (critical hits), 0 = default
        public StatusApply[] apply;
    }

    public class ItemDef
    {
        public string id, name, description;
        public int heal, mana;
    }

    public class EncounterDef
    {
        public string id;
        public Tier tier;
        public string[] enemies;
    }

    public class RewardDef
    {
        public string title, text;
        public int gold, healPercent;
        public string[] items = Array.Empty<string>();
    }

    public class NodeDef
    {
        public string id;
        public NodeType type;
        public int floor, lane;
        public string[] next = Array.Empty<string>();
        public string encounter;   // combat nodes
        public RewardDef reward;   // non-combat nodes
        public bool IsCombat => type == NodeType.Battle || type == NodeType.Elite || type == NodeType.Boss;
    }

    public class MapDef
    {
        public string name;
        public NodeDef[] nodes;
        public NodeDef Get(string id) => nodes.First(n => n.id == id);
        public int Floors => nodes.Max(n => n.floor) + 1;
    }
}
