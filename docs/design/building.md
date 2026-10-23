# Building

Players lay out the nursery on a grid. Placement matters more than in an ordinary nursery because plants
affect their neighbours: a layout is also a puzzle of who sits next to whom.

## Floor grid
- Cell = 0.5 m × 0.5 m, integer `(x, z)`, origin at the plot's south-west corner (`docs/decisions/0006`).
- The **plot** is a rectangle of buildable cells (data). It starts small; land expansion is an unlock.
- Every item covers one or more cells and rotates in 90° steps (90°/270° swap width and depth).
- Paths are at least 2 cells (1.0 m) wide so a handcart fits.

## Layers
| Layer | Holds | Rule |
|---|---|---|
| Floor | Benches, tables, rain barrel, order desk, storage crate, shop counter, bed, paths, cart bay | One floor item per cell |
| Slot | Pots and plug trays | Only in a bench's slots (below). Never on the floor or on another object |
| Overhead | Watering, lights | Later |
| Zone | Cold frames, glasshouses, warded enclosures | Change conditions inside them; floor items go inside. Zones don't overlap |

- **Valid placement:** a green or red ghost previews; the host's sim has the final say.
- **Picking up / moving:** placed items go back to the build inventory. **Moving a bench takes its pots with it, in the same slots.**
- **Selling back:** for `sellBackFraction` (0.5) of the price, rounded down.
- **No stacking:** nothing on a pot; a bench can't go on another object.

### Placement rejections (first failing reason)
`NotOwned`, `OutOfPlot`, `Occupied`, then for pots: `NotABench`, `SlotOccupied`, `WrongSlotSize`.

## Structures (zones)
- **Cold frame** (MVP): a small zone that is warmer; plants inside use their species' `coldFrame` season row.
- **Glasshouse** (later): warmer still. **Warded enclosure** (later): holds volatile stock; escapees can't leave it.

## Benches and slots
Bench tops are not free surfaces. Each bench has a **planting area** laid out in rows, and the pot size
decides how many rows fit. Placement stays a quick choice with a small puzzle in it.

### The planting mini-grid
The planting area is measured in **0.1 m units** (`u` along the bench, `v` across). A 3 m bench's planting
area is 30 × 6 units. Every slot is a rectangle of units (reference drawing: `source/grid-for-worktop-1x4.png`).

| Pot size | Slot (units) | Slot (m) | Rows across 0.6 m | Per row on 3 m | Most neighbours on a lone bench |
|---|---|---|---|---|---|
| Small | 2 × 2 | 0.2 × 0.2 | 3 | 15 (45 in all) | 4 |
| Medium | 3 × 3 | 0.3 × 0.3 | 2 | 10 (20 in all) | 3 |
| Large | 6 × 6 | 0.6 × 0.6 | 1 | 5 | 2 |
| Plug tray | 4 along × 6 across | 0.4 × 0.6 | 1 | 7 | 2 |

All sizes are data (`slotSizes`: per container, `alongUnits`, `acrossUnits`). Large is 0.6 m (agreed), so small,
medium and large all line up on 0.6 m boundaries.

### Slot grid (agreed: option A)
- **Pots snap to slots.** A bench can mix sizes along its length (a run of small, then medium, then large); each run uses its own row layout.
- Each size has a **fixed slot grid per bench**: slot `(col, row)` of a size covers units
  `u = col × alongUnits … + alongUnits − 1` and `v = row × acrossUnits … + acrossUnits − 1`, in bench-local units
  measured from the planting area's left-front corner (as seen from the working edge).
  Only slots that fit wholly inside the planting area exist (`col < floor(lengthUnits / alongUnits)`, `row < floor(depthUnits / acrossUnits)`).
  So on a 3 m bench: small 15 × 3, medium 10 × 2, large 5 × 1, plug tray 7 × 1 (the last 0.2 m is unused by trays).
- A slot is free if none of its units is used by another pot or tray. Mixing sizes may leave a small gap; runs that change size on a 0.6 m boundary never do.
- A slot is addressed as `(benchId, containerSize, col, row)`. Place rejections: `NotABench`, `NoSuchSlot`, `SlotOccupied`.
- Gamepad slot-stepping is **in the MVP** (`docs/decisions/0012`): the pointer steps slot by slot, across the planting area and the working strip.

### Neighbours
- Two pots are **neighbours** if their slots **share an edge of at least one unit** (left/right in a row, front/back across rows). Diagonal corners don't count.
- **Benches that touch share neighbours.** If two benches stand back to back (long sides touching on the floor grid) the facing edge rows count as neighbours wherever their slots overlap across the join, even with different pot sizes. End to end works the same for the end slots. A gap of one floor cell or more keeps benches apart.
- Implementation hint: express every planting unit in one nursery-wide unit space, so "shares an edge" works the same within a bench and across a join.
- Neighbours are recomputed when a pot or bench is placed, moved or removed.
- **The trade-off:** small pots pack in more plants and more neighbour interactions; large pots are worth more and easier to keep apart.

