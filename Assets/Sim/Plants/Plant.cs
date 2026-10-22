using PlantNursery.Sim.Content;
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
        /// <summary>Cell index within its container (0 for pots).</summary>
        public int CellIndex { get; set; }
        /// <summary>True once the plant has died.</summary>
        public bool IsDead { get; set; }

        /// <summary>Creates a plant without a genome.</summary>
        public Plant(int id, string varietyId) { Id = id; VarietyId = varietyId; }

        /// <summary>Creates a plant with a genome.</summary>
        public Plant(int id, string varietyId, Genome genome) : this(id, varietyId) { Genome = genome; }
    }
}
