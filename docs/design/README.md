# Game design

_The human owns this folder. The lead writes up agreed decisions here; dev agents read it and don't make design decisions._

**PlantNursery** is a cosy 1–4 player co-op plant nursery sim for Steam, played in first person.
Players build a nursery on a grid, grow plants to sale, fulfil shop and website orders, and
unlock better equipment. The original design document is `source/plantnursery-design-document.md`.
The files below are the source of truth when they differ from it.

| File | System |
|---|---|
| `overview.md` | Pillars, MVP scope, milestones |
| `controls.md` | First-person view, interaction, hands, build mode |
| `time.md` | Clock, days, seasons, sleep, pause |
| `building.md` | Grid, layers, footprints, placement, plot |
| `plants.md` | Containers, growth stages, water, root-bound, health, quality, death |
| `genetics.md` | Genes and how they show on placeholder plants |
| `logistics.md` | The container chain, carrying, trolley, vehicles |
| `sales.md` | Supplier, shop front, website orders |
| `economy.md` | Wallet, prices, sell-back |
| `progression.md` | XP and upgrade tracks |
| `coop.md` | Host authority, joining, sleeping together |
| `visuals.md` | Placeholder colours, art direction, scale |

Numbers in these docs are **defaults**. They all live in content data files (see
`docs/decisions/0004-content-data-format.md`) so they can be tuned without code changes.
