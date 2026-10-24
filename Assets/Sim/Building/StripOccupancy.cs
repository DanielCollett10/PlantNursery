using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>Why a working-strip placement or removal is refused.</summary>
    public enum StripRejection
    {
        /// <summary>Accepted.</summary>
        None = 0,
        /// <summary>The placed item does not exist or is not a bench.</summary>
        NotABench,
        /// <summary>The bench's strip does not list this item.</summary>
        NotAccepted,
        /// <summary>The item holds a plant (and is not a carry tray).</summary>
        NoPlantsOnStrip,
        /// <summary>No strip, or the slot is outside it.</summary>
        NoSuchSlot,
        /// <summary>Some unit of the slot is already used.</summary>
        SlotOccupied,
        /// <summary>Removal only: that exact slot holds nothing.</summary>
        SlotEmpty
    }

    /// <summary>What a thing offered to the strip holds.</summary>
    public enum StripContents
    {
        /// <summary>Nothing planted: tool, empty container, bundle.</summary>
        NoPlants = 0,
        /// <summary>A planted pot or plug tray, or a caught plant.</summary>
        HoldsPlant,
        /// <summary>A carry tray with pots in it: allowed on the strip.</summary>
        CarryTrayWithPots
    }

    /// <summary>
    /// Which working-strip units of each bench are used (docs/design/building.md, Working strip).
    /// Strip-local units: <c>u</c> along the bench, <c>v</c> across the strip from its left-front corner.
    /// Slots are a fixed grid per item footprint (<c>stripUnits</c>); a slot is free if none of its units is used.
    /// </summary>
    public sealed class StripOccupancy
    {
        /// <summary>Item id of the carry tray, the one item allowed on the strip with plants in it.</summary>
        public const string CarryTrayItemId = "carry_tray";

        private readonly ContentDb _content;
        private readonly BuildState _build;
        private readonly Dictionary<long, string> _units = new Dictionary<long, string>();
        private readonly HashSet<string> _slots = new HashSet<string>();

        /// <summary>Creates strip occupancy over a build state.</summary>
        public StripOccupancy(ContentDb content, BuildState build)
        {
            _content = content;
            _build = build;
            _build?.AddPickUpBlocker(p => HasAny(p.Id));
        }

        /// <summary>True if anything sits on the bench's working strip.</summary>
        public bool HasAny(int benchId)
        {
            var prefix = benchId + "|";
            foreach (var t in _slots) if (t.StartsWith(prefix, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Number of slot columns for an item on a bench's strip; 0 if there is no strip or it is not accepted.</summary>
        public int Cols(BenchDef bench, string itemId) => TryLayout(bench, itemId, out var c, out _, out _, out _) ? c : 0;

        /// <summary>Number of slot rows for an item on a bench's strip; 0 if there is no strip or it is not accepted.</summary>
        public int Rows(BenchDef bench, string itemId) => TryLayout(bench, itemId, out _, out var r, out _, out _) ? r : 0;

        /// <summary>Query-only: would placing this item into the slot be accepted? Never changes state.</summary>
        public StripRejection CanPlace(int benchId, string itemId, int col, int row, StripContents contents)
        {
            var r = Resolve(benchId, itemId, col, row, out var along, out var across);
            if (r != StripRejection.None) return r;
            if (contents == StripContents.HoldsPlant) return StripRejection.NoPlantsOnStrip;
            if (contents == StripContents.CarryTrayWithPots && itemId != CarryTrayItemId) return StripRejection.NoPlantsOnStrip;
            for (var u = col * along; u < (col + 1) * along; u++)
                for (var v = row * across; v < (row + 1) * across; v++)
                    if (_units.ContainsKey(Key(benchId, u, v))) return StripRejection.SlotOccupied;
            return StripRejection.None;
        }

        /// <summary>Mark a strip slot's units used. Returns the rejection, or None if placed.</summary>
        public StripRejection Place(int benchId, string itemId, int col, int row, StripContents contents)
        {
            var r = CanPlace(benchId, itemId, col, row, contents);
            if (r != StripRejection.None) return r;
            Resolve(benchId, itemId, col, row, out var along, out var across);
            var tag = Tag(benchId, itemId, col, row);
            for (var u = col * along; u < (col + 1) * along; u++)
                for (var v = row * across; v < (row + 1) * across; v++)
                    _units[Key(benchId, u, v)] = tag;
            _slots.Add(tag);
            return StripRejection.None;
        }

        /// <summary>Query-only: would removing from this exact slot be accepted?</summary>
        public StripRejection CanRemove(int benchId, string itemId, int col, int row)
        {
            var r = Resolve(benchId, itemId, col, row, out _, out _);
            if (r != StripRejection.None) return r;
            return _slots.Contains(Tag(benchId, itemId, col, row)) ? StripRejection.None : StripRejection.SlotEmpty;
        }

        /// <summary>Free the units of a slot that was placed with exactly this address.</summary>
        public StripRejection Remove(int benchId, string itemId, int col, int row)
        {
            var r = CanRemove(benchId, itemId, col, row);
            if (r != StripRejection.None) return r;
            Resolve(benchId, itemId, col, row, out var along, out var across);
            for (var u = col * along; u < (col + 1) * along; u++)
                for (var v = row * across; v < (row + 1) * across; v++)
                    _units.Remove(Key(benchId, u, v));
            _slots.Remove(Tag(benchId, itemId, col, row));
            return StripRejection.None;
        }

        private StripRejection Resolve(int benchId, string itemId, int col, int row, out int along, out int across)
        {
            along = across = 0;
            var placed = _build.Get(benchId);
            if (placed == null || !_content.ItemDefs.TryGetValue(placed.ItemId, out var def) || def.Bench == null)
                return StripRejection.NotABench;
            var bench = def.Bench;
            if (itemId == null) return StripRejection.NotAccepted;
            if (bench.StripDepthUnits <= 0) return StripRejection.NoSuchSlot;
            if (!TryLayout(bench, itemId, out var cols, out var rows, out along, out across))
                return StripRejection.NotAccepted;
            return col >= 0 && row >= 0 && col < cols && row < rows ? StripRejection.None : StripRejection.NoSuchSlot;
        }

        private static bool TryLayout(BenchDef bench, string itemId, out int cols, out int rows, out int along, out int across)
        {
            cols = rows = along = across = 0;
            if (bench == null || bench.StripDepthUnits <= 0 || bench.StripAccepts == null) return false;
            foreach (var a in bench.StripAccepts)
            {
                if (a.ItemId != itemId) continue;
                if (a.StripUnits.W <= 0 || a.StripUnits.D <= 0) return false;
                along = a.StripUnits.W;
                across = a.StripUnits.D;
                cols = bench.PlantingLengthUnits / along;
                rows = bench.StripDepthUnits / across;
                return true;
            }
            return false;
        }

        private static long Key(int benchId, int u, int v) => ((long)benchId << 40) ^ ((long)u << 20) ^ (long)v;

        private static string Tag(int benchId, string itemId, int col, int row) => benchId + "|" + itemId + "|" + col + "|" + row;
    }
}
