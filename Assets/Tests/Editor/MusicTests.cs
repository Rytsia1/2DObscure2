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
        public void LibraryMapsTheFourProjectTracks()
        {
            var lib = Library;
            Assert.IsNotNull(lib, "Resources/MusicLibrary.asset is missing");
            var expected = new (string id, string file, MusicCategory category)[]
            {
                (MusicIds.MainMenu, "The beginning of an endless journey- Main Menu", MusicCategory.Menu),
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
            Assert.AreEqual(4, lib.tracks.Select(t => t.clip).Distinct().Count(), "every id has its own clip");
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
                Assert.AreEqual(MusicIds.MainMenu, lib.MusicFor(exploring, null, rng), exploring.ToString());

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
    }
}
