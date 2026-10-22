using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Hands
{
    /// <summary>Sow one cell of a container on the surface grid from the held seed packet.</summary>
    public sealed class Sow : ICommand
    {
        public PlayerId PlayerId { get; }
        public int TrayId { get; }
        public int CellIndex { get; }
        public Sow(PlayerId player, int trayId, int cellIndex) { PlayerId = player; TrayId = trayId; CellIndex = cellIndex; }
    }

    /// <summary>Clear a dead plant from its cell or pot.</summary>
    public sealed class Compost : ICommand
    {
        public PlayerId PlayerId { get; }
        public int PlantId { get; }
        public Compost(PlayerId player, int plantId) { PlayerId = player; PlantId = plantId; }
    }

    /// <summary>Raised when a seed is sown.</summary>
    public sealed class PlantSown : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public int PlantId { get; }
        public int TrayId { get; }
        public int CellIndex { get; }
        public PlantSown(PlayerId player, int plantId, int trayId, int cellIndex)
        { PlayerId = player; PlantId = plantId; TrayId = trayId; CellIndex = cellIndex; }
    }

    /// <summary>Raised when a dead plant is composted.</summary>
    public sealed class PlantComposted : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public int PlantId { get; }
        public PlantComposted(PlayerId player, int plantId) { PlayerId = player; PlantId = plantId; }
    }
}
