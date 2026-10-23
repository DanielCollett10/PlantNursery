# Buyers, orders and the shop

Three kinds of buyer want plants for different reasons; wagon departures are where the day's pressure peaks.

| Buyer | Use | Values |
|---|---|---|
| Alchemists and apothecaries | Ingredients harvested repeatedly (sap, petals, spores) | Potency |
| Collectors and noble gardens | Live specimens | Looks and rarity |
| The town | Working plants (guard hedges, pest-eaters, lantern blooms) | Temperament and a specific behaviour |

## Supplier (order desk)
The order desk opens a UI Toolkit panel to buy stock with the shared purse. **Purchases arrive in the storage
crate by the order desk at once** (unlimited capacity in the MVP). Floor items go to the build inventory.

## Shop counter (walk-ins)
- Benches can be **marked for sale**. During opening hours (`time.md`) `walkInsPerDay` = 4–8 customers arrive (seeded RNG).
- Each buys 1–3 sellable plants from marked benches and **pays automatically at the till**. No theft, no penalty if nothing suits.

## Guild contracts
| Setting | First pass | Data key |
|---|---|---|
| Guild board | 3 offers, refreshed every morning | `board.offers` |
| Courier letters | About one every 3 days, bigger and better paid | `courier.everyDays` |
| A contract asks for | Species, minimum pot size, minimum quality, quantity (5–20) | — |
| Pay | 1.2 × the plants' sale value | `contractPayMultiplier` |
| XP | Half the contract's pay | `xpPerCoin` = 0.5 |
| Pickup | Players choose the day, 2–7 days ahead | `pickupWindowDays` |
| Part-loads | Pays for what is loaded if ≥ half the quantity; below half = missed | `partLoadMinFraction` = 0.5 |
| Reputation | +1 full, 0 partial, −1 missed (per guild) | — |

- **Departures are booked, not daily.** On the pickup day the wagon arrives, waits until its departure time (data), then leaves with whatever is loaded. Several departures can stack on one day.
- Only items meeting the spec count; others are left behind.
- A missed departure lapses the order and dents reputation; **no money or stock is lost**.
- Rush contracts (tighter departure, better pay, optional) come later. Reputation tiers come later.

## Stock ledger
The order desk's ledger lists stock by species, pot size and quality, next to each accepted contract's needs and
pickup day, plus empty pots and trays in the storage crate. Booked departures show on a calendar.
