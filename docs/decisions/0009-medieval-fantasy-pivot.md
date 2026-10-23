# 0009: Pivot to a medieval fantasy nursery with plant behaviour

**Context.** The first design was a modern, first-person plant nursery. The new design
(`docs/design/source/planetnursery-updated-with-theming.md`, 23 Oct 2026) keeps its systems
but moves it to a soft medieval fantasy setting and adds plants that behave and misbehave.

**Decision.**
- The game is a cosy co-op **medieval fantasy** nursery. `docs/design/` is rewritten to match; the old
  design document stays in `docs/design/source/` for history only.
- **Kept:** the pure C# sim (`Assets/Sim`), ticks, commands and events, JSON content and validation,
  floor grid and placement, wallet and supplier, hands, genes and phenotype, game clock and sleep,
  growth as a system, soft-lock guard, day-night lighting and the greybox approach.
- **Added:** a behaviour layer of data-driven modules (Need, Mood, Neighbour effect, Mobility) ticked
  after growth; bench **slots** with neighbours; escaped plants as sim entities with grid positions.
- **Replaced:** modern items become themed ones (tap → rain barrel, computer → order desk, delivery
  shelf → storage crate, trolley → handcart, website orders → guild contracts, poly tunnels → cold frames
  and glasshouses). The old content data (`varieties.json`, the leaf-colour genes) is replaced by the
  three species Hearthmoss, Chimebell and Snapjaw.
- Every number in the Mechanics spec is a starting value that lives in content data.

**Consequences.** Some built features need rework; the list is in `docs/design/migration.md`.
Escaped plants are the first sim objects that move between cells, which extends 0003 (see 0010).
