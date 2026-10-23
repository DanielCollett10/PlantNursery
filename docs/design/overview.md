# Overview

A cosy co-op fantasy nursery where 1–4 players breed, grow and ship misbehaving magical plants to the
guilds of a medieval town. Grid building, growth, genes, orders and co-op, with **plants that have behaviour** on top.

## Pillars
Every feature is judged against these; a feature that serves none of them waits.

1. **Co-op from the start.** 1–4 players share one nursery from the first build. Solo is the same game with one player.
2. **Cosy, with chaos you choose.** The everyday rhythm is calm. Chaos is the price of ambition: volatile,
   valuable plants bring trouble, and players decide how much to take on.
3. **Grow your nursery.** Start small and feel every upgrade, up to commercial scale.
4. **Plants with personality.** Plants have behaviour and temperament; breeding uncovers new traits and species.

## Core loop
Breed and grow → take contracts → survive the loading rush → unlock → slowly uncover what clients really
want the plants for. Each turn pays for equipment and bloodlines that make the next one bigger and calmer.

## Build order
1. **Shared systems:** grid, growth, watering, genes, orders, co-op. Works with plain plants (Hearthmoss, Chimebell).
2. **The prototype:** one bench, the Snapjaw with all four modules, a player who catches it, the follow camera
   with bench view, and a fixed camera for the loading yard. **If it's fun, the full MVP follows.**

## MVP scope
| Area | In the MVP | Later |
|---|---|---|
| Co-op | Steam host and join, mid-game join, sleep ready-up, shared purse and XP | — |
| Camera | Top-down follow camera, zoom-out, off-screen alerts, bench view, fixed rush cameras | Refinements |
| Building | Grid placement of benches and pots; cold frames | Glasshouse, warded enclosure, overhead watering |
| Plants | Hearthmoss, Chimebell, Snapjaw | More species (supplier, quests, wild, recipes) |
| Behaviour | Need, Mood, Neighbour effect, Mobility | More modules, tool-plants |
| Care | Watering can, feeding, re-potting caught plants | Lids, cages, sedating, wards |
| Genetics | Genes stored and shown | Breeding, accidental crosses, recipes, catalogue |
| Sales | Shop counter, guild contracts with departures and XP | Rush contracts, reputation tiers |
| Logistics | Hand carry, then handcart, onto a wagon | Mule cart, floating barrow, crates |
| Progression | First tier of each track | Higher tiers, land |
| World | The nursery | The town, wild zones |
| Time | Days, sleep, spring to autumn | Winter |
| Saves | Host save and load, autosave | — |
| Art | Coloured placeholders, mood indicators, day-night lighting | Stylised low poly |

## Milestones
| ID | Name | Playable result |
|---|---|---|
| M1 | First sprout | Done. First-person greybox (pre-pivot): buy, place, sow, water, grow, sleep |
| M2 (agreed) | Moss on the bench | All new sim rules (headless 5-day scenario), plus walking the top-down nursery and **tending the starter moss** from the walk view: pick up the can from the strip, refill at the barrel, water the driest tray, scrape moss, drop bundles in the crate, sleep at the bed. No sowing, buying or bench view yet (M3). The old computer shop is hidden in M2 |
| M3 (agreed) | A calm bench | Bench view, crate, order desk, inspect card, build mode, cleanup. See below |
| M4 (agreed) | Snapjaw prototype | See below; planned after M3 and its tweaks |

**M2 + M3 together, "A calm bench" (agreed scope).** Split in two so there is something to play halfway (M2). A solid base
before the complicated mechanics. One player on screen, but built
co-op ready (`coop.md`). Keyboard and mouse **and gamepad** (`docs/decisions/0012`). Top-down follow camera with zoom-out
on a small greybox nursery with the starter kit; a second bench can be bought and placed back to back.
- **In:** Hearthmoss (water, scrape into a bundle, regrow) and Chimebell (sow, pot on through sizes); bench slots,
  working strip and neighbours across back-to-back benches; bench view (place, pick up, pot on, carry tray, drag/hold
  watering, neighbour lines); smart targeting for scrape; storage crate (take/return); order desk supplier panel; rain
  barrel refill; water gauges; inspect card; bed and sleep; day-night lighting; retiring the pre-pivot code (`0011`).
- **Out:** Snapjaw and all behaviour, **selling of any kind**, wagon, carts, loading yard, networking, saves, cold
  frames, breeding, weather (`0013`). Coin only goes down; the starting 200 covers M2's purchases.
- **After M3:** a playtest and a round of adjustments to the foundation before M4 starts.

**M4 Snapjaw prototype.** Snapjaw with all four modules, grub feeding, mood indicators, off-screen alerts, catch and
re-pot, smart targeting for feed and catch, and a loading-yard rush zone with a fixed camera (no wagon yet).
**If it's fun, the full MVP follows.** Later milestones (contracts and wagon, co-op networking, saves) are planned after it.
