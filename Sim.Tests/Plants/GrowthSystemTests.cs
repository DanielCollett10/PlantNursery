using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    public class GrowthSystemTests
    {
        private SimWorld _world;
        private HandsState _hands;
        private SurfaceItem _tray;
        private GrowthSystem _growth;
        private ContentDb _db;

        private static ContentDb LoadReal()
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
            return ContentLoader.Load(files);
        }

        // Small config: 1 growth/minute at full conditions, stages every 2 growth points
        // (Seed 0, Seedling 2, Rooted 4, ... Mature 10). Tray caps at Rooted (4), dries 1 moisture/minute.
        // Seasons: Spring 1.0, Summer 0.5, Autumn 0; each season lasts 1 day.
        private void Build(float dryPerHour = 60f, GrowthConfig cfg = null)
        {
            var real = LoadReal();
            var variety = new VarietyDef
            {
                Id = "test", Name = "Test", GrowthPerHour = 60f,
                StageThresholds = new float[] { 0, 2, 4, 6, 8, 10 },
                SeasonGrowth = new float[] { 1f, 0.5f, 0f },
                Genes = real.VarietyDefs.Values.First().Genes
            };
            var tray = new ContainerDef
            {
                Id = "plug_tray", ItemId = "plug_tray", CellCount = 6, MaxStage = PlantStage.Rooted,
                AcceptsFromStage = PlantStage.Seed, DryRatePerHour = dryPerHour
            };
            var t = real.Time;
            var time = new TimeConfig
            {
                DayStartMinute = t.DayStartMinute, NightStartMinute = t.NightStartMinute,
                MorningEndMinute = t.MorningEndMinute, EveningStartMinute = t.EveningStartMinute,
                RealSecondsDay = t.RealSecondsDay, NightRateMultiplier = t.NightRateMultiplier,
                SeasonLengthDays = 1, ShopOpenMinute = t.ShopOpenMinute, ShopCloseMinute = t.ShopCloseMinute
            };
            _db = new ContentDb(time, real.ItemDefs,
                new Dictionary<string, ContainerDef> { { "plug_tray", tray } },
                new Dictionary<string, VarietyDef> { { "test", variety } },
                real.GeneDefs, real.Economy, real.Building);

            var state = new BuildState(_db);
            state.Inventory.Add("bench_standard");
            var grid = new SurfaceGrid(_db, state);
            state.Place("bench_standard", ItemLayer.Floor, new Cell(10, 10), Rotation.Deg0, out _);
            _hands = new HandsState(_db, grid);
            _world = new SimWorld();
            var clock = GameClock.Install(_world, time);
            HandsModule.Register(_world, _hands);
            _growth = GrowthSystem.Install(_world, _db, _hands, clock, cfg);
            _tray = grid.Place("plug_tray", new Unit(50, 50), Rotation.Deg0, out _);
        }

        private Plant AddPlant(float moisture = 100f, int id = 1)
        {
            var p = new Plant(id, "test") { Moisture = moisture, CellIndex = id - 1 };
            _hands.AddPlant(_tray, p);
            return p;
        }

        [Test]
        public void MoistureDriesByDryRateOverSixtyAndFloorsAtZero()
        {
            Build(dryPerHour: 6f);
            var p = AddPlant(1f);
            _world.Step();
            Assert.AreEqual(0.9f, p.Moisture, 1e-4);
            _world.Step(20);
            Assert.AreEqual(0f, p.Moisture);
        }

        [Test]
        public void WaterFactorFullHalfAndZero()
        {
            Build(dryPerHour: 0f);
            var full = AddPlant(30f, 1);
            var dry = AddPlant(29f, 2);
            var wilt = AddPlant(0f, 3);
            _world.Step();
            Assert.AreEqual(1f, full.Growth, 1e-4);
            Assert.AreEqual(0.5f, dry.Growth, 1e-4);
            Assert.AreEqual(0f, wilt.Growth, 1e-4);
        }

        [Test]
        public void SeasonFactorComesFromVariety()
        {
            Build(dryPerHour: 0f);
            var p = AddPlant();
            _world.Step();
            Assert.AreEqual(1f, p.Growth, 1e-4); // spring
            _world.Step(1439);                   // tick 1440 = summer day 2
            p.Growth = 0f; p.Stage = PlantStage.Seed;
            var before = p.Growth;
            _world.Step();
            Assert.AreEqual(0.5f, p.Growth - before, 1e-4);
            _world.Step(1439);                   // autumn
            p.Growth = 0f; p.Stage = PlantStage.Seed;
            before = p.Growth;
            _world.Step();
            Assert.AreEqual(0f, p.Growth - before, 1e-4);
        }

        [Test]
        public void GrowthCapsAtContainerCapAndStageChangesRaiseEvents()
        {
            Build(dryPerHour: 0f);
            var p = AddPlant();
            _world.Step(10);
            Assert.AreEqual(4f, p.Growth, 1e-4);
            Assert.AreEqual(PlantStage.Rooted, p.Stage);
            var changes = _world.DrainEvents().OfType<PlantStageChanged>().ToList();
            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(PlantStage.Seed, changes[0].From);
            Assert.AreEqual(PlantStage.Seedling, changes[0].To);
            Assert.AreEqual(PlantStage.Rooted, changes[1].To);
            Assert.AreEqual(p.Id, changes[1].PlantId);
        }

        [Test]
        public void RootBoundCountsThenLosesHealthAfterGrace()
        {
            Build(dryPerHour: 0f, cfg: new GrowthConfig { RootBoundGraceHours = 1f, RootBoundHealthLossPerHour = 6f, RecoverPerHour = 0f });
            var p = AddPlant();
            _world.Step(4); // reaches Rooted on tick 4, rootBound 1
            Assert.AreEqual(1, p.RootBoundMinutes);
            _world.Step(59); // rootBound 60: still within grace
            Assert.AreEqual(60, p.RootBoundMinutes);
            Assert.AreEqual(100f, p.Health, 1e-3);
            _world.Step(10); // 10 minutes past grace at 0.1/min
            Assert.AreEqual(99f, p.Health, 1e-2);
        }

        [Test]
        public void WiltingLosesHealthAndDeathStopsUpdates()
        {
            Build(dryPerHour: 0f, cfg: new GrowthConfig { WiltHealthLossPerHour = 60f });
            var p = AddPlant(0f);
            p.Health = 3f;
            _world.Step(2);
            Assert.AreEqual(1f, p.Health, 1e-3);
            Assert.IsFalse(p.IsDead);
            _world.Step();
            Assert.IsTrue(p.IsDead);
            Assert.AreEqual(0f, p.Health);
            Assert.AreEqual(1, _world.DrainEvents().OfType<PlantDied>().Count());
            var alive = p.AliveMinutes;
            _world.Step(5);
            Assert.AreEqual(alive, p.AliveMinutes);
            Assert.AreEqual(0, _world.DrainEvents().OfType<PlantDied>().Count());
        }

        [Test]
        public void RecoversWhenWellWateredUpToHundred()
        {
            Build(dryPerHour: 0f, cfg: new GrowthConfig { RecoverPerHour = 60f });
            var p = AddPlant(50f);
            p.Health = 98.5f;
            _world.Step();
            Assert.AreEqual(99.5f, p.Health, 1e-3);
            _world.Step(3);
            Assert.AreEqual(100f, p.Health);
        }

        [Test]
        public void NoRecoveryWhileDry()
        {
            Build(dryPerHour: 0f);
            var p = AddPlant(10f);
            p.Health = 50f;
            _world.Step(10);
            Assert.AreEqual(50f, p.Health, 1e-4);
        }

        [Test]
        public void CareCountersAndGrades()
        {
            Build(dryPerHour: 0f);
            var good = AddPlant(100f, 1);
            var mixed = AddPlant(100f, 2);
            _world.Step(10);
            mixed.Moisture = 10f; // dry: not good care
            _world.Step(10);
            Assert.AreEqual(20, good.AliveMinutes);
            Assert.AreEqual(20, good.GoodCareMinutes);
            Assert.AreEqual(1f, GrowthSystem.CareScore(good), 1e-4);
            Assert.AreEqual(CareGrade.A, _growth.GradeOf(good));
            Assert.AreEqual(10, mixed.GoodCareMinutes);
            Assert.AreEqual(0.5f, GrowthSystem.CareScore(mixed), 1e-4);
            Assert.AreEqual(CareGrade.C, _growth.GradeOf(mixed));
        }

        [Test]
        public void GradeBoundaries()
        {
            var c = new GrowthConfig();
            Assert.AreEqual(CareGrade.A, c.GradeFor(0.9f));
            Assert.AreEqual(CareGrade.B, c.GradeFor(0.89f));
            Assert.AreEqual(CareGrade.B, c.GradeFor(0.7f));
            Assert.AreEqual(CareGrade.C, c.GradeFor(0.69f));
        }

        [Test]
        public void HeldTrayPlantsAlsoGrow()
        {
            Build(dryPerHour: 0f);
            var p = AddPlant();
            _hands.PickUp(new PlayerId(1), _tray.Id);
            _world.Step();
            Assert.AreEqual(1f, p.Growth, 1e-4);
        }
    }
}
