# Economy

## Wallet
- One shared wallet per nursery, as a whole number of coins. Starting money is in data (default 200).
- A purchase is rejected with `InsufficientFunds` if the wallet would go below 0.

## Build inventory
- Shared per nursery: a count per item type of things owned but not placed.
- Starting inventory (data): 2 standard benches, 6 plug trays, 1 watering can, 1 tap, 1 computer, 1 bed, 1 delivery shelf.
  The tap, computer, bed and delivery shelf start already placed in the starter layout.

## Prices (first pass, all data)
| Item | Buy |
|---|---|
| Seed packet (6 seeds) | 10 |
| Plug tray | 15 |
| Standard bench | 40 |
| Small pot | 2 |
| Medium pot | 5 |

Sale prices (M2) are `basePrice[variety] × stageMultiplier × potMultiplier × qualityMultiplier`. Values are in data.

## Sell-back
Placed equipment can be sold back for `sellBackFraction` (default 0.5) of its price, rounded down. This comes later, not in M1.

## Soft-lock guard: "a neighbour's gift" (MVP)
A cosy game must never leave players stuck. The check runs once at each `DayStarted`.

**Trigger:** all of these are true:
1. wallet < price of the cheapest seed packet,
2. no living plant exists anywhere,
3. no seed packet with ≥ 1 seed exists anywhere (shelf, hands or surfaces).

**Effect:**
- One free seed packet of the cheapest variety is delivered to the delivery shelf, using the normal shelf placement.
- If the nursery owns no plug tray at all (placed, carried or in inventory), one free plug tray is added to the build inventory.
- Raises a `NeighbourGift` event. The HUD shows a short message: "A neighbour left you some seeds."
- If the delivery shelf has no free spot, the packet goes to the nearest free spot on any surface. If there is none, the gift waits until the next morning.

**Data:** `softLockGuard.enabled` (default true), `softLockGuard.giftVarietyId` (default: cheapest).

## Open
- Running costs (water, heating): none in the MVP. Revisit after M2.
