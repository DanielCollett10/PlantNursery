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

## Open
- Running costs (water, heating) and a soft-lock guard (broke with nothing growing) are still undecided. There are none in M1.
