using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Economy
{
    /// <summary>
    /// The BuyToCrate path (economy.md, Storage crate): <c>BuyItem</c> delivers pots and trays as container entities
    /// and packets as counted stock into the unlimited <see cref="StorageCrate"/>. Floor items still go to the
    /// <see cref="BuildInventory"/>. Use instead of <see cref="SupplierModule.Register"/> (the old delivery-shelf
    /// path, decision 0011, which stays as it is); registering both throws.
    /// </summary>
    public static class CrateSupplier
    {
        /// <summary>Register the crate-delivering BuyItem handler.</summary>
        public static void Register(SimWorld world, ContentDb content, Wallet wallet, BuildState build,
            StorageCrate crate, EntityFactory factory)
        {
            world.RegisterHandler<BuyItem>(new Handler(content, wallet, build, crate, factory));
        }

        private sealed class Handler : ICommandHandler<BuyItem>
        {
            private readonly ContentDb _content;
            private readonly Wallet _wallet;
            private readonly BuildState _build;
            private readonly StorageCrate _crate;
            private readonly EntityFactory _factory;

            public Handler(ContentDb content, Wallet wallet, BuildState build, StorageCrate crate, EntityFactory factory)
            {
                _content = content; _wallet = wallet; _build = build; _crate = crate; _factory = factory;
            }

            public CommandResult Validate(BuyItem c, SimWorld w)
            {
                if (c.ItemId == null || !_content.ItemDefs.TryGetValue(c.ItemId, out var def) || def.Price <= 0)
                    return CommandResult.Rejected(RejectReason.InvalidArgument, "UnknownItem");
                if (c.Quantity < 1) return CommandResult.Rejected(RejectReason.InvalidArgument, "InvalidQuantity");
                if (!_wallet.CanAfford((long)def.Price * c.Quantity))
                    return CommandResult.Rejected(RejectReason.InsufficientFunds, "InsufficientFunds");
                return CommandResult.Accepted();
            }

            public void Apply(BuyItem c, SimWorld w)
            {
                var def = _content.ItemDefs[c.ItemId];
                if (def.Layer == ItemLayer.Floor)
                    _build.Inventory.Add(c.ItemId, c.Quantity);
                else
                    for (var i = 0; i < c.Quantity; i++)
                    {
                        var container = _factory.CreateContainerForItem(c.ItemId);
                        if (container != null) _crate.AddContainer(container);
                        else _crate.AddStock(c.ItemId, 1);
                    }
                var cost = def.Price * c.Quantity;
                var old = _wallet.Balance;
                _wallet.TrySpend(cost);
                w.Raise(new WalletChanged(old, _wallet.Balance));
                w.Raise(new ItemsPurchased(c.PlayerId, c.ItemId, c.Quantity, cost));
            }
        }
    }
}
