using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Targeting
{
    /// <summary>
    /// One thing in reach that a smart-targeted action could act on, with its distance from the player
    /// (metres, measured by Unity). <see cref="TargetKey"/> matches the <c>ITargetsEntity</c> key of the command
    /// the action will submit, so another player's pending claim on it can be skipped.
    /// </summary>
    public sealed class TargetCandidate
    {
        /// <summary>Entity id. Ties between equal candidates go to the lowest id.</summary>
        public int EntityId { get; }

        /// <summary>Claim key of the command this candidate would be sent to (e.g. <c>container:7</c>).</summary>
        public string TargetKey { get; }

        /// <summary>Distance from the player in metres.</summary>
        public double Distance { get; }

        /// <summary>The container, for container targets; null otherwise.</summary>
        public Container Container { get; }

        private TargetCandidate(int entityId, string targetKey, double distance, Container container)
        {
            EntityId = entityId; TargetKey = targetKey; Distance = distance; Container = container;
        }

        /// <summary>A pot or tray in reach. Its key is <c>container:&lt;id&gt;</c>, as used by WaterContainer and ScrapeMoss.</summary>
        public static TargetCandidate ForContainer(Container container, double distanceMetres)
        {
            if (container == null) throw new System.ArgumentNullException(nameof(container));
            return new TargetCandidate(container.Id, "container:" + container.Id, distanceMetres, container);
        }
    }
}
