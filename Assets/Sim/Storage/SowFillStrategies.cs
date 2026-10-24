using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>
    /// The fill rule of one sowing click: which cells of a container get a plant (plants.md, Sowing).
    /// A new tray mode or container kind adds a class and a line in <see cref="SowFillStrategies"/>, not a branch in the command.
    /// </summary>
    public interface ISowFillStrategy
    {
        /// <summary>Cells to sow, in order, given the seeds available. Empty when the container is full.</summary>
        IReadOnlyList<int> CellsToFill(Container container, int seedsAvailable);
    }

    /// <summary>Pots: one plant in cell 0.</summary>
    public sealed class SinglePlantFill : ISowFillStrategy
    {
        /// <inheritdoc />
        public IReadOnlyList<int> CellsToFill(Container container, int seedsAvailable) =>
            container.PlantIds.Count > 0 || seedsAvailable < 1 ? new int[0] : new[] { 0 };
    }

    /// <summary>Plug trays, one seed per cell: every empty cell, until the seeds run out.</summary>
    public sealed class PerCellFill : ISowFillStrategy
    {
        /// <summary>Needs the plants to know which cells are taken.</summary>
        private readonly System.Func<int, Plant> _plantOf;

        /// <summary>Creates the strategy; <paramref name="plantOf"/> looks a plant up by id.</summary>
        public PerCellFill(System.Func<int, Plant> plantOf) { _plantOf = plantOf; }

        /// <inheritdoc />
        public IReadOnlyList<int> CellsToFill(Container container, int seedsAvailable)
        {
            var taken = new HashSet<int>();
            foreach (var id in container.PlantIds)
            {
                var p = _plantOf(id);
                if (p != null) taken.Add(p.CellIndex);
            }
            var cells = new List<int>();
            for (var i = 0; i < container.Def.CellCount && cells.Count < seedsAvailable; i++)
                if (!taken.Contains(i)) cells.Add(i);
            return cells;
        }
    }

    /// <summary>Plug trays of a species that covers the whole tray (Hearthmoss): one plant, cell 0, uses one seed.</summary>
    public sealed class WholeTrayFill : ISowFillStrategy
    {
        /// <inheritdoc />
        public IReadOnlyList<int> CellsToFill(Container container, int seedsAvailable) =>
            container.PlantIds.Count > 0 || seedsAvailable < 1 ? new int[0] : new[] { 0 };
    }

    /// <summary>Picks the fill strategy for a container and a species' tray mode.</summary>
    public sealed class SowFillStrategies
    {
        private readonly ISowFillStrategy _pot = new SinglePlantFill();
        private readonly Dictionary<TrayMode, ISowFillStrategy> _trayModes;

        /// <summary>Creates the table; <paramref name="plantOf"/> looks a plant up by id.</summary>
        public SowFillStrategies(System.Func<int, Plant> plantOf)
        {
            _trayModes = new Dictionary<TrayMode, ISowFillStrategy>
            {
                { TrayMode.PerCell, new PerCellFill(plantOf) },
                { TrayMode.WholeTray, new WholeTrayFill() }
            };
        }

        /// <summary>Strategy for this container (a single cell is a pot; more cells is a tray).</summary>
        public ISowFillStrategy For(ContainerDef def, TrayMode mode) =>
            def.CellCount <= 1 ? _pot : _trayModes[mode];
    }
}
