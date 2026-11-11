# Feed Actor Topology

Replaces the current 3-actor / pool topology with 3 actors / parent-child.

## Actors

### FeedCrawlManager (persistent singleton)

- Timer scheduling (IWithTimers, periodic Tick)
- Cursor persistence (FeedCursorAdvanced, FeedRunFailed events)
- Status queries (QueryFeedCrawlStatus)
- Options monitoring (IOptionsMonitor<FeedOptions>)
- Creates FeedHarvestActor as long-lived child at RecoveryCompleted

No Become states. No pipeline knowledge. No pool management.

### FeedHarvestActor (long-lived child of Manager)

- Non-persistent ReceiveActor
- Created once at Manager's RecoveryCompleted, lives as long as the Manager
- Resolves dependencies via `Context.GetActor<T>()` (not constructor params)
- Single `Receive<StartHarvest>` handler builds and runs a materialized
  Akka.Streams pipeline, result goes via `PipeTo(Sender)` to the Manager
- No Become states, no mutable state

### FeedStoreManager (persistent singleton) — unchanged

## Deleted

- FeedCrawlPageActor (pool routees)
- SmallestMailboxPool router
- FeedCrawlPageActorState
- FeedCrawlPageMessages (ProcessPage, ProcessPageResponse)
- FeedCrawlRunState

## Hierarchy

```
AkkaSetupContainer
├── FeedCrawlManager (singleton)
│   └── FeedHarvestActor (child, created once)
└── FeedStoreManager (singleton)
```
