using System.Collections.Generic;

namespace PlantNursery.Sim.Content
{
    /// <summary>Placement layer of an item (see docs/design/building.md).</summary>
    public enum ItemLayer { Floor, Surface, Overhead, Zone }

    /// <summary>Plant growth stages in order.</summary>
    public enum PlantStage { Seed, Seedling, Rooted, Young, Established, Mature }

    /// <summary>Gene kinds (see docs/design/genetics.md).</summary>
    public enum GeneKind { Mendelian, Blend }

    /// <summary>Seasons in order; growth multipliers are keyed by these.</summary>
    public enum Season { Spring, Summer, Autumn }

    /// <summary>Plant quality steps in order (docs/design/plants.md, Quality); value multipliers are keyed by these.</summary>
    public enum QualityGrade { Poor, Good, Excellent }

    /// <summary>Pot sizes in order; value caps are keyed by these. <c>Large</c> is the species' final size (no cap).</summary>
    public enum PotSize { Small, Medium, Large }

    /// <summary>Integer width x depth.</summary>
    public readonly struct Size2
    {
        /// <summary>Width.</summary>
        public readonly int W;
        /// <summary>Depth.</summary>
        public readonly int D;
        /// <summary>Creates a size.</summary>
        public Size2(int w, int d) { W = w; D = d; }
    }

    /// <summary>Time and pacing values (docs/design/time.md).</summary>
    public sealed class TimeConfig
    {
        /// <summary>Minute of day the morning starts.</summary>
        public int DayStartMinute;
        /// <summary>Minute of day the night starts.</summary>
        public int NightStartMinute;
        /// <summary>Minute of day the morning phase ends and daytime begins.</summary>
        public int MorningEndMinute;
        /// <summary>Minute of day the evening phase begins.</summary>
        public int EveningStartMinute;
        /// <summary>Real seconds for the playable day (dayStart to nightStart).</summary>
        public float RealSecondsDay;
        /// <summary>Multiplier (0..1) applied to need and drying rates at night.</summary>
        public float NightRateMultiplier = 0.25f;
        /// <summary>Days per season.</summary>
        public int SeasonLengthDays;
        /// <summary>Shop opening minute (M2).</summary>
        public int ShopOpenMinute;
        /// <summary>Shop closing minute (M2).</summary>
        public int ShopCloseMinute;
    }

    /// <summary>An item type that can be bought and placed.</summary>
    public sealed class ItemDef
    {
        /// <summary>Unique id.</summary>
        public string Id;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>Layer it is placed on.</summary>
        public ItemLayer Layer;
        /// <summary>Footprint in floor cells (Floor/Overhead/Zone items); zero for Surface items.</summary>
        public Size2 FloorFootprint;
        /// <summary>Footprint in 0.1 m mini-grid units (Surface items); zero otherwise.</summary>
        public Size2 SurfaceFootprint;
        /// <summary>True if surface items may sit on it.</summary>
        public bool ProvidesSurface;
        /// <summary>Buy price in coins.</summary>
        public int Price;
        /// <summary>Water charges a full watering can holds; 0 for items that are not cans.</summary>
        public int WaterCapacity;
        /// <summary>Seeds in one packet (seed packets only; 0 otherwise).</summary>
        public int SeedCount;
        /// <summary>Species a seed/spore packet grows; null for other items.</summary>
        public string SpeciesId;
        /// <summary>Variety a seed/spore packet grows; null for other items.</summary>
        public string VarietyId;
        /// <summary>Seeds (or trays' worth of spores) in one packet; 0 for items that are not packets.</summary>
        public int PacketSeeds;
        /// <summary>How many things the item carries (carry trays: containers); 0 otherwise.</summary>
        public int Capacity;
        /// <summary>Planting area and working strip; null for items that are not benches.</summary>
        public BenchDef Bench;
    }

    /// <summary>A plant container type (docs/design/plants.md).</summary>
    public sealed class ContainerDef
    {
        /// <summary>Unique id.</summary>
        public string Id;
        /// <summary>The item that represents this container.</summary>
        public string ItemId;
        /// <summary>Number of plant cells.</summary>
        public int CellCount;
        /// <summary>Highest stage a plant can reach in it.</summary>
        public PlantStage MaxStage;
        /// <summary>Lowest stage it accepts.</summary>
        public PlantStage AcceptsFromStage;
        /// <summary>Moisture lost per hour.</summary>
        public float DryRatePerHour;
        /// <summary>Length of the container's footprint in 0.1 m units, along the bench.</summary>
        public int AlongUnits;
        /// <summary>Width of the container's footprint in 0.1 m units, across the bench.</summary>
        public int AcrossUnits;
        /// <summary>Water meter lost per in-game hour (docs/design/plants.md, Containers).</summary>
        public float DrainPerHour;
        /// <summary>Can charges one watering uses.</summary>
        public int WateringCost;
    }

    /// <summary>A gene definition.</summary>
    public sealed class GeneDef
    {
        /// <summary>Unique id.</summary>
        public string Id;
        /// <summary>Owning species id for species.json genes; null for the old pre-pivot genes.</summary>
        public string SpeciesId;
        /// <summary>Mendelian or Blend.</summary>
        public GeneKind Kind;
        /// <summary>Mendelian alleles, most dominant first. Empty for Blend.</summary>
        public IReadOnlyList<string> Alleles;
        /// <summary>Blend range minimum.</summary>
        public float Min;
        /// <summary>Blend range maximum.</summary>
        public float Max;
    }

