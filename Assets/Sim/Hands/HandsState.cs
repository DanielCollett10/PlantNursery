using System;
using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Hands
{
    /// <summary>Why a hands action was refused. The name is used as the command result message.</summary>
    public enum HandsRejection
    {
        /// <summary>Accepted.</summary>
        None,
        /// <summary>The hand already holds something.</summary>
        HandsFull,
        /// <summary>The target is not a holdable surface item.</summary>
        NotHoldable,
        /// <summary>The hand is empty.</summary>
        NothingHeld,
        /// <summary>The target units are not over a surface.</summary>
        NoSurface,
        /// <summary>The target units are taken.</summary>
        Occupied,
        /// <summary>The player is not holding a watering can.</summary>
        NoCan,
        /// <summary>The can has no water.</summary>
        CanEmpty,
        /// <summary>The plant is dead.</summary>
        PlantDead,
        /// <summary>No plant with that id.</summary>
        UnknownPlant
    }

    /// <summary>What an item carries: plants (trays, pots) or water (cans).</summary>
    public sealed class ItemContents
    {
        /// <summary>Plants attached to this container.</summary>
        public List<Plant> Plants { get; } = new List<Plant>();
        /// <summary>Water charges (watering cans).</summary>
        public int Water { get; set; }
    }

    /// <summary>An item in a player's hand.</summary>
    public sealed class HeldItem
    {
        /// <summary>Item definition id.</summary>
        public string ItemId { get; }
        /// <summary>Plants and water travelling with it.</summary>
        public ItemContents Contents { get; }
        /// <summary>Creates a held item.</summary>
        public HeldItem(string itemId, ItemContents contents) { ItemId = itemId; Contents = contents; }
    }

    /// <summary>
    /// Per-player hand slots plus the contents (plants, water) of surface items. Contents stay with the
    /// container when it is picked up and put down, so carrying a planted tray carries its plants.
    /// </summary>
    public sealed class HandsState
    {
        private readonly ContentDb _content;
        private readonly SurfaceGrid _grid;
        private readonly Dictionary<int, HeldItem> _held = new Dictionary<int, HeldItem>();
        private readonly Dictionary<int, ItemContents> _contents = new Dictionary<int, ItemContents>();
        private readonly Dictionary<int, Plant> _plants = new Dictionary<int, Plant>();

        /// <summary>Creates hands state over a surface grid.</summary>
        public HandsState(ContentDb content, SurfaceGrid grid)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        /// <summary>What the player holds, or null.</summary>
        public HeldItem GetHeld(PlayerId player) => _held.TryGetValue(player.Value, out var h) ? h : null;

        /// <summary>Contents of a surface item on the grid (created on demand; new cans start full).</summary>
        public ItemContents GetContents(SurfaceItem item)
        {
            if (!_contents.TryGetValue(item.Id, out var c))
            {
                c = new ItemContents { Water = _content.ItemDefs[item.ItemId].WaterCapacity };
                _contents[item.Id] = c;
            }
            return c;
        }

        /// <summary>Attach a plant to a container sitting on the surface grid.</summary>
        public void AddPlant(SurfaceItem container, Plant plant)
        {
            GetContents(container).Plants.Add(plant);
            _plants[plant.Id] = plant;
        }

        /// <summary>Attach a plant to the container a player is holding.</summary>
        public void AddPlant(PlayerId player, Plant plant)
        {
            var h = GetHeld(player) ?? throw new InvalidOperationException("Nothing held.");
            h.Contents.Plants.Add(plant);
            _plants[plant.Id] = plant;
        }

        /// <summary>Finds a plant by id, or null.</summary>
        public Plant GetPlant(int plantId) => _plants.TryGetValue(plantId, out var p) ? p : null;

        /// <summary>Query-only check for picking up the surface item with this id.</summary>
        public HandsRejection CanPickUp(PlayerId player, int itemId)
        {
            if (_held.ContainsKey(player.Value)) return HandsRejection.HandsFull;
            return FindSurface(itemId) == null ? HandsRejection.NotHoldable : HandsRejection.None;
        }

        /// <summary>Pick up a surface item, freeing its units.</summary>
        public HandsRejection PickUp(PlayerId player, int itemId)
        {
            var r = CanPickUp(player, itemId);
            if (r != HandsRejection.None) return r;
            var item = FindSurface(itemId);
            var contents = GetContents(item);
            _contents.Remove(item.Id);
            _grid.Remove(item.Id);
            _held[player.Value] = new HeldItem(item.ItemId, contents);
            return HandsRejection.None;
        }

        /// <summary>Query-only check for putting the held item down.</summary>
        public HandsRejection CanPutDown(PlayerId player, Unit anchor, Rotation rotation)
        {
            var h = GetHeld(player);
            if (h == null) return HandsRejection.NothingHeld;
            switch (_grid.CanPlace(h.ItemId, anchor, rotation))
            {
                case BuildRejection.None: return HandsRejection.None;
                case BuildRejection.Occupied: return HandsRejection.Occupied;
                case BuildRejection.NoSurface: return HandsRejection.NoSurface;
                default: return HandsRejection.NotHoldable;
            }
        }

        /// <summary>Put the held item down on the grid; returns the new surface item or null.</summary>
        public SurfaceItem PutDown(PlayerId player, Unit anchor, Rotation rotation, out HandsRejection rejection)
        {
            rejection = CanPutDown(player, anchor, rotation);
            if (rejection != HandsRejection.None) return null;
            var h = _held[player.Value];
            var item = _grid.Place(h.ItemId, anchor, rotation, out _);
            _contents[item.Id] = h.Contents;
            _held.Remove(player.Value);
            return item;
        }

        /// <summary>Query-only check for refilling the held can at the tap.</summary>
        public HandsRejection CanRefill(PlayerId player) => IsHoldingCan(player) ? HandsRejection.None : HandsRejection.NoCan;

        /// <summary>Fill the held can to capacity.</summary>
        public HandsRejection RefillCan(PlayerId player)
        {
            var r = CanRefill(player);
            if (r != HandsRejection.None) return r;
            var h = _held[player.Value];
            h.Contents.Water = _content.ItemDefs[h.ItemId].WaterCapacity;
            return HandsRejection.None;
        }

        /// <summary>Query-only check for watering a plant with the held can.</summary>
        public HandsRejection CanWater(PlayerId player, int plantId)
        {
            if (!IsHoldingCan(player)) return HandsRejection.NoCan;
            if (_held[player.Value].Contents.Water <= 0) return HandsRejection.CanEmpty;
            var plant = GetPlant(plantId);
            if (plant == null) return HandsRejection.UnknownPlant;
            return plant.IsDead ? HandsRejection.PlantDead : HandsRejection.None;
        }

        /// <summary>Water a plant: moisture 100, can loses one charge.</summary>
        public HandsRejection WaterPlant(PlayerId player, int plantId)
        {
            var r = CanWater(player, plantId);
            if (r != HandsRejection.None) return r;
            GetPlant(plantId).Moisture = 100f;
            _held[player.Value].Contents.Water--;
            return HandsRejection.None;
        }

        private bool IsHoldingCan(PlayerId player)
        {
            var h = GetHeld(player);
            return h != null && _content.ItemDefs[h.ItemId].WaterCapacity > 0;
        }

        private SurfaceItem FindSurface(int id)
        {
            foreach (var i in _grid.Items) if (i.Id == id) return i;
            return null;
        }
    }
}
