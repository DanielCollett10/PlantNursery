using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class CrateSupplierTests
    {
        private static readonly PlayerId P = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _world;
        private BuildState _build;
        private Wallet _wallet;
        private StorageCrate _crate;
        private EntityFactory _factory;

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
            _world = new SimWorld();
            _build = new BuildState(_db);
            _wallet = new Wallet(1000);
            _crate = new StorageCrate();
            _factory = new EntityFactory(_world, _db);
            CrateSupplier.Register(_world, _db, _wallet, _build, _crate, _factory);
        }

        private void Buy(string id, int qty)
        {
            _world.Submit(new BuyItem(P, id, qty));
            _world.Step();
        }

        [Test]
        public void FactoryContainersGetWorldIds()
        {
            var a = _factory.CreateContainer("pot_small");
            var b = _factory.CreateContainer("plug_tray");
            Assert.AreEqual(1, a.Id);
            Assert.AreEqual(2, b.Id);
            Assert.AreEqual(_db.ContainerDefs["plug_tray"], b.Def);
            Assert.AreEqual(0, b.PlantIds.Count);
            Assert.Throws<ArgumentException>(() => _factory.CreateContainer("nope"));
            Assert.IsNull(_factory.CreateContainerForItem("watering_can"));
        }

        [Test]
        public void BuyingPotsMakesContainerEntitiesInCrate()
        {
            var before = _wallet.Balance;
            Buy("pot_small", 3);
            Assert.AreEqual(3, _crate.ContainerCount);
            Assert.AreEqual(3, _crate.Containers.Select(c => c.Id).Distinct().Count());
            Assert.IsTrue(_crate.Containers.All(c => c.Def.Id == "pot_small"));
            Assert.AreEqual(before - 3 * _db.ItemDefs["pot_small"].Price, _wallet.Balance);
            Assert.AreEqual(0, _build.Inventory.Count("pot_small"));
        }

        [Test]
        public void BuyingPacketsAddsCountedStockNotShelf()
        {
            var id = _db.ItemDefs.Values.First(d => d.PacketSeeds > 0 && d.Price > 0).Id;
            Buy(id, 4);
            Assert.AreEqual(4, _crate.StockCount(id));
            Assert.AreEqual(0, _crate.ContainerCount);
            Assert.IsTrue(_crate.TryTakeStock(id));
            Assert.AreEqual(3, _crate.StockCount(id));
        }

        [Test]
        public void FloorItemsStillGoToBuildInventory()
        {
            Buy("bench_3m", 1);
            Assert.AreEqual(1, _build.Inventory.Count("bench_3m"));
            Assert.AreEqual(0, _crate.ContainerCount);
            Assert.AreEqual(0, _crate.StockCount("bench_3m"));
        }

        [Test]
        public void InsufficientFundsChangesNothing()
        {
            _wallet.TrySpend(_wallet.Balance);
            Buy("pot_small", 1);
            Assert.AreEqual(0, _crate.ContainerCount);
            Assert.AreEqual(0, _wallet.Balance);
            Assert.IsTrue(_world.DrainEvents().OfType<CommandRejectedEvent>().Any());
        }

        [Test]
        public void CrateAddAndTakeContainer()
        {
            var c = _factory.CreateContainer("pot_large");
            _crate.AddContainer(c);
            Assert.AreSame(c, _crate.FindContainer(c.Id));
            Assert.AreSame(c, _crate.TakeContainer(c.Id));
            Assert.IsNull(_crate.TakeContainer(c.Id));
        }
    }
}
