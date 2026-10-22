using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Genetics;

namespace PlantNursery.Sim.Tests.Genetics
{
    public class GenomeTests
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

        private static ContentDb RealContent()
        {
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(ContentDir(), "*.json"))
            {
                var name = Path.GetFileName(path);
                if (name == ContentLoader.TimeDemoFile) continue;
                files[name] = File.ReadAllText(path);
            }
            return ContentLoader.Load(files);
        }

        private static AllelePair Mendel(string a, string b)
        {
            return new AllelePair { A = a, B = b };
        }

        private static AllelePair Blend(float a, float b)
        {
            return new AllelePair { ValueA = a, ValueB = b };
        }

        private static Dictionary<string, GeneDef> TestGenes()
        {
            return new Dictionary<string, GeneDef>
            {
                ["leafColour"] = new GeneDef
                {
                    Id = "leafColour", Kind = GeneKind.Mendelian,
                    Alleles = new List<string> { "green", "red", "yellow" },
                },
                ["spotSize"] = new GeneDef { Id = "spotSize", Kind = GeneKind.Blend, Min = 0f, Max = 1f },
            };
        }

        private static Dictionary<string, AllelePair> TestAlleles(AllelePair leaf, AllelePair spot)
        {
            return new Dictionary<string, AllelePair> { ["leafColour"] = leaf, ["spotSize"] = spot };
        }

        // ---- Genome holds two alleles per gene ----

        [Test]
        public void GenomeHoldsTwoAllelesForEveryContentGene()
        {
            var db = RealContent();
            var g = Genome.FromVariety(db, "yellow_striped");
            Assert.AreEqual("yellow", g.GetAlleles("leafColour").A);
            Assert.AreEqual("yellow", g.GetAlleles("leafColour").B);
            Assert.AreEqual("striped", g.GetAlleles("variegation").A);
            Assert.AreEqual("striped", g.GetAlleles("variegation").B);
            Assert.AreEqual(0.5f, g.GetAlleles("spotSize").ValueA);
            Assert.AreEqual(0.5f, g.GetAlleles("spotSize").ValueB);
        }

        [Test]
        public void GenomeRequiresEveryGeneInContent()
        {
            var alleles = TestAlleles(Mendel("green", "red"), Blend(0.1f, 0.2f));
            alleles.Remove("spotSize");
            Assert.Throws<GenomeException>(() => new Genome(TestGenes(), alleles));
        }

        [Test]
        public void GenomeRejectsAlleleNotDefinedForGene()
        {
            var alleles = TestAlleles(Mendel("green", "blue"), Blend(0.1f, 0.2f));
            Assert.Throws<GenomeException>(() => new Genome(TestGenes(), alleles));
        }

        [Test]
        public void GenomeRejectsBlendValueOutsideRange()
        {
            var alleles = TestAlleles(Mendel("green", "red"), Blend(0.1f, 1.5f));
            Assert.Throws<GenomeException>(() => new Genome(TestGenes(), alleles));
        }

        [Test]
        public void GenomeCopiesAllelesInsteadOfSharingThem()
        {
            var db = RealContent();
            var variety = db.VarietyDefs["green_plain"];
            var g = Genome.FromVariety(db, "green_plain");

            variety.Genes["leafColour"].A = "red";
            Assert.AreEqual("green", g.GetAlleles("leafColour").A);

            var copy = g.GetAlleles("leafColour");
            copy.A = "yellow";
            Assert.AreEqual("green", g.GetAlleles("leafColour").A);
        }

        // ---- Variety genome creation ----

        [Test]
        public void VarietyGenomeCopiesVarietyAllelesExactly()
        {
            var db = RealContent();
            foreach (var variety in db.VarietyDefs.Values)
            {
                var g = Genome.FromVariety(db, variety.Id);
                foreach (var geneId in db.GeneDefs.Keys)
                {
                    var expected = variety.Genes[geneId];
                    var actual = g.GetAlleles(geneId);
                    Assert.AreEqual(expected.A, actual.A, variety.Id + " " + geneId + ".A");
                    Assert.AreEqual(expected.B, actual.B, variety.Id + " " + geneId + ".B");
                    Assert.AreEqual(expected.ValueA, actual.ValueA, variety.Id + " " + geneId + ".ValueA");
                    Assert.AreEqual(expected.ValueB, actual.ValueB, variety.Id + " " + geneId + ".ValueB");
                }
            }
        }

        [Test]
        public void UnknownVarietyIsRejected()
        {
            Assert.Throws<GenomeException>(() => Genome.FromVariety(RealContent(), "no_such_variety"));
        }

        // ---- Phenotype ----

        [Test]
        public void MendelianPhenotypeFollowsDominanceOrderGreenOverRedOverYellow()
        {
            // Dominance order green > red > yellow.
            Assert.AreEqual("green", Phenotype.Mendelian(TestGenes()["leafColour"], "green", "yellow"));
            Assert.AreEqual("green", Phenotype.Mendelian(TestGenes()["leafColour"], "yellow", "green"));
            Assert.AreEqual("green", Phenotype.Mendelian(TestGenes()["leafColour"], "green", "red"));
            Assert.AreEqual("red", Phenotype.Mendelian(TestGenes()["leafColour"], "red", "yellow"));
            Assert.AreEqual("red", Phenotype.Mendelian(TestGenes()["leafColour"], "yellow", "red"));
            Assert.AreEqual("yellow", Phenotype.Mendelian(TestGenes()["leafColour"], "yellow", "yellow"));
            Assert.AreEqual("green", Phenotype.Mendelian(TestGenes()["leafColour"], "green", "green"));
        }

        [Test]
        public void GenomePhenotypeUsesMostDominantAllele()
        {
            var g = new Genome(TestGenes(), TestAlleles(Mendel("yellow", "red"), Blend(0.1f, 0.2f)));
            Assert.AreEqual("red", g.MendelianPhenotype("leafColour"));
        }

        [Test]
        public void BlendAveragesTheTwoValues()
        {
            Assert.AreEqual(0.5f, Phenotype.Blend(0.2f, 0.8f), 1e-6f);
            Assert.AreEqual(0.3f, Phenotype.Blend(0.3f, 0.3f), 1e-6f);
            Assert.AreEqual(0f, Phenotype.Blend(0f, 0f), 1e-6f);
            Assert.AreEqual(1f, Phenotype.Blend(1f, 1f), 1e-6f);
        }

        [Test]
        public void GenomeBlendPhenotypeIsMeanOfAlleles()
        {
            var g = new Genome(TestGenes(), TestAlleles(Mendel("green", "green"), Blend(0.1f, 0.6f)));
            Assert.AreEqual(0.35f, g.BlendPhenotype("spotSize"), 1e-6f);
        }

        [Test]
        public void MendelianPhenotypeOfBlendGeneIsRejected()
        {
            var g = new Genome(TestGenes(), TestAlleles(Mendel("green", "green"), Blend(0.1f, 0.2f)));
            Assert.Throws<GenomeException>(() => g.MendelianPhenotype("spotSize"));
        }

        [Test]
        public void BlendPhenotypeOfMendelianGeneIsRejected()
        {
            var g = new Genome(TestGenes(), TestAlleles(Mendel("green", "green"), Blend(0.1f, 0.2f)));
            Assert.Throws<GenomeException>(() => g.BlendPhenotype("leafColour"));
        }

        // ---- Unknown gene rejection ----

        [Test]
        public void UnknownGeneInGenomeIsRejected()
        {
            var alleles = TestAlleles(Mendel("green", "red"), Blend(0.1f, 0.2f));
            alleles["mystery"] = Mendel("green", "green");
            Assert.Throws<GenomeException>(() => new Genome(TestGenes(), alleles));
        }

        [Test]
        public void UnknownGeneInPhenotypeIsRejected()
        {
            var g = new Genome(TestGenes(), TestAlleles(Mendel("green", "red"), Blend(0.1f, 0.2f)));
            Assert.Throws<GenomeException>(() => g.MendelianPhenotype("mystery"));
            Assert.Throws<GenomeException>(() => g.BlendPhenotype("mystery"));
            Assert.Throws<GenomeException>(() => g.GetAlleles("mystery"));
            Assert.Throws<GenomeException>(() => g.GetAlleles(null));
        }
    }
}
