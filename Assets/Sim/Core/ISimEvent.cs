namespace PlantNursery.Sim.Core
{
    /// <summary>Marker for something that happened in the sim, collected for the Unity layer to drain.</summary>
    public interface ISimEvent { }

    /// <summary>Raised when a queued command is rejected at apply time.</summary>
    public sealed class CommandRejectedEvent : ISimEvent
    {
        /// <summary>The rejected command.</summary>
        public ICommand Command { get; }
        /// <summary>Why it was rejected.</summary>
        public CommandResult Result { get; }

        public CommandRejectedEvent(ICommand command, CommandResult result)
        {
            Command = command;
            Result = result;
        }
    }
}
