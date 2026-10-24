using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class SlotCommandsTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private static readonly PlayerId B = new PlayerId(2);
        private ContentDb _db;
        private SimWorld _world;
        private BuildState _build;
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
            var crate = new StorageCrate();
            _hands = new HandsV2(_db, crate);
            _factory = new EntityFactory(_world, _db);
            _contents = new SlotContents(new SlotOccupancy(_db, _build), new StripOccupancy(_db, _build), _hands);
            _contents.Register(_world);
        }

        private Container Hold(PlayerId p, string container)
        {
            var c = _factory.CreateContainer(container);
            _hands.TryGive(p, HeldThing.OfContainer(c));
            return c;
        }

        private static SlotAddress Pot(int bench, int col = 0) => SlotAddress.Planting(bench, "pot_small", col, 0);

        [Test]
        public void PutThenPickUpPlantingSlot()
        {
            var pot = Hold(A, "pot_small");
            _world.Submit(new PutInSlot(A, Pot(_bench)));
            _world.Step();
            Assert.IsNull(_hands.GetHeld(A));
            Assert.AreSame(pot, _contents.Get(Pot(_bench)).Container);

            _world.Submit(new PickUpFromSlot(B, Pot(_bench)));
            _world.Step();
            Assert.AreSame(pot, _hands.GetHeld(B).Container);
            Assert.IsNull(_contents.Get(Pot(_bench)));
        }

        [Test]
        public void PlantingSlotRejectsWrongSizeAndOccupied()
        {
            Hold(A, "pot_medium");
            Assert.AreEqual("WrongSize", _world.Validate(new PutInSlot(A, Pot(_bench))).Message);
            _hands.TakeHeld(A);
            Hold(A, "pot_small");
            _world.Submit(new PutInSlot(A, Pot(_bench)));
            _world.Step();
            Hold(A, "pot_small");
            Assert.AreEqual("SlotOccupied", _world.Validate(new PutInSlot(A, Pot(_bench))).Message);
            Assert.AreEqual("HandsFull", _world.Validate(new PickUpFromSlot(A, Pot(_bench))).Message);
        }

        [Test]
        public void PickUpFromEmptySlotRejected()
        {
            var r = _world.Validate(new PickUpFromSlot(A, Pot(_bench)));
            Assert.IsFalse(r.IsAccepted);
            Assert.AreEqual("SlotEmpty", r.Message);
            Assert.AreEqual("NothingHeld", _world.Validate(new PutInSlot(A, Pot(_bench))).Message);
        }

        [Test]
        public void StripAcceptsEmptyPotButNotPlantedOne()
        {
            var strip = SlotAddress.Strip(_bench, "pot_small", 0, 0);
            var pot = Hold(A, "pot_small");
            Assert.IsTrue(_world.Validate(new PutInSlot(A, strip)).IsAccepted);
            pot.AddPlant(99);
            Assert.AreEqual("NoPlantsOnStrip", _world.Validate(new PutInSlot(A, strip)).Message);
            pot.RemovePlant(99);

            _world.Submit(new PutInSlot(A, strip));
            _world.Step();
            Assert.AreSame(pot, _contents.Get(strip).Container);
            _world.Submit(new PickUpFromSlot(A, strip));
            _world.Step();
            Assert.AreSame(pot, _hands.GetHeld(A).Container);
        }

        [Test]
        public void SecondPlayerSamePotSameTickIsTaken()
        {
            var pot = Hold(A, "pot_small");
            _world.Submit(new PutInSlot(A, Pot(_bench)));
            _world.Step();
            _hands.TakeHeld(B);
            _world.DrainEvents();

            _world.Submit(new PickUpFromSlot(A, Pot(_bench)));
            _world.Submit(new PickUpFromSlot(B, Pot(_bench)));
            _world.Step();

            Assert.AreSame(pot, _hands.GetHeld(A).Container);
            Assert.IsNull(_hands.GetHeld(B));
            var rej = _world.DrainEvents().OfType<CommandRejectedEvent>().Single();
            Assert.AreEqual(RejectReason.Taken, rej.Result.Reason);
        }

        [Test]
        public void SecondPutIntoSameSlotIsTaken()
        {
            Hold(A, "pot_small");
            var second = Hold(B, "pot_small");
            _world.Submit(new PutInSlot(A, Pot(_bench)));
            _world.Submit(new PutInSlot(B, Pot(_bench)));
            _world.Step();
            Assert.AreSame(second, _hands.GetHeld(B).Container);
            Assert.AreEqual(RejectReason.Taken, _world.DrainEvents().OfType<CommandRejectedEvent>().Single().Result.Reason);
        }

        [Test]
        public void UnrelatedFailureIsNotTaken()
        {
            _world.Submit(new PickUpFromSlot(A, Pot(_bench)));
            _world.Step();
            Assert.AreEqual(RejectReason.InvalidTarget, _world.DrainEvents().OfType<CommandRejectedEvent>().Single().Result.Reason);
        }

        [Test]
        public void PendingTargetsListsOthersQueuedClaims()
        {
            _world.Submit(new PickUpFromSlot(A, Pot(_bench, 1)));
            _world.Submit(new PutInSlot(B, Pot(_bench, 2)));
            _world.Submit(new TakeFromCrate(B, "pot_small"));

            CollectionAssert.AreEqual(new[] { Pot(_bench, 1).Key }, _world.PendingTargets(B));
            CollectionAssert.AreEqual(new[] { Pot(_bench, 2).Key }, _world.PendingTargets(A));
            _world.Step();
            Assert.AreEqual(0, _world.PendingTargets(B).Count);
        }
    }
}
