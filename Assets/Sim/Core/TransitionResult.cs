namespace PlantNursery.Sim.Core
{
    /// <summary>Outcome of checking or making a state transition.</summary>
    public enum TransitionResult
    {
        /// <summary>The transition is allowed (and, for <c>TryTransition</c>, has been made).</summary>
        Applied = 0,
        /// <summary>The transition table has no entry from the current state to the requested state.</summary>
        Illegal,
        /// <summary>The entry exists but its guard returned false.</summary>
        GuardFailed
    }
}
