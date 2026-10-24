using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    public class HandsV2Tests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private static readonly PlayerId B = new PlayerId(2);
        private ContentDb _db;
        private SimWorld _world;
        private StorageCrate _crate;
        private EntityFactory _factory;
        private HandsV2 _hands;

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
            _crate = new StorageCrate();
            _factory = new EntityFactory(_world, _db);
            _hands = new HandsV2(_db, _crate);
            CrateCommands.Register(_world, _hands);
        }

        private CommandResult Run(ICommand c)
        {
            var r = _world.Validate(c);
            _world.Submit(c);
            _world.Step();
            return r;
        }

        [Test]
        public void TakeContainerByItemIdPutsItInHand()
        {
            _crate.AddContainer(_factory.CreateContainerForItem("pot_small"));
            Assert.IsTrue(Run(new TakeFromCrate(A, "pot_small")).IsAccepted);
            Assert.AreEqual(HeldKind.Container, _hands.GetHeld(A).Kind);
            Assert.AreEqual(0, _crate.ContainerCount);
        }

        [Test]
        public void TakeByEntityIdGetsThatContainer()
        {
            var first = _factory.CreateContainerForItem("pot_small");
            var second = _factory.CreateContainerForItem("pot_small");
            _crate.AddContainer(first); _crate.AddContainer(second);
            Assert.IsTrue(Run(new TakeFromCrate(A, second.Id)).IsAccepted);
            Assert.AreSame(second, _hands.GetHeld(A).Container);
            Assert.AreSame(first, _crate.FindContainer(first.Id));
        }

        [Test]
        public void TakeStockedPacketGivesFullSeeds()
        {
            _crate.AddStock("seeds_chimebell_white", 1);
            Assert.IsTrue(Run(new TakeFromCrate(A, "seeds_chimebell_white")).IsAccepted);
            var h = _hands.GetHeld(A);
            Assert.AreEqual(HeldKind.SeedPacket, h.Kind);
            Assert.AreEqual(6, h.SeedsLeft);
            Assert.AreEqual(0, _crate.StockCount("seeds_chimebell_white"));
        }

        [Test]
        public void TakeStockedCarryTrayIsCarryTray()
        {
            _crate.AddStock("carry_tray", 1);
            Run(new TakeFromCrate(A, "carry_tray"));
            Assert.AreEqual(HeldKind.CarryTray, _hands.GetHeld(A).Kind);
        }

        [Test]
        public void TakeWithFullHandsIsRejected()
        {
            _crate.AddStock("carry_tray", 2);
            Run(new TakeFromCrate(A, "carry_tray"));
            var r = Run(new TakeFromCrate(A, "carry_tray"));
            Assert.IsFalse(r.IsAccepted);
            Assert.AreEqual("HandsFull", r.Message);
            Assert.AreEqual(1, _crate.StockCount("carry_tray"));
        }

        [Test]
        public void TakeMissingItemIsNotInCrate()
        {
            var r = Run(new TakeFromCrate(A, "pot_small"));
            Assert.AreEqual("NotInCrate", r.Message);
            Assert.AreEqual("NotInCrate", Run(new TakeFromCrate(A, 9999)).Message);
            Assert.AreEqual("NotInCrate", Run(new TakeFromCrate(A, "no_such_item")).Message);
            Assert.IsNull(_hands.GetHeld(A));
        }

        [Test]
        public void ReturnWithEmptyHandsIsNothingHeld()
        {
            var r = Run(new ReturnToCrate(A));
            Assert.IsFalse(r.IsAccepted);
            Assert.AreEqual("NothingHeld", r.Message);
        }

        [Test]
        public void ReturnContainerPutsItBack()
        {
            var pot = _factory.CreateContainerForItem("pot_small");
            _crate.AddContainer(pot);
            Run(new TakeFromCrate(A, "pot_small"));
            Assert.IsTrue(Run(new ReturnToCrate(A)).IsAccepted);
            Assert.IsNull(_hands.GetHeld(A));
            Assert.AreSame(pot, _crate.FindContainer(pot.Id));
        }

        [Test]
        public void ReturnedCanKeepsChargesAndComesBackOut()
        {
            _crate.AddStock("watering_can", 1);
            Run(new TakeFromCrate(A, "watering_can"));
            _hands.GetHeld(A).Charges = 3;
            Assert.IsTrue(Run(new ReturnToCrate(A)).IsAccepted);
            Assert.IsTrue(_crate.HasItem("watering_can"));
            Run(new TakeFromCrate(B, "watering_can"));
            Assert.AreEqual(HeldKind.Can, _hands.GetHeld(B).Kind);
            Assert.AreEqual(3, _hands.GetHeld(B).Charges);
        }

        [Test]
        public void ReturnedBundleKeepsQuality()
        {
            _crate.AddThing(new HeldThing(HeldKind.Bundle, "bundle_hearthmoss") { Quality = QualityGrade.Excellent });
            Run(new TakeFromCrate(A, "bundle_hearthmoss"));
            Assert.AreEqual(QualityGrade.Excellent, _hands.GetHeld(A).Quality);
            Assert.IsTrue(Run(new ReturnToCrate(A)).IsAccepted);
            Assert.AreEqual(1, _crate.Things.Count);
        }

        [Test]
        public void ReturnedPacketKeepsSeedsLeft()
        {
            _crate.AddStock("seed_packet", 1);
            Run(new TakeFromCrate(A, "seed_packet"));
            _hands.GetHeld(A).SeedsLeft = 2;
            Run(new ReturnToCrate(A));
            Run(new TakeFromCrate(A, "seed_packet"));
            Assert.AreEqual(2, _hands.GetHeld(A).SeedsLeft);
        }

        [Test]
        public void PlayersHaveSeparateHands()
        {
            _crate.AddStock("carry_tray", 1);
            _crate.AddStock("watering_can", 1);
            Run(new TakeFromCrate(A, "carry_tray"));
            Run(new TakeFromCrate(B, "watering_can"));
            Assert.AreEqual(HeldKind.CarryTray, _hands.GetHeld(A).Kind);
            Assert.AreEqual(HeldKind.Can, _hands.GetHeld(B).Kind);
            Run(new ReturnToCrate(A));
            Assert.IsNull(_hands.GetHeld(A));
            Assert.AreEqual(HeldKind.Can, _hands.GetHeld(B).Kind);
        }

        [Test]
        public void PlayerCannotTakeFromOtherPlayersHands()
        {
            _crate.AddStock("carry_tray", 1);
            Run(new TakeFromCrate(A, "carry_tray"));
            var r = Run(new ReturnToCrate(B));
            Assert.AreEqual("NothingHeld", r.Message);
            Assert.AreEqual(HeldKind.CarryTray, _hands.GetHeld(A).Kind);
            Assert.AreEqual(0, _crate.StockCount("carry_tray"));
            Assert.IsFalse(_crate.HasItem("carry_tray"));
        }

        [Test]
        public void FirstRequestWinsForLastItem()
        {
            _crate.AddStock("carry_tray", 1);
            _world.Submit(new TakeFromCrate(A, "carry_tray"));
            _world.Submit(new TakeFromCrate(B, "carry_tray"));
            _world.Step();
            Assert.IsNotNull(_hands.GetHeld(A));
            Assert.IsNull(_hands.GetHeld(B));
        }

        [Test]
        public void FirstRequestWinsForSameContainerEntity()
        {
            var pot = _factory.CreateContainerForItem("pot_small");
            _crate.AddContainer(pot);
            _world.Submit(new TakeFromCrate(B, pot.Id));
            _world.Submit(new TakeFromCrate(A, pot.Id));
            _world.Step();
            Assert.AreSame(pot, _hands.GetHeld(B).Container);
            Assert.IsNull(_hands.GetHeld(A));
        }

        [Test]
        public void EventsAreRaised()
        {
            _crate.AddStock("carry_tray", 1);
            Run(new TakeFromCrate(A, "carry_tray"));
            Run(new ReturnToCrate(A));
            var types = new List<Type>();
            foreach (var e in _world.DrainEvents()) types.Add(e.GetType());
            CollectionAssert.Contains(types, typeof(TookFromCrate));
            CollectionAssert.Contains(types, typeof(ReturnedToCrate));
        }
    }
}
