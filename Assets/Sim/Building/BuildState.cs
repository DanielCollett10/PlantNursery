using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Building
{
    /// <summary>Why a placement or pick-up is refused. Placement reasons are listed in check order.</summary>
    public enum BuildRejection
    {
        /// <summary>Accepted.</summary>
        None = 0,
        /// <summary>The item id is not in content.</summary>
        UnknownItem,
        /// <summary>The requested layer does not match the item, or is not supported yet.</summary>
        WrongLayer,
        /// <summary>The item is not in the build inventory.</summary>
        NotOwned,
        /// <summary>A footprint cell is outside the plot.</summary>
        OutOfPlot,
        /// <summary>A footprint cell already holds a Floor item.</summary>
        Occupied,
        /// <summary>Surface items sit on the item being picked up.</summary>
        HasItemsOnTop,
        /// <summary>No placed item has that id.</summary>
        UnknownPlacedItem
    }

    /// <summary>Floor grid, placed items and the shared build inventory of one nursery.</summary>
    public sealed class BuildState
    {
        private readonly ContentDb _content;
        private readonly Dictionary<Cell, int> _cells = new Dictionary<Cell, int>();
        private readonly Dictionary<int, PlacedItem> _placed = new Dictionary<int, PlacedItem>();
        private int _nextId = 1;

        /// <summary>Plot width in cells.</summary>
        public int PlotWidth { get; }
        /// <summary>Plot depth in cells.</summary>
        public int PlotDepth { get; }
        /// <summary>Shared unplaced items.</summary>
        public BuildInventory Inventory { get; }
        /// <summary>Hook that reports surface items on top of a floor item.</summary>
        public ISurfaceOccupancy SurfaceOccupancy { get; set; } = new NoSurfaceOccupancy();
        /// <summary>All placed items.</summary>
        public IEnumerable<PlacedItem> PlacedItems => _placed.Values;

        /// <summary>Creates the state: plot size from content, inventory from the starter inventory.</summary>
        public BuildState(ContentDb content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            PlotWidth = content.Building.PlotWidth;
            PlotDepth = content.Building.PlotDepth;
            Inventory = new BuildInventory(content.Economy.StarterInventory);
        }

        /// <summary>The placed item covering a cell, or null.</summary>
        public PlacedItem GetAt(Cell cell) => _cells.TryGetValue(cell, out var id) ? _placed[id] : null;

        /// <summary>A placed item by id, or null.</summary>
        public PlacedItem Get(int placedId) => _placed.TryGetValue(placedId, out var p) ? p : null;

        /// <summary>Query-only: would this placement be accepted? Returns the first failing reason.</summary>
        public BuildRejection CanPlace(string itemId, ItemLayer layer, Cell anchor, Rotation rotation)
        {
            if (itemId == null || !_content.ItemDefs.TryGetValue(itemId, out var def)) return BuildRejection.UnknownItem;
            if (def.Layer != layer || layer != ItemLayer.Floor) return BuildRejection.WrongLayer;
            if (Inventory.Count(itemId) <= 0) return BuildRejection.NotOwned;
            var size = Footprints.Rotate(def.FloorFootprint, rotation);
            for (var dx = 0; dx < size.W; dx++)
                for (var dz = 0; dz < size.D; dz++)
                {
                    var x = anchor.X + dx;
                    var z = anchor.Z + dz;
                    if (x < 0 || z < 0 || x >= PlotWidth || z >= PlotDepth) return BuildRejection.OutOfPlot;
                }
            for (var dx = 0; dx < size.W; dx++)
                for (var dz = 0; dz < size.D; dz++)
                    if (_cells.ContainsKey(new Cell(anchor.X + dx, anchor.Z + dz))) return BuildRejection.Occupied;
            return BuildRejection.None;
        }

        /// <summary>Place an item, consuming one from the inventory. Returns the placed item, or null with the reason.</summary>
        public PlacedItem Place(string itemId, ItemLayer layer, Cell anchor, Rotation rotation, out BuildRejection rejection)
        {
            rejection = CanPlace(itemId, layer, anchor, rotation);
            if (rejection != BuildRejection.None) return null;
            var size = Footprints.Rotate(_content.ItemDefs[itemId].FloorFootprint, rotation);
            Inventory.TryRemove(itemId);
            var item = new PlacedItem(_nextId++, itemId, anchor, rotation, size.W, size.D);
            _placed[item.Id] = item;
            for (var dx = 0; dx < size.W; dx++)
                for (var dz = 0; dz < size.D; dz++)
                    _cells[new Cell(anchor.X + dx, anchor.Z + dz)] = item.Id;
            return item;
        }

        /// <summary>Query-only: would picking up be accepted?</summary>
        public BuildRejection CanPickUp(int placedId)
        {
            if (!_placed.TryGetValue(placedId, out var item)) return BuildRejection.UnknownPlacedItem;
            return SurfaceOccupancy.HasItemsOn(item) ? BuildRejection.HasItemsOnTop : BuildRejection.None;
        }

        /// <summary>Pick up a placed item back into the inventory. Returns the item, or null with the reason.</summary>
        public PlacedItem PickUp(int placedId, out BuildRejection rejection)
        {
            rejection = CanPickUp(placedId);
            if (rejection != BuildRejection.None) return null;
            var item = _placed[placedId];
            _placed.Remove(placedId);
            for (var dx = 0; dx < item.Width; dx++)
                for (var dz = 0; dz < item.Depth; dz++)
                    _cells.Remove(new Cell(item.Anchor.X + dx, item.Anchor.Z + dz));
            Inventory.Add(item.ItemId);
            return item;
        }

        /// <summary>Place the starter tap, computer, bed and delivery shelf from the inventory at fixed spots. Items not in the inventory are skipped.</summary>
        public void PlaceStarterLayout()
        {
            Place("tap", ItemLayer.Floor, new Cell(1, 1), Rotation.Deg0, out _);
            Place("computer", ItemLayer.Floor, new Cell(3, 1), Rotation.Deg0, out _);
            Place("bed", ItemLayer.Floor, new Cell(1, 4), Rotation.Deg0, out _);
            Place("delivery_shelf", ItemLayer.Floor, new Cell(PlotWidth - 2, 1), Rotation.Deg0, out _);
        }
    }
}
