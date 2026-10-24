using PlantNursery.Sim.Core;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Something the player can use when standing close. Features implement this and call
    /// <see cref="WorldInteractions.Register"/>; <see cref="WorldInteractions"/> never needs editing to add one.
    /// Implementations hold no game rules: they send a command and report what the Sim said.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>False while the interactable should be ignored (not ready, destroyed).</summary>
        bool IsAvailable { get; }

        /// <summary>UiStrings key of the action name shown in the prompt (for example "prompt.sleep").</summary>
        string PromptKey { get; }

        /// <summary>Horizontal distance in metres from a world point to the nearest part of this interactable.</summary>
        float DistanceTo(Vector3 point);

        /// <summary>Use it. Returns the Sim's answer (a rejection message is shown to the player).</summary>
        CommandResult Interact(SimHost host);
    }
}
