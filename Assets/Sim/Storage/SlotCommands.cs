using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>Address of a bench slot: a planting slot or a working-strip slot (strip slots are sized by item id).</summary>
    public readonly struct SlotAddress
    {
        /// <summary>True for a working-strip slot, false for a planting slot.</summary>
        public readonly bool IsStrip;
        /// <summary>The bench, size (container id, or item id on the strip), column and row.</summary>
        public readonly SlotRef Slot;

        /// <summary>Creates an address.</summary>
        public SlotAddress(bool isStrip, SlotRef slot) { IsStrip = isStrip; Slot = slot; }

        /// <summary>A planting slot address.</summary>
        public static SlotAddress Planting(int benchId, string containerId, int col, int row) =>
            new SlotAddress(false, new SlotRef(benchId, containerId, col, row));

        /// <summary>A working-strip slot address.</summary>
        public static SlotAddress Strip(int benchId, string itemId, int col, int row) =>
            new SlotAddress(true, new SlotRef(benchId, itemId, col, row));

        /// <summary>Stable key used for first-request-wins and pending claims.</summary>
        public string Key => (IsStrip ? "strip:" : "plant:") + Slot;
    }

    /// <summary>Take whatever sits at a slot into the player's empty hand.</summary>
    public sealed class PickUpFromSlot : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot.</summary>
        public SlotAddress Address { get; }
        /// <inheritdoc />
        public string TargetKey => Address.Key;
        /// <summary>Creates the command.</summary>
        public PickUpFromSlot(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>Put the player's held thing into a slot.</summary>
    public sealed class PutInSlot : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot.</summary>
        public SlotAddress Address { get; }
        /// <inheritdoc />
        public string TargetKey => Address.Key;
        /// <summary>Creates the command.</summary>
        public PutInSlot(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>Raised when a player takes a thing from a slot.</summary>
    public sealed class PickedUpFromSlot : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot.</summary>
        public SlotAddress Address { get; }
        /// <summary>Creates the event.</summary>
        public PickedUpFromSlot(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>Raised when a player puts a thing into a slot.</summary>
    public sealed class PutInSlotDone : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot.</summary>
        public SlotAddress Address { get; }
        /// <summary>Creates the event.</summary>
        public PutInSlotDone(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>Things sitting in bench slots, with the PickUpFromSlot / PutInSlot handlers using SlotOccupancy and StripOccupancy rules.</summary>
    public sealed class SlotContents
    {
        private readonly SlotOccupancy _slots;
        private readonly StripOccupancy _strip;
        private readonly HandsV2 _hands;
        private readonly Dictionary<string, HeldThing> _things = new Dictionary<string, HeldThing>();

        /// <summary>Creates slot contents over the occupancy maps and hands.</summary>
        public SlotContents(SlotOccupancy slots, StripOccupancy strip, HandsV2 hands)
        {
            _slots = slots; _strip = strip; _hands = hands;
        }

        /// <summary>What sits exactly at this address, or null.</summary>
        public HeldThing Get(SlotAddress a) => _things.TryGetValue(a.Key, out var t) ? t : null;

        /// <summary>Register both handlers.</summary>
        public void Register(SimWorld world)
        {
            world.RegisterHandler<PickUpFromSlot>(new PickH(this));
            world.RegisterHandler<PutInSlot>(new PutH(this));
        }

        private CommandResult CanPick(PlayerId p, SlotAddress a)
        {
            if (_hands.GetHeld(p) != null) return Fail(RejectReason.NotAllowed, "HandsFull");
            var r = a.IsStrip
                ? _strip.CanRemove(a.Slot.BenchId, a.Slot.Size, a.Slot.Col, a.Slot.Row).ToString()
                : _slots.CanRemoveFromSlot(a.Slot.BenchId, a.Slot.Size, a.Slot.Col, a.Slot.Row).ToString();
            if (r != "None") return Fail(RejectReason.InvalidTarget, r);
            return Get(a) == null ? Fail(RejectReason.InvalidTarget, "SlotEmpty") : CommandResult.Accepted();
        }

        private CommandResult CanPut(PlayerId p, SlotAddress a)
        {
            var held = _hands.GetHeld(p);
            if (held == null) return Fail(RejectReason.NotAllowed, "NothingHeld");
            var s = a.Slot;
            if (a.IsStrip)
            {
                if (held.ItemId != s.Size) return Fail(RejectReason.InvalidTarget, "NotAccepted");
                var contents = held.Kind == HeldKind.Container && held.Container.PlantIds.Count > 0
                    ? StripContents.HoldsPlant : StripContents.NoPlants;
                var r = _strip.CanPlace(s.BenchId, s.Size, s.Col, s.Row, contents);
                return r == StripRejection.None ? CommandResult.Accepted() : Fail(RejectReason.Blocked, r.ToString());
            }
            if (held.Kind != HeldKind.Container || held.Container.Def.Id != s.Size)
                return Fail(RejectReason.InvalidTarget, "WrongSize");
            var pr = _slots.CanPlaceInSlot(s.BenchId, s.Size, s.Col, s.Row);
            return pr == SlotRejection.None ? CommandResult.Accepted() : Fail(RejectReason.Blocked, pr.ToString());
        }

        private static CommandResult Fail(RejectReason reason, string message) => CommandResult.Rejected(reason, message);

        private sealed class PickH : ICommandHandler<PickUpFromSlot>
        {
            private readonly SlotContents _c;
            public PickH(SlotContents c) { _c = c; }
            public CommandResult Validate(PickUpFromSlot cmd, SimWorld w) => _c.CanPick(cmd.PlayerId, cmd.Address);

            public void Apply(PickUpFromSlot cmd, SimWorld w)
            {
                var a = cmd.Address;
                var s = a.Slot;
                if (a.IsStrip) _c._strip.Remove(s.BenchId, s.Size, s.Col, s.Row);
                else _c._slots.RemoveFromSlot(s.BenchId, s.Size, s.Col, s.Row);
                var thing = _c._things[a.Key];
                _c._things.Remove(a.Key);
                _c._hands.TryGive(cmd.PlayerId, thing);
                w.Raise(new PickedUpFromSlot(cmd.PlayerId, a));
            }
        }

        private sealed class PutH : ICommandHandler<PutInSlot>
        {
            private readonly SlotContents _c;
            public PutH(SlotContents c) { _c = c; }
            public CommandResult Validate(PutInSlot cmd, SimWorld w) => _c.CanPut(cmd.PlayerId, cmd.Address);

            public void Apply(PutInSlot cmd, SimWorld w)
            {
                var a = cmd.Address;
                var s = a.Slot;
                var held = _c._hands.TakeHeld(cmd.PlayerId);
                if (a.IsStrip)
                {
                    var contents = held.Kind == HeldKind.Container && held.Container.PlantIds.Count > 0
                        ? StripContents.HoldsPlant : StripContents.NoPlants;
                    _c._strip.Place(s.BenchId, s.Size, s.Col, s.Row, contents);
                }
                else _c._slots.PlaceInSlot(s.BenchId, s.Size, s.Col, s.Row);
                _c._things[a.Key] = held;
                w.Raise(new PutInSlotDone(cmd.PlayerId, a));
            }
        }
    }
}
