using System;

namespace PlantNursery.Sim.Building
{
    /// <summary>Address of a planting slot: <c>(benchId, containerSize, col, row)</c>.</summary>
    public readonly struct SlotRef : IEquatable<SlotRef>
    {
        /// <summary>Placed bench id.</summary>
        public readonly int BenchId;
        /// <summary>Container id that sets the slot size.</summary>
        public readonly string Size;
        /// <summary>Column along the bench.</summary>
        public readonly int Col;
        /// <summary>Row across the planting area.</summary>
        public readonly int Row;

        /// <summary>Creates a slot address.</summary>
        public SlotRef(int benchId, string size, int col, int row)
        {
            BenchId = benchId; Size = size; Col = col; Row = row;
        }

        public bool Equals(SlotRef o) => BenchId == o.BenchId && Size == o.Size && Col == o.Col && Row == o.Row;
        public override bool Equals(object obj) => obj is SlotRef s && Equals(s);
        public override int GetHashCode() => ((BenchId * 397 ^ (Size?.GetHashCode() ?? 0)) * 397 ^ Col) * 397 ^ Row;
        public override string ToString() => BenchId + "|" + Size + "|" + Col + "|" + Row;
    }
}
