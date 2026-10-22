using System;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Genetics
{
    /// <summary>
    /// Pure phenotype rules from docs/design/genetics.md.
    /// Mendelian: the most dominant allele present wins (earlier in <see cref="GeneDef.Alleles"/> is more dominant).
    /// Blend: the mean of the two values.
    /// </summary>
    public static class Phenotype
    {
        /// <summary>Returns the visible allele for a Mendelian gene with alleles <paramref name="a"/> and <paramref name="b"/>.</summary>
        /// <exception cref="GenomeException">If the gene is not Mendelian or an allele is not defined for it.</exception>
        public static string Mendelian(GeneDef gene, string a, string b)
        {
            if (gene == null) throw new ArgumentNullException(nameof(gene));
            if (gene.Kind != GeneKind.Mendelian)
                throw new GenomeException("gene '" + gene.Id + "' is not Mendelian");
            int ia = Rank(gene, a);
            int ib = Rank(gene, b);
            return ia <= ib ? a : b;
        }

        /// <summary>
        /// Dominance rank of an allele: 0 is the most dominant. Throws if the allele is not defined for the gene.
        /// </summary>
        /// <exception cref="GenomeException">If the allele is not defined for <paramref name="gene"/>.</exception>
        public static int Rank(GeneDef gene, string allele)
        {
            var alleles = gene.Alleles;
            for (int i = 0; i < alleles.Count; i++)
                if (alleles[i] == allele) return i;
            throw new GenomeException("unknown allele '" + allele + "' for gene '" + gene.Id + "'");
        }

        /// <summary>Returns the blended value of two Blend alleles: their mean.</summary>
        public static float Blend(float a, float b)
        {
            return (a + b) / 2f;
        }
    }
}
