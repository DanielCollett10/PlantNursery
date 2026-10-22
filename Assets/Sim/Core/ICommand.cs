namespace PlantNursery.Sim.Core
{
    /// <summary>A player action submitted to the sim. Concrete commands are plain data classes.</summary>
    public interface ICommand
    {
        /// <summary>The player issuing the command.</summary>
        PlayerId PlayerId { get; }
    }
}
