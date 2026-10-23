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

## Clashes
- **First request wins:** if two players grab the same pot or slot, the request that reaches the host first gets it. The other sees a brief "taken" cue and their hands stay empty.
- Two players can work the same bench at once.
- Smart targeting skips a plant another player is already feeding or catching.

## Disconnects
- A client who drops leaves what they held on the ground where they stood; anyone can pick it up.
- If the host drops, the session ends as for the host leaving.

The networking stack is decided when co-op is planned (candidates: Netcode for GameObjects + Steam transport, FishNet).
