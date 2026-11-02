## ADDED Requirements

### Requirement: FeedCrawler is a non-persistent child actor
FeedCrawlManager SHALL spawn a `FeedCrawler` child actor for each crawl run. FeedCrawler SHALL be a plain `ReceiveActor` (non-persistent) that executes exactly one crawl and then stops. FeedCrawlManager SHALL NOT contain crawl execution logic (page fetching, evaluation dispatch, gating, publishing) — only cursor persistence, timer scheduling, spawning FeedCrawler, and handling the result.

#### Scenario: Manager spawns crawler on tick
- **WHEN** FeedCrawlManager receives a Tick or StartFeedCrawl and no crawl is running
- **THEN** it SHALL spawn a FeedCrawler child actor with the current cursor, feed options snapshot, and actor references, and set `_running = true`

#### Scenario: Manager does not spawn concurrent crawlers
- **WHEN** FeedCrawlManager receives a Tick while a FeedCrawler is already running
- **THEN** it SHALL NOT spawn a second FeedCrawler

#### Scenario: Crawler stops after reporting result
- **WHEN** FeedCrawler has sent a CrawlResult to its parent
- **THEN** it SHALL call `Context.Stop(Self)` to terminate

### Requirement: Become-based probe phase
FeedCrawler SHALL use `Become` to separate the probe/setup phase from the crawl phase. The initial phase (Probing) SHALL resolve the list timestamp and rulesets via actor message exchanges before materializing any stream.

#### Scenario: Probe asks MediathekManager for list timestamp
- **WHEN** FeedCrawler starts in the Probing phase
- **THEN** it SHALL Ask MediathekManager for the current list timestamp using a single-item query (the same probe query as today), and handle the response as a typed message via PipeTo

#### Scenario: Probe fetches registered rulesets
- **WHEN** the list timestamp probe succeeds and the timestamp is newer than the cursor
- **THEN** FeedCrawler SHALL Ask RuleSetResolver for registered rulesets, build a RuleSetLookup, and compute the CrawlWindow — all before materializing the stream

#### Scenario: Probe detects unchanged list
- **WHEN** the probe response shows listTimestamp <= cursor
- **THEN** FeedCrawler SHALL send `CrawlResult.ForUnchanged` to its parent and stop, without materializing a stream

#### Scenario: Probe failure
- **WHEN** the probe Ask times out or returns an error response
- **THEN** FeedCrawler SHALL send `CrawlResult.ForFailed` to its parent and stop

### Requirement: Crawling phase materializes stream with immutable input
After the probe phase completes successfully, FeedCrawler SHALL `Become(Crawling)` and materialize the Akka.Streams pipeline. The stream SHALL receive all context (CrawlWindow, RuleSetLookup, page offsets, options) as immutable input — no mutable closure-captured variables for probe/setup state.

#### Scenario: Stream receives pre-resolved context
- **WHEN** FeedCrawler enters the Crawling phase
- **THEN** the stream SHALL be materialized with the CrawlWindow, RuleSetLookup, and computed page offsets already available as local variables, not as `null?` closures that are set mid-stream

#### Scenario: Stream result forwarded to manager
- **WHEN** the stream completes and produces a CrawlResult
- **THEN** FeedCrawler SHALL forward the CrawlResult to its parent (FeedCrawlManager) and stop

#### Scenario: Stream materialization failure
- **WHEN** the stream PipeTo fails with an unexpected exception
- **THEN** FeedCrawler SHALL send `CrawlResult.ForFailed` to its parent and stop

### Requirement: FeedCrawlManager watches crawler lifecycle
FeedCrawlManager SHALL use DeathWatch (`Context.Watch`) on the spawned FeedCrawler to detect unexpected termination.

#### Scenario: Crawler terminates without sending result
- **WHEN** FeedCrawlManager receives a `Terminated` message for the FeedCrawler without having received a CrawlResult
- **THEN** it SHALL treat this as a failed crawl, set `_running = false`, and schedule the next tick

#### Scenario: Normal termination after result
- **WHEN** FeedCrawlManager receives a CrawlResult followed by Terminated
- **THEN** it SHALL process the CrawlResult normally (persist cursor, schedule next) and ignore the subsequent Terminated message

### Requirement: FeedCrawlManager persistence behavior unchanged
FeedCrawlManager SHALL continue to persist FeedCursorAdvanced and FeedRunFailed events, manage the cursor, handle consecutive failure logic, and snapshot exactly as it does today. The HandleResult method logic SHALL remain in FeedCrawlManager.

#### Scenario: Completed crawl persists cursor
- **WHEN** FeedCrawlManager receives a CrawlResult with status Completed
- **THEN** it SHALL persist FeedCursorAdvanced with the result's fields, update state, and schedule next — identical to current behavior

#### Scenario: Status query still answered by manager
- **WHEN** FeedCrawlManager receives QueryFeedCrawlStatus
- **THEN** it SHALL respond with the current state, including whether a crawler is running
