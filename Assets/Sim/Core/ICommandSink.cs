namespace PlantNursery.Sim.Core
{
    /// <summary>
    /// The single entry point for player commands. Input in the Unity layer submits through a sink
    /// and never calls <see cref="SimWorld"/> directly. A local loopback applies commands in-process;
    /// a network transport can implement the same contract later.
    /// </summary>
    public interface ICommandSink
    {
        /// <summary>
        /// Hand a command to the simulation. The command keeps its <see cref="ICommand.PlayerId"/>;
        /// commands are applied in the order they were submitted.
        /// </summary>
        /// <param name="command">The command to submit. Must not be null.</param>
        void Submit(ICommand command);
    }
}
