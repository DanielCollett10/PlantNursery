# Co-op

The host's sim is the single authority on the nursery; up to three friends join through Steam.

| Rule | Decision |
|---|---|
| Players | 1–4. Solo is a session with one player |
| Money, XP, unlocks, build inventory | Shared, owned by the nursery |
| Hands, camera, bench view, highlight colour | Per player |
| Sleeping | Every player readies up at a bed; the host can force it if someone is away |
| Joining | Mid-game: the client gets the current state in the save format, then live changes |
| Saves | Belong to the host. Autosave often, host only |
| Host leaves | Session ends for everyone (autosave just before); clients return to the menu |
| Platform | Steam friend invites and lobbies through Steam networking, no servers |
| Pause | None in co-op |

- **Requests in, results out.** Clients send commands; the host validates, applies and sends back what changed. Every command carries a `PlayerId`; the sim never assumes one player.
- **Sync cheaply:** growth and needs change per in-game minute, so they go out in batches. Player movement, carried items and escaped plants need frequent updates.
- **Split roles fall out naturally:** one player minds the nursery, another loads the wagon.

## Co-op ready from M2 (before any networking exists)
Networking comes later, but nothing built from M2 on may assume one player:
- **Sim:** every command carries a `PlayerId`; per-player state (hands, an open bench view's bench, highlight colour) is
  keyed by `PlayerId`; there is no "local player" in the sim. Shared things (crate, strip, purse) are shared.
- **First request wins** is real from M2: two players' commands in one tick are applied in submission order, and the
  loser gets a rejection (`Taken`). Headless tests run at least two players.
- **Unity:** input never calls `SimWorld` directly; it submits through an `ICommandSink` (a local loopback for now, the
  network transport later). Views read the local player's id from the session, never assume player 0, and draw every
  player in the state.
- **State is plain data** (`docs/decisions/0014`), so it can become the save/join snapshot later.
- **No pause dependence:** nothing in M2 relies on pausing (co-op never pauses).

## Clashes
- **First request wins:** if two players grab the same pot or slot, the request that reaches the host first gets it. The other sees a brief "taken" cue and their hands stay empty.
- Two players can work the same bench at once.
- Smart targeting skips a plant another player is already feeding or catching.

## Disconnects
- A client who drops leaves what they held on the ground where they stood; anyone can pick it up.
- If the host drops, the session ends as for the host leaving.

The networking stack is decided when co-op is planned (candidates: Netcode for GameObjects + Steam transport, FishNet).
