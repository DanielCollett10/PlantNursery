# Behaviour layer

Behaviour is an optional list of **modules** on a species, set in data and ticked by the sim **after growth**.
A new behaviour is a new module type; a new plant using existing modules is data only.

## Tick order
Each tick, after the growth system: (1) Needs rise, (2) Moods update, (3) Neighbour effects apply, (4) escaped plants move.
Plants are processed in ascending plant id so results are deterministic.

## Modules
| Module | What it does | Data (per species) |
|---|---|---|
| **Need** | A meter beyond water (e.g. hunger) 0–100 that rises over time and is reset by care | `need.id`, `ratePerHour`, `resetBy` (item id), `resetTo` |
| **Mood** | Telegraph stages driven by one need: Content → Restless → Acting out → Escaped | `mood.need`, `restlessAt`, `actingOutAt`, `escapeAfterHoursAtMax` |
| **Neighbour effect** | Acts on neighbouring pots (`building.md`) while the plant is in a given mood | `effect.kind` (`graze`, `calm`, `pollinate`), `whileMood`, `targetSpecies`, `everyHours`, numbers per kind |
| **Mobility** | Lets an Escaped plant leave its pot and move around the nursery | `speedMetresPerSecond`, `seek` (species id), `blocksCarts` |

- Modules only run for **living** plants that have reached a stage the species marks as `behaviourFromStage` (default: first stage).
- Night rules (`time.md`) apply: needs ×0.25, mood capped at Restless.

## Chaos rules (apply to every module)
- Plants only act up for a reason: neglect, bad neighbours, or volatile stock the players chose.
- Trouble is telegraphed: twitch, then rattle, then escape. No mood skips a stage.
- Calm is progression: better cages, calmer bloodlines and soothing neighbours reduce chaos.
- Chain reactions stay local: one bench (plus benches touching it), never the whole nursery.
- No new fail states: chaos costs time and value only. Escapes never kill plants.

## Snapjaw numbers (first pass)
| Setting | Value | Data key |
|---|---|---|
| Hunger | 0–100, +8 per in-game hour (≈12 h fed → starving), × `temperament` gene | `need.ratePerHour` |
| Content | hunger < 40 | `restlessAt` = 40 |
| Restless | 40 ≤ hunger < 70: leaves twitch, jaws clack (the warning) | `actingOutAt` = 70 |
| Acting out | hunger ≥ 70: grazes on touching Hearthmoss | — |
| Escaped | hunger has been 100 for 2 in-game hours | `escapeAfterHoursAtMax` = 2 |
| Feeding | One portion of grubs sets hunger to 0 → Content | `resetBy` = `grubs`, `resetTo` = 0 |
| Grazing | While Acting out, every 2 in-game hours: eats one neighbouring Hearthmoss (lowest plant id): moss −1 quality step, Snapjaw hunger −30 | `everyHours` = 2, `qualitySteps` = 1, `needRelief` = 30 |
| Moving | 0.5 m/s real time (players 3 m/s) towards the nearest Hearthmoss | `speedMetresPerSecond` |
| Catching | Interact in reach: it goes into your hands | — |

- **Mood from hunger** is recomputed every tick, with one exception: Escaped stays Escaped until the plant is re-potted.
- Grazing timer starts when the plant enters Acting out; leaving Acting out resets it. A graze can drop the hunger below 70 (back to Restless).
- With no Hearthmoss neighbour, an Acting-out Snapjaw just waits for food (and may escape).

### Escape, catch, re-pot
1. On escape the plant leaves its pot; the empty pot stays in its slot. Raise `PlantEscaped`.
2. The escapee is a **sim entity with a floor position** (cells, sub-cell precision allowed) and paths through the floor grid around floor items (A* on cells, 4-way, deterministic tie-breaks). Unity only animates it.
3. It heads for the floor cell next to the nearest bench slot holding a Hearthmoss. Speed per tick = `speedMetresPerSecond × realSecondsPerTick`.
4. It **blocks carts** it touches (a cart can't move through its cell).
5. A player in reach with empty hands catches it → it's held as a "caught plant". Raise `PlantCaught`.
6. Placing it in an empty pot (any size its current stage allows) re-pots it → mood recomputed from hunger (usually Acting out until fed). Raise `PlantRepotted`.
7. Hunger keeps rising while escaped or held; water doesn't drain while out of a pot. _(Lead's default.)_

### Open (to confirm)
- What an escapee does when it **reaches** the moss (suggested: grazes it from the floor on the normal 2 h timer until caught).
- What it does if there is **no** Hearthmoss anywhere (suggested: wanders to a random reachable cell, seeded RNG, then waits).

## Neighbours on screen
- While holding a pot or in bench view, soft lines link each slot to its neighbours, coloured **and** patterned by effect: calming green solid, draining red dashed, pollen yellow dotted.
- Hovering a plant shows its neighbours and what they do to it.
