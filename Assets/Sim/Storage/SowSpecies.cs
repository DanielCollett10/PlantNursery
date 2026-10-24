using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>Sow the held seed packet into the container (by entity id) sitting in a bench slot. One click fills a tray or sows one pot.</summary>
    public sealed class SowSpecies : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>Entity id of the container.</summary>
        public int ContainerId { get; }
        /// <inheritdoc />
        public string TargetKey => "container:" + ContainerId;
        /// <summary>Creates the command.</summary>
        public SowSpecies(PlayerId player, int containerId) { PlayerId = player; ContainerId = containerId; }
    }

    /// <summary>Handles <see cref="SowSpecies"/>. Rejections: NoSeedPacket, PacketEmpty, NotInSlot, NotSowable, ContainerFull.</summary>
    public sealed class SowSystem
    {
        private readonly ContentDb _content;
        private readonly HandsV2 _hands;
        private readonly SlotContents _slots;
        private readonly SpeciesPlantFactory _plants;
        private readonly IDictionary<int, Plant> _registry;
        private readonly SowFillStrategies _strategies;

        /// <summary>Creates the system; new plants are added to <paramref name="registry"/> by id.</summary>
        public SowSystem(ContentDb content, HandsV2 hands, SlotContents slots, SpeciesPlantFactory plants, IDictionary<int, Plant> registry)
        {
            _content = content; _hands = hands; _slots = slots; _plants = plants; _registry = registry;
            _strategies = new SowFillStrategies(id => _registry.TryGetValue(id, out var p) ? p : null);
        }

        /// <summary>Register the handler.</summary>
        public void Register(SimWorld world) { world.RegisterHandler<SowSpecies>(new Handler(this)); }

        private Container FindInSlot(int id)
        {
            foreach (var t in _slots.Things)
                if (t.Kind == HeldKind.Container && t.Container.Id == id) return t.Container;
            return null;
        }

        private static CommandResult Fail(RejectReason r, string m) => CommandResult.Rejected(r, m);

        private CommandResult Check(SowSpecies c, out HeldThing packet, out Container container, out IReadOnlyList<int> cells)
        {
            container = null; cells = null;
            packet = _hands.GetHeld(c.PlayerId);
            if (packet == null || packet.Kind != HeldKind.SeedPacket) return Fail(RejectReason.NotAllowed, "NoSeedPacket");
            if (packet.SeedsLeft <= 0) return Fail(RejectReason.NotAllowed, "PacketEmpty");
            container = FindInSlot(c.ContainerId);
            if (container == null) return Fail(RejectReason.InvalidTarget, "NotInSlot");
            var def = _content.ItemDefs[packet.ItemId];
            if (def.SpeciesId == null || !_content.SpeciesDefs.TryGetValue(def.SpeciesId, out var species)
                || species.Sowing == null || !Contains(species.Sowing.SowInto, container.Def.Id))
                return Fail(RejectReason.NotAllowed, "NotSowable");
            cells = _strategies.For(container.Def, species.Sowing.TrayMode).CellsToFill(container, packet.SeedsLeft);
            return cells.Count == 0 ? Fail(RejectReason.Blocked, "ContainerFull") : CommandResult.Accepted();
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            foreach (var s in list) if (s == id) return true;
            return false;
        }

        private sealed class Handler : ICommandHandler<SowSpecies>
        {
            private readonly SowSystem _s;
            public Handler(SowSystem s) { _s = s; }

            public CommandResult Validate(SowSpecies c, SimWorld w) => _s.Check(c, out _, out _, out _);

            public void Apply(SowSpecies c, SimWorld w)
            {
                if (!_s.Check(c, out var packet, out var container, out var cells).IsAccepted) return;
                var def = _s._content.ItemDefs[packet.ItemId];
                foreach (var cell in cells)
                {
                    var plant = _s._plants.Create(def.SpeciesId, def.VarietyId, container.Def.Id, cell);
                    plant.Moisture = container.WaterMeter;
                    container.AddPlant(plant.Id);
                    _s._registry[plant.Id] = plant;
                    packet.SeedsLeft--;
                    w.Raise(new PlantSown(c.PlayerId, plant.Id, container.Id, cell));
                }
                if (packet.SeedsLeft <= 0) _s._hands.TakeHeld(c.PlayerId);
            }
        }
    }
}
