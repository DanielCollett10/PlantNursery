using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class NeighboursTests
    {
        private ContentDb _db;
        private BuildState _state;
        private SlotOccupancy _occ;
        private Neighbours _nb;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _state = new BuildState(_db);
            _state.Inventory.Add("bench_3m", 4);
            _state.Inventory.Add("bench_4m", 2);
            _occ = new SlotOccupancy(_db, _state);
            _nb = new Neighbours(_db, _state, _occ);
        }

        private int Bench(string id, int x, int z, Rotation r)
        {
            var p = _state.Place(id, ItemLayer.Floor, new Cell(x, z), r, out var rej);
            Assert.AreEqual(BuildRejection.None, rej);
            return p.Id;
        }

        private SlotRef Put(int bench, string size, int col, int row)
        {
            Assert.AreEqual(SlotRejection.None, _occ.PlaceInSlot(bench, size, col, row));
            return new SlotRef(bench, size, col, row);
        }

        private void Fill(int bench, string size)
        {
            var l = SlotLayout.For(_db.ItemDefs[_state.Get(bench).ItemId].Bench, _db.ContainerDefs[size]);
            for (var c = 0; c < l.Cols; c++)
                for (var r = 0; r < l.Rows; r++) Put(bench, size, c, r);
        }

        [TestCase("pot_small", 4)]
        [TestCase("pot_medium", 3)]
        [TestCase("pot_large", 2)]
        public void LoneBenchMaxima(string size, int max)
        {
            var b = Bench("bench_3m", 10, 10, Rotation.Deg0);
            Fill(b, size);
            var l = SlotLayout.For(_db.ItemDefs["bench_3m"].Bench, _db.ContainerDefs[size]);
            var best = 0;
            for (var c = 0; c < l.Cols; c++)
                for (var r = 0; r < l.Rows; r++)
                    best = System.Math.Max(best, _nb.Of(new SlotRef(b, size, c, r)).Count);
            Assert.AreEqual(max, best);
        }

        [Test]
        public void DiagonalsDoNotCount()
        {
            var b = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var a = Put(b, "pot_small", 0, 0);
            var d = Put(b, "pot_small", 1, 1);
            Assert.AreEqual(0, _nb.Of(a).Count);
            Assert.AreEqual(0, _nb.Of(d).Count);
            var e = Put(b, "pot_small", 1, 0);
            CollectionAssert.AreEquivalent(new[] { a, d }, _nb.Of(e));
        }

        [Test]
        public void BackToBackLinksOverlapWithMixedSizes()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var b = Bench("bench_3m", 10, 12, Rotation.Deg180);
            var s13 = Put(a, "pot_small", 13, 2);
            var s14 = Put(a, "pot_small", 14, 2);
            var s12 = Put(a, "pot_small", 12, 2);
            var m0 = Put(b, "pot_medium", 0, 1);
            CollectionAssert.AreEquivalent(new[] { s13, s14 }, _nb.Of(m0));
            CollectionAssert.Contains(_nb.Of(s14), m0);
            Assert.IsFalse(_nb.Of(s12).Contains(m0));
        }

        [Test]
        public void BackToBackRotatedBenches()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg90);
            var b = Bench("bench_3m", 12, 10, Rotation.Deg270);
            var a14 = Put(a, "pot_small", 14, 2);
            var a0 = Put(a, "pot_small", 0, 2);
            var b0 = Put(b, "pot_small", 0, 2);
            var b14 = Put(b, "pot_small", 14, 2);
            CollectionAssert.Contains(_nb.Of(a14), b0);
            CollectionAssert.Contains(_nb.Of(a0), b14);
            Assert.IsFalse(_nb.Of(a0).Contains(b0));
        }

        [Test]
        public void BackToFrontGivesNone()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var b = Bench("bench_3m", 10, 12, Rotation.Deg0);
            var x = Put(a, "pot_small", 3, 2);
            var y = Put(b, "pot_small", 3, 0);
            Assert.AreEqual(0, _nb.Of(x).Count);
            Assert.AreEqual(0, _nb.Of(y).Count);
        }

        [Test]
        public void OneCellGapGivesNone()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var b = Bench("bench_3m", 10, 13, Rotation.Deg180);
            var x = Put(a, "pot_small", 3, 2);
            var y = Put(b, "pot_small", 3, 2);
            Assert.AreEqual(0, _nb.Of(x).Count);
            Assert.AreEqual(0, _nb.Of(y).Count);
        }

        [Test]
        public void EndToEndLinksEndSlots()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var b = Bench("bench_4m", 16, 10, Rotation.Deg0);
            var ae = Put(a, "pot_small", 14, 1);
            var bs = Put(b, "pot_small", 0, 1);
            var mid = Put(b, "pot_small", 1, 1);
            CollectionAssert.Contains(_nb.Of(ae), bs);
            CollectionAssert.Contains(_nb.Of(bs), ae);
            Assert.IsFalse(_nb.Of(ae).Contains(mid));
        }

        [Test]
        public void UnoccupiedSlotHasNoNeighbours()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            Put(a, "pot_small", 0, 0);
            Assert.AreEqual(0, _nb.Of(new SlotRef(a, "pot_small", 5, 1)).Count);
        }

        [Test]
        public void StripItemsAreNeverInTheGraph()
        {
            // The graph is built from planting slots only; a strip item (even a carry tray of pots) has no slot address,
            // and a bench whose strip faces another bench's back stays unlinked.
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var b = Bench("bench_3m", 10, 8, Rotation.Deg0);
            var back = Put(b, "pot_small", 3, 2);
            var front = Put(a, "pot_small", 3, 0);
            Assert.AreEqual(0, _nb.Of(back).Count);
            Assert.AreEqual(0, _nb.Of(front).Count);
            Assert.AreEqual(2, _occ.PlacedSlots.Count());
        }

        [Test]
        public void GraphIsCachedAndRebuiltOnlyOnChange()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var s = Put(a, "pot_small", 0, 0);
            Assert.AreEqual(0, _nb.RebuildCount);
            _nb.Of(s);
            _nb.Of(s);
            Assert.AreEqual(1, _nb.RebuildCount);
            var t = Put(a, "pot_small", 1, 0);
            Assert.AreEqual(1, _nb.Of(s).Count);
            _nb.Of(t);
            Assert.AreEqual(2, _nb.RebuildCount);
            _occ.RemoveFromSlot(a, "pot_small", 1, 0);
            Assert.AreEqual(0, _nb.Of(s).Count);
            Assert.AreEqual(3, _nb.RebuildCount);
            _occ.RemoveFromSlot(a, "pot_small", 0, 0);
            _state.PickUp(a, out _);
            _nb.Of(s);
            _nb.Of(s);
            Assert.AreEqual(4, _nb.RebuildCount);
            // Queries never change state: a rejected placement does not rebuild.
            _occ.PlaceInSlot(999, "pot_small", 0, 0);
            _nb.Of(s);
            Assert.AreEqual(4, _nb.RebuildCount);
        }

        [Test]
        public void BenchRemovalDropsItsSlotsFromTheGraph()
        {
            var a = Bench("bench_3m", 10, 10, Rotation.Deg0);
            var b = Bench("bench_4m", 16, 10, Rotation.Deg0);
            var ae = Put(a, "pot_small", 14, 1);
            Put(b, "pot_small", 0, 1);
            Assert.AreEqual(1, _nb.Of(ae).Count);
            _state.PickUp(b, out _);
            Assert.AreEqual(0, _nb.Of(ae).Count);
        }
    }
}
