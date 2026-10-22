# 0002: Rename the NetSim template namespace to PlantNursery

**Context.** The repo was started from a network-sim template. Code, the asmdef, the test project and CLAUDE.md still say `NetSim`.

**Decision.**
- Sim code uses `PlantNursery.Sim` (and sub-namespaces), and Unity code uses `PlantNursery.Game`.
- The asmdef is renamed to `PlantNursery.Sim`, and the test root namespace to `PlantNursery.Sim.Tests`.
- CLAUDE.md and architecture.md are updated to match.
- This is done once, before any other code task (draft task `core-1`).

**Consequences.** All later tasks use the new namespaces. Nothing should mention devices, packets or links: those were template wording.
