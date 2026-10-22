# 0003: Unity owns bodies, the Sim owns things

**Context.** It's a first-person game: players walk, look and carry things, and later there's forklift physics. The sim must stay deterministic and Unity-free, but it must be the authority on goods, money and plants, so co-op can never duplicate or lose them.

**Decision.**
- **Unity owns:** player and vehicle positions, movement, camera, physics, raycasts, and reach checks.
- **The sim owns:** every item, plant, holder (hands, bench surface, trolley, vehicle) and what each holds, plus money, time and all rules.
- The sim has no world positions for players. Placed items have grid cells, not transforms.
- Unity decides an interaction is in reach, then sends a sim command with a `PlayerId`. The sim validates the rules (ownership, capacity, state) and accepts or rejects it.
- In co-op (M3), reach is checked on the host using the host's copy of player positions.

**Consequences.** Sim tests never need geometry. Cheating on reach is possible for clients, which is acceptable for friends-only co-op.
