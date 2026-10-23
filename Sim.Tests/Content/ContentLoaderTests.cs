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
            Assert.AreEqual(3600f, db.Time.RealSecondsDaylight);
            Assert.AreEqual(10, db.Time.SeasonLengthDays);
            Assert.AreEqual(3, db.VarietyDefs.Count);
            Assert.AreEqual(3, db.GeneDefs.Count);
            Assert.AreEqual(3, db.ContainerDefs.Count);
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
        public void DemoFileOverridesTimeValues()
        {
            var db = ContentLoader.Load(RealFiles(true));
            Assert.AreEqual(600f, db.Time.RealSecondsDaylight);
            Assert.AreEqual(120f, db.Time.RealSecondsNight);
            Assert.AreEqual(3, db.Time.SeasonLengthDays);
            Assert.AreEqual(360, db.Time.DayStartMinute);
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
