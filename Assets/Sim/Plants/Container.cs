using System.Collections.Generic;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// A pot or tray as a sim entity (plants.md, Containers). Plain data. Built only by <see cref="EntityFactory"/>.
    /// </summary>
    public sealed class Container
    {
        private readonly List<int> _plantIds = new List<int>();

        /// <summary>Entity id from SimWorld.NextEntityId.</summary>
        public int Id { get; }
        /// <summary>The container definition.</summary>
        public ContainerDef Def { get; }
        /// <summary>Water meter, 0..100.</summary>
        public float WaterMeter { get; set; }
        /// <summary>Ids of the plants growing in it.</summary>
        public IReadOnlyList<int> PlantIds => _plantIds;

        internal Container(int id, ContainerDef def)
        {
            Id = id; Def = def;
        }

        /// <summary>Record a plant in this container.</summary>
        public void AddPlant(int plantId) { _plantIds.Add(plantId); }

        /// <summary>Remove a plant id; false if it was not there.</summary>
        public bool RemovePlant(int plantId) { return _plantIds.Remove(plantId); }
    }
}
