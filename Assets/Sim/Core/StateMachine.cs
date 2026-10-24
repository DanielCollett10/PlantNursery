using System;
using System.Collections.Generic;

namespace PlantNursery.Sim.Core
{
    /// <summary>
    /// A small table-driven state machine for modes such as plant lifecycle and mood.
    /// Pure C#: no UnityEngine, no randomness, no clock.
    /// <para>
    /// The owner keeps the current state as plain data (<see cref="Current"/>, a value type) and
    /// restores it with <see cref="Restore"/> when loading. The machine itself holds the transition
    /// table, guards and callback, which are configuration, so it is not part of serialised entity state.
    /// </para>
    /// <para>
    /// Transitions are only the entries added with <see cref="Allow"/>. Anything else (including a
    /// transition to the same state) is <see cref="TransitionResult.Illegal"/>. A guard is evaluated only
    /// when its entry exists. A transition that is not applied changes nothing and invokes no callback.
    /// </para>
    /// </summary>
    /// <typeparam name="TState">State type, usually an enum.</typeparam>
    public sealed class StateMachine<TState>
    {
        private readonly Dictionary<TState, Dictionary<TState, Func<bool>>> _table =
            new Dictionary<TState, Dictionary<TState, Func<bool>>>();
        private readonly Action<TState, TState> _onTransition;

        /// <summary>The current state. Plain data, safe to save.</summary>
        public TState Current { get; private set; }

        /// <summary>
        /// Create a machine in <paramref name="initial"/>.
        /// </summary>
        /// <param name="initial">The starting state.</param>
        /// <param name="onTransition">
        /// Called as (from, to) after each applied transition, once <see cref="Current"/> is updated.
        /// The owner uses it to raise its own event. May be null.
        /// </param>
        public StateMachine(TState initial, Action<TState, TState> onTransition = null)
        {
            Current = initial;
            _onTransition = onTransition;
        }

        /// <summary>
        /// Add an allowed transition. A null <paramref name="guard"/> means always allowed.
        /// Returns this machine so entries can be chained.
        /// </summary>
        /// <exception cref="InvalidOperationException">The same from/to pair was already added.</exception>
        public StateMachine<TState> Allow(TState from, TState to, Func<bool> guard = null)
        {
            if (!_table.TryGetValue(from, out var edges))
            {
                edges = new Dictionary<TState, Func<bool>>();
                _table[from] = edges;
            }
            if (edges.ContainsKey(to))
                throw new InvalidOperationException("Transition " + from + " -> " + to + " is already defined.");
            edges[to] = guard;
            return this;
        }

        /// <summary>
        /// Report whether a transition to <paramref name="to"/> would be applied now.
        /// Changes no state and invokes no callback. Guards are evaluated.
        /// </summary>
        public TransitionResult Check(TState to)
        {
            if (!_table.TryGetValue(Current, out var edges) || !edges.TryGetValue(to, out var guard))
                return TransitionResult.Illegal;
            if (guard != null && !guard()) return TransitionResult.GuardFailed;
            return TransitionResult.Applied;
        }

        /// <summary>
        /// Move to <paramref name="to"/> if the table allows it and the guard passes.
        /// On success, <see cref="Current"/> is updated and the callback is called with (from, to).
        /// Otherwise the state is unchanged and the returned value says why it was refused.
        /// </summary>
        public TransitionResult TryTransition(TState to)
        {
            var result = Check(to);
            if (result != TransitionResult.Applied) return result;

            var from = Current;
            Current = to;
            _onTransition?.Invoke(from, to);
            return TransitionResult.Applied;
        }

        /// <summary>
        /// Set the current state without checking the table, guards or callback.
        /// Used when loading a saved state. Not for gameplay changes.
        /// </summary>
        public void Restore(TState state)
        {
            Current = state;
        }
    }
}
