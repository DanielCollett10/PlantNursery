# Genetics, breeding and discovery

Hybridisation is the long-term spine: bred lines are worth more than anything bought. Every plant carries genes
from the first day, so breeding can arrive later without changing saves or plant data.

## Gene model (built, keep)
- A gene is defined in data: `id`, `kind` (`Mendelian` or `Blend`), alleles or value range.
- Each plant has two alleles per gene. Mendelian: the most dominant allele shows. Blend: the mean of the two values (0–1).
- A species defines which genes it has; a bought seed/seedling copies its **variety's** alleles exactly.

## MVP genes
| Gene | Species | Kind | Alleles / range | Shows as / affects |
|---|---|---|---|---|
| `glowColour` | Hearthmoss | Mendelian | `amber` > `teal` > `violet` | Tint of the moss |
| `regrowthSpeed` | Hearthmoss | Blend | 0–1 → regrow time ×(1.25 − 0.5·value) | Regrow days |
| `bloomColour` | Chimebell | Mendelian | `white` > `blue` > `rose` | Bell tint |
| `bellPattern` | Chimebell | Mendelian | `plain` > `striped` > `spotted` | Placeholder stripe/dots (optional) |
| `temperament` | Snapjaw | Blend | 0–1 → hunger rate ×(0.5 + value) | Hunger rate |
| `jawSize` | Snapjaw | Blend | 0–1 | Jaw scale |

Each species ships with 1–2 varieties in data.

## Where a new plant's genes come from (agreed for M2)
- Varieties live **inside each species** in `species.json` (`species[].varieties[]`: `id`, two alleles per gene of that species).
  The old top-level `varieties.json` is a separate, pre-pivot file (`docs/decisions/0011`).
- A **seed packet** (and spores) is an item with a fixed `speciesId` and `varietyId`. Sowing copies that variety's alleles
  **exactly** into every plant it sows; the starter kit's moss uses its configured variety. **No randomness in M2**:
  the same content always gives the same genes. (`SimRandom` is kept for crosses later.)
- Phenotype (what shows) uses the built `Phenotype` rules (T-010): Mendelian = most dominant allele, Blend = mean.

| Variety id | Species | Alleles |
|---|---|---|
| `hearthmoss_amber` | Hearthmoss | glowColour amber/teal · regrowthSpeed 0.5/0.5 |
| `hearthmoss_violet` | Hearthmoss | glowColour violet/violet · regrowthSpeed 0.3/0.4 |
| `chimebell_white` | Chimebell | bloomColour white/rose · bellPattern plain/spotted |
| `chimebell_blue` | Chimebell | bloomColour blue/blue · bellPattern striped/striped |

## Later (post-MVP)
- **Deliberate crosses:** two plants → seeds mixing their genes; stabilising a line takes generations.
- **Accidental crosses:** neighbouring plants can cross by accident (a `pollinate` neighbour effect).
- **Emergent traits:** hidden recessives; recipes for new species (A × B under set conditions, e.g. heated glasshouse in autumn).
- **Hints:** client rumours, catalogue silhouettes, breeders' notes from wild zones.
- **Value and danger go together:** the most potent plants tend to be the most volatile.
- A milestone is needed for procedural gene-driven visuals and for balancing rare lines.
