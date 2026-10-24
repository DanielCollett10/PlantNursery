using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Genetics;

namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// A plant individual (docs/design/plants.md). Plain data; growth rules live in the growth system.
    /// </summary>
    public sealed class Plant
    {
        /// <summary>Unique plant id.</summary>
        public int Id { get; }
        /// <summary>Variety id.</summary>
        public string VarietyId { get; }
        /// <summary>Genome (may be null for plants created without genetics, e.g. in simple tests).</summary>
        public Genome Genome { get; }
        /// <summary>Growth points accumulated.</summary>
        public float Growth { get; set; }
        /// <summary>Current stage.</summary>
        public PlantStage Stage { get; set; } = PlantStage.Seed;
        /// <summary>Moisture 0-100.</summary>
        public float Moisture { get; set; } = 50f;
        /// <summary>Health 0-100.</summary>
        public float Health { get; set; } = 100f;
        /// <summary>Minutes spent root-bound.</summary>
        public int RootBoundMinutes { get; set; }
        /// <summary>Minutes of good care.</summary>
        public int GoodCareMinutes { get; set; }
        /// <summary>Minutes alive.</summary>
        public int AliveMinutes { get; set; }
        /// <summary>Ticks lived while alive (care counter, plants.md Quality).</summary>
        public int TicksAlive { get; set; }
        /// <summary>Ticks lived with water in the container (care counter).</summary>
        public int TicksNotDry { get; set; }
        /// <summary>True while the container meter is 0.</summary>
        public bool IsDry { get; set; }
        /// <summary>True while Wilted (set by the water system; also true for plants without a lifecycle machine).</summary>
        public bool IsWilted { get; set; }
        /// <summary>Consecutive Dry ticks.</summary>
        public int DryTicks { get; set; }
        /// <summary>Consecutive ticks spent Wilted.</summary>
        public int WiltedTicks { get; set; }
        /// <summary>Cell index within its container (0 for pots).</summary>
        public int CellIndex { get; set; }
        /// <summary>True once the plant has died.</summary>
        public bool IsDead { get; set; }

        /// <summary>Species id (null for old-variety plants, which keep the old growth rules).</summary>
        public string SpeciesId { get; private set; }
        /// <summary>Container definition id the plant sits in (species plants).</summary>
        public string ContainerId { get; set; }
        /// <summary>Index into the species' stage list.</summary>
        public int StageIndex { get; set; }
        /// <summary>Progress through the current stage, 0 to 1.</summary>
        public float StageProgress { get; set; }
        /// <summary>Quality steps lost to bad care (0 = none).</summary>
        public int QualityStepsLost { get; set; }
        /// <summary>True when this plant follows the species rules.</summary>
        public bool IsSpeciesPlant => SpeciesId != null;
        /// <summary>Lifecycle machine: Growing, RootBound, Wilted, Dead. Null for old-variety plants.</summary>
        public StateMachine<PlantLifecycle> Lifecycle { get; private set; }

        /// <summary>Creates a species plant (use <see cref="SpeciesPlantFactory"/>).</summary>
        public Plant(int id, string speciesId, string varietyId, Genome genome, string containerId, int cell)
            : this(id, varietyId, genome)
        {
            SpeciesId = speciesId;
            ContainerId = containerId;
            CellIndex = cell;
            Lifecycle = new StateMachine<PlantLifecycle>(PlantLifecycle.Growing)
                .Allow(PlantLifecycle.Growing, PlantLifecycle.RootBound)
                .Allow(PlantLifecycle.RootBound, PlantLifecycle.Growing)
                .Allow(PlantLifecycle.Growing, PlantLifecycle.Wilted)
                .Allow(PlantLifecycle.RootBound, PlantLifecycle.Wilted)
                .Allow(PlantLifecycle.Wilted, PlantLifecycle.Growing)
                .Allow(PlantLifecycle.Wilted, PlantLifecycle.RootBound)
                .Allow(PlantLifecycle.Wilted, PlantLifecycle.Dead);
        }

        /// <summary>Creates a plant without a genome.</summary>
        public Plant(int id, string varietyId) { Id = id; VarietyId = varietyId; }

        /// <summary>Creates a plant with a genome.</summary>
        public Plant(int id, string varietyId, Genome genome) : this(id, varietyId) { Genome = genome; }
    }
}
