using System.Collections.Generic;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>The unlimited storage crate: container entities plus counted stock (packets) by item id.</summary>
    public sealed class StorageCrate
    {
        private readonly Dictionary<int, Container> _containers = new Dictionary<int, Container>();
        private readonly List<int> _order = new List<int>();
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
