using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>Why a slot placement or removal is refused.</summary>
    public enum SlotRejection
    {
        /// <summary>Accepted.</summary>
        None = 0,
        /// <summary>The placed item does not exist or is not a bench.</summary>
        NotABench,
        /// <summary>The container size is unknown or the slot is outside the planting area.</summary>
        NoSuchSlot,
        /// <summary>Some unit of the slot is already used.</summary>
        SlotOccupied,
        /// <summary>Removal only: that exact slot holds nothing.</summary>
        SlotEmpty
    }

    /// <summary>
    /// Which planting units of each bench are used. A slot is occupied if any of its units is used,
    /// so slots of different sizes block each other where they overlap.
    /// </summary>
    public sealed class SlotOccupancy
    {
        private readonly ContentDb _content;
        private readonly BuildState _build;
        private readonly Dictionary<long, string> _units = new Dictionary<long, string>();
        private readonly HashSet<string> _slots = new HashSet<string>();

        /// <summary>Creates occupancy over a build state.</summary>
        public SlotOccupancy(ContentDb content, BuildState build)
        {
            _content = content;
            _build = build;
        }

        /// <summary>Query-only: would placing into this slot be accepted? Never changes state.</summary>
        public SlotRejection CanPlaceInSlot(int benchId, string size, int col, int row)
        {
            var r = Resolve(benchId, size, col, row, out var layout, out var bench);
            if (r != SlotRejection.None) return r;
            for (var u = layout.StartU(col); u < layout.StartU(col) + layout.AlongUnits; u++)
                for (var v = layout.StartV(row); v < layout.StartV(row) + layout.AcrossUnits; v++)
                    if (_units.ContainsKey(Key(benchId, u, v))) return SlotRejection.SlotOccupied;
            return SlotRejection.None;
        }

        /// <summary>Mark a slot's units used. Returns the rejection, or None if placed.</summary>
        public SlotRejection PlaceInSlot(int benchId, string size, int col, int row)
        {
            var r = CanPlaceInSlot(benchId, size, col, row);
            if (r != SlotRejection.None) return r;
            Resolve(benchId, size, col, row, out var layout, out _);
            var tag = Tag(benchId, size, col, row);
            for (var u = layout.StartU(col); u < layout.StartU(col) + layout.AlongUnits; u++)
                for (var v = layout.StartV(row); v < layout.StartV(row) + layout.AcrossUnits; v++)
                    _units[Key(benchId, u, v)] = tag;
            _slots.Add(tag);
            return SlotRejection.None;
        }

        /// <summary>Query-only: would removing from this exact slot be accepted?</summary>
        public SlotRejection CanRemoveFromSlot(int benchId, string size, int col, int row)
        {
            var r = Resolve(benchId, size, col, row, out _, out _);
            if (r != SlotRejection.None) return r;
            return _slots.Contains(Tag(benchId, size, col, row)) ? SlotRejection.None : SlotRejection.SlotEmpty;
        }

        /// <summary>Free the units of a slot that was placed with exactly this address.</summary>
        public SlotRejection RemoveFromSlot(int benchId, string size, int col, int row)
        {
            var r = CanRemoveFromSlot(benchId, size, col, row);
            if (r != SlotRejection.None) return r;
            Resolve(benchId, size, col, row, out var layout, out _);
            for (var u = layout.StartU(col); u < layout.StartU(col) + layout.AlongUnits; u++)
                for (var v = layout.StartV(row); v < layout.StartV(row) + layout.AcrossUnits; v++)
                    _units.Remove(Key(benchId, u, v));
            _slots.Remove(Tag(benchId, size, col, row));
            return SlotRejection.None;
        }

        /// <summary>True if the unit (planting-area-local) of a bench is used.</summary>
        public bool IsUnitUsed(int benchId, int u, int v) => _units.ContainsKey(Key(benchId, u, v));

        private SlotRejection Resolve(int benchId, string size, int col, int row, out SlotLayout layout, out BenchDef bench)
        {
            layout = default;
            bench = null;
            var placed = _build.Get(benchId);
            if (placed == null || !_content.ItemDefs.TryGetValue(placed.ItemId, out var def) || def.Bench == null)
                return SlotRejection.NotABench;
            bench = def.Bench;
            if (size == null || !_content.ContainerDefs.TryGetValue(size, out var container)) return SlotRejection.NoSuchSlot;
            layout = SlotLayout.For(bench, container);
            return layout.Contains(col, row) ? SlotRejection.None : SlotRejection.NoSuchSlot;
        }

        private static long Key(int benchId, int u, int v) => ((long)benchId << 40) ^ ((long)u << 20) ^ (long)v;

        private static string Tag(int benchId, string size, int col, int row) => benchId + "|" + size + "|" + col + "|" + row;
    }
}
