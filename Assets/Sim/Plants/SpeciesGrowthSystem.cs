using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// Growth phase system for species plants (plants.md, Growth): stage progress by days and season, stage changes,
    /// and RootBound when the container is too small for the next stage. Plants are found through delegates.
    /// </summary>
    public sealed class SpeciesGrowthSystem : ISimSystem
    {
        /// <summary>Progress at or above 1 minus this counts as finished (float accumulation error).</summary>
        private const float Epsilon = 2e-4f;

        private readonly ContentDb _content;
        private readonly Func<IEnumerable<Container>> _containers;
        private readonly Func<int, Plant> _plantById;
        private readonly GameClock _clock;

        /// <summary>Creates the system.</summary>
        public SpeciesGrowthSystem(ContentDb content, Func<IEnumerable<Container>> containers, Func<int, Plant> plantById, GameClock clock)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
            _plantById = plantById ?? throw new ArgumentNullException(nameof(plantById));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Creates a system and adds it to the world in the Growth phase.</summary>
        public static SpeciesGrowthSystem Install(SimWorld world, ContentDb content, Func<IEnumerable<Container>> containers,
            Func<int, Plant> plantById, GameClock clock)
        {
            var s = new SpeciesGrowthSystem(content, containers, plantById, clock);
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
                    if (p == null || p.IsDead || !p.IsSpeciesPlant) continue;
                    if (!_content.SpeciesDefs.TryGetValue(p.SpeciesId, out var sp)) continue;
                    Grow(world, p, sp, c.Def.Id);
                }
            }
        }

        private void Grow(SimWorld world, Plant p, SpeciesDef sp, string containerId)
        {
            if (p.Lifecycle != null && p.Lifecycle.Current == PlantLifecycle.Wilted) return;
            if (p.IsDry || p.IsWilted) return;
            p.ContainerId = containerId;
            var idx = p.StageIndex;
            if (idx >= sp.Stages.Count - 1) return; // final stage: nothing further to grow into
            var stage = sp.Stages[idx];
            var next = sp.Stages[idx + 1];
            var allowsNext = Contains(next.AllowedContainers, containerId);

            if (p.StageProgress < 1f - Epsilon)
            {
                var mult = SeasonMultiplier(sp);
                if (mult <= 0f || stage.Days <= 0) return;
                p.StageProgress += mult / (stage.Days * (float)GameClock.MinutesPerDay);
                if (p.StageProgress < 1f - Epsilon) return;
                p.StageProgress = 1f;
            }

            if (!allowsNext)
            {
                if (p.Lifecycle != null) p.Lifecycle.TryTransition(PlantLifecycle.RootBound);
                return;
            }
            if (p.Lifecycle != null && p.Lifecycle.Current == PlantLifecycle.RootBound)
                p.Lifecycle.TryTransition(PlantLifecycle.Growing);
            var from = p.Stage;
            p.StageIndex = idx + 1;
            p.StageProgress = 0f;
            p.Stage = (PlantStage)Math.Min(p.StageIndex, (int)PlantStage.Mature);
            world.Raise(new PlantStageChanged(p.Id, from, p.Stage, idx, p.StageIndex, next.Name));
        }

        private float SeasonMultiplier(SpeciesDef sp)
        {
            var i = (int)_clock.Season;
            return i < sp.SeasonMultipliers.Count ? sp.SeasonMultipliers[i] : 1f;
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            for (var i = 0; i < list.Count; i++) if (list[i] == id) return true;
            return false;
        }
    }
}
