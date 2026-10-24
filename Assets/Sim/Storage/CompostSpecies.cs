using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>
    /// Clear a dead plant (compost) so its container is free again (plants.md, Death). Gives nothing.
    /// Rejections: <c>NotFound</c> (no such plant), <c>NotDead</c>.
    /// </summary>
    public sealed class CompostSpecies : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The plant to clear.</summary>
        public int PlantId { get; }
        /// <inheritdoc />
        public string TargetKey => "plantentity:" + PlantId;
        /// <summary>Creates the command.</summary>
        public CompostSpecies(PlayerId player, int plantId) { PlayerId = player; PlantId = plantId; }
    }

    /// <summary>Raised when a dead species plant is cleared.</summary>
    public sealed class SpeciesPlantComposted : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The cleared plant.</summary>
        public int PlantId { get; }
        /// <summary>The container that was freed (0 if it could not be found).</summary>
        public int ContainerId { get; }
        /// <summary>Creates the event.</summary>
        public SpeciesPlantComposted(PlayerId player, int plantId, int containerId)
        { PlayerId = player; PlantId = plantId; ContainerId = containerId; }
    }

    /// <summary>Wires <see cref="CompostSpecies"/> to a nursery.</summary>
    public static class CompostCommands
    {
        /// <summary>Register the handler.</summary>
        public static void Register(SimWorld world, Nursery nursery)
        {
            world.RegisterHandler<CompostSpecies>(new Handler(nursery));
        }

        private sealed class Handler : ICommandHandler<CompostSpecies>
        {
            private readonly Nursery _n;
            public Handler(Nursery n) { _n = n; }

            public CommandResult Validate(CompostSpecies cmd, SimWorld w)
            {
                if (!_n.PlantMap.TryGetValue(cmd.PlantId, out var p))
                    return CommandResult.Rejected(RejectReason.InvalidTarget, "NotFound");
                return p.IsDead ? CommandResult.Accepted() : CommandResult.Rejected(RejectReason.NotAllowed, "NotDead");
            }

            public void Apply(CompostSpecies cmd, SimWorld w)
            {
                if (!Validate(cmd, w).IsAccepted) return;
                var container = _n.FindContainerOfPlant(cmd.PlantId);
                if (container != null) container.RemovePlant(cmd.PlantId);
                _n.PlantMap.Remove(cmd.PlantId);
                w.Raise(new SpeciesPlantComposted(cmd.PlayerId, cmd.PlantId, container != null ? container.Id : 0));
            }
        }
    }
}
