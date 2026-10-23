# 0008: One rebuild command for generated Unity assets, and the output is committed

**Context.** Agents wrote the greybox, scene and lighting generators, but nobody ran them, so the M1 playtest opened a stale `Starter.unity` with no Player, no prefabs and no HUD.
There are also several menus to run in a fragile order: the scene generator replaces the scene, which wipes the lighting setup. The menu roots are inconsistent too (`PlantNursery/` and `Plant Nursery/`).

**Decision.**
- All generator menus live under **`Plant Nursery/`**.
- **`Plant Nursery/Rebuild Greybox World`** runs every generator in the right order: URP setup (if missing), then greybox materials and prefabs, then the Starter scene, then day–night lighting. Then it saves.
- The scene generator adds everything the scene needs itself, including lighting. No step after generation may be needed.
- Generated assets (`Assets/Game/Greybox/**`, `Assets/Scenes/Starter.unity`, and their `.meta` files) are **committed**. Opening the project and pressing Play must work with no menu steps.
- An editor-lane task that changes a generator must run the rebuild and commit the output.
- An EditMode test checks that the Starter scene contains a configured Player, SimHost, WorldView, HUD, BuildMode and lit sun.

**Consequences.** The playtest only needs "open the scene, press Play". Generator changes show up as asset diffs in review.
