using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Economy
{
    /// <summary>Buy <see cref="Quantity"/> of an item from the supplier with the shared wallet.</summary>
    public sealed class BuyItem : ICommand
    {
        /// <summary>The buying player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Item definition id.</summary>
        public string ItemId { get; }
        /// <summary>How many to buy (at least 1).</summary>
        public int Quantity { get; }
        /// <summary>Creates the command.</summary>
        public BuyItem(PlayerId player, string itemId, int quantity)
        {
            PlayerId = player; ItemId = itemId; Quantity = quantity;
        }
    }

    /// <summary>Raised whenever the wallet balance changes.</summary>
    public sealed class WalletChanged : ISimEvent
    {
        /// <summary>Balance before.</summary>
        public int OldBalance { get; }
        /// <summary>Balance after.</summary>
        public int NewBalance { get; }
        /// <summary>Creates the event.</summary>
        public WalletChanged(int oldBalance, int newBalance) { OldBalance = oldBalance; NewBalance = newBalance; }
    }

    /// <summary>Raised after a purchase is delivered.</summary>
    public sealed class ItemsPurchased : ISimEvent
    {
        /// <summary>The buying player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Item bought.</summary>
        public string ItemId { get; }
        /// <summary>How many.</summary>
        public int Quantity { get; }
        /// <summary>Total coins spent.</summary>
        public int TotalCost { get; }
        /// <summary>Creates the event.</summary>
        public ItemsPurchased(PlayerId player, string itemId, int quantity, int totalCost)
        {
            PlayerId = player; ItemId = itemId; Quantity = quantity; TotalCost = totalCost;
        }
    }
}
