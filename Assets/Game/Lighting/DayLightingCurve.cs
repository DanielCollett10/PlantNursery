using UnityEngine;

namespace PlantNursery.Game.Lighting
{
    /// <summary>Sun pose and colour for one moment of the day.</summary>
    public struct SunState
    {
        /// <summary>Sun height above the horizon in degrees (negative at night).</summary>
        public float ElevationDeg;
        /// <summary>Compass direction the light travels, in degrees around the vertical axis.</summary>
        public float YawDeg;
        /// <summary>Light colour.</summary>
        public Color Colour;
        /// <summary>Light intensity multiplier, 0..1.</summary>
        public float Intensity;
    }

    /// <summary>
    /// Pure presentation curve: maps the sim's minute of day to a sun pose and colour.
    /// Warm morning, neutral day, orange evening, blue night (docs/design/visuals.md#lighting).
    /// Periodic over 24 hours. No game rules live here: it only decides how the day looks.
    /// </summary>
    public static class DayLightingCurve
    {
        /// <summary>Minutes in a day.</summary>
        public const float MinutesPerDay = 1440f;

        /// <summary>Lowest intensity (moonlit night).</summary>
        public const float NightIntensity = 0.12f;

        private struct ColourKey
        {
            public readonly float Hour;
            public readonly Color Colour;
            public ColourKey(float hour, int rgb)
            {
                Hour = hour;
                Colour = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
            }
        }

        // Hour of day -> colour, linearly blended. The 24:00 key repeats midnight so the curve wraps.
        private static readonly ColourKey[] Keys =
        {
            new ColourKey(0f, 0x2B3A67),     // night blue
            new ColourKey(5f, 0x3C4F8A),     // pre-dawn blue
            new ColourKey(7f, 0xFFB27A),     // warm morning
            new ColourKey(10f, 0xFFF1E0),    // neutral day
            new ColourKey(15f, 0xFFF6EC),    // neutral afternoon
            new ColourKey(17f, 0xFF9B5C),    // orange evening
            new ColourKey(18.5f, 0xFF8A4C),  // deep orange dusk
            new ColourKey(20.5f, 0x6A7FBF),  // dusk blue
            new ColourKey(22f, 0x2B3A67),    // night blue
            new ColourKey(24f, 0x2B3A67),    // wrap to midnight
        };

        /// <summary>Sun pose and colour at a minute of the day (any float; wrapped into 0..1440).</summary>
        public static SunState Evaluate(float minuteOfDay)
        {
            var hour = WrapMinutes(minuteOfDay) / 60f;
            var elevation = 60f * Mathf.Cos(2f * Mathf.PI * (hour - 12f) / 24f);
            return new SunState
            {
                ElevationDeg = elevation,
                YawDeg = 15f * (hour - 12f),
                Colour = ColourAt(hour),
                Intensity = Mathf.Lerp(NightIntensity, 1f, Mathf.SmoothStep(-8f, 10f, elevation)),
            };
        }

        /// <summary>Wraps any minute value into the range [0, 1440).</summary>
        public static float WrapMinutes(float minuteOfDay)
        {
            var m = minuteOfDay - MinutesPerDay * Mathf.Floor(minuteOfDay / MinutesPerDay);
            return m >= MinutesPerDay ? 0f : m;
        }

        /// <summary>Blended light colour for an hour in 0..24.</summary>
        public static Color ColourAt(float hour)
        {
            for (var i = 0; i < Keys.Length - 1; i++)
            {
                var a = Keys[i];
                var b = Keys[i + 1];
                if (hour < a.Hour || hour > b.Hour) continue;
                var span = b.Hour - a.Hour;
                var t = span <= 0f ? 0f : (hour - a.Hour) / span;
                return Color.Lerp(a.Colour, b.Colour, t);
            }
            return Keys[0].Colour;
        }
    }
}
