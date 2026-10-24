using System.Collections.Generic;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using PlantNursery.Sim.Targeting;

namespace PlantNursery.Sim.Tests
{
    /// <summary>SmartTarget.Pick and the Water/Scrape strategies (controls.md, Targeting; coop.md pending claims).</summary>
    public class SmartTargetTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private static readonly PlayerId B = new PlayerId(2);
        private ContentDb _db;
        private SimWorld _w;
        private EntityFactory _factory;
        private SpeciesPlantFactory _plants;
        private readonly Dictionary<int, Plant> _byId = new Dictionary<int, Plant>();
        private ScrapeTargetStrategy _scrape;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _w = new SimWorld(0.05, 1);
            _factory = new EntityFactory(_w, _db);
            _plants = new SpeciesPlantFactory(_w, _db);
            _byId.Clear();
            _scrape = new ScrapeTargetStrategy(_db, id => _byId.TryGetValue(id, out var p) ? p : null);
        }

        private Container Box(float meter, string containerId = "pot_small")
        {
            var c = _factory.CreateContainer(containerId);
            c.WaterMeter = meter;
            return c;
        }

        private Plant AddPlant(Container c, bool matureHearthmoss)
        {
            var p = _plants.Create("hearthmoss", "hearthmoss_amber", "plug_tray", c.PlantIds.Count);
            if (matureHearthmoss) p.StageIndex = _db.SpeciesDefs["hearthmoss"].Stages.Count - 1;
            _byId[p.Id] = p;
            c.AddPlant(p.Id);
            return p;
        }

        private static TargetCandidate Cand(Container c, double metres) => TargetCandidate.ForContainer(c, metres);

        private static int PickId(ISmartTargetStrategy s, PlayerId p, SimWorld w, params TargetCandidate[] cands)
        {
            var picked = SmartTarget.Pick(s, p, cands, w);
            return picked == null ? -1 : picked.EntityId;
        }

        // ---- Water ----

        [Test]
        public void WaterPicksLowestMeterEvenWhenFarther()
        {
            var wet = Box(60f);
            var dry = Box(10f);
            var picked = SmartTarget.Pick(new WaterTargetStrategy(), A,
                new[] { Cand(wet, 0.3), Cand(dry, 1.2) }, _w);
            Assert.AreSame(dry, picked.Container);
        }

        [Test]
        public void WaterTieOnMeterPicksNearest()
        {
            var far = Box(20f);
            var near = Box(20f);
            Assert.AreEqual(near.Id, PickId(new WaterTargetStrategy(), A, _w, Cand(far, 1.0), Cand(near, 0.4)));
        }

        [Test]
        public void WaterTieOnMeterAndDistancePicksLowestId()
        {
            var first = Box(20f);
            var second = Box(20f);
            // Candidates passed in reverse id order, so the result cannot come from list order.
            Assert.AreEqual(first.Id, PickId(new WaterTargetStrategy(), A, _w, Cand(second, 0.5), Cand(first, 0.5)));
        }

        [Test]
        public void WaterZeroMeterIsTheDriest()
        {
            var empty = Box(0f);
            var nearlyEmpty = Box(0.5f);
            Assert.AreEqual(empty.Id, PickId(new WaterTargetStrategy(), A, _w, Cand(nearlyEmpty, 0.1), Cand(empty, 1.4)));
        }

        [Test]
        public void WaterWithNoCandidatesReturnsNull()
        {
            Assert.IsNull(SmartTarget.Pick(new WaterTargetStrategy(), A, new TargetCandidate[0], _w));
        }

        [Test]
        public void WaterSkipsContainerAnotherPlayerIsWatering()
        {
            var driest = Box(5f);
            var next = Box(30f);
            _w.Submit(new WaterContainer(B, driest.Id)); // B's queued claim, not yet applied
            Assert.AreEqual(next.Id, PickId(new WaterTargetStrategy(), A, _w, Cand(driest, 0.2), Cand(next, 0.9)));
            // B's own claim does not exclude B's target.
            Assert.AreEqual(driest.Id, PickId(new WaterTargetStrategy(), B, _w, Cand(driest, 0.2), Cand(next, 0.9)));
        }

        [Test]
        public void ClaimIsReleasedOnceTheCommandIsApplied()
        {
            var driest = Box(5f);
            var next = Box(30f);
            _w.Submit(new WaterContainer(B, driest.Id));
            _w.Step(); // no handler registered here: the command is rejected and leaves the queue
            Assert.AreEqual(driest.Id, PickId(new WaterTargetStrategy(), A, _w, Cand(driest, 0.2), Cand(next, 0.9)));
        }

        // ---- Scrape ----

        [Test]
        public void ScrapeIgnoresNearerContainerWithoutHarvestableMoss()
        {
            var seedling = Box(50f, "plug_tray");
            AddPlant(seedling, matureHearthmoss: false);
            var ready = Box(50f, "plug_tray");
            AddPlant(ready, matureHearthmoss: true);
            var picked = SmartTarget.Pick(_scrape, A, new[] { Cand(seedling, 0.2), Cand(ready, 1.1) }, _w);
            Assert.AreSame(ready, picked.Container);
        }

        [Test]
        public void ScrapePicksNearestHarvestable()
        {
            var a = Box(50f, "plug_tray"); AddPlant(a, true);
            var b = Box(50f, "plug_tray"); AddPlant(b, true);
            Assert.AreEqual(b.Id, PickId(_scrape, A, _w, Cand(a, 1.0), Cand(b, 0.5)));
        }

        [Test]
        public void ScrapeTieOnDistancePicksLowestId()
        {
            var first = Box(50f, "plug_tray"); AddPlant(first, true);
            var second = Box(50f, "plug_tray"); AddPlant(second, true);
            Assert.AreEqual(first.Id, PickId(_scrape, A, _w, Cand(second, 0.7), Cand(first, 0.7)));
        }

        [Test]
        public void ScrapeTwoPlantTrayIsEligibleWhenOnePlantIsHarvestable()
        {
            var tray = Box(50f, "plug_tray");
            AddPlant(tray, matureHearthmoss: false);
            AddPlant(tray, matureHearthmoss: true);
            var other = Box(50f, "plug_tray"); AddPlant(other, true);
            Assert.AreEqual(tray.Id, PickId(_scrape, A, _w, Cand(tray, 0.3), Cand(other, 0.8)));
        }

        [Test]
        public void ScrapeNoHarvestableMossReturnsNull()
        {
            var empty = Box(50f, "plug_tray");
            var seedling = Box(50f, "plug_tray"); AddPlant(seedling, false);
            var regrowing = Box(50f, "plug_tray");
            AddPlant(regrowing, true).RegrowTicksLeft = 100;
            var dead = Box(50f, "plug_tray");
            AddPlant(dead, true).IsDead = true;
            Assert.IsNull(SmartTarget.Pick(_scrape, A,
                new[] { Cand(empty, 0.1), Cand(seedling, 0.2), Cand(regrowing, 0.3), Cand(dead, 0.4) }, _w));
        }

        [Test]
        public void ScrapeSkipsContainerAnotherPlayerIsScraping()
        {
            var first = Box(50f, "plug_tray"); AddPlant(first, true);
            var second = Box(50f, "plug_tray"); AddPlant(second, true);
            _w.Submit(new ScrapeMoss(B, first.Id));
            Assert.AreEqual(second.Id, PickId(_scrape, A, _w, Cand(first, 0.2), Cand(second, 0.9)));
        }

        // ---- Extending: a new action is a new strategy; the rule in SmartTarget does not change ----

        [Test]
        public void AnyStrategyCanBePassedToPick()
        {
            var a = Box(10f);
            var b = Box(90f);
            Assert.AreEqual(b.Id, PickId(new LargestMeterStrategy(), A, _w, Cand(a, 0.1), Cand(b, 0.9)));
        }

        private sealed class LargestMeterStrategy : ISmartTargetStrategy
        {
            public bool IsEligible(TargetCandidate c) => c.Container != null;
            public int Compare(TargetCandidate a, TargetCandidate b) => b.Container.WaterMeter.CompareTo(a.Container.WaterMeter);
        }
    }
}
