# Economy

## Purse
One shared purse per nursery, whole coins, starting at `startingMoney` = 200. A purchase that would go below 0
is rejected with `InsufficientFunds`. **No fixed daily costs in the MVP;** water is free, grubs are the running cost.

## Prices (first pass, all data)
| Item | Buy | Sell |
|---|---|---|
| Small / medium / large pot (empty) | 2 / 4 / 8 | — |
| Plug tray | 5 | — |
| Hearthmoss spores (one tray) | 10 | Bundle: 4 |
| Chimebell seeds (6) | 12 | Medium in bloom: 15 · large specimen: 35 |
| Snapjaw seedling (in a small pot) | 15 | Mature: 30 · tooth: 2 |
| Grubs (one feed) | 1 | — |
| Bench 3 m / 4 m | 40 / 50 | Sell back at half price |
| Carry tray (6 small pots) | 6 | — |

- Sale price = listed price × quality multiplier (poor 0.6, good 1.0, excellent 1.4), rounded down. The pot-size cap (`plants.md`) is already in the listed prices.
- Supplier purchases arrive in the storage crate at once.

## Starting kit
One 3 m bench, 6 plug trays with Hearthmoss spores, 2 carry trays, a watering can, a rain barrel, the storage
crate, the order desk with its ledger, the shop counter, a bed, and 200 coin. (Data: `starterKit`.)
The 6 plug trays start **sown with Hearthmoss, in the bench's plug-tray slots, watered to 100**.

## Taking stock from the crate
Interacting with the storage crate (empty hands) opens a small list of what it holds; choosing an item puts **one**
into your hands (an empty pot, a plug tray, a seed packet, a carry tray, a Snapjaw seedling in its pot). **Grubs**
come out as a **sack of up to `grubSackPortions` = 5 portions** (fewer if the crate holds fewer); each feed uses one.
Putting an item back into the crate (interact while holding it) returns it, including unused grub portions.

## Storage crate
Unlimited in the MVP. Holds bought stock, empty pots and harvested bundles. A capacity and a bigger storeroom can come later.

## Soft-lock guard: "a neighbour's gift" (built, rethemed)
Runs at each day start. If the purse is below the cheapest spores/seed price **and** no living plant exists
**and** no unsown spores or seeds exist anywhere, one free pack of Hearthmoss spores is put in the storage crate
(plus a plug tray if the nursery owns none). Raises `NeighbourGift`; the HUD shows "A neighbour left you some spores."

## First-hour check
Six trays of Hearthmoss give 6 bundles (≈24 coin) a day. A first 10-bundle contract pays ≈48 coin and 24 XP and
takes about two days. The handcart unlocks at 20 XP, so it lands around day 2–3 (20–30 real minutes).
The balancing harness checks this.
