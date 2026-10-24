using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Species stage growth: days, seasons, root-bound (plants.md, Growth).</summary>
    public class SpeciesGrowthSystemTests
    {
        private SimWorld _w;
        private GameClock _clock;
        private ContentDb _db;
        private EntityFactory _factory;
        private readonly List<Container> _containers = new List<Container>();
        private readonly Dictionary<int, Plant> _plants = new Dictionary<int, Plant>();

        private void Setup(int seasonLengthDays)
        {
            _w = new SimWorld();
            _clock = GameClock.Install(_w, new TimeConfig
            {
                DayStartMinute = 360, MorningEndMinute = 540, EveningStartMinute = 1200, NightStartMinute = 1320,
                RealSecondsDay = 600, NightRateMultiplier = 0.25f, SeasonLengthDays = seasonLengthDays
            });
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _factory = new EntityFactory(_w, _db);
            _containers.Clear();
            _plants.Clear();
            SpeciesGrowthSystem.Install(_w, _db, () => _containers, id => _plants.TryGetValue(id, out var p) ? p : null, _clock);
        }

        [SetUp]
        public void SetUp() { Setup(100); }

        private Plant Add(string species, string variety, string containerId)
        {
            var c = _factory.CreateContainer(containerId);
            c.WaterMeter = 100f;
            var p = new Plant(_w.NextEntityId(), species, variety, null, containerId, 0);
            _plants[p.Id] = p;
            c.AddPlant(p.Id);
            _containers.Add(c);
            return p;
        }

        private Plant Chimebell(string container = "pot_small") { return Add("chimebell", "chimebell_white", container); }

        /// <summary>Step with the container kept watered (no water system installed, so Dry is set by hand).</summary>
        private void Step(int n) { _w.Step(n); }

        /// <summary>Replace the plant's container with a fresh one of another kind.</summary>
        private void SwapContainer(Plant p, string containerId)
        {
            var old = _containers.First(c => c.PlantIds.Contains(p.Id));
            old.RemovePlant(p.Id);
            _containers.Remove(old);
            var c = _factory.CreateContainer(containerId);
            c.AddPlant(p.Id);
            _containers.Add(c);
        }

        [Test]
        public void ProgressAdvancesByStageDays()
        {
            var p = Chimebell();
            Step(720); // half a day of a 1-day stage
            Assert.AreEqual(0.5f, p.StageProgress, 0.001f);
            Assert.AreEqual(0, p.StageIndex);
        }

        [Test]
        public void StageChangeRaisesEvent()
        {
            var p = Chimebell();
            Step(1440);
            Assert.AreEqual(1, p.StageIndex);
            Assert.AreEqual(0f, p.StageProgress, 0.001f);
            var ev = _w.DrainEvents().OfType<PlantStageChanged>().ToList();
            Assert.AreEqual(1, ev.Count);
            Assert.AreEqual(p.Id, ev[0].PlantId);
            Assert.AreEqual(0, ev[0].FromIndex);
            Assert.AreEqual(1, ev[0].ToIndex);
            Assert.AreEqual("Young", ev[0].StageName);
        }

        [Test]
        public void ChimebellInSmallPotIsYoungThenRootBound()
        {
            var p = Chimebell();
            Step(1440);
            Assert.AreEqual(1, p.StageIndex); // Young
            Assert.AreEqual(PlantLifecycle.Growing, p.Lifecycle.Current);
            Step(1440);
            Assert.AreEqual(1, p.StageIndex);
            Assert.AreEqual(1f, p.StageProgress, 0.0001f);
            Assert.AreEqual(PlantLifecycle.RootBound, p.Lifecycle.Current);
            Assert.AreEqual(1, _w.DrainEvents().OfType<PlantStageChanged>().Count()); // only the Young change
            Step(5000);
            Assert.AreEqual(1f, p.StageProgress);
            Assert.AreEqual(100f, p.Health); // no other harm
            Assert.IsFalse(p.IsDead);
        }

        [Test]
        public void RootBoundResumesWhenContainerAllowsNextStage()
        {
            var p = Chimebell();
            Step(2880);
            Assert.AreEqual(PlantLifecycle.RootBound, p.Lifecycle.Current);
            _w.DrainEvents();
            SwapContainer(p, "pot_medium");
            Step(1);
            Assert.AreEqual(PlantLifecycle.Growing, p.Lifecycle.Current);
            Assert.AreEqual(2, p.StageIndex);
            Assert.AreEqual("pot_medium", p.ContainerId);
            Assert.AreEqual(1, _w.DrainEvents().OfType<PlantStageChanged>().Count());
        }

        [Test]
        public void FinalStageDoesNotAdvance()
        {
            var p = Chimebell("pot_medium");
            p.StageIndex = 3;
            Step(5000);
            Assert.AreEqual(3, p.StageIndex);
            Assert.AreEqual(0f, p.StageProgress);
            Assert.AreEqual(0, _w.DrainEvents().OfType<PlantStageChanged>().Count());
        }

        [Test]
        public void DryWiltedAndDeadPlantsDoNotGrow()
        {
            var dry = Chimebell(); dry.IsDry = true;
            var wilted = Chimebell(); wilted.IsWilted = true;
            var dead = Chimebell(); dead.IsDead = true;
            Step(100);
            Assert.AreEqual(0f, dry.StageProgress);
            Assert.AreEqual(0f, wilted.StageProgress);
            Assert.AreEqual(0f, dead.StageProgress);
            dry.IsDry = false;
            Step(10);
            Assert.Greater(dry.StageProgress, 0f);
        }

        [Test]
        public void SeasonMultiplierSlowsGrowth()
        {
            Setup(1); // day 1 Spring, 2 Summer, 3 Autumn
            Step(2880); // two days pass, now Autumn starts
            var q = Chimebell();
            Step(1000); // day 3 = Autumn, x0.5
            Assert.AreEqual(Season.Autumn, _clock.Season);
            Assert.AreEqual(500f / 1440f, q.StageProgress, 0.001f);
        }
    }
}
