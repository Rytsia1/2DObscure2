using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // =========================================================================
    // Difficulty System — Hardcore Tactical Survival
    // Primary Source of Truth: Assets/Docs/Difficulty_20System.md
    // =========================================================================
    public enum DifficultyDirection
    {
        HardcoreTacticalSurvival
    }

    public static class DifficultySystem
    {
        public const string DirectionName = "Hardcore Tactical Survival";
        public const int TotalDungeons = 8;
        public const int PartySize = 4;

        // Fixed sequential order of 8 dungeons (GDD Section 4.1)
        public static readonly string[] DungeonSequence = MapGenerator.DungeonNames;

        public static string GetDungeonName(int act)
        {
            if (act >= 1 && act <= DungeonSequence.Length)
                return DungeonSequence[act - 1];
            return $"Dungeon {act}";
        }

        public static bool IsLastDungeon(int act) => act >= TotalDungeons;

        public static int GetNextAct(int currentAct) => Math.Min(TotalDungeons, currentAct + 1);

        public static string FormatProgress(int act, int floor, int totalFloors = 8)
        {
            int safeAct = Math.Clamp(act, 1, TotalDungeons);
            return $"Dungeon {safeAct} of {TotalDungeons}: {GetDungeonName(safeAct)} (Floor {floor}/{totalFloors})";
        }

        // ---------------------------------------------------------------------
        // Party & Combat Survival Rules (GDD Section 3.1 & 3.2)
        // ---------------------------------------------------------------------

        public static bool CanBattleContinue(IReadOnlyList<Combatant> party)
        {
            if (party == null || party.Count == 0) return false;
            return party.Any(c => c.Alive);
        }

        public static bool CanBattleContinue(IReadOnlyList<Unit> party)
        {
            if (party == null || party.Count == 0) return false;
            return party.Any(u => u.Alive);
        }

        public static bool IsPartyWiped(IReadOnlyList<Combatant> party)
        {
            if (party == null || party.Count == 0) return true;
            return party.All(c => !c.Alive);
        }

        public static bool IsPartyWiped(IReadOnlyList<Unit> party)
        {
            if (party == null || party.Count == 0) return true;
            return party.All(u => !u.Alive);
        }

        public static int LivingCount(IReadOnlyList<Unit> party) => party != null ? party.Count(u => u.Alive) : 0;
        public static int FallenCount(IReadOnlyList<Unit> party) => party != null ? party.Count(u => !u.Alive) : 0;
        public static bool IsInPeril(IReadOnlyList<Unit> party) => LivingCount(party) == 1;

        // ---------------------------------------------------------------------
        // Dungeon Progression & Clear Evaluation (GDD Section 4.1 & 4.2)
        // ---------------------------------------------------------------------

        public static bool IsExpeditionComplete(int act, bool bossDefeated)
        {
            return act >= TotalDungeons && bossDefeated;
        }

        public static bool ValidateProgression(int currentAct, int nextAct, out string error)
        {
            if (currentAct < 1 || currentAct > TotalDungeons)
            {
                error = $"Invalid current dungeon act {currentAct}.";
                return false;
            }
            if (nextAct != currentAct + 1)
            {
                error = $"Dungeon progression is strictly sequential ({currentAct} -> {currentAct + 1}). Skipping or free selection is forbidden.";
                return false;
            }
            if (nextAct > TotalDungeons)
            {
                error = $"Cannot advance beyond the final dungeon ({TotalDungeons}).";
                return false;
            }
            error = null;
            return true;
        }

        // ---------------------------------------------------------------------
        // Run Failure Handling (GDD Section 4.3 & Core Requirements E)
        // ---------------------------------------------------------------------

        public static void HandleRunFailure(RunState run)
        {
            if (run == null) return;
            if (run.Status != RunStatus.Active) return; // Idempotent safeguard against repeated calls

            // Delegate to RunState's LoseCombat to mutate status, wipe gold, reset progression, and persist
            run.LoseCombat();
        }
    }
}

