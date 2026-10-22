namespace PlantNursery.Sim.Core
{
    /// <summary>Handles one command type. Registered with <see cref="SimWorld.RegisterHandler{T}"/>.</summary>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        /// <summary>Check the command without changing any state.</summary>
        CommandResult Validate(TCommand command, SimWorld world);

        /// <summary>Apply an already validated command. May raise events.</summary>
        void Apply(TCommand command, SimWorld world);
    }

    /// <summary>A system updated once per tick, after commands are applied.</summary>
    public interface ISimSystem
    {
        /// <summary>Update for one tick. May raise events via <see cref="SimWorld.Raise"/>.</summary>
        void Update(SimWorld world);
    }
}