### Bench types
| Bench | Footprint | Planting area | Buy | Unlock |
|---|---|---|---|---|
| 3 m bench | 6 × 2 cells (3.0 × 1.0 m) | 3.0 × 0.6 m (30 × 6 units) | 40 | Start |
| 4 m bench | 8 × 2 cells (4.0 × 1.0 m) | 4.0 × 0.6 m (40 × 6 units) | 50 | Tier 2 |
| Wide bench | 8 × 3 cells (4.0 × 1.5 m) | 4.0 × 1.5 m (40 × 15 units), no working edge | data | Tier 3 |

### Bench depth (agreed)
- Standard benches are **1.0 m deep (2 floor cells)**: a **0.4 m working strip at the front** and the **0.6 m planting area along the back**
  (reference drawing: `source/grid.png`, 30 × 10 units on a 3 m bench: 4 strip rows + 6 planting rows for every pot size.
  The "large" drawing there shows 2 + 8 rows; that is a drawing slip, 4 + 6 is correct).
  At rotation 0 the front faces south (−z); rotation turns the whole bench, planting area included.
- Back-to-back benches (backs touching) put their planting areas against each other, which is where the cross-bench puzzle lives.
  A bench whose back touches another bench's **front** gets no cross-bench neighbours (the working edge is in between).
- Data per bench item: `planting.lengthUnits`, `planting.depthUnits`, `planting.backOffsetUnits` (0 = flush with the back edge).

Wide bench rows: small 7, medium 5, large 2, plug tray 2, so every size gets interior slots surrounded on all sides.

### Working strip (agreed)
The front 0.4 m of a standard bench is a **workspace**: where you'd pot on in real life, and a handy spot to set
things down. It holds **things without plants**: tools, sacks, bundles and empty containers.
- **Size:** `workStrip.depthUnits` = 4 across, full bench length along (30 units on a 3 m bench). Per bench item, so it can
  be tuned or set to 0 (the wide bench has no strip). The planting area sits behind it (`planting.backOffsetUnits`).
- **What it accepts:** items listed in the bench's `workStrip.accepts` (data). Each listed item has a strip footprint
  `stripUnits` (along × across). First pass:

  | Item | Strip footprint (units) | Strip slots on a 3 m bench |
  |---|---|---|
  | Empty small pot | 2 × 2 | 15 × 2 |
  | Empty medium pot | 3 × 3 | 10 × 1 |
  | Empty plug tray | 6 × 4 (laid lengthwise) | 5 × 1 |
  | Carry tray (with or without its small pots) | 6 × 4 | 5 × 1 |
  | Watering can | 3 × 3 | 10 × 1 |
  | Grub sack | 3 × 3 | 10 × 1 |
  | Bundle (moss scrapings) | 2 × 2 | 15 × 2 |

  An empty large pot (6 × 6) doesn't fit a 4-deep strip, so it isn't listed. Changing the depth or the list is data only.
- **Slots:** same rule as the planting area (fixed slot grid per footprint, `col = u / along`, `row = v / across`, measured
  from the strip's left-front corner). Free if none of its units is used. Address: `(benchId, Strip, itemKind, col, row)`.
- **No plants on the strip**, with one exception. Anything holding a plant (a planted pot or plug tray, a caught plant)
  is rejected with `NoPlantsOnStrip`. **Exception: a carry tray may be set down on the strip with its pots in it**
  (that is how a player frees their hands mid-job). Pots in a carry tray keep draining water and growing as normal,
  but they are never neighbours. Other rejections: `NotAccepted` (not in the list), `NoSuchSlot`, `SlotOccupied`.
- **No neighbours:** strip items are never neighbours and play no part in adjacency, on the same or another bench.
- **Pot on at the strip:** holding a plant in its container, interact with an **empty bigger pot on the strip**. The plant
  moves into that pot, the **new pot (with the plant) goes into your hands**, and the old empty container goes back to the
  storage crate. Then put it into a planting slot. Same rejections as potting on in a slot (`plants.md`). Later, the potting
  animation plays here.
- **Picking up from the strip:** empty hands take the item, as from a slot. Anyone can take anything on any strip (shared).
- Strip items move with the bench (`MoveItem`) and block `PickUpItem` (`HasItemsOnTop`) like pots do.
- **Containers on the strip are empty**, so water drain doesn't matter there; an empty container's meter stays as it was.

### Carry trays
A carry tray (0.4 × 0.6 m) **moves** up to 6 small pots; it **never goes on a bench**. In bench view, take
small pots into the tray in your hands, carry it, then put the pots into slots one by one, or drop the
whole tray into a cart or the wagon. To free your hands, set the carry tray down on a bench's **working strip**
(pots and all, see Working strip). Setting it down on the ground is **dropped for now**; it may return with carts and the wagon.

## Moving a bench
`MoveItem(player, placedId, anchor, rotation)` moves a placed floor item. For a bench, every pot and tray in it moves
with it and keeps its `(containerSize, col, row)` slot. Same rejections as `PlaceItem` (ignoring the bench's own cells).
Neighbours are recomputed afterwards. `PickUpItem` on a bench that still holds pots is rejected with `HasItemsOnTop`.
