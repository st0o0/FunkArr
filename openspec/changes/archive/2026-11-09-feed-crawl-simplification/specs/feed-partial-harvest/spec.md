## MODIFIED Requirements

### Requirement: Evaluate collected items despite page failures

When some page processing fails during a crawl, the FeedCrawlManager SHALL still collect results from successful PageProcessed responses. Page errors are isolated per-page — a failed page does not affect other pages being processed concurrently in the pool.

#### Scenario: One page fails, others succeed

- **WHEN** a crawl dispatches 10 pages and 1 PageProcessed has an error
- **THEN** accepted entries from the 9 successful pages are collected
- **AND** the crawl outcome is `CompletedWithGap`

#### Scenario: Pages processed concurrently and independently

- **WHEN** multiple pages are in-flight in the pool simultaneously
- **AND** one page's MVW fetch fails
- **THEN** other in-flight pages continue processing independently
- **AND** the Manager continues dispatching new pages for remaining offsets

#### Scenario: All pages fail

- **WHEN** all PageProcessed responses have errors and 0 entries collected
- **THEN** the crawl outcome is `Failed`
- **AND** no Publishing phase is entered

### Requirement: CompletedWithGap crawl status

The system SHALL distinguish between fully successful crawls (`Completed`), partially successful crawls (`CompletedWithGap`), and failed crawls (`Failed`). The FeedCrawlRunState SHALL track whether any PageProcessed had an error for gap detection.

#### Scenario: CompletedWithGap outcome

- **WHEN** a crawl has some PageProcessed with errors but collected entries from others
- **THEN** the crawl outcome is `CompletedWithGap`
- **AND** the gap flag is `true`
- **AND** `NewItems` reflects only items from successful pages
- **AND** `Published` reflects accepted entries from those items

### Requirement: Cursor advances on partial harvest

The FeedCrawlManager SHALL advance the cursor when a `CompletedWithGap` crawl collected entries, treating it as a successful crawl with a gap.

#### Scenario: CompletedWithGap with entries advances cursor

- **WHEN** a crawl completes with `CompletedWithGap` and collected entries > 0
- **THEN** the cursor advances to `listTimestamp`
- **AND** the gap flag is set to `true`
- **AND** the consecutive failure counter resets to 0

#### Scenario: CompletedWithGap without entries counts as failure

- **WHEN** a crawl completes with `CompletedWithGap` and 0 entries collected
- **THEN** the cursor does NOT advance
- **AND** the consecutive failure counter increments

### Requirement: Accurate unmatched count

The `unmatched` field in the crawl result SHALL reflect the total number of items across all successful pages that did not match any ruleset. The FeedCrawlRunState SHALL aggregate unmatched counts from each PageProcessed.

#### Scenario: Aggregated unmatched count

- **WHEN** page 1 has 100 unmatched items and page 2 has 50 unmatched items
- **THEN** the total unmatched count is 150

#### Scenario: Failed pages excluded from unmatched

- **WHEN** a page fails with an error
- **THEN** its items SHALL NOT be counted in the unmatched total
