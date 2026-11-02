## Purpose

Defines how a feed crawl runs: as one stream from the MediathekViewWeb probe to publishing, with bounded parallelism, cancellation and explicit gaps.

## ADDED Requirements

### Requirement: Crawls run through one long-lived stream
The `FeedCrawlManager` SHALL materialize a single Akka.Streams graph once and run every crawl through it: page fetching, window evaluation, de-duplication, ruleset matching, grouping per ruleset, evaluation with the feed gate and batched publishing. The stream SHALL report its progress to the manager as messages; the manager SHALL probe MediathekViewWeb and load the rulesets before offering a crawl.

#### Scenario: Unchanged Filmliste
- **WHEN** the probe reports a list timestamp not newer than the cursor
- **THEN** the crawl SHALL issue exactly one MediathekViewWeb query and report `Unchanged`

#### Scenario: New entries
- **WHEN** a page contains a Tatort entry in the window and an entry without ruleset, and the next page reaches the cursor
- **THEN** the Tatort entry SHALL be evaluated with the ruleset's media IDs and published, the other entry SHALL be counted as unmatched, and the outcome SHALL be `Completed` without gap

### Requirement: Pages are fetched with bounded prefetch
The crawl SHALL keep at most three page queries in flight, preserve page order, and stop requesting pages after the first page that reaches the cursor or is incomplete.

#### Scenario: End on the first page
- **WHEN** the first page already reaches the cursor and 20 pages are allowed
- **THEN** no more than three page queries SHALL be issued

### Requirement: Failures are reported per group
A failed evaluation of one ruleset group SHALL NOT stop other groups. A failed group, a failed publish or a failed MediathekViewWeb query SHALL make the crawl `Failed`, and the crawl stream SHALL keep serving later crawls.

#### Scenario: Failing group
- **WHEN** the evaluation of the only group throws
- **THEN** the outcome SHALL be `Failed` with `1 of 1 ruleset groups failed` and nothing SHALL be published

#### Scenario: Failing page
- **WHEN** a page query answers with an error
- **THEN** the outcome SHALL be `Failed` with `MediathekViewWeb query failed: …` and the cursor SHALL stay

### Requirement: Gaps are explicit
A crawl SHALL report a gap when it stops at `MaxPages` before reaching the cursor, or when the cursor is older than `MaxCatchUp` and the window was truncated.

#### Scenario: Page limit
- **WHEN** `MaxPages` is 1 and the first page is full of entries inside the window
- **THEN** the outcome SHALL be `Completed` with gap

#### Scenario: Long outage
- **WHEN** the cursor is far older than `MaxCatchUp`
- **THEN** the window SHALL start at `ListTimestamp − MaxCatchUp` and the outcome SHALL report a gap

### Requirement: Crawls do not overlap
A crawl request while a crawl is running SHALL be ignored.

#### Scenario: Second start
- **WHEN** `StartFeedCrawl` arrives while a crawl waits for its probe
- **THEN** no second probe SHALL be sent and the status SHALL report the crawl as running
