# 0005: Commands in, events out

**Context.** Co-op needs host authority, and tests need to replay player actions without Unity.

**Decision.**
- Every player action is a command class implementing `ICommand`, with a `PlayerId`.
- `SimWorld.Submit(cmd)` queues it. Commands are applied in submission order at the start of the next tick, before systems update.
- Each command returns a `CommandResult`: `Accepted`, or `Rejected` with a reason enum and message.
- The command result is also available immediately through a query-only `Validate(cmd)`, which the build ghost uses.
- State changes raise events (`PlantSown`, `ItemPlaced`, `PlantStageChanged`, `PlantDied`, `WalletChanged`, ...). These are collected in an event list that the Unity layer drains every frame.
- Unity never mutates sim state directly.

**Consequences.** The same command streams drive tests, single player and (in M3) network replication.
