using System.Collections.Generic;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>The unlimited storage crate: container entities plus counted stock (packets) by item id.</summary>
    public sealed class StorageCrate
    {
        private readonly Dictionary<int, Container> _containers = new Dictionary<int, Container>();
        private readonly List<int> _order = new List<int>();
        private readonly List<HeldThing> _things = new List<HeldThing>();
        private readonly Dictionary<string, int> _stock = new Dictionary<string, int>();

        /// <summary>Number of container entities held.</summary>
        public int ContainerCount => _order.Count;

        /// <summary>Container entities in the order they were added.</summary>
        public IEnumerable<Container> Containers
        {
            get { foreach (var id in _order) yield return _containers[id]; }
        }

        /// <summary>Add a container entity.</summary>
        public void AddContainer(Container c) { _containers[c.Id] = c; _order.Add(c.Id); }

        /// <summary>Find a held container by entity id, or null.</summary>
        public Container FindContainer(int id) => _containers.TryGetValue(id, out var c) ? c : null;

        /// <summary>Remove a container by entity id; null if not held.</summary>
        public Container TakeContainer(int id)
        {
            if (!_containers.TryGetValue(id, out var c)) return null;
            _containers.Remove(id); _order.Remove(id);
            return c;
        }

        /// <summary>Loose things (cans, bundles, packets) returned with their state, oldest first.</summary>
        public IReadOnlyList<HeldThing> Things => _things;

        /// <summary>Add a returned non-container thing, keeping its charges, quality or seeds.</summary>
        public void AddThing(HeldThing t) { _things.Add(t); }

        /// <summary>Remove and return the oldest loose thing with this item id; null if none.</summary>
        public HeldThing TakeThing(string itemId)
        {
            for (var i = 0; i < _things.Count; i++)
                if (_things[i].ItemId == itemId) { var t = _things[i]; _things.RemoveAt(i); return t; }
            return null;
        }

        /// <summary>True if a container, loose thing or counted stock of this item id is held.</summary>
        public bool HasItem(string itemId)
        {
            if (StockCount(itemId) > 0) return true;
            foreach (var c in _containers.Values) if (c.Def.ItemId == itemId) return true;
            foreach (var t in _things) if (t.ItemId == itemId) return true;
            return false;
        }

        /// <summary>Count of counted stock (e.g. seed packets) for an item id.</summary>
        public int StockCount(string itemId) => itemId != null && _stock.TryGetValue(itemId, out var n) ? n : 0;

        /// <summary>Add counted stock.</summary>
        public void AddStock(string itemId, int count)
        {
            if (count > 0) _stock[itemId] = StockCount(itemId) + count;
        }

        /// <summary>Take one counted item; false (no change) if none.</summary>
        public bool TryTakeStock(string itemId)
        {
            var n = StockCount(itemId);
            if (n <= 0) return false;
            if (n == 1) _stock.Remove(itemId); else _stock[itemId] = n - 1;
            return true;
        }
    }
}
