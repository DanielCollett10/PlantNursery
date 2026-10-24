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
        /// <summary>True if a plug tray was also added (to the build inventory, or to the crate on the crate path).</summary>
        public bool TrayAdded { get; }
        /// <summary>True when the packet went into the storage crate as counted stock (crate path); then <see cref="SurfaceItemId"/> is -1.</summary>
        public bool InCrate { get; }

        /// <summary>Creates the event for a packet placed on the delivery shelf (old path).</summary>
        public NeighbourGift(string itemId, string varietyId, int surfaceItemId, bool trayAdded)
        {
            ItemId = itemId; VarietyId = varietyId; SurfaceItemId = surfaceItemId; TrayAdded = trayAdded;
        }

        /// <summary>Creates the event for a packet put into the storage crate (crate path).</summary>
        public NeighbourGift(string itemId, string varietyId, bool trayAdded)
        {
            ItemId = itemId; VarietyId = varietyId; SurfaceItemId = -1; TrayAdded = trayAdded; InCrate = true;
        }
    }
}
