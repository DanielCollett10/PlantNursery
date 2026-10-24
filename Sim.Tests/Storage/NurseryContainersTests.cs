using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Nursery.AllContainers / PlantById (T-150): what the container-water and stage-growth systems tick.</summary>
    public class NurseryContainersTests
    {
        [Test]
        public void AllContainersCoversKitPlantsAndHasNoDuplicates()
        {
            var db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            var n = NurseryBuilder.Build(new SimWorld(0.05, 1), db);
            var all = n.AllContainers().ToList();
            Assert.AreEqual(all.Count, all.Select(c => c.Id).Distinct().Count());
            foreach (var plant in n.Plants.Values)
            {
                Assert.AreSame(plant, n.PlantById(plant.Id));
                Assert.IsTrue(all.Any(c => c.PlantIds.Contains(plant.Id)), "container of plant " + plant.Id);
            }
            Assert.IsNull(n.PlantById(-5));
        }
    }
}
