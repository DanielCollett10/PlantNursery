using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    public class SoftLockGuardTests
    {
        private static readonly PlayerId P = new PlayerId(1);
        private const string TrayId = "plug_tray";
        private ContentDb _db;
        private SimWorld _world;
        private GameClock _clock;
        private BuildState _build;
        private SurfaceGrid _surface;
        private Wallet _wallet;
        private HandsState _hands;

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
            _clock = GameClock.Install(_world, _db.Time);
            _build = new BuildState(_db);
            _surface = new SurfaceGrid(_db, _build);
            _build.PlaceStarterLayout();
            _wallet = new Wallet(_db.Economy.StartingMoney);
            _hands = new HandsState(_db, _surface);
            SupplierModule.Register(_world, _db, _wallet, _build, _surface);
            SoftLockGuard.Install(_world, _db, _wallet, _build, _surface, _hands, _clock);
        }

        private string FirstVariety() => _db.VarietyDefs.Keys.First();

        /// <summary>Spend down to zero coins: the wallet condition is met.</summary>
        private void Broke() => _wallet.TrySpend(_wallet.Balance);

        private void NextMorning() => _world.Step(GameClock.MinutesPerDay);

        private List<SurfaceItem> Packets() =>
            _surface.Items.Where(i => i.ItemId == SupplierModule.SeedPacketId).ToList();

        private static List<NeighbourGift> Gifts(IReadOnlyList<ISimEvent> evts) =>
            evts.OfType<NeighbourGift>().ToList();

        private void Bench(Cell anchor) => _build.Place("bench_standard", ItemLayer.Floor, anchor, Rotation.Deg0, out _);

        /// <summary>A bench at cell (5,1) with a plug tray on it, at units (25,5). Returns the tray.</summary>
        private SurfaceItem BenchWithTray()
        {
            Bench(new Cell(5, 1));
            return _surface.Place(TrayId, new Unit(25, 5), Rotation.Deg0, out _);
        }

        private void RemoveAllTrays()
        {
            while (_build.Inventory.TryRemove(TrayId)) { }
        }

        /// <summary>Buy 25 packets (fills the shelf), then empty them so they hold no seeds.</summary>
        private void FillShelfWithEmptyPackets()
        {
            _wallet.Add(10000);
            _world.Submit(new BuyItem(P, SupplierModule.SeedPacketId, 25));
            _world.Step();
            foreach (var p in Packets()) _hands.GetContents(p).Seeds = 0;
            _world.DrainEvents();
        }

        [Test]
        public void GiftArrivesNextMorningWhenBrokeAndEmpty()
        {
            Broke();
            NextMorning();

            var packets = Packets();
            Assert.AreEqual(1, packets.Count);
            var packet = packets[0];
            var contents = _hands.GetContents(packet);
            Assert.AreEqual(packet.SeedCount, contents.Seeds);
            Assert.AreEqual(6, contents.Seeds);
            Assert.AreEqual(FirstVariety(), contents.VarietyId);
            Assert.AreSame(DeliveryShelf.Find(_build), _build.GetAt(SurfaceGrid.CellOf(packet.Anchor)));
            Assert.AreEqual(0, _wallet.Balance);

            var gift = Gifts(_world.DrainEvents()).Single();
            Assert.AreEqual(SupplierModule.SeedPacketId, gift.ItemId);
            Assert.AreEqual(packet.Id, gift.SurfaceItemId);
            Assert.AreEqual(FirstVariety(), gift.VarietyId);
        }

        [Test]
        public void NoGiftBeforeTheDayStarts()
        {
            Broke();
            _world.Step(GameClock.MinutesPerDay - 1);
            Assert.AreEqual(0, Packets().Count);
            _world.Step(1);
            Assert.AreEqual(1, Packets().Count);
        }

        [Test]
        public void WalletAtThePacketPriceBlocksGift()
        {
            var price = _db.ItemDefs[SupplierModule.SeedPacketId].Price;
            _wallet.TrySpend(_wallet.Balance - price);
            Assert.AreEqual(price, _wallet.Balance);
            NextMorning();
            Assert.AreEqual(0, Packets().Count);
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void WalletJustBelowThePacketPriceAllowsGift()
        {
            var price = _db.ItemDefs[SupplierModule.SeedPacketId].Price;
            _wallet.TrySpend(_wallet.Balance - (price - 1));
            NextMorning();
            Assert.AreEqual(1, Packets().Count);
        }

        [Test]
        public void LivingPlantOnBenchBlocksGift()
        {
            Broke();
            var tray = BenchWithTray();
            _hands.AddPlant(tray, new Plant(1, FirstVariety()));
            NextMorning();
            Assert.AreEqual(0, Packets().Count);
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void LivingPlantInHandBlocksGift()
        {
            Broke();
            var tray = BenchWithTray();
            _hands.AddPlant(tray, new Plant(1, FirstVariety()));
            Assert.AreEqual(HandsRejection.None, _hands.PickUp(P, tray.Id));
            NextMorning();
            Assert.AreEqual(0, Packets().Count);
        }

        [Test]
        public void DeadPlantDoesNotBlockGift()
        {
            Broke();
            var tray = BenchWithTray();
            _hands.AddPlant(tray, new Plant(1, FirstVariety()) { IsDead = true });
            NextMorning();
            Assert.AreEqual(1, Packets().Count);
        }

        [Test]
        public void SeedPacketOnShelfBlocksGift()
        {
            _world.Submit(new BuyItem(P, SupplierModule.SeedPacketId, 1));
            _world.Step();
            _world.DrainEvents();
            Broke();
            NextMorning();
            Assert.AreEqual(1, Packets().Count);
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void SeedPacketInHandBlocksGift()
        {
            _world.Submit(new BuyItem(P, SupplierModule.SeedPacketId, 1));
            _world.Step();
            _world.DrainEvents();
            Assert.AreEqual(HandsRejection.None, _hands.PickUp(P, Packets()[0].Id));
            Broke();
            NextMorning();
            Assert.AreEqual(0, Packets().Count);
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void EmptiedSeedPacketDoesNotBlockGift()
        {
            _world.Submit(new BuyItem(P, SupplierModule.SeedPacketId, 1));
            _world.Step();
            _world.DrainEvents();
            _hands.GetContents(Packets()[0]).Seeds = 0;
            Broke();
            NextMorning();
            Assert.AreEqual(2, Packets().Count);
            Assert.AreEqual(1, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void TrayNotAddedWhenOneIsOwnedInInventory()
        {
            var before = _build.Inventory.Count(TrayId);
            Assert.Greater(before, 0);
            Broke();
            NextMorning();
            Assert.AreEqual(before, _build.Inventory.Count(TrayId));
            Assert.IsFalse(Gifts(_world.DrainEvents()).Single().TrayAdded);
        }

        [Test]
        public void TrayAddedWhenNoneOwned()
        {
            RemoveAllTrays();
            Broke();
            NextMorning();
            Assert.AreEqual(1, _build.Inventory.Count(TrayId));
            Assert.IsTrue(Gifts(_world.DrainEvents()).Single().TrayAdded);
        }

        [Test]
        public void TrayNotAddedWhenOnePlacedOnBench()
        {
            RemoveAllTrays();
            BenchWithTray();
            Broke();
            NextMorning();
            Assert.AreEqual(0, _build.Inventory.Count(TrayId));
            Assert.AreEqual(1, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void TrayNotAddedWhenOneIsHeld()
        {
            RemoveAllTrays();
            var tray = BenchWithTray();
            Assert.AreEqual(HandsRejection.None, _hands.PickUp(P, tray.Id));
            Broke();
            NextMorning();
            Assert.AreEqual(0, _build.Inventory.Count(TrayId));
            Assert.AreEqual(1, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void FullShelfFallsBackToNearestFreeSpotOnAnySurface()
        {
            FillShelfWithEmptyPackets();
            Bench(new Cell(5, 1));
            Broke();
            NextMorning();

            var gift = Gifts(_world.DrainEvents()).Single();
            var packet = _surface.Items.Single(i => i.Id == gift.SurfaceItemId);
            var bench = _build.PlacedItems.Single(p => p.ItemId == "bench_standard");
            Assert.AreSame(bench, _build.GetAt(SurfaceGrid.CellOf(packet.Anchor)));
            Assert.AreEqual(FirstVariety(), _hands.GetContents(packet).VarietyId);
        }

        [Test]
        public void GiftWaitsForNextMorningWhenNoRoomAnywhere()
        {
            FillShelfWithEmptyPackets();
            RemoveAllTrays();
            Broke();
            var packetsBefore = Packets().Count;

            NextMorning();
            Assert.AreEqual(packetsBefore, Packets().Count);
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
            Assert.AreEqual(0, _build.Inventory.Count(TrayId), "no tray while the gift waits");

            Bench(new Cell(5, 1));
            NextMorning();
            var gift = Gifts(_world.DrainEvents()).Single();
            Assert.AreEqual(packetsBefore + 1, Packets().Count);
            Assert.IsTrue(gift.TrayAdded);
            Assert.AreEqual(1, _build.Inventory.Count(TrayId));
        }

        [Test]
        public void ConfiguredGiftVarietyIsUsed()
        {
            _db.Economy.SoftLockGuard.GiftVarietyId = "red_plain";
            Broke();
            NextMorning();
            Assert.AreEqual("red_plain", _hands.GetContents(Packets()[0]).VarietyId);
            Assert.AreEqual("red_plain", Gifts(_world.DrainEvents()).Single().VarietyId);
        }

        [Test]
        public void DisabledGuardGivesNoGift()
        {
            _db.Economy.SoftLockGuard.Enabled = false;
            Broke();
            NextMorning();
            Assert.AreEqual(0, Packets().Count);
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }
    }
}
