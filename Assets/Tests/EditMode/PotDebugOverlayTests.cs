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
                Assert.IsNull(overlay.IndicatorLabel);
                overlay.Refresh(Vector2.zero); // no host/world wired: nothing hovered
                overlay.Visible = true;
                Assert.IsNull(overlay.CurrentText);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ResolveHoverTriesCandidatesInOrder()
        {
            var cands = new System.Collections.Generic.List<Vector2> { new Vector2(10, 10), new Vector2(200, 100), new Vector2(300, 300) };
            var hit = PotDebugOverlay.ResolveHover(cands, p => p.x >= 200 ? "pot" + p.x : null, out var used);
            Assert.AreEqual("pot200", hit);
            Assert.AreEqual(new Vector2(200, 100), used);
        }

        [Test]
        public void ResolveHoverReturnsNullWhenNothingHits()
        {
            var cands = new System.Collections.Generic.List<Vector2> { new Vector2(1, 2) };
            Assert.IsNull(PotDebugOverlay.ResolveHover<string>(cands, p => null, out var used));
            Assert.AreEqual(new Vector2(1, 2), used);
        }

        [Test]
        public void ToCameraPixelsScalesIntoPixelRect()
        {
            Assert.AreEqual(new Vector2(1920, 1080), PotDebugOverlay.ToCameraPixels(new Vector2(960, 540), new Rect(0, 0, 1920, 1080), 960, 540));
            Assert.AreEqual(new Vector2(5, 7), PotDebugOverlay.ToCameraPixels(new Vector2(5, 7), new Rect(0, 0, 100, 100), 0, 0));
        }

        [Test]
        public void DiagnosticsShownOnlyWhenOn()
        {
            var go = new GameObject("overlay");
            try
            {
                var overlay = go.AddComponent<PotDebugOverlay>();
                overlay.Refresh(new Vector2(5, 5));
                Assert.IsNull(overlay.DiagnosticsLine);
                overlay.Visible = true;
                overlay.Refresh(new Vector2(5, 5));
                StringAssert.Contains("(5,5)", overlay.DiagnosticsLine);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void IndicatorShowsWhenOnAndHidesWhenOff()
        {
            var go = new GameObject("overlay");
            try
            {
                var overlay = go.AddComponent<PotDebugOverlay>();
                overlay.Visible = true;
                Assert.AreEqual("Pot debug ON (F1)", overlay.IndicatorLabel);
                overlay.Visible = false;
                Assert.IsNull(overlay.IndicatorLabel);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
