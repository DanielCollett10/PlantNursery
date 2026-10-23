# Medieval Fantasy Plant Nursery — Design Document

Oct 23, 2026 · @Dan

A cosy co-op fantasy nursery where 1–4 players breed, grow and ship misbehaving magical plants to the guilds of a medieval town. It keeps the plant-nursery game's systems (grid building, growth, genes, orders, co-op) and adds plants with behaviour on top.

## Pillars

Every feature is judged against these four pillars; a feature that serves none of them waits.

1. **Co-op from the start.** 1–4 players share one nursery from the first build. Solo play is the same game with one player.
2. **Cosy, with chaos you choose.** The everyday rhythm is calm. Chaos is the price of ambition: volatile, valuable plants bring trouble, and players decide how much to take on.
3. **Grow your nursery.** Players start small and feel every upgrade make them more capable, up to commercial scale.
4. **Plants with personality.** Plants have behaviour and temperament, and breeding uncovers new traits and species.

## Core loop

&#91;embedded content: core loop · 6 steps\]

Breed and grow, take contracts, survive the loading rush, unlock, and slowly uncover what clients really want the plants for. Each turn of the loop pays for equipment and bloodlines that make the next one bigger and calmer.

## Theme, look and camera

Soft medieval fantasy: warm, stylised, likely low poly, cosy rather than grim. Players see the nursery from above, each with their own camera.

| Thing | In this setting |
| --- | --- |
| Orders | Guild notice board and courier letters |
| Shop counter | Order desk and ledger |
| Moving stock | Handcart → mule cart → enchanted floating barrow |
| Growing structures | Cold frames, glasshouses, warded enclosures for volatile stock |
| Deliveries | Wagons and couriers |

**Camera:**

- **Follow camera everywhere except rush areas:** top-down or angled, following each player (online co-op, so everyone has their own camera).
- **Rush areas have a fixed camera.** Walking into the loading yard (and other rush hotspots such as the shop counter) pans the camera to a fixed view of the whole area, PlateUp style. Walking out returns to the follow camera.
- **Bench view:** interacting with a bench pans the camera to look straight down on it (see Targeting and controls).
- The world is bigger than the screen. Zoom out with a button, or automatically after standing still for a few seconds.
- Edge-of-screen alerts and sound flag trouble out of view.

## Targeting and controls

Walking around, care actions target automatically. Detailed work on a bench happens in **bench view**, a closer camera over that one bench. Players never have to line up on a 0.2 m pot from the wide view.

| Job | How the target is chosen |
| --- | --- |
| Water | **In bench view, click and drag** across pots with the watering can equipped; each pot the drag passes over is watered. Gamepad: hold the button while stepping through slots |
| Feed, scrape, catch (walking around) | **Smart targeting:** the neediest plant within reach (hungriest for feed, nearest escapee for catch) |
| Place, pick up, pot on, fill a carry tray | **Bench view:** interact with a bench and the camera pans to look straight down on it. Mouse and keyboard: point at a slot. Gamepad: step through the slot grid with the d-pad or bumpers. The slot lights up and the held pot's ghost snaps into it |
| Load the wagon | **Carry and drop,** PlateUp style: walk to the wagon holding an item and press interact |
| Inspect a plant | Hover (or select in bench view): a small card shows needs, mood, genes and neighbours |

- **Bench view never locks the player in.** Movement stays live: walking away from the bench pans the camera back out to the normal view. There's no menu to close.
- **Back-to-back benches** share one bench view, so cross-bench neighbours are visible while placing.
- **Neighbour lines** show in bench view while holding a pot.
- **Reach:** about 1.5 m from the player for smart targeting; bench view needs the player standing at the bench.
- **Both inputs from the start:** keyboard and mouse use the cursor in bench view; gamepad uses slot-stepping. The same rules, two pointers.
- **Co-op:** each player has their own camera, bench view and highlight colour. Two players can work the same bench at once; the first-request-wins rule settles clashes.

## Co-op

The host's game is the single authority on the nursery, and up to three friends join it through Steam.

