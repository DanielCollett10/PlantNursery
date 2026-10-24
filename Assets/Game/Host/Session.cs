using PlantNursery.Sim.Core;

namespace PlantNursery.Game
{
    /// <summary>
    /// The Unity-side session: which <see cref="PlayerId"/> this client plays as. Every command the Unity layer
    /// builds takes its player from here, never from a literal, so a networked client can later pass its own id.
    /// Holds no game rules.
    /// </summary>
    public sealed class Session
    {
        /// <summary>Create a session for the local player.</summary>
        /// <param name="localPlayer">The local player's id.</param>
        public Session(PlayerId localPlayer)
        {
            LocalPlayer = localPlayer;
        }

        /// <summary>The local player's id (solo play uses player 0).</summary>
        public PlayerId LocalPlayer { get; }
    }
}
