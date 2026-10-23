using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Economy
{
    /// <summary>
    /// Supplier purchases (docs/design/sales.md#supplier-m1). Delivery is instant: seed packets land on the
    /// delivery shelf surface, everything else goes to the build inventory. Purchases are all-or-nothing.
    /// </summary>
    public static class SupplierModule
    {
        /// <summary>Item id of seed packets.</summary>
        public const string SeedPacketId = "seed_packet";
        /// <summary>Message of the rejection when the shelf has no room.</summary>
        public const string DeliveryShelfFull = "DeliveryShelfFull";

        /// <summary>Register the BuyItem handler.</summary>
        public static void Register(SimWorld world, ContentDb content, Wallet wallet, BuildState build, SurfaceGrid surface)
        {
            world.RegisterHandler<BuyItem>(new BuyHandler(content, wallet, build, surface));
        }

        private sealed class BuyHandler : ICommandHandler<BuyItem>
        {
            private readonly ContentDb _content;
            private readonly Wallet _wallet;
            private readonly BuildState _build;
            private readonly SurfaceGrid _surface;

            public BuyHandler(ContentDb content, Wallet wallet, BuildState build, SurfaceGrid surface)
            {
                _content = content; _wallet = wallet; _build = build; _surface = surface;
            }

            private CommandResult Check(BuyItem c, out ItemDef def, out long total)
            {
                total = 0;
                if (c.ItemId == null || !_content.ItemDefs.TryGetValue(c.ItemId, out def) || def.Price <= 0)
                {
                    def = null;
                    return CommandResult.Rejected(RejectReason.InvalidArgument, "UnknownItem");
                }
                if (c.Quantity < 1) return CommandResult.Rejected(RejectReason.InvalidArgument, "InvalidQuantity");
                total = (long)def.Price * c.Quantity;
                if (!_wallet.CanAfford(total)) return CommandResult.Rejected(RejectReason.InsufficientFunds, "InsufficientFunds");
                return CommandResult.Accepted();
            }

            public CommandResult Validate(BuyItem c, SimWorld w)
            {
                var r = Check(c, out var def, out _);
                if (!r.IsAccepted || c.ItemId != SeedPacketId) return r;
                // Dry run: place everything, then take it off again so state is unchanged.
                var placed = Deliver(def, c.Quantity);
                if (placed == null) return CommandResult.Rejected(RejectReason.Blocked, DeliveryShelfFull);
                foreach (var p in placed) _surface.Remove(p.Id);
                return r;
            }

            public void Apply(BuyItem c, SimWorld w)
            {
                var def = _content.ItemDefs[c.ItemId];
                if (c.ItemId == SeedPacketId) Deliver(def, c.Quantity);
                else _build.Inventory.Add(c.ItemId, c.Quantity);
                var cost = def.Price * c.Quantity;
                var old = _wallet.Balance;
                _wallet.TrySpend(cost);
                w.Raise(new WalletChanged(old, _wallet.Balance));
                w.Raise(new ItemsPurchased(c.PlayerId, c.ItemId, c.Quantity, cost));
            }

            /// <summary>Place qty packets on the delivery shelf; null (and nothing placed) if they do not all fit.</summary>
            private List<SurfaceItem> Deliver(ItemDef def, int qty)
            {
                if (DeliveryShelf.Find(_build) == null) return null;
                var placed = new List<SurfaceItem>();
                for (var i = 0; i < qty; i++)
                {
                    var spot = DeliveryShelf.FindSpotOnShelf(_build, _surface, def);
                    var item = spot.HasValue ? _surface.Place(def.Id, spot.Value, Rotation.Deg0, out _, def.SeedCount) : null;
                    if (item == null)
                    {
                        foreach (var p in placed) _surface.Remove(p.Id);
                        return null;
                    }
                    placed.Add(item);
                }
                return placed;
            }
        }
    }
}
