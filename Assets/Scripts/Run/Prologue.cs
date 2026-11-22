using System;
using System.Collections.Generic;
using System.Linq;

// Tutorial Prologue (Assets/Docs/Tutorial_Prologue_Design.md, accepted 2026-11-21).
// Three linear nodes after Character Creation, then the Hub. Every number here is a [B] starting value (design §9).
namespace Obscure
{
    public static class Prologue
    {
        // Fixed rewards (design §4.3): never RunConfig.ExpFor / BattleGold, which would push the party to Level 7.
        public static readonly int[] NodeExp = { 40, 60, 0 };                     // per standing hero; 100 total = Level 2
        public static readonly string[] Node2Items = { "draught", "draught", "ether" }; // stocks the party for the boss
        public const int ClearGold = 40;                                         // Hub Gold on Prologue clear
        public const float FirstTimeSlow = 1.5f;                                 // first sight of each QTE type (Assist speed x1.5)
        public const int AssistOfferAfterWipes = 2;
        const int NodeSeed = 1631;                                               // fixed combat RNG per node (design §4.4)

        public static int TotalExp => NodeExp.Sum();
        public static Random NodeRng(int floor) => new Random(NodeSeed + floor);

        public static MapDef Map() => new MapDef
        {
            name = "The Threshold", act = 1, stage = 1, totalStages = 1, stageName = "Prologue", allowEliteNodes = false, isPrologue = true,
            nodes = new[]
            {
                new NodeDef { id = "p0", type = NodeType.Battle, floor = 0, next = new[] { "p1" }, encounter = "tut_n1" },
                new NodeDef { id = "p1", type = NodeType.Battle, floor = 1, next = new[] { "p2" }, encounter = "tut_n2" },
                new NodeDef { id = "p2", type = NodeType.Boss, floor = 2, encounter = "tut_boss" },
            },
        };
    }

    // Per-run tutorial state: which QTE types the player has met, and the scripted beats (design §3, §4.4).
    public class PrologueScript
    {
        readonly HashSet<string> seen = new HashSet<string>();
        public int Floor;                 // 0..2, set on node entry
        public int Wipes;                 // wipes on the current node
        public bool FearScripted, FeintScripted, CollapseSeen;

        // True the first time a QTE type appears (and marks it seen): that one runs slower.
        public bool FirstTime(string kind) => seen.Add(kind);
        public bool Seen(string kind) => seen.Contains(kind);

        public static string DefenseKind(EnemyAttack move, int hits)
        {
            if (move == null) return hits > 1 ? "chain" : "arrow";
            if (move.drain > 0f) return "struggle";
            if (move.UsesMagic) return move.allTargets ? "shared_ward" : "ward";
            if (move.HasHeavy) return move.hits > 1 ? "collapse" : "brace";
            return hits > 1 ? "chain" : "arrow";
        }

        // Feint / stutter ramp (design §4.4): none in Node 1, one scripted feint in Node 2, Normal rates in boss
        // phase 1, full Boss rates in phase 2, and never on the first Threshold Collapse.
        public (float feint, float stutter, bool forceFeint) Ramp(Combatant actor, EnemyAttack move)
        {
            if (Floor <= 0) return (0f, 0f, false);
            if (Floor == 1)
            {
                bool force = !FeintScripted && Seen("arrow") && move != null && !move.UsesMagic && move.hits == 1;
                if (force) FeintScripted = true;
                return (0f, 0f, force);
            }
            if (move != null && move.telegraph && !CollapseSeen)
            {
                CollapseSeen = true;
                return (0f, 0f, false);
            }
            var tier = actor?.phase >= 2 ? Tier.Boss : Tier.Normal;
            return (QteConfig.FeintChance(tier), QteConfig.StutterChance(tier), false);
        }

        // The first Dread Pulse always lands (the sheet's 20% would hide the status lesson from 4 of 5 players).
        public bool ForcesStatus(Combatant source, StatusApply ap)
        {
            if (FearScripted || ap.id != StatusId.Fear || source?.enemy?.id != "veil_stalker") return false;
            FearScripted = true;
            return true;
        }
    }

    // A roll that always succeeds: RollApplication compares against chance x (1 - resistance), and resistance caps at 75%.
    sealed class SureRandom : Random
    {
        protected override double Sample() => 0.0;
    }
}
