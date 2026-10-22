using System;

namespace PlantNursery.Sim.Core
{
    /// <summary>Identifies a player. The sim never assumes there is only one.</summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        /// <summary>Numeric id.</summary>
        public int Value { get; }

        public PlayerId(int value) { Value = value; }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId p && Equals(p);
        public override int GetHashCode() => Value;
        public override string ToString() => "Player" + Value;
    }
}
