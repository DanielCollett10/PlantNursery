using System;

namespace PlantNursery.Sim.Genetics
{
    /// <summary>Thrown when a genome or phenotype request does not match the gene content.</summary>
    public sealed class GenomeException : Exception
    {
        /// <summary>Creates the exception with a message.</summary>
        public GenomeException(string message) : base(message) { }
    }
}
