using System;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Economy
{
    /// <summary>
    /// Where delivered seed packets land: the placed delivery shelf first, then any surface
    /// (docs/design/economy.md). Query-only; the caller does the placing.
    /// </summary>
    public static class DeliveryShelf
    {
        /// <summary>Item id of the delivery shelf.</summary>
        public const string ItemId = "delivery_shelf";

        /// <summary>The placed delivery shelf with the lowest id, or null if none is placed.</summary>
        public static PlacedItem Find(BuildState build)
        {
            PlacedItem best = null;
            foreach (var p in build.PlacedItems)
                if (p.ItemId == ItemId && (best == null || p.Id < best.Id)) best = p;
            return best;
        }

        /// <summary>
        /// Nearest free anchor for the item such that its whole footprint lies on the delivery shelf,
        /// or null if there is no shelf or no room on it.
        /// </summary>
        public static Unit? FindSpotOnShelf(BuildState build, SurfaceGrid surface, ItemDef def)
        {
            var shelf = Find(build);
            if (shelf == null) return null;
            var u0 = shelf.Anchor.X * SurfaceGrid.UnitsPerCell;
            var v0 = shelf.Anchor.Z * SurfaceGrid.UnitsPerCell;
            var u1 = u0 + shelf.Width * SurfaceGrid.UnitsPerCell;
            var v1 = v0 + shelf.Depth * SurfaceGrid.UnitsPerCell;
            var size = Footprints.Rotate(def.SurfaceFootprint, Rotation.Deg0);
            var radius = Math.Max(u1 - u0, v1 - v0);
            return surface.FindNearestFreeSpot(def.Id, Rotation.Deg0, new Unit(u0, v0), radius,
                a => a.U >= u0 && a.V >= v0 && a.U + size.W <= u1 && a.V + size.D <= v1);
        }

        /// <summary>
        /// Nearest free anchor for the item on any surface: nearest to the delivery shelf when one is placed,
        /// otherwise nearest to the plot's south-west corner. Null if no surface has room.
        /// </summary>
        public static Unit? FindSpotAnywhere(BuildState build, SurfaceGrid surface, ItemDef def)
        {
            var shelf = Find(build);
            var desired = shelf != null
                ? new Unit(shelf.Anchor.X * SurfaceGrid.UnitsPerCell, shelf.Anchor.Z * SurfaceGrid.UnitsPerCell)
                : new Unit(0, 0);
            // Every surface unit lies within one plot span of any other, so this radius covers the whole plot.
            var radius = Math.Max(build.PlotWidth, build.PlotDepth) * SurfaceGrid.UnitsPerCell;
            return surface.FindNearestFreeSpot(def.Id, Rotation.Deg0, desired, radius);
        }
    }
}
