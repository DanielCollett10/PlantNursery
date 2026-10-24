using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    /// <summary>WaterContainer and RefillCan (plants.md, Watering) on a starter nursery.</summary>
    public class WateringCommandsTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _w;
        private Nursery _n;
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _w = new SimWorld(0.05, 1);
            _n = NurseryBuilder.Build(_w, _db);
            _factory = new EntityFactory(_w, _db);
            WaterCommands.Register(_w, _n, _db);
            var clock = GameClock.Install(_w, new TimeConfig
            {
                DayStartMinute = 360, MorningEndMinute = 540, EveningStartMinute = 1200, NightStartMinute = 1320,
                RealSecondsDay = 600, NightRateMultiplier = 0.25f, SeasonLengthDays = 3
            });
            WaterSystem.Install(_w, Containers, id => _n.Plants.TryGetValue(id, out var p) ? p : null, clock);
        }

        private IEnumerable<Container> Containers() =>
            _n.Contents.Things.Where(t => t.Kind == HeldKind.Container).Select(t => t.Container)
                .Concat(_n.Crate.Containers);

        private Container PlugTray() => Containers().First(c => c.Def.ItemId == "plug_tray");

        private Plant PlantOf(Container c) => _n.Plants[c.PlantIds[0]];

        private HeldThing GiveCan(int charges = -1)
        {
            var can = HeldThing.Fresh(_db.ItemDefs["watering_can"]);
            if (charges >= 0) can.Charges = charges;
            Assert.IsTrue(_n.Hands.TryGive(A, can));
            return can;
        }

        private CommandResult Run(ICommand c)
        {
            var r = _w.Validate(c);
            _w.Submit(c);
            _w.Step();
            return r;
        }

        private int PlacedBarrelId() => _n.Build.PlacedItems.Single(i => i.ItemId == "rain_barrel").Id;

        [Test]
        public void WaterSetsMeterTo100AndUsesOneChargeForATray()
        {
            var tray = PlugTray();
            tray.WaterMeter = 10f;
            var can = GiveCan(10);
            var r = Run(new WaterContainer(A, tray.Id));
            Assert.IsTrue(r.IsAccepted, r.Message);
            Assert.AreEqual(100f, tray.WaterMeter, 0.5f); // one tick of drain after the watering
            Assert.AreEqual(9, can.Charges);
        }

        [Test]
        public void LargePotUsesTwoCharges()
        {
            var pot = _factory.CreateContainer("pot_large");
            _n.Crate.AddContainer(pot);
            var can = GiveCan(10);
            Assert.IsTrue(Run(new WaterContainer(A, pot.Id)).IsAccepted);
            Assert.AreEqual(8, can.Charges);
            Assert.AreEqual(100f, pot.WaterMeter, 0.5f);
        }

        [Test]
        public void LargePotWithOneChargeIsCanEmptyAndNothingChanges()
        {
            var pot = _factory.CreateContainer("pot_large");
            pot.WaterMeter = 5f;
            _n.Crate.AddContainer(pot);
            var can = GiveCan(1);
            var r = Run(new WaterContainer(A, pot.Id));
            Assert.IsFalse(r.IsAccepted);
            Assert.AreEqual("CanEmpty", r.Message);
            Assert.AreEqual(1, can.Charges);
            Assert.Less(pot.WaterMeter, 5f); // only the drain happened
        }

        [Test]
        public void WithoutACanIsNoCan()
        {
            var r = Run(new WaterContainer(A, PlugTray().Id));
            Assert.AreEqual("NoCan", r.Message);
        }

        [Test]
        public void HoldingANonCanIsNoCan()
        {
            _n.Hands.TryGive(A, HeldThing.Fresh(_db.ItemDefs["carry_tray"]));
            Assert.AreEqual("NoCan", Run(new WaterContainer(A, PlugTray().Id)).Message);
        }

        [Test]
        public void UnknownContainerIsNotFound()
        {
            GiveCan(10);
            Assert.AreEqual("NotFound", Run(new WaterContainer(A, 99999)).Message);
        }

        [Test]
        public void WaterContainerTargetsTheContainer()
        {
            ITargetsEntity cmd = new WaterContainer(A, 7);
            Assert.AreEqual("container:7", cmd.TargetKey);
            Assert.AreEqual(new WaterContainer(A, 7).TargetKey, new WaterContainer(new PlayerId(2), 7).TargetKey);
        }

        [Test]
        public void WateringRaisesContainerWateredOnce()
        {
            var tray = PlugTray();
            GiveCan(10);
            Run(new WaterContainer(A, tray.Id));
            var events = _w.DrainEvents().OfType<ContainerWatered>().ToList();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(tray.Id, events[0].ContainerId);
            Assert.AreEqual(A, events[0].PlayerId);
        }

        [Test]
        public void WateringAWiltedPlantClearsWiltedNextTick()
        {
            var tray = PlugTray();
            var plant = PlantOf(tray);
            tray.WaterMeter = 0f;
            _w.Step(13 * 60); // 13 dry hours: past the 12 h wilt threshold
            Assert.AreEqual(PlantLifecycle.Wilted, plant.Lifecycle.Current);
            Assert.IsTrue(plant.IsWilted);
            GiveCan(10);
            Assert.IsTrue(Run(new WaterContainer(A, tray.Id)).IsAccepted);
            Assert.IsFalse(plant.IsWilted);
            Assert.IsFalse(plant.IsDry);
            Assert.AreEqual(PlantLifecycle.Growing, plant.Lifecycle.Current);
        }

        [Test]
        public void RefillFillsTheCanToCapacity()
        {
            var can = GiveCan(0);
            Assert.IsTrue(Run(new RefillCan(A, PlacedBarrelId())).IsAccepted);
            Assert.AreEqual(10, can.Charges);
            Assert.AreEqual(_db.ItemDefs["watering_can"].WaterCapacity, can.Charges);
        }

        [Test]
        public void RefillWithoutACanIsNoCan()
        {
            Assert.AreEqual("NoCan", Run(new RefillCan(A, PlacedBarrelId())).Message);
        }

        [Test]
        public void RefillAtANonBarrelIsNotABarrel()
        {
            var can = GiveCan(2);
            var bench = _n.Build.PlacedItems.First(i => i.ItemId != "rain_barrel");
            Assert.AreEqual("NotABarrel", Run(new RefillCan(A, bench.Id)).Message);
            Assert.AreEqual("NotABarrel", Run(new RefillCan(A, 99999)).Message);
            Assert.AreEqual(2, can.Charges);
        }
    }
}
