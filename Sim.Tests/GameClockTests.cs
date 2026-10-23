using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    public class GameClockTests
    {
        private static TimeConfig Cfg(int seasonDays = 3) => new TimeConfig
        {
            DayStartMinute = 360, MorningEndMinute = 540, EveningStartMinute = 1200, NightStartMinute = 1320,
            RealSecondsDay = 600, NightRateMultiplier = 0.25f, SeasonLengthDays = seasonDays
        };

        private static (SimWorld, GameClock) Make(int seasonDays = 3)
        {
            var w = new SimWorld();
            return (w, GameClock.Install(w, Cfg(seasonDays)));
        }

        // Minute-of-day -> ticks from start (dayStart = 360).
        private static int Ticks(int minuteOfDay) => (minuteOfDay - 360 + 1440) % 1440;

        [Test]
        public void StartsSpringDayOneAtDayStart()
        {
            var (_, c) = Make();
            Assert.AreEqual(1, c.Day);
            Assert.AreEqual(360, c.MinuteOfDay);
            Assert.AreEqual(Season.Spring, c.Season);
            Assert.AreEqual(1, c.DayOfSeason);
            Assert.AreEqual(DayPhase.Morning, c.Phase);
        }

        [TestCase(360, DayPhase.Morning)]
        [TestCase(539, DayPhase.Morning)]
        [TestCase(540, DayPhase.Daytime)]
        [TestCase(1199, DayPhase.Daytime)]
        [TestCase(1200, DayPhase.Evening)]
        [TestCase(1319, DayPhase.Evening)]
        [TestCase(1320, DayPhase.Night)]
        [TestCase(0, DayPhase.Night)]
        [TestCase(359, DayPhase.Night)]
        public void PhaseBoundaries(int minute, DayPhase expected)
        {
            var (w, c) = Make();
            w.Step(Ticks(minute));
            Assert.AreEqual(minute, c.MinuteOfDay);
            Assert.AreEqual(expected, c.Phase);
        }

        [Test]
        public void SeasonsLoop()
        {
            var (w, c) = Make(3);
            var expected = new[] { Season.Spring, Season.Summer, Season.Autumn, Season.Spring };
            for (var s = 0; s < 4; s++)
            {
                for (var d = 1; d <= 3; d++)
                {
                    Assert.AreEqual(expected[s], c.Season, "season block " + s + " day " + d);
                    Assert.AreEqual(d, c.DayOfSeason);
                    w.Step(1440);
                }
            }
            Assert.AreEqual(Season.Summer, c.Season);
        }

        [Test]
        public void RaisesDayStartedAndSeasonChanged()
        {
            var (w, _) = Make(2);
            w.Step(1440 * 2);
            var evts = w.DrainEvents();
            var days = evts.OfType<DayStartedEvent>().Select(e => e.Day).ToArray();
            CollectionAssert.AreEqual(new[] { 2, 3 }, days);
            var change = evts.OfType<SeasonChangedEvent>().Single();
            Assert.AreEqual(Season.Spring, change.From);
            Assert.AreEqual(Season.Summer, change.To);
        }

        [Test]
        public void SleepRejectedInMorningAndDaytime()
        {
            var (w, c) = Make();
            foreach (var minute in new[] { 360, 800, 1199 })
            {
                var target = Ticks(minute);
                w.Step((int)(target - w.Tick));
                var res = w.Validate(new SleepCommand(new PlayerId(0)));
                Assert.IsFalse(res.IsAccepted);
                Assert.AreEqual(RejectReason.NotTired, res.Reason);
            }
            w.Submit(new SleepCommand(new PlayerId(0)));
            w.Step();
            Assert.IsTrue(w.DrainEvents().OfType<CommandRejectedEvent>().Any());
            Assert.AreEqual(1199 - 360 + 1, w.Tick); // the rejected command's tick still advances once
        }

        [Test]
        public void SleepFromNightRunsToDayStart()
        {
            var (w, c) = Make();
            w.Step(Ticks(1380)); // 23:00
            Assert.AreEqual(DayPhase.Night, c.Phase);
            Assert.IsTrue(w.Validate(new SleepCommand(new PlayerId(0))).IsAccepted);
            w.Submit(new SleepCommand(new PlayerId(0)));
            w.Step();
            Assert.AreEqual(360, c.MinuteOfDay);
            Assert.AreEqual(2, c.Day);
        }

        [Test]
        public void SleepFromEveningRunsToNextDayStart()
        {
            var (w, c) = Make();
            w.Step(Ticks(1250));
            w.DrainEvents();
            w.Submit(new SleepCommand(new PlayerId(0)));
            w.Step();
            Assert.AreEqual(2, c.Day);
            Assert.AreEqual(360, c.MinuteOfDay);
            Assert.AreEqual(1440, w.Tick);
            Assert.AreEqual(2, w.DrainEvents().OfType<DayStartedEvent>().Single().Day);
        }

        [Test]
        public void SleepFromAfterMidnightRunsToSameMorning()
        {
            var (w, c) = Make();
            w.Step(Ticks(60)); // 01:00, still day 1 (days roll at dayStart)
            Assert.AreEqual(1, c.Day);
            w.Submit(new SleepCommand(new PlayerId(0)));
            w.Step();
            Assert.AreEqual(2, c.Day);
            Assert.AreEqual(360, c.MinuteOfDay);
            Assert.AreEqual(1440, w.Tick);
        }

        [Test]
        public void SleepAcrossSeasonBoundaryRaisesSeasonChanged()
        {
            var (w, _) = Make(1);
            w.Step(Ticks(1250));
            w.DrainEvents();
            w.Submit(new SleepCommand(new PlayerId(0)));
            w.Step();
            Assert.AreEqual(1, w.DrainEvents().OfType<SeasonChangedEvent>().Count());
        }

        [Test]
        public void RealSecondsPerTickComesFromData()
        {
            var (w, c) = Make();
            Assert.AreEqual(0.625, c.RealSecondsPerTick, 1e-9); // 600 s / 960 min
            w.Step(Ticks(1400));
            Assert.AreEqual(0.625, c.RealSecondsPerTick, 1e-9);
        }

        [Test]
        public void NightRulesHelper()
        {
            var (w, c) = Make();
            Assert.IsFalse(c.IsNight);
            Assert.AreEqual(1f, c.NeedRateMultiplier);
            w.Step(Ticks(1250));
            Assert.IsFalse(c.IsNight);
            Assert.AreEqual(1f, c.NeedRateMultiplier);
            w.Step(Ticks(1320) - Ticks(1250));
            Assert.IsTrue(c.IsNight);
            Assert.AreEqual(0.25f, c.NeedRateMultiplier);
            w.Step(Ticks(359) - Ticks(1320));
            Assert.IsTrue(c.IsNight);
            w.Step();
            Assert.IsFalse(c.IsNight);
        }
    }
}
