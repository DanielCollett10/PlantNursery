using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;

namespace PlantNursery.Sim.Tests
{
    public class SupplierTests
    {
        private static readonly PlayerId P = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _world;
        private BuildState _build;
        private SurfaceGrid _surface;
        private Wallet _wallet;

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
            _surface = new SurfaceGrid(_db, _build);
            _build.PlaceStarterLayout();
            _wallet = new Wallet(_db.Economy.StartingMoney);
            SupplierModule.Register(_world, _db, _wallet, _build, _surface);
        }

        private void Buy(string id, int qty)
        {
            _world.Submit(new BuyItem(P, id, qty));
            _world.Step();
        }

        private static CommandRejectedEvent Rejection(IReadOnlyList<ISimEvent> evts) =>
            evts.OfType<CommandRejectedEvent>().SingleOrDefault();

        [Test]
        public void WalletStartsWithContentMoney()
        {
            Assert.AreEqual(200, _wallet.Balance);
        }

        [Test]
        public void BuyEquipmentGoesToInventoryAndSpends()
        {
            var before = _build.Inventory.Count("bench_standard");
            Buy("bench_standard", 2);
            Assert.AreEqual(before + 2, _build.Inventory.Count("bench_standard"));
            Assert.AreEqual(120, _wallet.Balance);
            var changed = _world.DrainEvents().OfType<WalletChanged>().Single();
            Assert.AreEqual(200, changed.OldBalance);
            Assert.AreEqual(120, changed.NewBalance);
        }

        [Test]
        public void InsufficientFundsRejectsWithoutChange()
        {
            var before = _build.Inventory.Count("bench_standard");
            Buy("bench_standard", 6); // 240 > 200
            var rej = Rejection(_world.DrainEvents());
            Assert.IsNotNull(rej);
            Assert.AreEqual(RejectReason.InsufficientFunds, rej.Result.Reason);
            Assert.AreEqual(200, _wallet.Balance);
            Assert.AreEqual(before, _build.Inventory.Count("bench_standard"));
            Assert.AreEqual(0, _surface.Items.Count());
        }

        [Test]
        public void InsufficientFundsOverflowSafe()
        {
            var r = _world.Validate(new BuyItem(P, "bench_standard", int.MaxValue));
            Assert.AreEqual(RejectReason.InsufficientFunds, r.Reason);
        }

        [Test]
        public void SeedPacketsAppearOnShelfWithSeedCount()
        {
            Buy("seed_packet", 3);
            var items = _surface.Items.ToList();
            Assert.AreEqual(3, items.Count);
            Assert.IsTrue(items.All(i => i.ItemId == "seed_packet" && i.SeedCount == 6));
            Assert.AreEqual(170, _wallet.Balance);
            var shelf = _build.PlacedItems.Single(p => p.ItemId == "delivery_shelf");
            foreach (var i in items)
                Assert.AreSame(shelf, _build.GetAt(SurfaceGrid.CellOf(i.Anchor)));
            Assert.AreEqual(0, _build.Inventory.Count("seed_packet"));
        }

        [Test]
        public void ShelfFullRejectsAllOrNothing()
        {
            // Shelf is 5x10 units, packets 1x2: 25 fit. Give plenty of money.
            _wallet.Add(10000);
            Buy("seed_packet", 25);
            Assert.AreEqual(25, _surface.Items.Count());
            _world.DrainEvents();
            var balance = _wallet.Balance;
            Buy("seed_packet", 1);
            var rej = Rejection(_world.DrainEvents());
            Assert.IsNotNull(rej);
            Assert.AreEqual(SupplierModule.DeliveryShelfFull, rej.Result.Message);
            Assert.AreEqual(balance, _wallet.Balance);
            Assert.AreEqual(25, _surface.Items.Count());
        }

        [Test]
        public void PartialFitRollsBack()
        {
            _wallet.Add(10000);
            Buy("seed_packet", 24);
            _world.DrainEvents();
            Buy("seed_packet", 2);
            Assert.AreEqual(SupplierModule.DeliveryShelfFull, Rejection(_world.DrainEvents()).Result.Message);
            Assert.AreEqual(24, _surface.Items.Count());
        }

        [Test]
        public void ValidateIsQueryOnly()
        {
            var r = _world.Validate(new BuyItem(P, "seed_packet", 2));
            Assert.IsTrue(r.IsAccepted);
            Assert.AreEqual(0, _surface.Items.Count());
            Assert.AreEqual(200, _wallet.Balance);
        }

        [Test]
        public void BadArgumentsRejected()
        {
            Assert.AreEqual(RejectReason.InvalidArgument, _world.Validate(new BuyItem(P, "nope", 1)).Reason);
            Assert.AreEqual(RejectReason.InvalidArgument, _world.Validate(new BuyItem(P, "bench_standard", 0)).Reason);
            Assert.AreEqual(RejectReason.InvalidArgument, _world.Validate(new BuyItem(P, "tap", 1)).Reason);
        }

        [Test]
        public void NoDeliveryShelfPlacedMeansShelfFull()
        {
            var shelf = _build.PlacedItems.Single(p => p.ItemId == "delivery_shelf");
            _build.PickUp(shelf.Id, out _);
            Assert.AreEqual(SupplierModule.DeliveryShelfFull, _world.Validate(new BuyItem(P, "seed_packet", 1)).Message);
        }
    }
}
