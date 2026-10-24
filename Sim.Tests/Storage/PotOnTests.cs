using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class PotOnTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private static readonly PlayerId B = new PlayerId(2);
        private ContentDb _db;
        private SimWorld _world;
        private HandsV2 _hands;
        private StorageCrate _crate;
        private SlotContents _contents;
        private EntityFactory _factory;
        private SpeciesPlantFactory _plantFactory;
        private Dictionary<int, Plant> _plants;
        private BuildState _build;
        private int _benches;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _world = new SimWorld();
            _build = new BuildState(_db);
            _benches = 0;
            _crate = new StorageCrate();
            _hands = new HandsV2(_db, _crate);
            _contents = new SlotContents(new SlotOccupancy(_db, _build), new StripOccupancy(_db, _build), _hands, _db);
            _contents.Register(_world);
            _factory = new EntityFactory(_world, _db);
            _plantFactory = new SpeciesPlantFactory(_world, _db);
            _plants = new Dictionary<int, Plant>();
            _contents.RegisterPotOn(_world, _crate, _plants);
        }

        private int NewBench()
        {
            _build.Inventory.Add("bench_3m");
            var bench = _build.Place("bench_3m", ItemLayer.Floor, new Cell(10, 10 + 3 * _benches++), Rotation.Deg0, out var why);
            Assert.IsNotNull(bench, why.ToString());
            return bench.Id;
        }

        private SlotAddress PutPot(string containerId, bool strip, PlayerId p, out Container c)
        {
            c = _factory.CreateContainer(containerId);
            var bench = NewBench();
            var addr = strip ? SlotAddress.Strip(bench, containerId, 0, 0) : SlotAddress.Planting(bench, containerId, 0, 0);
            _hands.TryGive(p, HeldThing.OfContainer(c));
            _world.Submit(new PutInSlot(p, addr));
            _world.Step();
            Assert.IsNull(_hands.GetHeld(p));
            return addr;
        }

        private Plant AddPlant(Container c, string species, string variety, int cell, bool ready)
        {
            var p = _plantFactory.Create(species, variety, c.Def.Id, cell);
            p.StageProgress = ready ? 1f : 0.2f;
            p.Health = 77; p.TicksAlive = 5; p.TicksNotDry = 4; p.StageIndex = 0;
            c.AddPlant(p.Id);
            _plants[p.Id] = p;
            return p;
        }

        private Container Hold(string containerId) => Hold(containerId, A);

        private Container Hold(string containerId, PlayerId p)
        {
            var c = _factory.CreateContainer(containerId);
            _hands.TryGive(p, HeldThing.OfContainer(c));
            return c;
        }

        private CommandResult Check(SlotAddress t, int cell = -1) => _world.Validate(new PotOn(A, t, cell));
        private void Run(SlotAddress t, int cell = -1) { _world.Submit(new PotOn(A, t, cell)); _world.Step(); }

        [Test]
        public void IntoPlantingSlotMovesPlantAndSendsOldPotToCrate()
        {
            var target = PutPot("pot_medium", false, A, out var newPot);
            var old = Hold("pot_small");
            var plant = AddPlant(old, "chimebell", "chimebell_white", 0, true);
            _world.DrainEvents();
            Run(target);
            CollectionAssert.AreEqual(new[] { plant.Id }, newPot.PlantIds);
            Assert.AreEqual(0, old.PlantIds.Count);
            Assert.AreEqual("pot_medium", plant.ContainerId);
            Assert.AreEqual(77, plant.Health);
            Assert.AreEqual(5, plant.TicksAlive);
            Assert.AreSame(old, _crate.FindContainer(old.Id));
            Assert.IsNull(_hands.GetHeld(A));
            Assert.AreSame(newPot, _contents.Get(target).Container);
            Assert.AreEqual(1, _world.DrainEvents().OfType<PottedOn>().Count());
        }

        [Test]
        public void OntoStripPutsNewPotInHands()
        {
            var target = PutPot("pot_medium", true, A, out var newPot);
            var old = Hold("pot_small");
            var plant = AddPlant(old, "chimebell", "chimebell_white", 0, true);
            Run(target);
            Assert.AreSame(newPot, _hands.GetHeld(A).Container);
            CollectionAssert.AreEqual(new[] { plant.Id }, newPot.PlantIds);
            Assert.AreSame(old, _crate.FindContainer(old.Id));
            Assert.IsNull(_contents.Get(target));
        }

        [Test]
        public void FromPlugTrayFirstReadyCellMoves()
        {
            var target = PutPot("pot_small", false, A, out var pot);
            var tray = Hold("plug_tray");
            AddPlant(tray, "chimebell", "chimebell_white", 0, false);
            var ready = AddPlant(tray, "chimebell", "chimebell_white", 1, true);
            AddPlant(tray, "chimebell", "chimebell_white", 2, true);
            Run(target);
            CollectionAssert.AreEqual(new[] { ready.Id }, pot.PlantIds);
            Assert.AreEqual(2, tray.PlantIds.Count);
            Assert.AreSame(tray, _hands.GetHeld(A).Container, "tray with plants left stays in hand");
            Assert.IsNull(_crate.FindContainer(tray.Id));
        }

        [Test]
        public void FromPlugTrayChosenCellMoves()
        {
            var target = PutPot("pot_small", false, A, out var pot);
            var tray = Hold("plug_tray");
            AddPlant(tray, "chimebell", "chimebell_white", 0, true);
            var chosen = AddPlant(tray, "chimebell", "chimebell_white", 3, true);
            Run(target, 3);
            CollectionAssert.AreEqual(new[] { chosen.Id }, pot.PlantIds);
        }

        [Test]
        public void LastPlugTrayPlantSendsTrayToCrate()
        {
            var target = PutPot("pot_small", false, A, out _);
            var tray = Hold("plug_tray");
            AddPlant(tray, "chimebell", "chimebell_white", 0, true);
            Run(target);
            Assert.AreSame(tray, _crate.FindContainer(tray.Id));
            Assert.IsNull(_hands.GetHeld(A));
        }

        [Test]
        public void Rejections()
        {
            var full = PutPot("pot_medium", false, A, out var fullPot);
            AddPlant(fullPot, "chimebell", "chimebell_white", 0, true);
            var sameSize = PutPot("pot_small", false, A, out _);
            var large = PutPot("pot_large", false, A, out _);
            var medium = PutPot("pot_medium", false, A, out _);
            var small = Hold("pot_small");
            AddPlant(small, "chimebell", "chimebell_white", 0, true);
            Assert.AreEqual("PotNotEmpty", Check(full).Message);
            Assert.AreEqual("PotTooSmall", Check(sameSize).Message);
            Assert.AreEqual("PotTooBig", Check(large).Message);
            _plants[small.PlantIds[0]].StageProgress = 0.5f;
            Assert.AreEqual("NotReady", Check(medium).Message);
        }

        [Test]
        public void PlugTrayWithNoReadyCellIsNotReady()
        {
            var target = PutPot("pot_small", false, A, out _);
            var tray = Hold("plug_tray");
            AddPlant(tray, "chimebell", "chimebell_white", 0, false);
            Assert.AreEqual("NotReady", Check(target).Message);
            AddPlant(tray, "chimebell", "chimebell_white", 1, true);
            Assert.AreEqual("NotReady", Check(target, 0).Message);
            Assert.IsTrue(Check(target, 1).IsAccepted);
        }

        [Test]
        public void SecondPlayerToSameTargetIsTaken()
        {
            var target = PutPot("pot_medium", false, A, out _);
            var a = Hold("pot_small", A);
            AddPlant(a, "chimebell", "chimebell_white", 0, true);
            var b = Hold("pot_small", B);
            AddPlant(b, "chimebell", "chimebell_white", 0, true);
            _world.Submit(new PotOn(A, target));
            _world.Submit(new PotOn(B, target));
            _world.Step();
            var rejected = _world.DrainEvents().OfType<CommandRejectedEvent>().ToList();
            Assert.AreEqual(1, rejected.Count);
            Assert.AreEqual("Taken", rejected[0].Result.Message);
        }
    }
}