| Rule | Decision |
| --- | --- |
| Players | 1–4. Solo is a session with one player |
| Money | Shared purse |
| XP and unlocks | Shared, owned by the nursery |
| Sleeping | Every player readies up to end the day. The host can force it if someone is away |
| Joining | Players can join mid-game |
| Saves | Belong to the host. Autosave often, on the host only |
| Host leaves | The session ends for everyone. Clients return to the menu, progress saved in the host's game |
| Platform | Steam: friend invites and lobbies through Steam's networking, no servers to run |

- **Requests in, results out.** Clients send requests ("place bench", "feed Snapjaw", "catch plant"). The host checks each one, applies it and sends back what changed.
- **Plants sync cheaply.** Growth and needs change on in-game minutes, so their updates go out in batches. Player movement, carried items and escaped plants need frequent updates.
- **Joining reuses saving.** A joining player receives the current nursery state in the save format, then live changes.
- **Split roles fall out naturally:** one player minds the nursery while another runs to the wild zones or loads the wagon.

## Days, rounds and seasons

The nursery is persistent like Stardew: layouts and bloodlines carry over between days. Each day plays like a PlateUp round built around the wagon departures: a timer and a loading rush, with calm in between.

- **Day shape:** morning care and planning, trading through the day, any wagon departures booked for that day (each with a countdown), evening wind-down, then sleep. Many days have no departure at all.
- **Sleep:** everyone readies up. Plants grow, and needs such as hunger tick on, overnight.
- **Seasons:** spring, summer and autumn are the growing seasons. Each species grows well, slowly or not at all by season. Breeding recipes can require a season (for example, a heated glasshouse in autumn).
- **Winter:** undecided. Suggested default: short, with outdoor growth stopped, heated glasshouses producing at a running cost, and a winter market. The season system must allow skipping it too.
- **All timings live in data** (day length, growth speed, hunger rates, season length) so they can be tuned without code changes.

## Building

Players lay out the nursery on a grid, and placement matters more than in an ordinary nursery because plants affect their neighbours.

