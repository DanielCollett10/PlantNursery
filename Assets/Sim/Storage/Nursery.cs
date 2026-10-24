using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>The state objects of one freshly built nursery (see <see cref="NurseryBuilder"/>).</summary>
    public sealed class Nursery
    {
        /// <summary>Floor grid and placed items.</summary>
        public BuildState Build { get; }
        /// <summary>Planting slot occupancy.</summary>
        public SlotOccupancy Slots { get; }
        /// <summary>Working strip occupancy.</summary>
        public StripOccupancy Strip { get; }
        /// <summary>Things sitting in bench slots.</summary>
        public SlotContents Contents { get; }
        /// <summary>The storage crate.</summary>
        public StorageCrate Crate { get; }
        /// <summary>Per-player hands.</summary>
        public HandsV2 Hands { get; }
        /// <summary>The purse.</summary>
        public Wallet Wallet { get; }
        /// <summary>Every plant made for the kit, by plant id.</summary>
        public IReadOnlyDictionary<int, Plant> Plants => PlantMap;

        internal Dictionary<int, Plant> PlantMap { get; } = new Dictionary<int, Plant>();

        internal Nursery(BuildState build, SlotOccupancy slots, StripOccupancy strip, SlotContents contents,
            StorageCrate crate, HandsV2 hands, Wallet wallet)
        {
            Build = build; Slots = slots; Strip = strip; Contents = contents; Crate = crate; Hands = hands; Wallet = wallet;
        }

        /// <summary>Every container entity: in the crate, in bench slots, in hands, and pots inside carry trays.</summary>
        public IEnumerable<Container> AllContainers()
        {
            var seen = new HashSet<int>();
            foreach (var c in Crate.Containers) if (seen.Add(c.Id)) yield return c;
            foreach (var t in Contents.Things)
                foreach (var c in In(t)) if (seen.Add(c.Id)) yield return c;
            foreach (var t in Hands.AllHeld)
                foreach (var c in In(t)) if (seen.Add(c.Id)) yield return c;
        }

        /// <summary>The plant with this id, or null.</summary>
        public Plant PlantById(int id) => PlantMap.TryGetValue(id, out var p) ? p : null;

        private static IEnumerable<Container> In(HeldThing thing)
        {
            if (thing.Container != null) yield return thing.Container;
            foreach (var pot in thing.Pots) yield return pot;
        }

        /// <summary>
        /// Find a container entity by id wherever it is: in the crate, in a bench slot, in a hand, or as a pot in a carry tray
        /// (in a slot or a hand). Null if none.
        /// </summary>
        public Container FindContainer(int id)
        {
            var c = Crate.FindContainer(id);
            if (c != null) return c;
            foreach (var t in Contents.Things)
            {
                c = FindIn(t, id);
                if (c != null) return c;
            }
            foreach (var t in Hands.AllHeld)
            {
                c = FindIn(t, id);
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>Find the container (crate, slot, hand, or pot in a carry tray) that holds the plant. Null if none.</summary>
        public Container FindContainerOfPlant(int plantId)
        {
            foreach (var c in Crate.Containers)
                if (HasPlant(c, plantId)) return c;
            foreach (var t in Contents.Things) { var c = PlantIn(t, plantId); if (c != null) return c; }
            foreach (var t in Hands.AllHeld) { var c = PlantIn(t, plantId); if (c != null) return c; }
            return null;
        }

        private static bool HasPlant(Container c, int plantId)
        {
            foreach (var id in c.PlantIds) if (id == plantId) return true;
            return false;
        }

        private static Container PlantIn(HeldThing thing, int plantId)
        {
            if (thing.Container != null && HasPlant(thing.Container, plantId)) return thing.Container;
            foreach (var pot in thing.Pots)
                if (HasPlant(pot, plantId)) return pot;
            return null;
        }

        private static Container FindIn(HeldThing thing, int id)
        {
            if (thing.Container != null && thing.Container.Id == id) return thing.Container;
            foreach (var pot in thing.Pots)
                if (pot.Id == id) return pot;
            return null;
        }
    }
}
