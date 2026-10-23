# 0010: Top-down follow camera, bench view and fixed rush cameras (replaces first person)

**Context.** The game was first person, which made 0.2 m pots hard to target and hid the layout
puzzle that neighbour effects create. The new design shows the nursery from above, PlateUp style.

**Decision.**
- **No first-person view.** Each player has their own camera (also in online co-op).
- **Follow camera** (top-down or angled) tracks the local player everywhere except rush areas.
  Zoom out on a button, or automatically after standing still for a few seconds.
- **Rush areas** (loading yard first, later the shop counter) are trigger zones with a **fixed camera**
  showing the whole area. Entering pans to it; leaving returns to the follow camera.
- **Bench view:** interacting with a bench pans to look straight down on it (back-to-back benches share
  one view). It never locks the player in: walking away pans back out. Mouse points at slots;
  gamepad steps through them. Click-drag waters every pot the drag passes over.
- **Targeting:** off the bench, care actions use **smart targeting** within 1.5 m reach. Unity finds the
  candidates in reach (0003 still holds: Unity owns bodies and reach); the **sim** chooses among them
  (neediest, nearest escapee), because the choice is a game rule.
- Cameras, zones and pointers are Unity-only. The sim never knows which camera is active.

**Consequences.** `FirstPersonController`, camera raycast interaction and the 3 m reach are retired.
Build-mode ghosts (ground-plane ray) carry over. Edge-of-screen alerts are needed for trouble out of view.
