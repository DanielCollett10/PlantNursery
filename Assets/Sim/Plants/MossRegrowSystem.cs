using System;
using System.Collections.Generic;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// Growth-phase system that counts down the regrow time of scraped plants (see <see cref="MossRules.RegrowTicks"/>).
    /// Regrowing pauses while the plant is Dry or Wilted. Plants are found through delegates.
    /// </summary>
    public sealed class MossRegrowSystem : ISimSystem
    {
        private readonly Func<IEnumerable<Container>> _containers;
        private readonly Func<int, Plant> _plantById;

        /// <summary>Creates the system.</summary>
        public MossRegrowSystem(Func<IEnumerable<Container>> containers, Func<int, Plant> plantById)
        {
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
            _plantById = plantById ?? throw new ArgumentNullException(nameof(plantById));
        }

        /// <summary>Creates a system and adds it to the world in the Growth phase.</summary>
        public static MossRegrowSystem Install(SimWorld world, Func<IEnumerable<Container>> containers, Func<int, Plant> plantById)
        {
            var s = new MossRegrowSystem(containers, plantById);
            world.AddSystem(s, SystemPhase.Growth);
            return s;
        }

        /// <inheritdoc />
        public void Update(SimWorld world)
        {
            foreach (var c in _containers())
            {
                foreach (var id in c.PlantIds)
                {
                    var p = _plantById(id);
                    if (p == null || p.IsDead || !p.IsRegrowing || p.IsDry || p.IsWilted) continue;
                    p.RegrowTicksLeft--;
                }
            }
        }
    }
}
