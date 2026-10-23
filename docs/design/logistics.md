# Logistics

## The container chain
Goods are containers inside containers from day one: **plant → tray or pot → cart → crate or pallet → wagon**.
Upgrades add links (handcart, mule cart, floating barrow) without changing how orders work.
Every holder (hands, bench slots, carry tray, cart, wagon, storage crate) is a sim holder with a capacity; moves
over capacity are rejected. The sim owns who holds what; Unity owns where bodies are (`docs/decisions/0003`).

## Hands
One thing per player (see `controls.md`). Players can't take from another player's hands.

## Carts and the wagon
They are **containers, not benches**: dropping an item fills the next free space, with no slot picking.
Capacity uses the bench slot sizes.

| Carrier | Bed | Small | Medium | Large |
|---|---|---|---|---|
| Handcart (unlock 1) | 0.6 × 1.2 m | 18 | 8 | 2 |
| Mule cart (later) | 0.6 × 2.4 m | 36 | 16 | 4 |
| Wagon | 1.2 × 3 m (two 0.6 m beds) | 90 | 40 | 10 |

Mixed loads: each bed is filled like a bench planting area (same mini-grid, next free slot from one end).

## Loading (PlateUp style)
Carry one item at a time (a pot, a plug tray, a carry tray of up to 6 small pots, a bundle) to the wagon and
press interact to drop it in. A cart parked near the wagon shortens the walk; it never loads anything by itself.
With several players, loading becomes a relay. The loading yard is a rush zone with a fixed camera (`controls.md`).
An escaped plant on the loading yard is the classic rush-hour problem.
