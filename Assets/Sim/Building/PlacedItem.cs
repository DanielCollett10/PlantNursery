namespace PlantNursery.Sim.Building
{
    /// <summary>An item placed on the floor grid.</summary>
    public sealed class PlacedItem
    {
        /// <summary>Unique id of this placement.</summary>
        public int Id { get; }
        /// <summary>Item definition id.</summary>
        public string ItemId { get; }
        /// <summary>South-west anchor cell.</summary>
        public Cell Anchor { get; }
        /// <summary>Rotation.</summary>
        public Rotation Rotation { get; }
        /// <summary>Rotated footprint width in cells.</summary>
        public int Width { get; }
        /// <summary>Rotated footprint depth in cells.</summary>
        public int Depth { get; }

        public PlacedItem(int id, string itemId, Cell anchor, Rotation rotation, int width, int depth)
        {
            Id = id; ItemId = itemId; Anchor = anchor; Rotation = rotation; Width = width; Depth = depth;
        }
    }

    /// <summary>Asks whether surface items sit on a placed floor item. Implemented by the surface layer (build-3).</summary>
    public interface ISurfaceOccupancy
    {
        /// <summary>True if any Surface item sits on the placed item.</summary>
        bool HasItemsOn(PlacedItem item);
    }

    /// <summary>Stub used until the surface layer exists: nothing is ever on top.</summary>
    public sealed class NoSurfaceOccupancy : ISurfaceOccupancy
    {
        public bool HasItemsOn(PlacedItem item) => false;
    }
}
