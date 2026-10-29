using System;
using System.Linq;
using UnityEngine;

// All background music in one asset (Resources/MusicLibrary). Gameplay asks for music by id, never by clip.
namespace Obscure
{
    public enum MusicCategory { Menu, Battle, Exploration }

    public static class MusicIds
    {
        public const string MainMenu = "main_menu", Battle1 = "battle_1", Battle2 = "battle_2", Battle3 = "battle_3";
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
    }

    [CreateAssetMenu(menuName = "Obscure/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        public const string ResourcePath = "MusicLibrary";

        public MusicTrack[] tracks = Array.Empty<MusicTrack>();

        [Header("Selection (empty id = silence)")]
        public string menuMusic = MusicIds.MainMenu;
        public string creationMusic = "";       // character creation and prologue
        public string explorationMusic = MusicIds.MainMenu; // map, rewards, rest, events, run summary
        public string defaultBattleMusic = MusicIds.Battle1;
        public bool randomBattleMusic;           // encounters without their own theme pick a random battle track

        [Header("Transitions (seconds)")]
        public float fadeOut = 0.6f;
        public float fadeIn = 0.8f;
        public float transitionGap = 0.1f;       // silence between the old track ending and the new one starting

        public MusicTrack Get(string id) => string.IsNullOrEmpty(id) ? null : tracks.FirstOrDefault(t => t.id == id && t.clip != null);

        public float FadeIn(MusicTrack t) => t.fadeIn >= 0 ? t.fadeIn : fadeIn;
        public float FadeOut(MusicTrack t) => t.fadeOut >= 0 ? t.fadeOut : fadeOut;

        // The encounter's own theme if it has one, else a random battle theme (when enabled), else the default.
        public string BattleMusicFor(EncounterDef encounter, System.Random rng)
        {
            if (Get(encounter?.music) != null) return encounter.music;
            if (randomBattleMusic)
            {
                var battle = tracks.Where(t => t.category == MusicCategory.Battle && t.clip != null).ToArray();
                if (battle.Length > 0) return battle[rng.Next(battle.Length)].id;
            }
            return defaultBattleMusic;
        }

        // The one place that decides what each game state plays. null = keep whatever is playing.
        public string MusicFor(GameState state, RunState run, System.Random rng) => state switch
        {
            GameState.MainMenu => menuMusic,
            GameState.Settings or GameState.CharacterStatus => null, // overlays: the music underneath carries on
            GameState.CharacterCreation or GameState.OpeningSequence => creationMusic,
            GameState.Combat => BattleMusicFor(run?.CurrentEncounter, rng),
            _ => explorationMusic,
        };
    }
}
