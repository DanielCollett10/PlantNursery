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
| M2 (proposed) | Snapjaw prototype | See below |

**M2 Snapjaw prototype (agreed scope).** Solo, keyboard and mouse. Top-down follow camera with zoom-out on a small
greybox nursery with the starter kit; a second bench can be bought and placed back to back.
- **In:** Hearthmoss (sow, water, scrape, regrow), Chimebell (sow, pot on through sizes), Snapjaw (all four modules);
  bench slots and neighbours across back-to-back benches; bench view (place, pick up, pot on, carry tray, click-drag
  watering, neighbour lines); smart targeting for feed, scrape, catch; grub feeding from the storage crate; catch and
  re-pot; order desk supplier panel; mood indicators; off-screen alerts; a loading-yard rush zone with a fixed camera
  (no wagon or contracts yet); first-person controller kept as a debug view.
- **Out:** co-op, gamepad, **selling of any kind** (shop counter, contracts, selling bundles or plants), wagon, carts,
  saves, cold frames, breeding. Coin only goes down in M2; the starting 200 is enough for the prototype's purchases.

Later milestones (contracts and wagon, co-op, saves) are planned after the prototype playtest.
