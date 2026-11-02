## Purpose

Defines the pure data pipeline pattern for the feed crawl stream: CrawlInput in, CrawlResult out, errors as data, counting in closures, no actor-stream coupling.

## MODIFIED Requirements

### Requirement: Single ConcatMany at the page fan-out
The stream SHALL have exactly one `ConcatMany` — at the point where a `CrawlStep` fans out into pages. Probe and ruleset loading SHALL be plain `SelectAsync` stages that transform one record into another, not `Source.FromTask` or nested `ConcatMany`.

#### Scenario: Probe and rulesets as SelectAsync
- **WHEN** the stream processes a `CrawlInput`
- **THEN** `Probe` SHALL be a `SelectAsync(1)` producing `ProbeResult` and `LoadOrSkip` SHALL be a `SelectAsync(1)` producing `CrawlStep`, with no `Source.FromTask` or `ConcatMany` before the page fan-out

#### Scenario: Three crawl outcomes from one branch point
- **WHEN** `CrawlStep` is `Ready`, `Unchanged`, or `Failed`
- **THEN** the single `ConcatMany` SHALL route to either the full pipeline source, or a `Source.Single(CrawlResult)` for short-circuit cases

### Requirement: Errors flow as data records
Every async stage in the pipeline (probe, page fetch, evaluation, publish) SHALL produce a result record with an `Error?` field instead of throwing exceptions. The stream SHALL NOT use `Recover` to catch errors. Unexpected exceptions SHALL be handled by a `LoggingDecider` that resumes and drops the element.

#### Scenario: Failed page fetch
- **WHEN** a page fetch throws an HTTP exception
- **THEN** the stage SHALL produce a `PageEvaluation` with `Error` set, not propagate the exception

#### Scenario: Failed evaluation
- **WHEN** an evaluation actor fails
- **THEN** the stage SHALL produce an `EvalResult` with `Error` set, and the `Aggregate` SHALL count it as a failed group

#### Scenario: Unexpected exception
- **WHEN** an unexpected exception occurs in any stage
- **THEN** the `LoggingDecider` SHALL log and resume, dropping that element without killing the stream

### Requirement: Counting in stream closures
Item counting (`collected`, `matched`) SHALL happen in closure-captured mutable variables inside `RunPipeline`, not via WireTap messages to the actor. The actor SHALL NOT have `_collected` or `_matched` fields.

#### Scenario: Counts in CrawlResult
- **WHEN** a crawl processes 100 items of which 60 match a ruleset
- **THEN** the `CrawlResult` SHALL carry `NewItems=100` and `Unmatched=40`, computed entirely within the stream

### Requirement: CrawlInput carries all context
The actor SHALL offer `CrawlInput(Cursor, Options, StartedAt)` to the queue. The stream SHALL NOT read actor fields (`_state`, `_feedOptions`). Duration SHALL be computed in the stream from `StartedAt`.

#### Scenario: Options snapshot
- **WHEN** `FeedOptions` change after a crawl starts
- **THEN** the running crawl SHALL use the options from `CrawlInput`, not the current actor options

### Requirement: One CrawlResult with everything
The stream SHALL produce exactly one `CrawlResult` per crawl containing status, list timestamp, collected count, published count, unmatched count, gap flag, duration, and error. The actor SHALL NOT combine stream output with local state to build the persistence event.

#### Scenario: CrawlResult drives persistence directly
- **WHEN** a `CrawlResult` with status `Completed` arrives
- **THEN** the actor SHALL persist `FeedCursorAdvanced` using only fields from `CrawlResult`, without consulting any actor-side counters

## REMOVED Requirements

### Requirement: Progress events from stream to actor
`FeedItemCollected`, `FeedItemMatched`, `FeedPageCrawled` progress events, WireTap-based Tell to Self, and actor-side counter fields are removed.

### Requirement: FeedCrawlOutcome hierarchy
The `FeedCrawlOutcome` abstract record and its variants (`FeedCrawlCompleted`, `FeedCrawlUnchanged`, `FeedCrawlFailed`) are removed, replaced by `CrawlResult`.

### Requirement: FeedRunOutcome intermediate record
`FeedRunOutcome` and `FeedRunStatus` are removed. The actor maps `CrawlResult` directly to persistence events.
