using NUnit.Framework;
using PlantNursery.Game.Lighting;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    public sealed class DayLightingCurveTests
    {
        private const float Hour = 60f;

        [Test]
        public void SunIsHighestAtNoonAndBelowHorizonAtMidnight()
        {
            Assert.Greater(DayLightingCurve.Evaluate(12 * Hour).ElevationDeg, 50f);
            Assert.Less(DayLightingCurve.Evaluate(0f).ElevationDeg, -50f);
            Assert.Less(DayLightingCurve.Evaluate(0f).Intensity, 0.2f);
            Assert.Greater(DayLightingCurve.Evaluate(12 * Hour).Intensity, 0.95f);
        }

        [Test]
        public void MorningIsWarmerThanNoon()
        {
            var morning = DayLightingCurve.Evaluate(7 * Hour).Colour;
            var noon = DayLightingCurve.Evaluate(12 * Hour).Colour;
            Assert.Greater(morning.r - morning.b, noon.r - noon.b + 0.1f);
        }

        [Test]
        public void NoonIsNearNeutral()
        {
            var noon = DayLightingCurve.Evaluate(12 * Hour).Colour;
            Assert.That(noon.r - noon.b, Is.LessThan(0.15f));
            Assert.That(noon.r - noon.b, Is.GreaterThan(-0.15f));
        }

        [Test]
        public void EveningIsOrange()
        {
            var evening = DayLightingCurve.Evaluate(18 * Hour).Colour;
            Assert.Greater(evening.r, evening.g);
            Assert.Greater(evening.g, evening.b);
            Assert.Greater(evening.r - evening.b, 0.4f);
        }

        [Test]
        public void NightIsBlue()
        {
            var night = DayLightingCurve.Evaluate(1 * Hour).Colour;
            Assert.Greater(night.b, night.r);
        }

        [Test]
        public void CurveIsPeriodicOverADay()
        {
            var start = DayLightingCurve.Evaluate(0f);
            var end = DayLightingCurve.Evaluate(DayLightingCurve.MinutesPerDay);
            Assert.That(end.ElevationDeg, Is.EqualTo(start.ElevationDeg).Within(0.001f));
            Assert.That(end.Colour.b, Is.EqualTo(start.Colour.b).Within(0.001f));
            Assert.That(DayLightingCurve.Evaluate(-30f).ElevationDeg,
                Is.EqualTo(DayLightingCurve.Evaluate(DayLightingCurve.MinutesPerDay - 30f).ElevationDeg).Within(0.001f));
        }

        [Test]
        public void FractionalMinutesChangeSmoothly()
        {
            // A tenth of a minute must move the colour and sun only a little, including across midnight.
            foreach (var minute in new[] { 719.9f, 419.95f, DayLightingCurve.MinutesPerDay - 0.05f })
            {
                var a = DayLightingCurve.Evaluate(minute);
                var b = DayLightingCurve.Evaluate(minute + 0.1f);
                Assert.That(Mathf.Abs(a.ElevationDeg - b.ElevationDeg), Is.LessThan(0.1f));
                Assert.That(Mathf.Abs(a.Colour.r - b.Colour.r), Is.LessThan(0.01f));
                Assert.That(Mathf.Abs(a.Colour.b - b.Colour.b), Is.LessThan(0.01f));
            }
        }

        [Test]
        public void WrapMinutesKeepsValuesInDay()
        {
            Assert.That(DayLightingCurve.WrapMinutes(-1f), Is.EqualTo(DayLightingCurve.MinutesPerDay - 1f).Within(0.001f));
            Assert.That(DayLightingCurve.WrapMinutes(1441f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(DayLightingCurve.WrapMinutes(1440f), Is.EqualTo(0f));
        }

        [Test]
        public void IntensityStaysInRange()
        {
            for (var m = 0f; m < DayLightingCurve.MinutesPerDay; m += 10f)
            {
                var s = DayLightingCurve.Evaluate(m);
                Assert.That(s.Intensity, Is.InRange(DayLightingCurve.NightIntensity, 1f));
            }
        }
    }
}
