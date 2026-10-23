using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class SurfaceGridTests
    {
        private BuildState _state;
        private SurfaceGrid _grid;

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
            var db = ContentLoader.Load(files);
            _state = new BuildState(db);
            _state.Inventory.Add("bench_standard");
            _state.Inventory.Add("bench_standard");
            _grid = new SurfaceGrid(db, _state);
            // Benches at cells (10,10) and (12,10): each 2 wide, 4 deep -> units u 50..69, v 50..69; seam between u=59 and u=60.
            Assert.IsNotNull(_state.Place("bench_standard", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _));
            Assert.IsNotNull(_state.Place("bench_standard", ItemLayer.Floor, new Cell(12, 10), Rotation.Deg0, out _));
        }

        [Test]
        public void SmallPotsPackOnePerUnit()
        {
            for (var u = 50; u < 60; u++)
                Assert.IsNotNull(_grid.Place("pot_small", new Unit(u, 50), Rotation.Deg0, out var r), "u=" + u + " " + r);
            Assert.AreEqual(BuildRejection.Occupied, _grid.CanPlace("pot_small", new Unit(55, 50), Rotation.Deg0));
        }

        [Test]
        public void MediumPotTakesTwoByTwo()
        {
            _grid.Place("pot_medium", new Unit(50, 50), Rotation.Deg0, out _);
            Assert.AreEqual(BuildRejection.Occupied, _grid.CanPlace("pot_small", new Unit(51, 51), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.None, _grid.CanPlace("pot_small", new Unit(52, 50), Rotation.Deg0));
        }

        [Test]
        public void NoSurfaceOffBench()
        {
            Assert.AreEqual(BuildRejection.NoSurface, _grid.CanPlace("pot_small", new Unit(0, 0), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.NoSurface, _grid.CanPlace("pot_small", new Unit(49, 50), Rotation.Deg0));
            Assert.AreEqual(BuildRejection.NoSurface, _grid.CanPlace("pot_medium", new Unit(59, 69), Rotation.Deg0));
        }

        [Test]
        public void NoSurfaceOnNonSurfaceFloorItem()
        {
            _state.Inventory.Add("tap");
            _state.Place("tap", ItemLayer.Floor, new Cell(20, 20), Rotation.Deg0, out _);
            Assert.AreEqual(BuildRejection.NoSurface, _grid.CanPlace("pot_small", new Unit(100, 100), Rotation.Deg0));
        }

        [Test]
        public void TrayStraddlesTouchingBenches()
        {
            // Benches cover u 50..69; the seam is between u=59 and u=60.
            Assert.AreEqual(BuildRejection.None, _grid.CanPlace("plug_tray", new Unit(59, 50), Rotation.Deg0));
            // Anchor 69 spills onto u=70, which is off the benches.
            Assert.AreEqual(BuildRejection.NoSurface, _grid.CanPlace("plug_tray", new Unit(69, 50), Rotation.Deg0));
        }

        [Test]
        public void NudgeMovesTrayAcrossSeam()
        {
            _grid.Place("pot_small", new Unit(59, 50), Rotation.Deg0, out _);
            // Desired (59,50) is blocked; (60,50) is the nearest valid tray anchor (u 60..61, both on bench 2).
            Assert.AreEqual(new Unit(60, 50), _grid.FindNearestFreeSpot("plug_tray", Rotation.Deg0, new Unit(59, 50), 2));
        }

        [Test]
        public void RotationSwapsFootprint()
        {
            var tray = _grid.Place("plug_tray", new Unit(50, 50), Rotation.Deg90, out _);
            Assert.AreEqual(3, tray.Width);
            Assert.AreEqual(2, tray.Depth);
            Assert.AreSame(tray, _grid.GetAt(new Unit(52, 51)));
            Assert.IsNull(_grid.GetAt(new Unit(50, 52)));
        }

        [Test]
        public void PickUpBenchWithItemsIsRejected()
        {
            var bench = _state.GetAt(new Cell(10, 10));
            Assert.AreEqual(BuildRejection.None, _state.CanPickUp(bench.Id));
            var pot = _grid.Place("pot_small", new Unit(52, 65), Rotation.Deg0, out _);
            Assert.AreEqual(BuildRejection.HasItemsOnTop, _state.CanPickUp(bench.Id));
            Assert.AreEqual(BuildRejection.None, _state.CanPickUp(_state.GetAt(new Cell(12, 10)).Id));
            _grid.Remove(pot.Id);
            Assert.AreEqual(BuildRejection.None, _state.CanPickUp(bench.Id));
        }

        [Test]
        public void NudgeReturnsDesiredWhenFree()
        {
            Assert.AreEqual(new Unit(52, 52), _grid.FindNearestFreeSpot("pot_small", Rotation.Deg0, new Unit(52, 52), 2));
        }

        [Test]
        public void NudgePicksNearestFreeSpot()
        {
            _grid.Place("pot_medium", new Unit(52, 52), Rotation.Deg0, out _); // covers 52..53
            // From (53,53): (54,53) and (53,54) are both at distance 1 and free; smallest v wins.
            Assert.AreEqual(new Unit(54, 53), _grid.FindNearestFreeSpot("pot_small", Rotation.Deg0, new Unit(53, 53), 2));
        }

        [Test]
        public void NudgeTieBreakIsDeterministic()
        {
            _grid.Place("pot_small", new Unit(55, 55), Rotation.Deg0, out _);
            for (var i = 0; i < 3; i++)
                Assert.AreEqual(new Unit(55, 54), _grid.FindNearestFreeSpot("pot_small", Rotation.Deg0, new Unit(55, 55), 2));
            _grid.Place("pot_small", new Unit(55, 54), Rotation.Deg0, out _);
            Assert.AreEqual(new Unit(54, 55), _grid.FindNearestFreeSpot("pot_small", Rotation.Deg0, new Unit(55, 55), 2));
        }

        [Test]
        public void NudgeNoneWithinRadius()
        {
            Assert.IsNull(_grid.FindNearestFreeSpot("pot_small", Rotation.Deg0, new Unit(0, 0), 2));
            for (var u = 50; u < 70; u++)
                for (var v = 50; v < 70; v++)
                    _grid.Place("pot_small", new Unit(u, v), Rotation.Deg0, out _);
            Assert.IsNull(_grid.FindNearestFreeSpot("pot_small", Rotation.Deg0, new Unit(55, 55), 2));
        }
    }
}
