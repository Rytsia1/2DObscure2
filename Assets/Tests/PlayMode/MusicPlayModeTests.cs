using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Obscure.Tests
{
    // Real playback through the booted Game: state changes go through Game.Go exactly as in play.
    public class MusicPlayModeTests
    {
        static MusicManager Music => MusicManager.I;
        static MusicLibrary Library => Music.Library;
        int savedMusicVolume;
        bool savedMuted;

        static IEnumerator Until(Func<bool> done, float timeout = 6f)
        {
            for (float t = 0; !done() && t < timeout; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(done(), "timed out");
        }

        static IEnumerator GoTo(GameState state)
        {
            yield return Until(() => !Controls.Locked); // Go is ignored mid-transition
            Game.I.Go(state);
            yield return Until(() => Game.I.State == state && !Controls.Locked);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Until(() => Game.I != null && MusicManager.I != null);
            var s = Game.I.Settings;
            savedMusicVolume = s.musicVolume;
            savedMuted = s.musicMuted;
            s.musicVolume = 10;
            s.musicMuted = false;
            s.Apply(); // not saved: the player's preferences stay untouched
            if (Game.I.State != GameState.MainMenu) yield return GoTo(GameState.MainMenu);
            yield return Until(() => !Music.Fading);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var s = Game.I.Settings;
            s.musicVolume = savedMusicVolume;
            s.musicMuted = savedMuted;
            s.Apply();
            yield return null;
        }

        [UnityTest]
        public IEnumerator OneManagerAndOneMusicSource()
        {
            Assert.AreSame(Music, MusicManager.Create(Game.I.gameObject), "creating again reuses the instance");
            yield return GoTo(GameState.Settings);
            yield return GoTo(GameState.MainMenu);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<MusicManager>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(2, Game.I.GetComponents<AudioSource>().Length, "one for sound effects, one for music");
        }

        [UnityTest]
        public IEnumerator MainMenuPlaysLoopingMenuMusic()
        {
            Assert.AreEqual(MusicIds.MainMenu, Music.CurrentId);
            Assert.AreSame(Library.Get(MusicIds.MainMenu).clip, Music.Source.clip);
            Assert.IsTrue(Music.Source.loop);
            Assert.IsTrue(Music.Source.isPlaying);
            Assert.AreEqual(1f, Music.Source.volume, 1e-4f);
            yield break;
        }

        [UnityTest]
        public IEnumerator MenuMusicIsNotRestartedBySettingsOrRepeatRequests()
        {
            var clip = Music.Source.clip;
            yield return new WaitForSecondsRealtime(0.3f);
            int before = Music.Source.timeSamples;
            Music.Play(MusicIds.MainMenu);
            Assert.IsFalse(Music.Fading, "same track requested again: no fade, no restart");
            yield return GoTo(GameState.Settings);
            Assert.IsFalse(Music.Fading);
            yield return GoTo(GameState.MainMenu);
            Assert.IsFalse(Music.Fading);
            Assert.AreSame(clip, Music.Source.clip);
            Assert.IsTrue(Music.Source.isPlaying);
            Assert.GreaterOrEqual(Music.Source.timeSamples, before, "playback position kept advancing");
        }

        [UnityTest]
        public IEnumerator NewGameFadesMenuMusicOutAndTitleBringsItBack()
        {
            Game.I.StartNewGame();
            yield return Until(() => Game.I.State == GameState.CharacterCreation && !Controls.Locked);
            Assert.IsNull(Music.CurrentId, "character creation is silent");
            yield return Until(() => !Music.Fading);
            Assert.IsFalse(Music.Source.isPlaying);

            yield return GoTo(GameState.MainMenu);
            Assert.AreEqual(MusicIds.MainMenu, Music.CurrentId);
            yield return Until(() => !Music.Fading);
            Assert.IsTrue(Music.Source.isPlaying);
            Assert.AreEqual(1f, Music.Source.volume, 1e-4f);
        }

        [UnityTest]
        public IEnumerator EachBattleThemePlaysAndLoops()
        {
            foreach (var id in new[] { MusicIds.Battle1, MusicIds.Battle2, MusicIds.Battle3 })
            {
                Music.Play(id);
                Assert.AreEqual(id, Music.CurrentId);
                yield return Until(() => !Music.Fading);
                Assert.AreSame(Library.Get(id).clip, Music.Source.clip, id);
                Assert.IsTrue(Music.Source.loop, id);
                Assert.IsTrue(Music.Source.isPlaying, id);
                Assert.AreEqual(1f, Music.Source.volume, 1e-4f, id);
            }
            Music.Play(MusicIds.MainMenu);
            yield return Until(() => !Music.Fading);
        }

        [UnityTest]
        public IEnumerator BattleUsesItsEncounterThemeThenMapPlaysMenuMusic()
        {
            var run = new RunState(MapGenerator.Generate(11), Database.Party.Select(id => Database.Characters[id]), 11);
            typeof(Game).GetProperty(nameof(Game.Run)).SetValue(Game.I, run); // no Continue/save involved
            var node = run.Available.First(n => n.IsCombat);
            run.Enter(node);
            var expected = Database.Encounters[node.encounter].music ?? Library.defaultBattleMusic;

            yield return GoTo(GameState.Combat);
            Assert.AreEqual(expected, Music.CurrentId);
            yield return Until(() => !Music.Fading);
            Assert.AreSame(Library.Get(expected).clip, Music.Source.clip);
            Assert.IsTrue(Music.Source.isPlaying);

            yield return GoTo(GameState.Map); // battle -> exploration
            Assert.AreEqual(MusicIds.MainMenu, Music.CurrentId);
            yield return Until(() => !Music.Fading);
            Assert.AreSame(Library.Get(MusicIds.MainMenu).clip, Music.Source.clip);
            Assert.IsTrue(Music.Source.isPlaying);

            Game.I.EndRun(); // -> title: same track, so it carries on without restarting
            yield return Until(() => Game.I.State == GameState.MainMenu && !Controls.Locked);
            Assert.AreEqual(MusicIds.MainMenu, Music.CurrentId);
            Assert.IsFalse(Music.Fading);
        }

        [UnityTest]
        public IEnumerator InterruptedTransitionsEndOnTheLastRequest()
        {
            Music.Play(MusicIds.Battle1);
            yield return new WaitForSecondsRealtime(0.2f);  // menu music half faded out
            Music.Play(MusicIds.Battle2);
            yield return null;
            Music.Play(MusicIds.MainMenu);                  // back to the track still fading: resumes, no restart
            yield return Until(() => !Music.Fading);
            Assert.AreEqual(MusicIds.MainMenu, Music.CurrentId);
            Assert.AreSame(Library.Get(MusicIds.MainMenu).clip, Music.Source.clip);
            Assert.IsTrue(Music.Source.isPlaying);
            Assert.AreEqual(1f, Music.Source.volume, 1e-4f);
            Assert.AreEqual(2, Game.I.GetComponents<AudioSource>().Length);
        }

        [UnityTest]
        public IEnumerator MusicVolumeAndMuteLeaveSoundEffectsAlone()
        {
            var sfx = Game.I.GetComponents<AudioSource>().Single(a => a != Music.Source);
            float sfxVolume = sfx.volume, master = AudioListener.volume;
            var s = Game.I.Settings;

            s.musicVolume = 5;
            s.Apply();
            Assert.AreEqual(0.5f, Music.Source.volume, 1e-4f);
            s.musicMuted = true;
            s.Apply();
            Assert.AreEqual(0f, Music.Source.volume);
            Assert.IsTrue(Music.Source.isPlaying, "muting silences without stopping");
            Assert.AreEqual(sfxVolume, sfx.volume);
            Assert.AreEqual(master, AudioListener.volume);
            yield break;
        }
    }
}
