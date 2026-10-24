using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Storage
{
    /// <summary>
    /// Water a container (pot or tray) with the can the player holds (plants.md, Watering). Sets its meter to 100
    /// and uses the container's <c>wateringCost</c> charges. Rejections: <c>NoCan</c>, <c>NotFound</c>, <c>CanEmpty</c>.
    /// </summary>
    public sealed class WaterContainer : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Entity id of the container to water.</summary>
        public int ContainerId { get; }
        /// <inheritdoc />
        public string TargetKey => "container:" + ContainerId;
        /// <summary>Creates the command.</summary>
        public WaterContainer(PlayerId player, int containerId) { PlayerId = player; ContainerId = containerId; }
    }

    /// <summary>Raised when a container is watered.</summary>
    public sealed class ContainerWatered : ISimEvent
    {
        /// <summary>The player who watered.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The container entity id.</summary>
        public int ContainerId { get; }
        /// <summary>Creates the event.</summary>
        public ContainerWatered(PlayerId player, int containerId) { PlayerId = player; ContainerId = containerId; }
    }
}
