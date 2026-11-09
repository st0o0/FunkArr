## MODIFIED Requirements

### Requirement: Crawl outcome status

The FeedCrawlWorker SHALL produce one of four crawl outcomes using the
`CrawlOutcome` enum from `FunkArr.Messages.Feed`: `Completed`,
`CompletedWithGap`, `Failed`, or `Unchanged`. The domain-internal `CrawlStatus`
enum SHALL be removed; `CrawlResult.Status` SHALL use `CrawlOutcome` directly.

#### Scenario: All pages succeed

- **WHEN** all page fetches succeed and evaluation completes
- **THEN** the crawl outcome is `CrawlOutcome.Completed`

#### Scenario: Some pages fail, items collected

- **WHEN** some page fetches fail but items were collected and evaluated
- **THEN** the crawl outcome is `CrawlOutcome.CompletedWithGap`
- **AND** `gap` is `true`

#### Scenario: All pages fail

- **WHEN** all page fetches fail and 0 items are collected
- **THEN** the crawl outcome is `CrawlOutcome.Failed`
- **AND** `CrawlResult.Error` contains the failure description

#### Scenario: No new data available

- **WHEN** the probe returns a `listTimestamp` equal to or less than the cursor
- **THEN** the crawl outcome is `CrawlOutcome.Unchanged`

## ADDED Requirements

### Requirement: Crawl outcome enum across layers

The crawl outcome SHALL be represented as a typed `CrawlOutcome` enum across
all layers. The `LastOutcome: string?` field SHALL be replaced with
`LastOutcome: CrawlOutcome` plus a separate `LastError: string?` field for
failure details.

#### Scenario: FeedCrawlManagerState uses CrawlOutcome

- **WHEN** `FeedCrawlManagerState` records a crawl result
- **THEN** `LastOutcome` SHALL be of type `CrawlOutcome`
- **AND** `LastError` SHALL be `null` for non-failure outcomes
- **AND** `LastError` SHALL contain the error description when `LastOutcome` is `Failed`

#### Scenario: PersistedFeedCrawlState uses PersistedCrawlOutcome

- **WHEN** `PersistedFeedCrawlState` is inspected
- **THEN** `LastOutcome` SHALL be of type `PersistedCrawlOutcome`
- **AND** `LastError` SHALL be `string?`

#### Scenario: FeedCrawlStatusResult uses CrawlOutcome

- **WHEN** `FeedCrawlStatusResult` is inspected
- **THEN** `LastOutcome` SHALL be of type `CrawlOutcome`
- **AND** `LastError` SHALL be `string?`

#### Scenario: FeedCrawlStatusOutput uses CrawlOutcome

- **WHEN** `FeedCrawlStatusOutput` in `FunkArr.Api` is inspected
- **THEN** `LastOutcome` SHALL be of type `FunkArr.Api.Models.Feed.CrawlOutcome`
- **AND** `LastError` SHALL be `string?`

#### Scenario: CrawlOutcome enum members

- **WHEN** `CrawlOutcome` in any layer is inspected
- **THEN** it SHALL have members `Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4`

### Requirement: CrawlOutcome persistence mapping

Mapping between `CrawlOutcome` and `PersistedCrawlOutcome` SHALL use an
exhaustive switch expression. `Unknown` SHALL fall through to the
`_ => throw` default arm.

#### Scenario: CrawlOutcome round-trip

- **WHEN** `CrawlOutcome.CompletedWithGap` is mapped to persistence and back
- **THEN** the value SHALL round-trip unchanged

#### Scenario: Unknown throws on mapping

- **WHEN** `CrawlOutcome.Unknown` is mapped to persistence
- **THEN** the mapper SHALL throw `ArgumentOutOfRangeException`
