using NUnit.Framework;
using PlantNursery.Game;
using PlantNursery.Sim.Building;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    public class BuildAimTests
    {
        [Test]
        public void FloorAnchor_OneCellFootprint_SnapsToAimedCell()
        {
            var c = BuildAim.FloorAnchor(new Vector3(1.3f, 0f, 0.8f), Vector3.zero, 1, 1);
            Assert.AreEqual(2, c.X);
            Assert.AreEqual(1, c.Z);
        }

        [Test]
        public void FloorAnchor_TwoCellFootprint_CentresOnAimPoint()
        {
            var c = BuildAim.FloorAnchor(new Vector3(1.05f, 0f, 1.05f), Vector3.zero, 2, 2);
            Assert.AreEqual(1, c.X);
            Assert.AreEqual(1, c.Z);
        }

        [Test]
        public void SurfaceAnchor_SnapsToNearestUnit()
        {
            var u = BuildAim.SurfaceAnchor(new Vector3(0.34f, 1f, 0.52f), Vector3.zero, 1, 1);
            Assert.AreEqual(3, u.U);
            Assert.AreEqual(5, u.V);
        }

        [Test]
        public void Next_CyclesNinetyDegrees()
        {
            Assert.AreEqual(Rotation.Deg90, BuildAim.Next(Rotation.Deg0));
            Assert.AreEqual(Rotation.Deg0, BuildAim.Next(Rotation.Deg270));
        }

        [Test]
        public void CapsuleOverlaps_PlayerInsideRect_True()
        {
            var rect = BuildAim.FloorRect(new Cell(2, 2), 2, 1, Vector3.zero);
            Assert.IsTrue(BuildAim.CapsuleOverlaps(rect, 1f, new Vector3(1.5f, 0f, 1.25f), 0.3f, 1.8f));
        }

        [Test]
        public void CapsuleOverlaps_RadiusTouchesEdge_True_ButClearOfEdge_False()
        {
            var rect = BuildAim.FloorRect(new Cell(2, 2), 1, 1, Vector3.zero); // x 1..1.5, z 1..1.5
            Assert.IsTrue(BuildAim.CapsuleOverlaps(rect, 1f, new Vector3(0.8f, 0f, 1.2f), 0.3f, 1.8f));
            Assert.IsFalse(BuildAim.CapsuleOverlaps(rect, 1f, new Vector3(0.6f, 0f, 1.2f), 0.3f, 1.8f));
        }
    }
}
