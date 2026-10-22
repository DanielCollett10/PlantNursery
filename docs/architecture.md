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
- `Sim/Time/GameClock`: `GameClock.Install(world, timeConfig)` adds the clock system (raises `DayStartedEvent`, `SeasonChangedEvent`) and the `SleepCommand` handler. Tick 0 = Spring day 1 at `dayStartMinute`; days roll at `dayStartMinute`. Sleep (Evening/Night only, else `RejectReason.NotTired`) calls `SimWorld.RunUntil`, which makes `Step` keep stepping tick by tick until the next day start.
- `Sim/Genetics/`: `Genome` holds two alleles per content gene (copied; `Genome.FromVariety(db, varietyId)` copies a variety exactly). `Phenotype` has the pure rules: Mendelian = most dominant allele (earlier in the gene's `alleles` list), Blend = mean. `GenomeException` for unknown genes, alleles or varieties.
- `SimWorld` owns the tick clock. `Step(n)` advances time; nothing else does. 1 tick = 1 in-game minute.
- Commands: `Submit(cmd)` queues; each `Step` tick first applies queued commands in submission order (validate, then apply, via the `ICommandHandler<T>` registered with `RegisterHandler`), then updates `ISimSystem`s. Rejections at apply time raise `CommandRejectedEvent`. `Validate(cmd)` is query-only. A paused world keeps commands queued.
- Events: handlers and systems call `Raise(ISimEvent)`; the Unity layer calls `DrainEvents()` each frame.

## Unity layer (planned for M1)
- `SimHost` MonoBehaviour owns the `SimWorld`, loads content, steps ticks at the real-time pace from `time.json`, and drains events.
- View scripts mirror sim state into prefabs (placeholder mesh as a child). The first-person controller sends commands.

## Data flow

Player input → Unity layer → Sim command → `SimWorld.Step` → Sim state + events → Unity layer renders.
