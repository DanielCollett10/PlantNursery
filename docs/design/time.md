# Time and seasons

## Ticks
- **1 sim tick = 1 in-game minute.** A day is 1440 ticks. All rules use in-game time, never real time.
- The Unity layer decides how fast ticks happen in real time. The pace is set in the `time` data block:

| Setting | Default (release target) | Demo profile (M1) |
|---|---|---|
| `dayStartMinute` (morning) | 360 (06:00) | 360 |
| `nightStartMinute` | 1200 (20:00) | 1200 |
| `realSecondsDaylight` (06:00→20:00) | 3600 (1 h) | 600 (10 min) |
| `realSecondsNight` (20:00→06:00) | 1200 (20 min) | 120 (2 min) |

  Real seconds per tick in each phase = real seconds for that phase ÷ in-game minutes in that phase.
- The real-time pacing is still open. Plants should take about 4–7 days to "get going" at release. The demo profile is shorter so M1 can be shown in minutes.

## Day phases
Derived from the time of day; used by lighting, customers and the HUD.

| Phase | From | To |
|---|---|---|
| Morning | `dayStartMinute` | +180 min |
| Daytime | Morning end | `nightStartMinute` − 120 |
| Evening | Daytime end | `nightStartMinute` |
| Night | `nightStartMinute` | next `dayStartMinute` |

Shop opening hours (for M2) are separate data: `shopOpenMinute`, `shopCloseMinute`.

## Sleep
- **Solo:** interacting with the bed while it's Evening or Night sleeps. Sleep is rejected during Morning and Daytime.
- Sleeping advances the sim tick by tick to the next `dayStartMinute`, as fast as possible. Plants keep growing and drying overnight, so the result is the same as waiting.
- Co-op ready-up rules are in `coop.md` (M3).

## Seasons
- Order: Spring → Summer → Autumn → Spring (loops in the MVP). Winter will slot in later as data.
- `seasonLengthDays` is in data (default 10, demo 3). Day 1 is Spring day 1.
- Each variety has a growth multiplier per season (see `plants.md`).

## Pause
- `SimWorld` can pause: while paused, `Step` does nothing (T-001).
- Pausing is available **only in single-player sessions**. In co-op, time never pauses.
