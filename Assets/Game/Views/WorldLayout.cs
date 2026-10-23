using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Pure sim-to-world mapping (decision 0006): cells are 0.5 m, surface units 0.1 m, origin at the plot's
    /// south-west corner. No rules; only maths so it can be unit tested.
    /// </summary>
    public static class WorldLayout
    {
        /// <summary>Metres per floor cell.</summary>
        public const float CellSize = 0.5f;
        /// <summary>Metres per surface unit.</summary>
        public const float UnitSize = 0.1f;

        /// <summary>Leaf colour for the green allele (#3FA34D).</summary>
        public static readonly Color LeafGreen = new Color(0x3F / 255f, 0xA3 / 255f, 0x4D / 255f);
        /// <summary>Leaf colour for the red allele (#C0392B).</summary>
        public static readonly Color LeafRed = new Color(0xC0 / 255f, 0x39 / 255f, 0x2B / 255f);
        /// <summary>Leaf colour for the yellow allele (#E5C02A).</summary>
        public static readonly Color LeafYellow = new Color(0xE5 / 255f, 0xC0 / 255f, 0x2A / 255f);
        /// <summary>Dead plant colour (#6E6253).</summary>
        public static readonly Color DeadColour = new Color(0x6E / 255f, 0x62 / 255f, 0x53 / 255f);
        /// <summary>Stem colour (placeholder green #4CAF50).</summary>
        public static readonly Color StemGreen = new Color(0x4C / 255f, 0xAF / 255f, 0x50 / 255f);

        /// <summary>Yaw (degrees about Y) for a sim rotation.</summary>
        public static float Yaw(Rotation r) => (int)r;

        /// <summary>
        /// Pivot position of a prefab whose pivot is its south-west corner, so that the rotated footprint
        /// (<paramref name="width"/> x <paramref name="depth"/> after rotation, in metres) has its south-west corner at
        /// <paramref name="swCorner"/>. Yaw is clockwise seen from above.
        /// </summary>
        public static Vector3 PivotFor(Vector3 swCorner, Rotation r, float width, float depth)
        {
            switch (r)
            {
                case Rotation.Deg90: return swCorner + new Vector3(0f, 0f, depth);
                case Rotation.Deg180: return swCorner + new Vector3(width, 0f, depth);
                case Rotation.Deg270: return swCorner + new Vector3(width, 0f, 0f);
                default: return swCorner;
            }
        }

        /// <summary>World position (y = 0) of a floor item's pivot; cell x 0.5 m plus the plot origin.</summary>
        public static Vector3 FloorPosition(PlacedItem item, Vector3 plotOrigin)
            => PivotFor(plotOrigin + new Vector3(item.Anchor.X * CellSize, 0f, item.Anchor.Z * CellSize),
                item.Rotation, item.Width * CellSize, item.Depth * CellSize);

        /// <summary>World position of a surface item's pivot at the given top height; unit x 0.1 m plus the plot origin.</summary>
        public static Vector3 SurfacePosition(SurfaceItem item, Vector3 plotOrigin, float topHeight)
            => PivotFor(plotOrigin + new Vector3(item.Anchor.U * UnitSize, topHeight, item.Anchor.V * UnitSize),
                item.Rotation, item.Width * UnitSize, item.Depth * UnitSize);

        /// <summary>Uniform plant scale by stage: Seed 0.1 up to Mature 1.0 in even steps.</summary>
        public static float PlantScale(PlantStage stage)
            => 0.1f + 0.9f * ((int)stage / (float)(int)PlantStage.Mature);

        /// <summary>Wilt amount 0-1: 0 while moisture is 20 or more, 1 at moisture 0.</summary>
        public static float WiltAmount(float moisture) => Mathf.Clamp01(1f - moisture / 20f);

        /// <summary>Leaf colour for a leafColour phenotype allele; unknown or null gives green.</summary>
        public static Color LeafColour(string allele)
        {
            switch (allele)
            {
                case "red": return LeafRed;
                case "yellow": return LeafYellow;
                default: return LeafGreen;
            }
        }

        /// <summary>Tint after wilting (desaturate up to 60%) or death (brown-grey).</summary>
        public static Color Tinted(Color baseColour, float wilt, bool dead)
        {
            if (dead) return DeadColour;
            Color.RGBToHSV(baseColour, out var h, out var s, out var v);
            return Color.HSVToRGB(h, s * (1f - 0.6f * wilt), v * (1f - 0.2f * wilt));
        }

        /// <summary>Local centre (metres, container pivot space, unrotated) of plant cell <paramref name="index"/> in a footprint of units.</summary>
        public static Vector3 CellCentre(int index, int cellCount, int widthUnits, int depthUnits)
        {
            var cols = Mathf.Max(1, Mathf.Min(cellCount, widthUnits));
            var rows = Mathf.Max(1, (cellCount + cols - 1) / cols);
            var col = index % cols;
            var row = index / cols;
            return new Vector3((col + 0.5f) / cols * widthUnits * UnitSize, 0f, (row + 0.5f) / rows * depthUnits * UnitSize);
        }
    }
}
