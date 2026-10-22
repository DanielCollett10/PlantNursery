using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Time
{
    /// <summary>Sleep until the next dayStartMinute. Only allowed in Evening or Night.</summary>
    public sealed class SleepCommand : ICommand
    {
        /// <inheritdoc />
        public PlayerId PlayerId { get; }
        public SleepCommand(PlayerId playerId) { PlayerId = playerId; }
    }

    /// <summary>Handles <see cref="SleepCommand"/>: steps tick by tick to the next day start.</summary>
    public sealed class SleepHandler : ICommandHandler<SleepCommand>
    {
        private readonly GameClock _clock;
        public SleepHandler(GameClock clock) { _clock = clock; }

        /// <inheritdoc />
        public CommandResult Validate(SleepCommand command, SimWorld world) =>
            _clock.CanSleep
                ? CommandResult.Accepted()
                : CommandResult.Rejected(RejectReason.NotTired, "Can only sleep in the evening or at night.");

        /// <inheritdoc />
        public void Apply(SleepCommand command, SimWorld world)
        {
            // The world keeps stepping tick by tick (systems run each tick) until morning.
            world.RunUntil(w => _clock.IsDayStart);
        }
    }
}
