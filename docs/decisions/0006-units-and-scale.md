# 0006: Units and scale

**Context.** Placeholders and final art must agree on sizes, and the sim works in grid cells.

**Decision.**
| Measure | Value |
|---|---|
| Grid cell | 0.5 m × 0.5 m (1 Unity unit = 1 m) |
| Surface mini-grid unit | 0.1 m × 0.1 m (5 × 5 per cell) |
| Player height / eye height | 1.8 m / 1.65 m |
| Bench top height | 0.9 m (half player height) |
| Standard bench | 2 × 4 cells (1 m × 2 m) |
| Door and path width | 2 cells minimum |
| Interaction reach | 3.0 m (data) |

- Sim coordinates are integer cells `(x, z)` with the origin at the plot's south-west corner. Unity's world position = `cell × 0.5 + plotOrigin`.
- Surface items use integer mini-grid units `(u, v)` = cell × 5 + offset. World position = `unit × 0.1 + plotOrigin`, at the height of the supporting item's top.

**Update (0009/0010 pivot).** Grid cell and 0.1 m unit stay. Standard benches are now 3 m and 4 m long with a
0.6 m planting area (footprint depth: `docs/design/building.md`, open point A). Reach is 1.5 m (smart targeting).
Eye height is unused (no first person). Scale table: `docs/design/visuals.md`.

**Consequences.** Art briefs use these numbers. Changing the cell size means re-checking every footprint.
