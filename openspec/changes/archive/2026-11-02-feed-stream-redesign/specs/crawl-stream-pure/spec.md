## MODIFIED Requirements

### Requirement: Single ConcatMany at the page fan-out
Probe and ruleset loading are no longer stream stages — they are handled by the FeedCrawler actor's Probing phase before the stream materializes. The stream SHALL receive pre-computed page offsets as input via `Source.From(offsets)`. The stream SHALL NOT contain probe, ruleset-fetch, or CrawlStep branching stages. If the stream needs a fan-out (e.g., `SelectMany` for page offsets to items), it SHALL use at most one `SelectMany` or equivalent — no `ConcatMany` or nested sources.

#### Scenario: Stream starts from page offsets
- **WHEN** FeedCrawler materializes the crawl stream
- **THEN** the stream source SHALL be `Source.From(pageOffsets)` where pageOffsets is an `IEnumerable<int>` computed before materialization, not a `Source.Single(CrawlInput)` that resolves probe/rulesets inline

#### Scenario: No probe or ruleset stages in stream
- **WHEN** inspecting the stream graph
- **THEN** there SHALL be no `SelectAsync` stage that asks MediathekManager for a list timestamp or asks RuleSetResolver for rulesets — those are actor-phase concerns

### Requirement: CrawlInput carries all context
The stream SHALL receive all execution context as immutable local variables or a context record. This includes: CrawlWindow (lower/upper bounds, truncated flag), RuleSetLookup, page offsets, max concurrency, min confidence, lead limit, and start timestamp. The stream SHALL NOT read actor fields or mutable closure-captured setup state.

#### Scenario: Options snapshot
- **WHEN** `FeedOptions` change after a crawl starts
- **THEN** the running crawl SHALL use the options snapshot taken when FeedCrawlManager spawned the FeedCrawler, not the current live options

#### Scenario: Immutable context in stream
- **WHEN** the stream graph references CrawlWindow or RuleSetLookup
- **THEN** these SHALL be non-null local variables captured by closures, not nullable fields set mid-stream

### Requirement: Errors flow as data records
Every async stage in the pipeline (page fetch, evaluation, publish) SHALL produce a result record with an `Error?` field instead of throwing exceptions. The stream SHALL NOT use `Recover` to catch errors. Unexpected exceptions SHALL be handled by a `LoggingDecider` that resumes and drops the element.

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
Item counting (`collected`, `matched`) SHALL happen in closure-captured mutable variables within the stream pipeline method, not via WireTap messages to the actor. The FeedCrawler actor SHALL NOT have `_collected` or `_matched` fields.

#### Scenario: Counts in CrawlResult
- **WHEN** a crawl processes 100 items of which 60 match a ruleset
- **THEN** the `CrawlResult` SHALL carry `NewItems=100` and `Unmatched=40`, computed entirely within the stream

### Requirement: One CrawlResult with everything
The stream SHALL produce exactly one `CrawlResult` per crawl containing status, list timestamp, collected count, published count, unmatched count, gap flag, duration, and error. The FeedCrawler actor SHALL NOT combine stream output with local state to build the CrawlResult — it forwards the stream's result to FeedCrawlManager as-is.

#### Scenario: CrawlResult drives persistence directly
- **WHEN** a `CrawlResult` with status `Completed` arrives at FeedCrawlManager
- **THEN** the manager SHALL persist `FeedCursorAdvanced` using only fields from `CrawlResult`, without consulting any actor-side or crawler-side counters
