using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>
    /// Pot on: move a plant from the container in the player's hands into an empty bigger pot sitting in a planting slot
    /// or on a bench's working strip (plants.md Potting on, building.md Working strip).
    /// </summary>
    public sealed class PotOn : ITargetsEntity
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The slot holding the empty target pot.</summary>
        public SlotAddress Target { get; }
        /// <summary>Cell of the held container to move, or -1 for the first ready cell.</summary>
        public int Cell { get; }
        /// <inheritdoc />
        public string TargetKey => Target.Key;
        /// <summary>Creates the command.</summary>
        public PotOn(PlayerId player, SlotAddress target, int cell = -1) { PlayerId = player; Target = target; Cell = cell; }
    }

    /// <summary>Raised when a plant has been potted on.</summary>
    public sealed class PottedOn : ISimEvent
    {
        /// <summary>The player.</summary>
        public PlayerId PlayerId { get; }
        /// <summary>The plant that moved.</summary>
        public int PlantId { get; }
        /// <summary>Container it left.</summary>
        public int FromContainerId { get; }
        /// <summary>Container it entered.</summary>
        public int ToContainerId { get; }
        /// <summary>Creates the event.</summary>
        public PottedOn(PlayerId player, int plantId, int from, int to)
        { PlayerId = player; PlantId = plantId; FromContainerId = from; ToContainerId = to; }
    }

    public sealed partial class SlotContents
    {
        private StorageCrate _potCrate;
        private IReadOnlyDictionary<int, Plant> _potPlants;

        /// <summary>
        /// Register the <see cref="PotOn"/> handler. Needs the 4-arg constructor (content), the crate that takes the emptied
        /// container and the plant lookup.
        /// </summary>
        public void RegisterPotOn(SimWorld world, StorageCrate crate, IReadOnlyDictionary<int, Plant> plants)
        {
            _potCrate = crate; _potPlants = plants;
            world.RegisterHandler<PotOn>(new PotOnH(this));
        }

        // Size rank: plug trays 0, pots by footprint area.
        private static int SizeRank(ContainerDef d) => d.CellCount > 1 ? 0 : d.AlongUnits * d.AcrossUnits;

        private Plant ChooseSource(PotOn c, Container from)
        {
            Plant best = null;
            foreach (var id in from.PlantIds)
            {
                if (!_potPlants.TryGetValue(id, out var p)) continue;
                if (c.Cell >= 0) { if (p.CellIndex == c.Cell) return p; continue; }
                if (IsReady(p) && (best == null || p.CellIndex < best.CellIndex)) best = p;
            }
            return best;
        }

        private bool IsReady(Plant p)
        {
            if (p.IsDead) return false;
            if (!_content.SpeciesDefs.TryGetValue(p.SpeciesId ?? "", out var sp)) return p.StageProgress >= 1f;
            return p.StageIndex >= sp.Stages.Count - 1 || p.StageProgress >= 1f;
        }

        private CommandResult CheckPotOn(PotOn c, out Plant plant, out HeldThing target, out Container from)
        {
            plant = null; target = null; from = null;
            var held = _hands.GetHeld(c.PlayerId);
            if (held == null || held.Kind != HeldKind.Container || held.Container.PlantIds.Count == 0)
                return Fail(RejectReason.NotAllowed, "NoPlant");
            from = held.Container;
            target = Get(c.Target);
            if (target == null) return Fail(RejectReason.InvalidTarget, "SlotEmpty");
            if (target.Kind != HeldKind.Container) return Fail(RejectReason.InvalidTarget, "NotAPot");
            var to = target.Container;
            if (to.PlantIds.Count > 0) return Fail(RejectReason.Blocked, "PotNotEmpty");
            if (to.Def.CellCount != 1 || SizeRank(to.Def) <= SizeRank(from.Def)) return Fail(RejectReason.Blocked, "PotTooSmall");
            plant = ChooseSource(c, from);
            // Size check needs a plant for the species; with no plant at the chosen cell, report NotReady.
            if (plant == null) return Fail(RejectReason.Blocked, "NotReady");
            if (!OneSizeUp(from.Def, to.Def) && !SpeciesAllows(plant, to.Def.Id)) return Fail(RejectReason.Blocked, "PotTooBig");
            if (!IsReady(plant)) return Fail(RejectReason.Blocked, "NotReady");
            if (c.Target.IsStrip && from.PlantIds.Count > 1) return Fail(RejectReason.NotAllowed, "HandsFull");
            return CommandResult.Accepted();
        }

        private bool OneSizeUp(ContainerDef from, ContainerDef to)
        {
            var fr = SizeRank(from); var tr = SizeRank(to);
            foreach (var d in _content.ContainerDefs.Values)
            {
                if (d.CellCount != 1) continue;
                var r = SizeRank(d);
                if (r > fr && r < tr) return false;
            }
            return true;
        }

        private bool SpeciesAllows(Plant p, string containerId)
        {
            if (!_content.SpeciesDefs.TryGetValue(p.SpeciesId ?? "", out var sp)) return false;
            foreach (var st in sp.Stages)
                foreach (var id in st.AllowedContainers) if (id == containerId) return true;
            return false;
        }

        private sealed class PotOnH : ICommandHandler<PotOn>
        {
            private readonly SlotContents _c;
            public PotOnH(SlotContents c) { _c = c; }

            public CommandResult Validate(PotOn cmd, SimWorld w) => _c.CheckPotOn(cmd, out _, out _, out _);

            public void Apply(PotOn cmd, SimWorld w)
            {
                if (!_c.CheckPotOn(cmd, out var plant, out var target, out var from).IsAccepted) return;
                var to = target.Container;
                from.RemovePlant(plant.Id);
                to.AddPlant(plant.Id);
                plant.ContainerId = to.Def.Id;
                plant.CellIndex = 0;
                var a = cmd.Target;
                if (from.PlantIds.Count == 0)
                {
                    _c._hands.TakeHeld(cmd.PlayerId);
                    _c._potCrate.AddContainer(from);
                }
                if (a.IsStrip)
                {
                    var s = a.Slot;
                    _c._strip.Remove(s.BenchId, s.Size, s.Col, s.Row);
                    _c._things.Remove(a.Key);
                    _c._hands.TryGive(cmd.PlayerId, target);
                }
                w.Raise(new PottedOn(cmd.PlayerId, plant.Id, from.Id, to.Id));
            }
        }
    }
}
