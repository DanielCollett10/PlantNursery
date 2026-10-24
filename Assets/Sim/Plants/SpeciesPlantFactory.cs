using System;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Genetics;

namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// The one place species plants are made. Genes come from the variety's fixed alleles (no randomness in M2).
    /// </summary>
    public sealed class SpeciesPlantFactory
    {
        private readonly SimWorld _world;
        private readonly ContentDb _content;

        /// <summary>Creates the factory.</summary>
        public SpeciesPlantFactory(SimWorld world, ContentDb content)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>Create a seed-stage plant of a species variety.</summary>
        /// <exception cref="GenomeException">If the species or variety is unknown.</exception>
        public Plant Create(string speciesId, string varietyId, string containerId, int cell)
        {
            var genome = new Genome(_content, speciesId, varietyId); // validates before an id is spent
            return new Plant(_world.NextEntityId(), speciesId, varietyId, genome, containerId, cell);
        }
    }
}
