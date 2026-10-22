using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>Item rotation in 90 degree steps.</summary>
    public enum Rotation { Deg0 = 0, Deg90 = 90, Deg180 = 180, Deg270 = 270 }

    /// <summary>Footprint helpers.</summary>
    public static class Footprints
    {
        /// <summary>Footprint after rotation: 90 and 270 swap width and depth. The anchor (south-west cell) is unchanged.</summary>
        public static Size2 Rotate(Size2 size, Rotation rotation)
        {
            return rotation == Rotation.Deg90 || rotation == Rotation.Deg270
                ? new Size2(size.D, size.W)
                : size;
        }
    }

    /// <summary>Integer floor cell coordinate (x, z); (0,0) is the plot's south-west corner.</summary>
    public readonly struct Cell : System.IEquatable<Cell>
    {
        /// <summary>East-west coordinate.</summary>
        public readonly int X;
        /// <summary>North-south coordinate.</summary>
        public readonly int Z;
        /// <summary>Creates a cell.</summary>
        public Cell(int x, int z) { X = x; Z = z; }
        public bool Equals(Cell other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => X * 397 ^ Z;
        public override string ToString() => "(" + X + "," + Z + ")";
    }
}
