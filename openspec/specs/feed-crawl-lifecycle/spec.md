# Feed Crawl Lifecycle

The FeedCrawlManager uses a thin tell-and-receive pattern with no Become states.

## FeedCrawlManager Lifecycle

The Manager has no Become states. It is always in one state:

```
Receive<Tick>              → if enabled && !_harvesting: tell harvester
Receive<StartFeedCrawl>    → if !_harvesting: tell harvester
Receive<OptionsChanged>    → update state, reschedule if interval changed
Receive<QueryFeedCrawlStatus> → reply with status
Receive<HarvestCompleted>  → persist cursor, record telemetry
Receive<HarvestUnchanged>  → record unchanged telemetry
Receive<HarvestFailed>     → persist failure, track consecutive failures
Receive<Snapshot/Delete*>  → standard persistence cleanup
```

### Guard

`_harvesting` flag set to true when StartHarvest is sent, false when any
harvest response arrives. Prevents overlapping runs.

### Cursor Persistence

- FeedCursorAdvanced on Completed/CompletedWithGap
- FeedRunFailed on failure (with consecutive failure counter)
- Cursor force-advance after MaxConsecutiveFailures (3)
- Snapshot every 20 events

### Timer

- StartSingleTimer on startup (30s delay) and after each run (interval)
- Interval from FeedOptions, updated on OptionsChanged

## FeedHarvestActor Lifecycle

No Become states. Single handler:

```
Receive<StartHarvest>  → build stream → PipeTo(Sender)
```

The stream pipeline handles all phases internally. On completion, the
materialized result is sent to the Manager via PipeTo.

## Messages

### Internal (Feed domain)

```
StartHarvest(long Cursor, FeedOptions Options)

abstract record HarvestResponse
├── HarvestCompleted(IngestSummary Summary)
├── HarvestUnchanged
└── HarvestFailed(string Reason)
```

### IngestSummary (replaces CrawlResult)

```
IngestSummary(
    CrawlOutcome Status, long ListTimestamp,
    int ItemsFound, int Accepted, int Unmatched,
    bool Truncated, TimeSpan Duration, string? Error)
```

### CrawlOutcome enum

Unchanged from Messages layer: Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4
