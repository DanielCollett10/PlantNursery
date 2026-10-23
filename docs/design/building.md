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
| Large | 6 × 6 ⚠ | 0.6 × 0.6 | 1 | 5 | 2 |
| Plug tray | 6 × 4 (4 along, 6 across) ⚠ | 0.4 × 0.6 | 1 | 7 | 2 |

⚠ = being confirmed with the human (see Open points). All sizes are data (`slotSizes`).

- **Pots snap to slots.** A bench can mix sizes along its length (a run of small, then medium, then large); each run uses its own row layout.
- Slot positions along the bench: **see Open point B.**
- A slot is free if none of its units is used.

### Neighbours
- Two pots are **neighbours** if their slots **share an edge of at least one unit** (left/right in a row, front/back across rows). Diagonal corners don't count.
- **Benches that touch share neighbours.** If two benches stand back to back (long sides touching on the floor grid) the facing edge rows count as neighbours wherever their slots overlap across the join, even with different pot sizes. End to end works the same for the end slots. A gap of one floor cell or more keeps benches apart.
- Implementation hint: express every planting unit in one nursery-wide unit space, so "shares an edge" works the same within a bench and across a join.
- Neighbours are recomputed when a pot or bench is placed, moved or removed.
- **The trade-off:** small pots pack in more plants and more neighbour interactions; large pots are worth more and easier to keep apart.

### Bench types
| Bench | Footprint | Planting area | Buy | Unlock |
|---|---|---|---|---|
| 3 m bench | see Open point A | 3.0 × 0.6 m (30 × 6 units) | 40 | Start |
| 4 m bench | see Open point A | 4.0 × 0.6 m (40 × 6 units) | 50 | Tier 2 |
| Wide bench | 3 × 8 cells (1.5 × 4 m) | 4.0 × 1.5 m (40 × 15 units) | data | Tier 3 |

Wide bench rows: small 7, medium 5, large 2, plug tray 2, so every size gets interior slots surrounded on all sides.

### Carry trays
A carry tray (0.4 × 0.6 m) **moves** up to 6 small pots; it **never goes on a bench**. In bench view, take
small pots into the tray in your hands, carry it, then put the pots into slots one by one, or drop the
whole tray into a cart or the wagon. A carry tray can be set down on the ground (it then sits on the floor
cell under the player and blocks nothing).

## Open points (to confirm)
- **A. Bench depth vs the 0.5 m floor grid.** A 0.6 m deep bench is not a whole number of floor cells, so two benches can't sit exactly back to back. Suggested: the bench footprint is **1.0 m deep (2 cells)**, with the 0.6 m planting area along the **back** edge and a 0.4 m working edge at the front. Back-to-back benches then put their planting areas against each other.
- **B. Slot positions along the bench.** Suggested: slots of each size snap to multiples of their own width from the bench's start (small every 2 units, medium every 3, large/tray every 6/4). Mixing sizes may leave a small gap; runs that change size on a 0.6 m boundary never do.
- **C. Large pot size.** The drawing shows six 0.5 m large pots per 3 m; the spec says five 0.6 m. Suggested: 0.6 m, so all sizes line up every 0.6 m.
