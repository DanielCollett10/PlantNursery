using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>Integer surface mini-grid coordinate (u, v) = floor cell * 5 + offset (0-4), across the whole plot.</summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        /// <summary>East-west unit.</summary>
        public readonly int U;
        /// <summary>North-south unit.</summary>
        public readonly int V;
        /// <summary>Creates a unit coordinate.</summary>
        public Unit(int u, int v) { U = u; V = v; }
        public bool Equals(Unit other) => U == other.U && V == other.V;
        public override bool Equals(object obj) => obj is Unit o && Equals(o);
        public override int GetHashCode() => U * 397 ^ V;
        public override string ToString() => "(" + U + "," + V + ")";
    }

    /// <summary>A surface item resting on the mini-grid.</summary>
    public sealed class SurfaceItem
    {
        /// <summary>Unique id of this surface placement.</summary>
        public int Id { get; }
        /// <summary>Item definition id.</summary>
        public string ItemId { get; }
        /// <summary>South-west anchor unit.</summary>
        public Unit Anchor { get; }
        /// <summary>Rotation.</summary>
        public Rotation Rotation { get; }
        /// <summary>Rotated footprint width in units.</summary>
        public int Width { get; }
        /// <summary>Rotated footprint depth in units.</summary>
        public int Depth { get; }
        /// <summary>Seeds carried (seed packets); 0 for other items.</summary>
        public int SeedCount { get; }

        public SurfaceItem(int id, string itemId, Unit anchor, Rotation rotation, int width, int depth, int seedCount = 0)
        {
            SeedCount = seedCount; Id = id; ItemId = itemId; Anchor = anchor; Rotation = rotation; Width = width; Depth = depth;
        }
    }

    /// <summary>
    /// The 0.1 m mini-grid over bench tops. A unit is usable only above a floor item with providesSurface;
    /// adjacent benches form one continuous surface. One surface item per unit.
    /// Also the <see cref="ISurfaceOccupancy"/> for its <see cref="BuildState"/>.
    /// </summary>
    public sealed class SurfaceGrid : ISurfaceOccupancy
    {
        /// <summary>Units per floor cell edge.</summary>
        public const int UnitsPerCell = 5;

        private readonly ContentDb _content;
        private readonly BuildState _build;
        private readonly Dictionary<Unit, int> _units = new Dictionary<Unit, int>();
        private readonly Dictionary<int, SurfaceItem> _items = new Dictionary<int, SurfaceItem>();
        private int _nextId = 1;

        /// <summary>Creates the grid and registers it as the build state's surface occupancy.</summary>
        public SurfaceGrid(ContentDb content, BuildState build)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _build.SurfaceOccupancy = this;
        }

        /// <summary>All surface items.</summary>
        public IEnumerable<SurfaceItem> Items => _items.Values;

        /// <summary>The surface item covering a unit, or null.</summary>
        public SurfaceItem GetAt(Unit unit) => _units.TryGetValue(unit, out var id) ? _items[id] : null;

        /// <summary>The floor cell under a unit.</summary>
        public static Cell CellOf(Unit unit) => new Cell(FloorDiv(unit.U), FloorDiv(unit.V));

        private static int FloorDiv(int a) => a >= 0 ? a / UnitsPerCell : -((-a + UnitsPerCell - 1) / UnitsPerCell);

        /// <summary>True if the unit lies over a floor item that provides a surface.</summary>
        public bool IsUsable(Unit unit)
        {
            var placed = _build.GetAt(CellOf(unit));
            return placed != null && _content.ItemDefs[placed.ItemId].ProvidesSurface;
        }

        /// <summary>Query-only: would this surface placement be accepted? Checks Occupied, then NoSurface.</summary>
        public BuildRejection CanPlace(string itemId, Unit anchor, Rotation rotation)
        {
            if (itemId == null || !_content.ItemDefs.TryGetValue(itemId, out var def)) return BuildRejection.UnknownItem;
            if (def.Layer != ItemLayer.Surface) return BuildRejection.WrongLayer;
            var size = Footprints.Rotate(def.SurfaceFootprint, rotation);
            for (var du = 0; du < size.W; du++)
                for (var dv = 0; dv < size.D; dv++)
                    if (_units.ContainsKey(new Unit(anchor.U + du, anchor.V + dv))) return BuildRejection.Occupied;
            for (var du = 0; du < size.W; du++)
                for (var dv = 0; dv < size.D; dv++)
                    if (!IsUsable(new Unit(anchor.U + du, anchor.V + dv))) return BuildRejection.NoSurface;
            return BuildRejection.None;
        }

        /// <summary>Place a surface item. Returns it, or null with the reason.</summary>
        public SurfaceItem Place(string itemId, Unit anchor, Rotation rotation, out BuildRejection rejection, int seedCount = 0)
        {
            rejection = CanPlace(itemId, anchor, rotation);
            if (rejection != BuildRejection.None) return null;
            var size = Footprints.Rotate(_content.ItemDefs[itemId].SurfaceFootprint, rotation);
            var item = new SurfaceItem(_nextId++, itemId, anchor, rotation, size.W, size.D, seedCount);
            _items[item.Id] = item;
            for (var du = 0; du < item.Width; du++)
                for (var dv = 0; dv < item.Depth; dv++)
                    _units[new Unit(anchor.U + du, anchor.V + dv)] = item.Id;
            return item;
        }

        /// <summary>Remove a surface item. Returns false if the id is unknown.</summary>
        public bool Remove(int surfaceItemId)
        {
            if (!_items.TryGetValue(surfaceItemId, out var item)) return false;
            _items.Remove(surfaceItemId);
            for (var du = 0; du < item.Width; du++)
                for (var dv = 0; dv < item.Depth; dv++)
                    _units.Remove(new Unit(item.Anchor.U + du, item.Anchor.V + dv));
            return true;
        }

        /// <summary>
        /// Closest valid anchor to <paramref name="desired"/> within <paramref name="radius"/> units
        /// (per axis), or null. Closeness is squared Euclidean distance; ties go to smallest v, then smallest u.
        /// </summary>
        public Unit? FindNearestFreeSpot(string itemId, Rotation rotation, Unit desired, int radius)
            => FindNearestFreeSpot(itemId, rotation, desired, radius, null);

        /// <summary>As above, but only anchors for which <paramref name="accept"/> returns true are considered (null = all).</summary>
        public Unit? FindNearestFreeSpot(string itemId, Rotation rotation, Unit desired, int radius, Func<Unit, bool> accept)
        {
            Unit? best = null;
            var bestDist = int.MaxValue;
            for (var dv = -radius; dv <= radius; dv++)
                for (var du = -radius; du <= radius; du++)
                {
                    var dist = du * du + dv * dv;
                    var cand = new Unit(desired.U + du, desired.V + dv);
                    if (best.HasValue)
                    {
                        if (dist > bestDist) continue;
                        if (dist == bestDist && !Before(cand, best.Value)) continue;
                    }
                    if (accept != null && !accept(cand)) continue;
                    if (CanPlace(itemId, cand, rotation) != BuildRejection.None) continue;
                    best = cand;
                    bestDist = dist;
                }
            return best;
        }

        private static bool Before(Unit a, Unit b) => a.V < b.V || (a.V == b.V && a.U < b.U);

        /// <summary>True if any surface unit over the placed item's cells is occupied.</summary>
        public bool HasItemsOn(PlacedItem item)
        {
            var u0 = item.Anchor.X * UnitsPerCell;
            var v0 = item.Anchor.Z * UnitsPerCell;
            for (var u = u0; u < u0 + item.Width * UnitsPerCell; u++)
                for (var v = v0; v < v0 + item.Depth * UnitsPerCell; v++)
                    if (_units.ContainsKey(new Unit(u, v))) return true;
            return false;
        }
    }
}
