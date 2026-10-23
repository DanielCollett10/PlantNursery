# What changes from the first-person build

The pivot (`docs/decisions/0009`, `0010`) keeps the sim's foundations and replaces the parts below.
Dev agents: don't extend anything marked **Retire**; tasks will remove or replace it.

## Keep as is
| Built | Where | Notes |
|---|---|---|
| `SimWorld` ticks, pause, commands, events | `Sim/Core` | Unchanged |
| Content loader and validation | `Sim/Content` | New files/fields added; loader pattern stays |
| Floor grid, footprints, rotation, `PlaceItem`/`PickUpItem` | `Sim/Building` | Bench pick-up must now carry its pots |
| Wallet, `BuyItem` | `Sim/Economy` | Destination changes (storage crate) |
| `Genome`, `Phenotype` | `Sim/Genetics` | New genes are data |
| `GameClock`, sleep, seasons | `Sim/Time` | New numbers; night rules added |
| Day-night lighting | `Game/Lighting` | Unchanged |
| SimHost, WorldView reconcile, greybox generator, PanelSettings/UI Toolkit setup | `Game/`, `Editor/` | Unchanged pattern |
| Build mode ghost (ground-plane ray) | `Game/Building` | Works with a top-down camera |

## Rework
| Built | Why | Becomes |
|---|---|---|
| `SurfaceGrid` free 0.1 m mini-grid with auto-nudge, items straddling benches | Pots now snap to **slots in rows per pot size**, belong to one bench, and need neighbours | Bench slot model + neighbour query (`building.md`). The 0.1 m unit space can be reused |
| `HandsState` (pick up, put down, can, tap) | Put-down targets slots; can has 10 charges with per-container cost; new held kinds (carry tray, sack, caught plant, bundle) | Extend |
| `Plant` + `GrowthSystem` (growth points, per-plant moisture, health, root-bound damage, A/B/C grade) | Water meter is per **container**; stages in **days**; Dry → Wilted (12 h) → Dead (3 days); root-bound pauses with no harm; quality is poor/good/excellent with steps lost | Rewrite rules to `plants.md`; keep the system shape and events |
| `Sow` with seed packets on the delivery shelf | Stock arrives in the **storage crate**; Hearthmoss is one plant per tray; Snapjaw bought as a potted seedling | Rework |
| `DeliveryShelf`, `SupplierModule` placement | Storage crate is unlimited, no shelf placement | Simplify |
| `SoftLockGuard` | Spores, storage crate, new text | Retheme |
| `time.json` / `time.demo.json` | 06:00–22:00 in 10 real minutes | New values |
| `InteractionHud` and computer shop UI | Order desk UI; plant inspect card; mood/alerts | Retheme and extend |
| `WorldView`/`PlantView` placeholders | Species shapes, gene tints, mood indicators, escapees | Extend |
| Decision 0006 (units) | Bench size, reach 1.5 m, eye height unused | Updated note added there |

## Retire
| Built | Why |
|---|---|
| `FirstPersonController`, camera-raycast `InteractionController`, `InteractableTarget` | No first person (0010). Replaced by a top-down player controller, follow camera, smart targeting and bench view |
| 3 m interaction reach | Now 1.5 m smart targeting + bench view |
| Old content: `varieties.json` (leaf-colour line), `genes.json` (`leafColour`, `variegation`, `spotSize`), items tap / computer / delivery shelf / seed packet, 1 × 2 m standard bench | Replaced by Hearthmoss/Chimebell/Snapjaw, new genes, rain barrel, order desk, storage crate, 3 m bench |
| Old design docs: first-person `controls.md`, website orders, trolley, poly tunnels, display shelving | Rewritten in this folder |
| Old milestones M2–M5 (First sale, Together, Orders, Keep going) | Re-planned after the Snapjaw prototype |
