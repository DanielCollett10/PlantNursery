using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Plants
{
    /// <summary>Raised when a plant's stage changes through growth.</summary>
    public sealed class PlantStageChanged : ISimEvent
    {
        /// <summary>Plant id.</summary>
        public int PlantId { get; }
        /// <summary>Old stage.</summary>
        public PlantStage From { get; }
        /// <summary>New stage.</summary>
        public PlantStage To { get; }
        public PlantStageChanged(int plantId, PlantStage from, PlantStage to) { PlantId = plantId; From = from; To = to; }
    }

    /// <summary>Raised once when a plant's health reaches 0.</summary>
    public sealed class PlantDied : ISimEvent
    {
        /// <summary>Plant id.</summary>
        public int PlantId { get; }
        public PlantDied(int plantId) { PlantId = plantId; }
    }
}
