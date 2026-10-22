# 0006: Units and scale

**Context.** Placeholders and final art must agree on sizes, and the sim works in grid cells.

**Decision.**
| Measure | Value |
|---|---|
| Grid cell | 0.5 m × 0.5 m (1 Unity unit = 1 m) |
| Surface sub-slot | 0.25 m × 0.25 m (2 × 2 per cell) |
| Player height / eye height | 1.8 m / 1.65 m |
| Bench top height | 0.8 m |
| Standard bench | 1 × 4 cells |
| Door and path width | 2 cells minimum |
| Interaction reach | 3.0 m (data) |

- Sim coordinates are integer cells `(x, z)` with the origin at the plot's south-west corner. Unity's world position = `cell × 0.5 + plotOrigin`.

**Consequences.** Art briefs use these numbers. Changing the cell size means re-checking every footprint.
