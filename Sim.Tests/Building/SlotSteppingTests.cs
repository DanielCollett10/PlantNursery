using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class SlotSteppingTests
    {
        private ContentDb _db;
        private BuildState _state;
        private SlotOccupancy _occ;
        private StripOccupancy _strip;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _state = new BuildState(_db);
            _state.Inventory.Add("bench_3m", 4);
            _occ = new SlotOccupancy(_db, _state);
            _strip = new StripOccupancy(_db, _state);
        }

        private PlacedItem Bench(int x, int z, Rotation r)
        {
            var p = _state.Place("bench_3m", ItemLayer.Floor, new Cell(x, z), r, out var rej);
            Assert.AreEqual(BuildRejection.None, rej);
            return p;
        }

        private SlotStepper Stepper(string held, Rotation view = Rotation.Deg0) => new SlotStepper(_db, _state, held, view);

        private static SlotAddress P(int bench, string size, int c, int r) => SlotAddress.Planting(bench, size, c, r);
        private static SlotAddress S(int bench, string item, int c, int r) => SlotAddress.Strip(bench, item, c, r);

        private static void AreSame(SlotAddress? expected, SlotAddress? actual)
        {
            Assert.AreEqual(expected.HasValue, actual.HasValue, "has value");
            if (expected.HasValue) Assert.AreEqual(expected.Value.Key, actual.Value.Key);
        }

        // ---------- SlotPointer ----------

        [Test]
        public void PointerPlantingSlotsForHeldSize()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            // planting area spans v 0.4..1.0 m; small pots are 0.2 m.
            AreSame(P(b.Id, "pot_small", 0, 0), SlotPointer.SlotAt(_db, b, 0.05, 0.45, "pot_small"));
            AreSame(P(b.Id, "pot_small", 14, 2), SlotPointer.SlotAt(_db, b, 2.95, 0.95, "pot_small"));
            AreSame(P(b.Id, "pot_small", 7, 1), SlotPointer.SlotAt(_db, b, 1.5, 0.65, "pot_small"));
            AreSame(P(b.Id, "pot_medium", 9, 1), SlotPointer.SlotAt(_db, b, 2.95, 0.95, "pot_medium"));
            AreSame(P(b.Id, "pot_large", 4, 0), SlotPointer.SlotAt(_db, b, 2.95, 0.41, "pot_large"));
        }

        [Test]
        public void PointerPlugTrayLongSideAcross()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            AreSame(P(b.Id, "plug_tray", 6, 0), SlotPointer.SlotAt(_db, b, 2.55, 0.5, "plug_tray"));
        }

        [Test]
        public void PointerStripSlots()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            AreSame(S(b.Id, "pot_small", 0, 0), SlotPointer.SlotAt(_db, b, 0.0, 0.0, "pot_small"));
            AreSame(S(b.Id, "pot_small", 14, 1), SlotPointer.SlotAt(_db, b, 2.99, 0.39, "pot_small"));
            AreSame(S(b.Id, "watering_can", 9, 0), SlotPointer.SlotAt(_db, b, 2.99, 0.2, "watering_can"));
            AreSame(S(b.Id, "carry_tray", 4, 0), SlotPointer.SlotAt(_db, b, 2.99, 0.2, "carry_tray"));
        }

        [Test]
        public void PointerOffBenchAndUnacceptedAreNull()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            Assert.IsNull(SlotPointer.SlotAt(_db, b, -0.01, 0.5, "pot_small"));
            Assert.IsNull(SlotPointer.SlotAt(_db, b, 3.0, 0.5, "pot_small"));
            Assert.IsNull(SlotPointer.SlotAt(_db, b, 1.0, 1.0, "pot_small"));
            Assert.IsNull(SlotPointer.SlotAt(_db, b, 1.0, 0.2, "grub_sack"), "strip does not accept it");
        }

        [Test]
        public void PointerEmptyHandedDefaultsToSmallestThenFollowsOccupied()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            AreSame(P(b.Id, "pot_small", 7, 1), SlotPointer.SlotAt(_db, b, 1.5, 0.65, null));
            AreSame(S(b.Id, "pot_small", 7, 0), SlotPointer.SlotAt(_db, b, 1.5, 0.05, null));
            Assert.AreEqual(SlotRejection.None, _occ.PlaceInSlot(b.Id, "pot_medium", 4, 1));
            AreSame(P(b.Id, "pot_medium", 4, 1), SlotPointer.SlotAt(_db, b, 1.3, 0.7, null, _occ, _strip));
            AreSame(P(b.Id, "pot_small", 0, 0), SlotPointer.SlotAt(_db, b, 0.1, 0.5, null, _occ, _strip));
            Assert.AreEqual(StripRejection.None, _strip.Place(b.Id, "watering_can", 3, 0, StripContents.NoPlants));
            AreSame(S(b.Id, "watering_can", 3, 0), SlotPointer.SlotAt(_db, b, 1.0, 0.1, null, null, _strip));
        }

        [Test]
        public void PointerNonContainerHeldInPlantingUsesAnySize()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            Assert.AreEqual(SlotRejection.None, _occ.PlaceInSlot(b.Id, "pot_large", 2, 0));
            AreSame(P(b.Id, "pot_large", 2, 0), SlotPointer.SlotAt(_db, b, 1.5, 0.5, "watering_can", _occ));
        }

        // ---------- SlotStepper on one 3 m bench ----------

        [Test]
        public void StepAlongRowStopsAtEnds()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            var st = Stepper("pot_small");
            AreSame(P(b.Id, "pot_small", 1, 1), st.Step(P(b.Id, "pot_small", 0, 1), StepDirection.Right));
            AreSame(P(b.Id, "pot_small", 0, 1), st.Step(P(b.Id, "pot_small", 1, 1), StepDirection.Left));
            Assert.IsNull(st.Step(P(b.Id, "pot_small", 0, 1), StepDirection.Left));
            Assert.IsNull(st.Step(P(b.Id, "pot_small", 14, 1), StepDirection.Right));
        }

        [Test]
        public void StepAcrossRowsAndIntoStripAndBack()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            var st = Stepper("pot_small");
            AreSame(P(b.Id, "pot_small", 5, 2), st.Step(P(b.Id, "pot_small", 5, 1), StepDirection.Up));
            Assert.IsNull(st.Step(P(b.Id, "pot_small", 5, 2), StepDirection.Up), "no bench behind");
            AreSame(P(b.Id, "pot_small", 5, 0), st.Step(P(b.Id, "pot_small", 5, 1), StepDirection.Down));
            // front row down -> strip row nearest the planting area (2 rows of small strip slots, so row 1)
            AreSame(S(b.Id, "pot_small", 5, 1), st.Step(P(b.Id, "pot_small", 5, 0), StepDirection.Down));
            AreSame(S(b.Id, "pot_small", 5, 0), st.Step(S(b.Id, "pot_small", 5, 1), StepDirection.Down));
            Assert.IsNull(st.Step(S(b.Id, "pot_small", 5, 0), StepDirection.Down));
            AreSame(S(b.Id, "pot_small", 5, 1), st.Step(S(b.Id, "pot_small", 5, 0), StepDirection.Up));
            AreSame(P(b.Id, "pot_small", 5, 0), st.Step(S(b.Id, "pot_small", 5, 1), StepDirection.Up));
            AreSame(S(b.Id, "pot_small", 6, 0), st.Step(S(b.Id, "pot_small", 5, 0), StepDirection.Right));
            Assert.IsNull(st.Step(S(b.Id, "pot_small", 14, 0), StepDirection.Right));
        }

        [Test]
        public void StripColumnFollowsSlotCentreWhenSizesDiffer()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            // large pot col 4 covers u 24..30; strip for watering can (3 wide) -> centre 27 -> col 9
            var st = Stepper("watering_can");
            AreSame(S(b.Id, "watering_can", 9, 0), st.Step(P(b.Id, "pot_large", 4, 0), StepDirection.Down));
            AreSame(P(b.Id, "pot_small", 13, 0), Stepper(null).Step(S(b.Id, "pot_small", 13, 1), StepDirection.Up));
            // medium pot strip col 9 (u 27..30) -> planting medium col 9
            AreSame(P(b.Id, "pot_medium", 9, 0), Stepper("pot_medium").Step(S(b.Id, "pot_medium", 9, 0), StepDirection.Up));
            // plug tray: planting col 7 (u 28..32) is 4 wide; strip tray 6 wide -> col 4
            AreSame(S(b.Id, "plug_tray", 4, 0), Stepper("plug_tray").Step(P(b.Id, "plug_tray", 6, 0), StepDirection.Down));
        }

        [Test]
        public void HeldItemNotOnStripCannotEnterIt()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            Assert.IsNull(Stepper("grub_sack").Step(P(b.Id, "pot_small", 0, 0), StepDirection.Down));
        }

        [Test]
        public void EmptyHandedStaysInTheSizeOfTheCurrentSlot()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            var st = Stepper(null);
            AreSame(P(b.Id, "pot_medium", 4, 1), st.Step(P(b.Id, "pot_medium", 3, 1), StepDirection.Right));
            AreSame(S(b.Id, "pot_medium", 4, 0), st.Step(P(b.Id, "pot_medium", 4, 0), StepDirection.Down));
        }

        // ---------- back-to-back pair ----------

        private PlacedItem[] Pair() => new[] { Bench(10, 10, Rotation.Deg0), Bench(10, 12, Rotation.Deg180) };

        [Test]
        public void UpFromBackRowContinuesIntoFacingBench()
        {
            var pair = Pair(); var a = pair[0].Id; var b = pair[1].Id;
            var st = Stepper("pot_small");
            // a col 14 (u 28..29) faces b's local u 1 -> col 0
            AreSame(P(b, "pot_small", 0, 2), st.Step(P(a, "pot_small", 14, 2), StepDirection.Up));
            AreSame(P(b, "pot_small", 14, 2), st.Step(P(a, "pot_small", 0, 2), StepDirection.Up));
            AreSame(P(b, "pot_small", 7, 2), st.Step(P(a, "pot_small", 7, 2), StepDirection.Up));
            // not from a middle row
            AreSame(P(a, "pot_small", 7, 2), st.Step(P(a, "pot_small", 7, 1), StepDirection.Up));
        }

        [Test]
        public void FacingBenchIsSteppedInScreenFrame()
        {
            var pair = Pair(); var a = pair[0].Id; var b = pair[1].Id;
            var st = Stepper("pot_small", Rotation.Deg0);
            // b is turned 180 on screen: its back row is nearest a, screen up goes toward its front, left/right are mirrored
            AreSame(P(b, "pot_small", 0, 1), st.Step(P(b, "pot_small", 0, 2), StepDirection.Up));
            AreSame(P(b, "pot_small", 0, 2), st.Step(P(b, "pot_small", 0, 1), StepDirection.Down));
            AreSame(P(a, "pot_small", 14, 2), st.Step(P(b, "pot_small", 0, 2), StepDirection.Down));
            AreSame(P(b, "pot_small", 1, 2), st.Step(P(b, "pot_small", 0, 2), StepDirection.Left));
            AreSame(P(b, "pot_small", 0, 2), st.Step(P(b, "pot_small", 1, 2), StepDirection.Right));
            // from b's strip (its front is at the top of the screen): down goes back toward the planting rows
            AreSame(P(b, "pot_small", 14, 0), st.Step(S(b, "pot_small", 14, 1), StepDirection.Down));
            AreSame(S(b, "pot_small", 14, 1), st.Step(P(b, "pot_small", 14, 0), StepDirection.Up));
        }

        [Test]
        public void MixedSizesAcrossTheJoin()
        {
            var pair = Pair(); var a = pair[0].Id; var b = pair[1].Id;
            // a col 14 small (u 28..29) -> facing medium col 0 (b local u 0..2 contains 1)
            AreSame(P(b, "pot_medium", 0, 1), Stepper("pot_medium").Step(P(a, "pot_medium", 9, 1), StepDirection.Up));
            // large across the join: a large col 4 (u 24..29) faces b local 0..5 -> col 0
            AreSame(P(b, "pot_large", 0, 0), Stepper("pot_large").Step(P(a, "pot_large", 4, 0), StepDirection.Up));
        }

        [Test]
        public void GapOrBackToFrontGivesNoJoin()
        {
            var a = Bench(10, 10, Rotation.Deg0).Id;
            Bench(10, 13, Rotation.Deg180);
            Assert.IsNull(Stepper("pot_small").Step(P(a, "pot_small", 3, 2), StepDirection.Up));
            _state.Inventory.Add("bench_3m", 1);
            var c = Bench(30, 10, Rotation.Deg0).Id;
            Bench(30, 12, Rotation.Deg0);
            Assert.IsNull(Stepper("pot_small").Step(P(c, "pot_small", 3, 2), StepDirection.Up));
        }

        [Test]
        public void RotatedPairJoinsToo()
        {
            var a = Bench(10, 10, Rotation.Deg90).Id;
            var b = Bench(12, 10, Rotation.Deg270).Id;
            // a (90): back faces east; view oriented to a -> screen up. a col 14 faces b col 0 (from NeighboursTests)
            var st = Stepper("pot_small", Rotation.Deg90);
            AreSame(P(b, "pot_small", 0, 2), st.Step(P(a, "pot_small", 14, 2), StepDirection.Up));
            AreSame(P(b, "pot_small", 14, 2), st.Step(P(a, "pot_small", 0, 2), StepDirection.Up));
        }

        // ---------- Start ----------

        [Test]
        public void StartPicksNearestSlot()
        {
            var b = Bench(10, 10, Rotation.Deg0).Id;
            // bench spans x 5.0..8.0 m, z 5.0..6.0 m; front (south) edge is z = 5.0
            var st = Stepper("pot_small");
            AreSame(S(b, "pot_small", 0, 0), st.Start(new[] { b }, 5.05, 4.9));
            AreSame(S(b, "pot_small", 14, 0), st.Start(new[] { b }, 7.95, 4.5));
            AreSame(S(b, "pot_small", 7, 0), st.Start(new[] { b }, 6.55, 5.0));
            AreSame(P(b, "pot_small", 7, 2), st.Start(new[] { b }, 6.55, 6.5));
        }

        [Test]
        public void StartWithItemNotOnStripPicksPlantingSlot()
        {
            var b = Bench(10, 10, Rotation.Deg0).Id;
            AreSame(S(b, "pot_medium", 0, 0), Stepper("pot_medium").Start(new[] { b }, 5.0, 4.0));
            AreSame(P(b, "pot_small", 0, 0), Stepper("grub_sack").Start(new[] { b }, 5.0, 4.0));
        }

        [Test]
        public void StartChoosesNearestBenchOfAPair()
        {
            var pair = Pair(); var a = pair[0].Id; var b = pair[1].Id;
            // b spans z 6.0..7.0 m with its front (strip) to the north at z = 7.0
            var st = Stepper("pot_small");
            var fromA = st.Start(new[] { a, b }, 6.5, 4.8);
            Assert.AreEqual(a, fromA.Value.Slot.BenchId);
            Assert.IsTrue(fromA.Value.IsStrip);
            var fromB = st.Start(new[] { a, b }, 6.5, 7.2);
            Assert.AreEqual(b, fromB.Value.Slot.BenchId);
            Assert.IsTrue(fromB.Value.IsStrip);
            Assert.IsNull(st.Start(new int[0], 0, 0));
        }
    }
}
