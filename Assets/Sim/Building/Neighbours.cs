using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>
    /// Adjacency graph of occupied planting slots (docs/design/building.md, Neighbours). Every planting unit is mapped into
    /// one nursery-wide unit space (cell * 5), so two slots are neighbours when they share an edge of at least one unit,
    /// within a bench or across a join. Diagonals never count. Working-strip items are never in the graph, because the graph
    /// is built only from <see cref="SlotOccupancy"/> (planting slots). Cached; rebuilt lazily when the
    /// <see cref="BuildState.Version"/> or <see cref="SlotOccupancy.Version"/> has changed (place, move, remove).
    /// </summary>
    public sealed class Neighbours
    {
        /// <summary>Units per floor cell.</summary>
        public const int UnitsPerCell = 5;

        private static readonly IReadOnlyList<SlotRef> None = new SlotRef[0];

        private readonly ContentDb _content;
        private readonly BuildState _build;
        private readonly SlotOccupancy _slots;
        private readonly Dictionary<SlotRef, List<SlotRef>> _graph = new Dictionary<SlotRef, List<SlotRef>>();
        private int _buildVersion = -1;
        private int _slotVersion = -1;

        /// <summary>How many times the graph has been rebuilt.</summary>
        public int RebuildCount { get; private set; }

        /// <summary>Creates the graph over a build state and its slot occupancy.</summary>
        public Neighbours(ContentDb content, BuildState build, SlotOccupancy slots)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
        }

        /// <summary>Occupied slots that share an edge with <paramref name="slot"/>. Empty if it is not occupied.</summary>
        public IReadOnlyList<SlotRef> Of(SlotRef slot)
        {
            Refresh();
            return _graph.TryGetValue(slot, out var list) ? list : None;
        }

        private void Refresh()
        {
            if (_buildVersion == _build.Version && _slotVersion == _slots.Version) return;
            _buildVersion = _build.Version;
            _slotVersion = _slots.Version;
            RebuildCount++;
            Rebuild();
        }

        private void Rebuild()
        {
            _graph.Clear();
            var owner = new Dictionary<long, SlotRef>();
            var all = new List<SlotRef>();
            foreach (var slot in _slots.PlacedSlots)
            {
                var placed = _build.Get(slot.BenchId);
                if (placed == null || !_content.ItemDefs.TryGetValue(placed.ItemId, out var def) || def.Bench == null) continue;
                if (!_content.ContainerDefs.TryGetValue(slot.Size, out var container)) continue;
                all.Add(slot);
                _graph[slot] = new List<SlotRef>();
                var layout = SlotLayout.For(def.Bench, container);
                var bench = def.Bench;
                var length = def.FloorFootprint.W * UnitsPerCell;
                var depth = def.FloorFootprint.D * UnitsPerCell;
                var v0 = depth - bench.PlantingBackOffsetUnits - bench.PlantingDepthUnits;
                for (var u = layout.StartU(slot.Col); u < layout.StartU(slot.Col) + layout.AlongUnits; u++)
                    for (var v = layout.StartV(slot.Row); v < layout.StartV(slot.Row) + layout.AcrossUnits; v++)
                    {
                        ToWorld(placed, length, depth, u, v0 + v, out var x, out var z);
                        owner[Key(x, z)] = slot;
                    }
            }
            foreach (var kv in owner)
            {
                var x = (int)(kv.Key >> 32);
                var z = (int)(kv.Key & 0xffffffffL);
                Link(kv.Value, owner, x + 1, z);
                Link(kv.Value, owner, x, z + 1);
            }
        }

        private void Link(SlotRef a, Dictionary<long, SlotRef> owner, int x, int z)
        {
            if (!owner.TryGetValue(Key(x, z), out var b) || a.Equals(b)) return;
            var la = _graph[a];
            if (!la.Contains(b)) { la.Add(b); _graph[b].Add(a); }
        }

        /// <summary>
        /// Bench-local unit (u along from the left, v across from the front, as seen from the working edge) to a world unit.
        /// Rotation turns the bench clockwise seen from above; at 0 the front faces south and u runs east.
        /// </summary>
        private static void ToWorld(PlacedItem p, int length, int depth, int u, int v, out int x, out int z) =>
            BenchGeometry.ToWorld(p, length, depth, u, v, out x, out z);

        private static long Key(int x, int z) => ((long)x << 32) | (uint)z;
    }
}
