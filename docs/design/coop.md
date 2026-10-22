# Co-op (M3)

| Rule | Decision |
|---|---|
| Players | 1–4. Solo is a session with one player |
| Authority | The host's sim is the only authority. Clients send commands and receive events |
| Money, XP, build inventory | Shared, owned by the nursery |
| Hands | Per player |
| Sleeping | Every player readies up at a bed (any bed counts). Sleep starts when everyone is ready. The host can force sleep |
| Joining | Mid-game join: the client receives the full state snapshot (same format as a save), then live events |
| Host leaves | The session ends. Clients see "Host left, progress is saved in their game" |
| Pause | None in co-op (see `time.md`) |
| Griefing | Friends-only. No permissions. The host can kick |

Even in M1, every player action must be a sim command carrying a `PlayerId`, and the sim must never assume there is only one player.
The networking stack is decided in M3 (candidates: Netcode for GameObjects + Steam transport, or FishNet).
