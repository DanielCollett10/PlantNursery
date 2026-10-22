# PlantNursery — Design Document

Oct 22, 2026 · @Dan

PlantNursery is a cosy 1–4 player co-op plant nursery sim for Steam. Players build a nursery on a grid, grow plants to sale, fulfil shop and website orders, and unlock better equipment as they earn XP. Hybrid breeding follows once the core loop works.

## Pillars

Every feature is judged against these four pillars; a feature that serves none of them waits.

1. **Co-op from the start.** 1–4 players share one nursery from the first build. Solo play is the same game with one player, never a separate mode.
2. **Cosy with occasional crunch.** The everyday rhythm is calm: plant, water, tidy, serve. Pressure comes in short bursts, such as loading a wagon before it leaves, and players mostly choose to take it on.
3. **Grow your nursery.** Players start small (6 plug trays, carrying plants by hand) and feel every upgrade make them more capable.
4. **Plants worth caring about.** Plants are individuals with visible traits. Later, hybrid breeding turns them into a collection worth chasing.

## Core loop

&#91;embedded content: core loop · 6 steps\]

The loop runs in days: sow and care in the morning, sell and deliver through the day. Each turn of it pays for equipment that makes the next turn bigger.

## Co-op

The host's game is the single authority on the nursery, and up to three friends join it through Steam. Players never connect directly to each other.

| Rule | Decision |
| --- | --- |
| Players | 1–4. Solo is a session with one player |
| Money | Shared wallet |
| XP and unlocks | Shared, owned by the nursery, not by individual players |
| Sleeping | Every player readies up to sleep through the night. The host can force sleep if someone is away |
| Joining | Players can join mid-game |
| Saves | Belong to the host. Autosave often, on the host only |
| Host leaves | The session ends for everyone. Clients return to the menu with "Host left, progress is saved in their game" |
| Platform | Steam: friend invites and lobbies through Steam's networking, so there are no servers to run |

**How it works:**

- **Requests in, results out.** Clients send requests ("place bench here", "water tray 12"). The host checks each one, applies it, and sends back what changed. Two players can never build on the same cell or sell the same plant.
- **Plants sync cheaply.** Plants change slowly, on in-game minutes or hours, so their updates go out in batches. Only player movement and carried items need frequent updates.
- **Joining reuses saving.** A joining player receives the current nursery state in the same format as a save file, then live changes. Building saves gives most of the join feature for free.
- **Networking stack** (to confirm in planning): Unity Netcode for GameObjects with a Steam transport. FishNet is the main free alternative. Develop against Steam's shared test app (480, "Spacewar") until the game has its own app ID.
- **Testing:** Unity's Multiplayer Play Mode runs several players on one PC. Headless sim tests also replay several players' requests at once.

## Time and seasons

The nursery runs on a day cycle inside four seasons, and every timing (day length, growth speed, season length) lives in data so it can be tuned without code changes.

- **Days:** morning opening, daytime trading, evening wind-down, then sleep. Opening hours decide when walk-in customers come.
- **Sleeping:** everyone readies up (see Co-op). Sleep skips to the next morning, and plants grow overnight.
- **Seasons:** spring, summer and autumn are the main growing seasons. Each plant has seasons when it grows well, slowly or not at all.
- **Winter (undecided).** The season system must support all three options, so the choice can be made later by changing data:
  - **Skip it:** a fast-forward to spring. Simplest, but it loses a quarter of the year.
  - **A short, different season:** outdoor growth stops, heated tunnels keep producing at a running cost, and a winter market (poinsettias, holly, wreaths) brings demand. This gives tunnels and heating a purpose.
  - **A breather:** a building, upgrading and (later) breeding season with little to grow.
- **Suggested default:** the short, different winter. The MVP covers spring to autumn only.

## Building

Players lay out the nursery on a grid, placing equipment that occupies one or more cells on one of three layers.

- **Footprints and rotation:** every item has a footprint in cells (a bench might be 1×4) and can rotate in 90° steps.
- **Layers:**
  - **Floor:** benches, shelving, paths, the trolley bay, tunnels.
  - **Surface:** plug trays and pots, placed on top of benches and shelves. A tray needs a free bench cell under it.
  - **Overhead:** sprinklers, lights and heaters, mounted over benches or inside tunnels.
- **Poly tunnels** come in sizes (small, medium, large). Each is both an object and a zone with its own microclimate (warmer, frost-protected), which affects every plant inside.
- **Valid placement:** a green or red ghost previews the result. The host makes the final call, so two players can never build in the same place.
- **Moving and selling back:** placed items can be picked up and moved. Selling items back returns part of the price.
- **Plot:** the nursery starts on a small plot, and land expansion is an unlock (see Progression).

## Plants

Each plant is an individual that grows through stages and is moved up through pot sizes as it grows. Each move to a bigger pot adds value.

