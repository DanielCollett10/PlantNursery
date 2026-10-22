using System;

namespace PlantNursery.Sim.Economy
{
    /// <summary>The shared coin balance of one nursery. Never goes below zero.</summary>
    public sealed class Wallet
    {
        /// <summary>Current coins.</summary>
        public int Balance { get; private set; }

        /// <summary>Creates a wallet with the given starting coins (from content).</summary>
        public Wallet(int startingMoney)
        {
            if (startingMoney < 0) throw new ArgumentOutOfRangeException(nameof(startingMoney));
            Balance = startingMoney;
        }

        /// <summary>True if the wallet holds at least <paramref name="amount"/> coins.</summary>
        public bool CanAfford(long amount) => amount >= 0 && Balance >= amount;

        /// <summary>Spend coins. Returns false and changes nothing if the wallet cannot cover it.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Balance < amount) return false;
            Balance -= amount;
            return true;
        }

        /// <summary>Add coins.</summary>
        public void Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Balance = checked(Balance + amount);
        }
    }
}
