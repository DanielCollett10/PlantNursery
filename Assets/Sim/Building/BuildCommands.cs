using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Building
{
    /// <summary>Place an item from the build inventory.</summary>
    public sealed class PlaceItem : ICommand
    {
        public PlayerId PlayerId { get; }
        public string ItemId { get; }
        public ItemLayer Layer { get; }
        public Cell Anchor { get; }
        public Rotation Rotation { get; }
        public PlaceItem(PlayerId player, string itemId, ItemLayer layer, Cell anchor, Rotation rotation)
        {
            PlayerId = player; ItemId = itemId; Layer = layer; Anchor = anchor; Rotation = rotation;
        }
    }

    /// <summary>Pick a placed item up into the build inventory.</summary>
    public sealed class PickUpItem : ICommand
    {
        public PlayerId PlayerId { get; }
        public int PlacedId { get; }
        public PickUpItem(PlayerId player, int placedId) { PlayerId = player; PlacedId = placedId; }
    }

    /// <summary>Move a placed floor item; a bench keeps its pots and strip items in the same slots.</summary>
    public sealed class MoveItem : ICommand
    {
        public PlayerId PlayerId { get; }
        public int PlacedId { get; }
        public Cell Anchor { get; }
        public Rotation Rotation { get; }
        public MoveItem(PlayerId player, int placedId, Cell anchor, Rotation rotation)
        {
            PlayerId = player; PlacedId = placedId; Anchor = anchor; Rotation = rotation;
        }
    }

    /// <summary>Raised when an item is moved.</summary>
    public sealed class ItemMoved : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public PlacedItem Item { get; }
        public PlacedItem From { get; }
        public ItemMoved(PlayerId player, PlacedItem item, PlacedItem from) { PlayerId = player; Item = item; From = from; }
    }

    /// <summary>Raised when an item is placed.</summary>
    public sealed class ItemPlaced : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public PlacedItem Item { get; }
        public ItemPlaced(PlayerId player, PlacedItem item) { PlayerId = player; Item = item; }
    }

    /// <summary>Raised when an item is picked up.</summary>
    public sealed class ItemPickedUp : ISimEvent
    {
        public PlayerId PlayerId { get; }
        public PlacedItem Item { get; }
        public ItemPickedUp(PlayerId player, PlacedItem item) { PlayerId = player; Item = item; }
    }

    /// <summary>Wires the building commands to a <see cref="BuildState"/>.</summary>
    public static class BuildingModule
    {
        /// <summary>Register PlaceItem and PickUpItem handlers on the world.</summary>
        public static void Register(SimWorld world, BuildState state)
        {
            world.RegisterHandler<PlaceItem>(new PlaceHandler(state));
            world.RegisterHandler<PickUpItem>(new PickUpHandler(state));
            world.RegisterHandler<MoveItem>(new MoveHandler(state));
        }

        /// <summary>Map a building rejection to a command result; the message is the reason name.</summary>
        public static CommandResult ToResult(BuildRejection r)
        {
            switch (r)
            {
                case BuildRejection.None: return CommandResult.Accepted();
                case BuildRejection.NotOwned: return CommandResult.Rejected(RejectReason.NotAllowed, r.ToString());
                case BuildRejection.Occupied:
                case BuildRejection.HasItemsOnTop: return CommandResult.Rejected(RejectReason.Blocked, r.ToString());
                case BuildRejection.OutOfPlot:
                case BuildRejection.UnknownPlacedItem: return CommandResult.Rejected(RejectReason.InvalidTarget, r.ToString());
                default: return CommandResult.Rejected(RejectReason.InvalidArgument, r.ToString());
            }
        }

        private sealed class PlaceHandler : ICommandHandler<PlaceItem>
        {
            private readonly BuildState _s;
            public PlaceHandler(BuildState s) { _s = s; }
            public CommandResult Validate(PlaceItem c, SimWorld w) => ToResult(_s.CanPlace(c.ItemId, c.Layer, c.Anchor, c.Rotation));
            public void Apply(PlaceItem c, SimWorld w)
            {
                var item = _s.Place(c.ItemId, c.Layer, c.Anchor, c.Rotation, out _);
                if (item != null) w.Raise(new ItemPlaced(c.PlayerId, item));
            }
        }

        private sealed class MoveHandler : ICommandHandler<MoveItem>
        {
            private readonly BuildState _s;
            public MoveHandler(BuildState s) { _s = s; }
            public CommandResult Validate(MoveItem c, SimWorld w) => ToResult(_s.CanMove(c.PlacedId, c.Anchor, c.Rotation));
            public void Apply(MoveItem c, SimWorld w)
            {
                var from = _s.Get(c.PlacedId);
                var item = _s.Move(c.PlacedId, c.Anchor, c.Rotation, out _);
                if (item != null) w.Raise(new ItemMoved(c.PlayerId, item, from));
            }
        }

        private sealed class PickUpHandler : ICommandHandler<PickUpItem>
        {
            private readonly BuildState _s;
            public PickUpHandler(BuildState s) { _s = s; }
            public CommandResult Validate(PickUpItem c, SimWorld w) => ToResult(_s.CanPickUp(c.PlacedId));
            public void Apply(PickUpItem c, SimWorld w)
            {
                var item = _s.PickUp(c.PlacedId, out _);
                if (item != null) w.Raise(new ItemPickedUp(c.PlayerId, item));
            }
        }
    }
}
