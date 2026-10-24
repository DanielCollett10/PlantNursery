using System.Collections.Generic;

namespace PlantNursery.Sim.Content
{
    /// <summary>How a sown tray is filled (docs/design/plants.md, Sowing).</summary>
    public enum TrayMode { WholeTray, PerCell }

    /// <summary>Where and how a species is sown.</summary>
    public sealed class SowingDef
    {
        /// <summary>Container ids the species can be sown into.</summary>
        public IReadOnlyList<string> SowInto;
        /// <summary>Whether sowing fills the whole tray or one cell.</summary>
        public TrayMode TrayMode;
    }

    /// <summary>One growth stage of a species.</summary>
    public sealed class StageDef
    {
        /// <summary>Stage name, unique within the species.</summary>
        public string Name;
        /// <summary>In-game days spent in the stage (0 for a final stage).</summary>
        public int Days;
        /// <summary>Container ids the plant may be in during this stage.</summary>
        public IReadOnlyList<string> AllowedContainers;
    }

    /// <summary>A variety nested in a species: fixed alleles for each of the species' genes.</summary>
    public sealed class SpeciesVarietyDef
    {
        /// <summary>Globally unique variety id.</summary>
        public string Id;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>Owning species id.</summary>
        public string SpeciesId;
        /// <summary>Alleles per gene id; contains exactly the species' genes.</summary>
        public IReadOnlyDictionary<string, AllelePair> Genes;
    }

    /// <summary>A plant species (docs/design/species.md).</summary>
    public sealed class SpeciesDef
    {
        /// <summary>Unique id.</summary>
        public string Id;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>Stages in order.</summary>
        public IReadOnlyList<StageDef> Stages;
        /// <summary>Growth multiplier per season, indexed by (int)Season.</summary>
        public IReadOnlyList<float> SeasonMultipliers;
        /// <summary>Ids of the genes this species carries.</summary>
        public IReadOnlyList<string> GeneIds;
        /// <summary>Days before a harvested plant is harvestable again (0 = does not regrow).</summary>
        public int RegrowDays;
        /// <summary>Behaviour module names (empty for benign species).</summary>
        public IReadOnlyList<string> Modules;
        /// <summary>Sowing data.</summary>
        public SowingDef Sowing;
        /// <summary>Varieties by id.</summary>
        public IReadOnlyDictionary<string, SpeciesVarietyDef> Varieties;
    }
}
