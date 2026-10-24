using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Storage
{
    /// <summary>Take one item from the crate into the player's empty hand, by item id or container entity id.</summary>
    public sealed class TakeFromCrate : ICommand
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Item id to take; null when taking by entity id.</summary>
        public string ItemId { get; }
        /// <summary>Container entity id (when ItemId is null).</summary>
        public int EntityId { get; }
        /// <summary>Take by item id.</summary>
        public TakeFromCrate(PlayerId player, string itemId) { PlayerId = player; ItemId = itemId; }
        /// <summary>Take a specific container entity.</summary>
        public TakeFromCrate(PlayerId player, int entityId) { PlayerId = player; EntityId = entityId; }
    }

    /// <summary>Put the player's held thing back into the crate.</summary>
    public sealed class ReturnToCrate : ICommand
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Creates the command.</summary>
        public ReturnToCrate(PlayerId player) { PlayerId = player; }
    }

    /// <summary>Raised when a player takes something from the crate.</summary>
    public sealed class TookFromCrate : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Item id taken.</summary>
        public string ItemId { get; }
        /// <summary>Creates the event.</summary>
        public TookFromCrate(PlayerId player, string itemId) { PlayerId = player; ItemId = itemId; }
    }

    /// <summary>Raised when a player returns something to the crate.</summary>
    public sealed class ReturnedToCrate : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Item id returned.</summary>
        public string ItemId { get; }
        /// <summary>Creates the event.</summary>
        public ReturnedToCrate(PlayerId player, string itemId) { PlayerId = player; ItemId = itemId; }
    }

    /// <summary>Wires TakeFromCrate / ReturnToCrate to a <see cref="HandsV2"/>.</summary>
    public static class CrateCommands
    {
        /// <summary>Register both handlers.</summary>
        public static void Register(SimWorld world, HandsV2 hands)
        {
            world.RegisterHandler<TakeFromCrate>(new TakeH(hands));
            world.RegisterHandler<ReturnToCrate>(new ReturnH(hands));
        }

        private static CommandResult ToResult(CrateRejection r)
        {
            switch (r)
            {
                case CrateRejection.None: return CommandResult.Accepted();
                case CrateRejection.NotInCrate: return CommandResult.Rejected(RejectReason.InvalidTarget, r.ToString());
                default: return CommandResult.Rejected(RejectReason.NotAllowed, r.ToString());
            }
        }

        private sealed class TakeH : ICommandHandler<TakeFromCrate>
        {
            private readonly HandsV2 _h;
            public TakeH(HandsV2 h) { _h = h; }

            public CommandResult Validate(TakeFromCrate c, SimWorld w) =>
                ToResult(c.ItemId != null ? _h.CanTake(c.PlayerId, c.ItemId) : _h.CanTake(c.PlayerId, c.EntityId));

            public void Apply(TakeFromCrate c, SimWorld w)
            {
                var r = c.ItemId != null ? _h.Take(c.PlayerId, c.ItemId) : _h.Take(c.PlayerId, c.EntityId);
                if (r == CrateRejection.None) w.Raise(new TookFromCrate(c.PlayerId, _h.GetHeld(c.PlayerId).ItemId));
            }
        }

        private sealed class ReturnH : ICommandHandler<ReturnToCrate>
        {
            private readonly HandsV2 _h;
            public ReturnH(HandsV2 h) { _h = h; }
            public CommandResult Validate(ReturnToCrate c, SimWorld w) => ToResult(_h.CanReturn(c.PlayerId));

            public void Apply(ReturnToCrate c, SimWorld w)
            {
                var held = _h.GetHeld(c.PlayerId);
                if (_h.Return(c.PlayerId) == CrateRejection.None) w.Raise(new ReturnedToCrate(c.PlayerId, held.ItemId));
            }
        }
    }
}
