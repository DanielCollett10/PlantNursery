# Plants and care

Every plant shares one base definition: growth stages, care needs, genes, pot sizes and value. Behaviour comes
on top as optional modules (`behaviour.md`). A plant with no modules is a plain nursery plant.
Species data is in `species.md`.

## Lifecycle
1. **Sow** seeds, spores or cuttings into a plug tray or a small pot (see Sowing below).
2. **Grow on** until rooted.
3. **Pot on** into bigger pots, where the species allows.
4. **Sell or harvest:** some species sell whole; others are harvested repeatedly and regrow.

A plant is an individual (id, species, genome, stage, stage progress, quality, counters). Hearthmoss is the
exception: a tray of moss is **one** plant covering the whole tray (`species.md`).

## Sowing (agreed: one system for every species)
One sowing rule set, driven by species data, so new species and new seed types are data only.
- **Hold a seed packet** (from the crate) and interact with an **empty container in a slot**. One click:
  - **Plug tray:** fills **every empty cell** at once (one seed per cell), or the whole tray as one plant for species
    with `sowing.trayMode` = `wholeTray` (Hearthmoss). If the packet runs out part-way, the cells sown so far stay sown.
  - **Pot:** sows **one** plant.
- **Where you can sow** is per species (`sowing.sowInto`). Lore: seeds start in plug trays or small pots, never straight
  into medium or large pots; bigger pots are reached by potting on. First pass: Hearthmoss `[plugTray]`,
  Chimebell `[plugTray, smallPot]`.
- A packet holds `sowing.packetSeeds` (Chimebell 6; Hearthmoss spores 1 = one tray). An empty packet disappears from your hands.
- The new plant starts in the species' first stage, which must allow that container.
- Rejections: `NoSeedPacket`, `PacketEmpty`, `NotSowable` (container not in `sowInto`), `ContainerFull`, `NotInSlot`.
- Extensibility: the tray/pot fill rule is one strategy per container kind and `trayMode`; cuttings or other seed types
  add a new packet kind or mode, not a new system.

## Containers
| Container | Slot | Water drain per in-game hour | Can charges per watering |
|---|---|---|---|
| Plug tray | 0.6 × 0.4 m | 8 | 1 |
| Small pot | 0.2 m | 6 | 1 |
| Medium pot | 0.3 m | 4 | 1 |
| Large pot | 0.6 m | 3 | 2 |

Data keys: `containers[].drainPerHour`, `containers[].wateringCost`.

**Containers are sim entities.** Each pot or plug tray has an id, its container type (size), its plants (one per pot;
one per cell in a tray, or one for the whole tray for Hearthmoss) and its water meter. Empty pots and trays exist as
containers too: they come out of the crate, sit in slots and are filled by sowing, potting on or re-potting.

**Water feedback (MVP).** Each container shows a small water gauge when its meter is below 30, and a clear dry cue at 0,
readable from the follow camera. Bench view shows the gauge for every container on the bench.

## Watering
- **The water meter belongs to the container** (pot or tray), 0–100. Every plant in a tray shares it.
- One watering sets the meter to 100 (`wateringAmount` = 100, capped at 100).
- The watering can holds `canCharges` = 10 charges; a watering uses the container's cost. Not enough charges → `CanEmpty`.
- Refill at a rain barrel to full in `refillSeconds` = 1 (Unity timing; the sim refill is instant). Water is free.
- Each tick: `meter -= drainPerHour / 60` (× night multiplier, `time.md`), floored at 0.

**At 0:**
| State | Rule | Data key |
|---|---|---|
| Dry | Meter = 0: growth stops | — |
| Wilted | Dry for 12 in-game hours continuously | `wiltAfterHours` |
| Dead | Wilted for 3 in-game days continuously | `dieAfterWiltedDays` |

Watering clears Dry and Wilted at once (the counters reset); growth resumes next tick. Wilted plants droop on screen.

## Growth
- Each species lists its **stages** with a duration in in-game days and the container sizes allowed for each stage (`species.md`).
- Each tick a living plant that isn't Dry or root-bound gains `1 / (stageDays × 1440) × seasonMultiplier` stage progress. At 1.0 it moves to the next stage.
- **Root-bound:** a plant that finishes a stage while its container is too small for the next stage stops at progress 1.0. Growth pauses with **no other harm** until it is potted on.
- Season multipliers come from the species (e.g. `good` 1.0, `slow` 0.5, `none` 0), using the zone's row if inside a structure.

## Potting on
Carry the plant in its old container to an **empty bigger pot sitting in a slot** and interact.
The plant moves across with all its stats; the old pot (now empty) goes back to the storage crate.
The empty bigger pot may also stand on a bench's **working strip**; then the newly potted plant ends up in your hands
(`building.md`, Working strip).
From a plug tray, one plant (the one in the cell the player picked, or the first ready cell) moves.
Rejections: `PotNotEmpty`, `PotTooSmall`, `PotTooBig` (more than one size up is allowed only if the species allows that size), `NotReady` (stage not finished).

## Value
- **Pot size caps value:** small ×0.5, medium ×0.8, the species' final size ×1.0 (`potValueCaps`). Sale prices in `economy.md` already include this.
- **Quality** has three steps: **poor ×0.6, good ×1.0, excellent ×1.4** (`qualityMultipliers`).

## Quality
- A plant starts at **good**.
- Care history: over the plant's life, the share of ticks it was not Dry. ≥ 0.9 → can reach **excellent**; < 0.6 → drops to **poor**. Thresholds are data (`qualityCareThresholds`).
- **Quality steps:** behaviour can knock a plant down one step (e.g. a Snapjaw grazing moss). Quality = care grade minus the steps lost, never below poor.
- Neglect costs value and time; it never ends the game.

## Death
A dead plant stays visible until a player clears it (compost). Clearing gives nothing and frees the container.
Escapes never kill plants.
