using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// All background music in one asset (Resources/MusicLibrary). Gameplay asks for music by id, never by clip.
namespace Obscure
{
    public enum MusicCategory { Menu, Battle, Exploration, Boss, Elite }

    public static class MusicIds
    {
        public const string MainMenu = "main_menu", Map = "map", Battle1 = "battle_1", Battle2 = "battle_2", Battle3 = "battle_3";
        public const string Hub = "hub";
        public const string ZoroaP1 = "zoroa_p1", ZoroaP2 = "zoroa_p2";
        public const string Aetherion = "aetherion";
        public const string RequiemTheHarvester = ZoroaP1;
        public const string TheSoulReaping = ZoroaP2;
        public const string ThroneOfTheForsaken = Aetherion;
        public const string AHavenAtTheWorldsEnd = Hub;
        public const string IroncladOath = "ironclad_oath";
        public const string TheBellAtMidnight = "the_bell_at_midnight";
        public const string TheFallenSovereignsRite = "the_fallen_sovereigns_rite";
        public const string TombOfTheUnbroken = "tomb_of_the_unbroken";
    }

    [Serializable]
    public class MusicTrack
    {
        public string id;
        public AudioClip clip;
        public MusicCategory category;
        public bool loop = true;
        [Range(0, 1)] public float volume = 1f;
        public float fadeIn = -1f, fadeOut = -1f; // seconds; negative uses the library defaults
        [Range(1000, 22000)] public float lowPassCutoff = 22000f; // Hz; 22000 = transparent / bypass
    }

    [CreateAssetMenu(menuName = "Obscure/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        public const string ResourcePath = "MusicLibrary";

        public MusicTrack[] tracks = Array.Empty<MusicTrack>();

        [Header("Selection (empty id = silence)")]
        public string menuMusic = MusicIds.MainMenu;
        public string hubMusic = MusicIds.Hub;
        public string creationMusic = MusicIds.MainMenu; // character creation uses main menu theme
        public string explorationMusic = MusicIds.Map; // map, rewards, rest, events, run summary
        public string defaultBattleMusic = MusicIds.Battle1;
        public bool randomBattleMusic;           // encounters without their own theme pick a random battle track

        [Header("Transitions (seconds)")]
        public float fadeOut = 0.6f;
        public float fadeIn = 0.8f;
        public float transitionGap = 0.1f;       // silence between the old track ending and the new one starting

        public MusicTrack Get(string id) => string.IsNullOrEmpty(id) ? null : tracks.FirstOrDefault(t => t.id == id && t.clip != null);

        public float FadeIn(MusicTrack t) => t.fadeIn >= 0 ? t.fadeIn : fadeIn;
        public float FadeOut(MusicTrack t) => t.fadeOut >= 0 ? t.fadeOut : fadeOut;
        public float Cutoff(MusicTrack t) => t != null && t.lowPassCutoff >= 1000f ? t.lowPassCutoff : 22000f;

        public static bool IsEliteOrMinibossBattle(EncounterDef encounter, IEnumerable<string> enemyIds = null)
        {
            if (encounter != null && encounter.tier == Tier.Boss) return false;
            if (encounter != null && encounter.tier == Tier.Elite) return true;

            var ids = enemyIds ?? encounter?.enemies;
            if (ids == null) return false;
            foreach (var enemyId in ids)
            {
                if (string.IsNullOrEmpty(enemyId) || !Database.Enemies.TryGetValue(enemyId, out var def)) continue;
                if (def.tier == Tier.Elite || def.aiTier == AiTier.Elite || def.aiTier == AiTier.Miniboss)
                    return true;
            }
            return false;
        }

        // The encounter's own theme if it has one, else a random Elite/Miniboss track when fighting an Elite or Miniboss,
        // else a random battle theme (when enabled), else the default.
        public string BattleMusicFor(EncounterDef encounter, System.Random rng) =>
            BattleMusicFor(encounter, encounter?.enemies, rng);

        public string BattleMusicFor(EncounterDef encounter, IEnumerable<string> enemyIds, System.Random rng)
        {
            if (Get(encounter?.music) != null) return encounter.music;
            if (IsEliteOrMinibossBattle(encounter, enemyIds))
            {
                var elite = tracks.Where(t => t.category == MusicCategory.Elite && t.clip != null).ToArray();
                if (elite.Length > 0) return elite[(rng ?? new System.Random()).Next(elite.Length)].id;
            }
            if (randomBattleMusic)
            {
                var battle = tracks.Where(t => t.category == MusicCategory.Battle && t.clip != null).ToArray();
                if (battle.Length > 0) return battle[(rng ?? new System.Random()).Next(battle.Length)].id;
            }
            return defaultBattleMusic;
        }

        // The one place that decides what each game state plays. null = keep whatever is playing.
        public string MusicFor(GameState state, RunState run, System.Random rng) => state switch
        {
            GameState.MainMenu => menuMusic,
            GameState.Settings or GameState.CharacterStatus => null, // overlays: the music underneath carries on
            GameState.Hub => hubMusic,
            GameState.Party or GameState.HubShop or GameState.Armory or GameState.Blacksmith or GameState.Temple or GameState.MagicShop => null, // hub services keep the hub track
            GameState.CharacterCreation => creationMusic,
            GameState.OpeningSequence => "",
            GameState.Combat => BattleMusicFor(run?.CurrentEncounter, run?.CurrentEnemies, rng),
            _ => explorationMusic,
        };
    }
}
