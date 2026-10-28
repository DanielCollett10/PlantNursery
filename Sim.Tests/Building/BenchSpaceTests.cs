using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class BenchSpaceTests
    {
        private ContentDb _db;
        private BuildState _state;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _state = new BuildState(_db);
            _state.Inventory.Add("bench_3m", 6);
        }

        private PlacedItem Bench(int x, int z, Rotation r)
        {
            var p = _state.Place("bench_3m", ItemLayer.Floor, new Cell(x, z), r, out var rej);
            Assert.AreEqual(BuildRejection.None, rej);
            return p;
        }

        [TestCase(Rotation.Deg0)]
        [TestCase(Rotation.Deg90)]
        [TestCase(Rotation.Deg180)]
        [TestCase(Rotation.Deg270)]
        public void LocalWorldRoundTrips(Rotation r)
        {
            var b = Bench(10, 10, r);
            Assert.IsTrue(BenchSpace.TryLocalToWorld(_db, b, 0.7, 0.3, out var x, out var z));
            Assert.IsTrue(BenchSpace.TryWorldToLocal(_db, b, x, z, out var u, out var v));
            Assert.AreEqual(0.7, u, 1e-9);
            Assert.AreEqual(0.3, v, 1e-9);
        }

        [Test]
        public void PointerThroughWorldFindsTheSlot()
        {
            var b = Bench(10, 10, Rotation.Deg90);
            BenchSpace.TryLocalToWorld(_db, b, 1.55, 0.55, out var x, out var z);
            BenchSpace.TryWorldToLocal(_db, b, x, z, out var u, out var v);
            var a = SlotPointer.SlotAt(_db, b, u, v, "pot_small");
            Assert.IsTrue(a.HasValue);
            Assert.AreEqual(SlotAddress.Planting(b.Id, "pot_small", 7, 0).Key, a.Value.Key);
        }

        [Test]
        public void SlotRectMatchesPointer()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            var addr = SlotAddress.Planting(b.Id, "pot_small", 3, 1);
            var rect = BenchSpace.SlotRectOf(_db, _state, addr);
            Assert.IsTrue(rect.HasValue);
            var hit = SlotPointer.SlotAt(_db, b, rect.Value.CentreU, rect.Value.CentreV, "pot_small");
            Assert.AreEqual(addr.Key, hit.Value.Key);
            var strip = SlotAddress.Strip(b.Id, "watering_can", 2, 0);
            var sr = BenchSpace.SlotRectOf(_db, _state, strip);
            Assert.AreEqual(strip.Key, SlotPointer.SlotAt(_db, b, sr.Value.CentreU, sr.Value.CentreV, "watering_can").Value.Key);
        }

        [Test]
        public void BackToBackBenchesShareAViewGroup()
        {
            var a = Bench(10, 10, Rotation.Deg0);
            var behind = Bench(10, 12, Rotation.Deg180); // a is 2 cells deep: this one's back touches a's back
            var apart = Bench(10, 20, Rotation.Deg0);
            var group = BenchSpace.ViewGroup(_db, _state, a.Id);
            CollectionAssert.AreEquivalent(new[] { a.Id, behind.Id }, group);
            Assert.AreEqual(a.Id, group[0]);
            CollectionAssert.AreEquivalent(new[] { behind.Id, a.Id }, BenchSpace.ViewGroup(_db, _state, behind.Id));
            CollectionAssert.AreEqual(new[] { apart.Id }, BenchSpace.ViewGroup(_db, _state, apart.Id));
        }

        [Test]
        public void FrontToFrontOrSameWayBenchesDoNotShare()
        {
            var a = Bench(10, 10, Rotation.Deg0);
            Bench(10, 12, Rotation.Deg0);
            Assert.AreEqual(1, BenchSpace.ViewGroup(_db, _state, a.Id).Count);
        }

        [Test]
        public void DistanceToBenchEdge()
        {
            var b = Bench(10, 10, Rotation.Deg0); // x 5..8, z 5..6
            var ids = new[] { b.Id };
            Assert.AreEqual(0.0, BenchSpace.DistanceToBenches(_db, _state, ids, 6.0, 5.5), 1e-9);
            Assert.AreEqual(1.0, BenchSpace.DistanceToBenches(_db, _state, ids, 6.0, 4.0), 1e-9);
            Assert.AreEqual(1.0, BenchSpace.DistanceToBenches(_db, _state, ids, 9.0, 5.5), 1e-9);
        }
    }
}
