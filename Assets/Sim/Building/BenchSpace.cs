using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Building
{
    /// <summary>A slot's footprint in bench-local metres (<c>u</c> along from the left, <c>v</c> across from the front).</summary>
    public readonly struct SlotRect
    {
        /// <summary>Left edge, metres along the bench.</summary>
        public readonly double UMin;
        /// <summary>Front edge, metres across the bench.</summary>
        public readonly double VMin;
        /// <summary>Right edge, metres along the bench.</summary>
        public readonly double UMax;
        /// <summary>Back edge, metres across the bench.</summary>
        public readonly double VMax;

        /// <summary>Creates a rect.</summary>
        public SlotRect(double uMin, double vMin, double uMax, double vMax) { UMin = uMin; VMin = vMin; UMax = uMax; VMax = vMax; }

        /// <summary>Centre along the bench.</summary>
        public double CentreU => (UMin + UMax) / 2.0;
        /// <summary>Centre across the bench.</summary>
        public double CentreV => (VMin + VMax) / 2.0;
    }

    /// <summary>
    /// Public bench geometry for the Unity layer (bench view): bench-local metres to world metres and back, slot rectangles,
    /// and which benches share one bench view. World metres have 1 cell = 0.5 m with the plot origin at (0,0). Pure: no Unity types.
    /// </summary>
    public static class BenchSpace
    {
        private const double M = 1.0 / SlotPointer.UnitsPerMetre;

        /// <summary>Bench length and depth in metres. False if the item is not a bench.</summary>
        public static bool TrySize(ContentDb content, PlacedItem bench, out double lengthM, out double depthM)
        {
            lengthM = depthM = 0;
            if (!content.ItemDefs.TryGetValue(bench.ItemId, out var def) || def.Bench == null) return false;
            lengthM = BenchGeometry.Length(def) * M; depthM = BenchGeometry.Depth(def) * M;
            return true;
        }

        /// <summary>World metres of a bench-local point (metres).</summary>
        public static bool TryLocalToWorld(ContentDb content, PlacedItem bench, double uM, double vM, out double xM, out double zM)
        {
            xM = zM = 0;
            if (!content.ItemDefs.TryGetValue(bench.ItemId, out var def) || def.Bench == null) return false;
            BenchGeometry.ToWorldF(bench, BenchGeometry.Length(def), BenchGeometry.Depth(def),
                uM * SlotPointer.UnitsPerMetre, vM * SlotPointer.UnitsPerMetre, out var x, out var z);
            xM = x * M; zM = z * M;
            return true;
        }

        /// <summary>Bench-local metres of a world point (metres). The point may lie outside the bench.</summary>
        public static bool TryWorldToLocal(ContentDb content, PlacedItem bench, double xM, double zM, out double uM, out double vM)
        {
            uM = vM = 0;
            if (!content.ItemDefs.TryGetValue(bench.ItemId, out var def) || def.Bench == null) return false;
            double len = BenchGeometry.Length(def), dep = BenchGeometry.Depth(def);
            var lx = xM * SlotPointer.UnitsPerMetre - bench.Anchor.X * Neighbours.UnitsPerCell;
            var lz = zM * SlotPointer.UnitsPerMetre - bench.Anchor.Z * Neighbours.UnitsPerCell;
            double u, v;
            switch (bench.Rotation)
            {
                case Rotation.Deg90: u = len - lz; v = lx; break;
                case Rotation.Deg180: u = len - lx; v = dep - lz; break;
                case Rotation.Deg270: u = lz; v = dep - lx; break;
                default: u = lx; v = lz; break;
            }
            uM = u * M; vM = v * M;
            return true;
        }

        /// <summary>The footprint of a slot in bench-local metres, or null if the address does not exist on a placed bench.</summary>
        public static SlotRect? SlotRectOf(ContentDb content, BuildState build, SlotAddress address)
        {
            var placed = build.Get(address.Slot.BenchId);
            if (placed == null || !content.ItemDefs.TryGetValue(placed.ItemId, out var def) || def.Bench == null) return null;
            var s = address.Slot;
            if (address.IsStrip)
            {
                var a = SlotKinds.Accept(def.Bench, s.Size);
                if (a == null) return null;
                return new SlotRect(s.Col * a.StripUnits.W * M, s.Row * a.StripUnits.D * M,
                    (s.Col + 1) * a.StripUnits.W * M, (s.Row + 1) * a.StripUnits.D * M);
            }
            if (!content.ContainerDefs.TryGetValue(s.Size, out var c)) return null;
            var l = SlotLayout.For(def.Bench, c);
            if (!l.Contains(s.Col, s.Row)) return null;
            var v0 = BenchGeometry.PlantingV0(def);
            return new SlotRect(l.StartU(s.Col) * M, (v0 + l.StartV(s.Row)) * M,
                (l.StartU(s.Col) + l.AlongUnits) * M, (v0 + l.StartV(s.Row) + l.AcrossUnits) * M);
        }

        /// <summary>
        /// Bench ids that share one bench view with <paramref name="benchId"/> (itself first): benches backed against it,
        /// back to back (rotated 180 degrees to each other with the back rows touching), followed transitively.
        /// </summary>
        public static IReadOnlyList<int> ViewGroup(ContentDb content, BuildState build, int benchId)
        {
            var result = new List<int>();
            if (build.Get(benchId) == null) return result;
            result.Add(benchId);
            for (var i = 0; i < result.Count; i++)
            {
                var a = build.Get(result[i]);
                foreach (var other in build.PlacedItems)
                {
                    if (result.Contains(other.Id) || !BackToBack(content, a, other)) continue;
                    result.Add(other.Id);
                }
            }
            return result;
        }

        private static bool BackToBack(ContentDb content, PlacedItem a, PlacedItem b)
        {
            if (a == null || b == null || a.Id == b.Id) return false;
            if (!content.ItemDefs.TryGetValue(a.ItemId, out var ad) || ad.Bench == null) return false;
            if (!content.ItemDefs.TryGetValue(b.ItemId, out var bd) || bd.Bench == null) return false;
            if ((((int)b.Rotation - (int)a.Rotation) / 90 % 4 + 4) % 4 != 2) return false;
            int len = BenchGeometry.Length(ad), dep = BenchGeometry.Depth(ad);
            int blen = BenchGeometry.Length(bd), bdep = BenchGeometry.Depth(bd);
            for (var u = 0; u < len; u++)
            {
                BenchGeometry.ToWorld(a, len, dep, u, dep, out var wx, out var wz); // just behind a's back row
                if (BenchGeometry.ToLocal(b, blen, bdep, wx, wz, out _, out var bv) && bv == bdep - 1) return true;
            }
            return false;
        }

        /// <summary>
        /// Distance in metres from a world point to the nearest edge of the given benches' footprints (0 inside). Rotation-aware.
        /// </summary>
        public static double DistanceToBenches(ContentDb content, BuildState build, IEnumerable<int> benchIds, double xM, double zM)
        {
            var best = double.MaxValue;
            foreach (var id in benchIds)
            {
                var p = build.Get(id);
                if (p == null) continue;
                var minX = p.Anchor.X * 0.5; var minZ = p.Anchor.Z * 0.5;
                var dx = Math.Max(Math.Max(minX - xM, 0), xM - (minX + p.Width * 0.5));
                var dz = Math.Max(Math.Max(minZ - zM, 0), zM - (minZ + p.Depth * 0.5));
                best = Math.Min(best, Math.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }
    }
}
