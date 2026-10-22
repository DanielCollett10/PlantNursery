# Logistics

## The container chain
Goods are containers inside containers from day one:
`plant → plug tray / pot → trolley → (pallet, later) → vehicle`.

- Every holder (player hand, bench surface, trolley, vehicle bed) is a **holder** in the sim with a capacity.
  Moving goods means moving an item from one holder to another, and the sim rejects moves that would go over capacity.
- The sim owns *who holds what*. Unity owns *where bodies are* (see `docs/decisions/0003-sim-unity-boundary.md`).

## MVP handling
- **Hands:** one item (see `controls.md`).
- **Trolley (M4):** a pushable Floor-sized item with a shelf capacity in surface sub-slots (default 2 × 8 cells' worth). Pots and trays go on and off it by hand.
- **Vehicle (M4):** a pickup truck or wagon with a bed capacity. Goods are loaded by hand from hands or the trolley.

## Later
Pallets, pallet jack, forklift (physics). These add links to the chain. Orders and economy don't change.
