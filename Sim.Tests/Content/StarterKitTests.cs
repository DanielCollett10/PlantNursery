using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Economy values and the day-one starter kit in economy.json (T-125).</summary>
    public class StarterKitTests
    {
        private static Dictionary<string, string> Files() => ContentLoaderTests.RealFiles(false);

        private static ContentException Fails(Dictionary<string, string> files) =>
            Assert.Throws<ContentException>(() => ContentLoader.Load(files));

        /// <summary>Replaces text in economy.json and asserts that the replace really changed the file.</summary>
        private static Dictionary<string, string> EconomyWith(string from, string to)
        {
            var files = Files();
            var changed = files["economy.json"].Replace(from, to);
            Assert.AreNotEqual(files["economy.json"], changed, "Replace must change economy.json: " + from);
            files["economy.json"] = changed;
            return files;
        }

        [Test]
        public void RealEconomyValuesLoad()
        {
            var economy = ContentLoader.Load(Files()).Economy;
            Assert.AreEqual(200, economy.StartingMoney);
            Assert.AreEqual(0.5f, economy.SellBackFraction);
            Assert.AreEqual(0.6f, economy.QualityMultipliers[(int)QualityGrade.Poor], 0.0001f);
            Assert.AreEqual(1.0f, economy.QualityMultipliers[(int)QualityGrade.Good], 0.0001f);
            Assert.AreEqual(1.4f, economy.QualityMultipliers[(int)QualityGrade.Excellent], 0.0001f);
            Assert.AreEqual(0.5f, economy.PotValueCaps[(int)PotSize.Small], 0.0001f);
            Assert.AreEqual(0.8f, economy.PotValueCaps[(int)PotSize.Medium], 0.0001f);
            Assert.AreEqual(1.0f, economy.PotValueCaps[(int)PotSize.Large], 0.0001f);
            Assert.AreEqual(0.9f, economy.ExcellentCareThreshold, 0.0001f);
            Assert.AreEqual(0.6f, economy.PoorCareThreshold, 0.0001f);
        }

        [Test]
        public void OldEconomyFieldsStillLoad()
        {
            var economy = ContentLoader.Load(Files()).Economy;
            Assert.IsTrue(economy.SoftLockGuard.Enabled);
            Assert.AreEqual(2, economy.StarterInventory["bench_standard"]);
            Assert.AreEqual(6, economy.StarterInventory["plug_tray"]);
        }

        [Test]
        public void RealStarterKitHasTheFivePlacements()
        {
            var kit = ContentLoader.Load(Files()).StarterKit;
            Assert.AreEqual(5, kit.Placements.Count);
            var ids = new List<string>();
            foreach (var p in kit.Placements) ids.Add(p.ItemId);
            CollectionAssert.AreEquivalent(new[] { "bench_3m", "rain_barrel", "storage_crate", "order_desk", "bed" }, ids);
        }

        [Test]
        public void BenchHasSixSownPlugTraysInSlotsZeroToFive()
        {
            var bench = Bench();
            Assert.AreEqual(6, bench.Planting.Count);
            for (int i = 0; i < 6; i++)
            {
                var s = bench.Planting[i];
                Assert.AreEqual(i, s.Slot);
                Assert.AreEqual("plug_tray", s.ContainerItemId);
                Assert.AreEqual("hearthmoss_amber", s.VarietyId);
                Assert.AreEqual(100f, s.Moisture);
            }
        }

        [Test]
        public void WateringCanStartsOnBenchStripSlotZero()
        {
            var bench = Bench();
            Assert.AreEqual(1, bench.Strip.Count);
            Assert.AreEqual("watering_can", bench.Strip[0].ItemId);
            Assert.AreEqual(0, bench.Strip[0].Slot);
        }

        [Test]
        public void StorageCrateHoldsTwoCarryTrays()
        {
            var crate = Placement("storage_crate");
            Assert.AreEqual(1, crate.Holds.Count);
            Assert.AreEqual("carry_tray", crate.Holds[0].ItemId);
            Assert.AreEqual(2, crate.Holds[0].Count);
        }

        [Test]
        public void StarterPlacementsDoNotOverlapAndStayInPlot()
        {
            // Load already rejects overlaps; this checks the real layout is what the test expects.
            var db = ContentLoader.Load(Files());
            foreach (var p in db.StarterKit.Placements)
            {
                var size = Footprints.Rotate(db.ItemDefs[p.ItemId].FloorFootprint, (Rotation)p.RotationDegrees);
                Assert.LessOrEqual(p.X + size.W, db.Building.PlotWidth, p.ItemId);
                Assert.LessOrEqual(p.Z + size.D, db.Building.PlotDepth, p.ItemId);
            }
        }

        [Test]
        public void UnknownPlacementItemThrows()
        {
            var files = EconomyWith("\"item\": \"rain_barrel\"", "\"item\": \"teapot\"");
            var e = Fails(files);
            Assert.AreEqual("economy.json", e.File);
            Assert.AreEqual("starterKit.placements[1].item", e.Field);
        }

        [Test]
        public void UnknownStripItemIsRejectedAsNotAccepted()
        {
            var files = EconomyWith("{ \"slot\": 0, \"item\": \"watering_can\" }", "{ \"slot\": 0, \"item\": \"teapot\" }");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[0].strip[0].item", e.Field);
            StringAssert.Contains("not accepted", e.Message);
        }

        [Test]
        public void UnknownCarryItemThrows()
        {
            var files = EconomyWith("{ \"item\": \"carry_tray\", \"count\": 2 }", "{ \"item\": \"ghost\", \"count\": 2 }");
            Assert.AreEqual("starterKit.placements[2].holds[0].item", Fails(files).Field);
        }

        [Test]
        public void UnknownVarietyInPlantingThrows()
        {
            var files = EconomyWith("\"sow\": \"hearthmoss_amber\", \"moisture\": 100 },\n          { \"slot\": 1",
                "\"sow\": \"ghost\", \"moisture\": 100 },\n          { \"slot\": 1");
            Assert.AreEqual("starterKit.placements[0].planting[0].sow", Fails(files).Field);
        }

        [Test]
        public void SowingIntoAContainerTheSpeciesCannotUseThrows()
        {
            var files = EconomyWith("\"container\": \"plug_tray\", \"sow\": \"hearthmoss_amber\", \"moisture\": 100 },\n          { \"slot\": 1",
                "\"container\": \"pot_small\", \"sow\": \"hearthmoss_amber\", \"moisture\": 100 },\n          { \"slot\": 1");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[0].planting[0].sow", e.Field);
            StringAssert.Contains("cannot be sown", e.Message);
        }

        [Test]
        public void DuplicatePlantingSlotThrows()
        {
            var files = EconomyWith("{ \"slot\": 1, \"container\"", "{ \"slot\": 0, \"container\"");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[0].planting[1].slot", e.Field);
            StringAssert.Contains("already used", e.Message);
        }

        [Test]
        public void MoistureAboveHundredThrows()
        {
            var files = EconomyWith("\"slot\": 5, \"container\": \"plug_tray\", \"sow\": \"hearthmoss_amber\", \"moisture\": 100",
                "\"slot\": 5, \"container\": \"plug_tray\", \"sow\": \"hearthmoss_amber\", \"moisture\": 101");
            Assert.AreEqual("starterKit.placements[0].planting[5].moisture", Fails(files).Field);
        }

        [Test]
        public void OverlappingFloorPlacementsThrow()
        {
            // The bed (2 x 3) moved onto the bench's row (z 20) overlaps the 6 x 2 bench.
            var files = EconomyWith("\"item\": \"bed\", \"x\": 4, \"z\": 23", "\"item\": \"bed\", \"x\": 4, \"z\": 20");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[4].x", e.Field);
            StringAssert.Contains("overlaps", e.Message);
        }

        [Test]
        public void PlacementOutsidePlotThrows()
        {
            var files = EconomyWith("\"item\": \"bed\", \"x\": 4, \"z\": 23", "\"item\": \"bed\", \"x\": 39, \"z\": 23");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[4].x", e.Field);
            StringAssert.Contains("outside the plot", e.Message);
        }

        [Test]
        public void BadRotationThrows()
        {
            var files = EconomyWith("\"x\": 11, \"z\": 20, \"rotation\": 0", "\"x\": 11, \"z\": 20, \"rotation\": 45");
            Assert.AreEqual("starterKit.placements[1].rotation", Fails(files).Field);
        }

        [Test]
        public void StripSlotOutOfRangeThrows()
        {
            // bench_3m strip: 30 units along / 3 along for a watering can = 10 slots, one row.
            var files = EconomyWith("{ \"slot\": 0, \"item\": \"watering_can\" }", "{ \"slot\": 10, \"item\": \"watering_can\" }");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[0].strip[0].slot", e.Field);
            StringAssert.Contains("no such strip slot", e.Message);
        }

        [Test]
        public void OverlappingStripItemsThrow()
        {
            // Watering can (3 x 3) at slot 0 covers units 0..2; a small pot (2 x 2) at slot 1 starts at unit 2.
            var files = EconomyWith("{ \"slot\": 0, \"item\": \"watering_can\" }",
                "{ \"slot\": 0, \"item\": \"watering_can\" }, { \"slot\": 1, \"item\": \"pot_small\" }");
            var e = Fails(files);
            Assert.AreEqual("starterKit.placements[0].strip[1].slot", e.Field);
            StringAssert.Contains("overlaps", e.Message);
        }

        [Test]
        public void PlantingOnANonBenchThrows()
        {
            var files = EconomyWith("\"item\": \"rain_barrel\", \"x\": 11, \"z\": 20, \"rotation\": 0",
                "\"item\": \"rain_barrel\", \"x\": 11, \"z\": 20, \"rotation\": 0, \"planting\": [ { \"slot\": 0, \"container\": \"plug_tray\", \"sow\": \"hearthmoss_amber\", \"moisture\": 100 } ]");
            Assert.AreEqual("starterKit.placements[1].planting", Fails(files).Field);
        }

        [Test]
        public void NonFloorItemInKitThrows()
        {
            var files = EconomyWith("\"item\": \"bed\", \"x\": 4, \"z\": 23", "\"item\": \"watering_can\", \"x\": 4, \"z\": 23");
            Assert.AreEqual("starterKit.placements[4].item", Fails(files).Field);
        }

        [Test]
        public void UnknownQualityKeyThrows()
        {
            var files = EconomyWith("\"excellent\": 1.4", "\"great\": 1.4");
            Assert.AreEqual("qualityMultipliers.great", Fails(files).Field);
        }

        [Test]
        public void PotCapAboveOneThrows()
        {
            var files = EconomyWith("\"large\": 1.0 }", "\"large\": 1.5 }");
            Assert.AreEqual("potValueCaps", Fails(files).Field);
        }

        [Test]
        public void PoorThresholdAboveExcellentThrows()
        {
            var files = EconomyWith("\"excellent\": 0.9, \"poor\": 0.6", "\"excellent\": 0.5, \"poor\": 0.6");
            Assert.AreEqual("qualityCareThresholds", Fails(files).Field);
        }

        [Test]
        public void MissingStarterKitThrows()
        {
            var files = Files();
            var economy = JObject.Parse(files["economy.json"]);
            Assert.IsTrue(economy.Remove("starterKit"), "economy.json must contain starterKit");
            files["economy.json"] = economy.ToString();
            var e = Fails(files);
            Assert.AreEqual("economy.json", e.File);
            Assert.AreEqual("starterKit", e.Field);
        }

        private static StarterPlacement Placement(string itemId)
        {
            foreach (var p in ContentLoader.Load(Files()).StarterKit.Placements)
                if (p.ItemId == itemId) return p;
            Assert.Fail("no starter placement for " + itemId);
            return null;
        }

        private static StarterPlacement Bench() => Placement("bench_3m");
    }
}
