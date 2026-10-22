using NUnit.Framework;
using PlantNursery.Sim.Time;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    public class SimHostTests
    {
        private GameObject _go;
        private SimHost _host;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SimHost");
            _host = _go.AddComponent<SimHost>();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_go); }

        [Test]
        public void LoadsRealContentWithoutErrors()
        {
            Assert.DoesNotThrow(() => _host.Initialize());
            Assert.IsTrue(_host.IsReady);
            Assert.Greater(_host.Content.Building.PlotWidth, 0);
            Assert.AreEqual(0, _host.LocalPlayerId.Value);
        }

        [Test]
        public void DemoProfileLoads()
        {
            _host.UseDemoProfile = true;
            Assert.DoesNotThrow(() => _host.Initialize());
            Assert.AreEqual(3, _host.Content.Time.SeasonLengthDays);
        }

        [Test]
        public void AdvanceStepsTicksAtPhasePace()
        {
            _host.Initialize();
            var perTick = _host.Clock.RealSecondsPerTick;
            _host.Advance(perTick * 3.5);
            Assert.AreEqual(3, _host.World.Tick);
        }

        [Test]
        public void RejectedSleepDoesNotFastForward()
        {
            _host.Initialize();
            var result = _host.Submit(new SleepCommand(_host.LocalPlayerId));
            Assert.IsFalse(result.IsAccepted);
        }

        [Test]
        public void AcceptedSleepFastForwardsToNextDayAndDispatchesEvent()
        {
            _host.Initialize();
            var days = 0;
            _host.EventDispatched += e => { if (e is DayStartedEvent) days++; };
            _host.World.Step(_host.Content.Time.NightStartMinute - _host.Content.Time.DayStartMinute);
            Assert.IsTrue(_host.Clock.CanSleep);
            var result = _host.Submit(new SleepCommand(_host.LocalPlayerId));
            Assert.IsTrue(result.IsAccepted);
            Assert.AreEqual(2, _host.Clock.Day);
            Assert.IsTrue(_host.Clock.IsDayStart);
            Assert.AreEqual(1, days);
        }
    }
}
