using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Building
{
    /// <summary>A d-pad direction in the bench view's screen frame (screen up = the back of the view's bench).</summary>
    public enum StepDirection
    {
        /// <summary>Toward the back of the view's bench.</summary>
        Up = 0,
        /// <summary>Screen right.</summary>
        Right = 1,
        /// <summary>Toward the front of the view's bench.</summary>
        Down = 2,
        /// <summary>Screen left.</summary>
        Left = 3
    }

    /// <summary>
    /// Gamepad slot stepping (docs/design/controls.md, Bench view: stepping order). Left/right moves along a row, up/down across
    /// rows; down from the planting area's front row enters the working strip and up from the strip returns; up from the back
    /// row continues into the bench backed against this one. Slots are those of the held kind: a held container's size, a held
    /// item's strip slots, or (empty-handed) the size of the slot you are on. Pure: no Unity types.
    /// </summary>
    public sealed class SlotStepper
    {
        private readonly ContentDb _content;
        private readonly BuildState _build;
        private readonly string _heldKind;
        private readonly Rotation _viewRotation;

        /// <summary>Creates a stepper.</summary>
        /// <param name="content">Content tables.</param>
        /// <param name="build">Placed benches.</param>
        /// <param name="heldKind">Held container id or item id; null when empty-handed (any size).</param>
        /// <param name="viewRotation">Rotation of the bench the view is oriented to: its back is screen-up.
        /// A back-to-back bench (rotated 180 relative to it) is therefore stepped with up/down and left/right mirrored.</param>
        public SlotStepper(ContentDb content, BuildState build, string heldKind, Rotation viewRotation)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _heldKind = heldKind;
            _viewRotation = viewRotation;
        }

        /// <summary>The slot one d-pad step from <paramref name="from"/>, or null if the edge is reached (stay put).</summary>
        public SlotAddress? Step(SlotAddress from, StepDirection direction)
        {
            var placed = _build.Get(from.Slot.BenchId);
            if (placed == null || !_content.ItemDefs.TryGetValue(placed.ItemId, out var def) || def.Bench == null) return null;
            var k = (((int)placed.Rotation - (int)_viewRotation) / 90 % 4 + 4) % 4;
            var local = ((int)direction - k + 4) % 4; // 0 back, 1 +u, 2 front, 3 -u
            return from.IsStrip ? StepStrip(from.Slot, placed, def, local) : StepPlanting(from.Slot, placed, def, local);
        }

        /// <summary>
        /// The slot nearest the player, among the given benches. <paramref name="playerX"/>/<paramref name="playerZ"/> are world
        /// metres (1 cell = 0.5 m). Candidates are the held kind's planting slots and strip slots; empty-handed, the smallest
        /// container and smallest strip item. Ties go to the earlier bench in <paramref name="benchIds"/>, strip before planting.
        /// </summary>
        public SlotAddress? Start(IEnumerable<int> benchIds, double playerX, double playerZ)
        {
            if (benchIds == null) throw new ArgumentNullException(nameof(benchIds));
            SlotAddress? best = null;
            var bestD = double.MaxValue;
            var px = playerX * SlotPointer.UnitsPerMetre;
            var pz = playerZ * SlotPointer.UnitsPerMetre;
            foreach (var id in benchIds)
            {
                var placed = _build.Get(id);
                if (placed == null || !_content.ItemDefs.TryGetValue(placed.ItemId, out var def) || def.Bench == null) continue;
                var b = def.Bench;
                var len = BenchGeometry.Length(def); var dep = BenchGeometry.Depth(def);
                var item = StripItem(null) ?? SlotKinds.SmallestStripItem(b);
                var a = SlotKinds.Accept(b, item);
                if (a != null && a.StripUnits.W > 0 && a.StripUnits.D > 0)
                    for (var c = 0; c < b.PlantingLengthUnits / a.StripUnits.W; c++)
                        for (var r = 0; r < b.StripDepthUnits / a.StripUnits.D; r++)
                            Consider(placed, len, dep, (c + 0.5) * a.StripUnits.W, (r + 0.5) * a.StripUnits.D,
                                SlotAddress.Strip(id, item, c, r), px, pz, ref best, ref bestD);
                var size = PlantingSize(null);
                if (size != null)
                {
                    var l = SlotLayout.For(b, _content.ContainerDefs[size]);
                    var v0 = BenchGeometry.PlantingV0(def);
                    for (var c = 0; c < l.Cols; c++)
                        for (var r = 0; r < l.Rows; r++)
                            Consider(placed, len, dep, l.StartU(c) + l.AlongUnits / 2.0, v0 + l.StartV(r) + l.AcrossUnits / 2.0,
                                SlotAddress.Planting(id, size, c, r), px, pz, ref best, ref bestD);
                }
            }
            return best;
        }

        private static void Consider(PlacedItem placed, int len, int dep, double u, double v, SlotAddress addr,
            double px, double pz, ref SlotAddress? best, ref double bestD)
        {
            BenchGeometry.ToWorldF(placed, len, dep, u, v, out var x, out var z);
            var d = (x - px) * (x - px) + (z - pz) * (z - pz);
            if (d < bestD) { bestD = d; best = addr; }
        }

        private SlotAddress? StepPlanting(SlotRef s, PlacedItem placed, ItemDef def, int local)
        {
            var b = def.Bench;
            if (!_content.ContainerDefs.TryGetValue(s.Size, out var cdef)) return null;
            var l = SlotLayout.For(b, cdef);
            if (!l.Contains(s.Col, s.Row)) return null;
            switch (local)
            {
                case 1: return l.Contains(s.Col + 1, s.Row) ? SlotAddress.Planting(s.BenchId, s.Size, s.Col + 1, s.Row) : (SlotAddress?)null;
                case 3: return l.Contains(s.Col - 1, s.Row) ? SlotAddress.Planting(s.BenchId, s.Size, s.Col - 1, s.Row) : (SlotAddress?)null;
                case 0:
                    return l.Contains(s.Col, s.Row + 1)
                        ? SlotAddress.Planting(s.BenchId, s.Size, s.Col, s.Row + 1)
                        : AcrossJoin(s, placed, def, l);
                default:
                    if (s.Row > 0) return SlotAddress.Planting(s.BenchId, s.Size, s.Col, s.Row - 1);
                    return IntoStrip(s, b, l.StartU(s.Col) + l.AlongUnits / 2.0);
            }
        }

        private SlotAddress? IntoStrip(SlotRef s, BenchDef b, double centreU)
        {
            var item = StripItem(s.Size);
            var a = SlotKinds.Accept(b, item);
            if (a == null && _heldKind == null) { item = SlotKinds.SmallestStripItem(b); a = SlotKinds.Accept(b, item); }
            if (a == null || a.StripUnits.W <= 0 || a.StripUnits.D <= 0) return null;
            var cols = b.PlantingLengthUnits / a.StripUnits.W;
            var rows = b.StripDepthUnits / a.StripUnits.D;
            if (cols <= 0 || rows <= 0) return null;
            var col = Math.Min(cols - 1, (int)(centreU / a.StripUnits.W));
            return SlotAddress.Strip(s.BenchId, item, col, rows - 1);
        }

        private SlotAddress? StepStrip(SlotRef s, PlacedItem placed, ItemDef def, int local)
        {
            var b = def.Bench;
            var a = SlotKinds.Accept(b, s.Size);
            if (a == null || a.StripUnits.W <= 0 || a.StripUnits.D <= 0) return null;
            var cols = b.PlantingLengthUnits / a.StripUnits.W;
            var rows = b.StripDepthUnits / a.StripUnits.D;
            if (s.Col < 0 || s.Row < 0 || s.Col >= cols || s.Row >= rows) return null;
            switch (local)
            {
                case 1: return s.Col + 1 < cols ? SlotAddress.Strip(s.BenchId, s.Size, s.Col + 1, s.Row) : (SlotAddress?)null;
                case 3: return s.Col > 0 ? SlotAddress.Strip(s.BenchId, s.Size, s.Col - 1, s.Row) : (SlotAddress?)null;
                case 2: return s.Row > 0 ? SlotAddress.Strip(s.BenchId, s.Size, s.Col, s.Row - 1) : (SlotAddress?)null;
                default:
                    if (s.Row + 1 < rows) return SlotAddress.Strip(s.BenchId, s.Size, s.Col, s.Row + 1);
                    var size = PlantingSize(s.Size);
                    if (size == null) return null;
                    var l = SlotLayout.For(b, _content.ContainerDefs[size]);
                    if (l.Cols <= 0 || l.Rows <= 0) return null;
                    var col = Math.Min(l.Cols - 1, (int)((s.Col + 0.5) * a.StripUnits.W / l.AlongUnits));
                    return SlotAddress.Planting(s.BenchId, size, col, 0);
            }
        }

        /// <summary>Back row of a bench: the planting slot of the same size across a back-to-back join, if there is one.</summary>
        private SlotAddress? AcrossJoin(SlotRef s, PlacedItem placed, ItemDef def, SlotLayout l)
        {
            var len = BenchGeometry.Length(def); var dep = BenchGeometry.Depth(def);
            var v0 = BenchGeometry.PlantingV0(def);
            var vBack = v0 + l.StartV(s.Row) + l.AcrossUnits - 1; // last unit row of this slot
            var uStart = l.StartU(s.Col); var uEnd = uStart + l.AlongUnits;
            var centre = (uStart + uEnd - 1) / 2.0;
            SlotAddress? best = null; var bestDist = double.MaxValue;
            for (var u = uStart; u < uEnd; u++)
            {
                BenchGeometry.ToWorld(placed, len, dep, u, vBack + 1, out var wx, out var wz);
                var hit = SlotAtWorldUnit(placed.Id, wx, wz, s.Size);
                if (hit == null) continue;
                var dist = Math.Abs(u - centre);
                if (dist < bestDist) { bestDist = dist; best = hit; }
            }
            return best;
        }

        private SlotAddress? SlotAtWorldUnit(int excludeBench, int wx, int wz, string size)
        {
            foreach (var other in _build.PlacedItems)
            {
                if (other.Id == excludeBench || !_content.ItemDefs.TryGetValue(other.ItemId, out var od) || od.Bench == null) continue;
                if (!BenchGeometry.ToLocal(other, BenchGeometry.Length(od), BenchGeometry.Depth(od), wx, wz, out var u, out var v)) continue;
                var pv = v - BenchGeometry.PlantingV0(od);
                if (pv < 0 || pv >= od.Bench.PlantingDepthUnits) continue;
                var l = SlotLayout.For(od.Bench, _content.ContainerDefs[size]);
                if (l.AlongUnits <= 0) continue;
                var col = u / l.AlongUnits; var row = pv / l.AcrossUnits;
                if (l.Contains(col, row)) return SlotAddress.Planting(other.Id, size, col, row);
            }
            return null;
        }

        private bool HeldIsContainer => _heldKind != null && _content.ContainerDefs.ContainsKey(_heldKind);

        /// <summary>Planting size to step in; <paramref name="current"/> is the size or strip item of the slot we are on.</summary>
        private string PlantingSize(string current)
        {
            if (HeldIsContainer) return _heldKind;
            if (current != null)
            {
                if (_content.ContainerDefs.ContainsKey(current)) return current;
                foreach (var c in _content.ContainerDefs.Values) if (c.ItemId == current) return c.Id;
            }
            return SlotKinds.SmallestContainer(_content);
        }

        /// <summary>Strip item to step in; null result means this bench's strip does not take the held thing.</summary>
        private string StripItem(string current)
        {
            if (_heldKind != null) return SlotKinds.StripItemFor(_content, _heldKind);
            return current != null ? SlotKinds.StripItemFor(_content, current) : null;
        }
    }
}
