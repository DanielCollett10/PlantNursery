using System;

namespace PlantNursery.Sim.Core
{
    /// <summary>
    /// Deterministic seeded random source (xorshift64*). Same seed gives the same sequence on every platform.
    /// State is one ulong so it can be saved in a snapshot.
    /// </summary>
    public sealed class SimRandom
    {
        private ulong _state;

        /// <summary>Create a generator from a seed.</summary>
        public SimRandom(int seed)
        {
            Seed = seed;
            // SplitMix-style scramble so small seeds (and 0) give a good non-zero state.
            unchecked
            {
                var z = (ulong)(long)seed + 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                _state = z == 0 ? 0x9E3779B97F4A7C15UL : z;
            }
        }

        /// <summary>The seed this generator was created with.</summary>
        public int Seed { get; }

        /// <summary>Next non-negative 31-bit integer.</summary>
        public int Next() { return (int)(NextUInt64() >> 33); }

        /// <summary>Next integer in [0, maxExclusive).</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        /// <summary>Next integer in [minInclusive, maxExclusive).</summary>
        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + (int)(NextUInt64() % (ulong)((long)maxExclusive - minInclusive));
        }

        /// <summary>Next double in [0, 1).</summary>
        public double NextDouble() { return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0); }

        private ulong NextUInt64()
        {
            unchecked
            {
                _state ^= _state >> 12;
                _state ^= _state << 25;
                _state ^= _state >> 27;
                return _state * 0x2545F4914F6CDD1DUL;
            }
        }
    }
}
