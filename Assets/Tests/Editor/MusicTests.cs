using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Obscure.Tests
{
    public class MusicTests
    {
        static MusicLibrary Library => Resources.Load<MusicLibrary>(MusicLibrary.ResourcePath);
        static readonly string[] BattleIds = { MusicIds.Battle1, MusicIds.Battle2, MusicIds.Battle3 };

        [Test]
        public void LibraryMapsTheProjectTracks()
        {
            var lib = Library;
            Assert.IsNotNull(lib, "Resources/MusicLibrary.asset is missing");
            var expected = new (string id, string file, MusicCategory category)[]
            {
                (MusicIds.MainMenu, "The beginning of an endless journey- Main Menu", MusicCategory.Menu),
                (MusicIds.Map, "Stone_Path_at_Daybreak", MusicCategory.Exploration),
                (MusicIds.Battle1, "When the journey becomes a battle - Battle Theme 1", MusicCategory.Battle),
                (MusicIds.Battle2, "Every shadow hides a challenge - Battle theme 2", MusicCategory.Battle),
                (MusicIds.Battle3, "Let every strike become a melody - Battle theme 3", MusicCategory.Battle),
            };
            foreach (var (id, file, category) in expected)
            {
                var t = lib.Get(id);
                Assert.IsNotNull(t, id);
                Assert.AreEqual(file, t.clip.name, id);
                Assert.AreEqual(category, t.category, id);
                Assert.IsTrue(t.loop, id);
                Assert.Greater(t.clip.length, 30f, id);
            }
            Assert.AreEqual(lib.tracks.Length, lib.tracks.Select(t => t.clip).Distinct().Count(), "every id has its own clip");
        }

        [Test]
        public void EncountersChooseTheirBattleThemeCentrally()
        {
            var lib = Library;
            var rng = new System.Random(1);
            Assert.AreEqual(MusicIds.Battle1, lib.defaultBattleMusic);
            Assert.AreEqual(MusicIds.Battle1, lib.BattleMusicFor(Database.Encounters["skel2"], rng), "no theme: default");
            Assert.AreEqual(MusicIds.Battle1, lib.BattleMusicFor(Database.Encounters["skel3"], rng));
            Assert.AreEqual(MusicIds.Battle2, lib.BattleMusicFor(Database.Encounters["elite1"], rng));
            Assert.AreEqual(MusicIds.Battle2, lib.BattleMusicFor(Database.Encounters["elite2"], rng));
            Assert.AreEqual(MusicIds.Battle3, lib.BattleMusicFor(Database.Encounters["boss"], rng));
            Assert.AreEqual(MusicIds.Battle1, lib.BattleMusicFor(null, rng));
            Assert.AreEqual(MusicIds.Battle1, lib.BattleMusicFor(new EncounterDef { music = "no_such_track" }, rng), "unknown id falls back");
            foreach (var e in Database.Encounters.Values.Where(e => e.music != null))
                Assert.IsNotNull(lib.Get(e.music), $"{e.id} names a track the library lacks");
        }

        [Test]
        public void RandomBattleMusicPicksAmongBattleThemesOnly()
        {
            var lib = Object.Instantiate(Library);
            try
            {
                lib.randomBattleMusic = true;
                var rng = new System.Random(3);
                var picks = Enumerable.Range(0, 200).Select(_ => lib.BattleMusicFor(Database.Encounters["skel2"], rng)).Distinct();
                CollectionAssert.AreEquivalent(BattleIds, picks);
                Assert.AreEqual(MusicIds.Battle3, lib.BattleMusicFor(Database.Encounters["boss"], rng), "an encounter's own theme wins");
            }
            finally { Object.DestroyImmediate(lib); }
        }

        [Test]
        public void EachGameStateAsksForTheRightMusic()
        {
            var lib = Library;
            var rng = new System.Random(1);
            Assert.AreEqual(MusicIds.MainMenu, lib.MusicFor(GameState.MainMenu, null, rng));
            Assert.IsNull(lib.MusicFor(GameState.Settings, null, rng), "settings keeps the menu music going");
            Assert.IsNull(lib.MusicFor(GameState.CharacterStatus, null, rng));
            foreach (var quiet in new[] { GameState.CharacterCreation, GameState.OpeningSequence })
                Assert.AreEqual("", lib.MusicFor(quiet, null, rng), quiet.ToString());
            foreach (var exploring in new[] { GameState.Map, GameState.Reward, GameState.Rest, GameState.Event, GameState.GameOver, GameState.Victory })
                Assert.AreEqual(MusicIds.Map, lib.MusicFor(exploring, null, rng), exploring.ToString());

            var run = new RunState(MapGenerator.Generate(7), Database.Party.Select(id => Database.Characters[id]), 7);
            var node = run.Available.First(n => n.IsCombat);
            run.Enter(node);
            var encounter = Database.Encounters[node.encounter];
            Assert.AreEqual(encounter.music ?? lib.defaultBattleMusic, lib.MusicFor(GameState.Combat, run, rng));
        }

        [Test]
        public void MusicSettingsDefaultPersistAndStaySeparateFromMaster()
        {
            var old = JsonUtility.FromJson<GameSettings>("{\"volume\":5}"); // a save from before music settings existed
            Assert.AreEqual(8, old.musicVolume);
            Assert.IsFalse(old.musicMuted);

            old.musicVolume = 3;
            var back = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(old));
            Assert.AreEqual(3, back.musicVolume);
            Assert.AreEqual(5, back.volume);
            Assert.AreEqual(0.3f, back.MusicLevel, 1e-5f);
            back.musicMuted = true;
            Assert.AreEqual(0f, JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(back)).MusicLevel);
        }

        [Test]
        public void UiHoverSfxAssetAndSynthesisMatchQuarterSecondCrystallineSpec()
        {
            var clip = Resources.Load<AudioClip>(Sfx.UiHoverResourcePath);
            Assert.IsNotNull(clip, "Resources/Sfx/ui_hover.wav is missing");
            Assert.AreEqual(0.25f, clip.length, 0.005f, "UI hover sound must be 0.25 seconds");
            Assert.AreEqual(44100, clip.frequency);

            var samples = Sfx.GenerateUiHoverSamples();
            Assert.AreEqual(11025, samples.Length);
            Assert.AreEqual(0f, samples[samples.Length - 1], 1e-4f, "Clean fade-out with no end click");

            var host = new GameObject("TestSfxHost");
            try
            {
                Sfx.Init(host);
                Assert.IsNotNull(Sfx.GetClip(Sound.Move));
                Assert.AreEqual(0.25f, Sfx.GetClip(Sound.Move).length, 0.005f);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void UiClickSfxIsCrispThreeTenthsOfASecond()
        {
            var clip = Resources.Load<AudioClip>(Sfx.UiClickResourcePath);
            Assert.IsNotNull(clip, "Resources/Sfx/ui_click.wav is missing");
            Assert.AreEqual(0.3f, clip.length, 0.005f, "UI click must be 0.3 seconds");
            Assert.AreEqual(44100, clip.frequency);

            var samples = Sfx.GenerateUiClickSamples();
            Assert.AreEqual(13230, samples.Length);
            Assert.AreEqual(0f, samples[samples.Length - 1], 1e-4f, "Clean fade-out with no end click");
            CollectionAssert.AreEqual(samples, Sfx.GenerateUiClickSamples(), "same click every time");

            int peakAt = 0;
            for (int i = 1; i < samples.Length; i++)
                if (Mathf.Abs(samples[i]) > Mathf.Abs(samples[peakAt])) peakAt = i;
            Assert.Less(peakAt, 0.005f * Sfx.Rate, "sharp attack: loudest within the first 5ms");
            Assert.LessOrEqual(Mathf.Abs(samples[peakAt]), 0.95f, "no clipping");
            Assert.Less(Rms(samples, 0.2f, 0.3f), Rms(samples, 0f, 0.04f) * 0.1f, "short tail");

            var host = new GameObject("TestSfxHost");
            try
            {
                Sfx.Init(host);
                Assert.AreEqual(0.3f, Sfx.GetClip(Sound.Confirm).length, 0.005f);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void UiConfirmSfxIsARisingSixTenthsOfASecondChime()
        {
            var clip = Resources.Load<AudioClip>(Sfx.UiConfirmResourcePath);
            Assert.IsNotNull(clip, "Resources/Sfx/ui_confirm.wav is missing");
            Assert.AreEqual(0.6f, clip.length, 0.005f, "UI confirmation must be 0.6 seconds");
            Assert.AreEqual(44100, clip.frequency);

            var samples = Sfx.GenerateUiConfirmSamples();
            Assert.AreEqual(26460, samples.Length);
            Assert.AreEqual(0f, samples[samples.Length - 1], 1e-4f, "Clean fade-out with no end click");
            CollectionAssert.AreEqual(samples, Sfx.GenerateUiConfirmSamples(), "same chime every time");
            Assert.LessOrEqual(samples.Max(Mathf.Abs), 0.95f, "no clipping");

            // The arpeggio keeps the level up while it rises, then the sparkling tail decays softly.
            float rising = Rms(samples, 0.05f, 0.2f), tail = Rms(samples, 0.35f, 0.5f), end = Rms(samples, 0.5f, 0.6f);
            Assert.GreaterOrEqual(rising, Rms(samples, 0f, 0.05f) * 0.9f, "ascending notes sustain, not a single hit");
            Assert.Less(tail, rising);
            Assert.Greater(tail, 0f, "audible sparkling tail");
            Assert.Less(end, tail);

            var host = new GameObject("TestSfxHost");
            try
            {
                Sfx.Init(host);
                Assert.AreEqual(0.6f, Sfx.GetClip(Sound.Success).length, 0.005f);
                Assert.AreNotSame(Sfx.GetClip(Sound.Confirm), Sfx.GetClip(Sound.Success), "button click and confirmation differ");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        static float Rms(float[] s, float from, float to)
        {
            int a = (int)(from * Sfx.Rate), b = (int)(to * Sfx.Rate);
            double sum = 0;
            for (int i = a; i < b; i++) sum += s[i] * s[i];
            return (float)System.Math.Sqrt(sum / (b - a));
        }
    }
}
