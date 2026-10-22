using System;
using System.Collections.Generic;

namespace PlantNursery.Sim.Building
{
    /// <summary>Counts of unplaced build items, shared by all players of one nursery.</summary>
    public sealed class BuildInventory
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        /// <summary>Creates an inventory seeded from the starter inventory (may be null).</summary>
        public BuildInventory(IReadOnlyDictionary<string, int> starter = null)
        {
            if (starter == null) return;
            foreach (var kv in starter) if (kv.Value > 0) _counts[kv.Key] = kv.Value;
        }

        /// <summary>How many of the item are held.</summary>
        public int Count(string itemId) => itemId != null && _counts.TryGetValue(itemId, out var n) ? n : 0;

        /// <summary>Add items.</summary>
        public void Add(string itemId, int count = 1)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count > 0) _counts[itemId] = Count(itemId) + count;
        }

        /// <summary>Remove one item; false (and no change) if none held.</summary>
        public bool TryRemove(string itemId)
        {
            var n = Count(itemId);
            if (n <= 0) return false;
            if (n == 1) _counts.Remove(itemId); else _counts[itemId] = n - 1;
            return true;
        }
    }
}
