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
        private string _diag;
        private bool _textNearest;
        private Transform _player;
        private readonly List<Vector2> _candidates = new List<Vector2>();

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
            if (!Visible) { _text = null; _textContainer = -1; _diag = null; return; }
            var mouse = Mouse.current;
            if (mouse != null) _mouse = mouse.position.ReadValue();
            Refresh(_mouse);
        }

        /// <summary>Corner diagnostics line (mouse, screen size, camera pixelRect, hovered container), or null when off.</summary>
        public string DiagnosticsLine => Visible ? _diag : null;
        /// <summary>True when the current label is the nearest-pot fallback rather than a hover hit.</summary>
        public bool ShowingNearest { get; private set; }

        /// <summary>
        /// Try each candidate screen position in order and return the first non-null probe result (and the position that hit).
        /// Pure helper so the hover fallback order can be tested without a scene.
        /// </summary>
        public static T ResolveHover<T>(IList<Vector2> candidates, System.Func<Vector2, T> probe, out Vector2 used) where T : class
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var r = probe(candidates[i]);
                if (r != null) { used = candidates[i]; return r; }
            }
            used = candidates.Count > 0 ? candidates[0] : Vector2.zero;
            return null;
        }

        /// <summary>Rescale a position from the screen into the camera's pixel rect (handles Game view scale / odd viewports).</summary>
        public static Vector2 ToCameraPixels(Vector2 pos, Rect pixelRect, float screenW, float screenH)
        {
            if (screenW <= 0f || screenH <= 0f) return pos;
            return new Vector2(pixelRect.x + pos.x / screenW * pixelRect.width, pixelRect.y + pos.y / screenH * pixelRect.height);
        }

        private static string Fmt(Vector2 v) => "(" + v.x.ToString("0") + "," + v.y.ToString("0") + ")";

        /// <summary>Re-evaluate what is under a screen position and update <see cref="CurrentText"/>.</summary>
        public void Refresh(Vector2 screenPos)
        {
            _mouse = screenPos;
            ShowingNearest = false;
            var c = cam != null ? cam : Camera.main;
            if (host == null || !host.IsReady || world == null || c == null)
            {
                _text = null; _textContainer = -1;
                _diag = "mouse " + Fmt(screenPos) + "  (overlay not wired / host not ready)";
                return;
            }
            _candidates.Clear();
            _candidates.Add(screenPos);
#if ENABLE_LEGACY_INPUT_MANAGER
            _candidates.Add((Vector2)Input.mousePosition);
#endif
            _candidates.Add(ToCameraPixels(screenPos, c.pixelRect, Screen.width, Screen.height));
            var container = ResolveHover(_candidates, p => world.ContainerAt(c.ScreenPointToRay(p), _renderers)
                                                         ?? world.ContainerNear(c, p, HoverPixels, _renderers), out var used);
            var hovered = container != null;
            if (!hovered)
            {
                if (_player == null)
                {
                    var go = GameObject.Find("Player");
                    if (go != null) _player = go.transform;
                }
                if (_player != null) container = world.ContainerNearestTo(_player.position);
                ShowingNearest = container != null;
            }
            _diag = "mouse " + Fmt(screenPos) + "  screen " + Screen.width + "x" + Screen.height + "  pixelRect " + c.pixelRect
                    + "  hover " + (hovered ? "#" + container.Id + " via " + Fmt(used) : "none");
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
            if (_text != null && _textContainer == container.Id && _textPlant == plantId && _textPercent == percent
                && _textNearest == ShowingNearest) return;
            _textContainer = container.Id; _textPlant = plantId; _textPercent = percent; _textNearest = ShowingNearest;
            _text = Format(host.Content, plant, percent) + (ShowingNearest ? "\n(nearest)" : "");
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
            if (_diag != null) GUI.Box(new Rect(8f, 36f, Mathf.Min(Screen.width - 16f, 760f), 24f), _diag);
            if (_text == null) return;
            var rect = ShowingNearest
                ? new Rect(8f, 66f, 180f, 60f)
                : new Rect(_mouse.x + 16f, Screen.height - _mouse.y + 16f, 180f, 44f);
            GUI.Box(rect, _text);
        }
    }
}
