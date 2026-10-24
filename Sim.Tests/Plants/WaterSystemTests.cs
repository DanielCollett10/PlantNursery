using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Container water drain, Dry, Wilted, Dead and care counters (plants.md, Watering).</summary>
    public class WaterSystemTests
    {
        private SimWorld _w;
        private GameClock _clock;
        private ContentDb _db;
        private EntityFactory _factory;
        private readonly List<Container> _containers = new List<Container>();
        private readonly Dictionary<int, Plant> _plants = new Dictionary<int, Plant>();

        [SetUp]
        public void SetUp()
        {
            _w = new SimWorld();
            _clock = GameClock.Install(_w, new TimeConfig
            {
                DayStartMinute = 360, MorningEndMinute = 540, EveningStartMinute = 1200, NightStartMinute = 1320,
                RealSecondsDay = 600, NightRateMultiplier = 0.25f, SeasonLengthDays = 3
            });
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _factory = new EntityFactory(_w, _db);
            _containers.Clear();
            _plants.Clear();
            WaterSystem.Install(_w, () => _containers, id => _plants.TryGetValue(id, out var p) ? p : null, _clock);
        }

        private Container Pot(float meter, bool species = true)
        {
            var c = _factory.CreateContainer("pot_small");
            c.WaterMeter = meter;
            var id = _w.NextEntityId();
            var p = species ? new Plant(id, "sp", "v", null, "pot_small", 0) : new Plant(id, "v");
            _plants[id] = p;
            c.AddPlant(id);
            _containers.Add(c);
            return c;
        }

        private Plant PlantOf(Container c) { return _plants[c.PlantIds[0]]; }

        [Test]
        public void DrainsByDayRateAndFloorsAtZero()
        {
            var c = Pot(100f);
            _w.Step(60); // 06:00 to 07:00, daytime: 6 per hour
            Assert.AreEqual(94f, c.WaterMeter, 0.01f);
            var d = Pot(0.05f);
            _w.Step(1);
            Assert.AreEqual(0f, d.WaterMeter);
        }

        [Test]
        public void SleepAt2200LosesTwelveWaterFromSmallPot()
        {
            var c = Pot(100f);
            _w.Step(16 * 60); // 22:00
            Assert.AreEqual(1320, _clock.MinuteOfDay);
            c.WaterMeter = 100f;
            _w.Submit(new SleepCommand(new PlayerId(0)));
            _w.Step();
            Assert.AreEqual(360, _clock.MinuteOfDay);
            Assert.AreEqual(88f, c.WaterMeter, 0.05f); // 8 h x 6 x 0.25 = 12
        }

        [Test]
        public void CountersTrackAliveAndNotDry()
        {
            var c = Pot(0.1f); // dries on the first tick
            var p = PlantOf(c);
            _w.Step(10);
            Assert.AreEqual(10, p.TicksAlive);
            Assert.AreEqual(0, p.TicksNotDry);
            Assert.IsTrue(p.IsDry);
            c.WaterMeter = 100f;
            _w.Step(5);
            Assert.AreEqual(15, p.TicksAlive);
            Assert.AreEqual(5, p.TicksNotDry);
            Assert.IsFalse(p.IsDry);
        }

        [Test]
        public void WiltsAfterTwelveDryHoursAndRaisesEventOnce()
        {
            var c = Pot(0f);
            var p = PlantOf(c);
            _w.Step(12 * 60 - 1);
            Assert.AreEqual(PlantLifecycle.Growing, p.Lifecycle.Current);
            _w.Step(1);
            Assert.AreEqual(PlantLifecycle.Wilted, p.Lifecycle.Current);
            _w.Step(100);
            Assert.AreEqual(1, _w.DrainEvents().OfType<PlantWilted>().Count());
        }

        [Test]
        public void DiesAfterThreeWiltedDays()
        {
            var c = Pot(0f);
            var p = PlantOf(c);
            _w.Step(12 * 60 + 3 * 1440 - 1);
            Assert.IsFalse(p.IsDead);
            _w.Step(1);
            Assert.IsTrue(p.IsDead);
            Assert.AreEqual(PlantLifecycle.Dead, p.Lifecycle.Current);
            var died = _w.DrainEvents().OfType<PlantDied>().ToList();
            Assert.AreEqual(1, died.Count);
            var alive = p.TicksAlive;
            _w.Step(10);
            Assert.AreEqual(alive, p.TicksAlive);
            Assert.AreEqual(0, _w.DrainEvents().OfType<PlantDied>().Count());
        }

        [Test]
        public void WateringClearsWiltedAndResetsCounters()
        {
            var c = Pot(0f);
            var p = PlantOf(c);
            _w.Step(13 * 60);
            Assert.AreEqual(PlantLifecycle.Wilted, p.Lifecycle.Current);
            c.WaterMeter = 100f;
            _w.Step(1);
            Assert.AreEqual(PlantLifecycle.Growing, p.Lifecycle.Current);
            Assert.AreEqual(0, p.DryTicks);
            Assert.AreEqual(0, p.WiltedTicks);
            c.WaterMeter = 0f;
            _w.Step(12 * 60 - 1); // dry counter restarted, so not wilted yet
            Assert.AreEqual(PlantLifecycle.Growing, p.Lifecycle.Current);
        }

        [Test]
        public void PlantWithoutLifecycleStillDies()
        {
            var c = Pot(0f, species: false);
            var p = PlantOf(c);
            _w.Step(12 * 60 + 3 * 1440);
            Assert.IsTrue(p.IsDead);
        }
    }
}
