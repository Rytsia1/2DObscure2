using NUnit.Framework;

namespace Obscure.Tests
{
    // Remodeled unit art (Resources/Heroes, Resources/Enemies) and its idle-loop timing.
    public class UnitArtTests
    {
        static void AssertFrames(UnityEngine.Texture2D[] frames, int w, int h, string what, int count = 4)
        {
            Assert.IsNotNull(frames, what);
            Assert.AreEqual(count, frames.Length, what);
            foreach (var f in frames)
            {
                Assert.IsNotNull(f, what);
                Assert.AreEqual(w, f.width, what);
                Assert.AreEqual(h, f.height, what);
            }
        }

        [Test]
        public void EveryHeroSpriteLoadsFourCrispFrames()
        {
            foreach (var rows in new[] { Art.Knight, Art.Fighter, Art.Barbarian, Art.MartialArtist, Art.Rogue,
                                         Art.Ranger, Art.Healer, Art.Mage, Art.Warlock, Art.Druid })
                AssertFrames(UnitArt.Idle(rows), 22, 27, rows[0]);
        }

        [Test]
        public void EveryDungeon1EnemyLoadsArtSizedForItsBox()
        {
            foreach (var id in new[] { "hollow_squire", "gravebound_archer", "cinder_wisp", "soulbound_hound", "possessed_royal_guard",
                                       "crypt_leech", "forgotten_militia", "soul_reaper", "royal_ossuary_knight" })
                AssertFrames(UnitArt.Enemy(id, Art.Skeleton), 24, 26, id);
            AssertFrames(UnitArt.Enemy("zoroa_p1", Art.Warden), 48, 60, "zoroa_p1"); // 4x in the 192x240 boss box
            AssertFrames(UnitArt.Enemy("zoroa_p2", Art.Warden), 48, 60, "zoroa_p2");
        }

        [Test]
        public void EnemiesSharingRowsGetTheirOwnArt() =>
            Assert.AreNotSame(UnitArt.Enemy("cinder_wisp", Art.Skeleton)[0], UnitArt.Enemy("hollow_squire", Art.Skeleton)[0]);

        [Test]
        public void BossPhaseTwoHasDistinctArt() =>
            Assert.AreNotSame(UnitArt.Enemy("zoroa_p1", Art.Warden)[0], UnitArt.Enemy("zoroa_p2", Art.Warden)[0]);

        [Test]
        public void LegacyEnemiesFallBackByRows()
        {
            Assert.AreSame(UnitArt.Enemy("hollow_squire", null)[0], UnitArt.Enemy("skeleton", Art.Skeleton)[0]);
            Assert.AreSame(UnitArt.Enemy("zoroa_p1", null)[0], UnitArt.Enemy("warden", Art.Warden)[0]);
            Assert.IsNull(UnitArt.Enemy("unknown", Art.Knight));
        }

        [Test]
        public void HeroLookupIgnoresEnemyRows() => Assert.IsNull(UnitArt.Idle(Art.Skeleton));

        [Test]
        public void EveryHeroHasActionHitAndKoClips()
        {
            foreach (var rows in new[] { Art.Knight, Art.Fighter, Art.Barbarian, Art.MartialArtist, Art.Rogue,
                                         Art.Ranger, Art.Healer, Art.Mage, Art.Warlock, Art.Druid })
                foreach (var name in new[] { "act", "hit", "ko" })
                {
                    var clip = UnitArt.HeroClip(rows, name);
                    Assert.IsNotNull(clip, name + " " + rows[0]);
                    AssertFrames(clip.frames, 22, 27, name, clip.delays.Length); // one delay per frame
                    if (name == "act") Assert.That(clip.strike, Is.InRange(1, clip.frames.Length - 1), "strike frame");
                }
        }

        [Test]
        public void ActionStyleFollowsTheClass()
        {
            Assert.AreEqual("melee", UnitArt.HeroClip(Art.Fighter, "act").style);
            Assert.AreEqual("shoot", UnitArt.HeroClip(Art.Ranger, "act").style);
            Assert.AreEqual("cast", UnitArt.HeroClip(Art.Mage, "act").style);
            Assert.AreEqual("cast", UnitArt.HeroClip(Art.Healer, "act").style);
        }

        [Test]
        public void EveryDungeon1EnemyHasAnAttackClipSizedLikeItsIdle()
        {
            foreach (var id in new[] { "hollow_squire", "gravebound_archer", "cinder_wisp", "soulbound_hound", "possessed_royal_guard",
                                       "crypt_leech", "forgotten_militia", "soul_reaper", "royal_ossuary_knight", "zoroa_p1", "zoroa_p2" })
            {
                var clip = UnitArt.EnemyClip(id, Art.Skeleton);
                var idle = UnitArt.Enemy(id, Art.Skeleton)[0];
                Assert.IsNotNull(clip, id);
                AssertFrames(clip.frames, idle.width, idle.height, id, clip.delays.Length);
                Assert.That(clip.strike, Is.InRange(1, clip.frames.Length - 1), id + " strike");
            }
            Assert.AreEqual("reap", UnitArt.EnemyClip("zoroa_p2", null).style);
            Assert.AreEqual("pounce", UnitArt.EnemyClip("soulbound_hound", null).style);
        }

        [Test]
        public void LegacyEnemiesBorrowTheClosestAttackClip()
        {
            Assert.AreSame(UnitArt.EnemyClip("hollow_squire", null), UnitArt.EnemyClip("skeleton", Art.Skeleton));
            Assert.IsNull(UnitArt.EnemyClip("unknown", Art.Knight));
        }

        [Test]
        public void StrikeLandsAfterTheWindUp()
        {
            var act = UnitArt.HeroClip(Art.Fighter, "act");
            Assert.AreEqual(act.delays[0] + act.delays[1], act.TimeTo(act.strike), 1e-4f);
            Assert.IsNull(UnitArt.HeroClip(Art.Skeleton, "act"));
        }

        [Test]
        public void IdleFrameFollowsDelaysAndLoops()
        {
            Assert.AreEqual(0, UnitArt.FrameAt(0f));
            Assert.AreEqual(1, UnitArt.FrameAt(0.33f));
            Assert.AreEqual(2, UnitArt.FrameAt(0.51f));
            Assert.AreEqual(3, UnitArt.FrameAt(0.9f));
            Assert.AreEqual(0, UnitArt.FrameAt(1.05f));
        }
    }
}
