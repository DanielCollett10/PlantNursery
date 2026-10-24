using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Genetics;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Tests.Plants
{
    public class SpeciesPlantFactoryTests
    {
        private static ContentDb RealContent()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string content = null;
            while (dir != null && content == null)
            {
                var c = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(c)) content = c;
                dir = dir.Parent;
            }
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(content, "*.json"))
            {
                var name = Path.GetFileName(path);
                if (name == ContentLoader.TimeDemoFile) continue;
                files[name] = File.ReadAllText(path);
            }
            return ContentLoader.Load(files);
        }

        private static SpeciesPlantFactory Factory(ContentDb db, int seed = 1)
        {
            return new SpeciesPlantFactory(new SimWorld(0.05, seed), db);
        }

        [Test]
        public void Create_SetsSpeciesFieldsAndDefaults()
        {
            var p = Factory(RealContent()).Create("hearthmoss", "hearthmoss_amber", "plug_tray", 3);
            Assert.AreEqual("hearthmoss", p.SpeciesId);
            Assert.AreEqual("hearthmoss_amber", p.VarietyId);
            Assert.AreEqual("plug_tray", p.ContainerId);
            Assert.AreEqual(3, p.CellIndex);
            Assert.AreEqual(0, p.StageIndex);
            Assert.AreEqual(0f, p.StageProgress);
            Assert.AreEqual(0, p.QualityStepsLost);
            Assert.AreEqual(PlantLifecycle.Growing, p.Lifecycle.Current);
            Assert.IsTrue(p.IsSpeciesPlant);
        }

        [Test]
        public void Lifecycle_FollowsTable()
        {
            var p = Factory(RealContent()).Create("hearthmoss", "hearthmoss_amber", "plug_tray", 0);
            Assert.AreEqual(TransitionResult.Applied, p.Lifecycle.TryTransition(PlantLifecycle.RootBound));
            Assert.AreEqual(TransitionResult.Illegal, p.Lifecycle.TryTransition(PlantLifecycle.Dead));
            Assert.AreEqual(TransitionResult.Applied, p.Lifecycle.TryTransition(PlantLifecycle.Wilted));
            Assert.AreEqual(TransitionResult.Applied, p.Lifecycle.TryTransition(PlantLifecycle.Dead));
            Assert.AreEqual(TransitionResult.Illegal, p.Lifecycle.TryTransition(PlantLifecycle.Growing));
        }

        [Test]
        public void Genome_CopiesVarietyAllelesExactly()
        {
            var db = RealContent();
            var p = Factory(db).Create("hearthmoss", "hearthmoss_amber", "plug_tray", 0);
            var glow = p.Genome.GetAlleles("glowColour");
            Assert.AreEqual("amber", glow.A);
            Assert.AreEqual("teal", glow.B);
            var regrow = p.Genome.GetAlleles("regrowthSpeed");
            Assert.AreEqual(0.5f, regrow.ValueA);
            Assert.AreEqual(0.5f, regrow.ValueB);
        }

        [Test]
        public void Phenotype_HearthmossAmberShowsAmber()
        {
            var p = Factory(RealContent()).Create("hearthmoss", "hearthmoss_amber", "plug_tray", 0);
            Assert.AreEqual("amber", p.Genome.MendelianPhenotype("glowColour"));
        }

        [Test]
        public void Phenotype_ChimebellWhiteShowsWhitePlain()
        {
            var p = Factory(RealContent()).Create("chimebell", "chimebell_white", "plug_tray", 0);
            Assert.AreEqual("white", p.Genome.MendelianPhenotype("bloomColour"));
            Assert.AreEqual("plain", p.Genome.MendelianPhenotype("bellPattern"));
        }

        [Test]
        public void SameContentAndSeed_GiveIdenticalGenomes()
        {
            var a = Factory(RealContent(), 7).Create("chimebell", "chimebell_white", "plug_tray", 0);
            var b = Factory(RealContent(), 7).Create("chimebell", "chimebell_white", "plug_tray", 0);
            foreach (var gene in new[] { "bloomColour", "bellPattern" })
            {
                Assert.AreEqual(a.Genome.GetAlleles(gene).A, b.Genome.GetAlleles(gene).A);
                Assert.AreEqual(a.Genome.GetAlleles(gene).B, b.Genome.GetAlleles(gene).B);
            }
        }

        [Test]
        public void UnknownSpeciesOrVariety_Throws()
        {
            var f = Factory(RealContent());
            Assert.Throws<GenomeException>(() => f.Create("nope", "hearthmoss_amber", "plug_tray", 0));
            Assert.Throws<GenomeException>(() => f.Create("hearthmoss", "nope", "plug_tray", 0));
            Assert.Throws<GenomeException>(() => f.Create("hearthmoss", "chimebell_white", "plug_tray", 0));
        }

        [Test]
        public void OldPlants_HaveNoSpeciesState()
        {
            var p = new Plant(1, "old");
            Assert.IsFalse(p.IsSpeciesPlant);
            Assert.IsNull(p.Lifecycle);
        }
    }
}
