## Purpose

Rewrites the `FeedCrawlManager` so the entire crawl — from probe through publish — runs inside a single stream pipeline, eliminating `FeedCrawlPlan`, `FeedCrawlRun`, and `RunId`.

## MODIFIED Requirements

### Requirement: Probe and ruleset loading run inside the stream
The `FeedCrawlManager` SHALL offer a `Signal` to the stream queue on each tick. The stream SHALL probe MediathekViewWeb, branch on unchanged vs new data, load rulesets, and run the full crawl pipeline — all inside the stream. The actor SHALL NOT use `Ask`/`PipeTo` chains outside the stream for probe or ruleset loading.

#### Scenario: Unchanged probe flows through stream
- **WHEN** the timer ticks and the probe reports a list timestamp not newer than the cursor
- **THEN** the stream SHALL emit `FeedCrawlUnchanged` to the actor without loading rulesets or starting a crawl pipeline

#### Scenario: Full crawl flows through stream
- **WHEN** the probe reports a newer list timestamp
- **THEN** the stream SHALL load rulesets, build a `CrawlContext`, and run the full page→eval→publish pipeline, emitting a `FeedCrawlCompleted` or `FeedCrawlFailed` at the end

#### Scenario: Ruleset load failure inside stream
- **WHEN** the ruleset resolver fails during `LoadContext`
- **THEN** the stream SHALL recover and emit `FeedCrawlFailed` without crashing the long-lived stream

### Requirement: No FeedCrawlPlan or FeedCrawlRun records
The actor SHALL NOT use `FeedCrawlPlan` or `FeedCrawlRun` records. Crawl parameters SHALL be carried by a `CrawlContext` record built inside the stream. Progress tracking SHALL use simple counters (`_collected`, `_matched`) on the actor, incremented by progress events from WireTaps.

#### Scenario: Progress events increment counters
- **WHEN** the stream processes items
- **THEN** `FeedItemCollected` and `FeedItemMatched` events SHALL be sent to the actor via WireTap, incrementing counters that are combined with the stream outcome on completion

#### Scenario: Counters reset on outcome
- **WHEN** any `FeedCrawlOutcome` arrives at the actor
- **THEN** `_collected` and `_matched` SHALL be reset to zero

### Requirement: No RunId or WhenCurrent guards
Concurrency control SHALL rely on the `Source.Queue(1, DropNew)` backpressure instead of `RunId` matching. The actor SHALL handle `OfferAsync` results: if the queue is full (crawl running), reschedule the next tick without error.

#### Scenario: Concurrent tick while crawl is running
- **WHEN** a tick fires while a crawl is in the stream pipeline
- **THEN** `OfferAsync` SHALL return `DropNew` and the actor SHALL reschedule the next tick

### Requirement: Stream aggregates batch results
The inner crawl pipeline SHALL use `Aggregate` (fold) to combine `BatchResult` records into `CrawlTotals`, then produce a single `FeedCrawlCompleted` outcome. The actor SHALL NOT track per-batch progress.

#### Scenario: Multiple batches fold into one outcome
- **WHEN** a crawl produces three publish batches with 2/0/10, 1/1/5, and 3/0/8 (groups/failed/published)
- **THEN** the stream SHALL emit one `FeedCrawlCompleted` with 6 groups, 1 failed group, and 23 published

#### Scenario: Empty crawl (no items in window)
- **WHEN** all pages are outside the window
- **THEN** `Aggregate` SHALL emit `CrawlTotals.Empty` and the outcome SHALL be `FeedCrawlCompleted` with zeros

## REMOVED Requirements

### Requirement: Actor-side probe and ruleset orchestration
The `StartRun` → `HandleProbe` → `Offer` handler chain, the `Probed`, `RuleSetsLoaded`, `RuleSetsFailed`, and `CrawlOffered` internal messages, and the `WhenCurrent(runId)` guard pattern are removed.
