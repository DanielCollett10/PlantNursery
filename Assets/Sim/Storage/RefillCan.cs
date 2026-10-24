using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Storage
{
    /// <summary>
    /// Refill the can the player holds at a rain barrel (plants.md, Watering): the can goes to its full capacity.
    /// Rejections: <c>NoCan</c>, <c>NotABarrel</c> (also for an unknown barrel id).
    /// </summary>
    public sealed class RefillCan : ICommand
    {
        /// <summary>Item id of the rain barrel floor item.</summary>
        public const string RainBarrelItemId = "rain_barrel";

        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Placement id of the barrel on the floor.</summary>
        public int BarrelId { get; }
        /// <summary>Creates the command.</summary>
        public RefillCan(PlayerId player, int barrelId) { PlayerId = player; BarrelId = barrelId; }
    }
}
