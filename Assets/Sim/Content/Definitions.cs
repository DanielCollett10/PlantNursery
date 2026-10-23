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
    }

    /// <summary>A gene definition.</summary>
    public sealed class GeneDef
    {
        /// <summary>Unique id.</summary>
        public string Id;
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
