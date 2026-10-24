using System.Collections.Generic;
using NUnit.Framework;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Bench planting area and working strip data (docs/design/building.md).</summary>
    public class BenchContentTests
    {
        // Index of bench_3m in items.json (0-based, counting every item). Used only in error-field assertions.
        private const int Bench3mIndex = 16;

        private static ContentException Fails(Dictionary<string, string> files)
        {
            return Assert.Throws<ContentException>(() => ContentLoader.Load(files));
        }

        [Test]
        public void BenchesHavePlantingAreaAndStrip()
        {
            var db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            AssertBench(db.ItemDefs["bench_3m"].Bench, 30);
            AssertBench(db.ItemDefs["bench_4m"].Bench, 40);
        }

        private static void AssertBench(BenchDef bench, int lengthUnits)
        {
            Assert.IsNotNull(bench);
            Assert.AreEqual(lengthUnits, bench.PlantingLengthUnits);
            Assert.AreEqual(6, bench.PlantingDepthUnits);
            Assert.AreEqual(0, bench.PlantingBackOffsetUnits);
            Assert.AreEqual(4, bench.StripDepthUnits);
        }

        [Test]
        public void StripAcceptsMatchBuildingTable()
        {
            var db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            var expected = new Dictionary<string, Size2>
            {
                { "pot_small", new Size2(2, 2) },
                { "pot_medium", new Size2(3, 3) },
                { "plug_tray", new Size2(6, 4) },
                { "carry_tray", new Size2(6, 4) },
                { "watering_can", new Size2(3, 3) },
                { "bundle_hearthmoss", new Size2(2, 2) },
            };
            foreach (var benchId in new[] { "bench_3m", "bench_4m" })
            {
                var accepts = db.ItemDefs[benchId].Bench.StripAccepts;
                Assert.AreEqual(expected.Count, accepts.Count, benchId);
                for (int i = 0; i < accepts.Count; i++)
                {
                    var size = expected[accepts[i].ItemId];
                    Assert.AreEqual(size.W, accepts[i].StripUnits.W, benchId + " " + accepts[i].ItemId);
                    Assert.AreEqual(size.D, accepts[i].StripUnits.D, benchId + " " + accepts[i].ItemId);
                }
            }
        }

        [Test]
        public void NonBenchItemsHaveNoBench()
        {
            var db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            Assert.IsNull(db.ItemDefs["pot_small"].Bench);
            Assert.IsNull(db.ItemDefs["bench_standard"].Bench);
        }

        [Test]
        public void AcceptedItemDeeperThanStripFails()
        {
            var files = ContentLoaderTests.RealFiles(false);
            var changed = files["items.json"].Replace(
                "{ \"itemId\": \"bundle_hearthmoss\", \"stripUnits\": { \"w\": 2, \"d\": 2 } }",
                "{ \"itemId\": \"bundle_hearthmoss\", \"stripUnits\": { \"w\": 2, \"d\": 5 } }");
            Assert.AreNotEqual(files["items.json"], changed, "Replace must change the json");
            files["items.json"] = changed;
            var e = Fails(files);
            Assert.AreEqual("items.json", e.File);
            Assert.AreEqual("items[" + Bench3mIndex + "].workStrip.accepts[5].stripUnits", e.Field);
            StringAssert.Contains("deeper than the strip", e.Message);
        }

        [Test]
        public void AcceptedItemExactlyFillingStripLoads()
        {
            var files = ContentLoaderTests.RealFiles(false);
            var changed = files["items.json"].Replace(
                "{ \"itemId\": \"bundle_hearthmoss\", \"stripUnits\": { \"w\": 2, \"d\": 2 } }",
                "{ \"itemId\": \"bundle_hearthmoss\", \"stripUnits\": { \"w\": 2, \"d\": 4 } }");
            Assert.AreNotEqual(files["items.json"], changed, "Replace must change the json");
            files["items.json"] = changed;
            Assert.DoesNotThrow(() => ContentLoader.Load(files));
        }

        [Test]
        public void UnknownAcceptedItemFails()
        {
            var files = ContentLoaderTests.RealFiles(false);
            var changed = files["items.json"].Replace("{ \"itemId\": \"pot_small\"", "{ \"itemId\": \"teapot\"");
            Assert.AreNotEqual(files["items.json"], changed, "Replace must change the json");
            files["items.json"] = changed;
            var e = Fails(files);
            Assert.AreEqual("items.json", e.File);
            Assert.AreEqual("items[" + Bench3mIndex + "].workStrip.accepts[0].itemId", e.Field);
            StringAssert.Contains("unknown item id 'teapot'", e.Message);
        }

        [Test]
        public void WorkStripWithoutPlantingFails()
        {
            var files = ContentLoaderTests.RealFiles(false);
            var changed = files["items.json"].Replace(
                "\"planting\": { \"lengthUnits\": 30, \"depthUnits\": 6, \"backOffsetUnits\": 0 },", "");
            Assert.AreNotEqual(files["items.json"], changed, "Replace must change the json");
            files["items.json"] = changed;
            var e = Fails(files);
            Assert.AreEqual("items[" + Bench3mIndex + "].workStrip", e.Field);
        }
    }
}
