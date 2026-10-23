# Species: the first three plants

Two benign plants and one that interacts with one of them: enough to show the base systems, the behaviour
layer and a neighbour effect on a single bench. All values are data (`species.json`, replacing `varieties.json`).

| | Hearthmoss | Chimebell | Snapjaw |
|---|---|---|---|
| Role | Benign, teaches the basics | Benign control plant | Interacts with Hearthmoss |
| Grown in | Plug trays, stays in trays | Tray, then potted on through sizes | Small and medium pots |
| Buyer | Apothecaries | Collectors | The town (granary pest-eater) |
| Sold as | Harvested scrapings, regrows | Whole specimen | Whole working plant; shed teeth harvested too |
| Key genes | `glowColour`, `regrowthSpeed` | `bloomColour`, `bellPattern` | `temperament` (hunger rate), `jawSize` |
| Modules | None | None | Need, Mood, Neighbour effect, Mobility |

## Hearthmoss
A low, softly glowing moss. Water it, scrape it, it grows back. The first plant every player grows, and the cheapest to keep.
- **One plant per plug tray** (spores are sown as a whole tray).
- Becomes **harvestable** 1 in-game day after sowing. **Scrape** (smart target or bench view): gives 1 **bundle** per tray, sends it to the regrow state; harvestable again 1 day later (`regrowDays`, scaled by `regrowthSpeed`).
- Scraping needs empty hands; the **bundle goes into the player's hands**. Drop it in the storage crate, a cart or the wagon.- Quality of the bundle = the moss's current quality.

## Chimebell
A flowering plant whose bell-shaped blooms vary in colour and pattern. It never interacts with anything: if a bench of Chimebells is calm, the base systems are working.
| Stage | Container | Days |
|---|---|---|
| Seedling | Plug tray | 1 |
| Young | Small pot | 1 |
| Blooming | Medium pot | 2 (sellable in bloom) |
| Specimen | Large pot | 2 (sellable as specimen) |

## Snapjaw
A small carnivorous plant. Its hunger rises through the day and is reset by feeding it grubs.
| Stage | Container | Days |
|---|---|---|
| Young | Small pot | 2 |
| Mature | Medium pot | 2 then mature (sellable) |

- Bought as a **seedling** already in a small pot (no sowing).
- **Sheds 1 tooth per content day:** at each day start, a Mature Snapjaw that was never above Content during the previous day drops a tooth into its pot; harvesting collects it into the player's hands (`teethPerContentDay`).- Behaviour numbers: `behaviour.md`.

The layout choice: a Snapjaw next to Hearthmoss feeds itself and never escapes, but slowly eats the moss.
Players can treat moss as a deliberate feeder crop, or keep the two apart and feed by hand. Breeding calmer
Snapjaw lines is the first long-term breeding goal.

## Season multipliers (first pass)
| Species | Spring | Summer | Autumn |
|---|---|---|---|
| Hearthmoss | good | slow | good |
| Chimebell | good | good | slow |
| Snapjaw | slow | good | good |
