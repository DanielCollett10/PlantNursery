# Time, days and seasons

## Ticks
- **1 sim tick = 1 in-game minute.** A day is 1440 ticks. Rules use in-game time only.
- Unity decides how fast ticks happen in real time, from data.

| Setting | Data key | First pass |
|---|---|---|
| Playable day | `dayStartMinute` / `nightStartMinute` | 360 (06:00) / 1320 (22:00) |
| Real time for the playable day | `realSecondsDay` | 600 (10 min), so 0.625 s per tick |
| Phase boundaries | `morningEndMinute` / `eveningStartMinute` | 540 (09:00) / 1200 (20:00) |
| Night | — | 22:00–06:00, skipped by sleep |
| Night rate | `nightRateMultiplier` | 0.25 (validated 0..1); read via `GameClock.NeedRateMultiplier` |
| Shop opening hours | `shopOpenMinute` / `shopCloseMinute` | 480 (08:00) / 1080 (18:00) |
| Season length | `seasonLengthDays` | 10 (demo profile may shorten) |

## Day shape
Morning care and planning → trading through the day → any wagon departures booked for today (each with a
countdown) → evening wind-down → sleep. Many days have no departure.

Phases (lighting, HUD): Morning = 06:00–09:00, Daytime = 09:00–20:00, Evening = 20:00–22:00, Night = 22:00–06:00. Boundaries are data.

## Sleep
- Sleep is possible in Evening or Night. Solo: interact with the bed. Co-op: every player readies up; the host can force it (`coop.md`).
- Sleeping steps the sim tick by tick to the next `dayStartMinute`, so the result equals waiting.
- If the day reaches 22:00 with nobody asleep, time continues (Night rules apply) until everyone sleeps.

## Night rules
Nobody can act while everyone sleeps, so nothing may go wrong overnight that players couldn't have prevented.
During Night (`nightStartMinute` to `dayStartMinute`):
- Needs (e.g. hunger) and drying tick at `nightRateMultiplier` = 0.25 of their daytime rate.
- Moods can't pass **Restless**: no grazing or escapes at night. A hungry Snapjaw wakes up Restless.
- Growth continues at the normal rate.

## Seasons
- Spring → Summer → Autumn are growing seasons. Each species has a growth multiplier per season (`species.md`).
- MVP: Autumn loops back to Spring. **Winter** is undecided (`open-questions.md`); the season list is data so it can be added or skipped.

## Pause
Solo only (`SimWorld` pause). In co-op, time never pauses.
