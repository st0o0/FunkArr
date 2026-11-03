# feed-crawl-lifecycle Specification

## Purpose

Defines the FeedCrawlWorker child actor lifecycle and its split from FeedCrawlManager and FeedScheduler. TBD: refine.

## Requirements


### Requirement: Crawl outcome status

The FeedCrawlWorker SHALL produce one of four crawl statuses: `Completed`,
`CompletedPartial`, `Failed`, or `Unchanged`. The `CompletedPartial` status
indicates that some pages failed during harvest but collected items were
successfully evaluated.

#### Scenario: All pages succeed

- **WHEN** all page fetches succeed and evaluation completes
- **THEN** the crawl status is `Completed`

#### Scenario: Some pages fail, items collected

- **WHEN** some page fetches fail but items were collected and evaluated
- **THEN** the crawl status is `CompletedPartial`
- **AND** `gap` is `true`

#### Scenario: All pages fail

- **WHEN** all page fetches fail and 0 items are collected
- **THEN** the crawl status is `Failed`

#### Scenario: No new data available

- **WHEN** the probe returns a `listTimestamp` equal to or less than the cursor
- **THEN** the crawl status is `Unchanged`

### Requirement: Cursor advancement rules

The FeedCrawlManager SHALL advance the cursor based on crawl status:
`Completed` and `CompletedPartial` (with items) advance the cursor.
`Failed` only advances after reaching `MaxConsecutiveFailures`. `Unchanged`
does not modify the cursor.

#### Scenario: CompletedPartial advances cursor

- **WHEN** a crawl finishes with `CompletedPartial` and `NewItems > 0`
- **THEN** the cursor advances to `listTimestamp`
- **AND** `gap` is persisted as `true`
- **AND** `ConsecutiveFailures` resets to 0

#### Scenario: Failed crawl increments failure counter

- **WHEN** a crawl finishes with `Failed`
- **AND** `ConsecutiveFailures < MaxConsecutiveFailures`
- **THEN** the cursor does NOT advance
- **AND** `ConsecutiveFailures` increments
