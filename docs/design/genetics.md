# Genetics

Every plant stores genes from M1, so adding breeding later changes no saves or plant data.

## Gene model
- A **gene** is defined in data: `id`, `kind` (`Mendelian` or `Blend`), and its alleles or value range.
- Each plant has two alleles per gene (diploid). `Mendelian` genes have a dominance order. `Blend` genes are numbers in 0–1.
- The **phenotype** (what you see) is worked out from the alleles:
  - Mendelian: the most dominant allele present wins.
  - Blend: the average of the two values.

## MVP genes
| Gene | Kind | Alleles / range | Shows as |
|---|---|---|---|
| `leafColour` | Mendelian | `green` > `red` > `yellow` | Leaf tint |
| `variegation` | Mendelian | `plain` > `striped` | Placeholder leaf gets a pale stripe (optional in M1) |
| `spotSize` | Blend | 0–1 | Placeholder dot size (later) |

## Varieties
- A variety in data fixes the alleles for every gene. A plant sown from a packet copies its variety's genes exactly.
- The MVP has 3 varieties of one plant line (data), e.g. Green Plain, Red Plain, Yellow Striped.

## Later (not MVP)
Breeding (crossing two plants), stabilising strains, a catalogue, procedural leaf shaders and rarity-based value.
