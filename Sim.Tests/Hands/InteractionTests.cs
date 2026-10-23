using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    public class InteractionTests
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

        private void Do(ICommand c) { _world.Submit(c); _world.Step(); }

        private InteractionOffer Offer(InteractTarget t) => Interaction.Offer(P1, t, _hands, _grid);

        [Test]
        public void EmptyHandsOnHoldableOffersPickUp()
        {
            var o = Offer(new InteractTarget(TargetKind.SurfaceItem, _tray.Id));
            Assert.AreEqual("E: Pick up", o.Prompt);
            Assert.IsInstanceOf<PickUp>(o.Command);
        }

        [Test]
        public void SeedPacketOnCellOffersSowOneCell()
        {
            var packet = _grid.Place("seed_packet", new Unit(53, 50), Rotation.Deg0, out _, 6);
            Do(new PickUp(P1, packet.Id));
            var o = Offer(new InteractTarget(TargetKind.SurfaceItem, _tray.Id, 3));
            Assert.AreEqual("E: Sow", o.Prompt);
            var sow = (Sow)o.Command;
            Assert.AreEqual(3, sow.CellIndex);
            Assert.IsNull(Offer(new InteractTarget(TargetKind.SurfaceItem, _tray.Id)));
        }

        [Test]
        public void CanOnPlantOffersWaterAndTapOffersRefill()
        {
            var packet = _grid.Place("seed_packet", new Unit(53, 50), Rotation.Deg0, out _, 6);
            Do(new PickUp(P1, packet.Id));
            Do(new Sow(P1, _tray.Id, 1));
            Do(new PutDown(P1, new Unit(53, 50), Rotation.Deg0));
            var can = _grid.Place("watering_can", new Unit(51, 58), Rotation.Deg0, out _);
            Do(new PickUp(P1, can.Id));

            var plant = _hands.GetContents(_tray).Plants[0];
            var o = Offer(new InteractTarget(TargetKind.SurfaceItem, _tray.Id, 1));
            Assert.AreEqual("E: Water", o.Prompt);
            Assert.AreEqual(plant.Id, ((WaterPlant)o.Command).PlantId);
            Assert.IsNull(Offer(new InteractTarget(TargetKind.SurfaceItem, _tray.Id, 4)));
            Assert.IsInstanceOf<RefillCan>(Offer(new InteractTarget(TargetKind.Tap)).Command);
        }

        [Test]
        public void TapWithoutCanOffersNothing()
        {
            Assert.IsNull(Offer(new InteractTarget(TargetKind.Tap)));
        }

        [Test]
        public void BedOffersSleepAndComputerOpensShop()
        {
            Assert.IsInstanceOf<SleepCommand>(Offer(new InteractTarget(TargetKind.Bed)).Command);
            var c = Offer(new InteractTarget(TargetKind.Computer));
            Assert.IsTrue(c.OpensShop);
            Assert.IsNull(c.Command);
        }

        [Test]
        public void KindForPlacedItemMapsFixtures()
        {
            Assert.AreEqual(TargetKind.Tap, Interaction.KindForPlacedItem("tap"));
            Assert.AreEqual(TargetKind.None, Interaction.KindForPlacedItem("bench_standard"));
        }

        [Test]
        public void ReachComesFromContent()
        {
            Assert.AreEqual(3.0f, _db.Building.InteractReach);
        }
    }
}
