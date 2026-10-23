# 0014: How the architecture doc maps onto the existing code

**Context.** `source/architecture.md` names patterns (SimContext, StateMachine, factories, view catalog, event-driven
view sync) that partly exist under other names. It says to extend what exists, not rewrite it.

**Decision.**
- **`SimWorld` is the SimContext.** It grows to own the state, the systems in a fixed list, the clock, one seeded
  `SimRandom` and the `ContentDb`, plus one entity-id counter (ids are ints, never reused). No second context class.
- **Tick order** is explicit in `SimWorld`: Time → (Weather, later, `0013`) → Growth → Watering → Behaviour → Mobility → Orders → Economy.
  Growth reads the water meter as it was at the end of the previous tick.
- **`StateMachine<TState>`** (transition table, guards, an event per transition) is added in `Sim/Core` and used for plant
  lifecycle and mood. Existing code is not converted unless a task touches it.
- **Content stays in `Assets/StreamingAssets/Content/`** (`0004`); the doc's `Assets/Content` is not used.
- **Factories:** a sim `EntityFactory` builds containers and plants from definitions (purchases, starter kit, sowing, tests).
- **Views (proposed, awaiting the human's OK):** views **reconcile from sim state** (the current `WorldView` approach), with a
  `ViewCatalog` mapping definition ids to prefabs under it. **Events are only for moments**: sounds, a "taken" cue, a
  mood-change shake, a toast. Reason: in co-op, clients hold a copy of the state built from a snapshot plus updates, so one
  reconcile path serves solo, clients, joins and loads, and a lost or late event can't leave a stale view. If reconciling
  every frame gets slow, add per-entity version numbers and skip unchanged entities; don't switch to spawning from events.
- **Cameras:** Cinemachine virtual cameras per mode are allowed (Unity layer only).

**Consequences.** Tasks add to `SimWorld` and `WorldView` rather than creating parallel frameworks. Sim state stays plain
data (no delegates or Unity types) so it can be serialised into the snapshot later.
