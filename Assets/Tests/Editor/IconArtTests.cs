using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obscure.Tests
{
    // Generated icons (Resources/Icons, `node Tools/PixelArt/icons.js --export`) cover every catalogue id.
    public class IconArtTests
    {
        static void AssertIcons(IEnumerable<string> ids, System.Func<string, Texture2D> load, string what)
        {
            // other suites register throwaway "test_*" defs in the shared Database; they are not game content
            ids = ids.Where(id => !id.StartsWith("test_")).ToList();
            var missing = ids.Where(id => load(id) == null).ToList();
            Assert.IsEmpty(missing, what + " without an icon: " + string.Join(", ", missing));
            foreach (var id in ids)
            {
                var t = load(id);
                Assert.AreEqual(IconArt.Size, t.width, id);
                Assert.AreEqual(IconArt.Size, t.height, id);
            }
        }

        [Test]
        public void EveryItemHasAnIcon()
        {
            AssertIcons(Database.Items.Keys, IconArt.Item, "item");
            AssertIcons(Database.Consumables.Keys, IconArt.Item, "consumable");
            AssertIcons(Database.Materials.Keys, IconArt.Item, "material");
            AssertIcons(Database.KeyItems.Keys, IconArt.Item, "key item");
        }

        [Test]
        public void EveryGearPieceHasAnIcon()
        {
            AssertIcons(Database.Equipment.Keys, IconArt.Gear, "equipment");
            AssertIcons(Database.CatalogueEquipment.Keys, IconArt.Gear, "catalogue equipment");
        }

        [Test]
        public void EverySkillAndPassiveHasAnIcon()
        {
            AssertIcons(Database.Skills.Keys, IconArt.Skill, "skill");
            AssertIcons(Database.Passives.Keys, IconArt.Passive, "passive");
        }

        [Test]
        public void EveryStatusEffectHasAnIconAndBadgesBuild()
        {
            foreach (StatusId id in System.Enum.GetValues(typeof(StatusId)))
            {
                var tex = IconArt.Status(id);
                Assert.IsNotNull(tex, $"Missing status icon for {id}");
                Assert.AreEqual(IconArt.Size, tex.width, id.ToString());
                Assert.AreEqual(IconArt.Size, tex.height, id.ToString());
                Assert.AreEqual(FilterMode.Point, tex.filterMode, id.ToString());

                var badge = IconArt.StatusBadge(id, turns: 3, stacks: 2, scale: 2);
                Assert.IsNotNull(badge);
                Assert.AreEqual(1, badge.childCount);

                var chip = IconArt.StatusChip(id, $"{StatusInfo.Name(id)} 3T");
                Assert.IsNotNull(chip);
                Assert.AreEqual(2, chip.childCount);
            }
        }

        [Test]
        public void EveryRacialFeatHasAnIconWithSigilFallback()
        {
            AssertIcons(Database.Races.Values.Where(r => r.racialFeat != null).Select(r => r.racialFeat.id), IconArt.Racial, "racial feat");
            Assert.AreSame(IconArt.Racial("_sigil"), IconArt.Racial("no_such_feat"));
            Assert.IsNotNull(IconArt.Racial(null));
        }

        [Test]
        public void EveryElementHasAnIcon()
        {
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
            {
                var tex = IconArt.Element(e);
                Assert.IsNotNull(tex, $"Missing element icon for {e}");
                Assert.AreEqual(IconArt.Size, tex.width, e.ToString());
                Assert.AreEqual(FilterMode.Point, tex.filterMode, e.ToString());
            }
            Assert.AreSame(IconArt.Element(Element.Dark), IconArt.Element(Element.Shadow));
        }

        [Test]
        public void IconsAreCrispAndDistinct()
        {
            var a = IconArt.Skill("firebolt");
            Assert.AreEqual(FilterMode.Point, a.filterMode);
            Assert.AreNotSame(a, IconArt.Skill("frost_nova"));
            Assert.AreSame(IconArt.Gear("iron_broadsword"), IconArt.Gear(Database.Equipment["iron_broadsword"]));
            Assert.IsNull(IconArt.Skill("no_such_skill"));
            Assert.IsNull(IconArt.Item(null));
        }

        [Test]
        public void OverlaySitsUnderTheSlotLabelsAtWholePixelScale()
        {
            var slot = new VisualElement();
            slot.Add(new UnityEngine.UIElements.Label("R"));
            var icon = IconArt.Overlay(slot, IconArt.Item("CON001"));
            Assert.AreSame(icon, slot[0]);
            Assert.AreEqual(48f, icon.style.width.value.value);
            Assert.IsNull(IconArt.Overlay(slot, null));
            Assert.AreEqual(2, slot.childCount);
        }
    }
}
