# Building

## Grid
- Cell = 0.5 m × 0.5 m (see `docs/decisions/0006-units-and-scale.md`). Cells are integer `(x, z)`, with `(0,0)` at the plot's south-west corner.
- The **plot** is a rectangle of buildable cells. The starter plot is 40 × 30 cells (20 m × 15 m, in data). Placement outside it is invalid.

## Layers
| Layer | Holds | Rule |
|---|---|---|
| Floor | Benches, shelving, tap, computer, bed, paths, trolley bay | Each cell holds at most one Floor item |
| Surface | Plug trays, pots | Every footprint cell needs a Floor item with `providesSurface: true` under it |
| Overhead | Sprinklers, lights, heaters | Later |
| Zone | Poly tunnels (M5) | A tunnel covers an area. Floor items can be placed inside it. Zones may not overlap each other |

Tunnels sit on their own Zone layer so benches can go inside them. Only the tunnel's walls and door edge block Floor placement. The exact shape comes in M5.

## Surface mini-grid
Surfaces (bench and shelf tops) use a finer **mini-grid of 0.1 m units, 5 × 5 per floor cell**.
Surface items take up their physical size. The player chooses where each one sits, and there's no fixed "N pots per cell" rule.

| Item | Real size (approx.) | Surface footprint (units) |
|---|---|---|
| Small pot | 9 cm | 1 × 1 |
| Medium pot | 17 cm | 2 × 2 |
| Plug tray (6 cells) | 20 × 30 cm | 2 × 3 |
| Watering can (put down) | 20 × 30 cm | 2 × 3 |
| Seed packet (put down) | 10 × 20 cm | 1 × 2 |

Footprints are in data, and rotation (90° steps) swaps width and depth as on the floor grid.

**Rules**
- A surface unit is usable if the floor cell under it holds an item with `providesSurface: true`.
  Adjacent benches form one continuous surface, so a tray may straddle two benches that touch.
- Each surface unit holds at most one surface item.
- Mini-grid coordinates are integers `(u, v)` = floor cell × 5 + offset (0–4). Surface units are indexed across the whole plot, not per bench.

**Keeping it lightweight**
- The ghost snaps to the mini-grid unit nearest the aim point, centred on the item.
- **Auto-nudge:** if that spot is blocked, the sim's `FindNearestFreeSpot(item, rotation, desired, radius)` query returns the closest valid spot within `nudgeRadius` units (default 2). The ghost jumps there.
  Only if there is none does the ghost turn red. Ties are broken by smallest `v`, then smallest `u`, so the result is deterministic.
- Players never have to line pots up by hand. Dropping pots roughly in a row packs them neatly.
- Putting down a held surface item (pot, tray, can, packet) uses the same ghost and snapping. See `controls.md`.

## Footprints and rotation
- Each item type has a footprint `w × d` in cells (sub-slots for Surface items) and an anchor at its south-west corner.
- Rotation is 0/90/180/270. Rotating by 90° or 270° swaps `w` and `d`, keeping the anchor cell.
- Standard bench: 2 × 4 cells, `providesSurface: true`.

## Placement validation
`PlaceItem(player, itemId, layer, anchor, rotation)` succeeds only if all of these are true. Otherwise it is rejected with the first failing reason:
1. `NotOwned`: the item is not in the build inventory.
2. `OutOfPlot`: a footprint cell is outside the plot.
3. `Occupied`: a footprint cell (or surface unit) on that layer is taken.
4. `NoSurface`: (Surface items) a footprint cell has no surface-providing Floor item.

The ghost uses the same check through a query (`CanPlace`) that changes nothing.
Unity blocks placing Floor items where a player's body is standing. That is a local physics check, not a sim rule.

## Picking up and moving
- `PickUpItem(player, placedId)` returns a placed item to the build inventory.
- Rejected with `HasItemsOnTop` if any Surface item sits on it. Clear the bench first.
- Plug trays and pots with plants in them aren't build items. They are picked up by hand (see `controls.md`).
- Selling back placed items comes later (default 50% of price, see `economy.md`).
