using PlantNursery.Game.Lighting;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Drives the directional sun and flat ambient light from the sim clock. Reads
    /// <see cref="SimHost"/> state only. Between ticks the light glides: the minute shown is the current
    /// tick's minute plus the fraction of the real-time tick that has elapsed. A sleep fast-forward
    /// jumps the sim many minutes in one call, so the next frame shows the morning state directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DayNightLighting : MonoBehaviour
    {
        [SerializeField] private SimHost host;
        [SerializeField] private Light sun;
        [SerializeField] private bool driveAmbient = true;

        private long _lastTick = long.MinValue;
        private float _tickStartTime;

        /// <summary>The sim host to read the clock from. Found automatically when left empty.</summary>
        public SimHost Host { get => host; set => host = value; }

        /// <summary>The directional light that acts as the sun.</summary>
        public Light Sun { get => sun; set => sun = value; }

        private void Awake()
        {
            if (host == null) host = FindFirstObjectByType<SimHost>();
            if (sun == null) sun = FindSunLight();
            if (driveAmbient) RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }

        private void LateUpdate()
        {
            if (host == null || !host.IsReady) return;
            var world = host.World;
            var clock = host.Clock;

            if (world.Tick != _lastTick)
            {
                _lastTick = world.Tick;
                _tickStartTime = Time.unscaledTime;
            }

            var fraction = 0f;
            if (!world.IsPaused)
            {
                var perTick = clock.RealSecondsPerTick;
                if (perTick > 0.0)
                    fraction = Mathf.Clamp01((float)((Time.unscaledTime - _tickStartTime) / perTick));
            }

            var state = DayLightingCurve.Evaluate(clock.MinuteAt(world.Tick) + fraction);
            Apply(state);
        }

        private void Apply(SunState state)
        {
            if (sun != null)
            {
                sun.color = state.Colour;
                sun.intensity = state.Intensity;
                sun.transform.rotation = Quaternion.Euler(-state.ElevationDeg, state.YawDeg, 0f);
            }
            if (driveAmbient)
                RenderSettings.ambientLight = Color.Lerp(
                    new Color(0.10f, 0.12f, 0.20f), state.Colour * 0.55f, state.Intensity);
        }

        private static Light FindSunLight()
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) return light;
            return null;
        }
    }
}
