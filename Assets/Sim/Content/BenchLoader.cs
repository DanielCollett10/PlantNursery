using System.Collections.Generic;

namespace PlantNursery.Sim.Content
{
    /// <summary>
    /// Reads and validates the <c>planting</c> and <c>workStrip</c> blocks of bench items.
    /// Item ids in <c>workStrip.accepts</c> are checked by <see cref="ValidateAcceptedIds"/> once every item is loaded.
    /// </summary>
    internal static class BenchLoader
    {
        /// <summary>Reads a bench item's planting and strip. <paramref name="item"/> is the item's JSON object.</summary>
        /// <exception cref="ContentException">On missing or negative values, or an accepted footprint deeper than the strip.</exception>
        public static BenchDef Load(ContentNode item)
        {
            var planting = item.Obj("planting");
            var def = new BenchDef
            {
                PlantingLengthUnits = planting.Int("lengthUnits", 1),
                PlantingDepthUnits = planting.Int("depthUnits", 1),
                PlantingBackOffsetUnits = planting.Int("backOffsetUnits"),
            };

            var strip = item.Obj("workStrip");
            def.StripDepthUnits = strip.Int("depthUnits");
            var accepts = new List<StripAccept>();
            var seen = new HashSet<string>();
            foreach (var a in strip.ObjArray("accepts"))
            {
                var accept = new StripAccept { ItemId = a.Str("itemId"), StripUnits = a.Size("stripUnits") };
                if (!seen.Add(accept.ItemId)) throw a.Error("itemId", "duplicate item id '" + accept.ItemId + "'");
                if (accept.StripUnits.D > def.StripDepthUnits)
                    throw a.Error("stripUnits", "across (" + accept.StripUnits.D + ") is deeper than the strip (" + def.StripDepthUnits + " units)");
                accepts.Add(accept);
            }
            def.StripAccepts = accepts;
            return def;
        }

        /// <summary>Checks that every item named in a bench's <c>workStrip.accepts</c> exists.</summary>
        /// <exception cref="ContentException">On an unknown item id.</exception>
        public static void ValidateAcceptedIds(List<ContentNode> itemNodes, IReadOnlyDictionary<string, ItemDef> items)
        {
            for (int i = 0; i < itemNodes.Count; i++)
            {
                var def = items[itemNodes[i].Str("id")];
                if (def.Bench == null) continue;
                foreach (var a in itemNodes[i].Obj("workStrip").ObjArray("accepts"))
                {
                    var id = a.Str("itemId");
                    if (!items.ContainsKey(id)) throw a.Error("itemId", "unknown item id '" + id + "'");
                }
            }
        }
    }
}
