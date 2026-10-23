using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Economy
{
    /// <summary>Raised when the soft-lock guard delivers a free seed packet (docs/design/economy.md).</summary>
    public sealed class NeighbourGift : ISimEvent
    {
        /// <summary>Item id of the packet delivered.</summary>
        public string ItemId { get; }
        /// <summary>Variety of the seeds in the packet.</summary>
        public string VarietyId { get; }
        /// <summary>Surface placement id of the packet.</summary>
        public int SurfaceItemId { get; }
        /// <summary>True if a plug tray was also added to the build inventory.</summary>
        public bool TrayAdded { get; }

        /// <summary>Creates the event.</summary>
        public NeighbourGift(string itemId, string varietyId, int surfaceItemId, bool trayAdded)
        {
            ItemId = itemId; VarietyId = varietyId; SurfaceItemId = surfaceItemId; TrayAdded = trayAdded;
        }
    }
}
