using System;
using System.Collections.Generic;

namespace PlantNursery.Sim.Core
{
    /// <summary>
    /// Root of the simulation. Owns the game clock and, later, all plant, building and economy state.
    /// Pure C#: no UnityEngine. The Unity layer calls <see cref="Step"/> on a fixed timestep
    /// and reads state to render it.
    /// </summary>
    public sealed class SimWorld
    {
        private readonly Queue<ICommand> _pending = new Queue<ICommand>();
        private readonly List<ISimEvent> _events = new List<ISimEvent>();
        private readonly List<ISimSystem> _systems = new List<ISimSystem>();
        private readonly Dictionary<Type, Func<ICommand, SimWorld, CommandResult>> _validators =
            new Dictionary<Type, Func<ICommand, SimWorld, CommandResult>>();
        private readonly Dictionary<Type, Action<ICommand, SimWorld>> _appliers =
            new Dictionary<Type, Action<ICommand, SimWorld>>();

        /// <summary>Number of ticks simulated since the world was created.</summary>
        public long Tick { get; private set; }

        /// <summary>Simulated seconds per tick.</summary>
        public double SecondsPerTick { get; }

        public SimWorld(double secondsPerTick = 0.05)
        {
            if (secondsPerTick <= 0) throw new ArgumentOutOfRangeException(nameof(secondsPerTick));
            SecondsPerTick = secondsPerTick;
        }

        /// <summary>Total simulated time in seconds.</summary>
        public double SimulatedSeconds => Tick * SecondsPerTick;

        /// <summary>True while the simulation is paused; <see cref="Step"/> does nothing.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Pause the simulation. Idempotent.</summary>
        public void Pause() { IsPaused = true; }

        /// <summary>Resume a paused simulation. Idempotent.</summary>
        public void Resume() { IsPaused = false; }

        /// <summary>Number of commands waiting for the next tick.</summary>
        public int PendingCommandCount => _pending.Count;

        /// <summary>Register the handler for a command type. One handler per type.</summary>
        public void RegisterHandler<T>(ICommandHandler<T> handler) where T : ICommand
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_validators.ContainsKey(typeof(T)))
                throw new InvalidOperationException("Handler already registered for " + typeof(T).Name);
            _validators[typeof(T)] = (c, w) => handler.Validate((T)c, w);
            _appliers[typeof(T)] = (c, w) => handler.Apply((T)c, w);
        }

        /// <summary>Add a system, updated each tick in registration order.</summary>
        public void AddSystem(ISimSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            _systems.Add(system);
        }

        /// <summary>Queue a command; it is applied at the start of the next tick, in submission order.</summary>
        public void Submit(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _pending.Enqueue(command);
        }

        /// <summary>
        /// Apply all queued commands now, without advancing the tick. Lets the host make player commands
        /// take effect at once instead of waiting for the next (possibly seconds-long) tick.
        /// </summary>
        public void ApplyPending() { ApplyPendingCommands(); }

        /// <summary>Report whether the command would be accepted now. Changes no state.</summary>
        public CommandResult Validate(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!_validators.TryGetValue(command.GetType(), out var v))
                return CommandResult.Rejected(RejectReason.UnknownCommand, "No handler for " + command.GetType().Name);
            return v(command, this);
        }

        /// <summary>Raise an event for the Unity layer to drain.</summary>
        public void Raise(ISimEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            _events.Add(evt);
        }

        /// <summary>Return all events raised since the last drain and clear the list.</summary>
        public IReadOnlyList<ISimEvent> DrainEvents()
        {
            var result = _events.ToArray();
            _events.Clear();
            return result;
        }

        /// <summary>Advance the simulation by <paramref name="ticks"/> ticks. No-op while paused.</summary>
        public void Step(int ticks = 1)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (IsPaused) return;
            for (var i = 0; i < ticks; i++) StepOne();
            // A command (e.g. sleep) may ask to keep stepping until a condition holds.
            while (_runUntil != null)
            {
                if (_runUntil(this)) { _runUntil = null; break; }
                StepOne();
            }
        }

        private Func<SimWorld, bool> _runUntil;

        /// <summary>
        /// Keep stepping tick by tick, at the end of the current <see cref="Step"/>, until
        /// <paramref name="done"/> returns true. Used by commands such as Sleep.
        /// </summary>
        public void RunUntil(Func<SimWorld, bool> done)
        {
            _runUntil = done ?? throw new ArgumentNullException(nameof(done));
        }

        private void StepOne()
        {
            ApplyPendingCommands();
            Tick++;
            foreach (var system in _systems) system.Update(this);
        }

        private void ApplyPendingCommands()
        {
            // Commands submitted while applying wait for the next tick.
            var count = _pending.Count;
            for (var i = 0; i < count; i++)
            {
                var cmd = _pending.Dequeue();
                var result = Validate(cmd);
                if (result.IsAccepted) _appliers[cmd.GetType()](cmd, this);
                else Raise(new CommandRejectedEvent(cmd, result));
            }
        }
    }
}
