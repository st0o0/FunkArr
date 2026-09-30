## Why

`MatchMagicManager` accepts an `IActorRef historyRef` via constructor injection and passes it through the `ExecuteScoring` message to pool workers. This violates two conventions: no `IActorRef` in constructors (use runtime resolution) and no `IActorRef` in messages (couples messages to actor topology). Additionally, the registration never passes the parameter, so `historyRef` is always `ActorRefs.Nobody` — match history is silently never recorded.

## What Changes

- Remove `IActorRef? historyRef` constructor parameter from `MatchMagicManager`
- Remove `IActorRef HistoryRef` field from `ExecuteScoring` message
- `MatchMagicActor` resolves `IMatchHistoryRegion` at runtime via `Context.GetActor<IMatchHistoryRegion>()` and sends `RecordScoringResult` directly to the shard region
- This also fixes the bug where match history was never being recorded

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this is an internal implementation fix. The matchmagic-evaluation spec describes scoring behavior, not how actor refs are wired.

## Impact

- `MatchMagicManager.cs` — simplified constructor, no more `_historyRef` field
- `ExecuteScoring.cs` — one field removed from the record
- `MatchMagicActor.cs` — gains runtime resolution of history shard region, now actually writes history
- Existing tests may need adjustment for the changed `ExecuteScoring` signature
