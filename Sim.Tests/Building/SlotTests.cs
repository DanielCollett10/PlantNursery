using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class SlotTests
    {
        private ContentDb _db;
        private BuildState _state;
        private SlotOccupancy _occ;
        private int _bench3;
        private int _bench4;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _state = new BuildState(_db);
            _state.Inventory.Add("bench_3m");
            _state.Inventory.Add("bench_4m");
            _state.Inventory.Add("tap");
            _bench3 = _state.Place("bench_3m", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _).Id;
            _bench4 = _state.Place("bench_4m", ItemLayer.Floor, new Cell(10, 14), Rotation.Deg0, out _).Id;
            _occ = new SlotOccupancy(_db, _state);
        }

        private SlotLayout Layout(string bench, string size) =>
            SlotLayout.For(_db.ItemDefs[bench].Bench, _db.ContainerDefs[size]);

        [TestCase("pot_small", 15, 3)]
        [TestCase("pot_medium", 10, 2)]
        [TestCase("pot_large", 5, 1)]
        [TestCase("plug_tray", 7, 1)]
        public void ThreeMetreBenchLayout(string size, int cols, int rows)
        {
            var l = Layout("bench_3m", size);
            Assert.AreEqual(cols, l.Cols);
            Assert.AreEqual(rows, l.Rows);
        }

        [TestCase("pot_small", 20, 3)]
        [TestCase("pot_medium", 13, 2)]
        [TestCase("pot_large", 6, 1)]
        [TestCase("plug_tray", 10, 1)]
        public void FourMetreBenchScalesFromData(string size, int cols, int rows)
        {
            var l = Layout("bench_4m", size);
            Assert.AreEqual(cols, l.Cols);
            Assert.AreEqual(rows, l.Rows);
        }

        [Test]
        public void PlaceAndRemove()
        {
            Assert.AreEqual(SlotRejection.None, _occ.PlaceInSlot(_bench3, "pot_small", 0, 0));
            Assert.AreEqual(SlotRejection.SlotOccupied, _occ.CanPlaceInSlot(_bench3, "pot_small", 0, 0));
            Assert.AreEqual(SlotRejection.None, _occ.CanPlaceInSlot(_bench3, "pot_small", 1, 0));
            Assert.AreEqual(SlotRejection.None, _occ.RemoveFromSlot(_bench3, "pot_small", 0, 0));
            Assert.AreEqual(SlotRejection.None, _occ.CanPlaceInSlot(_bench3, "pot_small", 0, 0));
            Assert.AreEqual(SlotRejection.SlotEmpty, _occ.RemoveFromSlot(_bench3, "pot_small", 0, 0));
        }

        [Test]
        public void Rejections()
        {
            Assert.AreEqual(SlotRejection.NotABench, _occ.CanPlaceInSlot(999, "pot_small", 0, 0));
            var tap = _state.Place("tap", ItemLayer.Floor, new Cell(1, 1), Rotation.Deg0, out _).Id;
            Assert.AreEqual(SlotRejection.NotABench, _occ.CanPlaceInSlot(tap, "pot_small", 0, 0));
            Assert.AreEqual(SlotRejection.NoSuchSlot, _occ.CanPlaceInSlot(_bench3, "pot_small", 15, 0));
            Assert.AreEqual(SlotRejection.NoSuchSlot, _occ.CanPlaceInSlot(_bench3, "pot_small", 0, 3));
            Assert.AreEqual(SlotRejection.NoSuchSlot, _occ.CanPlaceInSlot(_bench3, "pot_small", -1, 0));
            Assert.AreEqual(SlotRejection.NoSuchSlot, _occ.CanPlaceInSlot(_bench3, "nope", 0, 0));
        }

        [Test]
        public void MixedSizesBlockWhereUnitsOverlap()
        {
            // small col 1 covers u 2..3; medium col 0 covers u 0..2 (overlap at u=2), medium col 1 covers u 3..5 (overlap at u=3).
            Assert.AreEqual(SlotRejection.None, _occ.PlaceInSlot(_bench3, "pot_small", 1, 0));
            Assert.AreEqual(SlotRejection.SlotOccupied, _occ.CanPlaceInSlot(_bench3, "pot_medium", 0, 0));
            Assert.AreEqual(SlotRejection.SlotOccupied, _occ.CanPlaceInSlot(_bench3, "pot_medium", 1, 0));
            Assert.AreEqual(SlotRejection.None, _occ.CanPlaceInSlot(_bench3, "pot_medium", 2, 0));
            // Occupied the other way round too, and a large pot spans every row of the area.
            Assert.AreEqual(SlotRejection.SlotOccupied, _occ.CanPlaceInSlot(_bench3, "pot_large", 0, 0));
            Assert.AreEqual(SlotRejection.None, _occ.CanPlaceInSlot(_bench3, "pot_large", 1, 0));
        }

        [Test]
        public void RemovingOneSlotKeepsOthers()
        {
            _occ.PlaceInSlot(_bench3, "pot_small", 0, 0);
            _occ.PlaceInSlot(_bench3, "pot_small", 1, 0);
            _occ.RemoveFromSlot(_bench3, "pot_small", 0, 0);
            Assert.AreEqual(SlotRejection.SlotOccupied, _occ.CanPlaceInSlot(_bench3, "pot_small", 1, 0));
            Assert.AreEqual(SlotRejection.SlotEmpty, _occ.CanRemoveFromSlot(_bench3, "pot_medium", 0, 0));
        }

        [Test]
        public void BenchesAreIndependent()
        {
            _occ.PlaceInSlot(_bench3, "pot_small", 0, 0);
            Assert.AreEqual(SlotRejection.None, _occ.CanPlaceInSlot(_bench4, "pot_small", 0, 0));
        }

        [Test]
        public void QueriesNeverChangeState()
        {
            _occ.CanPlaceInSlot(_bench3, "pot_small", 0, 0);
            Assert.IsFalse(_occ.IsUnitUsed(_bench3, 0, 0));
            _occ.PlaceInSlot(_bench3, "pot_small", 0, 0);
            _occ.CanRemoveFromSlot(_bench3, "pot_small", 0, 0);
            _occ.CanPlaceInSlot(_bench3, "pot_small", 0, 0);
            Assert.IsTrue(_occ.IsUnitUsed(_bench3, 0, 0));
            Assert.AreEqual(SlotRejection.None, _occ.CanRemoveFromSlot(_bench3, "pot_small", 0, 0));
        }
    }
}
