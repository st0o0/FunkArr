## Context

`MatchMagicManager` (cluster singleton) owns a `SmallestMailboxPool` of `MatchMagicActor` workers for scoring. Match history is recorded by `MatchHistoryWorker` (sharded by `RuleSetId` via `IMatchHistoryRegion`).

Currently the manager accepts `IActorRef? historyRef` in its constructor and threads it through the `ExecuteScoring` internal message to pool workers. The registration never passes the parameter, so `historyRef` defaults to `ActorRefs.Nobody` and history is never written.

## Goals / Non-Goals

**Goals:**
- Remove `IActorRef` from the manager constructor and from the `ExecuteScoring` message
- Use runtime actor resolution (`Context.GetActor<IMatchHistoryRegion>()`) in `MatchMagicActor` — consistent with how all other actors resolve dependencies
- Fix the bug where match history is silently never recorded

**Non-Goals:**
- Changing the scoring flow or message protocol visible to callers
- Modifying `MatchHistoryWorker` or its persistence

## Decisions

### Pool workers resolve the history region themselves

`MatchMagicActor` calls `Context.GetActor<IMatchHistoryRegion>()` to get the shard region ref at construction time and sends `RecordScoringResult` directly. This is the same pattern used by `TvSearchWorker`, `DownloadWorker`, and `RuleSetWorker`.

Alternative considered: manager intercepts `ScoreCompleted` and sends history itself. Rejected because it adds routing complexity (manager must track original senders) and the pool workers are the natural place — they already have all the data.

### RecordScoringResult routing already works

`RecordScoringResult` implements `IWithRuleSetId`, and `ShardMessageExtractor` already handles `IWithRuleSetId` → no changes needed in the shard message extraction layer.

## Risks / Trade-offs

- [Risk] Pool workers now have a dependency on `IMatchHistoryRegion` via `Context.GetActor`. → This is standard practice across the codebase and does not make workers stateful — it's a one-time lookup cached in a field.
- [Risk] Existing tests create `MatchMagicActor` and `MatchMagicManager` with TestProbes for history. → Tests need to be updated to either use `TestActorRef` with actor system that has the region registered, or adjust the test approach. The `MatchMagicActorTests` already use a `historyProbe` pattern that will need adjustment.
