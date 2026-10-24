using System;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>
    /// Scrape the moss in a container into the player's empty hands as one bundle (species.md, Hearthmoss).
    /// Rejections: <c>HandsFull</c>, <c>NotFound</c>, <c>NotHarvestable</c>.
    /// </summary>
    public sealed class ScrapeMoss : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Entity id of the container holding the moss.</summary>
        public int ContainerId { get; }
        /// <inheritdoc />
        public string TargetKey => "container:" + ContainerId;
        /// <summary>Creates the command.</summary>
        public ScrapeMoss(PlayerId player, int containerId) { PlayerId = player; ContainerId = containerId; }
    }

    /// <summary>Raised when moss is scraped.</summary>
    public sealed class MossScraped : ISimEvent
    {
        /// <summary>The player who scraped.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The container entity id.</summary>
        public int ContainerId { get; }
        /// <summary>The plant id.</summary>
        public int PlantId { get; }
        /// <summary>Quality of the bundle.</summary>
        public QualityGrade Quality { get; }
        /// <summary>Creates the event.</summary>
        public MossScraped(PlayerId player, int containerId, int plantId, QualityGrade quality)
        { PlayerId = player; ContainerId = containerId; PlantId = plantId; Quality = quality; }
    }

    /// <summary>Wires <see cref="ScrapeMoss"/> to a nursery.</summary>
    public static class MossCommands
    {
        /// <summary>The item id of a Hearthmoss bundle.</summary>
        public const string BundleItemId = "bundle_hearthmoss";

        /// <summary>Register the handler. <paramref name="plantById"/> looks plants up (null if unknown).</summary>
        public static void Register(SimWorld world, Nursery nursery, ContentDb content, Func<int, Plant> plantById)
        {
            world.RegisterHandler<ScrapeMoss>(new Handler(nursery, content, plantById));
        }

        private sealed class Handler : ICommandHandler<ScrapeMoss>
        {
            private readonly Nursery _n;
            private readonly ContentDb _content;
            private readonly Func<int, Plant> _plantById;

            public Handler(Nursery n, ContentDb content, Func<int, Plant> plantById)
            { _n = n; _content = content; _plantById = plantById; }

            private Plant FindMoss(ScrapeMoss cmd, out Container container)
            {
                container = _n.FindContainer(cmd.ContainerId);
                if (container == null) return null;
                foreach (var id in container.PlantIds)
                {
                    var p = _plantById(id);
                    if (MossRules.IsHarvestable(p, _content)) return p;
                }
                return null;
            }

            public CommandResult Validate(ScrapeMoss cmd, SimWorld w)
            {
                if (_n.Hands.GetHeld(cmd.PlayerId) != null)
                    return CommandResult.Rejected(RejectReason.NotAllowed, "HandsFull");
                if (_n.FindContainer(cmd.ContainerId) == null)
                    return CommandResult.Rejected(RejectReason.InvalidTarget, "NotFound");
                return FindMoss(cmd, out _) == null
                    ? CommandResult.Rejected(RejectReason.NotAllowed, "NotHarvestable")
                    : CommandResult.Accepted();
            }

            public void Apply(ScrapeMoss cmd, SimWorld w)
            {
                var plant = FindMoss(cmd, out _);
                var quality = MossRules.QualityOf(plant, _content.Economy);
                var bundle = new HeldThing(HeldKind.Bundle, BundleItemId) { Quality = quality };
                _n.Hands.TryGive(cmd.PlayerId, bundle);
                plant.RegrowTicksLeft = MossRules.RegrowTicks(plant, _content.SpeciesDefs[plant.SpeciesId], GameTicksPerDay);
                w.Raise(new MossScraped(cmd.PlayerId, cmd.ContainerId, plant.Id, quality));
            }

            private const int GameTicksPerDay = PlantNursery.Sim.Time.GameClock.MinutesPerDay;
        }
    }
}
