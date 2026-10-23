using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Tests
{
    public class BuildingTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);

        private static ContentDb LoadContent()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string found = null;
            while (dir != null && found == null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(candidate)) found = candidate;
                dir = dir.Parent;
            }
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(found, "*.json"))
            {
                var name = Path.GetFileName(path);
                if (name == ContentLoader.TimeDemoFile) continue;
                files[name] = File.ReadAllText(path);
            }
            return ContentLoader.Load(files);
        }

        private ContentDb _db;
        private BuildState _state;

        [SetUp]
        public void SetUp()
        {
            _db = LoadContent();
            _state = new BuildState(_db);
        }

        [Test]
        public void PlotSizeAndInventoryComeFromContent()
        {
            Assert.AreEqual(40, _state.PlotWidth);
            Assert.AreEqual(30, _state.PlotDepth);
            Assert.AreEqual(2, _state.Inventory.Count("bench_standard"));
            Assert.AreEqual(0, _state.Inventory.Count("pot_small"));
        }

        [Test]
        public void RotationSwapsWidthAndDepthKeepingAnchor()
        {
            var size = new Size2(1, 4);
            Assert.AreEqual(4, Footprints.Rotate(size, Rotation.Deg0).D);
            Assert.AreEqual(4, Footprints.Rotate(size, Rotation.Deg90).W);
            Assert.AreEqual(1, Footprints.Rotate(size, Rotation.Deg90).D);
            Assert.AreEqual(4, Footprints.Rotate(size, Rotation.Deg180).D);
            Assert.AreEqual(4, Footprints.Rotate(size, Rotation.Deg270).W);

            var item = _state.Place("bench_standard", ItemLayer.Floor, new Cell(5, 6), Rotation.Deg90, out var r);
            Assert.AreEqual(BuildRejection.None, r);
            Assert.AreEqual(new Cell(5, 6), item.Anchor);
            Assert.AreSame(item, _state.GetAt(new Cell(8, 6)));
            Assert.AreSame(item, _state.GetAt(new Cell(5, 7))); // 2 cells deep once rotated
            Assert.IsNull(_state.GetAt(new Cell(5, 8)));
        }

        [Test]
        public void RotatedPlacementCanBeOutOfPlot()
        {
            Assert.AreEqual(BuildRejection.None, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(38, 0), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.OutOfPlot, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(38, 0), Rotation.Deg90));
        }

        [Test]
        public void NotOwnedIsCheckedBeforeOutOfPlot()
        {
            _state.Place("tap", ItemLayer.Floor, new Cell(0, 0), Rotation.Deg0, out _);
            // No tap left in inventory, and the target is also out of plot.
            Assert.AreEqual(BuildRejection.NotOwned, _state.CanPlace("tap", ItemLayer.Floor, new Cell(-1, 0), Rotation.Deg0));
        }

        [Test]
        public void OutOfPlotIsCheckedBeforeOccupied()
        {
            _state.Place("bench_standard", ItemLayer.Floor, new Cell(0, 0), Rotation.Deg0, out _);
            Assert.AreEqual(BuildRejection.OutOfPlot, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(-1, 0), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.OutOfPlot, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(0, 28), Rotation.Deg0));
        }

        [Test]
        public void OccupiedWhenAnyCellOverlaps()
        {
            _state.Place("bench_standard", ItemLayer.Floor, new Cell(2, 2), Rotation.Deg0, out _);
            Assert.AreEqual(BuildRejection.Occupied, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(2, 5), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.Occupied, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(3, 2), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.None, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(4, 2), Rotation.Deg0));
        }

        [Test]
        public void StandardBenchIsTwoByFourCells()
        {
            var item = _state.Place("bench_standard", ItemLayer.Floor, new Cell(2, 2), Rotation.Deg0, out _);
            Assert.AreSame(item, _state.GetAt(new Cell(3, 5)));
            Assert.IsNull(_state.GetAt(new Cell(4, 2)));
        }

        [Test]
        public void UnknownItemAndWrongLayerAreRejected()
        {
            Assert.AreEqual(BuildRejection.UnknownItem, _state.CanPlace("nope", ItemLayer.Floor, new Cell(0, 0), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.WrongLayer, _state.CanPlace("bench_standard", ItemLayer.Surface, new Cell(0, 0), Rotation.Deg0));
        }

        [Test]
        public void CanPlaceDoesNotMutate()
        {
            _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(0, 0), Rotation.Deg0);
            Assert.AreEqual(2, _state.Inventory.Count("bench_standard"));
            Assert.IsNull(_state.GetAt(new Cell(0, 0)));
            Assert.IsFalse(_state.PlacedItems.Any());
        }

        [Test]
        public void PlaceItemCommandConsumesInventoryAndRaisesEvent()
        {
            var world = new SimWorld();
            BuildingModule.Register(world, _state);
            var cmd = new PlaceItem(P1, "bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0);
            Assert.IsTrue(world.Validate(cmd).IsAccepted);
            Assert.AreEqual(2, _state.Inventory.Count("bench_standard"));
            world.Submit(cmd);
            world.Step();
            Assert.AreEqual(1, _state.Inventory.Count("bench_standard"));
            var placed = world.DrainEvents().OfType<ItemPlaced>().Single();
            Assert.AreEqual("bench_standard", placed.Item.ItemId);
        }

        [Test]
        public void RejectedPlaceItemCommandRaisesRejectionWithReason()
        {
            var world = new SimWorld();
            BuildingModule.Register(world, _state);
            world.Submit(new PlaceItem(P1, "bench_standard", ItemLayer.Floor, new Cell(100, 4), Rotation.Deg0));
            world.Step();
            var evt = world.DrainEvents().OfType<CommandRejectedEvent>().Single();
            Assert.AreEqual("OutOfPlot", evt.Result.Message);
            Assert.AreEqual(2, _state.Inventory.Count("bench_standard"));
        }

        [Test]
        public void PickUpReturnsItemToInventoryAndFreesCells()
        {
            var world = new SimWorld();
            BuildingModule.Register(world, _state);
            var item = _state.Place("bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0, out _);
            world.Submit(new PickUpItem(P1, item.Id));
            world.Step();
            Assert.AreEqual(2, _state.Inventory.Count("bench_standard"));
            Assert.IsNull(_state.GetAt(new Cell(4, 4)));
            Assert.AreEqual(1, world.DrainEvents().OfType<ItemPickedUp>().Count());
            Assert.AreEqual(BuildRejection.None, _state.CanPlace("bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0));
        }

        private sealed class AlwaysOccupied : ISurfaceOccupancy
        {
            public bool HasItemsOn(PlacedItem item) => true;
        }

        [Test]
        public void PickUpRejectedWhenItemsOnTop()
        {
            var world = new SimWorld();
            BuildingModule.Register(world, _state);
            var item = _state.Place("bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0, out _);
            _state.SurfaceOccupancy = new AlwaysOccupied();
            Assert.AreEqual(BuildRejection.HasItemsOnTop, _state.CanPickUp(item.Id));
            world.Submit(new PickUpItem(P1, item.Id));
            world.Step();
            var evt = world.DrainEvents().OfType<CommandRejectedEvent>().Single();
            Assert.AreEqual("HasItemsOnTop", evt.Result.Message);
            Assert.IsNotNull(_state.Get(item.Id));
            Assert.AreEqual(1, _state.Inventory.Count("bench_standard"));
        }

        [Test]
        public void PickUpUnknownIdIsRejected()
        {
            Assert.AreEqual(BuildRejection.UnknownPlacedItem, _state.CanPickUp(999));
        }

        [Test]
        public void StarterLayoutPlacesTapComputerBedAndShelf()
        {
            _state.PlaceStarterLayout();
            var ids = _state.PlacedItems.Select(p => p.ItemId).OrderBy(s => s).ToArray();
            CollectionAssert.AreEqual(new[] { "bed", "computer", "delivery_shelf", "tap" }, ids);
            Assert.AreEqual(0, _state.Inventory.Count("tap"));
            Assert.AreEqual(2, _state.Inventory.Count("bench_standard"));
        }
    }
}
