using System;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Time
{
    /// <summary>Phase of the day (docs/design/time.md).</summary>
    public enum DayPhase { Morning, Daytime, Evening, Night }

    /// <summary>Raised when a new day begins (at dayStartMinute). Not raised for day 1.</summary>
    public sealed class DayStartedEvent : ISimEvent
    {
        /// <summary>The new day number (1-based).</summary>
        public int Day { get; }
        public DayStartedEvent(int day) { Day = day; }
    }

    /// <summary>Raised when the season changes.</summary>
    public sealed class SeasonChangedEvent : ISimEvent
    {
        /// <summary>Previous season.</summary>
        public Season From { get; }
        /// <summary>New season.</summary>
        public Season To { get; }
        public SeasonChangedEvent(Season from, Season to) { From = from; To = to; }
    }

    /// <summary>
    /// Game clock derived from the world tick (1 tick = 1 minute). Tick 0 is Spring day 1 at
    /// dayStartMinute. A day runs from dayStartMinute to the next dayStartMinute.
    /// Also an <see cref="ISimSystem"/> that raises day/season events.
    /// </summary>
    public sealed class GameClock : ISimSystem
    {
        /// <summary>Minutes in a day.</summary>
        public const int MinutesPerDay = 1440;
        private const int SeasonCount = 3;

        private readonly TimeConfig _cfg;
        private readonly SimWorld _world;

        public GameClock(TimeConfig config, SimWorld world)
        {
            _cfg = config ?? throw new ArgumentNullException(nameof(config));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            if (_cfg.DayStartMinute < 0 || _cfg.DayStartMinute >= MinutesPerDay) throw new ArgumentException("dayStartMinute out of range");
            if (_cfg.MorningEndMinute <= _cfg.DayStartMinute
                || _cfg.EveningStartMinute <= _cfg.MorningEndMinute
                || _cfg.NightStartMinute <= _cfg.EveningStartMinute
                || _cfg.NightStartMinute >= _cfg.DayStartMinute + MinutesPerDay)
                throw new ArgumentException("phase boundaries must increase: dayStart < morningEnd < eveningStart < nightStart");
            if (_cfg.RealSecondsDay <= 0f) throw new ArgumentException("realSecondsDay must be > 0");
            if (_cfg.NightRateMultiplier < 0f || _cfg.NightRateMultiplier > 1f)
                throw new ArgumentException("nightRateMultiplier must be 0..1");
            if (_cfg.SeasonLengthDays < 1) throw new ArgumentException("seasonLengthDays must be >= 1");
        }

        /// <summary>Creates a clock, adds it as a system and registers the Sleep handler.</summary>
        public static GameClock Install(SimWorld world, TimeConfig config)
        {
            var clock = new GameClock(config, world);
            world.AddSystem(clock, SystemPhase.Time);
            world.RegisterHandler<SleepCommand>(new SleepHandler(clock));
            return clock;
        }

        /// <summary>Day number, starting at 1.</summary>
        public int Day => DayAt(_world.Tick);

        /// <summary>Minute of the day, 0..1439.</summary>
        public int MinuteOfDay => MinuteAt(_world.Tick);

        /// <summary>Current day phase.</summary>
        public DayPhase Phase => PhaseAt(_world.Tick);

        /// <summary>Current season.</summary>
        public Season Season => SeasonAt(_world.Tick);

        /// <summary>Day within the season, starting at 1.</summary>
        public int DayOfSeason => (Day - 1) % _cfg.SeasonLengthDays + 1;

        /// <summary>Day number at a tick.</summary>
        public int DayAt(long tick) => (int)(tick / MinutesPerDay) + 1;

        /// <summary>Minute of day at a tick.</summary>
        public int MinuteAt(long tick) => (int)((_cfg.DayStartMinute + tick) % MinutesPerDay);

        /// <summary>Season at a tick.</summary>
        public Season SeasonAt(long tick) => (Season)(((DayAt(tick) - 1) / _cfg.SeasonLengthDays) % SeasonCount);

        /// <summary>Phase at a tick (boundaries come from the TimeConfig).</summary>
        public DayPhase PhaseAt(long tick)
        {
            var offset = (MinuteAt(tick) - _cfg.DayStartMinute + MinutesPerDay) % MinutesPerDay;
            if (offset < _cfg.MorningEndMinute - _cfg.DayStartMinute) return DayPhase.Morning;
            if (offset < _cfg.EveningStartMinute - _cfg.DayStartMinute) return DayPhase.Daytime;
            if (offset < _cfg.NightStartMinute - _cfg.DayStartMinute) return DayPhase.Evening;
            return DayPhase.Night;
        }

        /// <summary>True during the Night phase.</summary>
        public bool IsNight => Phase == DayPhase.Night;

        /// <summary>Multiplier for need and drying rates: 1.0 by day, nightRateMultiplier at night.</summary>
        public float NeedRateMultiplier => IsNight ? _cfg.NightRateMultiplier : 1f;

        /// <summary>True when the player may sleep (Evening or Night).</summary>
        public bool CanSleep => Phase == DayPhase.Evening || Phase == DayPhase.Night;

        /// <summary>True when the clock is exactly at dayStartMinute.</summary>
        public bool IsDayStart => MinuteOfDay == _cfg.DayStartMinute;

        /// <summary>Real seconds one tick lasts: realSecondsDay over the playable day's minutes.</summary>
        public double RealSecondsPerTick => _cfg.RealSecondsDay / (double)(_cfg.NightStartMinute - _cfg.DayStartMinute);

        /// <inheritdoc />
        public void Update(SimWorld world)
        {
            var tick = world.Tick;
            if (tick <= 0) return;
            if (DayAt(tick) != DayAt(tick - 1)) world.Raise(new DayStartedEvent(DayAt(tick)));
            var now = SeasonAt(tick);
            var before = SeasonAt(tick - 1);
            if (now != before) world.Raise(new SeasonChangedEvent(before, now));
        }
    }
}
