namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// Minimal plant record. Growth, health and genes are added by the growth task; hands only needs identity,
    /// moisture and death.
    /// </summary>
    public sealed class Plant
    {
        /// <summary>Unique plant id.</summary>
        public int Id { get; }
        /// <summary>Variety id.</summary>
        public string VarietyId { get; }
        /// <summary>Moisture 0-100.</summary>
        public float Moisture { get; set; } = 50f;
        /// <summary>True once the plant has died.</summary>
        public bool IsDead { get; set; }

        /// <summary>Creates a plant.</summary>
        public Plant(int id, string varietyId) { Id = id; VarietyId = varietyId; }
    }
}
