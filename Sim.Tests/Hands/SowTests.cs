using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Tests
{
    public class SowTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);
        private ContentDb _db;
        private SurfaceGrid _grid;
        private HandsState _hands;
        private SimWorld _world;
        private SurfaceItem _tray;

        [SetUp]
        public void SetUp()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string found = null;
            while (dir != null && found == null)
            {
                var c = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(c)) found = c;
                dir = dir.Parent;
            }
            var files = new Dictionary<string, string>();
            foreach (var p in Directory.GetFiles(found, "*.json"))
            {
                if (Path.GetFileName(p) == ContentLoader.TimeDemoFile) continue;
                files[Path.GetFileName(p)] = File.ReadAllText(p);
            }
            _db = ContentLoader.Load(files);
            var state = new BuildState(_db);
            state.Inventory.Add("bench_standard");
            _grid = new SurfaceGrid(_db, state);
            state.Place("bench_standard", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _);
            _hands = new HandsState(_db, _grid);
            _world = new SimWorld();
            HandsModule.Register(_world, _hands);
            _tray = _grid.Place("plug_tray", new Unit(50, 50), Rotation.Deg0, out _);
        }

        private CommandResult Run(ICommand c)
        {
            var r = _world.Validate(c);
            _world.Submit(c);
            _world.Step();
            return r;
        }

        private void HoldPacket(int seeds = 6)
        {
            var packet = _grid.Place("seed_packet", new Unit(53, 50), Rotation.Deg0, out _, seeds);
            Assert.IsTrue(Run(new PickUp(P1, packet.Id)).IsAccepted);
        }

        [Test]
        public void SowCreatesSeedPlantAndUsesOneSeed()
        {
            HoldPacket();
            Assert.IsTrue(Run(new Sow(P1, _tray.Id, 2)).IsAccepted);
            var plants = _hands.GetContents(_tray).Plants;
            Assert.AreEqual(1, plants.Count);
            var p = plants[0];
            Assert.AreEqual(PlantStage.Seed, p.Stage);
            Assert.AreEqual(50f, p.Moisture);
            Assert.AreEqual(100f, p.Health);
            Assert.AreEqual(2, p.CellIndex);
            Assert.IsNotNull(p.Genome);
            Assert.AreEqual(5, _hands.GetHeld(P1).Contents.Seeds);
            Assert.AreSame(p, _hands.GetPlant(p.Id));
            Assert.AreEqual(1, _world.DrainEvents().OfType<PlantSown>().Count());
        }

        [Test]
        public void SowRejectsWithoutPacket()
        {
            var r = Run(new Sow(P1, _tray.Id, 0));
            Assert.AreEqual("NoSeedPacket", r.Message);
            var can = _grid.Place("watering_can", new Unit(50, 55), Rotation.Deg0, out _);
            Run(new PickUp(P1, can.Id));
            Assert.AreEqual("NoSeedPacket", _world.Validate(new Sow(P1, _tray.Id, 0)).Message);
        }

        [Test]
        public void SowRejectsEmptyPacket()
        {
            HoldPacket(0);
            Assert.AreEqual("PacketEmpty", Run(new Sow(P1, _tray.Id, 0)).Message);
        }

        [Test]
        public void SowRejectsOccupiedCellButOtherCellsWork()
        {
            HoldPacket();
            Run(new Sow(P1, _tray.Id, 0));
            Assert.AreEqual("CellOccupied", Run(new Sow(P1, _tray.Id, 0)).Message);
            Assert.AreEqual(5, _hands.GetHeld(P1).Contents.Seeds);
            Assert.IsTrue(Run(new Sow(P1, _tray.Id, 1)).IsAccepted);
        }

        [Test]
        public void SowRejectsBadCellAndNonContainer()
        {
            HoldPacket();
            Assert.IsFalse(Run(new Sow(P1, _tray.Id, 6)).IsAccepted);
            Assert.IsFalse(Run(new Sow(P1, 9999, 0)).IsAccepted);
        }

        [Test]
        public void CompostRejectsLivingPlant()
        {
            HoldPacket();
            Run(new Sow(P1, _tray.Id, 0));
            var p = _hands.GetContents(_tray).Plants[0];
            Assert.AreEqual("NotDead", Run(new Compost(P1, p.Id)).Message);
            Assert.AreEqual(1, _hands.GetContents(_tray).Plants.Count);
        }

        [Test]
        public void CompostClearsDeadPlantAndFreesCell()
        {
            HoldPacket();
            Run(new Sow(P1, _tray.Id, 0));
            var p = _hands.GetContents(_tray).Plants[0];
            p.IsDead = true;
            Assert.IsTrue(Run(new Compost(P1, p.Id)).IsAccepted);
            Assert.AreEqual(0, _hands.GetContents(_tray).Plants.Count);
            Assert.IsNull(_hands.GetPlant(p.Id));
            Assert.AreEqual(1, _world.DrainEvents().OfType<PlantComposted>().Count());
            Assert.IsTrue(Run(new Sow(P1, _tray.Id, 0)).IsAccepted);
            Assert.AreNotEqual(p.Id, _hands.GetContents(_tray).Plants[0].Id);
        }

        [Test]
        public void CompostUnknownPlantRejected()
        {
            Assert.IsFalse(Run(new Compost(P1, 42)).IsAccepted);
        }
    }
}
