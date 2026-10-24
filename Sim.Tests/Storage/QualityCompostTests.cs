using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Quality grade, composting dead species plants and the price helper (T-143).</summary>
    public class QualityCompostTests
    {
        private static readonly PlayerId A = new PlayerId(1);
        private ContentDb _db;
        private SimWorld _w;
        private Nursery _n;

        [SetUp]
        public void SetUp()
        {
            _db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            _w = new SimWorld(0.05, 1);
            _n = NurseryBuilder.Build(_w, _db);
            CompostCommands.Register(_w, _n);
        }

        private static Plant PlantWith(int alive, int notDry, int lost = 0) =>
            new Plant(1, "v") { TicksAlive = alive, TicksNotDry = notDry, QualityStepsLost = lost };

        [TestCase(0, 0, QualityGrade.Good)]
        [TestCase(100, 100, QualityGrade.Excellent)]
        [TestCase(100, 90, QualityGrade.Excellent)]
        [TestCase(100, 89, QualityGrade.Good)]
        [TestCase(100, 60, QualityGrade.Good)]
        [TestCase(100, 59, QualityGrade.Poor)]
        [TestCase(100, 0, QualityGrade.Poor)]
        public void CareShareGivesGrade(int alive, int notDry, QualityGrade expected)
        {
            Assert.AreEqual(expected, MossRules.QualityOf(PlantWith(alive, notDry), _db.Economy));
        }

        [TestCase(100, 100, 1, QualityGrade.Good)]
        [TestCase(100, 100, 2, QualityGrade.Poor)]
        [TestCase(100, 100, 5, QualityGrade.Poor)]
        [TestCase(100, 70, 1, QualityGrade.Poor)]
        [TestCase(0, 0, 1, QualityGrade.Poor)]
        [TestCase(0, 0, 0, QualityGrade.Good)]
        public void StepsLostLowerQualityNeverBelowPoor(int alive, int notDry, int lost, QualityGrade expected)
        {
            Assert.AreEqual(expected, MossRules.QualityOf(PlantWith(alive, notDry, lost), _db.Economy));
        }

        [Test]
        public void PriceIsListedTimesMultiplierRoundedDown()
        {
            var e = _db.Economy;
            Assert.AreEqual(10, PlantPricing.PriceFor(10, QualityGrade.Good, e));
            Assert.AreEqual(14, PlantPricing.PriceFor(10, QualityGrade.Excellent, e));
            Assert.AreEqual(6, PlantPricing.PriceFor(10, QualityGrade.Poor, e));
            Assert.AreEqual(7, PlantPricing.PriceFor(5, QualityGrade.Excellent, e)); // 7.0, not 6.99 from float
            Assert.AreEqual(19, PlantPricing.PriceFor(14, QualityGrade.Excellent, e)); // 19.6 rounds down
            Assert.AreEqual(1, PlantPricing.PriceFor(3, QualityGrade.Poor, e)); // 1.8 rounds down
            Assert.AreEqual(0, PlantPricing.PriceFor(0, QualityGrade.Excellent, e));
        }

        private (Container tray, Plant plant) FirstPlanted()
        {
            var tray = _n.Contents.Things.Where(t => t.Kind == HeldKind.Container).Select(t => t.Container)
                .First(c => c.PlantIds.Count > 0);
            var second = new SpeciesPlantFactory(_w, _db).Create("hearthmoss", "hearthmoss_amber", "plug_tray", 1);
            _n.PlantMap[second.Id] = second;
            tray.AddPlant(second.Id);
            return (tray, _n.Plants[tray.PlantIds[0]]);
        }

        [Test]
        public void CompostRejectsLivingPlantAndUnknownPlant()
        {
            var (tray, plant) = FirstPlanted();
            var live = _w.Validate(new CompostSpecies(A, plant.Id));
            Assert.IsFalse(live.IsAccepted);
            Assert.AreEqual("NotDead", live.Message);
            var unknown = _w.Validate(new CompostSpecies(A, 99999));
            Assert.AreEqual("NotFound", unknown.Message);
            Assert.IsTrue(tray.PlantIds.Contains(plant.Id));
        }

        [Test]
        public void CompostClearsOnlyTheDeadPlantAndFreesItsCell()
        {
            var (tray, plant) = FirstPlanted();
            var other = tray.PlantIds[1];
            var count = tray.PlantIds.Count;
            plant.IsDead = true;
            _w.Submit(new CompostSpecies(A, plant.Id));
            _w.Step();
            Assert.AreEqual(count - 1, tray.PlantIds.Count);
            Assert.IsFalse(tray.PlantIds.Contains(plant.Id));
            Assert.IsTrue(tray.PlantIds.Contains(other));
            Assert.IsFalse(_n.Plants.ContainsKey(plant.Id));
            Assert.AreEqual(1, _w.DrainEvents().OfType<SpeciesPlantComposted>().Count(e => e.PlantId == plant.Id && e.ContainerId == tray.Id));
        }

        [Test]
        public void CompostedPlantCannotBeComposted()
        {
            var (_, plant) = FirstPlanted();
            plant.IsDead = true;
            _w.Submit(new CompostSpecies(A, plant.Id));
            _w.Step();
            Assert.AreEqual("NotFound", _w.Validate(new CompostSpecies(A, plant.Id)).Message);
        }
    }
}
