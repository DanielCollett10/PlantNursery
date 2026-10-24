using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class ContentLoaderTests
    {
        private static string ContentDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Assets/StreamingAssets/Content not found");
        }

        private static Dictionary<string, string> RealFiles(bool demo)
        {
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(ContentDir(), "*.json"))
            {
                var name = Path.GetFileName(path);
                if (name == ContentLoader.TimeDemoFile && !demo) continue;
                files[name] = File.ReadAllText(path);
            }
            return files;
        }

        private static ContentException Fails(Dictionary<string, string> files)
        {
            return Assert.Throws<ContentException>(() => ContentLoader.Load(files));
        }

        [Test]
        public void RealContentLoads()
        {
            var db = ContentLoader.Load(RealFiles(false));
            Assert.AreEqual(360, db.Time.DayStartMinute);
            Assert.AreEqual(600f, db.Time.RealSecondsDay);
            Assert.AreEqual(540, db.Time.MorningEndMinute);
            Assert.AreEqual(1200, db.Time.EveningStartMinute);
            Assert.AreEqual(1320, db.Time.NightStartMinute);
            Assert.AreEqual(0.25f, db.Time.NightRateMultiplier);
            Assert.AreEqual(10, db.Time.SeasonLengthDays);
            Assert.AreEqual(3, db.VarietyDefs.Count);
            Assert.AreEqual(3, db.GeneDefs.Count); // old genes only; species genes are in AllGeneDefs
            Assert.AreEqual(4, db.ContainerDefs.Count);
            Assert.AreEqual(40, db.Building.PlotWidth);
            Assert.AreEqual(30, db.Building.PlotDepth);
            Assert.AreEqual(2, db.Building.NudgeRadius);
            Assert.AreEqual(200, db.Economy.StartingMoney);
            Assert.IsTrue(db.Economy.SoftLockGuard.Enabled);
            Assert.IsNull(db.Economy.SoftLockGuard.GiftVarietyId);
            Assert.AreEqual(6, db.Economy.StarterInventory["plug_tray"]);
            Assert.AreEqual(2, db.Economy.StarterInventory["bench_standard"]);
        }

        [Test]
        public void RealItemsMatchBuildingTable()
        {
            var db = ContentLoader.Load(RealFiles(false));
            var bench = db.ItemDefs["bench_standard"];
            Assert.AreEqual(ItemLayer.Floor, bench.Layer);
            Assert.AreEqual(2, bench.FloorFootprint.W);
            Assert.AreEqual(4, bench.FloorFootprint.D);
            Assert.IsTrue(bench.ProvidesSurface);
            Assert.AreEqual(40, bench.Price);
            Assert.AreEqual(1, db.ItemDefs["pot_small"].SurfaceFootprint.W);
            Assert.AreEqual(2, db.ItemDefs["pot_medium"].SurfaceFootprint.D);
            Assert.AreEqual(2, db.ItemDefs["plug_tray"].SurfaceFootprint.W);
            Assert.AreEqual(3, db.ItemDefs["plug_tray"].SurfaceFootprint.D);
            Assert.AreEqual(3, db.ItemDefs["watering_can"].SurfaceFootprint.D);
            Assert.AreEqual(2, db.ItemDefs["seed_packet"].SurfaceFootprint.D);
            Assert.AreEqual(10, db.ItemDefs["seed_packet"].Price);
        }

        [Test]
        public void RealContainersMatchPlantsTable()
        {
            var db = ContentLoader.Load(RealFiles(false));
            var tray = db.ContainerDefs["plug_tray"];
            Assert.AreEqual(6, tray.CellCount);
            Assert.AreEqual(PlantStage.Rooted, tray.MaxStage);
            Assert.AreEqual(PlantStage.Seed, tray.AcceptsFromStage);
            Assert.AreEqual(4.0f, tray.DryRatePerHour);
            Assert.AreEqual(PlantStage.Mature, db.ContainerDefs["pot_medium"].MaxStage);
        }

        [Test]
        public void M2ItemCatalogueHasEveryIdAndPrice()
        {
            var db = ContentLoader.Load(RealFiles(false));
            var prices = new Dictionary<string, int>
            {
                { "spores_hearthmoss_amber", 10 }, { "spores_hearthmoss_violet", 10 },
                { "seeds_chimebell_white", 12 }, { "seeds_chimebell_blue", 12 },
                { "pot_small", 2 }, { "pot_medium", 4 }, { "pot_large", 8 },
                { "plug_tray", 5 }, { "carry_tray", 6 }, { "bench_3m", 40 }, { "bench_4m", 50 },
                { "watering_can", 0 }, { "rain_barrel", 0 }, { "storage_crate", 0 },
                { "order_desk", 0 }, { "bed", 0 }, { "bundle_hearthmoss", 0 },
            };
            foreach (var kv in prices)
            {
                Assert.IsTrue(db.ItemDefs.ContainsKey(kv.Key), kv.Key);
                Assert.AreEqual(kv.Value, db.ItemDefs[kv.Key].Price, kv.Key);
            }
            var amber = db.ItemDefs["spores_hearthmoss_amber"];
            Assert.AreEqual("hearthmoss", amber.SpeciesId);
            Assert.AreEqual("hearthmoss_amber", amber.VarietyId);
            Assert.AreEqual(1, amber.PacketSeeds);
            Assert.AreEqual(6, db.ItemDefs["seeds_chimebell_blue"].PacketSeeds);
            Assert.AreEqual("chimebell_blue", db.ItemDefs["seeds_chimebell_blue"].VarietyId);
            Assert.AreEqual(10, db.ItemDefs["watering_can"].WaterCapacity);
            Assert.AreEqual(6, db.ItemDefs["carry_tray"].Capacity);
        }

        [Test]
        public void SeedItemWithUnknownVarietyFails()
        {
            var files = RealFiles(false);
            var changed = files["items.json"].Replace("\"varietyId\": \"chimebell_blue\"", "\"varietyId\": \"ghost\"");
            Assert.AreNotEqual(files["items.json"], changed);
            files["items.json"] = changed;
            var e = Fails(files);
            StringAssert.Contains("varietyId", e.Message);
        }

        [Test]
        public void SeedItemWithUnknownSpeciesFails()
        {
            var files = RealFiles(false);
            var changed = files["items.json"].Replace("\"speciesId\": \"chimebell\"", "\"speciesId\": \"ghost\"");
            Assert.AreNotEqual(files["items.json"], changed);
            files["items.json"] = changed;
            StringAssert.Contains("speciesId", Fails(files).Message);
        }

        [Test]
        public void SeedItemMissingPacketSeedsFails()
        {
            var files = RealFiles(false);
            var changed = files["items.json"].Replace(", \"packetSeeds\": 1 }", " }");
            Assert.AreNotEqual(files["items.json"], changed);
            files["items.json"] = changed;
            StringAssert.Contains("packetSeeds", Fails(files).Message);
        }

        [Test]
        public void ContainersCarryFootprintDrainAndCost()
        {
            var db = ContentLoader.Load(RealFiles(false));
            var drain = new Dictionary<string, float> { { "plug_tray", 8 }, { "pot_small", 6 }, { "pot_medium", 4 }, { "pot_large", 3 } };
            var cost = new Dictionary<string, int> { { "plug_tray", 1 }, { "pot_small", 1 }, { "pot_medium", 1 }, { "pot_large", 2 } };
            foreach (var id in drain.Keys)
            {
                var c = db.ContainerDefs[id];
                Assert.AreEqual(drain[id], c.DrainPerHour, id);
                Assert.AreEqual(cost[id], c.WateringCost, id);
                Assert.Greater(c.AlongUnits, 0, id);
                Assert.Greater(c.AcrossUnits, 0, id);
            }
            Assert.AreEqual(6, db.ContainerDefs["plug_tray"].CellCount);
            Assert.AreEqual(6, db.ContainerDefs["plug_tray"].AlongUnits);
            Assert.AreEqual(4, db.ContainerDefs["plug_tray"].AcrossUnits);
        }

        [Test]
        public void DemoFileOverridesTimeValues()
        {
            var db = ContentLoader.Load(RealFiles(true));
            Assert.AreEqual(600f, db.Time.RealSecondsDay);
            Assert.AreEqual(3, db.Time.SeasonLengthDays);
            Assert.AreEqual(360, db.Time.DayStartMinute);
        }

        [TestCase("1.5")]
        [TestCase("-0.1")]
        public void NightRateMultiplierOutOfRangeFails(string bad)
        {
            var files = RealFiles(false);
            var changed = files["time.json"].Replace("\"nightRateMultiplier\": 0.25", "\"nightRateMultiplier\": " + bad);
            Assert.AreNotEqual(files["time.json"], changed, "Replace must change the json");
            files["time.json"] = changed;
            var e = Fails(files);
            Assert.AreEqual("time.json", e.File);
            Assert.AreEqual("nightRateMultiplier", e.Field);
        }

        [Test]
        public void DemoFileWithNegativeValueNamesDemoFile()
        {
            var files = RealFiles(false);
            files["time.demo.json"] = "{ \"seasonLengthDays\": -1 }";
            var e = Fails(files);
            Assert.AreEqual("time.demo.json", e.File);
            Assert.AreEqual("seasonLengthDays", e.Field);
        }

        [Test]
        public void MissingFileThrows()
        {
            var files = RealFiles(false);
            files.Remove("genes.json");
            Assert.AreEqual("genes.json", Fails(files).File);
        }

        [Test]
        public void InvalidJsonThrows()
        {
            var files = RealFiles(false);
            files["economy.json"] = "{ not json";
            Assert.AreEqual("economy.json", Fails(files).File);
        }

        [Test]
        public void MissingFieldNamesFileAndField()
        {
            var files = RealFiles(false);
            files["time.json"] = "{ \"dayStartMinute\": 360 }";
            var e = Fails(files);
            Assert.AreEqual("time.json", e.File);
            Assert.AreEqual("nightStartMinute", e.Field);
            StringAssert.Contains("time.json", e.Message);
            StringAssert.Contains("nightStartMinute", e.Message);
        }

        [Test]
        public void NegativePriceNamesItemField()
        {
            var files = RealFiles(false);
            files["items.json"] = files["items.json"].Replace("\"price\": 40", "\"price\": -40");
            var e = Fails(files);
            Assert.AreEqual("items.json", e.File);
            Assert.AreEqual("items[0].price", e.Field);
        }

        [Test]
        public void NegativeStartingMoneyThrows()
        {
            var files = RealFiles(false);
            files["economy.json"] = files["economy.json"].Replace("\"startingMoney\": 200", "\"startingMoney\": -5");
            Assert.AreEqual("startingMoney", Fails(files).Field);
        }

        [Test]
        public void UnknownItemInContainerThrows()
        {
            var files = RealFiles(false);
            files["containers.json"] = files["containers.json"].Replace("\"itemId\": \"pot_small\"", "\"itemId\": \"nope\"");
            var e = Fails(files);
            Assert.AreEqual("containers.json", e.File);
            Assert.AreEqual("containers[1].itemId", e.Field);
        }

        [Test]
        public void UnknownItemInStarterInventoryThrows()
        {
            var files = RealFiles(false);
            files["economy.json"] = files["economy.json"].Replace("\"tap\": 1", "\"teapot\": 1");
            var e = Fails(files);
            Assert.AreEqual("economy.json", e.File);
            Assert.AreEqual("starterInventory.teapot", e.Field);
        }

        [Test]
        public void UnknownGiftVarietyThrows()
        {
            var files = RealFiles(false);
            files["economy.json"] = files["economy.json"].Replace("\"giftVarietyId\": null", "\"giftVarietyId\": \"ghost\"");
            Assert.AreEqual("softLockGuard.giftVarietyId", Fails(files).Field);
        }

        [Test]
        public void UnknownAlleleInVarietyThrows()
        {
            var files = RealFiles(false);
            files["varieties.json"] = files["varieties.json"].Replace("[\"red\", \"red\"]", "[\"red\", \"blue\"]");
            var e = Fails(files);
            Assert.AreEqual("varieties.json", e.File);
            StringAssert.Contains("leafColour[1]", e.Field);
        }

        [Test]
        public void UnknownGeneInVarietyThrows()
        {
            var files = RealFiles(false);
            files["varieties.json"] = files["varieties.json"].Replace("\"spotSize\": [0.2, 0.2]", "\"mystery\": [0.2, 0.2]");
            Assert.AreEqual("varieties[0].genes.mystery", Fails(files).Field);
        }

        [Test]
        public void MissingItemFootprintThrows()
        {
            var files = RealFiles(false);
            files["items.json"] = files["items.json"].Replace("\"floorFootprint\": { \"w\": 2, \"d\": 4 }, ", "");
            Assert.AreEqual("items[0].floorFootprint", Fails(files).Field);
        }

        [Test]
        public void UnknownLayerThrows()
        {
            var files = RealFiles(false);
            files["items.json"] = files["items.json"].Replace("\"layer\": \"Floor\"", "\"layer\": \"Ceiling\"");
            Assert.AreEqual("items[0].layer", Fails(files).Field);
        }

        [Test]
        public void NegativePlotSizeThrows()
        {
            var files = RealFiles(false);
            files["building.json"] = "{ \"plot\": { \"w\": -4, \"d\": 30 }, \"nudgeRadius\": 2 }";
            var e = Fails(files);
            Assert.AreEqual("building.json", e.File);
            Assert.AreEqual("plot.w", e.Field);
        }
    }
}
