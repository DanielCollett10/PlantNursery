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
- **Mouse and keyboard:** the cursor points at a slot. **Gamepad:** d-pad or bumpers step through the slot grid
  (planting slots of the held item's size, or of any size when empty-handed, then the working strip). Same rules, two pointers.
- Stepping order: along the row (left/right), across rows (up/down), from the planting area's front row down into the
  working strip and back; on back-to-back benches, up from the back row continues into the facing bench. Starts at the
  slot nearest the player.
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
- Unity collects candidates in reach (each with its distance to the player, in metres); the **sim** picks the target
  with a query `SmartTarget.Pick(action, player, candidates)` (it's a rule). Feed: highest hunger (only plants with a
  Need above 0). Scrape: harvestable moss, then nearest. Catch: nearest escapee. Ties: lowest entity id.
  Unity then submits the normal command (`FeedPlant`, `ScrapeMoss`, `CatchPlant`) with the chosen id.
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
**Keyboard and mouse and gamepad from the start** (`docs/decisions/0012`). Every action has a binding on both; the
Input System's action maps per mode (Walk, Carry, BenchView, Build, UI) are rebindable. The last device used decides the
pointer (cursor or slot-stepping) and the button prompts. Every Unity feature task covers both devices.

## First-person debug view
The old first-person controller stays as a **debug option** (off by default, toggled from SimHost or a debug key in
the editor). It isn't maintained for gameplay features and nothing new may depend on it. Once the pre-pivot code
is removed, the debug view is **walk and look only**: its old raycast interactions (surface put-down, tap, computer) go away. We may revisit a first-person
view one day.
