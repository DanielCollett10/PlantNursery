using PlantNursery.Sim.Building;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Hands
{
    /// <summary>Pick up a surface item (by surface item id) into the empty hand.</summary>
    public sealed class PickUp : ICommand
    {
        public PlayerId PlayerId { get; }
        public int ItemId { get; }
        public PickUp(PlayerId player, int itemId) { PlayerId = player; ItemId = itemId; }
    }

    /// <summary>Put the held item down on the surface mini-grid.</summary>
    public sealed class PutDown : ICommand
    {
        public PlayerId PlayerId { get; }
        public Unit Anchor { get; }
        public Rotation Rotation { get; }
        public PutDown(PlayerId player, Unit anchor, Rotation rotation) { PlayerId = player; Anchor = anchor; Rotation = rotation; }
    }

    /// <summary>Refill the held watering can at the tap.</summary>
    public sealed class RefillCan : ICommand
    {
        public PlayerId PlayerId { get; }
        public RefillCan(PlayerId player) { PlayerId = player; }
    }

    /// <summary>Water a plant with the held can.</summary>
    public sealed class WaterPlant : ICommand
    {
        public PlayerId PlayerId { get; }
        public int PlantId { get; }
        public WaterPlant(PlayerId player, int plantId) { PlayerId = player; PlantId = plantId; }
    }

    /// <summary>Raised when a player picks up an item.</summary>
    public sealed class HandPickedUp : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public int ItemId { get; }
        public HandPickedUp(PlayerId player, int itemId) { PlayerId = player; ItemId = itemId; }
    }

    /// <summary>Raised when a player puts an item down.</summary>
    public sealed class HandPutDown : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public SurfaceItem Item { get; }
        public HandPutDown(PlayerId player, SurfaceItem item) { PlayerId = player; Item = item; }
    }

    /// <summary>Raised when a plant is watered.</summary>
    public sealed class PlantWatered : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public int PlantId { get; }
        public PlantWatered(PlayerId player, int plantId) { PlayerId = player; PlantId = plantId; }
    }

    /// <summary>Wires the hands commands to a <see cref="HandsState"/>.</summary>
    public static class HandsModule
    {
        /// <summary>Register all hands handlers.</summary>
        public static void Register(SimWorld world, HandsState s)
        {
            world.RegisterHandler<PickUp>(new PickUpH(s));
            world.RegisterHandler<PutDown>(new PutDownH(s));
            world.RegisterHandler<RefillCan>(new RefillH(s));
            world.RegisterHandler<WaterPlant>(new WaterH(s));
        }

        /// <summary>Map a rejection to a result; the message is the rejection name.</summary>
        public static CommandResult ToResult(HandsRejection r)
        {
            switch (r)
            {
                case HandsRejection.None: return CommandResult.Accepted();
                case HandsRejection.NotHoldable:
                case HandsRejection.UnknownPlant:
                case HandsRejection.NoSurface: return CommandResult.Rejected(RejectReason.InvalidTarget, r.ToString());
                case HandsRejection.Occupied: return CommandResult.Rejected(RejectReason.Blocked, r.ToString());
                default: return CommandResult.Rejected(RejectReason.NotAllowed, r.ToString());
            }
        }

        private sealed class PickUpH : ICommandHandler<PickUp>
        {
            private readonly HandsState _s;
            public PickUpH(HandsState s) { _s = s; }
            public CommandResult Validate(PickUp c, SimWorld w) => ToResult(_s.CanPickUp(c.PlayerId, c.ItemId));
            public void Apply(PickUp c, SimWorld w)
            {
                if (_s.PickUp(c.PlayerId, c.ItemId) == HandsRejection.None) w.Raise(new HandPickedUp(c.PlayerId, c.ItemId));
            }
        }

        private sealed class PutDownH : ICommandHandler<PutDown>
        {
            private readonly HandsState _s;
            public PutDownH(HandsState s) { _s = s; }
            public CommandResult Validate(PutDown c, SimWorld w) => ToResult(_s.CanPutDown(c.PlayerId, c.Anchor, c.Rotation));
            public void Apply(PutDown c, SimWorld w)
            {
                var item = _s.PutDown(c.PlayerId, c.Anchor, c.Rotation, out _);
                if (item != null) w.Raise(new HandPutDown(c.PlayerId, item));
            }
        }

        private sealed class RefillH : ICommandHandler<RefillCan>
        {
            private readonly HandsState _s;
            public RefillH(HandsState s) { _s = s; }
            public CommandResult Validate(RefillCan c, SimWorld w) => ToResult(_s.CanRefill(c.PlayerId));
            public void Apply(RefillCan c, SimWorld w) { _s.RefillCan(c.PlayerId); }
        }

        private sealed class WaterH : ICommandHandler<WaterPlant>
        {
            private readonly HandsState _s;
            public WaterH(HandsState s) { _s = s; }
            public CommandResult Validate(WaterPlant c, SimWorld w) => ToResult(_s.CanWater(c.PlayerId, c.PlantId));
            public void Apply(WaterPlant c, SimWorld w)
            {
                if (_s.WaterPlant(c.PlayerId, c.PlantId) == HandsRejection.None) w.Raise(new PlantWatered(c.PlayerId, c.PlantId));
            }
        }
    }
}
