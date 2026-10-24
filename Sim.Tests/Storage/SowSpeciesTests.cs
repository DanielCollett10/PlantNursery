using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class SowSpeciesTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _world;
        private HandsV2 _hands;
        private SlotContents _contents;
        private EntityFactory _factory;
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
            _hands = new HandsV2(_db, new StorageCrate());
            _contents = new SlotContents(new SlotOccupancy(_db, _build), new StripOccupancy(_db, _build), _hands);
            _contents.Register(_world);
            _factory = new EntityFactory(_world, _db);
            _plants = new Dictionary<int, Plant>();
            new SowSystem(_db, _hands, _contents, new SpeciesPlantFactory(_world, _db), _plants).Register(_world);
        }

        private Container Slot(string containerId)
        {
            var c = _factory.CreateContainer(containerId);
            _hands.TryGive(A, HeldThing.OfContainer(c));
            _build.Inventory.Add("bench_3m");
            var bench = _build.Place("bench_3m", ItemLayer.Floor, new Cell(10, 10 + 3 * _benches++), Rotation.Deg0, out var why);
            Assert.IsNotNull(bench, why.ToString());
            _world.Submit(new PutInSlot(A, SlotAddress.Planting(bench.Id, containerId, 0, 0)));
            _world.Step();
            Assert.IsNull(_hands.GetHeld(A));
            return c;
        }

        private HeldThing Hold(string itemId, int? seeds = null)
        {
            var p = HeldThing.Fresh(_db.ItemDefs[itemId]);
            if (seeds.HasValue) p.SeedsLeft = seeds.Value;
            _hands.TryGive(A, p);
            return p;
        }

        private CommandResult Sow(Container c) => _world.Validate(new SowSpecies(A, c.Id));

        private void Run(Container c) { _world.Submit(new SowSpecies(A, c.Id)); _world.Step(); }

        [Test]
        public void ChimebellFillsTrayAndPacketLeavesHands()
        {
            var tray = Slot("plug_tray");
            Hold("seeds_chimebell_white");
            _world.DrainEvents();
            Run(tray);
            Assert.AreEqual(6, tray.PlantIds.Count);
            Assert.IsNull(_hands.GetHeld(A));
            var plants = tray.PlantIds.Select(i => _plants[i]).ToList();
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 6), plants.Select(p => p.CellIndex));
            Assert.IsTrue(plants.All(p => p.VarietyId == "chimebell_white" && p.SpeciesId == "chimebell"));
            Assert.AreEqual(6, _world.DrainEvents().OfType<PlantSown>().Count());
        }

        [Test]
        public void PartialFillWhenPacketRunsOut()
        {
            var tray = Slot("plug_tray");
            var packet = Hold("seeds_chimebell_blue", 4);
            Run(tray);
            Assert.AreEqual(4, tray.PlantIds.Count);
            Assert.IsNull(_hands.GetHeld(A));
            Assert.AreEqual(0, packet.SeedsLeft);
        }

        [Test]
        public void SecondSowFillsOnlyEmptyCellsAndKeepsPacket()
        {
            var tray = Slot("plug_tray");
            Hold("seeds_chimebell_white", 2);
            Run(tray);
            _hands.TakeHeld(A);
            var packet = Hold("seeds_chimebell_white", 10);
            Run(tray);
            Assert.AreEqual(6, tray.PlantIds.Count);
            Assert.AreEqual(6, tray.PlantIds.Select(i => _plants[i].CellIndex).Distinct().Count());
            Assert.AreEqual(6, packet.SeedsLeft);
            Assert.AreSame(packet, _hands.GetHeld(A));
        }

        [Test]
        public void HearthmossSowsOneWholeTrayPlant()
        {
            var tray = Slot("plug_tray");
            Hold("spores_hearthmoss_amber");
            Run(tray);
            Assert.AreEqual(1, tray.PlantIds.Count);
            Assert.AreEqual("hearthmoss_amber", _plants[tray.PlantIds[0]].VarietyId);
            Assert.IsNull(_hands.GetHeld(A));
        }

        [Test]
        public void PotGetsOnePlant()
        {
            var pot = Slot("pot_small");
            var packet = Hold("seeds_chimebell_white");
            Run(pot);
            Assert.AreEqual(1, pot.PlantIds.Count);
            Assert.AreEqual(5, packet.SeedsLeft);
            Assert.AreEqual("ContainerFull", Sow(pot).Message);
        }

        [Test]
        public void Rejections()
        {
            var tray = Slot("plug_tray");
            var medium = Slot("pot_medium");
            var small = Slot("pot_small");
            var loose = _factory.CreateContainer("plug_tray");
            Assert.AreEqual("NoSeedPacket", Sow(tray).Message);

            Hold("seeds_chimebell_white", 0);
            Assert.AreEqual("PacketEmpty", Sow(tray).Message);
            _hands.TakeHeld(A);

            Hold("seeds_chimebell_white");
            Assert.AreEqual("NotInSlot", Sow(loose).Message);
            Assert.AreEqual("NotSowable", Sow(medium).Message);
            _hands.TakeHeld(A);

            Hold("spores_hearthmoss_amber");
            Assert.AreEqual("NotSowable", Sow(small).Message);
            Run(tray);
            Hold("spores_hearthmoss_amber");
            Assert.AreEqual("ContainerFull", Sow(tray).Message);
        }
    }
}
