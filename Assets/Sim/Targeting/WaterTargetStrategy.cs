namespace PlantNursery.Sim.Targeting
{
    /// <summary>
    /// Walking-around watering (controls.md): the driest container in reach, lowest water meter first, then nearest.
    /// </summary>
    public sealed class WaterTargetStrategy : ISmartTargetStrategy
    {
        /// <inheritdoc />
        public bool IsEligible(TargetCandidate candidate) => candidate.Container != null;

        /// <inheritdoc />
        public int Compare(TargetCandidate a, TargetCandidate b)
        {
            var meter = a.Container.WaterMeter.CompareTo(b.Container.WaterMeter);
            return meter != 0 ? meter : a.Distance.CompareTo(b.Distance);
        }
    }
}
