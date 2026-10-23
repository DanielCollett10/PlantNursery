using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    public class WorldLayoutTests
    {
        [Test]
        public void FloorItemSitsAtCellTimesHalfMetre()
        {
            var item = new PlacedItem(1, "bench", new Cell(3, 2), Rotation.Deg0, 1, 4);
            Assert.AreEqual(new Vector3(1.5f, 0f, 1f), WorldLayout.FloorPosition(item, Vector3.zero));
        }

        [Test]
        public void RotatedFootprintKeepsSouthWestCornerAtAnchor()
        {
            // 1x4 bench rotated 90: footprint becomes 4 wide, 1 deep; pivot shifts to the north-west of the box.
            var item = new PlacedItem(1, "bench", new Cell(2, 2), Rotation.Deg90, 4, 1);
            Assert.AreEqual(new Vector3(1f, 0f, 1.5f), WorldLayout.FloorPosition(item, Vector3.zero));
            var item180 = new PlacedItem(2, "bench", new Cell(0, 0), Rotation.Deg180, 1, 4);
            Assert.AreEqual(new Vector3(0.5f, 0f, 2f), WorldLayout.FloorPosition(item180, Vector3.zero));
        }

        [Test]
        public void SurfaceItemUsesTenthMetreUnitsAtSupportHeight()
        {
            var item = new SurfaceItem(1, "pot_small", new Unit(12, 7), Rotation.Deg0, 1, 1);
            var p = WorldLayout.SurfacePosition(item, new Vector3(1f, 0f, 1f), 0.8f);
            Assert.AreEqual(2.2f, p.x, 1e-4f);
            Assert.AreEqual(0.8f, p.y, 1e-4f);
            Assert.AreEqual(1.7f, p.z, 1e-4f);
        }

        [Test]
        public void PlantScaleRunsFromTenthToOne()
        {
            Assert.AreEqual(0.1f, WorldLayout.PlantScale(PlantStage.Seed), 1e-4f);
            Assert.AreEqual(1f, WorldLayout.PlantScale(PlantStage.Mature), 1e-4f);
            Assert.Greater(WorldLayout.PlantScale(PlantStage.Young), WorldLayout.PlantScale(PlantStage.Rooted));
        }

        [Test]
        public void LeafColourFollowsAllele()
        {
            Assert.AreEqual(WorldLayout.LeafRed, WorldLayout.LeafColour("red"));
            Assert.AreEqual(WorldLayout.LeafYellow, WorldLayout.LeafColour("yellow"));
            Assert.AreEqual(WorldLayout.LeafGreen, WorldLayout.LeafColour("green"));
        }

        [Test]
        public void WiltingDesaturatesAndDeathIsBrownGrey()
        {
            Assert.AreEqual(0f, WorldLayout.WiltAmount(50f));
            Assert.AreEqual(1f, WorldLayout.WiltAmount(0f));
            Color.RGBToHSV(WorldLayout.Tinted(WorldLayout.LeafRed, 0f, false), out _, out var fresh, out _);
            Color.RGBToHSV(WorldLayout.Tinted(WorldLayout.LeafRed, 1f, false), out _, out var wilted, out _);
            Assert.Less(wilted, fresh);
            Assert.AreEqual(WorldLayout.DeadColour, WorldLayout.Tinted(WorldLayout.LeafRed, 1f, true));
        }

        [Test]
        public void CellCentresStayInsideFootprint()
        {
            for (var i = 0; i < 6; i++)
            {
                var c = WorldLayout.CellCentre(i, 6, 2, 3);
                Assert.That(c.x, Is.InRange(0f, 0.2f));
                Assert.That(c.z, Is.InRange(0f, 0.3f));
            }
            Assert.AreNotEqual(WorldLayout.CellCentre(0, 6, 2, 3), WorldLayout.CellCentre(5, 6, 2, 3));
        }
    }
}
