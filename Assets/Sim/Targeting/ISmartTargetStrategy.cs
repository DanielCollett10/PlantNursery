namespace PlantNursery.Sim.Targeting
{
    /// <summary>
    /// One smart-targeting action (Water, Scrape; Feed and Catch later). A strategy says which candidates it can act on
    /// and how to rank them. <see cref="SmartTarget.Pick"/> does the rest: pending claims, the ranking and the id tie-break.
    /// New actions are new classes implementing this interface.
    /// </summary>
    public interface ISmartTargetStrategy
    {
        /// <summary>True if the action can act on this candidate at all.</summary>
        bool IsEligible(TargetCandidate candidate);

        /// <summary>
        /// Rank two eligible candidates: negative if <paramref name="a"/> is preferred, positive if
        /// <paramref name="b"/> is, zero if they tie (the lowest entity id then wins).
        /// </summary>
        int Compare(TargetCandidate a, TargetCandidate b);
    }
}
