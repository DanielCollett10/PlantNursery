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
        /// <summary>Species stage index before the change (-1 for old-variety plants).</summary>
        public int FromIndex { get; }
        /// <summary>Species stage index after the change (-1 for old-variety plants).</summary>
        public int ToIndex { get; }
        /// <summary>Species stage name after the change (null for old-variety plants).</summary>
        public string StageName { get; }
        /// <summary>Creates the event for an old-variety plant.</summary>
        public PlantStageChanged(int plantId, PlantStage from, PlantStage to) { PlantId = plantId; From = from; To = to; FromIndex = -1; ToIndex = -1; }
        /// <summary>Creates the event for a species plant.</summary>
        public PlantStageChanged(int plantId, PlantStage from, PlantStage to, int fromIndex, int toIndex, string stageName)
            : this(plantId, from, to) { FromIndex = fromIndex; ToIndex = toIndex; StageName = stageName; }
    }

    /// <summary>Raised once when a plant's health reaches 0.</summary>
    public sealed class PlantDied : ISimEvent
    {
        /// <summary>Plant id.</summary>
        public int PlantId { get; }
        public PlantDied(int plantId) { PlantId = plantId; }
    }
}