1. **Sow** seeds or cuttings into plug trays. A tray has a fixed number of cells.
2. **Grow on** until the plugs are rooted.
3. **Pot on** into small pots, then larger pots for higher-value stock.
4. **Sell** at a sellable stage. Bigger and healthier plants sell for more.

**Care needs:**

- **Water** (MVP): each pot or tray dries out over time. Under-watering slows growth, then wilts the plant; long neglect kills it. Over-watering is a later refinement.
- **Light, temperature and season** (MVP basic): growth speed depends on the season and on being inside a tunnel.
- **Feed** (later): fertiliser speeds growth and raises quality.
- **Pests and disease** (later): occasional problems that spread between neighbours if ignored.

**Quality:** a plant's care history becomes a quality grade that customers pay for. Good care pays; neglect only costs value, never ends the game.

**Getting stock:** players buy seeds and cuttings from a supplier, through the shop computer or a supplier visit. Later, breeding produces their own.

## Genetics and hybrids

Every plant carries genes from the MVP onwards, but breeding arrives only after the core loop works. That way, adding breeding later changes no saves or plant data.

- **Traits as genes:** leaf colour, striation and variegation pattern, dotted-spot size, leaf shape, and later more. Some traits are simple dominant/recessive ones; others, such as spot size, blend between the parents.
- **MVP:** varieties are fixed, bought from the supplier, each with its genes set. Traits are visible on the placeholder plants (see Visual direction).
- **Breeding (post-MVP):** cross two plants to get seeds that inherit a mix of their genes. First-generation results vary; stabilising a strain takes several generations, which gives long-term goals.
- **Value and collection:** rare trait combinations sell for more and attract special orders. A catalogue records every variety the nursery has produced.
- **The real work is not the genetics.** The breeding rules are small. The work sits in procedural leaf visuals (one leaf shader driven by the genes) and in balancing what rare varieties are worth, so breeding gets its own milestone.

## Customers, orders and logistics

Plants leave the nursery two ways, small sales at the shop front and bulk orders from the website, and the logistics around them create the game's crunch moments.

| Channel | Size | How it works | Rewards |
| --- | --- | --- | --- |
| Shop front | 1–5 plants | Walk-in customers browse displayed stock during opening hours and buy at the till | Money; small reputation gain |
| Website orders | 20+ plants | Arrive on the shop computer with a spec (variety, size, quantity), a price and a deadline. Players accept the orders they can meet | Money and XP; reputation |
| Rush orders | Any | Website orders with tight deadlines and higher pay. Always optional | More money and XP |

**Delivery:** each accepted website order names a pickup time. A vendor arrives with a pickup truck or wagon, waits until the departure time, then leaves with whatever is loaded. The schedule is visible days ahead.

**Missed deadlines:** a small reputation dent, and the order lapses. No money or stock is lost, which keeps the game cosy.

**The container chain.** Goods are modelled as containers inside containers from day one: plant → tray or pot → trolley → pallet → vehicle.

- **MVP:** carry by hand, then the trolley, loaded onto the vehicle by hand.
- **Later:** pallets, a pallet jack and a forklift add links to the chain and swap hand-carrying for physics-based movement. The economy and orders don't change.

**Reputation** grows with fulfilled orders and happy customers. It unlocks bigger and better-paying website orders.

## Progression

The nursery earns XP from website orders and milestones. XP unlocks an upgrade, and money buys it, so progress decides what is available and money decides when.

| Track | Start | Tier 2 | Tier 3 | Tier 4 |
| --- | --- | --- | --- | --- |
| Handling | Carry by hand | Trolley | Pallet jack | Forklift (physics) |
| Propagation | 6 plug trays | More and larger trays | Seed benches | Propagation house |
| Containers | Small pots | Medium pots | Large pots | Specimen sizes |
| Watering | Watering can | Hose | Sprinklers | Timed irrigation zones |
| Structures | Open benches | Small poly tunnel | Large poly tunnel | Heated glasshouse |
| Shop and sales | Basic till | Display shelving and signage | Website upgrades for bigger orders | Second sales channel |
| Land | Starter plot | Plot expansion | Plot expansion | Plot expansion |

- **Upgrades raise capacity, not just speed.** The trolley decides how much can be moved before the wagon leaves, so it caps which orders a team can realistically take. Each tier visibly opens bigger contracts.
- **All in data:** the tree, XP costs, prices and capacities live in data files. Content and balance change without code.
- **First-hour pacing:** starting with 6 plug trays must still give a satisfying first loop. Target the first unlock within 20–30 minutes of play. A headless balancing harness checks this (see Technical architecture).
- Tier contents above are a first pass for the lead to refine.

## Visual direction and scale

The MVP uses coloured placeholder shapes. The finished look is stylised low poly, in the spirit of My Time at Sandrock: soft shapes, warm light, readable silhouettes.

