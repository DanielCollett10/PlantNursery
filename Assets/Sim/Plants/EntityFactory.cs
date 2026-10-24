using System;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Plants
{
    /// <summary>Builds sim entities from definitions, taking ids from <see cref="SimWorld.NextEntityId"/> (decision 0014).</summary>
    public sealed class EntityFactory
    {
        private readonly SimWorld _world;
        private readonly ContentDb _content;

        /// <summary>Creates the factory.</summary>
        public EntityFactory(SimWorld world, ContentDb content)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>Create an empty, dry container of the given container definition id. Throws on an unknown id.</summary>
        public Container CreateContainer(string containerId)
        {
            if (containerId == null || !_content.ContainerDefs.TryGetValue(containerId, out var def))
                throw new ArgumentException("Unknown container " + containerId, nameof(containerId));
            return new Container(_world.NextEntityId(), def);
        }

        /// <summary>Create a container from the item id that represents it, or null if the item is not a container.</summary>
        public Container CreateContainerForItem(string itemId)
        {
            foreach (var def in _content.ContainerDefs.Values)
                if (def.ItemId == itemId) return CreateContainer(def.Id);
            return null;
        }
    }
}
