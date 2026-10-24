using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class StripTests
    {
        private ContentDb _db;
        private BuildState _state;
        private StripOccupancy _strip;
        private int _bench3;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _state = new BuildState(_db);
            _state.Inventory.Add("bench_3m");
            _bench3 = _state.Place("bench_3m", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _).Id;
            _strip = new StripOccupancy(_db, _state);
        }

        [TestCase("pot_small", 15, 2)]
        [TestCase("pot_medium", 10, 1)]
        [TestCase("plug_tray", 5, 1)]
        [TestCase("carry_tray", 5, 1)]
        [TestCase("watering_can", 10, 1)]
        [TestCase("bundle_hearthmoss", 15, 2)]
        public void ThreeMetreStripGrid(string item, int cols, int rows)
        {
            var bench = _db.ItemDefs["bench_3m"].Bench;
            Assert.AreEqual(cols, _strip.Cols(bench, item));
            Assert.AreEqual(rows, _strip.Rows(bench, item));
            Assert.AreEqual(StripRejection.None, _strip.CanPlace(_bench3, item, cols - 1, rows - 1, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.NoSuchSlot, _strip.CanPlace(_bench3, item, cols, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.NoSuchSlot, _strip.CanPlace(_bench3, item, 0, rows, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.NoSuchSlot, _strip.CanPlace(_bench3, item, -1, 0, StripContents.NoPlants));
        }

        [Test]
        public void PlaceRemoveAndOccupied()
        {
            Assert.AreEqual(StripRejection.None, _strip.Place(_bench3, "pot_small", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.SlotOccupied, _strip.CanPlace(_bench3, "pot_small", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.None, _strip.CanPlace(_bench3, "pot_small", 1, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.None, _strip.CanPlace(_bench3, "pot_small", 0, 1, StripContents.NoPlants));
            // A medium pot at col 0 covers units 0..2, overlapping the small pot at (0,0).
            Assert.AreEqual(StripRejection.SlotOccupied, _strip.CanPlace(_bench3, "pot_medium", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.None, _strip.Remove(_bench3, "pot_small", 0, 0));
            Assert.AreEqual(StripRejection.None, _strip.CanPlace(_bench3, "pot_medium", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.SlotEmpty, _strip.Remove(_bench3, "pot_small", 0, 0));
        }

        [Test]
        public void Rejections()
        {
            Assert.AreEqual(StripRejection.NotABench, _strip.CanPlace(999, "pot_small", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.NotAccepted, _strip.CanPlace(_bench3, "pot_large", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.NotAccepted, _strip.CanPlace(_bench3, "tap", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.NoPlantsOnStrip, _strip.CanPlace(_bench3, "pot_small", 0, 0, StripContents.HoldsPlant));
            Assert.AreEqual(StripRejection.NoPlantsOnStrip, _strip.CanPlace(_bench3, "plug_tray", 0, 0, StripContents.HoldsPlant));
            Assert.AreEqual(StripRejection.NoPlantsOnStrip, _strip.CanPlace(_bench3, "carry_tray", 0, 0, StripContents.HoldsPlant));
            Assert.AreEqual(StripRejection.NoPlantsOnStrip, _strip.CanPlace(_bench3, "pot_small", 0, 0, StripContents.CarryTrayWithPots));
        }

        [Test]
        public void CarryTrayAcceptedWithItsPots()
        {
            Assert.AreEqual(StripRejection.None, _strip.Place(_bench3, "carry_tray", 0, 0, StripContents.CarryTrayWithPots));
            Assert.AreEqual(StripRejection.SlotOccupied, _strip.CanPlace(_bench3, "carry_tray", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(StripRejection.None, _strip.CanPlace(_bench3, "carry_tray", 1, 0, StripContents.CarryTrayWithPots));
        }

        [Test]
        public void ZeroDepthStripHasNoSlots()
        {
            _db.ItemDefs["bench_3m"].Bench.StripDepthUnits = 0;
            Assert.AreEqual(0, _strip.Cols(_db.ItemDefs["bench_3m"].Bench, "pot_small"));
            Assert.AreEqual(StripRejection.NoSuchSlot, _strip.CanPlace(_bench3, "pot_small", 0, 0, StripContents.NoPlants));
        }
    }
}
