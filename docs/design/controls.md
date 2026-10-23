# Cameras, targeting and controls

See `docs/decisions/0010-top-down-cameras-and-bench-view.md`. There is **no first-person view**.

## Cameras (Unity only)
| Camera | When | Behaviour |
|---|---|---|
| Follow | Default | Top-down or angled, tracks the local player. Each player has their own |
| Zoomed out | Zoom button, or standing still for `autoZoomOutSeconds` (default 4) | Pulls back to show more of the nursery; any movement or input returns |
| Fixed rush | Player is inside a rush zone (loading yard; later the shop counter) | Pans to a fixed view of the whole zone, PlateUp style. Leaving the zone returns to Follow |
| Bench view | Player interacts with a bench | Pans to look straight down on that bench (and any bench touching it back to back) |

- Camera transitions are smooth pans (default 0.4 s), never cuts.
- The world is bigger than the screen. **Off-screen alerts:** an arrow at the screen edge plus a sound
  points at trouble out of view (a Restless or worse plant, an escapee).

## Bench view
- Entered by interacting with a bench while standing at it. Exited by walking away (more than `benchViewExitDistance`, default 1.0 m, from the bench edge). **No menu to close; movement stays live.**
- **Mouse and keyboard:** the cursor points at a slot. **Gamepad:** d-pad or bumpers step through the slot grid. Same rules, two pointers.
- The pointed slot lights up in the player's highlight colour; a held pot's ghost snaps into it (green valid, red invalid with reason).
- **Neighbour lines** show while holding a pot (see `behaviour.md`, Neighbours on screen).
- Back-to-back benches share one bench view so cross-bench neighbours are visible.

## Targeting
| Job | How the target is chosen |
|---|---|
| Water | **Bench view, click and drag** with the watering can held: every pot or tray the drag passes over is watered once per drag. Gamepad: hold the button while stepping through slots |
| Feed, scrape, catch (walking around) | **Smart targeting** within reach: hungriest plant for feed, most-ready moss for scrape, nearest escapee for catch |
| Place, pick up, pot on, fill a carry tray | **Bench view:** point at a slot and interact |
| Load the wagon or a cart | **Carry and drop:** walk to it holding an item and press interact |
| Inspect | Hover (or select in bench view): a card shows needs, mood, genes and neighbours |

- **Reach** for smart targeting: `reachMetres` = 1.5 from the player. Bench view needs the player standing at the bench.
- Unity collects candidates in reach; the **sim** picks the target (it's a rule). Ties: lowest entity id.
- Smart targeting skips a plant another player is already feeding or catching (see `coop.md`).
- A drag waters a pot at most once, even if the cursor passes it twice. Each watering uses can charges (`plants.md`); the drag stops watering when the can is empty.

## Hands
Each player holds **one thing**: one pot (any size, with or without a plant), one plug tray, one carry tray
(up to 6 small pots), one sack (e.g. grubs) or one caught plant. Players can't take from another player's hands.

| Holding | Target | Action |
|---|---|---|
| Nothing | Pot / tray in a slot (bench view) | Pick it up |
| Pot, plug tray | Empty slot (bench view) | Put it down |
| Plant in its pot | Empty bigger pot in a slot | Pot on (`plants.md`) |
| Carry tray | Small pot in a slot | Take it into the tray (if < 6) |
| Carry tray with pots | Empty small slot | Put one pot down |
| Watering can | Pots (drag) | Water |
| Watering can | Rain barrel | Refill (`refillSeconds` = 1) |
| Grubs sack | Hungry plant (smart target) | Feed one portion |
| Nothing | Escaped plant in reach | Catch it (it goes into your hands) |
| Caught plant | Empty pot in a slot | Re-pot it |
| Anything | Cart or wagon | Drop it in the next free space |
| Anything | Order desk | Open the ledger / supplier UI |
| Anything | Bed | Ready up for sleep |

## Build mode
- A key toggles build mode; a UI Toolkit panel lists owned, unplaced floor items.
- The ghost follows the cursor on the ground plane, snaps to floor cells, **R** rotates 90°, green/red from the sim's `CanPlace`.
- Click places; interacting with a placed item picks it up (a bench takes its pots with it, see `building.md`).

## Inputs
Keyboard and mouse **and** gamepad from the start, through the Input System, all rebindable.
