using PlantNursery.Sim.Building;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Pure aim maths for the build ghost: world point to anchor cell/unit (footprint centred on the aim point),
    /// rotation stepping and the local "player is standing here" check. No game rules (the sim validates).
    /// </summary>
    public static class BuildAim
    {
        /// <summary>South-west anchor cell that centres a <paramref name="width"/> x <paramref name="depth"/> cell footprint on <paramref name="point"/>.</summary>
        public static Cell FloorAnchor(Vector3 point, Vector3 plotOrigin, int width, int depth)
            => new Cell(
                Mathf.FloorToInt((point.x - plotOrigin.x) / WorldLayout.CellSize - width / 2f + 0.5f),
                Mathf.FloorToInt((point.z - plotOrigin.z) / WorldLayout.CellSize - depth / 2f + 0.5f));

        /// <summary>South-west anchor unit that centres a <paramref name="width"/> x <paramref name="depth"/> unit footprint on <paramref name="point"/>.</summary>
        public static Unit SurfaceAnchor(Vector3 point, Vector3 plotOrigin, int width, int depth)
            => new Unit(
                Mathf.FloorToInt((point.x - plotOrigin.x) / WorldLayout.UnitSize - width / 2f + 0.5f),
                Mathf.FloorToInt((point.z - plotOrigin.z) / WorldLayout.UnitSize - depth / 2f + 0.5f));

        /// <summary>The next rotation, 90 degrees clockwise.</summary>
        public static Rotation Next(Rotation r)
        {
            switch (r)
            {
                case Rotation.Deg0: return Rotation.Deg90;
                case Rotation.Deg90: return Rotation.Deg180;
                case Rotation.Deg180: return Rotation.Deg270;
                default: return Rotation.Deg0;
            }
        }

        /// <summary>World-space (x/z) rectangle of a floor footprint: min corner and size in metres.</summary>
        public static Rect FloorRect(Cell anchor, int width, int depth, Vector3 plotOrigin)
            => new Rect(plotOrigin.x + anchor.X * WorldLayout.CellSize, plotOrigin.z + anchor.Z * WorldLayout.CellSize,
                width * WorldLayout.CellSize, depth * WorldLayout.CellSize);

        /// <summary>
        /// True if a standing capsule (circle of <paramref name="radius"/> at <paramref name="feet"/> in x/z, spanning
        /// <paramref name="height"/> above the feet) overlaps a floor item of <paramref name="itemHeight"/> metres inside <paramref name="rect"/>.
        /// A local physics-style check, not a sim rule (docs/design/building.md).
        /// </summary>
        public static bool CapsuleOverlaps(Rect rect, float itemHeight, Vector3 feet, float radius, float height)
        {
            if (feet.y >= itemHeight || feet.y + height <= 0f) return false;
            var cx = Mathf.Clamp(feet.x, rect.xMin, rect.xMax);
            var cz = Mathf.Clamp(feet.z, rect.yMin, rect.yMax);
            var dx = feet.x - cx;
            var dz = feet.z - cz;
            return dx * dx + dz * dz < radius * radius;
        }
    }
}
