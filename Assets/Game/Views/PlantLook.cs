using PlantNursery.Sim.Content;
using PlantNursery.Sim.Plants;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Pure mapping from a plant's sim state to how it looks (visuals.md): tint from the colour phenotype,
    /// scale from the stage, droop when wilted, brown-grey when dead. No rules; only maths.
    /// </summary>
    public readonly struct PlantLook
    {
        /// <summary>Amber glow (#FFB300).</summary>
        public static readonly Color Amber = new Color(1f, 0xB3 / 255f, 0f);
        /// <summary>Teal glow (#1ABC9C).</summary>
        public static readonly Color Teal = new Color(0x1A / 255f, 0xBC / 255f, 0x9C / 255f);
        /// <summary>Violet glow (#8E44AD).</summary>
        public static readonly Color Violet = new Color(0x8E / 255f, 0x44 / 255f, 0xAD / 255f);
        /// <summary>White bloom (#F2F2F2).</summary>
        public static readonly Color White = new Color(0xF2 / 255f, 0xF2 / 255f, 0xF2 / 255f);
        /// <summary>Blue bloom (#3B7DDD).</summary>
        public static readonly Color Blue = new Color(0x3B / 255f, 0x7D / 255f, 0xDD / 255f);
        /// <summary>Rose bloom (#E75480).</summary>
        public static readonly Color Rose = new Color(0xE7 / 255f, 0x54 / 255f, 0x80 / 255f);

        private static readonly string[] ColourGenes = { "glowColour", "bloomColour", "leafColour" };

        /// <summary>Leaf/body tint after wilt and death.</summary>
        public Color Tint { get; }
        /// <summary>Uniform scale, 0.1 (first stage) to 1 (final stage).</summary>
        public float Scale { get; }
        /// <summary>Droop amount 0-1 (0 upright, 1 flopped).</summary>
        public float Droop { get; }
        /// <summary>True when the plant is dead.</summary>
        public bool Dead { get; }

        /// <summary>Build a look from raw values.</summary>
        public PlantLook(Color tint, float scale, float droop, bool dead)
        {
            Tint = tint; Scale = scale; Droop = droop; Dead = dead;
        }

        /// <summary>Base colour for the first colour gene the plant has (glowColour, bloomColour, leafColour); green otherwise.</summary>
        public static Color BaseColour(Plant plant)
        {
            if (plant == null || plant.Genome == null) return WorldLayout.LeafGreen;
            foreach (var gene in ColourGenes)
            {
                string allele;
                try { allele = plant.Genome.MendelianPhenotype(gene); }
                catch (System.Exception) { continue; }
                return ColourFor(allele);
            }
            return WorldLayout.LeafGreen;
        }

        /// <summary>Colour for a phenotype allele name; unknown gives leaf green.</summary>
        public static Color ColourFor(string allele)
        {
            switch (allele)
            {
                case "amber": return Amber;
                case "teal": return Teal;
                case "violet": return Violet;
                case "white": return White;
                case "blue": return Blue;
                case "rose": return Rose;
                default: return WorldLayout.LeafColour(allele);
            }
        }

        /// <summary>Scale by stage position: first stage 0.1, last stage 1.</summary>
        public static float ScaleFor(int stageIndex, int stageCount)
        {
            if (stageCount <= 1) return 1f;
            return 0.1f + 0.9f * Mathf.Clamp01(stageIndex / (float)(stageCount - 1));
        }

        /// <summary>
        /// Look of a plant. <paramref name="stageCount"/> is the species' stage count (0 for old-variety plants, which
        /// use the six-step <see cref="PlantStage"/> enum).
        /// </summary>
        public static PlantLook From(Plant plant, int stageCount)
        {
            var scale = stageCount > 0 ? ScaleFor(plant.StageIndex, stageCount) : WorldLayout.PlantScale(plant.Stage);
            var droop = plant.IsDead ? 1f
                : plant.IsSpeciesPlant ? (plant.IsWilted ? 1f : 0f)
                : WorldLayout.WiltAmount(plant.Moisture);
            return new PlantLook(WorldLayout.Tinted(BaseColour(plant), droop, plant.IsDead), scale, droop, plant.IsDead);
        }
    }

    /// <summary>
    /// Pure mapping from a container's water meter to its gauge and dry cue (plants.md, Water feedback):
    /// the gauge shows below 30, the dry cue at 0.
    /// </summary>
    public readonly struct ContainerLook
    {
        /// <summary>Gauge shows below this meter value.</summary>
        public const float GaugeBelow = 30f;

        /// <summary>True when the gauge is visible.</summary>
        public bool GaugeVisible { get; }
        /// <summary>Gauge fill 0-1.</summary>
        public float GaugeFill { get; }
        /// <summary>True at meter 0 (clear dry cue).</summary>
        public bool Dry { get; }

        private ContainerLook(bool visible, float fill, bool dry)
        {
            GaugeVisible = visible; GaugeFill = fill; Dry = dry;
        }

        /// <summary>Look for a water meter value 0-100.</summary>
        public static ContainerLook From(float waterMeter)
            => new ContainerLook(waterMeter < GaugeBelow, Mathf.Clamp01(waterMeter / 100f), waterMeter <= 0f);

        /// <summary>Look of a container.</summary>
        public static ContainerLook From(Container container) => From(container.WaterMeter);
    }
}