- **Colour code placeholders by function:** benches brown, trays and pots green, water equipment blue, vehicles yellow, with customers and vendors in their own colours.
- **Keep visuals separate from logic.** Every placeable item is a prefab with its behaviour and grid footprint on the root, and its placeholder mesh as a swappable child. Final art replaces only the child.
- **Placeholder plants show their genes:** a simple stem-and-leaf shape tinted by the colour gene and scaled by growth stage.
- **Rendering:** URP, which suits stylised looks and runs well on modest PCs.
- **Lighting early:** a basic day–night cycle and warm lighting go into the greybox, because the cosy feel depends on light as much as models.
- **Final art:** stylised low-poly asset packs can cover props, with custom work for hero pieces (plants, tunnels, shopfront). To decide later.

**Scale (first pass; the lead confirms these as a decision):**

| Measure | Value |
| --- | --- |
| Grid cell | 0.5 m × 0.5 m |
| Player height | 1.8 m |
| Bench top height | 0.8 m |
| Standard bench | 1 × 4 cells (0.5 m × 2.0 m) |
| Door and path width | 2 cells (1.0 m) minimum |

## Technical architecture

The game rules live in a pure C# simulation core (`Assets/Sim`) that knows nothing about Unity, and Unity only displays it and turns player input into requests. That split is what makes co-op, saves and agent-driven development work.

- **Commands in, events out.** Every player action is a command (`PlaceItem`, `WaterTray`, `AcceptOrder`) that the sim validates, then applies or rejects with a reason. The sim reports what changed as events. In co-op, clients send commands to the host's sim, and the host replicates the events.
- **Ticks, not frames.** The sim advances in fixed in-game time steps. Plant growth, drying and customer arrivals run on these ticks, so results are the same on every machine and in tests.
- **One state format.** The full nursery state serialises to one format, used for host saves and for players joining mid-game.
- **Content is data:** plants, items, footprints, unlocks, prices and order templates are data files that the sim loads and validates.
- **Headless tests:** the sim runs in plain .NET tests without Unity, including several-player command streams and save/load round-trips.
- **Balancing harness:** simulated players run the economy for weeks of game time headlessly, to check first-hour pacing and unlock timing.
- **Unity layer:** rendering, input, UI, the grid ghost, networking transport and Steam. It reads sim state and sends commands, never changing state directly.
- **Decisions for the lead to record:** the networking library, Steam integration, save format and storage location, and the data file format.

## MVP scope

The MVP is the full core loop for 1–4 players with placeholder art: build, grow, sell, unlock, from spring to autumn.

| Area | In the MVP | Later |
| --- | --- | --- |
| Co-op | Steam host and join, joining mid-game, sleep ready-up, shared wallet and XP | — |
| Building | Grid placement of benches, shelving, trays and pots; a small tunnel | Larger tunnels, glasshouse, overhead equipment beyond the hose |
| Plants | One plant line, a few fixed varieties with genes; growth through plug trays and two pot sizes | More species, feed, pests and disease |
| Care | Watering can and hose | Sprinklers, timed irrigation |
| Genetics | Genes stored and shown | Breeding, catalogue, procedural leaves |
| Sales | Shop front walk-ins; website orders with deadlines and XP | Rush orders, reputation tiers, second channel |
| Logistics | Hand carry, then trolley, onto a pickup truck or wagon | Pallets, pallet jack, forklift with physics |
| Progression | First tier of each track | Higher tiers, land expansion |
| Time | Days, sleep, spring to autumn | Winter design |
| Saves | Host save and load, autosave | — |
| Art | Coloured placeholders, basic day–night lighting | Stylised low-poly art |

## Open questions

These gaps aren't decided yet. Each has a suggested default so planning can start, and any can be changed before its milestone.

| Question | Suggested default |
| --- | --- |
| Camera and controls | Third-person camera, with a top-down build mode for placement. Mouse and keyboard first, gamepad later - Dan note from this, it is first person for everything. i think this comes from the possibility of building placement (poly tunnel etc) being in 3rd person / top down. but i think that being in first person makes sense&#32; |
| Player characters | A simple choice of colours and outfits; no stats |
| Running costs | Small daily costs (water, tunnel heating, later staff), so the economy has a sink and upgrades must pay for themselves |
| Plant death | Neglected plants wilt, then die after several days; a dead plant is composted for nothing. No other fail state |
| Pricing | Fixed prices by variety, size and quality in the MVP. Player-set shop prices later |
| Customer preferences | Walk-ins prefer certain varieties per season; website orders state exact specs |
| Weather | Not in the MVP. Later, frost and storms threaten outdoor stock and damage tunnels |
| Hired staff | Post-MVP automation track (waterers, shop assistants), unlocked late |
| Tutorial | A short guided first day: sow, water, pot on, sell, first website order |
| Map | One fixed nursery site with expandable plots; the town is reached by vendors, not by players |
| Audio | Placeholder ambience in the MVP; a cosy soundtrack later |
| Accessibility and languages | Rebindable controls and readable UI from the start; English first, text kept in data for later translation |
| Friendly fire and griefing | Friends-only co-op, so no permissions system. The host can kick a player |
| Monetisation | A one-off Steam purchase; no microtransactions |
