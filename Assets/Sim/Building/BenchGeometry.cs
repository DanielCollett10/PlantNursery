using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>
    /// Bench-local to nursery-wide unit mapping shared by <see cref="Neighbours"/>, <see cref="SlotPointer"/> and
    /// <see cref="SlotStepper"/>. Bench-local: <c>u</c> along from the left, <c>v</c> across from the front, as seen from the
    /// working edge, in 0.1 m units. Rotation turns the bench clockwise seen from above; at 0 the front faces south and
    /// <c>u</c> runs east. World units are cell * <see cref="Neighbours.UnitsPerCell"/>.
    /// </summary>
    internal static class BenchGeometry
    {
        public static int Length(ItemDef def) => def.FloorFootprint.W * Neighbours.UnitsPerCell;

        public static int Depth(ItemDef def) => def.FloorFootprint.D * Neighbours.UnitsPerCell;

        /// <summary>First bench-local v of the planting area (strip side is below it).</summary>
        public static int PlantingV0(ItemDef def) =>
            Depth(def) - def.Bench.PlantingBackOffsetUnits - def.Bench.PlantingDepthUnits;

        /// <summary>World unit index of a bench-local unit index (arithmetic also works just outside the bench).</summary>
        public static void ToWorld(PlacedItem p, int length, int depth, int u, int v, out int x, out int z)
        {
            int lx, lz;
            switch (p.Rotation)
            {
                case Rotation.Deg90: lx = v; lz = length - 1 - u; break;
                case Rotation.Deg180: lx = length - 1 - u; lz = depth - 1 - v; break;
                case Rotation.Deg270: lx = depth - 1 - v; lz = u; break;
                default: lx = u; lz = v; break;
            }
            x = p.Anchor.X * Neighbours.UnitsPerCell + lx;
            z = p.Anchor.Z * Neighbours.UnitsPerCell + lz;
        }

        /// <summary>World unit index to bench-local unit index. False if the unit is outside the bench footprint.</summary>
        public static bool ToLocal(PlacedItem p, int length, int depth, int x, int z, out int u, out int v)
        {
            var lx = x - p.Anchor.X * Neighbours.UnitsPerCell;
            var lz = z - p.Anchor.Z * Neighbours.UnitsPerCell;
            switch (p.Rotation)
            {
                case Rotation.Deg90: v = lx; u = length - 1 - lz; break;
                case Rotation.Deg180: u = length - 1 - lx; v = depth - 1 - lz; break;
                case Rotation.Deg270: v = depth - 1 - lx; u = lz; break;
                default: u = lx; v = lz; break;
            }
            return u >= 0 && v >= 0 && u < length && v < depth;
        }

        /// <summary>World position (units, continuous) of a continuous bench-local position.</summary>
        public static void ToWorldF(PlacedItem p, int length, int depth, double u, double v, out double x, out double z)
        {
            double lx, lz;
            switch (p.Rotation)
            {
                case Rotation.Deg90: lx = v; lz = length - u; break;
                case Rotation.Deg180: lx = length - u; lz = depth - v; break;
                case Rotation.Deg270: lx = depth - v; lz = u; break;
                default: lx = u; lz = v; break;
            }
            x = p.Anchor.X * Neighbours.UnitsPerCell + lx;
            z = p.Anchor.Z * Neighbours.UnitsPerCell + lz;
        }
    }
}
