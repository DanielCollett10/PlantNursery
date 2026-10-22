# Sales

## Supplier (M1)
- The shop computer opens a UI Toolkit shop. Players buy seed packets and equipment with the shared wallet.
- **Delivery is instant in the MVP:**
  - Equipment goes into the build inventory.
  - Seed packets appear on the **delivery shelf** next to the computer as holdable items. If the shelf is full, the purchase is rejected with `DeliveryShelfFull`.
- Supplier visits come later.

## Shop front (M2)
- Only plants on **display shelving** (a Floor item with surface) are for sale. Everything else is growing stock.
- During opening hours, walk-in customers arrive at a rate set in data. Each one browses, chooses 1–5 sellable plants on display, and queues at the till.
  - Customer preferences by season come later. In M2, choices are random from what's on display, using a seeded RNG.
- A player must **interact with the customer at the till** to complete the sale. The money goes to the wallet and the plants leave.
- Customers don't need supervising otherwise. There is no theft.
- A queued customer waits up to `customerPatienceMinutes` (default 120). If nobody serves them, they put the plants back and leave, with no penalty.
- If a customer finds nothing they want, they leave.

## Website orders (M4)
- Orders arrive on the shop computer with a spec (variety, minimum stage/pot, minimum quality, quantity), a price per plant, XP and a pickup time.
- Players accept the orders they want. The pickup schedule is visible days ahead.
- At pickup time a vehicle arrives, waits until departure, then leaves with whatever is loaded.
- **Partial loads:** the order pays for each loaded plant that meets the spec. Plants that don't meet the spec are left behind, not taken.
  XP scales with the fraction fulfilled. When reputation exists, it drops by the unfulfilled fraction.
- Missing an order entirely: the order lapses. No money or stock is lost.
- Rush orders come later.

## Reputation
Not in the MVP. It may later drive unlocks or order size (undecided).
