using System;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>
    /// The fixed slot grid of one container size on one bench type (docs/design/building.md, Slot grid).
    /// Coordinates are planting-area-local 0.1 m units: <c>u</c> along the bench, <c>v</c> across it,
    /// measured from the planting area's left-front corner. Only slots wholly inside the area exist.
    /// </summary>
    public readonly struct SlotLayout
    {
        /// <summary>Slots along the bench.</summary>
        public int Cols { get; }
        /// <summary>Slot rows across the planting area.</summary>
        public int Rows { get; }
        /// <summary>Slot extent along the bench, in units.</summary>
        public int AlongUnits { get; }
        /// <summary>Slot extent across the bench, in units.</summary>
        public int AcrossUnits { get; }

        private SlotLayout(int cols, int rows, int along, int across)
        {
            Cols = cols; Rows = rows; AlongUnits = along; AcrossUnits = across;
        }

        /// <summary>
        /// Layout for a container on a bench. Rectangular containers (plug trays) sit with the long side across the bench,
        /// so the along/across extents are the container's smaller/larger dimension.
        /// </summary>
        public static SlotLayout For(BenchDef bench, ContainerDef size)
        {
            if (bench == null) throw new ArgumentNullException(nameof(bench));
            if (size == null) throw new ArgumentNullException(nameof(size));
            var along = Math.Min(size.AlongUnits, size.AcrossUnits);
            var across = Math.Max(size.AlongUnits, size.AcrossUnits);
            if (along <= 0) return new SlotLayout(0, 0, 0, 0);
            return new SlotLayout(bench.PlantingLengthUnits / along, bench.PlantingDepthUnits / across, along, across);
        }

        /// <summary>True if slot (col, row) exists.</summary>
        public bool Contains(int col, int row) => col >= 0 && row >= 0 && col < Cols && row < Rows;

        /// <summary>First unit along the bench covered by a column.</summary>
        public int StartU(int col) => col * AlongUnits;

        /// <summary>First unit across the bench covered by a row.</summary>
        public int StartV(int row) => row * AcrossUnits;
    }
}
