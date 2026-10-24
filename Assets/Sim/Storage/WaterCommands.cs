using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Storage
{
    /// <summary>Wires <see cref="WaterContainer"/> and <see cref="RefillCan"/> to a nursery's hands, containers and floor.</summary>
    public static class WaterCommands
    {
        /// <summary>Register both handlers.</summary>
        public static void Register(SimWorld world, Nursery nursery, ContentDb content)
        {
            world.RegisterHandler<WaterContainer>(new WaterHandler(nursery));
            world.RegisterHandler<RefillCan>(new RefillHandler(nursery, content));
        }

        private static CommandResult Reject(RejectReason reason, string message) => CommandResult.Rejected(reason, message);

        /// <summary>The held can, or null if the player holds no can.</summary>
        private static HeldThing HeldCan(Nursery n, PlayerId player)
        {
            var h = n.Hands.GetHeld(player);
            return h != null && h.Kind == HeldKind.Can ? h : null;
        }

        private sealed class WaterHandler : ICommandHandler<WaterContainer>
        {
            private readonly Nursery _n;
            public WaterHandler(Nursery n) { _n = n; }

            public CommandResult Validate(WaterContainer cmd, SimWorld w)
            {
                var can = HeldCan(_n, cmd.PlayerId);
                if (can == null) return Reject(RejectReason.NotAllowed, "NoCan");
                var container = _n.FindContainer(cmd.ContainerId);
                if (container == null) return Reject(RejectReason.InvalidTarget, "NotFound");
                if (can.Charges < container.Def.WateringCost) return Reject(RejectReason.NotAllowed, "CanEmpty");
                return CommandResult.Accepted();
            }

            public void Apply(WaterContainer cmd, SimWorld w)
            {
                var can = HeldCan(_n, cmd.PlayerId);
                var container = _n.FindContainer(cmd.ContainerId);
                can.Charges -= container.Def.WateringCost;
                container.WaterMeter = 100f;
                w.Raise(new ContainerWatered(cmd.PlayerId, cmd.ContainerId));
            }
        }

        private sealed class RefillHandler : ICommandHandler<RefillCan>
        {
            private readonly Nursery _n;
            private readonly ContentDb _content;
            public RefillHandler(Nursery n, ContentDb content) { _n = n; _content = content; }

            public CommandResult Validate(RefillCan cmd, SimWorld w)
            {
                if (HeldCan(_n, cmd.PlayerId) == null) return Reject(RejectReason.NotAllowed, "NoCan");
                var placed = _n.Build.Get(cmd.BarrelId);
                if (placed == null || placed.ItemId != RefillCan.RainBarrelItemId)
                    return Reject(RejectReason.InvalidTarget, "NotABarrel");
                return CommandResult.Accepted();
            }

            public void Apply(RefillCan cmd, SimWorld w)
            {
                var can = HeldCan(_n, cmd.PlayerId);
                can.Charges = _content.ItemDefs[can.ItemId].WaterCapacity;
            }
        }
    }
}
