using System;

namespace PlantNursery.Sim.Core
{
    /// <summary>
    /// In-process <see cref="ICommandSink"/> that forwards every command straight to a <see cref="SimWorld"/>
    /// queue. Used for solo play and tests; applied on the world's next tick (or <see cref="SimWorld.ApplyPending"/>).
    /// </summary>
    public sealed class LoopbackCommandSink : ICommandSink
    {
        private readonly SimWorld _world;

        /// <summary>Create a loopback sink that submits to <paramref name="world"/>.</summary>
        /// <param name="world">The world that receives the commands.</param>
        public LoopbackCommandSink(SimWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>Forward the command to <see cref="SimWorld.Submit"/>.</summary>
        /// <param name="command">The command to submit. Must not be null.</param>
        public void Submit(ICommand command)
        {
            _world.Submit(command);
        }
    }
}
