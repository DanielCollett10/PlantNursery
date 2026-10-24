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
        private readonly List<PhasedSystem> _systems = new List<PhasedSystem>();
        private int _nextEntityId = 1;

        private readonly struct PhasedSystem
        {
            public readonly SystemPhase Phase;
            public readonly ISimSystem System;
            public PhasedSystem(SystemPhase phase, ISimSystem system) { Phase = phase; System = system; }
        }
        private readonly Dictionary<Type, Func<ICommand, SimWorld, CommandResult>> _validators =
            new Dictionary<Type, Func<ICommand, SimWorld, CommandResult>>();
        private readonly Dictionary<Type, Action<ICommand, SimWorld>> _appliers =
            new Dictionary<Type, Action<ICommand, SimWorld>>();

        /// <summary>Number of ticks simulated since the world was created.</summary>
        public long Tick { get; private set; }

        /// <summary>Simulated seconds per tick.</summary>
        public double SecondsPerTick { get; }

        /// <summary>Seed of the world's random source.</summary>
        public int Seed { get; }

        /// <summary>The one seeded random source. Systems must use this, never System.Random.</summary>
        public SimRandom Random { get; }

        public SimWorld(double secondsPerTick = 0.05, int seed = 0)
        {
            if (secondsPerTick <= 0) throw new ArgumentOutOfRangeException(nameof(secondsPerTick));
            SecondsPerTick = secondsPerTick;
            Seed = seed;
            Random = new SimRandom(seed);
        }

        /// <summary>Return the next entity id (1, 2, 3, ...). Ids are never reused.</summary>
        public int NextEntityId() { return _nextEntityId++; }

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

        /// <summary>Add a system in <see cref="SystemPhase.Behaviour"/>.</summary>
        public void AddSystem(ISimSystem system) { AddSystem(system, SystemPhase.Behaviour); }

        /// <summary>
        /// Add a system. Each tick systems update in phase order (Time first, Economy last);
        /// within a phase, in registration order.
        /// </summary>
        public void AddSystem(ISimSystem system, SystemPhase phase)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (!Enum.IsDefined(typeof(SystemPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            // Insert after the last system whose phase is <= this one: keeps the list sorted and stable.
            var i = _systems.Count;
            while (i > 0 && _systems[i - 1].Phase > phase) i--;
            _systems.Insert(i, new PhasedSystem(phase, system));
        }

        /// <summary>Number of systems registered in a phase.</summary>
        public int SystemCount(SystemPhase phase)
        {
            var n = 0;
            foreach (var s in _systems) if (s.Phase == phase) n++;
            return n;
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
            for (var i = 0; i < _systems.Count; i++) _systems[i].System.Update(this);
        }

        /// <summary>
        /// Targets of queued, not yet applied <see cref="ITargetsEntity"/> commands from players other than
        /// <paramref name="excludingPlayer"/>, in submission order. Smart targeting skips these.
        /// </summary>
        public IReadOnlyList<string> PendingTargets(PlayerId excludingPlayer)
        {
            var result = new List<string>();
            foreach (var cmd in _pending)
                if (cmd is ITargetsEntity t && !cmd.PlayerId.Equals(excludingPlayer)) result.Add(t.TargetKey);
            return result;
        }

        private void ApplyPendingCommands()
        {
            // Commands submitted while applying wait for the next tick.
            var count = _pending.Count;
            var used = new HashSet<string>();
            for (var i = 0; i < count; i++)
            {
                var cmd = _pending.Dequeue();
                var result = Validate(cmd);
                var target = cmd as ITargetsEntity;
                if (result.IsAccepted)
                {
                    _appliers[cmd.GetType()](cmd, this);
                    if (target != null) used.Add(target.TargetKey);
                }
                else
                {
                    // An earlier command in this batch used the same target: report Taken, not the generic reason.
                    if (target != null && used.Contains(target.TargetKey))
                        result = CommandResult.Rejected(RejectReason.Taken, "Taken");
                    Raise(new CommandRejectedEvent(cmd, result));
                }
            }
        }
    }
}
