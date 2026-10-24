using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    /// <summary>The crate-path soft-lock guard (economy.md, Soft-lock guard): spores into the crate, not the shelf.</summary>
    public class CrateSoftLockGuardTests
    {
        private static readonly PlayerId P = new PlayerId(1);
        private const string Gift = CrateSoftLockGuard.GiftItemId;
        private const string Tray = CrateSoftLockGuard.PlugTrayItemId;
        private ContentDb _db;
        private SimWorld _world;
        private Nursery _n;

        [SetUp]
        public void SetUp()
        {
            _db = Db();
            Build(_db);
        }

        private static ContentDb Db() => ContentLoader.Load(ContentLoaderTests.RealFiles(false));

        private void Build(ContentDb db)
        {
            _world = new SimWorld(0.05, 1);
            _n = NurseryBuilder.Build(_world, db);
            var clock = GameClock.Install(_world, db.Time);
            CrateSoftLockGuard.Install(_world, db, _n, clock);
        }

        /// <summary>Spend down to zero coins: the wallet condition is met.</summary>
        private void Broke() => _n.Wallet.TrySpend(_n.Wallet.Balance);

        private void NextMorning() => _world.Step(GameClock.MinutesPerDay);

        /// <summary>Every starter plant is dead, so no living plant is held anywhere.</summary>
        private void KillKitPlants()
        {
            foreach (var p in _n.Plants.Values) p.IsDead = true;
        }

        /// <summary>An empty pot in the crate (or in a hand) holding the given plant id.</summary>
        private Container PotWithPlant(int plantId)
        {
            var pot = new EntityFactory(_world, _db).CreateContainer("pot_small");
            pot.AddPlant(plantId);
            return pot;
        }

        private static List<NeighbourGift> Gifts(IReadOnlyList<ISimEvent> evts) =>
            evts.OfType<NeighbourGift>().ToList();

        private int CratePlugTrays() => _n.Crate.Containers.Count(c => c.Def.ItemId == Tray);

        [Test]
        public void GiftGoesIntoCrateWhenBrokeAndNothingLives()
        {
            KillKitPlants();
            Broke();
            NextMorning();

            Assert.AreEqual(1, _n.Crate.StockCount(Gift));
            Assert.AreEqual(0, _n.Wallet.Balance);
            var gift = Gifts(_world.DrainEvents()).Single();
            Assert.AreEqual(Gift, gift.ItemId);
            Assert.AreEqual("hearthmoss_amber", gift.VarietyId);
            Assert.IsTrue(gift.InCrate);
            Assert.AreEqual(-1, gift.SurfaceItemId);
            Assert.IsFalse(gift.TrayAdded, "the kit's plug trays already exist");
        }

        [Test]
        public void GiftCanBeTakenFromTheCrateAsOneUnsownPacket()
        {
            KillKitPlants();
            Broke();
            NextMorning();

            Assert.AreEqual(CrateRejection.None, _n.Hands.Take(P, Gift));
            var held = _n.Hands.GetHeld(P);
            Assert.AreEqual(HeldKind.SeedPacket, held.Kind);
            Assert.AreEqual(1, held.SeedsLeft);
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
        }

        [Test]
        public void NoGiftBeforeTheDayStarts()
        {
            KillKitPlants();
            Broke();
            _world.Step(GameClock.MinutesPerDay - 1);
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
            _world.Step(1);
            Assert.AreEqual(1, _n.Crate.StockCount(Gift));
        }

        [Test]
        public void GiftIsNotRepeatedWhileTheUnsownPacketExists()
        {
            KillKitPlants();
            Broke();
            NextMorning();
            _world.DrainEvents();
            NextMorning();
            Assert.AreEqual(1, _n.Crate.StockCount(Gift));
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void CheapestPacketPriceIsTheThreshold()
        {
            var price = _db.ItemDefs.Values.Where(d => d.PacketSeeds > 0 && d.Price > 0).Min(d => d.Price);
            Assert.AreEqual(10, price, "spores are the cheapest packet");
            KillKitPlants();
            Assert.IsTrue(_n.Wallet.TrySpend(_n.Wallet.Balance - price));
            Assert.AreEqual(price, _n.Wallet.Balance);
            NextMorning();
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void WalletJustBelowThePriceAllowsGift()
        {
            var price = _db.ItemDefs.Values.Where(d => d.PacketSeeds > 0 && d.Price > 0).Min(d => d.Price);
            KillKitPlants();
            Assert.IsTrue(_n.Wallet.TrySpend(_n.Wallet.Balance - (price - 1)));
            NextMorning();
            Assert.AreEqual(1, _n.Crate.StockCount(Gift));
        }

        [Test]
        public void StarterKitsLivingPlantsBlockTheGift()
        {
            Broke();
            NextMorning();
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void LivingPlantInCrateBlocksGift()
        {
            KillKitPlants();
            _n.Crate.AddContainer(PotWithPlant(999999));
            Broke();
            NextMorning();
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
        }

        [Test]
        public void LivingPlantInHandBlocksGift()
        {
            KillKitPlants();
            Assert.IsTrue(_n.Hands.TryGive(P, HeldThing.OfContainer(PotWithPlant(999999))));
            Broke();
            NextMorning();
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
        }

        [Test]
        public void DeadPlantDoesNotBlockGift()
        {
            KillKitPlants();
            var deadId = _n.Plants.Keys.First();
            _n.Crate.AddContainer(PotWithPlant(deadId));
            Broke();
            NextMorning();
            Assert.AreEqual(1, _n.Crate.StockCount(Gift));
        }

        [Test]
        public void UnsownPacketInCrateBlocksGift()
        {
            KillKitPlants();
            _n.Crate.AddStock(Gift, 1);
            Broke();
            NextMorning();
            Assert.AreEqual(1, _n.Crate.StockCount(Gift), "no second packet");
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void UnsownPacketInHandBlocksGift()
        {
            KillKitPlants();
            Assert.IsTrue(_n.Hands.TryGive(P, HeldThing.Fresh(_db.ItemDefs[Gift])));
            Broke();
            NextMorning();
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }

        [Test]
        public void EmptiedPacketInHandDoesNotBlockGift()
        {
            KillKitPlants();
            var packet = HeldThing.Fresh(_db.ItemDefs[Gift]);
            packet.SeedsLeft = 0;
            Assert.IsTrue(_n.Hands.TryGive(P, packet));
            Broke();
            NextMorning();
            Assert.AreEqual(1, _n.Crate.StockCount(Gift));
        }

        /// <summary>A nursery with no plug tray anywhere: no sown bench slots, and the starter inventory's trays removed.</summary>
        private void BuildWithoutPlugTrays()
        {
            var db = Db();
            db.StarterKit.Placements[0].Planting = new List<StarterSowing>();
            Build(db);
            _db = db;
            while (_n.Build.Inventory.TryRemove(Tray)) { }
            Assert.AreEqual(0, _n.Build.Inventory.Count(Tray));
        }

        [Test]
        public void PlugTrayAddedWhenNoneExistsAnywhere()
        {
            BuildWithoutPlugTrays();
            Broke();
            NextMorning();

            Assert.AreEqual(1, CratePlugTrays());
            var gift = Gifts(_world.DrainEvents()).Single();
            Assert.IsTrue(gift.TrayAdded);
            Assert.IsTrue(gift.InCrate);
        }

        [Test]
        public void PlugTrayNotAddedWhenOneIsAlreadyInCrate()
        {
            BuildWithoutPlugTrays();
            _n.Crate.AddContainer(new EntityFactory(_world, _db).CreateContainer(Tray));
            Broke();
            NextMorning();

            Assert.AreEqual(1, CratePlugTrays());
            Assert.IsFalse(Gifts(_world.DrainEvents()).Single().TrayAdded);
        }

        [Test]
        public void DisabledGuardGivesNoGift()
        {
            var db = Db();
            db.Economy.SoftLockGuard.Enabled = false;
            Build(db);
            _db = db;
            KillKitPlants();
            Broke();
            NextMorning();
            Assert.AreEqual(0, _n.Crate.StockCount(Gift));
            Assert.AreEqual(0, Gifts(_world.DrainEvents()).Count);
        }
    }
}
