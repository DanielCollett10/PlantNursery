# Controls and interaction

## View
- **First person, always**, including building. A top-down zoom-out view may be added later, but nothing should depend on it.
- Mouse and keyboard first, using Unity's Input System so every action can be rebound. Gamepad comes later.

## Interaction
- The player looks at something. A raycast from the camera finds the target within **reach** (default 3.0 m, in data).
- The HUD shows a prompt for the target's main action (e.g. "E: Water", "E: Sow", "E: Use computer").
- The target can be a placed item, a single plug cell, a pot, a plant, the tap, the computer or the bed.
- Only one action is offered per target and held item. The held item decides which (see table).

## Hands
Each player has **one hand slot** and holds at most one item. Interact picks up the item you're looking at
(only if your hands are empty), and a separate drop/put-down key puts it back.

| Holding | Looking at | Action |
|---|---|---|
| Nothing | Holdable item (can, seed packet, tray, pot) | Pick up |
| Watering can with water | Plant in a cell or pot | Water that plant |
| Watering can | Tap | Refill can to full |
| Seed packet with seeds left | Empty plug cell | Sow one seed (one click = one cell; revisit after the M1 playtest) |
| Anything | Computer | Open shop UI |
| Anything | Bed | Sleep (see `time.md`) |

Picking up a tray or pot carries the plants in it. While you hold a surface item and aim at a bench, a ghost shows where it will land.
The ghost snaps to the surface mini-grid and auto-nudges to the nearest free spot (see `building.md`). The put-down key places it there.

## Build mode
- A key toggles build mode. A small UI Toolkit panel lists the items you own but haven't placed (the **build inventory**, see `economy.md`).
- After you choose an item, a ghost follows the aimed cell. It snaps to the grid on the layer that item belongs to.
- **R** rotates the ghost 90°. The ghost is **green** if the sim says the placement is valid, otherwise **red**, with the reason shown.
- Click places it. Looking at a placed item and pressing the pick-up key returns it to the build inventory, if allowed (see `building.md`).
