using System.Collections.Generic;

namespace PlantNursery.Sim.Content
{
    /// <summary>All validated content, as typed definitions. Immutable after loading.</summary>
    public sealed class ContentDb
    {
        /// <summary>Time config (with demo overrides applied if supplied).</summary>
        public TimeConfig Time { get; }
        /// <summary>Items by id.</summary>
        public IReadOnlyDictionary<string, ItemDef> ItemDefs { get; }
        /// <summary>Containers by id.</summary>
        public IReadOnlyDictionary<string, ContainerDef> ContainerDefs { get; }
        /// <summary>Varieties by id.</summary>
        public IReadOnlyDictionary<string, VarietyDef> VarietyDefs { get; }
        /// <summary>Old (pre-pivot) genes by id, used by <see cref="VarietyDefs"/>. Species genes are in <see cref="AllGeneDefs"/>.</summary>
        public IReadOnlyDictionary<string, GeneDef> GeneDefs { get; }
        /// <summary>Every gene in genes.json by id, old and species genes.</summary>
        public IReadOnlyDictionary<string, GeneDef> AllGeneDefs { get; }
        /// <summary>Species by id (species.json).</summary>
        public IReadOnlyDictionary<string, SpeciesDef> SpeciesDefs { get; }
        /// <summary>Economy config.</summary>
        public EconomyConfig Economy { get; }
        /// <summary>Building config.</summary>
        public BuildingConfig Building { get; }

        /// <summary>Creates the database.</summary>
        public ContentDb(TimeConfig time, IReadOnlyDictionary<string, ItemDef> items,
            IReadOnlyDictionary<string, ContainerDef> containers,
            IReadOnlyDictionary<string, VarietyDef> varieties,
            IReadOnlyDictionary<string, GeneDef> genes,
            EconomyConfig economy, BuildingConfig building,
            IReadOnlyDictionary<string, SpeciesDef> species = null,
            IReadOnlyDictionary<string, GeneDef> allGenes = null)
        {
            Time = time;
            ItemDefs = items;
            ContainerDefs = containers;
            VarietyDefs = varieties;
            GeneDefs = genes;
            Economy = economy;
            Building = building;
            SpeciesDefs = species ?? new Dictionary<string, SpeciesDef>();
            AllGeneDefs = allGenes ?? genes;
        }
    }
}
