using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Tests
{
    public class HandsTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);
        private static readonly PlayerId P2 = new PlayerId(2);
        private ContentDb _db;
        private SurfaceGrid _grid;
        private HandsState _hands;
        private SimWorld _world;

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
            state.Inventory.Add("bench_standard");
            _grid = new SurfaceGrid(_db, state);
            // Units u 50..59, v 50..69 are surface.
            state.Place("bench_standard", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _);
            _hands = new HandsState(_db, _grid);
            _world = new SimWorld();
            HandsModule.Register(_world, _hands);
        }

        private SurfaceItem Put(string id, int u, int v) => _grid.Place(id, new Unit(u, v), Rotation.Deg0, out _);

        private CommandResult Run(ICommand c)
        {
            var r = _world.Validate(c);
            _world.Submit(c);
            _world.Step();
            return r;
        }

        [Test]
        public void PickUpFreesUnitsAndHoldsItem()
        {
            var tray = Put("plug_tray", 50, 50);
            Assert.IsTrue(Run(new PickUp(P1, tray.Id)).IsAccepted);
            Assert.AreEqual("plug_tray", _hands.GetHeld(P1).ItemId);
            Assert.IsNull(_grid.GetAt(new Unit(50, 50)));
            Assert.IsNotNull(Put("plug_tray", 50, 50));
        }

        [Test]
        public void PickUpRejectedWhenHandsFull()
        {
            var a = Put("pot_small", 50, 50);
            var b = Put("pot_small", 52, 50);
            _hands.PickUp(P1, a.Id);
            Assert.AreEqual("HandsFull", Run(new PickUp(P1, b.Id)).Message);
            Assert.IsNotNull(_grid.GetAt(new Unit(52, 50)));
        }

        [Test]
        public void PickUpRejectedNotHoldable()
        {
            Assert.AreEqual("NotHoldable", Run(new PickUp(P1, 9999)).Message);
        }

        [Test]
        public void PutDownRejections()
        {
            Assert.AreEqual("NothingHeld", Run(new PutDown(P1, new Unit(50, 50), Rotation.Deg0)).Message);
            var pot = Put("pot_small", 50, 50);
            Put("pot_small", 55, 55);
            _hands.PickUp(P1, pot.Id);
            Assert.AreEqual("Occupied", Run(new PutDown(P1, new Unit(55, 55), Rotation.Deg0)).Message);
            Assert.AreEqual("NoSurface", Run(new PutDown(P1, new Unit(0, 0), Rotation.Deg0)).Message);
            Assert.IsTrue(Run(new PutDown(P1, new Unit(51, 51), Rotation.Deg0)).IsAccepted);
            Assert.IsNull(_hands.GetHeld(P1));
            Assert.IsNotNull(_grid.GetAt(new Unit(51, 51)));
        }

        [Test]
        public void TwoPlayersHoldIndependently()
        {
            var a = Put("pot_small", 50, 50);
            var b = Put("pot_medium", 54, 50);
            _hands.PickUp(P1, a.Id);
            _hands.PickUp(P2, b.Id);
            Assert.AreEqual("pot_small", _hands.GetHeld(P1).ItemId);
            Assert.AreEqual("pot_medium", _hands.GetHeld(P2).ItemId);
            Assert.IsNotNull(_hands.PutDown(P2, new Unit(56, 60), Rotation.Deg0, out var r), r.ToString());
            Assert.IsNull(_hands.GetHeld(P2));
            Assert.IsNotNull(_hands.GetHeld(P1));
        }

        [Test]
        public void CarryingPlantedTrayKeepsPlantsAndPutsDownElsewhere()
        {
            var tray = Put("plug_tray", 50, 50);
            var plant = new Plant(1, "v");
            _hands.AddPlant(tray, plant);
            _hands.PickUp(P1, tray.Id);
            Assert.AreSame(plant, _hands.GetHeld(P1).Contents.Plants[0]);
            var moved = _hands.PutDown(P1, new Unit(54, 60), Rotation.Deg0, out _);
            Assert.AreSame(plant, _hands.GetContents(moved).Plants[0]);
            Assert.IsNull(_grid.GetAt(new Unit(50, 50)));
            Assert.AreSame(plant, _hands.GetPlant(1));
        }

        [Test]
        public void CanStartsFullAndRefills()
        {
            var can = Put("watering_can", 50, 50);
            _hands.PickUp(P1, can.Id);
            var cap = _db.ItemDefs["watering_can"].WaterCapacity;
            Assert.Greater(cap, 0);
            Assert.AreEqual(cap, _hands.GetHeld(P1).Contents.Water);
            _hands.GetHeld(P1).Contents.Water = 0;
            Assert.IsTrue(Run(new RefillCan(P1)).IsAccepted);
            Assert.AreEqual(cap, _hands.GetHeld(P1).Contents.Water);
        }

        [Test]
        public void RefillWithoutCanRejected()
        {
            Assert.AreEqual("NoCan", Run(new RefillCan(P1)).Message);
        }

        [Test]
        public void WaterPlantSetsMoistureAndUsesWater()
        {
            var can = Put("watering_can", 50, 50);
            var tray = Put("plug_tray", 54, 50);
            var plant = new Plant(7, "v") { Moisture = 5f };
            _hands.AddPlant(tray, plant);
            _hands.PickUp(P1, can.Id);
            var cap = _hands.GetHeld(P1).Contents.Water;
            Assert.IsTrue(Run(new WaterPlant(P1, 7)).IsAccepted);
            Assert.AreEqual(100f, plant.Moisture);
            Assert.AreEqual(cap - 1, _hands.GetHeld(P1).Contents.Water);
        }

        [Test]
        public void WaterPlantRejections()
        {
            var tray = Put("plug_tray", 54, 50);
            var plant = new Plant(7, "v");
            _hands.AddPlant(tray, plant);
            Assert.AreEqual("NoCan", Run(new WaterPlant(P1, 7)).Message);
            var can = Put("watering_can", 50, 50);
            _hands.PickUp(P1, can.Id);
            plant.IsDead = true;
            Assert.AreEqual("PlantDead", Run(new WaterPlant(P1, 7)).Message);
            plant.IsDead = false;
            _hands.GetHeld(P1).Contents.Water = 0;
            Assert.AreEqual("CanEmpty", Run(new WaterPlant(P1, 7)).Message);
        }

        [Test]
        public void StarterInventoryHasOneCanAndCansAreNotSold()
        {
            Assert.AreEqual(1, _db.Economy.StarterInventory["watering_can"]);
            Assert.AreEqual(0, _db.ItemDefs["watering_can"].Price); // starter kit only (economy.md, Item ids)
        }
    }
}