- **Footprints and rotation:** every item covers one or more cells and rotates in 90° steps.
- **Layers:** floor (receivers such as benches and tables, plus paths, cart bay and structures), surface (placeables such as trays and pots, which sit in a bench's slots; see below) and overhead (watering and lights).
- **Structures are zones:** cold frames, glasshouses and warded enclosures each change the conditions inside them. Glasshouses are warmer; warded enclosures hold volatile stock and stop escapes from leaving.
- **Adjacency is gameplay:** neighbour effects act on pots in touching slots, so a layout is also a puzzle of who sits next to whom.
- **Valid placement:** a green or red ghost previews the result; the host has the final say.
- **Moving and selling back:** placed items can be picked up and moved, or sold back for part of the price.
- **Plot:** the nursery starts small, and land expansion is an unlock.

### Benches and slots

Bench tops are not free grids: each bench has a 0.6 m deep planting area laid out in rows, and the pot size decides how many rows fit. Placement stays a quick choice with a small puzzle in it, instead of fiddly positioning.

| Pot size | Slot | Rows across | Per row on a 3 m bench | Most neighbours |
| --- | --- | --- | --- | --- |
| Small | 0.2 m × 0.2 m | 3 | 15 (45 in all) | 4 |
| Medium | 0.3 m × 0.3 m | 2 | 10 (20 in all) | 3 |
| Large | 0.6 m × 0.6 m | 1 | 5 | 2 |
| Plug tray | 0.6 m × 0.4 m | 1 | 7 | 2 |

- **Pots snap to slots.** A bench can mix sizes along its length (a run of small pots, then medium, then large), and each run uses its own row layout.
- **Neighbours are pots whose slots touch along an edge:** left and right in the row, and front and back across rows. Diagonals don't count. So on a bench standing alone, a small pot in the middle row has up to 4 neighbours, a medium pot 3, and a large pot only the 2 beside it.
- **Benches that touch share neighbours.** When two benches stand back to back (long sides touching on the floor grid), pots in the two facing edge rows count as neighbours wherever their slots overlap across the join, even with different pot sizes on each side. So a large pot on a back-to-back bench gains neighbours on the other bench, and a pair of benches becomes one bigger puzzle. Benches placed end to end work the same way for the end slots. Leave a gap between benches to keep them apart.
- **The trade-off:** small pots pack in more plants and more neighbour interactions (good and bad); large pots are worth more and easier to keep apart.
- **No stacking:** nothing goes on a pot, and a bench can't go on another object. Pots and trays can't go on the floor.
- **Moving a bench** takes its pots with it, in the same slots.

**Wide bench (upgrade).** A 1.5 m × 4 m bench with a 1.5 m planting area, so large pots fit in two rows and every size gets interior slots surrounded on all sides.

| Pot size | Rows across 1.5 m | On a 4 m wide bench |
| --- | --- | --- |
| Small | 7 | 140 |
| Medium | 5 | 65 |
| Large | 2 | 12 |
| Plug tray | 2 | 20 |

**Carry trays move small pots; they never go on a bench.** Benches hold pots only. A carry tray (0.4 m × 0.6 m, up to 6 small pots) is for moving: in bench view, pick small pots off the bench into the tray in your hands, carry it, then place the pots into slots one by one, or drop the whole tray into a cart or the wagon. A tray can also be set down on the ground. It can't be placed on a bench.

**Built to be tuned.** Slot sizes, rows per pot size, bench lengths and what counts as a neighbour all live in data files, not code, so they can be adjusted in playtests without rebuilding the system. The numbers above are starting values.

## Plants and care

Every plant shares one base definition: growth stages, care needs, genes, pot sizes and value. Behaviour comes on top as optional modules (next section).

1. **Sow** seeds or cuttings into plug trays.
2. **Grow on** until rooted.
3. **Pot on** into larger pots for higher-value stock, where the species allows.
4. **Sell or harvest:** some plants sell whole; others are harvested repeatedly (scrapings, petals, sap, shed teeth) and regrow.

**Care:**

- **Water** for every plant. Under-watering slows growth, then wilts it; long neglect kills it.
- **Containment as care** for plants with behaviour: feeding, pruning, sedating, and shutting lids and cages, alongside watering.
- **Light, temperature and season** set growth speed, and structures change them.

**Quality:** care history becomes a quality grade that buyers pay for. Neglect costs value and time, never ends the game.

## Behaviour layer

Behaviour is an optional list of **modules** on top of the base plant, set in data and ticked by the sim after growth. A plant with no modules is a plain nursery plant.

| Module | What it does |
| --- | --- |
| Need | A meter beyond water, such as hunger, that rises over time and is reset by care |
| Mood | Telegraph stages driven by needs: content → restless → acting out → escaped |
| Neighbour effect | Acts on pots in touching slots on the same bench (drain, calm, pollinate) |
| Mobility | Lets an escaped plant leave its pot and move around the nursery |

**How plants misbehave:**

- **Neighbour effects (build first).** Traits act on touching pots: one calms its neighbours, another feeds on them, a third spreads pollen. Layout becomes a puzzle.
- **Containment as care.** Feeding, pruning, sedating, and shutting lids and cages alongside watering.
- **Escapes.** Neglected or badly placed plants wander, tangle carts and block paths.
- **Plants as tools.** A scare-bloom herds runners; a sticky vine slows escapees during loading.

**Chaos rules:**

- Plants only act up for a reason: neglect, bad neighbours, or volatile stock the players chose.
- Trouble is telegraphed: twitch, then rattle, then escape.
- Calm is progression: better cages, calmer bloodlines and soothing neighbours reduce chaos over time.
- Chain reactions stay local: one bench, never the whole farm.
- No new fail states: chaos costs time and value only.

## First three plants

Two benign plants and one that interacts with one of them: enough to show the base systems, the behaviour layer and a neighbour effect on a single bench.

|  | Hearthmoss | Chimebell | Snapjaw |
| --- | --- | --- | --- |
| Role | Benign, teaches the basics | Benign control plant | Interacts with Hearthmoss |
| Grown in | Plug trays, stays in trays | Trays, then potted on through sizes | Small and medium pots |
| Buyer | Apothecaries | Collectors | The town (granary pest-eater) |
| Sold as | Harvested scrapings, regrows | Whole specimen | Whole working plant; shed teeth harvested too |
| Key genes | Glow colour, regrowth speed | Bloom colour, bell pattern | Temperament (hunger rate), jaw size |
| Modules | None | None | Need, Mood, Neighbour effect, Mobility |

**Hearthmoss.** A low, softly glowing moss. Water it, scrape it, it grows back. The first plant every player grows, and the cheapest to keep.

**Chimebell.** A flowering plant whose bell-shaped blooms vary in colour and pattern. It never interacts with anything, so it is the control: if a bench of Chimebells is calm, the base systems are working.

**Snapjaw.** A small carnivorous plant. Its hunger rises through the day and is reset by feeding it grubs.

| Mood | Trigger | What happens | Player fix |
| --- | --- | --- | --- |
| Content | Fed | Nothing; grows normally | — |
| Restless | Peckish | Leaves twitch, jaws clack (the warning) | Feed it |
| Acting out | Hungry | Grazes on Hearthmoss in touching slots, lowering its quality | Feed it, or move it away from the moss |
| Escaped | Hungry for too long | Leaves its pot and crawls to the nearest Hearthmoss, tangling carts on the way | Catch it, re-pot it, then feed it |

The interaction is also a layout choice. A Snapjaw next to Hearthmoss feeds itself and never escapes, but it slowly eats your moss. Players can treat moss as a deliberate feeder crop or keep the two apart and feed by hand. Breeding calmer Snapjaw lines (lower hunger rate) is the first long-term breeding goal.

## Genetics, breeding and discovery

Hybridisation is the long-term spine: bred lines are worth more than anything bought. Every plant carries genes from the first day, so breeding can arrive later without changing saves or plant data.

- **Traits as genes:** looks (glow, bloom colour, bell pattern, leaf shape) and temperament (hunger rate, volatility). Some are dominant/recessive; others, such as jaw size, blend between parents.
- **Deliberate crosses:** cross two plants to get seeds with a mix of their genes. Stabilising a line takes several generations.
- **Accidental crosses:** neighbouring plants can cross by accident, so breeding happens in the open as well as on purpose.
- **Emergent traits:** hidden recessive genes only show when both parents carry them. There are also recipes for new species: A × B under set conditions, such as a heated glasshouse in autumn.
- **Hints keep it from being trial and error:** client rumours, catalogue silhouettes of undiscovered plants, and breeders' notes found in wild zones.
- **Value and danger go together:** the most potent plants tend to be the most volatile, so chasing value brings chaos with it.
- **The real work** is procedural plant visuals driven by the genes, and balancing what rare lines are worth. Plan a milestone for both.

## Buyers, orders and logistics

Three kinds of buyer want plants for different reasons, and the wagon departures are where the day's pressure peaks.

| Buyer | Use | What they value |
| --- | --- | --- |
| Alchemists and apothecaries | Ingredients harvested repeatedly (sap, petals, spores) | Potency |
| Collectors and noble gardens | Live specimens | Looks and rarity |
| The town | Working plants: guard hedges, pest-eaters, lantern blooms | Temperament and a specific behaviour |

**Order channels:**

| Channel | Size | How it works |
| --- | --- | --- |
| Shop counter | 1–5 plants | Walk-in townsfolk buy displayed stock during opening hours |
| Guild notice board and courier letters | Bulk contracts | A spec (species, genes or temperament, size, quantity), a price and a departure time. Players accept the ones they can meet. Rewards money and XP |
| Rush contracts | Any | Tighter departure, better pay. Always optional |

**Departures are booked, not daily.** When players accept a contract from the guild board or a courier letter, they choose its pickup day, within the window the contract allows (first pass: 2–7 days ahead). The wagon or courier arrives that day, waits until its departure time, then leaves with whatever is loaded. Players control the rhythm: a light week, or several departures stacked on one day for a big payout. Booked departures show on a calendar at the order desk. A missed departure dents reputation and lapses the order; no money or stock is lost.

**The container chain.** Goods are containers inside containers from day one: plant → tray or pot → cart → crate or pallet → wagon. Upgrades add links (handcart, mule cart, floating barrow) without changing how orders work. An escaped plant on the loading yard is the classic rush-hour problem.

**Reputation** with each guild grows with fulfilled contracts and unlocks bigger, better-paying work.

## Progression

The nursery earns XP from contracts and milestones. XP unlocks an upgrade, and coin buys it, so progress decides what is available and coin decides when.

| Track | Start | Tier 2 | Tier 3 | Tier 4 |
| --- | --- | --- | --- | --- |
| Handling | Carry by hand | Handcart | Mule cart | Enchanted floating barrow |
| Propagation | 6 plug trays | More and larger trays | Seed benches | Propagation house |
| Containers | Small pots | Medium pots | Large pots | Specimen urns |
| Watering | Watering can | Rain barrels and buckets | Gravity-fed channels | Enchanted rain charms |
| Structures | Open benches | Cold frames | Glasshouse | Warded enclosure |
| Containment | Hand feeding | Lidded pots | Cages | Calming wards |
| Trade | Shop counter | Order desk and ledger | Guild notice board contracts | Second guild |
| Land | Starter plot | Plot expansion | Plot expansion | Plot expansion |
| Benches | 3 m bench | 4 m bench | Wide bench (1.5 m × 4 m) | — |

- **Upgrades raise capacity, not just speed.** Cart size caps what can be loaded before a departure, so it caps which contracts a team can take.
- **All in data:** the tree, XP costs, prices and capacities live in data files.
- **First-hour pacing:** the first unlock within 20–30 minutes of play, checked by a headless balancing harness.
- Tier names beyond the first two are a first pass.

**Getting new species:**

| Source | Stock | When |
| --- | --- | --- |
| Supplier unlocks (XP) | Common | MVP |
| Client questline rewards | Rare | Post-MVP |
| Wild zones | Exotic | Post-MVP |
| Breeding recipes | New species | Post-MVP |

## The town and wild zones

Both are post-MVP, reached from the nursery as separate areas.

**The town.** A small hub of 4–6 clients, each with a questline told through their orders. It holds the story, rare rewards and rumours that hint at hidden crosses. Deliberately light: no full Stardew village. Over time, players slowly uncover what clients really want the plants for.

**Wild zones (to design).** A rough first take:

- A few biomes, each with its own wild species: a bog, an old forest, a ruined monastery garden.
- One trip per day. In co-op, someone stays behind to mind the nursery.
- Carrying capacity depends on cart upgrades.
- No combat: feral plants misbehave, and players handle them with their own tool-plants.
- Source of breeders' notes and hints for hidden crosses.

Still open: how far, how dangerous, and how often.

## Visual rules and scale

The MVP and prototype use coloured placeholder shapes; final art is stylised low poly, decided later.

- **Colour code placeholders by function:** benches brown, trays and pots green, water blue, carts and wagons yellow, buyers in their own colours. Plants with behaviour get a distinct colour, plus a visible mood indicator (content, restless, acting out).
- **Keep visuals separate from logic.** Every placeable item is a prefab with its behaviour and grid footprint on the root, and a swappable placeholder mesh as a child.
- **Placeholder plants show their genes:** tint from the colour gene, size from the growth stage, and a twitch or shake animation for the Restless mood.
- **Readable from above:** silhouettes, colours and mood cues must work from the top-down camera, not up close.
- **Rendering:** URP, with a basic day–night cycle and warm lighting in the greybox.

**Scale (first pass, to confirm):**

| Measure | Value |
| --- | --- |
| Floor grid cell | 0.5 m × 0.5 m (benches, structures, paths) |
| Bench planting area | 0.6 m deep, in rows by pot size |
| Standard benches | 3 m and 4 m long |
| Bench top height | 0.9 m, about the player's waist |
| Player height | 1.8 m |
| Path width | 2 floor cells (1.0 m) minimum, so a handcart fits |

## Technical architecture

The game rules live in a pure C# simulation core (`Assets/Sim`) that knows nothing about Unity; Unity displays it and turns input into requests. Behaviour is one more sim system, not a separate game.

- **Commands in, events out.** Every action is a command (`PlaceItem`, `WaterTray`, `FeedPlant`, `CatchPlant`, `AcceptContract`) that the sim validates, then applies or rejects with a reason. In co-op, clients send commands to the host's sim.
- **Ticks, not frames.** The sim advances in fixed in-game steps. Each tick runs growth first, then behaviour modules: needs rise, moods update, neighbour effects apply, escaped plants move.
- **Modules are data.** A species lists its modules and their numbers (hunger rate, mood thresholds, which neighbours it affects). A new behaviour is a new module type; a new plant using existing modules is data only.
- **Mobile plants are sim entities.** An escaped plant has a position on the grid and paths through it in the sim, so co-op stays consistent and tests can check it. Unity only animates it.
- **One state format** for host saves and players joining mid-game.
- **Headless tests:** the sim runs in plain .NET tests, including several players' commands, a Snapjaw left hungry overnight, and save/load round-trips.
- **Balancing harness:** simulated players run weeks of game time headlessly to check pacing and how often chaos happens.

## Mechanics spec

First-pass numbers so the prototype can be built without guessing. Every value lives in data and is expected to change in playtests; the balancing harness checks the first hour against them.

### Time

| Setting | First pass |
| --- | --- |
| Real time per in-game day | 10 minutes (06:00–22:00 playable, night skipped by sleep) |
| Shop opening hours | 08:00–18:00 |
| Sim tick | 1 in-game minute |

### Carrying and loading

- **Hands hold one thing:** one pot (any size), one plug tray, one carry tray of up to 6 small pots, one sack or one caught plant. Players can't take from another player's hands.
- **Carts and the wagon are containers, not benches:** dropping an item in fills the next free space, with no slot picking. The capacities below use the bench slot sizes.

| Carrier | Bed | Small pots | Medium | Large |
| --- | --- | --- | --- | --- |
| Handcart (unlock 1) | 0.6 m × 1.2 m | 18 | 8 | 2 |
| Mule cart (later) | 0.6 m × 2.4 m | 36 | 16 | 4 |
| Wagon bed | 1.2 m × 3 m (two 0.6 m beds) | 90 | 40 | 10 |

- **Loading is by hand, PlateUp style.** Players carry one item at a time (a pot, a plug tray, or a carry tray of up to 6 small pots) to the wagon and drop it in. A cart parked near the wagon shortens the walk; it doesn't load anything by itself. With several players, loading becomes a relay.

### Watering

| Setting | First pass |
| --- | --- |
| Water meter | 0–100 per pot or tray; one watering adds 100 |
| Drain per in-game hour | Small pot 6, medium 4, large 3, plug tray 8 |
| Watering can | 10 waterings; refill at a rain barrel in 1 s |
| Cost per watering | 1 for a pot or tray; 2 for a large pot |
| At 0 | Growth stops; wilts after 12 in-game hours; dies after 3 days wilted |

### Growth

A plant that finishes a stage in a pot that is too small becomes **root-bound**: growth pauses, with no other harm, until it is potted on.

| Species | Stages and in-game days | Sellable |
| --- | --- | --- |
| Hearthmoss | Plug tray: harvestable after 1 day, regrows 1 day after each scrape | Each scrape gives 1 bundle per tray |
| Chimebell | Plug tray 1 day → small pot 1 day → medium pot 2 days (blooms) → large pot 2 days (specimen) | Medium in bloom, or large specimen |
| Snapjaw | Small pot 2 days → medium pot 2 days (mature) | Mature, in a medium pot; sheds 1 tooth per content day |

**Potting on:** carry the plant (in its old pot) to an empty bigger pot sitting in a slot and interact. The plant moves across and the empty old pot goes back to storage.

**Pot size caps value.** A plant can only reach part of its species' full value in a pot that's smaller than its final size: small ×0.5, medium ×0.8, final size ×1.0. Small pots stay quick and cheap, but they're clearly a stepping stone, which pushes players to pot on the plants that matter rather than fill every slot. The sell prices in the Economy table already include this cap.

### Snapjaw behaviour

| Setting | First pass |
| --- | --- |
| Hunger | 0–100, +8 per in-game hour (about 12 hours from fed to starving) |
| Moods | Content below 40 · Restless 40–70 · Acting out 70–100 · Escapes after 2 in-game hours at 100 |
| Feeding | One portion of grubs resets hunger to 0 |
| Grazing (acting out) | Every 2 in-game hours it eats a touching Hearthmoss: the moss drops one quality step and the Snapjaw's hunger drops by 30 |
| Escaped | Moves at 0.5 m/s (players 3 m/s) towards the nearest Hearthmoss, blocking carts it touches |
| Catching | Interact in reach: it goes into your hands. Place it in an empty pot to re-pot it, then feed it |

### Economy

| Item | Buy (coin) | Sell (coin) |
| --- | --- | --- |
| Starting purse | 200 | — |
| Small / medium / large pot (empty) | 2 / 4 / 8 | — |
| Plug tray | 5 | — |
| Hearthmoss spores (one tray) | 10 | Bundle: 4 |
| Chimebell seeds (6) | 12 | Medium in bloom: 15 · large specimen: 35 |
| Snapjaw seedling | 15 | Mature: 30 · tooth: 2 |
| Grubs (one feed) | 1 | — |
| Bench, 3 m / 4 m | 40 / 50 | Sell back at half price |
| Carry tray (holds 6 small pots) | 6 | — |

- **Quality multiplies price:** poor ×0.6, good ×1.0, excellent ×1.4.
- **No fixed daily costs in the MVP;** water is free, grubs are the running cost.
- **Supplier purchases** arrive in the storage crate by the order desk straight away.

### Contracts and the shop

| Setting | First pass |
| --- | --- |
| Guild board | 3 offers, refreshed every morning |
| Courier letters | One every \~3 days, bigger and better paid |
| A contract asks for | Species, minimum pot size, minimum quality, quantity (5–20 in the MVP) |
| Pay | 1.2 × the plants' sale value |
| XP | Half the contract's pay |
| Pickup | Chosen by the players, 2–7 days ahead |
| Part-loads | Pay for what is loaded if at least half is there; below half counts as missed |
| Reputation | +1 full, 0 partial, −1 missed |
| Walk-in customers | 4–8 a day in opening hours; each buys 1–3 plants from benches marked for sale and pays automatically at the till |

**First-hour check:** six starting plug trays of Hearthmoss give 6 bundles (about 24 coin) a day. A first Hearthmoss contract of 10 bundles pays about 48 coin and 24 XP, and takes roughly two days to fill. The **handcart unlocks at 20 XP**, so it lands after the first contract: about day 2–3, or 20–30 real minutes, on target. Later unlocks should cost a few contracts each; the balancing harness tunes the curve.

### Co-op clashes

- **First request wins:** if two players grab the same pot or slot, whoever's request reaches the host first gets it. The other sees a brief "taken" cue, and their hands stay empty.
- **Smart targeting avoids doubling up:** a plant another player is already feeding or catching is skipped.

### Neighbours on screen

- While holding a pot or in bench view, soft lines link each slot to its neighbours, coloured by effect (calming green, draining red, pollen yellow) and patterned too (solid, dashed, dotted), so colour isn't the only cue.
- Hovering a plant shows its neighbours and what they do to it.

### Starting kit

What a new nursery begins with, so day one is defined: one 3 m bench, 6 plug trays with Hearthmoss spores, 2 carry trays, a watering can, a rain barrel, the storage crate, the order desk with its ledger, the shop counter, and 200 coin.

### Night

Nobody can act while everyone sleeps, so nothing may go wrong overnight that players couldn't have prevented:

- Needs and drying tick at a quarter of their daytime rate overnight.
- Moods can't pass Restless overnight, so no plant escapes or grazes in the night. A hungry Snapjaw wakes up restless, as the morning's first job.
- Growth continues at the normal rate.

### Disconnects

- A client who drops out leaves what they were holding on the ground where they stood; any player can pick it up.
- If the host drops out, the session ends as for the host leaving, with an autosave just before.

### Stock ledger

The order desk's ledger lists what the nursery holds, by species, pot size and quality, next to each accepted contract's needs and pickup day. Players can see at a glance whether they can fill an order before the wagon comes. It also lists empty pots and trays in the storage crate.

### Storage

The storage crate has no limit in the MVP. Bought stock, empty pots and harvested bundles go there. A capacity limit and a bigger storeroom can come later as an upgrade.

## MVP scope

Build in two steps: the shared systems first, then the behaviour prototype that proves the direction is fun.

1. **Shared systems:** grid, growth, watering, genes, orders and co-op. This works with plain plants (Hearthmoss and Chimebell).
2. **The prototype:** one bench, the Snapjaw with all four modules, a player who catches it, the follow camera with bench view, and a fixed camera for the loading yard. If it's fun, the full MVP below follows.

| Area | In the MVP | Later |
| --- | --- | --- |
| Co-op | Steam host and join, joining mid-game, sleep ready-up, shared purse and XP | — |
| Camera | Top-down follow camera with zoom-out and off-screen alerts | Refinements from playtests |
| Building | Grid placement of benches, trays, pots; cold frames | Glasshouse, warded enclosure, overhead watering |
| Plants | Hearthmoss, Chimebell, Snapjaw | More species, from supplier, quests, wild and recipes |
| Behaviour | Need, Mood, Neighbour effect, Mobility | More modules, tool-plants |
| Care | Watering can, feeding, re-potting caught plants | Lids, cages, sedating, wards |
| Genetics | Genes stored and shown | Breeding, accidental crosses, recipes, catalogue |
| Sales | Shop counter, guild contracts with departures and XP | Rush contracts, guild reputation tiers |
| Logistics | Hand carry, then handcart, onto a wagon | Mule cart, floating barrow, crates |
| Progression | First tier of each track | Higher tiers, land |
| World | The nursery | The town, wild zones |
| Time | Days, sleep, spring to autumn | Winter design |
| Saves | Host save and load, autosave | — |
| Art | Coloured placeholders, mood indicators, basic day–night lighting | Stylised low-poly art |

## Open questions

Not decided yet; each has a suggested default so planning can start.

| Question | Suggested default |
| --- | --- |
| Winter | Short winter: outdoor growth stops, heated glasshouses work at a cost, winter market |
| Wild zones: distance, danger, frequency | One trip a day, a few minutes of walking, mild feral plants, no combat |
| Running costs | Small daily costs (feed, heating, later staff), so coin has a sink |
| Plant death | Neglected plants wilt, then die after several days; escapes never kill plants |
| Pricing | Fixed by species, size, quality and genes in the MVP; player-set shop prices later |
| Player characters | Simple colour and outfit choice, no stats |
| Hired help | Post-MVP apprentices for watering and feeding |
| Tutorial | A guided first day: sow moss, water, scrape, sell, first guild contract |
| Audio | Placeholder ambience; a cosy soundtrack later; clear audio warnings for restless plants |
| Accessibility | Rebindable controls, readable UI, mood shown by shape and animation as well as colour |
| Monetisation | One-off Steam purchase, no microtransactions |
| Bench depth | 0.6 m deep, so the bench is exactly its planting area (3 m or 4 m long). The alternative is a 1 m deep bench with a 0.6 m planting area and a 0.4 m working edge |
