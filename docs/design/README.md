# Game design

_The human owns this folder. The lead writes up agreed decisions here; dev agents read it and don't make design decisions._

**PlantNursery** is a cosy 1–4 player co-op **medieval fantasy** plant nursery for Steam, seen from above.
Players breed, grow and ship misbehaving magical plants to the guilds of a medieval town.
The source design is `source/medieval-fantasy-plant-nursery-design-document.md` (23 Oct 2026; same content as
the earlier `source/planetnursery-updated-with-theming.md`), with `source/architecture.md` for technical patterns and
`source/grid.png` for the bench worktop layout. The files below are the source of truth when they differ from them. The older first-person design (`source/plantnursery-design-document.md`)
is history only; see `docs/decisions/0009` and `migration.md`.

| File | System |
|---|---|
| `overview.md` | Pitch, pillars, core loop, MVP scope, prototype, milestones |
| `controls.md` | Cameras, bench view, targeting, hands, build mode |
| `time.md` | Clock, day shape, seasons, sleep, night rules |
| `building.md` | Floor grid, layers, structures, **benches and slots**, neighbours, carry trays |
| `plants.md` | Base plant: containers, watering, growth, potting on, quality, death |
| `species.md` | Hearthmoss, Chimebell, Snapjaw |
| `behaviour.md` | Behaviour modules (Need, Mood, Neighbour effect, Mobility), chaos rules, Snapjaw numbers |
| `genetics.md` | Genes, phenotype, breeding (later) |
| `sales.md` | Buyers, shop counter, guild contracts, departures, stock ledger |
| `economy.md` | Purse, prices, quality, starting kit, storage, soft-lock guard |
| `logistics.md` | Container chain, carrying, carts, wagon loading |
| `progression.md` | XP, unlock tracks, new species |
| `coop.md` | Host authority, joining, clashes, disconnects |
| `visuals.md` | Placeholder colours, mood cues, scale |
| `world.md` | The town and wild zones (post-MVP) |
| `open-questions.md` | Undecided questions with suggested defaults |
| `migration.md` | What from the first-person build no longer fits |

**Numbers are defaults.** Every number in these docs lives in content data (`docs/decisions/0004`)
so it can be tuned in playtests without code changes. Where a doc gives a **data key**, use that name
unless the content task says otherwise.
