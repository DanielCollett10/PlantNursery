using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    /// <summary>New nursery from the starter kit (T-134).</summary>
    public class NurseryBuilderTests
    {
        private static ContentDb Db() => ContentLoader.Load(ContentLoaderTests.RealFiles(false));

        private static Nursery Build(int seed, ContentDb db = null) =>
            NurseryBuilder.Build(new SimWorld(0.05, seed), db ?? Db());

        [Test]
        public void PlacesKitFloorItems()
        {
            var db = Db();
            var n = Build(1, db);
            foreach (var p in db.StarterKit.Placements)
            {
                var at = n.Build.GetAt(new Cell(p.X, p.Z));
                Assert.NotNull(at, p.ItemId);
                Assert.AreEqual(p.ItemId, at.ItemId);
            }
            Assert.AreEqual(db.StarterKit.Placements.Count, n.Build.PlacedItems.Count());
            foreach (var id in new[] { "bench_3m", "rain_barrel", "storage_crate", "order_desk", "bed" })
                Assert.AreEqual(1, n.Build.PlacedItems.Count(i => i.ItemId == id), id);
        }

        [Test]
        public void SixSownPlugTraysOnTheBench()
        {
            var n = Build(1);
            var bench = n.Build.PlacedItems.Single(i => i.ItemId == "bench_3m");
            Assert.AreEqual(6, n.Plants.Count);
            for (var col = 0; col < 6; col++)
            {
                var thing = n.Contents.Get(SlotAddress.Planting(bench.Id, "plug_tray", col, 0));
                Assert.NotNull(thing, "slot " + col);
                Assert.AreEqual(HeldKind.Container, thing.Kind);
                Assert.AreEqual(100f, thing.Container.WaterMeter);
                Assert.AreEqual(1, thing.Container.PlantIds.Count);
                var plant = n.Plants[thing.Container.PlantIds[0]];
                Assert.AreEqual("hearthmoss_amber", plant.VarietyId);
                Assert.AreEqual("hearthmoss", plant.SpeciesId);
                Assert.AreEqual(100f, plant.Moisture);
                Assert.NotNull(plant.Genome);
                Assert.AreEqual(SlotRejection.SlotOccupied, n.Slots.CanPlaceInSlot(bench.Id, "plug_tray", col, 0));
            }
            Assert.AreEqual(SlotRejection.None, n.Slots.CanPlaceInSlot(bench.Id, "plug_tray", 6, 0));
        }

        [Test]
        public void FullCanOnStripCrateHoldsTraysPurseIs200()
        {
            var db = Db();
            var n = Build(1, db);
            var bench = n.Build.PlacedItems.Single(i => i.ItemId == "bench_3m");
            var can = n.Contents.Get(SlotAddress.Strip(bench.Id, "watering_can", 0, 0));
            Assert.NotNull(can);
            Assert.AreEqual(HeldKind.Can, can.Kind);
            Assert.AreEqual(db.ItemDefs["watering_can"].WaterCapacity, can.Charges);
            Assert.AreEqual(StripRejection.SlotOccupied, n.Strip.CanPlace(bench.Id, "watering_can", 0, 0, StripContents.NoPlants));
            Assert.AreEqual(2, n.Crate.StockCount("carry_tray"));
            Assert.AreEqual(200, n.Wallet.Balance);
        }

        [Test]
        public void TwoBuildsWithTheSameSeedAreIdentical()
        {
            var db = Db();
            var a = Build(42, db);
            var b = Build(42, db);
            Assert.AreEqual(Describe(a, db), Describe(b, db));
            Assert.AreEqual(a.Plants.Keys.OrderBy(x => x).ToList(), b.Plants.Keys.OrderBy(x => x).ToList());
        }

        private static string Describe(Nursery n, ContentDb db)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var i in n.Build.PlacedItems.OrderBy(i => i.Id))
                sb.Append("P ").Append(i.Id).Append(' ').Append(i.ItemId).Append(' ').Append(i.Anchor.X).Append(',').Append(i.Anchor.Z).Append('\n');
            foreach (var plant in n.Plants.Values.OrderBy(p => p.Id))
            {
                sb.Append("L ").Append(plant.Id).Append(' ').Append(plant.VarietyId).Append(' ').Append(plant.Moisture).Append(' ').Append(plant.Stage);
                foreach (var g in db.SpeciesDefs[plant.SpeciesId].GeneIds)
                {
                    var al = plant.Genome.GetAlleles(g);
                    sb.Append(' ').Append(g).Append('=').Append(al.A).Append('/').Append(al.B).Append('/').Append(al.ValueA).Append('/').Append(al.ValueB);
                }
                sb.Append('\n');
            }
            sb.Append("C ").Append(n.Crate.StockCount("carry_tray")).Append(" W ").Append(n.Wallet.Balance);
            return sb.ToString();
        }
    }
}
