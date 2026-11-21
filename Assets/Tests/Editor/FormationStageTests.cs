using NUnit.Framework;

namespace Obscure.Tests
{
    // Combat placement: stage slots 0-1 are the front pair, 2-3 the back pair (CombatView.PartySlots).
    public class FormationStageTests
    {
        [Test]
        public void MeleeFrontRangedBack_RegardlessOfCreationOrder()
        {
            var stage = CombatView.StagePositions(new[] { "wizard", "fighter", "ranger", "barbarian" });
            CollectionAssert.AreEqual(new[] { 2, 0, 3, 1 }, stage);
        }

        [Test]
        public void ThirdMeleeSpillsToBack_CasterStaysBack()
        {
            var stage = CombatView.StagePositions(new[] { "cleric", "fighter", "barbarian", "martial_artist" });
            Assert.AreEqual(3, stage[0], "cleric behind everyone");
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, new[] { stage[1], stage[2], stage[3] });
        }

        [Test]
        public void RogueFillsTheGap()
        {
            // One melee: rogue takes the second front spot.
            CollectionAssert.AreEqual(new[] { 2, 1, 0, 3 }, CombatView.StagePositions(new[] { "wizard", "rogue", "fighter", "druid" }));
            // Two melee: rogue goes back but ahead of the caster.
            CollectionAssert.AreEqual(new[] { 3, 2, 0, 1 }, CombatView.StagePositions(new[] { "warlock", "rogue", "fighter", "barbarian" }));
        }
    }
}
