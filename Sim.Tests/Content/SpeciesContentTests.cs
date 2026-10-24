using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Tests
{
    public class SpeciesContentTests
    {
        private static Dictionary<string, string> RealFiles()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content")))
                dir = dir.Parent;
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content"), "*.json"))
            {
                var name = Path.GetFileName(path);
                if (name != ContentLoader.TimeDemoFile) files[name] = File.ReadAllText(path);
            }
            return files;
        }

        private static ContentException FailsAfter(string file, string from, string to)
        {
            var files = RealFiles();
            var changed = files[file].Replace(from, to);
            Assert.AreNotEqual(files[file], changed, "Replace must change the json");
            files[file] = changed;
            var e = Assert.Throws<ContentException>(() => ContentLoader.Load(files));
            Assert.AreEqual(file, e.File);
            return e;
        }

        [Test]
        public void RealSpeciesLoad()
        {
            var db = ContentLoader.Load(RealFiles());
            Assert.AreEqual(2, db.SpeciesDefs.Count);
            var moss = db.SpeciesDefs["hearthmoss"];
            Assert.AreEqual(1, moss.RegrowDays);
            Assert.AreEqual(0, moss.Modules.Count);
            Assert.AreEqual(TrayMode.WholeTray, moss.Sowing.TrayMode);
            CollectionAssert.AreEqual(new[] { "plug_tray" }, moss.Sowing.SowInto);
            Assert.AreEqual(0.5f, moss.SeasonMultipliers[(int)Season.Summer]);
            var bell = db.SpeciesDefs["chimebell"];
            Assert.AreEqual(TrayMode.PerCell, bell.Sowing.TrayMode);
            CollectionAssert.AreEqual(new[] { "plug_tray", "pot_small" }, bell.Sowing.SowInto);
            CollectionAssert.AreEqual(new[] { "Seedling", "Young", "Blooming", "Specimen" },
                System.Linq.Enumerable.Select(bell.Stages, s => s.Name));
            Assert.AreEqual(4, db.SpeciesDefs["hearthmoss"].Varieties.Count + db.SpeciesDefs["chimebell"].Varieties.Count);
        }

        [Test]
        public void VarietiesMatchGeneticsTable()
        {
            var db = ContentLoader.Load(RealFiles());
            var amber = db.SpeciesDefs["hearthmoss"].Varieties["hearthmoss_amber"];
            Assert.AreEqual("amber", amber.Genes["glowColour"].A);
            Assert.AreEqual("teal", amber.Genes["glowColour"].B);
            Assert.AreEqual(0.5f, amber.Genes["regrowthSpeed"].ValueA);
            var violet = db.SpeciesDefs["hearthmoss"].Varieties["hearthmoss_violet"];
            Assert.AreEqual(0.3f, violet.Genes["regrowthSpeed"].ValueA);
            Assert.AreEqual(0.4f, violet.Genes["regrowthSpeed"].ValueB);
            var white = db.SpeciesDefs["chimebell"].Varieties["chimebell_white"];
            Assert.AreEqual("rose", white.Genes["bloomColour"].B);
            Assert.AreEqual("spotted", white.Genes["bellPattern"].B);
            var blue = db.SpeciesDefs["chimebell"].Varieties["chimebell_blue"];
            Assert.AreEqual("striped", blue.Genes["bellPattern"].A);
            Assert.AreEqual("blue", blue.Genes["bloomColour"].B);
        }

        [Test]
        public void OldVarietiesStillLoadWithOldGenes()
        {
            var db = ContentLoader.Load(RealFiles());
            Assert.AreEqual(3, db.VarietyDefs.Count);
            Assert.AreEqual(3, db.GeneDefs.Count);
            Assert.AreEqual(7, db.AllGeneDefs.Count);
        }

        [Test]
        public void MissingSpeciesFileThrows()
        {
            var files = RealFiles();
            files.Remove("species.json");
            Assert.AreEqual("species.json", Assert.Throws<ContentException>(() => ContentLoader.Load(files)).File);
        }

        [Test]
        public void UnknownGeneRejected()
        {
            var e = FailsAfter("species.json", "\"geneIds\": [\"glowColour\"", "\"geneIds\": [\"nope\"");
            StringAssert.StartsWith("species[0].geneIds[0]", e.Field);
        }

        [Test]
        public void UnknownAlleleRejected()
        {
            var e = FailsAfter("species.json", "[\"violet\", \"violet\"]", "[\"violet\", \"pink\"]");
            StringAssert.Contains("glowColour", e.Field);
        }

        [Test]
        public void UnknownContainerRejected()
        {
            var e = FailsAfter("species.json", "[\"pot_medium\"]", "[\"pot_huge\"]");
            StringAssert.Contains("allowedContainers", e.Field);
        }

        [Test]
        public void NegativeDaysRejected()
        {
            var e = FailsAfter("species.json", "\"days\": 1, \"allowedContainers\": [\"plug_tray\"]", "\"days\": -1, \"allowedContainers\": [\"plug_tray\"]");
            StringAssert.EndsWith("days", e.Field);
        }

        [Test]
        public void MissingSeasonRejected()
        {
            var e = FailsAfter("species.json", "\"Summer\": 0.5, ", "");
            StringAssert.EndsWith("Summer", e.Field);
        }

        [Test]
        public void FirstStageMustAllowEverySowContainer()
        {
            var e = FailsAfter("species.json", "\"sowInto\": [\"plug_tray\", \"pot_small\"]", "\"sowInto\": [\"plug_tray\", \"pot_medium\"]");
            StringAssert.Contains("sowInto", e.Field);
        }

        [Test]
        public void BadTrayModeRejected()
        {
            var e = FailsAfter("species.json", "\"wholeTray\"", "\"halfTray\"");
            StringAssert.EndsWith("trayMode", e.Field);
        }

        [Test]
        public void SpecimenMustBeLast()
        {
            var files = RealFiles();
            var json = files["species.json"];
            var changed = json.Replace("{ \"name\": \"Seedling\"", "{ \"name\": \"Specimen\"")
                              .Replace("{ \"name\": \"Specimen\", \"days\": 2", "{ \"name\": \"Seedling\", \"days\": 2");
            Assert.AreNotEqual(json, changed);
            files["species.json"] = changed;
            var e = Assert.Throws<ContentException>(() => ContentLoader.Load(files));
            StringAssert.Contains("Specimen", e.Message);
        }

        [Test]
        public void VarietyMissingGeneRejected()
        {
            var e = FailsAfter("species.json", ", \"bellPattern\": [\"striped\", \"striped\"]", "");
            StringAssert.EndsWith("bellPattern", e.Field);
        }

        [Test]
        public void GeneWithUnknownSpeciesRejected()
        {
            var files = RealFiles();
            var changed = files["genes.json"].Replace("\"speciesId\": \"chimebell\", \"kind\": \"Mendelian\", \"alleles\": [\"white\"",
                "\"speciesId\": \"ghost\", \"kind\": \"Mendelian\", \"alleles\": [\"white\"");
            Assert.AreNotEqual(files["genes.json"], changed);
            files["genes.json"] = changed;
            // Reported against species.json: chimebell lists a gene that is not its own.
            var e = Assert.Throws<ContentException>(() => ContentLoader.Load(files));
            Assert.AreEqual("species.json", e.File);
            StringAssert.Contains("bloomColour", e.Message);
        }
    }
}
