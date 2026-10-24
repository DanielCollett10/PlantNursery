using System;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Economy
{
    /// <summary>Pure price rules (plants.md Value). Nothing sells yet.</summary>
    public static class PlantPricing
    {
        /// <summary>Listed price x the quality multiplier from economy.json, rounded down. Never negative.</summary>
        public static int PriceFor(int listedPrice, QualityGrade quality, EconomyConfig economy)
        {
            // decimal avoids float error such as 5 x 1.4f = 6.9999998
            var mult = (decimal)economy.QualityMultipliers[(int)quality];
            return Math.Max(0, (int)Math.Floor(listedPrice * mult));
        }
    }
}
