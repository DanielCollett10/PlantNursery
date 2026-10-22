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
        /// <summary>Genes by id.</summary>
        public IReadOnlyDictionary<string, GeneDef> GeneDefs { get; }
        /// <summary>Economy config.</summary>
        public EconomyConfig Economy { get; }
        /// <summary>Building config.</summary>
        public BuildingConfig Building { get; }

        /// <summary>Creates the database.</summary>
        public ContentDb(TimeConfig time, IReadOnlyDictionary<string, ItemDef> items,
            IReadOnlyDictionary<string, ContainerDef> containers,
            IReadOnlyDictionary<string, VarietyDef> varieties,
            IReadOnlyDictionary<string, GeneDef> genes,
            EconomyConfig economy, BuildingConfig building)
        {
            Time = time;
            ItemDefs = items;
            ContainerDefs = containers;
            VarietyDefs = varieties;
            GeneDefs = genes;
            Economy = economy;
            Building = building;
        }
    }
}
