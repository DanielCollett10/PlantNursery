using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Genetics
{
    /// <summary>
    /// A plant's diploid genome: two alleles for every gene defined in content (docs/design/genetics.md).
    /// The genome keeps its own copies of the allele data, so later edits to content or to the
    /// source variety do not change a plant that already exists.
    /// </summary>
    public sealed class Genome
    {
        private readonly IReadOnlyDictionary<string, GeneDef> _genes;
        private readonly Dictionary<string, AllelePair> _alleles = new Dictionary<string, AllelePair>();

        /// <summary>
        /// Creates a genome from allele pairs keyed by gene id.
        /// Every gene in <paramref name="genes"/> must be present, and no unknown gene ids are allowed.
        /// </summary>
        /// <exception cref="GenomeException">On an unknown gene, a missing gene, or an allele/value not valid for its gene.</exception>
        public Genome(IReadOnlyDictionary<string, GeneDef> genes, IReadOnlyDictionary<string, AllelePair> alleles)
        {
            if (genes == null) throw new ArgumentNullException(nameof(genes));
            if (alleles == null) throw new ArgumentNullException(nameof(alleles));
            _genes = genes;

            foreach (var entry in alleles)
            {
                if (!genes.TryGetValue(entry.Key, out var gene))
                    throw new GenomeException("unknown gene id '" + entry.Key + "'");
                if (entry.Value == null)
                    throw new GenomeException("gene '" + entry.Key + "' has no alleles");
                Validate(gene, entry.Value);
                _alleles[entry.Key] = Copy(entry.Value);
            }
            foreach (var id in genes.Keys)
                if (!_alleles.ContainsKey(id))
                    throw new GenomeException("missing alleles for gene '" + id + "'");
        }

        /// <summary>Creates the genome a plant sown from a packet of <paramref name="varietyId"/> gets: an exact copy of the variety's alleles.</summary>
        /// <exception cref="GenomeException">If the variety id is unknown.</exception>
        public static Genome FromVariety(ContentDb db, string varietyId)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (varietyId == null || !db.VarietyDefs.TryGetValue(varietyId, out var variety))
                throw new GenomeException("unknown variety id '" + varietyId + "'");
            return new Genome(db.GeneDefs, variety.Genes);
        }

        /// <summary>Returns a copy of the two alleles held for <paramref name="geneId"/>.</summary>
        /// <exception cref="GenomeException">If the gene is unknown.</exception>
        public AllelePair GetAlleles(string geneId)
        {
            Gene(geneId);
            return Copy(_alleles[geneId]);
        }

        /// <summary>The visible value of a Mendelian gene (see <see cref="Phenotype.Mendelian"/>).</summary>
        /// <exception cref="GenomeException">If the gene is unknown or not Mendelian.</exception>
        public string MendelianPhenotype(string geneId)
        {
            var gene = Gene(geneId);
            var pair = _alleles[geneId];
            return Phenotype.Mendelian(gene, pair.A, pair.B);
        }

        /// <summary>The visible value of a Blend gene (see <see cref="Phenotype.Blend"/>).</summary>
        /// <exception cref="GenomeException">If the gene is unknown or not Blend.</exception>
        public float BlendPhenotype(string geneId)
        {
            var gene = Gene(geneId);
            if (gene.Kind != GeneKind.Blend)
                throw new GenomeException("gene '" + geneId + "' is not Blend");
            var pair = _alleles[geneId];
            return Phenotype.Blend(pair.ValueA, pair.ValueB);
        }

        private GeneDef Gene(string geneId)
        {
            if (geneId == null || !_genes.TryGetValue(geneId, out var gene))
                throw new GenomeException("unknown gene id '" + geneId + "'");
            return gene;
        }

        private static void Validate(GeneDef gene, AllelePair pair)
        {
            if (gene.Kind == GeneKind.Mendelian)
            {
                Phenotype.Rank(gene, pair.A);
                Phenotype.Rank(gene, pair.B);
            }
            else
            {
                CheckRange(gene, pair.ValueA);
                CheckRange(gene, pair.ValueB);
            }
        }

        private static void CheckRange(GeneDef gene, float value)
        {
            if (value < gene.Min || value > gene.Max)
                throw new GenomeException("value " + value + " is outside " + gene.Min + ".." + gene.Max
                    + " for gene '" + gene.Id + "'");
        }

        private static AllelePair Copy(AllelePair p)
        {
            return new AllelePair { A = p.A, B = p.B, ValueA = p.ValueA, ValueB = p.ValueB };
        }
    }
}
