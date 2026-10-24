namespace PlantNursery.Sim.Core
{
    /// <summary>Fixed order in which systems update each tick (decisions 0013, 0014).</summary>
    public enum SystemPhase
    {
        /// <summary>Game clock.</summary>
        Time = 0,
        /// <summary>Reserved for later (decision 0013). No system is registered here by default.</summary>
        Weather = 1,
        /// <summary>Plant growth; reads the water meter as left by the previous tick.</summary>
        Growth = 2,
        /// <summary>Watering and drying.</summary>
        Watering = 3,
        /// <summary>Moods and needs.</summary>
        Behaviour = 4,
        /// <summary>Movement.</summary>
        Mobility = 5,
        /// <summary>Customer orders.</summary>
        Orders = 6,
        /// <summary>Money and guards.</summary>
        Economy = 7,
    }
}
