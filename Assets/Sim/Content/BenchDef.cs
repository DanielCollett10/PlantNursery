using System.Collections.Generic;

namespace PlantNursery.Sim.Content
{
    /// <summary>
    /// Planting area and working strip of a bench item (docs/design/building.md, Benches and slots / Working strip).
    /// Units are 0.1 m mini-grid units: <c>u</c> along the bench, <c>v</c> across it.
    /// </summary>
    public sealed class BenchDef
    {
        /// <summary>Planting area length along the bench, in units (<c>planting.lengthUnits</c>).</summary>
        public int PlantingLengthUnits;
        /// <summary>Planting area depth across the bench, in units (<c>planting.depthUnits</c>).</summary>
        public int PlantingDepthUnits;
        /// <summary>Gap in units between the planting area and the back edge; 0 = flush (<c>planting.backOffsetUnits</c>).</summary>
        public int PlantingBackOffsetUnits;
        /// <summary>Working strip depth across the bench, in units, at the front (<c>workStrip.depthUnits</c>). Zero = no strip.</summary>
        public int StripDepthUnits;
        /// <summary>Items the working strip accepts, in file order. Empty when the strip accepts nothing.</summary>
        public IReadOnlyList<StripAccept> StripAccepts;
    }

    /// <summary>One entry of a bench's <c>workStrip.accepts</c>.</summary>
    public sealed class StripAccept
    {
        /// <summary>Item id accepted on the strip.</summary>
        public string ItemId;
        /// <summary>Strip footprint in units: <c>W</c> along the bench, <c>D</c> across (must fit the strip depth).</summary>
        public Size2 StripUnits;
    }
}
