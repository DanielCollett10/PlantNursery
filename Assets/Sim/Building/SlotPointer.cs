using System;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Building
{
    /// <summary>
    /// Maps a bench-local point to a slot address (docs/design/controls.md, Bench view: the mouse pointer). Pure: no Unity types.
    /// A held container (<c>heldKind</c> = container id) selects that size's grid; any other held item selects the working-strip
    /// grid of that item where the strip accepts it; <c>null</c> (empty-handed) means any size.
    /// </summary>
    public static class SlotPointer
    {
        /// <summary>Units per metre (a unit is 0.1 m).</summary>
        public const double UnitsPerMetre = 10.0;

        /// <summary>
        /// The slot under a point. <paramref name="localU"/> runs along the bench from its left end and <paramref name="localV"/>
        /// across from the front (working edge), both in metres. Returns a strip address in the working strip, a planting
        /// address in the planting area, or null if the point is off the bench, in a gap, or no slot of that kind exists there.
        /// </summary>
        /// <param name="content">Content tables.</param>
        /// <param name="bench">The placed bench.</param>
        /// <param name="localU">Metres along the bench.</param>
        /// <param name="localV">Metres across the bench from the front.</param>
        /// <param name="heldKind">Held container id or item id; null when empty-handed (any size).</param>
        /// <param name="planting">Optional planting occupancy: empty-handed, an occupied slot under the point wins.</param>
        /// <param name="strip">Optional strip occupancy: empty-handed, an occupied slot under the point wins.</param>
        public static SlotAddress? SlotAt(ContentDb content, PlacedItem bench, double localU, double localV, string heldKind,
            SlotOccupancy planting = null, StripOccupancy strip = null)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (bench == null) throw new ArgumentNullException(nameof(bench));
            if (!content.ItemDefs.TryGetValue(bench.ItemId, out var def) || def.Bench == null) return null;
            var b = def.Bench;
            var uu = (int)Math.Floor(localU * UnitsPerMetre);
            var vv = (int)Math.Floor(localV * UnitsPerMetre);
            if (uu < 0 || vv < 0 || uu >= BenchGeometry.Length(def) || vv >= BenchGeometry.Depth(def)) return null;

            if (b.StripDepthUnits > 0 && vv < b.StripDepthUnits)
                return StripAt(content, bench, b, uu, vv, heldKind, strip);

            var v0 = BenchGeometry.PlantingV0(def);
            if (vv < v0 || vv >= v0 + b.PlantingDepthUnits) return null;
            return PlantingAt(content, bench, b, uu, vv - v0, heldKind, planting);
        }

        private static SlotAddress? PlantingAt(ContentDb content, PlacedItem bench, BenchDef b, int u, int v, string heldKind,
            SlotOccupancy occupancy)
        {
            string size = null;
            if (heldKind != null && content.ContainerDefs.ContainsKey(heldKind)) size = heldKind;
            else if (occupancy != null)
                foreach (var s in occupancy.PlacedSlots)
                {
                    if (s.BenchId != bench.Id || !content.ContainerDefs.TryGetValue(s.Size, out var c)) continue;
                    var l = SlotLayout.For(b, c);
                    if (u >= l.StartU(s.Col) && u < l.StartU(s.Col) + l.AlongUnits &&
                        v >= l.StartV(s.Row) && v < l.StartV(s.Row) + l.AcrossUnits) { size = s.Size; break; }
                }
            if (size == null) size = SlotKinds.SmallestContainer(content);
            if (size == null) return null;
            var layout = SlotLayout.For(b, content.ContainerDefs[size]);
            if (layout.AlongUnits <= 0) return null;
            var col = u / layout.AlongUnits;
            var row = v / layout.AcrossUnits;
            return layout.Contains(col, row) ? SlotAddress.Planting(bench.Id, size, col, row) : (SlotAddress?)null;
        }

        private static SlotAddress? StripAt(ContentDb content, PlacedItem bench, BenchDef b, int u, int v, string heldKind,
            StripOccupancy occupancy)
        {
            string item = null;
            if (heldKind != null) item = SlotKinds.StripItemFor(content, heldKind);
            else if (occupancy != null)
                foreach (var s in occupancy.PlacedSlots)
                {
                    if (s.BenchId != bench.Id) continue;
                    var a = SlotKinds.Accept(b, s.Size);
                    if (a == null) continue;
                    var w = a.StripUnits.W; var d = a.StripUnits.D;
                    if (u >= s.Col * w && u < (s.Col + 1) * w && v >= s.Row * d && v < (s.Row + 1) * d) { item = s.Size; break; }
                }
            if (item == null && heldKind == null) item = SlotKinds.SmallestStripItem(b);
            var accept = SlotKinds.Accept(b, item);
            if (accept == null || accept.StripUnits.W <= 0 || accept.StripUnits.D <= 0) return null;
            var col = u / accept.StripUnits.W;
            var row = v / accept.StripUnits.D;
            if (col >= b.PlantingLengthUnits / accept.StripUnits.W || row >= b.StripDepthUnits / accept.StripUnits.D) return null;
            return SlotAddress.Strip(bench.Id, item, col, row);
        }
    }
}
