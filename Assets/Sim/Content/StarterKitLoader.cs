using System;
using System.Collections.Generic;
using PlantNursery.Sim.Building;

namespace PlantNursery.Sim.Content
{
    /// <summary>
    /// Reads and validates economy.json <c>starterKit</c> (docs/design/economy.md, Starting kit).
    /// Checks item and variety ids, plot bounds, overlapping floor footprints, sowing rules, strip acceptance,
    /// strip slot range and overlap, and duplicate planting slots.
    /// </summary>
    internal static class StarterKitLoader
    {
        private static readonly int[] ValidDegrees = { 0, 90, 180, 270 };

        /// <summary>Loads the kit. Called after items, containers, species and building have loaded.</summary>
        /// <exception cref="ContentException">On an unknown id, a footprint outside the plot or overlapping another, and so on.</exception>
        public static StarterKitDef Load(ContentNode kit, IReadOnlyDictionary<string, ItemDef> items,
            IReadOnlyDictionary<string, ContainerDef> containers, IReadOnlyDictionary<string, SpeciesDef> species,
            BuildingConfig building)
        {
            var containerByItem = new Dictionary<string, ContainerDef>();
            foreach (var c in containers.Values) containerByItem[c.ItemId] = c;
            var speciesByVariety = new Dictionary<string, SpeciesDef>();
            foreach (var sp in species.Values)
                foreach (var v in sp.Varieties.Keys) speciesByVariety[v] = sp;

            var placements = new List<StarterPlacement>();
            var floorBoxes = new List<Box>();
            foreach (var n in kit.ObjArray("placements"))
            {
                var placement = ReadPlacement(n, items, containerByItem, speciesByVariety, building, floorBoxes);
                placements.Add(placement);
            }
            return new StarterKitDef { Placements = placements };
        }

        private static StarterPlacement ReadPlacement(ContentNode n, IReadOnlyDictionary<string, ItemDef> items,
            Dictionary<string, ContainerDef> containerByItem, Dictionary<string, SpeciesDef> speciesByVariety,
            BuildingConfig building, List<Box> floorBoxes)
        {
            var itemId = n.Str("item");
            if (!items.TryGetValue(itemId, out var def)) throw n.Error("item", "unknown item id '" + itemId + "'");
            if (def.Layer != ItemLayer.Floor) throw n.Error("item", "must be a floor item");

            var x = n.Int("x");
            var z = n.Int("z");
            var degrees = n.Int("rotation");
            if (Array.IndexOf(ValidDegrees, degrees) < 0) throw n.Error("rotation", "must be 0, 90, 180 or 270");

            var size = Footprints.Rotate(def.FloorFootprint, (Rotation)degrees);
            if (x + size.W > building.PlotWidth || z + size.D > building.PlotDepth)
                throw n.Error("x", "footprint is outside the plot");
            var box = new Box(x, z, x + size.W, z + size.D);
            for (int j = 0; j < floorBoxes.Count; j++)
                if (floorBoxes[j].Overlaps(box))
                    throw n.Error("x", "footprint overlaps starterKit.placements[" + j + "]");
            floorBoxes.Add(box);

            var planting = new List<StarterSowing>();
            var strip = new List<StarterStripItem>();
            var holds = new List<StarterStock>();
            if (n.Has("planting")) ReadPlanting(n, def, containerByItem, speciesByVariety, planting);
            if (n.Has("strip")) ReadStrip(n, def, strip);
            if (n.Has("holds")) ReadHolds(n, items, holds);
            return new StarterPlacement
            {
                ItemId = itemId, X = x, Z = z, RotationDegrees = degrees,
                Planting = planting, Strip = strip, Holds = holds,
            };
        }

        private static void ReadPlanting(ContentNode n, ItemDef def, Dictionary<string, ContainerDef> containerByItem,
            Dictionary<string, SpeciesDef> speciesByVariety, List<StarterSowing> into)
        {
            if (def.Bench == null) throw n.Error("planting", "only benches have planting slots");
            var used = new HashSet<int>();
            foreach (var s in n.ObjArray("planting"))
            {
                var slot = s.Int("slot");
                if (!used.Add(slot)) throw s.Error("slot", "slot " + slot + " is already used on this bench");

                var containerItem = s.Str("container");
                if (!containerByItem.TryGetValue(containerItem, out var container))
                    throw s.Error("container", "unknown container item '" + containerItem + "'");

                var varietyId = s.Str("sow");
                if (!speciesByVariety.TryGetValue(varietyId, out var sp))
                    throw s.Error("sow", "unknown variety id '" + varietyId + "'");
                if (sp.Sowing == null || !Contains(sp.Sowing.SowInto, container.Id))
                    throw s.Error("sow", "species '" + sp.Id + "' cannot be sown into '" + containerItem + "'");

                var moisture = s.Float("moisture");
                if (moisture > 100f) throw s.Error("moisture", "must be at most 100");

                into.Add(new StarterSowing { Slot = slot, ContainerItemId = containerItem, VarietyId = varietyId, Moisture = moisture });
            }
        }

        private static void ReadStrip(ContentNode n, ItemDef def, List<StarterStripItem> into)
        {
            if (def.Bench == null) throw n.Error("strip", "only benches have a working strip");
            var bench = def.Bench;
            var boxes = new List<Box>();
            foreach (var s in n.ObjArray("strip"))
            {
                var itemId = s.Str("item");
                var accept = FindAccept(bench, itemId);
                if (accept == null) throw s.Error("item", "'" + itemId + "' is not accepted on this bench's working strip");

                var along = accept.StripUnits.W;
                var across = accept.StripUnits.D;
                // The strip runs the bench's length (building.md, Working strip: 30 units on a 3 m bench).
                var cols = bench.PlantingLengthUnits / along;
                var rows = bench.StripDepthUnits / across;
                var slot = s.Int("slot");
                if (slot >= cols * rows) throw s.Error("slot", "no such strip slot (" + cols * rows + " fit)");

                var col = slot % cols;
                var row = slot / cols;
                var box = new Box(col * along, row * across, col * along + along, row * across + across);
                for (int j = 0; j < boxes.Count; j++)
                    if (boxes[j].Overlaps(box)) throw s.Error("slot", "overlaps another strip item on this bench");
                boxes.Add(box);
                into.Add(new StarterStripItem { Slot = slot, ItemId = itemId });
            }
        }

        private static void ReadHolds(ContentNode n, IReadOnlyDictionary<string, ItemDef> items, List<StarterStock> into)
        {
            foreach (var h in n.ObjArray("holds"))
            {
                var itemId = h.Str("item");
                if (!items.ContainsKey(itemId)) throw h.Error("item", "unknown item id '" + itemId + "'");
                into.Add(new StarterStock { ItemId = itemId, Count = h.Int("count", 1) });
            }
        }

        private static StripAccept FindAccept(BenchDef bench, string itemId)
        {
            foreach (var a in bench.StripAccepts)
                if (a.ItemId == itemId) return a;
            return null;
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            if (list == null) return false;
            foreach (var s in list)
                if (s == value) return true;
            return false;
        }

        /// <summary>Half-open rectangle in integer units, used for overlap checks.</summary>
        private readonly struct Box
        {
            private readonly int _x0, _y0, _x1, _y1;

            public Box(int x0, int y0, int x1, int y1)
            {
                _x0 = x0; _y0 = y0; _x1 = x1; _y1 = y1;
            }

            public bool Overlaps(Box o) => _x0 < o._x1 && o._x0 < _x1 && _y0 < o._y1 && o._y0 < _y1;
        }
    }
}
