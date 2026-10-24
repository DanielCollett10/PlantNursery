namespace PlantNursery.Sim.Plants
{
    /// <summary>Container water rules (docs/design/plants.md, Watering). Defaults match the design doc.</summary>
    public sealed class WaterConfig
    {
        /// <summary>In-game hours a plant must be continuously Dry before it Wilts (wiltAfterHours).</summary>
        public int WiltAfterHours { get; set; } = 12;
        /// <summary>In-game days a plant must be continuously Wilted before it dies (dieAfterWiltedDays).</summary>
        public int DieAfterWiltedDays { get; set; } = 3;
    }
}
