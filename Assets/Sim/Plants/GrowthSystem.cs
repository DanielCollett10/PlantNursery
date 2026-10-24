using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Plants
{
    /// <summary>
    /// Per-tick plant update (docs/design/plants.md "Growth per tick"): drying, growth, stages,
    /// root-bound, wilting, recovery, death and care counters. One tick = one minute.
    /// </summary>
    public sealed class GrowthSystem : ISimSystem
    {
        private readonly ContentDb _content;
        private readonly HandsState _hands;
        private readonly GameClock _clock;
        private readonly GrowthConfig _cfg;

        /// <summary>Creates the system.</summary>
        public GrowthSystem(ContentDb content, HandsState hands, GameClock clock, GrowthConfig config = null)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _hands = hands ?? throw new ArgumentNullException(nameof(hands));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _cfg = config ?? new GrowthConfig();
        }

        /// <summary>Creates a system and adds it to the world.</summary>
        public static GrowthSystem Install(SimWorld world, ContentDb content, HandsState hands, GameClock clock, GrowthConfig config = null)
        {
            var s = new GrowthSystem(content, hands, clock, config);
            world.AddSystem(s, SystemPhase.Growth);
            return s;
        }

        /// <summary>The config in use.</summary>
        public GrowthConfig Config => _cfg;

        /// <inheritdoc />
        public void Update(SimWorld world)
        {
            var season = _clock.SeasonAt(world.Tick);
            var list = new List<KeyValuePair<Plant, ContainerDef>>();
            _hands.CollectPlants(list);
            foreach (var kv in list)
                if (!kv.Key.IsDead) UpdatePlant(world, kv.Key, kv.Value, season);
        }

        private void UpdatePlant(SimWorld world, Plant p, ContainerDef container, Season season)
        {
            if (!_content.VarietyDefs.TryGetValue(p.VarietyId, out var variety)) return;

            p.Moisture = Math.Max(0f, p.Moisture - container.DryRatePerHour / 60f);

            var waterFactor = p.Moisture >= _cfg.WellWateredMoisture ? 1f : p.Moisture > 0f ? 0.5f : 0f;
            var seasonFactor = variety.SeasonGrowth[(int)season];
            var cap = variety.StageThresholds[(int)container.MaxStage];
            var rootBound = p.Stage >= container.MaxStage;
            if (!rootBound)
                p.Growth = Math.Min(cap, p.Growth + variety.GrowthPerHour / 60f * waterFactor * seasonFactor);

            var stage = StageFor(variety, p.Growth, container.MaxStage);
            if (stage != p.Stage)
            {
                var from = p.Stage;
                p.Stage = stage;
                world.Raise(new PlantStageChanged(p.Id, from, stage));
            }

            if (p.Stage >= container.MaxStage) p.RootBoundMinutes++;
            var pastGrace = p.Stage >= container.MaxStage && p.RootBoundMinutes > _cfg.RootBoundGraceHours * 60f;

            p.AliveMinutes++;
            var goodCare = p.Moisture >= _cfg.WellWateredMoisture && !pastGrace;
            if (goodCare) p.GoodCareMinutes++;

            var delta = 0f;
            if (pastGrace) delta -= _cfg.RootBoundHealthLossPerHour / 60f;
            if (p.Moisture <= 0f) delta -= _cfg.WiltHealthLossPerHour / 60f;
            if (goodCare) delta += _cfg.RecoverPerHour / 60f;
            p.Health = Math.Min(100f, p.Health + delta);

            if (p.Health <= 0f)
            {
                p.Health = 0f;
                p.IsDead = true;
                world.Raise(new PlantDied(p.Id));
            }
        }

        private static PlantStage StageFor(VarietyDef v, float growth, PlantStage cap)
        {
            var stage = PlantStage.Seed;
            for (var i = 0; i <= (int)cap; i++)
                if (growth >= v.StageThresholds[i]) stage = (PlantStage)i;
            return stage;
        }

        /// <summary>Care score = goodCareMinutes / aliveMinutes (0 before the first tick).</summary>
        public static float CareScore(Plant p) => p.AliveMinutes == 0 ? 0f : (float)p.GoodCareMinutes / p.AliveMinutes;

        /// <summary>Care grade of a plant.</summary>
        public CareGrade GradeOf(Plant p) => _cfg.GradeFor(CareScore(p));
    }
}
