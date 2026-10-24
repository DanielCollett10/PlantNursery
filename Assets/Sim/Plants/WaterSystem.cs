using System;
using System.Collections.Generic;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Plants
{
    /// <summary>Raised once when a plant becomes Wilted.</summary>
    public sealed class PlantWilted : ISimEvent
    {
        /// <summary>Plant id.</summary>
        public int PlantId { get; }
        /// <summary>Creates the event.</summary>
        public PlantWilted(int plantId) { PlantId = plantId; }
    }

    /// <summary>
    /// Water phase system (plants.md, Watering): drains each container's meter, then drives Dry, Wilted and Dead
    /// for its plants and updates the care counters. Plants are found through the two delegates.
    /// </summary>
    public sealed class WaterSystem : ISimSystem
    {
        private readonly Func<IEnumerable<Container>> _containers;
        private readonly Func<int, Plant> _plantById;
        private readonly GameClock _clock;
        private readonly WaterConfig _cfg;

        /// <summary>Creates the system.</summary>
        public WaterSystem(Func<IEnumerable<Container>> containers, Func<int, Plant> plantById, GameClock clock, WaterConfig config = null)
        {
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
            _plantById = plantById ?? throw new ArgumentNullException(nameof(plantById));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _cfg = config ?? new WaterConfig();
        }

        /// <summary>Creates a system and adds it to the world in the Watering phase.</summary>
        public static WaterSystem Install(SimWorld world, Func<IEnumerable<Container>> containers, Func<int, Plant> plantById,
            GameClock clock, WaterConfig config = null)
        {
            var s = new WaterSystem(containers, plantById, clock, config);
            world.AddSystem(s, SystemPhase.Watering);
            return s;
        }

        /// <inheritdoc />
        public void Update(SimWorld world)
        {
            // The minute that just elapsed (tick-1 to tick) sets the rate, so 22:00 to 06:00 is all night.
            var rate = _clock.NeedRateMultiplierAt(world.Tick - 1);
            var wiltTicks = _cfg.WiltAfterHours * 60;
            var dieTicks = _cfg.DieAfterWiltedDays * GameClock.MinutesPerDay;
            foreach (var c in _containers())
            {
                c.WaterMeter = Math.Max(0f, c.WaterMeter - c.Def.DrainPerHour / 60f * rate);
                var dry = c.WaterMeter <= 0f;
                foreach (var id in c.PlantIds)
                {
                    var p = _plantById(id);
                    if (p == null || p.IsDead) continue;
                    UpdatePlant(world, p, dry, wiltTicks, dieTicks);
                }
            }
        }

        private static void UpdatePlant(SimWorld world, Plant p, bool dry, int wiltTicks, int dieTicks)
        {
            p.TicksAlive++;
            p.IsDry = dry;
            if (!dry)
            {
                p.TicksNotDry++;
                p.DryTicks = 0;
                p.WiltedTicks = 0;
                if (p.IsWilted)
                {
                    p.IsWilted = false;
                    if (p.Lifecycle != null) p.Lifecycle.TryTransition(PlantLifecycle.Growing);
                }
                return;
            }
            p.DryTicks++;
            if (!p.IsWilted)
            {
                if (p.DryTicks < wiltTicks) return;
                p.IsWilted = true;
                if (p.Lifecycle != null) p.Lifecycle.TryTransition(PlantLifecycle.Wilted);
                p.WiltedTicks = 0;
                world.Raise(new PlantWilted(p.Id));
                return;
            }
            p.WiltedTicks++;
            if (p.WiltedTicks >= dieTicks)
            {
                if (p.Lifecycle != null) p.Lifecycle.TryTransition(PlantLifecycle.Dead);
                p.IsDead = true;
                world.Raise(new PlantDied(p.Id));
            }
        }
    }
}
