namespace PlantNursery.Sim.Core
{
    /// <summary>
    /// A command that acts on one target (a pot, a slot). The world uses <see cref="TargetKey"/> for first-request-wins:
    /// if an earlier command this tick already used the target and this one fails, it is rejected <c>Taken</c>.
    /// It also lists queued targets as pending claims (<see cref="SimWorld.PendingTargets"/>).
    /// </summary>
    public interface ITargetsEntity : ICommand
    {
        /// <summary>Stable key naming the target, equal for commands that fight over the same thing.</summary>
        string TargetKey { get; }
    }
}
