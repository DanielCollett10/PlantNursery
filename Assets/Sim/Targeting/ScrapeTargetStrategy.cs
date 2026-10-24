using System;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Targeting
{
    /// <summary>
    /// Walking-around scraping (controls.md): only containers with harvestable moss are eligible
    /// (<see cref="MossRules.IsHarvestable"/>, the same rule ScrapeMoss checks), and the nearest one wins.
    /// </summary>
    public sealed class ScrapeTargetStrategy : ISmartTargetStrategy
    {
        private readonly ContentDb _content;
        private readonly Func<int, Plant> _plantById;

        /// <summary>Creates the strategy. <paramref name="plantById"/> looks plants up (null if unknown).</summary>
        public ScrapeTargetStrategy(ContentDb content, Func<int, Plant> plantById)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _plantById = plantById ?? throw new ArgumentNullException(nameof(plantById));
        }

        /// <inheritdoc />
        public bool IsEligible(TargetCandidate candidate)
        {
            if (candidate.Container == null) return false;
            foreach (var id in candidate.Container.PlantIds)
                if (MossRules.IsHarvestable(_plantById(id), _content)) return true;
            return false;
        }

        /// <inheritdoc />
        public int Compare(TargetCandidate a, TargetCandidate b) => a.Distance.CompareTo(b.Distance);
    }
}
