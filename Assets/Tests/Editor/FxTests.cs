using NUnit.Framework;

namespace Obscure.Tests
{
    public class FxTests
    {
        // Every effect CombatView can play must load, and sheet slicing must start at the top-left frame.
        [Test]
        public void CombatFxSheets_LoadAndSliceTopDown()
        {
            foreach (var name in new[] { "hit", "crit", "parry", "break", "fire", "ice", "lightning", "arcane", "holy", "dark", "earth", "wind", "water", "heal", "death" })
                Assert.That(Fx.Load(name)?.frames, Is.Not.Null.And.Not.Empty, name);

            var hit = Fx.Load("hit");
            var first = hit.frames[0];
            Assert.That(hit.frames.Length, Is.EqualTo(16));
            Assert.That(first.rect.x, Is.EqualTo(0));
            Assert.That(first.rect.y, Is.EqualTo(first.texture.height - first.rect.height));
            Assert.That(hit.grounded, Is.True);
            Assert.That(Fx.Load("lightning").grounded, Is.False);
        }
    }
}
