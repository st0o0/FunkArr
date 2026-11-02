## Purpose

Defines the flat per-crawl stream pattern: one short-lived stream from Source.Single through Sink.Head, no ConcatMany, no helper classes, all logic inline.

## MODIFIED Requirements

### Requirement: Flat short-lived stream per crawl
The `FeedCrawlManager` SHALL materialize a fresh short-lived stream for each crawl using `Source.Single(input)` → stages → `RunWith(Sink.Head)` → `PipeTo(Self)`. The stream SHALL NOT use `Source.Queue`, `ConcatMany`, or any stream-in-stream pattern.

#### Scenario: Normal crawl
- **WHEN** a crawl timer fires and the probe reports new data
- **THEN** the stream SHALL fan out page offsets via `SelectMany`, process all pages through a flat pipeline, and deliver one `CrawlResult` via `PipeTo`

#### Scenario: Unchanged probe
- **WHEN** the probe shows no update
- **THEN** the prepare stage SHALL set `shortCircuit` and return an empty offsets array, and the final `Select` SHALL emit `CrawlResult.ForUnchanged`

#### Scenario: Stream failure
- **WHEN** an unexpected exception occurs in any stage
- **THEN** `PipeTo` failure handler SHALL produce `CrawlResult.ForFailed` — no `StreamCompleted`/`StreamFailed` handlers needed

### Requirement: All logic inline in lambdas
All stage logic SHALL be directly in stream lambdas. There SHALL be no `CrawlContext`, `FeedCrawlPlanner`, or `CrawlContextResponses` helper classes. DI dependencies (`_mediathek`, `_ruleSetResolver`, `_tvEvaluation`, `_movieEvaluation`, `_store`) SHALL be captured in closures.

#### Scenario: Page fetch is inline
- **WHEN** a page offset is processed by SelectAsync
- **THEN** the lambda SHALL build the `QueryMediathek` message, send the Ask, evaluate the response into a `PageEvaluation`, and handle errors — all in one lambda, no helper method

#### Scenario: Evaluation response matching is inline
- **WHEN** an evaluation actor returns a response
- **THEN** the lambda SHALL pattern-match the response directly into an `EvalResult` with error-as-data — no `ItemsOrThrow` extension methods

### Requirement: SelectMany for fan-out instead of ConcatMany
The 1-to-N expansion from one crawl to many page offsets SHALL use `SelectMany(offsets => offsets)` on an `int[]` produced by the prepare stage. This keeps the stream flat — no sub-source creation.

#### Scenario: Fan-out to page offsets
- **WHEN** the prepare stage completes successfully with PageSize=100, MaxPages=3
- **THEN** `SelectMany` SHALL expand `[0, 100, 200]` into three individual elements in the same stream

### Requirement: Closure-scoped state per crawl
Each crawl SHALL have its own closure-scoped variables (`lookup`, `window`, `listTimestamp`, `shortCircuit`, `collected`, `matched`, `pageError`) declared in `StartCrawl`. These SHALL NOT be actor fields.

#### Scenario: Concurrent crawl isolation
- **WHEN** the actor materializes a new crawl
- **THEN** all closure variables are fresh — no stale state from previous crawls

## REMOVED Requirements

### Requirement: Long-lived Source.Queue stream
The `Source.Queue` + `Sink.ActorRef` long-lived stream pattern, `OfferAsync`/`OfferResult` handling, `CrawlStreamCompleted`/`CrawlStreamFailed` crash handlers, and `StreamSupervision.LoggingDecider` are removed.

### Requirement: CrawlContext and FeedCrawlPlanner
The `CrawlContext` record with methods (`Read`, `Match`, `WithinLead`, `PageQuery`, `Group`, `EvaluateSeries`, `EvaluateMovies`, `ProbeQuery`, `Create`) and the `FeedCrawlPlanner` static class (`HasUpdate`, `Window`, `EvaluatePage`, `DistinctByStableId`, `PrimaryUrl`, `WithinLead`) are removed. Their logic is inlined in stream lambdas.

### Requirement: CrawlStep hierarchy for stream branching
The `CrawlStep`/`CrawlReady`/`CrawlUnchanged`/`CrawlProbeFailed` hierarchy used for ConcatMany dispatch is removed. Short-circuit is handled by the `shortCircuit` closure + empty offsets pattern.
