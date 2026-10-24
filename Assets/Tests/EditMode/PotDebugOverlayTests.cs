using NUnit.Framework;
using PlantNursery.Game;
using PlantNursery.Sim.Plants;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    public sealed class PotDebugOverlayTests
    {
        [Test]
        public void EmptyPotShowsNotAvailable()
        {
            Assert.AreEqual("N/A", PotDebugOverlay.Format(null, null, 0));
        }

        [Test]
        public void PlantShowsNameAndPercent()
        {
            var text = PotDebugOverlay.Format(null, new Plant(1, "moss"), 42);
            StringAssert.Contains("moss", text);
            StringAssert.Contains("42%", text);
        }

        [Test]
        public void OverlayIsOffByDefaultAndHidesLabelWhenOff()
        {
            var go = new GameObject("overlay");
            try
            {
                var overlay = go.AddComponent<PotDebugOverlay>();
                Assert.IsFalse(overlay.Visible);
                Assert.IsNull(overlay.CurrentText);
                overlay.Refresh(Vector2.zero); // no host/world wired: nothing hovered
                overlay.Visible = true;
                Assert.IsNull(overlay.CurrentText);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
