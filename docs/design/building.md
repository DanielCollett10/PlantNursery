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

## Surface sub-slots
A surface cell is split into **2 × 2 sub-slots** (0.25 m each), so small pots don't waste a whole cell.

| Item | Surface footprint |
|---|---|
| Plug tray (6 cells) | 2 × 2 sub-slots (one full surface cell) |
| Small pot | 1 sub-slot |
| Medium pot | 2 × 2 sub-slots |

Surface items snap to sub-slots. A sub-slot holds at most one surface item.

## Footprints and rotation
- Each item type has a footprint `w × d` in cells (sub-slots for Surface items) and an anchor at its south-west corner.
- Rotation is 0/90/180/270. Rotating by 90° or 270° swaps `w` and `d`, keeping the anchor cell.
- Standard bench: 1 × 4 cells, `providesSurface: true`.

## Placement validation
`PlaceItem(player, itemId, layer, anchor, rotation)` succeeds only if all of these are true. Otherwise it is rejected with the first failing reason:
1. `NotOwned`: the item is not in the build inventory.
2. `OutOfPlot`: a footprint cell is outside the plot.
3. `Occupied`: a footprint cell or sub-slot on that layer is taken.
4. `NoSurface`: (Surface items) a footprint cell has no surface-providing Floor item.

The ghost uses the same check through a query (`CanPlace`) that changes nothing.
Unity blocks placing Floor items where a player's body is standing. That is a local physics check, not a sim rule.

## Picking up and moving
- `PickUpItem(player, placedId)` returns a placed item to the build inventory.
- Rejected with `HasItemsOnTop` if any Surface item sits on it. Clear the bench first.
- Plug trays and pots with plants in them aren't build items. They are picked up by hand (see `controls.md`).
- Selling back placed items comes later (default 50% of price, see `economy.md`).
