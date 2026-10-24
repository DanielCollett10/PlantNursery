using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    /// <summary>ScrapeMoss, bundles and regrowth (species.md, Hearthmoss).</summary>
    public class ScrapeMossTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _w;
        private Nursery _n;
        private Container _tray;
        private Plant _moss;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _w = new SimWorld(0.05, 1);
            _n = NurseryBuilder.Build(_w, _db);
            CrateCommands.Register(_w, _n.Hands);
            MossCommands.Register(_w, _n, _db, Lookup);
            MossRegrowSystem.Install(_w, Containers, Lookup);
            _tray = Containers().First(c => c.Def.ItemId == "plug_tray" && c.PlantIds.Count > 0);
            _moss = _n.Plants[_tray.PlantIds[0]];
            Assert.AreEqual("hearthmoss_amber", _moss.VarietyId);
            Mature(_moss);
        }

        private Plant Lookup(int id) => _n.Plants.TryGetValue(id, out var p) ? p : null;

        private IEnumerable<Container> Containers() =>
            _n.Contents.Things.Where(t => t.Kind == HeldKind.Container).Select(t => t.Container).Concat(_n.Crate.Containers);

        private void Mature(Plant p) { p.StageIndex = _db.SpeciesDefs[p.SpeciesId].Stages.Count - 1; }

        private CommandResult Scrape()
        {
            var c = new ScrapeMoss(A, _tray.Id);
            var r = _w.Validate(c);
            _w.Submit(c);
            _w.Step();
            return r;
        }

        [Test]
        public void ScrapeGivesBundleWithMossQualityIntoHands()
        {
            _moss.TicksAlive = 100; _moss.TicksNotDry = 100; // excellent care
            Assert.IsTrue(Scrape().IsAccepted);
            var held = _n.Hands.GetHeld(A);
            Assert.AreEqual(HeldKind.Bundle, held.Kind);
            Assert.AreEqual("bundle_hearthmoss", held.ItemId);
            Assert.AreEqual(QualityGrade.Excellent, held.Quality);
            Assert.IsTrue(_moss.IsRegrowing);
        }

        [Test]
        public void PoorMossGivesPoorBundle()
        {
            _moss.TicksAlive = 100; _moss.TicksNotDry = 10;
            Scrape();
            Assert.AreEqual(QualityGrade.Poor, _n.Hands.GetHeld(A).Quality);
        }

        [Test]
        public void AmberRegrowsInOneDay()
        {
            Scrape(); // the scrape tick counts as regrow tick 1
            Assert.AreEqual(1440, MossRules.RegrowTicks(_moss, _db.SpeciesDefs["hearthmoss"], 1440));
            _w.Step(1438);
            Assert.IsFalse(MossRules.IsHarvestable(_moss, _db));
            _w.Step();
            Assert.IsTrue(MossRules.IsHarvestable(_moss, _db));
            _n.Hands.TakeHeld(A);
            Assert.IsTrue(Scrape().IsAccepted);
        }

        [Test]
        public void SlowerGenesRegrowSlower()
        {
            var violet = new SpeciesPlantFactory(_w, _db).Create("hearthmoss", "hearthmoss_violet", "plug_tray", 0);
            // speed 0.35 -> 1.25 - 0.175 = 1.075 days
            Assert.AreEqual(1548, MossRules.RegrowTicks(violet, _db.SpeciesDefs["hearthmoss"], 1440));
        }

        [Test]
        public void NotHarvestableWhileRegrowingOrYoungOrDead()
        {
            Scrape();
            _n.Hands.TakeHeld(A);
            Assert.AreEqual("NotHarvestable", Scrape().Message);
            _moss.RegrowTicksLeft = 0;
            _moss.StageIndex = 0;
            Assert.AreEqual("NotHarvestable", Scrape().Message);
            Mature(_moss);
            _moss.IsDead = true;
            Assert.AreEqual("NotHarvestable", Scrape().Message);
        }

        [Test]
        public void HandsFullAndNotFound()
        {
            _n.Hands.TryGive(A, HeldThing.Fresh(_db.ItemDefs["watering_can"]));
            Assert.AreEqual("HandsFull", Scrape().Message);
            _n.Hands.TakeHeld(A);
            Assert.AreEqual("NotFound", _w.Validate(new ScrapeMoss(A, 99999)).Message);
        }

        [Test]
        public void RegrowPausesWhileDry()
        {
            Scrape();
            var left = _moss.RegrowTicksLeft;
            _moss.IsDry = true;
            _w.Step(10);
            Assert.AreEqual(left, _moss.RegrowTicksLeft);
        }

        [Test]
        public void BundleGoesBackToCrate()
        {
            Scrape();
            _w.Submit(new ReturnToCrate(A));
            _w.Step();
            Assert.IsNull(_n.Hands.GetHeld(A));
            Assert.IsTrue(_n.Crate.HasItem("bundle_hearthmoss"));
        }

        [Test]
        public void BundleGoesOntoAStrip()
        {
            _n.Contents.Register(_w);
            Scrape();
            var placed = _n.Build.PlacedItems.First(p => _db.ItemDefs[p.ItemId].Bench != null);
            var addr = SlotAddress.Strip(placed.Id, "bundle_hearthmoss", 2, 0);
            Assert.IsTrue(_w.Validate(new PutInSlot(A, addr)).IsAccepted, _w.Validate(new PutInSlot(A, addr)).Message);
            _w.Submit(new PutInSlot(A, addr));
            _w.Step();
            Assert.IsNull(_n.Hands.GetHeld(A));
            Assert.AreEqual("bundle_hearthmoss", _n.Contents.Get(addr).ItemId);
        }

        [Test]
        public void ScrapeIsTargetedAtTheContainer()
        {
            Assert.AreEqual("container:" + _tray.Id, new ScrapeMoss(A, _tray.Id).TargetKey);
        }
    }
}
