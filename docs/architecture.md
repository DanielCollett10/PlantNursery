# Architecture

_Living document. Agents update it when they change how parts fit together._

## Layers

| Layer | Folder | Depends on | Tested by |
|---|---|---|---|
| Simulation core | `Assets/Sim/` (asmdef `PlantNursery.Sim`, no UnityEngine) | Newtonsoft.Json only | `Sim.Tests/` via `dotnet test` |
| Unity layer | `Assets/Game/` | `PlantNursery.Sim`, UnityEngine | Unity EditMode/PlayMode tests |
| Editor tooling | `Assets/Editor/` | UnityEditor | by hand / editor lane |
| Content data | `Assets/StreamingAssets/Content/*.json` | — | loaded and validated by the sim |

Key decisions: `docs/decisions/0003` (Unity owns bodies, the sim owns things),
`0004` (JSON content), `0005` (commands in, events out), `0006` (units).

## Simulation core (planned for M1)

| Folder | Contents |
|---|---|
| `Sim/Core/` | `SimWorld` (tick clock, command queue, event list), `ICommand`, `CommandResult`, `PlayerId` |
| `Sim/Content/` | Content definitions and `ContentLoader` |
| `Sim/Time/` | `GameClock`: day, minute of day, phase, season; sleep |
| `Sim/Building/` | `Grid`, layers, footprints, placement, `BuildInventory` |
| `Sim/Economy/` | `Wallet`, supplier purchases |
| `Sim/Plants/` | `Plant`, containers (plug tray, pot), growth system |
| `Sim/Genetics/` | Genes, alleles, phenotype |
| `Sim/Hands/` | Per-player held item, watering can |

- `Sim/Content/`: `ContentLoader.Load(filesByName)` takes JSON strings (`time`, `items`, `containers`, `genes`, `varieties`, `economy`, `building`, optional `time.demo`) from `Assets/StreamingAssets/Content/`, validates them and returns an immutable `ContentDb`. Errors throw `ContentException` (file + field path).
- `Sim/Building/`: `BuildState` holds the floor grid (plot size from `building.json`), placed items and the shared `BuildInventory` (seeded from `economy.starterInventory`). `CanPlace`/`CanPickUp` are query-only and return a `BuildRejection`; `PlaceItem`/`PickUpItem` commands (registered via `BuildingModule.Register`) use the same checks and raise `ItemPlaced`/`ItemPickedUp`. The rejection name is the `CommandResult.Message`. `SurfaceGrid` (constructed with `ContentDb` + `BuildState`, which it registers as the `ISurfaceOccupancy`) is the 0.1 m mini-grid: `Unit(u,v)` = cell*5+offset across the plot; a unit is usable only over a placed floor item with `providesSurface`, so touching benches form one surface. `CanPlace/Place` take rotation-aware footprints and reject `Occupied` then `NoSurface`; `FindNearestFreeSpot(item, rotation, desired, radius)` returns the nearest valid anchor (squared Euclidean, ties smallest v then u) or null. `PickUpItem` on a bench with surface items returns `HasItemsOnTop`. Surface commands/inventory wiring are not yet added.
- `Sim/Hands/`: `HandsState(ContentDb, SurfaceGrid)` holds one `HeldItem` per `PlayerId` and the `ItemContents` (plants, water charges) of surface items, keyed by surface item id. `PickUp` removes the surface item from the grid into the hand (rejects `HandsFull`, `NotHoldable`); `PutDown` re-places it via `SurfaceGrid.CanPlace/Place` (rejects `NothingHeld`, `NoSurface`, `Occupied`) and moves its contents to the new id, so plants stay attached to trays and pots. Cans are items with `waterCapacity` (items.json) and start full; `RefillCan` fills to capacity (`NoCan`); `WaterPlant` sets moisture 100 and uses a charge (`NoCan`, `CanEmpty`, `PlantDead`). Commands `PickUp/PutDown/RefillCan/WaterPlant` are registered by `HandsModule.Register`; the rejection name is the result message. `Sim/Plants/Plant` is a minimal stub (id, moisture, isDead) for the growth task to extend.
- `Sim/Economy/`: `Wallet` (shared coins, from `economy.startingMoney`, never below 0). `SupplierModule.Register(world, content, wallet, build, surface)` handles `BuyItem(player, itemId, qty)`: only items with price > 0; rejects `InsufficientFunds` (no change), `InvalidArgument` (unknown item / qty < 1) or `Blocked` with message `DeliveryShelfFull`. `seed_packet` is placed qty times on the placed `delivery_shelf` via `SurfaceGrid.FindNearestFreeSpot` (with an accept predicate keeping the packet on the shelf), as `SurfaceItem`s carrying `SeedCount` (`seedCount` in items.json); all-or-nothing. Everything else goes to `BuildInventory`. Raises `WalletChanged` and `ItemsPurchased`. `Validate` dry-runs shelf placement and rolls it back.
- `Sim/Time/GameClock`: `GameClock.Install(world, timeConfig)` adds the clock system (raises `DayStartedEvent`, `SeasonChangedEvent`) and the `SleepCommand` handler. Tick 0 = Spring day 1 at `dayStartMinute`; days roll at `dayStartMinute`. Sleep (Evening/Night only, else `RejectReason.NotTired`) calls `SimWorld.RunUntil`, which makes `Step` keep stepping tick by tick until the next day start.
- `Sim/Genetics/`: `Genome` holds two alleles per content gene (copied; `Genome.FromVariety(db, varietyId)` copies a variety exactly). `Phenotype` has the pure rules: Mendelian = most dominant allele (earlier in the gene's `alleles` list), Blend = mean. `GenomeException` for unknown genes, alleles or varieties.
- `SimWorld` owns the tick clock. `Step(n)` advances time; nothing else does. 1 tick = 1 in-game minute.
- Commands: `Submit(cmd)` queues; each `Step` tick first applies queued commands in submission order (validate, then apply, via the `ICommandHandler<T>` registered with `RegisterHandler`), then updates `ISimSystem`s. Rejections at apply time raise `CommandRejectedEvent`. `Validate(cmd)` is query-only. A paused world keeps commands queued.
- Events: handlers and systems call `Raise(ISimEvent)`; the Unity layer calls `DrainEvents()` each frame.

## Unity layer (planned for M1)
- Implemented: `Assets/Game/Host/SimHost.cs` (asmdef `PlantNursery.Game`). `Initialize()` loads `StreamingAssets/Content/*.json` (`UseDemoProfile` keeps/drops `time.demo.json`), builds SimWorld + GameClock + BuildState + SurfaceGrid + Wallet; `Advance(realSeconds)` steps ticks at `Clock.RealSecondsPerTick`; `Submit`/`Validate` pass commands; an accepted `SleepCommand` steps immediately to the next morning; `EventDispatched` relays drained events. `Plant Nursery/Generate Starter Scene` (`Assets/Editor/Scene`) writes `Assets/Scenes/Starter.unity`. EditMode tests: `Assets/Tests/EditMode`.
- `SimHost` MonoBehaviour owns the `SimWorld`, loads content, steps ticks at the real-time pace from `time.json`, and drains events.
- View scripts mirror sim state into prefabs (placeholder mesh as a child). The first-person controller sends commands.

## Data flow

Player input → Unity layer → Sim command → `SimWorld.Step` → Sim state + events → Unity layer renders.