    /// <summary>Two allele values for one gene. Mendelian uses the strings, Blend the numbers.</summary>
    public sealed class AllelePair
    {
        /// <summary>First Mendelian allele.</summary>
        public string A;
        /// <summary>Second Mendelian allele.</summary>
        public string B;
        /// <summary>First Blend value.</summary>
        public float ValueA;
        /// <summary>Second Blend value.</summary>
        public float ValueB;
    }

    /// <summary>A plant variety.</summary>
    public sealed class VarietyDef
    {
        /// <summary>Unique id.</summary>
        public string Id;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>Growth points gained per hour at full conditions.</summary>
        public float GrowthPerHour;
        /// <summary>Growth threshold per stage, indexed by (int)PlantStage. Non-decreasing.</summary>
        public IReadOnlyList<float> StageThresholds;
        /// <summary>Growth multiplier per season, indexed by (int)Season.</summary>
        public IReadOnlyList<float> SeasonGrowth;
        /// <summary>Alleles per gene id; contains every gene.</summary>
        public IReadOnlyDictionary<string, AllelePair> Genes;
    }

    /// <summary>Soft-lock guard settings ("a neighbour's gift").</summary>
    public sealed class SoftLockGuardConfig
    {
        /// <summary>Whether the guard runs.</summary>
        public bool Enabled;
        /// <summary>Variety gifted, or null for the cheapest.</summary>
        public string GiftVarietyId;
    }

    /// <summary>Economy values (docs/design/economy.md).</summary>
    public sealed class EconomyConfig
    {
        /// <summary>Starting coins.</summary>
        public int StartingMoney;
        /// <summary>Sell-back fraction of price (0..1).</summary>
        public float SellBackFraction;
        /// <summary>Soft-lock guard block.</summary>
        public SoftLockGuardConfig SoftLockGuard;
        /// <summary>Starting build inventory: item id to count.</summary>
        public IReadOnlyDictionary<string, int> StarterInventory;
        /// <summary>Value multiplier per <see cref="QualityGrade"/> (index = (int)grade): poor 0.6, good 1.0, excellent 1.4.</summary>
        public IReadOnlyList<float> QualityMultipliers;
        /// <summary>Value cap per <see cref="PotSize"/> (index = (int)size): small 0.5, medium 0.8, large 1.0.</summary>
        public IReadOnlyList<float> PotValueCaps;
        /// <summary>Care share (ticks not dry / ticks alive) at or above which a plant can reach excellent (0.9).</summary>
        public float ExcellentCareThreshold;
        /// <summary>Care share below which a plant drops to poor (0.6).</summary>
        public float PoorCareThreshold;
    }

    /// <summary>A plant placed in a bench's planting area at the start (plants.md, Sowing).</summary>
    public sealed class StarterSowing
    {
        /// <summary>Plug-tray slot index on the bench's planting area.</summary>
        public int Slot;
        /// <summary>Container item id (for example <c>plug_tray</c>).</summary>
        public string ContainerItemId;
        /// <summary>Variety sown into the container.</summary>
        public string VarietyId;
        /// <summary>Starting water meter, 0..100.</summary>
        public float Moisture;
    }

    /// <summary>An item on a bench's working strip at the start.</summary>
    public sealed class StarterStripItem
    {
        /// <summary>Slot index on the strip (row-major, from the strip's left-front corner).</summary>
        public int Slot;
        /// <summary>Item id; must be in the bench's <c>workStrip.accepts</c>.</summary>
        public string ItemId;
    }

    /// <summary>A count of an item a placed item starts holding (for example carry trays in the storage crate).</summary>
    public sealed class StarterStock
    {
        /// <summary>Item id.</summary>
        public string ItemId;
        /// <summary>How many, at least 1.</summary>
        public int Count;
    }

    /// <summary>One floor item of the day-one kit, with what it starts holding.</summary>
    public sealed class StarterPlacement
    {
        /// <summary>Floor item id.</summary>
        public string ItemId;
        /// <summary>South-west anchor cell, x.</summary>
        public int X;
        /// <summary>South-west anchor cell, z.</summary>
        public int Z;
        /// <summary>Rotation in degrees: 0, 90, 180 or 270.</summary>
        public int RotationDegrees;
        /// <summary>Sown plug trays (benches only). Empty for other items.</summary>
        public IReadOnlyList<StarterSowing> Planting;
        /// <summary>Strip items (benches only). Empty for other items.</summary>
        public IReadOnlyList<StarterStripItem> Strip;
        /// <summary>Stock the item starts holding. Empty for other items.</summary>
        public IReadOnlyList<StarterStock> Holds;
    }

    /// <summary>The day-one kit (economy.json <c>starterKit</c>): floor placements with their starting contents.</summary>
    public sealed class StarterKitDef
    {
        /// <summary>Placements in file order. Footprints never overlap.</summary>
        public IReadOnlyList<StarterPlacement> Placements = new List<StarterPlacement>();
    }

    /// <summary>Building values (docs/design/building.md).</summary>
    public sealed class BuildingConfig
    {
        /// <summary>Plot width in cells.</summary>
        public int PlotWidth;
        /// <summary>Plot depth in cells.</summary>
        public int PlotDepth;
        /// <summary>Auto-nudge radius in mini-grid units.</summary>
        public int NudgeRadius;
        /// <summary>How far (metres) the player can reach to interact.</summary>
        public float InteractReach = 3.0f;
    }
}
