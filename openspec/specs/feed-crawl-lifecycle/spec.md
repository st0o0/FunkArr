# feed-crawl-lifecycle Specification

## Purpose

Defines the FeedCrawlManager Become state machine lifecycle, self-scheduling, cursor persistence, and crawl outcome handling.

## Requirements

### Requirement: Crawl outcome status

The FeedCrawlManager SHALL produce one of four crawl outcomes using the `CrawlOutcome` enum from `FunkArr.Messages.Feed`: `Completed`, `CompletedWithGap`, `Failed`, or `Unchanged`. Crawl outcomes are determined by aggregating PageProcessed results from the page-pool, not from a child FeedCrawlWorker.

#### Scenario: All pages succeed

- **WHEN** all PageProcessed responses have no error and evaluation completes
- **THEN** the crawl outcome is `CrawlOutcome.Completed`

#### Scenario: Some pages fail, items collected

- **WHEN** some PageProcessed responses have errors but items were collected from others
- **THEN** the crawl outcome is `CrawlOutcome.CompletedWithGap`
- **AND** `gap` is `true`

#### Scenario: All pages fail

- **WHEN** all PageProcessed responses have errors and 0 items are collected
- **THEN** the crawl outcome is `CrawlOutcome.Failed`

#### Scenario: No new data available

- **WHEN** the probe returns a `listTimestamp` equal to or less than the cursor
- **THEN** the crawl outcome is `CrawlOutcome.Unchanged`

### Requirement: FeedCrawlManager Become state machine

FeedCrawlManager SHALL use Become() to transition through 5 states: Idle, Probing, LoadingRuleSets, Crawling, Publishing. Every state SHALL register a handler for QueryFeedCrawlStatus. StartFeedCrawl SHALL only be accepted in Idle state.

#### Scenario: Idle state

- **WHEN** FeedCrawlManager is in Idle state
- **THEN** it SHALL accept StartFeedCrawl (triggers probe) and QueryFeedCrawlStatus
- **AND** it SHALL manage its own crawl timer via IWithTimers

#### Scenario: Probing state

- **WHEN** FeedCrawlManager enters Probing
- **THEN** it SHALL Ask MediathekManager for the latest listTimestamp via PipeTo
- **AND** accept QueryFeedCrawlStatus
- **AND** NOT accept StartFeedCrawl

#### Scenario: LoadingRuleSets state

- **WHEN** FeedCrawlManager enters LoadingRuleSets
- **THEN** it SHALL Ask RuleSetResolver for registered rulesets via PipeTo
- **AND** on success, create FeedCrawlRunState with the window and lookup, then enter Crawling

#### Scenario: Crawling state

- **WHEN** FeedCrawlManager enters Crawling
- **THEN** it SHALL fire initial page requests into the page-pool
- **AND** on each PageProcessed, collect results and fire next page if needed
- **AND** when all pages are done, enter Publishing (if entries collected) or complete the crawl

#### Scenario: Publishing state

- **WHEN** FeedCrawlManager enters Publishing
- **THEN** it SHALL Ask FeedStoreManager with PublishFeedEntries
- **AND** on success, persist cursor advance and return to Idle
- **AND** on failure, persist failure and return to Idle

### Requirement: FeedCrawlManager self-scheduling

FeedCrawlManager SHALL implement IWithTimers and manage its own crawl timer. It SHALL use IOptionsMonitor<FeedOptions> to watch for interval and enabled changes. FeedScheduler SHALL be removed.

#### Scenario: Timer fires in Idle

- **WHEN** the crawl timer fires and FeedCrawlManager is in Idle state
- **THEN** it SHALL start a probe if FeedOptions.Enabled is true
- **AND** reschedule the timer for the configured interval

#### Scenario: Timer fires during active crawl

- **WHEN** the crawl timer fires and FeedCrawlManager is NOT in Idle state
- **THEN** the timer tick SHALL be ignored (crawl already in progress)

#### Scenario: Options change adjusts timer

