# Plants

## Plants are individuals
- Every plant has: `id`, `varietyId`, `genes` (see `genetics.md`), `growth` (float), `stage`, `moisture` 0–100,
  `health` 0–100, `rootBoundMinutes`, `goodCareMinutes`, `aliveMinutes`, `isDead`.
- **One plant per plug cell, one plant per pot. Always.** Water, care and sale are per plant.

## Containers
| Container | Holds | Max stage (cap) | Accepts plants from stage | Dry rate (moisture/hour) |
|---|---|---|---|---|
| Plug tray | 6 cells, 1 plant each | Rooted | sown as Seed | 4.0 |
| Small pot | 1 plant | Established | Rooted | 2.5 |
| Medium pot | 1 plant | Mature | Established | 1.5 |

All values come from data. Each tray type defines its cell count (the starter tray has 6).

## Stages
`Seed → Seedling → Rooted → Young → Established → Mature`

Each variety defines the `growth` threshold for each stage (in growth points). Stage = the highest threshold reached.
Sellable stages and prices are in `economy.md` (M2).

## Growth per tick
Each tick, for each living plant:
1. `moisture -= dryRate / 60`, floored at 0.
2. Water factor: moisture ≥ 30 → 1.0. Moisture 1–29 (**dry**) → 0.5. Moisture 0 (**wilting**) → 0.
3. Season factor: the variety's `seasonGrowth[season]` (e.g. good 1.0, slow 0.4, none 0).
4. If the plant is **root-bound** (its stage has reached the container's cap), growth factor is 0.
5. `growth += variety.growthPerHour / 60 × waterFactor × seasonFactor`.
   Growth is clamped so it never passes the container cap's threshold.

## Root-bound
- A plant is root-bound when its stage equals its container's cap. `rootBoundMinutes` counts up while root-bound.
- After `rootBoundGraceHours` (default 24), the plant loses `rootBoundHealthLossPerHour` health (default 2/h). It can die of this.
- Potting on to a bigger container resets `rootBoundMinutes` to 0, and growth resumes.

## Water and health
- `WaterPlant(player, plantId)` with a watering can that has water: plant moisture = 100, can water −1.
  Rejected if the plant is dead (`PlantDead`), the can is empty (`CanEmpty`) or the player isn't holding a can (`NoCan`).
- While wilting, the plant loses `wiltHealthLossPerHour` health (default 2/h, so roughly 2 days of wilting kills it).
- While moisture ≥ 30 and the plant isn't root-bound past its grace period, it regains `recoverPerHour` health (default 1/h), up to 100.
- Health 0 → `isDead = true`. Dead plants stop all updates.
- Over-watering, feed, pests and disease come later.

## Dead plants
- A dead plant stays visibly dead in its cell or pot until a player clears it ("Compost"). Clearing gives nothing and frees the cell or pot.
- There is no other fail state. Neglect costs value only.

## Quality (used from M2)
- Each tick alive: `aliveMinutes += 1`. The tick counts as good care if moisture ≥ 30 and the plant isn't root-bound past its grace period: `goodCareMinutes += 1`.
- `careScore = goodCareMinutes / aliveMinutes`. Grade **A** ≥ 0.9, **B** ≥ 0.7, otherwise **C**. Thresholds are in data.

## Sowing and potting on
- `Sow(player, trayId, cellIndex)`: the player holds a seed packet with ≥ 1 seed, and the cell is empty.
  This creates a plant at stage Seed with moisture 50 and health 100, and uses one seed.
- `PotOn(player, plantId, potId)` (M2): the target pot is empty and accepts the plant's stage. The plant moves and keeps its stats.

## Tunnels (M5)
- Plants inside a tunnel zone use the `tunnel` row of their variety's season table. It is warmer, so growth is better in spring and autumn.
