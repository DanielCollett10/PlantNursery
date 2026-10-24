using PlantNursery.Sim.Building;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Storage
{
    /// <summary>While holding a carry tray, take the small pot at a slot into the tray (max capacity, else TrayFull).</summary>
    public sealed class TakePotIntoTray : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot holding the pot.</summary>
        public SlotAddress Address { get; }
        /// <inheritdoc />
        public string TargetKey => Address.Key;
        /// <summary>Creates the command.</summary>
        public TakePotIntoTray(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>While holding a carry tray with pots, put its first pot into an empty small planting slot.</summary>
    public sealed class PutPotFromTray : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The planting slot.</summary>
        public SlotAddress Address { get; }
        /// <inheritdoc />
        public string TargetKey => Address.Key;
        /// <summary>Creates the command.</summary>
        public PutPotFromTray(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>Raised when a pot goes into a carry tray.</summary>
    public sealed class PotTakenIntoTray : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot it came from.</summary>
        public SlotAddress Address { get; }
        /// <summary>Creates the event.</summary>
        public PotTakenIntoTray(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    /// <summary>Raised when a pot leaves a carry tray into a slot.</summary>
    public sealed class PotPutFromTray : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot it went into.</summary>
        public SlotAddress Address { get; }
        /// <summary>Creates the event.</summary>
        public PotPutFromTray(PlayerId player, SlotAddress address) { PlayerId = player; Address = address; }
    }

    public sealed partial class SlotContents
    {
        private const string SmallPot = "pot_small";

        private void RegisterTray(SimWorld world)
        {
            world.RegisterHandler<TakePotIntoTray>(new TakeTrayH(this));
            world.RegisterHandler<PutPotFromTray>(new PutTrayH(this));
        }

        private HeldThing HeldTray(PlayerId p) { var h = _hands.GetHeld(p); return h != null && h.Kind == HeldKind.CarryTray ? h : null; }

        private int TrayCapacity(HeldThing tray) =>
            _content != null && _content.ItemDefs.TryGetValue(tray.ItemId, out var d) && d.Capacity > 0 ? d.Capacity : 6;

        private CommandResult CanTakeIntoTray(PlayerId p, SlotAddress a)
        {
            var tray = HeldTray(p);
            if (tray == null) return Fail(RejectReason.NotAllowed, "NoCarryTray");
            if (a.Slot.Size != SmallPot) return Fail(RejectReason.InvalidTarget, "WrongSize");
            var r = a.IsStrip
                ? _strip.CanRemove(a.Slot.BenchId, a.Slot.Size, a.Slot.Col, a.Slot.Row).ToString()
                : _slots.CanRemoveFromSlot(a.Slot.BenchId, a.Slot.Size, a.Slot.Col, a.Slot.Row).ToString();
            if (r != "None") return Fail(RejectReason.InvalidTarget, r);
            var thing = Get(a);
            if (thing == null) return Fail(RejectReason.InvalidTarget, "SlotEmpty");
            if (thing.Kind != HeldKind.Container) return Fail(RejectReason.InvalidTarget, "WrongSize");
            if (tray.Pots.Count >= TrayCapacity(tray)) return Fail(RejectReason.Blocked, "TrayFull");
            return CommandResult.Accepted();
        }

        private CommandResult CanPutFromTray(PlayerId p, SlotAddress a)
        {
            var tray = HeldTray(p);
            if (tray == null) return Fail(RejectReason.NotAllowed, "NoCarryTray");
            if (tray.Pots.Count == 0) return Fail(RejectReason.NotAllowed, "TrayEmpty");
            if (a.IsStrip || a.Slot.Size != SmallPot) return Fail(RejectReason.InvalidTarget, "WrongSize");
            var s = a.Slot;
            var pr = _slots.CanPlaceInSlot(s.BenchId, s.Size, s.Col, s.Row);
            return pr == SlotRejection.None ? CommandResult.Accepted() : Fail(RejectReason.Blocked, pr.ToString());
        }

        private sealed class TakeTrayH : ICommandHandler<TakePotIntoTray>
        {
            private readonly SlotContents _c;
            public TakeTrayH(SlotContents c) { _c = c; }
            public CommandResult Validate(TakePotIntoTray cmd, SimWorld w) => _c.CanTakeIntoTray(cmd.PlayerId, cmd.Address);

            public void Apply(TakePotIntoTray cmd, SimWorld w)
            {
                var a = cmd.Address;
                var s = a.Slot;
                if (a.IsStrip) _c._strip.Remove(s.BenchId, s.Size, s.Col, s.Row);
                else _c._slots.RemoveFromSlot(s.BenchId, s.Size, s.Col, s.Row);
                var thing = _c._things[a.Key];
                _c._things.Remove(a.Key);
                _c.HeldTray(cmd.PlayerId).AddPot(thing.Container);
                w.Raise(new PotTakenIntoTray(cmd.PlayerId, a));
            }
        }

        private sealed class PutTrayH : ICommandHandler<PutPotFromTray>
        {
            private readonly SlotContents _c;
            public PutTrayH(SlotContents c) { _c = c; }
            public CommandResult Validate(PutPotFromTray cmd, SimWorld w) => _c.CanPutFromTray(cmd.PlayerId, cmd.Address);

            public void Apply(PutPotFromTray cmd, SimWorld w)
            {
                var a = cmd.Address;
                var s = a.Slot;
                var pot = _c.HeldTray(cmd.PlayerId).TakeFirstPot();
                _c._slots.PlaceInSlot(s.BenchId, s.Size, s.Col, s.Row);
                _c._things[a.Key] = HeldThing.OfContainer(pot);
                w.Raise(new PotPutFromTray(cmd.PlayerId, a));
            }
        }
    }
}
