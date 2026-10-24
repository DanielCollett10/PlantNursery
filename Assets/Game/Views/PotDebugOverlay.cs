using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Plants;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// Debug overlay (F1, off by default): shows the plant name, stage and growth % of the pot under the mouse, or N/A
    /// for an empty pot. While on, a fixed "Pot debug ON (F1)" label is drawn in the top-left corner. Hover is forgiving:
    /// the ray test uses slightly inflated bounds, and if it misses, the nearest pot within <see cref="HoverPixels"/> wins.
    /// Reads sim state only and holds no rules. Drawn with IMGUI (debug tool).
    /// </summary>
    public sealed class PotDebugOverlay : MonoBehaviour
    {
        /// <summary>Text of the corner indicator shown while the overlay is on.</summary>
        public const string IndicatorText = "Pot debug ON (F1)";
        /// <summary>Screen-pixel radius for the nearest-pot fallback.</summary>
        public const float HoverPixels = 40f;

        [SerializeField] private SimHost host;
        [SerializeField] private WorldView world;
        [SerializeField] private Camera cam;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private string _text;
        private int _textContainer = -1;
        private int _textPercent = -1;
        private int _textPlant = -1;
        private Vector2 _mouse;

        /// <summary>True while the overlay is shown. Off by default.</summary>
        public bool Visible { get; set; }
        /// <summary>The label currently shown, or null when nothing is hovered (or the overlay is off).</summary>
        public string CurrentText => Visible ? _text : null;
        /// <summary>The corner indicator text, or null while the overlay is off.</summary>
        public string IndicatorLabel => Visible ? IndicatorText : null;

        /// <summary>Wire references (used by the scene generator and tests).</summary>
        public void Configure(SimHost simHost, WorldView worldView, Camera camera)
        {
            host = simHost; world = worldView; cam = camera;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) Visible = !Visible;
            if (!Visible) { _text = null; _textContainer = -1; return; }
            var mouse = Mouse.current;
            if (mouse != null) _mouse = mouse.position.ReadValue();
            Refresh(_mouse);
        }

        /// <summary>Re-evaluate what is under a screen position and update <see cref="CurrentText"/>.</summary>
        public void Refresh(Vector2 screenPos)
        {
            _mouse = screenPos;
            var c = cam != null ? cam : Camera.main;
            if (host == null || !host.IsReady || world == null || c == null) { _text = null; _textContainer = -1; return; }
            var container = world.ContainerAt(c.ScreenPointToRay(screenPos), _renderers)
                            ?? world.ContainerNear(c, screenPos, HoverPixels, _renderers);
            if (container == null) { _text = null; _textContainer = -1; return; }

            Plant plant = null;
            var ids = container.PlantIds;
            for (var i = 0; i < ids.Count; i++)
            {
                var p = host.Nursery.PlantById(ids[i]);
                if (p != null && !p.IsDead) { plant = p; break; }
            }
            var percent = plant != null ? PercentOf(host.Content, plant) : 0;
            var plantId = plant != null ? plant.Id : -1;
            if (_text != null && _textContainer == container.Id && _textPlant == plantId && _textPercent == percent) return;
            _textContainer = container.Id; _textPlant = plantId; _textPercent = percent;
            _text = Format(host.Content, plant, percent);
        }

        /// <summary>The label for a pot: "N/A" when empty, otherwise name, stage and growth %.</summary>
        public static string Format(ContentDb content, Plant plant, int percent)
        {
            if (plant == null) return "N/A";
            var name = plant.SpeciesId ?? plant.VarietyId;
            var stage = plant.Stage.ToString();
            if (content != null && plant.SpeciesId != null && content.SpeciesDefs.TryGetValue(plant.SpeciesId, out var sp))
            {
                name = sp.Name;
                if (plant.StageIndex >= 0 && plant.StageIndex < sp.Stages.Count) stage = sp.Stages[plant.StageIndex].Name;
            }
            return name + "\n" + stage + " - " + percent + "%";
        }

        /// <summary>
        /// Overall growth 0-100: species plants are measured in stage-days (earlier stages plus progress through the current one,
        /// against the total); older variety plants use <see cref="Plant.Growth"/> against the last of the variety's StageThresholds.
        /// </summary>
        public static int PercentOf(ContentDb content, Plant plant)
        {
            if (content == null) return 0;
            float frac;
            if (plant.SpeciesId != null && content.SpeciesDefs.TryGetValue(plant.SpeciesId, out var sp) && sp.Stages.Count > 0)
            {
                float total = 0f, done = 0f;
                for (var i = 0; i < sp.Stages.Count; i++)
                {
                    total += sp.Stages[i].Days;
                    if (i < plant.StageIndex) done += sp.Stages[i].Days;
                    else if (i == plant.StageIndex) done += sp.Stages[i].Days * plant.StageProgress;
                }
                frac = plant.StageIndex >= sp.Stages.Count - 1 ? 1f : (total > 0f ? done / total : 0f);
            }
            else if (content.VarietyDefs.TryGetValue(plant.VarietyId, out var v) && v.StageThresholds != null && v.StageThresholds.Count > 0)
            {
                var max = v.StageThresholds[v.StageThresholds.Count - 1];
                frac = max > 0f ? plant.Growth / max : 0f;
            }
            else return 0;
            return Mathf.Clamp(Mathf.FloorToInt(frac * 100f), 0, 100);
        }

        private void OnGUI()
        {
            if (!Visible) return;
            GUI.Box(new Rect(8f, 8f, 150f, 24f), IndicatorText);
            if (_text == null) return;
            GUI.Box(new Rect(_mouse.x + 16f, Screen.height - _mouse.y + 16f, 180f, 44f), _text);
        }
    }
}
