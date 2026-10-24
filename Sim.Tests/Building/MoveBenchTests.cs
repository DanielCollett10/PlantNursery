using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class MoveBenchTests
    {
        private ContentDb _db;
        private BuildState _state;
        private SlotOccupancy _occ;
        private StripOccupancy _strip;
        private Neighbours _nb;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _state = new BuildState(_db);
            _state.Inventory.Add("bench_3m", 4);
            _state.Inventory.Add("tap", 1);
            _occ = new SlotOccupancy(_db, _state);
            _strip = new StripOccupancy(_db, _state);
            _nb = new Neighbours(_db, _state, _occ);
        }

        private int Bench(int x, int z, Rotation r)
        {
            var p = _state.Place("bench_3m", ItemLayer.Floor, new Cell(x, z), r, out var rej);
            Assert.AreEqual(BuildRejection.None, rej);
            return p.Id;
        }

        [Test]
        public void MoveKeepsSlotsAndStripItems()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            Assert.AreEqual(SlotRejection.None, _occ.PlaceInSlot(b, "pot_small", 3, 1));
            Assert.AreEqual(StripRejection.None, _strip.Place(b, "carry_tray", 0, 0, StripContents.NoPlants));
            var moved = _state.Move(b, new Cell(20, 5), Rotation.Deg90, out var rej);
            Assert.AreEqual(BuildRejection.None, rej);
            Assert.AreEqual(b, moved.Id);
            Assert.AreEqual(new Cell(20, 5), _state.Get(b).Anchor);
            Assert.IsNull(_state.GetAt(new Cell(10, 10)));
            Assert.AreSame(moved, _state.GetAt(new Cell(20, 5)));
            Assert.AreEqual(SlotRejection.SlotOccupied, _occ.CanPlaceInSlot(b, "pot_small", 3, 1));
            Assert.AreEqual(SlotRejection.None, _occ.CanRemoveFromSlot(b, "pot_small", 3, 1));
            Assert.AreEqual(StripRejection.None, _strip.CanRemove(b, "carry_tray", 0, 0));
        }

        [Test]
        public void MoveIgnoresOwnCellsButNotOthers()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            Assert.AreEqual(BuildRejection.None, _state.CanMove(b, new Cell(11, 10), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.None, _state.CanMove(b, new Cell(10, 10), Rotation.Deg90));
            var other = Bench(10, 14, Rotation.Deg0);
            Assert.AreEqual(BuildRejection.Occupied, _state.CanMove(b, new Cell(10, 13), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.OutOfPlot, _state.CanMove(b, new Cell(-1, 0), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.UnknownPlacedItem, _state.CanMove(999, new Cell(1, 1), Rotation.Deg0));
            var v = _state.Version;
            Assert.IsNull(_state.Move(other, new Cell(10, 11), Rotation.Deg0, out var rej));
            Assert.AreEqual(BuildRejection.Occupied, rej);
            Assert.AreEqual(v, _state.Version);
        }

        [Test]
        public void PickUpRejectedWithPotsOrStripItems()
        {
            var a = Bench(10, 10, Rotation.Deg0);
            var b = Bench(20, 10, Rotation.Deg0);
            Assert.AreEqual(BuildRejection.None, _state.CanPickUp(a));
            _occ.PlaceInSlot(a, "pot_small", 0, 0);
            _strip.Place(b, "carry_tray", 0, 0, StripContents.NoPlants);
            Assert.AreEqual(BuildRejection.HasItemsOnTop, _state.CanPickUp(a));
            Assert.AreEqual(BuildRejection.HasItemsOnTop, _state.CanPickUp(b));
            _occ.RemoveFromSlot(a, "pot_small", 0, 0);
            _strip.Remove(b, "carry_tray", 0, 0);
            Assert.AreEqual(BuildRejection.None, _state.CanPickUp(a));
            Assert.AreEqual(BuildRejection.None, _state.CanPickUp(b));
        }

        [Test]
        public void NeighboursRecomputedAfterMove()
        {
            var a = Bench(10, 10, Rotation.Deg0);
            var b = Bench(10, 12, Rotation.Deg180);
            var s = new SlotRef(a, "pot_small", 12, 2);
            _occ.PlaceInSlot(s.BenchId, s.Size, s.Col, s.Row);
            // Find the slot on b that touches s.
            SlotRef touching = default;
            var layout = SlotLayout.For(_db.ItemDefs["bench_3m"].Bench, _db.ContainerDefs["pot_small"]);
            for (var c = 0; c < layout.Cols; c++)
                for (var r = 0; r < layout.Rows; r++)
                {
                    var cand = new SlotRef(b, "pot_small", c, r);
                    _occ.PlaceInSlot(b, "pot_small", c, r);
                    if (_nb.Of(s).Contains(cand)) touching = cand;
                    _occ.RemoveFromSlot(b, "pot_small", c, r);
                }
            Assert.AreNotEqual(default(SlotRef), touching, "test setup: expected a neighbouring slot");
            _occ.PlaceInSlot(b, touching.Size, touching.Col, touching.Row);
            CollectionAssert.Contains(_nb.Of(s), touching);

            Assert.IsNotNull(_state.Move(b, new Cell(10, 20), Rotation.Deg180, out _));
            Assert.AreEqual(0, _nb.Of(s).Count);
            Assert.AreEqual(0, _nb.Of(touching).Count);

            _state.Move(b, new Cell(10, 12), Rotation.Deg180, out _);
            CollectionAssert.Contains(_nb.Of(s), touching);
        }

        [Test]
        public void MoveCommandAcceptsAndRejects()
        {
            var b = Bench(10, 10, Rotation.Deg0);
            var world = new Core.SimWorld();
            BuildingModule.Register(world, _state);
            var p1 = new Core.PlayerId(1);
            world.Submit(new MoveItem(p1, b, new Cell(1000, 1000), Rotation.Deg0));
            world.Step();
            Assert.AreEqual("OutOfPlot", world.DrainEvents().OfType<Core.CommandRejectedEvent>().Single().Result.Message);
            Assert.AreEqual(new Cell(10, 10), _state.Get(b).Anchor);
            world.Submit(new MoveItem(p1, b, new Cell(15, 15), Rotation.Deg0));
            world.Step();
            Assert.AreEqual(1, world.DrainEvents().OfType<ItemMoved>().Count());
            Assert.AreEqual(new Cell(15, 15), _state.Get(b).Anchor);
        }
    }
}