- **WHEN** IOptionsMonitor detects an interval change
- **THEN** FeedCrawlManager SHALL reschedule the timer with the new interval

#### Scenario: Startup delay

- **WHEN** FeedCrawlManager starts (after recovery)
- **THEN** the first timer tick SHALL fire after a startup delay (30 seconds)

### Requirement: Crawl outcome enum across layers

The crawl outcome SHALL be represented as a typed `CrawlOutcome` enum across all layers. The `LastOutcome: string?` field SHALL be replaced with `LastOutcome: CrawlOutcome` plus a separate `LastError: string?` field for failure details.

#### Scenario: FeedCrawlManagerState uses CrawlOutcome

- **WHEN** `FeedCrawlManagerState` records a crawl result
- **THEN** `LastOutcome` SHALL be of type `CrawlOutcome`
- **AND** `LastError` SHALL be `null` for non-failure outcomes
- **AND** `LastError` SHALL contain the error description when `LastOutcome` is `Failed`

#### Scenario: PersistedFeedCrawlState uses PersistedCrawlOutcome

- **WHEN** `PersistedFeedCrawlState` is inspected
- **THEN** `LastOutcome` SHALL be of type `PersistedCrawlOutcome`
- **AND** `LastError` SHALL be `string?`

#### Scenario: CrawlOutcome enum members

- **WHEN** `CrawlOutcome` in any layer is inspected
- **THEN** it SHALL have members `Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4`

### Requirement: CrawlOutcome persistence mapping

Mapping between `CrawlOutcome` and `PersistedCrawlOutcome` SHALL use an exhaustive switch expression. `Unknown` SHALL fall through to the `_ => throw` default arm.

#### Scenario: CrawlOutcome round-trip

- **WHEN** `CrawlOutcome.CompletedWithGap` is mapped to persistence and back
- **THEN** the value SHALL round-trip unchanged

#### Scenario: Unknown throws on mapping

- **WHEN** `CrawlOutcome.Unknown` is mapped to persistence
- **THEN** the mapper SHALL throw `ArgumentOutOfRangeException`

### Requirement: Cursor advancement rules

The FeedCrawlManager SHALL advance the cursor based on aggregated crawl results: `Completed` and `CompletedWithGap` (with items) advance the cursor. `Failed` only advances after reaching `MaxConsecutiveFailures`. `Unchanged` does not modify the cursor.

#### Scenario: Completed advances cursor

- **WHEN** a crawl finishes with all pages successful and items published
- **THEN** the cursor advances to `listTimestamp`
- **AND** `ConsecutiveFailures` resets to 0

#### Scenario: CompletedWithGap advances cursor

- **WHEN** a crawl finishes with some page errors but items collected
- **THEN** the cursor advances to `listTimestamp`
- **AND** `gap` is persisted as `true`
- **AND** `ConsecutiveFailures` resets to 0

#### Scenario: Failed crawl increments failure counter

- **WHEN** a crawl finishes with `Failed`
- **AND** `ConsecutiveFailures < MaxConsecutiveFailures`
- **THEN** the cursor does NOT advance
- **AND** `ConsecutiveFailures` increments

### Requirement: Transient run state separation

FeedCrawlManager SHALL maintain two state objects: FeedCrawlManagerState (persistent) for cursor and failure tracking, and FeedCrawlRunState (transient, nullable) for active crawl session data. FeedCrawlRunState SHALL be null when no crawl is active and SHALL NOT be persisted.

#### Scenario: Run state lifecycle

- **WHEN** a crawl starts (entering Probing)
- **THEN** FeedCrawlRunState SHALL be created
- **WHEN** the crawl completes or fails (returning to Idle)
- **THEN** FeedCrawlRunState SHALL be set to null

#### Scenario: Crash recovery

- **WHEN** FeedCrawlManager recovers from a crash
- **THEN** FeedCrawlRunState SHALL be null (no active crawl)
- **AND** only FeedCrawlManagerState is restored from the journal
- **AND** the next timer tick starts a fresh crawl
