using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class CarryTrayTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _world;
        private BuildState _build;
        private SlotOccupancy _slots;
        private HandsV2 _hands;
        private SlotContents _contents;
        private EntityFactory _factory;
        private int _bench;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _world = new SimWorld();
            _build = new BuildState(_db);
            _build.Inventory.Add("bench_3m");
            _bench = _build.Place("bench_3m", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _).Id;
            _slots = new SlotOccupancy(_db, _build);
            _hands = new HandsV2(_db, new StorageCrate());
            _factory = new EntityFactory(_world, _db);
            _contents = new SlotContents(_slots, new StripOccupancy(_db, _build), _hands, _db);
            _contents.Register(_world);
        }

        private static SlotAddress Pot(int bench, int col) => SlotAddress.Planting(bench, "pot_small", col, 0);

        private Container PotInSlot(int col)
        {
            var c = _factory.CreateContainer("pot_small");
            Run(new PutInSlotAfterGive(c, Pot(_bench, col)));
            return c;
        }

        // helper: give pot to hand then put it in slot
        private sealed class PutInSlotAfterGive
        {
            public Container C; public SlotAddress A;
            public PutInSlotAfterGive(Container c, SlotAddress a) { C = c; A = a; }
        }

        private void Run(PutInSlotAfterGive g)
        {
            _hands.TryGive(A, HeldThing.OfContainer(g.C));
            _world.Submit(new PutInSlot(A, g.A));
            _world.Step();
        }

        private void HoldTray() => _hands.TryGive(A, HeldThing.Fresh(_db.ItemDefs["carry_tray"]));

        private void Do(ICommand cmd) { _world.Submit(cmd); _world.Step(); }

        [Test]
        public void TakePotsIntoTrayUpToSixThenTrayFull()
        {
            for (var i = 0; i < 7; i++) PotInSlot(i);
            HoldTray();
            for (var i = 0; i < 6; i++)
            {
                Assert.IsTrue(_world.Validate(new TakePotIntoTray(A, Pot(_bench, i))).IsAccepted);
                Do(new TakePotIntoTray(A, Pot(_bench, i)));
            }
            Assert.AreEqual(6, _hands.GetHeld(A).Pots.Count);
            Assert.IsNull(_contents.Get(Pot(_bench, 0)));
            var r = _world.Validate(new TakePotIntoTray(A, Pot(_bench, 6)));
            Assert.IsFalse(r.IsAccepted);
            Assert.AreEqual("TrayFull", r.Message);
            Assert.IsNotNull(_contents.Get(Pot(_bench, 6)));
        }

        [Test]
        public void TakeNeedsTrayAndPot()
        {
            PotInSlot(0);
            Assert.AreEqual("NoCarryTray", _world.Validate(new TakePotIntoTray(A, Pot(_bench, 0))).Message);
            HoldTray();
            Assert.AreEqual("SlotEmpty", _world.Validate(new TakePotIntoTray(A, Pot(_bench, 3))).Message);
        }

        [Test]
        public void PutOnePotFromTrayIntoEmptySlot()
        {
            var pot = PotInSlot(0);
            HoldTray();
            Do(new TakePotIntoTray(A, Pot(_bench, 0)));
            Do(new PutPotFromTray(A, Pot(_bench, 4)));
            Assert.AreEqual(0, _hands.GetHeld(A).Pots.Count);
            Assert.AreSame(pot, _contents.Get(Pot(_bench, 4)).Container);
            Assert.AreEqual("TrayEmpty", _world.Validate(new PutPotFromTray(A, Pot(_bench, 5))).Message);
        }

        [Test]
        public void PutIntoOccupiedSlotRejected()
        {
            PotInSlot(0); PotInSlot(1);
            HoldTray();
            Do(new TakePotIntoTray(A, Pot(_bench, 0)));
            Assert.AreEqual("SlotOccupied", _world.Validate(new PutPotFromTray(A, Pot(_bench, 1))).Message);
        }

        [Test]
        public void TrayWithPotsSetsDownOnStripAndComesBackWithPotsIntact()
        {
            var pot = PotInSlot(0);
            pot.WaterMeter = 70f;
            pot.AddPlant(42);
            HoldTray();
            Do(new TakePotIntoTray(A, Pot(_bench, 0)));
            var strip = SlotAddress.Strip(_bench, "carry_tray", 0, 0);
            Assert.IsTrue(_world.Validate(new PutInSlot(A, strip)).IsAccepted);
            Do(new PutInSlot(A, strip));
            Assert.IsNull(_hands.GetHeld(A));
            Assert.AreEqual(1, _contents.Get(strip).Pots.Count);

            Do(new PickUpFromSlot(A, strip));
            var tray = _hands.GetHeld(A);
            Assert.AreEqual(HeldKind.CarryTray, tray.Kind);
            Assert.AreSame(pot, tray.Pots[0]);
            Assert.AreEqual(70f, tray.Pots[0].WaterMeter);
            Assert.AreEqual(new[] { 42 }, tray.Pots[0].PlantIds);
        }

        [Test]
        public void PotsInTrayAreNeverNeighbours()
        {
            PotInSlot(0); PotInSlot(1);
            var neighbours = new Neighbours(_db, _build, _slots);
            Assert.AreEqual(1, neighbours.Of(new SlotRef(_bench, "pot_small", 0, 0)).Count);

            HoldTray();
            Do(new TakePotIntoTray(A, Pot(_bench, 1)));
            Assert.AreEqual(0, neighbours.Of(new SlotRef(_bench, "pot_small", 0, 0)).Count);
            Assert.AreEqual(0, neighbours.Of(new SlotRef(_bench, "pot_small", 1, 0)).Count);

            // Set down on the strip with a pot inside: still not neighbours.
            Do(new PutInSlot(A, SlotAddress.Strip(_bench, "carry_tray", 0, 0)));
            Assert.AreEqual(0, neighbours.Of(new SlotRef(_bench, "pot_small", 0, 0)).Count);
        }
    }
}
