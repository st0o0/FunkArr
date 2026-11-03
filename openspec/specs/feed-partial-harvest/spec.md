# feed-partial-harvest Specification

## Purpose

Defines evaluation and cursor behavior when a feed crawl harvest partially fails. TBD: refine.

## Requirements

### Requirement: Evaluate collected items despite page failures

When some MVW page fetches fail during harvest, the FeedCrawlWorker SHALL still
evaluate all successfully-collected items against registered rulesets. Page errors
SHALL prevent dispatching new page fetches but SHALL NOT skip the evaluation phase.

#### Scenario: One page fails, others succeed

- **WHEN** a feed crawl fetches 10 pages and 1 page returns HTTP 500
- **THEN** items from the 9 successful pages are evaluated against rulesets
- **AND** matching items are published as feed entries
- **AND** the crawl outcome is `CompletedPartial`

#### Scenario: Last page message is a failure

- **WHEN** the failed page's result message arrives after all successful pages
- **THEN** the evaluation phase still runs (no race condition)
- **AND** the same items are published as when a successful page arrives last

#### Scenario: All pages fail

- **WHEN** all page fetches fail and 0 items are collected
- **THEN** the crawl outcome is `Failed`
- **AND** the evaluation phase is skipped (nothing to evaluate)

### Requirement: CompletedPartial crawl status

The system SHALL distinguish between fully successful crawls (`Completed`),
partially successful crawls (`CompletedPartial`), and failed crawls (`Failed`).

#### Scenario: CompletedPartial outcome

- **WHEN** a crawl has page errors but collected items were evaluated
- **THEN** the `CrawlResult` status is `CompletedPartial`
- **AND** the gap flag is `true`
- **AND** the `NewItems` count reflects only successfully-collected items
- **AND** the `Published` count reflects evaluated-and-accepted items

### Requirement: Cursor advances on partial harvest

The FeedCrawlManager SHALL advance the cursor when a `CompletedPartial` crawl
collected and evaluated items, treating it as a successful crawl with a gap.

#### Scenario: CompletedPartial with items advances cursor

- **WHEN** a crawl completes with status `CompletedPartial` and collected items > 0
- **THEN** the cursor advances to `listTimestamp`
- **AND** the gap flag is set to `true`
- **AND** the consecutive failure counter resets to 0

#### Scenario: CompletedPartial without items counts as failure

- **WHEN** a crawl completes with status `CompletedPartial` and collected items = 0
- **THEN** the cursor does NOT advance
- **AND** the consecutive failure counter increments

### Requirement: Accurate unmatched count

The `unmatched` field in `CrawlResult` SHALL reflect the actual number of items
that were evaluated and did not match any ruleset. Items that were never evaluated
SHALL NOT be counted as unmatched.

#### Scenario: Items evaluated, some unmatched

- **WHEN** 1000 items are collected and 100 match rulesets
- **THEN** `unmatched` is 900

#### Scenario: No items collected (all pages failed)

- **WHEN** 0 items are collected due to all pages failing
- **THEN** `unmatched` is 0 (not equal to `NewItems`)
