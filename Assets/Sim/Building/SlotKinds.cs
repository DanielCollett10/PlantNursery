using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>Helpers that turn a held thing into the slot size or strip item it addresses.</summary>
    internal static class SlotKinds
    {
        /// <summary>The container with the smallest footprint (ties: lowest id); null if there are none.</summary>
        public static string SmallestContainer(ContentDb content)
        {
            string best = null; var bestArea = int.MaxValue;
            foreach (var c in content.ContainerDefs.Values)
            {
                var area = c.AlongUnits * c.AcrossUnits;
                if (area <= 0) continue;
                if (area < bestArea || (area == bestArea && string.CompareOrdinal(c.Id, best) < 0)) { best = c.Id; bestArea = area; }
            }
            return best;
        }

        /// <summary>Strip item id for a held kind: a container maps to its item; any other id is used as is.</summary>
        public static string StripItemFor(ContentDb content, string heldKind)
        {
            if (heldKind == null) return null;
            return content.ContainerDefs.TryGetValue(heldKind, out var c) && c.ItemId != null ? c.ItemId : heldKind;
        }

        /// <summary>The strip entry for an item id, or null.</summary>
        public static StripAccept Accept(BenchDef bench, string itemId)
        {
            if (bench == null || itemId == null || bench.StripDepthUnits <= 0 || bench.StripAccepts == null) return null;
            foreach (var a in bench.StripAccepts) if (a.ItemId == itemId) return a;
            return null;
        }

        /// <summary>The accepted strip item with the smallest footprint (ties: file order); null if none.</summary>
        public static string SmallestStripItem(BenchDef bench)
        {
            if (bench == null || bench.StripDepthUnits <= 0 || bench.StripAccepts == null) return null;
            string best = null; var bestArea = int.MaxValue;
            foreach (var a in bench.StripAccepts)
            {
                var area = a.StripUnits.W * a.StripUnits.D;
                if (area > 0 && area < bestArea) { best = a.ItemId; bestArea = area; }
            }
            return best;
        }
    }
}
